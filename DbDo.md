# DbDo User Guide

DbDo is a relational database manager built from the ground up for people who work by keyboard and screen reader. It opens SQLite, Access, Excel, dBASE, and delimited-text files, presents their tables as ordinary Windows lists you can navigate cell by cell, and lets you query, edit, relate, analyze, and report on the data without ever needing a mouse or a glance at the screen.

This guide is organized concepts first. The opening part explains the handful of ideas that everything else rests on: how windows are arranged, what the data list is, the difference between the grid you navigate and the physical table underneath it, how fields are categorized, and the keyboard patterns that recur throughout the program. Once those are clear, the later parts cover working with data, producing output, importing and exporting, SQL and scripting, and a full command reference. If you are new to DbDo, read Part 1 in order. If you are returning for a specific answer, jump to the reference tables near the end.

---

# Part 1 — Key concepts

## What DbDo is

DbDo is a single desktop application that presents **two interfaces over one live connection** to whatever database you open:

- A **graphical interface** of standard Windows menus and lists, driven entirely from the keyboard, where each open table appears in its own window.
- A **dot prompt**, a dBASE-style console where you type commands and short SQL directly, for people who prefer a command line or want to script.

Both interfaces talk to the same open database at the same time, so a change made in one is immediately visible in the other. You can spend all your time in the menus, all your time at the prompt, or move freely between them.

DbDo does not invent its own storage format. It reads and writes real files in their native formats — a SQLite `.db`, an Access `.mdb`/`.accdb`, an Excel workbook, a dBASE `.dbf`, a `.csv` — through the appropriate database driver. Its value is the *interface*, not the container.

## Windows: the multiple-document model

DbDo uses a **multiple-document interface (MDI)**. There is one application frame, and inside it each open recordset — normally a table, sometimes a view or a query result — lives in **its own child window**. Opening a second table does not replace the first; it opens alongside it, and you move between them with window commands.

This matters for a screen-reader user because it keeps each table's state — its position, its filter, its sort, its selected columns — self-contained and announced per window. When you switch windows you switch whole working contexts.

The window commands live in the **Window menu**:

- **Current Windows** (F4) lists every open window; pick one to jump to it.
- **Window Toggle** flips between the current window and the one you were in before it.
- **Next Window** (Ctrl+Tab) and **Previous Window** (Ctrl+Shift+Tab) cycle through them.
- **Say Windows Open** (Shift+F4) speaks how many windows are open and their titles.
- **Close Window** (Ctrl+F4) closes the current one; **Close All But Current Window** (Ctrl+Shift+F4) leaves only the active one. Closing the last window exits DbDo, because the menu bar lives in the window.

There is a deliberate distinction between **opening** a table and **going to** one. **Open Table** (Ctrl+Shift+T) makes a *new window* for a table; **Choose Table** (F7) changes the table shown in the *current* window. Open when you want both tables visible at once; choose when you simply want to switch what this window is looking at.

## The data list

Inside each window the records are shown in a **data list** — a standard Windows list control with a row per record and a column per field. This is the surface you spend most of your time on. Your screen reader reads it the way it reads any Windows list: arrow down to the next row, and the row's contents are announced.

The list is **single-select**: exactly one row is current at a time, and that row is the database cursor's current record (see below). DbDo deliberately does not use multi-select highlighting; instead it has an explicit **marking** system (covered in Part 2) so that "which records are selected for an operation" is a property you set and query on purpose, not a fragile visual highlight.

### Virtual cell navigation

Beyond moving row to row, DbDo lets you move **cell by cell within a row** without leaving the list. A *virtual cursor* tracks which column you are on. Move it left and right across the columns and DbDo speaks the value in the current cell together with its field name, so you always know both *what* the value is and *which field* it belongs to. This is how you inspect a wide record one field at a time by keyboard, and it is what several commands mean when they act on "the current column" or "the cell under the cursor."

## Two vocabularies: schema versus interface

DbDo is careful about words, and the guide follows the same discipline, because the two halves of the program describe the same data with two different vocabularies:

- **Schema vocabulary — Field, Record, Table.** These describe the *data model*: a table is made of records, each record has fields. Use these words when talking about structure, types, and design.
- **Interface vocabulary — Column, Row, Cell, Grid.** These describe *what you navigate*: the grid has rows and columns, and each intersection is a cell. Use these words when talking about moving around and reading.

A field and a column are the same underlying thing seen from two angles; likewise a record and a row, and a cell is one field of one record. One more term: an absent value is **blank**, never "empty" or "null" in the interface.

## The grid versus the physical database

This is the single most important distinction in DbDo.

The **physical database** is the table as stored on disk: every record, every field, in whatever order the file holds them.

The **grid** is a **virtual view** onto that table — specifically, the view produced by three things you control:

1. the **filter** currently in effect (a Where clause limiting which records appear),
2. the **sort** currently in effect (the order the records appear in), and
3. the **displayed columns** you have chosen to show.

The grid is what you see and navigate. It can be a small, reordered slice of a large table. The physical table is the whole thing underneath.

Editing writes **through** the grid to the physical table: when you change a cell, the underlying record is updated on disk. But *reading* commands split into two families, and knowing which is which prevents surprises:

- Commands that act on **the grid** operate on *what you are looking at* — the current filtered, sorted, column-selected view. **Generate from Grid** is the clearest example: it analyzes the virtual grid.
- Commands that act on **the physical table** operate on *all the records*, regardless of the current filter or sort. **Run Report** is the clearest example: it reads the whole physical table so a report is reproducible no matter what you were viewing.

Whenever a command's behavior could depend on the current view, this guide says explicitly whether it acts on the grid or the physical table.

## The database cursor

Underneath the grid is a **cursor**: a pointer to the **current record**. Exactly one record is current at any moment. Almost everything is expressed relative to the cursor:

- **Navigation** moves the cursor — to the next or previous record, the first or last, a record number, or the next record matching a search.
- **Editing** acts on the current record.
- **Filtering and sorting** rebuild the grid and leave the cursor on a sensible record within the new view.

DbDo uses a **client-side cursor**, meaning the working set is held in memory on your side of the connection; this is what makes instant sorting, filtering, and backward navigation possible even on file-based databases.

Edits **auto-commit**. When you finish adding or changing a record, DbDo writes it immediately. There is no separate "save the record" step. (The Save command, described later, is a different thing — it makes a copy of the whole database.)

## How fields are categorized

Every field has a **type** (also called its affinity), and DbDo uses that type to decide how the field is edited, displayed, and validated. On top of the type system sits a small set of **standard fields** that DbDo recognizes by name and treats specially. Understanding both is the key to designing a database that behaves well with a screen reader.

### Field types

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

### Standard fields

DbDo recognizes certain field **names** and maintains or exposes them for you. A table need not have all of them; DbDo simply uses the ones present. They are, by convention, kept out of the way — typically hidden from the default column display and reached through dedicated commands — so the grid stays focused on the data you care about while the housekeeping fields remain one keystroke away.

- **added** (TEXTTIME) — when the record was created. Maintained automatically. Speak it with Say Added (Shift+A).
- **edited** (TEXTTIME) — when the record was last changed. Maintained automatically on every update. Speak it with Say Edited (Shift+E).
- **marked** (BOOLEAN) — whether the record is currently marked for a batch operation. This is what the marking commands set and clear.
- **notes** (TEXTMARKDOWN) — a free-form Markdown note attached to the record. Edit it with Edit Notes (Alt+Shift+N); speak it with Say Notes (Shift+N).
- **tags** (TEXTMEMO) — free-form tags or keywords. Edit with Edit Tags (Alt+Shift+T); speak with Say Tags (Shift+T).
- **url** (TEXTLINE) — an associated link. Edit with Edit URL (Alt+Shift+U); open it with Open URL (Ctrl+Shift+U); speak with Say URL (Shift+U).
- **look** — a derived "display label" for the record: a short human-readable summary DbDo can compute so a record can be referred to by something friendlier than its key. Speak it with Say Look (Shift+L).
- **prm** (the *prime* field) — a record's primary human-facing identity value. Speak it with Say Prime (Shift+P).
- **unq** — a uniqueness/identity helper used internally for stable record identity.

The distinction to hold onto: **added** and **edited** are *automatic* (DbDo writes them for you); **marked** is *state* you toggle; **notes**, **tags**, and **url** are *content* you edit through their own focused dialogs; **look**, **prm**, and **unq** are *derived identity* fields DbDo computes so records can be named and matched reliably.

### Lookups and maps

Two optional companion tables let a database describe its own vocabulary:

- A **lookups** table lists the valid values for a field. When a field has lookups defined, its editor becomes a combo box of those values instead of a free-text box — so a screen-reader user arrows through the legal choices rather than typing and risking a typo. Pick Value (Ctrl+F2) surfaces the same choices on demand.
- A **maps** table is a junction that records relationships between records (many-to-many links), which the relationship-navigation commands can follow.

These are conventions, not requirements; a database without them simply offers free-text editing and no mapped relationships.

## Keyboard patterns

DbDo has three distinct keyboard channels, and recognizing which one you are using clears up most confusion about "why did that key do that."

### 1. Command chords

Every menu command has a name and, usually, a **chord** — a keyboard shortcut shown next to it in the menu. Chords follow a consistent **mnemonic rule**: the letter in the chord is the first letter of a word in the command's name. *Statistics from Column* is Alt+Shift+S; *Generate from Grid* is Alt+Shift+G; *Where Filter* is Ctrl+W. Once you know a command's name you can usually guess its chord, and vice versa. Chords are grouped by modifier so related commands share a shape — the Say-something status commands are mostly Shift+letter, for instance.

### 2. Convenience keys in text fields

When you are *inside a text box* — editing a cell, a note, a filter — a set of **convenience keys** is available in addition to normal typing. These operate on the text under your cursor and are meant to save a screen-reader user from hunting around:

- **Copy / cut the current line** without selecting it first (Ctrl+C / Ctrl+X with no selection), and **append** to the clipboard rather than replacing it (Alt+C / Alt+X).
- **Mark a selection** by setting a start (F8) and completing it (Shift+F8) as two separate keystrokes, rather than holding Shift while arrowing.
- **Copy all** (Ctrl+F8) or **read all** (Alt+F8) of the field's text.
- **Delete the current line** (Ctrl+D), with the next line spoken so you know where you landed.
- **Run at Cursor** (Shift+F5) — take the selection, or the current line if nothing is selected, treat it as a URL, file path, or email address, and open it with your system after a confirmation prompt. Put the cursor on a link in a note and press Shift+F5 to open it in your browser.

These are on by default and can be turned off with the `extraKeys` setting.

### 3. The dot prompt and status queries

The **dot prompt** (Enter Console, Ctrl+GraveAccent) is the command-line channel: type dBASE-style commands or short SQL and press Enter. It is covered in Part 5.

Woven through the menus is a large **Say-X status family** in the Query menu: quick commands that *speak a fact about your current situation* without changing anything — Say Status, Say Database, Say Order, Say Yield (record count), Say Where Filter, Say Position, Say Cell, and many more. These are how you interrogate the screen on demand instead of hunting for information, and they are listed in full in the command reference.

### Key Help

**Key Help Toggle** (Ctrl+F1) turns on a learning mode: while it is on, pressing a command chord **announces** what that command is and does **instead of running it**. Press Ctrl+F1 again to turn it off. This lets you explore the keyboard safely. The **Hotkey Summary** (Alt+Shift+H) and **Alternate Menu** (Alt+F10) give you, respectively, an auditable listing of every command with its key, and a single filterable list of all commands you can search and run.

---

# Part 2 — Working with data

## Opening databases and recordsets

- **Open Database** (Ctrl+O) opens a database file, choosing the right driver from the extension. **New Database** creates a fresh SQLite database; **Add Table** adds a table to it.
- **Open New Recordset** (Ctrl+Shift+T) opens a table (or arbitrary query) in a new window.
- **Recent Files** (Alt+R) reopens something you used lately.
- **Close Database** closes the current database; **Backup Database** writes a copy; **Compare Database** compares two.

When a database has several tables, **Choose Table** (F7) switches the current window to another table, and the *Next/Previous Visited Table* commands walk the tables you have already looked at. **Choose View** (and the object-switch commands) do the same for saved views.

## Navigating records

The cursor moves with ordinary list navigation (arrow keys, Home, End) and with explicit commands:

- **Go to Record** (Ctrl+G) jumps to a record number; **Repeat Go To** (Alt+G) repeats it.
- **Find Record** (Ctrl+F) searches for text; **Reverse Find** (Ctrl+Shift+F) searches backward; **Search Next** (F3) and **Search Previous** (Shift+F3) repeat the last search. **Find Regex** (Ctrl+F3) searches by regular expression.
- **Jump to Record** (Ctrl+J) and **Reverse Jump** move by a jump increment for quickly covering distance in a long table.
- **Jump to Next Initial** steps to the next record whose current column starts with a new letter — type-ahead through a sorted column.

## Editing records

- **New Record** (Ctrl+N) adds a record; **New Copy** (Ctrl+Shift+N) starts a new record pre-filled from the current one; **Copy Record** (Ctrl+Shift+C) copies it to the clipboard.
- **Edit View** (Ctrl+E) opens the whole record in a field-by-field dialog. **Edit Cell** (F2) edits just the current cell. **Open Cell Value** (Ctrl+Enter) opens a large value in its own box for comfortable reading and editing.
- **Pick Value** (Ctrl+F2) offers the legal values for a field that has lookups defined.
- **Edit Notes** (Alt+Shift+N), **Edit Tags** (Alt+Shift+T), and **Edit URL** (Alt+Shift+U) each open one standard field in a focused single-field dialog with OK and Cancel — the quick way to touch a note, tags, or a link without opening the whole record.
- **Replace Column** (Ctrl+R) replaces text across a column; **Regex Replace** (Ctrl+Shift+R) does the same by pattern; **Extract with Regex** (Ctrl+Shift+X) pulls matches out of a column.
- **Delete Record** (Ctrl+D) deletes the current record with confirmation; **Delete Without Confirmation** (Ctrl+Shift+D) skips the prompt.

All edits auto-commit. If a table is read-only, **Read Only Toggle** (Alt+Z) reports and controls that.

## Marking records

Marking is DbDo's explicit, queryable alternative to visual multi-select. The `marked` field carries the state.

- **Mark Record** (Ctrl+M) marks the current record; **Unmark Record** (Ctrl+Shift+M) clears it; **Toggle Marked** (Ctrl+Space) flips it; **Say Mark Status** (Shift+M) speaks it.
- **Mark All** (Ctrl+A), **Unmark All** (Ctrl+Shift+A), and **Invert Marked** (Alt+Shift+I) act across the current view.
- To mark a **range**, set an anchor with **Start Mark** (F8) and extend to the cursor with **Complete Mark** (Shift+F8). **Start Unmark** (Alt+F8) and **Complete Unmark to Anchor** (Alt+Shift+F8) do the same for clearing, with a separate anchor so building a mark range and an unmark range never clobber each other.

Marked records can then be reported, filtered, or exported as a set, and several Say commands report only the marked ones.

## Filtering and sorting

Filtering and sorting reshape the grid without touching the physical table.

- **Where Filter** (Ctrl+W) limits the grid to records matching a condition; **Clear Where** (Ctrl+Shift+W) removes it; **Filter by Regex** filters by pattern; **Say Where Filter** (Shift+W) speaks the current filter.
- **Order Records** (Alt+O) sorts the grid by a field; **Reverse Order** (Alt+Shift+O) reverses it; **Clear Sort** returns to natural order; **Say Order** (Shift+O) speaks the current sort.

Because these only change the view, you can slice a large table down to what you need, work on it, and clear the filter to see everything again — the records were never removed.

## Choosing displayed columns

**Select Columns to Display** (Alt+S) chooses which fields appear in the grid, and in what order. Hiding the standard housekeeping fields and showing only the columns you are working with keeps cell-by-cell navigation short. The choice is part of the grid, so it travels with the window and is remembered.

## Relationships between tables

DbDo can follow relationships defined by foreign keys (and by the maps junction):

- **Enter Child Table** (Alt+Right) follows a relationship from the current record into the related records in another table, opening them filtered to that parent.
- **Exit Child Table** (Alt+Left) returns to the parent you came from; **Exit to Root Table** (Alt+Home) returns all the way up a chain.
- **Related Records** (Query menu) shows the records related to the current one.

This is how you walk a normalized database — from an order to its line items, from a person to their contacts — entirely by keyboard, with each step announced.

---

# Part 3 — Producing output

DbDo offers several ways to turn data into something you can read, share, or file. They differ in **what they act on** and **what they produce**, and it is worth learning the distinction so you reach for the right one. There are two single-column tools, one grid tool, one physical-table tool, and the scripting facility.

## Statistics from Column (Alt+Shift+S)

Acts on **one column** — the column under the virtual cursor. It computes type-aware descriptive statistics: for a numeric column, things like count, populated percentage, minimum, maximum, mean, median, standard deviation, quartiles and interquartile range, outlier fences, and mode; for dates, a temporal summary; for text, the most frequent values. It speaks and shows the result. Use it to understand a single field at a glance. Its name emphasizes that it works *from a column*.

## Graphics Column (Ctrl+Shift+G)

Also acts on **one column** — the column under the virtual cursor — but produces a **chart** of that column (via Excel, out of process) rather than a table of numbers. Use it when you want a quick visual of one field's distribution.

## Generate from Grid (Alt+Shift+G)

Acts on the **virtual grid** — the current filtered, sorted, column-selected view — and is the ad-hoc, exploratory tool. It profiles the grid's columns by type and offers a menu of outputs:

- a **summary** across all columns,
- a **frequency table** of one column,
- a **cross-tab** of two columns,
- a **Markdown table** saved to a file, and
- **Excel charts** — bar, pareto, pie, histogram, box-and-whisker, scatter (two numeric columns), or timeline — generated out of process so they work regardless of your Office bitness.

The point of the name is that it operates **on the grid, not the physical database**: it analyzes *what you are looking at*. Filter and sort first to shape the grid, then Generate from Grid to analyze that exact view. It is one-off and exploratory; nothing is saved except the specific output file you ask for.

## Run Report (Alt+Shift+R)

Acts on the **physical table** and produces a **defined, reproducible document**. Where Generate from Grid analyzes whatever you happen to be viewing, Run Report renders a **report definition** — a saved template — against *all* the records in the table (optionally with its own filter and sort baked into the definition), so the same report comes out the same way every time no matter what your grid was showing.

Report definitions live in a **report.inix** file beside the database. Each definition is one `[section]` in that file, describing which table to read, how to group and order it, and a set of **bands** — header, detail, separators, footers, and group headers/footers — that lay out the text. Within a band, `$field` (or `${field}`) inserts a field's value, `{{ ... }}` evaluates a JScript expression, `{# ... #}` is a comment, and footer bands can compute aggregates like `$count`, `$sum_<field>`, `$avg_<field>`, `$min_<field>`, and `$max_<field>`. DbDo discovers the report files beside the open database, offers a pick-list of the definitions it finds, and writes the result as Markdown you can convert onward to HTML, DOCX, or PDF. The report-template language is detailed in Part 6.

## Snippets and scripts

The scripting facility is for **reusable automation** rather than one-off output. A snippet is a small script file kept in the Scripts folder (and, for a database's own automation, beside the `.db` file). DbDo recognizes three snippet types by extension:

- **`.js`** — **JScript .NET**. The general automation language: it can read and write fields, walk records, build strings, and call into DbDo. This is the same engine behind Evaluate Expression and the `{{ ... }}` substitutions in reports.
- **`.sql`** — a **SQL batch**. One or more SQL statements run against the open database.
- **`.dbdo`** — a **DbDo command batch**: a sequence of the same commands you would type at the dot prompt.

Run a snippet with **Invoke Script** (Alt+V); create or edit one with **Edit Snippet** (Alt+Shift+V), which offers the existing files plus a "new snippet" entry; open the folder with **Open Script Folder**. Use snippets when you find yourself doing the same multi-step task repeatedly.

## Which one do I want?

| Tool | Acts on | Produces | Reusable? |
|------|---------|----------|-----------|
| Statistics from Column | one column | descriptive statistics (spoken/shown) | one-off |
| Graphics Column | one column | a chart | one-off |
| Generate from Grid | the virtual grid (current view) | summary, frequency, cross-tab, Markdown, or chart | one-off |
| Run Report | the physical table | a formatted document from a saved definition | reproducible |
| Snippet / script | whatever the script does | anything (automation) | reusable |

The mental shortcut: **Column** tools look at one field; **Generate from Grid** analyzes what you *see*; **Run Report** produces a defined document from what is *there*; **snippets** automate what you *do*.

---

# Part 4 — Importing, exporting, opening, saving

DbDo distinguishes four file operations that are easy to confuse. The distinction is about whether the data keeps its original shape or is reshaped into DbDo's conventions.

- **Open** (Ctrl+O) opens a file *faithfully* — a round trip. An Excel workbook opens as its sheets, a CSV as its rows, and saving writes back to that same file in that same format. Use Open when you want to work with a file as it is.
- **Import** (Alt+I) reads an outside file and reshapes it *into a DbDo database* — normalizing columns, applying conventions, and producing a `.db` you then work with. Use Import when you want to bring foreign data into a proper DbDo database. **Transfer Import** and **Merge Data** (Alt+M) bring data in alongside existing data.
- **Export Data** (Alt+X) writes the **grid** — the current view — out to a new file in a chosen format. Use Export to hand off what you are looking at.
- **Save** (Ctrl+S) and **Save As** (Ctrl+Shift+S) concern the *whole database*. For a workbook opened faithfully, Save writes your edits back to the `.xlsx`. For a native database your edits were already committed, so Save reports that; Save As writes a fresh copy. **Open as Managed Copy** opens a file as a working copy so the original is untouched until you decide.

Supported formats include SQLite (`.db`, `.sqlite`, `.sqlite3`), Access (`.mdb`, `.accdb`), Excel (`.xlsx`, `.xls`), dBASE (`.dbf`), and delimited text (`.csv`, `.tsv`, `.tab`, `.txt`). The right driver is chosen from the extension.

---

# Part 5 — SQL and scripting

## Running SQL

- **Query** (Ctrl+Q) opens a box where you type SQL and run it; the results open as a recordset you can navigate like any table. **Query History** (Alt+Shift+Q) recalls previous queries.
- The **dot prompt** (Enter Console, Ctrl+GraveAccent) accepts both dBASE-style dot commands and short SQL inline. It is the fastest path for people who think in commands.
- **Sqlean Console** (Ctrl+Shift+GraveAccent) opens a console with the SQLean extension functions available (see Part 6).

DbDo runs your SQL against the live connection, so queries see uncommitted context the way the rest of the program does. Because the cursor is client-side, result sets are fully navigable — forward, backward, and by position.

## The scripting engine

DbDo's scripting language is **JScript .NET**. The same engine powers three things, so learning it once pays off in all of them: **Invoke Script** for `.js` snippets, **Evaluate Expression** (Ctrl+Equals) for one-off calculations, and the `{{ ... }}` expressions inside report templates.

- **Evaluate Expression** (Ctrl+Equals) prompts for an expression, evaluates it, and speaks and shows the result — `2+2*10`, string manipulation, date math. The result is shown rather than only copied, so it stays reachable even where clipboard access is restricted. The last expression is remembered for quick tweaking.
- In scripts and report expressions, field values are available for substitution and computation, so an expression can combine, format, or conditionally choose field values.

Snippet types and how to run them are covered in Part 3.

---

# Part 6 — Reference

## Command reference by menu

Chords use screen-reader-canonical key names. A command with no chord is reachable through its menu or through Alternate Menu (Alt+F10).

### File menu

| Command | Chord |
|---------|-------|
| New Database | — |
| Add Table | — |
| Open Database | Ctrl+O |
| Open New Recordset | Ctrl+Shift+T |
| Recent Files | Alt+R |
| Save | Ctrl+S |
| Save As | Ctrl+Shift+S |
| Close Database | — |
| Backup Database | — |
| Compare Database | — |
| Import | Alt+I |
| Merge Data | Alt+M |
| Transfer Import | — |
| Run Report | Alt+Shift+R |
| Export Data | Alt+X |
| Print | Ctrl+P |
| Choose Table | F7 |
| Choose View | — |
| Next / Previous Visited Table | Alt+F6 / Alt+Shift+F6 |
| Next / Previous Table or View | Ctrl+F6 / Ctrl+Shift+F6 |
| Exit DbDo | Alt+F4 |

### Edit menu

| Command | Chord |
|---------|-------|
| New Record | Ctrl+N |
| Edit View | Ctrl+E |
| Edit Cell | F2 |
| Delete Record | Ctrl+D |
| Delete Without Confirmation | Ctrl+Shift+D |
| Copy Record | Ctrl+Shift+C |
| Append Record to Clipboard | Alt+Shift+C |
| New Copy | Ctrl+Shift+N |
| Mail Record | — |
| Replace Column | Ctrl+R |
| Regex Replace | Ctrl+Shift+R |
| Mark Record | Ctrl+M |
| Say Mark Status | Shift+M |
| Toggle Marked | Ctrl+Space |
| Edit Notes | Alt+Shift+N |
| Edit Tags | Alt+Shift+T |
| Edit URL | Alt+Shift+U |
| Unmark Record | Ctrl+Shift+M |
| Mark All | Ctrl+A |
| Unmark All | Ctrl+Shift+A |
| Invert Marked | Alt+Shift+I |
| Start Mark / Complete Mark | F8 / Shift+F8 |
| Start Unmark / Complete Unmark to Anchor | Alt+F8 / Alt+Shift+F8 |
| Save Bookmark | Ctrl+B |
| List Bookmarks | Alt+B |
| Clear Bookmark | Ctrl+Shift+B |
| Open Cell Value | Ctrl+Enter |
| Open URL | Ctrl+Shift+U |
| Pick Value | Ctrl+F2 |

### Navigate menu

| Command | Chord |
|---------|-------|
| First / Last Record | — |
| Next / Previous Record | — |
| Go to Record | Ctrl+G |
| Repeat Go To | Alt+G |
| Find Record | Ctrl+F |
| Reverse Find | Ctrl+Shift+F |
| Jump to Record | Ctrl+J |
| Reverse Jump | Ctrl+Shift+J |
| Find Regex | Ctrl+F3 |
| Reverse Regex Find | Ctrl+Shift+F3 |
| Search Next / Previous | F3 / Shift+F3 |
| Enter Child Table | Alt+Right |
| Exit Child Table | Alt+Left |
| Exit to Root Table | Alt+Home |

### Query menu

The Query menu holds record inspection and the Say-X status family.

| Command | Chord |
|---------|-------|
| Inspect Record | Ctrl+I |
| Table Properties | Alt+Enter |
| Related Records | — |
| Show Schema | — |
| Say Status | Shift+Z |
| Say Database | Shift+D |
| Say Order | Shift+O |
| Say Goto | Shift+G |
| Say Yield | Shift+Y |
| Say Tables | Shift+F7 |
| Say Marked | — |
| Say Edited | Shift+E |
| Say Notes | Shift+N |
| Say Tags | Shift+T |
| Say Column Rest | Ctrl+L |
| Say Column Rest Marked | Ctrl+Shift+L |
| Say Records Rest | Alt+L |
| Say Marked Rows | Shift+Space |
| Say Sort and Filter | Shift+8 |
| Say Position | Alt+Delete |
| Say Clipboard | Alt+Apostrophe |
| Say Added | Shift+A |
| Say Cell | Shift+C |
| Say Where Filter | Shift+W |
| Say Find | Shift+F |
| Say Select Columns | Shift+S |
| Say Query | Shift+Q |
| Say Id | Shift+I |
| Say Look | Shift+L |
| Say Related | Shift+R |
| Say URL | Shift+U |
| Say Prime | Shift+P |
| Where Filter | Ctrl+W |
| Clear Where | Ctrl+Shift+W |
| Filter by Regex | — |
| Clear Sort | — |
| Order Records | Alt+O |
| Reverse Order | Alt+Shift+O |

### Misc menu

| Command | Chord |
|---------|-------|
| Refresh View | F5 |
| Read Only Toggle | Alt+Z |
| Database Summary | Alt+D |
| Table Summary | Alt+T |
| Statistics from Column | Alt+Shift+S |
| Select Columns to Display | Alt+S |
| Graphics Column | Ctrl+Shift+G |
| Generate from Grid | Alt+Shift+G |
| Extract with Regex | Ctrl+Shift+X |
| Append Cell to Clipboard | Alt+C |
| Copy Cell to Clipboard | Ctrl+C |
| Copy Visible Cells as TSV | — |
| Copy Column / Copy Grid | — |
| Jump to Next Initial | — |
| Query | Ctrl+Q |
| Query History | Alt+Shift+Q |
| Test Integrity | — |
| Test Drivers | — |
| Hotkey Summary | Alt+Shift+H |
| Open as Managed Copy | — |
| Describe Table | — |
| Facet Column | — |
| Open in Explorer | Alt+Pipe |
| Open Command Prompt | Ctrl+Slash |
| Open Dot Prompt | Ctrl+GraveAccent |
| Sqlean Console | Ctrl+Shift+GraveAccent |
| Invoke Script | Alt+V |
| Edit Snippet | Alt+Shift+V |
| Open Script Folder | — |
| Evaluate Expression | Ctrl+Equals |
| Edit Settings | Alt+Shift+C |

### Window menu

| Command | Chord |
|---------|-------|
| Open Table | Ctrl+Shift+T |
| Current Windows | F4 |
| Window Toggle | — |
| Next Window | Ctrl+Tab |
| Previous Window | Ctrl+Shift+Tab |
| Say Windows Open | Shift+F4 |
| Close Window | Ctrl+F4 |
| Close All But Current Window | Ctrl+Shift+F4 |

### Help menu

| Command | Chord |
|---------|-------|
| Documentation | F1 |
| History of Changes | Shift+F1 |
| Readme Guide | — |
| Sample Databases | — |
| Alternate Menu | Alt+F10 |
| Key Help Toggle | Ctrl+F1 |
| Where Am I | — |
| Test Screen Reader Speech | — |
| Email Log File | — |
| Elevate Version | F11 |

## Standard fields (summary)

| Field | Type | Role | Speak with |
|-------|------|------|-----------|
| added | TEXTTIME | creation time, automatic | Shift+A |
| edited | TEXTTIME | last-change time, automatic | Shift+E |
| marked | BOOLEAN | batch-operation state you toggle | Shift+M |
| notes | TEXTMARKDOWN | free-form Markdown note | Shift+N |
| tags | TEXTMEMO | keywords | Shift+T |
| url | TEXTLINE | associated link | Shift+U |
| look | derived | display label for the record | Shift+L |
| prm | derived | prime human-facing identity | Shift+P |
| unq | derived | internal uniqueness helper | — |

## Field types (summary)

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

## The report-template language

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

## The .inix configuration format

DbDo's own settings and several data files use **.inix**, an INI-style format: `[Section]` headers followed by `name = value` lines, with `;` or `#` beginning a comment and case-insensitive key names written in UpperCamelCase (usually two words). Global defaults live in **DbDo.inix** beside the program; a database may carry its own **`<DbName>.inix`** beside the database file for per-database overrides. Notable sections include `[General]` and `[Options]` for behavior toggles (such as `ExtraSpeech`, `CommandEcho`, and the `ExtraKeys` convenience-key switch), `[Validation]` for per-field regex patterns, `[ConnectStrings]` for per-extension connection-string overrides, and `[Hotkeys]` which documents and can re-describe command chords. **Edit Settings** (Alt+Shift+C) exposes the common options in a dialog and can open the file directly for advanced editing.

Values are usually a single line, but a key may hold a **multi-line value** — a fenced block opened and closed by a line containing only `` ` `` (or a triple-quote fence, used when the content itself contains a `` ` ``), taken verbatim. A key may also hold an **array of values**. A few short items with no spaces or commas can sit inline, comma-separated (`SelectFields = last_name, first_name, enterprise`); otherwise each item goes on its own line inside a fence, most recent first. Either shape reads back as the same ordered list.

A `<DbName>.inix` holds the settings that belong to one database. Its `[Database]` section carries **InitialTable** (the table opened in the first window when the database opens, so there is always a starting window — other tables open via Open Table, Control+Shift+O) and the recent-input arrays **FindText**, **JumpText**, **ReplaceText**, **ReplaceRegex**, and **QueryText**, each up to ten items, most recent first, which populate the matching command's combo box. A `[Table:<name>]` section carries that table's saved view — **SelectFields**, **OrderFields**, and **WhereFilter** — recalled when the table is next opened.

## SQLean extensions

When the SQLean extension functions are available, they add a large library of SQL functions — string and text helpers, math, statistics, fuzzy matching, regular expressions, and more — usable in queries and reachable through the **Sqlean Console** (Ctrl+Shift+GraveAccent). They extend what your `Query` and report expressions can compute without leaving SQL.

## Sample databases

DbDo ships with sample databases that follow the same column conventions as your own data, reachable from **Sample Databases** in the Help menu. They are the fastest way to see the standard fields, lookups, relationships, and reports working together on real records.

## Screen-reader settings

DbDo speaks directly to supplement your screen reader's own announcements. **Extra Speech** (toggled on Alt+Shift+Z) controls DbDo's additional commentary without affecting your screen reader's natural focus and selection announcements; turning it off leaves only the screen reader's own speech. **Command Echo** controls whether commands announce a confirmation as they run. **Test Screen Reader Speech** verifies that DbDo can reach your screen reader.

## Persistence and logging

DbDo remembers per-database state in the database's own `<DbName>.inix` — the last table, sort, filter, and displayed columns, plus a pinned **InitialTable** and the recent Find, Jump, Replace, Regex, and Query inputs — so reopening a database returns you to where you were. Activity is written to **DbDo.log**; **Email Log File** in the Help menu reveals the log and starts an email with its path, for reporting issues.

---

# Part 7 — Development

This part is for people building DbDo from source.

## Requirements

- .NET Framework 4.8, 64-bit.
- The build tools invoked by `buildDbDo.cmd` (the .NET Framework C# compiler and Inno Setup for packaging).
- The SQLite ODBC driver and the bundled dependency DLLs the build fetches.

## Build

Compile with **`buildDbDo.cmd`**, which builds `DbDo.exe`, converts this guide and the README to HTML with pandoc, and packages the installer with Inno Setup (`DbDo_setup.iss`). The dependency DLLs are gathered by `getDbDoDeps.ps1`. To replicate the development layout, keep the program and its subfolders under `C:\DbDo`.

## Architecture: one connection, two interfaces

DbDo maintains a single live database connection and presents it through both the graphical interface and the dot prompt, as described in Part 1. Each MDI child window owns its own manager and cursor, so tables are independent, while the connection and drivers are shared. The cursor is client-side, which is what makes instant sort, filter, and backward navigation possible on file-based databases.

## Coding style

DbDo is written in the "Camel Type" style. The full, authoritative description is a separate distributed file, **`Camel_Type_CSharp.md`**; consult it for specifics rather than duplicating them here. In brief, it uses Hungarian-style prefixes, lower-camel method names, alphabetized declarations, double-quoted strings, and for-each iteration; database identifiers use lower_snake_case.

## Layout by Code

DbDo builds its dialogs and menus **in code** rather than with a visual designer, so that every control is a standard, screen-reader-friendly Windows control created and labeled explicitly. The label-based-controls helpers assemble a dialog from labels, input boxes, memo boxes, list boxes, and pick boxes with OK/Cancel or custom buttons, keeping tab order and accessible names correct by construction.

## File layout

The source is a single large `DbDo.cs`. Alongside it live the build script, the installer script, the dependency fetcher, the `.inix` configuration, this guide (`DbDo.md`), the README, the coding-style file, and the sample databases with their scripts and `report.inix` definitions.
