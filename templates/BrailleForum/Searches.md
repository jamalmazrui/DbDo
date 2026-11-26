# Searching the Braille Forum

BrailleForum.db has a full-text index called articles_fts. It knows every word of every article's title, author and words, so a search finds matches at once and ranks them, best first. This page shows the searches it can do. Each one is ready to paste into a query in DbDo and run.

## The search to start from

Every search below has this shape. Only the words between the quotes after MATCH change.

```sql
SELECT a.title, a.author, a.published, snippet(articles_fts, -1, '', '', ' ... ', 24) AS passage
FROM articles_fts JOIN articles a ON a.article_id = articles_fts.rowid
WHERE articles_fts MATCH 'guide dog' AND a.kind = 'article'
ORDER BY bm25(articles_fts, 10.0, 5.0, 1.0, 1.0)
LIMIT 200;
```

What each part does:

- **MATCH** finds the articles that hold the words.
- **a.kind = 'article'** leaves out the masthead, the table of contents and the standing notices every issue repeats. Take it out to search those too.
- **bm25** ranks the matches. The four numbers weigh the title, author, body and text fields: a word in the title counts ten times a word in the article.
- **snippet** gives about 24 words around a match as plain text, so you hear why the article was found.

## The forms a search takes

Each line gives what goes between the quotes after MATCH, then what it finds.

- **Both words, anywhere:** `guide dog` finds articles with guide and dog anywhere in them.
- **A phrase:** `"audio description"` finds the two words together, in that order. Keep the double quotes inside the single ones.
- **Either word:** `paratransit OR rideshare` finds articles with either word.
- **Not a word:** `braille NOT display` finds braille, leaving out articles that also say display.
- **Words near each other:** `NEAR(guide dog, 5)` finds guide and dog within 5 words of each other.
- **A word's start:** `advoca*` finds advocate, advocacy and advocating.
- **One field only:** `title: convention` finds convention in titles only. Use author, title, body or text.
- **A group:** `(taxi OR uber) AND discrimination` combines the forms. AND, OR and NOT are in capitals.

Accents are ignored, so cafe finds café. Case is ignored too.

## Searches worth trying

- Articles by one writer: `author: dickelman`
- The history of a fight: `"pedestrian safety" OR "quiet cars" OR "hybrid vehicles"`
- Technology over the years: `NEAR(screen reader, 3)`
- What members said: `title: letters`
- One state's affiliate: `"Washington Council of the Blind"`

## Narrowing by date

Add a condition on published, which is the year and month, such as 2015-11:

```sql
WHERE articles_fts MATCH 'audio description' AND a.kind = 'article' AND a.published >= '2015'
```

## Reading what you find

The quickest way: in the articles table, press Control+F for Filter Records and choose Full-text match. Type the same words as between the quotes after MATCH, without the single quotes. The matching articles stay in the table, so you can read them, mark them and open them as usual. They keep the table's order; the searches on this page put the best matches first.

From a saved search's list: the search gives each article's title, author and published month. To read one, find it in the articles table by its title with Jump (Control+J), or mark the ones you want and run the reading_list report (Alt+Shift+R). An article that came from a web page has its words in body; one that came from the issue's text file has them in text.

## Does DbDo have full-text search?

Yes. The SQLite inside the driver DbDo opens databases with includes it, and each DbDo build tests it on a copy of this database: a search, Full-text match, every saved search, an edit and the index's own check. If a search ever says "no such module: fts5", that check will have said why in its log, and Keywords (Control+K) still works on any table.
