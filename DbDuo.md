# DbDuo Reference

**Manage databases in popular file formats, with synchronized interfaces between CLI and GUI modes, designed to maximize productivity for keyboard users of Windows.**

Version 1.0.20. Source and releases: <https://github.com/JamalMazrui/DbDuo>.

## How DbDuo is organized

DbDuo runs in two interfaces — a WinForms GUI and a dot-prompt CLI — simultaneously by default. Both interfaces drive the same ADO database connection, so a change in one shows up immediately in the other.

The GUI is a single Windows form containing a menu bar, a ListView data grid in the center, and a status bar at the bottom. Standard Windows conventions apply: Alt activates the menu bar; single-letter mnemonics open each top-level menu (File, Record, View, Schema, Tools, Help). Every menu command is reachable through both a hotkey and a click; no command is mouse-only.

The data grid is a virtual-mode ListView in Details view with FullRowSelect on. Arrow keys move between rows; Tab and Shift+Tab move between columns within the current row. Each column move triggers a live-region announcement of the form "ColumnHeader: value" so screen readers name the column you just entered.

The status bar at the bottom carries three items in this order: the word "marked" (only when the current row has its marked flag set), the row position "row N of M", and "updated YYYY-MM-DD" (only when the current row has an updated column). Two spaces separate the sections so the screen reader pauses naturally between them. Use the JAWS Insert+PageDown command to read the whole status bar at once.

The window title is `DbDuo - <database> - <table>`, with `(read-only)` inserted after the database name when the lock is on.

The Shift+F10 context menu in the data grid duplicates the most common record-level commands: Set-Record, Show-Record, Copy-Record, Remove-Record, Set-Mark, Clear-Mark, and Get-Property. Mouse users can right-click to reach the same items.

The CLI is a Windows console window running a dot prompt. Each line is a single Verb-Noun command, optionally with an argument; bare SQL is also accepted directly. The prompt is the current table's name followed by a dot. Use the Get-Help command, F1 from the GUI or `help` at the prompt, to see the command index; `help <command>` shows details for one command. Use the Out-File command, `Out-File path.txt` (output, tee, or `o` are aliases), to capture the next commands' output to a file while keeping it on screen at the same time.

### Switching between modes

Three grave-accent chords coordinate the GUI/CLI relationship. JAWS calls the unshifted key above Tab "GraveAccent."

Use the Enter-Console command, Control+GraveAccent, from the GUI menu to open or focus the dot prompt console. Use Alt+GraveAccent (a global hotkey) from inside the console to bring the GUI forward; this chord acts only when the console has focus, so it does not yank focus from Word or any other application. Use Alt+Control+GraveAccent (also global) from anywhere in Windows to toggle between GUI and console, whichever is not currently in front.

In CLI-only mode these chords work the same way; without a GUI, the "switch to GUI" chords simply have nothing to bring forward.

### Starting DbDuo

Setup creates five Start Menu shortcuts and one Desktop shortcut. The plain **DbDuo** shortcut opens both GUI and console, which is the default mode. Use the Alt+Control+G hotkey on the **DbDuo (GUI only)** shortcut (G for GUI) to launch the GUI without a console; use Alt+Control+L on the **DbDuo (CLI only)** shortcut (L for cLi) to launch the dot prompt without a GUI. The **DbDuo (read-only)** shortcut opens databases with the lock on. The **DbDuo sample database** shortcut opens the bundled `sample.db` directly so you can try DbDuo without your own data.

Use the Desktop hotkey, Alt+Control+D (D for Desktop), from anywhere in Windows to activate a running instance or launch a fresh one. DbDuo is single-instance: a second press of Alt+Control+D wakes the existing window rather than spawning a duplicate.

## Keyboard navigation in the data grid

The ListView in the center of the GUI uses three families of key gestures: arrow keys for row movement, Tab and Shift+Tab for column movement within a row, and any unmodified letter or digit for type-ahead jumping.

Use the arrow keys to step row by row. Use the PageUp and PageDown keys to jump by a screenful at a time. Use Home and End to jump to the first and last row.

Use the Tab key to advance to the next column within the current row, or Shift+Tab to move backward. After each Tab, DbDuo announces the new column with its header and current value, in the form "ColumnHeader: value." Note that Control+Tab and Control+Shift+Tab do something different — those cycle among recently-visited tables rather than columns; DbDuo lets the form-level Switch-Table command handle those rather than the ListView.

Use any unmodified letter key, lowercase or uppercase, to jump to the next row whose current column begins with that letter. Type two or more letters in quick succession to extend the search prefix; for example, typing "dr" on the `teachers` table jumps to the first row beginning with "Dr." (such as "Dr. Ada Lovelace"). The search wraps around at the end of the list and starts over from the top. The comparison is case-insensitive, so lowercase a and capital A both jump to rows starting with A; capital letters are not reserved for any other purpose.

This convention is deliberate. Other accessibility-focused tools — FileDir, for one — assign Shift-letter chords like Shift+D, Shift+L, and Shift+S to menu commands and accept that those specific capital letters cannot be used for type-ahead navigation. DbDuo takes the opposite approach: every capital letter remains a type-ahead key, and every menu hotkey involving a letter uses Control or Alt (often combined with Shift) as the modifier. The trade-off costs DbDuo a few possible one-key chord names, but it preserves the entire alphabet for quickly jumping around large recordsets — a workflow that matters more once a database has a few thousand rows.

## File menu

Use the New-Database command, Control+Shift+N (N for New, Shift to distinguish it from Control+N which makes a new row), to create an empty SQLite database at a chosen path. Use the Open-Database command, Control+O (O for Open), to bring up a file dialog and choose an existing database; DbDuo recognizes `.db`, `.sqlite`, `.sqlite3`, `.mdb`, `.accdb`, `.xlsx`, `.xls`, `.dbf`, `.csv`, `.tsv`, and `.txt` files.

Use the Save-DatabaseAs command, Control+S (S for Save), to write a copy of the open database to a new path and switch DbDuo to the new file. Use the Backup-Database command, Control+Shift+S, to write the same copy but keep the original open (S for Save, Shift to distinguish a backup snapshot from a Save-As). The Close-Database File command, Control+F4 (the MDI close convention), closes the open file without exiting DbDuo.

Use the Import-Data command, Control+Shift+I (I for Import), to bring a delimited file into the open database; for now this is a reserved menu entry, and importing is done by running an Invoke-Sql statement directly. Use the Export-Data command, Control+Shift+X (X for eXport), to write the current view to one or more files. At the dot prompt, `Export-Data` with no argument writes a single xlsx file into the database's folder, named after the current table; `Export-Data xlsx docx html csv` (or the single-letter shortcuts `x d h c`) writes one file of each kind in one call. After each export, DbDuo opens the result in its default Windows application so you immediately hear what was produced. The xlsx and docx formats use Word and Excel through late-bound COM and therefore need Microsoft Office; csv, tsv, and plain HTML work without Office.

Use the Out-Printer command, Control+P (P for Print), to print the current view; this is reserved for a future release. For now, export to HTML or docx and print from the corresponding application.

Use the Exit-Application command, Alt+F4 (the Windows-standard close-program key), to close DbDuo entirely. The dot prompt's `quit` and `q` commands map to Exit-Application as well; `exit`, `x`, and `bye` map to Exit-Console, which leaves the dot prompt but keeps the GUI running.

## Record menu

Every Record-menu command operates on the current row.

Use the New-Record command, Control+N (N for New), to add a row. DbDuo shows an edit dialog with one line per distinct field; bookkeeping fields (`added`, `updated`, `observed`, `marked`, the primary key) get their default values automatically. Use the Set-Record command, F2 (the Windows-standard rename key), to edit the current row. Use the Remove-Record command, Control+D (D for Delete), to remove the current row; the Delete key alone is a secondary binding so the gridlike Excel and Outlook convention works too.

Use the Update-Field command, Control+R (R for Replace), to do a find-and-replace across rows: pick a column, type the find string and the replace string, choose the scope (current row, filtered rows, or all rows), and DbDuo updates every match through ADO so the same triggers fire as for a SQL UPDATE.

Use the Show-Record command, plain Enter, to hear every column of the current row at once. Use the Copy-Record command, Control+Shift+C (Shift turns the native clipboard's Control+C into row-copy), to duplicate the current row as a new record. Use the Open-Cell command, Control+Enter (extends Enter into "open as URL"), to treat the current cell's value as a URL or a file path and open it in its default Windows application; useful when a database column holds links to PDFs, screenshots, or web pages.

Use the Jump-Record command, Control+J (J for Jump), to find a row. Type an ADO Filter expression and DbDuo positions the cursor on the first match without changing the filter; for example, `name LIKE '%bridge%'` or `marked = true`. Use the Jump-RecordAgain command, F3 (the Windows-standard find-next key), to step to the next match; use the Jump-RecordPrevious command, Shift+F3, to step backward.

Use the Set-Mark command, Control+M (M for Mark), to set the boolean `marked` column on the current row; when marked is true, the status bar reads "marked." Use the Clear-Mark command, Control+Shift+M, to clear it. Marks are useful for accumulating an ad-hoc selection across navigation; combine with `filter marked` at the dot prompt to scope subsequent commands.

Use the Set-Position command, Control+G (G for Go-to), to jump to a row number; out-of-range values clamp to the first or last row.

DbDuo offers two navigation directions between related tables. The first goes child to parent: use the Show-Related command (no hotkey, available from the Record menu) when the current row has a foreign-key column like `app_id` and you want to jump to the corresponding parent row in the `apps` table.

The second goes parent to child: use the Enter-Child command, Control+E (E for Enter), to drill from the current parent row into a child table whose schema includes the parent's primary-key column name. If exactly one child table matches, DbDuo opens it directly; otherwise it presents an alphabetized listbox so you can pick. The child table opens with its last-used sort order restored and a filter applied that shows only the rows whose foreign key matches this parent's primary key. The drill stack is unbounded; you can Enter-Child several levels deep. Use the Exit-Child command, Control+Shift+E, to pop back one level. DbDuo restores the parent's sort, filter, and exact row position each time.

Use the Save-Bookmark command, Control+K (K for booKmark), to remember the current row by primary key. Use the Restore-Bookmark command, Alt+K (Alt is the inverse of Save), to return to it later; use the Clear-Bookmark command, Control+Shift+K, to forget it. DbDuo holds one named bookmark per session.

## View menu

Use the Select-Record command, Control+F (F for Filter), to apply a filter expression to the current table. The dialog accepts the same ADO Filter dialect the dot prompt uses; the row count, status bar, and ListView all update together. Use the Reset-Filter command, Control+Shift+F (Shift inverts the Filter), to clear it.

Use the Sort-Ascending command, Alt+A (A for Ascending), to sort by the current column alphabetically; use Alt+Shift+A for Sort-Descending. Use the Sort-OldestFirst command, Alt+D (D for Date), to sort by the table's `updated` column oldest first; use Alt+Shift+D for Sort-RecentFirst. Use the Sort-Object command, Alt+Shift+O (O for Object, the PowerShell noun for a sortable thing), to type an arbitrary ADO Sort expression like `name ASC, year DESC`. Use the Reset-Sort command (no hotkey, View menu) to clear the sort so the recordset returns to its natural order.

Use the Select-Column command (no hotkey, View menu) to choose which columns appear in the ListView for the current table. The dialog accepts a comma-separated list of column names; columns missing from the list are still accessible through Show-Record and Set-Record. By default, DbDuo hides primary-key and foreign-key columns (any column ending in `_id` or named `id`) along with the bookkeeping columns (added, updated, marked, look, unq).

Use the Update-View command, F5 (the browser-standard refresh key), to re-query the database from disk; useful when another tool has written to the file while DbDuo had it open.

## Schema menu

Use the Select-Table command, F4 (F4 = MDI "switch document"), to bring up a listbox of base tables. Use the Select-View command, Shift+F4, for views only. Use the Switch-Table command, Control+Tab, to cycle among recently-visited base tables in most-recently-used order, like Control+Tab cycles tabs in a browser; Control+Shift+Tab cycles backward. Use the Switch-Object command, Control+F6 (F6 = cycle panes), to cycle through all tables and views without the MRU filter; Control+Shift+F6 cycles backward.

Use the Show-Schema command (no hotkey, Schema menu) to print the database's CREATE TABLE and CREATE VIEW statements to a dialog; this is long output, usually called from the dot prompt as `schema`. Use the Get-Property command, Alt+Enter (the Windows-standard properties chord), to open a dialog summarizing the current table: row count, column count, primary key, inferred foreign keys, and any cached settings.

## Tools menu

Use the Test-Database command (no hotkey, Tools menu) to run an integrity probe on the open database (`PRAGMA integrity_check` for SQLite, or the equivalent for the active provider). Use the Measure-Table command (no hotkey) to print row counts and per-column statistics for the current table.

Use the Invoke-Sql command, Control+Q (Q for Query), to run any SQL statement. SELECTs display the result as a new recordset; INSERT/UPDATE/DELETE/DDL run via ADO Connection.Execute. The dot prompt's `;` and `*` aliases map to the same command.

Use the Lock-Database command, Control+F7 (F7 = lock convention), to toggle the recordset between editable and read-only; the window title shows the change. Use the Test-Driver command (no hotkey) to print which ODBC and OLE DB providers Windows currently has registered, useful when troubleshooting a failed Open-Database. Use the Open-FileFolder command, Alt+Backslash (the backslash key evokes Windows paths), to open Explorer at the database file's folder with the file pre-selected.

Use the Enter-Console command, Control+GraveAccent, to open or focus the dot prompt console from the GUI.

## Help menu

Use the Get-Help command, F1 (the standard help key), for help. With no argument, F1 shows the command index; from the dot prompt, `help <topic>` shows details for one command.

Use the Get-Verb command (no hotkey) to see the PowerShell verb taxonomy with each verb's category and a brief description, so you recognize the naming conventions DbDuo follows. Use the Show-Command command, Alt+F10 (F10 = menu, Alt for "alternative menu"), to open a command picker: an alphabetized list of every command, with its current hotkey and a one-line description. The Alt+F10 chord echoes the EdSharp and FileDir convention of an "alternate menu" reachable through the Alt+F10 hotkey.

Use the Show-Status command, Control+F1 (F1 for help, Control for "where am I"), to hear the current row, table, filter, and sort state in detail. Use the Test-Reader command (no hotkey) to probe DbDuo's three speech paths (JAWS direct via COM, NVDA direct via the controller-client DLL, and the UIA live-region fallback for Narrator) and confirm which one is working with your screen reader.

Use the Trace-Command command, Alt+Control+F1 (F1 family for help-related; Alt+Control for "deep trace"), to toggle a mode that logs every command dispatch with its arguments before execution. Use the Show-Log command (no hotkey) to print the path of `DbDuo.log`, the per-session log file (truncated at every startup). Use the About command, Alt+F1, to read the version number and a brief credits block.

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

### Function-key family

F1 is help (Get-Help); Control+F1 is "where am I" (Show-Status); Alt+F1 is identification (About); Alt+Control+F1 is deep-help (Trace-Command). F2 edits the current row (Set-Record). F3 is find next (Jump-RecordAgain); Shift+F3 inverts it (Jump-RecordPrevious). F4 picks a table (Select-Table); Shift+F4 picks a view (Select-View); Control+F4 closes the open file (Close-Database). F5 refreshes (Update-View). Control+F6 cycles all objects (Switch-Object); Control+Shift+F6 cycles backward. Control+F7 toggles the lock (Lock-Database). Alt+F4 closes the program (Exit-Application). Alt+F10 opens the alternate menu (Show-Command).

### Control-letter family

Control+C is reserved for native clipboard, so the ListView's own cell-copy still works. Control+D removes the current row (D for Delete). Control+E enters a child table (E for Enter); Control+Shift+E exits back to the parent. Control+F filters (F for Filter); Control+Shift+F resets the filter. Control+G goes to a row number (G for Go-to). Control+J jumps to a row by criteria (J for Jump). Control+K saves a bookmark (K for booKmark); Control+Shift+K clears it. Control+M sets the mark (M for Mark); Control+Shift+M clears it. Control+N adds a row (N for New); Control+Shift+N creates a new database. Control+O opens a file (O for Open). Control+P prints (P for Print). Control+Q opens the SQL editor (Q for Query). Control+R replaces values across rows (R for Replace). Control+S saves the database to a new path (S for Save); Control+Shift+S takes a backup snapshot. Control+Shift+C duplicates the current row (Shift modifies native copy into row-copy). Control+Shift+I imports data (I for Import). Control+Shift+X exports data (X for eXport).

### Alt family

Alt+A sorts ascending (A for Ascending); Alt+Shift+A sorts descending. Alt+D sorts by date oldest first (D for Date); Alt+Shift+D sorts by date most recent first. Alt+K restores the saved bookmark. Alt+Shift+O sorts by an arbitrary expression (O for Object). Alt+Enter shows properties (Windows convention). Alt+Backslash opens the file's folder in Explorer (backslash evokes Windows paths).

### Navigation family

Tab and Shift+Tab move between columns in the current row. The arrow keys move between rows. Enter shows the full row (Show-Record); Control+Enter opens the cell's content as a URL or path (Open-Cell). Control+Tab cycles among recently-visited tables; Control+Shift+Tab cycles backward. Alt+Home and Alt+End jump to the first or last marked row; Alt+UpArrow and Alt+DownArrow step among marked rows.

### GraveAccent family

Control+GraveAccent is the GUI menu hotkey for Enter-Console (open the dot prompt). Alt+GraveAccent is a global hotkey: when the console has focus, it brings the GUI forward. Alt+Control+GraveAccent is a global hotkey that always acts: it toggles between GUI and console, whichever is not currently in front.

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
11. `look` — computed text. A pipe-joined rendering of the table's most identifying distinct fields; appears in listboxes and quick-search displays.

DbDuo hides every column ending in `_id` (primary and foreign keys), bare `id`, and the bookkeeping columns (added, updated, marked, look) from the ListView by default. Use the Select-Column command to override on a per-table basis.

## Persistence

Between sessions, DbDuo remembers the last opened database and table (relaunch goes straight there), per-table sort/filter/position/Select-Column lists within a session (the `TableSettings` cache), and one named bookmark per session. Settings live in `%LOCALAPPDATA%\DbDuo\DbDuo.ini`. Successful and failed writes are logged.

## Logging

DbDuo writes a per-session log to `%LOCALAPPDATA%\DbDuo\DbDuo.log` (truncated at every program start). The log records database opens, table switches, errors, and the result of Alt+GraveAccent and Alt+Control+GraveAccent hotkey registration. Use the Show-Log command (no hotkey) to print the exact path.

## Bundled documentation

- `README.md` and `README.htm` — summary and quick start with a guided tour of `sample.db`.
- `DbDuo.md` and `DbDuo.htm` — this reference.
- `License.md` and `License.htm` — MIT License text.
- `sample.db` — the bundled SQLite sample database (teachers, classes, students, enrollments).
