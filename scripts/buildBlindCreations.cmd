@echo off
rem Builds BlindCreations.db in templates\BlindCreations from the Blind directory pages.
rem With no argument it reads C:\MyPages\pages; give another folder after the command.
python "%~dp0buildBlindCreations.py" %*
