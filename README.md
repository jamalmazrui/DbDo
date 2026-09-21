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
  there. Control+F, Find, searches every field.
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

## Convention over configuration: four nouns and one junction

Most of the relational worlds people actually keep -- a convention, a club roster, a project tracker, a contact book -- reduce to a handful of nouns and the relationships among them. DbDo leans into that. NFB2026Convention.db uses exactly four noun tables plus DbDo's two standard infrastructure tables, and every table -- infrastructure included -- carries the full standard column set (`<singular>_id` primary key, `added`, `edited`, ..., `notes`, `tags`, `look`, `unq`, `marked`):

- **contacts** -- people and organizations. The field roster (`first_name`, `middle_name`, `last_name`, `gender`, `date_of_birth`, three phone fields, two email fields, `address1`/`address2`/`city`/`state`/`zip`/`nation`, `enterprise`, `job`, `url`) is a general-purpose contact schema designed so the Record Edit dialog can give every field a distinct accelerator key.
- **events** -- one row per discrete agenda entry: `event_date`, `start_time`, `end_time` (24-hour, so chronological sort is plain text sort), `title`, and a `details` memo. No subevents, no tracks -- every entry stands alone.
- **locations** -- the hotel's rooms and spaces: `name`, `level`, `hotel`. Levels follow the agenda's own rule (room numbers starting with N are on level N; lettered salons are the Lone Star Ballroom on 3; numbered salons are the JW Grand Ballroom on 4).
- **projects** -- products, services, and other ongoing endeavors: a work in progress that evolves over time justifies the term. Parsed from the agenda by named-program patterns (academies, awards, fairs, camps, scholarship programs) and a curated brand list (NFB-NEWSLINE, Aira, Monarch, Dot Pad), with shorter name variants merged into their fuller titles. Ownership and appearances are never columns here -- an event **features** a project, and an organization **offers** one, both as maps rows.
- **maps** -- the heart of the model: a *generic typed association* between any two records in any tables. Each row holds `(tbl1, unq1, kind, tbl2, unq2)` -- the subject, the relationship kind, and the object -- identified by `unq` values rather than integer keys, so map rows are human-readable in the grid, survive export and re-import, and can be authored by script. The kinds here: **presents** (a contact presents at, chairs, or leads an event; the stated role and affiliation ride in the map row's `notes`), **located_at** (an event happens at a location), **sponsors** (an organization sponsors an event), **features** (an event features, demonstrates, or discusses a project), and **offers** (an organization provides a project). Any table pair, any cardinality -- one-to-many and many-to-many are the same row shape, and parent/child is just a matter of which side of a kind you read. The same one table could equally relate a contact to a location, an event to an event, or anything to anything -- new relationship kinds need a lookups row, not a new junction table.
- **lookups** -- the standard valid-values table, seeded with the `maps.kind` vocabulary and the hotel names.

The point of the maps model: "all events related to this contact" and "all events at this location" are the SAME query shape -- filter maps by one side, read the other side -- and because the answer comes back as a single-table SELECT through an IN-subquery, the resulting view stays editable in DbDo.

## Valid values become comboboxes (the lookups table)

A **lookups** table defines the allowed values for a field, so the Record Edit dialog can present that field as a ComboBox -- the Windows control that works best from the keyboard, with type-ahead and arrow navigation that every screen reader announces cleanly -- instead of a bare text box. Each lookups row binds a value to a `tbl` and `fld` (with an optional `src` authority and a `descrip`). DbDo offers the combobox whenever a field has values defined.

NFB2026Convention.db carries lookups for its own fields -- every `maps.kind` it uses (presents, located_at, sponsors, features, offers, affiliated_with, part_of), the `projects.kind` list (product, service, program, app, ...), and `locations.hotel`. A separate, shared **lookups.db** ships alongside with global lists -- `state` and `country` -- bound to any table that has a field of that name, so they serve the other sample databases too (Northwind's `country` field gets a combobox with no per-database setup).

## Background: four decades of nonvisual database tools

DbDo is the latest in a line of accessible database managers I have built over nearly forty years. I worked as a database administrator at Harvard's Kennedy School of Government in the 1980s; when the field moved from the DOS command line to the Windows graphical interface and tools like Microsoft Access, the screen readers of the day could not make those tools usable -- a barrier that cost me a promotion and pushed me toward building accessible software myself.

- **Contact Tracking System (DOS).** My first accessible database tool: a keyboard-and-speech contact and records manager for the DOS era, when a well-structured text screen was the most accessible interface available.
- **DbDialog (AutoIt).** A friendly database manager for Windows, built in the AutoIt scripting language on top of my own label-based-controls library (LbC) so that every field was a standard, screen-reader-friendly Windows control. I later packaged DbDialog to run as a script package within the Window-Eyes screen reader as well.
- **DbDo (this program).** A Windows desktop application with two interfaces over one live connection -- a multiple-document graphical interface and a dBASE-style console dot prompt -- supporting SQLite, Access, Excel, dBASE, and delimited files in a relational model designed from the ground up for screen-reader and keyboard efficiency.

DbDo's keyboard and command conventions also draw on two other accessible tools I build and maintain: **[EdSharp](https://github.com/jamalmazrui/EdSharp)**, a text and code editor optimized for keyboard and screen-reader users, and **[FileDir](https://github.com/jamalmazrui/FileDir)**, a file and directory manager in the same spirit. The multiple-window model, the Key Help mode that announces a command instead of running it, the spoken status-query commands, and the in-place text-field convenience keys all matured in those programs first; DbDo carries the same muscle memory into a relational-database setting.

Across that span I also served as founding director of the Boston Computer Society's Visually Impaired and Blind User Group, as an analyst at the National Council on Disability, and as deputy director of the FCC's Accessibility and Innovation Initiative, and I maintain a large free-software repository for blind computer users. DbDo carries that history forward.

To the best of my research, no other general-purpose relational database manager has been built specifically for screen-reader users; mainstream desktop database tools are designed for sighted, mouse-driven use and are accessible only incidentally, if at all. DbDo is built to close that gap.

## License

DbDo is free and open source under the MIT License. See [License.md](License.md).
