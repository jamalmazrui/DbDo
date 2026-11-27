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
  search_text  TEXTMEMO: for a web page, its words with no markup at all, taken from the page itself; what the
          full-text index reads in place of body (26 November 2026, see THE INDEX READS PLAIN WORDS below).

HOW THE .txt IS DIVIDED. ACB's text files mark each section with a line of five asterisks, and a section's title with
asterisks before it ("** Looking Ahead to Jacksonville"). A byline is the next line when it begins "by" or "edited by".
A file without those markers is kept whole, as one row of kind "issue", and notes\BrailleForum_Coverage.md names it. The masthead, the
table of contents and the standing notices (ACB Officers, Are You Moving?, Accessing Your ACB Braille and E-Forums and
the like) are kept, with kind saying so, so a search can leave them out: kind = 'article'.

AN ARTICLE KEEPS ITS KEY. article_key, such as bf-2023-11-007, is the issue's key and a number. It was the prime until
26 November 2026; the prime is now the month, the issue's title and the article's place in it, which a person can read,
and the copy DbDo opens follows the template whole (FollowTemplate), so no copy is matched to it by key. A rebuild reads the previous template
first and gives each article the key it had: same web page, else same words (fingerprint), else the only article of
the issue with that title. A new article takes the issue's next number, which is kept on the issue row (next_number)
and never reused. notes\BrailleForum_Coverage.md lists every article that is new, gone, or could not be matched for certain.

THE FULL-TEXT INDEX, articles_fts, is the kit's homerFts: it indexes title, writer, search_text and text, and keeps
itself current through triggers.

THE INDEX READS PLAIN WORDS (26 November 2026, Jamal: "if Markdown is used for article content, that could disrupt FTS
searches ... because of Markdown related punctuation marks"). Tested: the tokenizer skips Markdown's punctuation, so
**bold**, ## headings, list dashes and backslash escapes cost no match. But three things did harm: a link's address
was indexed as words (https, www, acb, org and the path's words), so a search for org or acb found every article that
links to acb.org; an address between two words could make a phrase miss, or a phrase inside an address could match;
and the passages snippet gave for a match carried the markup, so a screen reader read star star by Janet Dickelman.
So the index reads search_text, the same words taken from the page with every tag removed, link and picture addresses
dropped and their words kept. body stays Markdown, to read. An edit to body in DbDo leaves search_text as it was, so
the index still finds the words the article had when it was built. The saved searches beside the template, and the BrailleForum section of help\\DbDo.md, show how to use it.

WHAT IS KEPT WHERE. The fetched pages and files are in data\BrailleForum (the kit's homerFetch cache), so a rebuild
asks acb.org only "has it changed?", and an issue older than four months is not asked about at all unless --refresh.
templates\BrailleForum\BrailleForum.db is built beside itself as BrailleForum.db.building, checked, and only then
put in place. Both stay on this computer: LocalFiles.txt keeps them off GitHub, and the installer leaves the
BrailleForum template out. ACB's words are ACB's.

THE LOG, logs\DbDo-makeBrailleForum-<date>-<time>.log, names every page fetched or read from the cache, every issue
and how it was divided, and every key decision. The console says one line a stage.
"""

import collections, datetime, hashlib, html, html.parser, io, os, platform, re, sqlite3, subprocess, sys, time, traceback, urllib.parse

sHere = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, sHere)
sKit = os.environ.get("HomerDev") or r"C:\HomerDev"
for sPath in (os.path.join(sKit, "scripts"), os.path.join(sKit, "exec", "Python")):
    if os.path.isdir(sPath) and sPath not in sys.path: sys.path.append(sPath)
import homerFetch, homerFts
import makeMusicTrail as trail

sVersion = "2026-11-26 version 6: no rebuild for a thin search_text, which was never thin; where body's bulk comes from is logged"
sSource = "BrailleForum"
# ONE BUILDER, TWO MAGAZINES (26 November 2026, Jamal asked for BrailleMonitor.db "structured in an analogous manner").
# makeBrailleMonitor imports this file and sets these names and its own discoverIssues and readIssue, so the database,
# its writers, keys, index, checks, statistics and coverage are made by one piece of code for both magazines.
sSite = "acb.org"
sFirstRun = "The first run fetches every article page, about an hour; later runs ask only about recent issues."
dEditions = {"ACB Braille Forum": "The magazine in braille, large print, cartridge, email and on the web.",
             "ACB E-Forum": "The magazine's electronic edition, which alternated months with the Braille Forum."}
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
dArticleTypes = {"article_key": "TEXTLINE", "writer": "TEXTLINE", "body": "TEXTMARKDOWN", "fingerprint": "TEXTLINE", "format": "TEXTLINE",
                 "issue": "TEXTLINE", "issue_id": "INTEGER", "kind": "TEXTLINE", "position": "INTEGER", "published": "TEXTLINE", "search_text": "TEXTMEMO", "text": "TEXTMEMO",
                 "title": "TEXTLINE", "words": "INTEGER"}
dIssueTypes = {"articles": "INTEGER", "edition": "TEXTLINE", "issue_key": "TEXTLINE", "next_number": "INTEGER", "number": "TEXTLINE",
               "published": "TEXTLINE", "published_end": "TEXTLINE", "text_url": "TEXTLINE", "title": "TEXTLINE", "volume": "TEXTLINE",
               "words": "INTEGER"}
lsArticleFields = ["title", "writer", "published", "issue", "issue_id", "position", "kind", "format", "words", "body", "text", "search_text", "article_key", "fingerprint"]
dWriterTypes = {"articles": "INTEGER", "first_published": "TEXTLINE", "last_published": "TEXTLINE", "name": "TEXTLINE"}
lsWriterFields = ["name", "articles", "first_published", "last_published"]
# WORDS THAT NAME A ROLE, NOT A PERSON: what follows a comma in a byline ("Kim Charlson, ACB President").
setRoleWords = {"acb", "affiliate", "american", "association", "assistant", "blind", "board", "chair", "chairman", "chairperson",
                "chapter", "coordinator", "council", "committee", "director", "editor", "executive", "for", "founder", "manager",
                "member", "officer", "of", "president", "secretary", "staff", "the", "treasurer", "vice"}
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
dPlain = {}  # characters of search_text from Pandoc and from the tag stripper, compared in the log (version 5)


# A HEARTBEAT ON THE CONSOLE (26 November 2026). Progress came every fifth issue, but recent issues are checked with the
# site one page a second, so the first five took a silent minute, and a rebuild looked hung and was stopped. Now, while
# the issues are read, any log line written 20 seconds or more after the last console message also says where the work
# stands, so the console is never quiet for long.
iHeartbeatSeconds = 20
sHeartbeat = ""          # what the long loop is on, set by main; empty outside it
fLastSay = [time.time()]


def say(sText):
    fLastSay[0] = time.time()
    return trail.say(sText)


def shortTitle(dIssue):
    """An issue's title for a status line: without the magazine's main name ("June 2026"), with any other edition's."""
    sTitle = issueTitle(dIssue)
    sMain = next(iter(dEditions), "")
    return sTitle[len(sMain) + 2:] if sMain and sTitle.startswith(sMain + ", ") else sTitle


def log(sText):
    oResult = trail.log(sText)
    if sHeartbeat and time.time() - fLastSay[0] >= iHeartbeatSeconds: say(sHeartbeat)
    return oResult


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


class plainParser(html.parser.HTMLParser):
    """The words of a piece of HTML, with no markup: block elements end a paragraph, a line break ends a line, a
    picture gives its alt text, and nothing of an address is kept."""
    lsBlocks = {"address", "article", "blockquote", "dd", "div", "dl", "dt", "figcaption", "figure", "footer", "h1", "h2", "h3", "h4",
                "h5", "h6", "header", "hr", "li", "ol", "p", "pre", "section", "table", "tr", "ul"}

    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.lsParts, self.iSkip = [], 0

    def handle_starttag(self, sTag, lAttributes):
        if sTag in ("script", "style"): self.iSkip += 1
        elif sTag in self.lsBlocks: self.lsParts.append("\n\n")
        elif sTag == "br": self.lsParts.append("\n")
        elif sTag in ("td", "th"): self.lsParts.append(" ")
        elif sTag == "img":
            sAlt = dict(lAttributes).get("alt") or ""
            if sAlt.strip(): self.lsParts.append(" " + sAlt.strip() + " ")
        return True

    def handle_endtag(self, sTag):
        if sTag in ("script", "style") and self.iSkip: self.iSkip -= 1
        elif sTag in self.lsBlocks: self.lsParts.append("\n\n")
        return True

    def handle_data(self, sText):
        if not self.iSkip: self.lsParts.append(sText)
        return True


def htmlToPlain(sHtml):
    """search_text: a web page's words, paragraphs kept apart by a blank line, no markup and no addresses."""
    oParser = plainParser()
    oParser.feed(sHtml)
    oParser.close()
    sText = "".join(oParser.lsParts).replace("\u00a0", " ")
    lsParagraphs = [re.sub(r"[ \t]+", " ", re.sub(r" *\n *", "\n", s)).strip() for s in re.split(r"\n\s*\n", sText)]
    return "\n\n".join(s for s in lsParagraphs if s)


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


def toMarkdown(sPandoc, lsFragments, sFormat=""):
    """Each fragment of markup as Pandoc's Markdown, in one run of Pandoc. Only what the markup holds is written:
    raw HTML, classes and spans are dropped, and lines are not wrapped. sFormat "plain" gives Pandoc's plain text
    instead: the same words with no markup, which is what search_text holds from version 5 on."""
    if not lsFragments: return []
    sJoined = "".join("<p>HOMERSPLIT%04d</p>\n%s\n" % (i, s) for i, s in enumerate(lsFragments))
    sFormat = sFormat or "markdown-raw_html-native_divs-native_spans-fenced_divs-bracketed_spans-link_attributes-header_attributes-raw_attribute"
    oRun = subprocess.run([sPandoc, "-f", "html", "-t", sFormat, "--wrap=none"], input=sJoined.encode("utf-8"), capture_output=True)
    if oRun.returncode != 0: raise RuntimeError("Pandoc exit %d: %s" % (oRun.returncode, oRun.stderr.decode("utf-8", "replace")[:300]))
    lsParts = re.split(r"(?m)^HOMERSPLIT\d{4}\s*$", oRun.stdout.decode("utf-8").replace("\r\n", "\n"))[1:]
    if len(lsParts) != len(lsFragments):
        # A FRAGMENT THAT LEAVES A TAG OPEN (26 November 2026). The Monitor's one-page issues of the 1960s divide at
        # headings that can fall inside a list or table, so a fragment may end with it still open, and the next
        # fragment's marker lands inside it, off the start of a line: "Pandoc returned 29 parts for 30 articles"
        # stopped the build at May 1968. The fragments are then converted one at a time, which costs a Pandoc run
        # each but cannot run two together.
        log("PANDOC %d parts for %d fragments in one run; converting each fragment alone" % (len(lsParts), len(lsFragments)))
        lsParts = []
        for sFragment in lsFragments:
            oOne = subprocess.run([sPandoc, "-f", "html", "-t", sFormat, "--wrap=none"], input=sFragment.encode("utf-8"), capture_output=True)
            if oOne.returncode != 0: raise RuntimeError("Pandoc exit %d: %s" % (oOne.returncode, oOne.stderr.decode("utf-8", "replace")[:300]))
            lsParts.append(oOne.stdout.decode("utf-8").replace("\r\n", "\n"))
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


def authorNames(sByline):
    """The people a byline names, as written: "Janet Dickelman", "Kim Charlson, ACB President" (the role set aside) or
    "Cynthia Hawkins, Kim Charlson and Dan Spoone" (three). A part that does not look like a person's name -- a role, an
    office, a committee -- is left out, so an author row is always a person."""
    sByline = re.sub(r"(?i)^\s*(?:compiled\s+|edited\s+|written\s+)?by\s+", "", (sByline or "").strip()).strip(" .,;")
    if not sByline: return []
    lsNames = []
    for sPart in re.split(r"\s+and\s+|\s*&\s*|\s*;\s*|\s+with\s+", sByline):
        for sPiece in sPart.split(","):
            sName = re.sub(r"\s+", " ", sPiece).strip(" .:").replace("\u2019", "'")
            lsWords = sName.split(" ")
            if not (2 <= len(lsWords) <= 5) or len(sName) > 50: continue
            if any(sWord.lower().strip(".") in setRoleWords for sWord in lsWords): continue
            # A NAME WITH AN INNER CAPITAL (26 November 2026): Jacobus tenBroek, the NFB's founder, starts his surname in
            # lower case, so a word may also be lower case with a capital inside it.
            if not all(re.match(r"^(?:[A-Z][\w.'-]*|[a-z]+[A-Z][\w'-]*|de|del|da|di|du|la|le|van|von|St\.?)$", sWord) for sWord in lsWords): continue
            if sName not in lsNames: lsNames.append(sName)
    return lsNames


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
        # SEARCH_TEXT FROM PANDOC'S PLAIN TEXT (26 November 2026, version 5). The build's own measure gave body 2.3 MB of
        # Markdown but search_text only 0.2 MB, a tenth of it, though both hold the same words; the kit's tag stripper
        # was losing most of the real pages' words. Pandoc reads these pages for body already, so its plain-text writer
        # gives search_text from the same reading: every word, no marks, no addresses. The tag stripper still runs, and
        # the two are compared at every build (SEARCHTEXT lines), so a page where they part ways is named in the log.
        for dWeb, sMarkdown, sPlain, sFragment in zip(lWeb, toMarkdown(sPandoc, lsFragments), toMarkdown(sPandoc, lsFragments, "plain"), lsFragments):
            sStripped = htmlToPlain(sFragment)
            dPlain["pandoc"] = dPlain.get("pandoc", 0) + len(sPlain)
            dPlain["stripped"] = dPlain.get("stripped", 0) + len(sStripped)
            if len(sStripped) < len(sPlain) // 2 and dPlain.get("samples", 0) < 5:
                dPlain["samples"] = dPlain.get("samples", 0) + 1
                log("SEARCHTEXT %s: Pandoc's plain text %d characters, the tag stripper's %d; page markup begins %r; stripper gave %r"
                    % (dWeb["url"], len(sPlain), len(sStripped), sFragment[:400], sStripped[:200]))
            dWeb.update({"format": "web page", "body": sMarkdown, "text": None, "search_text": sPlain or sStripped})
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
        lArticles.append({"title": sTitle, "author": sAuthor, "url": sTextUrl, "format": "plain text", "body": None, "text": sBody, "search_text": None, "kind": sKind})
    for dWeb in lWeb:
        if id(dWeb) not in setUsed: lArticles.append(dict(dWeb, kind=kindOf(dWeb["title"], 99, False)))
    for iPosition, dArticle in enumerate(lArticles, 1):
        # Words and fingerprint come from the plain words, so the same article fingerprints alike from either source.
        sWords = dArticle["search_text"] if dArticle["body"] is not None else dArticle["text"]
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
    # PRIMES A PERSON CAN READ (26 November 2026, Jamal: article_key "is meant for machine readable parsing", so no
    # look or prime should use it). A prime says what a record is and sets the default order, so an issue is known by its
    # month and title ("2026-07|Braille Monitor, July 2026"), and an article by its month, its issue's title and its place
    # in the issue ("2026-07|Braille Monitor, July 2026|3"): unique, in the order the magazine printed them, and readable.
    # The articles table gains issue, the issue's title, for it, since a prime holds no id. issue_key and article_key stay
    # as fields, for the builder's own stable keys and for links from outside, but neither look nor prime uses them.
    for sStatement in trail.tableDdl("issues", "issue_id", lsIssueFields, ["title"], ["published", "title"], dIssueTypes): oDb.execute(sStatement)
    for sStatement in trail.tableDdl("writers", "writer_id", lsWriterFields, ["name"], ["name"], dWriterTypes): oDb.execute(sStatement)
    for sStatement in trail.tableDdl("articles", "article_id", lsArticleFields, ["title", "published"], ["published", "issue", "position"], dArticleTypes): oDb.execute(sStatement)
    oDb.execute("CREATE INDEX idx_articles_issue_id ON articles (issue_id)")
    trail.addLookups(oDb, "articles", "kind", dKinds, sSource)
    trail.addLookups(oDb, "articles", "format", dFormats, sSource)
    trail.addLookups(oDb, "issues", "edition", dEditions, sSource)
    iArticles = 0
    setTitles = set()
    for dIssue in lIssues:
        lArticles = dIssue["lArticles"]
        # A second issue in one month with the same title (rare: a second Monitor in May 1959) is told apart in its title,
        # which the prime needs; the log names it.
        sIssueTitle = issueTitle(dIssue)
        if (dIssue["published"], sIssueTitle) in setTitles:
            iNth = 2
            while (dIssue["published"], "%s, issue %d" % (sIssueTitle, iNth)) in setTitles: iNth += 1
            log("ISSUETITLE %s: %s is already taken, so this one is %s, issue %d" % (dIssue["issue_key"], sIssueTitle, sIssueTitle, iNth))
            sIssueTitle = "%s, issue %d" % (sIssueTitle, iNth)
        setTitles.add((dIssue["published"], sIssueTitle))
        oCursor = oDb.execute("INSERT INTO issues (title, edition, published, published_end, volume, number, articles, words, issue_key, next_number, text_url, url) "
                              "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                              (sIssueTitle, dIssue["edition"], dIssue["published"], dIssue["published_end"] or None, dIssue.get("volume"),
                               dIssue.get("number"), len(lArticles), sum(d["words"] for d in lArticles), dIssue["issue_key"], dIssue["next_number"],
                               dIssue.get("text_url") or None, None if dIssue["page"].lower().endswith(".txt") else dIssue["page"]))
        iIssue = oCursor.lastrowid
        for d in lArticles:
            oDb.execute("INSERT INTO articles (title, writer, published, issue, issue_id, position, kind, format, words, body, text, search_text, article_key, fingerprint, url) "
                        "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                        (d["title"], d["author"] or None, d["published"], sIssueTitle, iIssue, d["position"], d["kind"], d["format"], d["words"], d["body"], d["text"], d["search_text"],
                         d["article_key"], d["fingerprint"] or None, d["url"] or None))
            iArticles += 1
    # THE WRITERS TABLE (26 November 2026, Jamal: "an authors table in addition, properly mapped"; named writers the same
    # morning, since authors and articles share a first letter, so typing a did not reach one table). One row a person,
    # found in every article's byline by authorNames, with how many articles they wrote and the first and last month.
    # Each article is mapped to each of its writers by a maps row of kind written_by -- the way DbDo links records many
    # to many, by prime, as MusicTrail maps performers to albums -- so an article by three people reaches all three, and
    # a writer reaches every article. The byline stays in articles.writer as the magazine printed it (the column was
    # named author until the evening of 26 November 2026, when Jamal noticed it; DbDo's word is writer, as in the writers table).
    dAuthors = collections.OrderedDict()
    lMaps = []
    for sKey, sByline, sPublished in oDb.execute("SELECT article_key, writer, published FROM articles WHERE writer IS NOT NULL ORDER BY published, position"):
        for sName in authorNames(sByline):
            # ONE PERSON, ONE ROW: "Cynthia G. Hawkins" and "Cynthia Hawkins" are the same writer, so a middle initial is
            # set aside in matching, and the row takes the form the bylines use most.
            sMatch = " ".join(sWord for sWord in sName.lower().split(" ") if not re.match(r"^[a-z]\.?$", sWord))
            dAuthor = dAuthors.setdefault(sMatch, {"name": sName, "variants": collections.Counter(), "articles": 0, "first_published": sPublished, "last_published": sPublished})
            dAuthor["variants"][sName] += 1
            dAuthor["articles"] += 1
            dAuthor["last_published"] = sPublished
            lMaps.append((sKey, sMatch))
    for dAuthor in dAuthors.values(): dAuthor["name"] = dAuthor["variants"].most_common(1)[0][0]
    # A maps row names each record by its prime, so the article's prime -- month, issue and place -- not its key.
    dPrimes = dict(oDb.execute("SELECT article_key, prime FROM articles"))
    lMaps = [("articles", dPrimes[sKey], "written_by", "writers", dAuthors[sMatch]["name"]) for sKey, sMatch in lMaps]
    for dAuthor in sorted(dAuthors.values(), key=lambda d: d["name"].lower()):
        oDb.execute("INSERT INTO writers (name, articles, first_published, last_published) VALUES (?, ?, ?, ?)",
                    (dAuthor["name"], dAuthor["articles"], dAuthor["first_published"], dAuthor["last_published"]))
    for tMap in lMaps: oDb.execute("INSERT INTO maps (tbl1, prime1, kind, tbl2, prime2) VALUES (?, ?, ?, ?, ?)", tMap)
    trail.addLookups(oDb, "maps", "kind", {"written_by": "The article (subject) was written by the writer (object), as its byline names them."}, sSource)
    iBylines = oDb.execute("SELECT count(*) FROM articles WHERE writer IS NOT NULL").fetchone()[0]
    log("WRITERS %s from %s; %s" % (plural(len(dAuthors), "person", "people"), plural(iBylines, "byline"), plural(len(lMaps), "written_by map")))
    say("Writers: %s, mapped to their articles by %s." % (plural(len(dAuthors), "person", "people"), plural(len(lMaps), "link")))
    sTokenize = ("porter " if sPorter else "") + homerFts.sDefaultTokenize
    homerFts.createIndex(oDb, "articles", "article_id", ["title", "writer", "search_text", "text"], "articles_fts", sTokenize)
    oDb.execute("COMMIT")
    homerFts.optimize(oDb, "articles_fts")
    oDb.execute("ANALYZE")
    oDb.close()
    # DbDo'S OWN REPAIRS BEFORE THE CHECKS (25 November 2026): the build's checkTemplates --repair would otherwise rewrite
    # the whole file after every rebuild, to put defs in its order and add its lookup index. Done here, once, on the copy.
    import checkTemplates as templateCheck
    templateCheck.sLogPath = sLogPath
    templateCheck.repair(sSource, sTemp)
    oDb = sqlite3.connect(sTemp, isolation_level=None)
    oDb.execute("VACUUM")
    lsProblems = ["DbDo's template repair: " + s for s in templateCheck.lErrors]
    if oDb.execute("PRAGMA integrity_check").fetchone()[0] != "ok": lsProblems.append("SQLite's integrity check failed")
    sFts = homerFts.integrityProblem(oDb, "articles_fts")
    if sFts: lsProblems.append("the full-text index does not match its table: " + sFts)
    iIndexed = oDb.execute("SELECT count(*) FROM articles_fts").fetchone()[0]
    if iIndexed != iArticles: lsProblems.append("the index holds %d rows for %d articles" % (iIndexed, iArticles))
    if not iArticles: lsProblems.append("no article was read")
    # WHAT EACH PART WEIGHS (26 November 2026, Jamal asked whether search_text makes the file much larger): the bytes of
    # body, search_text and text, and the full-text index's own share, measured on the real archive at every build.
    try:
        lSizes = oDb.execute("SELECT sum(length(CAST(body AS BLOB))), sum(length(CAST(search_text AS BLOB))), sum(length(CAST(text AS BLOB))) FROM articles").fetchone()
        iIndexBytes = sum(r[0] or 0 for r in oDb.execute("SELECT sum(length(block)) FROM articles_fts_data UNION ALL SELECT sum(length(term)) FROM articles_fts_idx"))
        iFile = os.path.getsize(sTemp)
        sWeights = "file %.1f MB: body (Markdown) %.1f MB, search_text %.1f MB, text %.1f MB, full-text index about %.1f MB" % tuple(
            [iFile / 1048576.0] + [(i or 0) / 1048576.0 for i in lSizes] + [iIndexBytes / 1048576.0])
        log("SIZES " + sWeights)
        logBodyBulk(oDb)
        if dPlain: log("SEARCHTEXT Pandoc's plain text %d characters in all, the tag stripper's %d; %d pages named above where the stripper kept less than half" % (dPlain.get("pandoc", 0), dPlain.get("stripped", 0), dPlain.get("samples", 0)))
        say("Sizes: " + sWeights + ".")
    except Exception as oError:
        log("SIZES not measured: %s" % oError)
    # EVERY EXAMPLE IN THE GUIDE IS RUN (26 November 2026). help\\DbDo.md teaches searching BrailleForum by example; each
    # example in backticks in its "Searching BrailleForum" part is tried on the new database, written for the index as
    # Full-text match writes it (toFullText, the kit's Search.toFullText in Python), and its count logged. A search FTS5
    # refuses (the first draft had four: NEAR takes no OR, and a group in parentheses needs AND beside it) or one that
    # finds nothing is caught by the build, not by a reader, and named in the coverage notes.
    # THE EXAMPLES MOVED TO NOTES (27 November 2026, Jamal's ruling): the magazine archives are named in no public DbDo
    # document, so their search guide is notes\\MagazineArchives.md, kept on the development computer. help\\DbDo.md is
    # still read when the notes file is missing, for an older copy of the project.
    sGuide = os.path.join(sProject, "notes", "MagazineArchives.md")
    if not os.path.isfile(sGuide): sGuide = os.path.join(sProject, "help", "DbDo.md")
    sGuideText = io.open(sGuide, encoding="utf-8-sig").read() if os.path.isfile(sGuide) else ""
    oPart = re.search(r"(?ms)^#### Searching %s\s*$(.*?)(?=^#{1,4} )" % sSource, sGuideText + "\n#### end\n")
    if oPart:
        iRun, lsRefused, lsEmpty = 0, [], []
        lsFields = [r[1] for r in oDb.execute("PRAGMA table_info(articles)")]
        lsIndexColumns = [r[1] for r in oDb.execute("PRAGMA table_info(articles_fts)")]
        for sExample in re.findall(r"^[ \t]*- (?:\*\*[^*\n]+\*\*[^`\n]*)?`([^`\n]+)`", oPart.group(1), re.M):
            iRun += 1
            if isFullTextSyntax(sExample):
                sWhere = '"article_id" IN (SELECT rowid FROM "articles_fts" WHERE "articles_fts" MATCH \'%s\')' % sExample.replace("'", "''")
            else:
                sWhere = patternWhere(sExample, lsFields, "articles_fts", "article_id", lsIndexColumns)
            try:
                iHits = oDb.execute("SELECT count(*) FROM articles WHERE " + sWhere).fetchone()[0]
                log("EXAMPLE %s: %s  (as %s)" % (plural(iHits, "article"), sExample, sWhere))
                if not iHits: lsEmpty.append(sExample)
            except sqlite3.Error as oError:
                log("EXAMPLE REFUSED (%s): %s  (as %s)" % (oError, sExample, sWhere))
                lsRefused.append(sExample)
        say("The guide's search examples: %s tried; %d refused, %d found nothing; the log gives each count." % (plural(iRun, "example"), len(lsRefused), len(lsEmpty)))
        for sExample in lsRefused: dReport["examples"].append("Refused by FTS5: " + sExample)
        for sExample in lsEmpty: dReport["examples"].append("Found nothing: " + sExample)
    else:
        log("EXAMPLES: %s has no Searching %s part, so no example was tried" % (sGuide, sSource))
    log("BUILT %s: %s, %s, tokenizer %s; problems: %s" % (sTemp, plural(len(lIssues), "issue"), plural(iArticles, "article"), sTokenize, "; ".join(lsProblems) or "none"))
    oDb.close()
    if lsProblems: raise RuntimeError("the new database failed its checks, so the old one was kept: " + "; ".join(lsProblems))
    try:
        os.replace(sTemp, sOut)
    except OSError as oError:
        raise RuntimeError("the new database is built, but %s could not be replaced (%s). Close any DbDo window showing the %s "
                           "template and run scripts\\make%s --offline, which rebuilds from the cache in a minute" % (sOut, oError, sSource, sSource))
    return iArticles


# ---------------------------------------------------------------- the Homer pattern, for the index

# A PYTHON TWIN OF THE KIT'S Search.parse, Search.ftsTerm AND DbDo'S patternWhere (26 November 2026), so each build tries
# the guide's examples exactly as Full-text match will run them: a term the index can say goes to the index by whole
# words; %letters, field%, field= and a star inside a word test their fields directly. Kept line for line with the C#.
ANY, ONE = "\x01", "\x02"


def isFullTextSyntax(sPattern):
    """Whether a pattern is in FTS5's own language: a parenthesis, a ^, or AND, OR, NOT or NEAR in capitals."""
    return bool(re.search(r"[()^]", sPattern or "")) or bool(re.search(r'(^|[\s"])(AND|OR|NOT|NEAR)($|[\s"(])', sPattern or ""))


def parsePattern(sPattern, lsFields):
    """The kit's Search.parse with fields: alternatives (|) of terms (&), each a dict of text, not, wild, field, op,
    letters and near."""
    llAlternatives, lTerms, lsChars = [], [], []
    dState = dict(bQuote=False, bNot=False, bWild=False, bQuoted=False, bLetters=False, sField="", cOp="", iNear=0)
    s = sPattern or ""
    i = 0
    while i <= len(s):
        c = s[i] if i < len(s) else "\0"
        bEnd = i == len(s)
        if not bEnd and dState["bQuote"]:
            if c == '"': dState["bQuote"] = False
            else: lsChars.append(c)
            i += 1; continue
        if not bEnd and c == '"': dState["bQuote"] = dState["bQuoted"] = True; i += 1; continue
        if not bEnd and c == "~" and dState["bQuoted"] and i + 1 < len(s) and s[i + 1].isdigit():
            j = i + 1
            while j < len(s) and s[j].isdigit(): j += 1
            dState["iNear"] = int(s[i + 1:j]); i = j; continue
        if not bEnd and lsFields is not None and not dState["sField"] and not dState["bQuoted"] and c in ":%=":
            sHead = "".join(lsChars).strip()
            sMatch = next((f for f in lsFields if f.lower() == sHead.lower()), None) if sHead and not dState["bWild"] else None
            if sMatch: dState["sField"], dState["cOp"] = sMatch, c; lsChars = []; i += 1; continue
            if c == "%" and not sHead and not dState["bLetters"]: dState["bLetters"] = True; i += 1; continue
        if bEnd or c in "&|":
            sTerm = "".join(lsChars).strip()
            if sTerm: lTerms.append(dict(sText=sTerm, bNot=dState["bNot"], bWild=dState["bWild"], sField=dState["sField"], cOp=dState["cOp"], bLetters=dState["bLetters"], iNear=dState["iNear"]))
            lsChars = []
            dState.update(bNot=False, bWild=False, bQuoted=False, bLetters=False, sField="", cOp="", iNear=0)
            if bEnd or c == "|":
                if lTerms: llAlternatives.append(lTerms)
                lTerms = []
            i += 1; continue
        if c == "!" and not "".join(lsChars).strip(): dState["bNot"] = True; lsChars = []; i += 1; continue
        if c == "*": lsChars.append(ANY); dState["bWild"] = True; i += 1; continue
        if c == "?": lsChars.append(ONE); dState["bWild"] = True; i += 1; continue
        lsChars.append(c); i += 1
    return llAlternatives


def ftsTerm(t, lsIndexColumns):
    """The kit's Search.ftsTerm: one term for FTS5, or "" when the index cannot say it."""
    if t["bLetters"] or t["cOp"] in ("%", "="): return ""
    sColumn = ""
    if t["cOp"] == ":":
        sColumn = next((c for c in lsIndexColumns if c.lower() == t["sField"].lower()), "")
        if not sColumn: return ""
    sText = t["sText"]
    if t["iNear"]:
        lsWords = []
        for sWord in sText.split():
            sStem = sWord.rstrip(ANY)
            if ANY in sStem or ONE in sStem or not sStem: return ""
            lsWords.append('"' + sStem.replace('"', '""') + '"' + ("*" if len(sStem) < len(sWord) else ""))
        sQuery = "NEAR(%s, %d)" % (" ".join(lsWords), t["iNear"])
    else:
        bPrefix = sText.endswith(ANY)
        sText = sText.rstrip(ANY)
        if ANY in sText or ONE in sText: return ""
        sText = sText.strip()
        if not sText: return ""
        sQuery = '"' + sText.replace('"', '""') + '"' + ("*" if bPrefix else "")
    return sColumn + " : " + sQuery if sColumn else sQuery


def likeBody(sText):
    return "".join("%" if c == ANY else "_" if c == ONE else ("\\" + c if c in "\\%_" else c) for c in sText)


def patternTermSql(t, lsExprs):
    """DbDo's patternTermSql: a term tested on its fields directly."""
    lsAny = []
    for sExpr in lsExprs:
        sValue = "lower(coalesce(%s, ''))" % sExpr
        sText = t["sText"]
        if t["iNear"]:
            lsAny.append("(" + " AND ".join("instr(%s, lower('%s')) > 0" % (sValue, w.replace(ANY, "").replace("'", "''")) for w in sText.split()) + ")")
        elif t["cOp"] == "=":
            lsAny.append("%s LIKE lower('%s') ESCAPE '\\'" % (sValue, likeBody(sText).replace("'", "''")) if t["bWild"] else "trim(%s) = lower('%s')" % (sValue, sText.strip().replace("'", "''")))
        elif t["cOp"] == ":":
            sSpaced = sValue
            for sMark in [".", ",", ";", ":", "!", "?", "(", ")", '"', "'", "-", "/", "\r", "\n", "\t"]: sSpaced = "replace(%s, '%s', ' ')" % (sSpaced, sMark.replace("'", "''"))
            lsAny.append("(' ' || %s || ' ') LIKE lower('%% %s %%') ESCAPE '\\'" % (sSpaced, likeBody(sText).replace("'", "''")))
        elif not t["bWild"]:
            lsAny.append("instr(%s, lower('%s')) > 0" % (sValue, sText.replace("'", "''")))
        else:
            lsAny.append("%s LIKE lower('%%%s%%') ESCAPE '\\'" % (sValue, likeBody(sText).replace("'", "''")))
    return "(" + " OR ".join(lsAny) + ")"


def patternWhere(sPattern, lsFields, sIndex, sKey, lsIndexColumns):
    """DbDo's patternWhere: a Homer pattern as one SQL condition on articles."""
    llPattern = parsePattern(sPattern, lsFields)
    lsAlternatives = []
    for lTerms in llPattern:
        lsAll = []
        for t in lTerms:
            sQuery = ftsTerm(t, lsIndexColumns)
            sTest = '"%s" IN (SELECT rowid FROM "%s" WHERE "%s" MATCH \'%s\')' % (sKey, sIndex, sIndex, sQuery.replace("'", "''")) if sQuery else \
                patternTermSql(t, ['CAST("%s" AS TEXT)' % t["sField"]] if t["sField"] else ['CAST("%s" AS TEXT)' % f for f in lsFields])
            lsAll.append("NOT " + sTest if t["bNot"] else sTest)
        lsAlternatives.append(lsAll[0] if len(lsAll) == 1 else "(" + " AND ".join(lsAll) + ")")
    return lsAlternatives[0] if len(lsAlternatives) == 1 else "(" + " OR ".join(lsAlternatives) + ")"


# ---------------------------------------------------------------- statistics

lsStopWords = set("""a about after all also an and any are as at be been but by can could did do does for from had has have he her
his how i if in into is it its just like may me more most my new no not now of on one only or other our out over said she so
some such than that the their them then there these they this those through to two up us was we were what when where which
while who will with would you your""".split())


def writeStatistics(sDbPath, sPath):
    """notes\\BrailleForum_Statistics.md: what the archive's numbers say, computed from the finished database at every
    build (26 November 2026, Jamal: "a report file that computes and shows interesting stats, e.g., the writer of the
    most articles or of the most total content"). Counts are of articles of kind article unless they say otherwise."""
    oDb = sqlite3.connect(sDbPath)
    sArticle = "a.kind = 'article'"
    def rows(sSql, tArgs=()): return oDb.execute(sSql, tArgs).fetchall()
    def one(sSql, tArgs=()): return oDb.execute(sSql, tArgs).fetchone()
    def n(i): return "{:,}".format(i or 0)
    lsLines = ["# %s Statistics" % sSource, "",
               "Computed by scripts\\make%s on %s from %s.db. An article here is a row of kind article: mastheads, tables of contents, standing notices and whole issues are left out." % (sSource, datetime.date.today().strftime("%d %B %Y").lstrip("0"), sSource), ""]
    iIssues, sFirst, sLast = one("SELECT count(*), min(published), max(published) FROM issues")
    iArticles, iWords = one("SELECT count(*), sum(words) FROM articles a WHERE " + sArticle)
    lsLines += ["## The archive", "",
                "- %s, from %s to %s." % (plural(iIssues, "issue"), sFirst, sLast)]
    for sEdition, iCount in rows("SELECT edition, count(*) FROM issues GROUP BY edition ORDER BY edition"): lsLines.append("- %s: %s." % (sEdition, plural(iCount, "issue")))
    lsLines += ["- %s and %s words, about %s words an article." % (plural(iArticles, "article"), n(iWords), n(round((iWords or 0) / max(iArticles, 1))))]
    for sFormat, iCount in rows("SELECT format, count(*) FROM articles a WHERE " + sArticle + " GROUP BY format ORDER BY format"): lsLines.append("- From %s: %s." % ({"web page": "web pages", "plain text": "plain-text files"}.get(sFormat, sFormat), plural(iCount, "article")))
    # Writers
    lsLines += ["", "## Writers", ""]
    iWriters = one("SELECT count(*) FROM writers")[0]
    iOnce = one("SELECT count(*) FROM writers WHERE articles = 1")[0]
    iShared = one("SELECT count(*) FROM (SELECT prime1 FROM maps WHERE kind = 'written_by' GROUP BY prime1 HAVING count(*) > 1)")[0]
    lsLines += ["- %s named in bylines; %s wrote just one article." % (plural(iWriters, "writer"), n(iOnce)),
                "- %s had more than one writer." % plural(iShared, "article")]
    sJoin = "FROM maps m JOIN articles a ON a.prime = m.prime1 AND m.tbl1 = 'articles' JOIN writers w ON w.name = m.prime2 AND m.tbl2 = 'writers' WHERE m.kind = 'written_by' AND " + sArticle
    lsLines += ["", "### Most articles", ""]
    for sName, iCount, sFrom, sTo in rows("SELECT w.name, count(*), min(a.published), max(a.published) " + sJoin + " GROUP BY w.name ORDER BY count(*) DESC, w.name LIMIT 10"):
        lsLines.append("- %s: %s, %s to %s" % (sName, plural(iCount, "article"), sFrom, sTo))
    lsLines += ["", "### Most words", ""]
    for sName, iSum, iCount in rows("SELECT w.name, sum(a.words), count(*) " + sJoin + " GROUP BY w.name ORDER BY sum(a.words) DESC LIMIT 10"):
        lsLines.append("- %s: %s words in %s" % (sName, n(iSum), plural(iCount, "article")))
    lsLines += ["", "### Longest careers", "", "From first article to last, for writers with at least five.", ""]
    for sName, sFrom, sTo, iCount in rows("SELECT w.name, min(a.published), max(a.published), count(*) " + sJoin + " GROUP BY w.name HAVING count(*) >= 5 ORDER BY (CAST(substr(max(a.published),1,4) AS INTEGER) * 12 + CAST(substr(max(a.published),6,2) AS INTEGER)) - (CAST(substr(min(a.published),1,4) AS INTEGER) * 12 + CAST(substr(min(a.published),6,2) AS INTEGER)) DESC LIMIT 10"):
        lsLines.append("- %s: %s to %s, %s" % (sName, sFrom, sTo, plural(iCount, "article")))
    lsLines += ["", "### Most years", "", "Writers whose articles appear in the most different years.", ""]
    for sName, iYears, iCount in rows("SELECT w.name, count(DISTINCT substr(a.published, 1, 4)), count(*) " + sJoin + " GROUP BY w.name ORDER BY 2 DESC, 3 DESC LIMIT 10"):
        lsLines.append("- %s: %s, %s" % (sName, plural(iYears, "year"), plural(iCount, "article")))
    lsLines += ["", "### Longest single articles", ""]
    for sTitle, sByline, sPublished, iWordsHere in rows("SELECT title, writer, published, words FROM articles a WHERE " + sArticle + " ORDER BY words DESC LIMIT 10"):
        lsLines.append("- %s%s, %s: %s words" % (sTitle, ", by " + sByline if sByline else "", sPublished, n(iWordsHere)))
    # Years
    lsLines += ["", "## Years", ""]
    lYears = rows("SELECT substr(a.published, 1, 4), count(*), sum(a.words) FROM articles a WHERE " + sArticle + " GROUP BY 1 ORDER BY 1")
    if lYears:
        tMost = max(lYears, key=lambda t: t[1]); tLeast = min(lYears, key=lambda t: t[1])
        lsLines += ["- The most articles: %s, with %s." % (tMost[0], n(tMost[1])), "- The fewest: %s, with %s." % (tLeast[0], n(tLeast[1])), ""]
        lsLines += ["Each decade's articles, with how long they ran on average:", ""]
        dDecades = collections.OrderedDict()
        for sYear, iCount, iSum in lYears:
            dDecade = dDecades.setdefault(sYear[:3] + "0s", [0, 0]); dDecade[0] += iCount; dDecade[1] += iSum or 0
        for sDecade, (iCount, iSum) in dDecades.items(): lsLines.append("- %s: %s, about %s words each" % (sDecade, plural(iCount, "article"), n(round(iSum / max(iCount, 1)))))
    lsLines += ["", "## The biggest issues", ""]
    for sTitle, iCount, iSum in rows("SELECT title, articles, words FROM issues ORDER BY words DESC LIMIT 5"):
        lsLines.append("- %s: %s words in %s" % (sTitle, n(iSum), plural(iCount, "row")))
    # Recurring titles
    lsLines += ["", "## Columns that recur", "", "Titles that appear in the most issues, so the magazine's regular features.", ""]
    dTitles = collections.Counter()
    dShown = {}
    for (sTitle,) in rows("SELECT title FROM articles a WHERE " + sArticle):
        sKey = titleKey(sTitle)
        if sKey: dTitles[sKey] += 1; dShown.setdefault(sKey, sTitle)
    for sKey, iCount in dTitles.most_common(12):
        if iCount < 3: break
        lsLines.append("- %s: %s" % (dShown[sKey], plural(iCount, "issue")))
    # Words
    lsLines += ["", "## Words the most articles use", "", "From the full-text index, in the articles' words only (not titles or bylines), leaving out everyday words such as the and with; each word with how many articles use it, notices included."]
    try:
        oDb.execute("CREATE VIRTUAL TABLE temp.vocabulary USING fts5vocab(main, 'articles_fts', 'col')")
        lWords = [(sTerm, iDocs) for sTerm, iDocs in rows("SELECT term, sum(doc) FROM temp.vocabulary WHERE col IN ('search_text', 'text') GROUP BY term ORDER BY 2 DESC LIMIT 400") if sTerm not in lsStopWords and len(sTerm) > 2 and not sTerm.isdigit()]
        lsLines.append("")
        lsLines += ["- %s: %s" % (sTerm, plural(iDocs, "article")) for sTerm, iDocs in lWords[:30]]
    except sqlite3.Error as oError:
        lsLines.append("- The index's words could not be counted: %s." % oError)
    oDb.close()
    with io.open(sPath, "w", encoding="utf-8-sig", newline="\r\n") as fOut: fOut.write("\n".join(lsLines) + "\n")
    log("STATISTICS written to %s" % sPath)
    return True


def writeCoverage(lIssues, iArticles, sPorter, sPath):
    """notes\\BrailleForum_Coverage.md: what the archive holds and what it lacks, written every build. In notes, with
    the developer's other notes, not beside the template, since the Homer doc set has no place for a page there."""
    dYears = collections.OrderedDict()
    for dIssue in sorted(lIssues, key=lambda d: d["published"]):
        dYear = dYears.setdefault(dIssue["published"][:4], collections.Counter())
        dYear["issues"] += 1
        for d in dIssue["lArticles"]:
            dYear["web page" if d["format"] == "web page" else "plain text"] += 1
            if d["kind"] == "article": dYear["articles"] += 1
    lsLines = ["# %s Coverage" % sSource, "",
               "Written by scripts\\make%s on %s. The template holds %s and %s, indexed with the %s tokenizer." % (
                   sSource, datetime.date.today().strftime("%d %B %Y").lstrip("0"), plural(len(lIssues), "issue"), plural(iArticles, "row"),
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
                                  ("gone", "No longer found", "Every article of the last build is still here."),
                                  ("examples", "Guide examples to fix", "Every search example in help\\DbDo.md runs and finds something.")):
        lsLines += ["", "## " + sHeading, ""]
        lsLines += ["- " + s for s in dReport[sKey]] if dReport[sKey] else [sNone]
    with io.open(sPath, "w", encoding="utf-8-sig", newline="\r\n") as fOut: fOut.write("\n".join(lsLines) + "\n")
    return True


def logBodyBulk(oDb):
    """Where body's bulk comes from, in BODYBULK lines; run by the build and by every repair."""
    # WHERE BODY'S BULK COMES FROM (26 November 2026): the Markdown is seven times the plain words. Each build names
    # the three articles where the gap is widest and counts what could fill it -- embedded pictures (data: addresses),
    # links and pictures by address -- so the next cycle can decide whether anything in body should change.
    for sTitle, sUrl, iBody, iPlain, iData, iLinks, iPics, sSample in oDb.execute(
            "SELECT title, url, length(body), length(coalesce(search_text, '')), "
            "(length(body) - length(replace(body, 'data:', ''))) / 5, (length(body) - length(replace(body, '](http', ''))) / 6, "
            "(length(body) - length(replace(body, '![', ''))) / 2, substr(body, max(1, instr(body, 'data:') - 20), 160) "
            "FROM articles WHERE body IS NOT NULL ORDER BY length(body) - length(coalesce(search_text, '')) DESC LIMIT 3"):
        log("BODYBULK %s (%s): body %d, plain words %d; data: addresses %d, links by address %d, pictures %d; around the first data: %r" % (sTitle, sUrl, iBody, iPlain, iData, iLinks, iPics, sSample))
    log("BODYBULK %d articles hold an embedded picture (data:image), with %d characters of body among them" % tuple(
        oDb.execute("SELECT count(*), coalesce(sum(length(body)), 0) FROM articles WHERE body LIKE '%data:image%'").fetchone()))


fnStale = None   # a builder's own test of an existing template: returns why it must be rebuilt, else ""
bFromSaved = False   # set when a stale template is rebuilt from pages already saved
sFromSaved = "Most pages are saved, so this takes half an hour to an hour."


def repairTemplate():
    """Mends a template built by an earlier version, in place and fetching nothing, as each build runs it. Each mend is
    logged, and the file is left untouched when there is nothing to mend.
      - The views table, retired on 4 November 2026, which version 1 still made: the kit's checkDb failed the first
        push on it (25 November 2026)."""
    if not os.path.isfile(sTarget):
        say("There is no %s.db to repair." % sSource)
        return 0
    oDb = sqlite3.connect(sTarget, isolation_level=None)
    try:
        # VERSION 1 INDEXED body, Markdown and all (26 November 2026): such a template is rebuilt from the kept pages,
        # asking acb.org for nothing, so its index reads search_text; every key is kept.
        lsColumns = [r[1] for r in oDb.execute("PRAGMA table_info(articles)")]
        bAuthors = oDb.execute("SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = 'writers'").fetchone()[0] > 0
        # NOT THIN AFTER ALL (26 November 2026, second cycle). Version 5 rebuilt whenever search_text held less than half
        # as much as body, taking body's 2.3 MB for lost words. Pandoc's own plain text came to the same 0.3 MB, so the
        # words were all there and the bulk of body is markup; the test rebuilt the database at every build. It is gone;
        # the sizes are only logged, and the build logs where body's bulk comes from (BODYBULK lines).
        if "search_text" in lsColumns:
            iBody, iPlain = oDb.execute("SELECT coalesce(sum(length(body)), 0), coalesce(sum(length(search_text)), 0) FROM articles WHERE body IS NOT NULL").fetchone()
            log("REPAIR body %d characters, search_text %d" % (iBody, iPlain))
            logBodyBulk(oDb)
        # A MAGAZINE'S OWN TEST (26 November 2026): a builder that shares this code may say its template lacks what a
        # newer version gathers (makeBrailleMonitor version 2, the issues before 1985); the template is then rebuilt by
        # an ordinary run, which reads the kept pages and asks the site only about what it does not have.
        sStale = fnStale(oDb) if fnStale else ""
        if sStale:
            oDb.close()
            say(sSource + ".db is being rebuilt: " + sStale)
            global bFromSaved
            bFromSaved = True
            return main([])
        if "search_text" not in lsColumns or not bAuthors or "writer" not in lsColumns or "issue" not in lsColumns:
            oDb.close()
            say(sSource + ".db is being rebuilt from saved pages for the newest design.")
            return main(["--offline"])
        lsMends = []
        if oDb.execute("SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = 'views'").fetchone()[0]:
            oDb.execute("DROP TABLE views")
            lsMends.append("dropped the retired views table")
        if lsMends:
            oDb.execute("VACUUM")
            sProblem = homerFts.integrityProblem(oDb, "articles_fts")
            if sProblem: raise RuntimeError("the full-text index failed its check after the repair: " + sProblem)
            say(sSource + ".db repaired: " + "; ".join(lsMends) + ".")
        else:
            log("REPAIR nothing to mend in " + sTarget)
    finally:
        oDb.close()
    return 0


def main(lsArgs):
    log("make%s.py started; %s; script %s" % (sSource, sVersion, os.path.abspath(sys.argv[0] if sys.argv and sys.argv[0].endswith(".py") else __file__)))
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
    oCache = homerFetch.Cache(os.path.join(sProject, "data", sSource), fLog=log, nDelay=nDelay, iTimeout=iTimeout, iRetries=iRetries,
                              sUserAgent=sUserAgent, bOffline=bOffline)
    say("Finding the issues at %s%s." % (sSite, " in the cache only" if bOffline else ""))
    dIssues = discoverIssues(oCache)
    if not dIssues:
        oCache.save()
        say("No issue was found, so nothing was built. The log says why: " + sLogPath)
        return 1
    lIssues = sorted(dIssues.values(), key=lambda d: d["published"], reverse=True)
    if iLimit: lIssues = lIssues[:iLimit]
    say("Reading %s, from %s to %s. %s"
        % (plural(len(lIssues), "issue"), lIssues[-1]["published"], lIssues[0]["published"],
           "A pilot takes a few minutes." if iLimit else ("Saved pages only." if bOffline else (sFromSaved if bFromSaved else sFirstRun))))
    sPandoc = pandocPath()
    log("PANDOC %s" % (sPandoc or "none"))
    dOld = previousKeys(sTarget)
    sCutoff = (datetime.date.today() - datetime.timedelta(days=iTrustDays)).strftime("%Y-%m")
    lBuilt = []
    global sHeartbeat
    for iAt, dIssue in enumerate(lIssues, 1):
        # SUCCINCT (26 November 2026, Jamal: the status lines reassure, but should be shorter): the issue's month, without
        # the magazine's own name, which every line would repeat.
        sShort = shortTitle(dIssue)
        sHeartbeat = "Issue %d of %d, %s%s." % (iAt, len(lIssues), sShort, ", checking with the site" if bRefresh or dIssue["published"] >= sCutoff else "")
        dIssue["lArticles"] = readIssue(oCache, dIssue, sPandoc, not bRefresh and dIssue["published"] < sCutoff)
        if not dIssue["lArticles"]: continue
        dIssue["next_number"] = assignKeys(dIssue, dIssue["lArticles"], dOld)
        lBuilt.append(dIssue)
        # PROGRESS ABOUT A MINUTE APART (25 November 2026, Jamal asked for more frequent status): every fifth issue, with
        # the one just read and the articles so far, so a long first run never goes quiet for long.
        if iAt % 5 == 0 or iAt == len(lIssues):
            say("%d of %d issues, %s: %s." % (iAt, len(lIssues), sShort, plural(sum(len(d["lArticles"]) for d in lBuilt), "row")))
            oCache.save()
    sHeartbeat = ""
    oCache.save()
    if not iLimit:
        for sIssue in sorted(set(dOld) - {d["issue_key"] for d in lBuilt}):
            dReport["gone"].append("%s: the whole issue, no longer found" % sIssue)
    if not lBuilt:
        say("No issue could be read, so nothing was built. The log says why: " + sLogPath)
        return 1
    # A PILOT IS NEVER THE TEMPLATE. --limit builds data\\BrailleForum\\BrailleForum-pilot.db, to try the searches in DbDo;
    # the template, whose keys a person's copy is updated by, is replaced only by a build of every issue.
    sOut = os.path.join(sProject, "data", sSource, sSource + "-pilot.db") if iLimit else sTarget
    iArticles = build(lBuilt, bPorter, sOut)
    sNotes = os.path.dirname(sOut) if iLimit else os.path.join(sProject, "notes")
    os.makedirs(sNotes, exist_ok=True)
    sCoverage = os.path.join(sNotes, sSource + "_Coverage.md")
    writeCoverage(lBuilt, iArticles, bPorter, sCoverage)
    sStatistics = os.path.join(sNotes, sSource + "_Statistics.md")
    writeStatistics(sOut, sStatistics)
    say("Built %s: %s and %s. %s says what is in it and what is not; %s gives its statistics." % (sOut, plural(len(lBuilt), "issue"), plural(iArticles, "row"), sCoverage, sStatistics))
    if dReport["failed"]: say("%s could not be fetched; the log and the coverage notes name them." % plural(len(dReport["failed"]), "page or file", "pages or files"))
    say("Log: " + sLogPath)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except Exception as oError:
        log("CRASH " + traceback.format_exc())
        print("make%s stopped: %s. The log is %s." % (sSource, oError, sLogPath))
        sys.exit(1)
