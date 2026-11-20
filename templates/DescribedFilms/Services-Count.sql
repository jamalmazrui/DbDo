-- Services-Count.sql -- how many described films each service carries, and how many of its offers use a synthetic voice.

SELECT o.service, count(DISTINCT o.film_id) AS films, sum(CASE WHEN o.voice = 'synthetic' THEN 1 ELSE 0 END) AS synthetic_offers
FROM offers AS o
GROUP BY o.service
ORDER BY films DESC, o.service;
