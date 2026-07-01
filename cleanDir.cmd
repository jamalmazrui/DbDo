@echo off
setlocal EnableExtensions EnableDelayedExpansion

rem ======================================================================
rem  cleanDir.cmd
rem
rem  Move every file that is NOT part of the DbDo GitHub repo and NOT
rem  packaged by DbDo_setup.iss out of the working tree and into a legacy
rem  folder.  Nothing is deleted -- files are MOVED, so anything possibly
rem  useful later is preserved and can be moved back.
rem
rem  "Keep" means:
rem    1. every file Git tracks in this repo (git ls-files) -- this already
rem       honors .gitignore, so ignored build/scratch files are not tracked
rem       and therefore not "in the repo"; PLUS
rem    2. the files DbDo_setup.iss packages that Git may NOT track because
rem       they are fetched (the NPOI / SharpZipLib / BouncyCastle DLLs via
rem       getDbDoDeps.ps1) or built (DbDo.exe, DbDo.dll); PLUS
rem    3. a small set of intentionally-untracked, keep-on-disk maintainer
rem       files noted in .gitignore (tagRelease.cmd / tagRelease.ps1) and
rem       this script itself.
rem
rem  Everything else that is present but untracked is moved to %LEGACY%,
rem  preserving its relative path.  Files Git TRACKS are never touched, so
rem  the repo (including uncommitted edits to tracked files) is safe.
rem
rem  SAFETY: dry-run by default.  Review the printed list, then re-run with
rem  the /go switch to actually move the files:
rem        cleanDir.cmd          (preview only -- moves nothing)
rem        cleanDir.cmd /go      (perform the moves)
rem
rem  TIP: before running with /go, "git add" anything currently untracked
rem  that you want to KEEP in the tree; otherwise it will be moved to the
rem  legacy folder (from where you can always retrieve it).
rem ======================================================================

rem ---------------------------- configure -------------------------------
set "SRC=C:\DbDo"
set "LEGACY=C:\DbDuo"
set "LOG=%TEMP%\cleanDir.log"

set "GO=0"
if /i "%~1"=="/go" set "GO=1"

rem  Basenames that DbDo_setup.iss needs but Git may not track (fetched or
rem  built), plus keep-on-disk maintainer files.  Matched case-insensitively
rem  against each untracked file's name.  All of Samples\ and Scripts\ is
rem  kept wholesale (setup.iss packages those folders).
set "PROTECT= DbDo.exe DbDo.exe.config DbDo.dll DbDo_setup.exe"
set "PROTECT=%PROTECT% NPOI.dll NPOI.OOXML.dll NPOI.OpenXml4Net.dll NPOI.OpenXmlFormats.dll"
set "PROTECT=%PROTECT% ICSharpCode.SharpZipLib.dll BouncyCastle.Crypto.dll nvdaControllerClient.dll"
set "PROTECT=%PROTECT% THIRD-PARTY-NOTICES.txt DbDo.ico DbDo.manifest DbDo.cs DbDo.js"
set "PROTECT=%PROTECT% buildDbDo.cmd getDbDoDeps.ps1 DbDo_setup.iss DbDo.inix lookups.db"
set "PROTECT=%PROTECT% DbDo.md DbDo.htm README.md README.htm Announce.md Announce.htm"
set "PROTECT=%PROTECT% History.md History.htm License.md License.htm"
set "PROTECT=%PROTECT% CamelType_CSharp.md CamelType_CSharp.htm DbDo_JAWS.zip DbDo.nvda-addon"
set "PROTECT=%PROTECT% tagRelease.cmd tagRelease.ps1 cleanDir.cmd "

rem ------------------------------ guards --------------------------------
cd /d "%SRC%" 2>nul || ( echo ERROR: cannot enter %SRC% & exit /b 1 )
git rev-parse --is-inside-work-tree >nul 2>&1 || (
    echo ERROR: %SRC% is not a Git working tree.
    echo Aborting so nothing is moved by mistake.
    exit /b 1
)

if "%GO%"=="1" if not exist "%LEGACY%" md "%LEGACY%"

> "%LOG%" echo cleanDir run %DATE% %TIME%   mode=%GO% (0=dry-run, 1=go)
echo.
if "%GO%"=="1" ( echo MODE: MOVING files to %LEGACY%
) else (         echo MODE: DRY RUN -- nothing will be moved.  Add /go to perform moves. )
echo.

rem --------------------- collect untracked files ------------------------
rem  Two disjoint lists: untracked-not-ignored, then ignored-but-present.
set "LIST=%TEMP%\cleanDir_untracked.txt"
git ls-files --others --exclude-standard          >  "%LIST%"
git ls-files --others --ignored --exclude-standard >> "%LIST%" 2>nul

set /a MOVED=0, KEPT=0, FAILED=0

for /f "usebackq delims=" %%F in ("%LIST%") do (
    set "REL=%%F"
    set "REL=!REL:/=\!"
    set "SKIP=0"

    rem  keep everything under Samples\ and Scripts\ (both are 8 chars)
    if /i "!REL:~0,8!"=="Samples\" set "SKIP=1"
    if /i "!REL:~0,8!"=="Scripts\" set "SKIP=1"

    rem  keep protected basenames
    if "!SKIP!"=="0" (
        for %%B in ("!REL!") do set "BASE=%%~nxB"
        for %%P in (!PROTECT!) do if /i "%%P"=="!BASE!" set "SKIP=1"
    )

    if "!SKIP!"=="1" (
        set /a KEPT+=1
        >> "%LOG%" echo KEEP  !REL!
    ) else (
        if "%GO%"=="1" (
            set "DST=%LEGACY%\!REL!"
            for %%D in ("!DST!") do if not exist "%%~dpD" md "%%~dpD" 2>nul
            move /Y "!REL!" "!DST!" >nul 2>&1 && (
                set /a MOVED+=1
                >> "%LOG%" echo MOVED !REL!
            ) || (
                set /a FAILED+=1
                >> "%LOG%" echo FAIL  !REL!
                echo FAILED to move: !REL!
            )
        ) else (
            set /a MOVED+=1
            >> "%LOG%" echo WOULD-MOVE !REL!
            echo WOULD MOVE: !REL!
        )
    )
)

rem ------------- remove directories left empty (safe: rd skips full) ----
if "%GO%"=="1" (
    for /f "delims=" %%D in ('dir /ad /b /s 2^>nul ^| sort /r') do (
        echo %%D | findstr /i /c:"\.git" >nul || rd "%%D" 2>nul
    )
)

echo.
if "%GO%"=="1" (
    echo Done.  Moved !MOVED! file^(s^) to %LEGACY%.
    if !FAILED! gtr 0 echo   ^(!FAILED! could not be moved -- see the log.^)
    echo Kept !KEPT! protected untracked file^(s^).  Tracked repo files were untouched.
) else (
    echo DRY RUN complete: !MOVED! file^(s^) WOULD move; !KEPT! protected file^(s^) would stay.
    echo Re-run as:  cleanDir.cmd /go   to perform the moves.
)
echo Full log: %LOG%
echo.

endlocal
exit /b 0
