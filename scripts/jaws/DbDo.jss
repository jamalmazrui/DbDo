; DbDo.jss -- JAWS scripts for DbDo.exe
;
; Purpose: define a single Script, PassDbDoKey, that the companion
; DbDo.jkm file references to hand certain keystrokes straight to
; DbDo instead of running JAWS's default handler for them.
;
; Why this stops JAWS speaking the key name: when a key such as
; Backspace (DbDo's Exit-Child command in the data grid) has no
; script bound for the app, JAWS falls back to announcing the key
; by name. Binding it to this script makes JAWS treat the key as
; handled, so the name is not spoken; DbDo supplies its own spoken
; feedback for the command.
;
; Why normal editing still works outside the grid: the body is the
; JAWS built-in Function TypeCurrentScriptKey(), which passes the
; keystroke to the focused control as if no script ran. JAWS's
; character echo follows the control's text change, not this
; script, so in an edit field Backspace and Delete still speak the
; character they remove. A key map's right-hand side must name a
; Script (not a Function), which is why this one-line wrapper exists.
;
; This file has no Include or Use dependencies, so scompile.exe can
; build it against any installed JAWS version. The DbDo installer
; compiles it into each JAWS year-version settings folder so the
; binary matches the JAWS version that loads it.

Script passDbDoKey ()
TypeCurrentScriptKey ()
EndScript

; THE GRID'S ROW KEYS, PASSED ONLY IN THE GRID (2 November 2026). Up and Down
; Arrow and Page Up and Page Down are DbDo's in its data grid: DbDo moves the
; row and speaks it itself, the row's shown values and then its position. Left
; to JAWS's own scripts, each key also read the new row with JAWS's position
; information ("44838 of 49186"). Unlike the chords above, these keys belong to
; every list and edit field too, so each script passes the key quietly only
; when the focus is DbDo's grid -- its one list view (window class
; SysListView32) -- and otherwise runs JAWS's own script for the key, so
; dialogs and edit fields sound exactly as before. JAWS's keyboard interrupt
; still cuts off DbDo's speech for the previous row when the next key comes.

Int Function inDbDoGrid ()
Return StringContains (GetWindowClass (GetFocus ()), "SysListView32") > 0
EndFunction

Script nextDbDoRow ()
If inDbDoGrid () Then
TypeCurrentScriptKey ()
Else
PerformScript SayNextLine ()
EndIf
EndScript

Script priorDbDoRow ()
If inDbDoGrid () Then
TypeCurrentScriptKey ()
Else
PerformScript SayPriorLine ()
EndIf
EndScript

Script nextDbDoPage ()
If inDbDoGrid () Then
TypeCurrentScriptKey ()
Else
PerformScript JAWSPageDown ()
EndIf
EndScript

Script priorDbDoPage ()
If inDbDoGrid () Then
TypeCurrentScriptKey ()
Else
PerformScript JAWSPageUp ()
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
    string sHelp, sPath
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
