"""Normalize DbDo databases to the prm naming: rename every legacy 'unq'
key column to 'prm', and the maps endpoint columns 'unq1'/'unq2' to
'prm1'/'prm2'. SQLite's RENAME COLUMN updates the dependent generated
'look'/'prm' expressions automatically. Indexes named with 'unq' are
recreated with 'prm' names. Idempotent: databases already on prm are
left unchanged. Usage:  python migratePrm.py <folder>  (default: .)"""
import sqlite3, sys, os, glob, re

def migrate_db(path):
    changes = []
    con = sqlite3.connect(path); cur = con.cursor()
    tables = [r[0] for r in cur.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")]
    for t in tables:
        cols = [r[1] for r in cur.execute("PRAGMA table_xinfo('%s')" % t)]
        if t == "maps":
            if "unq1" in cols and "prm1" not in cols:
                cur.execute("ALTER TABLE maps RENAME COLUMN unq1 TO prm1"); changes.append("maps.unq1->prm1")
            if "unq2" in cols and "prm2" not in cols:
                cur.execute("ALTER TABLE maps RENAME COLUMN unq2 TO prm2"); changes.append("maps.unq2->prm2")
            cols = [r[1] for r in cur.execute("PRAGMA table_xinfo('maps')")]
        if "unq" in cols and "prm" not in cols:
            cur.execute('ALTER TABLE "%s" RENAME COLUMN unq TO prm' % t); changes.append("%s.unq->prm" % t)
    con.commit()
    for name, sql in cur.execute(
        "SELECT name, sql FROM sqlite_master WHERE type='index' AND name LIKE '%unq%' AND sql IS NOT NULL").fetchall():
        newname = name.replace("unq", "prm")
        newsql = re.sub(r'(CREATE\s+(?:UNIQUE\s+)?INDEX\s+"?)' + re.escape(name) + r'("?)',
                        lambda m: m.group(1) + newname + m.group(2), sql, count=1, flags=re.I)
        cur.execute('DROP INDEX IF EXISTS "%s"' % name)
        cur.execute(newsql); changes.append("index %s->%s" % (name, newname))
    con.commit(); con.close()
    return changes

def main():
    root = sys.argv[1] if len(sys.argv) > 1 else "."
    dbs = sorted(glob.glob(os.path.join(root, "**", "*.db"), recursive=True))
    if not dbs: print("No .db files under", root); return
    for db in dbs:
        try:
            ch = migrate_db(db)
            print("%-52s %s" % (db, "; ".join(ch) if ch else "already prm"))
        except Exception as e:
            print("%-52s ERROR %s" % (db, e))

if __name__ == "__main__": main()
