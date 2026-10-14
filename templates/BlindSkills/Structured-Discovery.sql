-- Structured-Discovery.sql -- lessons about structured discovery, or made where it is taught.
--
-- Structured discovery is the way the National Federation of the Blind's training
-- centers teach: questions that lead the student to gather information
-- nonvisually and solve the problem, rather than step-by-step answers. The
-- structured_discovery field says Subject when the lesson is about the method,
-- and Setting when a center or agency that teaches this way made it.

SELECT structured_discovery, title, category, source, year
FROM lessons
WHERE structured_discovery IS NOT NULL
ORDER BY structured_discovery DESC, title;
