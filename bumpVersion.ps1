# bumpVersion.ps1
# Advance the application version so every release is newer than the
# one before it -- which is what the F11 "Elevate Version" check
# compares (installed version vs. the latest GitHub release tag).
#
# Called automatically by buildDbDo.cmd on every build. You never run
# it by hand and never pass arguments; just build.
#
# Where the number comes from (and why it is reliable):
#   The next version is derived from your GIT RELEASE TAGS, not from a
#   file. buildDbDo unarchives may overwrite DbDo.cs (and its version
#   literal) from a delivered zip, but nothing overwrites your tags, so
#   the release history is the trustworthy source. Rule:
#     * latest release tag exists  -> next = that tag with patch + 1
#     * you set BuildInfo.VersionString HIGHER than the latest tag
#       (a deliberate minor/major bump) -> next = that literal
#     * no tags yet (first release) -> next = the literal as-is
#   Rebuilding within the same release cycle is idempotent: until you
#   actually tag the release, the number stays put, so builds do not
#   inflate the version.
#
# It then stamps the result into DbDo.cs's BuildInfo.VersionString;
# syncIssVersion.ps1 (run next by buildDbDo) copies it into the .iss,
# and tagRelease reads the .iss. One source, no drift.
#
# Reusable across projects (DbDo, EdSharp, FileDir): with no -sCsPath
# it uses DbDo.cs if present, else the first *.cs here that declares
# BuildInfo.VersionString.

[CmdletBinding()]
param(
    [string] $sCsPath = ''
)

$ErrorActionPreference = 'Stop'

function toParts([string] $v)
{
    $p = @($v -split '\.')
    while ($p.Count -lt 3) { $p += '0' }
    return ,$p
}

function cmpVer([string] $a, [string] $b)
{
    $pa = toParts $a; $pb = toParts $b
    for ($i = 0; $i -lt 3; $i++)
    {
        $x = [int]$pa[$i]; $y = [int]$pb[$i]
        if ($x -ne $y) { return ($x - $y) }
    }
    return 0
}

try
{
    # Resolve the source file.
    if (-not $sCsPath)
    {
        if (Test-Path -LiteralPath 'DbDo.cs' -PathType Leaf)
        {
            $sCsPath = 'DbDo.cs'
        }
        else
        {
            $oFound = Get-ChildItem -File -Filter *.cs -ErrorAction SilentlyContinue |
                Where-Object { (Get-Content -LiteralPath $_.FullName -Raw) -match 'VersionString\s*=\s*"' } |
                Select-Object -First 1
            if ($oFound) { $sCsPath = $oFound.Name }
        }
    }
    if (-not $sCsPath -or -not (Test-Path -LiteralPath $sCsPath -PathType Leaf))
    {
        throw "No source file with BuildInfo.VersionString found. Pass -sCsPath <App>.cs."
    }

    $aBytes  = [System.IO.File]::ReadAllBytes([System.IO.Path]::GetFullPath($sCsPath))
    $bHasBom = ($aBytes.Length -ge 3 -and $aBytes[0] -eq 0xEF -and $aBytes[1] -eq 0xBB -and $aBytes[2] -eq 0xBF)

    $sCs = Get-Content -LiteralPath $sCsPath -Raw -Encoding UTF8
    $oMatch = [regex]::Match($sCs, 'VersionString\s*=\s*"([^"]+)"')
    if (-not $oMatch.Success) { throw "BuildInfo.VersionString not found in $sCsPath." }
    $sLit = $oMatch.Groups[1].Value.Trim()

    # Highest release tag (v1.2.3 / 1.2.3), if any.
    $sTag = $null
    try
    {
        $aRaw = & git tag --list 2>$null
        foreach ($t in $aRaw)
        {
            $s = ([string]$t).Trim()
            if ($s -match '^[vV]?(\d+\.\d+(?:\.\d+)?)$')
            {
                $cand = $Matches[1]
                if ($null -eq $sTag -or (cmpVer $cand $sTag) -gt 0) { $sTag = $cand }
            }
        }
    }
    catch { $sTag = $null }

    if ($null -eq $sTag)
    {
        $sNext = $sLit                                   # first release: use the literal
    }
    elseif ((cmpVer $sLit $sTag) -gt 0)
    {
        $sNext = $sLit                                   # deliberate manual bump above last release
    }
    else
    {
        $p = toParts $sTag; $p[2] = [string]([int]$p[2] + 1); $sNext = ($p -join '.')   # auto patch
    }

    if ($sNext -eq $sLit)
    {
        Write-Host "bumpVersion: version stays $sLit (latest tag: $(if ($sTag) { $sTag } else { 'none' }))."
        exit 0
    }

    $sCsUpdated = [regex]::Replace($sCs, '(VersionString\s*=\s*")[^"]+(")', ('${1}' + $sNext + '${2}'), 1)

    $sTmp = $sCsPath + '.tmp'
    $oEnc = New-Object System.Text.UTF8Encoding($bHasBom)
    [System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($sTmp), $sCsUpdated, $oEnc)
    Move-Item -LiteralPath $sTmp -Destination $sCsPath -Force

    Write-Host "bumpVersion: $sLit -> $sNext (latest tag: $(if ($sTag) { $sTag } else { 'none' }); in $sCsPath)."
    exit 0
}
catch
{
    Write-Host "bumpVersion: ERROR -- $($_.Exception.Message). Source left unchanged."
    exit 1
}
