# DbDo Reference

**An accessible, keyboard-first database manager for Windows.** DbDo opens SQLite, Microsoft Access, Excel, dBASE, and delimited-text files through one consistent set of PowerShell-flavored commands, in a GUI window and a dot-prompt console at the same time. JAWS, NVDA, and Narrator are all first-class through dedicated speech paths; every command is reachable by keyboard.

Source and releases: <https://github.com/JamalMazrui/DbDo>.

## How DbDo is organized

DbDo runs in two interfaces — a WinForms GUI and a dot-prompt CLI — simultaneously by default. Both interfaces drive the same ADO database connection, so a change in one shows up immediately in the other.

The GUI is a single Windows form containing a menu bar, a data list in the center, and a status bar at the bottom. Standard Windows conventions apply: Alt activates the menu bar; single-letter mnemonics open each top-level menu (File, Edit, Navigate, Query, Misc, Help). Every menu command is reachable through both a hotkey and a click; no command is mouse-only.

The data list is a virtual-mode ListView in Details view with FullRowSelect on. Arrow keys move between rows. There is no per-cell focus visually; for cell-level reading, DbDo provides two complementary mechanisms: Tab and Shift+Tab move an **announcement-only** column cursor (the screen reader speaks the new column name and value without changing the visible focus), and **Alt+Control+arrow** drives a virtual table cursor that also moves the visible row selection (see "Virtual cell navigation" below).

The status bar at the bottom carries three items in this order: the word "marked" (only when the current row has its marked flag set), the row position "row N of M", and "edited YYYY-MM-DD" (only when the current row has an edited column). Two spaces separate the sections so the screen reader pauses naturally between them. Use the JAWS Insert+PageDown command to read the whole status bar at once.

The window title is `DbDo - <database> - <table>`, with `(read-only)` inserted after the database name when the lock is on.

The Shift+F10 context menu in the data list duplicates the most common record-level commands.

The CLI is a Windows console window running a dot prompt. Each line is a single Verb-Noun command, optionally with an argument; bare SQL is also accepted directly. The prompt is the current table's name followed by a dot. Use the Get-Help command, F1 from the GUI or `help` at the prompt, to see the command index; `help <command>` shows details for one command. Use the Out-File command, `Out-File path.txt` (output, tee, or `o` are aliases), to capture the next commands' output to a file while keeping it on screen at the same time.

### Switching between modes

Three grave-accent chords coordinate the GUI/CLI relationship. JAWS calls the unshifted key above Tab "GraveAccent."

Use the Enter-Console command, Control+GraveAccent, from the GUI menu to open or focus the dot prompt console. Use Alt+Control+GraveAccent (a global hotkey) from anywhere in Windows to toggle between GUI and console, whichever is not currently in front — since DbDo is a single-instance application, this one chord covers both directions.

In CLI-only mode these chords work the same way; without a GUI, the "switch to GUI" chords simply have nothing to bring forward.

### Starting DbDo

The installer creates a single shortcut, DbDo, with hotkey Alt+Control+D (D for Desktop). Use the hotkey from anywhere in Windows to activate a running instance or launch a fresh one. DbDo is single-instance: a second press of Alt+Control+D wakes the existing window rather than spawning a duplicate.

## Terminology: row, column, record, field, cell

DbDo uses a small set of nouns deliberately. The words are not interchangeable, and they shift with the context in which the user is operating. The choices here match the dominant conventions of end-user database products (Microsoft Access, FileMaker Pro, dBASE/FoxPro, the ADODB API) rather than the strictly SQL-canonical vocabulary that PostgreSQL or Oracle documentation prefers. The goal is naturalness for the user reaching for the command, not theoretical purity.

**Record.** The primary noun for "a complete entity stored as a row in a table." Used in command names for actions that act on a complete row of data: New Record, Delete Record, Copy Record, Append Record, Mail Record, Mark Record, Unmark Record. Find (Ctrl+F) finds *a record*; Jump (Ctrl+J) jumps to *a record*. The Edit View dialog edits one record; the inputs inside are fields (see below). The same noun the user sees in Microsoft Access, FileMaker, dBASE — and the noun ADODB uses internally for `Recordset.AddNew`, `Recordset.Delete`, and similar entity-level operations.

**Cell.** The intersection of a record and a column — a single named value within one record, viewed in the listview's grid context. Used in command names for actions that act on one value at the row-and-column crosshair: Edit Cell (F2), Copy Cell (Ctrl+C), Append Cell (Alt+C), Open Cell (Ctrl+Enter), Say Cell (Shift+C). The virtual cursor moved by Alt+Control+arrow targets a cell.

**Column.** A vertical slice of the listview — one attribute considered across all records. Used in command names for actions that sweep vertically through one attribute: Order Records (Alt+O — sort by chosen columns), Reverse Order (Alt+Shift+O), Replace Column (Control+R), Statistics Column (Control+Shift+S), Graphics Column (Control+Shift+G — plots a column), and Select Columns (Alt+S — which columns are visible). The screen reader's "row N column M" announcement uses "column" because that's the geometric description it speaks.

**Field.** A named attribute on a single record, viewed as a labeled input in a vertical-stack dialog. The Edit View and New Record dialogs lay out one field per line; that layout is intrinsically not a "column" layout, so the word "field" reads naturally there. Also used for individual attributes referred to by name: the `url` field, the `notes` field, the `tags` field. Say Notes and Say Tags target named fields. The choice of "field" inside dialogs follows Microsoft Access and FileMaker, both of which call vertical-stack labeled inputs "fields" even though the underlying ADODB API uses the same word for column-typed metadata.

**Row.** The geometric unit of the listview — used mostly for screen-reader and navigation announcements. "Row N column M" is the live region's spatial readout. Set Position jumps to a row by absolute position. "Table has no rows" announces emptiness. The status bar reports "20 of 25 rows shown" when a filter is active. Avoid "row" in command names that target the data semantically; use "record" instead. The exception is geometric scope descriptions inside parentheticals where the natural English reading wins ("every record in filtered view" — record because the action treats the row as an entity; the parenthetical describes scope geometrically but uses the same vocabulary).

**Table.** A schema-level object holding a set of records of the same shape. The Choose Table command, Switch Table, Show Schema all use "table." Views (read-only result sets defined by a SELECT) are labeled distinctly as "views" in dialogs that distinguish; the Choose View command exists separately.

The principle behind these choices: **the noun matches the layout the user sees when invoking the command.** In the listview's grid, the user sees rows, columns, and cells, so commands that act on those grid units use those words. When the user opens an Edit View dialog, the layout rotates 90 degrees — fields stack vertically — and "column" would force a mental translation back to the listview view, so we say "field" instead. When the user issues an action that treats the whole record as one thing, the entity-level word "Record" reads naturally regardless of the layout being viewed.

If you come from a SQL or PostgreSQL background and prefer "row/column" everywhere, every Record command has a dot-prompt alias using SQL vocabulary: `find`, `new`, `edit`, `delete`, `copy`, `mark`, `unmark` all work. Type `verbs` at the dot prompt for the full list.

### A note on "url" — lowercase as ordinary English

DbDo writes **url** in lowercase as a regular English noun in prose (sentences, dialog labels, tooltips, status-bar messages, live-region announcements that aren't sentence-initial), and **Url** in title case where title-casing applies (command names like Open Url and Say Url, menu labels like "Sort by Url"). The lowercase convention follows the same path natural-English took with *laser*, *radar*, *scuba*, *sonar*, and *zip code* — words that started as acronyms (LASER for "Light Amplification by Stimulated Emission of Radiation"; RADAR for "Radio Detection And Ranging") and have settled into lowercase ordinary nouns now that the original expansion is no longer foreground knowledge for most users. Most people who type a url into their browser have never thought about what the letters stand for; calling the thing a "url" rather than a "URL" matches that lived experience.

The convention also matters for screen readers, which read "URL" as "U-R-L" (three letters spelled out) but read "url" as one syllable, "earl." For commands users issue frequently — Open Url, Say Url, the eight Sort-by-X variants of which Sort by Url is one — the one-syllable rendering is faster and less interrupting in the audio stream.

The house style applies to developer documentation as well: code comments and history entries write "url" in prose, "Url" in command names, and `sUrl` / `lUrls` in variable names per Camel Type's casing rules (see below).

### "Database" used broadly

In DbDo, "database" covers more than SQLite .db files. The Open Database command also accepts .xlsx (Excel workbook), .csv (comma-separated values), and other tabular file formats; each sheet or file is presented as a table the user can browse with the same navigation commands. SQLite .db is the default for full functionality (triggers, generated columns, foreign-key drill, the standard-column convention) and is what new databases created through DbDo use. Other formats are best thought of as data sources DbDo can read and write through the Import Data and Export Data commands, with as much of the listview experience preserved as the format supports. Standard columns (`look`, `unq`, `added`, `edited`, `url`, `tags`, `notes`, `marked`) cannot be assumed to exist on tables from .xlsx or .csv sources; commands that use them check first via `hasField` and announce a clear refusal when the column is absent rather than producing an error. Import Data and Export Data also handle JSON — a top-level array of objects (or a single object), with native numbers, booleans, and nulls — alongside Markdown tables and inix; JSON support uses the Json.NET (Newtonsoft.Json) library, which `buildDbDo.cmd` downloads automatically from nuget.org the first time you build.

### Key-name convention

Key combinations follow the Freedom Scientific / JAWS convention: **Control**, **Alt**, **Shift**, **Enter**, **Escape**, **Tab**, **Space**, **Backspace**, **Delete**, **Insert**, **Home**, **End**, **PageUp**, **PageDown**, **UpArrow**, **DownArrow**, **LeftArrow**, **RightArrow**, **F1** through **F12**, **Apostrophe** (the `'` key), **Asterisk** (Shift+8), and so on. Combinations are written with `+` and no spaces: `Control+F`, `Alt+Shift+S`, `Control+Shift+N`. The convention matches what JAWS announces aloud when a key is pressed, so the documentation reads the same way the user hears it.

### Camel Type for code

DbDo's source code follows the **Camel Type** coding style — Hungarian-prefixed lower camel case, alphabetized variable blocks, lowercase keywords where the language permits, no subprocedures (every routine is a function). The full specification is in the included `CamelType_CSharp.md` / `CamelType_CSharp.htm` file. Examples: `sName` for a string, `iCount` for an integer, `bFound` for a boolean, `aRows` for an array, `lFields` for a list, `dCache` for a dictionary. Constants use a `c_` prefix: `c_sFormat`. The style is optimized for screen-reader productivity — prefixes let the listener identify a variable's type from the first syllable rather than having to track type declarations elsewhere.

## The db cursor

The db cursor is your position in the current recordset: a current row and a current column.

The row is concrete: it is the data list's single focused row. Whenever the recordset has at least one row, exactly one row carries both keyboard focus and list selection. That selection is only the cursor; choosing multiple records is the job of DbDo's mark infrastructure (the standard boolean `marked` field, Set Mark Control+M, Toggle Marked Control+Space, the bulk-mark span chords, and the marked-navigation keys).

The column is virtual: a standard list view has no cell focus, so the current column is tracked by DbDo and voiced rather than drawn. Column-aware commands (Say Cell, type-ahead search, sorting defaults, Show Related, Edit Cell) act on it, and the Alt+Control navigation family moves it cell by cell.

Movement keys, in the data list:

Home and End move the cursor to the first and last column of the current row. Control+Home and Control+End move it to the first column of the first row and the last column of the last row. PageUp and PageDown page the focused row a screenful at a time. The guiding principle is column preservation: the cursor column persists across every row movement (arrows, paging, jumps, refreshes, even table switches, which restore that table's remembered column by name) and changes only when a command explicitly says otherwise.

The marked-record jumps formerly on Control+Home and Control+End now live on Control+Shift+Home and Control+Shift+End (first and last marked record); Control+UpArrow and Control+DownArrow remain previous and next marked record.

## Keyboard navigation in the data list

Three navigation modes operate independently in the data list: row navigation with arrow keys, column-announcement navigation with Tab, and cell-level virtual navigation with Alt+Control+arrow.

**Row navigation.** Use the arrow keys to step row by row. Use the PageUp and PageDown keys to jump by a screenful at a time. Use Home and End to jump to the first and last row. The listview's row selection moves on each press; the screen reader announces the focused row.

**Type-ahead jump.** Use a lowercase letter to jump to the next row whose value in the column under Tab announcement begins with that letter. Type two or more letters in quick succession to extend the search prefix. The search wraps around at the end of the list and starts over from the top. Capital letters are reserved for bare-Shift+Letter command shortcuts (F, G, J, R, S — see "Mnemonic hotkey groups" below), so lowercase navigation is the convention for type-ahead.

**Tab to hear cell values.** Use Tab and Shift+Tab to move an announcement-only column cursor across the current row. DbDo announces "Column: value" after each Tab. The visible row selection does not change; this is purely a screen-reader convenience for hearing what's in each cell of the current row without changing where you are. Tab does NOT target commands; commands always prompt for a column (see Virtual cell navigation below for the command-targeting cursor).

## Virtual cell navigation

DbDo overlays a screen-reader-style table cursor on the listview. The listview itself has no per-cell focus, so the virtual cursor lives in DbDo's state and announcements; it tracks a `(row, column)` pair separate from the listview's row selection. Use it for table-style reading and for targeting column-aware commands.

**Alt+Control + extended-key chords drive the virtual cursor:**

- **Alt+Control+Home** = first cell of the first row (top-left)
- **Alt+Control+End** = last cell of the last row (bottom-right)
- **Alt+Control+RightArrow** = next column, same row
- **Alt+Control+LeftArrow** = previous column, same row
- **Alt+Control+DownArrow** = next row, same column
- **Alt+Control+UpArrow** = previous row, same column
- **Alt+Control+PageDown** = last row of the current column
- **Alt+Control+PageUp** = first row of the current column
- **Alt+Control+Numpad5** = say the current cell value (twice in succession spells it character by character)

Each movement triggers a speech announcement with **direction-aware framing**: a vertical move (row changed only) says "Row N: value"; a horizontal move (column changed only) says "Header: value"; a corner jump (both changed, from Home or End) says "Row N, Header: value". This matches the JAWS / NVDA table-reading convention.

The **double-press dialog** convention applies to every speech-only command. Press the command once to hear the text through your screen reader without moving keyboard focus. Press the same chord again within two seconds to open an information dialog with a read-only multi-line textbox containing the text plus an OK button — useful for reviewing long status, paths, or values that don't fit comfortably in a single speech announcement. The dialog's textbox supports normal text-navigation and Control+C copy. The two-second window is deliberately more forgiving than the JAWS and NVDA defaults (around 500 milliseconds) so a thinking pause between presses still counts as one gesture. Commands that participate include: Say Status (Shift+Z), Say Database (Shift+D), Say Yield (Shift+Y), Say Tables (Shift+F7), Say Marked (Alt+M), Say Edited (Shift+E), Say Notes (Shift+F9), Say Tags (Shift+T), and Say Column Rest (Alt+L).

The virtual cursor synchronizes with the listview row selection in both directions: pressing plain Down/Up arrow updates the listview's row selection, and the virtual row follows along with the column unchanged. Pressing Alt+Control+arrow moves the virtual cursor first, then moves the listview's row selection to match — so you can see the row you're virtually browsing.

The virtual cursor's column and row are **remembered per table** within a session and across sessions. When you switch to a different table (and later return), DbDo restores the row and column you were on. When you open a database file, every previously-visited table's filter, sort, position, and virtual column are restored — switching to any one of them via Choose Table, Control+Tab, or the dot prompt picks up the saved state, not row 1. The F5 Refresh command re-reads the current rows' values from the database via the ADO Resync method and preserves your cursor position (it does not pick up rows added or removed outside the recordset; reopen the table for that).

**Column-aware commands default to the virtual column.** When a command needs a column — Replace Column, Statistics Column, Graphics Column, Open Cell Value, Jump to Match in One Column — its picker dialog defaults to the column currently under virtual focus. Just press Enter in the picker to accept that column, or arrow up/down to pick a different one.

## Screen-reader settings

### JAWS settings for DbDo

JAWS has its own table-navigation chord set on Alt+Control+arrow, and by default JAWS intercepts those chords before the focused application sees them. Without an adjustment, pressing Alt+Control+RightArrow inside DbDo gives "Not in a table" instead of moving the virtual cursor. The same applies to several other DbDo chords on Alt-letter and Shift-letter combinations.

The fix is a three-file JAWS settings bundle:

- `DbDo.jkm` — JAWS key map. Maps the chords DbDo wants to take over to a Script named `PassDbDoKey`.
- `DbDo.jss` — JAWS script source. Defines `PassDbDoKey` as a one-line Script that calls the JAWS built-in `TypeCurrentScriptKey()`, which passes the current keystroke through to the application as if no script were running.
- `DbDo.jsb` — compiled binary of the above, produced by JAWS's `scompile.exe`. JAWS loads this at run-time and resolves the `PassDbDoKey` reference in the JKM.

**Automatic install.** The DbDo installer offers to install this bundle automatically. The Finish-page checkbox "Install JAWS settings for DbDo (recommended if you use JAWS)" is checked by default. Selecting it does three things for every JAWS year-version present on your system, in every language subfolder inside each version's `Settings` folder:

1. Copy `DbDo.jkm` to the settings folder.
2. Copy `DbDo.jss` to the settings folder.
3. Run `scompile.exe DbDo.jss` from inside the settings folder to produce `DbDo.jsb`.

Compiling locally against each installed JAWS's own `scompile.exe` guarantees the binary is compatible with that JAWS version (JSB compilation is version-sensitive). If a JAWS installation can't be located via the registry, the installer falls back to `C:\Program Files\Freedom Scientific\JAWS\<year>\scompile.exe`.

**Manual install** (or to recompile after editing the JSS):

```
copy DbDo.jkm "%APPDATA%\Freedom Scientific\JAWS\<year>\Settings\enu\"
copy DbDo.jss "%APPDATA%\Freedom Scientific\JAWS\<year>\Settings\enu\"
cd "%APPDATA%\Freedom Scientific\JAWS\<year>\Settings\enu"
"%PROGRAMFILES%\Freedom Scientific\JAWS\<year>\scompile.exe" DbDo.jss
```

where `<year>` is your JAWS year-version. The settings work from JAWS's next launch (or, if JAWS is already running, when DbDo next gains focus).

The pass-through list is generated to mirror DbDo's compiled key bindings exactly, so DbDo — not the screen reader — produces the speech for every one of its hotkeys. It covers the virtual-cursor family (Alt+Control + arrows, Home, End, PageUp, PageDown, NumPad5); the parent-child drill family (Alt+RightArrow, Alt+LeftArrow, Alt+Home, Alt+End); marked-row navigation and the column-preserving extremes (Control + Home, End, UpArrow, DownArrow, PageDown, PageUp); bulk-mark spans (Shift+Home, Shift+End, and the Alt+Shift unmark variants); the search families (Control+F, Control+J, Control+F3 with their Shift variants, plus F3 and Shift+F3); and every menu and direct command chord — the Alt-, Control-, and Shift-letter shortcuts, the function keys, and special keys such as Alt+Delete, Shift+8, Alt+Apostrophe, and Alt+Backslash. Because the list is regenerated from DbDo's own bindings, it stays in step as commands are added, removed, or rebound.

If you customize the JKM in place and re-install DbDo, your changes will be overwritten. To preserve customizations across updates, copy your modified version to a different filename and load both via JAWS's chain mechanism, or keep a copy outside the Settings folder and merge by hand after each DbDo upgrade.

If you uninstall DbDo, the installer removes only the three files it placed. Other JKMs or JSBs you placed yourself in those folders are not touched.

### NVDA add-on

DbDo ships a `.nvda-addon` package that performs the same pass-through role for NVDA that the JKM and JSB do for JAWS. The add-on registers an app module that, when DbDo.exe is the foreground process, hands every chord DbDo cares about back to DbDo instead of running NVDA's own table-navigation or browse-mode commands.

NVDA must be running for the add-on to install. The installer's Finish-page checkbox "Install NVDA add-on" hands the `DbDo.nvda-addon` file to its Windows file association, which is registered to NVDA at NVDA install time. If NVDA is the active screen reader, NVDA's standard "Install this add-on?" dialog appears, and the user confirms or cancels. If JAWS or Narrator is the active screen reader (with NVDA installed but not running), the file association still launches NVDA, but the install dialog may not surface reliably; in that case, dismiss the installer's Finish page, switch to NVDA, then double-click `DbDo.nvda-addon` in the install folder to install manually. The Help menu's "Re-install NVDA Add-on" command (which invokes `DbDo.exe --install-nvda-addon`) does the same thing on demand.

After install, restart NVDA (NVDA menu > Restart) so the new app module is picked up. NVDA does not need to be restarted again for future updates of the same add-on.

If the add-on does not appear to take effect — Alt+Control+arrow still triggers NVDA's "Not in a table" speech — set NVDA's log level to "Debug" (NVDA menu > Preferences > Settings > General > Log level), restart NVDA, then open DbDo and press the chord. Open NVDA's log (NVDA menu > Tools > View log) and search for lines beginning `DbDo app module:`. Absence of those lines means NVDA never matched the app module to DbDo.exe; presence of them confirms the module loaded and the bindings were registered.

**Narrator does not support scripts or add-ons.** Narrator users get less polished cell-level navigation than JAWS or NVDA users; the virtual-cursor announcements still fire, but Narrator may layer its own announcement on top.

## File menu

Use the New-Database command, Control+Shift+N (N for New, Shift to distinguish it from Control+N which makes a new row), to create an empty SQLite database at a chosen path. Use the Open-Database command, Control+O (O for Open), to bring up a file dialog and choose an existing database; DbDo recognizes `.db`, `.sqlite`, `.sqlite3`, `.mdb`, `.accdb`, `.xlsx`, `.xls`, `.dbf`, `.csv`, `.tsv`, and `.txt` files.

Every file dialog remembers the folder you last used and opens there next time. New-Database, Open-Database, Save-DatabaseAs, and Backup-Database share one remembered "open" folder, since you typically keep your databases together. Import-Data and Export-Data remember their own folders separately. If no remembered value exists, the dialog falls back to the folder of the currently-open database, then to your Documents folder.

Use the Recent Files command, Alt+R (R for Recent), to open one of the last ten database files DbDo has seen. The dialog shows each path with the table that was active when the file was last closed; selecting an entry reopens the file, restores that table, and restores the per-table filter, sort, and row position. If any of those pieces no longer apply (the table was dropped, a filter column was removed, the row count shrank below the saved position), DbDo silently skips the incongruity and reopens with the best-fitting state it can.

Use the Save-DatabaseAs command, Control+S (S for Save), to write a copy of the open database to a new path and switch DbDo to the new file. The dialog suggests `<original>-copy` as the filename so a stray Enter doesn't overwrite the source. Use the Backup-Database command, Control+Shift+S, to write the same copy but keep the original open; the suggested filename is `<original>-backup-yyyyMMdd`. The Close-Database command, Control+F4 (the MDI close convention), closes the open file without exiting DbDo.

Use the Import-Data command, Control+Shift+I (I for Import), to read a GitHub-flavored Markdown table file and append its rows into the currently-open table. Header cells are matched case-insensitively to columns in the destination; cells with no matching column are dropped silently. Embedded `<br>` decodes back to newline, `\|` back to a literal pipe. Multi-table files (separated by blank lines) all import; per-row errors do not stop the import.

Use the Export-Data command, Control+Shift+X (X for eXport), to write the current filtered view to one or more files. Every input format DbDo can open is also an export format: xlsx, docx, filtered HTML, Markdown table, csv, tsv, SQLite, Access, dBASE. The GUI prompts for one destination at a time; the dot prompt accepts a multi-format argument like `Export-Data xlsx docx md csv` (or the short forms `x d m c`). After each export, DbDo opens the result in its default Windows application so you immediately hear what was produced.

The xlsx and docx formats use Word and Excel through late-bound COM and therefore need Microsoft Office; csv, tsv, md, plain HTML, SQLite, Access, and dBASE all work without Office. SQLite, Access, and dBASE exports open a separate ADODB connection to a fresh file, issue `CREATE TABLE` with portable text-typed columns, and INSERT row by row — the user's open recordset is not disturbed.

The File menu also hosts the table-switching commands: **Choose Table** (F7) opens a listbox of base tables, **Choose View** opens the equivalent for views, **Next Visited Table** / **Previous Visited Table** (Control+Tab / Control+Shift+Tab) cycle among recently-opened tables in MRU order, and **Next Object** / **Previous Object** (Control+F6 / Control+Shift+F6) cycle through every table and view without the MRU filter.

Use the Print command, Control+P (P for Print), to print the current view; this is reserved for a future release. For now, export to HTML or docx and print from the corresponding application.

Use the Exit DbDo command, Alt+F4 (the Windows-standard close-program key), to close DbDo entirely. The dot prompt's `quit` and `q` commands map to Exit-Application as well; `exit`, `x`, and `bye` map to Exit-Console, which leaves the dot prompt but keeps the GUI running.

## Edit menu

Every Edit-menu command operates on the current row, except where noted.

Use the New Record command, Control+N (N for New), to add a row. DbDo shows an edit dialog with one line per distinct field; bookkeeping fields (`added`, `edited`, `marked`, the primary key, `look`, `unq`) get their default values automatically. Use the Edit View command, Control+E, to edit the current record field-by-field (see Views below). Use the Delete Record command, the Delete key, to remove the current row; Control+Shift+D removes it without the confirmation prompt.

Use the Duplicate Record command, Control+Shift+C (Shift turns the native clipboard's Control+C into row-copy), to clone the current row as a new record.

Use the Find and Replace Across Rows command, Control+R (R for Replace), to do a find-and-replace: pick a column, type the find string and the replace string, choose the scope (current row, filtered rows, or all rows), and DbDo updates every match through ADO so the same triggers fire as for a SQL UPDATE.

Use the Mark Record command, Control+M (M for Mark), to set the boolean `marked` column on the current row; when marked is true, the status bar reads "marked." Use the Unmark Record command, Control+Shift+M, to clear it; Say Mark Status (Shift+M) reads whether the current row is marked. Marks are useful for accumulating an ad-hoc selection across navigation; combine with `filter marked` at the dot prompt to scope subsequent commands.

Use the Save Bookmark command, Control+B (B for Bookmark), to remember the current row by primary key. Use the List Bookmarks command, Alt+B, to return to a saved one; use the Clear Bookmark command, Control+Shift+B, to forget it.

Use the Open Cell Value command, Control+Enter (extends Enter into "open as url"), to treat a cell's value as a url or a file path and open it in its default Windows application. DbDo prompts for the column (defaulting to the column under virtual focus, so just press Enter to accept); useful when a database column holds links to PDFs, screenshots, or web pages.

## Navigate menu

The Navigate menu contains three families: record stepping, search, and parent-child drill.

**Record stepping.** Use the First Record / Last Record / Next Record / Previous Record commands to step through the current view. None have hotkeys by default because the listview's arrow keys and Control+Home/End handle the same movements natively. Use the Set Position command, Control+G, to jump to a row number (absolute, percentage, or relative); Say Goto (Shift+G) speaks the current position. Out-of-range values clamp to the first or last row.

**Search.** Three independent search families plus a unified "repeat last search" pair. Each family has its own forward and reverse chord, and each family's last-used term is remembered separately so reopening a family's dialog brings up its own prior text.

Use the Find Across All Columns command, Control+F (F for Find), to find a row whose value in ANY visible column contains a substring. The dialog has three controls: a Text input (defaulting to the last Find substring), a Recent listbox of up to the last 10 Find terms, and a Case-sensitive checkbox (off by default). Selecting a Recent entry copies its text into the Text input AND sets the Case-sensitive checkbox to how that term was last used (entries that were case-sensitive show an `[Aa]` suffix in the display). Use Control+Shift+F to search backward.

Use the Jump to Match in One Column command, Control+J (J for Jump), to find a row whose value in ONE column you pick contains a substring. The dialog adds a Column listbox to the standard Text + Recent + Case-sensitive layout; the column defaults to the column under virtual focus, falling back to the last Jump column or to the first column. Use Control+Shift+J for the reverse.

Use the Find Regex Across All Columns command, Control+F3 (F3 = Windows-standard search; Control turns it into the regex variant), to find a row matching a .NET regular expression across any visible column. Same Text + Recent + Case-sensitive dialog as Find; the regex is validated before the search begins. Use Control+Shift+F3 for the reverse.

Use the Search Next command, F3, to repeat whichever family was last invoked moving forward. Use Search Previous, Shift+F3, to repeat backward. DbDo tracks the last-used family separately from the per-family last-term state, so a Jump followed by a Find followed by F3 repeats the Find, not the Jump.

**Parent-child drill.** Two-way movement between related tables using foreign-key relationships.

Use the Enter Child Table command, Alt+RightArrow (right-arrow = into the child), to drill from the current parent row into a child table whose schema includes the parent's primary-key column. If exactly one child table matches, DbDo opens it directly; otherwise it presents an alphabetized listbox so you can pick. The child table opens with a filter applied that shows only the rows whose foreign key matches this parent's primary key.

Use the Exit Child Table command, Alt+LeftArrow (left-arrow = out of the child), to pop back one level. DbDo restores the parent's sort, filter, and exact row position. The drill stack is unbounded; you can Enter Child Table several levels deep, then Exit Child Table back up the same way.

Use the Exit to Root Table command, Alt+Home, to pop the entire drill stack and return to the topmost ancestor in one keypress.

## Query menu

The Query menu contains read-only commands that report on the data without modifying it. Most fall into three families: examine, speech-only, and shape (filter / sort).

**Show commands.** Use the Details View command, Control+D (or Enter), to open a read-only display of every visible field of the current record plus its related parent and child rows (via foreign-key relationships). Use the Table Properties command, Alt+Enter (the Windows-standard properties chord), to see metadata about the current table — row count, column count, primary key, inferred foreign keys, cached settings. Use the Say Related command, Shift+R, to speak the records related to the current row through foreign keys. Use the Show Schema command to print every CREATE TABLE and CREATE VIEW in the database; this is long output, usually called from the dot prompt as `schema`.

**Speech-only commands.** The Say-X family announces state without changing focus or position. All eight commands respect the **double-press-spells** convention: press the same speech chord twice within 1.5 seconds to hear the text spelled character by character.

- Say Status (Shift+Z) — table name, row position, filter, sort
- Say Database (Shift+D) — the open database's name and path
- Say Yield (Shift+Y) — row count and active filter
- Say Tables (Shift+F7) — tables visited in this session
- Say Marked (Query menu; unbound by default) — the `look` values of every marked row
- Say Edited (Shift+E) — the `edited` value of the current row, in a human-friendly local-time form (`December 14, 1963 at 5:42 AM`); the underlying SQLite text is unchanged
- Say Notes (Shift+N) — the `notes` field of the current row
- Say Tags (Shift+T) — the `tags` field of the current row
- Say Column Rest (Alt+L) — every value of the current column from the cursor row down (no length cap)
- Say Records Rest Marked (Alt+Shift+M) — every marked record in full, from the cursor down

**Shape commands.** Use the Filter Records command, Alt+Shift+F (F for Filter), to filter through a field form like the edit dialog — one box per field. A bare value is a case-insensitive substring match (the default, the same as a leading `%`, so both `bridge` and `%bridge` become `name LIKE '%bridge%'`); a leading `=` forces an exact match; and `<=`, `>=`, `<`, `>`, `<>` (or `!=`) compare. Filled fields combine with AND. When a filter is already active, a chooser offers Clear (the default), And, Or, New, Edit, and Cancel — And and Or wrap the existing filter and the new one as `(old) AND (new)` or `(old) OR (new)`, New replaces from a blank form, and Edit replaces from a form pre-filled with your last values. Say Filter (Shift+F) speaks the current filter; to clear a filter, choose Clear in the chooser. The Filter by Regex command (Query menu, unbound by default) is a server-side complement to Filter Records: it restricts the current column to rows matching a regular expression using SQLean's `REGEXP` operator — see the SQLean extensions section below. For sorting, use the Order Records command, Alt+O, to pick columns and a direction for each; Reverse Order (Alt+Shift+O) flips every direction; and Say Order (Shift+O) speaks the current sort.

Sorting is done through the Order Records dialog (Alt+O): pick one or more columns and a direction (ascending or descending) for each. The dialog's column list defaults to the column under virtual focus.

To sort by date, add the `edited` column in Order Records. Removing every column from the sort returns the recordset to its natural order.

## Misc menu

Use the Refresh View command, F5 (the browser-standard refresh key), to re-read the current rows' values from the database via the ADO Resync method; useful when another tool has edited rows DbDo has open. F5 preserves your cursor position (reopen the table to pick up rows added or removed externally).

Use the Read Only Toggle command, Alt+Z, to switch the recordset between editable and read-only; the window title shows the change. (Views and query results are read-only by nature and the toggle cannot unlock them.)

Use the Table Summary command, Alt+T, to print row counts and a columns overview for the current table. Use the Graphics Grid command, Alt+Shift+G, to render a frequency-by-column chart in Excel for analysis.

Use the Statistics Column command, Control+Shift+S, to compute descriptive statistics for the column under the virtual cursor. DbDo walks the column, detects whether the values look numeric, date-like, boolean-like, or text, and reports the statistics that fit. Numeric columns get count, unique, minimum, maximum, range, mean, median, sample standard deviation, Q1, Q3, IQR, mode (if unambiguous), and a skew indicator. Date columns get earliest, latest, median, and span. Boolean-like columns (`0/1`, `Y/N`, `true/false`) get true and false counts with percentages. Text columns get unique count, shortest/longest/mean length, and a top-10 frequency table. The report opens in the same multi-line read-only dialog used by speech commands on double-press; press Control+C inside the textbox to copy the whole report.

Use the Graphics Column command, Control+Shift+G, to produce an Excel chart matched to the data type of the column under the virtual cursor. DbDo runs the same data-type detection Statistics Column uses and chooses a chart shape from it. Numeric columns can plot as a histogram (Sturges-binned distribution) or as a box-and-whisker plot (Tukey's five-number summary as a single compact shape); a small dialog prompts you to pick one. Date columns can plot as a timeline (count by month or by day for short spans, line chart), as counts-per-calendar-year (column chart), or as counts-by-month-of-year (column chart for seasonal patterns). Boolean columns auto-pick a pie chart of true / false proportions. Text columns auto-pick a horizontal Pareto bar of the top 15 most frequent values. When there is only one sensible chart shape for the data type, DbDo skips the picker and generates it directly. The .xlsx file is written next to the database file with a name like `customers-region-pareto.xlsx`, then opened in Excel; from there you can polish the chart, copy it to other documents, or change the chart type by hand. Graphics Column requires Excel to be installed. The Graphics Grid command (Alt+Shift+G) remains for when you want to pick any column from a list and produce a column chart of value counts, regardless of data type.

Use the Select Columns command, Alt+S, to pick which columns appear in the data list for the current table. Hidden columns are still accessible through Details View and Edit View.

Use the Extract Regex command, Control+Shift+X, to walk every visible row, run a .NET regex against every visible column, and copy every match to the clipboard one per line. Useful for pulling email addresses, urls, or IDs out of free-text columns.

Use the Copy Cell command, Control+C, to copy the current virtual cell (the cell under the Alt+Control+arrow cursor) to the Windows clipboard, replacing whatever was there. Use the Append Cell command, Alt+C, to append the current virtual cell to the clipboard separated by a blank line (two CRLF), so you can accumulate values from multiple cells across rows or columns. If the clipboard is empty, Append acts the same as Copy.

Use the Copy Row as TSV to Clipboard command (no hotkey, accessed through the Misc menu) to copy the current row's visible columns as tab-separated values for pasting into Excel, Word tables, or chat clients. The label lacks a mnemonic letter because the only candidates fall mid-word — DbDo prefers no mnemonic to a mid-word one.

Use the Run SQL command, Control+Q (Q for Query), to run any SQL statement. SELECTs display the result as a new recordset; INSERT/UPDATE/DELETE/DDL run via ADO Connection.Execute. The dot prompt's `;` and `*` aliases map to the same command.

Use the Test Integrity command to run an integrity probe on the open database (`PRAGMA integrity_check` for SQLite, equivalents for other providers). Use the Test Drivers command to print which ODBC and OLE DB providers Windows currently has registered, useful when troubleshooting a failed Open Database.

Use the Open in Explorer command, Alt+Backslash (the backslash key evokes Windows paths), to open Explorer at the database file's folder with the file pre-selected.

Use the Open Dot Prompt command, Control+GraveAccent, to open or focus the dot prompt console from the GUI.

Use the Invoke Script command, Alt+V (V for inVoke), to pick and run one of your saved scripts. Use the Edit Script command, Alt+Shift+V, to edit an existing script or create a new one. Use the Open Script Folder command (no hotkey) to launch Explorer at the script folder. See the Scripting section below for the full reference.

Use the Configuration Settings command, Alt+Shift+C (matching the EdSharp and FileDir convention; F12 also works as a legacy alias), to open the per-user DbDo.inix settings dialog. The dialog exposes a curated subset of the settings the program reads — the UI mode, the Command Echo toggle, and a "Field Validation..." button that opens a sub-dialog for per-field regex patterns on the current table. The dialog also has an "Open file..." button that launches the raw DbDo.inix in your default text editor for advanced settings (`[Keys]` hotkey overrides, connection-string overrides, and operational housekeeping keys that DbDo writes itself).

Use the Start Mark Anchor command, F8, to record the current row as the start of a mark range. Then move to another row (above or below — direction does not matter) and press Complete Mark to Anchor, Shift+F8, to set the marked flag on every row in the range, inclusive. Use the parallel Start Unmark Anchor command, Alt+F8, and Complete Unmark to Anchor, Alt+Shift+F8, to do the same for clearing marks. The two anchors are independent, so you can stage a mark range and an unmark range without one gesture clobbering the other. Both anchors are direction-agnostic and transient: they reset when you close the database, and each "Complete" command refuses with a clear message if its anchor was set on a different table than the currently-open one. The pattern parallels EdSharp's Start/Complete Selection family (F8 / Shift+F8) and FileDir's Start Tag or Untag / Complete Tag / Complete Untag, with DbDo's "Mark" terminology and the additional independent unmark-anchor for symmetry.

Use the Say Position command, Alt+Delete, to hear the current cell's column header and row number — for example, "Column: name, Row: 30". Speech-only; does not move focus. This is the JAWS convention for "say cursor position", reframed in DbDo terms to mean the virtual cell (the cell under the Alt+Control+arrow cursor). Single-press speaks; double-press shows the same text in the multi-line dialog used by the other speech-only commands.

Use the Say Sort and Filter command, Shift+8 (the asterisk key; Numpad-asterisk also works), to hear the active sort order and filter criteria for the current table. Either or both may be empty; the speech explicitly says "(none)" rather than going silent, so you always get confirmation. Single-press speaks; double-press shows the same text in the multi-line dialog.

Use the Say Kin command, Shift+K (K for Kin — relatives by foreign key), to hear the `look` field of every related record. The announcement covers both directions: every parent reached by an outbound foreign-key column on the current row, and every child that points back via an inbound foreign key. Parent entries are listed first as "Parents: <table>: <look>; <table>: <look>"; children follow as "Children: <table> (N): <look>, <look>, ...". Single-press speaks via the screen reader; double-press shows the same content in the multi-line dialog, useful when the parent row has many children. The Say Kin command is read-only and does not move the cursor or change the open table; for an interactive jump to a specific related record, use the existing Show-Related command (Alt+Shift+R) instead.

Field validation: each editable field can have a regex pattern stored in DbDo.inix under a `[Validation:<table>]` section. When the pattern is set, both the Edit View dialog (Control+E) and the Edit Cell dialog (F2) refuse non-matching input. DbDo uses .NET regex syntax (the established powerful pattern language, the same one Find Regex uses) — examples appear in the sub-dialog. Empty pattern means no constraint.

## SQL reference: what Invoke-Sql actually runs

A common question worth answering precisely: when you press Control+Q for Run SQL, what SQL dialect does the database engine understand? The answer depends on which kind of file you have open, because DbDo uses three different drivers under the ADO ConnectionString and each one parses SQL differently.

### A portable SQL baseline that works everywhere

Most users do not need a SQL deep-dive. The basics — selecting rows, inserting rows, updating values, deleting rows — work across every database format DbDo opens (SQLite, Access, Excel-as-queryable, dBASE, CSV/TSV-as-readable) when you stay inside a careful ANSI SQL-92 subset.

```sql
-- SELECT rows
SELECT first_name, last_name FROM teachers WHERE year = 'Senior' ORDER BY last_name;

-- Aggregate
SELECT year, COUNT(*) AS n FROM students GROUP BY year ORDER BY year;

-- Join two tables
SELECT s.first_name, s.last_name, c.title
FROM students s
INNER JOIN enrollments e ON e.student_id = s.student_id
INNER JOIN classes c ON c.class_id = e.class_id
ORDER BY s.last_name, c.title;

-- INSERT, UPDATE, DELETE
INSERT INTO teachers (first_name, last_name) VALUES ('Grace', 'Hopper');
UPDATE teachers SET status = 'emeritus' WHERE teacher_id = 7;
DELETE FROM enrollments WHERE class_id = 42;

-- CREATE / DROP table
CREATE TABLE notes (note_id INTEGER PRIMARY KEY, body TEXT);
DROP TABLE notes;
```

The portable subset includes: `SELECT` with `WHERE`, `ORDER BY`, `GROUP BY`, `HAVING`, `DISTINCT`, `LIMIT` (SQLite) or `TOP` (Access — these are NOT interchangeable); `INNER JOIN` and `LEFT JOIN` on equality conditions; `COUNT`, `SUM`, `AVG`, `MIN`, `MAX`; comparison and boolean operators; `IS NULL`, `IS NOT NULL`, `BETWEEN`, `IN`; simple `LIKE` (but see the wildcard note below); the usual DML and the simple `CREATE TABLE` / `DROP TABLE`.

**Watch for these portability gotchas** when you cross between SQLite and the Access/Excel/dBASE side:

- **LIKE wildcards differ.** SQLite uses ANSI wildcards `%` and `_`. Jet/Access uses `*` and `?`.
- **String concatenation differs.** SQLite uses `||`; Jet uses `&`.
- **Date literals differ.** SQLite uses ISO strings: `'2026-01-15'`. Jet uses pound-sign delimiters: `#1/15/2026#`.
- **Boolean literals differ.** SQLite treats `1` and `0` as boolean; Jet uses `True` and `False`.
- **Row-limiting differs.** SQLite uses `LIMIT N`; Jet uses `SELECT TOP N`.
- **Identifier quoting differs.** SQLite accepts double-quotes or square brackets; Jet prefers square brackets.

**External references:**

- **Microsoft Jet 4.0 / Access SQL reference**: <https://learn.microsoft.com/en-us/office/client-developer/access/desktop-database-reference/microsoft-jet-sql-reference>
- **SQLite SQL syntax reference**: <https://sqlite.org/lang.html>
- **SQLite syntax diagrams**: <https://sqlite.org/syntaxdiagrams.html>

### SQLite: full modern SQL

When the open file is .db / .sqlite / .sqlite3, DbDo connects through the ch-werner SQLite ODBC driver. There is no SQL translation layer: the full SQLite SQL surface is available, including:

- Window functions with the OVER clause (since 3.25): `row_number()`, `rank()`, `dense_rank()`, etc.
- Common Table Expressions (WITH), including recursive CTEs.
- UPSERT (`INSERT ... ON CONFLICT ... DO UPDATE`).
- JSON1 functions: `json_extract`, `json_each`, `json_array`, `json_object`, etc.
- RETURNING clause on INSERT/UPDATE/DELETE (since 3.35).
- FTS5 full-text search if the database was built with FTS5 tables.
- R-Tree, generated columns, partial indexes, expression indexes.

PRAGMA statements work both as data-returning queries (when shaped like `PRAGMA table_info(foo)`, which returns a result set DbDo will render as a grid) and as setters (when shaped like `PRAGMA journal_mode = WAL`, which DbDo runs through Execute).

The few things SQLite genuinely lacks compared to PostgreSQL or SQL Server: no stored procedures (use views and triggers instead), no row-level security, no native UUID type, and only a limited ALTER TABLE.

Sample SQLite queries that work in Invoke-Sql against `sample.db`:

```sql
-- Recursive CTE walking a hierarchy
WITH RECURSIVE child_of(id, name, depth) AS (
  SELECT teacher_id, name, 0 FROM teachers WHERE teacher_id = 1
  UNION ALL
  SELECT t.teacher_id, t.name, c.depth + 1
  FROM teachers t JOIN child_of c ON t.teacher_id = c.id
)
SELECT * FROM child_of;

-- Window function: rank students within each class
SELECT class_id, student_id, grade,
       RANK() OVER (PARTITION BY class_id ORDER BY grade DESC) AS class_rank
FROM enrollments;

-- UPSERT
INSERT INTO students (student_id, name) VALUES (42, 'New')
ON CONFLICT (student_id) DO UPDATE SET name = excluded.name;

-- PRAGMA returning a result set
PRAGMA table_info(students);
```

### Access (.mdb / .accdb): Jet/ACE SQL

When the open file is .mdb or .accdb, DbDo connects through the Microsoft ACE OLEDB provider. This driver runs **Access SQL**, not standard SQL. The differences from SQLite or ANSI SQL are substantial.

Access SQL uses `IIF(condition, then, else)` instead of `CASE WHEN ... THEN ... ELSE ... END`. String concatenation is `&` not `||`. Wildcards in LIKE are `*` and `?`, not `%` and `_`. Date literals are delimited with hash marks: `#2025-05-12#`. Booleans are TRUE and FALSE.

Access SQL supports the basics but lacks CTEs (no WITH clause), window functions (no OVER clause), and RETURNING. It has a different set of built-in functions: `Format()`, `DateSerial()`, `DateDiff()`, `DatePart()`, `Nz()` (null-coalesce), and many more drawn from VBA.

```sql
-- IIF instead of CASE
SELECT student_id, name, IIF(grade >= 90, 'A', IIF(grade >= 80, 'B', 'C')) AS letter
FROM students;

-- LIKE with Access wildcards
SELECT * FROM students WHERE name LIKE 'Sm*';

-- Date literal with hash delimiters
SELECT * FROM enrollments WHERE edited >= #2024-01-01#;

-- Aggregate with TOP (Access's LIMIT)
SELECT TOP 10 class_id, COUNT(*) AS n FROM enrollments
GROUP BY class_id ORDER BY n DESC;
```

The hardest gotcha is the wildcard difference: queries copy-pasted from a SQLite tutorial often use `%` and `_` and silently match nothing in Access.

### Excel (.xlsx / .xls): a subset of Jet SQL

Excel files open through the same ACE OLEDB provider as Access. The engine treats each worksheet as a table, with the sheet name (followed by a `$`) as the table identifier. SELECT, basic WHERE, GROUP BY, ORDER BY, and joins between sheets in the same file all work. Most INSERTs and UPDATEs work. CREATE TABLE creates a new sheet; DROP TABLE removes a sheet. No CTEs, no window functions.

Worksheet names with spaces or special characters must be bracketed: `SELECT * FROM [Sheet 1$]`.

```sql
-- Read everything from a sheet
SELECT * FROM [Students$];

-- Range subset
SELECT * FROM [Students$A1:D50] WHERE grade > 80;

-- Join across two sheets in the same workbook
SELECT s.name, e.class_id
FROM [Students$] AS s INNER JOIN [Enrollments$] AS e ON s.student_id = e.student_id;
```

### dBASE (.dbf): minimal SQL

dBASE files open through ACE OLEDB. The connection is to the folder containing the .dbf, not the file itself; each .dbf in the folder becomes a table. The SQL surface is the smallest of the four backends: SELECT with WHERE, ORDER BY, GROUP BY, basic INSERT, UPDATE, DELETE, simple CREATE TABLE, DROP TABLE. dBASE field naming rules apply: identifiers max 10 characters, no spaces or hyphens.

### CSV and TSV: SELECT-only Jet text driver

When the open file is .csv or .tsv, DbDo connects through the Jet text driver. The driver is read-mostly: SELECT works well, INSERT works in a limited fashion, and structural commands like CREATE TABLE or ALTER TABLE don't apply.

The most useful pattern is to read a CSV with Invoke-Sql for analysis, and use Import-Data to copy into SQLite for any non-trivial work.

### Practical recommendation

For non-trivial analytical SQL — anything involving CTEs, window functions, recursive queries, JSON extraction, or modern SQL features — open the data in SQLite (either by saving a SQLite file directly or by using Import-Data from another format) and use Invoke-Sql against that. SQLite gives you the most expressive SQL of the four backends, the fastest engine, and the smallest set of dialect surprises.

## Scripting

DbDo's scripting feature lets you save and re-run units of work — computations, queries, command workflows — without having to remember or retype them. The model is one folder, one chord (Alt+V), and three file extensions that pick the right execution engine for the kind of work you want to do. The pattern is the same one EdSharp and FileDir pioneered for their snippet systems: a folder of plain text files; you write them in your own editor; you invoke them from a standard listbox dialog. DbDo extends the pattern by recognizing three distinct file types and dispatching to the engine that matches each.

### Where scripts live

Scripts live in two places. Generic scripts — ones that work against any database — live under `%APPDATA%\DbDo\Scripts\`, created on first access under your roaming application data so they survive DbDo upgrades and uninstalls. A database's own scripts live in the same folder as its `.db` file, beside the database, so they travel with it and stay out of the way when a different database is open. Invoke Script and Edit Script show both sets merged, with the database's own scripts taking precedence on a name clash. On first launch DbDo seeds the generic folder with the bundled generic sample scripts, and seeds each bundled database's folder (under `%APPDATA%\DbDo\Samples\<name>\`) with that database's scripts. A `.seeded` sentinel file in each folder records that seeding has run, so deleting a sample does not cause it to reappear later. To re-seed (to recover a deleted sample or pick up a new one bundled with a future release), delete the `.seeded` file and any matching scripts, then invoke any script command.

### The three file types

The extension on a script file determines which engine runs it. The picker shows the extension on every row, so you always know what kind of script you're picking before you press Enter.

**`.js`** — JScript .NET. A general-purpose computational language that runs inside the DbDo process with full access to the running form and recordset. The script has two pre-injected host variables, `db` (a `DbDoManager` — the open database) and `frm` (a `DbDoForm` — the GUI window). The DbDo.dll support module pre-imports `System`, `System.Collections`, `System.Data`, `System.IO`, `System.Reflection`, `System.Text`, `System.Text.RegularExpressions`, and `System.Windows.Forms`, so a script can use any type in those namespaces directly without its own `import` statements. The value of the last expression in the script is what DbDo gets back; if it's a string, that string is displayed in the result dialog. Use `.js` when you want to do computation: iterate over rows, transform values, build a report string, talk to the COM bridge, format output. This is the most powerful and the most code-heavy of the three.

**`.sql`** — SQL batch. A list of SQL statements separated by `;`. Each statement is run in order through the same `invokeSql` pipeline that powers the Invoke-Sql command. SELECT results are rendered as text tables; UPDATE / INSERT / DELETE return their row counts; PRAGMA output is shown verbatim. Statements that error abort the batch with an error message identifying the failing statement number. Line comments starting with `--` and block comments `/* ... */` are stripped before parsing. The first line is conventionally a `-- Description: <one-line summary>` comment so the picker has context. Use `.sql` when you want to ask the database something or change data: saved queries, repeatable reports, scheduled cleanups.

**`.dbdo`** — DbDo command batch. A list of dot-prompt commands, one per line, dispatched as if you had typed each line at the dot prompt. The execution surface is the entire DbDo command set, not just SQL — so a `.dbdo` script can open a database, switch tables, set filters and sorts, mark and unmark rows, speak status to the screen reader, export results, and so on. Use the **natural dot-prompt language** in `.dbdo` scripts — the short, single-word aliases like `path`, `status`, `tables-list`, `sort-filter`, `table <name>`, `filter <expr>`, `sort <expr>`, `find <text>`, `jump <expr>`, `next`, `previous`, `mark`, `unmark`, `add`, `edit`, `delete`, `schema`, `kin` — rather than the underlying canonical PowerShell-style verbs (`say-path`, `say-status`, `say-tables`, `select-table`, `select-record`, `sort-object`, etc.). Either form works because the dispatcher resolves aliases, but the natural form is shorter, more readable, and matches what you type interactively. Lines starting with `#` or `--` are comments. Blank lines are skipped. A leading `?` on a line means "continue on error" — useful for commands whose failure is harmless (e.g. clearing a filter when no filter is active). By default an error on any other line aborts the batch and the rest of the file is not run. Each line's output is captured and accumulated in the result dialog. Use `.dbdo` when you want to automate a workflow you would otherwise execute as a sequence of menu commands or dot-prompt entries.

The mental model is: `.js` is "compute"; `.sql` is "query"; `.dbdo` is "do." Each surface serves a different question.

The `.dbdo` extension is reserved for DbDo's command-batch format: it is simply the program's own name, so the association is unmistakable and the namespace is clean. Alternatives considered and rejected: `.dbd` (collides with ER/Studio Repository files and some InterBase backup formats), `.dot` (collides with Graphviz graphs and Microsoft Word templates), and `.duo` (an earlier choice, retired when the program's name settled as DbDo).

### The chord — Alt+V

Use the Invoke Script command, Alt+V, to pick and run a script. A standard `LbcDialog` listbox shows every `.js`, `.sql`, and `.dbdo` file in the open database's folder and the generic script folder, merged and sorted alphabetically, each row showing the full filename including extension. Type into the filter box to narrow the list; press Enter or click OK to run the highlighted script. The result (script output for `.js`, query results for `.sql`, accumulated command output for `.dbdo`, or an `ERROR: ...` message on failure) appears in a read-only multi-line memo dialog that screen readers can navigate line by line, word by word, or character by character.

The chord is named "Invoke Script" rather than "Invoke File" or "Invoke Query" because "script" generalizes naturally to all three engines — a JScript script, a SQL script, and a DbDo command script are all *scripts* in the recipe-for-doing-something sense, even when the engines differ.

### Editing scripts

Use the Edit Script command, Alt+Shift+V, to edit an existing script or create a new one. If the folder has existing scripts, the picker appears with a `[New script...]` entry at the top; picking that opens a Save File dialog whose Filter list offers JScript .NET, SQL batch, DbDo command batch, or any-extension. When you save a new script, DbDo seeds it with a starter template appropriate to the extension: a header comment block plus one example construct that runs successfully out of the box. After the file is created, your default editor opens on it.

The default editor is Notepad. To override, put a line in `DbDo.inix`:

```ini
[Scripts]
editor = C:\Program Files\Notepad++\notepad++.exe
```

### Opening the Scripts folder

Use the Open Script Folder command (no hotkey by default) to launch Explorer at the open database's folder — where its scripts live, beside the `.db` file — when a database is open, otherwise at the generic script folder. Useful when you want to copy a script in from elsewhere, rename a batch of files, or check on the `.seeded` sentinel.

### Bundled sample scripts

DbDo ships with two examples of each script type. They demonstrate the typical shape and idioms of each engine and serve as starting points for your own work.

**`.js` samples (computation):**

- **`CopyRowToClipboard.js`** — acts on the current row. Iterates the visible fields, builds a `name = value` listing, and puts it on the Windows clipboard. Demonstrates: row access via `db.getFieldValue`, the `StringBuilder` idiom, calling `frm.invokeMessage` for screen-reader confirmation.

- **`MarkRowsMatchingRegex.js`** — acts on the whole filtered view. Walks every row, tests every visible field against a regex, and marks each row whose values match. Demonstrates: iteration over the recordset, regex matching, conditional field updates via `db.setFieldValue` + `db.update`, the "soft search" idiom of marking matches you can later scope commands to.

**`.sql` samples (queries):**

- **`SchemaOverview.sql`** — three SELECTs against `sqlite_master` printing every table, view, and named index in the open database. Demonstrates: catalog introspection, the `UNION ALL` style for grouping related results, leading `--` comments for description.

- **`NorthwindRowCounts.sql`** — one SELECT per Northwind table, combined with `UNION ALL` and ordered by row count. Demonstrates: per-table count, multiple counts in one result, the leading description-comment convention.

**`.dbdo` samples (workflows):**

- **`RecentOrders.dbdo`** — switches to the orders table, filters to the most recent calendar year, sorts most-recent-first, and reports the result count via speech. Uses native dot-prompt aliases (`table`, `filter`, `sort`, `sort-filter`) instead of canonical PowerShell-style verbs. Demonstrates: multi-step workflow, the `?` continue-on-error prefix on the clear-filter line, speech-only verbs at the end as a screen-reader confirmation.

- **`StatusSnapshot.dbdo`** — runs five speech-only commands in a row (`path`, `tables-list`, `status`, `sort-filter`, `say-yield`) to print a complete "where am I" snapshot. Uses the natural dot-prompt aliases throughout — only `say-yield` keeps its canonical form because no shorter alias exists for the one-line row-count summary. Demonstrates: pure-read workflow, the speech-only command family, scriptable as a warm-up routine when sitting down to a workspace.

### The convention sample scripts

NFB2026Convention.db ships with its own set of seven demonstration scripts, in the `Scripts` folder of the distribution. They are separate from the generic samples above: where those teach the mechanics of each engine on whatever database is open, these answer real questions about the convention and double as templates for the kinds of work the maps model makes easy. To run one, place it in your script folder (Open Script Folder opens that folder in Explorer; copy the files in), after which it appears in the Invoke Script picker like any other.

All four `.sql` scripts share one idiom worth learning, because it is how every relational question against this schema is phrased. A record's identity is its `unq` -- for a person, `first|middle|last`; for an organization, its enterprise name; for an event, `date|time|title` -- and the maps table stores exactly those `unq` strings in `unq1` and `unq2`. So the join is always `JOIN contacts c ON c.unq = m.unq1` (or `events e ON e.unq = m.unq2`, and so on), and to ask a *different* relational question you change only the `kind` on the `WHERE` line. "Who presents at events," "who sponsors events," and "who offers which products" are the same query with `presents`, `sponsors`, or `offers` swapped in -- the payoff of one generic junction table instead of a separate join table per relationship.

**Query the relationships (`.sql`).**

- **Presenter-Events.sql** -- every session one presenter appears on, in time order, joining contacts to events through the `presents` kind. Adapt it by changing the surname on the `WHERE c.last_name = '...'` line. The canonical "follow one record to its related records" query.
- **Speaker-Directory.sql** -- groups the `presents` rows by presenter into an alphabetical directory: name, organization, role, session count, and the `url` link where one was confirmed. Shows how a maps join and a plain contact column combine into reference output, and how `GROUP BY` collapses a person's several sessions into one line. Commented variants reorder it by session count or restrict it to presenters with a confirmed link.
- **Convention-Stats.sql** -- three ranking queries (busiest speakers, rooms, and days) built with `GROUP BY ... COUNT(*)`, plus a footer noting the single-table questions you need no SQL for at all (`filter`, `yield`, `longest`, `reset-filter` at the dot prompt).
- **Sponsor-Showcase.sql** -- reads the `sponsors` (organization to event) and `offers` (organization to project) kinds, demonstrating that the join shape never changes when the relationship does.

**Automate a workflow (`.dbdo`).** Each line is a dot-prompt command run in order, so the file reads as the sequence of moves you would otherwise make by hand.

- **Daily-Schedule.dbdo** -- `filter` to one day, `order` by start time, `count`, `export` the schedule, then `reset-filter`/`reset-sort`. Change the date on the filter line; the convention runs 2026-07-03 through 2026-07-08.
- **Topic-Track.dbdo** -- the same shape applied to a topic instead of a day: `filter title LIKE '*Braille*'`, sort, count, and `export Braille-Track.docx`. Change the keyword to retopic it, or change the export extension (`.html`, `.md`, `.xlsx`, `.csv`) for the same view in another format.

**Compute and build output (`.js`).**

- **Marked-Schedule.js** -- walks the current view with `db.moveFirst()`/`db.moveNext()` and `db.getFieldValue`, grouping events under a day heading and writing one navigable HTML table per day to `MySchedule.html`. Because it reads only the rows in the current view, the filter and sort you set beforehand decide exactly what it captures -- run `filter marked = true` first to export only the sessions you marked. The template to copy when you want grouped or formatted output the plain Export command does not produce.

**Produce a report (`report.inix`).** The convention sample also ships a report definition file, `report.inix`, beside the database -- the declarative counterpart to Marked-Schedule.js. Where the script walks the rows in code, the report file describes the output in bands and lets the Produce Report command do the walking. It defines two reports. **`daily_agenda`** groups the sessions by day (`@group = event_date`) and prints each as a Markdown heading with the day's sessions beneath it and a session count per day -- the no-code equivalent of the grouped schedule the `.js` builds. **`session_list`** is a flat list you run after filtering the events table to one day, one room, or your marked sessions, so the filter is your "which sessions" parameter. Pick either from File > Produce Report, which renders it to Markdown and opens it in your editor; the full template language is covered under "Report templates" below. Its sibling is the *import* definition file, a transfer map (`transfer.inix`) described under "Importing with a transfer map" -- the two `.inix` definition files are how you bring data in and send formatted data out without writing code.

Together the seven map onto the same "compute / query / do" split as the generic samples: `.sql` to ask, `.dbdo` to drive a workflow, `.js` to build something. Copy whichever is closest to your need and change the noun.

### Errors

Compile-time errors and runtime errors in `.js` scripts are caught and returned as a string starting with `ERROR:`. The result dialog shows it with the error icon. The script never throws out to DbDo, so the UI stays responsive.

For `.sql` batches, an error on any statement aborts the batch; the result dialog shows the output of every statement that ran before the error plus the error message identifying which statement failed.

For `.dbdo` batches, by default an error on any line aborts the batch; the result dialog shows the output of every line that ran before the error plus the error message identifying the failing line. To let a specific line fail without aborting, prefix it with `?` ("continue on error"). This is useful for commands like `reset-filter` whose failure is harmless (no filter was active) but should not stop the rest of the script.



### Writing your own

A trivial script that just verifies things work:

```javascript
"Current table: " + db.currentTable + ", " + db.recordCount + " rows.";
```

Count rows then change the filter:

```javascript
var iBefore = db.recordCount;
db.filter = "City = 'Seattle'";
frm.invokeRefresh();
"Filtered from " + iBefore + " to " + db.recordCount + " rows.";
```

Walk every row and collect a chosen column's values (capped at 20 lines for the MessageBox):

```javascript
var aLines = [];
db.moveFirst();
while (!db.eof) {
  aLines.push(db.getFieldValue("name"));
  db.moveNext();
}
aLines.slice(0, 20).join("\n");
```

Trigger a form action (programmatic Save Bookmark):

```javascript
frm.recBookmarkClicked(null, null);
"Bookmark saved at row " + db.absolutePosition;
```

### Power and responsibility

Scripts run in the DbDo process with all the privileges DbDo has. There is no facade or sandbox. A script can call `Environment.Exit`, read or write files, launch other programs, modify the database. This is intentional for power-user automation; treat scripts the same way you would treat shell scripts or PowerShell scripts you run on your own machine.

## Text field conveniences

Every text box in DbDo's dialogs — single-line inputs, the multi-line memo editor, and even the read-only Error and Help viewers — shares a family of keyboard conveniences aimed at screen-reader review and editing. They belong to the text control itself, so they behave the same everywhere a text field appears, with no per-dialog setup.

- **Control+A** — Select all; **Control+Shift+A** — Unselect all. Both work even in read-only and multi-line boxes, where a plain select-all is often unreliable.
- **Control+C** — Copy. With a selection, copies it; with no selection, copies the current line (without its line break), so you can grab a line without selecting it first.
- **Alt+C** — Copy Append: add the selection (or current line) to what is already on the clipboard.
- **Control+X** — Cut. With a selection, cuts it; with none, cuts the current line including its break (removing the row) and speaks the line the cursor lands on.
- **Alt+X** — Cut Append: cut as above, but add to the clipboard instead of replacing it.
- **Control+D** — Delete Line: remove the current line without touching the clipboard.
- **F8 / Shift+F8** — Start Selection / Complete Selection: press F8 to drop an anchor, move the cursor, then Shift+F8 to select from the anchor to the cursor.
- **Control+F8** — Copy All; **Alt+F8** — Read All (speak the entire field).
- **Alt+Y** — Say Yield: speak the line and character counts.
- **Alt+Apostrophe** — Say Clipboard: speak the current clipboard text.
- **Shift+F1** — Focus Tip: speak the tip describing the field you are in.

Cut, Cut Append, and Delete Line do nothing in a read-only viewer — they announce "Read-only" — while the copy, read, count, clipboard, and tip commands work there too.

## SQLean extensions

DbDo can use SQLean, a bundle of SQLite extensions, to add SQL functions that plain SQLite lacks. If a file named `sqlean.dll` sits in the same folder as `DbDo.exe`, DbDo loads it automatically when it opens a SQLite database — there is nothing to configure. The bundle is 64-bit, matching DbDo's build. DbDo's build script (`buildDbDo.cmd`) downloads it for you and places it beside the executable, but you can also drop it in by hand. You can read the full function reference at the SQLean project: <https://github.com/nalgeon/sqlean>.

With SQLean loaded, two DbDo features come alive:

- **Filter by Regex** (Query menu, unbound by default) restricts the current column to rows matching a regular expression, using SQLean's `REGEXP` operator. The match runs inside SQLite rather than row-by-row in DbDo, so it is fast on large tables, and because the result is still a single-table SELECT it stays editable. An empty pattern clears the filter. This complements the client-side regex Find (which searches the displayed view) and Filter Records (which uses the ADO filter and cannot do regular expressions).
- **Extended field statistics** add `median`, `stddev`, `stddev_pop`, `variance`, `var_pop`, and `percentile_25` / `percentile_75` / `percentile_90` / `percentile_95` to the `Measure-Field` command at the dot prompt — for example, `Measure-Field amount median`. They are computed in SQL through SQLean's stats functions, alongside the built-in `count`, `min`, `max`, `sum`, `avg`, `longest`, and `shortest`.

SQLean's functions are also available in any SQL you run through Invoke-Sql — for instance `SELECT name FROM contacts WHERE name REGEXP '^Dr\.'` or `SELECT median(amount) FROM events`.

**The two SQLean files.** There are two distinct downloads, easy to confuse. `sqlean.dll` is the loadable *extension bundle* described above, which DbDo auto-loads into its own database connection. `sqlean.exe` is a separate *shell* — the standard SQLite command-line shell with the same extensions built in — from the companion project at <https://github.com/nalgeon/sqlite>. The build script fetches both and places them beside `DbDo.exe`. The shell powers the dot prompt's `!` lane: typing `!` followed by a line runs it in `sqlean.exe` against the current database file (opened read-only) and shows the output, giving you the full sqlite3/SQLean shell — `.tables`, `.schema`, `.dump`, `.stats`, and every SQLean function — without DbDo reimplementing any of it. The `/` lane is the complement: `/` followed by SQL runs through DbDo's own connection, where the result integrates with DbDo.

**What the bundle provides.** The SQLean main set spans, by module: crypto (hashing and encoding), define (user-defined functions and dynamic SQL), fileio (file and directory access), fuzzy (fuzzy matching and phonetics), ipaddr (IP-address functions), math (mathematical functions), regexp (regular expressions), stats (median, percentiles, and other statistics), text (Unicode-aware string functions), unicode (Unicode utilities), uuid (UUID generation), and vsv (reading delimited files as virtual tables). The regexp and stats modules are what power Filter by Regex and the extended field statistics above; the rest are available to any SQL you run.

If `sqlean.dll` is absent, these specific features report that the bundle is needed and the rest of DbDo is unaffected. The Test Extension Load command (Misc menu) reports whether the bundle loaded and which version.

## Help menu

Use the Documentation command, F1 (the standard help key), for help. With no argument, F1 shows the command index; from the dot prompt, `help <topic>` shows details for one command. The menu label matches EdSharp and FileDir: "Documentation".

Use the PowerShell Verb Reference command (no hotkey) to see the PowerShell verb taxonomy with each verb's category and a brief description. Use the Alternate Menu command, Alt+F10, to open an alphabetized list of every command with its current hotkey and a one-line summary. EdSharp and FileDir use the same command name; the chord and behavior are consistent across the three apps. The picker includes a detail panel below the list that shows the summary plus an optional longer description for the selected command — pick a command in the list and read what it does before pressing Enter to run it.

Every command in DbDo carries one-line summary metadata and an optional multi-line description, modeled on the EdSharp and FileDir convention. The summary appears in three places: in the menu status bar when the item has focus (so screen readers announce it as you arrow through the menus), inline after the chord in the Alternate Menu, and as the "Summary:" line in the Key Describer trace. The optional description, when present, gives the longer "why" or "gotchas" of the command and shows up in the Alternate Menu detail panel and at the bottom of the Key Describer trace. Commands without an explicit summary fall back to their menu label (minus the ampersand mnemonic markers) so every command is at least minimally self-describing.

Use the Where Am I command (no hotkey by default) to hear the current row, table, filter, and sort state in detail. Use the Test Screen Reader Speech command to probe DbDo's three speech paths (JAWS direct via COM, NVDA direct via the controller-client DLL, and the UIA live-region fallback for Narrator) and confirm which one is working with your screen reader.

Use the Key Describer command, Control+F1 (F1 for help, Control for "describe rather than fire"), to switch into a mode where every hotkey press announces the chord and its bound command instead of running it; press Control+F1 again to leave the mode. Use the Show Log Location command to print the path of `DbDo.log`, the per-session log file. Use the History of Changes command, Shift+F1, to read the chronological list of releases and what changed in each. Use the Readme Guide command to open `README.md` in your browser. Use the Open Website command to open the DbDo GitHub page. Use the Elevate Version command, F11, to ask GitHub for the latest release and offer to download and install it. Use the About command, Alt+F1, to read the version number and a brief credits block. The Help-menu commands carrying chords use the same name and chord as their EdSharp and FileDir counterparts (Documentation, History of Changes, Key Describer, Elevate Version, About, Alternate Menu).

The Help menu hosts a **Sample Databases** command that lists every bundled database — and any database you have added — and opens the one you choose. The list is built at runtime by scanning the per-user Samples folder, where each database lives in its own subfolder, so a database you drop in there appears alongside the bundled ones with no further setup. Three larger references also keep their own one-keystroke openers: **Open Convention Sample**, **Open Northwind Sample**, and **Open Chinook Sample**. Every one opens via the same code path File > Open Database uses, so the usual post-open behaviors — sort, filter, and position restore — apply. See "Bundled sample databases" below for what's in each.

## Commands available only at the dot prompt

A few commands have no GUI counterpart because they manage the dot prompt itself.

Use the Exit-Console command, `exit` (or `x` or `bye`), to leave the dot prompt while the GUI keeps running. Use the Switch-Focus command, `gui` (or `focus` or `window`), to bring the GUI forward from the console.

Use the Out-File command, `Out-File path.txt` (with aliases `output`, `tee`, and `o`), to send subsequent output to a file while also keeping it on screen. The `-a` flag appends rather than overwriting; `Out-File stdout` restores the screen-only behavior; bare `Out-File` reports the current target.

Use the Invoke-Script command, `Invoke-Script path.txt` (with aliases `read`, `script`, and `i`), to run a file of dot-prompt commands. Blank lines and lines beginning with `#` or `;` are treated as comments. Errors are reported per line but do not stop the script.

```
Out-File monthly_report.txt
Invoke-Script monthly_report.dbdo
Out-File stdout
```

## Mnemonic hotkey groups

This section restates every hotkey in the program, grouped by the part of the keyboard it lives on.

### Bare Shift+Letter family

Several capital-letter shortcuts fire from the data list (so capital letters typed in dialogs are not affected). These are the Say family: Shift+F (Say Filter), Shift+O (Say Order), Shift+G (Say Goto), Shift+R (Say Related), and the other Shift+letter say commands.

### Function-key family

F1 is Get Help; Shift+F1 is Show History; Alt+F1 is About DbDo; Control+F1 is Key Help Toggle. F2 is Edit Cell; Control+F2 is Pick Value. F3 is Search Next; Shift+F3 is Search Previous; Control+F3 is Find Regex; Control+Shift+F3 is the reverse. F4 is Current Windows; Shift+F4 is Say Windows Open; Control+F4 closes the window. F5 is Refresh View. F6 cycles the views; Shift+F6 cycles back; Alt+F6 and Control+F6 switch table and object (Alt+Shift+F6 and Control+Shift+F6 go backward). F7 is Choose Table; Shift+F7 is Say Tables. F8 starts a mark; Shift+F8 completes it; Alt+F8 starts an unmark; Alt+Shift+F8 unmarks a range. Shift+F9 is Say Notes; Control+F9 is Edit Notes. Alt+F10 opens the Alternate Menu. F11 is Elevate Version. Alt+F4 closes the program.

### Control-letter family

Control+C copies the current cell. Control+L, Control+E, and Control+D are List View, Edit View, and Details View. Control+F is Find Across All Columns; Control+Shift+F is the reverse. Control+J is Jump to Match in One Column; Control+Shift+J is the reverse. Control+B saves a bookmark and Control+Shift+B clears it. Control+M marks the current row; Control+Shift+M unmarks it. Control+N adds a row; Control+Shift+N copies the record as a new one. Control+O opens a database; Control+Shift+O opens a table in a new window. Control+P prints. Control+Q runs SQL. Control+R is Find and Replace Across Rows; Control+Shift+R is its regex variant. Control+S saves the database to a new path; Control+Shift+S is Statistics Column. Control+T edits tags; Control+U edits the url. Control+Shift+D deletes without confirmation. Control+Shift+G is Graphics Column; Control+Shift+X extracts regex matches. Control+G is Set Position. Control+Enter is Open Cell Value.

### Alt-letter family

Alt+B lists bookmarks. Alt+C appends the current cell to the clipboard; Alt+Shift+C appends the record. Alt+D is Database Summary; Alt+T is Table Summary. Alt+I imports data; Alt+X exports data. Alt+G repeats Set Position. Alt+L is Say Column Rest; Alt+Shift+L is Say Records Rest; Alt+M is Say Marked; Alt+Shift+M is Say Records Rest Marked. Alt+O is Order Records; Alt+Shift+O is Reverse Order. Alt+R is Recent Files. Alt+S is Select Columns. Alt+V invokes a script; Alt+Shift+V edits one. Alt+Z is Read Only Toggle; Alt+Shift+Z is Extra Speech Toggle. Alt+Shift+F is Filter Records; Alt+Shift+G is Graphics Grid. Alt+Enter is Table Properties. Alt+Backslash is Open in Explorer.

### Alt+Control extended-key family (virtual cell navigation)

Alt+Control+Home moves to the top-left cell; Alt+Control+End moves to the bottom-right. Alt+Control+RightArrow / LeftArrow / DownArrow / UpArrow move one cell in the named direction. Alt+Control+PageDown moves to the last row of the current column; Alt+Control+PageUp moves to the first row. Alt+Control+Numpad5 announces the current cell, or spells it on a second press.

### Alt+arrow family (parent-child drill)

Alt+RightArrow drills into a child table. Alt+LeftArrow returns to the parent row. Alt+Home pops the entire drill stack.

### Navigation family

Tab and Shift+Tab move an announcement-only column cursor across the current row. The arrow keys move the listview's row selection. Enter (or Control+D) opens Details View on the current row. Control+Tab cycles among recently-visited tables; Control+Shift+Tab cycles backward. Control+Shift+Home and Control+Shift+End jump to the first or last marked row; Control+UpArrow and Control+DownArrow step among marked rows. Shift+Home and Shift+End bulk-mark every row from the first through the current, or the current through the last; Alt+Shift+Home and Alt+Shift+End unmark the same spans.

### GraveAccent family

Control+GraveAccent is the GUI menu hotkey for Open Dot Prompt. Alt+Control+GraveAccent is a global hotkey that always acts: it toggles between GUI and console, whichever is not currently in front. (The former Alt+GraveAccent console-to-GUI hotkey was dropped as redundant with the toggle, and to stop reserving a second system-wide chord.)

## Schema documentation in comments

DbDo databases can carry their own documentation inside the schema itself, with no extra data-dictionary tables. SQLite stores the verbatim text of every CREATE statement -- comments included -- in sqlite_master, so comments written inside a CREATE TABLE body persist in the database file and travel with it.

DbDo reads the sqlite-docs convention (compatible with github.com/asg017/sqlite-docs):

A line beginning `--!` inside the CREATE TABLE body documents the table. When the text has the shape `key: value`, DbDo treats it as a YAML-style publishing field (name, author, version, description, abstract, relationships, or any key you choose); otherwise it is free prose. A line beginning `---` documents the column whose definition follows it.

Example:

    CREATE TABLE events (
      --! description: One row per discrete agenda entry
      --! author: Jamal Mazrui
      --! relationships: presents (contacts), located_at (locations)
      event_id INTEGER PRIMARY KEY,
      --- Start time in SQLite timestamp form
      starts TEXT,
      ...
    );

By DbDo convention, database-level metadata is the `--!` block of the builtin lookups table, since every DbDo database has one and the database file itself has no CREATE statement to carry comments.

Where it surfaces: Table Properties (Alt+Enter) shows a table's documentation and column documentation first; Database Summary shows the database-level block beneath the table count.

One caution, documented by SQLite users and the sqlite-docs project alike: ALTER TABLE ADD, RENAME, or DROP COLUMN rewrites the stored statement and can erase or mangle embedded comments. DbDo's regenerate-the-table approach to schema changes composes its own CREATE statements, so doc comments survive DbDo's edits; avoid raw ALTER TABLE on documented tables in outside tools.

## Standard fields

DbDo follows a convention for table design that the bundled sample databases illustrate and that the user manual recommends for new databases. Each table has the following "standard fields" in this order, with the "distinct fields" (the substantive columns) interleaved among them:

1. `<table>_id` — the primary key, integer, autoincrement (e.g., `teacher_id` in the `teachers` table).
2. `added` — datetime, default `current_timestamp`. When the row was created.
3. `edited` — datetime, default `current_timestamp`. Most recent change.
4. `url` — textline. A hyperlink, file path, or other openable reference associated with this row (added v1.0.66). Opened by the Open Url command (Ctrl+Shift+U) or read aloud by Say Url (Shift+U).
5. `tags` — textmemo. Comma-separated tag list for ad-hoc grouping. Read aloud by Say Tags (Shift+T).
6. `notes` — textmemo. Free-form annotations. Read aloud by Say Notes (Shift+N).
7. Foreign-key columns (`<parent>_id`, for child tables only).
8. **Distinct fields** — the substantive columns this table is actually for.
9. `marked` — boolean, default 0. The flag the Set-Mark command toggles.
10. `look` — computed text. A pipe-joined rendering of the most identifying distinct fields, designed for screen-reader readability. Appears in listboxes, quick-search displays, and the Details View's related-records section.
11. `unq` — computed text. Like `look` but optimized for uniqueness rather than readability. The intent is that the combination of column values in `unq` can confidently be considered unique for a row, so that an upsert-style command can update an existing row if `unq` matches, or insert a new row otherwise.

The TEXTLINE / TEXTMEMO / TEXTMARKDOWN type names on `url`, `tags`, and `notes` aren't standard SQLite types — they're convention labels DbDo reads from `PRAGMA table_info`. SQLite's type affinity treats them all as TEXT for storage. The difference is that the Edit View dialog renders a single-line input box for TEXT/TEXTLINE columns and a multi-line box for any type containing "memo" or "markdown". `tags` uses TEXTMEMO and `notes` uses TEXTMARKDOWN (a multi-line field whose content is expected to be Markdown) so the editor handles their longer content gracefully.

The `look` column is what makes the Details View's related-records section informative. When DbDo lists "Related students:" or "Related classes:", each line is one matching row's `look` value, so a single short string identifies who or what each related record is. Tables without a `look` column still show up under the right header but with a `(N row(s) -- no look column)` placeholder.

DbDo hides every column ending in `_id` (primary and foreign keys), bare `id`, and the bookkeeping columns (added, edited, marked, look, unq) from the listview by default. As of v1.0.68, `url`, `tags`, and `notes` are also hidden by default — they're "extended" data per row that the user reaches via the Say family (Say URL Shift+U, Say Tags Shift+T, Say Notes Shift+F9) or edits via Edit Cell (F2) or Edit View (Control+E), which shows the full field set. Use the Select Columns command (Alt+S) to override on a per-table basis; the override persists across sessions via DbDo.inix.

### Example schema using the standard fields

The `teachers` table from `sample.db`:

```sql
CREATE TABLE teachers (
    teacher_id  integer  primary key autoincrement,
    added       datetime not null default current_timestamp,
    edited      datetime not null default current_timestamp,
    name        text     not null,
    department  text,
    email       text,
    office      text,
    notes       text,
    tags        text,
    marked      integer  not null default 0,
    look text generated always as (
        rtrim(
            iif(length(name)>0,       name       || ' | ', '') ||
            iif(length(department)>0, department || ' | ', '') ||
            iif(length(office)>0,     office     || ' | ', ''),
            ' | '
        )
    ) stored,
    unq text generated always as (
        rtrim(
            iif(length(name)>0,  name  || ' | ', '') ||
            iif(length(email)>0, email,           ''),
            ' | '
        )
    ) stored
);
```

## Designing accessible databases

DbDo's standard-fields convention is not arbitrary house style; it applies, to relational tables, the same discipline that makes a spreadsheet accessible to a screen reader. The guidance Microsoft and the WCAG authors give for accessible spreadsheets and data tables comes down to a few principles, and each one has a direct relational counterpart that the convention bakes in. Following the convention is, in practice, the easiest way to get an accessible database; this section explains why, so the rules read as reasons rather than ritual.

**One table, one kind of thing.** Accessible-table guidance asks for a single, simple, rectangular region per logical subject — no tables stacked inside one sheet, no merged cells, no sub-tables. The relational equivalent is one table per entity, with relationships expressed as their own rows rather than nested inside a record. DbDo follows this literally: each table describes one kind of thing, and every association between two things lives in the `maps` table as a row of its own, never as a repeating group or a packed column. A screen-reader user can then navigate any table as a plain grid of uniform rows, and reach related things by a deliberate step (Enter Child, Say Related) rather than by decoding structure hidden inside a cell.

**A header row with clear, distinct names.** A spreadsheet is accessible only when each column has a header a screen reader can announce as it enters a cell, and when those headers are unambiguous. In a DbDo table the column names *are* that header row, and the convention keeps them descriptive and in `lower_snake_case` so each announces cleanly (`first_name`, `date_read`, not `f1` or `Column3`). Because the field names carry the meaning, the grid needs no second explanatory row — the very thing that most often breaks spreadsheet navigation.

**No blank rows or columns as separators.** In a spreadsheet, a blank row or column inserted for visual spacing tells a screen reader the table has ended, and the data beyond it falls out of table navigation. The relational model forbids this by construction — a table has no decorative gaps — and DbDo never inserts blank separator rows. The fixed order of the standard fields adds a second kind of predictability: once a user learns that the identity, then the timestamps, then the substantive fields, then `marked`, `look`, and `unq` appear in the same order in every table, the navigation learned on one table transfers to the next without relearning.

**A stable identity for every row.** Accessible data lets a reader name a location and return to it — the role Excel named ranges play. DbDo gives every row two identity columns for this: `look`, a short pipe-joined label built to be read aloud ("Ursula K. Le Guin | American"), and `unq`, a value intended to be unique so a row can be matched and updated rather than duplicated. `look` is what the related-records view and the pick lists speak, so a user always hears *which* row they are on, not merely a row number; `unq` is what the `maps` associations point at, so a relationship survives even as display text changes.

**Descriptive names at every level.** The same guidance that asks for named ranges and named sheets asks that the names be meaningful. DbDo extends this past the column: table names are singular-entity nouns, the primary key echoes the table (`book_id` in `books`), and a database is one folder named for its root. The payoff is that every announcement — a table name, a column name, a row's `look`, a related table — is a real word a listener can hold onto.

The shortest way to state the section: **a table a screen-reader user can work with efficiently is one whose structure lives entirely in its column names and its rows, with nothing meaningful hidden in layout.** The standard-fields convention is that idea written down. For the underlying general guidance, see Microsoft's "Make your Excel documents accessible to people with disabilities" and the WCAG techniques for data tables; DbDo's contribution is to carry those table-accessibility principles back into the schema itself, where a relational tool can actually enforce them.

## Bundled sample databases

DbDo ships four SQLite sample databases, each adapted to the standard column conventions described above. The first three are "textbook" databases borrowed from the broader SQLite community, each with a one-keystroke Help-menu command to open it (see "Help menu" above); the fourth is a "hobbyist" database that DbDo author Jamal Mazrui assembled based on research into the most popular real-world personal database categories. All open via the same code path File > Open Database uses.

**`sample.db`** — a small school domain. Four tables: `teachers`, `classes`, `students`, `enrollments`. Three rows each — twelve rows total — just enough to demonstrate parent-child relationships (a teacher teaches classes; students enroll in classes through the enrollments junction table). The minimum that exercises every standard column, the parent-child drill, and the related-records view. Opens automatically on first launch from a clean install.

**`northwind.db`** — the classic Microsoft Northwind sales sample, adapted to DbDo's column conventions. Eight tables: `categories` (8 rows), `suppliers` (10), `products` (24), `customers` (12), `employees` (9), `orders` (14), `order_details` (21), `shippers` (3) — 101 rows total. Rich parent-child shape: a category has products, a supplier has products, a customer places orders, an employee handles orders, an order has order_details, a shipper ships orders. Useful for exercising DbDo's parent-child drill against multi-level relationships with realistic row counts. **DbDo's adaptations are minimal and trivial**: every table has `<table>_id` primary keys (the canonical Northwind uses `CategoryID`, `SupplierID`, etc. — same idea, snake_case naming); the standard columns `added`, `edited`, `marked`, `notes`, `tags`, `look`, `unq`, and (new in v1.0.68) `url` are appended to each table; `notes` and `tags` are declared as `TEXTMEMO` so DbDo's Edit View dialog renders a multi-line box for them. The substantive columns (company, contact, city, country, phone for customers; first_name, last_name, title for employees; and so on) are preserved verbatim. Learn more about the canonical Microsoft Northwind:

- Microsoft Learn — [Northwind sample database overview](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/ef/loading-related-objects)
- GitHub — [microsoft/sql-server-samples / Northwind](https://github.com/microsoft/sql-server-samples/tree/master/samples/databases/northwind-pubs)
- Wikipedia — [Northwind Traders sample](https://en.wikipedia.org/wiki/Northwind_Traders)

**`chinook.db`** — the classic Chinook music-store sample, adapted to DbDo's column conventions. Nine tables: `genres` (14 rows), `media_types` (5), `artists` (20), `albums` (20), `tracks` (33), `employees` (8), `customers` (15), `invoices` (17), `invoice_items` (26) — 158 rows total. Three-deep parent-child chains (artist → album → track; customer → invoice → invoice_item) make this database the best for stress-testing the deeper drill stacks and the related-records view at multiple levels. **DbDo's adaptations are minimal and trivial** — same shape as the Northwind adaptation: primary keys are `<table>_id`; standard columns `added`, `edited`, `marked`, `notes`, `tags`, `look`, `unq`, `url` are appended; `notes` and `tags` are upgraded to `TEXTMEMO`. The substantive columns (name, title, artist_id, album_id, genre_id, etc.) are preserved verbatim. Learn more about the canonical Chinook:

- GitHub — [lerocha/chinook-database](https://github.com/lerocha/chinook-database) (Luis Rocha's reference repository; the SQL Server, MySQL, PostgreSQL, Oracle, and SQLite variants live here)
- SQLite Tutorial — [Chinook sample database](https://www.sqlitetutorial.net/sqlite-sample-database/) (with a clear ER diagram description)

**`cellar.db`** — a personal wine cellar. Three tables: `wines` (8 rows), `bottles` (10), `tastings` (4). Models the data model that CellarTracker, eSommelier, and VinCellar have refined: a wine identity (producer + vintage + varietal + region) is separate from the individual bottles you own (each with a bin location, purchase price, source, and consumption status), and tasting notes are stored separately so the same wine can have multiple tastings over years of aging. The standout analytical feature is the **drink-window query** — for every wine in the cellar, where does it sit in its drinkable range? — bundled as `Scripts/WineDrinkWindow.sql`. This database illustrates how a relational schema serves a real workflow that no flat list or spreadsheet can: "find the wines closest to the end of their drink window" is one ORDER-BY clause away.

All four databases use the same column conventions, so navigation commands and column-hiding rules behave consistently across them. The four are complementary rather than alternatives: `sample.db` is the gentle introduction; `northwind.db` is the relational textbook example; `chinook.db` is the deeper-hierarchy stress test; `cellar.db` is a starting point for hobbyist users who want to track their own real-world things.

### Edited-timestamp triggers

Each bundled database carries one SQLite trigger per table (named `trg_<table>_edited`) that maintains the `edited` column automatically. The trigger fires `AFTER UPDATE OF` the data-bearing columns and bumps `edited = CURRENT_TIMESTAMP` only when one of those columns actually changed in value. The `marked`, `added`, `edited`, `look`, and `unq` columns are deliberately excluded from the substantive set, so toggling the `marked` flag (Control+M / Control+U, or any of the range-mark commands) does NOT bump the timestamp — `marked` is a UI flag, not a content edit, and bumping the timestamp on every Mark Record would scramble "sort by recently edited" for users who use marking as a working-set tool.

The check uses `OLD.col IS NOT NEW.col` for each substantive column, joined by `OR`, so the trigger correctly skips both the "ADO only writes one column" case (Mark Record updates only `marked`) and the "Edit View writes every column with values unchanged" case (the edit dialog hands every field back even if untouched). NULLs are handled correctly because `IS NOT` is null-safe in SQLite, unlike the regular `<>` operator.

For users creating their own tables, the same trigger pattern is recommended:

```sql
CREATE TRIGGER trg_<table>_edited
AFTER UPDATE OF "col1", "col2" -- the data-bearing columns, including notes, tags, url
ON <table>
FOR EACH ROW
WHEN OLD."col1" IS NOT NEW."col1"
  OR OLD."col2" IS NOT NEW."col2"
  -- one OLD/NEW comparison per data-bearing column;
  -- omit <singular>_id, marked, added, edited, look, unq
BEGIN
    UPDATE <table> SET edited = CURRENT_TIMESTAMP
    WHERE <singular>_id = NEW.<singular>_id;
END;
```

DbDo itself does not write to `edited`; the timestamp behavior comes entirely from these triggers. The same pattern works on Access (using `ON UPDATE` data macros) but DbDo does not ship Access samples, so the trigger SQL above is SQLite-specific.

## Persistence

Between sessions, DbDo remembers the last opened database and table (relaunch goes straight there), the last folder used for each kind of file dialog (open/save/import/export remembered separately), per-table sort/filter/position/Select-Column lists within a session, and one named bookmark per session. Settings live in `%LOCALAPPDATA%\DbDo\DbDo.inix`.

The shipped `DbDo.inix` next to the executable holds configuration that ships with the install: `[General]` for `uiMode`, `[Keys]` for hotkey overrides. The per-user file in `%LOCALAPPDATA%\DbDo\` holds session state: `[Session]` for last-opened, `[Folders]` for remembered directories. The two files coexist; per-user values take precedence.

## Logging

DbDo writes a per-session log to `%LOCALAPPDATA%\DbDo\DbDo.log` (truncated at every program start). The log records database opens, table switches, errors, and the result of hotkey registration. Use the Show-Log command (no hotkey) to print the exact path.

## Bundled documentation

- `README.md` and `README.htm` — summary and quick start with a guided tour of `sample.db`.
- `Announce.md` and `Announce.htm` — release announcement.
- `DbDo.md` and `DbDo.htm` — this reference.
- `History.md` and `History.htm` — chronological release notes.
- `License.md` and `License.htm` — MIT License text.
- `CamelType_CSharp.md` — coding conventions used inside `DbDo.cs`, for developers.

# Development

This section is for anyone building DbDo from source or extending it.

## Requirements

DbDo is a single-file C# program targeting .NET Framework 4.8 on Windows x64. Compilation needs `csc.exe` from the .NET Framework 4.8 developer pack (Microsoft ships this in standalone form for free) — no Visual Studio required. Inno Setup 6 is used to build the installer. Pandoc is used to render the Markdown docs to HTML at release time.

## Build steps

```
buildDbDo.cmd
```

This single batch file compiles `DbDo.cs` to `DbDo.exe` with the right `csc.exe` flags (`/target:winexe`, `/platform:x64`, `/optimize+`, references to `System.Windows.Forms.dll`, `System.Drawing.dll`, and the Microsoft accessibility assembly), plus `jsc.exe` for the tiny `DbDo.dll` support assembly (compiled from `DbDo.js`). The build script auto-locates `csc.exe` by walking the .NET Framework install path; if your developer pack is in a non-standard location, edit the `set csc_path=` line at the top.

```
"%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" DbDo_setup.iss
```

This compiles the installer. `DbDo_setup.iss` is a standard Inno Setup script with a Welcome → SelectDir → Ready → Installing → Finish wizard, a `CurStepChanged` hook that fetches the SQLite ODBC and ACE drivers on the Installing step, and `[Files]` entries for the bundled artifacts. The output is `DbDo_setup.exe` in the project root.

## Architecture: one connection, two interfaces

The GUI form (`DbDoForm`) and the dot-prompt CLI (`Program.cmdXxx` static methods) share one `DbDoManager` instance. The manager owns the single `ADODB.Connection` and the active `ADODB.Recordset`. Both interfaces call manager methods (`openDatabase`, `selectTable`, `applyFilter`, etc.) rather than working directly with ADO objects. When an edit is committed in either interface, the manager's notification path tells the form to refresh its list view; the next dot-prompt command sees the new state automatically because it queries the same connection.

`CursorLocation = adUseClient` is set on every Connection.Open. This means filter, sort, and bookmark operations happen client-side after the recordset is fetched. The benefit is that the same code path works against SQLite, Access, Excel, and dBASE without per-provider quirks. The cost is that `.Filter` does not push down to the database engine.

## Coding style

DbDo.cs follows the **Camel Type** coding conventions documented in `CamelType_CSharp.md`. Key points:

- Hungarian-prefixed lower camel case variable names: `sName`, `iCount`, `bFound`, `aRows`, etc.
- The prefix `o` is reserved for COM objects only; managed-type instances use a class-name prefix instead.
- Constants follow the same naming pattern as variables (no `c_` prefix), but are declared with `const` or `static readonly` on lines separate from the variables.
- Methods rather than subprocedures; methods that return values where practical.
- `using` directives at the top of the file, alphabetized within their group.
- `foreach` over `for` when iterating a collection.

Read `CamelType_CSharp.md` for the full set of rules.

## Layout by Code

DbDo uses an approach called Layout by Code (LbC) for every dialog it creates. The idea is that a programmer composes a dialog by writing a sequence of "add this control" calls in code, in the order the user will encounter them — rather than dragging boxes around in a visual designer or hand-writing pixel coordinates. The result is dialogs that are screen-reader-friendly by construction: tab order matches reading order, every input control has a label that the reader announces, and the layout flows from top to bottom in one pass.

The concept was developed by Jamal Mazrui (the original author of DbDo) starting in 2006, with an AutoIt implementation that established the vocabulary still in use today. Layout by Code has since been ported to several languages — wxPython, JScript .NET (the Homer Application Framework), and now C# in DbDo. Each port keeps the same intuition: a dialog is a vertical stack of labeled controls, the programmer writes them out one at a time, and the framework handles the spacing, the focus-tip plumbing, the tab indices, and the accept/cancel wiring. What is notable about DbDo's port is that it preserves the **vocabulary** of the earlier ports — methods like `addInputBox`, `addMemoBox`, `addPickBox`, `addComboPickBox` — so a programmer who learned LbC in AutoIt or wxPython can pick up the C# version without retraining.

### The conceptual model: bands, the layout cursor, and dialog units

The earlier LbC implementations talk in terms of three concepts that are worth introducing because they underpin the API even where the C# port hides them behind a simpler stack.

A **band** is a horizontal row of related controls — for example, a Label plus an associated TextBox, or a Label plus an associated ComboBox. The Microsoft Official Guidelines for User Interface Design, published in the late 1990s and based on years of usability research, specify how much horizontal and vertical space should separate the controls within a band, between consecutive bands, and around the dialog's edges. The original AutoIt LbC implementation exposed bands and groups as first-class objects with their own ID numbers, addressable via functions like `_lbcStartBand` and `_lbcStartHGroup`. The Python version simplified this: each call to an `Add*` method starts a new band; adding a separator advances to the next group. DbDo's C# port simplifies further still: every control added with one of the `addX` methods is its own band, stacked vertically. The horizontal-band model is still available — `addInlineInputBox` puts a label and a textbox on a single row — but the default is one control per row, top to bottom.

A **layout cursor** is the invisible point on the dialog where the next control will be placed. In the original AutoIt port the cursor was an addressable global state — `$nLbcCol` and `$nLbcRow` for column and row — and one could move it manually with `_lbcCtrlSetLeft` or `_lbcCtrlSetTop`. In the C# port the cursor is an implementation detail managed by a `FlowLayoutPanel` plus a vertical-stack `Panel`; the programmer never sees it. What survived from the older implementations is the *concept* that controls are added sequentially and spacing is automatic.

A **dialog unit** is the Microsoft-defined measurement that LbC uses internally for spacing decisions. A horizontal dialog unit is a quarter of the average character width of the system font; a vertical dialog unit is an eighth of the average character height. The guidelines specify three dialog units between a label and its associated control, four between related controls in the same group, and seven between unrelated controls in consecutive groups. DbDo's C# port encodes these in named constants — `DefaultButtonHeight`, `DefaultLineHeight`, `DefaultPadding`, `DefaultRowGap`, `DefaultListHeight`, `DefaultMemoHeight`, and so on — rather than recomputing from font metrics, because modern Windows dialogs are dpi-aware and the WinForms framework's own font scaling handles the dialog-unit-to-pixel translation for us.

### Why LbC matters for a screen-reader audience

Visual dialog editors — the kind that ships with Visual Studio for WinForms, or with Glade for Gtk — produce dialogs whose controls are positioned by pixel coordinates the programmer specified by dragging boxes around. The pixel coordinates fix the visual layout but say nothing about reading order; in WinForms, reading order is governed by `TabIndex`, which a sighted programmer might forget to set, or set inconsistently with the visual layout. A screen-reader user navigating that dialog with Tab will visit controls in `TabIndex` order, not visual order, and the mismatch is one of the most common accessibility bugs in commercial software.

LbC eliminates the problem by construction: there is no separate `TabIndex` to forget. Each `add*` call increments an internal `iTabIndex` and assigns it to the new control. Reading order is the order of `add*` calls, which is also the order in which the programmer is thinking about the form. The visual layout is a deterministic function of the same sequence. The two cannot drift apart.

A second screen-reader benefit is the **focus-tip plumbing**. Each `add*` method takes an optional `sTip` parameter that is wired to a status bar at the bottom of the dialog. When the user tabs into the control, the tip text is written to the status bar; screen readers announce status-bar changes through their live-region facility, so the tip is read aloud automatically without forcing a popup dialog. This pattern is borrowed directly from Homer LbC (the JScript .NET port) which introduced it specifically because JAWS suppresses ordinary WinForms tooltips on tab navigation; status-bar messages, by contrast, are reliably announced.

A third benefit is the **memo/AcceptButton coordination**. When the user is typing into a multi-line TextBox (one created via `addMemoBox` or `addTextMemo`), pressing Enter should insert a newline rather than submit the dialog. The framework arranges this transparently: while focus is on a memo control, the form's `AcceptButton` is temporarily cleared, so Enter is delivered to the textbox as a newline insertion; on `LostFocus` the `AcceptButton` is restored, so Enter from single-line fields still submits. The Homer LbC convention is preserved in the C# port verbatim — programmers don't have to think about it.

### Anatomy of an LbC dialog

The typical usage pattern in DbDo is one `using` block per dialog:

```csharp
using (LbcDialog dlg = new LbcDialog("Configuration", this))
{
    TextBox  tbMode  = dlg.addInputBox("UI mode:",       "both", "How DbDo launches");
    CheckBox cbBeep  = dlg.addCheckBox("&Beep on errors", true,  "Audible cue on failure");
    TextBox  tbNote  = dlg.addMemoBox("Startup note:",   "",     "Free-form text shown at launch");
    if (dlg.runOkCancel())
    {
        string sMode = tbMode.Text;
        bool   bBeep = cbBeep.Checked;
        string sNote = tbNote.Text;
        // ... persist or apply
    }
}
```

Each line maps to one decision: what kind of control, what its label says, what the initial value is, what tip to surface when the user tabs into it. The framework handles everything else — label placement, accessible-name wiring, tab order, button-band insertion, modal presentation, and disposal. The `using` block ensures the underlying WinForms `Form` is disposed when control leaves the block.

The `runOkCancel()` call is the natural close. It appends an OK button and a Cancel button at the bottom, runs the dialog modally, and returns `true` when the user pressed OK (or Enter from any single-line field) or `false` for Cancel, Escape, or close. For dialogs with more buttons or different labels, `runWithButtons(new string[] { "Apply", "Apply All", "Cancel" })` is the more general form; it returns the label of the button the user pressed, or `""` if the user closed via the system menu. Per a convention added in v1.0.61, when only ONE button label is given (typically `"OK"` for a read-only memo dialog), that one button is wired as both the `AcceptButton` and the `CancelButton`, so Escape closes the dialog the same way pressing OK does.

### The add-control vocabulary

DbDo's LbcDialog exposes the following methods for assembling a dialog. Two naming patterns coexist: the **bare-control adders** (`addLabel`, `addTextBox`, `addCheckBox`) and the **labeled-control adders** that mirror the Homer LbC naming (`addInputBox`, `addMemoBox`, `addPickBox`, `addComboPickBox`). The labeled adders emit a `Label` first and then call the bare adder; both flavors are kept because some sites favor the explicit two-call form, others favor the compact one-call form.

`addLabel(string sText)` adds a static text label as its own row. Useful for paragraphs of instruction at the top of a dialog or above a group of fields. Returns the `Label`.

`addInputBox(string sLabel, string sValue, string sTip)` adds a label, a single-line `TextBox` below it, and registers the tip with the status bar. Returns the `TextBox`. The textbox's `AccessibleName` is set from the label so screen readers announce the field correctly on tab-in. This is the workhorse method for editing scalar values: names, paths, single integers, simple regex patterns.

`addInlineInputBox(string sLabel, string sValue, string sTip)` is the horizontal variant: label and textbox on a single row, sharing the same band. Used in the Edit View dialog where vertical density matters and there are many short fields.

`addMemoBox(string sLabel, string sValue, string sTip)` adds a label and a multi-line `TextBox` (96 pixels tall by default) suitable for longer text — notes, tags, descriptions. While focus is inside the memo, the dialog's `AcceptButton` is temporarily cleared so Enter inserts a newline rather than submitting; `LostFocus` restores it. Returns the `TextBox`.

`addCheckBox(string sLabel, bool bValue, string sTip)` adds a labeled checkbox. The label is part of the checkbox itself (WinForms checkboxes have an integral label), so there is no separate Label row. Tab moves into the checkbox, Spacebar toggles it. Returns the `CheckBox`.

`addListBox(IList<string> lNames, string sSelected, string sTip)` and the labeled variant `addPickBox(string sLabel, IList<string> lNames, string sSelected, string sTip)` add a single-selection list box (100 pixels tall by default). Arrow keys navigate; Enter from inside the list activates the AcceptButton. Returns the `ListBox`. The `sSelected` parameter pre-selects an item by its display string.

`addComboBox(IList<string> lNames, string sSelected, string sTip)` and the labeled variant `addComboPickBox(string sLabel, ...)` add a drop-down list with no free-text entry (`ComboBoxStyle.DropDownList`). Returns the `ComboBox`. Used for closed sets of choices: file extensions, encodings, sort orders.

`addRadioButton(string sLabel, bool bChecked, string sTip)` adds a single radio button. Multiple radio buttons added consecutively automatically form a group (WinForms scopes radio-button mutual-exclusion by container parent, and the LbC stack-panel is one container). The first-added or any with `bChecked=true` is initially selected.

`addNumericUpDown(string sLabel, int iValue, int iMin, int iMax, string sTip)` adds a spin control for bounded integer input. Useful for counts and limits.

`addSeparator()` inserts a thin horizontal line between rows. Acts as a visual group divider; corresponds to the Microsoft seven-dialog-unit spacing between unrelated controls.

### Run methods

After all controls are added, the caller invokes one of two run methods to make the dialog visible and modal.

`runOkCancel()` appends OK and Cancel buttons in a right-justified band at the bottom of the dialog, sets OK as the `AcceptButton` and Cancel as the `CancelButton`, shows the dialog, and returns `true` if the user dismissed it with OK or `false` for any other reason (Cancel, Escape, Alt+F4, system menu close).

`runWithButtons(string[] aButtonLabels)` is the general form. It appends every label in the array as a button (right-justified, first-listed on the left), sets the first one as the `AcceptButton`, and sets any button whose label is `Cancel` or `Close` as the `CancelButton`. It returns the label of the button the user pressed. When only one button label is provided, that one button is wired as both Accept and Cancel so Escape closes the dialog like clicking OK would.

The same returned form is also available after the run via the `form` property, in case the caller needs to inspect or tweak something the high-level API doesn't expose (such as adding an Icon).

### Looking up controls by name

Every control added to an LbC dialog is registered in a `widgets` dictionary keyed by an auto-generated name of the form `<Kind>_<CleanedLabel>` — for example, `TextBox_UI_mode`, `CheckBox_Beep_on_errors`. Two convenience access patterns are supported:

- **By reference:** the `addX` methods return the inner control; the caller can keep a local variable and read its properties after `runOkCancel`. This is the common pattern.
- **By name:** the `findControl(string)` method, plus typed accessors `getTextBox(string)`, `getCheckBox(string)`, `getComboBox(string)`, `getListBox(string)`, `getRadioButton(string)`, `getNumericUpDown(string)`, and `getLabel(string)`. These return the registered control or `null` if no widget by that name was added. Useful in generic event handlers that receive only the sender, or in walker code (validation, import/export) that iterates over the dialog's controls without keeping references.

### Focus tips and the status bar

Each `addX` method accepts an optional `sTip` string. When the user tabs into the control, the tip is written to the dialog's bottom status bar (`lblStatusBar`), where screen readers announce it as a live-region update. The mechanism is a `GotFocus` handler attached during `addX`; `LostFocus` clears the bar. The pattern lets the programmer add inline help without the user having to read a separate "?" button — the help comes to the user when they need it, in the same line of speech that the reader uses for the focus event.

The tip text is also assigned as the control's `ToolTipText` so mouse users hovering on the field see the same text as a tooltip popup. JAWS frequently suppresses tooltip popups during keyboard navigation, which is why the status bar is the primary delivery channel.

### Comparison with the original LbC implementations

The DbDo C# port intentionally simplifies what the original AutoIt LbC offered. The AutoIt version exposed bands and groups as first-class objects with manipulation functions (`_lbcStartBand`, `_lbcBandHCenter`, `_lbcBandEvenSpace`, `_lbcWinRespace`, and dozens more), allowed precise pixel-level positioning of controls after the fact, and let the programmer change widths or heights with reference-band alignment. The DbDo port omits these because:

- WinForms anchors and Dock properties make most band-respacing unnecessary — the form resizes the right way automatically.
- Most DbDo dialogs are small (under ten controls), so the gymnastics of fine alignment is not needed.
- A simpler API is easier to teach to contributors who haven't seen the AutoIt original.

What is preserved across all four LbC ports — AutoIt, wxPython, JScript .NET (Homer), and C# (DbDo) — is the **practice** of writing dialogs as a sequence of `addX` calls in reading order, with labels above or beside each control, tips wired to a status bar, tab order = call order, and submit/cancel wiring handled by the framework. A programmer fluent in one port can read code in any of the others and find the same shape.

Convenience-dialog wrappers like the Python port's `DialogShow`, `DialogConfirm`, `DialogInput`, `DialogPick`, `DialogMultiInput`, and `DialogBrowseForFolder` are not provided as named methods in the C# port — DbDo's call sites typically need only `runOkCancel` plus one or two add calls, so the wrappers would only save a few characters. The patterns are documented here in case a contributor wants to add them.

### Open-ended extension

LbcDialog is a public class (in `DbDo.cs`, line 5668 onward). It is reusable outside the DbDo command set — a script invoked via `Invoke-Script` could in principle construct its own LbcDialog through the C# COM bridge, though no bundled script currently does so. If you write a new GUI command for DbDo and need a custom dialog, use LbcDialog. Do not hand-roll a Form with manual TabIndex assignment; the screen-reader-friendliness will degrade.

### EdSharp-style text-edit hotkeys

Inside every LbcDialog, single-line text inputs (added via `addInputBox`, `addInlineInputBox`, `addTextLine`) and multi-line memos (added via `addMemoBox`, `addMemo`, `addTextMemo`) recognize a set of EdSharp-style hotkeys in addition to the standard Windows text-editing chords. The added chords are non-conflicting — they only act when the corresponding standard chord would have done nothing, or they bind to chords (F8, Ctrl+F8, Ctrl+D) that have no standard meaning in a TextBox.

The pattern is adapted verbatim from Jamal Mazrui's HomerLbc framework (`HomerLbc_40.js` lines 995–1162), which itself derives from EdSharp's text-editor conventions. The shared idea: when a screen-reader user navigates by character or word, the absence of a visible cursor and the difficulty of selecting precise ranges makes the standard Windows clipboard hotkeys awkward; line-oriented and "mark start / mark end" workflows fit screen-reader navigation better.

The hotkey set is uniform across all text-edit contexts in LbcDialog. The following table lists the chord, what it does when text is selected, and what it does when no text is selected (the asymmetry preserves backward compatibility with the standard Copy / Cut behavior).

| Chord     | With selection            | Without selection                                |
|-----------|---------------------------|--------------------------------------------------|
| Ctrl+C    | Copy selection (standard) | Copy the current line                            |
| Alt+C     | Append selection to clipboard | Append the current line to clipboard         |
| Ctrl+X    | Cut selection (standard)  | Cut the current line; speak the next line       |
| Alt+X     | Cut and append            | Cut current line and append to clipboard         |
| F8        | (same)                    | Mark start of selection at the caret             |
| Shift+F8  | (same)                    | Complete selection from saved start to caret    |
| Ctrl+F8   | Copy ALL text in the field to the clipboard                               ||
| Alt+F8    | Speak ALL text in the field via the live region                            ||
| Ctrl+D    | Delete the current line; speak the next line as feedback                  ||

Notes on the workflow:

- "Append to clipboard" (Alt+C, Alt+X) is the hotkey that EdSharp and Homer made famous: you copy or cut a line, navigate elsewhere, and copy or cut another line, and the clipboard accumulates them all in order, each on its own line. Useful when collecting several pieces of text from across a memo into a single destination.
- The F8 / Shift+F8 mark-start / mark-end pair is the screen-reader-friendly alternative to "Shift+arrow keys to extend a selection." Mark the start with F8, navigate freely (the selection is not yet drawn), then press Shift+F8 at the end point and the framework selects the entire range at once. JAWS, NVDA, and Narrator announce both the start character (at F8 time) and the final length (at Shift+F8 time).
- Ctrl+F8 / Alt+F8 "all text" operations are the natural complement to the line operations: Ctrl+C copies a line, Ctrl+F8 copies everything; Alt+C reads (well, appends) a line, Alt+F8 reads everything aloud.
- Ctrl+D deletes the current line without putting anything on the clipboard — useful for cleanup work where the deleted line is not worth keeping. The next line is read aloud so the user knows where the caret ended up.

**Master enable flag:** the entire set is controlled by `[Lbc] extraKeys` in `DbDo.inix`. Default is `Y` (on). Set to `N` to disable the EdSharp hotkeys entirely and let standard Windows text-edit behavior pass through unchanged. The setting is read once on first use and cached for the session.

**Status bar feedback:** every hotkey updates the dialog's status bar with a short label (`Copy Line`, `Append to Clipboard`, `Cut Line`, `Start Selection`, `Complete Selection`, `Copy All`, `Read All`, `Delete Line`). The live region echoes the action via a brief utterance (typically the operation name plus the affected text or character count) so the screen reader confirms each action without the user having to look at the status bar.

## GUI versus CLI response patterns

DbDo operates in two modes: a Windows Forms GUI with a ListView grid, menus, and modal dialogs, and a dot prompt running in a console window. Most commands work in both modes, with the same canonical verb name and the same effect on the database. The user-visible response is different in each mode by design — the GUI uses focus changes, status-bar updates, dialogs, and the screen-reader live region; the CLI prints lines of text to stdout and reads input from stdin. The principle: **same data effect, mode-appropriate confirmation**.

The remainder of this section walks through the command families and describes how each fits the GUI / CLI duality. Where a command does not reasonably apply in one mode, the table notes it explicitly.

**Navigation commands** (Step-Record family, Set-Position, Find, Jump-Record, Search-Next, Search-Previous, Step-InitialChange). GUI: move the listview row selection and the ADO cursor; the cell-changed handler speaks the new row via the live region. CLI: print the new row's summary (column-equals-value pairs from the display fields) on stdout. The data-layer behavior is identical — same ADO MoveNext / MovePrevious / AbsolutePosition call — only the confirmation differs.

**Display commands** (Show-Object, Show-Table, Show-Schema, Show-Status, Show-Related, Show-Log, Show-History, Show-Readme). GUI: open a read-only LbcDialog with a multi-line TextBox containing the requested content; OK to dismiss; Control+C inside the TextBox copies. CLI: print the same content to stdout, one line per row, with field-name prefix where helpful. Some commands have a GUI-only LbcDialog form (Show-Object, Show-Related) and a CLI-equivalent multi-line text dump; some are inherently textual (Show-Schema, Show-Log) and use the same wording in both.

**Speech-only commands** (Say-Status, Say-Path, Say-Yield, Say-Tables, Say-Marked, Say-Updated, Say-Notes, Say-Tags, Say-Column, Say-Position, Say-SortFilter, Say-YieldMarked). GUI: push text through the LiveRegion (JAWS COM, NVDA controller DLL, or UIA live-region fallback) so the screen reader speaks it without moving focus. Double-press the same chord within two seconds: open a read-only multi-line LbcDialog with the same text, useful for review and Control+C copy. CLI: print the same text to stdout. The "double-press shows dialog" gesture has no CLI analog and is silently dropped — pressing the verb twice in a CLI session just prints the same lines twice, which is harmless.

**Edit commands that mutate one row** (New-Record, Set-Record, Set-Cell, Remove-Record, Copy-Record, Set-Mark, Clear-Mark, Save-Bookmark, Restore-Bookmark, Clear-Bookmark). GUI: open an LbcDialog with per-field TextBoxes (`RecordEditDialog` for full row, single TextBox for Set-Cell), validate, OK commits, Cancel discards. The dialog respects per-field regex patterns from `[Validation:<table>]`. CLI: take the column-equals-value pairs on the command line (`set-cell email = a@b.com`) or for full row editing print each editable field and prompt for a new value at the prompt; same validation. No-arg CLI invocations of New-Record fall back to prompting line by line; this works but is uncommon — the typical CLI editing path is Set-Cell or Set-Field with the value on the command line.

**Edit commands that mutate many rows** (Update-Field, Mark-Range, Unmark-Range, Step-InitialChange, Extract-Regex). GUI: prompt for any missing parameters in an LbcDialog (find/replace strings, column, regex), execute the batch, refresh the listview, speak a summary like "Marked 17 of 22 rows (rows 5 to 26)." CLI: take parameters on the command line, run the batch, print the same summary to stdout. The batch logic is identical — only the input-collection layer differs.

**Table-level commands** (Open-Database, Close-Database, Select-Table, Backup-Database, Save-DatabaseAs, Test-Database, Test-Driver, Get-Table, Sync-Session, Update-View). GUI: OpenFileDialog or LbcDialog for inputs; status-bar update on completion. CLI: take a path or table name on the command line; print a one-line confirmation on completion. Test-Database and Test-Driver are inherently textual reports (lists of integrity-check results or driver-presence checks); both modes show the same lines, the GUI in an LbcDialog and the CLI on stdout.

**Filter and sort commands** (Select-Record, Reset-Filter, Sort-Object, Reset-Sort, Select-Column). GUI: LbcDialog for criteria input, with helpful prompt labels. CLI: take the criteria string directly (`select-record where year > 2020`). Both apply the same ADO Filter / Sort properties; the listview refreshes in GUI mode, the CLI prints the new row count.

**File-export commands** (Export-Data, Import-Data, New-Chart, New-Plot, Out-File). GUI: SaveFileDialog or LbcDialog for output path, then run the Office-COM or file-writing path; open the result in the default app on completion. CLI: take the output path on the command line, write the file, print the saved path on stdout. New-Plot is the meaningful asymmetry: charts inherently need a graphical viewer, so the CLI form prints the Describe-Column textual summary instead and tells the user to invoke the command from the GUI to see the chart. New-Chart works the same way.

**Configuration and meta commands** (Edit-Configuration, About-DbDo, Get-Help, Get-Verb, Switch-KeyDescriber, Switch-Focus, Enter-Console, Exit-Application, Exit-Console, Exit-Child, Enter-Child, Elevate-Version). GUI: LbcDialog or message box. CLI: print the equivalent text to stdout, or in the case of Edit-Configuration, bounce to the GUI dialog if a form is available (Both mode) or print the .ini path for hand-editing in CLI-only mode. Switch-Focus is GUI-specific in effect (brings the form to the foreground) but is meaningful from CLI when both UIs are running.

**Commands without a meaningful CLI form**: Open-FileFolder (launches Windows Explorer — works from either mode, but the action is the same shell-execute), Switch-Focus (no-op in CLI-only mode; prints a notice), Enter-Console (a no-op when issued from the CLI; prints "already at the dot prompt"). The reverse — commands that exist GUI-only and have no CLI verb — is now a closed set: there are none. Every menu item in DbDo has a canonical verb that the dot prompt accepts.

**The general rule**: every command's data effect is mode-independent; every command's confirmation layer is mode-appropriate; every CLI invocation accepts unique prefix matches against canonical verbs (so `first` resolves to `step-record-first` if no other canonical starts with `first`, `meas col` resolves to `measure-column`, and so on). Where a typed prefix is ambiguous, the CLI prints the candidate list so the user can disambiguate by typing more characters.

## Office automation: always CreateObject, never GetActiveObject

DbDo never attaches to a running Word or Excel instance. Every Office-using export path makes a fresh hidden instance, drives it to produce the requested file, then calls `Quit()` and releases the COM object. The reasons: attaching to a running instance would mutate settings the user has tuned for their session, driving a running instance through SaveAs and Quit risks side effects on documents the user has open, and the behavior would be non-deterministic depending on whether Word or Excel happens to be running.

## The Inix file format

DbDo can import from and export to **.inix** files, an extended .ini format designed to be a friendlier serialization than JSON or TOML for both configuration data and tabular data. The defining feature: a .inix file is plain text in any editor, with no escape characters (no `\n` notations, no doubled quote marks, no JSON-style backslash escaping). What you see is what is stored. DbDo's own settings file, `DbDo.inix`, uses the format too: every classic .ini construct still works there, and the shipped template's `[ConnectStrings]` section demonstrates the fenced multi-line form for values that contain `=` characters. (On first launch after upgrading, a per-user `DbDo.ini` from an earlier version is renamed to `DbDo.inix` automatically; no conversion is needed because every classic ini file is already a valid Inix file.)

The format extends the familiar Windows .ini convention with three powerful additions:

**Multi-line string values.** When a key's value spans multiple lines, two syntaxes are available. The plain form starts with `key=` and an empty value on that line; subsequent lines are part of the value until the next key or section header. The fenced form uses backtick or triple-quote delimiters on lines by themselves and accepts any character verbatim inside, including `=` and `[`. Example:

```
[notes]
short_note = single line here
long_note =
This is a multi-line note.
The plain form stops at the next key
or section header.

complex_note=`
This value contains an = sign and a [bracket] on the next line:
key=this is not a new key
[this is not a new section]
`
```

The fenced form is the reliable way to embed `=` or `[` characters in a value, since the plain form would interpret a line that looks like a key or section header as terminating the value.

**Sections as either a dictionary or a list.** When sections have unique names (`[Replace dog with cat]`, `[Replace sheep with goat]`), the file is a dictionary of dictionaries — typical configuration use. When sections are anonymous (`[]`) or follow the `[RecordNNN]` pattern, the file is a list of records — tabular data. DbDo's Export Data writes the list-of-records form when exporting a table, choosing the leading-zero width on the record number so that ASCII sort of section names matches numeric order: 5 records use `[Record1..5]`, 99 records use `[Record01..99]`, 999 records use `[Record001..999]`.

**Implicit Global section.** If a .inix file starts with key=value lines before any explicit section header, those keys go in an implicit `[Global]` section. This is useful for configuration files where a few top-level settings apply to the whole file, with sections below defining individual operations.

**Comment syntax.** A line whose first non-whitespace character is `;` or `#` is a comment. A section can be commented out by prefixing its name with `;` inside the brackets — `[;Replace dog with cat]` skips that section's entire body without requiring you to comment out each line. The semicolon at the start of `[;` is the comment-out marker; the rest of the section name is preserved so you can uncomment later by removing one character.

Files are written with UTF-8 BOM and CRLF line endings, matching DbDo's other text-file conventions. An .inix file can be opened in Notepad, read by a screen reader without any special viewer, and edited line by line.

Use cases for .inix:

- **Plain-text rendition of a small table** — exports the current view to a file you can read, search, edit, and re-import. The list-of-records form keeps each row visually separated by a blank line and a section header, which screen readers can navigate by heading-jump if rendered to Markdown or HTML.
- **Configuration with multi-line values** — when settings need free-form text that JSON would force you to escape (think: a SQL query as a setting value, or a multi-paragraph description).
- **Hand-authored tabular data** — easier to write by hand than CSV when individual cells have multi-line content; easier to read in an editor than JSON.

The .inix format was originally designed for the KeyLine toolkit; DbDo adopts it as another supported import and export format.

### Importing with a transfer map

Besides the list-of-records form above, an .inix file can act as a *transfer map* — a field-by-field recipe for pulling records out of a foreign database file and into the current table, renaming and reshaping each field on the way in. This is the modern heir of the transfer files DbDo's ancestors used, and it is reached through the **Transfer Import** command on the File menu (open the destination database and select the destination table first). Supported source formats are dBASE (`.dbf`), Access (`.mdb`/`.accdb`), and SQLite (`.db`/`.sqlite`/`.sqlite3`).

Each section of the file is one named mapping; you pick which one to run. Within a section, every line is a mapping of the form:

```
destField = sourceField [ ; jscript-expression ]
```

The destination field on the left is filled from the named source field on the right. If a JScript expression follows a semicolon, the value is passed through it first. Inside the expression, `$v` is the source field's value and `$other_field` is the value of any other field in the same source row. A line beginning with `@table =` names the table to read inside the source file (it defaults to the source file's base name, which is what a single-table dBASE file like `cts.dbf` wants). Two rules give the map its flexibility: an **empty source value is skipped**, so the destination keeps its default rather than being blanked; and a **destination field named on more than one line accumulates**, so several source fields can be concatenated into one. A worked example, importing a DOS-era CTS dBASE contact file and reformatting its packed `YYYYMMDD` dates:

```
[cts_contacts]
@table       = cts
created      = updated ; $v.substr(0,4) + "-" + $v.substr(4,2) + "-" + $v.substr(6,2)
title        = title
first_name   = firstname
last_name    = lastname
enterprise   = company
office_phone = workphone
extra_info   = notes ; $v + "\n"
extra_info   = text
```

Because the expression language is JScript .NET — the same engine the build already uses for snippets — anything it can compute is available: string slicing, conditionals (`$v == "Y" ? "Yes" : "No"`), arithmetic (`Number($v) * 100`), and so on. A bad expression falls back to copying the value unchanged rather than aborting the import, and as with the other importers a single bad row never sinks the batch.

## Report templates

The Produce Report command on the File menu renders a report over the current filtered set, in the current sort order, to a Markdown file that then opens in your editor. Markdown reads fine as plain text in a screen reader and converts cleanly to HTML, DOCX, or PDF with pandoc or Word, so one report definition serves every downstream format.

A report is defined in an `.inix` file — the same family as a transfer map — with one section per report, chosen from a picker when you run the command. Within a section, four optional bands shape the output: `header` (emitted once at the top), `detail` (emitted once per record, usually a fenced multi-line block), `separator` (emitted between records), and `footer` (emitted once at the end). An optional `@table = <name>` records which table the report is for; open that table first.

Inside any band, `$field` (or `${field}` when the name must abut other letters) expands to the current record's value, and `{{ jscript-expression }}` evaluates a JScript expression — the same `$field`/JScript model as a transfer map, so a "work-preferred" line is just `{{ $office_phone || $home_phone }}`. A `{# comment #}` is dropped. The rendering is mail-merge style: a line whose only content is a field or expression that comes out blank is dropped (so an absent middle name or second address leaves no gap), while a purely literal line — including a deliberately blank one and any Markdown structure — is kept verbatim. The `separator` value may be literal text or one of the keywords `blank` (a blank line), `rule` (a Markdown horizontal rule), or `page` (a form-feed page break).

For example, a detail band of

```
## {{ ($first_name + " " + $last_name).replace(/\s+/g, " ").trim() }}
$title $first_name $middle_name $last_name
$job
$enterprise
$address1
$address2
$city, $state $zip
$nation
```

prints each contact as a navigable Markdown heading followed by a clean address block, with the blank-field lines suppressed. A ready-to-use `report.inix` with this and a phone/email report ships alongside DbDo. Producing a report never moves your place in the data.

A report can optionally group its records. Adding `@group = <field>` sorts the records by that field automatically — so a grouped report is never wrong because you forgot to sort first — and fires two more bands as the value changes: `group_header` at the start of each group and `group_footer` at its end. In a footer band (the report `footer` or a `group_footer`) you also get aggregate values as ordinary `$`-fields: `$count` is the number of records in scope, and for a numeric column `$sum_<field>`, `$avg_<field>`, `$min_<field>`, and `$max_<field>` summarise it (blank and non-numeric values are ignored). Grouping is entirely optional — a report with no `@group` is exactly the flat header/detail/footer report described above. For instance, grouping contacts by state:

```
@group = state
group_header = """
## $state
"""
detail = """
- $first_name $last_name
"""
group_footer = """
*$count contact(s) in $state.*
"""
```

gives a heading and a running count for each state, with a single sort you never had to ask for. The bundled NFB convention sample ships a `report.inix` whose `daily_agenda` report uses exactly this pattern over its `events` table, grouping the sessions by `event_date`.

## File layout

```
DbDo.cs                    single-file C# source
DbDo_setup.iss             Inno Setup installer script
buildDbDo.cmd              compile DbDo.exe via csc.exe (+ jsc.exe for DbDo.dll)
DbDo.js                    JScript .NET source for the script support assembly
DbDo.inix                   shipped configuration (uiMode, [Keys] overrides)
sample.db                   small school sample (teachers/classes/students/enrollments)
northwind.db                Northwind sales sample, adapted
chinook.db                  Chinook music-store sample, adapted
SampleScripts/             three example .js scripts seeded into %APPDATA% on first run
README.md, README.htm       summary and quick start
DbDo.md, DbDo.htm         this reference
License.md, License.htm     MIT License
Announce.md, Announce.htm   release announcement
History.md, History.htm     chronological release notes
CamelType_CSharp.md         coding conventions used inside DbDo.cs
```
