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

- [00 - Overview and Table of Contents](#00-overview-and-table-of-contents)
- [01 - Installing DbDo](#01-installing-dbdo)
- [02 - Opening DbDo for the First Time](#02-opening-dbdo-for-the-first-time)
- [03 - Adding a New Record](#03-adding-a-new-record)
- [04 - Editing a Record](#04-editing-a-record)
- [05 - Find and Jump](#05-find-and-jump)
- [06 - Order and Where Filter](#06-order-and-where-filter)
- [07 - Select the Columns You Hear](#07-select-the-columns-you-hear)
- [08 - Report, Save and Copy](#08-report-save-and-copy)
- [09 - Look and Prime](#09-look-and-prime)
- [10 - Related Records](#10-related-records)
- [11 - Mark and Unmark](#11-mark-and-unmark)
- [12 - Inspecting a Record](#12-inspecting-a-record)
- [13 - Work Search Record for a Claim or Counselor](#13-work-search-record-for-a-claim-or-counselor)
- [1. Where to Go Next](#1-where-to-go-next)

<!-- walkthrough: written by makeTutorial.py, do not edit between the markers -->

## 00 - Overview and Table of Contents

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** Nothing yet.

### Step 1

DbDo is a database manager for people who work by keyboard and ear. SQLite, Access, spreadsheets, plain data -- every table is a list you arrow through.

Ten walkthroughs, three minutes each, all using JobTrail, the sample that ships with DbDo.

### Step 2

One installs it. Two opens it, and explains the grid and the Shift keys.

### Step 3

Three adds a New record, four edits one. Five is Find and Jump. Six is Order and Where filter.

### Step 4

Seven Selects the columns you hear. Eight is Report, Save and Copy. Nine is Look and Prime, the two computed columns.

Seven changes how every other row sounds.

### Step 5

Every key is named with the word it comes from, so key and command stay together.

If you forget one, Control plus F1 is the key describer: press it, then press the key, and it says what that key does instead of doing it.

**Something to try:** Pick the one that matches today's job.

## 01 - Installing DbDo

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** You have downloaded DbDo underscore setup dot e x e, running a Windows screen reader.

### Step 1: Enter

Run the installer. It needs administrator rights, so Windows asks first. Answer yes.

Screen reader:

- User Account Control dialog
- Do you want to allow this app to make changes to your device?
- Yes button

DbDo installs for everybody on the computer, which is why it asks.

### Step 2

The first page says whether this is a new install, an update from an earlier version, or a reinstall of the same one.

Screen reader:

- This will install DbDo 1.0.150.

If DbDo is already here, the page that asks for a folder is skipped and the update goes where the last one went.

### Step 3: Enter

Press Enter to install. The Install button already has focus, so Enter is all it takes.

Screen reader:

- Setup dialog, Installing, Please wait while Setup installs DbDo on your computer, progress bar

Nothing is asked during the copying.

### Step 4: Tab

The last page offers checkboxes for the optional parts. Each one says what it would do, with version numbers where there are any.

Screen reader:

- Install scripts for improving use with the JAWS screen reader check box checked

A box appears only for a screen reader you actually have installed.

### Step 5: Tab

Tab again for the local AI. If Ollama is already on the computer the box says reinstall, with the version you have, and is not ticked.

Screen reader:

- Reinstall Ollama 0.34.1 (current version) check box not checked

A checkbox that offers to install something you already have is a checkbox that did not look.

### Step 6: Enter

The last two are the ones you will want. Leave Launch DbDo ticked and press Enter to finish.

Screen reader:

- Launch DbDo (Alt+Control+D starts it any time) check box checked

The guide box is not ticked, because most people want the program first.

### Step 7

A results box appears before DbDo does. It says what was installed, what the optional parts look like now, and where the log is.

Screen reader:

- DbDo Setup Results dialog
- DbDo 1.0.156 is installed. Program files, C colon backslash Program Files backslash DbDo
- OK button

The box always appears. It is part of installing rather than a choice.

### Step 8: Enter

Press Enter to close it, and DbDo starts.

Screen reader:

- DbDo
- JobTrail dot d b, actions
- Records list view
- 2026-09-24 Follow up on the Accessibility Analyst interview awaiting reply, 1 of 7

DbDo starts only after the box is closed, so its window never lands on top of something you were reading.

**Something to try:** Open the log the results box named, and read the line that says where DbDo was installed.

## 02 - Opening DbDo for the First Time

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** DbDo is installed and not yet opened, so it starts on the JobTrail sample.

### Step 1: Alt+Control+D

Alt plus Control plus D starts DbDo from anywhere in Windows -- D for DbDo. First run opens the sample database, JobTrail.

Screen reader:

- DbDo
- JobTrail dot d b, jobs
- Records list view
- Demo Data Cooperative (made up) Data Quality Specialist rejected, 1 of 4

After the first time, DbDo opens whatever you had open last.

### Step 2

A window opened, so the title was read. Focus landed, so the control was read, then its value. Name, role, value, state -- that order never changes.

Screen reader people call it nervish -- name, role, value, state, hint. Learn the order once and every announcement makes sense.

### Step 3

A table is just a list you arrow through. Each row is one job, and you hear three things: the employer, the role, and where it stands.

Screen reader:

- Demo Data Cooperative (made up) Data Quality Specialist rejected, 1 of 4

Three fields, chosen so you can decide whether to keep going before the row finishes.

### Step 4: DownArrow

Arrow down.

Screen reader:

- Example Widgets Company (sample employer) Accessibility Analyst interviewing, 2 of 4

DownArrow interrupts speech, so you can move on as soon as you have heard enough.

### Step 5: RightArrow

It is a grid. Down and Up move between rows -- one record each. Left and Right move between columns -- one field each.

Screen reader:

- Accessibility Analyst

Only the columns you chose are in the grid. The record holds more, and Enter opens all of it.

### Step 6: Shift+C

Shift plus C is Say Cell: the column you are on, and its value here.

Screen reader:

- title, Accessibility Analyst

C for Cell. It never says the table or the sort, which is what Say Status on Shift plus Z is for.

### Step 7: DownArrow

Down keeps the column, so you can compare one field down a list.

Screen reader:

- Digital Services Assistant

Control plus Home and Control plus End go to the first and last record, and keep the column too.

### Step 8: i

Type a letter, and the list goes to the next row starting with it, the way any Windows list does. Type two or three letters for a closer landing.

Screen reader:

- Illustration City Library (fictional) Digital Services Assistant lead, 3 of 4

Lower case is fine, and case does not matter.

### Step 9: Shift+Z

Your screen reader's say line key reads the row again. Shift plus Z is Say Status: table, row of how many, and order.

Screen reader:

- status: jobs row 3 of 4, sort employer

A letter on its own moves. Shift plus a letter answers. The two layers never collide.

### Step 10: Shift+C

Shift plus N is Say Notes, which reads the note on this record, and Shift plus L is Say Look, the one-line summary of it.

Screen reader:

- notes, Illustrative record. Heard about it at a sample networking event; not applied yet.

Every answer starts with the name of what you asked for, so you know which key you pressed.

### Step 11: Control+PageDown

Control plus Page Down moves to the next table, the way Page Down moves through tabs everywhere else. JobTrail gives you five: actions, contacts, docs, jobs and stories.

Screen reader:

- JobTrail dot d b, contacts
- Records list view
- Alvarez Robin Sample Employment Network (illustration), 1 of 4

F7 lists them to pick from instead. Two more tables exist that DbDo keeps for itself, holding the pick lists and the links between records; you are not offered those.

### Step 12

That is the whole idea, and it is a small one: a table is a list, a letter moves, Shift plus a letter answers, and everything else is a menu away.

Alt plus F10 lists every command in one window you can filter by typing.

**Something to try:** Arrow through all four jobs, reading each row with your say line key, and notice which of the three fields you actually need before moving on.

## 03 - Adding a New Record

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** JobTrail is open on the jobs table.

### Step 1: Control+N

Press Control plus N for a New record -- N for New, as in most Windows programs. DbDo opens a dialog with one box per field, in the order you would fill them in.

Screen reader:

- New Record dialog
- Employer edit

The first box is the employer, because that is what you know first about a job.

### Step 2: Tab

Type the employer and press Tab.

Screen reader:

- Title edit

Tab moves forward, Shift plus Tab moves back. The order is the order of the fields in the table.

### Step 3: Tab

Type the role, Tab again, and you reach status. Listen to the end of this one.

Screen reader:

- Status edit
- F4 picks from 7 values

A field with a pick list says so. The tip is spoken when you arrive and shown in the status line.

### Step 4: F4

Press F4 to open the list, arrow to the value you want, and press Enter.

Screen reader:

- Status list box, applied, 1 of 7

Alt plus DownArrow does the same thing, which is the Windows way of opening a combo.

### Step 5: l

Type the first letter to jump instead of arrowing. L takes you to lead, which is where a job starts.

Screen reader:

- lead, 4 of 7

The values are alphabetical so that first letters land where you expect.

### Step 6: Enter

Press Enter to take it. The value goes into the field and you are back in the dialog.

Screen reader:

- Status edit, lead

Twenty three fields in JobTrail have pick lists, including the states and countries in contacts.

### Step 7: Control+Enter

Fill in what you know and leave the rest. Press Control plus Enter from anywhere in the dialog to save.

Screen reader:

- Record added
- Records list view
- Widget Works (your own entry) Support Analyst lead, 5 of 5

Control plus Enter saves from any field, so you never have to find the OK button.

**Something to try:** Add a second job at the same employer and notice that DbDo refuses a duplicate of the same employer and title.

## 04 - Editing a Record

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** JobTrail is open on the jobs table, on a job you have applied to.

### Step 1: Enter

Press Enter on a row to open it for editing. The same dialog you used to add a record, with the values in it.

Screen reader:

- Edit Record dialog
- Employer edit, Example Widgets Company (sample employer)

The caret starts at the beginning of the box, not at the end of the text.

### Step 2: F4

Tab to the field you want. Status has a pick list, so F4 offers the values rather than making you spell one.

Screen reader:

- Status list box, interviewing, 3 of 7

Picking rather than typing is how a vocabulary stays consistent across a hundred records.

### Step 3: Control+Enter

Enter takes the value; Control plus Enter saves the record.

Screen reader:

- Record saved
- Records list view

Escape leaves without saving, and DbDo asks first if you changed anything.

### Step 4: F2

For one field there is a shorter way. Move to the column you want with Right Arrow, then press F2 to edit just that cell.

Screen reader:

- Cell edit, interviewing

F2 is the Windows key for editing in place, and it works the same here.

### Step 5: F4

The cell editor has the same pick list. F4, a letter, Enter.

Screen reader:

- Cell list box, offer, 5 of 7

Anything a dialog can do to a field, the cell editor can do to the same field.

### Step 6: Enter

Enter saves the cell.

Screen reader:

- offer

Shift plus E is Say Edited, and it says when the record was last changed, if you want to be sure it took.

### Step 7: Alt+Shift+N

Some fields are longer than a line. Alt plus Shift plus N edits the Notes -- N for Notes -- in a window of their own, and Control plus Enter saves it.

Screen reader:

- Notes dialog
- Notes edit, multiline, Illustrative record. Interview booked for the 24th.

Alt plus Shift plus T does the same for tags, and Alt plus Shift plus U for the web address.

**Something to try:** Change a status with F2, then press Shift plus E and notice that the edited time has moved.

## 05 - Find and Jump

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** JobTrail is open on the jobs table, sorted by employer.

### Step 1: exa

The quickest way needs no command at all. Type the first letters of what you want, and the list goes there.

Screen reader:

- Example Widgets Company (sample employer) Accessibility Analyst interviewing, 2 of 4

Typing searches the first column, which is why the first column is the one you would look a record up by.

### Step 2: Control+J

Control plus J is Jump to Record -- J for Jump -- and it searches one column. DbDo asks for the text and says which column it will search.

Screen reader:

- Jump to Match (column, employer) dialog
- Text to search for combo box, blank, ALT+T

The box remembers your last ten answers, so a search you repeat is one arrow away.

### Step 3: Enter

Type part of the value and press Enter. DbDo moves to the first row that contains it.

Screen reader:

- Illustration City Library (fictional) Digital Services Assistant lead, 3 of 4

Control plus Shift plus J goes backwards to the previous match.

### Step 4: Shift+G

Shift plus G is Say Goto -- G for Goto, the jump search -- and it says what you looked for last, for when you have lost your place.

Screen reader:

- goto: library in employer

It says none when no jump is active, rather than saying nothing.

### Step 5: Control+F

Control plus F is Find -- F for Find, as everywhere in Windows -- and it searches every column, including the notes and the posting text.

Screen reader:

- Find dialog
- Text combo box, blank

Jump is one column and fast. Find is all of them and thorough.

### Step 6: Enter

Type what you want and press Enter.

Screen reader:

- Sample Health Network (illustration only) Records Coordinator applied, 4 of 4

F3 repeats the find forwards, and Control plus Shift plus F goes back.

### Step 7: Shift+F

Shift plus F is Say Find, the same way Shift plus G is Say Goto: F for Find, G for Goto.

Screen reader:

- find: schedule a

Every Shift plus letter answer begins with the name of what you asked for.

**Something to try:** Find a word you know is in a note rather than in a title, and notice which of the three ways gets you there.

## 06 - Order and Where Filter

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** JobTrail is open on the jobs table.

### Step 1: Control+O

Control plus O sets the Order -- O for Order. Sorting is what it does; Order is what it is called, and O is the key. DbDo offers the columns as a list rather than asking you to type one.

Screen reader:

- Order Records dialog
- Column to sort by list box, employer, 4 of 16, ALT+C

The list starts on the column the table is sorted by now.

### Step 2: Enter

Arrow to the column you want and press Enter. The list is read again in the new order.

Screen reader:

- Sorted by status
- Records list view
- Sample Health Network (illustration only) Records Coordinator applied, 1 of 4

Sorting alphabetically keeps a row in the same place every time you open the table, which is what makes first letters worth typing.

### Step 3: Shift+O

Shift plus O is Say Order, which says the current order at any time. Same O, asked rather than set.

Screen reader:

- order: status

Shift plus Z, Say Status, gives the order along with the table and the row.

### Step 4: Control+W

Control plus W sets the Where filter -- W for Where, as in the SQL word -- a condition deciding which rows you hear at all.

Screen reader:

- Where Filter dialog
- Filter combo box, blank, ALT+F

The condition is SQL, and the box remembers the last ten you used.

### Step 5: Enter

Type the condition and press Enter.

Screen reader:

- 2 rows
- Records list view
- Example Widgets Company (sample employer) Accessibility Analyst interviewing, 1 of 2

A count of zero is an answer too, and DbDo says it plainly rather than treating it as a fault.

### Step 6: Shift+W

Shift plus W is Say Where filter -- W for Where -- and Shift plus Y is Say Yield, how many rows it leaves.

Screen reader:

- where: status = 'applied'

Both say none and zero when there is nothing to report.

### Step 7: Control+Shift+W

Control plus Shift plus W clears the Where filter, and the whole table comes back. Shift reverses a command all through DbDo.

Screen reader:

- where: none
- 4 rows

DbDo remembers the sort and the filter for each table, so they are still there the next time you open it.

**Something to try:** Set a Where filter for the jobs you have applied to, Order them by the date you applied, then clear both and hear the count come back.

## 07 - Select the Columns You Hear

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** JobTrail is open on the jobs table, which shows the employer, the role and the status.

### Step 1: Shift+S

A row is heard rather than seen, so the columns it carries are worth a thought. Shift plus S is Say Select -- S for Select -- and names the ones you have.

Screen reader:

- select: employer, title, status

Three is the usual number. Four is the most that stays comfortable.

### Step 2: Control+S

Control plus S opens Select Columns -- S for Select. The columns you hear are the ones selected here. Every column in the table is there, ticked or not.

Screen reader:

- Select Columns dialog
- Columns check list box, employer check box checked, 1 of 16, ALT+C

The order in the dialog is the order in the table, which is the order somebody would fill the fields in.

### Step 3: DownArrow

Arrow down the list and press Space to tick or untick a column.

Screen reader:

- title check box checked, 2 of 16

The ticked ones are read in the order they appear here, so the first one is the one you hear first.

### Step 4: Space

Tick the applied date as a fourth column.

Screen reader:

- applied underscore date check box checked, 11 of 16

A date is a good fourth column because it is short. A note is a bad one because it is long.

### Step 5: Control+Enter

Press Control plus Enter to take the change. The list is read again with the new columns.

Screen reader:

- Records list view
- Example Widgets Company (sample employer) Accessibility Analyst interviewing 2026-09-02, 2 of 4

Everything you did not choose is still there. It is one keystroke away in the record.

### Step 6: Enter

Press Enter on a row to hear every field, whether it is a column or not.

Screen reader:

- Edit Record dialog
- Employer edit, Example Widgets Company (sample employer)

Choosing columns decides what you hear while moving, not what the record holds.

### Step 7: Escape

Escape closes it. DbDo remembers your columns for this table, so the choice is made once.

Screen reader:

- Records list view

The choice is kept in a settings file beside the database, along with the sort and the filter.

**Something to try:** Add a fourth column, listen to ten rows, then take it away again and decide which you preferred.

## 08 - Report, Save and Copy

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** JobTrail is open on the actions table, which is the log of everything you have done.

### Step 1: Control+C

Start with the simplest output there is: one record. Control plus C copies the record you are on -- C for Copy, as everywhere -- to the clipboard, as labelled lines.

Screen reader:

- Record copied

Alt plus Shift plus C adds a record to what is already on the clipboard, so you can gather several.

### Step 2: Alt+Shift+R

Alt plus Shift plus R runs a Report -- R for Report, and Shift because Alt plus R belongs to the menu bar. JobTrail comes with six, and DbDo offers them as a list.

Screen reader:

- Run Report dialog
- Output list box, application underscore history, 1 of 6, ALT+O

A report reads the whole table rather than the rows on screen, so the filter does not change what it produces.

### Step 3: w

The one that matters most is the work search record: every countable employer contact, in the shape an agency asks for.

Screen reader:

- work underscore search underscore record, 6 of 6

The values are alphabetical, so the first letter lands you near what you want.

### Step 4: Enter

Press Enter.

Screen reader:

- Report written
- EdSharp, work underscore search underscore record dot m d

It is Markdown, so pandoc turns it into a web page to send or a Word file to hand across a desk.

### Step 5: Control+Shift+S

For a whole table rather than a report, Control plus Shift plus S is Save As -- S for Save -- and writes the table to a file. DbDo asks for the name and decides the format from what you type.

Screen reader:

- Save As dialog
- File name edit

Type a name ending in x l s x for a spreadsheet, c s v for plain data, or d o c x for a document.

### Step 6: Enter

Type the name and press Enter. No Excel needed.

Screen reader:

- Saved actions dot x l s x

The header row is frozen and the columns are sized, so the file is usable by somebody else as it is.

### Step 7: Shift+Y

Shift plus Y is Say Yield -- Y for Yield, what the table yielded -- so it says how many rows went into the file.

Screen reader:

- yield: 7 rows

Every count in DbDo matches its noun, so one row is one row rather than one rows.

**Something to try:** Produce the work search record for the last four weeks and read it in your editor before sending it to anybody.

## 09 - Look and Prime

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** JobTrail is open on the jobs table, on the Example Widgets job.

### Step 1: Shift+L

Every table DbDo makes has two columns nobody types into. They are computed from the fields beside them, and each one has a job. Shift plus L is Say Look -- L for Look -- and it says the first.

Screen reader:

- look: Example Widgets Company (sample employer) | Accessibility Analyst | interviewing

Look is what this record looks like when it is mentioned somewhere else.

### Step 2

The idea is older than DbDo. In a form of fields, a field pointing at another table shows a number, and a number is not a record. Look is the glimpse that goes with it.

The parts are separated by a space, a bar and a space, because a screen reader pauses at punctuation and the parts then arrive as parts.

### Step 3: Shift+R

Shift plus R is Say Related -- R for Related -- and it shows the look working. Every record this one is related to is listed by its look, so you know which is which without opening any of them.

Screen reader:

- related, actions (1 record)
- 2026-09-08 bar First interview for Accessibility Analyst bar interview scheduled

A glimpse is deliberately not the whole record. Enter opens the record when you want the rest.

### Step 4: Shift+P

The second column is prime, which is short for primary key. Shift plus P is Say Prime -- P for Prime -- and it says it.

Screen reader:

- prime: Example Widgets Company (sample employer)|Accessibility Analyst

Bare bars, no spaces. This one is for a program rather than for you.

### Step 5: Shift+I

The table does have a formal key, a number the database hands out. Shift plus I is Say Id -- I for Id -- and it says that number. Notice how little it tells you.

Screen reader:

- job_id: 1

A number says nothing about the record. What decides whether two rows are the same job is the employer and the title.

### Step 6

So prime is computed from exactly the fields that make a record unique. That buys two things.

The rule can be changed by editing one expression, rather than by migrating a key.

### Step 7

And matching becomes one comparison. A script deciding whether to add a job or update the one already there compares prime, not three fields.

The maps table works the same way: a link is one prime, a kind, and another prime, so it needs no knowledge of how either table numbers its rows.

### Step 8

One line to remember. Look is for a person, joined with spaces so it reads aloud. Prime is for a program, joined without them so it matches exactly.

Neither is ever typed, and neither can be edited: the database keeps both current as the fields beside them change.

**Something to try:** Open a contact and press Shift plus R, then work out which job each line came from without opening anything.

## 10 - Related Records

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** JobTrail is open on the jobs table, on the Example Widgets job.

### Step 1: Shift+R

Shift plus R is Say Related -- R for Related. It names every record connected to this one, each by its look, so you know which is which without opening any.

Screen reader:

- related, actions (1 record)
- 2026-09-08 bar First interview for Accessibility Analyst bar interview scheduled

Connections come from two places: a field holding another record's id, and the maps table DbDo keeps for links across tables.

### Step 2: Alt+RightArrow

Alt plus Right Arrow enters the related records, the way Right Arrow goes deeper in a tree.

Screen reader:

- JobTrail dot d b, actions
- Records list view
- 2026-09-08 First interview for Accessibility Analyst interview scheduled, 1 of 1

The filter is set for you: only the actions belonging to that job.

### Step 3: Backspace

Backspace goes back up, as it does everywhere in Windows.

Screen reader:

- JobTrail dot d b, jobs
- Example Widgets Company (sample employer) Accessibility Analyst interviewing, 2 of 4

Alt plus Left Arrow does the same.

### Step 4: Shift+R

The link a record does not hold itself lives in maps: this document was sent for that job, this person is a contact for it.

Screen reader:

- related, docs via sent_for (2 records)

DbDo writes those rows; you are not offered the maps table when choosing one.

### Step 5

Say Related is a list, not a trip. Use it to decide, then Alt plus Right Arrow when you mean to go.

Control plus F1, the key describer, names any key you have forgotten.

**Something to try:** From a contact, reach the job they are a contact for, then come back with Backspace.

## 11 - Mark and Unmark

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** JobTrail is open on the jobs table.

### Step 1: Control+M

Control plus M Marks the record you are on -- M for Mark.

Screen reader:

- Marked

One word, because the state is the whole answer.

### Step 2: Control+U

Control plus U Unmarks it -- U for Unmark. The pair sits together on the keyboard and in the menu.

Screen reader:

- Unmarked

A toggle says the state it reached, never the key that got there.

### Step 3: Shift+M

Shift plus M is Say Mark, which tells you where you stand without changing anything.

Screen reader:

- Unmarked

Every Shift key asks; no Shift key changes.

### Step 4: Control+DownArrow

Control plus Down Arrow steps to the next marked record, Control plus Up Arrow to the previous, so a scattered set reads as a list.

Screen reader:

- Sample Health Network (illustration only) Records Coordinator applied, 4 of 4

Marks live for the session and are remembered per table.

### Step 5: Shift+End

Shift plus Home marks everything from here to the top; Shift plus End, to the bottom. Alt plus Shift plus Home and End undo those.

Screen reader:

- 3 marked

The same keys that select to top and bottom everywhere else in Windows.

### Step 6: Shift+Space

Marks are how you act on some records rather than all: copy them together, or delete them in one go.

Screen reader:

- marked rows, 3 of 4

Shift plus Space is Say Marked Rows, which counts them.

**Something to try:** Mark the two jobs you have applied to, then produce a report and notice it covers the table rather than your marks.

## 12 - Inspecting a Record

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** JobTrail is open on the jobs table, on the Example Widgets job.

### Step 1: Enter

A row gives you three fields. The record holds all of them, and Enter opens it.

Screen reader:

- Edit Record dialog
- Employer edit, Example Widgets Company (sample employer), ALT+E

Tab moves through the fields in the order you would fill them in.

### Step 2: Escape

Escape closes it without changing anything. For a quick answer, you do not have to open the record at all.

Screen reader:

- Records list view

The Shift keys answer questions about the record under the cursor.

### Step 3: Shift+N

Shift plus N is Say Notes, Shift plus T is Say Tags, Shift plus U is Say URL. Each says the field its letter starts.

Screen reader:

- notes, Illustrative record. Interview booked for the 24th; ask about screen reader testing tools.

An empty field says null or blank, so silence never means a key failed.

### Step 4: Shift+E

Shift plus A is Say Added and Shift plus E is Say Edited: when the record arrived, and when it last changed.

Screen reader:

- edited, September 19, 2026 at 10:01 PM

Useful after a save, to be sure it took.

### Step 5: Alt+Shift+N

Alt plus Shift plus N opens the Notes in a window of their own, for reading a long one line by line.

Screen reader:

- Notes dialog
- Notes edit, multiline, Illustrative record. Interview booked for the 24th., ALT+N

Control plus Enter saves it; Escape leaves it.

### Step 6: Shift+Z

Shift plus Z is Say Status: which table, which row of how many, and the order. The one to press when you have lost your place.

Screen reader:

- status, jobs row 2 of 4, sort employer

Your screen reader's say line key reads the row itself.

**Something to try:** Walk Shift plus A to Shift plus Z on one record and notice which three answers you would want while arrowing.

## 13 - Work Search Record for a Claim or Counselor

A simulated walk through DbDo, made with AI: a person working, and a screen reader answering.

**Before you start:** JobTrail is open on the actions table, the log of everything you have done.

### Step 1

Unemployment offices and vocational rehabilitation counselors ask for the same thing: what you did, when, with whom, and what came of it. The actions table holds exactly that.

Every contact is one row: date, kind, method, summary, outcome.

### Step 2: Shift+C

Three fields matter to the agency and to nobody else. Method is how you made contact. Evidence is what you kept. Counts says whether it is an employer contact at all.

Screen reader:

- method, internet

Preparation and workshops belong in the log and do not count toward a weekly minimum. Counts is how the report tells them apart.

### Step 3: Alt+Shift+R

Alt plus Shift plus R runs a Report -- R for Report.

Screen reader:

- Run Report dialog
- Output list box, application underscore history, 1 of 6, ALT+O

A report reads the whole table, so a filter on screen does not change what it produces.

### Step 4: w

Type W for the work search record.

Screen reader:

- work underscore search underscore record, 6 of 6

The values are alphabetical, so the first letter lands near what you want.

### Step 5: Enter

Press Enter.

Screen reader:

- Report written
- EdSharp, work underscore search underscore record dot m d

Countable employer contacts first, with a total and a count for each week; everything else after, under its own heading.

### Step 6

It is Markdown, so pandoc turns it into a web page to email or a Word file to hand across a desk.

Keep the log. Agencies can ask for it up to thirty days after a benefit year ends, and DbDo never deletes an action.

**Something to try:** Produce the record for the last four weeks and read it before sending it to anybody.

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
