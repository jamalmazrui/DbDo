Subject: Re: DBDo error with latest version

Hi Richard,

That speech-history capture was exactly what I needed — thank you. The good news is that the error was not in your file, and it is not the old Access-driver problem either. Import got all the way to driving Excel; it tripped on the very last step.

**What happened**

`0x80010114` ("the requested object does not exist") came from the hidden copy of Excel that Import uses behind the scenes. I was telling Excel to open your workbook before telling it to stay silent, so when something wanted to put up a dialog — most likely a "this file is already open" notice (the workbook was open in your visible Excel) or a Protected View block (the file arrived as an email attachment) — the hidden Excel had no way to show it, and the open quietly produced a dead reference. The next thing Import touched then reported that the object no longer existed.

**What I changed in the next build**

- Import now silences Excel *before* opening the file, so a background prompt can no longer wedge it.
- It now skips a leading title/metadata row. Your Sheet1 starts with "Updated 2026/03/18" above the real column names, and Import will now find the actual header row (Title, Subtitle, Author, and so on) on its own.
- It harmlessly ignores an empty tab like your Sheet2.

**Could you try the new build I am posting:**

1. Replace DbDo with the new build.
2. If Books read RT.xlsx happens to be open in Excel, close it first (not required, but it removes one variable).
3. In DbDo, open the **File** menu and choose **Import** (or **Alt+I**).
4. Pick **Books read RT.xlsx**.
5. It should bring Sheet1 in as a table with your columns, your Notes column landing in DbDo's standard notes field. Have a look and tell me whether the rows and the data look right.

**About the Copy button:** "could not access the clipboard" was Windows refusing the clipboard for a moment — usually because another program was holding it — rather than a DbDo bug. As you found, the error text also sits in the details field, which you can select and copy with Control+C, so that fallback is always there.

Roughly how many rows come through, whether the data looks right, and any error text would all help. Thanks again for the careful report.

Best,
Jamal
