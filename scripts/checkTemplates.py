"""checkTemplates.py -- checks every DbDo template the way the October 2026 audit did, and repairs what DbDo's own
conventions settle, so a template database and its companion files are proven to work together on every build.

WHY. An outside audit of the templates (12 October 2026) found the databases healthy and the workflows around them
broken: 13 of 22 SQL statements failed on the very databases they shipped with, because identity fields had been
renamed (unq and prm became prime, prime1 and prime2) and the queries had not. A build that checks only the databases
cannot see that. This script runs every statement on the database it ships beside, and the build stops when one fails.

WHAT IT CHECKS, for each templates\\<root>\\<root>.db:
  - SQLite's integrity and foreign-key checks.
  - Every map row resolves, at both ends, to exactly one record of the table it names.
  - Every relationship kind used in maps is a value in lookups (tbl = maps, fld = kind), so the edit dialog offers it.
  - Every table with a prime has a unique index on it, so a map can never point at two records.
  - Every edited trigger watches every content field: all but the id, added, edited, look, prime and marked, the same
    rule DbDo uses when it creates a table. Marking a row is deliberately not an edit.
  - Every SQL file in the folder: each statement runs, read-only, and returns at least one row.
  - <root>.inix: every SelectFields and OrderFields column exists in its table, and WhereFilter is valid SQL.
  - report.inix: every report names its @table, which exists; @filter, @sort and @group work on it; and every $field
    the bands use is a column of it.
  - The views table inside the database: each table it describes exists, its columns exist and its filter runs.
  - Every .dbdo script, in either form: its table exists, and its filter and order run together.
  - No record has an empty prime or look, so every record can be pointed at and spoken.
  - A template folder holds its database. RadioTrail's is built on this machine (it is not in git); the build makes it
    when it is missing.

WHAT --repair DOES, before checking, inside one transaction per database:
  - Merges records that share a prime: the first keeps its id, takes any field it lacks from the others, and gains
    their notes and tags lines; the others go. Their links were already the first's, since a map names a prime.
  - Adds a missing unique prime index once no duplicates remain.
  - Rewrites an edited trigger that misses a content field, in DbDo's own form.
  - Adds each relationship kind used in maps but missing from lookups, with a sentence saying which way it reads.
Repairs change only the templates in this development folder. A user's own copy is never touched.

WHAT --schema <root> DOES. RadioTrail's database is built on this machine and kept out of git, so a clean checkout
had no way to make one: fetchStations could only copy it. --schema writes templates\\<root>\\<root>.schema from the
database -- every table, index and trigger, and the rows of lookups and views, but no records -- and fetchStations
builds a new RadioTrail.db from it when there is none to copy. The file is rewritten only when it would change.

USAGE
  scripts\\checkTemplates                 check every template; exit 1 when anything fails
  scripts\\checkTemplates --repair        repair, then check
  scripts\\checkTemplates --repair JobTrail RadioTrail    only these templates
  scripts\\checkTemplates --repair --schema RadioTrail     also write RadioTrail.schema, as the build does

Every run writes logs\\DbDo-checkTemplates-<date>-<time>.log with every check and its result.
"""

import datetime, glob, os, platform, re, sqlite3, sys, traceback

dKindNotes = {
    "ContactTrail|in_group": "Subject contact belongs to the object group.",
    "MusicTrail|features": "Subject album features the object performer as a guest.",
    "MusicTrail|member_of": "Subject performer is or was a member of the object performer, a band or group.",
    "MusicTrail|recorded": "Subject performer recorded the object album.",
    "RecipeTrail|uses": "Subject recipe uses the object ingredient; the amount is in the map row's notes.",
}
lBandKeys = ["detail", "footer", "group_footer", "group_header", "header"]
# FIELDS A TEMPLATE LEAVES UNWATCHED ON PURPOSE (12 October 2026). "edited" means a person or a deliberate repair changed
# the record. RadioTrail's catalog refresh rewrites votes, clicks, trend and the check and seen dates of all 55,000
# stations, the probes stamp probed, and every play counts plays: watching those would make every station "edited"
# after each refresh or play. fetchStations keeps them out of its trigger, and this check honors that. The first run
# of this script did not, and rewrote RadioTrail's trigger to watch them; that was put right the same day.
dUnwatched = {"RadioTrail|stations": ["clicks", "last_check", "last_played", "last_seen", "plays", "probed", "trend", "votes"]}
lNotWatched = ["added", "edited", "look", "marked", "prime"]
lSchemaRows = ["lookups", "views"]
lSkipTables = ["sqlean_define", "views"]
sVersion = "2026-10-12 version 2: leaves sqlean_define alone; undoes a trigger that watches deliberately unwatched fields"

lErrors = []
lRepairs = []
sLogPath = ""
sProject = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def log(sText):
    """Appends a timestamped line to the log."""
    if not sLogPath: return False
    with open(sLogPath, "a", encoding="utf-8-sig") as f: f.write(datetime.datetime.now().strftime("%H:%M:%S") + " " + sText + "\n")
    return True


def say(sText):
    """Prints a line for the listener and logs it."""
    print(sText, flush=True)
    log("SAY " + sText)
    return True


def fail(sRoot, sText):
    """Records a failed check."""
    lErrors.append(sRoot + ": " + sText)
    log("FAIL " + sRoot + ": " + sText)
    return False


def repaired(sRoot, sText):
    """Records a repair."""
    lRepairs.append(sRoot + ": " + sText)
    log("REPAIR " + sRoot + ": " + sText)
    return True


def plural(iCount, sOne, sMany=""):
    """1 table, 2 tables."""
    return "{:,}".format(iCount) + " " + (sOne if iCount == 1 else (sMany or sOne + "s"))


def quoted(sName):
    """A name quoted for SQL."""
    return '"' + sName.replace('"', '""') + '"'


# ---------------------------------------------------------------- reading .inix

def readInix(sPath):
    """[(section, {key: value})] from an .inix file: [section] lines, key = value, and values fenced with three double
    quotes or a backtick across several lines. Enough for the keys this script checks."""
    lSections, sSection, dKeys, sKey, sFence, lValue = [], "", {}, "", "", []
    with open(sPath, encoding="utf-8-sig") as f: lLines = f.read().splitlines()
    for sLine in lLines:
        if sFence:
            if sLine.strip() == sFence:
                dKeys[sKey] = "\n".join(lValue)
                sFence, lValue = "", []
            else:
                lValue.append(sLine)
            continue
        if sLine.lstrip().startswith(";") or sLine.lstrip().startswith("#") or not sLine.strip(): continue
        oMatch = re.match(r"^\s*\[(.+)\]\s*$", sLine)
        if oMatch:
            lSections.append((sSection, dKeys))
            sSection, dKeys = oMatch.group(1).strip(), {}
            continue
        if "=" not in sLine: continue
        sKey, sValue = [s.strip() for s in sLine.split("=", 1)]
        if sValue in ('"""', "`"):
            sFence = sValue
            continue
        dKeys[sKey] = sValue.strip('"') if sValue.startswith('"') and sValue.endswith('"') and len(sValue) > 1 else sValue
    lSections.append((sSection, dKeys))
    return lSections


# ---------------------------------------------------------------- schema helpers

def tables(oDb):
    """The ordinary tables of a database."""
    return [r[0] for r in oDb.execute("SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name")]


def columns(oDb, sTable):
    """[(name, pk, hidden)] for every column, generated ones included (hidden 2 or 3)."""
    return [(r[1], r[5], r[6]) for r in oDb.execute("PRAGMA table_xinfo(" + quoted(sTable) + ")")]


def columnNames(oDb, sTable):
    """Every column name of a table, lower-cased."""
    return [s.lower() for s, iPk, iHidden in columns(oDb, sTable)]


def primaryKey(oDb, sTable):
    """The table's integer primary key column, or ''."""
    return next((s for s, iPk, iHidden in columns(oDb, sTable) if iPk), "")


def contentColumns(oDb, sTable, sRoot=""):
    """The columns an edited trigger must watch: every stored column but the key, DbDo's bookkeeping ones, and any the
    template leaves unwatched on purpose."""
    lSkip = lNotWatched + dUnwatched.get(sRoot + "|" + sTable, [])
    return [s for s, iPk, iHidden in columns(oDb, sTable) if not iPk and iHidden == 0 and s.lower() not in lSkip]


def watchedColumns(oDb, sTable):
    """The columns the table's edited trigger watches, or None when it has none."""
    oRow = oDb.execute("SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = ?", ("trg_" + sTable + "_edited",)).fetchone()
    if not oRow: return None
    oMatch = re.search(r"AFTER UPDATE OF (.*?) ON ", oRow[0], re.S)
    return [s.strip().strip('"') for s in oMatch.group(1).split(",")] if oMatch else []


def editedTrigger(sTable, sPk, lCols):
    """The edited trigger in exactly the form DbDo writes when it creates a table."""
    return ("CREATE TRIGGER " + quoted("trg_" + sTable + "_edited") + " AFTER UPDATE OF " + ", ".join(quoted(s) for s in lCols)
            + " ON " + quoted(sTable) + " FOR EACH ROW WHEN " + " OR ".join("OLD." + quoted(s) + " IS NOT NEW." + quoted(s) for s in lCols)
            + " BEGIN UPDATE " + quoted(sTable) + " SET edited = CURRENT_TIMESTAMP WHERE " + quoted(sPk) + " = NEW." + quoted(sPk) + "; END")


def hasUniquePrime(oDb, sTable):
    """Whether a unique index covers exactly the prime column."""
    for oIndex in oDb.execute("PRAGMA index_list(" + quoted(sTable) + ")").fetchall():
        if oIndex[2] and [r[2] for r in oDb.execute("PRAGMA index_info(" + quoted(oIndex[1]) + ")")] == ["prime"]: return True
    return False


def primeTables(oDb):
    """Tables carrying a prime, other than DbDo's own metadata."""
    return [s for s in tables(oDb) if s not in lSkipTables and "prime" in columnNames(oDb, s)]


# ---------------------------------------------------------------- repairs

def mergeDuplicates(oDb, sRoot, sTable):
    """Merges records sharing a prime into the first of them (lowest id)."""
    sPk = primaryKey(oDb, sTable)
    lGroups = oDb.execute("SELECT prime, group_concat(" + quoted(sPk) + ") FROM " + quoted(sTable) + " WHERE prime IS NOT NULL GROUP BY prime HAVING count(*) > 1").fetchall()
    if not lGroups or not sPk: return 0
    lCols = contentColumns(oDb, sTable)
    oDelete = oDb.execute("SELECT name, sql FROM sqlite_master WHERE type = 'trigger' AND name = ?", ("trg_" + sTable + "_maps_delete",)).fetchone()
    if oDelete: oDb.execute("DROP TRIGGER " + quoted(oDelete[0]))   # deleting a duplicate must not delete the links the keeper shares
    for sPrime, sIds in lGroups:
        lIds = sorted(int(s) for s in sIds.split(","))
        dKeep = dict(zip(lCols, oDb.execute("SELECT " + ", ".join(quoted(s) for s in lCols) + " FROM " + quoted(sTable) + " WHERE " + quoted(sPk) + " = ?", (lIds[0],)).fetchone()))
        for iOther in lIds[1:]:
            dOther = dict(zip(lCols, oDb.execute("SELECT " + ", ".join(quoted(s) for s in lCols) + " FROM " + quoted(sTable) + " WHERE " + quoted(sPk) + " = ?", (iOther,)).fetchone()))
            for sCol in lCols:
                vKeep, vOther = dKeep[sCol], dOther[sCol]
                if vOther in (None, ""): continue
                if sCol.lower() in ("notes", "tags") and vKeep not in (None, ""):
                    lLines = str(vKeep).splitlines()
                    dKeep[sCol] = "\n".join(lLines + [s for s in str(vOther).splitlines() if s not in lLines])
                elif vKeep in (None, ""):
                    dKeep[sCol] = vOther
            oDb.execute("DELETE FROM " + quoted(sTable) + " WHERE " + quoted(sPk) + " = ?", (iOther,))
        oDb.execute("UPDATE " + quoted(sTable) + " SET " + ", ".join(quoted(s) + " = ?" for s in lCols) + " WHERE " + quoted(sPk) + " = ?", [dKeep[s] for s in lCols] + [lIds[0]])
        repaired(sRoot, sTable + ": merged " + plural(len(lIds), "record") + " sharing the prime " + repr(sPrime) + " into id " + str(lIds[0]))
    if oDelete: oDb.execute(oDelete[1])
    return len(lGroups)


def seedKinds(oDb, sRoot):
    """Adds each relationship kind used in maps but missing from lookups."""
    lHave = tables(oDb)
    if "maps" not in lHave or "lookups" not in lHave: return 0
    lLookupCols = columnNames(oDb, "lookups")
    if not all(s in lLookupCols for s in ("tbl", "fld", "val")): return 0
    lSeeded = [r[0] for r in oDb.execute("SELECT val FROM lookups WHERE tbl = 'maps' AND fld = 'kind'")]
    iOrdinal = (oDb.execute("SELECT max(ordinal) FROM lookups WHERE tbl = 'maps' AND fld = 'kind'").fetchone()[0] or 0) if "ordinal" in lLookupCols else 0
    iAdded = 0
    for sKind, sTbl1, sTbl2 in oDb.execute("SELECT kind, min(tbl1), min(tbl2) FROM maps WHERE kind IS NOT NULL AND kind <> '' GROUP BY kind ORDER BY kind").fetchall():
        if sKind in lSeeded: continue
        sNote = dKindNotes.get(sRoot + "|" + sKind, "")
        if not sNote:
            sNote = "Subject " + sTbl1.rstrip("s") + " " + sKind.replace("_", " ") + " the object " + sTbl2.rstrip("s") + "."
            log("No written description for " + sRoot + " kind " + sKind + "; using the generic one: " + sNote)
        iOrdinal += 1
        dRow = {"tbl": "maps", "fld": "kind", "val": sKind, "ordinal": iOrdinal, "src": sRoot, "descrip": sNote}
        lCols = [s for s in dRow if s in lLookupCols]
        oDb.execute("INSERT INTO lookups (" + ", ".join(lCols) + ") VALUES (" + ", ".join("?" for s in lCols) + ")", [dRow[s] for s in lCols])
        repaired(sRoot, "added the relationship kind " + sKind + " to lookups: " + sNote)
        iAdded += 1
    return iAdded


def repair(sRoot, sDb):
    """Every repair DbDo's conventions settle, in one transaction."""
    oDb = sqlite3.connect(sDb, isolation_level=None)
    iBefore = len(lRepairs)
    try:
        oDb.execute("BEGIN")
        # SQLEAN_DEFINE IS LEFT ALONE (12 October 2026). Dropping the empty table was churn, not repair: the build's
        # search self-test opens RadioTrail with the SQLean bundle loaded, SQLean makes the table again, and the next
        # check dropped it again -- three times in one release cycle, each rewriting a 67 MB file. DbDo never lists it.
        for sTable in primeTables(oDb):
            mergeDuplicates(oDb, sRoot, sTable)
            if not hasUniquePrime(oDb, sTable):
                oDb.execute("CREATE UNIQUE INDEX " + quoted("idx_" + sTable + "_prime") + " ON " + quoted(sTable) + " (prime)")
                repaired(sRoot, sTable + ": added the unique prime index")
        for sTable in tables(oDb):
            lWatched = watchedColumns(oDb, sTable)
            if lWatched is None: continue
            lWanted = contentColumns(oDb, sTable, sRoot)
            lMissing = [s for s in lWanted if s not in lWatched]
            # A FIELD WATCHED THAT SHOULD NOT BE (12 October 2026): this script's first version rewrote RadioTrail's
            # trigger to watch votes, last_seen and plays, and DbDo 1.0.259 shipped that. It is undone here.
            lExtra = [s for s in lWatched if s.lower() in dUnwatched.get(sRoot + "|" + sTable, [])]
            if lMissing or lExtra:
                oDb.execute("DROP TRIGGER " + quoted("trg_" + sTable + "_edited"))
                oDb.execute(editedTrigger(sTable, primaryKey(oDb, sTable), [s for s in lWanted] + [s for s in lWatched if s not in lWanted and s not in lExtra]))
                repaired(sRoot, sTable + ": the edited trigger" + (" now also watches " + ", ".join(lMissing) if lMissing else "") + (" no longer watches " + ", ".join(lExtra) if lExtra else ""))
        seedKinds(oDb, sRoot)
        if len(lRepairs) == iBefore:
            oDb.execute("ROLLBACK")   # nothing to repair: the file is left byte for byte as it was, so git sees no change
            return True
        oDb.execute("COMMIT")
        oDb.execute("VACUUM")
    except Exception:
        oDb.execute("ROLLBACK")
        fail(sRoot, "repair rolled back: " + traceback.format_exc().strip().splitlines()[-1])
        log(traceback.format_exc())
    finally:
        oDb.close()
    return True


# ---------------------------------------------------------------- the schema of a database kept out of git

def literal(vValue):
    """A value written as an SQL literal."""
    if vValue is None: return "NULL"
    if isinstance(vValue, (int, float)): return repr(vValue)
    if isinstance(vValue, bytes): return "X'" + vValue.hex() + "'"
    return "'" + str(vValue).replace("'", "''") + "'"


def saveSchema(sRoot, sFolder):
    """Writes <root>.schema: tables, then the rows of lookups and views, then indexes and triggers (after the rows, so
    no trigger fires while they go in). Deterministic, so it changes only when the database's design does."""
    sDb = os.path.join(sFolder, sRoot + ".db")
    if not os.path.isfile(sDb): return fail(sRoot, "--schema asked for, but there is no " + sRoot + ".db to read it from")
    oDb = sqlite3.connect("file:" + sDb.replace("\\", "/") + "?mode=ro", uri=True)
    try:
        lOut = ["-- " + sRoot + ".schema -- the design of " + sRoot + ".db, written by scripts\\checkTemplates --schema.",
                "-- The database itself is built on the development machine and kept out of git. When there is none,",
                "-- fetchStations builds a new one from this file and then fetches the stations. Do not edit by hand: the",
                "-- build rewrites it from the database whenever the design changes.", ""]
        lItems = oDb.execute("SELECT type, name, tbl_name, sql FROM sqlite_master WHERE sql IS NOT NULL AND name NOT LIKE 'sqlite_%' ORDER BY rowid").fetchall()
        lTables = [t for t in lItems if t[0] == "table"]
        for t in lTables: lOut.append(t[3].strip() + ";")
        for sTable in [t[1] for t in lTables if t[1] in lSchemaRows]:
            lCols = [s for s, iPk, iHidden in columns(oDb, sTable) if iHidden == 0]
            for oRow in oDb.execute("SELECT " + ", ".join(quoted(c) for c in lCols) + " FROM " + quoted(sTable) + " ORDER BY rowid"):
                lOut.append("INSERT INTO " + quoted(sTable) + " (" + ", ".join(quoted(c) for c in lCols) + ") VALUES (" + ", ".join(literal(v) for v in oRow) + ");")
        for t in lItems:
            if t[0] in ("index", "view", "trigger"): lOut.append(t[3].strip() + ";")
    finally:
        oDb.close()
    sText = "\n".join(lOut) + "\n"
    sPath = os.path.join(sFolder, sRoot + ".schema")
    sOld = open(sPath, encoding="utf-8-sig").read().replace("\r\n", "\n") if os.path.isfile(sPath) else ""
    if sOld == sText:
        log(sRoot + ".schema is current")
        return True
    with open(sPath, "wb") as f: f.write(b"\xef\xbb\xbf" + sText.replace("\n", "\r\n").encode("utf-8"))
    say("Wrote " + sRoot + ".schema, the design a clean checkout builds " + sRoot + ".db from.")
    return True


# ---------------------------------------------------------------- checks

def checkDatabase(sRoot, oDb):
    """Integrity, foreign keys, maps, kinds, prime indexes and edited triggers."""
    sIntegrity = oDb.execute("PRAGMA integrity_check").fetchone()[0]
    if sIntegrity != "ok": fail(sRoot, "integrity check: " + sIntegrity)
    lForeign = oDb.execute("PRAGMA foreign_key_check").fetchall()
    if lForeign: fail(sRoot, plural(len(lForeign), "foreign-key violation") + ", first " + repr(lForeign[0]))
    lTables = tables(oDb)
    for sTable in primeTables(oDb):
        if not hasUniquePrime(oDb, sTable): fail(sRoot, sTable + " has no unique index on prime, so a map could point at two records")
        lDup = oDb.execute("SELECT prime, count(*) FROM " + quoted(sTable) + " GROUP BY prime HAVING count(*) > 1 LIMIT 3").fetchall()
        if lDup: fail(sRoot, sTable + " has records sharing a prime, such as " + repr(lDup[0][0]))
    for sTable in lTables:
        lWatched = watchedColumns(oDb, sTable)
        if lWatched is None: continue
        lMissing = [s for s in contentColumns(oDb, sTable, sRoot) if s not in lWatched]
        if lMissing: fail(sRoot, sTable + ": editing " + ", ".join(lMissing) + " does not update edited")
        lExtra = [s for s in lWatched if s.lower() in dUnwatched.get(sRoot + "|" + sTable, [])]
        if lExtra: fail(sRoot, sTable + ": the edited trigger watches " + ", ".join(lExtra) + ", which a refresh or a play changes on every station")
    if "maps" in lTables:
        iMaps = oDb.execute("SELECT count(*) FROM maps").fetchone()[0]
        for sSide in ("1", "2"):
            for sTable, iRows in oDb.execute("SELECT tbl" + sSide + ", count(*) FROM maps GROUP BY tbl" + sSide).fetchall():
                if sTable not in lTables or "prime" not in columnNames(oDb, sTable):
                    fail(sRoot, plural(iRows, "map row") + " name the table " + repr(sTable) + ", which has no prime")
                    continue
                iBad = oDb.execute("SELECT count(*) FROM maps m WHERE m.tbl" + sSide + " = ? AND (SELECT count(*) FROM " + quoted(sTable) + " t WHERE t.prime = m.prime" + sSide + ") <> 1", (sTable,)).fetchone()[0]
                if iBad: fail(sRoot, plural(iBad, "map row") + " point" + ("s" if iBad == 1 else "") + " at no single " + sTable + " record on side " + sSide)
        if "lookups" in lTables:
            lMissing = [r[0] for r in oDb.execute("SELECT DISTINCT kind FROM maps WHERE kind IS NOT NULL AND kind <> '' AND kind NOT IN (SELECT val FROM lookups WHERE tbl = 'maps' AND fld = 'kind') ORDER BY kind")]
            if lMissing: fail(sRoot, "relationship kinds used but not offered in lookups: " + ", ".join(lMissing))
        log(sRoot + ": " + plural(iMaps, "map row") + " checked")
    return True


def statements(sText):
    """The complete SQL statements in a file, in order, comments kept."""
    lOut, sPending = [], ""
    for sLine in sText.splitlines(True):
        sPending += sLine
        if sqlite3.complete_statement(sPending):
            if re.sub(r"--[^\n]*", "", sPending).strip(): lOut.append(sPending.strip())
            sPending = ""
    if re.sub(r"--[^\n]*", "", sPending).strip(): lOut.append(sPending.strip())
    return lOut


def checkSqlFiles(sRoot, sFolder, oDb):
    """Runs every statement of every SQL file, read-only, and expects rows from each."""
    iRun = 0
    for sFile in sorted(glob.glob(os.path.join(sFolder, "*.sql"))):
        sName = os.path.basename(sFile)
        with open(sFile, encoding="utf-8-sig") as f: lStatements = statements(f.read())
        for iIndex, sSql in enumerate(lStatements, 1):
            sWhich = sName + (" statement " + str(iIndex) if len(lStatements) > 1 else "")
            try:
                lRows = oDb.execute(sSql).fetchall()
            except Exception as oError:
                fail(sRoot, sWhich + " does not run: " + str(oError))
                continue
            iRun += 1
            log(sRoot + ": " + sWhich + " gave " + plural(len(lRows), "row"))
            if not lRows: fail(sRoot, sWhich + " runs but gives no rows on the shipped data, so a broken join could hide there")
    return iRun


def checkSettings(sRoot, sFolder, oDb):
    """<root>.inix: the columns each table is shown and sorted by exist, and its filter is valid."""
    sPath = os.path.join(sFolder, sRoot + ".inix")
    if not os.path.isfile(sPath): return 0
    lTables = tables(oDb)
    for sSection, dKeys in readInix(sPath):
        if sSection == "Database":
            sInitial = dKeys.get("InitialTable", "")
            if sInitial and sInitial not in lTables: fail(sRoot, "InitialTable " + sInitial + " is not a table")
            continue
        if not sSection.startswith("Table:"): continue
        sTable = sSection[6:].strip()
        if sTable not in lTables:
            fail(sRoot, sRoot + ".inix describes the table " + sTable + ", which is not in the database")
            continue
        lCols = columnNames(oDb, sTable)
        for sKey in ("SelectFields", "OrderFields"):
            for sItem in [s.strip() for s in dKeys.get(sKey, "").split(",") if s.strip()]:
                sCol = re.sub(r"\s+(asc|desc)$", "", sItem, flags=re.I).strip().strip('"[]').lower()
                if sCol not in lCols: fail(sRoot, sRoot + ".inix " + sTable + " " + sKey + " names " + sCol + ", which is not a column")
        sWhere = dKeys.get("WhereFilter", "").strip()
        if sWhere:
            try:
                oDb.execute("SELECT count(*) FROM " + quoted(sTable) + " WHERE " + sWhere).fetchone()
            except Exception as oError:
                fail(sRoot, sRoot + ".inix " + sTable + " WhereFilter does not run: " + str(oError))
    return 1


def checkReports(sRoot, sFolder, oDb):
    """report.inix: each report names a table it can run on, and its filter, sort, group and fields work there."""
    sPath = os.path.join(sFolder, "report.inix")
    if not os.path.isfile(sPath): return 0
    lTables, iReports = tables(oDb), 0
    for sSection, dKeys in readInix(sPath):
        if not sSection: continue
        iReports += 1
        sTable = dKeys.get("@table", "").strip()
        if not sTable:
            fail(sRoot, "report " + sSection + " names no @table, so it runs on whatever table happens to be open")
            continue
        if sTable not in lTables:
            fail(sRoot, "report " + sSection + " is for the table " + sTable + ", which is not in the database")
            continue
        lCols = columnNames(oDb, sTable)
        sSql = "SELECT * FROM " + quoted(sTable)
        if dKeys.get("@filter", "").strip(): sSql += " WHERE " + dKeys["@filter"].strip()
        if dKeys.get("@sort", "").strip(): sSql += " ORDER BY " + dKeys["@sort"].strip()
        try:
            iRows = len(oDb.execute(sSql).fetchall())
            log(sRoot + ": report " + sSection + " reads " + plural(iRows, "row") + " of " + sTable)
        except Exception as oError:
            fail(sRoot, "report " + sSection + " @filter or @sort does not run: " + str(oError))
        sGroup = dKeys.get("@group", "").strip().lower()
        if sGroup and sGroup not in lCols: fail(sRoot, "report " + sSection + " groups by " + sGroup + ", which is not a column of " + sTable)
        for sBand in lBandKeys:
            sText = re.sub(r"\{\{.*?\}\}", "", dKeys.get(sBand, ""), flags=re.S)
            for sField in re.findall(r"\$([A-Za-z_][A-Za-z0-9_]*)", sText):
                if sField.lower() == "count" or re.match(r"(sum|avg|min|max)_", sField, re.I): continue
                if sField.lower() not in lCols: fail(sRoot, "report " + sSection + " " + sBand + " uses $" + sField + ", which is not a column of " + sTable)
    return iReports


def checkStoredViews(sRoot, oDb):
    """The views table: each table it describes exists, the columns it shows and sorts by exist, and its filter runs.
    These are the settings a database carries inside itself, the same ones <root>.inix holds, and were not checked
    (14 October 2026, from an audit by another AI)."""
    lTables = tables(oDb)
    if "views" not in lTables: return 0
    iRows = 0
    for sTable, sField, sValue in oDb.execute("SELECT tbl, fld, coalesce(val, '') FROM views").fetchall():
        iRows += 1
        if sTable not in lTables:
            fail(sRoot, "views describes the table " + str(sTable) + ", which is not in the database")
            continue
        if sField in ("SelectFields", "OrderFields"):
            lCols = columnNames(oDb, sTable)
            for sItem in [s.strip() for s in sValue.split(",") if s.strip()]:
                sCol = re.sub(r"\s+(asc|desc)$", "", sItem, flags=re.I).strip().strip('"[]').lower()
                if sCol not in lCols: fail(sRoot, "views " + sTable + " " + sField + " names " + sCol + ", which is not a column")
        elif sField == "WhereFilter" and sValue.strip():
            try:
                oDb.execute("SELECT count(*) FROM " + quoted(sTable) + " WHERE " + sValue).fetchone()
            except Exception as oError:
                fail(sRoot, "views " + sTable + " WhereFilter does not run: " + str(oError))
    return iRows


def readScript(sPath):
    """The table, filter and order of a .dbdo script, in either of its forms: settings (table = jobs, where = ...,
    order = ...) or dot-prompt commands (table orders, filter ..., sort ...). Comment lines and ? lines are skipped."""
    dScript = {}
    for sLine in open(sPath, encoding="utf-8-sig").read().splitlines():
        sText = sLine.strip()
        if not sText or sText[0] in ";#?": continue
        oMatch = re.match(r"(table|where|order)\s*=\s*(.*)$", sText, re.I)
        if oMatch:
            dScript[oMatch.group(1).lower()] = oMatch.group(2).strip()
            continue
        oMatch = re.match(r"(table|filter|sort)\s+(.*)$", sText, re.I)
        if oMatch: dScript[{"table": "table", "filter": "where", "sort": "order"}[oMatch.group(1).lower()]] = oMatch.group(2).strip()
    return dScript


def checkScripts(sRoot, sFolder, oDb):
    """Every .dbdo script beside the database: it names a table that exists, and its filter and order run together on
    it, as Invoke Script would run them (14 October 2026, from an audit by another AI)."""
    iScripts = 0
    for sPath in sorted(glob.glob(os.path.join(sFolder, "*.dbdo"))):
        iScripts += 1
        sName = os.path.basename(sPath)
        dScript = readScript(sPath)
        sTable = dScript.get("table", "")
        if not sTable:
            fail(sRoot, sName + " names no table")
            continue
        if sTable not in tables(oDb):
            fail(sRoot, sName + " names the table " + sTable + ", which is not in the database")
            continue
        sSql = "SELECT count(*) FROM " + quoted(sTable)
        if dScript.get("where"): sSql += " WHERE " + dScript["where"]
        if dScript.get("order"): sSql += " ORDER BY " + dScript["order"]
        try:
            oDb.execute(sSql).fetchone()
        except Exception as oError:
            fail(sRoot, sName + " does not run: " + str(oError))
    return iScripts


def checkRecordNames(sRoot, oDb):
    """No record has an empty prime or look. A prime names a record, so maps point at it by its prime; a look is what
    a row says. An empty one is a record nothing can point at and nothing speaks (14 October 2026, from an audit by
    another AI)."""
    for sTable in tables(oDb):
        lCols = columnNames(oDb, sTable)
        for sCol in ("prime", "look"):
            if sCol not in lCols: continue
            iEmpty = oDb.execute("SELECT count(*) FROM " + quoted(sTable) + " WHERE " + sCol + " IS NULL OR trim(" + sCol + ") = ''").fetchone()[0]
            if iEmpty: fail(sRoot, sTable + " has " + plural(iEmpty, "record") + " with no " + sCol)


def checkTemplate(sFolder, bRepair):
    """Repairs (when asked) and checks one template folder."""
    sRoot = os.path.basename(sFolder)
    sDb = os.path.join(sFolder, sRoot + ".db")
    if not os.path.isfile(sDb):
        return fail(sRoot, "the folder holds no " + sRoot + ".db, so the installer would ship this template without its database")
    log("Template " + sRoot + ": " + sDb + ", " + "{:,}".format(os.path.getsize(sDb)) + " bytes")
    if bRepair: repair(sRoot, sDb)
    oDb = sqlite3.connect("file:" + sDb.replace("\\", "/") + "?mode=ro", uri=True)
    try:
        checkDatabase(sRoot, oDb)
        iSql = checkSqlFiles(sRoot, sFolder, oDb)
        checkSettings(sRoot, sFolder, oDb)
        iReports = checkReports(sRoot, sFolder, oDb)
        iViews = checkStoredViews(sRoot, oDb)
        iScripts = checkScripts(sRoot, sFolder, oDb)
        checkRecordNames(sRoot, oDb)
        log(sRoot + ": " + plural(iSql, "SQL statement") + " ran, " + plural(iReports, "report") + ", " + plural(iViews, "stored view setting")
            + " and " + plural(iScripts, "script") + " checked")
    finally:
        oDb.close()
    return True


def main():
    global sLogPath
    os.makedirs(os.path.join(sProject, "logs"), exist_ok=True)
    sLogPath = os.path.join(sProject, "logs", "DbDo-checkTemplates-" + datetime.datetime.now().strftime("%Y%m%d-%H%M%S") + ".log")
    log("checkTemplates.py started; " + sVersion + "; script " + os.path.abspath(__file__))
    log("Python " + sys.version.split()[0] + " at " + sys.executable + "; SQLite " + sqlite3.sqlite_version + "; " + platform.platform() + "; cwd " + os.getcwd() + "; command line " + " ".join(sys.argv))
    lArgs = sys.argv[1:]
    lUnknown = [s for s in lArgs if s.startswith("--") and s not in ("--repair", "--schema")]
    if lUnknown:
        say("Unknown option " + lUnknown[0] + ". The options are --repair and --schema <template>.")
        return 2
    bRepair = "--repair" in lArgs
    lSchema = [lArgs[i + 1] for i, s in enumerate(lArgs[:-1]) if s == "--schema"]
    if "--schema" in lArgs and not lSchema:
        say("--schema needs a template name after it, such as RadioTrail.")
        return 2
    lNames = [s for i, s in enumerate(lArgs) if not s.startswith("--") and not (i > 0 and lArgs[i - 1] == "--schema")]
    sTemplates = os.path.join(sProject, "templates")
    lFolders = sorted((s for s in glob.glob(os.path.join(sTemplates, "*")) if os.path.isdir(s)), key=lambda s: os.path.basename(s).lower())
    if lNames: lFolders = [s for s in lFolders if os.path.basename(s).lower() in [n.lower() for n in lNames]]
    log("Settings: repair=" + str(bRepair) + "; schema for " + (", ".join(lSchema) or "none") + "; templates " + ", ".join(os.path.basename(s) for s in lFolders))
    try:
        for sFolder in lFolders:
            checkTemplate(sFolder, bRepair)
            if os.path.basename(sFolder).lower() in [s.lower() for s in lSchema]: saveSchema(os.path.basename(sFolder), sFolder)
    except Exception as oError:
        log("CRASH: " + traceback.format_exc())
        say("checkTemplates hit an error: " + str(oError).splitlines()[0][:200] + ". Log: " + sLogPath)
        return 4
    if lRepairs: say("Template repairs: " + plural(len(lRepairs), "change") + " made; the log lists each.")
    if lErrors:
        say("Template check: " + plural(len(lErrors), "problem") + " in " + plural(len({s.split(":")[0] for s in lErrors}), "template") + ":")
        for s in lErrors[:30]: say("  " + s)
        if len(lErrors) > 30: say("  and " + str(len(lErrors) - 30) + " more in the log.")
        say("Log: " + sLogPath)
        return 1
    say("Template check: " + plural(len(lFolders), "template") + " passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
