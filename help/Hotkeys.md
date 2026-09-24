# DbDo Hotkeys

Every key DbDo binds, three ways: by menu, by key, and by command. The menu
section says why each key is the one it is.

## How the keys are chosen

- **A letter is the first letter of a word in the command.** Shift+C is Say Cell; Alt+O is Order.
- **Shift and a letter asks.** Every Say command is Shift and a letter, and none changes anything.
- **Adding Shift reverses.** Control+M marks and Control+Shift+M unmarks; Control+W sets the Where filter and Control+Shift+W clears it.
- **Z is for sleep.** Every Z key is a toggle: it wakes a behavior or puts it to sleep. Control+Z undoes in Windows, and restoring a behavior is the same idea.
- **X is for the sound of "ex"**, as in Export.
- **Alt takes the letter when Control already means something in Windows.** Control+O opens and Control+S saves everywhere, so Order is Alt+O and Select Columns is Alt+S.
- **Each function key is a family,** from Windows and Office habits, extended: F1 help, F2 editing, F3 searching, F4 picking, saying or closing open windows, F5 refreshing, F6 moving among parts of the window, F7 review, F8 selection, F9 a more readable view, F10 menus, F11 the version (elevate sounds like eleven), F12 files and AI.
- **Control+Tab moves among DbDo windows,** as it moves among tabs everywhere else.

## By menu

### Edit

- Append Record to Clipboard\
  Alt+Shift+C
- Clear Bookmark\
  Forget the saved bookmark. B for Bookmark. Shift reverses Save Bookmark.\
  Control+Shift+B
- Copy Record\
  Duplicate the current row as a new row. C for Copy.\
  Control+Shift+C
- Delete Record\
  Delete the current record (or marked records, when marks exist). D for Delete.\
  Control+D
- Delete Without Confirmation\
  D for Delete.\
  Control+Shift+D
- Edit Cell\
  Edit just the field under the virtual cursor. F2 is editing, as it renames a file in Windows.\
  F2
- Edit Notes\
  N for Notes.\
  Alt+Shift+N
- Edit Tags\
  T for Tags.\
  Alt+Shift+T
- Edit URL\
  U for URL.\
  Alt+Shift+U
- Edit View\
  E for Edit.\
  Control+E
- List Bookmarks\
  Show saved bookmarks in a listbox; navigate to the selected one. B for Bookmarks.\
  Alt+B
- Mark Record\
  Set the 'marked' flag on the current row. M for Mark.\
  Control+M
- New Copy\
  N for New.\
  Control+Shift+N
- New Record\
  Add a new row to the current table. N for New.\
  Control+N
- Open Cell Value\
  Control+Enter acts from anywhere in a Homer dialog; here it opens the cell's value.\
  Control+Enter
- Open Url\
  U for URL.\
  Control+Shift+U
- Pick Value\
  F2 is editing; Control+F2 edits by picking a value.\
  Control+F2
- Regex Replace\
  Regex find-and-replace within the current virtual column, over visible rows. R for Regex.\
  Control+Shift+R
- Replace Column\
  Substring find-and-replace within the current virtual column, over visible rows. R for Replace.\
  Control+R
- Save Bookmark\
  Save the current row position as a session bookmark. B for Bookmark.\
  Control+B
- Toggle Marked\
  Control+Space toggles a selection in any Windows list.\
  Control+Space
- Unmark Record\
  Clear the 'marked' flag on the current row. M for Mark. Shift reverses Mark Record.\
  Control+Shift+M

### Edit, Bulk Marking

F8 starts a run and Shift completes it; Alt makes the run an unmark instead of a mark.

- Complete Mark\
  Mark every row from the F8 anchor to the current row. F8 is selection; Shift completes the run F8 started.\
  Shift+F8
- Complete Unmark to Anchor\
  Unmark every row from the Alt+F8 anchor to the current row. F8 is selection; Shift completes the run Alt+F8 started.\
  Alt+Shift+F8
- Invert Marked\
  I for Invert.\
  Alt+Shift+I
- Mark All\
  A for All.\
  Control+A
- Start Mark\
  Drop a mark anchor at the current row. F8 is selection: F8 starts a run of marks.\
  F8
- Start Unmark\
  Drop an unmark anchor at the current row. F8 is selection; Alt turns the run into an unmark.\
  Alt+F8
- Unmark All\
  A for All. Shift reverses Mark All.\
  Control+Shift+A

### File

- Choose Table\
  T for Table.\
  Control+T
- Exit DbDo\
  Close DbDo. Alt+F4 closes the program, as in every Windows program.\
  Alt+F4
- Export Data\
  Export the current table or query to CSV, TSV, JSON, or another format. X for the sound of "ex".\
  Alt+X
- Import\
  Build a new DbDo database from another file -- Excel (read through Excel itself, no Access driver needed), Access, dBASE, CSV, or SQLite -- with one standard-shape table per source table, plus maps and lookups. I for Import.\
  Alt+I
- Merge Data\
  Merge rows from a Markdown table, JSON, or Inix file into the current table. M for Merge.\
  Alt+M
- Next Table or View\
  F6 moves among parts of the window; Control+F6 moves to the next table.\
  Control+F6
- Next Visited Table\
  F6 moves among parts; Alt+F6 returns to tables you have visited.\
  Alt+F6
- Open Database\
  Open a database file. O for Open.\
  Control+O
- Open Table in New Window\
  Control+Shift+T
- Previous Table or View\
  F6 moves among parts; Shift reverses Control+F6.\
  Control+Shift+F6
- Previous Visited Table\
  F6 moves among parts; Shift reverses Alt+F6.\
  Alt+Shift+F6
- Print\
  P for Printer.\
  Control+P
- Recent Files\
  R for Recent.\
  Alt+R
- Run Report\
  Alt+Shift+R. Render a saved report definition to a Markdown document. A report reads the whole PHYSICAL table (not the grid view), so it is reproducible regardless of your current filter or sort. R for Run.\
  Alt+Shift+R
- Save\
  Save changes to disk (writes an open workbook back to its .xlsx; a .db is already saved as you go). S for Save.\
  Control+S
- Save As\
  Save a copy to another path or file format. S for Save.\
  Control+Shift+S

### Help

- About\
  Show the version, author, and license. F1 is help; Alt+F1 is About.\
  Alt+F1
- Alternate Menu\
  Pick a command from a flat, filterable list. F10 is menus: F10 the menu bar, Shift+F10 the context menu, Alt+F10 the alternate menu.\
  Alt+F10
- Command Echo Toggle\
  Z is for sleep: it wakes this behavior or puts it to sleep.\
  Control+Shift+Z
- Documentation\
  Open the documentation in the default browser. F1 is help: this guide.\
  F1
- Elevate Version\
  Check for a newer version on GitHub and offer to install it. F11 is the version: elevate sounds like eleven.\
  F11
- Extra Speech Toggle\
  Z is for sleep: it wakes this behavior or puts it to sleep.\
  Alt+Shift+Z
- History of Changes\
  Open the history of changes in the default browser. F1 is help; Shift+F1 is the history of changes.\
  Shift+F1
- Hotkeys\
  F1 is help; Control+F1 describes one key, and adding Shift describes them all.\
  Control+Shift+F1
- Key Describer Toggle\
  Describe each key instead of running it (Ctrl+F1). F1 is help; Control+F1 describes whatever key you press next.\
  Control+F1
- ReadMe\
  F1 is help; Alt+Shift+F1 is the ReadMe, the short start.\
  Alt+Shift+F1

### Misc

- Append Cell to Clipboard\
  Append the value of the cell under the virtual cursor to the clipboard. C for Cell.\
  Alt+C
- Copy Cell to Clipboard\
  Copy the value of the cell under the virtual cursor to the clipboard. C for Copy.\
  Control+C
- Database Summary\
  D for Database.\
  Alt+D
- Edit Settings\
  Open the Settings dialog. E for Edit.\
  Alt+Shift+E
- Extract with Regex\
  Extract all regex matches in the current virtual column to the clipboard. X for the sound of "ex".\
  Control+Shift+X
- Generate from Grid\
  Alt+Shift+G. Ad-hoc analysis of the VIRTUAL grid (the current filtered, sorted view): profile the columns by type and pick an output -- summary statistics, frequency table, cross-tab, Markdown table, or an Excel chart (bar, pie, histogram, box-whisker, scatter, timeline). One-off and exploratory; acts on what you are looking at. G for Generate.\
  Alt+Shift+G
- Graphics Column\
  Plot the column under the virtual cursor as an Excel chart. G for Graphics.\
  Control+Shift+G
- Hotkey Summary\
  H for Hotkey.\
  Alt+Shift+H
- Refresh View\
  Refresh the visible row set from the database. F5 is refreshing.\
  F5
- Select Columns to Display\
  Choose which columns the grid shows AND their order, via the Available/Chosen sequence picker. S for Select.\
  Alt+S
- Statistics from Column\
  Print descriptive statistics for the column under the virtual cursor. S for Statistics.\
  Alt+Shift+S
- Table Summary\
  Print row count, column count, and storage statistics for the open database. T for Table.\
  Alt+T
- Toggle Read Only\
  Flip the database between read-write and read-only at runtime. Z is for sleep: it wakes this behavior or puts it to sleep.\
  Alt+Z

### Misc, Tools

- Chat about Table\
  F12 is AI; Shift sends the table along with the question.\
  Shift+F12
- Chat with AI\
  F12 is files and AI: F12 talks to the model on this computer.\
  F12
- Edit Snippet\
  E for Edit.\
  Control+Shift+E
- Evaluate Expression\
  Control+Equals
- Invoke Script\
  Run a script from the Scripts folder (.js, .sql, or .dbdo). I for Invoke.\
  Control+Shift+I
- Open Command Prompt\
  Control+Slash
- Open Dot Prompt\
  Send focus to the dot prompt console. Control+Grave opens a console, as it opens the terminal in Visual Studio Code.\
  Control+Grave
- Open in Explorer\
  Open the folder containing the open database in Windows Explorer.\
  Alt+Backslash
- Sqlean Console\
  Open the bundled sqlean.exe shell in its own console window on the current database.\
  Control+Shift+Grave

### Navigate

- Enter Child Table\
  Drill into related records: foreign-key child tables, plus any tables related to this record through the maps table (by kind). Alt+Right Arrow goes forward, and deeper, as in a browser.\
  Alt+RightArrow
- Exit Child Table\
  Return from a child drill-down to the parent table. Alt+Left Arrow goes back, as in a browser.\
  Alt+LeftArrow
- Exit to Root Table\
  Return from any drill-down depth to the original table. Home goes to the top; Alt+Home goes to the top table.\
  Alt+Home
- Find Regex\
  Search across all columns with a .NET regex pattern. F3 searches; Control makes the search a pattern.\
  Control+F3
- Go to Record\
  Jump to a specific row by number. G for Go, as Control+G goes to a line in Windows editors.\
  Control+G
- Jump to Record\
  Search within one chosen column for a substring. J for Jump.\
  Control+J
- Keywords\
  Search across all columns for a substring. K for Keywords.\
  Control+K
- Repeat Go To\
  Alt+G
- Reverse Jump\
  Jump Record backward. J for Jump. Shift reverses Jump to Record.\
  Control+Shift+J
- Reverse Keywords\
  Keywords backward: the previous match across all columns. K for Keywords. Shift reverses Keywords.\
  Control+Shift+K
- Reverse Regex Find\
  Find Regex backward. F3 searches; Control makes it a pattern and Shift reverses it.\
  Control+Shift+F3
- Search Next\
  Repeat the last Find forward. F3 is searching and searching again.\
  F3
- Search Previous\
  Repeat the last Find backward. F3 searches; Shift reverses the direction.\
  Shift+F3

### Query

- Clear Filter\
  Clear the active filter. F for Filter. Shift reverses Filter Records.\
  Control+Shift+F
- Filter Records\
  Show only rows matching one or more field conditions. F for Filter.\
  Control+F
- Inspect Record\
  I for Inspect.\
  Control+I
- Order Records\
  Sort the current table by a chosen column, ascending. O for Order.\
  Alt+O
- Query\
  Q for Query.\
  Control+Q
- Query History\
  Q for Query.\
  Alt+Shift+Q
- Reverse Order\
  O for Order. Shift reverses Order Records.\
  Alt+Shift+O
- Table Properties\
  Alt+Enter

### Query, Say

Shift and a letter asks. Nothing here changes anything; each answers a question about where you are, in one short line.

- Say Added\
  A for Added.\
  Shift+A
- Say Bookmark\
  B for Bookmark.\
  Shift+B
- Say Cell\
  C for Cell.\
  Shift+C
- Say Clipboard\
  Speak the current Windows clipboard text. The apostrophe is a quotation mark, and the clipboard is what you last quoted.\
  Alt+Apostrophe
- Say Column as List\
  Speak every value of the current virtual column, from the top. L for List.\
  Alt+L
- Say Column as List from Current\
  Speak the current virtual column from the current row down. L for List.\
  Control+L
- Say Column as List from Current Marked\
  Speak the current virtual column for marked rows only, from the current row down. L for List.\
  Control+Shift+L
- Say Column as List of Marked\
  Speak the current virtual column for marked rows only, from the top. L for List.\
  Alt+Shift+L
- Say Database\
  Speak the open database's name (single-press) or full path (double-press). D for Database.\
  Shift+D
- Say Edited\
  Speak the current row's 'edited' value in human-friendly local time. E for Edited.\
  Shift+E
- Say Filter\
  F for Filter.\
  Shift+F
- Say Goto\
  Speak the most recently used Jump search string. G for Goto.\
  Shift+G
- Say Id\
  I for ID.\
  Shift+I
- Say Jump\
  J for Jump.\
  Shift+J
- Say Keywords\
  Speak the current Keywords search string. K for Keywords.\
  Shift+K
- Say Look\
  L for Look.\
  Shift+L
- Say Mark Status\
  M for Mark.\
  Shift+M
- Say Marked Rows\
  Speak every marked row in full, scanning from the first row down (all displayed columns). Space marks in a list; Shift and Space asks how many are marked.\
  Shift+Space
- Say Notes\
  Speak the current row's 'notes' field. N for Notes.\
  Shift+N
- Say Order\
  Speak the active sort/order expression. O for Order.\
  Shift+O
- Say Position\
  Speak the column header and 1-based row number of the virtual cell. Delete echoes JAWS, where Insert+Delete says where the cursor is.\
  Alt+Delete
- Say Prime\
  Speak the current record's prime (unique-key) field. P for Prime.\
  Shift+P
- Say Query\
  Q for Query.\
  Shift+Q
- Say Records Rest Marked\
  Speak full marked records from the cursor row down. M for Marked.\
  Alt+Shift+M
- Say Regex Replace\
  X for the sound of "ex".\
  Shift+X
- Say Related\
  R for Related.\
  Shift+R
- Say Replacement Value\
  V for Value.\
  Shift+V
- Say Select Columns\
  S for Say.\
  Shift+S
- Say Sort and Filter\
  Speak the current sort and filter, or '(none)' for each. Shift+8 is the asterisk, the SQL sign for everything: sort and filter together.\
  Shift+8
- Say Status\
  Speak the table, row count, filter, and sort. Z is for sleep: it wakes this behavior or puts it to sleep.\
  Shift+Z
- Say Tags\
  Speak the current row's 'tags' field. T for Tags.\
  Shift+T
- Say URL\
  Speak the current record's url field. U for URL.\
  Shift+U
- Say Yield\
  Speak the current row count (after filter). Y for Yield.\
  Shift+Y

### Window

- Close All But Current Window\
  Close every window except the current one. F4 is open windows; Control+Shift+F4 closes all but this one.\
  Control+Shift+F4
- Close Window\
  Close the current recordset window. F4 is open windows; Control+F4 closes this one, as in Windows.\
  Control+F4
- Current Windows\
  Pick from the list of open recordset windows and activate the chosen one. F4 picks, says or closes open windows: F4 picks one.\
  F4
- Next Window\
  Activate the next open window. Control+Tab moves among DbDo windows.\
  Control+Tab
- Open Table\
  Open a chosen table of the current database in a new window. O for Open.\
  Control+Shift+O
- Previous Window\
  Activate the previous open window. Shift reverses Control+Tab.\
  Control+Shift+Tab
- Say Windows Open\
  Speak the count and titles of open recordset windows, marking the current one. F4 is open windows; Shift+F4 says which are open.\
  Shift+F4

## By key

### Alt

- Alt+Apostrophe\
  Speak the current Windows clipboard text.\
  Say Clipboard
- Alt+B\
  Show saved bookmarks in a listbox; navigate to the selected one.\
  List Bookmarks
- Alt+Backslash\
  Open the folder containing the open database in Windows Explorer.\
  Open in Explorer
- Alt+C\
  Append the value of the cell under the virtual cursor to the clipboard.\
  Append Cell to Clipboard
- Alt+D\
  Database Summary
- Alt+Delete\
  Speak the column header and 1-based row number of the virtual cell.\
  Say Position
- Alt+Enter\
  Table Properties
- Alt+G\
  Repeat Go To
- Alt+Home\
  Return from any drill-down depth to the original table.\
  Exit to Root Table
- Alt+I\
  Build a new DbDo database from another file -- Excel (read through Excel itself, no Access driver needed), Access, dBASE, CSV, or SQLite -- with one standard-shape table per source table, plus maps and lookups.\
  Import
- Alt+L\
  Speak every value of the current virtual column, from the top.\
  Say Column as List
- Alt+LeftArrow\
  Return from a child drill-down to the parent table.\
  Exit Child Table
- Alt+M\
  Merge rows from a Markdown table, JSON, or Inix file into the current table.\
  Merge Data
- Alt+O\
  Sort the current table by a chosen column, ascending.\
  Order Records
- Alt+R\
  Recent Files
- Alt+RightArrow\
  Drill into related records: foreign-key child tables, plus any tables related to this record through the maps table (by kind).\
  Enter Child Table
- Alt+S\
  Choose which columns the grid shows AND their order, via the Available/Chosen sequence picker.\
  Select Columns to Display
- Alt+T\
  Print row count, column count, and storage statistics for the open database.\
  Table Summary
- Alt+X\
  Export the current table or query to CSV, TSV, JSON, or another format.\
  Export Data
- Alt+Z\
  Flip the database between read-write and read-only at runtime.\
  Toggle Read Only

### Alt+Shift

- Alt+Shift+C\
  Append Record to Clipboard
- Alt+Shift+E\
  Open the Settings dialog.\
  Edit Settings
- Alt+Shift+G\
  Alt+Shift+G. Ad-hoc analysis of the VIRTUAL grid (the current filtered, sorted view): profile the columns by type and pick an output -- summary statistics, frequency table, cross-tab, Markdown table, or an Excel chart (bar, pie, histogram, box-whisker, scatter, timeline). One-off and exploratory; acts on what you are looking at.\
  Generate from Grid
- Alt+Shift+H\
  Hotkey Summary
- Alt+Shift+I\
  Invert Marked
- Alt+Shift+L\
  Speak the current virtual column for marked rows only, from the top.\
  Say Column as List of Marked
- Alt+Shift+M\
  Speak full marked records from the cursor row down.\
  Say Records Rest Marked
- Alt+Shift+N\
  Edit Notes
- Alt+Shift+O\
  Reverse Order
- Alt+Shift+Q\
  Query History
- Alt+Shift+R\
  Alt+Shift+R. Render a saved report definition to a Markdown document. A report reads the whole PHYSICAL table (not the grid view), so it is reproducible regardless of your current filter or sort.\
  Run Report
- Alt+Shift+S\
  Print descriptive statistics for the column under the virtual cursor.\
  Statistics from Column
- Alt+Shift+T\
  Edit Tags
- Alt+Shift+U\
  Edit URL
- Alt+Shift+Z\
  Extra Speech Toggle

### Control

- Control+A\
  Mark All
- Control+B\
  Save the current row position as a session bookmark.\
  Save Bookmark
- Control+C\
  Copy the value of the cell under the virtual cursor to the clipboard.\
  Copy Cell to Clipboard
- Control+D\
  Delete the current record (or marked records, when marks exist).\
  Delete Record
- Control+E\
  Edit View
- Control+Enter\
  Open Cell Value
- Control+Equals\
  Evaluate Expression
- Control+F\
  Show only rows matching one or more field conditions.\
  Filter Records
- Control+G\
  Jump to a specific row by number.\
  Go to Record
- Control+Grave\
  Send focus to the dot prompt console.\
  Open Dot Prompt
- Control+I\
  Inspect Record
- Control+J\
  Search within one chosen column for a substring.\
  Jump to Record
- Control+K\
  Search across all columns for a substring.\
  Keywords
- Control+L\
  Speak the current virtual column from the current row down.\
  Say Column as List from Current
- Control+M\
  Set the 'marked' flag on the current row.\
  Mark Record
- Control+N\
  Add a new row to the current table.\
  New Record
- Control+O\
  Open a database file.\
  Open Database
- Control+P\
  Print
- Control+Q\
  Query
- Control+R\
  Substring find-and-replace within the current virtual column, over visible rows.\
  Replace Column
- Control+S\
  Save changes to disk (writes an open workbook back to its .xlsx; a .db is already saved as you go).\
  Save
- Control+Slash\
  Open Command Prompt
- Control+Space\
  Toggle Marked
- Control+T\
  Choose Table
- Control+Tab\
  Activate the next open window.\
  Next Window

### Control+Shift

- Control+Shift+A\
  Unmark All
- Control+Shift+B\
  Forget the saved bookmark.\
  Clear Bookmark
- Control+Shift+C\
  Duplicate the current row as a new row.\
  Copy Record
- Control+Shift+D\
  Delete Without Confirmation
- Control+Shift+E\
  Edit Snippet
- Control+Shift+F\
  Clear the active filter.\
  Clear Filter
- Control+Shift+G\
  Plot the column under the virtual cursor as an Excel chart.\
  Graphics Column
- Control+Shift+Grave\
  Open the bundled sqlean.exe shell in its own console window on the current database.\
  Sqlean Console
- Control+Shift+I\
  Run a script from the Scripts folder (.js, .sql, or .dbdo).\
  Invoke Script
- Control+Shift+J\
  Jump Record backward.\
  Reverse Jump
- Control+Shift+K\
  Keywords backward: the previous match across all columns.\
  Reverse Keywords
- Control+Shift+L\
  Speak the current virtual column for marked rows only, from the current row down.\
  Say Column as List from Current Marked
- Control+Shift+M\
  Clear the 'marked' flag on the current row.\
  Unmark Record
- Control+Shift+N\
  New Copy
- Control+Shift+O\
  Open a chosen table of the current database in a new window.\
  Open Table
- Control+Shift+R\
  Regex find-and-replace within the current virtual column, over visible rows.\
  Regex Replace
- Control+Shift+S\
  Save a copy to another path or file format.\
  Save As
- Control+Shift+T\
  Open Table in New Window
- Control+Shift+Tab\
  Activate the previous open window.\
  Previous Window
- Control+Shift+U\
  Open Url
- Control+Shift+X\
  Extract all regex matches in the current virtual column to the clipboard.\
  Extract with Regex
- Control+Shift+Z\
  Command Echo Toggle

### Function keys

- Alt+F1\
  Show the version, author, and license.\
  About
- Alt+F10\
  Pick a command from a flat, filterable list.\
  Alternate Menu
- Alt+F4\
  Close DbDo.\
  Exit DbDo
- Alt+F6\
  Next Visited Table
- Alt+F8\
  Drop an unmark anchor at the current row.\
  Start Unmark
- Alt+Shift+F1\
  ReadMe
- Alt+Shift+F6\
  Previous Visited Table
- Alt+Shift+F8\
  Unmark every row from the Alt+F8 anchor to the current row.\
  Complete Unmark to Anchor
- Control+F1\
  Describe each key instead of running it (Ctrl+F1).\
  Key Describer Toggle
- Control+F2\
  Pick Value
- Control+F3\
  Search across all columns with a .NET regex pattern.\
  Find Regex
- Control+F4\
  Close the current recordset window.\
  Close Window
- Control+F6\
  Next Table or View
- Control+Shift+F1\
  Hotkeys
- Control+Shift+F3\
  Find Regex backward.\
  Reverse Regex Find
- Control+Shift+F4\
  Close every window except the current one.\
  Close All But Current Window
- Control+Shift+F6\
  Previous Table or View
- F1\
  Open the documentation in the default browser.\
  Documentation
- F11\
  Check for a newer version on GitHub and offer to install it.\
  Elevate Version
- F12\
  Chat with AI
- F2\
  Edit just the field under the virtual cursor.\
  Edit Cell
- F3\
  Repeat the last Find forward.\
  Search Next
- F4\
  Pick from the list of open recordset windows and activate the chosen one.\
  Current Windows
- F5\
  Refresh the visible row set from the database.\
  Refresh View
- F8\
  Drop a mark anchor at the current row.\
  Start Mark
- Shift+F1\
  Open the history of changes in the default browser.\
  History of Changes
- Shift+F12\
  Chat about Table
- Shift+F3\
  Repeat the last Find backward.\
  Search Previous
- Shift+F4\
  Speak the count and titles of open recordset windows, marking the current one.\
  Say Windows Open
- Shift+F8\
  Mark every row from the F8 anchor to the current row.\
  Complete Mark

### Shift

- Shift+8\
  Speak the current sort and filter, or '(none)' for each.\
  Say Sort and Filter
- Shift+A\
  Say Added
- Shift+B\
  Say Bookmark
- Shift+C\
  Say Cell
- Shift+D\
  Speak the open database's name (single-press) or full path (double-press).\
  Say Database
- Shift+E\
  Speak the current row's 'edited' value in human-friendly local time.\
  Say Edited
- Shift+F\
  Say Filter
- Shift+G\
  Speak the most recently used Jump search string.\
  Say Goto
- Shift+I\
  Say Id
- Shift+J\
  Say Jump
- Shift+K\
  Speak the current Keywords search string.\
  Say Keywords
- Shift+L\
  Say Look
- Shift+M\
  Say Mark Status
- Shift+N\
  Speak the current row's 'notes' field.\
  Say Notes
- Shift+O\
  Speak the active sort/order expression.\
  Say Order
- Shift+P\
  Speak the current record's prime (unique-key) field.\
  Say Prime
- Shift+Q\
  Say Query
- Shift+R\
  Say Related
- Shift+S\
  Say Select Columns
- Shift+Space\
  Speak every marked row in full, scanning from the first row down (all displayed columns).\
  Say Marked Rows
- Shift+T\
  Speak the current row's 'tags' field.\
  Say Tags
- Shift+U\
  Speak the current record's url field.\
  Say URL
- Shift+V\
  Say Replacement Value
- Shift+X\
  Say Regex Replace
- Shift+Y\
  Speak the current row count (after filter).\
  Say Yield
- Shift+Z\
  Speak the table, row count, filter, and sort.\
  Say Status

## By command

- About\
  Show the version, author, and license.\
  Alt+F1
- Alternate Menu\
  Pick a command from a flat, filterable list.\
  Alt+F10
- Append Cell to Clipboard\
  Append the value of the cell under the virtual cursor to the clipboard.\
  Alt+C
- Append Record to Clipboard\
  Alt+Shift+C
- Chat about Table\
  Shift+F12
- Chat with AI\
  F12
- Choose Table\
  Control+T
- Clear Bookmark\
  Forget the saved bookmark.\
  Control+Shift+B
- Clear Filter\
  Clear the active filter.\
  Control+Shift+F
- Close All But Current Window\
  Close every window except the current one.\
  Control+Shift+F4
- Close Window\
  Close the current recordset window.\
  Control+F4
- Command Echo Toggle\
  Control+Shift+Z
- Complete Mark\
  Mark every row from the F8 anchor to the current row.\
  Shift+F8
- Complete Unmark to Anchor\
  Unmark every row from the Alt+F8 anchor to the current row.\
  Alt+Shift+F8
- Copy Cell to Clipboard\
  Copy the value of the cell under the virtual cursor to the clipboard.\
  Control+C
- Copy Record\
  Duplicate the current row as a new row.\
  Control+Shift+C
- Current Windows\
  Pick from the list of open recordset windows and activate the chosen one.\
  F4
- Database Summary\
  Alt+D
- Delete Record\
  Delete the current record (or marked records, when marks exist).\
  Control+D
- Delete Without Confirmation\
  Control+Shift+D
- Documentation\
  Open the documentation in the default browser.\
  F1
- Edit Cell\
  Edit just the field under the virtual cursor.\
  F2
- Edit Notes\
  Alt+Shift+N
- Edit Settings\
  Open the Settings dialog.\
  Alt+Shift+E
- Edit Snippet\
  Control+Shift+E
- Edit Tags\
  Alt+Shift+T
- Edit URL\
  Alt+Shift+U
- Edit View\
  Control+E
- Elevate Version\
  Check for a newer version on GitHub and offer to install it.\
  F11
- Enter Child Table\
  Drill into related records: foreign-key child tables, plus any tables related to this record through the maps table (by kind).\
  Alt+RightArrow
- Evaluate Expression\
  Control+Equals
- Exit Child Table\
  Return from a child drill-down to the parent table.\
  Alt+LeftArrow
- Exit DbDo\
  Close DbDo.\
  Alt+F4
- Exit to Root Table\
  Return from any drill-down depth to the original table.\
  Alt+Home
- Export Data\
  Export the current table or query to CSV, TSV, JSON, or another format.\
  Alt+X
- Extra Speech Toggle\
  Alt+Shift+Z
- Extract with Regex\
  Extract all regex matches in the current virtual column to the clipboard.\
  Control+Shift+X
- Filter Records\
  Show only rows matching one or more field conditions.\
  Control+F
- Find Regex\
  Search across all columns with a .NET regex pattern.\
  Control+F3
- Generate from Grid\
  Alt+Shift+G. Ad-hoc analysis of the VIRTUAL grid (the current filtered, sorted view): profile the columns by type and pick an output -- summary statistics, frequency table, cross-tab, Markdown table, or an Excel chart (bar, pie, histogram, box-whisker, scatter, timeline). One-off and exploratory; acts on what you are looking at.\
  Alt+Shift+G
- Go to Record\
  Jump to a specific row by number.\
  Control+G
- Graphics Column\
  Plot the column under the virtual cursor as an Excel chart.\
  Control+Shift+G
- History of Changes\
  Open the history of changes in the default browser.\
  Shift+F1
- Hotkey Summary\
  Alt+Shift+H
- Hotkeys\
  Control+Shift+F1
- Import\
  Build a new DbDo database from another file -- Excel (read through Excel itself, no Access driver needed), Access, dBASE, CSV, or SQLite -- with one standard-shape table per source table, plus maps and lookups.\
  Alt+I
- Inspect Record\
  Control+I
- Invert Marked\
  Alt+Shift+I
- Invoke Script\
  Run a script from the Scripts folder (.js, .sql, or .dbdo).\
  Control+Shift+I
- Jump to Record\
  Search within one chosen column for a substring.\
  Control+J
- Key Describer Toggle\
  Describe each key instead of running it (Ctrl+F1).\
  Control+F1
- Keywords\
  Search across all columns for a substring.\
  Control+K
- List Bookmarks\
  Show saved bookmarks in a listbox; navigate to the selected one.\
  Alt+B
- Mark All\
  Control+A
- Mark Record\
  Set the 'marked' flag on the current row.\
  Control+M
- Merge Data\
  Merge rows from a Markdown table, JSON, or Inix file into the current table.\
  Alt+M
- New Copy\
  Control+Shift+N
- New Record\
  Add a new row to the current table.\
  Control+N
- Next Table or View\
  Control+F6
- Next Visited Table\
  Alt+F6
- Next Window\
  Activate the next open window.\
  Control+Tab
- Open Cell Value\
  Control+Enter
- Open Command Prompt\
  Control+Slash
- Open Database\
  Open a database file.\
  Control+O
- Open Dot Prompt\
  Send focus to the dot prompt console.\
  Control+Grave
- Open in Explorer\
  Open the folder containing the open database in Windows Explorer.\
  Alt+Backslash
- Open Table\
  Open a chosen table of the current database in a new window.\
  Control+Shift+O
- Open Table in New Window\
  Control+Shift+T
- Open Url\
  Control+Shift+U
- Order Records\
  Sort the current table by a chosen column, ascending.\
  Alt+O
- Pick Value\
  Control+F2
- Previous Table or View\
  Control+Shift+F6
- Previous Visited Table\
  Alt+Shift+F6
- Previous Window\
  Activate the previous open window.\
  Control+Shift+Tab
- Print\
  Control+P
- Query\
  Control+Q
- Query History\
  Alt+Shift+Q
- ReadMe\
  Alt+Shift+F1
- Recent Files\
  Alt+R
- Refresh View\
  Refresh the visible row set from the database.\
  F5
- Regex Replace\
  Regex find-and-replace within the current virtual column, over visible rows.\
  Control+Shift+R
- Repeat Go To\
  Alt+G
- Replace Column\
  Substring find-and-replace within the current virtual column, over visible rows.\
  Control+R
- Reverse Jump\
  Jump Record backward.\
  Control+Shift+J
- Reverse Keywords\
  Keywords backward: the previous match across all columns.\
  Control+Shift+K
- Reverse Order\
  Alt+Shift+O
- Reverse Regex Find\
  Find Regex backward.\
  Control+Shift+F3
- Run Report\
  Alt+Shift+R. Render a saved report definition to a Markdown document. A report reads the whole PHYSICAL table (not the grid view), so it is reproducible regardless of your current filter or sort.\
  Alt+Shift+R
- Save\
  Save changes to disk (writes an open workbook back to its .xlsx; a .db is already saved as you go).\
  Control+S
- Save As\
  Save a copy to another path or file format.\
  Control+Shift+S
- Save Bookmark\
  Save the current row position as a session bookmark.\
  Control+B
- Say Added\
  Shift+A
- Say Bookmark\
  Shift+B
- Say Cell\
  Shift+C
- Say Clipboard\
  Speak the current Windows clipboard text.\
  Alt+Apostrophe
- Say Column as List\
  Speak every value of the current virtual column, from the top.\
  Alt+L
- Say Column as List from Current\
  Speak the current virtual column from the current row down.\
  Control+L
- Say Column as List from Current Marked\
  Speak the current virtual column for marked rows only, from the current row down.\
  Control+Shift+L
- Say Column as List of Marked\
  Speak the current virtual column for marked rows only, from the top.\
  Alt+Shift+L
- Say Database\
  Speak the open database's name (single-press) or full path (double-press).\
  Shift+D
- Say Edited\
  Speak the current row's 'edited' value in human-friendly local time.\
  Shift+E
- Say Filter\
  Shift+F
- Say Goto\
  Speak the most recently used Jump search string.\
  Shift+G
- Say Id\
  Shift+I
- Say Jump\
  Shift+J
- Say Keywords\
  Speak the current Keywords search string.\
  Shift+K
- Say Look\
  Shift+L
- Say Mark Status\
  Shift+M
- Say Marked Rows\
  Speak every marked row in full, scanning from the first row down (all displayed columns).\
  Shift+Space
- Say Notes\
  Speak the current row's 'notes' field.\
  Shift+N
- Say Order\
  Speak the active sort/order expression.\
  Shift+O
- Say Position\
  Speak the column header and 1-based row number of the virtual cell.\
  Alt+Delete
- Say Prime\
  Speak the current record's prime (unique-key) field.\
  Shift+P
- Say Query\
  Shift+Q
- Say Records Rest Marked\
  Speak full marked records from the cursor row down.\
  Alt+Shift+M
- Say Regex Replace\
  Shift+X
- Say Related\
  Shift+R
- Say Replacement Value\
  Shift+V
- Say Select Columns\
  Shift+S
- Say Sort and Filter\
  Speak the current sort and filter, or '(none)' for each.\
  Shift+8
- Say Status\
  Speak the table, row count, filter, and sort.\
  Shift+Z
- Say Tags\
  Speak the current row's 'tags' field.\
  Shift+T
- Say URL\
  Speak the current record's url field.\
  Shift+U
- Say Windows Open\
  Speak the count and titles of open recordset windows, marking the current one.\
  Shift+F4
- Say Yield\
  Speak the current row count (after filter).\
  Shift+Y
- Search Next\
  Repeat the last Find forward.\
  F3
- Search Previous\
  Repeat the last Find backward.\
  Shift+F3
- Select Columns to Display\
  Choose which columns the grid shows AND their order, via the Available/Chosen sequence picker.\
  Alt+S
- Sqlean Console\
  Open the bundled sqlean.exe shell in its own console window on the current database.\
  Control+Shift+Grave
- Start Mark\
  Drop a mark anchor at the current row.\
  F8
- Start Unmark\
  Drop an unmark anchor at the current row.\
  Alt+F8
- Statistics from Column\
  Print descriptive statistics for the column under the virtual cursor.\
  Alt+Shift+S
- Table Properties\
  Alt+Enter
- Table Summary\
  Print row count, column count, and storage statistics for the open database.\
  Alt+T
- Toggle Marked\
  Control+Space
- Toggle Read Only\
  Flip the database between read-write and read-only at runtime.\
  Alt+Z
- Unmark All\
  Control+Shift+A
- Unmark Record\
  Clear the 'marked' flag on the current row.\
  Control+Shift+M
