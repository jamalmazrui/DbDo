-- DayAtAGlance.sql - one convention day, ordered by start time.
-- Change the date literal to the day you want: 2026-07-03 (Friday)
-- through 2026-07-08 (Wednesday).
select e.start_time as Start, e.end_time as Finish, e.title as Event,
       l.name as Location
from events e
left join maps m on m.tbl1 = 'events' and m.prm1 = e.prm
                and m.kind = 'located_at' and m.tbl2 = 'locations'
left join locations l on l.prm = m.prm2
where e.event_date = '2026-07-06'
order by e.start_time, e.title;
