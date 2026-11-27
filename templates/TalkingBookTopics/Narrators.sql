-- Narrators.sql -- who has read the most books announced in Talking Book Topics, with their first and last issues.

SELECT name, books, first_issue, last_issue
FROM narrators
ORDER BY books DESC, name;
