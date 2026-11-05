# Analysis in DbDo: A Plan from the Data-Science Toolkit

*5 November 2026. A proposal; nothing here is built until Jamal says so.*

## How this was weighed

Twenty-nine PDFs on pandas, NumPy, SQLite and Python data analysis were searched for how often each operation is named. The most named: grouping (pandas groupby, 342 mentions), cumulative distributions (289), correlation (242), missing values (203), percentiles (189), histograms (173), scatter plots (136), skew (118), running totals (117), time grouping (116), standard deviation (104), full-text search (104), joins (99), covariance (90) and regression (88).

DbDo's Generate from Grid already covers much of this: count, blanks, smallest, largest, sum, mean, median, the quartiles, range and standard deviation; frequencies with percentages; a cross-tab; and Excel's bar, Pareto, pie, histogram, box, scatter and date charts.

## The low-hanging fruit

*Built on 5 November 2026: the Group report and the Relation report, the running percentage and the takeaway sentences. Still to build: the 10th and 90th percentiles and skew in the Statistics report, and the chi-square test in the Cross-tab report.*

Each fits Generate from Grid, uses SQL or a few lines of C#, and needs no new package.

- **Group report (G), new.** Pandas' groupby and pivot_table, the most-used operation of all: pick a column to group by -- the current one first -- and a numeric column; for each group, its count, sum, mean, median, smallest and largest. A date column groups by year, month or weekday, which covers time grouping.
- **Relation report (R), new.** Correlation, covariance and regression: for the shown numeric columns, how strongly each pair rises and falls together, in plain words ("strong, rising together"); for a chosen pair, the line that best fits it.
- **Statistics report, more.** The 10th and 90th percentiles, skew in plain words ("leans toward high values"), and how many values are outliers by the usual rule (more than one and a half quartile ranges beyond the quartiles), with examples.
- **Frequency report, more.** A running percentage beside each value's percentage: the cumulative distribution, which says, for example, that the top three values cover 80% of records.
- **Cross-tab report, more.** Row and column totals, and a chi-square test saying in plain words whether the two columns seem related or could differ by chance.

The buttons would then be: Statistics report (S), Frequency report (F), Group report (G), Relation report (R), Cross-tab report (C), Markdown table (M), Excel chart (E) -- each a word's first letter.

## Already applied, 5 November 2026

From the Urban Institute's "Do No Harm Guide: Centering Accessibility in Data Visualization":

- **Every report opens with its takeaway.** The Statistics report says the range, the middle and the middle half in one sentence, and how many values lie far outside the rest; the Frequency report names the most common value and how few values cover 80% of records; the Cross-tab report names the largest combination. The details follow.
- **The Frequency report gives a running percentage** beside each value's own -- the cumulative distribution.
- **Every Excel chart carries a description:** its title and kind, its number of data points, and where its numbers are, in the workbook's Comments property and, for a chart on a sheet, its alternative text.

## Worth more, later

- **Charts without Excel,** through ScottPlot (NuGet; .NET Framework 4.6.2 and later): bar, histogram, box, scatter with its best-fit line, and a correlation heat map, saved as pictures, each with a description written from the data -- seaborn's common plots, with no Office.
- **Statistical tests and distributions** through Math.NET Numerics (NuGet; .NET Framework 4.6.1 and later, 4.8 included): t-tests, exact p-values, and fitted distributions, when the plain-language reports call for them.
- **Sonification,** hearing a column as rising and falling tones, which the guide calls early but promising, after the open-source work of Highcharts and Georgia Tech's Sonification Lab.
- **Full-text search** with SQLite's FTS5, already planned in DbDo's Developer guide from Quill Radio.

## Not proposed

A data-frame library such as Microsoft.Data.Analysis or Deedle: DbDo's tables are SQLite, and SQL already groups, joins and filters them, so a second model of the data would add weight without adding ability.
