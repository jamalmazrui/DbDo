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
- [10 - Conclusion](#10-conclusion)
- [11 - More Information](#11-more-information)
- [00 - Overview and Table of Contents](#00-overview-and-table-of-contents)
- [01 - Install and Launch](#01-install-and-launch)
- [02 - User Interface Concepts](#02-user-interface-concepts)
- [03 - Key Patterns](#03-key-patterns)
- [04 - Open and Move Through a Database](#04-open-and-move-through-a-database)
- [04 - Start an App from the Kit](#04-start-an-app-from-the-kit)
- [05 - Add, Inspect and Edit Records](#05-add-inspect-and-edit-records)
- [05 - Build, Check and Release](#05-build-check-and-release)
- [06 - Find, Order and Select](#06-find-order-and-select)
- [06 - The Installer and Its Finish Page](#06-the-installer-and-its-finish-page)
- [07 - Related, Marked, Reports and a Work Search Record](#07-related-marked-reports-and-a-work-search-record)
- [07 - Spoken Tutorials](#07-spoken-tutorials)
- [08 - RadioTrail: Find the Seahawks on the Air](#08-radiotrail-find-the-seahawks-on-the-air)
- [08 - Shared Code and the Homer Player](#08-shared-code-and-the-homer-player)
- [09 - Glossary](#09-glossary)
- [10 - Conclusion](#10-conclusion)
- [11 - More Information](#11-more-information)
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

<!-- walkthrough ends -->

<!-- walkthrough: written by makeTutorials.py, do not edit between the markers -->

## 00 - Overview and Table of Contents

What DbDo is, in a paragraph; the two reader keys every walk assumes; then the table of contents, one line per walk.

**Before you start:** Nothing is needed; this walk is listened to.

### Step 1

DbDo is a database program for working by ear. A database is a file of tables; a table is records; a record is one job, one book, one radio station. You hear a row, you search every field, you narrow the list, you play what you find. Every key is named for a word of its command, and every dialog works one way.

### Step 2: Insert+UpArrow

Two keys before anything else, both the reader's own. If a line goes by too fast, Insert plus Up Arrow says it again.

Screen reader:

- (the last line, read a second time)

### Step 3: Insert+Tab

And if you lose your place, Insert plus Tab says where you are: the control, its state, its position, and any hint it carries. The walks are heard with those hints off, the way most people work.

Screen reader:

- (the current control, with its state and position)

### Step 4

Now the table of contents. I say the number and the title; the reader says what the walk covers.

### Step 5

One, Install and Launch.

Screen reader:

- The download, the installer's pages and boxes, and DbDo opening by itself.

### Step 6

Two, User Interface Concepts.

Screen reader:

- Windows inside a window, the grid, the row you hear and the record behind it, dialogs that all work one way, the status bar, and where help is.

### Step 7

Three, Key Patterns.

Screen reader:

- The rules every key follows, so a key can be guessed before it is learned, and the keys that explain the keys.

### Step 8

Four, Open and Move Through a Database.

Screen reader:

- JobTrail opened and arrowed through: rows, cells, the next table, and the menus.

### Step 9

Five, Add, Inspect and Edit Records.

Screen reader:

- A record added with its pick lists, looked at whole, and changed.

### Step 10

Six, Find, Order and Select.

Screen reader:

- Typing, Jump and Keywords; ordering and filtering the list; choosing which fields a row speaks.

### Step 11

Seven, Related, Marked, Reports and a Work Search Record.

Screen reader:

- Following links, marking a few, look and prime, running a report, and a whole task end to end.

### Step 12

Eight, RadioTrail, Find the Seahawks on the Air.

Screen reader:

- Sixty thousand stations, worked through three wants: the home team, jazz anywhere, jazz near home; playing, recording, keeping.

### Step 13

Nine, Glossary.

Screen reader:

- The words DbDo uses, in alphabetical order, one line each.

### Step 14

Ten, Conclusion.

Screen reader:

- Four sentences to carry away, and where to begin.

### Step 15

Eleven, More Information.

Screen reader:

- The guide and history from inside DbDo, the documents, the project page, updates, and the other Homer Tools.

### Step 16

Twelve walks, each under five minutes, a little over an hour together. They are a course, not a reference: each one assumes those before it.

**Something to try:** Listen to the walks in order; each one assumes the ones before it.

## 01 - Install and Launch

Installing DbDo: the security warning, the permission prompt, the options at the end, and the program opening by itself.

**Before you start:** The installer is downloaded, and your reader is running.

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

Enter presses Install, since Install is the button the wizard is offering.

Screen reader:

- Click Install to continue with the installation, or click Back if you want to review or change any settings.
- Install Button, Alt i

### Step 5: Enter

Enter again presses Install. The last page lists the extras. I arrow through them; Space ticks one.

Screen reader:

- Setup has finished installing DbDo on your computer.
- Tree view, Install scripts for improving use with the screen reader, checked, 1 of 6

A line for a reader appears only when you have that reader.

### Step 6: DownArrow

Down Arrow moves to the next option in the list.

Screen reader:

- Install the add-on for improving use with the screen reader, checked, 2 of 6

### Step 7: DownArrow

Ollama runs AI on my own computer. I want it, so I tick it.

Screen reader:

- Install Ollama 0.34.2, not checked, 3 of 6

### Step 8: Space

Spacebar ticks it, and the answer is one word.

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

Enter presses Finish, and DbDo opens by itself.

Screen reader:

- DbDo
- JobTrail dot d b, jobs
- Records list view
- Demo Data Cooperative (made up) Data Quality Specialist rejected, 1 of 4
- DbDo ready

Alt plus Control plus D starts DbDo from anywhere afterwards. D for DbDo.

### Step 11

What each box on the last page means. The screen reader scripts: Install when they are not there, Update when a newer set is available, Reinstall when they are current -- the word is the state, so you need not ask.

### Step 12

mpv is the player behind Play Stream; the box offers it, and Install, Update or Reinstall says its state the same way. Ollama is optional, for the AI features; the models are large, so it is unticked unless you want it.

### Step 13

The results box after Finish reports each item by name, one line each -- installed, updated, or already current -- and the same summary is saved in the logs folder under your local application data.

### Step 14

Where things went. The program is in Program Files; your databases, settings and logs are under your local application data, in a folder named DbDo. Nothing of yours is in Program Files, so an update never touches your data.

### Step 15: Alt+Control+D

Alt plus Control plus D opens DbDo from anywhere in Windows from now on, or brings it forward when it is already open; DbDo is one instance, so the key never opens a second copy.

Screen reader:

- DbDo

### Step 16: F11

F11, Elevate Version, is the installer's other half: it asks the web for a newer DbDo and offers to fetch and run it. Elevate sounds like eleven, which is how the key was chosen.

Screen reader:

- Elevate Version dialog

### Step 17: Escape

Escape. To remove DbDo later, Windows Settings, Apps; your data folder stays unless you delete it yourself.

Screen reader:

- DbDo

### Step 18

What this walk taught. I say the key; the reader says what it does.

### Step 19

Alt plus R.

Screen reader:

- Recent Files

**Something to try:** Open the log the results box names, and find where DbDo was installed.

## 02 - User Interface Concepts

What DbDo is made of: windows inside a window, the grid, the row you hear and the record behind it, prime and look, dialogs that all work one way, and the status bar.

**Before you start:** DbDo is open with any database. Nothing needs pressing in this walk; it is listened to.

### Step 1

DbDo is one window with its own windows inside: each database you open is a window of its own, and Control plus Tab moves between them.

### Step 2

A database window is a grid. Each row is one record; each column is one field. Down and Up Arrows move between records, Left and Right between fields, and the reader reads the row, then the cell.

### Step 3

The row reads only the fields you chose to hear -- three or four of them. The rest of the record is there for searching and for Inspect, Control plus I, which shows every field on its own line.

### Step 4

A table has a prime, the field or fields that make a record unique, and a look, the few fields that say what a record is. Both are computed, and both travel with the database.

### Step 5

Every dialog is built the same way: a label and its control, Tab between them, Alt plus the underlined letter to jump to one, Control plus Enter for OK from anywhere, Escape to cancel.

### Step 6

The status bar at the bottom says the table, the row, and whether a filter or marks are in force. Shift plus Z says it; Z is the bottom of the alphabet, like the bar is the bottom of the window.

### Step 7

Help is in four places, and they are the same in every Homer program. F1 opens the guide, the whole program in one document. Shift plus F1 opens the history of changes. Alt plus F1 says the version and offers the newer one if there is one.

### Step 8

The Help menu, F10 then H, Hotel, holds the same three, and Play Tutorials, which plays these walks. And the menus themselves are help: arrow through any menu and the reader says each command with its key and its letter.

### Step 9

A template is a database that comes with DbDo -- books, contacts, jobs, radio and more. The first time you open one, DbDo copies it into your data folder, and that copy is yours; the template itself is never changed.

### Step 10

One dialog adds a record and edits it: a label and a box for each field, pick lists where a field has fixed values, and Control plus Enter to save from anywhere in it. Learn it once for both.

### Step 11

Two states colour everything you hear. A filter narrows the list, and the status bar says filtered; marks tick records for a command to act on together, and the status bar counts them. Shift plus Z says both.

### Step 12

Results that are documents -- reports, exports -- do not open in DbDo. They open in your editor, EdSharp if you have it, so you read and keep them there.

### Step 13

Every Say key, pressed twice, shows the same words in a window you can arrow through and copy from, for when speech went by too fast or you want to keep what was said.

### Step 14

DbDo writes a log for every session, in the logs folder under your local application data; when something goes wrong, that file with a line about what you were doing is the fastest way to a fix.

### Step 15

Several databases can be open at once, each in its own window inside DbDo; Control plus Tab moves between them, and the title says which you are in. A table inside a database is reached with the Next Table and Prior Table keys, and the status bar names it.

### Step 16

Order is a state too: Order Records sorts the list by the field you choose, and a letter typed on the grid jumps within that order, so sorting by employer and typing a letter reaches an employer.

### Step 17

Pick lists do two jobs. In the record dialog, F4 opens a field's fixed values; on the grid, F4 on a cell does the same, so a status is changed without opening the record.

### Step 18

Related records are the link between tables: on a job, Related Records lists its actions; on an action, the job it belongs to. One key follows the link either way.

### Step 19

Everything in this walk is Windows underneath -- a list view, an edit box, a dialog, a status bar -- so your reader's own commands, Insert plus Up Arrow, Insert plus Tab, Insert plus Page Down, work on all of it. DbDo adds what is said and when.

### Step 20

What this walk taught: the keys you heard.

**Something to try:** Open a database and name each thing as you reach it: the window, the grid, the row, the cell, the status bar.

## 03 - Key Patterns

The rules every DbDo key follows, so a key can be guessed before it is learned: the word gives the letter, Control does, Shift asks, Shift reverses, Alt Shift is a command with no control, and the function keys follow Windows.

**Before you start:** DbDo is open with any database.

### Step 1

Every key in DbDo is named for a word in its command: Control plus K is Keywords, Control plus F is Filter, Control plus M is Mark. A key never comes from the middle of a word.

### Step 2

Control plus a letter does something. Shift plus a letter asks something, and changes nothing: Shift plus C says the cell, Shift plus S the selected columns, Shift plus F the filter, Shift plus Z the status.

### Step 3

Adding Shift to a Control key reverses it: Control plus M marks, Control plus Shift plus M unmarks; Control plus F filters, Control plus Shift plus F clears the filter.

### Step 4

Alt plus Shift plus a letter is a command with no control of its own: Alt plus Shift plus P plays a stream, Alt plus Shift plus R runs a report.

### Step 5

The function keys follow Windows and Office: F1 help, F2 edit, F3 find again, F4 pick from a list, F5 refresh, F10 the menus, F11 the version, F12 files.

### Step 6

Pressing a Say key twice shows the same words in a window you can arrow through and copy from, for when speech went by too fast.

### Step 7: Control+F1

The keys that explain the keys. Control plus F1 is the Key Describer: on, every key says what it does instead of doing it, the safe way to explore the keyboard. Hotkeys, in the Help menu, lists every key three ways; F1 opens the guide; and your reader's own Insert plus Tab says where you are.

Screen reader:

- Key Describer On

### Step 8: Control+F1

Control plus F1 again turns it off.

Screen reader:

- No Key Describer

### Step 9

The rules let a key be guessed. I say a command; the reader says the key the rules give it.

### Step 10

Order Records.

Screen reader:

- Alt plus O

### Step 11

Go to Record, by number.

Screen reader:

- Control plus G

### Step 12

Jump to Record, by a word in one field.

Screen reader:

- Control plus J

### Step 13

Delete Record.

Screen reader:

- Control plus D

### Step 14

Say Prime.

Screen reader:

- Shift plus P

### Step 15

Say Look.

Screen reader:

- Shift plus L

### Step 16

Clear Filter.

Screen reader:

- Control plus Shift plus F

### Step 17

Keys that are the reader's are not DbDo's: anything with Insert in it belongs to the screen reader, and DbDo never uses the Insert key, so the two can never collide.

### Step 18

In a dialog, Alt plus a letter jumps to a control, Control plus Enter is OK, Escape is Cancel. In a menu, the letter alone runs the item, and arrowing a menu says each item's key, which is a quiet way to learn them.

### Step 19

What this walk taught. I say the key; the reader says what it does.

### Step 20

Control plus F1.

Screen reader:

- Key Describer Toggle

**Something to try:** Guess the key for Order Records, Inspect and Clear Filter before you look them up, then check with Control plus F1.

## 04 - Open and Move Through a Database

Opening JobTrail, the first template, and moving through it: what a row says, what a cell says, the next table, and then the menus, which name every key as you arrow through them. This walk assumes walks one to three.

**Before you start:** DbDo is open and no database is open yet.

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

My reader's say-line key reads the whole row again. Shift plus Z, Say Status, gives the summary. H for Here.

Screen reader:

- status, jobs row 3 of 4, sort employer

### Step 6: Control+PageDown

Control plus Page Down moves to the next table. JobTrail gives me five: actions, contacts, docs, jobs and stories.

Screen reader:

- JobTrail dot d b, stories
- Records list view

Two more tables hold DbDo's own pick lists and links. It keeps those out of the way.

### Step 7: Alt

Alt opens the menu bar. Each menu's first letter opens it directly.

Screen reader:

- Menu bar
- File, F

File, Edit, Navigate, Query, Misc, Window, Help. Every letter differs.

### Step 8: DownArrow

Down Arrow opens File. Each item gives its key, then its letter.

Screen reader:

- New Database..., N

### Step 9: DownArrow

Down Arrow moves to the next item, and each one says its letter last.

Screen reader:

- Add Table..., A

### Step 10: DownArrow

Down Arrow again, and this one has a key as well as a letter.

Screen reader:

- Open Database..., Control+O, O

The key works from anywhere. The letter works while the menu is open. They match whenever the key has a letter.

### Step 11: Escape

A few menus hold a submenu for a large or rarely used group. Edit, B for Bulk Marking, is one. Right Arrow goes in; Left Arrow comes back.

Screen reader:

- Leaving menus

The others: Query, S for Say, Misc, T for Tools, and Help, M for More Documents. None goes deeper than one level.

### Step 12: Control+Tab

DbDo can keep several tables open, each in its own window -- the multiple document interface. Control plus Tab moves among DbDo windows.

Screen reader:

- JobTrail dot d b, actions
- Records list view

Control plus Shift plus T opens a table in a new window. Control plus F4 closes one.

### Step 13: Control+F1

F1 is help: the guide. Shift plus F1 is the history, Control plus F1 describes the next key I press.

Screen reader:

- Key Describer On

Help also holds the ReadMe, Hotkeys, the FAQ, and Play Tutorials.

### Step 14: F11

F11 is Elevate Version -- elevate sounds like eleven. It checks for a newer DbDo.

Screen reader:

- Elevate Version dialog

Nothing downloads without asking.

### Step 15

Home and End reach the first and last field of the row; Control plus Home and Control plus End the first and last record; Page Up and Page Down move a screen of records at a time.

### Step 16

Typing a letter jumps to the first record whose first column starts with it, in the current order; so a sorted list is reached by its first letters, as a file list is.

### Step 17: Control+G

Control plus G, Go to Record, takes a number, for when you know the row.

Screen reader:

- Go to Record dialog, Row: edit

### Step 18: Escape

Escape. Everything in this walk works the same in every table and every template: the grid is the grid.

Screen reader:

- JobTrail jobs

### Step 19

What this walk taught. I say the key; the reader says what it does.

### Step 20

Shift plus C.

Screen reader:

- Say Cell

### Step 21

Shift plus Z.

Screen reader:

- Say Status

### Step 22

Control plus Tab.

Screen reader:

- Next Window

### Step 23

Control plus F1.

Screen reader:

- Key Describer Toggle

**Something to try:** Open JobTrail, arrow through it, and read the File and Edit menus to the end.

## 04 - Start an App from the Kit

One want: a new program of your own, built on the kit, running by the end of the walk. newHomerApp, what it makes, the first build, the first run, the first change, and where the kit's classes come in. This walk assumes walks one to three.

**Before you start:** The kit is at C colon backslash HomerDev, and a command prompt is open there.

### Step 1: newHomerApp Recipes

The want: a new program of your own, built on the kit, running by the end of the walk. In the kit folder, newHomerApp with a name makes the folder and its files.

Screen reader:

- Made C colon backslash Recipes with 14 files. Run build there.

### Step 2

What is in it. The program file, Recipes dot cs, with the Homer classes referenced from the kit; build dot cmd, the stages every Homer build runs; the installer script with the shared finish page; accept dot inix, the acceptance checks; the twelve tutorial skeletons in help; the ReadMe, guide and history, each with its HTML pair; and the policy files that say which files travel to the repository.

### Step 3

The template program is the fruit basket's shape with the new name: a dialog with a field and a list, a report, F1 help on the fields, a session log. It runs before you have written a line, so every change is made against a working program.

### Step 4: build

Change into the folder and build.

Screen reader:

- Kit, C colon backslash HomerDev version 1.52.8
- Version, 1.0.0
- Built Recipes dot exe version 1.0.0

### Step 5: Recipes

Run it.

Screen reader:

- Recipes, the list is empty
- Name, edit

### Step 6: pancakes

Type a name and press Enter.

Screen reader:

- pancakes added, 1 item in the list

### Step 7: Alt+F4

Alt plus F4, and run it again: kept.

Screen reader:

- Recipes, 1 item in the list

### Step 8

Now the first real change. Open Recipes dot cs in EdSharp; the Camel Type conventions the kit's skill teaches are already followed in it: a prefix on every name says its type, functions in lower camel case, constants with c underscore. An AI assistant that has read the kit's skills writes the same way, so what it adds reads like what was there.

### Step 9

Where the Homer classes come in. Lbc builds every dialog; Say speaks; Log writes the session log; Inix reads settings; Web fetches; Util has the pluralizer that said one item and not one items. None of them is in your folder; they compile from the kit, so a kit update improves every app on the next build.

### Step 10

The Python template, newHomerApp with dash py, makes the same program in Python, built with the kit's Python modules; buildFruitBasketPy showed the shape in walk one: a build environment made, what the build needs installed, and an executable at the end.

Screen reader:

- Creating the build environment
- Installing what the build needs
- Built FruitBasketPy dot exe version 1.0.0

### Step 11

A planned misstep. Name the app with a space, and newHomerApp refuses in one line, because the name becomes a folder, a file, a class and a shortcut key.

Screen reader:

- A Homer app's name is one word in upper camel case, like FruitBasket.

### Step 12

From here the work is yours: fields in the dialog, commands on the menus with their keys named for their words, a template database or two. Walk five is what happens each time you type build.

### Step 13

What this walk taught. I say the idea or the key; the reader says the command or the name.

### Step 14

Make a new app from the template.

Screen reader:

- newHomerApp

### Step 15

Every stage, from encoding to installer.

Screen reader:

- build

**Something to try:** Make an app of your own with newHomerApp, build it, run it, and add one field to its dialog.

## 05 - Add, Inspect and Edit Records

Adding a record with its pick lists, looking at one record whole, and changing a value and checking what was saved.

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

Typing a letter jumps to the first value starting with it, so L reaches lead. That is L, Lima.

Screen reader:

- lead, 4 of 7

### Step 6: Control+Enter

Enter takes it. Control plus Enter saves the record from anywhere in the dialog.

Screen reader:

- Records list view
- Widget Works (your own entry) Support Analyst lead, 5 of 5

### Step 7: Control+I

A row speaks three fields. Control plus I, Inspect Record, reads them all.

Screen reader:

- Inspect Record dialog
- employer: Example Widgets Company (sample employer)

I for Inspect. A read-only window; Escape closes it.

### Step 8: Escape

For one fact, I ask instead. Shift plus N, Say Notes.

Screen reader:

- Records list view

### Step 9: Shift+N

Shift plus N is Say Notes -- N for Notes -- and reads the whole note.

Screen reader:

- notes, Illustrative record. Interview booked for the 24th; ask about screen reader testing tools.

Shift plus T, Say Tags, and Shift plus U, Say URL, work the same way.

### Step 10: Shift+E

Shift plus E, Say Edited: when I last changed it.

Screen reader:

- edited, September 19, 2026 at 10:01 PM

Shift plus A, Say Added, says when it arrived.

### Step 11: Alt+Shift+N

Alt plus Shift plus N edits the notes in their own window, for a long one.

Screen reader:

- Notes dialog
- Notes edit, multiline, Illustrative record. Interview booked for the 24th.

Control plus Enter saves; Escape leaves.

### Step 12: Enter

Enter opens the record in the same dialog I used to add one.

Screen reader:

- Edit Record dialog
- Employer edit, Example Widgets Company (sample employer)

### Step 13: o

Tab to status, F4 for the list, O for offer. That is O, Oscar.

Screen reader:

- offer, 5 of 7

### Step 14: Control+Enter

Control plus Enter saves, the way it presses OK in any DbDo dialog.

Screen reader:

- Records list view
- Example Widgets Company (sample employer) Accessibility Analyst offer, 2 of 4

### Step 15: F2

For one field, F2 edits the cell in place -- the same F2 that renames a file in Windows.

Screen reader:

- Cell edit, offer

Enter saves the cell and keeps my place.

### Step 16: Escape

Shift plus C checks it.

Screen reader:

- Records list view

### Step 17: Shift+C

Shift plus C is Say Cell: the column, where the row is, and the value.

Screen reader:

- status
- row 2 of 4
- offer

### Step 18: Control+Shift+C

Control plus Shift plus C, Copy Record, duplicates the current row as a new one, for a job much like the last; then edit the few fields that differ.

Screen reader:

- Record copied

### Step 19: Control+D

Control plus D, Delete Record, removes the current record -- or every marked record when marks exist -- after asking once.

Screen reader:

- Delete this record? Yes button

### Step 20: Escape

Escape keeps it. The question is the only confirmation DbDo asks; everything else is saved as you go.

Screen reader:

- JobTrail jobs

### Step 21

What this walk taught. I say the key; the reader says what it does.

### Step 22

Control plus N.

Screen reader:

- New Record

### Step 23

F4.

Screen reader:

- Current Windows

### Step 24

Control plus Enter.

Screen reader:

- Open Cell Value

### Step 25

Control plus I.

Screen reader:

- Inspect Record

**Something to try:** Add a job of your own, inspect it, then change one field.

## 05 - Build, Check and Release

One want: a change made this morning, published by lunch, with nothing shipped that does not work. What build does, stage by stage; what check refuses, with the misstep everybody meets; what release publishes and what it leaves behind. This walk assumes walk four.

**Before you start:** An app built on the kit is open in a command prompt, with a change made and GitHub signed in.

### Step 1: build

The want: a change made this morning, published by lunch, with nothing shipped that does not work. Three commands do it, and each refuses when it should. First, build, in the app's folder.

Screen reader:

- Kit, C colon backslash HomerDev version 1.52.8
- Version, 1.0.228 (bumped)

### Step 2

The stages, in order, each logged. The kit's version is checked against what the app needs. Old file names are retired. The encoding of every file is put right: UTF-8 with a byte order mark, CRLF line endings. The hotkey list is generated from the program itself. Any tutorial whose audio is missing or older than its script is spoken. The program compiles. The installer is built.

### Step 3

Each stage says one line and writes the rest to the log. Here is the line that matters most.

Screen reader:

- Built DbDo underscore setup dot exe version 1.0.228

### Step 4: scripts\check

Second, check. It is also the first thing release runs, so you may skip it; but it is a few seconds, and it says exactly what release would refuse.

Screen reader:

- 13 checks passed, 0 checks failed, 2 checks not checked.

### Step 5

What the checks are. Every file in the Homer encoding. Every access letter unique within its dialog or menu. No key on a reader's key. The app's files under the Local tree, never Roaming. The hotkey list current. The tutorials clean. Each acceptance check in accept dot inix passing. And an evidence report written in Markdown, for anyone who asks what was checked.

### Step 6

A planned misstep, the one everybody meets. Change a dialog and reuse a letter, and check names the dialog and the two captions.

Screen reader:

- 12 checks passed, 1 check failed. failed: access letters -- Filter Records: F is used by Find and Filter

### Step 7: scripts\release

Third, release. It reads the version from the installer, refuses if the last build did not succeed, runs check, commits and pushes, tags the version, and publishes the installer on GitHub.

Screen reader:

- GitHub has no published release tagged v1.0.228.
- DbDo 1.0.228 published.

### Step 8

Release refuses for honest reasons and says which. A build still running: the last build did not succeed, build again, then release. A check failing: nothing was published, its report names it. And it never publishes an installer older than the source beside it.

### Step 9

What the release left behind. A tag, a release page with the installer, and in the logs folder a release log and an evidence report; the ReadMe's download link now points at the new installer. F11 in any installed copy finds it within the hour.

### Step 10

The logs. Every stage wrote its own: build, encoding, hotkeys, tutorials, check, push, release. The console said one line each; the logs say every command and its exit code. When something is wrong, the log is the thing to send -- never a description of the console.

### Step 11

A second planned misstep. Run release before the build has finished speaking its tutorials, and it refuses -- the build has not succeeded yet -- which is right, if blunt; wait for the Built line, then release.

### Step 12

What this walk taught. I say the idea or the key; the reader says the command or the name.

### Step 13

Every stage, encoding to installer.

Screen reader:

- build

### Step 14

What release would refuse, in seconds.

Screen reader:

- check

### Step 15

Tag, publish, and leave a log.

Screen reader:

- release

**Something to try:** Make a small change in an app of your own, then build, check and release it.

## 06 - Find, Order and Select

Reaching a record by typing and by Keywords; ordering the list and narrowing it with Filter Records; choosing which fields a row speaks.

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

Enter takes me to the record it found.

Screen reader:

- Illustration City Library (fictional) Digital Services Assistant lead, 3 of 4

### Step 4: Control+K

Control plus K, Keywords -- K for Keywords. It searches every field, notes included, which is why it is not called Find.

Screen reader:

- Keywords dialog
- Text combo box, blank, ALT+T

### Step 5: Enter

Enter again, and this is the second match.

Screen reader:

- Sample Health Network (illustration only) Records Coordinator applied, 4 of 4

F3 searches again. Shift reverses: Shift plus F3 searches back, Control plus Shift plus F finds backwards.

### Step 6: Shift+F

Shift plus K, Say Keywords, reminds me what I looked for.

Screen reader:

- find, screen reader

### Step 7: Alt+O

Alt plus O, Order -- O for Order. Control plus O is Open everywhere, so Order takes Alt.

Screen reader:

- Order Records dialog
- Column to sort by list box, employer, 4 of 16, ALT+C

### Step 8: s

Typing S jumps to the first field starting with it. That is S, Sierra.

Screen reader:

- status, 13 of 16

### Step 9: Enter

Enter sorts the list, and the reader reads the row I land on.

Screen reader:

- Records list view
- Sample Health Network (illustration only) Records Coordinator applied, 1 of 4

Shift plus O, Say Order, says it later.

### Step 10: Control+F

Control plus F, Filter Records -- F for Filter. It decides which rows I hear, and it writes the condition for me: one box per field, and a symbol in front of a value to compare instead of match.

Screen reader:

- Filter Records dialog
- Filter combo box, blank, ALT+F

### Step 11: Enter

I want what is still moving: status not rejected.

Screen reader:

- Records list view
- Example Widgets Company (sample employer) Accessibility Analyst interviewing, 1 of 3

Shift plus F, Say Filter, and Shift plus Y, Say Yield -- how many rows it left.

### Step 12: Control+Shift+F

Control plus Shift plus F clears it. Adding Shift reverses.

Screen reader:

- Records list view
- Demo Data Cooperative (made up) Data Quality Specialist rejected, 1 of 4

### Step 13: Shift+S

Shift plus S, Say Select, names the fields each row speaks.

Screen reader:

- select, employer, title, status

### Step 14: Alt+S

Alt plus S, Select Columns -- Control plus S saves, so Select takes Alt.

Screen reader:

- Select Columns dialog
- Columns check list box, employer check box checked, 1 of 16, ALT+C

### Step 15: Space

I add the date I applied. Space ticks it.

Screen reader:

- applied underscore date check box checked, 11 of 16

### Step 16: Enter

Enter applies the choice, and the row is shorter from here on.

Screen reader:

- Records list view
- Example Widgets Company (sample employer) Accessibility Analyst interviewing 2026-09-02, 2 of 4

Three or four fields is the useful range. Every row is heard, so every field costs time.

### Step 17

What this walk taught. I say the key; the reader says what it does.

### Step 18

Control plus J.

Screen reader:

- Jump to Record

### Step 19

Control plus K.

Screen reader:

- Keywords

### Step 20

Shift plus F.

Screen reader:

- Say Filter

### Step 21

Alt plus O.

Screen reader:

- Order Records

**Something to try:** Find a record three ways, then change the sort and the columns and listen to the difference.

## 06 - The Installer and Its Finish Page

One want: a program that installs itself and its helpers in one run and says what it did. The shared installer parts heard in DbDo's installer: boxes that say their state in one word, the results box, the summary, and the ten lines an app writes to get all of it. This walk assumes walk five.

**Before you start:** A Homer program's installer is downloaded, and your reader is running.

### Step 1

The want: a program that installs itself and its helpers in one run, and tells you what it did. The kit's HomerComponents dot iss is the shared part of every Homer installer: the finish page, its boxes, their states, and the results. Here is DbDo's.

### Step 2: Alt+R

The installer is downloaded; Enter opens it, and Windows asks because it came from the internet. Alt plus R, Run; then Alt plus Y, Yes, for administrator rights, because a Homer program installs for everyone.

Screen reader:

- User Account Control dialog

### Step 3: Enter

The pages are a wizard: the folder, then Install. Enter presses the default button on each. The page that asks a decision is the last.

Screen reader:

- Setup, Finish page

### Step 4: DownArrow

The finish page lists the optional pieces as boxes. Arrow through them; each says its name, its state, and its size.

Screen reader:

- Update screen reader scripts checked, 1 of 6

### Step 5

The three words, from the kit. Install when the piece is not there. Update when it is there and a newer version is available -- the installer asked winget. Reinstall when it is there and current, unticked unless you want it. You never have to know what is on your machine; the box says.

### Step 6: DownArrow

Down Arrow to the player.

Screen reader:

- Install mpv checked, 2 of 6

### Step 7: Space

Ollama runs AI on your own computer. Its models are large, so it is unticked unless you tick it; Spacebar changes a box, and the answer is one word.

Screen reader:

- checked

### Step 8: Enter

Launch is ticked already. Enter presses Finish, and whatever was ticked installs now; a results box then says what was done, one line per piece.

Screen reader:

- DbDo Setup, screen reader scripts: updated. mpv: installed. Ollama: installed.

### Step 9

The same summary is saved in the logs folder under your local application data, and summarizeSetup, in the program folder, shows it again on any later day.

### Step 10

What the app's installer script writes, and what the kit writes. The app names its pieces in a table -- a name, a winget id, an executable to look for, what it is used for -- and the kit does the rest: the probing, the three states, the labels, the ordering, the results. Ten lines of the app's, for a page that behaves the same in every Homer program.

### Step 11

Screen reader scripts are a piece of their own: the kit knows where each reader keeps its settings or its add-ons, and the box reads Install, Update or Reinstall for them as for the rest.

### Step 12

A planned misstep. Run the installer a second time: every piece already present says so, and Install becomes Reinstall, unticked. Nothing is installed twice.

### Step 13

Removing a Homer program is Windows Settings, Apps; the pieces it installed are programs in their own right and stay for the other Homer programs, and your data folder stays unless you delete it.

### Step 14

What this walk taught. I say the idea or the key; the reader says the command or the name.

### Step 15

The one word that is the box's state.

Screen reader:

- Install, Update, or Reinstall

### Step 16

The shared installer parts.

Screen reader:

- HomerComponents dot iss

**Something to try:** Run a Homer installer, arrow its finish page, and read the results box.

## 07 - Related, Marked, Reports and a Work Search Record

Following a record to the records that belong to it; marking a few to work with; what look and prime are; getting records out as a report; and a whole task end to end, the work search record a counselor asks for. This walk assumes walks four to six.

**Before you start:** JobTrail is open on the jobs table.

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

### Step 4: Control+M

Control plus M, Mark -- M for Mark.

Screen reader:

- Marked row 2

### Step 5: Control+Shift+M

Control plus Shift plus M unmarks. Adding Shift reverses.

Screen reader:

- Unmarked row 2

Shift plus M, Say Mark, asks without changing anything.

### Step 6: Control+DownArrow

Control plus Down Arrow steps to the next marked record.

Screen reader:

- Sample Health Network (illustration only) Records Coordinator applied, 4 of 4

### Step 7: Shift+Space

Shift plus Space counts them.

Screen reader:

- 2 marked rows: Example Widgets Company (sample employer), Sample Health Network (illustration only)

### Step 8: Control+A

For runs of rows: Edit, B for Bulk Marking, then A for Mark All.

Screen reader:

- Marked 4 rows

Control plus A is the key for Mark All; Control plus Shift plus A unmarks all.

### Step 9: Shift+L

Every table has two columns nobody types into. Shift plus L, Say Look.

Screen reader:

- look, Example Widgets Company (sample employer) | Accessibility Analyst | interviewing

Look is the record at a glance. It is what Say Related shows for a linked record.

### Step 10: Shift+P

Shift plus P, Say Prime: the fields that make the record unique.

Screen reader:

- prime, Example Widgets Company (sample employer)|Accessibility Analyst

Employer and title. Two jobs with both the same would be one job.

### Step 11: Control+Shift+C

To tell a friend about a job: Control plus Shift plus C copies the record. C for Copy; Control plus C alone copies the cell.

Screen reader:

- Row copied to clipboard

Labelled lines, ready to paste.

### Step 12: Alt+Shift+R

For a prepared document: File, R for Run Report. Its key is Alt plus Shift plus R.

Screen reader:

- Run Report dialog
- Output list box, application underscore history, 1 of 6, ALT+O

### Step 13: Enter

Enter opens the report in the editor.

Screen reader:

- EdSharp, application underscore history dot m d

### Step 14: Shift+Z

My counselor asks what I did, when, with whom, and what came of it. The actions table holds exactly that.

Screen reader:

- status, actions row 1 of 7, sort action date descending

### Step 15: Shift+C

Three fields matter to the agency: method, evidence, and counts -- whether it was an employer contact.

Screen reader:

- method
- row 1 of 7
- internet

### Step 16: Alt+Shift+R

File, R for Run Report.

Screen reader:

- Run Report dialog
- Output list box, application underscore history, 1 of 6, ALT+O

### Step 17: w

Typing W jumps to the report I want. That is W, Whiskey.

Screen reader:

- work underscore search underscore record, 6 of 6

### Step 18: Enter

Enter runs it, and the editor opens with the result.

Screen reader:

- EdSharp, work underscore search underscore record dot m d

Countable contacts first, with weekly totals; everything else after.

### Step 19

What this walk taught. I say the key; the reader says what it does.

### Step 20

Shift plus R.

Screen reader:

- Say Related

### Step 21

Alt plus RightArrow.

Screen reader:

- Enter Child Table

### Step 22

Control plus M.

Screen reader:

- Mark Record

### Step 23

Control plus Shift plus M.

Screen reader:

- Unmark Record

**Something to try:** Mark three jobs, run the Work Search Record report, and read what it made.

## 07 - Spoken Tutorials

One want: a spoken tutorial for your app, in two voices, without a microphone. The walk file and its four beats, the two voices and what they never do, the pattern of twelve, three to five minutes, the checker and its misstep, the tool that speaks and measures, and where the rules came from. This walk assumes walk five.

**Before you start:** An app built on the kit, with its help folder open.

### Step 1

The want: a spoken tutorial for your app, in the two voices, without a microphone. A walk is a text file in help, Tutorial underscore, a number, a title with underscores, dot inix. The build speaks it.

### Step 2

The format has four beats per key. Say: the host says the key with the word it comes from. Key: the key pressed. Hear: what the reader says, word for word, as it would be spoken -- a file name as said, not as written. Say again: what that meant. Name silence when a key says nothing. Name no screen reader.

### Step 3

Two voices, because a program has two: the person, and the reader answering. The exchange is the teaching device, and it is used for more than keystrokes: a glossary is the host saying the term and the reader saying the meaning; a recap is the host saying the key and the reader saying the command. What the voices never do is chat.

### Step 4

The pattern of twelve, the same for every app. Zero, the overview and contents. One, install and launch. Two, the interface. Three, the key rules. Four to eight, up to five tasks, each built around a plain want. Nine, the glossary. Ten, the conclusion. Eleven, more information, always last.

### Step 5

Three to five minutes for parts one to ten: under three is too thin to repay the listener's start; over five loses them. A thin walk gets substance -- the adjacent thing the want needs, a planned misstep and its recovery -- never padding. The tool measures the audio and says what runs under or over.

### Step 6: scripts\checkTutorial

checkTutorial reads every walk before anything is spoken. It wants Intro and Setup, a Say in every step, Hear lines in words, no reader named, the first walk teaching the two reader keys, and the pattern's names and numbers; a set not yet the pattern is a notice, not a silence.

Screen reader:

- 12 scripts checked, 0 problems.

### Step 7

A planned misstep. Write a Hear line with a plus sign in it, or a file name as written, and the checker names the step.

Screen reader:

- Tutorial underscore 17, step 6: Hear writes an access key with a plus sign; the reader says the words

### Step 8: scripts\buildTutorials -build

buildTutorials speaks them. Two voices: Kokoro, with a narrator and a reader, both licensed to redistribute; Piper when Kokoro is not there. The reader's voice shifts by context -- a cursor line, a message, a menu -- so a message is heard as a message.

Screen reader:

- Creating 04 underscore Open underscore and underscore Move dot mp3, 23 steps. A few minutes.

### Step 9

The audio is named like a chapter -- 04 underscore Open underscore and underscore Move dot mp3 -- so a folder or a player shows the number and the title. A walk whose audio is older than its script is spoken again; the rest are kept.

### Step 10

At the end, the lengths, and the two lines that matter.

Screen reader:

- 04 underscore Open underscore and underscore Move dot mp3 runs 2 33
- Wrote Tutorials dot m3u naming 12 tutorials, 27 minutes in all.

### Step 11

Under three minutes or over five, the tool says so by name, with the guideline in one line. Tutorials dot m3u is the playlist Play Tutorials uses; Tutorials dot md is the transcript, made from the same scripts; TutorialFeed dot xml is a podcast feed of the same audio.

### Step 12

Two kinds of listener. A walk is written for the person who will press the keys tomorrow; it is also what an AI assistant reads to learn the program, which is why the skills folder points at it.

### Step 13

The learning behind the rules came from ninety-eight recorded screen reader training sessions and from Quill Radio's tutorials: spell a lone letter with its alphabet word; one planned misstep per walk; close by naming the two or three keys taught; a concrete want before a feature. TutorialLearnings dot md in the kit's help has the rest.

### Step 14

What this walk taught. I say the idea or the key; the reader says the command or the name.

### Step 15

Reads every walk before anything is spoken.

Screen reader:

- checkTutorial

### Step 16

Speaks what is missing or stale and measures it.

Screen reader:

- buildTutorials

### Step 17

The playlist Play Tutorials uses.

Screen reader:

- Tutorials dot m3u

**Something to try:** Write one task walk for your app, run checkTutorial, and let the build speak it.

## 08 - RadioTrail: Find the Seahawks on the Air

The Internet radio directory that comes with DbDo, worked through three wants: the station carrying the Seattle Seahawks, found by a word it says of itself and played, recorded and kept; jazz from anywhere, by filtering on genre; and jazz near home, by adding the state. This walk assumes walks four to seven.

**Before you start:** DbDo is open, fetchStations has been run once, and no database is open yet.

### Step 1: Alt+F

You opened a template in walk four. The same way: Alt plus F, then T, Tango.

Screen reader:

- File menu

### Step 2: T

T, Tango.

Screen reader:

- Template Databases dialog, Choose a template database, list box, BookTrail, 1 of 14

### Step 3: R

R, Romeo, jumps to the first template starting with R.

Screen reader:

- RadioTrail, 11 of 14

### Step 4: Enter

Enter opens it. The first time, DbDo makes your own copy in your data folder; that copy is the one that fills with stations.

Screen reader:

- RadioTrail stations, 1, https://stream.0nlineradio.com/schlager?ref=crysta, various, The Russian Federation, 1 of 60313

### Step 5: Control+K

Now the want itself: the Seahawks game is on, and I do not know which station carries it. Control plus K, Keywords -- K for Keywords -- searches every field, on the row or off it: the name, the slogan, what the station says of itself, the notes from its record.

Screen reader:

- Keywords dialog, Keywords: edit

### Step 6: seahawks

I type seahawks, S E A H A W K S. No station is named that; it is in what one station says of itself.

Screen reader:

- seahawks

### Step 7: Enter

Enter.

Screen reader:

- RadioTrail stations, KIRO 710 ESPN Seattle, sports, news, The United States Of America, 22607 of 60313

### Step 8: Alt+Shift+P

Alt plus Shift plus P, Play Stream -- P for Play. The station's stream address goes to the Homer Player, the same player FileDir has. Mark several stations first, Control plus M, and they all queue.

Screen reader:

- 1 track from stations

### Step 9

The player opens on its track list, and the station begins. Scroll Lock pauses and resumes; its F1 lists the rest.

Screen reader:

- RadioTrail, Track list: list box, KIRO 710 ESPN Seattle, 1 of 1

### Step 10: Alt+Shift+R

Alt plus Shift plus R, Record -- R for Record -- keeps a copy of the stream as it arrives, in your Music folder under Homer Player; the same key, or the Stop recording button, stops it.

Screen reader:

- Recording KIRO 710 ESPN Seattle. Alt Shift R stops.

### Step 11: Escape

Escape closes the player and stops the sound, recording included. I am back on the row I left.

Screen reader:

- RadioTrail stations, KIRO 710 ESPN Seattle, sports, news, The United States Of America, 22607 of 60313

### Step 12: F4

Keeping it: Right Arrow to the status field is slow with this many columns, so F4 on the row opens the pick list for the cell I am on; I am on status.

Screen reader:

- status: list box, untried, 4 of 5

### Step 13: F

F, Foxtrot, for favorite.

Screen reader:

- favorite, 2 of 5

### Step 14: Enter

Enter sets it, saved at once. Rating, F2 on the rating cell, and notes, Shift plus N, work as in walk five; a refresh of the catalog never touches any of them.

Screen reader:

- favorite

### Step 15: Control+F

A second want: jazz, from anywhere. Keywords would jump to one station; Filter Records, which you know from walk six, narrows the list to all of them. Control plus F.

Screen reader:

- Filter Records dialog, name: edit

### Step 16: Tab

Tab to genre, and the word, with percent signs around it: contains jazz.

Screen reader:

- genre: edit

### Step 17: %jazz%

Percent jazz percent.

Screen reader:

- %jazz%

### Step 18: Control+Enter

Control plus Enter is OK, as in every dialog.

Screen reader:

- RadioTrail stations, filtered, 1 of 1200

### Step 19: Control+F

A third want: that jazz, but near home. Control plus F again -- the filter keeps what it has, and I add a field.

Screen reader:

- Filter Records dialog, name: edit

### Step 20: Tab

Tab past genre and country to state -- three Tabs; the boxes are in field order.

Screen reader:

- state: edit

### Step 21: Washington

Washington.

Screen reader:

- Washington

### Step 22: Control+Enter

Control plus Enter.

Screen reader:

- RadioTrail stations, filtered, 1 of 9

### Step 23: Shift+F

Shift plus F, Say Filter, as in walk six, reads the rule back: genre like jazz and state Washington. A city goes the same way, in the city box, or by Keywords when you are not sure which field holds it. Control plus Shift plus F would clear the filter.

Screen reader:

- genre like %jazz% and state = Washington

### Step 24

What this walk taught. I say the key; the reader says what it does.

### Step 25

Control plus K.

Screen reader:

- Keywords

### Step 26

Alt plus Shift plus P.

Screen reader:

- Play Stream

### Step 27

Control plus F.

Screen reader:

- Filter Records

**Something to try:** Find a station for your team or your town, play it, and mark it a favorite.

## 08 - Shared Code and the Homer Player

One want: a feature written once and heard in every program. The Homer Player heard in FileDir and then in DbDo, the same window from the same class; the other shared classes and what each does for the listener; what sharing buys and the one rule it costs; a misstep from this week. This walk assumes walks two and four.

**Before you start:** FileDir and DbDo are installed, with mpv.

### Step 1: Control+Shift+L

The want: a feature written once and heard in every program. The kit's shared code is where that happens, and the Homer Player is the plainest case. Here it is in FileDir, on a saved podcast page.

Screen reader:

- 4 tracks from Access On

### Step 2

The player opens on its track list, and the first track plays.

Screen reader:

- Access On, Track list: list box, Episode 212, 1 of 4

### Step 3: Alt+Shift+P

Now the same player in DbDo, on a radio station.

Screen reader:

- 1 track from stations
- RadioTrail, Track list: list box, KIRO 710 ESPN Seattle, 1 of 1

### Step 4

Scroll Lock pauses and resumes; the Volume and Rate sliders are ordinary sliders; Alt plus Shift plus R records a copy of the stream; Escape closes it. Learned once in FileDir, known in DbDo, because MediaPlayer dot cs lives in the kit and both programs compile it from there.

### Step 5

The other classes work the same way, less visibly. Lbc builds every dialog you heard in walk two. Say speaks every message, choosing the channel of whichever screen reader is running, or the Windows voice when none is, and never speaking over a keystroke. Log writes the session log in one format for every program.

### Step 6

Inix reads the settings files -- the dot inix format, sections and lines, with the comments the kit's rules ask for, so a settings file is documentation too. Web fetches with the same user agent and the same patience. Util holds the small things: the pluralizer, the short path, the version string.

### Step 7

Mpv dot cs finds the player engine, machine-wide, and drives it: play, pause, record, the sliders. Media dot cs knows what a track is -- a file, a stream, an episode -- and what to say about one.

### Step 8

What sharing buys. A fix to the player on Monday is in every program's next build; a new Say channel for a new reader reaches all of them; and a person who learned the dialog in one program has learned it in the next. The cost is one rule: an app never copies a kit class into its own folder; it compiles against the kit.

### Step 9

A planned misstep, from this very week. A kit class had a line that depended on one app, and compiled in that app alone; the kit's check now compiles every shared class on its own, so the mistake is caught in the kit and not in the fifth app to try it.

### Step 10

The Python side has the same shape: a Say, a Log, an Inix, a dialog builder, so a Python app -- HomerScribe is one -- speaks and logs and asks exactly as the C sharp ones do. Walk one's two fruit baskets were the proof.

### Step 11

Updating the kit is unarchiving HomerDev dot zip over the folder and building; every app's build checks the kit's version against the one it needs, and says so in one line when the kit is too old.

Screen reader:

- ERROR: FileDir needs HomerDev 1.52.8 or later, and C colon backslash HomerDev is 1.52.6.

### Step 12

What this walk taught. I say the idea or the key; the reader says the command or the name.

### Step 13

The player every Homer program shares.

Screen reader:

- MediaPlayer dot cs

### Step 14

The dialog class.

Screen reader:

- Lbc

### Step 15

The speech class.

Screen reader:

- Say

**Something to try:** Play something in FileDir and in DbDo, and notice the keys that are the same.

## 09 - Glossary

The words DbDo uses, in alphabetical order. I say the term; the reader says what it means. Each is one line, for looking up or for listening straight through.

**Before you start:** Nothing is needed.

### Step 1

action.

Screen reader:

- A step taken on a job -- a call, an application, an interview -- in JobTrail's actions table, linked to its job.

### Step 2

bulk marking.

Screen reader:

- The Edit submenu that marks or unmarks many records at once: all, none, or every record the filter shows.

### Step 3

catalog.

Screen reader:

- The list of radio stations RadioTrail fetches from Radio Browser, a public directory kept by volunteers.

### Step 4

cell.

Screen reader:

- One field of one record: where a row and a column meet. Shift plus C says it.

### Step 5

column.

Screen reader:

- One field, seen down the whole table. Select Columns chooses which columns a row speaks.

### Step 6

data folder.

Screen reader:

- Where your databases, settings and logs live: a DbDo folder under your local application data. Program Files holds only the program.

### Step 7

database.

Screen reader:

- One file holding one or more tables. Opening it opens a window inside DbDo.

### Step 8

editor.

Screen reader:

- The program that opens reports and exports: EdSharp when it is installed, otherwise the one Windows has for the file.

### Step 9

elevate.

Screen reader:

- Checking for a newer DbDo and installing it. F11, because elevate sounds like eleven.

### Step 10

field.

Screen reader:

- One named piece of a record, such as title, employer or stream address.

### Step 11

filter.

Screen reader:

- A rule that narrows the list to the records that match it, until it is cleared.

### Step 12

go to record.

Screen reader:

- Reaching a row by its number. Control plus G.

### Step 13

grid.

Screen reader:

- The rows and columns of a table, as the main window shows them.

### Step 14

Homer Player.

Screen reader:

- The player shared by the Homer Tools, which Play Stream opens: a track list, Scroll Lock to pause, Alt plus Shift plus R to record.

### Step 15

inspect.

Screen reader:

- Every field of the current record, one per line, in a window of its own. Control plus I.

### Step 16

jump.

Screen reader:

- Reaching a record by a word in one chosen field. Control plus J.

### Step 17

key describer.

Screen reader:

- A mode in which every key says what it does instead of doing it. Control plus F1 turns it on and off.

### Step 18

keywords.

Screen reader:

- A search across every field, on the row or off it. Control plus K; seahawks finds the station that carries the team, though no station is named that.

### Step 19

look.

Screen reader:

- The few fields that say what a record is, computed by the table, so the record can be named in one breath.

### Step 20

mark.

Screen reader:

- A tick on a record so that a command can act on several at once. Control plus M marks; Control plus Shift plus M unmarks.

### Step 21

order.

Screen reader:

- The sort of the list, by the field or fields you choose. Control plus O.

### Step 22

pick list.

Screen reader:

- The values a field offers, so a value is chosen and not typed. F4 opens it.

### Step 23

prime.

Screen reader:

- The field or fields that make a record unique, computed by the table, so two databases can be merged without doubling anything.

### Step 24

record.

Screen reader:

- One row of a table: one job, one book, one station.

### Step 25

related records.

Screen reader:

- The records in another table that belong to this one, reached by following the link.

### Step 26

report.

Screen reader:

- A document written from the table by a definition in report dot inix, and opened in your editor. Alt plus Shift plus R.

### Step 27

row.

Screen reader:

- What the reader says when you arrive on a record: the fields you chose to hear, in order.

### Step 28

say layer.

Screen reader:

- Every Shift plus letter: a question that changes nothing. Shift plus Z says the status.

### Step 29

select columns.

Screen reader:

- Choosing which fields a row speaks, and in what order. The rest stay in the record for searching and Inspect.

### Step 30

status.

Screen reader:

- The table, the row, and whether a filter or marks are in force; the bar at the bottom says it, and so does Shift plus Z.

### Step 31

stream.

Screen reader:

- A station's sound as it arrives over the Internet, played by the Homer Player. Alt plus Shift plus P.

### Step 32

table.

Screen reader:

- One kind of record, with its fields: jobs, actions, stations.

### Step 33

template.

Screen reader:

- A database that comes with DbDo, copied into your data folder the first time you open it, and yours from then on.

### Step 34

Thirty-three terms. The guide, F1, has each of them in context.

**Something to try:** Pick five terms you did not know and find each one in DbDo.

## 10 - Conclusion

What to carry away from the walks: four principles, one thing kept from each task walk, and where to begin. I say the idea; the reader says the key.

**Before you start:** Nothing is needed.

### Step 1

Four things to carry away, each with its key. A key is named for a word of its command, so it can be guessed.

### Step 2

Keywords, K.

Screen reader:

- Control plus K

### Step 3

Shift asks and never changes: what is in this cell?

Screen reader:

- Shift plus C

### Step 4

What is the state of the list -- the table, the filter, the marks?

Screen reader:

- Shift plus Z

### Step 5

What you write in a record is yours; a refresh of a template's catalog never takes it. And the dialog that adds a record is the dialog that edits it: OK from anywhere.

Screen reader:

- Control plus Enter

### Step 6

One thing kept from each task walk. Walk four: the grid is the grid, in every table and template; arrows, Home and End, a letter to jump.

### Step 7

Walk five: a record is added and edited in the one dialog, with F4 for a pick list, and Inspect reads the whole of it.

Screen reader:

- Control plus I

### Step 8

Walk six: Keywords jumps to a record by any word; Filter narrows the list to all of them; Order and Select Columns decide what you hear and in what order.

Screen reader:

- Control plus F

### Step 9

Walk seven: Related records follow the link between tables; marks gather a few; a report writes them out as a document in your editor.

Screen reader:

- Alt plus Shift plus R

### Step 10

Walk eight: sixty thousand stations are one more table; the station that carries your team is a word away, and Play Stream plays it.

Screen reader:

- Alt plus Shift plus P

### Step 11

Three habits that make the rest easy. Arrow a menu once a week: each item says its key. Press a Say key twice when speech went by too fast. And when a key is unknown, Control plus F1 and press it.

Screen reader:

- Key Describer

### Step 12

Where to begin. Start with one template that fits your life -- books, contacts, recipes, jobs, radio -- and add a record a day. The tables and the keys are the same in all of them, so the second template costs nothing to learn.

### Step 13

When DbDo is yours, make a database of your own: New Database, a table, a few fields; the dialog, the grid and the keys are the same as the templates', because the templates were made with them.

### Step 14

If something goes wrong, the session log under your local application data, with a line about what you were doing, is the fastest way to a fix; the project page on GitHub is where to send it.

### Step 15

Three things people do with DbDo every day, each in one breath. Log a job: Control plus N, the fields, Control plus Enter. Find the station carrying the game: Control plus K, the team, Alt plus Shift plus P. Send a counselor the month's record: mark the jobs, Alt plus Shift plus R, the Work Search Record.

### Step 16

And three things the other Homer programs share with it, so the second program costs nothing to learn: the dialog that works one way, the Say keys that change nothing, and the player that opens the same whether a file or a radio station is behind it.

**Something to try:** Open the template that fits you best and add your first record; then do one thing from each task walk in it.

## 11 - More Information

Where the rest is: the guide and history from inside DbDo, the documents, the GitHub page, updates, and the other Homer Tools.

**Before you start:** Nothing is needed.

### Step 1

F1 opens the guide, the whole of DbDo in one document, from inside the program. Shift plus F1 opens the history of changes. Alt plus F1 says the version.

### Step 2

The ReadMe is the short start; the guide is the reference; Hotkeys lists every key three ways. All three are in the help folder of the installation, and on the project's GitHub page.

### Step 3

F11 checks for a newer version and offers to install it. The project is at github dot com, slash JamalMazrui, slash DbDo, and its releases page holds every installer.

### Step 4

DbDo is one of the Homer Tools, free programs for working by ear: EdSharp for text, FileDir for files, DbDo for data. They share their keys and their player, so learning one is most of learning the next.

### Step 5

What this walk taught: the keys you heard.

**Something to try:** Press F1 and read the first section of the guide.

<!-- walkthrough ends -->

<!-- walkthrough: written by makeTutorials.py, do not edit between the markers -->

## 00 - Overview and Table of Contents

What DbDo is, in a paragraph; the two reader keys every walk assumes; then the table of contents, one line per walk.

**Before you start:** Nothing is needed; this walk is listened to.

### Step 1

DbDo is a database program for working by ear. A database is a file of tables; a table is records; a record is one job, one book, one radio station. You hear a row, you search every field, you narrow the list, you play what you find. Every key is named for a word of its command, and every dialog works one way.

### Step 2: Insert+UpArrow

Two keys before anything else, both the reader's own. If a line goes by too fast, Insert plus Up Arrow says it again.

Screen reader:

- (the last line, read a second time)

### Step 3: Insert+Tab

And if you lose your place, Insert plus Tab says where you are: the control, its state, its position, and any hint it carries. The walks are heard with those hints off, the way most people work.

Screen reader:

- (the current control, with its state and position)

### Step 4

Now the table of contents. I say the number and the title; the reader says what the walk covers.

### Step 5

One, Install and Launch.

Screen reader:

- The download, the installer's pages and boxes, and DbDo opening by itself.

### Step 6

Two, User Interface Concepts.

Screen reader:

- Windows inside a window, the grid, the row you hear and the record behind it, dialogs that all work one way, the status bar, and where help is.

### Step 7

Three, Key Patterns.

Screen reader:

- The rules every key follows, so a key can be guessed before it is learned, and the keys that explain the keys.

### Step 8

Four, Open and Move Through a Database.

Screen reader:

- JobTrail opened and arrowed through: rows, cells, the next table, and the menus.

### Step 9

Five, Add, Inspect and Edit Records.

Screen reader:

- A record added with its pick lists, looked at whole, and changed.

### Step 10

Six, Find, Order and Select.

Screen reader:

- Typing, Jump and Keywords; ordering and filtering the list; choosing which fields a row speaks.

### Step 11

Seven, Related, Marked, Reports and a Work Search Record.

Screen reader:

- Following links, marking a few, look and prime, running a report, and a whole task end to end.

### Step 12

Eight, RadioTrail, Find the Seahawks on the Air.

Screen reader:

- Sixty thousand stations, worked through three wants: the home team, jazz anywhere, jazz near home; playing, recording, keeping.

### Step 13

Nine, Glossary.

Screen reader:

- The words DbDo uses, in alphabetical order, one line each.

### Step 14

Ten, Conclusion.

Screen reader:

- Four sentences to carry away, and where to begin.

### Step 15

Eleven, More Information.

Screen reader:

- The guide and history from inside DbDo, the documents, the project page, updates, and the other Homer Tools.

### Step 16

Twelve walks, each under five minutes, a little over an hour together. They are a course, not a reference: each one assumes those before it.

**Something to try:** Listen to the walks in order; each one assumes the ones before it.

## 01 - Install and Launch

Installing DbDo: the security warning, the permission prompt, the options at the end, and the program opening by itself.

**Before you start:** The installer is downloaded, and your reader is running.

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

Enter presses Install, since Install is the button the wizard is offering.

Screen reader:

- Click Install to continue with the installation, or click Back if you want to review or change any settings.
- Install Button, Alt i

### Step 5: Enter

Enter again presses Install. The last page lists the extras. I arrow through them; Space ticks one.

Screen reader:

- Setup has finished installing DbDo on your computer.
- Tree view, Install scripts for improving use with the screen reader, checked, 1 of 6

A line for a reader appears only when you have that reader.

### Step 6: DownArrow

Down Arrow moves to the next option in the list.

Screen reader:

- Install the add-on for improving use with the screen reader, checked, 2 of 6

### Step 7: DownArrow

Ollama runs AI on my own computer. I want it, so I tick it.

Screen reader:

- Install Ollama 0.34.2, not checked, 3 of 6

### Step 8: Space

Spacebar ticks it, and the answer is one word.

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

Enter presses Finish, and DbDo opens by itself.

Screen reader:

- DbDo
- JobTrail dot d b, jobs
- Records list view
- Demo Data Cooperative (made up) Data Quality Specialist rejected, 1 of 4
- DbDo ready

Alt plus Control plus D starts DbDo from anywhere afterwards. D for DbDo.

### Step 11

What each box on the last page means. The screen reader scripts: Install when they are not there, Update when a newer set is available, Reinstall when they are current -- the word is the state, so you need not ask.

### Step 12

mpv is the player behind Play Stream; the box offers it, and Install, Update or Reinstall says its state the same way. Ollama is optional, for the AI features; the models are large, so it is unticked unless you want it.

### Step 13

The results box after Finish reports each item by name, one line each -- installed, updated, or already current -- and the same summary is saved in the logs folder under your local application data.

### Step 14

Where things went. The program is in Program Files; your databases, settings and logs are under your local application data, in a folder named DbDo. Nothing of yours is in Program Files, so an update never touches your data.

### Step 15: Alt+Control+D

Alt plus Control plus D opens DbDo from anywhere in Windows from now on, or brings it forward when it is already open; DbDo is one instance, so the key never opens a second copy.

Screen reader:

- DbDo

### Step 16: F11

F11, Elevate Version, is the installer's other half: it asks the web for a newer DbDo and offers to fetch and run it. Elevate sounds like eleven, which is how the key was chosen.

Screen reader:

- Elevate Version dialog

### Step 17: Escape

Escape. To remove DbDo later, Windows Settings, Apps; your data folder stays unless you delete it yourself.

Screen reader:

- DbDo

### Step 18

What this walk taught. I say the key; the reader says what it does.

### Step 19

Alt plus R.

Screen reader:

- Recent Files

**Something to try:** Open the log the results box names, and find where DbDo was installed.

## 02 - User Interface Concepts

What DbDo is made of: windows inside a window, the grid, the row you hear and the record behind it, prime and look, dialogs that all work one way, and the status bar.

**Before you start:** DbDo is open with any database. Nothing needs pressing in this walk; it is listened to.

### Step 1

DbDo is one window with its own windows inside: each database you open is a window of its own, and Control plus Tab moves between them.

### Step 2

A database window is a grid. Each row is one record; each column is one field. Down and Up Arrows move between records, Left and Right between fields, and the reader reads the row, then the cell.

### Step 3

The row reads only the fields you chose to hear -- three or four of them. The rest of the record is there for searching and for Inspect, Control plus I, which shows every field on its own line.

### Step 4

A table has a prime, the field or fields that make a record unique, and a look, the few fields that say what a record is. Both are computed, and both travel with the database.

### Step 5

Every dialog is built the same way: a label and its control, Tab between them, Alt plus the underlined letter to jump to one, Control plus Enter for OK from anywhere, Escape to cancel.

### Step 6

The status bar at the bottom says the table, the row, and whether a filter or marks are in force. Shift plus Z says it; Z is the bottom of the alphabet, like the bar is the bottom of the window.

### Step 7

Help is in four places, and they are the same in every Homer program. F1 opens the guide, the whole program in one document. Shift plus F1 opens the history of changes. Alt plus F1 says the version and offers the newer one if there is one.

### Step 8

The Help menu, F10 then H, Hotel, holds the same three, and Play Tutorials, which plays these walks. And the menus themselves are help: arrow through any menu and the reader says each command with its key and its letter.

### Step 9

A template is a database that comes with DbDo -- books, contacts, jobs, radio and more. The first time you open one, DbDo copies it into your data folder, and that copy is yours; the template itself is never changed.

### Step 10

One dialog adds a record and edits it: a label and a box for each field, pick lists where a field has fixed values, and Control plus Enter to save from anywhere in it. Learn it once for both.

### Step 11

Two states colour everything you hear. A filter narrows the list, and the status bar says filtered; marks tick records for a command to act on together, and the status bar counts them. Shift plus Z says both.

### Step 12

Results that are documents -- reports, exports -- do not open in DbDo. They open in your editor, EdSharp if you have it, so you read and keep them there.

### Step 13

Every Say key, pressed twice, shows the same words in a window you can arrow through and copy from, for when speech went by too fast or you want to keep what was said.

### Step 14

DbDo writes a log for every session, in the logs folder under your local application data; when something goes wrong, that file with a line about what you were doing is the fastest way to a fix.

### Step 15

Several databases can be open at once, each in its own window inside DbDo; Control plus Tab moves between them, and the title says which you are in. A table inside a database is reached with the Next Table and Prior Table keys, and the status bar names it.

### Step 16

Order is a state too: Order Records sorts the list by the field you choose, and a letter typed on the grid jumps within that order, so sorting by employer and typing a letter reaches an employer.

### Step 17

Pick lists do two jobs. In the record dialog, F4 opens a field's fixed values; on the grid, F4 on a cell does the same, so a status is changed without opening the record.

### Step 18

Related records are the link between tables: on a job, Related Records lists its actions; on an action, the job it belongs to. One key follows the link either way.

### Step 19

Everything in this walk is Windows underneath -- a list view, an edit box, a dialog, a status bar -- so your reader's own commands, Insert plus Up Arrow, Insert plus Tab, Insert plus Page Down, work on all of it. DbDo adds what is said and when.

### Step 20

What this walk taught: the keys you heard.

**Something to try:** Open a database and name each thing as you reach it: the window, the grid, the row, the cell, the status bar.

## 03 - Key Patterns

The rules every DbDo key follows, so a key can be guessed before it is learned: the word gives the letter, Control does, Shift asks, Shift reverses, Alt Shift is a command with no control, and the function keys follow Windows.

**Before you start:** DbDo is open with any database.

### Step 1

Every key in DbDo is named for a word in its command: Control plus K is Keywords, Control plus F is Filter, Control plus M is Mark. A key never comes from the middle of a word.

### Step 2

Control plus a letter does something. Shift plus a letter asks something, and changes nothing: Shift plus C says the cell, Shift plus S the selected columns, Shift plus F the filter, Shift plus Z the status.

### Step 3

Adding Shift to a Control key reverses it: Control plus M marks, Control plus Shift plus M unmarks; Control plus F filters, Control plus Shift plus F clears the filter.

### Step 4

Alt plus Shift plus a letter is a command with no control of its own: Alt plus Shift plus P plays a stream, Alt plus Shift plus R runs a report.

### Step 5

The function keys follow Windows and Office: F1 help, F2 edit, F3 find again, F4 pick from a list, F5 refresh, F10 the menus, F11 the version, F12 files.

### Step 6

Pressing a Say key twice shows the same words in a window you can arrow through and copy from, for when speech went by too fast.

### Step 7: Control+F1

The keys that explain the keys. Control plus F1 is the Key Describer: on, every key says what it does instead of doing it, the safe way to explore the keyboard. Hotkeys, in the Help menu, lists every key three ways; F1 opens the guide; and your reader's own Insert plus Tab says where you are.

Screen reader:

- Key Describer On

### Step 8: Control+F1

Control plus F1 again turns it off.

Screen reader:

- No Key Describer

### Step 9

The rules let a key be guessed. I say a command; the reader says the key the rules give it.

### Step 10

Order Records.

Screen reader:

- Alt plus O

### Step 11

Go to Record, by number.

Screen reader:

- Control plus G

### Step 12

Jump to Record, by a word in one field.

Screen reader:

- Control plus J

### Step 13

Delete Record.

Screen reader:

- Control plus D

### Step 14

Say Prime.

Screen reader:

- Shift plus P

### Step 15

Say Look.

Screen reader:

- Shift plus L

### Step 16

Clear Filter.

Screen reader:

- Control plus Shift plus F

### Step 17

Keys that are the reader's are not DbDo's: anything with Insert in it belongs to the screen reader, and DbDo never uses the Insert key, so the two can never collide.

### Step 18

In a dialog, Alt plus a letter jumps to a control, Control plus Enter is OK, Escape is Cancel. In a menu, the letter alone runs the item, and arrowing a menu says each item's key, which is a quiet way to learn them.

### Step 19

What this walk taught. I say the key; the reader says what it does.

### Step 20

Control plus F1.

Screen reader:

- Key Describer Toggle

**Something to try:** Guess the key for Order Records, Inspect and Clear Filter before you look them up, then check with Control plus F1.

## 04 - Open and Move Through a Database

Opening JobTrail, the first template, and moving through it: what a row says, what a cell says, the next table, and then the menus, which name every key as you arrow through them. This walk assumes walks one to three.

**Before you start:** DbDo is open and no database is open yet.

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

My reader's say-line key reads the whole row again. Shift plus Z, Say Status, gives the summary. H for Here.

Screen reader:

- status, jobs row 3 of 4, sort employer

### Step 6: Control+PageDown

Control plus Page Down moves to the next table. JobTrail gives me five: actions, contacts, docs, jobs and stories.

Screen reader:

- JobTrail dot d b, stories
- Records list view

Two more tables hold DbDo's own pick lists and links. It keeps those out of the way.

### Step 7: Alt

Alt opens the menu bar. Each menu's first letter opens it directly.

Screen reader:

- Menu bar
- File, F

File, Edit, Navigate, Query, Misc, Window, Help. Every letter differs.

### Step 8: DownArrow

Down Arrow opens File. Each item gives its key, then its letter.

Screen reader:

- New Database..., N

### Step 9: DownArrow

Down Arrow moves to the next item, and each one says its letter last.

Screen reader:

- Add Table..., A

### Step 10: DownArrow

Down Arrow again, and this one has a key as well as a letter.

Screen reader:

- Open Database..., Control+O, O

The key works from anywhere. The letter works while the menu is open. They match whenever the key has a letter.

### Step 11: Escape

A few menus hold a submenu for a large or rarely used group. Edit, B for Bulk Marking, is one. Right Arrow goes in; Left Arrow comes back.

Screen reader:

- Leaving menus

The others: Query, S for Say, Misc, T for Tools, and Help, M for More Documents. None goes deeper than one level.

### Step 12: Control+Tab

DbDo can keep several tables open, each in its own window -- the multiple document interface. Control plus Tab moves among DbDo windows.

Screen reader:

- JobTrail dot d b, actions
- Records list view

Control plus Shift plus T opens a table in a new window. Control plus F4 closes one.

### Step 13: Control+F1

F1 is help: the guide. Shift plus F1 is the history, Control plus F1 describes the next key I press.

Screen reader:

- Key Describer On

Help also holds the ReadMe, Hotkeys, the FAQ, and Play Tutorials.

### Step 14: F11

F11 is Elevate Version -- elevate sounds like eleven. It checks for a newer DbDo.

Screen reader:

- Elevate Version dialog

Nothing downloads without asking.

### Step 15

Home and End reach the first and last field of the row; Control plus Home and Control plus End the first and last record; Page Up and Page Down move a screen of records at a time.

### Step 16

Typing a letter jumps to the first record whose first column starts with it, in the current order; so a sorted list is reached by its first letters, as a file list is.

### Step 17: Control+G

Control plus G, Go to Record, takes a number, for when you know the row.

Screen reader:

- Go to Record dialog, Row: edit

### Step 18: Escape

Escape. Everything in this walk works the same in every table and every template: the grid is the grid.

Screen reader:

- JobTrail jobs

### Step 19

What this walk taught. I say the key; the reader says what it does.

### Step 20

Shift plus C.

Screen reader:

- Say Cell

### Step 21

Shift plus Z.

Screen reader:

- Say Status

### Step 22

Control plus Tab.

Screen reader:

- Next Window

### Step 23

Control plus F1.

Screen reader:

- Key Describer Toggle

**Something to try:** Open JobTrail, arrow through it, and read the File and Edit menus to the end.

## 04 - Start an App from the Kit

One want: a new program of your own, built on the kit, running by the end of the walk. newHomerApp, what it makes, the first build, the first run, the first change, and where the kit's classes come in. This walk assumes walks one to three.

**Before you start:** The kit is at C colon backslash HomerDev, and a command prompt is open there.

### Step 1: newHomerApp Recipes

The want: a new program of your own, built on the kit, running by the end of the walk. In the kit folder, newHomerApp with a name makes the folder and its files.

Screen reader:

- Made C colon backslash Recipes with 14 files. Run build there.

### Step 2

What is in it. The program file, Recipes dot cs, with the Homer classes referenced from the kit; build dot cmd, the stages every Homer build runs; the installer script with the shared finish page; accept dot inix, the acceptance checks; the twelve tutorial skeletons in help; the ReadMe, guide and history, each with its HTML pair; and the policy files that say which files travel to the repository.

### Step 3

The template program is the fruit basket's shape with the new name: a dialog with a field and a list, a report, F1 help on the fields, a session log. It runs before you have written a line, so every change is made against a working program.

### Step 4: build

Change into the folder and build.

Screen reader:

- Kit, C colon backslash HomerDev version 1.52.8
- Version, 1.0.0
- Built Recipes dot exe version 1.0.0

### Step 5: Recipes

Run it.

Screen reader:

- Recipes, the list is empty
- Name, edit

### Step 6: pancakes

Type a name and press Enter.

Screen reader:

- pancakes added, 1 item in the list

### Step 7: Alt+F4

Alt plus F4, and run it again: kept.

Screen reader:

- Recipes, 1 item in the list

### Step 8

Now the first real change. Open Recipes dot cs in EdSharp; the Camel Type conventions the kit's skill teaches are already followed in it: a prefix on every name says its type, functions in lower camel case, constants with c underscore. An AI assistant that has read the kit's skills writes the same way, so what it adds reads like what was there.

### Step 9

Where the Homer classes come in. Lbc builds every dialog; Say speaks; Log writes the session log; Inix reads settings; Web fetches; Util has the pluralizer that said one item and not one items. None of them is in your folder; they compile from the kit, so a kit update improves every app on the next build.

### Step 10

The Python template, newHomerApp with dash py, makes the same program in Python, built with the kit's Python modules; buildFruitBasketPy showed the shape in walk one: a build environment made, what the build needs installed, and an executable at the end.

Screen reader:

- Creating the build environment
- Installing what the build needs
- Built FruitBasketPy dot exe version 1.0.0

### Step 11

A planned misstep. Name the app with a space, and newHomerApp refuses in one line, because the name becomes a folder, a file, a class and a shortcut key.

Screen reader:

- A Homer app's name is one word in upper camel case, like FruitBasket.

### Step 12

From here the work is yours: fields in the dialog, commands on the menus with their keys named for their words, a template database or two. Walk five is what happens each time you type build.

### Step 13

What this walk taught. I say the idea or the key; the reader says the command or the name.

### Step 14

Make a new app from the template.

Screen reader:

- newHomerApp

### Step 15

Every stage, from encoding to installer.

Screen reader:

- build

**Something to try:** Make an app of your own with newHomerApp, build it, run it, and add one field to its dialog.

## 05 - Add, Inspect and Edit Records

Adding a record with its pick lists, looking at one record whole, and changing a value and checking what was saved.

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

Typing a letter jumps to the first value starting with it, so L reaches lead. That is L, Lima.

Screen reader:

- lead, 4 of 7

### Step 6: Control+Enter

Enter takes it. Control plus Enter saves the record from anywhere in the dialog.

Screen reader:

- Records list view
- Widget Works (your own entry) Support Analyst lead, 5 of 5

### Step 7: Control+I

A row speaks three fields. Control plus I, Inspect Record, reads them all.

Screen reader:

- Inspect Record dialog
- employer: Example Widgets Company (sample employer)

I for Inspect. A read-only window; Escape closes it.

### Step 8: Escape

For one fact, I ask instead. Shift plus N, Say Notes.

Screen reader:

- Records list view

### Step 9: Shift+N

Shift plus N is Say Notes -- N for Notes -- and reads the whole note.

Screen reader:

- notes, Illustrative record. Interview booked for the 24th; ask about screen reader testing tools.

Shift plus T, Say Tags, and Shift plus U, Say URL, work the same way.

### Step 10: Shift+E

Shift plus E, Say Edited: when I last changed it.

Screen reader:

- edited, September 19, 2026 at 10:01 PM

Shift plus A, Say Added, says when it arrived.

### Step 11: Alt+Shift+N

Alt plus Shift plus N edits the notes in their own window, for a long one.

Screen reader:

- Notes dialog
- Notes edit, multiline, Illustrative record. Interview booked for the 24th.

Control plus Enter saves; Escape leaves.

### Step 12: Enter

Enter opens the record in the same dialog I used to add one.

Screen reader:

- Edit Record dialog
- Employer edit, Example Widgets Company (sample employer)

### Step 13: o

Tab to status, F4 for the list, O for offer. That is O, Oscar.

Screen reader:

- offer, 5 of 7

### Step 14: Control+Enter

Control plus Enter saves, the way it presses OK in any DbDo dialog.

Screen reader:

- Records list view
- Example Widgets Company (sample employer) Accessibility Analyst offer, 2 of 4

### Step 15: F2

For one field, F2 edits the cell in place -- the same F2 that renames a file in Windows.

Screen reader:

- Cell edit, offer

Enter saves the cell and keeps my place.

### Step 16: Escape

Shift plus C checks it.

Screen reader:

- Records list view

### Step 17: Shift+C

Shift plus C is Say Cell: the column, where the row is, and the value.

Screen reader:

- status
- row 2 of 4
- offer

### Step 18: Control+Shift+C

Control plus Shift plus C, Copy Record, duplicates the current row as a new one, for a job much like the last; then edit the few fields that differ.

Screen reader:

- Record copied

### Step 19: Control+D

Control plus D, Delete Record, removes the current record -- or every marked record when marks exist -- after asking once.

Screen reader:

- Delete this record? Yes button

### Step 20: Escape

Escape keeps it. The question is the only confirmation DbDo asks; everything else is saved as you go.

Screen reader:

- JobTrail jobs

### Step 21

What this walk taught. I say the key; the reader says what it does.

### Step 22

Control plus N.

Screen reader:

- New Record

### Step 23

F4.

Screen reader:

- Current Windows

### Step 24

Control plus Enter.

Screen reader:

- Open Cell Value

### Step 25

Control plus I.

Screen reader:

- Inspect Record

**Something to try:** Add a job of your own, inspect it, then change one field.

## 05 - Build, Check and Release

One want: a change made this morning, published by lunch, with nothing shipped that does not work. What build does, stage by stage; what check refuses, with the misstep everybody meets; what release publishes and what it leaves behind. This walk assumes walk four.

**Before you start:** An app built on the kit is open in a command prompt, with a change made and GitHub signed in.

### Step 1: build

The want: a change made this morning, published by lunch, with nothing shipped that does not work. Three commands do it, and each refuses when it should. First, build, in the app's folder.

Screen reader:

- Kit, C colon backslash HomerDev version 1.52.8
- Version, 1.0.228 (bumped)

### Step 2

The stages, in order, each logged. The kit's version is checked against what the app needs. Old file names are retired. The encoding of every file is put right: UTF-8 with a byte order mark, CRLF line endings. The hotkey list is generated from the program itself. Any tutorial whose audio is missing or older than its script is spoken. The program compiles. The installer is built.

### Step 3

Each stage says one line and writes the rest to the log. Here is the line that matters most.

Screen reader:

- Built DbDo underscore setup dot exe version 1.0.228

### Step 4: scripts\check

Second, check. It is also the first thing release runs, so you may skip it; but it is a few seconds, and it says exactly what release would refuse.

Screen reader:

- 13 checks passed, 0 checks failed, 2 checks not checked.

### Step 5

What the checks are. Every file in the Homer encoding. Every access letter unique within its dialog or menu. No key on a reader's key. The app's files under the Local tree, never Roaming. The hotkey list current. The tutorials clean. Each acceptance check in accept dot inix passing. And an evidence report written in Markdown, for anyone who asks what was checked.

### Step 6

A planned misstep, the one everybody meets. Change a dialog and reuse a letter, and check names the dialog and the two captions.

Screen reader:

- 12 checks passed, 1 check failed. failed: access letters -- Filter Records: F is used by Find and Filter

### Step 7: scripts\release

Third, release. It reads the version from the installer, refuses if the last build did not succeed, runs check, commits and pushes, tags the version, and publishes the installer on GitHub.

Screen reader:

- GitHub has no published release tagged v1.0.228.
- DbDo 1.0.228 published.

### Step 8

Release refuses for honest reasons and says which. A build still running: the last build did not succeed, build again, then release. A check failing: nothing was published, its report names it. And it never publishes an installer older than the source beside it.

### Step 9

What the release left behind. A tag, a release page with the installer, and in the logs folder a release log and an evidence report; the ReadMe's download link now points at the new installer. F11 in any installed copy finds it within the hour.

### Step 10

The logs. Every stage wrote its own: build, encoding, hotkeys, tutorials, check, push, release. The console said one line each; the logs say every command and its exit code. When something is wrong, the log is the thing to send -- never a description of the console.

### Step 11

A second planned misstep. Run release before the build has finished speaking its tutorials, and it refuses -- the build has not succeeded yet -- which is right, if blunt; wait for the Built line, then release.

### Step 12

What this walk taught. I say the idea or the key; the reader says the command or the name.

### Step 13

Every stage, encoding to installer.

Screen reader:

- build

### Step 14

What release would refuse, in seconds.

Screen reader:

- check

### Step 15

Tag, publish, and leave a log.

Screen reader:

- release

**Something to try:** Make a small change in an app of your own, then build, check and release it.

## 06 - Find, Order and Select

Reaching a record by typing and by Keywords; ordering the list and narrowing it with Filter Records; choosing which fields a row speaks.

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

Enter takes me to the record it found.

Screen reader:

- Illustration City Library (fictional) Digital Services Assistant lead, 3 of 4

### Step 4: Control+K

Control plus K, Keywords -- K for Keywords. It searches every field, notes included, which is why it is not called Find.

Screen reader:

- Keywords dialog
- Text combo box, blank, ALT+T

### Step 5: Enter

Enter again, and this is the second match.

Screen reader:

- Sample Health Network (illustration only) Records Coordinator applied, 4 of 4

F3 searches again. Shift reverses: Shift plus F3 searches back, Control plus Shift plus F finds backwards.

### Step 6: Shift+F

Shift plus K, Say Keywords, reminds me what I looked for.

Screen reader:

- find, screen reader

### Step 7: Alt+O

Alt plus O, Order -- O for Order. Control plus O is Open everywhere, so Order takes Alt.

Screen reader:

- Order Records dialog
- Column to sort by list box, employer, 4 of 16, ALT+C

### Step 8: s

Typing S jumps to the first field starting with it. That is S, Sierra.

Screen reader:

- status, 13 of 16

### Step 9: Enter

Enter sorts the list, and the reader reads the row I land on.

Screen reader:

- Records list view
- Sample Health Network (illustration only) Records Coordinator applied, 1 of 4

Shift plus O, Say Order, says it later.

### Step 10: Control+F

Control plus F, Filter Records -- F for Filter. It decides which rows I hear, and it writes the condition for me: one box per field, and a symbol in front of a value to compare instead of match.

Screen reader:

- Filter Records dialog
- Filter combo box, blank, ALT+F

### Step 11: Enter

I want what is still moving: status not rejected.

Screen reader:

- Records list view
- Example Widgets Company (sample employer) Accessibility Analyst interviewing, 1 of 3

Shift plus F, Say Filter, and Shift plus Y, Say Yield -- how many rows it left.

### Step 12: Control+Shift+F

Control plus Shift plus F clears it. Adding Shift reverses.

Screen reader:

- Records list view
- Demo Data Cooperative (made up) Data Quality Specialist rejected, 1 of 4

### Step 13: Shift+S

Shift plus S, Say Select, names the fields each row speaks.

Screen reader:

- select, employer, title, status

### Step 14: Alt+S

Alt plus S, Select Columns -- Control plus S saves, so Select takes Alt.

Screen reader:

- Select Columns dialog
- Columns check list box, employer check box checked, 1 of 16, ALT+C

### Step 15: Space

I add the date I applied. Space ticks it.

Screen reader:

- applied underscore date check box checked, 11 of 16

### Step 16: Enter

Enter applies the choice, and the row is shorter from here on.

Screen reader:

- Records list view
- Example Widgets Company (sample employer) Accessibility Analyst interviewing 2026-09-02, 2 of 4

Three or four fields is the useful range. Every row is heard, so every field costs time.

### Step 17

What this walk taught. I say the key; the reader says what it does.

### Step 18

Control plus J.

Screen reader:

- Jump to Record

### Step 19

Control plus K.

Screen reader:

- Keywords

### Step 20

Shift plus F.

Screen reader:

- Say Filter

### Step 21

Alt plus O.

Screen reader:

- Order Records

**Something to try:** Find a record three ways, then change the sort and the columns and listen to the difference.

## 06 - The Installer and Its Finish Page

One want: a program that installs itself and its helpers in one run and says what it did. The shared installer parts heard in DbDo's installer: boxes that say their state in one word, the results box, the summary, and the ten lines an app writes to get all of it. This walk assumes walk five.

**Before you start:** A Homer program's installer is downloaded, and your reader is running.

### Step 1

The want: a program that installs itself and its helpers in one run, and tells you what it did. The kit's HomerComponents dot iss is the shared part of every Homer installer: the finish page, its boxes, their states, and the results. Here is DbDo's.

### Step 2: Alt+R

The installer is downloaded; Enter opens it, and Windows asks because it came from the internet. Alt plus R, Run; then Alt plus Y, Yes, for administrator rights, because a Homer program installs for everyone.

Screen reader:

- User Account Control dialog

### Step 3: Enter

The pages are a wizard: the folder, then Install. Enter presses the default button on each. The page that asks a decision is the last.

Screen reader:

- Setup, Finish page

### Step 4: DownArrow

The finish page lists the optional pieces as boxes. Arrow through them; each says its name, its state, and its size.

Screen reader:

- Update screen reader scripts checked, 1 of 6

### Step 5

The three words, from the kit. Install when the piece is not there. Update when it is there and a newer version is available -- the installer asked winget. Reinstall when it is there and current, unticked unless you want it. You never have to know what is on your machine; the box says.

### Step 6: DownArrow

Down Arrow to the player.

Screen reader:

- Install mpv checked, 2 of 6

### Step 7: Space

Ollama runs AI on your own computer. Its models are large, so it is unticked unless you tick it; Spacebar changes a box, and the answer is one word.

Screen reader:

- checked

### Step 8: Enter

Launch is ticked already. Enter presses Finish, and whatever was ticked installs now; a results box then says what was done, one line per piece.

Screen reader:

- DbDo Setup, screen reader scripts: updated. mpv: installed. Ollama: installed.

### Step 9

The same summary is saved in the logs folder under your local application data, and summarizeSetup, in the program folder, shows it again on any later day.

### Step 10

What the app's installer script writes, and what the kit writes. The app names its pieces in a table -- a name, a winget id, an executable to look for, what it is used for -- and the kit does the rest: the probing, the three states, the labels, the ordering, the results. Ten lines of the app's, for a page that behaves the same in every Homer program.

### Step 11

Screen reader scripts are a piece of their own: the kit knows where each reader keeps its settings or its add-ons, and the box reads Install, Update or Reinstall for them as for the rest.

### Step 12

A planned misstep. Run the installer a second time: every piece already present says so, and Install becomes Reinstall, unticked. Nothing is installed twice.

### Step 13

Removing a Homer program is Windows Settings, Apps; the pieces it installed are programs in their own right and stay for the other Homer programs, and your data folder stays unless you delete it.

### Step 14

What this walk taught. I say the idea or the key; the reader says the command or the name.

### Step 15

The one word that is the box's state.

Screen reader:

- Install, Update, or Reinstall

### Step 16

The shared installer parts.

Screen reader:

- HomerComponents dot iss

**Something to try:** Run a Homer installer, arrow its finish page, and read the results box.

## 07 - Related, Marked, Reports and a Work Search Record

Following a record to the records that belong to it; marking a few to work with; what look and prime are; getting records out as a report; and a whole task end to end, the work search record a counselor asks for. This walk assumes walks four to six.

**Before you start:** JobTrail is open on the jobs table.

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

### Step 4: Control+M

Control plus M, Mark -- M for Mark.

Screen reader:

- Marked row 2

### Step 5: Control+Shift+M

Control plus Shift plus M unmarks. Adding Shift reverses.

Screen reader:

- Unmarked row 2

Shift plus M, Say Mark, asks without changing anything.

### Step 6: Control+DownArrow

Control plus Down Arrow steps to the next marked record.

Screen reader:

- Sample Health Network (illustration only) Records Coordinator applied, 4 of 4

### Step 7: Shift+Space

Shift plus Space counts them.

Screen reader:

- 2 marked rows: Example Widgets Company (sample employer), Sample Health Network (illustration only)

### Step 8: Control+A

For runs of rows: Edit, B for Bulk Marking, then A for Mark All.

Screen reader:

- Marked 4 rows

Control plus A is the key for Mark All; Control plus Shift plus A unmarks all.

### Step 9: Shift+L

Every table has two columns nobody types into. Shift plus L, Say Look.

Screen reader:

- look, Example Widgets Company (sample employer) | Accessibility Analyst | interviewing

Look is the record at a glance. It is what Say Related shows for a linked record.

### Step 10: Shift+P

Shift plus P, Say Prime: the fields that make the record unique.

Screen reader:

- prime, Example Widgets Company (sample employer)|Accessibility Analyst

Employer and title. Two jobs with both the same would be one job.

### Step 11: Control+Shift+C

To tell a friend about a job: Control plus Shift plus C copies the record. C for Copy; Control plus C alone copies the cell.

Screen reader:

- Row copied to clipboard

Labelled lines, ready to paste.

### Step 12: Alt+Shift+R

For a prepared document: File, R for Run Report. Its key is Alt plus Shift plus R.

Screen reader:

- Run Report dialog
- Output list box, application underscore history, 1 of 6, ALT+O

### Step 13: Enter

Enter opens the report in the editor.

Screen reader:

- EdSharp, application underscore history dot m d

### Step 14: Shift+Z

My counselor asks what I did, when, with whom, and what came of it. The actions table holds exactly that.

Screen reader:

- status, actions row 1 of 7, sort action date descending

### Step 15: Shift+C

Three fields matter to the agency: method, evidence, and counts -- whether it was an employer contact.

Screen reader:

- method
- row 1 of 7
- internet

### Step 16: Alt+Shift+R

File, R for Run Report.

Screen reader:

- Run Report dialog
- Output list box, application underscore history, 1 of 6, ALT+O

### Step 17: w

Typing W jumps to the report I want. That is W, Whiskey.

Screen reader:

- work underscore search underscore record, 6 of 6

### Step 18: Enter

Enter runs it, and the editor opens with the result.

Screen reader:

- EdSharp, work underscore search underscore record dot m d

Countable contacts first, with weekly totals; everything else after.

### Step 19

What this walk taught. I say the key; the reader says what it does.

### Step 20

Shift plus R.

Screen reader:

- Say Related

### Step 21

Alt plus RightArrow.

Screen reader:

- Enter Child Table

### Step 22

Control plus M.

Screen reader:

- Mark Record

### Step 23

Control plus Shift plus M.

Screen reader:

- Unmark Record

**Something to try:** Mark three jobs, run the Work Search Record report, and read what it made.

## 07 - Spoken Tutorials

One want: a spoken tutorial for your app, in two voices, without a microphone. The walk file and its four beats, the two voices and what they never do, the pattern of twelve, three to five minutes, the checker and its misstep, the tool that speaks and measures, and where the rules came from. This walk assumes walk five.

**Before you start:** An app built on the kit, with its help folder open.

### Step 1

The want: a spoken tutorial for your app, in the two voices, without a microphone. A walk is a text file in help, Tutorial underscore, a number, a title with underscores, dot inix. The build speaks it.

### Step 2

The format has four beats per key. Say: the host says the key with the word it comes from. Key: the key pressed. Hear: what the reader says, word for word, as it would be spoken -- a file name as said, not as written. Say again: what that meant. Name silence when a key says nothing. Name no screen reader.

### Step 3

Two voices, because a program has two: the person, and the reader answering. The exchange is the teaching device, and it is used for more than keystrokes: a glossary is the host saying the term and the reader saying the meaning; a recap is the host saying the key and the reader saying the command. What the voices never do is chat.

### Step 4

The pattern of twelve, the same for every app. Zero, the overview and contents. One, install and launch. Two, the interface. Three, the key rules. Four to eight, up to five tasks, each built around a plain want. Nine, the glossary. Ten, the conclusion. Eleven, more information, always last.

### Step 5

Three to five minutes for parts one to ten: under three is too thin to repay the listener's start; over five loses them. A thin walk gets substance -- the adjacent thing the want needs, a planned misstep and its recovery -- never padding. The tool measures the audio and says what runs under or over.

### Step 6: scripts\checkTutorial

checkTutorial reads every walk before anything is spoken. It wants Intro and Setup, a Say in every step, Hear lines in words, no reader named, the first walk teaching the two reader keys, and the pattern's names and numbers; a set not yet the pattern is a notice, not a silence.

Screen reader:

- 12 scripts checked, 0 problems.

### Step 7

A planned misstep. Write a Hear line with a plus sign in it, or a file name as written, and the checker names the step.

Screen reader:

- Tutorial underscore 17, step 6: Hear writes an access key with a plus sign; the reader says the words

### Step 8: scripts\buildTutorials -build

buildTutorials speaks them. Two voices: Kokoro, with a narrator and a reader, both licensed to redistribute; Piper when Kokoro is not there. The reader's voice shifts by context -- a cursor line, a message, a menu -- so a message is heard as a message.

Screen reader:

- Creating 04 underscore Open underscore and underscore Move dot mp3, 23 steps. A few minutes.

### Step 9

The audio is named like a chapter -- 04 underscore Open underscore and underscore Move dot mp3 -- so a folder or a player shows the number and the title. A walk whose audio is older than its script is spoken again; the rest are kept.

### Step 10

At the end, the lengths, and the two lines that matter.

Screen reader:

- 04 underscore Open underscore and underscore Move dot mp3 runs 2 33
- Wrote Tutorials dot m3u naming 12 tutorials, 27 minutes in all.

### Step 11

Under three minutes or over five, the tool says so by name, with the guideline in one line. Tutorials dot m3u is the playlist Play Tutorials uses; Tutorials dot md is the transcript, made from the same scripts; TutorialFeed dot xml is a podcast feed of the same audio.

### Step 12

Two kinds of listener. A walk is written for the person who will press the keys tomorrow; it is also what an AI assistant reads to learn the program, which is why the skills folder points at it.

### Step 13

The learning behind the rules came from ninety-eight recorded screen reader training sessions and from Quill Radio's tutorials: spell a lone letter with its alphabet word; one planned misstep per walk; close by naming the two or three keys taught; a concrete want before a feature. TutorialLearnings dot md in the kit's help has the rest.

### Step 14

What this walk taught. I say the idea or the key; the reader says the command or the name.

### Step 15

Reads every walk before anything is spoken.

Screen reader:

- checkTutorial

### Step 16

Speaks what is missing or stale and measures it.

Screen reader:

- buildTutorials

### Step 17

The playlist Play Tutorials uses.

Screen reader:

- Tutorials dot m3u

**Something to try:** Write one task walk for your app, run checkTutorial, and let the build speak it.

## 08 - RadioTrail: Find the Seahawks on the Air

The Internet radio directory that comes with DbDo, worked through three wants: the station carrying the Seattle Seahawks, found by a word it says of itself and played, recorded and kept; jazz from anywhere, by filtering on genre; and jazz near home, by adding the state. This walk assumes walks four to seven.

**Before you start:** DbDo is open, fetchStations has been run once, and no database is open yet.

### Step 1: Alt+F

You opened a template in walk four. The same way: Alt plus F, then T, Tango.

Screen reader:

- File menu

### Step 2: T

T, Tango.

Screen reader:

- Template Databases dialog, Choose a template database, list box, BookTrail, 1 of 14

### Step 3: R

R, Romeo, jumps to the first template starting with R.

Screen reader:

- RadioTrail, 11 of 14

### Step 4: Enter

Enter opens it. The first time, DbDo makes your own copy in your data folder; that copy is the one that fills with stations.

Screen reader:

- RadioTrail stations, 1, https://stream.0nlineradio.com/schlager?ref=crysta, various, The Russian Federation, 1 of 60313

### Step 5: Control+K

Now the want itself: the Seahawks game is on, and I do not know which station carries it. Control plus K, Keywords -- K for Keywords -- searches every field, on the row or off it: the name, the slogan, what the station says of itself, the notes from its record.

Screen reader:

- Keywords dialog, Keywords: edit

### Step 6: seahawks

I type seahawks, S E A H A W K S. No station is named that; it is in what one station says of itself.

Screen reader:

- seahawks

### Step 7: Enter

Enter.

Screen reader:

- RadioTrail stations, KIRO 710 ESPN Seattle, sports, news, The United States Of America, 22607 of 60313

### Step 8: Alt+Shift+P

Alt plus Shift plus P, Play Stream -- P for Play. The station's stream address goes to the Homer Player, the same player FileDir has. Mark several stations first, Control plus M, and they all queue.

Screen reader:

- 1 track from stations

### Step 9

The player opens on its track list, and the station begins. Scroll Lock pauses and resumes; its F1 lists the rest.

Screen reader:

- RadioTrail, Track list: list box, KIRO 710 ESPN Seattle, 1 of 1

### Step 10: Alt+Shift+R

Alt plus Shift plus R, Record -- R for Record -- keeps a copy of the stream as it arrives, in your Music folder under Homer Player; the same key, or the Stop recording button, stops it.

Screen reader:

- Recording KIRO 710 ESPN Seattle. Alt Shift R stops.

### Step 11: Escape

Escape closes the player and stops the sound, recording included. I am back on the row I left.

Screen reader:

- RadioTrail stations, KIRO 710 ESPN Seattle, sports, news, The United States Of America, 22607 of 60313

### Step 12: F4

Keeping it: Right Arrow to the status field is slow with this many columns, so F4 on the row opens the pick list for the cell I am on; I am on status.

Screen reader:

- status: list box, untried, 4 of 5

### Step 13: F

F, Foxtrot, for favorite.

Screen reader:

- favorite, 2 of 5

### Step 14: Enter

Enter sets it, saved at once. Rating, F2 on the rating cell, and notes, Shift plus N, work as in walk five; a refresh of the catalog never touches any of them.

Screen reader:

- favorite

### Step 15: Control+F

A second want: jazz, from anywhere. Keywords would jump to one station; Filter Records, which you know from walk six, narrows the list to all of them. Control plus F.

Screen reader:

- Filter Records dialog, name: edit

### Step 16: Tab

Tab to genre, and the word, with percent signs around it: contains jazz.

Screen reader:

- genre: edit

### Step 17: %jazz%

Percent jazz percent.

Screen reader:

- %jazz%

### Step 18: Control+Enter

Control plus Enter is OK, as in every dialog.

Screen reader:

- RadioTrail stations, filtered, 1 of 1200

### Step 19: Control+F

A third want: that jazz, but near home. Control plus F again -- the filter keeps what it has, and I add a field.

Screen reader:

- Filter Records dialog, name: edit

### Step 20: Tab

Tab past genre and country to state -- three Tabs; the boxes are in field order.

Screen reader:

- state: edit

### Step 21: Washington

Washington.

Screen reader:

- Washington

### Step 22: Control+Enter

Control plus Enter.

Screen reader:

- RadioTrail stations, filtered, 1 of 9

### Step 23: Shift+F

Shift plus F, Say Filter, as in walk six, reads the rule back: genre like jazz and state Washington. A city goes the same way, in the city box, or by Keywords when you are not sure which field holds it. Control plus Shift plus F would clear the filter.

Screen reader:

- genre like %jazz% and state = Washington

### Step 24

What this walk taught. I say the key; the reader says what it does.

### Step 25

Control plus K.

Screen reader:

- Keywords

### Step 26

Alt plus Shift plus P.

Screen reader:

- Play Stream

### Step 27

Control plus F.

Screen reader:

- Filter Records

**Something to try:** Find a station for your team or your town, play it, and mark it a favorite.

## 08 - Shared Code and the Homer Player

One want: a feature written once and heard in every program. The Homer Player heard in FileDir and then in DbDo, the same window from the same class; the other shared classes and what each does for the listener; what sharing buys and the one rule it costs; a misstep from this week. This walk assumes walks two and four.

**Before you start:** FileDir and DbDo are installed, with mpv.

### Step 1: Control+Shift+L

The want: a feature written once and heard in every program. The kit's shared code is where that happens, and the Homer Player is the plainest case. Here it is in FileDir, on a saved podcast page.

Screen reader:

- 4 tracks from Access On

### Step 2

The player opens on its track list, and the first track plays.

Screen reader:

- Access On, Track list: list box, Episode 212, 1 of 4

### Step 3: Alt+Shift+P

Now the same player in DbDo, on a radio station.

Screen reader:

- 1 track from stations
- RadioTrail, Track list: list box, KIRO 710 ESPN Seattle, 1 of 1

### Step 4

Scroll Lock pauses and resumes; the Volume and Rate sliders are ordinary sliders; Alt plus Shift plus R records a copy of the stream; Escape closes it. Learned once in FileDir, known in DbDo, because MediaPlayer dot cs lives in the kit and both programs compile it from there.

### Step 5

The other classes work the same way, less visibly. Lbc builds every dialog you heard in walk two. Say speaks every message, choosing the channel of whichever screen reader is running, or the Windows voice when none is, and never speaking over a keystroke. Log writes the session log in one format for every program.

### Step 6

Inix reads the settings files -- the dot inix format, sections and lines, with the comments the kit's rules ask for, so a settings file is documentation too. Web fetches with the same user agent and the same patience. Util holds the small things: the pluralizer, the short path, the version string.

### Step 7

Mpv dot cs finds the player engine, machine-wide, and drives it: play, pause, record, the sliders. Media dot cs knows what a track is -- a file, a stream, an episode -- and what to say about one.

### Step 8

What sharing buys. A fix to the player on Monday is in every program's next build; a new Say channel for a new reader reaches all of them; and a person who learned the dialog in one program has learned it in the next. The cost is one rule: an app never copies a kit class into its own folder; it compiles against the kit.

### Step 9

A planned misstep, from this very week. A kit class had a line that depended on one app, and compiled in that app alone; the kit's check now compiles every shared class on its own, so the mistake is caught in the kit and not in the fifth app to try it.

### Step 10

The Python side has the same shape: a Say, a Log, an Inix, a dialog builder, so a Python app -- HomerScribe is one -- speaks and logs and asks exactly as the C sharp ones do. Walk one's two fruit baskets were the proof.

### Step 11

Updating the kit is unarchiving HomerDev dot zip over the folder and building; every app's build checks the kit's version against the one it needs, and says so in one line when the kit is too old.

Screen reader:

- ERROR: FileDir needs HomerDev 1.52.8 or later, and C colon backslash HomerDev is 1.52.6.

### Step 12

What this walk taught. I say the idea or the key; the reader says the command or the name.

### Step 13

The player every Homer program shares.

Screen reader:

- MediaPlayer dot cs

### Step 14

The dialog class.

Screen reader:

- Lbc

### Step 15

The speech class.

Screen reader:

- Say

**Something to try:** Play something in FileDir and in DbDo, and notice the keys that are the same.

## 09 - Glossary

The words DbDo uses, in alphabetical order. I say the term; the reader says what it means. Each is one line, for looking up or for listening straight through.

**Before you start:** Nothing is needed.

### Step 1

action.

Screen reader:

- A step taken on a job -- a call, an application, an interview -- in JobTrail's actions table, linked to its job.

### Step 2

bulk marking.

Screen reader:

- The Edit submenu that marks or unmarks many records at once: all, none, or every record the filter shows.

### Step 3

catalog.

Screen reader:

- The list of radio stations RadioTrail fetches from Radio Browser, a public directory kept by volunteers.

### Step 4

cell.

Screen reader:

- One field of one record: where a row and a column meet. Shift plus C says it.

### Step 5

column.

Screen reader:

- One field, seen down the whole table. Select Columns chooses which columns a row speaks.

### Step 6

data folder.

Screen reader:

- Where your databases, settings and logs live: a DbDo folder under your local application data. Program Files holds only the program.

### Step 7

database.

Screen reader:

- One file holding one or more tables. Opening it opens a window inside DbDo.

### Step 8

editor.

Screen reader:

- The program that opens reports and exports: EdSharp when it is installed, otherwise the one Windows has for the file.

### Step 9

elevate.

Screen reader:

- Checking for a newer DbDo and installing it. F11, because elevate sounds like eleven.

### Step 10

field.

Screen reader:

- One named piece of a record, such as title, employer or stream address.

### Step 11

filter.

Screen reader:

- A rule that narrows the list to the records that match it, until it is cleared.

### Step 12

go to record.

Screen reader:

- Reaching a row by its number. Control plus G.

### Step 13

grid.

Screen reader:

- The rows and columns of a table, as the main window shows them.

### Step 14

Homer Player.

Screen reader:

- The player shared by the Homer Tools, which Play Stream opens: a track list, Scroll Lock to pause, Alt plus Shift plus R to record.

### Step 15

inspect.

Screen reader:

- Every field of the current record, one per line, in a window of its own. Control plus I.

### Step 16

jump.

Screen reader:

- Reaching a record by a word in one chosen field. Control plus J.

### Step 17

key describer.

Screen reader:

- A mode in which every key says what it does instead of doing it. Control plus F1 turns it on and off.

### Step 18

keywords.

Screen reader:

- A search across every field, on the row or off it. Control plus K; seahawks finds the station that carries the team, though no station is named that.

### Step 19

look.

Screen reader:

- The few fields that say what a record is, computed by the table, so the record can be named in one breath.

### Step 20

mark.

Screen reader:

- A tick on a record so that a command can act on several at once. Control plus M marks; Control plus Shift plus M unmarks.

### Step 21

order.

Screen reader:

- The sort of the list, by the field or fields you choose. Control plus O.

### Step 22

pick list.

Screen reader:

- The values a field offers, so a value is chosen and not typed. F4 opens it.

### Step 23

prime.

Screen reader:

- The field or fields that make a record unique, computed by the table, so two databases can be merged without doubling anything.

### Step 24

record.

Screen reader:

- One row of a table: one job, one book, one station.

### Step 25

related records.

Screen reader:

- The records in another table that belong to this one, reached by following the link.

### Step 26

report.

Screen reader:

- A document written from the table by a definition in report dot inix, and opened in your editor. Alt plus Shift plus R.

### Step 27

row.

Screen reader:

- What the reader says when you arrive on a record: the fields you chose to hear, in order.

### Step 28

say layer.

Screen reader:

- Every Shift plus letter: a question that changes nothing. Shift plus Z says the status.

### Step 29

select columns.

Screen reader:

- Choosing which fields a row speaks, and in what order. The rest stay in the record for searching and Inspect.

### Step 30

status.

Screen reader:

- The table, the row, and whether a filter or marks are in force; the bar at the bottom says it, and so does Shift plus Z.

### Step 31

stream.

Screen reader:

- A station's sound as it arrives over the Internet, played by the Homer Player. Alt plus Shift plus P.

### Step 32

table.

Screen reader:

- One kind of record, with its fields: jobs, actions, stations.

### Step 33

template.

Screen reader:

- A database that comes with DbDo, copied into your data folder the first time you open it, and yours from then on.

### Step 34

Thirty-three terms. The guide, F1, has each of them in context.

**Something to try:** Pick five terms you did not know and find each one in DbDo.

## 10 - Conclusion

What to carry away from the walks: four principles, one thing kept from each task walk, and where to begin. I say the idea; the reader says the key.

**Before you start:** Nothing is needed.

### Step 1

Four things to carry away, each with its key. A key is named for a word of its command, so it can be guessed.

### Step 2

Keywords, K.

Screen reader:

- Control plus K

### Step 3

Shift asks and never changes: what is in this cell?

Screen reader:

- Shift plus C

### Step 4

What is the state of the list -- the table, the filter, the marks?

Screen reader:

- Shift plus Z

### Step 5

What you write in a record is yours; a refresh of a template's catalog never takes it. And the dialog that adds a record is the dialog that edits it: OK from anywhere.

Screen reader:

- Control plus Enter

### Step 6

One thing kept from each task walk. Walk four: the grid is the grid, in every table and template; arrows, Home and End, a letter to jump.

### Step 7

Walk five: a record is added and edited in the one dialog, with F4 for a pick list, and Inspect reads the whole of it.

Screen reader:

- Control plus I

### Step 8

Walk six: Keywords jumps to a record by any word; Filter narrows the list to all of them; Order and Select Columns decide what you hear and in what order.

Screen reader:

- Control plus F

### Step 9

Walk seven: Related records follow the link between tables; marks gather a few; a report writes them out as a document in your editor.

Screen reader:

- Alt plus Shift plus R

### Step 10

Walk eight: sixty thousand stations are one more table; the station that carries your team is a word away, and Play Stream plays it.

Screen reader:

- Alt plus Shift plus P

### Step 11

Three habits that make the rest easy. Arrow a menu once a week: each item says its key. Press a Say key twice when speech went by too fast. And when a key is unknown, Control plus F1 and press it.

Screen reader:

- Key Describer

### Step 12

Where to begin. Start with one template that fits your life -- books, contacts, recipes, jobs, radio -- and add a record a day. The tables and the keys are the same in all of them, so the second template costs nothing to learn.

### Step 13

When DbDo is yours, make a database of your own: New Database, a table, a few fields; the dialog, the grid and the keys are the same as the templates', because the templates were made with them.

### Step 14

If something goes wrong, the session log under your local application data, with a line about what you were doing, is the fastest way to a fix; the project page on GitHub is where to send it.

### Step 15

Three things people do with DbDo every day, each in one breath. Log a job: Control plus N, the fields, Control plus Enter. Find the station carrying the game: Control plus K, the team, Alt plus Shift plus P. Send a counselor the month's record: mark the jobs, Alt plus Shift plus R, the Work Search Record.

### Step 16

And three things the other Homer programs share with it, so the second program costs nothing to learn: the dialog that works one way, the Say keys that change nothing, and the player that opens the same whether a file or a radio station is behind it.

**Something to try:** Open the template that fits you best and add your first record; then do one thing from each task walk in it.

## 11 - More Information

Where the rest is: the guide and history from inside DbDo, the documents, the GitHub page, updates, and the other Homer Tools.

**Before you start:** Nothing is needed.

### Step 1

F1 opens the guide, the whole of DbDo in one document, from inside the program. Shift plus F1 opens the history of changes. Alt plus F1 says the version.

### Step 2

The ReadMe is the short start; the guide is the reference; Hotkeys lists every key three ways. All three are in the help folder of the installation, and on the project's GitHub page.

### Step 3

F11 checks for a newer version and offers to install it. The project is at github dot com, slash JamalMazrui, slash DbDo, and its releases page holds every installer.

### Step 4

DbDo is one of the Homer Tools, free programs for working by ear: EdSharp for text, FileDir for files, DbDo for data. They share their keys and their player, so learning one is most of learning the next.

### Step 5

What this walk taught: the keys you heard.

**Something to try:** Press F1 and read the first section of the guide.

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
