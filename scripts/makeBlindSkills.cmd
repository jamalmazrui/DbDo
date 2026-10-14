@echo off
rem makeBlindSkills.cmd -- rebuilds templates\BlindSkills\BlindSkills.db from the Blind Skills directory; arguments go to the script.
python "%~dp0makeBlindSkills.py" %*
exit /b %errorlevel%
