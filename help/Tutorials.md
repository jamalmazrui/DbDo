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

- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Inspecting a Record](#12-inspecting-a-record)
- [13 - Work Search Record for a Claim or Counselor](#13-work-search-record-for-a-claim-or-counselor)
- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Inspecting a Record](#12-inspecting-a-record)
- [13 - Work Search Record for a Claim or Counselor](#13-work-search-record-for-a-claim-or-counselor)
- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Inspecting a Record](#12-inspecting-a-record)
- [13 - Work Search Record for a Claim or Counselor](#13-work-search-record-for-a-claim-or-counselor)
- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Inspecting a Record](#12-inspecting-a-record)
- [13 - Work Search Record for a Claim or Counselor](#13-work-search-record-for-a-claim-or-counselor)
- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Inspecting a Record](#12-inspecting-a-record)
- [13 - Work Search Record for a Claim or Counselor](#13-work-search-record-for-a-claim-or-counselor)
- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Inspecting a Record](#12-inspecting-a-record)
- [13 - Work Search Record for a Claim or Counselor](#13-work-search-record-for-a-claim-or-counselor)
- [14 - Menus, Windows and Help](#14-menus-windows-and-help)
- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Inspecting a Record](#12-inspecting-a-record)
- [13 - Work Search Record for a Claim or Counselor](#13-work-search-record-for-a-claim-or-counselor)
- [14 - Menus, Windows and Help](#14-menus-windows-and-help)
- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Look and Prime](#12-look-and-prime)
- [13 - Report, Save and Copy](#13-report-save-and-copy)
- [14 - Work Search Record for a Claim or Counselor](#14-work-search-record-for-a-claim-or-counselor)
- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Look and Prime](#12-look-and-prime)
- [13 - Report, Save and Copy](#13-report-save-and-copy)
- [14 - Work Search Record for a Claim or Counselor](#14-work-search-record-for-a-claim-or-counselor)
- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Look and Prime](#12-look-and-prime)
- [13 - Report, Save and Copy](#13-report-save-and-copy)
- [14 - Work Search Record for a Claim or Counselor](#14-work-search-record-for-a-claim-or-counselor)
- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Look and Prime](#12-look-and-prime)
- [13 - Report, Save and Copy](#13-report-save-and-copy)
- [14 - Work Search Record for a Claim or Counselor](#14-work-search-record-for-a-claim-or-counselor)
- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Look and Prime](#12-look-and-prime)
- [13 - Report, Save and Copy](#13-report-save-and-copy)
- [14 - Work Search Record for a Claim or Counselor](#14-work-search-record-for-a-claim-or-counselor)
- [00 - Overview and Table of Contents](#00-overview-and-table-of-contents)
- [01 - Installing DbDo](#01-installing-dbdo)
- [02 - Opening JobTrail](#02-opening-jobtrail)
- [03 - Menus, Windows and Help](#03-menus-windows-and-help)
- [04 - Adding a New Record](#04-adding-a-new-record)
- [05 - Inspecting a Record](#05-inspecting-a-record)
- [06 - Editing a Record](#06-editing-a-record)
- [07 - Find and Jump](#07-find-and-jump)
- [08 - Order and Where Filter](#08-order-and-where-filter)
- [09 - Select the Columns You Hear](#09-select-the-columns-you-hear)
- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Look and Prime](#12-look-and-prime)
- [13 - Report, Save and Copy](#13-report-save-and-copy)
- [14 - Work Search Record for a Claim or Counselor](#14-work-search-record-for-a-claim-or-counselor)
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

Shift plus T, Say Tags, and Shift plus U, Say URL, work the same way.

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

## 07 - Find and Jump

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

### Step 4: Control+F

Control plus F, Find -- it searches every field, notes included.

Screen reader:

- Find dialog
- Text combo box, blank, ALT+T

### Step 5: Enter

Screen reader:

- Sample Health Network (illustration only) Records Coordinator applied, 4 of 4

F3 searches again. Shift reverses: Shift plus F3 searches back, Control plus Shift plus F finds backwards.

### Step 6: Shift+F

Shift plus F, Say Find, reminds me what I looked for.

Screen reader:

- find, screen reader

**Something to try:** Find a job by a word in its notes, then jump back to it by employer.

## 08 - Order and Where Filter

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

### Step 4: Control+W

Control plus W, Where filter -- W for Where, the word SQL uses. It decides which rows I hear.

Screen reader:

- Where Filter dialog
- Filter combo box, blank, ALT+F

### Step 5: Enter

I want what is still moving: status not rejected.

Screen reader:

- Records list view
- Example Widgets Company (sample employer) Accessibility Analyst interviewing, 1 of 3

Shift plus W, Say Where, and Shift plus Y, Say Yield -- how many rows it left.

### Step 6: Control+Shift+W

Control plus Shift plus W clears it. Adding Shift reverses.

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

<!-- walkthrough ends -->

## 1. Where to Go Next

The tutorials above cover the day-to-day. When you want more:

- **The guide.** F1 inside DbDo opens `DbDo.md`, which documents every command,
  the Say keys, and the two computed columns.
- **The hotkey list.** Alt+Shift+H shows every command with its key and a line
  saying what it does. Control+F1 does the same one key at a time.
- **The alternate menu.** Alt+F10 lists every command in one window you can
  filter by typing, which is the fastest way to find something whose name you
  half remember.
- **The sample databases.** JobTrail is one of fourteen that ship with DbDo.
  The others hold recipes, music, books, contacts and more, and each is a
  different shape of database to practise on.
- **The audio.** `Tutorials.mkv` holds all of these as one file with a chapter
  each, for listening through rather than reading.
