// MarkRowsMatchingRegex.js
//
// Walk every row in the current filtered view, test every visible field
// against a regex, and mark each row whose values match. Useful for
// accumulating an ad-hoc selection (a soft search) that you can later
// scope commands to with "filter marked = 1" at the dot prompt.
//
// Edit the c_sPattern constant below to change what you are searching
// for. The match is case-insensitive.
//
// Camel Type conventions: lower-camelCase names with Hungarian-style
// type prefixes. 'a' = array/collection, 'b' = boolean, 'i' = integer,
// 's' = string. Constants share the same prefix scheme with a leading
// 'c_' (so a constant string is c_sName). Variable-definition lines
// are alphabetical by type; within each line, variables are
// alphabetical.
//
// Host objects: db (DbDuoManager), frm (DbDuoForm).

const c_sPattern = "Seattle";   // <-- edit this

var aFieldNames;
var bMatch;
var iMarked, iScanned;
var regex;
var sName, sValue;

if (db.readOnly) {
  frm.invokeMessage("MarkRowsMatchingRegex: read-only");
  "Read-only database -- cannot mark rows.";
}
else if (!db.hasField("marked")) {
  frm.invokeMessage("MarkRowsMatchingRegex: no 'marked' column");
  "Current table has no 'marked' column. This script needs the standard DbDuo metadata column.";
}
else {
  aFieldNames = db.getDisplayFieldNames();
  iMarked = 0;
  iScanned = 0;
  regex = new System.Text.RegularExpressions.Regex(
    c_sPattern,
    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

  frm.invokeMessage("MarkRowsMatchingRegex: scanning " + db.recordCount + " row(s) for /" + c_sPattern + "/");

  db.moveFirst();
  while (!db.eof) {
    iScanned += 1;
    bMatch = false;
    for each (sName in aFieldNames) {
      sValue = db.getFieldValue(sName);
      if (sValue != null && regex.IsMatch(sValue)) {
        bMatch = true;
        break;
      }
    }
    if (bMatch) {
      db.setFieldValue("marked", "1");
      db.update();
      iMarked += 1;
    }
    db.moveNext();
  }

  frm.invokeRefresh();
  frm.invokeMessage("MarkRowsMatchingRegex: marked " + iMarked + " of " + iScanned + " row(s)");
  "Scanned " + iScanned + " rows. Marked " + iMarked + " matching /" + c_sPattern + "/ (case-insensitive).";
}
