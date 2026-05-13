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
        public const string VersionString = "1.0.51";
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
        //   2. NVDA:     nvdaControllerClient.dll P/Invoke. The DLL
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
            // Extra-Speech gate: when off, DbDuo's direct speech is
            // suppressed but the screen reader's natural focus and
            // selection announcements still occur. The flag is toggled
            // by Toggle-Extra-Speech (Alt+Shift+S) and persisted to
            // [General] extraSpeech in DbDuo.ini. The toggle command
            // itself uses sayForced so the user always hears their
            // own action confirmed regardless of the flag's state.
            if (!bExtraSpeechEnabled) return;
            sayForced(sText);
        }

        // sayForced: bypass the Extra-Speech gate. Used for two
        // narrow purposes: the Toggle-Extra-Speech command's own
        // confirmation (so the user hears "extra speech off" when
        // turning it off), and the Test-Reader command (which must
        // produce speech to be diagnostic).
        public static void sayForced(string sText)
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

        // Extra-Speech enabled flag. Public mutable so the toggle
        // command can flip it. Loaded from DbDuo.ini at startup;
        // default true. Persisted on change.
        public static bool bExtraSpeechEnabled = true;

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
            oSb.AppendLine("nvdaControllerClient.dll loadable: " + (bNvdaDll ? "yes" : "no (drop the DLL next to DbDuo.exe to enable NVDA support)"));
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
        // Starting in NVDA 2026.1 the DLL is named simply
        // nvdaControllerClient.dll (no architecture suffix); earlier
        // releases shipped it as nvdaControllerClient64.dll for x64
        // hosts and nvdaControllerClient32.dll for x86. NVDA's own
        // current C# example uses the unsuffixed name and that is
        // what the DbDuo build script downloads and bundles. Place
        // the DLL next to DbDuo.exe and the DllImport finds it via
        // the standard Windows DLL search order. If the DLL is
        // missing, DllNotFoundException is caught silently and the
        // NVDA path is unavailable.
        //
        // testIfRunning returns 0 when NVDA is running, non-zero
        // otherwise (it's a Windows error code). speakText takes
        // a wide-char string and returns 0 on success.
        [DllImport("nvdaControllerClient.dll", CharSet = CharSet.Unicode, EntryPoint = "nvdaController_testIfRunning")]
        private static extern int nvdaController_testIfRunning();
        [DllImport("nvdaControllerClient.dll", CharSet = CharSet.Unicode, EntryPoint = "nvdaController_speakText")]
        private static extern int nvdaController_speakText([MarshalAs(UnmanagedType.LPWStr)] string sText);
        [DllImport("nvdaControllerClient.dll", CharSet = CharSet.Unicode, EntryPoint = "nvdaController_cancelSpeech")]
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
    // JawsSettingsInstaller: copy DbDuo.jkm and DbDuo.jss into every
    // installed JAWS user-settings folder and run scompile.exe to
    // produce DbDuo.jsb there. The Pascal-Script equivalent that
    // shipped with v1.0.39 worked but lived inside DbDuo_setup.iss;
    // moving it to C# lets the user re-trigger it later without
    // re-running the full installer, and consolidates the JAWS-
    // version-discovery logic in one place.
    //
    // Invoked two ways:
    //   - From the installer's [Run] section as
    //     `DbDuo.exe --install-jaws-settings`, which runs the install
    //     and exits without launching the GUI.
    //   - From the Help menu's "Install JAWS settings" command, which
    //     re-runs the install (for users who upgraded JAWS to a new
    //     year-version after installing DbDuo).
    //
    // Returns a multi-line report of what was done. Caller chooses
    // whether to show it (the menu version pops a dialog; the CLI
    // version prints it).
    // =====================================================================
    public static class JawsSettingsInstaller
    {
        // Find scompile.exe for a given JAWS year-version. Tries the
        // registry value HKLM\Software\Freedom Scientific\JAWS\<ver>\
        // Target first, then falls back to {pf}\Freedom Scientific\
        // JAWS\<ver>\scompile.exe.
        private static string findScompilePath(string sVersion)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey oKey = Microsoft.Win32.Registry.LocalMachine
                    .OpenSubKey(@"Software\Freedom Scientific\JAWS\" + sVersion))
                {
                    if (oKey != null)
                    {
                        string sTarget = oKey.GetValue("Target") as string;
                        if (!string.IsNullOrEmpty(sTarget))
                        {
                            string sCompile = System.IO.Path.Combine(sTarget, "scompile.exe");
                            if (System.IO.File.Exists(sCompile)) return sCompile;
                        }
                    }
                }
            }
            catch { /* registry access can fail under low privilege; fall through */ }

            string sPf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string sFallback = System.IO.Path.Combine(sPf,
                @"Freedom Scientific\JAWS\" + sVersion + @"\scompile.exe");
            if (System.IO.File.Exists(sFallback)) return sFallback;
            return null;
        }

        // Run the install. Returns a human-readable report and, via
        // the iCopied / iCompiled out-parameters, totals the caller
        // can use for status text. Records every path placed in a
        // log under %APPDATA%\DbDuo\jawsSettings.log so the matching
        // uninstall path can remove exactly those files.
        public static string install(string sAppFolder, out int iCopied, out int iCompiled)
        {
            iCopied = 0;
            iCompiled = 0;
            System.Text.StringBuilder oSb = new System.Text.StringBuilder();
            System.Collections.Generic.List<string> lLog = new System.Collections.Generic.List<string>();

            string sJawsRoot = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Freedom Scientific\JAWS");
            if (!System.IO.Directory.Exists(sJawsRoot))
            {
                oSb.AppendLine("JAWS does not appear to be installed for the current user.");
                oSb.AppendLine("(No folder at " + sJawsRoot + ")");
                return oSb.ToString();
            }

            string sJkmSource = System.IO.Path.Combine(sAppFolder, "DbDuo.jkm");
            string sJssSource = System.IO.Path.Combine(sAppFolder, "DbDuo.jss");
            if (!System.IO.File.Exists(sJkmSource))
            {
                oSb.AppendLine("DbDuo.jkm not found in " + sAppFolder + ".");
                return oSb.ToString();
            }
            if (!System.IO.File.Exists(sJssSource))
            {
                oSb.AppendLine("DbDuo.jss not found in " + sAppFolder + ".");
                return oSb.ToString();
            }

            foreach (string sVersionPath in System.IO.Directory.GetDirectories(sJawsRoot))
            {
                string sVersion = System.IO.Path.GetFileName(sVersionPath);
                string sSettingsPath = System.IO.Path.Combine(sVersionPath, "Settings");
                if (!System.IO.Directory.Exists(sSettingsPath)) continue;
                string sScompile = findScompilePath(sVersion);

                foreach (string sLangPath in System.IO.Directory.GetDirectories(sSettingsPath))
                {
                    string sLang = System.IO.Path.GetFileName(sLangPath);
                    string sJkmTarget = System.IO.Path.Combine(sLangPath, "DbDuo.jkm");
                    string sJssTarget = System.IO.Path.Combine(sLangPath, "DbDuo.jss");
                    string sJsbTarget = System.IO.Path.Combine(sLangPath, "DbDuo.jsb");

                    bool bJkmOk = false, bJssOk = false, bJsbOk = false;
                    try { System.IO.File.Copy(sJkmSource, sJkmTarget, true); bJkmOk = true; iCopied++; lLog.Add(sJkmTarget); }
                    catch (Exception ex) { oSb.AppendLine("FAIL: copy jkm to " + sJkmTarget + ": " + ex.Message); }
                    try { System.IO.File.Copy(sJssSource, sJssTarget, true); bJssOk = true; iCopied++; lLog.Add(sJssTarget); }
                    catch (Exception ex) { oSb.AppendLine("FAIL: copy jss to " + sJssTarget + ": " + ex.Message); }

                    if (bJssOk && !string.IsNullOrEmpty(sScompile))
                    {
                        try
                        {
                            System.Diagnostics.ProcessStartInfo oPsi =
                                new System.Diagnostics.ProcessStartInfo(sScompile, "DbDuo.jss");
                            oPsi.WorkingDirectory = sLangPath;
                            oPsi.UseShellExecute = false;
                            oPsi.CreateNoWindow = true;
                            oPsi.RedirectStandardOutput = true;
                            oPsi.RedirectStandardError = true;
                            using (System.Diagnostics.Process oProc = System.Diagnostics.Process.Start(oPsi))
                            {
                                oProc.WaitForExit(10000);
                                if (oProc.HasExited && oProc.ExitCode == 0 && System.IO.File.Exists(sJsbTarget))
                                { bJsbOk = true; iCompiled++; lLog.Add(sJsbTarget); }
                                else
                                {
                                    string sErr = oProc.HasExited ? oProc.StandardError.ReadToEnd() : "(timed out)";
                                    oSb.AppendLine("FAIL: compile " + sJsbTarget + " - " + sErr.Trim());
                                }
                            }
                        }
                        catch (Exception ex) { oSb.AppendLine("FAIL: compile " + sJsbTarget + ": " + ex.Message); }
                    }
                    else if (bJssOk && string.IsNullOrEmpty(sScompile))
                    {
                        oSb.AppendLine("WARN: scompile.exe not found for JAWS " + sVersion
                            + "; placed jss but did not compile (run scompile manually).");
                    }

                    oSb.AppendLine("JAWS " + sVersion + " / " + sLang + ": "
                        + (bJkmOk ? "jkm " : "no-jkm ")
                        + (bJssOk ? "jss " : "no-jss ")
                        + (bJsbOk ? "jsb" : "no-jsb"));
                }
            }

            // Persist the log so the matching uninstall step can
            // remove exactly the files we placed.
            try
            {
                string sLogPath = getLogPath();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(sLogPath));
                System.IO.File.WriteAllLines(sLogPath, lLog);
            }
            catch (Exception ex) { oSb.AppendLine("WARN: could not write log: " + ex.Message); }

            return oSb.ToString();
        }

        // Uninstall: read the log, delete each path listed, then
        // delete the log itself. Mirrors what the Pascal Script
        // CurUninstallStepChanged did in v1.0.39.
        public static string uninstall(out int iDeleted)
        {
            iDeleted = 0;
            System.Text.StringBuilder oSb = new System.Text.StringBuilder();
            string sLogPath = getLogPath();
            if (!System.IO.File.Exists(sLogPath))
            {
                oSb.AppendLine("No jawsSettings.log found; nothing to remove.");
                return oSb.ToString();
            }
            try
            {
                string[] aPaths = System.IO.File.ReadAllLines(sLogPath);
                foreach (string sPath in aPaths)
                {
                    if (string.IsNullOrWhiteSpace(sPath)) continue;
                    try
                    {
                        if (System.IO.File.Exists(sPath))
                        {
                            System.IO.File.Delete(sPath);
                            iDeleted++;
                            oSb.AppendLine("removed " + sPath);
                        }
                    }
                    catch (Exception ex) { oSb.AppendLine("FAIL: " + sPath + ": " + ex.Message); }
                }
                try { System.IO.File.Delete(sLogPath); } catch { /* tolerate */ }
                try { System.IO.Directory.Delete(System.IO.Path.GetDirectoryName(sLogPath), false); }
                catch { /* only succeeds if empty; ok either way */ }
            }
            catch (Exception ex) { oSb.AppendLine("FAIL: read log: " + ex.Message); }
            return oSb.ToString();
        }

        private static string getLogPath()
        {
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"DbDuo\jawsSettings.log");
        }
    }

    // =====================================================================
    // SnippetHelper: file-system support for DbDuo's Invoke / Edit
    // Snippet commands. Modeled on EdSharp's snippet folder pattern.
    // Snippets live as plain files under %APPDATA%\DbDuo\Snippets\.
    // Files ending in .js are executed as JScript .NET via
    // DbDuoScripting.JS.Eval (in the separately-compiled dbDuoEval.dll);
    // all other extensions are treated as plain text and displayed in
    // a standard MessageBox as reference material.
    //
    // No custom script editor. The user edits snippets in their own
    // preferred editor (Notepad by default; configurable in DbDuo.ini
    // under [Snippets] editor=path-to-editor). The Pick dialog for
    // choosing a snippet to invoke or edit reuses the existing
    // LbcDialog used throughout DbDuo (Choose Table, Recent Files,
    // etc.).
    //
    // (Note on EdSharp's "Save Snippet" command: it captured selected
    // or whole-document text from the current editor view and wrote
    // it to a snippet file. DbDuo is not a text editor, so there is
    // no analogous "current selection" to capture. The Save Snippet
    // command from v1.0.44 dev preview is dropped here in favor of
    // Edit Snippet covering the new-file case too -- the user picks
    // a "[New snippet...]" entry in the pick list when no existing
    // file is the target.)
    // =====================================================================
    public static class SnippetHelper
    {
        // Snippet folder under %APPDATA%\DbDuo\Snippets. Created on
        // first access. Path is stable across DbDuo upgrades because
        // it lives under the user's roaming application data, not
        // the install folder.
        public static string getSnippetDir()
        {
            string sDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"DbDuo\Snippets");
            try { System.IO.Directory.CreateDirectory(sDir); }
            catch { /* tolerate; the caller will surface the error */ }
            return sDir;
        }

        // Editor command. The optional [Snippets] editor= setting in
        // the per-user DbDuo.ini overrides; default is Notepad on
        // PATH. Returns the editor executable path or "notepad" if
        // no override.
        //
        // IniSession lives nested inside DbDuoForm (where most ini
        // access happens) so we reach it via the qualified name.
        // No new helper class is introduced for one call site.
        public static string getEditorCommand()
        {
            string sIniEditor = DbDuoForm.IniSession.read("Snippets", "editor");
            if (!string.IsNullOrEmpty(sIniEditor)) return sIniEditor;
            return "notepad";
        }

        // List the snippet files, sorted alphabetically by name (not
        // by full path). Returns the bare filenames, suitable for an
        // LbcDialog presentation.
        //
        // Prefix note: dirInfo / fileInfos here use the lower-camel
        // class-name prefix per Camel Type for non-COM .NET objects;
        // they are NOT prefixed "o" (which is for COM/Variant types).
        public static string[] listSnippets()
        {
            string sDir = getSnippetDir();
            if (!System.IO.Directory.Exists(sDir)) return new string[0];
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(sDir);
            System.IO.FileInfo[] aFileInfos = dirInfo.GetFiles("*.*");
            string[] aNames = new string[aFileInfos.Length];
            for (int i = 0; i < aFileInfos.Length; i++) aNames[i] = aFileInfos[i].Name;
            Array.Sort(aNames, StringComparer.OrdinalIgnoreCase);
            return aNames;
        }

        // Open the user's editor on a snippet file path. The file is
        // created (touched) if it does not exist so the editor opens
        // a real path rather than failing on a "file not found" prompt.
        // Returns true on success.
        public static bool openInEditor(string sFilePath)
        {
            try
            {
                if (!System.IO.File.Exists(sFilePath))
                {
                    System.IO.File.WriteAllText(sFilePath, "");
                }
                System.Diagnostics.ProcessStartInfo psi =
                    new System.Diagnostics.ProcessStartInfo(getEditorCommand(), "\"" + sFilePath + "\"");
                psi.UseShellExecute = false;
                System.Diagnostics.Process.Start(psi);
                return true;
            }
            catch { return false; }
        }

        // Run a snippet. For .js files we load the file content and
        // call DbDuoScripting.JS.Eval via reflection (so DbDuo.cs does
        // not need a compile-time reference to dbDuoEval.dll's types --
        // the DLL is referenced at csc.exe compile time via
        // /reference: but we route the actual call through reflection
        // for symmetry with how the ADODB late-bound dispatch works
        // elsewhere in DbDuo). For non-.js files we read the file and
        // return its contents for display as reference text.
        //
        // The frm and db parameters are typed as object because at
        // this layer we don't need the static types -- the JScript
        // engine takes them as Object[] anyway. frm IS a DbDuoForm
        // and db IS a DbDuoManager at the call site.
        //
        // Returns the result string (script output, or file contents
        // for non-.js files, or "ERROR: ..." on failure).
        public static string runSnippet(string sFilePath, object frm, object db)
        {
            string sExt = System.IO.Path.GetExtension(sFilePath).ToLowerInvariant();
            string sBody;
            try { sBody = System.IO.File.ReadAllText(sFilePath); }
            catch (Exception ex) { return "ERROR: could not read " + sFilePath + ": " + ex.Message; }

            if (sExt != ".js")
            {
                // Non-script snippet: just return the file contents
                // so the caller can display them. Useful for canned
                // SQL fragments, templated row data, reference notes.
                return sBody;
            }

            // .js -- invoke DbDuoScripting.JS.Eval via reflection so
            // we don't take a compile-time dependency on the JScript
            // assembly's namespace from C#.
            try
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.Load("dbDuoEval");
                Type jsType = asm.GetType("DbDuoScripting.JS");
                if (jsType == null) return "ERROR: DbDuoScripting.JS type not found in dbDuoEval.dll";
                System.Reflection.MethodInfo methodInfo = jsType.GetMethod("Eval",
                    new Type[] { typeof(string), typeof(object), typeof(object) });
                if (methodInfo == null) return "ERROR: DbDuoScripting.JS.Eval(string, object, object) not found";
                object result = methodInfo.Invoke(null, new object[] { sBody, frm, db });
                return result != null ? result.ToString() : "";
            }
            catch (Exception ex) { return "ERROR: " + ex.Message; }
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
        // equivalent. Used for long descriptive text in Show-Object.
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
    //   3. Show-Object groups them in a separate "metadata" section at
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
        // Used by Show-Object and the Edit dialogs when displaying
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

            // Auto-create recommended indexes on SQLite databases:
            // every foreign-key column (any column whose name ends
            // in '_id' and is not the table's own primary key) and
            // every 'marked' column gets a CREATE INDEX IF NOT
            // EXISTS. This benefits Show-Object's related-records
            // lookups (which issue SELECT look FROM <child> WHERE
            // <fk> = <value>) and any filter on 'marked = true'.
            //
            // Skipped on read-only opens, on file formats other than
            // SQLite (Access manages indexes through ADOX; CSV and
            // dBASE don't index at all), and (silently) on any error
            // so a bad sweep can't break the open.
            if (!bReadOnlyFlag && (sExt == "db" || sExt == "sqlite" || sExt == "sqlite3"))
            {
                try { ensureRecommendedIndexes(); }
                catch (Exception oExIdx)
                { try { DbDuoLog.write("ensureRecommendedIndexes failed: " + oExIdx.Message); } catch { } }
            }

            if (!string.IsNullOrEmpty(sTable))
                selectTable(sTable);
        }

        // ensureRecommendedIndexes: for every base table in the
        // currently-open SQLite database, issue CREATE INDEX IF
        // NOT EXISTS on each foreign-key column (any column whose
        // name ends in '_id' but is not the table's own primary key)
        // and on any column named 'marked'. The IF NOT EXISTS guard
        // means re-opening a database is idempotent -- only the
        // first open of a database adds indexes; subsequent opens
        // do nothing observable.
        //
        // The naming convention is idx_<table>_<column>. SQLite
        // requires index names unique across the whole database,
        // so encoding both table and column in the name avoids
        // collisions between tables that share a column name like
        // 'marked'.
        //
        // The walk uses sqlite_master to enumerate tables, then
        // PRAGMA table_info for each to discover columns. Both
        // queries return ADODB recordsets in this DbDuo path.
        // INDEX creation goes through oConn.Execute directly,
        // not a recordset.
        //
        // Every created index is logged to DbDuoLog so the user
        // can see what changed.
        private void ensureRecommendedIndexes()
        {
            if (oConn == null || ((int)oConn.State) == 0) return;

            // Step 1: list all base tables.
            List<string> lTables = new List<string>();
            dynamic oRs = null;
            try
            {
                oRs = createComObject("ADODB.Recordset");
                oRs.Open("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'",
                    oConn, 0 /* adOpenForwardOnly */, 1 /* adLockReadOnly */, 1 /* adCmdText */);
                while (!(bool)oRs.EOF)
                {
                    object oN = oRs.Fields["name"].Value;
                    if (oN != null && oN != DBNull.Value) lTables.Add(oN.ToString());
                    oRs.MoveNext();
                }
                try { oRs.Close(); } catch { }
            }
            finally { releaseCom(oRs); oRs = null; }

            // Step 2: for each table, scan columns and create
            // missing indexes. The PK-column name pattern is
            // <table>_id; we skip indexes on that one since
            // SQLite already maintains a rowid alias index.
            foreach (string sTable in lTables)
            {
                string sPk = sTable + Metadata.PrimaryKeySuffix;
                List<string> lColumns = new List<string>();
                dynamic oCols = null;
                try
                {
                    oCols = createComObject("ADODB.Recordset");
                    string sQuoted = "\"" + sTable.Replace("\"", "\"\"") + "\"";
                    oCols.Open("PRAGMA table_info(" + sQuoted + ")",
                        oConn, 0, 1, 1);
                    while (!(bool)oCols.EOF)
                    {
                        object oN = oCols.Fields["name"].Value;
                        if (oN != null && oN != DBNull.Value) lColumns.Add(oN.ToString());
                        oCols.MoveNext();
                    }
                    try { oCols.Close(); } catch { }
                }
                catch { /* table may be inaccessible; skip */ }
                finally { releaseCom(oCols); oCols = null; }

                foreach (string sCol in lColumns)
                {
                    bool bIsFk = sCol.EndsWith("_id", StringComparison.OrdinalIgnoreCase)
                                 && !string.Equals(sCol, sPk, StringComparison.OrdinalIgnoreCase)
                                 && !string.Equals(sCol, "id", StringComparison.OrdinalIgnoreCase);
                    bool bIsMarked = string.Equals(sCol, Metadata.MarkedColumn,
                                                    StringComparison.OrdinalIgnoreCase);
                    if (!bIsFk && !bIsMarked) continue;

                    string sIdxName = "idx_" + sTable + "_" + sCol;
                    string sSql = "CREATE INDEX IF NOT EXISTS \""
                                + sIdxName.Replace("\"", "\"\"") + "\" ON \""
                                + sTable.Replace("\"", "\"\"") + "\" (\""
                                + sCol.Replace("\"", "\"\"") + "\")";
                    try
                    {
                        oConn.Execute(sSql, Type.Missing,
                            128 /* adExecuteNoRecords */ | 1 /* adCmdText */);
                        try { DbDuoLog.write("ensureRecommendedIndexes: " + sSql); } catch { }
                    }
                    catch (Exception oExSql)
                    {
                        try { DbDuoLog.write("ensureRecommendedIndexes skip " + sIdxName + ": " + oExSql.Message); } catch { }
                    }
                }
            }
        }


        // =====================================================================
        // buildConnectString: per-extension connection-string assembly.
        // Mirrors dbDot.vbs lines 869-914 in spirit; the hard-coded
        // strings are now fallbacks behind a [ConnectStrings] section
        // in DbDuo.ini that lets advanced users override on a per-
        // extension basis. The ini template substitutes {path} with
        // the full file path and {folder} with the parent folder; the
        // ini code never escapes characters in {path}, so the user
        // is responsible for any quoting that the connection string
        // syntax requires (the templates that ship use the right
        // quoting form for each provider).
        // =====================================================================
        private string buildConnectString(string sPath, string sExt, bool bIsFolder, ref string sTable)
        {
            // First check the [ConnectStrings] section of the per-user
            // DbDuo.ini for an override. Empty / missing value falls
            // through to the hard-coded defaults below.
            string sOverride = readConnectStringOverride(sExt);
            if (!string.IsNullOrEmpty(sOverride))
            {
                string sFolderForOverride = bIsFolder ? sPath : Path.GetDirectoryName(sPath) ?? "";
                // For dBASE / CSV / TSV the override may use {folder};
                // also infer a table name from the filename if not
                // already set, since the ACE Text and dBASE drivers
                // address tables as files-within-the-folder.
                if (sExt == "dbf" && !bIsFolder && string.IsNullOrEmpty(sTable))
                    sTable = Path.GetFileNameWithoutExtension(sPath);
                if ((sExt == "csv" || sExt == "tsv" || sExt == "tab" || sExt == "txt")
                    && string.IsNullOrEmpty(sTable))
                    sTable = Path.GetFileName(sPath);
                return sOverride
                    .Replace("{path}", sPath ?? "")
                    .Replace("{folder}", sFolderForOverride);
            }
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

        // readConnectStringOverride: consult the [ConnectStrings]
        // section of the per-user DbDuo.ini (with legacy fallback to
        // the shipped template next to the EXE) for an override of
        // the connection-string template for the given file
        // extension. Returns "" when no override is present.
        private static string readConnectStringOverride(string sExt)
        {
            try
            {
                // Per-user first.
                string sUserBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrEmpty(sUserBase))
                {
                    string sIni = Path.Combine(sUserBase, "DbDuo", "DbDuo.ini");
                    if (File.Exists(sIni))
                    {
                        string sV = readIniFromFile(sIni, "ConnectStrings", sExt);
                        if (!string.IsNullOrEmpty(sV)) return sV;
                    }
                }
                // Shipped template.
                string sShipped = Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
                    "DbDuo.ini");
                if (File.Exists(sShipped))
                {
                    string sV = readIniFromFile(sShipped, "ConnectStrings", sExt);
                    if (!string.IsNullOrEmpty(sV)) return sV;
                }
            }
            catch { }
            return "";
        }

        // readIniFromFile: bare-bones section/key reader used by
        // readConnectStringOverride. Not a full INI parser; handles
        // the [Section] header, key = value lines, and ;/# comments.
        private static string readIniFromFile(string sPath, string sSection, string sKey)
        {
            try
            {
                string sBracketed = "[" + sSection + "]";
                bool bInSection = false;
                foreach (string sRaw in File.ReadAllLines(sPath))
                {
                    string sLine = sRaw.Trim();
                    if (sLine.Length == 0) continue;
                    if (sLine.StartsWith(";") || sLine.StartsWith("#")) continue;
                    if (sLine.StartsWith("[") && sLine.EndsWith("]"))
                    {
                        bInSection = sLine.Equals(sBracketed, StringComparison.OrdinalIgnoreCase);
                        continue;
                    }
                    if (!bInSection) continue;
                    int iEq = sLine.IndexOf('=');
                    if (iEq <= 0) continue;
                    string sK = sLine.Substring(0, iEq).Trim();
                    string sV = sLine.Substring(iEq + 1).Trim();
                    if (sK.Equals(sKey, StringComparison.OrdinalIgnoreCase))
                        return sV;
                }
            }
            catch { }
            return "";
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
            dColumnTypes.Clear();
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

        // ---------------------------------------------------------------
        // Column declared-type metadata cache
        // ---------------------------------------------------------------
        //
        // dColumnTypes maps table -> column-name -> declared-type string.
        // Built lazily on first request per table; invalidated by
        // closeDatabase. Used to drive the textline/textmemo decision
        // for record-edit dialogs.
        //
        // The declared types stored here are whatever the schema says:
        //   - SQLite: the type token from CREATE TABLE, preserved
        //     verbatim by PRAGMA table_info. Common values include
        //     'textline', 'textmemo', 'text', 'integer', 'datetime',
        //     'boolean', etc. SQLite preserves the case the user used,
        //     so isMultilineColumn must compare case-insensitively.
        //   - Access: an integer ADOX Type constant translated to a
        //     name string (e.g. "MEMO" for adLongVarWChar). Mapping
        //     is approximate; we only need to recognize the memo case.
        //   - Other / unknown: empty string. isMultilineColumn returns
        //     false in this case, so the default single-line behavior
        //     applies.
        private Dictionary<string, Dictionary<string, string>> dColumnTypes
            = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        // getColumnDeclaredType: return the schema-declared type
        // string for one column, or "" if unknown. Caches per table
        // so repeated lookups during an edit-dialog build don't fire
        // duplicate PRAGMA queries.
        //
        // The returned string is the type as the schema author wrote
        // it (preserving case). Callers wanting to test for specific
        // type names should compare case-insensitively.
        public string getColumnDeclaredType(string sTable, string sColumn)
        {
            if (!isOpen() || string.IsNullOrEmpty(sTable) || string.IsNullOrEmpty(sColumn))
                return "";
            Dictionary<string, string> dForTable;
            if (!dColumnTypes.TryGetValue(sTable, out dForTable))
            {
                dForTable = loadColumnTypes(sTable);
                dColumnTypes[sTable] = dForTable;
            }
            string sType;
            return dForTable.TryGetValue(sColumn, out sType) ? sType : "";
        }

        // isMultilineColumn: should the edit dialog show this column
        // with a multi-line text box? True for SQLite types whose
        // declared name is 'textmemo' (case-insensitive); also true
        // for Access columns with MEMO affinity (ADOX type 203 ==
        // adLongVarWChar) or any declared type containing the
        // substring "memo".
        //
        // The convention is straightforward: name a SQLite column
        // 'textmemo' in CREATE TABLE if you intend its content to
        // span multiple lines. SQLite's type affinity rules group
        // anything containing 'TEXT' or 'BLOB' as TEXT or BLOB
        // affinity, so 'textmemo' stores identically to 'TEXT' --
        // the declared type is purely a hint for tools that read
        // the schema. PRAGMA table_info preserves it verbatim,
        // so DbDuo can read it back and decide on the widget.
        public bool isMultilineColumn(string sTable, string sColumn)
        {
            string sType = getColumnDeclaredType(sTable, sColumn);
            if (string.IsNullOrEmpty(sType)) return false;
            return sType.IndexOf("memo", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // loadColumnTypes: read every column's declared type for one
        // table from the schema metadata. SQLite path is PRAGMA
        // table_info; Access path is ADOX.Catalog.Tables[t].Columns.
        // Returns an empty dictionary on failure (callers fall back
        // to default single-line behavior).
        private Dictionary<string, string> loadColumnTypes(string sTable)
        {
            Dictionary<string, string> dResult
                = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!isOpen() || string.IsNullOrEmpty(sTable)) return dResult;

            string sExt = currentExtensionForSql();

            // SQLite path: PRAGMA table_info exposes declared types
            // in column index 2 (cid=0, name=1, type=2, notnull=3,
            // dflt_value=4, pk=5).
            if (sExt == "db" || sExt == "sqlite" || sExt == "sqlite3")
            {
                string sQuoted = "\"" + sTable.Replace("\"", "\"\"") + "\"";
                dynamic oRs = null;
                try
                {
                    oRs = createComObject("ADODB.Recordset");
                    oRs.Open("PRAGMA table_info(" + sQuoted + ")", oConn,
                        0 /* adOpenForwardOnly */, 1 /* adLockReadOnly */, 1 /* adCmdText */);
                    while (!(bool)oRs.EOF)
                    {
                        string sName = "", sType = "";
                        try
                        {
                            object oN = oRs.Fields["name"].Value;
                            object oT = oRs.Fields["type"].Value;
                            if (oN != null && oN != DBNull.Value) sName = oN.ToString();
                            if (oT != null && oT != DBNull.Value) sType = oT.ToString();
                        }
                        catch { }
                        if (!string.IsNullOrEmpty(sName))
                            dResult[sName] = sType ?? "";
                        oRs.MoveNext();
                    }
                    try { oRs.Close(); } catch { }
                    return dResult;
                }
                catch (Exception oEx)
                {
                    try { DbDuoLog.write("loadColumnTypes PRAGMA failed: " + oEx.Message); }
                    catch { }
                }
                finally { releaseCom(oRs); }
            }

            // ADOX path: works for Access, and is a fallback if the
            // SQLite path failed. The ADOX Type property is an
            // integer constant; we translate the few values that
            // matter for the textline/textmemo distinction.
            try
            {
                if (oCatalog == null)
                    oCatalog = createComObject("ADOX.Catalog");
                try { oCatalog.ActiveConnection = oConn; } catch { }
                dynamic oTable = oCatalog.Tables[sTable];
                dynamic oColumns = oTable.Columns;
                int iCount = (int)oColumns.Count;
                for (int i = 0; i < iCount; i++)
                {
                    dynamic oCol = oColumns[i];
                    string sName = "";
                    try { sName = (string)oCol.Name; } catch { }
                    if (string.IsNullOrEmpty(sName)) continue;
                    int iType = 0;
                    try { iType = Convert.ToInt32(oCol.Type); } catch { iType = 0; }
                    dResult[sName] = adoxTypeToDeclaredName(iType);
                }
            }
            catch { }

            return dResult;
        }

        // adoxTypeToDeclaredName: translate ADOX Type integer codes
        // to declared-type strings DbDuo recognizes. Only covers
        // the cases that affect the edit dialog's textline/textmemo
        // decision; everything else maps to "" (which yields the
        // default single-line behavior).
        //   adLongVarWChar  (203)  -> "textmemo"  (Access "Memo")
        //   adLongVarChar   (201)  -> "textmemo"
        //   adVarWChar      (202)  -> "textline"  (Access "Text 255")
        //   adVarChar       (200)  -> "textline"
        //   adWChar         (130)  -> "textline"
        //   adChar          (129)  -> "textline"
        private static string adoxTypeToDeclaredName(int iType)
        {
            switch (iType)
            {
                case 201: case 203: return "textmemo";
                case 129: case 130: case 200: case 202: return "textline";
                default: return "";
            }
        }


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

        // findRegexRecord: move the cursor to the next (or previous)
        // row whose value in the named column matches the .NET regex.
        // If sColumn is null/empty or "*", any column matching the
        // regex counts as a hit (the cursor stops at the first such
        // row encountered).
        //
        // Unlike findRecord (which delegates to ADO's own Find with
        // LIKE semantics), this walks the recordset row-by-row in
        // memory. The client-cursor recordset makes the walk O(N) in
        // memory access, not disk seeks. Use plain Find for the
        // common case of case-insensitive substring matching; use
        // this for regex requirements -- anchors, alternation,
        // character classes, lookahead, etc.
        //
        // Returns true if a match was found (cursor on the matched
        // row); false otherwise (cursor restored to its original row).
        public bool findRegexRecord(string sColumn, string sPattern,
                                    bool bForward, bool bFromCurrent)
        {
            if (!hasRecordset()) return false;
            if (string.IsNullOrEmpty(sPattern)) return false;

            System.Text.RegularExpressions.Regex oRegex;
            try
            {
                oRegex = new System.Text.RegularExpressions.Regex(sPattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            }
            catch (Exception oEx) { throw new Exception("Regex invalid: " + oEx.Message, oEx); }

            bool bAnyColumn = string.IsNullOrEmpty(sColumn) || sColumn == "*";
            List<string> lColumns = bAnyColumn ? getFieldNames() : new List<string>(new[] { sColumn });

            object oSavedBookmark = bookmark;

            // Choose the starting row and step direction.
            try
            {
                if (!bFromCurrent)
                {
                    if (bForward) moveFirst();
                    else          moveLast();
                }
                else
                {
                    // Step off the current row so we don't re-match it.
                    if (bForward) moveNext();
                    else          movePrevious();
                }
            }
            catch { }

            while (!eof && !bof)
            {
                foreach (string sCol in lColumns)
                {
                    string sV = getFieldValue(sCol);
                    if (sV != null && oRegex.IsMatch(sV))
                        return true;  // cursor is on the match
                }
                try { if (bForward) moveNext(); else movePrevious(); } catch { break; }
            }

            // No match. Restore.
            if (oSavedBookmark != null)
                try { bookmark = oSavedBookmark; } catch { }
            return false;
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
        // bookkeeping columns plus primary key. Show-Object groups
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

        // exportFrequencyChart: produce a bar chart of how often each
        // distinct value appears in one column of the current
        // filtered view. Writes an .xlsx file with two sheets: a
        // "Data" sheet containing value/count pairs sorted by count
        // descending, and a "Chart" sheet containing a bar chart
        // visualization. Useful for answering "what does my grade
        // distribution look like" or "how many students per major."
        //
        // The data walk uses the current recordset, so the active
        // Filter and Sort are respected -- chart only the rows the
        // user is currently viewing.
        //
        // Requires Excel installed (the .NET Framework 4.8 build of
        // DbDuo has no native xlsx writer; Excel via late-bound COM
        // is the established pattern in DbDuo). Throws InvalidOperation
        // with a human message if the ProgID isn't registered.
        //
        // Chart type: xlColumnClustered (51). Excel's default chart
        // style suffices; we don't customize colors or fonts, since
        // the goal is a quick at-a-glance summary not a publication
        // artifact. The user can polish the chart by hand in Excel.
        //
        // Returns the actual chart-data row count (i.e., distinct
        // value count); useful for the caller to report to the user.
        public int exportFrequencyChart(string sColumn, string sDestPath)
        {
            if (!hasRecordset()) throw new InvalidOperationException("No recordset open.");
            if (string.IsNullOrEmpty(sColumn)) throw new ArgumentException("Column name required.");
            if (!hasField(sColumn)) throw new ArgumentException("Column not found: " + sColumn);

            object oBookmark = bookmark;

            // Step 1: walk the recordset and accumulate counts. We
            // preserve insertion order in a List<string> so the
            // sort step below produces a stable secondary order
            // (alphabetical-by-first-seen) when counts tie.
            Dictionary<string, int> dCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            List<string> lOrder = new List<string>();
            moveFirst();
            while (!eof)
            {
                string sV = getFieldValue(sColumn);
                if (sV == null) sV = "";
                if (sV.Length == 0) sV = "(empty)";
                int iN;
                if (!dCounts.TryGetValue(sV, out iN)) { lOrder.Add(sV); iN = 0; }
                dCounts[sV] = iN + 1;
                moveNext();
            }

            // Sort by count descending, then alphabetically for ties.
            lOrder.Sort(delegate(string a, string b)
            {
                int iCmp = dCounts[b].CompareTo(dCounts[a]);
                if (iCmp != 0) return iCmp;
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            });

            int iDistinct = lOrder.Count;

            // Step 2: emit the workbook and chart via Excel COM.
            dynamic oApp = null;
            dynamic oBook = null;
            dynamic oDataSheet = null;
            dynamic oChartSheet = null;
            dynamic oRange = null;
            try
            {
                oApp = ComAutomation.createApp("Excel.Application");
                oApp.Visible = false;
                oApp.DisplayAlerts = false;
                try { oApp.ScreenUpdating = false; } catch { }

                oBook = oApp.Workbooks.Add();
                oDataSheet = oBook.Sheets[1];
                oDataSheet.Name = "Data";

                // Headers.
                oDataSheet.Cells[1, 1] = sColumn;
                oDataSheet.Cells[1, 2] = "count";

                // Data rows.
                int iRow = 2;
                foreach (string sVal in lOrder)
                {
                    oDataSheet.Cells[iRow, 1] = sVal;
                    oDataSheet.Cells[iRow, 2] = dCounts[sVal];
                    iRow++;
                }

                try { oDataSheet.Columns.AutoFit(); } catch { }
                try { oDataSheet.Range["A1:B1"].Font.Bold = true; } catch { }

                // The data range used for the chart: A1:B(N+1).
                string sRangeAddr = "A1:B" + (iDistinct + 1);
                oRange = oDataSheet.Range[sRangeAddr];

                // Step 3: build the chart. AddChart2 with the
                // Excel 2013+ API is the modern entry point;
                // older API Charts.Add is a fallback. We try the
                // modern path first.
                dynamic oCharts = oBook.Charts;
                dynamic oChart = null;
                bool bModernChart = false;
                try
                {
                    // Sheets.Add for a new chart sheet, then set
                    // ChartType + SetSourceData.
                    oChartSheet = oCharts.Add();
                    oChart = oChartSheet;
                    oChart.ChartType = 51; // xlColumnClustered
                    oChart.SetSourceData(oRange);
                    // The Charts.Add path already names the sheet
                    // "Chart1"; rename to something friendlier.
                    try { oChart.Name = "Chart"; } catch { }
                    bModernChart = true;
                }
                catch (Exception oExModern)
                {
                    try { DbDuoLog.write("Chart Charts.Add path failed: " + oExModern.Message); } catch { }
                }

                if (!bModernChart)
                {
                    // Fall back to embedding the chart on the data
                    // sheet via Shapes.AddChart2. This works on
                    // every modern Excel and is fine for a quick
                    // visualization.
                    try
                    {
                        oChart = oDataSheet.Shapes.AddChart2(-1 /* default style */,
                            51 /* xlColumnClustered */).Chart;
                        oChart.SetSourceData(oRange);
                    }
                    catch (Exception oExEmbed)
                    {
                        throw new InvalidOperationException(
                            "Could not create chart: " + oExEmbed.Message, oExEmbed);
                    }
                }

                // Chart title: "<column> distribution" -- short and
                // accurate.
                try
                {
                    oChart.HasTitle = true;
                    oChart.ChartTitle.Text = sColumn + " distribution";
                }
                catch { }

                // Make sure the workbook saves as .xlsx.
                oBook.SaveAs(sDestPath, 51 /* xlWorkbookDefault */);
                oBook.Close(false);

                try { DbDuoLog.write("Chart saved: " + sDestPath
                    + " (" + iDistinct + " distinct values)"); } catch { }
                return iDistinct;
            }
            finally
            {
                if (oBookmark != null) try { bookmark = oBookmark; } catch { }
                try { if (oApp != null) oApp.Quit(); } catch { }
                releaseCom(oRange);
                releaseCom(oChartSheet);
                releaseCom(oDataSheet);
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

            // Build the connection string. For dBASE the path
            // points at the parent folder (not the file), and the
            // table name is derived separately. buildConnectStringForExport
            // handles both shapes cleanly.
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
    // control at a time. Each add* method appends a labeled row
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
    // (LbC), which exists in C#, Python, AutoIt, and JScript .NET
    // versions and codifies the practice of laying out screen-
    // reader-friendly dialogs by composing simple "add this
    // control" calls in sequence rather than using a designer file.
    //
    // Features carried over from HomerLbc (the mature JScript .NET
    // implementation):
    //   - Focus tips: each add* method takes an optional tip string;
    //     when the user tabs into the control, the tip appears in
    //     a status bar at the bottom of the dialog. Screen readers
    //     announce status-bar changes, so the tip is read aloud
    //     without forcing a popup. Tips replace tooltip popups,
    //     which JAWS often suppresses.
    //   - Name-based widget lookup: every control is registered in
    //     a Widgets dictionary keyed by an auto-generated name
    //     (<Kind>_<CleanedLabel>) so callers can retrieve controls
    //     by string name as well as by reference. Useful for
    //     handlers that get the sender but not the original ref,
    //     and for generic walkers (import/export, validation).
    //   - Memo-vs-AcceptButton coordination: while focus is on a
    //     multi-line TextBox (added via addTextMemo / addMemoBox),
    //     the form's AcceptButton is temporarily cleared so Enter
    //     inserts a newline instead of submitting; on LostFocus
    //     the AcceptButton is restored. This is the Homer LbC
    //     convention -- it lets memo editing feel natural while
    //     still letting Enter submit from single-line fields.
    //
    // Conventions:
    //   - Each labeled control gets a Label above it (not beside).
    //     Uniform vertical rhythm, label-then-control reading order.
    //   - The label text passes through unchanged (caller may
    //     include '&' for mnemonic letters); the control's
    //     AccessibleName strips '&' and trailing ':'.
    //   - TabIndex is assigned in add order. WinForms Labels are
    //     non-focusable and naturally skipped during tab traversal.
    //   - The dialog auto-sizes its height to its contents up to
    //     a cap; AutoScroll engages above the cap.
    //   - Two naming patterns coexist: bare-control adders (addLabel,
    //     addTextBox, addCheckBox) and labeled-control adders that
    //     mirror Homer LbC (addInputBox, addMemoBox, addPickBox,
    //     addComboPickBox). The labeled variants are aliases that
    //     emit a Label first and then call the bare adder.
    //
    // Typical usage:
    //
    //   LbcDialog oDlg = new LbcDialog("Configuration", this);
    //   TextBox   tbMode = oDlg.addInputBox("UI mode", "both", "How DbDuo launches");
    //   CheckBox  cbBeep = oDlg.addCheckBox("Beep on errors", true, "Audible cue on failure");
    //   TextBox   tbNote = oDlg.addMemoBox("Startup note", "", "Free-form text shown at launch");
    //   if (oDlg.runOkCancel())
    //   {
    //       string sMode = tbMode.Text;
    //       bool   bBeep = cbBeep.Checked;
    //       string sNote = tbNote.Text;
    //       // ... persist or apply
    //   }
    //
    //   // Or look up by name later:
    //   TextBox tbAgain = oDlg.getTextBox("TextBox_UI_mode");
    // =====================================================================
    public class LbcDialog : IDisposable
    {
        // Layout constants, all alphabetical. Sized for screen-reader
        // users who often run at 125-150% display scaling -- generous
        // padding makes the dialog readable without crowding labels.
        private const int DefaultButtonHeight = 28;
        private const int DefaultButtonWidth  = 90;
        private const int DefaultDialogWidth  = 520;
        private const int DefaultLabelHeight  = 18;
        private const int DefaultLineHeight   = 24;
        private const int DefaultListHeight   = 100;
        private const int DefaultMaxHeight    = 600;
        private const int DefaultMemoHeight   = 96;
        private const int DefaultNumericWidth = 100;
        private const int DefaultPadding      = 12;
        private const int DefaultRowGap       = 6;
        private const int DefaultStatusHeight = 22;

        // Layout state. Built up by add* calls, consumed by runX.
        // Alphabetical declarations.
        private Dictionary<Control, string> dFocusTips;
        private Dictionary<string, int>     dNameCounts;
        private Dictionary<string, Control> dWidgets;
        private Control                     oFirstFocusable;
        private Form                        oForm;
        private IWin32Window                oOwner;
        private Button                      oSavedAcceptButton;
        private FlowLayoutPanel             oStack;
        private Label                       oStatusBar;
        private int                         iTabIndex;

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
            oForm.ClientSize = new Size(DefaultDialogWidth, 200);

            // Status bar at the bottom of the form. Updates as the
            // user tabs through controls -- each control's focus
            // tip (set via the optional sTip argument on add*
            // methods) appears here when the control receives focus.
            // JAWS, NVDA, and Narrator pick up status-bar text via
            // UIA live-region semantics; we set AccessibleRole to
            // StatusBar so the announcement is appropriately routed.
            //
            // Added BEFORE the stack so its Dock=Bottom claims the
            // bottom area. The button row (added later in run*) is
            // also Dock=Bottom and pushes the status bar up by its
            // own height.
            oStatusBar = new Label();
            oStatusBar.Text = "";
            oStatusBar.AccessibleRole = AccessibleRole.StatusBar;
            oStatusBar.AccessibleName = "Status";
            oStatusBar.Dock = DockStyle.Bottom;
            oStatusBar.Height = DefaultStatusHeight;
            oStatusBar.TextAlign = ContentAlignment.MiddleLeft;
            oStatusBar.Padding = new Padding(DefaultPadding, 2, DefaultPadding, 2);
            oStatusBar.BorderStyle = BorderStyle.Fixed3D;
            oForm.Controls.Add(oStatusBar);

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

            dFocusTips = new Dictionary<Control, string>();
            dNameCounts = new Dictionary<string, int>();
            dWidgets = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase);
            iTabIndex = 0;
            oFirstFocusable = null;
            oSavedAcceptButton = null;
        }

        // ------- Add helpers -------
        //
        // Each adds one control (with optional label and tip) to the
        // vertical stack and returns the control so the caller can
        // stash a reference, attach event handlers, or query the
        // final value after dismissal.
        //
        // Naming pattern (mirrors Homer LbC):
        //   addX        -- bare control, no separate label above it
        //                  (X carries its own label, like CheckBox,
        //                  or no label is wanted)
        //   addXBox     -- labeled control: a Label is added first,
        //                  then the bare X (the convention from
        //                  Homer Lbc where "Box" suffix means
        //                  "with a label above")
        //
        // The Tip parameter, when given, is shown in the status bar
        // when the control receives focus.

        // addLabel: standalone explanatory text. Not focusable, so
        // tip is not applicable.
        public Label addLabel(string sText)
        {
            Label lbl = new Label();
            lbl.Text = sText ?? "";
            lbl.AutoSize = false;
            lbl.Size = new Size(innerWidth(), DefaultLabelHeight);
            lbl.Margin = new Padding(0, 0, 0, DefaultRowGap);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            registerWidget(lbl, "Label", sText);
            oStack.Controls.Add(lbl);
            return lbl;
        }

        // addTextBox: bare single-line text input (no separate
        // Label above). Returns the TextBox.
        public TextBox addTextBox(string sValue, string sTip)
        {
            TextBox tb = new TextBox();
            tb.Text = sValue ?? "";
            tb.Size = new Size(innerWidth(), DefaultLineHeight);
            tb.TabIndex = iTabIndex++;
            tb.Margin = new Padding(0, 0, 0, DefaultRowGap);
            // Inherit the AccessibleName from the most recent Label,
            // if one was just added. Mirrors Homer LbC.
            Label oLastLabel = currentLabelOrNull();
            if (oLastLabel != null) tb.AccessibleName = oLastLabel.AccessibleName;
            tb.GotFocus += handleGotFocus;
            registerWidget(tb, "TextBox", tb.AccessibleName);
            if (!string.IsNullOrEmpty(sTip)) dFocusTips[tb] = sTip;
            oStack.Controls.Add(tb);
            if (oFirstFocusable == null) oFirstFocusable = tb;
            return tb;
        }

        // addInlineInputBox: labeled single-line text input where
        // the label and the text box share one row. Layout:
        //
        //   [Label:        ] [____textbox____________________]
        //
        // Used by the record edit dialog (New-Record / Set-Record)
        // for single-line fields, per the user-guide layout rule:
        // single-line fields put "Label: " inline with the textbox,
        // while multi-line fields put the label on the row above
        // the (taller) memo box. Inline placement gives the screen
        // reader a tight, scannable layout: Tab moves to the field
        // and JAWS announces "Label: <value>" without the user
        // hearing the bare label as a separate Tab stop.
        //
        // Implementation: a TableLayoutPanel with one row, two
        // columns. The label column is auto-sized to fit its text;
        // the textbox column fills the remaining width. The whole
        // panel is added as a single row to the outer FlowLayoutPanel.
        public TextBox addInlineInputBox(string sLabel, string sValue, string sTip)
        {
            TableLayoutPanel oRow = new TableLayoutPanel();
            oRow.ColumnCount = 2;
            oRow.RowCount = 1;
            oRow.AutoSize = true;
            oRow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            oRow.Margin = new Padding(0, 0, 0, DefaultRowGap);
            oRow.ColumnStyles.Clear();
            oRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            oRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            oRow.Width = innerWidth();

            Label lbl = new Label();
            lbl.Text = (sLabel ?? "").TrimEnd();
            if (!lbl.Text.EndsWith(":")) lbl.Text = lbl.Text + ":";
            lbl.AccessibleName = cleanLabel(sLabel);
            lbl.AutoSize = true;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Margin = new Padding(0, 4, DefaultPadding, 0);
            oRow.Controls.Add(lbl, 0, 0);

            TextBox tb = new TextBox();
            tb.Text = sValue ?? "";
            tb.Dock = DockStyle.Fill;
            tb.TabIndex = iTabIndex++;
            tb.Margin = new Padding(0, 0, 0, 0);
            tb.AccessibleName = cleanLabel(sLabel);
            tb.GotFocus += handleGotFocus;
            registerWidget(tb, "TextBox", tb.AccessibleName);
            if (!string.IsNullOrEmpty(sTip)) dFocusTips[tb] = sTip;
            oRow.Controls.Add(tb, 1, 0);

            oStack.Controls.Add(oRow);
            if (oFirstFocusable == null) oFirstFocusable = tb;
            return tb;
        }

        // addInputBox: labeled single-line text input. Adds a Label
        // first, then a TextBox whose AccessibleName comes from the
        // Label. Equivalent to AddInputBox in Homer LbC.
        //
        // Note: for the record edit dialog, callers should prefer
        // addInlineInputBox (label and textbox on one row). This
        // older addInputBox method (label above textbox) is still
        // used by some dialogs where vertical rhythm matters.
        public TextBox addInputBox(string sLabel, string sValue, string sTip)
        {
            addFieldLabel(sLabel);
            TextBox tb = addTextBox(sValue, sTip);
            // The label inheritance happened in addTextBox via
            // currentLabelOrNull. Set AccessibleName explicitly
            // here too as belt-and-suspenders.
            tb.AccessibleName = cleanLabel(sLabel);
            return tb;
        }

        // addTextLine: alias for addInputBox without a tip. Kept
        // for backward compatibility with the earlier API.
        public TextBox addTextLine(string sLabel, string sValue)
        {
            return addInputBox(sLabel, sValue, null);
        }

        // addMemo: bare multi-line text input. Coordinates with
        // AcceptButton so Enter inside the memo inserts a newline
        // instead of submitting the dialog.
        public TextBox addMemo(string sValue, string sTip)
        {
            TextBox tb = new TextBox();
            tb.Text = sValue ?? "";
            tb.Multiline = true;
            tb.AcceptsReturn = true;
            tb.AcceptsTab = false;
            tb.ScrollBars = ScrollBars.Vertical;
            tb.WordWrap = true;
            tb.Size = new Size(innerWidth(), DefaultMemoHeight);
            tb.TabIndex = iTabIndex++;
            tb.Margin = new Padding(0, 0, 0, DefaultRowGap);
            Label oLastLabel = currentLabelOrNull();
            if (oLastLabel != null) tb.AccessibleName = oLastLabel.AccessibleName;
            tb.GotFocus += handleMemoGotFocus;
            tb.LostFocus += handleMemoLostFocus;
            registerWidget(tb, "Memo", tb.AccessibleName);
            if (!string.IsNullOrEmpty(sTip)) dFocusTips[tb] = sTip;
            oStack.Controls.Add(tb);
            if (oFirstFocusable == null) oFirstFocusable = tb;
            return tb;
        }

        // addMemoBox: labeled multi-line text input. Equivalent to
        // AddMemoBox in Homer LbC.
        public TextBox addMemoBox(string sLabel, string sValue, string sTip)
        {
            addFieldLabel(sLabel);
            TextBox tb = addMemo(sValue, sTip);
            tb.AccessibleName = cleanLabel(sLabel);
            return tb;
        }

        // addTextMemo: alias for addMemoBox without a tip. Kept for
        // backward compatibility.
        public TextBox addTextMemo(string sLabel, string sValue)
        {
            return addMemoBox(sLabel, sValue, null);
        }

        // addCheckBox: boolean toggle. The label is part of the
        // checkbox itself (WinForms convention).
        public CheckBox addCheckBox(string sLabel, bool bValue, string sTip)
        {
            CheckBox cb = new CheckBox();
            cb.Text = sLabel ?? "";
            cb.AccessibleName = cleanLabel(sLabel);
            cb.Checked = bValue;
            cb.AutoSize = false;
            cb.Size = new Size(innerWidth(), DefaultLineHeight);
            cb.TabIndex = iTabIndex++;
            cb.Margin = new Padding(0, 0, 0, DefaultRowGap);
            cb.GotFocus += handleGotFocus;
            registerWidget(cb, "CheckBox", sLabel);
            if (!string.IsNullOrEmpty(sTip)) dFocusTips[cb] = sTip;
            oStack.Controls.Add(cb);
            if (oFirstFocusable == null) oFirstFocusable = cb;
            return cb;
        }

        // addCheckBox without tip: backward compat.
        public CheckBox addCheckBox(string sLabel, bool bValue)
        {
            return addCheckBox(sLabel, bValue, null);
        }

        // addListBox: bare pick-one list (no Label).
        public ListBox addListBox(IList<string> lNames, string sSelected, string sTip)
        {
            ListBox lb = new ListBox();
            lb.Size = new Size(innerWidth(), DefaultListHeight);
            lb.TabIndex = iTabIndex++;
            lb.Margin = new Padding(0, 0, 0, DefaultRowGap);
            populateListBox(lb, lNames, sSelected);
            Label oLastLabel = currentLabelOrNull();
            if (oLastLabel != null) lb.AccessibleName = oLastLabel.AccessibleName;
            lb.GotFocus += handleGotFocus;
            registerWidget(lb, "ListBox", lb.AccessibleName);
            if (!string.IsNullOrEmpty(sTip)) dFocusTips[lb] = sTip;
            oStack.Controls.Add(lb);
            if (oFirstFocusable == null) oFirstFocusable = lb;
            return lb;
        }

        // addPickBox: labeled pick-one list. Equivalent to AddPickBox
        // in Homer LbC.
        public ListBox addPickBox(string sLabel, IList<string> lNames, string sSelected, string sTip)
        {
            addFieldLabel(sLabel);
            ListBox lb = addListBox(lNames, sSelected, sTip);
            lb.AccessibleName = cleanLabel(sLabel);
            return lb;
        }

        // addListBox(label, items, selected): backward-compat
        // 3-arg form that maps to addPickBox without a tip.
        public ListBox addListBox(string sLabel, IList<string> lNames, string sSelected)
        {
            return addPickBox(sLabel, lNames, sSelected, null);
        }

        // addComboBox: bare drop-down pick-one. DropDownList style
        // so the user can only pick from the list.
        public ComboBox addComboBox(IList<string> lNames, string sSelected, string sTip)
        {
            ComboBox cb = new ComboBox();
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.Size = new Size(innerWidth(), DefaultLineHeight);
            cb.TabIndex = iTabIndex++;
            cb.Margin = new Padding(0, 0, 0, DefaultRowGap);
            populateComboBox(cb, lNames, sSelected);
            Label oLastLabel = currentLabelOrNull();
            if (oLastLabel != null) cb.AccessibleName = oLastLabel.AccessibleName;
            cb.GotFocus += handleGotFocus;
            registerWidget(cb, "ComboBox", cb.AccessibleName);
            if (!string.IsNullOrEmpty(sTip)) dFocusTips[cb] = sTip;
            oStack.Controls.Add(cb);
            if (oFirstFocusable == null) oFirstFocusable = cb;
            return cb;
        }

        // addComboPickBox: labeled drop-down pick-one. Equivalent
        // to AddComboPickBox in Homer LbC.
        public ComboBox addComboPickBox(string sLabel, IList<string> lNames, string sSelected, string sTip)
        {
            addFieldLabel(sLabel);
            ComboBox cb = addComboBox(lNames, sSelected, sTip);
            cb.AccessibleName = cleanLabel(sLabel);
            return cb;
        }

        // addComboBox(label, items, selected): backward-compat
        // 3-arg form that maps to addComboPickBox without a tip.
        public ComboBox addComboBox(string sLabel, IList<string> lNames, string sSelected)
        {
            return addComboPickBox(sLabel, lNames, sSelected, null);
        }

        // addRadioButton: one option in a radio-button group. The
        // first call after a non-RadioButton control starts a new
        // group automatically (WinForms convention).
        public RadioButton addRadioButton(string sLabel, bool bChecked, string sTip)
        {
            RadioButton rb = new RadioButton();
            rb.Text = sLabel ?? "";
            rb.AccessibleName = cleanLabel(sLabel);
            rb.Checked = bChecked;
            rb.AutoSize = false;
            rb.Size = new Size(innerWidth(), DefaultLineHeight);
            rb.TabIndex = iTabIndex++;
            rb.Margin = new Padding(0, 0, 0, DefaultRowGap);
            rb.GotFocus += handleGotFocus;
            registerWidget(rb, "RadioButton", sLabel);
            if (!string.IsNullOrEmpty(sTip)) dFocusTips[rb] = sTip;
            oStack.Controls.Add(rb);
            if (oFirstFocusable == null) oFirstFocusable = rb;
            return rb;
        }

        // addRadioButton without tip: backward compat.
        public RadioButton addRadioButton(string sLabel, bool bChecked)
        {
            return addRadioButton(sLabel, bChecked, null);
        }

        // addNumericUpDown: typed integer input with min/max bounds
        // and spinner. Equivalent to AddNumericUpDown in Homer LbC.
        public NumericUpDown addNumericUpDown(string sLabel, int iValue,
                                              int iMin, int iMax, string sTip)
        {
            if (!string.IsNullOrEmpty(sLabel)) addFieldLabel(sLabel);
            NumericUpDown nud = new NumericUpDown();
            nud.Minimum = iMin;
            nud.Maximum = iMax;
            nud.Value = Math.Max(iMin, Math.Min(iMax, iValue));
            nud.Size = new Size(DefaultNumericWidth, DefaultLineHeight);
            nud.TabIndex = iTabIndex++;
            nud.Margin = new Padding(0, 0, 0, DefaultRowGap);
            nud.AccessibleName = cleanLabel(sLabel);
            nud.GotFocus += handleGotFocus;
            registerWidget(nud, "NumericUpDown", sLabel);
            if (!string.IsNullOrEmpty(sTip)) dFocusTips[nud] = sTip;
            oStack.Controls.Add(nud);
            if (oFirstFocusable == null) oFirstFocusable = nud;
            return nud;
        }

        // addSeparator: a thin horizontal divider, for visually
        // grouping related fields. Not focusable.
        public void addSeparator()
        {
            Label sep = new Label();
            sep.Size = new Size(innerWidth(), 2);
            sep.BorderStyle = BorderStyle.Fixed3D;
            sep.Margin = new Padding(0, DefaultRowGap, 0, DefaultRowGap);
            oStack.Controls.Add(sep);
        }

        // ------- Widget lookup helpers -------
        //
        // Every widget added via add* methods is registered in
        // dWidgets under an auto-generated name of the form
        // <Kind>_<CleanedLabel>. These helpers let callers fetch
        // widgets by name without having to keep references.
        //
        // The name generator strips non-alphanumeric chars from
        // the label, replaces spaces with underscore, and appends
        // a 2/3/... suffix on collisions.

        public Control findControl(string sName)
        {
            if (string.IsNullOrEmpty(sName)) return null;
            Control oCtl;
            if (dWidgets.TryGetValue(sName, out oCtl)) return oCtl;
            return null;
        }

        public TextBox       getTextBox(string sName)       { return findControl(sName) as TextBox; }
        public CheckBox      getCheckBox(string sName)      { return findControl(sName) as CheckBox; }
        public ComboBox      getComboBox(string sName)      { return findControl(sName) as ComboBox; }
        public ListBox       getListBox(string sName)       { return findControl(sName) as ListBox; }
        public RadioButton   getRadioButton(string sName)   { return findControl(sName) as RadioButton; }
        public NumericUpDown getNumericUpDown(string sName) { return findControl(sName) as NumericUpDown; }
        public Label         getLabel(string sName)         { return findControl(sName) as Label; }

        // Snapshot of every widget added so far, keyed by name.
        // Useful for callers that want to walk the whole dialog.
        public IDictionary<string, Control> widgets
        { get { return new Dictionary<string, Control>(dWidgets); } }

        // ------- Finish and show -------

        // runOkCancel: convenience wrapper. Returns true on OK.
        public bool runOkCancel()
        {
            return string.Equals(runWithButtons(new string[] { "OK", "Cancel" }),
                                 "OK", StringComparison.OrdinalIgnoreCase);
        }

        // runWithButtons: add a button band at the bottom, show
        // modally, return the label of the button the user pressed
        // (or "" on Escape/close-box). The first label is the
        // AcceptButton (Enter); any "Cancel" or "Close" label is
        // the CancelButton (Escape).
        public string runWithButtons(string[] aButtonLabels)
        {
            FlowLayoutPanel oButtonRow = new FlowLayoutPanel();
            oButtonRow.FlowDirection = FlowDirection.RightToLeft;
            oButtonRow.AutoSize = false;
            oButtonRow.Dock = DockStyle.Bottom;
            oButtonRow.Height = DefaultButtonHeight + DefaultPadding * 2;
            oButtonRow.Padding = new Padding(DefaultPadding);

            string sResult = "";
            Button oAcceptBtn = null;
            Button oCancelBtn = null;

            // Add buttons right-to-left so the visual order reads
            // left-to-right as given. RightToLeft FlowDirection
            // puts the first-added at the right; we want first-
            // given at the left, so iterate in reverse.
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

                string sCaptured = sLabel.Replace("&", "");
                btn.Click += delegate(object o, EventArgs e) {
                    sResult = sCaptured;
                    oForm.DialogResult = DialogResult.OK;
                    oForm.Close();
                };
                registerWidget(btn, "Button", sCaptured);
                oButtonRow.Controls.Add(btn);
                if (i == 0) oAcceptBtn = btn;
                if (string.Equals(sCaptured, "Cancel", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(sCaptured, "Close", StringComparison.OrdinalIgnoreCase))
                    oCancelBtn = btn;
            }
            oForm.Controls.Add(oButtonRow);
            if (oAcceptBtn != null) oForm.AcceptButton = oAcceptBtn;
            if (oCancelBtn != null) oForm.CancelButton = oCancelBtn;

            int iContentHeight = computeStackHeight();
            int iTotalHeight = iContentHeight + oButtonRow.Height
                             + oStatusBar.Height + 8;
            if (iTotalHeight > DefaultMaxHeight) iTotalHeight = DefaultMaxHeight;
            if (iTotalHeight < 200) iTotalHeight = 200;
            oForm.ClientSize = new Size(DefaultDialogWidth, iTotalHeight);

            if (oFirstFocusable != null) oForm.ActiveControl = oFirstFocusable;
            oForm.ShowDialog(oOwner);
            return sResult;
        }

        // form: outer access for callers who need to tweak something
        // the high-level API doesn't expose (e.g., add an Icon).
        public Form form { get { return oForm; } }

        public void Dispose()
        {
            if (oForm != null) { oForm.Dispose(); oForm = null; }
        }

        // ------- Internal helpers (kept private) -------

        // currentLabelOrNull: the most-recently-added Label, if the
        // last control added was a Label. Returns null if the last
        // control was something else (or the stack is empty). Used
        // by bare-control adders to inherit the accessible name
        // from an immediately-preceding Label, mirroring the Homer
        // LbC convention where AddTextBox after AddLabel automatically
        // gets the label's AccessibleName.
        private Label currentLabelOrNull()
        {
            int iCount = oStack.Controls.Count;
            if (iCount == 0) return null;
            return oStack.Controls[iCount - 1] as Label;
        }

        // handleGotFocus: status-bar tip update on focus.
        private void handleGotFocus(object oSender, EventArgs oArgs)
        {
            Control oCtl = oSender as Control;
            if (oCtl == null) { oStatusBar.Text = ""; return; }
            string sTip;
            oStatusBar.Text = dFocusTips.TryGetValue(oCtl, out sTip) ? sTip : "";
        }

        // handleMemoGotFocus: while a memo has focus, Enter must
        // insert a newline instead of submitting. Clear the form's
        // AcceptButton (remembering it for restore on LostFocus).
        // Also update the status bar.
        private void handleMemoGotFocus(object oSender, EventArgs oArgs)
        {
            if (oForm.AcceptButton != null)
                oSavedAcceptButton = oForm.AcceptButton as Button;
            oForm.AcceptButton = null;
            handleGotFocus(oSender, oArgs);
        }

        // handleMemoLostFocus: restore the AcceptButton when the
        // memo loses focus, so Enter from a subsequent single-line
        // field submits as expected.
        private void handleMemoLostFocus(object oSender, EventArgs oArgs)
        {
            if (oSavedAcceptButton != null && oForm.AcceptButton == null)
                oForm.AcceptButton = oSavedAcceptButton;
        }

        // registerWidget: store the control under an auto-generated
        // name <Kind>_<CleanedLabel> in dWidgets. On collisions a
        // numeric suffix is appended (TextBox_Name, TextBox_Name_2).
        private void registerWidget(Control oCtl, string sKind, string sLabel)
        {
            string sClean = makeIdentifier(sLabel);
            string sBase = sKind + "_" + sClean;
            string sName = sBase;
            int iCount;
            if (dNameCounts.TryGetValue(sBase, out iCount) && iCount > 0)
                sName = sBase + "_" + (iCount + 1);
            dNameCounts[sBase] = (dNameCounts.ContainsKey(sBase) ? dNameCounts[sBase] : 0) + 1;
            oCtl.Name = sName;
            dWidgets[sName] = oCtl;
        }

        // makeIdentifier: turn a label into a programmer-friendly
        // identifier suffix. Strips '&' and ':', maps any run of
        // non-alphanumeric to a single underscore, trims leading
        // and trailing underscores, falls back to "field" on empty.
        private static string makeIdentifier(string sLabel)
        {
            if (string.IsNullOrEmpty(sLabel)) return "field";
            StringBuilder oSb = new StringBuilder();
            bool bLastWasUnderscore = true; // suppress leading
            foreach (char c in sLabel)
            {
                if (char.IsLetterOrDigit(c)) { oSb.Append(c); bLastWasUnderscore = false; }
                else if (!bLastWasUnderscore) { oSb.Append('_'); bLastWasUnderscore = true; }
            }
            string s = oSb.ToString();
            if (s.EndsWith("_")) s = s.Substring(0, s.Length - 1);
            return s.Length > 0 ? s : "field";
        }

        // The horizontal space inside the stack panel for a control,
        // accounting for padding and a scrollbar reservation.
        private int innerWidth()
        {
            return DefaultDialogWidth - DefaultPadding * 2 - 24;
        }

        // cleanLabel: strip '&' mnemonic markers and trailing ':'
        // before using a label as an AccessibleName.
        private string cleanLabel(string sLabel)
        {
            if (string.IsNullOrEmpty(sLabel)) return "";
            string s = sLabel.Replace("&", "");
            if (s.EndsWith(":")) s = s.Substring(0, s.Length - 1);
            return s.Trim();
        }

        // addFieldLabel: emit a Label above a field control. Reused
        // by every labeled-control adder (addInputBox, addMemoBox,
        // addPickBox, addComboPickBox, addNumericUpDown).
        private void addFieldLabel(string sText)
        {
            if (string.IsNullOrEmpty(sText)) return;
            Label lbl = new Label();
            lbl.Text = sText;
            lbl.AccessibleName = cleanLabel(sText);
            lbl.AutoSize = false;
            lbl.Size = new Size(innerWidth(), DefaultLabelHeight);
            lbl.Margin = new Padding(0, 0, 0, 0);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            oStack.Controls.Add(lbl);
        }

        // populateListBox: shared logic for filling a ListBox with
        // strings and pre-selecting one.
        private void populateListBox(ListBox lb, IList<string> lNames, string sSelected)
        {
            if (lNames == null) return;
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

        // populateComboBox: shared logic for filling a ComboBox.
        private void populateComboBox(ComboBox cb, IList<string> lNames, string sSelected)
        {
            if (lNames == null) return;
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

        // computeStackHeight: sum heights of stack children plus
        // margins. Used to choose an initial dialog height.
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
    // =====================================================================
    // RecordEditDialog: dynamic per-column field editor. Builds a
    // labeled text input per column from a list of column names.
    // The caller passes initial values, an editable-flags list (false
    // marks a column read-only -- typically for primary key columns),
    // and the manager so the dialog can ask each column whether its
    // declared type is 'textmemo' (multi-line) or 'textline' / other
    // (single-line). OK populates dValues with the final field values.
    //
    // Built on top of LbcDialog so it gets the focus-tip status bar,
    // name-based widget lookup, AcceptButton-vs-memo coordination,
    // and all the layout polish for free. Single-line columns use
    // addInputBox; multi-line columns (declared textmemo) use
    // addMemoBox.
    //
    // The textline/textmemo convention: in CREATE TABLE statements,
    // declare single-line columns as 'textline' and multi-line ones
    // as 'textmemo'. SQLite stores both with TEXT affinity (the
    // declared type is purely a hint for tools), but PRAGMA table_info
    // round-trips the declared type verbatim, so DbDuo can read it
    // back. Columns declared 'text' or anything else fall back to
    // single-line. Access provides the same distinction via ADOX
    // type codes (adLongVarWChar/adLongVarChar = memo).
    //
    // The constructor's API matches the prior version with one extra
    // parameter (the manager); existing callers update with one line.
    // The dialog is exposed as a class with an `OK` boolean and a
    // `dValues` result dictionary instead of being a Form subclass,
    // since LbcDialog is the actual Form now.
    // =====================================================================
    public class RecordEditDialog : IDisposable
    {
        public bool                        ok;        // true if user pressed OK
        public Dictionary<string, string>  dValues;   // final field values

        private LbcDialog                  oDlg;
        private List<string>               lColumnNames;
        private List<TextBox>              lTextBoxes;
        private List<string>               lDeclaredTypes;
        private DbDuoManager               oMgr;
        private string                     sTableName;

        public RecordEditDialog(string sTitle,
                                List<string> lColumns,
                                Dictionary<string, string> dInitial,
                                List<bool> lEditable,
                                DbDuoManager oManager)
        {
            ok = false;
            dValues = new Dictionary<string, string>();
            lColumnNames = new List<string>();
            lTextBoxes = new List<TextBox>();
            lDeclaredTypes = new List<string>();
            oMgr = oManager;
            sTableName = (oManager != null) ? oManager.currentTable : "";

            oDlg = new LbcDialog(sTitle, null);

            // Build one row per column. The widget kind comes from
            // the declared type:
            //   - textmemo, memo, longvarchar: tall multi-line memo
            //     box with the label on the row ABOVE the box
            //   - everything else: single-line input box with the
            //     label and the textbox sharing one row inline
            //
            // The two layouts are intentional. A single-line field
            // gets "Label: __textbox__" inline, mirroring how the
            // user-guide convention shows fields. A multi-line memo
            // gets the label on its own row above so the (tall) memo
            // box has the full row width.
            //
            // Type-aware display: for integer/real/numeric declared
            // types, the initial value goes through a string
            // round-trip that normalizes any locale-specific
            // thousands separators ("1,234" -> "1234") and trims
            // trailing zeros. Validation on OK re-parses the value
            // and refuses to close if a numeric field has non-
            // numeric content.
            for (int i = 0; i < lColumns.Count; i++)
            {
                string sCol = lColumns[i];
                lColumnNames.Add(sCol);
                string sValue = (dInitial != null && dInitial.ContainsKey(sCol))
                                ? dInitial[sCol] : "";
                bool bEditable = (lEditable == null
                                  || i >= lEditable.Count
                                  || lEditable[i]);

                string sDeclared = (oManager != null)
                    ? oManager.getColumnDeclaredType(sTableName, sCol)
                    : "";
                lDeclaredTypes.Add(sDeclared ?? "");

                // Format for display.
                string sDisplay = convertForDisplay(sValue, sDeclared);

                // Tip: type plus optional read-only marker plus the
                // regex constraint string from DbDuo.ini if one is
                // configured for <table>.<column>.
                string sRegex = lookupFieldRegex(sCol);
                StringBuilder oTip = new StringBuilder();
                if (!string.IsNullOrEmpty(sDeclared)) oTip.Append("type ").Append(sDeclared);
                if (!bEditable)
                {
                    if (oTip.Length > 0) oTip.Append(' ');
                    oTip.Append("(read-only)");
                }
                if (!string.IsNullOrEmpty(sRegex))
                {
                    if (oTip.Length > 0) oTip.Append(' ');
                    oTip.Append("must match ").Append(sRegex);
                }
                string sTip = oTip.ToString();

                bool bMultiline = (oManager != null)
                    && oManager.isMultilineColumn(sTableName, sCol);
                TextBox tb;
                if (bMultiline)
                {
                    // Multi-line memo: label on the row above.
                    tb = oDlg.addMemoBox(sCol + ":", sDisplay, sTip);
                }
                else
                {
                    // Single-line: label and textbox on one row.
                    tb = oDlg.addInlineInputBox(sCol, sDisplay, sTip);
                }
                if (!bEditable) tb.ReadOnly = true;
                lTextBoxes.Add(tb);
            }
        }

        // Show the dialog modally. Sets ok and populates dValues if
        // the user pressed OK. owner argument is the parent window.
        //
        // OK is gated by validation: every editable field's value
        // must (a) parse cleanly to its declared type and (b) match
        // its configured regex if one is set. A failed validation
        // pops a MessageBox naming the offending field, leaves the
        // dialog open with focus on that textbox, and the user can
        // correct and try again. Cancel is never gated.
        public DialogResult showDialog(IWin32Window oOwner)
        {
            Form oForm = oDlg.form;
            oForm.StartPosition = FormStartPosition.CenterParent;
            // Loop on OK so a validation failure can keep the
            // dialog open. LbcDialog.runWithButtons returns the
            // button label that was pressed; "OK" requires
            // validation to pass.
            while (true)
            {
                string sButton = oDlg.runWithButtons(new string[] { "OK", "Cancel" });
                bool bOk = string.Equals(sButton, "OK", StringComparison.OrdinalIgnoreCase);
                if (!bOk)
                { ok = false; return DialogResult.Cancel; }
                string sErr;
                int iOffending;
                if (validateValues(out sErr, out iOffending))
                {
                    ok = true;
                    for (int i = 0; i < lTextBoxes.Count; i++)
                        dValues[lColumnNames[i]] = convertForStorage(lTextBoxes[i].Text, lDeclaredTypes[i]);
                    return DialogResult.OK;
                }
                MessageBox.Show(oForm, sErr, "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (iOffending >= 0 && iOffending < lTextBoxes.Count)
                {
                    try { lTextBoxes[iOffending].Focus(); lTextBoxes[iOffending].SelectAll(); }
                    catch { }
                }
                // Loop continues -- dialog stays open for retry.
            }
        }

        // validateValues: walk every editable field, parse its value
        // against its declared type, and (if a regex is configured
        // for that field in DbDuo.ini) check Regex.IsMatch. Returns
        // true if all clean; otherwise false with sErr describing
        // the first failure and iOffending = index of the failing
        // textbox.
        private bool validateValues(out string sErr, out int iOffending)
        {
            sErr = null;
            iOffending = -1;
            for (int i = 0; i < lTextBoxes.Count; i++)
            {
                if (lTextBoxes[i].ReadOnly) continue;
                string sCol = lColumnNames[i];
                string sVal = lTextBoxes[i].Text ?? "";
                string sDeclared = lDeclaredTypes[i];

                // Empty values are allowed (nullable columns; the
                // database layer decides if NULL is acceptable).
                if (sVal.Length == 0) continue;

                // Type-aware parse check.
                string sTypeErr = checkTypeParse(sVal, sDeclared);
                if (sTypeErr != null)
                {
                    sErr = "Field '" + sCol + "': " + sTypeErr;
                    iOffending = i;
                    return false;
                }

                // Regex check.
                string sRegex = lookupFieldRegex(sCol);
                if (!string.IsNullOrEmpty(sRegex))
                {
                    bool bMatch = false;
                    try { bMatch = System.Text.RegularExpressions.Regex.IsMatch(sVal, sRegex); }
                    catch (Exception oEx)
                    {
                        sErr = "Field '" + sCol + "': invalid regex in DbDuo.ini: " + oEx.Message;
                        iOffending = i;
                        return false;
                    }
                    if (!bMatch)
                    {
                        sErr = "Field '" + sCol + "': value does not match required pattern (" + sRegex + ")";
                        iOffending = i;
                        return false;
                    }
                }
            }
            return true;
        }

        // checkTypeParse: returns null if sValue parses cleanly for
        // sDeclared, or a human-friendly error string otherwise.
        // The check is permissive on textual types (anything goes)
        // and strict on numeric/date types.
        private static string checkTypeParse(string sValue, string sDeclared)
        {
            if (string.IsNullOrEmpty(sDeclared)) return null;
            string sLow = sDeclared.ToLowerInvariant();
            // Numeric integer
            if (sLow.Contains("int") || sLow == "integer")
            {
                long iN;
                if (!long.TryParse(sValue,
                        System.Globalization.NumberStyles.Integer
                        | System.Globalization.NumberStyles.AllowThousands,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out iN))
                    return "expected an integer, got '" + sValue + "'";
                return null;
            }
            // Numeric real
            if (sLow.Contains("real") || sLow.Contains("float")
                || sLow.Contains("double") || sLow.Contains("numeric")
                || sLow.Contains("decimal"))
            {
                double dN;
                if (!double.TryParse(sValue,
                        System.Globalization.NumberStyles.Float
                        | System.Globalization.NumberStyles.AllowThousands,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out dN))
                    return "expected a number, got '" + sValue + "'";
                return null;
            }
            // Date
            if (sLow.Contains("date") || sLow.Contains("time"))
            {
                DateTime oDt;
                if (!DateTime.TryParse(sValue,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out oDt))
                    return "expected a date or date-time, got '" + sValue + "'";
                return null;
            }
            return null;
        }

        // convertForDisplay: take a raw value as it came from ADO
        // and produce a string suitable for the textbox. For most
        // types this is the value unchanged. For numerics it
        // normalizes thousands separators and trims trailing zeros
        // off real values. For dates it produces ISO yyyy-MM-dd
        // for date-only columns and yyyy-MM-dd HH:mm:ss for
        // date-time columns.
        private static string convertForDisplay(string sValue, string sDeclared)
        {
            if (string.IsNullOrEmpty(sValue) || string.IsNullOrEmpty(sDeclared)) return sValue;
            string sLow = sDeclared.ToLowerInvariant();
            try
            {
                if (sLow.Contains("int"))
                {
                    long iN;
                    if (long.TryParse(sValue,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out iN))
                        return iN.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (sLow.Contains("real") || sLow.Contains("float")
                         || sLow.Contains("double") || sLow.Contains("numeric")
                         || sLow.Contains("decimal"))
                {
                    double dN;
                    if (double.TryParse(sValue,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out dN))
                    {
                        // Round-trip without locale; "G17" preserves
                        // round-trippable double precision but reads
                        // ugly for typical user data. "G15" is more
                        // human-readable and lossless for most
                        // user-entered values.
                        return dN.ToString("G15",
                            System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                else if (sLow.Contains("date") || sLow.Contains("time"))
                {
                    DateTime oDt;
                    if (DateTime.TryParse(sValue,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out oDt))
                    {
                        bool bTimeOnly = sLow.Contains("time") && !sLow.Contains("date");
                        bool bDateOnly = sLow.Contains("date") && !sLow.Contains("time");
                        if (bDateOnly) return oDt.ToString("yyyy-MM-dd");
                        if (bTimeOnly) return oDt.ToString("HH:mm:ss");
                        return oDt.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
            }
            catch { /* fall through to raw */ }
            return sValue;
        }

        // convertForStorage: inverse of convertForDisplay. Takes a
        // textbox value (already validated) and produces the string
        // form the database layer will use. For most types this is
        // a passthrough; for dates we always emit ISO yyyy-MM-dd[T...]
        // since that's what every backend (SQLite, Access, dBASE)
        // accepts unambiguously.
        private static string convertForStorage(string sValue, string sDeclared)
        {
            if (string.IsNullOrEmpty(sValue) || string.IsNullOrEmpty(sDeclared)) return sValue;
            string sLow = sDeclared.ToLowerInvariant();
            try
            {
                if (sLow.Contains("int"))
                {
                    long iN;
                    if (long.TryParse(sValue,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out iN))
                        return iN.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (sLow.Contains("real") || sLow.Contains("float")
                         || sLow.Contains("double") || sLow.Contains("numeric")
                         || sLow.Contains("decimal"))
                {
                    double dN;
                    if (double.TryParse(sValue,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out dN))
                        return dN.ToString("G15",
                            System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (sLow.Contains("date") || sLow.Contains("time"))
                {
                    DateTime oDt;
                    if (DateTime.TryParse(sValue,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out oDt))
                    {
                        bool bTimeOnly = sLow.Contains("time") && !sLow.Contains("date");
                        bool bDateOnly = sLow.Contains("date") && !sLow.Contains("time");
                        if (bDateOnly) return oDt.ToString("yyyy-MM-dd");
                        if (bTimeOnly) return oDt.ToString("HH:mm:ss");
                        return oDt.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
            }
            catch { }
            return sValue;
        }

        // lookupFieldRegex: read the regex constraint for <table>.<column>
        // from DbDuo.ini. The config schema is one INI section per
        // table named [Validation:<table>]; within it, each key is a
        // column name and each value is a .NET regex pattern. Example:
        //
        //   [Validation:students]
        //   email = ^[^@\s]+@[^@\s]+\.[a-z]+$
        //   year = ^(Freshman|Sophomore|Junior|Senior)$
        //
        // A missing entry means no regex applied. The lookup is
        // table-scoped, so the same column name can have different
        // regexes in different tables.
        private string lookupFieldRegex(string sColumn)
        {
            if (string.IsNullOrEmpty(sColumn) || string.IsNullOrEmpty(sTableName)) return null;
            try { return IniValidation.lookup(sTableName, sColumn); }
            catch { return null; }
        }

        public void Dispose()
        {
            if (oDlg != null) { oDlg.Dispose(); oDlg = null; }
        }
    }

    // IniValidation: load and look up per-field regex constraints
    // from DbDuo.ini. The config schema is one section per table
    // named [Validation:<table>], with one key-value pair per
    // column where the key is the column name and the value is
    // a .NET regex pattern. The whole file is read lazily on
    // first lookup and cached for the rest of the session.
    public static class IniValidation
    {
        private static Dictionary<string, Dictionary<string, string>> dByTable;
        private static bool bLoaded = false;

        // Reset is used by the Edit-Configuration dialog after a
        // save, to invalidate the cache so subsequent dialogs pick
        // up the new regexes.
        public static void reset()
        {
            bLoaded = false;
            dByTable = null;
        }

        public static string lookup(string sTable, string sColumn)
        {
            if (!bLoaded) load();
            if (dByTable == null) return null;
            Dictionary<string, string> dCols;
            if (!dByTable.TryGetValue(sTable.ToLowerInvariant(), out dCols)) return null;
            string sRegex;
            if (!dCols.TryGetValue(sColumn.ToLowerInvariant(), out sRegex)) return null;
            return sRegex;
        }

        private static void load()
        {
            bLoaded = true;
            dByTable = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            // Compute the per-user DbDuo.ini path directly. The
            // matching helper inside DbDuoForm (IniSession.iniPath)
            // is private to that class; rather than expose it
            // publicly just for one caller, replicate the simple
            // path-build here. The path layout matches IniSession
            // exactly so the same file is read.
            string sIni;
            try
            {
                string sBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(sBase))
                {
                    // %LOCALAPPDATA% unavailable: fall back to the
                    // EXE folder. This keeps lookup working in the
                    // unusual case where the user profile path is
                    // unset (portable installs, sandboxes, etc.)
                    string sExeDir = System.IO.Path.GetDirectoryName(Application.ExecutablePath) ?? ".";
                    sIni = System.IO.Path.Combine(sExeDir, "DbDuo.ini");
                }
                else
                {
                    string sDir = System.IO.Path.Combine(sBase, "DbDuo");
                    sIni = System.IO.Path.Combine(sDir, "DbDuo.ini");
                }
            }
            catch { return; }
            if (string.IsNullOrEmpty(sIni)) return;
            if (!System.IO.File.Exists(sIni)) return;
            try
            {
                string[] aLines = System.IO.File.ReadAllLines(sIni);
                string sCurrentTable = null;
                foreach (string sLine in aLines)
                {
                    string sTrim = sLine.Trim();
                    if (sTrim.Length == 0) continue;
                    if (sTrim.StartsWith(";") || sTrim.StartsWith("#")) continue;
                    if (sTrim.StartsWith("[") && sTrim.EndsWith("]"))
                    {
                        string sSection = sTrim.Substring(1, sTrim.Length - 2).Trim();
                        const string sPrefix = "Validation:";
                        if (sSection.StartsWith(sPrefix, StringComparison.OrdinalIgnoreCase))
                            sCurrentTable = sSection.Substring(sPrefix.Length).Trim();
                        else
                            sCurrentTable = null;
                        continue;
                    }
                    if (sCurrentTable == null) continue;
                    int iEq = sTrim.IndexOf('=');
                    if (iEq <= 0) continue;
                    string sKey = sTrim.Substring(0, iEq).Trim();
                    string sVal = sTrim.Substring(iEq + 1).Trim();
                    if (sKey.Length == 0 || sVal.Length == 0) continue;
                    Dictionary<string, string> dCols;
                    string sTKey = sCurrentTable.ToLowerInvariant();
                    if (!dByTable.TryGetValue(sTKey, out dCols))
                    {
                        dCols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        dByTable[sTKey] = dCols;
                    }
                    dCols[sKey] = sVal;
                }
                try { DbDuoLog.write("IniValidation loaded " + dByTable.Count + " table section(s)"); } catch { }
            }
            catch (Exception oEx)
            {
                try { DbDuoLog.write("IniValidation load failed: " + oEx.Message); } catch { }
            }
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
            this.Text = "Filter Records";
            this.AccessibleName = "Filter records";
            this.AccessibleDescription = "";
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
            this.Text = "Custom Sort";
            this.AccessibleName = "Sort records";
            this.AccessibleDescription = "";
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
            this.Text = "Command Picker";
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
        // The recordset/connection manager. PUBLIC so the C# scripting
        // feature (Help > Run C# Script) can reach the current
        // database/table/recordset via oForm.db from script code.
        // Scripts get the same view of the data the form has -- no
        // facade in between.
        public DbDuoManager db;
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
        // announced as a unit, and uses Show-Object (Enter) for a
        // field-by-field dump of the current row. Sort and column-
        // specific commands (Sort Ascending, Sort Descending, Next
        // Initial Change, Open Cell Value, Jump to Match, etc.)
        // each prompt for the column via a standard LBC dialog,
        // rather than reading a "current column" from Tab state.
        // Tab still moves an announcement-only column cursor that
        // the screen reader speaks as the user hops across cells
        // in a focused row, but Tab no longer targets commands.
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

        // Note: an older DataGridView build of DbDuo carried a
        // private 'oCurrentData' DataTable here as its data source.
        // The virtual-mode ListView reads rows directly from the
        // ADO recordset via RetrieveVirtualItem, so the DataTable
        // is unused. The field has been removed; if you're reading
        // an older commit and miss it, that's why.
        private bool bSuppressCellChanged;

        // Tab-announced column index. The ListView has no per-cell
        // focus, but for screen-reader users it's useful to be able
        // to hop across cells in a focused row hearing each column
        // header + value announced. Tab increments this, Shift+Tab
        // decrements, arrow keys reset to 0. Used ONLY by
        // announceCurrentColumn (the speech helper); no command
        // reads it for targeting. Commands that need a specific
        // column prompt via LBC.
        private int iCurrentColumnIndex = 0;

        // VirtualCursor: cell-level navigation overlay. The ListView
        // has no per-cell focus, but screen-reader users benefit from
        // table-style cell navigation (the same way JAWS/NVDA expose
        // an HTML or Word table). The virtual cursor is a (row, col)
        // pair that the user moves with Alt+Control+arrow chords;
        // movement triggers an announcement of the cell value plus
        // (when the direction warrants) the column header or row
        // label.
        //
        // Direction-aware announcement: announce the column header
        // ONLY when the column index changed since the last virtual
        // move; announce the row label ("Row N") ONLY when the row
        // index changed. Matches the JAWS table-reading idiom: in
        // vertical movement you hear just the value (assuming the
        // column is implied by context); in horizontal movement you
        // hear the column header first; in jumps to corners you
        // hear both row and column.
        //
        // The cursor is coupled to the ListView's row selection:
        // virtual row changes update the ListView selection (so the
        // user can see the row they're virtually browsing), and
        // ListView selection changes (from normal Down/Up arrows or
        // from a Jump/Find result) update the virtual row.
        private int iVirtualRow = 1;  // 1-based, matches ADO absolutePosition
        private int iVirtualCol = 0;  // 0-based, matches ListView column index
        private int iPrevVirtualRow = -1;
        private int iPrevVirtualCol = -1;

        // Spell-on-second-press: tracks the most recent speech-only
        // chord and its timestamp. If the user presses the same
        // chord within DoublePressMillis of the prior press, the
        // helper spells the spoken text instead of repeating it
        // verbatim. EdSharp/FileDir use the same convention for
        // their speech-only commands (Say Title, Say Status, etc.)
        //
        // The key is the chord's Keys value cast to int (using
        // System.Windows.Forms.Keys directly here, since this
        // entire form is already importing System.Windows.Forms).
        // Cleared on any non-speech key event so a stray Tab or
        // arrow press resets the double-press state.
        private int  iLastSpeechChord = 0;
        private long iLastSpeechTicks = 0;
        private const int DoublePressMillis = 1500;

        // True after we've announced "Table has no rows" for the
        // current empty state. Prevents re-announcing on every
        // refresh while the user is still on the empty table.
        // Reset when the table changes OR the row count becomes
        // non-zero.
        private bool bAnnouncedEmpty = false;

        // Search-family state. Three independent search families with
        // separate "last-used" state for each, plus a single
        // sLastSearchKind tracker so F3 / Shift+F3 can repeat
        // whichever family was last invoked (per Jamal's design
        // spec; EdSharp's "one pair of find-again chords for both
        // plain and regex" generalized to three).
        //
        //   Jump-Record (Control+J / Control+Shift+J): substring
        //   match scoped to ONE user-picked column. Useful for
        //   "show me the row where Email contains 'jamal'." The
        //   dialog has a column listbox and a substring textbox.
        //
        //   Find (Control+F / Control+Shift+F): substring match
        //   across ALL visible columns. Useful for "show me any row
        //   that mentions 'urgent' somewhere." Dialog has just a
        //   substring textbox.
        //
        //   Find-Regex (Control+F3 / Control+Shift+F3): regex match
        //   across ALL visible columns. Dialog has just a regex
        //   textbox.
        //
        // The legacy ADO-Find dialog used to be column-scoped via
        // ADO Find syntax (e.g. "lastName LIKE '%Smith%'") -- that
        // facility is preserved via Invoke-Sql, where users with
        // SQL fluency can run a parameterized WHERE-clause query.
        // The dot-prompt 'find' alias now routes to the new
        // column-listbox Jump-Record dialog.
        private string sLastJumpColumn = "";
        private string sLastJumpSubstring = "";
        private string sLastFindSubstring = "";
        private string sLastFindRegex = "";
        // Case-sensitive flag per search family. Tracks the most
        // recent setting the user chose in each family's dialog.
        // Defaults to false everywhere (the standard "case-
        // insensitive by default" convention).
        private bool bLastJumpCaseSensitive = false;
        private bool bLastFindCaseSensitive = false;
        private bool bLastRegexCaseSensitive = false;
        // sLastSearchKind tracks which family was last invoked:
        // "" (none yet), "jump", "find", "regex".
        private string sLastSearchKind = "";

        // Search-family state for the GUI's three search dialogs.
        // Each family tracks its own "last term" so Control+F, Control+J,
        // and Control+F3 each remember what was typed into them. The
        // unified F3 / Shift+F3 dispatcher reads sLastSearchKind to
        // decide which family's last term to repeat.

        // ------- Menu items (all alphabetical within their menu) -------
        private ToolStripMenuItem miFile;
        private ToolStripMenuItem miFileNew;
        private ToolStripMenuItem miFileOpen;
        private ToolStripMenuItem miFileRecent;
        private ToolStripMenuItem miFileSaveAs;
        private ToolStripMenuItem miFileClose;
        private ToolStripMenuItem miFileBackup;
        private ToolStripMenuItem miFileCompare;
        private ToolStripMenuItem miFileImport;
        private ToolStripMenuItem miFileExport;
        private ToolStripMenuItem miFilePrint;
        private ToolStripMenuItem miFileExit;

        private ToolStripMenuItem miRecNew;
        private ToolStripMenuItem miRecSet;
        private ToolStripMenuItem miRecRemove;
        private ToolStripMenuItem miRecShow;
        private ToolStripMenuItem miRecCopy;
        // Search-family menu items. Three families (Jump / Find /
        // Find-Regex) plus the F3 / Shift+F3 unified "again" pair.
        // The legacy miRecFindNext / miRecFindRegexNext fields are
        // replaced by the unified miRecSearchAgain / miRecSearchPrev
        // since both Jump-Again and Find-Regex-Again now go through
        // the kind-aware dispatcher.
        private ToolStripMenuItem miRecFind;          // Control+F: Find (across all columns)
        private ToolStripMenuItem miRecFindPrev;      // Control+Shift+F: Find reverse
        private ToolStripMenuItem miRecJump;          // Control+J: Jump-Record (one-column)
        private ToolStripMenuItem miRecJumpPrev;      // Control+Shift+J: Jump reverse
        private ToolStripMenuItem miRecFindRegex;     // Control+F3: Find-Regex (across all columns)
        private ToolStripMenuItem miRecFindRegexPrev; // Control+Shift+F3: Find-Regex reverse
        private ToolStripMenuItem miRecSearchAgain;   // F3: repeat last search (whichever family)
        private ToolStripMenuItem miRecSearchPrev;    // Shift+F3: repeat last search reverse
        private ToolStripMenuItem miRecMark;
        private ToolStripMenuItem miRecUnmark;
        private ToolStripMenuItem miRecRelated;
        private ToolStripMenuItem miRecEnterChild;
        private ToolStripMenuItem miRecExitChild;
        private ToolStripMenuItem miRecExitChildToRoot;
        private ToolStripMenuItem miRecUpdateField;
        private ToolStripMenuItem miRecGoTo;
        private ToolStripMenuItem miRecBookmark;
        private ToolStripMenuItem miRecGotoBookmark;
        private ToolStripMenuItem miRecClearBookmark;
        private ToolStripMenuItem miRecOpenCell;

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

        private ToolStripMenuItem miSchemaSelectTable;
        private ToolStripMenuItem miSchemaSelectView;
        private ToolStripMenuItem miSchemaSwitch;
        private ToolStripMenuItem miSchemaSwitchPrev;
        private ToolStripMenuItem miSchemaSwitchAll;
        private ToolStripMenuItem miSchemaSwitchAllPrev;
        private ToolStripMenuItem miSchemaShow;
        private ToolStripMenuItem miSchemaProperties;

        private ToolStripMenuItem miToolsTest;
        private ToolStripMenuItem miToolsMeasure;
        private ToolStripMenuItem miToolsChart;
        private ToolStripMenuItem miToolsLock;
        private ToolStripMenuItem miToolsTestDriver;
        private ToolStripMenuItem miToolsOpenFolder;
        private ToolStripMenuItem miToolsConsole;
        private ToolStripMenuItem miToolsInvokeSql;
        private ToolStripMenuItem miToolsEditConfig;
        private ToolStripMenuItem miMiscInvokeSnippet;
        private ToolStripMenuItem miMiscEditSnippet;
        private ToolStripMenuItem miMiscOpenSnippetFolder;

        private ToolStripMenuItem miHelp;
        private ToolStripMenuItem miHelpContents;
        private ToolStripMenuItem miHelpHistory;
        private ToolStripMenuItem miHelpReadme;
        private ToolStripMenuItem miHelpVerbs;
        private ToolStripMenuItem miHelpShowCommand;
        private ToolStripMenuItem miHelpStatus;
        private ToolStripMenuItem miHelpTestReader;
        private ToolStripMenuItem miHelpSampleDb;
        private ToolStripMenuItem miHelpExtraSpeech;
        // (miHelpRunScript removed in v1.0.44. The snippet menu items
        // miMiscInvokeSnippet / miMiscEditSnippet / miMiscOpenSnippetFolder
        // live in the Misc menu, not Help.)
        private ToolStripMenuItem miHelpTraceCommand;
        private ToolStripMenuItem miHelpLog;
        private ToolStripMenuItem miHelpWebSite;
        private ToolStripMenuItem miHelpElevate;
        private ToolStripMenuItem miHelpAbout;

        // Top-level menus added for the FileDir-style layout
        // (File / Edit / Navigate / Query / Misc / Help). The
        // older miRecord / miView / miSchema / miTools menus are
        // retired; their leaf items are reparented under the new
        // top-level menus by category. The miRec* / miView* / miSchema* /
        // miTools* field names are kept as-is throughout the class --
        // they're internal identifiers, not user-visible strings.
        private ToolStripMenuItem miEdit;
        private ToolStripMenuItem miNavigate;
        private ToolStripMenuItem miQuery;
        private ToolStripMenuItem miMisc;

        // Navigate menu's step commands gain explicit GUI homes so
        // First / Last / Next / Previous have a discoverable
        // location. The CLI versions already exist as cmdStepRecord
        // routes; these GUI items simply call into the same logic.
        private ToolStripMenuItem miNavFirst;
        private ToolStripMenuItem miNavLast;
        private ToolStripMenuItem miNavNext;
        private ToolStripMenuItem miNavPrev;

        // Speech-only commands (Query menu's "Say..." family). Each
        // pushes a state summary through LiveRegion without changing
        // focus, selection, or recordset position. FileDir-style.
        private ToolStripMenuItem miSaySayStatus;
        private ToolStripMenuItem miSaySayPath;
        private ToolStripMenuItem miSaySayYield;
        private ToolStripMenuItem miSaySayTables;
        private ToolStripMenuItem miSaySayMarked;
        private ToolStripMenuItem miSaySayDate;
        private ToolStripMenuItem miSaySayType;
        private ToolStripMenuItem miSaySayYieldMarked;

        // Action commands added this turn to align with FileDir
        // Shift+Letter and EdSharp Control+Shift+E patterns.
        private ToolStripMenuItem miCopyRow;
        private ToolStripMenuItem miStepInitialChange;
        private ToolStripMenuItem miExtractRegex;

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
            // Load Extra-Speech setting from DbDuo.ini [General].
            // Default ON (Y) on first launch. Off explicitly via "N".
            string sExtra = IniSession.read("General", "extraSpeech");
            LiveRegion.bExtraSpeechEnabled = string.IsNullOrEmpty(sExtra)
                || !sExtra.Equals("N", StringComparison.OrdinalIgnoreCase);
            applyIniOverrides();
            updateMenuEnabled();
            updateStatusBar();
            if (miHelpExtraSpeech != null) miHelpExtraSpeech.Checked = LiveRegion.bExtraSpeechEnabled;
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

        // OnFormClosing: persist per-table state to the RecentFiles
        // ini block so the next launch's Recent Files dialog can
        // restore the user's view. Captures filter / sort / position
        // for the currently-active table. Other tables that were
        // visited during the session keep whatever state they had on
        // last save; full multi-table-state tracking would require
        // hooks at every Select-Table call, which we don't yet have.
        protected override void OnFormClosing(FormClosingEventArgs oArgs)
        {
            try
            {
                if (db != null && db.isOpen() && !string.IsNullOrEmpty(db.filePath)
                    && !string.IsNullOrEmpty(db.currentTable))
                {
                    string sFilter = "";
                    string sSort = "";
                    int    iPos  = 1;
                    try { sFilter = db.filter ?? ""; } catch { }
                    try { sSort   = db.sort   ?? ""; } catch { }
                    try { iPos    = db.absolutePosition; } catch { }
                    RecentFiles.recordTableState(db.filePath, db.currentTable,
                        sFilter, sSort, iPos);
                }
            }
            catch { /* never block form close */ }
            base.OnFormClosing(oArgs);
        }

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
        // for the Control+arrow / Control+Home/End mark-navigation
        // chords and the Shift+Home / Shift+End / Alt+Shift+Home /
        // Alt+Shift+End bulk-mark chords, because either family can
        // be intercepted by the MenuStrip on its way through normal
        // dispatch. Routing them here first guarantees they get to
        // the form-level handlers.
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
            // Control+Home/End/Up/Down for marked-row navigation.
            // Matches FileDir's convention for tagged-record nav
            // (Control+Home/End to first/last tagged, Control+Up/Down
            // to previous/next tagged). The ListView's native handling
            // of Control+Up/Down only moves the focus indicator (not
            // the selection) in our MultiSelect=false config, so we
            // are not conflicting with anything the user can see.
            // Control+Home/End in MultiSelect=false ListView behaves
            // the same as Home/End -- the Control modifier is a no-op
            // there, also fair game.
            if (oKey == (Keys.Control | Keys.Home))
            { oArgs.Handled = true; oArgs.SuppressKeyPress = true; jumpToMarkedRow(MarkJump.First);    return; }
            if (oKey == (Keys.Control | Keys.End))
            { oArgs.Handled = true; oArgs.SuppressKeyPress = true; jumpToMarkedRow(MarkJump.Last);     return; }
            if (oKey == (Keys.Control | Keys.Up))
            { oArgs.Handled = true; oArgs.SuppressKeyPress = true; jumpToMarkedRow(MarkJump.Previous); return; }
            if (oKey == (Keys.Control | Keys.Down))
            { oArgs.Handled = true; oArgs.SuppressKeyPress = true; jumpToMarkedRow(MarkJump.Next);     return; }

            // Shift+Home / Shift+End / Alt+Shift+Home / Alt+Shift+End
            // for bulk marking. Shift+Home marks every row from the
            // first up to and including the current; Shift+End marks
            // every row from the current to the last. Alt+Shift+Home
            // and Alt+Shift+End unmark the same spans. FileDir uses
            // the same chord family for tagging spans of files. The
            // ListView's MultiSelect=false config makes Shift+Home/End
            // a no-op natively, so we are not stealing anything.
            if (oKey == (Keys.Shift | Keys.Home))
            { oArgs.Handled = true; oArgs.SuppressKeyPress = true; bulkMark(BulkMark.MarkToStart);   return; }
            if (oKey == (Keys.Shift | Keys.End))
            { oArgs.Handled = true; oArgs.SuppressKeyPress = true; bulkMark(BulkMark.MarkToEnd);     return; }
            if (oKey == (Keys.Alt | Keys.Shift | Keys.Home))
            { oArgs.Handled = true; oArgs.SuppressKeyPress = true; bulkMark(BulkMark.UnmarkToStart); return; }
            if (oKey == (Keys.Alt | Keys.Shift | Keys.End))
            { oArgs.Handled = true; oArgs.SuppressKeyPress = true; bulkMark(BulkMark.UnmarkToEnd);   return; }

            // FileDir-style "tag and move" chords. These only fire
            // when the data list has focus, so that typing > or <
            // (or Shift+UpArrow/DownArrow) inside a dialog text box
            // works as normal character/selection input.
            //
            //   >                       = Mark and next
            //   <                       = Unmark and next
            //   Shift+DownArrow         = Mark and next (alternate)
            //   Shift+UpArrow           = Mark and previous
            //   Alt+Shift+DownArrow     = Unmark and next
            //   Alt+Shift+UpArrow       = Unmark and previous
            //
            // The ListView's MultiSelect=false config makes
            // Shift+UpArrow / Shift+DownArrow no-ops natively, so
            // we can claim them. Alt+Shift+ arrows have no native
            // ListView meaning either.
            bool bGridFocused = (this.ActiveControl == grid);
            if (bGridFocused)
            {
                if (oKey == (Keys.Shift | Keys.OemPeriod))
                { oArgs.Handled = true; oArgs.SuppressKeyPress = true; markAndMove(true,  true);  return; }
                if (oKey == (Keys.Shift | Keys.Oemcomma))
                { oArgs.Handled = true; oArgs.SuppressKeyPress = true; markAndMove(false, true);  return; }
                if (oKey == (Keys.Shift | Keys.Down))
                { oArgs.Handled = true; oArgs.SuppressKeyPress = true; markAndMove(true,  true);  return; }
                if (oKey == (Keys.Shift | Keys.Up))
                { oArgs.Handled = true; oArgs.SuppressKeyPress = true; markAndMove(true,  false); return; }
                if (oKey == (Keys.Alt | Keys.Shift | Keys.Down))
                { oArgs.Handled = true; oArgs.SuppressKeyPress = true; markAndMove(false, true);  return; }
                if (oKey == (Keys.Alt | Keys.Shift | Keys.Up))
                { oArgs.Handled = true; oArgs.SuppressKeyPress = true; markAndMove(false, false); return; }

                // ? (Shift+Slash) = Show-Where. Speaks the title
                // bar, the status bar, and the current row's
                // displayed columns. The "where am I?" question is
                // a frequent need for screen-reader users; making
                // it a single character keeps the answer fast.
                if (oKey == (Keys.Shift | Keys.OemQuestion))
                { oArgs.Handled = true; oArgs.SuppressKeyPress = true; sayWhere(); return; }
            }

            // Control+A / Control+Shift+A / Control+I work whether
            // or not the data list has focus, because they don't
            // collide with anything in our edit dialogs (Control+A
            // is "select all" in a text box, but those text boxes
            // are inside modal dialogs that own their own key flow;
            // we only intercept here when the dialog is not up).
            // We still gate by data-list focus to be safe.
            if (bGridFocused)
            {
                if (oKey == (Keys.Control | Keys.A))
                { oArgs.Handled = true; oArgs.SuppressKeyPress = true; markAll(true);   return; }
                if (oKey == (Keys.Control | Keys.Shift | Keys.A))
                { oArgs.Handled = true; oArgs.SuppressKeyPress = true; markAll(false);  return; }
                if (oKey == (Keys.Control | Keys.I))
                { oArgs.Handled = true; oArgs.SuppressKeyPress = true; invertMarks();   return; }
            }
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

        // invokeExitChildToRoot: pop the entire drill stack and
        // return the number of levels popped. Marshalled to the GUI
        // thread so the CLI's cmdExitChildToRoot can call it.
        public int invokeExitChildToRoot()
        {
            if (this.IsDisposed) return -1;
            int iBefore = oDrillStack.Count;
            try
            {
                this.Invoke(new Action(() => recExitChildToRootClicked(this, EventArgs.Empty)));
            }
            catch { }
            return iBefore - oDrillStack.Count;
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

            // FileDir-style top-level menu layout: File / Edit /
            // Navigate / Query / Misc / Help. Each leaf item keeps
            // its existing variable name (miRec*, miView*, miSchema*,
            // miTools*) so all the enable/disable code elsewhere in
            // the class continues to work unchanged.

            // ===== File menu: database files + cross-file movement =====
            miFile = addMenu("&File");
            miFileNew     = addItem(miFile, "&New Database...",           "New-Database",     Keys.Control | Keys.Shift | Keys.N,   fileNewClicked);
            miFileOpen    = addItem(miFile, "&Open Database...",          "Open-Database",    Keys.Control | Keys.O,                fileOpenClicked);
            miFileRecent  = addItem(miFile, "&Recent Files...",           "Recent-Files",     Keys.Alt | Keys.R,                    fileRecentClicked);
            miFileSaveAs  = addItem(miFile, "&Save Database As...",       "Save-DatabaseAs",  Keys.Control | Keys.S,                fileSaveAsClicked);
            miFileClose   = addItem(miFile, "&Close Database",            "Close-Database",   Keys.Control | Keys.F4,               fileCloseClicked);
            addSep(miFile);
            miFileBackup  = addItem(miFile, "&Backup Database...",        "Backup-Database",  Keys.Control | Keys.Shift | Keys.S,   fileBackupClicked);
            miFileCompare = addItem(miFile, "Co&mpare Database...",       "Compare-Database", Keys.None,                            fileCompareClicked);
            addSep(miFile);
            miFileImport  = addItem(miFile, "&Import Data...",            "Import-Data",      Keys.Control | Keys.Shift | Keys.I,   fileImportClicked);
            miFileExport  = addItem(miFile, "&Export Data...",            "Export-Data",      Keys.Control | Keys.Shift | Keys.X,   fileExportClicked);
            addSep(miFile);
            miFilePrint   = addItem(miFile, "&Print...",                  "Out-Printer",      Keys.Control | Keys.P,                filePrintClicked);
            addSep(miFile);
            // Switch-Table family lives in File because choosing
            // which file/table is on screen is a file-level operation,
            // and EdSharp's File menu carries Current Windows (F4)
            // by the same logic.
            miSchemaSelectTable = addItem(miFile, "Choose &Table...",           "Select-Table",         Keys.F4,                            schemaSelectTableClicked);
            miSchemaSelectView  = addItem(miFile, "Choose &View...",            "Select-View",          Keys.None,                          schemaSelectViewClicked);
            miSchemaSwitch      = addItem(miFile, "Next Visited Table",         "Switch-Table",         Keys.Control | Keys.Tab,            schemaSwitchClicked);
            miSchemaSwitchPrev  = addItem(miFile, "Previous Visited Table",     "Switch-TablePrevious", Keys.Control | Keys.Shift | Keys.Tab, schemaSwitchPrevClicked);
            miSchemaSwitchAll     = addItem(miFile, "Next Table or View",     "Switch-Object",         Keys.Control | Keys.F6,            schemaSwitchAllClicked);
            miSchemaSwitchAllPrev = addItem(miFile, "Previous Table or View", "Switch-ObjectPrevious", Keys.Control | Keys.Shift | Keys.F6, schemaSwitchAllPrevClicked);
            addSep(miFile);
            miFileExit    = addItem(miFile, "E&xit DbDuo",                "Exit-Application", Keys.Alt | Keys.F4,                   fileExitClicked);

            // ===== Edit menu: modify the data =====
            miEdit = addMenu("&Edit");
            miRecNew         = addItem(miEdit, "&New Record...",                          "New-Record",          Keys.Control | Keys.N,              recNewClicked);
            miRecSet         = addItem(miEdit, "&Edit Record...",                         "Set-Record",          Keys.F2,                            recSetClicked);
            miRecRemove      = addItem(miEdit, "&Delete Record",                          "Remove-Record",       Keys.Control | Keys.D,              recRemoveClicked);
            miRecCopy        = addItem(miEdit, "Duplicate &Record (copy as new)",         "Copy-Record",         Keys.Control | Keys.Shift | Keys.C, recCopyClicked);
            miRecUpdateField = addItem(miEdit, "Find and &Replace Across Rows...",        "Update-Field",        Keys.Control | Keys.R,              recUpdateFieldClicked);
            addSep(miEdit);
            // Marks: per-row boolean flags. Control+M / Control+U is
            // the canonical chord pair, chosen for symmetry (mark and
            // unmark differ only by the letter, not by an added
            // modifier).
            miRecMark        = addItem(miEdit, "&Mark Record",                            "Set-Mark",            Keys.Control | Keys.M,              recMarkClicked);
            miRecUnmark      = addItem(miEdit, "&Unmark Record",                          "Clear-Mark",          Keys.Control | Keys.U,              recUnmarkClicked);
            addSep(miEdit);
            // Bookmarks: EdSharp's Set/Clear/Go-to Bookmark trio
            // on Control+K / Control+Shift+K / Alt+K. Identical
            // chord family in DbDuo.
            miRecBookmark    = addItem(miEdit, "&Save Bookmark",                          "Save-Bookmark",       Keys.Control | Keys.K,              recBookmarkClicked);
            miRecGotoBookmark= addItem(miEdit, "&Go to Bookmark",                         "Restore-Bookmark",    Keys.Alt | Keys.K,                  recGotoBookmarkClicked);
            miRecClearBookmark=addItem(miEdit, "&Clear Bookmark",                         "Clear-Bookmark",      Keys.Control | Keys.Shift | Keys.K, recClearBookmarkClicked);
            addSep(miEdit);
            // Open Cell Value: open the URL, file path, or folder
            // path stored in a cell of the current row.
            miRecOpenCell    = addItem(miEdit, "&Open Cell Value (URL or path)...",       "Open-Cell",           Keys.Control | Keys.Enter,          recOpenCellClicked);

            // ===== Navigate menu: move around the data =====
            miNavigate = addMenu("&Navigate");
            // Step-Record family: First / Last / Next / Previous.
            // Names follow EdSharp/FileDir convention; chords are
            // unset (the listview's arrow keys handle next/previous
            // and Control+Home/End handle first/last natively).
            miNavFirst       = addItem(miNavigate, "&First Record",                          "Step-Record-First",    Keys.None,                          navFirstClicked);
            miNavLast        = addItem(miNavigate, "&Last Record",                           "Step-Record-Last",     Keys.None,                          navLastClicked);
            miNavNext        = addItem(miNavigate, "&Next Record",                        "Step-Record-Next",     Keys.None,                          navNextClicked);
            miNavPrev        = addItem(miNavigate, "&Previous Record",                    "Step-Record-Previous", Keys.None,                          navPrevClicked);
            miRecGoTo        = addItemLocal(miNavigate, "&Go to Row...",                  "Set-Position",         Keys.Shift | Keys.G,                recGoToClicked);
            addSep(miNavigate);
            // Search families. Three distinct families with their
            // own chord pairs, plus a unified F3 / Shift+F3 "repeat
            // last search" dispatcher that routes to whichever
            // family was last used.
            //
            //   Jump to Match (Control+J / Control+Shift+J): substring
            //   match scoped to ONE column the user picks from a
            //   listbox in the prompt dialog. dbDot heritage.
            //
            //   Find (Control+F / Control+Shift+F): substring match
            //   ACROSS ALL visible columns. The universal Office /
            //   browser Control+F idiom.
            //
            //   Find Regex (Control+F3 / Control+Shift+F3): .NET
            //   regex match across ALL visible columns.
            //
            // F3 / Shift+F3 repeat whichever family was most recently
            // invoked; sLastSearchKind routes the dispatch.
            miRecFind         = addItem(miNavigate, "&Find Across All Columns...",                 "Find",                Keys.Control | Keys.F,              recFindAllClicked);
            miRecFindPrev     = addItem(miNavigate, "Find &Previous Across All Columns",           "Find-Previous",       Keys.Control | Keys.Shift | Keys.F, recFindAllPrevClicked);
            miRecJump         = addItem(miNavigate, "&Jump to Match in One Column...",             "Jump-Record",         Keys.Control | Keys.J,              recJumpClicked);
            miRecJumpPrev     = addItem(miNavigate, "Jump to Previous Match in One Column",        "Jump-RecordPrevious", Keys.Control | Keys.Shift | Keys.J, recJumpPrevClicked);
            miRecFindRegex    = addItem(miNavigate, "Find &Regex Across All Columns...",           "Find-Regex",          Keys.Control | Keys.F3,             recFindRegexClicked);
            miRecFindRegexPrev = addItem(miNavigate, "Find Previous Regex Across All Columns",     "Find-RegexPrevious",  Keys.Control | Keys.Shift | Keys.F3, recFindRegexPrevClicked);
            miRecSearchAgain  = addItem(miNavigate, "Search &Next (repeat last search forward)",   "Search-Next",         Keys.F3,                            recSearchNextClicked);
            miRecSearchPrev   = addItem(miNavigate, "&Search Previous (repeat last search reverse)", "Search-Previous",   Keys.Shift | Keys.F3,               recSearchPrevClicked);
            addSep(miNavigate);
            // Parent-child drill family. Alt+RightArrow enters a
            // child table from the current row's foreign-key target;
            // Alt+LeftArrow exits back to the parent row that drilled
            // in. Alt+Home pops all the way back to the topmost
            // ancestor.
            miRecEnterChild      = addItem(miNavigate, "&Enter Child Table (drill into related rows)...", "Enter-Child",      Keys.Alt | Keys.Right, recEnterChildClicked);
            miRecExitChild       = addItem(miNavigate, "E&xit Child Table (return to parent row)",        "Exit-Child",       Keys.Alt | Keys.Left,  recExitChildClicked);
            miRecExitChildToRoot = addItem(miNavigate, "Exit to &Root Table (pop entire drill stack)",    "Exit-ChildToRoot", Keys.Alt | Keys.Home,  recExitChildToRootClicked);

            // ===== Query menu: read aspects of the data =====
            miQuery = addMenu("&Query");
            // Read-only inspection of current record / table / schema.
            miRecShow        = addItem(miQuery, "&Show Record (field-by-field)",  "Show-Object",   Keys.Enter,                       recShowClicked);
            miSchemaProperties  = addItem(miQuery, "Table &Properties",              "Get-Property",  Keys.Alt | Keys.Enter,            schemaPropertiesClicked);
            miRecRelated     = addItem(miQuery, "&Related Records (follow FK)...",   "Show-Related",  Keys.None,                        recRelatedClicked);
            miSchemaShow     = addItem(miQuery, "&Show Schema (all tables)",         "Show-Schema",   Keys.None,                        schemaShowClicked);
            addSep(miQuery);
            // Say-X family: speaks state without changing focus or
            // recordset position. FileDir's Say-X family is the model.
            miSaySayStatus       = addItem(miQuery, "Say Status (table, row, filter, sort)",   "Say-Status",       Keys.Alt | Keys.Z,    saySayStatus);
            miSaySayPath         = addItem(miQuery, "Say Path (database file path)",           "Say-Path",         Keys.Alt | Keys.P,    saySayPath);
            miSaySayYield        = addItem(miQuery, "Say Yield (row count and filter)",        "Say-Yield",        Keys.Alt | Keys.Y,    saySayYield);
            miSaySayTables       = addItem(miQuery, "Say Tables (visited this session)",      "Say-Tables",        Keys.Shift | Keys.F4, saySayTables);
            miSaySayMarked       = addItem(miQuery, "Say Marked (look-values of marked rows)", "Say-Marked",       Keys.Shift | Keys.L,  saySayMarked);
            miSaySayDate         = addItem(miQuery, "Say Date (updated value of current row)", "Say-Date",         Keys.Shift | Keys.D,  saySayDate);
            miSaySayType         = addItem(miQuery, "Say Type (table or view, row position)",  "Say-Type",         Keys.Shift | Keys.T,  saySayType);
            miSaySayYieldMarked  = addItem(miQuery, "Say Marked Yield (count of marked rows)", "Say-YieldMarked",  Keys.Shift | Keys.Y,  saySayYieldMarked);
            addSep(miQuery);
            // Filter / Sort -- data-shaping commands. FileDir's
            // Alpha Order / Date Order chord block on Alt+A / Alt+D
            // is the model; DbDuo's Sort Ascending / Sort Oldest First
            // mirror those exactly.
            miViewSelect     = addItemLocal(miQuery, "&Filter Records...",                          "Select-Record",     Keys.Shift | Keys.F,                viewSelectClicked);
            miViewResetFilter= addItemLocal(miQuery, "&Clear Filter",                               "Reset-Filter",      Keys.Shift | Keys.R,                viewResetFilterClicked);
            addSep(miQuery);
            miViewFormat     = addItemLocal(miQuery, "Custom &Sort...",                             "Sort-Object",       Keys.Shift | Keys.S,                viewFormatClicked);
            miViewSortAsc    = addItem(miQuery, "Sort &Ascending by Column (alpha)...",             "Sort-Ascending",    Keys.Alt | Keys.A,                  viewSortAscClicked);
            miViewSortDesc   = addItem(miQuery, "Sort &Descending by Column (alpha)...",            "Sort-Descending",   Keys.Alt | Keys.Shift | Keys.A,     viewSortDescClicked);
            miViewSortRecent = addItem(miQuery, "Sort by Date Updated (most recent first)",         "Sort-RecentFirst",  Keys.Alt | Keys.Shift | Keys.D,     viewSortRecentClicked);
            miViewSortOldest = addItem(miQuery, "Sort by Date Updated (oldest first)",              "Sort-OldestFirst",  Keys.Alt | Keys.D,                  viewSortOldestClicked);
            miViewResetSort  = addItem(miQuery, "Clear Sor&t",                                      "Reset-Sort",        Keys.None,                          viewResetSortClicked);

            // ===== Misc menu: utilities, tools, settings =====
            miMisc = addMenu("&Misc");
            miViewUpdate     = addItem(miMisc, "&Refresh View",                          "Update-View",       Keys.F5,                            viewUpdateClicked);
            miToolsLock      = addItem(miMisc, "Toggle Read-On&ly Lock",                 "Lock-Database",     Keys.Control | Keys.F7,             toolsLockClicked);
            addSep(miMisc);
            // Analytical utilities.
            miToolsMeasure   = addItem(miMisc, "Table &Statistics",                      "Measure-Table",     Keys.None,                          toolsMeasureClicked);
            miToolsChart     = addItem(miMisc, "Frequency &Chart (column to Excel)...",  "New-Chart",         Keys.None,                          toolsChartClicked);
            miViewSelectColumn = addItem(miMisc, "Choose &Visible Columns...",           "Select-Column",     Keys.None,                          viewSelectColumnClicked);
            // Extract Matches: walk every visible row, find every
            // regex match across every visible column, copy matches
            // to the clipboard. Alt+E ("E for Extract").
            miExtractRegex   = addItem(miMisc, "&Extract Regex Matches to Clipboard...", "Extract-Regex",     Keys.Alt | Keys.E,                  extractRegexClicked);
            addSep(miMisc);
            // List-navigation operations from FileDir's Shift+A / Shift+I.
            miCopyRow        = addItem(miMisc, "Copy Ro&w as TSV to Clipboard",          "Copy-Row",          Keys.Shift | Keys.A,                copyRowClicked);
            miStepInitialChange = addItem(miMisc, "Next &Initial Change (column-aware)...", "Step-InitialChange", Keys.Shift | Keys.I,             stepInitialChangeClicked);
            addSep(miMisc);
            miToolsInvokeSql = addItem(miMisc, "Run S&QL...",                            "Invoke-Sql",        Keys.Control | Keys.Q,              toolsInvokeSqlClicked);
            miToolsTest      = addItem(miMisc, "&Test Integrity",                        "Test-Database",     Keys.None,                          toolsTestClicked);
            miToolsTestDriver= addItem(miMisc, "Test &Drivers (ODBC and OLE DB)",        "Test-Driver",       Keys.None,                          toolsTestDriverClicked);
            addSep(miMisc);
            miToolsOpenFolder= addItem(miMisc, "Open in E&xplorer",                      "Open-FileFolder",   Keys.Alt | Keys.OemPipe,            toolsOpenFolderClicked);
            miToolsConsole   = addItem(miMisc, "Open D&ot Prompt",                       "Enter-Console",     Keys.Control | Keys.Oemtilde,       toolsConsoleClicked);
            addSep(miMisc);
            // Snippet family. Adapted from EdSharp's Invoke / View
            // Snippet pattern. Snippets are plain files in
            // %APPDATA%\DbDuo\Snippets; .js files are executed as
            // JScript .NET via dbDuoEval.dll, all other extensions
            // are displayed as reference text in a MessageBox.
            //
            // No Save-Snippet command: DbDuo is not a text editor, so
            // there is no "current selection" to save. Edit Snippet
            // handles both modifying an existing snippet and creating
            // a new one (via a "[New snippet...]" entry at the top of
            // the pick list).
            miMiscInvokeSnippet     = addItem(miMisc, "In&voke Snippet...",                     "Invoke-Snippet",     Keys.Alt | Keys.V,                  miscInvokeSnippetClicked);
            miMiscEditSnippet       = addItem(miMisc, "Edit Sni&ppet...",                       "Edit-Snippet",       Keys.Alt | Keys.Shift | Keys.V,     miscEditSnippetClicked);
            miMiscOpenSnippetFolder = addItem(miMisc, "Open Snippet &Folder",                   "Open-SnippetFolder", Keys.None,                          miscOpenSnippetFolderClicked);
            addSep(miMisc);
            miToolsEditConfig= addItem(miMisc, "Edit Confi&guration (DbDuo.ini)",        "Edit-Configuration", Keys.F12,                          toolsEditConfigClicked);

            // ===== Help menu =====
            miHelp = addMenu("Hel&p");
            miHelpContents     = addItem(miHelp, "Help &Contents",                       "Get-Help",          Keys.F1,                            helpContentsClicked);
            // Shift+F1 -- Version History. EdSharp and FileDir
            // both use this chord for the same purpose.
            miHelpHistory      = addItem(miHelp, "Version &History",                     "Show-History",      Keys.Shift | Keys.F1,               helpHistoryClicked);
            miHelpReadme       = addItem(miHelp, "&Readme Guide",                        "Show-Readme",       Keys.None,                          helpReadmeClicked);
            // Open Sample Database: a one-keystroke tour entry point.
            // The installer ships sample.db (teachers, classes,
            // students, enrollments) alongside the executable; this
            // command opens it via the same code path File > Open
            // Database uses, so all the normal post-open behaviors
            // (filter restore, status announcement, etc.) apply.
            miHelpSampleDb     = addItem(miHelp, "Open &Sample Database",                "Open-SampleDatabase", Keys.None,                        helpSampleDbClicked);
            miHelpVerbs        = addItem(miHelp, "PowerShell &Verb Reference",           "Get-Verb",          Keys.None,                          helpVerbsClicked);
            addSep(miHelp);
            miHelpShowCommand  = addItem(miHelp, "Command &Picker (alternate menu)...",  "Show-Command",      Keys.Alt | Keys.F10,                helpShowCommandClicked);
            // Control+F1 = Key Describer mode toggle. EdSharp and
            // FileDir both use Control+F1 for "Key Describer."
            miHelpTraceCommand = addItem(miHelp, "Toggle &Key Describer Mode",           "Trace-Command",     Keys.Control | Keys.F1,             helpTraceCommandClicked);
            miHelpStatus       = addItem(miHelp, "&Where Am I",                            "Show-Status",       Keys.None,                          helpStatusClicked);
            miHelpTestReader   = addItem(miHelp, "&Test Screen Reader Speech",           "Test-Reader",       Keys.None,                          helpTestReaderClicked);
            // Toggle-Extra-Speech: silence DbDuo's direct speech
            // messages without affecting the screen reader's natural
            // focus and selection announcements. EdSharp/FileDir's
            // model. Alt+Shift+S is free (data-list Shift+S is the
            // local Sort handler; Alt+Shift+S is global).
            miHelpExtraSpeech  = addItem(miHelp, "Toggle E&xtra Speech",                 "Toggle-Extra-Speech", Keys.Alt | Keys.Shift | Keys.S,   helpExtraSpeechClicked);
            addSep(miHelp);
            miHelpLog          = addItem(miHelp, "Show &Log Location",                   "Show-Log",          Keys.None,                          helpLogClicked);
            miHelpWebSite      = addItem(miHelp, "Open We&bsite (DbDuo on GitHub)",      "Open-WebSite",      Keys.None,                          helpWebSiteClicked);
            // Elevate-Version: check GitHub for a newer DbDuo_setup.exe
            // and offer to download / install. EdSharp's F11 and
            // FileDir's F11 are the model.
            miHelpElevate      = addItem(miHelp, "Check for &Update...",                 "Elevate-Version",   Keys.F11,                           helpElevateClicked);
            miHelpAbout        = addItem(miHelp, "&About DbDuo",                         "About-DbDuo",       Keys.Alt | Keys.F1,                 helpAboutClicked);

            // Delete key as a secondary binding for Remove-Record.
            // The primary menu shortcut is Control+D (shown next to
            // the menu item); Delete is a long-standing grid-editor
            // idiom (Excel tables, Outlook lists).
            KeyMap.registerAlias(Keys.Delete, miRecRemove);

            // Get-Property: secondary alias on Shift+F6 (EdSharp's
            // "Go to Contents" -- structural where-am-I).
            KeyMap.registerAlias(Keys.Shift | Keys.F6, miSchemaProperties);

            // Jump-Record: secondary alias on Shift+J for muscle
            // Shift+J: data-list alias for Jump-Record (single-column
            // substring). Preserves the bare-Shift+Letter family
            // muscle memory from earlier DbDuo versions, and parallels
            // the canonical Control+J binding. Note this binds to
            // miRecJump (the new column-listbox Jump-Record), NOT to
            // miRecFind (which is now the across-all-columns Find).
            KeyMap.registerAlias(Keys.Shift | Keys.J, miRecJump);

            // Alt+Letter bindings that don't collide with main-menu
            // accelerators. ProcessCmdKey dispatches form-level chords
            // BEFORE menu accelerators are processed, so even Alt+P
            // (which is Help's menu accelerator) and Alt+E (Edit's)
            // can host commands -- the command wins. But to keep
            // documentation simple and avoid confusion, I prefer
            // Alt+Letter bindings on letters that aren't main-menu
            // accelerators in the first place. The main menus use
            // F (File), E (Edit), N (Navigate), Q (Query), M (Misc),
            // P (Help); the bindings below all use other letters.
            //
            //   Alt+R = Recent-Files (R for Recent) -- the menu item
            //           registration above on miFileRecent binds this
            //           globally; no registerAlias needed here.
            //   Alt+Shift+R = Show-Related (Related uses the modified
            //           form since Recent took the bare Alt+R)
            //   Alt+T = Measure-Table (T for Table)
            //   Alt+C = New-Chart (C for Chart)
            //   Alt+L = Show-Table (L for List)
            //
            // These are global aliases for commands whose canonical
            // menu home is a no-hotkey menu item; the Alt+Letter
            // chord gives them keyboard reach.
            KeyMap.registerAlias(Keys.Alt | Keys.Shift | Keys.R, miRecRelated);
            KeyMap.registerAlias(Keys.Alt | Keys.T, miToolsMeasure);
            KeyMap.registerAlias(Keys.Alt | Keys.C, miToolsChart);
            KeyMap.registerAlias(Keys.Alt | Keys.L, miSchemaSelectTable);

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
            // Wrap the click handler so that, when Command Echo is on,
            // the canonical command name is announced through the live
            // region before the command body runs. Acts as JAWS-style
            // confirmation that the chord landed on the intended item.
            oItem.Click += (oS, oA) => { commandEcho(sCommand); oHandler(oS, oA); };
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
            oItem.Click += (oS, oA) => { commandEcho(sCommand); oHandler(oS, oA); };
            oParent.DropDownItems.Add(oItem);
            KeyMap.registerDisplayOnly(oKey, oItem, sCommand);
            return oItem;
        }

        // Command Echo: speak the canonical command name through the
        // live region just before the command runs. Setting lives in
        // [Options] commandEcho; default Y. Off-by-toggle for users
        // who find the confirmation noisy. The Trace-Command mode
        // (Control+F1) is the more verbose cousin: trace announces
        // chord + name and SUPPRESSES the command body; echo
        // announces just the name and RUNS the body.
        private static bool bCommandEchoCached = false;
        private static bool bCommandEchoLoaded = false;
        public static bool isCommandEchoOn()
        {
            if (!bCommandEchoLoaded)
            {
                try
                {
                    string sV = readEchoSetting();
                    bCommandEchoCached = !sV.Equals("N", StringComparison.OrdinalIgnoreCase)
                                      && !sV.Equals("No", StringComparison.OrdinalIgnoreCase)
                                      && !sV.Equals("0", StringComparison.Ordinal)
                                      && !sV.Equals("False", StringComparison.OrdinalIgnoreCase);
                }
                catch { bCommandEchoCached = true; }
                bCommandEchoLoaded = true;
            }
            return bCommandEchoCached;
        }
        public static void invalidateCommandEchoCache() { bCommandEchoLoaded = false; }
        private static string readEchoSetting()
        {
            string sV = IniSession.read("Options", "commandEcho");
            return string.IsNullOrEmpty(sV) ? "Y" : sV;
        }
        private void commandEcho(string sCommand)
        {
            if (!isCommandEchoOn()) return;
            if (string.IsNullOrEmpty(sCommand)) return;
            try { LiveRegion.say(sCommand); } catch { /* swallow */ }
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
            // Show-Object, Copy-Record, and Set-Mark act on the
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
            ctxGrid.Items.Add("Show Record", null, recShowClicked);
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
            // Keep the virtual cursor's row in sync with the
            // ListView's row selection so plain Down/Up arrows
            // don't desync the two cursors. Column stays put.
            virtSyncFromListSelection();
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

            // Bare Shift+Letter shortcut family. Only these letters
            // are bound; any other Shift+Letter falls through to the
            // ListView, which feeds the character to its virtual-mode
            // SearchForVirtualItem for type-ahead navigation. The
            // corresponding menu items have ShortcutKeyDisplayString
            // set so the chord appears in the menu UI and JAWS
            // announces it; the dispatch happens here, not through
            // the form-level KeyMap.
            //
            // Shift+E and Shift+X are deliberately NOT bound -- the
            // Alt+RightArrow / Alt+LeftArrow chords for Enter-Child
            // and Exit-Child obviate the need for any Letter binding
            // on those commands, freeing the E and X slots for
            // future commands. Same reasoning for Shift+M / Shift+U:
            // Set-Mark / Clear-Mark live on Control+M / Control+U
            // for symmetric chord pairing, and the bare Letter slots
            // are reserved.
            if (oArgs.Shift && !oArgs.Control && !oArgs.Alt)
            {
                ToolStripMenuItem oTarget = null;
                switch (oArgs.KeyCode)
                {
                    case Keys.F: oTarget = miViewSelect;       break;
                    case Keys.G: oTarget = miRecGoTo;          break;
                    case Keys.J: oTarget = miRecJump;          break;
                    case Keys.R: oTarget = miViewResetFilter;  break;
                    case Keys.S: oTarget = miViewFormat;       break;
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

        // =====================================================================
        // VirtualCursor: cell-level navigation for screen-reader users.
        // Alt+Control+Home/End/arrows/Numpad5/PageUp/PageDown drive
        // the virtual cursor and announce the resulting cell.
        // =====================================================================

        // virtCurrentColumnName: returns the field-name string of
        // the column currently under virtual focus, or "" if there
        // is no usable column. Used as the default selection in
        // column-prompt LBC dialogs so the user can just press
        // Enter to accept their current virtual column.
        private string virtCurrentColumnName()
        {
            if (grid == null || grid.Columns.Count == 0) return "";
            int iCol = iVirtualCol;
            if (iCol < 0 || iCol >= grid.Columns.Count) iCol = 0;
            return grid.Columns[iCol].Text ?? "";
        }

        // virtCellValue: read the cell at (iRow, iCol) of the
        // currently-displayed recordset view. Returns "" on any
        // failure (out-of-range, type conversion error, COM
        // exception). iRow is 1-based to match ADO absolutePosition.
        private string virtCellValue(int iRow, int iCol)
        {
            if (db == null || !db.hasRecordset()) return "";
            if (grid == null || grid.Columns.Count == 0) return "";
            if (iCol < 0 || iCol >= grid.Columns.Count) return "";
            if (iRow < 1 || iRow > db.recordCount) return "";
            string sHeader = grid.Columns[iCol].Text;
            if (string.IsNullOrEmpty(sHeader)) return "";
            int iSavedPos = db.absolutePosition;
            try
            {
                if (iSavedPos != iRow) db.absolutePosition = iRow;
                return formatCellValue(db.getFieldValue(sHeader)) ?? "";
            }
            catch { return ""; }
        }

        // virtSyncListSelection: move the ListView's row selection
        // to the virtual row, so the user can see the row they are
        // virtually browsing. Suppresses the cell-changed handler's
        // spoken announcement (we'll do our own direction-aware one).
        private void virtSyncListSelection(int iRow)
        {
            if (grid == null) return;
            int iZero = iRow - 1; // ListView is 0-based
            if (iZero < 0 || iZero >= grid.VirtualListSize) return;
            bSuppressCellChanged = true;
            try
            {
                grid.SelectedIndices.Clear();
                grid.SelectedIndices.Add(iZero);
                grid.EnsureVisible(iZero);
                grid.FocusedItem = grid.Items[iZero];
                // Also reposition the ADO cursor so getFieldValue
                // in subsequent calls reads the right row.
                try { db.absolutePosition = iRow; } catch { }
            }
            finally { bSuppressCellChanged = false; }
        }

        // virtResetToTop: place the virtual cursor at row 1, column 0.
        // Called when a new table is opened, F5 refreshes, or the
        // user explicitly jumps to the top of the grid.
        public void virtResetToTop()
        {
            iVirtualRow = (db != null && db.hasRecordset() && db.recordCount > 0) ? 1 : 0;
            iVirtualCol = (grid != null && grid.Columns.Count > 0) ? 0 : -1;
            iPrevVirtualRow = -1;
            iPrevVirtualCol = -1;
            // Sync the ListView selection silently. No announcement
            // here -- the table-opened or refresh path handles that
            // through its own message.
            if (iVirtualRow >= 1) virtSyncListSelection(iVirtualRow);
        }

        // virtSyncFromListSelection: keep the virtual ROW in lockstep
        // with the ListView's row selection when the user moves with
        // regular arrows / mouse / Find / Jump. Virtual COL stays put.
        // Called from gridSelectedIndexChanged.
        private void virtSyncFromListSelection()
        {
            if (grid == null || grid.SelectedIndices.Count == 0) return;
            int iNewRow = grid.SelectedIndices[0] + 1;
            if (iNewRow != iVirtualRow)
            {
                iPrevVirtualRow = iVirtualRow;
                iVirtualRow = iNewRow;
                // Column doesn't change in this path.
            }
        }

        // virtMoveTo: move the virtual cursor to (iRow, iCol) and
        // announce the resulting cell. Column header is spoken when
        // iCol differs from iPrevVirtualCol; row label is spoken
        // when iRow differs from iPrevVirtualRow. If both differ
        // (corner jumps from Home / End), both are spoken.
        private void virtMoveTo(int iRow, int iCol)
        {
            if (db == null || !db.hasRecordset() || db.recordCount == 0)
            {
                LiveRegion.say("No rows");
                return;
            }
            if (grid == null || grid.Columns.Count == 0)
            {
                LiveRegion.say("No columns");
                return;
            }
            // Clamp to valid range.
            if (iRow < 1) iRow = 1;
            if (iRow > db.recordCount) iRow = db.recordCount;
            if (iCol < 0) iCol = 0;
            if (iCol >= grid.Columns.Count) iCol = grid.Columns.Count - 1;

            iPrevVirtualRow = iVirtualRow;
            iPrevVirtualCol = iVirtualCol;
            iVirtualRow = iRow;
            iVirtualCol = iCol;

            bool bRowChanged = (iRow != iPrevVirtualRow);
            bool bColChanged = (iCol != iPrevVirtualCol);

            // Sync ListView row selection on any row change.
            if (bRowChanged) virtSyncListSelection(iRow);

            string sHeader = grid.Columns[iCol].Text;
            string sValue = virtCellValue(iRow, iCol);
            if (string.IsNullOrEmpty(sValue)) sValue = "(blank)";

            // Build the announcement per JAWS table-reading conventions:
            //   horizontal move (col changed only): header + value
            //   vertical move (row changed only):   row label + value
            //   corner jump (both changed):         row + header + value
            //   no change (refresh, Numpad5):       header + value
            // The "Row N" label is the bare row number; tables don't
            // have row headers per se. JAWS speaks the row index in
            // a similar context.
            string sSpoken;
            if (bRowChanged && bColChanged)
            {
                sSpoken = "Row " + iRow + ", " + sHeader + ": " + sValue;
            }
            else if (bColChanged)
            {
                sSpoken = sHeader + ": " + sValue;
            }
            else if (bRowChanged)
            {
                sSpoken = "Row " + iRow + ": " + sValue;
            }
            else
            {
                // No change -- happens on Numpad5 single-press, or
                // when the user hits the same arrow at the edge.
                sSpoken = sHeader + ": " + sValue;
            }
            LiveRegion.say(sSpoken);
        }

        // virtSayCurrent: announce the current virtual cell. On a
        // second press within DoublePressMillis, spell the value
        // instead of saying it. EdSharp/FileDir convention -- the
        // user gets verbatim on one press and character-by-character
        // on two presses. Triggered by Alt+Control+Numpad5.
        //
        // The chord identity is the Keys value of the key combo so
        // each speech-only chord has its own double-press timer.
        private void virtSayCurrent(int iChordKey)
        {
            string sHeader = (grid != null && iVirtualCol >= 0 && iVirtualCol < grid.Columns.Count)
                ? grid.Columns[iVirtualCol].Text : "";
            string sValue = virtCellValue(iVirtualRow, iVirtualCol);
            if (string.IsNullOrEmpty(sValue)) sValue = "(blank)";
            long iNow = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            bool bDouble = (iChordKey == iLastSpeechChord)
                        && (iNow - iLastSpeechTicks < DoublePressMillis);
            iLastSpeechChord = iChordKey;
            iLastSpeechTicks = iNow;
            if (bDouble)
            {
                // Spell the value: insert spaces between characters
                // so the screen reader pronounces each one separately.
                // No leading "Spelling: " word -- the spaced-out
                // characters are themselves a clear cue that this
                // is a spelling, and the prefix only adds noise.
                System.Text.StringBuilder oSb = new System.Text.StringBuilder();
                foreach (char ch in sValue)
                {
                    if (oSb.Length > 0) oSb.Append(' ');
                    if (char.IsLetterOrDigit(ch)) oSb.Append(ch);
                    else if (ch == ' ') oSb.Append("space");
                    else oSb.Append(ch);
                }
                LiveRegion.say(oSb.ToString());
            }
            else
            {
                LiveRegion.say("Row " + iVirtualRow + ", " + sHeader + ": " + sValue);
            }
        }

        // speakOrSpell: shared helper for the speech-only commands
        // (Say Status, Say Path, Say Yield, etc.) so they get the
        // same double-press = spell behavior as Numpad5. The chord
        // identity is the key value the helper was called from.
        // Callers pass the text they would normally pass to
        // LiveRegion.say; this wraps that with the double-press
        // check.
        private void speakOrSpell(string sText, int iChordKey)
        {
            if (string.IsNullOrEmpty(sText)) return;
            long iNow = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            bool bDouble = (iChordKey == iLastSpeechChord)
                        && (iNow - iLastSpeechTicks < DoublePressMillis);
            iLastSpeechChord = iChordKey;
            iLastSpeechTicks = iNow;
            if (bDouble)
            {
                System.Text.StringBuilder oSb = new System.Text.StringBuilder();
                foreach (char ch in sText)
                {
                    if (oSb.Length > 0) oSb.Append(' ');
                    if (char.IsLetterOrDigit(ch)) oSb.Append(ch);
                    else if (ch == ' ') oSb.Append("space");
                    else oSb.Append(ch);
                }
                LiveRegion.say(oSb.ToString());
            }
            else
            {
                LiveRegion.say(sText);
            }
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

                // No materialized data cache here; the ListView is
                // the single source of truth and pulls rows on
                // demand from the recordset via RetrieveVirtualItem.
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
            miRecFindPrev.Enabled = bHasTable && sLastFindSubstring.Length > 0;
            miRecJump.Enabled = bHasTable;
            miRecJumpPrev.Enabled = bHasTable && sLastJumpSubstring.Length > 0;
            miRecFindRegex.Enabled = bHasTable;
            miRecFindRegexPrev.Enabled = bHasTable && sLastFindRegex.Length > 0;
            // Search-Next / Search-Previous (F3 / Shift+F3) repeat
            // whichever search family was most recently invoked.
            // Enabled whenever any of the three families has state.
            bool bAnySearch = sLastJumpSubstring.Length > 0
                           || sLastFindSubstring.Length > 0
                           || sLastFindRegex.Length > 0;
            miRecSearchAgain.Enabled = bHasTable && bAnySearch;
            miRecSearchPrev.Enabled  = bHasTable && bAnySearch;
            miRecMark.Enabled = bWritable && bHasTable;
            miRecUnmark.Enabled = bWritable && bHasTable;
            miRecUpdateField.Enabled = bWritable && bHasTable;
            miRecRelated.Enabled = bHasTable;
            miRecEnterChild.Enabled = bHasTable;
            // Exit-Child is enabled only when there is something on
            // the drill stack to pop back to.
            miRecExitChild.Enabled = bHasTable && oDrillStack.Count > 0;
            miRecExitChildToRoot.Enabled = bHasTable && oDrillStack.Count > 0;
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
            miToolsChart.Enabled = bHasTable;
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
        // anywhere in the form, including from inside the data list:
        //   - Control+Tab        => cycle to next previously-opened table
        //   - Control+Shift+Tab  => cycle to previous previously-opened table
        //   - Control+Page Down  => same as Control+Tab
        //   - Control+Page Up    => same as Control+Shift+Tab
        //   - Control+Home       => first marked row
        //   - Control+End        => last marked row
        //   - Control+Up         => previous marked row
        //   - Control+Down       => next marked row
        //   - Shift+Home         => mark from first row through current
        //   - Shift+End          => mark from current row through last
        //   - Alt+Shift+Home     => unmark from first row through current
        //   - Alt+Shift+End      => unmark from current row through last
        //
        // The Control+ family for marked-row navigation matches
        // FileDir's convention for tagged-file nav. Our ListView is
        // MultiSelect=false with FullRowSelect=true, where the native
        // meanings of Control+Home/End/Up/Down are: in MultiSelect
        // off, Control+Home/End behave identically to Home/End
        // (Control is a no-op there), and Control+Up/Down move only
        // the focus rectangle without changing the selected row --
        // invisible to a screen reader since the selection didn't
        // change. So commandeering these chords doesn't conflict
        // with anything the user can observe. Shift+Home / Shift+End
        // similarly need MultiSelect=true to do anything natively,
        // and Alt+Shift+ chords have no native ListView meaning.
        //
        // Tables without a 'marked' column ignore mark-related
        // keystrokes with a brief live-region notice. Bulk-mark
        // chords also no-op cleanly when the database is read-only.
        //
        // Record stepping in the data list still uses the arrow keys (the
        // grid's native behavior); record-level next/previous does not
        // need a global hotkey.
        // =====================================================================
        protected override bool ProcessCmdKey(ref Message oMsg, Keys oKeyData)
        {
            // VirtualCursor chords (Alt+Control + Home/End/arrows/
            // Numpad5/PageDown/PageUp). These are the screen-reader
            // table-navigation conventions: Alt+Control+Home jumps to
            // top-left, End to bottom-right, arrows move one cell at
            // a time, Numpad5 announces the current cell (twice to
            // spell), PageDown/PageUp jump to the last/first row of
            // the current column. Per Jamal's spec, Alt+Control is
            // OK for these because the extended arrow / numpad keys
            // don't make sense as Windows global hotkeys, so the
            // usual "no Alt+Control for in-app commands" rule has
            // an exception for this navigation family.
            if (oKeyData == (Keys.Alt | Keys.Control | Keys.Home))
            { virtMoveTo(1, 0); return true; }
            if (oKeyData == (Keys.Alt | Keys.Control | Keys.End))
            {
                int iLastRow = (db != null && db.hasRecordset()) ? db.recordCount : 1;
                int iLastCol = (grid != null) ? grid.Columns.Count - 1 : 0;
                virtMoveTo(iLastRow, iLastCol);
                return true;
            }
            if (oKeyData == (Keys.Alt | Keys.Control | Keys.Right))
            { virtMoveTo(iVirtualRow, iVirtualCol + 1); return true; }
            if (oKeyData == (Keys.Alt | Keys.Control | Keys.Left))
            { virtMoveTo(iVirtualRow, iVirtualCol - 1); return true; }
            if (oKeyData == (Keys.Alt | Keys.Control | Keys.Down))
            { virtMoveTo(iVirtualRow + 1, iVirtualCol); return true; }
            if (oKeyData == (Keys.Alt | Keys.Control | Keys.Up))
            { virtMoveTo(iVirtualRow - 1, iVirtualCol); return true; }
            if (oKeyData == (Keys.Alt | Keys.Control | Keys.PageDown))
            {
                int iLastRow = (db != null && db.hasRecordset()) ? db.recordCount : 1;
                virtMoveTo(iLastRow, iVirtualCol);
                return true;
            }
            if (oKeyData == (Keys.Alt | Keys.Control | Keys.PageUp))
            { virtMoveTo(1, iVirtualCol); return true; }
            // Numpad5: in Windows the key generates Keys.Clear when
            // NumLock is off; the standard JAWS / NVDA convention
            // accepts both encodings. Some keyboards also surface
            // Numpad5 as Keys.NumPad5 when NumLock is on. Accept
            // either to cover both modes.
            if (oKeyData == (Keys.Alt | Keys.Control | Keys.Clear))
            { virtSayCurrent((int)oKeyData); return true; }
            if (oKeyData == (Keys.Alt | Keys.Control | Keys.NumPad5))
            { virtSayCurrent((int)oKeyData); return true; }

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
            // Control+Home/End/Up/Down for marked-row navigation.
            // Matches FileDir's tagged-record convention. The
            // formKeyDown handler is the primary intercept; this
            // ProcessCmdKey hook is a safety backstop because the
            // menu strip can sometimes consume modifier+key chords
            // during the routing dance.
            if (oKeyData == (Keys.Control | Keys.Home))
            {
                jumpToMarkedRow(MarkJump.First);
                return true;
            }
            if (oKeyData == (Keys.Control | Keys.End))
            {
                jumpToMarkedRow(MarkJump.Last);
                return true;
            }
            if (oKeyData == (Keys.Control | Keys.Up))
            {
                jumpToMarkedRow(MarkJump.Previous);
                return true;
            }
            if (oKeyData == (Keys.Control | Keys.Down))
            {
                jumpToMarkedRow(MarkJump.Next);
                return true;
            }

            // Bulk-mark span chords. Same backstop pattern as the
            // marked-nav block above.
            if (oKeyData == (Keys.Shift | Keys.Home))
            {
                bulkMark(BulkMark.MarkToStart);
                return true;
            }
            if (oKeyData == (Keys.Shift | Keys.End))
            {
                bulkMark(BulkMark.MarkToEnd);
                return true;
            }
            if (oKeyData == (Keys.Alt | Keys.Shift | Keys.Home))
            {
                bulkMark(BulkMark.UnmarkToStart);
                return true;
            }
            if (oKeyData == (Keys.Alt | Keys.Shift | Keys.End))
            {
                bulkMark(BulkMark.UnmarkToEnd);
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

        // Bulk-mark span directions. MarkToStart/MarkToEnd set the
        // 'marked' column TRUE on every row from the first up to
        // and including the current (MarkToStart), or from the
        // current to the last (MarkToEnd). UnmarkToStart and
        // UnmarkToEnd do the same in reverse, setting marked FALSE.
        //
        // The chord family mirrors FileDir's tagged-file convention:
        // Shift+Home to tag from start, Shift+End to tag to end,
        // Alt+Shift+Home / Alt+Shift+End to untag the same spans.
        private enum BulkMark { MarkToStart, MarkToEnd, UnmarkToStart, UnmarkToEnd }

        // Apply a bulk-marking operation across a contiguous span
        // of the current filtered/sorted view. The span is anchored
        // at the current row. Rows are walked in ADO order (the
        // recordset's current Filter and Sort apply), so the span
        // makes sense in the user's current arrangement.
        //
        // The cursor returns to the row it started on. Speaks a
        // brief live-region summary ("Marked 14 rows", etc.) so the
        // screen reader confirms the operation.
        private void bulkMark(BulkMark oDir)
        {
            if (db == null || !db.hasRecordset()) return;
            if (!db.hasField(Metadata.MarkedColumn))
            {
                LiveRegion.say("This table has no marked column");
                return;
            }
            if (db.readOnly)
            {
                LiveRegion.say("Database is read-only");
                return;
            }

            int iStart = db.absolutePosition;
            int iCount = db.recordCount;
            if (iCount <= 0 || iStart < 1) return;

            int iFrom, iTo;
            bool bMark;
            string sVerb;
            switch (oDir)
            {
                case BulkMark.MarkToStart:
                    iFrom = 1; iTo = iStart; bMark = true; sVerb = "Marked"; break;
                case BulkMark.MarkToEnd:
                    iFrom = iStart; iTo = iCount; bMark = true; sVerb = "Marked"; break;
                case BulkMark.UnmarkToStart:
                    iFrom = 1; iTo = iStart; bMark = false; sVerb = "Unmarked"; break;
                case BulkMark.UnmarkToEnd:
                    iFrom = iStart; iTo = iCount; bMark = false; sVerb = "Unmarked"; break;
                default:
                    return;
            }

            object oOriginal = null;
            try { oOriginal = db.bookmark; } catch { }
            int iChanged = 0;
            int iSkipped = 0;
            try
            {
                for (int i = iFrom; i <= iTo; i++)
                {
                    try
                    {
                        db.absolutePosition = i;
                        string sValue = db.getFieldValue(Metadata.MarkedColumn);
                        bool bCurrent = isMarkedTrue(sValue);
                        if (bCurrent == bMark) { iSkipped++; continue; }
                        db.setFieldValue(Metadata.MarkedColumn, bMark ? "true" : "false");
                        db.update();
                        iChanged++;
                    }
                    catch
                    {
                        // Per-row errors don't stop the run; the user
                        // gets a count of what succeeded at the end.
                        iSkipped++;
                    }
                }
            }
            finally
            {
                if (oOriginal != null)
                {
                    try { db.bookmark = oOriginal; } catch { }
                }
            }
            invokeRefresh();
            string sMsg = sVerb + " " + iChanged + " row"
                        + (iChanged == 1 ? "" : "s")
                        + (iSkipped > 0 ? "; " + iSkipped + " already in that state" : "");
            LiveRegion.say(sMsg);
            DbDuoLog.write("BulkMark " + oDir + ": " + sMsg);
        }

        // markAndMove: set or clear the 'marked' column on the
        // current row, then move the cursor to the next or previous
        // row. The FileDir tag-and-next pattern lets the user walk
        // a list and decide row by row whether to mark it, with the
        // focus advancing automatically after each decision. The
        // screen reader announces the new row as the ListView's
        // focused item changes -- no extra speech from DbDuo is
        // needed for the move itself.
        //
        // Bound to:
        //   >                  (Shift+OemPeriod) = Mark and next
        //   <                  (Shift+Oemcomma) = Unmark and next
        //   Shift+DownArrow                       = Mark and next
        //   Shift+UpArrow                         = Mark and previous
        //   Alt+Shift+DownArrow                   = Unmark and next
        //   Alt+Shift+UpArrow                     = Unmark and previous
        //
        // If the current row is already in the requested state, the
        // marked column is left untouched (no needless UPDATE).
        // Movement still happens. At the end of the recordset, the
        // cursor stays put rather than walking off into BOF/EOF.
        private void markAndMove(bool bMark, bool bForward)
        {
            if (db == null || !db.hasRecordset()) return;
            if (db.recordCount <= 0) return;
            if (!db.hasField(Metadata.MarkedColumn))
            {
                LiveRegion.say("This table has no marked column");
                return;
            }
            if (db.readOnly)
            {
                LiveRegion.say("Database is read-only");
                return;
            }
            try
            {
                string sValue = db.getFieldValue(Metadata.MarkedColumn);
                bool bCurrent = isMarkedTrue(sValue);
                if (bCurrent != bMark)
                {
                    db.setFieldValue(Metadata.MarkedColumn, bMark ? "true" : "false");
                    db.update();
                }
            }
            catch (Exception oEx)
            {
                LiveRegion.say("Mark error: " + oEx.Message);
                return;
            }
            // Move cursor one row in the requested direction; stay
            // put at the boundaries so the screen reader doesn't
            // announce an out-of-range condition.
            int iPos = db.absolutePosition;
            int iCount = db.recordCount;
            if (bForward && iPos < iCount) iPos++;
            else if (!bForward && iPos > 1) iPos--;
            try { db.absolutePosition = iPos; } catch { }
            invokeRefresh();
        }

        // markAll: set or clear the 'marked' column on every row in
        // the current filtered view. FileDir's Control+A / Control+
        // Shift+A pattern: select all / clear all. We keep the
        // cursor position via bookmark across the bulk update.
        private void markAll(bool bMark)
        {
            if (db == null || !db.hasRecordset()) return;
            if (!db.hasField(Metadata.MarkedColumn))
            {
                LiveRegion.say("This table has no marked column");
                return;
            }
            if (db.readOnly)
            {
                LiveRegion.say("Database is read-only");
                return;
            }
            int iCount = db.recordCount;
            if (iCount <= 0) return;

            object oOriginal = null;
            try { oOriginal = db.bookmark; } catch { }
            int iChanged = 0;
            int iSkipped = 0;
            try
            {
                for (int i = 1; i <= iCount; i++)
                {
                    try
                    {
                        db.absolutePosition = i;
                        bool bCurrent = isMarkedTrue(db.getFieldValue(Metadata.MarkedColumn));
                        if (bCurrent == bMark) { iSkipped++; continue; }
                        db.setFieldValue(Metadata.MarkedColumn, bMark ? "true" : "false");
                        db.update();
                        iChanged++;
                    }
                    catch { iSkipped++; }
                }
            }
            finally
            {
                if (oOriginal != null) try { db.bookmark = oOriginal; } catch { }
            }
            invokeRefresh();
            string sVerb = bMark ? "Marked" : "Unmarked";
            string sMsg = sVerb + " " + iChanged + " row" + (iChanged == 1 ? "" : "s")
                        + (iSkipped > 0 ? "; " + iSkipped + " already in that state" : "");
            LiveRegion.say(sMsg);
            DbDuoLog.write("MarkAll " + bMark + ": " + sMsg);
        }

        // invertMarks: flip the marked-state of every row in the
        // current filtered view. FileDir's Control+I pattern.
        // Useful for "give me everything I didn't already pick."
        // Cursor preserved by bookmark.
        private void invertMarks()
        {
            if (db == null || !db.hasRecordset()) return;
            if (!db.hasField(Metadata.MarkedColumn))
            {
                LiveRegion.say("This table has no marked column");
                return;
            }
            if (db.readOnly)
            {
                LiveRegion.say("Database is read-only");
                return;
            }
            int iCount = db.recordCount;
            if (iCount <= 0) return;

            object oOriginal = null;
            try { oOriginal = db.bookmark; } catch { }
            int iFlipped = 0;
            try
            {
                for (int i = 1; i <= iCount; i++)
                {
                    try
                    {
                        db.absolutePosition = i;
                        bool bCurrent = isMarkedTrue(db.getFieldValue(Metadata.MarkedColumn));
                        db.setFieldValue(Metadata.MarkedColumn, bCurrent ? "false" : "true");
                        db.update();
                        iFlipped++;
                    }
                    catch { /* skip row */ }
                }
            }
            finally
            {
                if (oOriginal != null) try { db.bookmark = oOriginal; } catch { }
            }
            invokeRefresh();
            string sMsg = "Inverted marks on " + iFlipped + " row"
                        + (iFlipped == 1 ? "" : "s");
            LiveRegion.say(sMsg);
            DbDuoLog.write("InvertMarks: " + sMsg);
        }

        // sayWhere: Show-Where -- read out the title bar, the
        // status bar, and the current row's displayed columns.
        // Bound to ? (Shift+Slash) from the data list.
        //
        // The "where am I" question is one screen-reader users hit
        // constantly. JAWS has Insert+T for title, Insert+PageDown
        // for status bar, and various row-readers, but they're
        // three separate gestures and the result is announced in
        // the screen reader's own voice and order. A single chord
        // that DbDuo composes itself gives the user a consistent
        // summary in DbDuo's own LiveRegion path -- title, then
        // status, then per-column values for the current row,
        // separated by " | " so JAWS' pause-on-punctuation pacing
        // is natural.
        //
        // Only displayed columns are included (Select-Column's
        // user override, or DbDuo's default visible columns); the
        // primary key and bookkeeping fields are not. To inspect
        // every column, including hidden ones, use Show-Object
        // (Enter) instead.
        private void sayWhere()
        {
            StringBuilder oSb = new StringBuilder();
            string sTitle = this.Text ?? "";
            if (sTitle.Length > 0) oSb.Append(sTitle);

            // Both status labels, joined with a space if both have
            // text. If either is empty we skip it cleanly.
            string sStatusLeft = (lblTable != null) ? (lblTable.Text ?? "") : "";
            string sStatusRight = (lblStatus != null) ? (lblStatus.Text ?? "") : "";
            string sStatus = (sStatusLeft + " " + sStatusRight).Trim();
            if (sStatus.Length > 0)
            {
                if (oSb.Length > 0) oSb.Append(" | ");
                oSb.Append(sStatus);
            }

            // Current row's displayed columns. If no recordset is
            // open or the recordset is empty, this section is
            // simply omitted.
            if (db != null && db.hasRecordset() && db.recordCount > 0)
            {
                try
                {
                    List<string> lCols = db.getDisplayFieldNames();
                    foreach (string sCol in lCols)
                    {
                        string sV = db.getFieldValue(sCol);
                        if (sV == null) sV = "";
                        if (oSb.Length > 0) oSb.Append(" | ");
                        oSb.Append(sCol);
                        oSb.Append(": ");
                        oSb.Append(sV);
                    }
                }
                catch { /* speak what we have */ }
            }

            string sMsg = oSb.ToString();
            if (sMsg.Length == 0) sMsg = "DbDuo, no database open";
            LiveRegion.say(sMsg);
        }

        // =======================================================================
        // Speech-only commands. Each builds a string from current state
        // and pushes it through LiveRegion.say() without changing focus,
        // selection, recordset position, or any other state. Modeled on
        // FileDir's "Say X" family (Say-Date, Say-Path, Say-Size, etc.)
        // and EdSharp's Alt+Letter status-query family (Address, Block,
        // Path, Yield, Status). Live in the Query menu by convention.
        // =======================================================================

        // saySayStatus: Alt+Z. Speak the table name, row position,
        // and any active filter / sort. FileDir's Alt+Z = "Say Status."
        // EdSharp's Alt+Z = "Status." Equivalent purpose in DbDuo.
        private void saySayStatus(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.isOpen())
            { LiveRegion.say("No database open"); return; }
            if (!db.hasRecordset())
            { speakOrSpell(db.filePath ?? "Database open, no table selected", 101); return; }
            StringBuilder oSb = new StringBuilder();
            oSb.Append(db.currentTable ?? "(no table)");
            oSb.Append(" row ").Append(db.absolutePosition).Append(" of ").Append(db.recordCount);
            if (db.filter.Length > 0) oSb.Append("; filter: ").Append(db.filter);
            if (db.sort.Length > 0) oSb.Append("; sort: ").Append(db.sort);
            speakOrSpell(oSb.ToString(), 101);
        }

        // saySayPath: Alt+P. Speak the path of the currently-open
        // database file. FileDir's Alt+P = "Say Path."
        private void saySayPath(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.isOpen())
            { LiveRegion.say("No database open"); return; }
            string sPath = db.filePath ?? "(unknown)";
            speakOrSpell(sPath, 102);
        }

        // saySayYield: Alt+Y. Speak the record count and any active
        // filter. EdSharp's Alt+Y = "Yield" (record count). FileDir's
        // Alt+Y = "Yield Files" (file count).
        private void saySayYield(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset())
            { LiveRegion.say("No table selected"); return; }
            StringBuilder oSb = new StringBuilder();
            oSb.Append(db.recordCount).Append(" row").Append(db.recordCount == 1 ? "" : "s");
            if (db.filter.Length > 0) oSb.Append(" (filter: ").Append(db.filter).Append(")");
            speakOrSpell(oSb.ToString(), 103);
        }

        // saySayTables: Shift+F4. Speak the names of tables that
        // have been opened in this DbDuo session. FileDir's Shift+F4 =
        // "Say Windows Open." Speech-only -- does NOT open a picker.
        // The picker (Select-Table) is F4 by FileDir's Current Windows
        // convention. Shift+F4 is the "tell me without changing
        // anything" variant.
        private void saySayTables(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.isOpen())
            { LiveRegion.say("No database open"); return; }
            List<string> lTables = db.visitedTableNames();
            if (lTables == null || lTables.Count == 0)
            { LiveRegion.say("No tables visited yet in this session"); return; }
            StringBuilder oSb = new StringBuilder();
            oSb.Append(lTables.Count).Append(" table").Append(lTables.Count == 1 ? "" : "s").Append(": ");
            for (int i = 0; i < lTables.Count; i++)
            {
                if (i > 0) oSb.Append(", ");
                oSb.Append(lTables[i]);
            }
            speakOrSpell(oSb.ToString(), 104);
        }

        // saySayMarked: Shift+L. Speak the look-column values of every
        // row whose 'marked' column is true. FileDir's Shift+L =
        // "List Tagged." Limited to the first 25 marked rows with a
        // "(plus N more)" footer if the list is longer, to keep the
        // announcement tractable.
        private void saySayMarked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset())
            { LiveRegion.say("No table selected"); return; }
            if (!db.hasField(Metadata.MarkedColumn))
            { LiveRegion.say("This table has no marked column"); return; }

            object oOriginal = null;
            try { oOriginal = db.bookmark; } catch { }
            const int iMaxAnnounce = 25;
            List<string> lLooks = new List<string>();
            int iTotal = 0;
            try
            {
                for (int i = 1; i <= db.recordCount; i++)
                {
                    try
                    {
                        db.absolutePosition = i;
                        if (!isMarkedTrue(db.getFieldValue(Metadata.MarkedColumn))) continue;
                        iTotal++;
                        if (lLooks.Count < iMaxAnnounce)
                        {
                            string sLook = db.getFieldValue("look");
                            if (string.IsNullOrEmpty(sLook)) sLook = "row " + i;
                            lLooks.Add(sLook);
                        }
                    }
                    catch { }
                }
            }
            finally
            {
                if (oOriginal != null) try { db.bookmark = oOriginal; } catch { }
            }
            if (iTotal == 0)
            { LiveRegion.say("No marked rows"); return; }
            StringBuilder oSb = new StringBuilder();
            oSb.Append(iTotal).Append(" marked row").Append(iTotal == 1 ? "" : "s").Append(": ");
            for (int i = 0; i < lLooks.Count; i++)
            {
                if (i > 0) oSb.Append("; ");
                oSb.Append(lLooks[i]);
            }
            if (iTotal > lLooks.Count)
                oSb.Append("; plus ").Append(iTotal - lLooks.Count).Append(" more");
            speakOrSpell(oSb.ToString(), 105);
        }

        // saySayDate: Shift+D. Speak the 'updated' column value of
        // the current row. FileDir's Shift+D = "Say Date."
        private void saySayDate(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset() || db.recordCount == 0)
            { LiveRegion.say("No record selected"); return; }
            string sCol = db.hasField("updated") ? "updated" : (db.hasField("added") ? "added" : null);
            if (sCol == null)
            { LiveRegion.say("No date column in this table"); return; }
            string sVal = db.getFieldValue(sCol);
            speakOrSpell(sCol + ": " + (string.IsNullOrEmpty(sVal) ? "(empty)" : sVal), 106);
        }

        // saySayType: Shift+T. Speak the table or view name along
        // with the row position, like a "what am I looking at" probe.
        // FileDir's Shift+T = "Say Type." DbDuo's analog is to say
        // table-or-view name + row position.
        private void saySayType(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.isOpen())
            { LiveRegion.say("No database open"); return; }
            string sName = db.currentTable ?? "(no table)";
            string sKind = db.currentIsView ? "view" : "table";
            if (!db.hasRecordset())
            { speakOrSpell(sKind + ": " + sName, 107); return; }
            speakOrSpell(sKind + ": " + sName + ", row " + db.absolutePosition + " of " + db.recordCount, 107);
        }

        // saySayYieldMarked: Shift+Y. Speak the count of marked rows.
        // FileDir's Shift+Y = "Yield Tagged."
        private void saySayYieldMarked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset())
            { LiveRegion.say("No table selected"); return; }
            if (!db.hasField(Metadata.MarkedColumn))
            { LiveRegion.say("This table has no marked column"); return; }
            object oOriginal = null;
            try { oOriginal = db.bookmark; } catch { }
            int iCount = 0;
            try
            {
                for (int i = 1; i <= db.recordCount; i++)
                {
                    try
                    {
                        db.absolutePosition = i;
                        if (isMarkedTrue(db.getFieldValue(Metadata.MarkedColumn))) iCount++;
                    }
                    catch { }
                }
            }
            finally
            {
                if (oOriginal != null) try { db.bookmark = oOriginal; } catch { }
            }
            speakOrSpell(iCount + " marked row" + (iCount == 1 ? "" : "s"), 108);
        }

        // =======================================================================
        // Action commands new in this turn (not speech-only). Extract,
        // Copy-Row, Step-Initial-Change, plus the wrapper handlers for
        // menu items that route to them.
        // =======================================================================

        // copyRow: Shift+A. Copy the current row's visible columns to
        // the clipboard as tab-separated values. FileDir's Shift+A =
        // "Append to Clipboard"; DbDuo's analog is row-as-TSV which
        // pastes cleanly into Excel, Word tables, and chat clients.
        private void copyRowClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset() || db.recordCount == 0)
            { LiveRegion.say("No record selected"); return; }
            try
            {
                List<string> lFields = db.getDisplayFieldNames();
                StringBuilder oSb = new StringBuilder();
                for (int i = 0; i < lFields.Count; i++)
                {
                    if (i > 0) oSb.Append('\t');
                    string sV = db.getFieldValue(lFields[i]) ?? "";
                    // Tab and newline are TSV-killers; replace with
                    // space and a slash-n marker respectively.
                    sV = sV.Replace('\t', ' ').Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");
                    oSb.Append(sV);
                }
                Clipboard.SetText(oSb.ToString());
                LiveRegion.say("Row copied to clipboard");
            }
            catch (Exception oEx) { LiveRegion.say("Copy-Row: " + oEx.Message); }
        }

        // stepInitialChange: jump to the next row whose value in a
        // user-picked column differs in its first character from the
        // current row. FileDir's Shift+I = "Initial Change." Useful
        // for skipping through alphabetically sorted data: jump from
        // all "A..." rows past the last A to the first B.
        //
        // The column is prompted via LBC dialog rather than read from
        // any Tab-tracked state -- the listview doesn't visually
        // highlight a "current column" so reading it from a state
        // var would be a hidden coupling that screen-reader users
        // can't see.
        private void stepInitialChangeClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset() || db.recordCount == 0)
            { LiveRegion.say("No record selected"); return; }
            List<string> lFields = db.getDisplayFieldNames();
            if (lFields == null || lFields.Count == 0)
            { LiveRegion.say("No columns"); return; }
            LbcDialog oDlg = new LbcDialog("Next Initial Change", this);
            string sColName;
            try
            {
                string sDefault = virtCurrentColumnName();
                if (string.IsNullOrEmpty(sDefault) || !lFields.Contains(sDefault)) sDefault = lFields[0];
                ListBox lb = oDlg.addPickBox(
                    "&Column to track for an initial-letter change:",
                    lFields, sDefault,
                    "The command jumps to the next row whose value in this column starts with a different letter");
                if (!oDlg.runOkCancel()) return;
                if (lb.SelectedItem == null) return;
                sColName = lb.SelectedItem.ToString();
            }
            finally { oDlg.Dispose(); }
            try
            {
                string sCurrent = db.getFieldValue(sColName) ?? "";
                char chCurrent = sCurrent.Length > 0
                    ? char.ToUpperInvariant(sCurrent[0])
                    : '\0';
                int iStart = db.absolutePosition;
                for (int iPos = iStart + 1; iPos <= db.recordCount; iPos++)
                {
                    db.absolutePosition = iPos;
                    string sV = db.getFieldValue(sColName) ?? "";
                    char ch = sV.Length > 0 ? char.ToUpperInvariant(sV[0]) : '\0';
                    if (ch != chCurrent)
                    {
                        invokeRefresh();
                        LiveRegion.say("Initial: " + (sV.Length > 0 ? sV.Substring(0, Math.Min(40, sV.Length)) : "(empty)"));
                        return;
                    }
                }
                // No change found; put cursor back.
                db.absolutePosition = iStart;
                LiveRegion.say("No further initial change in " + sColName);
            }
            catch (Exception oEx) { LiveRegion.say("Next Initial Change: " + oEx.Message); }
        }

        // extractRegex: Control+Shift+E. Prompt for a .NET regex,
        // walk every visible row, and copy to the clipboard every
        // matching substring from every visible column. EdSharp's
        // Control+Shift+E = "Extract with Regular Expression."
        // FileDir's Control+Shift+E = same. Each match goes on its
        // own line. Useful for pulling email addresses, URLs, IDs,
        // or any pattern out of free-text columns.
        private void extractRegexClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset() || db.recordCount == 0)
            { LiveRegion.say("No table selected"); return; }
            string sPattern;
            using (LbcDialog oDlg = new LbcDialog("Extract-Regex", this))
            {
                oDlg.addLabel("Extract every match of a .NET regex from every visible cell");
                oDlg.addLabel("in the current filtered view. Matches go to the clipboard,");
                oDlg.addLabel("one per line. Useful for pulling emails, URLs, or IDs out");
                oDlg.addLabel("of free-text columns.");
                oDlg.addSeparator();
                TextBox tbPat = oDlg.addInlineInputBox("Regex pattern",
                    "", "A standard .NET regex (e.g. \\b\\w+@\\w+\\.\\w+\\b for emails)");
                if (!oDlg.runOkCancel()) return;
                sPattern = tbPat.Text ?? "";
            }
            if (sPattern.Length == 0) return;
            System.Text.RegularExpressions.Regex oRe;
            try
            {
                oRe = new System.Text.RegularExpressions.Regex(sPattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, "Invalid regex: " + oEx.Message,
                    "Extract-Regex", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<string> lFields = db.getDisplayFieldNames();
            object oOriginal = null;
            try { oOriginal = db.bookmark; } catch { }
            int iMatches = 0;
            StringBuilder oOut = new StringBuilder();
            try
            {
                for (int i = 1; i <= db.recordCount; i++)
                {
                    try
                    {
                        db.absolutePosition = i;
                        foreach (string sCol in lFields)
                        {
                            string sV = db.getFieldValue(sCol) ?? "";
                            if (sV.Length == 0) continue;
                            foreach (System.Text.RegularExpressions.Match oM in oRe.Matches(sV))
                            {
                                oOut.AppendLine(oM.Value);
                                iMatches++;
                            }
                        }
                    }
                    catch { }
                }
            }
            finally
            {
                if (oOriginal != null) try { db.bookmark = oOriginal; } catch { }
            }
            invokeRefresh();
            if (iMatches == 0)
            { LiveRegion.say("No matches found"); return; }
            try
            {
                Clipboard.SetText(oOut.ToString());
                LiveRegion.say(iMatches + " match" + (iMatches == 1 ? "" : "es") + " copied to clipboard");
            }
            catch (Exception oEx) { LiveRegion.say("Extract-Regex clipboard error: " + oEx.Message); }
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
                MessageBox.Show(this, oEx.Message, "Choose Table", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Speak the current table name and row count through the live
        // region. Called whenever a table is freshly selected (via the
        // Select-Table dialog, the dot-prompt 'use' command, or the
        // Control+Tab cycle).
        private void announceTableOpened()
        {
            if (db == null || !db.isOpen() || string.IsNullOrEmpty(db.currentTable)) return;
            // Reset virtual cursor to (row 1, col 0) on every table
            // open so the user starts at a known position. F5 path
            // and table-switch paths both pass through here.
            virtResetToTop();
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
        // SearchHistory: per-family recent-search list persisted to
        // DbDuo.ini. Each family has its own section ([RecentJump],
        // [RecentFind], [RecentFindRegex]) holding up to 10 entries.
        // Each entry is a (term, caseSensitive) pair stored as two
        // keys: termN and caseN where N is 1..10. Entry 1 is the
        // most recent.
        //
        // The dialog shows the listbox of terms; selecting one
        // auto-populates the text input AND auto-sets the case-
        // sensitive checkbox to how the term was last used.
        //
        // The list is move-to-front: if the user re-uses a term
        // it shifts to the top rather than appearing twice.
        // =====================================================================
        public static class SearchHistory
        {
            public const string SectionJump  = "RecentJump";
            public const string SectionFind  = "RecentFind";
            public const string SectionRegex = "RecentFindRegex";
            private const int MaxEntries = 10;

            // Result of a load: parallel lists of terms and the
            // case-sensitive flags that were active when each term
            // was used.
            public class Entry
            {
                public string sTerm;
                public bool   bCaseSensitive;
            }

            // Load the recent list for the given family section.
            // Returns an empty list if the section is absent.
            public static List<Entry> load(string sSection)
            {
                List<Entry> l = new List<Entry>();
                for (int i = 1; i <= MaxEntries; i++)
                {
                    string sTerm = IniSession.read(sSection, "term" + i);
                    if (string.IsNullOrEmpty(sTerm)) break;
                    string sCase = IniSession.read(sSection, "case" + i);
                    Entry e = new Entry();
                    e.sTerm = sTerm;
                    e.bCaseSensitive = !string.IsNullOrEmpty(sCase)
                                    && (sCase.Equals("Y", StringComparison.OrdinalIgnoreCase)
                                     || sCase.Equals("Yes", StringComparison.OrdinalIgnoreCase)
                                     || sCase.Equals("1", StringComparison.Ordinal)
                                     || sCase.Equals("True", StringComparison.OrdinalIgnoreCase));
                    l.Add(e);
                }
                return l;
            }

            // Record a search. Moves the term to the front if it
            // already exists (with the new caseSensitive value),
            // otherwise prepends. Truncates to MaxEntries.
            public static void record(string sSection, string sTerm, bool bCaseSensitive)
            {
                if (string.IsNullOrEmpty(sTerm)) return;
                List<Entry> l = load(sSection);
                // Remove any existing entry with the same term
                // (case-insensitive comparison; the case-sensitivity
                // setting is per-search not per-term identity).
                l.RemoveAll(delegate(Entry e)
                {
                    return string.Equals(e.sTerm, sTerm, StringComparison.OrdinalIgnoreCase);
                });
                Entry oNew = new Entry();
                oNew.sTerm = sTerm;
                oNew.bCaseSensitive = bCaseSensitive;
                l.Insert(0, oNew);
                if (l.Count > MaxEntries) l.RemoveRange(MaxEntries, l.Count - MaxEntries);
                save(sSection, l);
            }

            // Serialize the list back to the ini section. Clears
            // any stale entries beyond the new list's length.
            public static void save(string sSection, List<Entry> l)
            {
                for (int i = 1; i <= MaxEntries; i++)
                {
                    if (i <= l.Count)
                    {
                        IniSession.write(sSection, "term" + i, l[i - 1].sTerm ?? "");
                        IniSession.write(sSection, "case" + i, l[i - 1].bCaseSensitive ? "Y" : "N");
                    }
                    else
                    {
                        IniSession.write(sSection, "term" + i, "");
                        IniSession.write(sSection, "case" + i, "");
                    }
                }
            }
        }

        // =====================================================================
        // RecentFiles: persists up to 10 recently opened database
        // file paths plus per-file state (last-active table, and
        // per-table filter / sort / position). All stored in the
        // per-user DbDuo.ini, one section per file:
        //
        //   [RecentFile1]                  <- index 1 = most recent
        //   path        = C:\path\to\file.db
        //   lastTable   = transactions
        //   t1_name     = transactions     <- per-table state, indexed
        //   t1_filter   = LOWER(memo) LIKE '%coffee%'
        //   t1_sort     = updated DESC
        //   t1_position = 47
        //   t2_name     = categories
        //   t2_position = 12
        //
        //   [RecentFile2] ... etc
        //
        // The "table state" is stored under indexed t1_/t2_/... keys
        // rather than the table name directly, because table names
        // can contain characters that are inconvenient for ini keys
        // (spaces, brackets, equals signs). The per-section index
        // is rebuilt on each save by enumerating the in-memory
        // dictionary.
        //
        // Restore strategy: when reopening a recent file, attempt
        // to set lastTable; if that table no longer exists, silently
        // fall back to the database's first table. For each table
        // we have state for, try to apply filter / sort / position;
        // any individual failure (column missing, position out of
        // range) is silently skipped.
        // =====================================================================
        public static class RecentFiles
        {
            public const int MaxEntries = 10;

            public class TableState
            {
                public string sName;
                public string sFilter;
                public string sSort;
                public int    iPosition;
            }
            public class FileState
            {
                public string sPath;
                public string sLastTable;
                public Dictionary<string, TableState> dTables = new Dictionary<string, TableState>(StringComparer.OrdinalIgnoreCase);
            }

            private static string sectionFor(int iIndex) { return "RecentFile" + iIndex; }

            // Load all recent file entries in recency order. Empty
            // section breaks the loop -- gaps mean "no more recents."
            public static List<FileState> loadAll()
            {
                List<FileState> l = new List<FileState>();
                for (int i = 1; i <= MaxEntries; i++)
                {
                    string sPath = IniSession.read(sectionFor(i), "path");
                    if (string.IsNullOrEmpty(sPath)) break;
                    FileState f = loadSection(sectionFor(i));
                    if (f != null) l.Add(f);
                }
                return l;
            }

            private static FileState loadSection(string sSection)
            {
                string sPath = IniSession.read(sSection, "path");
                if (string.IsNullOrEmpty(sPath)) return null;
                FileState f = new FileState();
                f.sPath = sPath;
                f.sLastTable = IniSession.read(sSection, "lastTable") ?? "";
                // Enumerate t1_, t2_, ... up to a sensible cap (32
                // tables-per-file is more than enough for typical use).
                for (int t = 1; t <= 32; t++)
                {
                    string sName = IniSession.read(sSection, "t" + t + "_name");
                    if (string.IsNullOrEmpty(sName)) break;
                    TableState ts = new TableState();
                    ts.sName     = sName;
                    ts.sFilter   = IniSession.read(sSection, "t" + t + "_filter");
                    ts.sSort     = IniSession.read(sSection, "t" + t + "_sort");
                    string sPos  = IniSession.read(sSection, "t" + t + "_position");
                    int iPos;
                    ts.iPosition = (!string.IsNullOrEmpty(sPos) && int.TryParse(sPos, out iPos)) ? iPos : 1;
                    f.dTables[sName] = ts;
                }
                return f;
            }

            // Find an existing entry by path (case-insensitive), or
            // null if not present.
            public static FileState findByPath(List<FileState> l, string sPath)
            {
                if (l == null || string.IsNullOrEmpty(sPath)) return null;
                foreach (FileState f in l)
                {
                    if (string.Equals(f.sPath, sPath, StringComparison.OrdinalIgnoreCase))
                        return f;
                }
                return null;
            }

            // Promote the file to the front of the recent list,
            // updating its state in place. Truncates to MaxEntries.
            public static void recordOpen(string sPath)
            {
                if (string.IsNullOrEmpty(sPath)) return;
                List<FileState> l = loadAll();
                FileState existing = findByPath(l, sPath);
                if (existing != null) l.Remove(existing);
                FileState f = existing ?? new FileState();
                f.sPath = sPath;
                l.Insert(0, f);
                if (l.Count > MaxEntries) l.RemoveRange(MaxEntries, l.Count - MaxEntries);
                saveAll(l);
            }

            // Update a single table's state inside a file. The file
            // entry is found by path; if absent it's created (and
            // promoted to the front via recordOpen).
            public static void recordTableState(string sPath, string sTable, string sFilter, string sSort, int iPosition)
            {
                if (string.IsNullOrEmpty(sPath) || string.IsNullOrEmpty(sTable)) return;
                List<FileState> l = loadAll();
                FileState f = findByPath(l, sPath);
                if (f == null)
                {
                    recordOpen(sPath);
                    l = loadAll();
                    f = findByPath(l, sPath);
                    if (f == null) return;
                }
                TableState ts;
                if (!f.dTables.TryGetValue(sTable, out ts))
                {
                    ts = new TableState(); ts.sName = sTable;
                    f.dTables[sTable] = ts;
                }
                ts.sFilter   = sFilter ?? "";
                ts.sSort     = sSort   ?? "";
                ts.iPosition = iPosition;
                f.sLastTable = sTable;
                saveAll(l);
            }

            // Persist the entire in-memory list. Older slots beyond
            // the current list size are cleared.
            public static void saveAll(List<FileState> l)
            {
                for (int i = 1; i <= MaxEntries; i++)
                {
                    string sSection = sectionFor(i);
                    if (i <= l.Count)
                    {
                        FileState f = l[i - 1];
                        IniSession.write(sSection, "path", f.sPath ?? "");
                        IniSession.write(sSection, "lastTable", f.sLastTable ?? "");
                        // Write each table's state under sequential t1_ / t2_ keys.
                        int iT = 1;
                        foreach (KeyValuePair<string, TableState> kv in f.dTables)
                        {
                            if (iT > 32) break;
                            TableState ts = kv.Value;
                            IniSession.write(sSection, "t" + iT + "_name",     ts.sName ?? "");
                            IniSession.write(sSection, "t" + iT + "_filter",   ts.sFilter ?? "");
                            IniSession.write(sSection, "t" + iT + "_sort",     ts.sSort ?? "");
                            IniSession.write(sSection, "t" + iT + "_position", ts.iPosition.ToString());
                            iT++;
                        }
                        // Clear unused indexes from prior writes (up to 32).
                        for (int j = iT; j <= 32; j++)
                        {
                            IniSession.write(sSection, "t" + j + "_name", "");
                            IniSession.write(sSection, "t" + j + "_filter", "");
                            IniSession.write(sSection, "t" + j + "_sort", "");
                            IniSession.write(sSection, "t" + j + "_position", "");
                        }
                    }
                    else
                    {
                        // Empty slot: clear path so loadAll's
                        // empty-section-breaks-the-loop semantic
                        // works. Don't bother zeroing every key.
                        IniSession.write(sSection, "path", "");
                        IniSession.write(sSection, "lastTable", "");
                    }
                }
            }
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
                oFd.Title = "New Database";
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
                oFd.Title = "Open Database";
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
                openDatabaseAndApplyState(oFd.FileName, null);
            }
        }

        // Recent Files (Alt+R / File > Recent Files): show an LBC
        // dialog listing up to the last 10 database files the user
        // has opened. Selecting one re-opens it, restoring the
        // last-active table (if it still exists) and that table's
        // filter / sort / position (silently skipping any piece
        // that no longer applies). The list is populated from the
        // [RecentFile1..10] sections of DbDuo.ini.
        private void fileRecentClicked(object oSender, EventArgs oArgs)
        {
            List<RecentFiles.FileState> l = RecentFiles.loadAll();
            if (l.Count == 0)
            {
                MessageBox.Show(this,
                    "No recent database files yet. Open a database from the File menu to start the list.",
                    "Recent Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            LbcDialog oDlg = new LbcDialog("Recent Files", this);
            try
            {
                List<string> lDisplay = new List<string>();
                foreach (RecentFiles.FileState f in l)
                {
                    string sDisp = f.sPath;
                    if (!string.IsNullOrEmpty(f.sLastTable))
                        sDisp += "   (last table: " + f.sLastTable + ")";
                    lDisplay.Add(sDisp);
                }
                ListBox lb = oDlg.addPickBox(
                    "&Recent database files (Enter to open):",
                    lDisplay, lDisplay[0],
                    "Each entry shows the path and the last table that was active. Opening restores the table, filter, sort, and position.");
                if (!oDlg.runOkCancel()) return;
                int iIdx = lb.SelectedIndex;
                if (iIdx < 0 || iIdx >= l.Count) return;
                openDatabaseAndApplyState(l[iIdx].sPath, l[iIdx]);
            }
            finally { oDlg.Dispose(); }
        }

        // openDatabaseAndApplyState: shared open path used by both
        // File > Open and File > Recent Files. Opens the file,
        // applies any saved per-table state from a FileState (or
        // none if null), records the open in RecentFiles, and
        // updates IniSession's last-opened pointers. Failures at
        // the per-table-state restore step are silently swallowed
        // per the spec ("if something is missing so cannot be
        // configured ... just silently skip the incongruity").
        private void openDatabaseAndApplyState(string sPath, RecentFiles.FileState oState)
        {
            try
            {
                DbDuoLog.write("Opening: " + sPath);
                oDrillStack.Clear();
                db.openDatabase(sPath, null, false);
                // Pick the table: saved lastTable if it still exists,
                // else first base table, else first view.
                string sDesiredTable = (oState != null) ? oState.sLastTable : null;
                List<string> lTables = db.getTableNames();
                if (lTables == null || lTables.Count == 0) lTables = db.getViewNames();
                if (lTables == null) lTables = new List<string>();
                if (string.IsNullOrEmpty(sDesiredTable) || !lTables.Contains(sDesiredTable))
                {
                    sDesiredTable = lTables.Count > 0 ? lTables[0] : null;
                }
                if (!string.IsNullOrEmpty(sDesiredTable))
                {
                    try { db.selectTable(sDesiredTable); } catch { }
                }
                // Apply per-table saved state (filter, sort, position).
                if (oState != null && oState.dTables != null && !string.IsNullOrEmpty(sDesiredTable))
                {
                    RecentFiles.TableState ts;
                    if (oState.dTables.TryGetValue(sDesiredTable, out ts))
                    {
                        if (!string.IsNullOrEmpty(ts.sFilter))
                        { try { db.applyFilter(ts.sFilter); } catch { /* silently skip */ } }
                        if (!string.IsNullOrEmpty(ts.sSort))
                        { try { db.sort = ts.sSort; } catch { /* silently skip */ } }
                        if (ts.iPosition > 0)
                        { try { db.absolutePosition = ts.iPosition; } catch { /* silently skip */ } }
                    }
                }
                invokeRefresh();
                DbDuoLog.write("Open succeeded. Table: " + (db.currentTable ?? "(none)"));
                IniSession.lastDatabase = sPath;
                IniSession.lastTable    = db.currentTable ?? "";
                // Record into Recent Files list (move-to-front).
                RecentFiles.recordOpen(sPath);
                announceTableOpened();
            }
            catch (Exception oEx)
            {
                DbDuoLog.write("Open failed: " + oEx.Message);
                MessageBox.Show(this, oEx.Message, "Open Database",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void fileSaveAsClicked(object oSender, EventArgs oArgs) { saveAsCommon("Save Database As"); }
        private void fileBackupClicked(object oSender, EventArgs oArgs) { saveAsCommon("Backup Database"); }

        // helpSampleDbClicked: open the sample.db that ships in the
        // DbDuo install folder. Uses the same open path File > Open
        // Database uses (openDatabaseAndApplyState) so the normal
        // post-open behaviors apply. If sample.db is missing, the
        // user sees a clean error rather than an opaque exception.
        private void helpSampleDbClicked(object oSender, EventArgs oArgs)
        {
            string sAppDir = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
            string sSample = Path.Combine(sAppDir, "sample.db");
            if (!File.Exists(sSample))
            {
                MessageBox.Show(this,
                    "sample.db not found in the DbDuo install folder:\n\n" + sSample
                    + "\n\nIf you installed DbDuo via the regular installer the sample is normally placed here automatically.",
                    "Open Sample Database", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try { openDatabaseAndApplyState(sSample, null); }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Open Sample Database",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

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
            MessageBox.Show(this, "Compare Database is not yet implemented.",
                "Compare Database", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                oFd.Title = "Import Data into " + (db.currentTable ?? "current table");
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
                    MessageBox.Show(this, oEx.Message, "Import Data",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void fileExportClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            using (SaveFileDialog oFd = new SaveFileDialog())
            {
                oFd.Title = "Export Data";
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
                    MessageBox.Show(this, oEx.Message, "Export Data",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void filePrintClicked(object oSender, EventArgs oArgs)
        {
            MessageBox.Show(this, "Print is not yet implemented. Use Export Data to HTML and print from a browser.",
                "Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            using (RecordEditDialog oDlg = new RecordEditDialog("New-Record", lFields, dInitial, lEditable, db))
            {
                if (oDlg.showDialog(this) != DialogResult.OK) return;
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
                    MessageBox.Show(this, oEx.Message, "New Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            using (RecordEditDialog oDlg = new RecordEditDialog("Set-Record", lFields, dInitial, lEditable, db))
            {
                if (oDlg.showDialog(this) != DialogResult.OK) return;
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
                    MessageBox.Show(this, oEx.Message, "Edit Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(this, oEx.Message, "Delete Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            HelpDialog.show(this, "Show Record (row " + db.absolutePosition + ")", oSb.ToString());
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

        // Format a field value for display in Show-Object. The recordset
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
            List<string> lFields = db.getDisplayFieldNames();
            if (lFields == null || lFields.Count == 0)
            { LiveRegion.say("No columns"); return; }
            // Prompt for the column whose cell value to open. The
            // default is the column under virtual focus -- pressing
            // Enter accepts it. Listview has no per-column focus,
            // so the virtual cursor is what stands in for "the
            // column the user means by 'this one'."
            string sColName;
            LbcDialog oDlg = new LbcDialog("Open Cell Value", this);
            try
            {
                string sDefault = virtCurrentColumnName();
                if (string.IsNullOrEmpty(sDefault) || !lFields.Contains(sDefault)) sDefault = lFields[0];
                ListBox lb = oDlg.addPickBox(
                    "&Column whose cell value to open:",
                    lFields, sDefault,
                    "The command opens the URL, file path, or folder path stored in this column of the current row");
                if (!oDlg.runOkCancel()) return;
                if (lb.SelectedItem == null) return;
                sColName = lb.SelectedItem.ToString();
            }
            finally { oDlg.Dispose(); }
            string sValue = "";
            try { sValue = (db.getFieldValue(sColName) ?? "").Trim(); }
            catch { }
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
                    "Open Cell Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ===============================================================
        // Search families. Three families with separate state and
        // separate forward / reverse handler pairs, plus a unified
        // F3 / Shift+F3 "search again" dispatcher that routes to
        // whichever family was last invoked.
        //
        // Jump (Control+J / Control+Shift+J): substring within one
        // user-picked column. The dialog has a column listbox and
        // a substring textbox. Uses the manager's findRecord with
        // an ADO Find expression like "col LIKE '%text%'".
        //
        // Find (Control+F / Control+Shift+F): substring across all
        // visible columns. The dialog has just a substring textbox.
        // Walks the recordset in C# checking every cell.
        //
        // Find-Regex (Control+F3 / Control+Shift+F3): regex across
        // all visible columns. The dialog has just a regex textbox.
        // Walks the recordset in C# applying Regex.IsMatch.
        // ===============================================================

        // Jump-Record (Control+J): prompt for column + substring,
        // jump to the next row whose value in that column contains
        // the substring (case-insensitive).
        private void recJumpClicked(object oSender, EventArgs oArgs)
        {
            doJump(true, false);
        }

        private void recJumpPrevClicked(object oSender, EventArgs oArgs)
        {
            doJump(false, false);
        }

        // doJump: shared between primary chord (prompts the dialog)
        // and the "again" path (uses cached state). When bAgain is
        // true the dialog is skipped.
        private void doJump(bool bForward, bool bAgain)
        {
            if (db == null || !db.hasRecordset()) return;
            if (!bAgain)
            {
                List<string> lCols = new List<string>();
                foreach (string sC in db.getDisplayFieldNames()) lCols.Add(sC);
                if (lCols.Count == 0)
                {
                    MessageBox.Show(this, "No columns to search.",
                        "Jump to Match", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                // Default column: prior Jump column if any, else
                // the column under virtual focus, else first column.
                string sDefaultCol = sLastJumpColumn;
                if (string.IsNullOrEmpty(sDefaultCol) || !lCols.Contains(sDefaultCol))
                    sDefaultCol = virtCurrentColumnName();
                if (string.IsNullOrEmpty(sDefaultCol) || !lCols.Contains(sDefaultCol))
                    sDefaultCol = lCols[0];
                SearchDialogResult oRes = runSearchDialog(
                    "Jump to Match",
                    "Substring (case-insensitive unless Case sensitive is checked):",
                    sLastJumpSubstring,
                    bLastJumpCaseSensitive,
                    SearchHistory.SectionJump,
                    lCols,
                    sDefaultCol);
                if (oRes == null) return;
                sLastJumpColumn = oRes.sColumn;
                sLastJumpSubstring = oRes.sText;
                bLastJumpCaseSensitive = oRes.bCaseSensitive;
                SearchHistory.record(SearchHistory.SectionJump, oRes.sText, oRes.bCaseSensitive);
                sLastSearchKind = "jump";
            }
            if (string.IsNullOrEmpty(sLastJumpColumn) || string.IsNullOrEmpty(sLastJumpSubstring)) return;
            try
            {
                bool bFound = jumpInColumn(sLastJumpColumn, sLastJumpSubstring, bLastJumpCaseSensitive, bForward, bAgain);
                if (!bFound)
                {
                    MessageBox.Show(this,
                        bForward ? "Not found (or no more matches)." : "No earlier matches.",
                        "Jump to Match", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                invokeRefresh();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Jump to Match",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // jumpInColumn: walk the recordset checking only one column
        // for substring containment. Returns true if a match was
        // found (cursor on the matched row); false otherwise (cursor
        // restored). The case-sensitive flag toggles between
        // Ordinal and OrdinalIgnoreCase comparison.
        private bool jumpInColumn(string sCol, string sSub, bool bCaseSensitive, bool bForward, bool bFromCurrent)
        {
            if (string.IsNullOrEmpty(sCol) || string.IsNullOrEmpty(sSub)) return false;
            int iCount = db.recordCount;
            if (iCount == 0) return false;
            int iStart = db.absolutePosition;
            int iFrom = bFromCurrent ? iStart : (bForward ? 1 : iCount);
            int iStep = bForward ? 1 : -1;
            int iPos = bFromCurrent ? iFrom + iStep : iFrom;
            object oOriginal = null;
            try { oOriginal = db.bookmark; } catch { }
            StringComparison oCmp = bCaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;
            while (iPos >= 1 && iPos <= iCount)
            {
                try
                {
                    db.absolutePosition = iPos;
                    string sVal = db.getFieldValue(sCol) ?? "";
                    if (sVal.IndexOf(sSub, oCmp) >= 0)
                        return true;
                }
                catch { }
                iPos += iStep;
            }
            // No match; restore.
            if (oOriginal != null) try { db.bookmark = oOriginal; } catch { }
            return false;
        }

        // Find (Control+F): prompt for substring, match across all
        // visible columns.
        private void recFindAllClicked(object oSender, EventArgs oArgs)
        {
            doFind(true, false);
        }

        private void recFindAllPrevClicked(object oSender, EventArgs oArgs)
        {
            doFind(false, false);
        }

        private void doFind(bool bForward, bool bAgain)
        {
            if (db == null || !db.hasRecordset()) return;
            if (!bAgain)
            {
                SearchDialogResult oRes = runSearchDialog(
                    "Find",
                    "Substring (case-insensitive unless Case sensitive is checked):",
                    sLastFindSubstring,
                    bLastFindCaseSensitive,
                    SearchHistory.SectionFind,
                    null, // no column picker for Find
                    null);
                if (oRes == null) return;
                sLastFindSubstring = oRes.sText;
                bLastFindCaseSensitive = oRes.bCaseSensitive;
                SearchHistory.record(SearchHistory.SectionFind, oRes.sText, oRes.bCaseSensitive);
                sLastSearchKind = "find";
            }
            if (string.IsNullOrEmpty(sLastFindSubstring)) return;
            try
            {
                bool bFound = findAcrossColumns(sLastFindSubstring, bLastFindCaseSensitive, bForward, bAgain);
                if (!bFound)
                {
                    MessageBox.Show(this,
                        bForward ? "Not found (or no more matches)." : "No earlier matches.",
                        "Find", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                invokeRefresh();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Find",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // findAcrossColumns: walk the recordset checking EVERY visible
        // column for substring containment. Case sensitivity is per-
        // user choice via the dialog checkbox.
        private bool findAcrossColumns(string sSub, bool bCaseSensitive, bool bForward, bool bFromCurrent)
        {
            if (string.IsNullOrEmpty(sSub)) return false;
            int iCount = db.recordCount;
            if (iCount == 0) return false;
            List<string> lFields = db.getDisplayFieldNames();
            if (lFields == null || lFields.Count == 0) return false;
            int iStart = db.absolutePosition;
            int iStep = bForward ? 1 : -1;
            int iFrom = bFromCurrent ? iStart : (bForward ? 1 : iCount);
            int iPos = bFromCurrent ? iFrom + iStep : iFrom;
            object oOriginal = null;
            try { oOriginal = db.bookmark; } catch { }
            StringComparison oCmp = bCaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;
            while (iPos >= 1 && iPos <= iCount)
            {
                try
                {
                    db.absolutePosition = iPos;
                    foreach (string sCol in lFields)
                    {
                        string sVal = db.getFieldValue(sCol) ?? "";
                        if (sVal.Length == 0) continue;
                        if (sVal.IndexOf(sSub, oCmp) >= 0)
                            return true;
                    }
                }
                catch { }
                iPos += iStep;
            }
            if (oOriginal != null) try { db.bookmark = oOriginal; } catch { }
            return false;
        }

        // Find-Regex (Control+F3): prompt for regex, match across all
        // visible columns.
        private void recFindRegexClicked(object oSender, EventArgs oArgs)
        {
            doFindRegex(true, false);
        }

        private void recFindRegexPrevClicked(object oSender, EventArgs oArgs)
        {
            doFindRegex(false, false);
        }

        private void doFindRegex(bool bForward, bool bAgain)
        {
            if (db == null || !db.hasRecordset()) return;
            if (!bAgain)
            {
                SearchDialogResult oRes = runSearchDialog(
                    "Find Regex",
                    ".NET regular expression (case-insensitive unless Case sensitive is checked):",
                    sLastFindRegex,
                    bLastRegexCaseSensitive,
                    SearchHistory.SectionRegex,
                    null, // no column picker for Find-Regex
                    null);
                if (oRes == null) return;
                // Pre-compile to validate; bad regex surfaces here
                // rather than crashing inside the walk.
                try
                {
                    System.Text.RegularExpressions.RegexOptions oOpts =
                        oRes.bCaseSensitive
                            ? System.Text.RegularExpressions.RegexOptions.None
                            : System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                    new System.Text.RegularExpressions.Regex(oRes.sText, oOpts);
                }
                catch (Exception oExRe)
                {
                    MessageBox.Show(this, "Invalid regex: " + oExRe.Message,
                        "Find Regex", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                sLastFindRegex = oRes.sText;
                bLastRegexCaseSensitive = oRes.bCaseSensitive;
                SearchHistory.record(SearchHistory.SectionRegex, oRes.sText, oRes.bCaseSensitive);
                sLastSearchKind = "regex";
            }
            if (string.IsNullOrEmpty(sLastFindRegex)) return;
            try
            {
                bool bFound = findRegexAcrossColumns(sLastFindRegex, bLastRegexCaseSensitive, bForward, bAgain);
                if (!bFound)
                {
                    MessageBox.Show(this,
                        bForward ? "Not found (or no more matches)." : "No earlier matches.",
                        "Find Regex", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                invokeRefresh();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Find Regex",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // findRegexAcrossColumns: walk every visible row, check every
        // visible column with Regex.IsMatch. Case sensitivity is
        // controlled by the RegexOptions flag.
        private bool findRegexAcrossColumns(string sPattern, bool bCaseSensitive, bool bForward, bool bFromCurrent)
        {
            if (string.IsNullOrEmpty(sPattern)) return false;
            int iCount = db.recordCount;
            if (iCount == 0) return false;
            List<string> lFields = db.getDisplayFieldNames();
            if (lFields == null || lFields.Count == 0) return false;
            System.Text.RegularExpressions.Regex oRe;
            try
            {
                System.Text.RegularExpressions.RegexOptions oOpts =
                    bCaseSensitive
                        ? System.Text.RegularExpressions.RegexOptions.None
                        : System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                oRe = new System.Text.RegularExpressions.Regex(sPattern, oOpts);
            }
            catch { return false; }
            int iStart = db.absolutePosition;
            int iStep = bForward ? 1 : -1;
            int iFrom = bFromCurrent ? iStart : (bForward ? 1 : iCount);
            int iPos = bFromCurrent ? iFrom + iStep : iFrom;
            object oOriginal = null;
            try { oOriginal = db.bookmark; } catch { }
            while (iPos >= 1 && iPos <= iCount)
            {
                try
                {
                    db.absolutePosition = iPos;
                    foreach (string sCol in lFields)
                    {
                        string sVal = db.getFieldValue(sCol) ?? "";
                        if (sVal.Length == 0) continue;
                        if (oRe.IsMatch(sVal)) return true;
                    }
                }
                catch { }
                iPos += iStep;
            }
            if (oOriginal != null) try { db.bookmark = oOriginal; } catch { }
            return false;
        }

        // SearchDialogResult: captures everything the search-family
        // dialogs need to return -- the user's text, the case-
        // sensitive flag, and (for Jump-Record only) the selected
        // column.
        private class SearchDialogResult
        {
            public string sText;
            public bool   bCaseSensitive;
            public string sColumn;
        }

        // runSearchDialog: shared dialog implementation for the three
        // search families. Builds an LBC-style modal dialog with:
        //
        //   - Optional column listbox (only when lCols != null)
        //   - Text input populated with the prior term
        //   - Recent list -- up to 10 most-recent terms from the
        //     family's history section. Selecting an entry copies
        //     its text to the input AND sets the case-sensitive
        //     checkbox to how that term was used.
        //   - Case-sensitive checkbox (off by default)
        //   - OK / Cancel buttons
        //
        // Returns null if the user cancelled or entered no text.
        private SearchDialogResult runSearchDialog(
            string sTitle,
            string sTextLabel,
            string sInitialText,
            bool   bInitialCase,
            string sHistorySection,
            List<string> lCols,
            string sInitialCol)
        {
            // The dialog is a standalone Form rather than an
            // LbcDialog because we need event wiring on the Recent
            // listbox (SelectedIndexChanged) that updates other
            // controls. LbcDialog's API exposes its widgets but the
            // event wiring is cleaner inline. The visual layout
            // mirrors LbcDialog conventions (label above field,
            // status-bar focus-tip, 12-pixel margin) so a screen-
            // reader user gets the same Tab-order experience.
            using (Form oDlg = new Form())
            {
                oDlg.Text = sTitle;
                oDlg.StartPosition = FormStartPosition.CenterParent;
                oDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                oDlg.MaximizeBox = false;
                oDlg.MinimizeBox = false;
                oDlg.ShowInTaskbar = false;
                oDlg.KeyPreview = true;
                oDlg.ClientSize = new Size(560, lCols != null ? 420 : 380);

                int iY = 12;
                int iLineHeight = 24;
                int iLabelHeight = 18;
                int iControlWidth = 536;

                // Optional column picker. Only present for the Jump
                // family; Find and Find-Regex omit it.
                ComboBox cbCol = null;
                if (lCols != null && lCols.Count > 0)
                {
                    Label lblCol = new Label();
                    lblCol.Text = "&Column to search:";
                    lblCol.Location = new Point(12, iY);
                    lblCol.Size = new Size(iControlWidth, iLabelHeight);
                    oDlg.Controls.Add(lblCol);
                    iY += iLabelHeight + 2;

                    cbCol = new ComboBox();
                    cbCol.DropDownStyle = ComboBoxStyle.DropDownList;
                    cbCol.Location = new Point(12, iY);
                    cbCol.Size = new Size(iControlWidth, iLineHeight);
                    foreach (string sC in lCols) cbCol.Items.Add(sC);
                    string sDefaultCol = (!string.IsNullOrEmpty(sInitialCol) && lCols.Contains(sInitialCol))
                        ? sInitialCol : lCols[0];
                    cbCol.SelectedItem = sDefaultCol;
                    cbCol.AccessibleName = "Column to search";
                    oDlg.Controls.Add(cbCol);
                    iY += iLineHeight + 8;
                }

                // Text input
                Label lblText = new Label();
                lblText.Text = "&Text: " + sTextLabel;
                lblText.Location = new Point(12, iY);
                lblText.Size = new Size(iControlWidth, iLabelHeight * 2);
                oDlg.Controls.Add(lblText);
                iY += lblText.Height + 2;

                TextBox tbText = new TextBox();
                tbText.Location = new Point(12, iY);
                tbText.Size = new Size(iControlWidth, iLineHeight);
                tbText.Text = sInitialText ?? "";
                tbText.AccessibleName = "Text";
                oDlg.Controls.Add(tbText);
                iY += iLineHeight + 8;

                // Recent listbox
                Label lblRecent = new Label();
                lblRecent.Text = "&Recent (up to 10; Enter or click to use):";
                lblRecent.Location = new Point(12, iY);
                lblRecent.Size = new Size(iControlWidth, iLabelHeight);
                oDlg.Controls.Add(lblRecent);
                iY += iLabelHeight + 2;

                ListBox lbRecent = new ListBox();
                lbRecent.Location = new Point(12, iY);
                lbRecent.Size = new Size(iControlWidth, 8 * 18);
                lbRecent.AccessibleName = "Recent searches";
                List<SearchHistory.Entry> lHistory = SearchHistory.load(sHistorySection);
                foreach (SearchHistory.Entry e in lHistory)
                {
                    string sDisp = e.sTerm;
                    if (e.bCaseSensitive) sDisp += "   [Aa]";
                    lbRecent.Items.Add(sDisp);
                }
                oDlg.Controls.Add(lbRecent);
                iY += lbRecent.Height + 8;

                // Case-sensitive checkbox
                CheckBox cbCase = new CheckBox();
                cbCase.Text = "Case &sensitive";
                cbCase.Checked = bInitialCase;
                cbCase.Location = new Point(12, iY);
                cbCase.Size = new Size(iControlWidth, iLineHeight);
                cbCase.AccessibleName = "Case sensitive";
                oDlg.Controls.Add(cbCase);
                iY += iLineHeight + 8;

                // Buttons: OK and Cancel
                Button btnOk = new Button();
                btnOk.Text = "&OK";
                btnOk.DialogResult = DialogResult.OK;
                btnOk.Size = new Size(90, 28);
                btnOk.Location = new Point(360, iY);
                oDlg.Controls.Add(btnOk);

                Button btnCancel = new Button();
                btnCancel.Text = "&Cancel";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.Size = new Size(90, 28);
                btnCancel.Location = new Point(456, iY);
                oDlg.Controls.Add(btnCancel);

                oDlg.AcceptButton = btnOk;
                oDlg.CancelButton = btnCancel;

                // Wire Recent selection: copy term to Text, set
                // case-sensitive checkbox to how that term was last
                // used. Don't auto-OK -- the user may want to edit.
                lbRecent.SelectedIndexChanged += delegate(object oS, EventArgs oA)
                {
                    int iIdx = lbRecent.SelectedIndex;
                    if (iIdx < 0 || iIdx >= lHistory.Count) return;
                    tbText.Text = lHistory[iIdx].sTerm;
                    cbCase.Checked = lHistory[iIdx].bCaseSensitive;
                };

                // Double-click in Recent acts as OK with that entry.
                lbRecent.DoubleClick += delegate(object oS, EventArgs oA)
                {
                    int iIdx = lbRecent.SelectedIndex;
                    if (iIdx >= 0 && iIdx < lHistory.Count)
                    {
                        tbText.Text = lHistory[iIdx].sTerm;
                        cbCase.Checked = lHistory[iIdx].bCaseSensitive;
                        oDlg.DialogResult = DialogResult.OK;
                        oDlg.Close();
                    }
                };

                // Initial focus on the text box (most common case);
                // the user can Tab to Recent / Case / OK.
                oDlg.Shown += delegate(object oS, EventArgs oA)
                {
                    tbText.Focus();
                    tbText.SelectAll();
                };

                if (oDlg.ShowDialog(this) != DialogResult.OK) return null;
                string sText = (tbText.Text ?? "").Trim();
                if (string.IsNullOrEmpty(sText)) return null;
                SearchDialogResult oResult = new SearchDialogResult();
                oResult.sText = sText;
                oResult.bCaseSensitive = cbCase.Checked;
                oResult.sColumn = (cbCol != null && cbCol.SelectedItem != null) ? cbCol.SelectedItem.ToString() : "";
                return oResult;
            }
        }

        // Search-Next (F3): repeat whichever search family was last
        // invoked, forward. Shift+F3 is the reverse counterpart.
        // EdSharp uses F3 / Shift+F3 for "find again" -- DbDuo
        // generalizes this to also include the Jump family.
        private void recSearchNextClicked(object oSender, EventArgs oArgs)
        {
            recSearchAgain(true);
        }

        private void recSearchPrevClicked(object oSender, EventArgs oArgs)
        {
            recSearchAgain(false);
        }

        private void recSearchAgain(bool bForward)
        {
            switch (sLastSearchKind)
            {
                case "jump":
                    doJump(bForward, true);
                    break;
                case "find":
                    doFind(bForward, true);
                    break;
                case "regex":
                    doFindRegex(bForward, true);
                    break;
                default:
                    MessageBox.Show(this,
                        "No prior search. Use Control+F to Find, Control+J to Jump-Record, "
                        + "or Control+F3 to Find-Regex first.",
                        "Search-Next", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
            }
        }

        // Set-Position: jump to a numbered row (1-based). Prompts for the
        // target row number. Bound to Control+G by EdSharp convention for
        // "Go to Percent" (the closest editor analog to "go to row N").
        private void recGoToClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            int iCount = db.recordCount;
            if (iCount <= 0) { MessageBox.Show(this, "No rows.", "Go to Row", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
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
                { MessageBox.Show(this, "Enter a percent between 0 and 100.", "Go to Row", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                iTarget = (int)Math.Round(iCount * n / 100.0);
                if (iTarget < 1) iTarget = 1;
            }
            else if (!int.TryParse(sInput, out iTarget))
            { MessageBox.Show(this, "Enter a row number or percent.", "Go to Row", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (iTarget < 1) iTarget = 1;
            if (iTarget > iCount) iTarget = iCount;
            try
            {
                db.absolutePosition = iTarget;
                invokeRefresh();
            }
            catch (Exception oEx) { MessageBox.Show(this, oEx.Message, "Go to Row", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // Save-Bookmark: remember the current row's bookmark so the user
        // can wander and return. Bound to Control+K by EdSharp convention
        // for "Set Bookmark." Only one bookmark slot is kept; saving a
        // new one replaces any previous.
        private object oSavedBookmark;
        // Step-Record menu items. Each fires the manager's
        // corresponding navigation method, then refreshes the
        // ListView and announces position via the status bar.
        // These give the Navigate menu first-class home for what
        // the dot prompt already does via "first", "last", "next",
        // "previous" -- they should be parallel everywhere.
        private void navFirstClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            try { db.moveFirst(); invokeRefresh(); }
            catch (Exception oEx) { LiveRegion.say("Step-Record-First: " + oEx.Message); }
        }

        private void navLastClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            try { db.moveLast(); invokeRefresh(); }
            catch (Exception oEx) { LiveRegion.say("Step-Record-Last: " + oEx.Message); }
        }

        private void navNextClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            try
            {
                if (db.absolutePosition < db.recordCount) db.absolutePosition = db.absolutePosition + 1;
                invokeRefresh();
            }
            catch (Exception oEx) { LiveRegion.say("Step-Record-Next: " + oEx.Message); }
        }

        private void navPrevClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            try
            {
                if (db.absolutePosition > 1) db.absolutePosition = db.absolutePosition - 1;
                invokeRefresh();
            }
            catch (Exception oEx) { LiveRegion.say("Step-Record-Previous: " + oEx.Message); }
        }

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
            catch (Exception oEx) { MessageBox.Show(this, oEx.Message, "Save Bookmark", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // Restore-Bookmark: jump back to the saved row. Bound to Alt+K by
        // EdSharp convention for "Go to Bookmark."
        private void recGotoBookmarkClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset()) return;
            if (oSavedBookmark == null)
            { MessageBox.Show(this, "No bookmark saved. Use Save-Bookmark (Control+K) first.", "Go to Bookmark", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            try
            {
                db.bookmark = oSavedBookmark;
                invokeRefresh();
                LiveRegion.say("Returned to bookmarked row " + db.absolutePosition);
            }
            catch (Exception oEx) { MessageBox.Show(this, oEx.Message, "Go to Bookmark", MessageBoxButtons.OK, MessageBoxIcon.Error); }
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
                    MessageBox.Show(this, oEx.Message, "Filter Records", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show(this, oEx.Message, "Custom Sort", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void viewSortAscClicked(object oSender, EventArgs oArgs) { sortByPickedColumn(true); }
        private void viewSortDescClicked(object oSender, EventArgs oArgs) { sortByPickedColumn(false); }

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

        // sortByPickedColumn: prompt for a column via LBC dialog,
        // then sort by it. Per Jamal's listview-not-grid spec,
        // Sort-Ascending and Sort-Descending no longer read the
        // Tab-tracked "current column" -- that's a screen-reader
        // announcement helper, not a command-targeting mechanism.
        // The user always picks the column explicitly.
        private void sortByPickedColumn(bool bAsc)
        {
            if (db == null || !db.hasRecordset()) return;
            List<string> lCols = db.getDisplayFieldNames();
            if (lCols == null || lCols.Count == 0)
            {
                LiveRegion.say("No columns to sort by");
                return;
            }
            // Default to the column under virtual focus. Pressing
            // Enter accepts it; the user can arrow up/down through
            // the listbox to pick a different column.
            string sDefault = virtCurrentColumnName();
            if (string.IsNullOrEmpty(sDefault) || !lCols.Contains(sDefault)) sDefault = lCols[0];
            LbcDialog oDlg = new LbcDialog(bAsc ? "Sort Ascending" : "Sort Descending", this);
            try
            {
                ListBox lb = oDlg.addPickBox(
                    "&Column to sort by " + (bAsc ? "ascending:" : "descending:"),
                    lCols, sDefault,
                    "Pick the column whose values determine the row order");
                if (!oDlg.runOkCancel()) return;
                if (lb.SelectedItem == null) return;
                string sCol = lb.SelectedItem.ToString();
                try
                {
                    db.sort = sCol + (bAsc ? " ASC" : " DESC");
                    invokeRefresh();
                    LiveRegion.say("Sorted by " + sCol + (bAsc ? ", ascending" : ", descending"));
                }
                catch (Exception oEx)
                {
                    MessageBox.Show(this, oEx.Message,
                        bAsc ? "Sort Ascending" : "Sort Descending",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally { oDlg.Dispose(); }
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
            try
            {
                db.requery();
                invokeRefresh();
                // Per spec: F5 refresh resyncs the virtual cursor to
                // row 1, first column. Keyboard focus follows.
                virtResetToTop();
                LiveRegion.say("Refreshed");
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this, oEx.Message, "Refresh View", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(this, oEx.Message, "Next Table or View", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        // recExitChildToRootClicked (Alt+Home): pop the entire drill
        // stack and return to the topmost parent table -- the one the
        // user was on before the first Enter-Child of the chain.
        // FileDir's Alt+Home pattern of "go all the way back to the
        // root" generalizes here from folder hierarchy to table
        // drill-down.
        //
        // Implementation: repeatedly invoke Exit-Child until the
        // drill stack empties. Each pop re-finds the parent row by
        // its primary-key value (the same robustness logic
        // recExitChildClicked uses), so any structural changes the
        // user made in child tables don't strand us on the wrong
        // row. If a single Exit-Child step fails partway through,
        // we stop there and let the user retry; we don't try to
        // recover automatically.
        private void recExitChildToRootClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.isOpen()) return;
            if (oDrillStack.Count == 0)
            {
                MessageBox.Show(this,
                    "Already at the root -- the drill-down stack is empty.",
                    "Exit-ChildToRoot", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int iLevels = oDrillStack.Count;
            while (oDrillStack.Count > 0)
            {
                int iBefore = oDrillStack.Count;
                recExitChildClicked(oSender, oArgs);
                if (oDrillStack.Count == iBefore)
                {
                    // Exit-Child didn't make progress; bail rather
                    // than loop forever.
                    break;
                }
            }
            // The per-step Exit-Child already announced its return
            // via the live region; a final confirmation here helps
            // the user know they popped multiple levels at once.
            LiveRegion.say("Returned to root (" + iLevels + " level" + (iLevels == 1 ? "" : "s") + ")");
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
                MessageBox.Show(this, "No table is open.", "Choose Visible Columns",
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

        // New-Chart: produce a frequency bar chart of how often each
        // distinct value appears in one column of the current
        // filtered view. The result is an .xlsx with a Data sheet
        // and a Chart sheet, opened in Excel.
        //
        // No hotkey by default; reached through Tools > New-Chart
        // or the dot prompt's New-Chart command. Sample workflows
        // worth trying with sample.db:
        //   - On enrollments: New-Chart grade           (A, B+, etc.)
        //   - On students:    New-Chart year            (Sophomore...)
        //   - On students:    New-Chart major           (CS, Bio...)
        //   - On teachers:    New-Chart department
        //
        // For meaningful charts, the column should be categorical
        // (a small number of distinct values relative to the row
        // count). Charting a unique-per-row column like a name
        // produces a bar per row with count 1 -- valid output,
        // but not informative. The dialog warns when distinct-value
        // count equals row count.
        private void toolsChartClicked(object oSender, EventArgs oArgs)
        {
            if (db == null || !db.hasRecordset())
            {
                MessageBox.Show(this, "Open a table first.",
                    "New-Chart", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (db.recordCount == 0)
            {
                MessageBox.Show(this, "No records to chart in the current filter.",
                    "New-Chart", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Build the column picker dialog with LbcDialog. The
            // user picks a visible column (we include all display
            // fields, not just textline-typed ones, so they can
            // chart any column they can see in the data list).
            List<string> lCols = db.getDisplayFieldNames();
            if (lCols.Count == 0) lCols = db.getFieldNames();
            if (lCols.Count == 0) return;

            string sColumn;
            using (LbcDialog oDlg = new LbcDialog("New-Chart", this))
            {
                oDlg.addLabel("Choose a column to chart. The chart will show how often");
                oDlg.addLabel("each distinct value appears in the current filtered view.");
                oDlg.addSeparator();
                ComboBox cbCol = oDlg.addComboPickBox("Column:",
                    lCols, lCols[0],
                    "Pick a categorical column with a few distinct values");
                if (!oDlg.runOkCancel()) return;
                sColumn = (cbCol.SelectedItem ?? "").ToString();
            }
            if (string.IsNullOrEmpty(sColumn)) return;

            // Build destination filename next to the database file.
            // Pattern: <database-folder>\<table>-by-<column>-chart.xlsx
            string sDbPath = db.filePath ?? "";
            string sFolder = string.IsNullOrEmpty(sDbPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : Path.GetDirectoryName(sDbPath);
            string sTable = db.currentTable ?? "table";
            string sCleanCol = sColumn.Replace(" ", "_").Replace("/", "_");
            string sDest = Path.Combine(sFolder,
                sTable + "-by-" + sCleanCol + "-chart.xlsx");

            try
            {
                int iDistinct = db.exportFrequencyChart(sColumn, sDest);
                LiveRegion.say("Chart saved with " + iDistinct + " distinct values; opening in Excel");
                try { System.Diagnostics.Process.Start(sDest); } catch { }
            }
            catch (Exception oEx)
            {
                MessageBox.Show(this,
                    "Could not create chart:\n\n" + oEx.Message
                    + "\n\nNote: New-Chart requires Excel to be installed.",
                    "New-Chart", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            string sCurrentEcho   = readIniValue(sUserIni, "Options", "commandEcho", "Y");
            bool bCurrentEcho = !(sCurrentEcho.Equals("N", StringComparison.OrdinalIgnoreCase)
                              || sCurrentEcho.Equals("No", StringComparison.OrdinalIgnoreCase)
                              || sCurrentEcho.Equals("0", StringComparison.Ordinal)
                              || sCurrentEcho.Equals("False", StringComparison.OrdinalIgnoreCase));

            LbcDialog oDlg = new LbcDialog("Edit-Configuration", this);
            try
            {
                oDlg.addLabel("Settings stored in: " + sUserIni);
                oDlg.addLabel("Most changes take effect the next time DbDuo starts.");
                oDlg.addSeparator();

                // uiMode is a pick-from-three -- ComboBox is perfect.
                // The focus-tip string appears in the status bar at
                // the bottom of the dialog when the user tabs in.
                ComboBox cbUiMode = oDlg.addComboPickBox(
                    "UI mode at launch (overridden by -cli / -gui / -both):",
                    new string[] { "both", "gui", "cli" },
                    sCurrentUiMode,
                    "both = GUI + console, gui = window only, cli = dot prompt only");

                // Command Echo toggle. ON by default; speaks the
                // canonical command name through the live region
                // just before each command runs. EdSharp's
                // 'ExtraSpeech=Y' setting is the model. Takes effect
                // on the next command, no restart needed.
                CheckBox cbEcho = oDlg.addCheckBox(
                    "Command Echo (speak each command name as it runs)",
                    bCurrentEcho,
                    "When ON, every menu/hotkey command announces its name through the screen reader before executing. Off-by-toggle for users who prefer less speech.");

                oDlg.addSeparator();
                oDlg.addLabel("For [Keys] overrides and advanced settings, use the");
                oDlg.addLabel("\"Open file...\" button to edit DbDuo.ini directly.");

                string sResult = oDlg.runWithButtons(new string[] { "OK", "Open file...", "Cancel" });
                if (string.Equals(sResult, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    string sNewUiMode = (cbUiMode.SelectedItem ?? "both").ToString();
                    string sNewEcho = cbEcho.Checked ? "Y" : "N";
                    writeIniValue(sUserIni, "General", "uiMode", sNewUiMode);
                    writeIniValue(sUserIni, "Options", "commandEcho", sNewEcho);
                    invalidateCommandEchoCache();
                    LiveRegion.say("Configuration saved");
                    DbDuoLog.write("Edit-Configuration saved: uiMode=" + sNewUiMode + "; commandEcho=" + sNewEcho);
                    MessageBox.Show(this,
                        "Configuration saved to " + sUserIni
                        + "\n\nuiMode takes effect on next DbDuo launch."
                        + "\nCommand Echo takes effect immediately.",
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

        // Show-Readme: open README.htm (the quick-start / summary
        // guide) in the system browser. Equivalent to Get-Help
        // (F1) but points at README.htm instead of DbDuo.htm.
        // README is shorter and more introductory; DbDuo.htm is
        // the full reference.
        private void helpReadmeClicked(object oSender, EventArgs oArgs)
        {
            string sPath = System.IO.Path.Combine(
                Application.StartupPath, "README.htm");
            if (System.IO.File.Exists(sPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(sPath);
                    DbDuoLog.write("Show-Readme: " + sPath);
                    return;
                }
                catch (Exception oEx)
                {
                    MessageBox.Show(this,
                        "Could not open the Readme:\n\n" + oEx.Message
                        + "\n\nThe file exists at:\n" + sPath,
                        "Show-Readme",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(this,
                    "README.htm was not found next to DbDuo.exe.\n\n"
                    + "Expected location:\n" + sPath + "\n\n"
                    + "Reinstall DbDuo or copy README.htm from the source bundle.",
                    "Show-Readme",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Show-History: open History.htm in the default browser.
        // Bound to Shift+F1 by the EdSharp / FileDir convention for
        // "History of Changes." The file is shipped next to the EXE
        // and contains the release notes that used to live in
        // README.md's "What's new in vX.Y.Z" sections.
        private void helpHistoryClicked(object oSender, EventArgs oArgs)
        {
            string sPath = System.IO.Path.Combine(
                Application.StartupPath, "History.htm");
            if (System.IO.File.Exists(sPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(sPath);
                    DbDuoLog.write("Show-History: " + sPath);
                    return;
                }
                catch (Exception oEx)
                {
                    MessageBox.Show(this,
                        "Could not open the History:\n\n" + oEx.Message
                        + "\n\nThe file exists at:\n" + sPath,
                        "Show-History",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(this,
                    "History.htm was not found next to DbDuo.exe.\n\n"
                    + "Expected location:\n" + sPath + "\n\n"
                    + "Reinstall DbDuo or copy History.htm from the source bundle.",
                    "Show-History",
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
            // Use sayForced: Test-Reader's whole purpose is to verify
            // speech, so it must speak even when Extra-Speech is off.
            LiveRegion.sayForced("Speech path test. If you hear this, the speech pipeline is working.");
            string sDiag = LiveRegion.speechDiagnostic();
            MessageBox.Show(this, sDiag, "Test-Reader",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // helpExtraSpeechClicked: toggle whether DbDuo emits its own
        // direct speech messages (status announcements, command echo,
        // virtual-cell readouts) through the screen reader. When OFF
        // the screen reader still hears DbDuo via its own natural
        // focus and selection announcements; the toggle suppresses
        // only the additional commentary DbDuo adds on top. EdSharp
        // and FileDir both ship a similar toggle. The setting is
        // persisted in DbDuo.ini [General] extraSpeech.
        private void helpExtraSpeechClicked(object oSender, EventArgs oArgs)
        {
            LiveRegion.bExtraSpeechEnabled = !LiveRegion.bExtraSpeechEnabled;
            miHelpExtraSpeech.Checked = LiveRegion.bExtraSpeechEnabled;
            string sUserIni = Path.Combine(Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "DbDuo.ini");
            try { writeIniValue(sUserIni, "General", "extraSpeech", LiveRegion.bExtraSpeechEnabled ? "Y" : "N"); }
            catch { /* best-effort persist; the runtime state is the source of truth */ }
            // Force-speak the new state so the user hears confirmation
            // even when they just turned speech off.
            LiveRegion.sayForced(LiveRegion.bExtraSpeechEnabled
                ? "Extra speech on"
                : "Extra speech off");
        }

        // =====================================================================
        // Snippet commands. EdSharp's Invoke / View Snippet pattern
        // adapted for DbDuo. The user manages snippets as plain files
        // in %APPDATA%\DbDuo\Snippets\, edited in their own choice of
        // external editor (Notepad by default; override via DbDuo.ini
        // [Snippets] editor=). .js files are executed as JScript .NET
        // against the running DbDuoForm; everything else is shown as
        // plain text in a MessageBox.
        //
        // No Save-Snippet command (DbDuo has no analogous "save
        // selected text" workflow since it is not a text editor).
        // Edit Snippet handles both editing an existing file and
        // creating a new one via a "[New snippet...]" entry at the
        // top of the pick list.
        // =====================================================================

        // Sentinel string for the "create new" pick-list entry. Square
        // brackets are conventional in screen-reader UI to mark a
        // non-data option in a list (JAWS reads "left bracket new
        // snippet right bracket"); they also sort before any real
        // filename in OrdinalIgnoreCase order so the entry stays at
        // the top of the list.
        private const string NewSnippetSentinel = "[New snippet...]";

        // miscInvokeSnippetClicked: pick a snippet from the folder
        // and run it. .js -> JScript .NET execution via dbDuoEval.dll;
        // everything else -> show file contents in a MessageBox as
        // reference text. Output (last expression value or
        // "ERROR: ..." string) is shown in a MessageBox so the screen
        // reader reads it through the standard dialog focus path.
        private void miscInvokeSnippetClicked(object sender, EventArgs args)
        {
            string[] aNames = SnippetHelper.listSnippets();
            if (aNames.Length == 0)
            {
                MessageBox.Show(this,
                    "No snippets found in:\n\n" + SnippetHelper.getSnippetDir()
                    + "\n\nUse Edit Snippet to create one, or Open Snippet Folder to manage files in Explorer.",
                    "Invoke Snippet", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Standard LbcDialog pick; native ListBox under the hood,
            // read line-by-line by every screen reader.
            using (LbcDialog dlg = new LbcDialog("Invoke Snippet", this))
            {
                dlg.addLabel("&Snippet:");
                ListBox lb = dlg.addListBox(aNames, aNames[0], null);
                if (!dlg.runOkCancel()) return;
                if (lb.SelectedItem == null) return;
                string sName = lb.SelectedItem.ToString();
                string sPath = System.IO.Path.Combine(SnippetHelper.getSnippetDir(), sName);
                string sResult = SnippetHelper.runSnippet(sPath, this, this.db);
                MessageBox.Show(this,
                    string.IsNullOrEmpty(sResult) ? "(no output)" : sResult,
                    "Invoke Snippet: " + sName,
                    MessageBoxButtons.OK,
                    sResult != null && sResult.StartsWith("ERROR:")
                        ? MessageBoxIcon.Error : MessageBoxIcon.Information);
            }
        }

        // miscEditSnippetClicked: edit an existing snippet OR create
        // a new one. If snippets already exist, the pick list shows
        // them with "[New snippet...]" at the top. If the folder is
        // empty, jump straight to the new-file workflow. Either way
        // the chosen path is then opened in the user's editor.
        private void miscEditSnippetClicked(object sender, EventArgs args)
        {
            string[] aExisting = SnippetHelper.listSnippets();
            string sPath;

            if (aExisting.Length == 0)
            {
                // No snippets yet: skip the pick and go straight to
                // the Save File dialog so the user can name the new
                // file.
                sPath = promptForNewSnippetPath();
                if (string.IsNullOrEmpty(sPath)) return;
            }
            else
            {
                // Build a pick list with "[New snippet...]" at the
                // top followed by existing names. Array.Sort on the
                // existing names happens inside listSnippets() so
                // they are already alphabetical here.
                string[] aChoices = new string[aExisting.Length + 1];
                aChoices[0] = NewSnippetSentinel;
                Array.Copy(aExisting, 0, aChoices, 1, aExisting.Length);

                string sPicked;
                using (LbcDialog dlg = new LbcDialog("Edit Snippet", this))
                {
                    dlg.addLabel("&Snippet:");
                    ListBox lb = dlg.addListBox(aChoices, aChoices[0], null);
                    if (!dlg.runOkCancel()) return;
                    if (lb.SelectedItem == null) return;
                    sPicked = lb.SelectedItem.ToString();
                }

                if (sPicked == NewSnippetSentinel)
                {
                    sPath = promptForNewSnippetPath();
                    if (string.IsNullOrEmpty(sPath)) return;
                }
                else
                {
                    sPath = System.IO.Path.Combine(SnippetHelper.getSnippetDir(), sPicked);
                }
            }

            if (!SnippetHelper.openInEditor(sPath))
            {
                MessageBox.Show(this,
                    "Could not launch the editor. Set [Snippets] editor= in DbDuo.ini to a working editor path.",
                    "Edit Snippet", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Standard Save File dialog seeded to the Snippets folder.
        // Returns the chosen path, or empty string on cancel. Used by
        // miscEditSnippetClicked when the user wants a new snippet.
        private string promptForNewSnippetPath()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "New Snippet";
                sfd.InitialDirectory = SnippetHelper.getSnippetDir();
                sfd.Filter = "JScript .NET (*.js)|*.js|Text (*.txt)|*.txt|SQL (*.sql)|*.sql|All Files (*.*)|*.*";
                sfd.DefaultExt = "js";
                sfd.AddExtension = true;
                sfd.OverwritePrompt = true;
                if (sfd.ShowDialog(this) != DialogResult.OK) return "";
                return sfd.FileName;
            }
        }

        // miscOpenSnippetFolderClicked: shellexec the Snippets folder
        // so the user can manage files in Explorer. The folder is
        // created on first access by SnippetHelper.getSnippetDir() so
        // this command never fails for a "folder does not exist"
        // reason.
        private void miscOpenSnippetFolderClicked(object sender, EventArgs args)
        {
            string sDir = SnippetHelper.getSnippetDir();
            try
            {
                System.Diagnostics.ProcessStartInfo psi =
                    new System.Diagnostics.ProcessStartInfo(sDir);
                psi.UseShellExecute = true;
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Could not open the snippet folder:\n\n" + sDir + "\n\n" + ex.Message,
                    "Open Snippet Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                + "An accessible, keyboard-first database manager for Windows.\n"
                + "Opens SQLite, Microsoft Access, Excel, dBASE, and delimited\n"
                + "text through one set of PowerShell-flavored commands, in a\n"
                + "GUI window and a dot-prompt console at the same time.\n"
                + "\n"
                + "Designed for screen-reader use from the ground up. JAWS,\n"
                + "NVDA, and Narrator are all first-class through dedicated\n"
                + "speech paths. Table-style cell navigation, direction-aware\n"
                + "announcements, and the double-press-spells convention\n"
                + "familiar from EdSharp and FileDir.\n"
                + "\n"
                + "C# / .NET Framework 4.8 / x64 / WinForms.\n"
                + "Database access via ADODB COM interop.\n"
                + "Built around Microsoft's PowerShell verb taxonomy.\n"
                + "\n"
                + "https://github.com/JamalMazrui/DbDuo\n"
                + "MIT License.\n");
        }

        // Elevate-Version (F11): check the GitHub Releases API for
        // a newer DbDuo version, and if found, download the latest
        // DbDuo_setup.exe and run it. The Inno Setup installer
        // detects the running DbDuo and offers to close it, so
        // no manual exit is required.
        private void helpElevateClicked(object oSender, EventArgs oArgs)
        {
            ElevateVersion.run();
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
        private static string sLastFindRegexCli = "";
        private static string sLastFindRegexColumnCli = "";
        // sLastFindKindCli mirrors the GUI's sLastFindKind: "plain",
        // "regex", or "" (no Find issued yet). jump-recordagain /
        // jump-recordprevious dispatch on this so the same chord
        // family repeats whichever was last fired.
        private static string sLastFindKindCli = "";

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
                    if (sLine.Length == 0) { printPositionOnly(); continue; }
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

        // Print just "N of M" for the current recordset. Used when the
        // user presses bare Enter at the dot prompt: they already know
        // which table is open and the word "row" is redundant when the
        // ratio N-of-M is the only piece they actually need to hear.
        // Filter / sort are also omitted -- those are best heard via
        // an explicit Say-Status or Say-Yield command.
        private static void printPositionOnly()
        {
            DbDuoManager oDb = db;
            if (oDb == null) { Console.WriteLine("(no manager)"); return; }
            if (!oDb.isOpen()) { Console.WriteLine("(no database open)"); return; }
            if (string.IsNullOrEmpty(oDb.currentTable)) { Console.WriteLine("(no table selected)"); return; }
            Console.WriteLine(oDb.absolutePosition + " of " + oDb.recordCount);
        }

        // =======================================================================
        // CLI implementations of the speech-only and new action commands.
        // The GUI variants live on DbDuoForm and push through LiveRegion;
        // these CLI variants print the same content to the console.
        // =======================================================================

        private static void cmdSayStatus()       { printRowSummary(); }

        private static void cmdSayPath()
        {
            if (db == null || !db.isOpen()) { Console.WriteLine("(no database open)"); return; }
            Console.WriteLine(db.filePath ?? "(unknown)");
        }

        private static void cmdSayYield()
        {
            if (!requireRecordset()) return;
            string sMsg = db.recordCount + " row" + (db.recordCount == 1 ? "" : "s");
            if (db.filter.Length > 0) sMsg += " (filter: " + db.filter + ")";
            Console.WriteLine(sMsg);
        }

        private static void cmdSayTables()
        {
            if (db == null || !db.isOpen()) { Console.WriteLine("(no database open)"); return; }
            List<string> lTables = db.visitedTableNames();
            if (lTables == null || lTables.Count == 0)
            { Console.WriteLine("(no tables visited yet in this session)"); return; }
            foreach (string s in lTables) Console.WriteLine(s);
        }

        private static void cmdSayMarked()
        {
            if (!requireRecordset()) return;
            if (!db.hasField(Metadata.MarkedColumn))
            { Console.WriteLine("(this table has no marked column)"); return; }
            object oOriginal = null;
            try { oOriginal = db.bookmark; } catch { }
            int iCount = 0;
            try
            {
                for (int i = 1; i <= db.recordCount; i++)
                {
                    try
                    {
                        db.absolutePosition = i;
                        if (!db.getFieldValue(Metadata.MarkedColumn).Trim().Equals("true",
                            StringComparison.OrdinalIgnoreCase)
                            && db.getFieldValue(Metadata.MarkedColumn).Trim() != "1") continue;
                        string sLook = db.getFieldValue("look");
                        if (string.IsNullOrEmpty(sLook)) sLook = "row " + i;
                        Console.WriteLine(sLook);
                        iCount++;
                    }
                    catch { }
                }
            }
            finally
            {
                if (oOriginal != null) try { db.bookmark = oOriginal; } catch { }
            }
            if (iCount == 0) Console.WriteLine("(no marked rows)");
        }

        private static void cmdSayDate()
        {
            if (!requireRecordset()) return;
            string sCol = db.hasField("updated") ? "updated" : (db.hasField("added") ? "added" : null);
            if (sCol == null) { Console.WriteLine("(no date column)"); return; }
            Console.WriteLine(sCol + ": " + (db.getFieldValue(sCol) ?? "(empty)"));
        }

        private static void cmdSayType()
        {
            if (db == null || !db.isOpen()) { Console.WriteLine("(no database open)"); return; }
            string sName = db.currentTable ?? "(no table)";
            string sKind = db.currentIsView ? "view" : "table";
            if (!db.hasRecordset()) { Console.WriteLine(sKind + ": " + sName); return; }
            Console.WriteLine(sKind + ": " + sName + ", row " + db.absolutePosition + " of " + db.recordCount);
        }

        private static void cmdSayYieldMarked()
        {
            if (!requireRecordset()) return;
            if (!db.hasField(Metadata.MarkedColumn))
            { Console.WriteLine("(this table has no marked column)"); return; }
            object oOriginal = null;
            try { oOriginal = db.bookmark; } catch { }
            int iCount = 0;
            try
            {
                for (int i = 1; i <= db.recordCount; i++)
                {
                    try
                    {
                        db.absolutePosition = i;
                        string sV = db.getFieldValue(Metadata.MarkedColumn).Trim();
                        if (sV.Equals("true", StringComparison.OrdinalIgnoreCase) || sV == "1") iCount++;
                    }
                    catch { }
                }
            }
            finally
            {
                if (oOriginal != null) try { db.bookmark = oOriginal; } catch { }
            }
            Console.WriteLine(iCount + " marked row" + (iCount == 1 ? "" : "s"));
        }

        // cmdExtractRegex: walks every visible row and prints every
        // match to stdout. The user can redirect the output with
        // Out-File ('output extracted.txt' or 'tee') if desired.
        // Argument is the regex itself.
        private static void cmdExtractRegex(string sPattern)
        {
            if (!requireRecordset()) return;
            sPattern = (sPattern ?? "").Trim();
            if (sPattern.Length == 0)
            { Console.WriteLine("Extract-Regex requires a regex argument."); return; }
            System.Text.RegularExpressions.Regex oRe;
            try
            {
                oRe = new System.Text.RegularExpressions.Regex(sPattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            catch (Exception oEx)
            { Console.WriteLine("Invalid regex: " + oEx.Message); return; }

            List<string> lFields = db.getDisplayFieldNames();
            object oOriginal = null;
            try { oOriginal = db.bookmark; } catch { }
            int iMatches = 0;
            try
            {
                for (int i = 1; i <= db.recordCount; i++)
                {
                    try
                    {
                        db.absolutePosition = i;
                        foreach (string sCol in lFields)
                        {
                            string sV = db.getFieldValue(sCol) ?? "";
                            if (sV.Length == 0) continue;
                            foreach (System.Text.RegularExpressions.Match oM in oRe.Matches(sV))
                            {
                                Console.WriteLine(oM.Value);
                                iMatches++;
                            }
                        }
                    }
                    catch { }
                }
            }
            finally
            {
                if (oOriginal != null) try { db.bookmark = oOriginal; } catch { }
            }
            if (iMatches == 0) Console.WriteLine("(no matches)");
            else Console.WriteLine("(" + iMatches + " match" + (iMatches == 1 ? "" : "es") + ")");
        }

        private static void cmdCopyRow()
        {
            if (!requireRecordset()) return;
            try
            {
                List<string> lFields = db.getDisplayFieldNames();
                StringBuilder oSb = new StringBuilder();
                for (int i = 0; i < lFields.Count; i++)
                {
                    if (i > 0) oSb.Append('\t');
                    string sV = db.getFieldValue(lFields[i]) ?? "";
                    sV = sV.Replace('\t', ' ').Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");
                    oSb.Append(sV);
                }
                Console.WriteLine(oSb.ToString());
                // The GUI form (if any) is what owns the WinForms
                // Clipboard. Try to set it from the CLI too; if no
                // form is loaded (CLI-only mode), Clipboard access
                // throws and we just print the row.
                try { Clipboard.SetText(oSb.ToString()); Console.WriteLine("(copied to clipboard)"); }
                catch { /* no clipboard in pure CLI mode */ }
            }
            catch (Exception oEx) { Console.WriteLine("Copy-Row: " + oEx.Message); }
        }

        private static void cmdStepInitialChange()
        {
            // Pure-CLI version is meaningless without a "current
            // column" concept (the data list has Tab-driven column
            // focus; the CLI dot prompt does not). Print a hint.
            Console.WriteLine("Step-InitialChange is a data-list command (GUI only).");
        }

        // Parse and dispatch one command line.
        // toCanonicalDisplay: format a lowercase hyphenated verb
        // ("step-record-first") into the user-visible canonical name
        // ("Step-Record-First") for the Command Echo marker.
        private static string toCanonicalDisplay(string sVerb)
        {
            if (string.IsNullOrEmpty(sVerb)) return sVerb;
            string[] aParts = sVerb.Split('-');
            for (int i = 0; i < aParts.Length; i++)
            {
                string p = aParts[i];
                if (p.Length == 0) continue;
                aParts[i] = char.ToUpperInvariant(p[0]) + p.Substring(1);
            }
            return string.Join("-", aParts);
        }

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

            // Command Echo is GUI-only. In the dot prompt the user's
            // own typed line and the command's stdout output are
            // already plenty of confirmation; an additional canonical-
            // name marker line just slows down screen-reader review of
            // recent output.

            switch (sVerb)
            {
                case "step-record":       cmdStepRecord(sRest);     break;
                case "step-record-first": cmdStepRecord("first");   break;
                case "step-record-last":  cmdStepRecord("last");    break;
                case "step-record-next":  cmdStepRecord("next " + sRest);     break;
                case "step-record-prev":  cmdStepRecord("previous " + sRest); break;
                case "set-position":     cmdSetPosition(sRest);    break;
                case "show-object":      cmdShowObject(sRest);     break;
                case "show-table":       cmdShowTable(sRest);      break;
                case "show-schema":      cmdShowSchema(sRest);     break;
                case "show-status":      printRowSummary();        break;
                // Speech-only commands. Their GUI handlers push
                // through LiveRegion; the CLI equivalents print to
                // Console since there's no live region in a console.
                case "say-status":       cmdSayStatus();           break;
                case "say-path":         cmdSayPath();             break;
                case "say-yield":        cmdSayYield();            break;
                case "say-tables":       cmdSayTables();           break;
                case "say-marked":       cmdSayMarked();           break;
                case "say-date":         cmdSayDate();             break;
                case "say-type":         cmdSayType();             break;
                case "say-yieldmarked":  cmdSayYieldMarked();      break;
                // Action commands new this turn.
                case "extract-regex":    cmdExtractRegex(sRest);   break;
                case "copy-row":         cmdCopyRow();             break;
                case "step-initialchange": cmdStepInitialChange(); break;
                case "get-field":        cmdGetField(sRest);       break;
                case "set-field":        cmdSetField(sRest);       break;
                case "jump-record":      cmdFindRecord(sRest);     break;
                case "jump-recordagain": cmdFindAgain();           break;
                case "jump-recordprevious": cmdFindPrevious();    break;
                case "find":             cmdFindRecord(sRest);     break; // CLI Find shares the ADO-Find handler
                case "find-previous":    cmdFindPrevious();        break;
                case "search-next":      cmdFindAgain();           break;
                case "search-previous":  cmdFindPrevious();        break;
                case "find-regex":       cmdFindRegex(sRest);      break;
                case "find-regexagain":  cmdFindRegexAgain();      break;
                case "find-regexprevious": cmdFindRegexPrevious(); break;
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
                case "exit-childtoroot": cmdExitChildToRoot();     break;
                case "update-field":     Console.WriteLine("(Update-Field is GUI-only in this build)"); break;
                case "measure-table":    cmdMeasureTable();        break;
                case "new-chart":        cmdNewChart(sRest);       break;
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
                case "show-history":     cmdShowHistory();          break;
                case "get-verb":         cmdGetVerb();             break;
                case "trace-command":    cmdTraceCommand(sRest);   break;
                case "sync-session":     printRowSummary();        break;
                case "out-file":         cmdOutFile(sRest);        break;
                case "invoke-script":    cmdInvokeScript(sRest);   break;
                case "exit-console":     bShouldExit = true;       break;
                case "exit-application": cmdExitApplication();      break;
                case "switch-focus":     cmdSwitchFocus();          break;
                case "about-dbduo":      cmdAboutDbDuo();          break;
                case "elevate-version":  cmdElevateVersion();      break;
                case "show-readme":      cmdShowReadme();          break;
                case "show-log":         cmdShowLog();             break;
                case "open-website":     cmdOpenWebsite();         break;
                case "open-filefolder":  cmdOpenFileFolder();      break;
                case "enter-console":    Console.WriteLine("(already at the dot prompt)"); break;
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

        // About-DbDuo: print the version banner to the CLI. Mirrors
        // the GUI About dialog. Always available.
        private static void cmdAboutDbDuo()
        {
            Console.WriteLine();
            Console.WriteLine("DbDuo " + BuildInfo.VersionString);
            Console.WriteLine("Dual-interface (GUI + CLI) database manager.");
            Console.WriteLine("Designed for keyboard productivity and screen-reader accessibility.");
            Console.WriteLine("Project: https://github.com/JamalMazrui/DbDuo");
            Console.WriteLine();
        }

        // Elevate-Version: check GitHub for a newer DbDuo_setup.exe,
        // download it if found, and run the installer. EdSharp's F11
        // and FileDir's F11 are the model. The download URL is the
        // stable "latest release" GitHub redirect; the version check
        // queries the GitHub Releases API for the tag of the latest
        // release. Inno Setup's installer detects a running DbDuo
        // and offers to close it, so we can run the installer
        // without first stopping the current process.
        private static void cmdElevateVersion()
        {
            ElevateVersion.run();
        }

        private static void cmdShowReadme()
        {
            string sPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
                "README.htm");
            if (!System.IO.File.Exists(sPath)) { Console.WriteLine("README.htm not found at " + sPath); return; }
            try { System.Diagnostics.Process.Start(sPath); Console.WriteLine("Opened README.htm"); }
            catch (Exception oEx) { Console.WriteLine("Could not open README.htm: " + oEx.Message); }
        }

        private static void cmdShowLog()
        {
            string sPath = DbDuoLog.getLogPath();
            Console.WriteLine("Log: " + sPath);
        }

        private static void cmdOpenWebsite()
        {
            try { System.Diagnostics.Process.Start("https://github.com/JamalMazrui/DbDuo"); Console.WriteLine("Opened DbDuo website."); }
            catch (Exception oEx) { Console.WriteLine("Open-WebSite failed: " + oEx.Message); }
        }

        private static void cmdOpenFileFolder()
        {
            if (db == null || !db.isOpen() || string.IsNullOrEmpty(db.filePath))
            { Console.WriteLine("Open-FileFolder: no database is open."); return; }
            try
            {
                string sArg = "/select,\"" + db.filePath + "\"";
                System.Diagnostics.Process.Start("explorer.exe", sArg);
                Console.WriteLine("Opened Explorer at " + db.filePath);
            }
            catch (Exception oEx) { Console.WriteLine("Open-FileFolder failed: " + oEx.Message); }
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

        // Exit-ChildToRoot: pop all the way back to the topmost
        // parent. Calls the form's invokeExitChildToRoot via the
        // marshalled method below.
        private static void cmdExitChildToRoot()
        {
            if (oForm == null || oForm.IsDisposed)
            {
                Console.WriteLine("Exit-ChildToRoot: not available in CLI-only mode.");
                return;
            }
            if (!oForm.hasDrillStack())
            {
                Console.WriteLine("Exit-ChildToRoot: drill stack is empty.");
                return;
            }
            int iPopped = oForm.invokeExitChildToRoot();
            Console.WriteLine("Returned to root (" + iPopped + " level" + (iPopped == 1 ? "" : "s") + ").");
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

        // resolveAlias: translate a typed verb to its canonical
        // PowerShell Verb-Noun name. Three rules govern this table:
        //
        //   (1) Every alias here matches one in dbDot.vbs (the
        //       VBScript predecessor) unless it's a single-letter
        //       or single-word shorthand that fits the broader
        //       DbDuo / PowerShell idiom.
        //   (2) No vowel-dropped abbreviations: "previous" yes,
        //       "prev" no; "delete" yes, "del" no; "display" yes,
        //       "disp" no; "properties" yes, "props" no;
        //       "average" yes, "avg" no.
        //   (3) Legacy dBASE aliases that overlap with a dbDot
        //       alias are dropped (e.g. dBASE's "locate" gives
        //       way to dbDot's "find").
        //
        // The result is a small, predictable set of aliases that
        // a dbDot user can already type and that don't ask the
        // newcomer to memorize cryptic shorthand.
        private static string resolveAlias(string sVerb)
        {
            switch (sVerb)
            {
                // Navigation
                case "n": case "+": case "next":     return "step-record-next";
                case "p": case "-": case "previous": return "step-record-prev";
                case "first":                        return "step-record-first";
                case "last":                         return "step-record-last";
                case "skip":                         return "step-record"; // dbDot SKIP n is retained -- the verb has no clean replacement.
                case "g": case "go": case "goto":    return "set-position";
                // Display
                case "show":                         return "show-object";
                case "l": case "list":               return "show-table";
                case "schema":                       return "show-schema";
                case "status": case "?":             return "show-status";
                // Speech-only family (Query menu). Each speaks a
                // piece of state without changing recordset position.
                // Note: 'yield' is NOT an alias for say-yield -- it
                // remains the long-standing alias for measure-table
                // (the dbDot heritage). Users get the speech-only
                // version by typing 'say-yield' literally; the
                // 'count'/'y'/'yield' chain gives the multi-line
                // verbose count via measure-table.
                case "path":                         return "say-path";
                case "tables-list":                  return "say-tables";
                case "marked-list":                  return "say-marked";
                case "date":                         return "say-date";
                case "type":                         return "say-type";
                case "yield-marked":                 return "say-yieldmarked";
                // Action commands new this turn.
                case "extract":                      return "extract-regex";
                case "copy-row-tsv":                 return "copy-row";
                // Edit
                case "a": case "&": case "add": case "append":
                                                     return "new-record";
                case "e": case "^": case "edit": case "modify":
                                                     return "set-record";
                case "d": case "delete":             return "remove-record";
                case "copy":                         return "copy-record";
                // Search families. Three distinct families with
                // dedicated CLI canonicals:
                //   find  = substring across all columns
                //   jump  = substring scoped to one column (legacy
                //           dbDot SEEK / dBASE FIND idiom)
                //   regex = .NET regex across all columns
                // F3 / Shift+F3 in the GUI map to search-next /
                // search-previous; at the dot prompt those are also
                // typeable verbs that repeat whichever family was
                // last invoked.
                case "f": case "find":               return "find";
                case "find-previous":                return "find-previous";
                case "j": case "jump":               return "jump-record";
                case "seek":                         return "jump-record"; // dbDot SEEK
                case "find-next":                    return "search-next"; // legacy alias
                case "find-again":                   return "search-next";
                case "again":                        return "search-next";
                case "previous-match": case "prev-match":
                                                     return "search-previous";
                case "regex":                        return "find-regex";
                case "regex-previous":               return "find-regexprevious";
                // Filter / sort / column
                case "filter": case "where":         return "select-record";
                case "columns":                      return "select-column";
                case "clear-filter":                 return "reset-filter";
                case "sort": case "order":           return "sort-object";
                // Tables
                case "table":                        return "select-table";
                case "select-view":                  return "select-table";
                case "tables":                       return "get-table";
                case "properties":                   return "get-property";
                // Marks (dbDot mark / unmark)
                case "m": case "mark":               return "set-mark";
                case "u": case "unmark":             return "clear-mark";
                case "related":                      return "show-related";
                // Parent-child drill (DbDuo addition; not in dbDot,
                // but all single English words)
                case "zoom": case "drill": case "enter":
                                                     return "enter-child";
                case "unzoom": case "back":          return "exit-child";
                case "root": case "exit-to-root":    return "exit-childtoroot";
                // SQL execution: dbDot uses "exec" and ";"
                case "exec": case ";": case "sql":   return "invoke-sql";
                // Measurements: dbDot has count, yield, y, max, min,
                // longest, shortest; sum and average go to Measure-Field.
                case "count": case "y": case "yield":
                                                     return "measure-table";
                case "longest":                      return "measure-longest";
                case "shortest":                     return "measure-shortest";
                case "max": case "maximum":          return "measure-maximum";
                case "min": case "minimum":          return "measure-minimum";
                case "sum":                          return "measure-field"; // dbDot SUM
                case "average":                      return "measure-field"; // dbDot AVERAGE
                case "fields":                       return "get-fieldname";
                // File / driver
                case "test":                         return "test-database";
                case "drivers":                      return "test-driver";
                case "requery": case "refresh":      return "update-view"; // dbDot REQUERY
                case "backup":                       return "backup-database";
                case "save-as":                      return "save-databaseas";
                case "export": case "ex":            return "export-data"; // dbDot ex
                case "open":                         return "open-database";
                case "close":                        return "close-database";
                // Bookmarks
                case "bookmark":                     return "save-bookmark";
                // Help / verbs / trace
                case "help":                         return "get-help";
                case "history":                      return "show-history";
                case "verbs":                        return "get-verb";
                case "trace":                        return "trace-command";
                case "sync":                         return "sync-session";
                // Output redirection (SQLite .output / PowerShell Out-File)
                case "output": case "tee": case "o":
                                                     return "out-file";
                // Run commands from a file (SQLite .read / psql \i)
                case "read": case "script": case "i":
                                                     return "invoke-script";
                case "x": case "exit": case "bye":   return "exit-console";
                case "q": case "quit":               return "exit-application";
                case "gui": case "focus": case "window":
                                                     return "switch-focus";
                // Additional aliases to give every command both a
                // PowerShell name (the canonical Verb-Noun, always
                // reachable by typing the hyphenated form) and a
                // shorter DbDuo / dbDot / English-natural alternate.
                // Per Jamal's guideline, aliases include first letter,
                // first word, or first two words of a longer command
                // where the abbreviation is unambiguous.
                case "lock":                         return "lock-database";
                case "new":                          return "new-database";
                case "save":                         return "save-databaseas";
                case "import": case "in":            return "import-data";
                case "compare": case "diff":         return "compare-database";
                case "print":                        return "out-printer";
                case "chart":                        return "new-chart";
                case "config": case "settings":      return "edit-configuration";
                case "console":                      return "enter-console";
                case "web": case "website":          return "open-website";
                case "folder": case "explorer":      return "open-filefolder";
                case "log":                          return "show-log";
                case "reader":                       return "test-reader";
                case "commands": case "command-picker":
                                                     return "show-command";
                case "about":                        return "about-dbduo";
                case "readme":                       return "show-readme";
                case "cell":                         return "open-cell";
                case "update": case "replace":       return "update-field";
                case "initial-change": case "initial":
                                                     return "step-initialchange";
                case "switch": case "next-table":    return "switch-table";
                case "prev-table": case "previous-table":
                                                     return "switch-tableprevious";
                case "next-object":                  return "switch-object";
                case "prev-object": case "previous-object":
                                                     return "switch-objectprevious";
                case "elevate": case "update-app":   return "elevate-version";
                case "restore":                      return "restore-bookmark";
                case "clear-mark-all": case "clear-all":
                                                     return "clear-mark-all";
                case "saved-bookmark": case "go-bookmark":
                                                     return "restore-bookmark";
            }
            // Note: step-record-first / step-record-last / step-record-next /
            // step-record-prev are deliberately NOT collapsed here. An
            // earlier version of resolveAlias mapped each of those to
            // bare "step-record", which dropped the direction argument
            // and caused "first" to silently mean "next" -- the bug
            // Jamal reported in May 2026. The dispatcher now switches
            // on the full compound verb and passes the direction
            // explicitly to cmdStepRecord.
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
                    db.moveFirst();
                    break;
                case "last":
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

        // Set-Position: jump to a numbered row, accepting either an
        // integer ordinal (1-based) or a percent like "50%" of the
        // current record count. Matches the GUI's Set-Position which
        // accepts both forms in its prompt. Percent is rounded to the
        // nearest row, clamped to [1, recordCount].
        private static void cmdSetPosition(string sArg)
        {
            if (!requireRecordset()) return;
            string sTrim = sArg.Trim();
            int iCount = db.recordCount;
            if (iCount <= 0) { Console.WriteLine("(no records)"); return; }
            int iTarget;
            if (sTrim.EndsWith("%"))
            {
                double dPct;
                if (!double.TryParse(sTrim.TrimEnd('%').Trim(),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out dPct)
                    || dPct < 0 || dPct > 100)
                { Console.WriteLine("Set-Position: percent must be 0-100, like 50%."); return; }
                iTarget = (int)Math.Round(iCount * dPct / 100.0);
                if (iTarget < 1) iTarget = 1;
            }
            else if (!int.TryParse(sTrim, out iTarget))
            { Console.WriteLine("Set-Position: requires a row number or percent."); return; }
            if (iTarget < 1) iTarget = 1;
            if (iTarget > iCount) iTarget = iCount;
            db.absolutePosition = iTarget;
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
            sLastFindKindCli = "plain";
            try
            {
                bool bFound = db.findRecord(sCriteria, true, false);
                if (!bFound) Console.WriteLine("Not found.");
                refresh();
                printRowSummary();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        // jump-recordagain (F3 / "n"): repeat whichever Find variant
        // was last invoked, plain or regex. EdSharp convention.
        private static void cmdFindAgain()
        {
            if (!requireRecordset()) return;
            if (sLastFindKindCli == "regex")
            {
                cmdFindRegexAgain();
                return;
            }
            if (string.IsNullOrEmpty(sLastFindCriteria)) { Console.WriteLine("No previous Find."); return; }
            try
            {
                bool bFound = db.findRecord(sLastFindCriteria, true, true);
                if (!bFound) Console.WriteLine("No more matches.");
                refresh();
                printRowSummary();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        // jump-recordprevious (Shift+F3): repeat the last Find backward,
        // dispatching on plain vs regex like cmdFindAgain.
        private static void cmdFindPrevious()
        {
            if (!requireRecordset()) return;
            if (sLastFindKindCli == "regex")
            {
                cmdFindRegexPrevious();
                return;
            }
            if (string.IsNullOrEmpty(sLastFindCriteria)) { Console.WriteLine("No previous Find."); return; }
            try
            {
                bool bFound = db.findRecord(sLastFindCriteria, false, true);
                if (!bFound) Console.WriteLine("No earlier matches.");
                refresh();
                printRowSummary();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        // Find-Regex: find a row matching a .NET regex.
        //
        // Argument grammar:
        //   Find-Regex <column> <pattern>
        //   Find-Regex * <pattern>            (scan all visible fields)
        //   Find-Regex <pattern>              (uses last column or "*")
        //
        // The first space-separated token is interpreted as a column
        // name if it matches one of the current table's field names
        // (case-insensitive) or is "*"; otherwise the whole argument
        // is treated as the pattern and the column defaults to the
        // last-used column (initially "*"). Pattern compilation
        // errors are surfaced verbatim.
        //
        // Find-RegexAgain and Find-RegexPrevious repeat the last
        // regex search forward / backward from the current row.
        private static void cmdFindRegex(string sArg)
        {
            if (!requireRecordset()) return;
            string sTrim = sArg.Trim();
            if (sTrim.Length == 0)
            { Console.WriteLine("Find-Regex requires a pattern. Try: find-regex <column> <pattern>"); return; }

            string sCol;
            string sPattern;
            int iSpace = sTrim.IndexOf(' ');
            if (iSpace > 0)
            {
                string sFirst = sTrim.Substring(0, iSpace);
                string sRest = sTrim.Substring(iSpace + 1).Trim();
                bool bIsCol = (sFirst == "*");
                if (!bIsCol)
                {
                    foreach (string sF in db.getFieldNames())
                        if (string.Equals(sF, sFirst, StringComparison.OrdinalIgnoreCase))
                        { bIsCol = true; break; }
                }
                if (bIsCol && sRest.Length > 0)
                { sCol = sFirst; sPattern = sRest; }
                else
                { sCol = string.IsNullOrEmpty(sLastFindRegexColumnCli) ? "*" : sLastFindRegexColumnCli;
                  sPattern = sTrim; }
            }
            else
            { sCol = string.IsNullOrEmpty(sLastFindRegexColumnCli) ? "*" : sLastFindRegexColumnCli;
              sPattern = sTrim; }

            sLastFindRegexCli = sPattern;
            sLastFindRegexColumnCli = sCol;
            sLastFindKindCli = "regex";
            try
            {
                bool bFound = db.findRegexRecord(sCol, sPattern, true, false);
                if (!bFound) Console.WriteLine("Not found.");
                refresh();
                printRowSummary();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        private static void cmdFindRegexAgain()
        {
            if (!requireRecordset()) return;
            if (string.IsNullOrEmpty(sLastFindRegexCli))
            { Console.WriteLine("No previous Find-Regex."); return; }
            try
            {
                bool bFound = db.findRegexRecord(
                    sLastFindRegexColumnCli, sLastFindRegexCli, true, true);
                if (!bFound) Console.WriteLine("No more matches.");
                refresh();
                printRowSummary();
            }
            catch (Exception oEx) { Console.WriteLine("Error: " + oEx.Message); }
        }

        private static void cmdFindRegexPrevious()
        {
            if (!requireRecordset()) return;
            if (string.IsNullOrEmpty(sLastFindRegexCli))
            { Console.WriteLine("No previous Find-Regex."); return; }
            try
            {
                bool bFound = db.findRegexRecord(
                    sLastFindRegexColumnCli, sLastFindRegexCli, false, true);
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

        // New-Chart <column>: produce a frequency bar chart of how
        // often each distinct value appears in one column of the
        // current filtered view, as an .xlsx file. Requires Excel.
        //
        // Argument is required. Empty argument prints a hint with
        // the visible column names so the user can copy one. The
        // result is saved next to the open database with a name
        // like '<table>-by-<column>-chart.xlsx' and opened in Excel
        // automatically via shell associations.
        //
        // Examples on sample.db:
        //   New-Chart grade
        //   New-Chart year
        //   New-Chart department
        private static void cmdNewChart(string sArg)
        {
            if (!requireRecordset()) return;
            string sCol = sArg.Trim();
            if (sCol.Length == 0)
            {
                Console.WriteLine("New-Chart requires a column name. Available visible columns:");
                foreach (string sC in db.getDisplayFieldNames())
                    Console.WriteLine("  " + sC);
                return;
            }
            if (!db.hasField(sCol))
            { Console.WriteLine("New-Chart: column not found: " + sCol); return; }
            if (db.recordCount == 0)
            { Console.WriteLine("New-Chart: no records to chart in the current filter."); return; }

            string sDbPath = db.filePath ?? "";
            string sFolder = string.IsNullOrEmpty(sDbPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : System.IO.Path.GetDirectoryName(sDbPath);
            string sTable = db.currentTable ?? "table";
            string sCleanCol = sCol.Replace(" ", "_").Replace("/", "_");
            string sDest = System.IO.Path.Combine(sFolder,
                sTable + "-by-" + sCleanCol + "-chart.xlsx");

            try
            {
                int iDistinct = db.exportFrequencyChart(sCol, sDest);
                Console.WriteLine("Chart saved with " + iDistinct + " distinct value"
                    + (iDistinct == 1 ? "" : "s") + ": " + sDest);
                try { System.Diagnostics.Process.Start(sDest); } catch { }
            }
            catch (Exception oEx)
            {
                Console.WriteLine("New-Chart failed: " + oEx.Message);
                Console.WriteLine("Note: New-Chart requires Excel to be installed.");
            }
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
                         + "In the GUI, this command is labeled \"Show Record\" and is\n"
                         + "bound to Enter; it opens a read-only dialog. Binary fields\n"
                         + "show length only.";
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
                         + "Show-Object, Get-Property, Set-Record, and the dot\n"
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

        // Show-History: open History.htm next to the EXE in the
        // default browser. Mirrors EdSharp / FileDir's Shift+F1
        // convention; the GUI side wires the chord directly to
        // helpHistoryClicked, while the CLI invokes the verb here.
        private static void cmdShowHistory()
        {
            string sPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
                "History.htm");
            if (!System.IO.File.Exists(sPath))
            {
                Console.WriteLine("History.htm not found at " + sPath);
                return;
            }
            try
            {
                System.Diagnostics.Process.Start(sPath);
                Console.WriteLine("Opened History.htm in your default browser.");
            }
            catch (Exception oEx)
            {
                Console.WriteLine("Could not open History.htm: " + oEx.Message);
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
    // =====================================================================
    // ElevateVersion: implements the Help > Elevate-Version command (F11).
    // Mirrors EdSharp's and FileDir's "check for newer installer, download
    // it, run it" workflow. The check hits the GitHub Releases API for
    // the latest tag, compares it to the locally compiled BuildInfo.VersionString,
    // and only proceeds if a newer version is available. The download
    // target is the stable GitHub redirect that always points at the
    // latest release's DbDuo_setup.exe asset. Inno Setup's installer
    // detects the running DbDuo process and offers to close it, so
    // the user doesn't have to exit DbDuo manually first.
    //
    // Why a hand-rolled JSON parse instead of Newtonsoft / System.Text.Json:
    // DbDuo is a single-file .NET 4.8 project with no NuGet dependencies.
    // The version field is a single short string in a well-known shape;
    // a regex picks it out cleanly without dragging in 300 KB of library.
    // =====================================================================
    public static class ElevateVersion
    {
        // Public so the dot-prompt cmdElevateVersion() can call the
        // same entry point as the menu's helpElevateClicked. Both
        // paths show progress through MessageBox dialogs because the
        // operation is rare and high-stakes (downloading and running
        // a new installer). The dialogs use a parent window when
        // possible, but fall back to message-box owner = null in
        // CLI-only mode.
        public static void run()
        {
            Form oParent = null;
            try { oParent = Form.ActiveForm; } catch { }

            // Step 1: query GitHub for the latest release's tag.
            string sLatestTag;
            try
            {
                // .NET 4.8 defaults to a mix of SSL3/TLS1.0 on some
                // older boxes; GitHub requires TLS 1.2 or newer. Set
                // explicitly, including TLS 1.3 in case it's available.
                System.Net.ServicePointManager.SecurityProtocol |=
                    (System.Net.SecurityProtocolType)3072   // Tls12
                    | (System.Net.SecurityProtocolType)12288; // Tls13 (if present)
                sLatestTag = fetchLatestTag();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(oParent,
                    "Could not check for updates.\n\n" + oEx.Message
                    + "\n\nCheck your internet connection and try again. You can also "
                    + "download the latest DbDuo_setup.exe directly from\n"
                    + "https://github.com/JamalMazrui/DbDuo/releases/latest",
                    "Elevate-Version", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(sLatestTag))
            {
                MessageBox.Show(oParent,
                    "Could not determine the latest DbDuo version.\n\n"
                    + "The GitHub API returned a response but no recognizable\n"
                    + "tag_name field. Try again later, or download directly from\n"
                    + "https://github.com/JamalMazrui/DbDuo/releases/latest",
                    "Elevate-Version", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Step 2: compare versions. Tags are like "v1.0.28" or
            // "1.0.28"; strip any leading "v" before parsing.
            string sLocal = BuildInfo.VersionString;
            string sLatest = sLatestTag.TrimStart('v', 'V').Trim();
            int iCmp = compareVersions(sLatest, sLocal);
            if (iCmp == 0)
            {
                MessageBox.Show(oParent,
                    "DbDuo is up to date.\n\n"
                    + "Installed version: " + sLocal + "\n"
                    + "Latest release:    " + sLatest,
                    "Elevate-Version", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (iCmp < 0)
            {
                // Local version is NEWER than the public release --
                // happens to developers running build snapshots.
                // Inform but don't offer to "upgrade" to an older
                // version.
                MessageBox.Show(oParent,
                    "DbDuo is running a newer version than the latest public release.\n\n"
                    + "Installed: " + sLocal + "\n"
                    + "Public:    " + sLatest + "\n\n"
                    + "No upgrade offered.",
                    "Elevate-Version", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Step 3: confirm with the user before downloading.
            DialogResult oAnswer = MessageBox.Show(oParent,
                "A newer DbDuo is available.\n\n"
                + "Installed: " + sLocal + "\n"
                + "Available: " + sLatest + "\n\n"
                + "Download and run the new installer now?\n\n"
                + "The installer will offer to close this DbDuo before it proceeds.",
                "Elevate-Version", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (oAnswer != DialogResult.Yes) return;

            // Step 4: download the installer to TEMP. The URL is the
            // stable GitHub "latest release asset" redirect, which
            // does not change as new versions ship.
            string sTempPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "DbDuo_setup_" + sLatest + ".exe");
            try
            {
                downloadInstaller(sTempPath);
            }
            catch (Exception oEx)
            {
                MessageBox.Show(oParent,
                    "Download failed.\n\n" + oEx.Message
                    + "\n\nTry again, or download directly from\n"
                    + "https://github.com/JamalMazrui/DbDuo/releases/latest",
                    "Elevate-Version", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Step 5: run the installer and let Inno Setup take over.
            try
            {
                System.Diagnostics.ProcessStartInfo oPsi = new System.Diagnostics.ProcessStartInfo();
                oPsi.FileName = sTempPath;
                oPsi.UseShellExecute = true; // allows UAC elevation if the installer manifest requests it
                System.Diagnostics.Process.Start(oPsi);
                // We do NOT exit DbDuo here. Inno Setup, when launched,
                // will detect the running DbDuo.exe and offer to close
                // it via its standard "AppMutex" prompt. Letting the
                // installer drive that flow means the user sees the
                // confirmation dialog they expect and can cancel out
                // if they change their mind.
            }
            catch (Exception oEx)
            {
                MessageBox.Show(oParent,
                    "The installer downloaded but could not be launched.\n\n"
                    + oEx.Message
                    + "\n\nThe file is at:\n" + sTempPath
                    + "\n\nYou can run it manually.",
                    "Elevate-Version", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // fetchLatestTag: determine the version tag of the latest
        // public DbDuo release on GitHub. Two paths:
        //
        //   (1) The public REST API at
        //       https://api.github.com/repos/JamalMazrui/DbDuo/releases/latest
        //       This requires no credentials. It is the documented
        //       way to read public release metadata. GitHub's rate
        //       limit for unauthenticated requests is 60 per hour
        //       per IP -- ample headroom for an Elevate-Version
        //       command a user might invoke once a week, but the
        //       limit could trip on shared / corporate / VPN IPs.
        //
        //   (2) Scrape the public release page HTML at
        //       https://github.com/JamalMazrui/DbDuo/releases/latest
        //       which GitHub redirects to the actual versioned page
        //       like /releases/tag/v1.0.29. The redirect target's
        //       last URL segment IS the tag. This path also requires
        //       no credentials and is not subject to the REST API
        //       rate limit -- though it does count against the
        //       general anonymous-request limits announced in May
        //       2025. Either limit is plenty for this use case.
        //
        // Path 1 is tried first; on failure (network error, 403
        // rate-limit, malformed JSON) the code falls through to
        // path 2 silently. Both paths use a non-empty User-Agent
        // header, which GitHub requires.
        private static string fetchLatestTag()
        {
            try
            {
                string sTag = fetchLatestTagViaApi();
                if (!string.IsNullOrEmpty(sTag)) return sTag;
            }
            catch { /* fall through to scrape */ }
            return fetchLatestTagViaScrape();
        }

        // fetchLatestTagViaApi: the REST-API path. Public endpoint,
        // no credentials, returns JSON.
        private static string fetchLatestTagViaApi()
        {
            const string sApiUrl = "https://api.github.com/repos/JamalMazrui/DbDuo/releases/latest";
            string sBody;
            using (System.Net.WebClient oWc = new System.Net.WebClient())
            {
                oWc.Headers.Add("User-Agent", "DbDuo-ElevateVersion/" + BuildInfo.VersionString);
                oWc.Headers.Add("Accept", "application/vnd.github+json");
                sBody = oWc.DownloadString(sApiUrl);
            }
            // The JSON payload has many fields. The tag is the first
            // "tag_name":"..." occurrence. A bounded regex is safe
            // here because the field name is unique in this response
            // shape and the value contains no escape sequences worth
            // worrying about (version tags are ASCII).
            System.Text.RegularExpressions.Match oM = System.Text.RegularExpressions.Regex.Match(
                sBody ?? "",
                "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
            return oM.Success ? oM.Groups[1].Value : "";
        }

        // fetchLatestTagViaScrape: the HTML-scrape fallback. The URL
        // https://github.com/JamalMazrui/DbDuo/releases/latest
        // returns a redirect (HTTP 302) whose Location header points
        // at the versioned release page such as
        // https://github.com/JamalMazrui/DbDuo/releases/tag/v1.0.29.
        // We capture the redirect via HttpWebRequest with
        // AllowAutoRedirect=false and read the final path segment.
        //
        // The implementation is robust against three cases: the
        // server may issue a 302 (older GitHub), a 301 (newer
        // GitHub), or follow the redirect transparently. If we get
        // a redirect status, the new URL is in the Location header
        // and we don't bother fetching the body. If we don't get a
        // redirect, we read the canonical URL from the final
        // response URI (WebClient or HttpWebResponse.ResponseUri).
        private static string fetchLatestTagViaScrape()
        {
            const string sUrl = "https://github.com/JamalMazrui/DbDuo/releases/latest";
            string sFinalUrl = "";
            try
            {
                System.Net.HttpWebRequest oReq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(sUrl);
                oReq.UserAgent = "DbDuo-ElevateVersion/" + BuildInfo.VersionString;
                oReq.AllowAutoRedirect = false;
                oReq.Method = "GET";
                oReq.Timeout = 15000;
                System.Net.HttpWebResponse oResp = null;
                try
                {
                    oResp = (System.Net.HttpWebResponse)oReq.GetResponse();
                }
                catch (System.Net.WebException oWebEx)
                {
                    // Some HttpWebRequest implementations throw on 3xx
                    // when AllowAutoRedirect is false; the response is
                    // still on the exception.
                    oResp = oWebEx.Response as System.Net.HttpWebResponse;
                    if (oResp == null) throw;
                }
                using (oResp)
                {
                    int iStatus = (int)oResp.StatusCode;
                    if (iStatus >= 300 && iStatus < 400)
                    {
                        sFinalUrl = oResp.Headers["Location"] ?? "";
                    }
                    else
                    {
                        // No redirect; the request may have been
                        // followed transparently, in which case
                        // ResponseUri is the resolved URL.
                        sFinalUrl = oResp.ResponseUri != null ? oResp.ResponseUri.AbsoluteUri : "";
                    }
                }
            }
            catch
            {
                // Last resort: ask WebClient to do the GET with
                // auto-redirect on and read its ResponseUri.
                try
                {
                    using (System.Net.WebClient oWc = new System.Net.WebClient())
                    {
                        oWc.Headers.Add("User-Agent", "DbDuo-ElevateVersion/" + BuildInfo.VersionString);
                        oWc.DownloadString(sUrl);
                        // WebClient doesn't expose ResponseUri directly,
                        // but for our use we accept the scrape failed
                        // silently if the HttpWebRequest path didn't work.
                    }
                }
                catch { /* both paths failed */ }
            }
            if (string.IsNullOrEmpty(sFinalUrl)) return "";
            // Extract the last path segment from a URL like
            // https://github.com/JamalMazrui/DbDuo/releases/tag/v1.0.29
            try
            {
                Uri oUri = new Uri(sFinalUrl);
                string sPath = oUri.AbsolutePath;
                int iSlash = sPath.LastIndexOf('/');
                if (iSlash < 0 || iSlash == sPath.Length - 1) return "";
                string sTag = sPath.Substring(iSlash + 1);
                // Sanity check: must not still be "latest" (would mean
                // GitHub didn't redirect, perhaps because no release
                // exists yet for this repo).
                if (sTag.Equals("latest", StringComparison.OrdinalIgnoreCase)) return "";
                return sTag;
            }
            catch { return ""; }
        }


        // downloadInstaller: download the stable "latest installer"
        // asset to the given local path. The URL is GitHub's special
        // /releases/latest/download/<filename> redirect that always
        // resolves to the most recent release's asset of that name.
        private static void downloadInstaller(string sLocalPath)
        {
            const string sDownloadUrl = "https://github.com/JamalMazrui/DbDuo/releases/latest/download/DbDuo_setup.exe";
            using (System.Net.WebClient oWc = new System.Net.WebClient())
            {
                oWc.Headers.Add("User-Agent", "DbDuo-ElevateVersion/" + BuildInfo.VersionString);
                oWc.DownloadFile(sDownloadUrl, sLocalPath);
            }
        }

        // compareVersions: dotted-numeric comparison, e.g.
        // "1.0.28" vs "1.0.29". Returns positive if sA > sB, zero
        // if equal, negative if sA < sB. Non-numeric segments fall
        // back to ordinal-string compare. Missing segments are
        // treated as zero, so "1.0" == "1.0.0".
        private static int compareVersions(string sA, string sB)
        {
            string[] aA = (sA ?? "").Split('.');
            string[] aB = (sB ?? "").Split('.');
            int iLen = Math.Max(aA.Length, aB.Length);
            for (int i = 0; i < iLen; i++)
            {
                string sNa = i < aA.Length ? aA[i] : "0";
                string sNb = i < aB.Length ? aB[i] : "0";
                int iA, iB;
                bool bIsNumA = int.TryParse(sNa, out iA);
                bool bIsNumB = int.TryParse(sNb, out iB);
                if (bIsNumA && bIsNumB)
                {
                    if (iA != iB) return iA - iB;
                }
                else
                {
                    int iCmp = string.Compare(sNa, sNb, StringComparison.OrdinalIgnoreCase);
                    if (iCmp != 0) return iCmp;
                }
            }
            return 0;
        }
    }


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

                // Early-exit install flags. These run their action and
                // exit without launching the UI. Used by the installer's
                // post-install task list and also by the user manually
                // re-running the install after a JAWS or NVDA upgrade.
                foreach (string sArg in aArgs)
                {
                    if (sArg.Equals("--install-jaws-settings", StringComparison.OrdinalIgnoreCase)
                     || sArg.Equals("-install-jaws-settings", StringComparison.OrdinalIgnoreCase)
                     || sArg.Equals("/install-jaws-settings", StringComparison.OrdinalIgnoreCase))
                    {
                        string sAppFolder = System.IO.Path.GetDirectoryName(
                            System.Reflection.Assembly.GetExecutingAssembly().Location);
                        int iCopied, iCompiled;
                        string sReport = JawsSettingsInstaller.install(sAppFolder, out iCopied, out iCompiled);
                        Console.WriteLine(sReport);
                        Console.WriteLine("Copied " + iCopied + " files, compiled " + iCompiled + " jsb.");
                        return 0;
                    }
                    if (sArg.Equals("--install-nvda-addon", StringComparison.OrdinalIgnoreCase)
                     || sArg.Equals("-install-nvda-addon", StringComparison.OrdinalIgnoreCase)
                     || sArg.Equals("/install-nvda-addon", StringComparison.OrdinalIgnoreCase))
                    {
                        // Locate the .nvda-addon file shipped next to DbDuo.exe
                        // and open it via its file association. NVDA registers
                        // itself as the handler for .nvda-addon during NVDA's own
                        // install, so Windows hands the file to NVDA which then
                        // shows its standard "Install this add-on?" dialog. The
                        // user confirms (or cancels) in NVDA's UI. This is the
                        // same experience as double-clicking the file in
                        // Explorer.
                        //
                        // Why not nvda.exe --install-addon: that CLI flag is not
                        // documented as a stable entry point. The file-
                        // association path is the documented user-facing way to
                        // install an add-on and works on every NVDA install.
                        //
                        // Failure modes handled:
                        //   - .nvda-addon missing next to DbDuo.exe (unusual --
                        //     the installer always ships it). Print a message
                        //     and exit non-zero.
                        //   - .nvda-addon present but no file association
                        //     registered (NVDA not installed). Print a hint and
                        //     exit non-zero.
                        string sAppFolder = System.IO.Path.GetDirectoryName(
                            System.Reflection.Assembly.GetExecutingAssembly().Location);
                        string sAddonPath = System.IO.Path.Combine(sAppFolder, "DbDuo.nvda-addon");
                        if (!System.IO.File.Exists(sAddonPath))
                        {
                            Console.Error.WriteLine("DbDuo.nvda-addon not found at " + sAddonPath);
                            Console.Error.WriteLine("This file is normally placed by the DbDuo installer.");
                            return 1;
                        }
                        try
                        {
                            System.Diagnostics.ProcessStartInfo oPsi =
                                new System.Diagnostics.ProcessStartInfo(sAddonPath);
                            oPsi.UseShellExecute = true;
                            System.Diagnostics.Process.Start(oPsi);
                            Console.WriteLine("Opened " + sAddonPath + " via its file association.");
                            Console.WriteLine("If NVDA is installed, its add-on install dialog should appear.");
                            return 0;
                        }
                        catch (System.ComponentModel.Win32Exception ex)
                        {
                            Console.Error.WriteLine("Could not open the add-on file: " + ex.Message);
                            Console.Error.WriteLine("This usually means NVDA is not installed. Install NVDA");
                            Console.Error.WriteLine("from https://www.nvaccess.org/ and re-run this command,");
                            Console.Error.WriteLine("or open " + sAddonPath + " from Explorer.");
                            return 1;
                        }
                    }
                    if (sArg.Equals("--uninstall-jaws-settings", StringComparison.OrdinalIgnoreCase)
                     || sArg.Equals("-uninstall-jaws-settings", StringComparison.OrdinalIgnoreCase)
                     || sArg.Equals("/uninstall-jaws-settings", StringComparison.OrdinalIgnoreCase))
                    {
                        int iDeleted;
                        string sReport = JawsSettingsInstaller.uninstall(out iDeleted);
                        Console.WriteLine(sReport);
                        Console.WriteLine("Removed " + iDeleted + " files.");
                        return 0;
                    }
                }

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
