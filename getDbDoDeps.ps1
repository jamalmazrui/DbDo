# getDbDoDeps.ps1 -- fetch the managed DLLs DbDo's .xlsx engine needs
# (NPOI 2.5.6, SharpZipLib 1.3.3, Portable.BouncyCastle 1.8.9) into this
# script's own folder. Idempotent: any DLL already present is left alone.
# Exits 0 on success, 1 (with guidance) on failure. buildDbDo.cmd calls it
# before compiling; it can also be run by hand:  powershell -ExecutionPolicy
# Bypass -File getDbDoDeps.ps1
#
# nuget.org requires TLS 1.2; Windows PowerShell 5.1 does not always enable
# it by default, which is the usual reason a bare Invoke-WebRequest fails.
$ErrorActionPreference = 'Stop'
try { [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12 } catch { }

$dest = $PSScriptRoot
if (-not $dest) { $dest = (Get-Location).Path }

# Each package: its direct nuget flat-container .nupkg URL and the DLLs to
# lift out of it. A .nupkg is a .zip; the DLLs live under lib\<tfm>\.
$packages = @(
  @{ Name = 'NPOI 2.5.6';
     Url  = 'https://api.nuget.org/v3-flatcontainer/npoi/2.5.6/npoi.2.5.6.nupkg';
     Files = @('NPOI.dll','NPOI.OOXML.dll','NPOI.OpenXml4Net.dll','NPOI.OpenXmlFormats.dll') },
  @{ Name = 'SharpZipLib 1.3.3';
     Url  = 'https://api.nuget.org/v3-flatcontainer/sharpziplib/1.3.3/sharpziplib.1.3.3.nupkg';
     Files = @('ICSharpCode.SharpZipLib.dll') },
  @{ Name = 'Portable.BouncyCastle 1.8.9';
     Url  = 'https://api.nuget.org/v3-flatcontainer/portable.bouncycastle/1.8.9/portable.bouncycastle.1.8.9.nupkg';
     Files = @('BouncyCastle.Crypto.dll') }
)

# Target-framework lib folders to try, best-for-.NET-4.8 first. net40 is
# included because Portable.BouncyCastle ships its DLL under lib\net40.
$tfms = @('net48','net472','net47','net462','net461','net46','net452','net451','net45','net40','netstandard2.1','netstandard2.0')

Add-Type -AssemblyName System.IO.Compression.FileSystem | Out-Null

function Test-AllPresent($files) {
  foreach ($f in $files) { if (-not (Test-Path (Join-Path $dest $f))) { return $false } }
  return $true
}

foreach ($pkg in $packages) {
  if (Test-AllPresent $pkg.Files) { Write-Host ("Already present: " + $pkg.Name); continue }

  $stem   = [IO.Path]::GetFileNameWithoutExtension($pkg.Url)
  $tmpZip = Join-Path $env:TEMP ($stem + '.zip')
  $tmpDir = Join-Path $env:TEMP ('dbdo_' + $stem)
  if (Test-Path $tmpZip) { Remove-Item -Force $tmpZip }
  if (Test-Path $tmpDir) { Remove-Item -Recurse -Force $tmpDir }

  Write-Host ("Fetching " + $pkg.Name + " ...")
  Invoke-WebRequest -Uri $pkg.Url -OutFile $tmpZip -UseBasicParsing
  [IO.Compression.ZipFile]::ExtractToDirectory($tmpZip, $tmpDir)

  foreach ($f in $pkg.Files) {
    $target = Join-Path $dest $f
    if (Test-Path $target) { continue }
    $hit = $null
    foreach ($tf in $tfms) {
      $cand = [IO.Path]::Combine($tmpDir, 'lib', $tf, $f)
      if (Test-Path $cand) { $hit = $cand; break }
    }
    if (-not $hit) {
      $hit = Get-ChildItem -Path $tmpDir -Recurse -Filter $f | Select-Object -First 1 -ExpandProperty FullName
    }
    if (-not $hit) { throw ("Could not find " + $f + " inside " + $pkg.Url) }
    Copy-Item -Path $hit -Destination $target -Force
    Write-Host ("  -> " + $f)
  }

  Remove-Item -Force $tmpZip
  Remove-Item -Recurse -Force $tmpDir
}

# Final verification across every required DLL.
$required = @('NPOI.dll','NPOI.OOXML.dll','NPOI.OpenXml4Net.dll','NPOI.OpenXmlFormats.dll','ICSharpCode.SharpZipLib.dll','BouncyCastle.Crypto.dll')
$missing = @()
foreach ($f in $required) { if (-not (Test-Path (Join-Path $dest $f))) { $missing += $f } }
if ($missing.Count -gt 0) {
  Write-Error ("Missing after fetch: " + ($missing -join ', '))
  exit 1
}
Write-Host "All NPOI .xlsx-engine dependencies are present."
exit 0
