"""makeDescribedFilms.py -- builds DbDo's DescribedFilms template from the Audio Description Project's title directory.

    scripts\\makeDescribedFilms                  fetches the directory from adp.acb.org and builds the database
    scripts\\makeDescribedFilms --from <folder>  builds from directory pages already saved in a folder, fetching nothing
    scripts\\makeDescribedFilms --limit <pages>  fetches only the first pages, for a quick trial

THE DIRECTORY. The American Council of the Blind's Audio Description Project (ADP) keeps the Film and Series Title
Directory at adp.acb.org/adp-search: every film and series available in the United States with audio description,
each with its release year, rating, genres, IMDb page, and the services that carry it -- each service a link to the
title there, with a note such as "Buy, Rent; TTS." It pages fifty titles at a time.

WHAT IS TAKEN (19 November 2026, Jamal's ruling): English movies only. ADP's own filters say so: "Movies Only"
(type_1=1) leaves out series, and its default language, labelled English, is the directory's main list. Documentaries
are movies there, filed under the Documentary genre, so they are in. Pages saved by hand (--from) may hold series too;
an entry whose media type is not Movie is left out and logged.

WHAT IT MAKES. templates\\DescribedFilms\\DescribedFilms.db, the Homer way (the homer-db skill), with the table helpers
makeMusicTrail uses:

  films    one row a movie: title, year, rating, genres, and services -- the services that carry it, in ADP's order,
           so the row says where to watch it and Filter Records finds every film on one service. url is the film's IMDb
           page. DbDo opens on this table.
  offers   one row a film on a service: service, terms (buy, rent, free with Prime, a channel subscription and so on),
           voice ("synthetic" when ADP marks the description as text-to-speech), and url, the film's page on that
           service. Alt+Right on a film goes to its offers, and Control+Shift+U on an offer opens it there.

defs hold the pick lists: each service, with its home page as url; each genre and rating; and the one voice value.

The old file is replaced only when the new one is complete and passes its own check. The log,
logs\\DbDo-makeDescribedFilms-<date>-<time>.log, names every page fetched, every film read and every entry left out.
"""

import collections, datetime, glob, html, os, platform, re, sqlite3, sys, time, traceback, urllib.error, urllib.parse, urllib.request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import makeMusicTrail as trail

iRetries = 3
iTimeout = 60
nDelay = 1.0
sBase = "https://adp.acb.org/adp-search?type_1=1&language_filter=All&order=title&sort=asc&page=%d"
sSource = "DescribedFilms"
sUserAgent = "DbDo makeDescribedFilms/1 (+https://github.com/JamalMazrui/DbDo; builds a template of audio-described films)"
sVersion = "2026-11-19 version 1: English movies from ADP's Film and Series Title Directory, with each service's offer"

dFilmTypes = {"genres": "TEXTLINE", "rating": "TEXTLINE", "services": "TEXTLINE", "title": "TEXTLINE", "year": "TEXTLINE"}
dOfferTypes = {"film_id": "INTEGER", "service": "TEXTLINE", "terms": "TEXTLINE", "voice": "TEXTLINE"}
dVoices = {"synthetic": "ADP marks this description as text-to-speech: software reads the script, not a human narrator."}
lsFilmFields = ["title", "year", "rating", "genres", "services"]
lsOfferFields = ["film_id", "service", "terms", "voice"]
# A STARTER WATCH LIST: three well-known films come marked, so the watch_list report shows what it does the first time.
# Unmark them freely. A film the directory no longer lists is logged and left alone.
lsStarterMarks = ["A Fish Called Wanda", "Hidden Figures", "The Wizard of Oz"]

sProject = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sLogPath = os.path.join(sProject, "logs", "DbDo-makeDescribedFilms-%s.log" % datetime.datetime.now().strftime("%Y%m%d-%H%M%S"))
trail.sLogPath = sLogPath  # the shared helpers write to this log


def log(sText): return trail.log(sText)
def say(sText): return trail.say(sText)


def plainText(sMarkup):
    """Markup to one line of text, entities undone however many times the site escaped them."""
    sText = re.sub(r"<[^>]+>", " ", sMarkup or "")
    for iPass in range(3):
        sNext = html.unescape(sText)
        if sNext == sText: break
        sText = sNext
    return re.sub(r"\s+", " ", sText).strip()


def fieldOf(sEntry, sName):
    oMatch = re.search(r'views-field-%s">.*?<(?:span|div) class="field-content">(.*?)</(?:span|div)>' % re.escape(sName), sEntry, re.S)
    return plainText(oMatch.group(1)) if oMatch else ""


def offersOf(sEntry):
    """Each service link of an entry as (service, terms, voice, url)."""
    oProviders = re.search(r'views-field-nothing">.*?<span class="field-content">(.*?)</span>\s*</div>', sEntry, re.S)
    lOffers = []
    if not oProviders: return lOffers
    for oLink in re.finditer(r"<a\s([^>]*)>(.*?)</a>", oProviders.group(1), re.S):
        sAttributes = oLink.group(1)
        oHref = re.search(r'href="([^"]*)"', sAttributes)
        oComment = re.search(r'title="with comment:\s*([^"]*)"', sAttributes)
        sUrl = html.unescape(oHref.group(1)).strip() if oHref else ""
        if not re.match(r"https?://", sUrl): sUrl = ""
        lsParts = [plainText(s).strip(" .") for s in re.split(r";", html.unescape(oComment.group(1)) if oComment else "")]
        sVoice = "synthetic" if any(s.upper() == "TTS" for s in lsParts) else ""
        sTerms = "; ".join(s for s in lsParts if s and s.upper() != "TTS")
        # ONE NAME A SERVICE (19 November 2026): where a service carries a film more than once -- a rental and a purchase,
        # a sign-language edition, bonus content -- ADP numbers the links, Prime Video 1, 2 and 3. The number says
        # nothing the terms do not, and it split one service into four in the pick list, so it is dropped.
        sService = re.sub(r"\s+\d+$", "", plainText(oLink.group(2)))
        if sService: lOffers.append((sService, sTerms, sVoice, sUrl))
    return lOffers


def entriesOf(sMarkup):
    """Each title entry of a directory page as a dictionary, with its media type."""
    lEntries = []
    lsChunks = re.split(r'<div class="views-view-responsive-grid__item-inner">', sMarkup)[1:]
    for sChunk in lsChunks:
        sTitle = fieldOf(sChunk, "title")
        if not sTitle: continue
        oImdb = re.search(r'href="(https?://(?:www\.)?imdb\.com/title/tt\d+)', sChunk)
        lEntries.append({"title": sTitle, "year": fieldOf(sChunk, "field-release-year"), "kind": fieldOf(sChunk, "type"),
                         "rating": fieldOf(sChunk, "field-rating"), "genres": fieldOf(sChunk, "field-genre"),
                         "url": oImdb.group(1).replace("http://", "https://") if oImdb else "", "offers": offersOf(sChunk)})
    return lEntries


def resultCount(sMarkup):
    oCount = re.search(r"Number of search results:\s*([\d,]+)", sMarkup)
    return int(oCount.group(1).replace(",", "")) if oCount else 0


def fetchPage(iPage):
    """One directory page, tried up to three times; the log has every attempt and its answer."""
    sUrl = sBase % iPage
    for iTry in range(1, iRetries + 1):
        nStart = time.time()
        try:
            oRequest = urllib.request.Request(sUrl, headers={"User-Agent": sUserAgent, "Accept": "text/html", "Referer": "https://adp.acb.org/"})
            with urllib.request.urlopen(oRequest, timeout=iTimeout) as oAnswer:
                sMarkup = oAnswer.read().decode("utf-8", errors="replace")
                log("GET %s: HTTP %d, %d characters, %.1f seconds, try %d" % (sUrl, oAnswer.status, len(sMarkup), time.time() - nStart, iTry))
                return sMarkup
        except (urllib.error.URLError, OSError) as oError:
            log("GET %s: failed on try %d after %.1f seconds: %s" % (sUrl, iTry, time.time() - nStart, oError))
            time.sleep(nDelay * 5 * iTry)
    raise RuntimeError("page %d of the directory did not answer after %d tries; the log has each one" % (iPage, iRetries))


def fetchDirectory(iLimit):
    """Every page of English movies, in title order, until a page comes back empty or the count is reached."""
    lEntries, iPage, iExpected = [], 0, 0
    while True:
        sMarkup = fetchPage(iPage)
        if iPage == 0:
            iExpected = resultCount(sMarkup)
            log("The directory lists %d English movies" % iExpected)
            say("The directory lists %d English movies; fetching them fifty at a time." % iExpected)
        lPage = entriesOf(sMarkup)
        log("Page %d: %d entries" % (iPage, len(lPage)))
        if not lPage: break
        lEntries += lPage
        iPage += 1
        if iPage % 20 == 0: say("  %d pages, %d movies so far ..." % (iPage, len(lEntries)))
        if iLimit and iPage >= iLimit: log("Stopped at the page limit, %d" % iLimit); break
        if iExpected and len(lEntries) >= iExpected: break
        time.sleep(nDelay)
    if iExpected and not iLimit and len(lEntries) < iExpected * 0.95:
        raise RuntimeError("only %d of the %d movies the directory lists were read; the old database is kept" % (len(lEntries), iExpected))
    return lEntries


def readFolder(sFolder):
    """Entries from directory pages saved in a folder, such as an HTTrack mirror; each page read once."""
    lEntries = []
    lsPaths = sorted(glob.glob(os.path.join(sFolder, "**", "*.htm*"), recursive=True))
    for sPath in lsPaths:
        with open(sPath, encoding="utf-8", errors="replace") as fPage: sMarkup = fPage.read()
        if "views-view-responsive-grid__item-inner" not in sMarkup: continue
        lPage = entriesOf(sMarkup)
        log("Saved page %s: %d entries" % (sPath, len(lPage)))
        lEntries += lPage
    return lEntries


def collectFilms(lEntries):
    """English movies, one per title and year: a title listed twice keeps every offer of both listings."""
    dFilms = collections.OrderedDict()
    for dEntry in lEntries:
        if dEntry["kind"] and dEntry["kind"].lower() != "movie":
            log("Left out, not a movie (%s): %s (%s)" % (dEntry["kind"], dEntry["title"], dEntry["year"]))
            continue
        sKey = dEntry["title"].lower() + "|" + dEntry["year"]
        if sKey not in dFilms:
            dFilms[sKey] = dict(dEntry, offers=list(dEntry["offers"]))
            continue
        dFilm = dFilms[sKey]
        for tOffer in dEntry["offers"]:
            if tOffer not in dFilm["offers"]: dFilm["offers"].append(tOffer)
        for sField in ("rating", "genres", "url"):
            if not dFilm[sField] and dEntry[sField]: dFilm[sField] = dEntry[sField]
    lFilms = list(dFilms.values())
    for dFilm in lFilms:
        lsServices = []
        for sService, sTerms, sVoice, sUrl in dFilm["offers"]:
            if sService not in lsServices: lsServices.append(sService)
        dFilm["services"] = ", ".join(lsServices)
    return lFilms


def serviceHomes(lFilms):
    """Each service's home page: the site most of its links go to."""
    dHosts = collections.defaultdict(collections.Counter)
    for dFilm in lFilms:
        for sService, sTerms, sVoice, sUrl in dFilm["offers"]:
            if sUrl: dHosts[sService][urllib.parse.urlsplit(sUrl).scheme + "://" + urllib.parse.urlsplit(sUrl).netloc + "/"] += 1
    # A catch-all such as Independent or Other links each film to its own maker's site, so it has no home of its own.
    return {sService: oCounts.most_common(1)[0][0] for sService, oCounts in dHosts.items() if sService.lower() not in ("independent", "other")}


def build(lFilms, sTarget):
    iOffers = sum(len(d["offers"]) for d in lFilms)
    say("Read %d English movies with %d offers on %d services." % (len(lFilms), iOffers, len({o[0] for d in lFilms for o in d["offers"]})))
    sTemp = sTarget + ".building"
    if os.path.exists(sTemp): os.remove(sTemp)
    os.makedirs(os.path.dirname(sTarget), exist_ok=True)
    oDb = sqlite3.connect(sTemp)
    # THE VIEWS TABLE IS RETIRED (4 November 2026): the shared helpers still make it for older builders, so it is left out.
    lsDdl = [s for s in trail.systemDdl() if not s.startswith("CREATE TABLE views")]
    lsDdl += trail.tableDdl("films", "film_id", lsFilmFields, ["title", "year"], ["title", "year"], dFilmTypes)
    # AN OFFER IS KNOWN BY ITS FILM (19 November 2026). Look and prime usually leave out an _id, but an offer has no name
    # of its own: one service may carry two films at the same address (a collection), so film_id, service and url
    # together are what make it unique, as northwind's orders keep customer_id and employee_id in theirs.
    lsDdl += trail.tableDdl("offers", "offer_id", lsOfferFields, ["service", "terms", "voice"], ["film_id", "service", "url"], dOfferTypes)
    for sDdl in lsDdl: oDb.execute(sDdl)
    oDb.execute("CREATE INDEX idx_offers_film_id ON offers (film_id)")
    oDb.execute("CREATE INDEX idx_offers_service ON offers (service)")
    oDb.execute("CREATE INDEX idx_films_year ON films (year)")
    dCounts = collections.Counter()
    for dFilm in sorted(lFilms, key=lambda d: (re.sub(r"^(a|an|the)\s+", "", d["title"].lower()), d["year"])):
        oCursor = oDb.execute("INSERT INTO films (title, year, rating, genres, services, url) VALUES (?, ?, ?, ?, ?, ?)",
                              [dFilm[s] or None for s in ("title", "year", "rating", "genres", "services", "url")])
        iFilm = oCursor.lastrowid
        setSeen = set()
        for sService, sTerms, sVoice, sUrl in dFilm["offers"]:
            if (sService, sUrl) in setSeen:
                log("Offer listed twice, kept once: %s on %s" % (dFilm["title"], sService))
                continue
            setSeen.add((sService, sUrl))
            oDb.execute("INSERT INTO offers (film_id, service, terms, voice, url) VALUES (?, ?, ?, ?, ?)", (iFilm, sService, sTerms or None, sVoice or None, sUrl or None))
            dCounts[sService] += 1
        log("Film: %s (%s) | %s | %s | %s" % (dFilm["title"], dFilm["year"] or "year unknown", dFilm["rating"], dFilm["services"], dFilm["url"]))
    dHomes = serviceHomes(lFilms)
    trail.addLookups(oDb, "offers", "service", {s: None for s in dCounts}, sSource)
    for sService, sHome in dHomes.items(): oDb.execute("UPDATE defs SET url = ? WHERE tbl = 'offers' AND fld = 'service' AND val = ?", (sHome, sService))
    trail.addLookups(oDb, "offers", "voice", dVoices, sSource)
    setGenres = {s.strip() for d in lFilms for s in d["genres"].split(",") if s.strip()}
    trail.addLookups(oDb, "films", "genres", {s: None for s in setGenres}, sSource)
    trail.addLookups(oDb, "films", "rating", {d["rating"]: None for d in lFilms if d["rating"]}, sSource)
    for sTitle in lsStarterMarks:
        if oDb.execute("UPDATE films SET marked = 1 WHERE title = ?", (sTitle,)).rowcount: log("Marked as a starter: " + sTitle)
        else: log("Starter not found, so not marked: " + sTitle)
    oDb.commit()
    sIntegrity = oDb.execute("PRAGMA integrity_check").fetchone()[0]
    iFilms = oDb.execute("SELECT count(*) FROM films").fetchone()[0]
    iSaved = oDb.execute("SELECT count(*) FROM offers").fetchone()[0]
    iSynthetic = oDb.execute("SELECT count(*) FROM offers WHERE voice = 'synthetic'").fetchone()[0]
    oDb.execute("PRAGMA journal_mode = DELETE")
    oDb.execute("VACUUM")
    oDb.close()
    log("Integrity %s; %d films; %d offers, %d synthetic; offers by service %s" % (sIntegrity, iFilms, iSaved, iSynthetic, dict(dCounts.most_common())))
    if sIntegrity != "ok" or iFilms != len(lFilms) or not lFilms:
        os.remove(sTemp)
        raise RuntimeError("the new database failed its own check (integrity %s, %d of %d films); the old one is kept" % (sIntegrity, iFilms, len(lFilms)))
    os.replace(sTemp, sTarget)
    say("Wrote %s: %d films, %d offers (%d with a synthetic voice), %d services." % (sTarget, iFilms, iSaved, iSynthetic, len(dCounts)))
    return iFilms


def main(lsArgs):
    log("makeDescribedFilms.py started; %s; script %s" % (sVersion, os.path.abspath(__file__)))
    log("Python %s at %s on %s; cwd %s; command line %s" % (platform.python_version(), sys.executable, platform.platform(), os.getcwd(), " ".join(sys.argv)))
    sFrom, iLimit = "", 0
    if "--from" in lsArgs:
        iAt = lsArgs.index("--from")
        sFrom = lsArgs[iAt + 1] if iAt + 1 < len(lsArgs) else ""
        if not os.path.isdir(sFrom):
            say("--from needs a folder of saved directory pages; %s is not one." % (sFrom or "nothing"))
            return 1
    if "--limit" in lsArgs:
        iAt = lsArgs.index("--limit")
        iLimit = int(lsArgs[iAt + 1]) if iAt + 1 < len(lsArgs) and lsArgs[iAt + 1].isdigit() else 0
    log("settings: from %s; page limit %s; address %s; delay %.1f seconds; timeout %d; retries %d" % (sFrom or "the web", iLimit or "none", sBase % 0, nDelay, iTimeout, iRetries))
    if sFrom:
        say("Reading saved directory pages in " + sFrom)
        lEntries = readFolder(sFrom)
    else:
        say("Fetching ADP's Film and Series Title Directory, English movies only. This takes about five minutes.")
        lEntries = fetchDirectory(iLimit)
    lFilms = collectFilms(lEntries)
    if not lFilms:
        say("No movies were read, so nothing was written. The log says why.")
        return 1
    build(lFilms, os.path.join(sProject, "templates", "DescribedFilms", "DescribedFilms.db"))
    say("Log: " + sLogPath)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except Exception as oError:
        log("CRASH " + traceback.format_exc())
        print("makeDescribedFilms stopped: %s. The log is %s." % (oError, sLogPath))
        sys.exit(1)
