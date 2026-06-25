# Learnings from DbDialog (Window-Eyes app, 2009)

DbDialog was your general-purpose Microsoft Access database manager that
ran as a global script inside the Window-Eyes screen reader (VBScript,
GW Toolkit, Homer Shared Object). It is the direct Windows ancestor of
DbDo, and the package confirms the lineage in detail: DbDialog's Contact
schema is exactly the field roster DbDo adopted, and its `Contact_Look`
field — a cached word-summary stored next to the numeric `Contact_ID`
so a relation reads as words rather than a number — is the direct
ancestor of DbDo's `look` column.

## Already carried forward (DbDialog -> DbDo)

The main-dialog command set maps almost one-for-one onto DbDo:

- Add / Modify / Copy / Remove -> the same record operations.
- Browse / View -> grid navigation and the Details view.
- Filter (with a leading comparison operator, else contains-for-Text)
  -> Filter Records.
- Go To (another table) -> Select / Open Table.
- Index (pick sort fields) -> Order Records / column sequence.
- Keywords / Next / Prior -> Find / Find Next.
- Output (say non-blank field names + values, copy to clipboard) ->
  the Output / Say family with row copy.
- Select (choose display fields) -> Select Columns (Alt+S).
- Tag / Untag, and the Extra-Tools "all in filter" variants ->
  marking, including global mark and filter-by-marked.
- Yield (count in filter and whole table) -> the `yield` command.
- Zoom (related records by foreign key) -> Enter Child / Say Related,
  the maps-aware traversal.
- =Lookup's core idea (relate by a human-readable summary, not a raw
  id) -> the `look` value shown in related-record lists and pickers.
- Add New Table wizard -> New Database / Add Table building the
  standard schema.
- Query with SQL / Help on SQL -> Invoke SQL and the dot prompt.
- Go to Different Database / Backup / Load in Access -> Open Database,
  Save As, and opening via the file's associated app.
- Custom Report templates in the config -> DbDo scripting (.sql/.js),
  now stored beside each database.
- Config-merge discipline (ship `.cfg` defaults, merge into the user's
  `.ini` only the keys that are missing, never clobber user choices,
  never replace the data file on update) -> DbDo's `DbDo.inix` user
  state, seed sentinels, and onlyifdoesntexist install flags.
- Status line of id / added / modified -> the status line and Say
  Status.

## Implemented now (v1.0.117)

**Insert Date / Insert Time into the focused field of the record
editor.** Both DbDo ancestors shipped this and DbDo lacked it: the
Contact Tracking System had F8 to stamp the current date into a memo,
and DbDialog had /Date (Alt-/) and :Time (Alt-Shift-;) buttons that
filled the focused field. In the Edit View form, DbDo now supports:

- **Control+Semicolon** -> insert today's date (`yyyy-MM-dd`)
- **Control+Shift+Semicolon** -> insert the current time (`HH:mm:ss`)

at the caret of the focused field, replacing any selection and speaking
what was inserted. The chords are the long-standing Excel date/time
shortcuts, so they are familiar muscle memory rather than something new
to learn. Crucially this lives in DbDo's own edit form (the
`applyFormViewSetup` conveniences, alongside Null Value and the
record-navigation chords), not in the shared EdSharp / FileDir text
control — which is why it sidesteps the family-consistency concern that
deferred the same idea earlier. Read-only fields ignore it.

## Learnings left as design decisions (not implemented)

- **=Lookup relate-picker in the edit form.** DbDialog's most powerful
  editing feature: on a foreign-key field, one key opened a minimal
  record browser to pick the related row, then filled both the id and a
  cached `*_Look` summary and advanced focus. DbDo has the pieces
  (`look`, `maps`, lookup pick lists) but not an in-editor "pick a
  related record, fill the key, show its look" flow. Genuinely useful,
  but a substantial addition to the central edit dialog and a schema
  question (cache a `*_look` column, or resolve through `maps`?).
- **Bulk "Modify All Records in Filter."** A form that applies new field
  values to the whole filtered set in one step (DbDo can do it via a SQL
  UPDATE, but not through a friendly form). Also its siblings: Import
  All / Export All / Remove All in filter. Useful, but a bulk-edit
  surface and a safety/confirmation design decision.
- **Config-defined report templates.** DbDialog reports were `.cfg`
  sections with Top / Line1..N / Record / Bottom expressions, `$field`
  substitution, and helper functions. DbDo's scripting already provides
  the capability by a different route; a declarative per-record report
  format would be a convenience layer over it, not a new capability.
- **Per-field Alt-Letter jump mnemonics in a long edit form.**
  DbDialog's Contact form gave every label a unique Alt-Letter so you
  could jump straight to Email or Gender instead of tabbing. DbDo's
  editor is dynamic over arbitrary tables, so auto-assigning collision-
  free mnemonics is fiddly; worth considering for long forms.

Say the word on any of these and I'll scope it.
