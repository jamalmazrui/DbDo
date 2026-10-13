@echo off
rem makeMusicTrail.cmd -- rebuilds templates\MusicTrail\MusicTrail.db from the two music books; arguments go to the script.
python "%~dp0makeMusicTrail.py" %*
exit /b %errorlevel%
