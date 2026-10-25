# DbDo -- Developer Guide

How to build DbDo from its source and change it. This file is written for
programmers.

## What you need

Nothing by hand. `build.cmd` finds or fetches everything it uses:

- the .NET Framework 4.8 C# compiler (`csc.exe`), from Visual Studio Build Tools;
- the Homer Development Kit in `C:\HomerDev`, whose shared C# classes DbDo
  compiles in -- Inix, KeyMap, KeyName, Lbc, Log, Ollama, Paths, Say, Util, Web;
- pandoc, for the .htm versions of the documents;
- Inno Setup, for the installer;
- Python, for the tutorial and hotkey documents;
- ffmpeg and two piper voices, for the spoken tutorials, the first time only.

Missing tools are fetched with winget. Everything the build does is written to
`logs\\DbDo-build-<date>-<time>.log`, a new file for every build.

## Building

From `C:\DbDo`:

    build

It compiles `DbDo.cs` into `exec\DbDo.exe`, writes `Hotkeys.md` from the menus,
converts every document to .htm, builds the spoken tutorials if they are
missing (delete help\\Tutorials.mkv and the Tutorial*.mp3 files to re-record), and compiles `DbDo_setup.exe`. The installer will not compile without
the tutorials: a release without them is refused at build time rather than
shipped.

## The four scripts, and the order they run in

1. **`build`** -- steps `version.txt`, writes `Version.cs`, refreshes the
   kit's tools into `scripts`, puts the project's files into the Homer
   encoding, compiles into `exec`, speaks any tutorial with no audio, and
   builds `DbDo_setup.exe` at the top of the project.
2. **`scripts\push "message"`** -- rewrites the whitelist from
   `RepoFiles.txt`, adds what it names, commits, pushes.
3. **`scripts\tidy`** -- the periodic clean: strays into `notes`,
   fetched things deleted, zero-byte files deleted, whitelist rewritten.
4. **`scripts\release`** -- runs `scripts\check --build`, tags the pushed
   commit with the version stamped in `DbDo_setup.exe`, publishes the
   installer as a GitHub release.

`scripts\unpushed` undoes a local commit that should not go up. Every tool
takes the project to be the folder it is run in, or its parent when that folder
is `scripts` or `exec`.

**The kit is a development-time dependency only.** DbDo needs HomerDev 1.43.6 or
later to build; the installed program runs with no kit on the machine.

## The layout

The development folder and the installed folder have the same shape. Sources and
build files stay at the top, with ReadMe and License; everything else sits in the
folder it is installed to. To try a build, run `exec\\DbDo.exe`.

`scripts\tidy`, from the Homer Development Kit, puts the folder back into this
shape: it moves programs into `exec`, documents into `help` and logs into
`logs`, and moves anything the project does not name into `notes`. Run
`tidy` to tidy the folder and the repository, `tidy --folder-only` to tidy the
folder alone. `LocalFiles.txt` tells it what belongs on this disk but not in the
repository: the tutorial scripts, the generated audio and the fetched voices.

- `configs` -- settings shipped with the program: `DbDo.inix`
- `data` -- the shared lookups database, `lookups.db`
- `exec` -- the program and its libraries
- `help` -- the documents, the tutorials and their recording
- `logs` -- in the development folder, one file per build, clean, tutorial or
  audit run, named `DbDo-<task>-<date>-<time>.log`. Sort by name and they are in
  order; zip the folder and you have them all.
- `scripts` -- screen reader scripts and tooling
- `templates` -- the template databases, copied to your own folder on first run

Your own copies live under `%LOCALAPPDATA%\DbDo`.

## Checks

`scripts\auditPatterns.py` checks the rules that are easy to break and hard to
notice, including:

- every Say command answers in the form `name: value`;
- every Shift+letter is a Say command;
- no menu letter falls in the middle of a word, and a menu letter matches its
  hotkey's letter;
- every key a tutorial teaches is bound in the program;
- tutorial scripts are complete.

Run it before a release. It writes `logs\\DbDo-audit-<date>-<time>.log`.

## Keys

Every key follows the Homer rules, written out in `C:\HomerDev\help\HomerDev.md`:
a letter is the first letter of a word in the command; Shift and a letter asks;
adding Shift reverses; each function key is a family. `Hotkeys.md` is generated
from the `addItem` calls, so a key changed in the code is changed in the
document at the next build.

## Tutorials

Each tutorial is a demo script, `help\Tutorial_NN_Topic.inix`: a `[global]`
section for the voices and timing, then one `[step]` per exchange --
`Say=` for the narrator, `Key=` for the keystroke, `Hear=` for the screen reader,
`Note=` for the written transcript only. `scripts\buildTutorials` speaks them
into `help\Tutorials.mkv`; `scripts\makeTutorial` writes `help\Tutorials.md`.
The scripts are not published; the recording and the transcript are.

## Releasing

After a build, `scripts\release` tags the version stamped in `DbDo_setup.exe`,
creates the GitHub release, and checks that the public download link answers.

## Learnings from DbDialog

DbDialog (2006 to 2015, AutoIt and ADO) was Jamal's single-dialog database manager, and DbDo's ancestor. Its source and guide were reviewed on 19 October 2026 for what DbDo should take back. The filter design and its Invert, Marked and Unmarked choices are done; the other four proposals are accepted and scheduled.

### Done: filters as one SQL condition

DbDialog built every filter as a SQL WHERE clause and read the table again with it. DbDo had moved field filters to ADO's Recordset.Filter, which cannot nest an OR group inside an AND. Control+F now builds one SQL condition again, with New, Clear, Narrow and Expand (see History, 19 October 2026). The operator prefixes in the Fields form (= != > >= < <= %) are DbDialog's, which read a value's first characters the same way.

### Proposed: Clone Table

DbDialog's New Table offered Clone: a new, empty table with the current table's structure, asked for in one question, the name. DbDo has no equivalent; Add Table always starts from a blank form.

- **Design:** Add Table asks first, with buttons, whether to Define a new structure or Clone the current table's.
- **How Clone works:** it reads the current table's distinct fields and their declared types (PRAGMA table_info). It passes them to lStandardTableDdl, so the clone gets DbDo's standard columns and triggers like any new table.

### Proposed: define a table as text, without the ten-field limit

DbDialog's Define offered a template in a multi-line text box, one field per line in the form "name: size", with a memo field last. A screen reader user reads and edits it like any text, can paste a structure copied from elsewhere, and is not limited to a fixed number of slots. DbDo's definition form has ten name-and-type slots, so a table with more fields needs SQL.

- **Design:** a Text button beside the form opens the same definition as editable lines, "name: TYPE", prefilled with the slots already filled.
- **Types:** the same type words as the form (TEXTLINE, TEXTMEMO, INTEGER and the rest), checked on OK.
- **Copying a structure:** Describe Table's output in the same "name: TYPE" form would let one table's structure be copied to start another.

### Proposed: fields named for unique access letters

DbDialog named its Contact fields so each had a unique initial letter, and so a unique Alt letter in the record form. DbDo already keeps this for contact tables. The proposal is a check: on OK, the definition form says which fields share a first letter, and that only the first of them will get an Alt letter.

### Proposed: Count by the shown columns

DbDialog's Output offered Count: for the fields shown in the list, the number of records with each combination of values. For example, with State and City shown, the number of contacts in each state and city. DbDo's Statistics from Column (Alt+Shift+S) counts one column only.

- **Design:** a Count by Columns report over the columns currently shown in the grid, within the filter in force. It runs SELECT those columns, count(*) ... WHERE the filter GROUP BY those columns ORDER BY those columns, and shows the result in a read-only window to review and copy, as Statistics from Column does.

### Done: Invert, Marked and Unmarked in Filter Records

DbDialog's Filter choices included Invert, Tagged and Untagged. Done on 19 October 2026:

- **Invert** is a fifth button beside New, Clear, Narrow and Expand while a filter is in force. It shows the records the filter leaves out, as NOT COALESCE((condition), 0), so a record whose field is empty is counted as not matching rather than falling out of both views.
- **Marked and Unmarked** sit beside Any Field and Fields, so New, Narrow and Expand combine them like any other condition. They appear when the table has DbDo's marked column.

### Decided: the other four proposals are accepted

Each was weighed on 19 October 2026 under the instruction to decide, justify and keep a way back. All four are accepted, to be built one per DbDo delivery after the router's phase B, since each is a small, separate change:

- **Clone Table.** Accepted. It answers a real gap (Add Table always starts blank) in one question, and reuses lStandardTableDdl, so a clone is a standard DbDo table.
- **Define a table as text.** Accepted. It removes the ten-field limit and lets a screen reader user read, paste and edit a structure as ordinary text, which is Homer's preference for text over forms of slots.
- **Fields named for unique access letters.** Accepted, as a warning on OK, not a refusal: a shared first letter still works, it only costs an Alt letter.
- **Count by Columns.** Accepted. It generalizes Statistics from Column to the columns shown, within the filter in force, in the same read-only window. Its key is chosen when it is built, under the key rules.

## Learnings from Quill Radio

From Quill Radio, Community Access's internet radio app, whose 60,000-station offline catalog is a SQLite database a screen reader user searches and arrows through all day (read 25 October 2026 in the QUILL repository, where Quill Radio now lives). Each is a plan to weigh, not yet built.

- **Instant search by SQLite's full-text index (FTS5).** Quill Radio keeps a `stations_fts` virtual table beside its catalog and answers Find Stations from it as each letter is typed. DbDo's Keywords (Control+K) reads every record. An FTS5 table over each user table's look and data fields, kept current by triggers like DbDo's own edited trigger, would make Keywords instant on a large table, and could rank matches. Two things first: the SQLite build DbDo opens databases with must include FTS5, and the index is derived data, so it belongs beside DbDo's own tables (lookups, maps, views), never mixed into a user's.
- **Choose Columns as two lists, never checkboxes in a list.** Quill Radio's reasoning: a report list is read one column at a time, so the columns chosen *are* the sentence heard on every row. Its dialog has Shown and Hidden lists with Move Up, Move Down, Hide and Show, because a checkbox in a list is a state the reader must be asked for, while a position in a list is a place you land on, and the announcement after a move says where you are. DbDo's views table already records a view's fields; this is the shape a dialog for choosing and ordering them should take.
- **A database rebuilt beside the live one, then switched.** Quill Radio never rewrites its live catalog: it builds the next copy as `catalog.<n+1>.db`, then replaces a tiny pointer file, so a crash leaves the old catalog untouched, and stale copies are removed on the next open. Windows will not replace a database file a connection holds open. This fits DbDo's derived files -- an FTS index, an import's staging copy -- not a user's database, which is edited in place.
- **Quick Actions, one reorder pattern everywhere.** Wherever Quill Radio lets a list be rearranged, it is the same real list box with Move Up, Move Down and Make Default, and the dialog knows nothing of what it reorders, so every app shares one. DbDo's field order, report columns and view fields could all use one such dialog from the kit.
