r"""makeBrailleMonitor.py -- builds DbDo's BrailleMonitor template, the articles of the National Federation of the
Blind's magazine, the Braille Monitor, searchable by full text, as BrailleForum.db holds the ACB Braille Forum.

    scripts\makeBrailleMonitor               gathers what changed at nfb.org and builds the database
    scripts\makeBrailleMonitor --limit 12    a pilot of the newest 12 issues, data\BrailleMonitor\BrailleMonitor-pilot.db
    scripts\makeBrailleMonitor --offline     builds from the copies already kept, asking nfb.org for nothing
    scripts\makeBrailleMonitor --refresh     asks nfb.org again about every issue, however old
    scripts\makeBrailleMonitor --porter      stems English words in the index, to compare
    scripts\makeBrailleMonitor --repair      mends the template in place, as the build runs it, fetching nothing

ONE BUILDER, TWO MAGAZINES (26 November 2026, Jamal: BrailleMonitor.db "structured in an analogous manner to
BrailleForum.db"). The tables, fields, keys, writers, full-text index, checks, statistics and coverage notes are
makeBrailleForum's own: this file imports it, gives it this magazine's names, and replaces only the two parts that
differ between the two web sites -- how the issues are found (discoverIssues) and how one issue is read (readIssue).
A fix to the shared part reaches both databases; nothing is copied.

WHERE THE ISSUES COME FROM. Two pages at nfb.org list them: the Braille Monitor archive (every issue from 1957 on,
under a heading for each year) and the magazine's own page (this year's issues). Every issue lives in a folder of its
own, /images/nfb/publications/bm/bmYY/bmYYNN/, where NN is the issue's number in its volume, not its month: an
August/September issue makes the later numbers run one behind the months. The folder holds:
    bmYYNNtc.htm    the table of contents, each article a link to its own page (HTML) beside its audio (MP3)
    bmYYNNxx.htm    one page an article, xx counting up
    bmYYNN.htm      the complete issue in one page
The month comes from the archive's link text ("August/September 2025"); a folder linked without a month is dated from
its own table of contents ("Vol. 68, No. 8   August/September 2025").

OLDER ISSUES HAVE NO FOLDER (version 2, from the first full run's log: 313 issues came in, 1985 to 1986 and 2000 on,
while the archive links about 420 more). Those issues are single pages under /publications/bm/, in many spellings:
    Abm/bm1957/BrailleMonitorAugust1957.html, bm1965/65BM-APR.html, bm76/bm76-december.html    a whole issue in one page
    bm61/BLIND-AMERICAN-DEC-1961.html, bm62/blindamerican-may1962.html    the same, for The Blind American
    bm87/brlm8701.htm                                                    a whole issue, 1987 to 1998
    bm99/bm9912tc.htm                                                    a table of contents, 1999, its articles beside it
A dated link to such a page is an issue: a table of contents is read as the folders' are, its articles the pages in its
own directory that it links to; a whole-issue page is divided at its own headings, as a folder's complete page is. When
a month has both a folder and a single page, the folder wins: it has the articles one a page.

ONE ARTICLE, ONE ROW, FROM ITS OWN PAGE. Every Monitor article is a web page with the NFB's own headings, lists and
links, so body is always TEXTMARKDOWN, converted by Pandoc, and search_text the same words as Pandoc's plain text,
which the full-text index reads; format is "web page". The article's title is its page's own heading matching the
table of contents; what precedes it on the page (the masthead line "Braille Monitor, <month year>") and the back,
contents and next links are left out. An issue whose folder has no table of contents is read from its complete page,
divided at its own headings when it has at least three of one level, else kept whole as one row of kind "issue".
Nothing is inferred: a heading is the NFB's heading or there is none.

AN ARTICLE KEEPS ITS KEY, as in BrailleForum: article_key, such as bm-2024-07-003, is kept across rebuilds by the
article's page, else its words, else its title within the issue.

TEST AND LEARN. Each run logs what kinds of link the listing pages hold (LINKKIND), with samples of any it does not
use, so a form of issue the first cycles did not foresee shows in the log and the next version can read it; and it
writes notes\BrailleMonitor_Coverage.md and notes\BrailleMonitor_Statistics.md, as makeBrailleForum does.

RESPECTFUL FETCHING. The kit's homerFetch asks nfb.org at most once a second, asks only "has it changed?" of a page it
has, and does not ask at all about an issue more than four months old unless --refresh. The first run reads about ten
thousand pages, so it takes some hours, with a line of progress every five issues; later runs take minutes.

WHAT IS KEPT WHERE. data\BrailleMonitor holds the fetched pages; templates\BrailleMonitor\BrailleMonitor.db is built
beside itself and put in place only when it passes its checks. Both stay on this computer: LocalFiles.txt keeps them
off GitHub, and the installer leaves the BrailleMonitor template out. The NFB's words are the NFB's.

THE LOG is logs\DbDo-makeBrailleMonitor-<date>-<time>.log.
"""

import collections, datetime, html, os, re, sys, traceback, urllib.parse

sHere = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, sHere)
import makeBrailleForum as core

core.sSource = "BrailleMonitor"
core.sSite = "nfb.org"
core.sVersion = "2026-11-26 version 4: the issues of 1987 to 1997 divided at the titles their own contents lists name"
core.sUserAgent = "DbDo makeBrailleMonitor/2 (+https://github.com/JamalMazrui/DbDo; a searchable archive of the NFB Braille Monitor)"
core.sFirstRun = "The first run reads about ten thousand pages, one a second, so it takes some hours; later runs ask only about recent issues."
core.sTemplate = os.path.join(core.sProject, "templates", "BrailleMonitor")
core.sTarget = os.path.join(core.sTemplate, "BrailleMonitor.db")
core.sLogPath = os.path.join(core.sProject, "logs", "DbDo-makeBrailleMonitor-%s.log" % datetime.datetime.now().strftime("%Y%m%d-%H%M%S"))
core.trail.sLogPath = core.sLogPath
core.lsBoilerplate = ["photo and web list"]
core.dEditions = {"Braille Monitor": "The magazine of the National Federation of the Blind, published since 1957.",
                  "The Blind American": "The magazine the National Federation of the Blind published in 1961 and 1962, while the Braille Monitor paused."}
core.dKinds = {"article": "An article, column or speech: what a search usually wants.",
               "boilerplate": "A list the issue repeats, such as its photo and web list.",
               "issue": "A whole issue whose page has no table of contents and too few headings to divide."}
core.dFormats = {"web page": "From the article's own web page at nfb.org, converted to Markdown, in the body field."}
core.setRoleWords |= {"division", "federation", "national", "nfb", "presented", "reviewed"}

lsEntryPages = ["https://nfb.org/node/3771", "https://nfb.org/resources/publications-and-media/braille-monitor"]
reFolder = re.compile(r"/publications/bm/bm(\d\d)/bm(\d\d)(\d\d)/", re.I)
reMedia = re.compile(r"\.(mp3|zip|doc|docx|brf|pdf|txt|m4a|wav)$", re.I)
sMonths = core.sMonthPattern
sJoin = r"(?:\s*[-/–&]\s*|\s*\b(?:through|thru|to|and)\b\s*)"
reDate = re.compile(r"\b(%s)(?:%s(%s))?,?\s*((?:19|20)\d\d)" % (sMonths, sJoin, sMonths), re.I)
reVolume = re.compile(r"Vol\.?\s*(\d+)\s*,?\s*No\.?\s*(\d+)\s+((?:%s)(?:%s(?:%s))?)\s*,?\s*((?:19|20)\d\d)" % (sMonths, sJoin, sMonths), re.I)
rePage = re.compile(r"/publications/bm/(?:[^/?#]+/)*[^/?#]+\.html?$", re.I)

log, say, plural = core.log, core.say, core.plural


def tidyUrl(sUrl):
    """An address with its doubled slashes folded (nfb.org//images/... appears in the wild) and its fragment gone."""
    oParts = urllib.parse.urlsplit(sUrl)
    return urllib.parse.urlunsplit((oParts.scheme, oParts.netloc, re.sub(r"/{2,}", "/", oParts.path), oParts.query, ""))


def datesOf(sText):
    """(year, first month, last month, special) from text such as "August/September 2025", else None."""
    oDate = reDate.search(sText or "")
    if not oDate: return None
    iFirst = core.dMonths[oDate.group(1).lower()]
    iLast = core.dMonths[oDate.group(2).lower()] if oDate.group(2) else iFirst
    return int(oDate.group(3)), iFirst, iLast, "special" in (sText or "").lower()


def folderOf(sHref):
    """(folder address, stem such as bm2407, two-digit year) when a link points into an issue's folder."""
    oMatch = reFolder.search(sHref)
    if not oMatch: return None
    return sHref[:oMatch.end()], "bm%s%s" % (oMatch.group(2), oMatch.group(3)), oMatch.group(1)


def pageOf(sHref):
    """(directory address, stem, whether a table of contents) when a link points to a one-page issue or a table of
    contents under /publications/bm/ that is not in an issue folder (version 2)."""
    if folderOf(sHref) or not rePage.search(urllib.parse.urlsplit(sHref).path): return None
    sDir, sName = sHref.rsplit("/", 1)
    sStem = re.sub(r"(?i)\.html?$", "", sName)
    bToc = bool(re.search(r"(?i)tc$", sStem))
    return sDir + "/", (sStem[:-2] if bToc else sStem), bToc


def editionOf(sHref):
    return "The Blind American" if re.search(r"(?i)blind-?american", sHref) else "Braille Monitor"


def issueTitle(dIssue):
    iYear, iFirst = int(dIssue["published"][:4]), int(dIssue["published"][5:7])
    sMonths = datetime.date(iYear, iFirst, 1).strftime("%B")
    if dIssue["published_end"]: sMonths += "/" + datetime.date(iYear, int(dIssue["published_end"][5:7]), 1).strftime("%B")
    return "%s, %s %d%s" % (dIssue["edition"], sMonths, iYear, ", Special Issue" if dIssue.get("special") else "")


def discoverIssues(oCache):
    """Every issue the listing pages lead to: {issue_key: issue}. A dated link into an issue folder is an issue; a dated
    link to another nfb.org page is followed one step, its folder links taking the date; a folder linked without a date
    is dated from its own table of contents."""
    dFolders = collections.OrderedDict()   # folder (lower case) -> issue facts
    dPages = collections.OrderedDict()     # one-page issue or folderless contents (lower case) -> issue facts
    dLinkKinds, dSamples = collections.Counter(), collections.defaultdict(list)
    lQueue = [(s, 0, None) for s in lsEntryPages]
    setSeen = set()
    while lQueue:
        sUrl, iDepth, tInherited = lQueue.pop(0)
        if sUrl.lower() in setSeen: continue
        setSeen.add(sUrl.lower())
        dResult = oCache.fetch(sUrl)
        if dResult["body"] is None:
            core.dReport["failed"].append("%s (%s)" % (sUrl, dResult["error"]))
            continue
        lLinks, sH1 = core.readLinks(core.decoded(dResult["body"]), sUrl)
        for sHref, sText, sHeading in lLinks:
            sHref = tidyUrl(sHref)
            tFolder = folderOf(sHref)
            tDate = datesOf(sText) or (datesOf(sText + " " + sHeading) if re.fullmatch(r"(?i)\s*(%s)(\s*[-/–&]\s*(%s))?\s*" % (sMonths, sMonths), sText or "") else None)
            sHost = urllib.parse.urlsplit(sHref).netloc.lower()
            tPage = None if tFolder else pageOf(sHref)
            if tFolder:
                dLinkKinds["issue folder"] += 1
                sFolder, sStem, sYy = tFolder
                dIssue = dFolders.setdefault(sFolder.lower(), {"folder": sFolder, "stem": sStem, "yy": sYy, "page": sHref, "date": None, "link_text": sText})
                if tDate or tInherited:
                    if not dIssue["date"]: dIssue.update(date=tDate or tInherited, link_text=sText)
                if sHref.lower().endswith("tc.htm"): dIssue["page"] = sHref
            elif tPage and (tDate or tInherited) and sHost.endswith("nfb.org"):
                sKind = "one-page issue" if not tPage[2] else "table of contents without a folder"
                dLinkKinds[sKind] += 1
                if len(dSamples[sKind]) < 5: dSamples[sKind].append("%s -> %s" % (sText, sHref))
                dPages.setdefault(sHref.lower(), {"folder": tPage[0], "stem": tPage[1], "page": sHref, "single": True, "toc": tPage[2],
                                                  "date": tDate or tInherited, "link_text": sText, "edition": editionOf(sHref)})
            elif "archive.org" in sHost:
                dLinkKinds["archive.org"] += 1
                if len(dSamples["archive.org"]) < 5: dSamples["archive.org"].append("%s -> %s" % (sText, sHref))
            elif tDate and sHost.endswith("nfb.org") and not reMedia.search(sHref) and iDepth < 1 and re.search(r"(?i)monitor", sHref):
                # Version 2: only a page about the Monitor is followed; version 1 also read other publications' archives.
                dLinkKinds["dated nfb.org page, followed"] += 1
                if len(dSamples["dated nfb.org page, followed"]) < 5: dSamples["dated nfb.org page, followed"].append("%s -> %s" % (sText, sHref))
                lQueue.append((sHref, iDepth + 1, tDate))
            elif tDate:
                dLinkKinds["dated, not used"] += 1
                if len(dSamples["dated, not used"]) < 10: dSamples["dated, not used"].append("%s -> %s" % (sText, sHref))
            else:
                dLinkKinds["other"] += 1
    for sKind, iCount in sorted(dLinkKinds.items()):
        log("LINKKIND %s: %s%s" % (sKind, plural(iCount, "link"), (" -- for example " + "; ".join(dSamples[sKind])) if dSamples.get(sKind) else ""))
    # A folder linked without a date: its table of contents says the month.
    for dIssue in dFolders.values():
        if dIssue["date"]: continue
        for sTry in (dIssue["folder"] + dIssue["stem"] + "tc.htm", dIssue["folder"] + dIssue["stem"] + ".htm"):
            dPage = oCache.fetch(sTry)
            oVolume = reVolume.search(re.sub(r"<[^>]+>", " ", core.decoded(dPage["body"]))) if dPage["body"] is not None else None
            if oVolume:
                dIssue["date"] = datesOf(oVolume.group(3) + " " + oVolume.group(4))
                log("DATED %s from its own contents: %s %s" % (dIssue["folder"], oVolume.group(3), oVolume.group(4)))
                break
        if not dIssue["date"]: core.dReport["failed"].append("%s: an issue folder whose month could not be found" % dIssue["folder"])
    # Keys: bm-<year>-<first month>, with b, c ... for a second issue in the same month, in folder order.
    # A single page whose month a folder already has, or that repeats another page's month, is left out and logged.
    setFolderMonths = set((d["date"][0], d["date"][1], d["date"][3]) for d in dFolders.values() if d["date"])
    dChosen = {}
    for sLower, dPage in dPages.items():
        tMonth = (dPage["date"][0], dPage["date"][1], dPage["date"][2], dPage["date"][3], dPage["edition"])
        if (tMonth[0], tMonth[1], tMonth[3]) in setFolderMonths:
            log("DUPLICATE %s: its month has an issue folder, which is read instead" % dPage["page"])
        elif tMonth in dChosen:
            log("DUPLICATE %s: the same issue as %s, which is read instead" % (dPage["page"], dChosen[tMonth]["page"]))
        else:
            dChosen[tMonth] = dPage
    lAll = [dFolders[s] for s in sorted(dFolders)] + sorted(dChosen.values(), key=lambda d: (d["date"], d["page"].lower()))
    dIssues, dTaken = {}, collections.Counter()
    for dIssue in sorted(lAll, key=lambda d: (d["date"] or (0, 0, 0, False))[:2]):
        if not dIssue["date"]: continue
        iYear, iFirst, iLast, bSpecial = dIssue["date"]
        sBase = "bm-%04d-%02d" % (iYear, iFirst)
        sKey = sBase + ("" if not dTaken[sBase] else "bcdefgh"[dTaken[sBase] - 1])
        dTaken[sBase] += 1
        dIssues[sKey] = {"issue_key": sKey, "edition": dIssue.get("edition", "Braille Monitor"), "year": iYear, "published": "%04d-%02d" % (iYear, iFirst),
                         "published_end": "%04d-%02d" % (iYear, iLast) if iLast != iFirst else "", "special": bSpecial,
                         "page": dIssue["page"], "folder": dIssue["folder"], "stem": dIssue["stem"], "link_text": dIssue["link_text"],
                         "single": dIssue.get("single", False), "toc": dIssue.get("toc", False)}
    log("DISCOVERED %s from %s: %s and %s" % (plural(len(dIssues), "issue"), plural(len(setSeen), "listing page"), plural(len(dFolders), "folder"),
                                              plural(len(dChosen), "single page")))
    for sEdition, iCount in sorted(collections.Counter(d["edition"] for d in dIssues.values()).items()):
        log("EDITION %s: %s, %s to %s" % (sEdition, plural(iCount, "issue"), min(d["published"] for d in dIssues.values() if d["edition"] == sEdition),
                                          max(d["published"] for d in dIssues.values() if d["edition"] == sEdition)))
    return dIssues


def tableOfContents(lLinks, sFolder, sStem):
    """The article pages a table of contents lists, in its order, as (address, title): the folder's own bmYYNNxx.htm
    pages, the title without its "(HTML)"."""
    reArticle = re.compile(r"^%s\w+\.html?$" % re.escape(sStem), re.I)
    lArticles, setSeen = [], set()
    for sHref, sText, sHeading in lLinks:
        sHref = tidyUrl(sHref)
        if not sHref.lower().startswith(sFolder.lower()): continue
        sName = sHref.rsplit("/", 1)[-1]
        if not reArticle.match(sName) or re.match(r"(?i)^%s(tc)?\.html?$" % re.escape(sStem), sName): continue
        sTitle = re.sub(r"\s*\((?:HTML|MP3)\)\s*$", "", sText or "", flags=re.I).strip()
        if not sTitle or re.fullmatch(r"(?i)\(?\s*(back|contents|next|previous)\s*\)?", sTitle) or sHref.lower() in setSeen: continue
        setSeen.add(sHref.lower())
        lArticles.append((sHref, sTitle))
    return lArticles


def plainOf(sHtml):
    return re.sub(r"\s+", " ", core.html.unescape(re.sub(r"<[^>]+>", " ", sHtml or ""))).strip()


def cleaned(sHtml):
    """A page's body without scripts, styles, comments or its back, contents and next links."""
    oBody = re.search(r"(?is)<body\b[^>]*>(.*)</body>", sHtml)
    sBody = oBody.group(1) if oBody else sHtml
    sBody = re.sub(r"(?is)<(script|style|noscript)\b.*?</\1>", "", sBody)
    sBody = re.sub(r"(?s)<!--.*?-->", "", sBody)
    sBody = re.sub(r"(?is)<a\b[^>]*>\s*\(?\s*(back|contents|next|previous)\s*\)?\s*</a>", "", sBody)
    return re.sub(r"\(\s*(?:&nbsp;|\s)*\)", "", sBody)


def headings(sBody):
    return [(o.start(), o.end(), int(o.group(1)), plainOf(o.group(2))) for o in re.finditer(r"(?is)<h([1-6])\b[^>]*>(.*?)</h\1>", sBody)]


def articleParts(sHtml, sTocTitle):
    """(title, body markup, how it was found) from an article page: from the heading that matches the table of
    contents' title to the end; failing a match, from the first heading that is not the masthead's."""
    sBody = cleaned(sHtml)
    sKey = core.titleKey(sTocTitle)
    lHeads = [t for t in headings(sBody) if t[2] <= 4 and t[3]]
    for iStart, iEnd, iLevel, sText in lHeads:
        sHeadKey = core.titleKey(sText)
        if sHeadKey and sKey and (sHeadKey == sKey or (min(len(sHeadKey), len(sKey)) > 10 and (sHeadKey.startswith(sKey) or sKey.startswith(sHeadKey)))):
            return sText, sBody[iEnd:], "the heading that matches the contents"
    for iStart, iEnd, iLevel, sText in lHeads:
        if re.search(r"(?i)braille monitor|^contents$", sText) or reDate.fullmatch(sText.strip()): continue
        return sText, sBody[iEnd:], "the first heading that is not the masthead"
    return sTocTitle, sBody, "the whole page, which has no heading"


def sectionsOf(sHtml):
    """A complete issue page divided at its own headings: [(title, markup)], or [] when it has fewer than three of
    any one level from 1 to 4."""
    sBody = cleaned(sHtml)
    lHeads = headings(sBody)
    dLevels = collections.Counter(t[2] for t in lHeads if t[2] <= 4 and t[3])
    lLevels = [i for i in sorted(dLevels) if dLevels[i] >= 3]
    if not lLevels: return []
    # The masthead ("THE BRAILLE MONITOR", "The Blind American") and a bare "Contents" are not articles.
    lAt = [t for t in lHeads if t[2] == lLevels[0] and t[3] and not re.search(r"(?i)^(contents|(the\s+)?(braille\s+monitor|blind\s+american)\b.{0,40})$", t[3])]
    if len(lAt) < 2: return []
    return [(sText, sBody[iEnd:(lAt[i + 1][0] if i + 1 < len(lAt) else len(sBody))]) for i, (iStart, iEnd, iLevel, sText) in enumerate(lAt)]


def pageLines(sBody):
    """A page's text, one line for each line break, paragraph or heading, with the markup gone."""
    sText = re.sub(r"(?is)<br\s*/?>|</?(p|div|h[1-6]|li|tr|pre|center|blockquote)\b[^>]*>", "\n", sBody)
    return [html.unescape(re.sub(r"<[^>]+>", "", s)).replace("\xa0", " ").strip() for s in sText.split("\n")]


def isCapsLine(sLine):
    """A title line of the 1987 to 1997 issues: four or more letters, none of them lower case."""
    sLetters = re.sub(r"[^A-Za-z]", "", sLine)
    return len(sLetters) >= 4 and sLetters == sLetters.upper() and not re.match(r"(?i)^by\s", sLine)


def titlePattern(sTitle):
    """A title found in the markup whatever lies between its words: spaces, line breaks, tags or entities."""
    lsWords = []
    for sWord in sTitle.split():
        lsWords.append("".join(re.escape(ch) if ch.isalnum() else r"(?:&[#\w]+;|[^\s\w<])?" for ch in sWord))
    return re.compile(r"(?:\s|<[^>]*>|&nbsp;|&#160;)+".join(lsWords))


def contentsSections(sHtml):
    """A one-page issue with no headings divided at the titles its own contents list names: [(title, markup)], or []
    when fewer than three are found. Version 4 (26 November 2026): the 75 issues of 1987 to 1997 were each kept as one
    row, since their titles are lines in capitals, not headings. Each issue's page begins with a plain list headed
    Contents, and each title it names starts an article further down, in the same capitals. That is the page's own
    structure, so nothing is inferred: a title the list names but the page does not repeat is left out and logged."""
    sBody = cleaned(sHtml)
    lsLines = pageLines(sBody)
    iContents = next((i for i, s in enumerate(lsLines) if re.match(r"(?i)^(table\s+of\s+)?contents\b\W*$", s)), -1)
    if iContents < 0: return []
    lsTitles = []
    for sLine in lsLines[iContents + 1:iContents + 200]:
        if not sLine: continue
        if lsTitles and core.titleKey(sLine) == core.titleKey(lsTitles[0]): break      # the first article has begun
        if isCapsLine(sLine) and not re.search(r"(?i)^(the\s+)?braille\s+monitor\b", sLine): lsTitles.append(re.sub(r"\s+", " ", sLine))
    if len(lsTitles) < 3: return []
    oContents = re.search(r"(?i)contents", sBody)
    iAt = oContents.end() if oContents else 0
    # Past the list itself: each title's first appearance after "Contents" is in the list.
    for sTitle in lsTitles:
        oInList = titlePattern(sTitle).search(sBody, iAt)
        if oInList: iAt = oInList.end()
    lFound = []
    for sTitle in lsTitles:
        oHere = titlePattern(sTitle).search(sBody, iAt)
        if not oHere:
            log("CONTENTS title not found further down, left out: %s" % sTitle)
            continue
        lFound.append((sTitle, oHere.start(), oHere.end()))
        iAt = oHere.end()
    if len(lFound) < 3: return []
    return [(sTitle, sBody[iEnd:(lFound[i + 1][1] if i + 1 < len(lFound) else len(sBody))]) for i, (sTitle, iStart, iEnd) in enumerate(lFound)]


def bylineOf(sPlain):
    """The byline: the article's first line when it begins "by", "presented by", "reviewed by" or "edited by"."""
    for sLine in (sPlain or "").splitlines():
        if not sLine.strip(): continue
        oBy = re.match(r"(?i)^\s*(?:presented\s+|reviewed\s+|edited\s+|compiled\s+|written\s+)?by\s+(.+?)\s*$", sLine)
        return oBy.group(1).strip().rstrip(",.") if oBy and len(oBy.group(1)) < 150 else ""
    return ""


def readIssue(oCache, dIssue, sPandoc, bTrust):
    """The issue's articles as a list of dictionaries, in the table of contents' order."""
    oCache.bTrustKept = bTrust
    sFolder, sStem = dIssue["folder"], dIssue["stem"]
    lTries = []
    lCandidates = [dIssue["page"]] if dIssue.get("single") else \
        [dIssue["page"], sFolder + sStem + "tc.htm", sFolder + sStem + ".htm", sFolder + sStem + "tc.html", sFolder + sStem + ".html"]
    for sTry in lCandidates:
        if sTry.lower() not in [s.lower() for s in lTries]: lTries.append(sTry)
    lPages, sWhole, sWholeUrl, sHow = [], "", "", "no page could be read"
    for sTry in lTries:
        # The .html spellings (bm1206.html) are asked about only when the .htm ones gave no page.
        if sWhole and sTry.lower().endswith(".html") and sTry.lower() != dIssue["page"].lower(): continue
        dPage = oCache.fetch(sTry)
        if dPage["body"] is None:
            log("ISSUE %s: %s not read (%s)" % (dIssue["issue_key"], sTry, dPage["error"]))
            continue
        sHtml = core.decoded(dPage["body"])
        oVolume = reVolume.search(plainOf(sHtml[:20000]))
        if oVolume and not dIssue.get("volume"): dIssue["volume"], dIssue["number"] = oVolume.group(1), oVolume.group(2)
        lLinks, sH1 = core.readLinks(sHtml, sTry)
        lPages = tableOfContents(lLinks, sFolder, sStem)
        if lPages:
            sHow = "the table of contents at " + sTry
            dIssue["page"] = sTry
            break
        if dIssue.get("toc"):
            # Test and learn: a folderless table of contents whose article links were not recognized shows its links.
            log("TOCLINKS %s: no article links recognized; its links include %s" % (sTry, "; ".join("%s -> %s" % (t[1], t[0]) for t in lLinks[:12]) or "none"))
        if not sWhole and (re.search(r"(?i)%s\.html?$" % re.escape(sStem), sTry) or (dIssue.get("single") and not dIssue.get("toc"))): sWhole, sWholeUrl = sHtml, sTry
    lsFragments, lWeb = [], []
    for sHref, sTocTitle in lPages:
        dArticle = oCache.fetch(sHref)
        if dArticle["body"] is None:
            core.dReport["failed"].append("%s: article page %s (%s)" % (dIssue["issue_key"], sHref, dArticle["error"]))
            continue
        sTitle, sBody, sFound = articleParts(core.decoded(dArticle["body"]), sTocTitle)
        if not plainOf(sBody):
            core.dReport["unread"].append("%s: %s held no article" % (dIssue["issue_key"], sHref))
            continue
        log("PAGE %s: %s, from %s" % (sHref, sTitle, sFound))
        lWeb.append({"title": sTitle, "author": "", "url": sHref, "kind": core.kindOf(sTitle, 99, False)})
        lsFragments.append(sBody)
    if not lPages and sWhole:
        lSections = sectionsOf(sWhole)
        log("HEADINGS %s: %s" % (dIssue["issue_key"], ", ".join("h%d %d" % (i, n) for i, n in sorted(collections.Counter(t[2] for t in headings(cleaned(sWhole)) if t[3]).items())) or "none"))
        if lSections:
            sHow = "its complete page, divided at its %s" % plural(len(lSections), "heading")
            for sTitle, sBody in lSections:
                lWeb.append({"title": sTitle, "author": "", "url": sWholeUrl, "kind": core.kindOf(sTitle, 99, False)})
                lsFragments.append(sBody)
        elif contentsSections(sWhole):
            lSections = contentsSections(sWhole)
            sHow = "its complete page, divided at the %s its contents list names" % plural(len(lSections), "title")
            # Test and learn: how the titles are marked up, from the first one, for the next cycle's log.
            sSample = cleaned(sWhole)
            oFirst = titlePattern(lSections[0][0]).search(sSample, sSample.lower().find("contents") + 8 + len(lSections[0][0]))
            if oFirst: log("CONTENTSMARKUP %s: %s" % (dIssue["issue_key"], re.sub(r"\s+", " ", sSample[max(0, oFirst.start() - 150):oFirst.end() + 150])))
            for sTitle, sBody in lSections:
                lWeb.append({"title": sTitle, "author": "", "url": sWholeUrl, "kind": core.kindOf(sTitle, 99, False)})
                lsFragments.append(sBody)
        else:
            sHow = "its complete page, kept whole: it has no table of contents and too few headings to divide"
            core.dReport["undivided"].append("%s (%s)" % (dIssue["issue_key"], sWholeUrl))
            lWeb.append({"title": issueTitle(dIssue), "author": "", "url": sWholeUrl, "kind": "issue"})
            lsFragments.append(cleaned(sWhole))
    if not lWeb:
        core.dReport["failed"].append("%s: %s (%s)" % (dIssue["issue_key"], issueTitle(dIssue), sHow))
        return []
    if not sPandoc: raise RuntimeError("Pandoc is needed to convert the article pages, and none could be found or installed")
    for dWeb, sMarkdown, sPlain in zip(lWeb, core.toMarkdown(sPandoc, lsFragments), core.toMarkdown(sPandoc, lsFragments, "plain")):
        dWeb.update({"format": "web page", "body": sMarkdown, "text": None, "search_text": sPlain or core.htmlToPlain(sMarkdown)})
        dWeb["author"] = bylineOf(sPlain)
    dIssue["text_url"] = ""
    log("ISSUE %s: %s, %s from %s" % (dIssue["issue_key"], issueTitle(dIssue), plural(len(lWeb), "row"), sHow))
    for iPosition, dArticle in enumerate(lWeb, 1):
        dArticle.update({"position": iPosition, "words": core.wordCount(dArticle["search_text"]), "fingerprint": core.fingerprint(dArticle["search_text"]),
                         "published": dIssue["published"]})
    return lWeb


def stale(oDb):
    """Why a template built by version 1 must be rebuilt: it has no issue before 1985."""
    if oDb.execute("SELECT count(*) FROM issues WHERE published < '1985'").fetchone()[0]: return ""
    return "it lacks the issues before 1985."


core.fnStale = stale
core.discoverIssues = discoverIssues
core.readIssue = readIssue
core.issueTitle = issueTitle
core.tableOfContents = tableOfContents
core.articleParts = articleParts

if __name__ == "__main__":
    try:
        sys.exit(core.main(sys.argv[1:]))
    except Exception as oError:
        log("CRASH " + traceback.format_exc())
        print("makeBrailleMonitor stopped: %s. The log is %s." % (oError, core.sLogPath))
        sys.exit(1)
