# DbDuo

**An accessible, keyboard-first database manager for Windows.** DbDuo opens SQLite, Microsoft Access, Excel, dBASE, and delimited-text files through one consistent set of PowerShell-flavored commands, in a GUI window and a dot-prompt console at the same time. The GUI is built from the ground up to work with screen readers — JAWS, NVDA, and Narrator are all supported through dedicated speech paths, with table-style cell navigation, direction-aware announcements, and the double-press-spells convention familiar from EdSharp and FileDir. Every command is reachable from the keyboard; no command is mouse-only.

## Who this is for

DbDuo is designed for people who work databases by keyboard. If you are a screen-reader user, a power-keyboard user, or anyone who finds mainstream database GUIs awkward to drive without a mouse, DbDuo is built for the way you actually work. The accessibility features are first-class: every menu announces its hotkey, every row change announces the new position, every cell move announces the column header and value, and the live region is wired through a JAWS-direct / NVDA-direct / UIA-fallback chain so speech reliably reaches your reader.

If you are a sighted developer who appreciates a keyboard-first tool with a built-in dot prompt, the speech work doesn't get in your way — DbDuo is silent unless a screen reader is running.

## What DbDuo lets you do

**Open the database formats you actually have.** SQLite (`.db`, `.sqlite`, `.sqlite3`), Microsoft Access (`.mdb`, `.accdb`), Excel workbooks (`.xlsx`, `.xls`), dBASE tables (`.dbf`), and delimited text (`.csv`, `.tsv`, `.txt`) all open through the same Open Database command. No driver paperwork; the setup program installs the SQLite ODBC driver and the Microsoft Access engine for you the first time they are needed.

**Read your data as a table, by cell.** A virtual cell cursor lets you navigate a row as a table — Alt+Control+RightArrow / LeftArrow move one column at a time within the current row; Alt+Control+DownArrow / UpArrow move one row at a time within the current column; Alt+Control+Home and End jump to the corners. Each move announces the resulting cell with direction-aware framing: a horizontal move says "Header: value"; a vertical move says "Row N: value"; a corner jump says both. The convention matches how JAWS and NVDA read HTML and Word tables, so the muscle memory transfers directly.

**Spell anything on a second press.** Press the Say Status command (Alt+Z), Say Path (Alt+P), Say Yield (Alt+Y), or any of the eight Say-X commands twice in succession to hear the text spelled character by character — the EdSharp and FileDir convention. The same convention applies to Alt+Control+Numpad5, which speaks or spells the current virtual cell.

**Move around your data by keyboard alone.** Arrow keys step between rows; Tab and Shift+Tab move an announcement-only column cursor across the current row so you can hear what's in each cell. Lowercase letters jump to the next row whose value in the announced column starts with that letter, like type-ahead in any Windows list. Five capital-letter chords — Shift+F, Shift+G, Shift+J, Shift+R, Shift+S — trigger the most-used commands directly from the data list.

**See a record's full story at a glance.** Press Enter on a record to open Show Record — a read-only view of every visible field as `name = value`, plus an automatic Related Records section. If you are on a teacher, you see the teacher's classes. If you are on an enrollment, you see the student and the class it ties together. The related records use each table's `look` summary column, so a few words tell you who or what each one is.

**Drill from a parent record into its related child records, and back out.** From a teacher's row, Alt+RightArrow (Enter Child Table) opens that teacher's classes — a filtered view of the `classes` table. Alt+RightArrow again from a class opens its students. Alt+LeftArrow (Exit Child Table) returns to the exact parent row you came from, preserving sort and filter at each level. Alt+Home pops the entire drill stack at once.

**Filter, sort, find — and search across three independent families.** Use Find (Control+F) for substring search across all visible columns; Jump to Match (Control+J) for substring search within one column you pick; Find Regex (Control+F3) for .NET regex search. Each family has its own dialog with a Text input, a Recent listbox of up to the last 10 terms, and a Case-sensitive checkbox. Choosing a Recent entry copies its text into the Text input AND sets the Case-sensitive checkbox to how that term was last used. F3 / Shift+F3 repeat whichever family you most recently invoked.

**Resume your work across launches.** Use the Recent Files command, Alt+R, to reopen one of the last 10 database files. DbDuo restores not just the file but the last-active table, the filter expression, the sort order, and the row position you were on. Anything that no longer applies (a dropped table, a renamed column) is silently skipped.

**Export to whatever your collaborators need.** Save Database As writes a copy of the open database. Export Data writes the current filtered view to xlsx, docx, filtered HTML, Markdown table, CSV, TSV, SQLite, Access, or dBASE — any format DbDuo can also open. Multiple formats in one call from the dot prompt: `Export-Data xlsx docx md csv` produces all four at once and opens each one in its default Windows application.

**Round-trip with Markdown.** Export Data to `.md` writes a GitHub-flavored Markdown table that you can paste into a README, an issue, or a chat message. Import Data reads that same format back into the table, matching header cells to columns by name. So you can hand a Markdown table to a colleague, get an edited one back, and append the changes.

**Talk to two interfaces at once.** The GUI window and the dot-prompt console drive the same live database connection. Edits in one appear immediately in the other. Use the GUI for browsing; use the dot prompt for one-off SQL, ad-hoc queries, or scripted batches. The grave-accent key family (Control, Alt, Alt+Control) coordinates jumping between them.

## How the accessibility design works

Every meaningful state change speaks. Row movement announces "Row N of M, value." Cell movement (Alt+Control+arrow) announces with direction awareness as described above. Filter / sort / mark / unmark all announce their result. Status changes announce the new row count.

Three speech paths in priority order: JAWS via direct COM automation (when JAWS is running), NVDA via the official controller-client DLL (when NVDA is running), UIA live-region fallback for Narrator and anything else. The data list itself is a virtual-mode `ListView` in Details view, which all three readers handle as a familiar Details list.

DbDuo ships JAWS settings (a JKM key map plus a compiled script binary) and an NVDA add-on. Both make their screen reader pass DbDuo's chords through to the application rather than intercepting them for table-navigation or browse-mode commands. Without the JAWS settings, JAWS reads Alt+Control+arrow as a JAWS table-navigation command. Without the NVDA add-on, NVDA does the same. The installer offers to install both as Finish-page checkboxes (both checked by default); you can re-run either install later from Help &gt; Install JAWS Settings or by double-clicking `DbDuo.nvda-addon` in the install folder.

Every command is reachable through both a menu and a hotkey. Menu items show their hotkey at the right edge, so navigating menus is also how you learn the keyboard. Single-letter mnemonics open each top-level menu (File, Edit, Navigate, Query, Misc, Help).

Lowercase letters never trigger a command in the data list — they always navigate. Capital letters trigger one-key shortcuts to the five most-used commands (Filter, Go to, Jump, Reset filter, Sort). This keeps the entire alphabet available for type-ahead row jumping in tables of any size.

**Turning DbDuo's direct speech off.** If you prefer to rely solely on your screen reader's natural focus and selection announcements without the additional commentary DbDuo adds, use Help > Toggle Extra Speech (Alt+Shift+S). When off, the screen reader still hears DbDuo via its own announcements — only the extra status / cell-position / command-echo speech is suppressed. The setting persists across launches. The EdSharp and FileDir conventions are the model.

## Requirements

64-bit Windows 10 or 11, with .NET Framework 4.8 (already present on current Windows). Microsoft Office is optional, needed only for Export Data's xlsx, docx, and filtered-HTML output paths; csv, tsv, md, HTML, SQLite, Access, and dBASE export all work without it.

Tested with JAWS, NVDA, and Narrator. Other screen readers that support the Microsoft UIA live-region pattern should also work through the fallback path.

## Installation

Download `DbDuo_setup.exe` from <https://github.com/JamalMazrui/DbDuo/releases/latest/download/DbDuo_setup.exe> and run it. (That URL always points to the latest published release.) Accept the defaults. The Welcome page carries a brief MIT license summary; the Select Destination Location page proposes `C:\Program Files\DbDuo`, which you can change. On the Ready page you'll see a checkbox labeled "Install JAWS settings for DbDuo (recommended if you use JAWS)" which is checked by default; uncheck it if you don't use JAWS or prefer to install the JAWS key map by hand. The Installing step silently fetches any missing driver. On the Finish page you can launch DbDuo immediately and open the documentation; both checkboxes are checked by default.

**Why a JAWS settings file?** JAWS has its own table-navigation chord set on Alt+Control+arrow, and by default it intercepts those chords before DbDuo sees them — you'd hear "Not in a table" instead of DbDuo's cell announcement. The JAWS settings file (`DbDuo.jkm`) tells JAWS to pass those chords through to DbDuo. The file is plain text; no script compilation is involved. See the JAWS settings section in `DbDuo.md` for details.

If you would rather have the full source bundle than just the installer, download <https://github.com/JamalMazrui/DbDuo/archive/main.zip>. That URL returns a zip containing the entire current main branch — the C# source, the Inno Setup script, the build script, the documentation in both Markdown and HTML, the License, and the bundled `sample.db`.

Setup creates five Start Menu entries — DbDuo (both modes), DbDuo (GUI only) with hotkey Alt+Control+G, DbDuo (CLI only) with hotkey Alt+Control+L, DbDuo (read-only), and DbDuo sample database (opens the bundled `sample.db`). Setup also creates one Desktop shortcut, DbDuo with hotkey Alt+Control+D. Use the Desktop hotkey, Alt+Control+D (D for Desktop), from anywhere in Windows to bring DbDuo to the foreground or, if it is not running, to launch a fresh copy.

## Quick start: a guided tour of `sample.db`

The bundled `sample.db` contains four related tables: `teachers`, `classes`, `students`, and `enrollments`. Teachers each teach one or more classes; students enroll in one or more classes through the `enrollments` junction table. This tour walks through the most common DbDuo commands on that data.

### Open the sample database

Use the Desktop hotkey, Alt+Control+D, to launch DbDuo. Use the Open Database command, Control+O (O for Open), to bring up a file dialog and pick `C:\Program Files\DbDuo\sample.db`. (You can also pick "DbDuo sample database" from the Start Menu, which opens the same file.) DbDuo opens the database, lands on the `classes` table, and announces the row count.

If the dot prompt console window did not open at the same time, use the Open Dot Prompt command, Control+GraveAccent (the unshifted key above Tab), to open it now.

### See what tables are present

Use the Choose Table command, F4, to bring up a listbox of base tables. Arrow keys move through `classes`, `enrollments`, `students`, `teachers`; Enter chooses one. To list tables from the dot prompt, type `tables` and press Enter.

### Move around the data list

The arrow keys move between rows. As you arrow up and down, DbDuo's screen-reader speech describes the row you land on.

Try the **cell-by-cell virtual cursor**: press **Alt+Control+RightArrow** to move one column right within the current row. DbDuo announces "Header: value." Press it again to move to the next column. Press **Alt+Control+DownArrow** to move one row down within the current column; DbDuo announces "Row N: value" (you hear just the new value, with the row index, since the column is implied). Press **Alt+Control+Home** to jump to the top-left, and **Alt+Control+End** to jump to the bottom-right. Use **Alt+Control+Numpad5** to say the current cell value; press it twice in succession to spell it character by character.

Type a lowercase letter to jump to the next row whose value in the announced column starts with that letter. Type quickly to extend the search prefix (typing `dr` goes to the next row starting with "Dr."). Capital letters are reserved for one-key shortcuts — see the next section.

### Show the current record

Use the Show Record command, plain Enter, to open a read-only dialog with every visible field of the current row as `field = value`. Underneath the fields, DbDuo lists every related record grouped by table: a teacher row shows their classes; a class row shows the teacher above and the enrollments below; an enrollment row shows the student and class it joins. The related records use each table's `look` summary column, so a single short line identifies each one.

OK closes the dialog; Escape works too.

### Drill into a related child table

Here is what distinguishes DbDuo. Move to the `teachers` table using Choose Table, F4, then arrow to the first row, Dr. Ada Lovelace. She teaches one class in the `classes` table. Use the Enter Child Table command, **Alt+RightArrow** (right-arrow = into the child), to drill into her classes.

DbDuo looks at every other base table in the database, finds the ones whose schema includes the column `teacher_id`, and applies the right filter. Only one child table matches (`classes`), so DbDuo opens it directly with a filter that shows only Ada Lovelace's class: CS101, "Introduction to Programming."

Drill once more. Press Alt+RightArrow again on CS101. The `enrollments` table is the unique child, so DbDuo opens it filtered to show only students enrolled in CS101.

Use the Exit Child Table command, **Alt+LeftArrow** (left-arrow = out of the child), to pop back to CS101 in `classes`. Press Alt+LeftArrow once more to pop back to Ada Lovelace in `teachers`. Use **Alt+Home** to pop the entire drill stack in one keypress. DbDuo restores each parent table's sort, filter, and exact row position along the way.

### Filter and sort

Use the Filter Records command, **Shift+F** (F for Filter), to filter the view. Type an ADO expression like `year = 'Senior'` on the `students` table to see only seniors. Use the Clear Filter command, **Shift+R** (R for Reset), to clear it.

Use the Sort Ascending command, **Alt+A** (A for Ascending), to sort by a column alphabetically; DbDuo prompts for the column, defaulting to whichever column your virtual cursor is on — just press Enter to accept. Alt+Shift+A sorts descending. Use **Alt+D** for Sort by Date (oldest first); Alt+Shift+D for most-recent first. Use **Shift+S** (S for Sort) for a custom ADO sort expression like `name ASC, year DESC`.

### Find and search

DbDuo has three independent search families. Each remembers its own last-used term, so reopening a family's dialog brings up its own prior text.

Use **Control+F** for Find Across All Columns — substring search across every visible column. The dialog has three controls: a Text input (defaulting to the last Find substring), a Recent listbox of up to the last 10 terms (with `[Aa]` marking entries that were case-sensitive), and a Case-sensitive checkbox (off by default). Selecting a Recent entry copies its text into the Text input AND sets the Case-sensitive checkbox to match how that term was last used. Use **Control+Shift+F** to search backward.

Use **Control+J** for Jump to Match in One Column — same dialog plus a column picker (defaulting to your virtual column). Use **Control+F3** for Find Regex Across All Columns — same dialog with regex semantics.

Use **F3** to repeat whichever family you most recently invoked; Shift+F3 to repeat backward.

### Mark, bookmark, jump by row

Use **Control+M** to mark the current row; Control+U to unmark. Marked rows show "marked" in the status bar. Combine marking with `filter marked` at the dot prompt to scope follow-up commands to your selection.

Use **Control+K** to save a bookmark; Alt+K to return to it; Control+Shift+K to clear it.

Use **Shift+G** (G for Go-to) to jump to a row number. Out-of-range values clamp to the first or last row.

### Recent Files

Use the Recent Files command, **Alt+R**, to reopen one of the last 10 databases. DbDuo restores the file, the last-active table, the filter, the sort, and your row position from when you last closed it.

### Export

Use the Export Data command, **Control+Shift+X** (X for eXport), to produce files from the current view. The GUI prompts for a destination; the dot prompt accepts a multi-format argument:

```
Export-Data xlsx docx md html csv
```

This writes five files into the database's folder, named after the current table, and opens each one in its default Windows application so you immediately hear what was produced. Every input format is also an export format: write to `.db` for SQLite, `.accdb` for Access, `.dbf` for dBASE.

### Round-trip with Markdown

Export Data to a `.md` file produces a Markdown table; Import Data, Control+Shift+I, reads that same format back in. The dialog opens in the folder you last imported from. Header cells are matched to columns by name (case-insensitive); unrecognized columns are dropped, and per-row errors do not stop the import.

### Switch between modes

The grave-accent key — the unshifted character above Tab on US keyboards — coordinates the two modes. Use the Open Dot Prompt command, Control+GraveAccent, to open the dot prompt. Once in the console, use Alt+GraveAccent to bring the GUI back forward. From anywhere in Windows, use Alt+Control+GraveAccent to toggle DbDuo between its two modes. JAWS calls this key "GraveAccent."

### Quit

Use the Exit DbDuo command, Alt+F4 (the Windows-standard close-program key), or type `quit` at the dot prompt, to close DbDuo entirely. If you want to leave the dot prompt but keep the GUI running, type `exit` (or `x` or `bye`) instead.

## Learning more

The full reference is in `DbDuo.htm` and `DbDuo.md`. Every menu, every command, and every hotkey appears there with its mnemonic and dot-prompt aliases. The Virtual cell navigation and Mnemonic hotkey groups sections cover the screen-reader-specific features in detail. Version history is in `History.md` / `History.htm`.

## License

MIT License. See `License.md`, or `License.htm` for the same text rendered in HTML.

## Project home

<https://github.com/JamalMazrui/DbDuo>
