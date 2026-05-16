@echo off
rem ====================================================================
rem buildDbDuo.cmd - build script for DbDuo.exe (v1.0.44 and later).
rem
rem Compiles two assemblies with the stock .NET Framework compilers:
rem
rem   1. DbDuo.dll -- JScript .NET support module, compiled from
rem      DbDuo.js by jsc.exe. Used by the Help > Invoke Snippet
rem      feature to run user-written .js snippets against the running
rem      DbDuoForm and DbDuoManager.
rem
rem   2. DbDuo.exe -- the main WinForms application, compiled from
rem      DbDuo.cs by csc.exe. The exe calls DbDuo.JS.runScript(...)
rem      via reflection (Assembly.LoadFrom + GetType + GetMethod),
rem      so csc.exe does NOT take /reference:DbDuo.dll at compile time.
rem      Avoiding the compile-time reference also prevents an assembly-
rem      name collision at load time: the EXE is also named DbDuo,
rem      so Assembly.Load("DbDuo") would resolve to DbDuo.exe instead
rem      of DbDuo.dll. We use Assembly.LoadFrom with the full path to
rem      DbDuo.dll for an unambiguous load.
rem
rem ---------- Why bare compilers, not MSBuild + NuGet ----------
rem
rem v1.0.42 and v1.0.43 used MSBuild + NuGet to pull in the Roslyn
rem C# scripting package (Microsoft.CodeAnalysis.CSharp.Scripting),
rem which dragged about a dozen transitive DLLs (~25-30 MB) into the
rem DbDuo install folder. That worked but was disproportionate to the
rem feature it enabled. v1.0.44 follows EdSharp's model: JScript .NET
rem via jsc.exe. JScript .NET ships with every .NET 4.x install in the
rem v4.0.30319 framework folder; no NuGet package, no shipped DLLs,
rem no binding redirects. The whole scripting subsystem is one ~10 KB
rem DbDuo.dll plus the snippet folder.
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

set "log=buildDbDuo.log"
echo DbDuo build log > "!log!"
echo Started at %DATE% %TIME% (Pacific time, Seattle) >> "!log!"
echo Script directory: %~dp0 >> "!log!"
echo Working directory: %CD% >> "!log!"
echo. >> "!log!"

rem ---- check source files ----
if not exist "DbDuo.cs"  goto :no_dbduo_cs
if not exist "DbDuo.js"  goto :no_dbduo_js
goto :have_sources

:no_dbduo_cs
echo ERROR: DbDuo.cs not found. >> "!log!"
echo ERROR: DbDuo.cs not found in script directory.
popd
exit /b 1

:no_dbduo_js
echo ERROR: DbDuo.js not found. >> "!log!"
echo ERROR: DbDuo.js not found in script directory.
popd
exit /b 1

:have_sources
echo Found: DbDuo.cs, DbDuo.js >> "!log!"

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
  "  $dll = Get-ChildItem -Path $tmpDir -Recurse ^| Where-Object { $_.Name -in @('nvdaControllerClient.dll', 'nvdaControllerClient64.dll') } ^| Select-Object -First 1 -ExpandProperty FullName;" ^
  "}" ^
  "if ($null -eq $dll) { throw 'nvdaControllerClient.dll (or nvdaControllerClient64.dll) not found in archive' };" ^
  "Copy-Item -Path $dll -Destination '%nvdaDll%' -Force;" ^
  "Remove-Item -Force $tmpZip;" ^
  "Remove-Item -Recurse -Force $tmpDir;" >> "!log!" 2>&1
if errorlevel 1 (
    echo WARNING: NVDA controller DLL download failed; see %log%.
    echo Manual fallback: fetch x64\nvdaControllerClient.dll from
    echo   %nvdaUrl%
    echo and place it next to DbDuo.exe.
)
:have_nvda_dll

rem ---- compile DbDuo.js -> DbDuo.dll (JScript .NET) ----
rem
rem /target:library so the result is a DLL we can load at runtime.
rem /reference: omitted -- jsc.exe auto-resolves mscorlib, System,
rem System.Windows.Forms, etc., from the framework directory.
rem /platform:anycpu so the DLL can be loaded by either x86 or x64
rem hosts (DbDuo is x64 but anycpu is the convention for class libs).
rem /out: names the assembly explicitly.
rem
rem The DLL is loaded by DbDuo.exe at run time via Assembly.LoadFrom,
rem not at compile time. No /reference: to DbDuo.dll is passed to
rem csc.exe below, because a same-name compile-time reference would
rem create an assembly-name collision with DbDuo.exe at load time.
echo. >> "!log!"
echo Compiling DbDuo.js -> DbDuo.dll ... >> "!log!"
echo Compiling DbDuo.js -> DbDuo.dll ...
"!jsc!" /target:library /platform:anycpu /nologo /out:DbDuo.dll DbDuo.js >> "!log!" 2>&1
if errorlevel 1 goto :build_failed
echo DbDuo.dll built.

rem ---- compile DbDuo.cs -> DbDuo.exe ----
rem
rem No /reference:DbDuo.dll: the C# code calls DbDuo.JS.runScript via
rem reflection (Assembly.LoadFrom + GetType + GetMethod), so no
rem compile-time reference is needed. Avoiding /reference: here also
rem prevents an assembly-name collision at load time, since both the
rem EXE and the snippet DLL have the simple name "DbDuo".
rem
rem Everything else is the same as v1.0.41 and earlier: csc.exe
rem auto-resolves framework references from csc.rsp.
echo. >> "!log!"
echo Compiling DbDuo.cs -> DbDuo.exe ... >> "!log!"
echo Compiling DbDuo.cs -> DbDuo.exe ...
if exist DbDuo.ico (
    "!csc!" /target:winexe /platform:x64 /optimize+ /nologo /win32icon:DbDuo.ico /out:DbDuo.exe DbDuo.cs >> "!log!" 2>&1
) else (
    echo NOTE: DbDuo.ico not found; building without embedded icon. >> "!log!"
    "!csc!" /target:winexe /platform:x64 /optimize+ /nologo /out:DbDuo.exe DbDuo.cs >> "!log!" 2>&1
)
if errorlevel 1 goto :build_failed
echo DbDuo.exe built.
dir DbDuo.exe | findstr DbDuo.exe

rem ---- generate HTML documentation ----
echo. >> "!log!"
echo Generating HTML documentation ... >> "!log!"
where pandoc >nul 2>&1
if errorlevel 1 goto :no_pandoc
pandoc --standalone --toc --toc-depth=3 --metadata=title:"DbDuo User Guide" -o DbDuo.htm DbDuo.md >> "!log!" 2>&1
pandoc --standalone --toc --toc-depth=3 --metadata=title:"DbDuo README" -o README.htm README.md >> "!log!" 2>&1
goto :doc_done
:no_pandoc
echo WARNING: pandoc not found on PATH. Install with: winget install JohnMacFarlane.Pandoc

:doc_done
echo.
echo Build complete. Artifacts in this directory:
echo   DbDuo.exe       -- the application
echo   DbDuo.dll       -- JScript .NET scripting support
echo   nvdaControllerClient.dll -- NVDA controller-client DLL
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
