r"""makeTalkingBookTopics.py -- builds DbDo's TalkingBookTopics template, the books announced in the National Library
Service's bimonthly magazine Talking Book Topics, searchable by full text. The plan is notes\TalkingBookTopics_Plan.md.

    scripts\makeTalkingBookTopics                 finds every issue and builds the template from all of them
    scripts\makeTalkingBookTopics --due           the same, only when the template is missing or older than this script (the build runs this)
    scripts\makeTalkingBookTopics --limit 24      a pilot of 24 issues instead, data\TalkingBookTopics\TalkingBookTopics-pilot.db
    scripts\makeTalkingBookTopics --offline       builds from the copies already kept, asking nobody

CYCLE 1, DISCOVERY AND A PILOT (27 November 2026, Jamal: "go ahead. Make sure that I get succinct status updates as the
harvest is occurring"). The questions the plan left open are answered by this run's log and its coverage report, not
guessed: how far back the issues go, in what shapes they were published, and from which year NLS printed publishers'
descriptions instead of its own. Cycle 2 builds the whole template from what cycle 1 learns.

WHERE THE ISSUES COME FROM.
    The live list      https://www.loc.gov/nls/tbt links each issue since January-February 2023 as one long page.
    The web archive    the Internet Archive's capture index (CDX) lists every page it kept under loc.gov/nls/tbt/
                       and lcweb.loc.gov/nls/tbt/; a page is read raw, without the archive's toolbar (the "id_" form).
Each page found is sorted into a kind (LINKKIND in the log, with samples): a whole issue as a web page or a text file,
an index, a section of an issue, or something else. An issue is known by its year and months, from its address or,
failing that, from its own first lines.

ONE BOOK, ONE ROW. An entry is found by its book number (DB, RC, TB, DBC, TBC or BR and digits). Around it are read
the title, author ("by"), narrator ("Read by", "Narrated by"), length, year, advisories (violence, strong language, sex,
commercial audiobook, bestseller), the heading it falls under (category, audience, fiction or nonfiction), and the
annotation with its source. KEEP ONLY WORDS THAT CAN BE GIVEN AWAY: an annotation is kept when NLS wrote it; one marked
"From publisher", Goodreads or Amazon is left out and annotation_source names its writer. Facts are kept for every book.

STATUS ON THE CONSOLE, SHORT. A line when each stage begins, a line per issue read, and a heartbeat at least every ten
seconds while pages are fetched, so the console never goes quiet. Everything else goes to the log.

RESPECTFUL FETCHING. The kit's homerFetch asks each server at most once every two seconds, keeps every page in
data\TalkingBookTopics, and asks only "has it changed?" of a page it has; archived pages never change, so a kept copy
is used without asking.

WHAT IS WRITTEN.
    data\TalkingBookTopics\TalkingBookTopics-pilot.db        the pilot, to open in DbDo and try searches on
    logs\DbDo-tbtCoverage-<date>-<time>.md                   issues found by year and kind, books read, sources by year
    logs\DbDo-makeTalkingBookTopics-<date>-<time>.log        every request, every count, sample entries
The template itself, templates\TalkingBookTopics\TalkingBookTopics.db, is cycle 2's.
"""

import collections, datetime, html.parser, json, os, platform, re, sqlite3, sys, time, traceback, urllib.parse

sHere = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, sHere)
import makeBrailleForum as core

sVersion = "2026-11-27 version 6: In Brief titles unescaped, untitled articles named, contact lists left out"
sSource = "TalkingBookTopics"
sUserAgent = "DbDo makeTalkingBookTopics/1 (+https://github.com/JamalMazrui/DbDo; a searchable list of NLS Talking Book Topics books)"
sProject = core.sProject
sStamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
sLogPath = os.path.join(sProject, "logs", "DbDo-make%s-%s.log" % (sSource, sStamp))
sCoveragePath = os.path.join(sProject, "logs", "DbDo-tbtCoverage-%s.md" % sStamp)
sDataDir = os.path.join(sProject, "data", sSource)
sPilotPath = os.path.join(sDataDir, sSource + "-pilot.db")
sTemplatePath = os.path.join(sProject, "templates", sSource, sSource + ".db")
sPandoc = ""
core.sLogPath = sLogPath
core.trail.sLogPath = sLogPath
core.iHeartbeatSeconds = 10
log, say, plural = core.log, core.say, core.plural

sLiveIndex = "https://www.loc.gov/nls/tbt"
# VERSION 2: cycle 1 found the archive's issues from 1997 to May-June 2017 under /nls/tbt/ and the live list's from 2023,
# and nothing between. NLS moved its pages in 2017; the new-materials and talking-book-topics addresses are asked too.
lsCdxPrefixes = ["loc.gov/nls/tbt/", "lcweb.loc.gov/nls/tbt/", "loc.gov/nls/new-materials/", "loc.gov/nls/talking-book-topics"]
# VERSION 3: versions 1 and 2 found nothing from July 2017 to 2022, and the live page offers 2023 on only. One wider
# question to the archive's index: any page under loc.gov/nls/ captured from 2017 to 2023 whose address names tbt or
# talking-book-topics. Whatever it finds is logged with its kind, so a missing period that exists anywhere shows.
sCdxWide = "https://web.archive.org/cdx/search/cdx?url=loc.gov/nls/&matchType=prefix&output=json&fl=original,timestamp,mimetype,statuscode,length&filter=statuscode:200&filter=original:.*(tbt|talking-book-topics).*&from=2017&to=2023&collapse=urlkey&limit=20000"
sCdx = "https://web.archive.org/cdx/search/cdx?url=%s&matchType=prefix&output=json&fl=original,timestamp,mimetype,statuscode,length&filter=statuscode:200&collapse=urlkey&limit=20000"
iPilot = 24
nDelay = 2.0

lsMonths = ["january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december"]
dMonthAbbrev = {s[:3]: i + 1 for i, s in enumerate(lsMonths)}
sMonthWord = r"(?:jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|june?|july?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)"
oNumber = re.compile(r"\b(DBC|TBC|DB|RC|TB|BR)[  ]?(\d{3,6})\b")
oByLine = re.compile(r"^\s*(?:by|edited by|compiled by|selected by|written and illustrated by|retold by)\s+(.+?)\s*\.?\s*$", re.I)
oNarrator = re.compile(r"\b(?:read|narrated)\s+by\s+([^.;\n]+)", re.I)
oHours = re.compile(r"\b(\d+)\s+hours?(?:,?\s+(?:and\s+)?(\d+)\s+minutes?)?", re.I)
oMedia = re.compile(r"\b(\d+)\s+(cassettes?|discs?|cartridges?|volumes?)\b", re.I)
oYearEnd = re.compile(r"\b((?:19|20)\d\d)\.?\s*$")
oSourceMark = re.compile(r"\b(From (?:the )?publisher|Goodreads|Amazon)\b[^\n]*$", re.I)
lsAdvisories = ["some violence", "violence", "some strong language", "strong language", "some descriptions of sex",
                "explicit descriptions of sex", "descriptions of sex", "some explicit descriptions of sex", "commercial audiobook",
                "bestseller", "unrated", "some violence and strong language", "recreated", "prize winner", "prizewinner",
                "for senior high and older readers", "for junior and senior high and older readers", "for junior high and older readers",
                "for grades k-3", "for grades 2-4", "for grades 3-6", "for grades 4-7", "for grades 5-8", "for grades 6-9", "for preschool-grade 2"]
lsDropLines = ["download from bard", "download", "order form", "back to top", "skip to"]


# ---------------------------------------------------------------- reading a page into headed lines

class linesParser(html.parser.HTMLParser):
    """A web page as lines of text, each with the heading it falls under. A heading, paragraph, list item, table cell
    or line break ends a line; script and style are skipped."""

    def __init__(self):
        html.parser.HTMLParser.__init__(self, convert_charrefs=True)
        self.lLines, self.lsCurrent, self.sHeading, self.bHeading, self.iSkip = [], [], "", False, 0
        self.iLevel, self.dLevels = 0, {}

    def flush(self):
        sText = re.sub(r"\s+", " ", "".join(self.lsCurrent)).strip()
        self.lsCurrent = []
        if not sText: return
        if self.bHeading:
            # THE HEADING PATH: a heading replaces those at its level and below, so "Adult Fiction > Mystery" is kept whole.
            self.dLevels = {i: s for i, s in self.dLevels.items() if i < self.iLevel}
            self.dLevels[self.iLevel] = sText
            self.sHeading = " > ".join(self.dLevels[i] for i in sorted(self.dLevels))
        self.lLines.append((self.sHeading, sText, self.bHeading))

    def handle_starttag(self, sTag, lAttrs):
        if sTag in ("script", "style", "nav", "header", "footer"): self.iSkip += 1
        if sTag in ("p", "li", "br", "div", "tr", "td", "dd", "dt", "h1", "h2", "h3", "h4", "h5", "h6"): self.flush()
        if sTag in ("h1", "h2", "h3", "h4", "h5", "h6"): self.bHeading, self.iLevel = True, int(sTag[1])

    def handle_endtag(self, sTag):
        if sTag in ("script", "style", "nav", "header", "footer"): self.iSkip = max(0, self.iSkip - 1)
        if sTag in ("p", "li", "div", "tr", "td", "dd", "dt", "h1", "h2", "h3", "h4", "h5", "h6"): self.flush()
        if sTag in ("h1", "h2", "h3", "h4", "h5", "h6"): self.bHeading = False

    def handle_data(self, sData):
        if not self.iSkip: self.lsCurrent.append(sData)


def pageLines(sText, bHtml):
    """(heading, line, is a heading) for each line of a page. A text file's headings are lines in capitals."""
    if bHtml:
        oParser = linesParser()
        oParser.feed(sText)
        oParser.flush()
        return oParser.lLines
    lResult, sHeading, lsPath = [], "", []
    for sLine in sText.splitlines():
        sLine = sLine.strip()
        if not sLine: continue
        bHeading = len(sLine) < 60 and sLine.upper() == sLine and re.search(r"[A-Z]{3}", sLine) is not None and not oNumber.search(sLine)
        if bHeading:
            lsPath = (lsPath + [sLine.title()])[-2:]
            sHeading = " > ".join(lsPath)
        lResult.append((sHeading, sLine, bHeading))
    return lResult


# ---------------------------------------------------------------- an issue's date

def monthNumber(sWord):
    return dMonthAbbrev.get(sWord.lower()[:3], 0)


def issueDate(sText):
    """(year, first month, last month) of an issue from its address or its first lines, or None."""
    sLower = sText.lower()
    oMatch = re.search(r"(" + sMonthWord + r")[\s\-/_,&and]*(" + sMonthWord + r")[\s\-/_,]*((?:19|20)\d\d)", sLower)
    if oMatch: return (int(oMatch.group(3)), monthNumber(oMatch.group(1)), monthNumber(oMatch.group(2)))
    oMatch = re.search(r"((?:19|20)\d\d)[/\-_]?\d{0,2}(" + sMonthWord + r")(" + sMonthWord + r")", sLower)
    if oMatch: return (int(oMatch.group(1)), monthNumber(oMatch.group(2)), monthNumber(oMatch.group(3)))
    oMatch = re.search(r"/((?:19|20)\d\d)/\d{0,2}(" + sMonthWord + r")(" + sMonthWord + r")?", sLower)
    if oMatch: return (int(oMatch.group(1)), monthNumber(oMatch.group(2)), monthNumber(oMatch.group(3) or oMatch.group(2)))
    return None


def issueTitle(tDate):
    iYear, iFirst, iLast = tDate
    sFirst, sLast = lsMonths[iFirst - 1].title(), lsMonths[iLast - 1].title()
    return "%s-%s %d" % (sFirst, sLast, iYear) if iFirst != iLast else "%s %d" % (sFirst, iYear)


# ---------------------------------------------------------------- finding the issues

def linkKind(sUrl, sMime, iLength):
    """What an archived address seems to be, from its shape. Logged with samples so the next cycle can read new shapes."""
    sLower = sUrl.lower().split("?")[0]
    sLeaf = sLower.rstrip("/").rsplit("/", 1)[-1]
    # VERSION 3: an issue's address may end in a slash, as loc.gov's new-materials pages do; version 2 called those indexes.
    if "talking-book-topics-" in sLower and issueDate(sLower): return "web issue"
    if sLeaf in ("", "index.html", "index.htm", "tbt") or "index" in sLeaf or sLower.endswith("/"): return "index"
    if re.search(r"\.(pdf|mp3|zip|gif|jpg|png|css|js)$", sLower): return "other file"
    if sLower.endswith(".txt"): return "text issue" if issueDate(sLower) else "text other"
    if re.search(r"\.html?$", sLower):
        if issueDate(sLower): return "web issue" if iLength == 0 or iLength > 20000 else "web section"
        return "web other"
    return "other"


def discoverIssues(oCache, bOffline):
    """Every issue found, as {key: dict}, the live ones first; the web archive fills the years before them."""
    dIssues = {}
    dKinds, dSamples = collections.Counter(), collections.defaultdict(list)
    # The live list.
    core.sHeartbeat = "Finding issues: the live list at loc.gov."
    dResult = oCache.fetch(sLiveIndex)
    sHtml = core.decoded(dResult.get("body"))
    for sHref in sorted(set(re.findall(r'href="([^"]*talking-book-topics-[a-z\-]+-\d{4}[^"]*)"', sHtml, re.I))):
        sUrl = urllib.parse.urljoin(sLiveIndex + "/", sHref.split("#")[0])
        tDate = issueDate(sUrl)
        if not tDate: continue
        sKey = "%04d-%02d" % (tDate[0], tDate[1])
        dIssues.setdefault(sKey, {"key": sKey, "date": tDate, "url": sUrl, "fetch": sUrl, "kind": "live web issue", "html": True})
        dKinds["live web issue"] += 1
    log("LIVE %s: %s" % (sLiveIndex, plural(len(dIssues), "issue")))
    say("Live list: %s." % plural(len(dIssues), "issue"))
    # The web archive.
    for sPrefix in lsCdxPrefixes + ["wide"]:
        core.sHeartbeat = "Finding issues: the web archive's list for %s." % sPrefix
        sQuery = sCdxWide if sPrefix == "wide" else sCdx % urllib.parse.quote(sPrefix, safe="")
        dResult = oCache.fetch(sQuery)
        try: lRows = json.loads(core.decoded(dResult.get("body")) or "[]")
        except Exception as oError:
            log("CDX %s could not be read: %s" % (sPrefix, oError)); lRows = []
        lRows = lRows[1:] if lRows and lRows[0] and lRows[0][0] == "original" else lRows
        log("CDX %s: %s" % (sPrefix, plural(len(lRows), "page")))
        for lRow in lRows:
            sOriginal, sTime, sMime = lRow[0], lRow[1], lRow[2]
            iLength = int(lRow[4]) if len(lRow) > 4 and str(lRow[4]).isdigit() else 0
            sKind = linkKind(sOriginal, sMime, iLength)
            dKinds[sKind] += 1
            if len(dSamples[sKind]) < 8: dSamples[sKind].append(sOriginal)
            if sKind not in ("web issue", "text issue"): continue
            tDate = issueDate(sOriginal)
            sKey = "%04d-%02d" % (tDate[0], tDate[1])
            dOld = dIssues.get(sKey)
            dNew = {"key": sKey, "date": tDate, "url": sOriginal, "fetch": "https://web.archive.org/web/%sid_/%s" % (sTime, sOriginal),
                    "kind": sKind, "html": sKind == "web issue"}
            # A web page beats a text file, which has no headings to tell a category by; the live page beats both.
            if dOld is None or (dOld["kind"] == "text issue" and sKind == "web issue"): dIssues[sKey] = dNew
    for sKind in sorted(dKinds):
        log("LINKKIND %s: %d; samples: %s" % (sKind, dKinds[sKind], " | ".join(dSamples.get(sKind, [])) or "(live list)"))
    core.sHeartbeat = ""
    return dIssues, dKinds


# ---------------------------------------------------------------- reading one issue

def entryOf(lBlock, sHeading):
    """A book from the lines of one entry, or None. lBlock is a list of line texts, the book number in the first."""
    sJoined = "\n".join(lBlock)
    oNum = oNumber.search(sJoined)
    if not oNum: return None
    sNumber = oNum.group(1) + oNum.group(2)
    sFirst = lBlock[0]
    sTitle = sFirst[:sFirst.find(oNum.group(0))].strip(" .,:;-–—") if oNum.group(0) in sFirst else ""
    lsRest = lBlock[1:]
    # The rest of the number's line -- "8 hours 39 minutes" in the live issues -- is read with the lines after it.
    sAfter = sFirst[sFirst.find(oNum.group(0)) + len(oNum.group(0)):].strip(" .,:;-\u2013\u2014") if oNum.group(0) in sFirst else ""
    if sAfter: lsRest = [sAfter] + lsRest
    if not sTitle and lsRest and not oByLine.match(lsRest[0]) and not (oHours.search(lsRest[0]) or oMedia.search(lsRest[0])): sTitle, lsRest = lsRest[0], lsRest[1:]
    dBook = {"title": sTitle, "number": sNumber, "author": "", "narrator": "", "length": "", "year": "", "annotation": "",
             "annotation_source": "NLS", "tags": [], "heading": sHeading}
    lsKeep = []
    # AN AUTHOR WITHOUT "BY": newer issues put the author's name alone on the line after the title. The first such line,
    # short, with no digits and no sentence's end, is taken as the author; the log's samples show whether that holds.
    if lsRest and not oByLine.match(lsRest[0]) and len(lsRest[0]) <= 70 and not re.search(r"\d", lsRest[0]) \
            and not lsRest[0].rstrip().endswith((".", "!", "?")) and not oNarrator.search(lsRest[0]):
        dBook["author"] = lsRest[0].strip(" ,;")
        lsRest = lsRest[1:]
    for sLine in lsRest:
        sLow = sLine.lower().strip()
        if any(sLow.startswith(s) for s in lsDropLines): continue
        oBy = oByLine.match(sLine)
        if oBy and not dBook["author"]: dBook["author"] = oBy.group(1).strip(" .,"); continue
        # A LINE THAT IS ONLY THE NARRATOR keeps the whole name, initials and all ("read by B. J. Harrison").
        oWhole = re.match(r"^\s*(?:read|narrated)\s+by\s+(.+?)\.?\s*$", sLine, re.I)
        if oWhole and not dBook["narrator"]:
            dBook["narrator"] = oWhole.group(1).strip(" ,")
            continue
        oRead = oNarrator.search(sLine)
        if oRead and not dBook["narrator"]:
            dBook["narrator"] = oRead.group(1).strip(" .,")
            sLine = (sLine[:oRead.start()] + sLine[oRead.end():]).strip(" .,;")
            if not sLine: continue
        oLen = oHours.search(sLine) or oMedia.search(sLine)
        if oLen and not dBook["length"]:
            dBook["length"] = oLen.group(0)
            sRemainder = (sLine[:oLen.start()] + sLine[oLen.end():]).strip(" .,;()")
            if re.fullmatch(r"(?i)(reading time:?|length:?)?", sRemainder): continue
        lsKeep.append(sLine)
    sText = " ".join(lsKeep).strip()
    for sAdvisory in sorted(lsAdvisories, key=len, reverse=True):
        if re.search(r"\b" + re.escape(sAdvisory) + r"\b", sText, re.I):
            dBook["tags"].append(sAdvisory[4:] if sAdvisory.startswith("for ") else sAdvisory)
            sText = re.sub(r"\b" + re.escape(sAdvisory) + r"\b\.?", "", sText, count=1, flags=re.I)
    oYear = oYearEnd.search(sText)
    if oYear:
        dBook["year"] = oYear.group(1)
        sText = sText[:oYear.start()].strip()
    # WHO WROTE IT, after the advisories and year are taken out, so a description marked "From publisher" in its middle
    # still gives up the labels NLS put after it.
    # ONLY ITS OWN MARK (version 4, from version 3's log): "From publisher." is how NLS marks a description it did not
    # write, and "From Goodreads." and "From Amazon." likewise. Version 3 also took the word Amazon anywhere -- the river,
    # the company -- and so dropped a few dozen of NLS's own descriptions, and mislabeled The Lost Tribe, whose
    # description mentions the Amazon and ends "From publisher." The last such mark is the one that counts.
    lMarks = list(re.finditer(r"\bFrom (?:the )?(publisher|Goodreads|Amazon)\b\.?", sText, re.I))
    oSource = lMarks[-1] if lMarks else None
    if oSource:
        sWord = oSource.group(1).lower()
        dBook["annotation_source"] = "publisher" if "publisher" in sWord else ("Goodreads" if "goodreads" in sWord else "Amazon")
        sText = sText[:oSource.start()].strip()
    sText = re.sub(r"\s+([.,;])", r"\1", re.sub(r"\s{2,}", " ", sText)).strip(" ,;")
    # What taking the advisories out leaves behind: "and." or ". ." where they stood.
    sText = re.sub(r"(?:\s*\band\b)?\s*\.(?:\s*\.)+", ".", re.sub(r"\.\s+and\s*\.", ".", sText)).strip(" ,;")
    sText = re.sub(r"\s+and\.?$", "", sText).strip(" ,;")
    if sText in (".", "and", "and."): sText = ""
    dBook["annotation"] = sText if dBook["annotation_source"] == "NLS" else ""
    dBook["annotation_seen"] = len(sText)
    sHead = sHeading.lower()
    dBook["audience"] = "children" if re.search(r"child|young|kids|grades|teen", sHead) else "adult"
    dBook["fiction"] = "nonfiction" if "nonfiction" in sHead.replace("-", "") else ("fiction" if "fiction" in sHead else "")
    dBook["category"] = sHeading.rsplit(" > ", 1)[-1]
    dBook["url"] = "https://nlsbard.loc.gov/bard2-web/search/%s/" % sNumber if sNumber.startswith("DB") else ""
    return dBook


# ---------------------------------------------------------------- In Brief, the magazine's news

# IN BRIEF AS MARKDOWN (version 5, 27 November 2026, Jamal: "Control+Shift+H has nothing to work on ... Could we populate
# a new textmarkdown field with meaningful text?"). Version 4's log found an In Brief section in 149 of 175 issues: NLS's
# own short articles -- new services, BARD, the move from cassettes to digital players. They are federal work, free to
# keep, and already in the saved pages, so nothing new is fetched. Each becomes a row of updates, its words as Markdown
# in body (Preview Markdown, Control+Shift+H, shows them as a web page) and as plain words in search_text for the index.
# HOW THE SECTION IS FOUND. A web issue goes through Pandoc whole; a text issue's lines are joined into paragraphs. The
# section starts at a line that is just "In Brief" (a heading, bold or alone -- not a contents link) and ends at the first
# of the book sections. Inside it, a heading, a line all in bold or one wrapped in underscores starts an article.
oBriefStart = re.compile(r"^in brief$", re.I)
oBriefEnd = re.compile(r"^(books for (adults|children|young (people|readers))|adult (non)?fiction|children'?s (books|non ?fiction|fiction)|"
                       r"audio magazines|magazines|nonfiction|fiction|order form|about talking book topics)\b", re.I)
oTitleLine = re.compile(r"^(#{1,6}\s+.+|\*\*[^*]+\*\*|__[^_]+__|_[^_]+_)$")


def bareLine(sLine):
    """A Markdown line's words, without heading marks, emphasis or a trailing colon."""
    sLine = re.sub(r"^#{1,6}\s+", "", sLine.strip())
    sLine = re.sub(r"\{[^}]*\}$", "", sLine).strip()
    # VERSION 6: Pandoc's escapes and inner emphasis come off a title ("Children\'s", "*Talking Book Topic*").
    sLine = re.sub(r"\\(.)", r"\1", sLine)
    sLine = re.sub(r"(\*\*|__|\*|_)(.+?)\1", r"\2", sLine)
    return sLine.strip("*_ ").rstrip(":").strip()


def plainWords(sMarkdown):
    """Markdown as plain words for the full-text index: link text kept, addresses and marks dropped."""
    sText = re.sub(r"\\([\\`*_{}\[\]()#+\-.!'\"<>|])", r"\1", sMarkdown)
    sText = re.sub(r"!\[[^\]]*\]\([^)]*\)", " ", sText)
    sText = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", sText)
    sText = re.sub(r"<https?://[^>]+>|https?://\S+", " ", sText)
    sText = re.sub(r"[#*_>`|\\]", " ", sText)
    return re.sub(r"\s+", " ", sText).strip()


def markdownLines(sText, bHtml):
    """An issue as Markdown lines, one paragraph or heading a line."""
    if bHtml:
        if not sPandoc: return []
        sMarkdown = core.toMarkdown(sPandoc, [sText])[0]
        lsLines, lsParagraph = [], []
        for sLine in sMarkdown.split("\n") + [""]:
            if sLine.strip(): lsParagraph.append(sLine.strip()); continue
            if lsParagraph: lsLines.append(" ".join(lsParagraph)); lsParagraph = []
        return lsLines
    lsLines, lsParagraph = [], []
    for sLine in sText.splitlines() + [""]:
        sLine = sLine.strip()
        if sLine and not oTitleLine.match(sLine) and not oBriefStart.match(bareLine(sLine)):
            lsParagraph.append(sLine); continue
        if lsParagraph: lsLines.append(" ".join(lsParagraph)); lsParagraph = []
        if sLine: lsLines.append(sLine)
    return lsLines


def briefArticles(sText, bHtml, sKey):
    """The In Brief articles of one issue: (title, Markdown body), in printed order."""
    try:
        lsLines = markdownLines(sText, bHtml)
    except Exception as oError:
        log("BRIEF %s: not read (%s)" % (sKey, oError))
        return []
    iStart = next((i for i, s in enumerate(lsLines) if oBriefStart.match(bareLine(s)) and "](" not in s
                   and i + 1 < len(lsLines) and not oBriefEnd.match(bareLine(lsLines[i + 1]))), -1)
    if iStart < 0: return []
    lArticles, sTitle, lsBody = [], "", []
    def keep():
        sBody = "\n\n".join(s for s in lsBody if not re.match(r"^\[?back to top", s, re.I))
        sWords = plainWords(sBody)
        # CONTACT LISTS ARE LEFT OUT (version 6). Version 5 kept the advisory group's member lists of 2005 to 2017:
        # volunteers' home addresses and email. They are public record, but a shipped database has no need to carry
        # them, and no reader searches for them. Two or more email addresses mark such a list.
        if len(re.findall(r"[\w.+-]+@[\w-]+\.[\w.]+", sBody)) >= 2:
            log("BRIEF %s: a contact list left out, %s" % (sKey, sTitle or "untitled"))
            return
        # AN UNTITLED ARTICLE (version 6) -- text before the first title, or a section with none -- is named by its
        # first sentence, as a reader would refer to it, rather than all being called "In Brief".
        sName = sTitle or re.split(r"(?<=[.!?])\s", sWords, 1)[0][:90].rstrip(" ,;:")
        if len(sWords.split()) >= 15:
            lArticles.append((sName, sBody))
        elif sBody:
            log("BRIEF %s: a part too short to keep, %s: %s" % (sKey, sTitle or "untitled", plainWords(sBody)[:120]))
    for sLine in lsLines[iStart + 1:]:
        sBare = bareLine(sLine)
        if oBriefEnd.match(sBare) and (oTitleLine.match(sLine) or len(sBare) < 40): break
        if oTitleLine.match(sLine) and len(sBare) < 120:
            keep()
            sTitle, lsBody = sBare, []
            continue
        lsBody.append(sLine)
    keep()
    if len(lArticles) > 20: log("BRIEF %s: %d articles, more than expected; the first is %s" % (sKey, len(lArticles), lArticles[0][0]))
    return lArticles


def readIssue(oCache, dIssue, iSamples):
    """The books of one issue, with counts for the coverage report."""
    dResult = oCache.fetch(dIssue["fetch"])
    binBody = dResult.get("body")
    if not binBody:
        log("ISSUE %s %s: nothing fetched (%s)" % (dIssue["key"], dIssue["fetch"], dResult.get("error", "")))
        return []
    sText = core.decoded(binBody)
    dIssue["updates"] = briefArticles(sText, dIssue["html"], dIssue["key"])
    if dIssue["updates"]: log("BRIEF %s: %s: %s" % (dIssue["key"], plural(len(dIssue["updates"]), "article"), " | ".join(t for t, _ in dIssue["updates"])[:400]))
    lLines = pageLines(sText, dIssue["html"])
    lBooks, lBlock, sBlockHeading = [], [], ""
    iNumberLines = 0
    sPrevious = ""
    lsHeadings = collections.Counter()
    bInBrief = False
    def close():
        if lBlock:
            dBook = entryOf(lBlock, sBlockHeading)
            if dBook: lBooks.append(dBook)
    for sHeading, sLine, bHeading in lLines:
        if bHeading:
            lsHeadings[sLine] += 1
            if re.search(r"in brief", sLine, re.I): bInBrief = True
            close()
            lBlock, sBlockHeading, sPrevious = [], sHeading, ""
            continue
        oNum = oNumber.search(sLine)
        if not oNum:
            if lBlock: lBlock.append(sLine)
            sPrevious = sLine
            continue
        iNumberLines += 1
        sNumber = oNum.group(1) + oNum.group(2)
        # THE CLOSING LINE (version 2, from cycle 1's log): the live issues, and those of 2015 to 2017, end an entry with
        # its number again, as a link, "DB139182 Wolverine: Violent Tendencies". The same number as the open entry's
        # closes it rather than starting a second book.
        oOpen = oNumber.search(lBlock[0]) if lBlock else None
        if oOpen and oOpen.group(1) + oOpen.group(2) == sNumber:
            close()
            lBlock, sPrevious = [], ""
            continue
        # A TITLE ON THE LINE BEFORE ITS NUMBER: when nothing stands before the number on its line, the title is the line
        # just before -- which the open entry collected as its last line, or which stood alone after a heading.
        sBefore = sLine[:oNum.start()].strip(" .,:;-\u2013\u2014")
        sTitleBefore = ""
        if not sBefore:
            if len(lBlock) > 1 and lBlock[-1] == sPrevious: sTitleBefore = lBlock.pop()
            elif not lBlock and sPrevious: sTitleBefore = sPrevious
        close()
        lBlock, sBlockHeading = [(sTitleBefore + " " + sLine).strip()], sHeading
        sPrevious = sLine
    close()
    # NOT A BOOK (version 3, from version 2's log): an issue that explains its book numbers ("DB/ RC 12345") gave entries
    # with no title worth the name and nothing else; an entry with no two letters together in its title, or with neither
    # author, narrator nor length, is set aside and counted.
    iJunk = len(lBooks)
    lBooks = [d for d in lBooks if re.search(r"[A-Za-z]{2}", d["title"] or "") and (d["author"] or d["narrator"] or d["length"])]
    if len(lBooks) < iJunk: log("ISSUE %s: %d entries that are not books set aside" % (dIssue["key"], iJunk - len(lBooks)))
    # A book listed twice in one issue (a closing line the rule above did not catch) is kept once.
    dSeen, lUnique = set(), []
    for dBook in lBooks:
        if dBook["number"] in dSeen: continue
        dSeen.add(dBook["number"])
        lUnique.append(dBook)
    if len(lUnique) < len(lBooks): log("ISSUE %s: %d repeated book numbers set aside" % (dIssue["key"], len(lBooks) - len(lUnique)))
    lBooks = lUnique
    dSources = collections.Counter(d["annotation_source"] for d in lBooks)
    log("ISSUE %s (%s, %s): %d lines, %d with a book number, %s; sources %s; headings %d; In Brief %s; %d without author; %d without title"
        % (dIssue["key"], dIssue["kind"], dIssue["url"], len(lLines), iNumberLines, plural(len(lBooks), "book"), dict(dSources),
           len(lsHeadings), "yes" if bInBrief else "no", sum(1 for d in lBooks if not d["author"]), sum(1 for d in lBooks if not d["title"])))
    log("HEADINGS %s: %s" % (dIssue["key"], " | ".join(s for s, _ in lsHeadings.most_common(25))))
    for dBook in lBooks[:iSamples]:
        log("SAMPLE %s: %s" % (dIssue["key"], json.dumps({k: v for k, v in dBook.items() if k != "url"}, ensure_ascii=False)[:900]))
    if not lBooks:
        log("NOBOOKS %s: first lines: %s" % (dIssue["key"], " || ".join(s for _, s, _ in lLines[:30])[:1500]))
    return lBooks


# ---------------------------------------------------------------- the pilot database

def buildDatabase(lIssues, sOut):
    """The pilot: issues and books as every DbDo template has them -- defs and maps, id, added and edited first, then the
    main fields, then notes, tags, url, look, prime and marked -- with a full-text index on books. VERSION 2: cycle 1's
    plain tables failed the kit's checkDb at push, which checks every database in the project."""
    os.makedirs(os.path.dirname(sOut), exist_ok=True)
    sNew = sOut + ".building"
    for sPath in (sNew, sNew + "-journal"):
        if os.path.exists(sPath): os.remove(sPath)
    oDb = sqlite3.connect(sNew)
    trail = core.trail
    for sStatement in [x for x in trail.systemDdl() if not x.startswith("CREATE TABLE views")]: oDb.execute(sStatement)
    dIssueTypes = {"title": "TEXTLINE", "published": "TEXTLINE", "books": "INTEGER", "kind": "TEXTLINE"}
    dBookTypes = {"title": "TEXTLINE", "author": "TEXTLINE", "narrator": "TEXTLINE", "number": "TEXTLINE", "audience": "TEXTLINE",
                  "fiction": "TEXTLINE", "category": "TEXTLINE", "length": "TEXTLINE", "year": "TEXTLINE", "issue": "TEXTLINE",
                  "issue_id": "INTEGER", "annotation": "TEXTMEMO", "annotation_source": "TEXTLINE"}
    for sStatement in trail.tableDdl("issues", "issue_id", ["title", "published", "books", "kind"], ["title"], ["published", "title"], dIssueTypes): oDb.execute(sStatement)
    for sStatement in trail.tableDdl("books", "book_id", ["title", "author", "narrator", "number", "audience", "fiction", "category", "length", "year",
                                                         "issue", "issue_id", "annotation", "annotation_source"], ["title", "author"], ["title", "author", "number"], dBookTypes):
        oDb.execute(sStatement)
    oDb.execute("CREATE INDEX idx_books_issue_id ON books (issue_id)")
    # UPDATES (version 5): the In Brief articles. Not "briefs" or "articles", whose first letters books and authors
    # already take, so typing u in the list of tables reaches it at once.
    dUpdateTypes = {"title": "TEXTLINE", "issue": "TEXTLINE", "issue_id": "INTEGER", "published": "TEXTLINE", "position": "INTEGER",
                    "body": "TEXTMARKDOWN", "search_text": "TEXTMEMO"}
    for sStatement in trail.tableDdl("updates", "update_id", ["title", "issue", "published", "position", "body", "search_text", "issue_id"],
                                     ["title"], ["published", "position", "title"], dUpdateTypes): oDb.execute(sStatement)
    # AUTHORS AND NARRATORS (version 3, the plan's people tables): one row a person, with how many books and the first and
    # last issue, mapped to each book by maps rows -- written_by and read_by -- as BrailleMonitor maps writers to articles.
    dPeopleTypes = {"name": "TEXTLINE", "books": "INTEGER", "first_issue": "TEXTLINE", "last_issue": "TEXTLINE"}
    for sTable, sId in (("authors", "author_id"), ("narrators", "narrator_id")):
        for sStatement in trail.tableDdl(sTable, sId, ["name", "books", "first_issue", "last_issue"], ["name"], ["name"], dPeopleTypes): oDb.execute(sStatement)
    iBooks = 0
    for dIssue in lIssues:
        oCursor = oDb.execute("INSERT INTO issues (title, published, books, kind, url) VALUES (?, ?, ?, ?, ?)",
                              (issueTitle(dIssue["date"]), "%04d-%02d-01" % (dIssue["date"][0], dIssue["date"][1]), len(dIssue["books"]),
                               dIssue["kind"], dIssue["url"]))
        iIssue = oCursor.lastrowid
        for d in dIssue["books"]:
            oCursor = oDb.execute("INSERT OR IGNORE INTO books (title, author, narrator, number, audience, fiction, category, length, year, issue, issue_id, "
                        "annotation, annotation_source, tags, url) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                        (d["title"] or None, d["author"] or None, d["narrator"] or None, d["number"], d["audience"] or None, d["fiction"] or None,
                         d["category"] or None, d["length"] or None, d["year"] or None, issueTitle(dIssue["date"]), iIssue, d["annotation"] or None,
                         d["annotation_source"], "\n".join(d["tags"]) or None, d["url"] or None))
            if oCursor.rowcount: iBooks += 1
            else: log("PRIME %s: %s by %s, %s, is already in the pilot" % (dIssue["key"], d["title"], d["author"], d["number"]))
        for iPosition, (sTitle, sBody) in enumerate(dIssue.get("updates", []), 1):
            oDb.execute("INSERT OR IGNORE INTO updates (title, issue, published, position, body, search_text, issue_id, url) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                        (sTitle, issueTitle(dIssue["date"]), "%04d-%02d-01" % (dIssue["date"][0], dIssue["date"][1]), iPosition, sBody,
                         plainWords(sBody), iIssue, dIssue["url"]))
    iUpdates = oDb.execute("SELECT count(*) FROM updates").fetchone()[0]
    log("UPDATES %s from %s" % (plural(iUpdates, "In Brief article"), plural(sum(1 for d in lIssues if d.get("updates")), "issue")))
    say("In Brief: %s." % plural(iUpdates, "article"))
    for sTable, sField, sKind, sMeaning in (("authors", "author", "written_by", "The book (subject) was written by the author (object), as the magazine names them."),
                                            ("narrators", "narrator", "read_by", "The book (subject) was read by the narrator (object), as the magazine names them.")):
        dPeople = collections.OrderedDict()
        lMaps = []
        for sPrime, sByline, sPublished in oDb.execute("SELECT b.prime, b.%s, i.published FROM books b JOIN issues i ON i.issue_id = b.issue_id "
                                                       "WHERE b.%s IS NOT NULL ORDER BY i.published" % (sField, sField)).fetchall():
            for sName in core.authorNames(sByline) or ([sByline.strip()] if 2 <= len(sByline.split()) <= 5 else []):
                dPerson = dPeople.setdefault(sName.lower(), {"name": sName, "books": 0, "first": sPublished, "last": sPublished})
                dPerson["books"] += 1
                dPerson["last"] = sPublished
                lMaps.append(("books", sPrime, sKind, sTable, dPerson["name"]))
        for dPerson in sorted(dPeople.values(), key=lambda d: d["name"].lower()):
            oDb.execute("INSERT INTO %s (name, books, first_issue, last_issue) VALUES (?, ?, ?, ?)" % sTable,
                        (dPerson["name"], dPerson["books"], dPerson["first"][:7], dPerson["last"][:7]))
        for tMap in lMaps: oDb.execute("INSERT OR IGNORE INTO maps (tbl1, prime1, kind, tbl2, prime2) VALUES (?, ?, ?, ?, ?)", tMap)
        trail.addLookups(oDb, "maps", "kind", {sKind: sMeaning}, sSource)
        log("PEOPLE %s: %s, %s" % (sTable, plural(len(dPeople), "person", "people"), plural(len(lMaps), sKind + " map")))
        say("%s: %s." % (sTable.title(), plural(len(dPeople), "person", "people")))
    # WHAT THE FIXED VALUES MEAN (version 4): defs rows for the fields with a few set values, so Shift+F1 and the pick
    # list on a value say what it is.
    trail.addLookups(oDb, "books", "audience", {"adult": "A book for adults.", "children": "A book for children or young people."}, sSource)
    trail.addLookups(oDb, "books", "fiction", {"fiction": "A made-up story.", "nonfiction": "A true account: history, biography, how-to and the like."}, sSource)
    trail.addLookups(oDb, "books", "annotation_source", {"NLS": "NLS wrote the description, and it is kept here.",
                     "publisher": "NLS printed the publisher's description, which is not kept here.",
                     "Goodreads": "NLS printed a description from Goodreads, which is not kept here.",
                     "Amazon": "NLS printed a description from Amazon, which is not kept here."}, sSource)
    trail.addLookups(oDb, "issues", "kind", {"live web issue": "Read from the issue's page at loc.gov.", "web issue": "Read from an archived copy of the issue's web page.",
                     "text issue": "Read from an archived copy of the issue's text file."}, sSource)
    sFts = ""
    if core.homerFts.available(oDb):
        # NO PREFIX INDEXES (version 4): version 3's template was 174 MB. A word's start (space*) is still found without
        # them, by a range in the main index, fast enough for sixty thousand books; the prefix indexes cost size for a
        # speed nobody would hear.
        sFts = core.homerFts.createIndex(oDb, "books", "book_id", ["title", "author", "narrator", "category", "annotation", "tags"], sPrefix="")
        # THE WEIGHTS LIVE IN THE DATABASE (version 4, from the SQLite FTS tutorial Jamal had ChatGPT write): a title match
        # counts ten times a match in the description, an author five, a narrator three, a category two. Stored as the
        # index's default rank, so every search that orders by rank -- the saved searches, and any DbDo adds -- weighs alike.
        oDb.execute("INSERT INTO books_fts (books_fts, rank) VALUES ('rank', 'bm25(10.0, 5.0, 3.0, 2.0, 1.0, 1.0)')")
        core.homerFts.optimize(oDb, sFts)
        sProblem = core.homerFts.integrityProblem(oDb, sFts)
        log("FTS %s: %s" % (sFts, sProblem or "sound"))
        for sMatch in ("blind*", "guide NEAR(dog, 5)", "\"civil war\"", "braille"):
            try:
                iHits = oDb.execute("SELECT count(*) FROM books_fts WHERE books_fts MATCH ?", (sMatch,)).fetchone()[0]
                log("SEARCH %s: %s" % (sMatch, plural(iHits, "book")))
            except Exception as oError:
                log("SEARCH %s failed: %s" % (sMatch, oError))
        # THE IN BRIEF INDEX (version 5): title and plain words, title counting most; body stays Markdown, to read.
        sUpdatesFts = core.homerFts.createIndex(oDb, "updates", "update_id", ["title", "search_text"], sPrefix="")
        oDb.execute("INSERT INTO updates_fts (updates_fts, rank) VALUES ('rank', 'bm25(5.0, 1.0)')")
        core.homerFts.optimize(oDb, sUpdatesFts)
        log("FTS %s: %s" % (sUpdatesFts, core.homerFts.integrityProblem(oDb, sUpdatesFts) or "sound"))
        runGuideExamples(oDb)
    else:
        log("FTS: this SQLite has no FTS5")
    oDb.commit()
    # SMALLER ON DISK (version 4): VACUUM rewrites the file without the pages that building and optimizing left empty, and
    # the log names what takes the room, table by table, for the next cycle's decisions.
    oDb.execute("VACUUM")
    try:
        for sName, iBytes in oDb.execute("SELECT name, sum(pgsize) FROM dbstat GROUP BY name ORDER BY 2 DESC LIMIT 12").fetchall():
            log("SIZE %s: %s KB" % (sName, "{:,}".format(iBytes // 1024)))
    except Exception as oError:
        # FIELD SIZES WITHOUT DBSTAT (version 5): the Python of version 4's run lacked dbstat, so each text field's
        # bytes are summed instead -- the table's own words, before the index.
        log("SIZE: dbstat is not in this SQLite (%s); field sizes instead" % oError)
        for sTable in ("books", "updates", "authors", "narrators", "issues", "maps", "defs"):
            for oCol in oDb.execute('PRAGMA table_info("%s")' % sTable).fetchall():
                iBytes = oDb.execute('SELECT coalesce(sum(length(CAST("%s" AS BLOB))), 0) FROM "%s"' % (oCol[1], sTable)).fetchone()[0]
                if iBytes >= 1024 * 100: log("SIZE %s.%s: %s KB" % (sTable, oCol[1], "{:,}".format(iBytes // 1024)))
    oDb.close()
    log("SIZE file: %s KB" % "{:,}".format(os.path.getsize(sNew) // 1024))
    if os.path.exists(sOut): os.remove(sOut)
    os.replace(sNew, sOut)
    return iBooks, sFts


def runGuideExamples(oDb):
    """EVERY EXAMPLE IN THE GUIDE IS RUN (version 5), as makeBrailleForum runs its own: each search in backticks in the
    "Searching TalkingBookTopics" part of help\\DbDo.md -- which the tutorials teach too -- is tried on the new database as
    Full-text match runs it, and its count logged, so a refused or empty example is caught by the build, not a reader."""
    sGuide = os.path.join(sProject, "help", "DbDo.md")
    sText = open(sGuide, encoding="utf-8-sig").read() if os.path.isfile(sGuide) else ""
    oPart = re.search(r"(?ms)^#### Searching %s\s*$(.*?)(?=^#{1,4} )" % sSource, sText + "\n#### end\n")
    if not oPart:
        log("EXAMPLES: help\\DbDo.md has no Searching %s part" % sSource)
        return
    iRun, iBad = 0, 0
    for sTable, sKey, sIndex in (("books", "book_id", "books_fts"), ("updates", "update_id", "updates_fts")):
        lsFields = [r[1] for r in oDb.execute('PRAGMA table_info("%s")' % sTable)]
        lsIndexColumns = [r[1] for r in oDb.execute('PRAGMA table_info("%s")' % sIndex)]
        sPart = oPart.group(1)
        # An example after the words "Searches of In Brief" searches updates; the rest search books.
        iSplit = sPart.find("Searches of In Brief")
        sPart = sPart[iSplit:] if sTable == "updates" and iSplit >= 0 else (sPart[:iSplit] if iSplit >= 0 else sPart)
        if sTable == "updates" and iSplit < 0: continue
        for sExample in re.findall(r"^[ \t]*- (?:\*\*[^*\n]+\*\*[^`\n]*)?`([^`\n]+)`", sPart, re.M):
            iRun += 1
            try:
                sWhere = ('"%s" IN (SELECT rowid FROM "%s" WHERE "%s" MATCH \'%s\')' % (sKey, sIndex, sIndex, sExample.replace("'", "''"))
                          if core.isFullTextSyntax(sExample) else core.patternWhere(sExample, lsFields, sIndex, sKey, lsIndexColumns))
                iHits = oDb.execute('SELECT count(*) FROM "%s" WHERE %s' % (sTable, sWhere)).fetchone()[0]
                log("EXAMPLE %s %s: %s" % (sTable, plural(iHits, "row"), sExample))
                if not iHits: iBad += 1
            except Exception as oError:
                iBad += 1
                log("EXAMPLE REFUSED %s (%s): %s" % (sTable, oError, sExample))
    say("The guide's search examples: %s tried, %d refused or empty; the log gives each count." % (plural(iRun, "example"), iBad))


def writeCoverage(dIssues, dKinds, lRead, sPath):
    """The coverage report: what was found by year, what the pilot read, and annotation sources by year."""
    dYears = collections.defaultdict(list)
    for d in dIssues.values(): dYears[d["date"][0]].append(d)
    lsOut = ["# Talking Book Topics: what cycle 1 found", "", "Written by makeTalkingBookTopics, %s, %s." % (sVersion, datetime.datetime.now().strftime("%d %B %Y %H:%M")), ""]
    lsOut += ["## Issues found, by year", ""]
    for iYear in sorted(dYears):
        lYear = sorted(dYears[iYear], key=lambda d: d["key"])
        lsOut.append("- %d: %s (%s)" % (iYear, plural(len(lYear), "issue"), ", ".join("%s %s" % (issueTitle(d["date"]).rsplit(" ", 1)[0], d["kind"]) for d in lYear)))
    lsOut += ["", "## Kinds of page found", ""] + ["- %s: %d" % (s, dKinds[s]) for s in sorted(dKinds)]
    lsOut += ["", "## The pilot", ""]
    dSourceYears = collections.defaultdict(collections.Counter)
    for d in lRead:
        dSources = collections.Counter(b["annotation_source"] for b in d["books"])
        for sKey, iCount in dSources.items(): dSourceYears[d["date"][0]][sKey] += iCount
        lsOut.append("- %s (%s): %s; without author %d; without narrator %d; annotations kept %d" % (
            issueTitle(d["date"]), d["kind"], plural(len(d["books"]), "book"), sum(1 for b in d["books"] if not b["author"]),
            sum(1 for b in d["books"] if not b["narrator"]), sum(1 for b in d["books"] if b["annotation"])))
    lsOut += ["", "## Who wrote the annotations, by year read", ""]
    for iYear in sorted(dSourceYears):
        lsOut.append("- %d: %s" % (iYear, ", ".join("%s %d" % (s, n) for s, n in dSourceYears[iYear].most_common())))
    with open(sPath, "w", encoding="utf-8-sig", newline="\r\n") as f: f.write("\n".join(lsOut) + "\n")
    return True


def pickPilot(lIssues, iCount):
    """The newest half of the pilot, then issues spread evenly over the older years."""
    lIssues = sorted(lIssues, key=lambda d: d["key"], reverse=True)
    iNew = iCount // 2
    lPick = lIssues[:iNew]
    lOld = lIssues[iNew:]
    iLeft = iCount - len(lPick)
    if lOld and iLeft > 0:
        nStep = max(1.0, len(lOld) / float(iLeft))
        lPick += [lOld[int(i * nStep)] for i in range(min(iLeft, len(lOld)))]
    return lPick


def main(lsArgs):
    lsLower = [s.lower() for s in lsArgs]
    # --due (the build's): nothing to do, and nothing said, when the template is newer than this script. --pilot-due, the
    # first cycles' word for it, is taken the same way, so an older build.cmd still works.
    if ("--due" in lsLower or "--pilot-due" in lsLower) and os.path.exists(sTemplatePath) and os.path.getmtime(sTemplatePath) >= os.path.getmtime(os.path.abspath(__file__)):
        return 0
    log("makeTalkingBookTopics.py started; %s; script %s" % (sVersion, os.path.abspath(__file__)))
    log("Python %s at %s on %s; SQLite %s; cwd %s; command line %s" % (platform.python_version(), sys.executable, platform.platform(),
                                                                      sqlite3.sqlite_version, os.getcwd(), " ".join(sys.argv)))
    lsKnown = ("--due", "--limit", "--offline", "--pilot-due")
    lsUnknown = [s for s in lsArgs if s.startswith("--") and s.lower() not in lsKnown]
    if lsUnknown:
        say("Unknown option %s. The options are %s." % (lsUnknown[0], ", ".join(lsKnown)))
        return 2
    # THE WHOLE TEMPLATE unless --limit asks for a pilot (version 3). A pilot is never the template.
    iLimit = 0
    if "--limit" in lsLower:
        iAt = lsLower.index("--limit")
        iLimit = int(lsArgs[iAt + 1]) if iAt + 1 < len(lsArgs) and lsArgs[iAt + 1].isdigit() else iPilot
    sOut = sPilotPath if iLimit else sTemplatePath
    bOffline = "--offline" in lsLower
    log("settings: %s; offline %s; delay %.1f s; data %s; out %s" % ("pilot of %d issues" % iLimit if iLimit else "every issue", bOffline, nDelay, sDataDir, sOut))
    say("Talking Book Topics: finding the issues, then reading %s. %s" % ("%d of them" % iLimit if iLimit else "all of them",
        "A few minutes." if iLimit else "The first time takes about ten minutes; saved pages are not asked for again."))
    # PANDOC FOR IN BRIEF (version 5): the kit's, found or installed as makeBrailleForum finds it. Without it, the web
    # issues' In Brief articles are left out and the log says so; the books are built all the same.
    global sPandoc
    sPandoc = core.pandocPath()
    log("PANDOC %s" % (sPandoc or "not found: web issues' In Brief articles are left out"))
    oCache = core.homerFetch.Cache(sDataDir, fLog=log, nDelay=nDelay, iTimeout=60, iRetries=3, sUserAgent=sUserAgent, bOffline=bOffline)
    dIssues, dKinds = discoverIssues(oCache, bOffline)
    oCache.save()
    if not dIssues:
        say("No issue was found, so nothing was built. The log says why: " + sLogPath)
        return 1
    lsYears = sorted(set(d["date"][0] for d in dIssues.values()))
    say("Found %s, %d to %d." % (plural(len(dIssues), "issue"), lsYears[0], lsYears[-1]))
    lPick = pickPilot(list(dIssues.values()), iLimit) if iLimit else sorted(dIssues.values(), key=lambda d: d["key"], reverse=True)
    lRead = []
    for iAt, dIssue in enumerate(lPick, 1):
        sTitle = issueTitle(dIssue["date"])
        core.sHeartbeat = "Issue %d of %d, %s, fetching." % (iAt, len(lPick), sTitle)
        # Archived pages never change: a kept copy is used without asking the archive again.
        oCache.bTrustKept = "web.archive.org" in dIssue["fetch"]
        dIssue["books"] = readIssue(oCache, dIssue, 2 if iAt <= 4 or iAt % 12 == 0 else 0)
        oCache.bTrustKept = False
        core.sHeartbeat = ""
        say("Issue %d of %d, %s: %s." % (iAt, len(lPick), sTitle, plural(len(dIssue["books"]), "book")))
        if dIssue["books"]: lRead.append(dIssue)
        if iAt % 6 == 0: oCache.save()
    oCache.save()
    writeCoverage(dIssues, dKinds, lRead, sCoveragePath)
    if not lRead:
        say("No book could be read from the pilot's issues. The log and %s say why." % sCoveragePath)
        return 1
    iBooks, sFts = buildDatabase(lRead, sOut)
    say("%s built: %s from %s, %s, %s. Coverage: %s" % ("Pilot" if iLimit else "Template", plural(iBooks, "book"), plural(len(lRead), "issue"),
        "searchable" if sFts else "no full-text index", "{:,} KB".format(os.path.getsize(sOut) // 1024), sCoveragePath))
    say("Log: " + sLogPath)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except Exception as oError:
        log("CRASH " + traceback.format_exc())
        print("makeTalkingBookTopics stopped: %s. The log is %s." % (oError, sLogPath))
        sys.exit(1)
