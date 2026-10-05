@echo off
rem fetchStations.cmd -- fill a RadioTrail database from Radio Browser and SomaFM.
rem Runs the Python beside it and passes every argument through: a database
rem path, --country "Name", --limit N, or --source somafm.
python "%~dp0fetchStations.py" %*
