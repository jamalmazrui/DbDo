@echo off
rem makeDescribedFilms.cmd -- rebuilds templates\DescribedFilms\DescribedFilms.db from ADP's title directory; arguments go to the script.
python "%~dp0makeDescribedFilms.py" %*
exit /b %errorlevel%
