-- No-Strong-Language.sql -- books about baseball that carry no warning of strong language.
--
-- NOT leaves out what follows it; tags : "strong language" looks for that phrase in the advisories only.

SELECT b.title, b.author, b.issue, snippet(books_fts, 4, '', '', ' ... ', 20) AS passage
FROM books_fts JOIN books b ON b.book_id = books_fts.rowid
WHERE books_fts MATCH 'baseball NOT tags : "strong language"'
ORDER BY books_fts.rank
LIMIT 300;
