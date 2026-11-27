-- Apps-By-Platform.sql -- how many apps run on each platform or screen reader, the most first.
--
-- A count, to see at a glance where blind developers' work is.

SELECT platform, count(*) AS apps
FROM apps
WHERE platform <> ''
GROUP BY platform
ORDER BY apps DESC, platform;
