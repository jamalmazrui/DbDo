-- App-Coverage.sql
-- How thoroughly the guide covers each app: the number of step-by-step
-- methods and the number of distinct tasks recorded for it. A quick
-- statistics view of where the hands-on detail lies, for Windows and iOS.
--
-- An app's identity is its name and platform (Word|Windows), so the joins
-- match each record's own prime and name both tables on every map row: the
-- same name on two platforms stays two apps.
SELECT a.name                     AS app,
       a.platform                 AS platform,
       a.category                 AS category,
       COUNT(DISTINCT wm.prime1)  AS methods,
       COUNT(DISTINCT ft.prime2)  AS tasks
FROM apps a
LEFT JOIN maps wm ON wm.kind = 'with_app' AND wm.tbl1 = 'methods' AND wm.tbl2 = 'apps' AND wm.prime2 = a.prime
LEFT JOIN maps ft ON ft.kind = 'for_task' AND ft.tbl1 = 'methods' AND ft.tbl2 = 'tasks' AND ft.prime1 = wm.prime1
GROUP BY a.prime
ORDER BY methods DESC, app, platform;
