# fetchStations.py -- fill a RadioTrail database from the Radio Browser community
# catalog and the SomaFM channel list, keeping everything the listener has written.
#
# WHAT RADIO BROWSER IS. A public, volunteer-kept directory of Internet radio
# stations, around sixty thousand of them, with a free JSON interface and no
# account. It is the backbone that Quill Radio built its catalog on, and the
# lessons from that work are applied here:
#
#   - Page the stations endpoint. Asked for everything at once it answers
#     1,000 rows and says nothing about the rest; 10,000 a page with a short
#     pause between pages brings the whole catalog in a few requests.
#   - Ask only for stations whose stream worked at the last check
#     (hidebroken=true), so the listener is not the one to find the dead ones.
#   - Key each station by its uuid, never by its stream address: thousands of
#     addresses are shared by more than one real station -- relays, network
#     feeds -- and merging on the address would fold real stations together.
#   - Identify yourself. The service asks every client for a User-Agent that
#     names the program, and may refuse one that does not.
#
# WHAT THIS SCRIPT NEVER TOUCHES: status, rating, notes and tags are the
# listener's, and a refresh leaves them exactly as they were. Everything the
# catalog knows is rewritten; everything the person wrote is kept.
#
# ONE COMMAND DOES THE WHOLE JOB. fetchStations with no arguments:
#
#   1. A clean copy from the template -- when there is none, when the copy is
#      not this template's shape, or when it holds nothing of yours yet (no
#      status set, no rating, no notes, no tags). A copy you have marked up is
#      kept and refreshed; --fresh forces a clean one. There is no migration of
#      an older copy: the structure going forward is the template's.
#   2. The catalog: every working Radio Browser station, and SomaFM's channels.
#   3. What each station says about itself: its stream's headers and its home
#      page, forty stations at a time. Every station, which takes an hour or
#      two for sixty thousand; stop it at any time, and the next run carries
#      on where it stopped, because each station is marked when it has been
#      asked.
#   4. What the record says: for a station with a broadcast call sign in its
#      name -- KIRO, WNYC, CBC -- the Wikipedia article's infobox, which is
#      kept from the licence: call sign, frequency, city of licence, owner,
#      format, first air date, and the article's opening paragraph. Only
#      taken when the article's own call sign appears in the station's name,
#      so a near miss is left empty rather than filled wrong.
#
# WHERE EACH THING GOES. A fact most stations have is a column: call sign,
# frequency, city, owner, format, wikipedia. Words that vary a lot -- the
# catalog's tags, a stream's genre words, a page's keywords, the article's
# format -- go to tags, one per line, so Filter Records and Keywords find any
# of them. Prose -- what the station or its article says in sentences -- goes
# to notes. Neither tags nor notes is ever emptied: lines are added, and a
# line you wrote stays. status and rating are yours alone.
#
#   fetchStations                       everything the check finds wanting, then the networks:
#                                       the best catalog, the one DbDo's installer ships
#   fetchStations --country "United States"   the catalog for one country only
#   fetchStations --limit 2000          the 2,000 most voted, for a taste
#   fetchStations --catalog-only        steps 1 and 2, no asking
#   fetchStations --enrich-only         step 3 only, on what is there
#   fetchStations --official-only       step 4 only, on what is there
#   fetchStations --clean               repairs alone: wrong article matches, leaked markup, duplicate
#                                       rows; for the shipped template, nobody's own stations or plays
#   fetchStations --affiliations        the networks each matched article names
#                                       (Seahawks Radio Network, ESPN Radio), as tags
#   fetchStations --no-official         skip step 4
#   fetchStations --report              the quality report alone, nothing fetched
#   fetchStations --force               every step, whatever the check says
#   fetchStations --import              add the stations in every playlist -- .m3u,
#                                       .m3u8 or .pls -- in Downloads, Documents and
#                                       Desktop, as your own, source "mine", to YOUR
#                                       copy, never the shipped template; a station
#                                       already there is left alone
#   fetchStations --import list.m3u     the same, from one playlist
#   fetchStations --import C:\Folder    the same, from every playlist in a folder
#
# STATIONS THE CATALOG HAS DROPPED. After a full catalog fetch, a Radio Browser
# station that was not in it any more -- its stream failed the directory's
# checks, or it was removed -- is marked dead, unless you had already set its
# status yourself. It stays in the table, so a favorite that went quiet is
# still yours to see and to try; Filter Records on status shows the dead ones.
#
# THE CHECK COMES FIRST. Before anything is fetched, the copy is measured
# against what it is for -- comprehensive: the whole catalog, fetched within a
# week; consistent: every row playable, no litter in genre, no duplicates
# hiding; coherent: the parts of a record agreeing; and how many stations have
# been asked and looked up -- and the report is said. Then only what is
# missing is fetched. A complete copy is left alone.
#   fetchStations --again               ask again the stations already asked
#   fetchStations --fresh               a clean copy even if yours holds notes
#   fetchStations path\to\other.db      another database instead of the copy
#   fetchStations --source somafm       only SomaFM in step 2
#
# WHAT A STATION SAYS ABOUT ITSELF. The catalog knows a station's name, its
# tags and where it is, and that is all: "Official home of the Seattle
# Seahawks" is in no catalog field. It is in two places the station itself
# publishes. A stream's first reply carries ICY headers -- icy-name,
# icy-description, icy-genre, icy-url -- and that description is usually the
# station's slogan. And the station's home page has a title and a meta
# description. --enrich reads both, twenty stations at a time, three seconds
# each, and files what it finds: the slogan into slogan, the rest into
# descrip, any new genre words into genre. Then Keywords finds "Seahawks".
# Each station is probed once; probed holds the date, and --enrich --again
# repeats the ones already done.
#
# WHERE THE SERVERS COME FROM. Radio Browser publishes its mirrors through DNS:
# all.api.radio-browser.info resolves to every mirror's address, and each
# address resolves back to a real host name, which TLS needs. The script asks
# DNS, shuffles what it gets so no one mirror carries everyone, and falls
# over to the next on an error -- the project's own documented recipe, and
# the one Quill Radio follows. A short list of known mirrors is the fallback
# when DNS gives nothing.
#
# SOMAFM is forty-six listener-supported channels with a published channel
# list at somafm.com/channels.json. Each channel offers playlists by format
# and quality; the best mp3 is taken, and its .pls resolved to the stream it
# names, as Quill Radio does. SomaFM channels carry no Radio Browser uuid and
# are keyed as somafm:<channel id>, so the two sources never collide.
#
# The log is %LOCALAPPDATA%\DbDo\logs\RadioTrail-fetch-<date>-<time>.log, where
# DbDo's own logs go: everything it did, every request, every count.
import datetime, html, json, os, random, re, socket, sqlite3, sys, threading, time, urllib.error, urllib.parse, urllib.request

c_sAgent = "RadioTrail/1.0 (DbDo; https://github.com/JamalMazrui/DbDo)"
c_lsServers = ["https://de1.api.radio-browser.info", "https://fi1.api.radio-browser.info",
               "https://nl1.api.radio-browser.info", "https://at1.api.radio-browser.info"]
c_sAllHosts = "all.api.radio-browser.info"
c_sSomaChannels = "https://somafm.com/channels.json"
c_lsSomaFormats = ["mp3", "aac", "aacp"]
c_lsSomaQuality = ["highest", "high", "low"]
c_iProbeThreads = 40
c_iProbeSeconds = 4
c_sWikiApi = "https://en.wikipedia.org/w/api.php"
# ONE REQUEST A SECOND, ONE THREAD. Wikipedia asks exactly that of a script,
# and refuses one that does more: on 5 October 2026 six threads were answered
# for the first thousand stations and silently refused for the next six
# thousand, which read as "no safe match" when it was "go away". A refusal is
# now told apart from a miss, waited out, and the station left for next time.
c_iWikiThreads = 1
c_dWikiPause = 1.0
c_lsCallSignCountries = ("US", "CA")
c_reCallSign = re.compile(r"\b([KWC][A-Z]{2,3})(?:[- ]?(AM|FM))?\b")


# THE TRAIL CONVENTIONS, KEPT BY EVERY RUN (9 October 2026). The kit's checkDb found RadioTrail without the triggers
# every other Trail has: editing or deleting a station left its map links pointing at nothing, and "edited" changed
# only when DbDo itself made the edit. This adds whatever is missing -- the unique prime indexes of lookups and maps,
# an edited trigger for each table, and the map-link triggers of stations and lookups -- in the form BookTrail uses,
# and changes nothing that is already there, so a listener's own copy gains them at its next refresh.
c_lsAutomatic = ["votes", "clicks", "trend", "last_check", "last_seen", "probed", "plays", "last_played"]


def markedLast(c):
    """MARKED IS THE LAST COLUMN (10 October 2026). The Trail rule, which the kit's database check fails, puts marked at
    the end of the stations table. A catalog grown over many fetches gained call_sign, frequency, city, owner, format,
    wikipedia, plays and last_played after marked, since SQLite adds a new column at the end; the 60,342 stations
    restored from the Recycle Bin failed the release for it. The table is rebuilt from its own definition with marked
    moved last -- every type, default and constraint kept -- its rows copied by column name, its indexes and triggers
    made again, in one transaction with foreign-key enforcement off, so a failure leaves the database as it was and no
    linked record is lost. Returns 1 when the table was rebuilt, 0 when marked was already last."""
    # table_xinfo, not table_info: the generated columns look and prime count as columns for the rule, and table_info
    # leaves them out. Only stored columns (hidden 0) are copied; generated ones compute themselves.
    lInfo = list(c.execute("pragma table_xinfo(stations)"))
    lsColumns = [r[1] for r in lInfo]
    lsStored = [r[1] for r in lInfo if r[6] == 0]
    if "marked" not in lsColumns or lsColumns[-1] == "marked": return 0
    sCreate = c.execute("select sql from sqlite_master where type='table' and name='stations'").fetchone()[0]
    iOpen, iClose = sCreate.index("("), sCreate.rindex(")")
    lsItems, sItem, iDepth = [], "", 0
    for sChar in sCreate[iOpen + 1:iClose]:
        if sChar == "(": iDepth += 1
        elif sChar == ")": iDepth -= 1
        if sChar == "," and iDepth == 0: lsItems.append(sItem.strip()); sItem = ""
        else: sItem += sChar
    if sItem.strip(): lsItems.append(sItem.strip())
    def columnOf(s): return s.split()[0].strip('"[]`') if s.split() else ""
    lsDefs = [s for s in lsItems if columnOf(s) in lsColumns]
    lsConstraints = [s for s in lsItems if columnOf(s) not in lsColumns]
    sMarked = [s for s in lsDefs if columnOf(s) == "marked"][0]
    lsDefs = [s for s in lsDefs if columnOf(s) != "marked"] + [sMarked]
    lsOrder = [columnOf(s) for s in lsDefs if columnOf(s) in lsStored]
    lsKeep = [r[0] for r in c.execute("select sql from sqlite_master where tbl_name='stations' and type in ('index', 'trigger') and sql is not null")]
    sList = ", ".join('"%s"' % s for s in lsOrder)
    c.execute("pragma foreign_keys=off")
    try:
        c.execute("begin")
        c.execute('create table "stations_marked_last" (' + ", ".join(lsDefs + lsConstraints) + ")")
        c.execute('insert into "stations_marked_last" (%s) select %s from stations' % (sList, sList))
        iBefore = c.execute("select count(*) from stations").fetchone()[0]
        iAfter = c.execute('select count(*) from "stations_marked_last"').fetchone()[0]
        if iBefore != iAfter: raise RuntimeError("copied %d of %d stations" % (iAfter, iBefore))
        c.execute("drop table stations")
        c.execute('alter table "stations_marked_last" rename to stations')
        for sSql in lsKeep: c.execute(sSql)
        c.execute("commit")
    except Exception:
        c.execute("rollback")
        raise
    finally:
        c.execute("pragma foreign_keys=on")
    return 1


def ensureConventions(c):
    """Adds the missing Trail indexes and triggers, after putting marked last; returns how many changes were made."""
    def quoted(s): return '"' + s.replace('"', '""') + '"'
    iAdded = markedLast(c)
    lsExisting = [r[0] for r in c.execute("select name from sqlite_master where type in ('index', 'trigger')")]
    for sTable in ("lookups", "maps"):
        sName = "idx_" + sTable + "_prime"
        if sName not in lsExisting:
            c.execute("create unique index if not exists %s on %s (prime)" % (quoted(sName), quoted(sTable)))
            iAdded += 1
    for sTable in ("stations", "lookups", "maps"):
        lsColumns = [r[1] for r in c.execute("pragma table_info(%s)" % quoted(sTable))]
        if not lsColumns: continue
        sKey = lsColumns[0]
        lsWatched = [s for s in lsColumns if s not in (sKey, "added", "edited", "marked") and not (sTable == "stations" and s in c_lsAutomatic)]
        sName = "trg_" + sTable + "_edited"
        if sName not in lsExisting and lsWatched:
            sOf = ", ".join(quoted(s) for s in lsWatched)
            sWhen = " OR ".join("OLD.%s IS NOT NEW.%s" % (quoted(s), quoted(s)) for s in lsWatched)
            c.execute("CREATE TRIGGER %s AFTER UPDATE OF %s ON %s FOR EACH ROW WHEN %s BEGIN UPDATE %s SET edited = CURRENT_TIMESTAMP WHERE %s = NEW.%s; END"
                      % (quoted(sName), sOf, quoted(sTable), sWhen, quoted(sTable), quoted(sKey), quoted(sKey)))
            iAdded += 1
        if sTable == "maps": continue
        sName = "trg_" + sTable + "_maps"
        if sName not in lsExisting:
            c.execute("CREATE TRIGGER %s AFTER UPDATE ON %s FOR EACH ROW WHEN OLD.prime IS NOT NEW.prime BEGIN UPDATE maps SET prime1 = NEW.prime WHERE tbl1 = '%s' AND prime1 = OLD.prime; UPDATE maps SET prime2 = NEW.prime WHERE tbl2 = '%s' AND prime2 = OLD.prime; END"
                      % (quoted(sName), quoted(sTable), sTable, sTable))
            iAdded += 1
        sName = "trg_" + sTable + "_maps_delete"
        if sName not in lsExisting:
            c.execute("CREATE TRIGGER %s AFTER DELETE ON %s FOR EACH ROW BEGIN DELETE FROM maps WHERE (tbl1 = '%s' AND prime1 = OLD.prime) OR (tbl2 = '%s' AND prime2 = OLD.prime); END"
                      % (quoted(sName), quoted(sTable), sTable, sTable))
            iAdded += 1
    c.commit()
    return iAdded

def addLines(sHave, lsNew):
    """Add lines to a one-per-line field, keeping every line already there."""
    lsOut = [s for s in (sHave or "").split("\n") if s.strip()]
    lsLow = set(s.strip().lower() for s in lsOut)
    for s in lsNew:
        s = (s or "").strip()
        if s and s.lower() not in lsLow: lsOut.append(s); lsLow.add(s.lower())
    return "\n".join(lsOut)

def wikiGet(dParams):
    dParams = dict(dParams); dParams["format"] = "json"
    oReq = urllib.request.Request(c_sWikiApi + "?" + urllib.parse.urlencode(dParams), headers={"User-Agent": c_sAgent})
    with urllib.request.urlopen(oReq, timeout=20) as oResp:
        return json.loads(oResp.read().decode("utf-8"))

def infoboxBody(sWikitext):
    """The text of the Infobox radio station template and nothing after it: the braces are counted, so the box ends
    at its own closing braces, not at the end of the article."""
    m = re.search(r"\{\{\s*Infobox radio station", sWikitext, re.I)
    if not m: return ""
    iDepth, i = 0, m.start()
    while i < len(sWikitext) - 1:
        if sWikitext[i:i + 2] == "{{": iDepth += 1; i += 2; continue
        if sWikitext[i:i + 2] == "}}":
            iDepth -= 1; i += 2
            if iDepth == 0: return sWikitext[m.end():i - 2]
            continue
        i += 1
    return sWikitext[m.end():]


def topLevelParts(sBody):
    """The template's parameters: the text split at each | that is not inside a nested template or link."""
    lsParts, sCur, iBraces, iBrackets, i = [], "", 0, 0, 0
    while i < len(sBody):
        s2 = sBody[i:i + 2]
        if s2 == "{{": iBraces += 1; sCur += s2; i += 2; continue
        if s2 == "}}" and iBraces: iBraces -= 1; sCur += s2; i += 2; continue
        if s2 == "[[": iBrackets += 1; sCur += s2; i += 2; continue
        if s2 == "]]" and iBrackets: iBrackets -= 1; sCur += s2; i += 2; continue
        if sBody[i] == "|" and not iBraces and not iBrackets: lsParts.append(sCur); sCur = ""; i += 1; continue
        sCur += sBody[i]; i += 1
    lsParts.append(sCur)
    return lsParts


def infoboxFields(sWikitext):
    """The fields of a Template:Infobox radio station, lightly cleaned.

    THE BOX AND ITS FIELDS, EXACTLY (11 October 2026). The box was taken to the end of the article and its fields split
    only where a line began with a bar, so a field sharing a line took the next one with it -- 506 cities read like
    "Key West, Florida | country = US" -- and the last field took the article's first sentence into a tag. Now the box
    ends at its own closing braces and is split at each bar outside a nested template or link, wherever it falls."""
    sBody = infoboxBody(sWikitext)
    if not sBody: return {}
    d = {}
    for sPart in topLevelParts(sBody)[1:]:
        mm = re.match(r"\s*([A-Za-z_ ]+?)\s*=\s*(.*)$", sPart, re.S)
        if not mm: continue
        k = mm.group(1).strip().lower().replace(" ", "_"); v = mm.group(2)
        v = re.sub(r"<ref[^>]*/>|<ref.*?</ref>", "", v, flags=re.S)
        # LISTS KEEP THEIR ITEMS (11 October 2026). An affiliations field is often a list -- {{hlist|ESPN Radio|
        # Seahawks Radio Network}}, {{ubl|...}}, a plainlist of "* " lines, or items split by <br> -- and the generic
        # template strip below dropped the whole list. Links are made plain first, then each list joined by commas.
        v = re.sub(r"\[\[(?:[^|\]]*\|)?([^\]]*)\]\]", r"\1", v)
        v = re.sub(r"<br\s*/?>", ", ", v, flags=re.I)
        v = re.sub(r"\{\{\s*(?:ubl|ublist|unbulleted list|hlist|flatlist|plainlist|bulleted list|cslist)\s*\|([^{}]*)\}\}",
                   lambda mm: ", ".join(s.strip(" *\n") for s in re.split(r"\||\n\s*\*", mm.group(1)) if s.strip(" *\n") and "=" not in s), v, flags=re.I)
        v = re.sub(r"\n\s*\*\s*", ", ", v)
        v = re.sub(r"\{\{(?:nowrap|nobr)\|([^}]*)\}\}", r"\1", v, flags=re.I)
        v = re.sub(r"\{\{\s*Frequency\s*\|\s*([^|}]+)\s*\|\s*([^|}]+)\s*\}\}", r"\1 \2", v, flags=re.I)
        v = re.sub(r"\{\{\s*Start date(?: and age)?\s*\|\s*(\d{4})(?:\s*\|\s*(\d{1,2}))?(?:\s*\|\s*(\d{1,2}))?[^}]*\}\}",
                   lambda mm: mm.group(1) + ("-" + mm.group(2).zfill(2) if mm.group(2) else "") + ("-" + mm.group(3).zfill(2) if mm.group(3) else ""), v, flags=re.I)
        v = re.sub(r"\{\{[^}]*\}\}", "", v)
        v = re.sub(r"\[\[(?:[^|\]]*\|)?([^\]]*)\]\]", r"\1", v)
        v = re.sub(r"<[^>]+>", " ", v)
        v = v.replace("'''", "").replace("''", "")
        v = re.sub(r"\s+", " ", v).strip(" \n}")
        if v: d[k] = v[:200]
    return d

class WikiRefused(Exception):
    """A refusal, and how long to wait: the server's Retry-After when it gives one (11 October 2026), else a minute.
    A polite client waits as long as it is told, no longer, and between five seconds and ten minutes."""
    def __init__(self, sWhy, iWait=60):
        Exception.__init__(self, sWhy)
        self.iWait = max(5, min(600, iWait))


def retryAfter(oError):
    """Seconds from an HTTP error's Retry-After header, or 60 when it has none or gives a date."""
    try: return int(str(oError.headers.get("Retry-After", "")).strip())
    except Exception: return 60

def wikiPages(dParams):
    """One query; the pages it returns, or a WikiRefused when the answer is a
    refusal -- which Wikipedia gives as an HTTP code or, as often, as an
    error object inside a 200."""
    try:
        d = wikiGet(dParams)
    except urllib.error.HTTPError as oError:
        if oError.code in (403, 429, 503): raise WikiRefused(str(oError.code), retryAfter(oError))
        return [], "http " + str(oError.code)
    except Exception as oError:
        return [], "network: " + str(oError)[:80]
    if "error" in d:
        sCode = str(d["error"].get("code", ""))
        if sCode in ("ratelimited", "maxlag", "readonly"): raise WikiRefused(sCode)
        return [], "api error " + sCode
    return list(d.get("query", {}).get("pages", {}).values()), ""

def namedFrequencies(sText):
    """Broadcast frequencies a text states: FM from 87.5 to 108.0 with its decimal, AM from 530 to 1710 kHz."""
    lsOut = set()
    for s in re.findall(r"(?<![\d.])(\d{2,3}\.\d)(?![\d])", sText or ""):
        if 87.5 <= float(s) <= 108.0: lsOut.add(s)
    for s in re.findall(r"(?<![\d.])(\d{3,4})(?![\d.])", sText or ""):
        if 530 <= int(s) <= 1710: lsOut.add(s)
    return lsOut


# Places a title's bracket can name that are not in the United States or Canada, where call signs like these belong.
c_reForeignTitle = re.compile(r"\((?:[^)]*\b)?(UK|United Kingdom|British|England|Scotland|Wales|Ireland|Irish|Australia|Australian|New Zealand|India|Indian|Philippines|South Africa|Germany|German|France|French|Nigeria|Kenya|Jamaica|Malaysia|Singapore)\b", re.I)


def confirmsFrequency(sName, sBoxFrequency, sTitle):
    """ONE RULE FOR THE LOOKUP AND THE REPAIR (11 October 2026), so neither undoes the other. When the station's name
    states a broadcast frequency: an article that gives one must share it; an article that gives none is taken only
    when nothing marks it as another country's station -- a bracket in its title naming one, as in "Kiss (UK radio
    station)", which twenty-four American KISS FM streams took, or a DAB frequency, which North America does not use.
    KQED-FM, WTOP-FM and the like, whose infobox frequency the parser cannot read, are still taken. (Delivered on 11
    October and lost before it reached the DbDo folder; restored 12 October from that delivery, after an audit found the
    History describing it and the script without it.)"""
    lsName, lsBox = namedFrequencies(sName), namedFrequencies(sBoxFrequency)
    if not lsName: return True
    if lsBox: return bool(lsName & lsBox)
    return not c_reForeignTitle.search(sTitle or "") and "DAB" not in (sBoxFrequency or "").upper()


def pageRecord(oPage, sCall, sName=""):
    """The record from one page, when its infobox is a station's and its
    call sign is ours; otherwise None, with the reason."""
    sTitle = oPage.get("title", "")
    if "missing" in oPage: return None, "no page " + sTitle
    sText = ""
    try: sText = oPage["revisions"][0]["slots"]["main"]["*"]
    except Exception: pass
    if not sText:
        try: sText = oPage["revisions"][0].get("content", "") or oPage["revisions"][0].get("*", "")
        except Exception: pass
    dBox = infoboxFields(sText)
    if not dBox: return None, "no infobox in " + sTitle
    # A NAME IS NOT A CALL SIGN (11 October 2026): with no call-sign field, the box's name was used, and "CBC Radio 3"
    # contains CBC, so sixty CBC streams took CBC Radio 3's record. Only the call-sign field, or the title, now counts.
    sBoxCall = (dBox.get("call_sign") or dBox.get("callsign") or "").upper()
    # THE CALL SIGN MUST BE THE ARTICLE'S: in its infobox, or as the whole of
    # its title before a band or a bracket -- KALW, WCRB (FM), KQED-FM.
    bOurs = bool(re.search(r"\b" + re.escape(sCall) + r"\b", sBoxCall)) or bool(re.match(r"^" + re.escape(sCall) + r"(-(AM|FM|LP|HD\d?))?( \(.*\))?$", sTitle.upper()))
    if not bOurs: return None, "other call sign in " + sTitle
    # THE FREQUENCY MUST AGREE, by the rule the repair also uses.
    if not confirmsFrequency(sName, dBox.get("frequency", ""), sTitle): return None, "frequency in the name not confirmed by " + sTitle
    d = {"wikipedia": "https://en.wikipedia.org/wiki/" + urllib.parse.quote(sTitle.replace(" ", "_")),
         "call_sign": dBox.get("call_sign") or dBox.get("callsign") or sCall,
         "frequency": dBox.get("frequency", ""), "city": dBox.get("city", "") or dBox.get("area", ""),
         "owner": dBox.get("owner", ""), "format": dBox.get("format", ""),
         "extract": (oPage.get("extract") or "").strip()[:1500], "airdate": dBox.get("airdate", ""),
         "affiliations": dBox.get("affiliations", "") or dBox.get("network", "")}
    return {k: v for k, v in d.items() if v}, ""

def officialStation(sName):
    """Wikipedia's infobox and opening paragraph for a station whose name
    carries a call sign. The article is asked for BY TITLE first -- KALW,
    KALW-FM, KALW (FM) and so on, redirects followed -- which is how a
    licensed station's article is named; a search is the fallback. Returns
    (record, reason): the record empty when nothing safe was found, the
    reason saying why, for the log."""
    # THE NAME AS WRITTEN (11 October 2026): call signs are written in capitals, and the name was searched after being
    # made upper case, so "Kiss FM" yielded KISS and "Music City" CITY, and 834 stations took another station's
    # article -- owner, city, frequency and all. A call sign is now only one written in capitals.
    m = c_reCallSign.search(sName)
    if not m: return {}, "no call sign in name"
    sCall = m.group(1); sBand = m.group(2) or ""
    lsTitles = [sCall, sCall + "-FM", sCall + "-AM", sCall + " (FM)", sCall + " (AM)", sCall + "-LP"]
    if sBand: lsTitles.insert(0, sCall + "-" + sBand); lsTitles.insert(1, sCall + " (" + sBand + ")")
    dCommon = {"action": "query", "prop": "revisions|extracts", "rvprop": "content", "rvslots": "main", "exintro": "1", "explaintext": "1", "redirects": "1"}
    lsReasons = []
    dParams = dict(dCommon); dParams["titles"] = "|".join(dict.fromkeys(lsTitles))
    lsPages, sWhy = wikiPages(dParams)
    if sWhy: lsReasons.append(sWhy)
    for oPage in lsPages:
        d, sReason = pageRecord(oPage, sCall, sName)
        if d: return d, "by title " + oPage.get("title", "")
        if sReason and not sReason.startswith("no page"): lsReasons.append(sReason)
    time.sleep(c_dWikiPause)
    dParams = dict(dCommon); dParams.update({"generator": "search", "gsrlimit": "5", "gsrsearch": sCall + (" " + sBand if sBand else "") + " radio station"})
    lsPages, sWhy = wikiPages(dParams)
    if sWhy: lsReasons.append(sWhy)
    for oPage in lsPages:
        d, sReason = pageRecord(oPage, sCall, sName)
        if d: return d, "by search " + oPage.get("title", "")
        if sReason: lsReasons.append(sReason)
    return {}, "; ".join(lsReasons[:4]) or "search found nothing"

def affiliationItems(sValue):
    """The items of an infobox affiliations field, one each: "ESPN Radio, Seahawks Radio Network" gives two."""
    return [s.strip(" .") for s in re.split(r"\s*[,;]\s*", sValue or "") if len(s.strip(" .")) > 1]


def cleanCopy(sDb, bTemplate, logLine, say):
    """REPAIR, MERGE AND, FOR THE SHIPPED COPY, MAKE IT EVERYONE'S (11 October 2026). A plain run does this first.

    WRONG MATCHES. A station whose stored call sign is not one its name states in capitals, or whose name states a
    frequency the article does not, took another station's article: its call sign, frequency, city, owner, format and
    article are cleared, so the next lookup tries again under the strict rules, and the article's paragraph in the notes
    -- which begins with that call sign -- and the format words it added to the tags are taken out. Text a listener
    added is not touched. MARKUP that leaked from an article's infobox into a field or a tag is cut away.

    DUPLICATES. Rows with the same stream and the same name are one station listed more than once by the catalog; the
    most voted is kept, the others' tags and notes are added to it, and they go. A group with any row the listener has
    touched -- rated, played, given a status or marked, or added as their own -- is left as it is.

    THE SHIPPED COPY ONLY. The template DbDo's installer ships holds nobody's own stations, plays or ratings, no station
    without a name, and no leftover empty table."""
    c = sqlite3.connect(sDb)
    lsCols = set(r[1] for r in c.execute("pragma table_info(stations)"))
    dCount = {"cleared": 0, "markup": 0, "merged": 0, "mine": 0, "personal": 0, "nameless": 0}
    def plainField(v):
        v = re.split(r"\s*\|\s*[A-Za-z_ ]+\s*=", v or "")[0]
        v = re.split(r"\}\}|\{\{", v)[0]; v = v.replace("'''", "").replace("''", "")
        v = re.sub(r"\[\[(?:[^|\]]*\|)?([^\]]*)\]\]", r"\1", v)
        return re.sub(r"\s+", " ", v).strip(" |}{")
    if "call_sign" in lsCols:
        for iId, sName, sCall, sFreq, sCity, sOwner, sFormat, sWiki, sNotes, sTags in c.execute(
                "select station_id, name, call_sign, frequency, city, owner, format, wikipedia, notes, tags from stations where coalesce(wikipedia,'') not in ('', '-')").fetchall():
            mName = c_reCallSign.search(sName or "")
            mStored = re.match(r"[KWC][A-Z]{2,3}", (sCall or "").upper())
            # Looked up again, under the strict rules, when: the name states no call sign in capitals, or another one; the
            # name states a frequency the record does not share, or cannot confirm because it has none; or the stored
            # call sign is a bare one the article's title is not -- the mark of a match through the box's name field.
            sTitle = urllib.parse.unquote((sWiki or "").rsplit("/", 1)[-1]).replace("_", " ").upper()
            bBare = bool(mStored) and (sCall or "").upper() == mStored.group(0)
            bTitled = bool(mStored) and bool(re.match(r"^" + re.escape(mStored.group(0)) + r"(-(AM|FM|LP|HD\d?))?( \(.*\))?$", sTitle))
            bWrong = (not mName or not mStored or mName.group(1) != mStored.group(0)
                      or not confirmsFrequency(sName, sFreq, urllib.parse.unquote((sWiki or "").rsplit("/", 1)[-1]).replace("_", " "))
                      or (bBare and not bTitled))
            if bWrong:
                lsParas = [p for p in (sNotes or "").split("\n\n") if not (sCall and p.strip().upper().startswith((sCall or "").upper()))]
                lsFormat = set(s.strip().lower() for s in re.split(r"[,/;]", sFormat or "") if s.strip())
                lsTagKeep = [t for t in (sTags or "").split("\n") if t.strip().lower() not in lsFormat]
                if not bTemplate: lsParas, lsTagKeep = [sNotes or ""], (sTags or "").split("\n")
                c.execute("update stations set call_sign='', frequency='', city='', owner='', format='', wikipedia='', notes=?, tags=?, edited=CURRENT_TIMESTAMP where station_id=?",
                          ("\n\n".join(lsParas), "\n".join(lsTagKeep), iId))
                dCount["cleared"] += 1
                logLine("cleared %d %s: had %s (%s)" % (iId, (sName or "")[:40], sCall, sWiki))
                continue
            lsNew = [plainField(v) for v in (sFreq, sCity, sOwner, sFormat)]
            # A tag is a word or a phrase: a line with a sentence break, or longer than sixty characters, is leaked prose.
            # Cut at leaked markup first, then judge what is left, so "Westwood One }} and the article's first sentence"
            # keeps Westwood One.
            lsTagNew = [s for s in (plainField(re.split(r"\}\}|'''|\{\{", t)[0]) for t in (sTags or "").split("\n")) if not (re.search(r"[a-z]\. [A-Z]", s) or len(s) > 60)]
            sTagNew = "\n".join(t for t in lsTagNew if t) if bTemplate else (sTags or "")
            if lsNew != [sFreq or "", sCity or "", sOwner or "", sFormat or ""] or sTagNew != (sTags or ""):
                c.execute("update stations set frequency=?, city=?, owner=?, format=?, tags=? where station_id=?", tuple(lsNew) + (sTagNew, iId))
                dCount["markup"] += 1
    # LEAKED TAGS ARE CUT ON EVERY STATION (11 October 2026), not only on those whose match was kept: a cleared match
    # kept the tags its wrong article leaked, and the next lookup counted them again. A line is cut at leaked markup,
    # and what is left goes if it is prose -- a sentence break, an article's "is a ... station", or over eighty
    # characters -- since a tag is a word or a phrase.
    def tagLines(sTags):
        lsOut = []
        for t in (sTags or "").split("\n"):
            s = plainField(re.split(r"\}\}|'''|\{\{", t)[0])
            if s and not (re.search(r"[a-z]\. [A-Z]", s) or re.search(r"\bis an? .*\b(radio|station)\b", s) or len(s) > 80): lsOut.append(s)
        return "\n".join(lsOut)
    # YOUR NOTES AND TAGS ARE YOURS (12 October 2026, from an audit): nothing records which lines of notes and tags
    # came from an article and which a listener wrote, and the audit showed a note beginning with a call sign and a
    # long tag of the listener's being cut. So on a person's own copy the repair clears only the catalog's own fields
    # -- call sign, frequency, city, owner, format, article -- and leaves notes and tags exactly as they are. The
    # shipped template holds nobody's words, and there the text is still cleaned.
    for iId, sTags in (c.execute("select station_id, tags from stations where tags like '%}}%' or tags like '%{{%' or tags like '%''''''%' or length(tags) > 80").fetchall() if bTemplate else []):
        sNew = tagLines(sTags)
        if sNew != (sTags or ""):
            c.execute("update stations set tags=? where station_id=?", (sNew, iId)); dCount["markup"] += 1
    # ONE NAME PER COUNTRY, AND NO PLACEHOLDER GENRES (12 October 2026, from an audit). The SomaFM channels and the
    # ACB Media and NFB Radio streams said "United States" with no country code, while Radio Browser says "The United
    # States Of America" with code US, so filtering on either name missed the other. And "tbd" stood in some genres
    # where the source had none. Both are catalog fields, not the listener's, so this is safe on any copy.
    if "countrycode" in lsCols:
        sUs = (c.execute("select country from stations where countrycode = 'US' and coalesce(country,'') <> '' group by country order by count(*) desc limit 1").fetchone() or ["The United States Of America"])[0]
        dCount["markup"] += c.execute("update stations set country = ?, countrycode = 'US' where country = 'United States' and coalesce(countrycode,'') = ''", (sUs,)).rowcount
    if "genre" in lsCols:
        for iId, sGenre in c.execute("select station_id, genre from stations where lower(genre) like '%tbd%'").fetchall():
            sNew = ", ".join(s.strip() for s in (sGenre or "").split(",") if s.strip() and s.strip().lower() != "tbd")
            if sNew != (sGenre or ""):
                c.execute("update stations set genre = ? where station_id = ?", (sNew, iId)); dCount["markup"] += 1
    sTouched = "coalesce(rating,'') <> '' or coalesce(plays,'') not in ('', '0') or coalesce(status,'') not in ('', 'untried', 'dead') or coalesce(marked, 0) <> 0 or source = 'mine'"
    # A STATION WITH LINKS IS TOUCHED (12 October 2026, from an audit): deleting a duplicate fires its maps trigger and
    # takes its links with it, so a group where any row is linked is left as it is.
    if c.execute("select count(*) from sqlite_master where type = 'table' and name = 'maps'").fetchone()[0]:
        sTouched += " or prime in (select prime1 from maps where tbl1 = 'stations') or prime in (select prime2 from maps where tbl2 = 'stations')"
    for sUrl, sKey in c.execute("select stream_url, lower(trim(name)) from stations group by 1, 2 having count(*) > 1").fetchall():
        lsRows = c.execute("select station_id, tags, notes, (" + sTouched + ") from stations where stream_url = ? and lower(trim(name)) = ? order by cast(votes as integer) desc, station_id", (sUrl, sKey)).fetchall()
        if any(r[3] for r in lsRows): continue
        iKeep, sTags, sNotes = lsRows[0][0], lsRows[0][1] or "", lsRows[0][2] or ""
        for r in lsRows[1:]:
            sTags = addLines(sTags, [t for t in (r[1] or "").split("\n") if t.strip()])
            if r[2] and r[2].strip() and r[2].strip()[:60].lower() not in sNotes.lower(): sNotes = (sNotes + "\n\n" if sNotes else "") + r[2].strip()
            c.execute("delete from stations where station_id = ?", (r[0],)); dCount["merged"] += 1
        c.execute("update stations set tags=?, notes=? where station_id=?", (sTags, sNotes, iKeep))
    if bTemplate:
        dCount["mine"] = c.execute("delete from stations where source = 'mine'").rowcount
        lsPersonal = [s for s in ("plays", "last_played", "rating") if s in lsCols]
        if lsPersonal:
            dCount["personal"] = c.execute("update stations set " + ", ".join(s + " = NULL" for s in lsPersonal) + " where " + " or ".join("coalesce(%s,'') not in ('', '0')" % s for s in lsPersonal)).rowcount
        dCount["nameless"] = c.execute("delete from stations where coalesce(trim(name), '') = ''").rowcount
        for (sTable,) in c.execute("select name from sqlite_master where type = 'table' and name = 'sqlean_define'").fetchall():
            if c.execute("select count(*) from " + sTable).fetchone()[0] == 0: c.execute("drop table " + sTable); logLine("dropped the empty table " + sTable)
    c.commit()
    if bTemplate:
        c.execute("vacuum")
        # ONE FILE, NOTHING BESIDE IT (11 October 2026): the shipped copy is in the ordinary journal mode, not write-ahead
        # logging, so everything is in RadioTrail.db itself and no -wal or -shm file is needed or left behind.
        c.execute("pragma journal_mode=DELETE")
    c.close()
    say("Repaired: %d wrong article matches cleared for a fresh lookup, %d fields freed of markup, %d duplicate rows merged%s." % (
        dCount["cleared"], dCount["markup"], dCount["merged"],
        ("; for the shipped copy, %d of your own stations, %d plays or ratings and %d nameless rows removed" % (dCount["mine"], dCount["personal"], dCount["nameless"])) if bTemplate else ""))
    logLine("clean " + repr(dCount))
    return 0


def affiliations(sDb, logLine, say):
    """--affiliations: WHICH NETWORKS CARRY A STATION (11 October 2026). A listener asked for the station carrying his
    team's game, and a station rarely says so itself: KTMT in Medford carries the Seahawks, yet nothing in its stream
    or home page says Seahawks. Wikipedia's station infobox has an affiliations field -- "Seahawks Radio Network",
    "ESPN Radio" -- and step 4 now files it as tags. This step reads it for stations step 4 already matched, fifty
    articles a request, one request a second, so a catalog looked up before 11 October gains its networks in minutes
    rather than hours. Each affiliation becomes a tag, so Any Field and Keywords find "Seahawks"; nothing else
    changes, and a second run adds nothing new."""
    c = sqlite3.connect(sDb)
    c.execute("PRAGMA journal_mode=WAL")
    lsCols = set(r[1] for r in c.execute("pragma table_info(stations)"))
    if "wikipedia" not in lsCols:
        say("This copy has no wikipedia column. Run fetchStations --fresh first."); c.close(); return 1
    dByTitle = {}
    for iId, sUrl, sTags in c.execute("select station_id, wikipedia, tags from stations where wikipedia like 'http%'"):
        sTitle = urllib.parse.unquote(sUrl.rsplit("/wiki/", 1)[-1]).replace("_", " ")
        dByTitle.setdefault(sTitle, []).append(iId)
    lsTitles = sorted(dByTitle)
    if not lsTitles:
        say("No station here has its Wikipedia article yet; run fetchStations --official-only first."); c.close(); return 0
    iMinutes = max(1, -(-(len(lsTitles) // 50 + 1) * 2 // 60))
    say("Reading the affiliations of %d Wikipedia article%s, fifty a request, one request a second: about %d minute%s." % (len(lsTitles), "" if len(lsTitles) == 1 else "s", iMinutes, "" if iMinutes == 1 else "s"))
    iStations = iTags = iArticles = 0
    for iStart in range(0, len(lsTitles), 50):
        lsBatch = lsTitles[iStart:iStart + 50]
        dParams = {"action": "query", "prop": "revisions", "rvprop": "content", "rvslots": "main", "titles": "|".join(lsBatch)}
        lsPages = None
        for iTry in range(3):
            try:
                lsPages, sWhy = wikiPages(dParams)
                if sWhy: logLine("affiliations batch %d: %s" % (iStart // 50 + 1, sWhy))
                break
            except WikiRefused as oRefused:
                logLine("affiliations batch %d refused (%s); waiting %d seconds, as asked" % (iStart // 50 + 1, oRefused, oRefused.iWait)); time.sleep(oRefused.iWait)
        if lsPages is None:
            say("Wikipedia kept refusing, so the step stopped; run fetchStations --affiliations again later. Nothing is lost."); break
        for oPage in lsPages:
            sText = ""
            try: sText = oPage["revisions"][0]["slots"]["main"]["*"]
            except Exception:
                try: sText = oPage["revisions"][0].get("content", "") or oPage["revisions"][0].get("*", "")
                except Exception: pass
            lsItems = affiliationItems(infoboxFields(sText).get("affiliations", "") or infoboxFields(sText).get("network", ""))
            if not lsItems: continue
            iArticles += 1
            for iId in dByTitle.get(oPage.get("title", ""), []):
                sHave = c.execute("select tags from stations where station_id=?", (iId,)).fetchone()[0] or ""
                sNew = addLines(sHave, lsItems)
                if sNew != sHave:
                    c.execute("update stations set tags=? where station_id=?", (sNew, iId))
                    iStations += 1; iTags += len(sNew.split("\n")) - len([s for s in sHave.split("\n") if s.strip()])
                    logLine("affiliations %d %s: %s" % (iId, oPage.get("title", ""), "; ".join(lsItems)))
        c.commit()
        time.sleep(c_dWikiPause)
    c.close()
    say("Affiliations: %d articles name networks; %d stations gained %d tags." % (iArticles, iStations, iTags))
    return 0


def importPlaylist(sDb, sPath, logLine, say):
    """The stations in an .m3u, .m3u8 or .pls file, added as the listener's
    own. A station whose name and address are already there is left alone."""
    if not os.path.isfile(sPath):
        say("No playlist at " + sPath); return 1
    sText = open(sPath, "rb").read().decode("utf-8-sig", "replace").replace("\r\n", "\n")
    lsRows = []
    if sPath.lower().endswith(".pls"):
        dFiles = {}; dTitles = {}
        for m in re.finditer(r"^\s*File(\d+)\s*=\s*(\S+)", sText, re.M | re.I): dFiles[m.group(1)] = m.group(2).strip()
        for m in re.finditer(r"^\s*Title(\d+)\s*=\s*(.+)$", sText, re.M | re.I): dTitles[m.group(1)] = m.group(2).strip()
        for k in sorted(dFiles, key=int): lsRows.append((dTitles.get(k, dFiles[k]), dFiles[k]))
    else:
        sTitle = ""
        for sLine in sText.split("\n"):
            sLine = sLine.strip()
            if not sLine: continue
            if sLine.upper().startswith("#EXTINF"):
                sTitle = sLine.split(",", 1)[1].strip() if "," in sLine else ""
            elif sLine.startswith("#"):
                continue
            else:
                lsRows.append((sTitle or sLine, sLine)); sTitle = ""
    lsRows = [(n, u) for n, u in lsRows if u.lower().startswith("http")]
    if not lsRows:
        say("No stream addresses in " + os.path.basename(sPath)); return 1
    c = sqlite3.connect(sDb)
    iAdded = iHad = 0
    for sName, sUrl in lsRows:
        if c.execute("select 1 from stations where stream_url = ? or (name = ? and source = 'mine')", (sUrl, sName)).fetchone():
            iHad += 1; continue
        c.execute("insert into stations (name, stream_url, source, source_id, status, last_seen) values (?,?,?,?,?,?)",
                  (sName, sUrl, "mine", "", "untried", datetime.date.today().isoformat()))
        iAdded += 1
        logLine("imported: %s -> %s" % (sName, sUrl))
    c.commit(); c.close()
    say("%d stations added from %s; %d were already here." % (iAdded, os.path.basename(sPath), iHad))
    return 0

def markDropped(sDb, sNow, logLine, say):
    """After a full catalog fetch: a Radio Browser station not seen today is
    dead -- unless the listener had set its status."""
    c = sqlite3.connect(sDb)
    iDead = c.execute("select count(*) from stations where source = 'Radio Browser' and last_seen < ? and coalesce(status,'') in ('', 'untried')", (sNow,)).fetchone()[0]
    c.execute("update stations set status = 'dead', edited = CURRENT_TIMESTAMP where source = 'Radio Browser' and last_seen < ? and coalesce(status,'') in ('', 'untried')", (sNow,))
    c.commit(); c.close()
    if iDead: say("%d stations the catalog no longer lists are marked dead; they stay in the table." % iDead)
    logLine("dropped marked dead: %d" % iDead)

def assess(sDb, logLine, say, bReadOnly=False):
    """How the database measures against what it is for: comprehensive --
    the whole catalog, recently; consistent -- every row playable, no litter;
    coherent -- the parts agree. Says a short report and returns what is
    still needed: catalog, enrich, official, each True or False."""
    c = sqlite3.connect(sDb)
    lsCols = set(r[1] for r in c.execute("pragma table_info(stations)"))
    def q(sSql):
        try: return c.execute(sSql).fetchone()[0] or 0
        except Exception: return 0
    iAll = q("select count(*) from stations")
    iPlayable = q("select count(*) from stations where stream_url like 'http%'")
    iDupUrl = q("select count(*) from (select stream_url from stations where stream_url <> '' group by stream_url having count(*) > 1)")
    sLastSeen = str(q("select max(last_seen) from stations where source = 'Radio Browser'") or "")
    iDaysOld = 999
    try: iDaysOld = (datetime.date.today() - datetime.date.fromisoformat(sLastSeen[:10])).days
    except Exception: pass
    iProbed = q("select count(*) from stations where coalesce(probed,'') <> ''") if "probed" in lsCols else 0
    iAnswered = q("select count(*) from stations where coalesce(slogan,'') <> '' or coalesce(descrip,'') <> ''") if "slogan" in lsCols else 0
    iTags = q("select count(*) from stations where coalesce(tags,'') <> ''")
    iNotes = q("select count(*) from stations where coalesce(notes,'') <> ''")
    iLitterGenre = q("select count(*) from stations where genre like '%://%'")
    iNumericName = q("select count(*) from stations where name glob '[0-9]*' and name not glob '*[a-zA-Z]*'")
    # A STATE WITHOUT A COUNTRY IS REPAIRED FROM THE CODE. The catalog gives a
    # country code for nearly every station, and the catalog itself says what
    # each code's country is called: the name most of that code's stations
    # carry. A record with a code and no name gets the name, and the check's
    # own number for this goes down instead of being reported every time.
    # REPORT-ONLY CHANGES NOTHING (12 October 2026, from an audit): --report reached this repair through assess and wrote
    # country names, so a run asked only to report could change the database. bReadOnly skips it.
    if bReadOnly: logLine("country repair skipped: report only")
    try:
        if bReadOnly: raise StopIteration
        dCode = {}
        for sCode, sName, iCount in c.execute("select countrycode, country, count(*) from stations where coalesce(countrycode,'') <> '' and coalesce(country,'') <> '' group by countrycode, country order by count(*)"):
            dCode[sCode] = sName  # the last row for a code is its most common name
        iRepaired = 0
        for iId, sCode in c.execute("select station_id, countrycode from stations where coalesce(country,'') = '' and coalesce(countrycode,'') <> ''").fetchall():
            if sCode in dCode:
                c.execute("update stations set country = ? where station_id = ?", (dCode[sCode], iId)); iRepaired += 1
        if iRepaired: c.commit(); say("  %d stations had a country code and no country name; the name is filled in from the code." % iRepaired)
    except StopIteration:
        pass
    except Exception as oError:
        logLine("country repair skipped: " + str(oError))
    iStateNoCountry = q("select count(*) from stations where coalesce(state,'') <> '' and coalesce(country,'') = ''")
    lsCall = []; iOfficial = 0; iCallNames = 0; iDash = 0
    if "call_sign" in lsCols:
        for r in c.execute("select name, coalesce(wikipedia,'') from stations where stream_url <> '' and countrycode in ('US','CA')"):
            if c_reCallSign.search(r[0] or ""):
                iCallNames += 1
                if r[1] == "": lsCall.append(r[0])
                elif r[1] == "-": iDash += 1
                else: iOfficial += 1
    c.close()
    def pct(a, b): return "%d%%" % (100 * a // b) if b else "0%"
    say("Quality check of " + os.path.basename(sDb) + ":")
    say("  Comprehensive: %d stations, catalog last fetched %s (%s)." % (iAll, sLastSeen[:10] or "never", ("%d days ago" % iDaysOld) if iDaysOld < 999 else "unknown"))
    say("  Consistent: %s playable (%d); %d stream addresses shared by more than one station; %d genres with litter; %d names that are only numbers." % (pct(iPlayable, iAll), iPlayable, iDupUrl, iLitterGenre, iNumericName))
    say("  Coherent: %d stations with a state but no country." % iStateNoCountry)
    say("  What the stations say: %s asked (%d), %s answered (%d); %s with tags, %s with notes." % (pct(iProbed, iAll), iProbed, pct(iAnswered, iAll), iAnswered, pct(iTags, iAll), pct(iNotes, iAll)))
    say("  Official record: %d US and Canadian names carry a call sign; %d have their record; %d found no safe article; %d not yet looked up. (--official-only --again asks about the unmatched ones again.)" % (iCallNames, iOfficial, iDash, len(lsCall)))
    bCatalog = iAll < 1000 or iDaysOld > 7
    bEnrich = (iAll - iProbed) > 0
    bOfficial = len(lsCall) > 0
    lsNeed = [s for s, b in (("the catalog", bCatalog), ("asking %d stations" % (iAll - iProbed), bEnrich), ("%d official records" % len(lsCall), bOfficial)) if b]
    say("  Needed: " + (", ".join(lsNeed) if lsNeed else "nothing. The database is as complete as its sources allow."))
    return {"catalog": bCatalog, "enrich": bEnrich, "official": bOfficial, "count": iAll}

def official(sDb, sCountry, iLimit, bAgain, logLine, say):
    """Step 4: the record behind the name, from Wikipedia's infobox."""
    c = sqlite3.connect(sDb)
    c.execute("PRAGMA journal_mode=WAL")
    lsCols = set(r[1] for r in c.execute("pragma table_info(stations)"))
    if "call_sign" not in lsCols:
        say("This copy has no columns for the official record. Run fetchStations --fresh first."); c.close(); return 1
    sWhere = "stream_url <> ''"
    lsArgs = []
    if not bAgain: sWhere += " and (wikipedia is null or wikipedia = '')"
    if sCountry: sWhere += " and country = ?"; lsArgs.append(sCountry)
    # Call signs are a North American habit in station names; a "KISS FM" in
    # Hamburg is not a licence. Only United States and Canadian stations are
    # looked up.
    sWhere += " and countrycode in ('US', 'CA')"
    lsRows = [r for r in c.execute("select station_id, name, notes, tags from stations where " + sWhere + " order by cast(votes as integer) desc", lsArgs)
              if c_reCallSign.search(r[1] or "")]
    if iLimit: lsRows = lsRows[:iLimit]
    if not lsRows:
        say("No station here with a call sign in its name is still unlooked-up."); c.close(); return 0
    say("Looking up %d stations with a call sign in their name on Wikipedia, one a second as it asks. About %d minutes; stop any time and the next run carries on." % (len(lsRows), max(1, len(lsRows) * 11 // 10 // 60)))
    oLock = threading.Lock(); lsOut = []; lsQueue = list(lsRows); iWritten = [0]; iFilled = [0]; bRefused = [False]
    def flush():
        with oLock:
            lsBatch = list(lsOut); del lsOut[:]
        for r, d, sWhy in lsBatch:
            # WHAT IS THERE NOW, NOT WHAT WAS READ AN HOUR AGO (12 October 2026, from an audit): notes and tags are read
            # again just before the write, so an edit made in DbDo while the lookups ran is kept, not written over.
            iId = r[0]
            sNotes, sTags = [v or "" for v in c.execute("select notes, tags from stations where station_id=?", (iId,)).fetchone()]
            if d:
                iFilled[0] += 1
                lsTagNew = [s.strip() for s in re.split(r"[,/;]", d.get("format", "")) if s.strip()] + affiliationItems(d.get("affiliations", ""))
                sNotesNew = sNotes
                if d.get("extract") and d["extract"][:60].lower() not in sNotes.lower():
                    sNotesNew = (sNotes + "\n\n" if sNotes else "") + d["extract"]
                c.execute("update stations set call_sign=?, frequency=?, city=?, owner=?, format=?, wikipedia=?, tags=?, notes=?, edited=CURRENT_TIMESTAMP where station_id=?",
                          (d.get("call_sign",""), d.get("frequency",""), d.get("city",""), d.get("owner",""), d.get("format",""), d.get("wikipedia",""),
                           addLines(sTags, lsTagNew), sNotesNew, iId))
            elif d is not None:
                c.execute("update stations set wikipedia='-' where station_id=?", (iId,))
            logLine("official %d %s: %s" % (iId, (r[1] or "")[:40], ("; ".join("%s=%s" % (k, str(v)[:60]) for k, v in d.items()) + " [" + sWhy + "]") if d else ("refused, left for next time" if d is None else "no safe match [" + sWhy + "]")))
            iWritten[0] += 1
        if lsBatch: c.commit()
    def worker():
        while True:
            with oLock:
                if not lsQueue: return
                r = lsQueue.pop()
            sWhy = ""
            try:
                d, sWhy = officialStation(r[1] or "")
            except WikiRefused as oRefused:
                d = None; sWhy = "refused: %s; waited %d seconds, as asked" % (oRefused, oRefused.iWait)
                time.sleep(oRefused.iWait)
                with oLock: bRefused[0] = True
            with oLock: lsOut.append((r, d, sWhy))
            time.sleep(c_dWikiPause)
    lsThreads = [threading.Thread(target=worker, daemon=True) for _ in range(c_iWikiThreads)]
    for o in lsThreads: o.start()
    iSaid = 0
    try:
        while any(o.is_alive() for o in lsThreads):
            time.sleep(5); flush()
            if iWritten[0] - iSaid >= 200: say("  %d of %d" % (iWritten[0], len(lsRows))); iSaid = iWritten[0]
        flush()
    except KeyboardInterrupt:
        with oLock: del lsQueue[:]
        flush(); c.close()
        say("Stopped after %d stations; what was found is kept. Run fetchStations again to carry on." % iWritten[0]); return 0
    c.close()
    say("%d stations have their record from Wikipedia; %d had no safe match and were left as they were." % (iFilled[0], iWritten[0] - iFilled[0]))
    if bRefused[0]: say("Wikipedia refused some requests; those stations were left for the next run.")
    return 0

def probeStation(sUrl, sHomepage):
    """What one station says about itself: ICY headers off its stream, and the
    title and description off its home page. Returns a dict; keys absent when
    nothing was found."""
    d = {}
    if sUrl:
        try:
            oReq = urllib.request.Request(sUrl, headers={"User-Agent": c_sAgent, "Icy-MetaData": "1"})
            with urllib.request.urlopen(oReq, timeout=c_iProbeSeconds) as oResp:
                h = oResp.headers
                for sKey, sField in (("icy-name", "icy_name"), ("icy-description", "icy_description"), ("icy-genre", "icy_genre"), ("icy-url", "icy_url")):
                    sVal = (h.get(sKey) or "").strip()
                    if sVal: d[sField] = sVal
                sType = (h.get("Content-Type") or "").strip()
                if sType: d["content_type"] = sType
        except Exception:
            pass
    sPage = sHomepage or d.get("icy_url", "")
    if sPage and sPage.lower().startswith("http"):
        try:
            oReq = urllib.request.Request(sPage, headers={"User-Agent": c_sAgent})
            with urllib.request.urlopen(oReq, timeout=c_iProbeSeconds) as oResp:
                sHtml = oResp.read(65536).decode("utf-8", "replace")
            m = re.search(r"<title[^>]*>(.*?)</title>", sHtml, re.I | re.S)
            if m: d["page_title"] = html.unescape(re.sub(r"\s+", " ", m.group(1))).strip()[:200]
            m = re.search(r'<meta[^>]+(?:name|property)=["\'](?:description|og:description)["\'][^>]+content=["\']([^"\']{3,500})', sHtml, re.I)
            if not m: m = re.search(r'<meta[^>]+content=["\']([^"\']{3,500})["\'][^>]+(?:name|property)=["\'](?:description|og:description)', sHtml, re.I)
            if m: d["page_description"] = html.unescape(re.sub(r"\s+", " ", m.group(1))).strip()
            m = re.search(r'<meta[^>]+name=["\']keywords["\'][^>]+content=["\']([^"\']{3,500})', sHtml, re.I)
            if m: d["page_keywords"] = html.unescape(m.group(1)).strip()
        except Exception:
            pass
    return d

def enrich(sDb, sCountry, iLimit, bAgain, logLine, say):
    """Probe stations and file what they say about themselves, never touching
    the listener's fields."""
    c = sqlite3.connect(sDb)
    c.execute("PRAGMA journal_mode=WAL")
    lsCols = set(r[1] for r in c.execute("pragma table_info(stations)"))
    if "slogan" not in lsCols or "probed" not in lsCols:
        say("This copy of RadioTrail is older than the template and has nowhere to put what a station says. Run rebuildRadioTrail, or fetchStations --fresh, first.")
        c.close(); return 1
    sWhere = "stream_url <> ''"
    lsArgs = []
    if not bAgain: sWhere += " and (probed is null or probed = '')"
    if sCountry: sWhere += " and country = ?"; lsArgs.append(sCountry)
    sSql = "select station_id, stream_url, homepage, genre, slogan, descrip, tags, notes from stations where " + sWhere + " order by cast(votes as integer) desc"
    if iLimit: sSql += " limit %d" % iLimit
    lsRows = c.execute(sSql, lsArgs).fetchall()
    if not lsRows:
        say("Nothing to probe: every station here has been asked already. Add --again to ask them again."); c.close(); return 0
    say("Asking %d stations what they say about themselves, %d at a time. About %d minutes." % (len(lsRows), c_iProbeThreads, max(1, len(lsRows) * c_iProbeSeconds // c_iProbeThreads // 60)))
    oLock = threading.Lock(); lsOut = []; lsQueue = list(lsRows)
    sNow = datetime.datetime.now().strftime("%Y-%m-%d")
    iFilled = [0]; iWritten = [0]
    def flush():
        """Write what has come back so far. Called every few seconds, so a
        stop at any moment keeps everything asked up to then."""
        with oLock:
            lsBatch = list(lsOut); del lsOut[:]
        for r, d in lsBatch:
            # The fields are read again just before the write, so an edit made while the probes ran is kept.
            iId = r[0]
            sGenre, sSlogan, sDescrip, sTags, sNotes = [v or "" for v in c.execute("select genre, slogan, descrip, tags, notes from stations where station_id=?", (iId,)).fetchone()]
            lsParts = []
            for k in ("icy_description", "page_title", "page_description"):
                v = d.get(k, "")
                if v and v.lower() not in sDescrip.lower() and v not in lsParts: lsParts.append(v)
            sSloganNew = sSlogan or d.get("icy_description", "")[:200]
            sDescripNew = sDescrip
            if lsParts: sDescripNew = (sDescrip + "\n\n" if sDescrip else "") + "\n".join(lsParts)
            sGenreNew = sGenre
            for sWord in re.split(r"[,/;|]", d.get("icy_genre", "")):
                sWord = sWord.strip().lower()
                if sWord and sWord not in sGenreNew.lower(): sGenreNew = (sGenreNew + ", " if sGenreNew else "") + sWord
            if d: iFilled[0] += 1
            logLine("probe %d: %s" % (iId, "; ".join("%s=%s" % (k, v[:80]) for k, v in d.items()) or "nothing"))
            lsTagNew = [w.strip().lower() for w in re.split(r"[,/;|]", d.get("icy_genre", "")) if w.strip()]
            lsTagNew += [w.strip() for w in re.split(r"[,;]", d.get("page_keywords", "")) if w.strip() and len(w.strip()) <= 40][:20]
            sNotesNew = sNotes
            sProse = d.get("page_description", "")
            if sProse and sProse[:60].lower() not in sNotes.lower(): sNotesNew = (sNotes + "\n\n" if sNotes else "") + sProse
            c.execute("update stations set slogan=?, descrip=?, genre=?, tags=?, notes=?, probed=?, edited=CURRENT_TIMESTAMP where station_id=?",
                      (sSloganNew, sDescripNew, sGenreNew, addLines(sTags, lsTagNew), sNotesNew, sNow, iId))
            iWritten[0] += 1
        if lsBatch: c.commit()
    def worker():
        while True:
            with oLock:
                if not lsQueue: return
                r = lsQueue.pop()
            d = probeStation(r[1], r[2])
            with oLock: lsOut.append((r, d))
    lsThreads = [threading.Thread(target=worker, daemon=True) for _ in range(c_iProbeThreads)]
    for o in lsThreads: o.start()
    iSaid = 0
    try:
        while any(o.is_alive() for o in lsThreads):
            time.sleep(5)
            flush()
            if iWritten[0] - iSaid >= 500: say("  %d of %d" % (iWritten[0], len(lsRows))); iSaid = iWritten[0]
        flush()
    except KeyboardInterrupt:
        with oLock: del lsQueue[:]
        flush(); c.close()
        say("Stopped after %d stations; what they said is kept. Run fetchStations again to carry on with the rest." % iWritten[0])
        return 0
    c.close()
    say("%d stations answered with something; %d said nothing. Keywords now finds what they said." % (iFilled[0], iWritten[0] - iFilled[0]))
    return 0

def resolveMirrors(logLine):
    """Every current Radio Browser mirror, from DNS, shuffled; the known list
    when DNS gives nothing."""
    lsHosts = []
    try:
        lsSeen = set()
        for _oFamily, _oType, _oProto, _sCanon, oAddr in socket.getaddrinfo(c_sAllHosts, 443, 0, socket.SOCK_STREAM):
            sIp = str(oAddr[0])
            if sIp in lsSeen: continue
            lsSeen.add(sIp)
            try:
                sHost = socket.gethostbyaddr(sIp)[0]
            except OSError:
                continue
            if sHost and sHost not in lsHosts: lsHosts.append(sHost)
    except OSError as oError:
        logLine("DNS for %s failed: %s" % (c_sAllHosts, oError))
    lsServers = ["https://" + s for s in lsHosts]
    random.shuffle(lsServers)
    if not lsServers: lsServers = list(c_lsServers)
    logLine("mirrors: " + ", ".join(lsServers))
    return lsServers

def firstStreamFromPls(sText):
    """File1=... out of a .pls playlist, or empty."""
    oHit = re.search(r"^\s*File1\s*=\s*(\S+)", sText, re.M | re.I)
    return oHit.group(1).strip() if oHit else ""

def fetchSomaFm(logLine, say):
    """The SomaFM channels as rows for the stations table."""
    oReq = urllib.request.Request(c_sSomaChannels, headers={"User-Agent": c_sAgent, "Accept": "application/json"})
    with urllib.request.urlopen(oReq, timeout=30) as oResp:
        dDoc = json.loads(oResp.read().decode("utf-8"))
    lsRows = []
    for d in dDoc.get("channels", []):
        sTitle = (d.get("title") or "").strip()
        sId = (d.get("id") or "").strip()
        if not sTitle or not sId: continue
        lsPlay = [p for p in d.get("playlists", []) if isinstance(p, dict)]
        def rank(p):
            sF = str(p.get("format", "")).lower(); sQ = str(p.get("quality", "")).lower()
            return (c_lsSomaFormats.index(sF) if sF in c_lsSomaFormats else 9,
                    c_lsSomaQuality.index(sQ) if sQ in c_lsSomaQuality else 9)
        sStream = ""
        if lsPlay:
            pBest = min(lsPlay, key=rank)
            sPls = str(pBest.get("url", "")).strip()
            if sPls:
                sStream = sPls
                try:
                    oReq = urllib.request.Request(sPls, headers={"User-Agent": c_sAgent})
                    with urllib.request.urlopen(oReq, timeout=20) as oResp:
                        sDirect = firstStreamFromPls(oResp.read().decode("utf-8", "replace"))
                    if sDirect: sStream = sDirect
                except Exception as oError:
                    logLine("pls for %s not resolved, keeping the playlist address: %s" % (sId, oError))
        if not sStream: continue
        lsRows.append({
            "name": "SomaFM " + sTitle, "stream_url": sStream, "homepage": "https://somafm.com/" + sId + "/",
            "country": "The United States Of America", "countrycode": "US", "state": "", "language": "english",
            "genre": ", ".join(s.strip() for s in str(d.get("genre", "")).split("|") if s.strip()),
            "codec": "MP3", "bitrate": "", "votes": "", "clicks": str(d.get("listeners") or ""),
            "source": "SomaFM", "source_id": "somafm:" + sId, "descrip": (d.get("description") or "").strip(),
        })
    say("SomaFM: %d channels" % len(lsRows))
    return lsRows
c_iPage = 10000
c_dPause = 0.5

def templatePath():
    return os.path.join(os.path.dirname(os.path.abspath(__file__)), "RadioTrail.db")

def buildFromSchema(sDb, sSchema):
    """A new, empty RadioTrail at sDb built from RadioTrail.schema: its tables, indexes, triggers, lookups and views,
    and no stations. THE TEMPLATE CAN BE MADE FROM SOURCE (12 October 2026): RadioTrail.db was then kept out of
    git, and an audit found a clean checkout had no way to make one, since this script could only copy it. The
    repository carries the catalog now too, and the schema remains the way to build one from source. The build writes the schema from the real database (scripts\\checkTemplates --schema RadioTrail), and
    with no database to copy, the catalog fetch that follows fills this one."""
    import sqlite3
    os.makedirs(os.path.dirname(sDb), exist_ok=True)
    with open(sSchema, encoding="utf-8-sig") as f: sText = f.read()
    cNew = sqlite3.connect(sDb)
    try:
        cNew.executescript(sText)
        cNew.commit()
    finally:
        cNew.close()
    print("Built an empty RadioTrail at " + sDb + " from " + os.path.basename(sSchema) + "; the stations come next.")
    return True

def copyCompanions(sDb):
    """The template's companion files beside a new copy at sDb. RadioTrail.inix, its settings, comes with every new copy
    as it always has; report.inix, the report definitions, comes only when the folder has none, so a report the
    listener has edited is kept. BOTH PATHS BRING THEM (12 October 2026, from an audit): a database built from the
    schema returned before the settings were copied, so it opened without them."""
    import shutil
    sFrom, sTo = os.path.dirname(templatePath()), os.path.dirname(sDb)
    for sName, bAlways in (("RadioTrail.inix", True), ("report.inix", False)):
        sSource, sTarget = os.path.join(sFrom, sName), os.path.join(sTo, sName)
        if os.path.isfile(sSource) and os.path.normcase(os.path.abspath(sSource)) != os.path.normcase(os.path.abspath(sTarget)) and (bAlways or not os.path.isfile(sTarget)):
            shutil.copy2(sSource, sTarget)
    return True

def copyTemplate(sDb):
    """A new copy of the template at sDb, with its companion files beside it. With no template database to copy, one is
    built from RadioTrail.schema beside this script."""
    import shutil
    sTemplate = templatePath()
    if not os.path.isfile(sTemplate):
        sSchema = os.path.join(os.path.dirname(sTemplate), "RadioTrail.schema")
        if not os.path.isfile(sSchema):
            print("No RadioTrail.db beside this script to copy from, and no RadioTrail.schema to build one from."); return False
        if not buildFromSchema(sDb, sSchema): return False
        copyCompanions(sDb)
        return True
    os.makedirs(os.path.dirname(sDb), exist_ok=True)
    shutil.copy2(sTemplate, sDb)
    copyCompanions(sDb)
    print("Made your copy of RadioTrail at " + sDb)
    return True

def setAside(sDb):
    """The copy at sDb becomes RadioTrail-old-<date and time>.db beside it.
    NOTHING SET ASIDE IS EVER DELETED (9 October 2026): this once named every
    old copy RadioTrail-old.db and removed the previous one, so a second
    set-aside could delete a database of sixty thousand stations kept from the
    first. Each now keeps its own dated name. Returns True when it was set
    aside, False when it was not --
    and the caller must not make a new copy over a database that is still
    there. THE WRITE-AHEAD LOG IS NEVER DELETED: a note committed a moment
    ago may live only there. The database is checkpointed, which folds the
    log into the main file and removes it, before the file is moved (audit
    of 8 October 2026)."""
    sOld = os.path.join(os.path.dirname(sDb), "RadioTrail-old-" + datetime.datetime.now().strftime("%Y%m%d-%H%M%S") + ".db")
    try:
        c = sqlite3.connect(sDb)
        try: c.execute("PRAGMA wal_checkpoint(TRUNCATE)")
        finally: c.close()
    except Exception as oError:
        print("Could not checkpoint the database before setting it aside: " + str(oError)); return False
    try:
        for sSide in ("-wal", "-shm"):
            if os.path.isfile(sDb + sSide):
                # Still here after the checkpoint: it goes with its database, not away.
                os.replace(sDb + sSide, sOld + sSide)
        os.rename(sDb, sOld)
        print("The old copy is now " + sOld)
        return True
    except Exception as oError:
        print("Could not set the old copy aside: " + str(oError)); return False

def migrateShape(sDb):
    """THE COPY CATCHES UP IN PLACE (12 October 2026, from an audit). An older copy was set aside whole and a new one
    made, on the belief that nobody's work was in it -- but its notes, favourites, marks and added stations are work,
    and a backup nobody finds is not keeping them. A copy whose stations table has rows now gains the template's missing
    columns in place, each with the template's type and default, after a dated backup is made. True when the copy now
    has the template's shape; False, with nothing changed, when it has no rows to keep or lacks a column that cannot be
    added in place (a stored generated column), and the caller sets it aside as before."""
    import shutil
    try:
        cT = sqlite3.connect(templatePath())
        lWant = [r for r in cT.execute("pragma table_xinfo(stations)")]
        cT.close()
        cD = sqlite3.connect(sDb)
        setHave = set(r[1] for r in cD.execute("pragma table_xinfo(stations)"))
        iRows = cD.execute("select count(*) from stations").fetchone()[0] if setHave else 0
        cD.close()
    except Exception as oError:
        print("Could not compare the copy with the template: " + str(oError)); return False
    lMissing = [r for r in lWant if r[1] not in setHave]
    if not setHave or iRows == 0: return False
    if any(r[6] for r in lMissing):
        print("The copy lacks a column that cannot be added in place (" + ", ".join(r[1] for r in lMissing if r[6]) + "), so it is set aside instead."); return False
    sBackup = os.path.join(os.path.dirname(sDb), "RadioTrail-before-update-" + datetime.datetime.now().strftime("%Y%m%d-%H%M%S-%f") + ".db")
    try:
        cD = sqlite3.connect(sDb)
        try: cD.execute("PRAGMA wal_checkpoint(TRUNCATE)")
        finally: cD.close()
        shutil.copy2(sDb, sBackup)
        cD = sqlite3.connect(sDb)
        try:
            for r in lMissing:
                sAdd = 'ALTER TABLE stations ADD COLUMN "%s" %s' % (r[1].replace('"', '""'), r[2] or "TEXT")
                if r[4] is not None: sAdd += " DEFAULT " + str(r[4])
                if r[3] and r[4] is not None: sAdd += " NOT NULL"
                cD.execute(sAdd)
            cD.commit()
        finally:
            cD.close()
    except Exception as oError:
        print("Could not bring the copy up to date in place: " + str(oError) + ". The copy was left as it was; a backup is at " + sBackup); return False
    print("Brought your copy up to date in place, keeping its %d stations: added %s. A backup of the copy as it was is at %s." % (iRows, ", ".join(r[1] for r in lMissing), sBackup))
    return True

def sameShape(sDb):
    """True when the copy's stations table has every column the template's
    has. An empty file or an older copy answers False."""
    try:
        cT = sqlite3.connect(templatePath()); lsWant = set(r[1] for r in cT.execute("pragma table_info(stations)")); cT.close()
        cD = sqlite3.connect(sDb); lsHave = set(r[1] for r in cD.execute("pragma table_info(stations)")); cD.close()
        return bool(lsHave) and lsWant <= lsHave
    except Exception:
        return False

def playlistsIn(lsFolders):
    """Every .m3u, .m3u8 and .pls file at the top of each folder, newest first."""
    lsFound = []
    for sFolder in lsFolders:
        if not sFolder or not os.path.isdir(sFolder): continue
        for sName in os.listdir(sFolder):
            sPath = os.path.join(sFolder, sName)
            if os.path.isfile(sPath) and os.path.splitext(sName)[1].lower() in (".m3u", ".m3u8", ".pls"): lsFound.append(sPath)
    return sorted(set(lsFound), key=lambda s: -os.path.getmtime(s))


def personalFolders():
    """The folders a downloaded playlist usually lands in: Downloads, Documents and Desktop, OneDrive's included."""
    sHome = os.path.expanduser("~")
    lsOut = []
    for sRoot in (sHome, os.environ.get("OneDrive", "")):
        if sRoot: lsOut += [os.path.join(sRoot, s) for s in ("Downloads", "Documents", "Desktop")]
    return lsOut


def main():
    """A PLAIN RUN MAKES THE BEST CATALOG (11 October 2026): every step the check finds wanting, then the networks each
    matched article names, so one command, with no options, leaves the copy as complete as the sources allow -- the one
    DbDo's installer ships. Any option runs as before."""
    lsArgs = sys.argv[1:]
    lsOptions = [s for s in lsArgs if s.startswith("--")]
    lsDbArgs = [s for s in lsArgs if not s.startswith("--")]
    if not lsOptions:
        print("First, repairs: wrong article matches, leaked markup and duplicate rows.")
        mainCore(lsDbArgs + ["--clean"])
    iCode = mainCore(lsArgs)
    if iCode == 0 and not lsOptions:
        print("Last, the networks each matched Wikipedia article names, such as a team's radio network.")
        iCode = mainCore([s for s in lsArgs if not s.startswith("--")] + ["--affiliations"])
        # AND THE REPAIRS ONCE MORE (11 October 2026): the lookups and the affiliations write after the first pass, and
        # the lookups switch the copy to write-ahead logging; this pass leaves the shipped copy one file, clean.
        print("Finally, the repairs once more, over what this run added.")
        mainCore(lsDbArgs + ["--clean"])
    return iCode


def mainCore(lsArgs):
    sDb = ""
    sCountry = ""
    sSource = "all"
    iLimit = 0
    bCatalogOnly = False
    bEnrichOnly = False
    bOfficialOnly = False
    bNoOfficial = False
    bReportOnly = False
    bForce = False
    bConventionsOnly = False
    bAffiliations = False
    bClean = False
    sImport = ""
    bAgain = False
    bFresh = False
    i = 0
    while i < len(lsArgs):
        sArg = lsArgs[i]
        if sArg == "--country" and i + 1 < len(lsArgs): sCountry = lsArgs[i + 1]; i += 2; continue
        if sArg == "--limit" and i + 1 < len(lsArgs): iLimit = int(lsArgs[i + 1]); i += 2; continue
        if sArg == "--source" and i + 1 < len(lsArgs): sSource = lsArgs[i + 1].lower(); i += 2; continue
        if sArg in ("--catalog-only", "--no-enrich"): bCatalogOnly = True; i += 1; continue
        if sArg in ("--enrich-only", "--enrich"): bEnrichOnly = True; i += 1; continue
        if sArg == "--official-only": bOfficialOnly = True; i += 1; continue
        if sArg == "--no-official": bNoOfficial = True; i += 1; continue
        if sArg in ("--report", "--report-only"): bReportOnly = True; i += 1; continue
        if sArg == "--force": bForce = True; i += 1; continue
        if sArg == "--import":
            # A PLAYLIST NEED NOT BE NAMED (11 October 2026): "--import" alone finds every .m3u, .m3u8 and .pls file in
            # Downloads, Documents and Desktop; a folder after it means every playlist in that folder; a file, that
            # file. (It was skipped in silence at first, then refused; neither helped someone with no file in mind.)
            if i + 1 >= len(lsArgs) or lsArgs[i + 1].startswith("--"):
                sImport = "*"; i += 1; continue
            sImport = lsArgs[i + 1]; i += 2; continue
        if sArg == "--fresh": bFresh = True; i += 1; continue
        if sArg == "--again": bAgain = True; i += 1; continue
        if sArg == "--conventions": bConventionsOnly = True; i += 1; continue
        if sArg == "--affiliations": bAffiliations = True; i += 1; continue
        if sArg == "--clean": bClean = True; i += 1; continue
        if sArg.startswith("-"):
            print("Unknown option %s, so nothing was done. Read the top of fetchStations.py for the options." % sArg); return 2
        sDb = sArg; i += 1
    if not sDb:
        # IN THE DEVELOPMENT TREE, THE TEMPLATE ITSELF (9 October 2026). Run from a project -- templates\RadioTrail
        # under a folder that holds DbDo.cs -- the stations go into the RadioTrail.db beside this script, the one the
        # installer ships, so every user gets the whole catalog and clearing DbDo's local folder cannot lose hours
        # of fetching. Run from an installed DbDo, whose folder is not writable, they go into the user's own copy
        # under %LOCALAPPDATA%, as before.
        sHere = os.path.dirname(os.path.abspath(__file__))
        sProjectDir = os.path.dirname(os.path.dirname(sHere))
        bInProject = os.path.isfile(os.path.join(sProjectDir, "DbDo.cs")) and os.access(sHere, os.W_OK)
        sDb = os.path.join(sHere, "RadioTrail.db") if bInProject else os.path.join(os.environ.get("LOCALAPPDATA", ""), "DbDo", "data", "RadioTrail", "RadioTrail.db")
        # YOUR STATIONS GO TO YOUR COPY, NEVER THE TEMPLATE (11 October 2026): the template is what DbDo's installer
        # ships, so stations imported from a person's own playlists, marked "mine", would go to everyone.
        if sImport and bInProject:
            sDb = os.path.join(os.environ.get("LOCALAPPDATA", ""), "DbDo", "data", "RadioTrail", "RadioTrail.db")
            bInProject = False
        print("Stations go into " + sDb + (" (the development tree's template)" if bInProject else " (your own copy)"))
    elif sImport and os.path.normcase(os.path.abspath(sDb)) == os.path.normcase(os.path.join(os.path.dirname(os.path.abspath(__file__)), "RadioTrail.db")):
        print("That is the template DbDo's installer ships, and your own stations must not go to everyone. Run --import without a database, and they go to your own copy.")
        return 2
    if bConventionsOnly:
        # --conventions (9 October 2026): only the Trail indexes and triggers, added where missing, then stop. DbDo's
        # build runs this on the development tree's RadioTrail, so a catalog restored or fetched before the triggers
        # existed meets the database check without a fetch.
        if not os.path.isfile(sDb):
            print("No database at " + sDb + "; nothing to do."); return 0
        cConventions = sqlite3.connect(sDb)
        try:
            iAdded = ensureConventions(cConventions); cConventions.commit()
        finally:
            cConventions.close()
        print("Trail conventions for %s: %d added." % (sDb, iAdded)); return 0
    if not os.path.isfile(sDb):
        # NO COPY YET? MAKE ONE from the template beside this script, rather
        # than sending the person away to open it in DbDo and come back.
        if not copyTemplate(sDb): return 1
    elif not sameShape(sDb) and not migrateShape(sDb):
        # THE COPY IS NOT THIS TEMPLATE'S SHAPE and could not be brought up to
        # date in place: an empty file left by a failed start, a table with no
        # rows, or a column only a new table can have. It is set aside under a
        # dated name and a clean one made. A copy with stations to keep is
        # migrated in place first (migrateShape), so this is the last resort.
        if not setAside(sDb):
            print("The copy was left as it is; nothing was replaced."); return 1
        if not copyTemplate(sDb): return 1
    # THE LOG GOES IN THE PROJECT'S logs FOLDER when the script runs from a
    # project -- C:\DbDo\logs, two levels above templates\RadioTrail -- because
    # that is where every other Homer log of the project is gathered from. When
    # the script runs from an installed copy, whose folder is not writable, the
    # log goes with the program's own under %LOCALAPPDATA%\DbDo\logs. Either way
    # the path is said FIRST, so nobody finishes a two-hour run and then looks
    # for a file that was somewhere else all along (5 October 2026).
    sProject = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    sLogDir = os.path.join(sProject, "logs")
    try:
        os.makedirs(sLogDir, exist_ok=True)
        sProbe = os.path.join(sLogDir, ".write-test"); open(sProbe, "w").close(); os.remove(sProbe)
    except Exception:
        sLogDir = os.path.join(os.environ.get("LOCALAPPDATA", ""), "DbDo", "logs")
    os.makedirs(sLogDir, exist_ok=True)
    sLog = os.path.join(sLogDir, "RadioTrail-fetch-" + datetime.datetime.now().strftime("%Y%m%d-%H%M%S") + ".log")
    oLog = open(sLog, "a", encoding="utf-8")   # two runs in one second share a log rather than one erasing the other
    def logLine(s): oLog.write(s + "\n"); oLog.flush()
    def say(s): print(s); logLine("CONSOLE: " + s)
    print("Log: " + sLog)
    logLine("fetchStations started " + datetime.datetime.now().isoformat())
    logLine("Script: " + os.path.abspath(__file__) + " | Python " + sys.version.split()[0] + " | " + sys.platform)
    logLine("Command line: " + " ".join(sys.argv) + " | working folder: " + os.getcwd())
    logLine("Database: " + sDb + " | source: " + sSource + " | country: " + (sCountry or "(all)") + " | limit: " + str(iLimit or "(none)"))

    if sImport:
        if sImport == "*": lsLists = playlistsIn(personalFolders()); sWhere = "Downloads, Documents and Desktop"
        elif os.path.isdir(sImport): lsLists = playlistsIn([sImport]); sWhere = sImport
        else: lsLists = [sImport]; sWhere = ""
        if not lsLists:
            say("No playlist was found in %s. A playlist is an .m3u, .m3u8 or .pls file; a station's Listen link often downloads one. Save it there, or name it after --import." % sWhere)
            say("The log is " + sLog); logLine("finished " + datetime.datetime.now().isoformat()); return 0
        say("Importing %d playlist%s into your own copy." % (len(lsLists), "" if len(lsLists) == 1 else "s"))
        iCode = 0
        for sList in lsLists:
            say("From " + sList + ":")
            iCode = max(iCode, importPlaylist(sDb, sList, logLine, say))
        say("The log is " + sLog); logLine("finished " + datetime.datetime.now().isoformat()); return iCode
    if bClean:
        bTemplate = os.path.normcase(os.path.abspath(sDb)) == os.path.normcase(templatePath())
        iCode = cleanCopy(sDb, bTemplate, logLine, say)
        say("The log is " + sLog); logLine("finished " + datetime.datetime.now().isoformat()); return iCode
    if bAffiliations:
        iCode = affiliations(sDb, logLine, say)
        say("The log is " + sLog); logLine("finished " + datetime.datetime.now().isoformat()); return iCode
    if bOfficialOnly:
        iCode = official(sDb, sCountry, iLimit, bAgain, logLine, say)
        say("The log is " + sLog); logLine("finished " + datetime.datetime.now().isoformat()); return iCode
    if bEnrichOnly:
        iCode = enrich(sDb, sCountry, iLimit, bAgain, logLine, say)
        say("The log is " + sLog); logLine("finished " + datetime.datetime.now().isoformat()); return iCode
    # THE CHECK COMES FIRST, AND FETCHING FOLLOWS ONLY WHERE IT SAYS. A copy
    # that already holds the whole catalog, recently fetched, every station
    # asked and every call sign looked up, needs nothing; one that is partly
    # done gets the parts it lacks; --force runs everything regardless.
    dNeed = assess(sDb, logLine, say, bReadOnly=bReportOnly)
    if bReportOnly:
        if dNeed["catalog"] or dNeed["enrich"] or dNeed["official"]:
            say("This was the report only. To fetch what is needed, run fetchStations with no arguments.")
        say("The log is " + sLog); logLine("finished " + datetime.datetime.now().isoformat()); return 0
    # THE OPTIONS COME BEFORE THE SHORTCUT (12 October 2026, from an audit): with a current catalog, this shortcut ran
    # even when --fresh asked for a clean copy, enriched under --catalog-only, and dropped --limit. --fresh now goes on
    # to the fresh start below; --catalog-only asks nothing further; --limit is passed on.
    if not bForce and not bFresh and not dNeed["catalog"]:
        say("The catalog is current; skipping to what is still needed.")
        iCode = 0
        if bCatalogOnly:
            say("Catalog only was asked for, so no station is asked and no call sign looked up.")
            say("The log is " + sLog); logLine("finished " + datetime.datetime.now().isoformat()); return 0
        if dNeed["enrich"]:
            say("Asking the stations not yet asked.")
            iCode = enrich(sDb, sCountry, iLimit, bAgain, logLine, say)
        if iCode == 0 and dNeed["official"] and not bNoOfficial:
            say("Looking up the call signs not yet looked up.")
            iCode = official(sDb, sCountry, iLimit, bAgain, logLine, say)
        if iCode == 0 and (dNeed["enrich"] or dNeed["official"]):
            say("Done. The report afterwards:")
            assess(sDb, logLine, say)
        say("The log is " + sLog); logLine("finished " + datetime.datetime.now().isoformat()); return iCode
    # A COPY NOBODY HAS MARKED UP IS REPLACED, not merged: a clean start gives
    # every field the template has. One with a status, a rating, a note or a
    # tag in it is yours, and is refreshed in place.
    if not bFresh and os.path.isfile(sDb):
        try:
            cPeek = sqlite3.connect(sDb)
            iMine = cPeek.execute("select count(*) from stations where (status is not null and status not in ('', 'untried')) or coalesce(rating,'') <> '' or coalesce(notes,'') <> '' or coalesce(tags,'') <> ''").fetchone()[0]
            cPeek.close()
            if iMine == 0: bFresh = True
            else: print("Your copy has %d stations you have marked up, so it is kept and refreshed; --fresh would replace it." % iMine)
        except Exception:
            bFresh = True
    if bFresh and os.path.isfile(sDb) and os.path.normcase(os.path.abspath(sDb)) == os.path.normcase(templatePath()) and not os.path.isfile(os.path.join(os.path.dirname(templatePath()), "RadioTrail.schema")):
        # THE TEMPLATE IS NEVER MOVED ASIDE WITH NOTHING TO REBUILD IT FROM (12 October 2026, from an audit): a fresh
        # start on the template itself set it aside and then had no template to copy, leaving no RadioTrail.db at all.
        # With RadioTrail.schema beside it, an empty one is built and the catalog fetched into it; without, nothing moves.
        print("A fresh start would set the template aside with no RadioTrail.schema to rebuild it from, so nothing was changed. Run DbDo's build once; it writes the schema.")
        return 1
    if bFresh and os.path.isfile(sDb):
        # A FRESH START: the old copy set aside as RadioTrail-old.db, and a new
        # copy made from the template at once. (Setting aside without copying
        # left an empty database on 5 October 2026, and the catalog then had
        # no table to land in.)
        if not setAside(sDb):
            print("The copy was left as it is; nothing was replaced."); return 1
        if not copyTemplate(sDb): return 1
    try:
        cConventions = sqlite3.connect(sDb)
        iConventions = ensureConventions(cConventions)
        cConventions.close()
        logLine("Trail conventions: %d index or trigger added" % iConventions if iConventions == 1 else "Trail conventions: %d indexes and triggers added" % iConventions)
    except Exception as oError:
        logLine("Trail conventions could not be checked: %s" % oError)
    say("Step 1 of 4: the copy is " + ("fresh from the template." if bFresh else "in place."))
    say("Step 2 of 4: the catalog.")

    # ---- fetch ----
    lsStations = []
    lsRows = []
    if sSource in ("all", "somafm"):
        try:
            lsRows.extend(fetchSomaFm(logLine, say))
        except Exception as oError:
            say("SomaFM did not answer: %s" % oError); logLine("ERROR somafm " + str(oError))
    if sSource == "somafm":
        iCode = mergeRows(sDb, lsRows, logLine, say, sLog)
        if iCode != 0 or bCatalogOnly: return iCode
        iCode = enrich(sDb, sCountry, 0, bAgain, logLine, say)
        if iCode != 0 or bNoOfficial: return iCode
        return official(sDb, sCountry, 0, bAgain, logLine, say)
    sServer = ""
    for sTry in resolveMirrors(logLine):
        try:
            oReq = urllib.request.Request(sTry + "/json/stats", headers={"User-Agent": c_sAgent})
            with urllib.request.urlopen(oReq, timeout=20) as oResp:
                json.loads(oResp.read().decode("utf-8"))
            sServer = sTry; break
        except Exception as oError:
            logLine("server %s not answering: %s" % (sTry, oError))
    if not sServer:
        say("None of the Radio Browser servers answered. Is the computer online? The log names each attempt.")
        return 1
    logLine("Using " + sServer)
    say("Fetching the catalog from Radio Browser. A full catalog is about 75 MB and takes a minute.")
    iOffset = 0
    bCatalogComplete = False
    while True:
        dQuery = {"hidebroken": "true", "limit": str(c_iPage), "offset": str(iOffset), "order": "votes", "reverse": "true"}
        sUrl = sServer + "/json/stations"
        if sCountry:
            sUrl = sServer + "/json/stations/bycountryexact/" + urllib.parse.quote(sCountry)
        sUrl += "?" + urllib.parse.urlencode(dQuery)
        logLine("GET " + sUrl)
        oReq = urllib.request.Request(sUrl, headers={"User-Agent": c_sAgent})
        try:
            with urllib.request.urlopen(oReq, timeout=120) as oResp:
                lsPage = json.loads(oResp.read().decode("utf-8"))
        except Exception as oError:
            say("The fetch stopped at %d stations: %s" % (len(lsStations), oError)); logLine("ERROR " + str(oError))
            break
        logLine("  %d stations in this page" % len(lsPage))
        lsStations.extend(lsPage)
        say("  %d so far" % len(lsStations))
        if len(lsPage) < c_iPage: bCatalogComplete = True; break
        if iLimit and len(lsStations) >= iLimit: break
        iOffset += c_iPage
        time.sleep(c_dPause)
    if iLimit: lsStations = lsStations[:iLimit]
    if not lsStations and not lsRows:
        say("No stations came back; nothing was changed."); return 1
    sNow = datetime.datetime.now().strftime("%Y-%m-%d")
    for d in lsStations:
        sUuid = (d.get("stationuuid") or "").strip()
        if not sUuid: continue
        lsRows.append({
            "name": (d.get("name") or "").strip(),
            "stream_url": (d.get("url_resolved") or d.get("url") or "").strip(),
            # the address as submitted, when it differs from the stream it
            # resolved to: a playlist that still works after a stream moves
            "playlist_url": (d.get("url") or "").strip() if (d.get("url") or "").strip() != (d.get("url_resolved") or "").strip() else "",
            "homepage": (d.get("homepage") or "").strip(),
            "country": (d.get("country") or "").strip(),
            "state": (d.get("state") or "").strip(),
            "language": (d.get("language") or "").strip(),
            # A tag that is an address, a number alone, or longer than a phrase
            # is catalog litter, not a genre; it stays out of the row.
            "genre": ", ".join(s.strip() for s in (d.get("tags") or "").split(",")
                               if s.strip() and "://" not in s and not s.strip().replace(".", "").isdigit() and len(s.strip()) <= 40),
            "codec": (d.get("codec") or "").strip(),
            "bitrate": str(d.get("bitrate") or ""),
            "votes": str(d.get("votes") or ""),
            "clicks": str(d.get("clickcount") or ""),
            "trend": str(d.get("clicktrend") or ""),
            "hls": "yes" if str(d.get("hls") or "0") not in ("0", "", "False", "false") else "",
            "countrycode": (d.get("countrycode") or "").strip(),
            "last_check": (d.get("lastchecktime_iso8601") or d.get("lastchecktime") or "")[:10],
            "source": "Radio Browser",
            "source_id": sUuid,
            "tags": "\n".join(s.strip() for s in (d.get("tags") or "").split(",") if s.strip() and "://" not in s and len(s.strip()) <= 40),
        })
    iCode = mergeRows(sDb, lsRows, logLine, say, sLog)
    # DROPPED STATIONS ARE MARKED ONLY AFTER A COMPLETE FETCH: one that reached
    # the catalog's last page, with no country and no limit. A fetch that
    # stopped on page two would otherwise mark the rest of the catalog dead
    # (audit of 8 October 2026). And never when the catalog came back far
    # smaller than what is here, which is a catalog fault, not sixty thousand
    # deaths.
    if iCode == 0 and bCatalogComplete and not sCountry and not iLimit and sSource in ("all", "radiobrowser"):
        try:
            cCount = sqlite3.connect(sDb); iHave = cCount.execute("select count(*) from stations where source = 'Radio Browser'").fetchone()[0]; cCount.close()
        except Exception: iHave = 0
        if iHave == 0 or len(lsStations) * 2 >= iHave:
            markDropped(sDb, datetime.date.today().strftime("%Y-%m-%d"), logLine, say)
        else:
            say("The catalog came back with %d stations against %d here; nothing was marked dead." % (len(lsStations), iHave)); logLine("markDropped skipped: catalog far smaller than the table")
    elif iCode == 0 and not bCatalogComplete and not sCountry and not iLimit:
        say("The fetch did not reach the catalog's end, so no station was marked dead."); logLine("markDropped skipped: incomplete fetch")
    if iCode != 0 or bCatalogOnly: return iCode
    say("Step 3 of 4: asking each station what it says about itself. Stop at any time; the next run carries on.")
    iCode = enrich(sDb, sCountry, 0, bAgain, logLine, say)
    if iCode != 0 or bNoOfficial: return iCode
    say("Step 4 of 4: the official record, from Wikipedia, for stations with a call sign in their name.")
    iCode = official(sDb, sCountry, 0, bAgain, logLine, say)
    if iCode == 0:
        say("Done. The report afterwards:")
        assess(sDb, logLine, say)
    return iCode

def mergeRows(sDb, lsRows, logLine, say, sLog):
    """Keyed by the source's own id, rewriting every catalog field and never
    status, rating, notes or tags."""
    sNow = datetime.datetime.now().strftime("%Y-%m-%d")
    # ONE ROW PER SOURCE ID IN A BATCH. The catalog has listed the same station
    # twice across pages while its order shifted under a fetch; the second
    # would then fail the prime's uniqueness and stop the merge (audit of
    # 8 October 2026). The last occurrence wins, and the count is said.
    dSeen = {}
    for dRow in lsRows:
        sKey = dRow.get("source_id", "") or (dRow.get("name", "") + "|" + dRow.get("stream_url", ""))
        dSeen[sKey] = dRow
    if len(dSeen) < len(lsRows):
        say("%d stations were listed more than once by the catalog; one of each is kept." % (len(lsRows) - len(dSeen)))
        logLine("duplicates in batch: %d" % (len(lsRows) - len(dSeen)))
    lsRows = list(dSeen.values())
    c = sqlite3.connect(sDb)
    c.execute("PRAGMA journal_mode=WAL")
    # THE COPY MAY BE OLDER THAN THE TEMPLATE. A copy made before a field was
    # added has no column for it, and an update naming it stops the whole run
    # ("no such column: playlist_url", 5 October 2026, after sixty thousand
    # stations had been fetched). So the columns the copy has are read first;
    # what it lacks is dropped from every row, and said once, with the way to
    # get the new fields.
    lsCols = set(r[1] for r in c.execute("pragma table_info(stations)"))
    lsAll = set()
    for dRow in lsRows: lsAll.update(dRow.keys())
    lsMissing = sorted(k for k in lsAll if k not in lsCols)
    if lsMissing:
        # Cannot happen after the shape check at the start; said plainly if it does.
        say("The copy lacks columns the catalog needs: " + ", ".join(lsMissing) + ". Run fetchStations --fresh.")
        logLine("missing columns: " + ", ".join(lsMissing)); c.close(); return 1
    dHave = {r[0]: r[1] for r in c.execute("select source_id, station_id from stations where source_id is not null and source_id <> ''")}
    iAdded = iUpdated = 0
    for dRow in lsRows:
        sKey = dRow.get("source_id", "")
        if not sKey: continue
        dRow["last_seen"] = sNow
        # The listener's fields are never in an update, whatever a source
        # handed over: the rule is enforced here, not trusted upstream.
        lsTheirs = ("status", "rating", "notes")
        for sTheirs in lsTheirs: dRow.pop(sTheirs, None)
        if "tags" in dRow:
            sTagsNew = dRow.pop("tags")
            if sKey in dHave:
                sHave = (c.execute("select tags from stations where station_id = ?", (dHave[sKey],)).fetchone() or [""])[0]
                c.execute("update stations set tags = ? where station_id = ?", (addLines(sHave, sTagsNew.split("\n")), dHave[sKey]))
            else:
                dRow["tags"] = sTagsNew
        # What --enrich learned is kept across a catalog refresh too: the
        # catalog never had a slogan or a description to overwrite them with,
        # and its genre is merged into what the probe added rather than
        # replacing it.
        if sKey in dHave:
            dRow.pop("slogan", None); dRow.pop("descrip", None); dRow.pop("probed", None)
            try:
                sOld = (c.execute("select genre from stations where station_id = ?", (dHave[sKey],)).fetchone() or [""])[0] or ""
                for sWord in [w.strip() for w in sOld.split(",") if w.strip()]:
                    if sWord.lower() not in dRow.get("genre", "").lower():
                        dRow["genre"] = (dRow.get("genre", "") + ", " if dRow.get("genre") else "") + sWord
            except Exception: pass
        if sKey in dHave:
            sSet = ", ".join('"%s" = ?' % k for k in dRow if k != "source_id")
            c.execute('update stations set %s, edited = CURRENT_TIMESTAMP where station_id = ?' % sSet,
                      [v for k, v in dRow.items() if k != "source_id"] + [dHave[sKey]])
            iUpdated += 1
        else:
            dRow["status"] = "untried"
            c.execute("insert into stations (%s) values (%s)" % (",".join('"%s"' % k for k in dRow), ",".join("?" * len(dRow))), list(dRow.values()))
            iAdded += 1
    # pick lists from what is now in the table, for F4
    for sFld in ("country", "language", "codec", "source"):
        c.execute("delete from lookups where tbl = 'stations' and fld = ? and src = 'fetched'", (sFld,))
        lsVals = [r[0] for r in c.execute("select distinct %s from stations where %s <> '' order by lower(%s)" % (sFld, sFld, sFld))]
        for iOrd, sVal in enumerate(lsVals, 1):
            c.execute("insert or ignore into lookups (src, tbl, fld, val, ordinal) values ('fetched','stations',?,?,?)", (sFld, sVal, iOrd))
    c.commit()
    iTotal = c.execute("select count(*) from stations").fetchone()[0]
    c.close()
    say("%d stations added, %d updated; the database now holds %d. Your status, rating, notes and tags were kept." % (iAdded, iUpdated, iTotal))
    say("The log is " + sLog)
    logLine("finished " + datetime.datetime.now().isoformat())
    return 0

if __name__ == "__main__":
    sys.exit(main())
