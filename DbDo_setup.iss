; =====================================================================
; DbDo installer script for Inno Setup 6.x
;
; Compile with Inno Setup IDE (ISCC.exe) to produce DbDo_setup.exe.
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

#define AppName       "DbDo"
; AppVersion is rewritten automatically by buildDbDo.cmd from
; DbDo.cs's BuildInfo.VersionString, so this literal is only the
; fallback for compiling the .iss without a prior build. Do not
; hand-edit it as the source of truth -- edit BuildInfo.VersionString.
; ---- Version -----------------------------------------------------------------
; The version number is NOT stored in this script.  It lives in version.txt, one
; line, which Build<App>.cmd increments on every build.  Inno reads it here, and
; Build<App>.cmd also generates Version.cs from it, so the program, the installer,
; and the release tag always report the same number -- which is what Elevate
; Version (F11) compares.  Because no version literal appears in this file, a
; stale copy of it can never rewind the version.
#define VerFile FileOpen(AddBackslash(SourcePath) + "version.txt")
#define AppVersion Trim(FileRead(VerFile))
#expr FileClose(VerFile)
#undef VerFile
#define AppPublisher  "Jamal Mazrui"
#define AppUrl        "https://github.com/JamalMazrui/DbDo"
#define AppExeName    "DbDo.exe"
#define AppCopyright  "Copyright (c) 2026 Jamal Mazrui. MIT License."

; HotKey is the Inno Setup HotKey: directive value (Ctrl syntax
; required by Inno Setup). HotKeyDisplay is the same key in DbDo's
; user-facing notation (Control instead of Ctrl, alpha-ordered).
; Only the Desktop "activate" shortcut is hotkey-bound. The Start
; Menu "GUI only" and "CLI only" shortcuts are deliberately NOT
; hotkey-bound so the global keyboard hotkey space has just one
; DbDo entry: Alt+Control+D, which performs a single-instance
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

; ---- THE TUTORIALS MUST EXIST BEFORE THIS INSTALLER CAN BE BUILT ----
;
; ISPP evaluates this while reading the script, so a missing walkthrough stops
; the compile with a sentence saying what to run, rather than producing an
; installer whose Help menu opens nothing.
;
; Run scripts\buildTutorials once; buildDbDo does it for you when the output is
; not already there.
#if !FileExists(AddBackslash(SourcePath) + "help\Tutorials.mkv")
  #error help\Tutorials.mkv is missing. Run scripts\buildTutorials (or buildDbDo) before building the installer.
#endif
#if !FileExists(AddBackslash(SourcePath) + "help\Tutorials.md")
  #error help\Tutorials.md is missing. Run scripts\makeTutorials (or buildDbDo) before building the installer.
#endif
#if !FileExists(AddBackslash(SourcePath) + "help\Tutorials.htm")
  #error help\Tutorials.htm is missing. buildDbDo converts it with pandoc; run buildDbDo before building the installer.
#endif

[Setup]
; THE INSTALLER KEEPS A LOG, always, and puts it where DbDo's own logs go.
; SetupLogging makes Inno write a detailed record of every file, registry key
; and run entry; the CurStepChanged handler below copies it to
;     %LOCALAPPDATA%\DbDo\logs\DbDo-setup-<yyyymmdd-hhmmss>.log
; when setup finishes, so one folder holds the whole story: the install, and
; every run since. Writing it costs nothing; not having it costs an evening.
SetupLogging=yes

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

; Hide the destination page when a previous install of the same AppId is found,
; which is what the other Homer Tools do: a reinstall then asks nothing at all
; and goes where the last one went. A first install still chooses the folder.
DisableDirPage=auto

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
UninstallDisplayIcon={app}\exec\{#AppExeName}
UninstallDisplayName={#AppName} {#AppVersion}

; The installer EXE itself uses DbDo.ico, so DbDo_setup.exe and the
; uninstaller and any Start Menu entries all carry the same icon.
SetupIconFile=DbDo.ico

MinVersion=10.0

; Enable .zip archive extraction support. The "full" method allows
; the [Files] section extractarchive flag and the Pascal Scripting
; ExtractArchive() function to handle .zip files (not just .7z, the
; default supported format). Inno Setup 6.4+ ships the necessary
; is7z.dll internally; nothing external needed. We use this to
; bundle DbDo's JAWS files (DbDo.jkm, DbDo.jss) inside
; DbDo_JAWS.zip rather than tracking them as two loose files in
; the GitHub repo.
ArchiveExtraction=full

; CloseApplications + RestartApplications enables Inno Setup's
; running-app detection. When the user runs DbDo_setup.exe via
; the Elevate-Version (F11) command while DbDo is already running,
; the installer detects the process by AppMutex name and offers a
; "close the running DbDo" dialog before continuing. Setting
; RestartApplications=yes also re-launches DbDo at the end if it
; was closed during setup, so the user returns to a running app.
; AppMutex must match a mutex DbDo creates at startup (see
; DbDoForm.cs single-instance handoff).
CloseApplications=yes
RestartApplications=yes
CloseApplicationsFilter=DbDo.exe
AppMutex=Local\DbDo.SingleInstance

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
; Welcome page: brief product description plus an MIT license note
; (so we do not need a separate License page). The driver-install
; behavior is mentioned so the user knows what to expect during
; the Installing step.
WelcomeLabel2=This will install [name/ver] on your computer.%n%n[name] is an accessible, keyboard-first database manager for Windows. It opens SQLite, Microsoft Access, Excel, dBASE, and delimited-text files, with first-class support for JAWS, NVDA, and Narrator.%n%n[name] is licensed under the MIT License: free to use, copy, modify, and distribute; provided "as is" with no warranty. The full license text is installed as License.htm.%n%nSetup will silently install the SQLite ODBC driver and the Microsoft Access Database Engine if either is missing; existing drivers are left alone.%n%nIt is recommended that you close all other applications before continuing.

[InstallDelete]
; THE OLD FLAT LAYOUT GOES. Until this version everything sat in {app}: the
; program, the DLLs, the documents, the scripts. Leaving those beside the new
; exec\, help\ and scripts\ folders would mean two DbDo.exe files on one
; machine, and the wrong one in every shortcut somebody made by hand. There is
; no migration path by design; this is how the old one is cleared.
Type: files; Name: "{app}\DbDo.exe"
Type: files; Name: "{app}\DbDo.exe.config"
Type: files; Name: "{app}\DbDo.dll"
Type: files; Name: "{app}\DbDo.ico"
Type: files; Name: "{app}\DbDo.manifest"
Type: files; Name: "{app}\DbDo.cs"
Type: files; Name: "{app}\DbDo.js"
Type: files; Name: "{app}\DbDo.inix"
Type: files; Name: "{app}\DbDo.md"
Type: files; Name: "{app}\DbDo.htm"
Type: files; Name: "{app}\DbDo_JAWS.zip"
Type: files; Name: "{app}\DbDo.nvda-addon"
Type: files; Name: "{app}\nvdaControllerClient.dll"
Type: files; Name: "{app}\NPOI*.dll"
Type: files; Name: "{app}\ICSharpCode.SharpZipLib.dll"
Type: files; Name: "{app}\BouncyCastle.Crypto.dll"
Type: files; Name: "{app}\lookups.db"
Type: files; Name: "{app}\Announce.*"
Type: files; Name: "{app}\History.*"
Type: files; Name: "{app}\CamelType_CSharp.*"
Type: filesandordirs; Name: "{app}\Scripts"
Type: filesandordirs; Name: "{app}\Samples"

; Cleanup on upgrade. Several files from earlier DbDo releases are
; no longer referenced and should be removed so the install folder
; reflects only files DbDo actually uses.
;
; v1.0.40-v1.0.42: NVDA controller-client DLL had a legacy 64-suffixed
; name. v1.0.43+ uses the modern unsuffixed name.
Type: files; Name: "{app}\nvdaControllerClient64.dll"
;
; v1.0.42-v1.0.43 bundled the Roslyn C# scripting assemblies; v1.0.44
; rolled that back in favor of JScript .NET (no shipped runtime DLLs).
; The 12 assemblies below plus the obsolete App.config binding-redirect
; file are all dead weight from v1.0.44 onward and are removed here.
; (DbDo.exe.config returns from a later build, but now it carries startup
; tuning rather than binding redirects, so it is installed by [Files]
; rather than deleted here.)
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
;
; v1.0.58 and earlier shipped dbDuoEval.dll as the JScript .NET support
; module. v1.0.59 renames it to DbDo.dll. Remove the old file on
; upgrade so the install folder doesn't carry an unused DLL.
Type: files; Name: "{app}\dbDuoEval.dll"

[Dirs]
; THE HOMER FOLDER LAYOUT. Every folder starts with a different letter, so a
; screen reader user reaches any of them with one keystroke:
;   configs data exec help scripts templates
; logs, results and temp are not here: they belong to the per-user tree under
; %LOCALAPPDATA%\DbDo, which DbDo makes for itself. A temp folder under Program
; Files could not be written to anyway.
Name: "{app}\configs"
Name: "{app}\data"
Name: "{app}\exec"
Name: "{app}\help"
Name: "{app}\scripts"
Name: "{app}\templates"

[Files]
Source: "exec\DbDo.exe";    DestDir: "{app}\exec"; Flags: ignoreversion
; Runtime configuration for DbDo.exe -- startup tuning (disables Authenticode
; publisher-evidence/CRL checks, enables concurrent GC). It must sit next to
; DbDo.exe; ignoreversion keeps it refreshed in sync with the executable.
Source: "DbDo.exe.config"; DestDir: "{app}\exec"; Flags: ignoreversion
Source: "exec\DbDo.dll"; DestDir: "{app}\exec"; Flags: ignoreversion
; NPOI (Apache-2.0) + SharpZipLib (MIT) + BouncyCastle (MIT-style) -- the
; managed .xlsx engine that lets DbDo open, edit, and save Excel workbooks
; with no Office installed and no bitness dependency. All three licenses
; permit redistribution in binary form, so they are bundled directly here
; (THIRD-PARTY-NOTICES.txt below carries the required license texts). The
; exact versions match what DbDo references, so they load without binding
; redirects: NPOI 2.5.6, SharpZipLib 1.3.3, Portable.BouncyCastle 1.8.9.
; buildDbDo.cmd fetches them next to DbDo.exe; it must run before ISCC so
; these files exist to bundle. (System.Drawing and System.Configuration are
; .NET Framework 4.8 assemblies, so they are not bundled.)
Source: "exec\NPOI.dll";                  DestDir: "{app}\exec"; Flags: ignoreversion
Source: "exec\NPOI.OOXML.dll";            DestDir: "{app}\exec"; Flags: ignoreversion
Source: "exec\NPOI.OpenXml4Net.dll";      DestDir: "{app}\exec"; Flags: ignoreversion
Source: "exec\NPOI.OpenXmlFormats.dll";   DestDir: "{app}\exec"; Flags: ignoreversion
Source: "exec\ICSharpCode.SharpZipLib.dll"; DestDir: "{app}\exec"; Flags: ignoreversion
Source: "exec\BouncyCastle.Crypto.dll";   DestDir: "{app}\exec"; Flags: ignoreversion
Source: "THIRD-PARTY-NOTICES.txt";   DestDir: "{app}"; Flags: ignoreversion
Source: "DbDo.ico";    DestDir: "{app}\exec"; Flags: ignoreversion
Source: "DbDo.manifest"; DestDir: "{app}\exec"; Flags: ignoreversion
; Source and build inputs, shipped so DbDo can be recompiled in place
; (EdSharp-style). buildDbDo.cmd fetches its own NuGet/tool dependencies and
; drives csc/jsc, so no Visual Studio install is required; run it first, then
; recompile this installer with ISCC if desired.
Source: "DbDo.cs";        DestDir: "{app}\exec"; Flags: ignoreversion
Source: "DbDo.js";        DestDir: "{app}\exec"; Flags: ignoreversion
Source: "buildDbDo.cmd";  DestDir: "{app}\exec"; Flags: ignoreversion
Source: "getDbDoDeps.ps1"; DestDir: "{app}\exec"; Flags: ignoreversion
Source: "DbDo_setup.iss"; DestDir: "{app}\exec"; Flags: ignoreversion
; THE SPOKEN WALKTHROUGHS, AND THEY ARE NOT OPTIONAL.
;
; No skipifsourcedoesntexist here, and the check above refuses to compile
; without them. Every other optional part of DbDo can be sorted out at run time
; -- a screen reader that is not installed, a model that has not been fetched --
; because the person can add it later and the program says how. A release
; missing its tutorials is different: nobody knows they are absent, the Help
; item leads nowhere, and the only fix is another release.
;
; So the failure belongs at compile time, on the machine where the fix takes one
; command, rather than at run time on somebody else's.
Source: "help\Tutorials.mkv"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\Tutorials.md";  DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\Tutorials.htm"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\Hotkeys.md";  DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\Hotkeys.htm"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\DbDo.md";     DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\DbDo.htm";    DestDir: "{app}\help"; Flags: ignoreversion
Source: "ReadMe.md";    DestDir: "{app}"; Flags: ignoreversion
Source: "ReadMe.htm";   DestDir: "{app}"; Flags: ignoreversion
Source: "help\Announce.md";  DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\Announce.htm"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\Developer.md";  DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\Developer.htm"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\FAQ.md";        DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\FAQ.htm";       DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\History.md";   DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\History.htm";  DestDir: "{app}\help"; Flags: ignoreversion
Source: "License.md";   DestDir: "{app}"; Flags: ignoreversion
Source: "License.htm";  DestDir: "{app}"; Flags: ignoreversion
; Sample databases: each lives in its own subfolder under templates
; (templates\<root>\<root>.db, plus that database's own scripts beside
; it), and the whole tree is copied to {app}\templates. THE HOMER LAYOUT HAS NO
; samples FOLDER: a template that shows what is possible is a sample with a
; purpose, so the two are one folder. On first access DbDo seeds it into
; %LOCALAPPDATA%\DbDo\data (see seedSampleDatabasesIfNew),
; where the Sample Databases Help command lists it. lookups.db is
; shared infrastructure, not a sample, so it stays in {app}.
Source: "lookups.db"; DestDir: "{app}\data"; Flags: ignoreversion
Source: "templates\*"; DestDir: "{app}\templates"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "DbDo.inix";   DestDir: "{app}\configs"; Flags: ignoreversion onlyifdoesntexist
;
; Scripts: the generic example scripts (each of the three DbDo
; script types) that apply to any database. DbDo seeds these into
; %APPDATA%\DbDo\Scripts on first access (see seedSampleScriptsIfNew).
; Database-specific scripts are NOT here -- they ride along in each
; database's own templates\<root>\ folder above.
;   .js   -- JScript .NET, full computational power, host objects
;   .sql  -- SQL batch run through invokeSql
;   .dbdo -- DbDo command batch dispatched line-by-line as if at
;            the dot prompt
; Source flag recursesubdirs isn't needed -- it's a flat folder.
Source: "scripts\*.js";  DestDir: "{app}\scripts"; Flags: ignoreversion skipifsourcedoesntexist
Source: "scripts\*.sql"; DestDir: "{app}\scripts"; Flags: ignoreversion skipifsourcedoesntexist
Source: "scripts\*.dbdo"; DestDir: "{app}\scripts"; Flags: ignoreversion skipifsourcedoesntexist
;
; DbDo_JAWS.zip contains DbDo.jkm and DbDo.jss at its root. We
; ship the zip rather than the two loose files so the GitHub repo
; stays uncluttered. The zip is extracted into {app} at install
; time by the Pascal procedure ExtractJawsArchive (see [Code]
; below), called from CurStepChanged(ssPostInstall) BEFORE the
; JAWS settings installer runs.
Source: "DbDo_JAWS.zip"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "DbDo.nvda-addon"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "exec\nvdaControllerClient.dll"; DestDir: "{app}\exec"; Flags: ignoreversion

; LOCAL AI. DbDo's Ask commands send a question and the shape of the current
; table to a model running on this machine, and nothing leaves it. Ollama is the
; runner and the model is a separate download, so both are offered on the finish
; page rather than bundled: an installer that quietly pulls two gigabytes is an
; installer people learn to cancel.
Source: "summarizeSetup.cmd"; DestDir: "{app}\exec"; Flags: ignoreversion
Source: "summarizeSetup.ps1"; DestDir: "{app}\exec"; Flags: ignoreversion
Source: "installOllama.cmd"; DestDir: "{app}\exec"; Flags: ignoreversion skipifsourcedoesntexist
Source: "installModels.cmd"; DestDir: "{app}\exec"; Flags: ignoreversion skipifsourcedoesntexist

; (No [Tasks] section. The JAWS settings install is exposed as a
; checkbox on the Finish page via [Run] above, delegating the work
; to DbDo.exe --install-jaws-settings rather than duplicating the
; install logic in Pascal Script.)

[Icons]
; Single shortcut policy: the only DbDo shortcut the installer
; creates is the desktop one with the Alt+Control+D hotkey. The
; -activate parameter performs a single-instance handoff: the
; first press of Alt+Control+D launches DbDo; subsequent presses
; bring the existing instance to the foreground rather than
; launching a duplicate. No Start Menu folder, no GUI-only or
; CLI-only or read-only variants -- the user reaches those modes
; from inside DbDo, not from external launchers.
Name: "{autodesktop}\{#AppName}"; \
  Filename: "{app}\exec\{#AppExeName}"; \
  WorkingDir: "{app}\exec"; \
  Parameters: "-activate"; \
  HotKey: {#HotKey}; \
  Comment: "Dual-mode database manager ({#HotKeyDisplay})"

[Run]
; The four Finish-page checkboxes, in this order.  All are checked by default
; except the user guide.  The order here IS the order shown.
;
; 1. JAWS scripts.  "DbDo.exe --install-jaws-settings" copies the script family into
;    every installed version of JAWS and compiles it there.  The implementation is the
;    shared Homer.JawsSettingsInstaller (in Say.cs), so EdSharp, FileDir, and DbDo all
;    install scripts by the same code, and the command can be re-run later.
FileName: "{app}\exec\DbDo.exe"; \
  Parameters: "--install-jaws-settings"; \
  WorkingDir: "{app}\exec"; \
  Description: "Install scripts for improving use with the JAWS screen reader"; \
  Check: haveJaws; \
  Flags: postinstall waituntilterminated runhidden skipifsilent

; 2. NVDA add-on.  Shell-executing the .nvda-addon hands it to NVDA's own file
;    association, so NVDA shows its native add-on install dialog.  skipifdoesntexist
;    means the checkbox simply does not appear if the app ships no add-on yet.
FileName: "{app}\scripts\DbDo.nvda-addon"; \
  WorkingDir: "{app}\scripts"; \
  Description: "Install add-on for improving use with the NVDA screen reader"; \
  Check: haveNvda; \
  Flags: postinstall shellexec waituntilterminated skipifsilent skipifdoesntexist

; ---- The optional components, each appearing three times ----
;
; One entry per state, grouped so the ones that do something come first:
; everything to be installed, then everything to be updated, then anything
; already current, offered last and never ticked because there is nothing to
; gain. Only ONE entry per tool is ever shown; the other two are skipped by
; their Check function.
;
; Every label carries the versions in play -- install what, update from what to
; what, reinstall which -- so the checkbox says what it would actually do.
;
; runascurrentuser matters: winget and ollama install per user, into the profile
; of whoever is signed in, while this installer runs elevated.
;
; Unticked by default. DbDo works without local AI; the Ask commands are the
; part that needs it, and two gigabytes should never arrive because somebody did
; not notice a checkbox.

; Ollama BEFORE the model. [Run] entries run in the order written, and an
; update to Ollama should land before anything is pulled through it. The
; update entry passes the word update, so installOllama upgrades rather than
; finding Ollama present and leaving the version alone.
FileName: "{cmd}"; \
  Parameters: "/c """"{app}\exec\installOllama.cmd"""""; \
  WorkingDir: "{app}\exec"; \
  Description: "{code:descOllama}"; \
  Flags: postinstall skipifsilent runascurrentuser unchecked; Check: ollamaNeedsInstall

FileName: "{cmd}"; \
  Parameters: "/c """"{app}\exec\installOllama.cmd"""" update"; \
  WorkingDir: "{app}\exec"; \
  Description: "{code:descOllama}"; \
  Flags: postinstall skipifsilent runascurrentuser unchecked; Check: ollamaNeedsUpdate

FileName: "{cmd}"; \
  Parameters: "/c """"{app}\exec\installOllama.cmd"""""; \
  WorkingDir: "{app}\exec"; \
  Description: "{code:descOllama}"; \
  Flags: postinstall skipifsilent runascurrentuser unchecked; Check: ollamaIsCurrent

FileName: "{cmd}"; \
  Parameters: "/c """"{app}\exec\installModels.cmd"""""; \
  WorkingDir: "{app}\exec"; \
  Description: "{code:descModel}"; \
  Flags: postinstall skipifsilent runascurrentuser unchecked

; ---- After the components: what to do now ----
;
; Two ordinary things somebody may want the moment setup ends, offered last
; because they are not installations. runasoriginaluser matters -- setup is
; elevated, and a program started from here would otherwise run as the
; administrator and write its settings into the wrong profile.
; THE LAUNCH IS RECORDED HERE AND HAPPENS AFTER THE RESULTS BOX.
;
; Starting DbDo from this entry puts its window on top of a box the user has not
; read yet, which is what happened in 1.0.145. So this entry leaves a marker and
; summarizeSetup starts DbDo once the box has been closed. The checkbox reads
; the same either way.
FileName: "{cmd}"; \
  Parameters: "/c echo launch > ""{localappdata}\{#AppName}\logs\{#AppName}_launch.flag"""; \
  WorkingDir: "{app}\exec"; \
  Description: "Launch DbDo (Alt+Control+D starts it any time)"; \
  Flags: postinstall skipifsilent runhidden runasoriginaluser

FileName: "{app}\help\DbDo.htm"; \
  Description: "Open the user guide (F1 opens it inside DbDo)"; \
  Flags: postinstall skipifsilent shellexec nowait runasoriginaluser unchecked

; The results summary is NOT listed here. It is not an option -- it always runs,
; and it must run last of all -- so it is started from code at the very end,
; once every entry above has finished. See DeinitializeSetup.

; Native image generation. Not checkboxes: these run automatically and elevated,
; so the installed copy starts from a cached native image instead of
; JIT-compiling. HasNgen skips them if ngen.exe is absent.
FileName: "{code:NgenExe}"; Parameters: "uninstall DbDo /nologo /silent"; Flags: runhidden; Check: HasNgen
FileName: "{code:NgenExe}"; Parameters: "install ""{app}\exec\DbDo.exe"" /AppBase:""{app}\exec"" /nologo /silent"; Flags: runhidden; Check: HasNgen

[UninstallRun]
; Symmetric to the JAWS-install [Run] entry above. Removes only
; the files DbDo placed in the JAWS settings folders, tracked
; via the install-time log at %APPDATA%\DbDo\jawsSettings.log.
; runhidden so no console window flashes during uninstall; ignored
; if DbDo.exe is already deleted (the install probably failed).
FileName: "{app}\exec\{#AppExeName}"; \
  Parameters: "--uninstall-jaws-settings"; \
  WorkingDir: "{app}\exec"; \
  Flags: runhidden waituntilterminated skipifdoesntexist

[Code]

(* ---- WHAT IS ALREADY ON THIS COMPUTER, AND AT WHICH VERSION ----

   A checkbox has to say what it would do, with numbers. "Install Ollama 0.12.3",
   "Update Ollama from 0.11.1 to 0.12.3", "Reinstall Ollama 0.12.3 (current
   version)" -- three labels, parallel in shape, each naming the versions in
   play. A label that just says "Install Ollama" on a machine that has it is
   worse than no label, because it says the installer did not look.

   This is EdSharp's devToolState and devToolDesc at DbDo's smaller scale.

   State: 0 not installed, 1 installed but out of date, 2 current.

   Two ways of asking, because either alone is wrong. winget knows the packages
   it installed and whether a newer version exists. Ollama also installs PER
   USER, into the profile, where an ELEVATED installer's PATH does not reach --
   so its own executable is asked for a version too. Anything found outside
   winget counts as installed: offer a reinstall, not a second copy.

   Every answer is cached. Each probe costs a second or two and the finish page
   asks more than once. *)
var
  gOllamaState: Integer;
  gOllamaKnown: Boolean;
  gOllamaDesc: String;
  gModelDesc: String;

procedure logProbe(sLogDir, sCommand: String; iExit: Integer; bRead: Boolean; lsLines: TArrayOfString);
var
  lsOut: TArrayOfString;
  sText: String;
  i: Integer;
begin
  sText := '[probe] ' + sCommand + '   exit ' + IntToStr(iExit);
  if not bRead then sText := sText + '   (no output captured)';
  SetArrayLength(lsOut, 1);
  lsOut[0] := sText;
  SaveStringsToFile(AddBackslash(sLogDir) + '{#AppName}_setup.log', lsOut, True);
  if bRead then
    for i := 0 to GetArrayLength(lsLines) - 1 do
      if Trim(lsLines[i]) <> '' then
      begin
        lsOut[0] := '[probe]   ' + lsLines[i];
        SaveStringsToFile(AddBackslash(sLogDir) + '{#AppName}_setup.log', lsOut, True);
      end;
end;

function probeLines(sCommand: String; var lsLines: TArrayOfString): Boolean;
var
  sCapture, sLogDir: String;
  iResult: Integer;
begin
  (* The capture goes to DbDo's own logs folder, like everything else this
     installer writes, and is deleted once its lines are in memory. *)
  sLogDir := ExpandConstant('{localappdata}\{#AppName}\logs');
  ForceDirectories(sLogDir);
  sCapture := sLogDir + '\{#AppName}_probe.tmp';
  (* /s /c, WHICH IS THE ONLY FORM THAT BEHAVES THE SAME EVERY TIME.
     Without /s, cmd decides for itself whether to strip the outer quotes, and
     the decision depends on how many quotes the rest of the line holds. The
     first attempt used no wrapper and broke a quoted path; the second wrapped
     everything and broke the plain commands instead -- "where ollama" came back
     exit 1 with no capture file at all, which is what a line cmd could not
     parse looks like.
     With /s the rule is fixed: strip the first and last quote, run the rest
     verbatim. One form, both cases. *)
  Result := Exec(ExpandConstant('{cmd}'), '/s /c "' + sCommand + ' > "' + sCapture + '" 2>&1"',
                 '', SW_HIDE, ewWaitUntilTerminated, iResult);
  if Result then Result := LoadStringsFromFile(sCapture, lsLines);
  (* EVERY PROBE IS LOGGED. A detection that goes wrong on somebody else's
     machine cannot be diagnosed otherwise, which is how the last two releases
     went. *)
  logProbe(sLogDir, sCommand, iResult, Result, lsLines);
  if FileExists(sCapture) then DeleteFile(sCapture);
end;

function wingetInfo(sId: String; var sInstalled, sAvailable: String): Boolean;
(* True when winget lists the package as installed; fills the installed version
   and, when an update exists, the available one. Columns are found by the
   header line, since names contain spaces. *)
var
  lsLines: TArrayOfString;
  i, iVer, iAvail, iSrc: Integer;
  sLine: String;
begin
  Result := False; sInstalled := ''; sAvailable := '';
  iVer := 0; iAvail := 0; iSrc := 0;
  if not probeLines('winget list --id ' + sId + ' --exact --disable-interactivity', lsLines) then exit;
  for i := 0 to GetArrayLength(lsLines) - 1 do
  begin
    sLine := lsLines[i];
    if (iVer = 0) and (Pos('Name', sLine) > 0) and (Pos('Version', sLine) > 0) then
    begin
      iVer := Pos('Version', sLine);
      iAvail := Pos('Available', sLine);
      iSrc := Pos('Source', sLine);
      continue;
    end;
    if (iVer > 0) and (Pos(sId, sLine) > 0) then
    begin
      Result := True;
      if iAvail > 0 then
      begin
        sInstalled := Trim(Copy(sLine, iVer, iAvail - iVer));
        if iSrc > iAvail then sAvailable := Trim(Copy(sLine, iAvail, iSrc - iAvail))
        else sAvailable := Trim(Copy(sLine, iAvail, 200));
      end
      else if iSrc > iVer then sInstalled := Trim(Copy(sLine, iVer, iSrc - iVer))
      else sInstalled := Trim(Copy(sLine, iVer, 200));
      exit;
    end;
  end;
end;

function wingetLatest(sId: String): String;
(* The newest version winget offers, installed or not, so the Install label can
   carry a number parallel to the Update one. *)
var
  lsLines: TArrayOfString;
  i: Integer;
  sLine: String;
begin
  Result := '';
  if not probeLines('winget show --id ' + sId + ' --exact --disable-interactivity', lsLines) then exit;
  for i := 0 to GetArrayLength(lsLines) - 1 do
  begin
    sLine := Trim(lsLines[i]);
    if Pos('Version:', sLine) = 1 then
    begin
      Result := Trim(Copy(sLine, 9, 100));
      exit;
    end;
  end;
end;

function lastWord(sText: String): String;
(* Ollama answers "ollama version is 0.34.1", so the version is the last word.
   Returning the whole sentence produced "Reinstall Ollama the installed version
   (installed version)" on Jamal's machine -- a label that says nothing twice. *)
var
  i: Integer;
begin
  Result := Trim(sText);
  for i := Length(Result) downto 1 do
    if Result[i] = ' ' then
    begin
      Result := Copy(Result, i + 1, Length(Result));
      exit;
    end;
end;

function exeVersion(sExe: String): String;
(* The tool's own version line, for an install winget does not know about. *)
var
  lsLines: TArrayOfString;
  i: Integer;
begin
  Result := '';
  if not probeLines(sExe + ' --version', lsLines) then exit;
  for i := 0 to GetArrayLength(lsLines) - 1 do
    if Trim(lsLines[i]) <> '' then
    begin
      Result := Trim(lsLines[i]);
      exit;
    end;
end;

function ollamaExe(): String;
(* Where Ollama actually is. It installs PER USER by default, so the profile
   copy is looked for first, then a machine-wide one, then whatever is on the
   PATH. *)
begin
  Result := ExpandConstant('{localappdata}\Programs\Ollama\ollama.exe');
  if FileExists(Result) then exit;
  Result := ExpandConstant('{localappdata}\Ollama\ollama.exe');
  if FileExists(Result) then exit;
  Result := ExpandConstant('{commonpf}\Ollama\ollama.exe');
  if FileExists(Result) then exit;
  (* Plain "ollama" last: the PATH is what installOllama.cmd uses and what
     actually resolves on a machine where Ollama installed itself somewhere
     these constants do not name. *)
  Result := 'ollama';
end;

function ollamaOnPath(): Boolean;
(* THE ONE ANSWER THAT WORKED.
   installOllama.cmd finds Ollama with "where ollama", and on this machine that
   succeeded while every path guess failed: the probe of
   %localappdata%\Programs\Ollama\ollama.exe returned exit 1 and no output,
   because Ollama is not there. So ask the way the script that works asks, and
   keep the path checks below only as a second opinion. *)
var
  lsLines: TArrayOfString;
begin
  Result := probeLines('where ollama', lsLines);
  if Result then
  begin
    Result := False;
    if GetArrayLength(lsLines) > 0 then
      Result := Pos('ollama', Lowercase(lsLines[0])) > 0;
  end;
end;

function ollamaIsOnDisk(): Boolean;
(* The cheapest and most reliable signal: the file itself, or the uninstall
   entry. No process to start, no quoting to get wrong, no PATH to depend on.
   An elevated installer still sees the file. *)
var
  sKey: String;
begin
  Result := ollamaOnPath();
  if Result then exit;
  Result := FileExists(ExpandConstant('{localappdata}\Programs\Ollama\ollama.exe'))
         or FileExists(ExpandConstant('{localappdata}\Ollama\ollama.exe'))
         or FileExists(ExpandConstant('{commonpf}\Ollama\ollama.exe'))
         or FileExists(ExpandConstant('{userpf}\Ollama\ollama.exe'));
  if Result then exit;
  sKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\Ollama';
  Result := RegKeyExists(HKEY_CURRENT_USER, sKey)
         or RegKeyExists(HKEY_LOCAL_MACHINE, sKey);
end;

function ollamaState(): Integer;
var
  sInstalled, sAvailable: String;
begin
  if gOllamaKnown then
  begin
    Result := gOllamaState;
    exit;
  end;
  Result := 0;
  if wingetInfo('Ollama.Ollama', sInstalled, sAvailable) then
  begin
    if sAvailable <> '' then Result := 1 else Result := 2;
  end;
  (* Installed outside winget still counts as installed: offer a reinstall, not
     a second copy. The file check comes before the version probe because it
     cannot fail for a reason nobody can see. *)
  if (Result = 0) and ollamaIsOnDisk() then Result := 2;
  gOllamaState := Result;
  gOllamaKnown := True;
end;

function ollamaNeedsInstall(): Boolean;
begin
  Result := ollamaState() = 0;
end;

function ollamaNeedsUpdate(): Boolean;
begin
  Result := ollamaState() = 1;
end;

function ollamaIsCurrent(): Boolean;
begin
  Result := ollamaState() = 2;
end;

function descOllama(sParam: String): String;
(* The three labels, each carrying the versions in play. *)
var
  sInstalled, sAvailable, sVersion: String;
begin
  if gOllamaDesc <> '' then
  begin
    Result := gOllamaDesc;
    exit;
  end;
  Result := '';
  if wingetInfo('Ollama.Ollama', sInstalled, sAvailable) then
  begin
    if sAvailable <> '' then
      Result := 'Update Ollama from ' + sInstalled + ' to ' + sAvailable
    else if sInstalled <> '' then
      Result := 'Reinstall Ollama ' + sInstalled + ' (current version)';
  end;
  if Result = '' then
  begin
    sVersion := lastWord(exeVersion('"' + ollamaExe() + '"'));
    if sVersion = '' then sVersion := lastWord(exeVersion('ollama'));
    if sVersion <> '' then
      Result := 'Reinstall Ollama ' + sVersion + ' (current version)'
    else if ollamaIsOnDisk() then
      Result := 'Reinstall Ollama (already installed)'
    else
    begin
      sVersion := wingetLatest('Ollama.Ollama');
      if sVersion <> '' then
        Result := 'Install Ollama ' + sVersion
      else
        Result := 'Install Ollama, which runs the AI model locally (about 600 MB)';
    end;
  end;
  gOllamaDesc := Result;
end;

function haveJaws(): Boolean;
(* A checkbox must know what is already installed -- and that applies to the
   screen readers too, not only to the AI. EdSharp gates its JAWS entry this
   way; DbDo offered both to everybody. *)
begin
  Result := RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\Freedom Scientific\JAWS')
         or RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\WOW6432Node\Freedom Scientific\JAWS');
end;

function haveNvda(): Boolean;
begin
  Result := FileExists(ExpandConstant('{commonpf32}\NVDA\nvda.exe'))
         or FileExists(ExpandConstant('{commonpf}\NVDA\nvda.exe'))
         or RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\NVDA');
end;

function descModel(sParam: String): String;
(* The model is not a winget package, so there is no version pair to report --
   it is there or it is not. The label says which, and how big the download is,
   because that is the number this checkbox actually costs. *)
var
  lsLines: TArrayOfString;
  i: Integer;
begin
  if gModelDesc <> '' then
  begin
    Result := gModelDesc;
    exit;
  end;
  Result := 'Install the llama3.2 model, which DbDo asks questions of (about 2 GB)';
  if ollamaState() > 0 then
  begin
    (* Ask the way installOllama.cmd asks -- through the PATH -- and look for
       THIS model rather than for any model. Five other models installed does
       not mean the one DbDo uses is there. *)
    if probeLines('ollama list', lsLines) then
      for i := 0 to GetArrayLength(lsLines) - 1 do
        if Pos('llama3.2', Lowercase(lsLines[i])) > 0 then
        begin
          Result := 'Reinstall the llama3.2 model (already installed)';
          break;
        end;
  end;
  gModelDesc := Result;
end;

(* ---- WHAT THIS INSTALL IS: a fresh install, an update, or a reinstall ----
   Read from DbDo's own uninstall key, so the Welcome page says which of the
   three is about to happen rather than leaving somebody to work it out. The
   same answer drives any step that should run once on a fresh install and not
   again on an update. *)
var
  sPriorVersion: String;

function priorVersion(): String;
var
  sFound: String;
begin
  Result := '';
  if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1',
                         'DisplayVersion', sFound) then Result := sFound
  else if RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1',
                         'DisplayVersion', sFound) then Result := sFound;
end;

function isFreshInstall(): Boolean;
begin
  Result := (sPriorVersion = '');
end;

(* describeThisInstall writes on the READY page, not the Welcome page.
   Inno Setup 6 hides the Welcome page by default, and the speech history of a
   clean install shows it never appears -- so the sentence saying install,
   update or reinstall was being written to a page nobody reaches. The Ready
   page is read aloud in full ("Click Install to continue..."), so the sentence
   goes at the front of that instead. *)
procedure describeThisInstall();
begin
  if sPriorVersion = '' then
    WizardForm.ReadyLabel.Caption := 'This will install {#AppName} {#AppVersion}.' + #13#10#13#10 + WizardForm.ReadyLabel.Caption
  else if sPriorVersion = '{#AppVersion}' then
    WizardForm.ReadyLabel.Caption := 'This will reinstall {#AppName} {#AppVersion}, which is already here.' + #13#10#13#10 + WizardForm.ReadyLabel.Caption
  else
    WizardForm.ReadyLabel.Caption := 'This will update {#AppName} from ' + sPriorVersion + ' to {#AppVersion}.' + #13#10#13#10 + WizardForm.ReadyLabel.Caption;
end;

(* --------------------------------------------------------------------
   Native image generation (ngen), identical to EdSharp and FileDir.

   ngen ships with the 64-bit .NET Framework runtime; on an ARM64 system
   the Framework64 path is the ARM64 framework.  HasNgen guards against a
   missing file, so the [Run] ngen entries are simply skipped when ngen is
   not present rather than failing the install.
   -------------------------------------------------------------------- *)
function NgenExe(sParam: string): string;
begin
  result := ExpandConstant('{win}\Microsoft.NET\Framework64\v4.0.30319\ngen.exe');
end;

function HasNgen(): boolean;
begin
  result := FileExists(ExpandConstant('{code:NgenExe}'));
end;
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
  (* Say on the Welcome page whether this is an install, an update or a
     reinstall, read from DbDo's own uninstall key. *)
  describeThisInstall();

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
    'Setup is fetching one or more drivers DbDo needs to read database files.',
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
          + 'Setup will continue. DbDo will install but some database '
          + 'formats may not open until the missing drivers are added '
          + 'manually. Re-run this installer with a network connection, '
          + 'or download the drivers from the URLs in DbDo.md.',
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
   DbDo.cs class JawsSettingsInstaller, invoked via
       DbDo.exe --install-jaws-settings
   from the [Run] section above, and via
       DbDo.exe --uninstall-jaws-settings
   from the [UninstallRun] section below. The C# implementation
   uses the same algorithm (registry-first scompile lookup with
   Program Files fallback, enumerate %APPDATA%\Freedom Scientific\
   JAWS\*\Settings\*, copy + scompile, log the paths placed) and
   the same uninstall log location ({userappdata}\DbDo\
   jawsSettings.log) so the upgrade from v1.0.39 to v1.0.40 is
   transparent. The advantage of the move: the user can re-run the
   install later from Help > Install JAWS Settings without re-
   running the full installer. *)

(* --- Post-install hook: extract JAWS archive, run driver installers --- *)

(* Extract DbDo_JAWS.zip into {app}. The zip contains DbDo.jkm
   and DbDo.jss at its root; after extraction these sit in
   {app}\scripts, which is where the Homer layout puts anything a
   screen reader runs. DbDo.exe --install-jaws-settings (run as the
   Finish-page checkbox) then reads them from there and copies them
   into each per-version JAWS settings folder.

   Requires the [Setup] directive ArchiveExtraction=full so Inno
   Setup's bundled is7z.dll can handle the .zip format (the default
   "basic" extraction only supports .7z). Uses Inno Setup's built-
   in Pascal ExtractArchive function (Inno Setup 6.4.0+). No
   external unzip tool is shipped with DbDo.

   On failure we surface a non-fatal SuppressibleMsgBox: DbDo
   still works for non-JAWS users even without these files.
*)
procedure ExtractJawsArchive;
var
  sZipPath: String;
  sDestDir: String;
begin
  (* THE HOMER LAYOUT: screen reader files are scripts, so the archive ships
     into {app}\scripts and unpacks there. DbDo.exe --install-jaws-settings
     reads DbDo.jkm and DbDo.jss from the same folder. *)
  sZipPath := ExpandConstant('{app}\scripts\DbDo_JAWS.zip');
  sDestDir := ExpandConstant('{app}\scripts');
  if not FileExists(sZipPath) then
  begin
    SuppressibleMsgBox(
      'DbDo_JAWS.zip was not found in the scripts folder of the installation.'#13#10#13#10
      + 'JAWS keymap and script files will not be available. If you'#13#10
      + 'use JAWS, re-run the installer.'#13#10#13#10
      + 'DbDo itself is fine and will run.',
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
      'Failed to extract DbDo_JAWS.zip:'#13#10#13#10
      + GetExceptionMessage + #13#10#13#10
      + 'JAWS keymap and script files will not be available.'#13#10
      + 'DbDo itself is fine and will run.',
      mbInformation, MB_OK, IDOK);
  end;
end;

procedure keepSetupLog();
var
  sFolder: String;
  sTarget: String;
begin
  sFolder := ExpandConstant('{localappdata}\{#AppName}\logs');
  if not DirExists(sFolder) then
    if not ForceDirectories(sFolder) then exit;
  sTarget := sFolder + '\{#AppName}-setup-' + GetDateTimeString('yyyymmdd-hhnnss', #0, #0) + '.log';
  FileCopy(ExpandConstant('{log}'), sTarget, False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  (* Keep the setup log with the program's own logs. *)
  if CurStep = ssDone then keepSetupLog();

  if CurStep = ssInstall then
  begin
    (* Download any missing driver installers before the main copy. *)
    DownloadMissingDrivers;
    exit;
  end;

  if CurStep <> ssPostInstall then exit;

  (* Extract JAWS keymap and script files BEFORE the Finish-page
     Run-section entry invokes DbDo.exe --install-jaws-settings;
     that command needs DbDo.jkm and DbDo.jss in {app}. *)
  ExtractJawsArchive;

  if bInstallSqliteOdbc then
  begin
    if not InstallSqliteOdbc then
      SuppressibleMsgBox(
        'The SQLite ODBC driver did not install cleanly.'#13#10#13#10
        + 'You can install it later by downloading from:'#13#10
        + '  http://www.ch-werner.de/sqliteodbc/'#13#10#13#10
        + 'DbDo is still installed and will work for other formats.',
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
        + 'DbDo is still installed and will work for SQLite files.',
        mbInformation, MB_OK, IDOK);
  end;

  (* JAWS settings install is handled in [Run] above as a Finish-page
     postinstall checkbox (item 1 of 4), which invokes
     DbDo.exe --install-jaws-settings. The C# implementation lives
     in class JawsSettingsInstaller in DbDo.cs. The previous Pascal
     InstallJawsSettings procedure was removed in v1.0.40 along with
     the [Tasks] entry that gated it; the call site here is a no-op
     now. *)
end;

(* --------------------------------------------------------------------
   Uninstall: remove only the JKMs we installed. The log file written
   by InstallJawsSettings holds the absolute paths of every DbDo.jkm
   we placed. We read it, delete each path, then delete the log
   itself. If the user customized the JKM in place, we still remove
   it -- the file's name is reserved for the application by JAWS, and
   leaving an orphan after DbDo is gone would just clutter the JAWS
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
  sLogPath := ExpandConstant('{userappdata}\DbDo\jawsSettings.log');
  if not FileExists(sLogPath) then exit;
  if not LoadStringsFromFile(sLogPath, oLog) then exit;
  for i := 0 to GetArrayLength(oLog) - 1 do
  begin
    if FileExists(oLog[i]) then
      DeleteFile(oLog[i]);
  end;
  DeleteFile(sLogPath);
  (* Try to remove the {userappdata}\DbDo folder if it's now empty.
     RemoveDir returns False if non-empty, which is fine -- the user
     may have other DbDo state in there. *)
  RemoveDir(ExtractFileDir(sLogPath));
end;

function InitializeSetup(): Boolean;
begin
  sPriorVersion := priorVersion();
  Result := True;
end;

(* ---- The Results summary: always, and always last ---- *)

procedure addAction(sFolder, sText: String);
(* One line saying what this installation actually DID. The Results box is built
   from these lines and nothing else, so a component that was already there and
   needed nothing is never mentioned. *)
var
  lsLines: TArrayOfString;
begin
  SetArrayLength(lsLines, 1);
  lsLines[0] := sText;
  SaveStringsToFile(AddBackslash(sFolder) + '{#AppName}_setup_actions.txt', lsLines, True);
end;

procedure saveResultsForSummary(sFolder, sText: String);
(* Hand what the installer already knows to the summary, so ONE box tells the
   whole story instead of two telling halves. *)
var
  lsLines: TArrayOfString;
begin
  SetArrayLength(lsLines, 1);
  lsLines[0] := sText;
  SaveStringsToFile(AddBackslash(sFolder) + '{#AppName}_setup_results.txt', lsLines, False);
end;

procedure showResultsSummary();
(* Started from code rather than listed as a checkbox: the summary is part of
   installing, not something to opt into, and it must run after every finish-page
   entry. Hidden, and setup does not wait for it, so closing the box is the last
   thing that happens. *)
var
  iResult: Integer;
begin
  try
    (* TWO pairs of quotes, not one. cmd /s strips the outermost pair and runs
       what is left verbatim -- so with one pair, C:\Program Files\... arrived
       unquoted, cmd tried to run "C:\Program", and the summary never started.
       That is why 1.0.168 showed no Results box. The probes survived the /s
       change because their commands carry their own quotes; this line did not. *)
    Exec(ExpandConstant('{cmd}'), '/s /c ""' + ExpandConstant('{app}\exec\summarizeSetup.cmd') + '""',
         ExpandConstant('{app}\exec'), SW_HIDE, ewNoWait, iResult);
  except
  end;
end;

procedure DeinitializeSetup();
var
  sBreak, sLogDir, sMessage: String;
begin
  (* DeinitializeSetup runs whenever Setup exits, INCLUDING WHEN THE USER
     CANCELS. Announcing success to somebody who backed out would be a plain
     lie, and in a silent installation there is nobody to close a box. *)
  if WizardSilent then exit;
  if not FileExists(ExpandConstant('{app}\exec\{#AppExeName}')) then exit;

  sBreak := Chr(13) + Chr(10);
  sLogDir := ExpandConstant('{localappdata}\{#AppName}\logs');
  ForceDirectories(sLogDir);

  sMessage := '{#AppName} {#AppVersion} is installed.' + sBreak + sBreak
    + 'Program files:' + sBreak + '  ' + ExpandConstant('{app}') + sBreak
    + 'Sample databases:' + sBreak + '  ' + ExpandConstant('{app}\templates');

  saveResultsForSummary(sLogDir, sMessage);
  showResultsSummary();
end;
