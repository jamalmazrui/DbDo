-- Group-Roster.sql -- every group with its members.
--
-- One line per group, naming its members by last name, and how many there are.
-- A group with nobody in it yet still appears, so none is forgotten.

SELECT g.name AS "group",
       count(c.contact_id) AS members,
       coalesce(group_concat(c.first_name || ' ' || c.last_name, '; '), '') AS who
FROM groups g
LEFT JOIN maps m ON m.tbl2 = 'groups' AND m.prime2 = g.prime AND m.kind = 'in_group' AND m.tbl1 = 'contacts'
LEFT JOIN contacts c ON c.prime = m.prime1
GROUP BY g.group_id
ORDER BY g.name;
