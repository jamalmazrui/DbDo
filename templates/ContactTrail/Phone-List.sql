-- Phone-List.sql -- everyone with a phone number, by last name.
--
-- The short list to read through when you need to call somebody: the name, then
-- each number they have, mobile first. People with no number are left out.

SELECT last_name, first_name,
       coalesce(nullif(wireless_phone, ''), '') AS mobile,
       coalesce(nullif(home_phone, ''), '') AS home,
       coalesce(nullif(enterprise, ''), '') AS company
FROM contacts
WHERE coalesce(wireless_phone, '') <> '' OR coalesce(home_phone, '') <> ''
ORDER BY last_name, first_name;
