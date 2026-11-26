-- Authors.sql -- who has written the most, with the years they wrote in.

SELECT author, count(*) AS articles, min(substr(published, 1, 4)) AS first_year, max(substr(published, 1, 4)) AS last_year
FROM articles
WHERE kind = 'article' AND author IS NOT NULL AND author <> ''
GROUP BY author
ORDER BY articles DESC, author;
