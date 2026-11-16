@echo off
rem Times DbDo's SQLite commands on a copy of RadioTrail through DbDo's own connection, and records which index each
rem query uses (SQLite plan step 1). The copy goes in work\measureSqlite; the log goes in logs, named
rem DbDo-measureSqlite with the date and time. Build DbDo first.
python "%~dp0measureSqlite.py" %*
exit /b %errorlevel%
