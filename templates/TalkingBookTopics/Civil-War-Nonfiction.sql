-- Civil-War-Nonfiction.sql -- true books about the American Civil War, best match first.
--
-- A phrase: words inside double quotes must come together, in that order. The fiction column then keeps nonfiction only.

SELECT b.title, b.author, b.issue, snippet(books_fts, 4, '', '', ' ... ', 20) AS passage
FROM books_fts JOIN books b ON b.book_id = books_fts.rowid
WHERE books_fts MATCH '"civil war"' AND b.fiction = 'nonfiction'
ORDER BY bm25(books_fts, 10.0, 5.0, 3.0, 2.0, 1.0, 1.0)
LIMIT 300;
