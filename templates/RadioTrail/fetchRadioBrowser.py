# fetchRadioBrowser.py -- fill a RadioTrail database from the Radio Browser
# community catalog, keeping everything the listener has written.
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
#   fetchRadioBrowser                       fills the RadioTrail copy under
#                                           %LOCALAPPDATA%\DbDo\data\RadioTrail
#   fetchRadioBrowser path\to\other.db      fills that database instead
#   fetchRadioBrowser --country "United States"   only one country
#   fetchRadioBrowser --limit 2000          the 2,000 most voted, for a taste
#
# The log is logs\RadioTrail-fetch-<date>-<time>.log beside the database,
# in the Homer layout: everything it did, every request, every count.
import datetime, json, os, sqlite3, sys, time, urllib.parse, urllib.request

c_sAgent = "RadioTrail/1.0 (DbDo; https://github.com/JamalMazrui/DbDo)"
c_lsServers = ["https://de1.api.radio-browser.info", "https://fi1.api.radio-browser.info",
               "https://nl1.api.radio-browser.info", "https://at1.api.radio-browser.info"]
c_iPage = 10000
c_dPause = 0.5

def main():
    lsArgs = sys.argv[1:]
    sDb = ""
    sCountry = ""
    iLimit = 0
    i = 0
    while i < len(lsArgs):
        sArg = lsArgs[i]
        if sArg == "--country" and i + 1 < len(lsArgs): sCountry = lsArgs[i + 1]; i += 2; continue
        if sArg == "--limit" and i + 1 < len(lsArgs): iLimit = int(lsArgs[i + 1]); i += 2; continue
        if sArg.startswith("-"): i += 1; continue
        sDb = sArg; i += 1
    if not sDb:
        sDb = os.path.join(os.environ.get("LOCALAPPDATA", ""), "DbDo", "data", "RadioTrail", "RadioTrail.db")
    if not os.path.isfile(sDb):
        print("No database at " + sDb + ". Open the RadioTrail template in DbDo once, or name a database.")
        return 1
    sLogDir = os.path.join(os.path.dirname(os.path.dirname(sDb)), "logs")
    os.makedirs(sLogDir, exist_ok=True)
    sLog = os.path.join(sLogDir, "RadioTrail-fetch-" + datetime.datetime.now().strftime("%Y%m%d-%H%M%S") + ".log")
    oLog = open(sLog, "w", encoding="utf-8")
    def logLine(s): oLog.write(s + "\n"); oLog.flush()
    def say(s): print(s); logLine("CONSOLE: " + s)
    logLine("fetchRadioBrowser started " + datetime.datetime.now().isoformat())
    logLine("Script: " + os.path.abspath(__file__) + " | Python " + sys.version.split()[0] + " | " + sys.platform)
    logLine("Database: " + sDb + " | country: " + (sCountry or "(all)") + " | limit: " + str(iLimit or "(none)"))

    # ---- fetch ----
    lsStations = []
    sServer = ""
    for sTry in c_lsServers:
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
    if not lsStations:
        say("No stations came back; nothing was changed."); return 1

    # ---- merge, keyed by uuid, keeping the listener's fields ----
    sNow = datetime.datetime.now().strftime("%Y-%m-%d")
    c = sqlite3.connect(sDb)
    c.execute("PRAGMA journal_mode=WAL")
    dHave = {r[0]: r[1] for r in c.execute("select source_id, station_id from stations where source_id is not null and source_id <> ''")}
    iAdded = iUpdated = 0
    for d in lsStations:
        sUuid = (d.get("stationuuid") or "").strip()
        if not sUuid: continue
        dRow = {
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
            "last_seen": sNow,
        }
        if sUuid in dHave:
            sSet = ", ".join('"%s" = ?' % k for k in dRow if k != "source_id")
            c.execute('update stations set %s, edited = CURRENT_TIMESTAMP where station_id = ?' % sSet,
                      [v for k, v in dRow.items() if k != "source_id"] + [dHave[sUuid]])
            iUpdated += 1
        else:
            dRow["status"] = "untried"
            c.execute("insert into stations (%s) values (%s)" % (",".join('"%s"' % k for k in dRow), ",".join("?" * len(dRow))), list(dRow.values()))
            iAdded += 1
    # pick lists from what is now in the table, for F4
    for sFld in ("country", "language", "codec"):
        c.execute("delete from lookups where tbl = 'stations' and fld = ? and src = 'Radio Browser'", (sFld,))
        lsVals = [r[0] for r in c.execute("select distinct %s from stations where %s <> '' order by lower(%s)" % (sFld, sFld, sFld))]
        for iOrd, sVal in enumerate(lsVals, 1):
            c.execute("insert or ignore into lookups (src, tbl, fld, val, ordinal) values ('Radio Browser','stations',?,?,?)", (sFld, sVal, iOrd))
    c.commit()
    iTotal = c.execute("select count(*) from stations").fetchone()[0]
    c.close()
    say("%d stations added, %d updated; the database now holds %d. Your status, rating, notes and tags were kept." % (iAdded, iUpdated, iTotal))
    logLine("finished " + datetime.datetime.now().isoformat())
    return 0

if __name__ == "__main__":
    sys.exit(main())
