; DbDuo.jss -- JAWS scripts for DbDuo.exe
;
; Purpose: define a single Script, PassDbDuoKey, that the companion
; DbDuo.jkm file references to disable JAWS's default behavior for
; certain key chords and let DbDuo handle them directly.
;
; Background: the JAWS key map file format requires that the right-
; hand side of each binding name a Script (not a Function). The
; JAWS built-in Function TypeCurrentScriptKey() does exactly what
; we want -- pass the current keystroke through to the foreground
; application as if no script were running -- but it cannot be
; invoked from a key map directly. So we wrap it in a Script.
;
; This file contains nothing else and has no Include or Use
; dependencies, so scompile.exe can build it with any installed
; JAWS version without needing shared headers to be on a search
; path. The DbDuo installer compiles it once into each JAWS year-
; version's settings folder so the binary is built against the
; same JAWS version that will load it.

Script PassDbDuoKey ()
TypeCurrentScriptKey ()
EndScript
