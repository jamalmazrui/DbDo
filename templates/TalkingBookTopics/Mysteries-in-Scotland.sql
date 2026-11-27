-- Mysteries-in-Scotland.sql -- mysteries set in Scotland: a word in the description, with a category to narrow it.

SELECT b.title, b.author, b.issue, snippet(books_fts, 4, '', '', ' ... ', 20) AS passage
FROM books_fts JOIN books b ON b.book_id = books_fts.rowid
WHERE books_fts MATCH '(scotland OR scottish OR edinburgh OR glasgow)' AND b.category LIKE '%mystery%'
ORDER BY bm25(books_fts, 10.0, 5.0, 3.0, 2.0, 1.0, 1.0)
LIMIT 300;
