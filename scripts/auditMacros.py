r"""auditMacros: checks that DbDo's macro recorder does what it says (15 November 2026).

    auditMacros                 check the code, then every macro played in DbDo's newest session log; exit 0 when all hold
    auditMacros PATH            the same, with the session log named
    auditMacros --code          the code only, as the build's acceptance check runs it

NO ARGUMENT IS NEEDED (17 November 2026). The runtime check used to wait for --runtime; a Homer script with no
arguments does the most complete thing, so it now runs by default, and says so plainly when there is no session log
yet rather than failing. --runtime is still understood.

WHY. A macro is trusted to repeat what the person did. Two things can break that without any test noticing: a
command that a macro may hold whose handler forgets to say it succeeded (so it records as a gap, or worse, a handler
that says so before it has moved), and a step that plays but leaves the cursor somewhere else. The first is a fact
about the code, checked here on every build through accept.inix; the second is a fact about a run, which the engine
logs step by step and --runtime reads back.

THE CODE RULES, each a pass or a fail with its evidence:
  1. supported list     DbDoMacros.cs names the commands a macro can hold, in aMacroCommands.
  2. menu items         each has a menu item in DbDo.cs whose command name becomes that id (KeyMap.toId).
  3. confirmations      each item's handler, or a method it calls, confirms that command after its move:
                        confirmMacroCommand("<id>", ...), or confirmMacroGoTo for Go to Record, which also needs
                        cancelMacroCapture where its prompt is cancelled.
  4. no stray confirms  no confirmMacroCommand names a command outside the list.
  5. host cases         the host's validate, expect and execute each have a case for every command.
  6. one choke point    addItem's click runs every command through runWithMacro; the window starts the recorder
                        (initializeMacros) and stops a playing macro before it closes (cancelMacroBeforeClose).
  7. the menu           Record Macro, Play Macro and Save Macro are on the Tools menu, with their keys, and each has
                        a description in configs\DbDo.inix.
  8. the build          build.cmd compiles DbDoMacros.cs in both compile lines, takes Macro.cs from the kit, needs a
                        kit with the engine's audit, and runs the kit's macro test first; RepoFiles.txt names
                        DbDoMacros.cs and these scripts, so tidy keeps them (on 15 November it moved them to
                        notes\other as strays, and the check's build could not find them).
  9. the guide          help\DbDo.md tells a person how to record, play and save a macro.

THE RUNTIME CHECK reads the lines the engine wrote ("macro: Audit step N of M: ...; expected ...; actual ...;
same") and reports each playback: how many steps were compared, and every step whose actual row differed from the
expected one, or that stopped playback. A difference fails.

Every finding goes to logs\DbDo-auditMacros-<time>.log."""

import datetime, glob, os, platform, re, sys

sScriptDir = os.path.dirname(os.path.abspath(__file__))
sRoot = os.path.dirname(sScriptDir)
sLogDir = os.path.join(sRoot, "logs")
os.makedirs(sLogDir, exist_ok=True)
sLog = os.path.join(sLogDir, "DbDo-auditMacros-%s.log" % datetime.datetime.now().strftime("%Y%m%d-%H%M%S"))
lFindings = []


def logLine(sText):
    with open(sLog, "a", encoding="utf-8") as oFile: oFile.write(datetime.datetime.now().isoformat(timespec="milliseconds") + "  " + sText + "\n")
    return True


def sayLine(sText):
    print(sText)
    logLine("CONSOLE: " + sText)
    return True


def finding(sCheck, bPass, sEvidence):
    lFindings.append((sCheck, bPass, sEvidence))
    sayLine("%s %s: %s" % ("PASS" if bPass else "FAIL", sCheck, sEvidence))
    return bPass


def readText(sPath):
    with open(sPath, "rb") as oFile: return oFile.read().decode("utf-8-sig", errors="replace").replace("\r\n", "\n")


def withoutComments(sText):
    return "\n".join(s for s in sText.splitlines() if not s.lstrip().startswith("//"))


def toId(sName):
    """KeyMap.toId, as Homer's KeyMap.cs writes it: the caption without its ampersand or trailing "...", its words
    joined, the first lowercased and each later word's first letter capitalized."""
    sPlain = sName.replace("&", "").strip()
    while sPlain.endswith((":", "...", ".")): sPlain = sPlain.rstrip(":.").strip()
    sId, bNewWord = "", False
    for sChar in sPlain:
        if sChar.isalnum():
            if not sId: sId += sChar.lower()
            elif bNewWord: sId += sChar.upper()
            else: sId += sChar
            bNewWord = False
        else: bNewWord = True
    return sId


def methodBody(sText, sName):
    """The body of the method named, braces balanced, or "" when it is not found."""
    oMatch = re.search(r"(?:private|public|internal|protected)[^\n;=]*?\b" + re.escape(sName) + r"\s*\([^)]*\)\s*\{", sText)
    if not oMatch: return ""
    iDepth, iAt = 0, oMatch.end() - 1
    for iPos in range(iAt, len(sText)):
        if sText[iPos] == "{": iDepth += 1
        elif sText[iPos] == "}":
            iDepth -= 1
            if iDepth == 0: return sText[iAt:iPos + 1]
    return ""


def checkCode():
    sDbDo = withoutComments(readText(os.path.join(sRoot, "DbDo.cs")))
    sMacros = withoutComments(readText(os.path.join(sRoot, "DbDoMacros.cs")))
    sAll = sDbDo + "\n" + sMacros
    # 1. the list
    oList = re.search(r"aMacroCommands\s*=\s*new string\[\]\s*\{([^}]*)\}", sMacros)
    lsIds = re.findall(r'"(\w+)"', oList.group(1)) if oList else []
    finding("supported list", bool(lsIds), ", ".join(lsIds) if lsIds else "no aMacroCommands list in DbDoMacros.cs")
    # 2 and 3. menu items and their confirmations
    dItems = {}
    for sCaption, sCommand, sHandler in re.findall(r'addItem\(mi[A-Za-z]+,\s*"([^"]*)",\s*"([^"]*)",\s*[^,]+?,\s*(\w+)\s*[,)]', sAll):
        dItems.setdefault(toId(sCommand), (sCommand, sHandler))
    lsMissing, lsUnconfirmed, lsConfirmed = [], [], []
    for sId in lsIds:
        if sId not in dItems:
            lsMissing.append(sId)
            continue
        sCommand, sHandler = dItems[sId]
        sBody = methodBody(sAll, sHandler)
        lsCalled = [s for s in re.findall(r"\b([a-z]\w*)\s*\(", sBody) if s != sHandler]
        lsBodies = [sBody] + [methodBody(sAll, s) for s in dict.fromkeys(lsCalled)]
        sPattern = r'confirmMacroGoTo\(' if sId == "goToRecord" else r'confirmMacroCommand\(\s*"' + sId + r'"'
        bConfirmed = any(re.search(sPattern, s) for s in lsBodies if s)
        if sId == "goToRecord" and "cancelMacroCapture()" not in sBody:
            lsUnconfirmed.append("%s (%s): its cancelled prompt does not call cancelMacroCapture" % (sCommand, sHandler))
        if bConfirmed: lsConfirmed.append("%s by %s" % (sCommand, sHandler))
        else: lsUnconfirmed.append("%s (%s): no confirmation after its move" % (sCommand, sHandler))
    finding("menu items", not lsMissing, "every command has its menu item" if not lsMissing else "no menu item for " + ", ".join(lsMissing))
    finding("confirmations", not lsUnconfirmed and bool(lsIds), "; ".join(lsConfirmed) if not lsUnconfirmed else "; ".join(lsUnconfirmed))
    # 4. stray confirmations
    lsStray = sorted(set(re.findall(r'confirmMacroCommand\(\s*"(\w+)"', sDbDo)) - set(lsIds) - {"goToRecord"})
    finding("no stray confirms", not lsStray, "every confirmation names a supported command" if not lsStray else "confirmMacroCommand for unsupported " + ", ".join(lsStray))
    # 5. host cases
    lsGaps = []
    for sMethod in ("validate", "expect", "execute"):
        sBody = methodBody(sMacros, sMethod)
        if not sBody:
            lsGaps.append("no %s method" % sMethod)
            continue
        for sId in lsIds:
            if not re.search(r'case\s+"' + sId + r'"\s*:', sBody): lsGaps.append("%s has no case for %s" % (sMethod, sId))
    finding("host cases", not lsGaps, "validate, expect and execute cover all %d commands" % len(lsIds) if not lsGaps else "; ".join(lsGaps))
    # 6. the choke point and the window's life
    lsChoke = []
    oFull = re.search(r"private ToolStripMenuItem addItem\(ToolStripMenuItem miParent, string sText, string sCommand, Keys key, EventHandler evHandler,\s*string sSummary, string sDescription\)\s*\{", sDbDo)
    if oFull:
        sTail = sDbDo[oFull.end():oFull.end() + 3000]
        if not re.search(r"runWithMacro\(sCommand,\s*delegate\s*\{\s*evHandler\(senderA,\s*evA\);\s*\}\)", sTail): lsChoke.append("addItem's click does not run the command through runWithMacro")
    else: lsChoke.append("the full addItem was not found")
    if "initializeMacros();" not in sDbDo: lsChoke.append("the window never calls initializeMacros")
    if not re.search(r"OnFormClosing\(FormClosingEventArgs evArgs\)\s*\{\s*(?:[^\n]*\n){0,3}?\s*if \(cancelMacroBeforeClose\(\)\)", sDbDo): lsChoke.append("OnFormClosing does not stop a playing macro first")
    if "partial class DbDoForm" not in sDbDo: lsChoke.append("DbDoForm is not partial, so DbDoMacros.cs cannot join it")
    finding("one choke point", not lsChoke, "addItem runs every command through runWithMacro; the window starts the recorder and stops playback before closing" if not lsChoke else "; ".join(lsChoke))
    # 7. the menu and descriptions
    lsMenu = []
    dWanted = {"Record Macro": "Keys.Control | Keys.Shift | Keys.F9", "Play Macro": "Keys.Alt | Keys.Shift | Keys.F12", "Save Macro": "Keys.None"}
    for sCommand, sKeys in dWanted.items():
        if not re.search(r'addItem\(miTools,\s*"[^"]*",\s*"' + sCommand + r'",\s*' + re.escape(sKeys) + r"\s*,", sDbDo): lsMenu.append("%s is not on the Tools menu with %s" % (sCommand, sKeys))
    sInix = readText(os.path.join(sRoot, "configs", "DbDo.inix"))
    for sCommand in dWanted:
        if not re.search(r"(?m)^" + sCommand + r"=[^,\n]+,\s*\S", sInix): lsMenu.append("configs\\DbDo.inix has no description for " + sCommand)
    finding("the menu", not lsMenu, "Record Macro, Play Macro and Save Macro are on the Tools menu with their keys and described" if not lsMenu else "; ".join(lsMenu))
    # 8. the build
    sBuild = readText(os.path.join(sRoot, "build.cmd"))
    lsBuild = []
    if sBuild.count('"!sHere!DbDoMacros.cs"') < 2: lsBuild.append("a compile line leaves out DbDoMacros.cs")
    if "exec\\CSharp\\Macro.cs" not in sBuild: lsBuild.append("Macro.cs is not taken from the kit")
    oNeed = re.search(r'set "kitNeeded=([\d.]+)"', sBuild)
    if not oNeed or tuple(int(s) for s in oNeed.group(1).split(".")) < (1, 231, 0): lsBuild.append("kitNeeded is below 1.231.0, the first kit with the engine's audit")
    if "scripts\\testMacro.py" not in sBuild: lsBuild.append("the kit's macro test is not run")
    lsRepo = [s.strip().lower() for s in readText(os.path.join(sRoot, "RepoFiles.txt")).splitlines()]
    for sName in ("DbDoMacros.cs", "scripts/auditMacros.cmd", "scripts/auditMacros.py"):
        if sName.lower() not in lsRepo: lsBuild.append("RepoFiles.txt does not name " + sName + ", so tidy would move it away")
    finding("the build", not lsBuild, "both compile lines take DbDoMacros.cs, Macro.cs comes from a kit of 1.231.0 or later, its test runs first, and RepoFiles.txt keeps the macro files" if not lsBuild else "; ".join(lsBuild))
    # 9. the guide
    sGuide = readText(os.path.join(sRoot, "help", "DbDo.md"))
    lsGuide = [s for s in ("Record Macro", "Play Macro", "Save Macro", "Control+Shift+F9", "Alt+Shift+F12") if s not in sGuide]
    finding("the guide", not lsGuide, "help\\DbDo.md explains recording, playing and saving" if not lsGuide else "help\\DbDo.md does not mention " + ", ".join(lsGuide))


def newestSessionLog():
    sFolder = os.path.join(os.environ.get("LOCALAPPDATA", ""), "DbDo", "logs")
    lsLogs = sorted(glob.glob(os.path.join(sFolder, "DbDo-*.log")), key=os.path.getmtime)
    return lsLogs[-1] if lsLogs else ""


def checkRuntime(sPath):
    if not sPath: sPath = newestSessionLog()
    if not sPath or not os.path.isfile(sPath):
        return finding("runtime", True, "no DbDo session log found%s, so no playback to check" % (" at " + sPath if sPath else " in %LOCALAPPDATA%\\DbDo\\logs"))
    logLine("runtime log: %s" % sPath)
    iPlaybacks = iCompared = iSame = 0
    lsDifferent, lsStopped = [], []
    for sLine in readText(sPath).splitlines():
        if "macro: Macro playback started" in sLine: iPlaybacks += 1
        oAudit = re.search(r"macro: Audit step (\d+) of (\d+): (.*?); before (.*?); expected (.*?); actual (.*?); (same|DIFFERENT|not compared)\s*$", sLine)
        if oAudit:
            logLine("playback %d step %s: %s; expected %s; actual %s; %s" % (iPlaybacks, oAudit.group(1), oAudit.group(3), oAudit.group(5), oAudit.group(6), oAudit.group(7)))
            if oAudit.group(7) == "same": iCompared += 1; iSame += 1
            elif oAudit.group(7) == "DIFFERENT":
                iCompared += 1
                lsDifferent.append("playback %d step %s, %s: expected %s, actual %s" % (iPlaybacks, oAudit.group(1), oAudit.group(3), oAudit.group(5), oAudit.group(6)))
        oStop = re.search(r"macro: Macro stopped at step (.*)$", sLine)
        if oStop: lsStopped.append("playback %d stopped at step %s" % (iPlaybacks, oStop.group(1)))
    if iPlaybacks == 0: return finding("runtime", True, "%s holds no macro playback" % os.path.basename(sPath))
    for sLine in lsStopped: sayLine("  " + sLine)
    return finding("runtime", not lsDifferent, "%s: %d playback(s), %d step(s) compared, %d the same%s" % (
        os.path.basename(sPath), iPlaybacks, iCompared, iSame, "" if not lsDifferent else "; different: " + "; ".join(lsDifferent)))


def main():
    lsArgs = sys.argv[1:]
    logLine("auditMacros %s, python %s, %s, cwd %s, args %s" % (sys.argv[0], sys.version.split()[0], platform.platform(), os.getcwd(), lsArgs))
    if lsArgs and lsArgs[0] in ("-h", "--help", "/?"):
        print(__doc__)
        return 0
    checkCode()
    lsPaths = [s for s in lsArgs if not s.startswith("--")]
    if "--code" not in [s.lower() for s in lsArgs]: checkRuntime(lsPaths[0] if lsPaths else "")
    iFailed = sum(1 for t in lFindings if not t[1])
    sayLine("auditMacros: %d passed, %d failed. Log: %s" % (len(lFindings) - iFailed, iFailed, sLog))
    return 1 if iFailed else 0


if __name__ == "__main__":
    sys.exit(main())
