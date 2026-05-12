# tagReleases.ps1
# Tag, push, and publish a GitHub Release for DbDuo.
#
# Hardcoded plan:
#   C:\DbDuo   -> tag v1.0.20  -> uploads DbDuo_setup.exe
#
# For DbDuo, the script:
#   1. Confirms the repo path exists and is a git working tree.
#   2. Confirms DbDuo_setup.exe exists in the repo root, and
#      reports its size and modification time to the log.
#   3. Warns if the working tree has uncommitted changes (since the .exe
#      may not match the tagged commit). Bails by default; use
#      -AllowDirty to proceed anyway.
#   4. Creates the tag (or skips if it already exists), then pushes it
#      to origin.
#   5. Creates the GitHub Release with --generate-notes and --latest,
#      attaching the .exe; OR if the release already exists, replaces
#      the asset with --clobber.
#   6. Verifies the public URL with Invoke-WebRequest -Method Head
#      and reports the HTTP status.
#
# Logging:
#   All output is captured to %TEMP%\tagReleases-<stamp>.log via
#   Start-Transcript. The log is always closed cleanly, even on
#   failure, and its path is reported on stdout as the final line.
#
# Requirements:
#   - git in PATH and authenticated for push.
#   - gh in PATH and authenticated.

[CmdletBinding()]
param(
    [switch]$AllowDirty
)

# ============================================================
# Setup
# ============================================================

$ErrorActionPreference = 'Stop'

# Build a timestamped log path in the current directory. This lets
# you cd to the repo before running and find the log right next to
# the source you just tagged, instead of hunting through %TEMP%.
$sStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$sLogPath = Join-Path $PWD.Path "tagReleases-$sStamp.log"

# Start transcript logging. If anything goes wrong launching the
# transcript, we still want a useful error message on the console.
try {
    Start-Transcript -LiteralPath $sLogPath -Force | Out-Null
} catch {
    Write-Host "ERROR: Could not start transcript at $sLogPath" -ForegroundColor Red
    Write-Host $_
    exit 1
}

# Plan as a list of objects. DbDuo is the only entry; other
# Mazrui repos (2htm, urlCheck, extCheck) are handled by a
# separate copy of this script in each of their own repos.
$aReleases = @(
    [pscustomobject]@{
        Name      = 'DbDuo'
        RepoPath  = 'C:\DbDuo'
        Version   = '1.0.20'
        SetupExe  = 'DbDuo_setup.exe'
    }
)

# ============================================================
# Helpers
# ============================================================

function invokeChecked {
    # Run an external program; show its output; if it returns non-zero,
    # throw with a clean message. Local $ErrorActionPreference =
    # 'Continue' prevents the script-level 'Stop' from converting
    # native-command stderr into a terminating exception before we
    # have a chance to inspect $LASTEXITCODE ourselves.
    param(
        [Parameter(Mandatory)] [string]   $sExe,
        [Parameter(Mandatory)] [string[]] $aArgs,
        [string]                          $sLabel = $null
    )
    if (-not $sLabel) { $sLabel = "$sExe $($aArgs -join ' ')" }
    Write-Host "  > $sLabel" -ForegroundColor DarkGray
    $ErrorActionPreference = 'Continue'
    & $sExe @aArgs
    $iCode = $LASTEXITCODE
    if ($iCode -ne 0) {
        throw "Command failed (exit $iCode): $sLabel"
    }
}

function invokeSilently {
    # Run an external program; capture combined stdout+stderr; if
    # non-zero, surface that captured output then throw. Same local
    # $ErrorActionPreference reset as invokeChecked: lets us reach
    # our own throw with a clean message rather than have PowerShell
    # short-circuit on native-command-error first.
    param(
        [Parameter(Mandatory)] [string]   $sExe,
        [Parameter(Mandatory)] [string[]] $aArgs,
        [string]                          $sLabel = $null
    )
    if (-not $sLabel) { $sLabel = "$sExe $($aArgs -join ' ')" }
    $ErrorActionPreference = 'Continue'
    $sOut = & $sExe @aArgs 2>&1 | Out-String
    $iCode = $LASTEXITCODE
    if ($iCode -ne 0) {
        Write-Host $sOut.TrimEnd() -ForegroundColor DarkGray
        throw "Command failed (exit $iCode): $sLabel"
    }
    return $sOut
}

function tryInvoke {
    # Run an external program; return the exit code, never throw.
    # Stderr from the program is intentionally swallowed (along with
    # stdout) since callers of this helper use it to ASK whether
    # something exists -- a non-zero exit with a "fatal:" stderr
    # message is the expected "no" answer, not an error condition.
    #
    # Local $ErrorActionPreference = 'Continue' is essential here:
    # the script-level setting of 'Stop' would otherwise convert the
    # native command's stderr stream into a terminating exception
    # via PowerShell's NativeCommandError handling, defeating the
    # whole purpose of this helper.
    param(
        [Parameter(Mandatory)] [string]   $sExe,
        [Parameter(Mandatory)] [string[]] $aArgs
    )
    $ErrorActionPreference = 'Continue'
    & $sExe @aArgs 2>$null | Out-Null
    return $LASTEXITCODE
}


function publishOne {
    param(
        [Parameter(Mandatory)] [pscustomobject] $oRelease
    )

    $sName     = $oRelease.Name
    $sRepoPath = $oRelease.RepoPath
    $sVersion  = $oRelease.Version
    $sSetupExe = $oRelease.SetupExe
    $sTag      = "v$sVersion"

    Write-Host ""
    Write-Host "=== $sName  ($sTag) ===" -ForegroundColor Cyan

    # 1. Confirm repo path exists.
    if (-not (Test-Path -LiteralPath $sRepoPath -PathType Container)) {
        throw "Repo path does not exist: $sRepoPath"
    }
    Write-Host "Repo path: $sRepoPath"

    Push-Location $sRepoPath
    try {
        # Confirm it's a git working tree.
        $iCode = tryInvoke -sExe 'git' -aArgs @('rev-parse', '--is-inside-work-tree')
        if ($iCode -ne 0) {
            throw "$sRepoPath is not a git working tree."
        }

        # 2. Confirm the .exe is present and report its details.
        if (-not (Test-Path -LiteralPath $sSetupExe -PathType Leaf)) {
            throw "$sSetupExe not found in $sRepoPath. Build it first (run buildDbDuo.cmd, then compile DbDuo_setup.iss with Inno Setup), then re-run."
        }
        $oExe = Get-Item -LiteralPath $sSetupExe
        Write-Host ("Asset:     {0}" -f $oExe.Name)
        Write-Host ("  size:    {0:N0} bytes" -f $oExe.Length)
        Write-Host ("  mtime:   {0}" -f $oExe.LastWriteTime)

        # 3. Check for uncommitted changes.
        $sStatus = & git status --porcelain 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) {
            throw "git status failed."
        }
        $sStatus = $sStatus.TrimEnd()
        if ($sStatus) {
            if ($AllowDirty) {
                Write-Host "WARN: working tree has uncommitted changes. Proceeding anyway (-AllowDirty)." -ForegroundColor Yellow
                Write-Host $sStatus -ForegroundColor Yellow
            } else {
                Write-Host "Uncommitted changes detected:" -ForegroundColor Yellow
                Write-Host $sStatus -ForegroundColor Yellow
                throw "Working tree at $sRepoPath has uncommitted changes. Either commit them (so the tag matches the .exe) or re-run with -AllowDirty."
            }
        } else {
            Write-Host "Working tree is clean."
        }

        # 4. Create or confirm tag, then push.
        $iCode = tryInvoke -sExe 'git' -aArgs @('rev-parse', $sTag)
        if ($iCode -ne 0) {
            Write-Host "Creating tag $sTag ..."
            invokeChecked -sExe 'git' -aArgs @('tag', '-a', $sTag, '-m', "$sName $sVersion")
            Write-Host "Pushing tag $sTag to origin ..."
            invokeChecked -sExe 'git' -aArgs @('push', 'origin', $sTag)
        } else {
            Write-Host "Tag $sTag already exists locally. Ensuring it is pushed to origin ..."
            # Push, but don't error if already up to date. Wrap the
            # invocation in a try/catch so that if PowerShell turns
            # git's stderr into a NativeCommandError under the
            # script-level $ErrorActionPreference='Stop', we still
            # proceed.
            try {
                $ErrorActionPreference = 'Continue'
                & git push origin $sTag 2>$null | Out-Null
            } catch {
                # Already-up-to-date, etc. Acceptable.
            }
        }

        # 5. Create or update the GitHub Release.
        $iCode = tryInvoke -sExe 'gh' -aArgs @('release', 'view', $sTag)
        if ($iCode -ne 0) {
            Write-Host "Creating release $sTag with asset $sSetupExe ..."
            invokeChecked -sExe 'gh' -aArgs @(
                'release', 'create', $sTag, $sSetupExe,
                '--title', "$sName $sVersion",
                '--generate-notes',
                '--latest'
            )
        } else {
            Write-Host "Release $sTag already exists. Replacing asset $sSetupExe ..."
            invokeChecked -sExe 'gh' -aArgs @(
                'release', 'upload', $sTag, $sSetupExe,
                '--clobber'
            )
        }

        # 6. Verify the public URL.
        $sUrl = "https://github.com/JamalMazrui/$sName/releases/latest/download/$sSetupExe"
        Write-Host "Public URL: $sUrl"
        try {
            $oResponse = Invoke-WebRequest -Uri $sUrl -Method Head -MaximumRedirection 5 -UseBasicParsing -ErrorAction Stop
            Write-Host ("URL check: HTTP {0}" -f $oResponse.StatusCode) -ForegroundColor Green
        } catch {
            # Some GitHub asset endpoints return errors on HEAD even though
            # GET would work. Try GET if HEAD fails.
            try {
                $oResponse = Invoke-WebRequest -Uri $sUrl -Method Get -MaximumRedirection 5 -UseBasicParsing -ErrorAction Stop
                Write-Host ("URL check: HTTP {0} (via GET; HEAD was rejected)" -f $oResponse.StatusCode) -ForegroundColor Green
            } catch {
                Write-Host "URL check failed: $($_.Exception.Message)" -ForegroundColor Yellow
                Write-Host "(The release may still be valid; GitHub's CDN can take a few seconds to propagate.)" -ForegroundColor Yellow
            }
        }

    } finally {
        Pop-Location
    }
}


# ============================================================
# Main
# ============================================================

$iExitCode = 0

try {
    Write-Host "=== tagReleases.ps1 ==="
    Write-Host "Started: $(Get-Date)"
    Write-Host "Log:     $sLogPath"
    if ($AllowDirty) {
        Write-Host "Mode:    -AllowDirty (will not bail on uncommitted changes)"
    }
    Write-Host ""

    # Tool checks.
    Write-Host "--- Tool checks ---"

    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        throw "git is not in PATH."
    }
    Write-Host "git: $(& git --version | Select-Object -First 1)"

    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        throw "gh is not in PATH. Install GitHub CLI from https://cli.github.com/ and run: gh auth login"
    }
    $sGhVer = (& gh --version 2>&1 | Out-String).Trim()
    # gh --version emits multiple lines; show only the first one in the log header.
    $sGhFirstLine = ($sGhVer -split "`n")[0].Trim()
    Write-Host "gh:  $sGhFirstLine"

    Write-Host ""
    Write-Host "Checking gh authentication..."
    $iAuthCode = tryInvoke -sExe 'gh' -aArgs @('auth', 'status')
    if ($iAuthCode -ne 0) {
        # Re-run for user-visible output.
        & gh auth status
        throw "gh is not authenticated. Run: gh auth login --web --git-protocol https"
    }
    Write-Host "gh authentication: OK"

    # Process each release.
    foreach ($oRelease in $aReleases) {
        publishOne -oRelease $oRelease
    }

    Write-Host ""
    Write-Host "=== Release published. ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Stable, public download URL (points to the latest release;"
    Write-Host "this URL does not change as you publish future versions):"
    Write-Host ""
    foreach ($oRelease in $aReleases) {
        Write-Host ("  https://github.com/JamalMazrui/{0}/releases/latest/download/{1}" -f $oRelease.Name, $oRelease.SetupExe)
    }
    Write-Host ""

} catch {
    $iExitCode = 1
    Write-Host ""
    Write-Host "=== FAILED ===" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ScriptStackTrace) {
        Write-Host ""
        Write-Host "Stack trace:" -ForegroundColor DarkGray
        Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
    }
} finally {
    Write-Host ""
    Write-Host "--- Log saved at: $sLogPath ---"
    if ($iExitCode -ne 0) {
        Write-Host "--- Exit code: $iExitCode ---"
    }
    try { Stop-Transcript | Out-Null } catch { }
}

exit $iExitCode
