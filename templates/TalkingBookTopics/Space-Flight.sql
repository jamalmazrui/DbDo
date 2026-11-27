-- Space-Flight.sql -- books about spaceflight: space* finds space, spacecraft, spaceflight, spaceship and spacewalk.

SELECT b.title, b.author, b.issue, snippet(books_fts, 4, '', '', ' ... ', 20) AS passage
FROM books_fts JOIN books b ON b.book_id = books_fts.rowid
WHERE books_fts MATCH 'space* AND (astronaut* OR nasa OR orbit* OR moon OR mars)'
ORDER BY books_fts.rank
LIMIT 300;
