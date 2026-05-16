# DbDuo History of Changes

This file is the chronological record of DbDuo releases. The most recent release is at the top. For the overview of what DbDuo is, see `Announce.md` or `README.md`. For the full reference, see `DbDuo.md`.

Press **Shift+F1** inside DbDuo to open this file in your browser, or type `history` at the dot prompt.

## v1.0.69 (current)

**The wave-2 commands from the v1.0.65 spec are complete.** Thirteen new commands wired up in this build, finishing the last pending block:

- **Append Record (Alt+Shift+C)** — like Copy Record but appends to the existing clipboard contents (separator: blank line). Each record renders as "field: value" lines so the clipboard accumulates several rows in human-readable form.
- **New Copy (Ctrl+Shift+N)** — duplicates the current row. Opens the New Record dialog pre-filled with the current row's distinct field values; the user reviews, edits, and OK inserts as a new row. The `unq` column is cleared in the pre-fill since stored generated columns must be unique.
- **Mail Record (Ctrl+Shift+M)** — scans the current row for an email-like column (containing 'email', 'e_mail', or 'mail' in the column name), then launches the system mail client via `mailto:` with the subject populated from `look` and body from `notes`. Uses `Uri.EscapeDataString` for proper URL-encoding of subject and body per RFC 6068.
- **Open New Recordset (Ctrl+Shift+O)** — prompts for a SQL SELECT or WITH statement and opens the result as a read-only recordset. Find, Filter, Sort, Say-X all work; Mark, Edit, Delete refuse because the result has no table identity. Useful for ad-hoc views that don't justify creating a permanent SQLite VIEW. New method `openSqlRecordset` on `DbDuoManager` does the work; calls `oRecordset.Open(sql, conn, adOpenStatic, adLockReadOnly, adCmdText)` against ADODB, sets a synthetic current-table name `(ad-hoc SELECT)` so dialogs and status bars have something to display.
- **Eight Sort-by-standard-column shortcuts.** Each pair Alt+letter / Alt+Shift+letter sorts by a fixed standard column ascending or descending: Alt+I / Alt+Shift+I = Sort by Id (the table's actual primary key, resolved via `actualPrimaryKey`), Alt+L / Alt+Shift+L = Sort by Look, Alt+T / Alt+Shift+T = Sort by Tags, Alt+U / Alt+Shift+U = Sort by Url. Convenience aliases over Sort-Object so the user doesn't have to pick the column through a dialog for the standards that always exist on DbDuo-convention tables. All eight thread through a single helper `sortByFixedColumn` that sets `db.sort` and refreshes.

**Chord conflict cleanup, three resolutions:**

- Three `Alt+letter` aliases were removed: `Alt+T → Measure-Table`, `Alt+C → New-Chart`, `Alt+L → Select-Table`. Each of those chords is now the primary chord for one of the new Sort-by-X commands (or Append Cell in the Alt+C case). Measure-Table, New-Chart, and Select-Table retain their menu entries with their canonical chords (F4 for Select-Table; no chord for the other two — reachable via the menu).
- **Append Cell moved from Shift+A to Alt+C** to resolve a silent chord collision with Say-Added (Shift+A, added in v1.0.67). WinForms registers only the last menu item assigned to a chord, so the prior assignment quietly clobbered Say-Added; the move puts Append Cell on the chord the user actually intended.
- **Copy Cell moved from Shift+C to Ctrl+C** to resolve a similar silent collision with Say-Cell (Shift+C, added in v1.0.67). The user's spec was `Ctrl+C = Copy Cell` and `Shift+C = Say Cell`; both are now in effect.

**Still pending:** the deferred trio (Database Summary on Shift+D, Window Summary on Shift+W, Pick Value on Ctrl+F2) still have their "deferred; not yet implemented" stubs. Each chord is reserved for the eventual real implementation.

## v1.0.68

**Northwind and Chinook adopt the v1.0.66 standard-column extensions.** Both bundled "canonical" sample databases were upgraded in place to match DbDuo's full standard-column convention. Discovery: both already had `<table>_id` primary keys, `added` / `updated` timestamps, `notes`, `tags`, `marked` columns, and (most usefully) `look` and `unq` were already present as `STORED GENERATED` columns computed from the substantive fields. The `look` column in `northwind.db::categories` for example is `rtrim(iif(length(name)>0, name || ' | ', '') || iif(length(description)>0, description || ' | ', ''), ' | ')` — exactly the right pipe-joined display nickname pattern. The only work for v1.0.68 was to **add `url` (TEXTLINE)** and **upgrade `notes`/`tags` from `TEXT` to `TEXTMEMO`** so DbDuo's Edit Record dialog renders the multi-line memo widget. The substantive columns (company, contact, city, country, phone for Northwind customers; name, title, artist_id for Chinook artists; etc.) are preserved verbatim.

Per the user's clarification this turn, *"additional columns in the canonical databases do not have to be displayed."* Three columns moved from "visible by default" to "hidden by default" so the listview matches the canonical schemas and isn't cluttered: `url`, `tags`, `notes` now join `added`, `updated`, `marked`, `look`, `unq` in DbDuo's `StandardHiddenColumns` set. Users who want any of those visible in their own databases can use the Select Columns command (Alt+S, v1.0.66) to override per-table; the override persists to DbDuo.ini via the `t<n>_selectlist` mechanism added in v1.0.66.

**Online documentation links for canonical samples.** The "Bundled sample databases" section of DbDuo.md now includes external links so users can learn more about the canonical Northwind and Chinook schemas and their broader uses:

- Northwind: Microsoft Learn, the official `microsoft/sql-server-samples` GitHub repo, and Wikipedia.
- Chinook: Luis Rocha's `lerocha/chinook-database` reference repo, plus the SQLite Tutorial walkthrough.

Both descriptions now explicitly enumerate DbDuo's adaptations to make clear they're minimal: same substantive columns, snake_case naming on the integer primary keys, standard columns appended, TEXTMEMO declared on notes/tags. The Foxbase-and-Clipper-era principle survives: real databases used for real work need certain things (timestamps, look-labels, free-text notes); DbDuo's standard columns formalize those needs without forcing users to redesign canonical sample schemas.

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

**Standard-column schema refresh for three bundled databases.** `sample.db`, `collection.db`, and `cellar.db` rebuilt with the v1.0.66 standard-column sequence: `<table>_id, added, updated, url, tags, notes, look, unq`. The new `url` column carries SQLite declared type `TEXTLINE` so DbDuo renders a single-line input box in Edit Record; `tags` and `notes` carry `TEXTMEMO` so they get the multi-line memo widget. Each row's data was rewritten to populate the new look/url/tags/notes fields with real content — sort names and Wikipedia URLs for the music collection, producer websites for the wine cellar, school emails for the students/teachers. The two textbook databases (`northwind.db`, `chinook.db`) were left untouched to preserve their canonical adapt-of-canonical-SQL-sample identity; ask if you want those regenerated too.

**Select-list persistence to DbDuo.ini.** The per-table column selection set via Alt+S now survives across sessions. `RecentFiles.TableState` gained an `sSelectList` field; `loadSection` reads `t<n>_selectlist` from the section; `saveAll` writes the same key; `recordAllTableStates` pulls from the manager's `TableSettings.sSelectList` cache; `seedTableSettings` accepts the new parameter and restores the manager's cache when the database is reopened.

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

**Critical fix: Key Describer mode could not be exited.** When Key Describer was on, Ctrl+F1 (the toggle chord) was intercepted by the describe-mode handler — DbDuo announced the chord and its summary instead of running the toggle. Result: the user could enter Key Describer but never leave, blocking application exit short of killing the process. The fix adds the same escape EdSharp and FileDir use (EdSharp.cs line 1640, FileDir.cs line 7533): when in Key Describer mode, the toggle command itself ALWAYS executes regardless of mode state. Every other chord still gets described. The wording on toggle ("Key Describer On" / "No Key Describer", with "No" leading when the mode turns off so the screen reader announces the state change instantly) was already correct in v1.0.63 and is unchanged.

**Planned for v1.0.65** (pending the next build cycle, per the spec received during v1.0.64 development): a roughly 40-chord rebinding to clean up the menu mnemonics around a verb-noun-pair pattern (Shift+Letter = Say-X, Alt+Letter = X-Order, Alt+Shift+Letter = Reverse-X-Order, Control+Letter = primary action on X, Control+Shift+Letter = secondary action on X); a new `url` standard column of type textline, with the standard-column order becoming `<table>_id, added, updated, url, tags, notes, look, unq` and `tags`/`notes` upgraded to textmemo type; Find / Reverse Find / Extract / Regex Find / Reverse Regex Find searching all columns (not just displayed); Replace / Regex Replace / Jump operating on the current virtual column only; Search Next / Search Previous supporting all three families (Find, Regex Find, Jump). Several commands deferred (Ctrl+F2 Pick Value, Shift+D Database Summary, Shift+W Window Summary).

## v1.0.63

**EdSharp-style text-edit hotkeys in LbcDialog.** Inside every LbcDialog, single-line text inputs and multi-line memos now recognize a set of EdSharp-style hotkeys in addition to the standard Windows text-editing chords. The pattern is adapted from Jamal Mazrui's HomerLbc framework (`HomerLbc_40.js` lines 995–1162), which itself derives from EdSharp's text-editor conventions. Nine new chords: Ctrl+C copies the current line when nothing is selected; Alt+C appends the current line (or selection) to the existing clipboard contents on a fresh line; Ctrl+X cuts the current line when nothing is selected and speaks the next line as feedback; Alt+X cuts and appends; F8 marks the start of a selection at the caret; Shift+F8 completes the selection from that mark to the current caret; Ctrl+F8 copies all text in the field; Alt+F8 speaks all text via the live region; Ctrl+D deletes the current line and speaks the next line as feedback. None conflicts with standard control behavior — the Ctrl+C / Ctrl+X overrides only act when there is no selection (the standard Copy and Cut don't do anything without a selection); the F8 family, Ctrl+F8, Alt+F8, and Ctrl+D are unbound in standard WinForms TextBoxes; the Alt+ variants are unbound. Implementation lives on LbcDialog (not via TextBox subclassing): a form-level KeyDown handler with KeyPreview=true dispatches to twelve helper methods, gated by the focused control's Name prefix (`TextBox_` or `Memo_`) and a master enable flag `[Lbc] extraKeys` in DbDuo.ini (default Y, cached on first read).

**Two new hobbyist sample databases.** `collection.db` is a personal music collection — three tables (`artists`, `albums`, `tracks`) modeled on the data conventions refined by CLZ Music, MyMusicCollection, and Musicnizer over the past decade. Includes collector fields (rating, location, loan tracking) that distinguish a personal collection from the broader Chinook music-store data. Seeded with 8 artists / 16 albums / 22 tracks spanning rock, jazz, soul, classical, and electronic. `cellar.db` is a personal wine cellar — three tables (`wines`, `bottles`, `tastings`) modeled on CellarTracker, eSommelier, and VinCellar. The schema split separates wine identity (producer + vintage + varietal + region) from physical bottles (each with bin location, purchase price, source, status) from tasting notes (multiple over time). Seeded with 8 wines / 10 bottle lots / 4 tasting notes. New canonical Help-menu commands `Open-CollectionDatabase` (Open Music Collection) and `Open-CellarDatabase` (Open Wine Cellar) parallel the existing sample-database openers. Documented in DbDuo.md's "Bundled sample databases" section.

**Wine drink-window analytical query bundled as a script.** `Scripts/WineDrinkWindow.sql` is the standout analytical workflow from the wine-cellar research: for every wine in the cellar with bottles still held, compute years remaining in its drink window, sort by urgency (closest to end of window first), and classify each as too-young / in-window / past-peak. The kind of query that no flat list or spreadsheet can answer naturally — one ORDER BY clause and a JOIN against the bottles inventory. Demonstrates DbDuo's value for hobbyist data: ad-hoc SQL serves real workflows.

**Camel Type compliance for bundled `.js` script samples.** `CopyRowToClipboard.js` and `MarkRowsMatchingRegex.js` now follow Camel Type conventions verbatim: Hungarian-style type prefixes (`aFieldNames` for array, `sb` for StringBuilder, `iMarked` for integer, `regex` for Regex, `bMatch` for boolean), all variable declarations at the top of the script grouped alphabetically by type with type-lines themselves in alphabetical order (a < b < i < regex < s < sb), the constant `c_sPattern` with the required `c_` prefix, and `for each (sName in aFieldNames)` for-each iteration instead of integer-indexed loops. The DbDuo.js support module was already Camel Type compliant and is unchanged.

**Native dot-prompt syntax for bundled `.duo` script samples.** The `.duo` files previously used PowerShell-style canonical verbs (`switch-table`, `select-record`, `reset-filter`, `sort-object`, `say-path`, `say-status`, `say-tables`, `say-sortfilter`). They now use the natural single-word dot-prompt aliases (`table`, `filter`, `clear-filter`, `sort`, `path`, `status`, `tables-list`, `sort-filter`) — matching what users type interactively at the dot prompt. Either form still works because the dispatcher resolves aliases, but the natural form is shorter, more readable, and consistent with the rest of DbDuo's dot-prompt vocabulary. The `.duo` starter template was also updated. `RecentOrders.duo` additionally replaces the invented `sort-recentfirst` (which didn't exist) with the standard SQL-style `sort order_date desc`. The Scripting section of DbDuo.md was updated to make this convention explicit, with a recommended list of short forms.

## v1.0.62

**Key Describer now matches EdSharp and FileDir verbatim.** The Key Describer mode at Control+F1 had three behaviors that diverged from its EdSharp/FileDir model and made the feature unusable in practice: (1) toggling the mode opened a MessageBox confirmation dialog instead of announcing the new state to the screen reader; (2) when the mode was on and the user pressed a chord to be described, DbDuo opened another MessageBox showing the chord/command pair instead of speaking the information; (3) the canonical verb was named `Trace-Command` (mirrored in user-visible status strings as "trace mode" and "Trace-Command mode"), which both leaked an implementation term into the UI and collided with the real PowerShell `Trace-Command` cmdlet.

Studied EdSharp's `menuItem_Click` (line 1640) and FileDir's `ClickOrDescribe` method (line 7533) — both follow the same pattern: a static `KeyDescriber` boolean, a Ctrl+F1 menu handler that just announces "Key Describer On" / "No Key Describer" via the live region (no dialog), and a gate at the top of menu-click dispatch that, when the flag is on and the click isn't the Key Describer menu item itself, speaks three pieces of information — command name, chord, summary — and *swallows* the click without firing the command. No MessageBox anywhere. No tracing terminology.

DbDuo now follows the same pattern. The `helpTraceCommandClicked` menu handler (Control+F1) toggles `KeyMap.bKeyDescriber` and announces the new state via `LiveRegion.say`. The `KeyMap.tryDispatch` path and the Shift+Letter handler both check `bKeyDescriber` and, when on, call `LiveRegion.say(command + ". " + chord + ". " + summary + ".")` — three pieces joined with sentence-ending punctuation for natural screen-reader pauses, in one utterance — and swallow the keystroke. The command does not run.

The canonical verb is now `Switch-KeyDescriber`, matching DbDuo's PowerShell-style verb-noun discipline (Switch- is an approved PowerShell verb for toggles, and KeyDescriber is the noun). The menu item's user-visible label is still "Key Describer" verbatim per the EdSharp/FileDir alignment principle. Dot-prompt aliases `trace`, `trace-command`, `key-describer`, `keydescriber`, `describe-key`, and `describer` all resolve to `switch-keydescriber` for backward compatibility. The dot-prompt cmd `cmdTraceCommand` is renamed to `cmdSwitchKeyDescriber`; the console echo also says "Key Describer On" / "No Key Describer" to match the live region. The field `KeyMap.bTraceMode` is renamed to `KeyMap.bKeyDescriber`.

## v1.0.61

**Primary-key heuristic now schema-first.** Opening Northwind and pressing Enter-Child on a `categories` row reported "Cannot determine the primary-key column for 'categories'." The fault was in the Enter-Child helper `computePrimaryKeyColumn`, which used naive `-s`-stripping to singularize a table name: `categories` became `categorie`, which has no matching column. The corrected helper now calls the existing schema-driven `actualPrimaryKey(sTable)` FIRST (which reads `PRAGMA table_info` on SQLite or the ADOX `Keys` collection on Access), falling back to the naming heuristic only when the schema lookup is unavailable. The heuristic itself was also fixed to handle the `-ies` → `-y`, `-ses`/`-xes`/`-ches`/`-shes` → drop `-es`, and plain `-s` → drop `-s` plural patterns. Either path now finds `category_id` from `categories`.

**"Snippet" renamed to "Script" throughout.** The `Invoke-Snippet` command is now `Invoke-Script` (Alt+V still); `Save-Snippet` and `View-Snippet` (where they exist as dot-prompt or method names) become `Save-Script` and `View-Script`. The bundled sample folder `SampleSnippets` is now `SampleScripts`. The user-facing motivation: "script" describes what these files do (executable code in JScript .NET that drives DbDuo via host objects), whereas "snippet" connotes a passive text fragment. EdSharp and FileDir use "Snippet" for their own paste-tag idiom which is different from DbDuo's; the rename also disambiguates DbDuo from the editors. Variable names like `miMiscInvokeSnippet` are renamed to `miMiscInvokeScript`. 135 occurrences replaced across the C# source, 44 in DbDuo.md, 12 in History.md, 7 in the installer script.

**Invoke-Script output now uses the LbcDialog memo box.** The script-output dialog was a `MessageBox` previously, which is unsuitable for line-by-line, word-by-word, or character-by-character exploration with a screen reader. Multi-line results (or any result containing an "ERROR:" marker) now display in the same `showInfoDialog` LbcDialog used by the speech-only commands' double-press: a read-only multi-line TextBox with an OK button. Short single-line results still use MessageBox since brevity matches that idiom. The `Test-Database`, `Measure-Field`, and `Invoke-Sql` commands have used the equivalent `HelpDialog.show` for multi-line output since earlier versions; Invoke-Script now matches the pattern.

**Escape now activates OK in single-button LbcDialogs.** When `runWithButtons` is called with only one button (typically "OK", as in the confirmation-only memo dialog used by Invoke-Script's output and the speech-only commands' double-press), that one button is now wired as BOTH `AcceptButton` (Enter) AND `CancelButton` (Escape). Previously the user had to Tab to OK and press Enter or Space; Escape did nothing because no Cancel button was present. The fix matches user expectation that Escape always dismisses a modal dialog.

**Help menu mnemonic is now Alt+H (was Alt+P).** The top-level Help menu was labeled `Hel&p`, which made Alt+P open it — a non-standard convention. The label is now `&Help`, matching every modern Windows app. Top-level menu mnemonics are now: Alt+F (File), Alt+E (Edit), Alt+N (Navigate), Alt+Q (Query), Alt+M (Misc), Alt+H (Help). Each is unique and matches the Windows convention.

**Layout by Code section added to DbDuo.md.** A new major section walks through the LbC approach DbDuo uses for every dialog: the origin (Jamal Mazrui's AutoIt LbC of 2006, ported through wxPython, JScript .NET, and now C#), the conceptual model (bands, layout cursor, dialog units), why LbC matters for a screen-reader audience (tab order = call order, focus tips routed to status bar, memo-vs-AcceptButton coordination), the anatomy of an LbC dialog with a usage example, the full add-control vocabulary (`addLabel`, `addInputBox`, `addInlineInputBox`, `addMemoBox`, `addCheckBox`, `addListBox`, `addPickBox`, `addComboBox`, `addComboPickBox`, `addRadioButton`, `addNumericUpDown`, `addSeparator`), the two run methods (`runOkCancel` and `runWithButtons`), and the lookup-by-name pattern using `findControl` and the typed accessors. The section closes with a comparison against the original AutoIt LbC, noting which features were deliberately simplified in the C# port and which were preserved verbatim.

**Tab and Shift+Tab now move the virtual cursor.** Earlier versions had a vestigial `iCurrentColumnIndex` state that Tab/Shift+Tab advanced, separate from the canonical `iVirtualCol` used by Set-Cell, Say-Column, Say-Position, and the Alt+Control+arrow chords. The Tab handler also did nothing in practice because WinForms ListView does not surface Tab as a KeyDown event by default. The fix wires `grid.PreviewKeyDown` to flag Tab (without Control) as an input key so the KeyDown handler actually fires, and the handler now calls `virtMoveTo(iVirtualRow, iNewCol)` — the same path the Alt+Control+arrow chords use. Single state, single announcement, single behavior. Control+Tab remains reserved for Switch-Table.

**Say Clipboard command added (Alt+Apostrophe).** New speech-only command in the Query menu, mirrors FileDir's Alt+Apostrophe Clipboard. Speaks the current Windows clipboard text via the screen-reader live region; double-press opens the read-only memo dialog for line-by-line review of long pasted content. Empty and non-text clipboards announce that fact rather than going silent. The "Say" prefix is retained per DbDuo's Say-X family convention (the prefix marks the command as speech-only, no visible side-effect; FileDir has no such family so its bare "Clipboard" label is unambiguous). Canonical verb `Say-Clipboard`. Pass-through configs `DbDuo_JAWS.zip` and `DbDuo.nvda-addon` extended with `Alt+Apostrophe=PassDbDuoKey` and `kb:alt+'` so JAWS's default Alt+Apostrophe = "Say JAWS Version" gives way to DbDuo's handler inside the data list.

**EdSharp/FileDir-aligned menu names in the Help menu.** Five Help-menu items renamed to match the EdSharp and FileDir convention exactly, since the chord was already a match in each case: "Help Contents" → "Documentation" (F1); "Version History" → "History of Changes" (Shift+F1); "Toggle Key Describer Mode" → "Key Describer" (Control+F1); "Check for Update..." → "Elevate Version..." (F11); "About DbDuo" → "About" (Alt+F1). Also renamed: the Alternate Menu command's label was previously "Command Picker (alternate menu)..." with a verbose parenthetical; it is now simply "Alternate Menu..." (the EdSharp/FileDir name, Alt+F10). The Misc-menu "Configuration Settings..." command is now "Configuration Options..." (Alt+Shift+C), again matching EdSharp/FileDir verbatim. The PowerShell verb-noun pairs (Get-Help, Show-History, Trace-Command, Elevate-Version, About-DbDuo, Alternate-Menu, Edit-Configuration) remain available at the dot prompt and in logs but are not surfaced in the user-visible UI. The principle: when DbDuo has a command that does what EdSharp or FileDir does and the chord matches, the menu label IS the EdSharp/FileDir label; the PowerShell synonym is for power users.

A few user-facing DbDuo commands deliberately keep their original (non-EdSharp/FileDir) labels because of meaningful domain-specific reasons: "Say Clipboard" keeps the "Say" prefix per the Say-X family marker; "Say Status" keeps the "Say" prefix for the same reason; "Table Properties" keeps the disambiguating "Table" qualifier since the bare "Properties" would be ambiguous in a database context (properties of what? the cell, the table, the database?); "Close Database" keeps its name because DbDuo does not have multiple windows in EdSharp's MDI sense.

## v1.0.60

**Per-command summaries and descriptions.** Every menu item, dot-prompt verb, and dispatcher arm now carries a one-line summary and an optional multi-line description as metadata. The summary appears on the menu status bar when the item has focus, in the Alternate Menu (Alt+F10) inline after the verb, and in the Key Describer trace dialog (Control+F1) on its own line. The description, when present, appears in the Alternate Menu's detail pane and in the Key Describer trace. This mirrors the EdSharp and FileDir convention for command-self-documentation, exposing the metadata through three different surfaces (status bar, picker, trace) so users can discover what a command does without reading separate documentation.

The summary follows a consistent voice: verb-first, plain text, one line, suggesting the chord where one exists. Examples: "Open a database file" (Open-Database), "Mark every row from the F8 anchor to the current row" (Mark-Range), "Speak the current sort and filter, or '(none)'" (Say-SortFilter). Commands without an explicit summary fall back to the menu label minus the ampersand mnemonic markers, so every command is at least minimally self-describing even when the metadata table has a gap.

The implementation: `addItem` and `addItemLocal` gain two optional trailing parameters `sSummary` and `sDescription` (both default to `""`); the values flow into new `KeyMap.dCommandToSummary` and `dCommandToDescription` dictionaries keyed by canonical verb; menu items get their `ToolTipText` populated with the summary so screen readers announce it on accelerator-key landings; a `mi.MouseEnter` + `mi.Select` handler copies the summary into `lblStatus` on the form's status bar.

**Version-string bump from v1.0.58.** The `BuildInfo.VersionString` constant was last set to `1.0.58` and stayed there through the v1.0.59 development cycle by oversight. The constant now reads `1.0.60` to match the headline, the installer's `AppVersion` is bumped the same way, and the History.md headline marks v1.0.60 as current.

## v1.0.59

Compaction summary of work staged through v1.0.59 (entered the public history retroactively when v1.0.60 was tagged):

Documentation cleanup. The standard-field set is corrected: `observed` and `method` are removed; the documented set is now `<table>_id`, `added`, `updated`, foreign keys, distinct fields, `notes`, `tags`, `marked`, `look`, `unq`. The `sample.db` bundled with the installer is rebuilt to match this set while preserving its existing rows.

Two additional sample databases ship: `northwind.db` (the classic Microsoft Northwind sales sample) and `chinook.db` (the classic Chinook music-store sample), both adapted to DbDuo's standard column conventions. They are useful for exercising DbDuo's parent-child drill against deeper relationships than the small `sample.db` provides. The Help menu has new one-keystroke commands to open each: **Open Northwind Sample** and **Open Chinook Sample**, parallel to the existing **Open Sample Database** command. All three open via the same code path as File > Open Database.

`CamelType_CSharp.md` ships with the distribution, documenting the coding conventions used inside `DbDuo.cs`. The conventions are updated to remove the `c_` prefix that was previously required for constants; constants now follow the same naming pattern as variables but are still declared on their own lines, distinguished by `const` or `static readonly` instead of by capitalization. The `o` prefix is reserved for COM objects only; managed-type instances use a class-name prefix.

`DbDuo.cs` is updated to conform to the revised conventions (`c_` removed; `o`-prefix usage adjusted) with the addition of two new Help-menu items (Open Northwind Sample, Open Chinook Sample) and a shared helper method `openInstallSampleDb` that the existing Open Sample Database command now also routes through, eliminating duplicated code.

The JScript .NET support module is renamed: `dbDuoEval.js` becomes `DbDuo.js`, compiled to `DbDuo.dll` (previously `dbDuoEval.dll`). The JScript package and class are renamed to match, so the reflection target is now `DbDuo.JS.runScript` (previously `DbDuoScripting.JS.Eval` then `DbDuoScripting.JS.runScript`). Because DbDuo.exe and DbDuo.dll now share the simple assembly name "DbDuo", the C# code loads the DLL with `Assembly.LoadFrom` and the full path next to the executable instead of `Assembly.Load` with the simple name — the simple-name load would have resolved to DbDuo.exe rather than DbDuo.dll. The `/reference:` flag is dropped from the csc.exe command line for the same reason (the C# code never needed a compile-time reference; it always called the JS module through reflection). Existing user scripts are unaffected by the rename — they see the same `frm` and `db` globals and call the same DbDuo API.

Three sample scripts ship in `{app}\SampleScripts\`: `DescribeTable.js`, `CopyRowToClipboard.js`, and `MarkRowsMatchingRegex.js`. On first access of the user's script folder (`%APPDATA%\DbDuo\Scripts\`), DbDuo copies any samples that aren't already there and writes a `.seeded` sentinel file so the seeding does not repeat on later launches. Deleting a sample does not cause it to reappear; deleting the `.seeded` file allows re-seeding (e.g. to recover a deleted sample, or to pick up new samples bundled in a future release). The samples are short, single-purpose, and modeled on the EdSharp script style: a read-only introspection demo, a current-row clipboard helper, and a regex-match-and-mark utility.

The installer's `[InstallDelete]` section now removes the old `dbDuoEval.dll` from upgrade installs so users moving from v1.0.58 do not end up with both the old and new DLL side by side.

**Speech-only commands and Shift+letter bindings reshuffled.** Seven Shift+letter chords now do focused per-cell, per-row work suited to screen-reader review: **Shift+A** appends the current virtual cell to the clipboard (two-CRLF separator, or just sets if the clipboard was empty); **Shift+C** copies the current virtual cell to the clipboard; **Shift+D** speaks the `updated` value in human-friendly local time (`December 14, 1963 at 5:42 AM`) — the underlying SQLite text is unchanged; **Shift+L** sweeps the current virtual column from the cursor downward; **Shift+N** speaks the current row's `notes` field; **Shift+T** speaks the current row's `tags` field. Shift+I remains Next Initial Change (the id is reachable via Show Record, Open Cell Value, or virtual cursor to the `_id` column). Say Marked moves from Shift+L to **Alt+Shift+M** (`M` for Marked). The prior Say Type (Shift+T) is dropped — Say Status already conveys the same context. Copy Row as TSV loses its Shift+A hotkey and now lives in the Misc menu without a mnemonic letter, per the Camel-Type rule that prefers no trigger letter to a mid-word one.

**Double-press behavior changed.** Every speech-only command now follows a single rule, regardless of chord: one press speaks the text through the screen reader without moving keyboard focus; a second press of the same chord within two seconds opens an information dialog with a read-only multi-line textbox and an OK button, useful for reviewing long content. The previous "double-press spells character-by-character" convention is removed. The two-second window is deliberately wider than the JAWS and NVDA defaults (~500 ms) so a thinking pause between presses still counts as one gesture.

**Per-table virtual-cell column is now remembered.** The cell under the Alt+Control+arrow cursor remembers its column (by name) and row, per table, both across table switches in a session and across sessions. Opening a database now seeds the in-session per-table cache from every previously-visited table's ini state, so switching to any one of them later restores its filter, sort, position, and virtual column — not just the table you land on first. F5 Refresh still resets to (row 1, first column) by design.

The JAWS `.jkm` and NVDA add-on are updated to pass the new Shift+letter chords (Shift+A, Shift+C, Shift+N) and Alt+Shift+M through to DbDuo.

**Descriptive statistics for the current virtual column.** A new command, **Describe Column** (Control+Shift+D, `Measure-Column` at the dot prompt, in the Misc menu), walks the column under the virtual cursor and reports the statistics best suited to the data it finds. Numeric columns get Tukey's five-number summary (min, Q1, median, Q3, max) plus mean, sample standard deviation, range, IQR, mode (only when unambiguous), and a skew indicator derived from mean-vs-median against a fraction of the standard deviation. Date columns get earliest, latest, median, and span (rendered as days, then months and days, then years and months for long spans). Boolean-like columns (`0/1`, `Y/N`, `true/false`) get true and false counts with percentages. Text columns get unique count, shortest/longest/mean length, and a top-ten frequency table with counts and percentages. The report opens in the same read-only multi-line dialog used by the speech-on-double-press commands; Control+C inside the textbox copies the whole report. The choice of statistics follows the consensus in statistics teaching (Tukey's five-number summary plus mean and SD, the R `summary()` and SAS `PROC UNIVARIATE` defaults, pandas `describe()` for categorical) and the cognitive-accessibility finding that screen-reader users benefit more from linearized summary statistics than from raw data — Lundgard and Satyanarayan's MIT study on chart accessibility makes this point directly.

**Graphical statistics for the current virtual column.** A second new command, **Plot Column** (Control+Shift+P, `New-Plot` at the dot prompt, in the Misc menu), is the graphical sibling of Describe Column. It runs the same data-type detection and produces an Excel chart matched to the dominant type. Numeric columns prompt for either a histogram (Sturges-binned column chart of the distribution shape) or an Excel 2016 box-and-whisker chart (which renders Tukey's five-number summary as a single compact shape). Date columns prompt for a timeline (line chart of counts by month, or by day for short spans), a counts-per-year column chart, or a counts-by-month-of-year column chart for seasonal patterns. Boolean columns auto-pick a pie of true/false proportions (binary proportions are pie charts' textbook use case). Text columns auto-pick a horizontal Pareto bar of the top 15 most frequent values, sorted by frequency descending. When only one chart shape fits the data type, DbDuo generates the file directly without an intermediate dialog. The .xlsx file is written next to the database file with a name like `customers-region-pareto.xlsx` and opened in Excel. Plot Column requires Excel; it reuses the same late-bound COM scaffolding as the existing Frequency Chart command. The choice of chart shapes follows the standard recommendations from Tukey (box plot), Tufte's visualization principles (Pareto), the data-viz consensus that line charts are the default "change over time" shape, and Excel's native chart-type enumeration (`xlColumnClustered`, `xlLine`, `xlPie`, `xlBarClustered`, `xlBoxwhisker`).

**Single-cell editor and Configuration Settings dialog.** A new command, **Edit Field** (Shift+F2, `Set-Cell` at the dot prompt, in the Edit menu) opens a small dialog with one labeled textbox for the current virtual cell — the cell under the Alt+Control+arrow cursor. F2 still opens the full-row editor; Shift+F2 is the fast path when you only need to change one value. Both editors share the per-field regex validation already enforced under `[Validation:<table>]` in DbDuo.ini, so a configured pattern (e.g., `^[^@\s]+@[^@\s]+\.[a-z]+$` for an email field) is respected from either entry point.

The **Configuration Settings** command (Alt+Shift+C, matching the EdSharp and FileDir convention; F12 kept as a legacy alias) is the renamed and extended Edit-Configuration dialog. It exposes the curated user-facing settings — UI mode, Command Echo — and adds a **"Field Validation..."** sub-dialog that lists the editable fields of the current table with one input per field for a regex pattern. The sub-dialog compiles each pattern as you save so it can warn on bad regex syntax. DbDuo deliberately uses .NET regex (the established powerful pattern language already used by Find Regex) rather than inventing a separate dBASE-PICTURE-style or WinForms-MaskedTextBox-style mask vocabulary. Operational settings (`[Session]`, `[Folders]`, `[Keys]` overrides, etc.) are still in the same .ini file but not shown in the dialog; the "Open file..." button is the escape hatch for raw editing.

The dot prompt picked up matching verbs for the new commands: `set-cell <column> = <value>`, `say-updated`, `say-notes`, `say-tags`, `say-column`, `append-cell`, `copy-cell`, `measure-column`, `new-plot`, `edit-configuration` (or `configuration`). The old `say-date` and `say-type` verbs (which corresponded to the dropped Shift+D / Shift+T speech commands) are removed.

**F8 / Shift+F8 / Alt+F8 / Alt+Shift+F8 range-mark family.** Four new commands in the Edit menu parallel EdSharp's Start/Complete Selection family and FileDir's Start Tag or Untag / Complete Tag / Complete Untag, using DbDuo's "Mark" terminology and two independent anchors. **Start Mark Anchor** (F8) and **Complete Mark to Anchor** (Shift+F8) form one pair; **Start Unmark Anchor** (Alt+F8) and **Complete Unmark to Anchor** (Alt+Shift+F8) form the other. Each pair operates on its own anchor, so the user can stage a mark range and an unmark range without one gesture clobbering the other. Both "Complete" commands are direction-agnostic — the range from anchor to current row is the same whether the anchor was set above or below. The anchors are transient form-local state: they reset on database close so a stale anchor never bleeds across files. Console verbs: `set-markanchor`, `set-unmarkanchor`, `mark-range`, `unmark-range`.

**Say Position (Alt+Delete).** A new JAWS-style "say cursor position" command. Speaks the current virtual cell's column header and 1-based row number — for example, "Column: name, Row: 30." Speech-only; does not move focus. Single-press speaks; double-press shows the same text in the dialog used by the other speech-only commands. Console verb: `say-position`. The pass-through configs for JAWS (DbDuo.jkm) and NVDA (DbDuo.nvda-addon) are extended to forward F8, Shift+F8, Alt+F8, Alt+Shift+F8, and Alt+Delete to DbDuo so its handlers always win over any default screen-reader behavior.

**Say Sort and Filter (Shift+8).** A new speech-only command on the asterisk key (Numpad-asterisk works as a hidden alias). Speaks the active sort order and filter criteria for the current table, with explicit "(none)" markers when either is empty, so the user gets confirmation rather than silence. Console verb: `say-sortfilter`.

**Dot prompt accepts any unique prefix.** The dispatcher now resolves typed verbs that are short unique prefixes of a canonical name, with hyphens and spaces interchangeable. So `first` resolves to `step-record-first`, `meas col` to `measure-column`, `step rec n` to `step-record-next`, and `config` to `configuration`. Where a prefix is ambiguous, the prompt prints the candidate list so the user can disambiguate by typing more characters. The expansion runs only after the existing alias table (resolveAlias) has had a chance, so the short single-character aliases like `n` (next) and `+` continue to work directly. The canonical-verb list lives in a single `s_aCanonicalVerbs` array near the dispatcher; new commands added in the future need a one-line entry there alongside their switch arm.

**GUI vs CLI response analysis.** A new "GUI versus CLI response patterns" section in DbDuo.md walks through the command families and documents how each behaves in each mode. The principle is "same data effect, mode-appropriate confirmation" — the GUI uses LbcDialogs, status-bar updates, and the LiveRegion; the CLI prints to stdout and reads from stdin. The few cases where a command does not reasonably apply in one mode (New-Plot in CLI, Switch-Focus and Enter-Console in CLI-only mode) are called out explicitly.

**Say Kin (Shift+K).** A new speech-only command that announces the `look` field of every related record — both parents (reached by outbound foreign-key columns on the current row) and children (records in other tables whose FK points back to this row's primary key). Output is laid out as "Parents: <table>: <look>; <table>: <look>. Children: <table> (N): <look>, <look>, ..." for speech; double-press shows the same in the multi-line dialog, useful when a parent row has many children. Read-only and does not navigate — for an interactive jump to one related record, use Show-Related (Alt+Shift+R) as before. Console verb: `say-kin`; aliases `kin` and `say-related`. The mnemonic letter K stands for "kin" (relatives) since R (Related) is already taken by Clear Filter; the menu label leads with "Kin" to reinforce the chord.

**Updated-timestamp triggers in the sample databases.** The bundled sample databases (`sample.db`, `northwind.db`, `chinook.db`) now carry one SQLite trigger per table that maintains the `updated` column automatically. Previously, the `updated` column defaulted to `current_timestamp` at INSERT time but was never bumped on UPDATE — the comment in the C# source claiming "SQLite triggers update 'updated' automatically" was aspirational rather than accurate. The new triggers fire `AFTER UPDATE FOR EACH ROW WHEN OLD.col1 IS NOT NEW.col1 OR OLD.col2 IS NOT NEW.col2 ...` for every substantive column. The `marked` column is deliberately excluded from the substantive set, so toggling a row's marked flag (Control+M / Control+U, or any of the F8-family range-mark commands) does NOT bump the timestamp — marking is a UI gesture for building a working set, not a content edit, and bumping the timestamp every time a user marks rows would scramble "sort by recently edited" for the very users most likely to mark. The `added`, `updated`, `look`, and `unq` columns are also excluded (they're either system-managed or stored-generated). The check uses `IS NOT` rather than `<>` for null-safety. The "Bundled sample databases" section of DbDuo.md gets a new sub-section with the recommended trigger SQL for users creating their own tables.

**Initial listview selection hardened.** The listview no longer leaves the first row unselected on table open. After `updateGrid` builds the rows, if the ADO recordset's `absolutePosition` is at BOF (≤ 0) but rows exist, DbDuo now moves both the ADO cursor and the listview selection to row 1. Previously, ADO providers that opened the recordset at BOF could leave the user with rows visible but no selection, which broke commands that operate on "the current row." The invariant is now: if the table has at least one row, the listview always has exactly one selected, focused, visible row.

The README, Announce, History, and DbDuo reference documentation are revised to drop development-process narrative that does not affect users and to incorporate concept and language refinements from the project's manual announcement.

## v1.0.58

NVDA add-on rewritten to fix the v1.0.57 six-pack-silence symptom.

First problem: bogus numpad identifier names. NVDA distinguishes numpad keys from six-pack keys at the gesture-identifier level. The v1.0.57 add-on bound names like `kb:alt+control+numpadRightArrow` and `kb:alt+control+numpadHome`, which do not exist in NVDA's identifier system. v1.0.58 uses the correct numeric identifiers: `numpad7` (Home), `numpad1` (End), `numpad9` (PageUp), `numpad3` (PageDown), `numpad8` (Up), `numpad2` (Down), `numpad4` (Left), `numpad6` (Right), `numpad5` (say-current-cell).

Second problem: `gesture.send()` extended-key asymmetry. NVDA's `KeyboardInputGesture.send()` fails to deliver synthesized extended-key chords (six-pack arrows) to DbDuo while delivering non-extended chords (numpad arrows) reliably. v1.0.58 replaces `gesture.send()` with a helper that constructs a fresh `KeyboardInputGesture` via `fromName(sCanonicalChord)` and sends that. The architectural benefit is that DbDuo always sees the same VK code regardless of which physical key the user pressed.

Add-on internal version bumped to 1.0.7.

## v1.0.57

NVDA add-on now binds both six-pack and numpad variants of navigation chords. Documented convention is six-pack arrows for the `Control+Alt+arrow` table-navigation family; the numpad-specific exception is `Control+Alt+Numpad5` for "say current cell."

## v1.0.56

Added `canPropagate=True` to all `@scriptHandler.script` decorators in the NVDA add-on. Without it, scripts only fire when the focused NVDAObject is the AppModule's top-level object; with it, scripts fire when the AppModule appears anywhere in the focused object's ancestor chain. This matters because the focused object in DbDuo is typically the data-grid ListView, a child of the form's NVDAObject.

Add-on internal version bumped to 1.0.5.

## v1.0.55

Build fix. v1.0.54's revert of a scaffolding block accidentally removed the `public static class JawsSettingsInstaller` declaration line, leaving the opening `{` unattached. The brace-counter check passed (matched totals) but a state-walking check now reports any spot where depth goes negative — catching this category of regression before delivery.

## v1.0.54

NVDA add-on rewritten using the modern decorator API and lowercase module name. The `.py` filename is now lowercase (`appModules/dbduo.py` instead of `appModules/DbDuo.py`), matching the convention every shipped NVDA app module follows. Gestures are now declared with the `@scriptHandler.script` decorator instead of a class-level `__gestures` dict, and scripts are grouped by intent (one decorator per logical group of chords) rather than every chord pointing at one omnibus script.

First-run default-database fallback added: on a fresh install with no saved session, if `{app}\sample.db` is present, it opens automatically. New users see DbDuo's school-domain sample on first run rather than facing an empty form.

## v1.0.53

Installer description for the NVDA add-on checkbox refined to flag the post-install restart requirement: "Install NVDA add-on (NVDA must be running; restart NVDA after install for it to take effect)".

## v1.0.52

Diagnostic logging added to the NVDA add-on's Python app module at four points: module import, AppModule.__init__, bindGesture loop completion, and script invocation. The pattern of presence/absence of these log lines pinpoints exactly which stage of the binding chain is failing for diagnostic purposes. NVDA must be running for the `.nvda-addon` file association to install correctly; this is now documented in both the installer's Finish-page checkbox description and the README.

## v1.0.51

Inno Setup compile fix. A Pascal block comment whose second line read `[Run] entry invokes DbDuo.exe...` was misparsed because Inno Setup's preprocessor scans for `[Section]` tags before Pascal-comment parsing.

## v1.0.50

NVDA add-on manifest format fix. String values containing spaces and special characters are now enclosed in quotes (required by the NVDA manifest format), and the zip no longer contains standalone directory entries. JAWS files now ship as a single `DbDuo_JAWS.zip` archive in the repo, extracted into place at install time by Inno Setup's built-in `ExtractArchive` Pascal function.

## v1.0.49

NVDA add-on install dialog now actually appears at end of setup. The Finish-page checkbox's `[Run]` entry now points `FileName` directly at `{app}\DbDuo.nvda-addon` with the `shellexec` flag, which is the documented mechanism for opening non-executable files via their file association. The previous chain through DbDuo.exe was racing against installer wizard completion.

## v1.0.48

JAWS settings installer migrated to Inno Setup `[Run]` entries. The C# `JawsSettingsInstaller` class added in v1.0.40 is preserved for use from the Help menu's "Re-install JAWS Settings" command.

## v1.0.47

Command-name cleanup: "object" removed from DbDuo's command names. Show-Object → Show Record; Set-Object → Edit Record; Get-Property → Table Properties; etc. The word was vague and intimidating; the new names describe what the commands actually operate on.

## v1.0.46

Times in version-history entries adjusted for Pacific time zone (Seattle). Documentation references to time-sensitive operations updated.

## v1.0.45

JScript .NET script feature lands. Save, Invoke, and Edit Script commands accessible from the Misc menu. Scripts live in `%APPDATA%\DbDuo\Scripts\` and run inside the DbDuo process with full access to the running form (`frm`) and recordset manager (`db`). The tiny `dbDuoEval.dll` support assembly is compiled at build time by `jsc.exe` from `dbDuoEval.js`. See `DbDuo.md`'s "Scripting with scripts" section.

## v1.0.44

Course-correction release. The Roslyn C# scripting feature from v1.0.42-v1.0.43 is rolled back; in its place is the EdSharp-style Save / Invoke / Edit Script pattern using JScript .NET. The Roslyn approach shipped 12 NuGet runtime DLLs totaling ~25-30 MB and required an MSBuild + NuGet build-system migration. The new approach matches the EdSharp precedent: standard controls, no shipped runtime DLLs beyond the tiny `dbDuoEval.dll`, no custom UI; the user writes scripts in their own editor.

## v1.0.43

NVDA controller DLL renamed: `nvdaControllerClient64.dll` → `nvdaControllerClient.dll`. NVDA 2026.1 ships the 64-bit DLL inside `x64/` with the unsuffixed name. The build script's DLL-extraction logic now looks in the archive's `x64/` folder and accepts either the modern or legacy filename.

## v1.0.42

Build system switches from bare `csc.exe` to MSBuild + NuGet to support a Roslyn-based C# scripting feature. (Both of these decisions are rolled back in v1.0.44.)

## v1.0.41

NVDA parity with JAWS. The `DbDuo.nvda-addon` package ships and the Finish-page checkbox "Install NVDA add-on for DbDuo" is checked by default. The add-on contains an app module that binds 49 keyboard gestures — the same set the JAWS `DbDuo.jkm` covers — to a `script_passThrough` method whose body is one line, `gesture.send()`. Without this add-on, NVDA intercepts Alt+Control+arrow for its own table-navigation commands.

The `--install-nvda-addon` CLI flag is implemented: it locates `DbDuo.nvda-addon` next to DbDuo.exe and opens it via Windows shell-execute, handing it to NVDA's file association.

## v1.0.40

Broad polish release.

- Post-install task list reordered (JAWS install first, NVDA install second, launch third, README fourth).
- JAWS install logic migrated from Pascal Script to a C# `JawsSettingsInstaller` class, accessible from the Help menu without re-running the installer.
- NVDA controller-client DLL (`nvdaControllerClient64.dll`) bundled at build time via PowerShell `Invoke-WebRequest` from the official NVDA source distribution.
- Command-name consistency cleanup: eleven dialog titles and MessageBox captions previously displayed the PowerShell verb-noun canonical name while their menu labels used natural English; all eleven captions now match.
- Extra Speech toggle added (Alt+Shift+S) following the EdSharp / FileDir model.
- Help > Open Sample Database opens `{app}\sample.db` via the same code path as File > Open.

## v1.0.39

JAWS settings install correctness fix. v1.0.38 shipped a JKM-only approach that turned out not to work: `TypeCurrentScriptKey` is a JAWS Function, not a Script, so it cannot be invoked from a JKM right-hand side. v1.0.39 adds a tiny script source file (`DbDuo.jss`) defining a one-line wrapper Script called `PassDbDuoKey`, and installer logic to compile it to `DbDuo.jsb` in each JAWS year-version's settings folder.

## v1.0.38

JAWS settings integration. The `DbDuo.jkm` JAWS key map ships and the installer places it in the right JAWS user-settings folders automatically. JAWS will pass DbDuo's chords through rather than intercepting them for its own table-navigation commands.

## v1.0.37

Build fixes.

## v1.0.36

Final program name selected as DbDuo.

## v1.0.35

Virtual cell navigation polish: column-aware commands default to the column currently under virtual focus.

## v1.0.34

Virtual cell cursor implementation. The data list is a virtual-mode ListView, but on top of it DbDuo overlays a `(row, column)` cursor you drive with Alt+Control + arrow / Home / End / PageDown / PageUp / Numpad5. Movement triggers a direction-aware announcement.

## v1.0.33

Search dialogs now have a Text input, a Recent list, and a Case-sensitive checkbox. Each of the three search families uses the same dialog layout; selecting a Recent entry copies its text into the Text input AND sets the Case-sensitive checkbox to how that term was last used.

Recent Files dialog on Alt+R opens one of the last 10 database files with full state restoration (last-active table, filter, sort, position).

Menu labels rewritten to natural-English DbDuo names. The PowerShell canonical names (Show-Object, Set-Mark, Sort-Object, etc.) remain available at the dot prompt.

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

Development history under earlier program-name candidates (DbDual, DbDo, DbDesk, etc.) before settling on DbDuo. Early work covered: WinForms architecture with FluentListView-derived virtualization, late-bound ADO via the SQLite ODBC driver, parent-child drill via foreign-key inference, the Show Record / Related Records pattern, three-mode keyboard model (rows / column-announcements / virtual cells), and the dual-interface (GUI + dot prompt) design.
