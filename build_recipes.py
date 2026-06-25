import sqlite3, os
exec(open('build.py').read())

path='/mnt/user-data/outputs/recipes.db'
if os.path.exists(path): os.remove(path)
c=sqlite3.connect(path)
infra(c)

create_table(c,'recipes','recipe',
  [('name','TEXTLINE'),('summary','TEXTMARKDOWN'),('steps','TEXTMARKDOWN'),
   ('course','TEXTLINE'),('cuisine','TEXTLINE'),('prep_minutes','INTEGER'),
   ('cook_minutes','INTEGER'),('servings','INTEGER'),('image','TEXTLINE')],
  keyfields=['name'])
create_table(c,'ingredients','ingredient',
  [('name','TEXTLINE'),('category','TEXTLINE')],
  keyfields=['name'])

ingredients=[
 ("All-purpose flour","Baking"),("Granulated sugar","Baking"),("Brown sugar","Baking"),
 ("Eggs","Dairy"),("Butter","Dairy"),("Milk","Dairy"),("Olive oil","Pantry"),
 ("Garlic","Produce"),("Yellow onion","Produce"),("Roma tomato","Produce"),
 ("Carrot","Produce"),("Celery","Produce"),("Chicken breast","Meat"),
 ("Ground beef","Meat"),("Spaghetti","Pantry"),("Arborio rice","Pantry"),
 ("Parmesan cheese","Dairy"),("Fresh basil","Produce"),("Salt","Pantry"),
 ("Black pepper","Pantry"),("Vanilla extract","Baking"),("Baking soda","Baking"),
 ("Semisweet chocolate chips","Baking"),("Vegetable broth","Pantry"),("Lemon","Produce"),
]
for n,cat in ingredients:
    c.execute("INSERT INTO ingredients(name,category) VALUES (?,?)",(n,cat))

# recipes: (name, summary, steps, course, cuisine, prep, cook, servings, image, [(ingredient, qty)...])
recipes=[
 ("Classic Chocolate Chip Cookies","Chewy cookies with crisp edges and pools of chocolate.",
  "1. Cream butter and both sugars.\n2. Beat in eggs and vanilla.\n3. Stir in flour and baking soda, then chocolate chips.\n4. Bake at 375F for 10-12 minutes.",
  "Dessert","American",15,12,24,"images/choc_chip.jpg",
  [("Butter","1 cup"),("Granulated sugar","3/4 cup"),("Brown sugar","3/4 cup"),("Eggs","2 large"),
   ("Vanilla extract","1 tsp"),("All-purpose flour","2 1/4 cups"),("Baking soda","1 tsp"),
   ("Semisweet chocolate chips","2 cups"),("Salt","1 tsp")]),
 ("Spaghetti Bolognese","A hearty slow-simmered meat sauce over pasta.",
  "1. Saute onion, carrot, celery, and garlic in olive oil.\n2. Brown the ground beef.\n3. Add tomatoes and simmer 45 minutes.\n4. Toss with cooked spaghetti and Parmesan.",
  "Main","Italian",20,60,4,"images/bolognese.jpg",
  [("Olive oil","2 tbsp"),("Yellow onion","1"),("Carrot","1"),("Celery","1 stalk"),
   ("Garlic","3 cloves"),("Ground beef","1 lb"),("Roma tomato","6"),("Spaghetti","1 lb"),
   ("Parmesan cheese","1/2 cup"),("Salt","1 tsp"),("Black pepper","1/2 tsp")]),
 ("Lemon Garlic Chicken","Pan-seared chicken in a bright lemon-garlic sauce.",
  "1. Season and sear chicken breasts.\n2. Add garlic and lemon; simmer.\n3. Finish with butter and basil.",
  "Main","American",10,25,4,None,
  [("Chicken breast","4"),("Garlic","4 cloves"),("Lemon","2"),("Butter","3 tbsp"),
   ("Olive oil","2 tbsp"),("Salt","1 tsp"),("Black pepper","1/2 tsp"),("Fresh basil","1/4 cup")]),
 ("Parmesan Risotto","Creamy stovetop risotto finished with Parmesan.",
  "1. Saute onion in butter.\n2. Toast arborio rice.\n3. Add warm broth a ladle at a time, stirring.\n4. Finish with Parmesan.",
  "Main","Italian",10,30,4,None,
  [("Butter","3 tbsp"),("Yellow onion","1"),("Arborio rice","1 1/2 cups"),
   ("Vegetable broth","5 cups"),("Parmesan cheese","3/4 cup"),("Salt","1 tsp")]),
 ("Simple Vanilla Pancakes","Fluffy weekend pancakes from pantry staples.",
  "1. Whisk dry ingredients.\n2. Whisk in milk, egg, and melted butter.\n3. Cook on a griddle until bubbles form, then flip.",
  "Breakfast","American",10,15,4,None,
  [("All-purpose flour","1 1/2 cups"),("Granulated sugar","2 tbsp"),("Baking soda","1 tsp"),
   ("Milk","1 1/4 cups"),("Eggs","1 large"),("Butter","3 tbsp"),("Vanilla extract","1 tsp"),("Salt","1/2 tsp")]),
 ("Tomato Basil Soup","A smooth roasted-tomato soup with fresh basil.",
  "1. Roast tomatoes, onion, and garlic.\n2. Simmer with broth.\n3. Blend smooth and stir in basil.",
  "Appetizer","American",15,40,4,None,
  [("Roma tomato","10"),("Yellow onion","1"),("Garlic","4 cloves"),("Olive oil","3 tbsp"),
   ("Vegetable broth","3 cups"),("Fresh basil","1/2 cup"),("Salt","1 tsp"),("Black pepper","1/2 tsp")]),
 ("Garlic Butter Spaghetti","A five-ingredient weeknight pasta.",
  "1. Cook spaghetti.\n2. Melt butter with garlic.\n3. Toss pasta with butter, Parmesan, and pepper.",
  "Main","Italian",5,15,2,None,
  [("Spaghetti","1/2 lb"),("Butter","4 tbsp"),("Garlic","4 cloves"),("Parmesan cheese","1/3 cup"),("Black pepper","1/2 tsp")]),
 ("Beef and Vegetable Skillet","A quick one-pan ground-beef dinner.",
  "1. Brown beef with onion and garlic.\n2. Add carrot and celery; cook until tender.\n3. Season and serve.",
  "Main","American",10,20,4,None,
  [("Ground beef","1 lb"),("Yellow onion","1"),("Garlic","2 cloves"),("Carrot","2"),
   ("Celery","2 stalks"),("Salt","1 tsp"),("Black pepper","1/2 tsp")]),
 ("Lemon Olive Oil Cake","A moist, fragrant single-layer cake.",
  "1. Whisk eggs and sugar.\n2. Add olive oil, milk, and lemon.\n3. Fold in flour and baking soda.\n4. Bake at 350F for 35 minutes.",
  "Dessert","Italian",15,35,8,None,
  [("Eggs","3 large"),("Granulated sugar","1 cup"),("Olive oil","3/4 cup"),("Milk","1/2 cup"),
   ("Lemon","2"),("All-purpose flour","1 1/2 cups"),("Baking soda","1 tsp"),("Salt","1/2 tsp")]),
 ("Chicken Noodle Soup","Comforting soup with chicken and vegetables.",
  "1. Simmer chicken in broth.\n2. Add carrot, celery, and onion.\n3. Add spaghetti broken into pieces; cook until tender.",
  "Main","American",15,45,6,None,
  [("Chicken breast","2"),("Vegetable broth","6 cups"),("Carrot","3"),("Celery","3 stalks"),
   ("Yellow onion","1"),("Spaghetti","1/2 lb"),("Salt","1 tsp"),("Black pepper","1/2 tsp")]),
 ("Brown Sugar Banana Bread","A simple quick bread (banana folded into the batter).",
  "1. Cream butter and brown sugar.\n2. Beat in eggs and vanilla.\n3. Fold in flour and baking soda.\n4. Bake at 350F for 55 minutes.",
  "Breakfast","American",15,55,10,None,
  [("Butter","1/2 cup"),("Brown sugar","1 cup"),("Eggs","2 large"),("Vanilla extract","1 tsp"),
   ("All-purpose flour","2 cups"),("Baking soda","1 tsp"),("Salt","1/2 tsp")]),
 ("Basil Parmesan Risotto Cakes","Crisp pan-fried cakes from leftover risotto.",
  "1. Form cold risotto into patties.\n2. Pan-fry in butter until golden.\n3. Top with Parmesan and basil.",
  "Appetizer","Italian",15,15,4,None,
  [("Arborio rice","2 cups"),("Parmesan cheese","1/2 cup"),("Eggs","1 large"),("Butter","3 tbsp"),
   ("Fresh basil","1/4 cup"),("Salt","1/2 tsp")]),
]
for (name,summ,steps,course,cuisine,prep,cook,serv,image,ings) in recipes:
    c.execute("""INSERT INTO recipes(name,summary,steps,course,cuisine,prep_minutes,cook_minutes,servings,image)
                 VALUES (?,?,?,?,?,?,?,?,?)""",(name,summ,steps,course,cuisine,prep,cook,serv,image))
    for ing,qty in ings:
        add_map(c,'recipes',name,'uses','ingredients',ing,note=qty)

add_lookups(c,'DbDo','recipes','course',[(v,v) for v in
  ["Breakfast","Appetizer","Main","Side","Dessert","Beverage"]])
add_lookups(c,'DbDo','recipes','cuisine',[(v,v) for v in
  ["American","Italian","Mexican","French","Indian","Thai","Mediterranean"]])
add_lookups(c,'DbDo','ingredients','category',[(v,v) for v in
  ["Produce","Dairy","Meat","Baking","Pantry","Spice","Frozen"]])

c.commit()
print("recipes:", c.execute("select count(*) from recipes").fetchone()[0])
print("ingredients:", c.execute("select count(*) from ingredients").fetchone()[0])
print("maps (uses):", c.execute("select count(*) from maps").fetchone()[0])
print("lookups:", c.execute("select count(*) from lookups").fetchone()[0])
print("sample map:", c.execute("select unq1,kind,unq2,notes from maps limit 3").fetchall())
c.close()
