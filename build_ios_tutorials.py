#!/usr/bin/env python3
# build_ios_tutorials.py - build iOSTutorials.db from "Personal Power:
# Getting the Most From iOS as a Totally Blind User" (Michael Feir).
#
# Same standard schema and conventions as build_windows_tutorials.py, with
# one deliberate difference: VoiceOver is the only screen reader in this
# guide, so it is NOT modeled as an app and there is no with_reader map --
# every method is implicitly performed with VoiceOver. Content about
# VoiceOver itself maps to the iOS app.
#
#   contacts - the author and the makers of the apps (Apple plus many
#              third parties), with their web addresses.
#   apps     - the iOS apps the guide teaches, each with a category.
#   tasks    - app-agnostic things the guide explains how to do, from its
#              subsection headings.
#   methods  - the numbered steps for one task with one app.
#   maps     - typed associations by (table, unq) pairs. Kinds used here:
#              makes (contact -> app), for_task (method -> task),
#              with_app (method -> app).
#   lookups  - valid maps.kind values and apps.category values.
#
# Usage: python build_ios_tutorials.py <guide.docx|.txt> [output.db]

import re
import sqlite3
import sys

c_sDefaultOutput = "iOSTutorials.db"

# ---------------------------------------------------------------------
# Schema (identical to the Windows builder, minus screen-reader handling)
# ---------------------------------------------------------------------

def sLookExpr(lCols):
    return "rtrim(" + " || ".join(
        "iif(%s IS NOT NULL AND length(CAST(%s AS TEXT))>0, CAST(%s AS TEXT) || ' | ', '')"
        % (c, c, c) for c in lCols) + ", ' | ')"

def sUnqExpr(lCols):
    return "||'|'||".join("coalesce(CAST(%s AS TEXT),'')" % c for c in lCols)

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
              "state TEXTLINE", "zip TEXTLINE", "nation TEXTLINE", "url TEXTLINE"],
        look=["first_name", "last_name", "enterprise"], unq=c_sContactUnqExpr),
    "apps": dict(
        data=["name TEXTLINE", "category TEXTLINE", "descrip TEXTMARKDOWN", "url TEXTLINE"],
        look=["name", "category"], unq=["name"]),
    "tasks": dict(
        data=["name TEXTLINE", "area TEXTLINE", "descrip TEXTMARKDOWN", "url TEXTLINE"],
        look=["name", "area"], unq=["name"]),
    "methods": dict(
        data=["name TEXTLINE", "summary TEXTLINE", "steps TEXTMARKDOWN", "url TEXTLINE"],
        look=["name"], unq=["name"]),
    "maps": dict(
        data=["tbl1 TEXTLINE", "unq1 TEXTLINE", "kind TEXTLINE", "tbl2 TEXTLINE", "unq2 TEXTLINE"],
        look=["tbl1", "unq1", "kind", "tbl2", "unq2"],
        unq=["tbl1", "unq1", "kind", "tbl2", "unq2"]),
    "lookups": dict(
        data=["src TEXTLINE", "tbl TEXTLINE", "fld TEXTLINE", "val TEXTLINE",
              "ordinal INTEGER", "descrip TEXTMARKDOWN", "url TEXTLINE"],
        look=["src", "tbl", "fld", "val"], unq=["src", "tbl", "fld", "val"]),
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
# Curated apps and makers (grounded in the guide's coverage). URLs are the
# entities' official sites. (app name, category, maker, app url, description)
# ---------------------------------------------------------------------

c_lApps = [
    # Apple operating system and built-in apps.
    ("iOS", "operating_system", "Apple", "https://www.apple.com/ios/",
     "Apple's operating system for iPhone, where VoiceOver lives."),
    ("Safari", "browser", "Apple", "https://www.apple.com/safari/", "Apple's web browser."),
    ("Apple Mail", "communication", "Apple", "https://support.apple.com/mail", "Apple's email app."),
    ("Messages", "communication", "Apple", "https://support.apple.com/messages", "Apple's texting app (iMessage and SMS)."),
    ("FaceTime", "communication", "Apple", "https://support.apple.com/facetime", "Apple's video and audio calling app."),
    ("Apple Maps", "navigation", "Apple", "https://www.apple.com/maps/", "Apple's maps and navigation app."),
    ("Apple Music", "music", "Apple", "https://www.apple.com/apple-music/", "Apple's music streaming service."),
    ("Apple Books", "reading", "Apple", "https://www.apple.com/apple-books/", "Apple's e-book and audiobook app."),
    ("Apple News", "news", "Apple", "https://www.apple.com/apple-news/", "Apple's news aggregator."),
    ("Apple Podcasts", "media", "Apple", "https://www.apple.com/apple-podcasts/", "Apple's podcast app."),
    ("Apple TV", "media", "Apple", "https://www.apple.com/apple-tv-app/", "Apple's video streaming app."),
    ("Notes", "productivity", "Apple", "https://support.apple.com/notes", "Apple's note-taking app."),
    ("Reminders", "productivity", "Apple", "https://support.apple.com/guide/iphone/reminders", "Apple's to-do and reminders app."),
    ("Calendar", "productivity", "Apple", "https://support.apple.com/guide/iphone/calendar", "Apple's calendar app."),
    ("Camera", "photography", "Apple", "https://support.apple.com/guide/iphone/camera", "Apple's camera app."),
    ("Photos", "photography", "Apple", "https://www.apple.com/ios/photos/", "Apple's photo library app."),
    ("Weather", "utilities", "Apple", "https://support.apple.com/guide/iphone/weather", "Apple's weather app."),
    ("Wallet", "utilities", "Apple", "https://www.apple.com/wallet/", "Apple's app for cards, passes, and payments."),
    ("Health", "health", "Apple", "https://www.apple.com/ios/health/", "Apple's health and fitness app."),
    ("Voice Memos", "utilities", "Apple", "https://support.apple.com/guide/iphone/voice-memos", "Apple's audio recorder."),
    ("Find My", "utilities", "Apple", "https://www.apple.com/icloud/find-my/", "Apple's device- and people-finding app."),
    ("Siri", "ai", "Apple", "https://www.apple.com/siri/", "Apple's voice assistant."),
    ("Voice Control", "utilities", "Apple", "https://support.apple.com/guide/iphone/voice-control", "Apple's voice command system."),
    ("App Store", "utilities", "Apple", "https://www.apple.com/app-store/", "Apple's store for downloading apps."),
    ("iTunes Store", "media", "Apple", "https://www.apple.com/itunes/", "Apple's store for music and video purchases."),
    ("Pages", "productivity", "Apple", "https://www.apple.com/pages/", "Apple's word processor."),
    ("Numbers", "productivity", "Apple", "https://www.apple.com/numbers/", "Apple's spreadsheet app."),
    ("Keynote", "productivity", "Apple", "https://www.apple.com/keynote/", "Apple's presentation app."),
    # Accessibility and assistive apps.
    ("Seeing AI", "accessibility", "Microsoft", "https://www.seeingai.com/", "Microsoft's talking camera for the blind."),
    ("Be My Eyes", "accessibility", "Be My Eyes", "https://www.bemyeyes.com/", "Connects blind users to sighted volunteers and AI."),
    ("Aira", "accessibility", "Aira", "https://aira.io/", "On-demand professional visual assistance."),
    ("Voice Dream Reader", "reading", "Voice Dream", "https://www.voicedream.com/", "Text-to-speech reading app."),
    ("BlindSquare", "navigation", "MIPsoft", "https://www.blindsquare.com/", "Accessible GPS navigation for the blind."),
    # Third-party mainstream apps the guide covers.
    ("Audible", "reading", "Audible", "https://www.audible.com/", "Amazon's audiobook service."),
    ("Kindle", "reading", "Amazon", "https://www.amazon.com/kindle", "Amazon's e-book reading app."),
    ("Bookshare", "reading", "Benetech", "https://www.bookshare.org/", "Accessible library for people with print disabilities."),
    ("Goodreads", "reading", "Amazon", "https://www.goodreads.com/", "Book cataloging and recommendations."),
    ("Overcast", "media", "Overcast", "https://overcast.fm/", "A popular third-party podcast app."),
    ("Castro", "media", "Castro", "https://castro.fm/", "A third-party podcast app."),
    ("Spotify", "music", "Spotify", "https://www.spotify.com/", "Music and podcast streaming service."),
    ("YouTube", "media", "Google", "https://www.youtube.com/", "Google's video sharing service."),
    ("Netflix", "media", "Netflix", "https://www.netflix.com/", "Video streaming service."),
    ("Amazon", "shopping", "Amazon", "https://www.amazon.com/", "Online shopping app."),
    ("Uber", "navigation", "Uber", "https://www.uber.com/", "Ride-hailing app."),
    ("Facebook", "social_media", "Meta", "https://www.facebook.com/", "Social network from Meta."),
    ("Mastodon", "social_media", "Mastodon", "https://joinmastodon.org/", "Decentralized social network."),
    ("Zoom", "communication", "Zoom", "https://zoom.us/", "Video meeting app."),
    ("Dropbox", "cloud_storage", "Dropbox", "https://www.dropbox.com/", "Cloud file storage."),
    ("ChatGPT", "ai", "OpenAI", "https://chatgpt.com/", "OpenAI's conversational AI assistant."),
    ("Google Gemini", "ai", "Google", "https://gemini.google.com/", "Google's conversational AI assistant."),
    ("AppleVis", "resource", "AppleVis", "https://www.applevis.com/",
     "Community website with guides and app directories for blind Apple users."),
]

# Official maker home pages (the contact's url), set carefully.
c_dOrgInfo = {
    "Apple": dict(url="https://www.apple.com/"),
    "Microsoft": dict(url="https://www.microsoft.com/"),
    "Be My Eyes": dict(url="https://www.bemyeyes.com/"),
    "Aira": dict(url="https://aira.io/"),
    "Voice Dream": dict(url="https://www.voicedream.com/"),
    "MIPsoft": dict(url="https://www.blindsquare.com/"),
    "Audible": dict(url="https://www.audible.com/"),
    "Benetech": dict(url="https://benetech.org/"),
    "Amazon": dict(url="https://www.amazon.com/"),
    "Overcast": dict(url="https://overcast.fm/"),
    "Castro": dict(url="https://castro.fm/"),
    "Spotify": dict(url="https://www.spotify.com/"),
    "Google": dict(url="https://www.google.com/"),
    "Netflix": dict(url="https://www.netflix.com/"),
    "Meta": dict(url="https://www.meta.com/"),
    "Mastodon": dict(url="https://joinmastodon.org/"),
    "Zoom": dict(url="https://zoom.us/"),
    "Dropbox": dict(url="https://www.dropbox.com/"),
    "OpenAI": dict(url="https://openai.com/"),
    "Uber": dict(url="https://www.uber.com/"),
    "AppleVis": dict(url="https://www.applevis.com/"),
}

# App-name keywords -> canonical app, longest first.
c_lAppKeywords = [
    ("apple music", "Apple Music"), ("apple books", "Apple Books"), ("apple news", "Apple News"),
    ("apple podcasts", "Apple Podcasts"), ("apple maps", "Apple Maps"), ("apple tv", "Apple TV"),
    ("app store", "App Store"), ("itunes store", "iTunes Store"), ("itunes", "iTunes Store"),
    ("voice dream", "Voice Dream Reader"), ("be my eyes", "Be My Eyes"), ("seeing ai", "Seeing AI"),
    ("blindsquare", "BlindSquare"), ("applevis", "AppleVis"), ("voice control", "Voice Control"),
    ("voice memos", "Voice Memos"), ("find my", "Find My"), ("facetime", "FaceTime"),
    ("google gemini", "Google Gemini"), ("chatgpt", "ChatGPT"), ("gemini", "Google Gemini"),
    ("goodreads", "Goodreads"), ("bookshare", "Bookshare"), ("kindle", "Kindle"), ("audible", "Audible"),
    ("overcast", "Overcast"), ("castro", "Castro"), ("spotify", "Spotify"),
    ("youtube", "YouTube"), ("netflix", "Netflix"), ("mastodon", "Mastodon"),
    ("facebook", "Facebook"), ("dropbox", "Dropbox"), ("amazon", "Amazon"),
    ("uber", "Uber"), ("zoom", "Zoom"), ("keynote", "Keynote"), ("numbers", "Numbers"),
    ("pages", "Pages"), ("wallet", "Wallet"), ("reminders", "Reminders"), ("calendar", "Calendar"),
    ("weather", "Weather"), ("camera", "Camera"), ("photos", "Photos"), ("health", "Health"),
    ("messages", "Messages"), ("safari", "Safari"), ("siri", "Siri"), ("notes", "Notes"),
    ("books", "Apple Books"), ("news", "Apple News"), ("podcast", "Apple Podcasts"),
    ("aira", "Aira"), ("mail", "Apple Mail"), ("maps", "Apple Maps"), ("music", "Apple Music"),
]

# Chapter (H1) keyword -> default app for that thematic section.
c_lChapterApp = [
    ("safari", "Safari"), ("apple music", "Apple Music"), ("itunes", "Apple Music"),
    ("reading books", "Apple Books"), ("news app", "Apple News"), ("app store", "App Store"),
    ("camera", "Camera"), ("siri", "Siri"), ("voice control", "Voice Control"),
    ("podcasts", "Apple Podcasts"), ("internet radio", "Apple Podcasts"),
    ("tv app", "Apple TV"), ("streaming", "Apple TV"), ("health", "Health"),
]

# Front-matter / non-task H1s to skip entirely.
c_lSkipChapters = ["personal power", "table of contents", "acknowledgement",
                   "changes in the", "introduction", "why i wrote", "fond farewell"]


# ---------------------------------------------------------------------
# Document acquisition
# ---------------------------------------------------------------------

def lLoadParagraphs(sPath):
    if sPath.lower().endswith(".docx"):
        import docx
        doc = docx.Document(sPath)
        out = []
        for p in doc.paragraphs:
            sLow = (p.style.name or "").strip().lower()
            sText = (p.text or "").strip()
            if not sText:
                continue
            bList = ("list" in sLow) or (p._p.pPr is not None and p._p.pPr.numPr is not None)
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
    out = []
    for sRaw in open(sPath, encoding="utf-8"):
        t = sRaw.rstrip("\n").strip()
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


def sClean(s):
    return re.sub(r"\s+", " ", (s or "")).strip().rstrip(":")

def sDetectApp(sText, sDefault):
    low = " " + (sText or "").lower() + " "
    for sKey, sApp in c_lAppKeywords:
        if sKey in low:
            return sApp
    return sDefault

def sChapterDefaultApp(sTitle):
    low = (sTitle or "").lower()
    for sKey, sApp in c_lChapterApp:
        if sKey in low:
            return sApp
    return "iOS"

def sNormSub(sRaw):
    # Strip a leading "How to[ do it]:" so "How to Do It: Adding a Voice"
    # becomes "Adding a Voice".
    s = sClean(sRaw)
    m = re.match(r"(?i)^how to(?:\s+do\s+it)?\s*[:\-]\s*(.+)$", s)
    if m:
        s = m.group(1).strip()
    return s

def bGenericHeading(s):
    low = s.lower().strip()
    if low in ("how to do it", "how to", "first method", "second method",
               "third method", "fourth method", "another method", "the method"):
        return True
    return bool(re.match(r"^method\s+\d+$", low))


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
    dAppMaker = {}
    setOrg = set()
    for sName, sCat, sMaker, sUrl, sDescrip in c_lApps:
        cur.execute("INSERT INTO apps (name, category, descrip, url) VALUES (?,?,?,?)",
                    (sName, sCat, sDescrip, sUrl))
        dAppMaker[sName] = sMaker
        setOrg.add(sMaker)
    lContacts = [("Michael", "", "Feir", "", "Author", {})]
    for sOrg in sorted(setOrg):
        lContacts.append(("", "", "", sOrg, "", c_dOrgInfo.get(sOrg, {})))
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

    setMap = set()
    def addMap(t1, u1, kind, t2, u2):
        k = (t1, u1, kind, t2, u2)
        if u1 and u2 and k not in setMap:
            setMap.add(k)
            cur.execute("INSERT INTO maps (tbl1,unq1,kind,tbl2,unq2) VALUES (?,?,?,?,?)", k)

    for sName, sMaker in dAppMaker.items():
        addMap("contacts", sMaker, "makes", "apps", sName)

    # --- walk the document for tasks + methods ---
    # Steps attach to the nearest heading: the current subsection (H3/H4) if
    # one has appeared under the current section, otherwise the section (H2)
    # itself, since this guide often numbers steps directly under a section.
    sArea = sDefApp = None
    sSecName = sSecApp = None
    sSubName = sSubApp = None
    sTaskDesc = None
    lSteps = []
    setTask = set()
    dMethodName = {}
    bSkip = False

    def ensureTask(sName, sApp):
        if sName and sName not in setTask:
            setTask.add(sName)
            cur.execute("INSERT INTO tasks (name, area, descrip) VALUES (?,?,?)",
                        (sName, sArea, (sTaskDesc or None)))

    def flushMethod():
        nonlocal lSteps, sTaskDesc
        sName = sSubName if sSubName else sSecName
        sApp = sSubApp if sSubName else sSecApp
        if sName is None or bSkip:
            lSteps = []; sTaskDesc = None
            return
        if lSteps:
            ensureTask(sName, sApp)
            bNameHasApp = bool(sApp) and sApp.lower() in sName.lower()
            sBase = sName + ((" in " + sApp) if (sApp and not bNameHasApp) else "")
            n = dMethodName.get(sBase, 0) + 1
            dMethodName[sBase] = n
            sMethodName = sBase if n == 1 else "%s (%d)" % (sBase, n)
            sSteps = "\n".join("- " + s for s in lSteps[:40])
            cur.execute("INSERT INTO methods (name, summary, steps) VALUES (?,?,?)",
                        (sMethodName, sName + ((" (" + sApp + ")") if sApp else ""), sSteps))
            addMap("methods", sMethodName, "for_task", "tasks", sName)
            if sApp:
                addMap("methods", sMethodName, "with_app", "apps", sApp)
        lSteps = []; sTaskDesc = None

    for (kind, sText) in lParas:
        if kind == "chapter":
            flushMethod(); sSecName = None; sSubName = None
            sTitle = sClean(sText)
            bSkip = any(k in sTitle.lower() for k in c_lSkipChapters)
            sArea = sTitle
            sDefApp = sChapterDefaultApp(sTitle)
        elif kind == "section":
            flushMethod(); sSubName = None
            sSecName = sClean(sText)
            sSecApp = sDetectApp(sSecName, sDefApp)
            sTaskDesc = None
        elif kind == "subsection":
            flushMethod()
            if bSkip:
                sSubName = None
                continue
            sNorm = sNormSub(sText)
            if bGenericHeading(sNorm):
                sSubName = None  # roll steps up to the section heading
            else:
                sSubName = sNorm
                sSubApp = sDetectApp(sSubName, sSecApp or sDefApp)
                ensureTask(sSubName, sSubApp)
            sTaskDesc = None; lSteps = []
        elif kind == "step":
            if (sSubName or sSecName) and not bSkip:
                lSteps.append(sClean(sText))
        elif kind == "prose":
            if (sSubName or sSecName) and sTaskDesc is None and len(sText) > 40:
                sTaskDesc = re.sub(r"\s+", " ", sText).strip()[:600]
    flushMethod()

    # --- lookups ---
    lLk = [
        ("PERSONALPOWER", "maps", "kind", "makes", 1,
         "Subject contact (a company or person) makes or maintains the object app."),
        ("PERSONALPOWER", "maps", "kind", "for_task", 2,
         "Subject method accomplishes the object task."),
        ("PERSONALPOWER", "maps", "kind", "with_app", 3,
         "Subject method is carried out using the object app (always with VoiceOver)."),
    ]
    for i, sCat in enumerate(sorted(set(a[1] for a in c_lApps))):
        lLk.append(("PERSONALPOWER", "apps", "category", sCat, i + 1,
                    "App category: " + sCat.replace("_", " ") + "."))
    for t in lLk:
        cur.execute("INSERT OR IGNORE INTO lookups (src,tbl,fld,val,ordinal,descrip) VALUES (?,?,?,?,?,?)", t)

    conn.commit()
    addUnqIndexes(conn)
    conn.commit()
    for sTable in ("contacts", "apps", "tasks", "methods", "maps", "lookups"):
        print("  %-9s %d" % (sTable, cur.execute('SELECT COUNT(*) FROM "%s"' % sTable).fetchone()[0]))
    for sKind in ("makes", "for_task", "with_app"):
        print("  maps.%-9s %d" % (sKind, cur.execute("SELECT COUNT(*) FROM maps WHERE kind=?", (sKind,)).fetchone()[0]))
    conn.close()


if __name__ == "__main__":
    if len(sys.argv) < 2:
        raise SystemExit("usage: build_ios_tutorials.py <guide.docx|.txt> [out.db]")
    buildDatabase(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else c_sDefaultOutput)
