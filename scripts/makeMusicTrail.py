"""makeMusicTrail.py -- builds DbDo's MusicTrail template from Jamal Mazrui's two music books.

    scripts\\makeMusicTrail                     finds both EPUB files under any drive's MyBooks folder
    scripts\\makeMusicTrail <folder or files>   reads them from where you say

THE BOOKS. Mastery in Music Genre: Among the Greatest Albums of All Time gives 100 albums, ten in each of ten genres in
ranked order, each with its artist, year, summary and awards; its conclusion links every album and every artist to a
Wikipedia page. Mastery in Popular Song: Among the Greatest Musicians of All Time gives 200 musicians, a woman and a man
for each of ten roles in each of the same ten genres, each with nationality, group, active years and a summary; its
conclusion links each one too. Both are the author's own, so the template ships their text.

WHAT IT MAKES. templates\\MusicTrail\\MusicTrail.db, the Homer way (the homer-db skill): albums and performers, each with
the standard fields, look, prime, the edited trigger and the two triggers that keep links whole; lookups holding the
genres and roles with the books' own descriptions; maps linking each performer to the albums it recorded and each
musician to a band in the table; views matching MusicTrail.inix. The old file is replaced only when the new one is
complete and passes its own check; no backup is left beside it, since everything in templates ships with DbDo and
Git keeps the version before.

THE LOG. logs\\DbDo-makeMusicTrail-<date>-<time>.log holds every decision: each record, each link and why, each name
the books spell two ways.
"""

import datetime, glob, html, os, platform, re, sqlite3, string, subprocess, sys, traceback, zipfile

sVersion = "2026-10-12 version 2: reads the manuscripts in MyBooks\\books; every performer keeps the book's address"
sGenreBook = "Mastery in Music Genre"
sSongBook = "Mastery in Popular Song"
dCountries = {"UK": "British", "US": "American", "USA": "American"}
setNations = {"American", "Australian", "Brazilian", "British", "Canadian", "Dutch", "English", "French", "German", "Irish", "Jamaican", "Northern Irish", "Scottish", "Swedish", "Welsh"}
lsAlbumFields = ["name", "artist", "year", "genre", "rank", "summary", "awards", "format", "rating", "source"]
lsPerformerFields = ["name", "sex", "nationality", "affiliation", "active", "role", "genre", "summary", "source"]
dColumnTypes = {"active": "TEXTLINE", "affiliation": "TEXTLINE", "artist": "TEXTLINE", "awards": "TEXTMARKDOWN", "format": "TEXTLINE", "genre": "TEXTLINE",
            "name": "TEXTLINE", "nationality": "TEXTLINE", "rank": "INTEGER", "rating": "INTEGER", "role": "TEXTLINE", "sex": "TEXTLINE",
            "source": "TEXTLINE", "summary": "TEXTMARKDOWN", "year": "INTEGER"}

sProject = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sLogPath = os.path.join(sProject, "logs", "DbDo-makeMusicTrail-%s.log" % datetime.datetime.now().strftime("%Y%m%d-%H%M%S"))



def viewFromInix(sTemplate, sTable, sField, sDefault):
    """THE TEMPLATE'S .inix DECIDES HOW A TABLE IS HEARD (13 October 2026). A builder wrote its own fields and order into
    the views table, so every rebuild undid a choice made in the template's .inix -- the copy DbDo actually reads, and
    the one a person changes. Now a builder asks the .inix first, and keeps its own value only for a table the .inix
    does not mention, so the choice lives in one place."""
    sInix = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "templates", sTemplate, sTemplate + ".inix")
    try:
        sSection = ""
        for sLine in open(sInix, encoding="utf-8-sig"):
            sLine = sLine.strip()
            if sLine.startswith("["): sSection = sLine
            elif sSection.lower() == "[table:" + sTable.lower() + "]" and not sLine.startswith(";") and "=" in sLine:
                sKey, sValue = sLine.split("=", 1)
                if sKey.strip().lower() == sField.lower() and sValue.strip(): return sValue.strip()
    except OSError:
        pass
    return sDefault

def log(sText):
    os.makedirs(os.path.dirname(sLogPath), exist_ok=True)
    with open(sLogPath, "a", encoding="utf-8-sig") as fLog: fLog.write(datetime.datetime.now().strftime("%H:%M:%S ") + sText + "\n")
    return True


def say(sText):
    print(sText)
    log("SAY " + sText)
    return True


# ---------------------------------------------------------------- the books

def findBooks(lsArgs):
    """The two books: named on the command line, in a folder named there, or under any drive's MyBooks folder. The
    Markdown manuscript in MyBooks\\books\\<book> is preferred to an EPUB file, since the EPUB is built from it."""
    lsPlaces = lsArgs or [sDrive + ":\\MyBooks" for sDrive in string.ascii_uppercase if os.path.isdir(sDrive + ":\\MyBooks")]
    dFound = {}
    for sPlace in lsPlaces:
        lsFiles = [sPlace] if os.path.isfile(sPlace) else glob.glob(os.path.join(sPlace, "**", "*.md"), recursive=True) + glob.glob(os.path.join(sPlace, "**", "*.epub"), recursive=True)
        for sFile in lsFiles:
            sName = os.path.basename(sFile).lower()
            if not re.match(r"mastery[_ ]in[_ ](music[_ ]genre|popular[_ ]song)\.(md|epub)$", sName): continue
            sKind = "genre" if "genre" in sName else "song"
            # The manuscript wins over the EPUB built from it.
            if sKind not in dFound or (sName.endswith(".md") and dFound[sKind].lower().endswith(".epub")): dFound[sKind] = sFile
    log("Looked in: " + "; ".join(lsPlaces))
    return dFound


def epubMarkdown(sEpub):
    """The book's chapters as one Markdown text, by Pandoc, in reading order. A manuscript's chapters are level two,
    as buildBooks writes them, and are raised one level so both kinds of source read alike."""
    if sEpub.lower().endswith(".md"):
        lsCommand = ["pandoc", "-f", "markdown", "-t", "markdown-raw_html-native_divs-native_spans", "--wrap=none", "--shift-heading-level-by=-1", sEpub]
        oRun = subprocess.run(lsCommand, capture_output=True, text=True, encoding="utf-8")
        log("RUN %s: exit %d" % (" ".join(lsCommand), oRun.returncode))
        if oRun.returncode != 0: raise RuntimeError("Pandoc could not read %s: %s" % (sEpub, oRun.stderr[-400:]))
        return oRun.stdout
    oZip = zipfile.ZipFile(sEpub)
    lsChapters = sorted(s for s in oZip.namelist() if re.search(r"text/ch\d+\.xhtml$", s))
    sHtml = "".join(oZip.read(s).decode("utf-8") for s in lsChapters)
    lsCommand = ["pandoc", "-f", "html", "-t", "markdown-raw_html-native_divs-native_spans", "--wrap=none"]
    oRun = subprocess.run(lsCommand, input=sHtml, capture_output=True, text=True, encoding="utf-8")
    log("RUN %s on %s (%d chapters): exit %d" % (" ".join(lsCommand), sEpub, len(lsChapters), oRun.returncode))
    if oRun.returncode != 0: raise RuntimeError("Pandoc could not read %s: %s" % (sEpub, oRun.stderr[-400:]))
    return oRun.stdout


def bodyOf(sText):
    return sText[sText.index("\n# Introduction"):sText.index("\n# Conclusion")]


def linksIn(sText):
    """{label: address} for every link in the book's conclusion, where it lists and links what it covered."""
    sTail = sText[sText.index("\n# Conclusion"):sText.index("\n# About the Author")]
    # An address may hold its own parentheses -- Wikipedia's Humans_(album) -- so one level of them is part of it.
    # The first version stopped at the first ")" and filed every such address cut short (12 October 2026).
    return {html.unescape(m.group(1)).strip(): m.group(2).strip() for m in re.finditer(r"\[([^\]]+)\]\((https?://(?:[^()\s]|\([^()\s]*\))+)\)", sTail)}


def readAlbums(sText):
    lAlbums, sGenre = [], ""
    for sBlock in re.split(r"\n(?=#{1,2} )", bodyOf(sText)):
        oGenre, oAlbum = re.match(r"# (.+)", sBlock), re.match(r"## (\d+)\. (.+) \((\d{4})\)", sBlock)
        if oGenre: sGenre = oGenre.group(1).strip()
        if not oAlbum: continue
        dAlbum = {"genre": sGenre, "rank": int(oAlbum.group(1)), "name": oAlbum.group(2).strip(), "year": int(oAlbum.group(3))}
        for sLabel in ("Artist", "Summary", "Awards"):
            oField = re.search(r"\*\*%s\*\*: (.+)" % sLabel, sBlock)
            dAlbum[sLabel.lower()] = oField.group(1).strip() if oField else ""
        lAlbums.append(dAlbum)
    return lAlbums


def splitDetails(sDetails):
    """(person, nationality, affiliation, active) from the parenthesis after a name. Three entries name a band in bold
    and the person first inside: "**Tuba Skinny** (Erika Lewis, American, Tuba Skinny, 2000s-present)"."""
    lsParts = [s.strip() for s in sDetails.split(", ")]
    sActive = lsParts.pop() if lsParts and re.search(r"\d{4}", lsParts[-1]) else ""
    bNation = lambda s: s in setNations or s in dCountries
    sPerson = lsParts.pop(0) if len(lsParts) >= 2 and not bNation(lsParts[0]) and bNation(lsParts[1]) else ""
    sNation = dCountries.get(lsParts[0], lsParts[0]) if lsParts else ""
    return sPerson, sNation, ", ".join(lsParts[1:]), sActive


def readPerformers(sText):
    """The 200 musicians, and the opening paragraph of each role's chapter."""
    lPeople, dRoles, sRole, sGenre, sSex = [], {}, "", "", ""
    for sBlock in re.split(r"\n(?=#{1,3} )", bodyOf(sText)):
        oHeading = re.match(r"(#{1,3}) (.+)", sBlock)
        if not oHeading: continue
        iLevel, sTitle = len(oHeading.group(1)), oHeading.group(2).strip()
        if iLevel == 1:
            sRole = sTitle
            lsParas = [s.strip() for s in sBlock.split("\n")[1:] if s.strip() and not s.startswith(":::")]
            if sRole != "Introduction" and lsParas: dRoles[sRole] = lsParas[0]
            continue
        if iLevel == 2:
            sGenre = sTitle
            continue
        sSex = sTitle
        for sLine in sBlock.split("\n")[1:]:
            # Three shapes: **Name** (details): summary; **Name** (details) summary; **Name (details)**: summary.
            oEntry = re.match(r"\*\*(.+?)\*\* \((.+?)\):? (.+)", sLine) or re.match(r"\*\*(.+?) \((.+?)\)\*\*:? (.+)", sLine)
            if not oEntry: continue
            sName, sDetails, sSummary = oEntry.group(1).strip(), oEntry.group(2), oEntry.group(3).strip()
            sPerson, sNation, sAffiliation, sActive = splitDetails(sDetails)
            if sPerson:
                log("Entry under the band %s is the musician %s" % (sName, sPerson))
                sAffiliation, sName = (sAffiliation or sName), sPerson
            # One summary begins with its verb ("has made significant waves"), its subject being the name before it.
            if sSummary[:1].islower(): sSummary = sName + " " + sSummary
            lPeople.append({"name": sName, "sex": sSex, "nationality": sNation, "affiliation": sAffiliation, "active": sActive,
                            "role": sRole, "genre": sGenre, "summary": sSummary})
    return lPeople, dRoles


def genreSentences(sText):
    """{genre: sentence} from the song book's introduction, which describes each genre in a sentence that names it in
    bold italics. A genre it does not describe gets no description."""
    sIntro = sText[sText.index("\n# Introduction"):]
    sIntro = sIntro[:sIntro.index("Ten song roles")]
    sIntro = re.sub(r"\*\*_(.+?)_\*\*", r"***\1***", sIntro)  # a manuscript writes bold italics as **_x_**
    dSentences = {}
    # Two sentences each describe two genres ("Folk music serves ..., while Hip hop showcases ..."), so a sentence is
    # split at ", while" and ", and" into clauses, and each genre gets the clause that names it, as a sentence.
    for sSentence in re.split(r"(?<=[.!?])\s+", re.sub(r"\s+", " ", sIntro)):
        for sClause in re.split(r",\s+(?:while|and)\s+(?=\*\*\*)", sSentence):
            for oMatch in re.finditer(r"\*\*\*(.+?)\*\*\*", sClause):
                sText = sClause.replace("***", "").strip().rstrip(".,;") + "."
                dSentences.setdefault(oMatch.group(1).strip().lower(), sText[0].upper() + sText[1:])
    return dSentences


def nameKey(sName):
    """A name as compared: case, a leading "The", "&" against "and", and punctuation set aside."""
    sKey = html.unescape(sName).lower().replace("&", " and ")
    sKey = re.sub(r"^the\s+", "", sKey)
    return re.sub(r"[^a-z0-9]+", " ", sKey).strip()


# ---------------------------------------------------------------- the database

def lookExpr(lsFields):
    lsParts = ["iif(\"%s\" IS NOT NULL AND length(CAST(\"%s\" AS TEXT))>0, CAST(\"%s\" AS TEXT) || ' | ', '')" % (s, s, s) for s in lsFields]
    return "rtrim(" + " || ".join(lsParts) + ", ' | ')"


def primeExpr(lsFields):
    return " || '|' || ".join("coalesce(CAST(\"%s\" AS TEXT),'')" % s for s in lsFields)


def tableDdl(sTable, sId, lsFields, lsLook, lsPrime, dTypes=None):
    """A user table as the homer-db skill gives it, with its triggers and its unique prime index. dTypes gives the
    column types; another Trail's builder (makeTechTraining) passes its own, and MusicTrail's are the default."""
    dTypes = dTypes or dColumnTypes
    lsColumns = ["\"%s\" INTEGER PRIMARY KEY AUTOINCREMENT" % sId, "added TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP", "edited TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP"]
    lsColumns += ["\"%s\" %s" % (s, dTypes[s]) for s in lsFields]
    lsColumns += ["url TEXTLINE", "notes TEXTMARKDOWN", "tags TEXTMEMO", "look TEXT GENERATED ALWAYS AS (%s) STORED" % lookExpr(lsLook),
                  "prime TEXT GENERATED ALWAYS AS (%s) STORED" % primeExpr(lsPrime), "marked INTEGER NOT NULL DEFAULT 0"]
    lsWatched = lsFields + ["url", "notes", "tags"]
    lsDdl = ["CREATE TABLE \"%s\" (\n  %s\n)" % (sTable, ",\n  ".join(lsColumns)),
             "CREATE TRIGGER \"trg_%s_edited\" AFTER UPDATE OF %s ON \"%s\" FOR EACH ROW WHEN %s BEGIN UPDATE \"%s\" SET edited = CURRENT_TIMESTAMP WHERE \"%s\" = NEW.\"%s\"; END" % (
                 sTable, ", ".join("\"%s\"" % s for s in lsWatched), sTable, " OR ".join("OLD.\"%s\" IS NOT NEW.\"%s\"" % (s, s) for s in lsWatched), sTable, sId, sId),
             "CREATE UNIQUE INDEX \"idx_%s_prime\" ON \"%s\" (prime)" % (sTable, sTable)]
    lsDdl += mapsTriggers(sTable)
    return lsDdl


def mapsTriggers(sTable):
    return ["CREATE TRIGGER \"trg_%s_maps\" AFTER UPDATE ON \"%s\" FOR EACH ROW WHEN OLD.prime IS NOT NEW.prime BEGIN UPDATE maps SET prime1 = NEW.prime WHERE tbl1 = '%s' AND prime1 = OLD.prime; UPDATE maps SET prime2 = NEW.prime WHERE tbl2 = '%s' AND prime2 = OLD.prime; END" % (sTable, sTable, sTable, sTable),
            "CREATE TRIGGER \"trg_%s_maps_delete\" AFTER DELETE ON \"%s\" FOR EACH ROW BEGIN DELETE FROM maps WHERE (tbl1 = '%s' AND prime1 = OLD.prime) OR (tbl2 = '%s' AND prime2 = OLD.prime); END" % (sTable, sTable, sTable, sTable)]


def systemDdl():
    """lookups, maps and views, as every Trail template has them."""
    lsLookups = ["src", "tbl", "fld", "val"]
    lsMaps = ["tbl1", "prime1", "kind", "tbl2", "prime2"]
    lsDdl = []
    for sTable, sId, lsData, lsPrime in (("lookups", "lookup_id", ["src TEXTLINE", "tbl TEXTLINE", "fld TEXTLINE", "val TEXTLINE", "ordinal INTEGER", "descrip TEXTMARKDOWN", "url TEXTLINE"], lsLookups),
                                         ("maps", "map_id", ["tbl1 TEXTLINE", "prime1 TEXTLINE", "kind TEXTLINE", "tbl2 TEXTLINE", "prime2 TEXTLINE"], lsMaps)):
        lsColumns = ["%s INTEGER PRIMARY KEY AUTOINCREMENT" % sId, "added TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP", "edited TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP"] + lsData
        lsColumns += ["notes TEXTMARKDOWN", "tags TEXTMEMO", "look TEXT GENERATED ALWAYS AS (%s) STORED" % lookExpr(lsPrime),
                      "prime TEXT GENERATED ALWAYS AS (%s) STORED" % primeExpr(lsPrime), "marked INTEGER NOT NULL DEFAULT 0"]
        lsWatched = [s.split()[0] for s in lsData] + ["notes", "tags"]
        lsDdl += ["CREATE TABLE \"%s\" (\n  %s\n)" % (sTable, ",\n  ".join(lsColumns)),
                  "CREATE TRIGGER \"trg_%s_edited\" AFTER UPDATE OF %s ON \"%s\" FOR EACH ROW WHEN %s BEGIN UPDATE \"%s\" SET edited = CURRENT_TIMESTAMP WHERE \"%s\" = NEW.\"%s\"; END" % (
                      sTable, ", ".join("\"%s\"" % s for s in lsWatched), sTable, " OR ".join("OLD.\"%s\" IS NOT NEW.\"%s\"" % (s, s) for s in lsWatched), sTable, sId, sId),
                  "CREATE UNIQUE INDEX \"idx_%s_prime\" ON \"%s\" (prime)" % (sTable, sTable)]
        lsDdl += mapsTriggers(sTable)
    lsDdl += ["CREATE INDEX \"idx_maps_record1\" ON \"maps\" (tbl1, prime1)", "CREATE INDEX \"idx_maps_record2\" ON \"maps\" (tbl2, prime2)",
              "CREATE TABLE views (tbl TEXTLINE NOT NULL, fld TEXTLINE NOT NULL, val TEXTLINE, PRIMARY KEY (tbl, fld))"]
    return lsDdl


def addLookups(oDb, sTable, sField, dValues, sSource):
    """A pick list, alphabetical so a first letter lands on its value, with ordinal following."""
    for iOrdinal, sValue in enumerate(sorted(dValues, key=lambda s: s.lower()), 1):
        oDb.execute("INSERT INTO lookups (src, tbl, fld, val, ordinal, descrip) VALUES (?, ?, ?, ?, ?, ?)", (sSource, sTable, sField, sValue, iOrdinal, dValues[sValue] or None))
    log("Pick list %s.%s: %d values" % (sTable, sField, len(dValues)))
    return len(dValues)


def build(sGenreText, sSongText, sTarget):
    lAlbums = readAlbums(sGenreText)
    lPeople, dRoles = readPerformers(sSongText)
    dAlbumLinks, dPeopleLinks = linksIn(sGenreText), linksIn(sSongText)
    dGenreSentences = genreSentences(sSongText)
    say("Read %d albums and %d musicians, in %d genres and %d roles." % (len(lAlbums), len(lPeople), len({a["genre"] for a in lAlbums}), len(dRoles)))
    if len(lAlbums) != 100 or len(lPeople) != 200: say("WARNING: the books describe 100 albums and 200 musicians; the counts above differ, so the log is worth reading.")

    # The genre book's own list of its artists: canonical names and their links, keyed by comparable name.
    dActs = {}
    for sLabel, sUrl in dAlbumLinks.items():
        if not re.search(r"\(\d{4}\)$", sLabel): dActs[nameKey(sLabel)] = (sLabel, sUrl)
    dAlbumUrls = {nameKey(re.sub(r"\s*\(\d{4}\)$", "", s)) + "|" + re.search(r"\((\d{4})\)$", s).group(1): u for s, u in dAlbumLinks.items() if re.search(r"\(\d{4}\)$", s)}

    # Performers: the 200 musicians first, then each album's act, one record per name.
    dPerformers = {}
    for dPerson in lPeople:
        sUrl = dPeopleLinks.get(dPerson["name"]) or dPeopleLinks.get(dPerson["affiliation"], "")
        dPerformers[nameKey(dPerson["name"])] = dict(dPerson, source=sSongBook, url=sUrl)
    lMaps = []
    for dAlbum in lAlbums:
        sCredit = dAlbum["artist"]
        sListed, sActUrl = dActs.get(nameKey(sCredit), (sCredit, ""))
        if nameKey(sCredit) not in dActs: log("Album %s: its artist %s is not in the book's list of artists; kept as credited" % (dAlbum["name"], sCredit))
        sAlbumPrime = dAlbum["name"] + "|" + sCredit
        # A duo whose partners are both performers here is two performers, not a third.
        lsParts = [s.strip() for s in re.split(r"\s+(?:and|&)\s+", sListed)]
        if len(lsParts) == 2 and all(nameKey(s) in dPerformers or nameKey(s) in dActs for s in lsParts):
            for sPart in lsParts:
                sKey = nameKey(sPart)
                if sKey not in dPerformers:
                    sName, sUrl = dActs[sKey]
                    dPerformers[sKey] = {"name": sName, "genre": dAlbum["genre"], "source": sGenreBook, "url": sUrl}
                lMaps.append(("performers", dPerformers[sKey]["name"], "recorded", "albums", sAlbumPrime))
            log("Album %s: the duo %s is linked to each partner" % (dAlbum["name"], sListed))
            continue
        sKey = nameKey(sListed)
        if sKey in dPerformers:
            if not dPerformers[sKey].get("url"): dPerformers[sKey]["url"] = sActUrl
            log("Album %s: its artist %s is also one of the 200 musicians, so one record" % (dAlbum["name"], sListed))
        else:
            # A duo with no page of its own keeps the address the book gives it: the page of the album they made together.
            if sActUrl and sActUrl == dAlbumUrls.get(nameKey(dAlbum["name"]) + "|" + str(dAlbum["year"])): log("Act %s: the book links it to its album's page, which is kept as its address" % sListed)
            dPerformers[sKey] = {"name": sListed, "genre": dAlbum["genre"], "source": sGenreBook, "url": sActUrl}
        lMaps.append(("performers", dPerformers[sKey]["name"], "recorded", "albums", sAlbumPrime))

    # A musician's group, when the group is a performer here, is a link as well as a word.
    dByKey = {k: d for k, d in dPerformers.items()}
    for dPerson in lPeople:
        sGroup = dPerson["affiliation"]
        oGroup = dByKey.get(nameKey(sGroup)) if sGroup else None
        if oGroup and oGroup["name"] != dPerson["name"]:
            lMaps.append(("performers", dPerson["name"], "member_of", "performers", oGroup["name"]))
            log("Link: %s member_of %s" % (dPerson["name"], oGroup["name"]))

    sTemp = sTarget + ".new"
    if os.path.exists(sTemp): os.remove(sTemp)
    oDb = sqlite3.connect(sTemp)
    for sDdl in tableDdl("albums", "album_id", lsAlbumFields, ["name", "artist"], ["name", "artist"]) + tableDdl("performers", "performer_id", lsPerformerFields, ["name"], ["name"]) + systemDdl():
        oDb.execute(sDdl)
    for dAlbum in sorted(lAlbums, key=lambda d: (d["genre"], d["rank"])):
        sUrl = dAlbumUrls.get(nameKey(dAlbum["name"]) + "|" + str(dAlbum["year"]), "")
        if not sUrl: log("Album %s (%d): no link in the book's list" % (dAlbum["name"], dAlbum["year"]))
        oDb.execute("INSERT INTO albums (name, artist, year, genre, rank, summary, awards, source, url) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    (dAlbum["name"], dAlbum["artist"], dAlbum["year"], dAlbum["genre"], dAlbum["rank"], dAlbum["summary"] or None, dAlbum["awards"] or None, sGenreBook, sUrl or None))
    for dPerson in sorted(dPerformers.values(), key=lambda d: d["name"].lower()):
        oDb.execute("INSERT INTO performers (%s, url) VALUES (%s)" % (", ".join(lsPerformerFields), ", ".join("?" * (len(lsPerformerFields) + 1))),
                    [dPerson.get(s) or None for s in lsPerformerFields] + [dPerson.get("url") or None])
    for tMap in lMaps: oDb.execute("INSERT INTO maps (tbl1, prime1, kind, tbl2, prime2) VALUES (?, ?, ?, ?, ?)", tMap)

    dGenres = {s: next((v for k, v in dGenreSentences.items() if k == s.lower()), "") for s in sorted({a["genre"] for a in lAlbums})}
    for sGenre, sSentence in dGenres.items():
        if not sSentence: log("Genre %s: the books describe it in no sentence of their own, so it has no description" % sGenre)
    addLookups(oDb, "albums", "genre", dGenres, "MusicTrail")
    addLookups(oDb, "performers", "genre", dGenres, "MusicTrail")
    addLookups(oDb, "performers", "role", dRoles, "MusicTrail")
    addLookups(oDb, "performers", "sex", {"Female": "", "Male": ""}, "MusicTrail")
    dSources = {sGenreBook: "Mastery in Music Genre: Among the Greatest Albums of All Time, by Jamal Mazrui.",
                sSongBook: "Mastery in Popular Song: Among the Greatest Musicians of All Time, by Jamal Mazrui."}
    addLookups(oDb, "albums", "source", dSources, "MusicTrail")
    addLookups(oDb, "performers", "source", dSources, "MusicTrail")
    addLookups(oDb, "albums", "format", {"CD": "", "Cassette": "", "Digital": "", "SACD": "", "Vinyl": ""}, "DbDo")
    addLookups(oDb, "albums", "rating", {"1": "Poor", "2": "Fair", "3": "Good", "4": "Very good", "5": "Excellent"}, "DbDo")
    addLookups(oDb, "maps", "kind", {"features": "Subject album features the object performer as a guest.",
                                     "member_of": "Subject performer is or was a member of the object performer, a band or group.",
                                     "recorded": "Subject performer recorded the object album."}, "MusicTrail")
    for sTable, sSelect in (("albums", "name, artist, genre"), ("lookups", "fld, val"), ("maps", "kind, prime1, prime2"), ("performers", "name, role, genre")):
        sOrder = {"lookups": "fld, ordinal", "maps": "kind, prime1"}.get(sTable, "name")
        for sField, sValue in (("SelectFields", sSelect), ("OrderFields", sOrder), ("WhereFilter", "")):
            oDb.execute("INSERT INTO views (tbl, fld, val) VALUES (?, ?, ?)", (sTable, sField, viewFromInix("MusicTrail", sTable, sField, sValue)))
    oDb.commit()
    sIntegrity = oDb.execute("PRAGMA integrity_check").fetchone()[0]
    iDangling = oDb.execute("SELECT count(*) FROM maps m WHERE NOT EXISTS (SELECT 1 FROM performers p WHERE m.tbl1 = 'performers' AND p.prime = m.prime1) "
                            "OR (m.tbl2 = 'albums' AND NOT EXISTS (SELECT 1 FROM albums a WHERE a.prime = m.prime2)) "
                            "OR (m.tbl2 = 'performers' AND NOT EXISTS (SELECT 1 FROM performers p WHERE p.prime = m.prime2))").fetchone()[0]
    dCounts = {s: oDb.execute("SELECT count(*) FROM %s" % s).fetchone()[0] for s in ("albums", "performers", "maps", "lookups")}
    oDb.execute("PRAGMA journal_mode = DELETE")
    oDb.execute("VACUUM")
    oDb.close()
    log("Integrity %s; links pointing at nothing %d; counts %s" % (sIntegrity, iDangling, dCounts))
    if sIntegrity != "ok" or iDangling:
        os.remove(sTemp)
        raise RuntimeError("the new database failed its own check (integrity %s, %d links pointing at nothing); the old one is kept" % (sIntegrity, iDangling))
    # No backup beside it: everything in templates ships in the installer and goes to the repository, and Git
    # already keeps the version before.
    os.replace(sTemp, sTarget)
    say("Wrote %s: %d albums, %d performers, %d links, %d pick-list values." % (sTarget, dCounts["albums"], dCounts["performers"], dCounts["maps"], dCounts["lookups"]))
    return dCounts


def main(lsArgs):
    log("makeMusicTrail.py started; %s; script %s" % (sVersion, os.path.abspath(__file__)))
    log("Python %s at %s on %s; cwd %s; command line %s" % (platform.python_version(), sys.executable, platform.platform(), os.getcwd(), " ".join(sys.argv)))
    dBooks = findBooks(lsArgs)
    log("Books found: %s" % dBooks)
    if set(dBooks) != {"genre", "song"}:
        say("Both books are needed: Mastery_in_Music_Genre.epub and Mastery_in_Popular_Song.epub. Name them, or their folder, after makeMusicTrail.")
        return 1
    sTarget = os.path.join(sProject, "templates", "MusicTrail", "MusicTrail.db")
    build(epubMarkdown(dBooks["genre"]), epubMarkdown(dBooks["song"]), sTarget)
    say("Log: " + sLogPath)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except Exception as oError:
        log("CRASH " + traceback.format_exc())
        print("makeMusicTrail stopped: %s. The log is %s." % (oError, sLogPath))
        sys.exit(1)
