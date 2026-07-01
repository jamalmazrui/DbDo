import sqlite3, shutil, os

CANON = "Samples/media/media.db"          # source of canonical infra DDL
FIELD_TYPES = ["BLOB","BOOLEAN","INTEGER","REAL","TEXT","TEXTLINE","TEXTMARKDOWN","TEXTMEMO","TEXTTIME"]
KINDS = ["related_to","part_of","located_at","member_of","affiliated_with"]

def canon_ddl():
    c=sqlite3.connect(CANON);cur=c.cursor();d={}
    for t in ("lookups","maps","sqlean_define"):
        cur.execute("select sql from sqlite_master where type='table' and name=?",(t,))
        d[t]=cur.fetchone()[0]
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

def migrate_maps_prm_to_unq(path, ddl):
    """NFB2026Convention: rebuild maps from prm1/prm2/prm -> unq1/unq2/unq."""
    c=sqlite3.connect(path);cur=c.cursor()
    cols=[r[1] for r in cur.execute("pragma table_xinfo('maps')").fetchall()]
    if "unq1" in cols: c.close(); return "already unq1/unq2"
    if "prm1" not in cols: c.close(); return "no prm1 (skip)"
    cur.execute("ALTER TABLE maps RENAME TO maps_old")
    cur.execute(ddl["maps"])
    cur.execute("""INSERT INTO maps(map_id,added,edited,tbl1,unq1,kind,tbl2,unq2,notes,tags,marked)
                   SELECT map_id,added,edited,tbl1,prm1,kind,tbl2,prm2,notes,tags,marked FROM maps_old""")
    n=cur.execute("select count(*) from maps").fetchone()[0]
    cur.execute("DROP TABLE maps_old")
    c.commit();c.close();return f"migrated {n} rows prm->unq"

ddl=canon_ddl()
FULL=["Samples/cellar/cellar.db","Samples/chinook/chinook.db","Samples/northwind/northwind.db"]
SQLEAN=["Samples/WindowsTutorials/WindowsTutorials.db","Samples/iOSTutorials/iOSTutorials.db"]
CONV="Samples/NFB2026Convention/NFB2026Convention.db"

for p in FULL+SQLEAN+[CONV]:
    shutil.copy(p,p+".bak")

for p in FULL:    print(f"{os.path.basename(p):22} infra added: {add_infra(p,ddl)}")
for p in SQLEAN:  print(f"{os.path.basename(p):22} added: {add_sqlean(p,ddl)}")
print(f"{os.path.basename(CONV):22} maps: {migrate_maps_prm_to_unq(CONV,ddl)}")
