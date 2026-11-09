# DbDo -- Tutorials

**Version 1.0**  
September 2026  
Copyright 2026 by Jamal Mazrui  
MIT License

Nine short walkthroughs, each about three minutes, each a real job done from
start to finish. Work through the first one and the rest will make sense; after
that, take whichever matches what you need today. Every one uses JobTrail, the
job search database that comes with DbDo, so you can follow along without
having anything of your own to lose.

**Simulations, made with AI.** One synthetic voice works through a task; a
second answers as a screen reader would. The voices are piper's kristin and
john, trained on public domain recordings; DbDo.md credits them. You always know which is talking without being told.

The screen reader answers are written for the middle setting every reader has:
**JAWS at intermediate verbosity**, or **Narrator at its default level 3**. You
hear the name, the kind of control, its value and its state -- and, in a dialog,
the Alt key that jumps straight to it, because that letter is rarely the one you
would guess. You do not hear the beginner's help after each control, since
anybody who knows what a check box is knows that Space toggles it.

Where a tutorial says to read the current line, use your own say line key --
JAWS or NVDA with Up Arrow -- rather than anything of DbDo's. It is the habit
you already have, and in this grid a line is a record.

Every key is named in full the first time it appears. If you forget one, press
Control+F1 for the key describer and press the key: it says the command's name
and what it does instead of running it.

## How to listen

Help, Play Tutorials opens `Tutorials.mkv` in whatever program plays video and
audio files on your computer. If none is set, Windows asks which app to use;
pick a media player such as VLC and choose Always, and it will not ask again.

**`Tutorials.mkv`** holds all nine as one recording with a chapter at the start
of each. In FileDir, put the cursor on it and press Control+Shift+H for the
Homer Player: it opens as one track, and **Control+Page Down and Control+Page Up
move between tutorials**, announcing "Chapter 3 of 9" as they go.
Control+Shift+Page Down and Control+Shift+Page Up jump to the last and first.

**If you have the nine .mp3 files**, open `Tutorials.m3u` in the Homer Player
instead. A playlist opens as nine separate tracks, each named for its tutorial,
and moving between tracks is then an arrow key in a list. The .mp3 files are not
in the repository -- they are rebuilt by `scripts\buildTutorials`.

## Contents

- [0 - Overview](#0-overview)
- [1 - User Interface](#1-user-interface)
- [2 - Install and Tune In](#2-install-and-tune-in)
- [3 - Find a Station](#3-find-a-station)
- [4 - Track Job Leads](#4-track-job-leads)
- [5 - Catalog Your Books](#5-catalog-your-books)
- [6 - Explore Music](#6-explore-music)
- [7 - More Templates](#7-more-templates)
- [8 - Analyze a Dataset](#8-analyze-a-dataset)
- [9 - Conclusion](#9-conclusion)
- [1. Where to Go Next](#1-where-to-go-next)

<!-- walkthrough: written by makeTutorial.py, do not edit between the markers -->

## 00 - Overview and Table of Contents

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

### Step 1

I am Kristin, the user.

Screen reader:

- I am John, the screen reader.

Both voices are synthetic, made with piper from public domain recordings. They introduce themselves once, here.

### Step 2: Alt+Control+D

I am on the trail of an accessibility analyst job. I keep every lead, contact, and step in JobTrail, a job search database built on DbDo.

Screen reader:

- DbDo
- Records list view

DbDo opens databases of any kind. JobTrail is the one that comes with it.

### Step 3: Shift+Z

These walkthroughs follow me in order. One installs DbDo, two opens JobTrail, three learns the menus. Four to six add, inspect and edit a job.

Screen reader:

- status, jobs row 1 of 4, sort employer

### Step 4

Then finding, ordering and filtering, choosing what I hear, following links, marking, the two computed columns, getting things out, and last, the work search record my counselor asks for.

Each one assumes the ones before it, so later ones say less.

**Something to try:** Take the next one: installing.

## 01 - Installing DbDo

**Before you start:** The installer is downloaded, and a Windows screen reader is running.

### Step 1: Enter

I run the installer. Windows asks first, because it came from the internet.

Screen reader:

- Open File - Security Warning dialog
- The publisher could not be verified. Are you sure you want to run this software?
- Run Button, alt+R

DbDo is not code signed, so this appears for every download.

### Step 2: Alt+R

Alt plus R, Run. Then Windows asks for administrator rights, because DbDo installs for everyone.

Screen reader:

- User Account Control

The prompt can open behind other windows. If nothing happens, Alt plus Tab finds it.

### Step 3: Alt+Y

Alt plus Y, Yes. The first page is the folder. Enter presses Next, the page's default button.

Screen reader:

- Setup - DbDo dialog
- To continue, click Next. If you would like to select a different folder, click Browse.
- Edit, C colon backslash Program Files backslash DbDo

A later update skips this page and goes where the last one went.

### Step 4: Enter

Screen reader:

- Click Install to continue with the installation, or click Back if you want to review or change any settings.
- Install Button, Alt+i

### Step 5: Enter

Enter again presses Install. The last page lists the extras. I arrow through them; Space ticks one.

Screen reader:

- Setup has finished installing DbDo on your computer.
- Tree view, Install scripts for improving use with the JAWS screen reader, checked, 1 of 6

A screen reader line appears only for a reader you actually have.

### Step 6: DownArrow

Screen reader:

- Install add-on for improving use with the NVDA screen reader, checked, 2 of 6

### Step 7: DownArrow

Ollama runs AI on my own computer. I want it, so I tick it.

Screen reader:

- Install Ollama 0.34.2, not checked, 3 of 6

### Step 8: Space

Screen reader:

- checked

The model comes next, about 2 gigabytes, and installs after Ollama.

### Step 9: Enter

Launch is ticked already. Enter presses Finish, and a results box says what was done.

Screen reader:

- DbDo Setup Results dialog
- DbDo 1.0.172 is installed. Program files, C colon backslash Program Files backslash DbDo
- OK Button

### Step 10: Enter

Screen reader:

- DbDo
- JobTrail dot d b, jobs
- Records list view
- Demo Data Cooperative (made up) Data Quality Specialist rejected, 1 of 4
- DbDo ready

Alt plus Control plus D starts DbDo from anywhere afterwards. D for DbDo.

**Something to try:** Open the log the results box names, and find where DbDo was installed.

## 02 - Opening JobTrail

**Before you start:** DbDo has just opened JobTrail on the jobs table.

### Step 1: DownArrow

Each row is one job I am after: employer, title, and where it stands.

Screen reader:

- Example Widgets Company (sample employer) Accessibility Analyst interviewing, 2 of 4

That one is the job I want most.

### Step 2: RightArrow

It is a grid. Down and Up move between records; Left and Right between fields.

Screen reader:

- Accessibility Analyst

### Step 3: Shift+C

Shift plus C, Say Cell -- C for Cell. The field I am on, and its value.

Screen reader:

- title, Accessibility Analyst

Every Shift and letter asks a question, and none changes anything.

### Step 4: DownArrow

Down keeps the field, so I can compare one field down the list.

Screen reader:

- Digital Services Assistant

### Step 5: Shift+Z

My screen reader's say line key reads the whole row again. Shift plus Z, Say Status, gives the summary. H for Here.

Screen reader:

- status, jobs row 3 of 4, sort employer

### Step 6: Control+PageDown

Control plus Page Down moves to the next table. JobTrail gives me five: actions, contacts, docs, jobs and stories.

Screen reader:

- JobTrail dot d b, stories
- Records list view

Two more tables hold DbDo's own pick lists and links. It keeps those out of the way.

**Something to try:** Arrow through the four jobs, then Left and Right through one of them, checking each cell with Shift plus C.

## 03 - Menus, Windows and Help

**Before you start:** JobTrail is open on the jobs table.

### Step 1: Alt

Alt opens the menu bar. Each menu's first letter opens it directly.

Screen reader:

- Menu bar
- File, F

File, Edit, Navigate, Query, Misc, Window, Help. Every letter differs.

### Step 2: DownArrow

Down Arrow opens File. Each item gives its key, then its letter.

Screen reader:

- New Database..., N

### Step 3: DownArrow

Screen reader:

- Add Table..., A

### Step 4: DownArrow

Screen reader:

- Open Database..., Control+O, O

The key works from anywhere. The letter works while the menu is open. They match whenever the key has a letter.

### Step 5: Escape

A few menus hold a submenu for a large or rarely used group. Edit, B for Bulk Marking, is one. Right Arrow goes in; Left Arrow comes back.

Screen reader:

- Leaving menus

The others: Query, S for Say, Misc, T for Tools, and Help, M for More Documents. None goes deeper than one level.

### Step 6: Control+Tab

DbDo can keep several tables open, each in its own window -- the multiple document interface. Control plus Tab moves among DbDo windows.

Screen reader:

- JobTrail dot d b, actions
- Records list view

Control plus Shift plus T opens a table in a new window. Control plus F4 closes one.

### Step 7: Control+F1

F1 is help: the guide. Shift plus F1 is the history, Control plus F1 describes the next key I press.

Screen reader:

- Key Describer On

Help also holds the ReadMe, Hotkeys, the FAQ, and Play Tutorials.

### Step 8: F11

F11 is Elevate Version -- elevate sounds like eleven. It checks for a newer DbDo.

Screen reader:

- Elevate Version dialog

Nothing downloads without asking.

**Something to try:** Open Help and find the document you would read second.

## 04 - Adding a New Record

**Before you start:** JobTrail is open on the jobs table. This morning I found a support analyst opening at Widget Works.

### Step 1: Control+N

Control plus N, New record -- N for New.

Screen reader:

- New Record dialog
- Employer edit

One box per field, in the order I would fill them in.

### Step 2: Tab

The employer, then Tab to the title.

Screen reader:

- Title edit

### Step 3: Tab

The title, then Tab to status.

Screen reader:

- Status edit
- F4 picks from 7 values

### Step 4: F4

F4 opens the pick list. F4 picks, all through Homer programs.

Screen reader:

- Status list box, applied, 1 of 7

### Step 5: l

Screen reader:

- lead, 4 of 7

### Step 6: Control+Enter

Enter takes it. Control plus Enter saves the record from anywhere in the dialog.

Screen reader:

- Records list view
- Widget Works (your own entry) Support Analyst lead, 5 of 5

**Something to try:** Add a job you are actually after, with its status.

## 05 - Inspecting a Record

**Before you start:** JobTrail is open on the jobs table, on Example Widgets.

### Step 1: Control+I

A row speaks three fields. Control plus I, Inspect Record, reads them all.

Screen reader:

- Inspect Record dialog
- employer: Example Widgets Company (sample employer)

I for Inspect. A read-only window; Escape closes it.

### Step 2: Escape

For one fact, I ask instead. Shift plus N, Say Notes.

Screen reader:

- Records list view

### Step 3: Shift+N

Screen reader:

- notes, Illustrative record. Interview booked for the 24th; ask about screen reader testing tools.

Shift plus T, Say Tags, and Shift plus U, Say url, work the same way.

### Step 4: Shift+E

Shift plus E, Say Edited: when I last changed it.

Screen reader:

- edited, September 19, 2026 at 10:01 PM

Shift plus A, Say Added, says when it arrived.

### Step 5: Alt+Shift+N

Alt plus Shift plus N edits the notes in their own window, for a long one.

Screen reader:

- Notes dialog
- Notes edit, multiline, Illustrative record. Interview booked for the 24th.

Control plus Enter saves; Escape leaves.

**Something to try:** Walk the Shift keys on one record and notice which three you want while arrowing.

## 06 - Editing a Record

**Before you start:** JobTrail is open on the jobs table. The interview at Example Widgets went well, and they made an offer.

### Step 1: Enter

Enter opens the record in the same dialog I used to add one.

Screen reader:

- Edit Record dialog
- Employer edit, Example Widgets Company (sample employer)

### Step 2: o

Tab to status, F4 for the list, O for offer.

Screen reader:

- offer, 5 of 7

### Step 3: Control+Enter

Screen reader:

- Records list view
- Example Widgets Company (sample employer) Accessibility Analyst offer, 2 of 4

### Step 4: F2

For one field, F2 edits the cell in place -- the same F2 that renames a file in Windows.

Screen reader:

- Cell edit, offer

Enter saves the cell and keeps my place.

### Step 5: Escape

Shift plus C checks it.

Screen reader:

- Records list view

### Step 6: Shift+C

Screen reader:

- status
- row 2 of 4
- offer

**Something to try:** Change the status of one of your own jobs both ways, and check it with Shift plus C.

## 07 - Keywords and Jump

**Before you start:** JobTrail is open on the jobs table.

### Step 1: exa

The quickest way is typing. The first letters of an employer go there.

Screen reader:

- Example Widgets Company (sample employer) Accessibility Analyst interviewing, 2 of 4

### Step 2: Control+J

Control plus J, Jump to Record -- J for Jump. It searches the field I am on.

Screen reader:

- Jump to Match (column, employer) dialog
- Text combo box, blank, ALT+T

### Step 3: Enter

Screen reader:

- Illustration City Library (fictional) Digital Services Assistant lead, 3 of 4

### Step 4: Control+K

Control plus K, Keywords -- K for Keywords. It searches every field, notes included, which is why it is not called Find.

Screen reader:

- Keywords dialog
- Text combo box, blank, ALT+T

### Step 5: Enter

Screen reader:

- Sample Health Network (illustration only) Records Coordinator applied, 4 of 4

F3 searches again. Shift reverses: Shift plus F3 searches back, Control plus Shift plus F finds backwards.

### Step 6: Shift+F

Shift plus K, Say Keywords, reminds me what I looked for.

Screen reader:

- find, screen reader

**Something to try:** Find a job by a word in its notes with Keywords, then jump back to it by employer.

## 08 - Order and Filter Records

**Before you start:** JobTrail is open on the jobs table.

### Step 1: Alt+O

Alt plus O, Order -- O for Order. Control plus O is Open everywhere, so Order takes Alt.

Screen reader:

- Order Records dialog
- Column to sort by list box, employer, 4 of 16, ALT+C

### Step 2: s

Screen reader:

- status, 13 of 16

### Step 3: Enter

Screen reader:

- Records list view
- Sample Health Network (illustration only) Records Coordinator applied, 1 of 4

Shift plus O, Say Order, says it later.

### Step 4: Control+F

Control plus F, Filter Records -- F for Filter. It decides which rows I hear, and it writes the condition for me: one box per field, and a symbol in front of a value to compare instead of match.

Screen reader:

- Filter Records dialog
- Filter combo box, blank, ALT+F

### Step 5: Enter

I want what is still moving: status not rejected.

Screen reader:

- Records list view
- Example Widgets Company (sample employer) Accessibility Analyst interviewing, 1 of 3

Shift plus F, Say Filter, and Shift plus Y, Say Yield -- how many rows it left.

### Step 6: Control+Shift+F

Control plus Shift plus F clears it. Adding Shift reverses.

Screen reader:

- Records list view
- Demo Data Cooperative (made up) Data Quality Specialist rejected, 1 of 4

**Something to try:** Filter to the jobs you applied to, order them by date applied, then clear the filter.

## 09 - Select the Columns You Hear

**Before you start:** JobTrail is open on the jobs table.

### Step 1: Shift+S

Shift plus S, Say Select, names the fields each row speaks.

Screen reader:

- select, employer, title, status

### Step 2: Alt+S

Alt plus S, Select Columns -- Control plus S saves, so Select takes Alt.

Screen reader:

- Select Columns dialog
- Columns check list box, employer check box checked, 1 of 16, ALT+C

### Step 3: Space

I add the date I applied. Space ticks it.

Screen reader:

- applied underscore date check box checked, 11 of 16

### Step 4: Enter

Screen reader:

- Records list view
- Example Widgets Company (sample employer) Accessibility Analyst interviewing 2026-09-02, 2 of 4

Three or four fields is the useful range. Every row is heard, so every field costs time.

**Something to try:** Choose three fields for the actions table that tell you what to do next.

## 10 - Related Records

**Before you start:** JobTrail is open on the jobs table, on Example Widgets.

### Step 1: Shift+R

Shift plus R, Say Related: every record linked to this job.

Screen reader:

- related, actions (1 record)
- 2026-09-08 bar First interview for Accessibility Analyst bar interview scheduled

### Step 2: Alt+RightArrow

Alt plus Right Arrow goes in, the way it goes forward in a browser.

Screen reader:

- JobTrail dot d b, actions
- 2026-09-08 First interview for Accessibility Analyst interview scheduled, 1 of 1

### Step 3: Backspace

Backspace comes back, as it goes up a level everywhere.

Screen reader:

- JobTrail dot d b, jobs
- Example Widgets Company (sample employer) Accessibility Analyst interviewing, 2 of 4

Alt plus Left Arrow does the same.

**Something to try:** From a contact, reach the job they belong to, and come back.

## 11 - Mark and Unmark

**Before you start:** JobTrail is open on the jobs table. I want to follow up on two of them this week.

### Step 1: Control+M

Control plus M, Mark -- M for Mark.

Screen reader:

- Marked row 2

### Step 2: Control+Shift+M

Control plus Shift plus M unmarks. Adding Shift reverses.

Screen reader:

- Unmarked row 2

Shift plus M, Say Mark, asks without changing anything.

### Step 3: Control+DownArrow

Control plus Down Arrow steps to the next marked record.

Screen reader:

- Sample Health Network (illustration only) Records Coordinator applied, 4 of 4

### Step 4: Shift+Space

Shift plus Space counts them.

Screen reader:

- 2 marked rows: Example Widgets Company (sample employer), Sample Health Network (illustration only)

### Step 5: Control+A

For runs of rows: Edit, B for Bulk Marking, then A for Mark All.

Screen reader:

- Marked 4 rows

Control plus A is the key for Mark All; Control plus Shift plus A unmarks all.

**Something to try:** Mark the jobs you will follow up on, then step through them.

## 12 - Look and Prime

**Before you start:** JobTrail is open on the jobs table, on Example Widgets.

### Step 1: Shift+L

Every table has two columns nobody types into. Shift plus L, Say Look.

Screen reader:

- look, Example Widgets Company (sample employer) | Accessibility Analyst | interviewing

Look is the record at a glance. It is what Say Related shows for a linked record.

### Step 2: Shift+P

Shift plus P, Say Prime: the fields that make the record unique.

Screen reader:

- prime, Example Widgets Company (sample employer)|Accessibility Analyst

Employer and title. Two jobs with both the same would be one job.

### Step 3: Shift+I

Shift plus I, Say ID, gives the row number, which tells a person almost nothing.

Screen reader:

- id, 2

Look is for people; prime is for matching.

**Something to try:** Say the look and prime of three records and notice which tells you more.

## 13 - Report, Save and Copy

**Before you start:** JobTrail is open on the jobs table, on Example Widgets.

### Step 1: Control+Shift+C

To tell a friend about a job: Control plus Shift plus C copies the record. C for Copy; Control plus C alone copies the cell.

Screen reader:

- Row copied to clipboard

Labelled lines, ready to paste.

### Step 2: Alt+Shift+R

For a prepared document: File, R for Run Report. Its key is Alt plus Shift plus R.

Screen reader:

- Run Report dialog
- Output list box, application underscore history, 1 of 6, ALT+O

### Step 3: Enter

Screen reader:

- EdSharp, application underscore history dot m d

### Step 4: Control+Shift+S

For the whole table as a file: Control plus Shift plus S, Save As.

Screen reader:

- Save As dialog
- File name edit, ALT+N

The extension decides the format: dot x l s x, dot c s v, dot h t m.

**Something to try:** Copy one record into an email to yourself.

## 14 - Work Search Record for a Claim or Counselor

**Before you start:** JobTrail is open on the actions table, the log of everything I have done.

### Step 1: Shift+Z

My counselor asks what I did, when, with whom, and what came of it. The actions table holds exactly that.

Screen reader:

- status, actions row 1 of 7, sort action date descending

### Step 2: Shift+C

Three fields matter to the agency: method, evidence, and counts -- whether it was an employer contact.

Screen reader:

- method
- row 1 of 7
- internet

### Step 3: Alt+Shift+R

File, R for Run Report.

Screen reader:

- Run Report dialog
- Output list box, application underscore history, 1 of 6, ALT+O

### Step 4: w

Screen reader:

- work underscore search underscore record, 6 of 6

### Step 5: Enter

Screen reader:

- EdSharp, work underscore search underscore record dot m d

Countable contacts first, with weekly totals; everything else after.

**Something to try:** Produce the record for the last four weeks and read it before sending.

<!-- walkthrough: written by makeTutorials.py, do not edit between the markers -->

## 0 - Overview

What DbDo is, the ready-made databases that come with it, three keys of the screen reader's own, and the table of contents.

**Before you start:** Nothing is needed; this walk is listened to.

### Step 1

DbDo is a database program for people who listen to the screen.

### Step 2

It shows one table at a time as a list of records you move through by row and by column. Every cell can be heard, edited, searched, marked and counted from the keyboard, and the program answers questions about where you are without changing anything.

### Step 3

Think of a table as a stack of index cards. Every card is one item, a radio station, say, and every card asks the same questions: its name, its country, its stream. A record is one card, and a column is one question asked of every card.

### Step 4

A database is not always the right tool. To write about one thing, a document serves better. DbDo earns its keep when you keep many things of the same kind, each with the same questions.

### Step 5

It also comes with ready-made databases, called templates, each a complete example you can use as it is or make your own. These walks are built around them.

### Step 6

Radio comes first: RadioTrail, internet radio stations from around the world, each one a key press from playing.

Screen reader:

- 49186 stations from 219 countries.

### Step 7

Then JobTrail, for a job search: openings, the people at each employer, what you sent and what happened.

Screen reader:

- Jobs, contacts, actions, documents and stories, linked to one another.

### Step 8

BookTrail, a catalog of what you read and want to read, in any form.

Screen reader:

- Audio from BARD, text from Bookshare, braille and eBooks, side by side.

### Step 9

And MusicTrail, the greatest albums of ten genres and the people who made them.

Screen reader:

- 100 albums and 270 performers, each album linked to who recorded it.

### Step 10

Four more get a shorter visit in walk seven: TechTraining, BlindCreations, ContactTrail and RecipeTrail.

### Step 11

Two voices take these walks. I am the host; the other voice plays the screen reader. It is a simulation, so your own reader's words may differ a little.

### Step 12: Insert+Up Arrow

You know your screen reader's own keys already, and three of them are worth remembering whenever you feel lost. Insert plus Up Arrow says the line you are on again.

Screen reader:

- ACB Media 1, The United States Of America, ACB Media, blindness, Row 1 of 49186

Confirm the wording with a live run: buildTutorials -live.

### Step 13: Insert+Tab

Insert plus Tab says what has the focus: its name, its role, and its value or state.

Screen reader:

- Records list view, ACB Media 1, 1 of 49186

Confirm the wording with a live run: buildTutorials -live.

### Step 14: Insert+T

Insert plus T says the title of the window.

Screen reader:

- DbDo, RadioTrail dot d b, stations

Confirm the wording with a live run: buildTutorials -live.

### Step 15

On a laptop keyboard, Caps Lock usually stands in for Insert, though in some laptop layouts the line command moves to a letter, such as Caps Lock plus L. And if speech goes by before you catch it, your reader can replay it: Insert plus X in some readers, a speech history in others.

### Step 16

DbDo has two keys of its own for the same moments. Shift plus F1 says what the thing you are on is for. Control plus slash says where you are, and pressed twice lists every key. Walk one shows them; for now, just remember they are there.

### Step 17

These walks do not read the guide to you, and they do not cover every command. Each gives you a practical route into one task, so that you feel ready to try the rest yourself. The guide, on F1, has every detail.

### Step 18

Now the table of contents. I say the number and the title; the reader says what the walk covers.

### Step 19

One, User Interface.

Screen reader:

- The window, the record list, the Say keys that ask without changing, the menus, the rules every key follows, and where help lives.

### Step 20

Two, Install and Tune In.

Screen reader:

- The installer, DbDo opening on RadioTrail, a station found by name and playing.

### Step 21

Three, Find a Station.

Screen reader:

- A station that may carry the Seahawks game: searched, filtered, narrowed, put in order and played.

### Step 22

Four, Track Job Leads.

Screen reader:

- JobTrail: a status brought up to date, a job added, notes kept, and the people linked to a job.

### Step 23

Five, Catalog Your Books.

Screen reader:

- BookTrail: what you are reading next, counts by format, and a reading log written as a document.

### Step 24

Six, Explore Music.

Screen reader:

- MusicTrail: from an album to the band that recorded it, to its members, and back.

### Step 25

Seven, More Templates.

Screen reader:

- TechTraining, BlindCreations, ContactTrail and RecipeTrail, and bringing your copy up to date when a template improves.

### Step 26

Eight, Analyze a Dataset: a scientist's measurements, asked in SQL and in plain words, grouped, related and handed on.

Screen reader:

- A dataset brought in, asked in SQL and in plain words, grouped, related and handed on.

### Step 27

Nine, Conclusion.

Screen reader:

- What to carry away, the words DbDo uses, and the help built in.

### Step 28

That is the map. Take walks one and two first; then the walk for the database you want most. It is your database, so make it your own as you go.

**Something to try:** Take walks one and two, then the walk for the database you want first.

## 1 - User Interface

How DbDo is laid out and how its keys are chosen, shown in RadioTrail: rows and columns, the Say keys, the menus, the rules behind every key, and the four places help lives.

**Before you start:** DbDo is open on RadioTrail.

### Step 1

DbDo is heard before it is seen. I name each part; the reader shows it.

### Step 2: Alt+Control+D

Alt plus Control plus D, D for DbDo, opens it or brings it forward.

Screen reader:

- DbDo
- DbDo ready

### Step 3: Down Arrow

The main view is a list of records. Down Arrow moves by row, and the reader speaks the record: here, a station's name, its country, and its genres.

Screen reader:

- ACB Media 10, The United States Of America, ACB Media, blindness, Row 2 of 49186

Confirm the wording with a live run: buildTutorials -live.

### Step 4: Right Arrow

Right Arrow moves by column within the record, saying the column's name and its value.

Screen reader:

- country, The United States Of America

Confirm the wording with a live run: buildTutorials -live.

### Step 5: Shift+Z

Now the Say keys. Shift and a letter asks, and changes nothing. Shift plus Z is Say Status.

Screen reader:

- stations, 49186 records, no filter, sorted by source, name

Confirm the wording with a live run: buildTutorials -live.

### Step 6: Shift+C

Table, count, filter and sort in one breath. Shift plus C, C for Cell, says the cell you are on.

Screen reader:

- country, The United States Of America

Confirm the wording with a live run: buildTutorials -live.

### Step 7: Shift+8

Shift plus Y, Say Yield, says how many records are in view; Shift plus 8 says the order and filter.

Screen reader:

- order: source, name; filter: none.

Confirm the wording with a live run: buildTutorials -live.

### Step 8: F10

F10 opens the menu bar.

Screen reader:

- Menu bar
- File menu

### Step 9: Down Arrow

Down Arrow to the first item.

Screen reader:

- New Database

### Step 10: Escape

The item, its key, then its letter: learn a key from the menu once, then skip the menu. Escape closes the menu.

Screen reader:

- File submenu

### Step 11: Escape

Escape again leaves the menu bar.

Screen reader:

- Leaving menus

### Step 12

The rules behind every key. A letter is the first letter of a word: Control plus O opens, Alt plus O orders. Control does; Shift asks.

### Step 13: F4

Adding Shift reverses: Control plus M marks, Control plus Shift plus M unmarks. Z is for sleep, so every Z key is a toggle. And each function key is a family: F2 edits, F3 searches again, F4 lists the windows open.

Screen reader:

- Current Windows dialog, RadioTrail, stations, 1 of 1

Confirm the wording with a live run: buildTutorials -live.

### Step 14: Escape

Escape closes it.

Screen reader:

- Records

### Step 15: Control+F1

Control plus F1 is the Key Describer: each key says what it does instead of doing it.

Screen reader:

- Key Describer On

### Step 16: Control+O

Now Control plus O only describes itself.

Screen reader:

- Open Database. Control+O.

### Step 17: Control+F1

Control plus F1 again, and it is off.

Screen reader:

- No Key Describer

### Step 18: Alt+F10

When a key will not come to mind, Alt plus F10, the Alternate Menu, lists every command in one list you type into, with its key beside it.

Screen reader:

- Alternate Menu
- Command, list box

### Step 19

I type the word play, and the list narrows to the commands with that word.

Screen reader:

- Play Stream, Alt plus Shift plus P

Confirm the wording with a live run: buildTutorials -live.

### Step 20: Escape

Escape closes the list without running anything. Help lives in four places, the same in every Homer program. I say which; the reader says the key. The guide.

Screen reader:

- F1

### Step 21

The changes, release by release.

Screen reader:

- Shift plus F1

### Step 22

The version.

Screen reader:

- Alt plus F1

### Step 23

Where you are, then every key, in a window you can search.

Screen reader:

- Control plus slash, pressed twice, Where Am I

### Step 24

What this walk taught. I name the command; the reader gives the key.

### Step 25

Say Status.

Screen reader:

- Shift plus Z

### Step 26

Pick a field's fixed value.

Screen reader:

- Control plus F2

### Step 27

Unmark, the reverse of Mark.

Screen reader:

- Control plus Shift plus M

### Step 28

Every command in one list.

Screen reader:

- Alt plus F10

**Something to try:** Press Shift plus Z in any table, and Alt plus F10 whenever a key will not come to mind.

## 2 - Install and Tune In

DbDo installed with what it works best with, opened from anywhere, and on its first run a station from RadioTrail found by name and playing.

**Before you start:** The DbDo setup program is downloaded.

### Step 1

One want: DbDo on this computer, and a station playing, in one sitting.

### Step 2: Enter

I open the downloaded setup program and allow it to make changes.

Screen reader:

- Setup - DbDo dialog
- Welcome to the DbDo Setup Wizard

Confirm the wording with a live run: buildTutorials -live.

### Step 3: Enter

The ordinary installer: Enter takes Next, and the defaults are right.

Screen reader:

- Select Destination Location

Confirm the wording with a live run: buildTutorials -live.

### Step 4: Tab

The last page offers what DbDo works best with, each as a box whose first word is your computer's state read for you: Install, Update or Reinstall.

Screen reader:

- Install mpv, plays a station for Play Stream, checked, 1 of 4

Confirm the wording with a live run: buildTutorials -live.

### Step 5

Scripts for the screen reader you use; the mpv player, which plays a station; and, for the AI commands, Ollama and a model, downloaded once. Install and Update are ticked; Reinstall is not, since what is current needs nothing.

### Step 6: Space

A planned misstep: clearing a box you need. Space clears it; Space again ticks it back.

Screen reader:

- not checked

### Step 7: Space

Space.

Screen reader:

- checked

### Step 8: Alt+Control+D

Enter installs what is ticked, and then the key that opens DbDo from anywhere.

Screen reader:

- DbDo
- DbDo ready

### Step 9: Shift+Z

The first time, DbDo opens RadioTrail: internet radio stations, one to a row. The ten streams of the American Council of the Blind and the NFB Radio Network come first, then every other station by name. Shift plus Z says so.

Screen reader:

- stations, 49186 records, no filter, sorted by source, name

Confirm the wording with a live run: buildTutorials -live.

### Step 10: Insert+Up Arrow

The first station is already one of them. Insert plus Up Arrow says the line again.

Screen reader:

- ACB Media 1, The United States Of America, ACB Media, blindness, Row 1 of 49186

Confirm the wording with a live run: buildTutorials -live.

### Step 11: Control+I

Control plus I, Inspect Record, shows the whole record, one field to a line.

Screen reader:

- Inspect Record dialog
- name: ACB Media 1

Confirm the wording with a live run: buildTutorials -live.

### Step 12: Alt+Shift+P

Its genre, country, language, and the address it streams from. Escape. Now the reason RadioTrail opens first: Alt plus Shift plus P, Play Stream, plays the station in the Homer Player.

Screen reader:

- Homer Player, ACB Media 1

Confirm the wording with a live run: buildTutorials -live.

### Step 13: Control+J

Escape closes the player. A station I already know is quickest found by its name. Control plus J, Jump to Record, asks which column and what to look for.

Screen reader:

- Jump to Record dialog

Confirm the wording with a live run: buildTutorials -live.

### Step 14: Enter

I leave the column as name, type K E X P, and press Enter.

Screen reader:

- KEXP, The United States Of America, afrobeats, alternative, alternative rock, ambient, dj mixes, dj remix, djs, electronic, folk, highlife, hip hop, indie, indie rock, jazz, live sessions, local music, mrcrosby, music, punk, rock, talk, various, Row 17872 of 49186

Confirm the wording with a live run: buildTutorials -live.

### Step 15

Control plus Shift plus J jumps backward to the match before, as Shift reverses everywhere in DbDo. Another database opens with Control plus O, and a template with Alt plus Shift plus D, D for Database.

### Step 16: Alt+F1

Two help keys belong to this walk. Alt plus F1, About, says which DbDo is installed.

Screen reader:

- About DbDo

Confirm the wording with a live run: buildTutorials -live.

### Step 17: F11

Escape, and F11 asks GitHub whether a newer version is out. If one is, Enter installs it.

Screen reader:

- Checking for a newer version

Confirm the wording with a live run: buildTutorials -live.

### Step 18

Installed, open and playing. If something was left out, run the setup again and tick its box; what is already current costs nothing.

### Step 19

What this walk taught. I name the command; the reader gives the key.

### Step 20

The key that opens DbDo.

Screen reader:

- Alt plus Control plus D

### Step 21

Find a record by one column.

Screen reader:

- Control plus J

### Step 22

Play the station.

Screen reader:

- Alt plus Shift plus P

**Something to try:** Find and play a station you already know, by its name.

## 3 - Find a Station

RadioTrail searched for words, filtered to the stations that mention the Seattle Seahawks, narrowed to Washington, put in order, and one played; then stations marked to keep.

**Before you start:** DbDo is open on RadioTrail.

### Step 1

One want: a station that may be carrying the Seattle Seahawks game, ready to play.

### Step 2: Alt+Control+D

Alt plus Control plus D, D for DbDo, opens it or brings it forward.

Screen reader:

- DbDo
- DbDo ready

### Step 3: Control+K

Control plus K, K for Keywords, searches every column for words: names, genres, notes, all of them.

Screen reader:

- Keywords dialog
- Keywords: edit

Confirm the wording with a live run: buildTutorials -live.

### Step 4: Enter

I type seahawks and press Enter. The cursor moves to the first station that mentions the team.

Screen reader:

- Radio Browser, 99.3 KDRM, Row 2514 of 49186

Confirm the wording with a live run: buildTutorials -live.

### Step 5: F3

F3 goes to the next match, Shift plus F3 to the one before. Keywords moves the cursor and hides nothing. To see only the matches, filter.

Screen reader:

- Radio Browser, CISL "Sportsnet 650" Richmond, BC

Confirm the wording with a live run: buildTutorials -live.

### Step 6: Control+F

A planned misstep: Control plus F is not Find here. In DbDo, F is for Filter. Control plus F shows only the records that match, beginning with a choice offered as buttons.

Screen reader:

- Filter Records dialog
- Any field button

Confirm the wording with a live run: buildTutorials -live.

### Step 7: Enter

Any field is the first button, so Enter presses it. It asks for the text to find, wherever it is.

Screen reader:

- Any Field Contains dialog
- Text to find in any field: edit

Confirm the wording with a live run: buildTutorials -live.

### Step 8: Enter

I type seahawks and press Enter.

Screen reader:

- 7 records match.

Confirm the wording with a live run: buildTutorials -live.

### Step 9: N

Seven: stations in Seattle and Tacoma, in British Columbia and in Idaho. Control plus F again; with a filter in force there are five buttons: Start over, Clear filter, Narrow filter, Expand filter and Invert filter. N for Narrow keeps seahawks and adds a condition.

Screen reader:

- Narrow Filter dialog
- Any field button

Confirm the wording with a live run: buildTutorials -live.

### Step 10: F

Narrow asks how to give the new condition, just as the first filter did. F for Field boxes opens a box for every field.

Screen reader:

- Filter Records dialog, name: edit

Confirm the wording with a live run: buildTutorials -live.

### Step 11: Enter

I move to the state box, type Washington, and press Enter.

Screen reader:

- 3 records match.

Confirm the wording with a live run: buildTutorials -live.

### Step 12: Shift+F

Shift plus F, Say Filter, says both parts, in the words each was asked in, each in its own parentheses.

Screen reader:

- where (any field contains seahawks) and (state contains Washington)

Confirm the wording with a live run: buildTutorials -live.

### Step 13: Alt+O

Alt plus O, O for Order, says the order in force, then offers the same buttons as Select Columns: Look columns, Prime columns, Main, Standard and All columns, Choose columns and Type columns. Prime columns, the default order, has focus.

Screen reader:

- Ordered now
- Order Records dialog
- Prime columns button

Confirm the wording with a live run: buildTutorials -live.

### Step 14: L

L for Look columns sorts by look's columns, name first, the way stations are said. Prime columns puts back the table's own order.

Screen reader:

- Ordered by name ASC

Confirm the wording with a live run: buildTutorials -live.

### Step 15: Down Arrow

Down Arrow to the second.

Screen reader:

- Radio Browser, KIRO 710 ESPN Seattle, Row 2 of 3

Confirm the wording with a live run: buildTutorials -live.

### Step 16: Alt+Shift+P

The sports station. Alt plus Shift plus P plays it.

Screen reader:

- Homer Player, KIRO 710 ESPN Seattle

Confirm the wording with a live run: buildTutorials -live.

### Step 17: Control+M

Escape. To keep stations, mark them. Control plus M, M for Mark, marks the station I am on, and Control plus Shift plus M unmarks: Shift reverses.

Screen reader:

- Marked

Confirm the wording with a live run: buildTutorials -live.

### Step 18: Control+A

Shift plus Down Arrow marks a station and moves to the next. For a run of records, F8 starts it where I am, and Shift plus F8 completes it where I stop. Control plus A marks every record in view.

Screen reader:

- Marked 3 records

Confirm the wording with a live run: buildTutorials -live.

### Step 19: Alt+Shift+P

Now the payoff. With records marked, Alt plus Shift plus P plays all of them, one after another, as a playlist.

Screen reader:

- Homer Player, 3 tracks, 99.3 KDRM

Confirm the wording with a live run: buildTutorials -live.

### Step 20: Control+Shift+F

Escape. Control plus Shift plus F clears the filter, and every station is back, the marks kept. Marks stay when DbDo closes, so a marked station is a favorite to come back to.

Screen reader:

- Filter cleared. 49186 records.

Confirm the wording with a live run: buildTutorials -live.

### Step 21

What this walk taught. I name the command; the reader gives the key.

### Step 22

Search every column.

Screen reader:

- Control plus K

### Step 23

Show only the matches.

Screen reader:

- Control plus F

### Step 24

Say the filter.

Screen reader:

- Shift plus F

### Step 25

Order by a column.

Screen reader:

- Alt plus O

### Step 26

Mark a record.

Screen reader:

- Control plus M

### Step 27

Play the marked stations.

Screen reader:

- Alt plus Shift plus P

**Something to try:** Filter RadioTrail for your own team or a favorite genre, then play what is left.

## 4 - Track Job Leads

JobTrail, a job search kept as linked tables: an interview's outcome recorded, a new opening added, a note kept, and from a job to the contact and documents linked to it.

**Before you start:** DbDo is open.

### Step 1

One want: a job search in one place, with nothing forgotten.

### Step 2: Alt+Control+D

Alt plus Control plus D, D for DbDo, opens it or brings it forward.

Screen reader:

- DbDo
- DbDo ready

### Step 3: Alt+Shift+D

JobTrail is a template for exactly that. Its records are samples, plainly named as samples, to show the shape before your own go in.

Screen reader:

- Template Databases dialog

Confirm the wording with a live run: buildTutorials -live.

### Step 4: Enter

I choose JobTrail and press Enter. It opens on actions, newest first, so the last thing done is the first thing heard.

Screen reader:

- 2026-09-24, Follow up on the Accessibility Analyst interview, awaiting reply, Row 1 of 7

Confirm the wording with a live run: buildTutorials -live.

### Step 5: Control+T

Control plus T, Choose Table, lists its tables: jobs, contacts, actions, documents and stories. I choose jobs.

Screen reader:

- Choose Table dialog, actions

Confirm the wording with a live run: buildTutorials -live.

### Step 6: Enter

Enter.

Screen reader:

- jobs, 4 records, no filter, sorted by employer, title

Confirm the wording with a live run: buildTutorials -live.

### Step 7: Right Arrow

Four openings, each at a different stage. I move to the status column of the Accessibility Analyst job.

Screen reader:

- status, interviewing

Confirm the wording with a live run: buildTutorials -live.

### Step 8: Control+F2

Status has fixed values, so Control plus F2, Pick Value, offers them and nothing is mistyped.

Screen reader:

- status: list box, interviewing, 3 of 7

Confirm the wording with a live run: buildTutorials -live.

### Step 9: Enter

Down Arrow twice to offer, and Enter. F2 would edit the cell as free text instead, as F2 renames a file in Windows.

Screen reader:

- status, offer

Confirm the wording with a live run: buildTutorials -live.

### Step 10: Control+N

Control plus N, N for New, adds a record, and focus goes to its first field.

Screen reader:

- New record

Confirm the wording with a live run: buildTutorials -live.

### Step 11: Shift+A

I fill employer, title and status with F2. Shift plus A, Say Added, says when it was added.

Screen reader:

- added: today at 3:40 PM

Confirm the wording with a live run: buildTutorials -live.

### Step 12: Control+D

A planned misstep: Control plus D deletes a record, D for Delete, and asks first. Escape answers no.

Screen reader:

- Delete this record? No button

Confirm the wording with a live run: buildTutorials -live.

### Step 13: Alt+Shift+N

Escape keeps it. Notes hold what does not fit a column. Alt plus Shift plus N, Edit Notes, opens them in a box of their own.

Screen reader:

- Notes: edit, multiline

Confirm the wording with a live run: buildTutorials -live.

### Step 14: Shift+N

I type a line about the opening, and Control plus Enter keeps it. Shift plus N says it back.

Screen reader:

- Heard about it from a former colleague; apply by Friday.

Confirm the wording with a live run: buildTutorials -live.

### Step 15

Tags work the same way: Alt plus Shift plus T to edit, Shift plus T to hear, single words such as remote, to filter by later. Alt plus Shift plus U edits the posting's web address.

### Step 16: Shift+R

Now the links. Up Arrow to the Accessibility Analyst job. Shift plus R, Say Related, says what it leads to, changing nothing.

Screen reader:

- contacts 1, docs 2

Confirm the wording with a live run: buildTutorials -live.

### Step 17: Alt+Right Arrow

Alt plus Right Arrow goes into the related records.

Screen reader:

- contacts: 1 related record

Confirm the wording with a live run: buildTutorials -live.

### Step 18: Down Arrow

The hiring manager, linked as the contact for this job.

Screen reader:

- Pat, Rivera, Example Widgets Company (sample employer)

Confirm the wording with a live run: buildTutorials -live.

### Step 19: Alt+Left Arrow

Alt plus Left Arrow comes back, as a browser goes back.

Screen reader:

- jobs, Example Widgets Company (sample employer), Accessibility Analyst

Confirm the wording with a live run: buildTutorials -live.

### Step 20: Control+Shift+I

JobTrail also keeps its own questions as scripts. Control plus Shift plus I, Invoke Script, lists them; Today shows the actions due, oldest first.

Screen reader:

- Invoke Script dialog, Today

Confirm the wording with a live run: buildTutorials -live.

### Step 21

Escape, for now. When the samples have shown the shape, delete them and add your own.

### Step 22

What this walk taught. I name the command; the reader gives the key.

### Step 23

Pick a fixed value.

Screen reader:

- Control plus F2

### Step 24

Add a record.

Screen reader:

- Control plus N

### Step 25

Say what a record leads to.

Screen reader:

- Shift plus R

### Step 26

Go into the related records.

Screen reader:

- Alt plus Right Arrow

**Something to try:** Replace one sample job with a real lead of your own.

## 5 - Catalog Your Books

BookTrail, a catalog of books in every form: the next book to read, a count of how books came, a reading log written as a document, and how bookFido fills it from your own libraries.

**Before you start:** DbDo is open.

### Step 1

One want: what to read next, and a record of what I have read, in every form books come in.

### Step 2: Alt+Control+D

Alt plus Control plus D, D for DbDo, opens it or brings it forward.

Screen reader:

- DbDo
- DbDo ready

### Step 3: Alt+Shift+D

BookTrail is the template for it.

Screen reader:

- Template Databases dialog

Confirm the wording with a live run: buildTutorials -live.

### Step 4: Enter

I choose BookTrail and press Enter. Each book is heard author first, then series, volume and title, the order it is sorted in, so a series reads in order.

Screen reader:

- Austen, Jane, Pride and Prejudice, library, Row 1 of 6

Confirm the wording with a live run: buildTutorials -live.

### Step 5: Down Arrow

Six sample books: audio from BARD, text from Bookshare, braille, an eBook, and a commercial audiobook. Down Arrow twice.

Screen reader:

- Doyle, Arthur Conan, Sherlock Holmes, 1, A Study in Scarlet, BARD (NLS), Row 3 of 6

Confirm the wording with a live run: buildTutorials -live.

### Step 6: Control+I

Control plus I, Inspect Record, shows every field that has a value, one to a line; empty ones are left out.

Screen reader:

- Inspect Record dialog
- title: A Study in Scarlet

Confirm the wording with a live run: buildTutorials -live.

### Step 7: Control+Shift+I

Status, format, series, and when it was read. Escape. What to read next is a saved script. Control plus Shift plus I, Invoke Script, lists the scripts beside this database.

Screen reader:

- Invoke Script dialog, Reading-By-Year, 1 of 2

Confirm the wording with a live run: buildTutorials -live.

### Step 8: Enter

Down Arrow to Reading List, and Enter. Books in progress come first, then what I want to read, each series in order.

Screen reader:

- Doyle, Arthur Conan, Sherlock Holmes, 3, The Adventures of Sherlock Holmes, Bookshare, Row 1 of 2

Confirm the wording with a live run: buildTutorials -live.

### Step 9: Shift+Z

The third Holmes book, in text from Bookshare, then Frankenstein in braille. Shift plus Z confirms the view the script made.

Screen reader:

- books, 2 records, filtered, sorted by status, series, volume, title

Confirm the wording with a live run: buildTutorials -live.

### Step 10: Alt+Shift+G

Control plus Shift plus F clears it. Now counts. I move to the format column, and Alt plus Shift plus G, Generate from Grid, offers every report and chart of the grid.

Screen reader:

- Generate from Grid

### Step 11: F

F for Frequency report.

Screen reader:

- Frequency Report

### Step 12: C

C for Current column counts each format.

Screen reader:

- Frequency: format
- audio (BARD), 2
- audio (commercial), 1
- braille, 1

Confirm the wording with a live run: buildTutorials -live.

### Step 13: Alt+Shift+R

The count of each value; for a number column, the sum, average, least and most too. Escape. Alt plus Shift plus R, Run Report, writes a saved report as a document.

Screen reader:

- Run Report dialog, reading log

Confirm the wording with a live run: buildTutorials -live.

### Step 14: Enter

Enter. The reading log, every book finished, newest first, opens to keep or share.

Screen reader:

- Reading Log, heading level 1

Confirm the wording with a live run: buildTutorials -live.

### Step 15

A planned misstep: expecting the report to follow a filter. A report reads the table its definition names, not the records in view. To share only some, filter them and use Export Data, Alt plus Shift plus X.

### Step 16: Alt+Shift+X

Alt plus Shift plus X writes the records in view as a spreadsheet, CSV, JSON or another format.

Screen reader:

- Export Data dialog

Confirm the wording with a live run: buildTutorials -live.

### Step 17

Escape, for now. Typing a whole library in is not the plan. bookFido, a companion Homer program, catalogs the books in your libraries into BookTrail, and once your own books are in, these six samples are removed.

### Step 18

Each book you add takes the same fields, so a reading log, a count by format, or the next in a series comes from the same few keys.

### Step 19

What this walk taught. I name the command; the reader gives the key.

### Step 20

Show a record whole.

Screen reader:

- Control plus I

### Step 21

Run a saved script.

Screen reader:

- Control plus Shift plus I

### Step 22

Report on the grid.

Screen reader:

- Alt plus Shift plus G

### Step 23

Run a report.

Screen reader:

- Alt plus Shift plus R

### Step 24

Export the records in view.

Screen reader:

- Alt plus Shift plus X

**Something to try:** Add a book you are reading now, with its format and status.

## 6 - Explore Music

MusicTrail, the greatest albums of ten genres: from an album to the band that recorded it, on to the band's members, and back; an album's article opened; and a genre heard in rank order.

**Before you start:** DbDo is open.

### Step 1

One want: from an album I know, to the people who made it.

### Step 2: Alt+Control+D

Alt plus Control plus D, D for DbDo, opens it or brings it forward.

Screen reader:

- DbDo
- DbDo ready

### Step 3: Alt+Shift+D

MusicTrail holds a hundred albums, ten for each of ten genres, and the performers who recorded them.

Screen reader:

- Template Databases dialog

Confirm the wording with a live run: buildTutorials -live.

### Step 4: Enter

I choose MusicTrail and press Enter. Albums are in the books' own order: by genre, and within each genre by rank, so the top blues album comes first, and each album is heard genre and rank first.

Screen reader:

- Blues, 1, Born Under a Bad Sign, Albert King, Row 1 of 100

Confirm the wording with a live run: buildTutorials -live.

### Step 5: Control+J

Control plus J, Jump to Record, finds an album by name. I type A Night at the Opera and press Enter.

Screen reader:

- Hard Rock, 9, A Night at the Opera, Queen, Row 39 of 100

Confirm the wording with a live run: buildTutorials -live.

### Step 6: Shift+R

Shift plus R, Say Related, says what this album leads to, changing nothing.

Screen reader:

- performers 1

Confirm the wording with a live run: buildTutorials -live.

### Step 7: Alt+Right Arrow

Records here are linked through a table of links, called maps, each with a kind: this album was recorded by one performer. Alt plus Right Arrow goes in.

Screen reader:

- performers: 1 related record
- Queen

Confirm the wording with a live run: buildTutorials -live.

### Step 8: Shift+R

Queen. Shift plus R again.

Screen reader:

- albums 1, performers 2

Confirm the wording with a live run: buildTutorials -live.

### Step 9: Alt+Right Arrow

The album I came from, and two performers: members of the band. Alt plus Right Arrow, and I choose performers.

Screen reader:

- performers: 2 related records
- Freddie Mercury

Confirm the wording with a live run: buildTutorials -live.

### Step 10: Down Arrow

Down Arrow.

Screen reader:

- John Deacon, Row 2 of 2

Confirm the wording with a live run: buildTutorials -live.

### Step 11: Alt+Left Arrow

Two levels deep. Alt plus Left Arrow comes back one, as a browser goes back.

Screen reader:

- performers, Queen

Confirm the wording with a live run: buildTutorials -live.

### Step 12: Alt+Home

A planned misstep: deep, and not sure how far. Alt plus Home returns to the table I started in, from any depth.

Screen reader:

- albums, A Night at the Opera, Row 39 of 100

Confirm the wording with a live run: buildTutorials -live.

### Step 13: Shift+P

What ties one record to another is its prime, a key DbDo builds from the record itself. Shift plus P, Say Prime, says it.

Screen reader:

- A Night at the Opera|Queen

Confirm the wording with a live run: buildTutorials -live.

### Step 14: Shift+U

Every album carries the address of its article. Control plus Shift plus U, Open url, opens it in the browser; Shift plus U says it.

Screen reader:

- https colon slash slash en dot wikipedia dot org slash wiki slash A Night at the Opera Queen album

Confirm the wording with a live run: buildTutorials -live.

### Step 15: Enter

Now a whole genre, in rank. Control plus F, then F for Fields, and I type Vocal Jazz in the genre box.

Screen reader:

- Filter applied. 10 records match.

Confirm the wording with a live run: buildTutorials -live.

### Step 16: Control+Home

Control plus Home goes to the first row. They are already in rank order, since MusicTrail sorts by genre, then rank.

Screen reader:

- Vocal Jazz, 1, Ella Fitzgerald Sings the Cole Porter Song Book, Ella Fitzgerald, Row 1 of 10

Confirm the wording with a live run: buildTutorials -live.

### Step 17: Control+Shift+O

Ranked first. A table can also have a window of its own: Control plus Shift plus O, Open Table, opens performers beside the albums.

Screen reader:

- Open Table dialog

Confirm the wording with a live run: buildTutorials -live.

### Step 18: Shift+F4

Enter. Shift plus F4 says the windows open, and Control plus Tab moves between them; Control plus F4 closes this one.

Screen reader:

- 2 windows: albums, performers

Confirm the wording with a live run: buildTutorials -live.

### Step 19

What this walk taught. I name the command; the reader gives the key.

### Step 20

Say what a record leads to.

Screen reader:

- Shift plus R

### Step 21

Go into the related records.

Screen reader:

- Alt plus Right Arrow

### Step 22

Come back a level.

Screen reader:

- Alt plus Left Arrow

### Step 23

Return to the start.

Screen reader:

- Alt plus Home

### Step 24

Open the record's web address.

Screen reader:

- Control plus Shift plus U

**Something to try:** Pick a genre you love, and follow one album to everyone who made it.

## 7 - More Templates

Four more ready-made databases in brief -- a lesson to play from TechTraining, a creator and their work in BlindCreations, the emergency list in ContactTrail, a shopping list from RecipeTrail -- and how to bring your copy of any template up to date when DbDo improves it.

**Before you start:** DbDo is open.

### Step 1

One want: to know what else is ready-made, and keep my copies current.

### Step 2: Enter

TechTraining first: more than a thousand free presentations about technology and accessibility, in audio, video and on the web, newest first. Alt plus Shift plus D, and I choose TechTraining.

Screen reader:

- 2026, A Demonstration and Walkthrough of Be My Eyes for Mac, AppleVis, Row 1 of 1167

Confirm the wording with a live run: buildTutorials -live.

### Step 3: Enter

A few minutes free? Control plus Shift plus I, Invoke Script, and Quick Lessons: the ones that play in fifteen minutes or less, shortest first.

Screen reader:

- Accessible Design is Good Design (Part 3) -- At a Glance, 1, video, Row 1 of 372

Confirm the wording with a live run: buildTutorials -live.

### Step 4: Alt+Shift+P

Alt plus Shift plus P, Play Stream, plays a presentation as it plays a station.

Screen reader:

- Homer Player, Accessible Design is Good Design (Part 3) -- At a Glance

Confirm the wording with a live run: buildTutorials -live.

### Step 5: Control+T

Escape. BlindCreations next: apps, books and presentations made by or for blind people, and the people who made them, linked by kind. Alt plus Shift plus D, BlindCreations, and Control plus T to the creators.

Screen reader:

- Choose Table dialog, apps

Confirm the wording with a live run: buildTutorials -live.

### Step 6: Enter

I choose creators, then Control plus J to the last name Bishop.

Screen reader:

- Jeff, Bishop, Row 7 of 133

Confirm the wording with a live run: buildTutorials -live.

### Step 7: Shift+R

Shift plus R says what this creator made, in each kind.

Screen reader:

- apps 4, presentations 3

Confirm the wording with a live run: buildTutorials -live.

### Step 8

Alt plus Right Arrow goes into any of them, as with the band in MusicTrail. Escape. ContactTrail keeps people and the groups they belong to. Its Emergency Contacts script lists everyone in the Emergency group, with every number.

### Step 9

RecipeTrail keeps recipes and their ingredients. Mark the recipes you mean to cook with Control plus M, then its Shopping List script lists each ingredient once, grouped by where it sits in the store.

### Step 10

Now the copies. The first time you open a template, DbDo gives you your own copy, so nothing you change touches the original. When a new DbDo brings an improved template, more stations or a new script, your copy stays as you left it.

### Step 11: U

To take the improvements, open your copy, and from the Help menu choose Update from Template. Alt plus H, then U.

Screen reader:

- Update from Template dialog
- Merge button

Confirm the wording with a live run: buildTutorials -live.

### Step 12: Tab

Two buttons. Merge adds what the template has and your copy lacks, and keeps everything of yours; it asks first what to do about records both have, and Add new only, the first choice, changes none of yours.

Screen reader:

- Replace button

Confirm the wording with a live run: buildTutorials -live.

### Step 13

Replace makes your copy the template again, and keeps your copy as it was beside it, as a dated backup, so nothing is lost. Either way, a new script or report in the template is added beside your copy.

### Step 14: Escape

A planned misstep: choosing Replace for a copy full of your own records. Merge is the first button, so Enter takes it; Escape leaves without a change.

Screen reader:

- DbDo ready

Confirm the wording with a live run: buildTutorials -live.

### Step 15

What this walk taught. I name the command; the reader gives the key.

### Step 16

Run a saved script.

Screen reader:

- Control plus Shift plus I

### Step 17

Play the record.

Screen reader:

- Alt plus Shift plus P

### Step 18

Say what a record leads to.

Screen reader:

- Shift plus R

### Step 19

Update your copy from its template.

Screen reader:

- Alt plus H, then U

**Something to try:** Open the template nearest your own life and make one record your own.

## 8 - Analyze a Dataset

Fisher's iris measurements, brought in from a CSV file; a question in SQL, the database's own language, and in plain words to a model on this computer; a number grouped by species; two measurements that rise together; and the answer handed on to Excel.

**Before you start:** DbDo is open, and installed in the usual place, with its data folder.

### Step 1

One want: to learn what a dataset says. DbDo comes with a famous one: Ronald Fisher's measurements of 150 iris flowers, three species, four measurements each, from 1936. Scientists still learn on it.

### Step 2: Alt+Control+D

Alt plus Control plus D, D for DbDo, opens it or brings it forward.

Screen reader:

- DbDo

Confirm the wording with a live run: buildTutorials -live.

### Step 3: Alt+I

Alt plus I, Import, builds a DbDo database from another file: a spreadsheet, an Access file, or, as here, a CSV file of comma-separated values.

Screen reader:

- Import into a new DbDo database

Confirm the wording with a live run: buildTutorials -live.

### Step 4: C:\Program Files\DbDo\data\iris.csv

I type where the sample is, in DbDo's own folder.

Screen reader:

- C:\Program Files\DbDo\data\iris.csv

Confirm the wording with a live run: buildTutorials -live.

### Step 5: Enter

Enter brings it in, as a working copy; the original file is untouched, and Control plus Shift plus S, Save As, keeps the database as my own.

Screen reader:

- Imported iris.csv into a new DbDo database

Confirm the wording with a live run: buildTutorials -live.

### Step 6: Control+Q

Now a question. For each species, how long are its petals on average? Control plus Q, Query.

Screen reader:

- Query dialog

Confirm the wording with a live run: buildTutorials -live.

### Step 7: select species, count(*) as flowers, round(avg(petal_length), 2) as petal from iris group by species order by petal

I type the question in SQL.

Screen reader:

- select species, count as flowers, round avg petal length, 2 as petal from iris group by species order by petal

Confirm the wording with a live run: buildTutorials -live.

### Step 8: Control+Enter

Control plus Enter runs it from inside the box, where Enter would start a new line.

Screen reader:

- setosa, 50, 1.46, Row 1 of 3

Confirm the wording with a live run: buildTutorials -live.

### Step 9: Control+End

Setosa has the shortest petals. Control plus End.

Screen reader:

- virginica, 50, 5.55, Row 3 of 3

Confirm the wording with a live run: buildTutorials -live.

### Step 10: Shift+Q

Virginica the longest, almost four times as long. Shift plus Q says the query in force; Alt plus Shift plus Q, Query History, lists the questions asked, to ask one again.

Screen reader:

- query: select species

Confirm the wording with a live run: buildTutorials -live.

### Step 11: Escape

A planned misstep: a misspelled column. The database names the word it does not know, and the box keeps the question to fix. Escape.

Screen reader:

- virginica, 50, 5.55, Row 3 of 3

Confirm the wording with a live run: buildTutorials -live.

### Step 12: Shift+F12

SQL is not the only way to ask. Back in the flowers, Shift plus F12, Chat about Table, sends the table with a question in plain words to the model on this computer.

Screen reader:

- Chat about Table

Confirm the wording with a live run: buildTutorials -live.

### Step 13: Which species has the widest sepals?

I ask which species has the widest sepals.

Screen reader:

- Which species has the widest sepals?

Confirm the wording with a live run: buildTutorials -live.

### Step 14: Enter

Enter. The answer comes in words, and the records are not sent anywhere.

Screen reader:

- setosa

Confirm the wording with a live run: buildTutorials -live.

### Step 15: Alt+Shift+G

Escape. A third way needs no SQL at all. Alt plus Shift plus G, Generate from Grid, is the one key for reports on the grid.

Screen reader:

- Generate from Grid

Confirm the wording with a live run: buildTutorials -live.

### Step 16: G

G for Group report: a number totalled and averaged for each value of another column. It asks which column to group by.

Screen reader:

- Group by which column?

Confirm the wording with a live run: buildTutorials -live.

### Step 17: sp

I type s p, for species.

Screen reader:

- sp

Confirm the wording with a live run: buildTutorials -live.

### Step 18: Enter

Enter chooses species.

Screen reader:

- Total and average which number column?

Confirm the wording with a live run: buildTutorials -live.

### Step 19: petal_l

Then the start of petal length.

Screen reader:

- petal l

Confirm the wording with a live run: buildTutorials -live.

### Step 20: Enter

Enter chooses petal length.

Screen reader:

- In short, virginica has the highest average petal length

Confirm the wording with a live run: buildTutorials -live.

### Step 21: Alt+Shift+G

The takeaway comes first, then each species: its count, average, middle, smallest and largest. Escape, and Alt plus Shift plus G again.

Screen reader:

- Generate from Grid

Confirm the wording with a live run: buildTutorials -live.

### Step 22: R

R for Relation report: how strongly each pair of measurements rises and falls together.

Screen reader:

- In short, the strongest relation is between petal length and petal width: very strong, rising together

Confirm the wording with a live run: buildTutorials -live.

### Step 23: Alt+Shift+X

Very strong: longer petals are wider. The report leaves out the flower number, a numbering rather than a measurement, and reminds that a relation is not a cause. Escape. To hand the answer on, Alt plus Shift plus X, Export Data, writes what the grid shows to a file: Excel, CSV, Markdown, a web page and more.

Screen reader:

- Export Data: the 150 records shown

Confirm the wording with a live run: buildTutorials -live.

### Step 24: Control+Shift+Z

Escape. Before a query that changes data, Control plus Shift plus Z, Read Only Toggle, makes the database read only, so a mistyped statement cannot change anything. Pressed again, it makes it writable.

Screen reader:

- read only

Confirm the wording with a live run: buildTutorials -live.

### Step 25

What this walk taught. I name the command; the reader gives the key.

### Step 26

Bring in a file.

Screen reader:

- Import: Alt plus I

### Step 27

Ask in SQL, and run it from inside the box.

Screen reader:

- Query: Control plus Q
- Run: Control plus Enter

### Step 28

Ask in plain words.

Screen reader:

- Chat about Table: Shift plus F12

### Step 29

Report on the grid.

Screen reader:

- Generate from Grid: Alt plus Shift plus G

### Step 30

Hand it on.

Screen reader:

- Export Data: Alt plus Shift plus X

**Something to try:** Bring in a CSV file of your own -- readings from an experiment, a budget, a fitness log -- and ask it which group is highest.

## 9 - Conclusion

What to carry away, the words DbDo uses, and the help built in.

**Before you start:** Nothing is needed; this walk is listened to.

### Step 1

To finish: what to carry away, the words DbDo uses, and where to get help. First, the summary. A table is a list you move through by row and column, and every question about where you are is a Shift key that changes nothing.

### Step 2

Keys follow words, so a key can be guessed. I name a word; the reader gives the key.

Screen reader:

- Open: Control plus O
- Order: Alt plus O
- Filter: Control plus F
- Play: Alt plus Shift plus P

### Step 3

Records lead to others.

Screen reader:

- Say Related: Shift plus R
- Into the related records: Alt plus Right Arrow
- Back: Alt plus Left Arrow

### Step 4

The templates are a start, not a museum. Make them yours; when DbDo improves one, Help, Update from Template keeps your copy current.

Screen reader:

- RadioTrail, to listen
- JobTrail, to search for work
- BookTrail, to read
- MusicTrail, to explore

### Step 5

Now the words, in alphabetical order. I say the term; the reader says what it means.

### Step 6

filter.

Screen reader:

- A condition that hides the records not matching it, until it is cleared.

### Step 7

link.

Screen reader:

- A row in the maps table that ties one record to another, with a kind, such as recorded or member of.

### Step 8

mark.

Screen reader:

- A flag on a record, for a later command to act on; Control plus M sets it, and Shift reverses it.

### Step 9

order.

Screen reader:

- The sort of the list, by a column, ascending or reversed.

### Step 10

pick list.

Screen reader:

- A field's fixed values, offered by Control plus F2, so nothing is mistyped.

### Step 11

prime.

Screen reader:

- The key DbDo builds from a record itself, by which links and merges find it.

### Step 12

query.

Screen reader:

- A question in SQL, answered as a table.

### Step 13

record.

Screen reader:

- One row: one station, one job, one book.

### Step 14

related records.

Screen reader:

- The records a record leads to, through a link or a shared key.

### Step 15

report.

Screen reader:

- A saved layout that writes records as a document.

### Step 16

Say key.

Screen reader:

- Shift and a letter: it asks and changes nothing.

### Step 17

script.

Screen reader:

- A saved question beside a database, run with Control plus Shift plus I.

### Step 18

SQLite.

Screen reader:

- The database format DbDo opens: one file, with its tables inside.

### Step 19

template database.

Screen reader:

- A ready-made database that comes with DbDo, opened with Alt plus Shift plus D; you work in your own copy of it.

### Step 20

Last, more information. Help is built in. The guide, and what changed.

Screen reader:

- F1, the guide
- Shift plus F1, History

### Step 21

The version, and what any key does without doing it.

Screen reader:

- Alt plus F1, About
- Control plus F1, Key Describer

### Step 22

Every command in one list, and these walks again: Alt plus F10, and Play Tutorials on the Help menu.

### Step 23

DbDo dot md, the guide, explains every command and the ideas behind it; Hotkeys dot md lists every key three ways; and the DbDo page on GitHub, at github dot com slash JamalMazrui slash DbDo, has the newest version.

**Something to try:** Open the template nearest your own life, and make it yours.

<!-- walkthrough ends -->

## 1. Where to Go Next

The tutorials above cover the day-to-day. When you want more:

- **The guide.** F1 inside DbDo opens `DbDo.md`, which documents every command,
  the Say keys, and the two computed columns.
- **The hotkey list.** Control+Slash pressed twice shows every command with its key and a line
  saying what it does. Control+F1 does the same one key at a time.
- **The alternate menu.** Alt+F10 lists every command in one window you can
  filter by typing, which is the fastest way to find something whose name you
  half remember.
- **The sample databases.** JobTrail is one of fourteen that ship with DbDo.
  The others hold recipes, music, books, contacts and more, and each is a
  different shape of database to practise on.
- **The audio.** `Tutorials.mkv` holds all of these as one file with a chapter
  each, for listening through rather than reading.
