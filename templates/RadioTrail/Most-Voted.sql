-- Most-Voted.sql -- the 100 stations Radio Browser's listeners have voted for most.

SELECT name, genre, state, country, votes
FROM stations
ORDER BY CAST(votes AS INTEGER) DESC
LIMIT 100;
