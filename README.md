# DbDuo

**An accessible, keyboard-first database manager for Windows.** DbDuo opens SQLite, Microsoft Access, Excel, dBASE, and delimited-text files through one consistent set of PowerShell-flavored commands, in a GUI window and a dot-prompt console at the same time. JAWS, NVDA, and Narrator are all supported through dedicated speech paths, with table-style cell navigation, direction-aware announcements, and the double-press-spells convention familiar from EdSharp and FileDir. Every command is reachable by keyboard.

## Who this is for

DbDuo is designed for people who work databases by keyboard. If you are a screen-reader user, a power-keyboard user, or anyone who finds mainstream database GUIs awkward to drive without a mouse, DbDuo is built for the way you actually work. Every menu announces its hotkey, every row change announces the new position, every cell move announces the column header and value, and the live region is wired through a JAWS-direct / NVDA-direct / UIA-fallback chain so speech reliably reaches your reader.

If you are a sighted developer who appreciates a keyboard-first tool with a built-in dot prompt, the speech work doesn't get in your way — DbDuo is silent unless a screen reader is running.

## What DbDuo lets you do

**Open the database formats you actually have.** SQLite (`.db`, `.sqlite`, `.sqlite3`), Microsoft Access (`.mdb`, `.accdb`), Excel workbooks (`.xlsx`, `.xls`), dBASE tables (`.dbf`), and delimited text (`.csv`, `.tsv`, `.txt`) all open through the same Open Database command. No driver paperwork; the setup program installs the SQLite ODBC driver and the Microsoft Access engine the first time they are needed.

**Read your data as a table, by cell.** A virtual cell cursor lets you navigate a row as a table — Alt+Control+RightArrow / LeftArrow move one column at a time within the current row; Alt+Control+DownArrow / UpArrow move one row at a time within the current column; Alt+Control+Home and End jump to the corners. Each move announces the resulting cell with direction-aware framing.

**Spell anything on a second press.** Press the Say Status command (Alt+Z), Say Path (Alt+P), Say Yield (Alt+Y), or any of the eight Say-X commands twice in succession to hear the text spelled character by character — the EdSharp and FileDir convention. The same applies to Alt+Control+Numpad5, which speaks or spells the current virtual cell.

**Move around your data by keyboard alone.** Arrow keys step between rows; Tab and Shift+Tab move an announcement-only column cursor across the current row so you can hear what's in each cell. Lowercase letters jump to the next row whose value in the announced column starts with that letter, like type-ahead in any Windows list. Five capital-letter chords — Shift+F, Shift+G, Shift+J, Shift+R, Shift+S — trigger the most-used commands directly from the data list.

**See a record's full story at a glance.** Press Enter on a record to open Show Record — a read-only view of every visible field as `name = value`, plus an automatic Related Records section. If you are on a teacher, you see the teacher's classes. If you are on an enrollment, you see the student and the class it ties together. The related records use each table's `look` summary column, so a few words tell you who or what each one is.

**Drill from a parent record into its related child records, and back out.** From a teacher's row, Alt+RightArrow opens that teacher's classes — a filtered view of the `classes` table. Alt+RightArrow again from a class opens its students. Alt+LeftArrow returns to the exact parent row you came from, preserving sort and filter at each level. Alt+Home pops the entire drill stack at once.

**Filter, sort, find — and search across three independent families.** Find (Control+F) for substring search across all visible columns; Jump to Match (Control+J) for substring within one column you pick; Find Regex (Control+F3) for .NET regex. Each family has its own dialog with a Text input, a Recent listbox of up to the last 10 terms, and a Case-sensitive checkbox. F3 / Shift+F3 repeat whichever family you most recently invoked.

**Resume your work across launches.** Use Recent Files, Alt+R, to reopen one of the last 10 database files. DbDuo restores not just the file but the last-active table, the filter expression, the sort order, and the row position. Anything that no longer applies (a dropped table, a renamed column) is silently skipped.

**Export to whatever your collaborators need.** Save Database As writes a copy of the open database. Export Data writes the current filtered view to xlsx, docx, filtered HTML, Markdown table, CSV, TSV, SQLite, Access, or dBASE — any format DbDuo can also open. Multiple formats in one call from the dot prompt: `Export-Data xlsx docx md csv` produces all four at once and opens each one in its default Windows application.

**Round-trip with Markdown.** Export Data to `.md` writes a GitHub-flavored Markdown table that you can paste into a README, an issue, or a chat message. Import Data reads that same format back into the table, matching header cells to columns by name. So you can hand a Markdown table to a colleague, get an edited one back, and append the changes.

**Talk to two interfaces at once.** The GUI window and the dot-prompt console drive the same live database connection. Edits in one appear immediately in the other. Use the GUI for browsing; use the dot prompt for one-off SQL, ad-hoc queries, or scripted batches. The grave-accent key family (Control, Alt, Alt+Control) coordinates jumping between them.

## How the accessibility design works

Every meaningful state change speaks. Row movement announces "Row N of M, value." Cell movement (Alt+Control+arrow) announces with direction awareness as described above. Filter / sort / mark / unmark all announce their result.

Three speech paths in priority order: JAWS via direct COM automation (when JAWS is running), NVDA via the official controller-client DLL (when NVDA is running), UIA live-region fallback for Narrator and anything else.

DbDuo ships JAWS settings (a JKM key map plus a compiled script binary) and an NVDA add-on. Both make their screen reader pass DbDuo's chords through to the application rather than intercepting them for table-navigation or browse-mode commands. The installer offers to install both as Finish-page checkboxes (both checked by default). The JAWS install just works; for the NVDA add-on to install, NVDA itself must be the currently-running screen reader, since NVDA owns the `.nvda-addon` file handler. If you ran the installer while using JAWS, dismiss the NVDA-install dialog if it appears, switch to NVDA, and re-install the add-on later from the DbDuo Help menu's "Re-install NVDA Add-on" command (which invokes `DbDuo.exe --install-nvda-addon`) or by double-clicking `DbDuo.nvda-addon` in the install folder.

Every command is reachable through both a menu and a hotkey. Menu items show their hotkey at the right edge, so navigating menus is also how you learn the keyboard. Single-letter mnemonics open each top-level menu (File, Edit, Navigate, Query, Misc, Help).

Lowercase letters never trigger a command in the data list — they always navigate. Capital letters trigger one-key shortcuts to the five most-used commands (Filter, Go to, Jump, Reset filter, Sort).

**Turning DbDuo's direct speech off.** If you prefer to rely solely on your screen reader's natural focus and selection announcements without DbDuo's added commentary, use Help > Toggle Extra Speech (Alt+Shift+S). When off, the screen reader still hears DbDuo through its own announcements — only the extra status / cell-position / command-echo speech is suppressed. The setting persists across launches.

## Requirements

64-bit Windows 10 or 11, with .NET Framework 4.8 (already present on current Windows). Microsoft Office is optional, needed only for Export Data's xlsx, docx, and filtered-HTML output paths; csv, tsv, md, plain HTML, SQLite, Access, and dBASE export all work without it.

Tested with JAWS, NVDA, and Narrator. Other screen readers that support the Microsoft UIA live-region pattern should also work through the fallback path.

## Installation

Download `DbDuo_setup.exe` from <https://github.com/JamalMazrui/DbDuo/releases/latest/download/DbDuo_setup.exe> and run it. (That URL always points to the latest published release.) Accept the defaults. The Welcome page carries a brief MIT license summary; the Select Destination Location page proposes `C:\Program Files\DbDuo`, which you can change. On the Finish page you can launch DbDuo immediately and read the documentation; both checkboxes are checked by default. Two further Finish-page checkboxes offer to install the JAWS settings and the NVDA add-on.

If you would rather have the full source bundle than the installer, download <https://github.com/JamalMazrui/DbDuo/archive/main.zip>. That URL returns a zip containing the entire current main branch.

Setup creates a single shortcut, DbDuo, with hotkey Alt+Control+D. Use the hotkey, Alt+Control+D (D for Desktop), from anywhere in Windows to bring DbDuo to the foreground or, if it is not running, to launch a fresh copy.

## Quick start: a guided tour of `sample.db`

The bundled `sample.db` contains four related tables: `teachers`, `classes`, `students`, and `enrollments`. Teachers each teach one or more classes; students enroll in one or more classes through the `enrollments` junction table.

### Open the sample database

Use the hotkey Alt+Control+D to launch DbDuo. Use the Open Database command, Control+O, to bring up a file dialog and pick `C:\Program Files\DbDuo\sample.db`. DbDuo opens the database, lands on the `classes` table, and announces the row count.

If the dot prompt console window did not open at the same time, use the Open Dot Prompt command, Control+GraveAccent (the unshifted key above Tab), to open it now.

### See what tables are present

Use the Choose Table command, F4, to bring up a listbox of base tables. Arrow keys move through `classes`, `enrollments`, `students`, `teachers`; Enter chooses one. To list tables from the dot prompt, type `tables` and press Enter.

### Move around the data list

The arrow keys move between rows. As you arrow up and down, DbDuo's screen-reader speech describes the row you land on.

Try the cell-by-cell virtual cursor: press Alt+Control+RightArrow to move one column right within the current row. DbDuo announces "Header: value." Press it again to move to the next column. Press Alt+Control+DownArrow to move one row down within the current column; DbDuo announces "Row N: value." Press Alt+Control+Home to jump to the top-left, and Alt+Control+End to jump to the bottom-right. Use Alt+Control+Numpad5 to say the current cell value; press it twice in succession to spell it character by character.

Type a lowercase letter to jump to the next row whose value in the announced column starts with that letter. Capital letters are reserved for one-key command shortcuts.

### Show the current record

Use the Show Record command, plain Enter, to open a read-only dialog with every visible field of the current row as `field = value`. Underneath the fields, DbDuo lists every related record grouped by table. OK or Escape closes the dialog.

### Drill into a related child table

Move to the `teachers` table, then arrow to the first row, Dr. Ada Lovelace. Use the Enter Child Table command, Alt+RightArrow, to drill into her classes. Press Alt+RightArrow again on a class to see its enrollments. Use Alt+LeftArrow to pop back; Alt+Home pops the whole stack at once.

### Filter and sort

Use the Filter Records command, Shift+F (F for Filter), to filter the view. Type an ADO expression like `year = 'Senior'` on the `students` table to see only seniors. Use the Clear Filter command, Shift+R (R for Reset), to clear it.

Use the Sort Ascending command, Alt+A, to sort by a column alphabetically; DbDuo prompts for the column, defaulting to whichever column your virtual cursor is on. Alt+Shift+A sorts descending. Alt+D sorts by date (oldest first); Alt+Shift+D most-recent first. Shift+S takes a custom ADO sort expression like `name ASC, year DESC`.

### Find and search

DbDuo has three independent search families. Each remembers its own last-used term.

Control+F is Find Across All Columns. The dialog has a Text input (defaulting to the last Find substring), a Recent listbox of up to 10 entries, and a Case-sensitive checkbox. Selecting a Recent entry copies its text into the Text input AND sets the Case-sensitive checkbox to match how that term was last used. Control+Shift+F searches backward.

Control+J is Jump to Match in One Column — same dialog plus a column picker. Control+F3 is Find Regex Across All Columns — same dialog with regex semantics. F3 repeats whichever family you most recently invoked; Shift+F3 repeats backward.

### Mark, bookmark, jump by row

Control+M marks the current row; Control+U unmarks. Marked rows show "marked" in the status bar.

Control+K saves a bookmark; Alt+K returns to it; Control+Shift+K clears it.

Shift+G jumps to a row number. Out-of-range values clamp to the first or last row.

### Recent Files

Alt+R reopens one of the last 10 databases. DbDuo restores the file, the last-active table, the filter, the sort, and your row position from when you last closed it.

### Export

Control+Shift+X (X for eXport) produces files from the current view. The GUI prompts for a destination; the dot prompt accepts multiple formats at once:

```
Export-Data xlsx docx md csv
```

That writes four files into the database's folder, named after the current table, and opens each one in its default Windows application.

### Round-trip with Markdown

Export Data to a `.md` file produces a Markdown table; Import Data, Control+Shift+I, reads that same format back in. Header cells are matched to columns by name (case-insensitive); unrecognized columns are dropped, and per-row errors do not stop the import.

### Switch between modes

The grave-accent key — the unshifted character above Tab on US keyboards — coordinates the two modes. Control+GraveAccent opens the dot prompt. From inside the console, Alt+GraveAccent brings the GUI back forward. From anywhere in Windows, Alt+Control+GraveAccent toggles between the two modes. JAWS calls this key "GraveAccent."

### Quit

Alt+F4 (the Windows-standard close-program key), or `quit` at the dot prompt, closes DbDuo entirely. If you want to leave the dot prompt but keep the GUI running, type `exit` (or `x` or `bye`) instead.

## Trying DbDuo on larger sample data

Two further sample databases ship in the install folder for exercising DbDuo against more realistic shapes:

- `northwind.db` — the classic Microsoft Northwind sales sample, adapted to DbDuo's standard column set. 8 tables, 101 rows.
- `chinook.db` — the classic Chinook music-store sample, adapted to the same set. 9 tables, 158 rows, with three-deep parent-child chains.

The Help menu has one-keystroke commands to open each: **Open Northwind Sample** and **Open Chinook Sample**, alongside the existing **Open Sample Database** that opens the original small `sample.db`. You can also open any of the three through File > Open Database. Try parent-child drills (artist → albums → tracks in Chinook; category → products → order details in Northwind) to see DbDuo's filtered-child-table navigation against larger row counts.

## Learning more

The full reference is in `DbDuo.htm` and `DbDuo.md`. Every menu, every command, and every hotkey appears there with its mnemonic and dot-prompt aliases. Version history is in `History.md` / `History.htm`.

The coding style used inside `DbDuo.cs` is documented in `CamelType_CSharp.md`, included with the distribution.

## License

MIT License. See `License.md`, or `License.htm` for the same text rendered in HTML.

## Project home

<https://github.com/JamalMazrui/DbDuo>
