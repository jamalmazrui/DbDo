@echo off
rem ====================================================================
rem buildDbDo.cmd - build script for DbDo.exe, built on the Homer Development Kit.
rem
rem DbDo compiles against the kit's shared classes in C:\HomerDev\CSharp rather
rem than its own copies of them. What that buys: a fix to Lbc or Say reaches
rem DbDo, EdSharp and FileDir together; the version DbDo was built against is
rem recorded in this log; and the kit's own checks prove those classes still
rem compile before DbDo ever sees them.
rem
rem Compiles two assemblies with the stock .NET Framework compilers:
rem
rem   1. DbDo.dll -- JScript .NET support module, compiled from
rem      DbDo.js by jsc.exe. Used by the Help > Invoke Snippet
rem      feature to run user-written .js snippets against the running
rem      DbDoForm and DbDoManager.
rem
rem   2. DbDo.exe -- the main WinForms application, compiled from
rem      DbDo.cs by csc.exe. The exe calls DbDo.JS.runScript(...)
rem      via reflection (Assembly.LoadFrom + GetType + GetMethod),
rem      so csc.exe does NOT take /reference:DbDo.dll at compile time.
rem      Avoiding the compile-time reference also prevents an assembly-
rem      name collision at load time: the EXE is also named DbDo,
rem      so Assembly.Load("DbDo") would resolve to DbDo.exe instead
rem      of DbDo.dll. We use Assembly.LoadFrom with the full path to
rem      DbDo.dll for an unambiguous load.
rem
rem ---------- Why bare compilers, not MSBuild + NuGet ----------
rem
rem v1.0.42 and v1.0.43 used MSBuild + NuGet to pull in the Roslyn
rem C# scripting package (Microsoft.CodeAnalysis.CSharp.Scripting),
rem which dragged about a dozen transitive DLLs (~25-30 MB) into the
rem DbDo install folder. That worked but was disproportionate to the
rem feature it enabled. v1.0.44 follows EdSharp's model: JScript .NET
rem via jsc.exe. JScript .NET ships with every .NET 4.x install in the
rem v4.0.30319 framework folder; no NuGet package, no shipped DLLs,
rem no binding redirects. The whole scripting subsystem is one ~10 KB
rem DbDo.dll plus the snippet folder.
rem
rem ---------- Compiler search order ----------
rem
rem csc.exe AND jsc.exe both live next to each other under either:
rem   - Visual Studio / Build Tools Roslyn folder (Roslyn-era csc.exe)
rem   - %SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\
rem
rem The Framework folder is present on every Windows install with
rem .NET 4.x, so the build works without Visual Studio. We prefer the
rem Visual Studio csc.exe when available for better diagnostics but
rem always use the Framework jsc.exe (no Roslyn equivalent for JScript).
rem ====================================================================

setlocal enableextensions enabledelayedexpansion

rem THE SCRIPT'S OWN FOLDER, CAPTURED ONCE, BEFORE ANYTHING CHANGES DIRECTORY.
rem
rem %~dp0 is not a constant. When a script is started by its bare name --
rem "buildDbDo.cmd", which is how checkHomerApp runs it -- %0 carries no path,
rem and %~dp0 is resolved against the CURRENT directory every time it is used.
rem After the pushd into exec it became C:\DbDo\exec\, so the JScript compile
rem looked for exec\DbDo.js, and fixEncoding was looked for there too and
rem silently never ran. Two failures, one cause.
rem
rem sHere is taken here and every later use names it instead.
pushd "%~dp0"
set "sHere=%CD%\"


rem THE LOG IS NAMED BY FULL PATH. It was relative, so once the build moved into
rem exec\ to compile, every line from the compile went to exec\buildDbDo.log
rem instead -- and the log beside this script jumped from the compiler check to
rem "Back to", saying nothing about whether DbDo.exe was built at all.
rem EVERY SESSION ITS OWN LOG, IN logs\, named as the program names its own:
rem <App>-<task>-yyyyMMdd-HHmmss.log. An alphabetical sort is then a
rem chronological one, and zipping logs\ gathers everything.
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set "sStamp=%%i"
if not exist "!sHere!logs" mkdir "!sHere!logs"
set "log=!sHere!logs\DbDo-build-%sStamp%.log"
echo DbDo build log > "!log!"
echo Started at %DATE% %TIME% (Pacific time, Seattle) >> "!log!"
echo Script directory: !sHere! >> "!log!"
echo Working directory: %CD% >> "!log!"
echo. >> "!log!"

rem ---- check source files ----
if not exist "DbDo.cs"  goto :no_dbdo_cs
if not exist "DbDo.js"  goto :no_dbdo_js
goto :have_sources

:no_dbdo_cs
echo ERROR: DbDo.cs not found. >> "!log!"
echo ERROR: DbDo.cs not found in script directory.
popd
exit /b 1

:no_dbdo_js
echo ERROR: DbDo.js not found. >> "!log!"
echo ERROR: DbDo.js not found in script directory.
popd
exit /b 1

:have_sources
echo Found: DbDo.cs, DbDo.js >> "!log!"
if not exist "DbDo.manifest" echo ERROR: DbDo.manifest not found.& popd & exit /b 1


rem ---- find the Homer Development Kit ----
rem DbDo no longer carries its own copies of the Homer classes. They live in
rem one place, so a fix reaches every app that uses them, and so the version
rem DbDo compiles against is a fact rather than a guess. Looked for in order:
rem the HomerDev environment variable, C:\HomerDev, then this folder.
set "homerDev="
if defined HomerDev if exist "%HomerDev%\CSharp\Lbc.cs" set "homerDev=%HomerDev%"
if not defined homerDev if exist "C:\HomerDev\CSharp\Lbc.cs" set "homerDev=C:\HomerDev"
if not defined homerDev if exist "%CD%\CSharp\Lbc.cs" set "homerDev=%CD%"
if not defined homerDev (
  echo ERROR: The Homer Development Kit was not found.
  echo Unzip it into C:\HomerDev, or set the HomerDev environment variable.
  echo ERROR: kit not found >> "!log!"
  popd
  exit /b 1
)
set "homerVer=unknown"
if exist "!homerDev!\version.txt" set /p homerVer=<"!homerDev!\version.txt"
echo Kit: !homerDev! version !homerVer! >> "!log!"
echo Kit: !homerDev! version !homerVer!

rem THE KIT MUST BE NEW ENOUGH FOR THE SOURCE. DbDo.cs uses what the kit gives
rem it -- Say.onSpoken, LbcMenuItem, Elevate -- and a kit older than the source
rem fails deep in the compiler with "Say does not contain a definition for
rem onSpoken", which names the symptom and not the cause. So the build says the
rem cause, first. Raise this whenever DbDo starts using something new.
set "kitNeeded=1.38.3"
powershell -NoProfile -Command "if ([version]'!homerVer!' -lt [version]'!kitNeeded!') { exit 1 } else { exit 0 }" >nul 2>&1
if errorlevel 1 (
  echo ERROR: DbDo needs HomerDev !kitNeeded! or later, and C:\HomerDev is !homerVer!. >> "!log!"
  echo.
  echo DbDo needs HomerDev !kitNeeded! or later, and C:\HomerDev is !homerVer!.
  echo Unzip HomerDev.zip into C:\HomerDev, then build again.
  exit /b 1
)

rem A MODULE MAY NEED ANOTHER MODULE, and only two do: Mdi.cs uses KeyMap, so
rem the pair is switched on together. DbDo has its own MDI frame for now, so
rem neither is compiled here; KeyMap is, because DbDo's hotkey document and its
rem alternate menu read from it.
set "homerSources="
rem ELEVATE GOES WHEREVER LBC GOES (kit 1.31 and later): Lbc's Help box carries
rem the version section and the F11 update offer, which are Elevate's.
set "homerSources=!homerSources! "!homerDev!\CSharp\Elevate.cs""
set "homerSources=!homerSources! "!homerDev!\CSharp\Inix.cs""
set "homerSources=!homerSources! "!homerDev!\CSharp\KeyMap.cs""
set "homerSources=!homerSources! "!homerDev!\CSharp\KeyName.cs""
set "homerSources=!homerSources! "!homerDev!\CSharp\Lbc.cs""
set "homerSources=!homerSources! "!homerDev!\CSharp\Log.cs""
set "homerSources=!homerSources! "!homerDev!\CSharp\Ollama.cs""
set "homerSources=!homerSources! "!homerDev!\CSharp\Paths.cs""
set "homerSources=!homerSources! "!homerDev!\CSharp\Say.cs""
set "homerSources=!homerSources! "!homerDev!\CSharp\Util.cs""
set "homerSources=!homerSources! "!homerDev!\CSharp\Web.cs""
echo Homer modules: !homerSources! >> "!log!"


rem ---- refresh the kit's tools into scripts\ ----
rem
rem ONE SOURCE OF TRUTH. These tools belong to the kit; DbDo keeps a working
rem copy so they are there beside the project, and every build takes the kit's
rem version again. Nothing here is edited in place: a fix made in the kit
rem reaches DbDo on its next build, and a change made here would be overwritten,
rem which is the point.
if not exist "scripts" mkdir "scripts"
for %%F in (buildTutorials.cmd buildTutorials.ps1 checkHomerApp.cmd checkHomerApp.py checkTutorial.cmd checkTutorial.py fixEncoding.cmd fixEncoding.py gitPush.cmd gitUnpushed.cmd gitUnpushed.py homerFinish.cmd homerInstall.cmd homerTidy.cmd homerTidy.py installOllama.cmd installScreenReaderSupport.cmd makeTutorials.cmd makeTutorials.py tagRelease.cmd tagRelease.ps1 uiCheck.cmd uiCheck.py) do (
  if exist "!homerDev!\scripts\%%F" copy /y "!homerDev!\scripts\%%F" scripts\ >nul
)
rem Tools the kit has retired, and DbDo's own near-duplicates of them. Similar
rem names are how the wrong tool gets run: makeTutorial beside makeTutorials,
rem cleanDir beside homerTidy, sayTutorial beside buildTutorials.
for %%F in (cleanDir.cmd cleanDir.py gitRelease.cmd homerPolicy.py installTools.cmd makeTutorial.cmd makeTutorial.py sayTutorial.cmd sayTutorial.py tidyRepo.cmd tidyRepo.py) do (
  if exist "scripts\%%F" del /q "scripts\%%F"
)
echo Refreshed the kit's tools into scripts. >> "!log!"

rem ---- the assemblies the Homer modules need ----
rem A shared module can need an assembly reference as well as another module.
rem Inix.cs reads and writes .xlsx files, which are zip archives, so it uses
rem System.IO.Compression. csc does not resolve that from csc.rsp, and the
rem failure reads as though the type were missing rather than the reference:
rem
rem   error CS0246: The type or namespace name 'ZipArchive' could not be found
rem
rem The kit's own build template passes these, and now so does this one. Each
rem module states what it needs in an ASSEMBLIES line at the top of its file.
set "homerRefs=/reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll"
set "homerRefs=!homerRefs! /reference:System.Core.dll /reference:System.Net.Http.dll"
set "homerRefs=!homerRefs! /reference:System.Web.Extensions.dll"
echo Homer references: !homerRefs! >> "!log!"

rem ---- remove the local copies the kit has replaced ----
rem An archive unpacked over this folder adds and replaces; it never deletes.
rem So the old Inix.cs, Lbc.cs, Say.cs and Web.cs would sit here looking
rem authoritative while nothing compiled them. Each goes only when the kit has
rem its replacement, so nothing is deleted without a copy already in place.
for %%f in (Inix.cs Lbc.cs Say.cs Web.cs) do (
  if exist "%%f" if exist "!homerDev!\CSharp\%%f" (
    del /f /q "%%f"
    echo REPLACED BY THE KIT: %%f >> "!log!"
  )
)

rem ---- version: version.txt is the SINGLE source of truth ----
rem The version lives in version.txt: one line, nothing else.  It is incremented
rem here, BEFORE anything is compiled, because both the program and the installer
rem bake the number in:
rem   * this script generates Version.cs from it, so the running program reports it
rem   * DbDo_setup.iss reads version.txt directly (see its #define AppVersion), so the
rem     installer is stamped with it
rem   * tagRelease tags the release with it
rem All three therefore always agree, which is what Elevate Version (F11) needs.
rem The .iss contains NO version number, so an old copy of it cannot rewind one.
rem
rem Pass "nobump" to recompile without taking a new number: buildDbDo.cmd nobump
if not exist "version.txt" (
  echo ERROR: version.txt not found. It must hold the current version, e.g. 1.0.0
  echo ERROR: version.txt not found. >> "!log!"
  popd
  exit /b 1
)
set "ver="
set /p ver=<version.txt
rem strip stray spaces; a version number never contains one
set "ver=!ver: =!"
if "!ver!"=="" (
  echo ERROR: version.txt is empty.
  echo ERROR: version.txt is empty. >> "!log!"
  popd
  exit /b 1
)

if /i "%~1"=="nobump" (
  echo Version: !ver! ^(nobump: keeping the current number^)
  echo Version: !ver! ^(nobump^) >> "!log!"
) else (
  call :takeNextVersion
)

rem ---- generate Version.cs from version.txt ----
rem Version.cs is generated output: do not edit it, and keep it out of git.
> Version.cs echo // Generated by buildDbDo.cmd from version.txt.  Do not edit; do not commit.
>> Version.cs echo public static class BuildVersion
>> Version.cs echo {
>> Version.cs echo     public const string Version = "!ver!";
>> Version.cs echo }

rem ---- locate csc.exe ----
rem Prefer Roslyn from Visual Studio Build Tools / VS 2022/2019, fall
rem back to legacy csc bundled with the Framework runtime.
set "csc="
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" set "csc=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not defined csc if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe" set "csc=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if defined csc goto :have_csc
echo ERROR: No csc.exe found. >> "!log!"
echo ERROR: No csc.exe found. Repair .NET Framework or install VS Build Tools.
popd
exit /b 1

:have_csc
echo C# compiler: !csc! >> "!log!"

rem ---- version note ----
rem DbDo used to bump DbDo.cs and then copy the number into DbDo_setup.iss.
rem That direction is retired: the .iss is now the single source of truth, the
rem bump happens inline above, and Version.cs carries the number into DbDo.cs
rem (BuildVersion.Version).  The old bumpVersion.ps1 and syncIssVersion.ps1 are
rem no longer used and may be deleted.

rem ---- locate UIA reference assemblies ----
rem v1.0.87 adds native UIA dispatch (NotificationHostControl /
rem AnnouncerProvider / UiaNative), which requires:
rem   UIAutomationProvider.dll  (IRawElementProviderSimple interface)
rem   UIAutomationTypes.dll     (AutomationNotificationKind / Processing)
rem
rem csc does not auto-resolve these from csc.rsp, so we pass them as
rem /reference: with full paths. Two probe locations, in priority order:
rem
rem   1. .NET Framework 4.8 Developer Pack reference assemblies (most
rem      machines that have csc also have these): C:\Program Files (x86)
rem      \Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\.
rem      This is the recommended source -- the reference-assembly DLLs
rem      are metadata-only and what csc is designed to bind against.
rem
rem   2. GAC runtime DLLs as fallback: %WINDIR%\Microsoft.NET\assembly
rem      \GAC_MSIL\<assembly>\v4.0_4.0.0.0__31bf3856ad364e35\<assembly>.dll.
rem      Always present on Windows 10+. csc accepts them as references
rem      even though they're the actual runtime images.
rem
rem If neither source is reachable, we abort with a clear message.
set "uiaProv="
set "uiaTypes="
set "refDir=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
if exist "!refDir!\UIAutomationProvider.dll" set "uiaProv=!refDir!\UIAutomationProvider.dll"
if exist "!refDir!\UIAutomationTypes.dll"    set "uiaTypes=!refDir!\UIAutomationTypes.dll"

rem Fallback to .NET Framework 4.7.2 / 4.7.1 / 4.7 / 4.6.2 reference
rem assemblies if 4.8 isn't installed. The API surface for these two
rem assemblies has been stable since 4.5.
if not defined uiaProv (
    for %%v in (v4.7.2 v4.7.1 v4.7 v4.6.2 v4.6.1 v4.6 v4.5.2 v4.5.1 v4.5) do (
        if not defined uiaProv if exist "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\%%v\UIAutomationProvider.dll" set "uiaProv=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\%%v\UIAutomationProvider.dll"
        if not defined uiaTypes if exist "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\%%v\UIAutomationTypes.dll" set "uiaTypes=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\%%v\UIAutomationTypes.dll"
    )
)

rem Fallback to the GAC runtime DLLs. Always present on Windows 10+.
if not defined uiaProv if exist "%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationProvider\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationProvider.dll" set "uiaProv=%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationProvider\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationProvider.dll"
if not defined uiaTypes if exist "%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationTypes\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationTypes.dll" set "uiaTypes=%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationTypes\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationTypes.dll"

if not defined uiaProv (
    echo ERROR: UIAutomationProvider.dll could not be located. >> "!log!"
    echo Checked Reference Assemblies for .NET Framework 4.5 through 4.8 >> "!log!"
    echo and the .NET 4.0 GAC under %SystemRoot%\Microsoft.NET\assembly. >> "!log!"
    echo ERROR: UIAutomationProvider.dll not found. Install the .NET
    echo Framework 4.8 Developer Pack from
    echo   https://dotnet.microsoft.com/download/dotnet-framework/net48
    echo or repair .NET Framework via Settings ^> Apps.
    popd
    exit /b 1
)
if not defined uiaTypes (
    echo ERROR: UIAutomationTypes.dll could not be located. >> "!log!"
    echo Checked Reference Assemblies for .NET Framework 4.5 through 4.8 >> "!log!"
    echo and the .NET 4.0 GAC under %SystemRoot%\Microsoft.NET\assembly. >> "!log!"
    echo ERROR: UIAutomationTypes.dll not found. Install the .NET
    echo Framework 4.8 Developer Pack from
    echo   https://dotnet.microsoft.com/download/dotnet-framework/net48
    echo or repair .NET Framework via Settings ^> Apps.
    popd
    exit /b 1
)
echo UIAutomationProvider: !uiaProv! >> "!log!"
echo UIAutomationTypes:    !uiaTypes! >> "!log!"

rem ---- locate jsc.exe ----
rem jsc.exe lives only in the Framework folder; there is no Roslyn
rem JScript compiler. Prefer 64-bit, fall back to 32-bit.
set "jsc="
if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\jsc.exe" set "jsc=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\jsc.exe"
if not defined jsc if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\jsc.exe" set "jsc=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\jsc.exe"

if defined jsc goto :have_jsc
echo ERROR: No jsc.exe found. >> "!log!"
echo ERROR: No jsc.exe found at %%SystemRoot%%\Microsoft.NET\Framework64\v4.0.30319\.
echo This file ships with the .NET Framework. Try repairing .NET Framework.
popd
exit /b 1

:have_jsc
echo JScript compiler: !jsc! >> "!log!"
echo Using compilers:
echo   C#:      !csc!
echo   JScript: !jsc!
echo. >> "!log!"

rem ---- EVERYTHING BINARY IS MADE IN exec\ ----
rem
rem The development folder mirrors the installed one: sources and build files at
rem the top, and each thing the program runs from in the folder it is installed
rem to. So the libraries are fetched into exec\, and DbDo.exe, DbDo.dll and the
rem importers are compiled into it. The sources are named by full path from
rem here, so nothing about them moves. To try a build, run exec\DbDo.exe.
if not exist "!sHere!exec" mkdir "!sHere!exec"
pushd "!sHere!exec"
echo Binaries go to: %CD% >> "!log!"

rem ---- fetch nvdaControllerClient.dll if missing (same as v1.0.43) ----
set "nvdaDll=nvdaControllerClient.dll"
set "nvdaUrl=https://download.nvaccess.org/releases/stable/nvda_2026.1_controllerClient.zip"
if exist "%nvdaDll%" goto :have_nvda_dll
echo Fetching %nvdaDll% ...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "$tmpZip = Join-Path $env:TEMP 'nvda_controllerClient.zip';" ^
  "$tmpDir = Join-Path $env:TEMP 'nvda_controllerClient';" ^
  "if (Test-Path $tmpZip) { Remove-Item -Force $tmpZip };" ^
  "if (Test-Path $tmpDir) { Remove-Item -Recurse -Force $tmpDir };" ^
  "Invoke-WebRequest -Uri '%nvdaUrl%' -OutFile $tmpZip -UseBasicParsing;" ^
  "Expand-Archive -Path $tmpZip -DestinationPath $tmpDir;" ^
  "$x64 = Join-Path $tmpDir 'x64';" ^
  "$dll = $null;" ^
  "foreach ($n in @('nvdaControllerClient.dll', 'nvdaControllerClient64.dll')) {" ^
  "  $p = Join-Path $x64 $n;" ^
  "  if (Test-Path $p) { $dll = $p; break }" ^
  "}" ^
  "if ($null -eq $dll) {" ^
  "  $dll = Get-ChildItem -Path $tmpDir -Recurse | Where-Object { $_.Name -in @('nvdaControllerClient.dll', 'nvdaControllerClient64.dll') } | Select-Object -First 1 -ExpandProperty FullName;" ^
  "}" ^
  "if ($null -eq $dll) { throw 'nvdaControllerClient.dll (or nvdaControllerClient64.dll) not found in archive' };" ^
  "Copy-Item -Path $dll -Destination '%nvdaDll%' -Force;" ^
  "Remove-Item -Force $tmpZip;" ^
  "Remove-Item -Recurse -Force $tmpDir;" >> "!log!" 2>&1
if errorlevel 1 (
    echo WARNING: NVDA controller DLL download failed; see %log%.
    echo Manual fallback: fetch x64\nvdaControllerClient.dll from
    echo   %nvdaUrl%
    echo and place it next to DbDo.exe.
)
:have_nvda_dll

rem ---- fetch Newtonsoft.Json.dll if missing (JSON / .ipynb support) ----
rem DbDo references Newtonsoft.Json (Json.NET, MIT license) for JSON
rem import/export, and for parsing Jupyter .ipynb notebooks. The DLL is
rem pulled from nuget.org (the official NuGet flat-container URL); the
rem net45 build, which runs on .NET 4.8, is extracted next to DbDo.exe.
set "jsonDll=Newtonsoft.Json.dll"
set "jsonVer=13.0.3"
set "jsonUrl=https://api.nuget.org/v3-flatcontainer/newtonsoft.json/13.0.3/newtonsoft.json.13.0.3.nupkg"
if exist "%jsonDll%" goto :have_json_dll

echo Fetching %jsonDll% ...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "$tmpZip = Join-Path $env:TEMP 'newtonsoft_json.zip';" ^
  "$tmpDir = Join-Path $env:TEMP 'newtonsoft_json';" ^
  "if (Test-Path $tmpZip) { Remove-Item -Force $tmpZip };" ^
  "if (Test-Path $tmpDir) { Remove-Item -Recurse -Force $tmpDir };" ^
  "Invoke-WebRequest -Uri '%jsonUrl%' -OutFile $tmpZip -UseBasicParsing;" ^
  "Expand-Archive -Path $tmpZip -DestinationPath $tmpDir;" ^
  "$dll = $null;" ^
  "foreach ($tf in @('net45','net46','net48','netstandard2.0')) {" ^
  "  $p = Join-Path $tmpDir ('lib\' + $tf + '\Newtonsoft.Json.dll');" ^
  "  if (Test-Path $p) { $dll = $p; break }" ^
  "}" ^
  "if ($null -eq $dll) {" ^
  "  $dll = Get-ChildItem -Path $tmpDir -Recurse -Filter 'Newtonsoft.Json.dll' | Select-Object -First 1 -ExpandProperty FullName;" ^
  "}" ^
  "if ($null -eq $dll) { throw 'Newtonsoft.Json.dll not found in package' };" ^
  "Copy-Item -Path $dll -Destination '%jsonDll%' -Force;" ^
  "Remove-Item -Force $tmpZip;" ^
  "Remove-Item -Recurse -Force $tmpDir;" >> "!log!" 2>&1
if errorlevel 1 (
    echo WARNING: Newtonsoft.Json download failed; see %log%.
    echo Manual fallback: download newtonsoft.json.%jsonVer%.nupkg from
    echo   https://www.nuget.org/packages/Newtonsoft.Json/%jsonVer%
    echo rename it to .zip, and copy lib\net45\Newtonsoft.Json.dll next to DbDo.exe.
)
:have_json_dll

rem ---- fetch NPOI + SharpZipLib + BouncyCastle (native .xlsx open/edit/save) ----
rem DbDo edits .xlsx via NPOI (Apache-2.0) + SharpZipLib (MIT) + BouncyCastle
rem (MIT). getDbDoDeps.ps1 downloads these managed DLLs (TLS 1.2, exact pinned
rem versions) into this folder and is idempotent. It runs AFTER the Newtonsoft
rem block on purpose: when Newtonsoft.Json.dll already exists, that blocks
rem guard does goto :have_json_dll, so anything placed before the label would be
rem skipped. The hard guard below stops the build with a clear message instead
rem of letting csc emit six CS0006 "Metadata file not found" errors.
if exist "NPOI.dll" if exist "NPOI.OOXML.dll" if exist "NPOI.OpenXml4Net.dll" if exist "NPOI.OpenXmlFormats.dll" if exist "ICSharpCode.SharpZipLib.dll" if exist "BouncyCastle.Crypto.dll" goto :have_npoi
echo Fetching NPOI 2.5.6, SharpZipLib 1.3.3, BouncyCastle 1.8.9 ...
powershell -NoProfile -ExecutionPolicy Bypass -File "!sHere!getDbDoDeps.ps1" >> "!log!" 2>&1
rem Wherever the fetch put them, they belong here.
for %%D in (NPOI.dll NPOI.OOXML.dll NPOI.OpenXml4Net.dll NPOI.OpenXmlFormats.dll ICSharpCode.SharpZipLib.dll BouncyCastle.Crypto.dll) do if exist "!sHere!%%D" if not exist "%%D" move /y "!sHere!%%D" . >nul
:have_npoi
set "xlsxMissing="
for %%D in (NPOI.dll NPOI.OOXML.dll NPOI.OpenXml4Net.dll NPOI.OpenXmlFormats.dll ICSharpCode.SharpZipLib.dll BouncyCastle.Crypto.dll) do if not exist "%%D" set "xlsxMissing=1"
if defined xlsxMissing (
    echo.
    echo ERROR: the NPOI .xlsx engine DLLs are missing and could not be downloaded.
    echo   getDbDoDeps.ps1 could not fetch them -- see %log% for the reason
    echo   ^(commonly no internet at build time, a proxy, or TLS/SSL blocking^).
    echo.
    echo   To fix by hand, on any PC with internet download these 3 packages,
    echo   rename each .nupkg to .zip, open it, and copy the listed DLLs into
    echo   "%CD%":
    echo     NPOI 2.5.6             lib\net45  -^> NPOI.dll, NPOI.OOXML.dll,
    echo                                         NPOI.OpenXml4Net.dll, NPOI.OpenXmlFormats.dll
    echo       https://www.nuget.org/api/v2/package/NPOI/2.5.6
    echo     SharpZipLib 1.3.3      lib\net45  -^> ICSharpCode.SharpZipLib.dll
    echo       https://www.nuget.org/api/v2/package/SharpZipLib/1.3.3
    echo     Portable.BouncyCastle  lib\net40  -^> BouncyCastle.Crypto.dll
    echo       https://www.nuget.org/api/v2/package/Portable.BouncyCastle/1.8.9
    echo   Then re-run buildDbDo.cmd.
    echo ERROR: NPOI .xlsx engine DLLs missing; aborted before compile. >> "!log!"
    exit /b 1
)


rem ---- fetch SQLean: the shell (sqlean.exe) and the extension
rem bundle (sqlean.dll) ----
rem These are TWO separate upstream projects, easy to confuse:
rem   sqlean.exe  -- the SQLite command-line shell with the SQLean
rem                  extensions built in, from nalgeon/sqlite. DbDo
rem                  shells out to it for the dot-prompt pass-through
rem                  lane, exposing the full sqlite3/SQLean shell.
rem   sqlean.dll  -- the all-in-one loadable extension bundle, from
rem                  nalgeon/sqlean (inside sqlean-win-x64.zip). DbDo
rem                  auto-loads it at connect time so REGEXP, median,
rem                  percentiles, and the rest are available to DbDo's
rem                  own connection. The init symbol sqlite3_sqlean_init
rem                  is derived from the filename, so it must stay named
rem                  sqlean.dll beside the executable.
rem Both are 64-bit, matching this x64 build. (nalgeon/sqlite is
rem archived but its release assets still download via /releases/latest.)
set "sqleanExe=sqlean.exe"
set "sqleanDll=sqlean.dll"
set "sqleanExeUrl=https://github.com/nalgeon/sqlite/releases/latest/download/sqlean.exe"
set "sqleanZipUrl=https://github.com/nalgeon/sqlean/releases/latest/download/sqlean-win-x64.zip"
if exist "%sqleanExe%" if exist "%sqleanDll%" goto :have_sqlean
echo Fetching %sqleanExe% and %sqleanDll% ...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "if (-not (Test-Path '%sqleanExe%')) { Invoke-WebRequest -Uri '%sqleanExeUrl%' -OutFile '%sqleanExe%' -UseBasicParsing };" ^
  "if (-not (Test-Path '%sqleanDll%')) {" ^
  "  $tmpZip = Join-Path $env:TEMP 'sqlean_ext.zip';" ^
  "  $tmpDir = Join-Path $env:TEMP 'sqlean_ext';" ^
  "  if (Test-Path $tmpZip) { Remove-Item -Force $tmpZip };" ^
  "  if (Test-Path $tmpDir) { Remove-Item -Recurse -Force $tmpDir };" ^
  "  Invoke-WebRequest -Uri '%sqleanZipUrl%' -OutFile $tmpZip -UseBasicParsing;" ^
  "  Expand-Archive -Path $tmpZip -DestinationPath $tmpDir;" ^
  "  $dll = Get-ChildItem -Path $tmpDir -Recurse -Filter 'sqlean.dll' | Select-Object -First 1 -ExpandProperty FullName;" ^
  "  if ($null -eq $dll) { throw 'sqlean.dll not found in bundle' };" ^
  "  Copy-Item -Path $dll -Destination '%sqleanDll%' -Force;" ^
  "  Remove-Item -Force $tmpZip;" ^
  "  Remove-Item -Recurse -Force $tmpDir;" ^
  "}" >> "!log!" 2>&1
if errorlevel 1 (
    echo WARNING: SQLean download failed; see %log%.
    echo The dot-prompt shell pass-through needs sqlean.exe, and REGEXP /
    echo median / percentiles need sqlean.dll, both beside DbDo.exe. Get them at:
    echo   sqlean.exe : https://github.com/nalgeon/sqlite/releases/latest
    echo   sqlean.dll : https://github.com/nalgeon/sqlean/releases/latest  ^(sqlean-win-x64.zip^)
)
:have_sqlean

rem ---- compile DbDo.js -> DbDo.dll (JScript .NET) ----
rem
rem /target:library so the result is a DLL we can load at runtime.
rem /reference: omitted -- jsc.exe auto-resolves mscorlib, System,
rem System.Windows.Forms, etc., from the framework directory.
rem /platform:anycpu so the DLL can be loaded by either x86 or x64
rem hosts (DbDo is x64 but anycpu is the convention for class libs).
rem /out: names the assembly explicitly.
rem
rem The DLL is loaded by DbDo.exe at run time via Assembly.LoadFrom,
rem not at compile time. No /reference: to DbDo.dll is passed to
rem csc.exe below, because a same-name compile-time reference would
rem create an assembly-name collision with DbDo.exe at load time.
echo. >> "!log!"
echo Compiling DbDo.js -> DbDo.dll ... >> "!log!"
echo Compiling DbDo.js -> DbDo.dll ...
rem THE SOURCE IS NAMED BY FULL PATH, because this runs with exec as the
rem working folder. A bare DbDo.js here looks for exec\DbDo.js and fails with
rem "Could not find file", which names the file and not the reason.
if not exist "!sHere!DbDo.js" (
  echo ERROR: DbDo.js is not in !sHere! -- the snippet module cannot be built. >> "!log!"
  echo DbDo.js is missing from the project folder. Unzip the delivery again.
  popd & popd & exit /b 1
)
"!jsc!" /target:library /platform:anycpu /nologo /out:DbDo.dll "!sHere!DbDo.js" >> "!log!" 2>&1
if errorlevel 1 goto :build_failed
echo DbDo.dll built.

rem ---- compile DbDo.cs -> DbDo.exe ----
rem
rem No /reference:DbDo.dll: the C# code calls DbDo.JS.runScript via
rem reflection (Assembly.LoadFrom + GetType + GetMethod), so no
rem compile-time reference is needed. Avoiding /reference: here also
rem prevents an assembly-name collision at load time, since both the
rem EXE and the snippet DLL have the simple name "DbDo".
rem
rem As of v1.0.87, csc.exe is passed explicit /reference: paths for
rem UIAutomationProvider.dll and UIAutomationTypes.dll (located above
rem and stored in !uiaProv! and !uiaTypes!). Other framework
rem references continue to auto-resolve from csc.rsp.
echo. >> "!log!"
echo Compiling DbDo.cs -> DbDo.exe ... >> "!log!"
echo Compiling DbDo.cs -> DbDo.exe ...
rem Delete any stale .exe first so a failed compile leaves no half-
rem written executable that Windows might mistake for a 16-bit binary
rem (the misleading "Unsupported 16-Bit Application" dialog appears
rem when the loader sees an empty or truncated MZ image).
if exist DbDo.exe del /f /q DbDo.exe
rem The icon is looked for where the sources are. After the build moved into
rem exec\ this check still said "if exist DbDo.ico", found nothing there, and
rem 1.0.173 was compiled without its icon.
if exist "!sHere!DbDo.ico" (
    "!csc!" /target:winexe /platform:x64 /optimize+ /nologo /win32icon:"!sHere!DbDo.ico" /win32manifest:"!sHere!DbDo.manifest" /reference:"!uiaProv!" /reference:"!uiaTypes!" /reference:"Newtonsoft.Json.dll" /reference:"Microsoft.VisualBasic.dll" /reference:"Microsoft.JScript.dll" /reference:"NPOI.dll" /reference:"NPOI.OOXML.dll" /reference:"NPOI.OpenXml4Net.dll" /reference:"NPOI.OpenXmlFormats.dll" /reference:"ICSharpCode.SharpZipLib.dll" /reference:"BouncyCastle.Crypto.dll" !homerRefs! /out:DbDo.exe "!sHere!Version.cs" "!sHere!DbDo.cs" !homerSources! >> "!log!" 2>&1
) else (
    echo NOTE: DbDo.ico not found; building without embedded icon. >> "!log!"
    "!csc!" /target:winexe /platform:x64 /optimize+ /nologo /win32manifest:"!sHere!DbDo.manifest" /reference:"!uiaProv!" /reference:"!uiaTypes!" /reference:"Newtonsoft.Json.dll" /reference:"Microsoft.VisualBasic.dll" /reference:"Microsoft.JScript.dll" /reference:"NPOI.dll" /reference:"NPOI.OOXML.dll" /reference:"NPOI.OpenXml4Net.dll" /reference:"NPOI.OpenXmlFormats.dll" /reference:"ICSharpCode.SharpZipLib.dll" /reference:"BouncyCastle.Crypto.dll" !homerRefs! /out:DbDo.exe "!sHere!Version.cs" "!sHere!DbDo.cs" !homerSources! >> "!log!" 2>&1
)
if errorlevel 1 goto :build_failed
echo DbDo.exe built.
dir DbDo.exe | findstr DbDo.exe

rem ---- build the 2db importer in BOTH bitnesses ----
rem 2db.cs is a standalone console converter (a source data file -> a
rem standard DbDo .db shell). It is built x86 AND x64 so DbDo can bridge
rem the 32/64-bit Office ACE boundary out-of-process: DbDo runs whichever
rem build matches the installed Office (and falls back to the other on a
rem provider-unavailable exit code). /target:exe (console) so the importer
rem can prompt and report on stdout/stderr. Microsoft.CSharp (for the
rem dynamic COM calls) auto-resolves from csc.rsp, as it does for DbDo.cs.
if not exist "!sHere!2db.cs" goto :skip_2db
echo. >> "!log!"
echo Compiling 2db.cs -^> 2db32.exe and 2db64.exe ... >> "!log!"
echo Compiling 2db.cs -^> 2db32.exe and 2db64.exe ...
if exist 2db32.exe del /f /q 2db32.exe
if exist 2db64.exe del /f /q 2db64.exe
"!csc!" /target:exe /platform:x86 /optimize+ /nologo /out:2db32.exe "!sHere!2db.cs" >> "!log!" 2>&1
if errorlevel 1 goto :build_failed
"!csc!" /target:exe /platform:x64 /optimize+ /nologo /out:2db64.exe "!sHere!2db.cs" >> "!log!" 2>&1
if errorlevel 1 goto :build_failed
echo 2db32.exe and 2db64.exe built.
dir 2db32.exe | findstr 2db32.exe
dir 2db64.exe | findstr 2db64.exe
goto :have_2db
:skip_2db
rem 2db.cs is optional, forward-looking infrastructure (a planned
rem out-of-process importer that would bridge the 32/64-bit Office ACE
rem boundary). It is not part of the current build: DbDo uses its
rem native readers and does not invoke 2db32/64.exe. When 2db.cs is
rem absent we skip it silently; drop a 2db.cs in this folder to build it.
:have_2db
popd
echo Back to: %CD% >> "!log!"


rem ---- SAMPLES THAT WERE RENAMED OR FOLDED INTO ANOTHER ----
rem
rem A renamed sample leaves its old folder behind, because unarchiving adds and
rem replaces but never deletes. These folders hold a database, its settings and
rem its scripts, so the whole folder goes rather than a list of files. Each was
rem either renamed (media to FilmTrail, reads to BookTrail, sample to
rem SchoolTrail, NFB2026Convention to ConventionTrail) or folded into another
rem (WindowsTutorials and iOSTutorials into HowToTrail).
rem
rem Only here, in the development folder. A copy in the user's own data folder
rem is theirs, and nothing in this build removes it.
for %%f in (cellar contacts howtos iOSTutorials media music NFB2026Convention reads recipes sample WindowsTutorials) do (
  if exist "templates\%%f" (
    rd /s /q "templates\%%f"
    echo Removed the retired sample "templates\%%f" >> "!log!"
  )
)

rem ---- files that were renamed ----
rem
rem FILES THAT WERE RENAMED LEAVE THE OLD COPY BEHIND, because unarchiving adds
rem and replaces but never deletes. The last run found 17 tutorials where there
rem are 9: the old names and the new ones, each built into the document twice
rem over. So the build removes what it knows has moved.
echo. >> "!log!"
for %%f in (
  "help\Tutorial_03_Adding.inix" "help\Tutorial_04_Editing.inix" "help\Tutorial_05_Finding.inix"
  "help\Tutorial_06_Sorting.inix" "help\Tutorial_07_Columns.inix" "help\Tutorial_08_Output.inix"
  "help\Tutorial_09_LookAndPrime.inix" "help\Tutorial_12_Inspect.inix" "help\Tutorial_13_WorkSearch.inix"
  "help\Tutorial_14_Menus.inix"
  "help\Tutorial_1_Installing.inix" "help\Tutorial_2_Opening.inix" "help\Tutorial_3_Adding.inix"
  "help\Tutorial_4_Editing.inix" "help\Tutorial_5_Finding.inix" "help\Tutorial_6_Sorting.inix"
  "help\Tutorial_7_Columns.inix" "help\Tutorial_8_Output.inix" "help\Tutorial_9_LookAndPrime.inix"
  "help\Tutorial.inix" "help\Tutorial_2_Adding.inix" "help\Tutorial_3_Editing.inix"
  "help\Tutorial_4_Finding.inix" "help\Tutorial_5_Sorting.inix" "help\Tutorial_6_Columns.inix"
  "help\Tutorial_7_Output.inix" "help\Tutorial_8_LookAndPrime.inix"
  "help\Tutorial_1_Installing.inix.bak"
  "help\Tutorial_Adding.inix" "help\Tutorial_Columns.inix" "help\Tutorial_Editing.inix"
  "help\Tutorial_Finding.inix" "help\Tutorial_Installing.inix" "help\Tutorial_LookAndPrime.inix"
  "help\Tutorial_Output.inix" "help\Tutorial_Sorting.inix"
  "scripts\buildTutorial.cmd" "scripts\buildTutorial.ps1" "cleanDir.cmd" "templates\reads\reads.db"
) do (
  if exist %%f (
    del /f /q %%f
    echo Removed the old %%f >> "!log!"
  )
)

rem ---- Hotkeys.md, from the menus themselves ----
rem Every key lives in one addItem call, so the hotkey document is generated
rem rather than kept by hand: a list kept by hand drifts the first time a key
rem changes, and this one would have drifted four times in a week.
echo. >> "!log!"
python "!sHere!scripts\makeHotkeys.py" >> "!log!" 2>&1
if errorlevel 1 echo WARN: makeHotkeys reported a problem; see logs\DbDo-hotkeys-*.log >> "!log!"

rem ---- generate HTML documentation ----
rem Every .md ships with a matching .htm, and pandoc is FETCHED when this machine
rem has none. A Homer build script asks the web for what it needs rather than
rem asking the person.
echo. >> "!log!"
echo Generating HTML documentation ... >> "!log!"
where pandoc >nul 2>&1
if errorlevel 1 (
  echo Installing pandoc, which writes the .htm copies of the documents...
  echo Pandoc not found; installing with winget ... >> "!log!"
  winget install --id JohnMacFarlane.Pandoc --silent --accept-source-agreements --accept-package-agreements >> "!log!" 2>&1
)
where pandoc >nul 2>&1
if errorlevel 1 goto :no_pandoc
pandoc --standalone --toc --toc-depth=3 --metadata=title:"DbDo User Guide" -o help\DbDo.htm help\DbDo.md >> "!log!" 2>&1
pandoc --standalone --toc --toc-depth=3 --metadata=title:"DbDo ReadMe" -o ReadMe.htm ReadMe.md >> "!log!" 2>&1
for %%m in (License.md) do (
  if exist "%%m" pandoc --standalone --metadata=title:"%%~nm" -o "%%~nm.htm" "%%m" >> "!log!" 2>&1
)
rem The help folder too: Tutorials.md is written by makeTutorials and the podcast
rem feed links to Tutorials.htm, so the pair has to stay together.
if exist "help\*.md" for %%m in (help\*.md) do (
  pandoc --standalone --toc --metadata=title:"%%~nm" -o "help\%%~nm.htm" "%%m" >> "!log!" 2>&1
)
echo Documentation converted with pandoc. >> "!log!"
goto :doc_done
:no_pandoc
echo NOTE: pandoc could not be installed, so the .htm files were not rebuilt.
echo Pandoc unavailable; .htm files left as they are. >> "!log!"

:doc_done

rem ---- the spoken tutorials, only when they are not already here ----
rem
rem A release carries help\Tutorials.mkv and help\Tutorials.md. Building them
rem takes minutes and fetches voices the first time, so it happens only when one
rem of them is MISSING -- which is exactly the state a fresh clone is in, and
rem never the state a working folder is in. To rebuild after editing a script,
rem run scripts\buildTutorials yourself.
rem ONLY WHAT IS MISSING IS BUILT. Speaking the tutorials takes minutes, and a
rem file's date is no guide to whether its content changed: unzipping a delivery
rem stamps every script as new. So the build makes the recording only when it is
rem not on disk. To re-record, delete help\Tutorials.mkv and the help\Tutorial*.mp3
rem files you want spoken again; each missing .mp3 is spoken, the rest are reused.
rem WHAT COUNTS AS BUILT: the transcript, and some audio in either form -- the
rem one recording with chapters, or one mp3 per walk in help\tutorials.
if not exist "help\Tutorials.md" goto :makeTutorials
if exist "help\Tutorials.mkv" goto :haveAudio
if exist "help\tutorials\*.mp3" goto :haveAudio
goto :makeTutorials
:haveAudio
echo Tutorials already built; delete the audio in help to rebuild. >> "!log!"
goto :tutorialsDone
:makeTutorials
if exist "help\Tutorial_*.inix" (
  echo Building the spoken tutorials. The first run fetches two voices...
  echo Tutorials.mkv missing; running buildTutorials -build >> "!log!"
  rem TWO THINGS THIS LINE GETS RIGHT, both learned the hard way.
  rem
  rem An EXPLICIT ARGUMENT: a bare call does not reset %*, so a build run as
  rem "buildDbDo nobump" hands "nobump" to the tutorial tool as though it were a
  rem script name. -build says what this call is.
  rem
  rem NO REDIRECTION: the tool names each walk as it speaks it, and speaking
  rem takes minutes. Sent to the log, the screen says nothing for twenty minutes
  rem and the build looks hung. Its own log still records the detail.
  call "!sHere!scripts\buildTutorials.cmd" -build
  if errorlevel 1 (
    rem THE BUILD STOPS HERE, because the installer needs what this makes and
    rem its own error names a missing file rather than the reason. The tutorial
    rem tool checks every walk before speaking any of them and says what is
    rem wrong; that is the message worth reading.
    echo ERROR: the tutorials were not built; see logs\DbDo-tutorials-check-*.log >> "!log!"
    echo.
    echo The tutorials were not built, so the installer cannot be made.
    echo The tutorial checker listed what is wrong with the walks:
    echo   logs\DbDo-tutorials-check-*.log
    popd
    exit /b 1
  )
) else (
  rem THE RECORDING IS MISSING AND SO ARE THE SCRIPTS THAT MAKE IT. The installer
  rem requires help\Tutorials.mkv, so this is the end of the build -- and saying so
  rem here, by name, beats the installer's error twenty lines later.
  rem
  rem How it happens: deleting "the tutorials" takes the Tutorial_*.inix scripts
  rem along with the .mkv and the .mp3 files. The scripts are the source; they are
  rem in the delivery zip and in nobody's repository, since they are listed in
  rem LocalFiles.txt.
  echo. >> "!log!"
  echo ERROR: help\Tutorials.mkv is missing, and so are the help\Tutorial_*.inix >> "!log!"
  echo scripts that would rebuild it. Unzip the delivery again to restore them. >> "!log!"
  echo.
  echo The tutorials cannot be built: help\Tutorials.mkv is missing, and so are
  echo the help\Tutorial_*.inix scripts that make it.
  echo.
  echo Unzip DbDo.zip into this folder again to restore the scripts, then build.
  echo To keep a recording you already have, put Tutorials.mkv back in help\.
  popd
  exit /b 1
)
:tutorialsDone

rem ---- the Homer encoding, after every file has been written ----
rem
rem THIS RUNS LATE, AND FROM THE PROJECT FOLDER, ON PURPOSE. It used to run
rem before the compile, guarded by "if exist scripts\fixEncoding.cmd" -- a
rem relative path tested while the working folder was exec, so the guard was
rem false and the tool never ran, silently. And pandoc, makeHotkeys and the
rem kit's refreshed scripts all write files after that point: pandoc with no
rem byte order mark, the kit's scripts with bare line feeds. Fixing encodings
rem before the files exist fixes nothing. So it runs here, at the top of the
rem project, when everything the installer will ship has been written, and it
rem is logged so its absence would be visible.
if exist "!sHere!scripts\fixEncoding.cmd" (
  echo Putting the files into the Homer encoding.
  echo Running fixEncoding >> "!log!"
  call "!sHere!scripts\fixEncoding.cmd" -build >> "!log!" 2>&1
  if errorlevel 1 echo WARN: fixEncoding reported a problem >> "!log!"
) else (
  echo WARN: scripts\fixEncoding.cmd is not here, so the encodings were not checked >> "!log!"
)

rem ---- build the installer ----
rem DbDo_setup.exe is part of the build, not a separate errand: one command
rem produces everything a release needs. Inno Setup is fetched with winget when
rem it is missing, for the same reason pandoc is.
echo. >> "!log!"
set "progFiles86=%ProgramFiles(x86)%"
set "progFiles=%ProgramFiles%"
set "iscc="
if exist "!progFiles86!\Inno Setup 6\ISCC.exe" set "iscc=!progFiles86!\Inno Setup 6\ISCC.exe"
if not defined iscc if exist "!progFiles!\Inno Setup 6\ISCC.exe" set "iscc=!progFiles!\Inno Setup 6\ISCC.exe"
if not defined iscc (
  echo Installing Inno Setup, which builds DbDo_setup.exe...
  echo Inno Setup not found; installing with winget ... >> "!log!"
  winget install --id JRSoftware.InnoSetup --silent --accept-source-agreements --accept-package-agreements >> "!log!" 2>&1
  if exist "!progFiles86!\Inno Setup 6\ISCC.exe" set "iscc=!progFiles86!\Inno Setup 6\ISCC.exe"
  if not defined iscc if exist "!progFiles!\Inno Setup 6\ISCC.exe" set "iscc=!progFiles!\Inno Setup 6\ISCC.exe"
)
if not defined iscc (
  echo ERROR: Inno Setup could not be installed, so DbDo_setup.exe was not built.
  echo ERROR: Inno Setup unavailable. >> "!log!"
  goto :build_failed
)
echo Inno Setup: !iscc! >> "!log!"
echo Compiling DbDo_setup.iss -^> DbDo_setup.exe ...
if not exist exec mkdir exec
if exist exec\DbDo_setup.exe del /f /q exec\DbDo_setup.exe
if exist DbDo_setup.exe del /f /q DbDo_setup.exe
"!iscc!" "DbDo_setup.iss" >> "!log!" 2>&1
if errorlevel 1 (
  echo ERROR: the installer build failed. See !log!.
  echo ERROR: the installer build failed. >> "!log!"
  goto :build_failed
)
if not exist exec\DbDo_setup.exe (
  echo ERROR: Inno Setup returned 0 but wrote no exec\DbDo_setup.exe.
  echo ERROR: no DbDo_setup.exe after a successful ISCC run. >> "!log!"
  goto :build_failed
)
echo Built DbDo_setup.exe version !ver!
echo Built DbDo_setup.exe version !ver! >> "!log!"

rem The build worked, so the number it used is now the current one.
if defined verPending (
  > version.txt echo !ver!
  echo version.txt is now !ver! >> "!log!"
)

echo.
echo Build complete. Artifacts in this directory:
echo   exec\DbDo_setup.exe -- the installer, version !ver!
echo   DbDo.exe       -- the application
echo   DbDo.dll       -- JScript .NET scripting support
echo   nvdaControllerClient.dll -- NVDA controller-client DLL
echo   Newtonsoft.Json.dll -- JSON (Json.NET) support
echo   sqlean.exe -- SQLite/SQLean shell for the dot-prompt pass-through lane
echo   sqlean.dll -- SQLean extension bundle (REGEXP, median, percentiles, ...)
if exist 2db64.exe echo   2db32.exe / 2db64.exe -- standalone importer (32- and 64-bit) for the Import command
echo.
echo To publish: gitPush "What changed." then gitRelease.
echo Build succeeded %DATE% %TIME% >> "!log!"
popd
endlocal
exit /b 0

:build_failed
echo. >> "!log!"
echo BUILD FAILED. See %log% for details.
type "!log!" | findstr /C:"error" /C:"Error" /C:"FAILED"
popd
endlocal
exit /b 1

:takeNextVersion
rem ---------------------------------------------------------------------------
rem Take the next version number: increment the last dotted part of !ver!.
rem
rem This makes NO network call.  An earlier version asked GitHub whether the number
rem was already taken -- but gh has no timeout, so a slow or unreachable network hung
rem the build with no message and no way to interrupt it.  A build script must never
rem wait on the network.  tagRelease does the "already released" check instead: that
rem is where a network call belongs, and where a stall is visible and interruptible.
rem
rem This runs as a subroutine rather than inside a parenthesised ( ) block, so each
rem line is parsed on its own -- avoiding the block-parsing traps batch has inside ( ).
rem ---------------------------------------------------------------------------
set "p1=" & set "p2=" & set "p3=" & set "p4="
set "new="
for /f "tokens=1-4 delims=." %%a in ("!ver!") do (
  set "p1=%%a" & set "p2=%%b" & set "p3=%%c" & set "p4=%%d"
)
if defined p4 (
  set /a p4=p4+1
  set "new=!p1!.!p2!.!p3!.!p4!"
) else if defined p3 (
  set /a p3=p3+1
  set "new=!p1!.!p2!.!p3!"
) else if defined p2 (
  set "new=!p1!.!p2!.1"
) else (
  set "new=!p1!.0.1"
)
if not defined new (
  echo ERROR: could not work out the next version from "!ver!".
  echo ERROR: could not work out the next version from "!ver!". >> "!log!"
  goto :eof
)
rem THE NUMBER IS TAKEN ONLY WHEN THE BUILD SUCCEEDS. version.txt used to be
rem written here, before the compiler had even been found, so every failed build
rem burned a release number and left version.txt ahead of the last thing that
rem actually built. The new number is used for this build; it is written to
rem version.txt at the end, after the installer is made.
echo Version: !ver! -^> !new! (not yet written to version.txt)
echo Version: !ver! -^> !new! (pending a successful build) >> "!log!"
set "ver=!new!"
set "verPending=1"
goto :eof
