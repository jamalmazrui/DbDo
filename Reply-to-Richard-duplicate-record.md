# Reply to Richard — the error when adding a record

Hi Richard,

Thanks for the detailed report on the error you hit adding *Whistler* — the full message and the steps were exactly what I needed.

First, the reassuring part: nothing is broken about adding records, and your database is fine. What you ran into is a duplicate guard. Every table keeps a hidden identity value (the `unq` field) that DbDo builds from a specific few of your columns, and it refuses to store two records with the same identity. Your new book came out with an identity that already existed, so the database declined it. The problem was that DbDo then threw the raw technical error in your face — the ADO provider text and the stack trace — which made an ordinary "this looks like a duplicate" situation look like a malfunction. That was a bug in how the message was presented, not in adding records.

I've fixed how this is handled. In the next build, when an add would duplicate an existing record, you'll get a plain-language message instead of the technical dump. It will:

- tell you, in one sentence, that the record wasn't added because another record already has the same identifying values;
- list the exact fields that make up the identity for that table, with the values you just entered beside each one — so you can see at a glance what matched;
- keep your entry intact and drop you back into the editor when you choose OK, so you only have to change one field and try again (or Cancel to abandon it).

So next time, instead of a wall of red text, you'll see something like which fields count and what you typed, and the path forward is just to adjust one of them.

On *Whistler* specifically: once you're on the new build, try adding it again. The message will name the fields that form the identity for that books table and show your values — my guess is you'll find the title (or title-plus-author) already matches a record that's in there, and from there it'll be obvious how to make the new one distinct. If it still looks wrong once you can see those fields and values, send me that new message and I'll dig in further.

Thanks again — this is exactly the kind of report that makes the rough edges easy to find.

Best,
Jamal
