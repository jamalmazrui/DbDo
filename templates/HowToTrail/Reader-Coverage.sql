-- Reader-Coverage.sql
-- A statistics summary for the Windows screen readers: for each, how many
-- methods involve it (configuring it, or written for it in another app), and
-- how many distinct apps and tasks those methods span. A compact measure of
-- how much of the guide each reader reaches.
SELECT a.name                       AS reader,
       COUNT(DISTINCT mp.prime1)    AS methods,
       COUNT(DISTINCT wa.prime2)    AS apps_touched,
       COUNT(DISTINCT ft.prime2)    AS tasks_touched
FROM apps a
JOIN maps mp      ON mp.kind IN ('with_app', 'with_reader') AND mp.tbl1 = 'methods' AND mp.tbl2 = 'apps' AND mp.prime2 = a.prime
LEFT JOIN maps wa ON wa.kind = 'with_app' AND wa.tbl1 = 'methods' AND wa.tbl2 = 'apps' AND wa.prime1 = mp.prime1
LEFT JOIN maps ft ON ft.kind = 'for_task' AND ft.tbl1 = 'methods' AND ft.tbl2 = 'tasks' AND ft.prime1 = mp.prime1
WHERE a.category = 'screen_reader'
GROUP BY a.prime
ORDER BY methods DESC, reader;
