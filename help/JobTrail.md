---
title: "JobTrail"
subtitle: "Plan for a job lead and application database built on DbDo"
author: "Jamal Mazrui"
---

# JobTrail

A record of every job lead you follow, every person you talk to, and every
document you send, kept in one file you own. It produces the Work Search Record
that an unemployment officer or a vocational rehabilitation counselor asks for.

JobTrail is a DbDo database, not a new program. `JobTrail.db` holds the tables;
the scripts and reports beside it do the work. That means it inherits everything
DbDo already does well by keyboard and screen reader, and it means the first
version can be finished and used rather than merely started.

## What this plan covers

- the naming conventions it follows, taken from the shipped DbDo databases
- the seven tables and every field in them
- the lookup values that fill the combo boxes
- how records connect to each other
- the scripts, and what each one saves you
- the reports, and who reads them

# The conventions this follows

Read from `contacts.db`, `reads.db` and `lookups.db` as DbDo ships them, so
JobTrail looks like a DbDo database to anybody who has used one.

Every table has, in this order:

1. `<singular>_id INTEGER PRIMARY KEY AUTOINCREMENT` -- the row's own number.
2. `added TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP` and `edited` likewise,
   both maintained by DbDo.
3. The data fields, in the order a person would fill them in.
4. `url TEXTLINE` where a record can point at something on the web.
5. `notes TEXTMARKDOWN` -- the long free text.
6. `tags TEXTMEMO` -- short labels for filtering.
7. `look TEXT GENERATED ALWAYS AS (...) STORED` -- what the record is called when
   it appears in a list, the parts joined with ` | `.
8. `prime TEXT GENERATED ALWAYS AS (...) STORED` -- the identifying fields joined
   with `|`, which is what `maps` points at and what a unique index guards.
9. `marked INTEGER NOT NULL DEFAULT 0` -- always last.

Field types are DbDo's: `TEXTLINE` for one line, `TEXTMARKDOWN` for prose,
`TEXTMEMO` for several short lines, `TEXTTIME` for a timestamp, `INTEGER` for a
number or a yes/no.

Names are lower case with underscores, singular for the id, plural for the
table. Nothing is abbreviated past recognition: `employer`, not `emp`.

**First letters matter.** The table list is reached by typing a letter, so the
seven tables begin with seven different letters: `actions`, `contacts`,
`docs`, `jobs`, `lookups`, `maps`, `stories`.

# The tables

## jobs

One row per opening you are pursuing. The heart of the database.

- `job_id` INTEGER PRIMARY KEY AUTOINCREMENT
- `added`, `edited` TEXTTIME
- `employer` TEXTLINE -- the organization, as they write it
- `employer_contact` TEXTLINE -- the address, phone or web address of the
  employer. **Required on a work search log**, and the field people most often
  wish they had captured when the posting has gone
- `title` TEXTLINE -- the role as advertised
- `reference` TEXTLINE -- the job reference or requisition number. **An agency
  accepts this in place of the position title**, and an ATS confirmation usually
  quotes it
- `status` TEXTLINE -- from lookups; starts at `lead`
- `source` TEXTLINE -- from lookups; where you found it
- `location` TEXTLINE -- city and state, or `remote`
- `pay` TEXTLINE -- free text, because postings say "DOE" as often as a number
- `applied_date` TEXTLINE -- ISO date, empty until you apply
- `ats` TEXTLINE -- from lookups; whose application system it uses
- `closing_date` TEXTLINE -- ISO date, when the posting says one
- `url` TEXTLINE -- the posting
- `posting` TEXTMARKDOWN -- the text of the posting, captured at the time
- `notes` TEXTMARKDOWN
- `tags` TEXTMEMO
- `look` = employer, title, status
- `prime` = employer, title
- `marked`

A unique index on `prime` stops you applying to the same role twice without
noticing, which is the most common wasted hour in a job search.

## contacts

The standard DbDo contacts table, unchanged. Keeping it identical means the
lookups, the accelerator keys and anything built for contacts elsewhere work
here without adjustment.

`contact_id`, `added`, `edited`, `first_name`, `middle_name`, `last_name`,
`enterprise`, `job`, `wireless_phone`, `home_phone`, `personal_email`,
`business_email`, `address1`, `city`, `state`, `zip`, `nation`, `url`, `notes`,
`tags`, `look`, `prime`, `marked`.

## actions

Every contact made and every thing still to do, in one table. This is what the
Work Search Record is built from, so its fields are the ones agencies ask for.

- `action_id` INTEGER PRIMARY KEY AUTOINCREMENT
- `added`, `edited` TEXTTIME
- `action_date` TEXTLINE -- ISO date; for a future action this is when it is due
- `kind` TEXTLINE -- from lookups: application, inquiry, interview, follow_up,
  preparation, workshop... The agency calls this the type of contact and
  requires it
- `method` TEXTLINE -- from lookups, and the list is the agency's own: email,
  fax, in person, internet, kiosk, mail, phone, video. **Required, because what
  else you must record depends on it.**
- `person` TEXTLINE -- the name or title of the person dealt with. Required for
  a fax, in person, mail, inquiry or interview contact
- `place` TEXTLINE -- where an interview happened, or which WorkSource office
  or American Job Center an activity was at. Required for those two
- `detail` TEXTLINE -- the phone number, email address, website or video service
  used. Which of those it holds depends on the method, and one field covers them
  all because only one is ever required
- `summary` TEXTLINE -- one line, which is what appears in the report
- `outcome` TEXTLINE -- from lookups: awaiting reply, interview scheduled,
  no response, not hiring, offer, rejected, withdrawn. **The agency calls this
  the result of contact and requires it on every entry**
- `evidence` TEXTLINE -- what proof you kept and where: "confirmation email
  saved", "screenshot in results folder". Reviews are random, and this is the
  column people wish they had filled
- `counts` INTEGER NOT NULL DEFAULT 1 -- 1 when this is an employer contact the
  agency counts, 0 when it is preparation. See below
- `done` INTEGER NOT NULL DEFAULT 0 -- 0 is still to do, 1 is finished
- `job_id` INTEGER -- the job this was about, when it was about one
- `contact_id` INTEGER -- the person, when they are in contacts
- `details` TEXTMARKDOWN -- what was said
- `notes` TEXTMARKDOWN
- `tags` TEXTMEMO
- `look` = action_date, kind, summary
- `prime` = action_date, kind, summary
- `marked`

Both foreign keys may be empty. A networking coffee is an action with a contact
and no job; a speculative application is an action with a job and no contact.

## docs

The resumes, cover letters and answer sets you send, so you know which version
went where.

- `doc_id` INTEGER PRIMARY KEY AUTOINCREMENT
- `added`, `edited` TEXTTIME
- `name` TEXTLINE -- what you call it
- `kind` TEXTLINE -- from lookups: resume, cover_letter, answers, portfolio,
  reference_list, offer
- `version` TEXTLINE -- your own marker, so "resume, plain text, March" is one
  row and "resume, chronological, March" is another
- `url` TEXTLINE -- the path or link to the file, so Open URL opens it
- `notes` TEXTMARKDOWN
- `tags` TEXTMEMO
- `look` = name, kind, version
- `prime` = name, version
- `marked`

## stories

For interview preparation, and the one table here that is not about
record keeping.

One row per accomplishment, told as Challenge, Action, Result and Takeaway. A
behavioral interview question -- "tell me about a time a team failed", "when you
convinced somebody to change their mind", "when you led a group of peers" --
is answered by picking the story that fits, and `tags` says which questions each
story answers.

- `story_id` INTEGER PRIMARY KEY AUTOINCREMENT
- `added`, `edited` TEXTTIME
- `name` TEXTLINE -- what you call the story, so you can find it under pressure
- `challenge` TEXTMARKDOWN -- the situation and what made it hard
- `action` TEXTMARKDOWN -- what you did, in the first person
- `result` TEXTMARKDOWN -- what happened, with a number in it where there is one
- `takeaway` TEXTMARKDOWN -- what it says about you, which is what the
  interviewer is actually asking
- `notes` TEXTMARKDOWN
- `tags` TEXTMEMO -- the question themes this story answers
- `look` = name
- `prime` = name
- `marked`

Prepared once, used for years. Its first letter, s, is free among the other
tables.

## lookups and maps

The two standard DbDo tables, exactly as DbDo makes them. `lookups` fills the
combo boxes; `maps` records the many-to-many links.

# What the agency requires

Read from the Washington Employment Security Department's Unemployed Worker
Handbook, which is the document behind the report. Other states differ in
detail; the shape is the same everywhere, and the fields below cover it.

## Every entry needs three things

The date of contact, the type of contact, and the result of contact. Those are
`action_date`, `kind` and `outcome`, and none of them may be empty on an entry
that counts.

## What else depends on how you made contact

- **An application** needs the position applied for **or the job reference
  number**, the employer's name **and address**, and then one more thing
  depending on the method: a phone number, the name or title of a person, a fax
  number, or the website, email address or newspaper used.
- **An inquiry** needs the employer's name and address, the name or title of the
  person contacted, how you reached them, the position asked about, and the
  result.
- **An interview** needs the position, the employer's name and address, **where
  the interview took place**, the names and titles of the people conducting it,
  a phone number or email address, and **which video service** if it was remote.
- **A WorkSource or American Job Center activity** needs the office's location,
  the activity completed, and the instructor's name.

That is why `person`, `place` and `detail` exist as separate fields. Between
them they hold everything the four cases above ask for, and the report prints
only the ones that apply.

## Not everything counts

The handbook is explicit that some activities are **not** employer contacts:
contacting an employer you already know is not hiring, browsing job listings,
posting a resume without applying, having a recruiter look on your behalf,
working to set up your own business, calling a job line, and checking in with a
recruiting service you have already registered with.

They still belong in the log -- they are what you did that week, and a counselor
wants to see them -- but they must not be counted toward a weekly minimum. That
is what the `counts` field is for. The Work Search Record lists countable
contacts first, with their total, and everything else after under a heading that
says what it is.

Getting this wrong is expensive: an incomplete log, or one that does not show a
genuine search, can mean benefits denied and already-paid benefits repaid.

## Keep the log

The handbook requires keeping it for at least thirty days after the end of the
benefit year, or thirty days after benefits stop, whichever is later, and
reviews are random. **JobTrail never deletes an action**, and the guide will say
so plainly.

# The lookup values

Seeded into `lookups` so that the fields with fixed vocabularies offer them from
the start. Each set is alphabetical, since that is the order a combo box is
searched by first letter.

- `jobs.status`: applied, closed, interviewing, lead, offer, rejected, withdrawn
- `jobs.source`: agency, board, employer site, event, network, other, recruiter,
  referral
- `jobs.ats`: Ashby, BrassRing, email, employer form, Greenhouse, iCIMS, Lever,
  other, Taleo, USAJOBS, Workday
- `actions.kind`: application, follow_up, inquiry, interview, meeting, note,
  offer, preparation, rejection, research, thank_you, workshop
- `actions.method`: email, fax, in person, internet, kiosk, mail, phone, video
  -- **the agency's own list**, so an entry can be transcribed to their form
  without translation
- `actions.outcome`: awaiting reply, hired, interview scheduled, no response,
  not hiring, offer, rejected, withdrawn
- `docs.kind`: answers, cover_letter, offer, portfolio, reference_list, resume
- `maps.kind`: contact_for, sent_for, works_at

# How records connect

Two mechanisms, each for a different frequency of use.

**Foreign keys, for what happens daily.** `actions.job_id` and
`actions.contact_id` are filled in as you work, usually by a script that already
knows which job you are on.

**Maps, for what happens occasionally.** Three kinds:

- `contact_for` -- this person is a contact for that job
- `works_at` -- this person works at that employer, linking contact to contact
- `sent_for` -- this document was sent for that job, with the date in the note

# The scripts

In `Scripts\`, which is where DbDo looks and what the Homer layout calls the
folder. Each one exists because it removes keystrokes from something you do many
times a week.

- **Capture-Posting.js** -- takes the job posting from the clipboard, makes a
  `jobs` row with status `lead` and today's date, and puts the text in
  `posting`. One paste instead of six fields.
- **Log-Application.js** -- the single most useful script. On a `jobs` row it
  sets `status` to `applied` and `applied_date` to today, writes an `actions`
  row of kind `applied` with the method you choose, and creates a second,
  undone `actions` row of kind `follow_up` dated a week out. One keystroke for
  what is otherwise three records.
- **Log-Contact.js** -- on a `contacts` row, writes an `actions` row for a call,
  an email or a meeting, links it to the job you were last on, and offers to
  schedule the follow-up.
- **Next-Follow-Up.js** -- moves an undone `follow_up` forward by a week, for the
  employer who has not replied yet.
- **Mark-Closed.js** -- sets a job's status, writes the matching action, and
  marks any outstanding follow-ups done, so a rejection does not leave a task
  list haunted by dead applications.
- **Today.dbdo** -- the saved view the database opens on: actions where `done`
  is 0 and `action_date` is today or earlier, oldest first.
- **Stale-Applications.dbdo** -- jobs with status `applied` whose most recent
  action is more than fourteen days old.
- **Active-Pipeline.sql** -- a count of jobs by status, which is the answer to
  "how is it going" in one line.
- **Neglected-Contacts.sql** -- people in `contacts` with no action in ninety
  days.
- **Ats-Experience.sql** -- your own record of which application systems worked:
  jobs grouped by `ats`, with how many applications reached an interview.

# The reports

Defined in `report.inix` beside the database, in DbDo's report format. Each is
Markdown first, so pandoc turns it into HTML to email or a Word file to hand
across a desk.

## Work Search Record

**The report that matters most, because somebody else reads it.**

Every action between two dates, in date order, printed the way the agency asks
for it: the date, the type of contact, the method, the employer with its
address, the position or its reference number, the person, the place, the phone
or email or website or video service, the result, and what evidence was kept.
Each entry prints only the fields its method requires, because a field that is
empty earns no line.

Two sections. **Countable employer contacts** first, with their total and a
count for each week in the period, so you can see at a glance whether a weekly
minimum was met. **Other job search activity** after it, for the preparation,
the workshops and the self-employment work that belong in the record but do not
count as employer contacts.

The period is stated at the top, every count matches its noun, and the heading
carries your name and the date the report was printed.

The period comes from `Settings.LastReportDate` in `JobTrail.inix`, so
"since the last reporting period" is a fact rather than arithmetic. Producing
the report updates that date, and a `--from` and `--to` let you produce any
period again.

## Pipeline Summary

For you. Jobs by status, with the count of each, then the open ones listed
newest first. One page that answers what is live, what is waiting and what is
closed.

## Follow-Up Schedule

Every undone action, in date order, with what it is about. This is what you read
on a Monday morning.

## Contact Directory

Everybody in `contacts`, with their employer, their phone and email, and the
jobs they are a contact for. Alphabetical by last name.

## Interview Preparation

For a specific job: the posting, every action taken on it, the people involved,
and the stories from `stories` whose tags match the kind of role. Printed the
night before.

## Application History

One job per section, with its posting details and every action taken on it in
order. This is the report you read before an interview, and the one you keep
after the search ends.

# What happens first

Phase one is the database, the scripts and the reports, with no new program:
`JobTrail.db`, `JobTrail.inix`, `report.inix`, the `Scripts\` folder, and the
document set. It can be used the day it is finished.

Phase two is a dedicated program, and only when a tester asks for something that
cannot be a script, a report, a lookup or a saved view. That test has not been
met yet, and might not be.
