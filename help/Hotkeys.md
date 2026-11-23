# DbDo Hotkeys

Every key DbDo binds, three ways: by menu, by key, and by command. The menu
section says why each key is the one it is.

## How the keys are chosen

- **A letter is the first letter of a word in the command.** Shift+C is Say Cell; Alt+O is Order.
- **Shift and a letter asks.** Every Say command is Shift and a letter, and none changes anything.
- **Adding Shift reverses.** Control+M marks and Control+Shift+M unmarks; Control+F filters and Control+Shift+F clears the filter.
- **Z is for sleep.** Every Z key is a toggle: it wakes a behavior or puts it to sleep. Control+Z undoes in Windows, and restoring a behavior is the same idea.
- **X is for the sound of "ex"**, as in Export.
- **Alt says, Control changes.** When a letter needs a modifier, a command that only speaks takes Alt and one that changes data takes Control. A command that opens a pick list may take either; a plain Alt and a letter never opens a box for typing.
- **W, L and Y read and count the grid.** W is wide, every shown column; L is lean, the current column; Y is yield, a count. Alt starts from the first record, Control from the current one, and Shift keeps only the marked records.
- **Alt takes the letter when Control already means something in Windows.** Control+O opens and Control+S saves everywhere, so Order is Alt+O and Show Columns is Alt+S.
- **Each function key is a family,** from Windows and Office habits, extended: F1 help, F2 editing, F3 searching, F4 picking, saying or closing open windows, F5 refreshing, F6 moving among parts of the window, F7 review, F8 selection, F9 a more readable view, F10 menus, F11 the version (elevate sounds like eleven), F12 files and AI.
- **Control+Tab moves among DbDo windows,** as it moves among tabs everywhere else.

## By menu

### Edit

- Append Clipboard\
  Add the clipboard text to the end of a file you name.\
  Control+Shift+Apostrophe
- Clear Bookmark\
  Forget the saved bookmark. B for Bookmark. Shift reverses Save Bookmark.\
  Control+Shift+B
- Clear Clipboard\
  Empty the clipboard. Shift reverses Say Clipboard.\
  Alt+Shift+Apostrophe
- Copy Append Record\
  C for Copy.\
  Alt+Shift+C
- Copy Record\
  Duplicate the current row as a new row. C for Copy.\
  Control+Shift+C
- Delete Record\
  Delete the current record (or marked records, when marks exist). D for Delete.\
  Control+D
- Delete without Confirmation\
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
- Play Media\
  Play the current record's stream address in the Homer Player; with records marked, play all of them. P for Play.\
  Control+P
- Record Playback\
  P for Playback.\
  Control+Shift+P
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
- Toggle Mark\
  Control+Space toggles a selection in any Windows list.\
  Control+Space
- Unmark Record\
  Clear the 'marked' flag on the current row. Shift reverses Mark: M for Mark.\
  Control+Shift+M

### Edit, Bulk Marking

F8 starts a run and Shift completes it; Alt makes the run an unmark instead of a mark.

- Complete Mark\
  Mark every row from the F8 start to the current row. F8 is selection; Shift completes the run F8 started.\
  Shift+F8
- Complete Unmark\
  Unmark every row from the F8 start to the current row. F8 is selection; Shift completes the run Alt+F8 started.\
  Alt+Shift+F8
- Invert Marked\
  I for Invert.\
  Control+I
- Mark All\
  A for All.\
  Control+A
- Start Mark or Unmark\
  Start marking or unmarking a range at the current record. F8 is selection: F8 starts a run of marks.\
  F8
- Unmark All\
  A for All. Shift reverses Mark All.\
  Control+Shift+A

### File

- Choose Table\
  T for Table.\
  Control+T
- Close All but Current Window\
  Close every window except the current one. F4 is open windows; Control+Shift+F4 closes all but this one.\
  Control+Shift+F4
- Close Window\
  Close the current recordset window. F4 is open windows; Control+F4 closes this one, as in Windows.\
  Control+F4
- Current Windows\
  Pick from the list of open recordset windows and activate the chosen one. F4 picks, says or closes open windows: F4 picks one.\
  F4
- Design Database\
  Build or change this database: add a table, table properties, or DbDo's own defs, files and maps tables. D for Design.\
  Alt+Shift+D
- Exit DbDo\
  Close DbDo. Alt+F4 closes the program, as in every Windows program.\
  Alt+F4
- Export Data\
  Export the records in view, with the shown columns, to a file. X for the sound of "ex".\
  Control+Shift+X
- Import Data\
  Build a new DbDo database from another file -- Excel (read by DbDo itself, with no Office or Access driver needed), Access, dBASE, CSV, or SQLite -- with one standard-shape table per source table, plus maps and defs. I for Import.\
  Control+Shift+I
- Merge Data\
  Merge rows from a Markdown table, JSON, or Inix file into the current table. M for Merge.\
  Alt+Shift+M
- Next Table or View\
  F6 moves among parts of the window; Control+F6 moves to the next table.\
  Control+F6
- Next Window\
  Activate the next open window. Control+Tab moves among DbDo windows.\
  Control+Tab
- Open Database\
  Open a database file. O for Open.\
  Control+O
- Open Table\
  Open a table in a new window. O for Open.\
  Control+Shift+O
- Open Template Database\
  Open one of the template databases that come with DbDo, such as RadioTrail or JobTrail. D for Database.\
  Alt+D
- Previous Table or View\
  F6 moves among parts; Shift reverses Control+F6.\
  Control+Shift+F6
- Previous Window\
  Activate the previous open window. Shift reverses Control+Tab.\
  Control+Shift+Tab
- Recent Databases\
  R for Recent.\
  Alt+R
- Run Report\
  Render a saved report definition to a Markdown document. A report reads the whole physical table, not the grid view, so it comes out the same whatever your filter or order. R for Run.\
  Alt+Shift+R
- Save\
  Save changes to disk (writes an open workbook back to its .xlsx; a .db is already saved as you go). S for Save.\
  Control+S
- Save As\
  Save a copy to another path or file format. S for Save.\
  Control+Shift+S
- Say Windows Open\
  Speak the count and titles of open recordset windows, marking the current one. F4 is open windows; Shift+F4 says which are open.\
  Shift+F4

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
  Describe each key instead of running it. F1 is help; Control+F1 describes whatever key you press next.\
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

- Copy Append\
  Append the value of the cell under the virtual cursor to the clipboard. C for Copy.\
  Alt+C
- Copy Cell\
  Copy the value of the cell under the virtual cursor to the clipboard. C for Copy.\
  Control+C
- Copy Grid\
  Copy the whole visible grid to the clipboard as tab-separated values with a header row.\
  Control+F8
- Edit Settings\
  Open the Settings dialog. E for Edit.\
  Alt+Shift+E
- Generate Graphics\
  Draw a chart of the records and columns you choose, as a picture, a web page and an Excel workbook. G for Generate.\
  Alt+Shift+G
- Generate Statistics\
  Read statistics of the records and columns you choose: summary, frequency, group, relation or cross-tab. S for Statistics.\
  Alt+Shift+S
- Read Only Toggle\
  Flip the database between read-write and read-only at runtime. Z is for sleep: it wakes this behavior or puts it to sleep.\
  Control+Shift+Z
- Refresh Records\
  Refresh the records' values from the database. F5 is refreshing.\
  F5
- Show Columns\
  Choose the columns the grid shows and their order. S for Show.\
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
  Alt+Shift+V
- Evaluate Expression\
  Control+Equals
- Invoke Script\
  Run a script from the Scripts folder (.js, .sql, .dbdo, or a macro's .inix).\
  Alt+V
- Open Command Prompt\
  Control+Backslash
- Open Dot Prompt\
  Send focus to the dot prompt console. Control+GraveAccent opens a console, as it opens the terminal in Visual Studio Code.\
  Control+GraveAccent
- Open in Explorer\
  Open the folder of the open database in Windows Explorer.\
  Alt+Backslash
- Play Macro\
  Play the macro you recorded or loaded last.\
  Alt+Shift+F12
- Record Macro\
  Start recording a macro of record moves, or stop and save it, or stop one that is playing.\
  Control+Shift+F9
- Show in FileDir\
  Show the open database's file, selected in its folder, in FileDir.\
  Control+Shift+Backslash
- Sqlean Console\
  Open the bundled sqlean.exe shell in its own console window on the current database.\
  Control+Shift+GraveAccent

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
- Forward Find with Regex\
  Find the next record matching a regular expression. F3 searches; Control makes the search a pattern.\
  Control+F3
- Go to Record\
  Go to a record by its number, a percent or text. G for Go, as Control+G goes to a line in Windows editors.\
  Control+G
- Go to Record Again\
  G for Go.\
  Alt+G
- Jump\
  Go to the next record whose current field holds the text. J for Jump.\
  Control+J
- Jump Again\
  J for Jump.\
  Alt+J
- Keywords\
  Go to the next record whose fields hold the keywords. K for Keywords.\
  Control+K
- Keywords Again\
  K for Keywords.\
  Alt+K
- Reverse Find with Regex\
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
  Alt+I
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
- Yield with Regex\
  Count the matches of a regular expression in the fields and records you choose. Y for Yield.\
  Control+Shift+Y

### Query, Say

Shift and a letter asks. Nothing here changes anything; each answers a question about where you are, in one short line.

- Read Lean\
  Read the current column's values from the first record. L for Lean.\
  Alt+L
- Read Lean from Current\
  Read the current column's values from the current record on. L for Lean.\
  Control+L
- Read Lean Marked\
  Read the current column's values in the marked records, from the first. L for Lean.\
  Alt+Shift+L
- Read Wide\
  Read the records in view from the first, every shown column. W for Wide.\
  Alt+W
- Read Wide from Current\
  Read the records in view from the current one on, every shown column. W for Wide.\
  Control+W
- Read Wide Marked\
  Read the marked records in view from the first, every shown column. W for Wide.\
  Alt+Shift+W
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
- Say Database\
  Say the database's name and folder. D for Database.\
  Shift+D
- Say Edited\
  Say when the record was last edited. E for Edited.\
  Shift+E
- Say Filter\
  F for Filter.\
  Shift+F
- Say Filter and Order\
  Speak the current filter and order, or none for each. Shift+8 is the asterisk, the SQL sign for everything: sort and filter together.\
  Shift+8
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
- Say Replace Values\
  R for Replace.\
  Shift+R
- Say Shown Columns\
  Say the columns the grid shows. S for Shown.\
  Shift+S
- Say Status\
  Say the table, record, order and filter. Z is the bottom of the alphabet, as the status bar is the bottom of the window.\
  Shift+Z
- Say Tags\
  Say the record's tags. T for Tags.\
  Shift+T
- Say url\
  Say the record's url. U for url.\
  Shift+U
- Say Whole Record\
  W for Whole.\
  Shift+W
- Say Yield\
  Say how many records are in view. Y for Yield.\
  Alt+Y
- Say Yield from Current\
  Say how many records there are from the current one on. Y for Yield.\
  Control+Y
- Say Yield Marked\
  Say how many records are marked. Y for Yield.\
  Alt+Shift+Y

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
  Copy Append
- Alt+D\
  Open one of the template databases that come with DbDo, such as RadioTrail or JobTrail.\
  Open Template Database
- Alt+Delete\
  Speak the column header and 1-based row number of the virtual cell.\
  Say Position
- Alt+Enter\
  Table Properties
- Alt+G\
  Go to Record Again
- Alt+Home\
  Go back to the first table.\
  Exit to Root Table
- Alt+I\
  Inspect Record
- Alt+J\
  Jump Again
- Alt+K\
  Keywords Again
- Alt+L\
  Read the current column's values from the first record.\
  Read Lean
- Alt+LeftArrow\
  Return from a child drill-down to the parent table.\
  Exit Child Table
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
  Show Columns
- Alt+T\
  Show a summary of the table: fields, types and keys.\
  Table Summary
- Alt+V\
  Run a script from the Scripts folder (.js, .sql, .dbdo, or a macro's .inix).\
  Invoke Script
- Alt+W\
  Read the records in view from the first, every shown column.\
  Read Wide
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
  Copy Append Record
- Alt+Shift+D\
  Build or change this database: add a table, table properties, or DbDo's own defs, files and maps tables.\
  Design Database
- Alt+Shift+E\
  Open the Settings dialog.\
  Edit Settings
- Alt+Shift+G\
  Draw a chart of the records and columns you choose, as a picture, a web page and an Excel workbook.\
  Generate Graphics
- Alt+Shift+L\
  Read the current column's values in the marked records, from the first.\
  Read Lean Marked
- Alt+Shift+M\
  Merge rows from a Markdown table, JSON, or Inix file into the current table.\
  Merge Data
- Alt+Shift+N\
  Edit Notes
- Alt+Shift+O\
  Reverse the order of the records.\
  Reverse Order
- Alt+Shift+Q\
  Query History
- Alt+Shift+R\
  Render a saved report definition to a Markdown document. A report reads the whole physical table, not the grid view, so it comes out the same whatever your filter or order.\
  Run Report
- Alt+Shift+S\
  Read statistics of the records and columns you choose: summary, frequency, group, relation or cross-tab.\
  Generate Statistics
- Alt+Shift+T\
  Edit Tags
- Alt+Shift+U\
  Edit url
- Alt+Shift+V\
  Edit Script
- Alt+Shift+W\
  Read the marked records in view from the first, every shown column.\
  Read Wide Marked
- Alt+Shift+Y\
  Say how many records are marked.\
  Say Yield Marked

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
- Control+GraveAccent\
  Send focus to the dot prompt console.\
  Open Dot Prompt
- Control+I\
  Invert Marked
- Control+J\
  Go to the next record whose current field holds the text.\
  Jump
- Control+K\
  Go to the next record whose fields hold the keywords.\
  Keywords
- Control+L\
  Read the current column's values from the current record on.\
  Read Lean from Current
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
  Play Media
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
  Toggle Mark
- Control+T\
  Choose Table
- Control+Tab\
  Activate the next open window.\
  Next Window
- Control+W\
  Read the records in view from the current one on, every shown column.\
  Read Wide from Current
- Control+Y\
  Say how many records there are from the current one on.\
  Say Yield from Current

### Control+Shift

- Control+Shift+A\
  Unmark All
- Control+Shift+Apostrophe\
  Add the clipboard text to the end of a file you name.\
  Append Clipboard
- Control+Shift+B\
  Forget the saved bookmark.\
  Clear Bookmark
- Control+Shift+Backslash\
  Show the open database's file, selected in its folder, in FileDir.\
  Show in FileDir
- Control+Shift+C\
  Duplicate the current row as a new row.\
  Copy Record
- Control+Shift+D\
  Delete without Confirmation
- Control+Shift+E\
  Extract with Regex
- Control+Shift+F\
  Clear the active filter.\
  Clear Filter
- Control+Shift+GraveAccent\
  Open the bundled sqlean.exe shell in its own console window on the current database.\
  Sqlean Console
- Control+Shift+I\
  Build a new DbDo database from another file -- Excel (read by DbDo itself, with no Office or Access driver needed), Access, dBASE, CSV, or SQLite -- with one standard-shape table per source table, plus maps and defs.\
  Import Data
- Control+Shift+J\
  Go to the previous record whose current field holds the text.\
  Reverse Jump
- Control+Shift+K\
  Go to the previous record whose fields hold the keywords.\
  Reverse Keywords
- Control+Shift+M\
  Clear the 'marked' flag on the current row.\
  Unmark Record
- Control+Shift+N\
  New Copy
- Control+Shift+O\
  Open a table in a new window.\
  Open Table
- Control+Shift+P\
  Record Playback
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
- Control+Shift+X\
  Export the records in view, with the shown columns, to a file.\
  Export Data
- Control+Shift+Y\
  Count the matches of a regular expression in the fields and records you choose.\
  Yield with Regex
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
- Alt+Shift+F1\
  ReadMe
- Alt+Shift+F12\
  Play the macro you recorded or loaded last.\
  Play Macro
- Alt+Shift+F8\
  Unmark every row from the F8 start to the current row.\
  Complete Unmark
- Control+F1\
  Describe each key instead of running it.\
  Key Describer Toggle
- Control+F12\
  Copy the session log to the clipboard as a file, ready to paste as an attachment.\
  Copy Log
- Control+F2\
  Pick Value
- Control+F3\
  Find the next record matching a regular expression.\
  Forward Find with Regex
- Control+F4\
  Close the current recordset window.\
  Close Window
- Control+F6\
  Next Table or View
- Control+F8\
  Copy the whole visible grid to the clipboard as tab-separated values with a header row.\
  Copy Grid
- Control+Shift+F1\
  Open the history of changes in the default browser.\
  History of Changes
- Control+Shift+F3\
  Find the previous record matching a regular expression.\
  Reverse Find with Regex
- Control+Shift+F4\
  Close every window except the current one.\
  Close All but Current Window
- Control+Shift+F6\
  Previous Table or View
- Control+Shift+F9\
  Start recording a macro of record moves, or stop and save it, or stop one that is playing.\
  Record Macro
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
  Start marking or unmarking a range at the current record.\
  Start Mark or Unmark
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
  Mark every row from the F8 start to the current row.\
  Complete Mark

### Shift

- Shift+8\
  Speak the current filter and order, or none for each.\
  Say Filter and Order
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
  Say the columns the grid shows.\
  Say Shown Columns
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
- Append Clipboard\
  Add the clipboard text to the end of a file you name.\
  Control+Shift+Apostrophe
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
- Close All but Current Window\
  Close every window except the current one.\
  Control+Shift+F4
- Close Window\
  Close the current recordset window.\
  Control+F4
- Complete Mark\
  Mark every row from the F8 start to the current row.\
  Shift+F8
- Complete Unmark\
  Unmark every row from the F8 start to the current row.\
  Alt+Shift+F8
- Copy Append\
  Append the value of the cell under the virtual cursor to the clipboard.\
  Alt+C
- Copy Append Record\
  Alt+Shift+C
- Copy Cell\
  Copy the value of the cell under the virtual cursor to the clipboard.\
  Control+C
- Copy Grid\
  Copy the whole visible grid to the clipboard as tab-separated values with a header row.\
  Control+F8
- Copy Log\
  Copy the session log to the clipboard as a file, ready to paste as an attachment.\
  Control+F12
- Copy Record\
  Duplicate the current row as a new row.\
  Control+Shift+C
- Current Windows\
  Pick from the list of open recordset windows and activate the chosen one.\
  F4
- Delete Record\
  Delete the current record (or marked records, when marks exist).\
  Control+D
- Delete without Confirmation\
  Control+Shift+D
- Design Database\
  Build or change this database: add a table, table properties, or DbDo's own defs, files and maps tables.\
  Alt+Shift+D
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
  Alt+Shift+V
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
  Control+Shift+X
- Extract with Regex\
  Control+Shift+E
- Filter Records\
  Show only the records that match what you choose.\
  Control+F
- Forward Find with Regex\
  Find the next record matching a regular expression.\
  Control+F3
- Generate Graphics\
  Draw a chart of the records and columns you choose, as a picture, a web page and an Excel workbook.\
  Alt+Shift+G
- Generate Statistics\
  Read statistics of the records and columns you choose: summary, frequency, group, relation or cross-tab.\
  Alt+Shift+S
- Go to Record\
  Go to a record by its number, a percent or text.\
  Control+G
- Go to Record Again\
  Alt+G
- History of Changes\
  Open the history of changes in the default browser.\
  Control+Shift+F1
- Import Data\
  Build a new DbDo database from another file -- Excel (read by DbDo itself, with no Office or Access driver needed), Access, dBASE, CSV, or SQLite -- with one standard-shape table per source table, plus maps and defs.\
  Control+Shift+I
- Inspect Record\
  Alt+I
- Invert Marked\
  Control+I
- Invoke Script\
  Run a script from the Scripts folder (.js, .sql, .dbdo, or a macro's .inix).\
  Alt+V
- Jump\
  Go to the next record whose current field holds the text.\
  Control+J
- Jump Again\
  Alt+J
- Key Describer Toggle\
  Describe each key instead of running it.\
  Control+F1
- Keywords\
  Go to the next record whose fields hold the keywords.\
  Control+K
- Keywords Again\
  Alt+K
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
  Alt+Shift+M
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
  Control+GraveAccent
- Open in Explorer\
  Open the folder of the open database in Windows Explorer.\
  Alt+Backslash
- Open Table\
  Open a table in a new window.\
  Control+Shift+O
- Open Template Database\
  Open one of the template databases that come with DbDo, such as RadioTrail or JobTrail.\
  Alt+D
- Open url\
  Control+Shift+U
- Order Records\
  Order the records by the fields you choose.\
  Alt+O
- Pick Value\
  Control+F2
- Play Macro\
  Play the macro you recorded or loaded last.\
  Alt+Shift+F12
- Play Media\
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
- Read Lean\
  Read the current column's values from the first record.\
  Alt+L
- Read Lean from Current\
  Read the current column's values from the current record on.\
  Control+L
- Read Lean Marked\
  Read the current column's values in the marked records, from the first.\
  Alt+Shift+L
- Read Only Toggle\
  Flip the database between read-write and read-only at runtime.\
  Control+Shift+Z
- Read Wide\
  Read the records in view from the first, every shown column.\
  Alt+W
- Read Wide from Current\
  Read the records in view from the current one on, every shown column.\
  Control+W
- Read Wide Marked\
  Read the marked records in view from the first, every shown column.\
  Alt+Shift+W
- ReadMe\
  Alt+Shift+F1
- Recent Databases\
  Alt+R
- Record Macro\
  Start recording a macro of record moves, or stop and save it, or stop one that is playing.\
  Control+Shift+F9
- Record Playback\
  Control+Shift+P
- Refresh Records\
  Refresh the records' values from the database.\
  F5
- Replace\
  Control+R
- Replace with Regex\
  Control+Shift+R
- Reverse Find with Regex\
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
  Render a saved report definition to a Markdown document. A report reads the whole physical table, not the grid view, so it comes out the same whatever your filter or order.\
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
- Say Database\
  Say the database's name and folder.\
  Shift+D
- Say Edited\
  Say when the record was last edited.\
  Shift+E
- Say Filter\
  Shift+F
- Say Filter and Order\
  Speak the current filter and order, or none for each.\
  Shift+8
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
- Say Replace Values\
  Shift+R
- Say Shown Columns\
  Say the columns the grid shows.\
  Shift+S
- Say Status\
  Say the table, record, order and filter.\
  Shift+Z
- Say Tags\
  Say the record's tags.\
  Shift+T
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
- Say Yield from Current\
  Say how many records there are from the current one on.\
  Control+Y
- Say Yield Marked\
  Say how many records are marked.\
  Alt+Shift+Y
- Search Next\
  Repeat the last Find forward.\
  F3
- Search Previous\
  Repeat the last Find backward.\
  Shift+F3
- Show Columns\
  Choose the columns the grid shows and their order.\
  Alt+S
- Show in FileDir\
  Show the open database's file, selected in its folder, in FileDir.\
  Control+Shift+Backslash
- Sqlean Console\
  Open the bundled sqlean.exe shell in its own console window on the current database.\
  Control+Shift+GraveAccent
- Start Mark or Unmark\
  Start marking or unmarking a range at the current record.\
  F8
- Table Properties\
  Alt+Enter
- Table Summary\
  Show a summary of the table: fields, types and keys.\
  Alt+T
- Toggle Mark\
  Control+Space
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
- Yield with Regex\
  Count the matches of a regular expression in the fields and records you choose.\
  Control+Shift+Y
