@echo off
rem probeOdbcFts.cmd -- tests full-text search (FTS5) through the SQLite ODBC driver DbDo uses; arguments go to the script.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0probeOdbcFts.ps1" %*
exit /b %errorlevel%
