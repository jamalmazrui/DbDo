# syncIssVersion.ps1
# Copy the application version from DbDo.cs's BuildInfo.VersionString
# into DbDo_setup.iss's "#define AppVersion" line, so the installer,
# the uninstall entry, and the tagRelease git tag can never drift
# behind the app the way 1.0.105 drifted behind 1.0.113.
#
# Called by buildDbDo.cmd on every build:
#     powershell -NoProfile -ExecutionPolicy Bypass -File syncIssVersion.ps1
#
# Single source of truth: DbDo.cs. This script never invents a
# version; if it cannot read one it leaves the .iss untouched and
# returns non-zero, so a quoting or path problem can never corrupt
# the installer script. The rewrite goes through a temp file and is
# moved into place only after it succeeds, so a failure mid-write
# cannot truncate DbDo_setup.iss.

[CmdletBinding()]
param(
    [string] $sCsPath  = 'DbDo.cs',
    [string] $sIssPath = 'DbDo_setup.iss'
)

$ErrorActionPreference = 'Stop'

try
{
    if (-not (Test-Path -LiteralPath $sCsPath -PathType Leaf))
    {
        Write-Host "syncIssVersion: $sCsPath not found; leaving $sIssPath unchanged."
        exit 1
    }
    if (-not (Test-Path -LiteralPath $sIssPath -PathType Leaf))
    {
        Write-Host "syncIssVersion: $sIssPath not found; nothing to sync."
        exit 1
    }

    # Pull the version out of:  public const string VersionString = "1.0.113";
    $sCs = Get-Content -LiteralPath $sCsPath -Raw -Encoding UTF8
    $oMatch = [regex]::Match($sCs, 'VersionString\s*=\s*"([^"]+)"')
    if (-not $oMatch.Success)
    {
        Write-Host "syncIssVersion: could not find BuildInfo.VersionString in $sCsPath; leaving $sIssPath unchanged."
        exit 1
    }
    $sVersion = $oMatch.Groups[1].Value.Trim()
    if ($sVersion.Length -eq 0)
    {
        Write-Host "syncIssVersion: VersionString is empty; leaving $sIssPath unchanged."
        exit 1
    }

    # Rewrite the AppVersion #define, preserving the leading text and
    # the file's existing line endings (Get-Content -Raw keeps them).
    $sIss = Get-Content -LiteralPath $sIssPath -Raw -Encoding UTF8
    $oDefine = [regex]::Match($sIss, '(?m)^(\s*#define\s+AppVersion\s+)"[^"]*"')
    if (-not $oDefine.Success)
    {
        Write-Host "syncIssVersion: no '#define AppVersion' line in $sIssPath; leaving it unchanged."
        exit 1
    }

    $sOld = $oDefine.Groups[0].Value
    $sNew = $oDefine.Groups[1].Value + '"' + $sVersion + '"'
    if ($sOld -eq $sNew)
    {
        Write-Host "syncIssVersion: DbDo_setup.iss already at $sVersion; no change."
        exit 0
    }

    $sIssUpdated = $sIss.Substring(0, $oDefine.Index) + $sNew + $sIss.Substring($oDefine.Index + $sOld.Length)

    # Write to a temp file first, then move into place, so a failure
    # cannot leave a half-written .iss.
    $sTmp = $sIssPath + '.tmp'
    $oEnc = New-Object System.Text.UTF8Encoding($false)   # UTF-8, no BOM
    [System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($sTmp), $sIssUpdated, $oEnc)
    Move-Item -LiteralPath $sTmp -Destination $sIssPath -Force

    Write-Host "syncIssVersion: DbDo_setup.iss AppVersion set to $sVersion (from $sCsPath)."
    exit 0
}
catch
{
    Write-Host "syncIssVersion: ERROR -- $($_.Exception.Message). Left $sIssPath unchanged."
    exit 1
}
