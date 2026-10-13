-- Recipe-Ingredients.sql -- every recipe with its ingredients and amounts.
--
-- One line per ingredient of each recipe, recipe by recipe, so a recipe can be
-- read through at the counter. Run it with every recipe, or filter the result
-- to the one you are making.

SELECT r.name AS recipe,
       i.name AS ingredient,
       coalesce(m.notes, '') AS amount
FROM recipes r
JOIN maps m ON m.tbl1 = 'recipes' AND m.prime1 = r.prime AND m.kind = 'uses' AND m.tbl2 = 'ingredients'
JOIN ingredients i ON i.prime = m.prime2
ORDER BY r.name, i.name;
