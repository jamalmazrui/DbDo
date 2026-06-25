import sqlite3, os
exec(open('build.py').read())

path='/mnt/user-data/outputs/reads.db'
if os.path.exists(path): os.remove(path)
c=sqlite3.connect(path)
infra(c)

create_table(c,'books','book',
  [('title','TEXTLINE'),('subtitle','TEXTLINE'),('summary','TEXTMARKDOWN'),
   ('volume','INTEGER'),('rating','INTEGER'),('format','TEXTLINE'),
   ('genre','TEXTLINE'),('date_read','TEXTLINE'),('pages','INTEGER')],
  keyfields=['title'])
create_table(c,'authors','author',
  [('name','TEXTLINE'),('nationality','TEXTLINE'),('summary','TEXTMARKDOWN')],
  keyfields=['name'])
create_table(c,'series','serie',   # singular of series -> serie_id (id field), table stays plural "series"
  [('name','TEXTLINE'),('summary','TEXTMARKDOWN')],
  keyfields=['name'])

# --- authors ---
authors=[
 ("Octavia E. Butler","American","Pioneering science-fiction author known for Kindred and the Patternist and Parable series."),
 ("Ursula K. Le Guin","American","Author of the Earthsea cycle and The Left Hand of Darkness."),
 ("Agatha Christie","British","The best-selling novelist of all time, creator of Hercule Poirot and Miss Marple."),
 ("Toni Morrison","American","Nobel laureate; author of Beloved and Song of Solomon."),
 ("Terry Pratchett","British","Author of the comic fantasy Discworld series."),
 ("N. K. Jemisin","American","First author to win the Hugo for Best Novel three years running, for the Broken Earth trilogy."),
 ("Kazuo Ishiguro","British","Nobel laureate; author of The Remains of the Day and Never Let Me Go."),
 ("Louise Penny","Canadian","Author of the Chief Inspector Gamache mystery series set in Three Pines."),
 ("Andy Weir","American","Author of The Martian and Project Hail Mary."),
 ("Madeline Miller","American","Author of Circe and The Song of Achilles."),
 ("Tana French","Irish","Author of the Dublin Murder Squad crime novels."),
 ("Becky Chambers","American","Author of the Wayfarers series of hopeful science fiction."),
]
for n,nat,bio in authors:
    c.execute("INSERT INTO authors(name,nationality,summary) VALUES (?,?,?)",(n,nat,bio))

# --- series ---
series=[
 ("Earthsea","Ursula K. Le Guin's cycle of fantasy novels set in an archipelago world."),
 ("Discworld","Terry Pratchett's long-running comic fantasy series."),
 ("The Broken Earth","N. K. Jemisin's Hugo-winning trilogy set on a geologically unstable world."),
 ("Chief Inspector Gamache","Louise Penny's mysteries centered on the Quebec village of Three Pines."),
 ("Wayfarers","Becky Chambers's loosely connected hopeful space-opera novels."),
 ("Hercule Poirot","Agatha Christie's mysteries featuring the Belgian detective."),
 ("Parable","Octavia E. Butler's near-future Earthseed novels."),
 ("Dublin Murder Squad","Tana French's interlinked Irish crime novels."),
]
for n,s in series:
    c.execute("INSERT INTO series(name,summary) VALUES (?,?)",(n,s))

# --- books: (title, subtitle, summary, volume, rating, format, genre, date_read, pages, author, series) ---
books=[
 ("A Wizard of Earthsea",None,"A gifted boy trains as a wizard and must face the shadow he unleashed.",1,5,"Audiobook","Fantasy","2025-01-12",215,"Ursula K. Le Guin","Earthsea"),
 ("The Tombs of Atuan",None,"A young priestess of the dark powers meets a wizard seeking a lost talisman.",2,4,"Audiobook","Fantasy","2025-02-03",180,"Ursula K. Le Guin","Earthsea"),
 ("The Fifth Season",None,"In a world of recurring apocalypses, a woman searches for her daughter.",1,5,"Ebook","Science Fiction","2025-02-20",468,"N. K. Jemisin","The Broken Earth"),
 ("The Obelisk Gate",None,"The middle volume of the Broken Earth trilogy.",2,5,"Ebook","Science Fiction","2025-03-15",433,"N. K. Jemisin","The Broken Earth"),
 ("Guards! Guards!",None,"The Ankh-Morpork City Watch confronts a summoned dragon.",8,5,"Paperback","Fantasy","2025-03-30",355,"Terry Pratchett","Discworld"),
 ("Kindred",None,"A modern Black woman is pulled back in time to a pre-Civil-War plantation.",None,5,"Hardcover","Science Fiction","2025-04-11",287,"Octavia E. Butler",None),
 ("Parable of the Sower",None,"A young woman founds a new faith amid societal collapse.",1,4,"Ebook","Science Fiction","2025-04-29",345,"Octavia E. Butler","Parable"),
 ("Still Life",None,"Inspector Gamache investigates a death in the village of Three Pines.",1,4,"Audiobook","Mystery","2025-05-08",312,"Louise Penny","Chief Inspector Gamache"),
 ("Murder on the Orient Express",None,"Poirot solves a murder aboard a snowbound train.",None,5,"Braille","Mystery","2025-05-22",256,"Agatha Christie","Hercule Poirot"),
 ("Project Hail Mary",None,"A lone astronaut wakes with amnesia on a mission to save humanity.",None,5,"Audiobook","Science Fiction","2025-06-01",476,"Andy Weir",None),
 ("Circe",None,"The witch of Greek myth tells her own story.",None,5,"Audiobook","Fantasy","2025-06-14",393,"Madeline Miller",None),
 ("The Long Way to a Small, Angry Planet",None,"The crew of a tunneling ship takes a long job across the galaxy.",1,4,"Ebook","Science Fiction","2025-06-25",441,"Becky Chambers","Wayfarers"),
 ("Beloved",None,"A formerly enslaved woman is haunted by the past.",None,5,"Hardcover","Literary","2025-07-02",324,"Toni Morrison",None),
 ("Never Let Me Go",None,"Students at an unusual English boarding school learn their purpose.",None,4,"Paperback","Literary","2025-07-13",288,"Kazuo Ishiguro",None),
]
for (title,sub,summ,vol,rating,fmt,genre,dr,pages,author,ser) in books:
    c.execute("""INSERT INTO books(title,subtitle,summary,volume,rating,format,genre,date_read,pages)
                 VALUES (?,?,?,?,?,?,?,?,?)""",(title,sub,summ,vol,rating,fmt,genre,dr,pages))
    # maps: book written_by author
    add_map(c,'books',title,'written_by','authors',author)
    if ser:
        add_map(c,'books',title,'in_series','series',ser,
                note=("volume: %d"%vol) if vol else None)

# --- lookups (pick lists + the rating constraint demonstrated via lookups) ---
add_lookups(c,'DbDo','books','format',[(v,v) for v in
  ["Hardcover","Paperback","Ebook","Audiobook","Braille","Large Print"]])
add_lookups(c,'DbDo','books','genre',[(v,v) for v in
  ["Fantasy","Science Fiction","Mystery","Literary","Biography","History","Poetry","Nonfiction"]])
add_lookups(c,'DbDo','books','rating',[("1","1 - Poor"),("2","2 - Fair"),("3","3 - Good"),("4","4 - Very good"),("5","5 - Excellent")])

c.commit()
print("books:", c.execute("select count(*) from books").fetchone()[0])
print("authors:", c.execute("select count(*) from authors").fetchone()[0])
print("series:", c.execute("select count(*) from series").fetchone()[0])
print("maps:", c.execute("select count(*) from maps").fetchone()[0])
print("lookups:", c.execute("select count(*) from lookups").fetchone()[0])
print("sample book look/unq:", c.execute("select look,unq from books limit 2").fetchall())
print("sample map:", c.execute("select tbl1,unq1,kind,tbl2,unq2,notes from maps limit 3").fetchall())
c.close()
