-- Maker-Apps.sql
-- Each maker (a company in the contacts table) and the apps it makes, via
-- the 'makes' relationship. Apple makes most of the built-in apps; the rest
-- come from a wide range of third parties. The canonical "follow one record
-- to its related records" query for this database.
--
-- Add a line like  AND m.unq1 = 'Apple'  to the WHERE clause to focus on one
-- maker.
SELECT m.unq1                       AS maker,
       COUNT(*)                     AS app_count,
       group_concat(a.name, ', ')   AS apps
FROM maps m
JOIN apps a ON a.name = m.unq2
WHERE m.kind = 'makes'
GROUP BY m.unq1
ORDER BY app_count DESC, maker;
