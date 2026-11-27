-- Blindness.sql -- books whose descriptions speak of blindness or blind people, best match first.
--
-- A full-text search limited to one column: annotation : (...) looks only in the description. rank orders the matches
-- by the weights stored in the database (a title match counts ten times a match in the description), and snippet reads the passage that matched, as plain text.

SELECT b.title, b.author, b.issue, snippet(books_fts, 4, '', '', ' ... ', 20) AS passage
FROM books_fts JOIN books b ON b.book_id = books_fts.rowid
WHERE books_fts MATCH 'annotation : (blind OR blindness OR braille)'
ORDER BY books_fts.rank
LIMIT 300;
