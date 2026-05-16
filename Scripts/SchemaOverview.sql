-- Description: Print the full schema of every table and view in the open database.
-- Reads the sqlite_master table directly; one SELECT per object type so the
-- result dialog shows tables before views before indexes.

SELECT 'TABLE: ' || name AS object, sql FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;

SELECT 'VIEW:  ' || name AS object, sql FROM sqlite_master WHERE type = 'view'  ORDER BY name;

SELECT 'INDEX: ' || name AS object, sql FROM sqlite_master WHERE type = 'index' AND sql IS NOT NULL ORDER BY name;
