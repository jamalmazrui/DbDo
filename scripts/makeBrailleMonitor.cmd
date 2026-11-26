@echo off
rem makeBrailleMonitor.cmd -- builds templates\BrailleMonitor\BrailleMonitor.db from the NFB Braille Monitor issues at nfb.org; arguments go to the script.
python "%~dp0makeBrailleMonitor.py" %*
exit /b %errorlevel%
