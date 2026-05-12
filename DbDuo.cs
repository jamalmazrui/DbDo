// =====================================================================
// DbDuo.cs - dual-mode (GUI + dot-prompt CLI) database manager
//
// Browses SQLite, Microsoft Access, Excel, dBASE, and CSV files via
// ADODB Connection and Recordset over COM interop. The same recordset
// is the single source of truth for current position, filter, and sort
// across both the WinForms GUI and the dBase-tradition dot-prompt CLI;
// changes from either side are visible in the other.
//
// User-facing vocabulary follows Microsoft's PowerShell verb canon
// (Common, Data, Diagnostic, Lifecycle, etc.). Internal C# code uses
// idiomatic API terminology (oRecordset.MoveNext, oRecordset.Filter,
// oRecordset.Bookmark, etc.) since that's natural in the language.
//
// Coding style: Camel Type. Lower-camelCase identifiers. Hungarian
// type prefixes (s string, i int, b bool, l list, d dictionary, a
// array, o other object). Constants named like variables but with
// words like "Default" or "Initial" rather than the c_ prefix.
//
// Targets .NET Framework 4.8 / x64. Compiled by buildDbDuo.cmd as
// a /target:winexe /platform:x64. No external runtime dependencies
// beyond ADODB (present on every Windows install since 2000) plus
// the appropriate ODBC or OLE DB driver for whichever file format
// the user opens. See DbDuo.md for deployment requirements.
// =====================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Automation;
using Microsoft.CSharp.RuntimeBinder;

namespace DbDuo
{
    // =====================================================================
    // BuildInfo: single source of truth for the application version
    // string. Update VersionString here on each release; the value
    // surfaces in the About dialog and (via VersionInfoVersion) in the
    // built EXE's file properties. The Inno Setup script reads its own
    // copy of the same value from DbDuo_setup.iss; keep the two in sync.
    // =====================================================================
    public static class BuildInfo
    {
        public const string VersionString = "1.0.21";
    }

    // =====================================================================
    // LiveRegion: hidden announcement surface for screen-reader output.
    //
    // Purpose: a single global "speak this string" sink that works with
    // any active screen reader (JAWS, NVDA, Narrator, Windows Magnifier
    // speech) without DbDuo having to know which one is in memory. The
    // screen reader uses its own active voice; DbDuo's job is to raise
    // the standard accessibility events that screen readers listen for.
    //
    // Mechanism (the right one for .NET Framework 4.8):
    //
    //   1. A real Label control is added to the form, sized 1x1 and
    //      tucked under the menu strip at (0, 0) but
    //      Visible = true (screen readers ignore Visible=false elements).
    //      The Label's LiveSetting property is set to Polite. This is
    //      a built-in WinForms feature added in .NET Framework 4.7.1
    //      and surfaced as a property in 4.8: any text change on a
    //      Label whose LiveSetting is Polite or Assertive automatically
    //      raises a UIA LiveRegionChanged event, which JAWS, NVDA, and
    //      Narrator all listen for.
    //
    //   2. As a belt-and-suspenders fallback, after updating the
    //      Label.Text we also try to fire the UIA Notification event
    //      via reflection. RaiseAutomationNotification, added in the
    //      Windows 10 Fall Creators Update, makes the screen reader
    //      announce arbitrary text without requiring a control to be
    //      shown. The reflection guard means this is silently skipped
    //      on older Windows versions.
    //
    // Why the previous custom-NotifyWinEvent approach didn't work:
    // EVENT_OBJECT_LIVEREGIONCHANGED on its own does not make a
    // control a live region. The screen reader, on receiving the
    // event, queries the source's UIA LiveSetting property; if that
    // property is Off (the WinForms default), the event is ignored.
    // The fix is to set LiveSetting to Polite or Assertive; once set,
    // text changes auto-raise the right event with the right metadata.
    //
    // Caveats:
    //   - Whether a particular screen reader actually announces depends
    //     on its verbosity settings.
    //   - The Label MUST be added to a form's Controls collection
    //     before say() will reach a screen reader. DbDuoForm wires
    //     this up in its constructor.
    //   - Polite (not Assertive) is the right choice: assertive
    //     interrupts whatever the screen reader is currently saying,
    //     which would make DbDuo announcements rude. Polite queues
    //     them after current speech.
    // =====================================================================
    public static class LiveRegion
    {
        private static Label oLabel;

        // Cache the reflection lookup of RaiseAutomationNotification so
        // we don't probe AccessibleObject's metadata every call. The
        // method exists from Windows 10 Fall Creators Update onward
        // (.NET 4.7.1+) and is invoked dynamically because its
        // signature uses internal-only enum types until later .NET
        // versions exposed them.
        private static System.Reflection.MethodInfo oRaiseNotification;
        private static bool bNotificationProbed;

        // Attach the live region to a form. Called once during form
        // construction. The Label is a real on-screen control with
        // LiveSetting=Polite; WinForms automatically raises the UIA
        // LiveRegionChanged event when the Label's Text changes,
        // which JAWS, NVDA, and Narrator all listen for.
        //
        // Placement: 1x1 at the form origin (0, 0). The Label sits
        // tucked under the MenuStrip so it is visually unnoticeable
        // to sighted users, but stays inside the form's client area,
        // which guarantees it participates in the UIA tree. (A
        // far-off-screen Location can sometimes be pruned from the
        // tree by WinForms.)
        public static void attach(Form oForm)
        {
            if (oLabel != null) return;
            oLabel = new Label();
            oLabel.AutoSize = false;
            oLabel.Size = new Size(1, 1);
            oLabel.Location = new Point(0, 0);
            oLabel.TabStop = false;
            oLabel.Visible = true;
            oLabel.Text = "";
            oLabel.AccessibleName = "";
            oLabel.AccessibleRole = AccessibleRole.StaticText;
            oLabel.LiveSetting = AutomationLiveSetting.Assertive;
            oForm.Controls.Add(oLabel);
            // Force the handle to be created so the AccessibleObject
            // is wired up before the first say() call. Without this,
            // the very first announcement can race ahead of UIA tree
            // initialization and be missed.
            IntPtr hForce = oLabel.Handle;
        }

        // Push a string to the live region. The Label's Text change
        // auto-raises the UIA LiveRegionChanged event (because
        // LiveSetting was set to Polite at attach time), and we also
        // fire the UIA Notification event by reflection as a belt-
        // and-suspenders fallback. If the live region was never
        // attached (e.g., CLI-only mode), the call is silently
        // ignored.
        //
        // The say() pipeline. Determines which screen reader is
        // currently running and speaks through that reader's own
        // API so the speech uses the user's configured voice and
        // verbosity. No SAPI fallback -- if no screen reader is
        // running, no speech happens. Per-reader paths:
        //
        //   1. JAWS:     FreedomSci.JawsApi COM object's SayString.
        //                Detection: FindWindow("JFWUI2"), which is
        //                JAWS's top-level UI window class. Avoids the
        //                cost of creating the COM object just to test.
        //
        //   2. NVDA:     nvdaControllerClient64.dll P/Invoke. The DLL
        //                must be shipped alongside DbDuo.exe. NVDA
        //                does NOT expose a COM API; the controller
        //                client is the documented IPC channel.
        //                Detection: nvdaController_testIfRunning()
        //                returns 0 when NVDA is running.
        //
        //   3. Narrator: UIA Notification event via
        //                AccessibleObject.RaiseAutomationNotification.
        //                Narrator does not have a per-app API; it
        //                listens to UIA events. RaiseAutomationNotification
        //                is the documented "announce this text now"
        //                event. Also reaches NVDA and JAWS in their
        //                UIA-enabled modes, but we already covered
        //                those by direct API, so this is effectively
        //                the Narrator path.
        //                Detection: SystemParametersInfo SPI_GETSCREENREADER
        //                returns true when any screen reader is
        //                running. We use this only as a "does anything
        //                care?" hint -- the Notification event is
        //                cheap to fire whether or not Narrator is on.
        //
        // The Label-based live-region path that previous versions
        // used (Label.LiveSetting=Assertive) is preserved as part of
        // the Narrator path because some configurations of NVDA and
        // JAWS in UIA-only mode listen for LiveRegionChanged in
        // addition to / instead of their direct APIs.
        //
        // Priority order: if JAWS is detected, JAWS speaks and we
        // stop (we don't want JAWS to also receive a UIA Notification
        // and speak twice). Same for NVDA. Otherwise UIA Notification
        // fires for Narrator (and any other UIA-listening reader).
        public static void say(string sText)
        {
            string sNew = sText ?? "";
            if (isJawsRunning() && jawsSay(sNew)) { sLastPath = "JAWS COM"; return; }
            if (isNvdaRunning() && nvdaSay(sNew)) { sLastPath = "NVDA controller client"; return; }
            // Fall through to the UIA path for Narrator and any
            // unrecognized UIA-aware reader. Also useful as a debug
            // signal: if you have a screen reader running that's
            // detectable via SPI_GETSCREENREADER but not JAWS or
            // NVDA, this is the path that reaches it.
            sayViaUia(sNew);
            sLastPath = "UIA Notification + LiveRegionChanged (Narrator and others)";
        }

        // Records which path the most recent say() call used. The
        // Test-Reader command displays this so the user can confirm
        // whether they are hearing speech through the direct JAWS
        // COM call, the direct NVDA controller client, or the
        // generic UIA Notification fallback.
        private static string sLastPath = "(none yet)";
        public static string lastSpeechPath()
        {
            return sLastPath;
        }

        // Diagnostic snapshot of the speech pipeline. Returns a
        // multi-line string covering which readers are detected,
        // which DLL is loadable, and which path the most recent
        // say() used. Test-Reader presents this in a MessageBox.
        public static string speechDiagnostic()
        {
            StringBuilder oSb = new StringBuilder();
            oSb.AppendLine("Speech pipeline diagnostic");
            oSb.AppendLine();
            oSb.AppendLine("JAWS running (window class JFWUI2 found): " + (isJawsRunning() ? "yes" : "no"));
            oSb.AppendLine("JAWS COM ProgID FreedomSci.JawsApi reachable: "
                + ((oJawsApi != null) ? "yes (cached)" : "(not yet probed or unavailable)"));
            oSb.AppendLine();
            bool bNvdaDll = false;
            try { nvdaController_testIfRunning(); bNvdaDll = true; }
            catch (DllNotFoundException) { bNvdaDll = false; }
            catch { bNvdaDll = true; }
            oSb.AppendLine("nvdaControllerClient64.dll loadable: " + (bNvdaDll ? "yes" : "no (drop the DLL next to DbDuo.exe to enable NVDA support)"));
            oSb.AppendLine("NVDA running (controller client says so): " + (isNvdaRunning() ? "yes" : "no"));
            oSb.AppendLine();
            // SystemParametersInfo SPI_GETSCREENREADER: a generic
            // "some screen reader is on" probe. Independent of JAWS
            // or NVDA detection.
            oSb.AppendLine("Windows reports any screen reader active: " + (isAnyScreenReaderActive() ? "yes" : "no"));
            oSb.AppendLine();
            oSb.AppendLine("Most recent say() used path: " + sLastPath);
            return oSb.ToString();
        }

        // SystemParametersInfo SPI_GETSCREENREADER (action 70).
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool SystemParametersInfo(int iAction, int iParam, ref bool bResult, int iUpdate);
        private const int SPI_GETSCREENREADER = 70;
        private static bool isAnyScreenReaderActive()
        {
            try
            {
                bool bActive = false;
                if (SystemParametersInfo(SPI_GETSCREENREADER, 0, ref bActive, 0))
                    return bActive;
            }
            catch { }
            return false;
        }

        // ---- JAWS: detection + speak ----
        // JAWS exposes a top-level UI window of class "JFWUI2". This
        // is the cheap detection path -- FindWindow does not require
        // COM startup. (Creating FreedomSci.JawsApi when JAWS is not
        // running is slow and produces a misleading "succeeded but
        // nobody listening" object.)
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string sClass, string sTitle);
        private static bool isJawsRunning()
        {
            try { return FindWindow("JFWUI2", null) != IntPtr.Zero; }
            catch { return false; }
        }

        // Speak through JAWS. The COM object is cached after first
        // successful creation. If COM creation succeeds, we re-use
        // the object across calls; if SayString throws (JAWS
        // crashed or was closed mid-session), reset and let the
        // next call try again.
        private static object oJawsApi;
        private static bool jawsSay(string sText)
        {
            if (string.IsNullOrEmpty(sText)) return false;
            try
            {
                if (oJawsApi == null)
                {
                    Type oType = Type.GetTypeFromProgID("FreedomSci.JawsApi");
                    if (oType == null) return false;
                    oJawsApi = Activator.CreateInstance(oType);
                    if (oJawsApi == null) return false;
                }
                dynamic oJaws = oJawsApi;
                // SayString(text, flush). flush=false queues politely
                // rather than interrupting whatever JAWS is reading.
                object oResult = oJaws.SayString(sText, false);
                return oResult != null && (bool)oResult;
            }
            catch
            {
                oJawsApi = null;
                return false;
            }
        }

        // ---- NVDA: detection + speak ----
        // NVDA ships a Controller Client DLL with C-style exports.
        // The 64-bit version is nvdaControllerClient64.dll; we ship
        // that one (DbDuo is x64). Place the DLL next to DbDuo.exe
        // and the DllImport finds it via the standard Windows DLL
        // search order. If the DLL is missing, DllNotFoundException
        // is caught silently and the NVDA path is unavailable.
        //
        // testIfRunning returns 0 when NVDA is running, non-zero
        // otherwise (it's a Windows error code). speakText takes
        // a wide-char string and returns 0 on success.
        [DllImport("nvdaControllerClient64.dll", CharSet = CharSet.Unicode, EntryPoint = "nvdaController_testIfRunning")]
        private static extern int nvdaController_testIfRunning();
        [DllImport("nvdaControllerClient64.dll", CharSet = CharSet.Unicode, EntryPoint = "nvdaController_speakText")]
        private static extern int nvdaController_speakText([MarshalAs(UnmanagedType.LPWStr)] string sText);
        [DllImport("nvdaControllerClient64.dll", CharSet = CharSet.Unicode, EntryPoint = "nvdaController_cancelSpeech")]
        private static extern int nvdaController_cancelSpeech();

        private static bool bNvdaProbed;
        private static bool bNvdaDllPresent;
        private static bool isNvdaRunning()
        {
            // First call: probe whether the DLL is loadable at all.
            // Subsequent calls skip the probe and just call
            // testIfRunning, which is cheap and reliable.
            if (!bNvdaProbed)
            {
                bNvdaProbed = true;
                try
                {
                    int iResult = nvdaController_testIfRunning();
                    bNvdaDllPresent = true;
                    return iResult == 0;
                }
                catch (DllNotFoundException)
                {
                    bNvdaDllPresent = false;
                    return false;
                }
                catch { bNvdaDllPresent = false; return false; }
            }
            if (!bNvdaDllPresent) return false;
            try { return nvdaController_testIfRunning() == 0; }
            catch { return false; }
        }

        private static bool nvdaSay(string sText)
        {
            if (string.IsNullOrEmpty(sText)) return false;
            if (!bNvdaDllPresent) return false;
            try { return nvdaController_speakText(sText) == 0; }
            catch { return false; }
        }

        // ---- Narrator / generic UIA path ----
        // Update the hidden Label's Text (auto-raises
        // LiveRegionChanged because LiveSetting=Assertive) and also
        // fire the UIA Notification event by reflection. Narrator
        // listens to both event families; the Label path is the
        // documented "live region" approach, the Notification path
        // is the explicit "announce this now" approach.
        private static void sayViaUia(string sText)
        {
            if (oLabel == null) return;
            if (oLabel.IsDisposed) return;
            try
            {
                if (oLabel.InvokeRequired)
                {
                    oLabel.Invoke(new Action<string>(sayViaUia), new object[] { sText });
                    return;
                }
                if (oLabel.Text == sText) oLabel.Text = "";
                oLabel.Text = sText;
                oLabel.AccessibleName = sText;
                raiseUiaNotification(sText);
            }
            catch { }
        }

        // Invoke AccessibleObject.RaiseAutomationNotification by
        // reflection. The method accepts (kind, processing, text,
        // activityId). We pass kind=4 (Other), processing=2 (All),
        // and a unique activity id so screen readers don't dedupe.
        private static void raiseUiaNotification(string sText)
        {
            try
            {
                if (!bNotificationProbed)
                {
                    bNotificationProbed = true;
                    oRaiseNotification = typeof(AccessibleObject)
                        .GetMethod("RaiseAutomationNotification");
                }
                if (oRaiseNotification == null) return;
                if (oLabel == null) return;
                AccessibleObject oAo = oLabel.AccessibilityObject;
                if (oAo == null) return;
                // Args: (AutomationNotificationKind, AutomationNotificationProcessing, string, string)
                // Pass the enum values as ints since the enum types
                // may be internal on some .NET 4.8 SKUs.
                object[] aArgs = new object[]
                {
                    /*AutomationNotificationKind.Other*/ 4,
                    /*AutomationNotificationProcessing.All*/ 2,
                    sText,
                    "DbDuo"
                };
                oRaiseNotification.Invoke(oAo, aArgs);
            }
            catch { /* reflection-path failures are non-fatal */ }
        }
    }

    // =====================================================================
    // ADODB constants. ADODB is COM, so the interop in this file is
    // late-bound through dynamic. Late binding doesn't carry the
    // type library's enum values, so we name them ourselves. The
    // values come from Microsoft's ADO documentation; they are stable
    // since ADO 2.0 (Windows 2000) and won't change.
    //
    // Listed alphabetically within each group. Naming follows Camel
    // Type for constants: lower-camelCase with a descriptive word
    // (Default, Initial, Mode, Type, Position) replacing the older
    // c_ prefix. The ADO-original names are kept verbatim where they
    // are well-known (CursorLocationEnum values etc.), since coders
    // looking at this file will be reading ADO docs alongside.
    // =====================================================================
    public static class AdoConstants
    {
        // CursorLocationEnum
        public const int adUseClient = 3;     // client-side cursor; gives us bookmarks, sort, filter uniformly across providers
        public const int adUseServer = 2;     // (default; not used here)

        // CursorTypeEnum
        public const int adOpenForwardOnly = 0;
        public const int adOpenKeyset      = 1;
        public const int adOpenDynamic     = 2;
        public const int adOpenStatic      = 3;  // we use this; client-side cursors are always static

        // LockTypeEnum
        public const int adLockReadOnly        = 1;
        public const int adLockPessimistic     = 2;
        public const int adLockOptimistic      = 3;  // we use this for editable views
        public const int adLockBatchOptimistic = 4;

        // ConnectModeEnum
        public const int adModeUnknown         = 0;
        public const int adModeRead            = 1;  // read-only mode (we set this when bReadOnly is true)
        public const int adModeWrite           = 2;
        public const int adModeReadWrite       = 3;
        public const int adModeShareDenyRead   = 4;
        public const int adModeShareDenyWrite  = 8;
        public const int adModeShareExclusive  = 12;
        public const int adModeShareDenyNone   = 16;

        // CommandTypeEnum
        public const int adCmdUnknown      = 8;
        public const int adCmdText         = 1;
        public const int adCmdTable        = 2;
        public const int adCmdStoredProc   = 4;
        public const int adCmdTableDirect  = 512;

        // ExecuteOptionEnum
        public const int adExecuteNoRecords = 128;

        // SearchDirectionEnum (for Recordset.Find)
        public const int adSearchForward  = 1;
        public const int adSearchBackward = -1;

        // BookmarkEnum (for Recordset.Find start position)
        public const int adBookmarkCurrent = 0;
        public const int adBookmarkFirst   = 1;
        public const int adBookmarkLast    = 2;

        // PositionEnum
        public const int adPosUnknown = -1;
        public const int adPosBOF     = -2;
        public const int adPosEOF     = -3;

        // GetRowsOptionEnum
        public const int adGetRowsRest = -1;
    }

    // =====================================================================
    // Str: string predicates and small helpers, in the spirit of
    // HomerLib's StringContains / StringEqual / StringEquiv / StringPlural.
    //
    // C# has all of these built in via various overloads of String.*,
    // String.Equals, etc., but reading the code with named predicates is
    // clearer than scattered case-insensitive flag arguments. The "Str"
    // namespace is short on purpose so calls don't overwhelm a line.
    //
    // The constants near the top (space, comma, commaSpace, bar, colon,
    // semicolon, quote) replace magic-string literals throughout the
    // codebase. They're separately useful for distinguishing "comma for
    // SQL" (Str.comma) from "comma-space for display" (Str.commaSpace).
    // =====================================================================
    public static class Str
    {
        // Common literals as named constants -- mirroring HomerLib's
        // xSpace, xComma, xCommaSpace, xBar, xQuote, xColon, xSemicolon.
        public const string space        = " ";
        public const string comma        = ",";
        public const string commaSpace   = ", ";
        public const string bar          = "|";
        public const string colon        = ":";
        public const string semicolon    = ";";
        public const string quote        = "\"";
        public const string apostrophe   = "'";
        public const string newLine      = "\r\n";

        // True for null or empty string. Mirrors HomerLib.IsBlank.
        public static bool isBlank(string s)
        {
            return string.IsNullOrEmpty(s);
        }

        // True for any non-empty string. Mirrors HomerLib.IsNonBlank.
        public static bool isNonBlank(string s)
        {
            return !isBlank(s);
        }

        // Case-sensitive equal. Mirrors HomerLib.StringEqual.
        public static bool equal(string s1, string s2)
        {
            if (s1 == null && s2 == null) return true;
            if (s1 == null || s2 == null) return false;
            return string.Compare(s1, s2, StringComparison.Ordinal) == 0;
        }

        // Case-insensitive equal. Mirrors HomerLib.StringEquiv.
        public static bool equiv(string s1, string s2)
        {
            if (s1 == null && s2 == null) return true;
            if (s1 == null || s2 == null) return false;
            return string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) == 0;
        }

        // True if sText contains sMatch (case-insensitive by default).
        public static bool contains(string sText, string sMatch)
        {
            if (sText == null || sMatch == null) return false;
            return sText.IndexOf(sMatch, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool startsWith(string sText, string sLead)
        {
            if (sText == null || sLead == null) return false;
            return sText.StartsWith(sLead, StringComparison.OrdinalIgnoreCase);
        }

        public static bool endsWith(string sText, string sTrail)
        {
            if (sText == null || sTrail == null) return false;
            return sText.EndsWith(sTrail, StringComparison.OrdinalIgnoreCase);
        }

        // Capitalize first letter, leave the rest. "jamal" -> "Jamal".
        public static string capitalize(string sText)
        {
            if (string.IsNullOrEmpty(sText)) return sText;
            return char.ToUpperInvariant(sText[0]) + sText.Substring(1);
        }

        // Capitalize first letter, lowercase the rest. "JAMAL" -> "Jamal".
        public static string properCase(string sText)
        {
            if (string.IsNullOrEmpty(sText)) return sText;
            return char.ToUpperInvariant(sText[0]) + sText.Substring(1).ToLowerInvariant();
        }

        // Singular or plural form. plural("record", 0) -> "0 records".
        // plural("record", 1) -> "1 record". plural("record", 5) -> "5 records".
        public static string plural(string sItem, int iCount)
        {
            if (iCount == 1) return iCount + " " + sItem;
            return iCount + " " + sItem + "s";
        }

        // Join a list with ", " in display style.
        public static string joinDisplay(IEnumerable<string> lItems)
        {
            return string.Join(commaSpace, lItems);
        }

        // Wrap text to a maximum line length. HomerLib.StringWrap
        // equivalent. Used for long descriptive text in Show-Record.
        public static string wrap(string sText, int iMaxLine)
        {
            if (string.IsNullOrEmpty(sText) || iMaxLine <= 0) return sText ?? "";
            StringBuilder oOut = new StringBuilder();
            foreach (string sLine in sText.Split('\n'))
            {
                string sCur = sLine.TrimEnd('\r');
                while (sCur.Length > iMaxLine)
                {
                    int iBreak = sCur.LastIndexOf(' ', iMaxLine);
                    if (iBreak <= 0) iBreak = iMaxLine;
                    oOut.AppendLine(sCur.Substring(0, iBreak));
                    sCur = sCur.Substring(iBreak).TrimStart();
                }
                oOut.AppendLine(sCur);
            }
            return oOut.ToString().TrimEnd('\r', '\n');
        }
    }

    // =====================================================================
    // Metadata: convention-based names for the bookkeeping columns that
    // appear on every well-formed DbDuo schema (modeled on Pax.db and the
    // older AccAudit schema).
    //
    // These are not enforced -- a SQLite database without them still works
    // -- but when present, DbDuo treats them specially:
    //
    //   1. The Edit-Record dialog hides metadata fields by default and
    //      shows only 'distinct' (substantive) fields. Use the "more"
    //      checkbox to expose them.
    //
    //   2. New-Record skips them entirely; the database fills them in via
    //      DEFAULT current_timestamp on added/updated/observed and
    //      DEFAULT 0 on marked.
    //
    //   3. Show-Record groups them in a separate "metadata" section at
    //      the bottom of the field list, so screen readers reach them
    //      after the substantive content.
    //
    //   4. Set-Mark / Clear-Mark work against the 'marked' column and
    //      do nothing if the table lacks one.
    //
    //   5. The view_ prefix on a table name is the convention DbDuo uses
    //      to distinguish views from tables in the schema tree. ADOX
    //      Catalog.Tables enumeration returns both as the same kind of
    //      object; we filter on the Type property and on the prefix.
    //
    // Tables and field names are lower snake_case by user convention,
    // without Hungarian prefix -- the case where database identifiers do
    // NOT follow Camel Type because they are persisted, named once, and
    // memorized by anyone querying the database. Internal C# variables
    // that hold these strings still get Camel Type prefix (s for string).
    // =====================================================================
    public static class Metadata
    {
        // DbDuo's standard-field convention. Verified against
        // Pax.db's seven canonical tables (apps, screens, controls,
        // rules, issues, states, lookups) and against dbDot.vbs's
        // fillFieldArrays (line 318): the column names suppressed
        // from the default data-list view are exactly:
        //
        //   <table>_id, added, updated, marked, look, unq
        //
        // 'look' and 'unq' are stored-generated (PRAGMA table_xinfo
        // hidden=2); the calculated-column auto-hide rule would
        // catch them even without the by-name rule, but the by-name
        // rule is faster and works on providers that don't expose
        // generated-column metadata.
        //
        // 'marked' is hidden from the data list but surfaces in the
        // status bar only when the current row's value is true (see
        // updateStatusBar).
        //
        // 'observed', 'method', 'notes', 'tags' -- although standard
        // by convention -- are SHOWN. They carry user-visible content
        // (data-source timestamps, collection method, free-text
        // annotation, comma-separated keywords) and the user expects
        // them in the row scan.
        //
        // All distinct fields are shown unless the schema marks them
        // as calculated (stored or virtual generated), in which case
        // they join the hidden set automatically.
        public static readonly string[] StandardHiddenColumns = new string[]
        {
            "added",
            "updated",
            "marked",
            "look",
            "unq"
        };

        // No "force visible" overrides at present. The default is:
        // visible unless StandardHiddenColumns or the calculated-
        // column rule applies.
        public static readonly string[] StandardVisibleColumns = new string[] { };

        // Standard fields of any kind (hidden + conventional
        // visible). Used by Set-Record's "show more" toggle to
        // distinguish the conventional columns from a table's
        // distinct (substantive) columns.
        public static readonly string[] BookkeepingColumns = new string[]
        {
            "added",
            "updated",
            "observed",
            "method",
            "look",
            "unq",
            "marked",
            "notes",
            "tags"
        };

        public const string PrimaryKeySuffix = "_id";
        public const string MarkedColumn = "marked";
        public const string NotesColumn = "notes";

        // Standard date-sort column resolution. dbDot's standard for
        // "when did this row last change" is 'updated' (the
        // application-level "last edit" timestamp). 'observed' is
        // also a timestamp but records "when was this entity last
        // seen by the data-collection method" -- a different
        // semantic. 'added' is the row-creation timestamp; useful as
        // a fallback when 'updated' is absent.
        public static readonly string[] DateSortColumns = new string[]
        {
            "updated",
            "added",
            "observed"
        };

        // True if a column is hidden by the standard-fields rule
        // (not counting the calculated-column rule, which is checked
        // separately because it depends on schema metadata, not on
        // the column name).
        public static bool isStandardHidden(string sColumn, string sTable)
        {
            if (string.IsNullOrEmpty(sColumn)) return false;
            string sLower = sColumn.ToLowerInvariant();
            foreach (string sN in StandardHiddenColumns)
                if (sLower == sN) return true;
            // Key columns: any column ending in '_id' (primary or
            // foreign key by DbDuo convention) and bare 'id'. These
            // hold integer references that don't carry meaningful
            // display value -- the user navigates between related
            // rows via Show-Related (Control+J) rather than reading
            // raw key values. Hides:
            //   - <table>_id (primary key, e.g. app_id, rule_id)
            //   - other *_id columns (foreign keys, e.g. screen_id
            //     in the controls table, audit_id in issues)
            //   - the literal column 'id'
            if (sLower == "id") return true;
            if (sLower.EndsWith(PrimaryKeySuffix)) return true;
            return false;
        }

        // True if a column is part of the standard-visible set.
        // Such columns are shown even if they would otherwise be
        // filtered (they don't override calculated-column hiding,
        // however -- if 'notes' were somehow a computed column the
        // calculated rule wins).
        public static bool isStandardVisible(string sColumn)
        {
            if (string.IsNullOrEmpty(sColumn)) return false;
            string sLower = sColumn.ToLowerInvariant();
            foreach (string sN in StandardVisibleColumns)
                if (sLower == sN) return true;
            return false;
        }

        // Legacy: bookkeeping = standard-fields-of-any-kind. Kept
        // because the Edit-Record dialog uses this to decide which
        // fields to put behind the "more" checkbox.
        public static bool isBookkeepingColumn(string sColumn, string sTable)
        {
            if (string.IsNullOrEmpty(sColumn)) return false;
            string sLower = sColumn.ToLowerInvariant();
            foreach (string sBk in BookkeepingColumns)
                if (sLower == sBk) return true;
            if (sLower.EndsWith(PrimaryKeySuffix))
            {
                if (!string.IsNullOrEmpty(sTable))
                {
                    string sExpected = sTable.ToLowerInvariant() + PrimaryKeySuffix;
                    if (sLower == sExpected) return true;
                }
                return true;
            }
            return false;
        }

        // Convert a snake_case database column name to a Title Case
        // display label, mirroring the manual labels in DbDialog.cfg
        // (e.g. "First_Name" -> "First Name", "app_id" -> "App ID").
        //
        // Special cases:
        //   - Words ending the suffix '_id' become "ID" rather than "Id".
        //   - Two-letter all-lowercase words ('os', 'cd', 'pc', 'ui')
        //     are uppercased to convention.
        //   - Pure underscores become spaces; all other characters are
        //     left alone except for the leading-letter capitalization.
        //
        // Used by Show-Record and the Edit dialogs when displaying
        // labels for users. The user can override these by providing a
        // sidecar <basename>.ini file with [TableName] FieldLabels=...
        // (DbDialog.cfg style); for now, we always compute them.
        public static string fieldLabel(string sColumn)
        {
            if (string.IsNullOrEmpty(sColumn)) return "";
            string[] aParts = sColumn.Split('_');
            StringBuilder oOut = new StringBuilder();
            for (int i = 0; i < aParts.Length; i++)
            {
                if (i > 0) oOut.Append(' ');
                string sPart = aParts[i];
                if (string.IsNullOrEmpty(sPart)) continue;
                // Common abbreviations -> uppercase.
                string sLower = sPart.ToLowerInvariant();
                if (sLower == "id" || sLower == "os" || sLower == "url"
                    || sLower == "ui" || sLower == "pc" || sLower == "ip"
                    || sLower == "html" || sLower == "xml" || sLower == "json"
                    || sLower == "csv" || sLower == "sql" || sLower == "api")
                {
                    oOut.Append(sPart.ToUpperInvariant());
                }
                else
                {
                    oOut.Append(char.ToUpperInvariant(sPart[0]));
                    if (sPart.Length > 1)
                        oOut.Append(sPart.Substring(1).ToLowerInvariant());
                }
            }
            return oOut.ToString();
        }
    }

    // =====================================================================
    // DbDuoManager: the single source of truth for the current
    // recordset state (current position, filter, sort, bookmarks).
    //
    // Wraps a long-lived ADODB.Connection and a current ADODB.Recordset.
    // All COM calls are late-bound through dynamic, so no Interop.ADODB
    // assembly is required at compile time and the resulting DbDuo.exe
    // ships as a single self-contained binary.
    //
    // The recordset is opened CursorLocation = adUseClient and CursorType
    // = adOpenStatic, which guarantees uniform support for Bookmark,
    // Sort, and Filter across SQLite (via SQLite ODBC), Access, Excel,
    // dBASE, and CSV (the last four via the ACE OLE DB Provider).
    //
    // File-extension dispatch in openDatabase() picks the driver:
    //   .db, .sqlite, .sqlite3      -> SQLite ODBC Driver
    //   .mdb, .accdb                -> ACE OLEDB.16.0 (Access)
    //   .xlsx, .xls                 -> ACE OLEDB.16.0 with Excel mode
    //   .dbf or folder              -> ACE OLEDB.16.0 with dBASE IV mode
    //   .csv, .tsv, .tab, .txt      -> ACE OLEDB.16.0 with Text mode
    //
    // Synchronization design: bookmark-survives-sort. Before any
    // operation that re-shapes the recordset (filter change, sort
    // change, requery), we capture oRecordset.Bookmark. After, we
    // assign it back. ADODB's bookmark is opaque but stable across
    // sort and filter -- if the bookmarked row survived, the cursor
    // returns to it; if not, we fall to the first row. This is the
    // pattern dbDot.vbs uses and is the central reason we picked
    // ADODB over rolling our own cursor on top of System.Data.SQLite.
    // =====================================================================
    public class DbDuoManager : IDisposable
    {
        // ------- Constants -------
        private const int InitialFetchBatch = 1000;  // rows fetched per chunk for table loads
        private const string DefaultProvider = "Microsoft.ACE.OLEDB.16.0";

        // ------- Fields (alphabetical within each type) -------
        private bool bDisposed;
        private bool bReadOnly;

        private dynamic oCatalog;     // ADOX.Catalog (lazy; only when getTableNames is called)
        private dynamic oConn;        // ADODB.Connection
        private dynamic oRecordset;   // ADODB.Recordset (current table)

        private string sConnectString;
        private string sCurrentTable;
        private string sFilePath;

        // ------- Constructor / Dispose -------
        public DbDuoManager()
        {
            sCurrentTable = "";
            sFilePath = "";
            sConnectString = "";
            bReadOnly = false;
        }

        public void Dispose()
        {
            if (bDisposed) return;
            try { closeRecordset(); } catch { }
            try { if (oConn != null) oConn.Close(); } catch { }
            releaseCom(oRecordset); oRecordset = null;
            releaseCom(oCatalog); oCatalog = null;
            releaseCom(oConn); oConn = null;
            bDisposed = true;
        }

        // ------- Public read-only properties -------
        public string filePath { get { return sFilePath; } }
        public string currentTable { get { return sCurrentTable; } }
        public bool readOnly { get { return bReadOnly; } }
        public string connectString { get { return sConnectString; } }

        // ------- isOpen / connection state -------
        public bool isOpen()
        {
            if (oConn == null) return false;
            try { return ((int)oConn.State) != 0; }
            catch { return false; }
        }

        public bool hasRecordset()
        {
            if (oRecordset == null) return false;
            try { return ((int)oRecordset.State) != 0; }
            catch { return false; }
        }

        // =====================================================================
        // openDatabase: file-extension dispatch to the right provider.
        //
        // sPath may be a file or a folder. A folder is treated as a
        // dBASE source (dbDot's convention). A relative path is
        // resolved against the current working directory, matching
        // dbDot's "if (sPath does not contain backslash) prepend cur dir."
        //
        // sTable is optional. If supplied, the recordset is opened on
        // that table after the connection is established. For .csv
        // and .dbf single-file paths the file's basename is used as
        // the table by default.
        // =====================================================================
        public void openDatabase(string sPath, string sTable, bool bReadOnlyFlag)
        {
            if (isOpen()) close();

            if (string.IsNullOrEmpty(sPath))
                throw new ArgumentException("openDatabase requires a path.");

            // Resolve relative path. If the input has no directory
            // separator, dbDot's pattern is to prepend the current
            // working directory. We mirror that.
            if (!Path.IsPathRooted(sPath))
                sPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, sPath));

            string sExt;
            bool bIsFolder = Directory.Exists(sPath);
            if (bIsFolder) sExt = "dbf";  // a folder is treated as a dBASE source
            else
            {
                if (!File.Exists(sPath))
                    throw new FileNotFoundException("File not found: " + sPath, sPath);
                sExt = Path.GetExtension(sPath).TrimStart('.').ToLowerInvariant();
            }

            sConnectString = buildConnectString(sPath, sExt, bIsFolder, ref sTable);
            sFilePath = sPath;
            bReadOnly = bReadOnlyFlag;

            oConn = createComObject("ADODB.Connection");
            try
            {
                oConn.CursorLocation = AdoConstants.adUseClient;
                if (bReadOnlyFlag) oConn.Mode = AdoConstants.adModeRead;
                oConn.Open(sConnectString);
            }
            catch (COMException oEx)
            {
                throw new Exception(translateConnectError(oEx, sExt), oEx);
            }

            if (!string.IsNullOrEmpty(sTable))
                selectTable(sTable);
        }

        // =====================================================================
        // buildConnectString: per-extension connection-string assembly.
        // Mirrors dbDot.vbs lines 869-914 exactly. The CSV and dBASE
        // single-file cases parse the filename out as the implicit
        // table name and use the parent folder as the data source --
        // ACE Text mode and dBASE mode both treat the data source as
        // a *folder* containing many tables.
        // =====================================================================
        private string buildConnectString(string sPath, string sExt, bool bIsFolder, ref string sTable)
        {
            switch (sExt)
            {
                case "db":
                case "sqlite":
                case "sqlite3":
                    return string.Format("DRIVER=SQLite3 ODBC Driver;Database={0};", sPath);

                case "mdb":
                case "accdb":
                    return string.Format(
                        "Provider={0};Data Source={1};Persist Security Info=False;",
                        DefaultProvider, sPath);

                case "xlsx":
                case "xls":
                case "xlsm":
                case "xlsb":
                    return string.Format(
                        "Provider={0};Data Source={1};Extended Properties=\"Excel 12.0 Xml;HDR=Yes;IMEX=1;\";",
                        DefaultProvider, sPath);

                case "dbf":
                {
                    string sFolder = bIsFolder ? sPath : Path.GetDirectoryName(sPath);
                    if (!bIsFolder && string.IsNullOrEmpty(sTable))
                        sTable = Path.GetFileNameWithoutExtension(sPath);
                    return string.Format(
                        "Provider={0};Data Source={1};Extended Properties=dBASE IV;",
                        DefaultProvider, sFolder);
                }

                case "csv":
                case "tsv":
                case "tab":
                case "txt":
                {
                    string sFolder = Path.GetDirectoryName(sPath);
                    if (string.IsNullOrEmpty(sTable))
                        sTable = Path.GetFileName(sPath);
                    string sFmt = (sExt == "tsv" || sExt == "tab") ? "TabDelimited" : "Delimited";
                    return string.Format(
                        "Provider={0};Data Source={1};Extended Properties=\"Text;HDR=Yes;FMT={2};\";",
                        DefaultProvider, sFolder, sFmt);
                }

                default:
                    throw new Exception("Unrecognized database file extension: ." + sExt
                        + ". Supported: .db, .sqlite, .sqlite3, .mdb, .accdb, .xlsx, .xls, .dbf, .csv, .tsv, .txt, or a folder.");
            }
        }

        // =====================================================================
        // translateConnectError: turns the typical COM error into
        // the actionable message about driver installation. The
        // SQLite ODBC and ACE providers each have a distinctive
        // failure mode when not installed.
        // =====================================================================
        private string translateConnectError(COMException oEx, string sExt)
        {
            string sMsg = oEx.Message ?? "";
            string sLower = sMsg.ToLowerInvariant();

            if (sLower.Contains("data source name not found") || sLower.Contains("im002"))
            {
                if (sExt == "db" || sExt == "sqlite" || sExt == "sqlite3")
                    return "SQLite ODBC driver not installed. Install sqliteodbc_w64.exe from "
                         + "http://www.ch-werner.de/sqliteodbc/ then retry. "
                         + "(No WinGet or Chocolatey package exists for this driver.) "
                         + "Original error: " + sMsg;
            }
            if (sLower.Contains("not registered") || sLower.Contains("provider cannot be found"))
            {
                return "Microsoft Access Database Engine 2016 Redistributable not installed. "
                     + "Install via WinGet: 'winget install Microsoft.AccessDatabaseEngine.2016 --silent', "
                     + "via Chocolatey: 'choco install made-2016 -y', "
                     + "or download accessdatabaseengine_X64.exe from "
                     + "https://www.microsoft.com/en-us/download/details.aspx?id=54920 and run it with the /passive flag. "
                     + "Original error: " + sMsg;
            }
            return sMsg;
        }

        // ------- close -------
        public void close()
        {
            try { closeRecordset(); } catch { }
            try { if (oConn != null && ((int)oConn.State) != 0) oConn.Close(); } catch { }
            releaseCom(oRecordset); oRecordset = null;
            releaseCom(oCatalog); oCatalog = null;
            releaseCom(oConn); oConn = null;
            sCurrentTable = "";
            sFilePath = "";
            sConnectString = "";
            bReadOnly = false;
            clearTableCache();
        }

        private void closeRecordset()
        {
            if (oRecordset != null)
            {
                try { if (((int)oRecordset.State) != 0) oRecordset.Close(); } catch { }
            }
            releaseCom(oRecordset); oRecordset = null;
        }

        // =====================================================================
        // selectTable: opens (or re-opens) the recordset on the named
        // table or view. Closes any prior recordset. Sets CursorType
        // and LockType appropriate for whether we're read-only AND for
        // whether the named object is a view (always read-only).
        //
        // The ADODB.Recordset.Open overload takes a Source, ActiveConn,
        // CursorType, LockType, Options. We use adCmdTable (2) so the
        // Source argument is treated as a literal table or view name
        // rather than an SQL string. Most providers happily accept a
        // view name through adCmdTable.
        // =====================================================================
        // =====================================================================
        // Session table cache: per-database, the manager remembers
        // every table or view the user has opened in this session and
        // each table's filter / sort / current-row position. When the
        // user switches back to a previously-visited table, those
        // settings are restored automatically. The cache lives only as
        // long as the database connection; closing the database
        // discards it.
        //
        // Why we cache *settings* and not the live Recordset object:
        //   - A live Recordset holds row data that can go stale when
        //     other writers (or the same writer through a different
        //     statement) modify the underlying table. Re-opening the
        //     recordset on each return guarantees fresh data.
        //   - ADO Recordsets in adUseClient mode hold the rowset in
        //     memory; keeping multiple open simultaneously balloons
        //     memory for tables of any size.
        //   - The settings are cheap to capture (three values: filter,
        //     sort, absolute position) and applying them to a freshly-
        //     opened recordset takes microseconds.
        //
        // Order: lVisitedTables is the insertion order of unique table
        // names visited. Used by Control+Tab cycling. selectTable
        // appends to it on first visit; subsequent visits leave the
        // ordering untouched.
        // =====================================================================
        public class TableSettings
        {
            public string sFilter;       // Recordset.Filter string ("" for none)
            public string sSort;         // Recordset.Sort string ("" for none)
            public int iAbsolutePosition;// 1-based row number (0 if no rows)
            // Comma-separated select list -- the explicit set of
            // columns the user wants visible in the ListView for
            // this table. Empty string means "use the default
            // rule-based selection (standard-hidden + calculated
            // hidden, everything else shown)."
            public string sSelectList = "";
        }
        private Dictionary<string, TableSettings> dTableCache = new Dictionary<string, TableSettings>(StringComparer.OrdinalIgnoreCase);
        private List<string> lVisitedTables = new List<string>();

        // Cache of table-name -> isView, populated when
        // getCatalogObjectNames walks the ADOX catalog. The TYPE
        // column from the catalog is the authoritative source for
        // "view vs base table"; we cache the answer here so
        // selectTable can consult it without re-walking the catalog
        // on every table switch. Cleared by close().
        private Dictionary<string, bool> dIsViewByName = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        // Cache of (table, column) -> isCalculated. Populated by
        // selectTable when a new table is opened. SQLite reports
        // calculated columns through PRAGMA table_xinfo's 'hidden'
        // column (values 2 = stored-generated, 3 = virtual-
        // generated). Other providers (ACE/Jet) do not reliably
        // expose this through ADO, so for those tables the cache
        // contains only Metadata's standard 'look' entry.
        //
        // Keys are formatted as "<table>::<column>" (case-
        // insensitive). The cache survives across selectTable
        // calls within a session; close() clears it.
        private Dictionary<string, bool> dCalculatedColumns = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public bool isCalculatedColumn(string sTable, string sColumn)
        {
            if (string.IsNullOrEmpty(sColumn)) return false;
            string sKey = (sTable ?? "") + "::" + sColumn;
            bool bResult;
            return dCalculatedColumns.TryGetValue(sKey, out bResult) && bResult;
        }

        // True if the name is in our schema cache and recorded as a
        // view. False otherwise (either it's a known table OR it's
        // not in the cache at all -- caller will open optimistic and
        // the provider will reject the lock if the name is wrong).
        public bool isKnownView(string sName)
        {
            if (string.IsNullOrEmpty(sName)) return false;
            bool bResult;
            return dIsViewByName.TryGetValue(sName, out bResult) && bResult;
        }

        // Snapshot the settings of the currently-open table, if any.
        // Called by selectTable just before switching away.
        private void cacheCurrentTableSettings()
        {
            if (string.IsNullOrEmpty(sCurrentTable)) return;
            if (oRecordset == null) return;
            TableSettings oSnap = new TableSettings();
            oSnap.sFilter = filter ?? "";
            oSnap.sSort   = sort ?? "";
            oSnap.iAbsolutePosition = absolutePosition;
            dTableCache[sCurrentTable] = oSnap;
        }

        // Apply a previously-captured snapshot to the just-opened
        // recordset. Called by selectTable after Open() succeeds.
        // Filter and sort are best-effort: if the cached expression no
        // longer parses (e.g., a referenced column was dropped), we
        // ignore the failure rather than fail the table switch.
        private void applyTableSettings(string sTable)
        {
            if (oRecordset == null) return;
            if (!dTableCache.ContainsKey(sTable)) return;
            TableSettings oSnap = dTableCache[sTable];
            if (!string.IsNullOrEmpty(oSnap.sFilter))
            {
                try { filter = oSnap.sFilter; } catch { /* stale filter; skip */ }
            }
            if (!string.IsNullOrEmpty(oSnap.sSort))
            {
                try { sort = oSnap.sSort; } catch { /* stale sort; skip */ }
            }
            if (oSnap.iAbsolutePosition > 0)
            {
                try { absolutePosition = oSnap.iAbsolutePosition; } catch { /* row may not exist anymore */ }
            }
        }

        // Public list of tables visited in this session, in insertion
        // order. Used by the Control+Tab cycle in DbDuoForm.
        public List<string> visitedTableNames()
        {
            return new List<string>(lVisitedTables);
        }

        // Drop the cache. Called by close() so a fresh database starts
        // with no stale entries.
        private void clearTableCache()
        {
            dTableCache.Clear();
            lVisitedTables.Clear();
            dIsViewByName.Clear();
        }

        public void selectTable(string sTable)
        {
            if (!isOpen()) throw new InvalidOperationException("No database open.");
            if (string.IsNullOrEmpty(sTable))
                throw new ArgumentException("selectTable requires a table name.");

            // Capture settings of the table we're leaving so a later
            // return restores them.
            cacheCurrentTableSettings();

            closeRecordset();

            oRecordset = createComObject("ADODB.Recordset");
            oRecordset.CursorLocation = AdoConstants.adUseClient;

            // Views are always read-only regardless of the manager's
            // bReadOnly flag, since SQLite views are not generally
            // updatable through ADO and ACE views never are.
            //
            // Detection: prefer the cached schema type populated by
            // getCatalogObjectNames -- that comes from ADOX's TYPE
            // column, the authoritative source. Fall back to opening
            // optimistic and letting the provider reject the lock if
            // the name is unknown to us (recently-created view that
            // we haven't catalogued yet, or a typed-in name from the
            // dot prompt).
            bool bIsView = isKnownView(sTable);
            int iLock;
            if (bReadOnly || bIsView)
                iLock = AdoConstants.adLockReadOnly;
            else
                iLock = AdoConstants.adLockOptimistic;

            try
            {
                oRecordset.Open(sTable, oConn,
                    AdoConstants.adOpenStatic, iLock, AdoConstants.adCmdTable);
            }
            catch (COMException oEx)
            {
                releaseCom(oRecordset); oRecordset = null;
                throw new Exception("Cannot open " + (bIsView ? "view" : "table")
                    + " '" + sTable + "': " + oEx.Message, oEx);
            }

            sCurrentTable = sTable;
            bCurrentIsView = bIsView;

            // Track session visit history. First visit appends to the
            // ordered list; revisits leave the order alone so cycling
            // remains predictable. Then restore any cached settings.
            bool bAlreadyVisited = false;
            foreach (string sN in lVisitedTables)
            {
                if (string.Equals(sN, sTable, StringComparison.OrdinalIgnoreCase))
                { bAlreadyVisited = true; break; }
            }
            if (!bAlreadyVisited) lVisitedTables.Add(sTable);
            applyTableSettings(sTable);

            // Populate the calculated-columns cache for this table
            // by running PRAGMA table_xinfo. SQLite returns a row
            // per column with a 'hidden' integer:
            //   0 = normal, 1 = hidden via constraint, 2 = stored
            //   generated, 3 = virtual generated. Values 2 and 3
            //   are calculated columns; we mark them hidden in the
            //   DbDuo display.
            //
            // For non-SQLite databases (ACE, Excel, dBase), the
            // PRAGMA call fails silently; the only calculated
            // column those tables will hide is 'look', which is
            // hardcoded in Metadata.
            populateCalculatedColumnsCache(sTable);
        }

        private void populateCalculatedColumnsCache(string sTable)
        {
            if (oConn == null) return;
            if (string.IsNullOrEmpty(sTable)) return;
            try
            {
                string sSql = "PRAGMA table_xinfo(\"" + sTable.Replace("\"", "\"\"") + "\")";
                dynamic oRs = createComObject("ADODB.Recordset");
                try
                {
                    oRs.Open(sSql, oConn, AdoConstants.adOpenStatic,
                        AdoConstants.adLockReadOnly, AdoConstants.adCmdText);
                    while (!(bool)oRs.EOF)
                    {
                        string sColName = "";
                        int iHidden = 0;
                        try { sColName = (string)oRs.Fields["name"].Value; } catch { }
                        try { iHidden = Convert.ToInt32(oRs.Fields["hidden"].Value); } catch { }
                        if (!string.IsNullOrEmpty(sColName))
                        {
                            // hidden values 2 and 3 are stored/virtual generated.
                            bool bCalc = (iHidden == 2 || iHidden == 3);
                            dCalculatedColumns[sTable + "::" + sColName] = bCalc;
                        }
                        oRs.MoveNext();
                    }
                }
                finally
                {
                    try { oRs.Close(); } catch { }
                    releaseCom(oRs);
                }
            }
            catch
            {
                // Non-SQLite or PRAGMA disallowed -- ignore. The
                // hardcoded 'look' rule in Metadata still works.
            }
        }

        // True if the currently-open object is a view (read-only).
        // Set by selectTable based on the name prefix.
        public bool currentIsView { get { return bCurrentIsView; } }
        private bool bCurrentIsView = false;

        // =====================================================================
        // getTableNames: ADOX.Catalog walks the connection's tables.
        // We filter out system / view / link types since users want
        // user data tables.
        //
        // ADOX is a separate COM library from ADODB; the Catalog
        // object connects to the same ADODB.Connection via its
        // ActiveConnection property.
        // =====================================================================
        public List<string> getTableNames()
        {
            return getCatalogObjectNames(true, false);
        }

        public List<string> getViewNames()
        {
            return getCatalogObjectNames(false, true);
        }

        public List<string> getTableAndViewNames()
        {
            return getCatalogObjectNames(true, true);
        }

        // Walk ADOX Catalog.Tables once and apply the appropriate filter.
        //
        // The TYPE column from ADOX is authoritative for distinguishing
        // a base table from a view. SQLite ODBC, ACE OLE DB, Jet,
        // dBASE, and all major ADODB-compatible providers populate
        // TYPE as "TABLE" or "VIEW" for the two cases. We rely on
        // this exclusively; the older view_ name-prefix convention
        // is no longer consulted.
        //
        // As a side effect we populate dIsViewByName so selectTable
        // can answer "is this name a view" without re-walking the
        // catalog. The cache is cleared on close().
        //
        // Tables that begin with "sqlite_" (sqlite_master, sqlite_sequence,
        // sqlite_stat1) are SQLite internals and excluded.
        //
        // Tables that begin with "MSys" are ACE/Jet system catalog and
        // excluded.
        private List<string> getCatalogObjectNames(bool bIncludeTables, bool bIncludeViews)
        {
            List<string> lResult = new List<string>();
            if (!isOpen()) return lResult;

            if (oCatalog == null)
                oCatalog = createComObject("ADOX.Catalog");

            try { oCatalog.ActiveConnection = oConn; }
            catch { /* ignore; some providers don't support ADOX */ }

            try
            {
                dynamic oTables = oCatalog.Tables;
                int iCount = (int)oTables.Count;
                for (int i = 0; i < iCount; i++)
                {
                    dynamic oTable = oTables[i];
                    string sName = (string)oTable.Name;
                    string sType = "";
                    try { sType = (string)oTable.Type; } catch { }

                    // Skip system catalog tables.
                    string sLowerName = sName.ToLowerInvariant();
                    if (sLowerName.StartsWith("sqlite_")) continue;
                    if (sName.StartsWith("MSys", StringComparison.OrdinalIgnoreCase)) continue;
                    if (sType == "ACCESS TABLE") continue;
                    if (sType == "SYSTEM TABLE") continue;

                    bool bIsView = (sType == "VIEW");
                    bool bIsTable = !bIsView
                        && (sType == "TABLE" || sType == "PASS-THROUGH" || sType == "");

                    // Populate the type cache for selectTable's use.
                    dIsViewByName[sName] = bIsView;

                    if (bIsView && bIncludeViews) lResult.Add(sName);
                    else if (bIsTable && bIncludeTables) lResult.Add(sName);
                }
            }
            catch
            {
                // Provider without ADOX; caller will fall back to direct
                // SQL against sqlite_master / INFORMATION_SCHEMA.
            }
            lResult.Sort(StringComparer.OrdinalIgnoreCase);
            return lResult;
        }

        // True if the given name refers to a view rather than a base
        // table. Consults the schema-type cache populated by
        // getCatalogObjectNames. Returns false for unknown names.
        public bool isViewName(string sName)
        {
            return isKnownView(sName);
        }

        // getColumnsOfTable: list the column names of a named table
        // WITHOUT switching the current recordset to it. Used by the
        // Enter-Child drill-down to scan candidate child tables for a
        // foreign-key column matching the parent's primary key.
        //
        // Walks ADOX.Catalog.Tables[sTable].Columns. If ADOX is
        // unavailable on the current provider, falls back to opening
        // a minimal "SELECT * FROM table WHERE 1=0" recordset to
        // read the Fields collection without fetching any rows.
        // Both paths are read-only and don't disturb the current
        // recordset's filter/sort/position.
        public List<string> getColumnsOfTable(string sTable)
        {
            List<string> lResult = new List<string>();
            if (!isOpen() || string.IsNullOrEmpty(sTable)) return lResult;

            // First try ADOX. Cheap when it works.
            try
            {
                if (oCatalog == null)
                    oCatalog = createComObject("ADOX.Catalog");
                try { oCatalog.ActiveConnection = oConn; } catch { }
                dynamic oTable = oCatalog.Tables[sTable];
                dynamic oCols = oTable.Columns;
                int iCount = (int)oCols.Count;
                for (int i = 0; i < iCount; i++)
                {
                    dynamic oCol = oCols[i];
                    lResult.Add((string)oCol.Name);
                }
                if (lResult.Count > 0) return lResult;
            }
            catch
            {
                // Provider without ADOX (or table not in catalog).
                // Fall through to the empty-recordset path.
            }

            // Fallback: open a no-row recordset to read field metadata.
            // Quoting matches the rest of DbDuo's SQL emission.
            try
            {
                string sQuoted = "[" + sTable.Replace("]", "]]") + "]";
                dynamic oRs = createComObject("ADODB.Recordset");
                oRs.Open("SELECT * FROM " + sQuoted + " WHERE 1=0", oConn,
                    1 /* adOpenKeyset */, 1 /* adLockReadOnly */, 1 /* adCmdText */);
                dynamic oFields = oRs.Fields;
                int iCount = (int)oFields.Count;
                for (int i = 0; i < iCount; i++)
                    lResult.Add((string)oFields[i].Name);
                try { oRs.Close(); } catch { }
                releaseCom(oRs);
            }
            catch { }
            return lResult;
        }

        // actualPrimaryKey: look up the actual primary-key column of
        // a named table from the database's own schema metadata,
        // rather than guessing from the table-name singularization.
        //
        // The schema-truth approach matters because English-plural
        // rules are irregular: a table named "classes" has PK
        // "class_id" (not "classe_id" or "classes_id"); a table
        // named "cities" has PK "city_id" (not "citie_id"); a
        // table named "teachers" happens to work with naive
        // s-stripping. Rather than enumerate plural rules, we
        // read the real metadata.
        //
        // Strategy:
        //   1. SQLite -- query "PRAGMA table_info(<table>)" through
        //      ADO and look for the row with pk > 0. This is the
        //      authoritative source for SQLite.
        //   2. ADOX fallback -- for Access and other providers
        //      that expose ADOX.Catalog, ask for the table's
        //      primary-key Keys collection and read the first
        //      column from it.
        //
        // Returns the actual column name (preserving case) or null
        // if no PK can be determined. For composite primary keys
        // returns the FIRST column of the key.
        public string actualPrimaryKey(string sTable)
        {
            if (!isOpen() || string.IsNullOrEmpty(sTable)) return null;

            // SQLite path. PRAGMA returns one row per column; the
            // 'pk' column is 0 for non-PK columns and a 1-based
            // ordinal for PK columns. We want pk = 1 (or, for
            // composite keys, the column with the smallest pk).
            string sExt = currentExtensionForSql();
            if (sExt == "db" || sExt == "sqlite" || sExt == "sqlite3")
            {
                string sQuoted = "\"" + sTable.Replace("\"", "\"\"") + "\"";
                dynamic oRs = null;
                try
                {
                    oRs = createComObject("ADODB.Recordset");
                    oRs.Open("PRAGMA table_info(" + sQuoted + ")", oConn,
                        0 /* adOpenForwardOnly */, 1 /* adLockReadOnly */, 1 /* adCmdText */);
                    string sFirstPk = null;
                    int iFirstPkOrdinal = int.MaxValue;
                    while (!(bool)oRs.EOF)
                    {
                        try
                        {
                            // Fields: cid, name, type, notnull, dflt_value, pk
                            int iPk = 0;
                            try { iPk = Convert.ToInt32(oRs.Fields["pk"].Value); } catch { iPk = 0; }
                            if (iPk > 0 && iPk < iFirstPkOrdinal)
                            {
                                object oN = oRs.Fields["name"].Value;
                                if (oN != null && oN != DBNull.Value)
                                {
                                    sFirstPk = oN.ToString();
                                    iFirstPkOrdinal = iPk;
                                }
                            }
                        }
                        catch { }
                        oRs.MoveNext();
                    }
                    try { oRs.Close(); } catch { }
                    if (!string.IsNullOrEmpty(sFirstPk)) return sFirstPk;
                }
                catch (Exception oEx)
                {
                    try { DbDuoLog.write("actualPrimaryKey PRAGMA failed: " + oEx.Message); } catch { }
                }
                finally { releaseCom(oRs); }
            }

            // ADOX path: applies to Access and other ADOX-friendly
            // providers, and is also a fallback when PRAGMA fails.
            try
            {
                if (oCatalog == null)
                    oCatalog = createComObject("ADOX.Catalog");
                try { oCatalog.ActiveConnection = oConn; } catch { }
                dynamic oTable = oCatalog.Tables[sTable];
                dynamic oKeys = oTable.Keys;
                int iKeyCount = (int)oKeys.Count;
                const int iAdKeyPrimary = 1;
                for (int i = 0; i < iKeyCount; i++)
                {
                    dynamic oKey = oKeys[i];
                    int iType = 0;
                    try { iType = Convert.ToInt32(oKey.Type); } catch { iType = 0; }
                    if (iType != iAdKeyPrimary) continue;
                    dynamic oCols = oKey.Columns;
                    if ((int)oCols.Count == 0) continue;
                    return (string)oCols[0].Name;
                }
            }
            catch { }

            return null;
        }

        // queryColumnValues: run a SELECT on a side connection to
        // pull values from one column of one table, with an optional
        // WHERE clause. Returns up to iMaxRows results as strings.
        // Used by Show-Object's related-records section to fetch
        // the 'look' values of child rows efficiently, without
        // touching the user's current recordset.
        //
        // We deliberately issue a parametric SQL query rather than
        // re-using the ADO client-side .Filter mechanism: the
        // server-side WHERE clause is O(matching rows) and lets
        // SQLite use any indexes on the FK column, while .Filter
        // is O(total rows in child table) because it post-filters
        // a fully-loaded client-cursor recordset. For Show-Object's
        // hot-path display this is the right choice.
        //
        // Returns iCountFound by reference so the caller can
        // distinguish "20 of 25 rows shown" from "exactly 20 rows
        // exist". Set iMaxRows to a large value (or int.MaxValue)
        // to get all rows.
        //
        // sTable and sColumn are quoted as identifiers per the
        // back-end's convention; sWhereExpr and sWhereValue go
        // through quoteSqlLiteral if you supply them, so caller
        // doesn't have to worry about SQL injection from FK values.
        public List<string> queryColumnValues(
            string sTable, string sColumn,
            string sWhereColumn, string sWhereValue,
            int iMaxRows, out int iCountFound)
        {
            iCountFound = 0;
            List<string> lValues = new List<string>();
            if (!isOpen() || string.IsNullOrEmpty(sTable) || string.IsNullOrEmpty(sColumn))
                return lValues;

            // Build the SQL. Use double-quoted identifiers, which
            // SQLite (default), Access (also accepts), and ANSI
            // SQL all support. Single-quote string literals with
            // apostrophe doubling.
            string sExt = currentExtensionForSql();
            StringBuilder oSb = new StringBuilder();
            oSb.Append("SELECT ");
            oSb.Append(quoteIdentifier(sColumn, sExt));
            oSb.Append(" FROM ");
            oSb.Append(quoteIdentifier(sTable, sExt));
            if (!string.IsNullOrEmpty(sWhereColumn))
            {
                oSb.Append(" WHERE ");
                oSb.Append(quoteIdentifier(sWhereColumn, sExt));
                if (string.IsNullOrEmpty(sWhereValue))
                {
                    oSb.Append(" IS NULL");
                }
                else
                {
                    // If the value parses as a number, leave bare;
                    // otherwise single-quote with apostrophe doubling.
                    double dN;
                    if (double.TryParse(sWhereValue,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out dN))
                    { oSb.Append(" = "); oSb.Append(sWhereValue); }
                    else
                    { oSb.Append(" = '"); oSb.Append(sWhereValue.Replace("'", "''")); oSb.Append("'"); }
                }
            }

            dynamic oRs = null;
            try
            {
                oRs = createComObject("ADODB.Recordset");
                // Server-side cursor for this fetch: we only need
                // forward-only reads and we don't want to disturb
                // the manager's client-side cursor settings.
                oRs.Open(oSb.ToString(), oConn,
                    0 /* adOpenForwardOnly */,
                    1 /* adLockReadOnly */,
                    1 /* adCmdText */);
                while (!(bool)oRs.EOF)
                {
                    iCountFound++;
                    if (lValues.Count < iMaxRows)
                    {
                        object oV;
                        try { oV = oRs.Fields[0].Value; } catch { oV = null; }
                        lValues.Add((oV == null || oV == DBNull.Value) ? "" : oV.ToString());
                    }
                    oRs.MoveNext();
                }
                try { oRs.Close(); } catch { }
            }
            catch (Exception oEx)
            {
                // Caller may want to know the query failed (e.g.,
                // 'look' column doesn't exist on this table). The
                // out param iCountFound stays at 0 and the list
                // stays empty; log for diagnostics.
                try { DbDuoLog.write("queryColumnValues failed: " + oEx.Message + " SQL=" + oSb.ToString()); }
                catch { }
            }
            finally
            {
                releaseCom(oRs);
            }
            return lValues;
        }

        // Current connection's extension key, used by
        // queryColumnValues to pick identifier-quoting style.
        // Falls back to "db" (SQLite) when nothing else fits.
        private string currentExtensionForSql()
        {
            if (string.IsNullOrEmpty(sFilePath)) return "db";
            string sExt = Path.GetExtension(sFilePath).TrimStart('.').ToLowerInvariant();
            switch (sExt)
            {
                case "db": case "sqlite": case "sqlite3": return "db";
                case "mdb": case "accdb": return "accdb";
                case "dbf": return "dbf";
                default: return "db";
            }
        }
        public bool eof
        {
            get
            {
                if (!hasRecordset()) return true;
                try { return (bool)oRecordset.EOF; } catch { return true; }
            }
        }

        public bool bof
        {
            get
            {
                if (!hasRecordset()) return true;
                try { return (bool)oRecordset.BOF; } catch { return true; }
            }
        }

        public int recordCount
        {
            get
            {
                if (!hasRecordset()) return 0;
                try { return (int)oRecordset.RecordCount; } catch { return -1; }
            }
        }

        // =====================================================================
        // absolutePosition: 1-based dBase-tradition position. Matches
        // dbDot's convention exactly. ADODB also uses 1-based positions
        // (with -1, -2, -3 sentinels for unknown / BOF / EOF).
        //
        // The setter is the canonical "go to row N" operation.
        // =====================================================================
        public int absolutePosition
        {
            get
            {
                if (!hasRecordset()) return 0;
                try
                {
                    int i = (int)oRecordset.AbsolutePosition;
                    return i < 0 ? 0 : i;  // hide BOF/EOF/unknown sentinels from callers
                }
                catch { return 0; }
            }
            set
            {
                if (!hasRecordset()) return;
                if (recordCount == 0) return;
                int i = value;
                if (i < 1) i = 1;
                if (i > recordCount) i = recordCount;
                try { oRecordset.AbsolutePosition = i; } catch { }
            }
        }

        // =====================================================================
        // bookmark: opaque handle to the current row's identity.
        // Survives sort and filter changes (when the row remains in
        // the filtered set). The single most important property in
        // the synchronization design -- both the GUI's grid refresh
        // and the CLI's navigation can capture-then-restore around
        // any state change.
        //
        // Caller holds the value as an `object` and treats it as
        // opaque; the getter and setter just round-trip it through COM.
        // =====================================================================
        public object bookmark
        {
            get
            {
                if (!hasRecordset()) return null;
                try
                {
                    if (eof || bof) return null;
                    return oRecordset.Bookmark;
                }
                catch { return null; }
            }
            set
            {
                if (!hasRecordset() || value == null) return;
                try { oRecordset.Bookmark = value; }
                catch
                {
                    // Bookmark no longer valid (record removed by
                    // filter or deletion). Fall to first row.
                    try { oRecordset.MoveFirst(); } catch { }
                }
            }
        }

        // =====================================================================
        // filter / sort: server-side recordset operations. Both
        // capture the bookmark before the change and try to restore
        // it after, so the user's logical "current record" follows
        // itself across sort and filter changes when possible.
        // =====================================================================
        public string filter
        {
            get
            {
                if (!hasRecordset()) return "";
                try { return (string)(oRecordset.Filter ?? ""); }
                catch { return ""; }
            }
            set
            {
                if (!hasRecordset()) return;
                object oBookmark = bookmark;
                try { oRecordset.Filter = value ?? ""; }
                catch (COMException oEx)
                {
                    throw new Exception("Filter rejected: " + oEx.Message, oEx);
                }
                if (oBookmark != null) bookmark = oBookmark;
            }
        }

        public string sort
        {
            get
            {
                if (!hasRecordset()) return "";
                try { return (string)(oRecordset.Sort ?? ""); }
                catch { return ""; }
            }
            set
            {
                if (!hasRecordset()) return;
                object oBookmark = bookmark;
                try { oRecordset.Sort = value ?? ""; }
                catch (COMException oEx)
                {
                    throw new Exception("Sort rejected: " + oEx.Message, oEx);
                }
                if (oBookmark != null) bookmark = oBookmark;
            }
        }

        public void resetFilter() { filter = ""; }
        public void resetSort() { sort = ""; }

        // applyFilter: convenience alias used by Show-Related, identical
        // to setting the filter property.
        public void applyFilter(string sCriteria)
        {
            filter = sCriteria;
        }

        // ------- Field-type introspection -------
        // These wrap ADO Field.Type, Field.DefinedSize, and translate
        // the integer DataTypeEnum to a human-readable name. Used by
        // Get-Property to render the schema details dialog.
        public int getFieldType(string sName)
        {
            if (!hasRecordset()) return 0;
            try { return (int)oRecordset.Fields[sName].Type; }
            catch { return 0; }
        }

        public int getFieldDefinedSize(string sName)
        {
            if (!hasRecordset()) return 0;
            try { return (int)oRecordset.Fields[sName].DefinedSize; }
            catch { return 0; }
        }

        public string getFieldTypeName(string sName)
        {
            int iType = getFieldType(sName);
            switch (iType)
            {
                case   2: return "smallint";
                case   3: return "integer";
                case   4: return "real";
                case   5: return "double";
                case   6: return "currency";
                case   7: return "date";
                case  11: return "boolean";
                case  17: return "tinyint";
                case  18: return "unsignedsmallint";
                case  19: return "unsignedint";
                case  20: return "bigint";
                case  72: return "guid";
                case 128: return "binary";
                case 130: return "wchar";
                case 131: return "decimal";
                case 133: return "dbdate";
                case 134: return "dbtime";
                case 135: return "dbtimestamp";
                case 200: return "varchar";
                case 201: return "longvarchar";
                case 202: return "varwchar";
                case 203: return "longvarwchar";
                case 204: return "varbinary";
                case 205: return "longvarbinary";
                default:  return "type" + iType;
            }
        }

        // ------- Navigation -------
        public void moveFirst()    { if (hasRecordset()) try { oRecordset.MoveFirst();    } catch { } }
        public void moveLast()     { if (hasRecordset()) try { oRecordset.MoveLast();     } catch { } }
        public void moveNext()     { if (hasRecordset()) try { oRecordset.MoveNext();     } catch { } }
        public void movePrevious() { if (hasRecordset()) try { oRecordset.MovePrevious(); } catch { } }

        // =====================================================================
        // findRecord: ADODB's Find method walks the cursor forward
        // (or backward) from a starting position to the first row
        // matching the criteria expression. The expression is a
        // single-column predicate: "lastName LIKE '%Smith%'" or
        // "salary > 50000". Multi-column searches require multiple
        // Find calls, but that matches the GUI's Find dialog UX.
        //
        // Returns true on match (cursor on the row); false on no
        // match (cursor at EOF or BOF, restored to original position).
        // =====================================================================
        public bool findRecord(string sCriteria, bool bForward, bool bFromCurrent)
        {
            if (!hasRecordset()) return false;
            if (string.IsNullOrEmpty(sCriteria)) return false;

            int iDir = bForward ? AdoConstants.adSearchForward : AdoConstants.adSearchBackward;
            int iStart = bFromCurrent
                ? AdoConstants.adBookmarkCurrent
                : (bForward ? AdoConstants.adBookmarkFirst : AdoConstants.adBookmarkLast);

            object oSavedBookmark = bookmark;
            try
            {
                // ADODB Find(criteria, skipRows, searchDirection, start)
                // Skip 1 row if we're starting from current and going
                // forward, so we don't match the row we're already on.
                int iSkip = bFromCurrent ? (bForward ? 1 : -1) : 0;
                oRecordset.Find(sCriteria, iSkip, iDir, iStart);
            }
            catch (COMException oEx)
            {
                throw new Exception("Find rejected: " + oEx.Message, oEx);
            }

            if (eof || bof)
            {
                // No match. Restore.
                if (oSavedBookmark != null) bookmark = oSavedBookmark;
                return false;
            }
            return true;
        }

        // =====================================================================
        // Field access. Field names are case-insensitive in ADODB.
        // Reading returns the stringified value; writing assigns and
        // does NOT call Update -- caller must call update() to commit.
        // This matches dbDot's pattern of "set fields then save."
        // =====================================================================
        public List<string> getFieldNames()
        {
            List<string> lResult = new List<string>();
            if (!hasRecordset()) return lResult;
            try
            {
                dynamic oFields = oRecordset.Fields;
                int iCount = (int)oFields.Count;
                for (int i = 0; i < iCount; i++)
                    lResult.Add((string)oFields[i].Name);
            }
            catch { }
            return lResult;
        }

        // getDisplayFieldNames: the columns the data list view
        // should show. Two-tier resolution:
        //
        //   (A) If the user has set an explicit select-list for the
        //       current table (via Select-Column), use that list
        //       after validating each name against the live field
        //       set. Names that no longer exist are dropped
        //       silently; the remaining names are shown in the
        //       order the user specified.
        //
        //   (B) Otherwise apply the default rules:
        //       1. Standard-visible columns (none currently) -- always shown.
        //       2. Standard-hidden columns (added, updated, marked,
        //          look, unq) -- always hidden.
        //       3. Any column ending in '_id' or named 'id' --
        //          always hidden (primary and foreign keys).
        //       4. Calculated columns from the schema (PRAGMA
        //          table_xinfo with hidden=2 or hidden=3) -- hidden.
        //
        // The Set-Record dialog and the dot prompt's 'show' command
        // still operate on the full getFieldNames() list, so hidden
        // columns are accessible for editing and inspection -- just
        // not present in the scrolling row view.
        public List<string> getDisplayFieldNames()
        {
            // (A) User-defined select list, if present.
            string sSelectList = getSelectList(sCurrentTable);
            if (!string.IsNullOrEmpty(sSelectList))
            {
                List<string> lFromUser = parseSelectList(sSelectList);
                HashSet<string> hFields = new HashSet<string>(getFieldNames(), StringComparer.OrdinalIgnoreCase);
                List<string> lValid = new List<string>();
                foreach (string sName in lFromUser)
                {
                    if (hFields.Contains(sName))
                    {
                        // Add with the canonical casing from the
                        // recordset (find the exact-cased name).
                        foreach (string sActual in getFieldNames())
                        {
                            if (string.Equals(sActual, sName, StringComparison.OrdinalIgnoreCase))
                            { lValid.Add(sActual); break; }
                        }
                    }
                }
                if (lValid.Count > 0) return lValid;
                // Fall through if the select-list yielded nothing
                // valid; the user expects to see something.
            }

            // (B) Default rule-based selection.
            List<string> lResult = new List<string>();
            foreach (string sName in getFieldNames())
            {
                if (Metadata.isStandardVisible(sName))
                {
                    lResult.Add(sName);
                    continue;
                }
                if (Metadata.isStandardHidden(sName, sCurrentTable))
                    continue;
                if (isCalculatedColumn(sCurrentTable, sName))
                    continue;
                lResult.Add(sName);
            }
            return lResult;
        }

        // Parse a comma-separated (with optional whitespace) list
        // into a list of names. Empty entries are dropped.
        public static List<string> parseSelectList(string sList)
        {
            List<string> lResult = new List<string>();
            if (string.IsNullOrEmpty(sList)) return lResult;
            foreach (string sRaw in sList.Split(','))
            {
                string sName = sRaw.Trim();
                if (sName.Length > 0) lResult.Add(sName);
            }
            return lResult;
        }

        // Get the comma-separated select list for the named table.
        // Empty string means "no user override; use default rules."
        public string getSelectList(string sTable)
        {
            if (string.IsNullOrEmpty(sTable)) return "";
            TableSettings oSettings;
            if (dTableCache.TryGetValue(sTable, out oSettings))
                return oSettings.sSelectList ?? "";
            return "";
        }

        // Set the select list for the named table. Pass empty
        // string to clear the override and revert to default rules.
        // Validates each name against the live field set; invalid
        // names are dropped from the stored list. Returns the
        // canonicalized list (the valid names, exact-cased,
        // comma-separated).
        public string setSelectList(string sTable, string sList)
        {
            if (string.IsNullOrEmpty(sTable)) return "";
            // If the table is the current one, we can validate
            // against the live recordset's field names. For other
            // tables we accept the input verbatim and let
            // getDisplayFieldNames validate when the table is
            // later opened.
            string sStored = sList ?? "";
            if (hasRecordset() && string.Equals(sTable, sCurrentTable, StringComparison.OrdinalIgnoreCase))
            {
                List<string> lRequested = parseSelectList(sStored);
                HashSet<string> hFields = new HashSet<string>(getFieldNames(), StringComparer.OrdinalIgnoreCase);
                List<string> lValid = new List<string>();
                foreach (string sName in lRequested)
                {
                    if (hFields.Contains(sName))
                    {
                        foreach (string sActual in getFieldNames())
                        {
                            if (string.Equals(sActual, sName, StringComparison.OrdinalIgnoreCase))
                            { lValid.Add(sActual); break; }
                        }
                    }
                }
                sStored = string.Join(", ", lValid.ToArray());
            }

            TableSettings oSettings;
            if (!dTableCache.TryGetValue(sTable, out oSettings))
            {
                oSettings = new TableSettings();
                dTableCache[sTable] = oSettings;
            }
            oSettings.sSelectList = sStored;
            return sStored;
        }

        // Distinct fields = field names that are NOT bookkeeping
        // (added, updated, observed, notes, tags, marked) and NOT the
        // primary-key column. These are the substantive content fields
        // and what the New-Record / Set-Record dialog shows by default.
        //
        // Pattern lifted from db.py's getDistinctFields, applied here at
        // the field level rather than the table-info level.
        public List<string> getDistinctFieldNames()
        {
            List<string> lResult = new List<string>();
            string sTable = sCurrentTable ?? "";
            foreach (string sField in getFieldNames())
            {
                if (!Metadata.isBookkeepingColumn(sField, sTable))
                    lResult.Add(sField);
            }
            return lResult;
        }

        // metadata fields = the complement of distinct fields; the
        // bookkeeping columns plus primary key. Show-Record groups
        // these at the bottom of its output.
        public List<string> getMetadataFieldNames()
        {
            List<string> lResult = new List<string>();
            string sTable = sCurrentTable ?? "";
            foreach (string sField in getFieldNames())
            {
                if (Metadata.isBookkeepingColumn(sField, sTable))
                    lResult.Add(sField);
            }
            return lResult;
        }

        // True if the current table has the named column (case-insensitive).
        public bool hasField(string sName)
        {
            if (string.IsNullOrEmpty(sName)) return false;
            string sLower = sName.ToLowerInvariant();
            foreach (string sF in getFieldNames())
                if (sF.ToLowerInvariant() == sLower) return true;
            return false;
        }

        public string getFieldValue(string sName)
        {
            if (!hasRecordset()) return "";
            if (eof || bof) return "";
            try
            {
                dynamic oField = oRecordset.Fields[sName];
                object oValue = oField.Value;
                return (oValue == null || oValue is DBNull) ? "" : oValue.ToString();
            }
            catch { return ""; }
        }

        // True if the named field is a binary (BLOB) type. Used by the
        // grid renderer to substitute a size summary for the unhelpful
        // "System.Byte[]" that ADO returns for BLOB columns.
        //
        // ADO Field.Type returns the DataTypeEnum value. The binary
        // types we care about are:
        //   adBinary       = 128
        //   adVarBinary    = 204
        //   adLongVarBinary = 205
        public bool isFieldBinary(string sName)
        {
            if (!hasRecordset()) return false;
            try
            {
                dynamic oField = oRecordset.Fields[sName];
                int iType = (int)oField.Type;
                return (iType == 128 || iType == 204 || iType == 205);
            }
            catch { return false; }
        }

        // Length of a binary field's value, in bytes. Returns 0 for null,
        // -1 if the field cannot be sized (rare). ADO Field has an
        // ActualSize property that gives the byte count for variable
        // length data including BLOBs.
        public int getFieldByteLength(string sName)
        {
            if (!hasRecordset()) return 0;
            if (eof || bof) return 0;
            try
            {
                dynamic oField = oRecordset.Fields[sName];
                object oValue = oField.Value;
                if (oValue == null || oValue is DBNull) return 0;
                try { return (int)oField.ActualSize; }
                catch { }
                if (oValue is byte[]) return ((byte[])oValue).Length;
                return -1;
            }
            catch { return -1; }
        }

        public void setFieldValue(string sName, string sValue)
        {
            if (!hasRecordset()) throw new InvalidOperationException("No recordset open.");
            if (bReadOnly) throw new InvalidOperationException("Database is read-only.");
            try
            {
                oRecordset.Fields[sName].Value = (sValue == null) ? (object)DBNull.Value : (object)sValue;
            }
            catch (COMException oEx)
            {
                throw new Exception("Cannot set field '" + sName + "': " + oEx.Message, oEx);
            }
        }

        // ------- Edit lifecycle -------
        public void addNew()
        {
            if (!hasRecordset()) throw new InvalidOperationException("No recordset open.");
            if (bReadOnly) throw new InvalidOperationException("Database is read-only.");
            oRecordset.AddNew();
        }

        public void update()
        {
            if (!hasRecordset()) return;
            if (bReadOnly) throw new InvalidOperationException("Database is read-only.");
            try { oRecordset.Update(); }
            catch (COMException oEx) { throw new Exception("Update failed: " + oEx.Message, oEx); }
        }

        public void cancelUpdate()
        {
            if (!hasRecordset()) return;
            try { oRecordset.CancelUpdate(); } catch { }
        }

        public void deleteCurrent()
        {
            if (!hasRecordset()) throw new InvalidOperationException("No recordset open.");
            if (bReadOnly) throw new InvalidOperationException("Database is read-only.");
            if (eof || bof) throw new InvalidOperationException("No current record.");
            try
            {
                oRecordset.Delete();
                // After Delete, ADODB leaves the cursor on the deleted
                // row (which is now invalid). Move to next.
                try { oRecordset.MoveNext(); } catch { }
                if (eof && recordCount > 0) try { oRecordset.MoveLast(); } catch { }
            }
            catch (COMException oEx)
            {
                throw new Exception("Delete failed: " + oEx.Message, oEx);
            }
        }

        // ------- requery (Update-View) -------
        public void requery()
        {
            if (!hasRecordset()) return;
            object oBookmark = bookmark;
            try { oRecordset.Requery(); } catch (COMException oEx) { throw new Exception("Requery failed: " + oEx.Message, oEx); }
            if (oBookmark != null) bookmark = oBookmark;
        }

        // =====================================================================
        // invokeSql: raw SQL via Connection.Execute. Used by the CLI's
        // Invoke-Sql command and by the GUI's Verbatim SQL dialog.
        // Routing:
        //   queries (SELECT/PRAGMA/EXPLAIN/WITH) -> render results to oOut
        //   statements (INSERT/UPDATE/DELETE/etc) -> records-affected count
        // Returns the records-affected count, or -1 if we rendered
        // results to oOut.
        // =====================================================================
        public int invokeSql(string sSql, TextWriter oOut)
        {
            if (!isOpen()) throw new InvalidOperationException("No database open.");
            if (string.IsNullOrEmpty(sSql)) return 0;

            string sTrim = sSql.TrimStart();
            string sUpper = sTrim.ToUpperInvariant();
            bool bIsQuery = sUpper.StartsWith("SELECT") || sUpper.StartsWith("PRAGMA")
                         || sUpper.StartsWith("EXPLAIN") || sUpper.StartsWith("WITH");

            if (bIsQuery)
            {
                dynamic oRs = createComObject("ADODB.Recordset");
                try
                {
                    oRs.CursorLocation = AdoConstants.adUseClient;
                    oRs.Open(sSql, oConn, AdoConstants.adOpenStatic,
                        AdoConstants.adLockReadOnly, AdoConstants.adCmdText);
                    renderRecordset(oRs, oOut);
                }
                finally
                {
                    try { if (((int)oRs.State) != 0) oRs.Close(); } catch { }
                    releaseCom(oRs); oRs = null;
                }
                return -1;
            }
            else
            {
                int iAffected = 0;
                try
                {
                    // Connection.Execute(commandText, recordsAffected, options)
                    // ADODB returns the records-affected count via a [out]
                    // VARIANT* parameter. Late-bound C# can pass `ref`
                    // through the DLR's COM binder, but we're defensive
                    // here: if the ref-pattern fails for any reason, we
                    // re-execute without the ref argument and return 0.
                    object oRecordsAffected = 0;
                    try
                    {
                        oConn.Execute(sSql, ref oRecordsAffected,
                            AdoConstants.adCmdText | AdoConstants.adExecuteNoRecords);
                        if (oRecordsAffected is int) iAffected = (int)oRecordsAffected;
                        else if (oRecordsAffected != null)
                        {
                            int iTmp;
                            if (int.TryParse(oRecordsAffected.ToString(), out iTmp)) iAffected = iTmp;
                        }
                    }
                    catch (RuntimeBinderException)
                    {
                        // DLR couldn't bind the ref; fall back to two-arg form.
                        // The records-affected count is then unavailable.
                        oConn.Execute(sSql, Type.Missing,
                            AdoConstants.adCmdText | AdoConstants.adExecuteNoRecords);
                        iAffected = 0;
                    }
                }
                catch (COMException oEx)
                {
                    throw new Exception("SQL failed: " + oEx.Message, oEx);
                }
                return iAffected;
            }
        }

        private void renderRecordset(dynamic oRs, TextWriter oOut)
        {
            dynamic oFields = oRs.Fields;
            int iFieldCount = (int)oFields.Count;
            List<string> lH = new List<string>();
            for (int i = 0; i < iFieldCount; i++) lH.Add((string)oFields[i].Name);
            oOut.WriteLine(string.Join(" | ", lH));
            oOut.WriteLine(new string('-', Math.Min(80, string.Join(" | ", lH).Length)));
            int iRow = 0;
            while (!(bool)oRs.EOF)
            {
                List<string> lV = new List<string>();
                for (int i = 0; i < iFieldCount; i++)
                {
                    object oValue = oFields[i].Value;
                    lV.Add((oValue == null || oValue is DBNull) ? "" : oValue.ToString());
                }
                oOut.WriteLine(string.Join(" | ", lV));
                iRow++;
                oRs.MoveNext();
            }
            oOut.WriteLine(string.Format("({0} row(s))", iRow));
        }

        // =====================================================================
        // exportData: write the current view (filtered, sorted) to a
        // file. Format inferred from extension (.csv, .tsv, .xlsx,
        // .html, .docx). Currently we implement CSV, TSV, and HTML
        // natively in C#; XLSX and DOCX are deferred to the form layer
        // which can use the COM Excel/Word automation that dbDot does
        // (multiArrayToTables in dbDot.vbs lines 52-186).
        //
        // Save-As to a different .db / .mdb / .xlsx etc. is a separate
        // method (saveAs) that copies the entire database, not just
        // the current view.
        // =====================================================================
        public void exportData(string sDestPath)
        {
            if (!hasRecordset()) throw new InvalidOperationException("No recordset open.");
            if (string.IsNullOrEmpty(sDestPath)) throw new ArgumentException("exportData requires a path.");

            string sExt = Path.GetExtension(sDestPath).TrimStart('.').ToLowerInvariant();
            switch (sExt)
            {
                // Plain-text tabular.
                case "csv": exportDelimited(sDestPath, ","); break;
                case "tsv":
                case "tab": exportDelimited(sDestPath, "\t"); break;
                case "md":
                case "markdown":
                    exportMarkdown(sDestPath);
                    break;
                // Document formats.
                case "html":
                case "htm": exportHtml(sDestPath); break;
                case "xlsx":
                case "xls":
                    exportSpreadsheet(sDestPath, sExt);
                    break;
                case "docx":
                case "doc":
                    exportWord(sDestPath, sExt, false);
                    break;
                // Database formats. Closes the "every input format
                // is also an export format" loop: anything DbDuo can
                // open, it can also write.
                case "db":
                case "sqlite":
                case "sqlite3":
                case "mdb":
                case "accdb":
                case "dbf":
                    exportDatabase(sDestPath, sExt);
                    break;
                default:
                    throw new Exception("exportData: unsupported format ." + sExt
                        + ". Supported: csv, tsv, md, html, xlsx, docx, db, sqlite, mdb, accdb, dbf. "
                        + "xlsx and docx require Microsoft Office.");
            }
        }

        // exportDataMulti: dbDot-compatible multi-format export.
        // Accepts a space-separated list of extensions (xlsx, docx,
        // html, csv) plus dbDot single-letter shortcuts (x, d, h, c).
        // Generates one file per extension in the same folder, named
        // after the current table with a unique numeric suffix if a
        // file by that name already exists. Returns the list of paths
        // actually written.
        //
        // xlsx and docx require Microsoft Office. The same Word
        // and Excel Application objects drive html and csv when
        // requested alongside; this matches dbDot.vbs / HomerLib.vbs
        // behavior verbatim.
        //
        // If sFolder is null/empty, the database file's folder is
        // used. If sBaseName is null/empty, the current table name
        // is used (with non-filesystem-safe characters replaced).
        public List<string> exportDataMulti(string sExtensions, string sFolder, string sBaseName)
        {
            if (!hasRecordset()) throw new InvalidOperationException("No recordset open.");
            string sExt = (sExtensions ?? "").Trim();
            if (sExt.Length == 0) sExt = "xlsx";

            // Normalize dbDot single-letter shortcuts and bare extension
            // names. The user can type "Export-Data x d h c m" or
            // "xlsx docx markdown" -- both work. Dots are stripped.
            sExt = sExt.Replace(".", " ").Replace(",", " ").ToLowerInvariant();
            List<string> lFormats = new List<string>();
            foreach (string sToken in sExt.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string sNorm = sToken;
                if (sNorm == "x") sNorm = "xlsx";
                else if (sNorm == "d") sNorm = "docx";
                else if (sNorm == "h") sNorm = "html";
                else if (sNorm == "c") sNorm = "csv";
                else if (sNorm == "t") sNorm = "tsv";
                else if (sNorm == "m" || sNorm == "markdown") sNorm = "md";
                else if (sNorm == "s" || sNorm == "sqlite" || sNorm == "sqlite3") sNorm = "db";
                else if (sNorm == "a" || sNorm == "access" || sNorm == "accdb") sNorm = "mdb";
                else if (sNorm == "b" || sNorm == "dbase") sNorm = "dbf";
                if (!lFormats.Contains(sNorm)) lFormats.Add(sNorm);
            }

            // Resolve folder.
            string sDir = sFolder;
            if (string.IsNullOrEmpty(sDir))
            {
                if (!string.IsNullOrEmpty(filePath)) sDir = Path.GetDirectoryName(filePath);
                if (string.IsNullOrEmpty(sDir)) sDir = Environment.CurrentDirectory;
            }
            try { if (!Directory.Exists(sDir)) Directory.CreateDirectory(sDir); }
            catch (Exception oEx) { throw new Exception("Cannot create folder " + sDir + ": " + oEx.Message); }

            // Resolve base name.
            string sBase = sBaseName;
            if (string.IsNullOrEmpty(sBase)) sBase = sCurrentTable;
            if (string.IsNullOrEmpty(sBase)) sBase = "Export";
            sBase = sanitizeFileName(sBase);

            List<string> lWritten = new List<string>();

            // Group all wanted formats.
            bool bWantXlsx = lFormats.Contains("xlsx");
            bool bWantCsv  = lFormats.Contains("csv");
            bool bWantTsv  = lFormats.Contains("tsv");
            bool bWantMd   = lFormats.Contains("md");
            bool bWantDocx = lFormats.Contains("docx");
            bool bWantHtml = lFormats.Contains("html") || lFormats.Contains("htm");
            bool bWantDb   = lFormats.Contains("db");
            bool bWantMdb  = lFormats.Contains("mdb");
            bool bWantDbf  = lFormats.Contains("dbf");

            // Plain-text tabular outputs (native, no Office needed).
            if (bWantCsv)
            {
                string sP = uniqueExportPath(sDir, sBase, "csv");
                exportDelimited(sP, ",");
                lWritten.Add(sP);
            }
            if (bWantTsv)
            {
                string sP = uniqueExportPath(sDir, sBase, "tsv");
                exportDelimited(sP, "\t");
                lWritten.Add(sP);
            }
            if (bWantMd)
            {
                string sP = uniqueExportPath(sDir, sBase, "md");
                exportMarkdown(sP);
                lWritten.Add(sP);
            }

            // Spreadsheet output via Excel COM.
            if (bWantXlsx)
            {
                string sP = uniqueExportPath(sDir, sBase, "xlsx");
                exportSpreadsheet(sP, "xlsx");
                lWritten.Add(sP);
            }

            // Document outputs via Word COM.
            if (bWantDocx)
            {
                string sP = uniqueExportPath(sDir, sBase, "docx");
                exportWord(sP, "docx", false);
                lWritten.Add(sP);
            }
            if (bWantHtml)
            {
                string sP = uniqueExportPath(sDir, sBase, "html");
                exportWord(sP, "html", true);
                lWritten.Add(sP);
            }

            // Database outputs via fresh ADODB.Connection.
            if (bWantDb)
            {
                string sP = uniqueExportPath(sDir, sBase, "db");
                exportDatabase(sP, "db");
                lWritten.Add(sP);
            }
            if (bWantMdb)
            {
                string sP = uniqueExportPath(sDir, sBase, "accdb");
                exportDatabase(sP, "accdb");
                lWritten.Add(sP);
            }
            if (bWantDbf)
            {
                string sP = uniqueExportPath(sDir, sBase, "dbf");
                exportDatabase(sP, "dbf");
                lWritten.Add(sP);
            }

            return lWritten;
        }

        // sanitizeFileName: keep alnum/underscore/dash/dot, replace
        // everything else with underscore. Conservative across all
        // Windows filesystems.
        private static string sanitizeFileName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Export";
            char[] aBad = Path.GetInvalidFileNameChars();
            StringBuilder oSb = new StringBuilder();
            foreach (char c in s)
            {
                bool bBad = false;
                foreach (char cBad in aBad) if (c == cBad) { bBad = true; break; }
                oSb.Append(bBad ? '_' : c);
            }
            return oSb.ToString();
        }

        // uniqueExportPath: <folder>\<base>.<ext>, or if that exists,
        // <folder>\<base>_2.<ext>, then _3, etc. Matches the spirit
        // of dbDot/HomerLib's PathGetUnique. Stops at 999 to bound
        // unbounded loops on a misconfigured folder.
        private static string uniqueExportPath(string sFolder, string sBase, string sExt)
        {
            string sFirst = Path.Combine(sFolder, sBase + "." + sExt);
            if (!File.Exists(sFirst)) return sFirst;
            for (int i = 2; i <= 999; i++)
            {
                string sCandidate = Path.Combine(sFolder, sBase + "_" + i + "." + sExt);
                if (!File.Exists(sCandidate)) return sCandidate;
            }
            // Worst case: timestamp.
            return Path.Combine(sFolder, sBase + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "." + sExt);
        }

        // exportSpreadsheet: write the current recordset to an .xlsx
        // file via Excel.Application late-bound COM. Requires Excel
        // to be installed. Throws InvalidOperationException with a
        // human message if the ProgID isn't registered.
        //
        // The output replicates dbDot's HomerLib.multiArrayToTables:
        // header row from getFieldNames, value rows below, column
        // widths auto-fit, cells wrap-text, vertical alignment top,
        // a named range "ColumnTitle" anchored at A1.
        //
        // For .xls (legacy), the same workflow runs and SaveAs uses
        // xlExcel8 (56) instead of xlWorkbookDefault (51).
        public void exportSpreadsheet(string sDestPath, string sExt)
        {
            if (!hasRecordset()) throw new InvalidOperationException("No recordset open.");
            object oBookmark = bookmark;

            dynamic oApp = null;
            dynamic oBook = null;
            dynamic oSheet = null;
            try
            {
                oApp = ComAutomation.createApp("Excel.Application");
                oApp.Visible = false;
                oApp.DisplayAlerts = false;
                try { oApp.ScreenUpdating = false; } catch { }

                oBook = oApp.Workbooks.Add();
                oSheet = oBook.Sheets[1];

                List<string> lFields = getDisplayFieldNames();
                if (lFields.Count == 0) lFields = getFieldNames();

                // Headers.
                for (int i = 0; i < lFields.Count; i++)
                    oSheet.Cells[1, i + 1] = lFields[i];

                // Anchor a named range at A1, matching dbDot.
                try { oSheet.Names.Add("ColumnTitle", oSheet.Range["A1"]); } catch { }

                // Rows.
                moveFirst();
                int iRow = 2;
                while (!eof)
                {
                    for (int i = 0; i < lFields.Count; i++)
                    {
                        string sVal = getFieldValue(lFields[i]) ?? "";
                        oSheet.Cells[iRow, i + 1] = sVal;
                    }
                    moveNext();
                    iRow++;
                }

                // Format like dbDot: auto-fit columns, wrap text,
                // align top, auto-fit rows. AutomationSecurity low
                // was set in dbDot to suppress macro prompts on Save.
                try { oSheet.Columns.AutoFit(); } catch { }
                try { oSheet.Cells.WrapText = true; } catch { }
                try { oSheet.Rows.AutoFit(); } catch { }
                try { oSheet.Rows.VerticalAlignment = -4160 /* xlVAlignTop */; } catch { }

                int iFmt = (sExt == "xls") ? 56 /* xlExcel8 */ : 51 /* xlWorkbookDefault */;
                oSheet.SaveAs(sDestPath, iFmt);
                oBook.Close(false);
            }
            finally
            {
                if (oBookmark != null) try { bookmark = oBookmark; } catch { }
                try { if (oApp != null) oApp.Quit(); } catch { }
                releaseCom(oSheet);
                releaseCom(oBook);
                releaseCom(oApp);
            }
        }

        // exportWord: write the current recordset to a .docx file or
        // a Filtered-HTML .html file via Word.Application late-bound
        // COM. Requires Word to be installed.
        //
        // The output replicates dbDot's HomerLib.multiArrayToTables
        // Word path: header row in the first row of a Word table,
        // value rows below, heading-format on row 1, auto-fit
        // behavior wdAutoFitContent, a bookmark "ColumnTitle"
        // anchored at the second cell of the first row.
        //
        // bFilteredHtml = true uses wdFormatFilteredHtml (10), which
        // produces clean HTML without MSO-specific markup. Filtered
        // HTML is the user's preferred format for screen readers.
        public void exportWord(string sDestPath, string sExt, bool bFilteredHtml)
        {
            if (!hasRecordset()) throw new InvalidOperationException("No recordset open.");
            object oBookmark = bookmark;

            dynamic oApp = null;
            dynamic oDoc = null;
            try
            {
                oApp = ComAutomation.createApp("Word.Application");
                oApp.Visible = false;
                try { oApp.DisplayAlerts = 0 /* wdAlertsNone */; } catch { }
                try { oApp.ScreenUpdating = false; } catch { }

                oDoc = oApp.Documents.Add();
                dynamic oRange = oDoc.Content;

                List<string> lFields = getDisplayFieldNames();
                if (lFields.Count == 0) lFields = getFieldNames();
                int iFieldCount = lFields.Count;

                int iRowCount = recordCount;
                if (iRowCount < 0) iRowCount = 0;

                // Pre-size the table: header row + N value rows.
                dynamic oTable = oDoc.Tables.Add(oRange, iRowCount + 1, iFieldCount);

                // Header row.
                for (int i = 0; i < iFieldCount; i++)
                {
                    try { oTable.Rows[1].Cells[i + 1].Range.Text = lFields[i]; } catch { }
                }

                // Value rows.
                moveFirst();
                int iRow = 2;
                while (!eof && iRow <= iRowCount + 1)
                {
                    for (int i = 0; i < iFieldCount; i++)
                    {
                        string sVal = getFieldValue(lFields[i]) ?? "";
                        try { oTable.Rows[iRow].Cells[i + 1].Range.Text = sVal; } catch { }
                    }
                    moveNext();
                    iRow++;
                }

                // Format like dbDot: auto-fit, heading-format on
                // header row, named bookmark anchored to row 1 / col 2.
                try { oTable.AllowAutoFit = true; } catch { }
                try { oTable.AutoFitBehavior(1 /* wdAutoFitContent */); } catch { }
                try { oTable.Rows[1].HeadingFormat = true; } catch { }
                try { oTable.ApplyStyleHeadingRows = true; } catch { }
                try { if (iFieldCount >= 2) oDoc.Bookmarks.Add("ColumnTitle", oTable.Rows[1].Cells[2].Range); } catch { }

                // SaveAs format codes:
                //   wdFormatDocumentDefault  = 16  (docx)
                //   wdFormatFilteredHtml     = 10  (filtered HTML)
                int iFmt = bFilteredHtml ? 10 : 16;
                oDoc.SaveAs(sDestPath, iFmt);
                oDoc.Close(0 /* wdDoNotSaveChanges */);
                try { if (!(bool)oApp.NormalTemplate.Saved) oApp.NormalTemplate.Saved = true; } catch { }
            }
            finally
            {
                if (oBookmark != null) try { bookmark = oBookmark; } catch { }
                try { if (oApp != null) oApp.Quit(); } catch { }
                releaseCom(oDoc);
                releaseCom(oApp);
            }
        }

        // exportDatabase: write the current recordset to a new
        // database file in one of the formats DbDuo can also OPEN
        // (SQLite, Access, dBASE). This closes the "every input
        // format is also an export format" loop. The new file gets
        // exactly one table, named after the source table.
        //
        // Strategy: open a fresh ADODB.Connection to the target
        // file using DbDuo's own buildConnectString helper (so the
        // same provider mapping that drives openDatabase drives the
        // export); issue CREATE TABLE with TEXT columns for every
        // field (a permissive type that all three back-ends accept
        // and that round-trips screen-reader values cleanly); then
        // INSERT one row at a time. The original recordset is not
        // disturbed.
        //
        // For SQLite (.db / .sqlite / .sqlite3), the file is
        // created on disk first (SQLite ODBC opens an existing path
        // or implicitly creates one). For Access (.mdb / .accdb),
        // ADOX.Catalog.Create is the canonical way to create a
        // fresh database file. For dBASE (.dbf), the connection is
        // to the parent folder and the .dbf file is created by the
        // CREATE TABLE statement itself.
        //
        // Requires the appropriate driver installed: SQLite ODBC
        // for .db / .sqlite, ACE for .mdb / .accdb / .dbf. The
        // installer already handles those.
        public void exportDatabase(string sDestPath, string sExt)
        {
            if (!hasRecordset()) throw new InvalidOperationException("No recordset open.");
            if (string.IsNullOrEmpty(sDestPath)) throw new ArgumentException("exportDatabase requires a path.");
            string sExtLower = (sExt ?? "").ToLowerInvariant().TrimStart('.');

            // Make sure the destination folder exists.
            string sDir = Path.GetDirectoryName(sDestPath);
            if (!string.IsNullOrEmpty(sDir) && !Directory.Exists(sDir))
                Directory.CreateDirectory(sDir);

            // For Access we must create the empty database file via
            // ADOX before we can connect to it; for SQLite the ODBC
            // driver implicitly creates an empty file on open; for
            // dBASE the file is created by the CREATE TABLE itself.
            if (sExtLower == "mdb" || sExtLower == "accdb")
            {
                // Don't overwrite silently.
                if (File.Exists(sDestPath))
                    throw new IOException("Target file already exists: " + sDestPath);
                createEmptyAccessFile(sDestPath, sExtLower);
            }
            else if (sExtLower == "db" || sExtLower == "sqlite" || sExtLower == "sqlite3")
            {
                if (File.Exists(sDestPath))
                    throw new IOException("Target file already exists: " + sDestPath);
                // The SQLite ODBC driver creates the database file
                // on Open() if it doesn't exist. Nothing to do here.
            }

            object oBookmark = bookmark;
            List<string> lFields = getDisplayFieldNames();
            if (lFields.Count == 0) lFields = getFieldNames();

            // Build the connection string using the same logic as
            // openDatabase so the providers and modes line up
            // exactly. The buildConnectString takes a ref string
            // for table-name extraction; we don't need it for
            // export, but the method signature requires the slot.
            string sUnusedTable = "";
            bool bIsFolder = (sExtLower == "dbf");
            // For dBASE, the connection is to the folder, not the
            // file. We construct the same way buildConnectString
            // expects for a folder.
            string sConnPath = sDestPath;
            string sTableName = sCurrentTable;
            if (string.IsNullOrEmpty(sTableName)) sTableName = "Export";
            if (sExtLower == "dbf")
            {
                // dBASE table names: max 8 chars, no extension, no spaces.
                sTableName = sanitizeDbaseTableName(sTableName);
            }
            string sConnect = buildConnectStringForExport(sConnPath, sExtLower, sTableName);

            dynamic oConnExport = null;
            try
            {
                oConnExport = createComObject("ADODB.Connection");
                oConnExport.CursorLocation = AdoConstants.adUseClient;
                oConnExport.Open(sConnect);

                // Build CREATE TABLE. TEXT columns are the safest
                // universal type. SQLite is dynamically typed, so
                // TEXT is fine. Access (Jet/ACE) accepts TEXT as a
                // synonym for the longest variable-length text
                // column it offers. dBASE accepts CHAR(n) and
                // MEMO; we use MEMO so very long fields don't
                // truncate.
                string sCreateSql = buildCreateTableSql(sTableName, lFields, sExtLower);
                oConnExport.Execute(sCreateSql);

                // Insert rows.
                string sInsertSql = buildInsertSql(sTableName, lFields, sExtLower);

                moveFirst();
                while (!eof)
                {
                    // Build the parameterized INSERT each time; we
                    // do this rather than prepared statements
                    // because late-bound ADO parameterization is
                    // awkward, and the values are already strings
                    // from getFieldValue. Escape single quotes
                    // and embedded delimiters per back-end.
                    List<string> lValues = new List<string>();
                    foreach (string sN in lFields)
                        lValues.Add(quoteSqlLiteral(getFieldValue(sN), sExtLower));
                    string sFullSql = sInsertSql.Replace("@@VALUES@@", string.Join(",", lValues));
                    oConnExport.Execute(sFullSql);
                    moveNext();
                }
            }
            finally
            {
                if (oBookmark != null) try { bookmark = oBookmark; } catch { }
                try { if (oConnExport != null) oConnExport.Close(); } catch { }
                releaseCom(oConnExport);
            }
        }

        // Build a connection string for an EXPORT target. Differs
        // from buildConnectString only in that the dBASE / text
        // cases want to point at the destination folder, not at the
        // destination file. For SQLite, Access, Excel we just reuse
        // the same provider strings.
        private static string buildConnectStringForExport(string sPath, string sExt, string sTable)
        {
            switch (sExt)
            {
                case "db":
                case "sqlite":
                case "sqlite3":
                    return string.Format("DRIVER=SQLite3 ODBC Driver;Database={0};", sPath);

                case "mdb":
                case "accdb":
                    return string.Format(
                        "Provider={0};Data Source={1};Persist Security Info=False;",
                        DefaultProvider, sPath);

                case "dbf":
                    // Point at the parent folder.
                    string sFolder = Path.GetDirectoryName(sPath);
                    if (string.IsNullOrEmpty(sFolder)) sFolder = Environment.CurrentDirectory;
                    return string.Format(
                        "Provider={0};Data Source={1};Extended Properties=\"dBASE IV;\"",
                        DefaultProvider, sFolder);

                default:
                    throw new Exception("exportDatabase: unsupported format ." + sExt);
            }
        }

        // Create an empty .mdb / .accdb file via ADOX.Catalog.Create.
        // This is the canonical way to fabricate a fresh Access
        // database without having to ship a stub file.
        private static void createEmptyAccessFile(string sPath, string sExt)
        {
            dynamic oCat = null;
            try
            {
                oCat = createComObject("ADOX.Catalog");
                string sConn = string.Format(
                    "Provider={0};Data Source={1};",
                    DefaultProvider, sPath);
                oCat.Create(sConn);
            }
            catch (Exception oEx)
            {
                throw new Exception("Could not create Access database: " + oEx.Message
                    + " (Is ADOX installed? It's part of MDAC/ACE.)", oEx);
            }
            finally
            {
                releaseCom(oCat);
            }
        }

        // dBASE limits table (file) names to 8 characters and a
        // small character set. Truncate and replace forbidden
        // characters with underscore.
        private static string sanitizeDbaseTableName(string sName)
        {
            if (string.IsNullOrEmpty(sName)) return "EXPORT";
            StringBuilder oSb = new StringBuilder();
            foreach (char c in sName.ToUpperInvariant())
            {
                if (oSb.Length >= 8) break;
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
                    oSb.Append(c);
                else
                    oSb.Append('_');
            }
            if (oSb.Length == 0) return "EXPORT";
            return oSb.ToString();
        }

        // Build CREATE TABLE statement appropriate for each
        // back-end. All fields are text-typed for portability.
        private string buildCreateTableSql(string sTable, List<string> lFields, string sExt)
        {
            StringBuilder oSb = new StringBuilder();
            oSb.Append("CREATE TABLE ");
            oSb.Append(quoteIdentifier(sTable, sExt));
            oSb.Append(" (");
            for (int i = 0; i < lFields.Count; i++)
            {
                if (i > 0) oSb.Append(", ");
                oSb.Append(quoteIdentifier(lFields[i], sExt));
                oSb.Append(' ');
                // Column type per back-end:
                //   SQLite -- TEXT (dynamic typing makes this no
                //     constraint anyway).
                //   Access -- MEMO (long text).
                //   dBASE -- MEMO (variable-length text, stored in
                //     a separate .dbt file alongside the .dbf).
                if (sExt == "db" || sExt == "sqlite" || sExt == "sqlite3")
                    oSb.Append("TEXT");
                else if (sExt == "mdb" || sExt == "accdb")
                    oSb.Append("MEMO");
                else // dbf
                    oSb.Append("MEMO");
            }
            oSb.Append(')');
            return oSb.ToString();
        }

        // Build the INSERT template, with the literal "@@VALUES@@"
        // placeholder where the per-row value list will be spliced.
        private string buildInsertSql(string sTable, List<string> lFields, string sExt)
        {
            StringBuilder oSb = new StringBuilder();
            oSb.Append("INSERT INTO ");
            oSb.Append(quoteIdentifier(sTable, sExt));
            oSb.Append(" (");
            for (int i = 0; i < lFields.Count; i++)
            {
                if (i > 0) oSb.Append(", ");
                oSb.Append(quoteIdentifier(lFields[i], sExt));
            }
            oSb.Append(") VALUES (@@VALUES@@)");
            return oSb.ToString();
        }

        // Quote an identifier per back-end:
        //   SQLite -- double quotes (ANSI SQL).
        //   Access -- square brackets.
        //   dBASE  -- bare, uppercased, no special characters.
        private static string quoteIdentifier(string sName, string sExt)
        {
            if (sExt == "mdb" || sExt == "accdb")
                return "[" + sName.Replace("]", "") + "]";
            if (sExt == "dbf")
                return sanitizeDbaseTableName(sName);
            // SQLite default.
            return "\"" + sName.Replace("\"", "\"\"") + "\"";
        }

        // Quote a literal value as a SQL string. NULL handling
        // differs slightly per back-end but SQL standard NULL is
        // accepted everywhere.
        private static string quoteSqlLiteral(string sValue, string sExt)
        {
            if (sValue == null) return "NULL";
            // All three back-ends accept SQL-standard apostrophe
            // doubling for embedded quotes within a single-quoted
            // string literal.
            return "'" + sValue.Replace("'", "''") + "'";
        }

        // exportMarkdown: write the current recordset as a GitHub-
        // flavored Markdown table. No external library needed --
        // the format is line-based plain text with pipe delimiters
        // and a header-separator row. Useful for pasting into a
        // README, an issue, a chat message, or any Markdown editor.
        public void exportMarkdown(string sDestPath)
        {
            if (!hasRecordset()) throw new InvalidOperationException("No recordset open.");
            object oBookmark = bookmark;
            List<string> lFields = getDisplayFieldNames();
            if (lFields.Count == 0) lFields = getFieldNames();

            using (StreamWriter oW = new StreamWriter(sDestPath, false, new UTF8Encoding(true)))
            {
                // Optional title line.
                if (!string.IsNullOrEmpty(sCurrentTable))
                {
                    oW.WriteLine("# " + sCurrentTable);
                    oW.WriteLine();
                }
                // Header row.
                oW.Write("|");
                foreach (string sN in lFields) { oW.Write(" "); oW.Write(escapeMarkdownCell(sN)); oW.Write(" |"); }
                oW.WriteLine();
                // Separator row.
                oW.Write("|");
                foreach (string sN in lFields) { oW.Write(" --- |"); }
                oW.WriteLine();
                // Value rows.
                moveFirst();
                while (!eof)
                {
                    oW.Write("|");
                    foreach (string sN in lFields)
                    {
                        oW.Write(" ");
                        oW.Write(escapeMarkdownCell(getFieldValue(sN)));
                        oW.Write(" |");
                    }
                    oW.WriteLine();
                    moveNext();
                }
            }
            if (oBookmark != null) bookmark = oBookmark;
        }

        // Escape a cell value for safe inclusion in a Markdown
        // table cell: pipes become \|, embedded newlines become
        // a <br> so the row doesn't collapse, and the original
        // newline/CR are stripped.
        private static string escapeMarkdownCell(string sValue)
        {
            if (sValue == null) return "";
            return sValue
                .Replace("|", "\\|")
                .Replace("\r\n", "<br>")
                .Replace("\n", "<br>")
                .Replace("\r", "<br>");
        }

        // importMarkdown: read a GitHub-flavored Markdown file
        // containing one or more pipe-delimited tables, and append
        // every value row into the currently-open recordset.
        //
        // Parse rules, deliberately lenient:
        //   * Lines starting with '|' (after trimming whitespace)
        //     are table rows.
        //   * The first table row is the header.
        //   * The second table row is the separator (---, :---,
        //     ---:, etc.); it is skipped.
        //   * Any subsequent | rows are value rows.
        //   * Header cell names are matched case-insensitively to
        //     columns in the currently-open table. Cells with no
        //     matching column are dropped silently.
        //   * Within a cell, "<br>" (case-insensitive) is converted
        //     back to a single newline, and "\|" back to a literal
        //     pipe. This is the inverse of escapeMarkdownCell.
        //   * If the file contains multiple tables (separated by
        //     blank lines), they all import; later tables append
        //     onto the same target.
        //
        // The current-table must already be open. The bookkeeping
        // columns (added, updated, marked, the primary key) take
        // their DEFAULT values automatically -- the import only
        // sets columns the Markdown header names.
        //
        // Returns the number of rows successfully inserted.
        public int importMarkdown(string sSourcePath)
        {
            if (!hasRecordset()) throw new InvalidOperationException("No recordset open. Use Open-Database first.");
            if (string.IsNullOrEmpty(sSourcePath)) throw new ArgumentException("importMarkdown requires a path.");
            if (!File.Exists(sSourcePath)) throw new FileNotFoundException("Markdown file not found.", sSourcePath);
            if (readOnly) throw new InvalidOperationException("The database is locked (read-only). Toggle the Lock-Database command first.");

            // Read every line. Files are usually small (Markdown
            // tables are rarely millions of rows); ReadAllLines is
            // acceptable.
            string[] aLines;
            try { aLines = File.ReadAllLines(sSourcePath, Encoding.UTF8); }
            catch (Exception oEx) { throw new Exception("Could not read " + sSourcePath + ": " + oEx.Message, oEx); }

            int iInserted = 0;
            List<string> lHeaders = null;
            bool bSawSeparator = false;

            // Get the destination's column set so we can match by
            // name. ADO column-name comparison is case-insensitive.
            List<string> lAllDestFields = getFieldNames();
            HashSet<string> hDestFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string sN in lAllDestFields) hDestFields.Add(sN);

            for (int iLine = 0; iLine < aLines.Length; iLine++)
            {
                string sLine = (aLines[iLine] ?? "").Trim();
                if (sLine.Length == 0)
                {
                    // Blank line ends one table; next | line starts another.
                    lHeaders = null;
                    bSawSeparator = false;
                    continue;
                }
                if (!sLine.StartsWith("|")) continue;   // not a table row

                List<string> lCells = splitMarkdownRow(sLine);

                if (lHeaders == null)
                {
                    // First table row in this group = header.
                    lHeaders = lCells;
                    bSawSeparator = false;
                    continue;
                }
                if (!bSawSeparator)
                {
                    // The separator row is all dashes/colons. Don't
                    // be strict; any cell that is purely [-:\s] is
                    // accepted as separator. If the row doesn't
                    // look like one, treat it as the first value
                    // row (some Markdown dialects omit the separator).
                    bool bAllSep = true;
                    foreach (string sCell in lCells)
                    {
                        string sT = sCell.Trim();
                        if (sT.Length == 0) continue;
                        foreach (char c in sT)
                        {
                            if (c != '-' && c != ':' && c != ' ') { bAllSep = false; break; }
                        }
                        if (!bAllSep) break;
                    }
                    bSawSeparator = true;
                    if (bAllSep) continue;
                    // Otherwise fall through and treat the cells
                    // as a value row.
                }

                // Value row. Build column->value pairs by header.
                Dictionary<string, string> dRow = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int iCell = 0; iCell < lCells.Count && iCell < lHeaders.Count; iCell++)
                {
                    string sCol = lHeaders[iCell].Trim();
                    if (!hDestFields.Contains(sCol)) continue;
                    dRow[sCol] = unescapeMarkdownCell(lCells[iCell]).Trim();
                }
                if (dRow.Count == 0) continue;

                // Insert via ADO AddNew so column defaults fire.
                try
                {
                    oRecordset.AddNew();
                    foreach (KeyValuePair<string, string> oPair in dRow)
                    {
                        try { oRecordset.Fields[oPair.Key].Value = oPair.Value; }
                        catch { /* skip un-settable cells */ }
                    }
                    oRecordset.Update();
                    iInserted++;
                }
                catch
                {
                    // Cancel and continue; one bad row shouldn't
                    // sink the whole import.
                    try { oRecordset.CancelUpdate(); } catch { }
                }
            }

            return iInserted;
        }

        // Split a Markdown table row on unescaped pipe characters.
        // Strips the leading and trailing pipes if present, treats
        // \| as a literal pipe inside a cell.
        private static List<string> splitMarkdownRow(string sLine)
        {
            // Remove surrounding pipes.
            string s = sLine.Trim();
            if (s.StartsWith("|")) s = s.Substring(1);
            if (s.EndsWith("|") && !s.EndsWith("\\|")) s = s.Substring(0, s.Length - 1);

            List<string> lCells = new List<string>();
            StringBuilder oSb = new StringBuilder();
            int iPos = 0;
            while (iPos < s.Length)
            {
                char c = s[iPos];
                if (c == '\\' && iPos + 1 < s.Length && s[iPos + 1] == '|')
                {
                    oSb.Append('|');
                    iPos += 2;
                    continue;
                }
                if (c == '|')
                {
                    lCells.Add(oSb.ToString());
                    oSb.Clear();
                    iPos++;
                    continue;
                }
                oSb.Append(c);
                iPos++;
            }
            lCells.Add(oSb.ToString());
            return lCells;
        }

        // Invert escapeMarkdownCell: <br> back to newline; \| back
        // to |. Case-insensitive on the <br>.
        private static string unescapeMarkdownCell(string sCell)
        {
            if (sCell == null) return "";
            string s = sCell;
            // Replace <br>, <br/>, <br /> (any case).
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"<br\s*/?>", "\n",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            // Note: splitMarkdownRow already handled \| -> | when
            // splitting on pipe boundaries; here we leave \| alone
            // in case any leaked through.
            return s;
        }

        private void exportDelimited(string sDestPath, string sDelim)
        {
            object oBookmark = bookmark;
            List<string> lFields = getFieldNames();
            using (StreamWriter oW = new StreamWriter(sDestPath, false, new UTF8Encoding(true)))
            {
                List<string> lH = new List<string>();
                foreach (string sN in lFields) lH.Add(escapeDelimField(sN, sDelim));
                oW.WriteLine(string.Join(sDelim, lH));

                moveFirst();
                while (!eof)
                {
                    List<string> lV = new List<string>();
                    foreach (string sN in lFields) lV.Add(escapeDelimField(getFieldValue(sN), sDelim));
                    oW.WriteLine(string.Join(sDelim, lV));
                    moveNext();
                }
            }
            if (oBookmark != null) bookmark = oBookmark;
        }

        private static string escapeDelimField(string sValue, string sDelim)
        {
            if (sValue == null) return "";
            bool bNeedsQuote = sValue.Contains(sDelim) || sValue.Contains("\"") || sValue.Contains("\n") || sValue.Contains("\r");
            if (!bNeedsQuote) return sValue;
            return "\"" + sValue.Replace("\"", "\"\"") + "\"";
        }

        private void exportHtml(string sDestPath)
        {
            object oBookmark = bookmark;
            List<string> lFields = getFieldNames();
            using (StreamWriter oW = new StreamWriter(sDestPath, false, new UTF8Encoding(true)))
            {
                oW.WriteLine("<!DOCTYPE html>");
                oW.WriteLine("<html><head><meta charset=\"utf-8\"><title>" + htmlEscape(sCurrentTable) + "</title></head>");
                oW.WriteLine("<body>");
                oW.WriteLine("<h1>" + htmlEscape(sCurrentTable) + "</h1>");
                oW.WriteLine("<table border=\"1\">");
                oW.Write("<thead><tr>");
                foreach (string sN in lFields) oW.Write("<th>" + htmlEscape(sN) + "</th>");
                oW.WriteLine("</tr></thead>");
                oW.WriteLine("<tbody>");
                moveFirst();
                while (!eof)
                {
                    oW.Write("<tr>");
                    foreach (string sN in lFields) oW.Write("<td>" + htmlEscape(getFieldValue(sN)) + "</td>");
                    oW.WriteLine("</tr>");
                    moveNext();
                }
                oW.WriteLine("</tbody></table></body></html>");
            }
            if (oBookmark != null) bookmark = oBookmark;
        }

        private static string htmlEscape(string s)
        {
            if (s == null) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                    .Replace("\"", "&quot;").Replace("'", "&#39;");
        }

        // =====================================================================
        // saveAs: copy the entire database to a new file. For SQLite
        // we use the SQLite ODBC driver's BACKUP support (PRAGMA-based)
        // or fall back to a plain file copy when the database is not
        // currently being written. For the others, the simplest
        // correct approach is a file copy after closing the connection.
        //
        // Edge case: if the destination has a different extension
        // (e.g., copying foo.db to foo.csv), we route to exportData
        // for the current table only. Save-As semantics for "the
        // whole database" only make sense within the same format.
        // =====================================================================
        public void saveAs(string sDestPath)
        {
            if (!isOpen()) throw new InvalidOperationException("No database open.");
            if (string.IsNullOrEmpty(sDestPath)) throw new ArgumentException("saveAs requires a path.");

            string sSrcExt = Path.GetExtension(sFilePath).TrimStart('.').ToLowerInvariant();
            string sDstExt = Path.GetExtension(sDestPath).TrimStart('.').ToLowerInvariant();

            if (sSrcExt != sDstExt)
            {
                // Cross-format save-as falls through to export of
                // current table.
                exportData(sDestPath);
                return;
            }

            // Same-format save-as is a file copy. We close the
            // connection first to release file locks.
            string sOriginalPath = sFilePath;
            string sOriginalTable = sCurrentTable;
            bool bOriginalReadOnly = bReadOnly;
            close();
            try
            {
                File.Copy(sOriginalPath, sDestPath, true);
            }
            catch (Exception oEx)
            {
                // Re-open original on failure.
                openDatabase(sOriginalPath, sOriginalTable, bOriginalReadOnly);
                throw new Exception("Save-As failed: " + oEx.Message, oEx);
            }
            // Re-open the *original* file after the copy. The user is
            // making a backup, not switching to it.
            openDatabase(sOriginalPath, sOriginalTable, bOriginalReadOnly);
        }

        // =====================================================================
        // measureField: per-column statistics. Implements dbDot's
        // longest/shortest/min/max plus standard count/sum/avg.
        // Returns a structured result the CLI and GUI both render.
        // =====================================================================
        public class FieldStatistic
        {
            public string fieldName;
            public string statistic;
            public string value;
            public int recordPosition;  // 1-based; the row where the extreme was found, where applicable
        }

        public FieldStatistic measureField(string sField, string sStatistic)
        {
            if (!hasRecordset()) throw new InvalidOperationException("No recordset open.");
            if (string.IsNullOrEmpty(sField)) throw new ArgumentException("measureField requires a field name.");

            string sStat = (sStatistic ?? "count").ToLowerInvariant();
            FieldStatistic oResult = new FieldStatistic { fieldName = sField, statistic = sStat };

            object oBookmark = bookmark;
            try
            {
                moveFirst();
                if (eof) { oResult.value = "(empty)"; return oResult; }

                switch (sStat)
                {
                    case "count":
                        oResult.value = recordCount.ToString();
                        return oResult;
                    case "longest":
                    case "shortest":
                        measureLength(sField, sStat == "longest", oResult);
                        return oResult;
                    case "min":
                    case "max":
                        measureMinMax(sField, sStat == "max", oResult);
                        return oResult;
                    case "sum":
                    case "avg":
                    case "average":
                        measureSumAvg(sField, sStat == "sum", oResult);
                        return oResult;
                    default:
                        throw new ArgumentException("Unknown statistic: " + sStat
                            + ". Use count, longest, shortest, min, max, sum, or avg.");
                }
            }
            finally
            {
                if (oBookmark != null) bookmark = oBookmark;
            }
        }

        private void measureLength(string sField, bool bLongest, FieldStatistic oResult)
        {
            int iExtreme = bLongest ? -1 : int.MaxValue;
            string sExtreme = "";
            int iPos = 1;
            int iAt = 1;
            moveFirst();
            while (!eof)
            {
                string sValue = getFieldValue(sField);
                int iLen = sValue.Length;
                if ((bLongest && iLen > iExtreme) || (!bLongest && iLen < iExtreme))
                {
                    iExtreme = iLen; sExtreme = sValue; iAt = iPos;
                }
                iPos++;
                moveNext();
            }
            oResult.value = string.Format("{0} chars: {1}", iExtreme, sExtreme);
            oResult.recordPosition = iAt;
        }

        private void measureMinMax(string sField, bool bMax, FieldStatistic oResult)
        {
            // Use ADODB to compare via the field's native type. We
            // collect all values and let .NET's IComparable do the
            // work, since string comparison and numeric comparison
            // disagree (strings sort "100" before "9"). The recordset
            // gives us the typed value through Field.Value.
            object oExtreme = null;
            int iAt = 1;
            int iPos = 1;
            moveFirst();
            while (!eof)
            {
                object oValue = null;
                try { oValue = oRecordset.Fields[sField].Value; } catch { }
                if (oValue != null && !(oValue is DBNull))
                {
                    if (oExtreme == null) { oExtreme = oValue; iAt = iPos; }
                    else
                    {
                        IComparable oCmp = oExtreme as IComparable;
                        if (oCmp != null)
                        {
                            int iCmp = 0;
                            try { iCmp = oCmp.CompareTo(oValue); } catch { }
                            if ((bMax && iCmp < 0) || (!bMax && iCmp > 0))
                            {
                                oExtreme = oValue; iAt = iPos;
                            }
                        }
                    }
                }
                iPos++;
                moveNext();
            }
            oResult.value = (oExtreme == null) ? "(no values)" : oExtreme.ToString();
            oResult.recordPosition = iAt;
        }

        private void measureSumAvg(string sField, bool bSum, FieldStatistic oResult)
        {
            double dSum = 0.0;
            int iCount = 0;
            moveFirst();
            while (!eof)
            {
                object oValue = null;
                try { oValue = oRecordset.Fields[sField].Value; } catch { }
                if (oValue != null && !(oValue is DBNull))
                {
                    double dV;
                    if (double.TryParse(oValue.ToString(), out dV))
                    {
                        dSum += dV; iCount++;
                    }
                }
                moveNext();
            }
            if (iCount == 0) { oResult.value = "(no numeric values)"; return; }
            double dResult = bSum ? dSum : (dSum / iCount);
            oResult.value = dResult.ToString("G");
        }

        // =====================================================================
        // COM helpers. createComObject is the late-bound creation
        // entry point; releaseCom releases the underlying RCW so we
        // don't hold COM objects past their natural lifetime.
        //
        // releaseCom takes a plain object (not ref); caller assigns
        // null after the call. We avoid ref-with-dynamic because
        // late-bound dispatch and ref parameters interact poorly:
        // the static type 'dynamic' doesn't satisfy a 'ref T' generic
        // constraint cleanly under C# overload resolution.
        // =====================================================================
        private static dynamic createComObject(string sProgId)
        {
            Type oType = Type.GetTypeFromProgID(sProgId);
            if (oType == null)
                throw new Exception("COM ProgID not registered: " + sProgId);
            return Activator.CreateInstance(oType);
        }

        private static void releaseCom(object oTarget)
        {
            if (oTarget == null) return;
            try
            {
                if (Marshal.IsComObject(oTarget))
                    Marshal.FinalReleaseComObject(oTarget);
            }
            catch { }
        }
    }

    // INSERTION POINT FOR SUPPORT CLASSES, FORM, CLI, AND PROGRAM

    // =====================================================================
    // KeyMap: central Dictionary<Keys, ToolStripMenuItem> dispatch table.
    //
    // Inspired by EdSharp's hashKey pattern. Every menu item registers
    // its preferred Keys value with KeyMap. The form's ProcessCmdKey
    // override consults this dictionary to dispatch a keystroke to the
    // matching menu item, bypassing ToolStripMenuItem.ShortcutKeys
    // validation. This means we can bind keystrokes the framework
    // otherwise rejects (plain Enter, plain Insert, plain Delete, Tab,
    // Escape, Alt+F4, Backspace, Space) while still showing them in
    // the menu via ShortcutKeyDisplayString.
    //
    // db.ini's [Keys] section can override any binding by command name.
    // Conflicts and parse-failures collect into lConflicts and surface
    // in a single startup dialog.
    // =====================================================================
    public static class KeyMap
    {
        public static Dictionary<Keys, ToolStripMenuItem> dKeyToMenu =
            new Dictionary<Keys, ToolStripMenuItem>();
        public static Dictionary<string, Keys> dCommandToKey =
            new Dictionary<string, Keys>();
        public static Dictionary<ToolStripMenuItem, string> dMenuToCommand =
            new Dictionary<ToolStripMenuItem, string>();
        public static List<string> lConflicts = new List<string>();
        public static bool bTraceMode = false;

        public static void register(Keys oKey, ToolStripMenuItem oItem, string sCommand)
        {
            if (oKey == Keys.None)
            {
                dMenuToCommand[oItem] = sCommand;
                return;
            }
            if (dKeyToMenu.ContainsKey(oKey))
            {
                string sExisting = dMenuToCommand.ContainsKey(dKeyToMenu[oKey]) ? dMenuToCommand[dKeyToMenu[oKey]] : "(unknown)";
                lConflicts.Add(string.Format("{0} already bound to '{1}'; cannot rebind to '{2}'",
                    friendlyKey(oKey), sExisting, sCommand));
                return;
            }
            dKeyToMenu[oKey] = oItem;
            dCommandToKey[sCommand] = oKey;
            dMenuToCommand[oItem] = sCommand;
            oItem.ShortcutKeyDisplayString = friendlyKey(oKey);
            string sBase = oItem.Text.Replace("&", "");
            if (sBase.EndsWith("...")) sBase = sBase.Substring(0, sBase.Length - 3).TrimEnd();
            oItem.AccessibleName = sBase + "   " + friendlyKey(oKey);
        }

        // registerDisplayOnly: show a chord in the menu UI without
        // routing it through the form-level dispatcher. Use this for
        // hotkeys that are dispatched locally by a specific control
        // (the data grid's KeyDown handler in DbDuo's case), so the
        // chord only fires when that control has focus. The menu
        // text and AccessibleName carry the chord exactly as
        // register() would, so JAWS still announces "Shift+F" along
        // with the menu item.
        //
        // This is the pattern FileDir uses for its Shift+D /
        // Shift+L / Shift+S shortcuts: the menu shows the chord,
        // but the listbox's own KeyDown handler is what actually
        // recognizes the keystroke. Form-level ProcessCmdKey never
        // sees the chord, so capital letters typed in text boxes
        // and dialogs are not intercepted.
        public static void registerDisplayOnly(Keys oKey, ToolStripMenuItem oItem, string sCommand)
        {
            dMenuToCommand[oItem] = sCommand;
            if (oKey == Keys.None) return;
            // Record the command->key mapping so help screens and
            // the chord-summary table can find this binding, but
            // DON'T add to dKeyToMenu (form-level dispatch table).
            dCommandToKey[sCommand] = oKey;
            oItem.ShortcutKeyDisplayString = friendlyKey(oKey);
            string sBase = oItem.Text.Replace("&", "");
            if (sBase.EndsWith("...")) sBase = sBase.Substring(0, sBase.Length - 3).TrimEnd();
            oItem.AccessibleName = sBase + "   " + friendlyKey(oKey);
        }

        // registerAlias: bind a SECONDARY key to a command that already
        // has a primary binding. Use this when a command has more than
        // one natural keystroke (e.g., Get-Property naturally responds
        // to both Alt+Enter as the Windows-standard "properties" key
        // and Shift+F6 as the EdSharp "go to contents" key).
        //
        // The primary binding keeps its menu shortcut display; the
        // alias is only routed through tryDispatch and does not appear
        // in the menu.
        public static void registerAlias(Keys oKey, ToolStripMenuItem oItem)
        {
            if (oKey == Keys.None) return;
            if (dKeyToMenu.ContainsKey(oKey))
            {
                string sExisting = dMenuToCommand.ContainsKey(dKeyToMenu[oKey]) ? dMenuToCommand[dKeyToMenu[oKey]] : "(unknown)";
                lConflicts.Add(string.Format("{0} already bound to '{1}'; cannot register as alias",
                    friendlyKey(oKey), sExisting));
                return;
            }
            dKeyToMenu[oKey] = oItem;
        }

        public static bool overrideKey(string sCommand, string sKeyText)
        {
            ToolStripMenuItem oItem = null;
            foreach (KeyValuePair<ToolStripMenuItem, string> kv in dMenuToCommand)
            {
                if (kv.Value == sCommand) { oItem = kv.Key; break; }
            }
            if (oItem == null) return false;

            if (dCommandToKey.ContainsKey(sCommand))
            {
                Keys oOld = dCommandToKey[sCommand];
                dKeyToMenu.Remove(oOld);
                dCommandToKey.Remove(sCommand);
            }

            string sTrim = sKeyText.Trim();
            if (sTrim.Length == 0 || sTrim.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                oItem.ShortcutKeyDisplayString = "";
                return true;
            }

            Keys oKey;
            try
            {
                oKey = (Keys)System.ComponentModel.TypeDescriptor
                    .GetConverter(typeof(Keys))
                    .ConvertFromString(sTrim);
            }
            catch
            {
                lConflicts.Add(string.Format("Could not parse key '{0}' for '{1}'", sKeyText, sCommand));
                return false;
            }

            if (dKeyToMenu.ContainsKey(oKey))
            {
                string sExisting = dMenuToCommand[dKeyToMenu[oKey]];
                lConflicts.Add(string.Format("{0} already bound to '{1}'; cannot rebind to '{2}'",
                    friendlyKey(oKey), sExisting, sCommand));
                return false;
            }
            dKeyToMenu[oKey] = oItem;
            dCommandToKey[sCommand] = oKey;
            oItem.ShortcutKeyDisplayString = friendlyKey(oKey);
            string sBase = oItem.Text.Replace("&", "");
            if (sBase.EndsWith("...")) sBase = sBase.Substring(0, sBase.Length - 3).TrimEnd();
            oItem.AccessibleName = sBase + "   " + friendlyKey(oKey);
            return true;
        }

        public static bool tryDispatch(Keys oKey, IWin32Window oOwner)
        {
            if (!dKeyToMenu.ContainsKey(oKey)) return false;
            ToolStripMenuItem oItem = dKeyToMenu[oKey];
            if (!oItem.Enabled)
            {
                // The hotkey is bound but the menu item is currently
                // disabled (typical reason: no database open, or no
                // table selected). Returning false here would let the
                // key fall through to the focused control, often the
                // DataGridView, which would silently ignore it. Instead
                // announce why the key did nothing so the screen-reader
                // user knows to take an action like opening a database
                // first. We still return true so the keystroke is
                // consumed and the grid never sees it.
                string sCmd2 = dMenuToCommand.ContainsKey(oItem) ? dMenuToCommand[oItem] : oItem.Text.Replace("&", "");
                LiveRegion.say(sCmd2 + " is unavailable right now (open a database file or select a table first)");
                return true;
            }
            if (bTraceMode)
            {
                string sCmd = dMenuToCommand.ContainsKey(oItem) ? dMenuToCommand[oItem] : oItem.Text.Replace("&", "");
                MessageBox.Show(oOwner,
                    string.Format("Trace-Command:\n\nKey: {0}\nCommand: {1}", friendlyKey(oKey), sCmd),
                    "Trace-Command", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                oItem.PerformClick();
            }
            return true;
        }

        // Format a Keys value as a hotkey display string for menus,
        // help screens, and trace messages.
        //
        // Convention adopted by DbDuo:
        //   - Modifiers in alphabetical order (Alt, then Control, then
        //     Shift), each followed by '+'.
        //   - The base key follows. Letter keys are capitalized whether
        //     or not Shift is a modifier (the on-screen label is the
        //     symbol on the physical keycap, not the typed character).
        //   - Function keys, arrow keys, digit keys, and named symbol
        //     keys are spelled out (F5, Up, 1, Backslash).
        //   - Examples: "Control+S", "Control+Shift+D", "Alt+Control+D",
        //     "Alt+F4", "F2", "Enter", "Ctrl+`" appears as
        //     "Control+Backquote".
        //
        // Unlike Keys.ToString(), which produces "S, Control" (letter
        // first, modifiers second, comma-separated), this method
        // produces the human-readable form with modifiers leading.
        public static string friendlyKey(Keys oKey)
        {
            Keys oBase = oKey & ~Keys.Modifiers;
            string sBase = baseKeyLabel(oBase);
            if (sBase.Length == 0) return "";

            StringBuilder oOut = new StringBuilder();
            if ((oKey & Keys.Alt)     == Keys.Alt)     oOut.Append("Alt+");
            if ((oKey & Keys.Control) == Keys.Control) oOut.Append("Control+");
            if ((oKey & Keys.Shift)   == Keys.Shift)   oOut.Append("Shift+");
            oOut.Append(sBase);
            return oOut.ToString();
        }

        // Render a single Keys value (without modifiers) as its display
        // label. Letter keys A-Z are returned uppercase. Digit keys
        // D0-D9 become "0"-"9". Named symbol keys map to common
        // English names. Function keys F1-F24, arrow keys, Enter, etc.
        // pass through.
        private static string baseKeyLabel(Keys oBase)
        {
            // Empty / pure-modifier
            if (oBase == Keys.None) return "";

            // Letter keys: A-Z always shown uppercase
            if (oBase >= Keys.A && oBase <= Keys.Z) return oBase.ToString();

            // Top-row digit keys: D0-D9
            if (oBase >= Keys.D0 && oBase <= Keys.D9)
                return ((int)(oBase - Keys.D0)).ToString();

            // Numpad digits
            if (oBase >= Keys.NumPad0 && oBase <= Keys.NumPad9)
                return "NumPad" + ((int)(oBase - Keys.NumPad0));

            // Named symbol keys: JAWS-canonical names (from JAWS
            // Default.JKM). This is the vocabulary the user is
            // already trained on through their screen reader.
            // Modifiers are kept in alpha order (Alt, Control,
            // Shift) by the caller; only base-key names are mapped
            // here.
            switch (oBase)
            {
                case Keys.OemQuotes:        return "Apostrophe";
                case Keys.OemSemicolon:     return "Semicolon";
                case Keys.OemQuestion:      return "Slash";
                case Keys.OemPeriod:        return "Period";
                case Keys.Oemcomma:         return "Comma";
                case Keys.OemMinus:         return "Minus";
                case Keys.Oemplus:          return "Equals";
                case Keys.OemOpenBrackets:  return "LeftBracket";
                case Keys.OemCloseBrackets: return "RightBracket";
                case Keys.OemPipe:          return "Backslash";  // also Oem5
                case Keys.Oemtilde:         return "GraveAccent";
                case Keys.Back:             return "Backspace";
                case Keys.Return:           return "Enter";
                case Keys.Escape:           return "Escape";
                case Keys.Space:            return "Space";
                case Keys.Tab:              return "Tab";
                case Keys.Up:               return "UpArrow";
                case Keys.Down:             return "DownArrow";
                case Keys.Left:             return "LeftArrow";
                case Keys.Right:            return "RightArrow";
                case Keys.PageUp:           return "PageUp";
                case Keys.PageDown:         return "PageDown";
                case Keys.Home:             return "Home";
                case Keys.End:              return "End";
                case Keys.Insert:           return "Insert";
                case Keys.Delete:           return "Delete";
                case Keys.PrintScreen:      return "PrintScreen";
                case Keys.CapsLock:         return "CapsLock";
                case Keys.NumLock:          return "NumLock";
                case Keys.Scroll:           return "ScrollLock";
            }

            // Function keys and anything else
            return oBase.ToString();
        }

        public static List<string[]> getAllCommands()
        {
            List<string[]> lOut = new List<string[]>();
            foreach (KeyValuePair<ToolStripMenuItem, string> kv in dMenuToCommand)
            {
                string sCmd = kv.Value;
                string sKey = dCommandToKey.ContainsKey(sCmd) ? friendlyKey(dCommandToKey[sCmd]) : "(unbound)";
                bool bEnabled = kv.Key.Enabled;
                lOut.Add(new string[] { sCmd, sKey, bEnabled ? "" : " (disabled)" });
            }
            lOut.Sort((a, b) => string.Compare(a[0], b[0], StringComparison.OrdinalIgnoreCase));
            return lOut;
        }
    }

    // =====================================================================
    // HelpDialog: reusable read-only multiline TextBox dialog. Used
    // for showing schema text, help contents, integrity check output,
    // and other long read-only content.
    // =====================================================================
    public static class HelpDialog
    {
        public static void show(IWin32Window oOwner, string sTitle, string sText)
        {
            using (Form oDlg = new Form())
            {
                oDlg.Text = sTitle;
                oDlg.AccessibleName = sTitle;
                oDlg.StartPosition = FormStartPosition.CenterParent;
                oDlg.ClientSize = new Size(720, 540);
                oDlg.MinimumSize = new Size(400, 300);
                oDlg.FormBorderStyle = FormBorderStyle.Sizable;
                oDlg.MaximizeBox = true;
                oDlg.MinimizeBox = false;
                oDlg.ShowInTaskbar = false;
                oDlg.KeyPreview = true;

                TextBox tb = new TextBox();
                tb.Multiline = true;
                tb.ReadOnly = true;
                tb.ScrollBars = ScrollBars.Vertical;
                tb.WordWrap = true;
                tb.Font = new Font(FontFamily.GenericMonospace, 10f);
                tb.Text = sText;
                tb.AccessibleName = "Help text";
                tb.TabIndex = 0;
                tb.SelectionStart = 0;
                tb.SelectionLength = 0;
                tb.Size = new Size(oDlg.ClientSize.Width - 20, oDlg.ClientSize.Height - 50);
                tb.Location = new Point(10, 10);
                tb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

                Button btnClose = new Button();
                btnClose.Text = "&OK";
                btnClose.AccessibleName = "OK";
                btnClose.DialogResult = DialogResult.OK;
                btnClose.Size = new Size(90, 28);
                btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                btnClose.Location = new Point(oDlg.ClientSize.Width - 100, oDlg.ClientSize.Height - 38);
                btnClose.UseVisualStyleBackColor = true;
                btnClose.TabIndex = 1;

                oDlg.Controls.Add(tb);
                oDlg.Controls.Add(btnClose);
                oDlg.AcceptButton = btnClose;
                oDlg.CancelButton = btnClose;
                oDlg.ActiveControl = tb;

                oDlg.ShowDialog(oOwner);
            }
        }
    }

    // =====================================================================
    // LbcDialog: "Layout by Code" dialog builder.
    //
    // A reusable WinForms dialog that callers build by adding one
    // control at a time. Each addX() method appends a labeled row
    // to a vertical stack, returns the inner editing control, and
    // wires its accessible name from the label so screen readers
    // announce the field correctly when the user tabs into it.
    //
    // After adding all controls, the caller invokes runOkCancel()
    // which appends an OK/Cancel button band, shows the dialog
    // modally, and returns true on OK. The caller then reads the
    // final values directly from the control references it kept.
    //
    // Borrowed pattern from Jamal Mazrui's Layout by Code system
    // (LbC), which exists in C#, Python, and AutoIt versions and
    // codifies the practice of laying out screen-reader-friendly
    // dialogs by composing simple "add this control" calls in
    // sequence rather than using a designer file.
    //
    // Conventions:
    //   - Each control gets a Label above it (not beside it). This
    //     gives a uniform vertical rhythm and ensures the label is
    //     in the screen-reader's reading order before the control.
    //   - The label text passes through unchanged (caller may
    //     include '&' for mnemonic letters); the control's
    //     AccessibleName is set to the label with '&' and ':'
    //     stripped, so JAWS announces a clean form-field label.
    //   - TabIndex is assigned in add order. Labels are skipped
    //     in tab traversal automatically (WinForms Labels aren't
    //     focusable).
    //   - The dialog auto-sizes its width to the widest control,
    //     up to a cap, and scrolls vertically when contents
    //     exceed the cap.
    //   - runOkCancel() makes OK the default (AcceptButton, Enter
    //     activates) and Cancel the dismiss (CancelButton, Escape
    //     dismisses).
    //
    // Typical usage:
    //
    //   LbcDialog oDlg = new LbcDialog("Configuration", this);
    //   TextBox   tbMode = oDlg.addTextLine("UI mode", "both");
    //   CheckBox  cbBeep = oDlg.addCheckBox("Beep on errors", true);
    //   TextBox   tbNote = oDlg.addTextMemo("Startup note", "");
    //   if (oDlg.runOkCancel())
    //   {
    //       string sMode  = tbMode.Text;
    //       bool   bBeep  = cbBeep.Checked;
    //       string sNote  = tbNote.Text;
    //       // ... persist or apply
    //   }
    // =====================================================================
    public class LbcDialog : IDisposable
    {
        // Layout constants. Sized for screen-reader users who often
        // run at 125-150% display scaling -- generous padding makes
        // the dialog readable without crowding labels into controls.
        private const int DefaultDialogWidth  = 520;
        private const int DefaultMaxHeight    = 600;
        private const int DefaultPadding      = 12;
        private const int DefaultRowGap       = 6;   // vertical gap between rows
        private const int DefaultLabelHeight  = 18;
        private const int DefaultLineHeight   = 24;  // single-line TextBox
        private const int DefaultMemoHeight   = 96;  // multi-line TextBox
        private const int DefaultListHeight   = 100; // ListBox
        private const int DefaultButtonWidth  = 90;
        private const int DefaultButtonHeight = 28;

        private Form oForm;
        private IWin32Window oOwner;
        private FlowLayoutPanel oStack;
        private int iTabIndex;
        private Control oFirstFocusable;

        public LbcDialog(string sTitle, IWin32Window oOwnerWindow)
        {
            oOwner = oOwnerWindow;
            oForm = new Form();
            oForm.Text = sTitle ?? "";
            oForm.AccessibleName = sTitle ?? "";
            oForm.StartPosition = FormStartPosition.CenterParent;
            oForm.FormBorderStyle = FormBorderStyle.Sizable;
            oForm.MaximizeBox = false;
            oForm.MinimizeBox = false;
            oForm.ShowInTaskbar = false;
            oForm.KeyPreview = true;
            oForm.MinimumSize = new Size(360, 200);
            oForm.ClientSize = new Size(DefaultDialogWidth, 200); // height adjusts later

            // Vertical FlowLayoutPanel as the form's main container.
            // AutoScroll on so dialogs with many fields scroll instead
            // of overflowing the screen.
            oStack = new FlowLayoutPanel();
            oStack.FlowDirection = FlowDirection.TopDown;
            oStack.WrapContents = false;
            oStack.AutoScroll = true;
            oStack.Dock = DockStyle.Fill;
            oStack.Padding = new Padding(DefaultPadding);
            oForm.Controls.Add(oStack);

            iTabIndex = 0;
            oFirstFocusable = null;
        }

        // ------- Add helpers -------
        //
        // Each adds one labeled control to the vertical stack and
        // returns the inner control so the caller can stash a
        // reference, attach event handlers, or query values later.

        // addLabel: standalone explanatory text. No editable control.
        // Useful for section headers or paragraph-style hints.
        public Label addLabel(string sText)
        {
            Label lbl = new Label();
            lbl.Text = sText ?? "";
            lbl.AutoSize = false;
            lbl.Size = new Size(innerWidth(), DefaultLabelHeight);
            lbl.Margin = new Padding(0, 0, 0, DefaultRowGap);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            oStack.Controls.Add(lbl);
            return lbl;
        }

        // addTextLine: single-line text input. Returns the TextBox.
        public TextBox addTextLine(string sLabel, string sValue)
        {
            addFieldLabel(sLabel);
            TextBox tb = new TextBox();
            tb.AccessibleName = cleanLabel(sLabel);
            tb.Text = sValue ?? "";
            tb.Size = new Size(innerWidth(), DefaultLineHeight);
            tb.TabIndex = iTabIndex++;
            tb.Margin = new Padding(0, 0, 0, DefaultRowGap);
            oStack.Controls.Add(tb);
            if (oFirstFocusable == null) oFirstFocusable = tb;
            return tb;
        }

        // addTextMemo: multi-line text input. Returns the TextBox.
        // Use this for free-form notes, descriptions, anything that
        // could span multiple lines.
        public TextBox addTextMemo(string sLabel, string sValue)
        {
            addFieldLabel(sLabel);
            TextBox tb = new TextBox();
            tb.AccessibleName = cleanLabel(sLabel);
            tb.Text = sValue ?? "";
            tb.Multiline = true;
            tb.AcceptsReturn = true;
            tb.AcceptsTab = false;  // Tab moves focus, doesn't insert
            tb.ScrollBars = ScrollBars.Vertical;
            tb.WordWrap = true;
            tb.Size = new Size(innerWidth(), DefaultMemoHeight);
            tb.TabIndex = iTabIndex++;
            tb.Margin = new Padding(0, 0, 0, DefaultRowGap);
            oStack.Controls.Add(tb);
            if (oFirstFocusable == null) oFirstFocusable = tb;
            return tb;
        }

        // addCheckBox: boolean toggle. Returns the CheckBox. The
        // label is part of the checkbox itself (WinForms convention),
        // so no separate Label is emitted.
        public CheckBox addCheckBox(string sLabel, bool bValue)
        {
            CheckBox cb = new CheckBox();
            cb.Text = sLabel ?? "";
            cb.AccessibleName = cleanLabel(sLabel);
            cb.Checked = bValue;
            cb.AutoSize = false;
            cb.Size = new Size(innerWidth(), DefaultLineHeight);
            cb.TabIndex = iTabIndex++;
            cb.Margin = new Padding(0, 0, 0, DefaultRowGap);
            oStack.Controls.Add(cb);
            if (oFirstFocusable == null) oFirstFocusable = cb;
            return cb;
        }

        // addListBox: pick-one list. Returns the ListBox. The names
        // argument supplies the visible items; sSelected is the
        // initial selection (matched by string).
        public ListBox addListBox(string sLabel, IList<string> lNames, string sSelected)
        {
            addFieldLabel(sLabel);
            ListBox lb = new ListBox();
            lb.AccessibleName = cleanLabel(sLabel);
            lb.Size = new Size(innerWidth(), DefaultListHeight);
            lb.TabIndex = iTabIndex++;
            lb.Margin = new Padding(0, 0, 0, DefaultRowGap);
            if (lNames != null)
            {
                foreach (string sN in lNames) lb.Items.Add(sN);
                int iSel = -1;
                if (!string.IsNullOrEmpty(sSelected))
                {
                    for (int i = 0; i < lb.Items.Count; i++)
                    {
                        if (string.Equals(lb.Items[i].ToString(), sSelected,
                                StringComparison.OrdinalIgnoreCase))
                        { iSel = i; break; }
                    }
                }
                if (iSel < 0 && lb.Items.Count > 0) iSel = 0;
                if (iSel >= 0) lb.SelectedIndex = iSel;
            }
            oStack.Controls.Add(lb);
            if (oFirstFocusable == null) oFirstFocusable = lb;
            return lb;
        }

        // addComboBox: a drop-down version of pick-one. Returns the
        // ComboBox. More compact than a ListBox; appropriate when
        // the choices are well known and few. DropDownStyle is set
        // to DropDownList so the user can only pick from the list.
        public ComboBox addComboBox(string sLabel, IList<string> lNames, string sSelected)
        {
            addFieldLabel(sLabel);
            ComboBox cb = new ComboBox();
            cb.AccessibleName = cleanLabel(sLabel);
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.Size = new Size(innerWidth(), DefaultLineHeight);
            cb.TabIndex = iTabIndex++;
            cb.Margin = new Padding(0, 0, 0, DefaultRowGap);
            if (lNames != null)
            {
                foreach (string sN in lNames) cb.Items.Add(sN);
                int iSel = -1;
                if (!string.IsNullOrEmpty(sSelected))
                {
                    for (int i = 0; i < cb.Items.Count; i++)
                    {
                        if (string.Equals(cb.Items[i].ToString(), sSelected,
                                StringComparison.OrdinalIgnoreCase))
                        { iSel = i; break; }
                    }
                }
                if (iSel < 0 && cb.Items.Count > 0) iSel = 0;
                if (iSel >= 0) cb.SelectedIndex = iSel;
            }
            oStack.Controls.Add(cb);
            if (oFirstFocusable == null) oFirstFocusable = cb;
            return cb;
        }

        // addRadioButton: one option in a radio-button group. The
        // first call after a non-RadioButton control starts a new
        // group automatically (WinForms convention: consecutive
        // RadioButtons in the same container form one group). Set
        // bChecked on whichever you want pre-selected.
        public RadioButton addRadioButton(string sLabel, bool bChecked)
        {
            RadioButton rb = new RadioButton();
            rb.Text = sLabel ?? "";
            rb.AccessibleName = cleanLabel(sLabel);
            rb.Checked = bChecked;
            rb.AutoSize = false;
            rb.Size = new Size(innerWidth(), DefaultLineHeight);
            rb.TabIndex = iTabIndex++;
            rb.Margin = new Padding(0, 0, 0, DefaultRowGap);
            oStack.Controls.Add(rb);
            if (oFirstFocusable == null) oFirstFocusable = rb;
            return rb;
        }

        // addSeparator: a thin horizontal divider, for visually
        // grouping related fields. Carries no accessible content.
        public void addSeparator()
        {
            Label sep = new Label();
            sep.Size = new Size(innerWidth(), 2);
            sep.BorderStyle = BorderStyle.Fixed3D;
            sep.Margin = new Padding(0, DefaultRowGap, 0, DefaultRowGap);
            oStack.Controls.Add(sep);
        }

        // ------- Finish and show -------

        // runOkCancel: add an OK/Cancel button band at the bottom,
        // show the dialog modally, and return true if the user
        // pressed OK (or Enter on a control whose AcceptButton
        // routes to OK), false on Cancel/Escape/close.
        public bool runOkCancel()
        {
            return runWithButtons(new string[] { "OK", "Cancel" }) == "OK";
        }

        // runWithButtons: more flexible -- specify the button labels
        // explicitly. The first label is the AcceptButton (Enter).
        // The last label is the CancelButton (Escape) IF its label
        // matches "Cancel" or "Close"; otherwise no CancelButton
        // is set (Escape does nothing). Returns the label of the
        // button the user pressed, or "" on Escape/close-box.
        public string runWithButtons(string[] aButtonLabels)
        {
            // Layout: a single-row FlowLayoutPanel at the bottom
            // of the form (docked Bottom), independent of the
            // main stack. This way it stays put when the stack
            // scrolls.
            FlowLayoutPanel oButtonRow = new FlowLayoutPanel();
            oButtonRow.FlowDirection = FlowDirection.RightToLeft;
            oButtonRow.AutoSize = false;
            oButtonRow.Dock = DockStyle.Bottom;
            oButtonRow.Height = DefaultButtonHeight + DefaultPadding * 2;
            oButtonRow.Padding = new Padding(DefaultPadding);

            string sResult = "";
            Button oAcceptBtn = null;
            Button oCancelBtn = null;

            // We add buttons right-to-left so the visual order
            // reads left-to-right as given (first label leftmost).
            // RightToLeft FlowDirection puts the first-added at
            // the right; we want first-given at the left, so add
            // in reverse.
            for (int i = aButtonLabels.Length - 1; i >= 0; i--)
            {
                string sLabel = aButtonLabels[i] ?? "";
                Button btn = new Button();
                btn.Text = "&" + sLabel.Replace("&", "");
                btn.AccessibleName = sLabel.Replace("&", "");
                btn.Size = new Size(DefaultButtonWidth, DefaultButtonHeight);
                btn.TabIndex = iTabIndex++;
                btn.Margin = new Padding(DefaultRowGap, 0, 0, 0);
                btn.UseVisualStyleBackColor = true;

                // Capture label-of-this-button by value for the closure.
                string sCaptured = sLabel.Replace("&", "");
                btn.Click += delegate(object o, EventArgs e) {
                    sResult = sCaptured;
                    oForm.DialogResult = DialogResult.OK;
                    oForm.Close();
                };
                oButtonRow.Controls.Add(btn);
                if (i == 0) oAcceptBtn = btn;  // first-given = leftmost = default
                if (string.Equals(sCaptured, "Cancel", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(sCaptured, "Close", StringComparison.OrdinalIgnoreCase))
                    oCancelBtn = btn;
            }
            oForm.Controls.Add(oButtonRow);
            if (oAcceptBtn != null) oForm.AcceptButton = oAcceptBtn;
            if (oCancelBtn != null) oForm.CancelButton = oCancelBtn;

            // Size the form to fit its content, capped at MaxHeight.
            int iContentHeight = computeStackHeight();
            int iTotalHeight = iContentHeight + oButtonRow.Height + 8;
            if (iTotalHeight > DefaultMaxHeight) iTotalHeight = DefaultMaxHeight;
            if (iTotalHeight < 160) iTotalHeight = 160;
            oForm.ClientSize = new Size(DefaultDialogWidth, iTotalHeight);

            if (oFirstFocusable != null) oForm.ActiveControl = oFirstFocusable;
            oForm.ShowDialog(oOwner);
            return sResult;
        }

        // Owning form / outer access for callers who need to tweak
        // something the high-level API doesn't expose.
        public Form form { get { return oForm; } }

        public void Dispose()
        {
            if (oForm != null) { oForm.Dispose(); oForm = null; }
        }

        // ------- Internal helpers (kept private) -------

        // The horizontal space inside the stack panel for a control,
        // accounting for padding and a scrollbar reservation.
        private int innerWidth()
        {
            return DefaultDialogWidth - DefaultPadding * 2 - 24;
        }

        // Strip '&' mnemonic markers and trailing ':' from a label
        // before using it as an AccessibleName, so screen readers
        // announce the clean text.
        private string cleanLabel(string sLabel)
        {
            if (string.IsNullOrEmpty(sLabel)) return "";
            string s = sLabel.Replace("&", "");
            if (s.EndsWith(":")) s = s.Substring(0, s.Length - 1);
            return s.Trim();
        }

        // Add a small Label above a field control. Reused by every
        // add method that wants a separate caption.
        private void addFieldLabel(string sText)
        {
            if (string.IsNullOrEmpty(sText)) return;
            Label lbl = new Label();
            lbl.Text = sText;
            lbl.AutoSize = false;
            lbl.Size = new Size(innerWidth(), DefaultLabelHeight);
            lbl.Margin = new Padding(0, 0, 0, 0);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            oStack.Controls.Add(lbl);
        }

        // Sum the heights of all controls in the stack, plus their
        // margins. Used to choose an initial dialog height that
        // matches the content (capped at MaxHeight).
        private int computeStackHeight()
        {
            int iTotal = oStack.Padding.Vertical;
            foreach (Control c in oStack.Controls)
                iTotal += c.Height + c.Margin.Vertical;
            return iTotal + 8;
        }
    }

    // =====================================================================
    // RecordEditDialog: dynamic per-column field editor. Builds a
    // label + text box per column from a list of column names. The
    // caller passes initial values and an editable-flags list (false
    // marks a column read-only -- typically for primary key columns).
    // OK populates dValues with the final field values.
    // =====================================================================
    public class RecordEditDialog : Form
    {
        public Dictionary<string, string> dValues = new Dictionary<string, string>();
        private List<TextBox> lTextBoxes = new List<TextBox>();
        private List<string> lColumnNames = new List<string>();

        public RecordEditDialog(string sTitle, List<string> lColumns, Dictionary<string, string> dInitial, List<bool> lEditable)
        {
            this.Text = sTitle;
            this.AccessibleName = sTitle;
            this.AccessibleDescription = "";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;

            int iY = 12;
            int iLabelWidth = 130;
            int iTextWidth = 280;
            int iRowHeight = 32;
            int iWidth = iLabelWidth + iTextWidth + 40;

            for (int i = 0; i < lColumns.Count; i++)
            {
                string sCol = lColumns[i];
                lColumnNames.Add(sCol);

                Label lbl = new Label();
                // No '&' mnemonic on dynamic field labels: with N fields, leading-letter
                // mnemonics collide and Alt-letter cycles unpredictably between them and
                // OK/Cancel. The TextBox's AccessibleName carries the column name, so
                // JAWS and NVDA still announce the field correctly on focus.
                lbl.Text = sCol + ":";
                lbl.Location = new Point(12, iY + 3);
                lbl.Size = new Size(iLabelWidth, 20);
                lbl.TextAlign = ContentAlignment.MiddleLeft;
                this.Controls.Add(lbl);

                TextBox tb = new TextBox();
                tb.AccessibleName = sCol;
                tb.Location = new Point(12 + iLabelWidth, iY);
                tb.Size = new Size(iTextWidth, 23);
                tb.TabIndex = i;
                if (dInitial != null && dInitial.ContainsKey(sCol))
                    tb.Text = dInitial[sCol];
                if (lEditable != null && i < lEditable.Count && !lEditable[i])
                    tb.ReadOnly = true;
                this.Controls.Add(tb);
                lTextBoxes.Add(tb);

                iY += iRowHeight;
            }

            iY += 8;

            Button btnOk = new Button();
            btnOk.Text = "&OK";
            btnOk.AccessibleName = "OK";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Size = new Size(90, 28);
            btnOk.Location = new Point(iWidth - 200, iY);
            btnOk.TabIndex = lColumns.Count;
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += okClicked;
            this.Controls.Add(btnOk);

            Button btnCancel = new Button();
            btnCancel.Text = "&Cancel";
            btnCancel.AccessibleName = "Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Size = new Size(90, 28);
            btnCancel.Location = new Point(iWidth - 100, iY);
            btnCancel.TabIndex = lColumns.Count + 1;
            btnCancel.UseVisualStyleBackColor = true;
            this.Controls.Add(btnCancel);

            this.ClientSize = new Size(iWidth, iY + 40);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
            if (lTextBoxes.Count > 0) this.ActiveControl = lTextBoxes[0];
        }

        private void okClicked(object oSender, EventArgs oArgs)
        {
            dValues.Clear();
            for (int i = 0; i < lTextBoxes.Count; i++)
                dValues[lColumnNames[i]] = lTextBoxes[i].Text;
        }
    }

    // =====================================================================
    // FilterDialog: Select-Record dialog. Three controls -- filter
    // text, column chooser, match-mode chooser. The column chooser
    // includes "All columns" as the first option (for any-column
    // matches). The match modes are Contains, Starts with, Equals.
    // =====================================================================
    public class FilterDialog : Form
    {
        public string sFilterText;
        public string sFilterColumn;
        public string sMatchMode;

        public FilterDialog(List<string> lColumns, string sCurrentText, string sCurrentColumn, string sCurrentMode)
        {
            this.Text = "Select-Record (filter)";
            this.AccessibleName = "Filter records";
            this.AccessibleDescription = "Type filter text, choose a column and match mode, then press OK.";
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(440, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;

            Label lbl1 = new Label();
            lbl1.Text = "Filter &text:";
            lbl1.Location = new Point(12, 16);
            lbl1.Size = new Size(110, 20);
            this.Controls.Add(lbl1);

            TextBox tbFilter = new TextBox();
            tbFilter.AccessibleName = "Filter text";
            tbFilter.Location = new Point(130, 13);
            tbFilter.Size = new Size(290, 23);
            tbFilter.TabIndex = 0;
            tbFilter.Text = sCurrentText ?? "";
            this.Controls.Add(tbFilter);

            Label lbl2 = new Label();
            lbl2.Text = "&In column:";
            lbl2.Location = new Point(12, 48);
            lbl2.Size = new Size(110, 20);
            this.Controls.Add(lbl2);

            ComboBox cbCol = new ComboBox();
            cbCol.AccessibleName = "Column";
            cbCol.Location = new Point(130, 45);
            cbCol.Size = new Size(290, 23);
            cbCol.TabIndex = 1;
            cbCol.DropDownStyle = ComboBoxStyle.DropDownList;
            cbCol.Items.Add("All columns");
            foreach (string s in lColumns) cbCol.Items.Add(s);
            cbCol.SelectedItem = (sCurrentColumn != null && cbCol.Items.Contains(sCurrentColumn)) ? (object)sCurrentColumn : (object)"All columns";
            this.Controls.Add(cbCol);

            Label lbl3 = new Label();
            lbl3.Text = "&Match mode:";
            lbl3.Location = new Point(12, 80);
            lbl3.Size = new Size(110, 20);
            this.Controls.Add(lbl3);

            ComboBox cbMode = new ComboBox();
            cbMode.AccessibleName = "Match mode";
            cbMode.Location = new Point(130, 77);
            cbMode.Size = new Size(290, 23);
            cbMode.TabIndex = 2;
            cbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cbMode.Items.Add("Contains");
            cbMode.Items.Add("Starts with");
            cbMode.Items.Add("Equals");
            cbMode.SelectedItem = (sCurrentMode != null && cbMode.Items.Contains(sCurrentMode)) ? (object)sCurrentMode : (object)"Contains";
            this.Controls.Add(cbMode);

            Button btnOk = new Button();
            btnOk.Text = "&OK";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Size = new Size(90, 28);
            btnOk.Location = new Point(240, 150);
            btnOk.TabIndex = 3;
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += (s, e) => {
                sFilterText = tbFilter.Text;
                sFilterColumn = cbCol.SelectedItem as string;
                sMatchMode = cbMode.SelectedItem as string;
            };
            this.Controls.Add(btnOk);

            Button btnCancel = new Button();
            btnCancel.Text = "&Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Size = new Size(90, 28);
            btnCancel.Location = new Point(336, 150);
            btnCancel.TabIndex = 4;
            btnCancel.UseVisualStyleBackColor = true;
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
            this.ActiveControl = tbFilter;
            tbFilter.SelectAll();
        }
    }

    // =====================================================================
    // SortDialog: Sort-Object dialog. Column chooser plus ascending /
    // descending toggle. The user picks a column from the recordset's
    // current field set; the dialog returns "col ASC" or "col DESC"
    // (the form expected by ADODB Recordset.Sort).
    //
    // descending radio buttons.
    // =====================================================================
    public class SortDialog : Form
    {
        public string sSortColumn;
        public bool bAscending;

        public SortDialog(List<string> lColumns, string sCurrentColumn, bool bCurrentAsc)
        {
            this.Text = "Sort-Object (custom sort)";
            this.AccessibleName = "Sort records";
            this.AccessibleDescription = "Choose a sort column and direction, then press OK.";
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(440, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;

            Label lbl1 = new Label();
            lbl1.Text = "&Column:";
            lbl1.Location = new Point(12, 16);
            lbl1.Size = new Size(110, 20);
            this.Controls.Add(lbl1);

            ComboBox cbCol = new ComboBox();
            cbCol.AccessibleName = "Sort column";
            cbCol.Location = new Point(130, 13);
            cbCol.Size = new Size(290, 23);
            cbCol.TabIndex = 0;
            cbCol.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (string s in lColumns) cbCol.Items.Add(s);
            if (lColumns.Count > 0)
                cbCol.SelectedItem = (sCurrentColumn != null && cbCol.Items.Contains(sCurrentColumn))
                    ? (object)sCurrentColumn : (object)lColumns[0];
            this.Controls.Add(cbCol);

            RadioButton rbAsc = new RadioButton();
            rbAsc.Text = "&Ascending";
            rbAsc.AccessibleName = "Ascending";
            rbAsc.Location = new Point(130, 50);
            rbAsc.Size = new Size(140, 22);
            rbAsc.TabIndex = 1;
            rbAsc.Checked = bCurrentAsc;
            this.Controls.Add(rbAsc);

            RadioButton rbDesc = new RadioButton();
            rbDesc.Text = "&Descending";
            rbDesc.AccessibleName = "Descending";
            rbDesc.Location = new Point(280, 50);
            rbDesc.Size = new Size(140, 22);
            rbDesc.TabIndex = 2;
            rbDesc.Checked = !bCurrentAsc;
            this.Controls.Add(rbDesc);

            Button btnOk = new Button();
            btnOk.Text = "&OK";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Size = new Size(90, 28);
            btnOk.Location = new Point(240, 130);
            btnOk.TabIndex = 3;
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += (s, e) => {
                sSortColumn = cbCol.SelectedItem as string;
                bAscending = rbAsc.Checked;
            };
            this.Controls.Add(btnOk);

            Button btnCancel = new Button();
            btnCancel.Text = "&Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Size = new Size(90, 28);
            btnCancel.Location = new Point(336, 130);
            btnCancel.TabIndex = 4;
            btnCancel.UseVisualStyleBackColor = true;
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
            this.ActiveControl = cbCol;
        }
    }

    // =====================================================================
    // CommandPickerDialog: flat picker over every registered command.
    // Filter text box on top, listbox below. Inspired by EdSharp's
    // Alternate Menu (Alt+F10), this implements PowerShell's
    // Show-Command idiom.
    // =====================================================================
    public class CommandPickerDialog : Form
    {
        public ToolStripMenuItem oChosenItem;
        private List<ToolStripMenuItem> lAllItems = new List<ToolStripMenuItem>();
        private List<string> lAllDisplay = new List<string>();
        private TextBox tbFilter;
        private ListBox lbCommands;

        public CommandPickerDialog()
        {
            this.Text = "Show-Command";
            this.AccessibleName = "Show Command";
            this.AccessibleDescription = "";
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(640, 420);
            this.MinimumSize = new Size(420, 240);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;

            Label lblF = new Label();
            lblF.Text = "&Filter:";
            lblF.Location = new Point(12, 14);
            lblF.Size = new Size(60, 20);
            this.Controls.Add(lblF);

            tbFilter = new TextBox();
            tbFilter.AccessibleName = "Filter";
            tbFilter.Location = new Point(80, 11);
            tbFilter.Size = new Size(540, 23);
            tbFilter.TabIndex = 0;
            tbFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbFilter.TextChanged += filterChanged;
            tbFilter.KeyDown += filterKeyDown;
            this.Controls.Add(tbFilter);

            Label lblL = new Label();
            lblL.Text = "&Commands:";
            lblL.Location = new Point(12, 42);
            lblL.Size = new Size(120, 20);
            this.Controls.Add(lblL);

            lbCommands = new ListBox();
            lbCommands.AccessibleName = "Commands";
            lbCommands.Location = new Point(12, 64);
            lbCommands.Size = new Size(608, 308);
            lbCommands.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            lbCommands.IntegralHeight = false;
            lbCommands.TabIndex = 1;
            lbCommands.DoubleClick += (s, e) => activateChosen();
            lbCommands.KeyDown += listKeyDown;
            this.Controls.Add(lbCommands);

            Button btnOk = new Button();
            btnOk.Text = "&OK";
            btnOk.Size = new Size(90, 28);
            btnOk.Location = new Point(440, 384);
            btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOk.TabIndex = 2;
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += (s, e) => activateChosen();
            this.Controls.Add(btnOk);

            Button btnCancel = new Button();
            btnCancel.Text = "&Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Size = new Size(90, 28);
            btnCancel.Location = new Point(536, 384);
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.TabIndex = 3;
            btnCancel.UseVisualStyleBackColor = true;
            this.Controls.Add(btnCancel);

            this.CancelButton = btnCancel;

            foreach (KeyValuePair<ToolStripMenuItem, string> kv in KeyMap.dMenuToCommand)
            {
                string sCmd = kv.Value;
                string sKey = KeyMap.dCommandToKey.ContainsKey(sCmd) ? KeyMap.friendlyKey(KeyMap.dCommandToKey[sCmd]) : "(unbound)";
                string sDisabled = kv.Key.Enabled ? "" : "  (disabled)";
                lAllItems.Add(kv.Key);
                lAllDisplay.Add(string.Format("{0}  [{1}]{2}", sCmd, sKey, sDisabled));
            }
            int[] aOrder = new int[lAllDisplay.Count];
            for (int i = 0; i < aOrder.Length; i++) aOrder[i] = i;
            Array.Sort(aOrder, (i, j) => string.Compare(lAllDisplay[i], lAllDisplay[j], StringComparison.OrdinalIgnoreCase));
            List<ToolStripMenuItem> lSortedItems = new List<ToolStripMenuItem>();
            List<string> lSortedDisplay = new List<string>();
            foreach (int i in aOrder) { lSortedItems.Add(lAllItems[i]); lSortedDisplay.Add(lAllDisplay[i]); }
            lAllItems = lSortedItems;
            lAllDisplay = lSortedDisplay;

            applyFilter("");
            this.ActiveControl = tbFilter;
        }

        private void applyFilter(string sNeedle)
        {
            lbCommands.BeginUpdate();
            lbCommands.Items.Clear();
            string sLow = sNeedle.ToLowerInvariant();
            for (int i = 0; i < lAllDisplay.Count; i++)
            {
                if (sLow.Length == 0 || lAllDisplay[i].ToLowerInvariant().Contains(sLow))
                    lbCommands.Items.Add(new PickerItem(i, lAllDisplay[i]));
            }
            if (lbCommands.Items.Count > 0) lbCommands.SelectedIndex = 0;
            lbCommands.EndUpdate();
        }

        private void filterChanged(object oSender, EventArgs oArgs) { applyFilter(tbFilter.Text); }

        private void filterKeyDown(object oSender, KeyEventArgs oArgs)
        {
            if (oArgs.KeyCode == Keys.Down)
            {
                if (lbCommands.Items.Count > 0)
                {
                    lbCommands.Focus();
                    if (lbCommands.SelectedIndex < 0) lbCommands.SelectedIndex = 0;
                }
                oArgs.Handled = true;
            }
            else if (oArgs.KeyCode == Keys.Enter)
            {
                activateChosen();
                oArgs.Handled = true;
                oArgs.SuppressKeyPress = true;
            }
        }

        private void listKeyDown(object oSender, KeyEventArgs oArgs)
        {
            if (oArgs.KeyCode == Keys.Enter)
            {
                activateChosen();
                oArgs.Handled = true;
                oArgs.SuppressKeyPress = true;
            }
        }

        private void activateChosen()
        {
            if (lbCommands.SelectedItem == null) return;
            PickerItem oPick = lbCommands.SelectedItem as PickerItem;
            if (oPick == null) return;
            oChosenItem = lAllItems[oPick.iOriginalIndex];
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private class PickerItem
        {
            public int iOriginalIndex;
            public string sDisplay;
            public PickerItem(int i, string s) { iOriginalIndex = i; sDisplay = s; }
            public override string ToString() { return sDisplay; }
        }
    }

    // =====================================================================
    // DbDuoForm: the WinForms GUI. Holds a single DbDuoManager
    // instance; the recordset inside is the single source of truth
    // for current position, filter, sort, and current table. The
    // form's grid is a render-only mirror, rebuilt on each refresh.
    //
    // Key architectural difference from the predecessor: the form
    // does NOT keep its own copies of filter/sort/current-row state.
    // All such state lives in the recordset; the form reads it on
    // every render, which guarantees consistency between GUI and CLI.
    // =====================================================================
    public class DbDuoForm : Form
    {
        // ------- Constants -------
        private const int InitialFormWidth = 1100;
        private const int InitialFormHeight = 700;

        // ------- Fields -------
        private DbDuoManager db;
        // The main data view. A standard WinForms ListView in
        // Details mode is used instead of DataGridView because
        // screen readers (JAWS, NVDA, Narrator) read DataGridView
        // cells with verbose "row N column M" verbiage that is
        // difficult to suppress. A ListView is announced as a
        // simple "list" with each row being a "list item" -- the
        // same accessibility model as Windows Explorer's file
        // list, which all three readers handle cleanly.
        //
        // Trade-off: per-cell navigation goes away. The user moves
        // row-by-row with up/down arrows, hears the focused row
        // announced as a unit, and uses Show-Record (Enter) for a
        // field-by-field dump of the current row. Sort and
        // column-specific operations work via the Sort-Object
        // dialog (Alt+Shift+O) or by setting a "current column"
        // through Tab navigation within the focused row.
        //
        // Virtual mode (VirtualMode=true) is essential. The list
        // never materializes more than a small window of
        // ListViewItems; row text is fetched from the ADO recordset
        // on demand via the RetrieveVirtualItem event. This keeps
        // memory tiny and load time near-zero even for 100K-row
        // tables.
        private ListView grid;
        private MenuStrip menuMain;
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblStatus;
        private ToolStripStatusLabel lblTable;
        private ContextMenuStrip ctxGrid;

        private DataTable oCurrentData;
        private bool bSuppressCellChanged;

        // The ListView has no per-cell focus, but operations like
        // Sort-Ascending want to know "which column is the user
        // looking at." We track this index manually: when the user
        // presses Tab within a focused row we increment it; when
        // they press Shift+Tab we decrement; arrow keys reset it
        // to 0. The screen reader announces the column header and
        // current cell value on each Tab so the user knows which
        // column they are on.
        private int iCurrentColumnIndex = 0;

        // True after we've announced "Table has no rows" for the
        // current empty state. Prevents re-announcing on every
        // refresh while the user is still on the empty table.
        // Reset when the table changes OR the row count becomes
        // non-zero.
        private bool bAnnouncedEmpty = false;
        private string sLastFindCriteria = "";

        // ------- Menu items (all alphabetical within their menu) -------
        private ToolStripMenuItem miFile;
        private ToolStripMenuItem miFileNew;
        private ToolStripMenuItem miFileOpen;
        private ToolStripMenuItem miFileSaveAs;
        private ToolStripMenuItem miFileClose;
        private ToolStripMenuItem miFileBackup;
        private ToolStripMenuItem miFileCompare;
        private ToolStripMenuItem miFileImport;
        private ToolStripMenuItem miFileExport;
        private ToolStripMenuItem miFilePrint;
        private ToolStripMenuItem miFileExit;

        private ToolStripMenuItem miRecord;
        private ToolStripMenuItem miRecNew;
        private ToolStripMenuItem miRecSet;
        private ToolStripMenuItem miRecRemove;
        private ToolStripMenuItem miRecShow;
        private ToolStripMenuItem miRecCopy;
        private ToolStripMenuItem miRecFind;
        private ToolStripMenuItem miRecFindNext;
        private ToolStripMenuItem miRecFindPrev;
        private ToolStripMenuItem miRecMark;
        private ToolStripMenuItem miRecUnmark;
        private ToolStripMenuItem miRecRelated;
        private ToolStripMenuItem miRecEnterChild;
        private ToolStripMenuItem miRecExitChild;
        private ToolStripMenuItem miRecUpdateField;
        private ToolStripMenuItem miRecGoTo;
        private ToolStripMenuItem miRecBookmark;
        private ToolStripMenuItem miRecGotoBookmark;
        private ToolStripMenuItem miRecClearBookmark;
        private ToolStripMenuItem miRecOpenCell;

        private ToolStripMenuItem miView;
        private ToolStripMenuItem miViewSelect;
        private ToolStripMenuItem miViewResetFilter;
        private ToolStripMenuItem miViewFormat;
        private ToolStripMenuItem miViewSortAsc;
        private ToolStripMenuItem miViewSortDesc;
        private ToolStripMenuItem miViewSortRecent;
        private ToolStripMenuItem miViewSortOldest;
        private ToolStripMenuItem miViewResetSort;
        private ToolStripMenuItem miViewUpdate;
        private ToolStripMenuItem miViewSelectColumn;

        private ToolStripMenuItem miSchema;
        private ToolStripMenuItem miSchemaSelectTable;
        private ToolStripMenuItem miSchemaSelectView;
        private ToolStripMenuItem miSchemaSwitch;
        private ToolStripMenuItem miSchemaSwitchPrev;
        private ToolStripMenuItem miSchemaSwitchAll;
        private ToolStripMenuItem miSchemaSwitchAllPrev;
        private ToolStripMenuItem miSchemaShow;
        private ToolStripMenuItem miSchemaProperties;

        private ToolStripMenuItem miTools;
        private ToolStripMenuItem miToolsTest;
        private ToolStripMenuItem miToolsMeasure;
        private ToolStripMenuItem miToolsLock;
        private ToolStripMenuItem miToolsTestDriver;
        private ToolStripMenuItem miToolsOpenFolder;
        private ToolStripMenuItem miToolsConsole;
        private ToolStripMenuItem miToolsInvokeSql;
        private ToolStripMenuItem miToolsEditConfig;

        private ToolStripMenuItem miHelp;
        private ToolStripMenuItem miHelpContents;
        private ToolStripMenuItem miHelpVerbs;
        private ToolStripMenuItem miHelpShowCommand;
        private ToolStripMenuItem miHelpStatus;
        private ToolStripMenuItem miHelpTestReader;
        private ToolStripMenuItem miHelpTraceCommand;
        private ToolStripMenuItem miHelpLog;
        private ToolStripMenuItem miHelpWebSite;
        private ToolStripMenuItem miHelpAbout;

        // ------- Constructor / lifecycle -------
        public DbDuoForm() : this(Program.UiMode.Both) { }

        public DbDuoForm(Program.UiMode oMode)
        {
            oUiMode = oMode;
            db = new DbDuoManager();
            initializeForm();
            buildMenus();
            buildGrid();
            buildStatusBar();
            LiveRegion.attach(this);
            applyIniOverrides();
            updateMenuEnabled();
            updateStatusBar();
        }

        // OnShown fires after the form is first displayed and has a
        // valid window handle. We use it to make a debug live-region
        // announcement so the user can confirm the live-region
        // pipeline is working with their screen reader. If they
        // hear "Live: DbDuo ready" on launch, every other live-
        // region announcement throughout the program will work too.
        //
        // We also register the system-wide Alt+GraveAccent hotkey
        // here, because RegisterHotKey requires the window to have
        // a valid HWND -- which is only guaranteed once the form has
        // been shown.
        protected override void OnShown(EventArgs oArgs)
        {
            base.OnShown(oArgs);
            LiveRegion.say("DbDuo ready");
            registerSwitchToGuiHotKey();
        }

        // The mode the form was started in. Read by Program.Main right
        // after Application.Run starts so it knows whether to also
        // spawn a console session (uiMode == Both).
        public Program.UiMode uiMode { get { return oUiMode; } }
        private Program.UiMode oUiMode;

        protected override void Dispose(bool bDisposing)
        {
            if (bDisposing) unregisterSwitchToGuiHotKey();
            if (bDisposing && db != null) { try { db.Dispose(); } catch { } db = null; }
            base.Dispose(bDisposing);
        }

        // ---------------- Single-instance wake-up handling ----------------
        //
        // When a second DbDuo.exe is launched with -activate (the desktop
        // hotkey path), it broadcasts the registered "DbDuo.WakeUp" message
        // and exits. Every top-level window in the user session receives
        // the message; the first instance's WndProc here recognizes its
        // own ID and brings the window forward.
        //
        // ShowWindow with SW_RESTORE handles the case where the form has
        // been minimized (the user iconified it before pressing the
        // hotkey). SetForegroundWindow then steals focus to the form.
        // Both calls are best-effort: SetForegroundWindow is allowed by
        // Windows only under specific conditions (e.g., when the calling
        // process has foreground rights), but as long as DbDuo's own
        // process is invoking it on its own window, it succeeds.
        // ---------------- ----------------

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int iCmdShow);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int iId, int iModifiers, int iVk);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int iId);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        private const int SW_RESTORE = 9;

        // Global hotkey: Alt+GraveAccent (Alt + the key above Tab).
        // Registered system-wide on this form's HWND. When pressed
        // anywhere in Windows, the form receives WM_HOTKEY. We only
        // act on it when DbDuo's own dot-prompt console window is
        // currently in the foreground -- otherwise we silently let
        // the keystroke proceed (so Alt+GraveAccent doesn't randomly
        // steal focus from Word, Excel, or any other application
        // the user happens to be in).
        //
        // The hotkey is a one-way "switch from console to GUI"
        // convenience. To go the other direction, the dot prompt's
        // 'gui' / 'focus' / 'window' command works from the GUI
        // already, and Alt+Tab works system-wide.
        //
        // MOD_ALT = 1, MOD_CONTROL = 2 (combinable). VK_OEM_3 (grave/tilde)
        // = 0xC0 in Windows virtual-key terms (Keys.Oemtilde in .NET).
        private const int ModAlt = 0x1;
        private const int ModControl = 0x2;
        private const int VkOemGrave = 0xC0;  // grave accent / tilde, US layout
        // Two hotkey IDs in the application-reserved 0x0000-0xBFFF range.
        // We pick values well above the typical range used by other
        // applications to minimize the chance of a numerical collision.
        private const int HotKeyIdSwitchToGui = 0x4421;
        private const int HotKeyIdToggleWindow = 0x4422;
        private bool bHotKeyRegistered = false;
        private bool bToggleHotKeyRegistered = false;

        private void registerSwitchToGuiHotKey()
        {
            try
            {
                IntPtr hWnd = this.Handle;
                if (hWnd == IntPtr.Zero) return;
                // Hotkey 1: Alt+GraveAccent = "summon GUI." Acts only when
                // DbDuo's console is foreground (polite to other apps).
                bool bOk = RegisterHotKey(hWnd, HotKeyIdSwitchToGui, ModAlt, VkOemGrave);
                if (bOk)
                {
                    bHotKeyRegistered = true;
                    DbDuoLog.write("Alt+GraveAccent registered as switch-to-GUI hotkey.");
                }
                else
                {
                    DbDuoLog.write("Alt+GraveAccent hotkey could not be registered (already in use system-wide). Switch via 'gui' / 'focus' at the dot prompt instead.");
                }
                // Hotkey 2: Alt+Control+GraveAccent = "toggle DbDuo." This
                // one always acts: from anywhere on Windows it activates
                // either the GUI or the console, whichever is NOT currently
                // foreground (preferring the one that isn't already there).
                // This is the global "summon DbDuo" chord. The trio is now:
                //   Control+GraveAccent       GUI menu hotkey, GUI -> console
                //   Alt+GraveAccent           Global, console -> GUI
                //   Alt+Control+GraveAccent   Global, toggle between them
                bool bOk2 = RegisterHotKey(hWnd, HotKeyIdToggleWindow,
                                           ModAlt | ModControl, VkOemGrave);
                if (bOk2)
                {
                    bToggleHotKeyRegistered = true;
                    DbDuoLog.write("Alt+Control+GraveAccent registered as toggle hotkey.");
                }
                else
                {
                    DbDuoLog.write("Alt+Control+GraveAccent hotkey could not be registered.");
                }
            }
            catch (Exception oEx)
            {
                DbDuoLog.write("Hotkey registration error: " + oEx.Message);
            }
        }

        private void unregisterSwitchToGuiHotKey()
        {
            if (bHotKeyRegistered)
            {
                try { UnregisterHotKey(this.Handle, HotKeyIdSwitchToGui); } catch { }
                bHotKeyRegistered = false;
            }
            if (bToggleHotKeyRegistered)
            {
                try { UnregisterHotKey(this.Handle, HotKeyIdToggleWindow); } catch { }
                bToggleHotKeyRegistered = false;
            }
        }

        // WM_HOTKEY = 0x0312.
        private const int WmHotKey = 0x0312;

        protected override void WndProc(ref Message oMsg)
        {
            // Message IDs from RegisterWindowMessage are in the
            // 0xC000-0xFFFF range, which fits in int with no sign issues,
            // but compare as uint to keep the types matched.
            if ((uint)oMsg.Msg == SingleInstance.WakeUpMessage)
            {
                bringForward();
                return;
            }
            if (oMsg.Msg == WmHotKey)
            {
                int iId = (int)oMsg.WParam;
                if (iId == HotKeyIdSwitchToGui)
                {
                    // Only react if foreground is our own console. The
                    // foreground check is what keeps Alt+GraveAccent from
                    // randomly stealing focus when the user is in Word or
                    // any other app.
                    IntPtr hFg = GetForegroundWindow();
                    IntPtr hCon = GetConsoleWindow();
                    if (hFg != IntPtr.Zero && hCon != IntPtr.Zero && hFg == hCon)
                    {
                        bringForward();
                    }
                    return;
                }
                if (iId == HotKeyIdToggleWindow)
                {
                    // The toggle: foreground is the GUI form (or any
                    // third-party window) -> bring console forward;
                    // foreground IS the console -> bring GUI forward.
                    // Always acts; the whole point is global summoning.
                    IntPtr hFg = GetForegroundWindow();
                    IntPtr hCon = GetConsoleWindow();
                    bool bConsoleIsFg = (hCon != IntPtr.Zero && hFg == hCon);
                    if (bConsoleIsFg)
                    {
                        bringForward();
                    }
                    else
                    {
                        // Bring console forward if it exists; otherwise
                        // there's no console open, so just bring GUI.
                        if (hCon != IntPtr.Zero)
                        {
                            try
                            {
                                ShowWindow(hCon, SW_RESTORE);
                                SetForegroundWindow(hCon);
                            }
                            catch { }
                        }
                        else
                        {
                            bringForward();
                        }
                    }
                    return;
                }
            }
            base.WndProc(ref oMsg);
        }

        // Restore the window if minimized, then yank it to the foreground.
        // Safe to call from the message-loop thread (WndProc); for cross-
        // thread callers, marshal through Invoke first.
        private void bringForward()
        {
            try
            {
                IntPtr hWnd = this.Handle;
                if (hWnd == IntPtr.Zero) return;
                if (IsIconic(hWnd) || this.WindowState == FormWindowState.Minimized)
                {
                    ShowWindow(hWnd, SW_RESTORE);
                    this.WindowState = FormWindowState.Normal;
                }
                this.Activate();
                SetForegroundWindow(hWnd);
                DbDuoLog.write("Brought forward by external wake-up message.");
            }
            catch { }
        }
        // ---------------- ---------------- ----------------

        private void initializeForm()
        {
            this.Text = "DbDuo";
            this.AccessibleName = "DbDuo";
            // No AccessibleDescription. Screen readers read the title
            // and the focused control on form open; an extra description
            // becomes a "banner" that wastes time before the user can
            // start working.
            this.ClientSize = new Size(InitialFormWidth, InitialFormHeight);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(formKeyDown);
        }

        // KeyPreview=true routes every keystroke through this handler
        // BEFORE the focused control sees it. We use it as a backstop
        // for Alt+arrow / Alt+Home / Alt+End mark-navigation keys,
        // because Alt+arrow can be intercepted by the MenuStrip on
        // its way through normal dispatch.
        //
        // For each key handled here, set both Handled=true (stops
        // KeyDown propagation) and SuppressKeyPress=true (prevents
        // the keystroke from reaching the focused control's
        // KeyPress and OnKeyDown overrides).
        //
        // Control+Tab and Control+Shift+Tab are NOT handled here.
        // They are wired as menu shortcuts on Schema > Switch-Table
        // (Control+Tab) and Schema > Switch-TablePrevious
        // (Control+Shift+Tab); the standard ProcessCmdKey -> KeyMap
        // dispatch handles them, and the menu items also give them
        // discoverability and disabled-state announcements through
        // the same mechanism every other hotkey uses.
        private void formKeyDown(object oSender, KeyEventArgs oArgs)
        {
            Keys oKey = oArgs.KeyData;
            // Alt+Home/End/Up/Down for marked-row navigation. These
            // have no menu binding (they would clutter the Record menu
            // with four near-identical items); the formKeyDown handler
            // is the only dispatch path for them.
            if (oKey == (Keys.Alt | Keys.Home))
            { oArgs.Handled = true; oArgs.SuppressKeyPress = true; jumpToMarkedRow(MarkJump.First);    return; }
            if (oKey == (Keys.Alt | Keys.End))
            { oArgs.Handled = true; oArgs.SuppressKeyPress = true; jumpToMarkedRow(MarkJump.Last);     return; }
            if (oKey == (Keys.Alt | Keys.Up))
            { oArgs.Handled = true; oArgs.SuppressKeyPress = true; jumpToMarkedRow(MarkJump.Previous); return; }
            if (oKey == (Keys.Alt | Keys.Down))
            { oArgs.Handled = true; oArgs.SuppressKeyPress = true; jumpToMarkedRow(MarkJump.Next);     return; }
        }

        // ------- Public surface for the CLI thread -------
        public DbDuoManager Db { get { return db; } }

        public void invokeRefresh()
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action(updateGrid)); } catch { }
            }
            else
            {
                updateGrid();
            }
        }

        public void invokeMessage(string sMsg)
        {
            if (this.IsDisposed) return;
            try { this.Invoke(new Action<string>(s => lblStatus.Text = s), new object[] { sMsg }); } catch { }
        }

        // Public entry points for the dot prompt to trigger drill-down
        // commands on the GUI thread. Both marshal to the form's
        // message loop because the click handlers manipulate UI state
        // (and the drill stack lives on the form). The console wraps
        // these in cmdEnterChild / cmdExitChild.
        //
        // Returns the count of stack entries AFTER the operation, so
        // the console can report depth to the user. Returns -1 if the
        // form is disposed.
        public int invokeEnterChild()
        {
            if (this.IsDisposed) return -1;
            try
            {
                this.Invoke(new Action(() => recEnterChildClicked(this, EventArgs.Empty)));
            }
            catch { }
            return oDrillStack.Count;
        }

        public int invokeExitChild()
        {
            if (this.IsDisposed) return -1;
            try
            {
                this.Invoke(new Action(() => recExitChildClicked(this, EventArgs.Empty)));
            }
            catch { }
            return oDrillStack.Count;
        }

        // True if there is at least one parent on the drill stack
        // (i.e., Exit-Child has something to pop). Used by the dot
        // prompt's cmdExitChild to give a no-op message without
        // marshaling to the GUI thread first.
        public bool hasDrillStack()
        {
            return oDrillStack.Count > 0;
        }

        // Drop any pending drill entries. Used when the database
        // changes (open or close) from outside the form's normal
        // click handlers -- the dot prompt's cmdOpenDatabase /
        // cmdCloseDatabase paths in particular.
        public void clearDrillStack()
        {
            oDrillStack.Clear();
        }

        // =====================================================================
        // Menu construction. Every menu item is registered with
        // KeyMap and gets a default hotkey (or Keys.None for items
        // without one). All hotkeys are user-overridable via DbDuo.ini.
        // =====================================================================
        private void buildMenus()
        {
            menuMain = new MenuStrip();
            menuMain.AccessibleName = "Main menu";

            miFile = addMenu("&File");
            miFileNew     = addItem(miFile, "&New Database File...",      "New-Database",     Keys.Control | Keys.Shift | Keys.N,   fileNewClicked);
            miFileOpen    = addItem(miFile, "&Open Database File...",     "Open-Database",    Keys.Control | Keys.O,                fileOpenClicked);
            miFileSaveAs  = addItem(miFile, "&Save Database File As...",  "Save-DatabaseAs",  Keys.Control | Keys.S,                fileSaveAsClicked);
            miFileClose   = addItem(miFile, "&Close Database File",       "Close-Database",   Keys.Control | Keys.F4,               fileCloseClicked);
            addSep(miFile);
            miFileBackup  = addItem(miFile, "&Backup Database File...",   "Backup-Database",  Keys.Control | Keys.Shift | Keys.S,   fileBackupClicked);
            miFileCompare = addItem(miFile, "Compare Database File...",  "Compare-Database", Keys.None,                            fileCompareClicked);
            addSep(miFile);
            miFileImport  = addItem(miFile, "&Import Data...",       "Import-Data",      Keys.Control | Keys.Shift | Keys.I,   fileImportClicked);
            miFileExport  = addItem(miFile, "&Export Data...",       "Export-Data",      Keys.Control | Keys.Shift | Keys.X,   fileExportClicked);
            addSep(miFile);
            miFilePrint   = addItem(miFile, "&Print...",             "Out-Printer",      Keys.Control | Keys.P,                filePrintClicked);
            addSep(miFile);
            miFileExit    = addItem(miFile, "Exit",                 "Exit-Application", Keys.Alt | Keys.F4,                   fileExitClicked);

            miRecord = addMenu("&Record");
            miRecNew         = addItem(miRecord, "&New-Record...",          "New-Record",          Keys.Control | Keys.N,              recNewClicked);
            miRecSet         = addItem(miRecord, "&Set-Record...",          "Set-Record",          Keys.F2,                            recSetClicked);
            miRecRemove      = addItem(miRecord, "Remove-Record",          "Remove-Record",       Keys.Control | Keys.D,              recRemoveClicked);
            miRecUpdateField = addItem(miRecord, "Update-Field (&Replace across rows)...", "Update-Field", Keys.Control | Keys.R,        recUpdateFieldClicked);
            addSep(miRecord);
            miRecShow        = addItem(miRecord, "Show-&Object (examine current record)", "Show-Object",  Keys.Enter,                         recShowClicked);
            miRecCopy        = addItem(miRecord, "&Copy-Record (duplicate current row as new record)", "Copy-Record",         Keys.Control | Keys.Shift | Keys.C, recCopyClicked);
            miRecOpenCell    = addItem(miRecord, "&Open-Cell (URL or path in cell)", "Open-Cell", Keys.Control | Keys.Enter,         recOpenCellClicked);
            addSep(miRecord);
            miRecFind        = addItemLocal(miRecord, "&Jump-Record...",         "Jump-Record",         Keys.Shift | Keys.J,                recFindClicked);
            miRecFindNext    = addItem(miRecord, "Jump Next",              "Jump-RecordAgain",    Keys.F3,                            recFindNextClicked);
            miRecFindPrev    = addItem(miRecord, "Jump Previous",          "Jump-RecordPrevious", Keys.Shift | Keys.F3,               recFindPrevClicked);
            addSep(miRecord);
            miRecMark        = addItemLocal(miRecord, "Set-&Mark (current row)", "Set-Mark",            Keys.Shift | Keys.M,                recMarkClicked);
            miRecUnmark      = addItemLocal(miRecord, "Clear-Mark / &Unmark (current row)", "Clear-Mark", Keys.Shift | Keys.U,             recUnmarkClicked);
            addSep(miRecord);
            miRecGoTo        = addItemLocal(miRecord, "Set-Position (&go to row)...", "Set-Position",   Keys.Shift | Keys.G,                recGoToClicked);
            miRecRelated     = addItem(miRecord, "Show-Related (jump to FK target)...", "Show-Related", Keys.None,                     recRelatedClicked);
            miRecEnterChild  = addItemLocal(miRecord, "&Enter-Child (drill to related child rows)...", "Enter-Child", Keys.Shift | Keys.E, recEnterChildClicked);
            miRecExitChild   = addItemLocal(miRecord, "E&xit-Child (return to parent row)", "Exit-Child", Keys.Shift | Keys.X, recExitChildClicked);
            addSep(miRecord);
            miRecBookmark    = addItem(miRecord, "&Save-Bookmark (current row)",  "Save-Bookmark",     Keys.Control | Keys.K,              recBookmarkClicked);
            miRecGotoBookmark= addItem(miRecord, "Restore-Bookmark (return to saved row)", "Restore-Bookmark", Keys.Alt | Keys.K,        recGotoBookmarkClicked);
            miRecClearBookmark=addItem(miRecord, "Clear-Bookmark",         "Clear-Bookmark",      Keys.Control | Keys.Shift | Keys.K, recClearBookmarkClicked);

            // Delete key as a secondary binding for Remove-Record. The
            // primary menu shortcut is Control+D (shown next to the
            // menu item); Delete is a long-standing grid-editor idiom
            // (Excel tables, Outlook lists). The same confirmation
            // dialog runs in both paths because both call recRemoveClicked.
            KeyMap.registerAlias(Keys.Delete, miRecRemove);

            miView = addMenu("&View");
            miViewSelect       = addItemLocal(miView, "Select-Record (&filter)...",        "Select-Record",     Keys.Shift | Keys.F,                viewSelectClicked);
            miViewResetFilter  = addItemLocal(miView, "&Reset filter",                     "Reset-Filter",      Keys.Shift | Keys.R,                viewResetFilterClicked);
            addSep(miView);
            miViewFormat       = addItemLocal(miView, "&Sort-Object (custom sort)...",     "Sort-Object",       Keys.Shift | Keys.S,                viewFormatClicked);
            miViewSortAsc      = addItem(miView, "Sort &ascending by current column (alpha)", "Sort-Ascending",  Keys.Alt | Keys.A,                  viewSortAscClicked);
            miViewSortDesc     = addItem(miView, "Sort &descending by current column (alpha)", "Sort-Descending", Keys.Alt | Keys.Shift | Keys.A,    viewSortDescClicked);
            miViewSortRecent   = addItem(miView, "Sort by date updated (most recent first)", "Sort-RecentFirst", Keys.Alt | Keys.Shift | Keys.D,    viewSortRecentClicked);
            miViewSortOldest   = addItem(miView, "Sort by date updated (oldest first)",     "Sort-OldestFirst", Keys.Alt | Keys.D,                 viewSortOldestClicked);
            miViewResetSort    = addItem(miView, "Reset sort",                         "Reset-Sort",        Keys.None,                          viewResetSortClicked);
            addSep(miView);
            miViewSelectColumn = addItem(miView, "Select-Column (visible columns)...", "Select-Column",     Keys.None,                          viewSelectColumnClicked);
            addSep(miView);
            miViewUpdate       = addItem(miView, "&Update-View (refresh)",              "Update-View",       Keys.F5,                            viewUpdateClicked);

            // Note: Format-Table / Format-List were removed. The
            // DataGridView is the most accessible view and is always
            // active. Show-Record (Enter) gives a per-record textual
            // dump for tables with many columns. Control+1 and
            // Control+2 are now free for future use.

            miSchema = addMenu("Sc&hema");
            miSchemaSelectTable = addItem(miSchema, "&Select-Table (base tables only)...", "Select-Table",       Keys.F4,                            schemaSelectTableClicked);
            miSchemaSelectView  = addItem(miSchema, "Select-&View (views only)...",        "Select-View",        Keys.Shift | Keys.F4,               schemaSelectViewClicked);
            miSchemaSwitch      = addItem(miSchema, "Switch-Table (next visited)",      "Switch-Table",         Keys.Control | Keys.Tab,            schemaSwitchClicked);
            miSchemaSwitchPrev  = addItem(miSchema, "Switch-TablePrevious (previous visited)", "Switch-TablePrevious", Keys.Control | Keys.Shift | Keys.Tab, schemaSwitchPrevClicked);
            miSchemaSwitchAll     = addItem(miSchema, "Switch-Object (next of all tables and views)", "Switch-Object",         Keys.Control | Keys.F6,            schemaSwitchAllClicked);
            miSchemaSwitchAllPrev = addItem(miSchema, "Switch-ObjectPrevious (previous of all)",       "Switch-ObjectPrevious", Keys.Control | Keys.Shift | Keys.F6, schemaSwitchAllPrevClicked);
            miSchemaShow        = addItem(miSchema, "Show-Schema (all tables)", "Show-Schema",        Keys.None,                          schemaShowClicked);
            miSchemaProperties  = addItem(miSchema, "Get-&Property (current table details)", "Get-Property", Keys.Alt | Keys.Enter,        schemaPropertiesClicked);

            miTools = addMenu("&Tools");
            miToolsTest       = addItem(miTools, "&Test-Database integrity",  "Test-Database",     Keys.None,                          toolsTestClicked);
            miToolsMeasure    = addItem(miTools, "&Measure-Table statistics", "Measure-Table",     Keys.None,                          toolsMeasureClicked);
            miToolsInvokeSql  = addItem(miTools, "Invoke-Sql...",            "Invoke-Sql",        Keys.Control | Keys.Q,              toolsInvokeSqlClicked);
            addSep(miTools);
            miToolsLock       = addItem(miTools, "Toggle &Lock (read-only)",  "Lock-Database",     Keys.Control | Keys.F7,             toolsLockClicked);
            miToolsTestDriver = addItem(miTools, "Test-&Driver (probe ODBC and OLE DB)", "Test-Driver", Keys.None,                       toolsTestDriverClicked);
            miToolsOpenFolder = addItem(miTools, "Open-FileFolder (Explorer at database)", "Open-FileFolder", Keys.Alt | Keys.OemPipe,  toolsOpenFolderClicked);
            addSep(miTools);
            miToolsEditConfig = addItem(miTools, "&Edit-Configuration (opens DbDuo.ini)", "Edit-Configuration", Keys.F12,             toolsEditConfigClicked);
            miToolsConsole    = addItem(miTools, "Enter-&Console (dot prompt)",  "Enter-Console",     Keys.Control | Keys.Oemtilde,    toolsConsoleClicked);

            miHelp = addMenu("Hel&p");
            miHelpContents     = addItem(miHelp, "Help &Contents",            "Get-Help",          Keys.F1,                            helpContentsClicked);
            miHelpVerbs        = addItem(miHelp, "PowerShell Verb &Reference", "Get-Verb",          Keys.None,                          helpVerbsClicked);
            addSep(miHelp);
            miHelpShowCommand  = addItem(miHelp, "&Show-Command (alternate menu)", "Show-Command",  Keys.Alt | Keys.F10,                helpShowCommandClicked);
            miHelpStatus       = addItem(miHelp, "&Where am I (Show-Status)",  "Show-Status",       Keys.Control | Keys.F1,             helpStatusClicked);
            miHelpTestReader   = addItem(miHelp, "Test-Reader (which screen reader speech path)", "Test-Reader",     Keys.None,                          helpTestReaderClicked);
            miHelpTraceCommand = addItem(miHelp, "Toggle &Trace-Command mode", "Trace-Command",     Keys.Alt | Keys.Control | Keys.F1,  helpTraceCommandClicked);
            addSep(miHelp);
            miHelpLog          = addItem(miHelp, "Show &Log location",          "Show-Log",          Keys.None,                          helpLogClicked);
            miHelpWebSite      = addItem(miHelp, "Open-We&bSite (DbDuo on GitHub)", "Open-WebSite",  Keys.None,                          helpWebSiteClicked);
            miHelpAbout        = addItem(miHelp, "&About",                    "About",             Keys.Alt | Keys.F1,                 helpAboutClicked);

            // Get-Property: secondary alias on Shift+F6 (EdSharp's
            // "Go to Contents" -- structural where-am-I).
            KeyMap.registerAlias(Keys.Shift | Keys.F6, miSchemaProperties);

            this.MainMenuStrip = menuMain;
            this.Controls.Add(menuMain);
        }

        private ToolStripMenuItem addMenu(string sText)
        {
            ToolStripMenuItem oItem = new ToolStripMenuItem(sText);
            oItem.AccessibleName = sText.Replace("&", "");
            menuMain.Items.Add(oItem);
            return oItem;
        }

        private ToolStripMenuItem addItem(ToolStripMenuItem oParent, string sText, string sCommand, Keys oKey, EventHandler oHandler)
        {
            ToolStripMenuItem oItem = new ToolStripMenuItem(sText);
            oItem.Click += oHandler;
            oParent.DropDownItems.Add(oItem);
            KeyMap.register(oKey, oItem, sCommand);
            return oItem;
        }

        // addItemLocal: like addItem, but the chord is dispatched
        // locally by a specific control (the data grid in DbDuo)
        // rather than by the form's ProcessCmdKey. The menu UI
        // still shows the chord and JAWS still announces it; the
        // form-level KeyMap dispatch table simply doesn't claim
        // it. Used for the Shift+Letter family so capital letters
        // typed in text boxes and dialogs are not intercepted.
        private ToolStripMenuItem addItemLocal(ToolStripMenuItem oParent, string sText, string sCommand, Keys oKey, EventHandler oHandler)
        {
            ToolStripMenuItem oItem = new ToolStripMenuItem(sText);
            oItem.Click += oHandler;
            oParent.DropDownItems.Add(oItem);
            KeyMap.registerDisplayOnly(oKey, oItem, sCommand);
            return oItem;
        }

        private void addSep(ToolStripMenuItem oParent)
        {
            oParent.DropDownItems.Add(new ToolStripSeparator());
        }

        // =====================================================================
        // Grid construction. Cell-level keyboard navigation, type-ahead
        // jump, and the cell-changed handler that syncs the recordset's
        // absolutePosition.
        // =====================================================================
        private void buildGrid()
        {
            grid = new ListView();
            grid.AccessibleName = "Records";
            grid.AccessibleRole = AccessibleRole.List;
            grid.Dock = DockStyle.Fill;
            grid.View = View.Details;
            grid.FullRowSelect = true;
            grid.MultiSelect = false;
            grid.HideSelection = false;
            grid.GridLines = false;
            grid.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            grid.LabelEdit = false;
            grid.UseCompatibleStateImageBehavior = false;
            grid.ShowItemToolTips = false;

            // Virtual mode: never materialize ListViewItems for the
            // whole table. The ListView calls RetrieveVirtualItem(N)
            // when it needs row N for display; we move the ADO
            // cursor to that position and build a ListViewItem from
            // the current field values.
            grid.VirtualMode = true;
            grid.VirtualListSize = 0;
            grid.RetrieveVirtualItem += gridRetrieveVirtualItem;
            grid.CacheVirtualItems += gridCacheVirtualItems;

            // Type-ahead search: ObjectListView's IsSearchOnSortColumn
            // pattern. When the user presses a letter inside the
            // list, jump to the next row whose first-displayed
            // column starts with that letter. WinForms' virtual
            // ListView raises SearchForVirtualItem for this; we
            // walk the recordset for the next match.
            grid.SearchForVirtualItem += gridSearchForVirtualItem;

            // Row change: when the focused item changes, move the
            // ADO recordset's cursor to match so commands like
            // Show-Record, Copy-Record, and Set-Mark act on the
            // row the user is reading.
            grid.SelectedIndexChanged += gridSelectedIndexChanged;
            grid.ItemSelectionChanged += gridItemSelectionChanged;

            // Column navigation within a row: Tab and Shift+Tab
            // step iCurrentColumnIndex; arrow keys reset it. We
            // also announce the new column on each Tab so the user
            // hears which column they are on. This matches the
            // mental model that Sort-Ascending and similar commands
            // act on "the current column" -- the column the user
            // last moved to.
            grid.KeyDown += gridKeyDown;

            ctxGrid = new ContextMenuStrip();
            ctxGrid.Items.Add("Show-Object", null, recShowClicked);
            ctxGrid.Items.Add("Set-Record...", null, recSetClicked);
            ctxGrid.Items.Add("Remove-Record", null, recRemoveClicked);
            ctxGrid.Items.Add("-");
            ctxGrid.Items.Add("Copy-Record", null, recCopyClicked);
            grid.ContextMenuStrip = ctxGrid;

            this.Controls.Add(grid);
        }

        // Virtual-mode callbacks: the ListView asks for one row at a
        // time as it scrolls. We move the ADO cursor to row N and
        // build a ListViewItem with one subitem per column.
        private void gridRetrieveVirtualItem(object oSender, RetrieveVirtualItemEventArgs oArgs)
        {
            if (db == null || !db.hasRecordset())
            {
                oArgs.Item = new ListViewItem("");
                return;
            }
            try
            {
                int iAdoPos = oArgs.ItemIndex + 1; // ADO is 1-based
                if (db.absolutePosition != iAdoPos)
                {
                    bSuppressCellChanged = true;
                    try { db.absolutePosition = iAdoPos; }
                    finally { bSuppressCellChanged = false; }
                }
                List<string> lFields = db.getDisplayFieldNames();
                string[] aRow = new string[lFields.Count];
                for (int i = 0; i < lFields.Count; i++)
                {
                    string sCol = lFields[i];
                    if (db.isFieldBinary(sCol))
                    {
                        int iLen = db.getFieldByteLength(sCol);
                        aRow[i] = (iLen > 0) ? "[BLOB, " + iLen + " bytes]"
                                : (iLen == 0) ? "" : "[BLOB]";
                    }
                    else
                    {
                        aRow[i] = formatCellValue(db.getFieldValue(sCol));
                    }
                }
                ListViewItem oItem = new ListViewItem(aRow);
                oArgs.Item = oItem;
            }
            catch
            {
                oArgs.Item = new ListViewItem("");
            }
        }

        // CacheVirtualItems: the ListView tells us a window of items
        // it is about to need. We could pre-fetch here, but our
        // ADO cursor is a single seek anyway; pre-fetching wouldn't
        // help. Leave as a no-op; RetrieveVirtualItem handles
        // each row as it is asked for.
        private void gridCacheVirtualItems(object oSender, CacheVirtualItemsEventArgs oArgs)
        {
            // intentionally empty
        }

        // Type-ahead search: walk the recordset for the next row
        // whose value in the user-selected column (defaulting to
        // the current displayed column under Tab focus, falling
        // back to the first displayed column) starts with the
        // typed text. Inspired by ObjectListView's IsSearchOnSortColumn.
        //
        // Returns the index of the matching row through Index on
        // the event args; the ListView then focuses that row.
        // Returns -1 (Index unset) if no match found.
        private void gridSearchForVirtualItem(object oSender, SearchForVirtualItemEventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            if (string.IsNullOrEmpty(oArgs.Text)) return;
            string sNeedle = oArgs.Text;
            // Pick the column to search: the user's Tab-focused
            // column if any, else column 0 (first displayed).
            int iCol = iCurrentColumnIndex;
            if (iCol < 0 || iCol >= grid.Columns.Count) iCol = 0;
            string sFieldName = grid.Columns[iCol].Text;
            if (string.IsNullOrEmpty(sFieldName)) return;

            // Walk forward from StartIndex+1, wrapping at the end.
            // Save and restore the cursor position so the search
            // doesn't leave the recordset displaced.
            int iTotal = db.recordCount;
            if (iTotal <= 0) return;
            int iStart = oArgs.StartIndex;
            if (iStart < 0 || iStart >= iTotal) iStart = 0;
            int iSavedPos = db.absolutePosition;
            bSuppressCellChanged = true;
            try
            {
                for (int i = 1; i <= iTotal; i++)
                {
                    int iCheck = (iStart + i) % iTotal;
                    int iAdoPos = iCheck + 1;
                    try { db.absolutePosition = iAdoPos; } catch { continue; }
                    string sValue = "";
                    try { sValue = db.getFieldValue(sFieldName) ?? ""; } catch { }
                    if (sValue.StartsWith(sNeedle, StringComparison.OrdinalIgnoreCase))
                    {
                        oArgs.Index = iCheck;
                        return;
                    }
                }
            }
            finally
            {
                try { if (iSavedPos > 0) db.absolutePosition = iSavedPos; } catch { }
                bSuppressCellChanged = false;
            }
            // Not found: leave oArgs.Index at its default (-1).
        }

        // Aspect-style formatter: produce a readable display string
        // from the raw field value ADO returns. Inspired by
        // ObjectListView's AspectToStringConverter pattern. Three
        // transformations:
        //
        //   null/empty                  -> ""
        //   raw datetime stamp          -> "yyyy-MM-dd HH:mm" if it
        //                                  has time, "yyyy-MM-dd"
        //                                  otherwise
        //   string with trailing newline-> trimmed (newlines confuse
        //                                  screen readers reading a
        //                                  cell value)
        //
        // The recordset's getFieldValue currently returns a string
        // already, so date detection is best-effort: try to parse
        // as DateTime; if successful, reformat.
        private string formatCellValue(string sValue)
        {
            if (sValue == null) return "";
            if (sValue.Length == 0) return "";
            // Reformat values that look unambiguously like dates.
            // Require: length >= 8 (yyyy-m-d minimum), contains
            // at least one of '-' or '/' or ':' (date or time
            // separators). This filters out short numeric strings
            // like "1", "100", or "2024" that DateTime.TryParse
            // would otherwise coerce into the current month/year.
            if (sValue.Length >= 8
                && (sValue.IndexOf('-') >= 0 || sValue.IndexOf('/') >= 0 || sValue.IndexOf(':') >= 0))
            {
                DateTime dt;
                if (DateTime.TryParse(sValue, out dt))
                {
                    if (dt.TimeOfDay.TotalSeconds == 0)
                        return dt.ToString("yyyy-MM-dd");
                    return dt.ToString("yyyy-MM-dd HH:mm");
                }
            }
            // Strip trailing newlines/whitespace so multi-line
            // text values like notes don't bleed into next row.
            return sValue.TrimEnd('\r', '\n', ' ', '\t');
        }

        private void gridSelectedIndexChanged(object oSender, EventArgs oArgs)
        {
            if (bSuppressCellChanged) return;
            if (db == null || !db.hasRecordset()) return;
            if (grid.SelectedIndices.Count == 0) return;
            int iRow = grid.SelectedIndices[0];
            try { db.absolutePosition = iRow + 1; } catch { }
            updateStatusBar();
        }

        private void gridItemSelectionChanged(object oSender, ListViewItemSelectionChangedEventArgs oArgs)
        {
            // Arrow-key row movement clears the column index so the
            // next Sort-Ascending sorts by the first column unless
            // the user has explicitly Tab'd to another column.
            iCurrentColumnIndex = 0;
        }

        // ListView KeyDown: intercept Tab and Shift+Tab to step
        // through columns within the focused row. The ListView's
        // default behavior is to move focus to the next control;
        // we override it for in-row column navigation, then
        // announce the new column.
        //
        // This handler also recognizes the bare Shift+Letter
        // shortcut family. Because the dispatch lives on the
        // ListView's own KeyDown event, the chord only fires when
        // the data grid has focus. When focus is in a text box,
        // dialog edit field, combo box, or the menu bar, this
        // handler does not run, so capital letters typed into
        // those controls reach them as plain character input --
        // no focus check is needed at the dispatch site. The
        // pattern is borrowed from FileDir.cs's MdiChild.
        // ListBox_KeyDown.
        //
        // Type-ahead navigation in the data grid is case-
        // insensitive: lowercase letters jump to rows by initial
        // character through the ListView's virtual-mode
        // SearchForVirtualItem event. The Shift+Letter shortcuts
        // override capital letters for those nine specific
        // letters (E F G J M R S U X); lowercase forms of the
        // same letters continue to type-ahead. The user-side
        // convention: lowercase to navigate, uppercase to
        // shortcut.
        private void gridKeyDown(object oSender, KeyEventArgs oArgs)
        {
            // Tab / Shift+Tab: in-row column navigation.
            if (oArgs.KeyCode == Keys.Tab)
            {
                // Control+Tab and Control+Shift+Tab are reserved for
                // Switch-Table / Switch-TablePrevious -- let them
                // bubble up so the form's ProcessCmdKey dispatches
                // them to the Schema menu items.
                if (oArgs.Control) return;
                int iColCount = grid.Columns.Count;
                if (iColCount == 0) return;
                if (oArgs.Shift)
                    iCurrentColumnIndex = (iCurrentColumnIndex - 1 + iColCount) % iColCount;
                else
                    iCurrentColumnIndex = (iCurrentColumnIndex + 1) % iColCount;
                announceCurrentColumn();
                oArgs.Handled = true;
                oArgs.SuppressKeyPress = true;
                return;
            }

            // Bare Shift+Letter shortcut family. Only the nine
            // letters below are bound; any other Shift+Letter falls
            // through to the ListView, which feeds the character
            // to its virtual-mode SearchForVirtualItem for type-
            // ahead navigation. The corresponding menu items have
            // ShortcutKeyDisplayString set so the chord appears in
            // the menu UI and JAWS announces it; the dispatch
            // happens here, not through the form-level KeyMap.
            if (oArgs.Shift && !oArgs.Control && !oArgs.Alt)
            {
                ToolStripMenuItem oTarget = null;
                switch (oArgs.KeyCode)
                {
                    case Keys.E: oTarget = miRecEnterChild;    break;
                    case Keys.F: oTarget = miViewSelect;       break;
                    case Keys.G: oTarget = miRecGoTo;          break;
                    case Keys.J: oTarget = miRecFind;          break;
                    case Keys.M: oTarget = miRecMark;          break;
                    case Keys.R: oTarget = miViewResetFilter;  break;
                    case Keys.S: oTarget = miViewFormat;       break;
                    case Keys.U: oTarget = miRecUnmark;        break;
                    case Keys.X: oTarget = miRecExitChild;     break;
                }
                if (oTarget != null)
                {
                    if (!oTarget.Enabled)
                    {
                        string sCmd = oTarget.Text.Replace("&", "");
                        if (sCmd.EndsWith("...")) sCmd = sCmd.Substring(0, sCmd.Length - 3).TrimEnd();
                        LiveRegion.say(sCmd + " is unavailable right now (open a database file or select a table first)");
                    }
                    else if (KeyMap.bTraceMode)
                    {
                        // Trace-Command mode: show what the chord
                        // would have done instead of doing it. Mirrors
                        // the form-level tryDispatch behavior so the
                        // Shift+Letter family is traceable too.
                        string sCmd = KeyMap.dMenuToCommand.ContainsKey(oTarget)
                            ? KeyMap.dMenuToCommand[oTarget]
                            : oTarget.Text.Replace("&", "");
                        MessageBox.Show(this,
                            string.Format("Trace-Command:\n\nKey: Shift+{0}\nCommand: {1}", oArgs.KeyCode, sCmd),
                            "Trace-Command", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        oTarget.PerformClick();
                    }
                    oArgs.Handled = true;
                    oArgs.SuppressKeyPress = true;
                    return;
                }
            }
        }

        // Speak the column header and the value of the focused row
        // for the current column. Used after Tab navigation so the
        // user hears which column they are on and what its current
        // value is.
        private void announceCurrentColumn()
        {
            if (grid == null || grid.Columns.Count == 0) return;
            int iCol = iCurrentColumnIndex;
            if (iCol < 0 || iCol >= grid.Columns.Count) iCol = 0;
            string sHeader = grid.Columns[iCol].Text;
            string sValue = "";
            if (grid.SelectedIndices.Count > 0 && db != null && db.hasRecordset())
            {
                // The ListView columns are the display subset of the
                // recordset's field set; the column header text IS
                // the field name (we set them equal in updateGrid).
                // Look up the value by name rather than by index, so
                // hidden columns don't shift our reference.
                try { sValue = formatCellValue(db.getFieldValue(sHeader)); }
                catch { }
            }
            LiveRegion.say(sHeader + ": " + sValue);
        }

        private void buildStatusBar()
        {
            statusBar = new StatusStrip();
            // Left section: filter and sort indicators when active.
            // The filename and current-table name are now in the
            // window title, not the status bar.
            lblTable = new ToolStripStatusLabel("");
            lblTable.AccessibleName = "View state";
            lblTable.Spring = true;
            lblTable.TextAlign = ContentAlignment.MiddleLeft;

            // Right section: row position and marked status. Read
            // via the screen reader's "say status bar" hotkey
            // (JAWS Insert+PageDown, NVDA NVDA+End), or by Tab-
            // navigating to the status strip.
            lblStatus = new ToolStripStatusLabel("");
            lblStatus.AccessibleName = "Row position";
            lblStatus.Spring = true;
            lblStatus.TextAlign = ContentAlignment.MiddleRight;

            statusBar.Items.Add(lblTable);
            statusBar.Items.Add(lblStatus);
            this.Controls.Add(statusBar);
        }

        // =====================================================================
        // updateGrid: rebuild the ListView's columns from the current
        // recordset's field metadata, set the virtual size to the
        // recordset's row count, and position selection at the
        // current record. No full materialization -- row data is
        // pulled on demand by gridRetrieveVirtualItem as the
        // ListView scrolls. Suppresses the SelectedIndexChanged
        // event during the rebuild so we don't echo our own
        // programmatic position changes back to the recordset.
        // =====================================================================
        private void updateGrid()
        {
            bSuppressCellChanged = true;
            try
            {
                if (db == null || !db.isOpen() || !db.hasRecordset())
                {
                    grid.VirtualListSize = 0;
                    grid.Columns.Clear();
                    oCurrentData = null;
                    updateStatusBar();
                    updateMenuEnabled();
                    return;
                }

                List<string> lFields = db.getDisplayFieldNames();

                // Rebuild columns only if the display-field set
                // changed (different table or schema change).
                // Rebuilding when not needed clears the user's
                // manual column widths, which is annoying.
                bool bColumnsMatch = (grid.Columns.Count == lFields.Count);
                if (bColumnsMatch)
                {
                    for (int i = 0; i < lFields.Count; i++)
                    {
                        if (!string.Equals(grid.Columns[i].Text, lFields[i],
                                StringComparison.Ordinal))
                        { bColumnsMatch = false; break; }
                    }
                }
                if (!bColumnsMatch)
                {
                    grid.Columns.Clear();
                    foreach (string sCol in lFields)
                    {
                        ColumnHeader oCh = new ColumnHeader();
                        oCh.Text = sCol;
                        oCh.Name = sCol;
                        oCh.Width = 150;
                        grid.Columns.Add(oCh);
                    }
                }

                int iCount = db.recordCount;
                grid.VirtualListSize = (iCount > 0) ? iCount : 0;

                // Empty-list announcement: borrow from ObjectListView's
                // "Displays a 'list is empty' message" feature. The
                // standard ListView has no built-in empty-state UI,
                // so we announce it through the live region. Fires
                // only when the count is freshly zero (a table was
                // selected and turned out to have no rows), not on
                // every refresh.
                if (iCount == 0 && !bAnnouncedEmpty)
                {
                    bAnnouncedEmpty = true;
                    LiveRegion.say("Table has no rows");
                }
                else if (iCount > 0)
                {
                    bAnnouncedEmpty = false;
                }

                // Position selection at the recordset's current row.
                int iPos = db.absolutePosition; // 1-based
                if (iPos >= 1 && iPos <= iCount)
                {
                    int iIndex = iPos - 1;
                    try
                    {
                        grid.SelectedIndices.Clear();
                        if (iIndex >= 0 && iIndex < grid.VirtualListSize)
                        {
                            grid.SelectedIndices.Add(iIndex);
                            grid.EnsureVisible(iIndex);
                            grid.FocusedItem = grid.Items[iIndex];
                        }
                    }
                    catch { }
                }

                // Column tracker resets to first column whenever the
                // ListView is rebuilt.
                iCurrentColumnIndex = 0;

                // No materialized data cache; the ListView is the
                // single source of truth and pulls rows on demand.
                oCurrentData = null;
            }
            finally
            {
                bSuppressCellChanged = false;
                updateStatusBar();
                updateMenuEnabled();
            }
        }

        // Legacy hook from when the field type was DataGridView.
        // Selection-changed routing now goes through
        // gridSelectedIndexChanged (registered in buildGrid). This
        // empty method is kept so old call sites that referenced it
        // by name still compile; can be removed once those are
        // migrated.
        private void gridCurrentCellChanged(object oSender, EventArgs oArgs)
        {
            // no-op
        }

        private void updateStatusBar()
        {
            if (db == null || !db.isOpen())
            {
                this.Text = "DbDuo";
                lblTable.Text = "";
                lblStatus.Text = "";
                return;
            }
            // Window title format: "DbDuo - <database> [read-only] - <table>".
            // The program name is first so screen-reader title announcements
            // and Alt+Tab previews start with "DbDuo", which is more useful
            // than starting with whatever .db file is open. Read-only state
            // is appended to the database name so the user always knows when
            // they can't edit.
            string sBaseName = Path.GetFileName(db.filePath);
            string sTable = string.IsNullOrEmpty(db.currentTable) ? "(no table)" : db.currentTable;
            string sReadOnly = db.readOnly ? " (read-only)" : "";
            this.Text = "DbDuo - " + sBaseName + sReadOnly + " - " + sTable;

            if (!db.hasRecordset())
            {
                lblTable.Text = "";
                lblStatus.Text = "";
                return;
            }

            // Left status: filter and sort indicators when active.
            string sFilter = string.IsNullOrEmpty(db.filter) ? "" : "filter: " + db.filter;
            string sSort = string.IsNullOrEmpty(db.sort) ? "" : "sort: " + db.sort;
            string sViewState = "";
            if (sFilter.Length > 0 && sSort.Length > 0) sViewState = sFilter + "  " + sSort;
            else if (sFilter.Length > 0) sViewState = sFilter;
            else if (sSort.Length > 0) sViewState = sSort;
            lblTable.Text = sViewState;

            // Right status. Order (user-specified): marked (only
            // when true), relative position, updated date.
            //
            //   marked     - the boolean 'marked' standard field;
            //                appears only when true so the user
            //                hears it as a noteworthy signal.
            //   row N of M - the row-of-total counter.
            //   updated    - the date portion of the 'updated'
            //                standard field (yyyy-MM-dd, no time).
            //
            // All three sections are separated by two spaces so the
            // screen reader pauses naturally between them when
            // reading the status bar.
            string sMarked = "";
            if (db.hasField(Metadata.MarkedColumn))
            {
                try
                {
                    string sVal = db.getFieldValue(Metadata.MarkedColumn) ?? "";
                    sVal = sVal.Trim().ToLowerInvariant();
                    bool bMarked = (sVal == "1" || sVal == "true" || sVal == "yes" || sVal == "-1");
                    if (bMarked) sMarked = "marked";
                }
                catch { }
            }

            string sUpdated = "";
            if (db.hasField("updated"))
            {
                try
                {
                    string sRaw = db.getFieldValue("updated") ?? "";
                    if (sRaw.Length > 0)
                    {
                        DateTime dt;
                        if (DateTime.TryParse(sRaw, out dt))
                            sUpdated = "updated " + dt.ToString("yyyy-MM-dd");
                        else
                        {
                            // Fall back to first 10 characters if
                            // the value is already in yyyy-MM-dd
                            // form but TryParse didn't accept it
                            // (e.g. unusual culture settings).
                            if (sRaw.Length >= 10) sUpdated = "updated " + sRaw.Substring(0, 10);
                        }
                    }
                }
                catch { }
            }

            List<string> lParts = new List<string>();
            if (sMarked.Length > 0)  lParts.Add(sMarked);
            lParts.Add(string.Format("row {0} of {1}", db.absolutePosition, db.recordCount));
            if (sUpdated.Length > 0) lParts.Add(sUpdated);
            lblStatus.Text = string.Join("  ", lParts.ToArray());
        }

        private void updateMenuEnabled()
        {
            bool bOpen = db != null && db.isOpen();
            bool bHasTable = bOpen && db.hasRecordset();
            bool bWritable = bOpen && !db.readOnly;

            miFileSaveAs.Enabled = bOpen;
            miFileClose.Enabled = bOpen;
            miFileBackup.Enabled = bOpen;
            miFileCompare.Enabled = bOpen;
            miFileImport.Enabled = bWritable;
            miFileExport.Enabled = bHasTable;
            miFilePrint.Enabled = bHasTable;

            miRecNew.Enabled = bWritable && bHasTable;
            miRecSet.Enabled = bWritable && bHasTable;
            miRecRemove.Enabled = bWritable && bHasTable;
            miRecShow.Enabled = bHasTable;
            miRecCopy.Enabled = bHasTable;
            miRecFind.Enabled = bHasTable;
            miRecFindNext.Enabled = bHasTable && sLastFindCriteria.Length > 0;
            miRecFindPrev.Enabled = bHasTable && sLastFindCriteria.Length > 0;
            miRecMark.Enabled = bWritable && bHasTable;
            miRecUnmark.Enabled = bWritable && bHasTable;
            miRecUpdateField.Enabled = bWritable && bHasTable;
            miRecRelated.Enabled = bHasTable;
            miRecEnterChild.Enabled = bHasTable;
            // Exit-Child is enabled only when there is something on
            // the drill stack to pop back to.
            miRecExitChild.Enabled = bHasTable && oDrillStack.Count > 0;
            miRecGoTo.Enabled = bHasTable;
            miRecBookmark.Enabled = bHasTable;
            miRecGotoBookmark.Enabled = bHasTable && oSavedBookmark != null;
            miRecClearBookmark.Enabled = oSavedBookmark != null;
            miRecOpenCell.Enabled = bHasTable;

            miViewSelect.Enabled = bHasTable;
            miViewResetFilter.Enabled = bHasTable && db.filter.Length > 0;
            miViewFormat.Enabled = bHasTable;
            miViewSortAsc.Enabled = bHasTable;
            miViewSortDesc.Enabled = bHasTable;
            miViewSortRecent.Enabled = bHasTable;
            miViewSortOldest.Enabled = bHasTable;
            miViewResetSort.Enabled = bHasTable && db.sort.Length > 0;
            miViewUpdate.Enabled = bHasTable;

            miSchemaSelectTable.Enabled = bOpen;
            miSchemaSelectView.Enabled = bOpen;
            miSchemaSwitch.Enabled = bOpen;
            miSchemaSwitchPrev.Enabled = bOpen;
            miSchemaSwitchAll.Enabled = bOpen;
            miSchemaSwitchAllPrev.Enabled = bOpen;
            miSchemaShow.Enabled = bOpen;

            miToolsTest.Enabled = bOpen;
            miToolsMeasure.Enabled = bHasTable;
            miToolsInvokeSql.Enabled = bOpen;
            miToolsLock.Enabled = bOpen;
            // miToolsTestDriver, miToolsOpenFolder, and miToolsConsole are always enabled.
        }

        // =====================================================================
        // ProcessCmdKey: the universal keystroke dispatcher. Consults
        // KeyMap.dKeyToMenu to find a menu item to PerformClick(). This
        // bypasses ToolStripMenuItem.ShortcutKeys validation, so plain
        // Enter, Insert, Delete, Tab, Escape, Backspace, Alt+F4 are
        // all bindable.
        //
        // Special-cased keys that don't go through KeyMap because they
        // operate at the session level (no menu home) but should work
        // anywhere in the form, including from inside the data grid:
        //   - Control+Tab        => cycle to next previously-opened table
        //   - Control+Shift+Tab  => cycle to previous previously-opened table
        //   - Control+Page Down  => same as Control+Tab
        //   - Control+Page Up    => same as Control+Shift+Tab
        //   - Alt+Home           => first marked row
        //   - Alt+End            => last marked row
        //   - Alt+Up             => previous marked row
        //   - Alt+Down           => next marked row
        // Mark-navigation hotkeys use Alt-modified keys deliberately
        // so DataGridView's standard Control+Home / Control+End /
        // Control+Up / Control+Down (jump to first cell / last cell /
        // up one cell / down one cell) keep their default behavior.
        // Screen reader users and keyboard users rely on those grid
        // defaults, so DbDuo does not override them. Alt+Home etc. are
        // unused anywhere else in DbDuo and (outside of browsers' "go
        // home" shortcut) have no entrenched Windows meaning, making
        // them safe to repurpose. FileDir uses similar Alt-modified
        // keys for tagged-file navigation; DbDuo applies the pattern
        // to the 'marked' column convention. Tables without a 'marked'
        // column ignore the keystroke with a brief live-region notice.
        // The cycling is over the *session-visited* tables, in the
        // order they were first opened. Settings (filter, sort, current
        // record) are restored from the per-table cache on each switch.
        // Record stepping in the grid still uses the arrow keys (the
        // grid's native behavior); record-level next/previous does not
        // need a global hotkey.
        // =====================================================================
        protected override bool ProcessCmdKey(ref Message oMsg, Keys oKeyData)
        {
            // Control+PageDown / Control+PageUp are alias keys for
            // Switch-Table / Switch-TablePrevious. The primary
            // bindings (Control+Tab and Control+Shift+Tab) are wired
            // to the Schema menu items and dispatched through KeyMap;
            // these PageDown/PageUp aliases are caught here because
            // they don't have a menu binding of their own.
            if (oKeyData == (Keys.Control | Keys.PageDown))
            {
                cycleVisitedTable(true);
                return true;
            }
            if (oKeyData == (Keys.Control | Keys.PageUp))
            {
                cycleVisitedTable(false);
                return true;
            }
            // Alt+Home/End/Up/Down for marked-row navigation. Same
            // intercept lives in formKeyDown as a safety backstop
            // because the menu strip can sometimes intercept Alt+
            // keystrokes during ProcessCmdKey routing.
            if (oKeyData == (Keys.Alt | Keys.Home))
            {
                jumpToMarkedRow(MarkJump.First);
                return true;
            }
            if (oKeyData == (Keys.Alt | Keys.End))
            {
                jumpToMarkedRow(MarkJump.Last);
                return true;
            }
            if (oKeyData == (Keys.Alt | Keys.Up))
            {
                jumpToMarkedRow(MarkJump.Previous);
                return true;
            }
            if (oKeyData == (Keys.Alt | Keys.Down))
            {
                jumpToMarkedRow(MarkJump.Next);
                return true;
            }

            if (KeyMap.tryDispatch(oKeyData, this)) return true;
            return base.ProcessCmdKey(ref oMsg, oKeyData);
        }

        // Mark-navigation directions. Used by jumpToMarkedRow to share
        // the precondition checks (table open, has marked column, has
        // any marked rows) across the four hotkey paths.
        private enum MarkJump { First, Last, Next, Previous }

        // Move the row pointer to a marked row in the requested
        // direction. Refuses gracefully (no exception, brief live-
        // region notice) when the table has no 'marked' column or no
        // rows are currently marked.
        private void jumpToMarkedRow(MarkJump oDir)
        {
            if (db == null || !db.hasRecordset()) return;
            if (!db.hasField(Metadata.MarkedColumn))
            {
                LiveRegion.say("This table has no marked column");
                return;
            }
            int iTarget = findMarkedRowPosition(oDir);
            if (iTarget <= 0)
            {
                LiveRegion.say("No marked rows");
                return;
            }
            try
            {
                db.absolutePosition = iTarget;
                invokeRefresh();
                LiveRegion.say("Marked row " + iTarget);
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Marked Row", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Walk the current recordset to find the next marked-row
        // position in the given direction. Returns 0 if no marked row
        // exists in that direction.
        //
        // The walk respects the recordset's current Filter and Sort,
        // because we step through ADO's view, not the underlying
        // table. So Control+Up after a Sort-Object sort moves to the
        // previous marked row IN SORTED ORDER, which matches user
        // intent.
        //
        // We save and restore the bookmark around the walk so the
        // pointer ends up exactly where we want it (at the target
        // row), with no flicker visible to the screen reader.
        private int findMarkedRowPosition(MarkJump oDir)
        {
            int iStart = db.absolutePosition;
            int iCount = db.recordCount;
            if (iCount <= 0) return 0;

            // Compute the iteration plan: where to start, what step,
            // when to stop.
            int iFrom, iStep, iEnd;
            switch (oDir)
            {
                case MarkJump.First:
                    iFrom = 1; iStep = 1; iEnd = iCount + 1; break;
                case MarkJump.Last:
                    iFrom = iCount; iStep = -1; iEnd = 0; break;
                case MarkJump.Next:
                    iFrom = iStart + 1; iStep = 1; iEnd = iCount + 1; break;
                case MarkJump.Previous:
                    iFrom = iStart - 1; iStep = -1; iEnd = 0; break;
                default:
                    return 0;
            }

            // Save the original position so we don't leave the cursor
            // somewhere unexpected if no marked row is found.
            object oOriginal = null;
            try { oOriginal = db.bookmark; } catch { }

            int iFound = 0;
            try
            {
                for (int i = iFrom; i != iEnd; i += iStep)
                {
                    if (i < 1 || i > iCount) continue;
                    db.absolutePosition = i;
                    string sValue = db.getFieldValue(Metadata.MarkedColumn);
                    if (isMarkedTrue(sValue))
                    {
                        iFound = i;
                        break;
                    }
                }
            }
            catch { /* fall through; restore happens below */ }

            // Restore the original position if we didn't find a target.
            if (iFound == 0 && oOriginal != null)
            {
                try { db.bookmark = oOriginal; } catch { }
            }
            return iFound;
        }

        // The 'marked' column's truthy values across the database
        // engines DbDuo speaks: SQLite stores it as integer 0/1,
        // Jet/ACE as BIT True/False. ADO normalizes BIT to "True"/
        // "False" string when read through the dynamic late-bound
        // path. Normalize all of these to a single boolean.
        private static bool isMarkedTrue(string sValue)
        {
            if (string.IsNullOrEmpty(sValue)) return false;
            sValue = sValue.Trim();
            if (sValue == "1") return true;
            if (string.Equals(sValue, "True", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(sValue, "Yes",  StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // Cycle through the list of tables visited in this session.
        // bForward=true moves to the next table; false to the previous.
        // Wraps around at the ends. Does nothing if fewer than two
        // tables have been visited.
        private void cycleVisitedTable(bool bForward)
        {
            if (db == null || !db.isOpen())
            {
                LiveRegion.say("No database file open; press Control+O to open one");
                return;
            }
            List<string> lVisited = db.visitedTableNames();
            if (lVisited.Count < 2) { LiveRegion.say("Only one table visited; press F4 to pick another table"); return; }
            string sCurrent = db.currentTable;
            int iCurrent = -1;
            for (int i = 0; i < lVisited.Count; i++)
            {
                if (string.Equals(lVisited[i], sCurrent, StringComparison.OrdinalIgnoreCase))
                { iCurrent = i; break; }
            }
            if (iCurrent < 0) iCurrent = 0;
            int iNext = bForward
                ? (iCurrent + 1) % lVisited.Count
                : (iCurrent - 1 + lVisited.Count) % lVisited.Count;
            string sNext = lVisited[iNext];
            try
            {
                db.selectTable(sNext);
                invokeRefresh();
                // Announce with ring position so the user can hear the
                // wraparound. "table X, position 3 of 3, 142 rows" then
                // the next Control+Tab says "position 1 of 3" -- the
                // user knows wraparound works because the position
                // number cycles.
                int iCount = db.recordCount;
                string sPosOf = "position " + (iNext + 1) + " of " + lVisited.Count;
                string sRows = ", " + iCount + (iCount == 1 ? " row" : " rows");
                LiveRegion.say(sNext + ", " + sPosOf + sRows);
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Switch-Table", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Speak the current table name and row count through the live
        // region. Called whenever a table is freshly selected (via the
        // Select-Table dialog, the dot-prompt 'use' command, or the
        // Control+Tab cycle).
        private void announceTableOpened()
        {
            if (db == null || !db.isOpen() || string.IsNullOrEmpty(db.currentTable)) return;
            int iCount = db.recordCount;
            string sTable = db.currentTable;
            string sMsg = sTable + ", " + iCount + (iCount == 1 ? " row" : " rows");
            LiveRegion.say(sMsg);
        }

        // =====================================================================
        // db.ini override application. Reads [Keys] section pairs and
        // calls KeyMap.overrideKey for each. Reports unknown commands,
        // parse failures, and conflicts in a single startup dialog.
        // =====================================================================
        private void applyIniOverrides()
        {
            string sIniPath = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
                "DbDuo.ini");
            if (!File.Exists(sIniPath)) return;

            string[] aLines = null;
            try { aLines = File.ReadAllLines(sIniPath); } catch { return; }

            bool bInKeysSection = false;
            foreach (string sLine in aLines)
            {
                string sTrim = sLine.Trim();
                if (sTrim.Length == 0) continue;
                if (sTrim.StartsWith(";") || sTrim.StartsWith("#")) continue;
                if (sTrim.StartsWith("["))
                {
                    bInKeysSection = sTrim.Equals("[Keys]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (!bInKeysSection) continue;
                int iEq = sTrim.IndexOf('=');
                if (iEq <= 0) continue;
                string sCommand = sTrim.Substring(0, iEq).Trim();
                string sKeyText = sTrim.Substring(iEq + 1).Trim();
                bool bKnown = false;
                foreach (string s in KeyMap.dCommandToKey.Keys) { if (s == sCommand) { bKnown = true; break; } }
                if (!bKnown)
                {
                    foreach (KeyValuePair<ToolStripMenuItem, string> kv in KeyMap.dMenuToCommand)
                        if (kv.Value == sCommand) { bKnown = true; break; }
                }
                if (!bKnown)
                {
                    KeyMap.lConflicts.Add(string.Format("Unknown command: '{0}'", sCommand));
                    continue;
                }
                KeyMap.overrideKey(sCommand, sKeyText);
            }

            if (KeyMap.lConflicts.Count > 0)
            {
                StringBuilder oSb = new StringBuilder();
                oSb.AppendLine("DbDuo.ini key configuration issues:");
                oSb.AppendLine();
                foreach (string s in KeyMap.lConflicts) oSb.AppendLine("  - " + s);
                HelpDialog.show(this, "DbDuo.ini issues", oSb.ToString());
                KeyMap.lConflicts.Clear();
            }
        }

        // =====================================================================
        // Session persistence: DbDuo.ini stores the last database file
        // opened and the last table within it. On launch with no
        // command-line arguments specifying a different file, the
        // saved database is reopened automatically and the saved
        // table is selected.
        //
        // The [Session] section is a simple key=value store managed by
        // IniSession (below). It coexists with the [Keys] and
        // [General] sections in the same DbDuo.ini file. Writing
        // preserves other sections; we only rewrite the [Session]
        // block.
        //
        // Save points:
        //   - After Open-Database succeeds (sets lastDatabase, clears lastTable)
        //   - After Select-Table succeeds (sets lastTable)
        //   - On Close-Database (clears both)
        //   - On form close (final flush, in case earlier writes were missed)
        // =====================================================================
        public static class IniSession
        {
            // Session state -- the user's last-opened database and
            // table -- is stored in a user-writable location so it
            // survives across launches even when DbDuo.exe lives in
            // a read-only directory (Program Files, network share,
            // user-locked install).
            //
            // Location: %LOCALAPPDATA%\DbDuo\DbDuo.ini. Created on
            // first write. Read order on launch: if the per-user
            // file exists, use it; otherwise consult the EXE-dir
            // DbDuo.ini for legacy installs.
            //
            // Why per-user: Program Files write attempts get UAC-
            // virtualized into VirtualStore (silently, sometimes,
            // depending on the process manifest), or fail outright
            // when running elevated, or succeed-then-vanish across
            // user accounts. Per-user is the only reliable answer.
            //
            // The [Keys] section and [General] uiMode setting still
            // live next to the EXE in the deployed DbDuo.ini -- those
            // are configuration shipped with the install, not user
            // session state. Only [Session] migrated here.
            private static string iniPath()
            {
                string sBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(sBase))
                {
                    // Extremely unusual: %LOCALAPPDATA% undefined.
                    // Fall back to EXE dir so we at least try.
                    return Path.Combine(
                        Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
                        "DbDuo.ini");
                }
                string sDir = Path.Combine(sBase, "DbDuo");
                try { if (!Directory.Exists(sDir)) Directory.CreateDirectory(sDir); }
                catch { }
                return Path.Combine(sDir, "DbDuo.ini");
            }

            // Legacy read fallback: an older DbDuo may have written
            // the [Session] section next to the EXE. If the per-user
            // file is absent or doesn't have a Session value, look
            // there too.
            private static string legacyIniPath()
            {
                return Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
                    "DbDuo.ini");
            }

            public static string read(string sKey)
            {
                return read("Session", sKey);
            }

            // Read a key from any section in the per-user ini.
            // Falls back to the legacy EXE-dir ini if the per-user
            // file has no value. Used by IniSession for [Session]
            // and by IniFolders for [Folders]; both share the
            // same physical file.
            public static string read(string sSection, string sKey)
            {
                string sResult = readFrom(iniPath(), sSection, sKey);
                if (!string.IsNullOrEmpty(sResult)) return sResult;
                return readFrom(legacyIniPath(), sSection, sKey);
            }

            private static string readFrom(string sPath, string sSection, string sKey)
            {
                if (!File.Exists(sPath)) return "";
                string[] aLines;
                try { aLines = File.ReadAllLines(sPath); } catch { return ""; }
                string sHeader = "[" + sSection + "]";
                bool bInSection = false;
                foreach (string sLine in aLines)
                {
                    string sTrim = sLine.Trim();
                    if (sTrim.Length == 0) continue;
                    if (sTrim.StartsWith(";") || sTrim.StartsWith("#")) continue;
                    if (sTrim.StartsWith("["))
                    {
                        bInSection = sTrim.Equals(sHeader, StringComparison.OrdinalIgnoreCase);
                        continue;
                    }
                    if (!bInSection) continue;
                    int iEq = sTrim.IndexOf('=');
                    if (iEq <= 0) continue;
                    string sName = sTrim.Substring(0, iEq).Trim();
                    string sValue = sTrim.Substring(iEq + 1).Trim();
                    if (string.Equals(sName, sKey, StringComparison.OrdinalIgnoreCase))
                        return sValue;
                }
                return "";
            }

            // Write to the per-user file. Creates DbDuo.ini in
            // %LOCALAPPDATA%\DbDuo\ if absent. Preserves any other
            // sections that may already exist. Writes the [Session]
            // section, replacing the key if present, adding it if
            // not, removing it if value is empty.
            public static void write(string sKey, string sValue)
            {
                write("Session", sKey, sValue);
            }

            // Write a key=value into any section of the per-user
            // ini. Preserves any other sections that may already
            // exist. Removes the key if value is empty.
            public static void write(string sSection, string sKey, string sValue)
            {
                string sPath = iniPath();
                string sHeader = "[" + sSection + "]";
                List<string> lLines = new List<string>();
                if (File.Exists(sPath))
                {
                    try { lLines.AddRange(File.ReadAllLines(sPath)); } catch { return; }
                }

                int iSectionStart = -1;
                int iSectionEnd = -1;
                for (int i = 0; i < lLines.Count; i++)
                {
                    string sTrim = lLines[i].Trim();
                    if (sTrim.Equals(sHeader, StringComparison.OrdinalIgnoreCase))
                    {
                        iSectionStart = i;
                        iSectionEnd = lLines.Count;
                        for (int j = i + 1; j < lLines.Count; j++)
                        {
                            string sJ = lLines[j].Trim();
                            if (sJ.StartsWith("[")) { iSectionEnd = j; break; }
                        }
                        break;
                    }
                }

                if (iSectionStart < 0)
                {
                    if (lLines.Count > 0 && lLines[lLines.Count - 1].Trim().Length > 0)
                        lLines.Add("");
                    lLines.Add(sHeader);
                    if (!string.IsNullOrEmpty(sValue))
                        lLines.Add(sKey + " = " + sValue);
                }
                else
                {
                    int iFound = -1;
                    for (int i = iSectionStart + 1; i < iSectionEnd; i++)
                    {
                        string sT = lLines[i].Trim();
                        if (sT.Length == 0) continue;
                        if (sT.StartsWith(";") || sT.StartsWith("#")) continue;
                        int iEq = sT.IndexOf('=');
                        if (iEq <= 0) continue;
                        string sN = sT.Substring(0, iEq).Trim();
                        if (string.Equals(sN, sKey, StringComparison.OrdinalIgnoreCase))
                        { iFound = i; break; }
                    }
                    if (iFound >= 0)
                    {
                        if (string.IsNullOrEmpty(sValue))
                            lLines.RemoveAt(iFound);
                        else
                            lLines[iFound] = sKey + " = " + sValue;
                    }
                    else if (!string.IsNullOrEmpty(sValue))
                    {
                        lLines.Insert(iSectionStart + 1, sKey + " = " + sValue);
                    }
                }

                try
                {
                    File.WriteAllLines(sPath, lLines.ToArray());
                    DbDuoLog.write("Ini write [" + sSection + "] " + sKey + " = " + sValue);
                }
                catch (Exception oEx)
                {
                    DbDuoLog.write("Ini write FAILED: " + sPath + " -- " + oEx.Message);
                }
            }

            public static string lastDatabase { get { return read("lastDatabase"); } set { write("lastDatabase", value); } }
            public static string lastTable    { get { return read("lastTable");    } set { write("lastTable",    value); } }
        }

        // =====================================================================
        // IniFolders: persists the last-used folder for each kind
        // of file dialog (Open, Save-As, Import, Export), so the
        // next invocation of the same dialog opens in the same
        // place the user worked in last. Uses the same per-user
        // DbDuo.ini that IniSession uses, but a [Folders] section.
        //
        // Kinds:
        //   open       -- New-Database, Open-Database, Save-DatabaseAs,
        //                 Backup-Database. All four operate on
        //                 whole-database files; one shared "open"
        //                 folder is more useful than four separate
        //                 ones because users typically keep all
        //                 their databases together.
        //   import     -- Import-Data source files (Markdown).
        //   export     -- Export-Data target files.
        //
        // If no remembered value exists, callers fall back to
        // (in order): the folder of the currently-open database,
        // the user's Documents folder.
        // =====================================================================
        public static class IniFolders
        {
            public static string openFolder
            {
                get { return IniSession.read("Folders", "open"); }
                set { IniSession.write("Folders", "open", value ?? ""); }
            }
            public static string importFolder
            {
                get { return IniSession.read("Folders", "import"); }
                set { IniSession.write("Folders", "import", value ?? ""); }
            }
            public static string exportFolder
            {
                get { return IniSession.read("Folders", "export"); }
                set { IniSession.write("Folders", "export", value ?? ""); }
            }

            // bestDirectory: choose a sensible initial directory for
            // a dialog, given a preferred remembered folder, the
            // folder of the currently-open database (if any), and
            // the user's Documents folder as last resort. Returns
            // the first option that actually exists on disk.
            public static string bestDirectory(string sRemembered, string sFallbackDbPath)
            {
                if (!string.IsNullOrEmpty(sRemembered) && Directory.Exists(sRemembered))
                    return sRemembered;
                if (!string.IsNullOrEmpty(sFallbackDbPath))
                {
                    string sParent = Path.GetDirectoryName(sFallbackDbPath);
                    if (!string.IsNullOrEmpty(sParent) && Directory.Exists(sParent))
                        return sParent;
                }
                try
                {
                    string sDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    if (!string.IsNullOrEmpty(sDocs) && Directory.Exists(sDocs))
                        return sDocs;
                }
                catch { }
                return ""; // let Windows pick
            }
        }

        // =====================================================================
        // FILE menu handlers
        // =====================================================================
        private void fileNewClicked(object oSender, EventArgs oArgs)
        {
            using (SaveFileDialog oFd = new SaveFileDialog())
            {
                oFd.Title = "New Database File";
                oFd.Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*";
                oFd.DefaultExt = "db";
                // Initial folder: last folder the user opened or saved
                // a database from, falling back to the current
                // database's folder, then Documents.
                string sCurDb = (db != null) ? db.filePath : null;
                oFd.InitialDirectory = IniFolders.bestDirectory(IniFolders.openFolder, sCurDb);
                if (oFd.ShowDialog(this) != DialogResult.OK) return;
                IniFolders.openFolder = Path.GetDirectoryName(oFd.FileName);
                try
                {
                    if (File.Exists(oFd.FileName)) File.Delete(oFd.FileName);
                    using (FileStream fs = File.Create(oFd.FileName)) { }
                    db.openDatabase(oFd.FileName, null, false);
                    invokeRefresh();
                }
                catch (Exception oEx)
                {
                    MessageBox.Show(this, oEx.Message, "New Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void fileOpenClicked(object oSender, EventArgs oArgs)
        {
            using (OpenFileDialog oFd = new OpenFileDialog())
            {
                oFd.Title = "Open Database File";
                oFd.Filter = "All supported|*.db;*.sqlite;*.sqlite3;*.mdb;*.accdb;*.xlsx;*.xls;*.dbf;*.csv;*.tsv;*.txt"
                           + "|SQLite (*.db;*.sqlite;*.sqlite3)|*.db;*.sqlite;*.sqlite3"
                           + "|Access (*.mdb;*.accdb)|*.mdb;*.accdb"
                           + "|Excel (*.xlsx;*.xls)|*.xlsx;*.xls"
                           + "|dBASE (*.dbf)|*.dbf"
                           + "|CSV / Text (*.csv;*.tsv;*.txt)|*.csv;*.tsv;*.txt"
                           + "|All Files (*.*)|*.*";
                // Initial folder: last folder the user opened a
                // database from, falling back to the currently-open
                // database's folder, then Documents. If the user
                // already opened a file last session, we also fill
                // in its name as the suggested filename for one-
                // press re-open (rare workflow, but the user is
                // free to clear it).
                string sCurDb = (db != null) ? db.filePath : null;
                oFd.InitialDirectory = IniFolders.bestDirectory(IniFolders.openFolder, sCurDb);
                if (oFd.ShowDialog(this) != DialogResult.OK) return;
                IniFolders.openFolder = Path.GetDirectoryName(oFd.FileName);
                try
                {
                    DbDuoLog.write("File > Open: " + oFd.FileName);
                    // Stale drill entries from a previously-open database
                    // make no sense in the newly-opened one. The manager
                    // clears its own per-table caches in openDatabase;
                    // the drill stack lives here on the form, so we
                    // clear it ourselves.
                    oDrillStack.Clear();
                    db.openDatabase(oFd.FileName, null, false);
                    // If we opened a database with multiple tables and no specific
                    // table was selected during connect, prompt for one.
                    if (!db.hasRecordset())
                    {
                        // Prefer base tables; fall back to views if there
                        // are no base tables (some pure-views schemas).
                        List<string> lTables = db.getTableNames();
                        if (lTables.Count == 0) lTables = db.getViewNames();
                        if (lTables.Count > 0) db.selectTable(lTables[0]);
                    }
                    invokeRefresh();
                    DbDuoLog.write("Open succeeded. Table: " + (db.currentTable ?? "(none)"));
                    // Persist for next launch.
                    IniSession.lastDatabase = oFd.FileName;
                    IniSession.lastTable    = db.currentTable ?? "";
                    announceTableOpened();
                }
                catch (Exception oEx)
                {
                    DbDuoLog.write("Open failed: " + oEx.Message);
                    MessageBox.Show(this, oEx.Message, "Open Database File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void fileSaveAsClicked(object oSender, EventArgs oArgs) { saveAsCommon("Save Database As"); }
        private void fileBackupClicked(object oSender, EventArgs oArgs) { saveAsCommon("Backup Database"); }

        private void saveAsCommon(string sTitle)
        {
            if (db == null || !db.isOpen()) return;
            using (SaveFileDialog oFd = new SaveFileDialog())
            {
                oFd.Title = sTitle;
                string sExt = Path.GetExtension(db.filePath).TrimStart('.');
                oFd.Filter = "Same format (*." + sExt + ")|*." + sExt + "|All Files (*.*)|*.*";
                oFd.DefaultExt = sExt;
                // Default folder = the currently-open database's folder
                // (the Save-As is typically a copy alongside the
                // original). Default filename = the source file's
                // name, leaving the user one keystroke from a
                // sensible "FooBackup.db" or similar by editing it.
                oFd.InitialDirectory = IniFolders.bestDirectory("", db.filePath);
                string sLeaf = Path.GetFileNameWithoutExtension(db.filePath);
                if (!string.IsNullOrEmpty(sLeaf))
                {
                    // Suggest "<original>-copy" so we don't overwrite
                    // the original by mistake when the user just
                    // presses Enter.
                    string sSuggest = sLeaf + "-copy";
                    if (sTitle.IndexOf("Backup", StringComparison.OrdinalIgnoreCase) >= 0)
                        sSuggest = sLeaf + "-backup-" + DateTime.Now.ToString("yyyyMMdd");
                    oFd.FileName = sSuggest;
                }
                if (oFd.ShowDialog(this) != DialogResult.OK) return;
                IniFolders.openFolder = Path.GetDirectoryName(oFd.FileName);
                try
                {
                    db.saveAs(oFd.FileName);
                    invokeRefresh();
                    MessageBox.Show(this, "Saved to: " + oFd.FileName, sTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception oEx)
                {
                    MessageBox.Show(this, oEx.Message, sTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void fileCloseClicked(object oSender, EventArgs oArgs)
        {
            if (db == null) return;
            try { db.close(); } catch { }
            // Reset the drill stack so navigation state doesn't leak
            // across databases. The TableSettings cache inside
            // DbDuoManager is cleared by db.close() itself.
            oDrillStack.Clear();
            invokeRefresh();
            // Clear session persistence so the next launch starts
            // empty rather than reopening the just-closed file.
            IniSession.lastDatabase = "";
            IniSession.lastTable    = "";
        }

        private void fileCompareClicked(object oSender, EventArgs oArgs)
        {
            MessageBox.Show(this, "Compare-Database is not yet implemented.",
                "Compare-Database", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void fileImportClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset())
            {
                MessageBox.Show(this, "Open a database and pick a target table first.",
                    "Import-Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (OpenFileDialog oFd = new OpenFileDialog())
            {
                oFd.Title = "Import-Data into " + (db.currentTable ?? "current table");
                oFd.Filter = "Markdown table (*.md;*.markdown)|*.md;*.markdown"
                           + "|All Files (*.*)|*.*";
                oFd.DefaultExt = "md";
                // Default folder: last import folder, else the
                // current database's folder, else Documents.
                oFd.InitialDirectory = IniFolders.bestDirectory(IniFolders.importFolder, db.filePath);
                if (oFd.ShowDialog(this) != DialogResult.OK) return;
                IniFolders.importFolder = Path.GetDirectoryName(oFd.FileName);
                try
                {
                    int iCount = db.importMarkdown(oFd.FileName);
                    DbDuoLog.write("Imported " + iCount + " row(s) from " + oFd.FileName);
                    invokeRefresh();
                    MessageBox.Show(this,
                        "Imported " + iCount + " row(s) into " + db.currentTable + ".",
                        "Import-Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception oEx)
                {
                    MessageBox.Show(this, oEx.Message, "Import-Data",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void fileExportClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            using (SaveFileDialog oFd = new SaveFileDialog())
            {
                oFd.Title = "Export-Data";
                oFd.Filter = "Excel workbook (*.xlsx)|*.xlsx"
                           + "|Word document (*.docx)|*.docx"
                           + "|HTML, filtered (*.html)|*.html"
                           + "|Markdown table (*.md)|*.md"
                           + "|CSV (*.csv)|*.csv"
                           + "|TSV (*.tsv)|*.tsv"
                           + "|SQLite database (*.db)|*.db"
                           + "|Access database (*.accdb)|*.accdb"
                           + "|dBASE table (*.dbf)|*.dbf"
                           + "|All Files (*.*)|*.*";
                oFd.DefaultExt = "xlsx";
                // Default folder: last export folder, else current
                // database's folder, else Documents.
                oFd.InitialDirectory = IniFolders.bestDirectory(IniFolders.exportFolder, db.filePath);
                // Suggest a filename based on the current table.
                if (!string.IsNullOrEmpty(db.currentTable))
                    oFd.FileName = db.currentTable;
                if (oFd.ShowDialog(this) != DialogResult.OK) return;
                IniFolders.exportFolder = Path.GetDirectoryName(oFd.FileName);
                try
                {
                    db.exportData(oFd.FileName);
                    DbDuoLog.write("Exported to " + oFd.FileName);
                    // Open the result in its default application so
                    // the user sees their export immediately, matching
                    // dbDot's behavior.
                    ComAutomation.shellOpen(oFd.FileName);
                }
                catch (Exception oEx)
                {
                    MessageBox.Show(this, oEx.Message, "Export-Data",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void filePrintClicked(object oSender, EventArgs oArgs)
        {
            MessageBox.Show(this, "Out-Printer is not yet implemented. Use Export-Data to HTML and print from a browser.",
                "Out-Printer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void fileExitClicked(object oSender, EventArgs oArgs) { this.Close(); }

        // =====================================================================
        // RECORD menu handlers
        // =====================================================================
        private void recNewClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset() || db.readOnly) return;
            if (db.currentIsView) {
                MessageBox.Show(this, "Cannot insert into a view (read-only).",
                    "New-Record", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // For New-Record, show only distinct (substantive) fields.
            // The bookkeeping fields (added, updated, observed, marked, _id)
            // get filled in by DEFAULT clauses on the columns: added /
            // updated / observed default to current_timestamp; marked
            // defaults to 0; the primary key is auto-incremented.
            List<string> lFields = db.getDistinctFieldNames();
            Dictionary<string, string> dInitial = new Dictionary<string, string>();
            List<bool> lEditable = new List<bool>();
            foreach (string s in lFields)
            {
                dInitial[s] = "";
                lEditable.Add(true);
            }
            using (RecordEditDialog oDlg = new RecordEditDialog("New-Record", lFields, dInitial, lEditable))
            {
                if (oDlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    db.addNew();
                    foreach (KeyValuePair<string, string> kv in oDlg.dValues)
                        if (!string.IsNullOrEmpty(kv.Value))
                            db.setFieldValue(kv.Key, kv.Value);
                    db.update();
                    invokeRefresh();
                }
                catch (Exception oEx)
                {
                    try { db.cancelUpdate(); } catch { }
                    MessageBox.Show(this, oEx.Message, "New-Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void recSetClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset() || db.readOnly) return;
            if (db.currentIsView) {
                MessageBox.Show(this, "Cannot edit a view (read-only).",
                    "Set-Record", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (db.eof || db.bof) return;
            // Show distinct fields editable, plus metadata fields read-only.
            // The user sees the housekeeping values for context but cannot
            // edit them; SQLite triggers update 'updated' automatically.
            List<string> lDistinct = db.getDistinctFieldNames();
            List<string> lMetadata = db.getMetadataFieldNames();
            List<string> lFields = new List<string>();
            lFields.AddRange(lDistinct);
            lFields.AddRange(lMetadata);
            Dictionary<string, string> dInitial = new Dictionary<string, string>();
            List<bool> lEditable = new List<bool>();
            foreach (string s in lDistinct)
            {
                dInitial[s] = db.getFieldValue(s);
                lEditable.Add(true);
            }
            foreach (string s in lMetadata)
            {
                dInitial[s] = db.getFieldValue(s);
                lEditable.Add(false);  // bookkeeping shown read-only
            }
            using (RecordEditDialog oDlg = new RecordEditDialog("Set-Record", lFields, dInitial, lEditable))
            {
                if (oDlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    foreach (string sCol in lDistinct)
                    {
                        if (oDlg.dValues.ContainsKey(sCol))
                            db.setFieldValue(sCol, oDlg.dValues[sCol]);
                    }
                    db.update();
                    invokeRefresh();
                }
                catch (Exception oEx)
                {
                    try { db.cancelUpdate(); } catch { }
                    MessageBox.Show(this, oEx.Message, "Set-Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void recRemoveClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset() || db.readOnly) return;
            if (db.eof || db.bof) return;
            DialogResult oRes = MessageBox.Show(this,
                "Remove the current record?", "Remove-Record",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (oRes != DialogResult.Yes) return;
            try
            {
                db.deleteCurrent();
                invokeRefresh();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Remove-Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Show-Object: open a read-only, multi-line view of the
        // currently-selected row. Three sections, separated by a
        // blank line:
        //
        //   1. The fields visible in the listview, one per line,
        //      in the form "field-name = value".
        //   2. For each PARENT row reached via outbound foreign-key
        //      column on this row: "Related <parent-table>:" header
        //      followed by the parent's look value indented under it.
        //      Skipped if this table has no FK columns.
        //   3. For each CHILD TABLE that references this row: a
        //      "Related <child-table>:" header followed by the look
        //      values of every matching child row, one per line.
        //      Capped at 25 per table with an "(N more)" footer so
        //      a heavily-related parent doesn't produce a wall of
        //      text. The user can press Shift+E to drill in for
        //      the full list.
        //
        // The look column is DbDuo's standard "summary" calculated
        // column -- a SQLite stored-generated text that concatenates
        // a handful of substantive fields with " | " separators,
        // designed to be a screen-reader-friendly one-line
        // representation of a record. Tables without a look column
        // fall back to "(no look column)" placeholders.
        //
        // The dialog is dismissed by OK (Enter) or Escape.
        //
        // Bound to Enter from the data grid for one-key access.
        // Get-Property (Alt+Enter) remains the way to see EVERY
        // field including hidden bookkeeping columns.
        private void recShowClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            if (db.eof || db.bof) return;

            // SECTION 1: the listview's visible columns.
            // Pull headers from the listview directly so the dialog
            // matches what the user is looking at. Fallback to the
            // manager's display-fields list if the listview isn't
            // built yet (CLI session). The separator is " = " --
            // dbDot/dBASE convention.
            List<string> lFields = new List<string>();
            if (grid != null && grid.Columns.Count > 0)
            {
                foreach (ColumnHeader oCol in grid.Columns)
                    lFields.Add(oCol.Text);
            }
            else
            {
                lFields = db.getDisplayFieldNames();
                if (lFields.Count == 0) lFields = db.getFieldNames();
            }

            StringBuilder oSb = new StringBuilder();
            foreach (string sCol in lFields)
            {
                string sValue = formatFieldValueForDisplay(db.getFieldValue(sCol), sCol);
                oSb.Append(sCol);
                oSb.Append(" = ");
                oSb.AppendLine(sValue);
            }

            // SECTIONS 2 and 3: related records. Skip silently if
            // the current row has no usable primary key, since
            // neither direction makes sense without one.
            appendRelatedRecords(oSb);

            HelpDialog.show(this, "Show-Object (row " + db.absolutePosition + ")", oSb.ToString());
        }

        // appendRelatedRecords: extend a Show-Object output with
        // the related-records sections. Two passes:
        //   (a) Outbound FKs on the current row -- columns named
        //       "<parent>_id" or "<plural-parent>_id". For each,
        //       fetch the single parent row's look value and emit
        //       it under a "Related <parent-table>:" header.
        //   (b) Other base tables that reference the current row
        //       via a column named like this table's PK. For each,
        //       fetch up to 25 look values matching the FK and emit
        //       them under a "Related <child-table>:" header.
        //
        // The query path is DbDuoManager.queryColumnValues, which
        // issues a direct SELECT on the live connection so SQLite
        // can use its own indexes (if any are defined on the FK
        // columns) -- much cheaper than loading the whole child
        // recordset to client-side and filtering.
        private void appendRelatedRecords(StringBuilder oSb)
        {
            const int iMaxPerSection = 25;
            const string sNoLook = "(no look column on this table)";

            string sCurrentTable = db.currentTable ?? "";
            if (string.IsNullOrEmpty(sCurrentTable)) return;
            List<string> lCurrentCols = db.getFieldNames();
            if (lCurrentCols.Count == 0) return;

            // Try schema metadata first for the current table's PK,
            // then fall back to the naming convention. This makes
            // Show-Object's child-section work even for tables
            // whose names have irregular English plurals
            // (e.g. classes -> class_id, cities -> city_id).
            string sCurrentPk = db.actualPrimaryKey(sCurrentTable);
            if (string.IsNullOrEmpty(sCurrentPk))
                sCurrentPk = computePrimaryKeyColumn(sCurrentTable, lCurrentCols);
            string sCurrentPkValue = "";
            if (!string.IsNullOrEmpty(sCurrentPk))
            {
                try { sCurrentPkValue = db.getFieldValue(sCurrentPk) ?? ""; }
                catch { sCurrentPkValue = ""; }
            }

            List<string> lAllTables = db.getTableNames();

            // -------------------------------------------------------
            // SECTION 2: outbound FKs -> parents. For every column on
            // the current row whose name ends in "_id" and matches
            // some other table's PK, fetch that parent's look value.
            // -------------------------------------------------------
            bool bAnyParent = false;
            foreach (string sCol in lCurrentCols)
            {
                if (string.Equals(sCol, sCurrentPk, StringComparison.OrdinalIgnoreCase))
                    continue;  // skip our own PK
                if (!sCol.EndsWith(Metadata.PrimaryKeySuffix, StringComparison.OrdinalIgnoreCase))
                    continue;  // not an FK by naming convention

                // The FK value on the current row.
                string sFkValue = "";
                try { sFkValue = db.getFieldValue(sCol) ?? ""; }
                catch { sFkValue = ""; }
                if (string.IsNullOrEmpty(sFkValue)) continue;

                // Find the parent table. The convention: an FK named
                // "<parent_singular>_id" points to table "<parent_plural>"
                // (e.g. teacher_id -> teachers). We also accept
                // "<parent_table>_id" without pluralization.
                string sParentTable = findParentTableForFk(sCol, lAllTables);
                if (string.IsNullOrEmpty(sParentTable)) continue;

                // Fetch the parent's look value via direct SQL.
                int iFound;
                List<string> lLook = db.queryColumnValues(
                    sParentTable, "look", sCol, sFkValue, 1, out iFound);

                if (!bAnyParent)
                {
                    if (oSb.Length > 0) oSb.AppendLine();  // blank separator
                    bAnyParent = true;
                }
                oSb.Append("Related ");
                oSb.Append(sParentTable);
                oSb.AppendLine(":");
                if (iFound == 0)
                    oSb.AppendLine("  (parent not found)");
                else if (lLook.Count > 0 && !string.IsNullOrEmpty(lLook[0]))
                    oSb.AppendLine("  " + lLook[0]);
                else
                    oSb.AppendLine("  " + sNoLook);
            }

            // -------------------------------------------------------
            // SECTION 3: child tables -> records pointing to me. For
            // every other base table that has a column named the
            // same as MY primary key, list its look values where
            // <fk> = <my-pk-value>.
            // -------------------------------------------------------
            if (string.IsNullOrEmpty(sCurrentPk) || string.IsNullOrEmpty(sCurrentPkValue))
                return;  // no PK -> no children to find

            foreach (string sChildTable in lAllTables)
            {
                if (string.Equals(sChildTable, sCurrentTable, StringComparison.OrdinalIgnoreCase))
                    continue;
                List<string> lChildCols = db.getColumnsOfTable(sChildTable);
                bool bHasFk = false;
                foreach (string sChildCol in lChildCols)
                {
                    if (string.Equals(sChildCol, sCurrentPk, StringComparison.OrdinalIgnoreCase))
                    { bHasFk = true; break; }
                }
                if (!bHasFk) continue;

                // Issue the query.
                int iFound;
                List<string> lLook = db.queryColumnValues(
                    sChildTable, "look", sCurrentPk, sCurrentPkValue,
                    iMaxPerSection, out iFound);
                if (iFound == 0) continue;  // no related rows; skip

                oSb.AppendLine();
                oSb.Append("Related ");
                oSb.Append(sChildTable);
                oSb.AppendLine(":");
                if (lLook.Count == 0)
                {
                    // Found rows but couldn't fetch look values --
                    // child table is missing the look column.
                    // Emit a placeholder count so the user knows.
                    oSb.AppendLine("  (" + iFound + " row(s) -- " + sNoLook + ")");
                }
                else
                {
                    foreach (string sLook in lLook)
                    {
                        if (string.IsNullOrEmpty(sLook))
                            oSb.AppendLine("  (empty look)");
                        else
                            oSb.AppendLine("  " + sLook);
                    }
                    if (iFound > lLook.Count)
                    {
                        int iMore = iFound - lLook.Count;
                        oSb.AppendLine("  (... " + iMore + " more; use Enter-Child to see all)");
                    }
                }
            }
        }

        // findParentTableForFk: given an FK column like "teacher_id"
        // or "class_id", find which table it points to. Inverts
        // the lookup by asking each candidate table whether its
        // actual primary-key column is the FK we're looking up.
        //
        // Uses actualPrimaryKey() to read the schema's own metadata
        // (PRAGMA table_info for SQLite, ADOX.Keys for Access),
        // so it works for any English plural irregularity --
        // "classes" -> "class_id", "cities" -> "city_id" -- as
        // long as the schema actually defines a PK. Falls back to
        // the naming-convention rule computePrimaryKeyColumn for
        // tables with no declared PK.
        private string findParentTableForFk(string sFkColumn, List<string> lAllTables)
        {
            if (string.IsNullOrEmpty(sFkColumn)) return null;
            foreach (string sT in lAllTables)
            {
                string sPk = db.actualPrimaryKey(sT);
                if (string.IsNullOrEmpty(sPk))
                {
                    List<string> lCols = db.getColumnsOfTable(sT);
                    sPk = computePrimaryKeyColumn(sT, lCols);
                }
                if (!string.IsNullOrEmpty(sPk)
                    && string.Equals(sPk, sFkColumn, StringComparison.OrdinalIgnoreCase))
                    return sT;
            }
            return null;
        }

        // Format a field value for display in Show-Record. The recordset
        // returns BLOBs as System.Byte[] which stringifies unhelpfully;
        // we render them as a size summary instead. Long text values
        // are kept as-is (newlines preserved by the multi-line dialog).
        private string formatFieldValueForDisplay(string sRaw, string sCol)
        {
            if (string.IsNullOrEmpty(sRaw)) return "";
            if (sRaw == "System.Byte[]")
            {
                // Try to get the actual blob length from the field.
                try
                {
                    int iLen = db.getFieldByteLength(sCol);
                    return "[BLOB, " + iLen + " bytes]";
                }
                catch { return "[BLOB]"; }
            }
            return sRaw;
        }

        private void recCopyClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            if (db.eof || db.bof) return;
            List<string> lV = new List<string>();
            foreach (string sCol in db.getFieldNames()) lV.Add(db.getFieldValue(sCol));
            try { Clipboard.SetText(string.Join("\t", lV)); }
            catch { /* clipboard unavailable */ }
        }

        // Open-Cell: take the value in the currently focused grid cell
        // and try to open it. Behavior:
        //   - URL (starts with http://, https://, file://, mailto:): open
        //     in the system default browser via Process.Start.
        //   - Existing file path: open in its default associated app
        //     (Process.Start lets the shell pick the handler).
        //   - Existing folder path: open in Windows Explorer.
        //   - Anything else: show a brief message in the live region
        //     and do nothing.
        // Bound to Control+Enter.
        private void recOpenCellClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            string sValue = "";
            try
            {
                // The ListView shows only the display fields; look
                // up the cell value via the column's header text
                // (which is the field name).
                if (grid != null && grid.Columns.Count > 0)
                {
                    int iCol = iCurrentColumnIndex;
                    if (iCol < 0 || iCol >= grid.Columns.Count) iCol = 0;
                    string sName = grid.Columns[iCol].Text;
                    if (!string.IsNullOrEmpty(sName))
                        sValue = db.getFieldValue(sName) ?? "";
                }
            }
            catch { }
            sValue = (sValue ?? "").Trim();
            if (sValue.Length == 0)
            {
                LiveRegion.say("Cell is empty");
                return;
            }
            try
            {
                bool bIsUrl = sValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                           || sValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                           || sValue.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                           || sValue.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
                if (bIsUrl || File.Exists(sValue) || Directory.Exists(sValue))
                {
                    System.Diagnostics.Process.Start(sValue);
                    LiveRegion.say("Opened: " + sValue);
                }
                else
                {
                    LiveRegion.say("Not a URL, file, or folder: " + sValue);
                }
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, "Could not open: " + sValue + "\n\n" + oEx.Message,
                    "Open-Cell", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void recFindClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            string sCriteria = promptFind();
            if (string.IsNullOrEmpty(sCriteria)) return;
            sLastFindCriteria = sCriteria;
            try
            {
                bool bFound = db.findRecord(sCriteria, true, false);
                if (!bFound) MessageBox.Show(this, "Not found.", "Jump-Record",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                invokeRefresh();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Jump-Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void recFindNextClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            if (string.IsNullOrEmpty(sLastFindCriteria)) return;
            try
            {
                bool bFound = db.findRecord(sLastFindCriteria, true, true);
                if (!bFound) MessageBox.Show(this, "No more matches.", "Jump-RecordAgain",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                invokeRefresh();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Jump-RecordAgain", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Find-RecordPrevious: search backwards from the current row using
        // the last entered Find criteria. Bound to Shift+F3 by EdSharp
        // convention for "Reverse Find Again."
        private void recFindPrevClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            if (string.IsNullOrEmpty(sLastFindCriteria)) return;
            try
            {
                bool bFound = db.findRecord(sLastFindCriteria, false, true);
                if (!bFound) MessageBox.Show(this, "No earlier matches.", "Jump-RecordPrevious",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                invokeRefresh();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Jump-RecordPrevious", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Set-Position: jump to a numbered row (1-based). Prompts for the
        // target row number. Bound to Control+G by EdSharp convention for
        // "Go to Percent" (the closest editor analog to "go to row N").
        private void recGoToClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            int iCount = db.recordCount;
            if (iCount <= 0) { MessageBox.Show(this, "No rows.", "Set-Position", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            string sInput = promptText("Set-Position",
                "Row number (1 to " + iCount + ", or use a percent like 50% for half-way):",
                (db.absolutePosition).ToString());
            if (string.IsNullOrEmpty(sInput)) return;
            sInput = sInput.Trim();
            int iTarget;
            if (sInput.EndsWith("%"))
            {
                double n;
                if (!double.TryParse(sInput.TrimEnd('%').Trim(), out n) || n < 0 || n > 100)
                { MessageBox.Show(this, "Enter a percent between 0 and 100.", "Set-Position", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                iTarget = (int)Math.Round(iCount * n / 100.0);
                if (iTarget < 1) iTarget = 1;
            }
            else if (!int.TryParse(sInput, out iTarget))
            { MessageBox.Show(this, "Enter a row number or percent.", "Set-Position", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (iTarget < 1) iTarget = 1;
            if (iTarget > iCount) iTarget = iCount;
            try
            {
                db.absolutePosition = iTarget;
                invokeRefresh();
            }
            catch (Exception oEx) { MessageBox.Show(this, oEx.Message, "Set-Position", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // Save-Bookmark: remember the current row's bookmark so the user
        // can wander and return. Bound to Control+K by EdSharp convention
        // for "Set Bookmark." Only one bookmark slot is kept; saving a
        // new one replaces any previous.
        private object oSavedBookmark;
        private void recBookmarkClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            try
            {
                oSavedBookmark = db.bookmark;
                string sMsg = "Bookmark saved at row " + db.absolutePosition;
                lblStatus.Text = sMsg;
                LiveRegion.say(sMsg);
            }
            catch (Exception oEx) { MessageBox.Show(this, oEx.Message, "Save-Bookmark", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // Restore-Bookmark: jump back to the saved row. Bound to Alt+K by
        // EdSharp convention for "Go to Bookmark."
        private void recGotoBookmarkClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            if (oSavedBookmark == null)
            { MessageBox.Show(this, "No bookmark saved. Use Save-Bookmark (Control+K) first.", "Restore-Bookmark", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            try
            {
                db.bookmark = oSavedBookmark;
                invokeRefresh();
                LiveRegion.say("Returned to bookmarked row " + db.absolutePosition);
            }
            catch (Exception oEx) { MessageBox.Show(this, oEx.Message, "Restore-Bookmark", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // Clear-Bookmark: forget the saved row. Bound to Control+Shift+K
        // by EdSharp convention for "Clear Bookmark."
        private void recClearBookmarkClicked(object oSender, EventArgs oArgs)
        {
            oSavedBookmark = null;
            lblStatus.Text = "Bookmark cleared.";
            LiveRegion.say("Bookmark cleared");
        }

        // Helper: prompt for a single line of text. Returns null if the
        // user cancels. Used by Set-Position and any other quick-input
        // command.
        private string promptText(string sTitle, string sPrompt, string sInitial)
        {
            using (Form oDlg = new Form())
            {
                oDlg.Text = sTitle;
                oDlg.AccessibleName = sTitle;
                oDlg.StartPosition = FormStartPosition.CenterParent;
                oDlg.ClientSize = new Size(420, 140);
                oDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                oDlg.MaximizeBox = false;
                oDlg.MinimizeBox = false;
                oDlg.ShowInTaskbar = false;
                oDlg.KeyPreview = true;

                Label lbl = new Label();
                lbl.Text = sPrompt;
                lbl.Location = new Point(12, 12);
                lbl.Size = new Size(396, 36);
                oDlg.Controls.Add(lbl);

                TextBox tb = new TextBox();
                tb.AccessibleName = sPrompt;
                tb.Location = new Point(12, 56);
                tb.Size = new Size(396, 23);
                tb.Text = sInitial ?? "";
                tb.SelectAll();
                oDlg.Controls.Add(tb);

                Button btnOk = new Button();
                btnOk.Text = "&OK";
                btnOk.DialogResult = DialogResult.OK;
                btnOk.Size = new Size(90, 28);
                btnOk.Location = new Point(218, 96);
                oDlg.Controls.Add(btnOk);

                Button btnCancel = new Button();
                btnCancel.Text = "&Cancel";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.Size = new Size(90, 28);
                btnCancel.Location = new Point(316, 96);
                oDlg.Controls.Add(btnCancel);

                oDlg.AcceptButton = btnOk;
                oDlg.CancelButton = btnCancel;
                oDlg.ActiveControl = tb;

                if (oDlg.ShowDialog(this) != DialogResult.OK) return null;
                return tb.Text;
            }
        }

        private string promptFind()
        {
            using (Form oDlg = new Form())
            {
                oDlg.Text = "Jump-Record";
                oDlg.AccessibleName = "Find Record";
                oDlg.AccessibleDescription = "Enter a search expression in ADO Find syntax: column LIKE '%text%' or column = 'value' or column > 100.";
                oDlg.StartPosition = FormStartPosition.CenterParent;
                oDlg.ClientSize = new Size(540, 140);
                oDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                oDlg.MaximizeBox = false;
                oDlg.MinimizeBox = false;
                oDlg.ShowInTaskbar = false;
                oDlg.KeyPreview = true;

                Label lbl = new Label();
                lbl.Text = "&Criteria (ADO Find syntax, e.g. lastName LIKE '%Smith%'):";
                lbl.Location = new Point(12, 12);
                lbl.Size = new Size(516, 20);
                oDlg.Controls.Add(lbl);

                TextBox tb = new TextBox();
                tb.AccessibleName = "Criteria";
                tb.Location = new Point(12, 38);
                tb.Size = new Size(516, 23);
                tb.Text = sLastFindCriteria ?? "";
                oDlg.Controls.Add(tb);

                Button btnOk = new Button();
                btnOk.Text = "&OK";
                btnOk.DialogResult = DialogResult.OK;
                btnOk.Size = new Size(90, 28);
                btnOk.Location = new Point(338, 90);
                oDlg.Controls.Add(btnOk);

                Button btnCancel = new Button();
                btnCancel.Text = "&Cancel";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.Size = new Size(90, 28);
                btnCancel.Location = new Point(434, 90);
                oDlg.Controls.Add(btnCancel);

                oDlg.AcceptButton = btnOk;
                oDlg.CancelButton = btnCancel;
                oDlg.ActiveControl = tb;
                tb.SelectAll();

                if (oDlg.ShowDialog(this) != DialogResult.OK) return null;
                return tb.Text;
            }
        }

        // =====================================================================
        // VIEW menu handlers
        // =====================================================================
        private void viewSelectClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            using (FilterDialog oDlg = new FilterDialog(db.getFieldNames(), "", "All columns", "Contains"))
            {
                if (oDlg.ShowDialog(this) != DialogResult.OK) return;
                string sExpr = buildFilterExpression(oDlg.sFilterText, oDlg.sFilterColumn, oDlg.sMatchMode, db.getFieldNames());
                try
                {
                    db.filter = sExpr;
                    invokeRefresh();
                }
                catch (Exception oEx)
                {
                    MessageBox.Show(this, oEx.Message, "Select-Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Translate the dialog's three controls into an ADO Filter expression.
        // ADO Filter syntax: "col LIKE '%text%'", "col = 'text'", optionally
        // chained with AND. For "All columns", we OR the predicates.
        private static string buildFilterExpression(string sText, string sCol, string sMode, List<string> lAllColumns)
        {
            if (string.IsNullOrEmpty(sText)) return "";
            string sEsc = sText.Replace("'", "''");
            string sPattern;
            switch (sMode ?? "Contains")
            {
                case "Equals":     sPattern = "= '" + sEsc + "'"; break;
                case "Starts with":sPattern = "LIKE '" + sEsc + "%'"; break;
                default:           sPattern = "LIKE '%" + sEsc + "%'"; break;
            }
            if (string.IsNullOrEmpty(sCol) || sCol == "All columns")
            {
                List<string> lOr = new List<string>();
                foreach (string sN in lAllColumns) lOr.Add(sN + " " + sPattern);
                return string.Join(" OR ", lOr);
            }
            return sCol + " " + sPattern;
        }

        private void viewResetFilterClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            db.resetFilter();
            invokeRefresh();
        }

        private void viewFormatClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            List<string> lFields = db.getFieldNames();
            if (lFields.Count == 0) return;
            using (SortDialog oDlg = new SortDialog(lFields, lFields[0], true))
            {
                if (oDlg.ShowDialog(this) != DialogResult.OK) return;
                string sExpr = oDlg.sSortColumn + (oDlg.bAscending ? " ASC" : " DESC");
                try
                {
                    db.sort = sExpr;
                    invokeRefresh();
                }
                catch (Exception oEx)
                {
                    MessageBox.Show(this, oEx.Message, "Sort-Object", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void viewSortAscClicked(object oSender, EventArgs oArgs) { sortByCurrentColumn(true); }
        private void viewSortDescClicked(object oSender, EventArgs oArgs) { sortByCurrentColumn(false); }

        // Sort by the table's "updated" or "added" timestamp column,
        // following DbDuo's standard-bookkeeping-columns convention.
        // Most-recent-first is the typical use case ("show me what
        // changed lately"), so Alt+Shift+D = recent-first; Alt+D =
        // oldest-first as the inverse.
        //
        // Resolution: prefer 'updated' when present, else 'added',
        // else 'observed'. If none of the three exist on the current
        // table, the sort is refused with a brief notice (the user can
        // fall back to Alt+A on a column they navigate to).
        private void viewSortRecentClicked(object oSender, EventArgs oArgs) { sortByDateColumn(false); }
        private void viewSortOldestClicked(object oSender, EventArgs oArgs) { sortByDateColumn(true); }

        private void sortByDateColumn(bool bOldestFirst)
        {
            if (db == null || !db.hasRecordset()) return;
            string sCol = pickDateColumn();
            if (sCol == null)
            {
                LiveRegion.say("No updated, added, or observed column on this table");
                MessageBox.Show(this,
                    "This table does not have an 'updated', 'added', or 'observed' column. "
                    + "Date-sort needs one of these. Use Alt+A to sort by another column instead.",
                    "Sort by date", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                db.sort = sCol + (bOldestFirst ? " ASC" : " DESC");
                invokeRefresh();
                LiveRegion.say("Sorted by " + sCol + ", " + (bOldestFirst ? "oldest first" : "most recent first"));
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Sort by date", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Find the best timestamp column for date-sort. DbDuo's
        // standard bookkeeping columns are 'added', 'updated',
        // 'observed' (see Metadata.BookkeepingColumns); 'updated' is
        // the freshest signal of "when did this row last change," so
        // we prefer it. Returns null if none of the three exist.
        private string pickDateColumn()
        {
            if (db == null || !db.hasRecordset()) return null;
            foreach (string sName in Metadata.DateSortColumns)
                if (db.hasField(sName)) return sName;
            return null;
        }

        private void sortByCurrentColumn(bool bAsc)
        {
            if (db == null || !db.hasRecordset()) return;
            if (grid == null || grid.Columns.Count == 0) return;
            int iCol = iCurrentColumnIndex;
            if (iCol < 0 || iCol >= grid.Columns.Count) iCol = 0;
            string sCol = grid.Columns[iCol].Name;
            if (string.IsNullOrEmpty(sCol)) sCol = grid.Columns[iCol].Text;
            try
            {
                db.sort = sCol + (bAsc ? " ASC" : " DESC");
                invokeRefresh();
                LiveRegion.say("Sorted by " + sCol + (bAsc ? ", ascending" : ", descending"));
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Sort-Object", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void viewResetSortClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            db.resetSort();
            invokeRefresh();
        }

        private void viewUpdateClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            try { db.requery(); invokeRefresh(); }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Update-View", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =====================================================================
        // SCHEMA menu handlers
        // =====================================================================
        private void schemaSelectTableClicked(object oSender, EventArgs oArgs)
        {
            selectFromList(db == null ? null : db.getTableNames(), "Select-Table", "table");
        }

        private void schemaSelectViewClicked(object oSender, EventArgs oArgs)
        {
            selectFromList(db == null ? null : db.getViewNames(), "Select-View", "view");
        }

        // Shared body of Select-Table and Select-View. Each prompts
        // the user with a filtered list (tables-only or views-only),
        // then opens the chosen object and persists it to the
        // session ini. The "table vs view" distinction relies on
        // DbDuo's view-name convention (a leading "view_" prefix on
        // a table name marks it as a view), since some ADODB
        // providers report views and base tables with identical
        // schema metadata.
        private void selectFromList(List<string> lNames, string sCommand, string sNoun)
        {
            if (db == null || !db.isOpen()) return;
            if (lNames == null || lNames.Count == 0)
            {
                string sMsg = "No " + sNoun + "s found in this database file.";
                if (sNoun == "view")
                    sMsg += " (Views are tables whose names start with 'view_'.)";
                MessageBox.Show(this, sMsg, sCommand,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LiveRegion.say(sMsg);
                return;
            }
            string sChosen = promptListChoice(sCommand,
                "Choose a " + sNoun + ":", lNames, db.currentTable);
            if (string.IsNullOrEmpty(sChosen)) return;
            try
            {
                db.selectTable(sChosen);
                invokeRefresh();
                announceTableOpened();
                IniSession.lastTable = sChosen;
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, sCommand, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Switch-Table / Switch-TablePrevious: cycle to the next or
        // previous table the user has visited in this session. The
        // ring is the same one tracked by cycleVisitedTable; these
        // handlers are the menu-item-bound entry points.
        //
        // Both keys are now ordinary menu shortcuts (Control+Tab and
        // Control+Shift+Tab). The KeyMap dispatcher routes them to
        // these handlers, which gives them the same disabled-state
        // announcement behavior as every other bound hotkey.
        private void schemaSwitchClicked(object oSender, EventArgs oArgs)
        {
            cycleVisitedTable(true);
        }
        private void schemaSwitchPrevClicked(object oSender, EventArgs oArgs)
        {
            cycleVisitedTable(false);
        }

        // Switch-Object / Switch-ObjectPrevious: cycle through ALL
        // tables and views in the database, not just those visited
        // this session. The ring is db.getTableAndViewNames() in
        // sorted order. Bound to Control+F6 / Control+Shift+F6
        // (the canonical Windows MDI "next document" / "previous
        // document" hotkeys, also used by Word and Visual Studio).
        //
        // Contrast with Switch-Table (Control+Tab) which cycles only
        // the tables the user has explicitly opened this session.
        // Switch-Object is the "show me everything in this database"
        // ring; Switch-Table is the "back to where I was" ring.
        private void schemaSwitchAllClicked(object oSender, EventArgs oArgs)
        {
            cycleAllTables(true);
        }
        private void schemaSwitchAllPrevClicked(object oSender, EventArgs oArgs)
        {
            cycleAllTables(false);
        }

        private void cycleAllTables(bool bForward)
        {
            if (db == null || !db.isOpen())
            {
                LiveRegion.say("No database file open; press Control+O to open one");
                return;
            }
            List<string> lAll = db.getTableAndViewNames();
            if (lAll.Count == 0)
            {
                LiveRegion.say("No tables or views in this database");
                return;
            }
            if (lAll.Count < 2)
            {
                LiveRegion.say("Only one object in this database");
                return;
            }
            string sCurrent = db.currentTable;
            int iCurrent = -1;
            for (int i = 0; i < lAll.Count; i++)
            {
                if (string.Equals(lAll[i], sCurrent, StringComparison.OrdinalIgnoreCase))
                { iCurrent = i; break; }
            }
            if (iCurrent < 0) iCurrent = 0;
            int iNext = bForward
                ? (iCurrent + 1) % lAll.Count
                : (iCurrent - 1 + lAll.Count) % lAll.Count;
            string sNext = lAll[iNext];
            try
            {
                db.selectTable(sNext);
                invokeRefresh();
                int iCount = db.recordCount;
                string sKind = db.isViewName(sNext) ? "view" : "table";
                string sPosOf = "position " + (iNext + 1) + " of " + lAll.Count;
                string sRows = ", " + iCount + (iCount == 1 ? " row" : " rows");
                LiveRegion.say(sNext + " (" + sKind + "), " + sPosOf + sRows);
                IniSession.lastTable = sNext;
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Switch-Object", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void schemaShowClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.isOpen()) return;
            StringBuilder oSb = new StringBuilder();
            List<string> lTables = db.getTableNames();
            List<string> lViews = db.getViewNames();
            if (lTables.Count > 0)
            {
                oSb.AppendLine("Tables (" + lTables.Count + "):");
                foreach (string sN in lTables) oSb.AppendLine("  " + sN);
            }
            if (lViews.Count > 0)
            {
                if (lTables.Count > 0) oSb.AppendLine();
                oSb.AppendLine("Views (" + lViews.Count + "):");
                foreach (string sN in lViews) oSb.AppendLine("  " + sN);
            }
            HelpDialog.show(this, "Show-Schema", oSb.ToString());
        }

        // =====================================================================
        // Set-Mark / Clear-Mark: toggle the 'marked' boolean column on
        // the current row. Standard convention from Pax.db's marked
        // column and the older AccAudit schema. The column defaults to
        // 0; Set-Mark stores 1, Clear-Mark stores 0.
        //
        // Refuses if the current table has no 'marked' column.
        // =====================================================================
        private void recMarkClicked(object oSender, EventArgs oArgs)
        {
            setMarkValue(true);
        }

        private void recUnmarkClicked(object oSender, EventArgs oArgs)
        {
            setMarkValue(false);
        }

        private void setMarkValue(bool bValue)
        {
            if (db == null || !db.hasRecordset() || db.readOnly) return;
            if (db.currentIsView) {
                MessageBox.Show(this, "Cannot mark rows of a view (read-only).",
                    bValue ? "Set-Mark" : "Clear-Mark",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (db.eof || db.bof) return;
            if (!db.hasField(Metadata.MarkedColumn))
            {
                MessageBox.Show(this,
                    "Current table has no '" + Metadata.MarkedColumn + "' column. "
                    + "Set-Mark and Clear-Mark require the standard metadata column.",
                    bValue ? "Set-Mark" : "Clear-Mark",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                db.setFieldValue(Metadata.MarkedColumn, bValue ? "1" : "0");
                db.update();
                invokeRefresh();
            }
            catch (Exception oEx)
            {
                try { db.cancelUpdate(); } catch { }
                MessageBox.Show(this, oEx.Message,
                    bValue ? "Set-Mark" : "Clear-Mark",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =====================================================================
        // Show-Related: pick a foreign key on the current row and open
        // a child form filtered to the related rows.
        //
        // For the moment, this is a name-based heuristic: any column
        // ending in _id (other than the table's own primary key) is
        // assumed to be a foreign key whose target table is the column
        // name without the _id suffix. So 'app_id' on a 'screens' row
        // means "show me apps where app_id = N", and 'screen_id' on an
        // 'issues' row means "show me screens where screen_id = N".
        //
        // A future version could read PRAGMA foreign_key_list(<table>)
        // to be authoritative; the heuristic works for any schema that
        // follows the <table>_id naming convention.
        // =====================================================================
        private void recRelatedClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            if (db.eof || db.bof) return;

            // Build a list of candidate FK columns: every column ending
            // in _id except the current table's own primary key column.
            string sOwnPk = (db.currentTable ?? "") + Metadata.PrimaryKeySuffix;
            List<string> lCandidates = new List<string>();
            foreach (string sCol in db.getFieldNames())
            {
                if (sCol.ToLowerInvariant() == sOwnPk.ToLowerInvariant()) continue;
                if (sCol.ToLowerInvariant().EndsWith(Metadata.PrimaryKeySuffix))
                    lCandidates.Add(sCol);
            }
            if (lCandidates.Count == 0)
            {
                MessageBox.Show(this,
                    "No foreign-key columns found on the current row.\n\n"
                    + "Show-Related uses the convention that any column ending "
                    + "in '_id' (other than the table's own primary key) refers "
                    + "to another table by the prefix.",
                    "Show-Related", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Default to the current column if it's a candidate.
            string sCurrent = "";
            if (grid != null && grid.Columns.Count > 0)
            {
                int iCol = iCurrentColumnIndex;
                if (iCol < 0 || iCol >= grid.Columns.Count) iCol = 0;
                sCurrent = grid.Columns[iCol].Name;
                if (string.IsNullOrEmpty(sCurrent)) sCurrent = grid.Columns[iCol].Text;
            }
            if (!lCandidates.Contains(sCurrent)) sCurrent = lCandidates[0];

            string sChosen = promptListChoice("Show-Related",
                "Choose a foreign-key column:", lCandidates, sCurrent);
            if (string.IsNullOrEmpty(sChosen)) return;

            // Compute target table: column name minus '_id' suffix.
            string sValue = db.getFieldValue(sChosen);
            string sTarget = sChosen.Substring(0,
                sChosen.Length - Metadata.PrimaryKeySuffix.Length);

            // Verify the target exists.
            List<string> lAllTables = db.getTableAndViewNames();
            string sTargetActual = null;
            foreach (string sT in lAllTables)
            {
                if (sT.ToLowerInvariant() == sTarget.ToLowerInvariant())
                { sTargetActual = sT; break; }
            }
            // Try plural -> singular trim if not found, else give up.
            if (sTargetActual == null && sTarget.EndsWith("s"))
            {
                string sSingular = sTarget.Substring(0, sTarget.Length - 1);
                foreach (string sT in lAllTables)
                {
                    if (sT.ToLowerInvariant() == sSingular.ToLowerInvariant())
                    { sTargetActual = sT; break; }
                }
            }
            if (sTargetActual == null)
            {
                MessageBox.Show(this,
                    "Cannot find target table for column '" + sChosen + "'.\n\n"
                    + "Looked for table named '" + sTarget + "'.",
                    "Show-Related", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Switch to the target table and apply a filter on the
            // matching column. The FK target column on the target table
            // is conventionally the same name as the FK column on this
            // table (so 'screens.app_id' joins to 'apps.app_id').
            try
            {
                db.selectTable(sTargetActual);
                if (!string.IsNullOrEmpty(sValue))
                    db.applyFilter(sChosen + " = " + sValue);
                invokeRefresh();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this,
                    "Could not navigate to related rows: " + oEx.Message,
                    "Show-Related", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =====================================================================
        // Enter-Child / Exit-Child: parent-to-child drill-down with
        // a navigation stack. Distinct from Show-Related (which goes
        // child to parent via the row's foreign-key columns); these
        // commands operate in the opposite direction.
        //
        // On Enter-Child, DbDuo finds every other table that has a
        // column matching the current table's primary-key name (the
        // dbDot convention <singular>_id). If exactly one such child
        // table exists, it is opened directly; if several, the user
        // picks from an alphabetized listbox.
        //
        // The child is then filtered to rows whose foreign-key value
        // equals the parent's primary-key value. The child's last-
        // used sort order is restored automatically by the existing
        // per-table TableSettings cache.
        //
        // Exit-Child pops one level off the stack and returns the
        // user to the same parent row they drilled from (located by
        // primary-key value, not by absolute position, so the
        // navigation is robust against intervening filter/sort
        // changes in the parent).
        //
        // The stack is unbounded; nesting issues -> screens -> issues
        // is fine. Closing the database clears the stack.
        // =====================================================================
        private class DrillEntry
        {
            public string sParentTable;    // table the user came from
            public string sParentPkColumn; // PK column name on parent
            public string sParentPkValue;  // PK value of the row at push time
        }
        private Stack<DrillEntry> oDrillStack = new Stack<DrillEntry>();

        // Given a table name, compute the expected primary-key column
        // name per dbDot convention: drop trailing 's' (plural -> singular),
        // append '_id'. Returns the actual column name from the table's
        // schema (case-preserving) if it exists, else the conventional name.
        private string computePrimaryKeyColumn(string sTable, List<string> lCols)
        {
            if (string.IsNullOrEmpty(sTable)) return null;
            string sLowerTable = sTable.ToLowerInvariant();
            // First try the plural-to-singular form.
            string sSingular = (sLowerTable.EndsWith("s") && sLowerTable.Length > 1)
                ? sLowerTable.Substring(0, sLowerTable.Length - 1)
                : sLowerTable;
            string sPkPlural = sSingular + Metadata.PrimaryKeySuffix;
            // Walk the column list; case-insensitive match returns the
            // actual case from the schema.
            foreach (string sCol in lCols)
            {
                if (sCol.ToLowerInvariant() == sPkPlural) return sCol;
            }
            // Second try: <table>_id (no plural stripping).
            string sPkBare = sLowerTable + Metadata.PrimaryKeySuffix;
            foreach (string sCol in lCols)
            {
                if (sCol.ToLowerInvariant() == sPkBare) return sCol;
            }
            // Third try: a bare 'id' column.
            foreach (string sCol in lCols)
            {
                if (sCol.ToLowerInvariant() == "id") return sCol;
            }
            return null;
        }

        // Build the ADO Filter expression for "<column> = <value>".
        // Strings get single-quoted (with embedded quotes doubled);
        // anything that parses as a number is left bare. Empty values
        // produce "<column> IS NULL".
        private string buildFkFilter(string sColumn, string sValue)
        {
            if (string.IsNullOrEmpty(sValue)) return sColumn + " IS NULL";
            double dN;
            if (double.TryParse(sValue, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out dN))
                return sColumn + " = " + sValue;
            // Quote as string, doubling embedded single quotes.
            return sColumn + " = '" + sValue.Replace("'", "''") + "'";
        }

        private void recEnterChildClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            if (db.eof || db.bof)
            {
                MessageBox.Show(this, "No current row to drill from.",
                    "Enter-Child", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string sParentTable = db.currentTable ?? "";
            if (string.IsNullOrEmpty(sParentTable)) return;

            // Compute the parent's primary-key column.
            List<string> lParentCols = db.getFieldNames();
            string sParentPk = computePrimaryKeyColumn(sParentTable, lParentCols);
            if (string.IsNullOrEmpty(sParentPk))
            {
                MessageBox.Show(this,
                    "Cannot determine the primary-key column for '" + sParentTable + "'.\n\n"
                    + "Enter-Child uses the convention '<table>_id' (or 'id') for the\n"
                    + "primary key. The current table has no column matching this pattern.",
                    "Enter-Child", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string sParentPkValue;
            try { sParentPkValue = db.getFieldValue(sParentPk) ?? ""; }
            catch { sParentPkValue = ""; }
            if (string.IsNullOrEmpty(sParentPkValue))
            {
                MessageBox.Show(this,
                    "The current row has no value for '" + sParentPk + "'.\n\n"
                    + "Enter-Child requires the parent row to have a primary-key value.",
                    "Enter-Child", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Find every other table that has a column named the same as
            // the parent's PK column -- those are the candidate children.
            // Views are excluded; you drill into base tables only.
            List<string> lAllTables = db.getTableNames();
            List<string> lChildren = new List<string>();
            foreach (string sCandidate in lAllTables)
            {
                if (string.Equals(sCandidate, sParentTable, StringComparison.OrdinalIgnoreCase))
                    continue;
                List<string> lCols = db.getColumnsOfTable(sCandidate);
                foreach (string sCol in lCols)
                {
                    if (string.Equals(sCol, sParentPk, StringComparison.OrdinalIgnoreCase))
                    {
                        lChildren.Add(sCandidate);
                        break;
                    }
                }
            }
            lChildren.Sort(StringComparer.OrdinalIgnoreCase);

            if (lChildren.Count == 0)
            {
                MessageBox.Show(this,
                    "No child tables found that reference '" + sParentPk + "'.\n\n"
                    + "Enter-Child looks for other tables that have a column with the\n"
                    + "same name as this table's primary key.",
                    "Enter-Child", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string sChosen;
            if (lChildren.Count == 1)
            {
                sChosen = lChildren[0];
            }
            else
            {
                sChosen = promptListChoice("Enter-Child",
                    "Choose a child table to drill into:", lChildren, lChildren[0]);
                if (string.IsNullOrEmpty(sChosen)) return;
            }

            // Push a stack entry BEFORE switching tables, so we
            // remember exactly where we came from. The TableSettings
            // cache will save the parent's current filter/sort/
            // position automatically as part of the table switch.
            DrillEntry oEntry = new DrillEntry();
            oEntry.sParentTable = sParentTable;
            oEntry.sParentPkColumn = sParentPk;
            oEntry.sParentPkValue = sParentPkValue;
            oDrillStack.Push(oEntry);

            try
            {
                // Switching the table auto-applies any cached
                // TableSettings (sort, filter, position) for the child.
                // We then override the filter with the FK constraint;
                // the cached sort persists, which is exactly what the
                // user asked for.
                db.selectTable(sChosen);
                string sFkFilter = buildFkFilter(sParentPk, sParentPkValue);
                try { db.applyFilter(sFkFilter); }
                catch (Exception oExFilter)
                {
                    MessageBox.Show(this,
                        "Could not apply filter '" + sFkFilter + "': " + oExFilter.Message,
                        "Enter-Child", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                // Position at the top of the filtered set.
                try { db.moveFirst(); } catch { }
                invokeRefresh();
            }
            catch (Exception oEx)
            {
                // Roll back the stack push since the table switch failed.
                if (oDrillStack.Count > 0) oDrillStack.Pop();
                MessageBox.Show(this,
                    "Could not open child table '" + sChosen + "': " + oEx.Message,
                    "Enter-Child", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void recExitChildClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.isOpen()) return;
            if (oDrillStack.Count == 0)
            {
                MessageBox.Show(this,
                    "Nothing to exit -- the drill-down stack is empty.\n\n"
                    + "Exit-Child returns from a child table opened via Enter-Child.",
                    "Exit-Child", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DrillEntry oEntry = oDrillStack.Pop();
            try
            {
                // Switch back to the parent. selectTable will restore
                // the parent's cached TableSettings (sort, filter,
                // and absolute-position-at-time-of-push).
                db.selectTable(oEntry.sParentTable);

                // Robustness: re-find the row by PK value rather than
                // trusting the position cache. If the user added or
                // removed rows on the parent while we were in the
                // child, the absolute position could now point to the
                // wrong row -- but the PK still uniquely identifies
                // the original.
                if (!string.IsNullOrEmpty(oEntry.sParentPkColumn)
                    && !string.IsNullOrEmpty(oEntry.sParentPkValue))
                {
                    string sFindExpr = buildFkFilter(oEntry.sParentPkColumn, oEntry.sParentPkValue);
                    // Try a Find on the current filtered view first;
                    // if the row was filtered out, briefly reset
                    // filter and try again.
                    bool bFound = false;
                    try
                    {
                        db.moveFirst();
                        bFound = db.findRecord(sFindExpr, true, false);
                    }
                    catch { }
                    if (!bFound)
                    {
                        // Row may be excluded by an active filter; try
                        // again with the filter momentarily cleared.
                        string sSavedFilter = db.filter;
                        try
                        {
                            db.applyFilter("");
                            db.moveFirst();
                            bFound = db.findRecord(sFindExpr, true, false);
                        }
                        catch { }
                        if (!bFound && !string.IsNullOrEmpty(sSavedFilter))
                        {
                            // Couldn't find it even without filter; the
                            // row was deleted. Restore the original
                            // filter so the parent table is back where
                            // the user left it.
                            try { db.applyFilter(sSavedFilter); } catch { }
                        }
                    }
                }
                invokeRefresh();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this,
                    "Could not return to parent table '" + oEntry.sParentTable + "': " + oEx.Message,
                    "Exit-Child", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =====================================================================
        // Update-Field: replace values across rows in a chosen column.
        // Asks for column, find string, replace string, scope (current
        // row, filtered rows, or all rows). Logs every change.
        //
        // Implemented as ADO recordset iteration with field assignment;
        // does not generate SQL UPDATE. This means it goes through the
        // same update triggers as a Set-Record edit.
        // =====================================================================
        private void recUpdateFieldClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset() || db.readOnly) return;
            if (db.currentIsView) {
                MessageBox.Show(this, "Cannot update fields of a view (read-only).",
                    "Update-Field", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // For now, surface a placeholder dialog. Full implementation
            // is a future iteration: needs find/replace dialog with
            // column picker, scope, case-sensitivity, and dry-run preview.
            MessageBox.Show(this,
                "Update-Field is not yet implemented in this build.\n\n"
                + "It will provide a find-and-replace dialog with column, "
                + "scope (current/filtered/all rows), and dry-run preview.\n\n"
                + "For now, use Tools > Invoke-Sql to run an UPDATE statement "
                + "directly.",
                "Update-Field", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // =====================================================================
        // Get-Property: show schema details for the current table:
        // columns and types, indexes, and any inferred foreign-key
        // relationships (columns ending in _id that match another
        // table's name).
        // =====================================================================
        private void schemaPropertiesClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            string sTable = db.currentTable;
            if (string.IsNullOrEmpty(sTable)) return;

            StringBuilder oSb = new StringBuilder();
            oSb.AppendLine((db.currentIsView ? "View" : "Table") + ": " + sTable);
            oSb.AppendLine();

            // Columns
            oSb.AppendLine("Columns:");
            List<string> lDistinct = db.getDistinctFieldNames();
            List<string> lMetadata = db.getMetadataFieldNames();
            foreach (string sCol in lDistinct)
            {
                int iType = db.getFieldType(sCol);
                int iSize = db.getFieldDefinedSize(sCol);
                oSb.AppendLine("  " + sCol
                    + "  [" + db.getFieldTypeName(sCol) + "]"
                    + (iSize > 0 && iSize < 0x7FFFFFFF ? " size=" + iSize : ""));
            }
            if (lMetadata.Count > 0)
            {
                oSb.AppendLine();
                oSb.AppendLine("metadata columns:");
                foreach (string sCol in lMetadata)
                    oSb.AppendLine("  " + sCol + "  [" + db.getFieldTypeName(sCol) + "]");
            }

            // Inferred foreign keys (columns ending in _id, excluding own PK)
            string sOwnPk = sTable + Metadata.PrimaryKeySuffix;
            List<string> lFks = new List<string>();
            foreach (string sCol in db.getFieldNames())
            {
                if (sCol.ToLowerInvariant() == sOwnPk.ToLowerInvariant()) continue;
                if (sCol.ToLowerInvariant().EndsWith(Metadata.PrimaryKeySuffix))
                    lFks.Add(sCol);
            }
            if (lFks.Count > 0)
            {
                oSb.AppendLine();
                oSb.AppendLine("Inferred foreign keys (by name convention):");
                foreach (string sFk in lFks)
                {
                    string sTarget = sFk.Substring(0, sFk.Length - Metadata.PrimaryKeySuffix.Length);
                    oSb.AppendLine("  " + sFk + " -> " + sTarget);
                }
            }

            oSb.AppendLine();
            oSb.AppendLine("Row count: " + db.recordCount);

            HelpDialog.show(this, "Get-Property: " + sTable, oSb.ToString());
        }

        // =====================================================================
        // Select-Column: choose which columns are visible in the
        // ListView. The user edits a comma-separated list of column
        // names in a text box. Each name is validated against the
        // current recordset; unknown names are dropped silently.
        // An empty list reverts to the rule-based default.
        //
        // PowerShell-canonical alias: Select-Object (which in
        // PowerShell takes -Property to pick fields). Persisted
        // per-table for the session via TableSettings.sSelectList.
        // =====================================================================
        private void viewSelectColumnClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset())
            {
                MessageBox.Show(this, "No table is open.", "Select-Column",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string sCurrentTable = db.currentTable;
            // Pre-fill: existing select-list if set, otherwise the
            // current effective display list (so the user can edit
            // the default rather than start from blank).
            string sInitial = db.getSelectList(sCurrentTable);
            if (string.IsNullOrEmpty(sInitial))
                sInitial = string.Join(", ", db.getDisplayFieldNames().ToArray());

            string sResult = promptSelectList(sInitial);
            if (sResult == null) return;  // user cancelled

            // setSelectList validates and stores. Empty string
            // clears the override.
            string sStored = db.setSelectList(sCurrentTable, sResult);

            // Compute dropped names so we can tell the user.
            List<string> lRequested = DbDuoManager.parseSelectList(sResult);
            List<string> lAccepted = DbDuoManager.parseSelectList(sStored);
            List<string> lDropped = new List<string>();
            HashSet<string> hAccepted = new HashSet<string>(lAccepted, StringComparer.OrdinalIgnoreCase);
            foreach (string sName in lRequested)
                if (!hAccepted.Contains(sName)) lDropped.Add(sName);

            invokeRefresh();

            if (lDropped.Count > 0)
            {
                string sMsg = "Dropped (not column names of this table): "
                    + string.Join(", ", lDropped.ToArray());
                LiveRegion.say(sMsg);
                MessageBox.Show(this, sMsg, "Select-Column",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                LiveRegion.say("Visible columns updated");
            }
        }

        // Modal dialog with a single-line text box pre-filled with
        // the comma-separated column list. OK accepts, Cancel
        // returns null. Empty-string return is meaningful: it
        // clears the user override and reverts to default rules.
        private string promptSelectList(string sInitial)
        {
            using (Form oDlg = new Form())
            {
                oDlg.Text = "Select-Column (visible columns)";
                oDlg.AccessibleName = "Select-Column";
                oDlg.StartPosition = FormStartPosition.CenterParent;
                oDlg.ClientSize = new Size(560, 160);
                oDlg.FormBorderStyle = FormBorderStyle.Sizable;
                oDlg.MaximizeBox = false;
                oDlg.MinimizeBox = false;
                oDlg.ShowInTaskbar = false;
                oDlg.KeyPreview = true;

                Label lbl = new Label();
                lbl.Text = "Comma-separated list of column names to display "
                         + "(empty clears the override and uses defaults):";
                lbl.Location = new Point(12, 12);
                lbl.Size = new Size(536, 36);
                lbl.AutoSize = false;
                oDlg.Controls.Add(lbl);

                TextBox txt = new TextBox();
                txt.Text = sInitial ?? "";
                txt.Location = new Point(12, 56);
                txt.Size = new Size(536, 22);
                txt.AccessibleName = "Comma-separated column names";
                txt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                oDlg.Controls.Add(txt);

                Button btnOk = new Button();
                btnOk.Text = "OK";
                btnOk.DialogResult = DialogResult.OK;
                btnOk.Location = new Point(372, 92);
                btnOk.Size = new Size(80, 28);
                btnOk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                oDlg.Controls.Add(btnOk);

                Button btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.Location = new Point(460, 92);
                btnCancel.Size = new Size(88, 28);
                btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                oDlg.Controls.Add(btnCancel);

                oDlg.AcceptButton = btnOk;
                oDlg.CancelButton = btnCancel;

                if (oDlg.ShowDialog(this) != DialogResult.OK) return null;
                return txt.Text.Trim();
            }
        }
        // =====================================================================

        private string promptListChoice(string sTitle, string sPrompt, List<string> lItems, string sCurrent)
        {
            using (Form oDlg = new Form())
            {
                oDlg.Text = sTitle;
                oDlg.AccessibleName = sTitle;
                oDlg.StartPosition = FormStartPosition.CenterParent;
                oDlg.ClientSize = new Size(420, 360);
                oDlg.FormBorderStyle = FormBorderStyle.Sizable;
                oDlg.MaximizeBox = true;
                oDlg.MinimizeBox = false;
                oDlg.ShowInTaskbar = false;
                oDlg.KeyPreview = true;

                Label lbl = new Label();
                lbl.Text = sPrompt;
                lbl.Location = new Point(12, 12);
                lbl.Size = new Size(396, 20);
                oDlg.Controls.Add(lbl);

                ListBox lb = new ListBox();
                lb.AccessibleName = sPrompt;
                lb.Location = new Point(12, 36);
                lb.Size = new Size(396, 270);
                lb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
                foreach (string s in lItems) lb.Items.Add(s);
                if (sCurrent != null && lb.Items.Contains(sCurrent)) lb.SelectedItem = sCurrent;
                else if (lb.Items.Count > 0) lb.SelectedIndex = 0;
                lb.DoubleClick += (s, e) => { oDlg.DialogResult = DialogResult.OK; oDlg.Close(); };
                oDlg.Controls.Add(lb);

                Button btnOk = new Button();
                btnOk.Text = "&OK";
                btnOk.DialogResult = DialogResult.OK;
                btnOk.Size = new Size(90, 28);
                btnOk.Location = new Point(218, 320);
                btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                oDlg.Controls.Add(btnOk);

                Button btnCancel = new Button();
                btnCancel.Text = "&Cancel";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.Size = new Size(90, 28);
                btnCancel.Location = new Point(316, 320);
                btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                oDlg.Controls.Add(btnCancel);

                oDlg.AcceptButton = btnOk;
                oDlg.CancelButton = btnCancel;
                oDlg.ActiveControl = lb;

                if (oDlg.ShowDialog(this) != DialogResult.OK) return null;
                return lb.SelectedItem as string;
            }
        }

        // Test-Database integrity. Runs PRAGMA integrity_check (SQLite)
        // or a driver-equivalent. The result is "ok" on a healthy
        // database (one line, typically), or a list of corruption
        // findings. We pick the presentation by size:
        //
        //   - Short result (under ~10 lines, under ~400 chars): a
        //     standard MessageBox is sufficient and dismissable with
        //     Enter / Escape / OK.
        //   - Long result: HelpDialog with a read-only multi-line
        //     TextBox and an OK button.
        //
        // This pattern is the project-wide rule: short feedback uses
        // MessageBox, extensive output uses HelpDialog. Both have a
        // dismiss button so the user knows how to close them.
        private void toolsTestClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.isOpen()) return;
            try
            {
                StringWriter oOut = new StringWriter();
                db.invokeSql("PRAGMA integrity_check", oOut);
                string sResult = oOut.ToString();
                if (isShortResult(sResult))
                    MessageBox.Show(this, sResult.Trim(), "Test-Database",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    HelpDialog.show(this, "Test-Database", sResult);
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Test-Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // True if a result string is brief enough that a MessageBox
        // suffices. Threshold tuned for typical Windows MessageBox
        // legibility: roughly 10 lines or 400 characters.
        private static bool isShortResult(string sText)
        {
            if (string.IsNullOrEmpty(sText)) return true;
            if (sText.Length > 400) return false;
            int iLines = 0;
            for (int i = 0; i < sText.Length; i++)
                if (sText[i] == '\n') iLines++;
            return iLines < 10;
        }

        private void toolsMeasureClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            string sChosenField = promptListChoice("Measure-Field", "Choose a field:", db.getFieldNames(), null);
            if (string.IsNullOrEmpty(sChosenField)) return;
            List<string> lStats = new List<string> { "count", "longest", "shortest", "min", "max", "sum", "avg" };
            string sChosenStat = promptListChoice("Measure-Field", "Choose a statistic:", lStats, "count");
            if (string.IsNullOrEmpty(sChosenStat)) return;
            try
            {
                DbDuoManager.FieldStatistic oStat = db.measureField(sChosenField, sChosenStat);
                StringBuilder oSb = new StringBuilder();
                oSb.AppendLine("Field: " + oStat.fieldName);
                oSb.AppendLine("Statistic: " + oStat.statistic);
                oSb.AppendLine("Value: " + oStat.value);
                if (oStat.recordPosition > 0) oSb.AppendLine("At row: " + oStat.recordPosition);
                string sResult = oSb.ToString();
                if (isShortResult(sResult))
                    MessageBox.Show(this, sResult.Trim(), "Measure-Field",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    HelpDialog.show(this, "Measure-Field", sResult);
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Measure-Field", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolsInvokeSqlClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.isOpen()) return;
            string sSql = promptInvokeSql();
            if (string.IsNullOrEmpty(sSql)) return;
            try
            {
                StringWriter oOut = new StringWriter();
                int iAffected = db.invokeSql(sSql, oOut);
                if (iAffected >= 0)
                    oOut.WriteLine("(" + iAffected + " record(s) affected)");
                string sResult = oOut.ToString();
                if (isShortResult(sResult))
                    MessageBox.Show(this, sResult.Trim(), "Invoke-Sql",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    HelpDialog.show(this, "Invoke-Sql", sResult);
                if (iAffected >= 0) invokeRefresh();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Invoke-Sql", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string promptInvokeSql()
        {
            using (Form oDlg = new Form())
            {
                oDlg.Text = "Invoke-Sql";
                oDlg.AccessibleName = "Invoke-Sql";
                oDlg.StartPosition = FormStartPosition.CenterParent;
                oDlg.ClientSize = new Size(640, 320);
                oDlg.FormBorderStyle = FormBorderStyle.Sizable;
                oDlg.MaximizeBox = true;
                oDlg.MinimizeBox = false;
                oDlg.ShowInTaskbar = false;
                oDlg.KeyPreview = true;

                Label lbl = new Label();
                lbl.Text = "Enter a &SQL command (Ctrl+Enter to run):";
                lbl.Location = new Point(12, 12);
                lbl.Size = new Size(616, 20);
                oDlg.Controls.Add(lbl);

                TextBox tb = new TextBox();
                tb.AccessibleName = "SQL command";
                tb.Multiline = true;
                tb.ScrollBars = ScrollBars.Vertical;
                tb.AcceptsReturn = true;
                tb.AcceptsTab = false;
                tb.Font = new Font(FontFamily.GenericMonospace, 10f);
                tb.Location = new Point(12, 36);
                tb.Size = new Size(616, 230);
                tb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
                tb.KeyDown += (s, e) => {
                    if (e.Control && e.KeyCode == Keys.Enter)
                    {
                        oDlg.DialogResult = DialogResult.OK;
                        oDlg.Close();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                };
                oDlg.Controls.Add(tb);

                Button btnOk = new Button();
                btnOk.Text = "&OK";
                btnOk.DialogResult = DialogResult.OK;
                btnOk.Size = new Size(90, 28);
                btnOk.Location = new Point(440, 280);
                btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                oDlg.Controls.Add(btnOk);

                Button btnCancel = new Button();
                btnCancel.Text = "&Cancel";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.Size = new Size(90, 28);
                btnCancel.Location = new Point(536, 280);
                btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                oDlg.Controls.Add(btnCancel);

                oDlg.AcceptButton = btnOk;
                oDlg.CancelButton = btnCancel;
                oDlg.ActiveControl = tb;

                if (oDlg.ShowDialog(this) != DialogResult.OK) return null;
                return tb.Text;
            }
        }

        private void toolsLockClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.isOpen()) return;
            string sPath = db.filePath;
            string sTable = db.currentTable;
            bool bWasReadOnly = db.readOnly;
            try
            {
                db.close();
                db.openDatabase(sPath, sTable, !bWasReadOnly);
                invokeRefresh();
                MessageBox.Show(this, "Database is now " + (db.readOnly ? "read-only" : "read-write") + ".",
                    "Lock-Database", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Lock-Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolsTestDriverClicked(object oSender, EventArgs oArgs)
        {
            StringBuilder oSb = new StringBuilder();
            oSb.AppendLine("Test-Driver: probing registered ODBC drivers and OLE DB providers.");
            oSb.AppendLine();

            // ODBC drivers: HKLM\SOFTWARE\ODBC\ODBCINST.INI\ODBC Drivers
            oSb.AppendLine("=== ODBC drivers ===");
            try
            {
                Microsoft.Win32.RegistryKey oKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\ODBC\ODBCINST.INI\ODBC Drivers");
                if (oKey != null)
                {
                    foreach (string sName in oKey.GetValueNames())
                        oSb.AppendLine("  " + sName + " = " + oKey.GetValue(sName));
                    oKey.Close();
                }
                else oSb.AppendLine("  (no drivers registry key)");
            }
            catch (Exception oEx) { oSb.AppendLine("  (error: " + oEx.Message + ")"); }

            // SQLite ODBC presence
            oSb.AppendLine();
            oSb.AppendLine("=== SQLite ODBC Driver ===");
            bool bSqlite = checkOdbcDriver("SQLite3 ODBC Driver");
            if (bSqlite)
                oSb.AppendLine("  PRESENT - .db / .sqlite / .sqlite3 files will work.");
            else
            {
                oSb.AppendLine("  MISSING - .db / .sqlite / .sqlite3 files cannot be opened.");
                oSb.AppendLine("  Install: download sqliteodbc_w64.exe from");
                oSb.AppendLine("           http://www.ch-werner.de/sqliteodbc/");
                oSb.AppendLine("  (No WinGet or Chocolatey package exists for this driver.)");
            }

            // ACE OLE DB Provider presence
            oSb.AppendLine();
            oSb.AppendLine("=== Microsoft Access Database Engine (ACE OLE DB) ===");
            bool bAce = checkClsId("Microsoft.ACE.OLEDB.16.0") || checkClsId("Microsoft.ACE.OLEDB.12.0");
            if (bAce)
                oSb.AppendLine("  PRESENT - .mdb / .accdb / .xlsx / .xls / .dbf / .csv files will work.");
            else
            {
                oSb.AppendLine("  MISSING - .mdb / .accdb / .xlsx / .xls / .dbf / .csv files cannot be opened.");
                oSb.AppendLine("  Install (any of these works):");
                oSb.AppendLine("    winget install --id Microsoft.AccessDatabaseEngine.2016 --silent");
                oSb.AppendLine("    choco install made-2016 -y");
                oSb.AppendLine("  Or download accessdatabaseengine_X64.exe and run with /passive from:");
                oSb.AppendLine("    https://www.microsoft.com/en-us/download/details.aspx?id=54920");
            }

            HelpDialog.show(this, "Test-Driver", oSb.ToString());
        }

        private static bool checkOdbcDriver(string sName)
        {
            try
            {
                Microsoft.Win32.RegistryKey oKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\ODBC\ODBCINST.INI\ODBC Drivers");
                if (oKey == null) return false;
                object oV = oKey.GetValue(sName);
                oKey.Close();
                return oV != null;
            }
            catch { return false; }
        }

        private static bool checkClsId(string sProgId)
        {
            try
            {
                Type oType = Type.GetTypeFromProgID(sProgId);
                return oType != null;
            }
            catch { return false; }
        }

        private void toolsConsoleClicked(object oSender, EventArgs oArgs)
        {
            DotPromptHost.enter(this);
        }

        // Edit-Configuration: open a Configuration Settings dialog
        // with one control per common option. The dialog uses the
        // Layout by Code (LbcDialog) helper for consistent layout
        // and accessibility. OK writes the changes to the per-user
        // DbDuo.ini at %LOCALAPPDATA%\DbDuo\DbDuo.ini and reminds
        // the user that some changes take effect on next launch.
        // An "Open file..." button is provided for raw editing,
        // useful for [Keys] overrides and other settings the GUI
        // doesn't expose yet.
        //
        // Bound to F12.
        //
        // Common settings exposed in the GUI:
        //   - uiMode (gui / cli / both)
        //   - default Markdown reading level for exports (informational)
        //   - "Open file..." escape hatch to the raw .ini
        //
        // Settings deliberately not exposed (managed automatically
        // or only relevant for power users):
        //   - [Session] last-opened state (DbDuo writes it itself)
        //   - [Folders] last-used directories (DbDuo writes it itself)
        //   - [Keys] hotkey overrides (rare; raw editing is fine)
        private void toolsEditConfigClicked(object oSender, EventArgs oArgs)
        {
            string sUserIni = configIniPath();
            if (string.IsNullOrEmpty(sUserIni)) return;
            ensureConfigIniExists(sUserIni);

            // Read current values from the ini, with sensible defaults
            // if the keys are missing.
            string sCurrentUiMode = readIniValue(sUserIni, "General", "uiMode", "both");

            LbcDialog oDlg = new LbcDialog("Edit-Configuration", this);
            try
            {
                oDlg.addLabel("Settings stored in: " + sUserIni);
                oDlg.addLabel("Most changes take effect the next time DbDuo starts.");
                oDlg.addSeparator();

                // uiMode is a pick-from-three -- ComboBox is perfect.
                ComboBox cbUiMode = oDlg.addComboBox(
                    "UI mode at launch (overridden by -cli / -gui / -both):",
                    new string[] { "both", "gui", "cli" },
                    sCurrentUiMode);

                oDlg.addSeparator();
                oDlg.addLabel("For [Keys] overrides and advanced settings, use the");
                oDlg.addLabel("\"Open file...\" button to edit DbDuo.ini directly.");

                string sResult = oDlg.runWithButtons(new string[] { "OK", "Open file...", "Cancel" });
                if (string.Equals(sResult, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    string sNewUiMode = (cbUiMode.SelectedItem ?? "both").ToString();
                    writeIniValue(sUserIni, "General", "uiMode", sNewUiMode);
                    LiveRegion.say("Configuration saved");
                    DbDuoLog.write("Edit-Configuration saved: uiMode=" + sNewUiMode);
                    MessageBox.Show(this,
                        "Configuration saved to " + sUserIni
                        + "\n\nRestart DbDuo for the new uiMode to take effect.",
                        "Edit-Configuration",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (string.Equals(sResult, "Open file...", StringComparison.OrdinalIgnoreCase))
                {
                    openIniInTextEditor(sUserIni);
                }
                // Cancel / Escape / close: do nothing.
            }
            finally
            {
                oDlg.Dispose();
            }
        }

        // Helper: path of the per-user DbDuo.ini. Returns null and
        // shows an error dialog if %LOCALAPPDATA% is unavailable.
        private string configIniPath()
        {
            string sBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(sBase))
            {
                MessageBox.Show(this,
                    "%LOCALAPPDATA% is not set. Cannot locate per-user DbDuo.ini.",
                    "Edit-Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            string sDir = Path.Combine(sBase, "DbDuo");
            try { if (!Directory.Exists(sDir)) Directory.CreateDirectory(sDir); }
            catch { }
            return Path.Combine(sDir, "DbDuo.ini");
        }

        // Helper: ensure DbDuo.ini exists at the given path. Seeds
        // from the shipped template next to the EXE if available,
        // otherwise writes a minimal stub.
        private void ensureConfigIniExists(string sUserIni)
        {
            if (File.Exists(sUserIni)) return;
            string sShipped = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
                "DbDuo.ini");
            if (File.Exists(sShipped))
            {
                try { File.Copy(sShipped, sUserIni, false); }
                catch (Exception oExC)
                {
                    DbDuoLog.write("Edit-Configuration: could not copy shipped ini: " + oExC.Message);
                }
            }
            if (!File.Exists(sUserIni))
            {
                try
                {
                    File.WriteAllText(sUserIni,
                        "; DbDuo per-user configuration." + Environment.NewLine +
                        "; Edit this file to change uiMode, override hotkeys," + Environment.NewLine +
                        "; or inspect remembered Session/Folders state." + Environment.NewLine +
                        "; Restart DbDuo for changes to take effect." + Environment.NewLine +
                        Environment.NewLine +
                        "[General]" + Environment.NewLine +
                        "uiMode = both" + Environment.NewLine +
                        Environment.NewLine +
                        "[Keys]" + Environment.NewLine +
                        "; Command-Name = Key+Combo" + Environment.NewLine);
                }
                catch (Exception oExW)
                {
                    MessageBox.Show(this, "Could not create " + sUserIni + ": " + oExW.Message,
                        "Edit-Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Open the configuration .ini in the system default handler.
        // Falls back to Notepad if the shell call fails. Shared by
        // the "Open file..." button and (for backward compat) any
        // CLI/menu entry that wants to bypass the dialog.
        private void openIniInTextEditor(string sUserIni)
        {
            try
            {
                System.Diagnostics.Process.Start(sUserIni);
                LiveRegion.say("Opened DbDuo.ini for editing");
                DbDuoLog.write("Edit-Configuration opened: " + sUserIni);
            }
            catch
            {
                try { System.Diagnostics.Process.Start("notepad.exe", "\"" + sUserIni + "\""); }
                catch (Exception oEx2)
                {
                    MessageBox.Show(this, "Could not open the file in Notepad: " + oEx2.Message,
                        "Edit-Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Read one value from the ini. Returns sDefault if the key
        // doesn't exist, the file is unreadable, or the value is
        // empty. Section is matched case-insensitively (the ini may
        // be hand-written with mixed case).
        private static string readIniValue(string sPath, string sSection, string sKey, string sDefault)
        {
            if (!File.Exists(sPath)) return sDefault;
            string[] aLines;
            try { aLines = File.ReadAllLines(sPath); } catch { return sDefault; }
            string sHeader = "[" + sSection + "]";
            bool bInSection = false;
            foreach (string sLine in aLines)
            {
                string sTrim = sLine.Trim();
                if (sTrim.Length == 0) continue;
                if (sTrim.StartsWith(";") || sTrim.StartsWith("#")) continue;
                if (sTrim.StartsWith("["))
                {
                    bInSection = sTrim.Equals(sHeader, StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (!bInSection) continue;
                int iEq = sTrim.IndexOf('=');
                if (iEq <= 0) continue;
                string sName = sTrim.Substring(0, iEq).Trim();
                string sValue = sTrim.Substring(iEq + 1).Trim();
                if (string.Equals(sName, sKey, StringComparison.OrdinalIgnoreCase))
                    return string.IsNullOrEmpty(sValue) ? sDefault : sValue;
            }
            return sDefault;
        }

        // Write one value into the ini, preserving every other
        // section/line that is already there. Adds the section
        // and/or key if missing. Removes the key (and only the
        // key) if the value is empty -- callers can pass "" to
        // clear a setting back to the default.
        //
        // This is the same algorithm as IniSession.write, but
        // exposed as a static helper so the configuration dialog
        // can use it without depending on the IniSession class's
        // particular file path.
        private static void writeIniValue(string sPath, string sSection, string sKey, string sValue)
        {
            string sHeader = "[" + sSection + "]";
            List<string> lLines = new List<string>();
            if (File.Exists(sPath))
            {
                try { lLines.AddRange(File.ReadAllLines(sPath)); } catch { return; }
            }

            int iSectionStart = -1;
            int iSectionEnd = -1;
            for (int i = 0; i < lLines.Count; i++)
            {
                string sTrim = lLines[i].Trim();
                if (sTrim.Equals(sHeader, StringComparison.OrdinalIgnoreCase))
                {
                    iSectionStart = i;
                    iSectionEnd = lLines.Count;
                    for (int j = i + 1; j < lLines.Count; j++)
                    {
                        string sJ = lLines[j].Trim();
                        if (sJ.StartsWith("[")) { iSectionEnd = j; break; }
                    }
                    break;
                }
            }

            if (iSectionStart < 0)
            {
                if (lLines.Count > 0 && lLines[lLines.Count - 1].Trim().Length > 0)
                    lLines.Add("");
                lLines.Add(sHeader);
                if (!string.IsNullOrEmpty(sValue)) lLines.Add(sKey + " = " + sValue);
            }
            else
            {
                int iFound = -1;
                for (int i = iSectionStart + 1; i < iSectionEnd; i++)
                {
                    string sT = lLines[i].Trim();
                    if (sT.Length == 0) continue;
                    if (sT.StartsWith(";") || sT.StartsWith("#")) continue;
                    int iEq = sT.IndexOf('=');
                    if (iEq <= 0) continue;
                    string sN = sT.Substring(0, iEq).Trim();
                    if (string.Equals(sN, sKey, StringComparison.OrdinalIgnoreCase))
                    { iFound = i; break; }
                }
                if (iFound >= 0)
                {
                    if (string.IsNullOrEmpty(sValue)) lLines.RemoveAt(iFound);
                    else                              lLines[iFound] = sKey + " = " + sValue;
                }
                else if (!string.IsNullOrEmpty(sValue))
                {
                    lLines.Insert(iSectionStart + 1, sKey + " = " + sValue);
                }
            }

            try { File.WriteAllLines(sPath, lLines.ToArray()); }
            catch (Exception oEx)
            { DbDuoLog.write("writeIniValue FAILED: " + sPath + " -- " + oEx.Message); }
        }

        // Open-FileFolder: open Windows Explorer at the folder containing
        // the currently open database file. EdSharp / FileDir convention
        // (Alt+Backslash). If no database is open, opens %USERPROFILE%
        // as a sensible fallback. Selects the database file in the
        // resulting Explorer window so the user can see exactly which
        // file is open.
        private void toolsOpenFolderClicked(object oSender, EventArgs oArgs)
        {
            try
            {
                string sPath = (db != null && db.isOpen()) ? db.filePath : null;
                if (!string.IsNullOrEmpty(sPath) && File.Exists(sPath))
                {
                    // explorer.exe /select,"path" highlights the file
                    System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + sPath + "\"");
                    LiveRegion.say("Opened folder: " + Path.GetDirectoryName(sPath));
                }
                else
                {
                    string sHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    System.Diagnostics.Process.Start("explorer.exe", sHome);
                    LiveRegion.say("No database open; opened user folder");
                }
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Open-FileFolder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // =====================================================================
        // HELP menu handlers
        // =====================================================================
        private void helpContentsClicked(object oSender, EventArgs oArgs)
        {
            // F1 opens the HTML version of the user guide in the
            // default browser. The HTML lives next to DbDuo.exe and is
            // generated by buildDbDuo.cmd via Pandoc from DbDuo.md.
            // Browsers expose far better navigation (heading-by-
            // heading, links, search, font scaling, screen-reader
            // table-of-contents) than an in-app TextBox could.
            //
            // If DbDuo.htm is missing (e.g., somebody copied DbDuo.exe
            // alone), fall back to a brief text pointer rather than
            // failing silently.
            string sPath = System.IO.Path.Combine(
                Application.StartupPath, "DbDuo.htm");
            if (System.IO.File.Exists(sPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(sPath);
                    return;
                }
                catch (Exception oEx)
                {
                    MessageBox.Show(this,
                        "Could not open the user guide:\n\n" + oEx.Message
                        + "\n\nThe file exists at:\n" + sPath,
                        "Help Contents",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(this,
                    "DbDuo.htm was not found next to DbDuo.exe.\n\n"
                    + "Expected location:\n" + sPath + "\n\n"
                    + "If you installed DbDuo via the setup, the file "
                    + "should already be there. Otherwise, copy "
                    + "DbDuo.htm from the bundle into the program folder.",
                    "Help Contents",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void helpVerbsClicked(object oSender, EventArgs oArgs)
        {
            HelpDialog.show(this, "Get-Verb",
                "PowerShell-canonical verbs used in DbDuo:\n\n"
                + "  COMMON      New, Get, Set, Remove, Show, Copy, Find, Select, Format,\n"
                + "              Enter, Exit, Step, Open, Close, Lock, Add, Group, Reset\n"
                + "  DATA        Backup, Restore, Import, Export, Update, Save, Compare,\n"
                + "              ConvertTo, Initialize, Sync, Out\n"
                + "  DIAGNOSTIC  Test, Measure, Resolve, Trace, Repair\n"
                + "  LIFECYCLE   Invoke\n"
                + "\n"
                + "PowerShell discourages synonyms. DbDuo never uses:\n"
                + "  Delete (use Remove)        Create  (use New)         Read   (use Get)\n"
                + "  Modify (use Set)           Cancel  (use Stop)        Search (use Find)\n"
                + "\n"
                + "Full canonical list:\n"
                + "  https://learn.microsoft.com/en-us/powershell/scripting/developer/cmdlet/approved-verbs-for-windows-powershell-commands");
        }

        private void helpShowCommandClicked(object oSender, EventArgs oArgs)
        {
            using (CommandPickerDialog oDlg = new CommandPickerDialog())
            {
                if (oDlg.ShowDialog(this) == DialogResult.OK && oDlg.oChosenItem != null)
                {
                    if (oDlg.oChosenItem.Enabled) oDlg.oChosenItem.PerformClick();
                }
            }
        }

        // Show-Status / Where am I: speak the current database file,
        // table, row position, filter, and sort through the live
        // region. The user presses Control+F1 at any time and hears
        // a one-sentence summary -- this is the "Where am I?" key.
        //
        // Doubles as a live-region pipeline test: if Show-Status
        // doesn't speak, the live region is broken regardless of
        // other announcements; if it does, every other live-region
        // call in DbDuo will too.
        private void helpStatusClicked(object oSender, EventArgs oArgs)
        {
            string sMsg = buildStatusMessage();
            LiveRegion.say(sMsg);
            // Also dump to the StatusStrip label so the user can
            // tab-navigate to it (or use the screen reader's "say
            // status bar" hotkey -- JAWS Insert+PageDown) and have
            // the same text as a fallback path.
            try
            {
                if (lblStatus != null) lblStatus.Text = sMsg;
            }
            catch { }
        }

        // Build the one-sentence status string. Used by helpStatusClicked
        // and reused by other contexts that want the same summary.
        private string buildStatusMessage()
        {
            if (db == null || !db.isOpen())
                return "No database file open. Press Control+O to open one.";
            string sFile = db.filePath ?? "";
            string sBase = string.IsNullOrEmpty(sFile)
                ? "Database open"
                : "Database " + System.IO.Path.GetFileName(sFile);
            if (string.IsNullOrEmpty(db.currentTable))
                return sBase + ", no table selected. Press F4 to choose a table.";
            int iCount = db.recordCount;
            int iPos = db.absolutePosition;
            string sRow = (iCount > 0 && iPos > 0)
                ? "row " + iPos + " of " + iCount
                : "no rows";
            string sFilter = "";
            try { sFilter = db.filter ?? ""; } catch { }
            string sSort = "";
            try { sSort = db.sort ?? ""; } catch { }
            StringBuilder oSb = new StringBuilder();
            oSb.Append(sBase);
            oSb.Append(", table " + db.currentTable);
            if (db.currentIsView) oSb.Append(" (view)");
            else if (db.readOnly) oSb.Append(" (read-only)");
            oSb.Append(", " + sRow);
            if (!string.IsNullOrEmpty(sFilter)) oSb.Append(", filter: " + sFilter);
            if (!string.IsNullOrEmpty(sSort))   oSb.Append(", sort: "   + sSort);
            return oSb.ToString();
        }

        // Test-Reader: speak a probe message through say() so the user
        // hears it, then display the diagnostic in a dialog so the
        // user can read which detection path was taken. The two
        // pieces together let the user confirm what's working:
        // they hear the speech (or not), AND they can read which
        // path reported success.
        private void helpTestReaderClicked(object oSender, EventArgs oArgs)
        {
            LiveRegion.say("Speech path test. If you hear this, the speech pipeline is working.");
            string sDiag = LiveRegion.speechDiagnostic();
            MessageBox.Show(this, sDiag, "Test-Reader",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void helpTraceCommandClicked(object oSender, EventArgs oArgs)
        {
            KeyMap.bTraceMode = !KeyMap.bTraceMode;
            miHelpTraceCommand.Checked = KeyMap.bTraceMode;
            string sState = KeyMap.bTraceMode
                ? "ON: keystrokes are now described, not executed."
                : "OFF: keystrokes are now executed normally.";
            MessageBox.Show(this, "Trace-Command mode is " + sState,
                "Trace-Command", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void helpLogClicked(object oSender, EventArgs oArgs)
        {
            string sLogPath = DbDuoLog.getLogPath();
            if (string.IsNullOrEmpty(sLogPath))
            {
                MessageBox.Show(this,
                    "Logging is disabled (no writable location).",
                    "Show-Log", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string sExists = File.Exists(sLogPath) ? "(exists)" : "(not yet written)";
            string sMsg = "DbDuo log file:\n\n" + sLogPath + "\n\n" + sExists
                        + "\n\nThe log is truncated to empty at every program startup, so it always\n"
                        + "reflects only the current session. Significant events (database opens,\n"
                        + "table switches, errors) are recorded with timestamps.";
            DialogResult oRes = MessageBox.Show(this,
                sMsg + "\n\nOpen the log file in Notepad now?",
                "Show-Log", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (oRes == DialogResult.Yes && File.Exists(sLogPath))
            {
                try { System.Diagnostics.Process.Start("notepad.exe", "\"" + sLogPath + "\""); }
                catch (Exception oEx)
                {
                    MessageBox.Show(this, "Could not open Notepad: " + oEx.Message,
                        "Show-Log", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Open-WebSite: open the DbDuo project page in the system's
        // default browser. The URL is the same one referenced from
        // the About dialog and the License/README. No tracking;
        // straight Process.Start("https://...") which on Windows
        // delegates to the default HTTP handler.
        private void helpWebSiteClicked(object oSender, EventArgs oArgs)
        {
            const string sUrl = "https://github.com/JamalMazrui/DbDuo";
            try
            {
                System.Diagnostics.Process.Start(sUrl);
                LiveRegion.say("Opened DbDuo on GitHub");
                DbDuoLog.write("Open-WebSite: " + sUrl);
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this,
                    "Could not open the browser:\n\n" + oEx.Message
                    + "\n\nThe URL is:\n" + sUrl,
                    "Open-WebSite", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void helpAboutClicked(object oSender, EventArgs oArgs)
        {
            // The About dialog is the right place for the long-form
            // tagline because it is on-demand (Alt+F1), not part of
            // launch or routine navigation. Keep the rest of the UI
            // -- window title, status bar, column announcements,
            // menu tooltips -- terse.
            HelpDialog.show(this, "About DbDuo",
                "DbDuo " + BuildInfo.VersionString + "\n"
                + "\n"
                + "Manage databases in popular file formats, with synchronized\n"
                + "interfaces between CLI and GUI modes, designed to maximize\n"
                + "productivity for keyboard users of Windows.\n"
                + "\n"
                + "C# / .NET Framework 4.8 / x64 / WinForms.\n"
                + "Database access via ADODB COM interop.\n"
                + "Built around Microsoft's PowerShell verb taxonomy.\n"
                + "\n"
                + "https://github.com/JamalMazrui/DbDuo\n"
                + "MIT License.\n");
        }
    }

    // =====================================================================
    // DotPromptHost: dBase-tradition dot-prompt CLI in a side-by-side
    // Win32 console window.
    //
    // Synchronization: every command operates on DbDuoForm.Db, which
    // is the same DbDuoManager the GUI is rendering. After any state
    // change, we call form.invokeRefresh() to redraw the grid. The
    // recordset is the single source of truth.
    //
    // Console allocation: AllocConsole gives this winexe process its
    // own console window. We rebind Console.In and Console.Out so
    // .NET's standard Read/Write functions work. SetConsoleCtrlHandler
    // catches the X-button so closing the console doesn't kill the
    // GUI.
    // =====================================================================
    public static class DotPromptHost
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        private static extern bool SetWindowText(IntPtr hWnd, string sText);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int CTRL_C_EVENT = 0;
        private const int CTRL_BREAK_EVENT = 1;
        private const int CTRL_CLOSE_EVENT = 2;
        private delegate bool ConsoleCtrlDelegate(int iCtrlType);
        private static ConsoleCtrlDelegate oCtrlHandler;
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate oHandler, bool bAdd);

        private static bool bAllocated = false;
        private static bool bShouldExit = false;
        private static System.Threading.Thread oInputThread;
        private static DbDuoForm oForm;
        // When running in CLI-only mode (Program.UiMode.Cli), there is
        // no DbDuoForm. The host owns the DbDuoManager directly via
        // oManager. The 'db' accessor below returns the form's manager
        // when a form exists, otherwise this one.
        private static DbDuoManager oManager;
        private static string sLastFindCriteria = "";

        // ====================================================================
        // runStandalone: CLI-only mode. The caller (Program.runCliOnly) has
        // already attached or allocated the console and rebound .NET's
        // Console handles. We run the dot-prompt loop on the calling
        // thread (the main thread, since there is no GUI message loop)
        // until the user enters Quit.
        //
        // The recordset state lives in oMgr; there is no form to refresh.
        // ====================================================================
        public static void runStandalone(DbDuoManager oMgr)
        {
            oManager = oMgr;
            oForm = null;
            bAllocated = false;     // we don't own the console; caller does
            bShouldExit = false;
            inputLoop();
        }

        // ====================================================================
        // enter: GUI-mode entry to the dot prompt. Called either at program
        // startup when uiMode == Both (auto-spawn) or interactively from
        // the Tools > Enter-Console menu item.
        //
        // AllocConsole creates a fresh console window for this process and
        // SetConsoleCtrlHandler intercepts the X-button close so closing
        // the console does not terminate the GUI. The dot-prompt loop
        // runs on a background thread; the GUI keeps its own message loop.
        // ====================================================================

        public static void enter(DbDuoForm oOwner)
        {
            oForm = oOwner;
            if (bAllocated)
            {
                IntPtr hWnd = GetConsoleWindow();
                if (hWnd != IntPtr.Zero) SetForegroundWindow(hWnd);
                return;
            }

            if (!AllocConsole())
            {
                MessageBox.Show(oOwner, "Could not allocate console window.",
                    "Enter-Console", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            bAllocated = true;
            bShouldExit = false;

            oCtrlHandler = delegate(int iCtrlType) { bShouldExit = true; return true; };
            SetConsoleCtrlHandler(oCtrlHandler, true);

            try
            {
                System.IO.Stream oOut = Console.OpenStandardOutput();
                System.IO.StreamWriter oWriter = new System.IO.StreamWriter(oOut) { AutoFlush = true };
                Console.SetOut(oWriter);
                System.IO.Stream oIn = Console.OpenStandardInput();
                System.IO.StreamReader oReader = new System.IO.StreamReader(oIn);
                Console.SetIn(oReader);
            }
            catch { }

            IntPtr hWnd2 = GetConsoleWindow();
            if (hWnd2 != IntPtr.Zero)
            {
                SetWindowText(hWnd2, "DbDuo - Dot Prompt");
                SetForegroundWindow(hWnd2);
            }

            oInputThread = new System.Threading.Thread(inputLoop);
            oInputThread.IsBackground = true;
            oInputThread.Start();
        }

        private static void inputLoop()
        {
            try
            {
                printBanner();
                while (!bShouldExit)
                {
                    Console.Write(". ");
                    string sLine;
                    try { sLine = Console.ReadLine(); } catch { break; }
                    if (sLine == null) break;
                    sLine = sLine.Trim();
                    if (sLine.Length == 0) { printRowSummary(); continue; }
                    try { dispatch(sLine); }
                    catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
                }
            }
            finally
            {
                Console.WriteLine();
                if (bAllocated)
                {
                    // We allocated this console; free it. The GUI form keeps running.
                    Console.WriteLine("Exiting Dot Prompt; closing console window.");
                    try { FreeConsole(); } catch { }
                    bAllocated = false;
                }
                else
                {
                    // CLI-only mode: caller (Program.runCliOnly) owns the console.
                    Console.WriteLine("Exiting Dot Prompt.");
                }
            }
        }

        // The dot-prompt's startup banner. Kept brief (one line plus a
        // hint) so screen readers don't drone before the user can
        // begin typing. Detailed help is one keystroke away ("help").
        private static void printBanner()
        {
            Console.WriteLine("DbDuo dot prompt. Type 'help' for commands, 'help <name>' for details.");
            Console.WriteLine();
            printRowSummary();
        }

        // Print "[table] row N of M  filter: ...  sort: ..." for the
        // current recordset. Uses the db accessor so it works in both
        // GUI/Both mode (db comes from the form) and CLI-only mode
        // (db comes from oManager).
        private static void printRowSummary()
        {
            DbDuoManager oDb = db;
            if (oDb == null) { Console.WriteLine("(no manager)"); return; }
            if (!oDb.isOpen()) { Console.WriteLine("(no database open)"); return; }
            string sTable = oDb.currentTable;
            if (string.IsNullOrEmpty(sTable)) { Console.WriteLine("(no table selected)"); return; }
            string sFilter = string.IsNullOrEmpty(oDb.filter) ? "" : "  filter: " + oDb.filter;
            string sSort = string.IsNullOrEmpty(oDb.sort) ? "" : "  sort: " + oDb.sort;
            Console.WriteLine(string.Format("[{0}] row {1} of {2}{3}{4}",
                sTable, oDb.absolutePosition, oDb.recordCount, sFilter, sSort));
        }

        // Parse and dispatch one command line.
        private static void dispatch(string sLine)
        {
            // Auto-space single-char shortcuts: =foo -> = foo
            string sFirst = sLine.Substring(0, 1);
            if ("+-=?*;#&^@".Contains(sFirst) && sLine.Length > 1 && sLine[1] != ' ')
                sLine = sFirst + " " + sLine.Substring(1);

            string[] aTokens = sLine.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            string sVerb = aTokens[0].ToLowerInvariant();
            string sRest = aTokens.Length > 1 ? aTokens[1].Trim() : "";
            sVerb = resolveAlias(sVerb);

            switch (sVerb)
            {
                case "step-record":      cmdStepRecord(sRest);     break;
                case "set-position":     cmdSetPosition(sRest);    break;
                case "show-object":      cmdShowObject(sRest);     break;
                case "show-table":       cmdShowTable(sRest);      break;
                case "show-schema":      cmdShowSchema(sRest);     break;
                case "show-status":      printRowSummary();        break;
                case "get-field":        cmdGetField(sRest);       break;
                case "set-field":        cmdSetField(sRest);       break;
                case "jump-record":      cmdFindRecord(sRest);     break;
                case "jump-recordagain": cmdFindAgain();           break;
                case "jump-recordprevious": cmdFindPrevious();    break;
                case "select-record":    cmdSelectRecord(sRest);   break;
                case "select-column":    cmdSelectColumn(sRest);   break;
                case "reset-filter":     cmdResetFilter();         break;
                case "sort-object":      cmdSortObject(sRest);     break;
                case "reset-sort":       cmdResetSort();           break;
                case "new-record":       cmdNewRecord();           break;
                case "set-record":       cmdSetRecord();           break;
                case "remove-record":    cmdRemoveRecord();        break;
                case "copy-record":      cmdCopyRecord();          break;
                case "select-table":     cmdSelectTable(sRest);    break;
                case "get-table":        cmdGetTable();            break;
                case "get-property":     cmdGetProperty();         break;
                case "set-mark":         cmdSetMark(true);         break;
                case "clear-mark":       cmdSetMark(false);        break;
                case "show-related":     Console.WriteLine("(Show-Related is GUI-only in this build)"); break;
                case "enter-child":      cmdEnterChild();          break;
                case "exit-child":       cmdExitChild();           break;
                case "update-field":     Console.WriteLine("(Update-Field is GUI-only in this build)"); break;
                case "measure-table":    cmdMeasureTable();        break;
                case "measure-field":    cmdMeasureField(sRest);   break;
                case "measure-longest":  cmdMeasureLength(sRest, true);  break;
                case "measure-shortest": cmdMeasureLength(sRest, false); break;
                case "measure-maximum":  cmdMeasureExtreme(sRest, true); break;
                case "measure-minimum":  cmdMeasureExtreme(sRest, false); break;
                case "get-fieldname":    cmdGetFieldNames();       break;
                case "test-database":    cmdTestDatabase();        break;
                case "test-driver":      cmdTestDriver();          break;
                case "update-view":      cmdUpdateView();          break;
                case "save-databaseas":  cmdSaveAs(sRest);         break;
                case "backup-database":  cmdSaveAs(sRest);         break;
                case "export-data":      cmdExportData(sRest);     break;
                case "import-data":      cmdImportData(sRest);     break;
                case "open-database":    cmdOpenDatabase(sRest);   break;
                case "close-database":   cmdCloseDatabase();       break;
                case "invoke-sql":       cmdInvokeSql(sRest);      break;
                case "save-bookmark":    cmdSaveBookmark();        break;
                case "restore-bookmark": cmdRestoreBookmark();     break;
                case "clear-bookmark":   cmdClearBookmark();       break;
                case "get-help":         cmdGetHelp(sRest);         break;
                case "get-verb":         cmdGetVerb();             break;
                case "trace-command":    cmdTraceCommand(sRest);   break;
                case "sync-session":     printRowSummary();        break;
                case "out-file":         cmdOutFile(sRest);        break;
                case "invoke-script":    cmdInvokeScript(sRest);   break;
                case "exit-console":     bShouldExit = true;       break;
                case "exit-application": cmdExitApplication();      break;
                case "switch-focus":     cmdSwitchFocus();          break;
                default:
                    Console.WriteLine("Unknown command: " + sVerb);
                    Console.WriteLine("Type 'help' for the command list.");
                    break;
            }
        }

        // Switch-Focus: bring the GUI form (if running in Both mode)
        // to the foreground. The dot prompt's console window remains
        // open; Alt+Tab returns to it. This is the dot-prompt
        // counterpart of Enter-Console (Control+\\) in the GUI.
        //
        // Internally calls SetForegroundWindow on the form's HWND
        // (declared earlier in this class). In CLI-only mode there is
        // no GUI; the command prints a notice and returns.
        private static void cmdSwitchFocus()
        {
            if (oForm == null || oForm.IsDisposed)
            {
                Console.WriteLine("Switch-Focus: no GUI form is available (CLI-only mode).");
                return;
            }
            try
            {
                IntPtr hWnd = oForm.Handle;
                if (hWnd != IntPtr.Zero) SetForegroundWindow(hWnd);
            }
            catch (Exception oEx)
            {
                Console.WriteLine("Switch-Focus failed: " + oEx.Message);
            }
        }

        // Enter-Child / Exit-Child from the dot prompt. The actual
        // drill-down logic lives on the form (because it needs the
        // ListView, the listbox dialog for picking among multiple
        // child tables, and access to the per-form drill stack); the
        // console marshals to the form's GUI thread via the public
        // invokeEnterChild / invokeExitChild entry points.
        //
        // In CLI-only mode there is no form, so these commands print
        // a notice and return without doing anything. (A CLI-only
        // implementation is possible but adds another GUI dialog
        // path to maintain; defer until there's demand.)
        private static void cmdEnterChild()
        {
            if (oForm == null || oForm.IsDisposed)
            {
                Console.WriteLine("Enter-Child: not available in CLI-only mode.");
                return;
            }
            int iDepth = oForm.invokeEnterChild();
            if (iDepth > 0)
                Console.WriteLine("Drilled into child table. Stack depth: " + iDepth + ".");
            printRowSummary();
        }

        private static void cmdExitChild()
        {
            if (oForm == null || oForm.IsDisposed)
            {
                Console.WriteLine("Exit-Child: not available in CLI-only mode.");
                return;
            }
            if (!oForm.hasDrillStack())
            {
                Console.WriteLine("Exit-Child: drill stack is empty. Use Enter-Child first.");
                return;
            }
            int iDepth = oForm.invokeExitChild();
            Console.WriteLine("Returned to parent table. Stack depth: " + iDepth + ".");
            printRowSummary();
        }

        // Exit-Application: shut down the entire DbDuo process,
        // not just the dot prompt. Aliased to 'quit' / 'q' at the
        // dot prompt. The 'exit' / 'x' / 'bye' aliases still mean
        // "close the dot prompt and return to the GUI" via
        // exit-console.
        //
        // In GUI/Both mode the form's Close() runs on the GUI
        // thread (we marshal via Invoke); closing the form
        // terminates the message loop and the process. In CLI-only
        // mode there is no form, so we set bShouldExit and call
        // Application.Exit() to break the Program.Main message
        // pump if one is running.
        private static void cmdExitApplication()
        {
            Console.WriteLine("Exiting DbDuo.");
            bShouldExit = true;  // break the dot-prompt input loop
            try
            {
                if (oForm != null && !oForm.IsDisposed)
                {
                    // Close the form on its own thread.
                    if (oForm.InvokeRequired)
                        oForm.BeginInvoke(new Action(() => { try { oForm.Close(); } catch { } }));
                    else
                        oForm.Close();
                }
                else
                {
                    // CLI-only mode.
                    try { Application.Exit(); } catch { }
                }
            }
            catch (Exception oEx)
            {
                Console.WriteLine("Exit-Application failed: " + oEx.Message);
            }
        }

        // Out-File: redirect future console output to a file, or
        // restore to the screen. Inspired by SQLite's '.output FILE'
        // / '.output stdout', psql's '\o file', and PowerShell's
        // Out-File cmdlet.
        //
        //   Out-File path.txt        Send all subsequent output to path.txt
        //   Out-File -a path.txt     Append rather than overwrite
        //   Out-File stdout          Restore output to the screen (the bare word "stdout")
        //   Out-File                 Show the current redirection target
        //
        // Implementation: when a file is set, Console.SetOut to a
        // TextWriter that tees to both the file and the original
        // stdout. The user sees the same text they would see at
        // the console AND the file gets a copy -- this matches
        // PowerShell's Out-File behavior more naturally than the
        // SQLite/psql "silent file capture" mode, and helps the
        // screen-reader user follow what's happening.
        private static System.IO.TextWriter oOriginalStdout = null;
        private static System.IO.StreamWriter oOutFileWriter = null;
        private static string sOutFilePath = "";

        private static void cmdOutFile(string sArg)
        {
            string sExpr = sArg.Trim();
            if (sExpr.Length == 0)
            {
                if (sOutFilePath.Length == 0)
                    Console.WriteLine("Out-File: not currently redirected.");
                else
                    Console.WriteLine("Out-File: " + sOutFilePath);
                Console.WriteLine("Usage: Out-File path  |  Out-File -a path  |  Out-File stdout");
                return;
            }
            if (string.Equals(sExpr, "stdout", StringComparison.OrdinalIgnoreCase))
            {
                closeOutFile();
                Console.WriteLine("Out-File: restored to stdout.");
                return;
            }
            bool bAppend = false;
            if (sExpr.StartsWith("-a ", StringComparison.OrdinalIgnoreCase)
                || sExpr.StartsWith("-append ", StringComparison.OrdinalIgnoreCase))
            {
                bAppend = true;
                int iSpace = sExpr.IndexOf(' ');
                sExpr = sExpr.Substring(iSpace + 1).Trim();
            }
            if (sExpr.Length == 0) { Console.WriteLine("Out-File: missing path."); return; }

            try
            {
                closeOutFile();
                if (oOriginalStdout == null) oOriginalStdout = Console.Out;
                oOutFileWriter = new System.IO.StreamWriter(sExpr, bAppend, System.Text.Encoding.UTF8);
                oOutFileWriter.AutoFlush = true;
                sOutFilePath = sExpr;
                // Tee writer: every write goes to both the file and the
                // original screen output, so the user sees what they're
                // capturing as they capture it.
                System.IO.TextWriter oTee = new TeeWriter(oOriginalStdout, oOutFileWriter);
                Console.SetOut(oTee);
                Console.WriteLine("Out-File: " + (bAppend ? "appending to " : "writing to ") + sExpr);
            }
            catch (Exception oEx)
            {
                Console.WriteLine("Out-File failed: " + oEx.Message);
                closeOutFile();
            }
        }

        private static void closeOutFile()
        {
            try
            {
                if (oOriginalStdout != null) Console.SetOut(oOriginalStdout);
            }
            catch { }
            try { if (oOutFileWriter != null) oOutFileWriter.Dispose(); } catch { }
            oOutFileWriter = null;
            sOutFilePath = "";
        }

        // TextWriter that forwards Write/WriteLine to two underlying
        // writers. Used by Out-File for screen-and-file tee output.
        private class TeeWriter : System.IO.TextWriter
        {
            private System.IO.TextWriter oA;
            private System.IO.TextWriter oB;
            public TeeWriter(System.IO.TextWriter a, System.IO.TextWriter b) { oA = a; oB = b; }
            public override System.Text.Encoding Encoding { get { return oA.Encoding; } }
            public override void Write(char ch) { try { oA.Write(ch); } catch { } try { oB.Write(ch); } catch { } }
            public override void Write(string s) { try { oA.Write(s); } catch { } try { oB.Write(s); } catch { } }
            public override void WriteLine(string s) { try { oA.WriteLine(s); } catch { } try { oB.WriteLine(s); } catch { } }
            public override void Flush() { try { oA.Flush(); } catch { } try { oB.Flush(); } catch { } }
        }

        // Invoke-Script path.txt: read commands from a file and
        // execute each non-blank, non-comment line as if typed at
        // the dot prompt. SQLite calls this '.read FILE', psql
        // calls it '\i FILE'. PowerShell-canonical name uses the
        // Lifecycle 'Invoke' verb plus the 'Script' noun.
        //
        // Lines starting with '#' or ';' are treated as comments.
        // The script can call any dot-prompt command, including
        // SQL via Invoke-Sql. An error in one line is reported
        // but does not stop the script (matches psql default).
        private static void cmdInvokeScript(string sArg)
        {
            string sPath = sArg.Trim();
            if (sPath.Length == 0)
            {
                Console.WriteLine("Invoke-Script path     run commands from path");
                Console.WriteLine("Aliases: read, script, i");
                return;
            }
            if (!System.IO.File.Exists(sPath))
            {
                Console.WriteLine("Invoke-Script: file not found: " + sPath);
                return;
            }
            string[] aLines;
            try { aLines = System.IO.File.ReadAllLines(sPath); }
            catch (Exception oEx) { Console.WriteLine("Invoke-Script: read failed: " + oEx.Message); return; }

            int iLine = 0;
            int iRun = 0;
            int iErr = 0;
            foreach (string sRaw in aLines)
            {
                iLine++;
                string sCmd = sRaw.Trim();
                if (sCmd.Length == 0) continue;
                if (sCmd.StartsWith("#") || sCmd.StartsWith(";")) continue;
                Console.WriteLine("[line " + iLine + "] " + sCmd);
                try { dispatch(sCmd); iRun++; }
                catch (Exception oEx)
                {
                    Console.WriteLine("  error at line " + iLine + ": " + oEx.Message);
                    iErr++;
                }
            }
            Console.WriteLine(string.Format("Invoke-Script done: {0} command(s), {1} error(s).", iRun, iErr));
        }

        private static string resolveAlias(string sVerb)
        {
            switch (sVerb)
            {
                case "n": case "+": case "next":     return "step-record-next";
                case "p": case "-": case "prev": case "previous": return "step-record-prev";
                case "t": case "top": case "first":  return "step-record-first";
                case "b": case "bot": case "bottom": case "last": return "step-record-last";
                case "skip":                         return "step-record"; // dBASE: SKIP n
                case "g": case "#": case "goto":     return "set-position";
                case "=": case "display": case "show": case "disp": return "show-object";
                case "l": case "list":               return "show-table";
                case "schema":                       return "show-schema";
                case "status": case "?":             return "show-status";
                case "a": case "&": case "add": case "append": return "new-record";
                case "e": case "^": case "edit": case "modify": return "set-record";
                case "del": case "delete": case "d": return "remove-record";
                case "copy":                         return "copy-record";
                case "f": case "find": case "j": case "jump":
                    return "jump-record";
                case "find-record":
                    return "jump-record";
                case "locate":                       return "jump-record"; // dBASE LOCATE FOR <cond>
                case "find-next":
                case "jump-next":
                case "find-recordagain":
                case "continue":                                 // dBASE CONTINUE
                    return "jump-recordagain";
                case "find-prev": case "find-previous":
                case "jump-prev": case "jump-previous":
                case "find-recordprevious":
                    return "jump-recordprevious";
                case "filter": case "where":         return "select-record";
                case "select-object": case "cols": case "columns":
                                                     return "select-column";
                case "set-fields":                   return "select-column"; // dBASE SET FIELDS TO
                case "all": case "clear-filter":     return "reset-filter";
                case "sort": case "order":           return "sort-object";
                case "use": case "@":                return "select-table";
                case "select-view":                  return "select-table";
                case "tables":                       return "get-table";
                case "props": case "properties":     return "get-property";
                case "mark":                         return "set-mark";
                case "unmark":                       return "clear-mark";
                case "related":                      return "show-related";
                // Enter-Child / Exit-Child: parent-to-child drill-down
                // with a stack. Aliased so users who think of this as
                // "zoom in / zoom out" or "drill / undrill" get the
                // same behavior. The bare 'zoom' aliases the forward
                // drill-down; 'unzoom' / 'back' aliases the reverse.
                case "zoom": case "drill": case "enter":
                    return "enter-child";
                case "unzoom": case "back": case "undrill":
                    return "exit-child";
                case "replace": case "repl":         return "update-field"; // dBASE REPLACE
                case "sql": case ";": case "*":      return "invoke-sql";
                case "count": case "y": case "yield": return "measure-table";
                case "longest":                      return "measure-longest";
                case "shortest":                     return "measure-shortest";
                case "max": case "maximum":          return "measure-maximum";
                case "min": case "minimum":          return "measure-minimum";
                case "sum":                          return "measure-field"; // dBASE SUM
                case "average": case "avg":          return "measure-field"; // dBASE AVERAGE
                case "calculate": case "calc":       return "measure-field"; // dBASE CALCULATE
                case "fields":                       return "get-fieldname";
                case "test":                         return "test-database";
                case "drivers":                      return "test-driver";
                case "refresh": case "requery":      return "update-view";
                case "backup":                       return "backup-database";
                case "save-as": case "saveas":       return "save-databaseas";
                case "export": case "ex":            return "export-data";
                case "open":                         return "open-database";
                case "close":                        return "close-database";
                case "bookmark":                     return "save-bookmark";
                case "goto-bookmark":                return "restore-bookmark";
                case "help":                         return "get-help";
                case "verbs":                        return "get-verb";
                case "trace":                        return "trace-command";
                case "sync":                         return "sync-session";
                // Output redirection (SQLite .output / psql \o / PowerShell Out-File).
                case "output": case "tee": case "o": return "out-file";
                // Run commands from a file (SQLite .read / psql \i).
                case "read": case "script": case "i": return "invoke-script";
                case "x": case "exit": case "bye": return "exit-console";
                case "q": case "quit":             return "exit-application";
                case "gui": case "focus": case "window":                   return "switch-focus";
                // 'seek' in dbDot is an indexed-recordset row jump.
                // ADODB recordsets opened via adCmdTable don't
                // expose Index/Seek through the OleDb cursor; the
                // user-facing equivalent is Jump-Record with a key
                // expression like "app_id = 5".
                case "seek":                         return "jump-record";
            }
            // Compound aliases for Step-Record:
            switch (sVerb)
            {
                case "step-record-next":  return "step-record";
                case "step-record-prev":  return "step-record";
                case "step-record-first": return "step-record";
                case "step-record-last":  return "step-record";
            }
            return sVerb;
        }

        // ============================ command handlers ============================

        // Manager accessor: use the form's manager if a form exists,
        // otherwise the directly-owned manager (CLI-only mode).
        private static DbDuoManager db
        {
            get
            {
                if (oForm != null) return oForm.Db;
                return oManager;
            }
        }

        private static bool requireOpen()
        {
            if (db == null || !db.isOpen()) { Console.WriteLine("(no database open)"); return false; }
            return true;
        }

        private static bool requireRecordset()
        {
            if (!requireOpen()) return false;
            if (!db.hasRecordset()) { Console.WriteLine("(no table selected)"); return false; }
            return true;
        }

        // refresh: redraw the GUI grid after any state change. In CLI-only
        // mode there is no grid to redraw, so this is a no-op.
        private static void refresh()
        {
            if (oForm != null) oForm.invokeRefresh();
        }

        private static void cmdStepRecord(string sArg)
        {
            if (!requireRecordset()) return;
            // Accept any of:
            //   <empty>                  step forward 1
            //   next [N]                 step forward 1 (or N)
            //   previous|prev [N]        step back 1 (or N)
            //   first|top                go to first row
            //   last|bottom              go to last row
            //   <integer>                bare integer: step forward
            //                            that many rows (negative
            //                            steps backward). Matches
            //                            dBASE's SKIP semantic.
            string sTrim = sArg.Trim();
            string sLower = sTrim.ToLowerInvariant();

            // Bare integer form: "+5", "-3", "10", etc.
            int iSkip;
            if (sTrim.Length > 0 && int.TryParse(sTrim, out iSkip))
            {
                skipBy(iSkip);
                refresh();
                printRowSummary();
                return;
            }

            // Tokenized forms.
            string[] aT = sTrim.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            string sVerb = aT.Length > 0 ? aT[0].ToLowerInvariant() : "";
            string sCount = aT.Length > 1 ? aT[1].Trim() : "";
            int iN = 1;
            if (sCount.Length > 0 && !int.TryParse(sCount, out iN))
            {
                Console.WriteLine("Step-Record: count must be an integer.");
                return;
            }
            if (iN < 1) iN = 1;

            switch (sVerb)
            {
                case "":
                case "next":
                    skipBy(iN);
                    break;
                case "previous":
                case "prev":
                    skipBy(-iN);
                    break;
                case "first":
                case "top":
                    db.moveFirst();
                    break;
                case "last":
                case "bottom":
                    db.moveLast();
                    break;
                default:
                    Console.WriteLine("Step-Record argument: next [N] | previous [N] | first | last | <integer>");
                    return;
            }
            refresh();
            printRowSummary();
        }

        // Move the cursor by N rows, positive = forward, negative
        // = backward. Bounded by BOF / EOF. Pattern lifted from
        // dBASE's SKIP n. The ADO recordset has no SKIP, so we
        // loop MoveNext / MovePrevious; the client cursor makes
        // this O(N) in memory, not in disk seeks.
        private static void skipBy(int iN)
        {
            if (iN == 0) return;
            if (iN > 0)
            {
                for (int i = 0; i < iN && !db.eof; i++) db.moveNext();
            }
            else
            {
                for (int i = 0; i < -iN && !db.bof; i++) db.movePrevious();
            }
        }

        private static void cmdSetPosition(string sArg)
        {
            if (!requireRecordset()) return;
            int i;
            if (!int.TryParse(sArg.Trim(), out i)) { Console.WriteLine("Set-Position requires a number."); return; }
            db.absolutePosition = i;
            refresh();
            printRowSummary();
        }

        // Show-Object: print the current row's fields, one per line,
        // in the form "field-name = value". After the fields, list
        // related records grouped by table -- parent rows reached
        // via outbound foreign keys, then child rows that reference
        // this one. The GUI equivalent opens the same content in a
        // read-only dialog (bound to Enter).
        //
        // Argument grammar:
        //   Show-Object              all display fields + related records
        //   Show-Object col1,col2    only the named columns; no related
        //   Show-Object all          every field of the recordset; no related
        //
        // The named-columns and 'all' modes are intended for
        // scripting where the user wants a precise field set, so
        // they skip the related-records section.
        private static void cmdShowObject(string sArg)
        {
            if (!requireRecordset()) return;
            if (db.recordCount == 0) { Console.WriteLine("(no records)"); return; }
            List<string> lAll = db.getFieldNames();
            string sArgTrim = sArg.Trim();
            bool bShowRelated = (sArgTrim.Length == 0);

            List<string> lWanted = new List<string>();
            if (sArgTrim.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                lWanted = lAll;
            }
            else if (sArgTrim.Length > 0)
            {
                foreach (string s in sArgTrim.Split(','))
                {
                    string sT = s.Trim();
                    if (lAll.Contains(sT)) lWanted.Add(sT);
                }
            }
            if (lWanted.Count == 0)
            {
                lWanted = db.getDisplayFieldNames();
                if (lWanted.Count == 0) lWanted = lAll;
            }

            foreach (string sCol in lWanted)
                Console.WriteLine(string.Format("  {0} = {1}", sCol, db.getFieldValue(sCol)));

            if (bShowRelated)
                cmdShowObjectRelated();
        }

        // Print the related-records section of Show-Object to the
        // CLI. Mirrors the GUI's appendRelatedRecords logic but
        // writes directly to Console.
        private static void cmdShowObjectRelated()
        {
            const int iMaxPerSection = 25;
            const string sNoLook = "(no look column on this table)";

            string sCurrentTable = db.currentTable ?? "";
            if (string.IsNullOrEmpty(sCurrentTable)) return;
            List<string> lCurrentCols = db.getFieldNames();
            if (lCurrentCols.Count == 0) return;

            string sCurrentPk = db.actualPrimaryKey(sCurrentTable);
            if (string.IsNullOrEmpty(sCurrentPk))
                sCurrentPk = computePrimaryKeyColumnStatic(sCurrentTable, lCurrentCols);
            string sCurrentPkValue = "";
            if (!string.IsNullOrEmpty(sCurrentPk))
            {
                try { sCurrentPkValue = db.getFieldValue(sCurrentPk) ?? ""; }
                catch { sCurrentPkValue = ""; }
            }
            List<string> lAllTables = db.getTableNames();

            // Outbound FK -> parent.
            bool bAnyEmitted = false;
            foreach (string sCol in lCurrentCols)
            {
                if (string.Equals(sCol, sCurrentPk, StringComparison.OrdinalIgnoreCase)) continue;
                if (!sCol.EndsWith(Metadata.PrimaryKeySuffix, StringComparison.OrdinalIgnoreCase)) continue;

                string sFkValue = "";
                try { sFkValue = db.getFieldValue(sCol) ?? ""; }
                catch { sFkValue = ""; }
                if (string.IsNullOrEmpty(sFkValue)) continue;

                string sParentTable = findParentTableForFkStatic(sCol, lAllTables);
                if (string.IsNullOrEmpty(sParentTable)) continue;

                int iFound;
                List<string> lLook = db.queryColumnValues(
                    sParentTable, "look", sCol, sFkValue, 1, out iFound);

                if (!bAnyEmitted) { Console.WriteLine(); bAnyEmitted = true; }
                Console.WriteLine("Related " + sParentTable + ":");
                if (iFound == 0)
                    Console.WriteLine("  (parent not found)");
                else if (lLook.Count > 0 && !string.IsNullOrEmpty(lLook[0]))
                    Console.WriteLine("  " + lLook[0]);
                else
                    Console.WriteLine("  " + sNoLook);
            }

            // Inbound FK -> children.
            if (string.IsNullOrEmpty(sCurrentPk) || string.IsNullOrEmpty(sCurrentPkValue))
                return;

            foreach (string sChildTable in lAllTables)
            {
                if (string.Equals(sChildTable, sCurrentTable, StringComparison.OrdinalIgnoreCase)) continue;
                List<string> lChildCols = db.getColumnsOfTable(sChildTable);
                bool bHasFk = false;
                foreach (string sChildCol in lChildCols)
                {
                    if (string.Equals(sChildCol, sCurrentPk, StringComparison.OrdinalIgnoreCase))
                    { bHasFk = true; break; }
                }
                if (!bHasFk) continue;

                int iFound;
                List<string> lLook = db.queryColumnValues(
                    sChildTable, "look", sCurrentPk, sCurrentPkValue,
                    iMaxPerSection, out iFound);
                if (iFound == 0) continue;

                Console.WriteLine();
                Console.WriteLine("Related " + sChildTable + ":");
                if (lLook.Count == 0)
                {
                    Console.WriteLine("  (" + iFound + " row(s) -- " + sNoLook + ")");
                }
                else
                {
                    foreach (string sLook in lLook)
                        Console.WriteLine("  " + (string.IsNullOrEmpty(sLook) ? "(empty look)" : sLook));
                    if (iFound > lLook.Count)
                    {
                        int iMore = iFound - lLook.Count;
                        Console.WriteLine("  (... " + iMore + " more; use Enter-Child to see all)");
                    }
                }
            }
        }

        // Static counterparts to the form instance methods, used by
        // the CLI dispatcher. Behaviorally identical -- just relocated.
        private static string computePrimaryKeyColumnStatic(string sTable, List<string> lCols)
        {
            if (string.IsNullOrEmpty(sTable)) return null;
            string sLowerTable = sTable.ToLowerInvariant();
            string sSingular = (sLowerTable.EndsWith("s") && sLowerTable.Length > 1)
                ? sLowerTable.Substring(0, sLowerTable.Length - 1) : sLowerTable;
            string sPkPlural = sSingular + Metadata.PrimaryKeySuffix;
            foreach (string sCol in lCols)
                if (sCol.ToLowerInvariant() == sPkPlural) return sCol;
            string sPkBare = sLowerTable + Metadata.PrimaryKeySuffix;
            foreach (string sCol in lCols)
                if (sCol.ToLowerInvariant() == sPkBare) return sCol;
            foreach (string sCol in lCols)
                if (sCol.ToLowerInvariant() == "id") return sCol;
            return null;
        }

        private static string findParentTableForFkStatic(string sFkColumn, List<string> lAllTables)
        {
            if (string.IsNullOrEmpty(sFkColumn)) return null;
            foreach (string sT in lAllTables)
            {
                string sPk = db.actualPrimaryKey(sT);
                if (string.IsNullOrEmpty(sPk))
                {
                    List<string> lCols = db.getColumnsOfTable(sT);
                    sPk = computePrimaryKeyColumnStatic(sT, lCols);
                }
                if (!string.IsNullOrEmpty(sPk)
                    && string.Equals(sPk, sFkColumn, StringComparison.OrdinalIgnoreCase))
                    return sT;
            }
            return null;
        }

        // Show-Table: print rows from the current recordset.
        // Argument grammar mirrors dBASE's LIST [scope] [fields]:
        //
        //   Show-Table                         first 50 rows
        //   Show-Table all                     every row
        //   Show-Table next 5                  next 5 rows starting from current
        //   Show-Table for <expr>              rows matching an ADO Filter expression
        //                                      (filter is temporarily applied, then
        //                                      restored; the active filter is preserved)
        //   Show-Table fields:col1,col2        restrict columns shown
        //
        // The scope and fields clauses can be combined in any order:
        //   Show-Table next 10 fields:name,updated
        //   Show-Table for "marked = true" fields:name
        //
        // Uses display fields by default (the Select-Column override
        // and the standard-hidden rules still apply); fields:<list>
        // overrides that with an explicit set.
        private static void cmdShowTable(string sArg)
        {
            if (!requireRecordset()) return;
            int iTotal = db.recordCount;
            if (iTotal == 0) { Console.WriteLine("(no records)"); return; }

            // Parse the argument grammar.
            bool bAll = false;
            int iNext = 50;          // default scope: first 50
            bool bScopeIsNext = false;  // true when "next N" used
            string sForExpr = "";
            List<string> lExplicitFields = null;
            string sRest = sArg.Trim();

            // fields:col1,col2 extraction first
            int iFieldsAt = sRest.ToLowerInvariant().IndexOf("fields:");
            if (iFieldsAt >= 0)
            {
                int iEnd = sRest.IndexOf(' ', iFieldsAt + 7);
                if (iEnd < 0) iEnd = sRest.Length;
                string sFieldList = sRest.Substring(iFieldsAt + 7, iEnd - (iFieldsAt + 7));
                lExplicitFields = new List<string>();
                foreach (string s in sFieldList.Split(','))
                {
                    string sT = s.Trim();
                    if (sT.Length > 0) lExplicitFields.Add(sT);
                }
                sRest = (sRest.Substring(0, iFieldsAt) + " "
                       + (iEnd < sRest.Length ? sRest.Substring(iEnd) : "")).Trim();
            }

            // for <expr> extraction (everything after "for ")
            int iForAt = -1;
            string sLower = sRest.ToLowerInvariant();
            if (sLower.StartsWith("for ")) iForAt = 0;
            else
            {
                int i2 = sLower.IndexOf(" for ");
                if (i2 >= 0) iForAt = i2 + 1;
            }
            if (iForAt >= 0)
            {
                sForExpr = sRest.Substring(iForAt + 3).Trim();
                sRest = sRest.Substring(0, iForAt).Trim();
            }

            // Scope: "all", "next N", or empty (default 50).
            sLower = sRest.ToLowerInvariant();
            if (sLower == "all") bAll = true;
            else if (sLower.StartsWith("next "))
            {
                string sN = sRest.Substring(5).Trim();
                int iN;
                if (!int.TryParse(sN, out iN) || iN < 1)
                {
                    Console.WriteLine("Show-Table: 'next' requires a positive integer.");
                    return;
                }
                iNext = iN;
                bScopeIsNext = true;
            }
            else if (sLower.Length > 0)
            {
                // Unknown trailing tokens: try as a plain integer.
                int iN;
                if (int.TryParse(sLower, out iN) && iN > 0)
                {
                    iNext = iN;
                }
                else
                {
                    Console.WriteLine("Show-Table: didn't understand '" + sRest + "'. "
                        + "Use: Show-Table [all | next N | for <expr>] [fields:col1,col2]");
                    return;
                }
            }

            // Choose fields to display.
            List<string> lFields;
            if (lExplicitFields != null && lExplicitFields.Count > 0)
            {
                // Validate against the recordset; drop unknown names.
                List<string> lAll = db.getFieldNames();
                HashSet<string> hAll = new HashSet<string>(lAll, StringComparer.OrdinalIgnoreCase);
                lFields = new List<string>();
                foreach (string s in lExplicitFields)
                    if (hAll.Contains(s)) lFields.Add(s);
                if (lFields.Count == 0)
                {
                    Console.WriteLine("Show-Table: none of the requested fields exist in this table.");
                    return;
                }
            }
            else
            {
                lFields = db.getDisplayFieldNames();
            }

            // Apply temporary FOR filter on top of existing filter.
            string sSavedFilter = db.filter;
            object oSavedBookmark = db.bookmark;
            try
            {
                if (sForExpr.Length > 0)
                {
                    string sCombined = sSavedFilter.Length > 0
                        ? "(" + sSavedFilter + ") AND (" + sForExpr + ")"
                        : sForExpr;
                    try { db.filter = sCombined; }
                    catch (Exception oEx)
                    {
                        Console.WriteLine("Filter expression rejected: " + oEx.Message);
                        return;
                    }
                }

                int iCount = db.recordCount;
                if (iCount == 0) { Console.WriteLine("(no rows match)"); return; }

                int iStartPos = bScopeIsNext ? db.absolutePosition : 1;
                if (bScopeIsNext && iStartPos < 1) iStartPos = 1;
                int iLimit = bAll ? iCount : iNext;

                Console.WriteLine(string.Join(" | ", lFields));
                int iHeaderLen = string.Join(" | ", lFields).Length;
                Console.WriteLine(new string('-', Math.Min(80, iHeaderLen)));

                if (bScopeIsNext) db.absolutePosition = iStartPos;
                else db.moveFirst();

                int iRowsPrinted = 0;
                int iRowNum = bScopeIsNext ? iStartPos : 1;
                while (!db.eof && iRowsPrinted < iLimit)
                {
                    List<string> lV = new List<string>();
                    foreach (string sN in lFields) lV.Add(db.getFieldValue(sN));
                    Console.WriteLine(string.Format("{0,3}. {1}", iRowNum, string.Join(" | ", lV)));
                    db.moveNext();
                    iRowsPrinted++;
                    iRowNum++;
                }
                int iRemaining = iCount - (bScopeIsNext ? iStartPos - 1 + iRowsPrinted : iRowsPrinted);
                if (iRemaining > 0 && !bAll)
                    Console.WriteLine(string.Format("... {0} more rows. Use 'Show-Table all' to see all.", iRemaining));
            }
            finally
            {
                if (sForExpr.Length > 0)
                {
                    try { db.filter = sSavedFilter; } catch { }
                }
                if (oSavedBookmark != null) try { db.bookmark = oSavedBookmark; } catch { }
                refresh();
            }
        }

        private static void cmdShowSchema(string sArg)
        {
            if (!requireOpen()) return;
            if (sArg.Length > 0)
            {
                List<string> lAll = db.getTableAndViewNames();
                if (!lAll.Contains(sArg)) { Console.WriteLine("(no such table or view)"); return; }
                bool bIsView = db.isViewName(sArg);
                Console.WriteLine("Schema for " + (bIsView ? "view" : "table") + ": " + sArg);
                // Temporarily switch to that table to read its schema.
                string sSavedTable = db.currentTable;
                try { db.selectTable(sArg); foreach (string sN in db.getFieldNames()) Console.WriteLine("  " + sN); }
                finally { if (!string.IsNullOrEmpty(sSavedTable) && sSavedTable != sArg) try { db.selectTable(sSavedTable); } catch { } }
            }
            else
            {
                List<string> lTables = db.getTableNames();
                List<string> lViews = db.getViewNames();
                if (lTables.Count > 0)
                {
                    Console.WriteLine("Tables (" + lTables.Count + "):");
                    foreach (string sN in lTables) Console.WriteLine("  " + sN);
                }
                if (lViews.Count > 0)
                {
                    if (lTables.Count > 0) Console.WriteLine();
                    Console.WriteLine("Views (" + lViews.Count + "):");
                    foreach (string sN in lViews) Console.WriteLine("  " + sN);
                }
            }
        }

        private static void cmdGetField(string sArg)
        {
            if (!requireRecordset()) return;
            string sCol = sArg.Trim();
            if (sCol.Length == 0) { Console.WriteLine("Get-Field requires a column name."); return; }
            if (!db.getFieldNames().Contains(sCol)) { Console.WriteLine("No such column: " + sCol); return; }
            Console.WriteLine(sCol + " = " + db.getFieldValue(sCol));
        }

        private static void cmdSetField(string sArg)
        {
            if (!requireRecordset()) return;
            if (db.readOnly) { Console.WriteLine("Database is read-only."); return; }
            string sCol, sValue;
            int iSplit = sArg.IndexOf('=');
            if (iSplit < 0) iSplit = sArg.IndexOf(' ');
            if (iSplit <= 0) { Console.WriteLine("Set-Field requires '<column> <value>' or '<column>=<value>'."); return; }
            sCol = sArg.Substring(0, iSplit).Trim();
            sValue = sArg.Substring(iSplit + 1).Trim();
            if (sValue.Length >= 2 && sValue.StartsWith("\"") && sValue.EndsWith("\""))
                sValue = sValue.Substring(1, sValue.Length - 2);
            try
            {
                db.setFieldValue(sCol, sValue);
                db.update();
                refresh();
                Console.WriteLine("Saved.");
            }
            catch (Exception oEx)
            {
                try { db.cancelUpdate(); } catch { }
                Console.WriteLine("Error: " + oEx.Message);
            }
        }

        private static void cmdFindRecord(string sArg)
        {
            if (!requireRecordset()) return;
            string sCriteria = sArg.Trim();
            if (sCriteria.Length == 0) { Console.WriteLine("Find-Record requires a criteria expression."); return; }
            sLastFindCriteria = sCriteria;
            try
            {
                bool bFound = db.findRecord(sCriteria, true, false);
                if (!bFound) Console.WriteLine("Not found.");
                refresh();
                printRowSummary();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        private static void cmdFindAgain()
        {
            if (!requireRecordset()) return;
            if (string.IsNullOrEmpty(sLastFindCriteria)) { Console.WriteLine("No previous Find-Record."); return; }
            try
            {
                bool bFound = db.findRecord(sLastFindCriteria, true, true);
                if (!bFound) Console.WriteLine("No more matches.");
                refresh();
                printRowSummary();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        // Find-RecordPrevious: search backward from current row using
        // the last entered Find criteria.
        private static void cmdFindPrevious()
        {
            if (!requireRecordset()) return;
            if (string.IsNullOrEmpty(sLastFindCriteria)) { Console.WriteLine("No previous Find-Record."); return; }
            try
            {
                bool bFound = db.findRecord(sLastFindCriteria, false, true);
                if (!bFound) Console.WriteLine("No earlier matches.");
                refresh();
                printRowSummary();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        private static void cmdSelectRecord(string sArg)
        {
            if (!requireRecordset()) return;
            string sExpr = sArg.Trim();
            // dbDot shortcuts: "filter marked" -> marked = true,
            // "filter unmarked" -> marked = false. The user types
            // the noun, we expand to the ADO Filter expression.
            // Case-insensitive; case "marked" maps to true,
            // "unmarked" to false.
            if (string.Equals(sExpr, "marked", StringComparison.OrdinalIgnoreCase))
                sExpr = "marked = true";
            else if (string.Equals(sExpr, "unmarked", StringComparison.OrdinalIgnoreCase))
                sExpr = "marked = false";
            try
            {
                db.filter = sExpr;
                refresh();
                Console.WriteLine("Filter: " + (db.filter.Length > 0 ? db.filter : "(none)"));
                printRowSummary();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        private static void cmdResetFilter()
        {
            if (!requireRecordset()) return;
            db.resetFilter();
            refresh();
            Console.WriteLine("Filter cleared.");
            printRowSummary();
        }

        // Select-Column: pick which columns are visible in the
        // ListView. Comma-separated list. With no argument, prints
        // the current select-list (or "(default rules)" if none
        // set). Empty argument or the literal word "reset" clears
        // the user override. Invalid column names are dropped
        // silently and reported.
        private static void cmdSelectColumn(string sArg)
        {
            if (!requireRecordset()) return;
            string sExpr = sArg.Trim();
            string sTable = db.currentTable;
            if (sExpr.Length == 0)
            {
                string sCurrent = db.getSelectList(sTable);
                if (sCurrent.Length == 0)
                    Console.WriteLine("Select-Column: (default rules) -- "
                        + string.Join(", ", db.getDisplayFieldNames().ToArray()));
                else
                    Console.WriteLine("Select-Column: " + sCurrent);
                Console.WriteLine("Usage: Select-Column col1, col2, col3");
                Console.WriteLine("       Select-Column reset       (revert to default rules)");
                return;
            }
            if (string.Equals(sExpr, "reset", StringComparison.OrdinalIgnoreCase))
            {
                db.setSelectList(sTable, "");
                refresh();
                Console.WriteLine("Select-Column cleared; default rules apply.");
                return;
            }

            List<string> lRequested = DbDuoManager.parseSelectList(sExpr);
            string sStored = db.setSelectList(sTable, sExpr);
            List<string> lAccepted = DbDuoManager.parseSelectList(sStored);
            HashSet<string> hAccepted = new HashSet<string>(lAccepted, StringComparer.OrdinalIgnoreCase);
            List<string> lDropped = new List<string>();
            foreach (string sName in lRequested)
                if (!hAccepted.Contains(sName)) lDropped.Add(sName);

            refresh();
            Console.WriteLine("Select-Column: " + (sStored.Length > 0 ? sStored : "(empty -- showing default)"));
            if (lDropped.Count > 0)
                Console.WriteLine("Dropped (not column names of this table): "
                    + string.Join(", ", lDropped.ToArray()));
        }

        // Sort the current recordset by the given expression. The expression
        // is ADO sort syntax: "col" or "col ASC" or "col DESC", optionally
        // multiple columns separated by commas. An empty string clears the
        // sort (equivalent to Reset-Sort).
        //
        // Note: this is the dot-prompt counterpart of the GUI's Sort-Object
        // menu item.
        private static void cmdSortObject(string sArg)
        {
            if (!requireRecordset()) return;
            try
            {
                db.sort = sArg.Trim();
                refresh();
                Console.WriteLine("Sort: " + (db.sort.Length > 0 ? db.sort : "(none)"));
                printRowSummary();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        private static void cmdResetSort()
        {
            if (!requireRecordset()) return;
            db.resetSort();
            refresh();
            Console.WriteLine("Sort cleared.");
            printRowSummary();
        }

        private static void cmdNewRecord()
        {
            if (!requireRecordset()) return;
            if (db.readOnly) { Console.WriteLine("Database is read-only."); return; }
            List<string> lFields = db.getFieldNames();
            Console.WriteLine("(Press Enter to skip a field. Type a value to set it.)");
            try
            {
                db.addNew();
                foreach (string sCol in lFields)
                {
                    Console.Write(sCol + ": ");
                    string sV = Console.ReadLine();
                    if (sV == null) { db.cancelUpdate(); return; }
                    if (sV.Length > 0) db.setFieldValue(sCol, sV);
                }
                db.update();
                refresh();
                Console.WriteLine("Saved new record.");
            }
            catch (Exception oEx)
            {
                try { db.cancelUpdate(); } catch { }
                Console.WriteLine("Error: " + oEx.Message);
            }
        }

        private static void cmdSetRecord()
        {
            if (!requireRecordset()) return;
            if (db.readOnly) { Console.WriteLine("Database is read-only."); return; }
            if (db.eof || db.bof) { Console.WriteLine("(no current record)"); return; }
            List<string> lFields = db.getFieldNames();
            Console.WriteLine("(Press Enter to keep the existing value. Type a new value to change it.)");
            try
            {
                foreach (string sCol in lFields)
                {
                    string sCur = db.getFieldValue(sCol);
                    Console.Write(string.Format("{0} [{1}]: ", sCol, sCur));
                    string sV = Console.ReadLine();
                    if (sV == null) { db.cancelUpdate(); return; }
                    if (sV.Length > 0) db.setFieldValue(sCol, sV);
                }
                db.update();
                refresh();
                Console.WriteLine("Saved.");
            }
            catch (Exception oEx)
            {
                try { db.cancelUpdate(); } catch { }
                Console.WriteLine("Error: " + oEx.Message);
            }
        }

        private static void cmdRemoveRecord()
        {
            if (!requireRecordset()) return;
            if (db.readOnly) { Console.WriteLine("Database is read-only."); return; }
            if (db.eof || db.bof) { Console.WriteLine("(no current record)"); return; }
            Console.Write("Confirm Remove-Record? (y/N): ");
            string s = Console.ReadLine();
            if (s == null || !s.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine("Cancelled."); return; }
            try
            {
                db.deleteCurrent();
                refresh();
                Console.WriteLine("Removed.");
                printRowSummary();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        private static void cmdCopyRecord()
        {
            if (!requireRecordset()) return;
            List<string> lV = new List<string>();
            foreach (string sN in db.getFieldNames()) lV.Add(db.getFieldValue(sN));
            string sLine = string.Join("\t", lV);
            try { oForm.Invoke(new Action(() => Clipboard.SetText(sLine))); }
            catch { Console.WriteLine("(clipboard unavailable)"); return; }
            Console.WriteLine("Row copied to clipboard.");
        }

        private static void cmdSelectTable(string sArg)
        {
            if (!requireOpen()) return;
            string sName = sArg.Trim();
            if (sName.Length == 0) { Console.WriteLine("Select-Table requires a table or view name."); return; }
            List<string> lT = db.getTableAndViewNames();
            if (!lT.Contains(sName))
            {
                Console.WriteLine("No such table or view: " + sName);
                Console.WriteLine("Available: " + string.Join(", ", lT));
                return;
            }
            try {
                db.selectTable(sName);
                refresh();
                Console.WriteLine("Switched to "
                    + (db.isViewName(sName) ? "view: " : "table: ") + sName);
                printRowSummary();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        private static void cmdGetTable()
        {
            if (!requireOpen()) return;
            List<string> lTables = db.getTableNames();
            List<string> lViews = db.getViewNames();
            if (lTables.Count > 0)
            {
                Console.WriteLine("Tables (" + lTables.Count + "):");
                foreach (string sN in lTables) Console.WriteLine("  " + sN);
            }
            if (lViews.Count > 0)
            {
                if (lTables.Count > 0) Console.WriteLine();
                Console.WriteLine("Views (" + lViews.Count + "):");
                foreach (string sN in lViews) Console.WriteLine("  " + sN);
            }
            if (lTables.Count == 0 && lViews.Count == 0)
                Console.WriteLine("(no tables or views)");
        }

        private static void cmdMeasureTable()
        {
            if (!requireRecordset()) return;
            Console.WriteLine("Records: " + db.recordCount);
            if (db.filter.Length > 0) Console.WriteLine("Filter:  " + db.filter);
            if (db.sort.Length > 0) Console.WriteLine("Sort:    " + db.sort);
        }

        // Get-Property: dump the schema details for the current table.
        // Mirrors the GUI's Get-Property dialog. Tables, columns, types,
        // size hints, and inferred foreign-key relationships.
        private static void cmdGetProperty()
        {
            if (!requireRecordset()) return;
            string sTable = db.currentTable ?? "";
            Console.WriteLine((db.currentIsView ? "View" : "Table") + ": " + sTable);
            Console.WriteLine("Records: " + db.recordCount);
            Console.WriteLine();
            Console.WriteLine("Columns:");
            foreach (string sCol in db.getDistinctFieldNames())
            {
                int iSize = db.getFieldDefinedSize(sCol);
                Console.WriteLine("  " + sCol + "  [" + db.getFieldTypeName(sCol) + "]"
                    + (iSize > 0 && iSize < 0x7FFFFFFF ? " size=" + iSize : ""));
            }
            List<string> lMeta = db.getMetadataFieldNames();
            if (lMeta.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("metadata columns:");
                foreach (string sCol in lMeta)
                    Console.WriteLine("  " + sCol + "  [" + db.getFieldTypeName(sCol) + "]");
            }
            // Inferred FKs (any *_id column other than own PK).
            string sOwnPk = sTable + Metadata.PrimaryKeySuffix;
            List<string> lFks = new List<string>();
            foreach (string sCol in db.getFieldNames())
            {
                if (Str.equiv(sCol, sOwnPk)) continue;
                if (Str.endsWith(sCol, Metadata.PrimaryKeySuffix))
                    lFks.Add(sCol);
            }
            if (lFks.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Inferred foreign keys (by name convention):");
                foreach (string sFk in lFks)
                {
                    string sTarget = sFk.Substring(0, sFk.Length - Metadata.PrimaryKeySuffix.Length);
                    Console.WriteLine("  " + sFk + " -> " + sTarget);
                }
            }
        }

        // Set-Mark / Clear-Mark: toggle the 'marked' column on the current row.
        private static void cmdSetMark(bool bValue)
        {
            if (!requireRecordset()) return;
            if (db.readOnly) { Console.WriteLine("(database is read-only)"); return; }
            if (db.currentIsView) { Console.WriteLine("(current selection is a view; not editable)"); return; }
            if (db.eof || db.bof) { Console.WriteLine("(no current row)"); return; }
            if (!db.hasField(Metadata.MarkedColumn))
            {
                Console.WriteLine("(current table has no '" + Metadata.MarkedColumn + "' column)");
                return;
            }
            try
            {
                db.setFieldValue(Metadata.MarkedColumn, bValue ? "1" : "0");
                db.update();
                refresh();
                Console.WriteLine((bValue ? "Marked" : "Cleared") + " row " + db.absolutePosition);
            }
            catch (Exception oEx)
            {
                try { db.cancelUpdate(); } catch { }
                Console.WriteLine("Error: " + oEx.Message);
            }
        }

        private static void cmdMeasureField(string sArg)
        {
            if (!requireRecordset()) return;
            string[] aT = sArg.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (aT.Length == 0) { Console.WriteLine("Measure-Field <field> [count|longest|shortest|min|max|sum|avg]"); return; }
            string sField = aT[0];
            string sStat = aT.Length > 1 ? aT[1] : "count";
            try
            {
                DbDuoManager.FieldStatistic oResult = db.measureField(sField, sStat);
                Console.WriteLine("Field: " + oResult.fieldName);
                Console.WriteLine("Statistic: " + oResult.statistic);
                Console.WriteLine("Value: " + oResult.value);
                if (oResult.recordPosition > 0) Console.WriteLine("At row: " + oResult.recordPosition);
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        // Measure-Longest <field> / Measure-Shortest <field>:
        // dbDot's "longest" / "shortest" commands. Walks the
        // recordset and reports the row with the longest /
        // shortest string value in the named column.
        private static void cmdMeasureLength(string sArg, bool bLongest)
        {
            if (!requireRecordset()) return;
            string sField = sArg.Trim();
            if (sField.Length == 0)
            {
                Console.WriteLine((bLongest ? "Measure-Longest" : "Measure-Shortest") + " <field>");
                return;
            }
            try
            {
                DbDuoManager.FieldStatistic oR = db.measureField(sField, bLongest ? "longest" : "shortest");
                Console.WriteLine(oR.value);
                if (oR.recordPosition > 0) Console.WriteLine("At row: " + oR.recordPosition);
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        // Measure-Maximum <field> / Measure-Minimum <field>: dbDot's
        // "max" / "min" commands. Walks the recordset comparing
        // values by their native type (numeric / date / string) and
        // reports the row holding the extreme.
        private static void cmdMeasureExtreme(string sArg, bool bMax)
        {
            if (!requireRecordset()) return;
            string sField = sArg.Trim();
            if (sField.Length == 0)
            {
                Console.WriteLine((bMax ? "Measure-Maximum" : "Measure-Minimum") + " <field>");
                return;
            }
            try
            {
                DbDuoManager.FieldStatistic oR = db.measureField(sField, bMax ? "max" : "min");
                Console.WriteLine(oR.value);
                if (oR.recordPosition > 0) Console.WriteLine("At row: " + oR.recordPosition);
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        // Get-FieldName: print the recordset's full field name list
        // (including standard hidden fields). dbDot's "fields"
        // command. Useful before calling Measure-Maximum or similar
        // when the user doesn't remember the exact column name.
        private static void cmdGetFieldNames()
        {
            if (!requireRecordset()) return;
            foreach (string sName in db.getFieldNames())
                Console.WriteLine(sName);
        }

        private static void cmdTestDatabase()
        {
            if (!requireOpen()) return;
            try { db.invokeSql("PRAGMA integrity_check", Console.Out); }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        private static void cmdTestDriver()
        {
            Console.WriteLine("=== ODBC drivers ===");
            try
            {
                Microsoft.Win32.RegistryKey oKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\ODBC\ODBCINST.INI\ODBC Drivers");
                if (oKey != null)
                {
                    foreach (string sName in oKey.GetValueNames())
                        Console.WriteLine("  " + sName + " = " + oKey.GetValue(sName));
                    oKey.Close();
                }
                else Console.WriteLine("  (no drivers registered)");
            }
            catch (Exception oEx) { Console.WriteLine("  (error: " + oEx.Message + ")"); }
            Console.WriteLine();
            Console.WriteLine("SQLite ODBC Driver: " + (Type.GetTypeFromProgID("ADODB.Connection") != null ? "(ADODB present)" : "(ADODB MISSING)"));
        }

        private static void cmdUpdateView()
        {
            if (!requireRecordset()) return;
            try { db.requery(); refresh(); Console.WriteLine("View refreshed."); printRowSummary(); }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        private static void cmdSaveAs(string sArg)
        {
            if (!requireOpen()) return;
            string sPath = sArg.Trim();
            if (sPath.Length == 0) { Console.WriteLine("Save-DatabaseAs requires a destination path."); return; }
            try { db.saveAs(sPath); refresh(); Console.WriteLine("Saved to " + sPath); }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        // cmdExportData: dbDot-compatible multi-format export.
        // Argument grammar:
        //
        //   Export-Data                       -> xlsx, into the
        //                                        database's folder,
        //                                        named after the
        //                                        current table.
        //   Export-Data xlsx docx html csv    -> all four at once.
        //   Export-Data x d h c               -> dbDot single-letter
        //                                        shortcuts; same as
        //                                        above.
        //   Export-Data csv path/to/file.csv  -> single-file form
        //                                        with explicit path
        //                                        (backward compat).
        //
        // After writing, each file is opened with its default Windows
        // application via ShellExecute (matches dbDot's shellOpen).
        private static void cmdExportData(string sArg)
        {
            if (!requireRecordset()) return;
            string sTrim = (sArg ?? "").Trim();

            // Detect the legacy single-path form: if the argument
            // contains a path separator or ends with a recognized
            // file extension, treat it as a single destination path.
            // Otherwise treat it as a dbDot-style extension list.
            bool bLegacyPath = sTrim.Contains("\\")
                            || sTrim.Contains("/")
                            || sTrim.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                            || sTrim.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase)
                            || sTrim.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                            || sTrim.EndsWith(".htm",  StringComparison.OrdinalIgnoreCase)
                            || sTrim.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                            || sTrim.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

            try
            {
                if (bLegacyPath)
                {
                    db.exportData(sTrim);
                    Console.WriteLine("Exported to " + sTrim);
                    ComAutomation.shellOpen(sTrim);
                    return;
                }

                // Multi-format form. Empty argument defaults to xlsx.
                List<string> lWritten = db.exportDataMulti(sTrim, null, null);
                if (lWritten.Count == 0)
                {
                    Console.WriteLine("Export-Data: nothing requested (no recognized format).");
                    Console.WriteLine("Try: Export-Data xlsx docx html csv");
                    return;
                }
                foreach (string sP in lWritten)
                {
                    Console.WriteLine("  wrote " + sP);
                    ComAutomation.shellOpen(sP);
                }
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        // cmdImportData: read a Markdown table file and append its
        // rows to the currently-open table. Header cell names are
        // matched case-insensitively to columns; cells with no
        // matching column are dropped.
        //
        //   Import-Data path\to\file.md
        //
        // For non-Markdown formats, use Invoke-Sql with INSERT INTO
        // ... SELECT or the database's own native import path.
        private static void cmdImportData(string sArg)
        {
            if (!requireRecordset()) return;
            string sPath = (sArg ?? "").Trim();
            if (sPath.Length == 0) { Console.WriteLine("Import-Data requires a file path. Currently supports .md (Markdown table)."); return; }
            try
            {
                int iCount = db.importMarkdown(sPath);
                Console.WriteLine("Imported " + iCount + " row(s) into " + db.currentTable + ".");
                refresh();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        private static void cmdOpenDatabase(string sArg)
        {
            string sPath = sArg.Trim();
            if (sPath.Length == 0) { Console.WriteLine("Open-Database requires a file path."); return; }
            try
            {
                // If a GUI form is up, clear its drill stack first --
                // stale entries from the previous database wouldn't
                // map to any row in the new one.
                if (oForm != null && !oForm.IsDisposed) oForm.clearDrillStack();
                db.openDatabase(sPath, null, false);
                if (!db.hasRecordset())
                {
                    // Prefer base tables; fall back to views.
                    List<string> lT = db.getTableNames();
                    if (lT.Count == 0) lT = db.getViewNames();
                    if (lT.Count > 0) db.selectTable(lT[0]);
                }
                refresh();
                Console.WriteLine("Opened: " + db.filePath);
                printRowSummary();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        private static void cmdCloseDatabase()
        {
            if (db == null) return;
            if (oForm != null && !oForm.IsDisposed) oForm.clearDrillStack();
            try { db.close(); refresh(); Console.WriteLine("Closed."); }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        private static object oSavedBookmark;
        private static void cmdSaveBookmark()
        {
            if (!requireRecordset()) return;
            oSavedBookmark = db.bookmark;
            Console.WriteLine("Bookmark saved at row " + db.absolutePosition);
        }

        private static void cmdRestoreBookmark()
        {
            if (!requireRecordset()) return;
            if (oSavedBookmark == null) { Console.WriteLine("No saved bookmark."); return; }
            db.bookmark = oSavedBookmark;
            refresh();
            Console.WriteLine("Bookmark restored.");
            printRowSummary();
        }

        // Clear-Bookmark: forget the saved row.
        private static void cmdClearBookmark()
        {
            oSavedBookmark = null;
            Console.WriteLine("Bookmark cleared.");
        }

        private static void cmdInvokeSql(string sArg)
        {
            if (!requireOpen()) return;
            string sSql = sArg.Trim();
            if (sSql.Length == 0) { Console.WriteLine("Invoke-Sql requires a SQL command."); return; }
            try
            {
                int iAffected = db.invokeSql(sSql, Console.Out);
                if (iAffected >= 0)
                {
                    Console.WriteLine("(" + iAffected + " record(s) affected)");
                    refresh();
                }
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        // Help dispatch: bare 'help' shows the command list; 'help X'
        // shows details for command X. Topic lookup is case-insensitive
        // and accepts both the canonical PowerShell name and any
        // aliases ('help find', 'help find-record', 'help f' all hit
        // the same topic).
        private static void cmdGetHelp(string sTopic)
        {
            sTopic = (sTopic ?? "").Trim();
            if (sTopic.Length == 0) { printHelpIndex(); return; }

            string sCanonical = resolveAlias(sTopic.ToLowerInvariant());
            string sDetails = lookupHelpTopic(sCanonical);
            if (sDetails == null)
                sDetails = lookupHelpTopic(sTopic.ToLowerInvariant());
            if (sDetails == null)
            {
                Console.WriteLine();
                Console.WriteLine("No help topic found for: " + sTopic);
                Console.WriteLine("Type 'help' (no argument) for the command list.");
                Console.WriteLine();
                return;
            }
            Console.WriteLine();
            Console.WriteLine(sDetails);
            Console.WriteLine();
        }

        // The command-list view shown by bare 'help'.
        private static void printHelpIndex()
        {
            Console.WriteLine();
            Console.WriteLine("DbDuo dot-prompt CLI commands. Aliases in parentheses.");
            Console.WriteLine("Type 'help <command>' for details on any command.");
            Console.WriteLine();
            Console.WriteLine("  NAVIGATION");
            Console.WriteLine("    Step-Record next      (n, +)");
            Console.WriteLine("    Step-Record previous  (p, -, prev)");
            Console.WriteLine("    Step-Record first     (t, top)");
            Console.WriteLine("    Step-Record last      (b, bot, last)");
            Console.WriteLine("    Set-Position N        (g N, #N)");
            Console.WriteLine();
            Console.WriteLine("  DISPLAY");
            Console.WriteLine("    Show-Object           (=, display, disp)");
            Console.WriteLine("    Show-Object col1,col2");
            Console.WriteLine("    Show-Table            (l, list)");
            Console.WriteLine("    Show-Table all");
            Console.WriteLine("    Show-Schema [table]");
            Console.WriteLine("    Show-Status           (?)");
            Console.WriteLine("    Get-Field name");
            Console.WriteLine();
            Console.WriteLine("  EDIT");
            Console.WriteLine("    New-Record            (a, &, add)");
            Console.WriteLine("    Set-Record            (e, ^, edit)");
            Console.WriteLine("    Set-Field name value");
            Console.WriteLine("    Remove-Record         (del, delete)");
            Console.WriteLine("    Copy-Record");
            Console.WriteLine();
            Console.WriteLine("  QUERY / FILTER / SORT");
            Console.WriteLine("    Find-Record \"col LIKE '%text%'\"  (f)");
            Console.WriteLine("    Find-RecordAgain                 (find-next)");
            Console.WriteLine("    Find-RecordPrevious              (find-prev)");
            Console.WriteLine("    Select-Record \"col LIKE '%text%'\" (filter)");
            Console.WriteLine("    Reset-Filter                     (all)");
            Console.WriteLine("    Select-Column col1, col2         (cols, Select-Object)");
            Console.WriteLine("    Sort-Object \"col ASC\"            (sort)");
            Console.WriteLine("    Reset-Sort");
            Console.WriteLine();
            Console.WriteLine("  TABLE / SCHEMA");
            Console.WriteLine("    Select-Table name     (use, @name)");
            Console.WriteLine("    Get-Table             (tables)");
            Console.WriteLine("    Get-Property          (props)");
            Console.WriteLine();
            Console.WriteLine("  DRILL-DOWN (parent <-> child)");
            Console.WriteLine("    Enter-Child           (zoom, drill)   -- drill from parent row into a child table");
            Console.WriteLine("    Exit-Child            (unzoom, back)  -- return from child to parent row");
            Console.WriteLine("    Show-Related          (related)       -- child to parent (the FK target)");
            Console.WriteLine();
            Console.WriteLine("  MARK");
            Console.WriteLine("    Set-Mark              (mark)");
            Console.WriteLine("    Clear-Mark            (unmark)");
            Console.WriteLine();
            Console.WriteLine("  TOOLS / DATA");
            Console.WriteLine("    Measure-Table                    (count, y)");
            Console.WriteLine("    Measure-Field name [stat]        (sum, avg)");
            Console.WriteLine("    Measure-Longest field            (longest)");
            Console.WriteLine("    Measure-Shortest field           (shortest)");
            Console.WriteLine("    Measure-Maximum field             (max)");
            Console.WriteLine("    Measure-Minimum field             (min)");
            Console.WriteLine("    Get-FieldName                     (fields)");
            Console.WriteLine("    Test-Database                    (test)");
            Console.WriteLine("    Test-Driver                      (drivers)");
            Console.WriteLine("    Update-View                      (refresh)");
            Console.WriteLine("    Save-DatabaseAs path             (save-as)");
            Console.WriteLine("    Backup-Database path             (backup)");
            Console.WriteLine("    Export-Data path                 (export)");
            Console.WriteLine("    Open-Database path               (open)");
            Console.WriteLine("    Close-Database                   (close)");
            Console.WriteLine("    Invoke-Sql query                 (sql, ;)");
            Console.WriteLine("    Out-File path | -a path | stdout (output, tee, o)");
            Console.WriteLine("    Invoke-Script path               (read, script, i)");
            Console.WriteLine();
            Console.WriteLine("  BOOKMARK");
            Console.WriteLine("    Save-Bookmark         (bookmark)");
            Console.WriteLine("    Restore-Bookmark      (goto-bookmark)");
            Console.WriteLine("    Clear-Bookmark");
            Console.WriteLine();
            Console.WriteLine("  META");
            Console.WriteLine("    Get-Help              (help)");
            Console.WriteLine("    Get-Verb              (verbs)");
            Console.WriteLine("    Trace-Command on|off  (trace)");
            Console.WriteLine("    Sync-Session          (sync)");
            Console.WriteLine("    Switch-Focus          (gui, focus, window)  -- bring GUI window to foreground");
            Console.WriteLine("    Exit-Console          (exit, x, bye)        -- leave dot prompt; GUI keeps running");
            Console.WriteLine("    Exit-Application      (quit, q)             -- close entire DbDuo program");
            Console.WriteLine();
        }

        // Per-command detail. Each topic entry is a self-contained
        // description: what the command does, syntax, example uses,
        // and any caveats. Topics are keyed by the canonical lower-
        // cased PowerShell name so the alias resolver in cmdGetHelp
        // can hit them without duplication.
        private static string lookupHelpTopic(string sName)
        {
            switch (sName)
            {
                case "step-record":
                    return "Step-Record - move the current row pointer.\n\n"
                         + "  Step-Record next [N]   (alias: n, +)        forward N rows (default 1)\n"
                         + "  Step-Record prev [N]   (alias: p, -)        backward N rows\n"
                         + "  Step-Record first      (alias: t, top)\n"
                         + "  Step-Record last       (alias: b, bot)\n"
                         + "  Step-Record 5          dBASE SKIP: forward 5 rows\n"
                         + "  Step-Record -3         backward 3 rows\n"
                         + "  skip 10                also forward 10 rows\n\n"
                         + "Bare 'n' / 'p' / 't' / 'b' at the dot prompt step in those\n"
                         + "directions. The current record is reflected in the GUI grid\n"
                         + "(in Both mode) on the next refresh.";
                case "set-position":
                    return "Set-Position N - jump to record number N (1-based).\n\n"
                         + "  Set-Position 42        Jump to row 42.\n"
                         + "  g 42                   Same.\n"
                         + "  #42                    Same.\n\n"
                         + "Out-of-range values clamp to the first or last row.\n"
                         + "Percent form (50%) is accepted in the GUI Set-Position\n"
                         + "dialog but not at the dot prompt.";
                case "show-object":
                    return "Show-Object - examine the current row.\n\n"
                         + "  Show-Object            Display fields + related records.\n"
                         + "  Show-Object col1,col2  Just the named columns (no related).\n"
                         + "  Show-Object all        Every field of the recordset (no related).\n"
                         + "  =, disp                Same as bare Show-Object.\n\n"
                         + "The bare form prints two sections:\n"
                         + "  1. Each visible field on the current row, as 'name = value'.\n"
                         + "  2. Related records, grouped by table. Each parent reached\n"
                         + "     by an outbound foreign key contributes one line. Each\n"
                         + "     child table that references this row contributes up to\n"
                         + "     25 'look' values; an '(... N more)' footer appears when\n"
                         + "     more rows exist. Use Enter-Child (Shift+E) to drill in\n"
                         + "     for the full child list.\n\n"
                         + "In the GUI, Show-Object is bound to Enter and opens a read-\n"
                         + "only dialog. Binary fields show length only.";
                case "show-table":
                    return "Show-Table - print rows of the current table. Argument\n"
                         + "grammar mirrors dBASE LIST [scope] [FIELDS list]:\n\n"
                         + "  Show-Table                               first 50 rows\n"
                         + "  Show-Table all                           every row\n"
                         + "  Show-Table next 10                       next 10 rows from current\n"
                         + "  Show-Table for \"marked = true\"           rows matching ADO filter\n"
                         + "  Show-Table fields:name,updated           restrict columns\n"
                         + "  Show-Table next 5 fields:name            combined\n"
                         + "  l                                        alias for Show-Table\n\n"
                         + "The recordset's current Filter and Sort apply on top of\n"
                         + "any 'for' clause (combined with AND). 'fields:' overrides\n"
                         + "the Select-Column display set for this call only.";
                case "show-schema":
                    return "Show-Schema - list tables and views, optionally with column\n"
                         + "definitions for one table.\n\n"
                         + "  Show-Schema            All tables and views in the database.\n"
                         + "  Show-Schema name       Detailed schema of the named table.\n\n"
                         + "View names in DbDuo conventionally start with 'view_' and\n"
                         + "are opened read-only by Select-Table.";
                case "show-status":
                    return "Show-Status - print a one-line summary of the current\n"
                         + "database, table, filter, sort, and row position.\n\n"
                         + "  Show-Status            (alias: ?)\n\n"
                         + "Useful as a checkpoint when working at the dot prompt.";
                case "new-record":
                    return "New-Record - append a row to the current table.\n\n"
                         + "  New-Record             Open the field editor with empty values.\n"
                         + "  a   &   add            Aliases.\n\n"
                         + "Primary-key columns the database manages (autoincrement) are\n"
                         + "shown read-only and left blank for the database to assign.";
                case "set-record":
                    return "Set-Record - edit the current row's values.\n\n"
                         + "  Set-Record             Open the field editor on the current row.\n"
                         + "  e   ^   edit           Aliases.\n\n"
                         + "Read-only columns (primary keys, view columns) are shown\n"
                         + "but cannot be changed.";
                case "set-field":
                    return "Set-Field name value - update a single column on the current row.\n\n"
                         + "  Set-Field title \"New title\"\n"
                         + "  Set-Field marked 1\n\n"
                         + "Strings with spaces must be quoted.";
                case "remove-record":
                    return "Remove-Record - delete the current row.\n\n"
                         + "  Remove-Record          (alias: del, delete)\n\n"
                         + "Prompts for confirmation in the GUI; at the dot prompt the\n"
                         + "deletion is immediate. Reversible only by restoring from a\n"
                         + "backup -- consider Save-DatabaseAs before bulk deletion.";
                case "copy-record":
                    return "Copy-Record - copy the current row to the clipboard as\n"
                         + "tab-separated values.\n\n"
                         + "Useful for pasting into Excel, a notes app, or Set-Record\n"
                         + "on a different row.";
                case "find-record":
                case "jump-record":
                    return "Jump-Record - move to the first row matching a SQL WHERE-\n"
                         + "style expression, starting from the current row.\n\n"
                         + "  Jump-Record \"title LIKE '%draft%'\"\n"
                         + "  j \"author = 'Smith'\"\n"
                         + "  f \"author = 'Smith'\"  (backward-compat alias)\n\n"
                         + "Matching is case-insensitive for SQLite TEXT columns by\n"
                         + "default. Use Jump-RecordAgain (F3 in the GUI, jump-next at\n"
                         + "the dot prompt) to repeat.\n\n"
                         + "Note: this command was previously called Find-Record. The\n"
                         + "verb changed to Jump so that Control+F could be repurposed\n"
                         + "for filter (Select-Record). The old name still works as an\n"
                         + "alias.";
                case "find-recordagain":
                case "find-recordprevious":
                case "jump-recordagain":
                case "jump-recordprevious":
                    return "Jump-RecordAgain / Jump-RecordPrevious - repeat the last\n"
                         + "Jump-Record forward or backward.\n\n"
                         + "  Jump-RecordAgain       (aliases: jump-next, find-next, F3 in GUI)\n"
                         + "  Jump-RecordPrevious    (aliases: jump-prev, find-prev, Shift+F3 in GUI)\n\n"
                         + "If no Jump-Record has been issued, prints a hint.";
                case "select-record":
                    return "Select-Record - filter the current table to rows matching\n"
                         + "a SQL WHERE expression. Filtered out rows stay in the\n"
                         + "underlying table; only the visible recordset shrinks.\n\n"
                         + "  Select-Record \"marked = 1\"\n"
                         + "  filter \"updated > '2024-01-01'\"\n"
                         + "  filter marked        (shortcut for: marked = true)\n"
                         + "  filter unmarked      (shortcut for: marked = false)\n\n"
                         + "Reset with Reset-Filter (alias 'all'). Filter and sort\n"
                         + "settings persist for the table for the rest of the\n"
                         + "session and are restored on Select-Table back to it.";
                case "reset-filter":
                    return "Reset-Filter - clear any active Select-Record filter so the\n"
                         + "full table is visible again.\n\n"
                         + "  Reset-Filter           (alias: all)";
                case "select-column":
                    return "Select-Column - pick which columns appear in the data\n"
                         + "list view. Other columns stay accessible through\n"
                         + "Show-Record, Get-Property, Set-Record, and the dot\n"
                         + "prompt -- only the scrolling row view is filtered.\n\n"
                         + "  Select-Column source, category, name\n"
                         + "  Select-Object source, category, name    (PowerShell-canonical alias)\n"
                         + "  cols source, category, name             (short alias)\n"
                         + "  Select-Column                           (print current selection)\n"
                         + "  Select-Column reset                     (revert to defaults)\n\n"
                         + "Invalid column names are dropped silently and reported.\n"
                         + "The selection is remembered per table for the session and\n"
                         + "is also exposed via the View > Select-Column menu item.";
                case "sort-object":
                    return "Sort-Object - order rows by one or more columns.\n\n"
                         + "  Sort-Object \"updated DESC\"\n"
                         + "  Sort-Object \"author ASC, title ASC\"\n"
                         + "  sort \"id\"\n\n"
                         + "Like Filter, the sort persists for the table for the\n"
                         + "rest of the session.";
                case "reset-sort":
                    return "Reset-Sort - clear any active Sort-Object so the table\n"
                         + "appears in its underlying storage order.";
                case "select-table":
                    return "Select-Table - switch to a different table or view.\n\n"
                         + "  Select-Table customers\n"
                         + "  use customers\n"
                         + "  @customers\n\n"
                         + "If the table was already opened earlier in this session,\n"
                         + "its filter, sort, and current row are restored. View names\n"
                         + "starting with 'view_' open read-only.";
                case "get-table":
                    return "Get-Table - list base tables in the current database.\n\n"
                         + "  Get-Table              (alias: tables)\n\n"
                         + "For both base tables and views, see Show-Schema.";
                case "get-property":
                    return "Get-Property - print structural details of the current\n"
                         + "table: column names, types, sizes, and inferred foreign\n"
                         + "key relationships.\n\n"
                         + "  Get-Property           (alias: props)";
                case "set-mark":
                case "clear-mark":
                    return "Set-Mark / Clear-Mark - flag a row for later batch action.\n\n"
                         + "  Set-Mark               Mark the current row.\n"
                         + "  Clear-Mark             Unmark the current row.\n\n"
                         + "Marking sets the row's 'marked' column to 1. Useful with\n"
                         + "Select-Record \"marked = 1\" to view marked rows, or\n"
                         + "Update-Field to bulk-edit them.";
                case "measure-table":
                    return "Measure-Table - count rows and bytes for each table in the\n"
                         + "current database.\n\n"
                         + "  Measure-Table          (alias: count, y)\n\n"
                         + "Output is one row per table with row count and approximate\n"
                         + "size in bytes (computed via the database engine's stats).";
                case "measure-field":
                    return "Measure-Field - compute a statistic on a column.\n\n"
                         + "  Measure-Field price                Default statistic (count).\n"
                         + "  Measure-Field price min\n"
                         + "  Measure-Field price max\n"
                         + "  Measure-Field price sum\n"
                         + "  Measure-Field price average\n\n"
                         + "Statistic names: count, min, max, sum, average, distinct.";
                case "measure-longest":
                    return "Measure-Longest <field> - find the row with the longest\n"
                         + "string value in a column.\n\n"
                         + "  Measure-Longest notes              (alias: longest notes)\n\n"
                         + "Prints the longest value and the row position. Mirrors\n"
                         + "dbDot's 'longest' command. The current row pointer is\n"
                         + "saved and restored; the scan does not move it.";
                case "measure-shortest":
                    return "Measure-Shortest <field> - find the row with the shortest\n"
                         + "string value in a column.\n\n"
                         + "  Measure-Shortest notes             (alias: shortest notes)\n\n"
                         + "Useful for finding empty or near-empty values. Mirrors\n"
                         + "dbDot's 'shortest' command.";
                case "measure-maximum":
                    return "Measure-Maximum <field> - find the row with the maximum\n"
                         + "value in a column. Comparison uses the column's native\n"
                         + "type, so numeric columns compare numerically, date columns\n"
                         + "compare chronologically.\n\n"
                         + "  Measure-Maximum updated            (alias: max updated)\n\n"
                         + "Mirrors dbDot's 'max' command.";
                case "measure-minimum":
                    return "Measure-Minimum <field> - find the row with the minimum\n"
                         + "value in a column.\n\n"
                         + "  Measure-Minimum added              (alias: min added)\n\n"
                         + "Mirrors dbDot's 'min' command.";
                case "get-fieldname":
                    return "Get-FieldName - print the full list of field (column)\n"
                         + "names for the current recordset, including standard\n"
                         + "hidden ones (table_id, added, updated, marked, look, etc).\n\n"
                         + "  Get-FieldName                      (alias: fields)\n\n"
                         + "Useful before Measure-Maximum or similar when you don't\n"
                         + "remember the exact column name. Mirrors dbDot's 'fields'\n"
                         + "command.";
                case "test-database":
                    return "Test-Database - run an integrity check on the current\n"
                         + "database file. For SQLite this is PRAGMA integrity_check;\n"
                         + "for ACE / Jet, a connection-level test.\n\n"
                         + "  Test-Database          (alias: test)\n\n"
                         + "Result is 'ok' on a healthy database, or a list of\n"
                         + "corruption findings.";
                case "test-driver":
                    return "Test-Driver - probe the system for ODBC and OLE DB providers\n"
                         + "DbDuo can use, and report which file formats are accessible.\n\n"
                         + "  Test-Driver            (alias: drivers)\n\n"
                         + "Useful when a particular file extension fails to open --\n"
                         + "the report says whether the underlying provider is\n"
                         + "installed.";
                case "update-view":
                    return "Update-View - refresh the visible recordset from the\n"
                         + "database. Picks up changes made by other connections or\n"
                         + "by Invoke-Sql in this session.\n\n"
                         + "  Update-View            (alias: refresh)";
                case "update-field":
                    return "Update-Field - search-and-replace across rows of the\n"
                         + "current table.\n\n"
                         + "  Update-Field colName oldText newText\n\n"
                         + "Updates every row where colName contains oldText. Quote\n"
                         + "values with spaces. Use Save-DatabaseAs first if you are\n"
                         + "not sure of the impact -- the change is committed\n"
                         + "immediately.";
                case "save-databaseas":
                    return "Save-DatabaseAs - copy the current database to a new file.\n\n"
                         + "  Save-DatabaseAs newpath.db   (alias: save-as)\n\n"
                         + "Closes the current database, copies the file, and reopens\n"
                         + "the new copy. Useful as a checkpoint before bulk edits.";
                case "backup-database":
                    return "Backup-Database - copy the current database file to a\n"
                         + "backup path WITHOUT switching to it.\n\n"
                         + "  Backup-Database backup.db    (alias: backup)\n\n"
                         + "Unlike Save-DatabaseAs, the original database stays open\n"
                         + "and the backup is a snapshot only.";
                case "export-data":
                    return "Export-Data - write the current table to a file in a\n"
                         + "format chosen by extension.\n\n"
                         + "  Export-Data out.csv          CSV with header row.\n"
                         + "  Export-Data out.xlsx         Excel workbook.\n"
                         + "  Export-Data out.json         JSON array of objects.\n\n"
                         + "Export-Data export                  Alias.";
                case "open-database":
                    return "Open-Database - open a database file. Despite the verb,\n"
                         + "DbDuo treats any ADODB-readable tabular file as a\n"
                         + "'database file' here -- .db, .mdb, .accdb, .xlsx, .csv,\n"
                         + ".dbf are all supported.\n\n"
                         + "  Open-Database path           (alias: open)\n\n"
                         + "Closes any currently open database first.";
                case "close-database":
                    return "Close-Database - release the current database connection,\n"
                         + "discarding the session table cache.\n\n"
                         + "  Close-Database         (alias: close)";
                case "invoke-sql":
                    return "Invoke-Sql - execute an arbitrary SQL statement against\n"
                         + "the current database.\n\n"
                         + "  Invoke-Sql SELECT count(*) FROM tasks\n"
                         + "  ; UPDATE tasks SET marked = 0 WHERE marked = 1\n"
                         + "  sql DELETE FROM log WHERE added < '2020-01-01'\n\n"
                         + "SELECT statements print results; INSERT/UPDATE/DELETE\n"
                         + "report the number of records affected.";
                case "save-bookmark":
                case "restore-bookmark":
                case "clear-bookmark":
                    return "Bookmark commands - remember and return to a row.\n\n"
                         + "  Save-Bookmark          Remember the current row.\n"
                         + "  Restore-Bookmark       Jump back to the saved row.\n"
                         + "  Clear-Bookmark         Forget the saved row.\n\n"
                         + "Aliases: bookmark, goto-bookmark.\n\n"
                         + "Only one bookmark slot exists; saving a new bookmark\n"
                         + "replaces any previous. The bookmark survives sort and\n"
                         + "filter changes (it is an ADO bookmark, not a row number).";
                case "trace-command":
                    return "Trace-Command - turn on or off a verbose mode that prints\n"
                         + "every keystroke and command alias as it is parsed.\n\n"
                         + "  Trace-Command on\n"
                         + "  Trace-Command off\n"
                         + "  trace                  Toggle.";
                case "get-verb":
                    return "Get-Verb - print the list of PowerShell-canonical verbs\n"
                         + "DbDuo uses, organized by category.";
                case "exit-console":
                    return "Exit-Console - leave the dot prompt and return to the GUI.\n"
                         + "In Both mode the GUI keeps running; the console window\n"
                         + "closes. In CLI-only mode this exits DbDuo (no GUI to\n"
                         + "return to).\n\n"
                         + "  exit                   Alias.\n"
                         + "  x                      Alias.\n"
                         + "  bye                    Alias.\n\n"
                         + "Use 'quit' instead to exit the entire DbDuo program\n"
                         + "(closing both the GUI and the dot prompt).";
                case "exit-application":
                    return "Exit-Application - shut down the entire DbDuo process.\n"
                         + "Closes the GUI form and ends the dot prompt at the\n"
                         + "same time. Equivalent to the File > Exit menu\n"
                         + "(Alt+F4) in the GUI.\n\n"
                         + "  quit                   Alias.\n"
                         + "  q                      Alias.\n\n"
                         + "Use 'exit' (or 'x' / 'bye') to leave only the dot\n"
                         + "prompt while keeping the GUI open.";
                case "out-file":
                    return "Out-File - tee subsequent console output to a file.\n"
                         + "Inspired by SQLite's .output and psql's \\o.\n\n"
                         + "  Out-File path.txt        Write to path.txt (overwrite)\n"
                         + "  Out-File -a path.txt     Append to path.txt\n"
                         + "  Out-File stdout          Restore to screen-only\n"
                         + "  Out-File                 Show current redirection target\n"
                         + "  output, tee, o           Aliases\n\n"
                         + "Output is mirrored to both the screen and the file\n"
                         + "while redirection is active, so the screen-reader\n"
                         + "user follows what's being captured. Persists across\n"
                         + "commands until 'Out-File stdout' is run.";
                case "invoke-script":
                    return "Invoke-Script - run commands from a text file as if\n"
                         + "typed at the dot prompt. Inspired by SQLite's .read\n"
                         + "and psql's \\i.\n\n"
                         + "  Invoke-Script path.txt   Run each line\n"
                         + "  read, script, i          Aliases\n\n"
                         + "Blank lines and lines starting with '#' or ';' are\n"
                         + "treated as comments. SQL is dispatched via Invoke-Sql.\n"
                         + "Errors are reported but don't stop the script.\n"
                         + "Combine with Out-File to capture script output:\n\n"
                         + "  Out-File report.txt\n"
                         + "  Invoke-Script daily_report.sql\n"
                         + "  Out-File stdout";
                case "enter-child":
                    return "Enter-Child - drill from the current parent row into a\n"
                         + "child table whose foreign key matches the parent's\n"
                         + "primary key. Distinct from Show-Related, which goes\n"
                         + "the opposite direction (child to parent).\n\n"
                         + "  Enter-Child            From the current row.\n"
                         + "  zoom, drill, enter     Aliases.\n"
                         + "  Hotkey: Control+E      In the GUI.\n\n"
                         + "DbDuo finds every other table that has a column with\n"
                         + "the same name as this table's primary key (the dbDot\n"
                         + "convention <singular>_id, e.g. apps -> app_id). If\n"
                         + "exactly one child table matches, it opens directly;\n"
                         + "otherwise an alphabetized listbox prompts for a\n"
                         + "choice. The child table opens with:\n\n"
                         + "  - Its last-used sort order (per-table sort cache).\n"
                         + "  - Filter set to '<pk> = <value>' from the parent.\n"
                         + "  - Cursor at the first row of the filtered set.\n\n"
                         + "Drill depth is unbounded; nested Enter-Child works\n"
                         + "naturally. Use Exit-Child (Control+Shift+E) to pop\n"
                         + "one level back.";
                case "exit-child":
                    return "Exit-Child - return from a child table opened via\n"
                         + "Enter-Child, back to the parent row you drilled\n"
                         + "from.\n\n"
                         + "  Exit-Child             Pop one level off the stack.\n"
                         + "  unzoom, back, undrill  Aliases.\n"
                         + "  Hotkey: Control+Shift+E In the GUI.\n\n"
                         + "DbDuo restores the parent table's filter, sort, and\n"
                         + "position from the cache, then re-finds the parent\n"
                         + "row by primary-key value (so the navigation is\n"
                         + "robust against intervening filter/sort changes or\n"
                         + "rows added/removed in the parent table). If the\n"
                         + "original parent row was deleted, the cursor lands\n"
                         + "wherever the Find ends.\n\n"
                         + "If no drill-down is in progress, this command\n"
                         + "prints a notice and returns.";
                case "switch-focus":
                    return "Switch-Focus - bring the GUI window to the foreground. The\n"
                         + "dot-prompt console stays open; Alt+Tab returns to it.\n"
                         + "Equivalent to clicking the DbDuo GUI in the taskbar.\n\n"
                         + "  gui                    Alias.\n"
                         + "  focus                  Alias.\n"
                         + "  window                 Alias.\n\n"
                         + "Useful when working primarily in the dot prompt and you\n"
                         + "need to glance at the GUI's data grid. In CLI-only mode\n"
                         + "this command prints a notice and returns.";
                case "get-help":
                    return "Get-Help [topic] - the command you are reading now.\n\n"
                         + "  help                   Print the command index.\n"
                         + "  help <command>         Print details for one command.\n"
                         + "  help find              Resolves through the alias map\n"
                         + "                         to find-record.";
                default:
                    return null;
            }
        }

        private static void cmdGetVerb()
        {
            Console.WriteLine();
            Console.WriteLine("PowerShell-canonical verb categories used in DbDuo:");
            Console.WriteLine();
            Console.WriteLine("  COMMON      New, Get, Set, Remove, Show, Copy, Find, Select, Format,");
            Console.WriteLine("              Enter, Exit, Step, Open, Close, Lock, Add, Reset, Clear");
            Console.WriteLine("  DATA        Backup, Restore, Import, Export, Update, Save, Compare,");
            Console.WriteLine("              Sync, Out");
            Console.WriteLine("  DIAGNOSTIC  Test, Measure, Resolve, Trace");
            Console.WriteLine("  LIFECYCLE   Invoke");
            Console.WriteLine("  OTHER       Sort");
            Console.WriteLine();
            Console.WriteLine("DbDuo avoids non-canonical synonyms (Delete, Create, Read, Modify,");
            Console.WriteLine("Cancel, Search, etc.).");
            Console.WriteLine();
        }

        private static void cmdTraceCommand(string sArg)
        {
            string s = sArg.Trim().ToLowerInvariant();
            if (s == "on") KeyMap.bTraceMode = true;
            else if (s == "off") KeyMap.bTraceMode = false;
            else KeyMap.bTraceMode = !KeyMap.bTraceMode;
            Console.WriteLine("Trace-Command mode: " + (KeyMap.bTraceMode ? "ON" : "OFF"));
        }
    }

    // =====================================================================
    // ComAutomation: late-bound COM utilities for Office automation.
    //
    // Pattern adapted from the user's own 2htm.cs (Microsoft Office
    // late-bound interop): keep COM objects typed as `dynamic`
    // throughout, instantiate via Type.GetTypeFromProgID + Activator,
    // silence interactive alerts so unattended automation does not
    // stall on a dialog. Office is an optional dependency: DbDuo
    // proper runs without it; only Export-Data to xlsx, docx, and
    // filtered-HTML requires Excel and Word.
    // =====================================================================
    public static class ComAutomation
    {
        // Create an Office Application object by ProgID (e.g.,
        // "Word.Application", "Excel.Application"). Throws a clear
        // InvalidOperationException if the ProgID is not registered
        // (Office not installed), rather than a bare HRESULT.
        public static dynamic createApp(string sProgId)
        {
            Type oType = Type.GetTypeFromProgID(sProgId);
            if (oType == null)
            {
                throw new InvalidOperationException(
                    "Office component '" + sProgId + "' is not installed or not registered. "
                    + "Export-Data to xlsx, docx, or filtered HTML requires Microsoft Office.");
            }
            dynamic oApp = Activator.CreateInstance(oType);
            silenceAlerts(sProgId, oApp);
            return oApp;
        }

        // Note: DbDuo never attaches to a running Office instance.
        // We always CreateObject a fresh one and Quit() it when we
        // are done with the export. Attaching to a running instance
        // would risk:
        //   - mutating settings (DisplayAlerts, AutomationSecurity)
        //     that the user has tuned for their own session,
        //   - leaving SaveAs / Quit side effects on documents the
        //     user has open in their editor,
        //   - non-deterministic behavior depending on whether Word
        //     or Excel happens to be running.
        // The cost is starting a hidden process per export, which
        // is acceptable.

        // Silence on-screen alerts on a freshly-created Office app
        // so unattended automation doesn't stall on a dialog. Each
        // setter is wrapped in its own try/catch because not all
        // versions of Office expose every property. Pattern lifted
        // from 2htm.cs which the user provided as the reference
        // implementation.
        private static void silenceAlerts(string sProgId, dynamic oApp)
        {
            // mso constants. Hard-coded to avoid an Office interop
            // assembly reference at compile time.
            const int iMsoAutomationSecurityForceDisable = 3;
            const int iWdAlertsNone = 0;

            string sLower = (sProgId ?? "").ToLowerInvariant();
            if (sLower == "word.application")
            {
                try { oApp.DisplayAlerts = iWdAlertsNone; } catch { }
                try { oApp.Options.ConfirmConversions = false; } catch { }
                try { oApp.Options.DoNotPromptForConvert = true; } catch { }
                try { oApp.AutomationSecurity = iMsoAutomationSecurityForceDisable; } catch { }
                try { oApp.Visible = false; } catch { }
                try { oApp.ScreenUpdating = false; } catch { }
            }
            else if (sLower == "excel.application")
            {
                try { oApp.DisplayAlerts = false; } catch { }
                try { oApp.AskToUpdateLinks = false; } catch { }
                try { oApp.AlertBeforeOverwriting = false; } catch { }
                try { oApp.AutomationSecurity = iMsoAutomationSecurityForceDisable; } catch { }
                try { oApp.Visible = false; } catch { }
                try { oApp.ScreenUpdating = false; } catch { }
            }
            else
            {
                // Generic best-effort.
                try { oApp.DisplayAlerts = false; } catch { }
                try { oApp.Visible = false; } catch { }
            }
        }

        // Open a file with its default Windows application via
        // ShellExecute. Equivalent to dbDot/HomerLib's shellOpen
        // ("WScript.Shell".Run); used post-export to launch each
        // generated file in its associated viewer.
        public static void shellOpen(string sPath)
        {
            if (string.IsNullOrEmpty(sPath)) return;
            try
            {
                var oPsi = new System.Diagnostics.ProcessStartInfo();
                oPsi.FileName = sPath;
                oPsi.UseShellExecute = true;
                System.Diagnostics.Process.Start(oPsi);
            }
            catch { /* swallow: open is a convenience, not an error */ }
        }
    }

    // =====================================================================
    // DbDuoLog: lightweight runtime log. Truncated to empty at every
    // program startup so the file always reflects only the current
    // session. Records database opens, table switches, errors, and
    // significant state changes.
    //
    // Location: DbDuo.log next to the DbDuo.exe binary. If that
    // directory is not writable (e.g., Program Files install without
    // admin rights), falls back to %TEMP%\DbDuo.log.
    // =====================================================================
    public static class DbDuoLog
    {
        private static string sPath = "";
        private static readonly object oLock = new object();

        public static void init()
        {
            string sExeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? ".";
            string sCandidate = Path.Combine(sExeDir, "DbDuo.log");
            try
            {
                using (FileStream fs = File.Create(sCandidate)) { }
                sPath = sCandidate;
            }
            catch
            {
                // Fall back to TEMP if the exe directory is not writable.
                try
                {
                    sCandidate = Path.Combine(Path.GetTempPath(), "DbDuo.log");
                    using (FileStream fs = File.Create(sCandidate)) { }
                    sPath = sCandidate;
                }
                catch
                {
                    sPath = "";  // logging disabled
                    return;
                }
            }
            write("DbDuo log initialized at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            write("Executable: " + Application.ExecutablePath);
            write("Working directory: " + Environment.CurrentDirectory);
        }

        public static string getLogPath() { return sPath; }

        public static void write(string sMessage)
        {
            if (string.IsNullOrEmpty(sPath)) return;
            try
            {
                lock (oLock)
                {
                    File.AppendAllText(sPath,
                        DateTime.Now.ToString("HH:mm:ss") + "  " + sMessage + Environment.NewLine);
                }
            }
            catch { /* logging never throws to callers */ }
        }
    }

    // =====================================================================
    // SingleInstance: detect-or-launch helper, used by the desktop-shortcut
    // hotkey path (DbDuo.exe -activate).
    //
    // Mechanism:
    //   1. A named, session-scoped Mutex serves as the "is anyone already
    //      running?" sentinel. The first instance acquires it and holds it
    //      for the life of the process; later launches see it taken.
    //   2. A registered window message ("DbDuo.WakeUp") is broadcast by the
    //      second instance. The first instance's main form has a WndProc
    //      override that listens for the message and brings itself to the
    //      foreground when received.
    //   3. The second instance signals, then exits without creating a form
    //      or opening a database.
    //
    // Why a Mutex plus a broadcast message instead of FindWindow:
    //   - FindWindow looks up windows by title, which is fragile (the title
    //     can be hidden, minimized to tray, or differ across configurations).
    //     A registered window-message ID is system-wide unique to the
    //     registering string, robust against title changes, and survives
    //     window-state transitions.
    //   - The Mutex makes the "am I the first?" decision deterministic
    //     before we even need to enumerate windows. Without it, there is
    //     a race window where two near-simultaneous launches would both
    //     decide to activate-or-launch and both might end up as the
    //     "first."
    //
    // Scope:
    //   The Mutex name is "Local\\DbDuo.SingleInstance" (no Global\\
    //   prefix), which scopes it to the current Windows user session.
    //   Multiple users on the same machine each get their own first
    //   instance. Multiple Remote Desktop sessions of the same user
    //   each get their own first instance.
    // =====================================================================
    public static class SingleInstance
    {
        private const string MutexName = "Local\\DbDuo.SingleInstance";
        public const string WakeUpMessageName = "DbDuo.WakeUp";

        // The cached message ID, looked up once via RegisterWindowMessage.
        // Both the broadcaster (second instance) and the listener (first
        // instance, in DbDuoForm.WndProc) call RegisterWindowMessage with
        // the same string and get back the same numeric ID. Windows
        // guarantees this; the ID is unique per logon session.
        public static uint WakeUpMessage { get { return registerOnce(); } }
        private static uint iCachedMessage;
        private static uint registerOnce()
        {
            if (iCachedMessage == 0) iCachedMessage = RegisterWindowMessageW(WakeUpMessageName);
            return iCachedMessage;
        }

        // Keep the mutex alive for the process lifetime by storing it in a
        // static field. Without this reference, the GC may collect it
        // before the program exits, which would release the mutex early.
        private static System.Threading.Mutex oMutex;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint RegisterWindowMessageW(string sName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint iMsg, IntPtr wParam, IntPtr lParam);

        // HWND_BROADCAST = (HWND)0xFFFF, sends to every top-level window.
        // This is how the second instance reaches the first without
        // knowing the first's window handle.
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);

        // Try to acquire the single-instance mutex. Returns true if this
        // process is the first instance (and now owns the mutex). The
        // mutex stays owned for the rest of the process lifetime; no
        // matching release call is needed.
        public static bool tryAcquire()
        {
            try
            {
                bool bCreatedNew;
                oMutex = new System.Threading.Mutex(true, MutexName, out bCreatedNew);
                if (!bCreatedNew)
                {
                    // Another instance owns the mutex. Drop our reference
                    // so it doesn't linger on the GC's heap.
                    try { oMutex.Close(); } catch { }
                    oMutex = null;
                    return false;
                }
                return true;
            }
            catch
            {
                // If anything goes wrong with the mutex (e.g., the named
                // kernel object is in a weird state), fall back to
                // "behave as first instance." Worst case: two instances
                // open. Better than refusing to launch at all.
                return true;
            }
        }

        // Send the wake-up message to all top-level windows. The first
        // instance's main form will recognize it and bring itself forward;
        // every other window ignores it. Returns true on success.
        public static bool wakeUpFirstInstance()
        {
            try
            {
                return PostMessage(HWND_BROADCAST, WakeUpMessage, IntPtr.Zero, IntPtr.Zero);
            }
            catch { return false; }
        }
    }

    // =====================================================================
    // Program: entry point with command-line argument parsing.
    //
    // Supported argument forms (mirroring dbDot.vbs and adding a few):
    //   DbDuo.exe                            -- empty form
    //   DbDuo.exe path                       -- open file (extension dispatch)
    //   DbDuo.exe path table                 -- open file and switch to table
    //   DbDuo.exe -cli path                  -- open and drop into dot prompt
    //   DbDuo.exe -readonly path             -- open in read-only mode
    //   DbDuo.exe -activate                  -- bring existing instance to
    //                                           the foreground if one is
    //                                           running, else launch fresh
    //                                           (used by the desktop hotkey)
    //
    // Folder paths are treated as dBASE sources per dbDot's convention.
    // Relative paths are resolved against the current working directory
    // by openDatabase().
    // =====================================================================
    public static class Program
    {
        // ATTACH_PARENT_PROCESS: per Microsoft docs, when AttachConsole
        // is called with this special pseudo-PID it attaches the calling
        // (Windows-subsystem) process to the console of its launching
        // process. This is the standard pattern for hybrid CLI/GUI
        // Windows programs: built with /SUBSYSTEM:WINDOWS so double-click
        // launches don't show a console, but able to write back to
        // cmd.exe when launched from a terminal.
        private const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        // uiMode values, parsed from DbDuo.ini [General] uiMode= or
        // overridden by command-line flags -cli / -gui / -both.
        public enum UiMode
        {
            Both = 0,    // GUI window with integrated dot-prompt panel (default)
            Gui  = 1,    // GUI window only, no dot-prompt panel
            Cli  = 2,    // pure dot-prompt CLI, no GUI form
        }

        // Read uiMode from DbDuo.ini [General] section. Default is Both.
        // Returns the parsed enum value.
        private static UiMode readUiModeFromIni()
        {
            string sIniPath = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
                "DbDuo.ini");
            if (!File.Exists(sIniPath)) return UiMode.Both;

            string[] aLines = null;
            try { aLines = File.ReadAllLines(sIniPath); } catch { return UiMode.Both; }

            bool bInGeneral = false;
            foreach (string sLine in aLines)
            {
                string sTrim = sLine.Trim();
                if (sTrim.Length == 0) continue;
                if (sTrim.StartsWith(";") || sTrim.StartsWith("#")) continue;
                if (sTrim.StartsWith("["))
                {
                    bInGeneral = sTrim.Equals("[General]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (!bInGeneral) continue;
                int iEq = sTrim.IndexOf('=');
                if (iEq <= 0) continue;
                string sKey = sTrim.Substring(0, iEq).Trim();
                string sValue = sTrim.Substring(iEq + 1).Trim();
                if (Str.equiv(sKey, "uiMode"))
                {
                    if      (Str.equiv(sValue, "cli"))  return UiMode.Cli;
                    else if (Str.equiv(sValue, "gui"))  return UiMode.Gui;
                    else if (Str.equiv(sValue, "both")) return UiMode.Both;
                }
            }
            return UiMode.Both;
        }

        [STAThread]
        public static int Main(string[] aArgs)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                DbDuoLog.init();
                DbDuoLog.write("Command line: " + string.Join(" ", aArgs));

                string sFile = null;
                string sTable = null;
                bool bReadOnly = false;
                bool bActivate = false;
                UiMode oMode = readUiModeFromIni();
                bool bModeFromCli = false;
                foreach (string sArg in aArgs)
                {
                    if (sArg.Equals("-cli", StringComparison.OrdinalIgnoreCase) ||
                        sArg.Equals("/cli", StringComparison.OrdinalIgnoreCase))
                    { oMode = UiMode.Cli; bModeFromCli = true; }
                    else if (sArg.Equals("-gui", StringComparison.OrdinalIgnoreCase) ||
                             sArg.Equals("/gui", StringComparison.OrdinalIgnoreCase))
                    { oMode = UiMode.Gui; bModeFromCli = true; }
                    else if (sArg.Equals("-both", StringComparison.OrdinalIgnoreCase) ||
                             sArg.Equals("/both", StringComparison.OrdinalIgnoreCase))
                    { oMode = UiMode.Both; bModeFromCli = true; }
                    else if (sArg.Equals("-readonly", StringComparison.OrdinalIgnoreCase) ||
                             sArg.Equals("/readonly", StringComparison.OrdinalIgnoreCase) ||
                             sArg.Equals("-r", StringComparison.OrdinalIgnoreCase))
                        bReadOnly = true;
                    else if (sArg.Equals("-activate", StringComparison.OrdinalIgnoreCase) ||
                             sArg.Equals("/activate", StringComparison.OrdinalIgnoreCase))
                        bActivate = true;
                    else if (sFile == null) sFile = sArg;
                    else if (sTable == null) sTable = sArg;
                }
                DbDuoLog.write("uiMode: " + oMode + (bModeFromCli ? " (from command line)" : " (from DbDuo.ini)"));

                // ----- Single-instance handoff: -activate flag.
                //
                // The desktop-shortcut hotkey passes -activate. Behavior:
                //   - If another DbDuo instance is already running, send it
                //     a wake-up message and exit. The first instance brings
                //     itself to the foreground (handled in DbDuoForm.WndProc).
                //   - If no other instance is running, fall through to a
                //     normal launch. The first instance acquires the
                //     single-instance mutex and holds it for its lifetime.
                //
                // Without -activate, every launch is independent: starts a
                // fresh process, doesn't touch the mutex, doesn't interact
                // with any running instance. This preserves the ability to
                // open multiple databases in separate processes from the
                // command line.
                if (bActivate)
                {
                    if (!SingleInstance.tryAcquire())
                    {
                        DbDuoLog.write("-activate: another instance is running; sending wake-up.");
                        SingleInstance.wakeUpFirstInstance();
                        return 0;
                    }
                    DbDuoLog.write("-activate: no existing instance; launching as first instance.");
                }

                // ----- CLI-only mode: no GUI form, attach parent console
                if (oMode == UiMode.Cli)
                {
                    return runCliOnly(sFile, sTable, bReadOnly);
                }

                // ----- GUI mode (with or without integrated prompt panel)
                DbDuoForm oForm = new DbDuoForm(oMode);
                DbDuoLog.write("Form created (uiMode=" + oMode + ").");

                if (!string.IsNullOrEmpty(sFile))
                {
                    try
                    {
                        DbDuoLog.write("Opening database: " + sFile + (sTable != null ? " (table: " + sTable + ")" : "") + (bReadOnly ? " [read-only]" : ""));
                        oForm.Db.openDatabase(sFile, sTable, bReadOnly);
                        if (!oForm.Db.hasRecordset())
                        {
                            // Prefer base tables; fall back to views.
                            List<string> lT = oForm.Db.getTableNames();
                            if (lT.Count == 0) lT = oForm.Db.getViewNames();
                            if (lT.Count > 0)
                            {
                                oForm.Db.selectTable(lT[0]);
                                DbDuoLog.write("Auto-selected: " + lT[0]);
                            }
                        }
                        oForm.invokeRefresh();
                        DbDuoLog.write("Database opened successfully. Connect string: " + oForm.Db.connectString);
                    }
                    catch (Exception oEx)
                    {
                        DbDuoLog.write("Open failed: " + oEx.Message);
                        MessageBox.Show(oForm,
                            "Could not open " + sFile + ":\n\n" + oEx.Message,
                            "DbDuo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    // No command-line file specified: try to restore the
                    // last database and table from DbDuo.ini's [Session]
                    // section. Saved on Open-Database, Select-Table, and
                    // Close-Database. If the saved path no longer exists
                    // (file moved or deleted), the restore is skipped
                    // silently; the user starts with no database open.
                    string sSavedDb = DbDuoForm.IniSession.lastDatabase;
                    string sSavedTbl = DbDuoForm.IniSession.lastTable;
                    if (!string.IsNullOrEmpty(sSavedDb) && System.IO.File.Exists(sSavedDb))
                    {
                        try
                        {
                            DbDuoLog.write("Restoring last session: " + sSavedDb
                                + (string.IsNullOrEmpty(sSavedTbl) ? "" : " (table: " + sSavedTbl + ")"));
                            oForm.Db.openDatabase(sSavedDb,
                                string.IsNullOrEmpty(sSavedTbl) ? null : sSavedTbl,
                                bReadOnly);
                            if (!oForm.Db.hasRecordset())
                            {
                                List<string> lT = oForm.Db.getTableNames();
                                if (lT.Count == 0) lT = oForm.Db.getViewNames();
                                if (lT.Count > 0) oForm.Db.selectTable(lT[0]);
                            }
                            oForm.invokeRefresh();
                            DbDuoLog.write("Session restored.");
                        }
                        catch (Exception oEx)
                        {
                            DbDuoLog.write("Session restore failed: " + oEx.Message);
                            // Don't show a dialog -- the user didn't ask
                            // to open this file; we did. Just log and
                            // start empty.
                        }
                    }
                }

                DbDuoLog.write("Entering Application.Run message loop.");
                // uiMode=both: spawn the dot-prompt console window in
                // addition to the GUI form. The console runs on a worker
                // thread that owns its AllocConsole-allocated console;
                // the GUI runs on the main message loop. Both share
                // oForm.Db (the single DbDuoManager). User can Alt+Tab
                // between the two windows.
                if (oMode == UiMode.Both)
                {
                    DbDuoLog.write("uiMode=both: spawning dot-prompt console window.");
                    DotPromptHost.enter(oForm);
                }
                Application.Run(oForm);
                DbDuoLog.write("Application.Run returned. Exiting normally.");
                return 0;
            }
            catch (Exception oEx)
            {
                try { DbDuoLog.write("FATAL: " + oEx.Message + " | " + oEx.StackTrace); } catch { }
                MessageBox.Show("Fatal error:\n\n" + oEx.Message + "\n\n" + oEx.StackTrace,
                    "DbDuo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        // CLI-only entry: no DbDuoForm created. Attach the launching
        // terminal's console (or allocate one if launched detached) and
        // run the dot-prompt loop on the main thread.
        //
        // The DbDuoManager and the DotPromptHost together do everything
        // the GUI provides except the visual grid -- including database
        // open, table select, record edit, sort/filter, and SQL.
        private static int runCliOnly(string sFile, string sTable, bool bReadOnly)
        {
            // Try to attach to the parent process's console (the cmd.exe
            // window that launched us). If that fails we were launched
            // detached (e.g., from Explorer with a -cli flag) so we
            // allocate a new console.
            bool bAttached = AttachConsole(ATTACH_PARENT_PROCESS);
            if (!bAttached) AllocConsole();

            // Re-bind the .NET Console class to the (newly attached or
            // newly allocated) console handles; without this, the first
            // Console.WriteLine after AttachConsole goes to the bit
            // bucket because Console caches its handles at first use.
            try
            {
                System.IO.Stream oOut = Console.OpenStandardOutput();
                System.IO.StreamWriter oWriter = new System.IO.StreamWriter(oOut) { AutoFlush = true };
                Console.SetOut(oWriter);
                System.IO.Stream oIn = Console.OpenStandardInput();
                System.IO.StreamReader oReader = new System.IO.StreamReader(oIn);
                Console.SetIn(oReader);
            }
            catch { }

            DbDuoLog.write("CLI-only mode: console " + (bAttached ? "attached" : "allocated"));

            // Open the database directly through a free DbDuoManager (no
            // form). DotPromptHost.runStandalone takes a manager and
            // runs the loop until the user quits.
            using (DbDuoManager oMgr = new DbDuoManager())
            {
                if (!string.IsNullOrEmpty(sFile))
                {
                    try
                    {
                        oMgr.openDatabase(sFile, sTable, bReadOnly);
                        if (!oMgr.hasRecordset())
                        {
                            List<string> lT = oMgr.getTableNames();
                            if (lT.Count == 0) lT = oMgr.getViewNames();
                            if (lT.Count > 0) oMgr.selectTable(lT[0]);
                        }
                    }
                    catch (Exception oEx)
                    {
                        Console.WriteLine("Could not open " + sFile + ": " + oEx.Message);
                    }
                }

                DotPromptHost.runStandalone(oMgr);
            }

            try { FreeConsole(); } catch { }
            return 0;
        }
    }
}
