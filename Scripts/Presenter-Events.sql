-- Presenter-Events.sql
-- List every event a chosen presenter appears on, in time order, showing
-- the date, start time, and title.
--
-- How to use: open NFB2026Convention.db, then run this file with Invoke
-- Script. The results appear in a dialog you can read and copy. Change
-- the last name on the WHERE line to the presenter you care about -- for
-- a fuller list try Burke, Riccobono, or O'Connor.
--
-- How it works: presenters are people in the contacts table; the link
-- between a contact and the events they present is stored in the maps
-- table (kind = 'presents'). Events have no stored key column, so the
-- join is made on the computed string  date|time|title , which is how
-- maps records point at an event.

SELECT
  e.event_date AS date,
  e.start_time AS starts,
  e.title      AS event
FROM contacts c
JOIN maps m
  ON  m.tbl1 = 'contacts'
  AND m.kind = 'presents'
  AND m.tbl2 = 'events'
  AND m.unq1 = c.first_name || '|' || IFNULL(c.middle_name, '') || '|' || c.last_name
JOIN events e
  ON  e.event_date || '|' || e.start_time || '|' || e.title = m.unq2
WHERE c.last_name = 'Mallett'
ORDER BY e.event_date, e.start_time;
