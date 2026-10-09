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
#   fetchStations                       all three steps, every station
#   fetchStations --country "United States"   the catalog for one country only
#   fetchStations --limit 2000          the 2,000 most voted, for a taste
#   fetchStations --catalog-only        steps 1 and 2, no asking
#   fetchStations --enrich-only         step 3 only, on what is there
#   fetchStations --official-only       step 4 only, on what is there
#   fetchStations --no-official         skip step 4
#   fetchStations --report              the quality report alone, nothing fetched
#   fetchStations --force               every step, whatever the check says
#   fetchStations --import list.m3u     add the stations in a playlist -- .m3u,
#                                       .m3u8 or .pls -- as your own, source
#                                       "mine", keyed by name and address;
#                                       a station already there is left alone
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


def ensureConventions(c):
    """Adds the missing Trail indexes and triggers; returns how many were added."""
    def quoted(s): return '"' + s.replace('"', '""') + '"'
    iAdded = 0
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

def infoboxFields(sWikitext):
    """The fields of a Template:Infobox radio station, lightly cleaned."""
    m = re.search(r"\{\{\s*Infobox radio station(.*)", sWikitext, re.I | re.S)
    if not m: return {}
    sBody = m.group(1)
    d = {}
    for mm in re.finditer(r"^\s*\|\s*([a-z_ ]+?)\s*=\s*(.*?)(?=^\s*\||\Z)", sBody, re.M | re.S):
        k = mm.group(1).strip().lower().replace(" ", "_"); v = mm.group(2)
        v = re.sub(r"<ref[^>]*/>|<ref.*?</ref>", "", v, flags=re.S)
        v = re.sub(r"\{\{(?:nowrap|nobr)\|([^}]*)\}\}", r"\1", v, flags=re.I)
        v = re.sub(r"\{\{\s*Frequency\s*\|\s*([^|}]+)\s*\|\s*([^|}]+)\s*\}\}", r"\1 \2", v, flags=re.I)
        v = re.sub(r"\{\{\s*Start date(?: and age)?\s*\|\s*(\d{4})(?:\s*\|\s*(\d{1,2}))?(?:\s*\|\s*(\d{1,2}))?[^}]*\}\}",
                   lambda mm: mm.group(1) + ("-" + mm.group(2).zfill(2) if mm.group(2) else "") + ("-" + mm.group(3).zfill(2) if mm.group(3) else ""), v, flags=re.I)
        v = re.sub(r"\{\{[^}]*\}\}", "", v)
        v = re.sub(r"\[\[(?:[^|\]]*\|)?([^\]]*)\]\]", r"\1", v)
        v = re.sub(r"<[^>]+>", " ", v)
        v = re.sub(r"\s+", " ", v).strip(" \n}")
        if v: d[k] = v[:200]
    return d

class WikiRefused(Exception):
    pass

def wikiPages(dParams):
    """One query; the pages it returns, or a WikiRefused when the answer is a
    refusal -- which Wikipedia gives as an HTTP code or, as often, as an
    error object inside a 200."""
    try:
        d = wikiGet(dParams)
    except urllib.error.HTTPError as oError:
        if oError.code in (403, 429, 503): raise WikiRefused(str(oError.code))
        return [], "http " + str(oError.code)
    except Exception as oError:
        return [], "network: " + str(oError)[:80]
    if "error" in d:
        sCode = str(d["error"].get("code", ""))
        if sCode in ("ratelimited", "maxlag", "readonly"): raise WikiRefused(sCode)
        return [], "api error " + sCode
    return list(d.get("query", {}).get("pages", {}).values()), ""

def pageRecord(oPage, sCall):
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
    sBoxCall = (dBox.get("call_sign") or dBox.get("callsign") or dBox.get("name") or "").upper()
    # THE CALL SIGN MUST BE THE ARTICLE'S: in its infobox, or as the whole of
    # its title before a band or a bracket -- KALW, WCRB (FM), KQED-FM.
    bOurs = bool(re.search(r"\b" + re.escape(sCall) + r"\b", sBoxCall)) or bool(re.match(r"^" + re.escape(sCall) + r"(-(AM|FM|LP|HD\d?))?( \(.*\))?$", sTitle.upper()))
    if not bOurs: return None, "other call sign in " + sTitle
    d = {"wikipedia": "https://en.wikipedia.org/wiki/" + urllib.parse.quote(sTitle.replace(" ", "_")),
         "call_sign": dBox.get("call_sign") or dBox.get("callsign") or sCall,
         "frequency": dBox.get("frequency", ""), "city": dBox.get("city", "") or dBox.get("area", ""),
         "owner": dBox.get("owner", ""), "format": dBox.get("format", ""),
         "extract": (oPage.get("extract") or "").strip()[:1500], "airdate": dBox.get("airdate", "")}
    return {k: v for k, v in d.items() if v}, ""

def officialStation(sName):
    """Wikipedia's infobox and opening paragraph for a station whose name
    carries a call sign. The article is asked for BY TITLE first -- KALW,
    KALW-FM, KALW (FM) and so on, redirects followed -- which is how a
    licensed station's article is named; a search is the fallback. Returns
    (record, reason): the record empty when nothing safe was found, the
    reason saying why, for the log."""
    m = c_reCallSign.search(sName.upper())
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
        d, sReason = pageRecord(oPage, sCall)
        if d: return d, "by title " + oPage.get("title", "")
        if sReason and not sReason.startswith("no page"): lsReasons.append(sReason)
    time.sleep(c_dWikiPause)
    dParams = dict(dCommon); dParams.update({"generator": "search", "gsrlimit": "5", "gsrsearch": sCall + (" " + sBand if sBand else "") + " radio station"})
    lsPages, sWhy = wikiPages(dParams)
    if sWhy: lsReasons.append(sWhy)
    for oPage in lsPages:
        d, sReason = pageRecord(oPage, sCall)
        if d: return d, "by search " + oPage.get("title", "")
        if sReason: lsReasons.append(sReason)
    return {}, "; ".join(lsReasons[:4]) or "search found nothing"

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

def assess(sDb, logLine, say):
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
    try:
        dCode = {}
        for sCode, sName, iCount in c.execute("select countrycode, country, count(*) from stations where coalesce(countrycode,'') <> '' and coalesce(country,'') <> '' group by countrycode, country order by count(*)"):
            dCode[sCode] = sName  # the last row for a code is its most common name
        iRepaired = 0
        for iId, sCode in c.execute("select station_id, countrycode from stations where coalesce(country,'') = '' and coalesce(countrycode,'') <> ''").fetchall():
            if sCode in dCode:
                c.execute("update stations set country = ? where station_id = ?", (dCode[sCode], iId)); iRepaired += 1
        if iRepaired: c.commit(); say("  %d stations had a country code and no country name; the name is filled in from the code." % iRepaired)
    except Exception as oError:
        logLine("country repair skipped: " + str(oError))
    iStateNoCountry = q("select count(*) from stations where coalesce(state,'') <> '' and coalesce(country,'') = ''")
    lsCall = []; iOfficial = 0; iCallNames = 0; iDash = 0
    if "call_sign" in lsCols:
        for r in c.execute("select name, coalesce(wikipedia,'') from stations where stream_url <> '' and countrycode in ('US','CA')"):
            if c_reCallSign.search((r[0] or "").upper()):
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
              if c_reCallSign.search((r[1] or "").upper())]
    if iLimit: lsRows = lsRows[:iLimit]
    if not lsRows:
        say("No station here with a call sign in its name is still unlooked-up."); c.close(); return 0
    say("Looking up %d stations with a call sign in their name on Wikipedia, one a second as it asks. About %d minutes; stop any time and the next run carries on." % (len(lsRows), max(1, len(lsRows) * 11 // 10 // 60)))
    oLock = threading.Lock(); lsOut = []; lsQueue = list(lsRows); iWritten = [0]; iFilled = [0]; bRefused = [False]
    def flush():
        with oLock:
            lsBatch = list(lsOut); del lsOut[:]
        for r, d, sWhy in lsBatch:
            iId, sNotes, sTags = r[0], r[2] or "", r[3] or ""
            if d:
                iFilled[0] += 1
                lsTagNew = [s.strip() for s in re.split(r"[,/;]", d.get("format", "")) if s.strip()]
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
                d = None; sWhy = "refused: " + str(oRefused)
                time.sleep(60)
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
            iId, sGenre, sSlogan, sDescrip, sTags, sNotes = r[0], r[3] or "", r[4] or "", r[5] or "", r[6] or "", r[7] or ""
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
            "country": "United States", "state": "", "language": "english",
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

def copyTemplate(sDb):
    """A new copy of the template at sDb, with its settings file beside it."""
    import shutil
    sTemplate = templatePath()
    if not os.path.isfile(sTemplate):
        print("No RadioTrail.db beside this script to copy from."); return False
    os.makedirs(os.path.dirname(sDb), exist_ok=True)
    shutil.copy2(sTemplate, sDb)
    sInix = os.path.join(os.path.dirname(sTemplate), "RadioTrail.inix")
    if os.path.isfile(sInix): shutil.copy2(sInix, os.path.join(os.path.dirname(sDb), "RadioTrail.inix"))
    print("Made your copy of RadioTrail at " + sDb)
    return True

def setAside(sDb):
    """The copy at sDb becomes RadioTrail-old.db beside it; a previous old
    copy goes. Returns True when it was set aside, False when it was not --
    and the caller must not make a new copy over a database that is still
    there. THE WRITE-AHEAD LOG IS NEVER DELETED: a note committed a moment
    ago may live only there. The database is checkpointed, which folds the
    log into the main file and removes it, before the file is moved (audit
    of 8 October 2026)."""
    sOld = os.path.join(os.path.dirname(sDb), "RadioTrail-old.db")
    try:
        c = sqlite3.connect(sDb)
        try: c.execute("PRAGMA wal_checkpoint(TRUNCATE)")
        finally: c.close()
    except Exception as oError:
        print("Could not checkpoint the database before setting it aside: " + str(oError)); return False
    try:
        if os.path.isfile(sOld): os.remove(sOld)
        for sSide in ("-wal", "-shm"):
            if os.path.isfile(sDb + sSide):
                # Still here after the checkpoint: it goes with its database, not away.
                os.replace(sDb + sSide, sOld + sSide)
        os.rename(sDb, sOld)
        print("The old copy is now " + sOld)
        return True
    except Exception as oError:
        print("Could not set the old copy aside: " + str(oError)); return False

def sameShape(sDb):
    """True when the copy's stations table has every column the template's
    has. An empty file or an older copy answers False."""
    try:
        cT = sqlite3.connect(templatePath()); lsWant = set(r[1] for r in cT.execute("pragma table_info(stations)")); cT.close()
        cD = sqlite3.connect(sDb); lsHave = set(r[1] for r in cD.execute("pragma table_info(stations)")); cD.close()
        return bool(lsHave) and lsWant <= lsHave
    except Exception:
        return False

def main():
    lsArgs = sys.argv[1:]
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
        if sArg == "--import" and i + 1 < len(lsArgs): sImport = lsArgs[i + 1]; i += 2; continue
        if sArg == "--fresh": bFresh = True; i += 1; continue
        if sArg == "--again": bAgain = True; i += 1; continue
        if sArg.startswith("-"): i += 1; continue
        sDb = sArg; i += 1
    if not sDb:
        sDb = os.path.join(os.environ.get("LOCALAPPDATA", ""), "DbDo", "data", "RadioTrail", "RadioTrail.db")
    if not os.path.isfile(sDb):
        # NO COPY YET? MAKE ONE from the template beside this script, rather
        # than sending the person away to open it in DbDo and come back.
        if not copyTemplate(sDb): return 1
    elif not sameShape(sDb):
        # THE COPY IS NOT THIS TEMPLATE'S SHAPE -- an older RadioTrail, or an
        # empty file left by a failed start. Nobody's work is in it that the
        # template does not supply, so it is set aside and a clean one made.
        # No migration: the structure going forward is the template's.
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
    oLog = open(sLog, "w", encoding="utf-8")
    def logLine(s): oLog.write(s + "\n"); oLog.flush()
    def say(s): print(s); logLine("CONSOLE: " + s)
    print("Log: " + sLog)
    logLine("fetchStations started " + datetime.datetime.now().isoformat())
    logLine("Script: " + os.path.abspath(__file__) + " | Python " + sys.version.split()[0] + " | " + sys.platform)
    logLine("Database: " + sDb + " | source: " + sSource + " | country: " + (sCountry or "(all)") + " | limit: " + str(iLimit or "(none)"))

    if sImport:
        iCode = importPlaylist(sDb, sImport, logLine, say)
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
    dNeed = assess(sDb, logLine, say)
    if bReportOnly:
        if dNeed["catalog"] or dNeed["enrich"] or dNeed["official"]:
            say("This was the report only. To fetch what is needed, run fetchStations with no arguments.")
        say("The log is " + sLog); logLine("finished " + datetime.datetime.now().isoformat()); return 0
    if not bForce and not dNeed["catalog"]:
        say("The catalog is current; skipping to what is still needed.")
        iCode = 0
        if dNeed["enrich"]:
            say("Asking the stations not yet asked.")
            iCode = enrich(sDb, sCountry, 0, bAgain, logLine, say)
        if iCode == 0 and dNeed["official"] and not bNoOfficial:
            say("Looking up the call signs not yet looked up.")
            iCode = official(sDb, sCountry, 0, bAgain, logLine, say)
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
