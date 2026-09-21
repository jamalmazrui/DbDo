-- Ats-Experience.sql -- which application systems actually worked.
--
-- Your own record of how far applications got, grouped by the system they went
-- through. Over a search this becomes evidence about which employers' systems
-- are usable, which is worth more to the next person than to you.

SELECT ats AS application_system,
       count(*) AS applications,
       sum(CASE WHEN status IN ('interviewing','offer') THEN 1 ELSE 0 END) AS reached_interview,
       sum(CASE WHEN status = 'rejected' THEN 1 ELSE 0 END) AS rejected,
       sum(CASE WHEN status = 'applied' THEN 1 ELSE 0 END) AS no_answer_yet
FROM jobs
WHERE applied_date IS NOT NULL AND applied_date <> ''
GROUP BY ats
ORDER BY applications DESC, ats;
