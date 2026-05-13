# DbDuo

**An accessible, keyboard-first database manager for Windows.** DbDuo opens SQLite, Microsoft Access, Excel, dBASE, and delimited text through one consistent set of PowerShell-flavored commands, in a GUI and a dot-prompt console at the same time. The GUI is engineered for screen-reader use from the ground up: JAWS, NVDA, and Narrator are all first-class through dedicated speech paths, with table-style cell navigation, direction-aware announcements, and a double-press-spells convention familiar from EdSharp and FileDir.

## Who DbDuo is for

DbDuo is built for people who work databases by keyboard — most especially for screen-reader users, who are often poorly served by mainstream database GUIs. Every menu announces its hotkey; every row change announces the new position; every cell move announces the column header and value; the live region is wired through a JAWS-direct / NVDA-direct / UIA-fallback chain so speech reliably reaches your reader. Power-keyboard users who don't run a screen reader still get a fast, dot-prompt-augmented GUI that doesn't depend on the mouse for anything.

## What sets DbDuo apart

**A virtual table cursor.** The data list is a virtual-mode ListView, but on top of it DbDuo overlays a `(row, column)` cursor you drive with Alt+Control + arrow / Home / End / PageDown / PageUp / Numpad5. Movement triggers a direction-aware announcement: a horizontal move says "Header: value"; a vertical move says "Row N: value"; a corner jump says both. The convention mirrors how JAWS and NVDA read HTML and Word tables, so the muscle memory transfers.

**Double-press to spell.** Any speech-only command (the eight Say-X commands, plus Alt+Control+Numpad5 for the current cell) spells the spoken text character by character when pressed twice within 1.5 seconds. The same convention is in EdSharp and FileDir, so it's already in your muscle memory if you use those.

**Three search families with persistent recent history.** Find (Control+F) for substring across all columns, Jump to Match (Control+J) for substring within one column, Find Regex (Control+F3) for .NET regex across all columns. Each has its own dialog with a Text input, a Recent listbox of up to the last 10 terms, and a Case-sensitive checkbox. Selecting a Recent entry copies its text AND sets the Case-sensitive checkbox to how that term was last used. F3 / Shift+F3 repeats whichever family was most recent.

**One connection, two interfaces.** The GUI window and the dot-prompt console drive the same live ADO recordset. Edits made in the prompt show up immediately in the data list, and vice versa. The grave-accent key family (Control, Alt, Alt+Control) coordinates jumping between them, including a global hotkey that works from anywhere in Windows.

**Recent Files with per-table state.** Alt+R opens the last 10 databases; each entry restores not just the file but the last-active table, the filter expression, the sort order, and the row position you were on when you closed it.

**Parent-child drill.** Alt+RightArrow drills from a parent row into a filtered child table (a teacher's classes, a class's enrollments). Alt+LeftArrow returns to the exact parent row, preserving sort and filter. The drill stack is unbounded; Alt+Home pops all the way back.

**Snippet scripting.** Save small scripts in your own editor, then run them inside DbDuo with a single keystroke. Invoke Snippet on Alt+V picks a file from your snippets folder and runs it; Edit Snippet on Alt+Shift+V picks one to edit (or creates a new one); Open Snippet Folder shows the folder in Explorer. Snippets can automate any operation you would do by hand — bulk record edits, scripted filter changes, custom reports, anything the database manager itself can do — without leaving the keyboard. Scripts that are not executable code (plain text, SQL fragments, reference notes) are also welcome in the folder and are shown in a dialog when invoked, so the same Alt+V hotkey doubles as a snippet library.

## Top-level menu structure

DbDuo uses a six-menu layout — File, Edit, Navigate, Query, Misc, Help — with single-letter Alt mnemonics for each.

- **File** — database files (New, Open, Recent Files on Alt+R, Save As, Close, Backup, Compare, Import, Export, Print), table picker (Choose Table on F4), and the Next Visited Table / Next Table or View cycling family
- **Edit** — modify the data (New Record on Control+N, Edit Record on F2, Delete Record on Control+D, Find and Replace Across Rows on Control+R), marks (Mark Record on Control+M, Unmark Record on Control+U), bookmarks (Control+K / Alt+K / Control+Shift+K), Open Cell Value on Control+Enter
- **Navigate** — Step-Record family, Go to Row on Shift+G, the three search families (Control+F, Control+J, Control+F3) with reverse variants and F3 / Shift+F3 dispatcher, parent-child drill (Alt+RightArrow, Alt+LeftArrow, Alt+Home)
- **Query** — read-only inspection (Show Record on Enter, Table Properties on Alt+Enter, Related Records, Show Schema), the Say-X speech family (Alt+Z status, Alt+P path, Alt+Y yield, etc.), and the filter / sort cluster
- **Misc** — utilities (Refresh View on F5, Toggle Read-Only Lock on Control+F7, Run SQL on Control+Q, Test Integrity, Test Drivers, Open in Explorer, Open Dot Prompt on Control+GraveAccent, Edit Configuration on F12), plus Table Statistics, Frequency Chart, Choose Visible Columns, Extract Regex Matches, and the snippet family (Invoke Snippet on Alt+V, Edit Snippet on Alt+Shift+V, Open Snippet Folder)
- **Help** — Help Contents on F1, Version History on Shift+F1, About DbDuo on Alt+F1, Toggle Key Describer Mode on Control+F1, Command Picker on Alt+F10, Check for Update on F11

Menu labels use natural English. The PowerShell canonical command names (Show-Object, Set-Mark, Sort-Object, etc.) remain available at the dot prompt for users who prefer Verb-Noun typing.

## Stable characteristics

- **Accessibility-first.** JAWS, NVDA, and Narrator are all explicitly tested. Three speech paths in priority order: JAWS via direct COM, NVDA via the controller-client DLL, UIA live-region fallback for everything else.
- **One connection, two interfaces.** Edits made in the dot prompt show up immediately in the ListView, and the reverse. No synchronization step.
- **Native ADO.** SQLite via ODBC, Access / Excel / dBASE via the Access Database Engine, CSV / TSV via the Jet text driver. The recordset, filter, and sort are first-class objects.
- **PowerShell verb taxonomy** for the canonical command names. Aliases line up with dbDot dot commands (`add`, `edit`, `find`, `next`, `previous`) and PowerShell shorthand (`out`, `read`, `tee`). Menu labels use natural English so the GUI reads cleanly even though the CLI keeps Verb-Noun.
- **JAWS-canonical key names** throughout the menus, help, and status messages: UpArrow, DownArrow, NumPad0 through NumPad9, GraveAccent, Minus rather than Dash, modifiers always in alpha order (Alt+Control+Shift).
- **Single-instance with hotkey wake-up.** A second launch activates the running window rather than starting a duplicate. The Desktop shortcut on Alt+Control+D activates from anywhere.

## Project home and downloads

- **Project page (source, issues, discussion):** <https://github.com/JamalMazrui/DbDuo>
- **Direct installer download (always the latest release):** <https://github.com/JamalMazrui/DbDuo/releases/latest/download/DbDuo_setup.exe>
- **Full project as a single zip (current main branch):** <https://github.com/JamalMazrui/DbDuo/archive/main.zip>

For per-release notes, see `History.md` (or open `History.htm` with Shift+F1 inside DbDuo, or type `history` at the dot prompt).

Released under the MIT License.

Feedback from blind and low-vision developers is especially welcome — keyboard ergonomics, screen-reader announcements, command-name choices, anything that helps the tool fit how people actually work.
