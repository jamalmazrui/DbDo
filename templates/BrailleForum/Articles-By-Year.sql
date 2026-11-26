-- Articles-By-Year.sql -- how many articles each year holds, and how many came from web pages.

SELECT substr(published, 1, 4) AS year, count(*) AS articles, sum(format = 'web page') AS web_pages, sum(words) AS words
FROM articles
WHERE kind = 'article'
GROUP BY year
ORDER BY year DESC;
