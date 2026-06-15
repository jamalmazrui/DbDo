-- Convention-Stats.sql
-- A few "who and what is most popular" questions about the NFB 2026
-- convention, answered by counting rows in the maps table. Open
-- NFB2026Convention.db and run this with Invoke Script; each query's
-- results appear in turn, ranked from most to least.
--
-- These cross-table counts need a GROUP BY, which is why they are SQL
-- rather than dot-prompt commands. For single-table questions you do not
-- need SQL at all -- see the notes at the bottom.

-- 1. Most popular speakers, by number of presentations.
SELECT
  c.first_name || ' ' || c.last_name AS speaker,
  COUNT(*)                           AS presentations
FROM maps m
JOIN contacts c
  ON c.first_name || '|' || IFNULL(c.middle_name, '') || '|' || c.last_name = m.unq1
WHERE m.tbl1 = 'contacts' AND m.kind = 'presents' AND m.tbl2 = 'events'
GROUP BY m.unq1
ORDER BY presentations DESC, speaker
LIMIT 10;

-- 2. Most popular locations, by number of events held there.
SELECT
  l.name   AS room,
  l.hotel  AS hotel,
  COUNT(*) AS events
FROM maps m
JOIN locations l
  ON l.name || '|' || IFNULL(l.hotel, '') = m.unq2
WHERE m.tbl1 = 'events' AND m.kind = 'located_at' AND m.tbl2 = 'locations'
GROUP BY m.unq2
ORDER BY events DESC, room
LIMIT 10;

-- 3. Busiest days, by number of events.
SELECT
  event_date AS date,
  COUNT(*)   AS events
FROM events
GROUP BY event_date
ORDER BY events DESC;

-- Single-table questions need no SQL. From the events table you can type,
-- at the dot prompt:
--   filter event_date = '2026-07-03'    (the quotes here are ADO's filter
--                                         grammar for a text value, passed
--                                         through as written -- DbDo adds
--                                         no quoting of its own)
--   yield                               (how many events that day)
--   longest title                       (the longest session title)
--   reset-filter
--
-- Where DbDo's "type it verbatim" rule shows: value commands like
--   find National Federation of the Blind
--   jump Amateur Radio
-- take the whole tail literally -- no surrounding quotes, no escaping.
