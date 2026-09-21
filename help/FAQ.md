# DbDo -- Frequently Asked Questions

## What is DbDo for?

Keeping records you want to find, sort and share -- job leads, contacts, a book
list, a club roster -- in a database you can use entirely by keyboard and
screen reader.

## Which screen readers does it work with?

JAWS, NVDA and Narrator. The installer can add scripts for JAWS and an add-on
for NVDA; a box for each appears only if that reader is installed.

## What kinds of files does it open?

SQLite databases, which is what it creates, plus Access, Excel, dBASE and
delimited text files.

## Do I need Microsoft Office?

No. Spreadsheets and Word documents are written without Office.

## What is JobTrail?

A job search database that comes with DbDo and opens the first time you run it.
It keeps jobs, contacts, documents, interview stories and a log of every step
you take. The spoken tutorials use it throughout.

## How do I learn it?

Help, Play Tutorials. Fifteen walkthroughs, about three minutes each, meant to
be heard in order.

## I forgot a key. How do I find it?

Control+F1 is the Key Describer: press it, then any key, and DbDo says what that
key does instead of doing it. Alt+F10 lists every command in one window you can
filter by typing. Control+Shift+F1 opens the full list of keys.

## Why is the key for Order Alt+O, not Control+O?

Control+O opens a file in every Windows program, so DbDo keeps it for that. Order
takes Alt, and O is still the first letter of its name. The same reasoning puts
Select Columns on Alt+S, since Control+S saves.

## What does Z mean in a key?

Sleep. Every Z key is a toggle that wakes a behavior or puts it to sleep, such
as Alt+Z for Toggle Read Only.

## Can DbDo use AI?

Yes, if Ollama is installed. F12, Chat with AI, asks a question; Shift+F12, Chat
about Table, sends the table you are on along with it. Everything runs on your
own computer.

## A field sounds empty. Is it empty or missing?

Shift+C says "null" when a field has no value at all and "blank" when it holds
empty text. DbDo saves an empty box as null, so the two are not mixed.

## How do I update DbDo?

F11, Elevate Version -- elevate sounds like eleven. It checks for a newer
release and offers to install it.

## Where are my files?

Your databases and settings live under `%LOCALAPPDATA%\DbDo`. Help, Show Log
Location opens the folder with the log files.

## How do I report a problem?

Help, File GitHub Issue, or email the log from Help, Email Log File.
