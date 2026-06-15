% Using DbDo with the NFB 2026 Convention Database
% Short, practical walk-throughs

These walk-throughs use the bundled `NFB2026Convention.db` to show how a
screen-reader user can plan a convention with DbDo as a fast, fully
keyboard- and speech-driven database. Each scenario lists the commands you
type at the dot prompt. Everything here works with features DbDo already
has -- no add-ins required, and in particular **no HTML Agility Pack**:
that library reads and rewrites existing HTML, whereas DbDo only needs to
*write* HTML, which the Export command already does.

The database has four tables. `events` holds every agenda item (date,
start and end time, title, details). `contacts` holds people and
organizations. `locations` holds rooms and hotels. `maps` records the
relationships between them -- who presents or sponsors an event, where an
event is held, and so on.

# How you type arguments

DbDo follows a simple rule meant to spare your keyboard: whatever you type
after a command is taken **verbatim**. You almost never need quotation
marks, and you never need escape characters.

    find National Federation of the Blind
    jump Amateur Radio
    order start_time, title

All three pass their tail through exactly as written. Quotation marks are
*optional*: if you do wrap a value in a single matching pair of double or
single quotes, that one outer pair is removed and the inside is kept
literally -- so `find "John Smith"`, `find 'John Smith'`, and
`find John Smith` all search for the same thing. You would reach for quotes
only to protect leading or trailing spaces, or to keep a value that itself
begins and ends with a quote (wrap it once more: `""x""` yields `"x"`).
Nothing inside is ever un-escaped, so to search for a literal quote you
just type it: `find a "quote" here` looks for that text as written.

Two cautions. First, a bare value's leading and trailing spaces are
trimmed; wrap it in quotes to keep them. Second, this rule governs how
*DbDo* reads your line -- when the tail is handed to another engine with
its own grammar, that grammar still applies. The clearest case is
`filter`, whose tail goes to the database's filter engine: a text value
there is written in that engine's single quotes, `filter city = 'Austin'`,
which DbDo passes along untouched. In the rare case a value cannot be
expressed under these rules, the command refuses it and explains why,
rather than asking you to memorize an escape.

# Scenario 1: A presenter's events

You want every session a particular presenter is part of.

The quickest way is the bundled `Presenter-Events.sql`. Open the database,
run the script with Invoke Script, and you get a readable, copyable list of
that presenter's events in date and time order. To change who you're
looking up, edit the last name on the `WHERE` line of the script.

To do the same thing by browsing: open the `contacts` table, use Find to
land on the presenter, then use Say Related to hear the events linked to
them, or Enter Child to step into that list of events and read it row by
row.

# Scenario 2: Collect events of interest, then export them

This is the "build my personal schedule" workflow.

1. Switch to the `events` table.
2. Narrow to what you're considering. To see one day:
   `filter event_date = '2026-07-06'`. To find sessions on a topic:
   `filter title like '%Braille%'`.
3. Put them in time order: `order start_time, title`.
4. Read down the rows. When one interests you, mark it with the Set Mark
   command (or toggle it). Marks accumulate as you browse.
5. When you've gathered your picks, keep only them:
   `filter marked = true`.
6. Export a schedule you can keep: `export My-Picks.html`. The file opens
   in any browser and carries proper table headers for navigation.

For a nicer result, run the bundled `Marked-Schedule.js` instead of the
plain export in step 6. It writes one table per day under a date heading,
which reads more naturally than a single long table.

# Scenario 3: One day at a glance

When you just want the whole agenda for a single day, the bundled
`Daily-Schedule.dbdo` does the entire job: it filters to one date, sorts by
start time, counts the sessions, and exports the day to an HTML file. Open
the `events` table, edit the date inside the script if you like, and run it
with Invoke Script. Because a `.dbdo` script is only the dot-prompt
commands you would otherwise type, you can read it to learn the commands,
then start typing them yourself.

# Scenario 4: Where is it, and what's nearby

To find sessions in a particular place, open `locations`, find the room,
and use Say Related to hear the events held there. Going the other way,
from an event you can ask for its related location to learn where to go.

# Scenario 5: Popularity and counts

Who presents the most? Which room is busiest? Which day is fullest? These
are counting questions. When the count crosses tables -- presenters to
events, events to locations -- it needs a `GROUP BY`, so the bundled
`Convention-Stats.sql` answers them: run it with Invoke Script and you get
speakers ranked by number of presentations, locations ranked by events
held, and days ranked by event count, each from most to least. (For the
record, the agenda's busiest day is the opening Friday, and its top speaker
gives five sessions.)

When the question stays inside one table, you do not need SQL. From the
events table, to learn how full a single day is:

    filter event_date = '2026-07-03'
    yield
    reset-filter

`yield` reports how many rows are in the current view, so filtering then
yielding answers "how many of these." The statistics commands work the
same way on a chosen field: `longest title` finds the longest session
title in the current view, and `max`, `min`, `average`, and `median` report
on a field you name. Combine them with `filter` and `order` to ask sharper
questions -- order the events by start time within a day, or filter to a
topic before counting.

# The three script types, and when to reach for each

DbDo runs three kinds of script from the Scripts folder, all through
Invoke Script:

- **`.dbdo`** -- a list of dot-prompt commands, run top to bottom. Best
  when you want to automate the same sequence you'd type by hand, like the
  daily-schedule routine. Easy to read and edit; no programming.
- **`.sql`** -- a SQL query or batch. Best for questions that cross tables,
  like "which events does this presenter give," where the answer needs a
  join the dot prompt can't express on its own.
- **`.js`** -- a JScript program with full access to the open database and
  the program. Best when you want custom output or logic, like writing a
  grouped, formatted schedule file the built-in export doesn't produce.

A good rule of thumb: reach for `.dbdo` to repeat what you can already do,
`.sql` to ask cross-table questions, and `.js` when you need to build
something the other two can't.
