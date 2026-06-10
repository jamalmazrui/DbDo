# DbDo -- the keyboard-first relational database manager

**DbDo opens a relational database and lets you read it, reorder it, filter it, follow its relationships, and hand it to other people in whatever format they need -- entirely from the keyboard, with a screen reader doing the talking.** JAWS, NVDA, and Narrator are all first-class. Every command has a hotkey, every row and cell move is spoken, and a dot-prompt console rides alongside the GUI for one-off SQL.

This build opens **NFB2026Convention.db** automatically on first launch (until you open something else, after which DbDo remembers your last file). It is built on the small, general schema DbDo favors, so the navigation habits you form here carry over to almost any data you keep.

## Convention over configuration: five nouns and one junction

Most of the relational worlds people actually keep -- a convention, a club roster, a project tracker, a contact book -- reduce to a handful of nouns and the relationships among them. DbDo leans into that. NFB2026Convention.db uses exactly five noun tables plus one junction, and every table shares the same standard columns (a primary key, `added`, `edited`, ..., `notes`, `tags`, `look`, `unq`, `marked`). Note: this database still uses the older bare-`id` primary-key shape; it is awaiting a schema redesign, after which its keys will follow the `<singular>_id` convention described below:

- **contacts** -- every *party*, person or organization. Persons fill `first_name`/`last_name`; organizations leave those empty and fill `org_name`. A self-reference `organization_id` points a person at their employer, which is just another contact row -- so "who works where" needs no second table.
- **places** -- venues, rooms, cities. A self-reference `parent_id` gives room-inside-venue-inside-city (here: the JW Marriott Austin's rooms inside the venue inside Austin).
- **events** -- the convention and its sessions. A self-reference `parent_id` makes the convention the root event and each session a child; `place_id` says where it is held.
- **projects** -- the cross-cutting combiners. The convention's tracks are projects; a project gathers sessions and people from across the other tables.
- **associations** -- the universal junction, and the heart of the model. Each row carries a `role` plus nullable foreign keys to each noun: `contact_id`, `event_id`, `place_id`, `project_id`. One row can say "Mark Riccobono, as Presiding Officer, at the Opening General Session, in the Lone Star Grand Ballroom, under the Advocacy & Policy project." Because every link is a real declared foreign key, DbDo can drill each one in both directions.

The point is that you learn these five nouns once and navigate them everywhere. Organizations are not a separate concept from people; a venue, a room, and a city are all just places; the convention and its talks are all just events; and a project is simply a named purpose that ties chosen entities together.

## Valid values become comboboxes (the lookups table)

A **lookups** table defines the allowed values for a field, so the Record Edit dialog can present that field as a ComboBox -- the Windows control that works best from the keyboard, with type-ahead and arrow navigation that every screen reader announces cleanly -- instead of a bare text box. Each lookups row binds a value to a `tbl` and `fld` (with an optional `src` authority and a `descrip`). DbDo offers the combobox whenever a field has values defined.

NFB2026Convention.db carries lookups for its own fields -- `contacts.kind` (person, org), `places.kind` (city, venue, room), `events.kind` (General Session, Division/Group Meeting, Seminar, ...), `projects.kind`/`status`, and `associations.role` (Speaker, Moderator, Panelist, ...). A separate, shared **lookups.db** ships alongside with global lists -- `state` and `country` -- bound to any table that has a field of that name, so they serve the other sample databases too (Northwind's `country` field gets a combobox with no per-database setup).

## A guided tour by keyboard

Launch DbDo. On a first run it opens NFB2026Convention.db on the **events** table and announces the row count.

**Read the schedule.** Arrow up and down to move between sessions; DbDo speaks each row. To hear one session field by field, use the virtual cell cursor: Alt+Control+RightArrow / LeftArrow step across columns announcing "header: value," and Alt+Control+Numpad5 says the current cell (twice to spell it).

**Reorder and filter to the question you're asking.** Sort, Shift+S, with `day, start_time` for chronological order or `kind, start_time` to group by session type. Filter, Shift+F, with `day = '2026-07-05'` for one day, `kind = 'General Session'` for the plenaries, or `ticketed = 1` for what needs a ticket; Clear Filter is Shift+R. Find Across All Columns is Control+F; Jump to Match in one column is Control+J.

**See a record's whole story.** Press Enter on a session for Show Record: every field as `name = value`, then an automatic Related Records section. On a session you see its parent convention, its place, and the people associated with it; on a person you see their organization and the sessions they are part of.

**Drill through the relationships.** Enter Child Table, Alt+RightArrow, opens the children of the current row: from a **project** it opens that track's associations (and onward to its sessions and people); from a **place** it opens the sessions held there; from a **contact** it opens that person's associations; from the convention **event** it opens its sessions. Alt+LeftArrow walks back to the exact parent row; Alt+Home pops the whole stack.

**Pick from valid values.** When you edit a field that has a lookups list -- a session's `kind`, an association's `role`, a place's `kind` -- the editor is a combobox: arrow or type-ahead to a value, or type a new one. Country and state fields anywhere draw on the shared lookups.db.

**Hand it to someone else in their format.** Export Data, Control+Shift+X, writes the current filtered, sorted view to xlsx, docx, filtered HTML, Markdown, CSV, TSV, SQLite, Access, or dBASE. From the dot prompt, `Export-Data xlsx docx md csv` writes all four at once.

## The included demo scripts

Three SQL scripts in `Scripts` show the relational queries behind the views above; because this database still uses bare-`id` keys, its joins qualify the key by table (`events.id`, `places.id`):

- **ConventionSchedule.sql** -- the full schedule, each session with its place and the project it belongs to.
- **SpeakerSessions.sql** -- every session a given person is part of, with their role (change the surname on the WHERE line).
- **DayAtAGlance.sql** -- one day's sessions ordered by time and place, with a people count.

## The other sample databases -- the same column convention

The Help menu also opens five more databases, all migrated to the same standard columns (`<singular>_id` primary keys such as `teacher_id` and `wine_id`, with each foreign key carrying the same name as the parent primary key it references; `TEXTTIME` `added`/`edited` maintained by triggers; generated `look`/`unq`; `marked` last): the school `sample.db`, the classic `northwind.db` and `chinook.db`, a music `collection.db`, and a wine `cellar.db`. They keep their own domain tables -- the five-noun model is the favored shape for a *new* database, not a straitjacket for every domain -- but the keyboard moves are identical, which is the whole mission: whatever relational data you already know, DbDo gives a screen-reader and keyboard user full, efficient command of it.

## Building this archive

This is a **source distribution**: compile once with `buildDbDo.cmd` to produce `DbDo.exe`. Three Camel Type modules are included to wire in during that build -- `FkResolution.cs` (schema-truth relationship navigation), `Lookups.cs` (field comboboxes), and `ImportNormalization.cs` (convention-conforming import). See `DbDo_BuildNotes.md` for the integration steps.

## License

MIT. See `License.md`.
