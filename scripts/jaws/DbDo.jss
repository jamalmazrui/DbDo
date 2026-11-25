;DbDo.jss -- JAWS scripts for DbDo.exe, from the HomerDev template.
;
; Each key has a way and a scope, the same as in DbDo's NVDA add-on (see the
; homer-screen-reader skill's references\key-handling.md). The app speaks for
; itself through the kit's Say; these scripts only decide who gets the key.
; No Include or Use: scompile builds it against any installed JAWS version.

; QUIET ROW MOVES (24 November 2026). Jamal heard Alt+DownArrow read the whole line after DbDo said the cell. DbDo
; moves the list's selection to the new record, and JAWS announces that change from its events -- the same events
; that read a row when the plain arrows move, which is why the plain arrows need no SayLine of their own. So a key
; DbDo answers itself marks the moment, and for the next 700 milliseconds JAWS's announcement of a new row in DbDo's
; list is skipped. A plain arrow clears the mark, so its row is read as always. GetTickCount can wrap, so a negative
; age counts as stale (the kit's key-handling reference).
Globals int iDbDoQuietTick

; Whether focus is in DbDo's list, the grid of records.
Int Function inDbDoList ()
Return StringContains (GetWindowClass (GetFocus ()), "SysListView32") > 0
EndFunction

Void Function quietDbDoRow ()
Let iDbDoQuietTick = GetTickCount ()
EndFunction

Int Function dbDoRowIsQuiet ()
Var
    int iAge
If iDbDoQuietTick == 0 Then
Return 0
EndIf
Let iAge = GetTickCount () - iDbDoQuietTick
If iAge < 0 || iAge > 700 Then
Let iDbDoQuietTick = 0
Return 0
EndIf
Return 1
EndFunction

; ActiveItemChangedEvent is overridden as the JAWS manual's own example does, calling the default for every other
; case. SayHighlightedText is left alone until a speech history shows JAWS using it here, since a wrong parameter
; list would stop the whole script file from compiling.
Void Function ActiveItemChangedEvent (handle hCurrent, int iCurrentObject, int iCurrentChild, handle hPrior, int iPriorObject, int iPriorChild)
If dbDoRowIsQuiet () && inDbDoList () Then
Return
EndIf
ActiveItemChangedEvent (hCurrent, iCurrentObject, iCurrentChild, hPrior, iPriorObject, iPriorChild)
EndFunction

; Way: pass quietly. Where: everywhere in DbDo. Alt+Control with an arrow, Home or End can move the record, which
; DbDo then says in its own words, so JAWS's announcement of the new row is skipped.
Script passDbDoKey ()
quietDbDoRow ()
TypeCurrentScriptKey ()
EndScript

; Way: pass quietly. Where: only here, in DbDo's list; elsewhere JAWS's own script. inDbDoList is defined near the
; top, before the first function that calls it.

Script nextDbDoRow ()
Let iDbDoQuietTick = 0
If inDbDoList () Then
TypeCurrentScriptKey ()
Else
PerformScript SayNextLine ()
EndIf
EndScript

; Way: pass quietly. Where: everywhere in DbDo; in the list DbDo moves a record and says the cell itself, and in
; a dialog's box the key opens its list as always. Passing the key was not enough to keep JAWS quiet (23 November
; 2026): JAWS reads the new row from its events, so the move is marked quiet first (24 November 2026).
Script altDbDoRow ()
quietDbDoRow ()
TypeCurrentScriptKey ()
EndScript

; JAWS'S OWN QUESTIONS, ANSWERED BY DbDo IN THE GRID (24 November 2026). Way: answer with the app's key. Where: only
; here, in DbDo's list with the PC cursor; everywhere else JAWS's own script runs, so the keys keep their meaning in
; dialogs and boxes. Each overrides JAWS's script by name, so the key a user has for it -- desktop or laptop layout,
; or one they chose -- is the key that works. The same DbDo keys answer NVDA's.
; SayFont (JAWS+F): the font means nothing in a grid of records; the field's declared type is its format.
Script SayFont ()
If inDbDoList () && IsPCCursor () Then
TypeKey ("Alt+Shift+F")
Else
PerformScript SayFont ()
EndIf
EndScript

; SayActiveCursor (JAWS+NumPadDelete): the cursor's place in pixels means nothing here; the record's place does, as
; EdSharp and FileDir answer the same key with their own position.
Script SayActiveCursor ()
If inDbDoList () && IsPCCursor () Then
TypeKey ("Alt+Delete")
Else
PerformScript SayActiveCursor ()
EndIf
EndScript

; SayCell (JAWS's Alt+Control+NumPad5): JAWS found no table here and said so; DbDo says the cell.
Script SayCell ()
If inDbDoList () && IsPCCursor () Then
TypeCurrentScriptKey ()
Else
PerformScript SayCell ()
EndIf
EndScript

; JAWS+A (Jamal's idea): in a browser JAWS reads the address bar; in DbDo's grid the address is the cell's -- its
; column's name, then its record and column numbers -- which DbDo gives on Alt+Control+Shift+NumPad5. The key map
; names Insert+A, not JAWSKey+A, so a laptop's CapsLock+A stays Say All. Elsewhere the key goes on, as when unbound.
Script sayDbDoAddress ()
If inDbDoList () && IsPCCursor () Then
TypeKey ("Alt+Control+Shift+NumPad5")
Else
TypeCurrentScriptKey ()
EndIf
EndScript

Script priorDbDoRow ()
Let iDbDoQuietTick = 0
If inDbDoList () Then
TypeCurrentScriptKey ()
Else
PerformScript SayPriorLine ()
EndIf
EndScript

; WHO THESE SCRIPTS ARE, AND THEIR GUIDE (2 November 2026). Two standard entry
; points every Homer app's scripts share. JAWS+Q starts with the scripts' name
; and the program's own version, read from the running program so it is never
; stale. JAWS+F1 pressed twice opens the program's guide; pressed once, JAWS's
; own help, as always.

Script ScriptFileName ()
ScriptAndAppNames ("DbDo scripts, for DbDo " + GetVersionInfoString (GetAppFilePath (), "FileVersion"))
EndScript

Script ScreenSensitiveHelp ()
Var
    ; EACH NAME TAKES ITS TYPE (3 November 2026): "string sHelp, sPath" stopped scompile at the second
    ; name, in every JAWS version, so no Homer app's JAWS scripts could install from this template.
    string sHelp, string sPath
If IsSameScript () Then
Let sPath = GetAppFilePath ()
Let sHelp = StringLeft (sPath, StringLength (sPath) - StringLength (GetAppFileName ())) + "help\\DbDo.htm"
If FileExists (sHelp) Then
Run (sHelp)
Return
EndIf
EndIf
PerformScript ScreenSensitiveHelp ()
EndScript

Script HotKeyHelp ()
; HOMER'S KEYS ON JAWS+H (9 November 2026). JAWS has no hot key help of its own for a Homer app, so JAWS+H
; presses Control+Slash twice: Where Am I in a window, followed by every command and key, the same as a
; screen reader user without these scripts gets from Control+Slash pressed twice.
TypeKey ("Control+Slash")
TypeKey ("Control+Slash")
EndScript

; Homer suite pass-through script -- written by the kit's tidy, because the key map names it; do not edit
; Way: pass quietly. Where: everywhere in DbDo. A key the suite hands on reaches the app.
Script TypeCurrentScriptKey ()
TypeCurrentScriptKey ()
EndScript
; end of Homer suite pass-through script
