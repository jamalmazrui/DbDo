-- Methods-For-App.sql
-- Every step-by-step method recorded for one app, with the task it
-- accomplishes. Change 'iOS' on the WHERE line to any app name -- for
-- example 'Apple Books' or 'Overcast'. Because VoiceOver is the only screen
-- reader in this guide, every method is performed with VoiceOver, so there
-- is no separate reader to choose: this is the canonical "go to all the
-- records of an app" query.
SELECT m.name   AS method,
       ft.unq2  AS task
FROM maps wa
JOIN methods m   ON m.unq = wa.unq1
LEFT JOIN maps ft ON ft.tbl1 = 'methods' AND ft.unq1 = m.unq AND ft.kind = 'for_task'
WHERE wa.kind = 'with_app' AND wa.tbl2 = 'apps' AND wa.unq2 = 'iOS'
ORDER BY task;
