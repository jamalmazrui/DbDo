@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM =====================================================================
REM  cleanDir.cmd -- leave a SINGLE MASTER copy of each file in the DbDo
REM  development directory by moving duplicates / obsolete items out.
REM
REM  Targets:
REM    1. Loose <name>.db files in the root that are ALREADY the master
REM       copy under Samples\<name>\<name>.db.
REM    2. The obsolete central Scripts\ folder -- scripts now live beside
REM       their own .db inside each Samples\<name>\ folder.
REM
REM  Nothing is deleted. Everything is MOVED to a timestamped "attic"
REM  folder created NEXT TO (outside) the dev directory, so you can
REM  review it and delete it yourself once satisfied.
REM
REM  KEPT on purpose:
REM    - lookups.db  (root-level database; there is no Samples\lookups
REM      master, so it is not a duplicate).
REM    - Every source, config, and build file in the root.
REM =====================================================================

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

REM Databases whose master copy is Samples\<name>\<name>.db
set "DBLIST=cellar chinook contacts howtos media music NFB2026Convention northwind reads recipes sample"

REM ---- Pass 1: find candidates ----------------------------------------
set /a nFound=0
echo.
echo Scanning: %ROOT%
echo.
echo Duplicate / obsolete items found:
echo.

for %%D in (%DBLIST%) do (
    if exist "%ROOT%\%%D.db" if exist "%ROOT%\Samples\%%D\%%D.db" (
        echo   [db]      %%D.db     -- master kept at Samples\%%D\%%D.db
        set /a nFound+=1
    )
)
if exist "%ROOT%\Scripts" (
    echo   [folder]  Scripts\   -- scripts now live beside each .db in Samples\
    set /a nFound+=1
)

echo.
if %nFound%==0 (
    echo Nothing to clean -- already a single master of each file.
    goto :done
)
echo Found %nFound% item^(s^) to move.
echo.

set "ANS="
set /p "ANS=Move these to an attic folder now? (Y/N): "
if /I not "%ANS%"=="Y" (
    echo.
    echo Cancelled -- nothing was moved.
    goto :done
)

REM ---- Build a timestamped attic OUTSIDE the dev directory -------------
set "STAMP="
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set "STAMP=%%i"
if not defined STAMP set "STAMP=cleanup"
set "ATTIC=%ROOT%\..\DbDo_Attic_%STAMP%"
mkdir "%ATTIC%" 2>nul

REM ---- Pass 2: move ---------------------------------------------------
set /a nMoved=0
echo.
for %%D in (%DBLIST%) do (
    if exist "%ROOT%\%%D.db" if exist "%ROOT%\Samples\%%D\%%D.db" (
        move /y "%ROOT%\%%D.db" "%ATTIC%\" >nul
        if errorlevel 1 (
            echo   FAILED to move %%D.db
        ) else (
            echo   moved  %%D.db
            set /a nMoved+=1
        )
    )
)
if exist "%ROOT%\Scripts" (
    move /y "%ROOT%\Scripts" "%ATTIC%\Scripts" >nul
    if errorlevel 1 (
        echo   FAILED to move Scripts\
    ) else (
        echo   moved  Scripts\
        set /a nMoved+=1
    )
)

echo.
echo Done. Moved %nMoved% item^(s^) to:
echo   %ATTIC%
echo.
echo Review that folder, then delete it once you are satisfied.
echo NOTE: Scripts\ held 4 generic scripts not stored under Samples\
echo   ^(CopyRowToClipboard.js, MarkRowsMatchingRegex.js, SchemaOverview.sql,
echo    StatusSnapshot.dbdo^) -- retrieve them from the attic if you want them
echo   beside a specific database.
echo lookups.db was kept in the root ^(it has no Samples master^).

:done
echo.
endlocal
