r"""makeBrailleForum.py -- builds DbDo's BrailleForum template, the articles of the American Council of the Blind's
magazine, The ACB Braille Forum, with its sister the ACB E-Forum, searchable by full text.

    scripts\makeBrailleForum                 gathers what changed at acb.org and builds the database
    scripts\makeBrailleForum --limit 12      a pilot of the newest 12 issues, data\BrailleForum\BrailleForum-pilot.db
    scripts\makeBrailleForum --offline       builds from the copies already kept, asking acb.org for nothing
    scripts\makeBrailleForum --refresh       asks acb.org again about every issue, however old
    scripts\makeBrailleForum --porter        stems English words in the index (describe finds described), to compare
    scripts\makeBrailleForum --repair        mends the template in place, as the build runs it, fetching nothing

WHERE THE ISSUES COME FROM. Two pages at acb.org list them: the past issues (through 2023) and the current issues
(2024 on). Each leads, through pages of years, to one page per issue. An issue's page offers its plain text (.txt),
large-print Word (.doc) and braille (.brf) files, and for most issues a table of contents whose every entry is the
article's own web page.

TWO SOURCES, TWO FIELDS (25 November 2026, Jamal's ruling). An article's words are kept in TEXTMARKDOWN only when the
source itself has structure; no heading, list, link or bold is ever inferred from plain text.
  body    TEXTMARKDOWN: the article's web page, converted by Pandoc. Its headings, lists, links and bold are ACB's own.
  text    TEXTMEMO: the article's words exactly as the issue's .txt file has them, used only when there is no web
          page for it -- an older issue, or a section the issue page does not link, such as ACB Officers.
format says which: "web page" or "plain text". A row has one or the other, never both.

HOW THE .txt IS DIVIDED. ACB's text files mark each section with a line of five asterisks, and a section's title with
asterisks before it ("** Looking Ahead to Jacksonville"). A byline is the next line when it begins "by" or "edited by".
A file without those markers is kept whole, as one row of kind "issue", and Coverage.md names it. The masthead, the
table of contents and the standing notices (ACB Officers, Are You Moving?, Accessing Your ACB Braille and E-Forums and
the like) are kept, with kind saying so, so a search can leave them out: kind = 'article'.

AN ARTICLE KEEPS ITS KEY. article_key, such as bf-2023-11-007, is the issue's key and a number, and it is the prime,
so a person's own copy of the database is updated from the template by it. A rebuild reads the previous template
first and gives each article the key it had: same web page, else same words (fingerprint), else the only article of
the issue with that title. A new article takes the issue's next number, which is kept on the issue row (next_number)
and never reused. Coverage.md lists every article that is new, gone, or could not be matched for certain.

THE FULL-TEXT INDEX, articles_fts, is the kit's homerFts: it indexes title, author, body and text, and keeps itself
current through triggers. The saved searches beside the template, and the template's Searches.md, show how to use it.

WHAT IS KEPT WHERE. The fetched pages and files are in data\BrailleForum (the kit's homerFetch cache), so a rebuild
asks acb.org only "has it changed?", and an issue older than four months is not asked about at all unless --refresh.
templates\BrailleForum\BrailleForum.db is built beside itself as BrailleForum.db.building, checked, and only then
put in place. Both stay on this computer: LocalFiles.txt keeps them off GitHub, and the installer leaves the
BrailleForum template out. ACB's words are ACB's.

THE LOG, logs\DbDo-makeBrailleForum-<date>-<time>.log, names every page fetched or read from the cache, every issue
and how it was divided, and every key decision. The console says one line a stage.
"""

import collections, datetime, hashlib, html, html.parser, io, os, platform, re, sqlite3, subprocess, sys, traceback, urllib.parse

sHere = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, sHere)
sKit = os.environ.get("HomerDev") or r"C:\HomerDev"
for sPath in (os.path.join(sKit, "scripts"), os.path.join(sKit, "exec", "Python")):
    if os.path.isdir(sPath) and sPath not in sys.path: sys.path.append(sPath)
import homerFetch, homerFts
import makeMusicTrail as trail

sVersion = "2026-11-25 version 1: web pages as Markdown, plain text otherwise, keys kept across rebuilds"
sSource = "BrailleForum"
sUserAgent = "DbDo makeBrailleForum/1 (+https://github.com/JamalMazrui/DbDo; a searchable archive of the ACB Braille Forum)"
lsEntryPages = ["https://www.acb.org/acb-braille-forum-and-acb-e-forum", "https://www.acb.org/acb-braille-forum-past-issues"]
iTrustDays = 120          # an issue older than this is read from the cache without asking acb.org, unless --refresh
nDelay = 1.0
iTimeout = 60
iRetries = 3

dMonths = {s: i for i, s in enumerate(["january", "february", "march", "april", "may", "june", "july", "august", "september",
                                        "october", "november", "december"], 1)}
sMonthPattern = "|".join(dMonths)
# THE STANDING NOTICES: sections every issue repeats. Matched on the start of the title, case and punctuation aside.
lsBoilerplate = ["accessing your acb braille", "acb officers", "acb board of directors", "acb board of publications",
                 "are you moving", "be a part of acb", "how to submit articles", "upcoming forum themes", "in memoriam dedication"]
dArticleTypes = {"article_key": "TEXTLINE", "author": "TEXTLINE", "body": "TEXTMARKDOWN", "fingerprint": "TEXTLINE", "format": "TEXTLINE",
                 "issue_id": "INTEGER", "kind": "TEXTLINE", "position": "INTEGER", "published": "TEXTLINE", "text": "TEXTMEMO",
                 "title": "TEXTLINE", "words": "INTEGER"}
dIssueTypes = {"articles": "INTEGER", "edition": "TEXTLINE", "issue_key": "TEXTLINE", "next_number": "INTEGER", "number": "TEXTLINE",
               "published": "TEXTLINE", "published_end": "TEXTLINE", "text_url": "TEXTLINE", "title": "TEXTLINE", "volume": "TEXTLINE",
               "words": "INTEGER"}
lsArticleFields = ["title", "author", "published", "issue_id", "position", "kind", "format", "words", "body", "text", "article_key", "fingerprint"]
lsIssueFields = ["title", "edition", "published", "published_end", "volume", "number", "articles", "words", "issue_key", "next_number", "text_url"]
dKinds = {"article": "An article, column or letter: what a search usually wants.",
          "boilerplate": "A standing notice every issue repeats, such as ACB Officers or Are You Moving?",
          "issue": "A whole issue whose text file has no section markers, so it could not be divided.",
          "masthead": "The issue's opening: its title, volume, number and publication notes.",
          "table of contents": "The issue's own list of what is in it."}
dFormats = {"plain text": "From the issue's .txt file, exactly as ACB wrote it, in the text field.",
            "web page": "From the article's own web page at acb.org, converted to Markdown, in the body field."}

sProject = os.path.dirname(sHere)
sTemplate = os.path.join(sProject, "templates", "BrailleForum")
sTarget = os.path.join(sTemplate, "BrailleForum.db")
sLogPath = os.path.join(sProject, "logs", "DbDo-makeBrailleForum-%s.log" % datetime.datetime.now().strftime("%Y%m%d-%H%M%S"))
trail.sLogPath = sLogPath
dReport = collections.defaultdict(list)


def log(sText): return trail.log(sText)
def say(sText): return trail.say(sText)


def plural(iCount, sOne, sMany=""):
    return "%s %s" % ("{:,}".format(iCount), sOne if iCount == 1 else (sMany or sOne + "s"))


# ---------------------------------------------------------------- reading pages

class linkParser(html.parser.HTMLParser):
    """Every link of a page as (href, text, the heading it falls under), and the first h1."""

    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.lLinks, self.sHeading, self.sH1, self.iHeading, self.sHeadingText = [], "", "", 0, ""
        self.lOpen = []

    def handle_starttag(self, sTag, lAttributes):
        if sTag in ("h1", "h2", "h3", "h4"):
            self.iHeading, self.sHeadingText = int(sTag[1]), ""
        if sTag == "a": self.lOpen.append([dict(lAttributes).get("href", ""), ""])
        return True

    def handle_endtag(self, sTag):
        if sTag in ("h1", "h2", "h3", "h4") and self.iHeading:
            sText = re.sub(r"\s+", " ", self.sHeadingText).strip()
            if self.iHeading == 1 and not self.sH1: self.sH1 = sText
            if self.iHeading <= 3: self.sHeading = sText
            self.iHeading = 0
        if sTag == "a" and self.lOpen:
            sHref, sText = self.lOpen.pop()
            self.lLinks.append((sHref, re.sub(r"\s+", " ", sText).strip(), self.sHeading))
        return True

    def handle_data(self, sText):
        if self.iHeading: self.sHeadingText += sText
        for lLink in self.lOpen: lLink[1] += sText
        return True


def readLinks(sHtml, sBase):
    oParser = linkParser()
    oParser.feed(sHtml)
    return [(urllib.parse.urljoin(sBase, sHref).split("#")[0], sText, sHeading) for sHref, sText, sHeading in oParser.lLinks if sHref], oParser.sH1


def decoded(binBody):
    """Text from bytes: UTF-8 when it is, Windows-1252 otherwise, as older ACB files are."""
    if binBody is None: return ""
    for sEncoding in ("utf-8-sig", "cp1252"):
        try: return binBody.decode(sEncoding)
        except UnicodeDecodeError: continue
    return binBody.decode("latin-1")


def issueOf(sText, sHref):
    """(edition, year, first month, last month) when a link names an issue, else None."""
    sLow = sText.lower()
    oDate = re.search(r"(%s)(?:\s*[-/\u2013]\s*(%s))?,?\s+((?:19|20)\d\d)" % (sMonthPattern, sMonthPattern), sLow)
    oFile = re.search(r"/(bf|ef)(\d\d)(\d{4})(?:_\d+)?\.txt$", sHref.lower()) or re.search(r"/(bf|ef)(\d\d)(\d\d)$", sHref.lower())
    if oDate and ("forum" in sLow or re.search(r"\b(bf|ef)\b", sLow)):
        sEdition = "ef" if ("e-forum" in sLow or re.search(r"\bef\b", sLow)) else "bf"
        iFirst = dMonths[oDate.group(1)]
        return sEdition, int(oDate.group(3)), iFirst, dMonths[oDate.group(2)] if oDate.group(2) else iFirst
    if oFile:
        iYear = int(oFile.group(3)) if len(oFile.group(3)) == 4 else 2000 + int(oFile.group(2))
        iMonth = int(oFile.group(2)) if len(oFile.group(3)) == 4 else int(oFile.group(3))
        if 1 <= iMonth <= 12: return oFile.group(1), iYear, iMonth, iMonth
    return None


def isListingPage(sUrl, sText):
    """A page of years or of a range of years, as the entry pages link them."""
    sLow = (sText + " " + sUrl).lower()
    return "acb.org" in sUrl.lower() and "issues" in sLow and re.search(r"(19|20)\d\d", sLow) is not None and not sUrl.lower().endswith((".txt", ".doc", ".brf", ".zip"))


def discoverIssues(oCache):
    """Every issue the entry pages lead to: {issue_key: issue}."""
    dIssues, setSeen = {}, set()
    lQueue = [(s, 0) for s in lsEntryPages]
    while lQueue:
        sUrl, iDepth = lQueue.pop(0)
        if sUrl in setSeen: continue
        setSeen.add(sUrl)
        dResult = oCache.fetch(sUrl)
        if dResult["body"] is None:
            dReport["failed"].append("%s (%s)" % (sUrl, dResult["error"]))
            continue
        lLinks, sH1 = readLinks(decoded(dResult["body"]), sUrl)
        for sHref, sText, sHeading in lLinks:
            tIssue = issueOf(sText, sHref)
            if tIssue and "acb.org" in sHref.lower():
                sEdition, iYear, iFirst, iLast = tIssue
                sKey = "%s-%04d-%02d" % (sEdition, iYear, iFirst)
                dOld = dIssues.get(sKey)
                if dOld and not (dOld["page"].endswith(".txt") and not sHref.lower().endswith(".txt")): continue
                dIssues[sKey] = {"issue_key": sKey, "edition": "ACB E-Forum" if sEdition == "ef" else "ACB Braille Forum", "year": iYear,
                                 "published": "%04d-%02d" % (iYear, iFirst), "published_end": "%04d-%02d" % (iYear, iLast) if iLast != iFirst else "",
                                 "page": sHref, "link_text": sText}
            elif iDepth < 3 and isListingPage(sHref, sText) and sHref not in setSeen:
                lQueue.append((sHref, iDepth + 1))
    log("DISCOVERED %s from %s" % (plural(len(dIssues), "issue"), plural(len(setSeen), "listing page")))
    return dIssues


def issueTitle(dIssue):
    iYear, iFirst = int(dIssue["published"][:4]), int(dIssue["published"][5:7])
    sMonths = datetime.date(iYear, iFirst, 1).strftime("%B")
    if dIssue["published_end"]: sMonths += "-" + datetime.date(iYear, int(dIssue["published_end"][5:7]), 1).strftime("%B")
    return "%s, %s %d" % (dIssue["edition"], sMonths, iYear)


def tableOfContents(lLinks):
    """The article pages an issue page lists under its Table of Contents heading."""
    lArticles = []
    for sHref, sText, sHeading in lLinks:
        if "table of contents" not in sHeading.lower(): continue
        sLow = sHref.lower()
        if "acb.org" not in sLow or sLow.endswith((".txt", ".doc", ".docx", ".brf", ".zip", ".mp3", ".pdf")) or not sText: continue
        if sHref not in [s for s, t in lArticles]: lArticles.append((sHref, sText))
    return lArticles


def textFileOf(lLinks):
    for sHref, sText, sHeading in lLinks:
        if re.search(r"/sites/default/files/[^/]+\.txt$", sHref, re.I): return sHref
    return ""


# ---------------------------------------------------------------- an article's web page

def balanced(sHtml, iStart, sTag):
    """The element that opens at iStart, its nesting balanced."""
    iDepth = 0
    for oToken in re.finditer(r"<(/?)%s\b[^>]*>" % sTag, sHtml[iStart:], re.I):
        iDepth = iDepth - 1 if oToken.group(1) else iDepth + 1
        if iDepth == 0: return sHtml[iStart: iStart + oToken.end()]
    return sHtml[iStart:]


def articleParts(sHtml):
    """(title, body markup, how it was found) from an article page. The body is the page's body field, as Drupal marks
    it; failing that, the <article> element; failing that, what lies between the title and "Need to reach us?"."""
    oH1 = re.search(r"<h1\b[^>]*>(.*?)</h1>", sHtml, re.I | re.S)
    sTitle = re.sub(r"\s+", " ", html.unescape(re.sub(r"<[^>]+>", " ", oH1.group(1)))).strip() if oH1 else ""
    for sPattern, sHow in ((r"<div\b[^>]*class=\"[^\"]*\bfield--name-body\b", "the body field"),
                           (r"<div\b[^>]*class=\"[^\"]*\bfield-name-body\b", "the body field"),
                           (r"<article\b", "the article element")):
        oMatch = re.search(sPattern, sHtml, re.I)
        if oMatch:
            sTag = "div" if sPattern.startswith("<div") else "article"
            sBody = balanced(sHtml, oMatch.start(), sTag)
            if sTag == "article" and oH1 and oH1.start() > oMatch.start(): sBody = sBody.replace(oH1.group(0), "", 1)
            return sTitle, sBody, sHow
    iFrom = oH1.end() if oH1 else 0
    oEnd = re.search(r"<h2\b[^>]*>\s*Need to reach us", sHtml[iFrom:], re.I)
    return sTitle, sHtml[iFrom: iFrom + oEnd.start()] if oEnd else "", "the text between the title and Need to reach us?"


def pandocPath():
    """The newest Pandoc on the machine; the kit's installPandoc is run once when there is none."""
    try:
        import media
        sPandoc = media.pandocProgram()
        log(media.searchLog())
    except Exception as oError:
        log("media.py not usable (%s); looking for pandoc on the PATH" % oError)
        sPandoc = ""
        for sFolder in os.environ.get("PATH", "").split(os.pathsep):
            for sName in ("pandoc.exe", "pandoc"):
                if os.path.isfile(os.path.join(sFolder, sName)): sPandoc = sPandoc or os.path.join(sFolder, sName)
    if sPandoc: return sPandoc
    sInstall = os.path.join(sKit, "scripts", "installPandoc.cmd")
    if os.name == "nt" and os.path.isfile(sInstall):
        say("Pandoc is needed to convert the article pages to Markdown, so the kit's installPandoc is being run.")
        oRun = subprocess.run(["cmd", "/c", sInstall], env=dict(os.environ, noPause="1"))
        log("RUN %s: exit %d" % (sInstall, oRun.returncode))
        return pandocPath() if oRun.returncode == 0 else ""
    return ""


def toMarkdown(sPandoc, lsFragments):
    """Each fragment of markup as Pandoc's Markdown, in one run of Pandoc. Only what the markup holds is written:
    raw HTML, classes and spans are dropped, and lines are not wrapped."""
    if not lsFragments: return []
    sJoined = "".join("<p>HOMERSPLIT%04d</p>\n%s\n" % (i, s) for i, s in enumerate(lsFragments))
    sFormat = "markdown-raw_html-native_divs-native_spans-fenced_divs-bracketed_spans-link_attributes-header_attributes-raw_attribute"
    oRun = subprocess.run([sPandoc, "-f", "html", "-t", sFormat, "--wrap=none"], input=sJoined.encode("utf-8"), capture_output=True)
    if oRun.returncode != 0: raise RuntimeError("Pandoc exit %d: %s" % (oRun.returncode, oRun.stderr.decode("utf-8", "replace")[:300]))
    lsParts = re.split(r"(?m)^HOMERSPLIT\d{4}\s*$", oRun.stdout.decode("utf-8").replace("\r\n", "\n"))[1:]
    if len(lsParts) != len(lsFragments): raise RuntimeError("Pandoc returned %d parts for %d articles" % (len(lsParts), len(lsFragments)))
    return [re.sub(r"\n{3,}", "\n\n", s).strip() for s in lsParts]


# ---------------------------------------------------------------- an issue's .txt

def plainTitle(sLine):
    return re.sub(r"\s+", " ", sLine.strip().lstrip("*").strip())


def bylineOf(lsLines):
    """The author when the first line of a section's text is a byline."""
    for sLine in lsLines:
        if not sLine.strip(): continue
        oBy = re.match(r"(?i)\s*(?:edited\s+)?by\s+(.+?)\s*$", sLine)
        return oBy.group(1).strip().rstrip(",.") if oBy and len(oBy.group(1)) < 120 else ""
    return ""


def splitText(sText):
    """The sections of an issue's .txt as [(title, author, text)], and how it was divided."""
    sText = sText.replace("\r\n", "\n").replace("\r", "\n")
    lsChunks = re.split(r"(?m)^\s*\*{5,}\s*$", sText)
    sHow = "five-asterisk dividers"
    if len(lsChunks) < 3:
        lsChunks = re.split(r"(?m)^(?=\*\*\s*\S)", sText)
        sHow = "two-asterisk titles"
    if len(lsChunks) < 3: return [("", "", sText.strip())], "not divided: no section markers"
    lSections = []
    for sChunk in lsChunks:
        lsLines = sChunk.strip("\n").split("\n")
        while lsLines and not lsLines[0].strip(): lsLines.pop(0)
        if not lsLines: continue
        sTitle = ""
        if lsLines[0].lstrip().startswith("*"):
            sTitle = plainTitle(lsLines.pop(0))
            if not sTitle:   # an empty marker, with the title on the next line
                while lsLines and not lsLines[0].strip(): lsLines.pop(0)
                sTitle = plainTitle(lsLines.pop(0)) if lsLines else ""
            # A TITLE THAT ENDS IN A COLON goes on to the next line ("Audio Description on the Hill:" / "Why the CVTA Matters").
            if sTitle.endswith(":") and lsLines and lsLines[0].strip() and not re.match(r"(?i)\s*(edited\s+)?by\s", lsLines[0]):
                sTitle += " " + plainTitle(lsLines.pop(0))
        sBody = "\n".join(lsLines).strip()
        if not sTitle and not sBody: continue
        lSections.append((sTitle, bylineOf(lsLines), sBody))
    return lSections, sHow


def kindOf(sTitle, iIndex, bBeforeContents):
    sKey = titleKey(sTitle)
    if sKey == "table of contents": return "table of contents"
    if any(sKey.startswith(s) for s in lsBoilerplate): return "boilerplate"
    if bBeforeContents and iIndex == 0: return "masthead"
    return "article"


def titleKey(sTitle):
    """A title as compared: case, punctuation and a leading The set aside."""
    sKey = html.unescape(sTitle or "").lower().replace("\u2019", "'")
    sKey = re.sub(r"[^a-z0-9 ]+", " ", sKey)
    return re.sub(r"\s+", " ", re.sub(r"^the\s+", "", sKey.strip())).strip()


def wordCount(sText):
    return len(re.findall(r"\w+", sText or ""))


def fingerprint(sText):
    """The words of an article, case and markup aside, as a short hash: the same article from either source matches."""
    sPlain = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", sText or "")
    lsWords = re.findall(r"[a-z0-9]+", sPlain.lower())
    return hashlib.sha1(" ".join(lsWords).encode("utf-8")).hexdigest()[:16] if lsWords else ""


# ---------------------------------------------------------------- one issue

def readIssue(oCache, dIssue, sPandoc, bTrust):
    """The issue's articles as a list of dictionaries, in the issue's order."""
    oCache.bTrustKept = bTrust
    lArticles, lPages, sTextUrl = [], [], ""
    if dIssue["page"].lower().endswith(".txt"):
        sTextUrl = dIssue["page"]
    else:
        dPage = oCache.fetch(dIssue["page"])
        if dPage["body"] is None:
            dReport["failed"].append("%s: issue page %s (%s)" % (dIssue["issue_key"], dIssue["page"], dPage["error"]))
            return []
        lLinks, sH1 = readLinks(decoded(dPage["body"]), dIssue["page"])
        sTextUrl = textFileOf(lLinks)
        lPages = tableOfContents(lLinks)
    dIssue["text_url"] = sTextUrl
    # The web pages, converted together.
    lsFragments, lWeb = [], []
    for sHref, sText in lPages:
        dArticle = oCache.fetch(sHref)
        if dArticle["body"] is None:
            dReport["failed"].append("%s: article page %s (%s)" % (dIssue["issue_key"], sHref, dArticle["error"]))
            continue
        sTitle, sBody, sHow = articleParts(decoded(dArticle["body"]))
        if not sBody.strip():
            dReport["unread"].append("%s: %s held no article body" % (dIssue["issue_key"], sHref))
            continue
        log("PAGE %s: %s, body from %s" % (sHref, sTitle or sText, sHow))
        oBy = re.search(r",\s*(?:edited\s+)?by\s+(.+)$", sText, re.I)
        lWeb.append({"title": sTitle or re.sub(r",\s*(edited\s+)?by\s+.+$", "", sText, flags=re.I), "author": oBy.group(1).strip() if oBy else "", "url": sHref})
        lsFragments.append(sBody)
    if lWeb:
        if not sPandoc: raise RuntimeError("Pandoc is needed to convert the article pages, and none could be found or installed")
        for dWeb, sMarkdown in zip(lWeb, toMarkdown(sPandoc, lsFragments)):
            dWeb.update({"format": "web page", "body": sMarkdown, "text": None})
            if not dWeb["author"]:
                oBy = re.match(r"(?i)\**\s*(?:edited\s+)?by\s+([^*\n]+)", sMarkdown)
                if oBy: dWeb["author"] = oBy.group(1).strip().rstrip(",.")
    # The .txt: the issue's volume and number, and every section no web page holds.
    lSections, sHow = [], "no text file"
    if sTextUrl:
        dText = oCache.fetch(sTextUrl)
        if dText["body"] is None: dReport["failed"].append("%s: text file %s (%s)" % (dIssue["issue_key"], sTextUrl, dText["error"]))
        else:
            sText = decoded(dText["body"])
            oVolume = re.search(r"Volume\s+([LXVIC]+)\b.*?No\.\s*(\d+)", sText[:3000], re.I | re.S)
            if oVolume: dIssue["volume"], dIssue["number"] = oVolume.group(1).upper(), oVolume.group(2)
            lSections, sHow = splitText(sText)
    if sHow.startswith("not divided") and not lWeb: dReport["undivided"].append("%s (%s)" % (dIssue["issue_key"], sTextUrl))
    log("ISSUE %s: %s, %s; text %s, divided by %s into %s" % (dIssue["issue_key"], issueTitle(dIssue), plural(len(lWeb), "web page"),
        sTextUrl or "none", sHow, plural(len(lSections), "section")))
    # The order is the text file's; a web page its sections do not name goes after them.
    dWebByTitle = collections.defaultdict(list)
    for dWeb in lWeb: dWebByTitle[titleKey(dWeb["title"])].append(dWeb)
    setUsed = set()
    bBeforeContents = any(titleKey(s[0]) == "table of contents" for s in lSections)
    for iIndex, (sTitle, sAuthor, sBody) in enumerate(lSections):
        if titleKey(sTitle) == "table of contents": bBeforeContents = False
        sKey = titleKey(sTitle)
        lMatch = [d for d in dWebByTitle.get(sKey, []) if id(d) not in setUsed] if sKey else []
        if not lMatch and sKey and len(sKey) > 12:
            lMatch = [d for d in lWeb if id(d) not in setUsed and (titleKey(d["title"]).startswith(sKey) or sKey.startswith(titleKey(d["title"])))]
        if lMatch:
            setUsed.add(id(lMatch[0]))
            lArticles.append(dict(lMatch[0], kind=kindOf(sTitle, iIndex, bBeforeContents and iIndex == 0)))
            continue
        if sHow.startswith("not divided"):
            sTitle, sKind = issueTitle(dIssue), "issue"
        else:
            # AN UNTITLED FIRST SECTION IS THE MASTHEAD, with or without a table of contents after it.
            sKind = "masthead" if not sTitle and iIndex == 0 else kindOf(sTitle, iIndex, bBeforeContents)
            sTitle = sTitle or ("Masthead" if sKind == "masthead" else "Untitled section")
        lArticles.append({"title": sTitle, "author": sAuthor, "url": sTextUrl, "format": "plain text", "body": None, "text": sBody, "kind": sKind})
    for dWeb in lWeb:
        if id(dWeb) not in setUsed: lArticles.append(dict(dWeb, kind=kindOf(dWeb["title"], 99, False)))
    for iPosition, dArticle in enumerate(lArticles, 1):
        sWords = dArticle["body"] if dArticle["body"] is not None else dArticle["text"]
        dArticle.update({"position": iPosition, "words": wordCount(sWords), "fingerprint": fingerprint(sWords), "published": dIssue["published"]})
    return lArticles


# ---------------------------------------------------------------- keys kept across rebuilds

def previousKeys(sPath):
    """What the template already holds: {issue_key: {"next": n, "articles": [(key, url, fingerprint, titleKey)]}}."""
    dOld = {}
    if not os.path.isfile(sPath): return dOld
    try:
        oDb = sqlite3.connect("file:" + sPath.replace("\\", "/") + "?mode=ro", uri=True)
        for sKey, iNext in oDb.execute("SELECT issue_key, next_number FROM issues"): dOld[sKey] = {"next": iNext or 1, "articles": []}
        for sIssue, sArticle, sUrl, sFinger, sTitle in oDb.execute(
                "SELECT i.issue_key, a.article_key, a.url, a.fingerprint, a.title FROM articles a JOIN issues i ON i.issue_id = a.issue_id"):
            dOld.setdefault(sIssue, {"next": 1, "articles": []})["articles"].append((sArticle, sUrl if sUrl and not sUrl.lower().endswith(".txt") else "", sFinger or "", titleKey(sTitle)))
        oDb.close()
        log("PREVIOUS %s: %s, %s" % (sPath, plural(len(dOld), "issue"), plural(sum(len(d["articles"]) for d in dOld.values()), "article")))
    except Exception as oError:
        log("PREVIOUS %s could not be read (%s); every key starts afresh" % (sPath, oError))
        dReport["conflicts"].append("The previous template could not be read (%s), so every article has a new key." % oError)
    return dOld


def assignKeys(dIssue, lArticles, dOld):
    """Gives each article the key it had, else the issue's next number. Returns the issue's next number."""
    dPrior = dOld.get(dIssue["issue_key"], {"next": 1, "articles": []})
    lOld = list(dPrior["articles"])
    iNext = max([dPrior["next"]] + [int(t[0].rsplit("-", 1)[1]) + 1 for t in lOld if re.search(r"-\d+$", t[0])])
    setTaken = set()
    for sRule, iField, fValue in (("same page", 1, lambda d: d["url"] if d["format"] == "web page" else ""),
                                  ("same words", 2, lambda d: d["fingerprint"]),
                                  ("same title", 3, lambda d: titleKey(d["title"]))):
        for dArticle in lArticles:
            if dArticle.get("article_key"): continue
            sValue = fValue(dArticle)
            if not sValue: continue
            lCandidates = [t for t in lOld if t[iField] == sValue and t[0] not in setTaken]
            lRivals = [d for d in lArticles if not d.get("article_key") and fValue(d) == sValue]
            if len(lCandidates) == 1 and len(lRivals) == 1:
                dArticle["article_key"] = lCandidates[0][0]
                setTaken.add(lCandidates[0][0])
            elif len(lCandidates) > 1 or (lCandidates and len(lRivals) > 1):
                dReport["conflicts"].append("%s: %s matches %s by %s, so it was not matched that way" % (dIssue["issue_key"], dArticle["title"],
                                            plural(len(lCandidates), "earlier article"), sRule))
    for dArticle in lArticles:
        if dArticle.get("article_key"): continue
        dArticle["article_key"] = "%s-%03d" % (dIssue["issue_key"], iNext)
        iNext += 1
        if lOld: dReport["new"].append("%s: %s (%s)" % (dIssue["issue_key"], dArticle["title"], dArticle["article_key"]))
    for t in lOld:
        if t[0] not in setTaken: dReport["gone"].append("%s: %s, no longer found" % (t[0], t[3]))
    return iNext


# ---------------------------------------------------------------- the database

def build(lIssues, sPorter, sOut):
    sTemp = sOut + ".building"
    for sPath in (sTemp, sTemp + "-journal"):
        if os.path.exists(sPath): os.remove(sPath)
    os.makedirs(os.path.dirname(sOut), exist_ok=True)
    oDb = sqlite3.connect(sTemp, isolation_level=None)
    if not homerFts.available(oDb): raise RuntimeError("this Python's SQLite has no FTS5, so the full-text index cannot be built")
    oDb.execute("PRAGMA journal_mode = DELETE")
    oDb.execute("BEGIN")
    # The views table was retired on 4 November 2026, so it is left out, as makeDescribedFilms does; the kit's checkDb
    # failed the first push on it.
    for sStatement in [s for s in trail.systemDdl() if not s.startswith("CREATE TABLE views")]: oDb.execute(sStatement)
    for sStatement in trail.tableDdl("issues", "issue_id", lsIssueFields, ["title"], ["issue_key"], dIssueTypes): oDb.execute(sStatement)
    for sStatement in trail.tableDdl("articles", "article_id", lsArticleFields, ["title", "published"], ["article_key"], dArticleTypes): oDb.execute(sStatement)
    oDb.execute("CREATE INDEX idx_articles_issue_id ON articles (issue_id)")
    trail.addLookups(oDb, "articles", "kind", dKinds, sSource)
    trail.addLookups(oDb, "articles", "format", dFormats, sSource)
    trail.addLookups(oDb, "issues", "edition", {"ACB Braille Forum": "The magazine in braille, large print, cartridge, email and on the web.",
                                                "ACB E-Forum": "The magazine's electronic edition, which alternated months with the Braille Forum."}, sSource)
    iArticles = 0
    for dIssue in lIssues:
        lArticles = dIssue["lArticles"]
        oCursor = oDb.execute("INSERT INTO issues (title, edition, published, published_end, volume, number, articles, words, issue_key, next_number, text_url, url) "
                              "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                              (issueTitle(dIssue), dIssue["edition"], dIssue["published"], dIssue["published_end"] or None, dIssue.get("volume"),
                               dIssue.get("number"), len(lArticles), sum(d["words"] for d in lArticles), dIssue["issue_key"], dIssue["next_number"],
                               dIssue.get("text_url") or None, None if dIssue["page"].lower().endswith(".txt") else dIssue["page"]))
        iIssue = oCursor.lastrowid
        for d in lArticles:
            oDb.execute("INSERT INTO articles (title, author, published, issue_id, position, kind, format, words, body, text, article_key, fingerprint, url) "
                        "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                        (d["title"], d["author"] or None, d["published"], iIssue, d["position"], d["kind"], d["format"], d["words"], d["body"], d["text"],
                         d["article_key"], d["fingerprint"] or None, d["url"] or None))
            iArticles += 1
    sTokenize = ("porter " if sPorter else "") + homerFts.sDefaultTokenize
    homerFts.createIndex(oDb, "articles", "article_id", ["title", "author", "body", "text"], "articles_fts", sTokenize)
    oDb.execute("COMMIT")
    homerFts.optimize(oDb, "articles_fts")
    oDb.execute("ANALYZE")
    oDb.close()
    # DbDo'S OWN REPAIRS BEFORE THE CHECKS (25 November 2026): the build's checkTemplates --repair would otherwise rewrite
    # the whole file after every rebuild, to put defs in its order and add its lookup index. Done here, once, on the copy.
    import checkTemplates as templateCheck
    templateCheck.sLogPath = sLogPath
    templateCheck.repair("BrailleForum", sTemp)
    oDb = sqlite3.connect(sTemp, isolation_level=None)
    oDb.execute("VACUUM")
    lsProblems = ["DbDo's template repair: " + s for s in templateCheck.lErrors]
    if oDb.execute("PRAGMA integrity_check").fetchone()[0] != "ok": lsProblems.append("SQLite's integrity check failed")
    sFts = homerFts.integrityProblem(oDb, "articles_fts")
    if sFts: lsProblems.append("the full-text index does not match its table: " + sFts)
    iIndexed = oDb.execute("SELECT count(*) FROM articles_fts").fetchone()[0]
    if iIndexed != iArticles: lsProblems.append("the index holds %d rows for %d articles" % (iIndexed, iArticles))
    if not iArticles: lsProblems.append("no article was read")
    log("BUILT %s: %s, %s, tokenizer %s; problems: %s" % (sTemp, plural(len(lIssues), "issue"), plural(iArticles, "article"), sTokenize, "; ".join(lsProblems) or "none"))
    oDb.close()
    if lsProblems: raise RuntimeError("the new database failed its checks, so the old one was kept: " + "; ".join(lsProblems))
    try:
        os.replace(sTemp, sOut)
    except OSError as oError:
        raise RuntimeError("the new database is built, but %s could not be replaced (%s). Close any DbDo window showing the BrailleForum "
                           "template and run scripts\\makeBrailleForum --offline, which rebuilds from the cache in a minute" % (sOut, oError))
    return iArticles


def writeCoverage(lIssues, iArticles, sPorter, sFolder):
    """templates\\BrailleForum\\Coverage.md: what the archive holds and what it lacks, written every build."""
    dYears = collections.OrderedDict()
    for dIssue in sorted(lIssues, key=lambda d: d["published"]):
        dYear = dYears.setdefault(dIssue["published"][:4], collections.Counter())
        dYear["issues"] += 1
        for d in dIssue["lArticles"]:
            dYear["web page" if d["format"] == "web page" else "plain text"] += 1
            if d["kind"] == "article": dYear["articles"] += 1
    lsLines = ["# BrailleForum Coverage", "",
               "Written by scripts\\makeBrailleForum on %s. The template holds %s and %s, indexed with the %s tokenizer." % (
                   datetime.date.today().strftime("%d %B %Y").lstrip("0"), plural(len(lIssues), "issue"), plural(iArticles, "row"),
                   "porter (stemming)" if sPorter else "unicode61"), "",
               "## By year", "",
               "Each line gives the issues, the articles (leaving out mastheads, contents and standing notices), and how many rows came from web pages and from plain text.", ""]
    for sYear, dCount in reversed(list(dYears.items())):
        lsLines.append("- %s: %s, %s; %d from web pages, %d from plain text" % (sYear, plural(dCount["issues"], "issue"), plural(dCount["articles"], "article"),
                                                                                 dCount["web page"], dCount["plain text"]))
    for sKey, sHeading, sNone in (("failed", "Not fetched", "Every page and file was fetched or read from the cache."),
                                  ("undivided", "Kept whole", "Every text file was divided into sections."),
                                  ("unread", "Pages without an article", "Every article page held its article."),
                                  ("conflicts", "Keys not matched for certain", "Every earlier article was matched one to one, or not at all."),
                                  ("new", "New since the last build", "No article is new since the last build, or this is the first."),
                                  ("gone", "No longer found", "Every article of the last build is still here.")):
        lsLines += ["", "## " + sHeading, ""]
        lsLines += ["- " + s for s in dReport[sKey]] if dReport[sKey] else [sNone]
    with io.open(os.path.join(sFolder, "Coverage.md"), "w", encoding="utf-8-sig", newline="\r\n") as fOut: fOut.write("\n".join(lsLines) + "\n")
    return True


def repairTemplate():
    """Mends a template built by an earlier version, in place and fetching nothing, as each build runs it. Each mend is
    logged, and the file is left untouched when there is nothing to mend.
      - The views table, retired on 4 November 2026, which version 1 still made: the kit's checkDb failed the first
        push on it (25 November 2026)."""
    if not os.path.isfile(sTarget):
        say("There is no BrailleForum.db to repair.")
        return 0
    oDb = sqlite3.connect(sTarget, isolation_level=None)
    try:
        lsMends = []
        if oDb.execute("SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = 'views'").fetchone()[0]:
            oDb.execute("DROP TABLE views")
            lsMends.append("dropped the retired views table")
        if lsMends:
            oDb.execute("VACUUM")
            sProblem = homerFts.integrityProblem(oDb, "articles_fts")
            if sProblem: raise RuntimeError("the full-text index failed its check after the repair: " + sProblem)
            say("BrailleForum.db repaired: " + "; ".join(lsMends) + ".")
        else:
            log("REPAIR nothing to mend in " + sTarget)
    finally:
        oDb.close()
    return 0


def main(lsArgs):
    log("makeBrailleForum.py started; %s; script %s" % (sVersion, os.path.abspath(__file__)))
    log("Python %s at %s on %s; SQLite %s; cwd %s; command line %s" % (platform.python_version(), sys.executable, platform.platform(), sqlite3.sqlite_version,
                                                                      os.getcwd(), " ".join(sys.argv)))
    lsKnown = ("--limit", "--offline", "--porter", "--refresh", "--repair")
    lsUnknown = [s for s in lsArgs if s.startswith("--") and s.lower() not in lsKnown]
    if lsUnknown:
        say("Unknown option %s. The options are %s." % (lsUnknown[0], ", ".join(lsKnown)))
        return 2
    lsLower = [s.lower() for s in lsArgs]
    iLimit = 0
    if "--limit" in lsLower:
        iAt = lsLower.index("--limit")
        iLimit = int(lsArgs[iAt + 1]) if iAt + 1 < len(lsArgs) and lsArgs[iAt + 1].isdigit() else 0
        if not iLimit:
            say("--limit needs a number of issues after it, such as --limit 12.")
            return 2
    if "--repair" in lsLower: return repairTemplate()
    bOffline, bPorter, bRefresh = "--offline" in lsLower, "--porter" in lsLower, "--refresh" in lsLower
    log("settings: limit %s; offline %s; porter %s; refresh %s; trust kept copies older than %d days; template %s"
        % (iLimit or "none", bOffline, bPorter, bRefresh, iTrustDays, sTarget))
    oCache = homerFetch.Cache(os.path.join(sProject, "data", "BrailleForum"), fLog=log, nDelay=nDelay, iTimeout=iTimeout, iRetries=iRetries,
                              sUserAgent=sUserAgent, bOffline=bOffline)
    say("Finding the issues at acb.org%s." % (" in the cache only" if bOffline else ""))
    dIssues = discoverIssues(oCache)
    if not dIssues:
        oCache.save()
        say("No issue was found, so nothing was built. The log says why: " + sLogPath)
        return 1
    lIssues = sorted(dIssues.values(), key=lambda d: d["published"], reverse=True)
    if iLimit: lIssues = lIssues[:iLimit]
    say("Reading %s, from %s to %s. %s"
        % (plural(len(lIssues), "issue"), lIssues[-1]["published"], lIssues[0]["published"],
           "A pilot takes a few minutes." if iLimit else "The first run fetches every article page, about an hour; later runs ask only about recent issues."))
    sPandoc = pandocPath()
    log("PANDOC %s" % (sPandoc or "none"))
    dOld = previousKeys(sTarget)
    sCutoff = (datetime.date.today() - datetime.timedelta(days=iTrustDays)).strftime("%Y-%m")
    lBuilt = []
    for iAt, dIssue in enumerate(lIssues, 1):
        dIssue["lArticles"] = readIssue(oCache, dIssue, sPandoc, not bRefresh and dIssue["published"] < sCutoff)
        if not dIssue["lArticles"]: continue
        dIssue["next_number"] = assignKeys(dIssue, dIssue["lArticles"], dOld)
        lBuilt.append(dIssue)
        # PROGRESS ABOUT A MINUTE APART (25 November 2026, Jamal asked for more frequent status): every fifth issue, with
        # the one just read and the articles so far, so a long first run never goes quiet for long.
        if iAt % 5 == 0 or iAt == len(lIssues):
            say("%d of %d issues read, through %s; %s so far." % (iAt, len(lIssues), issueTitle(dIssue), plural(sum(len(d["lArticles"]) for d in lBuilt), "row")))
            oCache.save()
    oCache.save()
    if not iLimit:
        for sIssue in sorted(set(dOld) - {d["issue_key"] for d in lBuilt}):
            dReport["gone"].append("%s: the whole issue, no longer found" % sIssue)
    if not lBuilt:
        say("No issue could be read, so nothing was built. The log says why: " + sLogPath)
        return 1
    # A PILOT IS NEVER THE TEMPLATE. --limit builds data\\BrailleForum\\BrailleForum-pilot.db, to try the searches in DbDo;
    # the template, whose keys a person's copy is updated by, is replaced only by a build of every issue.
    sOut = os.path.join(sProject, "data", "BrailleForum", "BrailleForum-pilot.db") if iLimit else sTarget
    iArticles = build(lBuilt, bPorter, sOut)
    writeCoverage(lBuilt, iArticles, bPorter, os.path.dirname(sOut))
    say("Built %s: %s and %s. Coverage.md beside it says what is in it and what is not." % (sOut, plural(len(lBuilt), "issue"), plural(iArticles, "row")))
    if dReport["failed"]: say("%s could not be fetched; the log and Coverage.md name them." % plural(len(dReport["failed"]), "page or file", "pages or files"))
    say("Log: " + sLogPath)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except Exception as oError:
        log("CRASH " + traceback.format_exc())
        print("makeBrailleForum stopped: %s. The log is %s." % (oError, sLogPath))
        sys.exit(1)
