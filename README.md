# DbDuo

Manage databases in popular file formats, with synchronized interfaces between CLI and GUI modes, designed to maximize productivity for keyboard users of Windows.

## About

DbDuo runs as both a screen-reader-friendly WinForms GUI and a PowerShell-flavored dot-prompt CLI at the same time, over a single shared ADO database connection. A change in either interface shows up immediately in the other; switching between them is one keypress. DbDuo opens SQLite, Microsoft Access, Excel, dBASE, and delimited-text files through the same set of Verb-Noun commands.

DbDuo is built for blind developers, accessibility testers, and anyone who works faster with the keyboard than with the mouse.

## Features

- **One database connection, two simultaneous interfaces.** Edits in the GUI grid show up immediately in the dot prompt and vice versa, with no synchronization step.
- **Wide format support through native ADO.** SQLite via ODBC, Access / Excel / dBASE / CSV / TSV / TXT via the Access Database Engine (ACE).
- **PowerShell-canonical verb taxonomy.** Every command is a Verb-Noun pair (`Step-Record`, `Show-Table`, `Sort-Object`, `Invoke-Sql`, `Enter-Child`), with short aliases for fast typing and dBASE compatibility (`skip`, `locate`, `replace`).
- **Drill-down navigation.** Use the Enter-Child command, Control+E, to drill from a parent row into a related child table; use Exit-Child, Control+Shift+E, to pop back to the parent's exact row.
- **Multi-format Export-Data.** Use Control+Shift+X (X for eXport) to write xlsx, docx, filtered HTML, csv, and tsv files in any combination, in a single call. xlsx and docx use Word and Excel via late-bound COM; csv and tsv work without Microsoft Office.
- **JAWS-canonical key naming throughout.** UpArrow, DownArrow, NumPad0 through NumPad9, GraveAccent, Minus rather than Dash; modifiers always in alpha order (Alt+Control+Shift).
- **Single-instance with global hotkey wake-up.** Use the Desktop hotkey, Alt+Control+D, from anywhere in Windows to activate a running instance or launch a fresh one.

## Requirements

DbDuo needs 64-bit Windows 10 or 11 and the .NET Framework 4.8, which is already present on current Windows. The setup program checks for the SQLite ODBC driver and the Microsoft Access Database Engine and silently installs whichever is missing; it uses WinGet for the Access engine when available, and falls back to a direct download from Microsoft otherwise. Chocolatey is not assumed and not needed.

Microsoft Office is optional. It is required only when you use the Export-Data command, Control+Shift+X, to produce xlsx, docx, or filtered-HTML output. Everything else, including export to csv, tsv, and plain HTML, works without Office.

## Installation

Download `DbDuo_setup.exe` from <https://github.com/JamalMazrui/DbDuo/releases/latest/download/DbDuo_setup.exe> and run it. (That URL always points to the latest published release.) Accept the defaults. The Welcome page carries a brief MIT license summary; the Select Destination Location page proposes `C:\Program Files\DbDuo`, which you can change. The Installing step silently fetches any missing driver. On the Finish page you can launch DbDuo immediately and open the documentation; both checkboxes are checked by default.

Setup creates five Start Menu entries — DbDuo (both modes), DbDuo (GUI only) with hotkey Alt+Control+G, DbDuo (CLI only) with hotkey Alt+Control+L, DbDuo (read-only), and DbDuo sample database (opens the bundled `sample.db`). Setup also creates one Desktop shortcut, DbDuo with hotkey Alt+Control+D. Use the Desktop hotkey, Alt+Control+D (D for Desktop), from anywhere in Windows to bring DbDuo to the foreground or, if it is not running, to launch a fresh copy.

## Quick start: a guided tour of `sample.db`

The bundled `sample.db` contains four related tables: `teachers`, `classes`, `students`, and `enrollments`. Teachers each teach one or more classes; students enroll in one or more classes through the `enrollments` junction table. This tour walks through the most common DbDuo commands on that data.

### Open the sample database

Use the Desktop hotkey, Alt+Control+D, to launch DbDuo. Use the Open-Database command, Control+O (O for Open), to bring up a file dialog and pick `C:\Program Files\DbDuo\sample.db`. (You can also pick "DbDuo sample database" from the Start Menu, which opens the same file.) DbDuo opens the database, lands on the `classes` table, and announces the row count.

If the dot prompt console window did not open at the same time, use the Enter-Console command, Control+GraveAccent (the unshifted key above Tab), to open it now.

### See what tables are present

Use the Select-Table command, F4 (F4 = MDI "switch document" convention), to bring up a listbox of base tables. Arrow keys move through `classes`, `enrollments`, `students`, `teachers`; Enter chooses one. To list tables from the dot prompt, type `tables` (the alias for Get-Table) and press Enter.

### Move around the data grid

The arrow keys move between rows; Tab and Shift+Tab move between columns within a row. As you move, DbDuo announces each cell as "ColumnHeader: value" so the screen reader names the column you just entered. Use the Show-Record command, plain Enter, to hear all the columns of the current row at once.

Type a letter to jump to the next row whose current column starts with that letter. Both lowercase and capital letters work for this; type quickly to extend the search prefix (typing "dr" goes to the next row starting with "Dr."). Type-ahead is case-insensitive, so capital A and lowercase a both jump to rows starting with A.

### Drill into a related child table

Here is what distinguishes DbDuo. Move to the `teachers` table using Select-Table, F4, then arrow to the first row, Dr. Ada Lovelace. She teaches one class in the `classes` table. Use the Enter-Child command, Control+E (E for Enter), to drill into her classes.

DbDuo looks at every other base table in the database, finds the ones whose schema includes the column `teacher_id`, and applies the right filter. Only one child table matches (`classes`), so DbDuo opens it directly with a filter that shows only Ada Lovelace's class: CS101, "Introduction to Programming."

Now drill once more. Press Control+E again on CS101. The `enrollments` table is the unique child, so DbDuo opens it filtered to show only students enrolled in CS101.

Use the Exit-Child command, Control+Shift+E (Shift inverts Control+E), to pop back to CS101 in `classes`. Press Control+Shift+E once more to pop back to Ada Lovelace in `teachers`. DbDuo restores each parent table's sort, filter, and exact row position along the way.

### Filter and sort

Use the Select-Record command, Control+F (F for Filter), to filter the view. Type an ADO expression like `year = 'Senior'` on the `students` table to see only seniors. Use the Reset-Filter command, Control+Shift+F (Shift modifies Filter into "reset"), to clear it.

Use the Sort-Ascending command, Alt+A (A for Ascending), to sort by the current column alphabetically; Alt+Shift+A sorts descending. Use the Sort-OldestFirst command, Alt+D (D for Date), to sort by the `updated` column oldest first; Alt+Shift+D sorts most-recent first. Use the Sort-Object command, Alt+Shift+O (O for Object, the PowerShell noun for a sortable thing), to type an arbitrary ADO sort expression like `name ASC, year DESC`.

### Export

Use the Export-Data command, Control+Shift+X (X for eXport), to produce one or more files from the current view. The GUI prompts for a destination; the dot prompt accepts a multi-format argument:

```
Export-Data xlsx docx html csv
```

This writes all four files into the database's folder, named after the current table, and opens each one in its default Windows application so you immediately hear what was produced.

### Switch between modes

The grave-accent key — the unshifted character above Tab on US keyboards — coordinates the two modes. Use the Enter-Console command, Control+GraveAccent (from the GUI menu), to open the dot prompt. Once in the console, use Alt+GraveAccent to bring the GUI back forward. From anywhere in Windows, use Alt+Control+GraveAccent to toggle DbDuo between its two modes. JAWS calls this key "GraveAccent."

### Quit

Use the Exit-Application command, Alt+F4 (the Windows-standard close-program key), or type `quit` at the dot prompt, to close DbDuo entirely. If you want to leave the dot prompt but keep the GUI running, type `exit` (or `x` or `bye`) instead.

## Learning more

The full reference is in `DbDuo.htm` and `DbDuo.md`. Every menu, every command, and every hotkey appears there with its mnemonic and dot-prompt aliases.

## License

MIT License. See `License.md`, or `License.htm` for the same text rendered in HTML.

## Project home

<https://github.com/JamalMazrui/DbDuo>
