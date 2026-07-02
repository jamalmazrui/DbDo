@echo off
rem ============================================================
rem  bumpVersion.cmd  -  Launcher for bumpVersion.ps1.
rem
rem  Increments DbDo.cs's BuildInfo.VersionString (the single
rem  source of truth) so the next release build carries a higher
rem  number than the installed copy -- which is what F11 (Elevate
rem  Version) compares. Run this BEFORE buildDbDo.cmd when cutting
rem  a release.
rem
rem  Usage:
rem    bumpVersion.cmd                 (patch: x.y.Z -> x.y.Z+1)
rem    bumpVersion.cmd -Part minor     (x.Y.z -> x.Y+1.0)
rem    bumpVersion.cmd -Part major     (X.y.z -> X+1.0.0)
rem    bumpVersion.cmd -Set 1.2.0      (set an explicit version)
rem ============================================================

setlocal

set "sScriptDir=%~dp0"
set "sPsScript=%sScriptDir%bumpVersion.ps1"

if not exist "%sPsScript%" (
    echo ERROR: Cannot find bumpVersion.ps1 next to bumpVersion.cmd
    echo Expected at: %sPsScript%
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%sPsScript%" %*
exit /b %ERRORLEVEL%
