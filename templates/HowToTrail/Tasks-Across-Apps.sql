-- Tasks-Across-Apps.sql
-- Tasks the book shows how to do in more than one app -- the payoff of
-- keeping tasks app-agnostic and tying each app-specific procedure to the
-- shared task through the maps table. Each row is a task and the apps that
-- have a method for it.
SELECT ft.unq2                          AS task,
       COUNT(DISTINCT wa.unq2)          AS app_count,
       group_concat(DISTINCT wa.unq2)   AS apps
FROM maps ft
JOIN maps wa ON wa.tbl1 = 'methods' AND wa.unq1 = ft.unq1 AND wa.kind = 'with_app'
WHERE ft.kind = 'for_task'
GROUP BY ft.unq2
HAVING app_count > 1
ORDER BY app_count DESC, task;
