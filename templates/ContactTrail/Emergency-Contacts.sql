-- Emergency-Contacts.sql -- the people to call in an emergency, with every number.
--
-- Everyone linked to the Emergency group, with all their phone numbers and an
-- email address, so the list is ready before it is needed. Put a person in the
-- group with Link Records, or by adding an in_group link in the maps table.

SELECT c.last_name, c.first_name,
       coalesce(c.wireless_phone, '') AS mobile,
       coalesce(c.home_phone, '') AS home,
       coalesce(c.office_phone, '') AS office,
       coalesce(nullif(c.personal_email, ''), c.business_email, '') AS email
FROM contacts c
JOIN maps m ON m.tbl1 = 'contacts' AND m.prime1 = c.prime AND m.kind = 'in_group' AND m.tbl2 = 'groups'
WHERE m.prime2 = 'Emergency'
ORDER BY c.last_name, c.first_name;
