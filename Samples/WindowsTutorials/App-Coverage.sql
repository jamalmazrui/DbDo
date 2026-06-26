-- App-Coverage.sql
-- How thoroughly the primer covers each app: the number of step-by-step
-- methods and the number of distinct tasks recorded for it. A quick
-- statistics view of where the book's depth lies (Word, Outlook, Excel, and
-- the screen readers themselves tend to top the list).
SELECT a.name                    AS app,
       a.category                AS category,
       COUNT(DISTINCT wm.unq1)   AS methods,
       COUNT(DISTINCT ft.unq2)   AS tasks
FROM apps a
LEFT JOIN maps wm ON wm.kind = 'with_app' AND wm.tbl2 = 'apps' AND wm.unq2 = a.name
LEFT JOIN maps ft ON ft.kind = 'for_task' AND ft.unq1 = wm.unq1
GROUP BY a.name, a.category
ORDER BY methods DESC, app;
