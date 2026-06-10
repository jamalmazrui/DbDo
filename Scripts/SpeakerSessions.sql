-- Description: Every session a given person is part of, with their role, walked
-- through the associations junction. Persons live in contacts (kind='person')
-- with first_name/last_name. Change the surname on the WHERE line. The joins are
-- qualified: associations.contact_id -> contacts.id, associations.event_id ->
-- events.id, associations.place_id -> places.id.
SELECT
    trim(c.first_name || ' ' || c.last_name) AS person,
    a.role       AS role,
    e.day        AS day,
    e.start_time AS start,
    e.title      AS session,
    p.name       AS place
FROM associations a
JOIN contacts c ON a.contact_id = c.id
JOIN events   e ON a.event_id   = e.id
LEFT JOIN places p ON a.place_id = p.id
WHERE c.last_name = 'Riccobono'   -- change to any contact
ORDER BY e.day, e.start_time;
