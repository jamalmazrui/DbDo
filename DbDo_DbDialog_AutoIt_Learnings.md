# Learnings from DbDialog (AutoIt, 2006-2015)

This is the AutoIt incarnation of DbDialog — an accessible, general-purpose
database manager built on standard Windows controls and ADO, supporting
Access, MySQL, and dBASE. It is the closest ancestor to DbDo in spirit and
architecture (standard controls + ADO + a relational model), and most of
its design is already present in DbDo. The notable new material is its
import/transfer machinery, which this turn revives.

## Configuration model (mostly already in DbDo)

Each table is configured by a section of keys that DbDo has equivalents for:

- `ConnectString` (per table/format: Jet/ACE for Access, ODBC for MySQL,
  dBASE driver) -> DbDo's per-extension ADO connect strings.
- `CreateList` (schema with ADO types) -> DbDo's CREATE with standard fields.
- `InputList`, `StatusList`, `LabelList`, `SelectList`, `IndexList`,
  `LookList`, `MemoField` -> DbDo's input/edit forms, status line, Select
  Columns, Order Records, the generated `look`, and the multi-line memo.
- `PickList` (`ARTIST_ID:Contact.ID`) -> the foreign-key relate source.
- `ZoomList` (`ID:Album.ARTIST_ID`) -> Enter Child / Say Related traversal.
- `*_LOOK` cached columns beside each `*_ID` -> DbDo's `look`, the same idea
  (store/show a human-readable summary of a relation, not a bare number).

The Contact schema in this `.ini` is, once more, exactly DbDo's adopted
roster — the lineage is unmistakable.

## The transfer.ini import model (this is the valuable new material)

DbDialog could open a foreign file (a dBASE `cts.dbf`, an Access table, a
MySQL table) as a source and copy its records into a destination table,
mapping fields by a `transfer.ini`. Each line was:

```
destField = sourceField [ ; expression ]
```

with `$v` bound to the source value, AutoIt expressions for transforms
(e.g. reformatting a packed `YYYYMMDD` date), an empty-source skip, and
repeated destination keys concatenating. The shipped example mapped a
legacy CTS dBASE file into the Contact table — closing the loop across all
three generations of the tool (DOS CTS -> DbDialog -> DbDo).

## Implemented in DbDo (v1.0.118): the .inix transfer map

DbDo already had an `.inix` format, but only in its *record* form (each
section is a row). This turn adds the *transfer-map* form and a command to
run it, reusing the existing `InixCodec` parser (which already preserves
order, duplicate keys, comments, and fenced values):

- **File > Transfer Import.** Open the destination database, pick the
  destination table, then choose a transfer `.inix`, a section, and a
  source file (`.dbf`, `.mdb`/`.accdb`, `.db`). DbDo opens the source on a
  secondary ADO connection, maps each row, and inserts via the same
  per-row AddNew/Update path the JSON/Markdown importers use, so one bad
  row never sinks the batch.
- **The map.** `destField = sourceField [ ; jscript-expression ]`, with
  `@table = <name>` to name the source table (default: file base name).
  `$v` is the source value, `$other_field` any other source field. Empty
  source values are skipped; a repeated destination field concatenates.
- **JScript .NET expressions.** Rather than invent an expression language,
  the transform runs through `Microsoft.JScript` at run time — the same
  engine the build already uses for snippets. So slicing, conditionals,
  and arithmetic are all available (`$v.substr(0,4)`,
  `$v == "Y" ? "Yes" : "No"`, `Number($v) * 100`). A failed expression
  falls back to copying the value unchanged. The build now passes
  `/reference:Microsoft.JScript.dll`; no new runtime dependency, since
  JScript .NET ships with the Framework.

The parser, the same-destination accumulation, the empty-source skip, and
the date-reformat expression were validated in Python against the real
legacy `transfer.ini` before the C# was written. A ready-to-use
`transfer.inix` (the CTS-to-contact map, in JScript form) ships alongside.

## Left as design decisions (not implemented)

- **More source formats** (CSV, Excel) for Transfer Import. Straightforward
  to add once you want them; the current set covers the relational legacy
  sources the feature is for.
- **Export side / "Modify All in Filter".** DbDialog used the same mapping
  engine to push records out and to bulk-edit a filter. The import side is
  the most useful first cut; the others can reuse the same map machinery.
- **Upsert on import.** The current import appends (like the JSON/Markdown
  importers). Matching on `unq` to update-or-insert would be a natural,
  optional enhancement.

Say the word and I'll extend any of these.
