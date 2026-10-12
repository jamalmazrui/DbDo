@echo off
rem Checks every DbDo template with its SQL files, settings and reports, the way the
rem audit of 12 October 2026 did. --repair first fixes what DbDo's conventions settle;
rem --schema RadioTrail also writes RadioTrail.schema. The build runs it with both.
python "%~dp0checkTemplates.py" %*
exit /b %errorlevel%
