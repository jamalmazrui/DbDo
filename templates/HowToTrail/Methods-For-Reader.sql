-- Methods-For-Reader.sql
-- Every step-by-step method that involves one screen reader, with the task it
-- accomplishes and the app it runs in. This is the canonical "go to all the
-- records of a screen reader" query: it follows BOTH the with_app link (the
-- procedures that configure the reader itself) and the with_reader link (the
-- procedures in other apps whose steps are written for that reader).
--
-- Adapt it by changing 'JAWS' on the WHERE line to 'NVDA' or 'Narrator'.
SELECT m.name                       AS method,
       ft.unq2                      AS task,
       COALESCE(wa.unq2, '(general)') AS app
FROM maps r
JOIN methods m  ON m.unq = r.unq1
LEFT JOIN maps ft ON ft.tbl1 = 'methods' AND ft.unq1 = m.unq AND ft.kind = 'for_task'
LEFT JOIN maps wa ON wa.tbl1 = 'methods' AND wa.unq1 = m.unq AND wa.kind = 'with_app'
WHERE r.kind IN ('with_reader', 'with_app') AND r.tbl2 = 'apps' AND r.unq2 = 'JAWS'
ORDER BY app, task;
