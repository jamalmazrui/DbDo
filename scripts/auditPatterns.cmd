@echo off
rem auditPatterns.cmd -- check DbDo against the patterns it is supposed to follow.
rem
rem     auditPatterns
rem
rem Installer labels, the Shift say layer, the shape of every Say answer, and
rem section headings spliced into comments. Each of these was broken at least
rem once and found by a person reading a speech history; they are checks now so
rem a build finds them instead.
rem
rem Exit code 0 when nothing failed, 1 when something did. Writes
rem logs\DbDo-audit-<date>-<time>.log.
setlocal
where python >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python was not found on the PATH.
    endlocal
    exit /b 1
)
python "%~dp0auditPatterns.py" %*
set exitCode=%errorlevel%
endlocal & exit /b %exitCode%
