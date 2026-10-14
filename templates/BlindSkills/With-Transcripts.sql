-- With-Transcripts.sql -- lessons with a transcript to read.
--
-- A transcript can be read in braille or by speech, at your own pace, and
-- searched. Control+Enter on transcript_url opens it.

SELECT title, skill, source, transcript_url
FROM lessons
WHERE transcript_url IS NOT NULL
ORDER BY title;
