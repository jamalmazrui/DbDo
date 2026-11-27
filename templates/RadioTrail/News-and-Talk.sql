-- News-and-Talk.sql -- news and talk stations in the United States, the most voted first.

SELECT name, state, genre, votes
FROM stations
WHERE countrycode = 'US' AND (genre LIKE '%news%' OR genre LIKE '%talk%')
ORDER BY CAST(votes AS INTEGER) DESC
LIMIT 300;
