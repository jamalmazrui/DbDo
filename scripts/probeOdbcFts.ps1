# probeOdbcFts.ps1 -- asks the SQLite ODBC driver DbDo uses whether full-text search (FTS5) works through it, and
# tries the BrailleForum template's searches through it, so each build_release answers the question by itself.
#
# WHY (25 November 2026, Jamal: "if you are unsure whether the ODBC driver that DbDo uses supports FTS, please
# incorporate tests, with catch guards, in the build_release process, so that you can learn from the results of one
# cycle to improve the next one"). Python's SQLite has FTS5, but DbDo opens databases through the "SQLite3 ODBC
# Driver", whose own SQLite may be compiled without it. SQLean, which DbDo loads when sqlean.dll is beside it, adds
# functions but not FTS5. Only a test through the driver itself can say.
#
# WHAT IT TRIES, each step guarded, so a failure is recorded and the next step still runs:
#   1. The driver: its registry entry, its DLL and every DLL beside it, which may include loadable extensions.
#   2. A scratch database in work\probeOdbcFts, opened as DbDo opens one (with LoadExt for sqlean.dll when present):
#      sqlite_version(), the compile options, and an FTS5 table made, filled, searched and dropped.
#   3. When that fails, each DLL beside the driver whose name mentions fts is tried as a LoadExt, and the test repeated.
#   4. When templates\BrailleForum\BrailleForum.db exists: a copy in work, opened the same way, then a MATCH on
#      articles_fts, Filter Records' Full-text match condition, every saved .sql of the template, an edit of an
#      article's title (which fires the index's triggers), and the index's integrity check.
#
# It never fails the build: the answer is information for the next cycle. The console gets one line; the log,
# logs\DbDo-probeOdbcFts-<date>-<time>.log, gets every connection string, statement, result and error.

param([string]$sProject = "")

$ErrorActionPreference = "Continue"
if (-not $sProject) { $sProject = Split-Path -Parent $PSScriptRoot }
$sLogs = Join-Path $sProject "logs"
New-Item -ItemType Directory -Force -Path $sLogs | Out-Null
$sLog = Join-Path $sLogs ("DbDo-probeOdbcFts-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")
$sDriverName = "SQLite3 ODBC Driver"

function writeLog([string]$sText) {
    $sLine = (Get-Date -Format "HH:mm:ss.fff") + " " + $sText
    [System.IO.File]::AppendAllText($sLog, $sLine + "`r`n", (New-Object System.Text.UTF8Encoding($true)))
    return $true
}

function openConnection([string]$sDatabase, [string]$sExtra) {
    $sConnect = "DRIVER={" + $sDriverName + "};Database=" + $sDatabase + ";" + $sExtra
    writeLog ("CONNECT " + $sConnect) | Out-Null
    $oConn = New-Object System.Data.Odbc.OdbcConnection($sConnect)
    $oConn.Open()
    return $oConn
}

function runScalar($oConn, [string]$sSql) {
    $oCmd = $oConn.CreateCommand()
    $oCmd.CommandText = $sSql
    $vResult = $oCmd.ExecuteScalar()
    writeLog ("SQL " + $sSql + " -> " + $vResult) | Out-Null
    return $vResult
}

function runRows($oConn, [string]$sSql) {
    $oCmd = $oConn.CreateCommand()
    $oCmd.CommandText = $sSql
    $oReader = $oCmd.ExecuteReader()
    $lRows = New-Object System.Collections.Generic.List[string]
    while ($oReader.Read()) {
        $aValues = @()
        for ($i = 0; $i -lt $oReader.FieldCount; $i++) { $aValues += [string]$oReader.GetValue($i) }
        $lRows.Add(($aValues -join " | "))
    }
    $oReader.Close()
    writeLog ("SQL " + $sSql + " -> " + $lRows.Count + " row(s)") | Out-Null
    return ,$lRows
}

function runStatement($oConn, [string]$sSql) {
    $oCmd = $oConn.CreateCommand()
    $oCmd.CommandText = $sSql
    $iRows = $oCmd.ExecuteNonQuery()
    writeLog ("SQL " + $sSql + " -> done, " + $iRows) | Out-Null
    return $iRows
}

# FTS5 works when a table can be made, filled and searched. Returns "yes" or "no: <reason>".
function testFts5($oConn) {
    try {
        runStatement $oConn "CREATE VIRTUAL TABLE temp.homer_fts5_probe USING fts5(sText)" | Out-Null
        runStatement $oConn "INSERT INTO temp.homer_fts5_probe (sText) VALUES ('homer probe words')" | Out-Null
        $vHits = runScalar $oConn "SELECT count(*) FROM temp.homer_fts5_probe WHERE homer_fts5_probe MATCH 'probe'"
        runStatement $oConn "DROP TABLE temp.homer_fts5_probe" | Out-Null
        if ([string]$vHits -eq "1") { return "yes" }
        return "no: the test search found " + $vHits
    } catch {
        writeLog ("ERROR " + $_.Exception.Message) | Out-Null
        return "no: " + $_.Exception.Message.Trim()
    }
}

function tryStep([string]$sName, [scriptblock]$oBlock) {
    try { $vResult = & $oBlock; writeLog ("STEP " + $sName + ": " + $vResult) | Out-Null; return [string]$vResult }
    catch { writeLog ("STEP " + $sName + " FAILED: " + $_.Exception.Message.Trim()) | Out-Null; return "failed: " + $_.Exception.Message.Trim() }
}

writeLog ("probeOdbcFts.ps1 started; script " + $PSCommandPath) | Out-Null
writeLog ("PowerShell " + $PSVersionTable.PSVersion + "; " + [Environment]::OSVersion.VersionString + "; 64-bit process " + [Environment]::Is64BitProcess + "; cwd " + (Get-Location) + "; project " + $sProject) | Out-Null

$sAnswer = "unknown"
$lSummary = New-Object System.Collections.Generic.List[string]
try {
    # 1. The driver and what sits beside it.
    $sDriverDll = ""
    foreach ($sKey in @("HKLM:\SOFTWARE\ODBC\ODBCINST.INI\$sDriverName", "HKLM:\SOFTWARE\WOW6432Node\ODBC\ODBCINST.INI\$sDriverName")) {
        try {
            $oEntry = Get-ItemProperty -Path $sKey -ErrorAction Stop
            writeLog ("DRIVER ENTRY " + $sKey + ": " + (($oEntry.PSObject.Properties | Where-Object { $_.Name -notlike "PS*" } | ForEach-Object { $_.Name + "=" + $_.Value }) -join "; ")) | Out-Null
            if (-not $sDriverDll -and $oEntry.Driver) { $sDriverDll = [string]$oEntry.Driver }
        } catch { writeLog ("DRIVER ENTRY " + $sKey + ": none") | Out-Null }
    }
    $lExtensions = @()
    if ($sDriverDll -and (Test-Path $sDriverDll)) {
        $sDriverFolder = Split-Path -Parent $sDriverDll
        foreach ($oFile in Get-ChildItem -Path $sDriverFolder -Filter "*.dll" -ErrorAction SilentlyContinue) {
            writeLog ("BESIDE THE DRIVER " + $oFile.FullName + ", " + $oFile.Length + " bytes, version " + $oFile.VersionInfo.FileVersion) | Out-Null
            if ($oFile.Name -match "fts") { $lExtensions += $oFile.FullName }
        }
    } else { writeLog ("The driver's DLL was not found: '" + $sDriverDll + "'") | Out-Null }

    # 2. A scratch database, opened as DbDo opens one.
    $sWork = Join-Path $sProject "work\probeOdbcFts"
    New-Item -ItemType Directory -Force -Path $sWork | Out-Null
    $sScratch = Join-Path $sWork "scratch.db"
    if (Test-Path $sScratch) { Remove-Item $sScratch -Force }
    $sSqlean = ""
    foreach ($sCandidate in @((Join-Path $sProject "exec\sqlean.dll"), (Join-Path $sProject "sqlean.dll"))) { if (-not $sSqlean -and (Test-Path $sCandidate)) { $sSqlean = $sCandidate } }
    $sDbDoExtra = if ($sSqlean) { "LoadExt=" + $sSqlean.Replace("\", "/") + ";" } else { "" }
    writeLog ("SQLean " + ($(if ($sSqlean) { $sSqlean } else { "not found, so connections are made without LoadExt, as DbDo would" }))) | Out-Null
    $oConn = openConnection $sScratch $sDbDoExtra
    $sVersion = tryStep "sqlite_version" { runScalar $oConn "SELECT sqlite_version()" }
    $sOptions = tryStep "compile options" { (runRows $oConn "PRAGMA compile_options") -join " " }
    $sAnswer = testFts5 $oConn
    $oConn.Close()
    $lSummary.Add("SQLite " + $sVersion + " in the driver; FTS5 " + $sAnswer)
    writeLog ("FTS5 THROUGH THE DRIVER AS DbDo OPENS IT: " + $sAnswer + "; FTS options compiled in: " + (($sOptions -split " " | Where-Object { $_ -match "FTS" }) -join " ")) | Out-Null

    # 3. Without it, each fts extension beside the driver.
    $sWorkingExtra = $sDbDoExtra
    if ($sAnswer -ne "yes") {
        foreach ($sExtension in $lExtensions) {
            $sExtra = "LoadExt=" + (@($sExtension.Replace("\", "/")) + $(if ($sSqlean) { @($sSqlean.Replace("\", "/")) } else { @() }) -join ",") + ";"
            $sTried = tryStep ("FTS5 with " + $sExtension) { $oTry = openConnection $sScratch $sExtra; $sResult = testFts5 $oTry; $oTry.Close(); $sResult }
            $lSummary.Add("with " + (Split-Path -Leaf $sExtension) + ": FTS5 " + $sTried)
            if ($sTried -eq "yes") { $sAnswer = "yes, with " + (Split-Path -Leaf $sExtension); $sWorkingExtra = $sExtra; break }
        }
        if (-not $lExtensions) { writeLog "No DLL beside the driver mentions fts, so no extension was tried." | Out-Null }
    }

    # 4. The BrailleForum template's own searches, on a copy.
    $sTemplate = Join-Path $sProject "templates\BrailleForum\BrailleForum.db"
    if ((Test-Path $sTemplate) -and $sAnswer.StartsWith("yes")) {
        $sCopy = Join-Path $sWork "BrailleForum.db"
        Copy-Item $sTemplate $sCopy -Force
        $oDb = openConnection $sCopy $sWorkingExtra
        $lSummary.Add("MATCH on articles_fts: " + (tryStep "MATCH" { runScalar $oDb "SELECT count(*) FROM articles_fts WHERE articles_fts MATCH 'braille'" }) + " rows")
        $lSummary.Add("Full-text match filter: " + (tryStep "Full-text match" { runScalar $oDb "SELECT count(*) FROM articles WHERE ""article_id"" IN (SELECT rowid FROM ""articles_fts"" WHERE ""articles_fts"" MATCH '""audio description""')" }) + " rows")
        foreach ($oSql in Get-ChildItem -Path (Split-Path -Parent $sTemplate) -Filter "*.sql") {
            $sText = [System.IO.File]::ReadAllText($oSql.FullName)
            $sText = (($sText -split "`n") | Where-Object { -not $_.TrimStart().StartsWith("--") }) -join "`n"
            $sText = $sText.Trim().TrimEnd(";")
            $lSummary.Add($oSql.Name + ": " + (tryStep $oSql.Name { (runRows $oDb $sText).Count }) + " rows")
        }
        $lSummary.Add("edit a title (index triggers): " + (tryStep "edit" { runStatement $oDb "UPDATE articles SET title = title || ' (probe)' WHERE article_id = (SELECT min(article_id) FROM articles)" }))
        $lSummary.Add("integrity check: " + (tryStep "integrity" { runStatement $oDb "INSERT INTO articles_fts(articles_fts, rank) VALUES('integrity-check', 1)" | Out-Null; "ok" }))
        $oDb.Close()
        Remove-Item $sCopy -Force -ErrorAction SilentlyContinue
    } elseif (Test-Path $sTemplate) {
        $lSummary.Add("BrailleForum's searches were not tried, since FTS5 does not work through the driver")
    }
} catch {
    writeLog ("CRASH " + $_.Exception.ToString()) | Out-Null
    $lSummary.Add("the probe stopped: " + $_.Exception.Message.Trim())
}
foreach ($sLine in $lSummary) { writeLog ("SUMMARY " + $sLine) | Out-Null }
Write-Host ("ODBC full-text probe: FTS5 " + $sAnswer + ". " + $lSummary.Count + " findings in " + $sLog)
writeLog "RUN ENDED exit 0" | Out-Null
exit 0
