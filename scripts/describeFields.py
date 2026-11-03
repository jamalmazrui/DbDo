r"""describeFields.py -- write a one-sentence description of every field into each template's lookups table.

WHY (3 November 2026). Shift+F1 on a field in DbDo's record, new-record and filter forms says what the field is
for. The sentences live in templates\fieldDescriptions.inix, one per field, and this step, run by build.cmd after
the template check, writes them into the lookups table of every template database, as rows with tbl and fld filled,
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
        lLookupCols = [r[1] for r in oConn.execute("PRAGMA table_xinfo(lookups)")]
        if "descrip" not in lLookupCols:
            logLine(sTemplate + ": no lookups table with a descrip column; nothing written")
            return (0, 0, 0)
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
        for sTable, sField, sSentence in lWanted:
            oHave = oConn.execute("SELECT lookup_id, coalesce(descrip, ''), coalesce(src, '') FROM lookups WHERE tbl = ? AND fld = ? AND coalesce(val, '') = '' ORDER BY lookup_id LIMIT 1", (sTable, sField)).fetchone()
            if oHave is None:
                iWritten += 1
                if not bDry:
                    oConn.execute("INSERT INTO lookups (tbl, fld, val, src, descrip) VALUES (?, ?, NULL, ?, ?)", (sTable, sField, sSource, sSentence))
            elif oHave[1] == "" or (oHave[2] == sSource and oHave[1] != sSentence):
                iUpdated += 1
                if not bDry:
                    oConn.execute("UPDATE lookups SET descrip = ?, src = ?, edited = CURRENT_TIMESTAMP WHERE lookup_id = ?", (sSentence, sSource, oHave[0]))
            else:
                iKept += 1
        if not bDry: oConn.commit()
    finally:
        oConn.close()
    logLine("%s: %d written, %d updated, %d kept as they were" % (sTemplate, iWritten, iUpdated, iKept))
    return (iWritten, iUpdated, iKept)


def main():
    bDry = "--dry" in [s.lower() for s in sys.argv[1:]]
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
            iW, iU, _iK = describeDatabase(sDb, sTemplate, dSections, bDry)
            iWritten += iW; iUpdated += iU
        except Exception as exceptionError:
            iFailed += 1
            logLine("ERROR " + sTemplate + ": " + str(exceptionError))
    sVerb = "would be" if bDry else "were"
    print("Field descriptions: %d %s written and %d %s updated in the templates' lookups%s." % (
        iWritten, sVerb, iUpdated, sVerb, ("; %d database%s could not be written, and the log says why" % (iFailed, "" if iFailed == 1 else "s")) if iFailed else ""))
    return 1 if iFailed else 0


if __name__ == "__main__":
    sys.exit(main())
