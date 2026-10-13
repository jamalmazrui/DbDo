"""makeTechTraining.py -- builds DbDo's TechTraining template from the Tech Training directory.

    scripts\\makeTechTraining                 finds TechTraining.md under any drive's MyPages folder
    scripts\\makeTechTraining <file or folder> reads it from where you say

THE DIRECTORY. Tech Training, Jamal Mazrui's GitHub Page of free audio and video presentations on technology and
accessibility, kept in MyPages\\pages\\TechTraining\\TechTraining.md: about 1,170 presentations in 20 topic
categories, each a level-three heading linking the presentation itself, with its year, and labelled lines for its
description, length, presenter, series and source, then any further links (slides, transcripts, handouts).

WHAT IT MAKES. templates\\TechTraining\\TechTraining.db, the Homer way (the homer-db skill), with the table helpers
makeMusicTrail uses: one table, presentations, with the standard fields, look, prime and triggers. Its url field is
the presentation's own address, so Play Stream (Alt+Shift+P) plays it in the Homer Player -- audio files directly,
YouTube and the like through yt-dlp -- and marked presentations play as a play list. medium says which: audio, video,
or page (a presentation page, opened in the browser with Control+Enter on its url). minutes is the length in whole
minutes, so lessons can be sorted and filtered by time. lookups hold the categories with the directory's own
definition of each, the sources, the series and the media.

The old file is replaced only when the new one is complete and passes its own check. The log,
logs\\DbDo-makeTechTraining-<date>-<time>.log, names every entry it read and every one it could not.
"""

import datetime, glob, os, platform, re, sqlite3, string, sys, traceback

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import makeMusicTrail as trail

sVersion = "2026-10-12 version 1: from the Tech Training directory; Play Stream plays each presentation"
lsFields = ["title", "year", "category", "minutes", "medium", "presenter", "series", "source", "summary", "source_url", "resources"]
dColumnTypes = {"category": "TEXTLINE", "medium": "TEXTLINE", "minutes": "INTEGER", "presenter": "TEXTLINE", "resources": "TEXTMARKDOWN",
            "series": "TEXTLINE", "source": "TEXTLINE", "source_url": "TEXTLINE", "summary": "TEXTMARKDOWN", "title": "TEXTLINE", "year": "TEXTLINE"}
lsStarterMarks = ["Convert PowerPoint to PDF and Maintain Document Structure", "Quiet the Noise: Managing VoiceOver Sensory Overload on iOS",
                    "Transform PDFs with AI-Powered OCR: Your Guide to PDFgear for Mac"]
dMediaKinds = {"audio": "An audio file: Play Stream (Alt+Shift+P) plays it in the Homer Player.",
            "page": "A presentation page: Control+Enter on its url opens it in the browser, where it plays.",
            "video": "A video on a host such as YouTube: Play Stream (Alt+Shift+P) plays it in the Homer Player, through yt-dlp."}

sProject = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sLogPath = os.path.join(sProject, "logs", "DbDo-makeTechTraining-%s.log" % datetime.datetime.now().strftime("%Y%m%d-%H%M%S"))
trail.sLogPath = sLogPath  # the shared helpers write to this log


def log(sText): return trail.log(sText)
def say(sText): return trail.say(sText)


def findDirectory(lsArgs):
    """TechTraining.md: named, in a folder named, or under any drive's MyPages folder."""
    lsPlaces = lsArgs or [sDrive + ":\\MyPages" for sDrive in string.ascii_uppercase if os.path.isdir(sDrive + ":\\MyPages")]
    for sPlace in lsPlaces:
        if os.path.isfile(sPlace): return sPlace
        lsFound = glob.glob(os.path.join(sPlace, "**", "TechTraining.md"), recursive=True)
        lsFound = [s for s in lsFound if "notes" not in s.lower().split(os.sep)]
        if lsFound: return sorted(lsFound, key=os.path.getmtime)[-1]
    log("Looked in: " + "; ".join(lsPlaces))
    return ""


def minutesOf(sLength):
    """'42 min', '1 hour 5 min', '2 hours' as whole minutes; None when it says nothing countable."""
    oHours = re.search(r"(\d+)\s*hours?", sLength or "")
    oMinutes = re.search(r"(\d+)\s*min", sLength or "")
    if not oHours and not oMinutes: return None
    return (int(oHours.group(1)) * 60 if oHours else 0) + (int(oMinutes.group(1)) if oMinutes else 0)


def mediumOf(sUrl):
    sLower = sUrl.lower().split("?")[0]
    if sLower.endswith((".mp3", ".m4a", ".aac", ".ogg", ".opus", ".wav")): return "audio"
    if re.search(r"(youtube\.com|youtu\.be|vimeo\.com)/", sLower): return "video"
    return "page"


def readDirectory(sText):
    """Each presentation as a dictionary, the category definitions and the source names."""
    sText = sText.replace("\r\n", "\n")
    iEnd = sText.find("\n## Appendix A")
    sBody = sText[:iEnd if iEnd > 0 else len(sText)]
    dCategories = {}
    for oCategory in re.finditer(r"(?m)^## (.+?) \(\d+\) \{#[^}]+\}\n\n(.+)$", sBody):
        dCategories[oCategory.group(1).strip()] = oCategory.group(2).strip()
    dSources = {s.strip(): "" for s in re.findall(r"(?m)^- (.+?) \(\d+\)$", sBody[:sBody.find("\n## Table of Contents") if "\n## Table of Contents" in sBody else len(sBody)])}
    lPresentations, sCategory = [], ""
    for sChunk in re.split(r"\n(?=## |### )", sBody):
        oCategory = re.match(r"## (.+?) \(\d+\) \{#", sChunk)
        if oCategory: sCategory = oCategory.group(1).strip(); continue
        oHead = re.match(r"### \[(.+)\]\((\S+)\) \{#[^}]+\}", sChunk)
        if not oHead: continue
        sTitle, sUrl = oHead.group(1).strip(), oHead.group(2).strip()
        oYear = re.search(r"\s*\((\d{4}|Year unknown)\)$", sTitle)
        sYear = oYear.group(1) if oYear and oYear.group(1) != "Year unknown" else ""
        if oYear: sTitle = sTitle[:oYear.start()].strip()
        dFields = {}
        for sLabel, sValue in re.findall(r"(?m)^\*\*([^*]+?):\*\* (.+)$", sChunk): dFields[sLabel.strip().lower()] = sValue.strip()
        oSource = re.match(r"\[(.+?)\]\((\S+)\)", dFields.get("source", ""))
        lsResources = [s.strip() for s in re.findall(r"(?m)^- (\[.+\]\(\S+\).*)$", sChunk)]
        lPresentations.append({"title": sTitle, "year": sYear, "category": sCategory, "minutes": minutesOf(dFields.get("length", "")),
                               "medium": mediumOf(sUrl), "presenter": dFields.get("presenter", dFields.get("presenter(s)", "")),
                               "series": dFields.get("series", ""), "source": oSource.group(1) if oSource else dFields.get("source", ""),
                               "summary": dFields.get("description", ""), "source_url": oSource.group(2) if oSource else "",
                               "resources": "\n".join(lsResources), "url": sUrl})
    return lPresentations, dCategories, dSources


def build(sText, sTarget):
    lPresentations, dCategories, dSources = readDirectory(sText)
    say("Read %d presentations in %d categories, from %d sources." % (len(lPresentations), len(dCategories), len(dSources)))
    sTemp = sTarget + ".building"
    if os.path.exists(sTemp): os.remove(sTemp)
    os.makedirs(os.path.dirname(sTarget), exist_ok=True)
    oDb = sqlite3.connect(sTemp)
    for sDdl in trail.systemDdl() + trail.tableDdl("presentations", "presentation_id", lsFields, ["title", "year", "source"], ["title", "year"], dColumnTypes):
        oDb.execute(sDdl)
    oDb.execute("CREATE INDEX idx_presentations_category ON presentations (category)")
    oDb.execute("CREATE INDEX idx_presentations_minutes ON presentations (minutes)")
    lsColumns = lsFields + ["url"]
    for dRow in sorted(lPresentations, key=lambda d: re.sub(r"^(a|an|the)\s+", "", d["title"].lower())):
        oDb.execute("INSERT INTO presentations (%s) VALUES (%s)" % (", ".join('"%s"' % s for s in lsColumns), ", ".join("?" * len(lsColumns))),
                    [dRow[s] if dRow[s] not in ("",) else None for s in lsColumns])
        log("Presentation: %s (%s) | %s | %s | %s" % (dRow["title"], dRow["year"] or "year unknown", dRow["category"], dRow["medium"], dRow["url"]))
    for sSource in {d["source"] for d in lPresentations if d["source"]}: dSources.setdefault(sSource, "")
    trail.addLookups(oDb, "presentations", "category", dCategories, "TechTraining")
    trail.addLookups(oDb, "presentations", "source", dSources, "TechTraining")
    trail.addLookups(oDb, "presentations", "series", {d["series"]: "" for d in lPresentations if d["series"]}, "TechTraining")
    trail.addLookups(oDb, "presentations", "medium", dMediaKinds, "TechTraining")
    for sTable, sSelect, sOrder in (("lookups", "fld, val", "fld, ordinal"), ("maps", "kind, prime1, prime2", "kind, prime1"),
                                    ("presentations", "title, minutes, medium", "title")):
        for sField, sValue in (("SelectFields", sSelect), ("OrderFields", sOrder), ("WhereFilter", "")):
            oDb.execute("INSERT INTO views (tbl, fld, val) VALUES (?, ?, ?)", (sTable, sField, trail.viewFromInix("TechTraining", sTable, sField, sValue)))
    # A STARTER LISTENING LIST: three short presentations come marked, so the listening_list report and a play list
    # from Play Stream (Alt+Shift+P with records marked) show what they do the first time. Unmark them freely.
    for sTitle in lsStarterMarks:
        if oDb.execute("UPDATE presentations SET marked = 1 WHERE title = ?", (sTitle,)).rowcount: log("Marked as a starter: " + sTitle)
        else: log("Starter not found, so not marked: " + sTitle)
    oDb.commit()
    sIntegrity = oDb.execute("PRAGMA integrity_check").fetchone()[0]
    dCounts = {s: oDb.execute("SELECT count(*) FROM %s" % s).fetchone()[0] for s in ("presentations", "lookups")}
    dMedia = dict(oDb.execute("SELECT medium, count(*) FROM presentations GROUP BY medium").fetchall())
    oDb.execute("PRAGMA journal_mode = DELETE")
    oDb.execute("VACUUM")
    oDb.close()
    log("Integrity %s; counts %s; media %s" % (sIntegrity, dCounts, dMedia))
    if sIntegrity != "ok" or dCounts["presentations"] != len(lPresentations) or not lPresentations:
        os.remove(sTemp)
        raise RuntimeError("the new database failed its own check (integrity %s, %d of %d presentations); the old one is kept" % (sIntegrity, dCounts["presentations"], len(lPresentations)))
    os.replace(sTemp, sTarget)
    say("Wrote %s: %d presentations (%d audio, %d video, %d pages), %d pick-list values." % (sTarget, dCounts["presentations"], dMedia.get("audio", 0), dMedia.get("video", 0), dMedia.get("page", 0), dCounts["lookups"]))
    return dCounts


def main(lsArgs):
    log("makeTechTraining.py started; %s; script %s" % (sVersion, os.path.abspath(__file__)))
    log("Python %s at %s on %s; cwd %s; command line %s" % (platform.python_version(), sys.executable, platform.platform(), os.getcwd(), " ".join(sys.argv)))
    sSource = findDirectory(lsArgs)
    log("Directory: %s" % (sSource or "not found"))
    if not sSource:
        say("TechTraining.md was not found. Name it, or its folder, after makeTechTraining.")
        return 1
    say("Reading " + sSource)
    with open(sSource, encoding="utf-8-sig") as fSource: sText = fSource.read()
    build(sText, os.path.join(sProject, "templates", "TechTraining", "TechTraining.db"))
    say("Log: " + sLogPath)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except Exception as oError:
        log("CRASH " + traceback.format_exc())
        print("makeTechTraining stopped: %s. The log is %s." % (oError, sLogPath))
        sys.exit(1)
