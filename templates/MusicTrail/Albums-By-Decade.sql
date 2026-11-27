-- Albums-By-Decade.sql -- how many of the albums came out in each decade.

SELECT (CAST(year AS INTEGER) / 10) * 10 AS decade, count(*) AS albums
FROM albums
WHERE CAST(year AS INTEGER) > 0
GROUP BY decade
ORDER BY decade;
