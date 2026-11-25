# DbDo User Guide

DbDo is a relational database manager built from the ground up for people who work by keyboard and screen reader. It opens SQLite, Access, Excel, dBASE, and delimited-text files, presents their tables as ordinary Windows lists you can navigate cell by cell, and lets you query, edit, relate, analyze, and report on the data without ever needing a mouse or a glance at the screen.

This guide is organized concepts first. The opening part explains the handful of ideas that everything else rests on: how windows are arranged, what the data list is, the difference between the grid you navigate and the physical table underneath it, how fields are categorized, and the keyboard patterns that recur throughout the program. Once those are clear, the later parts cover working with data, producing output, importing and exporting, SQL and scripting, and a full command reference. If you are new to DbDo, read Part 1 in order. If you are returning for a specific answer, jump to the reference tables near the end.

---

## Part 1 — Key concepts

### What DbDo is

DbDo is a single desktop application that presents **two interfaces over one live connection** to whatever database you open:

- A **graphical interface** of standard Windows menus and lists, driven entirely from the keyboard, where each open table appears in its own window.
- A **dot prompt**, a dBASE-style console where you type commands and short SQL directly, for people who prefer a command line or want to script.

Both interfaces talk to the same open database at the same time, so a change made in one is immediately visible in the other. You can spend all your time in the menus, all your time at the prompt, or move freely between them.

DbDo does not invent its own storage format. It reads and writes real files in their native formats — a SQLite `.db`, an Access `.mdb`/`.accdb`, an Excel workbook, a dBASE `.dbf`, a `.csv` — through the appropriate database driver. Its value is the *interface*, not the container.

### Windows: the multiple-document model

DbDo uses a **multiple-document interface (MDI)**. There is one application frame, and inside it each open recordset — normally a table, sometimes a view or a query result — lives in **its own child window**. Opening a second table does not replace the first; it opens alongside it, and you move between them with window commands.

This matters for a screen-reader user because it keeps each table's state — its position, its filter, its sort, its selected columns — self-contained and announced per window. When you switch windows you switch whole working contexts.

The window commands live on the **File menu**, just before Exit DbDo. DbDo has no Window menu, so Alt+W is free to read the grid wide (see "Reading the grid aloud: wide, lean and yield", below):

- **Current Windows** (F4) lists every open window; pick one to jump to it.
- **Window Toggle** flips between the current window and the one you were in before it.
- **Next Window** (Control+Tab) and **Previous Window** (Control+Shift+Tab) cycle through them.
- **Say Windows Open** (Shift+F4) speaks how many windows are open and their titles.
- **Close Window** (Control+F4) closes the current one; **Close All but Current Window** (Control+Shift+F4) leaves only the active one. Closing the last window exits DbDo, because the menu bar lives in the window.

**Open Table** (Control+T, T for Table) opens a table or a view in a *new window*, so you can keep several on screen. A table already open in a window is still listed; choosing it goes to that window. Control+T only opens; Control+Tab and Control+Shift+Tab move among the windows, and Control+F4 closes one you no longer want. O is for a database: **Open Database** (Control+O) opens a file, and **Open Template Database** (Control+Shift+O), a variation of Open, opens one of the databases that come with DbDo.

### The data list

Inside each window the records are shown in a **data list** — a standard Windows list control with a row per record and a column per field. This is the surface you spend most of your time on. Your screen reader reads it the way it reads any Windows list: arrow down to the next row, and the row's contents are announced.

The list is **single-select**: exactly one row is current at a time, and that row is the database cursor's current record (see below). DbDo deliberately does not use multi-select highlighting; instead it has an explicit **marking** system (covered in Part 2) so that "which records are selected for an operation" is a property you set and query on purpose, not a fragile visual highlight.

#### Virtual cell navigation

Beyond moving row to row, DbDo lets you move **cell by cell within a row** without leaving the list. A *virtual cursor* tracks which column you are on. Move it left and right across the columns and DbDo speaks the value in the current cell together with its field name, so you always know both *what* the value is and *which field* it belongs to. This is how you inspect a wide record one field at a time by keyboard, and it is what several commands mean when they act on "the current column" or "the cell under the cursor."

### Two vocabularies: schema versus interface

DbDo is careful about words, and the guide follows the same discipline, because the two halves of the program describe the same data with two different vocabularies:

- **Schema vocabulary — Field, Record, Table.** These describe the *data model*: a table is made of records, each record has fields. Use these words when talking about structure, types, and design.
- **Interface vocabulary — Column, Row, Cell, Grid.** These describe *what you navigate*: the grid has rows and columns, and each intersection is a cell. Use these words when talking about moving around and reading.

A field and a column are the same underlying thing seen from two angles; likewise a record and a row, and a cell is one field of one record. One more term: an absent value is **blank**, never "empty" or "null" in the interface.

### The grid versus the physical database

This is the single most important distinction in DbDo.

The **physical database** is the table as stored on disk: every record, every field, in whatever order the file holds them.

The **grid** is a **virtual view** onto that table — specifically, the view produced by three things you control:

1. the **filter** currently in effect (a Where clause limiting which records appear),
2. the **sort** currently in effect (the order the records appear in), and
3. the **displayed columns** you have chosen to show.

The grid is what you see and navigate. It can be a small, reordered slice of a large table. The physical table is the whole thing underneath.

Editing writes **through** the grid to the physical table: when you change a cell, the underlying record is updated on disk. But *reading* commands split into two families, and knowing which is which prevents surprises:

- Commands that act on **the grid** operate on *what you are looking at* — the current filtered, sorted, column-selected view. **Generate Statistics** and **Generate Graphics** are the clearest examples: they analyze the virtual grid.
- Commands that act on **the physical table** operate on *all the records*, regardless of the current filter or sort. **Run Report** is the clearest example: it reads the whole physical table so a report is reproducible no matter what you were viewing.

Whenever a command's behavior could depend on the current view, this guide says explicitly whether it acts on the grid or the physical table.

### The database cursor

Underneath the grid is a **cursor**: a pointer to the **current record**. Exactly one record is current at any moment. Almost everything is expressed relative to the cursor:

- **Navigation** moves the cursor — to the next or previous record, the first or last, a record number, or the next record matching a search.
- **Editing** acts on the current record.
- **Filtering and sorting** rebuild the grid and leave the cursor on a sensible record within the new view.

DbDo uses a **client-side cursor**, meaning the working set is held in memory on your side of the connection; this is what makes instant sorting, filtering, and backward navigation possible even on file-based databases.

Edits **auto-commit**. When you finish adding or changing a record, DbDo writes it immediately. There is no separate "save the record" step. (The Save command, described later, is a different thing — it makes a copy of the whole database.)

### How fields are categorized

Every field has a **type** (also called its affinity), and DbDo uses that type to decide how the field is edited, displayed, and validated. On top of the type system sits a small set of **standard fields** that DbDo recognizes by name and treats specially. Understanding both is the key to designing a database that behaves well with a screen reader.

#### Field types

DbDo recognizes these declared types. The first group is text refinements — all stored as text, but each tells DbDo how to present and edit the field:

- **TEXT** — general text of any length.
- **TEXTLINE** — a single line of text. The editor is a one-line box; newlines are not expected.
- **TEXTMEMO** — multi-line plain text. The editor is a multi-line box.
- **TEXTMARKDOWN** — multi-line text written in Markdown. Edited in a multi-line box and understood as Markdown when rendered into reports.
- **TEXTTIME** — a timestamp stored as text in a sortable form. Used by the automatic `added` and `edited` fields.

The remaining types are the ordinary storage classes:

- **INTEGER** — whole numbers.
- **REAL** — floating-point numbers.
- **NUMERIC** — numbers generally (integer or real).
- **BOOLEAN** — true/false, presented and toggled as such.
- **BLOB** — binary data.

Because the type controls the editing widget, choosing TEXTLINE versus TEXTMEMO versus TEXTMARKDOWN for a field is not cosmetic: it determines whether a screen-reader user gets a single-line box, a multi-line box, or a Markdown-aware box when they edit that field.

#### Standard fields

DbDo recognizes certain field **names** and maintains or exposes them for you. A table need not have all of them; DbDo simply uses the ones present. They are, by convention, kept out of the way — typically hidden from the default column display and reached through dedicated commands — so the grid stays focused on the data you care about while the housekeeping fields remain one keystroke away.

- **added** (TEXTTIME) — when the record was created. Maintained automatically. Speak it with Say Added (Shift+A).
- **edited** (TEXTTIME) — when the record was last changed. Maintained automatically on every update. Speak it with Say Edited (Shift+E).
- **marked** (BOOLEAN) — whether the record is currently marked for a batch operation. This is what the marking commands set and clear.
- **notes** (TEXTMARKDOWN) — a free-form Markdown note attached to the record. Edit it with Edit Notes (Alt+Shift+N); speak it with Say Notes (Shift+N).
- **tags** (TEXTMEMO) — free-form tags or keywords. Edit with Edit Tags (Alt+Shift+T); speak with Say Tags (Shift+T).
- **url** (TEXTLINE) — an associated link. Edit with Edit url (Alt+Shift+U); open it with Open url (Control+Shift+U); speak with Say url (Shift+U).
- **look** — how a record is said when it is mentioned somewhere else: just enough to know it, such as first and last name. Its fields are the columns a table opens with. Speak it with Say Look (Shift+L).
- **prime** — short for primary key: the fields that together make a record one of a kind, such as name and zip code. Its fields set the order a table opens in. Speak it with Say Prime (Shift+P).

The distinction to hold onto: **added** and **edited** are *automatic* (DbDo writes them for you); **marked** is *state* you toggle; **notes**, **tags**, and **url** are *content* you edit through their own focused dialogs; **look** and **prime** are *derived identity* fields DbDo computes so records can be named and matched reliably.

#### Defs and maps

Two optional companion tables let a database describe its own vocabulary:

- A **defs** table lists the valid values for a field. When a field has choices in defs, its editor becomes a combo box of those values instead of a free-text box — so a screen-reader user arrows through the legal choices rather than typing and risking a typo. Pick Value (Control+F2) surfaces the same choices on demand.
- A **maps** table links any record in the database to any other record, with a **kind** saying how they are related -- "contact for", "sent for", "part of" -- and the kinds kept in the defs table, so F4 offers them. One maps table serves every pair of tables, so an occasional or many-to-many relationship needs no table of its own.

These are conventions, not requirements; a database without them simply offers free-text editing, and its relationships are its foreign keys.

### Keyboard patterns

DbDo has three distinct keyboard channels, and recognizing which one you are using clears up most confusion about "why did that key do that."

#### 1. Command chords

Every menu command has a name and, usually, a **chord** — a keyboard shortcut shown next to it in the menu. Chords follow a consistent **mnemonic rule**: the letter in the chord is the first letter of a word in the command's name. *Generate Statistics* is Alt+Shift+S; *Generate Graphics* is Alt+Shift+G; *Filter Records* is Control+F. Once you know a command's name you can usually guess its chord, and vice versa. Chords are grouped by modifier so related commands share a shape — the Say-something status commands are mostly Shift+letter, for instance.

#### 2. Convenience keys in text fields

When you are *inside a text box* — editing a cell, a note, a filter — a set of **convenience keys** is available in addition to normal typing. These operate on the text under your cursor and are meant to save a screen-reader user from hunting around:

- **Copy / cut the current line** without selecting it first (Control+C / Control+X with no selection), and **append** to the clipboard rather than replacing it (Alt+C / Alt+X).
- **Mark a selection** by setting a start (F8) and completing it (Shift+F8) as two separate keystrokes, rather than holding Shift while arrowing.
- **Copy all** (Control+F8) or **read all** (Alt+F8) of the field's text.
- **Delete the current line** (Control+D), with the next line spoken so you know where you landed.
- **Run at Cursor** (Shift+F5) — take the selection, or the current line if nothing is selected, treat it as a URL, file path, or email address, and open it with your system after a confirmation prompt. Put the cursor on a link in a note and press Shift+F5 to open it in your browser.

These are on by default and can be turned off with the `extraKeys` setting.

#### 3. The dot prompt and status queries

The **dot prompt** (Enter Console, Control+GraveAccent) is the command-line channel: type dBASE-style commands or short SQL and press Enter. It is covered in Part 5.

Woven through the menus is a large **Say-X status family** in the Query menu: quick commands that *speak a fact about your current situation* without changing anything — Say Status, Say Database, Say Order, Say Yield (record count), Say Filter, Say Position, Say Cell, and many more. These are how you interrogate the screen on demand instead of hunting for information, and they are listed in full in the command reference.

#### Reading the grid aloud: wide, lean and yield

Three letters read or count the records in view. Each works with the same three modifiers, so learning one teaches all three.

- **W, wide,** reads every shown column of each record: the grid's whole width. Read Wide is Alt+W.
- **L, lean,** reads only the current column. Read Lean is Alt+L.
- **Y, yield,** counts the records. Say Yield is Alt+Y.

W and L are the widest and narrowest letters of the alphabet, which helps them stick.

- **Alt** starts from the first record.
- **Alt+Shift** keeps only the marked records.
- **Control** starts from the current record.

So Control+L reads the current column from here down, and Alt+Shift+Y says how many records are marked. Readings go one value at a time, and any key stops them. Press a reading key twice to see the same values in a window instead. Shift+Space also reads the marked records wide.

A fourth letter, **H, headed,** reads like W but names everything: "row 9", then each column's name before its value, such as "name", "Radio Nova", "country", "Germany". Use it on a table you do not know yet, or when values sound alike. H takes the same keys as W, except one: Read Headed Marked is Alt+Shift+H and Read Headed from Current is Control+H, but Alt+H opens the Help menu. To read headed from the first record, press Control+Home, then Control+H.

**Say Header** (Shift+H) says the name of the column you are on, as Alt+Control+Shift+UpArrow does. Do not confuse it with **Say Look** (Shift+L), which says the record's look: the value in its look column, such as "Radio Nova | France", not the names of the columns that make it.

The fourth form, marked records from the current one on, is on the menu with no key: Read Wide Marked from Current, Read Lean Marked from Current and Say Yield Marked from Current. Control and Shift with a letter is kept for commands that change things, and Control+Shift+Y is Yield with Regex, beside Extract with Regex (Control+Shift+E) and Replace with Regex (Control+Shift+R), as in every Homer Suite app.

FileDir reads and counts its list with the same L and Y keys. It has no W keys, since its list has one column.

#### Key Help

**Key Describer Toggle** (Control+F1) turns on a learning mode: while it is on, pressing a command chord **announces** what that command is and does **instead of running it**. Press Control+F1 again to turn it off. This lets you explore the keyboard safely. **Where Am I** (Control+Slash, pressed twice) lists every command with its key, and the **Alternate Menu** (Alt+F10) gives you one list of all commands to search and run, in menu, name or key order.

---

## Part 2 — Working with data

### Opening databases and recordsets

- **Open Template Database** (Control+Shift+O) starts from one of the databases DbDo ships -- BookTrail, JobTrail and the rest -- opening your own copy of it.
- **Open Database** (Control+O) opens a database file, choosing the right driver from the extension. **New Database** creates a fresh SQLite database; **Add Table** adds a table to it.

  **Add Table** asks first whether to **Define** a new table, **Clone** the open one's fields (when a table is open), or make one **From file**: a CSV or TSV file becomes a table of the open database, named for the file and shaped as Import shapes it, numbers as numbers. That is how two files meet in one database, so a query can look a value up from one in the other. Define or Clone which then fill the form for you to name the new table and change what you like. Every new table gets DbDo's standard columns: its id, added, edited, notes, tags and marked. The form has ten name-and-type slots; its **Text** button opens the same fields as lines you can read and edit like any text, one field a line in the form `name: TYPE`, with no limit to how many, and you can paste a structure in. On OK, if fields begin with the same letter, DbDo says which, since only the first of each gets an Alt letter in the record form, and offers to go back and rename them.
- **Open Table** (Control+T) opens a table or view in a new window, or goes to the window it is already in.
- **Import Data** (Control+Shift+I) makes a new DbDo database from a spreadsheet, Access, dBASE, CSV or other file, with every worksheet a table.
- **Recent Databases** (Alt+R) reopens something you used lately.
- **Close Database** closes the current database; **Backup Database** writes a copy; **Compare Database** compares two.

When a database has several tables, **Open Table** (Control+T) gives each its own window, and Control+Tab moves among them. Open Table lists a database's views beside its tables, so a view opens the same way.

### Moving in the grid

The grid has one keyboard grammar. Plain keys move within what you are on. Control jumps along the column. Shift marks along the same jump, and adding Alt unmarks. Alt with Control keeps the meanings it has in tables in web pages, Word and Adobe Reader.

- **Arrows** move one cell. Up and Down Arrow are left to your screen reader, which reads the row as it moves; Left and Right Arrow move DbDo's cursor from column to column.
- **Home and End** go to the first and last column of the row.
- **Page Up and Page Down** move a screenful, staying in the column.
- **Control+Home and Control+End** go to the first and last row, staying in the column.
- **Control+Up Arrow and Control+Down Arrow** go to the previous and next marked record.
- **Control+Page Up and Control+Page Down** go to the first and last marked record.

The screen reader table keys work as they do in tables in web pages, Word and Adobe Reader:

- **Alt+Up Arrow and Alt+Down Arrow** move one cell up or down in the column and say only the cell, the quick way to read down a column.
- **Alt+Control+arrows** move one cell. Moving along a row, DbDo says the new column's name, then the cell. Moving down a column, it says the record's look, then the cell, just as a screen reader says a row header; when the cell is itself part of the look, it says the cell alone. At the edge of the grid, the cursor stays, and DbDo says First row, Last row, First column or Last column.
- **Alt+Control+Home and Alt+Control+End** go to the top-left and bottom-right corners, as in JAWS and Narrator. (NVDA uses them for the first and last column of the row; in DbDo, Home and End do that.)
- **Alt+Control+Page Up and Alt+Control+Page Down** go to the first and last row of the column, as in NVDA. Under JAWS they change the voice rate for the moment, as everywhere, so use Control+Home and Control+End, which do the same moves.
- **Alt+Control+Number Pad 5** says the cell with its column's name and record number; **Shift+C** says the value alone.

Narrator's table reading keys read without moving:

- **Alt+Control+Shift+Left Arrow** says the record's look, its row header.
- **Alt+Control+Shift+Up Arrow** says the column's name, its column header.
- **Alt+Control+Shift+Right Arrow** says every shown field of the record.
- **Alt+Control+Shift+Down Arrow** says every value in the column, from the top.
- **Alt+Control+Shift+Number Pad 5** says the cell's address: its column's name, then its record and column numbers.

In the Key Describer (Control+F1), every one of these keys says what it does.

#### Your screen reader's own keys in the grid

A few of JAWS's and NVDA's own keys ask a question that has no good answer in a grid of records, such as which font the text uses. DbDo's scripts answer those keys in the grid with DbDo's own answer, and leave them to the screen reader everywhere else, in dialogs and boxes too.

- **Font (JAWS+F, or NVDA+F)** says the field's type instead, as **Say Field Type** (Alt+Shift+F) does: "type: textmemo, several lines of text".
- **Where the cursor is (NVDA+Number Pad Delete)** says the record's position, as **Say Position** (Alt+Delete) does. JAWS's own key for where the cursor is, Alt+Delete, already reaches DbDo, as in every Homer app.
- **Address (JAWS+A, in JAWS's desktop layout)** says the cell's address, as Alt+Control+Shift+Number Pad 5 does: the column's name, then the record and column numbers. In JAWS's laptop layout and in NVDA's, JAWS+A and NVDA+A are Say All, so DbDo leaves them alone there.
- **Say Current Cell (JAWS's Alt+Control+Number Pad 5)** goes to DbDo, which says the cell, as above.
- **NVDA's table reading keys** read DbDo's records: NVDA+Alt+Control+Left Arrow reads the whole record, as Alt+Control+Shift+Right Arrow does; NVDA+Alt+Control+Up Arrow the whole column, as Alt+Control+Shift+Down Arrow does; and NVDA+Alt+Control+Down Arrow the column from the current record down, as Read Lean from Current (Control+L) does.

The rest of the screen reader's keys work as always: JAWS+Tab or NVDA+Tab says the focused control, JAWS+T or NVDA+T the title, JAWS+Up Arrow or NVDA+Up Arrow the current row.

### Search syntax

Every Homer search that is not a regular expression reads what you type the same way. A plain word is enough most of the time; the symbols are there when you need more.

- **A word** is found anywhere, whatever its case: `river` finds "Riverside".
- **`*`** stands for any run of characters, and **`?`** for any one character: `chap*`, `c?t`.
- **`&`** means all of them: `red & blue`.
- **`|`** means any of them: `red | blue`. `&` is read first, so `red & blue | green` means red and blue, or else green.
- **`!`** before a word means not: `!draft`.
- **Quotes** find exactly what is inside, symbols and all: `"C#"`, `"a & b"`.

Where a wildcard counts depends on what is searched. When the search reads a **name** -- a file name, a line of a list, one field -- a word with `*` or `?` must match the whole name, so `*.md` means a name ending in .md, and `chap*` one starting with chap. When it reads **text** -- a file's contents, a whole record -- every word may match anywhere. A word with no wildcard is found anywhere in both.

F3 repeats the last search, in the same way and over the same things, going forward; Shift+F3 repeats it going back.

In DbDo, Keywords and Any Field read a whole record as text, so the words of `red & blue` may sit in different fields; Jump Record and a Fields box read one field as a name. Forward Find with Regex, and Replace with Use regex pattern checked, take regular expressions instead; otherwise Replace finds exactly the text you type.

### Navigating records

Beyond the grid's keys, the cursor moves with these commands:

- **Go to Record** (Control+G) jumps to a record number; **Go to Record Again** (Alt+G) repeats it. **Jump Again** (Alt+J) and **Keywords Again** (Alt+K) repeat those searches forward, as in FileDir; F3 repeats whichever search came last.
- **Keywords** (Control+K) searches for text; **Reverse Keywords** (Control+Shift+K) searches backward; **Search Next** (F3) and **Search Previous** (Shift+F3) repeat the last search. **Forward Find with Regex** (Control+F3) and **Reverse Find with Regex** (Control+Shift+F3) search by regular expression.
- **Jump** (Control+J) and **Reverse Jump** move by a jump increment for quickly covering distance in a long table.
- **Jump to Next Initial** steps to the next record whose current column starts with a new letter — type-ahead through a sorted column.

### Editing records

- **New Record** (Control+N) adds a record; **New Copy** (Control+Shift+N) starts a new record pre-filled from the current one; **Copy Record** (Control+Shift+C) copies it to the clipboard.
- **Edit Record** (Control+E) opens the whole record in a field-by-field dialog. **Edit Cell** (F2) edits just the current cell. **Open Cell Value** (Control+Enter) opens a large value in its own box for comfortable reading and editing.
- **Pick Value** (Control+F2) offers the legal values for a field whose choices are in defs.
- **Edit Notes** (Alt+Shift+N), **Edit Tags** (Alt+Shift+T), and **Edit url** (Alt+Shift+U) each open one standard field in a focused single-field dialog with OK and Cancel — the quick way to touch a note, tags, or a link without opening the whole record.
- **Replace** (Control+R) puts new text in place of old. One dialog asks what to find and what to put in its place, then where: **Columns** -- the current column, the shown columns, or all columns, hidden ones too -- and **Which records** -- all records in view, the marked records, or the current record. It starts on the current column, and on the marked records when some are marked, else all records in view; changing more than one record asks once, naming how many. Check **Use regex pattern** to find by regular expression, with $1 and $2 putting back the parts in parentheses; **Replace with Regex** (Control+Shift+R) opens the same dialog with it checked. **Dry run** counts what would change and changes nothing. Replace never writes the fields DbDo keeps itself: look, prime, added, edited, marked and the id.
- **Calculate Column** (Edit menu, no key) adds a column whose values come from a formula over the others, as pandas does with `df["ratio"] = df["a"] / df["b"]`. It asks for the new column's **Name**, a **Kind of formula** built on the current column, the **Formula** itself, and **Which records** to fill: all records, the records in view, or the marked ones. The kinds are: arithmetic with a number; divided by, or plus, another column; rounded; the length of a text; lower or upper case; the text before or after a comma (a surname from "Braund, Mr. Owen Harris"); the year, month, weekday or hour of a date; each value mapped to another (male to M); yes or no by a test; and a value looked up from another table. Each puts its formula in the Formula box to use as it is or change; the formula is SQL, with column names in double quotes. It is tried first, so an error is reported before anything changes. The new column holds whole numbers, numbers or text, as the formula gives, and is shown last.
- **Rename Column** (Edit menu, no key) gives a column of the open table a new name and keeps its values; look and prime follow, and so do the order, the filter and the shown columns. The columns DbDo keeps itself keep their names.
- **Extract with Regex** (Control+Shift+E) copies every match of a regular expression to the clipboard, one per line, from the same Columns and Records choices; it starts on all columns, hidden ones too, so an email address in the notes is found. It has the same name and key as in EdSharp and FileDir.
- **Yield with Regex** (Control+Shift+Y) asks the same questions and says how many matches there are, and in how many records, leaving the clipboard alone. It has the same name and key as in EdSharp, FileDir and HomerView.
- **Delete Record** (Control+D) deletes the current record with confirmation; **Delete without Confirmation** (Control+Shift+D) skips the prompt.

All edits auto-commit. If a table is read-only, **Read Only Toggle** (Control+Shift+Z) reports and controls that.

### Marking records

Marking is DbDo's explicit, queryable alternative to visual multi-select. The `marked` field carries the state.

- **Mark Record** (Semicolon) marks the current record; **Unmark Record** (Slash) clears it, the same keys as in FileDir; **Toggle Mark** (Control+Space or Space) flips it -- a Space typed within a second of a letter goes on to the type-ahead search instead, so "new y" still finds New York; **Say Marked** (Shift+M) speaks it.
- **Mark All** (Control+A), **Unmark All** (Control+Shift+A), and **Invert Marked** (Control+I) act across the current view.
- **Shift+Down Arrow and Shift+Up Arrow** mark the record and move to the next or previous one. **Control+Shift+Down Arrow and Control+Shift+Up Arrow** unmark it and move.
- **> and <** (Shift+Period and Shift+Comma) are the same moves from the home row: > marks and moves to the next record, < unmarks and moves to the next. Like Space, Semicolon and Slash, they go to the type-ahead search instead when typed within a second of a letter or digit.
- **Control+Shift+Home** marks every record from the first to this one; **Control+Shift+End** marks from this one to the last. **Alt+Control+Shift+Home** and **Alt+Control+Shift+End** unmark the same spans. These were Shift+Home and Shift+End through version 1.0.316; those keys now say where marking went.
- **Control+Up Arrow and Control+Down Arrow** go to the previous and next marked record; **Control+Page Up and Control+Page Down** go to the first and last.
- To mark a **range**, start it with **Start Mark or Unmark** (F8), move, and finish with **Complete Mark** (Shift+F8) to mark every record between, or **Complete Unmark** (Alt+Shift+F8) to unmark them. This is FileDir's way too. Alt+F8 reads everything (Read Wide), as Read All does in EdSharp and HomerView.

Marked records can then be reported, filtered, or exported as a set, and several Say commands report only the marked ones.

**Marked or Current.** A command that can act on one record or on several asks which you mean whenever records are marked: Delete Record, Delete without Confirmation, Copy Record, Copy Append Record, Copy Visible Cells, Mail Record, Open url, Play Media and Record Playback. Enter takes the marked records; C takes the current one alone and leaves the marks as they are. It does not ask when the only marked record is the current one. FileDir asks the same question, the same way. Commands that work on the whole view, such as Export Data and Copy Grid, and the Say and Read commands, never ask.

### Filtering and sorting

Filtering and sorting reshape the grid without touching the physical table.

- **Filter Records** (Control+F) limits the grid to the records that meet a condition.
  - **In a query window too.** A window holding the results of your own SQL is filtered the same way: DbDo runs your query again inside SQL, with the condition around it, so every choice works there, Invert filter included.
  - **How to give a condition.** With no filter in force, it asks at once how to give one. **Any field** finds text wherever it is, whatever its case. **Main columns**, **Standard columns** and **All columns** each give a box for those columns, and a value typed with no operator finds records whose field contains it, whatever its case: `seattle` in RadioTrail's city box finds "Seattle, Washington". A value with no operator also takes the Homer search syntax, reading the field as a name: `*.com` is a value ending in .com, `smith | jones` either, `!draft` not. A value may begin with an operator: `=` is, `!=` is not, `>` more than, `>=` at least, `<` less than, `<=` at most, `%` contains, `!%` does not contain. Is not and does not contain also keep records whose field is empty. An operator alone tests for an empty field: `=` alone finds records whose field is blank, and `!=` alone finds those whose field holds something. A number typed after `>`, `>=`, `<` or `<=` is compared as a number, so `> 35` in an age box finds ages above 35; a blank, or text that is not a number, never passes. Two values joined by two dots give a range, both ends included: `2025..2026` in date_read finds the books finished in those two years, `2026-01-01..2026-03-31` a span of dates, and `5..12` a span of numbers. A year stands for every day in it, since dates are kept as year-month-day. Several filled boxes must all match.
  - **With a filter in force,** Control+F first offers its buttons. **Start over** begins a new filter. **Clear filter** shows every record again and returns you to the list on the same record. **Narrow filter** keeps only the records that also meet a new condition, joining it with AND. **Expand filter** also takes in the records that meet a new condition, joining it with OR. Then it asks how to give the new condition, as above.
  - **Grouping.** The condition in force, however many parts it has, is wrapped whole in parentheses before the AND or OR, and the new condition in its own. So narrowing and expanding in turn always mean exactly what was asked. Say Filter (Shift+F) speaks the whole condition, led by "where" and grouped the same way.
  - **Nothing found.** A condition that would keep no record says so and leaves the view as it was.
  - **Clear Filter** (Control+Shift+F) removes the filter, keeping you on the same record. A table that reopens with a filter still in force says so as it opens.
  - **Filter with Regex** filters by pattern; **Say Filter** (Shift+F) speaks the current filter.
- **Order Records** (Alt+O) has the same buttons as Show Columns. In any order, blank values come last, whether the order is ascending or descending. **Prime columns** comes first and is the default (the first focus, so Enter takes it): it sorts by the primary key, prime's fields, ascending. **Look columns** sorts by look's fields, the default columns. **Main columns**, **Standard columns** and **All columns** sort by those columns, ascending. **Choose columns** picks the columns and the direction of each, in two lists; **Type columns** takes them separated by commas, with desc after a column for largest or latest first. When look and prime hold the same fields, one button, **Look columns**, stands for both, in Order Records and Show Columns alike. Last comes **Reverse order** (R), which turns the order in force the other way, as **Reverse Order** (Alt+Shift+O) does from the grid.

Sorting reads numbers as numbers: volume 9 comes after 7 and 6.5 even if some
values were stored as text and others as numbers. It also ignores capital
letters, so "spencer" sorts with "Spencer".

Because these only change the view, you can slice a large table down to what you need, work on it, and clear the filter to see everything again — the records were never removed.

### Choosing displayed columns

**Show Columns** (Alt+S) chooses which fields appear in the grid, and in what order. Showing only the columns you are working with keeps cell-by-cell navigation short. Its buttons are:

The dialog goes straight to its buttons. To hear which columns are shown now, press Say Shown Columns (Shift+S) in the grid.

Each button is on its own line and names its columns in parentheses, so you hear what a choice holds before you make it: for example, **Look columns (name, country)**. A long list wraps inside its button. Alt and the button's letter still choose in one keystroke, and Shift+F1 says what the button does. Order Records lists its columns the same way.

- **Look columns** shows the default columns: look's fields, in look's order, the way each record is said, and the columns a table opens with. With no look, every main column. Enter picks it.
- **Prime columns** shows the primary key: prime's fields, in prime's order, which together make each record unique and set the default order.
- **Main columns** shows the columns about the table's subject.
- **Standard columns** shows the standard columns the table has.
- **All columns** shows every column.
- **Choose columns** opens two lists, Columns available and Columns shown, with the columns now shown already in Columns shown, so you can add or drop one; the order of Columns shown is the grid's order. Order Records uses the same dialog for a sort, as Fields available and Order of fields, with Add ascending, Add descending and Prime order.
- **Type columns** takes the column names separated by commas.

**Where am I, and what does this do?** Control+Slash, Control and the question mark, says where you are: in a dialog, the dialog, what it is for, the control with focus, every control and the keys; in the main window, the database, table, record, filter and order, then what the table is for. Pressed twice, it shows the same in a window you can review, followed in the main window by every command and key. Shift+F1 says what a control does: its sentence, its whole description and the keys that work in it; pressed twice, the same in a window. Speech can always be stopped with Control.

**What each field is for.** In the record, new-record and filter forms, Shift+F1 on a field says what it is for. The sentences are rows of the database's own defs table: tbl and fld name the field, val is left empty, and descrip holds the sentence; tbl `*` covers a field of that name in every table. Edit a row of defs to change a sentence. The rows that do have a val are the choices F4 offers for that field.

**What each table is for.** A defs row with tbl naming a table, and fld and val both empty, describes the table in one sentence. Shift+F1 says it in Open Table, Say Database prints it, and Where Am I pressed twice says it for the table open.

**Patterns for fields.** A field's own row in defs -- tbl and fld naming the field, val empty -- has a pattern column beside descrip. Put a .NET regular expression there, and the field's value must match it when you press OK in a record form; the first field that does not is said and takes focus, with its text selected. An empty value is tested too, so a pattern can make a field required: `^\s*\S` refuses a blank, and `^$|` in front of any pattern lets the field stay blank. What DbDo says on a failure is the field's description, so write the form a value takes into descrip, such as "starting with `http://` or `https://`". Filter Records checks no patterns. To set them without editing defs, use Settings, then Field Validation for This Table: one box per field, saved into defs. In a record or new-record form, **Alt+Shift+P** (P for pattern) edits the pattern of the field you are on: a small dialog shows it under the field's description, OK saves it and puts it in force at once, and an empty pattern removes it.

**Why descrip, not desc.** the defs table's column names are short so they cannot be mistaken for SQL words: tbl, fld, val. AccAudit, where this table began under the name lookups, called its description column desc, the commonest short form; but DESC is SQL's word for descending order, reserved in Access, SQL Server, MySQL and PostgreSQL, where a column of that name must be quoted in every query, and "ORDER BY desc DESC" reads as a riddle. So the column is descrip, by decision of 3 November 2026.

**What the database is for.** A defs row with tbl `*` -- every table, which is to say the database -- and fld and val empty describes the database itself; Say Database prints it as About. (A `*` row with a field named describes that field in every table.)

**How DbDo's dialogs name things.** Every control's name has at least two words, OK, Cancel, Help and Close aside, and its letter begins one of them. Columns are what the grid shows and arranges; fields are what a record holds, which filters and sorts work on. A filter's actions are Start over, Revise filter, Narrow filter, Expand filter, Invert filter and Clear filter wherever they appear, with Clear filter last, as Reverse order is last in Order Records: each has a key of its own from the grid. Shift+F1 on any control says what it does in one sentence.

The choice is remembered with the table. **Say Shown Columns** (Shift+S) speaks it. Show Columns, Order Records and Filter Records are the three questions that shape what you see -- which columns, which order, which records -- and Shift with the same letter says each answer: Shift+S, Shift+O and Shift+F. (Show Columns was called Select Columns until 23 November 2026; in Windows, to select is to highlight.)

### Kinds of fields

A table has two kinds of field, and every DbDo dialog lists them in the same order. Knowing the order tells you where to find a field in Show Columns, Order Records, Filter Records and the record forms alike.

- **Main fields** come first, in the table's own order. They are about the table's subject: a contact's names and phone numbers, a station's name and country. A field ending in _id that points to a record in another table, such as category_id, is a main field too.
- **Standard fields** surround the main fields, in this order: the record's own id, added and edited first; then the main fields, in the order you would enter them; then notes, tags, url, look, prime and marked. That is the order every template's tables are built in, and the order every list of columns follows. Show Columns, Order Records and Filter Records leave look and prime out of Standard columns and All columns, since they only repeat other columns; Choose columns and Type columns still offer them. Every DbDo table has them. You type url, tags and notes. Look is a short summary that tells one record from the others, and prime is the combination of fields that must be unique in the table; DbDo figures out both from the record's main fields. DbDo keeps marked, added, edited and the id up to date by itself.

**Look and prime, in one sentence each.** Look is how a record is said, and its fields are the **default columns**: the grid shows them unless you choose others. Prime is the **primary key**: its fields, together, make each record unique, and they set the **default order**. They are fields that say what the record is, never an id: Look and Prime Fields refuses an id, and Import leaves a file's own row numbers, such as the Titanic's passengerid, out of the prime. So a person is said first name first, by look, and sorted last name first, by prime, with tiebreakers until no two records can share a place. In Show Columns, Look columns brings back the default columns and Prime columns shows the primary key; in Order Records, Prime columns brings back the default order and Look columns sorts the way records are said. A short way to hold it: look is the record's label; prime is its key. Every change to the columns, sort or filter is saved at once with the table.

Look and prime never include url, notes, tags, marked, added, edited or the record's own id.

### Links read as look

A field ending in _id that points to a record in another table holds a number that means nothing to a listener. So DbDo shows and speaks that record's **look** instead, such as "Mazrui, Ali". When another record in that table has the same look, DbDo shows its **prime**, which is unique, so you can tell the two apart. You never have to hear the bare number. Sorting by a link sorts by what it shows, and Keywords, Jump and Filter Records find a link by what it shows. In a record form, the link's box shows the look too: press F4 or Alt+Down Arrow to pick from the linked records, or type the look or the id. DbDo stores the id. Exports to CSV and spreadsheets keep the number, so the data can come back in cleanly.

### Relationships between tables

DbDo recognizes relationships recorded in either of two ways, or both in the same database, and treats them alike:

- **Foreign keys, the traditional way.** A field holds the id of a record in another table: an order's customer, a track's album. DbDo follows a field named for a table in the singular, such as `job_id` for the `jobs` table, as the Northwind and Chinook templates name theirs. Keys declared in the database itself appear in Table Summary (Alt+T); following a declared key whose field is named differently is planned.
- **The maps table, the DbDo way.** A map links one record to another, names how they are related, and needs no table built for that one purpose.

The same commands follow both:

- **Say Whole Record** (Shift+W) ends with what the current record is related to, by foreign key and by map, each related record by its look.
- **Related Records** (Query menu) shows the same list in a window.
- **Enter Child Table** (Alt+Right) follows a relationship into the related records in another table, opening them filtered to the record you came from.
- **Exit Child Table** (Alt+Left) returns to the record you came from; **Exit to Root Table** (Alt+Home) returns all the way up a chain.

### Adding related records

New Record (Control+N) and New Copy (Control+Shift+N) work in every window, however you reached its table. When the window holds related records, a new record starts out related the same way, so it stays in the window:

- **By a link field.** In a window opened from a contact, a new record's contact_id starts as that contact, shown as its look. Press F4 in the field to pick another. If you pick another, DbDo says the record now belongs elsewhere, since this window lists only the first contact's records. A window you filtered to one linked record, such as author_id = 17, starts new records the same way.
- **By a map.** In a window opened through a map, such as events via presents, DbDo adds the map that links the new record to the record you came from, and says so.
- **Through another table.** In a window opened two steps away, such as locations via events, DbDo cannot tell which record in the middle to use. It adds the record and says it is not linked yet.

A related table with no records yet still opens, so you can add the first one. Enter Child also offers a map kind that other records of the table use but this one does not yet, with a count of 0, so its first such link can be made the same way.

In any record form, a link field shows the linked record's look, F4 or Alt+Down Arrow picks another, and Edit Cell on a link column works the same way.

A map names each record by its table and its prime, not its number, so a link still holds after a merge or an export, when the numbers change. Two small triggers on each table keep links whole as you work: when you edit a field that is part of a record's prime, its links follow the new prime, and when you delete a record, its links go with it. DbDo adds these triggers to every table it makes, and every template carries them.

This is how you walk a database -- from an order to its line items, from a person to the jobs they are a contact for -- entirely by keyboard, with each step announced.

---

## Part 3 — Asking, analyzing, producing and automating

DbDo has many ways to work with data, and each has one job. Pick by what you want to do.

### To ask a question

- **Filter Records** (Control+F) shows only the records that meet a condition, with no SQL: text in any field, or a value in each column. **Facet Column**, on the Query menu, lists the values of the current column with their counts, and shows just the records holding the one you pick.
- **Query** (Control+Q) asks in **SQL**, the database's own language, for answers no single command gives: counts by group, averages, records joined from several tables. The answer opens as a table you move through like any other. **Query History** (Alt+Shift+Q) asks a recent question again.
- **Chat about Table** (Shift+F12) asks in plain words, of a model on this computer, so the records are not sent anywhere.
- **Evaluate Expression** (Control+Equals) works out a single calculation -- 2+2*10, a date plus 30 days -- and says the answer.
- **The dot prompt** (Control+GraveAccent) takes the same commands and short SQL typed as lines, for people who think in commands.

### To analyze what you see

Two commands analyze the grid: **Generate Statistics** (Alt+Shift+S) for reports you read, and **Generate Graphics** (Alt+Shift+G) for charts you hand to sighted colleagues. Both open the same dataset dialog, with four choices:

- **Which columns** (Alt+C): the current column, the shown columns (the usual start), or all columns, hidden ones too.
- **Which records** (Alt+R): all records in view, the marked records (offered, and chosen first, when some are marked), or the current record. This takes the place of the Marked or Current question.
- **Arrange** (Alt+A): by column, where each column is a variable and the records are its values -- the usual way -- or by row, where each record is a series across the columns, such as a record holding twelve months. By row lets you sum up or draw the current record alone.
- **Kind** (Alt+K): what to make. Generate Graphics adds **Drawn by** (Alt+D): Python or Excel.

Every report and chart starts with its takeaway, then the details.

Generate Statistics offers these kinds:

- **Profile,** the first: one line per column, so you can arrow through the whole table's shape. Each line gives the column's kind, how full it is, and its range and middle, its span of dates, its yes count, or its number of different values and the most common one. Columns blank in every record are listed together at the end.
- **Summary:** each chosen column's range, middle and spread, and values far outside the rest. Arranged by row, each record's total, average and middle, and which column is highest and lowest.
- **Frequency:** how many records hold each value of one column, or each combination of several, with a running percentage.
- **Group:** a number column totalled and averaged for each value of another column, or for each year, month, weekday, day or hour of a date. Hour is offered when the dates carry a time, and the time is taken as written, so a time zone after it does not shift it. A yes-or-no column kept as 0 and 1, such as survived, can be averaged too: its average is the share that are 1.
- **Relation:** how strongly each pair of number columns rises and falls together, and the straight line that best fits the strongest pair. A numbering, such as a record number, is left out. Last comes where the strongest pair's points fall: each column's range split into four equal parts, and one line for each quarter of the second column, highest first, counting the points across the quarters of the first, lowest first. It tells you where the points cluster, the way a sighted reader sees it on a scatter plot.
- **Cross-tab:** one column by another. **Count records** counts the records holding each pair of values. **Average a number**, offered when there is a number column besides the two, gives the average of that column for each pair -- the average fare for each sex and class, say -- then each row's average, each column's average and the overall average, the takeaway first. This is the table pandas calls a pivot table. Numbers among the values read in number order: class 1, 2, 3.

Generate Graphics offers a bar chart (how often each value occurs), a Pareto chart (the same, largest first, with the running share), a pie chart (the split of a yes-or-no column), a histogram (how a number column spreads), a whisker plot (the middle and range of each number column), a scatter plot (one number column against another, with the line that fits), a line chart and a date chart (records by month, year or season). When a date column is among the columns chosen, the line chart draws each number column across its dates, in time order, and its page names the highest and lowest of each line and when they came. Arranged by row, the bar and line charts draw each record across its number columns, up to eight records. Each chart's web page says what the chart shows before its numbers: the kind of chart, each axis with the values marked on it, and what is drawn in words that fit the kind, such as each box of a whisker plot with its middle, box, whiskers and outliers. A scatter plot's page adds a table of where its points fall.

Drawn by Python, a chart is four files in Documents, DbDo Charts, named for the table, the chart and the time:

- **A web page** with the takeaway sentence, the picture with the same sentence as its description, what the chart shows in words, and the numbers as a table.
- **A picture,** as PNG for a message or a slide, and SVG for print.
- **An Excel workbook** with the numbers and a real Excel chart a colleague can restyle, then every record used. Excel is not needed to make it.

DbDo says the takeaway, then offers to open the page, the workbook or the folder. The colors are chosen so that a reader with a color vision difference can tell them apart. Python and its free chart packages (pandas, matplotlib, seaborn and openpyxl) install themselves when first needed; if they cannot, DbDo offers the installer, or Excel. Drawn by Excel, the chart opens in Excel as before 21 November 2026, from every record in view, and needs Microsoft Office.

Filter and order first to shape the grid, then report on exactly what you see.

### To produce something to keep or share

- **Export Data** (Control+Shift+X) writes the grid -- shown columns, kept records -- to a file: CSV, TSV, Markdown, a web page, Excel, Word, JSON, an .inix table, or another database. It is the one way to write data to a file.
- **Run Report** (Alt+Shift+R) writes a designed document from a report definition, the same way every time, from the whole table rather than the grid. Definitions live in report.inix beside the database (see "The report-template language" in Part 6); BookTrail, ContactTrail, RadioTrail and TechTraining come with some.
- **Print** (File menu, no key) prints the current record or the marked ones. Control+P plays media, as in every Homer app.
- **The clipboard:** Copy Cell (Control+C) and Copy Append (Alt+C); Copy Append Record (Alt+Shift+C); and, on the Misc menu, Copy Column, Copy Grid, and Copy Visible Cells, as tab-separated text that pastes into a spreadsheet.
- **Save As** (Control+Shift+S) copies the whole database, to keep it or to change its format.

### To automate what you repeat

A **script** is a file that does a task again, run with **Invoke Script** (Alt+V), written or changed with **Edit Script** (Alt+Shift+V), its name and key in EdSharp too. A database's own scripts sit beside it, and shared ones in DbDo's Scripts folder (**Open Script Folder** opens it). There are three kinds, from the simplest:

- **.sql -- a saved question.** One or more SQL statements: a Query you keep. Most templates come with some: ContactTrail's Emergency Contacts, RecipeTrail's Shopping List, JobTrail's Weekly Count.
- **.dbdo -- a saved sequence of DbDo commands,** one per line, as typed at the dot prompt: filter, order, select columns, export. For a routine you would otherwise do by hand each week, such as JobTrail's Stale Applications.
- **.js -- a program,** in JScript .NET, for work that needs decisions and loops: reading and changing records one by one, or bringing data in from another program, such as ContactTrail's Import Outlook Contacts.

The same JScript .NET works inside Evaluate Expression and in a report definition's {{ ... }} expressions, so learning it once serves all three.

### Recording a macro

A **macro** repeats a sequence of moves you made, without writing a script. In this first version a macro holds the record moves: **First Record**, **Last Record**, **Next Record** and **Previous Record** on the Navigate menu, and **Go to Record** (Control+G) with a row number.

1. **Record Macro** (Control+Shift+F9, on the Tools menu) asks where the macro may play. **This View** keeps it to this database, table, filter and order; **Any View** lets it play on any table with records.
2. Make your moves from the Navigate menu, or with Go to Record and a row number. DbDo says each one as usual.
3. Press **Record Macro** (Control+Shift+F9) again to stop. DbDo says how many steps it recorded and asks for a name, which becomes the macro's file name in the Scripts folder. If you cancel, **Save Macro** (Tools menu, M) saves it later.

**Play Macro** (Alt+Shift+F12) plays the macro you recorded or loaded last, in the window you are in. A saved macro is also listed by **Invoke Script** (Alt+V), beside your scripts; choosing it plays it. Press Control+Shift+F9 while a macro plays to stop it.

**What makes a recording incomplete.** Only moves a macro can repeat are recorded. If you use another command while recording, press an arrow, Home, End or Page key in the grid, type a letter to jump to a row, or give Go to Record a percent, a relative amount or text, DbDo says so at once, and the recording will not play. Say commands are fine: they only listen. Cancelling Go to Record's prompt records nothing and keeps the recording complete.

**Every step is checked.** As a macro plays, DbDo works out the row each step should reach -- Next Record from row 3 should reach row 4 -- and compares it with the row it did reach. Both go into DbDo's log, and a step that lands somewhere else stops the macro there and says why. scripts\auditMacros.cmd --runtime reads your newest log and lists every step compared.

A macro is an .inix file you can read and edit in any text editor: a FileTask = macro header and one [step] section per move, each naming the command, such as Go to Record, and its row number when it has one.

### Ordering by one column, then another

**Add Column to Order** (Alt+A) orders the records by the column you are in, smallest or earliest first, and keeps the order that was already in force for breaking ties. **Alt+Shift+A** does the same, largest or latest first.

The column you add last counts most. To order contacts by state, and by city within each state, go to the City column and press Alt+A, then go to the State column and press Alt+A. It works the way a stack of index cards does: sort them by city, then sort them by state, and they end up by state, with each state's cards still in city order. DbDo says the result, such as "Ordered by state, then city."

Pressing Alt+A or Alt+Shift+A on the column that is already first just sets its direction. Order Records (Alt+O) shows the whole order and lets you start over from look or prime; Reverse Order (Alt+Shift+O) turns every column around.

### Hearing a whole record

**Say Whole Record** (Shift+W) says every field of the current record that has a value, each name followed by its value, including the columns that Show Columns hides. A link field says the linked record's look, not its number. Press Shift+W twice to read the same lines in a window. The screen reader's own read-line command already says the columns you see; Shift+W is for the whole record without leaving the grid.

**Lean and whole.** Shift+L says the current record lean: just its look. Shift+W says it whole: every field with a value, then what it is related to. They follow the reading keys, where W is wide and L is lean: Alt+W and Alt+L read many records, and Shift+W and Shift+L say the one you are on.

### One file carries it all

A DbDo database can carry everything it needs besides its data -- its scripts, its report definitions, its transfer maps and its settings -- inside the .db itself, so sharing the one file shares everything you have built around it. The person you send it to opens it in DbDo, and its reports, scripts, opening table and newest-first orders are there.

- **Pack Files into Database** (Tools menu, P) puts the scripts and .inix files beside the database into it. The database's own settings file travels as settings.inix, a name that survives renaming the database. Packing a file again replaces the copy it carried.
- **Unpack Files from Database** (Tools menu, U) writes the carried files back beside the database, to read or change them; pack them again afterwards.
- **A file beside the database wins** over a carried file of the same name, so you can always override what a database carries with a copy of your own.
- **Your own choices stay yours.** The columns, orders and filters you pick are saved beside your copy, never inside the .db, so opening a shared database never changes it.
- **Every template carries its own files,** so a template copied anywhere keeps its scripts and reports.

Each carried file's **kind** names its format in the same words a column's declared type uses -- TEXTSQL for a question, TEXTDBDO for a routine, TEXTJSCRIPT for a program, TEXTINIX for a report definition or settings -- since one column holds them all. DbDo knows forty column types, from the kit's vocabulary, and offers every one when you define a table. A name's first word says how it is stored -- TEXT for text, BLOB for bytes -- and the rest names the format by its usual file extension, as downloads do: TEXTJSON, TEXTICS and TEXTSQL hold text in those formats and are edited on several lines; BLOBPNG, BLOBPDF and BLOBMP3 hold pictures, documents and sound; and TEXTURL, TEXTEMAIL, TEXTPHONE and TEXTPATH hold a single web address, email address, phone number or file path, each with its natural action.

The files are kept in DbDo's own table, **files**, beside defs and maps; DbDo leaves all three out of Open Table, as tables of its own, except in a design window, where Open Table lists only them. A program in another language, such as RadioTrail's fetchStations.py, which needs Python to refresh the station list, is not carried: it is a tool, not part of the database.

### Which one do I want?

- **To see some records:** Filter Records, or Facet Column.
- **To answer a question once:** Query, Chat about Table, or Evaluate Expression.
- **To understand a column or a pair:** Generate Statistics.
- **To show a sighted colleague:** Generate Graphics.
- **To hand data on:** Export Data, or the clipboard.
- **To make a document:** Run Report.
- **To do it again next week:** a script -- .sql for a question, .dbdo for a routine, .js for a program.

---

## Part 4 — Importing, exporting, opening, saving

DbDo distinguishes four file operations that are easy to confuse. The distinction is about whether the data keeps its original shape or is reshaped into DbDo's conventions.

- **Open** (Control+O) opens a file *faithfully* — a round trip. An Excel workbook opens as its sheets, a CSV as its rows, and saving writes back to that same file in that same format. Use Open when you want to work with a file as it is.
- **Import Data** (Control+Shift+I) reads an outside file and reshapes it *into a DbDo database* — normalizing columns, applying conventions, and producing a `.db` you then work with. It pairs with **Export Data** (Control+Shift+X). Use Import when you want to bring foreign data into a proper DbDo database. **Transfer Import** and **Merge Data** (Control+Shift+M) bring data in alongside existing data. When Import reads a CSV, TSV or Excel file, a column that holds only whole numbers becomes a column of whole numbers, and one that holds only numbers a column of numbers, so filters, orders and reports compare them as numbers; a blank in such a column stays empty, not zero. A number with a leading zero, such as the zip code 02134, keeps its column as text, so the zero is not lost. Column names become lowercase words joined by underscores: date.utc becomes date_utc. Merge Data finds a file's columns by either name. A table keeps one record for each set of values, so a row that repeats another exactly is left out; Import says how many it left out.
- **Export Data** (Control+Shift+X) writes the **grid** — the current view — out to a new file in a chosen format. Use Export to hand off what you are looking at. A CSV or TSV file holds your values exactly as they are. Since a spreadsheet may run a value beginning with =, +, - or @ as a formula, DbDo asks before opening such a file, and an .xlsx export is the safe way into a spreadsheet: it keeps every text value as text, and writes a column the database keeps as numbers as Excel numbers, ready to sum and chart.
- **Save** (Control+S) and **Save As** (Control+Shift+S) concern the *whole database*. For a workbook opened faithfully, Save writes your edits back to the `.xlsx`. For a native database your edits were already committed, so Save reports that; Save As writes a fresh copy. **Open as Managed Copy** opens a file as a working copy so the original is untouched until you decide.

Supported formats include SQLite (`.db`, `.sqlite`, `.sqlite3`), Access (`.mdb`, `.accdb`), Excel (`.xlsx`, `.xls`), dBASE (`.dbf`), and delimited text (`.csv`, `.tsv`, `.tab`, `.txt`). The right driver is chosen from the extension.

---

## Part 5 — SQL and scripting

### Running SQL

- **Query** (Control+Q) opens a box where you type SQL and run it; the results open as a recordset you can navigate like any table. **Query History** (Alt+Shift+Q) recalls previous queries.
- The **dot prompt** (Enter Console, Control+GraveAccent) accepts both dBASE-style dot commands and short SQL inline. It is the fastest path for people who think in commands.
- **Sqlean Console** (Control+Shift+GraveAccent) opens a console with the SQLean extension functions available (see Part 6).

DbDo runs your SQL against the live connection, so queries see uncommitted context the way the rest of the program does. Because the cursor is client-side, result sets are fully navigable — forward, backward, and by position.

### The scripting engine

DbDo's scripting language is **JScript .NET**. The same engine powers three things, so learning it once pays off in all of them: **Invoke Script** for `.js` scripts, **Evaluate Expression** (Control+Equals) for one-off calculations, and the `{{ ... }}` expressions inside report templates.

- **Evaluate Expression** (Control+Equals) prompts for an expression, evaluates it, and speaks and shows the result — `2+2*10`, string manipulation, date math. The result is shown rather than only copied, so it stays reachable even where clipboard access is restricted. The last expression is remembered for quick tweaking.
- In scripts and report expressions, field values are available for substitution and computation, so an expression can combine, format, or conditionally choose field values.

How the three kinds of script divide the work, and how to run them, is in Part 3, under "To automate what you repeat".

---

## Part 6 — Reference

### Command reference by menu

Chords use screen-reader-canonical key names. A command with no chord is reachable through its menu or through Alternate Menu (Alt+F10).

#### File menu

| Command | Chord |
|---------|-------|
| New Database | — |
| Add Table | — |
| Open Database | Control+O |
| Open Template Database | Control+Shift+O |
| Design Database | Alt+Shift+D |
| Open Table | Control+T |
| Recent Databases | Alt+R |
| Save | Control+S |
| Save As | Control+Shift+S |
| Close Database | — |
| Backup Database | — |
| Compare Database | — |
| Merge Data | Control+Shift+M |
| Transfer Import | — |
| Run Report | Alt+Shift+R |
| Export Data | Control+Shift+X |
| Print | — |
| Exit DbDo | Alt+F4 |

#### Edit menu

| Command | Chord |
|---------|-------|
| New Record | Control+N |
| Edit Record | Control+E |
| Edit Cell | F2 |
| Delete Record | Control+D |
| Delete without Confirmation | Control+Shift+D |
| Copy Record | Control+Shift+C |
| Copy Append Record | Alt+Shift+C |
| New Copy | Control+Shift+N |
| Mail Record | Control+M |
| Replace | Control+R |
| Replace with Regex | Control+Shift+R |
| Extract with Regex | Control+Shift+E |
| Yield with Regex | Control+Shift+Y |
| Mark Record | SemiColon |
| Say Marked | Shift+M |
| Toggle Mark | Control+Space or Space |
| Edit Notes | Alt+Shift+N |
| Edit Tags | Alt+Shift+T |
| Edit url | Alt+Shift+U |
| Unmark Record | Slash |
| Mark All | Control+A |
| Unmark All | Control+Shift+A |
| Invert Marked | Control+I |
| Mark and Next / Previous | Shift+Down or Shift+Period / Shift+Up |
| Unmark and Next / Previous | Control+Shift+Down or Shift+Comma / Control+Shift+Up |
| Mark to First / Last | Control+Shift+Home / Control+Shift+End |
| Unmark to First / Last | Alt+Control+Shift+Home / Alt+Control+Shift+End |
| Next / Previous Marked | Control+Down / Control+Up |
| First / Last Marked | Control+Page Up / Control+Page Down |
| Start Mark / Complete Mark | F8 / Shift+F8 |
| Complete Unmark | Alt+Shift+F8 |
| Save Bookmark | Control+B |
| List Bookmarks | Alt+B |
| Clear Bookmark | Control+Shift+B |
| Open Cell Value | Control+Enter |
| Open url | Control+Shift+U |
| Pick Value | Control+F2 |

#### Navigate menu

| Command | Chord |
|---------|-------|
| First / Last Column of the Row | Home / End |
| First / Last Row of the Column | Control+Home / Control+End |
| First / Last Record | — |
| Next / Previous Record | — |
| Go to Record | Control+G |
| Go to Record Again | Alt+G |
| Jump Again | Alt+J |
| Keywords Again | Alt+K |
| Keywords | Control+K |
| Reverse Keywords | Control+Shift+K |
| Jump | Control+J |
| Reverse Jump | Control+Shift+J |
| Forward Find with Regex | Control+F3 |
| Reverse Find with Regex | Control+Shift+F3 |
| Search Next / Previous | F3 / Shift+F3 |
| Enter Child Table | Alt+Right |
| Exit Child Table | Alt+Left |
| Exit to Root Table | Alt+Home |

#### Query menu

The Query menu holds record inspection and the Say-X status family.

| Command | Chord |
|---------|-------|
| Inspect Record | Alt+I |
| Table Properties | Alt+Enter |
| Related Records | — |
| Show Schema | — |
| Say Status | Shift+Z or Alt+Z |
| Say Database | Shift+D |
| Say Order | Shift+O |
| Say Go to | Shift+G |
| Say Yield | Alt+Y |
| Say Yield Marked | Alt+Shift+Y |
| Say Yield from Current | Control+Y |
| Say Yield Marked from Current | — |
| Say Marked | Shift+M |
| Say Edited | Shift+E |
| Say Notes | Shift+N |
| Say Tags | Shift+T |
| Read Wide | Alt+W |
| Read Wide Marked | Alt+Shift+W or Shift+Space |
| Read Wide from Current | Control+W |
| Read Wide Marked from Current | — |
| Read Lean | Alt+L |
| Read Lean Marked | Alt+Shift+L |
| Read Lean from Current | Control+L |
| Read Lean Marked from Current | — |
| Read Headed | — |
| Read Headed Marked | Alt+Shift+H |
| Read Headed from Current | Control+H |
| Read Headed Marked from Current | — |
| Say Filter and Order | Shift+8 |
| Say Position | Alt+Delete |
| Say Clipboard | Alt+Apostrophe |
| Say Added | Shift+A |
| Say Cell | Shift+C |
| Say Filter | Shift+F |
| Say Keywords | Shift+K |
| Say Shown Columns | Shift+S |
| Say Query | Shift+Q |
| Say id | Shift+I |
| Say Look | Shift+L |
| Say Header | Shift+H |
| Say Replace Values | Shift+R |
| Say Invoke Script | Shift+V |
| Say url | Shift+U |
| Say Whole Record | Shift+W |
| Say Prime | Shift+P |
| Filter Records | Control+F |
| Clear Filter | Control+Shift+F |
| Filter with Regex | — |
| Clear Order | — |
| Order Records | Alt+O |
| Reverse Order | Alt+Shift+O |

#### Misc menu

| Command | Chord |
|---------|-------|
| Refresh Records | F5 |
| Read Only Toggle | Control+Shift+Z |
| Say Database, then its summary | Shift+D |
| Table Summary | Alt+T |
| Add Column to Order | Alt+A |
| Add Column to Order Descending | Alt+Shift+A |
| Show Columns | Alt+S |
| Generate Graphics | Alt+Shift+G |
| Generate Statistics | Alt+Shift+S |
| Copy Append | Alt+C |
| Copy Cell | Control+C |
| Copy Visible Cells | — |
| Copy Column | — |
| Copy Grid | Control+F8 |
| Jump to Next Initial | — |
| Query | Control+Q |
| Query History | Alt+Shift+Q |
| Test Integrity | — |
| Hotkey Summary | Help menu; Control+Slash pressed twice |
| Open as Managed Copy | — |
| Field Statistics | — |
| Facet Column | — |
| Open in Explorer | Alt+Pipe |
| Open Command Prompt | Control+Backslash |
| Open Dot Prompt | Control+GraveAccent |
| Sqlean Console | Control+Shift+GraveAccent |
| Invoke Script | Alt+V |
| Edit Script | Alt+Shift+V |
| Open Script Folder | — |
| Evaluate Expression | Control+Equals |
| Edit Settings | Alt+Shift+E |

#### Window commands, on the File menu

DbDo has no Window menu. These commands sit on the File menu, just before Exit DbDo.

| Command | Chord |
|---------|-------|
| Open Table | Control+T |
| Current Windows | F4 |
| Next Window | Control+Tab |
| Previous Window | Control+Shift+Tab |
| Window Toggle | — |
| Say Windows Open | Shift+F4 |
| Close Window | Control+F4 |
| Close All but Current Window | Control+Shift+F4 |

#### Help menu

| Command | Chord |
|---------|-------|
| Documentation | F1 |
| What This Does | Shift+F1 |
| History of Changes | none; on the Help menu |
| ReadMe | Alt+Shift+F1 |
| Alternate Menu | Alt+F10 |
| Key Describer Toggle | Control+F1 |
| Where Am I | — |
| Email Log File | — |
| Elevate Version | F11 |

### Standard fields (summary)

| Field | Type | Role | Speak with |
|-------|------|------|-----------|
| added | TEXTTIME | creation time, automatic | Shift+A |
| edited | TEXTTIME | last-change time, automatic | Shift+E |
| marked | BOOLEAN | batch-operation state you toggle | Shift+M |
| notes | TEXTMARKDOWN | free-form Markdown note | Shift+N |
| tags | TEXTMEMO | keywords | Shift+T |
| url | TEXTLINE | associated link | Shift+U |
| look | derived | display label for the record | Shift+L |
| prime | derived | prime human-facing identity | Shift+P |

### Field types (summary)

| Type | Stored as | Editor |
|------|-----------|--------|
| TEXT | text | text box |
| TEXTLINE | text | single-line box |
| TEXTMEMO | text | multi-line box |
| TEXTMARKDOWN | text | multi-line, Markdown-aware |
| TEXTTIME | text | sortable timestamp |
| INTEGER | integer | number |
| REAL | real | number |
| NUMERIC | number | number |
| BOOLEAN | true/false | toggle |
| BLOB | binary | — |

### The report-template language

A report definition is a `[section]` in a **report.inix** file beside the database. Directives configure the source; bands lay out the text.

**Directives** (at the top of the section):

- `@table` — the table to read.
- `@group` — a field to group by (implies sorting by that field).
- `@filter` — an optional Where condition.
- `@sort` — an optional sort order.

**Bands** (each is a block of literal text with substitutions):

- `header` / `footer` — once at the top and bottom of the report.
- `detail` — repeated once per record.
- `separator` — between detail records.
- `group_header` / `group_footer` — at the start and end of each group.

**Inside a band:**

- `$field` or `${field}` — insert the value of a field.
- `{{ expression }}` — evaluate a JScript expression (field values are in scope).
- `{# comment #}` — a comment, omitted from output.
- Footer bands can use aggregates: `$count`, `$sum_<field>`, `$avg_<field>`, `$min_<field>`, `$max_<field>`.
- A line that resolves to blank can be suppressed, and runs of whitespace normalized, so grouped output stays tidy.

Reports render to Markdown, which you can convert to HTML, DOCX, or PDF.

### The .inix configuration format

DbDo's own settings and several data files use **.inix**, an INI-style format: `[Section]` headers followed by `name = value` lines, with `;` or `#` beginning a comment and case-insensitive key names written in UpperCamelCase (usually two words). Global defaults live in **DbDo.inix** beside the program; a database may carry its own **`<DbName>.inix`** beside the database file for per-database overrides. Notable sections include `[General]` and `[Options]` for behavior toggles (such as the `ExtraKeys` convenience-key switch), `[Validation]` for per-field regex patterns, `[ConnectStrings]` for per-extension connection-string overrides, and `[Hotkeys]` which documents and can re-describe command chords. **Edit Settings** (Alt+Shift+C) exposes the common options in a dialog and can open the file directly for advanced editing.

Values are usually a single line, but a key may hold a **multi-line value** — a fenced block opened and closed by a line containing only `` ` `` (or a triple-quote fence, used when the content itself contains a `` ` ``), taken verbatim. A key may also hold an **array of values**. A few short items with no spaces or commas can sit inline, comma-separated (`SelectFields = last_name, first_name, enterprise`); otherwise each item goes on its own line inside a fence, most recent first. Either shape reads back as the same ordered list.

A `<DbName>.inix` holds the settings that belong to one database. Its `[Database]` section carries **InitialTable** (the table opened in the first window when the database opens, so there is always a starting window — other tables open via Open Table, Control+Shift+O) and the recent-input arrays **FindText**, **JumpText**, **ReplaceText**, **ReplaceRegex**, and **QueryText**, each up to ten items, most recent first, which populate the matching command's combo box. A `[Table:<name>]` section carries that table's saved view — **SelectFields**, **OrderFields**, and **WhereFilter** — recalled when the table is next opened.

### SQLean extensions

When the SQLean extension functions are available, they add a large library of SQL functions — string and text helpers, math, statistics, fuzzy matching, regular expressions, and more — usable in queries and reachable through the **Sqlean Console** (Control+Shift+GraveAccent). They extend what your `Query` and report expressions can compute without leaving SQL.

### Template databases

DbDo ships with template databases that follow the same column conventions as your own data, reachable with File, **Open Template Database**, or Control+Shift+O from anywhere. They are the fastest way to see the standard fields, defs, relationships, and reports working together on real records.

### Screen-reader settings

DbDo speaks directly to supplement your screen reader's own announcements. A command says its own name as it starts only when nothing else would tell you at once that it is under way. A command that opens a dialog does not, because the dialog's title says it. A Say command or a toggle does not, because it speaks its answer or its new state. A command that moves the focus does not, because your screen reader says where you landed. Others, such as Clear Filter, say their name at once, while the grid is being rebuilt. **Test Screen Reader Speech** verifies that DbDo can reach your screen reader.

### Persistence and logging

DbDo remembers per-database state in the database's own `<DbName>.inix` — the last table, sort, filter, and displayed columns, plus a pinned **InitialTable** and the recent Find, Jump, Replace, Regex, and Query inputs — so reopening a database returns you to where you were. Activity is written to a log for each session, named DbDo with the date and time it began, in `%LOCALAPPDATA%\DbDo\logs`; **Email Log File** in the Help menu reveals the log and starts an email with its path, for reporting issues.

---

## Part 7 — Development

This part is for people building DbDo from source.

### Requirements

- .NET Framework 4.8, 64-bit.
- The build tools invoked by `build.cmd` (the .NET Framework C# compiler and Inno Setup for packaging).
- The SQLite ODBC driver and the bundled dependency DLLs the build fetches.

### Build

Compile with **`build.cmd`**, which builds `DbDo.exe`, converts this guide and the README to HTML with pandoc, and packages the installer with Inno Setup (`DbDo_setup.iss`). The dependency DLLs are gathered by `getDbDoDeps.ps1`. To replicate the development layout, keep the program and its subfolders under `C:\DbDo`.

### Architecture: one connection, two interfaces

DbDo maintains a single live database connection and presents it through both the graphical interface and the dot prompt, as described in Part 1. Each MDI child window owns its own manager and cursor, so tables are independent, while the connection and drivers are shared. The cursor is client-side, which is what makes instant sort, filter, and backward navigation possible on file-based databases.

### Coding style

DbDo is written in the "Camel Type" style. The full, authoritative description is a separate distributed file, **`Camel_Type_CSharp.md`**; consult it for specifics rather than duplicating them here. In brief, it uses Hungarian-style prefixes, lower-camel method names, alphabetized declarations, double-quoted strings, and for-each iteration; database identifiers use lower_snake_case.

### Layout by Code

DbDo builds its dialogs and menus **in code** rather than with a visual designer, so that every control is a standard, screen-reader-friendly Windows control created and labeled explicitly. The label-based-controls helpers assemble a dialog from labels, input boxes, memo boxes, list boxes, and pick boxes with OK/Cancel or custom buttons, keeping tab order and accessible names correct by construction.

### File layout

The source is a single large `DbDo.cs`. Alongside it live the build script, the installer script, the dependency fetcher, the `.inix` configuration, this guide (`DbDo.md`), the README, the coding-style file, and the template databases with their scripts and `report.inix` definitions.




### Nothing, and the two kinds of it

A field can be empty in two ways, and databases have always kept them apart.

- **null** means nobody has said. The value is unknown, or does not apply.
- **blank** means somebody said nothing: a real value, a string of no length.

#### What DbDo stores

**An empty box stores null.** When you leave a field alone, or clear it, DbDo
writes null rather than an empty string.

This is the settled practice, not a preference. Allowing both in one column
gives two values one meaning, so every search has to ask for both and every
search that forgets is quietly wrong. Oracle went as far as treating an empty
string AS null; MySQL and SQL Server guidance is to forbid the empty string with
a rule on the column. SQLite keeps the two apart and leaves the choice to the
program, so DbDo makes it once, where values are written, rather than leaving it
to each dialog.

You can still store an empty string on purpose -- a space is a value -- and a
database built elsewhere keeps whatever it already holds. DbDo changes nothing
it did not write.

#### What DbDo says

**A null field says "null". An empty one says "blank".**

Both words are borrowed rather than invented. Database tools print the literal
word NULL for a null and have done for decades: phpLiteAdmin shows it in italics
so it cannot be mistaken for the text "NULL", DB Browser and SQL Server
Management Studio do the same, and the sqlite3 shell has a setting for choosing
the word. "Blank" is what JAWS and NVDA say when they reach an empty cell, in
Excel and in any grid, so it is a word you have heard for years that already
means exactly this.

#### What the screen readers do, and what DbDo does about it

The three readers disagree about an empty cell, and DbDo settles it rather than
leaving it to them:

- **JAWS says "blank."**
- **NVDA says nothing** and moves to the next cell, though it still reads the
  column header.
- **Narrator** is its own case again.

Silence is the one answer a cell must not give, because it cannot be told from a
key that did not register or a column that is not there. So **DbDo puts the word
in the cell**: an empty cell reads "null" or "blank", and all three readers say
the same thing because all three are reading the same text.

Each word is **one syllable**, so the precise answer costs no more time than a
vague one -- and "blank" is the word JAWS would have said anyway, so nothing
contradicts what you already hear elsewhere in Windows.

Accessibility guidance for data tables says the same thing from the other side:
never leave a cell visually empty, and do not use a dash, which NVDA does not
read either.

**The word is shown, never stored or copied.** Copy a cell and you get what the
cell holds, not the word; the same is true of exports and reports. Set
**ShowEmptyWords** to No in DbDo.inix for the older, silent grid.

#### What Microsoft Access does, for comparison

Access reaches the same place from the other end. Its text boxes trim what you
type and save an empty box as null; a zero-length string cannot be typed into a
datasheet at all, and takes an update query to create. Its **Allow Zero Length**
property exists to forbid them outright, and long-standing Access guidance is to
set it to No, because -- in the words of the reference most Access developers
learned from -- there is no visible difference between a zero-length string and
a null, and the distinction should not be forced on the end user.

DbDo does the same thing at the point of writing and then, unlike Access, can
tell you which one you are on when you ask.

#### Why it is worth the distinction

- A job with no applied date has not been applied to. A job whose applied date
  was cleared has been un-applied. The first is a lead; the second is a mistake
  to look at.
- Searching for null finds what nobody has filled in. Searching for blank finds
  what somebody emptied.
- **Uniqueness does not see nulls.** SQLite, like every other SQL database,
  treats each null as different from every other, so a unique column accepts as
  many nulls as you like. DbDo's `prime` column is built with coalesce for that
  reason: it turns nulls into empty text before joining, so two records missing
  the same field still collide as duplicates rather than slipping past.
- **Sorting puts nulls first.** In SQLite a null sorts before every value, so
  the rows nobody has filled in arrive at the top of an ascending sort. That is
  usually what you want and always worth knowing.

### The Help menu

Every document that comes with DbDo opens from Help, in your web browser. The
F1 keys open the ones you will want most. F1 is help in every Windows program,
and each extra key picks a different kind of help.

- **Documentation, F1.** This guide.
- **What This Does, Shift+F1.** One sentence on what the control with focus does, here and in every dialog. The sentences live in configs\Hints.inix, where you can change them.
- **History of Changes.** What changed in each version; on the Help menu and the Alternate Menu, without a key of its own.
- **About, Alt+F1.** The version and the license.
- **Key Describer, Control+F1.** Press it, then any key, to hear what that key does.
- **ReadMe, Alt+Shift+F1.** The short start: installing and a first walk.
- **Hotkeys, Control+Shift+F1.** Every key, by menu, by key and by command. Control+F1 describes one key; adding Shift describes them all.
- **Frequently Asked Questions.**
- **Play Tutorials.** The spoken walkthroughs.
- **Update from Template.** Brings your copy of a template up to date from the one installed with DbDo, by merging or replacing; see the template databases, below.
- **Update Copies from Templates** (Alt+H, then C). Does the same for every copy of DbDo's templates at once. It names your copies first, then offers **Replace all**, which makes each copy its template again -- the quick way to the newest when you have added or changed nothing -- keeping each copy as it was beside it as a dated backup, or **Merge all**, which adds to each copy what its template has and it lacks, keeps everything of yours, and asks once how records in both are treated. The database you had open is opened again afterwards -- with the template's own settings, if it was one of the copies replaced -- and a report names what happened to each copy. Files SQLite keeps beside a database while it works, such as a -wal file, are never copied.
- **More Documents.** The Announcement, the Developer Guide, the License, and the Tutorials Transcript. These are used rarely, so they sit one level down.

### Four ways to reach a record

Each has its own letter, and the letter is the first letter of its own word.

- **Jump, Control+J** -- type part of what you hear on the line and arrive there.
  No syntax, no dialog to fill in: three letters is usually enough.
- **Keywords, Control+K** -- search inside the records, every column, including
  ones not on screen. The cursor moves to the match; F3 finds the next one and
  Shift+F3 the one before. Shift+K says what you searched for.
- **Filter Records, Control+F** -- shorten the list to the records you want. It
  begins with a list of choices, the first already selected, so Enter takes it;
  each choice begins with its own letter, so typing the letter lands on it.
  - **Any field contains text** asks for the text and keeps every record where
    some field holds it, whatever the field and whatever the case: type Dallas
    Cowboys, and every station whose name, notes or tags mention the team stays.
    It takes the Homer search syntax, as Keywords does: `red & blue` means
    both, `red | blue` either, `!red` not, and `re*d` lets a star stand for
    anything (see Search syntax).
    Numbers and dates are searched as they read. Case is ignored for English
    letters; type accented letters as they appear. A search that finds nothing
    says so and leaves the list as it was.
  - **Fields** asks which fields: **Main** (the fields about the table's
    subject), **Standard** (the id, added, edited, notes, tags, url and
    marked) or **All** (every field but look and prime). It then opens a form with one box per field, in
    DbDo's natural order. A value on its own matches anywhere in the field,
    whatever the case; `=` in front means the field is exactly that; a symbol in
    front compares (`>`, `>=`, `<`, `<=`, `!=`). Type dates as year-month-day,
    such as 2026-10-29. Fill in several boxes and they all have to match.
  - **Marked** keeps the records you have marked, and **Unmarked** the rest.
  - With a filter in force, the list first offers **Start over** (a new filter),
    **Clear filter** (show every record), **Narrow filter** (keep only records that also meet
    a new condition), **Expand filter** (also take in records that meet one) and
    **Invert filter** (the records the filter leaves out).
  **Control+Shift+F** clears; **Shift+F** says what is in force.
- **Query, Control+Q** -- write SQL yourself, when you want to.

The first three need no SQL at all. The old name for Filter Records was "Where
Filter", which promised SQL to people who never write any; the dialog has not
changed.

### Sharing a database with somebody else

A DbDo database is one file, so sharing it is sending that file. What the person
receiving it does depends on whether they already keep one of their own.

**If they do not**, they save the file wherever they like -- their documents,
their desktop, anywhere -- and open it with Control+O. Nothing has to be copied
into a folder belonging to DbDo.

**If they want it in a database of their own**, they start one from a template:
File, **Open Template Database**, and pick BookTrail, which opens their own copy
under their own account. Then **Merge Data, Control+Shift+M**, and choose the file they
were sent, wherever they saved it. DbDo folds the records in.

#### How a merge decides

Every record carries a **prime**, the computed key built from the fields that say
which record it is -- for books, the author and the title. That is what DbDo
compares. A prime never holds an id, whether the table's own number, a source's
code or a link's number: each differs from one database to another, so a prime
with one would never find the record already there.

- A record whose prime is **not** in your database is **new**, and is added.
- A record whose prime **is** there is the **same record**, and you decide what
  happens to it. DbDo asks once, before anything is written:
  - **Add new only** -- nothing you already have is touched. The safe choice.
  - **Update** -- records you already have take the incoming values.
  - **Fill blanks** -- incoming values fill your empty fields and nothing else.
  - **Newer wins** -- incoming values win only where they were edited later.

**Links between records survive**, because a link names its two ends by prime
rather than by row number: a link from the other database points at the same two
records in yours. Pick lists merge too, one entry per table, field and value.

**Nothing is ever deleted by a merge**, and DbDo copies your database first,
leaving it beside the original as `<name>-before-merge.db`.

#### A shared database knows how to present itself

The fields a row speaks, the order and the filter are kept inside the database as
well as in the settings file beside it. So a database you send somebody arrives
looking the way you left it. Your own settings file still wins on your own
computer, which means your column choices do not follow the file to everybody
else.

### Files that are not databases

Spreadsheets and text files are not databases, so DbDo treats them all the same
way, and the rule is one sentence: **the file you opened stays the file of
record.**

When you open a .xlsx, .csv, .tsv or .txt file with Control+O, DbDo reads it into a working copy
and opens that. You get the whole program -- sorting, filtering, the Say keys,
reports -- and your file is only read.

- **Control+S writes your changes back** to the file you opened. A spreadsheet
  keeps its formulas, its formatting and its other sheets. A text file is
  written out afresh, and the first time you save, DbDo leaves the file as it
  was beside it, named `<name>-before-dbdo.csv`.

  In a spreadsheet, a row you deleted has its cells cleared, not removed, so a
  formula elsewhere that points at that row still points at a row. A row you
  added goes below the others. Save never replaces or removes a formula: if you
  changed a cell that holds one, Save writes everything else and then shows you
  which cells it left alone, by row and column. Those changes stay only in DbDo;
  to make one, edit the formula in Excel.
- **Control+Shift+S, Save As, keeps the working copy as a database** instead, if
  you would rather leave the spreadsheet behind and work in DbDo from now on.
- **Nothing is copied into a hidden folder.** A file you opened from your
  documents stays in your documents.

Access and SQLite files are databases already, so DbDo opens them as they are
and your edits go straight into them.

### Bringing in a spreadsheet

There are two ways, and one of them is almost always the right one.

**Open, Control+O**, edits the workbook itself. DbDo reads it with no Excel and
nothing else installed. A workbook with more than one worksheet asks which one
to edit. Control+S writes your changes back to that worksheet, keeping formulas,
formatting and the other worksheets.

**Import, Control+Shift+I** -- I for Import, beside Control+Shift+X for Export --
makes a new DbDo database from the workbook. Each sheet with a header row
becomes a table named after the sheet, each column becomes a field, and an empty
sheet is skipped. The result opens as an unsaved copy; press Control+Shift+S,
Save As, to keep it. Your workbook is only read.

Open is for when the workbook stays the file you share; Import is for when you
want a new database and no further connection to the workbook.

### Where a database's files live

Everything that belongs to one database sits in one folder, named after the
database:

    BookTrail\BookTrail.db      the database
    BookTrail\BookTrail.inix    which table it opens on, and what each row says
    BookTrail\report.inix       its reports, if it has any
    BookTrail\*.dbdo, *.sql     its own scripts

DbDo finds them by the folder, so two databases never read each other's
settings even when the files inside have the same names. A script written for
one database appears in Invoke Script only while that database is open.

**Scripts that are not about one database** -- something you would run against
any of them -- go in your own scripts folder instead, which DbDo makes at
`%LOCALAPPDATA%\DbDo\scripts` and seeds with examples. Both lists are offered
together, and where a name appears in both, the database's own script wins.

The templates that come with DbDo follow the same rule: each is a folder under
`templates`, and DbDo copies them into `%LOCALAPPDATA%\DbDo\data` the first
time you run it, so your copies are yours to change.

### The template databases

Each template is a folder under `templates`, copied into your own folder the first
time you run DbDo, or the first time after an update that adds one. Your copy is
yours. An update refreshes it only while you have never changed it; once you have,
it stays exactly as you left it.

When a newer DbDo brings an improved template -- more stations, a new script, a
corrected record -- open your copy and choose **Update from Template** on the Help
menu (Alt+H, then U). It offers two buttons:

- **Merge Data** adds what the template has and your copy lacks, and keeps everything of
  yours. A field the template has gained is added to your copy first, empty, so a copy
  made before it existed gets it too. It uses Merge Database, so it first asks what to do with records both have;
  Add new only, the first choice, changes none of yours. Your copy as it was is kept
  beside it, named with "before-merge".
- **Replace** makes your copy the template again. Your copy as it was is kept beside it
  as a dated backup, named with "before-update". DbDo keeps the two newest of these
  backups for each database and removes older ones, so they do not pile up.

Either way, a script, query or report the template has and your copy lacks is copied in
beside it; none of yours is overwritten. Merge is the first button, so Enter takes it.

**Your Outlook contacts, in one step.** ContactTrail and JobTrail each come with a script,
Import Outlook Contacts. Open the database, press Alt+V, Invoke Script, and choose
it. If Microsoft Outlook is on your computer, every person in its Contacts folder who is not
already in the database is added, with names, phones, email, address and job, and tagged
outlook so a filter finds them all. Nothing already there is changed, so running it again
adds only people who are new. Outlook's business phone becomes the Office Phone, and its
title, second address line, birthday and gender fill those fields. If Outlook is not on your
computer, the script says so and adds nothing. Press F5 afterwards to see them.

Every template earns its place in one of three ways: it carries real data worth
exploring, it serves a task people really do every day, or a recorded tutorial
teaches with it. Six earlier templates were retired in October 2026: CellarTrail,
CollectionTrail, FilmTrail and SchoolTrail for falling short of that, ConventionTrail
because its convention is past, and HowToTrail because how-to material reads better as
documents. If you already have one, it stays in your own folder and opens as it always
did; it just is no longer offered as a new template. For the next convention or
conference, HomerDev's convention-database skill builds one afresh from its agenda. The ones written for DbDo share the Trail name:

- **BookTrail** -- books read, reading, and meant to be read. It comes with six sample books; bookFido fills your copy from your Audible, Kindle, Goodreads, Bookshare and BARD libraries, and removes the samples you have not touched once your own books are in. Reading-List shows what is next, series in order, and the reading_log report writes up what you have finished.
- **ContactTrail** -- people, and the groups you sort them into. Emergency-Contacts gives the people to call with every number, Phone-List the short list to call from, Group-Roster each group's members, and the address_book report an address book to read through or print.
- **JobTrail** -- a job search: applications, contacts, and what you did, with the work search record an unemployment office asks for
- **MusicTrail** -- 100 great albums and 200 great musicians across ten genres, from two books by DbDo's author, with who recorded what and who played in which band
- **RadioTrail** -- Internet radio, some 55,000 working stations, with a script that refreshes the catalog
- **TechTraining** -- about 1,170 free presentations on technology and accessibility, from the Tech Training directory, each with its year, length, description and source. Play Media (Control+P) plays the presentation itself, and marked ones play as a list; Quick-Lessons finds the ones of fifteen minutes or less.
- **DescribedFilms** -- about 10,000 English movies with audio description in the United States, documentaries included, from the Audio Description Project of the American Council of the Blind. Each film has its year, rating, genres and the services that carry it, and Alt+Right lists each service's terms and link. Free-To-Watch finds the ones free with a subscription or ads.
- **BlindSkills** -- 84 free videos and podcast episodes that teach blindness skills, from the Blind Skills directory: cane travel, guiding, braille, cooking, dressing, labeling, money, crafts and more, in twelve skill groups, each lesson with the task it teaches. Play Media (Control+P) plays a lesson and marked ones play as a list; none come marked. Control+Enter on transcript_url opens a transcript. Skill-Index lists lessons by task, With-Transcripts those you can read, and the skills_handbook report writes up the whole set by group. scripts\checkBlindSkillsMedia, in DbDo's development folder, asks every lesson for the start of its stream, with no sign-in, as an anonymous player would, and writes a play list of those that answer with audio or video.
- **RecipeTrail** -- recipes, their ingredients and the amounts each needs. Mark the recipes you mean to cook, and Shopping-List gives what to buy, aisle by aisle, with the amount each recipe needs; Quick-Meals finds dinner in half an hour.

**chinook** and **northwind** keep their usual names. They are the two databases
most database tutorials use, so the names are how people recognise them.

#### BlindCreations gathers work by blind people

**BlindCreations** holds the apps, books, presentations and creators from the
Blind directories on GitHub Pages: about a hundred apps, a hundred books, sixty
presentations and over a hundred and twenty creators. It began with a proposal
on the BITS development list for one searchable place for software and books
made by or for blind people.

Each record names its makers in its **by** field, and the **maps** table links
each creator to their work: a creator is `author_of` a book, `developer_of` an
app, or `presenter_of` a presentation. On a creator, Shift+W ends with what they made,
and Alt+Right goes to it. An app's **platform** is where it runs, so Fields in
Filter Records narrows the list to one system. Categories, platforms, formats and
roles are also in **tags**, one to a line, so Keywords finds them.

#### TechTraining plays what it lists

Each presentation's **url** is the presentation itself, so Play Media
(Control+P) plays it in the Homer Player: an audio file directly, a video on
YouTube or a similar site through yt-dlp. Mark several with Space, and
Control+P plays them all, in order. The row speaks the title, the length in
minutes, and the **medium**: audio or video play this way, and a page -- a
presentation on its publisher's site -- opens in your browser with Control+Enter on
its url. Control+Enter on **source_url** opens the publisher's page about it, and
**resources** lists any slides, transcripts or handouts. Filter on **category**
to stay in one topic; its pick list gives each category's definition.

#### DescribedFilms says where to watch

**DescribedFilms** holds the English movies in the Film and Series Title Directory
of the Audio Description Project (ADP), run by the American Council of the Blind:
every movie ADP lists as available in the United States with audio description.
Documentaries are in, since ADP files them as movies; series are not. DbDo opens on
**films**, and each row says the title, the year and the **services** that carry
it, so Filter Records on services finds every film on one service, and on
**genres** every documentary, comedy or western. Control+Shift+U opens the film's
IMDb page.

Alt+Right goes to the film's **offers**, one row a service. **terms** says what
watching it takes there, as ADP notes it: buy, rent, free with Prime, free with
ads, or a channel subscription. **voice** says "synthetic" when ADP marks the
description as text-to-speech, for people who would rather hear a human narrator.
Control+Shift+U on an offer opens the film on that service. Four scripts come with
it: Free-To-Watch, Documentaries, Newest, and Services-Count, which counts the films
each service carries. The watch_list report writes up the films you have marked.

The database is made from ADP's directory by **`scripts\makeDescribedFilms`** in
DbDo's development folder, which fetches its pages, fifty movies to a page, in
about five minutes. The build runs it when the database is missing.

#### RadioTrail plays Internet radio

Fourteen stations are in the template to start. To build the real thing, run
**`fetchStations`** in the template's folder, `templates\RadioTrail`. It begins
by measuring what is there -- how many stations, how recent the catalog, how
many are playable, asked, looked up -- and says so; then it fetches only what
is missing, and leaves a complete copy alone. `fetchStations --report` gives the
report by itself, and fetches nothing. Its log is named first thing, in the project's `logs`
folder. From nothing, one command does four things: a clean copy from the template; the whole catalog --
some fifty thousand working stations from Radio Browser, a volunteer-kept public
directory, plus SomaFM's channels, a minute; each station asked what it says
about itself, an hour or two that can be stopped and picked up again; and, for
a station with a broadcast call sign in its name, its official record from
Wikipedia -- **call_sign**, **frequency**, **city** of licence, **owner**,
**format**, and the article's opening paragraph -- taken only when the
article's own call sign is in the station's name.

That third step is what makes searching work. The catalog knows a station's
name, tags and place, and no more. "Official home of the Seattle Seahawks" is in
the station's own stream headers and on its home page, and that is where
fetchStations reads it: the slogan into **slogan**, the rest into **descrip**,
new genre words into **genre**. Then Keywords, Control+K, finds "Seahawks".

Most stations that carry a team's games never say so themselves. A small
station in Medford, Oregon, may carry every Seahawks game while its stream and
home page talk only about local news. So the Wikipedia step also keeps each
station's **affiliations** -- the networks its article says it carries, such as
"Seahawks Radio Network" or "ESPN Radio" -- one per line in **tags**. Any Field
for `seahawks` then finds those stations too. Only some articles name a team's
network, so this finds some of a team's stations, not all: on 11 October it
added 4,059 network tags to 1,579 stations, but only KHTQ in Spokane and KIRO-FM
in Seattle for the Seahawks, out of the 65 or so stations the team lists. A copy looked up before this was
added gains its networks with `fetchStations --affiliations`, which takes
minutes, not hours.

Finding a station is not the same as hearing the game. Leagues often require
a station to block game audio on its stream for listeners outside its own
broadcast area, and teams say so on their own pages. Some listeners report
hearing a game from one station one week and not the next. So treat a team's
stations as candidates: try several, and record what worked in **status** and
**notes**, which no refresh ever changes.

A row reads name, genre and country. State is left off the row because it is
empty for most of the world and an empty cell is spoken as "blank" on every
row; once you have filtered to one country, Show Columns adds it. Every other
field is there for Keywords and Filter Records, and Inspect shows them all.
Words that vary -- the catalog's tags, a stream's genre words, a page's
keywords, the article's format -- are in **tags**, one per line. Prose -- what
the station says of itself and the article's opening paragraph -- is in
**notes**. Both only grow: a line you add to either stays through every
refresh.

To listen, press **Control+P**, Play Media, on any station's row. The Homer
Player opens -- the same player FileDir has, built from the same code in the
kit -- with the station playing: Scroll Lock pauses and resumes, the Volume and
Rate sliders are ordinary controls, and Escape closes it. Mark several stations
first, with Semicolon, and Play Media queues all of them, as tagged files are queued
in FileDir, with the current one first. The first time, if mpv is not on your
computer, DbDo offers to fetch it.

A copy you have not marked up, or one not in the template's current shape, is
replaced by a clean one, so the structure is always the template's. Once you have set a **status** or a **rating**, or written in
**notes** or **tags**, the copy is yours: it is refreshed in place, status and
rating are never written by the script, and notes and tags are only added to. `fetchStations --country "United States"` keeps a run to
one country; `--catalog-only` skips the asking; `--enrich-only` does only that.

##### The spoken walks

Every Homer program has twelve spoken walks, Help menu, Play Tutorials, in the
same order: 00 overview and table of contents; 01 install and launch; 02 what
the screen is made of; 03 the rules every key follows; 04 to 08 up to five
tasks; 09 a glossary, where one voice says the term and the other says what it
means; 10 a conclusion; 11 where the rest is. DbDo's five tasks are opening and moving
through a database; adding, inspecting and editing records; finding, ordering
and choosing columns; related and marked records, reports and a work search
record; and RadioTrail, walk 08, worked through three wants -- the station
carrying the Seattle Seahawks, found by a word it says of itself; jazz from
anywhere, by filtering on genre; jazz near home, by adding the state -- then
playing what was found, recording it, and keeping it as a favorite. The walks' scripts live in `help`; a database's own files --
its settings, its scripts -- live in its template folder.

##### Where RadioTrail's data and design come from

The stations are from **Radio Browser**, a public directory kept by
volunteers, and from **SomaFM**'s own channel list; the official records are
from **Wikipedia**. The ACB Media and NFB Radio Network addresses, and much of
the design -- keying stations by the catalog's own id rather than by a stream
address that many stations share, paging the catalog, never refreshing what a
listener has written -- follow **Quill Radio**, the successor to the ACB Radio
Tuner, whose published requirements and source were studied while RadioTrail
was built. RadioTrail owes it a debt, and its listeners the ten ACB Media
streams.

### BookTrail, for books

BookTrail is a template database for the books you have read, are reading, and
mean to read. One table, books, holds the author, title, subtitle, narrator,
series and volume, status, format, source, catalog number, genre, rating, the
dates added and read, why you chose it, a summary and your notes.

- **The list reads author, title, series, volume,** and is ordered by author,
  then series, then volume, so a series comes out in reading order.
- **Pick lists** on status, format, source and genre -- F4 in the field. A
  series spelled two ways is the commonest reason a book sorts out of place, so
  choose it from the list rather than typing it again.
- **Volume is kept as you type it.** 6.5 for a novella between 6 and 7 sorts
  between them, and 10 comes after 9.
- **Formats a blind reader uses**: BARD audio, commercial audio, braille,
  Bookshare text, large print, eBook and print.

### The spoken walkthroughs

**Help, Play Tutorials** opens `Tutorials.mkv` -- fourteen walkthroughs, about
three minutes each, as one recording with a chapter at the start of every one.
DbDo hands the file to Windows, so whatever plays that kind of file plays it. If Windows asks which app to use, pick a media player such as VLC, and choose Always so it does not ask again. In
a player that reads chapters, Control+Page Down and Control+Page Up move from
one walkthrough to the next.

The same walkthroughs are written out in `Tutorials.md`, in the help folder, for
reading rather than listening.

### Asking the model on your computer

**F12, Chat with AI**, asks a plain question. **Shift+F12, Chat about Table**,
asks the same kind of question with the table sent alongside it: its name, its
columns, how many rows it holds, and the record you are on. So "how do I write a
filter for the last thirty days" suits F12, and "which of these columns would
tell me whether I have applied" suits Shift+F12.

The keys are EdSharp's and FileDir's, unchanged, so one habit works across all
three. Nothing leaves your computer: Ollama runs locally and the model sits in
your own profile. If Ollama is not installed, either command says so and tells
you how to add it -- and it is shared with EdSharp and FileDir, so installing it
once covers all three.

### Two keys worth knowing early

**Shift+C, Say Cell**, says only what is in the cell -- "Accessibility Analyst".
**Shift+H, Say Header**, says which column you are in -- "title". Shift+Z says
where the record is, so each key answers one question.

**Shift+Z, Say Status**, gives the summary: which table, which row of how many,
and the order. Z is the bottom of the alphabet, and the status bar is the bottom
of the window.

### Tables DbDo keeps for itself

Three tables in a DbDo database belong to DbDo rather than to you:

- **defs** fills the pick lists and holds each field's description and
  pattern. F4 in a field offers what this table holds for that table and field.
- **files** holds the scripts, reports and settings the database carries.
- **maps** records links between records, which is what Say Related reads.

None is offered when you open a table with Control+T. They are on a hidden list, and
anything NOT on that list is yours and is offered.

**Design Database (Alt+Shift+D)** is where you reach them, along with the other
parts of a database you build rather than fill. Its buttons are Add table, Defs
table, Files table, Maps table and Table properties. D is for Design and for
Database: Shift+D says the database, and Alt+Shift+D designs this one. With no database open, it offers New database and
Open template database.

Defs table, Files table and Maps table each open that table in its own window,
a **design window**, where every record key works: filter, edit, F4, export.
The window decides the family: in a design window, Open Table (Control+T) lists
defs, files and maps only; in a window on your own data, it lists your tables
and views only. Control+F4 closes the design window and takes you back. DbDo
never reopens a design window by itself the next time it starts.

Take care in maps: each link names its two records by table and prime, so a
mistyped prime points nowhere.

DbDo knows them by their columns as well as their names: its maps table has
`tbl1`, `prime1`, `kind`, `tbl2` and `prime2`, defs has `tbl`,
`fld` and `val`, and files has `name`, `kind` and `content`. (A table named views, which once kept a table's saved columns, sort
and filter, is retired: those live in the database's .inix file, and DbDo removes an
old views table when it opens the database. A database may still hold SQL views of
its own, which DbDo offers like tables.) A table of your own that only shares the name -- a table of
geographic maps, say -- is yours, is offered like any other, and is never read
as links.

They are not locked away. At the dot prompt, `select-table defs` opens any
of them by name, and any SQL statement can read or change them. The rule is about
what a person meets while browsing, not about permission.

SQLite's own tables -- anything beginning with `sqlite_`, and the `sqlean_`
tables the extensions create -- are filtered out earlier and are never offered
at all.

### look and prime: the two computed columns

Every table DbDo makes carries two columns that nobody types into. They are
computed from the fields beside them and kept up to date by the database itself.

#### look: a glimpse of a record from somewhere else

`look` is what a record looks like when it is mentioned somewhere other than its
own table.

The idea is older than DbDo. In Clipper, under DOS, a form with a foreign key in
it showed a number, and a number is not a record. So the practice was to put a
glimpse of the referenced record in parentheses after the id: enough to know
which one it is, and no more. Tab through a form of fields, reach the field that
points at another table, and hear something meaningful rather than "4".

That is what `look` holds. It joins the fields that identify a record to a
person, separated by a space, a vertical bar and a space:

    Example Widgets Company (template employer) | Accessibility Analyst | interviewing

The separator is chosen for the ear: a screen reader pauses at punctuation, so
the parts arrive as parts rather than as one run-on line.

A glimpse is deliberately not the whole record. When you need the rest, it is
one keystroke away: Enter opens the record, and Shift plus W ends by listing what this
record is related to, showing each related record by its own `look`.

#### prime: the primary key, computed

`prime` is short for primary key, and it is a primary key in the sense that
matters rather than the sense SQL means.

The table does have a formal key -- `<table>_id`, a number the database hands
out -- but that number says nothing about the record. What decides whether two
rows are the same job is the employer and the title. So `prime` is computed from
exactly the fields that make a record unique, joined with a vertical bar and
nothing else:

    Example Widgets Company (template employer)|Accessibility Analyst

Two things follow, and both are the reason it is done this way.

**The rule can be changed.** Deciding that a job is unique by employer, title
and location as well is a change to one expression, not a migration of a key.

**Matching becomes one comparison.** When a script asks whether to add a record
or update the one already there, it compares one value instead of three, in SQL
or in any language: `WHERE prime = ?`. The maps table works the same way -- it
records a link as `prime1`, a kind, and `prime2` -- so a relationship needs no
knowledge of how either table numbers its rows.

#### The difference in one line

`look` is for a person, joined with spaces so it reads aloud. `prime` is for a
program, joined without them so it matches exactly.

### How a Say command answers

Every Say command answers in one shape:

    <name>: <value>

The name is the word in the Say menu and in the hotkey list, so the answer says
which question was asked. That matters when you are walking the alphabet and
lose your place: "Unmarked" does not say which key produced it, and
"mark: unmarked" does.

An empty answer keeps its label and says none -- "keywords: none", not "No find
string" -- and every count matches its noun.

**One exception, and it is a rule of its own.** A two-state answer whose value
already names the setting says the state alone: Say Marked answers "Marked" or
"Unmarked", not "mark: marked". Prefixing it would say the same word twice, and
only the second word carries information. It is the shape a toggle uses
everywhere else: "<setting> on" and "no <setting>", never "<setting>: on".

Two commands answer differently. Say id uses the column name instead of the menu
word, because that is what the grid calls the field: `job_id: 3`. Say Cell says
the value alone, `Example Widgets Company`, since Shift+H gives the column's
name.

One press speaks. A quick second press spells the answer, a character at a
time. A third shows it in a read-only window you can read line by line,
spell phonetically and copy from. Every Homer app's Say keys work this way.

#### The Say keys

- Shift+A -- added
- Shift+B -- bookmark, how many are saved and the newest
- Shift+C -- cell, its value alone
- Shift+D -- database, as file and folder
- Shift+E -- edited
- Shift+F -- filter, the condition in force, or none
- Shift+G -- go to, what Go to Record (Control+G) would offer next
- Shift+I -- id
- Shift+J -- jump, the text Jump Record would search for next, and its column
- Shift+K -- keywords, the text Keywords would search for
- Shift+L -- look
- Shift+M -- mark
- Shift+N -- notes
- Shift+O -- order
- Shift+P -- prime
- Shift+Q -- query
- Shift+R -- replace values, what Replace (match and replace) and Replace with Regex (pattern and substitute) would offer, or none
- Shift+S -- columns, the columns shown
- Shift+T -- tags
- Shift+U -- url
- Shift+V -- script, the one Invoke Script (Alt+V) would choose
- Shift+W -- whole record, every field with a value, hidden fields too, then the related records
- Shift+Z -- status: the table, the record and the order

Shift plus a letter answers a question. A letter on its own moves: type the
first letters of a value and the list view goes there, in lower case and
without regard to case. The two layers do not collide, which is why the Say
commands live on Shift.

Three letters are unused -- H, X and Y -- and are kept that way on purpose, so a
new question can be added later without moving an answer somebody has learned.
W became Say Whole Record on 5 November 2026, and X was Say Regex Replace until
the same day, when Replace became one dialog and Shift+V took on its pattern too.
Pressing an unused letter says so -- "Shift+H: no Say command" -- rather than
staying silent: the say layer always answers, because silence cannot be told
from a key that did not register.

**A command that opens a dialog with a value already in it has a value worth
hearing first.** That is what Shift+B, Shift+J, Shift+R and Shift+V are: the bookmark
list, the text Jump would offer, the pairs Replace would offer, and the script Invoke Script would offer. Hearing the
answer is often enough, and the dialog never has to open.

**none and blank are different answers.** "none" means there is no value --
nothing was ever set. "blank" means there is a value and it is empty. A listener
cannot tell those apart from silence, and they are different facts.

Shift+J used to open the Jump dialog. It now says the jump text instead, which
is what Say Jump on that key was always meant to do. Jump keeps
Control+J.

### Credits for the tutorial voices

The spoken tutorials in `Tutorials.mkv` were produced with
[piper](https://github.com/rhasspy/piper), which is MIT licensed, using two
voices trained by Bryce Beattie and published in the
[piper-voices](https://huggingface.co/rhasspy/piper-voices) collection:

- **kristin (medium)** -- the narrator. A US English female voice trained from
  scratch on the [LJ Speech dataset](https://keithito.com/LJ-Speech-Dataset/),
  which is in the public domain.
- **john (medium)** -- the screen reader. A US English male voice built from
  [LibriVox](https://librivox.org) recordings, which are in the public domain.

Both were chosen because their training data carries no restriction on reuse.
Several better-known piper voices do: the lessac voice comes from the Blizzard
2013 corpus, which permits research use only, and the ryan and hfc voices are
licensed CC BY-NC-SA, which bars commercial use and requires share-alike terms
that would conflict with the MIT licence on this repository.

No acknowledgement is legally required for public domain material. These credits
are here because the people who recorded and trained these voices deserve them.
