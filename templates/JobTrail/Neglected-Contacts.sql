-- Neglected-Contacts.sql -- people you have not spoken to in a while.
--
-- Networking decays quietly. This lists everybody in contacts with no action in
-- the last ninety days, most-forgotten first.

SELECT c.last_name, c.first_name, c.enterprise, c.role,
       coalesce(max(a.action_date), 'never') AS last_contact
FROM contacts c
LEFT JOIN actions a ON a.contact_id = c.contact_id
GROUP BY c.contact_id
HAVING last_contact = 'never' OR last_contact < date('now','-90 days')
ORDER BY last_contact;
