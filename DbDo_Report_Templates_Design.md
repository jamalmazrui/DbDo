# DbDo report templates — language specification (v1)

This is the finalized design, settled before coding. It folds in your two
clarifications: **Markdown is the output format**, and **Jinja2 was reviewed**
for ideas. The language stays in the same family as `transfer.inix` and reuses
the `JScriptExpr` engine already in the build.

## Output is Markdown

A report renders to a `.md` file. Markdown is plain text a screen reader can
read directly, and it converts cleanly to HTML, DOCX, or PDF with pandoc or
Word — so one report definition serves every downstream format. DbDo writes the
`.md` and opens it in your editor; conversion is then your choice of tool.
Because the output is Markdown, a template can produce real structure: start
each record's detail with `## $look` and every contact becomes a navigable
heading in the converted HTML or Word document.

## What was taken from Jinja2

Jinja2 is the reference point for "templates with embedded variables and
expressions," so I checked it deliberately. What it confirmed, and what I
adapted or declined:

- **`{{ expression }}` for interpolation** — adopted; it is also the
  Mustache/Handlebars convention, and it matches the `{{ }}` already in the
  proposal.
- **`{# comment #}` inline comments** — adopted. Useful inside a fenced
  multi-line `detail` block, where the `.inix` line-comment characters (`;`,
  `#`) don't apply.
- **Filters, `{{ x | upper }}`** — declined as a separate mechanism. Since
  expressions are full JScript, the filters people reach for are already there:
  `| upper` is `.toUpperCase()`, `| lower` is `.toLowerCase()`,
  `| trim` is `.trim()`, `| default("—")` is `$x || "—"`, `| join(", ")` is
  `.join(", ")`. Keeping one expression language (the same one `transfer.inix`
  uses) is worth more than a second idiom.
- **`{% if %}` / `{% for %}` statements** — declined. Per-record iteration is
  the report's banded structure (DbDo loops the records for you), inline
  conditionals are covered by the suppress-if-blank rule and JScript ternaries
  (`$x ? a : b`), so a second control syntax would add a parser and little
  power. (A future "sub-band" could add related-record loops if wanted.)
- **Whitespace control, `{%- -%}`** — DbDo's line-level rule (below) is the
  simpler, line-oriented equivalent and needs no markup.
- **Undefined-safe access and autoescaping** — a missing field renders as empty
  (never an error), and values are inserted into Markdown literally (you control
  the structure; no surprise escaping).

One deliberate divergence from Jinja2: a *bare* field is `$field`, not
`{{ field }}`. That keeps the `$field` sigil identical to `transfer.inix`, and
the `$field` (plain field) vs `{{ expression }}` (computed) split is itself a
clear, learnable line.

## File and structure

A report lives in an `.inix` file — same family as `transfer.inix`. Each section
is one report, chosen at run time from a picker. Within a section:

- `@table = <name>` — optional; the table the report is meant for. Open that
  table first; the report runs over what's currently open. (A mismatch is
  reported, not silently run against the wrong table.)
- `header` — emitted once at the top.
- `detail` — emitted once per record (usually a fenced multi-line block).
- `separator` — emitted between records: a keyword or literal text.
- `footer` — emitted once at the end.

All four bands are optional, though a report without a `detail` band has little
to say. Keys are lower-case, matching DbDo's identifier convention.

### Separator keywords

`separator` may be literal text or one of:

- `blank` — one blank line between records.
- `rule` — a Markdown thematic break (`---`), i.e. a horizontal rule.
- `page` — a form-feed character, a page break in plain text. (For DOCX/PDF
  page breaks, `rule` is the portable choice, or put a raw pandoc page-break in
  literal `separator` text.)

## Substitution

Inside any band:

- **`$field` / `${field}`** — the current record's value for `field`
  (case-insensitive). The braced form lets a field abut other letters:
  `$last_name` vs `${last_name}s`. This is the same `$` sigil as
  `transfer.inix`.
- **`{{ jscript-expression }}`** — a JScript .NET expression, evaluated by the
  same `JScriptExpr` engine as transfer maps. Inside it, `$field` / `${field}`
  refer to the record's fields exactly as in a transfer expression. This is the
  "larger expressions" path: `{{ $office_phone || $home_phone }}`,
  `{{ $last_name.toUpperCase() }}`, `{{ $price ? "$" + $price : "" }}`.
- **`{# comment #}`** — removed from the output.

In the header and footer bands there is no current record, so `$field` renders
empty there; those bands are for fixed text.

## Rendering rules (line-oriented, mail-merge style)

Each band is rendered line by line. For one source line:

1. Strip `{# ... #}` comments.
2. Evaluate every `{{ ... }}` expression and substitute its result.
3. Substitute every `$field` / `${field}`.
4. **Suppress-if-blank:** if the line *contained* at least one substitution
   token and the result is empty or all-whitespace, the line is dropped. This
   is DbDialog's "a line prints only if non-blank," generalized — an absent
   Address2 or Job leaves no gap.
5. **Normalize data lines:** a line that contained a substitution token is
   trimmed and its interior runs of spaces collapsed to one, so
   `$title $first_name $middle_name $last_name` reads cleanly whichever name
   parts exist.
6. A line with **no** substitution token is emitted verbatim — including a
   deliberately blank line and any Markdown indentation. So literal structure is
   preserved while data lines self-clean.

## Worked example

```
; DbDo report templates. Produce Report -> pick this file -> pick a section.
; Output is Markdown over the current filtered set, in the current sort order.

[contact_addresses]
@table = contact
header = """
# Contact Addresses
"""
detail = """
## {{ ($first_name + " " + $last_name).replace(/\s+/g, " ").trim() }}
$title $first_name $middle_name $last_name
$job
$enterprise
$address1
$address2
$city, $state $zip
$nation
"""
separator = rule

[contact_phone_email]
@table = contact
header = """
# Phone and Email
(work preferred)
"""
detail = """
**$first_name $last_name**
{{ $office_phone || $home_phone }}
{{ $business_email || $personal_email }}
"""
separator = blank
```

For a contact with no middle name, job, second address, or nation, the
`contact_addresses` detail prints a clean heading plus name, enterprise,
street, city line, and nothing where the blank fields were — then a horizontal
rule before the next contact. Converted with pandoc, each contact is an `H2`
you can jump between.

## Consistency with transfer.inix

| Concern | transfer.inix | report .inix |
| --- | --- | --- |
| File and section pick | `.inix`, run-time pick | `.inix`, run-time pick |
| Table directive | `@table` | `@table` |
| Field reference | `$v`, `$field` | `$field`, `${field}` |
| Expressions | JScript via `JScriptExpr` | JScript via `JScriptExpr` (in `{{ }}`) |
| Blank handling | empty source skipped | line suppressed if its data is blank |
| Engine | `JScriptExpr.eval` | same `JScriptExpr.eval` |

## Grouping (single level)

A report may optionally group its records. Adding the directive `@group = <field>` does three things: it sorts the records by that field automatically (so a grouped report is never wrong because the user forgot to sort), and it enables two more bands that fire as the group value changes — `group_header` at the start of each group and `group_footer` at its end. The group bands see the group's field values (so `$state` works) plus the aggregates below.

Grouping is deliberately limited to a single level. Nested grouping is where the conceptual cost of a report language jumps, and one level covers the large majority of real reports; a report with no `@group` is exactly the flat header/detail/footer report described above, with nothing new to learn.

**Aggregates.** In any footer band — the report `footer` or a `group_footer` — a small set of pseudo-fields is available as ordinary `$`-fields, scoped to the whole report or the current group respectively:

- `$count` — the number of records in scope.
- `$sum_<field>`, `$avg_<field>`, `$min_<field>`, `$max_<field>` — for a numeric column. Values that don't parse as numbers (including blanks) are ignored; currency symbols and thousands separators are tolerated.

They are one idea — aggregate fields — not a function language, and they cost nothing in a report that doesn't use them. A grouped example:

```
[contacts_by_state]
@table = contact
@group = state
group_header = """
## $state
"""
detail = """
- $first_name $last_name
"""
group_footer = """
*$count contact(s) in $state.*
"""
footer = """
**$count contacts total.**
"""
```

Parameters and named calculations were considered and deliberately left out: a report already runs over the current filtered, sorted view, so you "parameterise" it simply by filtering before you run, using the database controls you already know.

## Command and scope

A **Produce Report** command (File menu) picks the report `.inix`, picks the
section, renders the current filtered set in the current sort order to a `.md`
file you name, and opens it in your editor. Generation never disturbs your
place in the data (cursor position is saved and restored).
