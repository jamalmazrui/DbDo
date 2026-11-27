-- Top-5-By-Genre.sql -- each genre's five highest-ranked albums, genre by genre.

SELECT genre, rank, name, artist, year
FROM albums
WHERE CAST(rank AS INTEGER) BETWEEN 1 AND 5
ORDER BY genre, CAST(rank AS INTEGER);
