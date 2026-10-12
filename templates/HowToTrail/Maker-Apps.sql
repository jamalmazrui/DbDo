-- Maker-Apps.sql
-- Each maker (a company or person in the contacts table) and the apps it
-- makes, via the 'makes' relationship, with each app's platform. The
-- canonical "follow one record to its related records" query for this
-- database.
--
-- Add a line like  AND c.enterprise = 'Apple'  to the WHERE clause to focus
-- on one maker.
SELECT coalesce(nullif(c.enterprise, ''), trim(coalesce(c.first_name, '') || ' ' || coalesce(c.last_name, ''))) AS maker,
       COUNT(*)                                                AS app_count,
       group_concat(a.name || ' (' || a.platform || ')', ', ') AS apps
FROM maps m
JOIN contacts c ON c.prime = m.prime1
JOIN apps a     ON a.prime = m.prime2
WHERE m.kind = 'makes' AND m.tbl1 = 'contacts' AND m.tbl2 = 'apps'
GROUP BY c.prime
ORDER BY app_count DESC, maker;
