-- Topic-By-Year.sql -- how many articles about a subject each year holds, so you hear when it rose and fell.
--
-- Change the words between the single quotes to follow another subject; the BrailleMonitor part of DbDo's guide shows
-- the forms a search takes.

SELECT substr(a.published, 1, 4) AS year, count(*) AS articles
FROM articles_fts JOIN articles a ON a.article_id = articles_fts.rowid
WHERE articles_fts MATCH '"minimum wage"' AND a.kind = 'article'
GROUP BY year
ORDER BY year;
