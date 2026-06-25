# DbDo Sample Databases -- Build Notes

Six hobbyist sample databases, each built on DbDo's standard schema
convention and validated for referential integrity. Regenerable from
the Python builders delivered alongside (`build.py` holds the shared
helpers; the rest each emit one or more `.db` files).

## The set

| Database      | Data tables (rows)                  | Association kinds        | Lookups |
|---------------|-------------------------------------|--------------------------|---------|
| `reads.db`    | books (14), authors (12), series (8)| written_by, in_series    | 19      |
| `recipes.db`  | recipes (12), ingredients (25)      | uses                     | 20      |
| `music.db`    | artists (12), albums (14)           | recorded, features       | 21      |
| `media.db`    | films (12), directors (10)          | directed_by              | 14      |
| `contacts.db` | contacts (11), groups (5)           | in_group                 | 6       |
| `howtos.db`   | articles (13), categories (6)       | in_category, relates_to  | 4       |

`reads.db` is the natural counterpart to Richard's book collection:
three nouns, two association kinds, and a `volume` field on the
`in_series` maps note so a series reads in order.

## Schema convention (every table)

`<singular>_id INTEGER PRIMARY KEY AUTOINCREMENT`, then `added` /
`edited` (`TEXTTIME`, default `CURRENT_TIMESTAMP`), the data fields,
then `notes` (`TEXTMARKDOWN`), `tags` (`TEXTMEMO`), generated `look`
and `unq`, and `marked` last. Each database also carries the three
infrastructure tables: `maps` (all inter-table associations),
`lookups` (valid-value lists), and `sqlean_define`.

`look` is the human-readable row label; `unq` is the identity key the
`maps` table points at. Both are `GENERATED ALWAYS AS (...) STORED`.
`contacts` uses the conditional `unq` shape: a person is
`first|middle|last`, an organization falls back to `enterprise`.

## Validation performed

- Every data table has the full standard skeleton and a
  `<singular>_id` primary key.
- `look` / `unq` are present as generated columns (`table_xinfo`
  hidden flag 3 = STORED) and populate for all rows -- no blank `unq`.
- Maps referential integrity: all 168 association endpoints across the
  six databases resolve to a real `unq` in the named table. Zero
  dangling references, so Enter-Child and Say-Related have a live
  target on every association.

## One unrelated note for DbDo.cs

While checking generated-column flags I confirmed SQLite 3.45.1
reports `table_xinfo.hidden` as **3 for STORED** and **2 for VIRTUAL**
generated columns. The calculated-column cache comment in DbDo.cs has
those two labels swapped ("2 = stored-generated, 3 = virtual-
generated"). No behavior changes -- the code hides both 2 and 3 as
calculated, which is correct -- but the comment is worth fixing so the
next reader isn't misled.

## Not yet done (your call)

These are built and validated but not wired into DbDo: no Help-menu
open commands, no `DbDo_setup.iss` `Source:` lines, no `DbDo.md`
paragraphs. That ties into the bundled-lineup curation you were
weighing (the flagship + standard-reference + distinct-domain set,
with the redundant music collection flagged to cut). Say which of the
six earn a slot and I'll do the full wiring pass -- menu command,
installer line, and docs -- so nothing dangles.
