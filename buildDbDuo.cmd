@echo off
rem ====================================================================
rem buildDbDuo.cmd - build script for DbDuo.exe
rem
rem Compiles DbDuo.cs to DbDuo.exe as a 64-bit WinForms program
rem targeting .NET Framework 4.8.
rem
rem ---------- Why this script is simpler than typical ----------
rem
rem We let csc.exe auto-resolve all framework references from its own
rem csc.rsp configuration file. csc.rsp lives in the same directory as
rem csc.exe and references mscorlib, System, System.Core, System.Data,
rem System.Drawing, System.Windows.Forms, System.Xml, Microsoft.CSharp,
rem and other foundation assemblies. We need every one of those.
rem
rem That means: NO /nostdlib+, NO /reference: flags for system DLLs,
rem and crucially NO need for the .NET Framework 4.8 reference assembly
rem package (the targeting pack is convenience for cross-targeting in
rem MSBuild; it is not required for compiling on the same machine you
rem will run on). csc.exe links against the runtime DLLs in its own
rem directory.
rem
rem ---------- Compiler search order ----------
rem
rem 1. Roslyn csc.exe from VS Build Tools / Community / etc. Supports
rem    modern C# language features. Found under
rem    "%VSINSTALL%\MSBuild\Current\Bin\Roslyn\csc.exe".
rem 2. Legacy csc.exe from %SystemRoot%\Microsoft.NET\Framework64\
rem    v4.0.30319\csc.exe. Always present on Windows since 2012 (it
rem    is bundled with the .NET Framework runtime, not the SDK).
rem    Supports up to C# 5.
rem
rem DbDuo.cs uses only C# 5 features (lambdas, dynamic, var, generic
rem methods) so either compiler succeeds. We prefer Roslyn when
rem available for better diagnostics.
rem
rem ---------- Parens-in-paths gotcha ----------
rem cmd.exe parses an entire `if (...)` or `for (...)` block before any
rem variable substitution. If a %var% expands to a path containing
rem `(x86)`, the literal parens inside the expanded text terminate the
rem block early. Defenses:
rem   1. No `for %%P in (list-of-paths)` -- sequential if-exist checks.
rem   2. No %var% containing (x86) inside `if (...)` blocks -- use
rem      !var! delayed expansion (substitution after parsing).
rem   3. Negated-and-goto pattern instead of nesting blocks.
rem ====================================================================

setlocal enableextensions enabledelayedexpansion

rem cd to the script's own directory so relative paths to DbDuo.cs work
rem regardless of where the user invoked the script from.
pushd "%~dp0"

set "log=buildDbDuo.log"

rem ---- start a fresh log ----
echo DbDuo build log > "!log!"
echo Started at %DATE% %TIME% >> "!log!"
echo Script directory: %~dp0 >> "!log!"
echo Working directory: %CD% >> "!log!"
echo. >> "!log!"

rem ---- check that DbDuo.cs is here ----
if exist "DbDuo.cs" goto :have_source
echo ERROR: DbDuo.cs not found in script directory. >> "!log!"
echo ERROR: DbDuo.cs not found in script directory.
echo The build script must be run from the same folder as DbDuo.cs.
type "!log!"
popd
exit /b 1
:have_source
echo Found: DbDuo.cs >> "!log!"

rem ---- locate csc.exe (sequential if-exist; no for-in-list) ----
rem
rem Prefer Roslyn first. Fall back to the runtime-bundled legacy
rem compiler last.
set "csc="

rem Roslyn from VS Build Tools / VS / MSBuild Tools, both Program Files variants.
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\Roslyn\csc.exe"
if not defined csc if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe"

rem Last-resort: legacy csc.exe bundled with .NET Framework runtime.
rem Always present on Windows since the .NET 4.5 days.
if not defined csc if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" set "csc=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not defined csc if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe" set "csc=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if defined csc goto :have_csc
echo ERROR: No csc.exe found anywhere. This is unusual on a Windows system. >> "!log!"
echo ERROR: No csc.exe found anywhere. This is unusual on a Windows system.
echo Either install Visual Studio Build Tools 2022 or repair .NET Framework.
type "!log!"
popd
exit /b 1
:have_csc
echo Compiler: !csc! >> "!log!"
echo Using csc: !csc!
echo. >> "!log!"

rem ---- compile ----
rem
rem Bare minimum flags: target, platform, output, optimize. No
rem /reference: -- csc auto-resolves from csc.rsp. No /langversion --
rem the code uses only C# 5 features which every csc.exe supports.
rem
echo Compiling DbDuo.cs ... >> "!log!"
echo Compiling DbDuo.cs ...
echo.

"!csc!" /target:winexe /platform:x64 /optimize+ /nologo /out:DbDuo.exe DbDuo.cs >> "!log!" 2>&1

if errorlevel 1 goto :build_failed

echo. >> "!log!"
echo Build successful. >> "!log!"
echo Build successful: DbDuo.exe
dir DbDuo.exe | findstr DbDuo.exe

rem ---- generate HTML documentation from Markdown sources -------------
rem
rem The installer ships DbDuo.htm and README.htm so the Start Menu
rem documentation entry and the Finish-page "read documentation"
rem checkbox can open the manual in a browser. The HTML versions are
rem produced from the Markdown sources via Pandoc, which is available
rem standalone or via WinGet (winget install JohnMacFarlane.Pandoc).
rem
rem If Pandoc is not on PATH, the .htm files are not generated; the
rem build still succeeds, but Inno Setup will fail to package the
rem missing files. The user can either install Pandoc, or open
rem DbDuo.md / README.md directly in their preferred Markdown viewer.
rem
rem Pandoc options chosen:
rem   --standalone    Wrap output in a complete HTML document, not a
rem                   fragment. Required so the .htm renders by itself
rem                   when launched from Start Menu or Explorer.
rem   --metadata      Set page title for the browser tab. Pandoc reads
rem                   the title from the first H1 by default; we set
rem                   it explicitly for predictability.
rem   --toc           Generate a clickable table of contents at the top.
rem                   Helpful for users who land in the middle of a
rem                   long document.
rem   --toc-depth=3   Limit the TOC to H1/H2/H3 (skip H4+ to keep the
rem                   list manageable).
rem
echo. >> "!log!"
echo Generating HTML documentation ... >> "!log!"
echo Generating HTML documentation ...

where pandoc >nul 2>&1
if errorlevel 1 goto :no_pandoc

pandoc --standalone --toc --toc-depth=3 --metadata=title:"DbDuo User Guide" -o DbDuo.htm DbDuo.md >> "!log!" 2>&1
if errorlevel 1 (
    echo WARNING: pandoc failed on DbDuo.md; see !log! >> "!log!"
    echo WARNING: pandoc failed on DbDuo.md; see !log!
)

pandoc --standalone --toc --toc-depth=3 --metadata=title:"DbDuo README" -o README.htm README.md >> "!log!" 2>&1
if errorlevel 1 (
    echo WARNING: pandoc failed on README.md; see !log! >> "!log!"
    echo WARNING: pandoc failed on README.md; see !log!
)

if exist DbDuo.htm echo Generated: DbDuo.htm
if exist README.htm echo Generated: README.htm
goto :doc_done

:no_pandoc
echo WARNING: pandoc not found on PATH. >> "!log!"
echo WARNING: pandoc not found on PATH. Install with:
echo     winget install JohnMacFarlane.Pandoc
echo The build succeeded but DbDuo.htm and README.htm were NOT
echo generated. Inno Setup will fail to package those files until
echo Pandoc is installed and this script is rerun.

:doc_done
echo.
echo NOTE: DbDuo.exe needs ADODB (always present on Windows) plus a
echo provider for whichever file format you open:
echo   .db / .sqlite -- install sqliteodbc_w64.exe
echo   .mdb / .accdb / .xlsx / .dbf / .csv -- install accessdatabaseengine_X64.exe
echo See DbDuo.md for the install URLs and Test-Driver command.
popd
endlocal
exit /b 0

:build_failed
echo. >> "!log!"
echo BUILD FAILED. >> "!log!"
echo.
echo BUILD FAILED. See !log! for details.
type "!log!"
popd
endlocal
exit /b 1
