r"""describeFields.py -- write a one-sentence description of every field into each template's defs table.

WHY (3 November 2026). Shift+F1 on a field in DbDo's record, new-record and filter forms says what the field is
for. The sentences live in templates\fieldDescriptions.inix, one per field, and this step, run by build.cmd after
the template check, writes them into the defs table of every template database, as rows with tbl and fld filled,
val empty and descrip the sentence. [*] in the file is written as tbl '*', which DbDo reads for a field of that name
in any table; a table's own row wins. A table's own id, such as station_id, gets a sentence of its own.

THE RULE FOR EXISTING ROWS. A row this step wrote before (src 'fieldDescriptions') is updated when its sentence
changes; a row with an empty descrip is filled; a sentence anyone else wrote is never replaced.

USAGE
    describeFields                 every template database under templates
    describeFields --dry           say what would change, change nothing

Writes logs\DbDo-describeFields-<stamp>.log. Exit code 0, or 1 when a database could not be written.
"""

import datetime, glob, os, platform, re, sqlite3, sys

sSource = "fieldDescriptions"
sIdSentence = "The record's own number, given by DbDo; it never changes."

sScriptDir = os.path.dirname(os.path.abspath(__file__))
sProject = os.path.dirname(sScriptDir)
sStamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
sLogPath = os.path.join(sProject, "logs", "DbDo-describeFields-" + sStamp + ".log")


def logLine(sText):
    os.makedirs(os.path.dirname(sLogPath), exist_ok=True)
    with open(sLogPath, "a", encoding="utf-8") as fLog: fLog.write(datetime.datetime.now().strftime("%H:%M:%S") + "  " + sText + "\n")
    return True


def readSentences(sPath):
    """The file's sections: {section: {field: sentence}}."""
    dSections, sSection = {}, None
    with open(sPath, "r", encoding="utf-8-sig") as fIn:
        for sRaw in fIn:
            sLine = sRaw.strip()
            if not sLine or sLine.startswith(";"): continue
            oMatch = re.match(r"^\[(.+)\]$", sLine)
            if oMatch:
                sSection = oMatch.group(1).strip()
                dSections.setdefault(sSection, {})
                continue
            if sSection and "=" in sLine:
                sKey, sValue = [s.strip() for s in sLine.split("=", 1)]
                dSections[sSection][sKey] = sValue
    return dSections


def describeDatabase(sDb, sTemplate, dSections, bDry):
    """Writes the sentences into one database. Returns (written, updated, kept)."""
    iWritten = iUpdated = iKept = 0
    oConn = sqlite3.connect(sDb)
    try:
        lLookupCols = [r[1] for r in oConn.execute("PRAGMA table_xinfo(defs)")]
        if "descrip" not in lLookupCols:
            logLine(sTemplate + ": no defs table with a descrip column; nothing written")
            return (0, 0, 0)
        # THE PATTERN COLUMN (3 November 2026): a field's own row holds what it is for (descrip) and the form its
        # value takes (pattern), so the value column stays for the choices F4 offers.
        if "pattern" not in lLookupCols:
            if not bDry: oConn.execute("ALTER TABLE defs ADD COLUMN pattern TEXTLINE")
            logLine(sTemplate + ": defs gained its pattern column")
        lWanted = []
        for sField, sSentence in dSections.get("*", {}).items(): lWanted.append(("*", sField, sSentence))
        lTables = [r[0] for r in oConn.execute("SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'")]
        for sTable in lTables:
            dOwn = dSections.get(sTemplate + "/" + sTable) or dSections.get(sTable) or {}
            for oRow in oConn.execute('PRAGMA table_xinfo("' + sTable.replace('"', '""') + '")'):
                sField, bPrimaryKey = oRow[1], oRow[5] > 0
                if sField in dSections.get("*", {}) and sField not in dOwn: continue
                sSentence = dOwn.get(sField, "")
                if not sSentence and bPrimaryKey and sField.endswith("_id"): sSentence = sIdSentence
                if not sSentence:
                    logLine(sTemplate + ": no sentence for " + sTable + "." + sField)
                    continue
                lWanted.append((sTable, sField, sSentence))
        # THE TABLES THEMSELVES (3 November 2026): [tables] keys "<template>/<table>", or a DbDo table's name alone,
        # written with fld empty, so DbDo's table lists and Database Summary can say what each table is for.
        dTables = dSections.get("tables", {})
        for sTable in lTables:
            sSentence = dTables.get(sTemplate + "/" + sTable) or dTables.get(sTable) or ""
            if sSentence: lWanted.append((sTable, "", sSentence))
            else: logLine(sTemplate + ": no sentence for the table " + sTable)
        # THE DATABASE ITSELF (3 November 2026): [database] by template name, as a row with tbl * and fld and val empty --
        # "every table", the whole database. Not tbl empty, as first written: a row's look is built from tbl, fld and val,
        # so it had no look, and the template check refuses a record with none. A * field row always names its field.
        oConn.execute("UPDATE defs SET tbl = '*' WHERE coalesce(tbl, '') = '' AND coalesce(fld, '') = '' AND coalesce(val, '') = '' AND src = ?", (sSource,))
        sAboutDb = dSections.get("database", {}).get(sTemplate, "")
        if sAboutDb: lWanted.append(("*", "", sAboutDb))
        for sTable, sField, sSentence in lWanted:
            oHave = oConn.execute("SELECT def_id, coalesce(descrip, ''), coalesce(src, '') FROM defs WHERE coalesce(tbl, '') = ? AND coalesce(fld, '') = ? AND coalesce(val, '') = '' ORDER BY def_id LIMIT 1", (sTable, sField)).fetchone()
            if oHave is None:
                iWritten += 1
                if not bDry:
                    oConn.execute("INSERT INTO defs (tbl, fld, val, src, descrip) VALUES (?, ?, NULL, ?, ?)", (sTable or None, sField or None, sSource, sSentence))
            elif oHave[1] == "" or (oHave[2] == sSource and oHave[1] != sSentence):
                iUpdated += 1
                if not bDry:
                    oConn.execute("UPDATE defs SET descrip = ?, src = ?, edited = CURRENT_TIMESTAMP WHERE def_id = ?", (sSentence, sSource, oHave[0]))
            else:
                iKept += 1
        # Each field's pattern, into the pattern column of its own row: set where it is empty or this step wrote it.
        iPatterns = 0
        for sKey, sPattern in dSections.get("patterns", {}).items():
            lParts = sKey.split("/")
            if len(lParts) != 3 or lParts[0] != sTemplate: continue
            oRow = oConn.execute("SELECT def_id, coalesce(pattern, ''), coalesce(src, '') FROM defs WHERE tbl = ? AND fld = ? AND coalesce(val, '') = '' ORDER BY def_id LIMIT 1", (lParts[1], lParts[2])).fetchone() if not bDry or "pattern" in lLookupCols else None
            if oRow is None:
                # A STANDARD FIELD, such as url, is described once for every table, in a * row (5 November 2026); its
                # pattern needs a row of its own table, so one is added, carrying the shared description.
                if bDry: logLine(sTemplate + ": would add a row for the pattern of " + lParts[1] + "." + lParts[2]); continue
                oStar = oConn.execute("SELECT coalesce(descrip, '') FROM defs WHERE tbl = '*' AND fld = ? AND coalesce(val, '') = '' LIMIT 1", (lParts[2],)).fetchone()
                oConn.execute("INSERT INTO defs (tbl, fld, val, src, descrip, pattern) VALUES (?, ?, NULL, ?, ?, ?)", (lParts[1], lParts[2], sSource, (oStar[0] if oStar and oStar[0] else None), sPattern))
                iPatterns += 1
                logLine(sTemplate + ": a row for the pattern of " + lParts[1] + "." + lParts[2] + " was added")
                continue
            if oRow[1] == sPattern or (oRow[1] and oRow[2] != sSource): continue
            iPatterns += 1
            if not bDry: oConn.execute("UPDATE defs SET pattern = ?, edited = CURRENT_TIMESTAMP WHERE def_id = ?", (sPattern, oRow[0]))
        if iPatterns: logLine("%s: %d pattern%s written" % (sTemplate, iPatterns, "" if iPatterns == 1 else "s"))
        iUpdated += iPatterns
        if not bDry: oConn.commit()
    finally:
        oConn.close()
    logLine("%s: %d written, %d updated, %d kept as they were" % (sTemplate, iWritten, iUpdated, iKept))
    return (iWritten, iUpdated, iKept)


def renameToDefs(sDb, sTemplate, bDry):
    """LOOKUPS BECOMES DEFS (4 November 2026, by Jamal's choice). DbDo's own table of descriptions, patterns and choices
    is named defs, its key def_id: a plain plural whose every row is a definition, short to type, and no echo of the
    standard field look. A template still holding the old table is renamed here, before anything reads it: the table,
    its key, and the triggers and indexes whose names carried the old name (SQLite rewrites their bodies on a rename,
    but not their names). Only DbDo's own table -- one with tbl, fld and val -- is renamed, and only when no defs exists.
    Answers True when it renamed."""
    sOld, sOldId = "look" + "ups", "look" + "up_id"   # spelled in two parts so the renaming of the sources leaves them
    oConn = sqlite3.connect(sDb)
    try:
        lNames = [r[0] for r in oConn.execute("SELECT name FROM sqlite_master WHERE type = 'table'")]
        if sOld not in lNames or "defs" in lNames: return False
        lCols = [r[1] for r in oConn.execute("PRAGMA table_info(%s)" % sOld)]
        if not all(c in lCols for c in ("tbl", "fld", "val")): return False
        if bDry: logLine(sTemplate + ": would rename " + sOld + " to defs"); return True
        oConn.execute("ALTER TABLE %s RENAME TO defs" % sOld)
        if sOldId in lCols: oConn.execute("ALTER TABLE defs RENAME COLUMN %s TO def_id" % sOldId)
        # Triggers and indexes: their names, and in a trigger the table's name as a quoted value -- the maps triggers
        # say tbl1 = 'lookups', which a rename does not touch, and without which a renamed definition loses its links.
        sQuoted = "'" + sOld + "'"
        for sType, sName, sSql in oConn.execute("SELECT type, name, sql FROM sqlite_master WHERE tbl_name = 'defs' AND type IN ('trigger', 'index') AND sql IS NOT NULL").fetchall():
            sNew = sName.replace(sOldId, "def_id").replace(sOld, "defs")
            sNewSql = sSql.replace(sName, sNew, 1).replace(sQuoted, "'defs'")
            if sNew == sName and sNewSql == sSql: continue
            oConn.execute("DROP %s %s" % (sType.upper(), sName))
            oConn.execute(sNewSql)
            logLine("  %s %s rebuilt as %s" % (sType, sName, sNew))
        # Rows that name the table: its own descriptions and choices in defs, and its links in maps.
        oConn.execute("UPDATE defs SET tbl = 'defs' WHERE tbl = ?", (sOld,))
        oConn.execute("UPDATE defs SET fld = 'def_id' WHERE tbl = 'defs' AND fld = ?", (sOldId,))
        # The old views table's rows for the renamed table follow it, so retiring views never writes a section for a
        # table that is gone (13:20 build, 4 November: six templates' .inix gained [Table:lookups]).
        if "views" in lNames: oConn.execute("UPDATE views SET tbl = 'defs' WHERE tbl = ?", (sOld,))
        if "maps" in lNames:
            oConn.execute("UPDATE maps SET tbl1 = 'defs' WHERE tbl1 = ?", (sOld,))
            oConn.execute("UPDATE maps SET tbl2 = 'defs' WHERE tbl2 = ?", (sOld,))
        oConn.commit()
        logLine(sTemplate + ": " + sOld + " renamed defs, with def_id")
        return True
    finally:
        oConn.close()


def lookExpression(lsFields):
    """Look as DbDo's builders write it: each field with a separator, blanks left out, the last separator trimmed."""
    return "rtrim(" + " || ".join("iif(%s IS NOT NULL AND length(CAST(%s AS TEXT))>0, CAST(%s AS TEXT) || ' | ', '')" % (f, f, f) for f in lsFields) + ", ' | ')"


def primeExpression(lsFields):
    """Prime as DbDo's builders write it: each field as text, joined by bars."""
    return "||'|'||".join("coalesce(CAST(%s AS TEXT),'')" % f for f in lsFields)


def generatedSpan(sSql, sColumn):
    """Where the expression of a generated column look or prime sits in a CREATE TABLE: (start, end), or None."""
    oMatch = re.search(r'"?\b' + sColumn + r'\b"?\s+TEXT\s+GENERATED ALWAYS AS\s*\(', sSql, re.I)
    if not oMatch: return None
    iStart = oMatch.end(); iDepth = 1; iAt = iStart
    while iDepth and iAt < len(sSql):
        iDepth += {"(": 1, ")": -1}.get(sSql[iAt], 0); iAt += 1
    return (iStart, iAt - 1)


def fieldsOf(sExpression):
    lOut = []
    for sField in re.findall(r'CAST\(\s*"?(\w+)"?\s+AS\s+TEXT', sExpression, re.I) + re.findall(r'iif\(\s*"?(\w+)"?\s+IS NOT NULL', sExpression, re.I):
        if sField not in lOut: lOut.append(sField)
    return lOut


def applyLookPrime(sDb, sTemplate, dSections, bDry):
    """LOOK AND PRIME AS DECIDED (5 November 2026). [lookprime] in fieldDescriptions.inix names a table's look or prime;
    a table whose definition differs is rebuilt with the new one: its rows and ids kept, its indexes and triggers made
    again, and its maps links re-pointed, since a link names a record by its prime. A new prime that is not unique
    stops the change for that table, logged, with nothing altered. A table that already matches is left alone."""
    lWanted = [(k.split("/")[1], k.split("/")[2], [f.strip() for f in v.split(",") if f.strip()])
               for k, v in dSections.get("lookprime", {}).items() if k.count("/") == 2 and k.split("/")[0] == sTemplate]
    if not lWanted: return 0
    oConn = sqlite3.connect(sDb)
    iRebuilt = 0
    try:
        lTables = [r[0] for r in oConn.execute("SELECT name FROM sqlite_master WHERE type = 'table'")]
        for sTable in sorted(set(t for t, _c, _f in lWanted)):
            if sTable not in lTables: logLine(sTemplate + ": [lookprime] names " + sTable + ", which is not here"); continue
            sSql = oConn.execute("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", (sTable,)).fetchone()[0]
            sNewSql, lChanges = sSql, []
            for sWantTable, sColumn, lsFields in lWanted:
                if sWantTable != sTable or sColumn.lower() not in ("look", "prime"): continue
                oSpan = generatedSpan(sNewSql, sColumn)
                if oSpan is None: logLine(sTemplate + ": " + sTable + " has no generated " + sColumn); continue
                if [f.lower() for f in fieldsOf(sNewSql[oSpan[0]:oSpan[1]])] == [f.lower() for f in lsFields]: continue
                sExpr = lookExpression(lsFields) if sColumn.lower() == "look" else primeExpression(lsFields)
                sNewSql = sNewSql[:oSpan[0]] + sExpr + sNewSql[oSpan[1]:]
                lChanges.append((sColumn.lower(), lsFields, sExpr))
            if not lChanges: continue
            sNewPrime = next((e for c, _f, e in lChanges if c == "prime"), None)
            if sNewPrime:
                iDuplicates = oConn.execute('SELECT COUNT(*) FROM (SELECT (%s) AS p, COUNT(*) AS n FROM "%s" GROUP BY p HAVING n > 1)' % (sNewPrime, sTable)).fetchone()[0]
                if iDuplicates:
                    logLine("ERROR " + sTemplate + ": the new prime of " + sTable + " is not unique (" + str(iDuplicates) + " values repeat); left as it was")
                    continue
            if bDry: logLine(sTemplate + ": would rebuild " + sTable + " for " + ", ".join(c for c, _f, _e in lChanges)); continue
            lCols = [r[1] for r in oConn.execute('PRAGMA table_xinfo("%s")' % sTable) if r[6] == 0]  # stored, not generated
            sId = next((r[1] for r in oConn.execute('PRAGMA table_info("%s")' % sTable) if r[5] == 1), None)
            lOldPrime = oConn.execute('SELECT "%s", prime FROM "%s"' % (sId, sTable)).fetchall() if sNewPrime and sId else []
            lExtras = [r[0] for r in oConn.execute("SELECT sql FROM sqlite_master WHERE tbl_name = ? AND type IN ('index', 'trigger') AND sql IS NOT NULL", (sTable,))]
            sQuotedCols = ", ".join('"%s"' % c for c in lCols)
            oConn.execute("PRAGMA foreign_keys = OFF")
            oConn.execute("BEGIN")
            oConn.execute(sNewSql.replace(sSql[:sSql.index("(")], 'CREATE TABLE "%s__rebuild" ' % sTable, 1))
            oConn.execute('INSERT INTO "%s__rebuild" (%s) SELECT %s FROM "%s"' % (sTable, sQuotedCols, sQuotedCols, sTable))
            oConn.execute('DROP TABLE "%s"' % sTable)
            oConn.execute('ALTER TABLE "%s__rebuild" RENAME TO "%s"' % (sTable, sTable))
            for sExtra in lExtras: oConn.execute(sExtra)
            iLinks = 0
            if lOldPrime and "maps" in lTables:
                dNew = dict(oConn.execute('SELECT "%s", prime FROM "%s"' % (sId, sTable)).fetchall())
                for iId, sOld in lOldPrime:
                    sNew = dNew.get(iId)
                    if sNew is None or sNew == sOld: continue
                    iLinks += oConn.execute("UPDATE maps SET prime1 = ? WHERE tbl1 = ? AND prime1 = ?", (sNew, sTable, sOld)).rowcount
                    iLinks += oConn.execute("UPDATE maps SET prime2 = ? WHERE tbl2 = ? AND prime2 = ?", (sNew, sTable, sOld)).rowcount
            oConn.execute("COMMIT")
            iRebuilt += 1
            for sColumn, lsFields, _e in lChanges: logLine("%s: %s %s is now %s" % (sTemplate, sTable, sColumn, ", ".join(lsFields)))
            logLine("%s: %s rebuilt with %d index%s and trigger%s kept; %d maps link%s re-pointed" % (sTemplate, sTable, len(lExtras), "es" if len(lExtras) != 1 else "", "s", iLinks, "" if iLinks == 1 else "s"))
    finally:
        oConn.close()
    return iRebuilt


# THE FILES TABLE (5 November 2026): a twin of DbDoManager.sFilesTableSql and aFilesTableExtras in DbDo.cs; change both
# together. Each row is a file a database carries -- a script, a report or transfer definition, its settings.
lsFilesTableSql = [
    "CREATE TABLE IF NOT EXISTS \"files\" (file_id INTEGER PRIMARY KEY AUTOINCREMENT, "
    "added TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP, edited TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP, "
    "name TEXTLINE NOT NULL, kind TEXTLINE, content TEXTMEMO, url TEXTLINE, notes TEXTMARKDOWN, tags TEXTMEMO, "
    "look TEXT GENERATED ALWAYS AS (coalesce(CAST(name AS TEXT),'')) STORED, "
    "prime TEXT GENERATED ALWAYS AS (coalesce(CAST(name AS TEXT),'')) STORED, marked INTEGER NOT NULL DEFAULT 0)",
    "CREATE UNIQUE INDEX IF NOT EXISTS \"idx_files_prime\" ON \"files\" (prime)",
    "CREATE TRIGGER IF NOT EXISTS \"trg_files_edited\" AFTER UPDATE OF \"name\", \"kind\", \"content\", \"url\", \"notes\", \"tags\" ON \"files\" FOR EACH ROW "
    "WHEN OLD.\"name\" IS NOT NEW.\"name\" OR OLD.\"kind\" IS NOT NEW.\"kind\" OR OLD.\"content\" IS NOT NEW.\"content\" OR OLD.\"url\" IS NOT NEW.\"url\" OR OLD.\"notes\" IS NOT NEW.\"notes\" OR OLD.\"tags\" IS NOT NEW.\"tags\" "
    "BEGIN UPDATE \"files\" SET edited = CURRENT_TIMESTAMP WHERE \"file_id\" = NEW.\"file_id\"; END",
    "CREATE TRIGGER IF NOT EXISTS \"trg_files_maps\" AFTER UPDATE ON \"files\" FOR EACH ROW WHEN OLD.prime IS NOT NEW.prime BEGIN "
    "UPDATE maps SET prime1 = NEW.prime WHERE tbl1 = 'files' AND prime1 = OLD.prime; UPDATE maps SET prime2 = NEW.prime WHERE tbl2 = 'files' AND prime2 = OLD.prime; END",
    "CREATE TRIGGER IF NOT EXISTS \"trg_files_maps_delete\" AFTER DELETE ON \"files\" FOR EACH ROW BEGIN "
    "DELETE FROM maps WHERE (tbl1 = 'files' AND prime1 = OLD.prime) OR (tbl2 = 'files' AND prime2 = OLD.prime); END"]


def kitColumnTypes():
    """The kit's vocabulary of column types, scripts\\columnTypes.py, from the kit DbDo builds against."""
    import importlib.util
    for sKit in (os.environ.get("homerDev", ""), os.environ.get("HomerDev", ""), r"C:\HomerDev", os.path.join(os.path.dirname(sProject), "HomerDev")):
        sPath = os.path.join(sKit, "scripts", "columnTypes.py") if sKit else ""
        if sPath and os.path.isfile(sPath):
            oSpec = importlib.util.spec_from_file_location("columnTypes", sPath); oModule = importlib.util.module_from_spec(oSpec)
            oSpec.loader.exec_module(oModule); return oModule
    raise RuntimeError("the kit's scripts\\columnTypes.py was not found; build HomerDev 1.138.0 or later first")


def packCarried(sDb, sTemplate, bDry):
    """A TEMPLATE IS ITS OWN PACKAGE (5 November 2026). The files beside a template's database -- its scripts (.sql, .dbdo,
    .js), its report and transfer definitions and its own settings (.inix, carried as settings.inix) -- are packed into
    its files table, so the .db alone carries everything; DbDo reads a file beside a database first, the carried copy
    when there is none. A row is rewritten only when its text has changed. Answers how many rows were written."""
    sFolder = os.path.dirname(sDb); sOwnInix = os.path.splitext(os.path.basename(sDb))[0].lower() + ".inix"
    lPaths = [os.path.join(sFolder, n) for n in sorted(os.listdir(sFolder), key=str.lower)
              if n.lower().endswith((".sql", ".dbdo", ".js", ".inix")) and os.path.isfile(os.path.join(sFolder, n))]
    if not lPaths: return 0
    oTypes = kitColumnTypes()
    oConn = sqlite3.connect(sDb)
    try:
        if not bDry:
            # A files table made before kind was added (5 November 2026) holds only what is packed from disk: remake it.
            lsCols = [r[1] for r in oConn.execute("PRAGMA table_info(files)")]
            if lsCols and "kind" not in lsCols: oConn.execute("DROP TABLE files"); logLine("%s: files table remade with kind" % sTemplate)
            for sSql in lsFilesTableSql: oConn.execute(sSql)
        iWritten = 0
        for sPath in lPaths:
            sName = os.path.basename(sPath)
            if sName.lower() == sOwnInix: sName = "settings.inix"
            sText = open(sPath, encoding="utf-8-sig").read().replace("\r\n", "\n").replace("\n", "\r\n")
            sKind = oTypes.typeForName(sName)
            oRow = None if bDry else oConn.execute("SELECT content, kind FROM files WHERE lower(name) = lower(?)", (sName,)).fetchone()
            if oRow is not None and oRow[0] == sText and oRow[1] == sKind: continue
            if bDry: logLine("%s: would carry %s" % (sTemplate, sName)); continue
            oConn.execute("DELETE FROM files WHERE lower(name) = lower(?)", (sName,))
            oConn.execute("INSERT INTO files (name, kind, content) VALUES (?, ?, ?)", (sName, sKind, sText))
            iWritten += 1
            logLine("%s: carries %s, %d characters" % (sTemplate, sName, len(sText)))
        oConn.commit()
        return iWritten
    finally:
        oConn.close()


def compactIfLoose(sDb, sTemplate, bDry):
    """FREED SPACE RETURNED (5 November 2026). Rebuilding a table, or dropping one, leaves its old pages empty inside the
    file until SQLite's VACUUM; RadioTrail.db grew from 61.9 MB to 112.6 MB after its stations table was rebuilt for the
    new prime, over GitHub's 100 MB limit, and the push refused it. A template whose free pages pass 1 MB, or a twentieth
    of the file, is compacted; RadioTrail's came back to 70.9 MB."""
    oConn = sqlite3.connect(sDb)
    try:
        iFree = oConn.execute("PRAGMA freelist_count").fetchone()[0] * oConn.execute("PRAGMA page_size").fetchone()[0]
        iSize = os.path.getsize(sDb)
        if iFree < 1048576 and iFree * 20 < iSize: return False
        if bDry: logLine("%s: would compact, %d bytes free of %d" % (sTemplate, iFree, iSize)); return True
        oConn.execute("VACUUM")
    finally:
        oConn.close()
    logLine("%s: compacted from %d bytes to %d (%d were free pages)" % (sTemplate, iSize, os.path.getsize(sDb), iFree))
    return True


def retireViews(sDb, sTemplate, bDry):
    """THE VIEWS TABLE IS RETIRED (4 November 2026). DbDo once kept each table's shown columns, sort and filter in a table
    named views inside the database. It was never approved as part of DbDo's schema, and it added nothing: every one of
    its 147 rows across the templates was already in the template's own .inix, which DbDo reads first. So a row the .inix
    lacks is written there, under [Table:<tbl>], and the table is dropped. Only DbDo's own views -- columns tbl, fld and
    val -- is touched; a table of someone's own with that name stays. Answers how many rows moved, or -1 for none there."""
    oConn = sqlite3.connect(sDb)
    try:
        lCols = [r[1] for r in oConn.execute("PRAGMA table_info(views)")]
        if [s.lower() for s in lCols] != ["tbl", "fld", "val"]: return -1
        sInix = os.path.join(os.path.dirname(sDb), os.path.splitext(os.path.basename(sDb))[0] + ".inix")
        dHave, sSection = {}, ""
        if os.path.exists(sInix):
            for sLine in open(sInix, encoding="utf-8-sig"):
                sLine = sLine.strip()
                if sLine.startswith("[") and sLine.endswith("]"): sSection = sLine[1:-1].strip().lower(); continue
                if sSection and "=" in sLine and not sLine.startswith(";"): dHave[(sSection, sLine.split("=", 1)[0].strip().lower())] = sLine.split("=", 1)[1].strip()
        lTables = [r[0].lower() for r in oConn.execute("SELECT name FROM sqlite_master WHERE type IN ('table', 'view')")]
        lMove = [(t, k, v) for t, k, v in oConn.execute("SELECT tbl, fld, coalesce(val, '') FROM views")
                 if v and t.lower() in lTables and not dHave.get(("table:" + t.lower(), k.lower()))]
        if lMove and not bDry:
            with open(sInix, "a", encoding="utf-8", newline="\r\n") as fOut:
                fOut.write("\n; Saved view settings moved from the retired views table (describeFields, 4 November 2026).\n")
                for t, k, v in lMove: fOut.write("[Table:%s]\n%s = %s\n" % (t, k, v))
        if not bDry:
            oConn.execute("DROP TABLE views")
            # Its descriptions go with it: this step wrote them, and they would now describe a table that is not there.
            oConn.execute("DELETE FROM defs WHERE tbl = 'views' AND src = ?", (sSource,))
            oConn.commit()
        logLine("%s: views table retired; %d row%s moved to %s" % (sTemplate, len(lMove), "" if len(lMove) == 1 else "s", os.path.basename(sInix)))
        return len(lMove)
    finally:
        oConn.close()


def repairInix(sInix):
    """A template's .inix with a [Table:lookups] section -- written by the 13:20 build on 4 November, or kept from before
    the rename -- has it renamed [Table:defs], or dropped when [Table:defs] is already there. Answers True when changed."""
    sOld = "[table:" + "look" + "ups]"
    if not os.path.exists(sInix): return False
    raw = open(sInix, "rb").read()
    bBom = raw.startswith(b"\xef\xbb\xbf")
    sText = raw.decode("utf-8-sig")
    if sOld not in sText.lower(): return False
    bHasDefs = "[table:defs]" in sText.lower()
    lOut, bSkip = [], False
    for sLine in sText.splitlines():
        sTrim = sLine.strip().lower()
        if sTrim.startswith("[") and sTrim.endswith("]"):
            bSkip = False
            if sTrim == sOld:
                if bHasDefs: bSkip = True; continue
                sLine = "[Table:defs]"
        if not bSkip: lOut.append(sLine)
    open(sInix, "wb").write((b"\xef\xbb\xbf" if bBom else b"") + ("\r\n".join(lOut) + "\r\n").encode("utf-8"))
    logLine(os.path.basename(sInix) + ": its " + sOld + " section " + ("dropped, since [Table:defs] is there" if bHasDefs else "renamed [Table:defs]"))
    return True


def main():
    bDry = "--dry" in [s.lower() for s in sys.argv[1:]]
    # --rename-only (4 November 2026): the build runs this first, before any step that opens a template -- RadioTrail's
    # fetchStations --conventions expects defs, and failed at 13:20 because the rename came later.
    if "--rename-only" in [s.lower() for s in sys.argv[1:]]:
        logLine("describeFields --rename-only: " + os.path.abspath(__file__))
        iRenamed = 0
        for sDb in sorted(glob.glob(os.path.join(sProject, "templates", "*", "*.db"))):
            sTemplate = os.path.basename(os.path.dirname(sDb))
            try:
                if renameToDefs(sDb, sTemplate, bDry): iRenamed += 1
                repairInix(os.path.join(os.path.dirname(sDb), os.path.splitext(os.path.basename(sDb))[0] + ".inix"))
            except Exception as exceptionError:
                logLine("ERROR " + sTemplate + ": " + str(exceptionError)); print("The defs rename failed for " + sTemplate + "; the log says why."); return 1
        # THE SHARED DATABASE TOO (13:32 build, 4 November): data\lookups.db, DbDo's global lists, is data\defs.db now;
        # the installer and RepoFiles already name it so, and the installer build stopped because the file was not there.
        sOldShared = os.path.join(sProject, "data", "look" + "ups.db")
        sShared = os.path.join(sProject, "data", "defs.db")
        if os.path.exists(sOldShared) and not os.path.exists(sShared) and not bDry:
            os.replace(sOldShared, sShared)
            logLine("data: renamed " + os.path.basename(sOldShared) + " defs.db")
        if os.path.exists(sShared):
            if renameToDefs(sShared, "data\\defs.db", bDry): iRenamed += 1
        print("defs: %d database%s renamed from the old name." % (iRenamed, "" if iRenamed == 1 else "s"))
        return 0
    logLine("describeFields started: " + os.path.abspath(__file__))
    logLine("Python " + sys.version.replace("\n", " ") + "; " + platform.platform() + "; cwd " + os.getcwd() + "; args " + " ".join(sys.argv[1:]))
    sFile = os.path.join(sProject, "templates", "fieldDescriptions.inix")
    if not os.path.isfile(sFile):
        print("No templates\\fieldDescriptions.inix, so no field is described.")
        logLine("missing " + sFile)
        return 1
    dSections = readSentences(sFile)
    logLine("%d sections, %d sentences, from %s" % (len(dSections), sum(len(d) for d in dSections.values()), sFile))
    iWritten = iUpdated = iFailed = 0
    for sDb in sorted(glob.glob(os.path.join(sProject, "templates", "*", "*.db"))):
        sTemplate = os.path.basename(os.path.dirname(sDb))
        try:
            renameToDefs(sDb, sTemplate, bDry)
            retireViews(sDb, sTemplate, bDry)
            applyLookPrime(sDb, sTemplate, dSections, bDry)
            packCarried(sDb, sTemplate, bDry)
            compactIfLoose(sDb, sTemplate, bDry)
            if not bDry: repairInix(os.path.join(os.path.dirname(sDb), os.path.splitext(os.path.basename(sDb))[0] + ".inix"))
            iW, iU, _iK = describeDatabase(sDb, sTemplate, dSections, bDry)
            iWritten += iW; iUpdated += iU
        except Exception as exceptionError:
            iFailed += 1
            logLine("ERROR " + sTemplate + ": " + str(exceptionError))
    sVerb = "would be" if bDry else "were"
    print("Field descriptions: %d %s written and %d %s updated in the templates' defs%s." % (
        iWritten, sVerb, iUpdated, sVerb, ("; %d database%s could not be written, and the log says why" % (iFailed, "" if iFailed == 1 else "s")) if iFailed else ""))
    return 1 if iFailed else 0


if __name__ == "__main__":
    sys.exit(main())
