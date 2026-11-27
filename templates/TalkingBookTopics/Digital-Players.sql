-- Digital-Players.sql -- In Brief articles about the move from cassettes to digital talking-book players, best match first.
--
-- A search of the updates table: OR gives either word, and player* every word that begins with player.

SELECT u.title, u.issue, snippet(updates_fts, 1, '', '', ' ... ', 20) AS passage
FROM updates_fts JOIN updates u ON u.update_id = updates_fts.rowid
WHERE updates_fts MATCH '(digital OR cassette*) AND player*'
ORDER BY updates_fts.rank
LIMIT 300;
