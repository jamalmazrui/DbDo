# DbDo — Transition Brief for a New Chat

Hand this file (and the latest distribution file set) to a fresh DbDo chat so
prior problems and decisions are not lost. It is written for Claude to read on
arrival.

---

## 1. Who I am and how to work with me

- I am **Jamal Mazrui**, a blind developer (Bellingham, WA). I use **JAWS**
  primarily, also **NVDA** and **VoiceOver**. I cannot see images; put all
  explanation in the **text chat** so I can read it.
- **Start every response with a line containing only `Claude:`** (a screen-reader
  landmark) and **end every response with a `## Summary` heading** plus a brief
  summary.
- Default output format is **Pandoc-flavored Markdown** unless I say otherwise.
- **Deliver files as loose files via `present_files`** — never wrapped in a
  Claude-made `.zip`, so they sit at the root of the download archive.
- I code in a personal style called **Camel Type** (Hungarian prefixes — `s`
  string, `i` integer, `b` boolean, `l` list, `d` dictionary, `o` other, `a`
  array, `n` real, etc.; lowerCamel methods; `c_` constants; double quotes;
  one-line if-then; alphabetized declarations; for-each over index loops). Use it
  by default for any code.
- **My questions are brainstorming, not change requests.** Implement only what I
  explicitly ask for ("please do X").
- **No backward-compatibility pressure:** DbDo is pre-release alpha with no users,
  so prior choices should not constrain recommendations unless I say so. Separately,
  stay conservative with code/UI so prior development decisions are not lost by accident.
- **Always include logging** (`DbDoLog`) or other debugging aids in every feature.
- **Always proactively draft my follow-up reply to a tester/user** (e.g. Richard)
  as a downloadable Markdown file, without being asked.
- **You (Claude) cannot compile or run anything here** — no Windows/.NET, and the
  sandbox network is disabled. **I build locally** with `buildDbDo.cmd` and test on
  Windows. So validate algorithms against real data (e.g. in Python) before writing
  C#, and treat all C# you produce as untested.

---

## 2. The project

- **DbDo** — an accessible, keyboard-first WinForms C# relational database manager
  designed for screen-reader users. GitHub: `https://github.com/JamalMazrui/DbDo`.
- **Stack:** .NET Framework 4.8; **single source file `DbDo.cs`** (~1.65 MB,
  ~32,700+ lines, intentionally one file — do not split it). SQLite accessed via
  **ADODB COM with the ch-werner SQLite ODBC driver** (client-side cursors).
  Compiled **64-bit only** via `buildDbDo.cmd`. Installer via **Inno Setup**
  (`DbDo_setup.iss`). **EdSharp** (my earlier app) is the reference model for
  config and recompile-in-place patterns.
- **Vocabulary:** schema vocabulary is **Field / Record / Table**; UI vocabulary
  is **Column / Row / Cell / Grid**. Empty values say **"blank"**, not "(empty)".
- **Schema conventions:** primary key `singular_id`; a `maps` table holds all
  inter-table associations; a `lookups` table (14 seed rows) and `maps` table are
  standard infrastructure in every database; standard column set includes
  pk, added, modified (this is the correct name, **not** `edited`), data fields,
  notes (TEXTMARKDOWN), tags (TEXTMEMO), look + unq (GENERATED, STORED), marked.
  There is a `UNIQUE` index on `unq`. Database identifiers are lower_snake_case.
- **Key build facts:** `buildDbDo.cmd` compiles `DbDo.cs` with csc
  (`/target:winexe /platform:x64`) and `DbDo.js` with jsc into `DbDo.dll`. It
  fetches `Newtonsoft.Json.dll` and SQLean (`sqlean.exe`/`sqlean.dll`, the
  dot-prompt shell — a separate feature, keep it) via PowerShell from NuGet.

---

## 3. What was done in the most recent work (this just-ended chat)

All C# below is **written but untested** (I had not yet confirmed a clean local
build of the final state at handoff — see Open Items).

### 3a. Native `.xlsx` import — replaced Excel/COM entirely
The original Import drove a hidden Excel via COM. On a machine where DbDo is
64-bit and Office is 32-bit (my tester Richard's setup), that COM hand-off fails
with `0x80010114 "The requested object does not exist."` No COM tuning fixed it.
**An `.xlsx` is a ZIP of XML, so DbDo now reads it directly** — no Excel, no COM,
no bitness, no ACE. Implementation in `DbDo.cs` (in the `DbDoForm` class):

- `private class XlsxZip` — minimal in-memory ZIP reader: reads the file bytes,
  scans the End-Of-Central-Directory record, walks the central directory into a
  name→{localOffset,method,compSize} dictionary, and inflates entries with
  `System.IO.Compression.DeflateStream`. **`DeflateStream` lives in `System.dll`,
  already referenced, so there is NO build change.** XML parsed with `System.Xml`
  (`XmlDocument`), available by default. No ZIP64/encryption handling (an Office
  package needs neither).
- `importXlsxNative(sXlsxPath)` — parses `xl/sharedStrings.xml`,
  `xl/styles.xml` (to flag which cell styles are date formats),
  `xl/workbook.xml` + `xl/_rels/workbook.xml.rels` (ordered sheet name→part
  paths), and each sheet; builds a temp DbDo "shell" database (standard-shape
  table per non-empty sheet, plus maps+lookups via `lInfraDdl(true,true)`).
  Returns the temp `.db` path. Empty sheets are skipped.
- `lGridFromSheet(...)` → `List<string[]>` grid; `sCellXmlValue(...)` resolves
  shared strings / inline strings / booleans, and renders date-styled numbers via
  `DateTime.FromOADate` as `yyyy-MM-dd` (or with time). `iColFromRef` converts a
  cell ref's letters to a 1-based column number. `bIsDateFormat` detects builtin
  date numFmt IDs (14–22, 45–47, 27–36, 50–58) plus custom codes containing date
  tokens. Other helpers: `localIs`, `firstLocal`, `xmlAttr`, `xmlAttrLocal`,
  `appendXmlText`, `iFilledCells`.
- `buildShellTableFromGrid(...)` — shared grid→table builder: detects the header
  row (skips a sparse title/metadata row above the real headers), routes
  notes/tags headers into the standard columns, suffixes other standard-name
  collisions with `_in`, runs `lStandardTableDdl`, and inserts data rows with
  literal apostrophe-escaped SQL; a row that trips the UNIQUE `unq` index is
  skipped and logged.
- `importToShell` dispatch: `.xlsx` → `importXlsxNative`; `.xls` →
  `importWorkbookFile` (legacy Excel COM, kept only for the old binary format,
  which is not a ZIP); everything else → `importAdoToShell` (ACE).
- The algorithm was **validated in Python against Richard's real
  `Books_read_RT.xlsx`**: row 1 metadata skipped, headers on row 2, dates like
  `2024-10-01` and `2021-02-18 15:03:12`, empty Sheet2 skipped, ~692 data rows.

### 3b. Email Log feature (the Copy button was failing for the tester)
The error dialog's Copy button can report "could not access the clipboard," so
the new path avoids the clipboard:
- `internal static void emailLogFile(IWin32Window owner)` in `DbDoForm` — reveals
  `DbDo.log` in the file manager selected (`explorer /select`) and opens a new
  `mailto:` with the log's full path in the body (mailto cannot attach, so the
  path is given). Uses `DbDoLog.getLogPath()` (log lives next to the exe as
  `DbDo.log`, fallback `%TEMP%`).
- A Help-menu command **Email Log File…** (`miHelpEmailLog`) and an **Email Log**
  button on the error dialog (`ErrorDialog.show`) both call it.

### 3c. Build cleanup — removed unused System.Data.SQLite
`System.Data.SQLite` appeared **only in a comment** (zero real usage; the engine
is the ODBC driver via ADODB). Removed from `buildDbDo.cmd`: the
`System.Data.SQLite.dll` + `SQLite.Interop.dll` download block, both csc
`/reference` entries, and the build-summary line. `sqlean.exe`/`sqlean.dll`
(separate dot-prompt feature) were left in place.

### 3d. Launch/performance config (modeled on EdSharp)
Created **`DbDo.exe.config`** next to the exe: `supportedRuntime v4.0.30319`,
`<generatePublisherEvidence enabled="false"/>` (skips the Authenticode/CRL stall
at startup) and `<gcConcurrent enabled="true"/>`. The CLR reads it automatically;
no build change needed.

### 3e. Setup script reconciliation (`DbDo_setup.iss`)
Now ships `DbDo.exe.config` and stops deleting it in `[InstallDelete]` (the old
delete targeted an obsolete binding-redirect config). Added source + build inputs
to `[Files]` so the app can be recompiled in place, EdSharp-style:
`DbDo.cs`, `DbDo.js`, `buildDbDo.cmd`, `DbDo_setup.iss`.

### 3f. Table-selection fix (the actual cause of the tester's "14 records")
On open with no remembered table, DbDo fell back to `lTables[0]`, and
`getTableNames()` (ADOX) returns names **alphabetically**, so `lookups` (14 rows)
won over `maps` and a user's data table (e.g. `sheet1`). Fixed
`openDatabaseAndApplyState` to **prefer the first non-infrastructure table**
(skip `maps`/`lookups`), falling back to the first table only if every table is
infrastructure. This applies to both opening a file and viewing right after an
import.

---

## 4. The Richard saga (tester feedback loop) — current status

- **Tester:** Richard Turner (`richardturner42@outlook.com`); I am
  `jamal.mazrui@outlook.com`. Richard runs **32-bit Microsoft 365**; DbDo is
  **64-bit** — the root of the whole arc.
- **Arc:** (1) Open of `.xlsx` failed because 64-bit DbDo needs 64-bit ACE that
  his 32-bit Office blocks → the durable answer was Import, not Open. (2) Import
  via Excel COM failed `0x80010114`; a prompt-suppression theory was **wrong**.
  (3) Root cause = the 64-bit-DbDo/32-bit-Excel COM bridge itself → replaced with
  the native reader (3a). (4) Latest report: import ran with **no error**, saved
  file was **1,828 KB** (his ~692 rows are clearly present), but JAWS read "1 of
  14 / lookups, 14 rows" — i.e. DbDo opened onto the `lookups` infrastructure
  table. **This was the table-selection bug (3f), not a parse failure.**
- **The `unq` UNIQUE column cannot silently drop distinct book rows** — it is
  generated from all data fields concatenated, so only a fully-identical row would
  be rejected.
- **Most recent reply drafted:** `Reply-to-Richard-table-selection.md` — explains
  his data imported fine and is in the `sheet1` table, and that he can confirm it
  on his current build via **F7 (Choose Table) → pick `sheet1`** (the next build
  will land there automatically).
- **Pending confirmation:** Richard verifies `sheet1` shows ~690 rows with correct
  columns/dates. If it does, the import saga closes.
- Earlier reply drafts (history): `Reply-to-Richard-DbDo-Import.md`,
  `Reply-to-Richard-Import-fix.md`, `Reply-to-Richard-native-xlsx.md`.

---

## 5. Open decisions and next steps

1. **Confirm the native reader's output** once Richard checks `sheet1` (~690 rows).
   Treat as the gating item before any larger xlsx rework.
2. **xlsx engine direction (a question I keep raising — brainstorming):**
   - Keep the dependency-free native reader (my standing recommendation, since it
     needs no Excel and no bitness match, and it appears to have worked).
   - OR a **NuGet package** — `ExcelDataReader` (least code; pulls a
     `System.Text.Encoding.CodePages` dependency) or `DocumentFormat.OpenXml`
     (Microsoft official; can be single-DLL on .NET 4.8). Caveat: Claude cannot
     test dependency wiring here, so do it as its own build cycle.
   - OR a **32-bit `xlsx2db.exe`** (from `xlsx2db.cs`) that drives Excel COM and is
     called by DbDo as a process — elegantly fixes bitness by matching Excel, but
     reintroduces the Excel dependency we just removed, ships a second artifact,
     and needs a no-Excel fallback anyway. Recommended last.
3. **"Make sure `DbDo_setup.iss` does the compilation" — needs clarification.**
   It currently ships the build script + source (EdSharp-style: run `buildDbDo.cmd`
   before ISCC). If instead I want ISCC itself to invoke `buildDbDo.cmd` at
   installer-compile time (a `#expr Exec(...)` preprocessor line), say so — it was
   not added because a failing compile-time Exec would block the ISCC build.
4. **Console `import-data` command** still does the old merge-style behavior and
   diverges from the GUI Open/Import/Merge taxonomy; offered to align it (not done).
5. **Docs** (`DbDo.md`, `README.md`) still say "Import Data"; update to the
   Open / Import / Merge vocabulary.
6. **Carry-overs from older sessions:** remove the vestigial
   `iCurrentColumnIndex` field and dead `announceCurrentColumn` method; tune the
   NFB convention DB parser's presenter extraction; decide whether to keep the
   shared installer AppId GUID or mint a new one; optional `collection.db` removal
   + `rebuildSamples.py` prune; a stale-name JAWS quirk after Control+W.

---

## 6. Verb taxonomy already in place (context for the above)

Three distinct verbs exist in `DbDo.cs`: **Open** (Control+O) mounts a file with a
live driver; **Import** (Alt+I) transfers every source table into a fresh DbDo
shell; **Merge** (Alt+M) appends rows from a Markdown/JSON/Inix file into the
current table (this is the renamed former "Import Data"). "Open as Managed Copy"
remains SQLite-only.

---

## 7. Files you will upload to the new chat

The latest distribution set (at minimum): `DbDo.cs`, `buildDbDo.cmd`,
`DbDo_setup.iss`, `DbDo.exe.config`, and any new tester error reports. A working
copy lives on my side at `c:\DbDo`. Note again: Claude builds nothing here; I
compile and test on Windows.
