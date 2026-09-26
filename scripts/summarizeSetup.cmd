@echo off
rem summarizeSetup.cmd -- run the Results summary, hidden, with PowerShell's
rem parameters supplied here so the .iss does not have to carry them.
rem
rem Started from DbDo_setup.iss at the very end of setup, after every finish-page
rem entry has run. It is not a checkbox: the summary always happens.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0summarizeSetup.ps1" %*
