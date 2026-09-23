// Marked-HowTo.js
// Build a personal how-to guide from the CURRENT view of the methods table
// and save it as MyHowTo.md, ready to read, print, or convert to another
// format. With the methods table open, mark the procedures you want to study
// (or filter the table however you like -- for example to one app, or by a
// word in the name), then run this file with Invoke Script. Each method
// becomes a heading followed by its steps, so you end up with just the
// procedures you are learning gathered in one document.
//
// The script reads only the rows in the current view, so your filter and
// sort decide exactly what the guide contains. This is the methods-table
// counterpart of the convention sample's Marked-Schedule.js.

var c_sOut = "MyHowTo.md";

var sb = new System.Text.StringBuilder();
sb.AppendLine("# My How-To Guide");
sb.AppendLine("");

var iCount = 0;
db.moveFirst();
while (!db.eof) {
  var sName = "" + db.getFieldValue("name");
  var sSummary = "" + db.getFieldValue("summary");
  var sSteps = "" + db.getFieldValue("steps");

  sb.AppendLine("## " + sName);
  sb.AppendLine("");
  if (sSummary != "" && sSummary != "null" && sSummary != sName) {
    sb.AppendLine("*" + sSummary + "*");
    sb.AppendLine("");
  }
  if (sSteps != "" && sSteps != "null") {
    sb.AppendLine(sSteps);  // already a Markdown bullet list of steps
    sb.AppendLine("");
  }
  iCount++;
  db.moveNext();
}
System.IO.File.WriteAllText(c_sOut, sb.ToString());

// The final expression is what Invoke Script reports back to you.
"Wrote " + iCount + " method(s) to " + c_sOut + ". Open it to read your how-to guide.";
