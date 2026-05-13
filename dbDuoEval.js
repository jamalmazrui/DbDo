/*
dbDuoEval.js -- JScript .NET support module for DbDuo scripting.

Modeled directly on Jamal Mazrui's Eval.js / jsSupport.js for EdSharp.
The pattern is the same: a tiny class with a static Eval function whose
body uses JScript's built-in `eval(code, "unsafe")`, executed in a
context where the host application's namespace and the standard .NET
BCL namespaces have already been imported. User snippets can then
reference any of those types directly without writing import statements
themselves.

This file is compiled at build time by jsc.exe (the JScript .NET
compiler, ships with .NET Framework v4.0.30319) into dbDuoEval.dll.
DbDuo.exe references that DLL via /reference: and calls
DbDuoScripting.JS.Eval(sCode, frmArg, dbArg) from C# to run user
scripts.

Globals exposed inside the eval scope, visible to user .js snippets:
  frm -- the DbDuoForm instance (passed in from C# as frmArg).
  db  -- shortcut for frm.db (the DbDuoManager).

Naming follows Camel Type conventions: lower-camelCase variables, no
"o" prefix on these (frm is the conventional VB-era abbreviation for
form; db is the conventional abbreviation for a database object).

Usage from C#:
  string sCode = File.ReadAllText("Snippets\\MyScript.js");
  string sResult = DbDuoScripting.JS.Eval(sCode, frmInstance, frmInstance.db);

The returned string is whatever the script's last expression evaluated
to, converted with .ToString(). On compile or runtime error, the
returned string is "ERROR: " followed by the exception message; the
script does NOT throw out to the host so DbDuo's UI stays responsive.

Notes for the build:
  We deliberately do NOT include `import DbDuo;` here. The host DbDuo.exe
  is built AFTER dbDuoEval.dll, so a compile-time DbDuo import would
  create a circular dependency. Snippets reach DbDuo types via the
  late-bound `frm` and `db` parameters which are typed as Object.
  JScript's late-bound dispatch resolves member access at runtime
  with no compile-time type information needed.
*/

import System;
import System.Collections;
import System.Data;
import System.IO;
import System.Reflection;
import System.Text;
import System.Text.RegularExpressions;
import System.Windows.Forms;

package DbDuoScripting {

public class JS {

  // Eval: run a JScript .NET expression / statement block.
  // frmArg and dbArg become the in-scope variables frm and db that
  // user scripts can read and call.
  //
  // Naming: the JScript .NET compiler requires parameter names that
  // start with the type letter ($ is forbidden, Hungarian prefixes
  // are encouraged); we use sCode for the string, and frmArg / dbArg
  // for the host references so the in-scope short names (frm, db)
  // are free for the user snippet to use.
  public static function Eval(sCode : String, frmArg : Object, dbArg : Object) : String {
    // Surface frm and db as in-scope variables to the eval body.
    var frm = frmArg;
    var db  = dbArg;
    try {
      var result = eval(sCode, "unsafe");
      if (result == null) return "";
      if (result == undefined) return "";
      return result.ToString();
    }
    catch (ex) {
      return "ERROR: " + ex.message;
    }
  }

} // class JS

} // package DbDuoScripting
