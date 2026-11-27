-- Structured-Discovery.sql -- articles about structured discovery, the NFB's approach to teaching blindness skills.
--
-- A full-text search: articles_fts MATCH finds the words, bm25 ranks the matches (a title match counts ten times a
-- match in the words, a writer match five times), and snippet gives a passage around them, as plain text. Change the
-- words between the quotes to search for something else; the BrailleMonitor part of DbDo's guide shows the forms a search takes.

SELECT a.title, a.writer, a.published, snippet(articles_fts, -1, '', '', ' ... ', 24) AS passage
FROM articles_fts JOIN articles a ON a.article_id = articles_fts.rowid
WHERE articles_fts MATCH '"structured discovery"' AND a.kind = 'article'
ORDER BY bm25(articles_fts, 10.0, 5.0, 1.0, 1.0)
LIMIT 200;
