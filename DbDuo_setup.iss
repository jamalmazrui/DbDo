; =====================================================================
; DbDuo installer script for Inno Setup 6.x
;
; Compile with Inno Setup IDE (ISCC.exe) to produce DbDuo_setup.exe.
;
; Design notes (May 2026 revision):
;   - Minimal wizard: Welcome, Select Destination, Ready, Installing,
;     Finish. No extra License page (MIT summary is in the Welcome
;     text). No extra Prerequisites page; driver probes and silent
;     installs happen inside the Installing step.
;   - 64-bit Windows 10 (or later) only. Requires admin.
;   - Driver strategy:
;       Microsoft Access Database Engine (ACE):
;         1) winget install Microsoft.AccessDatabaseEngine.2016
;         2) direct download from Microsoft's CDN, run /passive
;         (Chocolatey is not assumed; absent on most fresh systems.)
;       SQLite ODBC Driver:
;         Only choice: direct download from Christian Werner's site
;   - The probes test the registry for existing driver presence;
;     present drivers are left alone (no reinstall, no prompt).
;
; Pascal Script uses (* ... *) comments because brace comments
; conflict with InnoSetup constants like {tmp}, {app}.
; =====================================================================

#define AppName       "DbDuo"
#define AppVersion    "1.0.51"
#define AppPublisher  "Jamal Mazrui"
#define AppUrl        "https://github.com/JamalMazrui/DbDuo"
#define AppExeName    "DbDuo.exe"
#define AppCopyright  "Copyright (c) 2026 Jamal Mazrui. MIT License."

; HotKey is the Inno Setup HotKey: directive value (Ctrl syntax
; required by Inno Setup). HotKeyDisplay is the same key in DbDuo's
; user-facing notation (Control instead of Ctrl, alpha-ordered).
; Only the Desktop "activate" shortcut is hotkey-bound. The Start
; Menu "GUI only" and "CLI only" shortcuts are deliberately NOT
; hotkey-bound so the global keyboard hotkey space has just one
; DbDuo entry: Alt+Control+D, which performs a single-instance
; foreground handoff (see [Icons] below).
#define HotKey        "Alt+Ctrl+D"
#define HotKeyDisplay "Alt+Control+D"

; Direct download URLs. SQLite ODBC: the publisher republishes the
; same filename on each underlying SQLite-version bump, so we do not
; pin a hash. ACE: pinned to the known x64 build on Microsoft's CDN.
#define SqliteOdbcUrl  "http://www.ch-werner.de/sqliteodbc/sqliteodbc_w64.exe"
#define SqliteOdbcHash ""
#define AceUrl         "https://download.microsoft.com/download/3/5/C/35C84C36-661A-44E6-9324-8786B8DBE231/accessdatabaseengine_X64.exe"
#define AceHash        "04e96c9f1a1f7d251a88aececf1dc10ff65950392787427c00814a43308003de"

[Setup]
AppId={{F8E2A1C4-9D3B-4E5F-A7B6-1234567890AB}

AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}
AppUpdatesURL={#AppUrl}/releases
AppCopyright={#AppCopyright}
VersionInfoVersion={#AppVersion}

DefaultDirName={autopf}\{#AppName}
; No Start Menu folder is created -- the installer's [Icons] section
; declares only the single desktop shortcut with Alt+Control+D as
; its hotkey. DefaultGroupName is not set because no {group}\ items
; exist. DisableProgramGroupPage=yes hides the (now-unused) "select
; Start Menu folder" page in the install wizard.
DisableProgramGroupPage=yes
UsePreviousAppDir=yes
DisableDirPage=no

OutputDir=.
OutputBaseFilename={#AppName}_setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

Uninstallable=yes
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName} {#AppVersion}

; The installer EXE itself uses DbDuo.ico, so DbDuo_setup.exe and the
; uninstaller and any Start Menu entries all carry the same icon.
SetupIconFile=DbDuo.ico

MinVersion=10.0

; Enable .zip archive extraction support. The "full" method allows
; the [Files] section extractarchive flag and the Pascal Scripting
; ExtractArchive() function to handle .zip files (not just .7z, the
; default supported format). Inno Setup 6.4+ ships the necessary
; is7z.dll internally; nothing external needed. We use this to
; bundle DbDuo's JAWS files (DbDuo.jkm, DbDuo.jss) inside
; DbDuo_JAWS.zip rather than tracking them as two loose files in
; the GitHub repo.
ArchiveExtraction=full

; CloseApplications + RestartApplications enables Inno Setup's
; running-app detection. When the user runs DbDuo_setup.exe via
; the Elevate-Version (F11) command while DbDuo is already running,
; the installer detects the process by AppMutex name and offers a
; "close the running DbDuo" dialog before continuing. Setting
; RestartApplications=yes also re-launches DbDuo at the end if it
; was closed during setup, so the user returns to a running app.
; AppMutex must match a mutex DbDuo creates at startup (see
; DbDuoForm.cs single-instance handoff).
CloseApplications=yes
RestartApplications=yes
CloseApplicationsFilter=DbDuo.exe
AppMutex=Local\DbDuo.SingleInstance

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
; Welcome page: brief product description plus an MIT license note
; (so we do not need a separate License page). The driver-install
; behavior is mentioned so the user knows what to expect during
; the Installing step.
WelcomeLabel2=This will install [name/ver] on your computer.%n%n[name] is an accessible, keyboard-first database manager for Windows. It opens SQLite, Microsoft Access, Excel, dBASE, and delimited-text files, with first-class support for JAWS, NVDA, and Narrator.%n%n[name] is licensed under the MIT License: free to use, copy, modify, and distribute; provided "as is" with no warranty. The full license text is installed as License.htm.%n%nSetup will silently install the SQLite ODBC driver and the Microsoft Access Database Engine if either is missing; existing drivers are left alone.%n%nIt is recommended that you close all other applications before continuing.

[InstallDelete]
; Cleanup on upgrade. Several files from earlier DbDuo releases are
; no longer referenced and should be removed so the install folder
; reflects only files DbDuo actually uses.
;
; v1.0.40-v1.0.42: NVDA controller-client DLL had a legacy 64-suffixed
; name. v1.0.43+ uses the modern unsuffixed name.
Type: files; Name: "{app}\nvdaControllerClient64.dll"
;
; v1.0.42-v1.0.43 bundled the Roslyn C# scripting assemblies; v1.0.44
; rolled that back in favor of JScript .NET (no shipped runtime DLLs).
; The 12 assemblies below plus DbDuo.exe.config and the App.config
; binding-redirect file are all obsolete from v1.0.44 onward.
Type: files; Name: "{app}\DbDuo.exe.config"
Type: files; Name: "{app}\App.config"
Type: files; Name: "{app}\Microsoft.CodeAnalysis.dll"
Type: files; Name: "{app}\Microsoft.CodeAnalysis.CSharp.dll"
Type: files; Name: "{app}\Microsoft.CodeAnalysis.Scripting.dll"
Type: files; Name: "{app}\Microsoft.CodeAnalysis.CSharp.Scripting.dll"
Type: files; Name: "{app}\System.Collections.Immutable.dll"
Type: files; Name: "{app}\System.Reflection.Metadata.dll"
Type: files; Name: "{app}\System.Memory.dll"
Type: files; Name: "{app}\System.Buffers.dll"
Type: files; Name: "{app}\System.Runtime.CompilerServices.Unsafe.dll"
Type: files; Name: "{app}\System.Numerics.Vectors.dll"
Type: files; Name: "{app}\System.Threading.Tasks.Extensions.dll"
Type: files; Name: "{app}\System.Text.Encoding.CodePages.dll"
Type: files; Name: "{app}\Microsoft.CSharp.dll"

[Files]
Source: "DbDuo.exe";    DestDir: "{app}"; Flags: ignoreversion
Source: "dbDuoEval.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "DbDuo.ico";    DestDir: "{app}"; Flags: ignoreversion
Source: "DbDuo.md";     DestDir: "{app}"; Flags: ignoreversion
Source: "DbDuo.htm";    DestDir: "{app}"; Flags: ignoreversion
Source: "README.md";    DestDir: "{app}"; Flags: ignoreversion
Source: "README.htm";   DestDir: "{app}"; Flags: ignoreversion
Source: "License.md";   DestDir: "{app}"; Flags: ignoreversion
Source: "License.htm";  DestDir: "{app}"; Flags: ignoreversion
Source: "sample.db";    DestDir: "{app}"; Flags: ignoreversion
Source: "DbDuo.ini";    DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist
;
; DbDuo_JAWS.zip contains DbDuo.jkm and DbDuo.jss at its root. We
; ship the zip rather than the two loose files so the GitHub repo
; stays uncluttered. The zip is extracted into {app} at install
; time by the Pascal procedure ExtractJawsArchive (see [Code]
; below), called from CurStepChanged(ssPostInstall) BEFORE the
; JAWS settings installer runs.
Source: "DbDuo_JAWS.zip"; DestDir: "{app}"; Flags: ignoreversion
Source: "DbDuo.nvda-addon"; DestDir: "{app}"; Flags: ignoreversion
Source: "nvdaControllerClient.dll"; DestDir: "{app}"; Flags: ignoreversion

; (No [Tasks] section. The JAWS settings install is exposed as a
; checkbox on the Finish page via [Run] above, delegating the work
; to DbDuo.exe --install-jaws-settings rather than duplicating the
; install logic in Pascal Script.)

[Icons]
; Single shortcut policy: the only DbDuo shortcut the installer
; creates is the desktop one with the Alt+Control+D hotkey. The
; -activate parameter performs a single-instance handoff: the
; first press of Alt+Control+D launches DbDuo; subsequent presses
; bring the existing instance to the foreground rather than
; launching a duplicate. No Start Menu folder, no GUI-only or
; CLI-only or read-only variants -- the user reaches those modes
; from inside DbDuo, not from external launchers.
Name: "{autodesktop}\{#AppName}"; \
  Filename: "{app}\{#AppExeName}"; \
  WorkingDir: "{app}"; \
  Parameters: "-activate"; \
  HotKey: {#HotKey}; \
  Comment: "Dual-mode database manager ({#HotKeyDisplay})"

[Run]
; The four entries below appear as checkboxes on the installer's
; Finish page in this exact order. Entry 1 delegates to
; DbDuo.exe's --install-jaws-settings mode; the C# implementation
; lives in DbDuo.cs (class JawsSettingsInstaller) so the same
; logic can be re-run later from the Help menu. Entry 2 hands
; DbDuo.nvda-addon to its Windows file association via Inno
; Setup's shellexec flag so NVDA shows its native add-on install
; dialog directly (see the comment block on entry 2 for why this
; replaced the v1.0.45-v1.0.48 approach of going through
; DbDuo.exe --install-nvda-addon).
;
; 1. Install JAWS settings (checked by default).
FileName: "{app}\{#AppExeName}"; \
  Parameters: "--install-jaws-settings"; \
  WorkingDir: "{app}"; \
  Description: "Install JAWS settings for {#AppName} (recommended if you use JAWS)"; \
  Flags: postinstall waituntilterminated runhidden skipifsilent

; 2. Install NVDA add-on (checked by default).
;
; This entry uses Inno Setup's shellexec flag to hand the
; .nvda-addon file directly to its Windows file association,
; rather than going through DbDuo.exe --install-nvda-addon as
; v1.0.45-v1.0.48 did. The CLI flag in DbDuo.exe is still
; supported (it's how the Help menu's Re-install NVDA Add-on
; command works), but routing through it at install time was
; subtly broken: DbDuo.exe called Process.Start with
; UseShellExecute=true and then exited immediately with code 0,
; satisfying Inno Setup's "waituntilterminated" wait before the
; shell-execute resolution had completed. NVDA's add-on install
; dialog therefore never appeared even when NVDA was running and
; correctly registered as the .nvda-addon handler.
;
; Using shellexec directly here lets Inno Setup do the shell-
; execute itself, which is what shellexec is designed for: it
; uses ShellExecuteEx with proper handle tracking. NVDA opens its
; standard "Install this add-on?" dialog, the user confirms or
; cancels, NVDA finishes, and only then does Inno Setup move on.
;
; If NVDA is not installed (no .nvda-addon file association),
; Windows shows its "How do you want to open this file?" picker.
; That is acceptable: the user can dismiss it (NVDA is not in
; use anyway) or pick an app. The Description below mentions
; the "recommended if you use NVDA" condition.
FileName: "{app}\DbDuo.nvda-addon"; \
  WorkingDir: "{app}"; \
  Description: "Install NVDA add-on for {#AppName} (recommended if you use NVDA)"; \
  Flags: postinstall shellexec waituntilterminated skipifsilent

; 3. Launch DbDuo (checked by default).
FileName: "{app}\{#AppExeName}"; \
  WorkingDir: "{app}"; \
  Description: "Launch {#AppName} now (or use the desktop hotkey {#HotKeyDisplay} anytime)"; \
  Flags: nowait postinstall skipifsilent

; 4. Open README (checked by default).
FileName: "{app}\README.htm"; \
  Description: "Read the {#AppName} README"; \
  Flags: postinstall shellexec skipifsilent

[UninstallRun]
; Symmetric to the JAWS-install [Run] entry above. Removes only
; the files DbDuo placed in the JAWS settings folders, tracked
; via the install-time log at %APPDATA%\DbDuo\jawsSettings.log.
; runhidden so no console window flashes during uninstall; ignored
; if DbDuo.exe is already deleted (the install probably failed).
FileName: "{app}\{#AppExeName}"; \
  Parameters: "--uninstall-jaws-settings"; \
  WorkingDir: "{app}"; \
  Flags: runhidden waituntilterminated skipifdoesntexist

[Code]
(* --------------------------------------------------------------------
   Pascal Script: silent driver install during the Installing step.

   No extra wizard page. The user accepts the install via the
   standard Ready / Installing flow; if the SQLite ODBC driver or the
   Microsoft Access Database Engine is missing, Setup downloads and
   installs it silently as part of the same Installing step. Existing
   drivers are left alone.

   ACE preference order:
     1) winget install Microsoft.AccessDatabaseEngine.2016
     2) direct download accessdatabaseengine_X64.exe /passive
   Chocolatey is not assumed and not probed.

   SQLite ODBC: only choice is direct download from publisher.
   -------------------------------------------------------------------- *)

var
  bWingetAvailable: Boolean;
  sWingetExe: string;

  bInstallSqliteOdbc: Boolean;
  bInstallAce: Boolean;
  oDownloadPage: TDownloadWizardPage;

(* --- Driver-presence probes --- *)

function NeedsSqliteOdbc: Boolean;
var
  sValue: string;
begin
  Result := not RegQueryStringValue(HKLM64,
    'SOFTWARE\ODBC\ODBCINST.INI\ODBC Drivers',
    'SQLite3 ODBC Driver', sValue);
end;

function NeedsAceProvider: Boolean;
var
  sValue: string;
begin
  if RegQueryStringValue(HKCR, 'Microsoft.ACE.OLEDB.16.0\CLSID',
    '', sValue) then begin Result := False; exit; end;
  if RegQueryStringValue(HKCR, 'Microsoft.ACE.OLEDB.12.0\CLSID',
    '', sValue) then begin Result := False; exit; end;
  Result := True;
end;

(* --- Package-manager probe (winget only) --- *)

function ProbeWinget: Boolean;
var
  iResultCode: Integer;
begin
  sWingetExe := '';
  if Exec('winget.exe', '--version', '', SW_HIDE,
    ewWaitUntilTerminated, iResultCode) and (iResultCode = 0) then
  begin
    sWingetExe := 'winget.exe';
    Result := True;
    exit;
  end;
  Result := False;
end;

procedure InitializeWizard;
begin
  bWingetAvailable := ProbeWinget;
  bInstallSqliteOdbc := NeedsSqliteOdbc;
  bInstallAce := NeedsAceProvider;
  Log('Driver probe: needs SQLite ODBC=' + IntToStr(Ord(bInstallSqliteOdbc))
    + ' needs ACE=' + IntToStr(Ord(bInstallAce))
    + ' winget=' + IntToStr(Ord(bWingetAvailable)));

  (* Pre-create a download page; we may use it during installation
     to fetch driver installers. Hidden if no downloads are needed. *)
  oDownloadPage := CreateDownloadPage(
    'Downloading database drivers',
    'Setup is fetching one or more drivers DbDuo needs to read database files.',
    nil);
end;

(* --- ACE install: winget first, then direct download --- *)

function InstallAceViaWinget: Boolean;
var
  iResultCode: Integer;
begin
  Log('Installing ACE via winget...');
  Result := Exec(sWingetExe,
    'install --id Microsoft.AccessDatabaseEngine.2016 --silent'
    + ' --accept-package-agreements --accept-source-agreements'
    + ' --disable-interactivity',
    '', SW_HIDE, ewWaitUntilTerminated, iResultCode);
  Log('winget exit code: ' + IntToStr(iResultCode));
  Result := Result and (iResultCode = 0);
end;

function InstallAceViaDownload: Boolean;
var
  sExe: string;
  iResultCode: Integer;
begin
  sExe := ExpandConstant('{tmp}\accessdatabaseengine_X64.exe');
  Result := False;
  if not FileExists(sExe) then
  begin
    Log('ACE installer not found at expected path: ' + sExe);
    exit;
  end;
  Log('Installing ACE via direct download: ' + sExe);
  (* /passive bypasses the Office-architecture conflict check that
     /quiet alone trips when 32-bit Office is installed. *)
  Result := Exec(sExe, '/passive', '', SW_HIDE,
    ewWaitUntilTerminated, iResultCode);
  Log('ACE direct-install exit code: ' + IntToStr(iResultCode));
  Result := Result and (iResultCode = 0);
end;

function InstallAce: Boolean;
begin
  Result := False;
  if bWingetAvailable then
  begin
    Result := InstallAceViaWinget;
    if Result then exit;
    Log('winget install of ACE failed; trying direct-download fallback.');
  end;
  Result := InstallAceViaDownload;
end;

(* --- SQLite ODBC install: direct download only --- *)

function InstallSqliteOdbc: Boolean;
var
  sExe: string;
  iResultCode: Integer;
begin
  sExe := ExpandConstant('{tmp}\sqliteodbc_w64.exe');
  Result := False;
  if not FileExists(sExe) then
  begin
    Log('SQLite ODBC installer not found at expected path: ' + sExe);
    exit;
  end;
  Log('Installing SQLite ODBC: ' + sExe);
  (* /S is the NSIS silent flag (Christian Werner's installer is NSIS). *)
  Result := Exec(sExe, '/S', '', SW_HIDE,
    ewWaitUntilTerminated, iResultCode);
  Log('SQLite ODBC install exit code: ' + IntToStr(iResultCode));
  Result := Result and (iResultCode = 0);
end;

(* --- Driver downloads, triggered during installation --- *)

function DownloadMissingDrivers: Boolean;
var
  bNeedDownload: Boolean;
begin
  Result := True;
  oDownloadPage.Clear;
  bNeedDownload := False;

  if bInstallSqliteOdbc then
  begin
    oDownloadPage.Add('{#SqliteOdbcUrl}', 'sqliteodbc_w64.exe', '{#SqliteOdbcHash}');
    bNeedDownload := True;
  end;

  (* For ACE: only download if winget is unavailable. Otherwise
     winget will fetch the installer from Microsoft's source itself. *)
  if bInstallAce and (not bWingetAvailable) then
  begin
    oDownloadPage.Add('{#AceUrl}', 'accessdatabaseengine_X64.exe', '{#AceHash}');
    bNeedDownload := True;
  end;

  if not bNeedDownload then exit;

  oDownloadPage.Show;
  try
    try
      oDownloadPage.Download;
    except
      if oDownloadPage.AbortedByUser then
      begin
        Log('Driver download aborted by user.');
        Result := False;
      end
      else
      begin
        SuppressibleMsgBox(
          'Failed to download one or more drivers:'#13#10#13#10
          + GetExceptionMessage + #13#10#13#10
          + 'Setup will continue. DbDuo will install but some database '
          + 'formats may not open until the missing drivers are added '
          + 'manually. Re-run this installer with a network connection, '
          + 'or download the drivers from the URLs in DbDuo.md.',
          mbInformation, MB_OK, IDOK);
        Result := True;
      end;
    end;
  finally
    oDownloadPage.Hide;
  end;
end;

(* JAWS settings install (moved from Pascal to C# in v1.0.40):
   The work that v1.0.39's InstallJawsSettings + FindScompilePath +
   CurUninstallStepChanged Pascal procedures did is now in
   DbDuo.cs class JawsSettingsInstaller, invoked via
       DbDuo.exe --install-jaws-settings
   from the [Run] section above, and via
       DbDuo.exe --uninstall-jaws-settings
   from the [UninstallRun] section below. The C# implementation
   uses the same algorithm (registry-first scompile lookup with
   Program Files fallback, enumerate %APPDATA%\Freedom Scientific\
   JAWS\*\Settings\*, copy + scompile, log the paths placed) and
   the same uninstall log location ({userappdata}\DbDuo\
   jawsSettings.log) so the upgrade from v1.0.39 to v1.0.40 is
   transparent. The advantage of the move: the user can re-run the
   install later from Help > Install JAWS Settings without re-
   running the full installer. *)

(* --- Post-install hook: extract JAWS archive, run driver installers --- *)

(* Extract DbDuo_JAWS.zip into {app}. The zip contains DbDuo.jkm
   and DbDuo.jss at its root; after extraction these sit alongside
   DbDuo.exe just as they did when shipped as loose files in
   v1.0.49 and earlier. DbDuo.exe --install-jaws-settings (run as
   the Finish-page checkbox) then reads them from {app} and copies
   them into each per-version JAWS settings folder.

   Requires the [Setup] directive ArchiveExtraction=full so Inno
   Setup's bundled is7z.dll can handle the .zip format (the default
   "basic" extraction only supports .7z). Uses Inno Setup's built-
   in Pascal ExtractArchive function (Inno Setup 6.4.0+). No
   external unzip tool is shipped with DbDuo.

   On failure we surface a non-fatal SuppressibleMsgBox: DbDuo
   still works for non-JAWS users even without these files.
*)
procedure ExtractJawsArchive;
var
  sZipPath: String;
  sDestDir: String;
begin
  sZipPath := ExpandConstant('{app}\DbDuo_JAWS.zip');
  sDestDir := ExpandConstant('{app}');
  if not FileExists(sZipPath) then
  begin
    SuppressibleMsgBox(
      'DbDuo_JAWS.zip was not found in the installation folder.'#13#10#13#10
      + 'JAWS keymap and script files will not be available. If you'#13#10
      + 'use JAWS, re-run the installer.'#13#10#13#10
      + 'DbDuo itself is fine and will run.',
      mbInformation, MB_OK, IDOK);
    exit;
  end;
  try
    (* FullPaths=False: the zip is flat (no subdirectories), so
       this is the same as True for our case, but False is more
       defensive against future zip layouts. Empty Password and
       nil OnExtractionProgress: no password, no progress UI. *)
    ExtractArchive(sZipPath, sDestDir, '', False, nil);
  except
    SuppressibleMsgBox(
      'Failed to extract DbDuo_JAWS.zip:'#13#10#13#10
      + GetExceptionMessage + #13#10#13#10
      + 'JAWS keymap and script files will not be available.'#13#10
      + 'DbDuo itself is fine and will run.',
      mbInformation, MB_OK, IDOK);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    (* Download any missing driver installers before the main copy. *)
    DownloadMissingDrivers;
    exit;
  end;

  if CurStep <> ssPostInstall then exit;

  (* Extract JAWS keymap and script files BEFORE the Finish-page
     Run-section entry invokes DbDuo.exe --install-jaws-settings;
     that command needs DbDuo.jkm and DbDuo.jss in {app}. *)
  ExtractJawsArchive;

  if bInstallSqliteOdbc then
  begin
    if not InstallSqliteOdbc then
      SuppressibleMsgBox(
        'The SQLite ODBC driver did not install cleanly.'#13#10#13#10
        + 'You can install it later by downloading from:'#13#10
        + '  http://www.ch-werner.de/sqliteodbc/'#13#10#13#10
        + 'DbDuo is still installed and will work for other formats.',
        mbInformation, MB_OK, IDOK);
  end;

  if bInstallAce then
  begin
    if not InstallAce then
      SuppressibleMsgBox(
        'The Microsoft Access Database Engine did not install cleanly.'#13#10#13#10
        + 'This often happens on machines with 32-bit Office installed; '
        + 'in that case run the following from an admin command prompt:'#13#10
        + '  winget install Microsoft.AccessDatabaseEngine.2016 --silent'#13#10#13#10
        + 'or download accessdatabaseengine_X64.exe from Microsoft and run '
        + 'it with the /passive flag.'#13#10#13#10
        + 'DbDuo is still installed and will work for SQLite files.',
        mbInformation, MB_OK, IDOK);
  end;

  (* JAWS settings install is handled in [Run] above as a Finish-page
     postinstall checkbox (item 1 of 4), which invokes
     DbDuo.exe --install-jaws-settings. The C# implementation lives
     in class JawsSettingsInstaller in DbDuo.cs. The previous Pascal
     InstallJawsSettings procedure was removed in v1.0.40 along with
     the [Tasks] entry that gated it; the call site here is a no-op
     now. *)
end;

(* --------------------------------------------------------------------
   Uninstall: remove only the JKMs we installed. The log file written
   by InstallJawsSettings holds the absolute paths of every DbDuo.jkm
   we placed. We read it, delete each path, then delete the log
   itself. If the user customized the JKM in place, we still remove
   it -- the file's name is reserved for the application by JAWS, and
   leaving an orphan after DbDuo is gone would just clutter the JAWS
   settings folders. If the user moved their changes to a different
   filename first, those changes are untouched.
   ----------------------------------------------------------------- *)

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  sLogPath: String;
  oLog: TArrayOfString;
  i: Integer;
begin
  if CurUninstallStep <> usUninstall then exit;
  sLogPath := ExpandConstant('{userappdata}\DbDuo\jawsSettings.log');
  if not FileExists(sLogPath) then exit;
  if not LoadStringsFromFile(sLogPath, oLog) then exit;
  for i := 0 to GetArrayLength(oLog) - 1 do
  begin
    if FileExists(oLog[i]) then
      DeleteFile(oLog[i]);
  end;
  DeleteFile(sLogPath);
  (* Try to remove the {userappdata}\DbDuo folder if it's now empty.
     RemoveDir returns False if non-empty, which is fine -- the user
     may have other DbDuo state in there. *)
  RemoveDir(ExtractFileDir(sLogPath));
end;
