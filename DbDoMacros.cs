// DbDoMacros.cs: DbDo's macro recorder (15 November 2026), on the kit's engine in exec\CSharp\Macro.cs (kit 1.231.0).
// Part of DbDoForm, so each window records and plays its own macro.
//
// WHAT IS RECORDED. First Record, Last Record, Next Record, Previous Record, and Go to Record with a row number. Each
// handler says it succeeded by calling confirmMacroCommand after its move and refresh; a handler that returns without
// saying so, a Go To that was cancelled or used a percent or relative amount, any other command, and a key in the
// grid that no command claims all make the recording incomplete. Say commands and the Key Describer only listen, so
// they are left out of the recording and do not spoil it.
//
// WHAT IS CHECKED. Before each step plays, the host says where it should leave the cursor -- "row 9 of 12" -- and after
// it, where the cursor is. The engine logs both and stops at a difference. scripts\auditMacros.py checks the code
// itself: every supported command has its menu item, its confirmation, and its validate, expect and execute cases.
using Homer;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DbDo
{
    public partial class DbDoForm
    {
        private bool bMacroCancelled, bMacroConfirmed, bMacroGapSaid;
        private int iMacroDepth;
        private MacroSession macroSession;
        private string sMacroArgument = "", sMacroCommandId = "";

        // The commands a macro can hold, by KeyMap.toId of their menu names. auditMacros.py reads this line.
        private static readonly string[] aMacroCommands = new string[] { "firstRecord", "goToRecord", "lastRecord", "nextRecord", "previousRecord" };

        private sealed class DbDoMacroHost : IMacroHost
        {
            private readonly DbDoForm dbDoForm;

            public DbDoMacroHost(DbDoForm formOwner)
            {
                dbDoForm = formOwner;
            }

            public string sApp { get { return "DbDo"; } }

            // The view a This View macro needs: the database's full path, the table, the filter and the order. Empty
            // when there are no records, so nothing is recorded or played there.
            public string sScope
            {
                get
                {
                    if (dbDoForm.db == null || !dbDoForm.db.hasRecordset() || dbDoForm.db.recordCount < 1) return "";
                    return Path.GetFullPath(dbDoForm.db.filePath).ToUpperInvariant() + "\n" + (dbDoForm.db.currentTable ?? "") + "\n" + (dbDoForm.db.filterState ?? "") + "\n" + (dbDoForm.db.sort ?? "");
                }
            }

            public bool log(string sMessage)
            {
                DbDoLog.write("macro: " + sMessage);
                return true;
            }

            public string observe()
            {
                if (dbDoForm.db == null || !dbDoForm.db.hasRecordset()) return "no records";
                return "row " + dbDoForm.db.absolutePosition + " of " + dbDoForm.db.recordCount;
            }

            // Where the step should leave the cursor, from where it is now; the same words observe uses.
            public string expect(MacroStep macroStep)
            {
                int iCount, iRow, iTarget;
                if (dbDoForm.db == null || !dbDoForm.db.hasRecordset()) return "";
                iCount = dbDoForm.db.recordCount;
                iRow = dbDoForm.db.absolutePosition;
                switch (macroStep.sCommandId)
                {
                    case "firstRecord": iTarget = 1; break;
                    case "lastRecord": iTarget = iCount; break;
                    case "nextRecord": iTarget = Math.Min(iRow + 1, iCount); break;
                    case "previousRecord": iTarget = Math.Max(iRow - 1, 1); break;
                    case "goToRecord": if (!int.TryParse(macroStep.sArgument, NumberStyles.None, CultureInfo.InvariantCulture, out iTarget)) return ""; break;
                    default: return "";
                }
                return "row " + iTarget + " of " + iCount;
            }

            public bool validate(MacroStep macroStep)
            {
                int iRow;
                if (dbDoForm.IsDisposed || dbDoForm.Disposing || string.IsNullOrEmpty(sScope)) return false;
                if (dbDoForm.MdiParent != null && dbDoForm.MdiParent.ActiveMdiChild != dbDoForm) return false;
                switch (macroStep.sCommandId)
                {
                    case "firstRecord": case "lastRecord": case "nextRecord": case "previousRecord":
                        return macroStep.sArgument == "";
                    case "goToRecord":
                        return int.TryParse(macroStep.sArgument, NumberStyles.None, CultureInfo.InvariantCulture, out iRow) && iRow > 0 && iRow <= dbDoForm.db.recordCount;
                    default: return false;
                }
            }

            // The same moves the menu commands make, without their menus, so playback is never recorded again.
            public bool execute(MacroStep macroStep)
            {
                if (!validate(macroStep)) return false;
                switch (macroStep.sCommandId)
                {
                    case "firstRecord": dbDoForm.db.moveFirst(); break;
                    case "lastRecord": dbDoForm.db.moveLast(); break;
                    case "nextRecord": if (dbDoForm.db.absolutePosition < dbDoForm.db.recordCount) dbDoForm.db.absolutePosition = dbDoForm.db.absolutePosition + 1; break;
                    case "previousRecord": if (dbDoForm.db.absolutePosition > 1) dbDoForm.db.absolutePosition = dbDoForm.db.absolutePosition - 1; break;
                    case "goToRecord":
                        dbDoForm.virtMoveTo(int.Parse(macroStep.sArgument, NumberStyles.None, CultureInfo.InvariantCulture), dbDoForm.iVirtualCol);
                        return true;
                    default: return false;
                }
                dbDoForm.invokeRefresh();
                return true;
            }
        }

        private bool initializeMacros()
        {
            if (macroSession != null) return true;
            macroSession = new MacroSession(new DbDoMacroHost(this));
            // A key in the grid that no command claimed moves the cursor in a way a macro cannot repeat: Up, Down,
            // Page Up, Page Down, Home, End, or a letter that jumps to a row. Left and Right change the column, not the
            // record, so they are left alone; a key DbDo's own handler took is left alone too.
            grid.KeyDown += delegate(object oSender, KeyEventArgs evArgs)
            {
                Keys keyCode = evArgs.KeyCode;
                if (macroSession == null || !macroSession.bRecording || evArgs.Handled || evArgs.Control || evArgs.Alt) return;
                if (keyCode == Keys.Up || keyCode == Keys.Down || keyCode == Keys.PageUp || keyCode == Keys.PageDown || keyCode == Keys.Home || keyCode == Keys.End ||
                    (keyCode >= Keys.A && keyCode <= Keys.Z) || (keyCode >= Keys.D0 && keyCode <= Keys.D9) || keyCode == Keys.Space || keyCode == Keys.Delete)
                    markMacroGap("the key " + keyCode + " in the grid, which no command claims");
            };
            DbDoLog.write("macro: ready; commands " + string.Join(", ", aMacroCommands));
            return true;
        }

        private static bool isMacroControl(string sCommand)
        {
            return sCommand == "Record Macro" || sCommand == "Play Macro" || sCommand == "Save Macro";
        }

        private static bool macroObservational(string sCommand)
        {
            return sCommand.StartsWith("Say ", StringComparison.Ordinal) || sCommand == "Key Describer Toggle";
        }

        private static bool macroSupported(string sId)
        {
            return Array.IndexOf(aMacroCommands, sId) >= 0;
        }

        // Every menu command passes through here (addItem's click). The outermost command is the one recorded; a
        // command one runs in turn is not recorded again. An error still reaches DbDo's own handling, as before.
        private bool runWithMacro(string sCommand, Action actionCommand)
        {
            bool bRoot = iMacroDepth == 0;
            if (macroSession != null && macroSession.bPlaying && !isMacroControl(sCommand) && !macroObservational(sCommand))
            {
                Say.sayForced("A macro is playing. Record Macro stops it.");
                DbDoLog.write("macro: refused " + sCommand + " during playback");
                return false;
            }
            iMacroDepth++;
            try
            {
                if (bRoot)
                {
                    bMacroCancelled = false;
                    bMacroConfirmed = false;
                    sMacroArgument = "";
                    sMacroCommandId = Homer.KeyMap.toId(sCommand);
                    if (macroSession != null && macroSession.bRecording && !isMacroControl(sCommand) && !macroObservational(sCommand) && !macroSupported(sMacroCommandId))
                        markMacroGap(sCommand + ", which a macro cannot hold yet");
                }
                actionCommand();
                if (bRoot && macroSession != null && macroSession.bRecording && macroSupported(sMacroCommandId))
                {
                    // The view may have changed under a This View recording; that is a gap, never an error for the command.
                    if (bMacroConfirmed)
                    {
                        try { macroSession.record(sMacroCommandId, sCommand, sMacroArgument); }
                        catch (Exception exError) { markMacroGap(sCommand + " could not be recorded: " + exError.Message); }
                    }
                    else if (bMacroCancelled) DbDoLog.write("macro: " + sCommand + " was cancelled, so it is not recorded");
                    else markMacroGap(sCommand + " did not say it succeeded");
                }
                return true;
            }
            catch (Exception)
            {
                if (macroSession != null && macroSession.bRecording) markMacroGap(sCommand + " failed");
                throw;
            }
            finally { iMacroDepth--; }
        }

        // Something the macro cannot repeat. The recording goes on, so the person can finish what they were doing, but
        // it is marked incomplete and will not play; the first such thing is said at once, so it is no surprise later.
        private bool markMacroGap(string sReason)
        {
            if (macroSession == null || !macroSession.bRecording) return false;
            macroSession.gap(sReason);
            if (!bMacroGapSaid) Say.sayForced("Macro: " + sReason + ". This recording will not play; use the Navigate menu or Go to Record with a row number.");
            bMacroGapSaid = true;
            return true;
        }

        // Go To's prompt was cancelled: nothing happened, so nothing is recorded and the recording stays complete.
        private bool cancelMacroCapture()
        {
            if (iMacroDepth != 1) return false;
            bMacroCancelled = true;
            return true;
        }

        // A supported handler calls this after its move and refresh, so only a move that happened is recorded.
        private bool confirmMacroCommand(string sId, string sArgument)
        {
            if (iMacroDepth != 1 || sMacroCommandId != sId) return false;
            bMacroConfirmed = true;
            sMacroArgument = sArgument ?? "";
            return true;
        }

        // Go To is recorded only for a row number, and only when the cursor is on that row afterwards.
        private bool confirmMacroGoTo(string sInput)
        {
            int iRow;
            if (macroSession == null || !macroSession.bRecording) return false;
            sInput = (sInput ?? "").Trim();
            if (!int.TryParse(sInput, NumberStyles.None, CultureInfo.InvariantCulture, out iRow) || iRow < 1 || iRow > db.recordCount || db.absolutePosition != iRow)
            {
                markMacroGap("Go to Record with " + sInput + ", which is not a row number the cursor reached");
                return false;
            }
            return confirmMacroCommand("goToRecord", iRow.ToString(CultureInfo.InvariantCulture));
        }

        private void macroRecordClicked(object oSender, EventArgs evArgs)
        {
            toggleMacroRecording();
        }

        private async void macroPlayClicked(object oSender, EventArgs evArgs)
        {
            await playLastMacroAsync();
        }

        private void macroSaveClicked(object oSender, EventArgs evArgs)
        {
            try { saveRecordedMacro(); }
            catch (Exception exError) { reportMacroError(exError); }
        }

        private bool toggleMacroRecording()
        {
            string sChoice;
            try
            {
                if (macroSession.bPlaying) return macroSession.cancel();
                if (macroSession.bRecording)
                {
                    macroSession.stop();
                    Say.sayForced("Recorded " + macroSession.iCount + (macroSession.iCount == 1 ? " step." : " steps.") + (macroSession.bIncomplete ? " It has actions a macro cannot repeat, so it will not play." : ""));
                    if (macroSession.iCount == 0) return true;
                    return saveRecordedMacro();
                }
                using (LbcDialog lbcDialog = new LbcDialog("Record Macro", this))
                {
                    lbcDialog.addLabel("A macro records First, Last, Next and Previous Record, and Go to Record with a row number. Choose This View to play it only on this table with this filter and order, or Any View to play it on any table.");
                    sChoice = lbcDialog.runWithButtons(new string[] { "This View", "Any View", "Cancel" });
                }
                if (sChoice != "This View" && sChoice != "Any View") return false;
                macroSession.start(sChoice == "Any View");
                bMacroGapSaid = false;
                Say.sayForced("Recording a macro. Press Control+Shift+F12 to stop.");
                return true;
            }
            catch (Exception exError) { return reportMacroError(exError); }
        }

        private bool saveRecordedMacro()
        {
            string sName, sPath;
            TextBox txtName;
            if (macroSession.bPlaying || macroSession.bRecording) throw new InvalidOperationException("Stop the macro before saving it.");
            if (macroSession.iCount == 0) throw new InvalidOperationException("There is no macro to save. Record one first.");
            using (LbcDialog lbcDialog = new LbcDialog("Save Macro", this))
            {
                lbcDialog.addLabel("Macro &name:");
                txtName = lbcDialog.addTextBox(macroSession.sName, "The name is also the file's name in the Scripts folder.");
                if (!lbcDialog.runOkCancel()) { Say.sayForced("Not saved. Save Macro saves it later."); return false; }
                sName = txtName.Text.Trim();
            }
            if (string.IsNullOrEmpty(sName) || sName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || sName == "." || sName == ".." || sName.EndsWith(".") || sName.Length > 100)
                throw new ArgumentException("Give the macro a name of at most 100 characters that a file name can hold.");
            sPath = Path.Combine(ScriptHelper.getScriptDir(), sName + ".inix");
            if (File.Exists(sPath) && !MacroSession.isMacroFile(sPath)) throw new IOException("A settings file already has that name. Choose another.");
            if (File.Exists(sPath) && LbcMessage.show(this, "Replace the macro " + sName + "?", "Save Macro", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
            macroSession.save(sPath, sName);
            Say.sayForced("Macro saved: " + sName + ".");
            return true;
        }

        private bool reportMacroError(Exception exError)
        {
            DbDoLog.write("macro: error: " + exError);
            Say.sayForced("Macro: " + exError.Message);
            return false;
        }

        private async Task<bool> playLastMacroAsync()
        {
            try
            {
                Say.sayForced("Playing macro " + macroSession.sName + ".");
                await macroSession.playAsync();
                Say.sayForced("Macro done, " + macroSession.iPlayed + (macroSession.iPlayed == 1 ? " step." : " steps."));
                return true;
            }
            catch (OperationCanceledException) { Say.sayForced("Macro stopped after " + macroSession.iPlayed + (macroSession.iPlayed == 1 ? " step." : " steps.")); return false; }
            catch (Exception exError) { return reportMacroError(exError); }
        }

        private async Task<bool> playMacroFileAsync(string sPath)
        {
            try { macroSession.load(sPath); }
            catch (Exception exError) { return reportMacroError(exError); }
            return await playLastMacroAsync();
        }

        private bool cancelMacroBeforeClose()
        {
            if (macroSession == null || !macroSession.bPlaying) return false;
            macroSession.cancel();
            Say.sayForced("Stopping the macro. Close the window again once it stops.");
            return true;
        }
    }
}
