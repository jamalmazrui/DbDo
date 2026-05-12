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
#define AppVersion    "1.0.19"
#define AppPublisher  "Jamal Mazrui"
#define AppUrl        "https://github.com/JamalMazrui/DbDuo"
#define AppExeName    "DbDuo.exe"
#define AppCopyright  "Copyright (c) 2026 Jamal Mazrui. MIT License."

; HotKey is the Inno Setup HotKey: directive value (Ctrl syntax
; required by Inno Setup). HotKeyDisplay is the same key in DbDuo's
; preferred "Alt+Control+D" notation for human-readable tooltips
; and comments.
#define HotKey        "Alt+Ctrl+D"
#define HotKeyDisplay "Alt+Control+D"
#define HotKeyGui     "Alt+Ctrl+G"
#define HotKeyGuiDisp "Alt+Control+G"
#define HotKeyCli     "Alt+Ctrl+L"
#define HotKeyCliDisp "Alt+Control+L"

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
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UsePreviousAppDir=yes
DisableDirPage=no
UsePreviousGroup=yes

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

MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
; Welcome page: brief product description plus an MIT license note
; (so we do not need a separate License page). The driver-install
; behavior is mentioned so the user knows what to expect during
; the Installing step.
WelcomeLabel2=This will install [name/ver] on your computer.%n%n[name] is licensed under the MIT License: free to use, copy, modify, and distribute; provided "as is" with no warranty. The full license text is installed as License.htm.%n%nSetup will silently install the SQLite ODBC driver and the Microsoft Access Database Engine if either is missing; existing drivers are left alone.%n%nIt is recommended that you close all other applications before continuing.

[Files]
Source: "DbDuo.exe";    DestDir: "{app}"; Flags: ignoreversion
Source: "DbDuo.md";     DestDir: "{app}"; Flags: ignoreversion
Source: "DbDuo.htm";    DestDir: "{app}"; Flags: ignoreversion
Source: "README.md";    DestDir: "{app}"; Flags: ignoreversion
Source: "README.htm";   DestDir: "{app}"; Flags: ignoreversion
Source: "License.md";   DestDir: "{app}"; Flags: ignoreversion
Source: "License.htm";  DestDir: "{app}"; Flags: ignoreversion
Source: "sample.db";    DestDir: "{app}"; Flags: ignoreversion
Source: "DbDuo.ini";    DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist

[Icons]
Name: "{group}\{#AppName}"; \
  Filename: "{app}\{#AppExeName}"; \
  WorkingDir: "{app}"; \
  Comment: "Dual-mode (GUI + dot-prompt CLI) database manager"

Name: "{group}\{#AppName} (GUI only)"; \
  Filename: "{app}\{#AppExeName}"; \
  WorkingDir: "{app}"; \
  Parameters: "-gui"; \
  HotKey: {#HotKeyGui}; \
  Comment: "Launch GUI only ({#HotKeyGuiDisp})"

Name: "{group}\{#AppName} (CLI only)"; \
  Filename: "{app}\{#AppExeName}"; \
  WorkingDir: "{app}"; \
  Parameters: "-cli"; \
  HotKey: {#HotKeyCli}; \
  Comment: "Launch dot-prompt console only ({#HotKeyCliDisp})"

Name: "{group}\{#AppName} (read-only)"; \
  Filename: "{app}\{#AppExeName}"; \
  WorkingDir: "{app}"; \
  Parameters: "-readonly"; \
  Comment: "Open files read-only"

Name: "{group}\{#AppName} sample database"; \
  Filename: "{app}\{#AppExeName}"; \
  WorkingDir: "{app}"; \
  Parameters: """{app}\sample.db"""; \
  Comment: "Open the bundled sample database"

Name: "{group}\{#AppName} documentation"; \
  Filename: "{app}\DbDuo.htm"; \
  WorkingDir: "{app}"; \
  Comment: "User manual"

Name: "{group}\Uninstall {#AppName}"; \
  Filename: "{uninstallexe}"; \
  Comment: "Remove {#AppName} from this computer"

; Desktop shortcut with Alt+Control+D hotkey. -activate performs a
; single-instance handoff: a second press wakes the running instance
; to the foreground rather than launching a duplicate.
Name: "{autodesktop}\{#AppName}"; \
  Filename: "{app}\{#AppExeName}"; \
  WorkingDir: "{app}"; \
  Parameters: "-activate"; \
  HotKey: {#HotKey}; \
  Comment: "Dual-mode database manager ({#HotKeyDisplay})"

[Run]
FileName: "{app}\{#AppExeName}"; \
  WorkingDir: "{app}"; \
  Description: "Launch {#AppName} now (or use the desktop hotkey {#HotKeyDisplay} anytime)"; \
  Flags: nowait postinstall skipifsilent

FileName: "{app}\DbDuo.htm"; \
  Description: "Read documentation for {#AppName}"; \
  Flags: postinstall shellexec skipifsilent

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

(* --- Post-install hook: run driver installers --- *)

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    (* Download any missing driver installers before the main copy. *)
    DownloadMissingDrivers;
    exit;
  end;

  if CurStep <> ssPostInstall then exit;

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
end;
