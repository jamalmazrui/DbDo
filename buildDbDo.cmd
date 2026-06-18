@echo off
rem ====================================================================
rem buildDbDo.cmd - build script for DbDo.exe (v1.0.44 and later).
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

pushd "%~dp0"

set "log=buildDbDo.log"
echo DbDo build log > "!log!"
echo Started at %DATE% %TIME% (Pacific time, Seattle) >> "!log!"
echo Script directory: %~dp0 >> "!log!"
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
"!jsc!" /target:library /platform:anycpu /nologo /out:DbDo.dll DbDo.js >> "!log!" 2>&1
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
if exist DbDo.ico (
    "!csc!" /target:winexe /platform:x64 /optimize+ /nologo /win32icon:DbDo.ico /win32manifest:DbDo.manifest /reference:"!uiaProv!" /reference:"!uiaTypes!" /reference:"Newtonsoft.Json.dll" /out:DbDo.exe DbDo.cs >> "!log!" 2>&1
) else (
    echo NOTE: DbDo.ico not found; building without embedded icon. >> "!log!"
    "!csc!" /target:winexe /platform:x64 /optimize+ /nologo /win32manifest:DbDo.manifest /reference:"!uiaProv!" /reference:"!uiaTypes!" /reference:"Newtonsoft.Json.dll" /out:DbDo.exe DbDo.cs >> "!log!" 2>&1
)
if errorlevel 1 goto :build_failed
echo DbDo.exe built.
dir DbDo.exe | findstr DbDo.exe

rem ---- generate HTML documentation ----
echo. >> "!log!"
echo Generating HTML documentation ... >> "!log!"
where pandoc >nul 2>&1
if errorlevel 1 goto :no_pandoc
pandoc --standalone --toc --toc-depth=3 --metadata=title:"DbDo User Guide" -o DbDo.htm DbDo.md >> "!log!" 2>&1
pandoc --standalone --toc --toc-depth=3 --metadata=title:"DbDo README" -o README.htm README.md >> "!log!" 2>&1
goto :doc_done
:no_pandoc
echo WARNING: pandoc not found on PATH. Install with: winget install JohnMacFarlane.Pandoc

:doc_done
echo.
echo Build complete. Artifacts in this directory:
echo   DbDo.exe       -- the application
echo   DbDo.dll       -- JScript .NET scripting support
echo   nvdaControllerClient.dll -- NVDA controller-client DLL
echo   Newtonsoft.Json.dll -- JSON (Json.NET) support
echo   sqlean.exe -- SQLite/SQLean shell for the dot-prompt pass-through lane
echo   sqlean.dll -- SQLean extension bundle (REGEXP, median, percentiles, ...)
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
