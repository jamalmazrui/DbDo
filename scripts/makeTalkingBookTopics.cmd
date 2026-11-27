@echo off
rem makeTalkingBookTopics.cmd -- finds the NLS Talking Book Topics issues and builds a pilot of their books; arguments go to the script.
python "%~dp0makeTalkingBookTopics.py" %*
exit /b %errorlevel%
