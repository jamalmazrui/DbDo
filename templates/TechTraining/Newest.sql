-- Newest.sql -- presentations from this year and last, newest first.
--
-- Technology moves quickly, so the newest presentations on a topic are often the
-- ones to hear first. Presentations whose year is unknown are left out.

SELECT year, title, minutes, medium, category
FROM presentations
WHERE year IS NOT NULL AND CAST(year AS INTEGER) >= CAST(strftime('%Y', 'now') AS INTEGER) - 1
ORDER BY year DESC, category, title;
