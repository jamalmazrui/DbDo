@echo off
rem fetchStations.cmd -- build the RadioTrail database: a clean copy, the whole
rem catalog, and what every station says about itself. One command; an hour or
rem two, most of it the asking, which can be stopped and picked up again.
rem Passes every argument through: --country "Name", --limit N, --catalog-only,
rem --enrich-only, --again, --fresh, or a database path.
python "%~dp0fetchStations.py" %*
