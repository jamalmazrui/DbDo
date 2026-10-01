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
# ONLY WHAT WAS TICKED (30 September 2026). The installer wrote the ticked
# captions to DbDo_ticked.txt when Finish was pressed; each component is
# reported only when its box was among them, and each line says whether what
# the box asked for happened. Then where the logs are, and nothing else.
$sTickedFile = Join-Path $sLogDir "$sApp`_ticked.txt"
$lsTicked = @()
if (Test-Path -LiteralPath $sTickedFile) {
  $lsTicked = @(Get-Content -LiteralPath $sTickedFile -ErrorAction SilentlyContinue | Where-Object { $_.Trim() -ne "" })
  try { Remove-Item -LiteralPath $sTickedFile -Force } catch { }
}
function tickedLine($sWord) { return @($lsTicked | Where-Object { $_ -match [regex]::Escape($sWord) })[0] }
$lsOutcomes = @()

$sCaption = tickedLine "JAWS"
if ($sCaption) {
  $sRecord = Join-Path $env:LOCALAPPDATA "$sApp\jawsSettings.log"
  $sStamp = Join-Path $env:LOCALAPPDATA "$sApp\jawsSettings.version"
  if ((Test-Path -LiteralPath $sRecord) -and (Test-Path -LiteralPath $sStamp) -and
      ((Get-Item -LiteralPath $sStamp).LastWriteTime -gt (Get-Date).AddMinutes(-30))) {
    $lsOutcomes += "JAWS scripts: installed."
  } else {
    $lsOutcomes += "JAWS scripts: NOT installed. The log says why."
  }
}

if (tickedLine "NVDA") {
  # The kit's screen reader script wrote its own line, installed or not.
  $sReaders = Join-Path $sLogDir "$sApp`_screenReaders.txt"
  $lsReader = @()
  if (Test-Path -LiteralPath $sReaders) { $lsReader = @(Get-Content -LiteralPath $sReaders | Where-Object { $_ -match "NVDA" }) }
  if ($lsReader.Count -gt 0) { $lsOutcomes += ($lsReader | ForEach-Object { $_.Trim().TrimEnd(".") + "." }) }
  else { $lsOutcomes += "NVDA add-on: the step left no record. The log says why." }
}

$sOllama = ollamaExe
$sHave = ""
if ($sOllama -ne "") { $sHave = (((probe $sOllama "--version") -split "\s+") | Where-Object { $_ -match "^\d+(\.\d+)+$" } | Select-Object -Last 1) }
$sCaption = tickedLine "Ollama"
if ($sCaption) {
  if ($sCaption -match "^Update Ollama from ([\d.]+) to ([\d.]+)") {
    if ($sHave -eq $Matches[2]) { $lsOutcomes += "Ollama: updated to $sHave." }
    else { $lsOutcomes += "Ollama: NOT updated -- still $sHave, though $($Matches[2]) was offered. The log has winget's answer." }
  } elseif ($sCaption -match "^Reinstall") {
    if ($sHave) { $lsOutcomes += "Ollama $sHave`: reinstalled." } else { $lsOutcomes += "Ollama: NOT reinstalled. The log says why." }
  } else {
    if ($sHave) { $lsOutcomes += "Ollama $sHave`: installed." } else { $lsOutcomes += "Ollama: NOT installed. The log says why." }
  }
}

$sCaption = tickedLine "llama3.2"
if ($sCaption) {
  $bModel = $false
  if ($sOllama -ne "") { $bModel = ((probe $sOllama "list") -match "llama3\.2") }
  $sVerb = if ($sCaption -match "^Reinstall") { "reinstalled" } else { "installed" }
  if ($bModel) { $lsOutcomes += "llama3.2: $sVerb." } else { $lsOutcomes += "llama3.2: NOT $sVerb. The log says why." }
}

if ($lsOutcomes.Count -gt 0) {
  say ""
  foreach ($sLine in $lsOutcomes) { say "  $sLine" }
}
say ""
say "Logs are kept in $sLogDir."
try { Remove-Item -LiteralPath $sActionsFile -ErrorAction SilentlyContinue } catch { }
if (-not $bQuiet) {
  Add-Type -AssemblyName System.Windows.Forms | Out-Null
  [System.Windows.Forms.MessageBox]::Show(($lLines -join [Environment]::NewLine), "$sApp Setup Results",
    [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information) | Out-Null
}

# The launch checkbox left a marker rather than starting the program, so that
# the program's own window cannot arrive on top of a box nobody has read yet.
# Now the box is closed, so the program can start.
# DbDo itself is started by the installer once this box is closed, as the
# person rather than with the installer's elevated rights (30 September 2026).
