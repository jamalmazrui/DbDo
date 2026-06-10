-- Description: One day at a glance -- every session on a chosen day with its
-- place, ticket status, and how many people are listed. Change the date. The
-- people count is a correlated subquery over associations keyed on events.id.
SELECT
    e.start_time AS start,
    e.end_time   AS finish,
    e.title      AS session,
    e.kind       AS kind,
    p.name       AS place,
    iif(e.ticketed = 1, 'ticketed', '') AS ticketed,
    (SELECT count(*) FROM associations a WHERE a.event_id = e.id) AS people
FROM events e
LEFT JOIN places p ON e.place_id = p.id
WHERE e.day = '2026-07-05' AND e.kind <> 'Convention'
ORDER BY e.start_time, p.name;
