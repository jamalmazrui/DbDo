-- Quick-Meals.sql -- recipes ready in half an hour or less.
--
-- Preparation and cooking time together, the quickest first, for the evening
-- there is no time to spare.

SELECT name,
       coalesce(prep_minutes, 0) + coalesce(cook_minutes, 0) AS total_minutes,
       course, servings
FROM recipes
WHERE coalesce(prep_minutes, 0) + coalesce(cook_minutes, 0) <= 30
ORDER BY total_minutes, name;
