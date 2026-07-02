# bumpVersion.ps1
# Increment the application version in the ONE source of truth:
# the C# file's BuildInfo.VersionString literal. Run this when you
# cut a release, BEFORE building.
#
# Why this exists (and why the build does not auto-bump):
#   Versioning best practice is that a version increment is a
#   DELIBERATE act -- "defined in the source code ... subject to the
#   same review as a code change" -- not something a build system
#   invents on every compile. So bumping is explicit: you ask for it.
#   Everything downstream is then DERIVED from this one literal:
#     BuildInfo.VersionString  (source of truth, in <App>.cs)
#       -> assembly attributes + the app's F11 "installed" version
#       -> syncIssVersion.ps1 copies it into <App>_setup.iss
#       -> tagRelease reads the .iss to tag + publish the release
#   Because only this literal is edited and the rest is copied, the
#   .exe, the installer, and the git tag can never drift apart.
#
# Reusable across projects (DbDo, EdSharp, FileDir, ...): with no
# -sCsPath it uses <App>.cs = DbDo.cs if present, otherwise the first
# *.cs in the current folder that declares BuildInfo.VersionString.
#
# Usage:
#   .\bumpVersion.ps1                 patch bump (x.y.Z -> x.y.Z+1)
#   .\bumpVersion.ps1 -Part minor     x.Y.z -> x.Y+1.0
#   .\bumpVersion.ps1 -Part major     X.y.z -> X+1.0.0
#   .\bumpVersion.ps1 -Set 1.2.0      set an explicit version
#   .\bumpVersion.ps1 -sCsPath EdSharp.cs   (another project)
#
# Safety: only the VersionString literal changes (the assembly
# attributes reference the const, so they follow); the file is
# rewritten through a temp file moved into place only on success,
# and the original UTF-8 BOM state is preserved.

[CmdletBinding()]
param(
    [ValidateSet('patch','minor','major')] [string] $Part = 'patch',
    [string] $Set = '',
    [string] $sCsPath = ''
)

$ErrorActionPreference = 'Stop'

try
{
    # Resolve the source file. Explicit -sCsPath wins; else DbDo.cs;
    # else the first *.cs here that declares BuildInfo.VersionString.
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

    # Preserve the file's UTF-8 BOM state on rewrite.
    $aBytes  = [System.IO.File]::ReadAllBytes([System.IO.Path]::GetFullPath($sCsPath))
    $bHasBom = ($aBytes.Length -ge 3 -and $aBytes[0] -eq 0xEF -and $aBytes[1] -eq 0xBB -and $aBytes[2] -eq 0xBF)

    $sCs = Get-Content -LiteralPath $sCsPath -Raw -Encoding UTF8
    $oMatch = [regex]::Match($sCs, 'VersionString\s*=\s*"([^"]+)"')
    if (-not $oMatch.Success)
    {
        throw "BuildInfo.VersionString not found in $sCsPath."
    }
    $sOld = $oMatch.Groups[1].Value.Trim()

    if ($Set)
    {
        if ($Set -notmatch '^\d+(\.\d+)*$')
        {
            throw "-Set must be a dotted numeric version like 1.0.123."
        }
        $sNew = $Set.Trim()
    }
    else
    {
        $aParts = @($sOld -split '\.')
        while ($aParts.Count -lt 3) { $aParts += '0' }
        $iIdx = @{ 'major' = 0; 'minor' = 1; 'patch' = 2 }[$Part]
        if ($aParts[$iIdx] -notmatch '^\d+$')
        {
            throw "Version component '$($aParts[$iIdx])' is not numeric; use -Set X.Y.Z."
        }
        $aParts[$iIdx] = [string]([int]$aParts[$iIdx] + 1)
        for ($i = $iIdx + 1; $i -lt $aParts.Count; $i++) { $aParts[$i] = '0' }
        $sNew = ($aParts -join '.')
    }

    if ($sNew -eq $sOld)
    {
        Write-Host "bumpVersion: no change ($sOld) in $sCsPath."
        exit 0
    }

    $sCsUpdated = [regex]::Replace($sCs, '(VersionString\s*=\s*")[^"]+(")', ('${1}' + $sNew + '${2}'), 1)

    $sTmp = $sCsPath + '.tmp'
    $oEnc = New-Object System.Text.UTF8Encoding($bHasBom)
    [System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($sTmp), $sCsUpdated, $oEnc)
    Move-Item -LiteralPath $sTmp -Destination $sCsPath -Force

    Write-Host "bumpVersion: $sOld -> $sNew (in $sCsPath)."
    exit 0
}
catch
{
    Write-Host "bumpVersion: ERROR -- $($_.Exception.Message). Source left unchanged."
    exit 1
}
