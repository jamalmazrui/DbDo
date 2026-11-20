-- Documentaries.sql -- the documentaries, newest first, with where to watch each one.

SELECT title, year, services
FROM films
WHERE genres LIKE '%Documentary%'
ORDER BY year DESC, title;
