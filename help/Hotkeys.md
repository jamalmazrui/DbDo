# DbDo Hotkeys

Every key DbDo binds, three ways: by menu, by key, and by command. The menu
section says why each key is the one it is.

## How the keys are chosen

- **A letter is the first letter of a word in the command.** Shift+C is Say Cell; Alt+O is Order.
- **Shift and a letter asks.** Every Say command is Shift and a letter, and none changes anything.
- **Adding Shift reverses.** Control+M marks and Control+Shift+M unmarks; Control+F filters and Control+Shift+F clears the filter.
- **Z is for sleep.** Every Z key is a toggle: it wakes a behavior or puts it to sleep. Control+Z undoes in Windows, and restoring a behavior is the same idea.
- **X is for the sound of "ex"**, as in Export.
- **Alt takes the letter when Control already means something in Windows.** Control+O opens and Control+S saves everywhere, so Order is Alt+O and Select Columns is Alt+S.
- **Each function key is a family,** from Windows and Office habits, extended: F1 help, F2 editing, F3 searching, F4 picking, saying or closing open windows, F5 refreshing, F6 moving among parts of the window, F7 review, F8 selection, F9 a more readable view, F10 menus, F11 the version (elevate sounds like eleven), F12 files and AI.
- **Control+Tab moves among DbDo windows,** as it moves among tabs everywhere else.

## By menu

### Edit

- Append Clipboard\
  Add the clipboard text to the end of a file you name.\
  Control+Shift+Apostrophe
- Append Record\
  Alt+Shift+C
- Clear Bookmark\
  Forget the saved bookmark. B for Bookmark. Shift reverses Save Bookmark.\
  Control+Shift+B
- Clear Clipboard\
  Empty the clipboard. Shift reverses Say Clipboard.\
  Alt+Shift+Apostrophe
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
- Edit Record\
  Edit every field of the current row. E for Edit.\
  Control+E
- Edit Tags\
  T for Tags.\
  Alt+Shift+T
- Edit url\
  U for url.\
  Alt+Shift+U
- List Bookmarks\
  Show saved bookmarks in a listbox; navigate to the selected one. B for Bookmarks.\
  Alt+B
- Mark Record\
  Set the 'marked' flag on the current row. M for Mark.\
  Control+M
- Marked Toggle\
  Control+Space toggles a selection in any Windows list.\
  Control+Space
- New Copy\
  N for New.\
  Control+Shift+N
- New Record\
  Add a new record at the cursor. N for New.\
  Control+N
- Open Cell Value\
  Control+Enter acts from anywhere in a Homer dialog; here it opens the cell's value.\
  Control+Enter
- Open url\
  U for url.\
  Control+Shift+U
- Pick Value\
  F2 is editing; Control+F2 edits by picking a value.\
  Control+F2
- Play Stream\
  Play the current record's stream address in the Homer Player; with records marked, play all of them. P for Play.\
  Control+P
- Replace\
  R for Replace.\
  Control+R
- Replace with Regex\
  R for Replace.\
  Control+Shift+R
- Save Bookmark\
  Save the current row position as a session bookmark. B for Bookmark.\
  Control+B
- Save Clipboard\
  Save the clipboard text to a file you name.\
  Control+Apostrophe
- Unmark Record\
  Clear the 'marked' flag on the current row. Shift reverses Mark: M for Mark.\
  Control+Shift+M

### Edit, Bulk Marking

F8 starts a run and Shift completes it; Alt makes the run an unmark instead of a mark.

- Complete Mark\
  Mark every row from the F8 anchor to the current row. F8 is selection; Shift completes the run F8 started.\
  Shift+F8
- Complete Unmark\
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
  Export the records in view, with the shown columns, to a file. X for the sound of "ex".\
  Alt+Shift+X
- Import Data\
  Build a new DbDo database from another file -- Excel (read by DbDo itself, with no Office or Access driver needed), Access, dBASE, CSV, or SQLite -- with one standard-shape table per source table, plus maps and defs. I for Import.\
  Alt+I
- Merge Data\
  Merge rows from a Markdown table, JSON, or Inix file into the current table. M for Merge.\
  Alt+M
- Next Table or View\
  F6 moves among parts of the window; Control+F6 moves to the next table.\
  Control+F6
- Open Database\
  Open a database file. O for Open.\
  Control+O
- Open Template Database\
  Open one of the template databases that come with DbDo, such as RadioTrail or JobTrail. D for Database.\
  Alt+Shift+D
- Previous Table or View\
  F6 moves among parts; Shift reverses Control+F6.\
  Control+Shift+F6
- Recent Databases\
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
  Pick any command from one list, in menu, name or key order. F10 is menus: F10 the menu bar, Shift+F10 the context menu, Alt+F10 the alternate menu.\
  Alt+F10
- Copy Log\
  Copy the session log to the clipboard as a file, ready to paste as an attachment.\
  Control+F12
- Documentation\
  Open the documentation in the default browser. F1 is help: this guide.\
  F1
- Elevate Version\
  Check for a newer version on GitHub and offer to install it. F11 is the version: elevate sounds like eleven.\
  F11
- History of Changes\
  Open the history of changes in the default browser. F1 is help; Control+Shift+F1 is the history of changes.\
  Control+Shift+F1
- Key Describer Toggle\
  Describe each key instead of running it (Ctrl+F1). F1 is help; Control+F1 describes whatever key you press next.\
  Control+F1
- Play Tutorials\
  Alt+F12
- ReadMe\
  F1 is help; Alt+Shift+F1 is the ReadMe, the short start.\
  Alt+Shift+F1
- What This Does\
  F1 is help; Shift+F1 says what the control with focus does.\
  Shift+F1
- Where Am I\
  Show the table, row, filter, and sort in a read-only dialog. Control+Slash lists the keys, as in Google Docs, Slack and ChatGPT.\
  Control+Slash

### Misc

- Append Cell\
  Append the value of the cell under the virtual cursor to the clipboard. C for Cell.\
  Alt+C
- Copy Cell\
  Copy the value of the cell under the virtual cursor to the clipboard. C for Copy.\
  Control+C
- Database Summary\
  D for Database.\
  Alt+D
- Edit Settings\
  Open the Settings dialog. E for Edit.\
  Alt+Shift+E
- Generate from Grid\
  Make a report from the records in the grid. G for Generate.\
  Alt+Shift+G
- Read Only Toggle\
  Flip the database between read-write and read-only at runtime. Z is for sleep: it wakes this behavior or puts it to sleep.\
  Control+Shift+Z
- Refresh Records\
  Refresh the records' values from the database. F5 is refreshing.\
  F5
- Select Columns\
  Choose the columns the grid shows and their order. S for Select.\
  Alt+S
- Table Summary\
  Show a summary of the table: fields, types and keys. T for Table.\
  Alt+T

### Misc, Tools

- Chat about Table\
  F12 is AI; Shift sends the table along with the question.\
  Shift+F12
- Chat with AI\
  F12 is files and AI: F12 talks to the model on this computer.\
  F12
- Edit Script\
  S for Script.\
  Alt+Shift+S
- Evaluate Expression\
  Control+Equals
- Invoke Script\
  Run a script from the Scripts folder (.js, .sql, or .dbdo).\
  Alt+V
- Open Command Prompt\
  Control+Backslash
- Open Dot Prompt\
  Send focus to the dot prompt console. Control+Grave opens a console, as it opens the terminal in Visual Studio Code.\
  Control+Grave
- Open in Explorer\
  Open the folder of the open database in Windows Explorer.\
  Alt+Backslash
- Show in FileDir\
  Show the open database's file, selected in its folder, in FileDir.\
  Control+Shift+Oem5
- Sqlean Console\
  Open the bundled sqlean.exe shell in its own console window on the current database.\
  Control+Shift+Grave

### Navigate

- Enter Child Table\
  Go into the records related to this one. Alt+Right Arrow goes forward, and deeper, as in a browser.\
  Alt+RightArrow
- Exit Child Table\
  Return from a child drill-down to the parent table. Alt+Left Arrow goes back, as in a browser.\
  Alt+LeftArrow
- Exit to Root Table\
  Go back to the first table. Home goes to the top; Alt+Home goes to the top table.\
  Alt+Home
- Find Regex\
  Find the next record matching a regular expression. F3 searches; Control makes the search a pattern.\
  Control+F3
- Go to Record\
  Go to a record by its number, a percent or text. G for Go, as Control+G goes to a line in Windows editors.\
  Control+G
- Jump\
  Go to the next record whose current field holds the text. J for Jump.\
  Control+J
- Keywords\
  Go to the next record whose fields hold the keywords. K for Keywords.\
  Control+K
- Repeat Go to\
  G for Go.\
  Alt+G
- Reverse Find Regex\
  Find the previous record matching a regular expression. F3 searches; Control makes it a pattern and Shift reverses it.\
  Control+Shift+F3
- Reverse Jump\
  Go to the previous record whose current field holds the text. J for Jump. Shift reverses Jump.\
  Control+Shift+J
- Reverse Keywords\
  Go to the previous record whose fields hold the keywords. K for Keywords. Shift reverses Keywords.\
  Control+Shift+K
- Search Next\
  Repeat the last Find forward. F3 is searching and searching again.\
  F3
- Search Previous\
  Repeat the last Find backward. F3 searches; Shift reverses the direction.\
  Shift+F3

### Query

- Add Column to Order\
  A for Add.\
  Alt+A
- Add Column to Order Descending\
  A for Add.\
  Alt+Shift+A
- Clear Filter\
  Clear the active filter. F for Filter. Shift reverses Filter Records.\
  Control+Shift+F
- Extract with Regex\
  E for Extract.\
  Control+Shift+E
- Filter Records\
  Show only the records that match what you choose. F for Filter.\
  Control+F
- Inspect Record\
  I for Inspect.\
  Control+I
- Order Records\
  Order the records by the fields you choose. O for Order.\
  Alt+O
- Query\
  Q for Query.\
  Control+Q
- Query History\
  Q for Query.\
  Alt+Shift+Q
- Reverse Order\
  Reverse the order of the records. O for Order. Shift reverses Order Records.\
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
  Say the column's values from the current record down. L for List.\
  Control+L
- Say Column as List from Current Marked\
  Say the column's values for marked records from the current record down. L for List.\
  Control+Shift+L
- Say Column as List of Marked\
  Speak the current virtual column for marked rows only, from the top. L for List.\
  Alt+Shift+L
- Say Database\
  Say the database's name and folder. D for Database.\
  Shift+D
- Say Edited\
  Say when the record was last edited. E for Edited.\
  Shift+E
- Say Filter\
  F for Filter.\
  Shift+F
- Say Go to\
  Say the last Go to entry. G for Go.\
  Shift+G
- Say Id\
  I for ID.\
  Shift+I
- Say Invoke Script\
  Shift+V
- Say Jump\
  J for Jump.\
  Shift+J
- Say Keywords\
  Say the last Keywords entry. K for Keywords.\
  Shift+K
- Say Look\
  L for Look.\
  Shift+L
- Say Marked\
  M for Marked.\
  Shift+M
- Say Notes\
  Say the record's notes. N for Notes.\
  Shift+N
- Say Order\
  Say the order in force. O for Order.\
  Shift+O
- Say Order and Filter\
  Speak the current order and filter, or none for each. Shift+8 is the asterisk, the SQL sign for everything: sort and filter together.\
  Shift+8
- Say Path\
  Speak the open database's full path; press twice to show it. P for Path.\
  Alt+P
- Say Position\
  Speak the column header and 1-based row number of the virtual cell. Delete echoes JAWS, where Insert+Delete says where the cursor is.\
  Alt+Delete
- Say Prime\
  Say the record's prime fields. P for Prime.\
  Shift+P
- Say Query\
  Q for Query.\
  Shift+Q
- Say Records from Current Marked\
  Speak every marked record in full, from the current record down. M for Marked.\
  Alt+Shift+M
- Say Records of Marked\
  Speak every marked record in full, from the first record down (all displayed columns). Space marks in a list; Shift and Space asks how many are marked.\
  Shift+Space
- Say Replace Values\
  R for Replace.\
  Shift+R
- Say Select Columns\
  S for Say.\
  Shift+S
- Say Status\
  Say the table, record, order and filter. Z is for sleep: it wakes this behavior or puts it to sleep.\
  Shift+Z
- Say Tags\
  Say the record's tags. T for Tags.\
  Shift+T
- Say Time\
  Speak the current date and time.\
  Alt+Semicolon
- Say url\
  Say the record's url. U for url.\
  Shift+U
- Say Whole Record\
  W for Whole.\
  Shift+W
- Say Yield\
  Say how many records are in view. Y for Yield.\
  Alt+Y
- Say Yield Marked\
  Say how many records are marked. Y for Yield.\
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
  Open a table in a new window. O for Open.\
  Control+Shift+O
- Previous Window\
  Activate the previous open window. Shift reverses Control+Tab.\
  Control+Shift+Tab
- Say Windows Open\
  Speak the count and titles of open recordset windows, marking the current one. F4 is open windows; Shift+F4 says which are open.\
  Shift+F4

## By key

### Alt

- Alt+A\
  Add Column to Order
- Alt+Apostrophe\
  Speak the current Windows clipboard text.\
  Say Clipboard
- Alt+B\
  Show saved bookmarks in a listbox; navigate to the selected one.\
  List Bookmarks
- Alt+Backslash\
  Open the folder of the open database in Windows Explorer.\
  Open in Explorer
- Alt+C\
  Append the value of the cell under the virtual cursor to the clipboard.\
  Append Cell
- Alt+D\
  Database Summary
- Alt+Delete\
  Speak the column header and 1-based row number of the virtual cell.\
  Say Position
- Alt+Enter\
  Table Properties
- Alt+G\
  Repeat Go to
- Alt+Home\
  Go back to the first table.\
  Exit to Root Table
- Alt+I\
  Build a new DbDo database from another file -- Excel (read by DbDo itself, with no Office or Access driver needed), Access, dBASE, CSV, or SQLite -- with one standard-shape table per source table, plus maps and defs.\
  Import Data
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
  Order the records by the fields you choose.\
  Order Records
- Alt+P\
  Speak the open database's full path; press twice to show it.\
  Say Path
- Alt+R\
  Recent Databases
- Alt+RightArrow\
  Go into the records related to this one.\
  Enter Child Table
- Alt+S\
  Choose the columns the grid shows and their order.\
  Select Columns
- Alt+Semicolon\
  Speak the current date and time.\
  Say Time
- Alt+T\
  Show a summary of the table: fields, types and keys.\
  Table Summary
- Alt+V\
  Run a script from the Scripts folder (.js, .sql, or .dbdo).\
  Invoke Script
- Alt+Y\
  Say how many records are in view.\
  Say Yield

### Alt+Shift

- Alt+Shift+A\
  Add Column to Order Descending
- Alt+Shift+Apostrophe\
  Empty the clipboard.\
  Clear Clipboard
- Alt+Shift+C\
  Append Record
- Alt+Shift+D\
  Open one of the template databases that come with DbDo, such as RadioTrail or JobTrail.\
  Open Template Database
- Alt+Shift+E\
  Open the Settings dialog.\
  Edit Settings
- Alt+Shift+G\
  Make a report from the records in the grid.\
  Generate from Grid
- Alt+Shift+I\
  Invert Marked
- Alt+Shift+L\
  Speak the current virtual column for marked rows only, from the top.\
  Say Column as List of Marked
- Alt+Shift+M\
  Speak every marked record in full, from the current record down.\
  Say Records from Current Marked
- Alt+Shift+N\
  Edit Notes
- Alt+Shift+O\
  Reverse the order of the records.\
  Reverse Order
- Alt+Shift+Q\
  Query History
- Alt+Shift+R\
  Alt+Shift+R. Render a saved report definition to a Markdown document. A report reads the whole PHYSICAL table (not the grid view), so it is reproducible regardless of your current filter or sort.\
  Run Report
- Alt+Shift+S\
  Edit Script
- Alt+Shift+T\
  Edit Tags
- Alt+Shift+U\
  Edit url
- Alt+Shift+X\
  Export the records in view, with the shown columns, to a file.\
  Export Data

### Control

- Control+A\
  Mark All
- Control+Apostrophe\
  Save the clipboard text to a file you name.\
  Save Clipboard
- Control+B\
  Save the current row position as a session bookmark.\
  Save Bookmark
- Control+Backslash\
  Open Command Prompt
- Control+C\
  Copy the value of the cell under the virtual cursor to the clipboard.\
  Copy Cell
- Control+D\
  Delete the current record (or marked records, when marks exist).\
  Delete Record
- Control+E\
  Edit every field of the current row.\
  Edit Record
- Control+Enter\
  Open Cell Value
- Control+Equals\
  Evaluate Expression
- Control+F\
  Show only the records that match what you choose.\
  Filter Records
- Control+G\
  Go to a record by its number, a percent or text.\
  Go to Record
- Control+Grave\
  Send focus to the dot prompt console.\
  Open Dot Prompt
- Control+I\
  Inspect Record
- Control+J\
  Go to the next record whose current field holds the text.\
  Jump
- Control+K\
  Go to the next record whose fields hold the keywords.\
  Keywords
- Control+L\
  Say the column's values from the current record down.\
  Say Column as List from Current
- Control+M\
  Set the 'marked' flag on the current row.\
  Mark Record
- Control+N\
  Add a new record at the cursor.\
  New Record
- Control+O\
  Open a database file.\
  Open Database
- Control+P\
  Play the current record's stream address in the Homer Player; with records marked, play all of them.\
  Play Stream
- Control+Q\
  Query
- Control+R\
  Replace
- Control+S\
  Save changes to disk (writes an open workbook back to its .xlsx; a .db is already saved as you go).\
  Save
- Control+Slash\
  Show the table, row, filter, and sort in a read-only dialog.\
  Where Am I
- Control+Space\
  Marked Toggle
- Control+T\
  Choose Table
- Control+Tab\
  Activate the next open window.\
  Next Window

### Control+Shift

- Control+Shift+A\
  Unmark All
- Control+Shift+Apostrophe\
  Add the clipboard text to the end of a file you name.\
  Append Clipboard
- Control+Shift+B\
  Forget the saved bookmark.\
  Clear Bookmark
- Control+Shift+C\
  Duplicate the current row as a new row.\
  Copy Record
- Control+Shift+D\
  Delete Without Confirmation
- Control+Shift+E\
  Extract with Regex
- Control+Shift+F\
  Clear the active filter.\
  Clear Filter
- Control+Shift+Grave\
  Open the bundled sqlean.exe shell in its own console window on the current database.\
  Sqlean Console
- Control+Shift+J\
  Go to the previous record whose current field holds the text.\
  Reverse Jump
- Control+Shift+K\
  Go to the previous record whose fields hold the keywords.\
  Reverse Keywords
- Control+Shift+L\
  Say the column's values for marked records from the current record down.\
  Say Column as List from Current Marked
- Control+Shift+M\
  Clear the 'marked' flag on the current row.\
  Unmark Record
- Control+Shift+N\
  New Copy
- Control+Shift+O\
  Open a table in a new window.\
  Open Table
- Control+Shift+Oem5\
  Show the open database's file, selected in its folder, in FileDir.\
  Show in FileDir
- Control+Shift+R\
  Replace with Regex
- Control+Shift+S\
  Save a copy to another path or file format.\
  Save As
- Control+Shift+Tab\
  Activate the previous open window.\
  Previous Window
- Control+Shift+U\
  Open url
- Control+Shift+Z\
  Flip the database between read-write and read-only at runtime.\
  Read Only Toggle

### Function keys

- Alt+F1\
  Show the version, author, and license.\
  About
- Alt+F10\
  Pick any command from one list, in menu, name or key order.\
  Alternate Menu
- Alt+F12\
  Play Tutorials
- Alt+F4\
  Close DbDo.\
  Exit DbDo
- Alt+F8\
  Drop an unmark anchor at the current row.\
  Start Unmark
- Alt+Shift+F1\
  ReadMe
- Alt+Shift+F8\
  Unmark every row from the Alt+F8 anchor to the current row.\
  Complete Unmark
- Control+F1\
  Describe each key instead of running it (Ctrl+F1).\
  Key Describer Toggle
- Control+F12\
  Copy the session log to the clipboard as a file, ready to paste as an attachment.\
  Copy Log
- Control+F2\
  Pick Value
- Control+F3\
  Find the next record matching a regular expression.\
  Find Regex
- Control+F4\
  Close the current recordset window.\
  Close Window
- Control+F6\
  Next Table or View
- Control+Shift+F1\
  Open the history of changes in the default browser.\
  History of Changes
- Control+Shift+F3\
  Find the previous record matching a regular expression.\
  Reverse Find Regex
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
  Refresh the records' values from the database.\
  Refresh Records
- F8\
  Drop a mark anchor at the current row.\
  Start Mark
- Shift+F1\
  What This Does
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
  Speak the current order and filter, or none for each.\
  Say Order and Filter
- Shift+A\
  Say Added
- Shift+B\
  Say Bookmark
- Shift+C\
  Say Cell
- Shift+D\
  Say the database's name and folder.\
  Say Database
- Shift+E\
  Say when the record was last edited.\
  Say Edited
- Shift+F\
  Say Filter
- Shift+G\
  Say the last Go to entry.\
  Say Go to
- Shift+I\
  Say Id
- Shift+J\
  Say Jump
- Shift+K\
  Say the last Keywords entry.\
  Say Keywords
- Shift+L\
  Say Look
- Shift+M\
  Say Marked
- Shift+N\
  Say the record's notes.\
  Say Notes
- Shift+O\
  Say the order in force.\
  Say Order
- Shift+P\
  Say the record's prime fields.\
  Say Prime
- Shift+Q\
  Say Query
- Shift+R\
  Say Replace Values
- Shift+S\
  Say Select Columns
- Shift+Space\
  Speak every marked record in full, from the first record down (all displayed columns).\
  Say Records of Marked
- Shift+T\
  Say the record's tags.\
  Say Tags
- Shift+U\
  Say the record's url.\
  Say url
- Shift+V\
  Say Invoke Script
- Shift+W\
  Say Whole Record
- Shift+Y\
  Say how many records are marked.\
  Say Yield Marked
- Shift+Z\
  Say the table, record, order and filter.\
  Say Status

## By command

- About\
  Show the version, author, and license.\
  Alt+F1
- Add Column to Order\
  Alt+A
- Add Column to Order Descending\
  Alt+Shift+A
- Alternate Menu\
  Pick any command from one list, in menu, name or key order.\
  Alt+F10
- Append Cell\
  Append the value of the cell under the virtual cursor to the clipboard.\
  Alt+C
- Append Clipboard\
  Add the clipboard text to the end of a file you name.\
  Control+Shift+Apostrophe
- Append Record\
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
- Clear Clipboard\
  Empty the clipboard.\
  Alt+Shift+Apostrophe
- Clear Filter\
  Clear the active filter.\
  Control+Shift+F
- Close All But Current Window\
  Close every window except the current one.\
  Control+Shift+F4
- Close Window\
  Close the current recordset window.\
  Control+F4
- Complete Mark\
  Mark every row from the F8 anchor to the current row.\
  Shift+F8
- Complete Unmark\
  Unmark every row from the Alt+F8 anchor to the current row.\
  Alt+Shift+F8
- Copy Cell\
  Copy the value of the cell under the virtual cursor to the clipboard.\
  Control+C
- Copy Log\
  Copy the session log to the clipboard as a file, ready to paste as an attachment.\
  Control+F12
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
- Edit Record\
  Edit every field of the current row.\
  Control+E
- Edit Script\
  Alt+Shift+S
- Edit Settings\
  Open the Settings dialog.\
  Alt+Shift+E
- Edit Tags\
  Alt+Shift+T
- Edit url\
  Alt+Shift+U
- Elevate Version\
  Check for a newer version on GitHub and offer to install it.\
  F11
- Enter Child Table\
  Go into the records related to this one.\
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
  Go back to the first table.\
  Alt+Home
- Export Data\
  Export the records in view, with the shown columns, to a file.\
  Alt+Shift+X
- Extract with Regex\
  Control+Shift+E
- Filter Records\
  Show only the records that match what you choose.\
  Control+F
- Find Regex\
  Find the next record matching a regular expression.\
  Control+F3
- Generate from Grid\
  Make a report from the records in the grid.\
  Alt+Shift+G
- Go to Record\
  Go to a record by its number, a percent or text.\
  Control+G
- History of Changes\
  Open the history of changes in the default browser.\
  Control+Shift+F1
- Import Data\
  Build a new DbDo database from another file -- Excel (read by DbDo itself, with no Office or Access driver needed), Access, dBASE, CSV, or SQLite -- with one standard-shape table per source table, plus maps and defs.\
  Alt+I
- Inspect Record\
  Control+I
- Invert Marked\
  Alt+Shift+I
- Invoke Script\
  Run a script from the Scripts folder (.js, .sql, or .dbdo).\
  Alt+V
- Jump\
  Go to the next record whose current field holds the text.\
  Control+J
- Key Describer Toggle\
  Describe each key instead of running it (Ctrl+F1).\
  Control+F1
- Keywords\
  Go to the next record whose fields hold the keywords.\
  Control+K
- List Bookmarks\
  Show saved bookmarks in a listbox; navigate to the selected one.\
  Alt+B
- Mark All\
  Control+A
- Mark Record\
  Set the 'marked' flag on the current row.\
  Control+M
- Marked Toggle\
  Control+Space
- Merge Data\
  Merge rows from a Markdown table, JSON, or Inix file into the current table.\
  Alt+M
- New Copy\
  Control+Shift+N
- New Record\
  Add a new record at the cursor.\
  Control+N
- Next Table or View\
  Control+F6
- Next Window\
  Activate the next open window.\
  Control+Tab
- Open Cell Value\
  Control+Enter
- Open Command Prompt\
  Control+Backslash
- Open Database\
  Open a database file.\
  Control+O
- Open Dot Prompt\
  Send focus to the dot prompt console.\
  Control+Grave
- Open in Explorer\
  Open the folder of the open database in Windows Explorer.\
  Alt+Backslash
- Open Table\
  Open a table in a new window.\
  Control+Shift+O
- Open Template Database\
  Open one of the template databases that come with DbDo, such as RadioTrail or JobTrail.\
  Alt+Shift+D
- Open url\
  Control+Shift+U
- Order Records\
  Order the records by the fields you choose.\
  Alt+O
- Pick Value\
  Control+F2
- Play Stream\
  Play the current record's stream address in the Homer Player; with records marked, play all of them.\
  Control+P
- Play Tutorials\
  Alt+F12
- Previous Table or View\
  Control+Shift+F6
- Previous Window\
  Activate the previous open window.\
  Control+Shift+Tab
- Query\
  Control+Q
- Query History\
  Alt+Shift+Q
- Read Only Toggle\
  Flip the database between read-write and read-only at runtime.\
  Control+Shift+Z
- ReadMe\
  Alt+Shift+F1
- Recent Databases\
  Alt+R
- Refresh Records\
  Refresh the records' values from the database.\
  F5
- Repeat Go to\
  Alt+G
- Replace\
  Control+R
- Replace with Regex\
  Control+Shift+R
- Reverse Find Regex\
  Find the previous record matching a regular expression.\
  Control+Shift+F3
- Reverse Jump\
  Go to the previous record whose current field holds the text.\
  Control+Shift+J
- Reverse Keywords\
  Go to the previous record whose fields hold the keywords.\
  Control+Shift+K
- Reverse Order\
  Reverse the order of the records.\
  Alt+Shift+O
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
- Save Clipboard\
  Save the clipboard text to a file you name.\
  Control+Apostrophe
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
  Say the column's values from the current record down.\
  Control+L
- Say Column as List from Current Marked\
  Say the column's values for marked records from the current record down.\
  Control+Shift+L
- Say Column as List of Marked\
  Speak the current virtual column for marked rows only, from the top.\
  Alt+Shift+L
- Say Database\
  Say the database's name and folder.\
  Shift+D
- Say Edited\
  Say when the record was last edited.\
  Shift+E
- Say Filter\
  Shift+F
- Say Go to\
  Say the last Go to entry.\
  Shift+G
- Say Id\
  Shift+I
- Say Invoke Script\
  Shift+V
- Say Jump\
  Shift+J
- Say Keywords\
  Say the last Keywords entry.\
  Shift+K
- Say Look\
  Shift+L
- Say Marked\
  Shift+M
- Say Notes\
  Say the record's notes.\
  Shift+N
- Say Order\
  Say the order in force.\
  Shift+O
- Say Order and Filter\
  Speak the current order and filter, or none for each.\
  Shift+8
- Say Path\
  Speak the open database's full path; press twice to show it.\
  Alt+P
- Say Position\
  Speak the column header and 1-based row number of the virtual cell.\
  Alt+Delete
- Say Prime\
  Say the record's prime fields.\
  Shift+P
- Say Query\
  Shift+Q
- Say Records from Current Marked\
  Speak every marked record in full, from the current record down.\
  Alt+Shift+M
- Say Records of Marked\
  Speak every marked record in full, from the first record down (all displayed columns).\
  Shift+Space
- Say Replace Values\
  Shift+R
- Say Select Columns\
  Shift+S
- Say Status\
  Say the table, record, order and filter.\
  Shift+Z
- Say Tags\
  Say the record's tags.\
  Shift+T
- Say Time\
  Speak the current date and time.\
  Alt+Semicolon
- Say url\
  Say the record's url.\
  Shift+U
- Say Whole Record\
  Shift+W
- Say Windows Open\
  Speak the count and titles of open recordset windows, marking the current one.\
  Shift+F4
- Say Yield\
  Say how many records are in view.\
  Alt+Y
- Say Yield Marked\
  Say how many records are marked.\
  Shift+Y
- Search Next\
  Repeat the last Find forward.\
  F3
- Search Previous\
  Repeat the last Find backward.\
  Shift+F3
- Select Columns\
  Choose the columns the grid shows and their order.\
  Alt+S
- Show in FileDir\
  Show the open database's file, selected in its folder, in FileDir.\
  Control+Shift+Oem5
- Sqlean Console\
  Open the bundled sqlean.exe shell in its own console window on the current database.\
  Control+Shift+Grave
- Start Mark\
  Drop a mark anchor at the current row.\
  F8
- Start Unmark\
  Drop an unmark anchor at the current row.\
  Alt+F8
- Table Properties\
  Alt+Enter
- Table Summary\
  Show a summary of the table: fields, types and keys.\
  Alt+T
- Unmark All\
  Control+Shift+A
- Unmark Record\
  Clear the 'marked' flag on the current row.\
  Control+Shift+M
- What This Does\
  Shift+F1
- Where Am I\
  Show the table, row, filter, and sort in a read-only dialog.\
  Control+Slash
