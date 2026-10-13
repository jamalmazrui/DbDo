// Import-Outlook-Contacts.js -- adds the people in your Microsoft Outlook contacts to this database's contacts table.
//
// Run it with Invoke Script, Control+Shift+I, with this database open. It asks Outlook, through COM, for every contact
// in your default Contacts folder and adds each one that is not here already, so running it again adds only people
// who are new. Nothing already here is changed. Each person it adds is tagged outlook, so a filter on that tag finds
// them all, and Control+Shift+F clears it again. Press F5 afterwards to see them.
//
// The same script sits beside every DbDo template with a contacts table. It follows KeyLine's olContacts.vbs: the
// Outlook object model's default Contacts folder is number 10, and an item of class 40 is a contact rather than a
// distribution list. The fields are DbDo's contact pattern, from DbDialog: Outlook's business phone is the Office Phone,
// a second line of its street address is Address 2, and its title, birthday and gender fill those fields. A copy of a
// database made before those fields existed still imports: only the fields the table has are written.
//
// Camel Type JScript .NET, run by DbDo with db (the open database) and frm (its window) in scope.

const iContactClass = 40, iContactsFolder = 10, iFemale = 1, iMale = 2, iNoDateYear = 4501;
var aColumns, dHas, iAdded, iSeen, iSkipped, lFailures, oContact, oFolder, oItems, oNamespace, oOutlook, sFailure, sResult;

function sqlText(vValue) {
  // A value as a SQL literal: NULL when empty, otherwise quoted with each quote doubled.
  var sValue = (vValue == null) ? "" : String(vValue).replace(/^\s+|\s+$/g, "");
  if (sValue == "") return "NULL";
  return "'" + sValue.replace(/'/g, "''") + "'";
}

function text(vValue) {
  return (vValue == null) ? "" : String(vValue).replace(/^\s+|\s+$/g, "");
}

function email(vValue) {
  // An Exchange account inside an organization can answer with a directory path, not an address; that is not kept.
  var sValue = text(vValue);
  return (sValue.indexOf("@") > 0) ? sValue : "";
}

function tableColumns() {
  // The contacts table's columns, as a lookup of name to true, read once.
  var dColumns = {}, oWriter = new System.IO.StringWriter();
  db.invokeSql("SELECT '|' || name || '|' AS col FROM pragma_table_info('contacts')", oWriter);
  var aFound = oWriter.ToString().match(/\|[a-z0-9_]+\|/g) || [];
  for (var iFound = 0; iFound < aFound.length; iFound++) dColumns[aFound[iFound].replace(/\|/g, "")] = true;
  return dColumns;
}

function birthday(vValue) {
  // Outlook's birthday as yyyy-mm-dd, or "" for its no-date value, a date in the year 4501.
  try {
    var dtValue = new Date(vValue);
    if (isNaN(dtValue.getTime()) || dtValue.getFullYear() >= iNoDateYear) return "";
    var sMonth = String(dtValue.getMonth() + 1), sDay = String(dtValue.getDate());
    return dtValue.getFullYear() + "-" + (sMonth.length < 2 ? "0" : "") + sMonth + "-" + (sDay.length < 2 ? "0" : "") + sDay;
  }
  catch (e) { return ""; }
}

function gender(vValue) {
  if (vValue == iFemale) return "female";
  if (vValue == iMale) return "male";
  return "";
}

// OUTLOOK BY NAME, NOT BY TYPE (13 October 2026). new ActiveXObject reached Outlook through Office's interop assembly,
// and on a computer whose Office type library cannot be loaded -- common after several Office installations -- the
// first call failed: "Unable to cast COM object of type 'Microsoft.Office.Interop.Outlook.ApplicationClass'". Outlook
// is now created from its program id and every call goes by member name through its dispatch interface, which needs no
// type library. Any error names the stage it happened in and keeps .NET's full text, HRESULT included.
var oComType = null, sStage = "";
function comGet(oObject, sName) {
  return oComType.InvokeMember(sName, System.Reflection.BindingFlags.GetProperty, null, oObject, null);
}
function comCall(oObject, sName, vArg) {
  var aArgs = new System.Object[1];
  aArgs[0] = vArg;
  return oComType.InvokeMember(sName, System.Reflection.BindingFlags.InvokeMethod, null, oObject, aArgs);
}

iAdded = 0; iSeen = 0; iSkipped = 0; lFailures = [];
try {
  sStage = "finding Outlook";
  oComType = System.Type.GetTypeFromProgID("Outlook.Application");
  oOutlook = oComType == null ? null : System.Activator.CreateInstance(oComType);
}
catch (e) {
  oOutlook = null;
}

if (oOutlook == null) {
  sResult = "Microsoft Outlook was not found on this computer, so no contacts were added.";
}
else if (!(dHas = tableColumns())["last_name"]) {
  sResult = "This database has no contacts table with names, so no contacts were added.";
}
else {
  try {
    sStage = "opening Outlook's address book";
    oNamespace = comCall(oOutlook, "GetNamespace", "MAPI");
    sStage = "opening the Contacts folder";
    oFolder = comCall(oNamespace, "GetDefaultFolder", iContactsFolder);
    oItems = comGet(oFolder, "Items");
    iItems = comGet(oItems, "Count");
  }
  catch (e) {
    throw new Error("Outlook could not be read while " + sStage + ". " + e.ToString());
  }
  for (var iIndex = 1; iIndex <= iItems; iIndex++) {
    sStage = "reading contact " + iIndex;
    oContact = comCall(oItems, "Item", iIndex);
    if (comGet(oContact, "Class") != iContactClass) continue;
    iSeen++;
    var sCompany = text(comGet(oContact, "CompanyName")), sFirst = text(comGet(oContact, "FirstName")), sLast = text(comGet(oContact, "LastName")), sMiddle = text(comGet(oContact, "MiddleName"));
    if (sFirst == "" && sLast == "" && sCompany == "") { iSkipped++; continue; }
    var aStreet = text(comGet(oContact, "MailingAddressStreet")).split(/\r?\n/);
    var sBusinessPhone = text(comGet(oContact, "BusinessTelephoneNumber"));
    // In the contact pattern's order.
    aColumns = [
      ["title", text(comGet(oContact, "Title"))], ["first_name", sFirst], ["middle_name", sMiddle], ["last_name", sLast],
      ["job", text(comGet(oContact, "JobTitle"))], ["enterprise", sCompany],
      ["address1", aStreet[0]], ["address2", aStreet.slice(1).join(", ")],
      ["city", text(comGet(oContact, "MailingAddressCity"))], ["state", text(comGet(oContact, "MailingAddressState"))],
      ["zip", text(comGet(oContact, "MailingAddressPostalCode"))], ["nation", text(comGet(oContact, "MailingAddressCountry"))],
      ["office_phone", sBusinessPhone], ["home_phone", text(comGet(oContact, "HomeTelephoneNumber"))],
      ["wireless_phone", text(comGet(oContact, "MobileTelephoneNumber"))], ["business_email", email(comGet(oContact, "Email1Address"))],
      ["personal_email", email(comGet(oContact, "Email2Address"))], ["date_of_birth", birthday(comGet(oContact, "Birthday"))],
      ["gender", gender(comGet(oContact, "Gender"))], ["url", text(comGet(oContact, "WebPage"))], ["tags", "outlook"]];
    // A copy made before office_phone existed keeps the business phone, in its notes.
    if (!dHas["office_phone"] && sBusinessPhone != "") aColumns.push(["notes", "Business phone: " + sBusinessPhone]);
    var lNames = [], lValues = [];
    for (var iColumn = 0; iColumn < aColumns.length; iColumn++) {
      if (!dHas[aColumns[iColumn][0]]) continue;
      lNames.push(aColumns[iColumn][0]);
      lValues.push(sqlText(aColumns[iColumn][1]));
    }
    // THE TABLE DECIDES WHO IS ALREADY HERE (13 October 2026). Its generated prime and unique index are the one rule
    // for a contact's identity; this script used to compute the same key itself, and two copies of one rule drift. A
    // contact the table already holds is ignored, and counted as already here.
    try {
      var iRows = db.invokeSql("INSERT OR IGNORE INTO contacts (" + lNames.join(", ") + ") VALUES (" + lValues.join(", ") + ")", null);
      if (iRows > 0) iAdded++; else iSkipped++;
    }
    catch (e) {
      sFailure = (sFirst + " " + sLast).replace(/^\s+|\s+$/g, "") || sCompany;
      lFailures.push(sFailure + ": " + e.message);
    }
  }
  sResult = "Outlook has " + iSeen + (iSeen == 1 ? " contact" : " contacts") + ". "
    + (iAdded == 0 ? "Nobody new was added" : "Added " + iAdded + (iAdded == 1 ? " person" : " people") + ", tagged outlook")
    + "; " + iSkipped + (iSkipped == 1 ? " was" : " were") + " already here or had no name."
    + (iAdded > 0 ? " Press F5 to see them." : "");
  if (lFailures.length > 0) sResult += " " + lFailures.length + " could not be added: " + lFailures.join("; ");
}
sResult;
