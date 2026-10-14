-- Audio-Lessons.sql -- the lessons made to be heard, newest first.
--
-- Podcast episodes need no picture, so they suit listening anywhere. Play Stream
-- (Alt+Shift+P) plays each one in the Homer Player.

SELECT title, category, source, year
FROM lessons
WHERE medium = 'audio'
ORDER BY year DESC, title;
