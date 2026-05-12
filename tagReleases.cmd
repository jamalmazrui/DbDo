@echo off
rem ============================================================
rem  tagReleases.cmd  -  Launcher for tagReleases.ps1.
rem
rem  Invokes the PowerShell script with execution policy bypass for
rem  this single invocation (does not change the system policy),
rem  forwarding any arguments. The PowerShell script does its own
rem  logging to %TEMP%\tagReleases-<stamp>.log via Start-Transcript.
rem
rem  Usage:
rem    tagReleases.cmd
rem    tagReleases.cmd -AllowDirty
rem ============================================================

setlocal

set "sScriptDir=%~dp0"
set "sPsScript=%sScriptDir%tagReleases.ps1"

if not exist "%sPsScript%" (
    echo ERROR: Cannot find tagReleases.ps1 next to tagReleases.cmd
    echo Expected at: %sPsScript%
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%sPsScript%" %*
exit /b %ERRORLEVEL%
