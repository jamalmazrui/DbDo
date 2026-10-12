-- Tasks-Across-Apps.sql
-- Tasks the guide shows how to do in more than one app -- the payoff of
-- keeping tasks app-agnostic and tying each app-specific procedure to the
-- shared task through the maps table. Each row is a task, its platform, and
-- the apps that have a method for it.
SELECT t.name                          AS task,
       t.platform                      AS platform,
       COUNT(DISTINCT a.prime)         AS app_count,
       group_concat(DISTINCT a.name)   AS apps
FROM maps ft
JOIN tasks t   ON t.prime = ft.prime2
JOIN maps wa   ON wa.kind = 'with_app' AND wa.tbl1 = 'methods' AND wa.tbl2 = 'apps' AND wa.prime1 = ft.prime1
JOIN apps a    ON a.prime = wa.prime2
WHERE ft.kind = 'for_task' AND ft.tbl1 = 'methods' AND ft.tbl2 = 'tasks'
GROUP BY t.prime
HAVING app_count > 1
ORDER BY app_count DESC, task;
