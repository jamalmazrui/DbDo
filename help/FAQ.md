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

A job search database that comes with DbDo. It keeps jobs, contacts, documents,
interview stories and a log of every step you take. Control+Shift+O, Open
Template Database, opens it. The first time you run DbDo, it opens RadioTrail,
a database of Internet radio stations, instead.

## How do I learn it?

Alt+F12, Play Tutorials, on the Help menu. Ten spoken lessons, three to five
minutes each, meant to be heard in order.

## I forgot a key. How do I find it?

Control+F1 is the Key Describer: press it, then any key, and DbDo says what that
key does instead of doing it. Alt+F10, the Alternate Menu, lists every command in one window; Control+F there
keeps only the commands whose names hold the words you type. Control+Shift+F1 opens the full list of keys.

## Why is the key for Order Alt+O, not Control+O?

Control+O opens a file in every Windows program, so DbDo keeps it for that. Order
takes Alt, and O is still the first letter of its name. The same reasoning puts
Show Columns on Alt+S, since Control+S saves.

## What does Z mean in a key?

Sleep. Every Z key is a toggle that wakes a behavior or puts it to sleep, such
as Control+Shift+Z for Read Only Toggle.

## Why do I see only two or three columns?

A table first shows the columns that name a record, such as a book's title and
author, so Down Arrow says just enough. Alt+S, Show Columns, then A for All
columns, shows every column, and DbDo keeps that choice for the table. Shift+W,
Say Whole Record, reads the whole record whatever is shown.

## How do I find the radio stations of a city?

In RadioTrail, Control+F, Filter Records, then Any field, and type the city:
seattle finds stations with Seattle in their name, city or state. For a whole
state, choose Main fields and type Washington in the state box. A station
DbDo does not have can be added with Control+N, New Record.

## How do I bring a spreadsheet into a database I already have?

Open the database and the table, then Control+Shift+I, Import Data, and choose
the workbook. Merge records adds its rows to the table, matching columns by
name, so a column called Title fills the title field; DbDo says how many rows
came in and how many were left out, and why. New table keeps it as a table of
its own. To make a new database from a spreadsheet, open the spreadsheet with
Control+O and use Save As, Control+Shift+S, with a .db name; Alt+R, Recent
Databases, opens it again.

## Can DbDo search long text, such as descriptions?

Yes. A table with a full-text index, such as the books of TalkingBookTopics,
offers Full-text match in Filter Records (Control+F). It finds whole words in
every field at once: a phrase, either word, a word left out, a word's start. The
template's saved searches, Alt+V, put the best matches first. Walk 7 of the
tutorials shows how.

## How do I use a database someone shared with me?

Get Database, on the File menu (Alt+F, then G), copies it into your own DbDo
folder from its path or web address, and opens it. A zip holding a database
works too.

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
