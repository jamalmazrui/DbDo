-- Space-Flight.sql -- books about spaceflight: space* finds space, spacecraft, spaceflight, spaceship and spacewalk.

SELECT b.title, b.author, b.issue, snippet(books_fts, 4, '', '', ' ... ', 20) AS passage
FROM books_fts JOIN books b ON b.book_id = books_fts.rowid
WHERE books_fts MATCH 'space* AND (astronaut* OR nasa OR orbit* OR moon OR mars)'
ORDER BY bm25(books_fts, 10.0, 5.0, 3.0, 2.0, 1.0, 1.0)
LIMIT 300;
