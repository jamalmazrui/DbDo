@echo off
rem fetchRadioBrowser.cmd -- fill a RadioTrail database from Radio Browser. Runs the
rem Python beside it and passes every argument through: a database path,
rem --country "Name", or --limit N.
python "%~dp0fetchRadioBrowser.py" %*
