# DbDuo Reference

**An accessible, keyboard-first database manager for Windows.** DbDuo opens SQLite, Microsoft Access, Excel, dBASE, and delimited-text files through one consistent set of PowerShell-flavored commands, in a GUI window and a dot-prompt console at the same time. JAWS, NVDA, and Narrator are all first-class through dedicated speech paths; every command is reachable by keyboard.

Source and releases: <https://github.com/JamalMazrui/DbDuo>.

## How DbDuo is organized

DbDuo runs in two interfaces — a WinForms GUI and a dot-prompt CLI — simultaneously by default. Both interfaces drive the same ADO database connection, so a change in one shows up immediately in the other.

The GUI is a single Windows form containing a menu bar, a data list in the center, and a status bar at the bottom. Standard Windows conventions apply: Alt activates the menu bar; single-letter mnemonics open each top-level menu (File, Edit, Navigate, Query, Misc, Help). Every menu command is reachable through both a hotkey and a click; no command is mouse-only.

The data list is a virtual-mode ListView in Details view with FullRowSelect on. Arrow keys move between rows. There is no per-cell focus visually; for cell-level reading, DbDuo provides two complementary mechanisms: Tab and Shift+Tab move an **announcement-only** column cursor (the screen reader speaks the new column name and value without changing the visible focus), and **Alt+Control+arrow** drives a virtual table cursor that also moves the visible row selection (see "Virtual cell navigation" below).

The status bar at the bottom carries three items in this order: the word "marked" (only when the current row has its marked flag set), the row position "row N of M", and "updated YYYY-MM-DD" (only when the current row has an updated column). Two spaces separate the sections so the screen reader pauses naturally between them. Use the JAWS Insert+PageDown command to read the whole status bar at once.

The window title is `DbDuo - <database> - <table>`, with `(read-only)` inserted after the database name when the lock is on.

The Shift+F10 context menu in the data list duplicates the most common record-level commands.

The CLI is a Windows console window running a dot prompt. Each line is a single Verb-Noun command, optionally with an argument; bare SQL is also accepted directly. The prompt is the current table's name followed by a dot. Use the Get-Help command, F1 from the GUI or `help` at the prompt, to see the command index; `help <command>` shows details for one command. Use the Out-File command, `Out-File path.txt` (output, tee, or `o` are aliases), to capture the next commands' output to a file while keeping it on screen at the same time.

### Switching between modes

Three grave-accent chords coordinate the GUI/CLI relationship. JAWS calls the unshifted key above Tab "GraveAccent."

Use the Enter-Console command, Control+GraveAccent, from the GUI menu to open or focus the dot prompt console. Use Alt+GraveAccent (a global hotkey) from inside the console to bring the GUI forward; this chord acts only when the console has focus, so it does not yank focus from Word or any other application. Use Alt+Control+GraveAccent (also global) from anywhere in Windows to toggle between GUI and console, whichever is not currently in front.

In CLI-only mode these chords work the same way; without a GUI, the "switch to GUI" chords simply have nothing to bring forward.

### Starting DbDuo

The installer creates a single shortcut, DbDuo, with hotkey Alt+Control+D (D for Desktop). Use the hotkey from anywhere in Windows to activate a running instance or launch a fresh one. DbDuo is single-instance: a second press of Alt+Control+D wakes the existing window rather than spawning a duplicate.

## Keyboard navigation in the data list

Three navigation modes operate independently in the data list: row navigation with arrow keys, column-announcement navigation with Tab, and cell-level virtual navigation with Alt+Control+arrow.

**Row navigation.** Use the arrow keys to step row by row. Use the PageUp and PageDown keys to jump by a screenful at a time. Use Home and End to jump to the first and last row. The listview's row selection moves on each press; the screen reader announces the focused row.

**Type-ahead jump.** Use a lowercase letter to jump to the next row whose value in the column under Tab announcement begins with that letter. Type two or more letters in quick succession to extend the search prefix. The search wraps around at the end of the list and starts over from the top. Capital letters are reserved for bare-Shift+Letter command shortcuts (F, G, J, R, S — see "Mnemonic hotkey groups" below), so lowercase navigation is the convention for type-ahead.

**Tab to hear cell values.** Use Tab and Shift+Tab to move an announcement-only column cursor across the current row. DbDuo announces "Column: value" after each Tab. The visible row selection does not change; this is purely a screen-reader convenience for hearing what's in each cell of the current row without changing where you are. Tab does NOT target commands; commands always prompt for a column (see Virtual cell navigation below for the command-targeting cursor).

## Virtual cell navigation

DbDuo overlays a screen-reader-style table cursor on the listview. The listview itself has no per-cell focus, so the virtual cursor lives in DbDuo's state and announcements; it tracks a `(row, column)` pair separate from the listview's row selection. Use it for table-style reading and for targeting column-aware commands.

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

The **double-press dialog** convention applies to every speech-only command. Press the command once to hear the text through your screen reader without moving keyboard focus. Press the same chord again within two seconds to open an information dialog with a read-only multi-line textbox containing the text plus an OK button — useful for reviewing long status, paths, or values that don't fit comfortably in a single speech announcement. The dialog's textbox supports normal text-navigation and Control+C copy. The two-second window is deliberately more forgiving than the JAWS and NVDA defaults (around 500 milliseconds) so a thinking pause between presses still counts as one gesture. Commands that participate: Say Status (Alt+Z), Say Path (Alt+P), Say Yield (Alt+Y), Say Tables (Shift+F4), Say Marked (Alt+Shift+M), Say Updated (Shift+D), Say Notes (Shift+N), Say Tags (Shift+T), Say Column from Cursor (Shift+L), Say Marked Yield (Shift+Y).

The virtual cursor synchronizes with the listview row selection in both directions: pressing plain Down/Up arrow updates the listview's row selection, and the virtual row follows along with the column unchanged. Pressing Alt+Control+arrow moves the virtual cursor first, then moves the listview's row selection to match — so you can see the row you're virtually browsing.

The virtual cursor's column and row are **remembered per table** within a session and across sessions. When you switch to a different table (and later return), DbDuo restores the row and column you were on. When you open a database file, every previously-visited table's filter, sort, position, and virtual column are restored — switching to any one of them via Choose Table, Control+Tab, or the dot prompt picks up the saved state, not row 1. The F5 Refresh command still resets the virtual cursor to (row 1, first column) by design.

**Column-aware commands default to the virtual column.** When a command needs a column — Sort Ascending, Sort Descending, Open Cell Value, Next Initial Change, Jump to Match in One Column — its picker dialog defaults to the column currently under virtual focus. Just press Enter in the picker to accept that column, or arrow up/down to pick a different one.

## Screen-reader settings

### JAWS settings for DbDuo

JAWS has its own table-navigation chord set on Alt+Control+arrow, and by default JAWS intercepts those chords before the focused application sees them. Without an adjustment, pressing Alt+Control+RightArrow inside DbDuo gives "Not in a table" instead of moving the virtual cursor. The same applies to several other DbDuo chords on Alt-letter and Shift-letter combinations.

The fix is a three-file JAWS settings bundle:

- `DbDuo.jkm` — JAWS key map. Maps the chords DbDuo wants to take over to a Script named `PassDbDuoKey`.
- `DbDuo.jss` — JAWS script source. Defines `PassDbDuoKey` as a one-line Script that calls the JAWS built-in `TypeCurrentScriptKey()`, which passes the current keystroke through to the application as if no script were running.
- `DbDuo.jsb` — compiled binary of the above, produced by JAWS's `scompile.exe`. JAWS loads this at run-time and resolves the `PassDbDuoKey` reference in the JKM.

**Automatic install.** The DbDuo installer offers to install this bundle automatically. The Finish-page checkbox "Install JAWS settings for DbDuo (recommended if you use JAWS)" is checked by default. Selecting it does three things for every JAWS year-version present on your system, in every language subfolder inside each version's `Settings` folder:

1. Copy `DbDuo.jkm` to the settings folder.
2. Copy `DbDuo.jss` to the settings folder.
3. Run `scompile.exe DbDuo.jss` from inside the settings folder to produce `DbDuo.jsb`.

Compiling locally against each installed JAWS's own `scompile.exe` guarantees the binary is compatible with that JAWS version (JSB compilation is version-sensitive). If a JAWS installation can't be located via the registry, the installer falls back to `C:\Program Files\Freedom Scientific\JAWS\<year>\scompile.exe`.

**Manual install** (or to recompile after editing the JSS):

```
copy DbDuo.jkm "%APPDATA%\Freedom Scientific\JAWS\<year>\Settings\enu\"
copy DbDuo.jss "%APPDATA%\Freedom Scientific\JAWS\<year>\Settings\enu\"
cd "%APPDATA%\Freedom Scientific\JAWS\<year>\Settings\enu"
"%PROGRAMFILES%\Freedom Scientific\JAWS\<year>\scompile.exe" DbDuo.jss
```

where `<year>` is your JAWS year-version. The settings work from JAWS's next launch (or, if JAWS is already running, when DbDuo next gains focus).

The chords passed through to DbDuo include: the virtual-cursor family (Alt+Control + arrows, Home, End, PageUp, PageDown, NumPad5); the parent-child drill family (Alt+RightArrow, Alt+LeftArrow, Alt+Home); the three search families (Control+F, Control+J, Control+F3 plus their Shift variants and F3 / Shift+F3); marked-row navigation (Control+Home, Control+End, Control+UpArrow, Control+DownArrow); bulk-mark spans (Shift+Home, Shift+End, plus Alt+Shift variants); and the Alt-letter command shortcuts (Alt+A, C, D, E, K, L, P, R, T, Y, Z plus relevant Alt+Shift variants).

If you customize the JKM in place and re-install DbDuo, your changes will be overwritten. To preserve customizations across updates, copy your modified version to a different filename and load both via JAWS's chain mechanism, or keep a copy outside the Settings folder and merge by hand after each DbDuo upgrade.

If you uninstall DbDuo, the installer removes only the three files it placed. Other JKMs or JSBs you placed yourself in those folders are not touched.

### NVDA add-on

DbDuo ships a `.nvda-addon` package that performs the same pass-through role for NVDA that the JKM and JSB do for JAWS. The add-on registers an app module that, when DbDuo.exe is the foreground process, hands every chord DbDuo cares about back to DbDuo instead of running NVDA's own table-navigation or browse-mode commands.

NVDA must be running for the add-on to install. The installer's Finish-page checkbox "Install NVDA add-on" hands the `DbDuo.nvda-addon` file to its Windows file association, which is registered to NVDA at NVDA install time. If NVDA is the active screen reader, NVDA's standard "Install this add-on?" dialog appears, and the user confirms or cancels. If JAWS or Narrator is the active screen reader (with NVDA installed but not running), the file association still launches NVDA, but the install dialog may not surface reliably; in that case, dismiss the installer's Finish page, switch to NVDA, then double-click `DbDuo.nvda-addon` in the install folder to install manually. The Help menu's "Re-install NVDA Add-on" command (which invokes `DbDuo.exe --install-nvda-addon`) does the same thing on demand.

After install, restart NVDA (NVDA menu > Restart) so the new app module is picked up. NVDA does not need to be restarted again for future updates of the same add-on.

If the add-on does not appear to take effect — Alt+Control+arrow still triggers NVDA's "Not in a table" speech — set NVDA's log level to "Debug" (NVDA menu > Preferences > Settings > General > Log level), restart NVDA, then open DbDuo and press the chord. Open NVDA's log (NVDA menu > Tools > View log) and search for lines beginning `DbDuo app module:`. Absence of those lines means NVDA never matched the app module to DbDuo.exe; presence of them confirms the module loaded and the bindings were registered.

**Narrator does not support scripts or add-ons.** Narrator users get less polished cell-level navigation than JAWS or NVDA users; the virtual-cursor announcements still fire, but Narrator may layer its own announcement on top.

## File menu

Use the New-Database command, Control+Shift+N (N for New, Shift to distinguish it from Control+N which makes a new row), to create an empty SQLite database at a chosen path. Use the Open-Database command, Control+O (O for Open), to bring up a file dialog and choose an existing database; DbDuo recognizes `.db`, `.sqlite`, `.sqlite3`, `.mdb`, `.accdb`, `.xlsx`, `.xls`, `.dbf`, `.csv`, `.tsv`, and `.txt` files.

Every file dialog remembers the folder you last used and opens there next time. New-Database, Open-Database, Save-DatabaseAs, and Backup-Database share one remembered "open" folder, since you typically keep your databases together. Import-Data and Export-Data remember their own folders separately. If no remembered value exists, the dialog falls back to the folder of the currently-open database, then to your Documents folder.

Use the Recent Files command, Alt+R (R for Recent), to open one of the last ten database files DbDuo has seen. The dialog shows each path with the table that was active when the file was last closed; selecting an entry reopens the file, restores that table, and restores the per-table filter, sort, and row position. If any of those pieces no longer apply (the table was dropped, a filter column was removed, the row count shrank below the saved position), DbDuo silently skips the incongruity and reopens with the best-fitting state it can.

Use the Save-DatabaseAs command, Control+S (S for Save), to write a copy of the open database to a new path and switch DbDuo to the new file. The dialog suggests `<original>-copy` as the filename so a stray Enter doesn't overwrite the source. Use the Backup-Database command, Control+Shift+S, to write the same copy but keep the original open; the suggested filename is `<original>-backup-yyyyMMdd`. The Close-Database command, Control+F4 (the MDI close convention), closes the open file without exiting DbDuo.

Use the Import-Data command, Control+Shift+I (I for Import), to read a GitHub-flavored Markdown table file and append its rows into the currently-open table. Header cells are matched case-insensitively to columns in the destination; cells with no matching column are dropped silently. Embedded `<br>` decodes back to newline, `\|` back to a literal pipe. Multi-table files (separated by blank lines) all import; per-row errors do not stop the import.

Use the Export-Data command, Control+Shift+X (X for eXport), to write the current filtered view to one or more files. Every input format DbDuo can open is also an export format: xlsx, docx, filtered HTML, Markdown table, csv, tsv, SQLite, Access, dBASE. The GUI prompts for one destination at a time; the dot prompt accepts a multi-format argument like `Export-Data xlsx docx md csv` (or the short forms `x d m c`). After each export, DbDuo opens the result in its default Windows application so you immediately hear what was produced.

The xlsx and docx formats use Word and Excel through late-bound COM and therefore need Microsoft Office; csv, tsv, md, plain HTML, SQLite, Access, and dBASE all work without Office. SQLite, Access, and dBASE exports open a separate ADODB connection to a fresh file, issue `CREATE TABLE` with portable text-typed columns, and INSERT row by row — the user's open recordset is not disturbed.

The File menu also hosts the table-switching commands: **Choose Table** (F4) opens a listbox of base tables, **Choose View** opens the equivalent for views, **Next Visited Table** / **Previous Visited Table** (Control+Tab / Control+Shift+Tab) cycle among recently-opened tables in MRU order, and **Next Object** / **Previous Object** (Control+F6 / Control+Shift+F6) cycle through every table and view without the MRU filter.

Use the Print command, Control+P (P for Print), to print the current view; this is reserved for a future release. For now, export to HTML or docx and print from the corresponding application.

Use the Exit DbDuo command, Alt+F4 (the Windows-standard close-program key), to close DbDuo entirely. The dot prompt's `quit` and `q` commands map to Exit-Application as well; `exit`, `x`, and `bye` map to Exit-Console, which leaves the dot prompt but keeps the GUI running.

## Edit menu

Every Edit-menu command operates on the current row, except where noted.

Use the New Record command, Control+N (N for New), to add a row. DbDuo shows an edit dialog with one line per distinct field; bookkeeping fields (`added`, `updated`, `marked`, the primary key, `look`, `unq`) get their default values automatically. Use the Edit Record command, F2 (the Windows-standard rename key), to edit the current row. Use the Delete Record command, Control+D (D for Delete), to remove the current row; the Delete key alone is a secondary binding so the Excel and Outlook convention works too.

Use the Duplicate Record command, Control+Shift+C (Shift turns the native clipboard's Control+C into row-copy), to clone the current row as a new record.

Use the Find and Replace Across Rows command, Control+R (R for Replace), to do a find-and-replace: pick a column, type the find string and the replace string, choose the scope (current row, filtered rows, or all rows), and DbDuo updates every match through ADO so the same triggers fire as for a SQL UPDATE.

Use the Mark Record command, Control+M (M for Mark), to set the boolean `marked` column on the current row; when marked is true, the status bar reads "marked." Use the Unmark Record command, Control+U (U for Unmark), to clear it. Marks are useful for accumulating an ad-hoc selection across navigation; combine with `filter marked` at the dot prompt to scope subsequent commands.

Use the Save Bookmark command, Control+K (K for booKmark), to remember the current row by primary key. Use the Go to Bookmark command, Alt+K (Alt is the inverse of Save), to return to it later; use the Clear Bookmark command, Control+Shift+K, to forget it. DbDuo holds one named bookmark per session.

Use the Open Cell Value command, Control+Enter (extends Enter into "open as URL"), to treat a cell's value as a URL or a file path and open it in its default Windows application. DbDuo prompts for the column (defaulting to the column under virtual focus, so just press Enter to accept); useful when a database column holds links to PDFs, screenshots, or web pages.

## Navigate menu

The Navigate menu contains three families: record stepping, search, and parent-child drill.

**Record stepping.** Use the First Record / Last Record / Next Record / Previous Record commands to step through the current view. None have hotkeys by default because the listview's arrow keys and Control+Home/End handle the same movements natively. Use the Go to Row command, Shift+G (G for Go-to), to jump to a row number; out-of-range values clamp to the first or last row.

**Search.** Three independent search families plus a unified "repeat last search" pair. Each family has its own forward and reverse chord, and each family's last-used term is remembered separately so reopening a family's dialog brings up its own prior text.

Use the Find Across All Columns command, Control+F (F for Find), to find a row whose value in ANY visible column contains a substring. The dialog has three controls: a Text input (defaulting to the last Find substring), a Recent listbox of up to the last 10 Find terms, and a Case-sensitive checkbox (off by default). Selecting a Recent entry copies its text into the Text input AND sets the Case-sensitive checkbox to how that term was last used (entries that were case-sensitive show an `[Aa]` suffix in the display). Use Control+Shift+F to search backward.

Use the Jump to Match in One Column command, Control+J (J for Jump), to find a row whose value in ONE column you pick contains a substring. The dialog adds a Column listbox to the standard Text + Recent + Case-sensitive layout; the column defaults to the column under virtual focus, falling back to the last Jump column or to the first column. Use Control+Shift+J for the reverse.

Use the Find Regex Across All Columns command, Control+F3 (F3 = Windows-standard search; Control turns it into the regex variant), to find a row matching a .NET regular expression across any visible column. Same Text + Recent + Case-sensitive dialog as Find; the regex is validated before the search begins. Use Control+Shift+F3 for the reverse.

Use the Search Next command, F3, to repeat whichever family was last invoked moving forward. Use Search Previous, Shift+F3, to repeat backward. DbDuo tracks the last-used family separately from the per-family last-term state, so a Jump followed by a Find followed by F3 repeats the Find, not the Jump.

**Parent-child drill.** Two-way movement between related tables using foreign-key relationships.

Use the Enter Child Table command, Alt+RightArrow (right-arrow = into the child), to drill from the current parent row into a child table whose schema includes the parent's primary-key column. If exactly one child table matches, DbDuo opens it directly; otherwise it presents an alphabetized listbox so you can pick. The child table opens with a filter applied that shows only the rows whose foreign key matches this parent's primary key.

Use the Exit Child Table command, Alt+LeftArrow (left-arrow = out of the child), to pop back one level. DbDuo restores the parent's sort, filter, and exact row position. The drill stack is unbounded; you can Enter Child Table several levels deep, then Exit Child Table back up the same way.

Use the Exit to Root Table command, Alt+Home, to pop the entire drill stack and return to the topmost ancestor in one keypress.

## Query menu

The Query menu contains read-only commands that report on the data without modifying it. Most fall into three families: examine, speech-only, and shape (filter / sort).

**Show commands.** Use the Show Record command, plain Enter, to open a read-only dialog showing every visible field of the current row plus its related parent and child rows (via foreign-key relationships). Use the Table Properties command, Alt+Enter (the Windows-standard properties chord), to see metadata about the current table — row count, column count, primary key, inferred foreign keys, cached settings. Use the Related Records command (no hotkey by default; Alt+Shift+R as an alias) to navigate from the current row's foreign-key column to the corresponding parent row in the related table. Use the Show Schema command to print every CREATE TABLE and CREATE VIEW in the database; this is long output, usually called from the dot prompt as `schema`.

**Speech-only commands.** The Say-X family announces state without changing focus or position. All eight commands respect the **double-press-spells** convention: press the same speech chord twice within 1.5 seconds to hear the text spelled character by character.

- Say Status (Alt+Z) — table name, row position, filter, sort
- Say Path (Alt+P) — the open database file's full path
- Say Yield (Alt+Y) — row count and active filter
- Say Tables (Shift+F4) — tables visited in this session
- Say Marked (Alt+Shift+M) — the `look` values of every marked row
- Say Updated (Shift+D) — the `updated` value of the current row, in a human-friendly local-time form (`December 14, 1963 at 5:42 AM`); the underlying SQLite text is unchanged
- Say Notes (Shift+N) — the `notes` field of the current row
- Say Tags (Shift+T) — the `tags` field of the current row
- Say Column from Cursor (Shift+L) — values of the current virtual column starting at the current row, semicolon-separated
- Say Marked Yield (Shift+Y) — count of marked rows

**Shape commands.** Use the Filter Records command, Shift+F (F for Filter), to apply an ADO Filter expression like `name LIKE '%bridge%'`. Use the Clear Filter command, Shift+R (R for Reset), to clear it. Use the Custom Sort command, Shift+S (S for Sort), to type an arbitrary ADO Sort expression like `name ASC, year DESC`.

Use the Sort Ascending by Column command, Alt+A (A for Ascending), to sort by a chosen column alphabetically; use Alt+Shift+A for Sort Descending. Each prompts for the column with a listbox that defaults to the column under virtual focus — just press Enter to accept.

Use the Sort by Date Updated (oldest first) command, Alt+D (D for Date), to sort by the table's `updated` column with the oldest at the top; use Alt+Shift+D for the most-recent-first variant. Use the Clear Sort command to drop the sort so the recordset returns to its natural order.

## Misc menu

Use the Refresh View command, F5 (the browser-standard refresh key), to re-query the database from disk; useful when another tool has written to the file while DbDuo had it open. F5 also resets the virtual cursor to (row 1, first column).

Use the Toggle Read-Only Lock command, Control+F7 (F7 = lock convention), to switch the recordset between editable and read-only; the window title shows the change.

Use the Table Statistics command (no hotkey) to print row counts and per-column statistics for the current table. Use the Frequency Chart command (no hotkey; Alt+C as an alias) to render a frequency-by-column chart in Excel for analysis.

Use the Describe Column command, Control+Shift+D (D for Describe), to compute descriptive statistics for the column under the virtual cursor. DbDuo walks the column, detects whether the values look numeric, date-like, boolean-like, or text, and reports the statistics that fit. Numeric columns get count, unique, minimum, maximum, range, mean, median, sample standard deviation, Q1, Q3, IQR, mode (if unambiguous), and a skew indicator. Date columns get earliest, latest, median, and span. Boolean-like columns (`0/1`, `Y/N`, `true/false`) get true and false counts with percentages. Text columns get unique count, shortest/longest/mean length, and a top-10 frequency table. The report opens in the same multi-line read-only dialog used by speech commands on double-press; press Control+C inside the textbox to copy the whole report.

Use the Plot Column command, Control+Shift+P (P for Plot), to produce an Excel chart matched to the data type of the column under the virtual cursor. DbDuo runs the same data-type detection Describe Column uses and chooses a chart shape from it. Numeric columns can plot as a histogram (Sturges-binned distribution) or as a box-and-whisker plot (Tukey's five-number summary as a single compact shape); a small dialog prompts you to pick one. Date columns can plot as a timeline (count by month or by day for short spans, line chart), as counts-per-calendar-year (column chart), or as counts-by-month-of-year (column chart for seasonal patterns). Boolean columns auto-pick a pie chart of true / false proportions. Text columns auto-pick a horizontal Pareto bar of the top 15 most frequent values. When there is only one sensible chart shape for the data type, DbDuo skips the picker and generates it directly. The .xlsx file is written next to the database file with a name like `customers-region-pareto.xlsx`, then opened in Excel; from there you can polish the chart, copy it to other documents, or change the chart type by hand. Plot Column requires Excel to be installed (same dependency as Frequency Chart). The existing Frequency &Chart command (Alt+C) remains for when you want to pick any column from a list and produce a column chart of value counts, regardless of data type.

Use the Choose Visible Columns command (no hotkey; Alt+L as an alias) to pick which columns appear in the data list for the current table. Hidden columns are still accessible through Show Record and Edit Record.

Use the Extract Regex Matches to Clipboard command, Alt+E (E for Extract), to walk every visible row, run a .NET regex against every visible column, and copy every match to the clipboard one per line. Useful for pulling email addresses, URLs, or IDs out of free-text columns.

Use the Copy Cell to Clipboard command, Shift+C, to copy the current virtual cell (the cell under the Alt+Control+arrow cursor) to the Windows clipboard, replacing whatever was there. Use the Append Cell to Clipboard command, Shift+A, to append the current virtual cell to the clipboard separated by a blank line (two CRLF), so you can accumulate values from multiple cells across rows or columns. If the clipboard is empty, Append acts the same as Copy.

Use the Copy Row as TSV to Clipboard command (no hotkey, accessed through the Misc menu) to copy the current row's visible columns as tab-separated values for pasting into Excel, Word tables, or chat clients. The label lacks a mnemonic letter because the only candidates fall mid-word — DbDuo prefers no mnemonic to a mid-word one.

Use the Next Initial Change command, Shift+I, to jump to the next row whose value in a chosen column starts with a different first letter. The column picker defaults to the column under virtual focus.

Use the Run SQL command, Control+Q (Q for Query), to run any SQL statement. SELECTs display the result as a new recordset; INSERT/UPDATE/DELETE/DDL run via ADO Connection.Execute. The dot prompt's `;` and `*` aliases map to the same command.

Use the Test Integrity command to run an integrity probe on the open database (`PRAGMA integrity_check` for SQLite, equivalents for other providers). Use the Test Drivers command to print which ODBC and OLE DB providers Windows currently has registered, useful when troubleshooting a failed Open Database.

Use the Open in Explorer command, Alt+Backslash (the backslash key evokes Windows paths), to open Explorer at the database file's folder with the file pre-selected.

Use the Open Dot Prompt command, Control+GraveAccent, to open or focus the dot prompt console from the GUI.

Use the Invoke Script command, Alt+V (V for inVoke), to pick and run one of your saved scripts. Use the Edit Script command, Alt+Shift+V, to edit an existing script or create a new one. Use the Open Script Folder command (no hotkey) to launch Explorer at the script folder. See the Scripting section below for the full reference.

Use the Configuration Settings command, Alt+Shift+C (matching the EdSharp and FileDir convention; F12 also works as a legacy alias), to open the per-user DbDuo.ini settings dialog. The dialog exposes a curated subset of the settings the program reads — the UI mode, the Command Echo toggle, and a "Field Validation..." button that opens a sub-dialog for per-field regex patterns on the current table. The dialog also has an "Open file..." button that launches the raw DbDuo.ini in your default text editor for advanced settings (`[Keys]` hotkey overrides, connection-string overrides, and operational housekeeping keys that DbDuo writes itself).

Use the Start Mark Anchor command, F8, to record the current row as the start of a mark range. Then move to another row (above or below — direction does not matter) and press Complete Mark to Anchor, Shift+F8, to set the marked flag on every row in the range, inclusive. Use the parallel Start Unmark Anchor command, Alt+F8, and Complete Unmark to Anchor, Alt+Shift+F8, to do the same for clearing marks. The two anchors are independent, so you can stage a mark range and an unmark range without one gesture clobbering the other. Both anchors are direction-agnostic and transient: they reset when you close the database, and each "Complete" command refuses with a clear message if its anchor was set on a different table than the currently-open one. The pattern parallels EdSharp's Start/Complete Selection family (F8 / Shift+F8) and FileDir's Start Tag or Untag / Complete Tag / Complete Untag, with DbDuo's "Mark" terminology and the additional independent unmark-anchor for symmetry.

Use the Say Position command, Alt+Delete, to hear the current cell's column header and row number — for example, "Column: name, Row: 30". Speech-only; does not move focus. This is the JAWS convention for "say cursor position", reframed in DbDuo terms to mean the virtual cell (the cell under the Alt+Control+arrow cursor). Single-press speaks; double-press shows the same text in the multi-line dialog used by the other speech-only commands.

Use the Say Sort and Filter command, Shift+8 (the asterisk key; Numpad-asterisk also works), to hear the active sort order and filter criteria for the current table. Either or both may be empty; the speech explicitly says "(none)" rather than going silent, so you always get confirmation. Single-press speaks; double-press shows the same text in the multi-line dialog.

Use the Say Kin command, Shift+K (K for Kin — relatives by foreign key), to hear the `look` field of every related record. The announcement covers both directions: every parent reached by an outbound foreign-key column on the current row, and every child that points back via an inbound foreign key. Parent entries are listed first as "Parents: <table>: <look>; <table>: <look>"; children follow as "Children: <table> (N): <look>, <look>, ...". Single-press speaks via the screen reader; double-press shows the same content in the multi-line dialog, useful when the parent row has many children. The Say Kin command is read-only and does not move the cursor or change the open table; for an interactive jump to a specific related record, use the existing Show-Related command (Alt+Shift+R) instead.

Field validation: each editable field can have a regex pattern stored in DbDuo.ini under a `[Validation:<table>]` section. When the pattern is set, both the Edit Record dialog (F2) and the Edit Field dialog (Shift+F2) refuse non-matching input. DbDuo uses .NET regex syntax (the established powerful pattern language, the same one Find Regex uses) — examples appear in the sub-dialog. Empty pattern means no constraint.

## SQL reference: what Invoke-Sql actually runs

A common question worth answering precisely: when you press Control+Q for Run SQL, what SQL dialect does the database engine understand? The answer depends on which kind of file you have open, because DbDuo uses three different drivers under the ADO ConnectionString and each one parses SQL differently.

### A portable SQL baseline that works everywhere

Most users do not need a SQL deep-dive. The basics — selecting rows, inserting rows, updating values, deleting rows — work across every database format DbDuo opens (SQLite, Access, Excel-as-queryable, dBASE, CSV/TSV-as-readable) when you stay inside a careful ANSI SQL-92 subset.

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

When the open file is .db / .sqlite / .sqlite3, DbDuo connects through the ch-werner SQLite ODBC driver. There is no SQL translation layer: the full SQLite SQL surface is available, including:

- Window functions with the OVER clause (since 3.25): `row_number()`, `rank()`, `dense_rank()`, etc.
- Common Table Expressions (WITH), including recursive CTEs.
- UPSERT (`INSERT ... ON CONFLICT ... DO UPDATE`).
- JSON1 functions: `json_extract`, `json_each`, `json_array`, `json_object`, etc.
- RETURNING clause on INSERT/UPDATE/DELETE (since 3.35).
- FTS5 full-text search if the database was built with FTS5 tables.
- R-Tree, generated columns, partial indexes, expression indexes.

PRAGMA statements work both as data-returning queries (when shaped like `PRAGMA table_info(foo)`, which returns a result set DbDuo will render as a grid) and as setters (when shaped like `PRAGMA journal_mode = WAL`, which DbDuo runs through Execute).

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

When the open file is .mdb or .accdb, DbDuo connects through the Microsoft ACE OLEDB provider. This driver runs **Access SQL**, not standard SQL. The differences from SQLite or ANSI SQL are substantial.

Access SQL uses `IIF(condition, then, else)` instead of `CASE WHEN ... THEN ... ELSE ... END`. String concatenation is `&` not `||`. Wildcards in LIKE are `*` and `?`, not `%` and `_`. Date literals are delimited with hash marks: `#2025-05-12#`. Booleans are TRUE and FALSE.

Access SQL supports the basics but lacks CTEs (no WITH clause), window functions (no OVER clause), and RETURNING. It has a different set of built-in functions: `Format()`, `DateSerial()`, `DateDiff()`, `DatePart()`, `Nz()` (null-coalesce), and many more drawn from VBA.

```sql
-- IIF instead of CASE
SELECT student_id, name, IIF(grade >= 90, 'A', IIF(grade >= 80, 'B', 'C')) AS letter
FROM students;

-- LIKE with Access wildcards
SELECT * FROM students WHERE name LIKE 'Sm*';

-- Date literal with hash delimiters
SELECT * FROM enrollments WHERE updated >= #2024-01-01#;

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

When the open file is .csv or .tsv, DbDuo connects through the Jet text driver. The driver is read-mostly: SELECT works well, INSERT works in a limited fashion, and structural commands like CREATE TABLE or ALTER TABLE don't apply.

The most useful pattern is to read a CSV with Invoke-Sql for analysis, and use Import-Data to copy into SQLite for any non-trivial work.

### Practical recommendation

For non-trivial analytical SQL — anything involving CTEs, window functions, recursive queries, JSON extraction, or modern SQL features — open the data in SQLite (either by saving a SQLite file directly or by using Import-Data from another format) and use Invoke-Sql against that. SQLite gives you the most expressive SQL of the four backends, the fastest engine, and the smallest set of dialect surprises.

## Scripting

DbDuo's scripting feature lets you save and re-run units of work — computations, queries, command workflows — without having to remember or retype them. The model is one folder, one chord (Alt+V), and three file extensions that pick the right execution engine for the kind of work you want to do. The pattern is the same one EdSharp and FileDir pioneered for their snippet systems: a folder of plain text files; you write them in your own editor; you invoke them from a standard listbox dialog. DbDuo extends the pattern by recognizing three distinct file types and dispatching to the engine that matches each.

### Where scripts live

Scripts live under `%APPDATA%\DbDuo\Scripts\`. The folder is created on first access; it lives under your roaming application data so it survives DbDuo upgrades and uninstalls. On first launch DbDuo seeds the folder with the bundled sample scripts (described at the end of this section). A `.seeded` sentinel file in the folder records that seeding has run, so deleting a sample does not cause it to reappear later. To re-seed (to recover a deleted sample or pick up a new one bundled with a future release), delete the `.seeded` file and any matching scripts, then invoke any script command.

### The three file types

The extension on a script file determines which engine runs it. The picker shows the extension on every row, so you always know what kind of script you're picking before you press Enter.

**`.js`** — JScript .NET. A general-purpose computational language that runs inside the DbDuo process with full access to the running form and recordset. The script has two pre-injected host variables, `db` (a `DbDuoManager` — the open database) and `frm` (a `DbDuoForm` — the GUI window). The DbDuo.dll support module pre-imports `System`, `System.Collections`, `System.Data`, `System.IO`, `System.Reflection`, `System.Text`, `System.Text.RegularExpressions`, and `System.Windows.Forms`, so a script can use any type in those namespaces directly without its own `import` statements. The value of the last expression in the script is what DbDuo gets back; if it's a string, that string is displayed in the result dialog. Use `.js` when you want to do computation: iterate over rows, transform values, build a report string, talk to the COM bridge, format output. This is the most powerful and the most code-heavy of the three.

**`.sql`** — SQL batch. A list of SQL statements separated by `;`. Each statement is run in order through the same `invokeSql` pipeline that powers the Invoke-Sql command. SELECT results are rendered as text tables; UPDATE / INSERT / DELETE return their row counts; PRAGMA output is shown verbatim. Statements that error abort the batch with an error message identifying the failing statement number. Line comments starting with `--` and block comments `/* ... */` are stripped before parsing. The first line is conventionally a `-- Description: <one-line summary>` comment so the picker has context. Use `.sql` when you want to ask the database something or change data: saved queries, repeatable reports, scheduled cleanups.

**`.duo`** — DbDuo command batch. A list of dot-prompt commands, one per line, dispatched as if you had typed each line at the dot prompt. The execution surface is the entire DbDuo command set, not just SQL — so a `.duo` script can open a database, switch tables, set filters and sorts, mark and unmark rows, speak status to the screen reader, export results, and so on. Use the **natural dot-prompt language** in `.duo` scripts — the short, single-word aliases like `path`, `status`, `tables-list`, `sort-filter`, `table <name>`, `filter <expr>`, `sort <expr>`, `find <text>`, `jump <expr>`, `next`, `previous`, `mark`, `unmark`, `add`, `edit`, `delete`, `schema`, `kin` — rather than the underlying canonical PowerShell-style verbs (`say-path`, `say-status`, `say-tables`, `select-table`, `select-record`, `sort-object`, etc.). Either form works because the dispatcher resolves aliases, but the natural form is shorter, more readable, and matches what you type interactively. Lines starting with `#` or `--` are comments. Blank lines are skipped. A leading `?` on a line means "continue on error" — useful for commands whose failure is harmless (e.g. clearing a filter when no filter is active). By default an error on any other line aborts the batch and the rest of the file is not run. Each line's output is captured and accumulated in the result dialog. Use `.duo` when you want to automate a workflow you would otherwise execute as a sequence of menu commands or dot-prompt entries.

The mental model is: `.js` is "compute"; `.sql` is "query"; `.duo` is "do." Each surface serves a different question.

The `.duo` extension is reserved for DbDuo's command-batch format. It does not collide with any major file type. Two alternatives were considered: `.dbd` (collides with ER/Studio Repository files and some InterBase backup formats) and `.dot` (collides with Graphviz graphs and Microsoft Word templates). `.duo` was chosen because the namespace is clean and the extension is recognizable as DbDuo's own.

### The chord — Alt+V

Use the Invoke Script command, Alt+V, to pick and run a script. A standard `LbcDialog` listbox shows every `.js`, `.sql`, and `.duo` file in the folder, sorted alphabetically, each row showing the full filename including extension. Type into the filter box to narrow the list; press Enter or click OK to run the highlighted script. The result (script output for `.js`, query results for `.sql`, accumulated command output for `.duo`, or an `ERROR: ...` message on failure) appears in a read-only multi-line memo dialog that screen readers can navigate line by line, word by word, or character by character.

The chord is named "Invoke Script" rather than "Invoke File" or "Invoke Query" because "script" generalizes naturally to all three engines — a JScript script, a SQL script, and a DbDuo command script are all *scripts* in the recipe-for-doing-something sense, even when the engines differ.

### Editing scripts

Use the Edit Script command, Alt+Shift+V, to edit an existing script or create a new one. If the folder has existing scripts, the picker appears with a `[New script...]` entry at the top; picking that opens a Save File dialog whose Filter list offers JScript .NET, SQL batch, DbDuo command batch, or any-extension. When you save a new script, DbDuo seeds it with a starter template appropriate to the extension: a header comment block plus one example construct that runs successfully out of the box. After the file is created, your default editor opens on it.

The default editor is Notepad. To override, put a line in `DbDuo.ini`:

```ini
[Scripts]
editor = C:\Program Files\Notepad++\notepad++.exe
```

### Opening the Scripts folder

Use the Open Script Folder command (no hotkey by default) to launch Explorer at the script folder. Useful when you want to copy a script in from elsewhere, rename a batch of files, or check on the `.seeded` sentinel.

### Bundled sample scripts

DbDuo ships with two examples of each script type. They demonstrate the typical shape and idioms of each engine and serve as starting points for your own work.

**`.js` samples (computation):**

- **`CopyRowToClipboard.js`** — acts on the current row. Iterates the visible fields, builds a `name = value` listing, and puts it on the Windows clipboard. Demonstrates: row access via `db.getFieldValue`, the `StringBuilder` idiom, calling `frm.invokeMessage` for screen-reader confirmation.

- **`MarkRowsMatchingRegex.js`** — acts on the whole filtered view. Walks every row, tests every visible field against a regex, and marks each row whose values match. Demonstrates: iteration over the recordset, regex matching, conditional field updates via `db.setFieldValue` + `db.update`, the "soft search" idiom of marking matches you can later scope commands to.

**`.sql` samples (queries):**

- **`SchemaOverview.sql`** — three SELECTs against `sqlite_master` printing every table, view, and named index in the open database. Demonstrates: catalog introspection, the `UNION ALL` style for grouping related results, leading `--` comments for description.

- **`NorthwindRowCounts.sql`** — one SELECT per Northwind table, combined with `UNION ALL` and ordered by row count. Demonstrates: per-table count, multiple counts in one result, the leading description-comment convention.

**`.duo` samples (workflows):**

- **`RecentOrders.duo`** — switches to the orders table, filters to the most recent calendar year, sorts most-recent-first, and reports the result count via speech. Uses native dot-prompt aliases (`table`, `filter`, `sort`, `sort-filter`) instead of canonical PowerShell-style verbs. Demonstrates: multi-step workflow, the `?` continue-on-error prefix on the clear-filter line, speech-only verbs at the end as a screen-reader confirmation.

- **`StatusSnapshot.duo`** — runs five speech-only commands in a row (`path`, `tables-list`, `status`, `sort-filter`, `say-yield`) to print a complete "where am I" snapshot. Uses the natural dot-prompt aliases throughout — only `say-yield` keeps its canonical form because no shorter alias exists for the one-line row-count summary. Demonstrates: pure-read workflow, the speech-only command family, scriptable as a warm-up routine when sitting down to a workspace.

### Errors

Compile-time errors and runtime errors in `.js` scripts are caught and returned as a string starting with `ERROR:`. The result dialog shows it with the error icon. The script never throws out to DbDuo, so the UI stays responsive.

For `.sql` batches, an error on any statement aborts the batch; the result dialog shows the output of every statement that ran before the error plus the error message identifying which statement failed.

For `.duo` batches, by default an error on any line aborts the batch; the result dialog shows the output of every line that ran before the error plus the error message identifying the failing line. To let a specific line fail without aborting, prefix it with `?` ("continue on error"). This is useful for commands like `reset-filter` whose failure is harmless (no filter was active) but should not stop the rest of the script.



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

Scripts run in the DbDuo process with all the privileges DbDuo has. There is no facade or sandbox. A script can call `Environment.Exit`, read or write files, launch other programs, modify the database. This is intentional for power-user automation; treat scripts the same way you would treat shell scripts or PowerShell scripts you run on your own machine.

## Help menu

Use the Documentation command, F1 (the standard help key), for help. With no argument, F1 shows the command index; from the dot prompt, `help <topic>` shows details for one command. The menu label matches EdSharp and FileDir: "Documentation".

Use the PowerShell Verb Reference command (no hotkey) to see the PowerShell verb taxonomy with each verb's category and a brief description. Use the Alternate Menu command, Alt+F10, to open an alphabetized list of every command with its current hotkey and a one-line summary. EdSharp and FileDir use the same command name; the chord and behavior are consistent across the three apps. The picker includes a detail panel below the list that shows the summary plus an optional longer description for the selected command — pick a command in the list and read what it does before pressing Enter to run it.

Every command in DbDuo carries one-line summary metadata and an optional multi-line description, modeled on the EdSharp and FileDir convention. The summary appears in three places: in the menu status bar when the item has focus (so screen readers announce it as you arrow through the menus), inline after the chord in the Alternate Menu, and as the "Summary:" line in the Key Describer trace. The optional description, when present, gives the longer "why" or "gotchas" of the command and shows up in the Alternate Menu detail panel and at the bottom of the Key Describer trace. Commands without an explicit summary fall back to their menu label (minus the ampersand mnemonic markers) so every command is at least minimally self-describing.

Use the Where Am I command (no hotkey by default) to hear the current row, table, filter, and sort state in detail. Use the Test Screen Reader Speech command to probe DbDuo's three speech paths (JAWS direct via COM, NVDA direct via the controller-client DLL, and the UIA live-region fallback for Narrator) and confirm which one is working with your screen reader.

Use the Key Describer command, Control+F1 (F1 for help, Control for "describe rather than fire"), to switch into a mode where every hotkey press announces the chord and its bound command instead of running it; press Control+F1 again to leave the mode. Use the Show Log Location command to print the path of `DbDuo.log`, the per-session log file. Use the History of Changes command, Shift+F1, to read the chronological list of releases and what changed in each. Use the Readme Guide command to open `README.md` in your browser. Use the Open Website command to open the DbDuo GitHub page. Use the Elevate Version command, F11, to ask GitHub for the latest release and offer to download and install it. Use the About command, Alt+F1, to read the version number and a brief credits block. The Help-menu commands carrying chords use the same name and chord as their EdSharp and FileDir counterparts (Documentation, History of Changes, Key Describer, Elevate Version, About, Alternate Menu).

The Help menu also hosts one-keystroke commands to open each of the three bundled sample databases: **Open Sample Database** (sample.db, the small school domain), **Open Northwind Sample** (the classic Microsoft sales sample), and **Open Chinook Sample** (the classic music-store sample). All three open via the same code path File > Open Database uses, so the usual post-open behaviors apply. See "Bundled sample databases" below for what's in each.

## Commands available only at the dot prompt

A few commands have no GUI counterpart because they manage the dot prompt itself.

Use the Exit-Console command, `exit` (or `x` or `bye`), to leave the dot prompt while the GUI keeps running. Use the Switch-Focus command, `gui` (or `focus` or `window`), to bring the GUI forward from the console.

Use the Out-File command, `Out-File path.txt` (with aliases `output`, `tee`, and `o`), to send subsequent output to a file while also keeping it on screen. The `-a` flag appends rather than overwriting; `Out-File stdout` restores the screen-only behavior; bare `Out-File` reports the current target.

Use the Invoke-Script command, `Invoke-Script path.txt` (with aliases `read`, `script`, and `i`), to run a file of dot-prompt commands. Blank lines and lines beginning with `#` or `;` are treated as comments. Errors are reported per line but do not stop the script.

```
Out-File monthly_report.txt
Invoke-Script monthly_report.dbduo
Out-File stdout
```

## Mnemonic hotkey groups

This section restates every hotkey in the program, grouped by the part of the keyboard it lives on.

### Bare Shift+Letter family

Five one-key shortcuts fire from the data list only (so capital letters typed in dialogs are not affected): Shift+F filters (Filter Records); Shift+G goes to a row (Go to Row); Shift+J as a synonym for Jump to Match in One Column; Shift+R resets the filter (Clear Filter); Shift+S sorts by an arbitrary expression (Custom Sort).

### Function-key family

F1 is Documentation; Shift+F1 is History of Changes; Alt+F1 is About; Control+F1 is Key Describer. F2 edits the current row; Shift+F2 edits just the current field (virtual cell). F3 is search next; Shift+F3 is search previous. Control+F3 is Find Regex Across All Columns; Control+Shift+F3 is the reverse. F4 picks a table; Shift+F4 is Say Tables; Control+F4 closes the open file. F5 refreshes and resets the virtual cursor. Control+F6 cycles all objects; Control+Shift+F6 cycles backward. Control+F7 toggles the lock. F11 is Elevate Version. Alt+F4 closes the program. Alt+F10 opens the Alternate Menu. F12 is a legacy alias for Configuration Options (now on Alt+Shift+C, the same chord EdSharp and FileDir use for their Configuration Options command).

### Control-letter family

Control+C is reserved for native clipboard. Control+D deletes the current row. Control+F is Find Across All Columns; Control+Shift+F is the reverse. Control+J is Jump to Match in One Column; Control+Shift+J is the reverse. Control+K saves a bookmark; Control+Shift+K clears it. Control+M marks the current row; Control+U unmarks it. Control+N adds a row; Control+Shift+N creates a new database. Control+O opens a file. Control+P prints. Control+Q runs SQL. Control+R is Find and Replace Across Rows. Control+S saves the database to a new path; Control+Shift+S takes a backup snapshot. Control+Shift+C duplicates the current row. Control+Shift+I imports a Markdown table; Control+Shift+X exports data. Control+Enter is Open Cell Value.

### Alt-letter family

Alt+A sorts ascending; Alt+Shift+A sorts descending. Alt+C is an alias for Frequency Chart. Alt+D sorts by date oldest first; Alt+Shift+D sorts most recent first. Alt+E is Extract Regex Matches to Clipboard. Alt+K is Go to Bookmark. Alt+L is an alias for Choose Table. Alt+P is Say Path. Alt+R is Recent Files; Alt+Shift+R is Related Records. Alt+T is an alias for Table Statistics. Alt+Y is Say Yield. Alt+Z is Say Status. Alt+Enter is Table Properties. Alt+Backslash is Open in Explorer.

### Alt+Control extended-key family (virtual cell navigation)

Alt+Control+Home moves to the top-left cell; Alt+Control+End moves to the bottom-right. Alt+Control+RightArrow / LeftArrow / DownArrow / UpArrow move one cell in the named direction. Alt+Control+PageDown moves to the last row of the current column; Alt+Control+PageUp moves to the first row. Alt+Control+Numpad5 announces the current cell, or spells it on a second press.

### Alt+arrow family (parent-child drill)

Alt+RightArrow drills into a child table. Alt+LeftArrow returns to the parent row. Alt+Home pops the entire drill stack.

### Navigation family

Tab and Shift+Tab move an announcement-only column cursor across the current row. The arrow keys move the listview's row selection. Enter opens Show Record on the current row. Control+Tab cycles among recently-visited tables; Control+Shift+Tab cycles backward. Control+Home and Control+End jump to the first or last marked row; Control+UpArrow and Control+DownArrow step among marked rows. Shift+Home and Shift+End bulk-mark every row from the first through the current, or the current through the last; Alt+Shift+Home and Alt+Shift+End unmark the same spans.

### GraveAccent family

Control+GraveAccent is the GUI menu hotkey for Open Dot Prompt. Alt+GraveAccent is a global hotkey: when the console has focus, it brings the GUI forward. Alt+Control+GraveAccent is a global hotkey that always acts: it toggles between GUI and console, whichever is not currently in front.

## Standard fields

DbDuo follows a convention for table design that the bundled sample databases illustrate and that the user manual recommends for new databases. Each table has the following "standard fields" in this order, with the "distinct fields" (the substantive columns) interleaved among them:

1. `<table>_id` — the primary key, integer, autoincrement (e.g., `teacher_id` in the `teachers` table).
2. `added` — datetime, default `current_timestamp`. When the row was created.
3. `updated` — datetime, default `current_timestamp`. Most recent change.
4. `url` — textline. A hyperlink, file path, or other openable reference associated with this row (added v1.0.66). Opened by the Open Url command (Ctrl+Shift+U) or read aloud by Say Url (Shift+U).
5. `tags` — textmemo. Comma-separated tag list for ad-hoc grouping. Read aloud by Say Tags (Shift+T).
6. `notes` — textmemo. Free-form annotations. Read aloud by Say Notes (Shift+N).
7. Foreign-key columns (`<parent>_id`, for child tables only).
8. **Distinct fields** — the substantive columns this table is actually for.
9. `marked` — boolean, default 0. The flag the Set-Mark command toggles.
10. `look` — computed text. A pipe-joined rendering of the most identifying distinct fields, designed for screen-reader readability. Appears in listboxes, quick-search displays, and Show Record's Related Records section.
11. `unq` — computed text. Like `look` but optimized for uniqueness rather than readability. The intent is that the combination of column values in `unq` can confidently be considered unique for a row, so that an upsert-style command can update an existing row if `unq` matches, or insert a new row otherwise.

The TEXTLINE / TEXTMEMO type names on `url`, `tags`, and `notes` aren't standard SQLite types — they're convention labels DbDuo reads from `PRAGMA table_info`. SQLite's type affinity treats all three as TEXT for storage. The difference is that DbDuo's Edit Record dialog renders a single-line input box for TEXT/TEXTLINE columns and a multi-line memo widget for any type containing "memo". Use TEXTMEMO for `tags` and `notes` so the editor handles their potentially-long content gracefully.

The `look` column is what makes Show Record's Related Records section informative. When DbDuo lists "Related students:" or "Related classes:", each line is one matching row's `look` value, so a single short string identifies who or what each related record is. Tables without a `look` column still show up under the right header but with a `(N row(s) -- no look column)` placeholder.

DbDuo hides every column ending in `_id` (primary and foreign keys), bare `id`, and the bookkeeping columns (added, updated, marked, look, unq) from the listview by default. As of v1.0.68, `url`, `tags`, and `notes` are also hidden by default — they're "extended" data per row that the user reaches via the Say-X family (Say Url, Say Tags, Say Notes) or edits via Edit Record (F2 Edit Cell or Ctrl+E Edit Record, which both show the FULL field set). Use the Select Columns command (Alt+S) to override on a per-table basis; the override persists across sessions via DbDuo.ini.

### Example schema using the standard fields

The `teachers` table from `sample.db`:

```sql
CREATE TABLE teachers (
    teacher_id  integer  primary key autoincrement,
    added       datetime not null default current_timestamp,
    updated     datetime not null default current_timestamp,
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

## Bundled sample databases

DbDuo ships five SQLite sample databases, each adapted to the standard column conventions described above. The Help menu has one-keystroke commands to open each one (see "Help menu" above); all five open via the same code path File > Open Database uses. The first three are "textbook" databases borrowed from the broader SQLite community; the latter two are "hobbyist" databases that DbDuo author Jamal Mazrui assembled based on research into the most popular real-world personal database categories.

**`sample.db`** — a small school domain. Four tables: `teachers`, `classes`, `students`, `enrollments`. Three rows each — twelve rows total — just enough to demonstrate parent-child relationships (a teacher teaches classes; students enroll in classes through the enrollments junction table). The minimum that exercises every standard column, the parent-child drill, and the related-records view. Opens automatically on first launch from a clean install.

**`northwind.db`** — the classic Microsoft Northwind sales sample, adapted to DbDuo's column conventions. Eight tables: `categories` (8 rows), `suppliers` (10), `products` (24), `customers` (12), `employees` (9), `orders` (14), `order_details` (21), `shippers` (3) — 101 rows total. Rich parent-child shape: a category has products, a supplier has products, a customer places orders, an employee handles orders, an order has order_details, a shipper ships orders. Useful for exercising DbDuo's parent-child drill against multi-level relationships with realistic row counts. **DbDuo's adaptations are minimal and trivial**: every table has `<table>_id` primary keys (the canonical Northwind uses `CategoryID`, `SupplierID`, etc. — same idea, snake_case naming); the standard columns `added`, `updated`, `marked`, `notes`, `tags`, `look`, `unq`, and (new in v1.0.68) `url` are appended to each table; `notes` and `tags` are declared as `TEXTMEMO` so DbDuo's Edit Record dialog renders a multi-line memo widget for them. The substantive columns (company, contact, city, country, phone for customers; first_name, last_name, title for employees; and so on) are preserved verbatim. Learn more about the canonical Microsoft Northwind:

- Microsoft Learn — [Northwind sample database overview](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/ef/loading-related-objects)
- GitHub — [microsoft/sql-server-samples / Northwind](https://github.com/microsoft/sql-server-samples/tree/master/samples/databases/northwind-pubs)
- Wikipedia — [Northwind Traders sample](https://en.wikipedia.org/wiki/Northwind_Traders)

**`chinook.db`** — the classic Chinook music-store sample, adapted to DbDuo's column conventions. Nine tables: `genres` (14 rows), `media_types` (5), `artists` (20), `albums` (20), `tracks` (33), `employees` (8), `customers` (15), `invoices` (17), `invoice_items` (26) — 158 rows total. Three-deep parent-child chains (artist → album → track; customer → invoice → invoice_item) make this database the best for stress-testing the deeper drill stacks and the related-records view at multiple levels. **DbDuo's adaptations are minimal and trivial** — same shape as the Northwind adaptation: primary keys are `<table>_id`; standard columns `added`, `updated`, `marked`, `notes`, `tags`, `look`, `unq`, `url` are appended; `notes` and `tags` are upgraded to `TEXTMEMO`. The substantive columns (name, title, artist_id, album_id, genre_id, etc.) are preserved verbatim. Learn more about the canonical Chinook:

- GitHub — [lerocha/chinook-database](https://github.com/lerocha/chinook-database) (Luis Rocha's reference repository; the SQL Server, MySQL, PostgreSQL, Oracle, and SQLite variants live here)
- SQLite Tutorial — [Chinook sample database](https://www.sqlitetutorial.net/sqlite-sample-database/) (with a clear ER diagram description)

**`collection.db`** — a personal music collection. Three tables: `artists` (8 rows), `albums` (16), `tracks` (22). Models a domain that hobbyist software like CLZ Music, MyMusicCollection, and Musicnizer have refined for decades: per-album metadata (title, release year, format, label, catalog number, genre), per-artist metadata (sort name, country, active years), per-track metadata (track number, title, duration). Includes fields for personal ratings, physical location, and loan tracking. Useful as a template for building your own collection database; the schema generalizes naturally to books, DVDs, or any other catalogable hobby. The schema differs from `chinook.db` by emphasizing per-album collector fields (loan status, location, rating, catalog number) rather than per-track sales data.

**`cellar.db`** — a personal wine cellar. Three tables: `wines` (8 rows), `bottles` (10), `tastings` (4). Models the data model that CellarTracker, eSommelier, and VinCellar have refined: a wine identity (producer + vintage + varietal + region) is separate from the individual bottles you own (each with a bin location, purchase price, source, and consumption status), and tasting notes are stored separately so the same wine can have multiple tastings over years of aging. The standout analytical feature is the **drink-window query** — for every wine in the cellar, where does it sit in its drinkable range? — bundled as `Scripts/WineDrinkWindow.sql`. This database illustrates how a relational schema serves a real workflow that no flat list or spreadsheet can: "find the wines closest to the end of their drink window" is one ORDER-BY clause away.

All five databases use the same column conventions, so navigation commands and column-hiding rules behave consistently across them. The five are complementary rather than alternatives: `sample.db` is the gentle introduction; `northwind.db` is the relational textbook example; `chinook.db` is the deeper-hierarchy stress test; `collection.db` and `cellar.db` are starting points for hobbyist users who want to track their own real-world things.

### Updated-timestamp triggers

Each bundled database carries one SQLite trigger per table (named `dbduo_<table>_updated`) that maintains the `updated` column automatically. The trigger fires `AFTER UPDATE` and bumps `updated = current_timestamp` only when one of the substantive columns actually changed in value. The `marked`, `added`, `updated`, `look`, and `unq` columns are deliberately excluded from the substantive set, so toggling the `marked` flag (Control+M / Control+U, or any of the range-mark commands) does NOT bump the timestamp — `marked` is a UI flag, not a content edit, and bumping the timestamp on every Mark Record would scramble "sort by recently edited" for users who use marking as a working-set tool.

The check uses `OLD.col IS NOT NEW.col` for each substantive column, joined by `OR`, so the trigger correctly skips both the "ADO only writes one column" case (Mark Record updates only `marked`) and the "Edit Record writes every column with values unchanged" case (the F2 dialog hands every field back even if untouched). NULLs are handled correctly because `IS NOT` is null-safe in SQLite, unlike the regular `<>` operator.

For users creating their own tables, the same trigger pattern is recommended:

```sql
CREATE TRIGGER dbduo_<table>_updated
AFTER UPDATE ON <table>
FOR EACH ROW
WHEN OLD."col1" IS NOT NEW."col1"
  OR OLD."col2" IS NOT NEW."col2"
  -- one OLD/NEW comparison per substantive column;
  -- omit marked, added, updated, look, unq
BEGIN
    UPDATE <table> SET updated = current_timestamp WHERE rowid = NEW.rowid;
END;
```

DbDuo itself does not write to `updated`; the timestamp behavior comes entirely from these triggers. The same pattern works on Access (using `ON UPDATE` data macros) but DbDuo does not ship Access samples, so the trigger SQL above is SQLite-specific.

## Persistence

Between sessions, DbDuo remembers the last opened database and table (relaunch goes straight there), the last folder used for each kind of file dialog (open/save/import/export remembered separately), per-table sort/filter/position/Select-Column lists within a session, and one named bookmark per session. Settings live in `%LOCALAPPDATA%\DbDuo\DbDuo.ini`.

The shipped `DbDuo.ini` next to the executable holds configuration that ships with the install: `[General]` for `uiMode`, `[Keys]` for hotkey overrides. The per-user file in `%LOCALAPPDATA%\DbDuo\` holds session state: `[Session]` for last-opened, `[Folders]` for remembered directories. The two files coexist; per-user values take precedence.

## Logging

DbDuo writes a per-session log to `%LOCALAPPDATA%\DbDuo\DbDuo.log` (truncated at every program start). The log records database opens, table switches, errors, and the result of hotkey registration. Use the Show-Log command (no hotkey) to print the exact path.

## Bundled documentation

- `README.md` and `README.htm` — summary and quick start with a guided tour of `sample.db`.
- `Announce.md` and `Announce.htm` — release announcement.
- `DbDuo.md` and `DbDuo.htm` — this reference.
- `History.md` and `History.htm` — chronological release notes.
- `License.md` and `License.htm` — MIT License text.
- `CamelType_CSharp.md` — coding conventions used inside `DbDuo.cs`, for developers.

# Development

This section is for anyone building DbDuo from source or extending it.

## Requirements

DbDuo is a single-file C# program targeting .NET Framework 4.8 on Windows x64. Compilation needs `csc.exe` from the .NET Framework 4.8 developer pack (Microsoft ships this in standalone form for free) — no Visual Studio required. Inno Setup 6 is used to build the installer. Pandoc is used to render the Markdown docs to HTML at release time.

## Build steps

```
buildDbDuo.cmd
```

This single batch file compiles `DbDuo.cs` to `DbDuo.exe` with the right `csc.exe` flags (`/target:winexe`, `/platform:x64`, `/optimize+`, references to `System.Windows.Forms.dll`, `System.Drawing.dll`, and the Microsoft accessibility assembly), plus `jsc.exe` for the tiny `DbDuo.dll` support assembly (compiled from `DbDuo.js`). The build script auto-locates `csc.exe` by walking the .NET Framework install path; if your developer pack is in a non-standard location, edit the `set csc_path=` line at the top.

```
"%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" DbDuo_setup.iss
```

This compiles the installer. `DbDuo_setup.iss` is a standard Inno Setup script with a Welcome → SelectDir → Ready → Installing → Finish wizard, a `CurStepChanged` hook that fetches the SQLite ODBC and ACE drivers on the Installing step, and `[Files]` entries for the bundled artifacts. The output is `DbDuo_setup.exe` in the project root.

## Architecture: one connection, two interfaces

The GUI form (`DbDuoForm`) and the dot-prompt CLI (`Program.cmdXxx` static methods) share one `DbDuoManager` instance. The manager owns the single `ADODB.Connection` and the active `ADODB.Recordset`. Both interfaces call manager methods (`openDatabase`, `selectTable`, `applyFilter`, etc.) rather than working directly with ADO objects. When an edit is committed in either interface, the manager's notification path tells the form to refresh its list view; the next dot-prompt command sees the new state automatically because it queries the same connection.

`CursorLocation = adUseClient` is set on every Connection.Open. This means filter, sort, and bookmark operations happen client-side after the recordset is fetched. The benefit is that the same code path works against SQLite, Access, Excel, and dBASE without per-provider quirks. The cost is that `.Filter` does not push down to the database engine.

## Coding style

DbDuo.cs follows the **Camel Type** coding conventions documented in `CamelType_CSharp.md`. Key points:

- Hungarian-prefixed lower camel case variable names: `sName`, `iCount`, `bFound`, `aRows`, etc.
- The prefix `o` is reserved for COM objects only; managed-type instances use a class-name prefix instead.
- Constants follow the same naming pattern as variables (no `c_` prefix), but are declared with `const` or `static readonly` on lines separate from the variables.
- Methods rather than subprocedures; methods that return values where practical.
- `using` directives at the top of the file, alphabetized within their group.
- `foreach` over `for` when iterating a collection.

Read `CamelType_CSharp.md` for the full set of rules.

## Layout by Code

DbDuo uses an approach called Layout by Code (LbC) for every dialog it creates. The idea is that a programmer composes a dialog by writing a sequence of "add this control" calls in code, in the order the user will encounter them — rather than dragging boxes around in a visual designer or hand-writing pixel coordinates. The result is dialogs that are screen-reader-friendly by construction: tab order matches reading order, every input control has a label that the reader announces, and the layout flows from top to bottom in one pass.

The concept was developed by Jamal Mazrui (the original author of DbDuo) starting in 2006, with an AutoIt implementation that established the vocabulary still in use today. Layout by Code has since been ported to several languages — wxPython, JScript .NET (the Homer Application Framework), and now C# in DbDuo. Each port keeps the same intuition: a dialog is a vertical stack of labeled controls, the programmer writes them out one at a time, and the framework handles the spacing, the focus-tip plumbing, the tab indices, and the accept/cancel wiring. What is notable about DbDuo's port is that it preserves the **vocabulary** of the earlier ports — methods like `addInputBox`, `addMemoBox`, `addPickBox`, `addComboPickBox` — so a programmer who learned LbC in AutoIt or wxPython can pick up the C# version without retraining.

### The conceptual model: bands, the layout cursor, and dialog units

The earlier LbC implementations talk in terms of three concepts that are worth introducing because they underpin the API even where the C# port hides them behind a simpler stack.

A **band** is a horizontal row of related controls — for example, a Label plus an associated TextBox, or a Label plus an associated ComboBox. The Microsoft Official Guidelines for User Interface Design, published in the late 1990s and based on years of usability research, specify how much horizontal and vertical space should separate the controls within a band, between consecutive bands, and around the dialog's edges. The original AutoIt LbC implementation exposed bands and groups as first-class objects with their own ID numbers, addressable via functions like `_lbcStartBand` and `_lbcStartHGroup`. The Python version simplified this: each call to an `Add*` method starts a new band; adding a separator advances to the next group. DbDuo's C# port simplifies further still: every control added with one of the `addX` methods is its own band, stacked vertically. The horizontal-band model is still available — `addInlineInputBox` puts a label and a textbox on a single row — but the default is one control per row, top to bottom.

A **layout cursor** is the invisible point on the dialog where the next control will be placed. In the original AutoIt port the cursor was an addressable global state — `$nLbcCol` and `$nLbcRow` for column and row — and one could move it manually with `_lbcCtrlSetLeft` or `_lbcCtrlSetTop`. In the C# port the cursor is an implementation detail managed by a `FlowLayoutPanel` plus a vertical-stack `Panel`; the programmer never sees it. What survived from the older implementations is the *concept* that controls are added sequentially and spacing is automatic.

A **dialog unit** is the Microsoft-defined measurement that LbC uses internally for spacing decisions. A horizontal dialog unit is a quarter of the average character width of the system font; a vertical dialog unit is an eighth of the average character height. The guidelines specify three dialog units between a label and its associated control, four between related controls in the same group, and seven between unrelated controls in consecutive groups. DbDuo's C# port encodes these in named constants — `DefaultButtonHeight`, `DefaultLineHeight`, `DefaultPadding`, `DefaultRowGap`, `DefaultListHeight`, `DefaultMemoHeight`, and so on — rather than recomputing from font metrics, because modern Windows dialogs are dpi-aware and the WinForms framework's own font scaling handles the dialog-unit-to-pixel translation for us.

### Why LbC matters for a screen-reader audience

Visual dialog editors — the kind that ships with Visual Studio for WinForms, or with Glade for Gtk — produce dialogs whose controls are positioned by pixel coordinates the programmer specified by dragging boxes around. The pixel coordinates fix the visual layout but say nothing about reading order; in WinForms, reading order is governed by `TabIndex`, which a sighted programmer might forget to set, or set inconsistently with the visual layout. A screen-reader user navigating that dialog with Tab will visit controls in `TabIndex` order, not visual order, and the mismatch is one of the most common accessibility bugs in commercial software.

LbC eliminates the problem by construction: there is no separate `TabIndex` to forget. Each `add*` call increments an internal `iTabIndex` and assigns it to the new control. Reading order is the order of `add*` calls, which is also the order in which the programmer is thinking about the form. The visual layout is a deterministic function of the same sequence. The two cannot drift apart.

A second screen-reader benefit is the **focus-tip plumbing**. Each `add*` method takes an optional `sTip` parameter that is wired to a status bar at the bottom of the dialog. When the user tabs into the control, the tip text is written to the status bar; screen readers announce status-bar changes through their live-region facility, so the tip is read aloud automatically without forcing a popup dialog. This pattern is borrowed directly from Homer LbC (the JScript .NET port) which introduced it specifically because JAWS suppresses ordinary WinForms tooltips on tab navigation; status-bar messages, by contrast, are reliably announced.

A third benefit is the **memo/AcceptButton coordination**. When the user is typing into a multi-line TextBox (one created via `addMemoBox` or `addTextMemo`), pressing Enter should insert a newline rather than submit the dialog. The framework arranges this transparently: while focus is on a memo control, the form's `AcceptButton` is temporarily cleared, so Enter is delivered to the textbox as a newline insertion; on `LostFocus` the `AcceptButton` is restored, so Enter from single-line fields still submits. The Homer LbC convention is preserved in the C# port verbatim — programmers don't have to think about it.

### Anatomy of an LbC dialog

The typical usage pattern in DbDuo is one `using` block per dialog:

```csharp
using (LbcDialog dlg = new LbcDialog("Configuration", this))
{
    TextBox  tbMode  = dlg.addInputBox("UI mode:",       "both", "How DbDuo launches");
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

DbDuo's LbcDialog exposes the following methods for assembling a dialog. Two naming patterns coexist: the **bare-control adders** (`addLabel`, `addTextBox`, `addCheckBox`) and the **labeled-control adders** that mirror the Homer LbC naming (`addInputBox`, `addMemoBox`, `addPickBox`, `addComboPickBox`). The labeled adders emit a `Label` first and then call the bare adder; both flavors are kept because some sites favor the explicit two-call form, others favor the compact one-call form.

`addLabel(string sText)` adds a static text label as its own row. Useful for paragraphs of instruction at the top of a dialog or above a group of fields. Returns the `Label`.

`addInputBox(string sLabel, string sValue, string sTip)` adds a label, a single-line `TextBox` below it, and registers the tip with the status bar. Returns the `TextBox`. The textbox's `AccessibleName` is set from the label so screen readers announce the field correctly on tab-in. This is the workhorse method for editing scalar values: names, paths, single integers, simple regex patterns.

`addInlineInputBox(string sLabel, string sValue, string sTip)` is the horizontal variant: label and textbox on a single row, sharing the same band. Used in the Edit Record dialog where vertical density matters and there are many short fields.

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

The DbDuo C# port intentionally simplifies what the original AutoIt LbC offered. The AutoIt version exposed bands and groups as first-class objects with manipulation functions (`_lbcStartBand`, `_lbcBandHCenter`, `_lbcBandEvenSpace`, `_lbcWinRespace`, and dozens more), allowed precise pixel-level positioning of controls after the fact, and let the programmer change widths or heights with reference-band alignment. The DbDuo port omits these because:

- WinForms anchors and Dock properties make most band-respacing unnecessary — the form resizes the right way automatically.
- Most DbDuo dialogs are small (under ten controls), so the gymnastics of fine alignment is not needed.
- A simpler API is easier to teach to contributors who haven't seen the AutoIt original.

What is preserved across all four LbC ports — AutoIt, wxPython, JScript .NET (Homer), and C# (DbDuo) — is the **practice** of writing dialogs as a sequence of `addX` calls in reading order, with labels above or beside each control, tips wired to a status bar, tab order = call order, and submit/cancel wiring handled by the framework. A programmer fluent in one port can read code in any of the others and find the same shape.

Convenience-dialog wrappers like the Python port's `DialogShow`, `DialogConfirm`, `DialogInput`, `DialogPick`, `DialogMultiInput`, and `DialogBrowseForFolder` are not provided as named methods in the C# port — DbDuo's call sites typically need only `runOkCancel` plus one or two add calls, so the wrappers would only save a few characters. The patterns are documented here in case a contributor wants to add them.

### Open-ended extension

LbcDialog is a public class (in `DbDuo.cs`, line 5668 onward). It is reusable outside the DbDuo command set — a script invoked via `Invoke-Script` could in principle construct its own LbcDialog through the C# COM bridge, though no bundled script currently does so. If you write a new GUI command for DbDuo and need a custom dialog, use LbcDialog. Do not hand-roll a Form with manual TabIndex assignment; the screen-reader-friendliness will degrade.

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

**Master enable flag:** the entire set is controlled by `[Lbc] extraKeys` in `DbDuo.ini`. Default is `Y` (on). Set to `N` to disable the EdSharp hotkeys entirely and let standard Windows text-edit behavior pass through unchanged. The setting is read once on first use and cached for the session.

**Status bar feedback:** every hotkey updates the dialog's status bar with a short label (`Copy Line`, `Append to Clipboard`, `Cut Line`, `Start Selection`, `Complete Selection`, `Copy All`, `Read All`, `Delete Line`). The live region echoes the action via a brief utterance (typically the operation name plus the affected text or character count) so the screen reader confirms each action without the user having to look at the status bar.

## GUI versus CLI response patterns

DbDuo operates in two modes: a Windows Forms GUI with a ListView grid, menus, and modal dialogs, and a dot prompt running in a console window. Most commands work in both modes, with the same canonical verb name and the same effect on the database. The user-visible response is different in each mode by design — the GUI uses focus changes, status-bar updates, dialogs, and the screen-reader live region; the CLI prints lines of text to stdout and reads input from stdin. The principle: **same data effect, mode-appropriate confirmation**.

The remainder of this section walks through the command families and describes how each fits the GUI / CLI duality. Where a command does not reasonably apply in one mode, the table notes it explicitly.

**Navigation commands** (Step-Record family, Set-Position, Find, Jump-Record, Search-Next, Search-Previous, Step-InitialChange). GUI: move the listview row selection and the ADO cursor; the cell-changed handler speaks the new row via the live region. CLI: print the new row's summary (column-equals-value pairs from the display fields) on stdout. The data-layer behavior is identical — same ADO MoveNext / MovePrevious / AbsolutePosition call — only the confirmation differs.

**Display commands** (Show-Object, Show-Table, Show-Schema, Show-Status, Show-Related, Show-Log, Show-History, Show-Readme). GUI: open a read-only LbcDialog with a multi-line TextBox containing the requested content; OK to dismiss; Control+C inside the TextBox copies. CLI: print the same content to stdout, one line per row, with field-name prefix where helpful. Some commands have a GUI-only LbcDialog form (Show-Object, Show-Related) and a CLI-equivalent multi-line text dump; some are inherently textual (Show-Schema, Show-Log) and use the same wording in both.

**Speech-only commands** (Say-Status, Say-Path, Say-Yield, Say-Tables, Say-Marked, Say-Updated, Say-Notes, Say-Tags, Say-Column, Say-Position, Say-SortFilter, Say-YieldMarked). GUI: push text through the LiveRegion (JAWS COM, NVDA controller DLL, or UIA live-region fallback) so the screen reader speaks it without moving focus. Double-press the same chord within two seconds: open a read-only multi-line LbcDialog with the same text, useful for review and Control+C copy. CLI: print the same text to stdout. The "double-press shows dialog" gesture has no CLI analog and is silently dropped — pressing the verb twice in a CLI session just prints the same lines twice, which is harmless.

**Edit commands that mutate one row** (New-Record, Set-Record, Set-Cell, Remove-Record, Copy-Record, Set-Mark, Clear-Mark, Save-Bookmark, Restore-Bookmark, Clear-Bookmark). GUI: open an LbcDialog with per-field TextBoxes (`RecordEditDialog` for full row, single TextBox for Set-Cell), validate, OK commits, Cancel discards. The dialog respects per-field regex patterns from `[Validation:<table>]`. CLI: take the column-equals-value pairs on the command line (`set-cell email = a@b.com`) or for full row editing print each editable field and prompt for a new value at the prompt; same validation. No-arg CLI invocations of New-Record fall back to prompting line by line; this works but is uncommon — the typical CLI editing path is Set-Cell or Set-Field with the value on the command line.

**Edit commands that mutate many rows** (Update-Field, Mark-Range, Unmark-Range, Step-InitialChange, Extract-Regex). GUI: prompt for any missing parameters in an LbcDialog (find/replace strings, column, regex), execute the batch, refresh the listview, speak a summary like "Marked 17 of 22 rows (rows 5 to 26)." CLI: take parameters on the command line, run the batch, print the same summary to stdout. The batch logic is identical — only the input-collection layer differs.

**Table-level commands** (Open-Database, Close-Database, Select-Table, Backup-Database, Save-DatabaseAs, Test-Database, Test-Driver, Get-Table, Sync-Session, Update-View). GUI: OpenFileDialog or LbcDialog for inputs; status-bar update on completion. CLI: take a path or table name on the command line; print a one-line confirmation on completion. Test-Database and Test-Driver are inherently textual reports (lists of integrity-check results or driver-presence checks); both modes show the same lines, the GUI in an LbcDialog and the CLI on stdout.

**Filter and sort commands** (Select-Record, Reset-Filter, Sort-Object, Reset-Sort, Select-Column). GUI: LbcDialog for criteria input, with helpful prompt labels. CLI: take the criteria string directly (`select-record where year > 2020`). Both apply the same ADO Filter / Sort properties; the listview refreshes in GUI mode, the CLI prints the new row count.

**File-export commands** (Export-Data, Import-Data, New-Chart, New-Plot, Out-File). GUI: SaveFileDialog or LbcDialog for output path, then run the Office-COM or file-writing path; open the result in the default app on completion. CLI: take the output path on the command line, write the file, print the saved path on stdout. New-Plot is the meaningful asymmetry: charts inherently need a graphical viewer, so the CLI form prints the Describe-Column textual summary instead and tells the user to invoke the command from the GUI to see the chart. New-Chart works the same way.

**Configuration and meta commands** (Edit-Configuration, About-DbDuo, Get-Help, Get-Verb, Switch-KeyDescriber, Switch-Focus, Enter-Console, Exit-Application, Exit-Console, Exit-Child, Enter-Child, Elevate-Version). GUI: LbcDialog or message box. CLI: print the equivalent text to stdout, or in the case of Edit-Configuration, bounce to the GUI dialog if a form is available (Both mode) or print the .ini path for hand-editing in CLI-only mode. Switch-Focus is GUI-specific in effect (brings the form to the foreground) but is meaningful from CLI when both UIs are running.

**Commands without a meaningful CLI form**: Open-FileFolder (launches Windows Explorer — works from either mode, but the action is the same shell-execute), Switch-Focus (no-op in CLI-only mode; prints a notice), Enter-Console (a no-op when issued from the CLI; prints "already at the dot prompt"). The reverse — commands that exist GUI-only and have no CLI verb — is now a closed set: there are none. Every menu item in DbDuo has a canonical verb that the dot prompt accepts.

**The general rule**: every command's data effect is mode-independent; every command's confirmation layer is mode-appropriate; every CLI invocation accepts unique prefix matches against canonical verbs (so `first` resolves to `step-record-first` if no other canonical starts with `first`, `meas col` resolves to `measure-column`, and so on). Where a typed prefix is ambiguous, the CLI prints the candidate list so the user can disambiguate by typing more characters.

## Office automation: always CreateObject, never GetActiveObject

DbDuo never attaches to a running Word or Excel instance. Every Office-using export path makes a fresh hidden instance, drives it to produce the requested file, then calls `Quit()` and releases the COM object. The reasons: attaching to a running instance would mutate settings the user has tuned for their session, driving a running instance through SaveAs and Quit risks side effects on documents the user has open, and the behavior would be non-deterministic depending on whether Word or Excel happens to be running.

## File layout

```
DbDuo.cs                    single-file C# source
DbDuo_setup.iss             Inno Setup installer script
buildDbDuo.cmd              compile DbDuo.exe via csc.exe (+ jsc.exe for DbDuo.dll)
DbDuo.js                    JScript .NET source for the script support assembly
DbDuo.ini                   shipped configuration (uiMode, [Keys] overrides)
sample.db                   small school sample (teachers/classes/students/enrollments)
northwind.db                Northwind sales sample, adapted
chinook.db                  Chinook music-store sample, adapted
SampleScripts/             three example .js scripts seeded into %APPDATA% on first run
README.md, README.htm       summary and quick start
DbDuo.md, DbDuo.htm         this reference
License.md, License.htm     MIT License
Announce.md, Announce.htm   release announcement
History.md, History.htm     chronological release notes
CamelType_CSharp.md         coding conventions used inside DbDuo.cs
```
