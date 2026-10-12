-- Methods-For-Reader.sql
-- Every step-by-step method that involves one screen reader, with the task it
-- accomplishes and the app it runs in. This is the canonical "go to all the
-- records of a screen reader" query: it follows BOTH the with_app link (the
-- procedures that configure the reader itself) and the with_reader link (the
-- procedures in other apps whose steps are written for that reader).
--
-- Adapt it by changing 'JAWS' on the WHERE line to 'NVDA' or 'Narrator'. The
-- iOS methods are written for VoiceOver, which the guide treats as part of
-- iOS rather than as a separate app.
SELECT m.name                          AS method,
       t.name                          AS task,
       COALESCE(ap.name, '(general)')  AS app
FROM apps rd
JOIN maps r        ON r.kind IN ('with_reader', 'with_app') AND r.tbl1 = 'methods' AND r.tbl2 = 'apps' AND r.prime2 = rd.prime
JOIN methods m     ON m.prime = r.prime1
LEFT JOIN maps ft  ON ft.kind = 'for_task' AND ft.tbl1 = 'methods' AND ft.tbl2 = 'tasks' AND ft.prime1 = m.prime
LEFT JOIN tasks t  ON t.prime = ft.prime2
LEFT JOIN maps wa  ON wa.kind = 'with_app' AND wa.tbl1 = 'methods' AND wa.tbl2 = 'apps' AND wa.prime1 = m.prime
LEFT JOIN apps ap  ON ap.prime = wa.prime2
WHERE rd.name = 'JAWS' AND rd.category = 'screen_reader'
GROUP BY m.prime, t.prime, ap.prime
ORDER BY app, task, method;
