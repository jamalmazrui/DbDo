-- Quick-Lessons.sql -- presentations you can play in fifteen minutes or less.
--
-- Only the ones Play Stream (Alt+Shift+P) plays -- audio and video, not web
-- pages -- shortest first, so a few minutes free is enough to learn something.

SELECT title, minutes, medium, category, source
FROM presentations
WHERE minutes IS NOT NULL AND minutes <= 15 AND medium IN ('audio', 'video')
ORDER BY minutes, title;
