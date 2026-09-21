-- Reader-Coverage.sql
-- A statistics summary for the three screen readers: for each, how many
-- methods involve it (configuring it, or written for it in another app), and
-- how many distinct apps and tasks those methods span. A compact measure of
-- how much of the book each reader reaches.
SELECT a.name                     AS reader,
       COUNT(DISTINCT mp.unq1)    AS methods,
       COUNT(DISTINCT wa.unq2)    AS apps_touched,
       COUNT(DISTINCT ft.unq2)    AS tasks_touched
FROM apps a
JOIN maps mp      ON mp.unq2 = a.name AND mp.kind IN ('with_app', 'with_reader')
LEFT JOIN maps wa ON wa.unq1 = mp.unq1 AND wa.kind = 'with_app'
LEFT JOIN maps ft ON ft.unq1 = mp.unq1 AND ft.kind = 'for_task'
WHERE a.category = 'screen_reader'
GROUP BY a.name
ORDER BY methods DESC;
