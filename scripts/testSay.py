r"""testSay.py -- every Shift+letter key in DbDo, pressed once and twice, then described, with what JAWS said.

WHY THIS EXISTS (9 November 2026)

DbDo's Shift+letter keys are its Say family: each speaks one fact about the record, and a second press may spell a
short value or show a longer one in a window. Jamal hears them through JAWS, and what JAWS says can differ from what
DbDo meant to say. This script presses each key in the real program, records the computer's sound while JAWS speaks,
writes it down with a local speech recognizer, and gathers everything into one folder under logs to upload:

    1. Shift+A through Shift+Z, each pressed once, then pressed twice in quick succession, then Escape, which closes
       any window a second press opened.
    2. Control+F1 turns on the Key Describer, Shift+A through Shift+Z are each pressed once, so each is described
       rather than run, and Control+F1 turns the Key Describer off.

It does the pressing, recording and transcribing through the kit's recordWalk, the tool the tutorials are checked
with, by writing a walk for it. recordWalk sets beside each key what JAWS was heard to say and what DbDo's own speech
log says it said.

BEFORE RUNNING. Open DbDo on a table with records, on a record worth hearing about, with JAWS running as usual.
Then keep your hands off the keyboard: the script presses keys in DbDo. To stop it, switch to any other window;
recordWalk checks before each key that DbDo is still in front, and stops if not.

USAGE

    testSay                     the whole run, about 15 minutes
    testSay --letters ABC       only those letters
    testSay --no-describer      leave out the Key Describer pass
    testSay --wait 8            listen up to eight seconds after each key (default 6)
    testSay --model base.en     a smaller, faster recognizer, passed on to recordWalk (default small.en)

WHAT IT WRITES

    logs\DbDo-testSay-<stamp>.log       this script's own log: environment, settings, every command and exit code
    logs\testSay-<stamp>\               the folder to upload:
        testSay.inix                    the walk recordWalk performed
        DbDo-recordWalk-<stamp>.md      recordWalk's report: key by key, heard and logged speech side by side
        DbDo-recordWalk-<stamp>.log     recordWalk's log
        recordings\                     one recording per key
        appLogs\                        DbDo's own session logs from the run, with every say: line
        testSay.md                      what is in the folder

Exit code: recordWalk's, or 2 when recordWalk could not be found or the run could not start.
"""

import argparse, datetime, glob, os, platform, shutil, subprocess, sys, time, traceback

sStamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
sScriptDir = os.path.dirname(os.path.abspath(__file__))
sProject = os.path.dirname(sScriptDir)
sApp = os.path.basename(sProject) or "DbDo"
sLogDir = os.path.join(sProject, "logs")
sLogPath = os.path.join(sLogDir, sApp + "-testSay-" + sStamp + ".log")
sOutDir = os.path.join(sLogDir, "testSay-" + sStamp)


def logLine(sText):
    os.makedirs(sLogDir, exist_ok=True)
    with open(sLogPath, "a", encoding="utf-8-sig", newline="\r\n") as fLog:
        fLog.write(datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S") + " " + sText + "\n")


def say(sText):
    print(sText, flush=True)
    logLine("CONSOLE: " + sText)


def findRecordWalk():
    """The app's own copy of recordWalk.py, else the kit's, beside the project or at the root of its drive."""
    lCandidates = [os.path.join(sScriptDir, "recordWalk.py"),
                   os.path.join(os.path.dirname(sProject), "HomerDev", "scripts", "recordWalk.py"),
                   os.path.join(os.path.splitdrive(sProject)[0] + os.sep, "HomerDev", "scripts", "recordWalk.py")]
    for sCandidate in lCandidates:
        logLine("recordWalk candidate " + sCandidate + (" found" if os.path.isfile(sCandidate) else " not found"))
        if os.path.isfile(sCandidate): return sCandidate
    return ""


def writeWalk(sPath, sLetters, iWait, bDescriber):
    """The walk: each letter once, twice and Escape; then, unless left out, the Key Describer pass."""
    lLines = ["; testSay.inix -- written by testSay.py on " + sStamp + " for recordWalk", ""]
    def step(sKey, iPress, iStepWait, sSay):
        lLines.extend(["[step]", "Say=" + sSay, "Key=" + sKey, "Wait=" + str(iStepWait)])
        if iPress > 1: lLines.append("Press=" + str(iPress))
        lLines.append("")
    for sLetter in sLetters:
        step("Shift+" + sLetter, 1, iWait, "Shift+" + sLetter + ", pressed once.")
        step("Shift+" + sLetter, 2, iWait + 2, "Shift+" + sLetter + ", pressed twice.")
        step("Escape", 1, 2, "Escape, closing any window the second press opened.")
    if bDescriber:
        step("Control+F1", 1, iWait, "Control+F1, the Key Describer on.")
        for sLetter in sLetters:
            step("Shift+" + sLetter, 1, iWait, "Shift+" + sLetter + ", described.")
        step("Control+F1", 1, iWait, "Control+F1, the Key Describer off.")
    with open(sPath, "w", encoding="utf-8-sig", newline="\r\n") as fWalk:
        fWalk.write("\n".join(lLines))
    logLine("walk written: %s, %d steps" % (sPath, sum(1 for s in lLines if s == "[step]")))


def newerThan(sPattern, nSince):
    return sorted(s for s in glob.glob(sPattern) if os.path.getmtime(s) >= nSince)


def copyInto(lFiles, sFolder):
    os.makedirs(sFolder, exist_ok=True)
    for sFile in lFiles:
        shutil.copy2(sFile, sFolder)
        logLine("copied " + sFile + " into " + sFolder)
    return len(lFiles)


def main():
    oParser = argparse.ArgumentParser(description="Press DbDo's Shift+letter keys, record what JAWS says, and gather it under logs.")
    oParser.add_argument("--letters", default="ABCDEFGHIJKLMNOPQRSTUVWXYZ")
    oParser.add_argument("--no-describer", action="store_true")
    oParser.add_argument("--wait", type=int, default=6)
    oParser.add_argument("--model", default="")
    oArgs = oParser.parse_args()
    sLetters = "".join(s for s in oArgs.letters.upper() if "A" <= s <= "Z")
    logLine("testSay started; script=%s; Python %s; %s; cwd=%s; command line=%s" % (
        os.path.abspath(__file__), sys.version.split()[0], platform.platform(), os.getcwd(), " ".join(sys.argv)))
    logLine("settings: app=%s; project=%s; letters=%s; wait=%d; describer=%s; model=%s; folder=%s" % (
        sApp, sProject, sLetters, oArgs.wait, "no" if oArgs.no_describer else "yes", oArgs.model or "recordWalk's default", sOutDir))
    if not sLetters:
        say("No letters to press. Nothing was done.")
        return 2
    sRecordWalk = findRecordWalk()
    if not sRecordWalk:
        say("recordWalk.py was not found in this project's scripts or the kit's, so nothing was pressed.")
        return 2
    os.makedirs(sOutDir, exist_ok=True)
    sWalk = os.path.join(sOutDir, "testSay.inix")
    writeWalk(sWalk, sLetters, oArgs.wait, not oArgs.no_describer)
    iSteps = len(sLetters) * 3 + (0 if oArgs.no_describer else len(sLetters) + 2)
    say("testSay: %d keys in %s, about %d minutes. Keep your hands off the keyboard; switch windows to stop." % (
        iSteps, sApp, max(1, iSteps * (oArgs.wait + 3) // 60)))
    lCommand = [sys.executable, sRecordWalk, "--walk", sWalk, "--app", sApp, "--no-axe", "--keep", "--wait", str(oArgs.wait)]
    if oArgs.model: lCommand += ["--model", oArgs.model]
    nStart = time.time()
    logLine("run: " + subprocess.list2cmdline(lCommand) + " in " + sProject)
    try:
        iCode = subprocess.call(lCommand, cwd=sProject)
    except Exception:
        logLine("recordWalk did not start: " + traceback.format_exc())
        say("recordWalk could not be started; the log says why.")
        return 2
    logLine("recordWalk exit code %d after %.0f seconds" % (iCode, time.time() - nStart))
    # GATHERED IN ONE FOLDER, to upload with logs: recordWalk's report and log, its recordings, DbDo's own logs.
    iReports = copyInto(newerThan(os.path.join(sLogDir, sApp + "-recordWalk-*.*"), nStart), sOutDir)
    lWaves = newerThan(os.path.join(sProject, "results", "recordWalk-*"), nStart)
    iWaves = 0
    for sFolder in lWaves:
        if os.path.isdir(sFolder): iWaves += copyInto(sorted(glob.glob(os.path.join(sFolder, "*.wav"))), os.path.join(sOutDir, "recordings"))
    sAppLogs = os.path.join(os.environ.get("LOCALAPPDATA", os.path.expanduser("~")), sApp, "logs")
    iAppLogs = copyInto(newerThan(os.path.join(sAppLogs, sApp + "-*.log"), nStart - 60), os.path.join(sOutDir, "appLogs"))
    with open(os.path.join(sOutDir, "testSay.md"), "w", encoding="utf-8-sig", newline="\r\n") as fOut:
        fOut.write("\n".join([
            "# testSay, " + sStamp, "",
            "- Letters: " + sLetters + "; each pressed once, twice, then Escape" + ("" if oArgs.no_describer else "; then each described with the Key Describer on") + ".",
            "- recordWalk exit code: %d (0 means every step was heard as its walk expected; this walk expects nothing, so 1 is usual)." % iCode,
            "- recordWalk report and log: %d file%s." % (iReports, "" if iReports == 1 else "s"),
            "- Recordings: %d, in recordings." % iWaves,
            "- DbDo's own logs: %d, in appLogs." % iAppLogs,
            "- This script's log: " + os.path.basename(sLogPath) + ", in logs.", ""]))
    say("testSay finished: %d recording%s and %d report file%s in %s. Upload the logs folder." % (
        iWaves, "" if iWaves == 1 else "s", iReports, "" if iReports == 1 else "s", sOutDir))
    return iCode


if __name__ == "__main__":
    try:
        sys.exit(main())
    except SystemExit:
        raise
    except Exception:
        logLine("ERROR " + traceback.format_exc())
        print("testSay stopped on an error; the log says why: " + sLogPath)
        sys.exit(2)
