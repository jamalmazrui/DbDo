-- renameToPrime.sql -- rename the computed key column prm to prime.
--
-- WHY. The column holds the PRIME fields of a record: the ones that identify it
-- to a person, joined with a vertical bar. It is not a formal primary key -- the
-- <table>_id column is that -- so "prime" says what it is, and "prm" said only
-- that somebody was saving three letters.
--
-- WHAT THIS DOES. One ALTER TABLE per table that still has the old name. SQLite
-- renames a generated column in place, and nothing in the schema refers to it by
-- name, so the rebuild is not needed. Run it once against a database made before
-- DbDo started writing "prime"; running it twice is harmless, because a table
-- that has already been renamed simply is not listed.
--
-- HOW TO RUN IT. Open the database in DbDo and run this from the Scripts menu,
-- or paste it at the dot prompt. Back the file up first, as with any schema
-- change.

-- The data tables in this database that still carry the old name:
--   SELECT name FROM pragma_table_list
--   WHERE schema='main' AND type='table'
--     AND EXISTS (SELECT 1 FROM pragma_table_info(name) WHERE name='prm');
--
-- DbDo's own script generates the statements; these are the standard tables
-- every DbDo database has, plus the two columns in maps.

ALTER TABLE lookups RENAME COLUMN prm TO prime;
ALTER TABLE maps RENAME COLUMN prm TO prime;
ALTER TABLE maps RENAME COLUMN prm1 TO prime1;
ALTER TABLE maps RENAME COLUMN prm2 TO prime2;

-- Then one line per data table, for example:
-- ALTER TABLE contacts RENAME COLUMN prm TO prime;
