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
- **Next Window** (Control+Tab) and **Previous Window** (Control+Shift+Tab) cycle through them.
- **Say Windows Open** (Shift+F4) speaks how many windows are open and their titles.
- **Close Window** (Control+F4) closes the current one; **Close All But Current Window** (Control+Shift+F4) leaves only the active one. Closing the last window exits DbDo, because the menu bar lives in the window.

There is a deliberate distinction between **opening** a table and **going to** one. **Open Table in New Window** (Control+Shift+T) makes a *new window* for a table; **Choose Table** (Control+T) changes the table shown in the *current* window. Open when you want both tables visible at once; choose when you simply want to switch what this window is looking at.

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
- **url** (TEXTLINE) — an associated link. Edit with Edit URL (Alt+Shift+U); open it with Open URL (Control+Shift+U); speak with Say URL (Shift+U).
- **look** — a derived "display label" for the record: a short human-readable summary DbDo can compute so a record can be referred to by something friendlier than its key. Speak it with Say Look (Shift+L).
- **prime** (the *prime* field) — a record's primary human-facing identity value. Speak it with Say Prime (Shift+P).
- **unq** — a uniqueness/identity helper used internally for stable record identity.

The distinction to hold onto: **added** and **edited** are *automatic* (DbDo writes them for you); **marked** is *state* you toggle; **notes**, **tags**, and **url** are *content* you edit through their own focused dialogs; **look**, **prime**, and **unq** are *derived identity* fields DbDo computes so records can be named and matched reliably.

### Lookups and maps

Two optional companion tables let a database describe its own vocabulary:

- A **lookups** table lists the valid values for a field. When a field has lookups defined, its editor becomes a combo box of those values instead of a free-text box — so a screen-reader user arrows through the legal choices rather than typing and risking a typo. Pick Value (Control+F2) surfaces the same choices on demand.
- A **maps** table is a junction that records relationships between records (many-to-many links), which the relationship-navigation commands can follow.

These are conventions, not requirements; a database without them simply offers free-text editing and no mapped relationships.

## Keyboard patterns

DbDo has three distinct keyboard channels, and recognizing which one you are using clears up most confusion about "why did that key do that."

### 1. Command chords

Every menu command has a name and, usually, a **chord** — a keyboard shortcut shown next to it in the menu. Chords follow a consistent **mnemonic rule**: the letter in the chord is the first letter of a word in the command's name. *Statistics from Column* is Alt+Shift+S; *Generate from Grid* is Alt+Shift+G; *Where Filter* is Control+W. Once you know a command's name you can usually guess its chord, and vice versa. Chords are grouped by modifier so related commands share a shape — the Say-something status commands are mostly Shift+letter, for instance.

### 2. Convenience keys in text fields

When you are *inside a text box* — editing a cell, a note, a filter — a set of **convenience keys** is available in addition to normal typing. These operate on the text under your cursor and are meant to save a screen-reader user from hunting around:

- **Copy / cut the current line** without selecting it first (Control+C / Control+X with no selection), and **append** to the clipboard rather than replacing it (Alt+C / Alt+X).
- **Mark a selection** by setting a start (F8) and completing it (Shift+F8) as two separate keystrokes, rather than holding Shift while arrowing.
- **Copy all** (Control+F8) or **read all** (Alt+F8) of the field's text.
- **Delete the current line** (Control+D), with the next line spoken so you know where you landed.
- **Run at Cursor** (Shift+F5) — take the selection, or the current line if nothing is selected, treat it as a URL, file path, or email address, and open it with your system after a confirmation prompt. Put the cursor on a link in a note and press Shift+F5 to open it in your browser.

These are on by default and can be turned off with the `extraKeys` setting.

### 3. The dot prompt and status queries

The **dot prompt** (Enter Console, Control+GraveAccent) is the command-line channel: type dBASE-style commands or short SQL and press Enter. It is covered in Part 5.

Woven through the menus is a large **Say-X status family** in the Query menu: quick commands that *speak a fact about your current situation* without changing anything — Say Here, Say Database, Say Order, Say Yield (record count), Say Where Filter, Say Position, Say Cell, and many more. These are how you interrogate the screen on demand instead of hunting for information, and they are listed in full in the command reference.

### Key Help

**Key Help Toggle** (Control+F1) turns on a learning mode: while it is on, pressing a command chord **announces** what that command is and does **instead of running it**. Press Control+F1 again to turn it off. This lets you explore the keyboard safely. The **Hotkey Summary** (Alt+Shift+H) and **Alternate Menu** (Alt+F10) give you, respectively, an auditable listing of every command with its key, and a single filterable list of all commands you can search and run.

---

# Part 2 — Working with data

## Opening databases and recordsets

- **Open Database** (Control+O) opens a database file, choosing the right driver from the extension. **New Database** creates a fresh SQLite database; **Add Table** adds a table to it.
- **Open Table in New Window** (Control+Shift+T) opens a table (or arbitrary query) in a new window.
- **Recent Files** (Alt+R) reopens something you used lately.
- **Close Database** closes the current database; **Backup Database** writes a copy; **Compare Database** compares two.

When a database has several tables, **Choose Table** (Control+T) switches the current window to another table, and the *Next/Previous Visited Table* commands walk the tables you have already looked at. **Choose View** (and the object-switch commands) do the same for saved views.

## Navigating records

The cursor moves with ordinary list navigation (arrow keys, Home, End) and with explicit commands:

- **Go to Record** (Control+G) jumps to a record number; **Repeat Go To** (Alt+G) repeats it.
- **Find Record** (Control+F) searches for text; **Reverse Find** (Control+Shift+F) searches backward; **Search Next** (F3) and **Search Previous** (Shift+F3) repeat the last search. **Find Regex** (Control+F3) searches by regular expression.
- **Jump to Record** (Control+J) and **Reverse Jump** move by a jump increment for quickly covering distance in a long table.
- **Jump to Next Initial** steps to the next record whose current column starts with a new letter — type-ahead through a sorted column.

## Editing records

- **New Record** (Control+N) adds a record; **New Copy** (Control+Shift+N) starts a new record pre-filled from the current one; **Copy Record** (Control+Shift+C) copies it to the clipboard.
- **Edit View** (Control+E) opens the whole record in a field-by-field dialog. **Edit Cell** (F2) edits just the current cell. **Open Cell Value** (Control+Enter) opens a large value in its own box for comfortable reading and editing.
- **Pick Value** (Control+F2) offers the legal values for a field that has lookups defined.
- **Edit Notes** (Alt+Shift+N), **Edit Tags** (Alt+Shift+T), and **Edit URL** (Alt+Shift+U) each open one standard field in a focused single-field dialog with OK and Cancel — the quick way to touch a note, tags, or a link without opening the whole record.
- **Replace Column** (Control+R) replaces text across a column; **Regex Replace** (Control+Shift+R) does the same by pattern; **Extract with Regex** (Control+Shift+X) pulls matches out of a column.
- **Delete Record** (Control+D) deletes the current record with confirmation; **Delete Without Confirmation** (Control+Shift+D) skips the prompt.

All edits auto-commit. If a table is read-only, **Read Only Toggle** (Alt+Z) reports and controls that.

## Marking records

Marking is DbDo's explicit, queryable alternative to visual multi-select. The `marked` field carries the state.

- **Mark Record** (Control+M) marks the current record; **Unmark Record** (Control+Shift+M) clears it; **Toggle Marked** (Control+Space) flips it; **Say Mark Status** (Shift+M) speaks it.
- **Mark All** (Control+A), **Unmark All** (Control+Shift+A), and **Invert Marked** (Alt+Shift+I) act across the current view.
- To mark a **range**, set an anchor with **Start Mark** (F8) and extend to the cursor with **Complete Mark** (Shift+F8). **Start Unmark** (Alt+F8) and **Complete Unmark to Anchor** (Alt+Shift+F8) do the same for clearing, with a separate anchor so building a mark range and an unmark range never clobber each other.

Marked records can then be reported, filtered, or exported as a set, and several Say commands report only the marked ones.

## Filtering and sorting

Filtering and sorting reshape the grid without touching the physical table.

- **Where Filter** (Control+W) limits the grid to records matching a condition; **Clear Where** (Control+Shift+W) removes it; **Filter by Regex** filters by pattern; **Say Where Filter** (Shift+W) speaks the current filter.
- **Order Records** (Alt+O) sorts the grid by a field; **Reverse Order** (Alt+Shift+O) reverses it; **Clear Sort** returns to natural order; **Say Order** (Shift+O) speaks the current sort.

Sorting reads numbers as numbers: volume 9 comes after 7 and 6.5 even if some
values were stored as text and others as numbers. It also ignores capital
letters, so "spencer" sorts with "Spencer".

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

## Graphics Column (Control+Shift+G)

Also acts on **one column** — the column under the virtual cursor — but produces a **chart** of that column (via Excel, out of process -- the one export that still needs Office) rather than a table of numbers. Use it when you want a quick visual of one field's distribution.

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

- **Open** (Control+O) opens a file *faithfully* — a round trip. An Excel workbook opens as its sheets, a CSV as its rows, and saving writes back to that same file in that same format. Use Open when you want to work with a file as it is.
- **Import** (Alt+I) reads an outside file and reshapes it *into a DbDo database* — normalizing columns, applying conventions, and producing a `.db` you then work with. Use Import when you want to bring foreign data into a proper DbDo database. **Transfer Import** and **Merge Data** (Alt+M) bring data in alongside existing data.
- **Export Data** (Alt+X) writes the **grid** — the current view — out to a new file in a chosen format. Use Export to hand off what you are looking at.
- **Save** (Control+S) and **Save As** (Control+Shift+S) concern the *whole database*. For a workbook opened faithfully, Save writes your edits back to the `.xlsx`. For a native database your edits were already committed, so Save reports that; Save As writes a fresh copy. **Open as Managed Copy** opens a file as a working copy so the original is untouched until you decide.

Supported formats include SQLite (`.db`, `.sqlite`, `.sqlite3`), Access (`.mdb`, `.accdb`), Excel (`.xlsx`, `.xls`), dBASE (`.dbf`), and delimited text (`.csv`, `.tsv`, `.tab`, `.txt`). The right driver is chosen from the extension.

---

# Part 5 — SQL and scripting

## Running SQL

- **Query** (Control+Q) opens a box where you type SQL and run it; the results open as a recordset you can navigate like any table. **Query History** (Alt+Shift+Q) recalls previous queries.
- The **dot prompt** (Enter Console, Control+GraveAccent) accepts both dBASE-style dot commands and short SQL inline. It is the fastest path for people who think in commands.
- **Sqlean Console** (Control+Shift+GraveAccent) opens a console with the SQLean extension functions available (see Part 6).

DbDo runs your SQL against the live connection, so queries see uncommitted context the way the rest of the program does. Because the cursor is client-side, result sets are fully navigable — forward, backward, and by position.

## The scripting engine

DbDo's scripting language is **JScript .NET**. The same engine powers three things, so learning it once pays off in all of them: **Invoke Script** for `.js` snippets, **Evaluate Expression** (Control+Equals) for one-off calculations, and the `{{ ... }}` expressions inside report templates.

- **Evaluate Expression** (Control+Equals) prompts for an expression, evaluates it, and speaks and shows the result — `2+2*10`, string manipulation, date math. The result is shown rather than only copied, so it stays reachable even where clipboard access is restricted. The last expression is remembered for quick tweaking.
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
| Open Database | Control+O |
| Open Table in New Window | Control+Shift+T |
| Recent Files | Alt+R |
| Save | Control+S |
| Save As | Control+Shift+S |
| Close Database | — |
| Backup Database | — |
| Compare Database | — |
| Import | Alt+I |
| Merge Data | Alt+M |
| Transfer Import | — |
| Run Report | Alt+Shift+R |
| Export Data | Alt+X |
| Print | Control+P |
| Choose Table | Control+T |
| Choose View | — |
| Next / Previous Visited Table | Alt+F6 / Alt+Shift+F6 |
| Next / Previous Table or View | Control+F6 / Control+Shift+F6 |
| Exit DbDo | Alt+F4 |

### Edit menu

| Command | Chord |
|---------|-------|
| New Record | Control+N |
| Edit View | Control+E |
| Edit Cell | F2 |
| Delete Record | Control+D |
| Delete Without Confirmation | Control+Shift+D |
| Copy Record | Control+Shift+C |
| Append Record to Clipboard | Alt+Shift+C |
| New Copy | Control+Shift+N |
| Mail Record | — |
| Replace Column | Control+R |
| Regex Replace | Control+Shift+R |
| Mark Record | Control+M |
| Say Mark Status | Shift+M |
| Toggle Marked | Control+Space |
| Edit Notes | Alt+Shift+N |
| Edit Tags | Alt+Shift+T |
| Edit URL | Alt+Shift+U |
| Unmark Record | Control+Shift+M |
| Mark All | Control+A |
| Unmark All | Control+Shift+A |
| Invert Marked | Alt+Shift+I |
| Start Mark / Complete Mark | F8 / Shift+F8 |
| Start Unmark / Complete Unmark to Anchor | Alt+F8 / Alt+Shift+F8 |
| Save Bookmark | Control+B |
| List Bookmarks | Alt+B |
| Clear Bookmark | Control+Shift+B |
| Open Cell Value | Control+Enter |
| Open URL | Control+Shift+U |
| Pick Value | Control+F2 |

### Navigate menu

| Command | Chord |
|---------|-------|
| First / Last Record | — |
| Next / Previous Record | — |
| Go to Record | Control+G |
| Repeat Go To | Alt+G |
| Find Record | Control+F |
| Reverse Find | Control+Shift+F |
| Jump to Record | Control+J |
| Reverse Jump | Control+Shift+J |
| Find Regex | Control+F3 |
| Reverse Regex Find | Control+Shift+F3 |
| Search Next / Previous | F3 / Shift+F3 |
| Enter Child Table | Alt+Right |
| Exit Child Table | Alt+Left |
| Exit to Root Table | Alt+Home |

### Query menu

The Query menu holds record inspection and the Say-X status family.

| Command | Chord |
|---------|-------|
| Inspect Record | Control+I |
| Table Properties | Alt+Enter |
| Related Records | — |
| Show Schema | — |
| Say Here | Shift+H |
| Say Database | Shift+D |
| Say Order | Shift+O |
| Say Goto | Shift+G |
| Say Yield | Shift+Y |
| Say Marked | — |
| Say Edited | Shift+E |
| Say Notes | Shift+N |
| Say Tags | Shift+T |
| Say Column Rest | Control+L |
| Say Column Rest Marked | Control+Shift+L |
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
| Where Filter | Control+W |
| Clear Where | Control+Shift+W |
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
| Graphics Column | Control+Shift+G |
| Generate from Grid | Alt+Shift+G |
| Extract with Regex | Control+Shift+X |
| Append Cell to Clipboard | Alt+C |
| Copy Cell to Clipboard | Control+C |
| Copy Visible Cells as TSV | — |
| Copy Column / Copy Grid | — |
| Jump to Next Initial | — |
| Query | Control+Q |
| Query History | Alt+Shift+Q |
| Test Integrity | — |
| Test Drivers | — |
| Hotkey Summary | Alt+Shift+H |
| Open as Managed Copy | — |
| Describe Table | — |
| Facet Column | — |
| Open in Explorer | Alt+Pipe |
| Open Command Prompt | Control+Slash |
| Open Dot Prompt | Control+GraveAccent |
| Sqlean Console | Control+Shift+GraveAccent |
| Invoke Script | Alt+V |
| Edit Snippet | Alt+Shift+V |
| Open Script Folder | — |
| Evaluate Expression | Control+Equals |
| Edit Settings | Alt+Shift+C |

### Window menu

| Command | Chord |
|---------|-------|
| Open Table | Control+Shift+T |
| Current Windows | F4 |
| Window Toggle | — |
| Next Window | Control+Tab |
| Previous Window | Control+Shift+Tab |
| Say Windows Open | Shift+F4 |
| Close Window | Control+F4 |
| Close All But Current Window | Control+Shift+F4 |

### Help menu

| Command | Chord |
|---------|-------|
| Documentation | F1 |
| History of Changes | Shift+F1 |
| Readme Guide | — |
| Sample Databases | — |
| Alternate Menu | Alt+F10 |
| Key Help Toggle | Control+F1 |
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
| prime | derived | prime human-facing identity | Shift+P |
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

When the SQLean extension functions are available, they add a large library of SQL functions — string and text helpers, math, statistics, fuzzy matching, regular expressions, and more — usable in queries and reachable through the **Sqlean Console** (Control+Shift+GraveAccent). They extend what your `Query` and report expressions can compute without leaving SQL.

## Sample databases

DbDo ships with sample databases that follow the same column conventions as your own data, reachable from **Sample Databases** in the Help menu. They are the fastest way to see the standard fields, lookups, relationships, and reports working together on real records.

## Screen-reader settings

DbDo speaks directly to supplement your screen reader's own announcements. **Extra Speech** (toggled from the Help menu) controls DbDo's additional commentary without affecting your screen reader's natural focus and selection announcements; turning it off leaves only the screen reader's own speech. **Command Echo** controls whether commands announce a confirmation as they run. **Test Screen Reader Speech** verifies that DbDo can reach your screen reader.

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




## Nothing, and the two kinds of it

A field can be empty in two ways, and databases have always kept them apart.

- **null** means nobody has said. The value is unknown, or does not apply.
- **blank** means somebody said nothing: a real value, a string of no length.

### What DbDo stores

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

### What DbDo says

**A null field says "null". An empty one says "blank".**

Both words are borrowed rather than invented. Database tools print the literal
word NULL for a null and have done for decades: phpLiteAdmin shows it in italics
so it cannot be mistaken for the text "NULL", DB Browser and SQL Server
Management Studio do the same, and the sqlite3 shell has a setting for choosing
the word. "Blank" is what JAWS and NVDA say when they reach an empty cell, in
Excel and in any grid, so it is a word you have heard for years that already
means exactly this.

### What the screen readers do, and what DbDo does about it

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

### What Microsoft Access does, for comparison

Access reaches the same place from the other end. Its text boxes trim what you
type and save an empty box as null; a zero-length string cannot be typed into a
datasheet at all, and takes an update query to create. Its **Allow Zero Length**
property exists to forbid them outright, and long-standing Access guidance is to
set it to No, because -- in the words of the reference most Access developers
learned from -- there is no visible difference between a zero-length string and
a null, and the distinction should not be forced on the end user.

DbDo does the same thing at the point of writing and then, unlike Access, can
tell you which one you are on when you ask.

### Why it is worth the distinction

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

## The Help menu

Every document that comes with DbDo opens from Help, in your web browser. The
F1 keys open the ones you will want most. F1 is help in every Windows program,
and each extra key picks a different kind of help.

- **Documentation, F1.** This guide.
- **History of Changes, Shift+F1.** What changed in each version.
- **About, Alt+F1.** The version and the license.
- **Key Describer, Control+F1.** Press it, then any key, to hear what that key does.
- **ReadMe, Alt+Shift+F1.** The short start: installing and a first walk.
- **Hotkeys, Control+Shift+F1.** Every key, by menu, by key and by command. Control+F1 describes one key; adding Shift describes them all.
- **Frequently Asked Questions.**
- **Play Tutorials.** The spoken walkthroughs.
- **More Documents.** The Announcement, the Developer Guide, the License, and the Tutorials Transcript. These are used rarely, so they sit one level down.

## The spoken walkthroughs

**Help, Play Tutorials** opens `Tutorials.mkv` -- fourteen walkthroughs, about
three minutes each, as one recording with a chapter at the start of every one.
DbDo hands the file to Windows, so whatever plays that kind of file plays it. If Windows asks which app to use, pick a media player such as VLC, and choose Always so it does not ask again. In
a player that reads chapters, Control+Page Down and Control+Page Up move from
one walkthrough to the next.

The same walkthroughs are written out in `Tutorials.md`, in the help folder, for
reading rather than listening.

## Asking the model on your computer

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

## Tables DbDo keeps for itself

Two tables in every DbDo database belong to DbDo rather than to you:

- **lookups** fills the pick lists. F4 in a field offers what this table holds
  for that table and field.
- **maps** records links between records, which is what Say Related reads.

Neither is offered when you choose a table, open a table in a new window, or
step through tables with Control+Page Down. They are on a hidden list, and
anything NOT on that list is yours and is offered.

They are not locked away. At the dot prompt, `select-table lookups` opens either
one by name, and any SQL statement can read or change them. The rule is about
what a person meets while browsing, not about permission.

SQLite's own tables -- anything beginning with `sqlite_`, and the `sqlean_`
tables the extensions create -- are filtered out earlier and are never offered
at all.

## look and prime: the two computed columns

Every table DbDo makes carries two columns that nobody types into. They are
computed from the fields beside them and kept up to date by the database itself.

### look: a glimpse of a record from somewhere else

`look` is what a record looks like when it is mentioned somewhere other than its
own table.

The idea is older than DbDo. In Clipper, under DOS, a form with a foreign key in
it showed a number, and a number is not a record. So the practice was to put a
glimpse of the referenced record in parentheses after the id: enough to know
which one it is, and no more. Tab through a form of fields, reach the field that
points at another table, and hear something meaningful rather than "4".

That is what `look` holds. It joins the fields that identify a record to a
person, separated by a space, a vertical bar and a space:

    Example Widgets Company (sample employer) | Accessibility Analyst | interviewing

The separator is chosen for the ear: a screen reader pauses at punctuation, so
the parts arrive as parts rather than as one run-on line.

A glimpse is deliberately not the whole record. When you need the rest, it is
one keystroke away: Enter opens the record, and Shift plus R lists what this
record is related to, showing each related record by its own `look`.

### prime: the primary key, computed

`prime` is short for primary key, and it is a primary key in the sense that
matters rather than the sense SQL means.

The table does have a formal key -- `<table>_id`, a number the database hands
out -- but that number says nothing about the record. What decides whether two
rows are the same job is the employer and the title. So `prime` is computed from
exactly the fields that make a record unique, joined with a vertical bar and
nothing else:

    Example Widgets Company (sample employer)|Accessibility Analyst

Two things follow, and both are the reason it is done this way.

**The rule can be changed.** Deciding that a job is unique by employer, title
and location as well is a change to one expression, not a migration of a key.

**Matching becomes one comparison.** When a script asks whether to add a record
or update the one already there, it compares one value instead of three, in SQL
or in any language: `WHERE prime = ?`. The maps table works the same way -- it
records a link as `prime1`, a kind, and `prime2` -- so a relationship needs no
knowledge of how either table numbers its rows.

### The difference in one line

`look` is for a person, joined with spaces so it reads aloud. `prime` is for a
program, joined without them so it matches exactly.

## How a Say command answers

Every Say command answers in one shape:

    <name>: <value>

The name is the word in the Say menu and in the hotkey list, so the answer says
which question was asked. That matters when you are walking the alphabet and
lose your place: "Unmarked" does not say which key produced it, and
"mark: unmarked" does.

An empty answer keeps its label and says none -- "find: none", not "No find
string" -- and every count matches its noun.

**One exception, and it is a rule of its own.** A two-state answer whose value
already names the setting says the state alone: Say Mark answers "Marked" or
"Unmarked", not "mark: marked". Prefixing it would say the same word twice, and
only the second word carries information. It is the shape a toggle uses
everywhere else: "<setting> on" and "no <setting>", never "<setting>: on".

Two commands use the column name instead of the menu word, because there the
column is the more useful label and it is what the grid already calls that
field:

- Say Cell answers `employer: Example Widgets Company`
- Say Id answers `job_id: 3`

One press speaks. A second press within a moment shows the same text in a
read-only window you can read line by line and copy from.

### The Say keys

- Shift+A -- added
- Shift+B -- bookmark, how many are saved and the newest
- Shift+C -- cell, as column and value
- Shift+D -- database, as file and folder
- Shift+E -- edited
- Shift+F -- find
- Shift+G -- goto, the jump search
- Shift+B -- bookmark
- Shift+I -- id
- Shift+J -- jump, the text Jump to Record would offer next
- Shift+J -- jump, the text the Jump dialog would offer next
- Shift+L -- look
- Shift+M -- mark
- Shift+N -- notes
- Shift+O -- order
- Shift+P -- prime
- Shift+Q -- query
- Shift+R -- related
- Shift+S -- select, the columns shown
- Shift+T -- tags
- Shift+U -- url
- Shift+V -- replacement value, the text Replace would offer next
- Shift+V -- replace, the text Replace would offer and what it would put there
- Shift+W -- where, the filter
- Shift+X -- regex replace, the pattern Regex Replace would offer next
- Shift+Y -- yield, how many rows
- Shift+H -- here: the table, the row and the order

Shift plus a letter answers a question. A letter on its own moves: type the
first letters of a value and the list view goes there, in lower case and
without regard to case. The two layers do not collide, which is why the Say
commands live on Shift.

Three letters are still unused -- H, K and X -- and are kept that way on purpose,
so a new question can be added later without moving an answer somebody has
learned. Pressing one of them says so rather than staying silent: the say layer
always answers, because silence cannot be told from a key that did not register.

**A command that opens a dialog with a value already in it has a value worth
hearing first.** That is what Shift+B, Shift+J and Shift+V are: the bookmark
list, the text Jump would offer, and the pair Replace would offer. Hearing the
answer is often enough, and the dialog never has to open.

**none and blank are different answers.** "none" means there is no value --
nothing was ever set. "blank" means there is a value and it is empty. A listener
cannot tell those apart from silence, and they are different facts.

Shift+J used to open the Jump dialog. It now says the jump text instead, which
is what Say Jump on that key was always meant to do. Jump to Record keeps
Control+J.

## Credits for the tutorial voices

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
