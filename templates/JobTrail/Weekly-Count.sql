-- Weekly-Count.sql -- countable employer contacts per week.
--
-- Unemployment programs usually require a number of employer contacts each
-- week. This says how many you made, week by week, so the answer is a fact
-- before anybody asks. It counts only actions marked as counting: preparation
-- and inquiries that produced no application are excluded, as the rules say.

SELECT strftime('%Y-W%W', action_date) AS week,
       count(*) AS contacts,
       group_concat(DISTINCT kind) AS kinds
FROM actions
WHERE counts = 1 AND action_date IS NOT NULL AND action_date <> ''
GROUP BY week
ORDER BY week DESC;
