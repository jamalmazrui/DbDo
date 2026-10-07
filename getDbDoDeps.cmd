@echo off
rem getDbDoDeps.cmd -- runs getDbDoDeps.ps1 beside it, passing every argument
rem through: fetches the pinned NPOI, SharpZipLib and BouncyCastle libraries
rem DbDo's .xlsx engine needs into this folder. build calls the .ps1 itself;
rem this is for running it by hand.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0getDbDoDeps.ps1" %*
exit /b %errorlevel%
