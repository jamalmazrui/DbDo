@echo off
rem rebuildRadioTrail.cmd -- a fresh RadioTrail from the template, filled from the
rem catalog and SomaFM, then every United States station asked what it says
rem about itself. Three steps, one command; about an hour, most of it the asking.
rem
rem   rebuildRadioTrail                   United States stations are enriched
rem   rebuildRadioTrail "United Kingdom"  another country is
rem   rebuildRadioTrail all               every station is asked (hours)
rem
rem The old copy is kept beside the new one as RadioTrail-old-<date>-<time>.db.
rem Each step logs as RadioTrail-fetch-<stamp>.log: in the project's logs folder
rem when run from C:\DbDo, and under %%LOCALAPPDATA%%\DbDo\logs when run from
rem an installed DbDo. A step that fails stops the rebuild.
setlocal
set "sCountry=%~1"
if "%sCountry%"=="" set "sCountry=United States"
echo Step 1 of 3: a fresh copy from the template, and the catalog.
python "%~dp0fetchStations.py" --fresh
if errorlevel 1 exit /b 1
echo.
if /i "%sCountry%"=="all" (
  echo Step 2 of 3: asking every station what it says about itself. This takes hours.
  python "%~dp0fetchStations.py" --enrich
) else (
  echo Step 2 of 3: asking the %sCountry% stations what they say about themselves.
  python "%~dp0fetchStations.py" --enrich --country "%sCountry%"
)
if errorlevel 1 (
  echo Step 2 failed; the log says why. RadioTrail has its catalog but not every station's own description.
  exit /b 1
)
echo.
echo Step 3 of 3: open RadioTrail in DbDo. Keywords, Control+K, now searches what the stations said.
