-- Blindness.sql -- books whose descriptions speak of blindness or blind people, best match first.
--
-- A full-text search limited to one column: annotation : (...) looks only in the description. bm25 ranks the matches
-- (a title match counts ten times a match in the description), and snippet reads the passage that matched, as plain text.

SELECT b.title, b.author, b.issue, snippet(books_fts, 4, '', '', ' ... ', 20) AS passage
FROM books_fts JOIN books b ON b.book_id = books_fts.rowid
WHERE books_fts MATCH 'annotation : (blind OR blindness OR braille)'
ORDER BY bm25(books_fts, 10.0, 5.0, 3.0, 2.0, 1.0, 1.0)
LIMIT 300;
