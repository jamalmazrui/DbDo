@echo off
rem DbDo.cmd -- run the program built in exec\, from the top of the project.
rem
rem Typing DbDo in C:\DbDo used to start DbDo.exe beside the sources. The build
rem now makes it in exec\, where the installer takes it from, so this short
rem wrapper keeps "DbDo" working as the quick test after a build. It passes its
rem arguments through, such as a database to open.
"%~dp0exec\DbDo.exe" %*
