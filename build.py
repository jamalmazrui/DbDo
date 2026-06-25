import sqlite3, os

def look_expr(fields):
    parts = ["iif({f} IS NOT NULL AND length(CAST({f} AS TEXT))>0, CAST({f} AS TEXT) || ' | ', '')".format(f=f) for f in fields]
    return "rtrim(" + " || ".join(parts) + ", ' | ')"

def unq_expr(fields):
    return "||'|'||".join("coalesce(CAST({f} AS TEXT),'')".format(f=f) for f in fields)

def create_table(c, table, singular, datacols, keyfields, notes_type="TEXTMARKDOWN"):
    # datacols: list of (name, type). Standard skeleton wraps them.
    cols = []
    cols.append("  {s}_id INTEGER PRIMARY KEY AUTOINCREMENT".format(s=singular))
    cols.append("  added TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP")
    cols.append("  edited TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP")
    for n,t in datacols:
        cols.append("  {n} {t}".format(n=n,t=t))
    cols.append("  notes {nt}".format(nt=notes_type))
    cols.append("  tags TEXTMEMO")
    cols.append("  look TEXT GENERATED ALWAYS AS ({e}) STORED".format(e=look_expr(keyfields)))
    cols.append("  unq TEXT GENERATED ALWAYS AS ({e}) STORED".format(e=unq_expr(keyfields)))
    cols.append("  marked INTEGER NOT NULL DEFAULT 0")
    sql = 'CREATE TABLE "{t}" (\n'.format(t=table) + ",\n".join(cols) + "\n)"
    c.execute(sql)

MAPS_SQL = '''CREATE TABLE "maps" (
  map_id INTEGER PRIMARY KEY AUTOINCREMENT,
  added TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  edited TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  tbl1 TEXTLINE, unq1 TEXTLINE, kind TEXTLINE, tbl2 TEXTLINE, unq2 TEXTLINE,
  notes TEXTMARKDOWN, tags TEXTMEMO,
  look TEXT GENERATED ALWAYS AS (rtrim(iif(tbl1 IS NOT NULL AND length(CAST(tbl1 AS TEXT))>0, CAST(tbl1 AS TEXT) || ' | ', '') || iif(unq1 IS NOT NULL AND length(CAST(unq1 AS TEXT))>0, CAST(unq1 AS TEXT) || ' | ', '') || iif(kind IS NOT NULL AND length(CAST(kind AS TEXT))>0, CAST(kind AS TEXT) || ' | ', '') || iif(tbl2 IS NOT NULL AND length(CAST(tbl2 AS TEXT))>0, CAST(tbl2 AS TEXT) || ' | ', '') || iif(unq2 IS NOT NULL AND length(CAST(unq2 AS TEXT))>0, CAST(unq2 AS TEXT) || ' | ', ''), ' | ')) STORED,
  unq TEXT GENERATED ALWAYS AS (coalesce(CAST(tbl1 AS TEXT),'')||'|'||coalesce(CAST(unq1 AS TEXT),'')||'|'||coalesce(CAST(kind AS TEXT),'')||'|'||coalesce(CAST(tbl2 AS TEXT),'')||'|'||coalesce(CAST(unq2 AS TEXT),'')) STORED,
  marked INTEGER NOT NULL DEFAULT 0
)'''

LOOKUPS_SQL = '''CREATE TABLE "lookups" (
  lookup_id INTEGER PRIMARY KEY AUTOINCREMENT,
  added TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  edited TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  src TEXTLINE, tbl TEXTLINE, fld TEXTLINE, val TEXTLINE, ordinal INTEGER,
  descrip TEXTMARKDOWN, url TEXTLINE, notes TEXTMARKDOWN, tags TEXTMEMO,
  look TEXT GENERATED ALWAYS AS (rtrim(iif(src IS NOT NULL AND length(CAST(src AS TEXT))>0, CAST(src AS TEXT) || ' | ', '') || iif(tbl IS NOT NULL AND length(CAST(tbl AS TEXT))>0, CAST(tbl AS TEXT) || ' | ', '') || iif(fld IS NOT NULL AND length(CAST(fld AS TEXT))>0, CAST(fld AS TEXT) || ' | ', '') || iif(val IS NOT NULL AND length(CAST(val AS TEXT))>0, CAST(val AS TEXT) || ' | ', ''), ' | ')) STORED,
  unq TEXT GENERATED ALWAYS AS (coalesce(CAST(src AS TEXT),'')||'|'||coalesce(CAST(tbl AS TEXT),'')||'|'||coalesce(CAST(fld AS TEXT),'')||'|'||coalesce(CAST(val AS TEXT),'')) STORED,
  marked INTEGER NOT NULL DEFAULT 0
)'''

def infra(c):
    c.execute(MAPS_SQL); c.execute(LOOKUPS_SQL)
    c.execute("CREATE TABLE sqlean_define(name text primary key, type text, body text)")

def add_lookups(c, src, tbl, fld, pairs):
    # pairs: list of (val, descrip)
    for i,(val,desc) in enumerate(pairs):
        c.execute("INSERT INTO lookups(src,tbl,fld,val,ordinal,descrip) VALUES (?,?,?,?,?,?)",
                  (src, tbl, fld, val, i, desc))

def add_map(c, t1, u1, kind, t2, u2, note=None):
    c.execute("INSERT INTO maps(tbl1,unq1,kind,tbl2,unq2,notes) VALUES (?,?,?,?,?,?)",
              (t1,u1,kind,t2,u2,note))

print("helpers ready")
