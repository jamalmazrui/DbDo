# Introducing DbDuo

I have been working on a small Windows tool for managing databases from the keyboard, and I would like to share an initial release.

DbDuo is a dual-mode database manager that runs as both a WinForms GUI and a dot-prompt CLI at the same time, over a single shared database connection. The GUI uses a ListView grid that JAWS, NVDA, and Narrator can read cell by cell; the CLI is a PowerShell-flavored prompt where commands use Verb-Noun names like `Step-Record`, `Show-Table`, `Set-Mark`, `Sort-Object`, `Invoke-Sql`, `Enter-Child`. Either mode can drive any database the other can; pressing **Alt+GraveAccent** switches from the CLI to the GUI, and **Control+GraveAccent** switches the other way.

A few characteristics that might interest other accessibility-focused developers:

- **One connection, two interfaces.** Edits made in the dot prompt show up immediately in the ListView, and the reverse. There is no synchronization step and no second copy of the data.
- **Native ADO.** SQLite via ODBC, Access, Excel, and dBASE via the Access Database Engine, plain CSV and TSV via the Jet text driver. The recordset, filter, and sort are first-class objects, not query-string artifacts.
- **Drill navigation.** `Enter-Child` (Control+E) finds child tables by foreign-key naming convention, opens the unique match, and filters to the related rows. `Exit-Child` (Control+Shift+E) pops back to the parent's exact row.
- **PowerShell verb taxonomy.** Every command is a single Verb-Noun pair; aliases line up with both dBASE dot commands (`skip`, `locate`, `replace`) and SQLite shell commands (`.read`, `.output`).
- **Multi-format export.** `Export-Data` (Control+Shift+X) writes xlsx, docx, filtered HTML, csv, and tsv in any combination, in a single call. xlsx and docx use Word and Excel via late-bound COM; csv and tsv use native code with no Office dependency.
- **JAWS-canonical key names** throughout the menus, help, and status messages: UpArrow, DownArrow, NumPad0 through NumPad9, GraveAccent, Minus rather than Dash, modifiers always in alpha order (Alt+Control+Shift).
- **Single-instance with hotkey wake-up.** A second launch activates the running window rather than starting a duplicate. The Desktop shortcut on Alt+Control+D activates from anywhere.

Project home, source, issues, and releases:

<https://github.com/JamalMazrui/DbDuo>

Released under the MIT License.

I would welcome feedback from blind and low-vision developers especially — keyboard ergonomics, screen-reader announcements, command-name choices, anything that helps the tool fit how people actually work.
