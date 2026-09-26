@echo off
rem acceptCheck.cmd -- one acceptance test, answered by the exit code.
rem
rem WHY THIS EXISTS. An acceptance line in accept.inix is run through a shell,
rem and "cmd /c if exist X (exit 0) else (exit 1)" does not survive the trip:
rem the runner already wraps the line in cmd /c, so the parentheses and the
rem else are parsed a second time and the answer comes back 1 whatever is
rem true. Nine checks failed that way, and not one of them was about DbDo.
rem
rem So the test lives in a file, where cmd's own grammar works properly, and
rem the .inix line is a plain command with two words after it.
rem
rem   acceptCheck exists <path>    0 when the file or folder is there
rem   acceptCheck missing <path>   0 when it is NOT there
rem
rem Paths are relative to the project folder, which is this script's parent.
setlocal
set "sHere=%~dp0"
pushd "%sHere%.."
set "sWhat=%~1"
set "sPath=%~2"
if "%sPath%"=="" (
  echo acceptCheck: nothing to look for.
  popd
  endlocal
  exit /b 2
)
if /i "%sWhat%"=="exists" (
  if exist "%sPath%" (
    echo Found %sPath%.
    popd
    endlocal
    exit /b 0
  )
  echo Missing %sPath%.
  popd
  endlocal
  exit /b 1
)
if /i "%sWhat%"=="missing" (
  if exist "%sPath%" (
    echo %sPath% is still here.
    popd
    endlocal
    exit /b 1
  )
  echo %sPath% is gone, as it should be.
  popd
  endlocal
  exit /b 0
)
echo acceptCheck: "%sWhat%" is not exists or missing.
popd
endlocal
exit /b 2
