-- Newest.sql -- films released this year and last, newest first, with where to watch each one.

SELECT title, year, genres, services
FROM films
WHERE year IS NOT NULL AND CAST(year AS INTEGER) >= CAST(strftime('%Y', 'now') AS INTEGER) - 1
ORDER BY year DESC, title;
