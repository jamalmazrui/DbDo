# DbDuo

DbDuo is a dual-mode database manager for Windows. It runs as both a screen-reader-friendly WinForms GUI and a PowerShell-flavored dot-prompt CLI at the same time, over a single shared database connection. Either mode can drive a SQLite, Microsoft Access, Excel, dBASE, or delimited-text database; switching between them is one keypress.

DbDuo is built for blind developers, accessibility testers, and anyone who works faster with the keyboard than with the mouse.

## Requirements

DbDuo needs 64-bit Windows 10 or 11 and the .NET Framework 4.8, which is part of current Windows. The setup program checks for the SQLite ODBC driver and the Microsoft Access Database Engine and silently installs whichever is missing; it uses WinGet for the Access engine when available, and falls back to a direct download from Microsoft otherwise. Chocolatey is not assumed and not needed.

Microsoft Office is optional. It is required only when you use the Export-Data command, Control+Shift+X (X for eXport), to produce xlsx, docx, or filtered-HTML output. Everything else, including export to csv, tsv, and plain HTML, works without Office.

## Installation

Download `DbDuo_setup.exe` from the project's GitHub releases page at <https://github.com/JamalMazrui/DbDuo>, run it, and accept the defaults. The Welcome page summarizes the MIT license; the Select Destination Location page proposes `C:\Program Files\DbDuo`, which you can change or leave alone. The Installing step silently fetches any missing driver and writes everything to disk. On the Finish page you can launch DbDuo immediately and open the documentation; both checkboxes are checked by default.

Setup creates five Start Menu entries, all under "DbDuo":

- **DbDuo** — both GUI and console, the default mode.
- **DbDuo (GUI only)** — GUI without the console window. Use the Alt+Control+G hotkey (G for GUI) to launch it directly.
- **DbDuo (CLI only)** — dot prompt without a GUI window. Use Alt+Control+L (L for cLi) to launch it directly.
- **DbDuo (read-only)** — opens databases with the lock on.
- **DbDuo sample database** — opens the bundled `sample.db` so you can try DbDuo without your own data.

Setup also creates one Desktop shortcut, **DbDuo** with the Alt+Control+D hotkey (D for Desktop). Use Alt+Control+D, the Desktop hotkey, from anywhere in Windows to bring DbDuo to the foreground or, if it is not running, to launch a fresh copy.

## Quick start: a guided tour of `sample.db`

The bundled `sample.db` contains four related tables: `teachers`, `classes`, `students`, and `enrollments`. Teachers each teach one or more classes; students enroll in one or more classes through the `enrollments` junction table. This tutorial walks through the most common DbDuo commands on that data, with each hotkey shown alongside its mnemonic.

### Open the sample database

Use the Desktop hotkey, Alt+Control+D, to launch DbDuo. Use the Open-Database command, Control+O (O for Open), to bring up a file dialog and pick `C:\Program Files\DbDuo\sample.db`. (You can also pick "DbDuo sample database" from the Start Menu, which opens the same file.) DbDuo opens the database, lands on the `classes` table, and speaks the row count.

If the dot prompt console window did not open at the same time, use the Enter-Console command, Control+GraveAccent (the unshifted key above Tab), to open it now. From here on, every action has both a GUI hotkey and a dot-prompt command; either works at any time.

### See what tables are present

Use the Select-Table command, F4 (F4 = MDI "switch document" convention), to bring up a listbox of base tables. Arrow keys move through `classes`, `enrollments`, `students`, `teachers`; Enter chooses one. To list tables from the dot prompt, type `tables` (the alias for Get-Table) and press Enter.

### Move around the data grid

The arrow keys move between rows; Tab and Shift+Tab move between columns within a row. As you move, DbDuo announces each cell as "ColumnHeader: value" so the screen reader names the column you entered. Use the Show-Record command, plain Enter, to hear all the columns of the current row at once. Use the Show-Status command, Control+F1 (F1 for help, Control for "where am I"), to hear which table, row, and filter you are currently in.

### Drill into a related child table

Now for the part that distinguishes DbDuo. Move to the `teachers` table using Select-Table, F4, then arrow to the first row, Dr. Ada Lovelace. She teaches one class in the `classes` table. Use the Enter-Child command, Control+E (E for Enter), to drill into her classes.

DbDuo looks at every other base table in the database, finds the ones whose schema includes the column `teacher_id`, and applies the right filter. Only one child table matches (`classes`), so DbDuo opens it directly with a filter that shows only Ada Lovelace's class: CS101, "Introduction to Programming."

Now drill once more. Press Control+E again on CS101. This time two child tables match: `enrollments` (which has a `class_id` column) and nothing else from `classes` itself. Since there is only one match, DbDuo opens the `enrollments` table filtered to show only students enrolled in CS101.

Use the Exit-Child command, Control+Shift+E (Shift inverts Control+E), to pop back to CS101 in `classes`. Press Control+Shift+E once more to pop back to Ada Lovelace in `teachers`. DbDuo restores the parent table's sort, filter, and exact row position each time.

### Filter and sort

Use the Select-Record command, Control+F (F for Filter), to apply a filter. Type the ADO expression `year = 'Senior'` on the `students` table to see only seniors. Use the Reset-Filter command, Control+Shift+F (Shift modifies Filter into "reset"), to clear it.

Use the Sort-Ascending command, Alt+A (A for Ascending), to sort by the current column alphabetically; Alt+Shift+A inverts it. Use the Sort-OldestFirst command, Alt+D (D for Date), to sort by the `updated` column oldest first; Alt+Shift+D inverts it. Use the Sort-Object command, Alt+Shift+O (O for Object, the PowerShell noun for a sortable thing), to type an arbitrary ADO sort expression yourself, for example `name ASC, year DESC`.

### Export

Use the Export-Data command, Control+Shift+X (X for eXport), to produce one or more files from the current view. The GUI prompts for a destination; the dot prompt accepts a multi-format argument:

```
Export-Data xlsx docx html csv
```

This writes all four files into the database's folder, named after the current table, and opens each one in its default Windows application so you immediately hear what was produced. The xlsx and docx formats need Microsoft Office, which most readers will have. The csv and tsv formats work without Office.

### Switch between modes

The grave-accent key — the unshifted character above Tab on US keyboards — coordinates the two modes. Use the Enter-Console command, Control+GraveAccent (from the GUI menu), to open the dot prompt. Once in the console, use Alt+GraveAccent to bring the GUI back forward; this chord acts only while the console has focus, so it does not interfere when you are typing in Word or any other application. From anywhere in Windows, use Alt+Control+GraveAccent to toggle DbDuo between its two modes. JAWS calls this key "GraveAccent."

### Quit

Use the Exit-Application command, Alt+F4 (the Windows-standard close-program key), or type `quit` at the dot prompt, to close DbDuo entirely. If you want to leave the dot prompt but keep the GUI running, type `exit` (or `x` or `bye`) instead; that maps to Exit-Console.

## Learning more

The full reference is in `DbDuo.htm` and `DbDuo.md`. Every menu, every command, every hotkey appears there with its mnemonic and its dot-prompt aliases, plus walk-throughs of each feature group.

## License

MIT License. See `License.md`, or `License.htm` for the same text rendered in HTML.

## Project home

<https://github.com/JamalMazrui/DbDuo>
