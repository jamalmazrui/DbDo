import sqlite3, shutil, os, re

CANON = "Samples/media/media.db"          # source of canonical infra DDL
FIELD_TYPES = ["BLOB","BOOLEAN","INTEGER","REAL","TEXT","TEXTLINE","TEXTMARKDOWN","TEXTMEMO","TEXTTIME"]
KINDS = ["related_to","part_of","located_at","member_of","affiliated_with"]

def canon_ddl():
    c=sqlite3.connect(CANON);cur=c.cursor();d={}
    for t in ("lookups","maps","sqlean_define"):
        cur.execute("select sql from sqlite_master where type='table' and name=?",(t,))
        sql=cur.fetchone()[0]
        # Normalize any legacy unq naming to prm so infra copied into
        # other databases is always prm-based (never unq).
        sql=sql.replace("unq1","prm1").replace("unq2","prm2")
        sql=re.sub(r"\bunq\b","prm",sql)
        d[t]=sql
    c.close();return d

def has(cur,name):
    cur.execute("select 1 from sqlite_master where type='table' and name=?",(name,))
    return cur.fetchone() is not None

def add_infra(path, ddl):
    c=sqlite3.connect(path);cur=c.cursor();made=[]
    for t in ("lookups","maps","sqlean_define"):
        if not has(cur,t): cur.execute(ddl[t]); made.append(t)
    if "lookups" in made:
        for i,ft in enumerate(FIELD_TYPES,1):
            cur.execute("insert into lookups(src,tbl,fld,val,ordinal) values('DbDo','*','type',?,?)",(ft,i))
        for i,k in enumerate(KINDS,1):
            cur.execute("insert into lookups(src,tbl,fld,val,ordinal) values('DbDo','maps','kind',?,?)",(k,i))
    c.commit();c.close();return made

def add_sqlean(path, ddl):
    c=sqlite3.connect(path);cur=c.cursor();made=[]
    if not has(cur,"sqlean_define"): cur.execute(ddl["sqlean_define"]); made.append("sqlean_define")
    c.commit();c.close();return made

def migrate_maps_unq_to_prm(path):
    """Rename any legacy maps endpoint columns unq1/unq2 -> prm1/prm2.
    SQLite's RENAME COLUMN rewrites the dependent look/prm generated
    expressions automatically. Idempotent (prm already => no-op)."""
    c=sqlite3.connect(path);cur=c.cursor()
    cols=[r[1] for r in cur.execute("pragma table_xinfo('maps')").fetchall()]
    done=[]
    if "unq1" in cols and "prm1" not in cols:
        cur.execute("ALTER TABLE maps RENAME COLUMN unq1 TO prm1");done.append("unq1->prm1")
    if "unq2" in cols and "prm2" not in cols:
        cur.execute("ALTER TABLE maps RENAME COLUMN unq2 TO prm2");done.append("unq2->prm2")
    c.commit();c.close()
    return ", ".join(done) if done else "already prm1/prm2"

ddl=canon_ddl()
FULL=["Samples/cellar/cellar.db","Samples/chinook/chinook.db","Samples/northwind/northwind.db"]
SQLEAN=["Samples/WindowsTutorials/WindowsTutorials.db","Samples/iOSTutorials/iOSTutorials.db"]
CONV="Samples/NFB2026Convention/NFB2026Convention.db"

for p in FULL+SQLEAN+[CONV]:
    shutil.copy(p,p+".bak")

for p in FULL:    print(f"{os.path.basename(p):22} infra added: {add_infra(p,ddl)}")
for p in SQLEAN:  print(f"{os.path.basename(p):22} added: {add_sqlean(p,ddl)}")
for p in FULL+SQLEAN+[CONV]:
    print(f"{os.path.basename(p):22} maps: {migrate_maps_unq_to_prm(p)}")
