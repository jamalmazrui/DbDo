-- Skill-Index.sql -- every lesson by the task it teaches.
--
-- Titles often name a series or a person; the skill names the task, so lessons
-- that teach the same thing in different ways sit together here.

SELECT skill, title, medium, source
FROM lessons
ORDER BY skill, title;
