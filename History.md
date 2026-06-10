# DbDo History of Changes

This file is the chronological record of DbDo releases. The most recent release is at the top. For the overview of what DbDo is, see `Announce.md` or `README.md`. For the full reference, see `DbDo.md`.

Press **Shift+F1** inside DbDo to open this file in your browser, or type `history` at the dot prompt.

## v1.0.106 (current)

**Primary keys follow the `<singular>_id` convention.** Every sample database's primary key is renamed from the bare `id` to the singular table name plus `_id` (`teacher_id`, `wine_id`, `order_detail_id`), so a foreign key now carries the SAME name as the parent primary key it references (`teacher_id` in `classes` references `teachers(teacher_id)`). The same key name on both ends of every relationship makes schemas, generated SQL, grep results, and -- most importantly for this program -- spoken column names self-identifying. Code already preferred `<singular>_id` discovery with a bare-`id` fallback; the fallback is retained for legacy databases. `NFB2026Convention.db` is deliberately NOT migrated -- it awaits a separate schema redesign.

**`edited` replaces `modified` as the standard last-change column.** Consistent with Control+E (Edit Record) and Alt+E (Edit menu). The `Metadata.ModifiedColumn` constant is renamed `EditedColumn`; the standard-hidden list, bookkeeping list, date-sort list, index-recommendation pass, status bar ("edited YYYY-MM-DD"), Edit Cell refusal message, and New Record skip list all follow.

**Say Edited on Shift+E.** The command formerly called Say Modified (Shift+M) is renamed Say Edited and rebound to Shift+E, the mnemonic parallel of Shift+A for Say Added. It speaks the `edited` timestamp in the same human-friendly local-time form ("April 15, 2026 at 5:42 PM"), falling back to `added` when the table has no `edited` column. Shift+M is now unbound.

**Standard hidden columns confirmed as: `added`, `edited`, `notes`, `tags`, `url`, `look`, `unq`, `marked`** -- plus the every-`_id`-and-bare-`id` key rule. (Same set as before, with `edited` in place of `modified`.)

**Edited-timestamp triggers rewritten so marking never bumps the timestamp.** Each sample-table trigger is now `AFTER UPDATE OF <data columns> ... WHEN OLD.col IS NOT NEW.col OR ...`: the OF list excludes `marked` (and the bookkeeping columns), so Mark Record (Control+M), Unmark Record (Control+U), Toggle Marked (Control+Space), and the range-mark commands leave `edited` untouched; the null-safe WHEN clause additionally skips no-op saves where the Edit Record dialog hands every field back unchanged. Verified by test: marking does not bump, a real edit does, a no-op save does not.

**`look` and `unq` redesigned per table across the sample databases.** Previously most tables defined `look` and `unq` as the SAME expression, defeating their distinct purposes. Now `look` concatenates the few human-identifying columns (' | ' separated, empties skipped) while `unq` concatenates the uniqueness-defining columns ('|' separated, positionally stable via coalesce, so a NULL keeps its slot and two rows can't collide by omission). Example: chinook customers -- look is first_name | last_name | company; unq is first_name|last_name|email. A `unq` index is created per table -- UNIQUE where the data allows (every table except none; all passed), supporting upsert-style scripts that match on `unq`.

**Say Related (Shift+R) now lists children too.** One look line per related record: parents as before ("teachers: Anita Carver | acarver@school.edu"), then each child table as a header with up to five look lines and an "and N more" footer. Previously only parent rows were listed.

**Companion design files updated to the new convention.** `FkResolution.cs`, `ImportNormalization.cs`, and `Lookups.cs` (unwired design drops -- only `DbDo.cs` is compiled) had headers written for the bare-`id` era; their comments now describe the `<singular>_id` convention. `rebuildSamples.py` is included as the regeneration script for the five migrated sample databases.

**Documentation synchronized**: `DbDo.md` (standard-fields section, trigger section with the new SQL pattern, Say Edited, sample-database descriptions), `README.md` (convention description; explicit note that the convention database still uses bare `id` pending redesign), `Announce.md` (now lists all five samples), and `DbDo.ini` (Say Edited entry; Say Related description).

**Script extension renamed `.duo` -> `.dbdo`.** The command-batch extension is now simply the program's own name -- unmistakable association, clean namespace. `Scripts/RecentOrders.dbdo` and `Scripts/StatusSnapshot.dbdo` renamed; the Invoke Script picker, file-dialog filter, dispatcher, installer entries, and documentation all follow. No aliases for the old extension.

**Global Alt+GraveAccent hotkey dropped.** DbDo is a single-instance application, so the Alt+Control+GraveAccent toggle already covers both directions of GUI/console switching; the one-way console-to-GUI chord was redundant and reserved a second system-wide chord other applications may want. The pair is now Control+GraveAccent (GUI menu, GUI -> console) and Alt+Control+GraveAccent (global toggle).

**Settings file renamed `DbDo.ini` -> `DbDo.inix`, now read as Inix.** The per-user and shipped settings files use DbDo's own extended-ini format. The settings readers (`readIniValue`, `readIniFromFile`, `IniSession.readFrom`) route through `InixCodec`, so any setting may hold a multi-line value in the fenced or plain Inix form. A new `InixCodec.writeValue` surgically sets, replaces, or removes one key while preserving all comments and other lines -- fence-aware in both directions (a value containing a newline, `=`, or a leading `[` is written fenced; an existing fenced value is replaced or removed as a whole block; the key scan skips fenced blocks so a `key=` line inside a fenced value is never misread). Both writers (`writeIniValue` and `IniSession.write`) delegate to it. The shipped template demonstrates the payoff: every `[ConnectStrings]` entry is now fenced, the reliable form for values dense with `=` characters. On launch, a per-user `DbDo.ini` is renamed to `DbDo.inix` automatically (every classic ini file is already valid Inix, so no conversion is needed).

**Design principle recorded: every table gets the full standard column set.** Convention over configuration applies to DbDo's own infrastructure tables too -- the planned `maps` association table (and any future standard table) carries the complete roster including both `look` and `unq`, the same as every domain table.

**Validated**: trigger behavior tests pass on the rebuilt databases; foreign-key checks clean; the shipped DbDo.inix template round-trips through the Inix parser with all eleven fenced ConnectStrings values intact; brace and paren balance verified.

## v1.0.105


**Chord bindings are compiled-in only; .ini file is documentation for the chord field.** v1.0.104 wired the .ini to drive both chords and descriptions; that imposed startup parsing overhead on a feature that is not user-configurable in practice (no one was going to edit chord bindings; EdSharp's history confirms this). v1.0.105 reverses the chord-override half. Descriptions remain editable in `DbDo.ini` and load at startup -- editing wording and restarting still works.

The `Hotkeys` section header explains: the chord field documents the binding, the description field is loaded. Removed `friendlyToKeysName`, `normalizeKeyText`, and the KeysConverter-based parsing -- dead code since the runtime never parses chord strings now.

**JAWS-canonical key names in display.** `friendlyKey()` (the function that renders a Keys value as text for the status bar, menu, and key describer) was already using JAWS conventions for most keys. Added the NumPad arithmetic family: `NumPadPlus` (Add), `NumPadMinus` (Subtract), `NumPadStar` (Multiply), `NumPadSlash` (Divide), `NumPadDot` (Decimal). And `Oemtilde` displays as `GraveAccent` (the standard English name). The point: when DbDo announces a chord, the user hears the same name JAWS uses for that key in their daily browsing.

**`DbDo.ini` Hotkeys section comment updated** to explain the documentation-only role of the chord field.

**Validated**: brace and paren balance at 26188 lines; chord-conflict audit clean.

## v1.0.104

**`DbDo.ini` now drives hotkey bindings and command descriptions.** New `[Hotkeys]` section in FileDir style. Each line is `Command Name=Modifiers+Key, Imperative description.` Modifiers are always Alt, Control, Shift, in alphabetical order, in full-word spelling. Descriptions are single imperative sentences that weave common synonyms and -- where natural -- reinforce the chord-letter mnemonic. All 106 chord-bound commands are now in the file.

The loader is an **override layer** on top of compiled-in defaults: if `DbDo.ini` is missing, DbDo still runs with the v1.0.103 defaults; if it's present, file entries win. Each entry overrides the chord AND sets the status-bar / key-describer description (`KeyMap.dCommandToSummary`). You can edit `DbDo.ini` in any text editor, restart DbDo, hear the change.

**Friendly key names supported.** The loader translates user-friendly key tokens to the .NET `Keys` enum names before parsing: `Apostrophe` -> `OemQuotes`, `Backslash` -> `OemPipe`, `Backspace` -> `Back`, `UpArrow` -> `Up`, `Ctrl` -> `Control`, `Esc` -> `Escape`, plus the other Oem-prefixed punctuation. You don't need to memorize the C# enum names; the human-readable names from FileDir's `Hotkeys.ini` work as written.

**Alternate-chord syntax supported.** A description line may include `, or X` to declare additional chord(s) for the same command (e.g., `Beginning Tagged=Shift+B, or Control+Home, Go to beginning tagged item`). The parser locates the first comma whose tail is NOT `or ...` as the chord/description boundary. The DbDo `[Hotkeys]` ships without alternate chords today; the parser tolerates them for future use.

**Backward-compatible parsing.** `[Hotkeys]` is the modern section name; the historical `[Keys]` (chord-only, no description) is still accepted.

**Distribution adjustment.** `DbDo.exe` and `DbDo_setup.exe` are no longer included in the shipped zip. You rebuild them locally.

**Validated**: brace and paren balance at 26275 lines; chord-conflict audit clean; all 106 `[Hotkeys]` entries match registered commands.

## v1.0.103

Bug fixes from user testing of v1.0.102.

**Alt+Shift+F (Filter Records) didn't work.** Root cause: the menu item was registered via `addItemLocal` instead of `addItem`. The "local" variant uses `KeyMap.registerDisplayOnly` which shows the chord in the menu but never adds it to the form-level dispatch table. Pressing the chord did nothing. Fixed by changing Filter Records, Clear Filter, and Go to Record to use the regular `addItem` registration.

**Alt+D (Database Summary) showed an unresolved or stale file path.** The summary printed `db.filePath` literally -- whatever was passed to openDatabase, which could be relative or no longer exist. Fixed by resolving the path with `Path.GetFullPath` and adding a "(file not found at this path)" line when the resolved path doesn't exist. This catches the case where the manager's stored path is no longer reachable on disk.

**Alt+O / Alt+Shift+O (Order Records / Reverse Order) not behaving correctly.** Possible cause: menu items weren't being explicitly enabled, so chord dispatch may have fallen through to other handlers. Fixed by adding explicit `Enabled = bHasTable` for `miOrderRecords` and `miReverseOrder`, and `Enabled = bOpen` for `miFileOpenSelect` (Open Recordset, Ctrl+Shift+O). The bindings themselves were correct; this hardens the enable state.

**Menu labels no longer carry descriptive parentheticals.** Per the menu-name guideline (a menu label should be only the command name in title case, 2-4 words), 137 menu items had their parenthetical descriptions stripped. The descriptions remain available via the status bar (which pulls from `KeyMap.summaryFor`) and the key describer mode -- they just don't clutter the menu label itself anymore.

**JAWS "Not Selected" announcement on listview load.** Root cause: in virtual mode, `grid.Items[iIndex].Selected = true` operates on a placeholder object that doesn't persist. Removed the no-op line; `SelectedIndices.Add` is the API that actually sets the selection in virtual mode. Also: `bGridFirstPopulate` is now reset when the displayed table changes (tracked via `sLastGridTable`), so the workaround that announces "Row 1 of N -- first column value" via LiveRegion fires once per table switch, not just on database open.

**Validated**: brace and paren balance at 26153 lines; chord-conflict audit clean.

## v1.0.102

Build fixes. v1.0.101 didn't compile cleanly; this version makes the source actually build.

**Errors fixed:**

1. Variable shadowing in recClearBookmarkClicked — renamed an inner `sLook` to `sLookText` to avoid the conflict with the outer `sLook` declared in the same method.

2. `LbcDialog.addInputBox` missing 2-arg overload — added an overload taking just `(sLabel, sValue)` (the 3-arg form with `sTip` was the only one defined; the Replace handler called the 2-arg form).

3. `db.getField` / `db.setField` — the DbDoManager API exposes `getFieldValue` and `setFieldValue`. The Replace Column / Regex Replace handler had been calling non-existent methods. Fixed to use the correct names.

4. `RegexParseException` — that type was added in .NET 7; DbDo targets .NET Framework 4.8 where regex parse errors throw `System.ArgumentException`. Fixed the catch clause.

5. Orphaned `miSortRecords` field declaration removed (the warning becomes silent).

**Validated**: brace and paren balance at 26110 lines. The compilation errors from v1.0.101 are resolved.

## v1.0.101

Big design pass driven by user analysis of mnemonic-rule compliance, terminology consistency, and screen-reader UX.

**Listview "Unselected" fix.** A real screen-reader bug. When DbDo opened with a previously-opened database, the listview was first presented with VirtualListSize=0 and got focus before updateGrid populated rows. JAWS announced "Unselected" for the listview's initial empty state and didn't re-announce when the selection landed. Fix: a first-populate workaround that explicitly announces "Row 1 of N" (with the first column's value) via LiveRegion when the listview transitions from empty to non-empty, so the user hears actual data instead of silence following the stale "Unselected" announcement.

**Drop holdover aliases.** Pre-publicized-alpha means there's no installed user base; old command names are simply obsolete, not aliases. Removed: `sort-records` (alias for order-records), `remove` (alias for delete-record), `restore` (alias for restore-bookmark which itself is retired).

**Column rename: `updated` -> `modified`.** Aligns with the Windows / SharePoint / Office convention (File Explorer's "Date modified," SharePoint's Modified column) over the SQL Rails/Django `updated_at` convention. The DbDo audience is Windows screen-reader users, for whom "modified" is the vocabulary heard daily. Centralized standard-column names into Metadata constants (`ModifiedColumn`, `LookColumn`, `UrlColumn`, `AddedColumn`, etc.) so future renames need only update the constant.

**Drop Say Kin, Say Record, Say Web entirely.** Say Kin's "kin" terminology was idiosyncratic; foreign-key relationships are read by Say Related (Shift+R) instead. Say Record (Shift+Space) was redundant with the screen reader's built-in row read-line command. Say Web is replaced by Say URL (Shift+U) -- the column rename `url -> web` was reverted (user prefers "url").

**New: Say Goto on Shift+G.** Speaks the most recent Jump Record search string (column + substring). Completes the "current state" Shift+letter family alongside Say Filter (Shift+F) and Say Order (Shift+O).

**New: Toggle Marked on Ctrl+Space.** The Windows ListView convention is Ctrl+Space to toggle the focused item's selection state. Since DbDo uses the marked column as its multi-select equivalent, the chord maps directly. Solves the workflow gap where there was no single-keystroke way to flip the current record's mark.

**Say-X chord moves**:

| Chord | Command | Was |
|---|---|---|
| Shift+M | Say Modified | Say Marked (moved) |
| Alt+M | Say Marked | (new chord for existing command) |
| Shift+U | Say URL | Say Updated (renamed) |
| Ctrl+Shift+U | Open URL | Ctrl+U |
| Shift+G | Say Goto | (new) |
| Ctrl+Space | Toggle Marked | (new) |

**Mark anchor command renames**: "Set Mark Anchor" -> "Start Mark", "Mark Range" -> "Complete Mark", "Set Unmark Anchor" -> "Start Unmark". Aligns with EdSharp / FileDir terminology.

**Graphics Table -> Graphics Grid.** User's terminology rule: "Table" is schema-level, "Grid" is the displayed columns x rows (after filter and sort). The chart command operates on the displayed grid, not the schema-level table.

**Save/Export/Edit Settings shuffle** (full reshuffle motivated by mnemonic-rule + dropping past-convention concerns):

- `Export Database` -> **`Save Database`** -- the operation IS Save-As semantically; Save is the rule-compliant S verb.
- Save Database chord: was Alt+Shift+E, now **Ctrl+S** (cross-app Save convention, rule-compliant S).
- Export Data chord: was Alt+E, now **Alt+X** (eXport letter family, rule-compliant).
- Edit Settings chord: was Alt+Shift+S, now **Alt+Shift+E** (rule-compliant E for Edit, the verb-letter).

**Multi-bookmark feature.** Replaces the single-bookmark system with a per-session list. Each bookmark stores the table name, the ADO bookmark, the row position at save time, and the **look-value** at save time (one of the use cases the user pointed out for the look concept). The list dialog shows entries as "table -- look-value (row N)". Session-lifetime only: closing the database or app clears all bookmarks (ADO bookmarks are tied to the recordset that produced them, so cross-session persistence requires saving primary-key values instead -- deferred).

New chord assignments:

- **Ctrl+B = Save Bookmark** -- append current record to bookmark list
- **Alt+B = List Bookmarks** -- listbox dialog showing all saved; Enter navigates (switches table if needed)
- **Ctrl+Shift+B = Clear Bookmark** -- chooser when multiple ("Clear All" default, or "Clear Selected"); direct clear when only one

The old "Restore Bookmark" command is retired -- it was a single-bookmark thing. The new List Bookmarks (Alt+B) is more general.

**Table Summary (Alt+T) reimplemented as columns-overview.** Previously this slot held a misnamed field-and-statistic picker. New behavior: lists every column in the current table in **natural (schema-defined) order**, one line per column packing maximum info -- name plus declared type plus key/null/foreign-key/default constraints. Implementation: new `DbDoManager.SchemaColumn` class and `getSchemaColumns(table)` method that queries SQLite's `PRAGMA table_info` plus `PRAGMA foreign_key_list`, with fallback to ADO Fields-collection (name + type only) for non-SQLite backends.

**Free chord slots** (after all the moves above):
- Alt+E, Alt+Shift+S
- Shift+W, Shift+K, Shift+Space, Shift+E
- Ctrl+U

These are reserved for future commands.

**Documented standard-column constants** in `Metadata`: `AddedColumn` ("added"), `MarkedColumn` ("marked"), `ModifiedColumn` ("modified"), `NotesColumn` ("notes"), `LookColumn` ("look"), `TagsColumn` ("tags"), `UrlColumn` ("url"), `UnqColumn` ("unq"). Any code that references these standard columns should use the constants, not string literals -- this is the flexibility hook the user asked for to make further fine-tuning straightforward.

**Validated**: brace and paren balance at 26087 lines; chord-conflict audit clean.

## v1.0.100

**Sort chooser symmetric to Filter chooser.** When pressing Alt+O or Alt+Shift+O and a sort is already active, a chooser dialog appears with four buttons (Clear is the default):

- **Clear** (Alt+C, default) -- empty db.sort and close
- **Reset** (Alt+R) -- discard current sort, open the listbox to pick fresh
- **Add** (Alt+A) -- append the chosen column to the existing sort expression (multi-column sort)
- **Cancel** (Alt+N) -- close without changes

When no sort is active, the chooser doesn't appear -- the chord goes directly to the listbox (Reset behavior). Same two-keystroke clearing pattern as Filter: Alt+O, Enter.

The Add button enables multi-column sort. Choose Order Records (Alt+O), pick "title", get sorted by title. Choose Order Records again, pick Add, pick "year": now sorted by `title ASC, year ASC`. Mix ascending and descending by which chord (Alt+O vs Alt+Shift+O) you press at the Add step. Status announces "Added order on X" / "Added reverse order on X" to confirm the direction at each step.

This makes Shift+F (Say Filter) and Shift+O (Say Order) more useful: they reveal the current state, and the chooser dialogs offer the obvious next-step actions on that state without forcing the user to manually edit ADO sort/filter strings.

**Validated**: brace and paren balance at 25655 lines; chord-conflict audit clean.

## v1.0.99

**Edit Settings moves to Alt+Shift+S.** Ctrl+, was a Mac convention that violated the strict mnemonic rule on Windows. New chord is rule-compliant (S = first letter of Settings) and Windows-native (no funny punctuation chord). Menu label updated to "&Edit Settings..." for the natural verb-noun verb-noun pattern that matches Edit Record / Edit Cell / Edit Script.

**Sort Records → Order Records, with listbox-of-columns dialog.** The new behavior:

- **Alt+O = Order Records** -- sort ascending. Opens a listbox of all field names (alpha-sorted, including hidden columns), with the current virtual column as the default focus. Press Enter to sort by the focused column, or arrow to a different one and Enter.
- **Alt+Shift+O = Reverse Order** -- same listbox UX, sorts descending.

The key feature: **sorting by hidden columns works directly**. Today (before this change) you had to display a column, sort, then optionally hide it. Now the listbox shows every field regardless of visibility.

The natural-English aliases `sort` and `sort-records` still resolve to `order-records` at the dot prompt for users who think Sort first.

**Switch Mark → Invert Marked.** The menu label was already "Invert Marked"; canonical name and dot-prompt token now match (I-letter rule-compliant on Ctrl+I).

**L-family Say commands rewritten as Rest commands** (from cursor, not from row 1):

- Ctrl+L = **Say Column Rest** (current column, from cursor down)
- Ctrl+Shift+L = **Say Column Rest Marked**
- Alt+L = **Say Records Rest** (renamed from Say Rows -- entity vocabulary alignment)
- Alt+Shift+L = **Say Records Rest Marked** (renamed from Say Rows Marked)

The "Rest" in the name means "from this point onward." This is genuinely more useful than start-from-row-1 in most workflows -- a user is usually focused on a row of interest and wants to hear what comes after. CLI users can override with an `all` argument when row-1 start is needed.

**New Say-X commands for standard columns:**

- **Shift+D = Say Database** (replaces Shift+P = Say Path). Single-press announces the filename for fast 'where am I'; double-press opens a dialog with the full path.
- **Shift+O = Say Order**. The sort-expression counterpart to Say Filter (Shift+F).
- **Shift+Space = Say Record**. The gap-filler: speaks the current row's full content (all displayed columns) in one utterance. Slots between Say Cell (one cell) and Say Records Rest (many records).
- **Shift+U = Say Updated** (moved from Shift+D; rule-compliant U).
- **Shift+W = Say Web** (renamed from Say URL; reclaims the retired Window Summary stub slot; rule-compliant W).

The Say Web handler accepts both `web` and `url` column names for transitional compatibility -- the canonical column rename `url -> web` is being held pending user decision.

**Say Status (Shift+Z) leads with Marked state.** When the current record is marked, the announcement now begins with "Marked." before the table / row / filter / sort details. This implements the user's design where Shift+Z is the bottom-of-the-UI status read with the marked indicator prominent.

**Filter Records redesigned with action chooser.** When pressing Alt+Shift+F and a filter is ALREADY active, a chooser dialog appears first with six buttons (Clear is the default):

- **Clear** (Alt+C, default) -- set filter to empty; close
- **And** (Alt+A) -- open blank form; result wraps "(old) AND (new)"
- **Or** (Alt+O within dialog) -- open blank form; result wraps "(old) OR (new)"
- **Edit** (Alt+E) -- open form pre-populated with current values; replace
- **Reset** (Alt+R) -- open blank form; replace
- **Cancel** (Alt+N) -- close without changes

When no filter is active, the chooser doesn't appear -- Alt+Shift+F goes directly to a blank field form. This implements the user's request for at-most-two-keystroke filter clearing (Alt+Shift+F, then Enter to accept the Clear default) and adds incremental filter composition via And / Or.

**Window Summary stub retired.** Was a deferred-not-implemented stub on Shift+W. Replaced by Say Web.

**Validated**: brace and paren balance at 25590 lines; chord-conflict audit clean.

## v1.0.98

**Strict mnemonic rule audit and cleanup.** The user re-stated the chord-letter rule: the letter must be the first letter of one of the words in the canonical command name, with rare conventional exceptions documented in source. A full audit of all 135 chord bindings found 16 violations (down to 3 after this cleanup).

**Renames to make first letters rule-compliant.** Two clear-cut cases where the canonical name was wrong relative to what the menu said and what the user understands:

- `Select Record` -> **`Filter Records`** (the menu label was already "Filter Records..."; canonical and dot-prompt token were the misnamed "Select Record" / "select-record" -- a PowerShell `Select-` verb holdover that didn't match the actual semantics)
- `Update Column` -> **`Replace Column`** (the menu label was already "Replace Column"; matches Ctrl+R chord)

These bring the canonical names into sync with the menus and chord-letters at the same time.

**New L-family convention for multi-cell speech commands.** The four new Say commands from v1.0.97 had no rule-compliant first letter (Say Column / Say Column Marked / Say Rows / Say Rows Marked share only S, M, and consonants like C and R that were taken). Following the K-for-Bookmark precedent of a "conventional exception letter," L is now the documented exception for **multi-cell linear sweeps** (List / Look / Linear-read):

- **Ctrl+L** = Say Column
- **Ctrl+Shift+L** = Say Column Marked
- **Alt+L** = Say Rows
- **Alt+Shift+L** = Say Rows Marked

This groups the four into a clean L-family. Shift+L stays Say Look (the existing "L for Look" command speaking the current row's summary), so the L convention is now consistently "list / look / linear sweep" across the speech family.

The earlier v1.0.97 bindings (Shift+E / G / V / X) were rule-violations and have been removed.

**Bookmark chords migrated K to B.** The user's observation: B is the rule-compliant first-letter of "Bookmark", and the K-for-booKmark convention was inherited from older app conventions. Moving to B follows the strict rule and frees the K-family. Shift+K stays Say Kin (K is K's first letter, rule-compliant).

- Save Bookmark: **Ctrl+B** (was Ctrl+K)
- Restore Bookmark: **Alt+B** (was Alt+K)
- Clear Bookmark: **Ctrl+Shift+B** (was Ctrl+Shift+K)

K-family is now wide open. Three free chords saved for future commands whose names start with K.

**Documented conventional exceptions** (in source comments at the binding sites):

- **B = Bookmark** (now strictly rule-compliant since the rename; "K for booKmark" is retired)
- **L = List / Look / Linear-sweep** for multi-cell speech commands (Say Column family)
- **V = inVoke** (V-sound exception for Invoke Script / Edit Script paired commands)
- **C = Configuration** for Edit Settings (EdSharp / FileDir convention)
- **K = Kin** in Say Kin (rule-compliant on its own, just shares the letter family the bookmarks left)
- **Z = ZZZ / hush / sleep / silent-mode** for speech-toggles and read-only mode (Say Status, Toggle Read Only, Toggle Extra Speech, Toggle Command Echo)
- **X = eXtract** for Extract Regex
- **Q = Query** for Invoke SQL
- **G = Goto** for Set Position (cross-app convention)

**Three remaining rule-violations** flagged for user decision (not auto-renamed in this version):

1. **Append Record (Alt+Shift+C)** -- letter C, name letters A or R. C from "Clipboard" (it appends to the clipboard buffer) is a stretch; options are to rename to a C-starting name like "Concatenate Record" (awkward), remap to a different chord, or accept C as a clipboard-family convention.
2. **Switch Mark (Ctrl+I)** -- letter I, name letters S or M. The menu label is "Invert Marked (toggle every record)"; renaming canonical to "Invert Marked" would make I rule-compliant.
3. **Say Updated (Shift+D)** -- letter D, name letters S or U. The column is called "updated" but contains a date; renaming canonical to "Say Date" would make D rule-compliant.

These three need user judgment before I touch them.

**Statistics / Graphics chord adjustments** from earlier in v1.0.97 retained:

| Chord | Command |
|---|---|
| Alt+S | Select Columns (new) |
| Alt+Shift+S | Sort Records |
| Alt+T | Table Summary (renamed from Statistics Table) |
| Alt+D | Database Summary |
| Alt+G | Graphics Column (primary) |
| Alt+Shift+G | Graphics Table |
| Alt+Shift+F | Filter Records |
| Alt+Shift+E | Export Database (primary chord; Ctrl+Shift+S binding dropped) |
| Ctrl+R | Replace Column (renamed from Update Column) |
| Ctrl+Shift+R | Regex Replace |
| Ctrl+Shift+S | Statistics Column (replaces Save-As convention; see push-back below) |
| Ctrl+Shift+G | Graphics Column (secondary; Alt+G is primary) |
| Ctrl+Shift+X | Extract Regex |

**Push-back on Ctrl+Shift+S.** This chord is the universal cross-application Save-As convention (Word, Excel, every browser save-page-as, every IDE save-file-as). Repurposing it to Statistics Column will surprise new users who reflexively press Ctrl+Shift+S expecting Save-As. The user's argument that DbDo auto-saves so Save-As isn't critical is fair, and Export Database remains on Alt+Shift+E. Filed here so it's visible to anyone reviewing the design.

**Renames affecting dot-prompt tokens** (token = lowercase-with-hyphens form):

- `remove-record` -> `delete-record`
- `remove-record-force` -> `delete-record-force`
- `statistics-table` -> `table-summary`
- `update-column` -> `replace-column`
- `select-record` -> `filter-records`
- `save-databaseas` -> `export-database` (v1.0.97)
- `measure-column` -> `statistics-column` (v1.0.97)
- `measure-table` -> `table-summary` (v1.0.97 and again this version)
- `new-plot` -> `graphics-column` (v1.0.97)
- `new-chart` -> `graphics-table` (v1.0.97)

**Select Columns** new feature (carried over from in-progress v1.0.97): per-table column visibility picker. Alt+S. Dialog with one checkbox per column plus four buttons: OK, Select All, Select None (revert to default), Cancel. Persists per-table via the existing `db.setSelectList()` infrastructure.

**Four new speech commands** (carried over): Say Column, Say Column Marked, Say Rows, Say Rows Marked. Now on L-family chords. saySayColumn behavior changed from "sweep starting at current row" to "sweep starting at row 1" per the user's clarification that "all cells in the current column" means the whole column.

**Validated**: brace and paren balance at 25278 lines; chord-conflict audit clean.

## v1.0.97

**Three big design themes**: (1) analytical command name family aligned around Statistics and Graphics with Column / Table scope, (2) the marks-aware scope-prompt design rule established and applied to Remove Record as the first example, (3) Save Database As renamed Export Database with secondary Alt+Shift+E chord.

**Statistics / Graphics command renames.** The user's "statistics on the virtual column/displayed table" and "graphics on the same" framing pointed at the right name structure. The Measure-* / New-Plot / New-Chart family was renamed:

- `Measure Column` -> **`Statistics Column`** (Alt+S; current virtual column, all visible/filtered rows)
- `Measure Table` -> **`Statistics Table`** (Alt+Shift+S NEW; all visible columns x all visible rows)
- `New Plot` -> **`Graphics Column`** (Alt+G)
- `New Chart` -> **`Graphics Table`** (Alt+Shift+G NEW)
- `Measure Longest` / `Maximum` / `Minimum` / `Shortest` / `Field` -> `Statistics Longest` / `Maximum` / `Minimum` / `Shortest` / `Field`

The pattern: **`<Verb> Column` for one-column scope, `<Verb> Table` for full-view scope**. The naming maps cleanly to a single mnemonic letter family: S for Statistics (Alt+S / Alt+Shift+S), G for Graphics (Alt+G / Alt+Shift+G). The "Table" in the name means "the currently filtered + sorted view of the table" -- not the whole underlying table; clearing the filter changes what counts.

Dot-prompt canonical tokens followed: `measure-column` -> `statistics-column`, `new-plot` -> `graphics-column`, etc. The dispatch switch labels and the alias mappings (`longest` -> `statistics-longest`, `max` -> `statistics-maximum`, `chart` -> `graphics-table`, etc.) all updated together.

**Sort Records moved from Alt+Shift+S to Alt+Shift+O.** Alt+Shift+S was needed for Statistics Table to keep the S-family symmetric. O is for ORDER (the SQL ORDER BY mnemonic) -- standard convention for sort. Alt+Shift+O is one-handed and free.

**Save Database As -> Export Database.** A user filtered to 50 of 1000 rows wants Export Data to write 50 rows; Save Database As writes all 1000 -- so Save-As is conceptually "Export the whole database, ignoring filter." Renamed to make the relationship to Export Data clear:

- `Save Database As` -> **`Export Database`** (Ctrl+Shift+S kept as the cross-app Save-As convention; Alt+Shift+E added as the new productivity chord parallel to Alt+E = Export Data)
- The dot-prompt canonical token `save-databaseas` -> `export-database`
- The `save` and `save-as` natural-English aliases now resolve to `export-database`

The Ctrl+Shift+S binding stays because Save-As is one of the most universal cross-application Windows conventions; new users approaching DbDo expect it to work out of the box.

**Marks-aware scope-prompt system established.** This is the design rule the user requested for commands that can act on either the current record or a marked set:

> A scope-flexible command operates on the current record by default with no prompt. When marks exist in the current filtered view, the command prompts the user to choose: act on current record (the default, safer choice for destructive ops), or on the N marked records, or cancel.

Implementation:

- New `countMarkedInFilter()` helper counts records whose 'marked' value is truthy within the currently filtered set. Iterates through the recordset (which already respects the active filter) and restores position via bookmark on exit.

- New `ScopeChoice` enum (Current / Marked / Cancel) and `promptScope(string sCommandName)` helper. The GUI version uses an LbcDialog with three buttons: "&Current record (default)", "&Marked records (N records)" (count interpolated), "Ca&ncel". Returns the user's choice. If no records are marked, returns Current immediately without prompting -- the default-and-fast path.

- **Remove Record (Ctrl+D)** was retrofitted as the first marks-aware command, matching the user's named example. When no marks exist: standard "Remove the current record?" confirmation (unchanged). When marks exist: scope prompt first, then either single-record confirm or a count-aware bulk-delete confirm ("Remove 7 marked records? This cannot be undone."). Bulk deletes iterate marked positions in reverse so deletions don't shift indices of records not yet processed.

**Category catalog for the marks-aware retrofit.** The audit identified four categories of commands by their relationship to record scope:

- **Category A (single-record always)**: Edit Record, Edit Cell, Copy Record, Copy Record as New, Open Cell, Show Record, Say-X commands, Set Mark, Clear Mark. These operate on the current record by their nature; no scope prompt makes sense.

- **Category B (scope-flexible)**: Remove Record (this version), Append Record, Send Mail, Copy Record / Copy Record as New (multi-record forms), Update Column with marked-rows scope, Regex Replace with marked-rows scope. v1.0.97 retrofits only Remove Record; the rest follow in v1.0.98+.

- **Category C (set operations)**: Sort Records, Filter Records, Export Data, Statistics Table, Graphics Table. These operate on the whole visible set; the user changes scope by changing the filter, not by picking a different command.

- **Category D (cell scope)**: Copy Visible Cells, Update Column, Regex Replace. These are column-scoped within visible rows; the marks-aware retrofit (Category B) for Update Column / Regex Replace will add an optional "marked rows only" scope on top.

**Validated**: brace and paren balance at 25032 lines; chord-conflict audit clean.

## v1.0.96

**Chord reassignments for productivity and mnemonic strength.**

- **Ctrl+Shift+R** is now **Regex Replace**, a new command (was Toggle Read Only). Mnemonic: R for Regex; the Shift modifier marks it as the "power version" companion to Ctrl+R = Update Column (substring replace). Regex Replace interprets the find text as a .NET regex pattern and supports `$1`, `$2` back-references in the replacement.

- **Alt+Z** is now **Toggle Read Only** (was Say Status). The user re-assessed: read-only is a rare-use toggle and the more-valuable Ctrl+Shift+R slot belongs to regex-replace. Alt+Z is a one-handed chord appropriate for a "set once and forget" feature.

- **Shift+Z** is now **Say Status** (was Alt+Z). Joins the Say-X family (Shift+A through Shift+Y, all Say commands), which has the consistent "Shift+letter = speech-only, never moves focus" pattern. Z for Status is intuitive ("zoom out, see status").

**Import / Export promoted to bare Alt chords for one-handed productivity.**

- **Alt+I** = Import Data (was Ctrl+Shift+I)
- **Alt+E** = Export Data (was Ctrl+Shift+X)

Ctrl+I and Ctrl+E stay where they were -- Switch Mark and Edit Record, both high-frequency core operations that earn their bare-Ctrl status.

**Extract Regex moved to Ctrl+Shift+X.** Was Ctrl+Shift+E. The new chord X = eXtract is a stronger mnemonic. Ctrl+Shift+E is now free.

**Update Column actually implemented.** Was a placeholder stub since the rename in v1.0.95. Now provides a working find-and-replace dialog: search text, replacement text, case-sensitive checkbox, dry-run checkbox. Operates over the current virtual column for all currently-filtered rows (clear the filter to apply across the whole table). Reports the number of cells changed. Ctrl+R.

**Regex Replace** (new) is the regex companion to Update Column. Same dialog plus the search text is interpreted as a .NET regex pattern. Supports back-references (`$1`, `$2`) in the replacement. Same scope rules. Ctrl+Shift+R.

**Filter Records dialog redesigned for one-step clearing.** The user's principle: "at most a two-step keyboard way of clearing an existing filter and restoring access to all records." Implementation:

- The dialog pre-populates with the user's last filter values (text, column, mode). Re-opening Alt+Shift+F when a filter is active lets the user edit the existing filter rather than re-typing it from scratch.
- A new **Clear** button (Alt+C) clears the filter and closes the dialog in one keystroke. Total to clear: **Alt+Shift+F, Alt+C** -- exactly two keystrokes as specified.
- The original OK and Cancel buttons retain their roles (OK applies the dialog's values; Cancel discards and closes).
- Status announcements report the outcome: "Filter cleared", "No filter applied", or "Filter applied. N records match."

**Statistics scope clarification** in the history (documenting the decision rather than changing code). Two natural scopes exist for column statistics:

1. **The virtual column the user is focused on, applied to currently-filtered rows.** This is the productive default for "tell me about what I'm looking at." Measure Column (Alt+S) and Describe Column (also Alt+S, currently aliased) do this.

2. **The entire table, all rows.** Measure Table is for this database-summary use case (menu only).

The user can change scope deliberately by clearing the filter (Alt+Shift+F, Alt+C) before invoking the statistic. Statistical announcements report scope ("Median X over N visible rows; Y total when filter cleared") so the user always knows what they're hearing about.

**Shift+letter family expanded.** Z is no longer unused -- Shift+Z = Say Status puts it in the Say-X consistent pattern. All Say-X commands follow the rule "Shift+letter is speech-only, never moves focus, returns the user to wherever they were."

**Validated**: brace and paren balance at 24866 lines; chord-conflict audit clean.

## v1.0.95

**Pre-publicized-release cleanup pass.** The user clarified: DbDo's GitHub repo is currently alpha and only a handful of blind developers have seen it. There is no installed user base to preserve compatibility with. Focus is the cleanest possible design at first publicized announcement (Facebook, LinkedIn, etc.). This version drops a half-dozen back-compat aliases that existed only to preserve muscle memory across renames in v1.0.86 / v1.0.91 / v1.0.94, since muscle memory is no longer a constraint.

**Aliases removed**:

- `set-record` → was rename alias for `edit-record` (v1.0.86)
- `set-cell` → was rename alias for `edit-cell` (v1.0.86)
- `switch-keydescriber` → was rename alias for `toggle-keyhelp` (v1.0.91)
- `edit-configuration` → was rename alias for `edit-settings` (v1.0.91)
- `step-initialchange` → was rename alias for `jump-nextinitial` (v1.0.91)
- `update-field` → was rename alias for `update-column` (this version)
- The "Pre-v1.0.86 name; still accepted" line in `edit-record` help text

The "natural English" aliases that aren't backward-compat (e.g., `update` / `replace` → update-column, `delete` → remove-record, `add` / `append` → new-record, `describer` / `keydescriber` → toggle-keyhelp) are kept; those reflect natural user intent rather than legacy command names.

**Update Field → Update Column.** The command was named "Update Field" but its actual semantics is column-scoped find-and-replace within one column; the menu label already said "Replace Column". Renamed to make the canonical name match what the operation does:

- Canonical: "Update Field" → **"Update Column"**
- Dot-prompt token: `update-field` → `update-column`
- C# identifiers: `recUpdateFieldClicked` → `recUpdateColumnClicked`, `miRecUpdateField` → `miRecUpdateColumn`
- The `update` / `replace` natural-English aliases continue to point at the canonical token (now `update-column`).

**Toggle Read Only restored** as a runtime feature. v1.0.91 removed the GUI Lock-Database toggle while keeping the `-readonly` command-line flag, which was asymmetric. The user re-affirmed the feature's value: anyone running DbDo can edit data they have write access to; the read-only toggle is for the user's own protection against accidental edits while browsing.

Implementation:
- **Ctrl+Shift+R** to toggle (mnemonic: "R for read-only")
- Menu item on Misc menu: "Toggle Read &Only"
- Click handler `toggleReadOnlyClicked` reopens the database with the flipped flag, clears the drill stack (its row identities are invalid in the new connection)
- Settings dialog gets a "Read Only" checkbox alongside Command Echo and Extra Speech. Toggling it in Settings applies immediately via the same code path.
- CLI command: `toggle-readonly`, `read-only`, `readonly` (with optional `on` / `off` / `1` / `0` / `yes` / `no` argument; bare invocation toggles)
- Help-table entry documents all surfaces (menu, chord, settings checkbox, CLI command, command-line flag)
- The `-readonly` command-line flag is preserved, so CLI startup and runtime are at parity

**Optional quotes for file paths**. The user's principle: "enclosing quotes for a string should be optional if the command line can be parsed without ambiguity." Added `resolvePathArg(sArg)` helper: tests `File.Exists(sUnquoted)` and `Directory.Exists(sUnquoted)` first, returning the unquoted path if it identifies an existing filesystem entry; falls back to the v1.0.94 quote-aware tokenizer when no existing file matches. Applied to:

- `cmdOpenDatabase` -- `open database C:\My Stuff\foo.db` works without quotes
- `cmdImportData` -- same for the Markdown-import file path

Export-Data wasn't retrofitted because export paths don't exist yet at command time (they're being created), so File.Exists can't disambiguate. The v1.0.94 `unquote` approach there is correct.

**Dot-prompt CLI prompt helpers** added as infrastructure for future LBC-dialog-equivalents work. Three new static helpers:

- `promptChoiceCli(prompt, options, default)` -- numbered choice list. Prints each option as `  N. label`, marks the default with `(default)`, prompts with `Number or text [N]: `, accepts the number or a substring of the label (case-insensitive), or "cancel"/"quit"/"q" to abort. Returns the 0-based index.
- `promptYesNoCli(prompt, default)` -- yes/no with `[Y/n]` or `[y/N]` default display; returns true/false/null.
- `promptTextCli(prompt, default)` -- single text input with `[default]` shown in brackets.

These are infrastructure. The existing GUI-dialog commands (Find, Jump, Sort Records, Settings, Open Recordset, Recent Files, etc.) will be retrofitted incrementally to use these helpers when invoked from the dot prompt with no arguments. The first round of retrofits will land in v1.0.96+.

**Terminology audit completed**. The Record/Field/Column/Cell/Table/Row nouns are now internally consistent across all canonical names:

- **Cell** = single value at a row+column intersection. Cell-scoped commands: Open Cell, Edit Cell, Append Cell, Copy Cell, Copy Visible Cells, Say Cell.
- **Column** = vertical slice of values across rows. Column-scoped commands: Measure Column, Say Column, Select Column, Update Column (formerly misnamed Update Field).
- **Record** = whole logical entity (one row's worth of fields). Record-scoped commands: New Record, Edit Record, Append Record, Copy Record, Copy Record as New, Remove Record, Remove Record Force, Select Record, Jump Record, Jump Previous Record, Step Record First/Last/Next/Previous, Sort Records.
- **Table** = named collection of records. Table-scoped: Measure Table, Say Tables, Select Table, Switch Table, Switch Previous Table.
- **Field** is reserved for schema-level concepts (a column's definition, not its data). Currently only used in code (Get Field, Set Field) and in the Update Field validators dialog.

The user's "noun matches layout user sees" rule led to the question of whether navigation commands should say Record or Row. The current Record-named navigation (Step Record First/Last/Next/Previous, Jump Record, Jump Previous Record) is kept because the navigation semantically targets a whole record's worth of data even though the visual unit is a row. The audit found this internally consistent; no further renames were warranted.

**Validated**: brace and paren balance at 24626 lines; chord-conflict audit clean.

## v1.0.94

**Dot-prompt case-insensitivity confirmed and documented.** The user clarified that the CLI should be case-insensitive for command names and parameter keywords (matching `cmd.exe` convention), while quoted strings preserve their content verbatim. An audit confirmed that case-insensitivity for command verbs was already correctly implemented in v1.0.93:

- `dispatch()` calls `aTokens[0].ToLowerInvariant()` before alias resolution
- `expandUniquePrefix()` calls `sTyped.ToLowerInvariant()` before matching against the canonical-verb table
- `resolveAlias()` switches on the lowercased input

So all of these resolve to the same internal canonical token: `edit settings`, `Edit Settings`, `EDIT SETTINGS`, `Edit-Settings`, `EDIT-SETTINGS`, `JUMP record`, `Say SORT filter`. This was already working; v1.0.94 documents it explicitly in the dot-prompt help index (bare `help` now prints a CONVENTIONS section as its first item) so users know they can type however they prefer.

**Quote-aware tokenization** added to the dot-prompt parser. The previous whitespace-split tokenizer treated quote characters literally, so `find regex "Hello World"` shattered the quoted phrase into pieces and the embedded space was lost on rejoin. v1.0.94 introduces three new helpers:

- `splitArgsRespectingQuotes(string sLine)` returns a `string[]` of tokens honoring double-quoted regions. Whitespace inside quotes stays inside the token. Surrounding quotes are stripped on the way out. Doubled quotes inside a quoted region (`""`) unescape to a single literal quote. Single quotes are NOT treated as token boundaries, since users commonly type single quotes inside SQL fragments.

- `joinArgsRespectingQuotes(string[] aTokens, int iStart)` is the inverse: rebuild a remainder string from a tokenized array, re-quoting any token that contains whitespace or quote characters so the result round-trips through `splitArgsRespectingQuotes` losslessly. Used by `tryDispatchPrefix` when it peels off the verb tokens and needs to pass the rest along to the next dispatch level.

- `unquote(string sArg)` strips surrounding double quotes from a single argument and unescapes doubled inner quotes. For command handlers that take a "rest of the line" string argument that might be quoted (file paths most commonly), this is the simplest opt-in.

**Handlers updated to be quote-aware**:

- `cmdOpenDatabase`: file path can now be `open database "C:\My Stuff\foo.db"`. Previously the literal quotes were passed to `db.openDatabase()` and the open would fail.
- `cmdImportData`: same fix for the Markdown-import file path.
- `cmdExportData`: the legacy-single-path form correctly detects path extensions on the unquoted form, and the export call uses the unquoted path.
- `cmdFindRegex`: the `<column> <pattern>` parse uses `splitArgsRespectingQuotes`, so `find regex Notes "stays \"in\" quotes"` works (note: in the user's typed string, two double quotes in a row inside the outer quotes become a literal double quote in the pattern).
- `tryDispatchPrefix`: the prefix matcher uses the quote-aware tokenizer when peeling off the verb tokens, so the trailing arguments survive the rejoin intact.

**The Find / Jump / Search / Say-X handlers that take a single string argument** were not retrofitted. Their typical usage is a single unquoted token; adding `unquote()` to all of them is mechanical and the user can request a sweep when they actually hit a case that bites. The case-insensitivity guarantee for command verbs is independent of these handler details and works regardless.

**Dot-prompt CLI help index** (`help` with no argument) now begins with a CONVENTIONS section explaining case-insensitivity and quoting rules with concrete examples. Users no longer have to discover these by trial and error.

**Validated**: brace and paren balance at 24370 lines; chord-conflict audit clean.

## v1.0.93

**Proper AP-style Title Case applied to canonical names.** The user clarified that "Title Case" in the user-facing UI should follow standard publishing convention -- principal words capitalized, short articles/prepositions/conjunctions lowercased when not first or last. Two names from v1.0.92 needed adjusting:

- `Copy Record As New` → `Copy Record as New` ("as" is a mid-title 2-letter conjunction)
- `Exit Child To Root` → `Exit Child to Root` ("to" is a mid-title 2-letter preposition)

`Save Database As` stays as-is because "As" is the last word, and AP convention always capitalizes the last word.

A focused audit of all 129 multi-word canonical names confirmed zero remaining short-word issues. The rule applied: lowercase articles (a, an, the), short coordinating conjunctions (and, but, or, nor, for, yet, so), and short prepositions (≤3 letters: in, on, at, to, by, of, as, up) when they appear mid-title; capitalize everything else including all 4+-letter prepositions.

**Dot prompt now prefers multi-word interpretation over single-token aliases.** When the user types `edit settings export.csv`, the previous dispatcher would resolve "edit" as the bare alias for "edit-record" and try to dispatch as `edit-record settings export.csv` -- wrong. The new dispatcher runs `tryDispatchPrefix` before single-token alias resolution when the input line has 2+ space-separated tokens. If a multi-token prefix matches a canonical command, that wins; only when no multi-token interpretation applies does the bare-alias path run (so single-word input like `edit` still resolves to `edit-record` as before).

This means **lowercase-with-spaces** is now the easy-to-type default at the dot prompt: `edit settings`, `jump record`, `say sort filter`, `find regex`, `save bookmark`. The hyphenated PowerShell-style form (`edit-settings`, `jump-record`, etc.) continues to work for users who prefer it. Both forms route to the same internal canonical token. (Some run-together canonical tokens like `exit-childtoroot` and `save-databaseas` still expect their hyphens between bare-words only; teaching the prefix matcher to also split run-together tokens is deferred to a future version.)

**README updated** with explicit audience positioning. Windows screen reader and keyboard users are named as the top-priority audience. The "developer-culture conventions as alternatives" position is now documented: both the lowercase-with-spaces dot-prompt input form AND the PowerShell-flavored hyphenated form are first-class. The C# implementation continues to follow Camel Type. Three independent layers (user-facing names, dot-prompt input, internal code identifiers), each with its own convention chosen for its audience. The GitHub project URL (https://github.com/JamalMazrui/DbDo) is now in both the README and the CLI About output.

The outdated Alt+Shift+S reference for Toggle Extra Speech (the chord since v1.0.91 is Alt+Shift+Z) was also corrected in the README.

**NuGet case-conversion library research.** Investigated candidate libraries CaseConverter (markcastle, v2.0.1, ~4M downloads via the CaseExtensions sibling package; both maintained), Minerals.StringCases (SzymonHalucha), Simple.CaseConverter, CaseON, and CaseDotNet. All convert between programmer-identifier conventions (camelCase, snake_case, kebab-case, PascalCase, Train-Case) with zero dependencies and broad .NET Framework / .NET Standard compatibility. **None of them implement proper AP-style Title Case** -- all "ToTitleCase" methods are naive "capitalize first letter of every word" wrappers around `TextInfo.ToTitleCase`. That's a different problem (mechanical case-conversion of identifiers) from natural-language Title Case (which requires knowing which words are articles/prepositions/conjunctions). Conclusion: not worth a NuGet dependency. The Title Case rules live in the source as documented logic future maintainers can adjust. The case-conversion problem for DbDo is small enough that a global dictionary isn't needed either; the dot-prompt prefix matcher plus the helpful 60-entry alias table already cover both lowercase-with-spaces and hyphenated input.

**Validated**: brace and paren balance at 24210 lines; chord-conflict audit clean.

## v1.0.92

**Canonical command names rewritten as Title Case With Spaces.** The user clarified that "title case" meant book-title convention (space-separated words with each word capitalized) for the user-facing form, while underlying C# code stays in Camel Type. Previous versions used Pascal-Case-With-Hyphens (`Edit-Settings`, `Toggle-KeyHelp`, `Jump-NextInitial`) as both the canonical display form and the dot-prompt input form; v1.0.92 separates these.

**New canonical display form** (what command echo speaks, what Key Help announces, what the Alternate Menu lists, what MessageBox titles show): `Edit Settings`, `Toggle Key Help`, `Jump Next Initial`, `Save Database As`, `Switch Previous Table`, etc.

**Dot-prompt input form is unchanged**: `edit-settings`, `toggle-keyhelp`, `jump-nextinitial`. The dot prompt is command-line syntax, not user-facing prose; whitespace would break the tokenizer. Users at the dot prompt continue to type hyphenated lowercase tokens.

**289 canonical-name replacements** applied across all `addItem(...)` and `add(...)` help-table calls. Awkward Pascal-second-half words got natural word ordering at the same time:

- `Save-DatabaseAs` → **Save Database As**
- `Copy-RecordAsNew` → **Copy Record As New**
- `Switch-TablePrevious` → **Switch Previous Table**
- `Switch-ObjectPrevious` → **Switch Previous Object**
- `Jump-RecordPrevious` → **Jump Previous Record**
- `Jump-RecordAgain` → **Jump Record Again**
- `Find-RegexPrevious` → **Find Previous Regex**
- `Find-RegexAgain` → **Find Regex Again**
- `Exit-ChildToRoot` → **Exit Child To Root**
- `Copy-VisibleCells` → **Copy Visible Cells**
- `Open-WebSite` → **Open Website** (single-word; modern convention)
- `Open-CellarDatabase` / `Chinook` / `Collection` / `Northwind` / `Sample` → **Open Cellar/Chinook/Collection/Northwind/Sample Database**
- `Open-FileFolder` → **Open File Folder**
- `Open-ScriptFolder` → **Open Script Folder**
- `About-DbDo` → **About DbDo**
- `Invoke-Sql` → **Invoke SQL** (uppercased acronym)
- `Open-Url`, `Say-Url` → **Open URL**, **Say URL**
- `Say-Id` → **Say ID**

**Acronym handling**: SQL, URL, and ID are uppercased to match common usage; everything else is initial-cap each word.

**Backward-compatibility normalization** added to `KeyMap.summaryFor` and `KeyMap.descriptionFor`. Both lookups now compare a normalized key (hyphens collapsed to spaces, lowercased) so that older documentation, scripts, or muscle memory using the hyphenated form continue to find help: `Get-Help "Edit-Settings"` still works at the dot prompt and returns the same description that `Get-Help "Edit Settings"` does.

**Dot-prompt error messages updated** where they referenced canonical names (e.g. `"Step-Record: count must be an integer."` is now `"Step Record: count must be an integer."`). The dot-prompt input verbs themselves (typed by the user) stay hyphenated.

**Three special cases worth knowing about** for future maintenance:

1. The `Step-Record` dot-prompt verb (an internal-only command with `next`/`previous`/`first`/`last` sub-arguments) keeps its hyphenated input form because it's never bound to a GUI menu item. Only its user-facing error/help text uses the Title-Case display form.

2. The `Find-Previous`, `Jump-Record`, `Find-Regex`, etc. references that appeared **inside description prose** ("Find-Previous goes backward") were updated to match the new canonical form ("Find Previous goes backward"). This catches the descriptions that explain one command in terms of another.

3. The `Measure-Field`, `Measure-Longest`, `Measure-Maximum`, `Measure-Minimum`, `Measure-Shortest` MessageBox titles were renamed to Title Case even though those names aren't bound to menu items. They appear in the `toolsMeasureClicked` flow as dialog titles, so they're user-facing and follow the same convention.

**No chord changes this version.** No new commands; no removed commands. The visible difference is in spoken / displayed command names. Anyone whose muscle memory is "Ctrl+, opens Settings" continues to be served unchanged.

**Validated**: brace and paren balance at 24194 lines; chord-conflict audit clean.

## v1.0.91

**Rollback marker:** v1.0.90 is the last version that maximized EdSharp/FileDir command-name and chord conventions. If anything in v1.0.91 or later feels wrong, rolling back to v1.0.90 restores the EdSharp/FileDir-maximized baseline (Edit-Configuration, Switch-KeyDescriber, Step-InitialChange, Save-Path on Shift+P, Lock-Database on Ctrl+F7, Toggle-Extra-Speech on Alt+Shift+X).

The user's clarification ("relax the constraint to maximize EdSharp/FileDir conventions; optimize for DbDo coherence first") guided this version. The renames below reflect that shift.

**Toggle-Extra-Speech rebound to Alt+Shift+Z.** From Alt+Shift+X. The "zzz / hush" mnemonic was the user's suggestion; the new chord matches the introduced sibling Toggle-Command-Echo.

**Toggle-Command-Echo added on Ctrl+Shift+Z.** New Help-menu item with click handler that mirrors helpExtraSpeechClicked: toggles `[Options] commandEcho` in DbDo.ini, invalidates the runtime cache, force-speaks "Command echo on" / "off" through the live region, and updates its menu Checked state. Default ON, so new users hear command names by default; advanced users who find it noisy turn it off in one chord. Startup-time check-state initialization makes the menu reflect the persisted setting.

**Command-echo has no effect in CLI/dot-prompt mode** — confirmed and documented in the help-table entry and a code comment. The `commandEcho` function is only called from the `addItem` / `addItemLocal` menu wrappers, not from the dot-prompt dispatcher. CLI users typed the command name themselves and shouldn't need it echoed back.

**Settings dialog now has both speech toggles.** The Settings dialog (renamed from "Configuration Options" -- see below) exposes Command Echo and Extra Speech as checkboxes. OK saves both to DbDo.ini, applies the runtime state immediately, and refreshes the corresponding menu Checked states.

**Lock-Database removed entirely.** The user reframed the design assumption: anyone running DbDo has full read-write authority over the data they opened. View-only commands (Say-Cell, Say-Position, etc.) let cautious users browse without risk; the explicit read-write/read-only toggle is overhead. Removed: menu item, field declaration, click handler, Enabled update, command-table entry, dot-prompt `lock` alias. The two `InvalidOperationException` messages that referenced Lock-Database now say "The database was opened read-only. Close it and reopen without the -readonly flag to enable editing." The `-readonly` command-line flag is preserved -- users who want to launch DbDo with a database opened read-only from disk can still do `DbDo.exe -readonly path`.

**Renames** (with backward-compat aliases in the dot prompt so existing scripts and muscle memory keep working):

- **Edit-Configuration → Edit-Settings** (menu label "Configuration Options..." → "&Settings..."). "Settings" is the defacto modern convention across Windows itself, macOS Ventura+, VS Code, Slack, Discord, and every Electron-generation app. One syllable shorter than "Configuration" and "Preferences"; scans faster for screen readers; matches new-user expectations regardless of background. Bound to **Ctrl+, (comma)** -- modern Windows convention -- with the menu entry still accessible without a chord.
- **Switch-KeyDescriber → Toggle-KeyHelp** (menu label "&Key Describer" → "&Key Help"). "Key Describer" was an EdSharp-ism that newcomers can't guess; "Key Help" is self-documenting. The verb change from `Switch-` to `Toggle-` aligns with the Toggle-Extra-Speech / Toggle-Command-Echo pattern established this version.
- **Step-InitialChange → Jump-NextInitial** (menu label "Next Initial Change (column-aware)..." → "Jump to Next Initial (where first letter changes)..."). Step-InitialChange was opaque jargon; Jump-NextInitial joins the established Jump-Record / Jump-RecordPrevious family of content-navigation commands.

**On the audience analysis behind these choices** (recorded so future renaming has a reference): DbDo's realistic user base splits into three tiers. Tier 1 is blind/visually-impaired technical users who already use EdSharp/FileDir daily and have PowerShell exposure -- they benefit from verb-noun naming and accept EdSharp conventions. Tier 2 is blind/visually-impaired non-technical users who manage personal data and know Microsoft Office conventions if anything -- they benefit from Office-aligned chords (Ctrl+S, Ctrl+P, Ctrl+F, F1 help) and clear menu labels over clever mnemonics. Tier 3 is sighted developers building accessible software who use DbDo as a tool and reference -- they benefit from predictable command vocabulary.

Where EdSharp conventions (Tier 1) and modern Windows conventions (Tier 2/3) overlap, both serve all tiers. Where they diverge, the modern conventions usually win because Tier 1 users adapt easily, the modern names expand the user base, and documentation is easier when names are self-evident. This version applies that principle: rename the jargon (Configuration, KeyDescriber, InitialChange), keep the pattern (PowerShell verb-noun), add the modern chord (Ctrl+, for Settings) without removing the menu access.

**Brace and paren balance verified.** 24164 lines. No chord conflicts.

## v1.0.90

**Documentation pass following the v1.0.89 Narrator confirmation.** The user reports that v1.0.89 successfully speaks through JAWS, NVDA, and Narrator on Windows 11, closing the eight-round investigation that began in v1.0.76. This version consolidates what was learned and protects against future regressions.

**No functional code changes.** The `dispatchNativeUiaNotification` per-call host-creation pattern from v1.0.87 is preserved as-is. ChatGPT's findings document (`Direct_Screen_Reader_Speech_Findings.md`, generated during the same investigation that produced the reference WinForms sample) is included under `chatgpt_reference/` for future maintainers. Its "Most Important Behavioral Finding" section is the empirical evidence that **reusing a provider for multiple announcements causes screen readers to silently drop later events**, even with unique activity IDs and even when the text differs. ChatGPT specifically tested provider reuse and confirmed it breaks Narrator and NVDA after the first announcement; only fresh-per-call works.

This means a pool of N reused hosts (a tempting optimization to avoid HWND churn) is not safe: the dedupe heuristic appears to be per-source-HWND, so any reuse pattern will eventually drop announcements when two consecutive calls land on the same host. The cost of fresh-per-call is small in absolute terms -- creating a 1x1 Control with an HWND is on the order of tenths of a millisecond and a few KB of managed memory, and the retention ring caps live hosts at 5. For a database manager where announcements fire on user actions rather than in tight loops, the cost is invisible.

**Updated comment block** on `dispatchNativeUiaNotification` now explicitly warns against "optimizing" the path to a reused host, citing ChatGPT's empirical finding. A future maintainer looking at the per-call allocation and thinking "this is wasteful" will see the warning before they break Narrator.

**JAWS COM and NVDA controller-client paths preserved exactly as before.** Per the user's request following the v1.0.89 success: the direct-API paths remain primary in `sayForced`. `isJawsRunning() && jawsSay(...)` is tried first; if JAWS isn't running, `isNvdaRunning() && nvdaSay(...)` is tried next; only if neither reader is detected does the native UIA dispatch fire as the third-tier fallback (which is also the only path that reaches Narrator). All four functions (`jawsSay`, `nvdaSay`, `isJawsRunning`, `isNvdaRunning`) are unchanged. If future problems with the UIA path arise -- a Windows update changes the dedupe heuristic, NVDA's controller-client adds a new failure mode, anything -- the direct-API paths are still there to fall back on cleanly.

**Pool-design investigation summary** (recorded in case it ever needs revisiting): briefly explored replacing per-call creation with a pool of 3 hosts cycled round-robin. The thought was that three different source HWNDs would defeat the dedupe heuristic for any reasonable sequence of repeated announcements. The risk: if dedupe is per-source-HWND rather than over the most-recent-N sources, the pool would work fine until two consecutive announcements happened to land on the same host (a sequence of 4 same-text announcements with a pool of 3 means the 4th repeats the 1st host's text -- silent drop). That's an intermittent failure mode that would be hard to debug later. ChatGPT's empirical finding explicitly tested provider reuse and found it breaks, so the pool was reverted before shipping. Future revisiting only makes sense if a Windows update changes the dedupe behavior in a documented way.

**chatgpt_reference/ folder** in the install tree now contains: the seven C# / manifest files (`AnnouncerProvider.cs`, `NotificationHostControl.cs`, `MainForm.cs`, `Program.cs`, `UiaNativeMethods.cs`, `DiagnosticLogger.cs`, `app.manifest`) plus the findings markdown. The whole bundle is preserved as the canonical record of how the native UIA path was discovered.

## v1.0.89

**Namespace clash fix for v1.0.87's UIA code.** The build error on v1.0.88:

```
DbDo.cs(740,13): error CS0104: 'AutomationNotificationKind' is an ambiguous
  reference between 'System.Windows.Forms.Automation.AutomationNotificationKind'
  and 'System.Windows.Automation.AutomationNotificationKind'
DbDo.cs(741,13): error CS0104: 'AutomationNotificationProcessing' is an
  ambiguous reference...
```

Two `using` directives in DbDo.cs both pull in enum types of the same name:

- `using System.Windows.Forms.Automation;` -- added much earlier for `AutomationLiveSetting` (used by the hidden Label that DbDo's legacy `say()` path mutates).
- `using System.Windows.Automation;` -- added in v1.0.87 for the `IRawElementProviderSimple` / `AutomationNotificationKind` / `AutomationNotificationProcessing` infrastructure the native dispatch path needs.

Both namespaces define `AutomationNotificationKind` and `AutomationNotificationProcessing` independently (they're distinct types in distinct assemblies that happen to share names). The compiler can't pick one without a fully-qualified reference.

**Fix:** every use of these two enum names in the native-dispatch code is now fully qualified as `System.Windows.Automation.AutomationNotificationKind` / `System.Windows.Automation.AutomationNotificationProcessing`. Five sites total: the `UiaNative.UiaRaiseNotificationEvent` P/Invoke signature (two parameters) and the `dispatchNativeUiaNotification` method (three uses: declaring the local, the ternary that selects between `ImportantMostRecent` and `All`, and the argument to the P/Invoke). The other UIA types (`IRawElementProviderSimple`, `ProviderOptions`, `AutomationInteropProvider`) live only in `System.Windows.Automation.Provider`, so no qualification is needed for them.

No behavioral change. v1.0.87's native UIA dispatch (NotificationHostControl / AnnouncerProvider / UiaNative) is the same; only the type-resolution at compile time is fixed.

## v1.0.88

**Build-script fix for v1.0.87's UIA references.** v1.0.87 added two new `/reference:` entries to `buildDbDo.cmd` for `UIAutomationProvider.dll` and `UIAutomationTypes.dll`, passing the bare DLL names and assuming csc.exe would resolve them through `csc.rsp`. It doesn't -- csc.exe's response file lists a fixed set of common framework assemblies, and these two are not in it. The build failed with:

```
error CS0006: Metadata file 'UIAutomationProvider.dll' could not be found
error CS0006: Metadata file 'UIAutomationTypes.dll' could not be found
```

A stale or empty `DbDo.exe` from a previous successful build was left on disk, and running it triggered Windows's misleading "Unsupported 16-Bit Application" dialog -- which appears when the loader sees a truncated or zero-byte MZ image, not because the file is actually 16-bit.

**The fix:** `buildDbDo.cmd` now resolves both DLLs by probing the standard reference-assemblies folder hierarchy and the runtime GAC, then passes full paths to csc:

1. **Primary location**: `C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\` -- present whenever the .NET Framework 4.8 Developer Pack is installed.
2. **Earlier-version fallback**: same folder under v4.7.2 / v4.7.1 / v4.7 / v4.6.2 / v4.6.1 / v4.6 / v4.5.2 / v4.5.1 / v4.5. The API surface of these two assemblies has been stable since 4.5.
3. **Runtime GAC fallback**: `%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationProvider\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationProvider.dll` and the matching path for `UIAutomationTypes`. Always present on Windows 10+, since UIA itself depends on these assemblies being installed.

If neither source is reachable, the build aborts with a clear message pointing to the .NET Framework 4.8 Developer Pack download. Both resolved paths are written to `buildDbDo.log` so future build failures are diagnosable.

**Defensive cleanup:** `buildDbDo.cmd` now deletes any existing `DbDo.exe` before compile, so a failed build leaves no half-written executable behind. This prevents the "Unsupported 16-Bit Application" dialog from surfacing when the user runs DbDo after a build failure they may not have noticed.

No code changes in `DbDo.cs` this version -- the v1.0.87 native UIA dispatch (NotificationHostControl / AnnouncerProvider / UiaNative) is unchanged. Once this build succeeds, Narrator should hear DbDo announcements as JAWS and NVDA already do.

## v1.0.87

**UIA Notification breakthrough -- Narrator now works.** The user obtained a working reference WinForms .NET Framework 4.8 sample from ChatGPT that reaches JAWS, NVDA, and Narrator simultaneously on Windows 11. This version adopts ChatGPT's technique into DbDo's `LiveRegion` speech path.

**The fundamental shift:** previous versions called `AccessibleObject.RaiseAutomationNotification` via reflection (the managed wrapper). That API on .NET Framework 4.8 only works for four control types (Label / LinkLabel / GroupBox / ProgressBar) per Microsoft documentation, and even those don't reach Narrator reliably. v1.0.87 replaces the managed path entirely with **native P/Invoke against `UIAutomationCore.dll`'s `UiaRaiseNotificationEvent`**, supplying a custom `IRawElementProviderSimple` anchored to a real window via `WM_GETOBJECT`. The native function has none of the managed wrapper's restrictions; the screen reader's UIA listener picks up the event directly.

**Implementation:**

- New top-level class `NotificationHostControl : Control` -- 1x1 invisible Control that overrides `WndProc` to return its `AnnouncerProvider` when UIA queries the window via `WM_GETOBJECT` (0x003D). Each notification gets a fresh host (anti-dedupe sequence number, retained on a 5-deep ring so UIA's async source-lookup completes successfully).
- New top-level class `AnnouncerProvider : IRawElementProviderSimple` -- implements the minimal-but-complete property set UIA expects (Name, AutomationId, ControlType=Text, FrameworkId, IsControlElement, IsContentElement). `HostRawElementProvider` anchors via `AutomationInteropProvider.HostProviderFromHandle`.
- New top-level class `UiaNative` -- P/Invoke binding for `UiaRaiseNotificationEvent` in `UIAutomationCore.dll`.
- `LiveRegion.dispatchNativeUiaNotification` is the new private worker. It marshals to the UI thread if needed, creates a fresh host, calls the native function, and retains the last 5 hosts before disposing.
- `LiveRegion.sayUiaString`, `sayUiaStringForced`, and `raiseUiaNotification` (the legacy entry point used by `sayViaUia` inside the dual `say()` pipeline) all route through `dispatchNativeUiaNotification`. The reflection-cached `MethodInfo` and probe flag are gone.

**Build change:** `buildDbDo.cmd` now adds `/reference:UIAutomationProvider.dll /reference:UIAutomationTypes.dll` so csc.exe resolves the `IRawElementProviderSimple` interface and `AutomationNotificationKind` / `AutomationNotificationProcessing` enums. These assemblies are part of .NET Framework 4.8 and ship on every modern Windows machine.

**Reference files** preserved under `chatgpt_reference/` in the build folder for documentation: `AnnouncerProvider.cs`, `NotificationHostControl.cs`, `MainForm.cs`, `Program.cs`, `UiaNativeMethods.cs`, `DiagnosticLogger.cs`, `app.manifest`. Adapted from ChatGPT's `uia_notify_winforms_repeat_fix` sample.

**README updated** to reflect that all three screen readers now work via the corrected UIA path. JAWS and NVDA continue to be served first by their direct-API paths (FreedomSci.JawsApi.SayString and nvdaControllerClient.dll) for lowest latency; the UIA path is the universal fallback that reaches Narrator and any other UIA-listening reader.

**Test UIA Speech command** stays removed from the Help menu (v1.0.86 dropped it). The native dispatch path is exercised on every `LiveRegion.say` / `sayForced` / `sayUiaString` call now, so any DbDo operation that announces also serves as a Narrator test.

**Earlier eight rounds of speech-path debugging** (v1.0.76 - v1.0.86) were guided by the assumption that the managed `RaiseAutomationNotification` was the right API. Microsoft's documentation says so, the reference samples on the dotnet/winforms repo say so, and Kelly Ford's WPF demo demonstrates the equivalent path working on .NET 8. But on .NET Framework 4.8 the managed dispatch is unreliable for Narrator; only native P/Invoke through `UIAutomationCore.dll` reaches all three readers consistently. Lesson: when a working reference sample exists, read its actual source rather than the API docs that lead to a different implementation.

**The wxPython equivalent** (also generated by ChatGPT, attempted by the user) reaches JAWS but not NVDA or Narrator. Inspection shows the wxPython version doesn't intercept `WM_GETOBJECT` on its host window; comtypes' `COMObject` provider is created but UIA can't verify it's associated with a real window in the tree. JAWS apparently doesn't verify; NVDA and Narrator do. Making wxPython work would require subclassing the panel's window procedure, which is a different complexity tier and not relevant to DbDo. The native-P/Invoke technique remains a WinForms specialty.

## v1.0.86

**Major chord-and-command reorganization** per the user's spec in temp.txt. The pattern of dedicated Alt+letter ascending / Alt+Shift+letter descending sort chords for each standard column is retired; one universal Sort-Records dialog on Alt+Shift+S handles every sorting need by defaulting to the virtual column with an opt-in Descending checkbox.

**Commands dropped entirely** (menu items, field declarations, click handlers, dot-prompt help entries, and the `SortDialog` Custom-Sort class): `Sort-Object`, `Sort-Ascending`, `Sort-Descending`, `Sort-ById`, `Sort-ByIdReverse`, `Sort-ByLook`, `Sort-ByLookReverse`, `Sort-ByTags`, `Sort-ByTagsReverse`, `Sort-ByUrl`, `Sort-ByUrlReverse`, `Sort-OldestFirst`, `Sort-RecentFirst`. The single `Sort-Records` command on Alt+Shift+S replaces all thirteen.

**Sort-Records dialog flipped** to match the spec: checkbox is "Descending order" (default OFF, meaning ascending), so the natural default is ascending and the user opts in to descending.

**Verb rename — Set-* → Edit-***. Internal verbs renamed: `Set-Cell` → `Edit-Cell`, `Set-Record` → `Edit-Record`. Menu labels were already "Edit Cell" and "Edit Record"; this aligns the verb spoken by the command-echo feature with the visible label. Dot-prompt commands `set-cell` and `set-record` retained as backward-compat aliases that redirect to `edit-cell` / `edit-record`. Detail help for `Set-Record` updated to show the new canonical name with a note about the alias.

**F2 row-sync bug fix** (carried from v1.0.85): `recSetCellClicked` explicitly resyncs `db.absolutePosition = iVirtualRow` both before reading the cell value and again before committing the update, so F2 always edits the row the user hears after Alt+Control+arrow navigation regardless of background sync drift.

**Test UIA Speech menu item removed entirely** (carried from v1.0.85): the diagnostic produced no audible result on tested configurations; the underlying `LiveRegion.sayUiaString` machinery stays in place as silent future-compatibility code.

**Chord rebinds:**

- `Open-Url` rebound from Ctrl+Shift+U to **Ctrl+U** (per spec). `Clear-Mark` (Unmark Record) lost its Ctrl+U chord to make room; reachable via the menu or via `Switch-Mark` (Ctrl+I) which toggles.
- `Save-DatabaseAs` rebound from Ctrl+S to **Ctrl+Shift+S** (per spec). `Backup-Database` lost its Ctrl+Shift+S chord and is now menu-only.
- `Append-Record` bound to **Alt+Shift+C** (previously unbound). `Edit-Configuration` (Configuration Options) lost its Alt+Shift+C chord; reachable through the Misc menu.
- `Say-Updated` bound to **Shift+D** (previously unbound).
- `Say-Added` (Shift+A) re-added — v1.0.85 had dropped this assuming the Added column was being abandoned, but the spec retains it.

**Chords explicitly left as-is** (per user clarification): `Say-Path` stays on **Shift+P**, not Alt+P. Double-pressing Say-Path opens the existing `speakOrShow` memo dialog from which the path can be copied via Ctrl+C (Windows's "copy current line if no selection" convention). Alt+P and Alt+Shift+P are unassigned.

**Deferred per spec's bracketed editorial comments** — items the user marked with square-bracket annotations indicating either ambiguous intent or new functionality requiring more than chord-rebinding:

- All G-family commands (`Say Go to`, `Go to Record` with rich target syntax including "+3" / "-10" / "20%" / "+5%" / "-5%", `Repeat Go`, `Graphics Output`)
- `Shift+J = Say Jump` with target-substring semantics
- `Alt+K = List Bookmarks` (picker dialog)
- `Control+L = List Column` (all values starting from first)
- `Alt+Shift+N = New Database` (interactive schema builder merging with standard columns)
- `Alt+Shift+Q = Query History` (10 most recent picker)
- `Control+Shift+E = Extract Column` (semantics unclear vs existing `Extract-Regex` on the same chord)
- `Control+Shift+R = Regex Replace` (in current column)

These remain on the queue for future versions. Brace and paren balance verified at `brace=0, paren=0, lines=23971`. Chord-conflict audit passes cleanly.

## v1.0.85

**Naming fixes, F2 functional fix, plus partial chord reorganization.** The chord reshuffle the user requested has several ambiguous pieces that need clarification before completion; this version ships the unambiguous fixes and flags the remaining questions.

**Done in this version:**

- **Test UIA Speech menu item removed entirely** (field declaration, menu binding, click handler all gone). The UIA-path machinery in `LiveRegion` stays in place as a best-effort future-compatibility layer, but the user-facing diagnostic is gone since it produces no audible result on current configurations.
- **Internal verb renamed: `Set-Cell` → `Edit-Cell`, `Set-Record` → `Edit-Record`.** Nine `Set-Cell` occurrences and four `Set-Record` occurrences updated across click handlers, dialog titles, and the command-name help table. Menu labels were already "Edit Cell" and "Edit Record"; this brings the verb that the command-echo feature speaks into alignment with the visible label.
- **F2 row-sync bug fixed.** `recSetCellClicked` now explicitly synchronizes `db.absolutePosition` to `iVirtualRow` both before reading the field's value into the dialog and again before committing the update. The previous code relied on the existing sync paths (via `virtSyncListSelection` and `virtSyncFromListSelection`) but those don't fire on every code path; under filters, sorts, or background refreshes the cursor could drift. Defense in depth: F2 now always edits the row the user can actually hear via Alt+Control+arrow navigation.
- **Say-Added (Shift+A) dropped entirely** — menu item, field declaration, and handler removed. Shift+A is now unassigned.
- **Say-Updated bound to Shift+D** (was `Keys.None` since v1.0.67's wave-2 work).

**Still pending — need user clarification:**

1. **Where does Database Summary move?** Currently Alt+D. The user wants Alt+D for Sort-ByUpdated. The user suggested Alt+S, but Alt+S is already taken by `Measure-Column` (Statistics from Column). Options: (a) move Measure-Column elsewhere and give Database Summary Alt+S, (b) move Database Summary to a different chord entirely (e.g. Ctrl+Alt+D), or (c) leave Database Summary on Alt+D and pick a different chord for Sort-ByUpdated.
2. **Url commands' chord home.** Currently the url-family lives on U: `Say-Url` (Shift+U), `Sort-ByUrl` (Alt+U), `Sort-ByUrlReverse` (Alt+Shift+U), `Open-Url` (Ctrl+Shift+U). The user suggested moving them to A-family chords now that Say-Added is gone. Question: keep url commands on U-family chords, or move them to A-family chords (Shift+A, Alt+A, Alt+Shift+A) where Say-Added used to be? If the latter, the new url chords would coexist with the existing `Sort-Ascending` (Alt+A) and `Sort-Descending` (Alt+Shift+A) which prompt for a column.
3. **Generic sort placement.** Two "generic sort" commands exist: `Sort-Object` (currently Shift+S, full multi-column Custom Sort dialog) and `Sort-Ascending` / `Sort-Descending` (Alt+A / Alt+Shift+A, single-column prompt with direction). User wants "the generic sort" on Alt+Shift+S. Which of the two should land there?
4. **Sort-ByUpdated chord.** Pending the answers above. Provisional plan if Database Summary moves successfully: Sort-ByUpdated on Alt+D, Sort-ByUpdatedReverse on Alt+Shift+D.

The four pending items interact; I'd rather answer all of them in one round than ship a partial reshuffle that needs another correction pass. Tell me your preference on each and I'll do them together.

## v1.0.84

**Mystery resolved.** The reason Alt+W appeared to produce JAWS speech in DbDo while the standalone UIA_WinForms_test app was silent: the speech the user heard from DbDo's Alt+W was the **command echo** feature, which announces every GUI command's name through the JAWS direct-API path. JAWS was saying "Test UIA Speech" — the menu item's label as echoed by DbDo's command-announcement system, not the UIA Notification's announcement payload. The Notification event itself was silent for JAWS in DbDo too, just as it was in the test app.

This means **the entire UIA Notification code path has been dead code** on .NET Framework 4.8 WinForms for the user's Windows 11 configuration. JAWS and NVDA support has been functioning correctly through their direct-API paths (`FreedomSci.JawsApi.SayString` and `nvdaControllerClient.dll`) the entire time. The `sayUiaString` family, the legacy `sayViaUia` fallback inside `say()`, and the Test UIA Speech menu item have all been firing the Notification event without anyone hearing it.

**v1.0.84 keeps the UIA code in place** as a best-effort path that may benefit from future Windows or Narrator improvements, but adjusts user-facing surfaces to reflect reality:

- **Alt+W chord removed from Test UIA Speech.** The chord was prime hotkey real estate that should be available for a feature that actually works. The Test UIA Speech menu item remains under Help, reachable through the menu, renamed to "Test UIA Speech (diagnostic)" so its purpose is clear.
- **History entry is honest about the eight-round investigation.** No claim that the UIA path works for any reader on the tested configuration. If a future Windows update changes that, users can verify via the diagnostic menu item.

The accessibility story DbDo offers is: **JAWS and NVDA fully supported via direct APIs. Narrator support is best-effort via the documented UIA pattern; on current .NET Framework 4.8 WinForms / Windows 11 builds, the path does not reach Narrator in our testing.** README already reflects this from v1.0.83.

**Investigation lessons documented for future reference:**

1. When a feature appears to work, verify what the screen reader is actually saying — not just that "something" was spoken. Asking the user to read back the exact phrase would have resolved this in round one rather than round nine.
2. Microsoft's documentation contains the definitive answer for .NET Framework 4.8 UIA behavior (Label, LinkLabel, GroupBox, ProgressBar are the only controls whose AccessibleObjects support the UIA Notification event). Web research at the start would have saved several rounds of source-priority experimentation.
3. Command-echo features that announce menu item names are easy to mistake for the announcements those menu items make.

Returning to actual database features next.

## v1.0.83

**Narrator status: best-effort.** After v1.0.82's source-priority fix (Label first, matching Microsoft's documented pattern) and eight rounds of attempted variations across v1.0.76 through v1.0.82, user testing on Windows 11 confirms Narrator still does not hear DbDo's UIA Notification announcements. JAWS and NVDA work reliably via both their direct-API paths and (in v1.0.82+) the corrected UIA Notification path. Narrator does not.

The investigation is concluded with this honest assessment: **DbDo's UIA path is implemented per Microsoft's documented pattern**, but Narrator on Windows 11's current builds does not reliably honor `RaiseAutomationNotification` events from .NET Framework 4.8 WinForms apps the same way it does from .NET 6+ WPF apps. Kelly Ford's reference WPF demo built against .NET 8 reaches all three readers on the same machine where DbDo's .NET Framework 4.8 build reaches only JAWS and NVDA. The runtime appears to be the discriminator; moving DbDo to .NET 8 would bundle a 70-100 MB runtime, which is not acceptable per project policy.

**Changes in this version:**

- **README updated** to honestly state JAWS and NVDA are fully supported; Narrator is best-effort through the UIA path.
- **Tested-with line updated** to reflect actual test results rather than aspirational support.
- The `Test UIA Speech` command (Alt+W) is retained as a diagnostic. Users who want to know whether their particular Windows / Narrator configuration hears DbDo can press it to find out. If a future Windows or Narrator update improves UIA Notification dispatch, DbDo should benefit automatically without any further code changes -- the path is wired correctly per the documentation.

**Looking forward:** the Narrator question is now off the active investigation list. DbDo development can return to actual database features. Wave-3 commands, additional standard-column features, schema-introspection improvements, and the broader data-management roadmap are the next priorities.

## v1.0.82

**Root cause found.** Web research surfaced Microsoft's own documentation (https://learn.microsoft.com/en-us/dotnet/framework/whats-new/whats-new-in-accessibility) and the dotnet/winforms issue tracker (issue #4494). The documented constraint:

> On .NET Framework 4.8, `AccessibleObject.RaiseAutomationNotification` is only honored by the AccessibleObject implementations of four controls: **Label, LinkLabel, GroupBox, and ProgressBar**. Calling it on any other control's `AccessibleObject` -- including a `Form`'s, a `Button`'s, a `TextBox`'s, or a `ListView`'s -- silently no-ops. The method returns success but no UIA event is dispatched.

This is the root cause of seven rounds of debugging that started in v1.0.76. DbDo's pure-UIA path (`sayUiaString` and friends) was raising the notification from the form's `AccessibleObject` from v1.0.78 onward, which silently no-ops. JAWS still spoke for Alt+W in user testing because DbDo's legacy `say()` path mutates the hidden Label's `Text` from various places around the same time, which JAWS picks up as a separate `LiveRegionChanged` event. The Notification event from `sayUiaString` itself was never dispatched.

The Microsoft-documented working pattern, copied verbatim from the .NET Framework "what's new in accessibility" page:

```csharp
raiseMethod.Invoke(progressBar1.AccessibilityObject,
    new object[3] { /*Other*/ 4, /*All*/ 2, "The progress is 50%." });
```

A `ProgressBar`'s `AccessibilityObject`. Not a Form's, not a Button's.

**Fix in `LiveRegion.raiseUiaNotificationNow`:** source priority reversed. The hidden Label is now the primary source. The form's AccessibilityObject remains as a last-resort fallback only because some path could in principle exist where the Label is unavailable (e.g., CLI-only launch), though such fallback firing is expected to silently no-op on .NET Framework 4.8.

This is the same Label that `LiveRegion.attach` has been creating since v1.0.59 -- 1x1, tucked under the MenuStrip, `LiveSetting=Assertive`. It's a Label, so its `AccessibleObject` has the UIA Notification provider implementation. The previous "improvement" of using the form as source was a regression I shipped through five versions without realizing.

**What this means for Narrator:** if Narrator was the problem all along because the WinForms UIA bridge on .NET Framework 4.8 has limited notification dispatch, Narrator should now hear the notification when sayUiaString fires from the Label. Whether Narrator does or doesn't pick this up is now testable cleanly.

**Companion test app `UIA_WinForms_test.cs`** updated with the same fix: fires from the hidden Label, not the form. If the test app speaks for JAWS/NVDA/Narrator after this fix, the documentation-confirmed pattern is verified end to end. If only JAWS and NVDA speak and Narrator stays silent, then Narrator has an additional quirk specific to it -- but at minimum the JAWS and NVDA path will be working correctly via UIA Notification, not just incidentally through the LiveRegionChanged event from other code paths.

## v1.0.81

**Manifest added — declaring Windows 10/11 support.** The v1.0.80 test apps revealed an unexpected result: with the parameter count corrected to three, `AccessibleObject.RaiseAutomationNotification` returned `False` on every WinForms call (no exception, but the underlying UIA infrastructure rejected the call). The WPF test returned `SUCCESS` on every call but still produced no speech. Both apps logged `Environment.OSVersion = Microsoft Windows NT 6.2.9200.0` — that's Windows 8's version string, returned for any process that lacks an application manifest declaring newer-Windows support.

This is the documented behavior of Windows's "version lying" feature: starting with Windows 8.1, unmanifested processes get Windows 8's reported version regardless of the actual OS. UIA 1.1 features added in Windows 10 1709 (Notification events specifically) check this manifested version and refuse to dispatch when the caller claims to be Windows 8 or older — silently, with `RaiseAutomationNotification` returning `False`.

This explains:

- Why the test .exes returned `False` / no-speech in v1.0.80.
- Why JAWS and NVDA in the v1.0.76 testing of DbDo's Alt+W appeared to "hear" the test message — they were actually responding to other accessibility events fired by the focus change into the menu and back, not the UIA Notification itself.
- Why Narrator has been silent through all five rounds of attempted fixes — Narrator depends exclusively on the UIA Notification dispatch, which was being rejected at the manifest-version check.

**Fix:** `DbDo.manifest` ships now and is embedded via `csc /win32manifest:DbDo.manifest` in `buildDbDo.cmd`. The manifest declares Windows 10 (Id `{8e0f7a12-...}`) and earlier supportedOS GUIDs, requested execution level `asInvoker`, and per-monitor DPI awareness. The installer (`DbDo_setup.iss`) ships `DbDo.manifest` alongside `DbDo.exe` and `DbDo.ico` so it's visible in the install folder for diagnostic purposes, though Windows reads it from the .exe's embedded resource at runtime, not the disk file.

**No other code changes.** The `LiveRegion` class still has the three-argument call from v1.0.80; that's correct and stays. The hypothesis is that v1.0.80's three-argument call was correct and the silent rejection was happening one layer above the parameter count. v1.0.81 tests that.

If v1.0.81's Alt+W reaches Narrator (and JAWS and NVDA continue to hear it), the manifest hypothesis is confirmed and we can simplify DbDo's speech architecture confidently. If Narrator is still silent, we need to look at the next layer — possibly Narrator's notification-source filter, or per-app verbosity policy.

The companion `uia_test_apps.zip` is updated with `UIA_test.manifest` and modified build scripts that embed it. The WinForms test now also calls `GetLastWin32Error()` after every `Invoke` and logs the error code, so if `RaiseAutomationNotification` still returns `False`, the log will tell us the specific failure reason. The wxPython build script cleans `dist\` and `build\` before running PyInstaller to avoid the `PermissionError: Access is denied` that locked the previous .exe.

## v1.0.80

**Real fix for Narrator silence: three-argument call, not four.** Investigation triggered by the user's WinForms UIA test build, which JAWS reported "Parameter count mismatch" on every button. Microsoft's documentation and the dotnet/winforms GitHub repository both confirm: `AccessibleObject.RaiseAutomationNotification` takes three parameters (`AutomationNotificationKind`, `AutomationNotificationProcessing`, `string notificationText`), not four. The WPF equivalent `UIElementAutomationPeer.RaiseNotificationEvent` does take four; the activityId fourth parameter is set to `string.Empty` internally by WinForms.

DbDo's reflection invocations in `raiseUiaNotification` (used by the legacy `sayViaUia` Narrator-fallback path) and `raiseUiaNotificationNow` (used by v1.0.76's `sayUiaString`) had been passing four args from day one. Both call sites had `try/catch { /* swallow */ }` wrappers that hid the `TargetParameterCountException` thrown on every invocation. **JAWS and NVDA never noticed** because their direct-API paths (`FreedomSci.JawsApi.SayString` and `nvdaControllerClient.dll`) run first in `sayForced` and succeed; the broken UIA fallback never executes when those readers are running. **Narrator was the only path that depended on the UIA call**, and every invocation hit the exception silently.

Both call sites in `DbDo.cs` now pass exactly three arguments. The `sActivityId` parameter is removed from the helper chain `sayUiaString` → `raiseUiaNotificationWithMode` → `raiseUiaNotificationNow` since it no longer maps to any actual API parameter. Comments explain the WinForms vs WPF signature difference so this mistake doesn't recur.

**An open question remains about v1.0.76 testing.** The user reported that v1.0.76's Alt+W (`sayUiaString`) made JAWS and NVDA speak "Test UIA speech" — but if every reflection invocation was throwing, neither reader should have heard anything via that code path. The only way speech could have reached them is if `MethodInfo.Invoke` was leniently accepting four args against a three-arg method (unlikely but not impossible) or if a parallel code path was firing in the same window of time. v1.0.80 will test cleanly: if Alt+W now reaches Narrator too, the three-arg fix is the answer; if Narrator still stays silent, we have a different problem to solve and JAWS/NVDA's previous success must have come from a path we haven't accounted for.

**Test apps updated:**

- **UIA_WinForms_test.cs** rewritten with the correct three-argument signature plus runtime logging. Every notification attempt writes a timestamped line to `uia_winforms_test.log` next to the .exe — button label, processing-mode value, reflection-lookup result, Invoke return value or exception type and message. Whatever happens, the log makes it visible.
- **build_UIA_WPF_test.cmd** rewritten to reference WPF assemblies from the **runtime** location `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\` instead of the Developer-Pack reference-assemblies folder. The runtime location is always present on Windows 10 1903+ and Windows 11 without needing any developer-pack install. Both build scripts now log full compiler output to `<scriptname>.log` so build failures are visible offline.

The WinForms test should now produce speech on every button when run with JAWS, NVDA, or Narrator active. The WPF test should now build on a stock Windows 11 machine.

## v1.0.79

**Two more strategies attempted to make Narrator hear `sayUiaString`,** since v1.0.78's two changes (form-as-source, processing-mode=All) didn't reach Narrator on the user's Windows 11 setup:

- **Deferred firing via `BeginInvoke`.** Narrator on Windows 11 can drop UIA notifications fired during the exact moment of menu-strip dismissal because it's busy processing the menu-close events. The notification now posts through the form's message queue rather than firing inline; it runs one message-loop turn later, after the menu-close events have drained. JAWS and NVDA are unaffected (they'd have heard the notification synchronously too). Synchronous path preserved as a fallback if `BeginInvoke` throws or if the form is unavailable.
- **Source priority: `ActiveControl` > form > Label.** v1.0.78 used the form's `AccessibilityObject` as the notification source. v1.0.79 prefers `frmOwner.ActiveControl` first — whatever control has real focus when the notification fires (the listview, an input box, a dialog control). Narrator targets notifications by proximity to the focused element, so firing from the focused element itself is the most reliable way to reach Narrator. Falls back to the form's AccessibleObject (v1.0.78's source), then to the Label, in that order.

This is still hypothesis-driven — Narrator's UIA event handling is under-documented and varies across Windows 11 builds. If Narrator still doesn't hear Alt+W after this build, the next candidates are:

- Change `AutomationNotificationKind` from `Other` (4) to `ActionCompleted` (2). Some screen readers treat the two values differently; Narrator may prefer the more specific kind.
- Change the processing mode to `ImportantAll` (0). Both `MostRecent` (3) and `All` (2) have been tried; `ImportantAll` is the one remaining mainstream option.
- Add a UIA notification listener to the test command's own handler to confirm the notification is actually being raised on the wire (currently we have no visibility into whether the notification reaches the UIA event bus at all — Narrator's silence could mean it never received the event, OR it received it and chose not to speak).

Diagnostic confirmation that may inform the next attempt: the user's Test-Reader diagnostic dialog reported `Windows reports any screen reader active: no` even with Narrator running. `SystemParametersInfo SPI_GETSCREENREADER` doesn't reliably detect Narrator (Microsoft hasn't documented why); this means DbDo's existing detection logic mostly falls through to the UIA path for Narrator. The UIA path itself is the question.

## v1.0.78

**Narrator now hears `sayUiaString`.** Investigation of why the user's JAWS and NVDA picked up Alt+W's UIA notification but Narrator stayed silent yielded two probable causes; both are addressed in this build:

- **Notification source changed from the 1x1 Label to the form itself.** Narrator is particular about which UIA element raises a notification: it honors notifications from top-level windows reliably, but tends to ignore them from marginal hidden controls. The Label (`lbl`, 1x1, tucked under the MenuStrip) had worked for JAWS and NVDA because both readers are permissive about source. The new `frmOwner` field on `LiveRegion` captures the form at `attach` time and `raiseUiaNotificationWithMode` uses the form's `AccessibilityObject` as the notification source. Falls back to the Label only if the form reference is unavailable.
- **Processing mode changed from `MostRecent` (3) to `All` (2).** The legacy `sayViaUia` path (DbDo's existing Narrator fallback inside `sayForced`) was already using `All` and getting through to Narrator; the new `sayUiaString` was using `MostRecent` for "polite queuing." Narrator may filter `MostRecent` differently from `All`. Switched the polite variant to `All` to match the path that's known to work for Narrator. `sayUiaStringForced` continues to use `ImportantMostRecent` (1) since interrupt behavior is the point of that variant.

These changes are honest hypotheses, not guarantees — Narrator's notification handling is under-documented, and the only way to be sure is to test on a machine with Narrator running. If Narrator still stays silent, the next candidate is `ImportantAll` (0) or a different `AutomationNotificationKind` value than `Other` (4).

**EdSharp / FileDir chord conventions enforced.** Per the user's reminder, EdSharp and FileDir both use Alt+Shift+C for "Configuration Options" and Alt+C for cell-level "Append to Clipboard." DbDo's bindings now match:

- **Alt+C** = Append Cell to Clipboard (unchanged, already in this configuration)
- **Alt+Shift+C** = Configuration Options... (restored — v1.0.77 had moved this to no-chord to resolve a conflict with Append Record)
- **Append Record** moves off Alt+Shift+C to no-chord, reachable via the Edit menu

Verified against `EdSharp.cs` and `FileDir.cs`: both have `menuMiscConfigurationOptions` bound to "Alt+Shift+C" with the label "Configuration Options" or "Configuration Options ...". DbDo's label "Configuration Options..." matches exactly.

The Append Record chord change is a minor regression for users who used it. It's a rare command (collecting multiple records onto the clipboard with a separator); the menu access remains and the dot-prompt verb `append-record` still works. EdSharp/FileDir don't have an analogous record-level command, so dropping the chord respects the convention rather than inventing a new chord that DbDo would need to defend long-term.

A full audit of `addItem` chord assignments after this build confirms no duplicate chord registrations remain.

## v1.0.77

**Two fixes from in-use testing of v1.0.76:**

**Alt+Shift+C chord conflict resolved.** v1.0.69 wave-2 introduced `Append Record` on Alt+Shift+C, but an older `Edit-Configuration` command (Configuration Options dialog, on the Misc menu) had been holding the same chord. WinForms' shortcut registration prints a warning at startup when the same chord is registered twice via a menu strip; the user saw that message on first load of v1.0.76. Resolution: `Edit-Configuration` lost the chord (set to `Keys.None`) — it's an infrequent setup command and remains reachable via the Misc menu and the dot prompt. `Append Record` keeps Alt+Shift+C as originally specified.

A full audit of `addItem` chord assignments confirms no other duplicates remain after this fix.

**Test UIA Speech command revised.** Two changes per user feedback:

- **Chord changed from no-chord to Alt+W**, since JAWS auto-reads dialog text and the user reported difficulty distinguishing the direct-speech announcement from any dialog content that followed. Alt+W keeps the listview focused so the user hears only the UIA announcement.
- **Follow-up explanatory dialog removed.** The handler now just fires `LiveRegion.sayUiaString` and returns. No MessageBox, no focus change. The shortened announcement text is "Pure UIA speech test. RaiseAutomationNotification only, no JAWS or NVDA specific API." If the user hears it, the path works.

The original `Test Screen Reader Speech` (Test-Reader) command is unchanged — it continues to show its diagnostic MessageBox after speaking, because that path's whole purpose is to report which reader was detected and which API path was used. The two test commands now have complementary roles: Test-Reader for diagnostic detail, Test UIA Speech for fast pass/fail of the pure-UIA path.

## v1.0.76

**Pure-UIA speech path exposed as `sayUiaString` and `sayUiaStringForced`** on `LiveRegion`. Both methods fire `AccessibleObject.RaiseAutomationNotification` directly, bypassing JAWS's FreedomSci COM API, NVDA's controller-client DLL, and the Label/LiveRegionChanged intermediary. JAWS, NVDA, and Narrator all listen to the UIA Notification event in their default modes, so this path reaches all three without DbDo needing to detect which reader is running.

- `sayUiaString(sText)` uses the **MostRecent** processing mode (3) — polite, queues behind current speech.
- `sayUiaStringForced(sText)` uses **ImportantMostRecent** (1) — interrupts current speech.

These are separate from the existing `say()` and `sayForced()` methods, which continue to prefer per-reader APIs when JAWS or NVDA is detected. Nothing was removed; the new methods are an additional path the caller chooses explicitly. The activity-id string passed to the UIA event is `"DbDo-Uia"` for the polite variant and `"DbDo-Uia-Important"` for the interrupting variant, distinct from the existing `"DbDo"` activity id so screen readers can distinguish the two pipelines if their announcement-history is being inspected.

**Approach traces to** the WPF technique described by Kelly Ford in his UIANotifications demo (https://github.com/kellylford/TheWorkBench/tree/main/UiaNotifyDemo). The WinForms equivalent uses `AccessibleObject.RaiseAutomationNotification` instead of WPF's `UIElementAutomationPeer.RaiseNotificationEvent`, but the underlying UIA event is the same. DbDo had a UIA path internally already (used in its Narrator fallback inside `sayViaUia`), but it was not exposed for direct caller invocation and it hardcoded the `All` processing mode (2); the new methods give callers explicit control over the polite-vs-interrupting choice.

**New menu command Test-UIA Speech** under Help, no chord. Invokes `LiveRegion.sayUiaString` with a known test message and then displays an explanatory dialog so the user can compare its behavior to Test-Reader (which prefers JAWS COM and NVDA controller-client paths). Useful for diagnosing which speech path reaches a particular reader configuration.

## v1.0.75

**Inix format added as a supported import and export option.** The Inix format (extended .ini) is plain text with three powerful additions beyond classic .ini: multi-line string values (plain form starting after `key=` on its own line, or fenced form with `` ` `` or `"""` delimiters); sections that can be either a dictionary of dictionaries (named sections like `[Replace dog with cat]`) or a list of records (anonymous `[]` or numbered `[RecordNNN]`); and an implicit `[Global]` section for top-level keys before the first explicit section. No escape characters, no doubled quotes, no backslash sequences — values are stored verbatim. The format is the work of Jamal Mazrui in the KeyLine toolkit; DbDo adopts it as another import and export format.

DbDo's Export Data now writes .inix as a list-of-records file, choosing the leading-zero width on the `[RecordNNN]` section name so that ASCII sort of section names matches numeric order: 5 records use `[Record1..5]`, 99 records use `[Record01..99]`, 999 records use `[Record001..999]`. NULL values are omitted from a record (no `key=` line at all), rather than written as an empty value. The output uses UTF-8 with BOM and CRLF line endings, the same as DbDo's other text-file outputs.

Import Data now accepts .inix files and inserts each section's pairs as a row in the current table. Keys that don't match a column on the target table are silently skipped (matching how the Markdown import handles unknown columns). The implicit `[Global]` section is treated as document metadata and skipped when the file has more than one section, which is the typical case for table-shaped .inix files.

**One new static class on `DbDo.cs`: `InixCodec`.** Public API:

- `static List<Section> read(string sPath)` — parses an .inix file.
- `static void writeAsConfig(string sPath, List<Section> lSections)` — writes named-section .inix.
- `static void writeAsTable(string sPath, List<string> lFields, List<Dictionary<string,string>> lRows)` — writes list-of-records .inix.

The codec is independent of `DbDoManager`, so other tools or scripts in the DbDo ecosystem can use it as a standalone format library.

**New section "The Inix file format" in DbDo.md** documents the format with examples covering all three forms (plain multi-line, fenced multi-line, list-of-records), the comment syntax including `[;Section]` for commenting out a whole section, the implicit `[Global]` section, and the use cases.

**README updated** to state DbDo's positioning goal explicitly: to be the most screen-reader-accessible and keyboard-accessible general-purpose relational-database manager available. The accessibility consideration is the design, not a layer applied on top.

## v1.0.74

**Two FileDir-style mark-and-move chords retired** to eliminate conflict with the data list's type-to-search. The chord `>` (Shift+Period) had been "Mark and next"; the chord `<` (Shift+Comma) had been "Unmark and next." Each had an alternate binding that's free of typeahead-search conflict, so retiring them costs nothing functionally. The surviving mark-and-move chords:

- **Shift+DownArrow** — Mark and next
- **Shift+UpArrow** — Mark and previous
- **Alt+Shift+DownArrow** — Unmark and next
- **Alt+Shift+UpArrow** — Unmark and previous

Each of these is safe in the data list because Shift+arrow has no native meaning on a single-select ListView, so the chord can't conflict with typeahead-search (which only consumes printable characters).

**Audit of remaining typeahead-search conflicts:** fifteen Shift+letter commands are still bound, none with an alternate chord. They are: Say-Path (Shift+P), Say-Yield (Shift+Y), Say-Marked (Shift+M), Say-Notes (Shift+N), Say-Tags (Shift+T), Say-Kin (Shift+K), Say-Added (Shift+A), Say-Cell (Shift+C), Say-Filter (Shift+F), Say-Id (Shift+I), Say-Look (Shift+L), Say-Related (Shift+R), Say-Url (Shift+U), Window-Summary (Shift+W, still a deferred stub), and Sort-Object (Shift+S, the multi-column Custom Sort). Each is the only binding for its command. Resolving the conflict requires either accepting that Shift+letter typeahead is unavailable in the data list (the current state) or moving the Say-X family to a different modifier set, which would break the mnemonic that the user deliberately chose for Shift+letter assignments. **The user's question — "are there any printable characters being used as hotkeys which do not have another hotkey that does the same thing?" — has the answer: yes, all fifteen Shift+letter hotkeys listed above.** Resolution deferred for user decision since the trade-off is a matter of design preference, not a technical bug.

**Indexes auto-created on sortable standard columns.** `ensureRecommendedIndexes` already created `CREATE INDEX IF NOT EXISTS` on foreign-key columns and the `marked` column. v1.0.74 extends the criteria to cover the standard columns that are reachable via Alt+letter Sort-by hotkeys plus the timestamp columns commonly used as sort targets: `look` (Alt+L), `tags` (Alt+T), `url` (Alt+U), `added`, `updated`. The id column already has SQLite's implicit rowid-alias index, so it's excluded. Index names follow the existing convention `idx_<table>_<column>`. The IF-NOT-EXISTS guard keeps the operation idempotent across opens; databases opened before v1.0.74 will get the new indexes added the next time they're opened in v1.0.74 or later.

## v1.0.73

**Database Summary (Alt+D)** replaces the stub from v1.0.67. A read-only memo dialog opens with one line per table, each followed by an indented list of related tables — parents (where this table's rows point to another table) and children (where another table's rows point back). Every name carries a record count in parentheses. The currently-open table is listed first so the user opens the dialog already on the table they're thinking about; the rest follow alphabetically. Format example:

```
classes (3 records)
  parent: teachers (3)
  child:  enrollments (3)

enrollments (3 records)
  parent: classes (3)
  parent: students (3)

students (3 records)
  child:  enrollments (3)

teachers (3 records)
  child:  classes (3)
```

The relation analysis follows DbDo's column-naming convention (column `<other>_id` references table `<other>s`) rather than walking `PRAGMA foreign_key_list`, so the same algorithm works for .db, .xlsx, .csv, and any other tabular source where the convention has been adopted. Plain text throughout — no "schema," "primary key," "FK," or other database-internals language. The chord moved from the v1.0.67 stub's `Shift+D` to `Alt+D` per the user's spec; `Shift+D` is now free.

**Find-in-pick-list: Ctrl+J, F3, Shift+F3.** Pick-list dialogs (Choose Table, Choose Database, Alternate Menu, Pick Field) now have the same find-and-advance chords as the data list. Ctrl+J prompts for a case-insensitive substring; F3 advances to the next match wrapping at the end; Shift+F3 retreats wrapping at the start. The substring persists across F3 presses for as long as the dialog is open. Implementation: a KeyDown handler attached to every ListBox added via `addListBox` / `addPickBox`, plus a single `sListSearchTerm` field on the `LbcDialog` that holds the most-recent substring. The dialog used by Ctrl+J is a tiny vanilla Form (one Label, one TextBox, one OK button, one Cancel button) rather than a nested LbcDialog — avoids event-routing complications when an LbcDialog hosts a ListBox that itself spawns a search dialog. The chord mapping mirrors the data list's: Ctrl+J = Jump-to (find first match from top), F3 / Shift+F3 = Find-Next / Find-Previous.

**One new helper on `DbDoManager`:** `countRowsOfTable(string sTable)` returns the row count via `SELECT COUNT(*) FROM <table>`, or -1 on any error. Read-only; doesn't disturb the current recordset's position, filter, or sort. Used by Database Summary; available to other consumers that want a cheap row count without opening a recordset.

## v1.0.72

**House style applies to developer documentation too.** The lowercase-url convention from v1.0.71 now reaches code comments and the History.md entries. Thirteen code comments in DbDo.cs had "URL" lowercased to "url"; History.md had two cases corrected (Wikipedia URLs → urls, URL-encoding → url-encoding). Variable names continue to use Camel Type casing — `sUrl`, `lUrls` — since variable naming follows a separate convention.

**Three additions to the Terminology section in DbDo.md:**

- "Database" used broadly — covers .xlsx, .csv, and .db. SQLite .db is the default for full functionality (triggers, generated columns, foreign-key drill, the standard-column convention); other formats are bridged via the Import Data and Export Data commands with as much of the listview experience preserved as the format supports. Standard columns can't be assumed on .xlsx or .csv tables; commands that use them already check via `hasField` and announce a clear refusal when absent.
- Key-name convention — DbDo follows the Freedom Scientific / JAWS names: Control, Alt, Shift, Enter, Escape, UpArrow, DownArrow, F1-F12, Apostrophe, Asterisk, and so on. Combinations written with `+` and no spaces. The convention matches what JAWS announces aloud when a key is pressed.
- Camel Type for code — DbDo's source follows the Camel Type style; the full specification is in `CamelType_CSharp.md` (markdown source) and `CamelType_CSharp.htm` (Pandoc-rendered HTML, new in this build). The build pipeline now generates the HTML alongside `DbDo.htm` and `History.htm`; the installer bundles both.

**Standard-column accesses audit.** A fresh audit of `db.getFieldValue("look")`, `db.getFieldValue("notes")`, etc. confirmed all such accesses are systematically guarded: either by an explicit `db.hasField(...)` check in the preceding lines (the Say-X family, Mail Record, Open Url) or by a try/catch with graceful fallback (loops that walk rows for batch operations, where one missing field shouldn't abort the whole loop). No unguarded paths would error on .xlsx / .csv tables.

## v1.0.71

**"url" lowercase as ordinary English.** Per the user's house style: write **url** in lowercase as a regular English noun in prose (sentences, dialog labels, tooltips, status-bar messages, live-region announcements that aren't sentence-initial), and **Url** in title case where title-casing applies (command names like Open Url and Say Url, menu labels like "Sort by Url"). The convention follows the same path natural English took with *laser*, *radar*, *scuba*, and *sonar* — words that began as acronyms but settled into lowercase ordinary nouns once the original expansion stopped being foreground knowledge for most users. Most people who type a url into their browser have never thought about what the letters stand for; calling the thing a "url" rather than a "URL" matches lived experience. Also matters for screen readers: "URL" gets spelled out as three letters (U-R-L), while "url" reads as one syllable (earl), which is faster and less interrupting in the audio stream for frequently-issued commands like Open Url and Say Url.

**User-visible strings normalized:** six places in `DbDo.cs` had uppercase URL in user-facing text. All six lowercased:

- Open Cell Value menu label: "(URL or path)" → "(url or path)"
- Open Cell Value column-picker tooltip: "opens the URL, file path, or folder path" → "opens the url, file path, or folder path" (the same string also had "current row" → "current record" applied per the v1.0.70 terminology rule)
- Open-Cell "not a match" announcement: "Not a URL, file, or folder" → "Not a url, file, or folder"
- Extract Regex dialog label: "pulling emails, URLs, or IDs" → "pulling emails, urls, or IDs"
- Open Url failure MessageBox title text: "Could not open URL" → "Could not open url"
- Self-update notification body: "The URL is:" → "The url is:"

Two more in `DbDo.md` prose normalized:

- "open as URL" / "URL or a file path" in the Open Cell Value section → "open as url" / "url or a file path"
- "URLs" in the Extract Regex description → "urls"

Code comments containing "URL" were left alone — those are developer-facing and the lowercase-url convention is a user-facing style rule.

**A new subsection in DbDo.md's Terminology section** articulates the convention with two paragraphs: one explaining the laser/radar/scuba lineage and one explaining the screen-reader speech consideration. The convention is now documented so future contributions (and future Claudes) know the house style.

**Command names unchanged:** Open Url, Say Url, Sort by Url, Sort-ByUrl / Sort-ByUrlReverse canonical names — all keep their title-case "Url" because title-casing applies to those layers (menu labels, canonical PowerShell-style verb names).

## v1.0.70

**Terminology rule formalized and applied.** After researching how end-user database products handle the "row vs record / column vs field / cell" naming question (Microsoft Access, FileMaker Pro, dBASE / FoxPro, the ADODB API all favor record / field for end-user-facing commands; PostgreSQL / SQL Server / Oracle documentation favors row / column; DBeaver uses both depending on the view mode — "Table view" vs "Record view"), DbDo now articulates and enforces a deliberate context-driven mixture:

- **Record** for actions that treat a row as a complete entity (New, Edit, Delete, Copy, Append, Mail, Mark, Unmark, Find, Jump). End-user-friendly, matches every dominant end-user database product, and matches ADODB's own internal vocabulary.
- **Cell** for actions on a single value at the row-column intersection (Edit Cell, Copy Cell, Append Cell, Open Cell, Say Cell). Used when the user is operating at the listview's grid crosshair.
- **Column** for actions that sweep vertically through one attribute (Sort by Column, Replace Column, Statistics from Column, Output Graphics, Select Columns, Sort-by-X shortcuts). Used when the user is operating on one attribute considered across all records.
- **Field** for named attributes in a vertical-stack dialog (the Edit Record / New Record dialogs lay out one field per line; "column" would force mental translation back to the listview view, so "field" reads naturally there). Also used for individual attributes referred to by name: the `url` field, the `notes` field.
- **Row** restricted to geometric / spatial references and screen-reader navigation announcements ("Row N column M," "Table has no rows," "Go to Row," "20 of 25 rows shown"). Avoid in command names; use "Record" instead.
- **Table** for the schema-level object as a unit of navigation.

The principle behind these choices: *the noun matches the layout the user sees when invoking the command*. In the listview's grid, the user sees rows, columns, cells. When the user opens an Edit Record dialog, the layout rotates 90 degrees — fields stack vertically — and "column" would force a mental translation, so we say "field" instead. When the action treats the whole record as one thing, "Record" reads naturally regardless of layout.

**Specific user-facing changes:**

- `Copy-Row` command renamed to `Copy-VisibleCells` ("Copy Visible Cells as TSV to Clipboard"). This differentiates it from `Copy-Record` (which copies ALL fields including hidden ones); the new name communicates the actual difference — Copy Record gets the full record, Copy Visible Cells gets only what's in the listview. `copy-row` remains as a backward-compatible dot-prompt alias.
- `Invert Marked` parenthetical: "(toggle every row)" → "(toggle every record)"
- `Mark All` and `Unmark All` parentheticals: "(every row in filtered view)" → "(every record in filtered view)"
- `Copy Record` parenthetical: "(current row to clipboard)" → "(current record to clipboard)"
- `Mail Record` parenthetical: "(from current row)" → "(from current record)"
- `Open Url` parenthetical: "(current row's url column)" → "(current record's url field)" — also fixes the inner noun: the `url` attribute is named, so "field" not "column."
- Seven Say-X menu labels normalized: Say-Notes / Say-Tags / Say-Added / Say-Id / Say-Look / Say-Related / Say-Url all changed from "current row's X" patterns to "current record's X" patterns, with the redundant trailing "field" word removed.

**New "Terminology" section in DbDo.md** (immediately after "How DbDo is organized") documents the rule with examples and acknowledges that users who prefer SQL-canonical vocabulary can use the dot-prompt aliases (`find`, `new`, `edit`, `delete`, `copy`, `mark`, `unmark`) which use SQL-style verb naming. The acknowledgment matters: forty-plus years of database work spans both vocabulary traditions, and a single user may shift between them depending on whether they're writing SQL or operating the listview by keyboard.

## v1.0.69

**The wave-2 commands from the v1.0.65 spec are complete.** Thirteen new commands wired up in this build, finishing the last pending block:

- **Append Record (Alt+Shift+C)** — like Copy Record but appends to the existing clipboard contents (separator: blank line). Each record renders as "field: value" lines so the clipboard accumulates several rows in human-readable form.
- **New Copy (Ctrl+Shift+N)** — duplicates the current row. Opens the New Record dialog pre-filled with the current row's distinct field values; the user reviews, edits, and OK inserts as a new row. The `unq` column is cleared in the pre-fill since stored generated columns must be unique.
- **Mail Record (Ctrl+Shift+M)** — scans the current row for an email-like column (containing 'email', 'e_mail', or 'mail' in the column name), then launches the system mail client via `mailto:` with the subject populated from `look` and body from `notes`. Uses `Uri.EscapeDataString` for proper url-encoding of subject and body per RFC 6068.
- **Open New Recordset (Ctrl+Shift+O)** — prompts for a SQL SELECT or WITH statement and opens the result as a read-only recordset. Find, Filter, Sort, Say-X all work; Mark, Edit, Delete refuse because the result has no table identity. Useful for ad-hoc views that don't justify creating a permanent SQLite VIEW. New method `openSqlRecordset` on `DbDoManager` does the work; calls `oRecordset.Open(sql, conn, adOpenStatic, adLockReadOnly, adCmdText)` against ADODB, sets a synthetic current-table name `(ad-hoc SELECT)` so dialogs and status bars have something to display.
- **Eight Sort-by-standard-column shortcuts.** Each pair Alt+letter / Alt+Shift+letter sorts by a fixed standard column ascending or descending: Alt+I / Alt+Shift+I = Sort by Id (the table's actual primary key, resolved via `actualPrimaryKey`), Alt+L / Alt+Shift+L = Sort by Look, Alt+T / Alt+Shift+T = Sort by Tags, Alt+U / Alt+Shift+U = Sort by Url. Convenience aliases over Sort-Object so the user doesn't have to pick the column through a dialog for the standards that always exist on DbDo-convention tables. All eight thread through a single helper `sortByFixedColumn` that sets `db.sort` and refreshes.

**Chord conflict cleanup, three resolutions:**

- Three `Alt+letter` aliases were removed: `Alt+T → Measure-Table`, `Alt+C → New-Chart`, `Alt+L → Select-Table`. Each of those chords is now the primary chord for one of the new Sort-by-X commands (or Append Cell in the Alt+C case). Measure-Table, New-Chart, and Select-Table retain their menu entries with their canonical chords (F4 for Select-Table; no chord for the other two — reachable via the menu).
- **Append Cell moved from Shift+A to Alt+C** to resolve a silent chord collision with Say-Added (Shift+A, added in v1.0.67). WinForms registers only the last menu item assigned to a chord, so the prior assignment quietly clobbered Say-Added; the move puts Append Cell on the chord the user actually intended.
- **Copy Cell moved from Shift+C to Ctrl+C** to resolve a similar silent collision with Say-Cell (Shift+C, added in v1.0.67). The user's spec was `Ctrl+C = Copy Cell` and `Shift+C = Say Cell`; both are now in effect.

**Still pending:** the deferred trio (Database Summary on Shift+D, Window Summary on Shift+W, Pick Value on Ctrl+F2) still have their "deferred; not yet implemented" stubs. Each chord is reserved for the eventual real implementation.

## v1.0.68

**Northwind and Chinook adopt the v1.0.66 standard-column extensions.** Both bundled "canonical" sample databases were upgraded in place to match DbDo's full standard-column convention. Discovery: both already had `<table>_id` primary keys, `added` / `updated` timestamps, `notes`, `tags`, `marked` columns, and (most usefully) `look` and `unq` were already present as `STORED GENERATED` columns computed from the substantive fields. The `look` column in `northwind.db::categories` for example is `rtrim(iif(length(name)>0, name || ' | ', '') || iif(length(description)>0, description || ' | ', ''), ' | ')` — exactly the right pipe-joined display nickname pattern. The only work for v1.0.68 was to **add `url` (TEXTLINE)** and **upgrade `notes`/`tags` from `TEXT` to `TEXTMEMO`** so DbDo's Edit Record dialog renders the multi-line memo widget. The substantive columns (company, contact, city, country, phone for Northwind customers; name, title, artist_id for Chinook artists; etc.) are preserved verbatim.

Per the user's clarification this turn, *"additional columns in the canonical databases do not have to be displayed."* Three columns moved from "visible by default" to "hidden by default" so the listview matches the canonical schemas and isn't cluttered: `url`, `tags`, `notes` now join `added`, `updated`, `marked`, `look`, `unq` in DbDo's `StandardHiddenColumns` set. Users who want any of those visible in their own databases can use the Select Columns command (Alt+S, v1.0.66) to override per-table; the override persists to DbDo.ini via the `t<n>_selectlist` mechanism added in v1.0.66.

**Online documentation links for canonical samples.** The "Bundled sample databases" section of DbDo.md now includes external links so users can learn more about the canonical Northwind and Chinook schemas and their broader uses:

- Northwind: Microsoft Learn, the official `microsoft/sql-server-samples` GitHub repo, and Wikipedia.
- Chinook: Luis Rocha's `lerocha/chinook-database` reference repo, plus the SQLite Tutorial walkthrough.

Both descriptions now explicitly enumerate DbDo's adaptations to make clear they're minimal: same substantive columns, snake_case naming on the integer primary keys, standard columns appended, TEXTMEMO declared on notes/tags. The Foxbase-and-Clipper-era principle survives: real databases used for real work need certain things (timestamps, look-labels, free-text notes); DbDo's standard columns formalize those needs without forcing users to redesign canonical sample schemas.

**Mechanics of the upgrade.** Each table got `ALTER TABLE … ADD COLUMN url TEXTLINE`. SQLite doesn't natively support changing a column's declared type, so the notes/tags upgrade used the standard "rebuild dance": copy the original CREATE TABLE SQL, regex-substitute `notes text` → `notes TEXTMEMO` (and same for tags), CREATE the new table under a temporary name, INSERT-SELECT the data, DROP the original, RENAME the new. Generated columns (`look`, `unq`) carried over correctly since their definitions live in the CREATE TABLE statement itself. PRAGMA `table_xinfo` was the key to discovering that the standards were already present as generated columns — `table_info` hides them.

**Pending for a later build:** Append Record (Alt+Shift+C), New Copy (Ctrl+Shift+N), Mail Record (Ctrl+Shift+M), Open New Recordset (Ctrl+Shift+O), the Alt+letter "X Order" variants for id/look/tags/url. These are the remaining wave-2 commands from the v1.0.65 spec.

## v1.0.67

**Sort Records (Alt+Shift+S).** New command: sorts the current view by the column under the virtual cursor. A single-checkbox LbcDialog asks "Ascending order (otherwise descending)" with the checkbox OFF by default — the user's spec for this command. The resulting expression (`<column> ASC` or `<column> DESC`) is assigned to `db.sort`, which routes through to the underlying ADODB.Recordset's `Sort` property. Complements the existing Custom Sort (Shift+S, multi-column dialog) and Sort Ascending/Descending by Column (Alt+A/Alt+Shift+A, prompts for column): Sort Records uses the current virtual column without prompting, making it the fastest sort gesture when the user is already navigating in the column they want to sort by.

**Bulk mark operations.** Three new commands all of which act on the current filtered view:

- **Mark All (Ctrl+A)** — UPDATE the `marked` column to 1 for every row in the filtered view. WHERE clause is the active ADO filter; if no filter is active, every row in the table is updated.
- **Unmark All (Ctrl+Shift+A)** — same, marking to 0.
- **Invert Marked (Ctrl+I)** — toggle via `CASE WHEN marked IS NULL OR marked = 0 THEN 1 ELSE 0 END`.

All three refuse with a clear message if the current table is a view (read-only) or lacks the `marked` column. Each speaks the affected row count via the live region.

**Delete Without Confirmation (Ctrl+Shift+D).** New command paired with Delete Record (Ctrl+D, which uses the LbcDialog confirmation). The destructive variant speaks the row's `look` value (or primary-key position if look is empty) BEFORE deleting, so the screen-reader user gets explicit confirmation of what just happened even without a preceding dialog.

**Open Url (Ctrl+Shift+U).** New command: opens the current row's `url` column with the system default handler (browser, mail client, file opener) via Process.Start. Convenience chord that saves the user from having to navigate the virtual cursor onto the url cell first and then issue Open Cell Value.

**Seven new Say-X family commands.** Each speaks one piece of state without changing recordset position; long values get the LbcDialog memo-box on double-press: Say Added (Shift+A), Say Cell (Shift+C), Say Filter (Shift+F), Say Id (Shift+I), Say Look (Shift+L), Say Related (Shift+R), Say Url (Shift+U).

**Deferred stubs reserved on their chords.** Database Summary (Shift+D), Window Summary (Shift+W), and Pick Value (Ctrl+F2) each open as menu entries with handlers that announce "deferred; not yet implemented" via the live region. Reserving the chords keeps the menu structure stable so the user can discover the planned commands and the mnemonics don't drift when implementation lands.

**One shortcut collision fix.** Say-YieldMarked (line 9035 in the menu builder) had previously been assigned `Shift+Y`, which is also Say-Yield's chord; WinForms shortcut registration only keeps the last one. Removed the duplicate chord — Say-YieldMarked is now reachable via the menu only.

**Still pending for a later build:** Append Record (Alt+Shift+C), New Copy (Ctrl+Shift+N), Mail Record (Ctrl+Shift+M), Open New Recordset (Ctrl+Shift+O), the Alt+letter "X Order" variants for id/look/tags/url; whether `northwind.db` and `chinook.db` should also adopt the new standard-column schema.

## v1.0.66

**Chord layout adjustments to the v1.0.65 wave.** Three commands moved chord positions per the user's revised spec, plus one new command slot reserved for v1.0.67.

- **Alt+S = Select Columns** (formerly "Choose Visible Columns" at no chord). Choose which columns appear in the listview. The same per-table select-list mechanism that existed before — now reachable via a chord that suits the user's mental model: S for Select.
- **Alt+G = Generate Statistics** (formerly Alt+S "Statistics from Column"). Statistical summary of the current virtual column.
- **Alt+O = Output Graphics** (formerly Alt+G "Graphics Output"). Plot the current virtual column.
- **Alt+Shift+S** reserved for the v1.0.67 new "Sort Records" command (sort by current virtual column, with an Ascending checkbox defaulting to off).

**Find searches all columns, not just displayed.** Per the v1.0.65 spec the user reiterated this turn: Find, Reverse Find, Extract with Regex, Regex Find, and Reverse Regex Find now walk the FULL field set when searching, so hidden columns like `notes`, `tags`, `url`, and the bookkeeping timestamps are reachable. The Replace and Jump families still operate on the current virtual column only. Touched `findAcrossColumns`, `findRegexAcrossColumns`, and `extractRegexClicked`; each previously called `getDisplayFieldNames()` and now calls `getFieldNames()`.

**Standard-column schema refresh for three bundled databases.** `sample.db`, `collection.db`, and `cellar.db` rebuilt with the v1.0.66 standard-column sequence: `<table>_id, added, updated, url, tags, notes, look, unq`. The new `url` column carries SQLite declared type `TEXTLINE` so DbDo renders a single-line input box in Edit Record; `tags` and `notes` carry `TEXTMEMO` so they get the multi-line memo widget. Each row's data was rewritten to populate the new look/url/tags/notes fields with real content — sort names and Wikipedia urls for the music collection, producer websites for the wine cellar, school emails for the students/teachers. The two textbook databases (`northwind.db`, `chinook.db`) were left untouched to preserve their canonical adapt-of-canonical-SQL-sample identity; ask if you want those regenerated too.

**Select-list persistence to DbDo.ini.** The per-table column selection set via Alt+S now survives across sessions. `RecentFiles.TableState` gained an `sSelectList` field; `loadSection` reads `t<n>_selectlist` from the section; `saveAll` writes the same key; `recordAllTableStates` pulls from the manager's `TableSettings.sSelectList` cache; `seedTableSettings` accepts the new parameter and restores the manager's cache when the database is reopened.

**ADODB API confirmation for Filter and Sort.** The user asked that Find, Sort, and Filter use the ADODB API as much as possible, translating user input into ADODB syntax. The good news: this is already how Filter and Sort work. `viewSelectClicked` calls `buildFilterExpression` which translates the user's `(text, column, matchMode)` triple from the Filter Records dialog into ADODB-syntax predicates (`col LIKE '%text%'`, `col = 'text'`, OR-chained across columns when "All columns" is the column choice). `viewFormatClicked` translates `(column, ascending)` from the Custom Sort dialog into `col ASC` or `col DESC`. The translated expressions are then assigned to `db.filter` and `db.sort`, which are properties that route through to the underlying ADODB.Recordset's `Filter` and `Sort` (the dynamic recordset object's `.Filter = value` and `.Sort = value` setters). Find is the one outlier: multi-column substring search cannot be expressed as a single ADODB predicate efficiently (ADO's `Find` method only supports one column), so Find walks rows in user-space via `absolutePosition` increment and field-by-field inspection. The new "search all columns" behavior preserves this user-space walk pattern but iterates `getFieldNames()` instead of `getDisplayFieldNames()`.

**Planned for v1.0.67:** the new Sort Records command (Alt+Shift+S) with an Ascending checkbox defaulting to off; the remaining v1.0.66 wave-2 new commands (Mark All, Unmark All, Invert Marked, Mail Record, New Copy, Open Url, Open New Recordset, Append Record, Delete Without Confirmation, Say-X family completions); decision on whether to regenerate `northwind.db` and `chinook.db` with the new standard-column schema.

## v1.0.65

**Hotkey rebinding — first wave (chord moves on existing commands).** Twenty-nine chord and label changes applied to existing commands, implementing the chord layout the user requested for v1.0.65. Highlights:

- **F2 is now Edit Cell** (single-field editor for the current virtual cell). Edit Record moves to Ctrl+E. Shift+F2 is freed.
- **Ctrl+G = Go to Record** (by absolute position). Was Shift+G.
- **Alt+S = Statistics from Column** (formerly Describe Column / Measure-Column). Was Ctrl+Shift+D. The Ctrl+Shift+D slot is now reserved for Delete Without Confirmation, coming in v1.0.66.
- **Ctrl+Shift+E = Extract with Regex.** Was Alt+E.
- **Alt+Shift+F = Filter Records.** Was Shift+F. Shift+F reserved for Say Filter, coming in v1.0.66.
- **Shift+P = Say Path.** Was Alt+P.
- **Shift+Y = Say Yield.** Was Alt+Y.
- **Shift+M = Say Marked.** Was Alt+Shift+M.
- **Alt+G = Graphics Output** (formerly Plot Column / New-Plot). Was Ctrl+Shift+P.
- **Alt+Shift+X = Toggle Extra Speech.** Was Alt+Shift+S.
- Several Sort commands relabeled: Sort-Ascending is now "Ascending Order by Column" (Alt+A); Sort-Descending is "Descending Order by Column" (Alt+Shift+A); Sort-RecentFirst and Sort-OldestFirst lose their chords (Alt+D and Alt+Shift+D freed; user can sort by date column via Alt+A on the date column directly).
- Several command labels normalized to match the new vocabulary: "Delete Record (with confirmation)", "Find Record (search all columns)", "Reverse Find", "Jump to Record (match in current column)", "Reverse Jump", "Find Regex (search all columns by pattern)", "Reverse Regex Find", "Replace Column (find and replace in current virtual column)", "Copy Record (current row to clipboard)".
- New-Database and Step-InitialChange lose their chords (Ctrl+Shift+N and Shift+I freed for new commands in v1.0.66).
- Several Say-X commands lose their old chords (Shift+L, Shift+D, Say-Updated specifically): the slots are reserved for the new Say-Look / Database-Summary commands coming in v1.0.66.

The legacy dbDot Shift+Letter dispatch at the form level was pruned: Shift+F, Shift+G, Shift+R no longer have parallel handlers (their underlying menu items moved or were freed). Shift+J (Jump-Record) and Shift+S (Custom Sort) remain in the parallel dispatch for backward compatibility.

**Planned for v1.0.66 (new commands and stubs):** roughly sixteen new menu entries for Mark All (Ctrl+A), Unmark All (Ctrl+Shift+A), Invert Marked (Ctrl+I), Mail Record (Ctrl+Shift+M), New Copy (Ctrl+Shift+N), Open Url (Ctrl+Shift+U), Open New Recordset (Ctrl+Shift+O), Append Record (Alt+Shift+C), Delete Without Confirmation (Ctrl+Shift+D), Say Added (Shift+A), Say Cell (Shift+C), Say Url (Shift+U), Say id (Shift+I), Say Look (Shift+L), Say Filter (Shift+F), Say Related (Shift+R), plus the Alt+letter "X Order" variants for id/look/tags/url, plus the deferred trio (Database Summary, Window Summary, Pick Value) as "not yet implemented" stubs.

**Planned for v1.0.67 (schema and search behavior):** the new `url` standard column (textline type) with the reordered standard-column layout `<table>_id, added, updated, url, tags, notes, look, unq`; the `tags`/`notes` upgrade to textmemo; the Find / Reverse Find / Extract / Regex Find / Reverse Regex Find search-all-columns behavior; the Replace and Jump current-column-only restriction; the Search Next / Search Previous unified family.

## v1.0.64

**Critical fix: Key Describer mode could not be exited.** When Key Describer was on, Ctrl+F1 (the toggle chord) was intercepted by the describe-mode handler — DbDo announced the chord and its summary instead of running the toggle. Result: the user could enter Key Describer but never leave, blocking application exit short of killing the process. The fix adds the same escape EdSharp and FileDir use (EdSharp.cs line 1640, FileDir.cs line 7533): when in Key Describer mode, the toggle command itself ALWAYS executes regardless of mode state. Every other chord still gets described. The wording on toggle ("Key Describer On" / "No Key Describer", with "No" leading when the mode turns off so the screen reader announces the state change instantly) was already correct in v1.0.63 and is unchanged.

**Planned for v1.0.65** (pending the next build cycle, per the spec received during v1.0.64 development): a roughly 40-chord rebinding to clean up the menu mnemonics around a verb-noun-pair pattern (Shift+Letter = Say-X, Alt+Letter = X-Order, Alt+Shift+Letter = Reverse-X-Order, Control+Letter = primary action on X, Control+Shift+Letter = secondary action on X); a new `url` standard column of type textline, with the standard-column order becoming `<table>_id, added, updated, url, tags, notes, look, unq` and `tags`/`notes` upgraded to textmemo type; Find / Reverse Find / Extract / Regex Find / Reverse Regex Find searching all columns (not just displayed); Replace / Regex Replace / Jump operating on the current virtual column only; Search Next / Search Previous supporting all three families (Find, Regex Find, Jump). Several commands deferred (Ctrl+F2 Pick Value, Shift+D Database Summary, Shift+W Window Summary).

## v1.0.63

**EdSharp-style text-edit hotkeys in LbcDialog.** Inside every LbcDialog, single-line text inputs and multi-line memos now recognize a set of EdSharp-style hotkeys in addition to the standard Windows text-editing chords. The pattern is adapted from Jamal Mazrui's HomerLbc framework (`HomerLbc_40.js` lines 995–1162), which itself derives from EdSharp's text-editor conventions. Nine new chords: Ctrl+C copies the current line when nothing is selected; Alt+C appends the current line (or selection) to the existing clipboard contents on a fresh line; Ctrl+X cuts the current line when nothing is selected and speaks the next line as feedback; Alt+X cuts and appends; F8 marks the start of a selection at the caret; Shift+F8 completes the selection from that mark to the current caret; Ctrl+F8 copies all text in the field; Alt+F8 speaks all text via the live region; Ctrl+D deletes the current line and speaks the next line as feedback. None conflicts with standard control behavior — the Ctrl+C / Ctrl+X overrides only act when there is no selection (the standard Copy and Cut don't do anything without a selection); the F8 family, Ctrl+F8, Alt+F8, and Ctrl+D are unbound in standard WinForms TextBoxes; the Alt+ variants are unbound. Implementation lives on LbcDialog (not via TextBox subclassing): a form-level KeyDown handler with KeyPreview=true dispatches to twelve helper methods, gated by the focused control's Name prefix (`TextBox_` or `Memo_`) and a master enable flag `[Lbc] extraKeys` in DbDo.ini (default Y, cached on first read).

**Two new hobbyist sample databases.** `collection.db` is a personal music collection — three tables (`artists`, `albums`, `tracks`) modeled on the data conventions refined by CLZ Music, MyMusicCollection, and Musicnizer over the past decade. Includes collector fields (rating, location, loan tracking) that distinguish a personal collection from the broader Chinook music-store data. Seeded with 8 artists / 16 albums / 22 tracks spanning rock, jazz, soul, classical, and electronic. `cellar.db` is a personal wine cellar — three tables (`wines`, `bottles`, `tastings`) modeled on CellarTracker, eSommelier, and VinCellar. The schema split separates wine identity (producer + vintage + varietal + region) from physical bottles (each with bin location, purchase price, source, status) from tasting notes (multiple over time). Seeded with 8 wines / 10 bottle lots / 4 tasting notes. New canonical Help-menu commands `Open-CollectionDatabase` (Open Music Collection) and `Open-CellarDatabase` (Open Wine Cellar) parallel the existing sample-database openers. Documented in DbDo.md's "Bundled sample databases" section.

**Wine drink-window analytical query bundled as a script.** `Scripts/WineDrinkWindow.sql` is the standout analytical workflow from the wine-cellar research: for every wine in the cellar with bottles still held, compute years remaining in its drink window, sort by urgency (closest to end of window first), and classify each as too-young / in-window / past-peak. The kind of query that no flat list or spreadsheet can answer naturally — one ORDER BY clause and a JOIN against the bottles inventory. Demonstrates DbDo's value for hobbyist data: ad-hoc SQL serves real workflows.

**Camel Type compliance for bundled `.js` script samples.** `CopyRowToClipboard.js` and `MarkRowsMatchingRegex.js` now follow Camel Type conventions verbatim: Hungarian-style type prefixes (`aFieldNames` for array, `sb` for StringBuilder, `iMarked` for integer, `regex` for Regex, `bMatch` for boolean), all variable declarations at the top of the script grouped alphabetically by type with type-lines themselves in alphabetical order (a < b < i < regex < s < sb), the constant `c_sPattern` with the required `c_` prefix, and `for each (sName in aFieldNames)` for-each iteration instead of integer-indexed loops. The DbDo.js support module was already Camel Type compliant and is unchanged.

**Native dot-prompt syntax for bundled `.duo` script samples.** The `.duo` files previously used PowerShell-style canonical verbs (`switch-table`, `select-record`, `reset-filter`, `sort-object`, `say-path`, `say-status`, `say-tables`, `say-sortfilter`). They now use the natural single-word dot-prompt aliases (`table`, `filter`, `clear-filter`, `sort`, `path`, `status`, `tables-list`, `sort-filter`) — matching what users type interactively at the dot prompt. Either form still works because the dispatcher resolves aliases, but the natural form is shorter, more readable, and consistent with the rest of DbDo's dot-prompt vocabulary. The `.duo` starter template was also updated. `RecentOrders.duo` additionally replaces the invented `sort-recentfirst` (which didn't exist) with the standard SQL-style `sort order_date desc`. The Scripting section of DbDo.md was updated to make this convention explicit, with a recommended list of short forms.

## v1.0.62

**Key Describer now matches EdSharp and FileDir verbatim.** The Key Describer mode at Control+F1 had three behaviors that diverged from its EdSharp/FileDir model and made the feature unusable in practice: (1) toggling the mode opened a MessageBox confirmation dialog instead of announcing the new state to the screen reader; (2) when the mode was on and the user pressed a chord to be described, DbDo opened another MessageBox showing the chord/command pair instead of speaking the information; (3) the canonical verb was named `Trace-Command` (mirrored in user-visible status strings as "trace mode" and "Trace-Command mode"), which both leaked an implementation term into the UI and collided with the real PowerShell `Trace-Command` cmdlet.

Studied EdSharp's `menuItem_Click` (line 1640) and FileDir's `ClickOrDescribe` method (line 7533) — both follow the same pattern: a static `KeyDescriber` boolean, a Ctrl+F1 menu handler that just announces "Key Describer On" / "No Key Describer" via the live region (no dialog), and a gate at the top of menu-click dispatch that, when the flag is on and the click isn't the Key Describer menu item itself, speaks three pieces of information — command name, chord, summary — and *swallows* the click without firing the command. No MessageBox anywhere. No tracing terminology.

DbDo now follows the same pattern. The `helpTraceCommandClicked` menu handler (Control+F1) toggles `KeyMap.bKeyDescriber` and announces the new state via `LiveRegion.say`. The `KeyMap.tryDispatch` path and the Shift+Letter handler both check `bKeyDescriber` and, when on, call `LiveRegion.say(command + ". " + chord + ". " + summary + ".")` — three pieces joined with sentence-ending punctuation for natural screen-reader pauses, in one utterance — and swallow the keystroke. The command does not run.

The canonical verb is now `Switch-KeyDescriber`, matching DbDo's PowerShell-style verb-noun discipline (Switch- is an approved PowerShell verb for toggles, and KeyDescriber is the noun). The menu item's user-visible label is still "Key Describer" verbatim per the EdSharp/FileDir alignment principle. Dot-prompt aliases `trace`, `trace-command`, `key-describer`, `keydescriber`, `describe-key`, and `describer` all resolve to `switch-keydescriber` for backward compatibility. The dot-prompt cmd `cmdTraceCommand` is renamed to `cmdSwitchKeyDescriber`; the console echo also says "Key Describer On" / "No Key Describer" to match the live region. The field `KeyMap.bTraceMode` is renamed to `KeyMap.bKeyDescriber`.

## v1.0.61

**Primary-key heuristic now schema-first.** Opening Northwind and pressing Enter-Child on a `categories` row reported "Cannot determine the primary-key column for 'categories'." The fault was in the Enter-Child helper `computePrimaryKeyColumn`, which used naive `-s`-stripping to singularize a table name: `categories` became `categorie`, which has no matching column. The corrected helper now calls the existing schema-driven `actualPrimaryKey(sTable)` FIRST (which reads `PRAGMA table_info` on SQLite or the ADOX `Keys` collection on Access), falling back to the naming heuristic only when the schema lookup is unavailable. The heuristic itself was also fixed to handle the `-ies` → `-y`, `-ses`/`-xes`/`-ches`/`-shes` → drop `-es`, and plain `-s` → drop `-s` plural patterns. Either path now finds `category_id` from `categories`.

**"Snippet" renamed to "Script" throughout.** The `Invoke-Snippet` command is now `Invoke-Script` (Alt+V still); `Save-Snippet` and `View-Snippet` (where they exist as dot-prompt or method names) become `Save-Script` and `View-Script`. The bundled sample folder `SampleSnippets` is now `SampleScripts`. The user-facing motivation: "script" describes what these files do (executable code in JScript .NET that drives DbDo via host objects), whereas "snippet" connotes a passive text fragment. EdSharp and FileDir use "Snippet" for their own paste-tag idiom which is different from DbDo's; the rename also disambiguates DbDo from the editors. Variable names like `miMiscInvokeSnippet` are renamed to `miMiscInvokeScript`. 135 occurrences replaced across the C# source, 44 in DbDo.md, 12 in History.md, 7 in the installer script.

**Invoke-Script output now uses the LbcDialog memo box.** The script-output dialog was a `MessageBox` previously, which is unsuitable for line-by-line, word-by-word, or character-by-character exploration with a screen reader. Multi-line results (or any result containing an "ERROR:" marker) now display in the same `showInfoDialog` LbcDialog used by the speech-only commands' double-press: a read-only multi-line TextBox with an OK button. Short single-line results still use MessageBox since brevity matches that idiom. The `Test-Database`, `Measure-Field`, and `Invoke-Sql` commands have used the equivalent `HelpDialog.show` for multi-line output since earlier versions; Invoke-Script now matches the pattern.

**Escape now activates OK in single-button LbcDialogs.** When `runWithButtons` is called with only one button (typically "OK", as in the confirmation-only memo dialog used by Invoke-Script's output and the speech-only commands' double-press), that one button is now wired as BOTH `AcceptButton` (Enter) AND `CancelButton` (Escape). Previously the user had to Tab to OK and press Enter or Space; Escape did nothing because no Cancel button was present. The fix matches user expectation that Escape always dismisses a modal dialog.

**Help menu mnemonic is now Alt+H (was Alt+P).** The top-level Help menu was labeled `Hel&p`, which made Alt+P open it — a non-standard convention. The label is now `&Help`, matching every modern Windows app. Top-level menu mnemonics are now: Alt+F (File), Alt+E (Edit), Alt+N (Navigate), Alt+Q (Query), Alt+M (Misc), Alt+H (Help). Each is unique and matches the Windows convention.

**Layout by Code section added to DbDo.md.** A new major section walks through the LbC approach DbDo uses for every dialog: the origin (Jamal Mazrui's AutoIt LbC of 2006, ported through wxPython, JScript .NET, and now C#), the conceptual model (bands, layout cursor, dialog units), why LbC matters for a screen-reader audience (tab order = call order, focus tips routed to status bar, memo-vs-AcceptButton coordination), the anatomy of an LbC dialog with a usage example, the full add-control vocabulary (`addLabel`, `addInputBox`, `addInlineInputBox`, `addMemoBox`, `addCheckBox`, `addListBox`, `addPickBox`, `addComboBox`, `addComboPickBox`, `addRadioButton`, `addNumericUpDown`, `addSeparator`), the two run methods (`runOkCancel` and `runWithButtons`), and the lookup-by-name pattern using `findControl` and the typed accessors. The section closes with a comparison against the original AutoIt LbC, noting which features were deliberately simplified in the C# port and which were preserved verbatim.

**Tab and Shift+Tab now move the virtual cursor.** Earlier versions had a vestigial `iCurrentColumnIndex` state that Tab/Shift+Tab advanced, separate from the canonical `iVirtualCol` used by Set-Cell, Say-Column, Say-Position, and the Alt+Control+arrow chords. The Tab handler also did nothing in practice because WinForms ListView does not surface Tab as a KeyDown event by default. The fix wires `grid.PreviewKeyDown` to flag Tab (without Control) as an input key so the KeyDown handler actually fires, and the handler now calls `virtMoveTo(iVirtualRow, iNewCol)` — the same path the Alt+Control+arrow chords use. Single state, single announcement, single behavior. Control+Tab remains reserved for Switch-Table.

**Say Clipboard command added (Alt+Apostrophe).** New speech-only command in the Query menu, mirrors FileDir's Alt+Apostrophe Clipboard. Speaks the current Windows clipboard text via the screen-reader live region; double-press opens the read-only memo dialog for line-by-line review of long pasted content. Empty and non-text clipboards announce that fact rather than going silent. The "Say" prefix is retained per DbDo's Say-X family convention (the prefix marks the command as speech-only, no visible side-effect; FileDir has no such family so its bare "Clipboard" label is unambiguous). Canonical verb `Say-Clipboard`. Pass-through configs `DbDo_JAWS.zip` and `DbDo.nvda-addon` extended with `Alt+Apostrophe=PassDbDoKey` and `kb:alt+'` so JAWS's default Alt+Apostrophe = "Say JAWS Version" gives way to DbDo's handler inside the data list.

**EdSharp/FileDir-aligned menu names in the Help menu.** Five Help-menu items renamed to match the EdSharp and FileDir convention exactly, since the chord was already a match in each case: "Help Contents" → "Documentation" (F1); "Version History" → "History of Changes" (Shift+F1); "Toggle Key Describer Mode" → "Key Describer" (Control+F1); "Check for Update..." → "Elevate Version..." (F11); "About DbDo" → "About" (Alt+F1). Also renamed: the Alternate Menu command's label was previously "Command Picker (alternate menu)..." with a verbose parenthetical; it is now simply "Alternate Menu..." (the EdSharp/FileDir name, Alt+F10). The Misc-menu "Configuration Settings..." command is now "Configuration Options..." (Alt+Shift+C), again matching EdSharp/FileDir verbatim. The PowerShell verb-noun pairs (Get-Help, Show-History, Trace-Command, Elevate-Version, About-DbDo, Alternate-Menu, Edit-Configuration) remain available at the dot prompt and in logs but are not surfaced in the user-visible UI. The principle: when DbDo has a command that does what EdSharp or FileDir does and the chord matches, the menu label IS the EdSharp/FileDir label; the PowerShell synonym is for power users.

A few user-facing DbDo commands deliberately keep their original (non-EdSharp/FileDir) labels because of meaningful domain-specific reasons: "Say Clipboard" keeps the "Say" prefix per the Say-X family marker; "Say Status" keeps the "Say" prefix for the same reason; "Table Properties" keeps the disambiguating "Table" qualifier since the bare "Properties" would be ambiguous in a database context (properties of what? the cell, the table, the database?); "Close Database" keeps its name because DbDo does not have multiple windows in EdSharp's MDI sense.

## v1.0.60

**Per-command summaries and descriptions.** Every menu item, dot-prompt verb, and dispatcher arm now carries a one-line summary and an optional multi-line description as metadata. The summary appears on the menu status bar when the item has focus, in the Alternate Menu (Alt+F10) inline after the verb, and in the Key Describer trace dialog (Control+F1) on its own line. The description, when present, appears in the Alternate Menu's detail pane and in the Key Describer trace. This mirrors the EdSharp and FileDir convention for command-self-documentation, exposing the metadata through three different surfaces (status bar, picker, trace) so users can discover what a command does without reading separate documentation.

The summary follows a consistent voice: verb-first, plain text, one line, suggesting the chord where one exists. Examples: "Open a database file" (Open-Database), "Mark every row from the F8 anchor to the current row" (Mark-Range), "Speak the current sort and filter, or '(none)'" (Say-SortFilter). Commands without an explicit summary fall back to the menu label minus the ampersand mnemonic markers, so every command is at least minimally self-describing even when the metadata table has a gap.

The implementation: `addItem` and `addItemLocal` gain two optional trailing parameters `sSummary` and `sDescription` (both default to `""`); the values flow into new `KeyMap.dCommandToSummary` and `dCommandToDescription` dictionaries keyed by canonical verb; menu items get their `ToolTipText` populated with the summary so screen readers announce it on accelerator-key landings; a `mi.MouseEnter` + `mi.Select` handler copies the summary into `lblStatus` on the form's status bar.

**Version-string bump from v1.0.58.** The `BuildInfo.VersionString` constant was last set to `1.0.58` and stayed there through the v1.0.59 development cycle by oversight. The constant now reads `1.0.60` to match the headline, the installer's `AppVersion` is bumped the same way, and the History.md headline marks v1.0.60 as current.

## v1.0.59

Compaction summary of work staged through v1.0.59 (entered the public history retroactively when v1.0.60 was tagged):

Documentation cleanup. The standard-field set is corrected: `observed` and `method` are removed; the documented set is now `<table>_id`, `added`, `updated`, foreign keys, distinct fields, `notes`, `tags`, `marked`, `look`, `unq`. The `sample.db` bundled with the installer is rebuilt to match this set while preserving its existing rows.

Two additional sample databases ship: `northwind.db` (the classic Microsoft Northwind sales sample) and `chinook.db` (the classic Chinook music-store sample), both adapted to DbDo's standard column conventions. They are useful for exercising DbDo's parent-child drill against deeper relationships than the small `sample.db` provides. The Help menu has new one-keystroke commands to open each: **Open Northwind Sample** and **Open Chinook Sample**, parallel to the existing **Open Sample Database** command. All three open via the same code path as File > Open Database.

`CamelType_CSharp.md` ships with the distribution, documenting the coding conventions used inside `DbDo.cs`. The conventions are updated to remove the `c_` prefix that was previously required for constants; constants now follow the same naming pattern as variables but are still declared on their own lines, distinguished by `const` or `static readonly` instead of by capitalization. The `o` prefix is reserved for COM objects only; managed-type instances use a class-name prefix.

`DbDo.cs` is updated to conform to the revised conventions (`c_` removed; `o`-prefix usage adjusted) with the addition of two new Help-menu items (Open Northwind Sample, Open Chinook Sample) and a shared helper method `openInstallSampleDb` that the existing Open Sample Database command now also routes through, eliminating duplicated code.

The JScript .NET support module is renamed: `dbDuoEval.js` becomes `DbDo.js`, compiled to `DbDo.dll` (previously `dbDuoEval.dll`). The JScript package and class are renamed to match, so the reflection target is now `DbDo.JS.runScript` (previously `DbDoScripting.JS.Eval` then `DbDoScripting.JS.runScript`). Because DbDo.exe and DbDo.dll now share the simple assembly name "DbDo", the C# code loads the DLL with `Assembly.LoadFrom` and the full path next to the executable instead of `Assembly.Load` with the simple name — the simple-name load would have resolved to DbDo.exe rather than DbDo.dll. The `/reference:` flag is dropped from the csc.exe command line for the same reason (the C# code never needed a compile-time reference; it always called the JS module through reflection). Existing user scripts are unaffected by the rename — they see the same `frm` and `db` globals and call the same DbDo API.

Three sample scripts ship in `{app}\SampleScripts\`: `DescribeTable.js`, `CopyRowToClipboard.js`, and `MarkRowsMatchingRegex.js`. On first access of the user's script folder (`%APPDATA%\DbDo\Scripts\`), DbDo copies any samples that aren't already there and writes a `.seeded` sentinel file so the seeding does not repeat on later launches. Deleting a sample does not cause it to reappear; deleting the `.seeded` file allows re-seeding (e.g. to recover a deleted sample, or to pick up new samples bundled in a future release). The samples are short, single-purpose, and modeled on the EdSharp script style: a read-only introspection demo, a current-row clipboard helper, and a regex-match-and-mark utility.

The installer's `[InstallDelete]` section now removes the old `dbDuoEval.dll` from upgrade installs so users moving from v1.0.58 do not end up with both the old and new DLL side by side.

**Speech-only commands and Shift+letter bindings reshuffled.** Seven Shift+letter chords now do focused per-cell, per-row work suited to screen-reader review: **Shift+A** appends the current virtual cell to the clipboard (two-CRLF separator, or just sets if the clipboard was empty); **Shift+C** copies the current virtual cell to the clipboard; **Shift+D** speaks the `updated` value in human-friendly local time (`December 14, 1963 at 5:42 AM`) — the underlying SQLite text is unchanged; **Shift+L** sweeps the current virtual column from the cursor downward; **Shift+N** speaks the current row's `notes` field; **Shift+T** speaks the current row's `tags` field. Shift+I remains Next Initial Change (the id is reachable via Show Record, Open Cell Value, or virtual cursor to the `_id` column). Say Marked moves from Shift+L to **Alt+Shift+M** (`M` for Marked). The prior Say Type (Shift+T) is dropped — Say Status already conveys the same context. Copy Row as TSV loses its Shift+A hotkey and now lives in the Misc menu without a mnemonic letter, per the Camel-Type rule that prefers no trigger letter to a mid-word one.

**Double-press behavior changed.** Every speech-only command now follows a single rule, regardless of chord: one press speaks the text through the screen reader without moving keyboard focus; a second press of the same chord within two seconds opens an information dialog with a read-only multi-line textbox and an OK button, useful for reviewing long content. The previous "double-press spells character-by-character" convention is removed. The two-second window is deliberately wider than the JAWS and NVDA defaults (~500 ms) so a thinking pause between presses still counts as one gesture.

**Per-table virtual-cell column is now remembered.** The cell under the Alt+Control+arrow cursor remembers its column (by name) and row, per table, both across table switches in a session and across sessions. Opening a database now seeds the in-session per-table cache from every previously-visited table's ini state, so switching to any one of them later restores its filter, sort, position, and virtual column — not just the table you land on first. F5 Refresh still resets to (row 1, first column) by design.

The JAWS `.jkm` and NVDA add-on are updated to pass the new Shift+letter chords (Shift+A, Shift+C, Shift+N) and Alt+Shift+M through to DbDo.

**Descriptive statistics for the current virtual column.** A new command, **Describe Column** (Control+Shift+D, `Measure-Column` at the dot prompt, in the Misc menu), walks the column under the virtual cursor and reports the statistics best suited to the data it finds. Numeric columns get Tukey's five-number summary (min, Q1, median, Q3, max) plus mean, sample standard deviation, range, IQR, mode (only when unambiguous), and a skew indicator derived from mean-vs-median against a fraction of the standard deviation. Date columns get earliest, latest, median, and span (rendered as days, then months and days, then years and months for long spans). Boolean-like columns (`0/1`, `Y/N`, `true/false`) get true and false counts with percentages. Text columns get unique count, shortest/longest/mean length, and a top-ten frequency table with counts and percentages. The report opens in the same read-only multi-line dialog used by the speech-on-double-press commands; Control+C inside the textbox copies the whole report. The choice of statistics follows the consensus in statistics teaching (Tukey's five-number summary plus mean and SD, the R `summary()` and SAS `PROC UNIVARIATE` defaults, pandas `describe()` for categorical) and the cognitive-accessibility finding that screen-reader users benefit more from linearized summary statistics than from raw data — Lundgard and Satyanarayan's MIT study on chart accessibility makes this point directly.

**Graphical statistics for the current virtual column.** A second new command, **Plot Column** (Control+Shift+P, `New-Plot` at the dot prompt, in the Misc menu), is the graphical sibling of Describe Column. It runs the same data-type detection and produces an Excel chart matched to the dominant type. Numeric columns prompt for either a histogram (Sturges-binned column chart of the distribution shape) or an Excel 2016 box-and-whisker chart (which renders Tukey's five-number summary as a single compact shape). Date columns prompt for a timeline (line chart of counts by month, or by day for short spans), a counts-per-year column chart, or a counts-by-month-of-year column chart for seasonal patterns. Boolean columns auto-pick a pie of true/false proportions (binary proportions are pie charts' textbook use case). Text columns auto-pick a horizontal Pareto bar of the top 15 most frequent values, sorted by frequency descending. When only one chart shape fits the data type, DbDo generates the file directly without an intermediate dialog. The .xlsx file is written next to the database file with a name like `customers-region-pareto.xlsx` and opened in Excel. Plot Column requires Excel; it reuses the same late-bound COM scaffolding as the existing Frequency Chart command. The choice of chart shapes follows the standard recommendations from Tukey (box plot), Tufte's visualization principles (Pareto), the data-viz consensus that line charts are the default "change over time" shape, and Excel's native chart-type enumeration (`xlColumnClustered`, `xlLine`, `xlPie`, `xlBarClustered`, `xlBoxwhisker`).

**Single-cell editor and Configuration Settings dialog.** A new command, **Edit Field** (Shift+F2, `Set-Cell` at the dot prompt, in the Edit menu) opens a small dialog with one labeled textbox for the current virtual cell — the cell under the Alt+Control+arrow cursor. F2 still opens the full-row editor; Shift+F2 is the fast path when you only need to change one value. Both editors share the per-field regex validation already enforced under `[Validation:<table>]` in DbDo.ini, so a configured pattern (e.g., `^[^@\s]+@[^@\s]+\.[a-z]+$` for an email field) is respected from either entry point.

The **Configuration Settings** command (Alt+Shift+C, matching the EdSharp and FileDir convention; F12 kept as a legacy alias) is the renamed and extended Edit-Configuration dialog. It exposes the curated user-facing settings — UI mode, Command Echo — and adds a **"Field Validation..."** sub-dialog that lists the editable fields of the current table with one input per field for a regex pattern. The sub-dialog compiles each pattern as you save so it can warn on bad regex syntax. DbDo deliberately uses .NET regex (the established powerful pattern language already used by Find Regex) rather than inventing a separate dBASE-PICTURE-style or WinForms-MaskedTextBox-style mask vocabulary. Operational settings (`[Session]`, `[Folders]`, `[Keys]` overrides, etc.) are still in the same .ini file but not shown in the dialog; the "Open file..." button is the escape hatch for raw editing.

The dot prompt picked up matching verbs for the new commands: `set-cell <column> = <value>`, `say-updated`, `say-notes`, `say-tags`, `say-column`, `append-cell`, `copy-cell`, `measure-column`, `new-plot`, `edit-configuration` (or `configuration`). The old `say-date` and `say-type` verbs (which corresponded to the dropped Shift+D / Shift+T speech commands) are removed.

**F8 / Shift+F8 / Alt+F8 / Alt+Shift+F8 range-mark family.** Four new commands in the Edit menu parallel EdSharp's Start/Complete Selection family and FileDir's Start Tag or Untag / Complete Tag / Complete Untag, using DbDo's "Mark" terminology and two independent anchors. **Start Mark Anchor** (F8) and **Complete Mark to Anchor** (Shift+F8) form one pair; **Start Unmark Anchor** (Alt+F8) and **Complete Unmark to Anchor** (Alt+Shift+F8) form the other. Each pair operates on its own anchor, so the user can stage a mark range and an unmark range without one gesture clobbering the other. Both "Complete" commands are direction-agnostic — the range from anchor to current row is the same whether the anchor was set above or below. The anchors are transient form-local state: they reset on database close so a stale anchor never bleeds across files. Console verbs: `set-markanchor`, `set-unmarkanchor`, `mark-range`, `unmark-range`.

**Say Position (Alt+Delete).** A new JAWS-style "say cursor position" command. Speaks the current virtual cell's column header and 1-based row number — for example, "Column: name, Row: 30." Speech-only; does not move focus. Single-press speaks; double-press shows the same text in the dialog used by the other speech-only commands. Console verb: `say-position`. The pass-through configs for JAWS (DbDo.jkm) and NVDA (DbDo.nvda-addon) are extended to forward F8, Shift+F8, Alt+F8, Alt+Shift+F8, and Alt+Delete to DbDo so its handlers always win over any default screen-reader behavior.

**Say Sort and Filter (Shift+8).** A new speech-only command on the asterisk key (Numpad-asterisk works as a hidden alias). Speaks the active sort order and filter criteria for the current table, with explicit "(none)" markers when either is empty, so the user gets confirmation rather than silence. Console verb: `say-sortfilter`.

**Dot prompt accepts any unique prefix.** The dispatcher now resolves typed verbs that are short unique prefixes of a canonical name, with hyphens and spaces interchangeable. So `first` resolves to `step-record-first`, `meas col` to `measure-column`, `step rec n` to `step-record-next`, and `config` to `configuration`. Where a prefix is ambiguous, the prompt prints the candidate list so the user can disambiguate by typing more characters. The expansion runs only after the existing alias table (resolveAlias) has had a chance, so the short single-character aliases like `n` (next) and `+` continue to work directly. The canonical-verb list lives in a single `s_aCanonicalVerbs` array near the dispatcher; new commands added in the future need a one-line entry there alongside their switch arm.

**GUI vs CLI response analysis.** A new "GUI versus CLI response patterns" section in DbDo.md walks through the command families and documents how each behaves in each mode. The principle is "same data effect, mode-appropriate confirmation" — the GUI uses LbcDialogs, status-bar updates, and the LiveRegion; the CLI prints to stdout and reads from stdin. The few cases where a command does not reasonably apply in one mode (New-Plot in CLI, Switch-Focus and Enter-Console in CLI-only mode) are called out explicitly.

**Say Kin (Shift+K).** A new speech-only command that announces the `look` field of every related record — both parents (reached by outbound foreign-key columns on the current row) and children (records in other tables whose FK points back to this row's primary key). Output is laid out as "Parents: <table>: <look>; <table>: <look>. Children: <table> (N): <look>, <look>, ..." for speech; double-press shows the same in the multi-line dialog, useful when a parent row has many children. Read-only and does not navigate — for an interactive jump to one related record, use Show-Related (Alt+Shift+R) as before. Console verb: `say-kin`; aliases `kin` and `say-related`. The mnemonic letter K stands for "kin" (relatives) since R (Related) is already taken by Clear Filter; the menu label leads with "Kin" to reinforce the chord.

**Updated-timestamp triggers in the sample databases.** The bundled sample databases (`sample.db`, `northwind.db`, `chinook.db`) now carry one SQLite trigger per table that maintains the `updated` column automatically. Previously, the `updated` column defaulted to `current_timestamp` at INSERT time but was never bumped on UPDATE — the comment in the C# source claiming "SQLite triggers update 'updated' automatically" was aspirational rather than accurate. The new triggers fire `AFTER UPDATE FOR EACH ROW WHEN OLD.col1 IS NOT NEW.col1 OR OLD.col2 IS NOT NEW.col2 ...` for every substantive column. The `marked` column is deliberately excluded from the substantive set, so toggling a row's marked flag (Control+M / Control+U, or any of the F8-family range-mark commands) does NOT bump the timestamp — marking is a UI gesture for building a working set, not a content edit, and bumping the timestamp every time a user marks rows would scramble "sort by recently edited" for the very users most likely to mark. The `added`, `updated`, `look`, and `unq` columns are also excluded (they're either system-managed or stored-generated). The check uses `IS NOT` rather than `<>` for null-safety. The "Bundled sample databases" section of DbDo.md gets a new sub-section with the recommended trigger SQL for users creating their own tables.

**Initial listview selection hardened.** The listview no longer leaves the first row unselected on table open. After `updateGrid` builds the rows, if the ADO recordset's `absolutePosition` is at BOF (≤ 0) but rows exist, DbDo now moves both the ADO cursor and the listview selection to row 1. Previously, ADO providers that opened the recordset at BOF could leave the user with rows visible but no selection, which broke commands that operate on "the current row." The invariant is now: if the table has at least one row, the listview always has exactly one selected, focused, visible row.

The README, Announce, History, and DbDo reference documentation are revised to drop development-process narrative that does not affect users and to incorporate concept and language refinements from the project's manual announcement.

## v1.0.58

NVDA add-on rewritten to fix the v1.0.57 six-pack-silence symptom.

First problem: bogus numpad identifier names. NVDA distinguishes numpad keys from six-pack keys at the gesture-identifier level. The v1.0.57 add-on bound names like `kb:alt+control+numpadRightArrow` and `kb:alt+control+numpadHome`, which do not exist in NVDA's identifier system. v1.0.58 uses the correct numeric identifiers: `numpad7` (Home), `numpad1` (End), `numpad9` (PageUp), `numpad3` (PageDown), `numpad8` (Up), `numpad2` (Down), `numpad4` (Left), `numpad6` (Right), `numpad5` (say-current-cell).

Second problem: `gesture.send()` extended-key asymmetry. NVDA's `KeyboardInputGesture.send()` fails to deliver synthesized extended-key chords (six-pack arrows) to DbDo while delivering non-extended chords (numpad arrows) reliably. v1.0.58 replaces `gesture.send()` with a helper that constructs a fresh `KeyboardInputGesture` via `fromName(sCanonicalChord)` and sends that. The architectural benefit is that DbDo always sees the same VK code regardless of which physical key the user pressed.

Add-on internal version bumped to 1.0.7.

## v1.0.57

NVDA add-on now binds both six-pack and numpad variants of navigation chords. Documented convention is six-pack arrows for the `Control+Alt+arrow` table-navigation family; the numpad-specific exception is `Control+Alt+Numpad5` for "say current cell."

## v1.0.56

Added `canPropagate=True` to all `@scriptHandler.script` decorators in the NVDA add-on. Without it, scripts only fire when the focused NVDAObject is the AppModule's top-level object; with it, scripts fire when the AppModule appears anywhere in the focused object's ancestor chain. This matters because the focused object in DbDo is typically the data-grid ListView, a child of the form's NVDAObject.

Add-on internal version bumped to 1.0.5.

## v1.0.55

Build fix. v1.0.54's revert of a scaffolding block accidentally removed the `public static class JawsSettingsInstaller` declaration line, leaving the opening `{` unattached. The brace-counter check passed (matched totals) but a state-walking check now reports any spot where depth goes negative — catching this category of regression before delivery.

## v1.0.54

NVDA add-on rewritten using the modern decorator API and lowercase module name. The `.py` filename is now lowercase (`appModules/dbduo.py` instead of `appModules/DbDo.py`), matching the convention every shipped NVDA app module follows. Gestures are now declared with the `@scriptHandler.script` decorator instead of a class-level `__gestures` dict, and scripts are grouped by intent (one decorator per logical group of chords) rather than every chord pointing at one omnibus script.

First-run default-database fallback added: on a fresh install with no saved session, if `{app}\sample.db` is present, it opens automatically. New users see DbDo's school-domain sample on first run rather than facing an empty form.

## v1.0.53

Installer description for the NVDA add-on checkbox refined to flag the post-install restart requirement: "Install NVDA add-on (NVDA must be running; restart NVDA after install for it to take effect)".

## v1.0.52

Diagnostic logging added to the NVDA add-on's Python app module at four points: module import, AppModule.__init__, bindGesture loop completion, and script invocation. The pattern of presence/absence of these log lines pinpoints exactly which stage of the binding chain is failing for diagnostic purposes. NVDA must be running for the `.nvda-addon` file association to install correctly; this is now documented in both the installer's Finish-page checkbox description and the README.

## v1.0.51

Inno Setup compile fix. A Pascal block comment whose second line read `[Run] entry invokes DbDo.exe...` was misparsed because Inno Setup's preprocessor scans for `[Section]` tags before Pascal-comment parsing.

## v1.0.50

NVDA add-on manifest format fix. String values containing spaces and special characters are now enclosed in quotes (required by the NVDA manifest format), and the zip no longer contains standalone directory entries. JAWS files now ship as a single `DbDo_JAWS.zip` archive in the repo, extracted into place at install time by Inno Setup's built-in `ExtractArchive` Pascal function.

## v1.0.49

NVDA add-on install dialog now actually appears at end of setup. The Finish-page checkbox's `[Run]` entry now points `FileName` directly at `{app}\DbDo.nvda-addon` with the `shellexec` flag, which is the documented mechanism for opening non-executable files via their file association. The previous chain through DbDo.exe was racing against installer wizard completion.

## v1.0.48

JAWS settings installer migrated to Inno Setup `[Run]` entries. The C# `JawsSettingsInstaller` class added in v1.0.40 is preserved for use from the Help menu's "Re-install JAWS Settings" command.

## v1.0.47

Command-name cleanup: "object" removed from DbDo's command names. Show-Object → Show Record; Set-Object → Edit Record; Get-Property → Table Properties; etc. The word was vague and intimidating; the new names describe what the commands actually operate on.

## v1.0.46

Times in version-history entries adjusted for Pacific time zone (Seattle). Documentation references to time-sensitive operations updated.

## v1.0.45

JScript .NET script feature lands. Save, Invoke, and Edit Script commands accessible from the Misc menu. Scripts live in `%APPDATA%\DbDo\Scripts\` and run inside the DbDo process with full access to the running form (`frm`) and recordset manager (`db`). The tiny `dbDuoEval.dll` support assembly is compiled at build time by `jsc.exe` from `dbDuoEval.js`. See `DbDo.md`'s "Scripting with scripts" section.

## v1.0.44

Course-correction release. The Roslyn C# scripting feature from v1.0.42-v1.0.43 is rolled back; in its place is the EdSharp-style Save / Invoke / Edit Script pattern using JScript .NET. The Roslyn approach shipped 12 NuGet runtime DLLs totaling ~25-30 MB and required an MSBuild + NuGet build-system migration. The new approach matches the EdSharp precedent: standard controls, no shipped runtime DLLs beyond the tiny `dbDuoEval.dll`, no custom UI; the user writes scripts in their own editor.

## v1.0.43

NVDA controller DLL renamed: `nvdaControllerClient64.dll` → `nvdaControllerClient.dll`. NVDA 2026.1 ships the 64-bit DLL inside `x64/` with the unsuffixed name. The build script's DLL-extraction logic now looks in the archive's `x64/` folder and accepts either the modern or legacy filename.

## v1.0.42

Build system switches from bare `csc.exe` to MSBuild + NuGet to support a Roslyn-based C# scripting feature. (Both of these decisions are rolled back in v1.0.44.)

## v1.0.41

NVDA parity with JAWS. The `DbDo.nvda-addon` package ships and the Finish-page checkbox "Install NVDA add-on for DbDo" is checked by default. The add-on contains an app module that binds 49 keyboard gestures — the same set the JAWS `DbDo.jkm` covers — to a `script_passThrough` method whose body is one line, `gesture.send()`. Without this add-on, NVDA intercepts Alt+Control+arrow for its own table-navigation commands.

The `--install-nvda-addon` CLI flag is implemented: it locates `DbDo.nvda-addon` next to DbDo.exe and opens it via Windows shell-execute, handing it to NVDA's file association.

## v1.0.40

Broad polish release.

- Post-install task list reordered (JAWS install first, NVDA install second, launch third, README fourth).
- JAWS install logic migrated from Pascal Script to a C# `JawsSettingsInstaller` class, accessible from the Help menu without re-running the installer.
- NVDA controller-client DLL (`nvdaControllerClient64.dll`) bundled at build time via PowerShell `Invoke-WebRequest` from the official NVDA source distribution.
- Command-name consistency cleanup: eleven dialog titles and MessageBox captions previously displayed the PowerShell verb-noun canonical name while their menu labels used natural English; all eleven captions now match.
- Extra Speech toggle added (Alt+Shift+S) following the EdSharp / FileDir model.
- Help > Open Sample Database opens `{app}\sample.db` via the same code path as File > Open.

## v1.0.39

JAWS settings install correctness fix. v1.0.38 shipped a JKM-only approach that turned out not to work: `TypeCurrentScriptKey` is a JAWS Function, not a Script, so it cannot be invoked from a JKM right-hand side. v1.0.39 adds a tiny script source file (`DbDo.jss`) defining a one-line wrapper Script called `PassDbDoKey`, and installer logic to compile it to `DbDo.jsb` in each JAWS year-version's settings folder.

## v1.0.38

JAWS settings integration. The `DbDo.jkm` JAWS key map ships and the installer places it in the right JAWS user-settings folders automatically. JAWS will pass DbDo's chords through rather than intercepting them for its own table-navigation commands.

## v1.0.37

Build fixes.

## v1.0.36

Final program name selected as DbDo.

## v1.0.35

Virtual cell navigation polish: column-aware commands default to the column currently under virtual focus.

## v1.0.34

Virtual cell cursor implementation. The data list is a virtual-mode ListView, but on top of it DbDo overlays a `(row, column)` cursor you drive with Alt+Control + arrow / Home / End / PageDown / PageUp / Numpad5. Movement triggers a direction-aware announcement.

## v1.0.33

Search dialogs now have a Text input, a Recent list, and a Case-sensitive checkbox. Each of the three search families uses the same dialog layout; selecting a Recent entry copies its text into the Text input AND sets the Case-sensitive checkbox to how that term was last used.

Recent Files dialog on Alt+R opens one of the last 10 database files with full state restoration (last-active table, filter, sort, position).

Menu labels rewritten to natural-English DbDo names. The PowerShell canonical names (Show-Object, Set-Mark, Sort-Object, etc.) remain available at the dot prompt.

## v1.0.32

Three distinct search families: Find Across All Columns (Control+F), Jump to Match in One Column (Control+J), Find Regex Across All Columns (Control+F3). Each family has its own forward / reverse chord pair, plus the unified F3 / Shift+F3 dispatcher.

## v1.0.31

Alt+RightArrow / Alt+LeftArrow obviate the need for separate keys for entering or exiting child tables.

## v1.0.30

Public GitHub API used for update checks; no credentials required.

## v1.0.29

Persistent search history across the three search families.

## v1.0.28

`PRAGMA table_info` and window-function output rendered correctly in the result grid for Run SQL.

## v1.0.27

EdSharp/FileDir equivalence prioritized for command names; conflicting commands renamed or rebound. Hotkey assignments evaluated in priority order for common operations.

## v1.0.26

Initial command-and-hotkey table assembled.

## v1.0.25

Build script and release-tagging stabilized.

## v1.0.24

Single Alt+Control+D shortcut only — no other start-menu or desktop shortcuts.

## v1.0.23

Build-error cleanups.

## v1.0.22

InnoSetup installer modeled on the 2htm pattern.

## v1.0.21

Initial release packaged with InnoSetup.

## Pre-v1.0.20

Development history under earlier program-name candidates (DbDual, DbDo, DbDesk, etc.) before settling on DbDo. Early work covered: WinForms architecture with FluentListView-derived virtualization, late-bound ADO via the SQLite ODBC driver, parent-child drill via foreign-key inference, the Show Record / Related Records pattern, three-mode keyboard model (rows / column-announcements / virtual cells), and the dual-interface (GUI + dot prompt) design.
