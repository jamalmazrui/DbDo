-- Description: Wine cellar -- what's in the drink window right now?
-- For every wine in the cellar where bottles remain (status = 'held'),
-- report producer, vintage, varietal, and where in its drink window it
-- currently sits. Rows are sorted by "urgency": wines closest to the
-- end of their drink window come first. Use this query to plan what to
-- open tonight, or to identify wines drifting past their prime.
--
-- Built for the cellar.db schema. The current year is hardwired as
-- 2026 below; edit the CAST in the WHERE/SELECT to use a different
-- reference year.

SELECT
  w.producer,
  w.vintage,
  w.varietal,
  SUM(b.qty)            AS bottles_held,
  w.drink_from,
  w.drink_to,
  CASE
    WHEN 2026 < w.drink_from THEN 'too young (' || (w.drink_from - 2026) || ' years to go)'
    WHEN 2026 > w.drink_to   THEN 'past peak (' || (2026 - w.drink_to)  || ' years over)'
    ELSE 'in window'
  END                   AS drink_status,
  (w.drink_to - 2026)   AS years_remaining
FROM wines w
JOIN bottles b ON b.wine_id = w.wine_id AND b.status = 'held'
WHERE w.drink_to IS NOT NULL
GROUP BY w.wine_id
ORDER BY years_remaining ASC, w.producer;
