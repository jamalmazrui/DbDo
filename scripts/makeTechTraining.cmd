@echo off
rem makeTechTraining.cmd -- rebuilds templates\TechTraining\TechTraining.db from the Tech Training directory; arguments go to the script.
python "%~dp0makeTechTraining.py" %*
exit /b %errorlevel%
