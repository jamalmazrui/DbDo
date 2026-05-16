# DbDuo

Manage database files in a dual interface of console and graphical modes, maximizing productivity for screen reader and keyboard users.

After decades of personal interest in how to achieve nonvisual usability in database management, I have developed a general-purpose program in this area with AI assistance. DbDuo is a free, open source program designed to boost productivity in the input, querying, and output of data.

- [DbDuo project page](https://github.com/JamalMazrui/DbDuo)
- [DbDuo project archive](https://github.com/JamalMazrui/DbDuo/archive/main.zip)
- [DbDuo executable installer](https://github.com/JamalMazrui/DbDuo/releases/latest/download/DbDuo_setup.exe)

DbDuo defaults to an SQLite database file (`.db` extension), which supports management of related tables via either a command-line interface (like the memorable dBASE dot prompt) or a graphical user interface with a menu system and standard controls. You can easily switch between the two interface windows, depending on what technique is more convenient for the task at hand. Other database-like file formats are also supported, for import or export, including `.dbf`, `.mdb`, `.accdb`, `.xlsx`, `.csv`, and `.tsv`.

Every DbDuo command can be accomplished efficiently from the keyboard. In addition, extensions for the JAWS and NVDA screen readers are bundled with the DbDuo installer to further optimize productivity. Advanced features include statistics, charts, and scripts.

## Highlights

**A virtual table cursor for the data list.** Alt+Control plus arrow / Home / End / PageDown / PageUp / Numpad5 moves a `(row, column)` cursor through the data, with direction-aware announcements: a horizontal move says "Header: value"; a vertical move says "Row N: value"; a corner jump says both. The convention matches how JAWS and NVDA read HTML and Word tables, so the muscle memory transfers.

**Double-press to spell.** Any speech-only command spells its text character by character when pressed twice within 1.5 seconds — the convention familiar from EdSharp and FileDir.

**Three search families with persistent history.** Find (Control+F) for substring across all columns, Jump to Match (Control+J) for substring within one column, Find Regex (Control+F3) for .NET regex. Each family remembers its last 10 terms along with their case-sensitive flags. F3 / Shift+F3 repeats whichever family was most recent.

**Parent-child drill.** Alt+RightArrow drills from a parent row into a filtered child table (a teacher's classes, a class's enrollments). Alt+LeftArrow returns to the exact parent row; Alt+Home pops the whole drill stack at once.

**Recent files with per-table state.** Alt+R reopens any of the last 10 databases, restoring not just the file but the last-active table, filter, sort, and row position.

**One connection, two interfaces.** The GUI window and the dot-prompt console drive the same live ADO recordset. Edits made in the prompt show up immediately in the data list, and vice versa.

**Snippet scripting.** Save small JavaScript or text snippets in your own editor, then invoke them inside DbDuo with a single keystroke (Alt+V). Scripts can automate bulk record edits, filter changes, custom reports — anything the database manager itself can do.

## Sample databases

DbDuo ships three SQLite sample databases adapted to its standard column conventions:

- `sample.db` — a small school domain (teachers, classes, students, enrollments) for first-launch exploration.
- `northwind.db` — the classic Microsoft Northwind sales sample (categories, suppliers, products, customers, employees, orders, order details, shippers).
- `chinook.db` — the classic Chinook music-store sample (artists, albums, tracks, genres, customers, invoices, invoice items).

The two larger samples are useful for exercising DbDuo against real-shaped data with multiple parent-child relationships.

## Accessibility design

Three speech paths in priority order: JAWS via direct COM, NVDA via the controller-client DLL, UIA live-region fallback for Narrator and anything else. The data list is a virtual-mode `ListView` in Details view, which all three readers handle as a familiar Details list. JAWS settings (a JKM key map plus a compiled script) and an NVDA add-on ship with the installer so that both screen readers pass DbDuo's chords through rather than intercepting them for their own table-navigation or browse-mode commands.

JAWS-canonical key names appear throughout the menus, help text, and status messages — UpArrow, DownArrow, NumPad0 through NumPad9, GraveAccent, Minus rather than Dash, modifiers always in alphabetical order (Alt+Control+Shift).

## Project home and downloads

- **Source, issues, discussion:** <https://github.com/JamalMazrui/DbDuo>
- **Latest installer (always points to the current release):** <https://github.com/JamalMazrui/DbDuo/releases/latest/download/DbDuo_setup.exe>
- **Full project as a zip:** <https://github.com/JamalMazrui/DbDuo/archive/main.zip>

For per-release notes, see `History.md`. Released under the MIT License.

Feedback from blind and low-vision developers is especially welcome — keyboard ergonomics, screen-reader announcements, command-name choices, anything that helps the tool fit how people actually work.

Jamal
