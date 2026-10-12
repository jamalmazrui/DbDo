-- Methods-For-App.sql
-- Every step-by-step method recorded for one app, with the task it
-- accomplishes. Change 'Microsoft Word' on the WHERE line to any app name --
-- for example 'JAWS', 'Google Drive' or 'Apple Books'. Where an app has the
-- same name on both platforms, the platform line narrows it to one.
SELECT m.name      AS method,
       m.platform  AS platform,
       t.name      AS task
FROM apps a
JOIN maps wa      ON wa.kind = 'with_app' AND wa.tbl1 = 'methods' AND wa.tbl2 = 'apps' AND wa.prime2 = a.prime
JOIN methods m    ON m.prime = wa.prime1
LEFT JOIN maps ft ON ft.kind = 'for_task' AND ft.tbl1 = 'methods' AND ft.tbl2 = 'tasks' AND ft.prime1 = m.prime
LEFT JOIN tasks t ON t.prime = ft.prime2
WHERE a.name = 'Microsoft Word'
--  AND a.platform = 'Windows'
ORDER BY task, method;
