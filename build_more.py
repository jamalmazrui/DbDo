import sqlite3, os
exec(open('build.py').read())

def newdb(name):
    p='/mnt/user-data/outputs/%s.db'%name
    if os.path.exists(p): os.remove(p)
    c=sqlite3.connect(p); infra(c); return c,p

# ============================================================ MUSIC
c,_=newdb('music')
create_table(c,'artists','artist',
  [('name','TEXTLINE'),('nationality','TEXTLINE'),('summary','TEXTMARKDOWN')],['name'])
create_table(c,'albums','album',
  [('name','TEXTLINE'),('year','INTEGER'),('genre','TEXTLINE'),('format','TEXTLINE'),
   ('rating','INTEGER'),('summary','TEXTMARKDOWN')],['name'])
artists=[
 ("Miles Davis","American","Jazz trumpeter and bandleader, a central figure in bebop, cool jazz, and fusion."),
 ("Joni Mitchell","Canadian","Singer-songwriter known for confessional lyrics and unconventional tunings."),
 ("Stevie Wonder","American","Singer, songwriter, and multi-instrumentalist; a Motown great."),
 ("Kate Bush","British","Art-pop singer-songwriter known for theatrical, literary songwriting."),
 ("Herbie Hancock","American","Pianist and composer spanning post-bop, funk, and electronic jazz."),
 ("Nina Simone","American","Singer and pianist blending jazz, blues, folk, and classical."),
 ("Radiohead","British","Alternative rock band known for experimental, electronic-tinged albums."),
 ("Aretha Franklin","American","The Queen of Soul."),
 ("Brian Eno","British","Composer and producer, a pioneer of ambient music."),
 ("Esperanza Spalding","American","Bassist, singer, and composer working across jazz and beyond."),
 ("Fleetwood Mac","British-American","Rock band famed for layered harmonies and the album Rumours."),
 ("Sufjan Stevens","American","Indie songwriter known for orchestral folk and electronic works."),
]
for n,nat,s in artists: c.execute("INSERT INTO artists(name,nationality,summary) VALUES (?,?,?)",(n,nat,s))
# (title, year, genre, format, rating, summary, primary_artist, [guest_artists])
albums=[
 ("Kind of Blue",1959,"Jazz","Vinyl",5,"The best-selling jazz album of all time.","Miles Davis",[]),
 ("Blue",1971,"Folk","Vinyl",5,"A landmark confessional singer-songwriter album.","Joni Mitchell",[]),
 ("Songs in the Key of Life",1976,"Soul","CD",5,"A sprawling double-album masterpiece.","Stevie Wonder",[]),
 ("Hounds of Love",1985,"Art Pop","CD",5,"Features the suite The Ninth Wave.","Kate Bush",[]),
 ("Head Hunters",1973,"Jazz-Funk","Vinyl",4,"A jazz-funk crossover landmark.","Herbie Hancock",[]),
 ("I Put a Spell on You",1965,"Jazz","CD",4,"Includes the title track and Feeling Good.","Nina Simone",[]),
 ("OK Computer",1997,"Alternative Rock","CD",5,"An influential turn-of-the-century rock album.","Radiohead",[]),
 ("I Never Loved a Man the Way I Love You",1967,"Soul","Vinyl",5,"Aretha's Atlantic debut.","Aretha Franklin",[]),
 ("Ambient 1: Music for Airports",1978,"Ambient","CD",4,"A founding work of ambient music.","Brian Eno",[]),
 ("Emily's D+Evolution",2016,"Jazz","CD",4,"A genre-crossing rock-jazz project.","Esperanza Spalding",[]),
 ("Rumours",1977,"Rock","Vinyl",5,"One of the best-selling albums ever.","Fleetwood Mac",[]),
 ("Illinois",2005,"Indie Folk","CD",5,"An orchestral concept album about the state of Illinois.","Sufjan Stevens",[]),
 ("In a Silent Way",1969,"Jazz Fusion","Vinyl",4,"An early electric, fusion-leaning Davis album.","Miles Davis",["Herbie Hancock"]),
 ("River: The Joni Letters",2007,"Jazz","CD",4,"Hancock's tribute to Joni Mitchell.","Herbie Hancock",["Joni Mitchell"]),
]
for (t,yr,g,fmt,r,s,art,guests) in albums:
    c.execute("INSERT INTO albums(name,year,genre,format,rating,summary) VALUES (?,?,?,?,?,?)",(t,yr,g,fmt,r,s))
    add_map(c,'artists',art,'recorded','albums',t)
    for guest in guests: add_map(c,'albums',t,'features','artists',guest)
add_lookups(c,'DbDo','albums','format',[(v,v) for v in ["Vinyl","CD","Cassette","Digital","SACD"]])
add_lookups(c,'DbDo','albums','genre',[(v,v) for v in ["Jazz","Soul","Rock","Folk","Indie Folk","Art Pop","Ambient","Classical","Jazz-Funk","Jazz Fusion","Alternative Rock"]])
add_lookups(c,'DbDo','albums','rating',[("1","1 - Poor"),("2","2 - Fair"),("3","3 - Good"),("4","4 - Very good"),("5","5 - Excellent")])
c.commit(); print("music:", c.execute("select count(*) from artists").fetchone()[0],"artists,",c.execute("select count(*) from albums").fetchone()[0],"albums,",c.execute("select count(*) from maps").fetchone()[0],"maps"); c.close()

# ============================================================ MEDIA WATCHLIST
c,_=newdb('media')
create_table(c,'films','film',
  [('title','TEXTLINE'),('year','INTEGER'),('genre','TEXTLINE'),('rating','INTEGER'),
   ('format','TEXTLINE'),('date_watched','TEXTLINE'),('runtime','INTEGER'),('summary','TEXTMARKDOWN')],['title','year'])
create_table(c,'directors','director',
  [('name','TEXTLINE'),('nationality','TEXTLINE'),('summary','TEXTMARKDOWN')],['name'])
directors=[
 ("Bong Joon-ho","South Korean","Director of Parasite and Memories of Murder."),
 ("Greta Gerwig","American","Director of Lady Bird and Little Women."),
 ("Hayao Miyazaki","Japanese","Co-founder of Studio Ghibli."),
 ("Denis Villeneuve","Canadian","Director of Arrival and Dune."),
 ("Agnes Varda","French","A leading figure of the French New Wave."),
 ("Jordan Peele","American","Director of Get Out and Nope."),
 ("Akira Kurosawa","Japanese","Director of Seven Samurai and Rashomon."),
 ("Chloe Zhao","Chinese-American","Director of Nomadland."),
 ("Christopher Nolan","British-American","Director of Inception and Oppenheimer."),
 ("Celine Sciamma","French","Director of Portrait of a Lady on Fire."),
]
for n,nat,s in directors: c.execute("INSERT INTO directors(name,nationality,summary) VALUES (?,?,?)",(n,nat,s))
films=[
 ("Parasite",2019,"Thriller",5,"Streaming","2025-02-14",132,"A poor family schemes their way into a wealthy household.","Bong Joon-ho"),
 ("Little Women",2019,"Drama",4,"Blu-ray","2025-03-01",135,"The March sisters come of age in 19th-century Massachusetts.","Greta Gerwig"),
 ("Spirited Away",2001,"Animation",5,"Streaming","2025-03-20",125,"A girl wanders into a world of spirits.","Hayao Miyazaki"),
 ("Arrival",2016,"Science Fiction",5,"Blu-ray","2025-04-02",116,"A linguist works to communicate with visiting aliens.","Denis Villeneuve"),
 ("Cleo from 5 to 7",1962,"Drama",4,"Streaming","2025-04-18",90,"Two hours in the life of a singer awaiting test results.","Agnes Varda"),
 ("Get Out",2017,"Horror",5,"Streaming","2025-05-05",104,"A man uncovers a disturbing secret on a weekend visit.","Jordan Peele"),
 ("Seven Samurai",1954,"Action",5,"Blu-ray","2025-05-19",207,"Villagers hire seven masterless samurai for protection.","Akira Kurosawa"),
 ("Nomadland",2020,"Drama",4,"Streaming","2025-06-01",107,"A woman travels the American West living in her van.","Chloe Zhao"),
 ("Oppenheimer",2023,"Drama",5,"Digital","2025-06-15",180,"The story of the physicist behind the atomic bomb.","Christopher Nolan"),
 ("Portrait of a Lady on Fire",2019,"Romance",5,"Streaming","2025-06-28",122,"A painter and her subject fall in love in 18th-century France.","Celine Sciamma"),
 ("Dune",2021,"Science Fiction",4,"Blu-ray","2025-07-04",155,"A noble family takes control of a desert planet.","Denis Villeneuve"),
 ("Nope",2022,"Horror",4,"Streaming","2025-07-12",130,"Ranchers witness something uncanny in the sky.","Jordan Peele"),
]
for (t,yr,g,r,fmt,dw,rt,s,director) in films:
    c.execute("INSERT INTO films(title,year,genre,rating,format,date_watched,runtime,summary) VALUES (?,?,?,?,?,?,?,?)",(t,yr,g,r,fmt,dw,rt,s))
    add_map(c,'films',"%s|%d"%(t,yr),'directed_by','directors',director)
add_lookups(c,'DbDo','films','format',[(v,v) for v in ["Streaming","Blu-ray","DVD","Digital","Theater"]])
add_lookups(c,'DbDo','films','genre',[(v,v) for v in ["Drama","Thriller","Horror","Science Fiction","Action","Animation","Romance","Comedy","Documentary"]])
c.commit(); print("media:", c.execute("select count(*) from films").fetchone()[0],"films,",c.execute("select count(*) from directors").fetchone()[0],"directors,",c.execute("select count(*) from maps").fetchone()[0],"maps"); c.close()

# ============================================================ CONTACTS (regex showcase)
c,_=newdb('contacts')
# Mirror DbDo's own contacts shape; unq = person (first|middle|last) else organization name.
def contacts_table(c):
    cols=['  contact_id INTEGER PRIMARY KEY AUTOINCREMENT',
      '  added TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP','  edited TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP']
    for n in ['first_name','middle_name','last_name','enterprise','job','wireless_phone',
              'home_phone','personal_email','business_email','address1','city','state','zip','nation','url']:
        cols.append('  %s TEXTLINE'%n)
    cols.append('  notes TEXTMARKDOWN'); cols.append('  tags TEXTMEMO')
    cols.append("  look TEXT GENERATED ALWAYS AS (rtrim(iif(first_name IS NOT NULL AND length(CAST(first_name AS TEXT))>0, CAST(first_name AS TEXT)||' | ','')||iif(last_name IS NOT NULL AND length(CAST(last_name AS TEXT))>0, CAST(last_name AS TEXT)||' | ','')||iif(enterprise IS NOT NULL AND length(CAST(enterprise AS TEXT))>0, CAST(enterprise AS TEXT)||' | ',''),' | ')) STORED")
    cols.append("  unq TEXT GENERATED ALWAYS AS (iif(last_name IS NOT NULL AND length(last_name)>0, coalesce(first_name,'')||'|'||coalesce(middle_name,'')||'|'||coalesce(last_name,''), coalesce(enterprise,''))) STORED")
    cols.append('  marked INTEGER NOT NULL DEFAULT 0')
    c.execute('CREATE TABLE "contacts" (\n'+",\n".join(cols)+"\n)")
contacts_table(c)
create_table(c,'groups','group',[('name','TEXTLINE'),('summary','TEXTMARKDOWN')],['name'])
people=[
 ("Aisha","R","Okafor","","Software Engineer","206-555-0142","","aisha.okafor@example.com","","123 Pine St","Seattle","WA","98101","USA","",["Work"]),
 ("Daniel","","Reyes","","Teacher","360-555-0188","360-555-0190","danreyes@example.com","","45 Elm Ave","Bellingham","WA","98225","USA","",["Friends"]),
 ("Mei","L","Tan","","Accessibility Specialist","415-555-0117","","mei.tan@example.com","mtan@workmail.com","9 Market St","San Francisco","CA","94103","USA","https://meitan.example.com",["Work"]),
 ("Carlos","","Mendez","","Chef","","512-555-0173","carlos@example.com","","700 Congress Ave","Austin","TX","78701","USA","",["Friends"]),
 ("Priya","","Sharma","","Researcher","617-555-0199","","priya.sharma@example.com","","12 Beacon St","Boston","MA","02108","USA","",["Work"]),
 ("Tom","","Becker","","Musician","","206-555-0155","tomb@example.com","","88 Lake Dr","Seattle","WA","98109","USA","",["Friends"]),
 ("Grace","M","Lee","","Librarian","","","grace.lee@example.com","","5 Oak Ln","Portland","OR","97201","USA","",["Family"]),
 ("Sam","","Whitfield","","Plumber","503-555-0144","","sam.w@example.com","","210 River Rd","Portland","OR","97202","USA","",[]),
 ("Lena","","Novak","","Doctor","","","lena.novak@example.com","lnovak@clinic.example.com","33 Hill St","Denver","CO","80202","USA","",["Family"]),
 ("Omar","","Haddad","","Architect","303-555-0161","","omar.haddad@example.com","","77 Mesa Blvd","Denver","CO","80203","USA","",["Work"]),
]
orgs=[("Bellingham Public Library","Local library","","","","360-555-0100","","","info@bpl.example.org","210 Central Ave","Bellingham","WA","98225","USA","https://bpl.example.org",["Community"])]
for (f,m,l,ent,job,wp,hp,pe,be,a1,city,st,zp,nat,url,grps) in people:
    c.execute("""INSERT INTO contacts(first_name,middle_name,last_name,enterprise,job,wireless_phone,home_phone,personal_email,business_email,address1,city,state,zip,nation,url)
                 VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",(f,m,l,ent,job,wp,hp,pe,be,a1,city,st,zp,nat,url))
    unq="%s|%s|%s"%(f,m,l)
    for g in grps: add_map(c,'contacts',unq,'in_group','groups',g)
for (name,desc,m,l,ent,wp,hp,pe,be,a1,city,st,zp,nat,url,grps) in orgs:
    c.execute("""INSERT INTO contacts(enterprise,job,home_phone,business_email,address1,city,state,zip,nation,url)
                 VALUES (?,?,?,?,?,?,?,?,?,?)""",(name,desc,hp,be,a1,city,st,zp,nat,url))
groups=[("Work","Professional contacts."),("Friends","Personal friends."),("Family","Family members."),("Community","Local organizations and services."),("Emergency","Emergency contacts.")]
for n,s in groups: c.execute("INSERT INTO groups(name,summary) VALUES (?,?)",(n,s))
add_lookups(c,'USPS','contacts','state',[("WA","Washington"),("OR","Oregon"),("CA","California"),("TX","Texas"),("MA","Massachusetts"),("CO","Colorado")])
c.commit(); print("contacts:", c.execute("select count(*) from contacts").fetchone()[0],"contacts,",c.execute("select count(*) from groups").fetchone()[0],"groups,",c.execute("select count(*) from maps").fetchone()[0],"maps"); c.close()

# ============================================================ HOW-TO KNOWLEDGE BASE (preferred names)
c,_=newdb('howtos')
create_table(c,'articles','article',
  [('name','TEXTLINE'),('summary','TEXTMARKDOWN'),('issue','TEXTMARKDOWN'),('impact','TEXTLINE'),
   ('techniques','TEXTMARKDOWN'),('steps','TEXTMARKDOWN'),('details','TEXTMARKDOWN'),
   ('version','TEXTLINE'),('path','TEXTLINE'),('image','TEXTLINE'),('url','TEXTLINE')],['name'])
create_table(c,'categories','category',[('name','TEXTLINE'),('summary','TEXTMARKDOWN')],['name'])
cats=[("Screen Readers","Working with JAWS, NVDA, and VoiceOver."),
      ("Keyboard Access","Hotkeys, focus, and navigation."),
      ("Data Entry","Forms, validation, and editing."),
      ("Import/Export","Moving data in and out."),
      ("Speech","Speech output and verbosity."),
      ("Braille","Refreshable braille and embossing.")]
for n,s in cats: c.execute("INSERT INTO categories(name,summary) VALUES (?,?)",(n,s))
arts=[
 ("Silence the key name on a hotkey","Stop the screen reader announcing a command key by name.",
  "In a list view, JAWS speaks the key name (e.g. 'Backspace') for a command key that has no script bound.",
  "High",
  "Bind the key to a silent pass-through script so the screen reader treats it as handled.",
  "1. Add the key to the .jkm keymap.\n2. Point it at a TypeCurrentScriptKey wrapper.\n3. Recompile the .jss.",
  "Character echo in edit fields is preserved because it follows the text change, not the key script.",
  "1.0","DbDo.jkm","","",["Screen Readers","Keyboard Access"]),
 ("Sort by more than one column","Order records by several fields in priority order.",
  "Single-column sort is not enough for series with author, then series, then volume.",
  "Medium",
  "Use Order Records to pick the columns in precedence order.",
  "1. Press Alt+O.\n2. Add Author, then Series, then Volume.\n3. Press OK.",
  "The sort is saved per table and new records slot into place.",
  "1.0","","","",["Data Entry"]),
 ("Validate a field with a regular expression","Constrain what a field will accept on entry.",
  "Free-text fields admit malformed emails and phone numbers.",
  "Medium",
  "Add a regex for the table.field in the [Validation] section of DbDo.inix.",
  "1. Open DbDo.inix.\n2. Add contacts.personal_email = pattern.\n3. Reopen the table.",
  "The field tip announces 'must match <regex>' and entry is checked on OK.",
  "1.0","DbDo.inix","","",["Data Entry"]),
 ("Open a command prompt in the database folder","Reach a shell where the open database lives.",
  "Switching to a terminal in the right folder takes several steps.",
  "Low",
  "Use the Command Prompt command, which opens cmd.exe in the database's folder.",
  "1. Press Control+Slash.",
  "Companion to Open in Explorer (Alt+Backslash).",
  "1.0","","","",["Keyboard Access"]),
 ("Drill into related records","Move from a record to the rows related to it.",
  "Finding the events at a location, or the books by an author, is tedious by hand.",
  "Medium",
  "Use Enter-Child to open the related view; the picker labels each choice 'via <kind>'.",
  "1. Focus a record.\n2. Press Enter (or Alt+RightArrow).\n3. Pick a relation if more than one.",
  "Backspace (Alt+LeftArrow) returns to the calling window and row; Alt+Home returns to the root.",
  "1.0","","","",["Keyboard Access"]),
 ("Export the current view","Save what you see to another format.",
  "Sharing a filtered, sorted view with others requires a portable file.",
  "Low",
  "Export to CSV, HTML, Word, Excel, Markdown, JSON, SQLite, Access, or dBASE.",
  "1. Choose Export.\n2. Pick a file name and extension.\n3. Confirm.",
  "The export honors the current filter and sort.",
  "1.0","","","",["Import/Export"]),
 ("Hear a field's full contents","Read a long note that doesn't fit the row.",
  "Multiline notes are truncated in the row view.",
  "Low",
  "Use Say Notes to speak the field, or press it twice to open the text in a box.",
  "1. Focus the row.\n2. Press Shift+N.\n3. Press Shift+N again to open the box.",
  "Notes is a standard field present on every table.",
  "1.0","","","",["Speech"]),
 ("Pick a value from a lookup list","Choose from preset values instead of typing.",
  "Typing the same category values repeatedly is error-prone.",
  "Low",
  "Seed the lookups table for table.field; the editor offers the values on F4.",
  "1. Focus the field.\n2. Press F4 (or Alt+DownArrow).\n3. Choose a value.",
  "The field tip notes how many values are available.",
  "1.0","lookups","","",["Data Entry"]),
 ("Back up a database","Keep a safe copy of your data.",
  "A single file can be lost or corrupted.",
  "High",
  "Use Backup Database to write a separate copy under a new name.",
  "1. Choose Backup Database.\n2. Give it a new name.\n3. Save.",
  "Saving over the open database is refused to avoid a copy-onto-itself stall.",
  "1.0","","","",["Import/Export"]),
 ("Read the hotkey summary","Review every command, its key, and description.",
  "Remembering all the hotkeys is hard.",
  "Low",
  "Use Hotkey Summary to list every command with its key and description.",
  "1. Press Alt+Shift+H.",
  "Shared with EdSharp and FileDir, which use the same chord.",
  "1.0","","","",["Keyboard Access"]),
 ("Evaluate an expression","Do quick math or string work without leaving DbDo.",
  "Switching to a calculator breaks the flow.",
  "Low",
  "Use Evaluate Expression, which runs a one-off expression through the script engine.",
  "1. Press Control+Equals.\n2. Type an expression like 2+2*10.\n3. Read the result.",
  "The result is spoken and shown rather than copied to the clipboard.",
  "1.0","","","",["Keyboard Access"]),
 ("Switch the dot prompt to a table","Point the command prompt at a specific table's window.",
  "The dot prompt needs to act on the right table.",
  "Low",
  "Type 'table <name>' to activate or open the window showing that table.",
  "1. Open the dot prompt.\n2. Type: table books.",
  "Opens the table in a new window if none is showing it.",
  "1.0","","","",["Keyboard Access"]),
 ("Position a hidden standard field","Show a standard field like notes where you want it.",
  "Standard fields are hidden by default and appear at the end when shown.",
  "Low",
  "Use Select Columns to choose the display order, including standard fields.",
  "1. Press Alt+S.\n2. Add the fields in the order you want.\n3. Press OK.",
  "The order is saved per table in DbDo.inix.",
  "1.0","","","",["Data Entry","Screen Readers"]),
]
for (name,summ,issue,impact,tech,steps,details,ver,path,image,url,cats_) in arts:
    c.execute("""INSERT INTO articles(name,summary,issue,impact,techniques,steps,details,version,path,image,url)
                 VALUES (?,?,?,?,?,?,?,?,?,?,?)""",(name,summ,issue,impact,tech,steps,details,ver,path,image,url))
    for cat in cats_: add_map(c,'articles',name,'in_category','categories',cat)
# a couple of cross-references between articles (relates_to)
add_map(c,'articles','Validate a field with a regular expression','relates_to','articles','Pick a value from a lookup list')
add_map(c,'articles','Drill into related records','relates_to','articles','Position a hidden standard field')
add_lookups(c,'DbDo','articles','impact',[("Low","Low"),("Medium","Medium"),("High","High"),("Critical","Critical")])
c.commit(); print("howtos:", c.execute("select count(*) from articles").fetchone()[0],"articles,",c.execute("select count(*) from categories").fetchone()[0],"categories,",c.execute("select count(*) from maps").fetchone()[0],"maps"); c.close()
print("DONE")
