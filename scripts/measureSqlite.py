"""measureSqlite.py -- times DbDo's SQLite commands on a copy of RadioTrail through DbDo's own connection, and records
which index each query uses (SQLite plan step 1, 15 November 2026).

WHY. Claude and ChatGPT agreed (DbDo_SQLite_Review Claude3 and ChatGPT3) that no speed change goes into DbDo until it
is measured on Windows through the same SQLite ODBC driver DbDo uses. A Python measurement would time Python's own
SQLite, which may be another version with other features. So this script only prepares and reports: DbDo.exe
--measure-sqlite does the measuring, read-only, with the very properties and functions the window uses.

WHAT IT DOES.
  1. Finds DbDo.exe: exec\\DbDo.exe in this project, else DbDo.exe beside it.
  2. Copies templates\\RadioTrail\\RadioTrail.db to work\\measureSqlite\\RadioTrail.db with SQLite's backup API, so
     the copy is consistent even if the template is open, and the template itself is only read.
  3. Runs DbDo three times on the copy: sorted by votes twice (the first run cold, the second with the file cached),
     then sorted by name. Each run also filters on Germany and builds the country pick list.
  4. Writes every run's report into the log, and prints a short summary: the driver probe, each time, and which
     queries use no index.

USAGE
  scripts\\measureSqlite                      measure RadioTrail with the defaults above
  scripts\\measureSqlite <database> <table>   measure another database's table (a copy is made the same way)

Every run writes logs\\DbDo-measureSqlite-<date>-<time>.log. Send that log back so step 3 can be decided.
"""

import datetime, os, platform, sqlite3, subprocess, sys, traceback

sFilter = "country = 'Germany'"
sLookup = "country"
sLogPath = ""


def log(sText):
    """Appends a time-stamped line to the log."""
    with open(sLogPath, "a", encoding="utf-8") as oFile:
        oFile.write(datetime.datetime.now().isoformat(timespec="milliseconds") + "  " + sText + "\n")
    return True


def say(sText):
    """Prints a line for the listener and logs it."""
    print(sText)
    return log(sText)


def findExe(sProject):
    """DbDo.exe from exec, else beside the project, else ''."""
    for sPath in [os.path.join(sProject, "exec", "DbDo.exe"), os.path.join(sProject, "DbDo.exe")]:
        if os.path.isfile(sPath): return sPath
    return ""


def copyDatabase(sSource, sTarget):
    """A consistent copy through SQLite's backup API; the source is opened read-only."""
    os.makedirs(os.path.dirname(sTarget), exist_ok=True)
    for sSuffix in ["", "-journal", "-wal", "-shm"]:
        if os.path.isfile(sTarget + sSuffix): os.remove(sTarget + sSuffix)
    oSource = sqlite3.connect("file:" + sSource.replace("\\", "/") + "?mode=ro", uri=True)
    oTarget = sqlite3.connect(sTarget)
    try: oSource.backup(oTarget)
    finally:
        oTarget.close()
        oSource.close()
    return os.path.getsize(sTarget)


def runDbDo(sExe, sReport, sDb, sTable, sSort):
    """Runs one measurement and returns (exit code, report lines)."""
    lCommand = [sExe, "--measure-sqlite", sReport, sDb, sTable, sSort, sFilter, sLookup]
    log("run: " + subprocess.list2cmdline(lCommand))
    oDone = subprocess.run(lCommand, capture_output=True, text=True, timeout=900)
    log("exit code %d" % oDone.returncode)
    if oDone.stderr.strip(): log("stderr: " + oDone.stderr.strip())
    lLines = open(sReport, encoding="utf-8-sig", errors="replace").read().splitlines() if os.path.isfile(sReport) else oDone.stdout.splitlines()
    for sLine in lLines: log("  " + sLine)
    return oDone.returncode, lLines


def main():
    global sLogPath
    sProject = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    os.makedirs(os.path.join(sProject, "logs"), exist_ok=True)
    sLogPath = os.path.join(sProject, "logs", "DbDo-measureSqlite-" + datetime.datetime.now().strftime("%Y%m%d-%H%M%S") + ".log")
    try:
        log("measureSqlite %s, python %s, %s, cwd %s, args %s" % (sys.argv[0], sys.version.split()[0], platform.platform(), os.getcwd(), sys.argv[1:]))
        sSource = sys.argv[1] if len(sys.argv) > 1 else os.path.join(sProject, "templates", "RadioTrail", "RadioTrail.db")
        sTable = sys.argv[2] if len(sys.argv) > 2 else "stations"
        sExe = findExe(sProject)
        if not sExe:
            say("measureSqlite: DbDo.exe was not found in exec. Build DbDo first. Log: " + sLogPath)
            return 2
        if not os.path.isfile(sSource):
            say("measureSqlite: %s was not found. Log: %s" % (sSource, sLogPath))
            return 2
        sWork = os.path.join(sProject, "work", "measureSqlite")
        sCopy = os.path.join(sWork, os.path.basename(sSource))
        iBytes = copyDatabase(sSource, sCopy)
        say("Copied %s to %s, %.1f MB." % (sSource, sCopy, iBytes / 1048576.0))
        lRuns = [("votes", "cold"), ("votes", "warm"), ("name", "warm")]
        iResult = 0
        for iRun, (sSort, sState) in enumerate(lRuns, 1):
            sReport = os.path.join(sWork, "report%d.txt" % iRun)
            iCode, lLines = runDbDo(sExe, sReport, sCopy, sTable, sSort)
            if iCode != 0: iResult = 1
            say("Run %d, sorted by %s, file %s: %s" % (iRun, sSort, sState, "measured" if iCode == 0 else "FAILED, exit code %d" % iCode))
            for sLine in lLines:
                if sLine.startswith("TIME ") or sLine.startswith("ERROR "): say("  " + sLine)
                if iRun == 1 and sLine.startswith("SQLite driver probe"): say("  " + sLine.split("; compile options")[0])
                if iRun == 1 and sLine.startswith("PLAN ") and "USING" not in sLine.upper() and "SEARCH" not in sLine.upper(): say("  No index: " + sLine[5:])
        for sSuffix in ["-wal", "-shm", "-journal"]:
            if os.path.isfile(sCopy + sSuffix): say("Note: " + os.path.basename(sCopy + sSuffix) + " was left beside the copy.")
        say("measureSqlite finished. Please send the log: " + sLogPath)
        return iResult
    except Exception as oError:
        log(traceback.format_exc())
        print("measureSqlite stopped: %s. Log: %s" % (oError, sLogPath))
        return 2


if __name__ == "__main__":
    sys.exit(main())
