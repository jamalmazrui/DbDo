-- Description: The convention schedule -- each session with its place and the
-- project (track) it belongs to. Sessions are the child events of the
-- convention; the project is reached through the associations junction, so we
-- take the distinct project linked to each event. Keys are qualified
-- (events.id, places.id, projects.id) because every primary key is now "id".
SELECT
    e.day        AS day,
    e.start_time AS start,
    e.title      AS session,
    e.kind       AS kind,
    p.name       AS place,
    (SELECT pr.name FROM associations a JOIN projects pr ON a.project_id = pr.id
     WHERE a.event_id = e.id AND a.project_id IS NOT NULL LIMIT 1) AS project
FROM events e
LEFT JOIN places p ON e.place_id = p.id
WHERE e.kind <> 'Convention'
ORDER BY e.day, e.start_time, p.name;
