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
#   fetchStations                       fills the RadioTrail copy under
#                                       %LOCALAPPDATA%\DbDo\data\RadioTrail
#   fetchStations path\to\other.db      fills that database instead
#   fetchStations --country "United States"   only one country from Radio Browser
#   fetchStations --limit 2000          the 2,000 most voted, for a taste
#   fetchStations --source somafm       only the SomaFM channels (46 of them)
#   fetchStations --source radiobrowser only Radio Browser; the default is both
#   fetchStations --fresh               throw the copy away and start from the
#                                       template, so new fields arrive; your
#                                       status, rating, notes and tags go too
#   fetchStations --enrich              ask each unprobed station what it says
#                                       about itself; add --country or --limit
#                                       to take a part of the list first
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
import datetime, html, json, os, random, re, socket, sqlite3, sys, threading, time, urllib.parse, urllib.request

c_sAgent = "RadioTrail/1.0 (DbDo; https://github.com/JamalMazrui/DbDo)"
c_lsServers = ["https://de1.api.radio-browser.info", "https://fi1.api.radio-browser.info",
               "https://nl1.api.radio-browser.info", "https://at1.api.radio-browser.info"]
c_sAllHosts = "all.api.radio-browser.info"
c_sSomaChannels = "https://somafm.com/channels.json"
c_lsSomaFormats = ["mp3", "aac", "aacp"]
c_lsSomaQuality = ["highest", "high", "low"]
c_iProbeThreads = 20
c_iProbeSeconds = 4

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
    sSql = "select station_id, stream_url, homepage, genre, slogan, descrip from stations where " + sWhere + " order by cast(votes as integer) desc"
    if iLimit: sSql += " limit %d" % iLimit
    lsRows = c.execute(sSql, lsArgs).fetchall()
    if not lsRows:
        say("Nothing to probe: every station here has been asked already. Add --again to ask them again."); c.close(); return 0
    say("Asking %d stations what they say about themselves, %d at a time. About %d minutes." % (len(lsRows), c_iProbeThreads, max(1, len(lsRows) * c_iProbeSeconds // c_iProbeThreads // 60)))
    oLock = threading.Lock(); lsOut = []; lsQueue = list(lsRows)
    def worker():
        while True:
            with oLock:
                if not lsQueue: return
                r = lsQueue.pop()
            d = probeStation(r[1], r[2])
            with oLock: lsOut.append((r, d))
    lsThreads = [threading.Thread(target=worker, daemon=True) for _ in range(c_iProbeThreads)]
    for o in lsThreads: o.start()
    iDone = 0
    while any(o.is_alive() for o in lsThreads):
        time.sleep(5)
        with oLock: iNow = len(lsOut)
        if iNow - iDone >= 200: say("  %d of %d" % (iNow, len(lsRows))); iDone = iNow
    for o in lsThreads: o.join()
    sNow = datetime.datetime.now().strftime("%Y-%m-%d")
    iFilled = 0
    for r, d in lsOut:
        iId, sGenre, sSlogan, sDescrip = r[0], r[3] or "", r[4] or "", r[5] or ""
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
        if d: iFilled += 1
        logLine("probe %d: %s" % (iId, "; ".join("%s=%s" % (k, v[:80]) for k, v in d.items()) or "nothing"))
        c.execute("update stations set slogan=?, descrip=?, genre=?, probed=?, edited=CURRENT_TIMESTAMP where station_id=?", (sSloganNew, sDescripNew, sGenreNew, sNow, iId))
    c.commit(); c.close()
    say("%d stations answered with something; %d said nothing. Keywords now finds what they said." % (iFilled, len(lsOut) - iFilled))
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

def main():
    lsArgs = sys.argv[1:]
    sDb = ""
    sCountry = ""
    sSource = "all"
    iLimit = 0
    bEnrich = False
    bAgain = False
    bFresh = False
    i = 0
    while i < len(lsArgs):
        sArg = lsArgs[i]
        if sArg == "--country" and i + 1 < len(lsArgs): sCountry = lsArgs[i + 1]; i += 2; continue
        if sArg == "--limit" and i + 1 < len(lsArgs): iLimit = int(lsArgs[i + 1]); i += 2; continue
        if sArg == "--source" and i + 1 < len(lsArgs): sSource = lsArgs[i + 1].lower(); i += 2; continue
        if sArg == "--enrich": bEnrich = True; i += 1; continue
        if sArg == "--fresh": bFresh = True; i += 1; continue
        if sArg == "--again": bAgain = True; i += 1; continue
        if sArg.startswith("-"): i += 1; continue
        sDb = sArg; i += 1
    if not sDb:
        sDb = os.path.join(os.environ.get("LOCALAPPDATA", ""), "DbDo", "data", "RadioTrail", "RadioTrail.db")
    if bFresh and os.path.isfile(sDb):
        # A FRESH START, asked for by name. The old copy is kept beside the new
        # one as RadioTrail-old.db until the next fresh start, so a status or a
        # note that mattered can still be read out of it.
        sOld = os.path.join(os.path.dirname(sDb), "RadioTrail-old.db")
        try:
            if os.path.isfile(sOld): os.remove(sOld)
            for sSide in ("", "-wal", "-shm"):
                if os.path.isfile(sDb + sSide) and sSide: os.remove(sDb + sSide)
            os.rename(sDb, sOld)
            print("The old copy is now " + sOld)
        except Exception as oError:
            print("Could not set the old copy aside: " + str(oError)); return 1
    if not os.path.isfile(sDb):
        # NO COPY YET? MAKE ONE. DbDo copies the templates into its data
        # folder the first time Template Databases is opened, and a person who
        # builds and runs this script first has not opened it yet. The
        # template is beside this script, so the script makes the copy itself
        # rather than sending the person away to do a step and come back.
        sTemplate = os.path.join(os.path.dirname(os.path.abspath(__file__)), "RadioTrail.db")
        if os.path.isfile(sTemplate):
            os.makedirs(os.path.dirname(sDb), exist_ok=True)
            import shutil
            shutil.copy2(sTemplate, sDb)
            for sSide in ("RadioTrail.inix",):
                sSrc = os.path.join(os.path.dirname(sTemplate), sSide)
                if os.path.isfile(sSrc): shutil.copy2(sSrc, os.path.join(os.path.dirname(sDb), sSide))
            print("Made your copy of RadioTrail at " + sDb)
        else:
            print("No database at " + sDb + ", and no RadioTrail.db beside this script to copy. Name a database.")
            return 1
    # THE LOG GOES WHERE THE PROGRAM'S LOGS GO: %LOCALAPPDATA%\DbDo\logs, beside
    # the data folder, not inside it. The first version wrote into data\logs,
    # which is where nobody looks.
    sLogDir = os.path.join(os.environ.get("LOCALAPPDATA", ""), "DbDo", "logs")
    os.makedirs(sLogDir, exist_ok=True)
    sLog = os.path.join(sLogDir, "RadioTrail-fetch-" + datetime.datetime.now().strftime("%Y%m%d-%H%M%S") + ".log")
    oLog = open(sLog, "w", encoding="utf-8")
    def logLine(s): oLog.write(s + "\n"); oLog.flush()
    def say(s): print(s); logLine("CONSOLE: " + s)
    logLine("fetchStations started " + datetime.datetime.now().isoformat())
    logLine("Script: " + os.path.abspath(__file__) + " | Python " + sys.version.split()[0] + " | " + sys.platform)
    logLine("Database: " + sDb + " | source: " + sSource + " | country: " + (sCountry or "(all)") + " | limit: " + str(iLimit or "(none)"))

    if bEnrich:
        iCode = enrich(sDb, sCountry, iLimit, bAgain, logLine, say)
        say("The log is " + sLog); logLine("finished " + datetime.datetime.now().isoformat()); return iCode

    # ---- fetch ----
    lsStations = []
    lsRows = []
    if sSource in ("all", "somafm"):
        try:
            lsRows.extend(fetchSomaFm(logLine, say))
        except Exception as oError:
            say("SomaFM did not answer: %s" % oError); logLine("ERROR somafm " + str(oError))
    if sSource == "somafm":
        return mergeRows(sDb, lsRows, logLine, say, sLog)
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
        if len(lsPage) < c_iPage: break
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
            "genre": ", ".join(s.strip() for s in (d.get("tags") or "").split(",") if s.strip()),
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
        })
    return mergeRows(sDb, lsRows, logLine, say, sLog)

def mergeRows(sDb, lsRows, logLine, say, sLog):
    """Keyed by the source's own id, rewriting every catalog field and never
    status, rating, notes or tags."""
    sNow = datetime.datetime.now().strftime("%Y-%m-%d")
    c = sqlite3.connect(sDb)
    c.execute("PRAGMA journal_mode=WAL")
    # THE COPY MAY BE OLDER THAN THE TEMPLATE. A copy made before a field was
    # added has no column for it, and an update naming it stops the whole run
    # ("no such column: playlist_url", 5 October 2026, after sixty thousand
    # stations had been fetched). So the columns the copy has are read first;
    # what it lacks is dropped from every row, and said once, with the way to
    # get the new fields.
    lsCols = set(r[1] for r in c.execute("pragma table_info(stations)"))
    lsMissing = sorted(k for k in lsRows[0] if k not in lsCols) if lsRows else []
    if lsMissing:
        say("This copy of RadioTrail is older than the template and lacks " + ", ".join(lsMissing)
            + ". Those were left out. For the new fields run rebuildRadioTrail, or fetchStations --fresh.")
        logLine("missing columns: " + ", ".join(lsMissing))
    dHave = {r[0]: r[1] for r in c.execute("select source_id, station_id from stations where source_id is not null and source_id <> ''")}
    iAdded = iUpdated = 0
    for dRow in lsRows:
        for k in lsMissing: dRow.pop(k, None)
        sKey = dRow.get("source_id", "")
        if not sKey: continue
        dRow["last_seen"] = sNow
        # The listener's fields are never in an update, whatever a source
        # handed over: the rule is enforced here, not trusted upstream.
        lsTheirs = ("status", "rating", "notes", "tags")
        for sTheirs in lsTheirs: dRow.pop(sTheirs, None)
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
