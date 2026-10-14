"""checkBlindSkillsMedia.py -- checks that every BlindSkills lesson plays for anyone, with no sign-in.

    scripts\\checkBlindSkillsMedia                  checks every lesson in templates\\BlindSkills\\BlindSkills.db
    scripts\\checkBlindSkillsMedia <database>       checks the lessons of another copy
    scripts\\checkBlindSkillsMedia --audio          checks the audio lessons only, which needs no yt-dlp

WHY. Blind Skills promises that every lesson needs no sign-in and plays at once, in a browser, in DbDo's Play Stream
and in FileDir's Homer Player. Neither AI that built it could prove that: one sandbox had no network, and the other's
data-center address was refused the video bytes YouTube sends a home computer. This check runs where the lessons are
actually heard, and asks the way an anonymous player asks: no cookies, no browser session, no yt-dlp configuration.

HOW. An audio lesson is asked for its first 512 bytes, which must be audio rather than a web page. A video lesson is
resolved by yt-dlp to its stream, whose first bytes are then asked for the same way; a stream given as an HLS play list
has its first segment checked instead. yt-dlp is found on the PATH or beside DbDo, and fetched into DbDo's temp folder
when it is missing. A lesson that fails is named on the screen with the reason; a reason that mentions signing in,
cookies or bots is reported as blocked: a lesson the directory must not keep if it is refused everywhere, though such a
check can be about one network rather than the lesson.

WHAT IT WRITES. results\\BlindSkills-media.json, every lesson's result; results\\BlindSkills_Checked.m3u, a play list of
the lessons that answered with audio or video, with each length yt-dlp measured; and logs\\DbDo-checkBlindSkillsMedia-<date>-<time>.log, with
every request and its result. The database itself is not changed. A failure here is a fact about this computer on this
day -- a host can refuse one network and serve another -- so it is a reason to listen, not proof the lesson is gone.

Adapted on 14 October 2026 from a checker the second AI wrote for this directory: same probes, reading DbDo's database
rather than a separate data file, in Play Stream's own order of address fields, and logging the Homer way. What a probe
shows is said plainly: a lesson that answers has started a stream, which is strong evidence but not a play-through.
"""

import datetime, json, os, platform, shutil, sqlite3, subprocess, sys, time, traceback, urllib.parse, urllib.request

sVersion = "2026-10-14 version 2: Play Stream's own address order; says what a probe shows, not more"
iSampleBytes = 512
iTimeoutSeconds = 20
lsGateWords = ["sign in", "signin", "log in", "login", "cookies", "not a bot", "authenticate", "confirm your age"]
# PLAY STREAM'S ORDER (14 October 2026, from the second AI): DbDo plays the first of these that holds a web address, so
# a stream_url a person adds wins over url here too, and the check tests what Alt+Shift+P will actually play.
lsAddressFields = ["stream_url", "url", "stream", "link", "address"]
lsMediaSignatures = [b"ID3", b"OggS", b"RIFF", b"fLaC", b"\x1aE\xdf\xa3"]
sYtdlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"

sProject = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sStamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
sLogPath = os.path.join(sProject, "logs", "DbDo-checkBlindSkillsMedia-%s.log" % sStamp)


def log(sText):
    os.makedirs(os.path.dirname(sLogPath), exist_ok=True)
    with open(sLogPath, "a", encoding="utf-8-sig") as f: f.write(datetime.datetime.now().strftime("%H:%M:%S") + " " + sText + "\n")
    return True


def say(sText):
    print(sText, flush=True)
    return log(sText)


def findYtdlp():
    """yt-dlp on the PATH, beside DbDo, or fetched into DbDo's temp folder; '' when none can be had."""
    for sPlace in [shutil.which("yt-dlp") or "", os.path.join(sProject, "yt-dlp.exe"), os.path.join(sProject, "exec", "yt-dlp.exe"),
                   os.path.join(os.environ.get("LOCALAPPDATA", ""), "DbDo", "yt-dlp.exe"), os.path.join(sProject, "temp", "yt-dlp.exe")]:
        if sPlace and os.path.isfile(sPlace): return sPlace
    if platform.system() != "Windows": return ""
    sTarget = os.path.join(sProject, "temp", "yt-dlp.exe")
    try:
        os.makedirs(os.path.dirname(sTarget), exist_ok=True)
        say("Fetching yt-dlp, which checks the video lessons.")
        log("GET " + sYtdlpUrl)
        urllib.request.urlretrieve(sYtdlpUrl, sTarget)
        return sTarget
    except Exception as oError:
        log("yt-dlp could not be fetched: %s" % oError)
        return ""


def isMedia(binData, sType):
    if sType.startswith(("audio/", "video/")): return True
    if any(binData.startswith(b) for b in lsMediaSignatures): return True
    if len(binData) > 1 and binData[0] == 255 and (binData[1] & 224) == 224: return True
    return b"ftyp" in binData[:24]


def probeBytes(sUrl, dHeaders=None, iDepth=0):
    """The first bytes of a stream, asked for anonymously. Raises with the reason when they are not audio or video."""
    dSend = {k: v for k, v in (dHeaders or {}).items() if k.lower() not in ("cookie", "authorization", "proxy-authorization")}
    dSend.setdefault("User-Agent", "Mozilla/5.0")
    oOpener = urllib.request.build_opener()  # no cookie processor: nothing remembered between requests
    oRequest = urllib.request.Request(sUrl, headers=dict(dSend, Range="bytes=0-%d" % (iSampleBytes - 1)))
    with oOpener.open(oRequest, timeout=iTimeoutSeconds) as oResponse:
        sType = oResponse.headers.get("Content-Type", "").lower()
        binData = oResponse.read(iSampleBytes)
        iStatus = oResponse.getcode()
    log("  %s -> HTTP %d, %s, %d bytes" % (sUrl[:120], iStatus, sType or "no type", len(binData)))
    if iStatus not in (200, 206): raise ValueError("HTTP %d" % iStatus)
    if "html" in sType or binData.lstrip().lower().startswith((b"<!doctype html", b"<html")): raise ValueError("a web page came back instead of media")
    if binData.lstrip().startswith(b"#EXTM3U") or "mpegurl" in sType:
        if iDepth >= 3: raise ValueError("too many nested HLS play lists")
        with oOpener.open(urllib.request.Request(sUrl, headers=dSend), timeout=iTimeoutSeconds) as oManifest:
            sManifest = oManifest.read(65536).decode("utf-8-sig", "replace")
            sBase = oManifest.geturl()
        lsSegments = [s.strip() for s in sManifest.splitlines() if s.strip() and not s.startswith("#")]
        if not lsSegments: raise ValueError("an HLS play list with nothing in it")
        return probeBytes(urllib.parse.urljoin(sBase, lsSegments[0]), dSend, iDepth + 1)
    if not isMedia(binData, sType): raise ValueError("the reply was not audio or video (%s)" % (sType or "no type"))
    return True


def checkLesson(dLesson, sYtdlp):
    dResult = {"title": dLesson["title"], "url": dLesson["url"], "medium": dLesson["medium"], "status": "answered", "seconds": -1}
    try:
        if not dLesson["url"]: raise ValueError("the record has no playback address")
        if dLesson["medium"] == "audio":
            probeBytes(dLesson["url"])
        else:
            if not sYtdlp: raise LookupError("yt-dlp is not available, so the video was not checked")
            lsArgs = [sYtdlp, "--ignore-config", "--no-playlist", "--skip-download", "--dump-single-json", "--no-cookies-from-browser",
                      "--socket-timeout", str(iTimeoutSeconds), "--retries", "0", "-f", "bestaudio/best", dLesson["url"]]
            if shutil.which("node"): lsArgs[1:1] = ["--js-runtimes", "node"]
            log("  " + " ".join(lsArgs))
            oRun = subprocess.run(lsArgs, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=iTimeoutSeconds * 4)
            log("  yt-dlp exit %d" % oRun.returncode)
            if oRun.returncode: raise ValueError((oRun.stderr or "yt-dlp failed").strip().splitlines()[-1])
            dInfo = json.loads(oRun.stdout)
            if not dInfo.get("url"): raise ValueError("yt-dlp found no single playable stream")
            probeBytes(dInfo["url"], dInfo.get("http_headers"))
            if dInfo.get("duration"): dResult["seconds"] = int(round(dInfo["duration"]))
    except LookupError as oError:
        dResult.update(status="not checked", reason=str(oError))
    except Exception as oError:
        sReason = str(oError)
        # A SIGN-IN OR BOT CHECK IS REPORTED AS SUCH, NOT AS PROOF (14 October 2026, from the second AI): YouTube asks
        # some networks to prove they are not robots, so the same lesson can be refused here and served elsewhere.
        dResult.update(status="blocked" if any(s in sReason.lower() for s in lsGateWords) else "failed", reason=sReason)
    log("%s: %s%s" % (dResult["status"], dLesson["title"], " -- " + dResult["reason"] if dResult.get("reason") else ""))
    return dResult


def main(lsArgs):
    log("checkBlindSkillsMedia.py started; %s; script %s" % (sVersion, os.path.abspath(__file__)))
    log("Python %s at %s on %s; cwd %s; command line %s" % (platform.python_version(), sys.executable, platform.platform(), os.getcwd(), " ".join(sys.argv)))
    bAudioOnly = any(s.lower() in ("--audio", "-a") for s in lsArgs)
    lsPaths = [s for s in lsArgs if not s.startswith("-")]
    sDatabase = lsPaths[0] if lsPaths else os.path.join(sProject, "templates", "BlindSkills", "BlindSkills.db")
    log("Settings: database %s; audio only %s; sample %d bytes; timeout %d seconds" % (sDatabase, bAudioOnly, iSampleBytes, iTimeoutSeconds))
    if not os.path.isfile(sDatabase):
        say("No database at " + sDatabase + ". Nothing was checked.")
        return 2
    oDb = sqlite3.connect("file:" + sDatabase + "?mode=ro", uri=True)
    oDb.row_factory = sqlite3.Row
    lLessons = []
    for oRow in oDb.execute("SELECT * FROM lessons ORDER BY category, title"):
        dRow = dict(oRow)
        sAddress = next((str(dRow.get(s) or "").strip() for s in lsAddressFields if str(dRow.get(s) or "").strip().lower().startswith(("http://", "https://"))), "")
        lLessons.append({"title": dRow["title"], "url": sAddress, "medium": str(dRow.get("medium") or "").lower()})
    oDb.close()
    for dLesson in [d for d in lLessons if not d["url"]]: log("No playback address: " + dLesson["title"])
    if bAudioOnly: lLessons = [d for d in lLessons if d["medium"] == "audio"]
    sYtdlp = "" if bAudioOnly else findYtdlp()
    log("yt-dlp: " + (sYtdlp or "none"))
    say("Checking %d lessons, as an anonymous player would." % len(lLessons))
    lResults = []
    for iIndex, dLesson in enumerate(lLessons, 1):
        lResults.append(checkLesson(dLesson, sYtdlp))
        if iIndex % 10 == 0: say("Checked %d of %d." % (iIndex, len(lLessons)))
    sResults = os.path.join(sProject, "results")
    os.makedirs(sResults, exist_ok=True)
    with open(os.path.join(sResults, "BlindSkills-media.json"), "w", encoding="utf-8") as f: json.dump(lResults, f, ensure_ascii=False, indent=2)
    lPassed = [d for d in lResults if d["status"] == "answered"]
    with open(os.path.join(sResults, "BlindSkills_Checked.m3u"), "w", encoding="utf-8", newline="") as f:
        f.write("#EXTM3U\r\n" + "".join("#EXTINF:%d,%s\r\n%s\r\n" % (d["seconds"], d["title"], d["url"]) for d in lPassed))
    lGated = [d for d in lResults if d["status"] == "blocked"]
    lFailed = [d for d in lResults if d["status"] == "failed"]
    lSkipped = [d for d in lResults if d["status"] == "not checked"]
    say("%d of %d %s answered with audio or video; %d met a sign-in or bot check; %d failed; %d not checked." % (len(lPassed), len(lResults),
        "lesson" if len(lResults) == 1 else "lessons", len(lGated), len(lFailed), len(lSkipped)))
    if lPassed: say("Answering is the start of a stream, not proof the whole lesson plays; Homer Player is the final test.")
    if lGated: say("A sign-in or bot check can be about this network rather than the lesson; try again later or from another connection.")
    for d in lGated + lFailed: say("  %s: %s -- %s" % (d["status"], d["title"], d["reason"]))
    say("Play list of the lessons that answered: " + os.path.join(sResults, "BlindSkills_Checked.m3u"))
    say("Log: " + sLogPath)
    return 0 if len(lPassed) == len(lResults) else 1


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except Exception as oError:
        log("CRASH " + traceback.format_exc())
        print("checkBlindSkillsMedia stopped: %s. The log is %s." % (oError, sLogPath))
        sys.exit(1)
