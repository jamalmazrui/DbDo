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
import datetime, json, os, random, re, socket, sqlite3, sys, time, urllib.parse, urllib.request

c_sAgent = "RadioTrail/1.0 (DbDo; https://github.com/JamalMazrui/DbDo)"
c_lsServers = ["https://de1.api.radio-browser.info", "https://fi1.api.radio-browser.info",
               "https://nl1.api.radio-browser.info", "https://at1.api.radio-browser.info"]
c_sAllHosts = "all.api.radio-browser.info"
c_sSomaChannels = "https://somafm.com/channels.json"
c_lsSomaFormats = ["mp3", "aac", "aacp"]
c_lsSomaQuality = ["highest", "high", "low"]

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
    i = 0
    while i < len(lsArgs):
        sArg = lsArgs[i]
        if sArg == "--country" and i + 1 < len(lsArgs): sCountry = lsArgs[i + 1]; i += 2; continue
        if sArg == "--limit" and i + 1 < len(lsArgs): iLimit = int(lsArgs[i + 1]); i += 2; continue
        if sArg == "--source" and i + 1 < len(lsArgs): sSource = lsArgs[i + 1].lower(); i += 2; continue
        if sArg.startswith("-"): i += 1; continue
        sDb = sArg; i += 1
    if not sDb:
        sDb = os.path.join(os.environ.get("LOCALAPPDATA", ""), "DbDo", "data", "RadioTrail", "RadioTrail.db")
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
            "homepage": (d.get("homepage") or "").strip(),
            "country": (d.get("country") or "").strip(),
            "state": (d.get("state") or "").strip(),
            "language": (d.get("language") or "").strip(),
            "genre": ", ".join(s.strip() for s in (d.get("tags") or "").split(",") if s.strip()),
            "codec": (d.get("codec") or "").strip(),
            "bitrate": str(d.get("bitrate") or ""),
            "votes": str(d.get("votes") or ""),
            "clicks": str(d.get("clickcount") or ""),
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
    dHave = {r[0]: r[1] for r in c.execute("select source_id, station_id from stations where source_id is not null and source_id <> ''")}
    iAdded = iUpdated = 0
    for dRow in lsRows:
        sKey = dRow.get("source_id", "")
        if not sKey: continue
        dRow["last_seen"] = sNow
        # The listener's fields are never in an update, whatever a source
        # handed over: the rule is enforced here, not trusted upstream.
        lsTheirs = ("status", "rating", "notes", "tags")
        for sTheirs in lsTheirs: dRow.pop(sTheirs, None)
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
