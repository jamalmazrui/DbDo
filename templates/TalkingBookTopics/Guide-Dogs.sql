-- Guide-Dogs.sql -- books about guide dogs: the word guide within three words of dog, dogs or doggy.
--
-- NEAR finds words close together in any order; dog* finds every word that begins with dog.

SELECT b.title, b.author, b.issue, snippet(books_fts, 4, '', '', ' ... ', 20) AS passage
FROM books_fts JOIN books b ON b.book_id = books_fts.rowid
WHERE books_fts MATCH 'NEAR(guide dog*, 3)'
ORDER BY bm25(books_fts, 10.0, 5.0, 3.0, 2.0, 1.0, 1.0)
LIMIT 300;
