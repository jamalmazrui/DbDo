# DbDo System Table Names: Analysis and Proposal

*4 November 2026. Decided the same evening: **defs**, with def_id, is DbDo's name for this table, and the naming rule below is a DbDo guideline. The weighing is kept as the record of why.*

DbDo's own tables are now **lookups** and **maps**. A third, **views**, has been retired: see the last section. This note weighs a new name for lookups.

## The rule the names should follow

A DbDo system table is named with a plain English plural ending in "s." Its key is the singular plus "_id," such as map_id. The name should also:

- **Avoid SQL keywords,** so it can't be mistaken for SQL.
- **Not begin like a standard field name:** added, edited, id, look, marked, notes, prime, tags or url.
- **Begin with a letter no common table uses,** so a letter press in a list of tables lands on it at once.

The template tables already use the letters a, b, c, d, e, g, i, j, l, m, o, p, r, s, t and v. The free letters are f, h, k, n, q, u, w, x, y and z.

## What the table holds

Four kinds of row: a description of the database, of each table, and of each field; each field's pattern; and the choices F4 offers. So it's DbDo's data dictionary as well as its list of permitted values.

## The candidates

- **blocks, block_id:** too vague. A block could be anything, and nothing in the table is a block. B is taken by books.
- **defs, def_id:** short for definitions, and every row is one: what the database, a table or a field is; the form a value must take; the values allowed. It's short to type at the SQLite prompt, and nothing like "look." It's an abbreviation, though a common one in programming, and D is shared with docs, in JobTrail only.
- **forms, form_id:** misleading. DbDo's record forms are dialogs, so "forms" would suggest them. F is free.
- **frames, frame_id:** too loose. "Frame of reference" fits only faintly. F is free.
- **lookups, lookup_id:** the standard database term for a table of permitted values, and familiar to you. But it sounds like "look," an unrelated standard field, and it now holds more than lookups. L is shared with lessons.
- **metas, meta_id:** awkward. "Meta" is a prefix, so its plural isn't an ordinary word. M is shared with maps, the next table in the list.
- **settings, setting_id:** suggests options and preferences, not descriptions and permitted values. S is the busiest letter in the templates.
- **Others weighed:**
  - **hints:** matches Shift+F1 and F4, but would blur with Homer's Hints.inix.
  - **norms:** fits values and patterns, not descriptions.
  - **terms:** fits values and descriptions; T is taken by tracks.
  - **values:** an SQL keyword.

## Recommendation

**defs, with def_id.** It names what every row is, a definition. It's the shortest of all to type. It removes the echo of "look," and it was one of your own candidates. If you'd rather not rename, **lookups** remains a sound, standard name, and the guide can say plainly that look is a field and lookups is a table.

## What a rename would involve

- **The kit:** checkDb, the conversion, and the homer-db skill's schema.
- **DbDo:** about 120 places in DbDo.cs, plus describeFields, checkTemplates and fieldDescriptions.inix.
- **The templates:** renamed by the build itself: the table, lookup_id and the edited trigger.
- **The guide and tutorials.**
- **Older databases:** DbDo renames an older table when it opens one, so nothing breaks.

## The views table, retired

A table named views kept each table's shown columns, sort and filter inside the database. DbDo created it by itself, the first time it saved a table's state, so a database sent to someone would arrive knowing its columns. It was never approved, and it added nothing:

- **Its rows were already elsewhere:** every one of its 147 rows across the templates was already in the template's .inix. 125 were identical, and the 22 that differed were overridden by the .inix, which DbDo reads first.
- **Its purpose is now served by defaults:** look's fields are the default columns and prime's fields the default order, in any database.

It is now retired:

- **The build** drops it from every template, along with its descriptions.
- **DbDo** no longer reads or writes it. When DbDo opens an older database that has one, it gives the .inix any row the .inix lacks, then drops the table.
- **The kit** no longer creates or checks it.

A database may of course hold SQL views of its own; SQLite tells views from tables.

## files, added 5 November 2026

A third table of DbDo's own, **files**, with file_id, holds the files a database carries -- scripts, report and transfer definitions, and its settings as settings.inix -- so the .db alone is the whole package. It was not folded into defs, whose every row is one definition of a database, table or field, nor into maps, whose rows link records: a script or a report definition is a whole document with a name. The name follows the rule: a plain plural ending in s, not an SQL keyword, not the start of a standard field, and F is a letter no template table uses.
