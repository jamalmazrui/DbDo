-- Kindle-Books.sql -- books by blind authors that can be read on a Kindle, newest first.

SELECT title, by, year, format
FROM books
WHERE format LIKE '%Kindle%'
ORDER BY year DESC, title;
