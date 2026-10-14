# DbDo -- the keyboard-first database manager

DbDo opens a database and lets you read it, add to it, sort it, filter it, follow
its links, and hand it to other people -- all from the keyboard, with your screen
reader doing the talking. It works with JAWS, NVDA and Narrator.

Every table is a list you arrow through. Every command has a key, and every key
is named for a word in its command, so it can be remembered rather than looked
up.

## Installing

Download [the DbDo installer](https://github.com/JamalMazrui/DbDo/releases/latest/download/DbDo_setup.exe)
and run it. Windows asks for permission first, because DbDo installs for
everyone on the computer; the prompt can open behind other windows, so press
Alt+Tab if nothing seems to happen.

On the last page you can add scripts for JAWS and NVDA, and Ollama, which lets
DbDo answer questions with AI that runs on your own computer. Leave **Launch
DbDo** ticked and press Enter. After that, **Alt+Control+D** starts DbDo from
anywhere in Windows -- D for DbDo.

To update later, press **F11** inside DbDo: Elevate Version. It checks for a
newer release and offers to install it.

## Quick start

The first time it runs, DbDo opens **JobTrail**, a job search database that
comes with it. JobTrail keeps the jobs you are after, the people you meet, the
documents you send, the stories you tell in interviews, and a log of every step
you take -- the log an unemployment office or a rehabilitation counselor asks
for.

Five things to try:

- **Arrow through a table.** Each row is one record. Down and Up move between
  records; Left and Right move between fields. Shift+C, Say Cell, tells you
  which field you are on and what it holds.
- **Add a record.** Control+N, New. Tab through the fields and press
  Control+Enter to save.
- **Find something.** Type the first letters of what you want and the list goes
  there. Control+K, Find, searches every field.
- **Choose what you hear.** Alt+S, Select Columns, picks the fields each row
  speaks.
- **Get something out.** Alt+Shift+R runs a Report, such as the Work Search
  Record for a claim.

## Learning more

- **Play Tutorials,** on the Help menu, plays fifteen short spoken walkthroughs
  that follow one job seeker through JobTrail. Take them in order: each one
  builds on the last.
- **[DbDo.md](help/DbDo.md)** (F1) is the full guide.
- **[Hotkeys.md](help/Hotkeys.md)** (Control+Shift+F1) lists every key, by menu, by key and by
  command, with the reason for each.
- **[FAQ.md](help/FAQ.md)** answers common questions.
- **[History.md](help/History.md)** (Shift+F1) says what changed in each version.
- **[Developer.md](help/Developer.md)** explains how to build DbDo from its source.

## Convention over configuration: a few nouns and one junction

Most of the related records people actually keep -- a club roster, a project tracker, a record collection, a convention -- come down to a handful of nouns and how they relate. DbDo builds on that. Every table, the system tables included, has the same standard columns. First comes a `<singular>_id` key, then `added` and `edited` stamps kept by triggers, then the table's own fields. After them come `url`, `notes` and `tags`, a generated `look` that a row speaks, a generated `prime` that names the record, and `marked`.

The **maps** table is the heart of the model. It is a general, typed link between any two records in any two tables. Each row holds `(tbl1, prime1, kind, tbl2, prime2)`: the subject, the kind of relationship, and the object. Records are named by their `prime` values rather than by number keys. So map rows read plainly in the grid, survive export and import, and can be written by a script. One-to-many and many-to-many links have the same row shape. In MusicTrail, a performer **recorded** an album, and a musician is a **member_of** a band, so John Bonham leads to Led Zeppelin and on to Led Zeppelin IV. In BlindCreations, a creator is **author_of** a book or **developer_of** an app.

The point of the maps model is this: "everything this person made" and "everyone who made this" are the SAME kind of query. You filter maps by one side and read the other. The answer comes back as a query on a single table, through an IN-subquery, so the view it makes stays editable in DbDo.

For a convention or conference of any organization, the HomerDev kit's **convention-database** skill builds the model from the agenda. It makes events, locations, projects and contacts, linked by **presents**, **located_at**, **sponsors**, **features** and **offers**. Its scripts turn the sessions you mark into a calendar file, an accessible schedule and contact cards.

## Valid values become comboboxes (the lookups table)

A **lookups** table lists the allowed values for a field. The Record Edit dialog can then show that field as a ComboBox instead of a bare text box. A ComboBox is the Windows control that works best from the keyboard: you can type ahead or use the arrows, and every screen reader announces it clearly. Each lookups row ties a value to a `tbl` and a `fld`, with an optional `src` source and a `descrip`. DbDo offers the combobox whenever a field has values listed.

Each template carries lookups for its own fields. They include every `maps.kind` it uses, MusicTrail's genres and roles with the books' own descriptions, and TechTraining's categories with the directory's definition of each. A separate, shared **lookups.db** comes with DbDo, holding the global lists `state` and `country`. They apply to any table with a field of that name, so they serve every database. For example, Northwind's `country` field gets a combobox with no setup in that database.

## Background: four decades of nonvisual database tools

DbDo is the latest in a line of accessible database managers I have built over nearly forty years. I worked as a database administrator at Harvard's Kennedy School of Government in the 1980s. Then the field moved from the DOS command line to the Windows graphical interface and tools like Microsoft Access, and the screen readers of the day could not make those tools usable. That barrier cost me a promotion, and it pushed me toward building accessible software myself.

- **Contact Tracking System (DOS).** My first accessible database tool. It managed contacts and records by keyboard and speech, in the DOS era, when a well-structured text screen was the most accessible interface there was.
- **DbDialog (AutoIt).** A friendly database manager for Windows, built in the AutoIt scripting language. It used my own label-based-controls library (LbC), so that every field was a standard Windows control that screen readers handle well. I later packaged DbDialog to run as a script package within the Window-Eyes screen reader as well.
- **DbDo (this program).** A Windows desktop application with two interfaces over one live connection: a graphical interface that holds several documents, and a dBASE-style console dot prompt. It supports SQLite, Access, Excel, dBASE and delimited files, in a relational model designed from the ground up for screen reader and keyboard efficiency.

DbDo's keyboard and command conventions also draw on two other accessible tools I build and maintain. **[EdSharp](https://github.com/jamalmazrui/EdSharp)** is a text and code editor made for keyboard and screen reader users, and **[FileDir](https://github.com/jamalmazrui/FileDir)** is a file and directory manager in the same spirit. Several ideas matured in those programs first: the model of several windows, the Key Help mode that announces a command instead of running it, the spoken status commands, and the convenience keys inside text fields. DbDo carries the same habits of hand into a relational database.

Across that span I also served as founding director of the Boston Computer Society's Visually Impaired and Blind User Group, as an analyst at the National Council on Disability, and as deputy director of the FCC's Accessibility and Innovation Initiative. I also maintain a large free-software repository for blind computer users. DbDo carries that history forward.

To the best of my research, no other general-purpose relational database manager has been built for screen reader users. Mainstream desktop database tools are designed for sighted use with a mouse, and are accessible only by chance, if at all. DbDo is built to close that gap.

## License

DbDo is free and open source under the MIT License. See [License.md](License.md).
