#!/usr/bin/env python3
# build_windows_tutorials.py - build WindowsTutorials.db from the Windows Screenreader
# Primer (David Kingsbury, The Carroll Center for the Blind).
#
# Modeled on build_convention.py: the same standard column set, the same
# generated look/unq STORED columns, the same edited trigger, and the same
# generic maps table for typed associations. Four content tables plus the
# two standard infrastructure tables:
#
#   contacts - people and organizations: the author/publisher and the makers
#              of the apps. Same field roster as the convention's contacts.
#   apps     - the applications the book teaches (Microsoft Word, Google
#              Chrome, Claude, JAWS, ...), each with a category.
#   tasks    - app-agnostic things the tutorial explains how to do, taken
#              from the book's subsection headings (e.g. "Unzipping or
#              Extracting Files", "Scheduling Delayed Message Delivery").
#   methods  - the steps for accomplishing one task with one app, taken from
#              the procedure (the bullet/numbered steps under a subsection).
#   maps     - typed associations by (table, unq) pairs. Kinds used here:
#              makes (contact -> app), for_task (method -> task),
#              with_app (method -> app), and with_reader (method -> the
#              screen reader its steps are written for), so a screen reader
#              links to every task, app, and method it touches.
#   lookups  - valid maps.kind values and apps.category values.
#
# Usage:
#   python build_tutorials.py <primer.docx|.txt> [output.db]
# A .docx is read with python-docx (heading styles -> structure); a pre-
# extracted .txt/.md with #/##/###/#### headings and "- " bullets also works.

import re
import sqlite3
import sys

c_sDefaultOutput = "WindowsTutorials.db"

# ---------------------------------------------------------------------
# Schema (mirrors build_convention.py)
# ---------------------------------------------------------------------

def sLookExpr(lCols):
    lParts = ["iif(%s IS NOT NULL AND length(CAST(%s AS TEXT))>0, CAST(%s AS TEXT) || ' | ', '')"
              % (c, c, c) for c in lCols]
    return "rtrim(" + " || ".join(lParts) + ", ' | ')"

def sUnqExpr(lCols):
    return "||'|'||".join("coalesce(CAST(%s AS TEXT),'')" % c for c in lCols)

# Contacts: a PERSON's identity is the name; an ORGANIZATION (no last_name)
# is identified by its enterprise.
c_sContactUnqExpr = ("iif(last_name IS NOT NULL AND length(last_name)>0, "
                     "coalesce(first_name,'')||'|'||coalesce(middle_name,'')"
                     "||'|'||coalesce(last_name,''), coalesce(enterprise,''))")

dSchema = {
    "contacts": dict(
        data=["first_name TEXTLINE", "middle_name TEXTLINE", "last_name TEXTLINE",
              "enterprise TEXTLINE", "job TEXTLINE",
              "gender TEXTLINE", "date_of_birth TEXTLINE",
              "wireless_phone TEXTLINE", "home_phone TEXTLINE", "office_phone TEXTLINE",
              "personal_email TEXTLINE", "business_email TEXTLINE",
              "address1 TEXTLINE", "address2 TEXTLINE", "city TEXTLINE",
              "state TEXTLINE", "zip TEXTLINE", "nation TEXTLINE",
              "url TEXTLINE"],
        look=["first_name", "last_name", "enterprise"],
        unq=c_sContactUnqExpr),
    "apps": dict(
        data=["name TEXTLINE", "category TEXTLINE", "descrip TEXTMARKDOWN", "url TEXTLINE"],
        look=["name", "category"],
        unq=["name"]),
    "tasks": dict(
        data=["name TEXTLINE", "area TEXTLINE", "descrip TEXTMARKDOWN", "url TEXTLINE"],
        look=["name", "area"],
        unq=["name"]),
    "methods": dict(
        data=["name TEXTLINE", "summary TEXTLINE", "steps TEXTMARKDOWN", "url TEXTLINE"],
        look=["name"],
        unq=["name"]),
    "maps": dict(
        data=["tbl1 TEXTLINE", "unq1 TEXTLINE", "kind TEXTLINE",
              "tbl2 TEXTLINE", "unq2 TEXTLINE"],
        look=["tbl1", "unq1", "kind", "tbl2", "unq2"],
        unq=["tbl1", "unq1", "kind", "tbl2", "unq2"]),
    "lookups": dict(
        data=["src TEXTLINE", "tbl TEXTLINE", "fld TEXTLINE", "val TEXTLINE",
              "ordinal INTEGER", "descrip TEXTMARKDOWN", "url TEXTLINE"],
        look=["src", "tbl", "fld", "val"],
        unq=["src", "tbl", "fld", "val"]),
}


def createSchema(conn):
    cur = conn.cursor()
    for sTable, d in dSchema.items():
        sSingular = sTable[:-1] if sTable.endswith("s") else sTable
        sPk = sSingular + "_id"
        lDataNames = [sCol.split()[0] for sCol in d["data"]]
        lLines = ["  %s INTEGER PRIMARY KEY AUTOINCREMENT" % sPk,
                  "  added TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP",
                  "  edited TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP"]
        lLines += ["  " + sCol for sCol in d["data"]]
        sUnq = d["unq"] if isinstance(d["unq"], str) else sUnqExpr(d["unq"])
        lLines += ["  notes TEXTMARKDOWN", "  tags TEXTMEMO",
                   "  look TEXT GENERATED ALWAYS AS (%s) STORED" % sLookExpr(d["look"]),
                   "  unq TEXT GENERATED ALWAYS AS (%s) STORED" % sUnq,
                   "  marked INTEGER NOT NULL DEFAULT 0"]
        cur.execute('CREATE TABLE "%s" (\n%s\n)' % (sTable, ",\n".join(lLines)))
        lTrig = lDataNames + ["notes", "tags"]
        sOf = ", ".join('"%s"' % c for c in lTrig)
        sWhen = " OR ".join('OLD."%s" IS NOT NEW."%s"' % (c, c) for c in lTrig)
        cur.execute('CREATE TRIGGER "trg_%s_edited" AFTER UPDATE OF %s ON "%s" '
                    'FOR EACH ROW WHEN %s BEGIN UPDATE "%s" SET edited = '
                    'CURRENT_TIMESTAMP WHERE %s = NEW.%s; END'
                    % (sTable, sOf, sTable, sWhen, sTable, sPk, sPk))
    cur.execute('CREATE INDEX idx_maps_side1 ON maps (tbl1, unq1)')
    cur.execute('CREATE INDEX idx_maps_side2 ON maps (tbl2, unq2)')
    conn.commit()


def addUnqIndexes(conn):
    cur = conn.cursor()
    for sTable in dSchema:
        cur.execute('CREATE UNIQUE INDEX "idx_%s_unq" ON "%s" (unq)' % (sTable, sTable))
    conn.commit()


# ---------------------------------------------------------------------
# Curated apps and their makers (grounded in the book's coverage). The
# maker links to a contact via a maps "makes" association, never a column.
# (app name, category, maker enterprise, url, description)
# ---------------------------------------------------------------------

c_lApps = [
    ("JAWS", "screen_reader", "Freedom Scientific",
     "https://www.freedomscientific.com/products/software/jaws/",
     "Job Access With Speech, a commercial Windows screen reader."),
    ("NVDA", "screen_reader", "NV Access", "https://www.nvaccess.org/",
     "NonVisual Desktop Access, a free, open-source Windows screen reader."),
    ("Narrator", "screen_reader", "Microsoft", "https://support.microsoft.com/windows/narrator",
     "The screen reader built into Windows."),
    ("Windows", "operating_system", "Microsoft", "https://www.microsoft.com/windows",
     "Microsoft's desktop operating system."),
    ("Microsoft Word", "office", "Microsoft", "https://www.microsoft.com/microsoft-365/word",
     "Word processor in Microsoft 365."),
    ("Microsoft Outlook", "office", "Microsoft", "https://www.microsoft.com/microsoft-365/outlook",
     "Email and calendar client in Microsoft 365."),
    ("Microsoft Excel", "office", "Microsoft", "https://www.microsoft.com/microsoft-365/excel",
     "Spreadsheet in Microsoft 365."),
    ("Microsoft PowerPoint", "office", "Microsoft", "https://www.microsoft.com/microsoft-365/powerpoint",
     "Presentation program in Microsoft 365."),
    ("Google Chrome", "browser", "Google", "https://www.google.com/chrome/",
     "Google's web browser."),
    ("Microsoft Edge", "browser", "Microsoft", "https://www.microsoft.com/edge",
     "Microsoft's web browser."),
    ("Mozilla Firefox", "browser", "Mozilla", "https://www.mozilla.org/firefox/",
     "Mozilla's web browser."),
    ("Adobe Acrobat Reader", "pdf", "Adobe", "https://www.adobe.com/acrobat/pdf-reader.html",
     "Reader for PDF files."),
    ("Dropbox", "cloud_storage", "Dropbox", "https://www.dropbox.com/",
     "Cloud file storage and sharing."),
    ("Microsoft OneDrive", "cloud_storage", "Microsoft", "https://www.microsoft.com/microsoft-365/onedrive",
     "Microsoft's cloud file storage."),
    ("Google Drive", "cloud_storage", "Google", "https://www.google.com/drive/",
     "Google's cloud file storage."),
    ("Google Docs", "google_workspace", "Google", "https://docs.google.com/",
     "Google's online word processor."),
    ("Google Sheets", "google_workspace", "Google", "https://sheets.google.com/",
     "Google's online spreadsheet."),
    ("Google Slides", "google_workspace", "Google", "https://slides.google.com/",
     "Google's online presentation tool."),
    ("Gmail", "google_workspace", "Google", "https://mail.google.com/",
     "Google's web email service."),
    ("Google Calendar", "google_workspace", "Google", "https://calendar.google.com/",
     "Google's online calendar."),
    ("Google Forms", "google_workspace", "Google", "https://forms.google.com/",
     "Google's online survey and form builder."),
    ("Google Classroom", "google_workspace", "Google", "https://classroom.google.com/",
     "Google's learning-management tool."),
    ("Zoom", "meeting", "Zoom", "https://zoom.us/",
     "Video meeting and webinar platform."),
    ("Microsoft Teams", "meeting", "Microsoft", "https://www.microsoft.com/microsoft-teams",
     "Microsoft's meeting and collaboration app."),
    ("Google Meet", "meeting", "Google", "https://meet.google.com/",
     "Google's video meeting service."),
    ("ChatGPT", "ai", "OpenAI", "https://chatgpt.com/",
     "OpenAI's conversational AI assistant."),
    ("Claude", "ai", "Anthropic", "https://claude.ai/",
     "Anthropic's conversational AI assistant."),
    ("Google Gemini", "ai", "Google", "https://gemini.google.com/",
     "Google's conversational AI assistant."),
    ("Microsoft Copilot", "ai", "Microsoft", "https://copilot.microsoft.com/",
     "Microsoft's AI assistant across Windows and 365."),
    ("Notebook LM", "ai", "Google", "https://notebooklm.google.com/",
     "Google's AI research and note-taking tool."),
    ("Be My Eyes", "ai", "Be My Eyes", "https://www.bemyeyes.com/",
     "Visual-assistance app with AI image descriptions."),
    ("Audacity", "media", "Muse Group", "https://www.audacityteam.org/",
     "Free, open-source audio editor."),
    ("REAPER", "media", "Cockos", "https://www.reaper.fm/",
     "Digital audio workstation used here for video editing."),
    ("Victor Reader Stream", "reading", "HumanWare",
     "https://www.humanware.com/", "Handheld accessible media player."),
    ("Voice Dream Reader", "reading", "Voice Dream", "https://www.voicedream.com/",
     "Text-to-speech reading app."),
    ("Bookshare", "reading", "Benetech", "https://www.bookshare.org/",
     "Accessible online library for people with print disabilities."),
]

# Extra contact details for makers/author/publisher, grounded in the book
# (author and publisher) or each entity's official site. Keyed by enterprise.
c_dOrgInfo = {
    "The Carroll Center for the Blind": dict(
        url="https://carroll.org/", address1="770 Centre Street",
        city="Newton", state="MA", zip="02468", nation="United States"),
    "Freedom Scientific": dict(url="https://www.freedomscientific.com/"),
    "NV Access": dict(url="https://www.nvaccess.org/"),
    "Microsoft": dict(url="https://www.microsoft.com/"),
    "Google": dict(url="https://www.google.com/"),
    "Mozilla": dict(url="https://www.mozilla.org/"),
    "Adobe": dict(url="https://www.adobe.com/"),
    "Dropbox": dict(url="https://www.dropbox.com/"),
    "Zoom": dict(url="https://zoom.us/"),
    "OpenAI": dict(url="https://openai.com/"),
    "Anthropic": dict(url="https://www.anthropic.com/"),
    "Google ": dict(url="https://www.google.com/"),
    "Be My Eyes": dict(url="https://www.bemyeyes.com/"),
    "Muse Group": dict(url="https://www.musehub.com/"),
    "Cockos": dict(url="https://www.cockos.com/"),
    "HumanWare": dict(url="https://www.humanware.com/"),
    "Voice Dream": dict(url="https://www.voicedream.com/"),
    "Benetech": dict(url="https://benetech.org/"),
}

# App-name keywords -> canonical app name, longest first so "Google Docs"
# beats "Google" and "Microsoft Edge" beats "Edge".
c_lAppKeywords = [
    ("google classroom", "Google Classroom"), ("google calendar", "Google Calendar"),
    ("google slides", "Google Slides"), ("google sheets", "Google Sheets"),
    ("google docs", "Google Docs"), ("google drive", "Google Drive"),
    ("google forms", "Google Forms"), ("googleforms", "Google Forms"),
    ("google gemini", "Google Gemini"), ("google meet", "Google Meet"),
    ("notebook lm", "Notebook LM"), ("notebooklm", "Notebook LM"),
    ("microsoft teams", "Microsoft Teams"), ("microsoft edge", "Microsoft Edge"),
    ("microsoft copilot", "Microsoft Copilot"), ("powerpoint", "Microsoft PowerPoint"),
    ("be my eyes", "Be My Eyes"), ("victor reader", "Victor Reader Stream"),
    ("voice dream", "Voice Dream Reader"), ("acrobat", "Adobe Acrobat Reader"),
    ("onedrive", "Microsoft OneDrive"), ("dropbox", "Dropbox"),
    ("audacity", "Audacity"), ("reaper", "REAPER"), ("bookshare", "Bookshare"),
    ("chat gpt", "ChatGPT"), ("chatgpt", "ChatGPT"), ("copilot", "Microsoft Copilot"),
    ("gemini", "Google Gemini"), ("claude", "Claude"), ("gmail", "Gmail"),
    ("outlook", "Microsoft Outlook"), ("excel", "Microsoft Excel"),
    ("firefox", "Mozilla Firefox"), ("chrome", "Google Chrome"),
    ("narrator", "Narrator"), ("nvda", "NVDA"), ("jaws", "JAWS"),
    ("zoom", "Zoom"), ("teams", "Microsoft Teams"), ("edge", "Microsoft Edge"),
    ("word", "Microsoft Word"),
]

# The screen readers, by app name. A method is connected to each reader its
# steps are written for (kind with_reader), so a reader is navigable to every
# task, app, and method it touches.
c_lReaders = ["JAWS", "NVDA", "Narrator"]

# Chapter-title keyword -> (short area label, default app for that chapter)
c_lChapterRules = [
    ("jaws, nvda", ("Screen Readers", "JAWS")),
    ("windows environment", ("Windows", "Windows")),
    ("word", ("Word", "Microsoft Word")),
    ("outlook", ("Outlook", "Microsoft Outlook")),
    ("excel", ("Excel", "Microsoft Excel")),
    ("powerpoint", ("PowerPoint", "Microsoft PowerPoint")),
    ("browsing the web", ("Web Browsing", "Google Chrome")),
    ("adobe acrobat", ("PDF Files", "Adobe Acrobat Reader")),
    ("cloud storage", ("Cloud Storage", "Microsoft OneDrive")),
    ("google workspace", ("Google Workspace", "Google Docs")),
    ("zoom, microsoft teams", ("Meetings", "Zoom")),
    ("artificial intelligence", ("AI", "ChatGPT")),
    ("audio and video", ("Audio and Video", "Audacity")),
    ("help and learning", ("Help Resources", None)),
]


# ---------------------------------------------------------------------
# Document acquisition: yield (level, kind, text) where kind is
# 'chapter'|'section'|'subsection'|'step'|'prose'.
# ---------------------------------------------------------------------

def lLoadParagraphs(sPath):
    if sPath.lower().endswith(".docx"):
        import docx
        doc = docx.Document(sPath)
        out = []
        for p in doc.paragraphs:
            sStyle = (p.style.name or "").strip()
            sLow = sStyle.lower()
            sText = (p.text or "").strip()
            if not sText:
                continue
            bList = sLow.startswith("list") or (
                p._p.pPr is not None and p._p.pPr.numPr is not None)
            if sLow == "heading 1":
                out.append(("chapter", sText))
            elif sLow == "heading 2":
                out.append(("section", sText))
            elif sLow in ("heading 3", "heading 4"):
                out.append(("subsection", sText))
            elif bList:
                out.append(("step", sText))
            else:
                out.append(("prose", sText))
        return out
    # Pre-extracted markdown/text fallback.
    out = []
    for sRaw in open(sPath, encoding="utf-8"):
        s = sRaw.rstrip("\n")
        t = s.strip()
        if not t:
            continue
        if t.startswith("# "):
            out.append(("chapter", t[2:].strip()))
        elif t.startswith("## "):
            out.append(("section", t[3:].strip()))
        elif t.startswith("### ") or t.startswith("#### "):
            out.append(("subsection", t.lstrip("#").strip()))
        elif t.startswith("- ") or t.startswith("* ") or re.match(r"^\d+[.)]\s", t):
            out.append(("step", re.sub(r"^([-*]\s+|\d+[.)]\s+)", "", t)))
        else:
            out.append(("prose", t))
    return out


# ---------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------

def sStripNumber(sHeading):
    # Remove a leading section number like "12.4.1.1", "a1.7", "2.5 7",
    # "13.3.1 .2", keeping the title that follows.
    s = re.sub(r"^\s*[A-Za-z]?\d[\d.\s]*\s+", "", sHeading).strip()
    return s or sHeading.strip()

def sClean(s):
    return re.sub(r"\s+", " ", (s or "")).strip()

def sDetectApp(sText, sDefault):
    low = " " + (sText or "").lower() + " "
    for sKey, sApp in c_lAppKeywords:
        if sKey in low:
            return sApp
    return sDefault

def sChapterRule(sChapterTitle):
    low = (sChapterTitle or "").lower()
    for sKey, tRule in c_lChapterRules:
        if sKey in low:
            return tRule
    return (None, None)

def sChapterArea(sChapterTitle):
    # "Chapter 6 PowerPoint" -> "PowerPoint"; strip a leading
    # "Chapter N" / "Appendix N" label.
    s = re.sub(r"^\s*(Chapter|Appendix)\s+[IVXLC0-9]+\s*", "", sChapterTitle or "").strip()
    return s or (sChapterTitle or "").strip()


# ---------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------

def buildDatabase(sInput, sOutput):
    import os
    if os.path.exists(sOutput):
        os.remove(sOutput)
    conn = sqlite3.connect(sOutput)
    createSchema(conn)
    cur = conn.cursor()

    lParas = lLoadParagraphs(sInput)

    # --- apps + maker contacts ---
    dAppByName = {}
    setOrg = set()
    lContacts = []   # (first, middle, last, enterprise, job, info-dict)
    for sName, sCat, sMaker, sUrl, sDescrip in c_lApps:
        cur.execute("INSERT INTO apps (name, category, descrip, url) VALUES (?,?,?,?)",
                    (sName, sCat, sDescrip, sUrl))
        dAppByName[sName] = sMaker
        if sMaker and sMaker not in setOrg:
            setOrg.add(sMaker)
    # Author + publisher (from the book's title pages).
    lContacts.append(("David", "", "Kingsbury", "The Carroll Center for the Blind",
                      "Assistive Technology Instructor", {}))
    setOrg.add("The Carroll Center for the Blind")
    for sOrg in sorted(setOrg):
        info = c_dOrgInfo.get(sOrg, {})
        lContacts.append(("", "", "", sOrg, "", info))
    setContact = set()
    for (sF, sM, sL, sEnt, sJob, info) in lContacts:
        key = (sF, sM, sL) if sL else sEnt
        if key in setContact:
            continue
        setContact.add(key)
        cols = dict(first_name=sF or None, middle_name=sM or None, last_name=sL or None,
                    enterprise=sEnt or None, job=sJob or None)
        cols.update(info)
        names = [k for k in cols if cols[k] not in (None, "")]
        cur.execute("INSERT INTO contacts (%s) VALUES (%s)"
                    % (",".join(names), ",".join("?" * len(names))),
                    tuple(cols[k] for k in names))

    # --- maps deduper ---
    setMap = set()
    def addMap(t1, u1, kind, t2, u2):
        k = (t1, u1, kind, t2, u2)
        if not u1 or not u2 or k in setMap:
            return
        setMap.add(k)
        cur.execute("INSERT INTO maps (tbl1,unq1,kind,tbl2,unq2) VALUES (?,?,?,?,?)", k)

    # maker makes app (contact.unq for an org is its enterprise; app.unq is name)
    for sName, sMaker in dAppByName.items():
        addMap("contacts", sMaker, "makes", "apps", sName)

    # --- walk the document for tasks + methods ---
    sChapter = sArea = sDefApp = None
    sSubName = sSubApp = None
    sTaskDesc = None
    lSteps = []
    setTask = set()
    dMethodName = {}   # base name -> count, to keep method names unique
    bSkip = False      # true inside appendices (glossary, keystroke lists, etc.)

    def flushMethod():
        nonlocal lSteps, sSubName, sSubApp, sTaskDesc
        if sSubName is None:
            lSteps = []
            return
        # Register the task (app-agnostic, deduped by name).
        if sSubName not in setTask:
            setTask.add(sSubName)
            cur.execute("INSERT INTO tasks (name, area, descrip) VALUES (?,?,?)",
                        (sSubName, sArea, (sTaskDesc or None)))
        # A method exists only if the subsection had procedure steps.
        if lSteps:
            # Append the app for uniqueness/clarity, unless the heading
            # already names it (so "Opening JAWS" does not become
            # "Opening JAWS in JAWS").
            bNameHasApp = bool(sSubApp) and sSubApp.lower() in sSubName.lower()
            sBase = sSubName + ((" in " + sSubApp) if (sSubApp and not bNameHasApp) else "")
            n = dMethodName.get(sBase, 0) + 1
            dMethodName[sBase] = n
            sMethodName = sBase if n == 1 else "%s (%d)" % (sBase, n)
            sSteps = "\n".join("- " + s for s in lSteps[:40])
            cur.execute("INSERT INTO methods (name, summary, steps) VALUES (?,?,?)",
                        (sMethodName, sSubName + ((" (" + sSubApp + ")") if sSubApp else ""),
                         sSteps))
            addMap("methods", sMethodName, "for_task", "tasks", sSubName)
            if sSubApp:
                addMap("methods", sMethodName, "with_app", "apps", sSubApp)
            # Connect every screen reader the steps are actually written for,
            # so a reader becomes a node joined to all the tasks, apps, and
            # methods it appears in. Skip a reader that is already this
            # method's app (the Chapter-1 procedures, where the reader IS the
            # app, are linked via with_app instead).
            sScan = sSubName + " " + sSteps
            for sReader in c_lReaders:
                if sReader == sSubApp:
                    continue
                if re.search(r"\b" + sReader + r"\b", sScan, re.IGNORECASE):
                    addMap("methods", sMethodName, "with_reader", "apps", sReader)
        lSteps = []
        sTaskDesc = None

    for (kind, sText) in lParas:
        if kind == "chapter":
            flushMethod()
            sSubName = None
            sChapter = sClean(sText)
            bSkip = sChapter.lower().startswith("appendix")
            rArea, sDefApp = sChapterRule(sChapter)
            sArea = rArea or sChapterArea(sChapter)
        elif kind == "section":
            flushMethod()
            sSubName = None
            # A section can also name an app (e.g. "13.2 Audio Editing with
            # Audacity"); remember it as a hint for its subsections.
            sSecApp = sDetectApp(sStripNumber(sText), None)
            sDefApp = sSecApp or sDefApp
        elif kind == "subsection":
            flushMethod()
            if bSkip:
                sSubName = None
                continue
            sSubName = sStripNumber(sClean(sText))
            sSubApp = sDetectApp(sSubName, sDefApp)
            sTaskDesc = None
            lSteps = []
        elif kind == "step":
            if sSubName is not None:
                lSteps.append(sClean(sText))
        elif kind == "prose":
            if sSubName is not None and sTaskDesc is None and len(sText) > 40:
                sTaskDesc = sClean(sText)[:600]
    flushMethod()

    # --- lookups: kinds and categories ---
    lLk = [
        ("PRIMER", "maps", "kind", "makes", 1,
         "Subject contact (a company or person) makes or maintains the object app."),
        ("PRIMER", "maps", "kind", "for_task", 2,
         "Subject method accomplishes the object task."),
        ("PRIMER", "maps", "kind", "with_app", 3,
         "Subject method is carried out using the object app."),
        ("PRIMER", "maps", "kind", "with_reader", 4,
         "Subject method is written for the object screen reader (JAWS, NVDA, or Narrator)."),
    ]
    lCats = sorted(set(a[1] for a in c_lApps))
    for i, sCat in enumerate(lCats):
        lLk.append(("PRIMER", "apps", "category", sCat, i + 1,
                    "App category: " + sCat.replace("_", " ") + "."))
    for t in lLk:
        cur.execute("INSERT OR IGNORE INTO lookups (src,tbl,fld,val,ordinal,descrip) "
                    "VALUES (?,?,?,?,?,?)", t)

    conn.commit()
    addUnqIndexes(conn)
    conn.commit()

    # --- report ---
    for sTable in ("contacts", "apps", "tasks", "methods", "maps", "lookups"):
        n = cur.execute('SELECT COUNT(*) FROM "%s"' % sTable).fetchone()[0]
        print("  %-9s %d" % (sTable, n))
    for sKind in ("makes", "for_task", "with_app"):
        n = cur.execute("SELECT COUNT(*) FROM maps WHERE kind=?", (sKind,)).fetchone()[0]
        print("  maps.%-9s %d" % (sKind, n))
    conn.close()


if __name__ == "__main__":
    if len(sys.argv) < 2:
        raise SystemExit("usage: build_tutorials.py <primer.docx|.txt> [out.db]")
    buildDatabase(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else c_sDefaultOutput)
