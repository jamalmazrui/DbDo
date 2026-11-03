@echo off
rem describeFields.cmd -- runs describeFields.py, passing every argument through.
setlocal
set "sScript=%~dp0describeFields.py"
where py >nul 2>nul
if not errorlevel 1 goto usePy
python "%sScript%" %*
exit /b %errorlevel%
:usePy
py -3 "%sScript%" %*
exit /b %errorlevel%
