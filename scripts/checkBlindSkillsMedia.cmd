@echo off
rem checkBlindSkillsMedia.cmd -- checks that every BlindSkills lesson plays with no sign-in; arguments go to the script.
python "%~dp0checkBlindSkillsMedia.py" %*
exit /b %errorlevel%
