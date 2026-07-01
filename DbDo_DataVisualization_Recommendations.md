# Data Visualization for DbDo: What Each Data Shape Supports, and How to Make It Nonvisually Accessible

## 1. The key idea: chart choice follows *measurement type*, not column count

"Single column versus grid" is the right starting instinct, but the deeper driver is the **measurement type** of each field:

- **Nominal / categorical** (unordered labels): country, genre, status.
- **Ordinal** (ordered labels): small / medium / large, rating 1-5.
- **Quantitative** (numbers you can do arithmetic on): price, count, weight.
- **Temporal** (dates and times): a special, ordered quantitative type.

The number of fields involved (one, two, many) combined with the measurement type of each determines which statistics and which charts are possible. The sections below go shape by shape.

Because DbDo assumes Excel is available through COM automation, every statistic named below exists as an Excel worksheet function and every chart below exists in Excel's charting engine — both reachable over COM. DbDo can therefore *either* compute and render natively *or* delegate to Excel, and the recommendations in section 7 lean on Excel as the chart engine while keeping the accessible text core native.

## 2. One field (univariate output)

### Categorical field
- **Statistics:** frequency count per category, relative frequency (percent), mode, number of distinct values (cardinality), count of missing.
- **Charts:** bar chart (the workhorse), pie or donut (only when there are few categories), Pareto chart.
- **pandas:** `df["genre"].value_counts()`.

### Quantitative field
- **Statistics:** count, missing, mean, median, mode, standard deviation, variance, minimum, maximum, range, quartiles (Q1, Q2 / median, Q3), inter-quartile range, skewness, kurtosis, outlier count. This is essentially `df["price"].describe()` plus a couple of extras.
- **Charts:** histogram (bins the values), box plot (median, quartiles, outliers), density / KDE curve, ECDF, violin, strip or dot plot.

### Temporal field alone
- Usually you count events per period, which turns it into a time series (a bar or line of counts per day, month, year).

## 3. Two fields (bivariate output) — the common "compare X by Y"

### Two quantitative fields
- **Statistics:** Pearson correlation, Spearman rank correlation, covariance, linear-regression slope / intercept / R-squared.
- **Charts:** scatter plot, scatter with a regression line, hexbin or 2-D histogram for many points, line if one axis is ordered.

### One categorical + one quantitative ("a number broken down by a group")
- **Statistics:** mean / median / sum / count of the number within each category.
- **Charts:** grouped or clustered bar (bar of means or sums), box plot per group, violin per group, strip plot.

### Two categorical fields
- **Statistics:** contingency table (cross-tabulation of counts), row and column percentages, chi-square association.
- **Charts:** grouped or stacked bar, heatmap of the crosstab, mosaic plot.
- **pandas:** `pd.crosstab(df["a"], df["b"])`.

### Temporal + quantitative
- The classic time-series line chart. **Statistics:** trend direction, minimum and maximum points, period-over-period change, moving average.

## 4. Many fields (multivariate / the full grid)

- **Several quantitative fields:** correlation matrix rendered as a heatmap (statistics = all pairwise correlations); scatter-plot matrix / pairplot (every pair of fields); parallel coordinates; PCA for dimensionality reduction.
- **Pivot / aggregated grid** (one categorical as rows, another as columns, a number aggregated in the cells): a heatmap is the natural picture, and the pivot table itself is the accessible form. pandas `pivot_table`.
- **Three quantitative fields:** bubble chart (x, y, and size).
- **One quantitative field + geography** (region codes or latitude / longitude): choropleth map.
- **Small multiples:** the same chart repeated once per category.

## 5. When a field must be a particular type (the rules that "facilitate" a chart)

This is the part that matters most for DbDo's engine, because field-type detection gates everything.

| Chart | Requires |
|---|---|
| Bar / pie | a categorical (or binned) axis + a numeric measure (or counts) |
| Histogram | exactly one quantitative field (it bins automatically) |
| Box / violin | quantitative values, optionally split by a categorical |
| Line / time series | an ordered axis (temporal or ordinal) + a quantitative value |
| Scatter / correlation / regression | two quantitative fields |
| Heatmap | a matrix of numbers (a pivot or a correlation matrix) |
| Crosstab / mosaic | two categorical fields |
| Choropleth | a quantitative field + a geographic key |

Mapped to Excel's `XlChartType` enum (the values DbDo would set over COM), these become: bar → `xlColumnClustered` or `xlBarClustered`; pie → `xlPie`; line / time series → `xlLine`; scatter → `xlXYScatter`; histogram → `xlHistogram` (Excel 2016+, or a pre-binned `xlColumnClustered` for older versions); box plot → `xlBoxwhisker` (Excel 2016+); heatmap → there is no native Excel heatmap type, so render a pivot range with conditional-formatting color scales instead. Excel enforces the same type prerequisites DbDo would — for example, `xlXYScatter` expects two numeric ranges — so DbDo's affinity gating and Excel's own requirements line up, and a field DbDo would reject for a scatter is one Excel would mis-plot anyway.

The traps, all of which DbDo can catch with its existing affinity detection:

- **Dates stored as text** will not sort or plot as a timeline until parsed to a real date type.
- **Numbers stored as text** ("1,234", "$5.00") will not compute statistics until coerced to numeric.
- **High-cardinality categoricals** (hundreds of distinct labels) make bar and pie useless, so they need a "top N plus other" rollup or a warning.
- **Mixed or dirty fields** (a numeric field with stray text) silently break aggregation.
- **Mostly-missing fields** cannot support meaningful statistics.

DbDo already classifies field affinity via its `[Validation]` regex store. That same classification is exactly what should drive (a) which charts are offered and (b) which coercions and warnings fire.

## 6. Making all of this nonvisually accessible — what the research says

The central design framework is Lundgard and Satyanarayan's **four-level model of semantic content**: L1 = the chart's construction and encodings, L2 = statistics and relationships, L3 = perceptual trends and phenomena, L4 = domain context and insight. The important finding for DbDo: blind readers find a plain restatement of the visual encoding (L1) much *less* useful than summary statistics (L2) and trends (L3). So DbDo's generated text should lead with statistics and trends, not "this is a bar chart with blue bars."

A second key finding sets the priority. When a screen reader even detects a web chart, blind users spend roughly 211% more time and are about 61% less accurate at extracting information than sighted users. That gap is exactly what a tool like DbDo can close by generating statistics, tables, and trend text directly instead of relying on an image.

The dominant accessible-output approach today is **multimodal**:

- **Text description** (screen-reader speech) — lead with statistics and trends.
- **Data table** — the universal fallback; the WCAG best practice is a short alt text plus a long description that is usually the data table.
- **Sonification** — pitch or tone encodes value; good for conveying shape and trend.
- **Braille** — refreshable braille patterns can convey chart shape.
- **Interactive review / navigation** — step through individual points at a chosen granularity.

A point specific to the Excel route: an Excel chart is a rendered image, and Excel does not caption it or expose its data to a screen reader as anything richer than a selectable object. So even when Excel draws the picture, DbDo must still generate the text description and the data table itself — Excel is the *drawing* engine, not the *accessibility* engine. This is why the accessible core below stays native to DbDo regardless of who renders the chart.

Existing tools DbDo can lean on or learn from:

- **MAIDR / py-maidr** (Python): wraps matplotlib and seaborn and adds sonification, braille, text, and a "review" mode for bar, box, heatmap, scatter, and histogram. It is explicitly a "design for us" bidirectional tool, so sighted and blind users share one artifact.
- **MatplotAlt** (Python): one-line automatic alt-text and data-table generation for matplotlib figures, built directly on the four-level model.
- **BrailleR** (R): programmatic text descriptions of base-R and ggplot2 graphics.
- **Umwelt** (research system): an authoring environment where a screen-reader user builds a multimodal representation from a dataset *without* needing a visual chart first.
- **Chartability**: a heuristic checklist for evaluating whether a visualization is actually accessible.

## 7. Recommended features for DbDo

Prioritized, most value first.

1. **"Describe Field" command (highest value, lowest cost).** From the grid the user selects one or more fields; DbDo reads their affinity and emits a screen-reader-native textual summary as Markdown:
   - one quantitative field → count, missing, mean, median, standard deviation, min / Q1 / median / Q3 / max, skew direction, outlier count, plus a one-line trend if the field is ordered;
   - one categorical field → distinct count, top categories with counts and percentages, and a long-tail note;
   - two fields → correlation (quantitative pair), group means (categorical by quantitative), or a crosstab (two categoricals).
   This is the core deliverable. It is what the blind user actually consumes, it is pure text / Markdown, and it needs no charting library.

2. **Always pair every chart with its underlying aggregated table.** The frequency table behind a bar chart, the bin counts behind a histogram, the crosstab behind a heatmap — exported as Markdown or CSV. This is the universal accessible form and satisfies the short-alt-text-plus-data-table convention.

3. **Smart chart picker driven by affinity.** When the user asks to chart a selection, DbDo offers only the chart types the field types support (per the rules table in section 5), so the user never has to know that "scatter needs two numbers." Each offered chart names what it will show ("Bar chart of counts by genre," "Histogram of price in 20 bins").

4. **Dual artifact for sharing with sighted colleagues, drawn by Excel.** Produce both (a) the accessible text and table for the user and (b) an Excel chart for colleagues. Because Excel is assumed available over COM, DbDo writes the aggregated table to a worksheet, adds a chart, and either keeps it as a live `.xlsx` deliverable or exports it to a PNG via `Chart.Export`. Prefer handing colleagues the **live workbook**, not just an image: a `.xlsx` containing the data table plus the chart is more useful and more accessible than a bare PNG (the colleague gets the underlying numbers and can re-style the chart), and it round-trips back into DbDo. Keep PNG export as a lightweight option for email or documents, and save the generated description as the image's alt text / a sidecar `.md` so the figure travels captioned.

5. **Type guardrails using the existing affinity store.** Before plotting: coerce text-dates to dates and text-numbers to numerics; warn on high-cardinality categoricals and offer a top-N rollup; refuse impossible combinations (scatter of two text fields) with a helpful message that names what is required; flag mostly-missing fields.

6. **Sonification as an optional second modality (stretch goal).** For an ordered or quantitative series, generate a short audio sweep where pitch tracks value — excellent for conveying shape and trend that text can state but not let you *feel*. This can be a self-contained WAV writer in C#, or delegated to py-maidr.

### Implementation architecture (fitting DbDo's constraints)

Since DbDo already assumes Excel via COM automation, Excel itself becomes the natural charting engine, and the architecture simplifies into two layers:

- **The accessible core (text, table, statistics) stays native C#.** It is arithmetic and string building — no charting dependency, fully testable, and it is the part the blind user actually consumes. As section 6 notes, Excel will not caption its own charts, so DbDo must generate the description and data table regardless of who draws the picture. Keeping this native also means the most important output still works when Excel is absent or unreachable.
- **The chart-and-workbook layer uses Excel COM.** DbDo writes the aggregated data to a worksheet, adds a chart with `ChartObjects().Add` (or `Shapes.AddChart2`), sets `Chart.ChartType` from the `XlChartType` enum per the mapping in section 5, and either saves the workbook as a live `.xlsx` deliverable or calls `Chart.Export(sPngPath, "PNG")` for a standalone image. This reuses the assumed Excel dependency — no Python and no extra C# plotting library — and yields polished, editable charts colleagues already know how to use.
- **Statistics can use either engine.** Prefer computing them natively in C# for the accessible summary (testable, and available without Excel). When a workbook is already open you may instead read them from Excel worksheet functions (`AVERAGE`, `MEDIAN`, `STDEV`, `QUARTILE`, `CORREL`, `COUNTIF`). Validate the algorithms in Python / pandas first either way — the `describe()` set, `value_counts()`, `corr()`, and `crosstab()` are the reference implementations to mirror — matching your standing practice of validating against real data before committing to C#.

Illustrative Excel-COM chart export (untested; COM members keep their required casing, locals follow Camel Type):

```csharp
// oWorksheet already holds the aggregated two-column table (label, count) in the source range.
const int c_iChartHeight = 320, c_iChartLeft = 10, c_iChartTop = 10, c_iChartWidth = 480;
const string c_sChartFormat = "PNG", c_sChartTitle = "Records by genre", c_sDataRange = "A1:B13";
Excel.Chart oChart;
Excel.ChartObject oChartObject;
Excel.Range oData;
string sPngPath;
sPngPath = @"C:\DbDo\Output\genreCounts.png";
oData = oWorksheet.Range[c_sDataRange];
oChartObject = oWorksheet.ChartObjects().Add(c_iChartLeft, c_iChartTop, c_iChartWidth, c_iChartHeight);
oChart = oChartObject.Chart;
oChart.SetSourceData(oData);
oChart.ChartType = Excel.XlChartType.xlColumnClustered;
oChart.HasTitle = true;
oChart.ChartTitle.Text = c_sChartTitle;
oChart.Export(sPngPath, c_sChartFormat);
```

**One operational caveat — the bitness wall.** Excel COM automation requires the automating process and Excel to share bitness. This is the same 64-bit / 32-bit boundary that previously broke the COM-based xlsx *import* (a 64-bit DbDo could not drive a 32-bit Office, as on Richard's machine). Charting over COM is subject to the identical constraint, so it must run in a matching-bitness context or through whatever mechanism DbDo already uses for its other Excel COM work, and it should **degrade gracefully** — when the COM call fails, fall back to the native text-and-table summary, which is the output that matters most anyway. The sensible division of labor mirrors what import already taught the project: *read* arbitrary files with the native, COM-free path (robust across bitness), but *render* charts through Excel COM where Excel is present and bitness matches.

A natural first milestone: ship features 1 and 2 (Describe Field plus a paired data table) with native C# and Markdown output. They deliver the most accessibility value, carry no dependencies, fit your delivery format, work without Excel, and lay the groundwork that the Excel-COM chart picker and workbook export build on later.

A note on command naming, following your 2-4 word title-case rule and Field/Record/Table schema vocabulary: "Describe Field" reads well for the textual-summary command, and "Chart Field" or "Visualize Field" for the picker; chord-letter assignment is left to you under the mnemonic rule.

## References

- Lundgard & Satyanarayan, "Accessible Visualization via Natural Language Descriptions: A Four-Level Model of Semantic Content" (IEEE TVCG, 2022): http://vis.csail.mit.edu/pubs/vis-text-model
- MIT, "Rich Screen Reader Experiences for Accessible Data Visualization": https://vis.csail.mit.edu/pubs/rich-screen-reader-vis-experiences/
- MIT News, "Umwelt enables interactive, accessible charts for blind and low-vision users" (2024): https://news.mit.edu/2024/umwelt-enables-interactive-accessible-charts-creation-blind-low-vision-users-0327
- MAIDR (CHI 2024): https://dl.acm.org/doi/10.1145/3613904.3642730
- Py maidr: https://arxiv.org/html/2509.13532
- MatplotAlt (EuroVis 2025): https://arxiv.org/abs/2503.20089 and https://pypi.org/project/matplotalt/
- BrailleR (R package) — referenced in the MAIDR paper above.
- Charts & Diagrams alt-text best practice (short + long description / data table): https://sc.edu/about/offices_and_divisions/digital-accessibility/toolbox/best_practices/alternative_text/charts-diagrams/
