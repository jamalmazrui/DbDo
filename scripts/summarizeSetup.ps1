# summarizeSetup.ps1 -- the single Results box, shown after everything.
#
# WHY THIS EXISTS, AND WHY IT IS NOT A CHECKBOX
#
# The Results box is part of installing, not an option somebody chooses. Inno
# runs the finish-page entries in the order they are listed, so the only moment
# at which the disposition of every checkbox is a fact rather than a guess is
# after the last one has run. DbDo_setup.iss starts this script from code at
# that point, hidden and without waiting, so closing the box is the last thing
# that happens.
#
# WHY POWERSHELL RATHER THAN A BATCH FILE. Batch has no way to bound a command
# that hangs, no reliable quoting for the text these lines carry, and a missing
# label ends the script without a word -- and this runs hidden, so there would
# be no window to find. Here every probe has a time limit, every line is written
# to disk as it is produced, and a failure is reported inside the box rather
# than ending it.
#
# WHAT THE BOX SAYS. Only what this installation actually did: where DbDo is,
# one line for each component that was installed or updated, what the AI
# components look like now, and where the log is. A step that did not run is not
# mentioned, because "not offered" reads like a fault to somebody who did not
# want it. Counts match their nouns.

param([switch]$bQuiet)

$sApp = "DbDo"
$sLogDir = Join-Path $env:LOCALAPPDATA "$sApp\logs"
$sLogFile = Join-Path $sLogDir "$sApp`_setup.log"
$sResultsFile = Join-Path $sLogDir "$sApp`_setup_results.txt"
$sActionsFile = Join-Path $sLogDir "$sApp`_setup_actions.txt"
$lLines = @()

function say($sText) {
  $script:lLines += $sText
  try { if ($sText.Trim() -ne "") { Add-Content -LiteralPath $sLogFile -Value "[summary] $sText" -Encoding UTF8 } } catch { }
}

function probe($sFile, $sArgs) {
  # A probe that hangs must not take the box with it.
  try {
    $oProcess = Start-Process -FilePath $sFile -ArgumentList $sArgs -NoNewWindow -PassThru `
      -RedirectStandardOutput "$env:TEMP\dbdo_probe.txt" -RedirectStandardError "$env:TEMP\dbdo_probe_err.txt"
    if (-not $oProcess.WaitForExit(20000)) { $oProcess.Kill(); return "" }
    return (Get-Content -LiteralPath "$env:TEMP\dbdo_probe.txt" -Raw -ErrorAction SilentlyContinue)
  } catch { return "" }
}

function ollamaExe() {
  $sUser = Join-Path $env:LOCALAPPDATA "Programs\Ollama\ollama.exe"
  if (Test-Path -LiteralPath $sUser) { return $sUser }
  $oCommand = Get-Command ollama -ErrorAction SilentlyContinue
  if ($oCommand) { return $oCommand.Source }
  return ""
}

# ---- what the installer already knew, written before the finish page ran ----
if (Test-Path -LiteralPath $sResultsFile) {
  foreach ($sLine in (Get-Content -LiteralPath $sResultsFile -ErrorAction SilentlyContinue)) { say $sLine }
} else {
  say "$sApp is installed."
}

# ---- what the finish-page entries did, one line each ----
$lActions = @()
if (Test-Path -LiteralPath $sActionsFile) {
  $lActions = @(Get-Content -LiteralPath $sActionsFile -ErrorAction SilentlyContinue | Where-Object { $_.Trim() -ne "" })
}
say ""
if ($lActions.Count -eq 0) {
  say "No optional component needed installing."
} else {
  say "Results"
  foreach ($sLine in $lActions) { say "  $sLine" }
}

# ---- the state of the AI components, since those are the ones people ask about ----
$sOllama = ollamaExe
if ($sOllama -ne "") {
  $sVersion = (probe $sOllama "--version").Trim()
  if ($sVersion -eq "") { $sVersion = "installed" }
  say ""
  say "Local AI"
  say "  Ollama: $sVersion"
  $sModels = probe $sOllama "list"
  $lModels = @($sModels -split "`n" | Select-Object -Skip 1 | Where-Object { $_.Trim() -ne "" })
  if ($lModels.Count -eq 0) {
    say "  Models: 0 installed. DbDo's Ask commands need one."
  } elseif ($lModels.Count -eq 1) {
    say "  Models: 1 installed."
  } else {
    say ("  Models: " + $lModels.Count + " installed.")
  }
}

say ""
say "Log"
say "  $sLogFile"

# ---- one box, at the end, and only then the program ----
try { Remove-Item -LiteralPath $sActionsFile -ErrorAction SilentlyContinue } catch { }
if (-not $bQuiet) {
  Add-Type -AssemblyName System.Windows.Forms | Out-Null
  [System.Windows.Forms.MessageBox]::Show(($lLines -join [Environment]::NewLine), "$sApp Setup Results",
    [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information) | Out-Null
}

# The launch checkbox left a marker rather than starting the program, so that
# the program's own window cannot arrive on top of a box nobody has read yet.
# Now the box is closed, so the program can start.
$sFlag = Join-Path $sLogDir "$sApp`_launch.flag"
if (Test-Path -LiteralPath $sFlag) {
  try { Remove-Item -LiteralPath $sFlag -ErrorAction SilentlyContinue } catch { }
  $sExe = Join-Path (Split-Path -Parent $PSScriptRoot) "exec\$sApp.exe"
  if (-not (Test-Path -LiteralPath $sExe)) { $sExe = Join-Path $PSScriptRoot "$sApp.exe" }
  try { Start-Process -FilePath $sExe -WorkingDirectory (Split-Path -Parent $sExe) } catch { }
}
