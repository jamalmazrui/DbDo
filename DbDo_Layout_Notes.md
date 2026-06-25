# DbDo Layout: Scripts and Databases

## The decision, in one rule

> **A database's scripts live in the same folder as its `.db` file.
> Truly generic scripts live in `%APPDATA%\DbDo\Scripts`. Invoke Script
> and Edit Script show both sets merged, with the database's own
> scripts taking precedence on a name clash.**

Everything below follows from that rule. The organizing habit it
encourages is **one folder per database**: a folder named for the
database root, holding the `.db` and the scripts that belong to it.

## Why this, and what I rejected

Your instinct was right on the evidence: of the 17 bundled scripts,
13 are database-specific (10 convention, 2 northwind, 1 cellar) and
only 4 are generic. Pooling them in one flat `Scripts` folder mixed
unrelated tools together and showed convention scripts while northwind
was open.

I evaluated three structures against "simplest that works":

1. **Per-database folder keyed by name in AppData**
   (`%APPDATA%\DbDo\<name>\`). Rejected: two databases both named
   `books.db` in different places would collide, scripts wouldn't
   travel with the database, and it needs name-derivation and
   collision code.

2. **A further `Scripts` subfolder inside each database folder**
   (`...\music\Scripts\`). Rejected: this is the extra nesting level
   you were worried about, and it buys nothing — the database folder
   already distinguishes scripts from the `.db` by file extension.

3. **Scripts beside the `.db` file (chosen).** A database's folder
   holds `music.db` and its scripts together, flat. Works identically
   for a bundled sample and for a database you open from anywhere on
   disk, with no name-keying and no collisions. Scripts travel with
   the database when you copy its folder.

Generic scripts stay in one well-known place so they're available no
matter which database is open.

## The three trees

The `Samples` wrapper folder groups the bundled databases and keeps
the picker's job trivial (enumerate its subfolders). Nesting tops out
at `Samples\<name>\<file>` — one level deeper than before, no more.

### Source / working tree (`C:\DbDo`, pushed to GitHub)

```
C:\DbDo\
  DbDo.cs, buildDbDo.cmd, DbDo_setup.iss, *.md, *.htm ...
  lookups.db                     (shared infrastructure, not a sample)
  Scripts\                       (GENERIC scripts only)
    CopyRowToClipboard.js
    MarkRowsMatchingRegex.js
    SchemaOverview.sql
    StatusSnapshot.dbdo
  Samples\
    NFB2026Convention\           NFB2026Convention.db + 10 convention scripts
    cellar\                      cellar.db + WineDrinkWindow.sql
    chinook\                     chinook.db
    northwind\                   northwind.db + NorthwindRowCounts.sql, RecentOrders.dbdo
    sample\                      sample.db
    contacts\ howtos\ media\ music\ reads\ recipes\   (the six new hobbyist DBs)
```

### Program tree (`{app}`, what the installer lays down)

Mirrors the source tree: `lookups.db` and `Scripts\` at the root,
and the whole `Samples\` tree copied recursively. The installer line is

```
Source: "Samples\*"; DestDir: "{app}\Samples"; Flags: ignoreversion recursesubdirs createallsubdirs
```

### Per-user tree (`%APPDATA%\DbDo`)

```
%APPDATA%\DbDo\
  DbDo.inix                      (settings and per-table state -- unchanged)
  Scripts\                       (seeded once from {app}\Scripts)
  Samples\
    music\  music.db + scripts   (seeded once from {app}\Samples)
    reads\  reads.db + scripts
    ...
```

Seeding is one-time per folder (a `.seeded` sentinel), so a database
or script you delete does not reappear. Drop your own `.db` into a new
`Samples\<name>\` subfolder and it shows up in the picker; drop a
script beside any open database's `.db` and it shows up in Invoke
Script for that database.

## How the commands behave now

- **Sample Databases (Help):** lists one database per `Samples`
  subfolder, shown by root name, opened through the normal
  state-restoring path (sort/filter/position persist).
- **Invoke Script / Edit Script (Misc):** the pick list is the open
  database's own scripts merged with the generic scripts. A *new*
  script is created in the open database's folder when one is open,
  otherwise in the generic folder.
- **Open Script Folder (Misc):** opens the open database's folder when
  one is open (where its scripts live), otherwise the generic folder.

## Reorganize C:\DbDo

Run this once at the root of your working copy. It assumes the six new
`.db` files are already present at the root (move them in first if not).

```bat
cd /d C:\DbDo
mkdir Samples
for %D in (sample northwind chinook cellar NFB2026Convention reads recipes music media contacts howtos) do mkdir "Samples\%D"
for %D in (sample northwind chinook cellar NFB2026Convention reads recipes music media contacts howtos) do if exist "%D.db" move "%D.db" "Samples\%D\"
move "Scripts\Convention-Stats.sql"   "Samples\NFB2026Convention\"
move "Scripts\ConventionSchedule.sql" "Samples\NFB2026Convention\"
move "Scripts\Daily-Schedule.dbdo"    "Samples\NFB2026Convention\"
move "Scripts\DayAtAGlance.sql"       "Samples\NFB2026Convention\"
move "Scripts\Marked-Schedule.js"     "Samples\NFB2026Convention\"
move "Scripts\Presenter-Events.sql"   "Samples\NFB2026Convention\"
move "Scripts\Speaker-Directory.sql"  "Samples\NFB2026Convention\"
move "Scripts\SpeakerSessions.sql"    "Samples\NFB2026Convention\"
move "Scripts\Sponsor-Showcase.sql"   "Samples\NFB2026Convention\"
move "Scripts\Topic-Track.dbdo"       "Samples\NFB2026Convention\"
move "Scripts\NorthwindRowCounts.sql" "Samples\northwind\"
move "Scripts\RecentOrders.dbdo"      "Samples\northwind\"
move "Scripts\WineDrinkWindow.sql"    "Samples\cellar\"
```

`lookups.db` and the four generic scripts stay where they are.

## A few notes

- **`lookups.db` is not a sample.** It's the shared valid-values store
  used across databases, so it stays in `{app}` (and at your repo
  root), out of `Samples` and out of the picker.
- **`NFB2026Convention.db` keeps its capitalized name.** It's
  referenced by name in code and predates the lowercase-database-name
  convention. Its folder matches the file root exactly
  (`Samples\NFB2026Convention\`), so the mapping stays trivial. New
  databases should use lowercase roots (the six new ones already do).
- **Managed-copy opens.** Per-database scripts are found beside the
  *real* `.db`. If you open a database as a managed temp copy (the
  opt-in Misc command), `db.filePath` points at the temp file, so its
  scripts won't be found. Normal Open (Ctrl+O) opens the real file, so
  this only affects that opt-in path.
- **Version** bumped to 1.0.115 for this build (carries the
  sort-persistence fix, the Sample Databases picker, and this layout).
- **Still open:** wiring the bundled lineup into the docs
  (`DbDo.md` Help-menu section, `Announce.md`). The installer and code
  are done; only the prose references remain, which I can update once
  you confirm the final lineup.
```
