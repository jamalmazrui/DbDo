// CopyRowToClipboard.js
//
// Copy the current row to the clipboard as a key=value listing, one
// field per line. Useful for pasting a quick record summary into an
// email, a chat client, or a doc. Only the visible (non-hidden) fields
// are included; the bookkeeping columns (added, updated, look, unq,
// primary key, foreign keys) are suppressed because the data list
// itself hides them.
//
// Camel Type variable names: lower-camelCase with Hungarian-style
// type prefixes. 'a' = array/collection, 'sb' = StringBuilder (common
// abbreviation), 's' = string. Variable definitions are alphabetical
// on a single line per type; type lines are themselves ordered
// alphabetically (a < s < sb).
//
// Host objects: db (DbDoManager), frm (DbDoForm).

var aFieldNames;
var sName, sText, sValue;
var sb;

if (db.eof || db.bof) {
  frm.invokeMessage("CopyRowToClipboard: no current row");
  "No current row to copy.";
}
else {
  aFieldNames = db.getDisplayFieldNames();
  sb = new System.Text.StringBuilder();

  for each (sName in aFieldNames) {
    sValue = db.getFieldValue(sName);
    sb.AppendLine(sName + " = " + sValue);
  }

  sText = sb.ToString();
  System.Windows.Forms.Clipboard.SetText(sText);
  frm.invokeMessage("CopyRowToClipboard: " + aFieldNames.Count + " fields copied");
  "Copied row " + db.absolutePosition + " (" + aFieldNames.Count + " fields) to clipboard.";
}
