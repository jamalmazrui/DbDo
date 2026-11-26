-- Writers.sql -- who has written the most, from the writers table, with the months of their first and last article.

SELECT name, articles, first_published, last_published
FROM writers
ORDER BY articles DESC, name;
