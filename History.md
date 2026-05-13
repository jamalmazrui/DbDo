# DbDuo History of Changes

This file is the chronological record of DbDuo releases. The most recent release is at the top. For the overview of what DbDuo is and the current feature set, see `Announce.md` or `README.md`. For the full reference, see `DbDuo.md`.

Press **Shift+F1** inside DbDuo to open this file in your browser, or type `history` at the dot prompt.

## v1.0.51 (current)

Inno Setup compile fix. v1.0.50's `CurStepChanged` procedure contained a Pascal block comment whose second line read `[Run] entry invokes DbDuo.exe...`. Inno Setup's preprocessor scans every line at column 0 (after stripping leading whitespace) for `[Section]` tags BEFORE Pascal-comment parsing happens, so a line whose first non-whitespace token is the literal `[Run]` is treated as a section header even when it lives inside a `(* ... *)` block comment. The compiler reported "Invalid section tag" at the offending line.

The fix is to rephrase the comment so no line begins with a bracketed section name: `[Run] entry` became `Run-section entry`. Other Pascal comments in the file mention `[Run]`, `[Setup]`, `[UninstallRun]`, `[Tasks]` etc., but in every case the bracketed token is preceded by other text on the same line (e.g. `Requires the [Setup] directive...`), so the bracket is not the first token and the preprocessor leaves them alone. Only the one offending line needed editing.

## v1.0.50

Two changes: NVDA add-on manifest format fix, and JAWS files repackaged as a single zip in the repo.

**NVDA add-on manifest now follows the documented format.** v1.0.46-v1.0.49 produced a `manifest.ini` that NVDA rejected with an "invalid format" error at install time. Comparing against the working AIChatbot add-on uploaded for reference revealed two problems: string values containing spaces and special characters (the `summary`, `description`, and `author` fields) were not enclosed in quotes; and the zip contained a standalone `appModules/` directory entry. The NVDA Developer Guide is explicit that "all string values must be enclosed in quotes" in the manifest, and the working reference includes no standalone directory entries. v1.0.50 fixes both: free-form text fields (`summary`, `description`, `author`) are now quoted (triple-quoted for the multi-line description), identifier-like fields (`name`, `url`, `version`, `docFileName`, version strings, `updateChannel`) remain unquoted matching the AIChatbot reference, double-quote characters inside the description were replaced with single quotes to avoid embedded-quote parse ambiguity, and the zip is repacked listing only files (no standalone directory entry). Add-on internal version bumped to 1.0.2 so existing 1.0.1 installs (the broken version) see this as an upgrade.

**JAWS files now ship as DbDuo_JAWS.zip.** Previously the repo carried `DbDuo.jkm` (the JAWS keymap) and `DbDuo.jss` (the JAWS script) as two loose files alongside DbDuo.cs and DbDuo_setup.iss. v1.0.50 replaces them in the repo with a single `DbDuo_JAWS.zip` archive containing both, keeping the repo's top level less cluttered. At install time Inno Setup's `[Files]` section drops `DbDuo_JAWS.zip` into `{app}`, and a new Pascal procedure `ExtractJawsArchive` (called from `CurStepChanged(ssPostInstall)`, before the Finish-page checkbox runs) extracts the zip in place. The result is identical to v1.0.49's install layout: `DbDuo.jkm` and `DbDuo.jss` sit alongside `DbDuo.exe`, and the `DbDuo.exe --install-jaws-settings` command (run as the Finish-page checkbox) reads them from there.

The extraction uses Inno Setup's built-in `ExtractArchive` Pascal function (Inno Setup 6.4+, January 2025) with the new `[Setup]` directive `ArchiveExtraction=full`. The "full" mode loads Inno Setup's bundled `is7z.dll` (compiled from 7-Zip source by the Inno Setup maintainer), which handles `.zip` natively. **No external unzip tool is shipped** — no `7z.exe`, no `unzip.exe`, no DLLs the project would need to track or update. The `.gitignore` now lists `DbDuo.jkm` and `DbDuo.jss` as ignored: developers who need to edit those files unzip them locally, edit, then re-zip into `DbDuo_JAWS.zip`. Only `DbDuo_JAWS.zip` is the version-controlled source of truth.

## v1.0.49

NVDA add-on install dialog now actually appears at end of setup. **Diagnosis:** The Finish-page checkbox "Install NVDA add-on" was wired to invoke `DbDuo.exe --install-nvda-addon`, which then called `Process.Start` on `DbDuo.nvda-addon` with `UseShellExecute = true` and returned 0 immediately. Inno Setup's `waituntilterminated` waited for DbDuo.exe to exit — but DbDuo.exe exits as soon as it has handed the file off to Windows, BEFORE the shell-execute resolution has located the .nvda-addon file handler and launched NVDA's dialog. The result: at the speed of the installer's wizard, the NVDA dialog often never appeared, or appeared briefly behind the installer-completed page where the user couldn't see it.

**Fix:** Cut DbDuo.exe out of the chain at install time. The `[Run]` entry now points `FileName` directly at `{app}\DbDuo.nvda-addon` and uses Inno Setup's `shellexec` flag, which is exactly the documented mechanism for "open this non-executable file with its file association." Combined with `waituntilterminated`, this lets Inno Setup do the ShellExecuteEx call itself with proper handle tracking — NVDA opens its standard "Install this add-on?" dialog, the user confirms or cancels, NVDA finishes its add-on install, AND ONLY THEN does the wizard's Finish page complete.

If NVDA is not installed at all (no `.nvda-addon` file association registered), Windows shows its "How do you want to open this file?" picker, which the user can dismiss; this is acceptable since the user wouldn't have NVDA add-on integration to lose anyway.

The C# `--install-nvda-addon` flag in DbDuo.exe is unchanged and still supported. It's how the user re-installs the add-on later from outside the installer, and it works fine in that context because the user is interactive and there is no parallel "wizard completion" race. The flag will continue to be available for the Help menu's "Re-install NVDA Add-on" command and for command-line invocation.

## v1.0.48

Inno Setup compile fix. `DbDuo_setup.iss` line 499 called the Pascal procedure `InstallJawsSettings`, which was removed in v1.0.40 when JAWS settings install moved from Inno Setup's `[Code]` section to the C# class `JawsSettingsInstaller`. The v1.0.40 cleanup left an orphaned call site that referenced both the deleted function AND a `[Tasks]` entry (`jawsSettings`) that had also been removed. The Inno Setup compiler reported it as `Unknown identifier 'InstallJawsSettings'` at line 499. Removed the dead block; the JAWS settings install path is unaffected because the Finish-page `[Run]` entry at line 197 already invokes `DbDuo.exe --install-jaws-settings` as a postinstall checkbox.

A separate audit of the `[Code]` section confirmed no other dangling references to v1.0.39-era Pascal procedures. `CurUninstallStepChanged` is kept intact: it duplicates the work done by the `[UninstallRun]` call to `DbDuo.exe --uninstall-jaws-settings`, but harmlessly — the first to run deletes the log file, the second sees the missing log and exits early. The redundancy is defense in depth: if DbDuo.exe is missing or fails at uninstall time, the Pascal procedure still cleans up the JAWS settings files via the log it reads directly.

## v1.0.47

Two doc-and-label improvements driven by user review.

**No "Object" in GUI command labels.** The word "Object" was scrubbed from every user-facing GUI command label, dialog title, and context-menu entry. The PowerShell canonical command names (`Show-Object`, `Sort-Object`, `Select-Object`, `Switch-Object`, `Switch-ObjectPrevious`) remain unchanged — they are the third `addItem` argument used at the dot prompt, where the Verb-Noun pattern is the convention. The renames:

- "Examine Record (field-by-field)" → "Show Record (field-by-field)"
- "Next Object (any table or view)" → "Next Table or View"
- "Previous Object (any table or view)" → "Previous Table or View"
- Right-click context-menu entry "Show-Object" → "Show Record"
- HelpDialog title from Show Record → "Show Record (row N)" (was "Show-Object (row N)")
- MessageBox caption on Switch failure → "Next Table or View" (was "Switch-Object")
- The dot-prompt help text for `Show-Object` now says "In the GUI, this command is labeled 'Show Record' and is bound to Enter" (previously referred to the GUI label as Show-Object)

Same scrub applied to `DbDuo.md` (the user guide), `README.md`, and `Announce.md`. The H2 section "Show-Object and the look column" in the developer reference became "Show Record and the look column" with the canonical name still mentioned in the body.

**Dedicated scripting section in DbDuo.md and announcement in Announce.md.** v1.0.46 documented the snippet feature inline in the Misc menu section. v1.0.47 promotes it to its own H2 section, "Scripting with JScript .NET snippets", positioned between the SQL reference section and the Help menu section. The new section covers: where snippets live (`%APPDATA%\DbDuo\Snippets\`); file-type behavior (.js executes, everything else displays as reference text); the three commands (Invoke / Edit / Open Folder); the editor configuration (`[Snippets] editor=`); the `frm` and `db` globals and default imports visible to scripts; return value semantics; error handling; four sample snippets; and the power-and-responsibility note that snippets run with full DbDuo process privileges (no sandbox). The Misc-menu section now defers depth to the new H2 with a single short paragraph pointing readers downstream. The Announce.md gained a "Snippet scripting" bullet in the "What sets DbDuo apart" section — high-level only, no technical depth — plus a mention of the snippet commands in the Misc-menu line of the Top-level menu structure.

## v1.0.46

Three changes: NVDA add-on belt-and-suspenders binding for the table-navigation chords, a `.gitignore` overhaul that matches how the source tree is actually distributed, and a tagRelease script rewrite that no longer needs editing between releases.

**NVDA add-on: table-navigation chords now reliably pass through.** v1.0.40-v1.0.45 shipped the NVDA add-on with bindings registered only via `bindGesture` in `__init__`. In practice some users found those bindings not taking effect — Alt+Control+arrow still triggered NVDA's "Not in a table" instead of moving DbDuo's virtual cell cursor. The fix is belt-and-suspenders: the add-on now declares both a class-level `__gestures` dictionary (NVDA's older but most-supported binding mechanism, walked on every gesture lookup regardless of instance state) AND keeps the `bindGesture` loop in `__init__`. Both registrations point to the same `script_passThrough` method. The gesture list itself moved to a module-level `_passThroughGestures` tuple so both registrations share one source of truth. The `bindGesture` loop is wrapped in a try/except so a single malformed gesture id can no longer kill the whole `__init__` and prevent any bindings from working. Add-on version bumped to 1.0.1 so existing 1.0.0 installs see this as an upgrade rather than a re-install.

**.gitignore overhaul.** The previous .gitignore was minimal (just `bin/`, `obj/`, `*.suo`, etc.) and missed several patterns important to this project's actual release workflow. The new version: ignores transient build artifacts (`buildDbDuo.log`, `tagRelease-*.log`); explicitly ignores `tagRelease.cmd` and `tagRelease.ps1` since they are maintainer-only convenience scripts, not part of any release; ignores leftover MSBuild / NuGet scratch from the v1.0.42-v1.0.43 era (`bin/`, `obj/`, `packages/`, `DbDuo.csproj`, `App.config`) so a stale dev checkout cannot accidentally re-commit them; ignores editor backup patterns and Windows file-system clutter. Does NOT ignore `*.exe` or `*.dll` — `DbDuo.exe`, `DbDuo_setup.exe`, and `nvdaControllerClient.dll` are intentionally tracked because the project distributes them both via the source tree and via GitHub Releases.

**tagRelease.ps1: singular, version-dynamic, version-aware dirty check.** Three changes to the release-tagging script: (1) The version is now read at runtime from `DbDuo_setup.iss`'s `#define AppVersion` line rather than hardcoded — this means the script can be re-run for every future release without per-release maintenance. (2) Dropped the multi-repo `$aReleases` array and `publishOne` function; the script now operates on exactly one repo (DbDuo) and one asset (`DbDuo_setup.exe`), with everything inline. (3) The dirty-tree check now filters not just `tagRelease-*.log` but also `buildDbDuo.log`, `DbDuo.exe`, `DbDuo_setup.exe`, and `dbDuoEval.dll` — these are tracked-but-rebuilt artifacts that commonly appear as "modified" immediately after a build, which is the expected state right before a release tag. Pacific time (Seattle) is noted explicitly in the log header for the record. `tagRelease.cmd` is a thin launcher that bypass-policy-invokes the .ps1; its log-location comment was updated from `%TEMP%` to `.\` to match where Start-Transcript actually writes.

## v1.0.45

Bug-fix release on top of v1.0.44's scripting course-correction. Three changes.

**Compile fix: `IniSession` reference scoping.** v1.0.44's `SnippetHelper.getEditorCommand()` called `IniSession.read("Snippets", "editor")` as if `IniSession` were a top-level class in the DbDuo namespace. It is not — `IniSession` is a public static class nested inside `DbDuoForm`. v1.0.44's csc.exe call failed with `CS0103: The name 'IniSession' does not exist in the current context`. The fix is the qualified reference `DbDuoForm.IniSession.read(...)`. The same pattern is documented in a comment near `IniValidation.load()` (around line 6178), but I missed it when writing SnippetHelper.

**Drop Save Snippet menu item.** EdSharp's Save Snippet captures the editor's currently-selected (or whole-document) text and writes it to a snippet file. That workflow has no equivalent in DbDuo — DbDuo is a database manager, not a text editor; there is no "current selection" of text to capture. The v1.0.44 "Save Snippet" implementation just opened an empty file in Notepad, which is redundant with what the user can do from Open Snippet Folder plus Explorer's New > Text Document. Removed entirely. Alt+S is no longer bound to anything.

**Edit Snippet now covers the "create new" case too.** Picks an existing snippet if any exist; if the folder is empty, jumps straight to a Save File dialog. When the folder has existing snippets, a sentinel entry `[New snippet...]` appears at the top of the pick list (square brackets are conventional in screen-reader UI for non-data list options; they also sort before any real filename so the entry stays at the top in OrdinalIgnoreCase order). Picking that entry opens a Save File dialog seeded to the Snippets folder. Either way the chosen path is opened in the editor.

**Camel Type cleanup.** The v1.0.44 dev preview used `o` prefix for non-COM .NET object variables (`oSfd`, `oDlg`, `oPsi`, `oEx`, `oResult`, etc.). Camel Type reserves the `o` prefix for COM objects or true `object`-typed variants; for specifically-typed .NET classes the prefix should be the class name in lower camel case or a common abbreviation. Updated throughout the new snippet code: `sfd` (SaveFileDialog), `dlg` (LbcDialog), `psi` (ProcessStartInfo), `ex` (Exception), `lb` (ListBox), `dirInfo` / `aFileInfos`, `asm`, `methodInfo`, `jsType`, `result`. The JScript globals exposed to user snippets follow the same rule: `frm` (form, VB-era abbreviation) and `db` (database manager, conventional abbreviation), not `oForm` / `oDb`. The dbDuoEval.js parameter names became `frmArg` / `dbArg` so the user-script-visible scope variables `frm` / `db` are free.

**Updated snippet examples** for the JScript globals rename:

```javascript
// Count rows in current table.
db.recordCount;
```

```javascript
// Apply a filter, refresh, show the new count.
db.filter = "City = 'Seattle'";
frm.refresh();
"Filtered to " + db.recordCount + " rows.";
```

```javascript
// Trigger a form action via late-bound dispatch.
frm.recBookmarkClicked(null, null);
"Bookmark saved at row " + db.absolutePosition;
```

```javascript
// Iterate and dump first-column values.
var aLines = [];
db.moveFirst();
while (!db.EOF) {
  aLines.push(db.getFieldValue(0));
  db.moveNext();
}
aLines.slice(0, 20).join("\n");
```

## v1.0.44

A course-correction release. The Roslyn C# scripting feature from v1.0.42-v1.0.43 is rolled back; in its place is the EdSharp-style Save / Invoke / Edit Snippet pattern using JScript .NET. The change is driven by three problems with the Roslyn approach: it shipped 12 NuGet runtime DLLs totaling ~25-30 MB; it required an MSBuild + NuGet build-system migration; and it added a custom multi-textbox script dialog when standard Windows controls plus the user's own editor were the natively accessible answer all along. The new approach matches the explicit ask: standard controls, no shipped runtime DLLs beyond a tiny support assembly, no custom UI.

**Build system reverted to bare csc.exe.** `DbDuo.csproj` and `App.config` are gone. `buildDbDuo.cmd` is back to the bare csc.exe pattern from v1.0.41 with one addition: it also runs `jsc.exe` (the JScript .NET compiler, ships with every .NET Framework 4.x install at `%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\jsc.exe`) to compile a tiny `dbDuoEval.js` source file into `dbDuoEval.dll`. The C# build then takes `/reference:dbDuoEval.dll` so DbDuo.exe can call into the JScript .NET scripting engine. Two compiler invocations total. No NuGet, no MSBuild, no binding redirects.

**Install footprint dropped back to v1.0.41 levels.** The 12 Roslyn DLLs are gone from the installer's [Files] section. The new `dbDuoEval.dll` is about 10 KB. Net change from v1.0.43: ~25-30 MB removed, 10 KB added. The `[InstallDelete]` section explicitly removes the stale Roslyn DLLs and `DbDuo.exe.config` from the install folder when upgrading from v1.0.42 or v1.0.43, so the install folder reflects only what DbDuo actually uses.

**Scripting: EdSharp's Save / Invoke / View Snippet pattern, adapted for DbDuo.** The custom multi-textbox scripting dialog from v1.0.42-v1.0.43 is gone. Four new menu items appear at the bottom of the Misc menu (right above Edit Configuration):

- **Save Snippet (Alt+S)** opens a standard Save File dialog seeded to the Snippets folder. The user types a filename and extension (.js for executable snippets, .txt/.sql/anything else for reference snippets), confirms with the standard OK button. DbDuo then opens the new file in the user's editor of choice — Notepad by default; override via `DbDuo.ini [Snippets] editor=`. The editor handles all the text-entry UI; DbDuo doesn't supply one.
- **Invoke Snippet (Alt+V)** scans the Snippets folder, pops the existing standard `LbcDialog` listbox-pick UI (same one used by Choose Table, Recent Files, etc.) to let the user pick a file, then runs it. .js files are evaluated as JScript .NET via the dbDuoEval.dll support assembly; everything else is read and its contents displayed in a standard MessageBox as reference text. Script output (last expression value, or `ERROR: ...` on compile/runtime failure) is displayed in a MessageBox so the screen reader reads it through the standard dialog focus path.
- **Edit Snippet (Alt+Shift+V)** pops the same LbcDialog pick and opens the chosen file in the user's editor.
- **Open Snippet Folder** shell-executes `%APPDATA%\DbDuo\Snippets\` so the user can manage files from their preferred shell.

**Scripting language: JScript .NET.** Frozen but stable — Microsoft has not added features since 2005 but has not deprecated it either; it ships with every .NET Framework 4.x install. (This is JScript .NET, the compiled-to-CIL language, NOT the deprecated classic JScript that runs under MSScriptControl and has had its underlying engine changed in Windows 11 24H2.) Snippets have full access to the .NET Framework Class Library and to DbDuo's running form. The eval scope has two pre-injected variables:

- `oForm` — the running `DbDuoForm`.
- `oDb` — shortcut for `oForm.db` (the `DbDuoManager`).

The dbDuoEval.dll support file pre-imports `DbDuo`, `System`, `System.Collections`, `System.Data`, `System.IO`, `System.Reflection`, `System.Text`, `System.Text.RegularExpressions`, `System.Windows.Forms`. Snippets can use any type in those namespaces directly without `import` statements.

**Snippet folder location.** `%APPDATA%\DbDuo\Snippets\`. Created on first access. Lives under roaming application data so it survives DbDuo upgrades and uninstalls; the user's snippets are their data, not the application's.

**No facade between snippets and DbDuo internals.** The form's `db` field was made public back in v1.0.42 and stays public. Snippets call `oForm.<anything>` and `oDb.<anything>` directly. Internal helper methods that are private to DbDuoForm remain private — but every public method on the form is reachable, every public method on DbDuoManager is reachable, and every type in the imported namespaces is reachable. JScript's late-bound dispatch means snippets don't need to know exact .NET method signatures to invoke methods correctly.

**Example .js snippets:**

```javascript
// Count rows in current table.
oDb.recordCount;
```

```javascript
// Apply a filter, refresh, and show the new count.
oDb.filter = "City = 'Seattle'";
oForm.refresh();
"Filtered to " + oDb.recordCount + " rows.";
```

```javascript
// Open a database programmatically.
oForm.openDatabaseAndApplyState("C:\\Data\\northwind.db", null);
oDb.currentTable;
```

```javascript
// Iterate and dump first-column values.
var aLines = [];
oDb.moveFirst();
while (!oDb.EOF) {
  aLines.push(oDb.getFieldValue(0));
  oDb.moveNext();
}
aLines.slice(0, 20).join("\n");
```

The last expression of a JScript snippet is the value returned to DbDuo and shown in the MessageBox. To produce multi-line output, build a string and let it fall through as the final expression.

**.txt and other non-.js snippets** are useful for canned SQL fragments, reference notes, templated text. Invoke Snippet displays them in a MessageBox where the screen reader reads them line-by-line; the user can copy from there if they want to paste into a SQL window.

**Why this is materially better than the Roslyn v1.0.43 design.** Standard Windows controls only — no custom multi-line edit/output dialog; the user writes scripts in their own editor (which they already trust to be accessible). No 25-30 MB of shipped DLLs. No build-system complexity. Snippets persist as files rather than dialog-bound text, so the user can version them, copy them between machines, share them, edit them outside DbDuo. The EdSharp precedent confirms this pattern works well for screen-reader-driven workflows; DbDuo just inherits it.

**Compatibility note for users on v1.0.42-v1.0.43.** The Roslyn-era script dialog and Help > Run C# Script menu item are gone. If you had scripts in mind that you were going to enter via that dialog, write them as .js files in the Snippets folder instead and invoke them with Alt+V. The expressive power is the same in practice — JScript .NET has all the language features needed for the kinds of snippets DbDuo scripting is meant to support (database iteration, filter setup, form action triggering, simple data transformation).

## v1.0.43

A focused fix-up release on top of v1.0.42's build-system switch and scripting feature. Three changes, all about NVDA.

**NVDA controller DLL rename: `nvdaControllerClient64.dll` → `nvdaControllerClient.dll`.** Starting in NVDA 2026.1 (released May 6, 2026), NVDA's controller-client zip ships the 64-bit DLL inside `x64/` with the unsuffixed name `nvdaControllerClient.dll`. The architecture suffix (64 / 32 / arm64) is now derived from the folder the file lives in, not the filename. NVDA's own current C# example uses the unsuffixed name in its `DllImport` declarations. DbDuo follows suit: the three `DllImport` attributes in `LiveRegion` (testIfRunning, speakText, cancelSpeech) now reference `nvdaControllerClient.dll`. The diagnostic message printed by Test-Reader also updated. Comments throughout were updated to reflect the new naming.

**Build script DLL-extraction fix.** v1.0.42's `buildDbDuo.cmd` searched the NVDA controller-client zip recursively for a file literally named `nvdaControllerClient64.dll`. That file does not exist in NVDA 2026.1's zip. The download succeeded, the recursive search came back empty, and the build script printed "WARNING: NVDA controller DLL download failed" and continued without the DLL. The new logic looks specifically in the archive's `x64/` folder, accepts EITHER `nvdaControllerClient.dll` (modern NVDA 2026.1+) OR `nvdaControllerClient64.dll` (older NVDA 2025.x and earlier), and falls back to a recursive search if neither is in `x64/` directly. The file is always written out as `nvdaControllerClient.dll` to match the `DllImport` declarations. The warning message on failure now also points users to the upstream URL so they can fetch the file manually if their build machine is offline.

**Installer cleanup for upgraders.** A new `[InstallDelete]` section removes the legacy `nvdaControllerClient64.dll` from the install folder when upgrading from v1.0.40 - v1.0.42. The legacy file is harmless (no DbDuo code references it anymore) but cluttering. New installs are unaffected.

## v1.0.42

A two-part release: the build system switches from bare `csc.exe` to MSBuild + NuGet (no behavior change in the produced DbDuo.exe by itself), and on that new foundation the C# scripting feature lands.

**Build system: MSBuild + NuGet, replacing bare csc.exe.** Adding the Roslyn scripting package as a NuGet dependency with about a dozen transitive packages required this change — `csc.exe` alone cannot resolve transitive references or generate the binding redirects those packages need to coexist with .NET Framework 4.8's built-in `System.*` shims. The new `DbDuo.csproj` is the old-style format (not the .NET SDK-style); it lists `Microsoft.CodeAnalysis.CSharp.Scripting` 4.12.0 as the only `PackageReference` and pulls in everything else transitively. `AutoGenerateBindingRedirects = true` in the csproj tells MSBuild to write the appropriate `<assemblyBinding>` block into `DbDuo.exe.config` so the NuGet `System.Memory`, `System.Buffers`, `System.Collections.Immutable`, `System.Runtime.CompilerServices.Unsafe`, etc., resolve correctly at runtime. The new `buildDbDuo.cmd` walks the same Visual Studio 2022 / 2019 install paths the old script did but invokes `msbuild.exe` rather than `csc.exe` directly. It also bootstraps `nuget.exe` from `dist.nuget.org` if not on PATH. The NVDA controller-client DLL download step is unchanged. The compiler underneath is still `csc.exe`; MSBuild just orchestrates it.

**Install footprint grew by about 25-30 MB.** This is the honest cost of bundling Roslyn. The new `[Files]` entries cover the ~12 NuGet runtime DLLs (`Microsoft.CodeAnalysis.dll`, `Microsoft.CodeAnalysis.CSharp.dll`, `Microsoft.CodeAnalysis.Scripting.dll`, `Microsoft.CodeAnalysis.CSharp.Scripting.dll`, `System.Collections.Immutable.dll`, `System.Reflection.Metadata.dll`, `System.Memory.dll`, `System.Buffers.dll`, `System.Runtime.CompilerServices.Unsafe.dll`, `System.Numerics.Vectors.dll`, `System.Threading.Tasks.Extensions.dll`, `System.Text.Encoding.CodePages.dll`) plus the `DbDuo.exe.config` binding-redirect file. Roslyn does not slow DbDuo startup — the assemblies are JIT-compiled on first use of the scripting feature, not at launch — and the increase is bounded; no further package growth is planned.

**`nvdaControllerClient64.dll` now properly installed.** A latent bug from v1.0.40-v1.0.41: the build script placed the DLL alongside DbDuo.exe in the build folder, but the installer's `[Files]` section did not list it, so installed copies did NOT receive it. Added now. Existing v1.0.41 installs that worked on the developer's own machine but did not speak through NVDA on other machines should be upgraded.

**Help > Run C# Script (Alt+F12).** New menu item, new dialog. The dialog has a multi-line monospace script editor (TabIndex 0, accessible as "Script"), a multi-line read-only output pane (TabIndex 1, accessible as "Output"), a Run button (Alt+R), a Clear Output button (Alt+C), and a Close button. The dialog is modeless so the user can leave it open and run multiple scripts against the same database session.

**Globals exposed to scripts:**
- `oForm` — the running `DbDuoForm`. The form's `db` field is now public so scripts can reach `oForm.db` directly.
- `oDb` — shortcut for `oForm.db` (the `DbDuoManager` instance).
- `say` — `Action<string>` that calls `LiveRegion.sayForced`. Speaks through the screen reader regardless of the Extra Speech toggle.
- `sayBack` — `Action<string>` that appends a line to the dialog's output pane. The natural target for script status messages.

**Default imports:** `System`, `System.Collections.Generic`, `System.IO`, `System.Linq`, `System.Text`, `System.Windows.Forms`, `DbDuo`. The script can `using` any other namespace if it needs to.

**Error handling.** Compile-time errors (syntax, unknown identifier, type mismatch) catch as `CompilationErrorException` and the dialog appends one line per diagnostic with file/line/column info. Runtime errors catch as their actual exception type and the dialog appends `Runtime error: <TypeName>: <Message>`. The script process is not affected — the user can fix the script and Run again.

**Threading.** Roslyn scripting is async-only. The Run button wraps the eval in `Task.Run(...)` plus `.GetAwaiter().GetResult()` so the eval runs off-thread; the UI thread blocks during the eval but does NOT deadlock the SynchronizationContext as it would with a direct `.Result`. For long-running scripts the UI will be unresponsive — this is intentional simplicity; users running multi-minute scripts can fork to a `Task` inside the script itself.

**On the design choice (C# vs JScript via MSScriptControl).** I considered routing scripting through the `MSScriptControl` COM object hosting a legacy JScript engine — the same pattern EdSharp uses for JScript. JScript would avoid the 25-30 MB Roslyn footprint and would not require an MSBuild migration. Reasons against: Microsoft has been actively deprecating JScript (Windows 11 24H2 changed JScript behavior in October 2024 in ways that broke working VBScript+JScript hybrids); JScript syntax is ES3-era with none of modern JavaScript's amenities, would be a stylistic regression from the C# the host is written in; runtime-only error reporting versus Roslyn's compile-time diagnostics is much worse for screen-reader workflow where every error you hit costs review time; and "all privileges" requires the host objects to be `[ComVisible(true)]` either way, so the visibility surgery is comparable. The 25-30 MB is a real cost but a one-time cost, and pays for a feature on a foundation Microsoft is actively maintaining.

**No facade between scripts and DbDuo internals.** Scripts call `oForm.<anything>` and `oDb.<anything>` directly. No translation layer, no permission wrapper. The user runs code in their own process; this is a power-user tool by design. The only visibility change was making `DbDuoForm.db` public. The rest of the host surface is already public.

**Example uses:**

```csharp
// Count rows.
sayBack("Row count: " + oDb.recordCount);
```

```csharp
// Iterate, log first-column values.
oDb.moveFirst();
var aLines = new List<string>();
while (!oDb.EOF)
{
    aLines.Add(oDb.getFieldValue(0));
    oDb.moveNext();
}
sayBack(string.Join("\n", aLines.Take(20)));
```

```csharp
// Trigger a form action.
oForm.recBookmarkClicked(null, null);
sayBack("Bookmark saved at row " + oDb.absolutePosition);
```

```csharp
// Apply a filter via the manager.
oDb.filter = "City = 'Seattle'";
oForm.refresh();
sayBack("Filtered to " + oDb.recordCount + " rows.");
```

## v1.0.41

A focused release on NVDA parity with JAWS. The previous release wired NVDA speech (downloaded `nvdaControllerClient64.dll` during build and placed it next to DbDuo.exe) but left the NVDA add-on as a placeholder. This release ships the actual add-on.

**NVDA add-on bundled.** `DbDuo.nvda-addon` now ships in `{app}\DbDuo.nvda-addon` and the Finish-page checkbox "Install NVDA add-on for DbDuo" is checked by default. The add-on contains a `manifest.ini` (name=dbDuo, summary, version 1.0.0, minimumNVDAVersion 2019.3, lastTestedNVDAVersion 2026.1), an `appModules/DbDuo.py` Python module, and a `readme.html`. The Python `AppModule` class subclasses `appModuleHandler.AppModule` and binds 49 keyboard gestures — exactly the same set the JAWS DbDuo.jkm covers — to a single `script_passThrough` method whose body is one line, `gesture.send()`. That is the NVDA equivalent of the JAWS Function `TypeCurrentScriptKey()` that the existing DbDuo.jss/DbDuo.jsb pair calls. The chord coverage is identical: virtual-cell navigation, Alt-letter command shortcuts, parent-child drill, three search families, marked-row navigation, bulk-mark span, and the Say-X data-list chords. Without this add-on, NVDA would intercept Alt+Control+arrow for its own table-navigation commands and DbDuo's virtual-mode ListView would report "Not in a table"; with the add-on, NVDA steps out of DbDuo's way for exactly the chord set DbDuo needs.

**`--install-nvda-addon` CLI flag implemented.** The placeholder from v1.0.40 is replaced. The flag locates `DbDuo.nvda-addon` next to DbDuo.exe and opens it via `ProcessStartInfo.UseShellExecute = true`. Windows hands the file to NVDA via the `.nvda-addon` file association NVDA registers at install time. NVDA then shows its standard "Install this add-on?" dialog where the user confirms. Failure modes are handled cleanly: missing add-on file (rare; installer always places it) prints a clear message and exits non-zero; missing file association (NVDA not installed) catches the `Win32Exception` and points the user to nvaccess.org.

**Build script (no change).** `buildDbDuo.cmd` continues to download `nvdaControllerClient64.dll` from `download.nvaccess.org/releases/stable/nvda_2026.1_controllerClient.zip` and place it next to DbDuo.exe. With the add-on now installed and the DLL bundled, both halves of NVDA support (chord pass-through and direct speech) work end-to-end.

**Re-running the install later.** The same UX pattern as JAWS — to re-install the add-on after upgrading NVDA, the user double-clicks `DbDuo.nvda-addon` in the install folder, or runs `DbDuo.exe --install-nvda-addon` from a command prompt. The add-on update workflow is NVDA's standard one.

**Not in this release (deferred):** C# scripting feature. See the proposal below; that is genuinely substantial work and deserves a focused turn.

## v1.0.40

A broad polish release. Six families of changes: installer task reordering, NVDA controller-client DLL bundling, command-name consistency cleanup, dot-prompt cleanup, the Extra Speech toggle, and a new Help > Open Sample Database entry. Plus a structural move: the JAWS settings install/uninstall logic migrated from Pascal Script in the .iss to a C# `JawsSettingsInstaller` class, so the same code can be re-run from the menu without re-running the full installer.

**Post-install task list reordered.** The installer's Finish page now shows four checkboxes in this order: (1) Install JAWS settings for DbDuo (checked by default — recommended if you use JAWS); (2) Install NVDA add-on for DbDuo (unchecked — not yet implemented, returns a "planned for a future release" message); (3) Launch DbDuo now (checked by default); (4) Read the DbDuo README (checked by default). The README link is the README.htm, not the longer DbDuo.htm reference document, since most users want the brief introduction at first run. The `[Tasks]` section that previously exposed the JAWS install as a Ready-page checkbox is gone — the install is now a Finish-page action delegated to `DbDuo.exe --install-jaws-settings`. The matching `[UninstallRun]` entry calls `DbDuo.exe --uninstall-jaws-settings`.

**JAWS install logic migrated to C#.** The Pascal Script `InstallJawsSettings`, `FindScompilePath`, and `CurUninstallStepChanged` procedures (~170 lines in v1.0.39's DbDuo_setup.iss) are gone. The same algorithm is now in a `JawsSettingsInstaller` static class in DbDuo.cs: registry-first scompile.exe lookup with Program Files fallback, enumeration of `%APPDATA%\Freedom Scientific\JAWS\*\Settings\*`, copy of DbDuo.jkm and DbDuo.jss into each (version, language) folder, scompile invocation with output capture, and persistent log of placed paths at `%APPDATA%\DbDuo\jawsSettings.log`. Three new CLI flags expose the work: `--install-jaws-settings` (runs the install and exits), `--install-nvda-addon` (placeholder, prints "not yet implemented"), and `--uninstall-jaws-settings` (reads the log and removes only the files DbDuo placed). The advantage of the move: a user who upgrades JAWS to a new year-version can re-run the install manually without finding the original DbDuo installer.

**NVDA controller-client DLL bundling.** NVDA's end-user distribution does not include `nvdaControllerClient64.dll`; the DLL lives only in NVDA's source distribution at `extras/controllerClient/`. The DbDuo build script (`buildDbDuo.cmd`) now downloads the official `nvda_2026.1_controllerClient.zip` from `download.nvaccess.org/releases/stable/` via PowerShell `Invoke-WebRequest`, extracts it via `Expand-Archive`, finds the 64-bit DLL via `Get-ChildItem -Recurse`, and places it next to `DbDuo.exe`. Idempotent (skips if already present); graceful on failure (warns but continues). The DbDuo P/Invoke declarations for `nvdaController_testIfRunning`, `nvdaController_speakText`, and `nvdaController_cancelSpeech` were already in place from earlier releases — the speech-failure-with-NVDA symptom you may have observed was the missing DLL, not a code defect. With the DLL bundled, NVDA direct speech works through the same code path as JAWS speech.

**Command-name consistency cleanup.** Eleven dialog titles and MessageBox captions previously displayed the PowerShell verb-noun canonical name (e.g., `"Open-Database File"`, `"Set-Position"`, `"Save-Bookmark"`, `"Restore-Bookmark"`, `"Select-Record"`, `"Switch-Table"`, `"Step-InitialChange"`, `"New-Chart"`, `"Test-Driver"`, `"Edit-Configuration"`, `"Select-Column"`) while their menu labels used the natural-English form (`"Open Database"`, `"Go to Row"`, `"Save Bookmark"`, etc.). The mismatch was audible: a screen-reader user invoking a command from the menu and then hearing an error dialog would hear two different names for what they thought was one command. All eleven captions now match the menu labels. Three form captions also fixed: `"Select-Record (filter)"` → `"Filter Records"`, `"Sort-Object (custom sort)"` → `"Custom Sort"`, `"Show-Command"` → `"Command Picker"`. Two "not yet implemented" MessageBoxes (`Compare-Database`, `Out-Printer`) also re-titled to match their menu labels.

**First/Top label redundancy fixed.** Navigate menu items `"&First Record (top)"` and `"&Last Record (bottom)"` had parenthetical synonyms that the screen reader read aloud as additional words — "First Record top" and "Last Record bottom". The parentheticals are gone; the labels are now simply `"&First Record"` and `"&Last Record"`. Same treatment for Help menu's `"&Where Am I (Show Status)"` → `"&Where Am I"` (the parenthetical was literally the canonical command name, which adds nothing).

**Legacy dBASE dot-prompt aliases dropped.** The `resolveAlias` table previously accepted `t` / `top` / `bottom` / `b` (covered by `first` / `last`), `=` / `display` (covered by `show`), `use` / `@` (covered by `table`), and `#` (covered by `g` / `go` / `goto`). All gone; the non-dBASE-shaped aliases remain for the same actions. `skip` is retained — that verb has no clean replacement and isn't ambiguous. The matching dead branches in `cmdStepRecord`'s switch (`case "top":`, `case "bottom":`) also removed.

**Dot-prompt command echo removed.** When `Command Echo` was on, the dot prompt previously printed a `[Canonical-Name]` marker line before each command's output. That's noise — the user's own typed line plus the command's stdout output is already plenty of confirmation, and the extra line just slows screen-reader review of recent output. Command Echo is now GUI-only. The GUI's announcement path through `LiveRegion.say` is unchanged.

**Bare-Enter row summary trimmed.** Pressing Enter alone at the dot prompt previously printed `[teachers] row 8 of 25  filter: ...  sort: ...`. Three of those pieces are redundant when the user already knows which table is open and presses Enter for a quick position check. The new bare-Enter output is just `8 of 25` — table name omitted, the word `row` omitted, filter and sort omitted. The full summary is still available by typing `status` (or `?`), which calls the unchanged `printRowSummary` function. The banner at dot-prompt startup also still shows the full summary.

**"Spelling: " prefix dropped on the spell-on-double-press paths.** Both `virtSayCurrent` (Alt+Control+NumPad5 double-press) and `speakOrSpell` (Say-X family double-press) used to lead the spelled output with a literal word `"Spelling: "`. The spaced-out characters that follow are themselves a clear cue that the speech is a spelling; the prefix only added a word of noise. Now just the spaced characters.

**Two control-operation hints dropped.** The Filter Records dialog's `AccessibleDescription` was `"Type filter text, choose a column and match mode, then press OK."` and the Custom Sort dialog's was `"Choose a sort column and direction, then press OK."`. Both empty now. Screen-reader users already know how to operate standard controls; the hints were just noise read once when the dialog opened.

**Extra Speech toggle.** The EdSharp / FileDir model. A new menu item Toggle Extra Speech (Help menu, Alt+Shift+S) and matching `Toggle-Extra-Speech` dot-prompt command flip a `LiveRegion.bExtraSpeechEnabled` flag that gates every `LiveRegion.say` call. When off, DbDuo's direct speech (status announcements, command echo, virtual-cell readouts, the "DbDuo ready" startup announcement, etc.) is suppressed but the screen reader's natural focus and selection announcements still fire. The setting is persisted to `DbDuo.ini [General] extraSpeech` and loaded at form construction. A new `LiveRegion.sayForced` method bypasses the gate for two narrow cases: the Toggle-Extra-Speech command's own confirmation (so you always hear "Extra speech off" when turning it off), and the Test-Reader command (whose purpose is to verify speech). The companion `Toggle-Extra-Speech-Log` from EdSharp is not implemented in this release — the conflict-resolution work it would need isn't worth the marginal benefit.

**Help > Open Sample Database.** A new Help menu item opens `{app}\sample.db` via the same `openDatabaseAndApplyState` code path File > Open Database uses, so all the normal post-open behaviors (Recent Files entry, status announcement, etc.) apply. If the sample is missing (rare — it ships with every installer), a clean information MessageBox explains where to find it.

**Not in this release (deferred):** C# scripting feature via Roslyn. The CSharp-Scripting-Tutorial.md and the design conversation deserve a focused turn rather than a tacked-on implementation. Coming in a follow-up release.

## v1.0.39

JAWS settings install correctness fix. The v1.0.38 release shipped a JKM-only approach that turned out not to work: the JKM bindings referenced `TypeCurrentScriptKey` directly, but that name is a JAWS *Function*, not a *Script*, and JKM right-hand sides can only invoke Scripts. Loading the JKM produced "Unknown call to TypeCurrentScriptKey" from JAWS for every bound chord. This release adds the missing piece: a tiny script source file (`DbDuo.jss`) defining a one-line wrapper Script called `PassDbDuoKey`, and installer logic to compile it to `DbDuo.jsb` in each JAWS year-version's settings folder.

**Why the JKM-only approach failed.** The Freedom Scientific scripting documentation states: "JAWS first searches the application script file for the script name found in the key map file. ... When JAWS does not find the name of the script attached to the keystroke ... an unknown script call error message occurs." The JAWS Function `TypeCurrentScriptKey()` (which passes the current keystroke through to the application as if no script were running) is the right primitive, but it can only be invoked from inside a Script body — never from a JKM line directly. The standard idiom, confirmed in EdSharp's own JSS (which defines a Script called `SilentKey` that wraps the same call), is to write a one-line Script and reference it by name from the JKM.

**The new three-file approach.** Each (JAWS year-version, language) settings folder now receives three files:

- `DbDuo.jkm` — every binding's right-hand side changed from `TypeCurrentScriptKey` to `PassDbDuoKey` (49 replacements).
- `DbDuo.jss` — JAWS script source. The complete contents are three lines: `Script PassDbDuoKey ()` / `TypeCurrentScriptKey ()` / `EndScript`. No Include directives, no Use directives, no constant references — the smallest possible JSS that does the job.
- `DbDuo.jsb` — compiled binary produced by JAWS's own `scompile.exe`. Generated locally per-folder during install.

**Per-version compilation.** JSB compilation is JAWS-version-sensitive: a JSB compiled against JAWS 2024's compiler isn't guaranteed to load cleanly in JAWS 2026. The installer therefore compiles `DbDuo.jss` separately in each settings folder, using the `scompile.exe` shipped with that specific JAWS year-version. The Pascal procedure `FindScompilePath` resolves the compiler path for a given version by checking `HKLM\Software\Freedom Scientific\JAWS\<version>\Target` first (the canonical source), then falling back to `{pf}\Freedom Scientific\JAWS\<version>` if the registry value is missing. The compilation itself uses Inno Setup's standard `Exec` with `SW_HIDE` (so no console window flashes) and `ewWaitUntilTerminated` (so all three files for each (version, language) pair are in place before moving to the next). Working directory is set to the settings folder so scompile both reads the JSS from there and writes the JSB alongside.

**Uninstaller updates.** The `jawsSettings.log` file now records every JKM, JSS, and JSB path the installer wrote — three lines per (version, language) pair instead of one. `CurUninstallStepChanged` reads the log and removes each path listed, preserving exact symmetry: only files we installed are removed.

**Updated documentation.** The "JAWS settings for DbDuo" section in DbDuo.md now describes all three files, explains why the wrapping Script is needed, gives a four-line manual-install recipe for users who want to skip the installer, and notes that scompile.exe lookup falls back to the standard Program Files location if registry detection fails.

**Backward compatibility.** Users running the v1.0.39 installer over a v1.0.38 install will get the new three-file bundle plus the working JSB. There's no migration logic needed beyond what the installer already does — copying the JKM is idempotent, and the JSS / JSB are simply new files. The uninstall log overwrites any previous v1.0.38 log so subsequent uninstall removes the right set.

## v1.0.38

JAWS settings integration. JAWS has its own table-navigation chord set on Alt+Control+arrow and by default intercepts those chords before DbDuo sees them — pressing Alt+Control+RightArrow inside DbDuo without any settings adjustment gives "Not in a table" instead of moving the virtual cursor. The fix is a JAWS key map file (`DbDuo.jkm`) that tells JAWS to pass those chords directly to DbDuo, plus the same treatment for the Alt-letter command family, the search-family chords, marked-row navigation, and bulk-mark spans. This release ships the key map file and adds an opt-out checkbox to the installer that places it in the right JAWS user-settings folders automatically.

**`DbDuo.jkm` added to the bundle.** Plain-text JAWS key map. Each binding uses the special action `TypeCurrentScriptKey`, which means "send this key to the application as if no JAWS script were running." Sections covered:

- *Virtual cell navigation* (the primary reason this file exists): Alt+Control + Home / End / RightArrow / LeftArrow / DownArrow / UpArrow / PageDown / PageUp / NumPad5
- *Alt-letter command chords*: Alt+A / C / D / E / K / L / P / R / T / Y / Z plus Alt+Shift+A / D / R
- *Parent-child drill*: Alt+RightArrow / LeftArrow / Home / End
- *Three search families*: Control+F / J / F3 plus their Shift variants, and F3 / Shift+F3 dispatcher
- *Marked-row navigation*: Control+Home / End / UpArrow / DownArrow
- *Bulk-mark spans*: Shift+Home / End plus Alt+Shift variants
- *Say-X data-list chords*: Shift+D / L / T / Y / F4

The `[Keyboard Layouts]` section maps Desktop and Laptop both to the Common section, so a single key list applies regardless of which JAWS layout the user prefers.

**Installer integration.** The `DbDuo_setup.iss` script now bundles `DbDuo.jkm` and adds a `[Tasks]` entry named `jawsSettings` with the description "Install JAWS settings for DbDuo (recommended if you use JAWS)". The task is checked by default and appears on the Ready page under the heading "Screen reader integration:". Users who don't run JAWS, or who prefer to install the key map by hand, can uncheck it.

The Pascal Script `InstallJawsSettings` procedure runs at `ssPostInstall` if the task is selected. It enumerates `%APPDATA%\Freedom Scientific\JAWS\*` via `FindFirst` / `FindNext` (not registry — JAWS's registry layout has shifted across the year-numbering transitions, but the AppData folder layout has been stable since JAWS 6 in 2006). For each year subfolder it enumerates the `Settings\*` subfolders (language codes like `enu`, `esp`, `deu`) and copies `DbDuo.jkm` into each one. The list of paths actually written is logged to `%APPDATA%\DbDuo\jawsSettings.log` so the uninstaller can target exactly those paths.

**Uninstall.** The new `CurUninstallStepChanged` procedure reads `jawsSettings.log`, deletes each path it lists, removes the log itself, and tries to remove the `%APPDATA%\DbDuo` folder (which `RemoveDir` only succeeds at if it's empty — preserving any other DbDuo state files the user may have there). The uninstaller does NOT enumerate JAWS folders independently and delete every `DbDuo.jkm` it finds; it only removes the ones we installed.

**Documentation.** A new "JAWS settings for DbDuo" section in `DbDuo.md` (between Virtual cell navigation and File menu) explains the why, the what, and the how — including the manual-install path for users who want to copy the file themselves. The Installation section of `README.md` mentions the checkbox and the rationale.

**NVDA and Narrator status.** NVDA has the same chord-interception problem and would need an add-on (a `.nvda-addon` file). That work is planned for a future release. Narrator cannot be scripted at all; Narrator users will hear DbDuo's announcements layered with whatever Narrator says by default, which is less polished but functional.

**Compatibility.** Tested for JAWS 2024 and later. The folder enumeration pattern works back to JAWS 6, but older JAWS versions (pre-Unicode era) used different settings paths that this installer doesn't enumerate.

## v1.0.37

A maintenance release. Two items: code warnings cleared, and a portable-SQL-baseline section added to the reference document.

**CS0414 warnings cleared.** The v1.0.36 build produced two compiler warnings for the form-scope fields `sLastFindRegexColumn` and `sLastFindKind`, both flagged as "assigned but never used." These were legacy carryovers from the v1.0.32 search-restructure that should have been removed at the time. A related field `sLastFindCriteria` on `DbDuoForm` was in the same state (assigned once, never read). All three are now gone from `DbDuoForm`, along with their three write sites. The `DotPromptHost.sLastFindCriteria` static field is a different field in a different class and is unaffected — it remains in active use for the CLI's find-next path. The form's per-family last-term state continues to work through `sLastJumpSubstring` / `sLastFindSubstring` / `sLastFindRegex` plus `sLastSearchKind`; the removed fields were entirely redundant.

**Portable SQL baseline section added to DbDuo.md.** A new subsection at the top of the SQL reference, "A portable SQL baseline that works everywhere," explains which SQL forms run unchanged across SQLite, Access, Excel, dBASE, and CSV/TSV. The framing is honest about what "portable" actually means here: it's not "Jet 4.0 SQL works on SQLite" (which would be wrong — Jet has Microsoft-specific syntax for wildcards, string concatenation, date literals, boolean literals, and row-limiting that fails on SQLite), but rather "a careful ANSI SQL-92 subset works everywhere." The section gives worked examples for SELECT, INSERT, UPDATE, DELETE, JOIN, and CREATE TABLE in the portable subset, lists the specific portability gotchas users will hit (LIKE wildcards, concatenation, date literals, boolean literals, LIMIT vs. TOP, identifier quoting), and points to two external references for users who want the full per-dialect details:

- Microsoft Jet 4.0 / Access SQL reference at `learn.microsoft.com/en-us/office/client-developer/access/desktop-database-reference/microsoft-jet-sql-reference`
- SQLite SQL syntax reference at `sqlite.org/lang.html` (and the railroad diagrams at `sqlite.org/syntaxdiagrams.html`)

The section explicitly notes that most users will not need to go beyond the portable subset — Filter Records (Shift+F) covers the great majority of ad-hoc filtering through ADO Filter expressions without needing custom SQL at all. Run SQL exists for the cases when you do, and the per-dialect subsections below the new "portable baseline" section cover the full feature surface of each engine.

## v1.0.36

A repositioning release. After three releases of substantial accessibility work (Recent Files, the redesigned case-sensitive search dialogs, virtual cell navigation, the double-press-spells convention, the column-prompt model), the user-facing description of DbDuo had drifted out of step with what the tool actually is. The README opened with "Manage your databases by keyboard"; the About dialog described "synchronized interfaces between CLI and GUI modes"; the Announce file led with "for keyboard users of Windows." All accurate, but none of them surfaced the accessibility work to the audience most likely to value it.

This release brings the user-facing copy in line with reality. No code or behavior changes — only documentation and presentation strings.

**README.md rewritten.** The opening paragraph now leads with "An accessible, keyboard-first database manager for Windows" and explicitly names JAWS, NVDA, and Narrator. A new "Who this is for" section sits second, telling screen-reader users directly that the tool is built for them and reassuring sighted developers that the speech work doesn't get in their way (DbDuo is silent unless a screen reader is running). The "What DbDuo lets you do" section now includes the virtual cell cursor, the spell-on-second-press convention, the three search families, and Recent Files. The Quick Start tour was rewritten end-to-end to use current chord assignments — the previous version still used Shift+E for Enter-Child, Shift+X for Exit-Child, and Shift+M / Shift+U for marking, all of which were superseded in earlier releases.

**Announce.md rewritten.** Same accessibility-first repositioning. Lead paragraph names JAWS, NVDA, and Narrator. New "Who DbDuo is for" section. "What sets DbDuo apart" replaces a generic feature list with the actual differentiators: virtual table cursor, double-press-spells, three-family search with persistent history, dual-interface synchronization, Recent Files with per-table state, parent-child drill. Menu summaries updated to use natural-English menu labels (the v1.0.33 layout: File / Edit / Navigate / Query / Misc / Help).

**DbDuo.md tagline updated.** The reference document's opening tagline now matches the README opening.

**About dialog rewritten.** The Help > About DbDuo dialog (Alt+F1) now leads with "An accessible, keyboard-first database manager for Windows" and explicitly mentions screen-reader-first design, JAWS / NVDA / Narrator support, table-style cell navigation, and the double-press-spells convention. The PowerShell and ADO architecture notes are kept but moved below the user-facing description.

**Installer welcome page updated.** Inno Setup's `WelcomeLabel2` now describes DbDuo as "an accessible, keyboard-first database manager for Windows" with first-class screen-reader support, before the license note and driver-install note.

**Naming decision.** The name "DbDuo" is retained. A naming analysis weighed candidates including "DbAcc," "DbAccess," "DbA11y," "DbVoice," "DbReader," and "DbEcho" against the criteria of audience signaling, distinctiveness, screen-reader pronunciation, trademark adjacency, and database-category clarity. "DbAccess" has serious adjacency problems with Microsoft Access. "DbAcc" reads as unfinished. "DbA11y" is insider jargon that doesn't pronounce cleanly. "DbVoice" misleads about whether the tool produces TTS itself. "DbReader" implies read-only. "DbEcho" was the strongest alternative, but doesn't beat DbDuo cleanly enough to justify the costs of renaming. The conclusion: keep DbDuo as a serviceable name and let the README opening, the tagline, and the About dialog carry the audience-signaling work that a short name cannot.

## v1.0.35

A documentation release. The DbDuo reference (`DbDuo.md`) was substantially out of date — it still described the old Record / View / Schema / Tools menu structure, mentioned chord assignments that had been changed in the last three releases, and gave no coverage at all of virtual cell navigation, the case-sensitive search dialogs, or Recent Files. This release rewrites the affected sections so the reference matches v1.0.34's actual behavior.

**Updated sections in `DbDuo.md`.** Every menu section now uses the correct menu name (File / Edit / Navigate / Query / Misc / Help) and the correct visible-label DbDuo names that v1.0.33 introduced; for example, "Show-Object" is now described as "Examine Record," "Set-Mark" as "Mark Record," "Reset-Filter" as "Clear Filter," etc. The PowerShell canonical name stays current in the dot-prompt reference but no longer leaks into menu descriptions.

**New section: Virtual cell navigation.** A dedicated section between "Keyboard navigation in the data list" and the menu reference explains the Alt+Control + extended-key chord family with all eight chords laid out, the direction-aware announcement behavior (vertical move → "Row N: value"; horizontal move → "Header: value"; corner jump → "Row N, Header: value"), and the double-press-spells convention applied across the Say-X family. The section also describes the cursor's two-way synchronization with the listview row selection and how column-aware commands (Sort, Open Cell Value, Next Initial Change, Jump to Match) default to the column under virtual focus.

**Updated section: Keyboard navigation in the data list.** Now explains the three independent navigation modes — row navigation with arrows, column-announcement-only navigation with Tab, and cell-level virtual navigation with Alt+Control+arrow. The previous version incorrectly described Tab as a column-targeting mechanism for commands; that has not been true since v1.0.33. Tab is now correctly described as announcement-only with no command-targeting role.

**Updated section: Mnemonic hotkey groups.** All seven subsections refreshed:

- **Bare Shift+Letter family** now lists 5 chords (F, G, J, R, S) instead of 9 — the parent-child drill and mark/unmark pairs moved to Alt+arrow and Control+M/U respectively.
- **Function-key family** updated to cover the three search families on F3 / Control+F3 with their reverse variants, the unified F3 / Shift+F3 search-again dispatcher, and the new F11 = Check for Update binding.
- **Control-letter family** covers all three search families (Control+F, Control+J, Control+F3) with their Shift variants, plus the new Control+M / Control+U mark pair.
- **Alt-letter family** now correctly lists Alt+R as Recent Files and Alt+Shift+R as Related Records (these flipped in v1.0.33).
- **New: Alt+Control extended-key family** documents the virtual-cursor chord set with its rationale.
- **New: Alt+arrow family (parent-child drill)** explains Alt+Right / Alt+Left / Alt+Home for Enter Child Table / Exit Child Table / Exit to Root Table.
- **Navigation family** corrected to describe Tab as announcement-only and to drop references to per-cell-targeting from arrow keys.

**No code changes in this release.** Only documentation. DbDuo.cs is at v1.0.35 only to match the bundle and history-file version line; the binary behavior is identical to v1.0.34.

## v1.0.34

Virtual cell-level navigation in the data list, with screen-reader-style direction-aware announcements and the double-press-spells convention for speech-only commands.

**The virtual cursor.** A second navigation cursor — separate from the listview's row selection — that tracks a `(row, column)` pair. The listview itself only has row selection (the .NET ListView with `MultiSelect=false` has no per-cell focus concept), so the virtual cursor lives entirely in DbDuo's state and announcements. The user moves it with Alt+Control + arrow / Home / End / PageUp / PageDown chords. Each movement triggers a speech announcement of the resulting cell value, with direction-aware framing.

**Chord family.** All six Alt+Control combinations on the extended arrow / numpad cluster — per your "exception about Alt+Control combinations for this navigation family because the extended-arrow / numpad keys do not make sense for desktop shortcut hotkeys":

- **Alt+Control+Home** = top-left cell (row 1, column 0)
- **Alt+Control+End** = bottom-right cell (last row, last column)
- **Alt+Control+RightArrow** / **LeftArrow** = next / previous column, same row
- **Alt+Control+DownArrow** / **UpArrow** = next / previous row, same column
- **Alt+Control+PageDown** = last row, current column
- **Alt+Control+PageUp** = first row, current column
- **Alt+Control+Numpad5** (or Alt+Control+Clear, which is what Numpad5 maps to with NumLock off) = announce current cell

**Direction-aware announcements.** Per the JAWS / NVDA table-reading idiom:

- **Vertical move** (row changed only): say `Row N: value` — the column is implied by context, just the row index and the new value
- **Horizontal move** (column changed only): say `Header: value` — the row stays the same, hear the new column's header and value
- **Corner jump** (both changed, from Home or End): say `Row N, Header: value` — explicit on both axes
- **Repeat or refresh** (neither changed, from Numpad5 single-press): say `Header: value` — full context for the current cell

The previous row and column are tracked in `iPrevVirtualRow` / `iPrevVirtualCol`; comparison to the current row/column determines what to announce.

**Double-press spells.** EdSharp / FileDir convention: press a speech-only chord twice in succession to spell the spoken text character by character. DbDuo now does this through a shared `speakOrSpell(text, chordId)` helper that tracks the last-pressed chord identity and timestamp. A second press within 1.5 seconds of the same chord spells the text instead of repeating it. Applied to:

- **Alt+Control+Numpad5** = say or spell the current virtual cell
- **Say Status** (Alt+Z), **Say Path** (Alt+P), **Say Yield** (Alt+Y) — the per-session-state speech commands
- **Say Tables** (Shift+F4), **Say Marked** (Shift+L)
- **Say Date** (Shift+D), **Say Type** (Shift+T), **Say Yield Marked** (Shift+Y)

The spell output inserts spaces between characters so the screen reader pronounces each one separately; `space` is the literal word for whitespace; punctuation is announced as-is by most screen readers.

**Virtual cursor synchronization.** Two cursors must stay in sync to avoid confusion:

- **F5 (Refresh View)** resets virtual focus to (row 1, column 0) and resyncs the listview row selection. The user hears "Refreshed."
- **Opening a database file or switching tables** also resets virtual to (row 1, column 0).
- **Regular Down / Up arrow** moves the listview's row selection; the virtual row follows (column stays put). This is the natural case — you're scrolling through rows, the virtual cursor's row index follows.
- **Alt+Control+Arrow** moves the virtual cursor first, then syncs the listview's row selection on any row change. So when the user uses table-navigation chords, the visible selection follows them.

The two paths are mediated by `bSuppressCellChanged`: a programmatic listview-selection update sets the flag, so the cell-changed handler doesn't echo the change back into the virtual cursor or re-fire the announcement.

**Column-prompt dialogs default to the virtual column.** Per your spec, "When a command is issued that prompts for a column name, it should default to the column with virtual focus." The four affected commands now use `virtCurrentColumnName()` as the default selection in their column-picker `addPickBox`:

- **Sort Ascending / Sort Descending** (Alt+A / Alt+Shift+A) default the picker to whatever column the user just had under virtual focus
- **Next Initial Change** defaults the same way
- **Open Cell Value** defaults the same way — useful because the user can virtually navigate to a URL column, press Control+Enter, and just hit Enter on the picker to confirm
- **Jump to Match in One Column** (Control+J) uses the prior Jump column first, falling back to the virtual column, then to the first column

In every case, just pressing Enter in the picker accepts the default — making the picker effectively a confirmation step rather than a friction-adding extra click.

**Tab-announced cursor remains for screen-reader Tab-hop.** The original `iCurrentColumnIndex` field is unchanged in scope: Tab inside a focused row still hops across cell announcements (the screen-reader-friendly "what's the value here, what's the next value over" workflow). Tab does not move the virtual cursor and does not target commands. The two cursors are independent: Tab announces, Alt+Control+arrow navigates.

**Implementation summary.** New helpers added to DbDuoForm: `virtCellValue(row, col)`, `virtSyncListSelection(row)`, `virtResetToTop()`, `virtSyncFromListSelection()`, `virtMoveTo(row, col)`, `virtSayCurrent(chordId)`, `virtCurrentColumnName()`, `speakOrSpell(text, chordId)`. State: `iVirtualRow`, `iVirtualCol`, `iPrevVirtualRow`, `iPrevVirtualCol`, plus the spell-tracker pair `iLastSpeechChord` / `iLastSpeechTicks` and the `DoublePressMillis` constant (1500). Eight chord intercepts added to `ProcessCmdKey` before the dispatch routing. Eight Say-X handler call-sites switched from `LiveRegion.say` to `speakOrSpell` with unique chord IDs (101-108).

## v1.0.33

Four major user-visible improvements: the redesigned search dialogs, Recent Files, the listview-appropriate column-prompt model, and DbDuo-name menu labels.

**Search dialogs now have a Text input, a Recent list, and a Case-sensitive checkbox.** Each of the three search families — Jump to Match in One Column, Find Across All Columns, Find Regex Across All Columns — uses the same dialog layout: optional column listbox (Jump only), Text input (with the prior term as default), Recent listbox (up to 10 entries, most-recent first; `[Aa]` suffix marks entries that were case-sensitive), Case-sensitive checkbox (off by default), OK / Cancel. Selecting a Recent entry copies its text into the Text input AND sets the Case-sensitive checkbox to how that term was last used. Double-clicking a Recent entry acts as OK. Recent histories are persisted to per-user DbDuo.ini under `[RecentJump]`, `[RecentFind]`, `[RecentFindRegex]` sections; each section holds up to 10 (term, case-sensitive) pairs as `termN` / `caseN` keys. The list is move-to-front (using a term again shifts it to the top rather than appearing twice).

**Per-family last-search-term defaults.** When the user invokes Control+J the dialog's Text input contains the last Jump substring (NOT the last Find or Regex substring). Same logic for Control+F and Control+F3. The per-family state has always existed in the code; the dialog redesign just surfaces it correctly. The F3 / Shift+F3 search-again dispatcher continues to repeat whichever family was last used.

**Recent Files dialog on Alt+R.** A new File menu item ("Recent Files...") shows up to the last 10 database files opened, with the last-active table noted in the display. Selecting one reopens the file, restores the saved table (silently falling back to the first base table if it no longer exists), and applies the saved filter / sort / position for that table. Any individual restore step that can no longer apply (filter references a dropped column, position out of range, etc.) is silently skipped per the spec. Recent file state is persisted to per-user DbDuo.ini under `[RecentFile1..10]` sections; each section holds the path, last-active table, and per-table state (filter, sort, position) under indexed `tN_name` / `tN_filter` / `tN_sort` / `tN_position` keys. Capture happens in `OnFormClosing`, so closing DbDuo always saves the current view for the next session.

**Alt+R reassignment.** Last release had Alt+R as a global alias for Show-Related. With Recent Files claiming the Alt+R slot, Show-Related moves to **Alt+Shift+R** — same "R for Related" mnemonic, modified-form chord since Recent took the primary.

**Listview-appropriate column-prompt model.** Several commands previously read a "current column" from the Tab-tracked `iCurrentColumnIndex` field. This worked but had a hidden coupling that wasn't visible to a screen-reader user: pressing Tab moved an invisible cursor, then a sort command would silently use whatever column Tab had landed on. Per Jamal's spec ("the program has to prompt for a column name"), the affected commands now prompt via standard LBC dialog with `addPickBox`:

- **Sort Ascending / Sort Descending** prompt for a column
- **Next Initial Change** prompts for a column to track
- **Open Cell Value** prompts for a column whose cell to open

The Tab-announced column index still exists for the screen-reader speech helper (`announceCurrentColumn`), which speaks the column header and value as the user hops across cells in a focused row. But Tab no longer *targets* commands. Commands always prompt.

**Menu labels rewritten to DbDuo names.** Per Jamal's spec: in the menu system, the visible label uses the DbDuo (natural English) name, not the PowerShell canonical. The PowerShell name (third addItem argument) stays unchanged for dot-prompt usage, so `find` / `Find` and `sort-ascending` / `Sort-Ascending` both still work as you'd expect. The mapping follows: if a command has an EdSharp/FileDir equivalent, the menu label uses that (e.g., Get-Help → "Help Contents"); otherwise Proper Case with spaces (e.g., Show-Object → "Examine Record", Get-Property → "Table Properties", Reset-Filter → "Clear Filter", Sort-Object → "Custom Sort", Set-Mark → "Mark Record", Show-Status → "Where Am I", Trace-Command → "Toggle Key Describer Mode", Show-History → "Version History", Test-Reader → "Test Screen Reader Speech", Update-Field → "Find and Replace Across Rows"). Every menu label that previously read like a PowerShell token is now natural English. Dot-prompt usage is unchanged.

**Per-table state captured on form close.** A new `OnFormClosing` override reads the currently-open database's path, table, filter, sort, and position, and writes them to RecentFiles before exit. Failures at any step are silently swallowed so they never block form close. The next session's Recent Files dialog can use this to restore the user's view.

**SearchHistory helper class.** Public static class inside DbDuoForm with `load(section)`, `record(section, term, caseSensitive)`, `save(section, list)` methods. Move-to-front semantics; 10-entry cap; `(term, caseSensitive)` pair storage. Used by all three search dialogs.

**RecentFiles helper class.** Public static class with `loadAll()` / `recordOpen(path)` / `recordTableState(path, table, filter, sort, position)` / `saveAll(list)` methods. `FileState` and `TableState` inner classes. `openDatabaseAndApplyState(path, state)` is a shared helper used by both File > Open and File > Recent Files to apply per-table restore.

**Both helper classes designed for reuse.** They live in `DbDuo.cs` for now per Jamal's request to keep the LBC library co-located, but are written as self-contained static classes with no DbDuo-specific assumptions beyond IniSession (which is also general-purpose). Future .NET projects should be able to lift them out as-is.

## v1.0.32

A substantial reorganization of the search-command family plus a small set of new Alt+Letter mnemonics.

**Three distinct search families, each with forward / reverse chord pairs, plus a unified "search again" dispatcher.** The previous design conflated column-scoped find (which had been on Control+F) with across-all-columns find (which was missing). The new layout separates them cleanly:

- **Jump-Record** on **Control+J** (forward) and **Control+Shift+J** (reverse). Prompts an LbcDialog with a column listbox and a substring textbox. Matches case-insensitively against the chosen column only. Useful for "show me the row where Email contains 'jamal'." The dbDot SEEK / dBASE FIND heritage command.
- **Find** on **Control+F** (forward) and **Control+Shift+F** (reverse). Prompts only for a substring; matches across every visible column. The universal Office / browser Control+F idiom, "show me any row that mentions X anywhere."
- **Find-Regex** on **Control+F3** (forward) and **Control+Shift+F3** (reverse). Prompts only for a .NET regex; matches across every visible column.
- **F3** (forward) and **Shift+F3** (reverse) now repeat whichever family was most recently invoked. Internal `sLastSearchKind` tracker has values "jump", "find", or "regex"; the new `recSearchAgain` dispatcher switches on it.

The Find / Find-Regex matching logic walks the recordset row-by-row in C# (using the client-cursor recordset's O(N) memory traversal) checking every visible column for `IndexOf(...IgnoreCase) >= 0` for Find, or `Regex.IsMatch` for Find-Regex. Jump-Record uses the same walk but scoped to one column. All three preserve the original cursor position when no match is found.

The legacy column-picker on Find-Regex's dialog is gone; the spec is now "across all columns" for both Find and Find-Regex. Column-scoped regex remains reachable via `Invoke-Sql` with a `WHERE column REGEXP pattern` clause for SQLite or comparable SQL constructs.

**Menu items reorganized.** New menu items in the Navigate menu: Find / Find Previous / Jump-Record / Jump-Record Previous / Find-Regex / Find-Regex Previous / Search-Next / Search-Previous. The legacy "Jump Next" and "Jump Previous" (which were really "search-again forward/reverse" with the Jump label) are replaced by the cleaner Search-Next / Search-Previous naming since they now dispatch across all three families.

**Bare-Shift+J data-list alias retained.** Still works from the data list and routes to Jump-Record (the column-scoped command) for muscle memory continuity.

**Alt+Letter audit and four new mnemonic chords added.** ProcessCmdKey dispatches form-level chords BEFORE menu accelerators are processed in WinForms, so even Alt+Letter chords whose letter is a main-menu accelerator (Alt+F for File, Alt+E for Edit, Alt+P for Help) can host commands — but to keep the chord layout clean and documentable, the new bindings all use letters that aren't main-menu accelerators. The main menus use F / E / N / Q / M / P; the new chords below use other letters.

- **Alt+R** = Show-Related (R for Related)
- **Alt+T** = Measure-Table (T for Table)
- **Alt+C** = New-Chart (C for Chart)
- **Alt+L** = Show-Table (L for List)

These are global aliases for commands whose canonical menu home was a no-hotkey menu item; the Alt+Letter chord gives them keyboard reach.

**Alt+F = Reset-Filter alias dropped.** Last release's v1.0.29 had added Alt+F as a global alias for Reset-Filter to "round out the F-modifier family," but the actual ergonomic case for it was thin — the bare Shift+R chord from the data list is sufficient — and the chord collides with the File menu accelerator on the visible-menu-accelerators reading. Dropping it leaves Alt+F doing what it does in every other Windows app: opening the File menu.

**CLI alias table expanded for the new search canonicals.** `f` / `find` → `find` (across-all-columns substring, was `jump-record`); `j` / `jump` → `jump-record` (one-column substring); `find-previous`, `find-next`, `find-again`, `again`, `previous-match` route to the appropriate canonical. The CLI dispatcher gains `case "find"`, `case "find-previous"`, `case "search-next"`, `case "search-previous"` entries. Existing CLI find handlers (which use ADO Find LIKE syntax for the dot-prompt's text-only workflow) continue to handle these — the column-listbox dialog is a GUI affordance that doesn't fit the dot prompt.

## v1.0.31

Cleanup of the bare-Shift+Letter chord family. Last release's v1.0.30 introduced Alt+RightArrow / Alt+LeftArrow / Alt+Home for the parent-child drill commands, but mistakenly kept Shift+E and Shift+X registered as data-list aliases. The point of the Alt+arrow chords was to *replace* the bare-Letter chords on E and X, freeing those slots for future commands. Likewise for Set-Mark / Clear-Mark on Control+M / Control+U: the legacy Shift+M / Shift+U aliases were kept by mistake and have now been removed.

**Four bare-Shift+Letter slots freed for future commands.** Shift+E, Shift+M, Shift+U, and Shift+X are now unbound and reserved. The bare-Shift+Letter family now uses only five letters from the data list: **Shift+F** for Select-Record (filter), **Shift+G** for Set-Position (go to row), **Shift+J** for Jump-Record (find), **Shift+R** for Reset-Filter, **Shift+S** for Sort-Object (custom sort). Each of these has a primary command whose canonical chord uses a different modifier; the bare-Shift+Letter chord is a data-list-only ergonomic shortcut. The freed E, M, U, X slots remain available for future mnemonic-driven additions without disturbing existing convention.

**Menu labels cleaned up.** The "(or Shift+E from data list)" and "(or Shift+X from data list)" notes added in v1.0.30 are gone, since those chords no longer exist. Same for the "(or Shift+M from data list)" / "(or Shift+U from data list)" notes on the Set-Mark / Clear-Mark menu items. Each menu label now states only its canonical chord.

**Confirmed chord summary for the parent-child drill family:**

- **Alt+RightArrow** = Enter-Child (drill into related child rows)
- **Alt+LeftArrow** = Exit-Child (return to parent row, one level)
- **Alt+Home** = Exit-ChildToRoot (pop entire drill stack)

And for the row-marking family (single-row operations):

- **Control+M** = Set-Mark (mark current row)
- **Control+U** = Clear-Mark (unmark current row)

The bulk marking family (multi-row span operations) was already complete in v1.0.30 and is unchanged: Shift+DownArrow / Shift+UpArrow / Alt+Shift+DownArrow / Alt+Shift+UpArrow for tag-and-move; Shift+Home / Shift+End / Alt+Shift+Home / Alt+Shift+End for span tagging; Control+A / Control+Shift+A / Control+I for all-tag / all-untag / invert-tag.

## v1.0.30

Four related improvements focusing on the parent-child drill family, the marking chord pair, and Elevate-Version robustness.

**Parent-child drill family moved to Alt+arrow chords.** Enter-Child is now on **Alt+RightArrow**, Exit-Child on **Alt+LeftArrow**. A new **Alt+Home = Exit-ChildToRoot** command pops the entire drill stack and returns to the topmost ancestor in one step, with a confirmation announcement of how many levels were popped ("Returned to root (3 levels)"). The arrow chords are global; they work from anywhere in the form. The bare Shift+E / Shift+X chords remain as data-list-only aliases via the grid-keydown switch, preserving the bare-Shift+Letter family parity with FileDir. Menu labels mention the alternate chord for discoverability: "Enter-Child (drill to related child rows; Shift+E from data list)..." and similar for Exit-Child.

**Set-Mark / Clear-Mark pair moved to Control+M / Control+U** for chord symmetry. Previously Shift+M / Shift+U, which worked but had an asymmetric feel — marking and unmarking should be parallel operations with parallel chords. Control+M / Control+U gives that parallelism: mark and unmark differ only by the letter, not by an added modifier. The bare Shift+M / Shift+U chords remain registered in the grid-keydown switch as data-list aliases, so muscle memory from earlier versions still works. Mentioned in the menu labels: "Set-Mark (current row; Shift+M from data list)" and similar.

**Control+E and Control+Shift+E freed.** Both chord slots are now unbound for future use. Extract-Regex (which previously occupied Control+Shift+E) moved to **Alt+E** — "E for Extract" mnemonic preserved, just with the Alt modifier instead. Control+E was already free in the menu system but had no global alternative; the move now leaves both quadrants of the E family available.

**Elevate-Version hardened with scrape fallback.** The GitHub Releases REST API at `https://api.github.com/repos/JamalMazrui/DbDuo/releases/latest` is genuinely public — no credentials required — and returns standard JSON. However, GitHub's unauthenticated rate limit is 60 requests per hour per IP, which is plenty for one user invoking Elevate-Version once a week but could trip on shared / corporate / VPN IPs. As a fallback, Elevate-Version now also implements an HTML-scrape path: hit `https://github.com/JamalMazrui/DbDuo/releases/latest` with `AllowAutoRedirect=false` and read the 302 Location header, whose final URL segment is the version tag (the GitHub site redirects `/releases/latest` to `/releases/tag/v1.0.30` or similar). The fallback uses no credentials and is not subject to the REST API rate limit. The API path is tried first; on any failure (network error, 403 rate-limit, malformed JSON, empty `tag_name`), the code falls through to the scrape path silently.

**FileDir-style marking family confirmed complete.** Audit of the existing chord set against the FileDir tag conventions shows every chord already implemented: Shift+DownArrow = Mark and next, Shift+UpArrow = Mark and previous, Alt+Shift+DownArrow = Unmark and next, Alt+Shift+UpArrow = Unmark and previous, Shift+Home = Mark to top, Shift+End = Mark to bottom, Alt+Shift+Home = Unmark to top, Alt+Shift+End = Unmark to bottom, Control+A = Mark all, Control+Shift+A = Unmark all, Control+I = Invert marks. The `>` and `<` aliases for Mark-and-Next / Unmark-and-Next (FileDir's keyboard shorthand) are also wired. No additions needed here — only the Set-Mark / Clear-Mark pair (which is the single-row operation) was updated.

## v1.0.29

Five related improvements that round out the EdSharp / FileDir alignment and the keyboard ergonomics of the F-modifier family.

**The F-modifier family is now complete without File-Find.** Following Jamal's "no Alt+Control combinations for in-app commands" rule (Alt+Control is reserved for Windows global hotkeys, like the desktop shortcut Alt+Control+D), the in-app F-family uses four chords: **Control+F = Jump-Record** (universal find idiom), **Control+Shift+F = Find-Regex** (regex variant), **Shift+F = Select-Record / Filter** (bare-Shift+Letter from the data list), **Alt+F = Reset-Filter** (the natural "undo filter," works anywhere). Alt+Shift+F is deliberately unbound for future use. The File-Find proposal from earlier planning was dropped — its function is covered by the existing Open-Database / Switch-Table picker on F4.

**Command Echo setting.** A new `[Options] commandEcho` setting in DbDuo.ini (default Y) controls whether DbDuo announces each command's canonical name as it runs. EdSharp's `ExtraSpeech=Y` is the model. In the GUI, the announcement goes through the LiveRegion so the screen reader speaks the command name before the command body executes; in the CLI, the dispatcher prints a `[Step-Record-First]`-style marker line before running. The setting is toggleable through the Edit-Configuration dialog (F12) — a new checkbox sits next to the uiMode picker, with focus-tip status text describing what the option does. Takes effect immediately on save; no restart needed. The cache is invalidated when the user changes the value, so the next command reflects the new setting.

**Every command reachable from the dot prompt by both PowerShell and DbDuo names.** Most commands already had this through their canonical Verb-Noun names; the gaps were the casual single-word forms users expect. Added aliases include `lock`, `new`, `save`, `import`/`in`, `compare`/`diff`, `print`, `chart`, `config`/`settings`, `console`, `web`/`website`, `folder`/`explorer`, `log`, `reader`, `commands`/`command-picker`, `about`, `readme`, `cell`, `update`/`replace`, `initial-change`/`initial`, `switch`/`next-table`, `prev-table`, `next-object`, `prev-object`, `elevate`/`update-app`, `restore`. CLI dispatch entries added for the canonicals `about-dbduo`, `elevate-version`, `show-readme`, `show-log`, `open-website`, `open-filefolder`, `enter-console` with corresponding `cmd*` handler functions. The rule per Jamal's guideline: aliases include the first letter, first word, or first two words of a longer command where the abbreviation is unambiguous.

**`[ConnectStrings]` section in DbDuo.ini.** A new section maps each supported file extension to the ADODB connection-string template DbDuo will use. The `{path}` placeholder is replaced with the full file path at connection time; `{folder}` is replaced with the directory containing the file (used by drivers that open a folder of files rather than a single file, such as the Jet text driver and dBASE). The shipped template documents the defaults for every extension — db / sqlite / sqlite3 (ch-werner SQLite ODBC), mdb / accdb (ACE OLEDB), xls / xlsx / xlsm (ACE Excel), dbf (ACE dBASE), csv / tsv (ACE Text). The DbDuoManager code consults the [ConnectStrings] section first (per-user file then shipped template) and falls back to the hard-coded defaults if the section is missing or empty — so existing installations keep working without configuration. Advanced users can switch SQLite to a different ODBC driver name, change Excel's IMEX setting for stricter type detection, or otherwise customize, all without recompiling.

**Elevate-Version command on F11.** EdSharp's F11 and FileDir's F11 are the model. The command checks the GitHub Releases API for the latest release tag, compares it to the locally compiled `BuildInfo.VersionString`, and if newer, downloads `https://github.com/JamalMazrui/DbDuo/releases/latest/download/DbDuo_setup.exe` to TEMP and launches it. The Inno Setup installer detects the running DbDuo (via the existing `Local\DbDuo.SingleInstance` mutex, now declared as the installer's `AppMutex`) and offers to close the running process before continuing, then re-launches DbDuo after installation completes (`CloseApplications=yes`, `RestartApplications=yes`). The user gets three confirmation steps: the version-found dialog, the download-and-run dialog, and Inno Setup's own close-the-running-app dialog. The TLS 1.2 fix is applied before the API call (.NET 4.8 defaults to a mix of TLS versions; GitHub requires 1.2+). A no-Newtonsoft regex parses `tag_name` from the JSON response.

**Menu-label "..." convention applied.** Per Jamal's reminder of the Windows convention: menu items leading to dialogs requiring input use a trailing "...", items that fire immediately do not. Audit of the existing menu labels found two that needed the suffix: **Show-Command (alternate menu)** opens a picker, so now reads "Show-Command (alternate menu)...". **Elevate-Version (check for an update)** opens a confirmation dialog before downloading, so now reads "Elevate-Version (check for an update)...". Every other menu label was already in the correct form.

**Menu rebalance after these changes.** Final counts after the v1.0.29 additions: File 14, Edit 11, Navigate 11, Query 17, Misc 13, Help 12. Still in the comparable-count range Jamal asked for, with Help gaining one item (Elevate-Version) and the rest unchanged.

## v1.0.28

Three small but useful changes: a build-fix release that also adds an extensive SQL reference section to the user manual and tightens command-name conformance to the EdSharp/FileDir 2-3-word convention.

**Build fix.** The new `case "yield":` alias I added for the speech-only `say-yield` command in v1.0.27 collided with the long-standing `case "yield":` for `measure-table` (the count-rows command from the dbDot heritage). C# rejects duplicate switch-case labels as CS0152, breaking the build. The fix drops my new alias and keeps the existing chain: typing `count`, `y`, or `yield` at the dot prompt continues to give the verbose multi-line measure-table output; typing `say-yield` explicitly gives the one-line speech-only summary. Both commands remain in the codebase serving different purposes.

**Command-name 2-3-word conformance.** Two commands had bare single-word menu labels or canonical names, out of step with the EdSharp / FileDir convention where every command is 2-3 words (EdSharp uses "Exit EdSharp" / "About EdSharp"; FileDir uses "Exit FileDir" / "About FileDir"). Fixed by renaming: "Exit" → "Exit DbDuo" in the File menu label (canonical command name stays `Exit-Application`); "About" → "About DbDuo" in the Help menu, and the canonical command name becomes `About-DbDuo`. Every other DbDuo command was already 2-3 words via PowerShell Verb-Noun naming.

**SQL reference section in DbDuo.md.** A new section, "SQL reference: what Invoke-Sql actually runs," documents the dialect surface for each of DbDuo's four backends. The headline takeaway: SQLite gives you the **full modern SQL surface** — window functions with OVER, CTEs including recursive, UPSERT, JSON1, RETURNING, all of it — because the ch-werner ODBC driver is a pass-through to the SQLite library itself. There is no JET 4.0 limit on SQLite operations; JET is only involved for Access, Excel, dBASE, and CSV/TSV files.

The section also documents the SELECT vs Execute code path: SELECT-shaped statements open an ADODB.Recordset which DbDuo materializes into a `List<List<string>>` via the C# loop `while (!oRs.EOF) { ...oRs.Fields[i].Value...; oRs.MoveNext(); }`; INSERT/UPDATE/DELETE/DDL run via `oConn.Execute(sql, out iAffected, adExecuteNoRecords | adCmdText)` and announce the affected-row count. The captured result grid is fully materialized in C# memory and can be redirected with `tee` / `output` to a file from the dot prompt.

Access SQL, Excel SQL, and dBASE SQL each get their own subsection covering the gotchas: Access uses IIF instead of CASE, `*` and `?` instead of `%` and `_` for LIKE, `&` instead of `||` for concatenation, hash-delimited dates `#2025-05-12#`. Excel adds the `$` suffix to sheet names and the HDR=Yes / IMEX=1 type-coercion gotchas. dBASE keeps the 10-character identifier limit. CSV and TSV get a SELECT-only Jet text driver. The section closes with a practical recommendation: for non-trivial analytical SQL, work in SQLite; reserve the other backends for genuine files of that type.

## v1.0.27

This release adopts as many EdSharp / FileDir command and hotkey conventions as practical for DbDuo's domain. The headline changes are a new speech-only command family on the Query menu, the universal Control+F find chord, the EdSharp Key Describer convention on Control+F1, and FileDir's Extract-Regex and Shift+Letter additions.

**Speech-only command family.** Eight new commands speak a piece of state through the LiveRegion without changing focus, selection, or recordset position. Each mirrors FileDir's "Say X" pattern: Say-Status on Alt+Z (table, row position, filter, sort), Say-Path on Alt+P (database file path), Say-Yield on Alt+Y (row count and filter), Say-Tables on Shift+F4 (session-visited tables), Say-Marked on Shift+L (list look-values of marked rows), Say-Date on Shift+D (updated value of current row), Say-Type on Shift+T (table or view name with row position), Say-YieldMarked on Shift+Y (count of marked rows). All live on the Query menu — your suggestion that Query was the right home for speech-only commands.

**Control+F = Jump-Record.** The universal Find chord across Office, browsers, and File Explorer. Bare Shift+J remains as a secondary alias for muscle memory and parity with the bare Shift+Letter data-list family. Control+Shift+F is the regex variant (Find-Regex). Shift+F is still Filter (Select-Record); Shift+R is still Reset-Filter. The F-family is fully populated: Control+F find, Control+Shift+F regex find, Shift+F filter, Shift+R reset filter, F3 / Shift+F3 find-again, Control+F3 / Control+Shift+F3 regex find-again.

**Control+F1 = Trace-Command toggle (Key Describer).** EdSharp and FileDir both use Control+F1 for "Key Describer" — a mode where pressing any key announces the chord and its bound command without firing the command. DbDuo's Trace-Command does the same thing; previously it was on Alt+Control+F1, now on Control+F1 by convention. Show-Status / "Where am I" still has a menu home but no longer occupies the convention slot — its function is available via `?` (Show-Where) and Alt+Z (Say-Status).

**Extract-Regex on Control+Shift+E.** EdSharp and FileDir both use Control+Shift+E for "Extract with Regular Expression." DbDuo's Extract-Regex walks every visible row, finds every regex match across every visible column, and copies all matches to the clipboard (one per line). Useful for pulling email addresses, URLs, IDs, or any pattern out of free-text columns without writing SQL. Lives on the Misc menu.

**Six new bare Shift+Letter chords matching FileDir.** Shift+A = Copy-Row (current row as TSV to clipboard, parallels FileDir's Append-to-Clipboard); Shift+D = Say-Date (parallels FileDir's Say-Date); Shift+I = Step-InitialChange (next row whose current column starts with a different letter, parallels FileDir's Initial-Change); Shift+L = Say-Marked (parallels FileDir's List-Tagged); Shift+T = Say-Type (parallels FileDir's Say-Type); Shift+Y = Say-YieldMarked (parallels FileDir's Yield-Tagged). The bare Shift+Letter family is now 15 letters strong (A, D, E, F, G, I, J, L, M, R, S, T, U, X, Y).

**Menu rebalance.** The Query menu was getting heavy with speech-only commands added; Measure-Table, New-Chart, Select-Column, Extract-Regex, Copy-Row, and Step-InitialChange moved to Misc. Query is now read-only-inspection commands (Show-*, Get-Property, Say-*, Select-Record/filter, Sort family). Misc is utilities and operations that do things. Counts are now approximately balanced: File 14, Edit 11, Navigate 11, Query 17, Misc 13, Help 11.

**The Step-Record "first" alias fix from v1.0.26** is preserved. Typing `first`, `top`, `t`, `last`, `bottom`, `b`, `next`, `n`, `+`, `previous`, `p`, `-` at the dot prompt all do what you'd expect.

**Shift+F4 changed.** It was Select-View (focus-changing picker). Now Say-Tables (speech-only, no focus change), matching FileDir's "Say Windows Open" on the same chord. Select-View remains accessible from the File menu without a hotkey; Switch-Object (Control+F6) is the recommended way to cycle through all tables and views including views.

**DbDuo-Commands.md retired.** That document had grown stale and overlapped with DbDuo.md and the alignment analysis in EdSharp-FileDir-Shared.md. Removed from the bundle.

## v1.0.26

Top-level menu reorganization to match the FileDir / EdSharp convention: **File** / **Edit** / **Navigate** / **Query** / **Misc** / **Help**. Every existing leaf command is reparented under one of these six menus, grouped by purpose. The old File / Record / View / Schema / Tools / Help layout is retired.

**File** holds database-file operations (New, Open, Save As, Close, Backup, Compare, Import, Export, Print) and the cross-file navigation cluster (Select-Table on F4, Select-View on Shift+F4, Switch-Table on Control+Tab, Switch-TablePrevious on Control+Shift+Tab, Switch-Object on Control+F6, Switch-ObjectPrevious on Control+Shift+F6, Exit on Alt+F4). This matches EdSharp's File menu, which carries Current Windows (F4) by the same logic.

**Edit** holds the data-modifying commands: New-Record (Control+N), Set-Record (F2), Remove-Record (Control+D), Copy-Record (Control+Shift+C), Update-Field (Control+R), plus the marks family (Set-Mark on Shift+M, Clear-Mark on Shift+U), the bookmarks family (Save-Bookmark on Control+K, Restore-Bookmark on Alt+K, Clear-Bookmark on Control+Shift+K), and Open-Cell (Control+Enter). The bookmark chord family matches EdSharp exactly.

**Navigate** holds movement commands: Step-Record-First, Step-Record-Last, Step-Record-Next, Step-Record-Previous (each with a menu home for discoverability; CLI equivalents remain `first`, `last`, `next`, `previous`). Set-Position on Shift+G for go-to-row. The Find / Jump family on Shift+J (Jump-Record), F3 (Jump-RecordAgain), Shift+F3 (Jump-RecordPrevious), Control+Shift+J (Find-Regex), Control+F3 (Find-RegexAgain), Control+Shift+F3 (Find-RegexPrevious) — matches EdSharp's find chords. Enter-Child (Shift+E) and Exit-Child (Shift+X) for parent-child drill.

**Query** holds read-only inspection commands: Show-Object on Enter, Get-Property on Alt+Enter, Show-Related, Show-Schema. Measure-Table for row count and stats. New-Chart for Excel frequency charts. Then the filter / sort / column-visibility cluster: Select-Record on Shift+F (filter), Reset-Filter on Shift+R, Sort-Object on Shift+S, Sort-Ascending on Alt+A, Sort-Descending on Alt+Shift+A, Sort-OldestFirst on Alt+D, Sort-RecentFirst on Alt+Shift+D, Reset-Sort, Select-Column. The sort-by-column chord family mirrors FileDir's Alt+A / Alt+Shift+A / Alt+D / Alt+Shift+D / Alt+S / Alt+Shift+S / Alt+T / Alt+Shift+T column-sort family.

**Misc** is the utility menu: Update-View on F5, Toggle Lock on Control+F7, Invoke-Sql on Control+Q, Test-Database, Test-Driver, Open-FileFolder on Alt+Backslash, Enter-Console on Control+GraveAccent, Edit-Configuration on F12.

**Help** is essentially unchanged, plus a new History entry: F1 (Help Contents), **Shift+F1 (History of Changes)** — new, matching EdSharp/FileDir convention; Show-Readme; PowerShell Verb Reference; Alt+F10 (Show-Command alternate menu); Control+F1 (Show-Status / where am I); Test-Reader; Alt+Control+F1 (Trace-Command toggle); Show-Log; Open-WebSite; Alt+F1 (About).

**Step-Record alias bug fixed.** Typing `first` at the dot prompt no longer silently moves to the next row. The compound-alias collapse that mapped `step-record-first` to bare `step-record` was dropping the direction argument; the dispatcher now explicitly handles each compound verb with its direction. The CLI commands that move you around: `first` (or `top`, or `t`) for the first row, `last` (or `bottom`, or `b`) for the last, `next` (or `n`, or `+`) for forward one row, `previous` (or `p`, or `-`) for back one row, `g 5` (or `goto 5`) for absolute row 5, plain `5` for relative jump (positive forward, negative backward).

**Release notes moved out of README.md.** Previously every release's "What's new" section accumulated in README.md. They now live in this History.md file (rendered to History.htm next to the EXE). README.md stays focused on what DbDuo is right now and how to install it, without growing forever.

## v1.0.24

This release tightened the installer's shortcut policy, cleaned up the CLI alias table to align with dbDot.vbs, and upgraded the Record edit dialog with type-aware validation and configurable regex constraints.

**Single shortcut policy.** The installer now creates exactly one shortcut: a desktop entry bound to Alt+Control+D. No Start Menu folder, no GUI-only or CLI-only variants. The shortcut launches DbDuo if not running, and brings the existing instance to the foreground on subsequent presses (single-instance handoff via `-activate`). The user reaches GUI-only or CLI-only modes from inside DbDuo via Misc > Edit-Configuration, not from external launchers.

**CLI alias table aligned with dbDot.vbs.** Every retained alias is either a single letter, a single word, or a hyphenated full-word combo. Vowel-dropped abbreviations are gone: type `previous` not `prev`, `delete` not `del`, `display` not `disp`, `properties` not `props`, `average` not `avg`, `calculate` not `calc`, `replace` (removed entirely — there's no dbDot equivalent). Legacy dBASE aliases that overlap with a dbDot alias are dropped (e.g., dBASE's `locate` gives way to dbDot's `find`). The result is a smaller, more predictable set that a dbDot user can already type.

**Type-aware Record edit dialog.** New-Record and Set-Record now validate field values against their declared types before committing. Integer columns reject non-integer text with a clear "expected an integer, got 'X'" message; real-number columns parse with thousands-separator tolerance; date-time columns parse and re-format to ISO `yyyy-MM-dd` (or `yyyy-MM-dd HH:mm:ss` for datetime). Validation failures keep the dialog open with focus on the offending field; corrections take a single Tab and retry.

**Regex field validation from DbDuo.ini.** A new INI schema lets you declare a regex constraint for any column of any table:

```
[Validation:students]
email = ^[^@\s]+@[^@\s]+\.[a-z]+$
year = ^(Freshman|Sophomore|Junior|Senior)$
```

The record edit dialog refuses OK if a non-empty value doesn't match. The error message names the field and shows the required pattern. Constraints are scoped per table, so the same column name can have different regexes in different tables.

**Inline single-line field layout.** Single-line columns in the record edit dialog now use a "Label: ____textbox____" layout where the label and the textbox share one row. Multi-line memo columns keep the label-above layout. The two layouts are intentional: single-line fields read fast under JAWS (one Tab stop announces "Label: value"); multi-line memos need the row width for the tall textbox.

## v1.0.22

This was a hotkey alignment release. The headline changes were unified Find-again, FileDir-style marked-row navigation, and bulk-marking span chords.

**F3 and Shift+F3 now repeat whichever Find variant was last invoked.** Earlier versions tied F3 to plain Jump-Record only and Control+F3 to Find-Regex. EdSharp's convention is to have one pair of find-again chords handle both kinds based on what was last fired; DbDuo now matches. The dedicated Control+F3 / Control+Shift+F3 chords still force a regex repeat specifically.

**Marked-row navigation moved to FileDir's Control+Home/End/UpArrow/DownArrow family.** Earlier versions used Alt+Home/End/UpArrow/DownArrow as a workaround for DataGridView's reserved Control+arrow chords. The switch to ListView made the cleaner Control+ family available, so DbDuo now follows FileDir's tagged-record convention directly. The ListView's MultiSelect=false + FullRowSelect=true configuration means Control+Home/End behave like Home/End natively (Control is a no-op) and Control+Up/Down only move the focus indicator without changing the selected row — invisible to screen readers — so commandeering these chords doesn't break anything observable.

**New bulk-mark chords: Shift+Home, Shift+End, Alt+Shift+Home, Alt+Shift+End.** Mark every row from the first row through the current row (Shift+Home) or from the current row through the last (Shift+End); unmark the same spans with Alt+Shift+Home and Alt+Shift+End. FileDir uses the same chord family for tagging spans of files; DbDuo applies it to record-level marking. Each operation announces "Marked N rows" or "Unmarked N rows; M already in that state" through the live region.

## v1.0.21

This release shifted the most-used commands from the Control-letter family to bare Shift+Letter chords that fire only from the data list, plus a substantial set of new features.

**Bare Shift+Letter one-key shortcuts from the data list.** Nine commands you reach for constantly are now a single keypress. Shift+E enters a child table; Shift+X exits one. Shift+F filters; Shift+R resets the filter. Shift+J jumps to a row by criteria; Shift+G goes to a row number. Shift+M marks the current row; Shift+U unmarks it. Shift+S sorts by an arbitrary expression. Lowercase letters in the same range still navigate type-ahead, since the Shift+Letter chords are only intercepted when the data list has focus.

**Show-Object with automatic Related Records.** Press Enter on a row to open a read-only view of every visible field as `name = value`, plus an automatic Related Records section. A teacher row shows the teacher's classes; an enrollment row shows the student and the class it ties together. The related records use each table's `look` summary column so a short line identifies each one. The lookup uses real `SELECT look FROM <child> WHERE <fk> = <value>` queries through the live ADO connection, so any FK indexes in the schema are used. Up to 25 related rows per table show; an `(... N more)` footer signals truncation, and Enter-Child opens the full child list.

**Schema-driven multi-line edit fields.** SQLite columns declared `textmemo` (case-insensitive, in CREATE TABLE) now get a tall multi-line text box in the New-Record and Set-Record dialogs; columns declared `textline` (or any non-memo text) get a single-line text box. The decision comes from `PRAGMA table_info`, so the schema controls the UI without any per-column hand-tuning. SQLite stores `textline` and `textmemo` with the same TEXT affinity as plain `TEXT`, so the convention is purely a hint that round-trips through the schema metadata. Access tables get the same distinction via ADOX `Type` codes (`adLongVarWChar` = memo).

**Markdown round-trip.** Export-Data to a `.md` file now produces a GitHub-flavored Markdown table that you can paste into a README, an issue, or a chat message. Import-Data reads that same format back in, matching header cells to columns by name, decoding `<br>` to newline and `\|` to a literal pipe. Hand a table to a colleague, get an edited one back, append the changes.

**Every input format is also an export format.** Export-Data writes to SQLite, Access, and dBASE as well as the existing xlsx, docx, HTML, Markdown, csv, and tsv. The SQLite, Access, and dBASE paths open a separate ADODB connection to a fresh file, issue `CREATE TABLE` with portable text-typed columns, and INSERT row by row, without disturbing your open recordset.

**Smart defaults across every file dialog.** Each kind of file dialog (Open, Save, Import, Export) remembers the folder you last used and opens there next time. Save-As suggests `<original>-copy` as the filename so a stray Enter doesn't overwrite the source; Backup suggests `<original>-backup-yyyyMMdd`. Export pre-fills the filename with the current table's name. Folders persist across DbDuo runs in a `[Folders]` section of the per-user `DbDuo.ini`.

**Edit-Configuration dialog and Open-WebSite menu item.** The new Misc > Edit-Configuration command (F12) opens a small dialog for changing the most common settings, with one tunable per row, focus-tip hints in a status bar at the bottom of the dialog, and an "Open file..." button that falls back to raw .ini editing for advanced settings. The new Help > Open-WebSite command launches the DbDuo GitHub page in the default browser.

**Custom executable icon.** DbDuo.exe ships with a multi-resolution icon (16/24/32/48/64/128/256 pixels) showing a blue database cylinder above a dark console with a `>_` prompt, on a soft slate plate. The same icon is used by the installer, Start Menu entries, taskbar, and Alt+Tab list.

**Layout by Code dialog framework.** A new `LbcDialog` class provides a reusable widget-by-widget dialog builder. Common patterns now compose as: construct the dialog with a title, call `addInputBox` / `addMemoBox` / `addCheckBox` / `addComboPickBox` / `addPickBox` / `addNumericUpDown` once per row (each call optionally taking a focus-tip string), then `runOkCancel()`. Each control is registered by name so handlers can retrieve siblings without parameter passing, and a focus-tip status bar announces the tip via UIA when the user tabs through. Used by Edit-Configuration and the rebuilt New-Record / Set-Record dialogs.

## Earlier releases

Earlier versions of DbDuo are tracked in the project's git history. The major milestones, in order: 1.0.5 first dual-interface release (CLI + GUI sharing one ADO connection); 1.0.10 ADODB recordset filter and sort wired through; 1.0.15 multi-format Import-Data / Export-Data (xlsx, docx, csv, tsv, md, htm); 1.0.18 Show-Object with Related-Records section; 1.0.20 schema-driven textline / textmemo edit fields and FK auto-index creation; 1.0.21 the bare Shift+Letter family and the Layout by Code dialog framework. Versions 1.0.23 and 1.0.25 were internal build-fix releases.
