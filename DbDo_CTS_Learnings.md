# Learnings from the Contact Tracking System (CTS, 1993)

CTS was your DOS, FoxBase-compiled, speech-friendly contact/mailing-list
manager. Reviewing its documentation, the striking finding is how much
of its design DbDo already carries forward — the lineage is real and
direct. This note records what was inherited, the one learning just
incorporated, and the learnings left as design decisions.

## Already carried forward (CTS idea -> DbDo equivalent)

- **Speak the right thing through any screen reader's defaults.** CTS:
  "the most relevant information on the screen is spoken automatically
  using the default settings of any screen reader." DbDo realizes this
  with the UIA live-region announcement path that JAWS, NVDA, and
  Narrator all listen for — no custom scripts required.
- **Scan all fields for a string** -> **Find** (Ctrl+F), "search across
  all columns for a substring."
- **Filter on specific fields** -> **Filter Records** (Alt+Shift+F).
- **Tag / untag / global tag, and filter to tagged** -> marking
  (Set Mark, Toggle Marked, Invert Marked, and the marked filter).
- **Yield** (count records, or the filtered set) -> the `yield`
  command, inherited by name.
- **Next / Previous / Beginning / End / Jump** navigation -> the same
  navigation verbs.
- **List mode** (brief one-line-per-record, arrow to move, spoken) ->
  the grid / listview and pick lists.
- **Examine** (cursor on a field value for character/word review) ->
  the Say family plus the virtual cursor.
- **F2 picks a valid value** -> F4 / Alt+DownArrow pick-list lookups
  from the builtin `lookups` table.
- **Retrieve by key with prefix matching** ("maz" finds "Mazrui") ->
  Find / type-ahead.
- **Per-record memo field** -> the `notes` column (multi-line editor).
- **Upsert on a key match** (Transfer's Update/Replace on same key) ->
  the `unq` column, designed precisely so a row can be updated when its
  identity matches rather than duplicated.
- **Single-key menus with the selected option described on the status
  line** -> the menus plus the status line, Key Describer (Ctrl+F1),
  and Alternate Menu (Alt+F10).
- **Per-user config profiles** (`cts jane`) -> per-user settings in
  `%APPDATA%\DbDo`.

## Incorporated now (v1.0.116)

**Per-field contextual help — CTS's "F1 = help on current field."**
CTS shipped a help screen for every field giving its purpose, format,
and an example. DbDo's edit dialog already spoke a field tip on
Shift+F1 (type, read-only, regex constraint, lookup availability,
Markdown). It now leads that tip with the field's own documentation,
drawn from the table's `---` schema-doc comments via `getSchemaDoc`.
So on a documented table, Shift+F1 in Edit View / New Record / Filter
now speaks, for example, "When the row was created -- type datetime."
Fields whose column has no doc comment are unchanged, so nothing
regresses on the undocumented sample databases. This reuses DbDo's
existing schema-documentation feature rather than adding a parallel
mechanism.

## Learnings left as design decisions (not implemented)

These are real CTS capabilities, but each is either a substantial new
subsystem or a choice only you should make for a general relational
manager rather than a mailing-list tool:

- **Correspondence and label generation.** CTS could address an
  envelope or letter to the current contact (return address, date,
  inside address, formal/informal salutation, closing), produce
  1-/2-/3-across mailing labels and return-address labels from a
  filtered set, and generate a WordPerfect mail-merge secondary file.
  The general-purpose analog for DbDo would be template-driven document
  and label output from selected/marked rows. This is the largest and
  most useful gap, but it is a new output subsystem and a format/design
  decision (which template engine, what output format).
- **An activity-log model with a one-key "Zoom" to related history.**
  CTS paired each contact with STATUS records (a typed, dated activity
  log — Should/Have/Will/Did call/meet/write) reached by pressing Z,
  which auto-started a new one when none existed. DbDo can already model
  this with a log table plus `maps`, and Enter Child already jumps to
  related rows; the only novel touches are the typed should/have/will/
  did vocabulary and the "start adding one if none exist" shortcut.
  Domain-specific; offered, not assumed.
- **Transfer with a side-by-side field-level merge.** When a copied row
  collided on key, CTS offered Update / Replace / Display (side-by-side)
  and let you merge address vs. phones vs. the whole record. DbDo has
  the core idea (upsert via `unq`); the field-level merge UI is a
  refinement.
- **Insert-current-date keystroke while editing** (CTS's F8 in the
  memo, to stamp when a note was made). Small and useful, but DbDo's
  text-field conveniences are shared with the EdSharp / FileDir family,
  so adding a keystroke is a family-consistency and chord-choice
  decision rather than a unilateral change. Worth doing if you pick the
  chord.

Say the word on any of these and I'll scope it.
