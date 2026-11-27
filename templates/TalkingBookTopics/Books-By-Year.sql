-- Books-By-Year.sql -- how many books Talking Book Topics announced each year.

SELECT substr(i.published, 1, 4) AS year, count(*) AS books
FROM books b JOIN issues i ON i.issue_id = b.issue_id
GROUP BY substr(i.published, 1, 4)
ORDER BY year;
