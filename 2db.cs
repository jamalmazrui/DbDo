// =====================================================================
// 2db.cs -- standalone "to DbDo" importer.
//
// Reads the tables of a source data file (Access .mdb/.accdb, Excel
// .xlsx/.xls/.xlsm/.xlsb, dBASE .dbf, delimited .csv/.tsv/.txt, or
// SQLite .db/.sqlite/.sqlite3) and writes them into a NEW standard DbDo
// SQLite shell -- every table in the standard shape (<singular>_id,
// added, edited, the data fields, notes, tags, look, prm, marked) plus
// the builtin maps and lookups infrastructure. The shell is created
// beside the source with the same root name and a .db extension.
//
// WHY THIS EXISTS AS A SEPARATE PROGRAM (and in two bitnesses):
// DbDo runs 64-bit, but the Microsoft ACE provider that reads Office and
// dBASE files must match the bitness of the installed Office, and only
// one Office bitness is allowed per machine. A 64-bit process cannot use
// a 32-bit ACE provider. So this converter is built BOTH ways --
// 2db32.exe (/platform:x86) and 2db64.exe (/platform:x64) -- and DbDo
// runs whichever matches the installed Office, bridging the boundary
// out-of-process. SQLite (via the ch-werner SQLite ODBC driver) and the
// native paths need no Office, but they ride along here for one uniform
// importer.
//
// EXIT CODES (a contract DbDo relies on to decide whether to retry the
// other bitness):
//   0  success (a shell was written) OR the user declined to replace an
//      existing file (nothing written, but not an error)
//   2  the data provider for this file type is not available in this
//      process's bitness (e.g. 64-bit ACE absent because Office is
//      32-bit) -- DbDo should try the other 2db build
//   3  the source could not be read, or the conversion failed -- do NOT
//      retry the other bitness; the file or the run is the problem
//   4  bad arguments / usage
//
// USAGE:
//   2db64 <source-file> [<dest.db>] [/force] [/quiet]
//     <dest.db>  optional; defaults to <source-dir>\<source-root>.db
//     /force     replace an existing destination without prompting
//                (DbDo passes this after showing its own GUI prompt)
//     /quiet     suppress progress lines on stdout (errors still print)
//
// Build (see buildDbDo.cmd): csc /platform:x86 -> 2db32.exe,
//                            csc /platform:x64 -> 2db64.exe.
// Requires a reference to Microsoft.CSharp.dll for the dynamic COM calls.
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TwoDb
{
    internal static class Program
    {
        // ADO constants (late-bound; no PIA reference needed).
        private const int adSchemaTables   = 20;
        private const int adOpenForwardOnly = 0;
        private const int adLockReadOnly    = 1;
        private const int adCmdText         = 1;

        // The standard field-type vocabulary DbDo seeds into lookups, so
        // an imported shell looks like a DbDo-created one.
        private static readonly string[] c_aFieldTypes = new string[]
        { "BLOB", "BOOLEAN", "INTEGER", "REAL", "TEXT", "TEXTLINE", "TEXTMARKDOWN", "TEXTMEMO", "TEXTTIME" };

        private static bool bQuiet = false;

        private static int Main(string[] args)
        {
            try
            {
                string sSource = null, sDest = null;
                bool bForce = false;
                foreach (string a in args)
                {
                    if (string.IsNullOrEmpty(a)) continue;
                    if (a.StartsWith("/") || a.StartsWith("-"))
                    {
                        string sFlag = a.TrimStart('/', '-').ToLowerInvariant();
                        if (sFlag == "force" || sFlag == "y" || sFlag == "yes") bForce = true;
                        else if (sFlag == "quiet" || sFlag == "q") bQuiet = true;
                        else if (sFlag == "?" || sFlag == "help") { usage(); return 0; }
                        else { err("Unknown option: " + a); usage(); return 4; }
                    }
                    else if (sSource == null) sSource = a;
                    else if (sDest == null) sDest = a;
                    else { err("Too many arguments."); usage(); return 4; }
                }

                if (string.IsNullOrEmpty(sSource)) { usage(); return 4; }
                if (!File.Exists(sSource)) { err("Source file not found: " + sSource); return 3; }

                string sExt = Path.GetExtension(sSource).TrimStart('.').ToLowerInvariant();
                if (!isSupportedExt(sExt))
                {
                    err("Unsupported source type: ." + sExt
                        + ". Supported: .mdb .accdb .xlsx .xls .xlsm .xlsb .dbf .csv .tsv .txt .db .sqlite .sqlite3");
                    return 3;
                }

                // Destination defaults to <source-dir>\<source-root>.db.
                if (string.IsNullOrEmpty(sDest))
                    sDest = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(sSource)),
                        Path.GetFileNameWithoutExtension(sSource) + ".db");
                // Never write over the source itself (e.g. a .db source).
                if (string.Equals(Path.GetFullPath(sDest), Path.GetFullPath(sSource),
                        StringComparison.OrdinalIgnoreCase))
                    sDest = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(sSource)),
                        Path.GetFileNameWithoutExtension(sSource) + "-dbdo.db");

                // Confirm before replacing an existing destination, unless
                // /force (DbDo shows its own accessible prompt and passes
                // /force; the console prompt is for standalone use).
                if (File.Exists(sDest) && !bForce)
                {
                    Console.Write("\"" + sDest + "\" already exists. Replace it? (y/N): ");
                    string sAns = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
                    if (sAns != "y" && sAns != "yes")
                    {
                        info("Cancelled; no file was created.");
                        return 0;
                    }
                }

                return convert(sSource, sExt, sDest);
            }
            catch (Exception ex)
            {
                err("Unexpected failure: " + ex.Message);
                return 3;
            }
        }

        private static bool isSupportedExt(string sExt)
        {
            switch (sExt)
            {
                case "mdb": case "accdb":
                case "xlsx": case "xls": case "xlsm": case "xlsb":
                case "dbf":
                case "csv": case "tsv": case "tab": case "txt":
                case "db": case "sqlite": case "sqlite3":
                    return true;
                default: return false;
            }
        }

        // convert: open the source, build the shell, copy every table,
        // add the infrastructure. Returns the process exit code.
        private static int convert(string sSource, string sExt, string sDest)
        {
            dynamic oSrc = null, oDest = null;
            bool bDestOpen = false, bInTrans = false;
            int iTables = 0, iRows = 0;
            try
            {
                // --- open the source (ACE formats fall back 16.0 -> 12.0) ---
                try { oSrc = openSource(sSource, sExt); }
                catch (ProviderUnavailable pu) { err(pu.Message); return 2; }

                // --- create / open the destination SQLite shell ---
                try { if (File.Exists(sDest)) File.Delete(sDest); }
                catch (Exception exDel) { err("Could not replace destination: " + exDel.Message); return 3; }
                try
                {
                    oDest = newCom("ADODB.Connection");
                    oDest.Open("DRIVER=SQLite3 ODBC Driver;Database=" + sDest + ";");
                    bDestOpen = true;
                }
                catch (Exception exDest)
                {
                    if (isProviderError(exDest))
                    { err("The SQLite ODBC driver is not available in this bitness: " + exDest.Message); return 2; }
                    err("Could not create the destination database: " + exDest.Message);
                    return 3;
                }

                // One transaction for the whole import: far faster, and
                // all-or-nothing on failure.
                oDest.BeginTrans(); bInTrans = true;

                List<string> lUsedTableNames = new List<string>();
                foreach (string sSrcTable in enumerateSourceTables(oSrc))
                {
                    string sNorm = sNormalizeIdentifier(stripSheetSuffix(sSrcTable));
                    if (sNorm == "maps" || sNorm == "lookups") continue; // shell supplies its own

                    // Read the source table's columns and rows.
                    dynamic oRs = newCom("ADODB.Recordset");
                    try
                    {
                        oRs.Open("SELECT * FROM [" + sSrcTable + "]", oSrc,
                            adOpenForwardOnly, adLockReadOnly, adCmdText);
                    }
                    catch (Exception exOpen)
                    {
                        info("Skipping unreadable source table \"" + sSrcTable + "\": " + exOpen.Message);
                        try { releaseCom(oRs); } catch { }
                        continue;
                    }

                    List<string> lSrcCols = new List<string>();
                    int iFieldCount = (int)oRs.Fields.Count;
                    for (int i = 0; i < iFieldCount; i++) lSrcCols.Add((string)oRs.Fields[i].Name);
                    if (lSrcCols.Count == 0) { try { oRs.Close(); } catch { } releaseCom(oRs); continue; }

                    // Table name: normalized, de-collided, non-numeric start.
                    string sTable = sNorm.Length == 0 ? "table_" + (iTables + 1) : sNorm;
                    if (char.IsDigit(sTable[0])) sTable = "t_" + sTable;
                    string sBaseT = sTable; int iDupT = 1;
                    while (lUsedTableNames.Contains(sTable)) { iDupT++; sTable = sBaseT + "_" + iDupT; }
                    lUsedTableNames.Add(sTable);
                    string sSingular = sTable.EndsWith("s") ? sTable.Substring(0, sTable.Length - 1) : sTable;
                    string sPk = sSingular + "_id";

                    // Shape the columns: notes/tags route to the standard
                    // fields; collisions with the key or a standard name get
                    // an "_in" suffix; duplicates get "_2", "_3", ...
                    string[] aReserved = new string[] { "added", "edited", "look", "prm", "marked" };
                    List<string[]> lFieldDefs = new List<string[]>();
                    List<string> lColTarget = new List<string>();  // per source column -> dest column (or null)
                    List<string> lUsedNames = new List<string>();
                    foreach (string sCol in lSrcCols)
                    {
                        string sHdr = sNormalizeIdentifier(sCol);
                        if (sHdr.Length == 0) sHdr = "column_" + (lColTarget.Count + 1);
                        if (char.IsDigit(sHdr[0])) sHdr = "c_" + sHdr;
                        if (sHdr == "notes") { lColTarget.Add("notes"); continue; }
                        if (sHdr == "tags")  { lColTarget.Add("tags");  continue; }
                        if (sHdr == sPk || Array.IndexOf(aReserved, sHdr) >= 0) sHdr = sHdr + "_in";
                        string sBaseH = sHdr; int iDupH = 1;
                        while (lUsedNames.Contains(sHdr)) { iDupH++; sHdr = sBaseH + "_" + iDupH; }
                        lUsedNames.Add(sHdr);
                        lFieldDefs.Add(new string[] { sHdr, "TEXTLINE" });
                        lColTarget.Add(sHdr);
                    }
                    if (lFieldDefs.Count == 0) lFieldDefs.Add(new string[] { "value", "TEXTLINE" });

                    foreach (string sSql in lStandardTableDdl(sTable, lFieldDefs))
                        oDest.Execute(sSql);

                    // Copy the rows. Empty rows are skipped; a row that
                    // would collide on the UNIQUE prm index is skipped with
                    // a note rather than aborting the import.
                    int iTableRows = 0;
                    while (!(bool)oRs.EOF)
                    {
                        List<string> lInsCols = new List<string>();
                        List<string> lVals = new List<string>();
                        bool bAny = false;
                        for (int i = 0; i < lSrcCols.Count; i++)
                        {
                            string sTarget = (i < lColTarget.Count) ? lColTarget[i] : null;
                            if (sTarget == null) continue;
                            string sCell = cellToString(oRs.Fields[i].Value);
                            if (sCell.Length > 0) bAny = true;
                            lInsCols.Add("\"" + sTarget + "\"");
                            lVals.Add("'" + sCell.Replace("'", "''") + "'");
                        }
                        if (bAny && lInsCols.Count > 0)
                        {
                            string sInsert = "INSERT INTO \"" + sTable + "\" ("
                                + string.Join(", ", lInsCols.ToArray()) + ") VALUES ("
                                + string.Join(", ", lVals.ToArray()) + ")";
                            try { oDest.Execute(sInsert); iTableRows++; iRows++; }
                            catch (Exception exRow) { info("  skipped a row in " + sTable + ": " + exRow.Message); }
                        }
                        oRs.MoveNext();
                    }
                    try { oRs.Close(); } catch { }
                    releaseCom(oRs);
                    iTables++;
                    info("Imported table \"" + sTable + "\" (" + iTableRows + " row(s)).");
                }

                if (iTables == 0)
                {
                    if (bInTrans) { try { oDest.RollbackTrans(); } catch { } bInTrans = false; }
                    err("No importable tables were found in " + Path.GetFileName(sSource) + ".");
                    return 3;
                }

                // Builtin infrastructure (maps + lookups + seed vocabulary).
                foreach (string sSql in lInfraDdl())
                    oDest.Execute(sSql);

                oDest.CommitTrans(); bInTrans = false;
                info("Done. " + iTables + " table(s), " + iRows + " row(s) -> " + sDest);
                return 0;
            }
            catch (Exception ex)
            {
                if (bInTrans) { try { oDest.RollbackTrans(); } catch { } }
                err("Conversion failed: " + ex.Message);
                // Remove a half-written shell so a failed run leaves nothing.
                try { if (bDestOpen) { oDest.Close(); bDestOpen = false; } } catch { }
                try { if (File.Exists(sDest)) File.Delete(sDest); } catch { }
                return isProviderError(ex) ? 2 : 3;
            }
            finally
            {
                try { if (bDestOpen) oDest.Close(); } catch { }
                try { if (oDest != null) releaseCom(oDest); } catch { }
                try { if (oSrc != null) { try { oSrc.Close(); } catch { } releaseCom(oSrc); } } catch { }
            }
        }

        // openSource: open an ADO connection to the source. For ACE
        // formats, try provider 16.0 then 12.0; if neither is registered,
        // throw ProviderUnavailable so the caller can exit 2.
        private static dynamic openSource(string sPath, string sExt)
        {
            if (sExt == "db" || sExt == "sqlite" || sExt == "sqlite3")
            {
                try
                {
                    dynamic oConn = newCom("ADODB.Connection");
                    oConn.Open("DRIVER=SQLite3 ODBC Driver;Database=" + sPath + ";");
                    return oConn;
                }
                catch (Exception ex)
                {
                    if (isProviderError(ex))
                        throw new ProviderUnavailable("The SQLite ODBC driver is not available in this bitness: " + ex.Message);
                    throw;
                }
            }

            // ACE-backed formats: try 16.0, then 12.0.
            string[] aProviders = new string[] { "Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0" };
            Exception exLast = null;
            foreach (string sProv in aProviders)
            {
                try
                {
                    dynamic oConn = newCom("ADODB.Connection");
                    oConn.Open(sourceConnString(sPath, sExt, sProv));
                    return oConn;
                }
                catch (Exception ex)
                {
                    exLast = ex;
                    if (!isProviderError(ex)) throw; // a real open error, not a missing provider
                    // else: provider not registered -> try the next one
                }
            }
            throw new ProviderUnavailable(
                "The Microsoft Access Database Engine (ACE) provider is not available in this "
                + (Is64() ? "64" : "32") + "-bit process"
                + (exLast != null ? (": " + exLast.Message) : "")
                + ". Install the matching-bitness Access Database Engine redistributable, "
                + "or let DbDo try the other 2db build.");
        }

        // sourceConnString: the per-extension ADO connection string,
        // matching DbDo's own templates.
        private static string sourceConnString(string sPath, string sExt, string sProvider)
        {
            switch (sExt)
            {
                case "mdb":
                case "accdb":
                    return string.Format("Provider={0};Data Source={1};Persist Security Info=False;", sProvider, sPath);
                case "xlsx":
                case "xls":
                case "xlsm":
                case "xlsb":
                    return string.Format(
                        "Provider={0};Data Source={1};Extended Properties=\"Excel 12.0 Xml;HDR=Yes;IMEX=1;\";",
                        sProvider, sPath);
                case "dbf":
                    return string.Format(
                        "Provider={0};Data Source={1};Extended Properties=dBASE IV;",
                        sProvider, Path.GetDirectoryName(Path.GetFullPath(sPath)));
                case "csv":
                case "tsv":
                case "tab":
                case "txt":
                {
                    string sFmt = (sExt == "tsv" || sExt == "tab") ? "TabDelimited" : "Delimited";
                    return string.Format(
                        "Provider={0};Data Source={1};Extended Properties=\"Text;HDR=Yes;FMT={2};\";",
                        sProvider, Path.GetDirectoryName(Path.GetFullPath(sPath)), sFmt);
                }
                default:
                    throw new InvalidOperationException("Unsupported source type: ." + sExt);
            }
        }

        // enumerateSourceTables: the user tables of the source, via the
        // ADO schema rowset. System/internal tables are skipped; views are
        // included (read like tables). For a delimited-text or dBASE
        // source the "tables" are the files in the folder, so we keep only
        // the one matching the source file's own leaf name.
        private static List<string> enumerateSourceTables(dynamic oConn)
        {
            List<string> lTables = new List<string>();
            dynamic oRs = null;
            try
            {
                oRs = oConn.OpenSchema(adSchemaTables);
                while (!(bool)oRs.EOF)
                {
                    string sName = cellToString(oRs.Fields["TABLE_NAME"].Value);
                    string sType = cellToString(oRs.Fields["TABLE_TYPE"].Value).ToUpperInvariant();
                    if (!string.IsNullOrEmpty(sName)
                        && sType != "SYSTEM TABLE" && sType != "ACCESS TABLE")
                        lTables.Add(sName);
                    oRs.MoveNext();
                }
            }
            finally
            {
                try { if (oRs != null) { oRs.Close(); releaseCom(oRs); } } catch { }
            }
            return lTables;
        }

        // stripSheetSuffix: Excel sheets enumerate as "Sheet1$"; drop the
        // trailing '$' for the DbDo table name (the SELECT still brackets
        // the original).
        private static string stripSheetSuffix(string sName)
        {
            if (!string.IsNullOrEmpty(sName) && sName.EndsWith("$"))
                return sName.Substring(0, sName.Length - 1);
            return sName;
        }

        // cellToString: an ADO field value as a culture-invariant string;
        // NULL becomes "".
        private static string cellToString(object oVal)
        {
            if (oVal == null || oVal is DBNull) return "";
            if (oVal is string) return (string)oVal;
            try { return Convert.ToString(oVal, CultureInfo.InvariantCulture) ?? ""; }
            catch { return oVal.ToString(); }
        }

        // ---- standard shell DDL (identical shape to DbDo, emitting prm) ----

        private static List<string> lStandardTableDdl(string sTable, List<string[]> lFieldDefs)
        {
            string sSingular = sTable.EndsWith("s") ? sTable.Substring(0, sTable.Length - 1) : sTable;
            string sPk = sSingular + "_id";
            List<string> lCols = new List<string>();
            foreach (string[] aF in lFieldDefs) lCols.Add(aF[0]);

            StringBuilder sbLook = new StringBuilder("rtrim(");
            StringBuilder sbPrm = new StringBuilder();
            for (int i = 0; i < lCols.Count; i++)
            {
                string sC = "\"" + lCols[i] + "\"";
                if (i > 0) { sbLook.Append(" || "); sbPrm.Append("||'|'||"); }
                sbLook.Append("iif(" + sC + " IS NOT NULL AND length(CAST(" + sC
                    + " AS TEXT))>0, CAST(" + sC + " AS TEXT) || ' | ', '')");
                sbPrm.Append("coalesce(CAST(" + sC + " AS TEXT),'')");
            }
            sbLook.Append(", ' | ')");
            if (lCols.Count == 0) { sbLook = new StringBuilder("''"); sbPrm = new StringBuilder("''"); }

            StringBuilder sbCreate = new StringBuilder();
            sbCreate.Append("CREATE TABLE \"" + sTable + "\" (");
            sbCreate.Append("\"" + sPk + "\" INTEGER PRIMARY KEY AUTOINCREMENT, ");
            sbCreate.Append("added TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP, ");
            sbCreate.Append("edited TEXTTIME NOT NULL DEFAULT CURRENT_TIMESTAMP, ");
            foreach (string[] aF in lFieldDefs)
                sbCreate.Append("\"" + aF[0] + "\" " + aF[1] + ", ");
            sbCreate.Append("notes TEXTMARKDOWN, tags TEXTMEMO, ");
            sbCreate.Append("look TEXT GENERATED ALWAYS AS (" + sbLook + ") STORED, ");
            sbCreate.Append("prm TEXT GENERATED ALWAYS AS (" + sbPrm + ") STORED, ");
            sbCreate.Append("marked INTEGER NOT NULL DEFAULT 0)");

            List<string> lTrigCols = new List<string>(lCols);
            lTrigCols.Add("notes"); lTrigCols.Add("tags");
            List<string> lOf = new List<string>();
            List<string> lWhen = new List<string>();
            foreach (string sC in lTrigCols)
            {
                lOf.Add("\"" + sC + "\"");
                lWhen.Add("OLD.\"" + sC + "\" IS NOT NEW.\"" + sC + "\"");
            }
            string sTrigger = "CREATE TRIGGER \"trg_" + sTable + "_edited\" AFTER UPDATE OF "
                + string.Join(", ", lOf.ToArray()) + " ON \"" + sTable
                + "\" FOR EACH ROW WHEN " + string.Join(" OR ", lWhen.ToArray())
                + " BEGIN UPDATE \"" + sTable + "\" SET edited = CURRENT_TIMESTAMP WHERE \""
                + sPk + "\" = NEW.\"" + sPk + "\"; END";

            List<string> lDdl = new List<string>();
            lDdl.Add(sbCreate.ToString());
            lDdl.Add(sTrigger);
            lDdl.Add("CREATE UNIQUE INDEX \"idx_" + sTable + "_prm\" ON \"" + sTable + "\" (prm)");
            return lDdl;
        }

        // lInfraDdl: the builtin maps and lookups tables every DbDo
        // database carries, plus the lookups seed vocabulary -- emitting
        // prm / prm1 / prm2 (the modern standard, replacing unq).
        private static List<string> lInfraDdl()
        {
            List<string> lDdl = new List<string>();

            List<string[]> lMapFields = new List<string[]>();
            foreach (string sC in new string[] { "tbl1", "prm1", "kind", "tbl2", "prm2" })
                lMapFields.Add(new string[] { sC, "TEXTLINE" });
            lDdl.AddRange(lStandardTableDdl("maps", lMapFields));
            lDdl.Add("CREATE INDEX idx_maps_side1 ON maps (tbl1, prm1)");
            lDdl.Add("CREATE INDEX idx_maps_side2 ON maps (tbl2, prm2)");

            List<string[]> lLkFields = new List<string[]>();
            lLkFields.Add(new string[] { "src", "TEXTLINE" });
            lLkFields.Add(new string[] { "tbl", "TEXTLINE" });
            lLkFields.Add(new string[] { "fld", "TEXTLINE" });
            lLkFields.Add(new string[] { "val", "TEXTLINE" });
            lLkFields.Add(new string[] { "ordinal", "INTEGER" });
            lLkFields.Add(new string[] { "descrip", "TEXTMEMO" });
            lLkFields.Add(new string[] { "url", "TEXTLINE" });
            lDdl.AddRange(lStandardTableDdl("lookups", lLkFields));

            int iOrd = 0;
            foreach (string sType in c_aFieldTypes)
            {
                iOrd++;
                lDdl.Add("INSERT INTO lookups (src, tbl, fld, val, ordinal) VALUES "
                    + "('DbDo', '*', 'type', '" + sType + "', " + iOrd + ")");
            }
            int iKindOrd = 0;
            foreach (string sKind in new string[] { "related_to", "part_of", "located_at", "member_of", "affiliated_with" })
            {
                iKindOrd++;
                lDdl.Add("INSERT INTO lookups (src, tbl, fld, val, ordinal) VALUES "
                    + "('DbDo', 'maps', 'kind', '" + sKind + "', " + iKindOrd + ")");
            }
            return lDdl;
        }

        // sNormalizeIdentifier: lower_snake_case, dropping anything that is
        // not a letter, digit, or underscore (spaces and hyphens -> '_').
        private static string sNormalizeIdentifier(string sRaw)
        {
            if (sRaw == null) return "";
            StringBuilder sb = new StringBuilder();
            foreach (char ch in sRaw.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
                else if (ch == ' ' || ch == '-') sb.Append('_');
            }
            return sb.ToString();
        }

        // ---- COM + error helpers ----

        private static dynamic newCom(string sProgId)
        {
            Type t = Type.GetTypeFromProgID(sProgId, false);
            if (t == null) throw new ProviderUnavailable("COM component not registered: " + sProgId);
            return Activator.CreateInstance(t);
        }

        private static void releaseCom(object o)
        {
            try { if (o != null && Marshal.IsComObject(o)) Marshal.ReleaseComObject(o); } catch { }
        }

        // isProviderError: heuristically tell "the provider/driver is not
        // installed in this bitness" apart from ordinary open/data errors,
        // so the caller can return exit code 2 (retry the other bitness)
        // versus 3 (don't).
        private static bool isProviderError(Exception ex)
        {
            if (ex is ProviderUnavailable) return true;
            string m = (ex.Message ?? "").ToLowerInvariant();
            if (m.Contains("provider cannot be found")
                || m.Contains("provider is not registered")
                || m.Contains("not registered on the local machine")
                || m.Contains("data source name not found")
                || m.Contains("specified driver could not be loaded")
                || m.Contains("could not find installable isam")
                || m.Contains("architecture mismatch"))
                return true;
            // Common HRESULTs for "provider not found" / "class not registered".
            int h = Marshal.GetHRForException(ex);
            uint u = unchecked((uint)h);
            if (u == 0x800A0E7A      // ADO: provider not found
                || u == 0x80040154)  // REGDB_E_CLASSNOTREG
                return true;
            return false;
        }

        private static bool Is64() { return IntPtr.Size == 8; }

        private static void info(string s) { if (!bQuiet) Console.Out.WriteLine(s); }
        private static void err(string s) { Console.Error.WriteLine("2db: " + s); }

        private static void usage()
        {
            Console.Out.WriteLine(
                "2db -- import a data file into a standard DbDo .db shell.\n\n"
              + "Usage: 2db" + (Is64() ? "64" : "32") + " <source-file> [<dest.db>] [/force] [/quiet]\n\n"
              + "  <source-file>  .mdb .accdb .xlsx .xls .xlsm .xlsb .dbf .csv .tsv .txt .db .sqlite .sqlite3\n"
              + "  <dest.db>      optional; defaults to <source-dir>\\<source-root>.db\n"
              + "  /force         replace an existing destination without prompting\n"
              + "  /quiet         suppress progress output\n\n"
              + "Exit codes: 0 ok/declined, 2 provider unavailable (try other bitness),\n"
              + "            3 source/convert error, 4 usage.");
        }
    }

    // Distinguishes "the provider/driver isn't available in this bitness"
    // from ordinary failures, so it can map cleanly to exit code 2.
    internal sealed class ProviderUnavailable : Exception
    {
        public ProviderUnavailable(string sMessage) : base(sMessage) { }
    }
}
