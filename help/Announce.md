# DbDo

Manage database files in two interfaces, a console and a graphical window, built for the speed of screen reader and keyboard users.

For decades I have been interested in how to make database work usable without sight. With AI assistance, I have now developed a general-purpose program for it. DbDo is a free, open source program that makes entering, querying and reporting data faster.

This is not my first attempt at the problem. My work on nonvisual database access began with a Contact Tracking System I wrote for DOS. It continued with DbDialog, a friendly database manager I built in the AutoIt language and later packaged to run as a script within the Window-Eyes screen reader. It now reaches DbDo, a Windows desktop program with both a graphical window, which can hold several documents, and a console dot prompt. DbDo supports several file formats, in a relational model designed from the ground up for screen reader and keyboard efficiency. As far as I have been able to find, with research help, DbDo is the only general-purpose relational database manager built for screen reader users. Mainstream desktop database tools are designed for sighted use with a mouse, and are accessible only by chance, if at all.

My hope is that DbDo can grow into the most accessible general-purpose database manager for screen reader users. I don't claim it is there yet. It is early, evolving software, shared in the spirit of continuous improvement, and it will get better with use and feedback. To be clear about its scope from the start: DbDo today serves Windows users of the JAWS, NVDA or Narrator screen readers. That is the goal I am building toward, and any feedback that moves it closer is truly welcome.

- [DbDo project page](https://github.com/JamalMazrui/DbDo)
- [DbDo project archive](https://github.com/JamalMazrui/DbDo/archive/main.zip)
- [DbDo executable installer](https://github.com/JamalMazrui/DbDo/releases/latest/download/DbDo_setup.exe)

DbDo uses an SQLite database file, with the `.db` extension, by default. You can manage its related tables from a command line, like the memorable dBASE dot prompt, or from a graphical window with menus and standard controls. You can switch between the two windows easily, whichever suits the task at hand. DbDo also imports and exports other database-like formats, including `.dbf`, `.mdb`, `.accdb`, `.xlsx`, `.csv` and `.tsv`.

Every DbDo command can be done quickly from the keyboard. The installer also brings extensions for the JAWS and NVDA screen readers, to make you more productive still. Advanced features include statistics, charts and scripts.

## Highlights

**A table cursor for the data list.** Alt+Control with an arrow key, Home, End, PageUp, PageDown or NumPad5 moves through the data a cell at a time. Moving across says the column and its value, as "Header: value". Moving down says the row and its value, as "Row N: value". A jump to a corner says both. This is how JAWS and NVDA read tables on the web and in Word, so keys you already know work here.

**Press twice to spell.** Any command that only speaks spells its text letter by letter when you press it twice within one and a half seconds, as in EdSharp and FileDir.

**Three kinds of search, each with a history.** Find, Control+K, looks for text in every column. Jump to Match, Control+J, looks in one column. Find Regex, Control+F3, uses a .NET regular expression. Each remembers its last 10 searches, and whether each one matched case. F3 and Shift+F3 repeat whichever kind you used last.

**From a row to the rows that belong to it.** Alt+RightArrow opens the rows under the current one, such as an artist's albums or an album's tracks. Alt+LeftArrow goes back to the exact row you came from, and Alt+Home goes all the way back at once.

**Recent files that remember where you were.** Alt+R reopens any of your last 10 databases. It brings back the table you had open, with its filter, its sort order and the row you were on.

**One connection, two ways in.** The window and the dot-prompt console work on the same live data. An edit in one shows up in the other at once.

**Small scripts.** Save short JavaScript or text snippets in your own editor, then run one inside DbDo with a single key, Control+Shift+I (I for Invoke). A script can change many records, set filters or make a report: anything DbDo itself can do.

## Template databases

DbDo comes with ten template databases, copied into your own data folder the first time it runs, so each is yours to change. Every one earns its place: it carries real data worth exploring, serves a task people do every day, or is taught by a recorded tutorial. Each follows DbDo's conventions: `<singular>_id` keys, `added` and `edited` stamps kept by triggers, a generated `prime` that names a record, and a `maps` table that links records across tables.

Real data to explore:

- **RadioTrail** — some 55,000 working Internet radio stations, with a script that refreshes the catalog. Find a station by name, place, language or genre, and play it.
- **MusicTrail** — 100 great albums and 200 great musicians across ten genres, with who recorded what and who played in which band, each linked to Wikipedia.
- **BlindCreations** — apps, books and presentations made by blind people, with their creators: about a hundred apps, a hundred books, sixty presentations and over a hundred and twenty creators.
- **TechTraining** — about 1,170 free presentations on technology and accessibility, from webinars to podcasts. Press Alt+Shift+P to play one, or mark several and play them as a list.
- **BlindSkills** — 84 free videos and podcast episodes that teach blindness skills, such as cane travel, braille, cooking and crafts. Press Alt+Shift+P to play a lesson.

Everyday tasks:

- **JobTrail** — a job search: applications, contacts, documents and actions, with the work search record an unemployment office asks for. DbDo opens it the first time, and its tutorials follow one job seeker through it.
- **BookTrail** — your reading: what you have read, are reading and mean to read. Its companion bookFido fills it from your BARD, Audible and Kindle libraries.
- **ContactTrail** — an address book with groups, an emergency list with every number, and an address book report to read or print.
- **RecipeTrail** — recipes and their ingredients with amounts. Mark what you mean to cook, and its shopping list says what to buy, aisle by aisle.

For learning:

- **northwind** and **chinook** — the classic sales and music-store databases most database tutorials use, with real foreign keys and deep parent-child chains. DbDo's spoken tutorials use them to teach following related records and asking with SQL.

## Accessibility design

DbDo speaks through three paths, in this order: to JAWS directly, to NVDA through its controller library, and otherwise through Windows' own live regions, for Narrator and other screen readers. The data list is a standard Details list, which all three read the way they read any such list in Windows. The installer adds JAWS settings, a key map and a compiled script, and an NVDA add-on. With them, both screen readers pass DbDo's keys through, instead of using those keys for their own table or browse commands.

Menus, help and messages name keys the way JAWS does: UpArrow, DownArrow, NumPad0 to NumPad9, GraveAccent, and Minus rather than Dash. Modifiers always come in alphabetical order, as in Alt+Control+Shift.

## Project home and downloads

- **Source, issues, discussion:** <https://github.com/JamalMazrui/DbDo>
- **Latest installer (always points to the current release):** <https://github.com/JamalMazrui/DbDo/releases/latest/download/DbDo_setup.exe>
- **Full project as a zip:** <https://github.com/JamalMazrui/DbDo/archive/main.zip>

For per-release notes, see `History.md`. Released under the MIT License.

Feedback from blind and low-vision developers is especially welcome — keyboard ergonomics, screen-reader announcements, command-name choices, anything that helps the tool fit how people actually work.

Jamal
