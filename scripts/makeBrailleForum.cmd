@echo off
rem makeBrailleForum.cmd -- builds templates\BrailleForum\BrailleForum.db from the ACB Braille Forum issues at acb.org; arguments go to the script.
python "%~dp0makeBrailleForum.py" %*
exit /b %errorlevel%
