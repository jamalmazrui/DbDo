# DbDo -- the keyboard-first relational database manager

**DbDo opens a relational database and lets you read it, reorder it, filter it, follow its relationships, and hand it to other people in whatever format they need -- entirely from the keyboard, with a screen reader doing the talking.** JAWS, NVDA, and Narrator are all first-class. Every command has a hotkey, every row and cell move is spoken, and a dot-prompt console rides alongside the GUI for one-off SQL.

This build opens **NFB2026Convention.db** automatically on first launch (until you open something else, after which DbDo remembers your last file). It is built on the small, general schema DbDo favors, so the navigation habits you form here carry over to almost any data you keep.

## Convention over configuration: five nouns and one junction

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

NFB2026Convention.db carries lookups for its own fields -- `maps.kind` (presents, located_at, sponsors) and `locations.hotel`. A separate, shared **lookups.db** ships alongside with global lists -- `state` and `country` -- bound to any table that has a field of that name, so they serve the other sample databases too (Northwind's `country` field gets a combobox with no per-database setup).

## A guided tour by keyboard

Launch DbDo. On a first run it opens NFB2026Convention.db on the **events** table and announces the row count.

**Read the schedule.** Arrow up and down to move between sessions; DbDo speaks each row. To hear one session field by field, use the virtual cell cursor: Alt+Control+RightArrow / LeftArrow step across columns announcing "header: value," and Alt+Control+Numpad5 says the current cell (twice to spell it).

**Reorder and filter to the question you're asking.** Sort, Shift+S, with `event_date, start_time` for chronological order. Filter, Shift+F, with `event_date = '2026-07-05'` for one day or `title LIKE '*Braille*'` for a topic; Clear Filter is Shift+R. Find Across All Columns is Control+F; Jump to Match in one column is Control+J.

**See a record's whole story.** Press Enter on an event for Show Record: every field as `name = value`. Say Related, Shift+R, then lists the look line of each associated record -- on an event, its location and its presenters; on a contact, every event they present; on a location, everything happening there.

**Follow the associations.** Enter Child Table, Alt+RightArrow, now drills through maps as well as foreign keys: on a contact it offers "events via presents"; on an event, "contacts via presents (incoming)" and "locations via located_at"; pick one and the related records open as a filtered view, with Alt+LeftArrow returning to the exact row you left and Alt+Home popping the whole drill stack. The **maps** table is also a first-class grid in its own right: open it to browse every relationship as a readable row, filter it by `kind`, or add a row to declare a new association -- relating any record to any other is an ordinary record edit, not a schema change.

**Pick from valid values.** When you edit a field that has a lookups list -- a map row's `kind`, a location's `hotel` -- the editor is a combobox: arrow or type-ahead to a value, or type a new one. Country and state fields anywhere draw on the shared lookups.db.

**Hand it to someone else in their format.** Export Data, Control+Shift+X, writes the current filtered, sorted view to xlsx, docx, filtered HTML, Markdown, CSV, TSV, SQLite, Access, or dBASE. From the dot prompt, `Export-Data xlsx docx md csv` writes all four at once.

## The included demo scripts

Three SQL scripts in `Scripts` show the relational queries behind the views above, each demonstrating the maps join pattern (filter both `tbl` columns explicitly, join on `unq` with `=`):

- **ConventionSchedule.sql** -- the full schedule, each event with its location and level.
- **SpeakerSessions.sql** -- every event a given person presents, with their stated role (change the surname on the WHERE line).
- **DayAtAGlance.sql** -- one day's events ordered by start time, with locations.

## The other sample databases -- the same column convention

The Help menu also opens five more databases, all migrated to the same standard columns (`<singular>_id` primary keys such as `teacher_id` and `wine_id`, with each foreign key carrying the same name as the parent primary key it references; `TEXTTIME` `added`/`edited` maintained by triggers; generated `look`/`unq`; `marked` last): the school `sample.db`, the classic `northwind.db` and `chinook.db`, a music `collection.db`, and a wine `cellar.db`. They keep their own domain tables -- the five-noun model is the favored shape for a *new* database, not a straitjacket for every domain -- but the keyboard moves are identical, which is the whole mission: whatever relational data you already know, DbDo gives a screen-reader and keyboard user full, efficient command of it.

## Building this archive

This is a **source distribution**: compile once with `buildDbDo.cmd` to produce `DbDo.exe`. Three Camel Type modules are included to wire in during that build -- `FkResolution.cs` (schema-truth relationship navigation), `Lookups.cs` (field comboboxes), and `ImportNormalization.cs` (convention-conforming import). See `DbDo_BuildNotes.md` for the integration steps.

## License

MIT. See `License.md`.
