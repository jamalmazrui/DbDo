# DbDuo Reference

**An accessible, keyboard-first database manager for Windows.** DbDuo opens SQLite, Microsoft Access, Excel, dBASE, and delimited-text files through one consistent set of PowerShell-flavored commands, in a GUI window and a dot-prompt console at the same time. JAWS, NVDA, and Narrator are all first-class through dedicated speech paths; every command is reachable by keyboard.

Version 1.0.51. Source and releases: <https://github.com/JamalMazrui/DbDuo>.

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

Setup creates five Start Menu shortcuts and one Desktop shortcut. The plain **DbDuo** shortcut opens both GUI and console, which is the default mode. The **DbDuo (GUI only)**, **DbDuo (CLI only)**, **DbDuo (read-only)**, and **DbDuo sample database** shortcuts cover the variant launch modes.

Use the Desktop hotkey, Alt+Control+D (D for Desktop), from anywhere in Windows to activate a running instance or launch a fresh one. DbDuo is single-instance: a second press of Alt+Control+D wakes the existing window rather than spawning a duplicate.

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

The double-press-spells convention applies to all speech-only commands, not just Numpad5: press Say Status (Alt+Z), Say Path (Alt+P), Say Yield (Alt+Y), Say Tables (Shift+F4), Say Marked (Shift+L), Say Date (Shift+D), Say Type (Shift+T), or Say Marked Yield (Shift+Y) twice within 1.5 seconds to hear the text spelled out character by character.

The virtual cursor synchronizes with the listview row selection in both directions: pressing plain Down/Up arrow updates the listview's row selection, and the virtual row follows along with the column unchanged. Pressing Alt+Control+arrow moves the virtual cursor first, then moves the listview's row selection to match — so you can see the row you're virtually browsing.

The virtual cursor resets to (row 1, first column) whenever a table is freshly opened or the view is refreshed with F5.

**Column-aware commands default to the virtual column.** When a command needs a column — Sort Ascending, Sort Descending, Open Cell Value, Next Initial Change, Jump to Match in One Column — its picker dialog defaults to the column currently under virtual focus. Just press Enter in the picker to accept that column, or arrow up/down to pick a different one.

## JAWS settings for DbDuo

JAWS has its own table-navigation chord set on Alt+Control+arrow, and by default JAWS intercepts those chords before the focused application sees them. When you press Alt+Control+RightArrow inside DbDuo without any settings adjustment, JAWS doesn't recognize the virtual-mode ListView as a table and announces "Not in a table" instead of letting DbDuo run its virtual-cell command. The same applies to several other DbDuo chords on Alt-letter and Shift-letter combinations.

The fix is a three-file JAWS settings bundle:

- `DbDuo.jkm` — JAWS key map. Maps the chords DbDuo wants to take over to a Script named `PassDbDuoKey`.
- `DbDuo.jss` — JAWS script source. Defines `PassDbDuoKey` as a one-line Script that calls the JAWS built-in `TypeCurrentScriptKey()`, which passes the current keystroke through to the application as if no script were running.
- `DbDuo.jsb` — compiled binary of the above, produced by JAWS's `scompile.exe`. JAWS loads this at run-time and resolves the `PassDbDuoKey` reference in the JKM.

The reason the JSS-and-JSB pair is needed (rather than just the JKM) is that the JKM format requires a Script name on the right-hand side of each binding. `TypeCurrentScriptKey()` is a JAWS *Function* — callable from inside Scripts but not from a JKM directly. So we wrap it in a one-line Script.

**Automatic install.** The DbDuo installer offers to install this bundle automatically. On the Ready page you'll see a checkbox "Install JAWS settings for DbDuo (recommended if you use JAWS)" which is checked by default. Selecting it does three things for every JAWS year-version present on your system, in every language subfolder inside each version's `Settings` folder:

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

where `<year>` is your JAWS year-version (2024, 2025, etc.). The settings work from JAWS's next launch (or, if JAWS is already running, when DbDuo next gains focus).

The chords passed through to DbDuo include: the virtual-cursor family (Alt+Control + arrows, Home, End, PageUp, PageDown, NumPad5); the parent-child drill family (Alt+RightArrow, Alt+LeftArrow, Alt+Home); the three search families (Control+F, Control+J, Control+F3 plus their Shift variants and F3 / Shift+F3); marked-row navigation (Control+Home, Control+End, Control+UpArrow, Control+DownArrow); bulk-mark spans (Shift+Home, Shift+End, plus Alt+Shift variants); and the Alt-letter command shortcuts (Alt+A, C, D, E, K, L, P, R, T, Y, Z plus relevant Alt+Shift variants).

If you customize the JKM in place and re-install DbDuo, your changes will be overwritten. To preserve customizations across updates, copy your modified version to a different filename and load both via JAWS's chain mechanism, or keep a copy outside the Settings folder and merge by hand after each DbDuo upgrade.

If you uninstall DbDuo, the installer removes only the three files it placed. Other JKMs or JSBs you placed yourself in those folders are not touched.

**NVDA support is planned for a future release.** NVDA has the same issue — it intercepts Alt+Control+arrow for its own table-navigation commands. The fix for NVDA is an add-on (`.nvda-addon` file). Until that ships, NVDA users can work around the problem by pressing NVDA+F2 (single-press pass-through) before each Alt+Control+arrow chord, which sends the next keystroke directly to the application.

**Narrator does not support scripts or add-ons.** Narrator users will get less polished cell-level navigation than JAWS or NVDA users; the virtual-cursor announcements still fire, but Narrator may layer its own announcement on top.

## File menu

Use the New-Database command, Control+Shift+N (N for New, Shift to distinguish it from Control+N which makes a new row), to create an empty SQLite database at a chosen path. Use the Open-Database command, Control+O (O for Open), to bring up a file dialog and choose an existing database; DbDuo recognizes `.db`, `.sqlite`, `.sqlite3`, `.mdb`, `.accdb`, `.xlsx`, `.xls`, `.dbf`, `.csv`, `.tsv`, and `.txt` files.

Every file dialog remembers the folder you last used and opens there next time. New-Database, Open-Database, Save-DatabaseAs, and Backup-Database share one remembered "open" folder, since you typically keep your databases together. Import-Data and Export-Data remember their own folders separately. If no remembered value exists, the dialog falls back to the folder of the currently-open database, then to your Documents folder.

Use the Recent Files command, Alt+R (R for Recent), to open one of the last ten database files DbDuo has seen. The dialog shows each path with the table that was active when the file was last closed; selecting an entry reopens the file, restores that table, and restores the per-table filter, sort, and row position. If any of those pieces no longer apply (the table was dropped, a filter column was removed, the row count shrank below the saved position), DbDuo silently skips the incongruity and reopens with the best-fitting state it can.

Use the Save-DatabaseAs command, Control+S (S for Save), to write a copy of the open database to a new path and switch DbDuo to the new file. The dialog suggests `<original>-copy` as the filename so a stray Enter doesn't overwrite the source. Use the Backup-Database command, Control+Shift+S, to write the same copy but keep the original open; the suggested filename is `<original>-backup-yyyyMMdd` (S for Save, Shift to distinguish a backup snapshot from a Save-As). The Close-Database command, Control+F4 (the MDI close convention), closes the open file without exiting DbDuo.

Use the Import-Data command, Control+Shift+I (I for Import), to read a GitHub-flavored Markdown table file and append its rows into the currently-open table. Header cells are matched case-insensitively to columns in the destination; cells with no matching column are dropped silently. Embedded `<br>` decodes back to newline, `\|` back to a literal pipe. Multi-table files (separated by blank lines) all import; per-row errors do not stop the import.

Use the Export-Data command, Control+Shift+X (X for eXport), to write the current filtered view to one or more files. Every input format DbDuo can open is also an export format: xlsx, docx, filtered HTML, Markdown table, csv, tsv, SQLite, Access, dBASE. The GUI prompts for one destination at a time; the dot prompt accepts a multi-format argument like `Export-Data xlsx docx md csv` (or the short forms `x d m c`). After each export, DbDuo opens the result in its default Windows application so you immediately hear what was produced.

The xlsx and docx formats use Word and Excel through late-bound COM and therefore need Microsoft Office; csv, tsv, md, plain HTML, SQLite, Access, and dBASE all work without Office. SQLite, Access, and dBASE exports open a separate ADODB connection to a fresh file, issue `CREATE TABLE` with portable text-typed columns, and INSERT row by row — the user's open recordset is not disturbed.

The File menu also hosts the table-switching commands: **Choose Table** (F4) opens a listbox of base tables, **Choose View** opens the equivalent for views, **Next Visited Table** / **Previous Visited Table** (Control+Tab / Control+Shift+Tab) cycle among recently-opened tables in MRU order, and **Next Object** / **Previous Object** (Control+F6 / Control+Shift+F6) cycle through every table and view without the MRU filter. These commands live in the File menu because choosing what's on screen is a file-level operation (and EdSharp uses F4 for "Current Windows" on the same logic).

Use the Print command, Control+P (P for Print), to print the current view; this is reserved for a future release. For now, export to HTML or docx and print from the corresponding application.

Use the Exit DbDuo command, Alt+F4 (the Windows-standard close-program key), to close DbDuo entirely. The dot prompt's `quit` and `q` commands map to Exit-Application as well; `exit`, `x`, and `bye` map to Exit-Console, which leaves the dot prompt but keeps the GUI running.

## Edit menu

Every Edit-menu command operates on the current row, except where noted.

Use the New Record command, Control+N (N for New), to add a row. DbDuo shows an edit dialog with one line per distinct field; bookkeeping fields (`added`, `updated`, `observed`, `marked`, the primary key) get their default values automatically. Use the Edit Record command, F2 (the Windows-standard rename key), to edit the current row. Use the Delete Record command, Control+D (D for Delete), to remove the current row; the Delete key alone is a secondary binding so the Excel and Outlook convention works too.

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
- Say Marked (Shift+L) — the `look` values of every marked row
- Say Date (Shift+D) — the `updated` value of the current row
- Say Type (Shift+T) — whether the current object is a table or view, plus the row position
- Say Marked Yield (Shift+Y) — count of marked rows

**Shape commands.** Use the Filter Records command, Shift+F (F for Filter), to apply an ADO Filter expression like `name LIKE '%bridge%'`. Use the Clear Filter command, Shift+R (R for Reset), to clear it. Use the Custom Sort command, Shift+S (S for Sort), to type an arbitrary ADO Sort expression like `name ASC, year DESC`.

Use the Sort Ascending by Column command, Alt+A (A for Ascending), to sort by a chosen column alphabetically; use Alt+Shift+A for Sort Descending. Each prompts for the column with a listbox that defaults to the column under virtual focus — just press Enter to accept.

Use the Sort by Date Updated (oldest first) command, Alt+D (D for Date), to sort by the table's `updated` column with the oldest at the top; use Alt+Shift+D for the most-recent-first variant. Use the Clear Sort command to drop the sort so the recordset returns to its natural order.

## Misc menu

Use the Refresh View command, F5 (the browser-standard refresh key), to re-query the database from disk; useful when another tool has written to the file while DbDuo had it open. F5 also resets the virtual cursor to (row 1, first column).

Use the Toggle Read-Only Lock command, Control+F7 (F7 = lock convention), to switch the recordset between editable and read-only; the window title shows the change.

Use the Table Statistics command (no hotkey) to print row counts and per-column statistics for the current table. Use the Frequency Chart command (no hotkey; Alt+C as an alias) to render a frequency-by-column chart in Excel for analysis.

Use the Choose Visible Columns command (no hotkey; Alt+L as an alias) to pick which columns appear in the data list for the current table. Hidden columns are still accessible through Show Record and Edit Record.

Use the Extract Regex Matches to Clipboard command, Alt+E (E for Extract), to walk every visible row, run a .NET regex against every visible column, and copy every match to the clipboard one per line. Useful for pulling email addresses, URLs, or IDs out of free-text columns.

Use the Copy Row as TSV to Clipboard command, Shift+A (FileDir's Shift+A = "Append to Clipboard"), to copy the current row's visible columns to the clipboard as tab-separated values for pasting into Excel, Word tables, or chat clients.

Use the Next Initial Change command, Shift+I (FileDir's Shift+I = "Initial Change"), to jump to the next row whose value in a chosen column starts with a different first letter. The column picker defaults to the column under virtual focus.

Use the Run SQL command, Control+Q (Q for Query), to run any SQL statement. SELECTs display the result as a new recordset; INSERT/UPDATE/DELETE/DDL run via ADO Connection.Execute. The dot prompt's `;` and `*` aliases map to the same command.

Use the Test Integrity command to run an integrity probe on the open database (`PRAGMA integrity_check` for SQLite, equivalents for other providers). Use the Test Drivers command to print which ODBC and OLE DB providers Windows currently has registered, useful when troubleshooting a failed Open Database.

Use the Open in Explorer command, Alt+Backslash (the backslash key evokes Windows paths), to open Explorer at the database file's folder with the file pre-selected.

Use the Open Dot Prompt command, Control+GraveAccent, to open or focus the dot prompt console from the GUI.

Use the Invoke Snippet command, Alt+V (V for inVoke), to pick and run one of your saved snippets. Use the Edit Snippet command, Alt+Shift+V, to edit an existing snippet or create a new one. Use the Open Snippet Folder command (no hotkey) to launch Explorer at the snippet folder. See the Scripting with JScript .NET snippets section below for the full reference.

Use the Edit Configuration command, F12 (the Windows-standard "settings" key), to open DbDuo.ini in your default editor for hotkey customizations and connection-string overrides.

## SQL reference: what Invoke-Sql actually runs

A common question worth answering precisely: when you press Control+Q for Run SQL, what SQL dialect does the database engine understand? The answer depends entirely on which kind of file you have open, because DbDuo uses three different drivers under the ADO ConnectionString and each one parses SQL differently.

### A portable SQL baseline that works everywhere

Most users do not need a SQL deep-dive. The basics — selecting rows, inserting rows, updating values, deleting rows — work across every database format DbDuo opens (SQLite, Access, Excel-as-queryable, dBASE, CSV/TSV-as-readable) when you stay inside a careful ANSI SQL-92 subset. If you write SQL that fits the patterns below, the same statement will run unchanged against any of these formats, and you will rarely hit a dialect difference.

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

The portable subset includes: `SELECT` with `WHERE`, `ORDER BY`, `GROUP BY`, `HAVING`, `DISTINCT`, `LIMIT` (SQLite) or `TOP` (Access — these are NOT interchangeable); `INNER JOIN` and `LEFT JOIN` on equality conditions; `COUNT`, `SUM`, `AVG`, `MIN`, `MAX`; the comparison operators `=`, `<>`, `<`, `>`, `<=`, `>=`; the boolean connectives `AND`, `OR`, `NOT`; `IS NULL` and `IS NOT NULL`; `BETWEEN` and `IN`; the simple `LIKE` patterns (but see the wildcard note below); `INSERT INTO ... VALUES (...)`; `UPDATE ... SET ... WHERE ...`; `DELETE FROM ... WHERE ...`; `CREATE TABLE` with `INTEGER`, `TEXT`, `REAL`, `DATETIME` types; and `DROP TABLE`.

**Watch for these portability gotchas** when you cross between SQLite and the Access/Excel/dBASE side:

- **LIKE wildcards differ.** SQLite uses ANSI wildcards `%` (any number of characters) and `_` (one character). Jet/Access uses `*` and `?` in its default mode. When you're writing portable SQL, prefer the ANSI form — and if it fails on an Access file, the legacy Jet wildcard set is what's expected.
- **String concatenation differs.** SQLite uses `||` (ANSI); Jet uses `&` (Microsoft) or `+`. Neither dialect accepts the other's operator.
- **Date literals differ.** SQLite uses ISO strings: `'2026-01-15'`. Jet uses pound-sign delimiters: `#1/15/2026#`. There is no portable date literal short of building the value with the engine's own date functions.
- **Boolean literals differ.** SQLite treats `1` and `0` as boolean (and recent versions accept `TRUE` and `FALSE`); Jet uses `True` and `False`.
- **Row-limiting differs.** SQLite uses `LIMIT N`; Jet uses `SELECT TOP N` at the start of the column list. These are not interchangeable.
- **Identifier quoting differs.** SQLite accepts double-quotes or square brackets around identifiers; Jet prefers square brackets and treats double-quoted text as a string literal in some configurations. If your column or table name has a space or a reserved word in it, square brackets are the more portable choice.

For most uses of Run SQL — quick row counts, ad-hoc filters, occasional updates — the portable subset is more than enough. The dialect-specific subsections below cover the cases when you want to use a feature that does not exist in the portable subset.

**External references:**

- **Microsoft Jet 4.0 / Access SQL reference** (canonical Microsoft documentation for the dialect used with `.mdb`, `.accdb`, `.xlsx`, `.dbf`, `.csv`, and `.tsv` files): <https://learn.microsoft.com/en-us/office/client-developer/access/desktop-database-reference/microsoft-jet-sql-reference>
- **SQLite SQL syntax reference** (canonical SQLite documentation, comprehensive language coverage): <https://sqlite.org/lang.html>
- **SQLite syntax diagrams** (railroad-style diagrams of every statement form): <https://sqlite.org/syntaxdiagrams.html>

### How Invoke-Sql is implemented

DbDuo holds a single ADODB.Connection (created via late-bound COM as `ADODB.Connection`). When you submit a SQL statement through Invoke-Sql, DbDuo passes the text directly to the connection. The execution path depends on whether the statement returns rows.

For data-returning statements (anything starting with SELECT, plus WITH/CTE queries, PRAGMA queries on SQLite, and a few others), DbDuo opens a fresh ADODB.Recordset on the SQL text, with adOpenStatic + adLockReadOnly + adCmdText cursor and lock flags. The recordset is then enumerated row by row through dynamic late binding: `while (!oRs.EOF) { for each field in oRs.Fields: read oRs.Fields[i].Value; oRs.MoveNext(); }`. Each row's cells are pushed into a List<List<string>>; the column names are pulled from `oRs.Fields[i].Name`. The result is presented in a multi-line dialog (or printed to the console at the dot prompt) as a fixed-width grid. The returned data is fully captured in managed C# memory — it does not have to be displayed as a recordset to be useful; the same code path supports redirecting the result to a file via the `tee` / `output` aliases in the CLI.

For action queries (INSERT, UPDATE, DELETE, CREATE TABLE, DROP TABLE, CREATE INDEX, REINDEX, VACUUM, ALTER TABLE, ATTACH, DETACH, BEGIN, COMMIT, ROLLBACK, PRAGMA-as-set, anything that doesn't return rows), DbDuo calls `oConn.Execute(sql, out iAffected, adExecuteNoRecords | adCmdText)` instead. The `iAffected` out-parameter receives the count of rows touched, which DbDuo announces in the status bar ("3 rows updated"). No recordset is opened, no result grid is shown.

Invoke-Sql does not auto-detect which path to take from the SQL text; it tries the SELECT path first and falls back to Execute on a known error. This means even slightly unusual statements like `INSERT ... RETURNING ...` (SQLite 3.35+) or `WITH ... DELETE` (a recursive CTE wrapping a DELETE) get the chance to surface their result rows when the engine produces them.

The ADO recordset captured by Invoke-Sql is independent of DbDuo's main data list. The main recordset (which the F4 picker selected) keeps its position, filter, and sort. Invoke-Sql results are a separate view shown in a popup or printed inline.

### SQLite: full modern SQL

When the open file is .db / .sqlite / .sqlite3, DbDuo connects through the ch-werner SQLite ODBC driver. This driver is a thin pass-through to the SQLite library itself (currently SQLite 3.43+ in the bundled installer). There is no SQL translation layer: whatever the library version supports, you can type into Invoke-Sql.

That means the full modern SQLite SQL surface is available. The basics — SELECT / INSERT / UPDATE / DELETE / CREATE TABLE / DROP TABLE / CREATE INDEX / CREATE VIEW / CREATE TRIGGER / ALTER TABLE / VACUUM / REINDEX / ANALYZE — all work. So do the modern features that some people don't realize SQLite supports: window functions with the OVER clause (since 3.25), including row_number(), rank(), dense_rank(), percent_rank(), cume_dist(), ntile(), lag(), lead(), first_value(), last_value(), nth_value(), and ordinary aggregates used as window functions; the EXCLUDE clause; GROUPS frame types; window chaining; and PRECEDING/FOLLOWING boundaries in RANGE frames (since 3.28). Common Table Expressions (WITH) including recursive CTEs work. UPSERT (`INSERT ... ON CONFLICT ... DO UPDATE`) works. The JSON1 functions (json_extract, json_each, json_array, json_object, json_group_array, json_set) work. The RETURNING clause on INSERT/UPDATE/DELETE works (since SQLite 3.35). FTS5 full-text search works if the database was built with FTS5 tables. R-Tree, generated columns, partial indexes, expression indexes — all work.

PRAGMA statements work both as data-returning queries (when they're shaped like `PRAGMA table_info(foo)`, which returns a result set DbDuo will render as a grid) and as setters (when they're shaped like `PRAGMA journal_mode = WAL`, which DbDuo runs through Execute). PRAGMAs are how you inspect schema metadata and how you adjust performance characteristics like journal mode, cache size, and foreign-key enforcement.

The few things SQLite genuinely lacks compared to PostgreSQL or SQL Server: there are no stored procedures (you use views and triggers instead), no row-level security, no native UUID type (use TEXT or BLOB), and only a limited ALTER TABLE (ADD COLUMN works since forever, RENAME COLUMN since 3.25, DROP COLUMN since 3.35 — but only one column at a time and with some restrictions on referenced columns).

Sample SQLite queries that work in Invoke-Sql against `sample.db`:

```sql
-- Recursive CTE walking a hierarchy
WITH RECURSIVE child_of(id, name, depth) AS (
  SELECT id, full_name, 0 FROM teachers WHERE id = 1
  UNION ALL
  SELECT t.id, t.full_name, c.depth + 1
  FROM teachers t JOIN child_of c ON t.mentor_id = c.id
)
SELECT * FROM child_of;

-- Window function: rank students within each class
SELECT class_id, student_id, grade,
       RANK() OVER (PARTITION BY class_id ORDER BY grade DESC) AS class_rank
FROM enrollments;

-- JSON1: extract a field from a JSON column
SELECT id, json_extract(metadata, '$.address.city') AS city
FROM students WHERE json_extract(metadata, '$.active') = 1;

-- UPSERT
INSERT INTO students (id, full_name) VALUES (42, 'New')
ON CONFLICT (id) DO UPDATE SET full_name = excluded.full_name;

-- PRAGMA returning a result set
PRAGMA table_info(students);
```

All of these return rows that Invoke-Sql renders in the result grid.

### Access (.mdb / .accdb): Jet/ACE SQL

When the open file is .mdb or .accdb, DbDuo connects through the Microsoft ACE OLEDB provider (the modern Jet replacement, which Office installs as part of Office or via the standalone Access Database Engine redistributable). This driver runs **Access SQL**, not standard SQL. The differences from SQLite or ANSI SQL are substantial and worth knowing.

Access SQL uses `IIF(condition, then, else)` instead of `CASE WHEN ... THEN ... ELSE ... END` (although Access did finally add CASE in recent versions, IIF is the older idiom). String concatenation is `&` not `||`. Wildcards in LIKE are `*` (zero or more characters) and `?` (single character), not the SQL-standard `%` and `_`. Date literals are delimited with hash marks: `#2025-05-12#` not `'2025-05-12'`. Booleans are TRUE and FALSE (or -1 and 0); SQLite-style 1/0 won't auto-cast.

Access SQL supports SELECT, INSERT, UPDATE, DELETE, CREATE TABLE, DROP TABLE, CREATE INDEX, ALTER TABLE. It supports inner and outer joins, GROUP BY, HAVING, ORDER BY. It does NOT support CTEs (no WITH clause), window functions (no OVER clause), or RETURNING. It has a different set of built-in functions: Format(), DateSerial(), DateDiff(), DatePart(), Nz() (the null-coalesce, equivalent to COALESCE for a single null check), Iif(), and a few hundred others mostly drawn from VBA. The PRAGMA family doesn't exist; equivalent metadata comes from system tables like MSysObjects (which is typically hidden and requires admin rights to query).

Sample Access SQL queries:

```sql
-- IIF instead of CASE
SELECT id, full_name, IIF(grade >= 90, 'A', IIF(grade >= 80, 'B', 'C')) AS letter
FROM students;

-- LIKE with Access wildcards
SELECT * FROM students WHERE full_name LIKE 'Sm*';

-- Date literal with hash delimiters
SELECT * FROM enrollments WHERE updated >= #2024-01-01#;

-- Concatenation with &
SELECT first_name & ' ' & last_name AS full_name FROM teachers;

-- Aggregate with TOP (Access's LIMIT)
SELECT TOP 10 class_id, COUNT(*) AS n FROM enrollments
GROUP BY class_id ORDER BY n DESC;
```

The hardest gotcha is the wildcard difference: queries copy-pasted from a SQLite tutorial often use `%` and `_` and silently match nothing in Access. DbDuo doesn't translate; what you type is what runs.

### Excel (.xlsx / .xls): a subset of Jet SQL

Excel files open through the same ACE OLEDB provider as Access, with `Extended Properties="Excel 12.0 Xml;HDR=Yes;IMEX=1"` appended. The SQL surface is a deliberately restricted subset of Jet SQL — the engine treats each worksheet as a table, with the sheet name (followed by a `$`) acting as the table identifier. SELECT, basic WHERE, GROUP BY, ORDER BY, and joins between sheets in the same file all work. Most INSERTs and UPDATEs work, though there are quirks around named ranges and protected sheets. CREATE TABLE creates a new sheet; DROP TABLE removes a sheet. No CTEs, no window functions, no triggers (Excel doesn't have triggers as a concept).

Worksheet names with spaces or special characters must be bracketed: `SELECT * FROM [Sheet 1$]`. A named range or a specific range is selectable with `SELECT * FROM [Sheet1$A1:D100]`. The HDR=Yes flag treats the first row as column headers; if your sheet doesn't have headers, set HDR=No in the connection string and refer to columns as F1, F2, F3 (which the engine auto-names).

Sample Excel SQL:

```sql
-- Read everything from a sheet
SELECT * FROM [Students$];

-- Range subset
SELECT * FROM [Students$A1:D50] WHERE grade > 80;

-- Join across two sheets in the same workbook
SELECT s.full_name, e.class_id
FROM [Students$] AS s INNER JOIN [Enrollments$] AS e ON s.id = e.student_id;
```

The biggest practical limit with Excel is that the engine is much slower than SQLite for non-trivial queries (each cell is a string-typed Variant that has to be coerced), and it sometimes guesses column types from the first 8 rows in a way that surprises you. If you have a numeric column where the first 8 rows happen to be empty, the engine may type the column as text and refuse to compare it numerically. IMEX=1 mitigates this but doesn't fix it entirely.

### dBASE (.dbf): minimal SQL

dBASE files open through ACE OLEDB with `Extended Properties="dBASE IV;HDR=No;IMEX=1"`. The connection is to the folder containing the .dbf, not the file itself; each .dbf in the folder becomes a table. The SQL surface is the smallest of the four backends: SELECT with WHERE, ORDER BY, GROUP BY, basic INSERT, UPDATE, DELETE, simple CREATE TABLE, DROP TABLE. No CTEs, no window functions, no joins more complex than INNER JOIN, no triggers. dBASE field naming rules apply: identifiers max 10 characters, no spaces or hyphens, only A-Z 0-9 and underscore.

```sql
-- Simple SELECT against a .dbf in the connected folder
SELECT * FROM students WHERE grade > 80 ORDER BY full_name;

-- INSERT (subject to dBASE field-length rules)
INSERT INTO students (id, full_name) VALUES (99, 'New Student');
```

dBASE is a legacy format and DbDuo supports it primarily so people with old data files can read them. New data should generally go into SQLite.

### CSV and TSV: SELECT-only Jet text driver

When the open file is .csv or .tsv, DbDuo connects through the Jet text driver (`Microsoft.ACE.OLEDB.12.0` with `Extended Properties="text;HDR=Yes;FMT=Delimited"` for CSV, or `FMT=TabDelimited` for TSV). The connection is to the folder containing the file. The driver is read-mostly: SELECT works well, INSERT works in a limited fashion (the engine appends new lines but cannot easily UPDATE in place or DELETE rows), and structural commands like CREATE TABLE or ALTER TABLE don't apply (you'd just save a new CSV instead).

The most useful pattern is to read a CSV with Invoke-Sql for analysis, and use Import-Data to copy into SQLite for any non-trivial work.

```sql
-- Read a CSV as a result set
SELECT * FROM [students.csv] WHERE grade > 80;
```

### How DbDuo captures the result in C#

For SELECTs, the result-capture loop in C# looks essentially like this (paraphrased from the actual code in `cmdInvokeSql` / `invokeSqlClicked`):

```csharp
dynamic oRs = oConn.Execute(sSql);   // returns Recordset for SELECT
if (oRs == null) return;
int iFieldCount = (int)oRs.Fields.Count;
List<string> lHeaders = new List<string>();
for (int i = 0; i < iFieldCount; i++)
    lHeaders.Add((string)oRs.Fields[i].Name);
List<List<string>> aaRows = new List<List<string>>();
while (!oRs.EOF)
{
    List<string> lRow = new List<string>();
    for (int i = 0; i < iFieldCount; i++)
    {
        object oV = oRs.Fields[i].Value;
        lRow.Add(oV == null || oV == DBNull.Value ? "" : oV.ToString());
    }
    aaRows.Add(lRow);
    oRs.MoveNext();
}
oRs.Close();
```

The result is a fully materialized in-memory grid (a List<List<string>>), so anything Invoke-Sql can show you, the rest of DbDuo can also operate on (count, export to clipboard, export to file). The dot prompt `tee filename.tsv` redirection captures the same grid as a tab-separated file; the `output filename.tsv` redirection does the same. The grid is text-typed at the C# layer regardless of the underlying SQL types — DbDuo's accessibility-first design treats every cell as a screen-readable string, and conversions to numeric or date types happen at display time, not at fetch time.

### Practical recommendation

For non-trivial analytical SQL — anything involving CTEs, window functions, recursive queries, JSON extraction, or modern SQL features — open the data in SQLite (either by saving a SQLite file directly or by using Import-Data from another format) and use Invoke-Sql against that. SQLite gives you the most expressive SQL of the four backends, the fastest engine, and the smallest set of dialect surprises. Reserve Access SQL queries for genuine .accdb files; Excel SQL for genuine .xlsx exploration; dBASE for legacy data import. CSV and TSV are best used for read-only intake into SQLite.

## Scripting with JScript .NET snippets

DbDuo's snippet feature lets you write small JScript .NET scripts that run inside the DbDuo process with full access to the running form and recordset. The pattern is the same one EdSharp pioneered: a folder of plain text files; you write them in your own editor; you invoke them from a standard listbox dialog. No custom script editor inside DbDuo, no shipped scripting runtime — just one ~10 KB support DLL (`dbDuoEval.dll`) compiled at build time from `dbDuoEval.js` by `jsc.exe`, the JScript .NET compiler that ships with every .NET Framework 4.x install.

### Where snippets live

Snippets are plain files under `%APPDATA%\DbDuo\Snippets\`. The folder is created on first access; it lives under your roaming application data so it survives DbDuo upgrades and uninstalls. Snippets are your data, not the application's. You can copy the folder between machines, version it in your own git repo, or share individual files with other users.

### File types

Files ending in `.js` are executed as JScript .NET. Files with any other extension (`.txt`, `.sql`, `.md`, etc.) are shown as plain text in a MessageBox; useful for canned SQL fragments, templated row data, or reference notes. The file extension is the only thing that decides whether DbDuo treats a file as a script or as reference text.

### The three commands

Use the Invoke Snippet command, Alt+V (V for inVoke), to pick and run a snippet. A standard listbox dialog shows every file in the folder, sorted alphabetically. Pick one, press OK, and DbDuo runs it. Script output (the last expression's value, or `ERROR: ...` on failure) is shown in a MessageBox.

Use the Edit Snippet command, Alt+Shift+V, to edit an existing snippet or create a new one. If the folder has existing snippets the same listbox appears with a `[New snippet...]` entry at the top; picking that brings up a standard Save File dialog to name the new file. If the folder is empty the Save File dialog appears directly. Either way the chosen file opens in your editor.

Use the Open Snippet Folder command (no hotkey by default) to launch Explorer at the snippet folder. Useful for renaming, deleting, or copying files outside DbDuo's UI.

### Editor

The default editor is Notepad. To override, put a line in `DbDuo.ini`:

```ini
[Snippets]
editor = C:\Program Files\Notepad++\notepad++.exe
```

Any text editor that accepts a file path as a command-line argument works. EdSharp, Notepad++, gedit, Visual Studio Code, and so on.

### What a JScript snippet sees

Inside the script's eval scope, two pre-injected variables are visible:

- `frm` — the running `DbDuoForm`. Every public method and property of the form is reachable.
- `db` — shortcut for `frm.db` (the `DbDuoManager`). Every public method and property of the recordset manager.

The dbDuoEval.dll support module pre-imports `System`, `System.Collections`, `System.Data`, `System.IO`, `System.Reflection`, `System.Text`, `System.Text.RegularExpressions`, and `System.Windows.Forms`. Snippets can use any type in those namespaces directly without their own `import` statements. JScript's late-bound dispatch resolves member access at runtime, so snippets don't need to know exact .NET method signatures — just write `db.recordCount`, `frm.refresh()`, etc.

### What a snippet returns

The last expression of the snippet is the value DbDuo gets back. If it's a string, that string is shown in the MessageBox. If it's a number or other type, `.ToString()` is called. If the snippet has no value (only side effects), the MessageBox shows `(no output)`. To produce multi-line output, build a string with newlines and let it fall through as the final expression.

### Errors

Compile-time errors (syntax, unknown identifier, type mismatch) and runtime errors (null reference, division by zero, etc.) are both caught and returned to DbDuo as a string starting with `ERROR:`. The MessageBox shows this with the error icon. The script never throws out to DbDuo, so the UI stays responsive — fix the script and Invoke again.

### Sample snippets

Verify scripting works:

```javascript
"Current table: " + db.currentTable + ", " + db.recordCount + " rows.";
```

Count rows then change the filter:

```javascript
var iBefore = db.recordCount;
db.filter = "City = 'Seattle'";
frm.refresh();
"Filtered from " + iBefore + " to " + db.recordCount + " rows.";
```

Walk every row and collect first-column values (capped at 20 lines for the MessageBox):

```javascript
var aLines = [];
db.moveFirst();
while (!db.EOF) {
  aLines.push(db.getFieldValue(0));
  db.moveNext();
}
aLines.slice(0, 20).join("\n");
```

Trigger a form action:

```javascript
frm.recBookmarkClicked(null, null);
"Bookmark saved at row " + db.absolutePosition;
```

### Power and responsibility

Snippets run in the DbDuo process with all the privileges DbDuo has. There is no facade or sandbox. A snippet can call `Environment.Exit`, read or write files, launch other programs, modify the database. This is intentional for power-user automation; treat snippets the same way you would treat shell scripts or PowerShell scripts you run on your own machine.

## Help menu

Use the Help Contents command, F1 (the standard help key), for help. With no argument, F1 shows the command index; from the dot prompt, `help <topic>` shows details for one command.

Use the PowerShell Verb Reference command (no hotkey) to see the PowerShell verb taxonomy with each verb's category and a brief description, so you recognize the naming conventions DbDuo follows. Use the Command Picker command, Alt+F10 (F10 = menu, Alt for "alternative menu"), to open an alphabetized list of every command with its current hotkey and a one-line description. The Alt+F10 chord echoes the EdSharp and FileDir convention.

Use the Where Am I command (no hotkey by default) to hear the current row, table, filter, and sort state in detail. Use the Test Screen Reader Speech command to probe DbDuo's three speech paths (JAWS direct via COM, NVDA direct via the controller-client DLL, and the UIA live-region fallback for Narrator) and confirm which one is working with your screen reader.

Use the Toggle Key Describer Mode command, Control+F1 (F1 for help, Control for "describe rather than fire"), to switch into a mode where every hotkey press announces the chord and its bound command instead of running it; press Control+F1 again to leave the mode. Use the Show Log Location command to print the path of `DbDuo.log`, the per-session log file. Use the Version History command, Shift+F1, to read the chronological list of releases and what changed in each. Use the Readme Guide command to open `README.md` in your browser. Use the Open Website command to open the DbDuo GitHub page. Use the Check for Update command, F11, to ask GitHub for the latest release and offer to download and install it. Use the About DbDuo command, Alt+F1, to read the version number and a brief credits block.

## Commands available only at the dot prompt

A few commands have no GUI counterpart because they manage the dot prompt itself.

Use the Exit-Console command, `exit` (or `x` or `bye`), to leave the dot prompt while the GUI keeps running. Use the Switch-Focus command, `gui` (or `focus` or `window`), to bring the GUI forward from the console.

Use the Out-File command, `Out-File path.txt` (with aliases `output`, `tee`, and `o`), to send subsequent output to a file while also keeping it on screen. The `-a` flag appends rather than overwriting; `Out-File stdout` restores the screen-only behavior; bare `Out-File` reports the current target. The simultaneous screen-and-file teeing lets the screen-reader user follow what is being captured.

Use the Invoke-Script command, `Invoke-Script path.txt` (with aliases `read`, `script`, and `i`), to run a file of dot-prompt commands. Blank lines and lines beginning with `#` or `;` are treated as comments. Errors are reported per line but do not stop the script. Out-File and Invoke-Script combine well:

```
Out-File monthly_report.txt
Invoke-Script monthly_report.dbduo
Out-File stdout
```

## Mnemonic hotkey groups

This section restates every hotkey in the program, grouped by the part of the keyboard it lives on. Use it as a quick visual reference.

### Bare Shift+Letter family

Five one-key shortcuts fire from the data list only (so capital letters typed in dialogs are not affected): Shift+F filters (Filter Records); Shift+G goes to a row (Go to Row); Shift+J as a synonym for Jump to Match in One Column; Shift+R resets the filter (Clear Filter); Shift+S sorts by an arbitrary expression (Custom Sort). The previously-bound Shift+E (Enter-Child), Shift+M (Mark), Shift+U (Unmark), and Shift+X (Exit-Child) chords are no longer in this family — the parent-child drill moved to the Alt+arrow chord pair, and the mark/unmark pair moved to Control+M / Control+U for symmetry, freeing the bare Shift+E, M, U, and X slots for future use.

### Function-key family

F1 is help (Help Contents); Shift+F1 is Version History; Alt+F1 is About DbDuo; Control+F1 is Toggle Key Describer Mode. F2 edits the current row (Edit Record). F3 is search next (repeats whichever search family was last invoked: Find, Jump, or Regex); Shift+F3 is search previous. Control+F3 is Find Regex Across All Columns; Control+Shift+F3 is the reverse. F4 picks a table (Choose Table); Shift+F4 is Say Tables; Control+F4 closes the open file (Close Database). F5 refreshes (Refresh View) and resets the virtual cursor. Control+F6 cycles all objects (Next Object); Control+Shift+F6 cycles backward. Control+F7 toggles the lock (Toggle Read-Only Lock). F11 checks for an update (Check for Update). Alt+F4 closes the program (Exit DbDuo). Alt+F10 opens the Command Picker. F12 opens Edit Configuration.

### Control-letter family

Control+C is reserved for native clipboard. Control+D deletes the current row (D for Delete). Control+F is Find Across All Columns; Control+Shift+F is the reverse. Control+J is Jump to Match in One Column; Control+Shift+J is the reverse. Control+K saves a bookmark; Control+Shift+K clears it. Control+M marks the current row; Control+U unmarks it. Control+N adds a row (N for New); Control+Shift+N creates a new database. Control+O opens a file (O for Open). Control+P prints. Control+Q runs SQL (Q for Query). Control+R is Find and Replace Across Rows. Control+S saves the database to a new path; Control+Shift+S takes a backup snapshot. Control+Shift+C duplicates the current row. Control+Shift+I imports a Markdown table; Control+Shift+X exports data. Control+Enter is Open Cell Value.

### Alt-letter family

Alt+A sorts ascending; Alt+Shift+A sorts descending. Alt+C is an alias for Frequency Chart. Alt+D sorts by date oldest first; Alt+Shift+D sorts most recent first. Alt+E is Extract Regex Matches to Clipboard. Alt+K is Go to Bookmark. Alt+L is an alias for Choose Table. Alt+P is Say Path. Alt+R is Recent Files; Alt+Shift+R is Related Records. Alt+T is an alias for Table Statistics. Alt+Y is Say Yield. Alt+Z is Say Status. Alt+Enter is Table Properties. Alt+Backslash is Open in Explorer.

### Alt+Control extended-key family (virtual cell navigation)

This is the screen-reader table-navigation family. The Alt+Control combination is reserved for desktop global hotkeys elsewhere in Windows, but extended-arrow / numpad keys don't make sense as global hotkeys, so DbDuo uses this combination for cell-level navigation inside the data list.

Alt+Control+Home moves to the top-left cell; Alt+Control+End moves to the bottom-right. Alt+Control+RightArrow / LeftArrow / DownArrow / UpArrow move one cell in the named direction. Alt+Control+PageDown moves to the last row of the current column; Alt+Control+PageUp moves to the first row. Alt+Control+Numpad5 announces the current cell, or spells it on a second press.

### Alt+arrow family (parent-child drill)

Alt+RightArrow drills into a child table (Enter Child Table). Alt+LeftArrow returns to the parent row (Exit Child Table). Alt+Home pops the entire drill stack (Exit to Root Table).

### Navigation family

Tab and Shift+Tab move an announcement-only column cursor across the current row — the screen reader announces "Column: value" without changing the visible selection. The arrow keys move the listview's row selection. Enter opens Show Record on the current row. Control+Tab cycles among recently-visited tables; Control+Shift+Tab cycles backward. Control+Home and Control+End jump to the first or last marked row; Control+UpArrow and Control+DownArrow step among marked rows. Shift+Home and Shift+End bulk-mark every row from the first through the current, or the current through the last; Alt+Shift+Home and Alt+Shift+End unmark the same spans.

### GraveAccent family

Control+GraveAccent is the GUI menu hotkey for Open Dot Prompt. Alt+GraveAccent is a global hotkey: when the console has focus, it brings the GUI forward. Alt+Control+GraveAccent is a global hotkey that always acts: it toggles between GUI and console, whichever is not currently in front.

## Standard fields

DbDuo follows a convention for table design that the bundled `sample.db` illustrates and that the user manual recommends for new databases. Each table has the following "standard fields" in this order, with the "distinct fields" (the substantive columns) interleaved among them:

1. `<table>_id` — the primary key, integer, autoincrement.
2. `added` — datetime, default `current_timestamp`. When the row was created.
3. `updated` — datetime, default `current_timestamp`. Most recent change.
4. Foreign-key columns (`<parent>_id`, for child tables only).
5. `observed` — datetime, default `current_timestamp`. When the data behind the row was observed (often different from when it was entered).
6. `method` — text. How this row was added or observed (e.g., "import", "manual", "scrape").
7. **Distinct fields** — the substantive columns this table is actually for.
8. `notes` — text. Free-form annotations.
9. `tags` — text. Comma-separated tag list for ad-hoc grouping.
10. `marked` — boolean, default 0. The flag the Set-Mark command toggles.
11. `look` — computed text. A pipe-joined rendering of the most identifying distinct fields, designed for screen-reader readability. Appears in listboxes, quick-search displays, and Show-Object's Related Records section.
12. `unq` — computed text (optional). Like `look` but optimized for uniqueness rather than readability. Used by upsert-style imports as a natural deduplication key.

The `look` column is what makes Show-Object's Related Records section informative. When DbDuo lists "Related students:" or "Related classes:", each line is one matching row's `look` value, so a single short string identifies who or what each related record is. Tables without a `look` column still show up under the right header but with a `(N row(s) -- no look column)` placeholder.

DbDuo hides every column ending in `_id` (primary and foreign keys), bare `id`, and the bookkeeping columns (added, updated, marked, look, unq) from the list by default. Use the Select-Column command to override on a per-table basis.

## Persistence

Between sessions, DbDuo remembers the last opened database and table (relaunch goes straight there), the last folder used for each kind of file dialog (open/save/import/export remembered separately), per-table sort/filter/position/Select-Column lists within a session (the `TableSettings` cache), and one named bookmark per session. Settings live in `%LOCALAPPDATA%\DbDuo\DbDuo.ini`. Successful and failed writes are logged.

The shipped `DbDuo.ini` next to the executable holds configuration that ships with the install: `[General]` for `uiMode`, `[Keys]` for hotkey overrides. The per-user file in `%LOCALAPPDATA%\DbDuo\` holds session state: `[Session]` for last-opened, `[Folders]` for remembered directories. The two files coexist; per-user values take precedence.

## Logging

DbDuo writes a per-session log to `%LOCALAPPDATA%\DbDuo\DbDuo.log` (truncated at every program start). The log records database opens, table switches, errors, and the result of Alt+GraveAccent and Alt+Control+GraveAccent hotkey registration. Use the Show-Log command (no hotkey) to print the exact path.

## Bundled documentation

- `README.md` and `README.htm` — summary and quick start with a guided tour of `sample.db`.
- `DbDuo.md` and `DbDuo.htm` — this reference.
- `License.md` and `License.htm` — MIT License text.
- `sample.db` — the bundled SQLite sample database (teachers, classes, students, enrollments).

# Development

This section is for anyone building DbDuo from source or extending it.

## Requirements

DbDuo is a single-file C# program targeting .NET Framework 4.8 on Windows x64. Compilation needs `csc.exe` from the .NET Framework 4.8 developer pack (Microsoft ships this in standalone form for free) — no Visual Studio required. Inno Setup 6 is used to build the installer. Pandoc is used to render the Markdown docs to HTML at release time. None of these tools needs to be on the user's machine; they live in the build environment only.

## Build steps

```
buildDbDuo.cmd
```

This single batch file compiles `DbDuo.cs` to `DbDuo.exe` with the right `csc.exe` flags: `/target:winexe`, `/platform:x64`, `/optimize+`, references to `System.Windows.Forms.dll`, `System.Drawing.dll`, and the Microsoft accessibility assembly. The build script auto-locates `csc.exe` by walking the .NET Framework install path; if your developer pack is in a non-standard location, edit the `set csc_path=` line at the top.

```
"%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" DbDuo_setup.iss
```

This compiles the installer. `DbDuo_setup.iss` is a vanilla Inno Setup script with a five-page wizard (Welcome → SelectDir → Ready → Installing → Finish), a `CurStepChanged` hook that fetches the SQLite ODBC and ACE drivers on the Installing step, and `[Files]` entries for the eight bundled artifacts. The output is `DbDuo_setup.exe` in the project root.

## Release process

```
tagRelease.cmd
```

Runs the bundled `tagRelease.ps1`, which: checks for `git` and `gh` on PATH; verifies `gh auth status`; reads the version from `DbDuo_setup.iss`; checks for uncommitted changes (ignoring its own log file); creates the `v<version>` git tag and pushes it; calls `gh release create v<version> DbDuo_setup.exe --generate-notes --latest`; and HEAD-checks `https://github.com/JamalMazrui/DbDuo/releases/latest/download/DbDuo_setup.exe` to confirm the asset is downloadable. The log goes to `tagRelease-<timestamp>.log` in the current directory.

The release URL `releases/latest/download/DbDuo_setup.exe` is stable: every new release with that asset inherits the URL automatically, so the documentation never has to be updated for a version bump.

## Architecture: one connection, two interfaces

The GUI form (`DbDuoForm`) and the dot-prompt CLI (`Program.cmdXxx` static methods) share one `DbDuoManager` instance. The manager owns the single `ADODB.Connection` and the active `ADODB.Recordset`. Both interfaces call manager methods (`openDatabase`, `selectTable`, `applyFilter`, etc.) rather than working directly with ADO objects. When an edit is committed in either interface, the manager's notification path tells the form to refresh its list view; the next dot-prompt command sees the new state automatically because it queries the same connection.

`CursorLocation = adUseClient` (3) is set on every Connection.Open. This means filter, sort, and bookmark operations happen client-side after the recordset is fetched. The benefit is that the same code path works against SQLite, Access, Excel, and dBASE without per-provider quirks. The cost is that `.Filter` does not push down to the database engine — see the SQLite indexing discussion below.

## ADO client cursor and SQLite indexing

A natural question is whether adding indexes to foreign-key columns in a SQLite database would speed up Enter-Child's drill-down. The answer is no, given DbDuo's current implementation.

Enter-Child works by opening a fresh recordset on the child table with bare `SELECT * FROM <child>`, then calling `.Filter = "<fk> = <value>"` on the resulting client-cursor recordset. Microsoft's ADO documentation states that the Filter property on a client-side cursor "operates entirely in client memory" — the full result set is already in the recordset's in-memory copy when the SELECT returns, and Filter just hides non-matching rows from view. SQLite's query planner never sees the FK predicate, so any index on the FK column is unused.

For Show-Object's Related Records section, by contrast, DbDuo uses `DbDuoManager.queryColumnValues` which builds a real `SELECT look FROM <child> WHERE <fk> = <value>` SQL statement and executes it server-side. This path does benefit from FK indexes if they exist in the schema. The difference matters: at one row per Show-Object invocation, the WHERE-clause path is essentially free regardless of child-table size; the client-cursor path is O(total rows in child table) every time.

A future optimization would be to convert Enter-Child to the WHERE-clause approach as well — open the child recordset with `WHERE <fk> = <value>` instead of bare SELECT. This would make Enter-Child fast on large child tables, but would require careful handling of how subsequent Set-Record edits and Update-View round-trip through the bounded recordset.

## Office automation: always CreateObject, never GetActiveObject

DbDuo never attaches to a running Word or Excel instance via `Marshal.GetActiveObject` or `GetObject`. Every Office-using export path (`exportSpreadsheet`, `exportWord`) calls `Activator.CreateInstance` to make a fresh hidden instance, drives it to produce the requested file, then calls `Quit()` and releases the COM object. The reasons:

- Attaching to a running instance would mutate settings the user has tuned for their session — `DisplayAlerts`, `AutomationSecurity`, `ScreenUpdating`.
- Driving a running instance through SaveAs and Quit risks side effects on documents the user has open in their editor.
- The behavior would be non-deterministic depending on whether Word or Excel happens to be running when DbDuo's export runs.

The cost is one Word.exe or Excel.exe process per export. Acceptable.

## Show Record and the look column

The Show Record command's Related Records section uses each child table's `look` column to identify rows. (The canonical PowerShell-style command name is `Show-Object`, available at the dot prompt and as an alias inside DbDuo.) The `look` column is a SQLite stored-generated text column that concatenates a handful of substantive fields with " | " separators — for `sample.db`'s `teachers` table, it's `name || ' | ' || department || ' | ' || office`, yielding strings like `Dr. Ada Lovelace | Computer Science | Babbage Hall 204`. The convention is documented at the top of `DbDuo.cs` in the `Metadata` class.

The companion `unq` column (also a stored-generated text) is similar but optimized for uniqueness rather than readability: it concatenates only the fields that together determine identity, in a form suitable for natural-key deduplication during imports. `unq` is not yet used by any DbDuo command at this writing; it's an extension point for a future Import-Data upsert mode.

`findParentTableForFk` discovers parent tables by inverting the lookup: rather than guessing English plural rules ("class" → "classes", "city" → "cities"), it asks the schema itself which table has the FK column as its primary key. The PK comes from `actualPrimaryKey`, which reads `PRAGMA table_info` for SQLite and `ADOX.Catalog.Tables[t].Keys` for Access. This handles every plural irregularity correctly because it reads the schema's own truth rather than applying heuristics.

## File layout

```
DbDuo.cs             single-file C# source, ~11,000 lines
DbDuo_setup.iss      Inno Setup installer script
buildDbDuo.cmd       compile DbDuo.exe via csc.exe
tagRelease.cmd      Windows wrapper for tagRelease.ps1
tagRelease.ps1      PowerShell release-cutting script
DbDuo.ini            shipped configuration (uiMode, [Keys] overrides)
sample.db            bundled SQLite sample (teachers/classes/students/enrollments)
README.md, README.htm     summary and quick start
DbDuo.md, DbDuo.htm       this reference
License.md, License.htm   MIT License
Announce.md, Announce.htm release announcement
```
