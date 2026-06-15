#!/usr/bin/env python3
# build_convention.py - build NFB2026Convention.db from the official
# agenda document.
#
# Simplified design (v1.0.107): three noun tables plus the two
# standard infrastructure tables. No subevents, no tracks; every
# agenda entry is a discrete event.
#
#   contacts  - people and organizations. The field roster comes from
#               the DbDialog.mdb Contact schema, designed so an edit
#               dialog can give every field a distinct accelerator.
#   events    - one row per discrete agenda entry (date, times, title,
#               details).
#   locations - hotel rooms and spaces, with the level inferred per
#               the agenda's own rule (room numbers starting with N
#               are on level N; lettered salons are the Lone Star
#               Ballroom on 3; numbered salons are the JW Grand
#               Ballroom on 4).
#   maps      - generic typed associations between any two records,
#               identified by (table, unq) pairs. Kinds used here:
#               presents (contact -> event) and located_at
#               (event -> location). Full standard column set,
#               including look AND unq.
#   lookups   - the standard lookups table, seeded with the valid
#               maps.kind values and the hotel names.
#
# Usage:
#   python build_convention.py <agenda.docx-or-.txt> [output.db]
# If the input is .docx, python-docx is used when available;
# otherwise pre-extract the text (UTF-8) and pass the .txt.

import re
import sqlite3
import sys

c_sDefaultOutput = "NFB2026Convention.db"
c_sHotel = "JW Marriott Austin"

dDayDates = {
    "Friday": "2026-07-03", "Saturday": "2026-07-04", "Sunday": "2026-07-05",
    "Monday": "2026-07-06", "Tuesday": "2026-07-07", "Wednesday": "2026-07-08",
}

# ---------------------------------------------------------------------
# Schema
# ---------------------------------------------------------------------

def sLookExpr(lCols):
    lParts = ["iif(%s IS NOT NULL AND length(CAST(%s AS TEXT))>0, CAST(%s AS TEXT) || ' | ', '')"
              % (c, c, c) for c in lCols]
    return "rtrim(" + " || ".join(lParts) + ", ' | ')"

def sUnqExpr(lCols):
    return "||'|'||".join("coalesce(CAST(%s AS TEXT),'')" % c for c in lCols)

# Contacts use the conditional-unq pattern: a PERSON's identity is the
# name; an ORGANIZATION (no last_name) is identified by its enterprise.
c_sContactUnqExpr = ("iif(last_name IS NOT NULL AND length(last_name)>0, "
                     "coalesce(first_name,'')||'|'||coalesce(middle_name,'')"
                     "||'|'||coalesce(last_name,''), coalesce(enterprise,''))")

dSchema = {
    "contacts": dict(
        # Field order is the dialog/reading order: identity, then
        # professional (enterprise directly after the name, job with
        # it), then contact methods, then postal address, then url.
        data=["first_name TEXTLINE", "middle_name TEXTLINE", "last_name TEXTLINE",
              "enterprise TEXTLINE", "job TEXTLINE",
              "gender TEXTLINE", "date_of_birth TEXTLINE",
              "wireless_phone TEXTLINE", "home_phone TEXTLINE", "office_phone TEXTLINE",
              "personal_email TEXTLINE", "business_email TEXTLINE",
              "address1 TEXTLINE", "address2 TEXTLINE", "city TEXTLINE",
              "state TEXTLINE", "zip TEXTLINE", "nation TEXTLINE",
              "url TEXTLINE"],
        look=["first_name", "last_name", "enterprise"],
        unq=c_sContactUnqExpr),
    "events": dict(
        data=["event_date TEXTLINE", "start_time TEXTLINE", "end_time TEXTLINE",
              "title TEXTLINE", "details TEXTMARKDOWN", "url TEXTLINE"],
        look=["event_date", "start_time", "title"],
        unq=["event_date", "start_time", "title"]),
    "locations": dict(
        data=["name TEXTLINE", "level TEXTLINE", "hotel TEXTLINE"],
        look=["name", "level"],
        unq=["name", "hotel"]),
    "projects": dict(
        # A project is a product, service, or other ongoing endeavor
        # -- a work in progress that evolves over time. Ownership and
        # appearances are maps associations, never columns here.
        data=["name TEXTLINE", "kind TEXTLINE", "descrip TEXTMARKDOWN", "url TEXTLINE"],
        look=["name", "kind"],
        unq=["name"]),
    "maps": dict(
        data=["tbl1 TEXTLINE", "unq1 TEXTLINE", "kind TEXTLINE",
              "tbl2 TEXTLINE", "unq2 TEXTLINE"],
        look=["tbl1", "unq1", "kind", "tbl2", "unq2"],
        unq=["tbl1", "unq1", "kind", "tbl2", "unq2"]),
    "lookups": dict(
        data=["src TEXTLINE", "tbl TEXTLINE", "fld TEXTLINE", "val TEXTLINE",
              "ordinal INTEGER", "descrip TEXTMARKDOWN", "url TEXTLINE"],
        look=["src", "tbl", "fld", "val"],
        unq=["src", "tbl", "fld", "val"]),
}


def createSchema(conn):
    cur = conn.cursor()
    for sTable, d in dSchema.items():
        sSingular = sTable[:-1] if sTable.endswith("s") else sTable
        sPk = sSingular + "_id"
        lDataNames = [sCol.split()[0] for sCol in d["data"]]
        lLines = ["  %s INTEGER PRIMARY KEY AUTOINCREMENT" % sPk,
                  "  added TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP",
                  "  edited TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP"]
        lLines += ["  " + sCol for sCol in d["data"]]
        sUnq = d["unq"] if isinstance(d["unq"], str) else sUnqExpr(d["unq"])
        lLines += ["  notes TEXTMARKDOWN", "  tags TEXTMEMO",
                   "  look TEXT GENERATED ALWAYS AS (%s) STORED" % sLookExpr(d["look"]),
                   "  unq TEXT GENERATED ALWAYS AS (%s) STORED" % sUnq,
                   "  marked INTEGER NOT NULL DEFAULT 0"]
        cur.execute('CREATE TABLE "%s" (\n%s\n)' % (sTable, ",\n".join(lLines)))
        # Edited trigger: fires only when a data-bearing column is in
        # the SET list AND actually changed; marking never bumps it.
        lTrig = lDataNames + ["notes", "tags"]
        sOf = ", ".join('"%s"' % c for c in lTrig)
        sWhen = " OR ".join('OLD."%s" IS NOT NEW."%s"' % (c, c) for c in lTrig)
        cur.execute('CREATE TRIGGER "trg_%s_edited" AFTER UPDATE OF %s ON "%s" '
                    'FOR EACH ROW WHEN %s BEGIN UPDATE "%s" SET edited = '
                    'CURRENT_TIMESTAMP WHERE %s = NEW.%s; END'
                    % (sTable, sOf, sTable, sWhen, sTable, sPk, sPk))
    # Join-path indexes for maps, created up front; unq indexes are
    # added after load so duplicates can be detected first.
    cur.execute('CREATE INDEX idx_maps_side1 ON maps (tbl1, unq1)')
    cur.execute('CREATE INDEX idx_maps_side2 ON maps (tbl2, unq2)')
    conn.commit()


# ---------------------------------------------------------------------
# Agenda text acquisition
# ---------------------------------------------------------------------

def sLoadAgendaText(sPath):
    if sPath.lower().endswith(".docx"):
        try:
            import docx
        except ImportError:
            raise SystemExit("python-docx is not installed; pre-extract the "
                             "text and pass the .txt instead.")
        doc = docx.Document(sPath)
        lLines = []
        for p in doc.paragraphs:
            sStyle = (p.style.name or "").lower()
            sText = p.text
            if sStyle.startswith("heading 1"): sText = "# " + sText
            elif sStyle.startswith("heading 2"): sText = "## " + sText
            elif sStyle.startswith("heading 3"): sText = "### " + sText
            elif sStyle.startswith("heading 4"): sText = "#### " + sText
            lLines.append(sText)
        return "\n".join(lLines)
    return open(sPath, encoding="utf-8").read()


# ---------------------------------------------------------------------
# Time and location parsing
# ---------------------------------------------------------------------

c_sTimeToken = r"(?:[0-9]{1,2}(?::[0-9]{2})?|[Nn]oon|[Mm]idnight)"
c_sMeridiem = r"(?:a\.m\.|p\.m\.|am|pm)"
reTimeRange = re.compile(
    r"(%s)\s*(%s)?\s*(?:[\u2013\u2014-]|to)\s*(%s)\s*(%s)?" %
    (c_sTimeToken, c_sMeridiem, c_sTimeToken, c_sMeridiem))
reTimeSingle = re.compile(r"(%s)\s*(%s)" % (c_sTimeToken, c_sMeridiem))


def sTo24h(sTok, sMeridiem):
    sTok = sTok.lower()
    if sTok == "noon": return "12:00"
    if sTok == "midnight": return "00:00"
    if ":" in sTok: iHour, iMin = (int(x) for x in sTok.split(":"))
    else: iHour, iMin = int(sTok), 0
    bPm = sMeridiem is not None and sMeridiem.lower().startswith("p")
    bAm = sMeridiem is not None and sMeridiem.lower().startswith("a")
    if bPm and iHour != 12: iHour += 12
    if bAm and iHour == 12: iHour = 0
    return "%02d:%02d" % (iHour, iMin)


def tParseTimes(sText):
    """Return (start, end, remainder) from the front of sText, or
    (None, None, sText) when no leading time expression parses."""
    m = reTimeRange.match(sText)
    if m:
        sTok1, sMer1, sTok2, sMer2 = m.groups()
        # Shared trailing meridiem: "7:15-8:00 a.m." applies to both.
        # When only the trailing one is given and the raw start hour
        # is numerically greater than the end hour, the start is the
        # OTHER meridiem ("11:00-1:45 p.m." starts at 11 a.m.).
        if sMer1 is None and sMer2 is not None:
            sMer1 = sMer2
            try:
                iH1 = int(sTok1.split(":")[0]) % 12
                iH2 = int(sTok2.split(":")[0]) % 12
                if iH1 > iH2 and sTok1.lower() not in ("noon", "midnight"):
                    sMer1 = "a.m." if sMer2.lower().startswith("p") else "p.m."
            except ValueError:
                pass
        return (sTo24h(sTok1, sMer1), sTo24h(sTok2, sMer2),
                sText[m.end():].strip(" \u2013\u2014-").strip())
    m = reTimeSingle.match(sText)
    if m:
        return (sTo24h(m.group(1), m.group(2)), "",
                sText[m.end():].strip(" \u2013\u2014-").strip())
    return (None, None, sText)


reParenLevel = re.compile(r"\s*\((?:on\s+)?level\s+([0-9])\)", re.I)
reRoom = re.compile(r"^Rooms?\s+([0-9]{3})", re.I)
reTrailParen = re.compile(r"\(([^()]*(?:Room|Salon)[^()]*)\)\s*$", re.I)


def tParseLocation(sText):
    """Recognize a location phrase. Returns (name, level) or None."""
    if not sText: return None
    sText = sText.strip().rstrip(".")
    sLevel = ""
    m = reParenLevel.search(sText)
    if m:
        sLevel = m.group(1)
        sText = reParenLevel.sub("", sText).strip()
    if len(sText) > 60: return None
    m = reRoom.match(sText)
    if m:
        return (sText, sLevel or sText[len("Room "):][0] if False else (sLevel or m.group(1)[0]))
    sLow = sText.lower()
    bLooksLocation = (sLow.startswith(("salon", "room", "griffin hall",
                                       "jw grand ballroom", "lone star ballroom",
                                       "brazos", "exhibit hall")))
    if not bLooksLocation: return None
    if not sLevel:
        if re.search(r"salons?\s+[A-Ha-h]\b", sText): sLevel = "3"
        elif re.search(r"salons?\s+[0-9]", sText): sLevel = "4"
        elif "griffin" in sLow: sLevel = "2"
        elif "jw grand ballroom" in sLow: sLevel = "4"
        elif "lone star" in sLow: sLevel = "3"
    return (sText, sLevel)


# ---------------------------------------------------------------------
# Presenter parsing
# ---------------------------------------------------------------------

c_lNonNameStarts = ("room ", "salon", "zoom", "1capapp", "for ", "the ", "join ",
                    "sponsored", "preregistration", "registration", "any ",
                    "national ", "discuss", "learn", "discover", "your ",
                    "stop ", "all ", "we ", "this ", "it", "be ", "find ",
                    "access ", "kenneth ", "president riccobono", "affiliate ",
                    "final ", "experience ", "i ", "2026 ")
reNameWord = r"[A-Z][A-Za-z'\u2019.\-]+"
reName = re.compile(r"^(%s(?:\s+%s){0,3})$" % (reNameWord, reNameWord))

c_lUsStates = {"alabama", "alaska", "arizona", "arkansas", "california",
    "colorado", "connecticut", "delaware", "florida", "georgia", "hawaii",
    "idaho", "illinois", "indiana", "iowa", "kansas", "kentucky",
    "louisiana", "maine", "maryland", "massachusetts", "michigan",
    "minnesota", "mississippi", "missouri", "montana", "nebraska",
    "nevada", "new hampshire", "new jersey", "new mexico", "new york",
    "north carolina", "north dakota", "ohio", "oklahoma", "oregon",
    "pennsylvania", "rhode island", "south carolina", "south dakota",
    "tennessee", "texas", "utah", "vermont", "virginia", "washington",
    "west virginia", "wisconsin", "wyoming"}

c_lPluralRoles = ("co-", "chairs", "chairpersons", "coordinators",
                  "presenters", "facilitators", "hosts", "co-chairs")

# A comma part containing one of these words is a ROLE (job), not a
# name and not an organization.
c_lRoleTokens = {"chair", "chairs", "chairperson", "chairman", "chairwoman",
    "co-chair", "co-chairs", "president", "vice", "coordinator", "director",
    "manager", "specialist", "officer", "consultant", "moderator", "host",
    "facilitator", "instructor", "professor", "teacher", "engineer",
    "attorney", "ceo", "cto", "founder", "owner", "advocate", "analyst",
    "counselor", "secretary", "treasurer", "representative", "liaison",
    "ambassador", "dean", "principal", "supervisor", "lead", "head",
    "member", "chief", "executive", "senior", "junior", "associate",
    "vp", "avp", "svp", "evp"}

c_lNameSuffixes = {"esq", "esq.", "esquire", "phd", "ph.d.", "md", "m.d.", "jr", "jr.", "sr", "sr."}


def bHasRoleToken(sText):
    for w in sText.split():
        sW = w.lower().strip(".,")
        if sW in c_lRoleTokens: return True
        if sW.endswith("s") and sW[:-1] in c_lRoleTokens: return True
    return False


def bPluralRole(sJob):
    """True when the job's last word is a plural role ("Co-Chairs",
    "Accessibility Excellence Advocates") -- a credit that applies to
    every job-less presenter named before it."""
    if not sJob: return False
    sLast = sJob.split()[-1].lower().strip(".,")
    if sLast.startswith("co-"): return True
    return sLast.endswith("s") and (sLast in c_lRoleTokens or sLast[:-1] in c_lRoleTokens)


def lParsePresenters(sLine):
    """Parse a standalone presenter line into a list of dicts with
    first/middle/last/job/enterprise. Returns [] if the line does not
    look like a presenter credit."""
    sLine = sLine.strip().rstrip(".").strip()
    if not sLine or len(sLine) > 220: return []
    if sLine.lower().startswith(c_lNonNameStarts): return []
    # Semicolons either separate multiple credits ("A, Role; B, Role")
    # or set off a "; City, State" tail. Parse each segment; keep the
    # ones that look like credits, and reject city/state tails via the
    # state-name guard below.
    lSemiParts = [p.strip() for p in sLine.split(";") if p.strip()]
    if len(lSemiParts) > 1:
        lAll = []
        for sPart in lSemiParts:
            lAll.extend(lParsePresenters(sPart))
        if lAll: return lAll
    sLine = lSemiParts[0] if lSemiParts else sLine
    if "," not in sLine: return []
    # Split multi-presenter credits on ", and " / " and " between name groups.
    lSegments = re.split(r",?\s+and\s+", sLine)
    c_lOrgTokens = {"division", "committee", "association", "federation",
        "center", "group", "board", "department", "council", "institute",
        "fund", "foundation", "library", "university", "office",
        "program", "project", "team", "club", "society", "network",
        "alliance", "coalition", "recreation", "services", "national"}

    def bIsPersonName(sPart):
        if not reName.match(sPart): return False
        lW = sPart.split()
        if len(lW) < 2 or len(lW) > 4: return False
        if any(w.lower().rstrip(".,") in c_lOrgTokens for w in lW): return False
        if bHasRoleToken(sPart): return False
        return True

    def dPerson(sPart):
        lW = sPart.split()
        return dict(first=lW[0], middle=" ".join(lW[1:-1]), last=lW[-1],
                    job="", enterprise="")

    lFound = []
    sSharedTail = None
    for iSeg, sSeg in enumerate(lSegments):
        lParts = [p.strip() for p in sSeg.split(",") if p.strip()]
        # Drop honorific suffixes (Esq., PhD) -- not data worth a column.
        lParts = [p for p in lParts if p.lower() not in c_lNameSuffixes]
        if not lParts: continue
        if not bIsPersonName(lParts[0]):
            return []  # first part not a clean person name: not a credit line
        # Classify each subsequent part: a role token makes it the
        # job; a clean person name is ANOTHER presenter; anything
        # else starts the organization (which runs to segment end).
        lSegPeople = [dPerson(lParts[0])]
        sJob = ""
        sOrg = ""
        for iPart in range(1, len(lParts)):
            sPart = lParts[iPart]
            if sOrg:
                sOrg += ", " + sPart
            elif bHasRoleToken(sPart):
                sJob = (sJob + ", " + sPart) if sJob else sPart
            elif bIsPersonName(sPart) and not sJob:
                lSegPeople.append(dPerson(sPart))
            else:
                sOrg = sPart
        for dP in lSegPeople:
            dP["job"] = sJob if dP is lSegPeople[-1] or len(lSegPeople) == 1 else ""
            dP["enterprise"] = sOrg
        # A role stated once in a multi-name segment applies to the
        # whole segment ("A, B, and C, Co-Chairs" handled at the
        # plural-role stage; here a job binds to its nearest name).
        lFound.extend(lSegPeople)
        if sOrg: sSharedTail = sOrg
    # "A, Title1, and B, Title2, Org": the trailing org applies to all.
    if sSharedTail:
        for d in lFound:
            if not d["enterprise"]: d["enterprise"] = sSharedTail
    # Reject lines whose "job" is actually prose (too long / verbish),
    # and city/state tails ("Goodyear, Arizona") where the "job" is a
    # bare US state name.
    for d in lFound:
        if len(d["job"]) > 80: return []
        if d["job"].lower() in c_lUsStates: return []
    # A plural role anywhere in the credit ("A and B, Co-Chairs, and
    # C, Director") applies to every job-less presenter named BEFORE
    # the person carrying it -- the natural reading of such lines.
    for iPerson, d in enumerate(lFound):
        if d["job"] and bPluralRole(d["job"]):
            for iEarlier in range(iPerson):
                if not lFound[iEarlier]["job"]:
                    lFound[iEarlier]["job"] = d["job"]
    return lFound


# ---------------------------------------------------------------------
# Agenda walk
# ---------------------------------------------------------------------

# ---------------------------------------------------------------------
# Tag derivation. The tags field holds one short, lowercase tag per
# line (a TEXTMEMO column), chosen so a screen-reader user can filter
# the data list by a single typed tag -- e.g. "workshop", "saturday",
# "presenter", "ballroom".
# ---------------------------------------------------------------------

c_lEventTagRules = [
    ("general session", "general session"), ("workshop", "workshop"),
    ("seminar", "seminar"), ("board", "board"), ("committee", "committee"),
    ("division", "division"), ("luncheon", "meal"), ("banquet", "meal"),
    ("breakfast", "meal"), ("dinner", "meal"), ("brunch", "meal"),
    ("reception", "social"), ("social", "social"), ("mixer", "social"),
    ("dance", "social"), ("party", "social"), ("tour", "tour"),
    ("exhibit", "exhibit hall"), ("award", "recognition"),
    ("scholarship", "recognition"), ("registration", "registration"),
    ("meeting", "meeting"),
]

def lEventTags(dEv):
    """Weekday, then event-type tags inferred from the title, then a
    'virtual' tag when the event has a streaming link."""
    lTags = []
    if dEv.get("dayname"): lTags.append(dEv["dayname"].lower())
    sT = (dEv.get("title") or "").lower()
    for sKey, sTag in c_lEventTagRules:
        if sKey in sT and sTag not in lTags: lTags.append(sTag)
    if dEv.get("url") and "virtual" not in lTags: lTags.append("virtual")
    return lTags

c_lContactRoleKw = ("president", "director", "chair", "coordinator",
                    "manager", "officer", "founder", "secretary",
                    "treasurer", "chief", "specialist", "counsel")

def lContactTags(dC, setKinds):
    """person vs organization, the association roles the contact plays
    (presenter / sponsor / provider), and any title keyword in the job."""
    lTags = ["person" if dC.get("last") else "organization"]
    for sKind, sTag in (("presents", "presenter"), ("sponsors", "sponsor"),
                        ("offers", "provider")):
        if sKind in setKinds: lTags.append(sTag)
    sJob = (dC.get("job") or "").lower()
    for sKw in c_lContactRoleKw:
        if sKw in sJob and sKw not in lTags: lTags.append(sKw)
    return lTags

def lLocationTags(sName, sLevel):
    """The level, then the kind of space inferred from the name."""
    lTags = []
    if sLevel: lTags.append(sLevel.lower())
    sN = (sName or "").lower()
    for sKey, sTag in (("ballroom", "ballroom"), ("salon", "salon"),
                       ("boardroom", "boardroom"), ("suite", "suite"),
                       ("foyer", "foyer"), ("terrace", "terrace"),
                       ("lobby", "lobby"), ("room", "room"), ("hall", "hall")):
        if sKey in sN:
            lTags.append(sTag)
            break
    return lTags


def buildDatabase(sAgendaPath, sDbPath):
    sText = sLoadAgendaText(sAgendaPath)
    lLines = sText.splitlines()

    conn = sqlite3.connect(sDbPath)
    createSchema(conn)
    cur = conn.cursor()

    dContacts = {}   # unq -> contact dict
    dLocations = {}  # unq -> (name, level)
    dEvents = {}     # unq -> event dict
    dProjects = {}   # unq (name) -> dict(name, kind)
    lMaps = []       # (tbl1, unq1, kind, tbl2, unq2, notes)

    lProjectHits = []  # (name, kind, owner, event unq)

    def harvestProjects(dEv, sEvUnq):
        """Collect candidate project mentions from an event's title and
        details. Names are canonicalized in a second pass once every
        variant has been seen, so 'Parent Leadership Program' merges
        into 'NOPBC Parent Leadership Program'."""
        sScan = (dEv["title"] or "") + "\n" + (dEv["details"] or "")
        for m in reProgram.finditer(sScan):
            sName = m.group(1).strip()
            for sLead in c_lProjectStrip:
                if sName.startswith(sLead): sName = sName[len(sLead):]
            if len(sName.split()) < 2: continue
            if sName in c_lProjectStop: continue
            lProjectHits.append((sName, "program", None, sEvUnq))
        for sBrand, (sKind, sOwner) in dCuratedProjects.items():
            if re.search(r"\b" + re.escape(sBrand) + r"\b", sScan):
                lProjectHits.append((sBrand, sKind, sOwner, sEvUnq))

    def resolveProjects():
        """Second pass: a name that is a strict word-suffix of a longer
        seen name is the same project under its fuller title; the
        longer form wins. Then create the project rows and the maps
        associations (features from events; offers from owning orgs)."""
        lNames = sorted(set(h[0] for h in lProjectHits), key=len, reverse=True)
        dCanon = {}
        for sName in lNames:
            sCanonical = sName
            for sLonger in lNames:
                if len(sLonger) > len(sName) and sLonger.endswith(" " + sName):
                    sCanonical = dCanon.get(sLonger, sLonger)
                    break
            dCanon[sName] = sCanonical
        for sName, sKind, sOwner, sEvUnq in lProjectHits:
            sFull = dCanon[sName]
            if sFull not in dProjects:
                dProjects[sFull] = dict(name=sFull, kind=sKind)
            lMaps.append(("events", sEvUnq, "features", "projects", sFull, ""))
            if sOwner:
                sOUnq = addOrgContact(sOwner)
                lMaps.append(("contacts", sOUnq, "offers", "projects", sFull, ""))

    # A contact's identity mirrors the table's conditional unq
    # expression exactly (the maps rows store these strings, so the
    # Python computation and the SQL generated column MUST agree):
    # a person's identity is the name; an organization's (no last
    # name) is its enterprise. The person row keeps the richest
    # professional data seen; everything else lands in notes.
    def sContactUnq(d):
        if d["last"]:
            return "%s|%s|%s" % (d["first"], d["middle"], d["last"])
        return d["enterprise"]

    def sEventUnq(d):
        return "%s|%s|%s" % (d["date"], d["start"], d["title"])

    def addLocation(sName, sLevel):
        sUnq = "%s|%s" % (sName, c_sHotel)
        if sUnq not in dLocations: dLocations[sUnq] = (sName, sLevel)
        return sUnq

    def addEvent(dEv):
        dEvents.setdefault(sEventUnq(dEv), dEv)
        return sEventUnq(dEv)

    reSponsor = re.compile(r"Sponsored by (?:the )?([A-Z][^.;\n]{3,120}?)(?:\.|;|$)")

    # Project detection. Pattern A: named programs by suffix word
    # ("... Program", "... Academy", "... Award", "... Scholarship",
    # "... Camp", "... Fair", "... Contest", "... Initiative").
    # Pattern B: curated product/service brands, each with its kind
    # and (when unambiguous in the agenda) the enterprise that offers
    # it -- used to add an 'offers' association from the organization.
    reProgram = re.compile(
        r"\b((?:[A-Z][\w'\u2019-]*\s+(?:&\s+)?){1,4}"
        r"(?:Program|Academy|Award|Scholarships?|Camp|Fair|Contest|Initiative))\b")
    dCuratedProjects = {
        "NFB-NEWSLINE": ("service", None),
        "Aira": ("service", "Aira"),
        "Dot Pad": ("product", "Dot Inc"),
        "Monarch": ("product", None),
        "1CapApp": ("service", None),
        # Products and services named in session titles/details, each tied
        # to the event that features it and, where the agenda makes the
        # maker evident, to that maker (offers). Kind: product = hardware
        # device, app = software application, service = online/AI service.
        "ChatGPT": ("service", None),
        "Claude": ("service", None),
        "Gemini": ("service", None),
        "Microsoft 365 Copilot": ("service", "Microsoft"),
        "Copilot": ("service", "Microsoft"),
        "Narrator": ("app", "Microsoft"),
        "Seeing AI": ("app", "Microsoft"),
        "JAWS": ("app", "Vispero"),
        "BrailleNote Evolve": ("product", "HumanWare"),
        "VictorReader Stream": ("product", "HumanWare"),
        "Ray-Ban Meta": ("product", "Meta"),
    }
    # Leading words that begin sentences rather than names, and whole
    # matches that are descriptive phrases rather than named projects.
    c_lProjectStrip = ("The ", "This ", "Our ", "A ", "An ", "Each ", "Every ",
                       "Annual ", "Your ", "His ", "Her ", "Their ")
    c_lProjectStop = {"Welcome Fair", "Job Fair"}
    reZoomUrl = re.compile(r"((?:https?://)?[\w.-]*zoom\.us/[^\s)\]]+)", re.I)
    reAnyUrl = re.compile(r"(https?://[^\s)\]]+)", re.I)

    def addOrgContact(sOrgName):
        """An organization is a contact with enterprise only; its unq
        is the enterprise name (the conditional-unq pattern)."""
        sOrgName = sOrgName.strip().rstrip(",")
        dOrg = dict(first="", middle="", last="", job="", enterprise=sOrgName)
        return addContact(dOrg)

    def addContact(dC):
        """Prefer professional data for the dedicated columns; never
        lose data -- additional distinct role/affiliation pairs go to
        the contact's notes as "Also: role, org" lines."""
        sUnq = sContactUnq(dC)
        dExisting = dContacts.get(sUnq)
        if dExisting is None:
            dC = dict(dC)
            dC.setdefault("alsoRoles", [])
            dContacts[sUnq] = dC
            return sUnq
        # Component-wise merge: fill an empty job or enterprise from
        # the new sighting; a CONFLICTING value (both non-empty and
        # different) is preserved as an "Also:" note rather than lost.
        lDemoted = []
        for sKey in ("job", "enterprise"):
            sNew, sOld = dC[sKey], dExisting[sKey]
            if not sNew or sNew == sOld: continue
            if not sOld:
                dExisting[sKey] = sNew
            else:
                lDemoted.append(sNew)
        if lDemoted:
            sAlso = ", ".join(lDemoted)
            if sAlso not in dExisting["alsoRoles"]:
                dExisting["alsoRoles"].append(sAlso)
        return sUnq

    sDate = None
    sDayName = None
    iLine = 0
    lCurrent = None  # open event being accumulated: dict + body list
    lSessionTimes = (None, None)  # general-session time window

    def closeCurrent():
        nonlocal lCurrent
        if lCurrent is None: return
        dEv, lBody = lCurrent
        # First location-looking body line becomes located_at; remaining
        # body text becomes details. Trailing presenter line(s) become
        # presents maps.
        lDetail = []
        sLocUnq = None
        lPresenterRows = []
        for iB, sB in enumerate(lBody):
            tLoc = tParseLocation(sB)
            if tLoc and sLocUnq is None and iB <= 1:
                sLocUnq = addLocation(tLoc[0], tLoc[1])
                continue
            lDetail.append(sB)
        # Presenter credits: scan trailing detail lines.
        while lDetail:
            lP = lParsePresenters(lDetail[-1])
            if not lP: break
            lPresenterRows = lP + lPresenterRows
            lDetail.pop()
        # Logistics lines (Zoom, 1CapApp, dial-in) are misc info, not
        # description: they move to the event's notes, and the first
        # Zoom web link also fills the standard url column.
        lKeep, lLogistics = [], []
        for sD in lDetail:
            if re.match(r"^(Zoom|1CapApp)", sD, re.I): lLogistics.append(sD)
            else: lKeep.append(sD)
        dEv["details"] = "\n".join(lKeep).strip()
        dEv["notes"] = "\n".join(lLogistics).strip()
        mZoom = reZoomUrl.search(dEv["notes"]) or reZoomUrl.search(dEv["details"])
        dEv["url"] = mZoom.group(1) if mZoom else ""
        if not dEv["url"]:
            # No Zoom link: fall back to the first plain web link in the
            # description or logistics so the standard url column is used
            # whenever the agenda offers any link at all.
            mAny = reAnyUrl.search(dEv["details"]) or reAnyUrl.search(dEv["notes"])
            dEv["url"] = mAny.group(1) if mAny else ""
        # Trim trailing sentence punctuation a greedy URL match can grab.
        dEv["url"] = (dEv["url"] or "").rstrip(".,;:")
        sEvUnq = addEvent(dEv)
        harvestProjects(dEv, sEvUnq)
        # Sponsors named in the description become organization
        # contacts related through maps with kind 'sponsors', and their
        # names are also appended to the event notes so the sponsorship
        # is readable on the event itself, not only via the relation.
        lSponsorNames = []
        for mS in reSponsor.finditer(dEv["details"]):
            sOrg = re.sub(r"\s*\([A-Z][A-Za-z&. ]{1,15}\)\s*$", "", mS.group(1)).strip()
            if len(sOrg) < 4: continue
            sOrgUnq = addOrgContact(sOrg)
            lMaps.append(("contacts", sOrgUnq, "sponsors", "events", sEvUnq, ""))
            if sOrg not in lSponsorNames: lSponsorNames.append(sOrg)
        if lSponsorNames:
            sNote = "Sponsored by: " + ", ".join(lSponsorNames)
            dEv["notes"] = (dEv["notes"] + "\n" + sNote).strip() if dEv["notes"] else sNote
        if sLocUnq:
            lMaps.append(("events", sEvUnq, "located_at", "locations", sLocUnq, ""))
        for dP in lPresenterRows:
            sCUnq = addContact(dP)
            lNote = []
            if dP["job"]: lNote.append("role: " + dP["job"])
            if dP["enterprise"]: lNote.append("org: " + dP["enterprise"])
            lMaps.append(("contacts", sCUnq, "presents", "events", sEvUnq,
                          "; ".join(lNote)))
        lCurrent = None

    while iLine < len(lLines):
        sRaw = lLines[iLine].strip()
        iLine += 1
        if not sRaw: continue
        # Strip markdown emphasis and links for parsing purposes.
        sLine = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", sRaw)
        sLine = sLine.replace("**", "").replace("*", "").strip()

        mDay = re.match(r"^##\s+(Friday|Saturday|Sunday|Monday|Tuesday|Wednesday),", sLine)
        if mDay:
            closeCurrent()
            sDate = dDayDates[mDay.group(1)]
            sDayName = mDay.group(1)
            lSessionTimes = (None, None)
            continue
        if sLine.startswith("## "):
            closeCurrent()
            sDate = None  # left the day sections
            sDayName = None
            continue
        if sDate is None: continue

        mH = re.match(r"^(#{3,4})\s+(.*)$", sLine)
        if mH:
            closeCurrent()
            sHead = mH.group(2).strip()
            sStart, sEnd, sTitle = tParseTimes(sHead)
            if sStart is None:
                # Time may trail in parens: "Opening General Session (9:00 a.m.-12:00 p.m.)"
                mTail = re.search(r"\(([^()]*[0-9][^()]*)\)\s*$", sHead)
                if mTail:
                    sStart, sEnd, _ = tParseTimes(mTail.group(1))
                    if sStart is not None:
                        sTitle = sHead[:mTail.start()].strip()
            if sStart is None:
                # Untimed #### inside a general session inherits the
                # session window (discrete event, same time span).
                if mH.group(1) == "####" and lSessionTimes[0]:
                    sStart, sEnd = lSessionTimes
                    sTitle = sHead
                else:
                    continue  # untimed ### heading: not an event
            else:
                sTitle = sTitle.strip(" \u2013\u2014-")
            if mH.group(1) == "###" and "general session" in sTitle.lower():
                lSessionTimes = (sStart, sEnd)
            lCurrent = (dict(date=sDate, start=sStart, end=sEnd or "",
                             title=sTitle, details="", dayname=sDayName), [])
            continue

        if lCurrent is not None:
            # Workshop list lines with a trailing room: discrete events.
            mW = reTrailParen.search(sLine)
            if mW and not sLine.startswith(("-", "Zoom", "1CapApp")):
                tLoc = tParseLocation(mW.group(1))
                if tLoc:
                    dParent = lCurrent[0]
                    sWTitle = reTrailParen.sub("", sLine).strip().rstrip(":").strip()
                    if sWTitle and sWTitle != dParent["title"]:
                        dW = dict(date=dParent["date"], start=dParent["start"],
                                  end=dParent["end"], title=sWTitle, details="",
                                  notes="", url="", dayname=dParent.get("dayname"))
                        sWUnq = addEvent(dW)
                        lMaps.append(("events", sWUnq, "located_at", "locations",
                                      addLocation(tLoc[0], tLoc[1]), ""))
                        continue
            lCurrent[1].append(sLine)

    closeCurrent()
    resolveProjects()

    # Products named only in the sponsor-ad section (not in any session
    # body, so the per-event scan above never sees them). Added as
    # projects with an offers map to their maker; no featuring event and
    # no room, since a sponsor ad ties them to a company, not a session.
    dSponsorProducts = {
        "Nemonic Dot Printer": ("product", "Dot Inc"),
        "BrailleSense 7": ("product", "Selvas BLV"),
    }
    for sBrand, (sKind, sOwner) in dSponsorProducts.items():
        if re.search(r"\b" + re.escape(sBrand) + r"\b", sText):
            if sBrand not in dProjects:
                dProjects[sBrand] = dict(name=sBrand, kind=sKind)
            if sOwner:
                sOUnq = addOrgContact(sOwner)
                lMaps.append(("contacts", sOUnq, "offers", "projects", sBrand, ""))

    # Location notes: bullets from the "Navigate the Hotel" section
    # that name a known location are preserved on that location's
    # notes -- misc way-finding info with no dedicated column.
    dLocationNotes = {}
    bInNavigate = False
    for sRaw in lLines:
        sLine = sRaw.strip().lstrip("- ").replace("**", "")
        if sLine.startswith("## "):
            bInNavigate = sLine.lower().startswith("## navigate the hotel")
            continue
        if not bInNavigate or not sLine or sLine.startswith("#"): continue
        if re.match(r"^Page \d+$", sLine): continue
        for sUnq, (sName, sLevel) in dLocations.items():
            if re.search(r"\b" + re.escape(sName) + r"\b(?!\s+(?:Street|Avenue|Boulevard|Drive))", sLine, re.I):
                dLocationNotes.setdefault(sUnq, [])
                if sLine not in dLocationNotes[sUnq]:
                    dLocationNotes[sUnq].append(sLine)

    # ---------------- inserts ----------------
    # Which association kinds each contact plays (subject side of
    # maps), used to tag contacts as presenter / sponsor / provider.
    dContactKinds = {}
    for t in lMaps:
        if t[0] == "contacts": dContactKinds.setdefault(t[1], set()).add(t[2])
    # Curated bio URLs for clearly public figures whose identity and
    # authoritative bio page were verified by research. The agenda carries
    # no per-presenter URLs, so in practice nothing in it competes; the
    # d.get("url") check below still lets an agenda-supplied URL take
    # precedence if one ever appears. Keyed by "First Last". Add a URL
    # ONLY when it is unambiguously the right person and an authoritative
    # source (official organization bio, the person's own site, or
    # Wikipedia) -- never a bare name-matched guess such as a LinkedIn
    # profile that happens to share the name.
    c_dContactBioUrls = {
        # Each verified by matching the agenda's organization and role to
        # the linked profile, so the association is beyond reasonable
        # doubt. Authoritative org/personal bios are preferred; a LinkedIn
        # profile is used when it is unambiguously the right person (its
        # stated employer and title match the agenda) and no cleaner bio
        # exists.
        "Mark Riccobono": "https://nfb.org/about-us/leadership/presidents-corner/mark-riccobono",
        "Jonathan Mosen": "https://mosen.org/",
        "Jason Broughton": "https://www.loc.gov/nls/who-we-are/",
        "Saqib Shaikh": "https://blogs.microsoft.com/accessibility/author/saqib-shaikh-founder-and-lead-microsoft-seeing-ai/",
        "Troy Otillio": "https://www.linkedin.com/in/troyo/",
        "Gary Wunder": "https://www.linkedin.com/in/gary-wunder-35960b29/",
        "Ronza Othman": "https://www.linkedin.com/in/ronza-othman-4b44698/",
        "J.J. Meddaugh": "https://www.linkedin.com/in/jasonmeddaugh/",
        "Everette Bacon": "https://nfb.org/about-us/leadership/board-directors/everette-bacon",
        # Named-employer individuals, each verified beyond reasonable doubt by
        # matching the agenda's organization and role to the linked source.
        # Where an authoritative employer bio exists it is the primary url and
        # any LinkedIn goes to notes (see c_dContactLinkedIn); otherwise an
        # unambiguous LinkedIn profile is the primary url.
        "Kiran Kaja": "https://www.linkedin.com/in/kirankaja12/",
        "Greg Stilson": "https://www.linkedin.com/in/greg-stilson/",
        "Nimer Jaber": "https://www.linkedin.com/in/nimer-jaber-68b96ba5/",
        "Danielle Lane": "https://www.linkedin.com/in/danielle-montour-a586b9238/",
        "Josh Loebner": "https://www.vml.com/people/josh-loebner",
        # Second batch. NFB national board members carry their official
        # nfb.org board-director bios; others use an authoritative employer
        # bio (LinkedIn then in notes) or an unambiguous LinkedIn.
        "Donald Porterfield": "https://nfb.org/about-us/leadership/board-directors/donald-porterfield",
        "Shawn Callaway": "https://nfb.org/about-us/leadership/board-directors/shawn-callaway",
        "Krystle Allen": "https://eyeslikemine.org/team/",
        "Jerred Mace": "https://www.onecourt.io/about",
        "Tanner Gers": "https://accessabilityofficer.com/about",
        "Nicky Shaw": "https://www.linkedin.com/in/nicky-s-52077425/",
        "Max Schafer": "https://www.linkedin.com/in/max-schafer-4914988/",
        # Third batch. All confirmed via exact employer + role; LinkedIn is
        # the primary url (no authoritative employer bio page exists for
        # these). "Andrew Flattres" keeps the agenda's misspelling so the key
        # matches the stored contact; his profile confirms the correct
        # spelling "Flatres".
        "Matt Philipenko": "https://www.linkedin.com/in/mattphilipenko/",
        "Kyungjun Lee": "https://www.linkedin.com/in/kyungjunlee/",
        "Andrew Flattres": "https://www.linkedin.com/in/andrew-flatres-3a54b695/",
    }
    # Secondary URLs worth exposing when the contact already carries a primary
    # url (above or from the agenda). Appended to notes as "LinkedIn: <url>"
    # only when present and different from the primary url. Keyed by
    # "First Last".
    c_dContactLinkedIn = {
        "Josh Loebner": "https://www.linkedin.com/in/joshloebner/",
        "Krystle Allen": "https://www.linkedin.com/in/krystle-allen-21529a32/",
        "Jerred Mace": "https://www.linkedin.com/in/jerred-mace/",
        "Tanner Gers": "https://www.linkedin.com/in/tannergers/",
    }
    for sUnq, d in dContacts.items():
        sFullName = " ".join(x for x in (d["first"], d["last"]) if x)
        sUrl = d.get("url") or c_dContactBioUrls.get(sFullName)
        lAlso = [a for a in d.get("alsoRoles", [])
                 if a != ", ".join(x for x in (d["job"], d["enterprise"]) if x)]
        lNoteLines = ["Also: " + a for a in lAlso]
        sLinked = c_dContactLinkedIn.get(sFullName)
        if sLinked and sLinked != sUrl: lNoteLines.append("LinkedIn: " + sLinked)
        sNotes = "\n".join(lNoteLines) or None
        sTags = "\n".join(lContactTags(d, dContactKinds.get(sUnq, set()))) or None
        cur.execute("INSERT INTO contacts (first_name, middle_name, last_name, "
                    "enterprise, job, notes, url, tags) VALUES (?,?,?,?,?,?,?,?)",
                    (d["first"] or None, d["middle"] or None, d["last"] or None,
                     d["enterprise"] or None, d["job"] or None, sNotes,
                     sUrl or None, sTags))
    for sUnq, (sName, sLevel) in dLocations.items():
        sNotes = "\n".join(dLocationNotes.get(sUnq, [])) or None
        sTags = "\n".join(lLocationTags(sName, sLevel)) or None
        cur.execute("INSERT INTO locations (name, level, hotel, notes, tags) "
                    "VALUES (?,?,?,?,?)",
                    (sName, sLevel or None, c_sHotel, sNotes, sTags))
    for d in dEvents.values():
        sTags = "\n".join(lEventTags(d)) or None
        cur.execute("INSERT INTO events (event_date, start_time, end_time, title, "
                    "details, notes, url, tags) VALUES (?,?,?,?,?,?,?,?)",
                    (d["date"], d["start"], d["end"] or None, d["title"],
                     d["details"] or None, d.get("notes") or None,
                     d.get("url") or None, sTags))
    dMapRows = {}
    lMapOrder = []
    for d in dProjects.values():
        sTags = d["kind"] or None
        cur.execute("INSERT INTO projects (name, kind, tags) VALUES (?,?,?)",
                    (d["name"], d["kind"], sTags))
    for t in lMaps:
        tKey = t[:5]
        if tKey not in dMapRows:
            dMapRows[tKey] = []
            lMapOrder.append(tKey)
        if t[5] and t[5] not in dMapRows[tKey]:
            dMapRows[tKey].append(t[5])
    for tKey in lMapOrder:
        addMap(cur, tKey[0], tKey[1], tKey[2], tKey[3], tKey[4],
               "; ".join(dMapRows[tKey]) or None)

    # Lookups seed: valid maps.kind values plus hotel names.
    lSeed = [
        ("NFB26", "maps", "kind", "presents", 1,
         "Subject contact presents at, chairs, or leads the object event. "
         "The stated role is in the map row's notes."),
        ("NFB26", "maps", "kind", "located_at", 2,
         "Subject event takes place at the object location."),
        ("NFB26", "maps", "kind", "sponsors", 3,
         "Subject contact or organization sponsors the object event."),
        ("NFB26", "maps", "kind", "features", 4,
         "Subject event features, demonstrates, or discusses the object project."),
        ("NFB26", "maps", "kind", "offers", 5,
         "Subject contact or organization provides the object project "
         "(its product, service, or program)."),
        ("NFB26", "projects", "kind", "product", 1,
         "A tangible offering that evolves over releases."),
        ("NFB26", "projects", "kind", "service", 2,
         "An ongoing offering delivered over time."),
        ("NFB26", "projects", "kind", "program", 3,
         "An organized, recurring endeavor such as an academy, award, or scholarship."),
        ("NFB26", "projects", "kind", "app", 4,
         "A software application for a computer or mobile device."),
        ("NFB26", "projects", "kind", "other", 5,
         "Any other ongoing endeavor."),
        ("NFB26", "locations", "hotel", c_sHotel, 1,
         "Headquarters hotel, 110 E 2nd Street, Austin, Texas 78701."),
        ("NFB26", "locations", "hotel", "Marriott Austin Downtown", 2,
         "Overflow hotel."),
    ]
    for t in lSeed:
        cur.execute("INSERT INTO lookups (src, tbl, fld, val, ordinal, descrip) "
                    "VALUES (?,?,?,?,?,?)", t)
    # Enrichment pass: promote broad-sense organizations into projects
    # with rich descrip / url / notes / tags, and add affiliation and
    # parent-of maps. Runs against the rows just inserted above.
    iCleaned = cleanContactEnterprises(cur)
    if iCleaned: print("Tightened %d contact enterprise value(s)." % iCleaned)
    enrichOrganizations(cur, lLines)
    (iDrop, iFix, iProj, iMaps) = cleanArtifacts(cur)
    print("Cleaned artifacts: dropped %d non-person contact(s), repaired %d, "
          "resolved %d project(s), removed %d map(s)." % (iDrop, iFix, iProj, iMaps))
    conn.commit()

    # unq indexes: UNIQUE where the data allows.
    for sTable in dSchema:
        iDupes = cur.execute(
            'SELECT count(*) FROM (SELECT unq FROM "%s" GROUP BY unq '
            'HAVING count(*) > 1)' % sTable).fetchone()[0]
        sKind = "UNIQUE INDEX" if iDupes == 0 else "INDEX"
        cur.execute('CREATE %s "idx_%s_unq" ON "%s" (unq)' % (sKind, sTable, sTable))
        if iDupes:
            print("NOTE: %s has %d duplicate unq values; plain index" % (sTable, iDupes))
    conn.commit()
    conn.execute("VACUUM")

    for sTable in dSchema:
        print("%-10s %4d rows" % (sTable, cur.execute(
            'select count(*) from "%s"' % sTable).fetchone()[0]))
    conn.close()


# ====================================================================
# Organization enrichment. Promotes broad-sense "projects" -- organi-
# zations, agencies, corporations, nonprofits, committees, and official
# collaborations -- from (a) the agenda's Sponsors / Sponsor Ads section
# and (b) the enterprise affiliations already parsed into contacts.
# Each gets a kind plus as much descrip / url / notes / tags as the
# agenda states or context reasonably implies, and 'affiliated_with'
# (person -> organization) and 'part_of' (sub-unit -> parent) maps.
# ====================================================================

c_sNfb = "National Federation of the Blind"

# Authoritative roster of the NFB's national divisions (from
# nfb.org/about-us/divisions-committees-and-groups/divisions). Lets the
# enrichment recognize genuine divisions whose names lack the literal
# words "Division", "Committee", or "National Federation of the Blind"
# (e.g. the National Association of Blind Lawyers) as part_of the NFB,
# and lets a sub-unit be filed under its own division -- its lowest-
# level parent -- rather than jumping straight to the NFB.
c_lNfbDivisions = [
    ("Assistive Technology Trainers Division", []),
    ("DeafBlind Division", []),
    ("Diabetes Action Network", ["DAN"]),
    ("Human Services Division", []),
    ("National Association of Blind Government Employees", ["NABGE"]),
    ("National Association of Blind Lawyers", ["NABL"]),
    ("National Association of Blind Merchants", ["NABM"]),
    ("National Association of Blind Rehabilitation Professionals", []),
    ("National Association of Blind Students", ["NABS"]),
    ("National Association of Blind Veterans", ["NABV"]),
    ("National Association of Guide Dog Users", ["NAGDU"]),
    ("National Federation of the Blind in Computer Science", ["NFBCS"]),
    ("National Organization of Blind Black Leaders", ["NOBBL"]),
    ("National Organization of Blind Educators", ["NOBE"]),
    ("National Organization of Parents of Blind Children", ["NOPBC"]),
    ("National Organization of Professionals in Blindness Education", ["PIBE"]),
    ("Performing Arts Division", ["PAD"]),
    ("Science and Engineering Division", []),
    ("Seniors Division", []),
    ("Sports and Recreation Division", []),
]

c_lDomainTags = [
    (r"guide dog|seeing eye|leader dog", "guide dogs"),
    (r"robotaxi|rideshare|\brides?\b|transit|transportation|\btnc\b|waymo|uber|lyft|zoox|tesla|uzurv", "transportation"),
    (r"braille", "braille"),
    (r"magnifier|low vision|video magnif", "low vision"),
    (r"\bai\b|artificial intelligence|voice assistant|smart glasses", "AI"),
    (r"voting|ballot|election", "voting"),
    (r"attorney|\blaw\b|\bllp\b|legal|goldstein", "legal"),
    (r"\bstem\b|laboratory|\bscience\b", "STEM"),
    (r"employ|hiring|career|workforce", "employment"),
    (r"receipt|point-of-sale|payment", "fintech"),
    (r"medication|pharmaceutic|wellness|\bhealth\b", "health"),
    (r"education|learning|curriculum|mcgraw|school", "education"),
    (r"refurbished|computers for the blind|digital divide", "technology access"),
]

c_setStateNames = set(s.lower() for s in (
    "Alabama Alaska Arizona Arkansas California Colorado Connecticut Delaware "
    "Florida Georgia Hawaii Idaho Illinois Indiana Iowa Kansas Kentucky Louisiana "
    "Maine Maryland Massachusetts Michigan Minnesota Mississippi Missouri Montana "
    "Nebraska Nevada Ohio Oklahoma Oregon Pennsylvania Tennessee Texas Utah Vermont "
    "Virginia Washington Wisconsin Wyoming").split())

def sNormKey(sName):
    """Loose match key: lowercase, strip punctuation and corporate
    suffixes/stopwords, collapse whitespace. Lets 'Brown, Goldstein &
    Levy, LLP' (roster) meet 'Brown, Goldstein, & Levy' (enterprise)."""
    s = (sName or "").lower().replace("&", " and ")
    s = re.sub(r"[.,/\\\u2019']", " ", s)
    s = re.sub(r"\b(the|inc|incorporated|llp|llc|lp|corp|corporation|"
               r"ltd|co|company|a|of|for|and|division|department)\b", " ", s)
    s = re.sub(r"\s+", " ", s).strip()
    return s

# normKey -> canonical division name; uppercase acronym -> canonical name.
c_dNfbDivByKey = {sNormKey(s): s for s, _ in c_lNfbDivisions}
c_dNfbDivByAcr = {a.upper(): s for s, la in c_lNfbDivisions for a in la}

def sNfbDivisionExact(sName):
    """Return the canonical NFB division name when sName *is* that division
    -- exactly, or with a trailing ', a Division of ...' tail; else None."""
    k = sNormKey(sName)
    if not k: return None
    if k in c_dNfbDivByKey: return c_dNfbDivByKey[k]
    for sKey, sCanon in c_dNfbDivByKey.items():
        if k.startswith(sKey + " "): return sCanon
    return None

def sParentDivisionFor(sName):
    """Return the canonical division a sub-unit belongs to, detected by a
    division acronym inside a committee/seminar/planning name (never the
    federation acronym NFB); None when sName is not a sub-unit or names no
    division. Files e.g. 'NAGDU Seminar Planning Committee' under NAGDU."""
    n = sName or ""
    if not re.search(r"\b(committee|subcommittee|seminar|planning|task\s*force|caucus|group)\b",
                     n, re.I):
        return None
    for sAcr, sCanon in c_dNfbDivByAcr.items():
        if re.search(r"\b" + re.escape(sAcr) + r"\b", n) and sNormKey(sCanon) != sNormKey(n):
            return sCanon
    return None

# Enterprise fragments that are job titles or stray tokens, not real
# organizations; kept out of the projects table. The 'Role, Org' case
# (e.g. 'Archivist, National Federation of the Blind') is split earlier,
# in cleanContactEnterprises, so the org survives and only the role drops.
c_setWeakOrgKey = set(sNormKey(s) for s in (
    "Copilot", "VML", "Concept Engineering", "Registered Nurse", "Archivist",
    "Director", "Coordinator", "Manager", "Specialist", "Consultant"))

def bWeakOrg(sName):
    """True when sName is a parser artifact rather than an organization."""
    return sNormKey(sName) in c_setWeakOrgKey

def addMap(cur, sTbl1, sUnq1, sKind, sTbl2, sUnq2, sNotes=None):
    """Insert one maps row in canonical order: the alphabetically-earlier
    table is always side 1. This is a stored constraint -- a relationship
    is recorded once, never also as a reverse-kind duplicate, and a lookup
    need match only one side. Same-table maps (e.g. project part_of
    project) are exempt from the swap and keep their given direction, which
    the kind encodes (side 1 = child, side 2 = parent). A swap is never
    needed when a cross-table kind is defined with the alpha-earlier table
    as its subject; if one is needed it is announced so the kind's wording
    can be reviewed rather than silently inverted."""
    if sTbl1 != sTbl2 and sTbl1 > sTbl2:
        print("NOTE: maps row reordered to alpha table order: "
              "(%s,%s) %s (%s,%s)" % (sTbl1, sUnq1, sKind, sTbl2, sUnq2))
        sTbl1, sUnq1, sTbl2, sUnq2 = sTbl2, sUnq2, sTbl1, sUnq1
    cur.execute("INSERT INTO maps (tbl1, unq1, kind, tbl2, unq2, notes) "
                "VALUES (?,?,?,?,?,?)", (sTbl1, sUnq1, sKind, sTbl2, sUnq2, sNotes))

def dExtractInfo(sBody):
    """Pull a primary url, an email, and a phone out of ad-copy text."""
    sUrl = None
    for m in re.finditer(r"\((https?://[^)\s]+)\)", sBody):
        sCand = m.group(1)
        if "mailto:" in sCand or "file:" in sCand: continue
        sUrl = sCand; break
    if not sUrl:
        m = re.search(r"\b((?:www\.)?[A-Za-z0-9-]+\.(?:org|com|app|ai|io|gov|net|edu)"
                      r"(?:/[A-Za-z0-9._/?=-]*)?)\b", sBody)
        if m:
            sUrl = m.group(1)
            if not sUrl.lower().startswith("http"): sUrl = "https://" + sUrl
    sEmail = None
    m = re.search(r"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", sBody)
    if m: sEmail = m.group(0)
    sPhone = None
    m = re.search(r"(?:1[-\s]?)?(?:\(\d{3}\)\s?|\d{3}[-.\s])\d{3}[-.\s]\d{4}"
                  r"|\b\d{3}-[A-Z0-9]{3}-[A-Z0-9]{4}\b", sBody)
    if m: sPhone = m.group(0).strip()
    return dict(url=sUrl, email=sEmail, phone=sPhone)

def dParseSponsors(lLines):
    """Return normKey -> dict(name, tier, descrip, url, email, phone)
    built from the tier roster and the per-sponsor ad blocks."""
    iHdr = next((i for i, l in enumerate(lLines)
                 if l.strip().startswith("## Sponsors and Sponsor Ads")), None)
    iAds = next((i for i, l in enumerate(lLines) if l.strip() == "## Sponsor Ads"), None)
    if iHdr is None or iAds is None: return {}
    iEnd = next((i for i in range(iAds + 1, len(lLines))
                 if lLines[i].strip().startswith("## ") and "Sponsor" not in lLines[i]),
                len(lLines))
    dOut = {}
    sTier = None
    for l in lLines[iHdr:iAds]:
        t = l.strip()
        m = re.match(r"^###\s+(.+?):?\s*$", t)
        if m and not t.startswith("####"):
            sTier = m.group(1).strip(); continue
        if not t or t.startswith("#"): continue
        sName = t.lstrip("-").strip()
        if not sName: continue
        k = sNormKey(sName)
        if k and k not in dOut:
            dOut[k] = dict(name=sName, tier=sTier, descrip=None, url=None, email=None, phone=None)
    lState = [None, []]   # [current name, buffer]
    def flush():
        sCur, lBuf = lState
        if not sCur: return
        sRaw = " ".join(x.strip() for x in lBuf if x.strip())
        sBody = sRaw
        for sCut in ("Images include", "Image includes", "Image description", "Image include"):
            iCut = sBody.find(sCut)
            if iCut > 0: sBody = sBody[:iCut].strip()
        d = dExtractInfo(sBody)
        k = sNormKey(re.sub(r"\(.*?\)", "", sCur))
        mPar = re.search(r"\(([^)]+)\)", sCur)
        kPar = sNormKey(mPar.group(1)) if mPar else None
        kHit = k if k in dOut else (kPar if (kPar and kPar in dOut) else None)
        if not kHit:
            for kk in dOut:
                if kk and (kk in k or k in kk or (kPar and (kk in kPar or kPar in kk))):
                    kHit = kk; break
        if not kHit:
            dOut[k] = dict(name=sCur, tier=None, descrip=None, url=None, email=None, phone=None)
            kHit = k
        if sRaw: dOut[kHit]["ad"] = sRaw
        if sBody: dOut[kHit]["descrip"] = sBody
        for sF in ("url", "email", "phone"):
            if d.get(sF) and not dOut[kHit].get(sF): dOut[kHit][sF] = d[sF]
    for l in lLines[iAds:iEnd]:
        t = l.strip()
        if t.startswith("#### "):
            flush(); lState[0] = t[5:].strip().rstrip(":"); lState[1] = []
        elif t.startswith("#"):
            continue
        else:
            lState[1].append(l)
    flush()
    return dOut

def applySponsorAds(cur, dSpon):
    """Append each sponsor's full ad text to that sponsor's contact record,
    under a 'Sponsor Ad:' header on its own line. Existing notes are kept
    (the ad is appended, never overwritten) and any ad already present is
    not added again. In this schema an organization is a contact, so a
    sponsor that so far exists only as a project gets an organization
    contact created here to hold its ad."""
    def sStrip(sKey):
        return re.sub(r"(inc|llc|llp|ltd|co|corp|corporation|company)$",
                      "", sNormKey(sKey or ""))
    dOrg = {}   # stripped key -> (contact_id, notes); people excluded
    for (iId, sEnt, sNotes) in cur.execute(
            "SELECT contact_id, enterprise, notes FROM contacts "
            "WHERE (last_name IS NULL OR last_name='') "
            "AND enterprise IS NOT NULL AND enterprise<>''"):
        dOrg.setdefault(sStrip(sEnt), (iId, sNotes))
    iAdd = iUpd = 0
    for k, dS in dSpon.items():
        sAd = dS.get("ad")
        sName = dS.get("name") or ""
        if not sAd or not sName: continue
        sBlock = "Sponsor Ad:\n" + sAd
        sKey = sStrip(sName)
        tHit = dOrg.get(sKey)
        if tHit:
            iId, sNotes = tHit
            sNotes = sNotes or ""
            if sBlock in sNotes: continue
            sNew = (sNotes.rstrip() + "\n\n" + sBlock) if sNotes.strip() else sBlock
            cur.execute("UPDATE contacts SET notes=? WHERE contact_id=?", (sNew, iId))
            dOrg[sKey] = (iId, sNew)
            iUpd += 1
        else:
            cur.execute("INSERT INTO contacts (enterprise, notes) VALUES (?,?)",
                        (sName, sBlock))
            dOrg[sKey] = (cur.lastrowid, sBlock)
            iAdd += 1
    if iAdd or iUpd:
        print("Sponsor ads: %d appended to existing contacts, "
              "%d new sponsor contacts created." % (iUpd, iAdd))


def sClassifyOrg(sName):
    """Best-guess kind for a broad-sense organization project."""
    n = (sName or "").lower()
    if sNfbDivisionExact(sName): return "division"   # authoritative NFB divisions
    if "committee" in n: return "committee"
    if "division" in n: return "division"
    if re.search(r"\bllp\b|goldstein|jackson walker|& levy", n): return "lawfirm"
    if re.search(r"comptroller|tax commission|library service|department of|\bstate of\b", n):
        return "agency"
    if n.strip() in ("oklahoma", "utah"): return "agency"
    if re.search(r"foundation|federation|association|society|institute|national industries|"
                 r"printing house|seeing eye|leader dog|american foundation|saavi|envision|"
                 r"computers for the blind|inspiration", n):
        return "nonprofit"
    if re.search(r"\binc\b|\bcorp\b|\bllc\b|technolog|pharmaceutic|networks?|communications|"
                 r"interface|robotaxi|\bai\b|systems?\b", n):
        return "corporation"
    return "organization"

def cleanContactEnterprises(cur):
    """Tighten person contacts' enterprise values before organizations are
    mined from them: split 'Role, Org' credits into job + enterprise,
    demote bare job titles to the job field, and drop known non-org
    artifacts. Operates only on people (rows with a last_name), so the
    organization-as-contact rows are left untouched. Returns the count of
    rows changed."""
    sOrgPat = (r"National Federation of the Blind|Federation|Association|Foundation|"
               r"Institute|Industries|Services|University|College|Company|Center|"
               r"Inc\.?|LLC|LLP|Corporation|Department|Division|Library|Commission|"
               r"Technologies|Systems|Networks|Solutions|Walker|Goldstein")
    setDrop = set(sNormKey(s) for s in ("Copilot", "VML", "Concept Engineering"))
    setJobTitle = set(sNormKey(s) for s in (
        "Registered Nurse", "Archivist", "Director", "Coordinator", "Manager",
        "Specialist", "Consultant", "Administrator", "Analyst"))
    lFix = []
    for (sUnq, sEnt, sJob) in cur.execute(
            "SELECT unq, enterprise, coalesce(job,'') FROM contacts "
            "WHERE coalesce(last_name,'')<>'' AND coalesce(enterprise,'')<>''").fetchall():
        sE = (sEnt or "").strip(); sJ = (sJob or "").strip(); k = sNormKey(sE)
        m = re.match(r"^(.{2,40}?),\s*(.+)$", sE)
        if m and re.search(sOrgPat, m.group(2)) and not re.search(sOrgPat, m.group(1)):
            lFix.append((sUnq, m.group(2).strip(), sJ or m.group(1).strip())); continue
        if k in setDrop:
            lFix.append((sUnq, None, sJ)); continue
        if k in setJobTitle:
            lFix.append((sUnq, None, sJ or sE)); continue
    for (sUnq, sNewEnt, sNewJob) in lFix:
        cur.execute("UPDATE contacts SET enterprise=?, job=? WHERE unq=?",
                    (sNewEnt, (sNewJob or None), sUnq))
    return len(lFix)

def cleanArtifacts(cur):
    """Remove parser artifacts and consolidate split records so the database
    reads as clean, real-world data. Three jobs:

      1. Delete non-person 'contacts' the presenter parser minted from agenda
         phrases -- a product ('Seeing AI'), a program ('Achieving Access'),
         a place ('New York'), a support desk ('Disability Answer Desk'), the
         'Managing Partner' fragment from the law-firm name split -- along
         with every maps row that referenced them.
      2. Repair real people whose enterprise/job the parser garbled (most from
         'Brown, Goldstein & Levy, LLP' being split with 'Brown' read as a
         first name, and company tails truncated to bare 'Inc').
      3. Merge the duplicate 'Goldstein & Levy' project into the full firm
         name, and drop the spurious 'Accessibility Innovation' project (a
         Meta session/team label, not an endeavor), repointing or removing
         their maps and de-duplicating the result.

    Returns (dropped, fixed, projects, mapsRemoved) for the build log."""
    # 1. Non-person contacts, by unq (the presenter parser's mistaken names).
    c_lDropUnqs = (
        "Seeing||AI", "Achieving||Access", "Oklahoma||City", "Salt|Lake|City",
        "Blindness|Narrative|Curator", "Disability|Answer|Desk", "Print||Disabled",
        "TRE|Legal|Practice", "Impact||Producer", "Commercial||Support",
        "New||York", "Managing||Partner",
    )
    # 2. Real people to repair: (unq, new_enterprise_or_None, new_job_or_None).
    c_lFixContacts = (
        ("Jessica||Weber", "Brown, Goldstein & Levy, LLP", None),
        ("Xiaoran||Wang", None, "CEO"),
        ("Fan||Zhang", None, "Principal Engineer"),
    )
    # 3. Projects: (old_unq, new_unq_or_None). None means drop outright;
    #    otherwise repoint the old project's maps onto new_unq, then drop it.
    c_lProjectMerges = (
        ("Goldstein & Levy", "Brown, Goldstein & Levy, LLP"),
        ("Accessibility Innovation", None),
    )
    iMaps = 0
    for sUnq in c_lDropUnqs:
        cur.execute("DELETE FROM maps WHERE (tbl1='contacts' AND unq1=?) "
                    "OR (tbl2='contacts' AND unq2=?)", (sUnq, sUnq))
        iMaps += cur.rowcount
        cur.execute("DELETE FROM contacts WHERE unq=?", (sUnq,))
    for (sUnq, sEnt, sJob) in c_lFixContacts:
        cur.execute("UPDATE contacts SET enterprise=?, job=? WHERE unq=?",
                    (sEnt, sJob, sUnq))
    for (sOld, sNew) in c_lProjectMerges:
        if sNew is None:
            cur.execute("DELETE FROM maps WHERE (tbl1='projects' AND unq1=?) "
                        "OR (tbl2='projects' AND unq2=?)", (sOld, sOld))
            iMaps += cur.rowcount
        else:
            cur.execute("UPDATE maps SET unq1=? WHERE tbl1='projects' AND unq1=?",
                        (sNew, sOld))
            cur.execute("UPDATE maps SET unq2=? WHERE tbl2='projects' AND unq2=?",
                        (sNew, sOld))
        cur.execute("DELETE FROM projects WHERE unq=?", (sOld,))
    # A repoint can create a twin map (same tbl1|unq1|kind|tbl2|unq2, which is
    # exactly maps.unq); keep the earliest rowid of each.
    cur.execute("DELETE FROM maps WHERE rowid NOT IN ("
                "SELECT min(rowid) FROM maps GROUP BY tbl1, unq1, kind, tbl2, unq2)")
    iMaps += cur.rowcount
    return (len(c_lDropUnqs), len(c_lFixContacts), len(c_lProjectMerges), iMaps)

def enrichOrganizations(cur, lLines):
    dSpon = dParseSponsors(lLines)
    dExisting = {}
    for (sName, sKind) in cur.execute("SELECT name, kind FROM projects").fetchall():
        dExisting[sNormKey(sName)] = sName
    dAffil = {}        # normKey(enterprise) -> set(person 'First Last')
    dEntDisplay = {}   # normKey -> a display enterprise name
    lPersonMap = []    # (contact unq, enterprise, normKey)
    for (sUnq, sFirst, sLast, sEnt) in cur.execute(
            "SELECT unq, first_name, last_name, enterprise FROM contacts "
            "WHERE coalesce(enterprise,'')<>''").fetchall():
        k = sNormKey(sEnt)
        dEntDisplay.setdefault(k, sEnt)
        if sLast:
            sLook = " ".join(x for x in (sFirst, sLast) if x).strip()
            dAffil.setdefault(k, set()).add(sLook)
            lPersonMap.append((sUnq, sEnt, k))
    dProjByKey = {}
    for k in (set(dSpon) | set(dEntDisplay)):
        if not k: continue
        dS = dSpon.get(k, {})
        sName = dExisting.get(k) or dS.get("name") or dEntDisplay.get(k)
        if not sName: continue
        if sName.strip().lower() in c_setStateNames: continue  # parser artifact, not an org
        if bWeakOrg(sName): continue  # job title or stray token, not an organization
        sTier = dS.get("tier")
        sKind = sClassifyOrg(sName)
        if sKind == "organization" and sTier:
            sKind = "corporation"   # a paying sponsor that is not a
                                    # nonprofit/agency/division is a company
        lTags = [sKind]
        if sTier:
            lTags += [sTier.lower() + " sponsor", "sponsor"]
        sBlob = (sName + " " + (dS.get("descrip") or "")).lower()
        for sPat, sTag in c_lDomainTags:
            if re.search(sPat, sBlob): lTags.append(sTag)
        if sKind in ("division", "committee"): lTags.append("nfb " + sKind)
        if k in dEntDisplay: lTags.append("convention affiliation")
        lTags = list(dict.fromkeys(lTags))
        lNote = []
        if sTier: lNote.append("Sponsor of the 2026 NFB National Convention -- %s tier." % sTier)
        if dS.get("phone"): lNote.append("Phone: " + dS["phone"])
        if dS.get("email"): lNote.append("Email: " + dS["email"])
        if sKind in ("division", "committee"):
            lNote.append("A %s within the National Federation of the Blind." % sKind)
        setAff = dAffil.get(k) or set()
        if setAff:
            lNote.append("Convention participants affiliated with this organization: "
                         + ", ".join(sorted(setAff)) + ".")
        sNotes = "\n".join(lNote) or None
        sDescrip = dS.get("descrip") or None
        sUrl = dS.get("url") or None
        sTags = "\n".join(lTags) or None
        if k in dExisting:
            cur.execute("UPDATE projects SET descrip=coalesce(?,descrip), "
                        "url=coalesce(?,url), notes=coalesce(?,notes), tags=? WHERE name=?",
                        (sDescrip, sUrl, sNotes, sTags, dExisting[k]))
            dProjByKey[k] = dExisting[k]
        else:
            cur.execute("INSERT INTO projects (name, kind, descrip, url, notes, tags) "
                        "VALUES (?,?,?,?,?,?)", (sName, sKind, sDescrip, sUrl, sNotes, sTags))
            dExisting[k] = sName
            dProjByKey[k] = sName
    setSeen = set()
    for (sUnq, sEnt, k) in lPersonMap:
        sProj = dProjByKey.get(k)
        if not sProj: continue
        tKey = ("contacts", sUnq, "affiliated_with", "projects", sProj)
        if tKey in setSeen: continue
        setSeen.add(tKey)
        addMap(cur, *tKey)
    sNfbProj = dProjByKey.get(sNormKey(c_sNfb))
    if sNfbProj:
        def ensureDivisionProject(sCanon):
            """Return the project name for a canonical division, creating
            the row (kind=division, part_of NFB) the first time it is
            needed as a sub-unit's parent."""
            kC = sNormKey(sCanon)
            if kC in dProjByKey: return dProjByKey[kC]
            cur.execute("INSERT INTO projects (name, kind, notes, tags) VALUES (?,?,?,?)",
                        (sCanon, "division",
                         "A division of the National Federation of the Blind.",
                         "division\nnfb division"))
            dProjByKey[kC] = sCanon
            addMap(cur, "projects", sCanon, "part_of", "projects", sNfbProj)
            return sCanon
        for k, sProj in list(dProjByKey.items()):
            if sProj == sNfbProj: continue
            # Sub-unit of a specific division -> file under that division
            # (the lowest-level parent), which is itself part_of the NFB.
            sParent = sParentDivisionFor(sProj)
            if sParent and sNormKey(sParent) != sNormKey(sProj):
                sParentProj = ensureDivisionProject(sParent)
                addMap(cur, "projects", sProj, "part_of", "projects", sParentProj)
                continue
            # Otherwise, a national division/committee or anything naming
            # the federation is directly part_of the NFB.
            nlow = sProj.lower()
            if ("national federation of the blind" in nlow
                    or sClassifyOrg(sProj) in ("division", "committee")
                    or sNfbDivisionExact(sProj)):
                addMap(cur, "projects", sProj, "part_of", "projects", sNfbProj)
    lLk = [
        ("NFB26", "maps", "kind", "affiliated_with", 6,
         "Subject contact is affiliated with (works for, belongs to, or represents) "
         "the object organization."),
        ("NFB26", "maps", "kind", "part_of", 7,
         "Subject organization is a division, committee, or sub-unit of the object organization."),
        ("NFB26", "projects", "kind", "organization", 5,
         "A broad-sense organization or official collaborative effort."),
        ("NFB26", "projects", "kind", "nonprofit", 6,
         "A nonprofit organization, foundation, or association."),
        ("NFB26", "projects", "kind", "corporation", 7, "A for-profit company or corporation."),
        ("NFB26", "projects", "kind", "agency", 8, "A government agency or public body."),
        ("NFB26", "projects", "kind", "committee", 9,
         "A committee or planning body, often within a larger organization."),
        ("NFB26", "projects", "kind", "division", 10,
         "A division or chapter of a larger organization."),
        ("NFB26", "projects", "kind", "lawfirm", 11, "A law firm or legal-services organization."),
    ]
    for t in lLk:
        cur.execute("INSERT OR IGNORE INTO lookups (src,tbl,fld,val,ordinal,descrip) "
                    "VALUES (?,?,?,?,?,?)", t)

    # Curated project URLs, applied only where the agenda supplied none so
    # any agenda-provided link always wins. Division and program links come
    # from the authoritative nfb.org divisions page and site navigation;
    # brand links are the entities' official sites, used only where the
    # project name unambiguously identifies that single entity (so the
    # association is beyond reasonable doubt). Organizations with no public
    # website (e.g. Human Services Division, Sports and Recreation Division)
    # and generic convention activities are deliberately left blank.
    dProjectUrls = {
        "National Association of Blind Lawyers": "http://www.blindlawyers.net/",
        "National Association of Blind Veterans, a Division of the National Federation of the Blind":
            "http://nabv.org/",
        "National Association of Guide Dog Users": "https://www.nagdu.org/",
        "National Organization of Parents of Blind Children Division": "http://nopbc.org/",
        "National Federation of the Blind Performing Arts Division": "https://nfb-pad.org/",
        "NFB-NEWSLINE": "https://nfb.org/programs-services/nfb-newsline",
        "National Federation of the Blind": "https://nfb.org",
        "National Library Service for the Blind": "https://www.loc.gov/nls",
        "Monarch": "https://www.aph.org/product/monarch/",
        "Dot Pad": "https://dotincorp.com/en",
        "Aira": "https://aira.io",
        "American Foundation for the Blind": "https://www.afb.org",
        "National Industries for the Blind": "https://www.nib.org",
        "HumanWare": "https://www.humanware.com",
        "Vispero": "https://www.vispero.com",
        "Google": "https://www.google.com",
        "Uber": "https://www.uber.com",
        "Lyft": "https://www.lyft.com",
        "Tesla": "https://www.tesla.com",
        "Zoox": "https://zoox.com",
        "CVS Health": "https://www.cvshealth.com",
        # Official pages verified against each maker's own site.
        "BrailleNote Evolve": "https://www.humanware.com/braillenote-evolve/",
        "VictorReader Stream":
            "https://store.humanware.com/hus/victor-reader-stream-handheld-media-player.html",
        "JAWS": "https://www.freedomscientific.com/products/software/jaws/",
        "Saavi Services for the Blind": "https://saavi.us/",
        "A. T. Guys": "https://atguys.com/",
        "National Blindness Professional Certification Board": "https://nbpcb.org/",
        "Storm Interface": "https://www.storm-interface.com/",
        # Well-established official domains/product pages.
        "ChatGPT": "https://chatgpt.com",
        "Claude": "https://claude.ai",
        "Gemini": "https://gemini.google.com",
        "Seeing AI": "https://www.seeingai.com",
        "Microsoft 365 Copilot": "https://www.microsoft.com/microsoft-365/copilot",
        "Nemonic Dot Printer": "https://dotincorp.com/en",
        "BrailleSense 7": "https://www.selvasblv.com",
        "VitalSource": "https://www.vitalsource.com",
        "Texas Comptroller of Public Accounts": "https://comptroller.texas.gov",
        "Amazon Worldwide Stores Accessibility": "https://www.amazon.com/accessibility",
        "Ray-Ban Meta": "https://www.meta.com/ai-glasses/",
        # Mis-split of Brown, Goldstein & Levy, LLP (the parser took "Brown"
        # as a first name); same firm, same official site.
        "Goldstein & Levy": "https://browngold.com",
    }
    for sName, sUrl in dProjectUrls.items():
        cur.execute("UPDATE projects SET url=? WHERE name=? AND (url IS NULL OR url='')",
                    (sUrl, sName))

    applySponsorAds(cur, dSpon)

    # Official contact data for sponsor contacts, taken from each
    # organization's own website. Keyed by the contact's enterprise; only
    # blank fields are filled, so nothing already present is overwritten.
    # Source: vandapharma.com/about/locations (corporate headquarters).
    dSponsorContactData = {
        "Vanda": dict(
            office_phone="202-734-3400", url="https://www.vandapharma.com",
            address1="2200 Pennsylvania Ave NW", address2="Suite 300E",
            city="Washington", state="DC", zip="20037", nation="United States"),
    }
    for sEnt, dFields in dSponsorContactData.items():
        for sCol, sVal in dFields.items():
            cur.execute("UPDATE contacts SET %s=? WHERE enterprise=? "
                        "AND (last_name IS NULL OR last_name='') "
                        "AND (%s IS NULL OR %s='')" % (sCol, sCol, sCol),
                        (sVal, sEnt))


if __name__ == "__main__":
    if len(sys.argv) < 2:
        raise SystemExit("usage: build_convention.py <agenda.docx|.txt> [out.db]")
    buildDatabase(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else c_sDefaultOutput)
