@echo off
rem auditMacros.cmd: runs auditMacros.py with the arguments given; see that script.
python "%~dp0auditMacros.py" %*
exit /b %errorlevel%
