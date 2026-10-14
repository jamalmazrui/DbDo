"""makeBlindSkills.py -- builds DbDo's BlindSkills template from the Blind Skills directory.

    scripts\\makeBlindSkills                 finds BlindSkills.md under any drive's MyPages folder
    scripts\\makeBlindSkills <file or folder> reads it from where you say

THE DIRECTORY. Blind Skills, Jamal Mazrui's GitHub Page of free videos and podcasts that teach blindness skills -- cane
travel, braille, cooking, dressing, labeling and more -- kept in MyPages\\pages\\BlindSkills\\BlindSkills.md. Each lesson is
a level-three heading under its skill group, linking the lesson itself inline, then
fields in alphabetical order, one to a paragraph: Audience, Description, Duration, Episode page or Source page, Medium,
Presenter, Series, Skill, Source, Structured discovery, Transcript, Year.

WHAT IT MAKES. templates\\BlindSkills\\BlindSkills.db, the Homer way, with the table helpers makeMusicTrail uses (as
makeTechTraining does): one table, lessons, with the standard fields, look, prime and triggers. Its url field is the
lesson's own address, so Play Stream (Alt+Shift+P) plays it in the Homer Player -- MP3 episodes directly, videos through
yt-dlp -- and marked lessons play as a play list. lookups hold the skill groups with the directory's own definition of
each, the skills, the sources, the series, the audiences, the media and the two structured discovery values. skill is
the task a lesson teaches; minutes is its length where the publisher gives it; source_url and transcript_url open
the publisher's page and the transcript with Control+Enter; tags repeat group, skill and source for Keywords.

The old file is replaced only when the new one is complete and passes its own check. The log,
logs\\DbDo-makeBlindSkills-<date>-<time>.log, names every lesson it read.
"""

import datetime, glob, os, platform, re, sqlite3, string, sys, traceback

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import makeMusicTrail as trail

sVersion = "2026-10-14 version 3: reads the FileDir-ready page (inline heading links, one field to a paragraph, Episode page links); no starter marks"
lsFields = ["title", "skill", "category", "year", "minutes", "medium", "audience", "presenter", "series", "source", "structured_discovery",
            "summary", "source_url", "transcript_url"]
dColumnTypes = {"audience": "TEXTLINE", "category": "TEXTLINE", "medium": "TEXTLINE", "minutes": "INTEGER", "presenter": "TEXTLINE",
            "series": "TEXTLINE", "skill": "TEXTLINE", "source": "TEXTLINE", "source_url": "TEXTLINE", "structured_discovery": "TEXTLINE",
            "summary": "TEXTMARKDOWN", "title": "TEXTLINE", "transcript_url": "TEXTLINE", "year": "TEXTLINE"}
# NO STARTER MARKS (14 October 2026): with any record marked, Play Stream plays the marked list rather than the lesson
# the cursor is on, so marks made for a demonstration would hijack the first Alt+Shift+P a person presses. TechTraining
# marks three; here the play list is a person's own, and the whole directory is a play list already (BlindSkills.m3u).
lsStarterMarks = []
dMediaKinds = {"audio": "An audio file, usually a podcast episode: Play Stream (Alt+Shift+P) plays it in the Homer Player.",
            "video": "A video on a host such as YouTube or Vimeo: Play Stream (Alt+Shift+P) plays it in the Homer Player, through yt-dlp."}
dDiscovery = {"Setting": "Made by a training center or agency that teaches by structured discovery.",
            "Subject": "The lesson is about structured discovery itself, the method of learning by guided questions and problem solving."}

sProject = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sLogPath = os.path.join(sProject, "logs", "DbDo-makeBlindSkills-%s.log" % datetime.datetime.now().strftime("%Y%m%d-%H%M%S"))
trail.sLogPath = sLogPath  # the shared helpers write to this log


def log(sText): return trail.log(sText)
def say(sText): return trail.say(sText)


def findDirectory(lsArgs):
    """BlindSkills.md: named, in a folder named, or under any drive's MyPages folder."""
    lsPlaces = lsArgs or [sDrive + ":\\MyPages" for sDrive in string.ascii_uppercase if os.path.isdir(sDrive + ":\\MyPages")]
    for sPlace in lsPlaces:
        if os.path.isfile(sPlace): return sPlace
        lsFound = glob.glob(os.path.join(sPlace, "**", "BlindSkills.md"), recursive=True)
        lsFound = [s for s in lsFound if "notes" not in s.lower().split(os.sep)]
        if lsFound: return sorted(lsFound, key=os.path.getmtime)[-1]
    log("Looked in: " + "; ".join(lsPlaces))
    return ""


def linkOf(sValue, dRefs):
    """The address behind a reference-style link such as [Transcript][tr-x], or the value itself when it is an address."""
    oRef = re.match(r"\[[^\]]*\]\[([^\]]+)\]$", sValue)
    if oRef: return dRefs.get(oRef.group(1), "")
    return sValue if sValue.startswith("http") else ""


def minutesOf(sDuration):
    """A measured '3:56' or '1:04:00', or a rounded 'About 64 minutes', as whole minutes; '' when it is neither."""
    oAbout = re.match(r"About (\d+) minutes?$", sDuration)
    if oAbout: return int(oAbout.group(1))
    lsParts = sDuration.split(":")
    if not sDuration or not all(p.isdigit() for p in lsParts) or len(lsParts) > 3: return ""
    iSeconds = 0
    for sPart in lsParts: iSeconds = iSeconds * 60 + int(sPart)
    return int(round(iSeconds / 60.0))


def readDirectory(sText):
    """Each lesson as a dictionary, and the skill groups with their definitions.

    THE PAGE IS WRITTEN FOR FILEDIR TOO (14 October 2026): each heading links its lesson inline, so a player takes the
    title with the address; each field is its own paragraph, **Field:** value, which FileDir keeps as a fact of the
    track; and a podcast's page is a plain link named Episode page, whose address FileDir keeps. Reference-style links
    -- the episode or source page and the transcript -- are resolved from the definitions at the end of the page."""
    sText = sText.replace("\r\n", "\n")
    dRefs = dict(re.findall(r"(?m)^\[([^\]]+)\]: (\S+)\s*$", sText))
    iEnd = sText.find("\n## Appendix A")
    sBody = sText[:iEnd if iEnd > 0 else len(sText)]
    dCategories = {}
    for oCategory in re.finditer(r"(?m)^## (.+?) \(\d+\) \{#cat-[^}]+\}\n\n(.+)$", sBody):
        dCategories[oCategory.group(1).strip()] = oCategory.group(2).strip()
    lLessons, sCategory = [], ""
    for sChunk in re.split(r"\n(?=## |### )", sBody):
        oCategory = re.match(r"## (.+?) \(\d+\) \{#cat-", sChunk)
        if oCategory: sCategory = oCategory.group(1).strip(); continue
        if sChunk.startswith("## "): sCategory = ""; continue
        oHead = re.match(r"### \[(.+)\]\((https?://[^)\s]+)\) \{#[^}]+\}", sChunk)
        if not oHead or not sCategory: continue
        sTitle, sUrl = oHead.group(1).strip(), oHead.group(2).strip()
        dFields = {sLabel.strip().lower(): sValue.strip() for sLabel, sValue in re.findall(r"(?m)^\*\*([^*]+?):\*\* (.+)$", sChunk)}
        oEpisode = re.search(r"(?m)^\[Episode page\]\[([^\]]+)\]\s*$", sChunk)
        sPage = dRefs.get(oEpisode.group(1), "") if oEpisode else linkOf(dFields.get("source page", ""), dRefs)
        dRow = {"title": sTitle, "skill": dFields.get("skill", ""), "category": sCategory, "year": dFields.get("year", ""),
                "minutes": minutesOf(dFields.get("duration", "")), "medium": dFields.get("medium", "").lower(),
                "audience": dFields.get("audience", ""), "presenter": dFields.get("presenter", ""), "series": dFields.get("series", ""),
                "source": dFields.get("source", ""), "structured_discovery": dFields.get("structured discovery", ""),
                "summary": dFields.get("description", ""), "source_url": sPage,
                "transcript_url": linkOf(dFields.get("transcript", ""), dRefs), "url": sUrl}
        # TAGS, one to a line, so Keywords (Control+K) finds a lesson by its group, task, source or method, as BlindCreations does.
        dRow["tags"] = "\n".join(s for s in [sCategory, dRow["skill"], dRow["source"],
                                             "structured discovery" if dRow["structured_discovery"] else ""] if s)
        lLessons.append(dRow)
    # EVERY LESSON HEADING MUST BE READ: a heading written another way -- a reference-style link, a missing address --
    # would otherwise drop out of the database without a word, and of the play list with it.
    iHeadings = len(re.findall(r"(?m)^### ", sBody))
    if iHeadings != len(lLessons):
        raise RuntimeError("the page has %d lesson headings but %d could be read; each must be ### [Title](address) {#id}" % (iHeadings, len(lLessons)))
    return lLessons, dCategories


def build(sText, sTarget):
    lLessons, dCategories = readDirectory(sText)
    say("Read %d lessons in %d skill groups." % (len(lLessons), len(dCategories)))
    sTemp = sTarget + ".building"
    if os.path.exists(sTemp): os.remove(sTemp)
    os.makedirs(os.path.dirname(sTarget), exist_ok=True)
    oDb = sqlite3.connect(sTemp)
    for sDdl in trail.systemDdl() + trail.tableDdl("lessons", "lesson_id", lsFields, ["title", "year", "source"], ["title", "year"], dColumnTypes):
        oDb.execute(sDdl)
    oDb.execute("CREATE INDEX idx_lessons_category ON lessons (category)")
    oDb.execute("CREATE INDEX idx_lessons_skill ON lessons (skill)")
    lsColumns = lsFields + ["url", "tags"]
    for dRow in sorted(lLessons, key=lambda d: re.sub(r"^(a|an|the)\s+", "", d["title"].lower())):
        oDb.execute("INSERT INTO lessons (%s) VALUES (%s)" % (", ".join('"%s"' % s for s in lsColumns), ", ".join("?" * len(lsColumns))),
                    [dRow[s] if dRow[s] != "" else None for s in lsColumns])
        log("Lesson: %s (%s) | %s | %s | %s" % (dRow["title"], dRow["year"] or "year unknown", dRow["category"], dRow["medium"], dRow["url"]))
    trail.addLookups(oDb, "lessons", "category", dCategories, "BlindSkills")
    trail.addLookups(oDb, "lessons", "source", {d["source"]: "" for d in lLessons if d["source"]}, "BlindSkills")
    trail.addLookups(oDb, "lessons", "series", {d["series"]: "" for d in lLessons if d["series"]}, "BlindSkills")
    trail.addLookups(oDb, "lessons", "audience", {d["audience"]: "" for d in lLessons if d["audience"]}, "BlindSkills")
    trail.addLookups(oDb, "lessons", "medium", dMediaKinds, "BlindSkills")
    trail.addLookups(oDb, "lessons", "skill", {d["skill"]: "" for d in lLessons if d["skill"]}, "BlindSkills")
    trail.addLookups(oDb, "lessons", "structured_discovery", dDiscovery, "BlindSkills")
    for sTable, sSelect, sOrder in (("lookups", "fld, val", "fld, ordinal"), ("maps", "kind, prime1, prime2", "kind, prime1"),
                                    ("lessons", "title, medium, source", "category, title")):
        for sField, sValue in (("SelectFields", sSelect), ("OrderFields", sOrder), ("WhereFilter", "")):
            oDb.execute("INSERT INTO views (tbl, fld, val) VALUES (?, ?, ?)", (sTable, sField, trail.viewFromInix("BlindSkills", sTable, sField, sValue)))
    for sTitle in lsStarterMarks:
        if oDb.execute("UPDATE lessons SET marked = 1 WHERE title = ?", (sTitle,)).rowcount: log("Marked as a starter: " + sTitle)
        else: log("Starter not found, so not marked: " + sTitle)
    oDb.commit()
    sIntegrity = oDb.execute("PRAGMA integrity_check").fetchone()[0]
    dCounts = {s: oDb.execute("SELECT count(*) FROM %s" % s).fetchone()[0] for s in ("lessons", "lookups")}
    dMedia = dict(oDb.execute("SELECT medium, count(*) FROM lessons GROUP BY medium").fetchall())
    iNoSkill = oDb.execute("SELECT count(*) FROM lessons WHERE skill IS NULL").fetchone()[0]
    if iNoSkill: log("Lessons with no skill: %d" % iNoSkill)
    oDb.execute("PRAGMA journal_mode = DELETE")
    oDb.execute("VACUUM")
    oDb.close()
    log("Integrity %s; counts %s; media %s" % (sIntegrity, dCounts, dMedia))
    if sIntegrity != "ok" or dCounts["lessons"] != len(lLessons) or not lLessons or iNoSkill:
        os.remove(sTemp)
        raise RuntimeError("the new database failed its own check (integrity %s, %d of %d lessons, %d with no skill); the old one is kept" % (sIntegrity, dCounts["lessons"], len(lLessons), iNoSkill))
    os.replace(sTemp, sTarget)
    say("Wrote %s: %d lessons (%d audio, %d video), %d pick-list values." % (sTarget, dCounts["lessons"], dMedia.get("audio", 0), dMedia.get("video", 0), dCounts["lookups"]))
    return dCounts


def main(lsArgs):
    log("makeBlindSkills.py started; %s; script %s" % (sVersion, os.path.abspath(__file__)))
    log("Python %s at %s on %s; cwd %s; command line %s" % (platform.python_version(), sys.executable, platform.platform(), os.getcwd(), " ".join(sys.argv)))
    sSource = findDirectory(lsArgs)
    log("Directory: %s" % (sSource or "not found"))
    if not sSource:
        say("BlindSkills.md was not found. Name it, or its folder, after makeBlindSkills.")
        return 1
    say("Reading " + sSource)
    with open(sSource, encoding="utf-8-sig") as fSource: sText = fSource.read()
    build(sText, os.path.join(sProject, "templates", "BlindSkills", "BlindSkills.db"))
    say("Log: " + sLogPath)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except Exception as oError:
        log("CRASH " + traceback.format_exc())
        print("makeBlindSkills stopped: %s. The log is %s." % (oError, sLogPath))
        sys.exit(1)
