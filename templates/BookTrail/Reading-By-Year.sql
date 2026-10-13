-- Reading-By-Year.sql -- how many books you finished each year, and in what form.
--
-- A year's reading at a glance: the count, and how they came -- BARD audio,
-- Bookshare, braille, eBook. Books with no finishing date are left out.

SELECT substr(date_read, 1, 4) AS year,
       count(*) AS books,
       group_concat(DISTINCT format) AS formats
FROM books
WHERE status = 'read' AND coalesce(date_read, '') <> ''
GROUP BY year
ORDER BY year DESC;
