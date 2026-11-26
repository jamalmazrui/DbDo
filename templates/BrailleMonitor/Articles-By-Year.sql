-- Articles-By-Year.sql -- how many articles each year holds, and how many words.

SELECT substr(published, 1, 4) AS year, count(*) AS articles, sum(words) AS words
FROM articles
WHERE kind = 'article'
GROUP BY year
ORDER BY year DESC;
