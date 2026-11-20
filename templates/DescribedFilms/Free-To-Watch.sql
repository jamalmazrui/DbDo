-- Free-To-Watch.sql -- films you can watch at no extra cost: free with ads, free with Prime, or free outright.
--
-- One row an offer, so a film free in two places is listed twice. The voice column says "synthetic" when ADP marks
-- the description as text-to-speech, for those who would rather hear a human narrator.

SELECT f.title, f.year, o.service, o.terms, o.voice
FROM offers AS o JOIN films AS f ON f.film_id = o.film_id
WHERE o.terms LIKE '%Free%'
ORDER BY f.title, f.year, o.service;
