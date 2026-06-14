# DbDo build notes -- v1.0.107 delivery

This archive contains the full DbDo source tree after the v1.0.106 and v1.0.107 change sets: `<singular>_id` primary keys, `edited` replacing `modified`, Say Edited on Shift+E, mark-proof edited triggers, per-table `look`/`unq` redesign, the `.dbdo` script extension, the Alt+GraveAccent hotkey removal, the DbDo.inix settings file with Inix multi-line support, the gui-by-default startup fix, the virtual-cursor synchronization fix, and the rebuilt convention database on the simplified maps model. See the v1.0.107 and v1.0.106 entries in `History.md` for details. See the v1.0.106 entry at the top of `History.md` for the complete change list.

## Building

Run `buildDbDo.cmd` on a Windows machine with the .NET Framework 4.8 SDK (csc.exe + jsc.exe). It compiles `DbDo.js` to `DbDo.dll` and `DbDo.cs` to `DbDo.exe`. Compiled artifacts are deliberately excluded from this archive; rebuild locally. Regenerate the NVDA add-on and JAWS settings through the normal build/installer steps, and compile the installer with `"%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" DbDo_setup.iss`.

## Files in this archive

- `DbDo.cs` -- the single-file program source (the only .cs file on the csc line).
- `FkResolution.cs`, `ImportNormalization.cs`, `Lookups.cs` -- UNWIRED companion design files: specification plus code drops not yet on the build line. Their comments are updated to the `<singular>_id` convention. Wiring `FkResolution.cs` in (schema-truth relationship resolution via PRAGMA foreign_key_list) remains the recommended next structural step.
- `build_convention.py` -- the parser/builder for NFB2026Convention.db (pass the agenda .docx, or pre-extracted UTF-8 text).
- `rebuildSamples.py` -- the script that regenerated the five migrated sample databases (PK renames, FK REFERENCES updates, look/unq redesign, trigger rewrite, unq indexes). Edit the per-table config dictionaries at the top to tune look/unq column choices and re-run.
- Sample databases: `sample.db`, `northwind.db`, `chinook.db`, `collection.db`, `cellar.db` (all migrated), `lookups.db` (migrated; look/unq design preserved as the prototype), `NFB2026Convention.db` (rebuilt on the three-noun + maps + lookups model with `<singular>_id` keys).
- `Scripts/` -- demo SQL and dot-prompt scripts. `WineDrinkWindow.sql` already joined on `wine_id` and now matches the live schema. The three convention scripts still target the unmigrated NFB database and remain correct against it.
- `DbDo.inix` -- the shipped settings template (renamed from DbDo.ini; the [ConnectStrings] section demonstrates Inix fenced multi-line values).
- Documentation: `DbDo.md` (reference), `README.md`, `Announce.md`, `History.md`, `License.md`, plus their .htm renders regenerated via Pandoc; `Camel_Type_C#.md` (coding conventions).

## Known follow-ups

- `Metadata.DateSortColumns` is defined but currently unreferenced (dead constant; candidate for removal or for wiring a Sort by Date Edited command).
- `DbDo.md` describes a "Sort by Date Edited" command on Alt+D, but Alt+D is bound to Database Summary in code -- a pre-existing doc/binding discrepancy to resolve in a docs pass.
- The NFB convention database migration (PK convention plus the planned maps-table association model) is deferred by design.
