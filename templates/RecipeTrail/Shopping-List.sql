-- Shopping-List.sql -- what to buy for the recipes you have marked.
--
-- Mark the recipes you mean to cook (Space on a row), then run this. Each
-- ingredient appears once, grouped by where it sits in the store, with the
-- amount each marked recipe needs and which recipe needs it -- so two recipes
-- that both use butter show both amounts on butter's line. The amounts are
-- the ones each recipe's link to the ingredient carries in its notes.

SELECT i.category AS aisle,
       i.name AS ingredient,
       group_concat(coalesce(nullif(m.notes, ''), 'some') || ' for ' || r.name, '; ') AS amounts
FROM recipes r
JOIN maps m ON m.tbl1 = 'recipes' AND m.prime1 = r.prime AND m.kind = 'uses' AND m.tbl2 = 'ingredients'
JOIN ingredients i ON i.prime = m.prime2
WHERE r.marked = 1
GROUP BY i.ingredient_id
ORDER BY i.category, i.name;
