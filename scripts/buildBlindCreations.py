"""buildBlindCreations.py -- builds BlindCreations.db from the Blind directories on GitHub Pages

WHY IT EXISTS (10 October 2026). A member of the BITS development list proposed one searchable place for books,
software and hardware made by or for blind people, filtered by type and operating system. The Blind directories
already hold that knowledge -- Blind Apps, Blind Books, Blind Authors, Blind Developers, Blind Presenters and Blind
Creators -- as web pages. This script turns them into a DbDo database, so the same knowledge can be searched,
filtered, sorted, marked, played and linked in DbDo.

WHAT IT BUILDS. Four tables, each with its own first letter, in the Trail conventions of the homer-db skill:
apps, books, creators and presentations. Who made what is recorded the Homer way, in the maps table, by prime:
a creator is author_of a book, developer_of an app or presenter_of a presentation, so DbDo's Related Records,
Say Related and Enter Child follow the links both ways. Each record also says its makers in a plain by field,
so a row and a search show them, and its category, platform, technology, format or role go into tags, one to a
line, so searching "AI" or "Windows" finds them.

    scripts\\buildBlindCreations.cmd                 from C:\\MyPages\\pages
    scripts\\buildBlindCreations.cmd D:\\pages        from another copy

The database, templates\\BlindCreations\\BlindCreations.db, is rebuilt whole; run this again whenever the directories
change, then build DbDo so its installer carries the new copy. A detailed log
goes to the project's logs folder.
"""

import datetime, html, os, platform, re, sqlite3, sys, unicodedata

# A development tool in DbDo's scripts folder, not in the template's own folder: DbDo copies everything in a template
# folder to each user, and this needs the MyPages source, which users do not have.
sHere = os.path.dirname(os.path.abspath(__file__))
sProjectRoot = os.path.dirname(sHere)
sDb = os.path.join(sProjectRoot, "templates", "BlindCreations", "BlindCreations.db")
oLog = None


def logLine(s):
    oLog.write(datetime.datetime.now().strftime("%H:%M:%S") + " " + s + "\n"); oLog.flush()


def say(s):
    print(s); logLine("CONSOLE: " + s)


def startLog():
    global oLog
    sLogs = os.path.join(sProjectRoot, "logs")
    os.makedirs(sLogs, exist_ok=True)
    sPath = os.path.join(sLogs, "DbDo-buildBlindCreations-" + datetime.datetime.now().strftime("%Y%m%d-%H%M%S") + ".log")
    oLog = open(sPath, "w", encoding="utf-8-sig", newline="\r\n")
    logLine("buildBlindCreations.py started; Python %s on %s; SQLite %s" % (platform.python_version(), platform.platform(), sqlite3.sqlite_version))
    logLine("script %s; cwd %s; command line %s" % (os.path.abspath(__file__), os.getcwd(), " ".join(sys.argv)))
    return sPath


def readPage(sPages, sName):
    sPath = os.path.join(sPages, sName, sName + ".md")
    logLine("reading " + sPath)
    return open(sPath, encoding="utf-8-sig").read().replace("\r\n", "\n")


def key(sName):
    """A name as compared: accents, punctuation, case and spacing set aside."""
    s = unicodedata.normalize("NFKD", sName or "")
    s = "".join(c for c in s if not unicodedata.combining(c)).lower()
    return re.sub(r"\s+", " ", re.sub(r"[^a-z0-9 ]", " ", s)).strip()


def plain(sMarkdown):
    """Markdown to plain text: links become their words, emphasis and anchors go."""
    s = re.sub(r"\[([^\]]+)\]\[[^\]]*\]", r"\1", sMarkdown)
    # An address may hold brackets of its own, as Wikipedia's often do: it ends at the ")" before a space,
    # punctuation or the line's end.
    s = re.sub(r"\[([^\]]+)\]\(\S+?\)(?=[\s,.;:!?]|$)", r"\1", s)
    s = re.sub(r"\s*\{#[^}]*\}", "", s)
    return html.unescape(re.sub(r"[*_`]", "", s)).strip()


def references(s):
    return {m.group(1).lower(): m.group(2) for m in re.finditer(r"(?m)^\[([^\]]+)\]:\s*(\S+)", s)}


def entries(s):
    """The ### entries under the letter headings (## A {#letter-a}), stopping at the first other ## section."""
    lOut, bIn = [], False
    for sBlock in re.split(r"(?m)^(?=## )", s):
        if re.match(r"## .*\{#letter-", sBlock): bIn = True
        elif sBlock.startswith("## "):
            if bIn: break
            continue
        if bIn: lOut += [x for x in re.split(r"(?m)^(?=### )", sBlock) if x.startswith("### ")]
    return lOut


def fields(sEntry):
    """The heading's words and link name, the description, and the "- Label: value" bullets."""
    lLines = sEntry.split("\n")
    m = re.match(r"### \[([^\]]+)\]\[([^\]]+)\]", lLines[0]) or re.match(r"### \[([^\]]+)\]\(([^)]+)\)", lLines[0])
    dBullets = {}
    lSummary = []
    for sLine in lLines[1:]:
        b = re.match(r"- ([A-Za-z ]+): (.*)$", sLine)
        if b: dBullets[b.group(1).strip().lower()] = b.group(2).strip()
        elif sLine.strip() and not sLine.startswith(("-", "#")): lSummary.append(sLine.strip())
    return (m.group(1) if m else plain(lLines[0][4:])), (m.group(2) if m else ""), " ".join(lSummary), dBullets


def names(sValue):
    """The people a Developer or Author bullet names, by their link words."""
    lFound = re.findall(r"\[([^\]]+)\]\[[^\]]+\]", sValue) or re.findall(r"\[([^\]]+)\]\([^)]*\)", sValue)
    return lFound or [x.strip() for x in re.split(r",| and ", plain(sValue)) if x.strip()]


def parseCreators(s):
    lPeople = []
    for sBlock in re.split(r"(?m)^(?=## )", s):
        m = re.match(r"## (.+?) \{#cr-([^}]+)\}", sBlock)
        if not m: continue
        sName, sAnchor = m.group(1).strip(), m.group(2)
        lWords = sName.split()
        sToken = sAnchor.split("-")[0]
        iSurname = next((i for i, w in enumerate(lWords) if key(w).replace(" ", "") == sToken), len(lWords) - 1)
        lParas = [p.strip() for p in re.split(r"\n\s*\n", sBlock) if p.strip()]
        sSummary = plain(lParas[1]) if len(lParas) > 1 and not lParas[1].startswith(("Profiles", "#", "-")) else ""
        lProfiles = re.findall(r"\[([^\]]+)\]\((https?://\S+?)\)(?=[\s,.;:!?]|$)", sBlock.split("### ")[0])
        lRoles = [r for r, h in (("author", "### Books"), ("developer", "### Apps"), ("presenter", "### Presentations")) if h in sBlock]
        lPeople.append({"name": sName, "first": " ".join(lWords[:iSurname]), "last": " ".join(lWords[iSurname:]),
                        "summary": sSummary, "profiles": lProfiles, "roles": lRoles})
    return lPeople


def parsePresentations(s):
    dKind = {}
    i = s.find("{#media-by-type}")
    if i >= 0:
        for mType in re.finditer(r"(?ms)^### (.+?)(?: \{#[^}]*\})?\n(.*?)(?=^### |\Z)", s[i:]):
            for a in re.findall(r"\(#([^)]+)\)", mType.group(2)): dKind[a] = plain(mType.group(1))
    lOut = []
    for sPerson in re.split(r"(?m)^(?=### )", s[:i if i >= 0 else len(s)]):
        m = re.match(r"### (.+?) \{#pres-", sPerson)
        if not m: continue
        for w in re.finditer(r"(?ms)^#### \[([^\]]+)\]\((\S+?)\) \{#([^}]+)\}\n(.*?)(?=^#### |\Z)", sPerson):
            lOut.append({"title": w.group(1).strip(), "url": w.group(2), "by": m.group(1).strip(),
                         "kind": dKind.get(w.group(3), ""), "summary": plain(w.group(4).strip().split("\n")[0])})
    return lOut


# ---- the schema, in the Trail conventions (homer-db skill), as JobTrail writes it ----

def lookSql(lsFields):
    return "rtrim(" + " || ".join("iif(%s IS NOT NULL AND length(CAST(%s AS TEXT))>0, CAST(%s AS TEXT) || ' | ', '')" % (f, f, f) for f in lsFields) + ", ' | ')"


def primeSql(lsFields):
    return "||'|'||".join("coalesce(CAST(%s AS TEXT),'')" % f for f in lsFields)


def createTable(c, sTable, sId, lData, lsLook, lsPrime, bUrl=True):
    lCols = ["%s INTEGER PRIMARY KEY AUTOINCREMENT" % sId,
             "added TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP", "edited TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP"]
    lCols += ["%s %s" % (n, t) for n, t in lData]
    if bUrl: lCols.append("url TEXTLINE")
    lCols += ["notes TEXTMARKDOWN", "tags TEXTMEMO",
              "look TEXT GENERATED ALWAYS AS (%s) STORED" % lookSql(lsLook),
              "prime TEXT GENERATED ALWAYS AS (%s) STORED" % primeSql(lsPrime),
              "marked INTEGER NOT NULL DEFAULT 0"]
    c.execute('CREATE TABLE "%s" (\n  %s\n)' % (sTable, ",\n  ".join(lCols)))
    lWatch = [n for n, _ in lData] + (["url"] if bUrl else []) + ["notes", "tags"]
    c.execute('CREATE TRIGGER "trg_%s_edited" AFTER UPDATE OF %s ON "%s" FOR EACH ROW WHEN %s BEGIN UPDATE "%s" SET edited = CURRENT_TIMESTAMP WHERE "%s" = NEW."%s"; END'
              % (sTable, ", ".join('"%s"' % f for f in lWatch), sTable, " OR ".join('OLD."%s" IS NOT NEW."%s"' % (f, f) for f in lWatch), sTable, sId, sId))
    c.execute('CREATE TRIGGER "trg_%s_maps" AFTER UPDATE ON "%s" FOR EACH ROW WHEN OLD.prime IS NOT NEW.prime BEGIN UPDATE maps SET prime1 = NEW.prime WHERE tbl1 = \'%s\' AND prime1 = OLD.prime; UPDATE maps SET prime2 = NEW.prime WHERE tbl2 = \'%s\' AND prime2 = OLD.prime; END' % (sTable, sTable, sTable, sTable))
    c.execute('CREATE TRIGGER "trg_%s_maps_delete" AFTER DELETE ON "%s" FOR EACH ROW BEGIN DELETE FROM maps WHERE (tbl1 = \'%s\' AND prime1 = OLD.prime) OR (tbl2 = \'%s\' AND prime2 = OLD.prime); END' % (sTable, sTable, sTable, sTable))
    c.execute('CREATE UNIQUE INDEX "idx_%s_prime" ON "%s" (prime)' % (sTable, sTable))


def tagLines(*lValues):
    lOut = []
    for v in lValues:
        for s in (v if isinstance(v, list) else re.split(r",\s*", v or "")):
            s = plain(s).strip()
            if s and s.lower() not in [x.lower() for x in lOut]: lOut.append(s)
    return "\n".join(lOut)


def main():
    sLogPath = startLog()
    sPages = sys.argv[1] if len(sys.argv) > 1 else r"C:\MyPages\pages"
    if not os.path.isdir(sPages):
        say("The directory pages were not found in " + sPages + ". Give their folder after the command.")
        return 1
    say("Building BlindCreations.db from " + sPages + ".")
    sApps, sBooks = readPage(sPages, "BlindApps"), readPage(sPages, "BlindBooks")
    sCreators, sPresenters = readPage(sPages, "BlindCreators"), readPage(sPages, "BlindPresenters")
    lPeople = parseCreators(sCreators)
    lPresentations = parsePresentations(sPresenters)
    if os.path.exists(sDb): os.remove(sDb)
    c = sqlite3.connect(sDb, isolation_level=None)
    c.execute("PRAGMA journal_mode = DELETE"); c.execute("BEGIN")
    # DbDo's own tables, as in JobTrail.
    createTable(c, "lookups", "lookup_id", [("tbl", "TEXTLINE"), ("fld", "TEXTLINE"), ("val", "TEXTLINE"), ("ordinal", "INTEGER"), ("src", "TEXTLINE"), ("descrip", "TEXTMARKDOWN")], ["tbl", "fld", "val"], ["src", "tbl", "fld", "val"])
    createTable(c, "maps", "map_id", [("kind", "TEXTLINE"), ("tbl1", "TEXTLINE"), ("prime1", "TEXTLINE"), ("tbl2", "TEXTLINE"), ("prime2", "TEXTLINE")], ["kind", "prime1", "prime2"], ["tbl1", "prime1", "kind", "tbl2", "prime2"], bUrl=False)
    c.execute("CREATE TABLE views (tbl TEXTLINE NOT NULL, fld TEXTLINE NOT NULL, val TEXTLINE, PRIMARY KEY (tbl, fld))")
    # The four tables, data fields in the order a person would fill them in.
    createTable(c, "apps", "app_id", [("name", "TEXTLINE"), ("by", "TEXTLINE"), ("category", "TEXTLINE"), ("platform", "TEXTLINE"), ("tech", "TEXTLINE"), ("available_from", "TEXTLINE"), ("summary", "TEXTMARKDOWN")], ["name", "by"], ["name", "by"])
    createTable(c, "books", "book_id", [("title", "TEXTLINE"), ("by", "TEXTLINE"), ("year", "TEXTLINE"), ("category", "TEXTLINE"), ("format", "TEXTLINE"), ("available_from", "TEXTLINE"), ("summary", "TEXTMARKDOWN")], ["title", "by"], ["title", "by"])
    createTable(c, "creators", "creator_id", [("last_name", "TEXTLINE"), ("first_name", "TEXTLINE"), ("roles", "TEXTLINE"), ("summary", "TEXTMARKDOWN")], ["last_name", "first_name"], ["last_name", "first_name"])
    createTable(c, "presentations", "presentation_id", [("title", "TEXTLINE"), ("by", "TEXTLINE"), ("kind", "TEXTLINE"), ("summary", "TEXTMARKDOWN")], ["title", "by"], ["title", "by"])

    dPrime = {}   # creator name key -> prime
    for p in lPeople:
        sUrl = next((u for t, u in p["profiles"] if t.lower() == "website"), p["profiles"][0][1] if p["profiles"] else "")
        sNotes = ("Profiles:\n\n" + "\n".join("- [%s](%s)" % (t, u) for t, u in p["profiles"])) if p["profiles"] else ""
        c.execute("INSERT INTO creators (last_name, first_name, roles, summary, url, notes, tags) VALUES (?,?,?,?,?,?,?)",
                  (p["last"], p["first"], ", ".join(p["roles"]), p["summary"], sUrl, sNotes, tagLines(p["roles"])))
        dPrime[key(p["name"])] = c.execute("SELECT prime FROM creators WHERE creator_id = last_insert_rowid()").fetchone()[0]
    iMaps, lUnmatched = [0], []

    def link(lsNames, sKind, sTable, sPrime):
        for sName in lsNames:
            sCreator = dPrime.get(key(sName))
            if not sCreator: lUnmatched.append("%s (%s %s)" % (sName, sKind, sPrime)); continue
            c.execute("INSERT OR IGNORE INTO maps (tbl1, prime1, kind, tbl2, prime2) VALUES ('creators', ?, ?, ?, ?)", (sCreator, sKind, sTable, sPrime))
            iMaps[0] += 1

    dAppRefs, iApps = references(sApps), 0
    for e in entries(sApps):
        sName, sRef, sSummary, d = fields(e)
        lBy = names(d.get("developer", d.get("developers", "")))
        sUrl = dAppRefs.get(sRef.lower(), sRef if sRef.startswith("http") else "")
        c.execute("INSERT INTO apps (name, by, category, platform, tech, available_from, summary, url, tags) VALUES (?,?,?,?,?,?,?,?,?)",
                  (sName, ", ".join(lBy), plain(d.get("category", "")), plain(d.get("platform", d.get("platforms", ""))), plain(d.get("tech", "")),
                   plain(d.get("available from", "")), sSummary and plain(sSummary), sUrl,
                   tagLines(d.get("category", ""), d.get("platform", d.get("platforms", "")), d.get("tech", ""))))
        link(lBy, "developer_of", "apps", c.execute("SELECT prime FROM apps WHERE app_id = last_insert_rowid()").fetchone()[0]); iApps += 1

    dBookRefs, iBooks = references(sBooks), 0
    for e in entries(sBooks):
        sTitle, sRef, sSummary, d = fields(e)
        lBy = names(d.get("author", d.get("authors", "")))
        sUrl = dBookRefs.get(sRef.lower(), sRef if sRef.startswith("http") else "")
        c.execute("INSERT INTO books (title, by, year, category, format, available_from, summary, url, tags) VALUES (?,?,?,?,?,?,?,?,?)",
                  (sTitle, ", ".join(lBy), plain(d.get("year", "")), plain(d.get("category", "")), plain(d.get("format", "")),
                   plain(d.get("available from", "")), sSummary and plain(sSummary), sUrl, tagLines(d.get("category", ""), d.get("format", ""))))
        link(lBy, "author_of", "books", c.execute("SELECT prime FROM books WHERE book_id = last_insert_rowid()").fetchone()[0]); iBooks += 1

    for p in lPresentations:
        c.execute("INSERT OR IGNORE INTO presentations (title, by, kind, summary, url, tags) VALUES (?,?,?,?,?,?)",
                  (p["title"], p["by"], p["kind"], p["summary"], p["url"], tagLines(p["kind"])))
        link([p["by"]], "presenter_of", "presentations", c.execute("SELECT prime FROM presentations WHERE title = ? AND by = ?", (p["title"], p["by"])).fetchone()[0])

    # Pick lists: the kinds of link, and each field whose values form a short fixed set, alphabetical.
    def lookup(sTbl, sFld, lValues, dDescrip=None):
        for i, v in enumerate(sorted(set(x for x in lValues if x), key=str.lower), 1):
            c.execute("INSERT INTO lookups (src, tbl, fld, val, ordinal, descrip) VALUES ('', ?, ?, ?, ?, ?)", (sTbl, sFld, v, i, (dDescrip or {}).get(v, "")))
    lookup("maps", "kind", ["author_of", "developer_of", "presenter_of"], {"author_of": "The creator wrote the book.", "developer_of": "The creator made or co-made the app.", "presenter_of": "The creator presents or hosts the presentation."})
    lookup("apps", "category", [r[0] for r in c.execute("SELECT category FROM apps")])
    lookup("books", "category", [r[0] for r in c.execute("SELECT category FROM books")])
    lookup("presentations", "kind", [r[0] for r in c.execute("SELECT kind FROM presentations")])

    # How each table is heard: three fields, the one a person looks a record up by first.
    for sTbl, sSelect, sOrder in (("apps", "name, category, platform", "name"), ("books", "title, by, year", "title"),
                                  ("creators", "last_name, first_name, roles", "last_name, first_name"),
                                  ("presentations", "title, by, kind", "title")):
        for sFld, sVal in (("SelectFields", sSelect), ("OrderFields", sOrder), ("WhereFilter", "")):
            c.execute("INSERT INTO views (tbl, fld, val) VALUES (?,?,?)", (sTbl, sFld, sVal))
    c.execute("COMMIT"); c.execute("VACUUM")
    dCount = {t: c.execute('SELECT count(*) FROM "%s"' % t).fetchone()[0] for t in ("apps", "books", "creators", "presentations", "maps")}
    c.close()
    for s in lUnmatched: logLine("NOT LINKED, no such creator: " + s)
    say("Built: %(apps)d apps, %(books)d books, %(creators)d creators and %(presentations)d presentations, with %(maps)d links." % dCount)
    if lUnmatched: say("%d names named no creator and were not linked; the log lists them." % len(lUnmatched))
    say("Log: " + sLogPath)
    return 0


if __name__ == "__main__":
    try: sys.exit(main())
    except Exception:
        import traceback
        if oLog: logLine("CRASH: " + traceback.format_exc())
        print("Something went wrong; the log has why."); sys.exit(1)
