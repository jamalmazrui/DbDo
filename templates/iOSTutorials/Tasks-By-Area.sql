-- Tasks-By-Area.sql
-- A statistics overview: how many topics the guide covers in each thematic
-- area (its major sections), from most to least. Because this guide is
-- organized by subject rather than by app, this is the clearest map of where
-- the book spends its attention.
SELECT area        AS area,
       COUNT(*)    AS topics
FROM tasks
WHERE area IS NOT NULL
GROUP BY area
ORDER BY topics DESC, area;
