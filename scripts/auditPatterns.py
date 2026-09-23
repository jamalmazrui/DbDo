#!/usr/bin/env python3
r"""auditPatterns.py -- check DbDo against the patterns it is supposed to follow.

WHY THIS EXISTS

Four times in one week a pattern was broken, fixed, and broken again somewhere
else: a checkbox label with a placeholder in it, a [Dirs] block inserted into
[Setup], a Say command answering in a shape of its own, a modal dialog bound to
the say layer. Each was found by a person running the program and reading the
speech history. That is the most expensive way to find any of them.

So the rules are written as checks. checkHomerApp runs this through accept.inix,
and a broken pattern fails a build rather than a user's afternoon.

WHAT IT CHECKS

  installer labels   every Description: in the .iss -- no placeholder phrases,
                     no double parenthetical, no label that says nothing
  duplicates         no class defines the same member twice, which C# reports
                     as CS0111 only once the build reaches Jamal's machine
  say layer          every Keys.Shift | Keys.<letter> in the source, wherever it
                     is bound, must reach a Say command. A menu shortcut, a
                     local alias and a key handler are three different bindings
                     and an audit that reads one of them is not an audit
  hotkey doc         DbDo_hotkeys.inix claims no Shift letter that is not a Say
                     command -- the source-only check cannot see an .inix
  say answers        every Say command speaks "<name>: <value>", with the two
                     documented exceptions: Say Cell and Say Id use the column
                     name, and Say Mark answers Marked or Unmarked
  tutorials          every tutorial script has a title, three or more steps,
                     and something spoken in each
  section edits      a line that opens a section heading contains nothing else.
                     A comment mentioning [Files] is fine; a heading with a
                     sentence trailing off it is what a substring-anchored
                     insertion leaves behind

Exit code 0 when nothing failed, 1 when something did. Writes auditPatterns.log
beside this script.
"""

import datetime
import os
import re
import sys

c_lsPlaceholders = [
    "the installed version",
    "do not check",
    "tbd",
    "todo",
    "xxx",
]

# Say Mark answers "Marked" or "Unmarked": a two-state answer whose value
# already names the setting. Say Cell and Say Id label with the column name.
c_lsLabelExempt = ["saySayMark", "saySayCell", "saySayId"]

sScriptDir = os.path.dirname(os.path.abspath(__file__))
sRoot = os.path.dirname(sScriptDir)
# logs\ at the top of the project, one file per run, named as the program names
# its own logs, so an alphabetical sort is a chronological one.
import datetime as _dt
sLogDir = os.path.join(os.path.dirname(sScriptDir), "logs")
os.makedirs(sLogDir, exist_ok=True)
sLogPath = os.path.join(sLogDir, "DbDo-audit-%s.log" % _dt.datetime.now().strftime("%Y%m%d-%H%M%S"))
oLog = None
lsFindings = []


def logLine(sText):
    if oLog is not None:
        oLog.write(sText + "\n")
        oLog.flush()
    return True


def finding(sCheck, sVerdict, sEvidence):
    lsFindings.append((sCheck, sVerdict, sEvidence))
    logLine("%-16s %-5s %s" % (sCheck, sVerdict.upper(), sEvidence))
    return True


def readText(sPath):
    try:
        return open(sPath, "rb").read().decode("utf-8-sig", errors="replace").replace("\r\n", "\n")
    except Exception:
        return ""


def countNoun(iCount, sSingular, sPlural=None):
    if sPlural is None: sPlural = sSingular + "s"
    return "%d %s" % (iCount, sSingular if iCount == 1 else sPlural)


# --- 1. installer labels ----------------------------------------------------

def checkLabels():
    lsBad = []
    iChecked = 0
    for sName in sorted(os.listdir(sRoot)):
        if not sName.lower().endswith(".iss"): continue
        for sLine in readText(os.path.join(sRoot, sName)).splitlines():
            oMatch = re.search(r'Description:\s*"([^"]*)"', sLine)
            if not oMatch: continue
            sLabel = oMatch.group(1)
            iChecked += 1
            if sLabel.startswith("{code:"): continue
            for sBad in c_lsPlaceholders:
                if sBad in sLabel.lower():
                    lsBad.append("%s: %r contains %r" % (sName, sLabel, sBad))
            if sLabel.count("(") > 1:
                lsBad.append("%s: %r has more than one parenthetical" % (sName, sLabel))
    # the {code:} label functions are strings in the [Code] section
    for sName in sorted(os.listdir(sRoot)):
        if not sName.lower().endswith(".iss"): continue
        for sLine in readText(os.path.join(sRoot, sName)).splitlines():
            if "Result :=" not in sLine: continue
            for sBad in c_lsPlaceholders:
                if sBad in sLine.lower():
                    lsBad.append("%s: a label function builds %r" % (sName, sBad))
    for s in lsBad: logLine("LABEL: " + s)
    if lsBad:
        return finding("labels", "fail", "%s; each is in the log" % countNoun(len(lsBad), "label problem"))
    return finding("labels", "pass", "%s carry no placeholder and no double parenthetical" %
                   countNoun(iChecked, "label"))


# --- 2. the say layer -------------------------------------------------------

def checkDuplicateMembers():
    """No class defines the same member twice.

    C# says this as CS0111 and the build stops, which is the right outcome and
    an expensive way to hear it: the compiler runs on Jamal's machine, not mine.
    Adding isFieldNull to a class that already had one cost a build cycle, and
    the method was one screen away from the copy I wrote.

    Classes are found by their declaration line and members by the indentation
    beneath it, which is enough for a file written in one style.
    """
    sSource = readText(os.path.join(sRoot, "DbDo.cs"))
    sClass = "(file)"
    dSeen = {}
    lsBad = []
    iChecked = 0
    for sLine in sSource.splitlines():
        oClass = re.match(r"\s*(?:public|private|internal|protected|static|sealed|partial|abstract|\s)*class\s+(\w+)", sLine)
        if oClass:
            sClass = oClass.group(1)
            continue
        oMember = re.match(
            r"\s*(?:public|private|internal|protected)\s+(?:static\s+|override\s+|virtual\s+|new\s+|async\s+)*"
            r"[\w<>\[\],\.]+\s+(\w+)\s*\(([^)]*)\)\s*$", sLine)
        if not oMember: continue
        iChecked += 1
        sKey = (sClass, oMember.group(1), re.sub(r"\s+", " ", oMember.group(2)).strip())
        if sKey in dSeen:
            lsBad.append("%s.%s(%s) is defined more than once" % sKey)
        dSeen[sKey] = True
    for s in lsBad: logLine("DUPLICATE: " + s)
    if lsBad:
        return finding("duplicates", "fail", "%s; each is in the log" % countNoun(len(lsBad), "member"))
    return finding("duplicates", "pass", "%s, none defined twice in one class" %
                   countNoun(iChecked, "member"))


def checkShadowedKit():
    """Names DbDo defines itself that the kit also defines.

    The kit's classes live in namespace Homer; DbDo's own live in the global
    one. So an unqualified KeyMap in DbDo.cs is always DbDo's KeyMap, and a call
    written against the kit's API compiles against the app's -- or does not,
    which is how CS0117 arrived: KeyMap.commandForKey exists in Homer.KeyMap and
    not in DbDo's.

    This lists the collisions rather than failing on them. They are a fact about
    the port, not a fault: DbDo predates the kit. What matters is knowing which
    names are ambiguous before writing a call.
    """
    sSource = readText(os.path.join(sRoot, "DbDo.cs"))
    lsLocal = set(re.findall(r"^\s*(?:public|internal)\s+(?:static\s+|sealed\s+|partial\s+)*class\s+(\w+)",
                             sSource, re.M))
    sKit = os.environ.get("HOMERDEV", r"C:\HomerDev")
    lsKit = set()
    for sDir in [os.path.join(sKit, "CSharp"), "/home/claude/kit/CSharp"]:
        if not os.path.isdir(sDir): continue
        for sName in sorted(os.listdir(sDir)):
            if not sName.endswith(".cs"): continue
            lsKit |= set(re.findall(r"^\s*public\s+(?:static\s+|sealed\s+|partial\s+)*class\s+(\w+)",
                                    readText(os.path.join(sDir, sName)), re.M))
        break
    if not lsKit:
        return finding("kit shadow", "skip", "no kit sources found to compare against")
    lsBoth = sorted(lsLocal & lsKit)
    for s in lsBoth:
        logLine("SHADOW: %s is defined by DbDo and by the kit; unqualified uses mean DbDo's" % s)
    return finding("kit shadow", "pass",
                   "%s also defined by the kit: %s" %
                   (countNoun(len(lsBoth), "class"), ", ".join(lsBoth) if lsBoth else "none"))


def checkSayLayer():
    sSource = readText(os.path.join(sRoot, "DbDo.cs"))
    lsSay = set(re.findall(r'addItem\(miSay,[^;]*?Keys\.Shift \| Keys\.([A-Za-z0-9]+)', sSource, re.S))
    lsAll = set()
    # THE THIRD BINDING PATH. A Shift-guarded switch on a bare KeyCode --
    #     if (evArgs.Shift && !evArgs.Control && !evArgs.Alt) { switch (...) {
    #         case Keys.J: target = miRecJump; break;
    # -- binds Shift+J without the text "Keys.Shift | Keys.J" appearing
    # anywhere. It runs before the menu shortcuts, so it wins. Two audits
    # missed Shift+J because they read the other two paths only.
    for oBlock in re.finditer(
            r'evArgs\.Shift && !evArgs\.Control && !evArgs\.Alt(.{0,4000}?)\n            \}',
            sSource, re.S):
        for sKey in re.findall(r'case Keys\.([A-Za-z])\s*:', oBlock.group(1)):
            lsAll.add(sKey)
            logLine("SAY LAYER: Shift+%s is dispatched by the key handler, not by a menu shortcut" % sKey)
    for oMatch in re.finditer(r'Keys\.Shift \| Keys\.([A-Za-z])\b', sSource):
        sLine = sSource[max(0, sSource.rfind("\n", 0, oMatch.start())):oMatch.end() + 80]
        if "Alt |" in sLine or "Control |" in sLine: continue
        lsAll.add(oMatch.group(1))
    # PRINT THE MAP. Twice now I have listed the free letters by hand and got
    # them wrong -- B and V were called free while both were bound. A list a
    # person types is a list a person mistypes; this one comes from the source.
    lsLetters = sorted(s.upper() for s in lsSay if len(s) == 1)
    logLine("SAY LAYER: bound -- " + ", ".join("Shift+" + s for s in lsLetters))
    lsFree = [chr(i) for i in range(65, 91) if chr(i) not in lsLetters]
    logLine("SAY LAYER: free  -- " + ", ".join("Shift+" + s for s in lsFree))
    lsStray = sorted(lsAll - lsSay)
    for s in lsStray:
        logLine("SAY LAYER: Shift+%s is bound outside the Say menu" % s)
    if lsStray:
        return finding("say layer", "fail",
                       "Shift+%s reaches something that is not a Say command" % ", Shift+".join(lsStray))
    return finding("say layer", "pass",
                   "%s on the say layer, all of them Say commands" % countNoun(len(lsSay), "key"))


# --- 3. the answers ---------------------------------------------------------

def checkHotkeyDoc():
    """The hotkey document and the source must agree about the say layer.

    DbDo_hotkeys.inix says "Jump Record=Control+J, or Shift+J" -- a binding the
    source no longer has. A document that describes a key nobody can press is
    worse than no document, and the source-only audit could not see it, because
    it read .cs files and this is an .inix.
    """
    sDoc = os.path.join(sRoot, "DbDo_hotkeys.inix")
    if not os.path.isfile(sDoc):
        return finding("hotkey doc", "skip", "no DbDo_hotkeys.inix in this folder")
    sSource = readText(os.path.join(sRoot, "DbDo.cs"))
    lsSay = set(re.findall(r'addItem\(miSay,[^;]*?Keys\.Shift \| Keys\.([A-Za-z0-9]+)', sSource, re.S))
    lsBad = []
    iChecked = 0
    for sLine in readText(sDoc).splitlines():
        if "=" not in sLine or sLine.strip().startswith(";"): continue
        sCommand, sRest = sLine.split("=", 1)
        iChecked += 1
        for sKey in re.findall(r"(?<![+\w])Shift\+([A-Za-z])\b", sRest):
            if sKey.upper() in (s.upper() for s in lsSay): continue
            lsBad.append("%s is documented on Shift+%s, which is not a Say command"
                         % (sCommand.strip(), sKey))
    for s in lsBad: logLine("HOTKEY DOC: " + s)
    if lsBad:
        return finding("hotkey doc", "fail", "%s; each is in the log" % countNoun(len(lsBad), "line"))
    return finding("hotkey doc", "pass",
                   "%s, none claiming a Shift letter that is not a Say command" %
                   countNoun(iChecked, "documented command"))


def checkSayAnswers():
    sSource = readText(os.path.join(sRoot, "DbDo.cs"))
    lsBad = []
    iChecked = 0
    for oMatch in re.finditer(r'private void (saySay\w+)\(object sender[^\n]*\n\s*\{(.*?)\n        \}', sSource, re.S):
        sName, sBody = oMatch.group(1), oMatch.group(2)
        if sName in c_lsLabelExempt: continue
        iChecked += 1
        # Say.say and Say.sayForced speak their first argument. speakOrShow's
        # FIRST argument is the dialog title -- not speech -- so only its second
        # is checked. Reading the first was the checker's own version of the
        # mistake it exists to catch: matching a shape instead of the thing.
        lsSpoken = re.findall(r'(?:Say\.say|Say\.sayForced)\(\s*"([^"]*)"', sBody)
        lsSpoken += re.findall(r'speakOrShow\w*\(\s*"[^"]*"\s*,\s*"([^"]*)"', sBody)
        for sText in lsSpoken:
            if sText.strip() == "": continue
            if ":" in sText: continue
            if sText.startswith("No ") or sText[0].isupper():
                lsBad.append("%s speaks %r with no label" % (sName, sText))
    for s in lsBad: logLine("ANSWER: " + s)
    if lsBad:
        return finding("say answers", "fail", "%s; each is in the log" % countNoun(len(lsBad), "answer"))
    return finding("say answers", "pass",
                   "%s answer in the <name>: <value> shape" % countNoun(iChecked, "Say command"))


# --- 4. sections that landed inside comments --------------------------------

def checkTutorialKeys():
    """Every key a tutorial teaches must be a key DbDo actually binds.

    Four tutorials taught the wrong key: Control+O for Order (it is Open),
    Control+S for Select Columns (it is Save), Control+U for Unmark, and
    Control+C for Copy Record (it copies the cell). Each was written from what
    the command does rather than from the menu that defines it. A speech history
    of the File menu showed two of them at once.

    Keys any Windows program has -- Enter, Tab, arrows, Escape, Alt on its own,
    and the dialog-wide Control+Enter -- are not DbDo's to bind and are skipped,
    along with Alt+Control+D, which the desktop shortcut owns.
    """
    sSource = readText(os.path.join(sRoot, "DbDo.cs"))
    lsBound = set()
    for oMatch in re.finditer(r"(Keys\.[A-Za-z0-9]+(?:\s*\|\s*Keys\.[A-Za-z0-9]+)*)", sSource):
        lsParts = frozenset(s.strip().replace("Keys.", "").lower() for s in oMatch.group(1).split("|"))
        lsBound.add(lsParts)
    dAlias = {"control": "control", "alt": "alt", "shift": "shift", "enter": "enter",
              "pagedown": "pagedown", "pageup": "pageup", "downarrow": "down", "uparrow": "up",
              "rightarrow": "right", "leftarrow": "left", "space": "space", "backspace": "back",
              "escape": "escape", "tab": "tab", "home": "home", "end": "end", "delete": "delete"}
    lsFree = {"enter", "tab", "escape", "alt", "down", "up", "left", "right", "space", "back",
              "control+enter", "alt+tab", "alt+y", "alt+control+d", "alt+r", "alt+h"}
    lsBad = []
    iChecked = 0
    sHelp = os.path.join(sRoot, "help")
    if not os.path.isdir(sHelp):
        return finding("tutorial keys", "skip", "no help folder")
    for sName in sorted(os.listdir(sHelp)):
        if not (sName.startswith("Tutorial") and sName.endswith(".inix")): continue
        for sLine in readText(os.path.join(sHelp, sName)).splitlines():
            if not sLine.startswith("Key="): continue
            sKey = sLine[4:].strip()
            if not sKey or "+" not in sKey and len(sKey) <= 3: continue
            lsParts = [dAlias.get(s.strip().lower(), s.strip().lower()) for s in sKey.split("+")]
            if "+".join(lsParts) in lsFree or (len(lsParts) == 1 and lsParts[0] in lsFree): continue
            iChecked += 1
            if frozenset(lsParts) not in lsBound:
                lsBad.append("%s teaches %s, which DbDo does not bind" % (sName, sKey))
    for s in lsBad: logLine("TUTORIAL KEY: " + s)
    if lsBad:
        return finding("tutorial keys", "fail", "%s; each is in the log" % countNoun(len(lsBad), "key"))
    return finding("tutorial keys", "pass", "%s taught, all bound in DbDo.cs" % countNoun(iChecked, "key"))


def checkAccessLetters():
    """Access letters within one menu, and names that hide them.

    Two things a speech history showed. First, the screen reader announced
    "Add Table, A" for an item whose letter was T: the menu items had their
    AccessibleName replaced with the caption minus its ampersand, which hides the
    mnemonic, so the reader fell back to the first letter. That fails outright.

    Second, letters repeat within a menu. Windows cycles through duplicates, so
    it is not broken, but a letter pressed once should land. File has none now;
    the larger menus have more items than the alphabet has letters, so this
    reports rather than fails, and says where.
    """
    sSource = readText(os.path.join(sRoot, "DbDo.cs"))
    lsBad = []
    if re.search(r"mi\.AccessibleName\s*=\s*sBase", sSource) or re.search(r"mi\.AccessibleName\s*=\s*sText\.Replace", sSource):
        lsBad.append("a menu item's AccessibleName is replaced, which hides its access letter")
    dMenus = {}
    for sMenu, sCaption in re.findall(r'addItem\((mi[A-Za-z]+),\s*"([^"]*)"', sSource):
        oM = re.search(r"&(.)", sCaption)
        if not oM: continue
        dMenus.setdefault(sMenu, {}).setdefault(oM.group(1).upper(), []).append(sCaption)
    lsReport = []
    for sMenu in sorted(dMenus):
        iDup = len([k for k, v in dMenus[sMenu].items() if len(v) > 1])
        if iDup:
            lsReport.append("%s %d" % (sMenu[2:], iDup))
            for k, v in sorted(dMenus[sMenu].items()):
                if len(v) > 1: logLine("LETTER: %s %s -- %s" % (sMenu[2:], k, " | ".join(v)))
    for s in lsBad: logLine("LETTER: " + s)
    # THE TWO HOMER RULES.
    #   1. A trigger letter is the FIRST letter of one of the command's words.
    #      Never a letter from the middle of a word: better no letter at all.
    #      Standing exceptions: X for a word with the "ex" sound (Export, Exit,
    #      Extract, Regex); Shift reversing a command whose name begins Un-
    #      (Control+M marks, Control+Shift+M unmarks); and Z for a toggle, since
    #      Z is sleep -- a Z key wakes a behaviour or puts it to sleep.
    #   2. In a menu, the trigger letter is the letter of the item's hotkey,
    #      when the hotkey has one.
    for sMenu, sCaption, sExpr in re.findall(r'addItem\((mi[A-Za-z]+),\s*"([^"]*)",\s*"[^"]*",\s*([^,]+?),', sSource):
        oAmp = re.search(r"&(.)", sCaption)
        lsLetters = [k for k in re.findall(r"Keys\.([A-Za-z0-9]+)", sExpr) if len(k) == 1 and k.isalpha()]
        sHot = lsLetters[0].upper() if lsLetters else ""
        sPlain = sCaption.replace("&", "")
        lsInitials = [w[0].upper() for w in re.findall(r"[A-Za-z][A-Za-z0-9']*", sPlain)]
        bSoundX = sHot == "X" and re.search(r"[Ee]x", sPlain)
        bUnShift = sHot and "Shift" in sExpr and re.search(r"\bUn" + sHot.lower(), sPlain, re.I)
        # Z is for sleep: a toggle that puts a behaviour to sleep or wakes it.
        # Z has two documented associations. A toggle: Z is sleep, catching some
        # Z's, so a Z key wakes a behavior or puts it to sleep. And Status: Z is
        # the bottom of the alphabet, and the status bar is the bottom of the
        # window.
        bSleepZ = sHot == "Z" and re.search(r"\b(toggle|status)\b", sPlain, re.I)
        bSoundX = bSoundX or bSleepZ
        if oAmp:
            iAt = sCaption.index("&")
            bStart = iAt == 0 or not sCaption[iAt - 1].isalnum()
            if not bStart and not (bSoundX or bUnShift):
                lsBad.append("%s: %s puts its letter mid-word" % (sMenu[2:], sPlain))
            if sHot and oAmp.group(1).upper() != sHot:
                lsBad.append("%s: %s has letter %s but hotkey letter %s" % (sMenu[2:], sPlain, oAmp.group(1).upper(), sHot))
        if sHot and sHot not in lsInitials and not (bSoundX or bUnShift):
            lsBad.append("%s: %s is bound to %s, and %s starts none of its words"
                         % (sMenu[2:], sPlain, sExpr.replace("Keys.", "").strip(), sHot))
    for s in lsBad: logLine("LETTER: " + s)
    if lsBad:
        return finding("access letters", "fail", "%s; each is in the log" % countNoun(len(lsBad), "rule break"))
    return finding("access letters", "pass",
                   "no hidden letters; repeated letters by menu: " + (", ".join(lsReport) if lsReport else "none"))


def checkAccessibleNames():
    """An accessible name must not repeat words the control already carries.

    This is the mechanism behind years of doubled speech in these programs: a
    list box labelled "&Fields:" whose AccessibleName is also "Fields" is named
    twice, and every reader says it twice. FileDir found forty-eight of them in
    one file; DbDo had nineteen.

    So: every AccessibleName literal is compared, letters and digits only,
    against every caption and label literal in the same file. A match fails.
    A name that matches nothing is the good case -- a control with no words of
    its own, such as the records grid.
    """
    sSource = readText(os.path.join(sRoot, "DbDo.cs"))
    def flat(s): return "".join(c.lower() for c in s if c.isalnum())
    lsCaptions = set(flat(m.group(1)) for m in re.finditer(r'\.Text\s*=\s*"([^"]{2,60})"', sSource))
    lsBad = []
    iChecked = 0
    for oMatch in re.finditer(r'(\w+)\.AccessibleName\s*=\s*"([^"]{2,60})"', sSource):
        iChecked += 1
        if flat(oMatch.group(2)) in lsCaptions:
            lsBad.append("%s is named %r, which is already a caption or label"
                         % (oMatch.group(1), oMatch.group(2)))
    for s in lsBad: logLine("NAME: " + s)
    if lsBad:
        return finding("accessible names", "fail", "%s; each is in the log" % countNoun(len(lsBad), "duplicate"))
    return finding("accessible names", "pass", "%s, none repeating a caption" % countNoun(iChecked, "accessible name"))


def checkSections():
    lsBad = []
    iChecked = 0
    for sDirPath, lsDirs, lsNames in os.walk(sRoot):
        for sName in sorted(lsNames):
            if not sName.lower().endswith((".iss", ".inix")): continue
            iChecked += 1
            sPath = os.path.join(sDirPath, sName)
            for iLine, sLine in enumerate(readText(sPath).splitlines(), 1):
                sTrim = sLine.strip()
                if sTrim.startswith(";") or sTrim.startswith("//"): continue
                # THE RULE, stated precisely rather than approximately: a line
                # that OPENS a section heading must contain nothing else. A
                # comment that mentions [Files] in a sentence is fine and common;
                # a heading with a sentence trailing off it is what a
                # substring-anchored insertion leaves behind, and it is what
                # broke JobTrail.inix.
                if sTrim.startswith("[") and not re.match(r"^\[[^\]]+\]$", sTrim):
                    lsBad.append("%s line %d: %s" % (sName, iLine, sTrim[:70]))
    for s in lsBad: logLine("SECTION: " + s)
    if lsBad:
        return finding("sections", "fail", "%s look like a spliced comment" % countNoun(len(lsBad), "line"))
    return finding("sections", "pass", "%s have no section heading spliced into a comment" %
                   countNoun(iChecked, "file"))


def checkTutorials():
    """Every tutorial script has a title, steps, and something spoken in each.

    Writing the tutorials is what found half the inconsistencies this week, so
    the scripts are worth keeping honest: a step with no Say is a step nobody
    can follow, and a script with no [about] Title has no name in the feed.
    """
    sHelp = os.path.join(sRoot, "help")
    lsBad = []
    iChecked = 0
    if not os.path.isdir(sHelp):
        return finding("tutorials", "skip", "no help folder")
    for sName in sorted(os.listdir(sHelp)):
        if not (sName.startswith("Tutorial") and sName.endswith(".inix")): continue
        iChecked += 1
        sText = readText(os.path.join(sHelp, sName))
        if not re.search(r"^\[about\]\s*$", sText, re.M):
            lsBad.append("%s has no [about] section" % sName)
        if not re.search(r"^Title=", sText, re.M):
            lsBad.append("%s has no Title" % sName)
        lsSteps = re.split(r"^\[step\]\s*$", sText, flags=re.M)[1:]
        if len(lsSteps) < 3:
            lsBad.append("%s has %s" % (sName, countNoun(len(lsSteps), "step")))
        for i, sStep in enumerate(lsSteps, 1):
            # A step needs SOMETHING spoken: narration, or a key the screen
            # reader answers. A step that is only a keystroke and its answer --
            # arrowing to the next checkbox -- is the succinct form and is right;
            # making the narrator say "arrow down" every time is padding.
            bSay = re.search(r"^Say=\S", sStep, re.M) is not None
            bAnswer = re.search(r"^Key=\S", sStep, re.M) is not None and re.search(r"^Hear=\S", sStep, re.M) is not None
            if not (bSay or bAnswer):
                lsBad.append("%s step %d says nothing" % (sName, i))
    for s in lsBad: logLine("TUTORIAL: " + s)
    if lsBad:
        return finding("tutorials", "fail", "%s; each is in the log" % countNoun(len(lsBad), "problem"))
    return finding("tutorials", "pass", "%s, each with a title and at least 3 steps" %
                   countNoun(iChecked, "tutorial"))


def usage(bBad):
    """What this script does, and what it accepts: nothing.

    An audit invoked with an argument it ignores is an audit somebody thinks
    they configured. It takes no options, so it says so and stops.
    """
    print("auditPatterns.py -- check DbDo's source, tutorials and keys against")
    print("the rules that are easy to break and hard to notice.")
    print("")
    print("It takes no options. Run it from anywhere:  scripts\\auditPatterns")
    print("It writes logs\\DbDo-audit-<date>-<time>.log beside the project.")
    return 2 if bBad else 0


def main():
    if len(sys.argv) > 1:
        bHelp = sys.argv[1].lower() in ("-h", "--help", "/?", "help")
        if not bHelp: print("auditPatterns takes no options, and %r is not one." % sys.argv[1])
        return usage(not bHelp)
    global oLog
    oLog = open(sLogPath, "w", encoding="utf-8")
    logLine("auditPatterns started %s" % datetime.datetime.now().isoformat(" ", "seconds"))
    logLine("Project: %s" % sRoot)
    logLine("Python: %s" % sys.version.replace("\n", " "))

    checkLabels()
    checkDuplicateMembers()
    checkShadowedKit()
    checkSayLayer()
    checkHotkeyDoc()
    checkSayAnswers()
    checkSections()
    checkTutorials()
    checkTutorialKeys()
    checkAccessLetters()
    checkAccessibleNames()

    iFailed = len([t for t in lsFindings if t[1] == "fail"])
    iPassed = len([t for t in lsFindings if t[1] == "pass"])
    print("%s passed, %s failed." % (countNoun(iPassed, "check"), countNoun(iFailed, "check")))
    for sCheck, sVerdict, sEvidence in lsFindings:
        if sVerdict == "fail": print("  failed: %s -- %s" % (sCheck, sEvidence))
    if iFailed: print("The detail is in %s." % os.path.basename(sLogPath))
    logLine("Finished %s" % datetime.datetime.now().isoformat(" ", "seconds"))
    oLog.close()
    return 1 if iFailed else 0


if __name__ == "__main__":
    sys.exit(main())
