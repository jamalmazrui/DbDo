/*
DbDuo.js -- JScript .NET support module for DbDuo scripting.

Modeled on Jamal Mazrui's Eval.js / jsSupport.js for EdSharp. The pattern
is the same: a tiny class with a static function whose body uses JScript's
built-in eval(code, "unsafe"), executed in a context where the host
application's namespace and the standard .NET BCL namespaces have already
been imported. User snippets can then reference any of those types
directly without writing import statements themselves.

This file is compiled at build time by jsc.exe (the JScript .NET compiler,
ships with .NET Framework v4.0.30319) into DbDuo.dll. DbDuo.exe references
that DLL via /reference: and calls DbDuo.JS.runScript via reflection from
C# to run user scripts.

Globals exposed inside the eval scope, visible to user .js snippets:
  frm -- the DbDuoForm instance (passed in from C# as frmArg).
  db  -- shortcut for frm.db (the DbDuoManager).

Naming follows Camel Type conventions: lower-camelCase for functions,
methods, and variables; no "o" prefix on managed types; frm and db are
the conventional short forms for Form and database. The function is
named runScript rather than the more obvious eval because eval is a
JScript built-in -- we cannot shadow it inside our own body.

Usage from C# (via reflection so DbDuo.cs takes no compile-time
dependency on the JScript assembly):
  var asm = Assembly.Load("DbDuo");
  var jsType = asm.GetType("DbDuo.JS");
  var mi = jsType.GetMethod("runScript",
    new Type[] { typeof(string), typeof(object), typeof(object) });
  string sResult = (string)mi.Invoke(null, new object[] { sCode, frm, db });

The returned string is whatever the script's last expression evaluated
to, converted with .ToString(). On compile or runtime error, the
returned string is "ERROR: " followed by the exception message; the
script does NOT throw out to the host so DbDuo's UI stays responsive.

Notes for the build:
  We deliberately do NOT use the DbDuo namespace inside DbDuo.exe's
  C# code at compile time. The host DbDuo.exe is built AFTER DbDuo.dll,
  so a compile-time C# `using DbDuo;` could be ambiguous with the C#
  namespace DbDuo declared in DbDuo.cs. Snippets reach DbDuo types via
  the late-bound frm and db parameters which are typed as Object.
  JScript's late-bound dispatch resolves member access at runtime
  with no compile-time type information needed.

  Note also that JScript .NET's package keyword creates a CLR namespace.
  Because DbDuo.cs's C# namespace is also DbDuo, the type lookup from
  C# is done by string ("DbDuo.JS") via Assembly.GetType, which is
  unambiguous because it queries DbDuo.dll specifically rather than
  the global type table.
*/

import System;
import System.Collections;
import System.Data;
import System.IO;
import System.Reflection;
import System.Text;
import System.Text.RegularExpressions;
import System.Windows.Forms;

package DbDuo {

public class JS {

  // runScript: run a JScript .NET expression or statement block.
  // frmArg and dbArg become the in-scope variables frm and db that
  // user scripts can read and call. The method is named runScript
  // rather than eval because eval is a JScript built-in we use
  // inside the body.
  public static function runScript(sCode : String, frmArg : Object, dbArg : Object) : String {
    // Surface frm and db as in-scope variables to the eval body.
    // JScript .NET's var is untyped so all three variables are
    // declared on one line in alphabetical order per Camel Type.
    var db = dbArg, frm = frmArg, result = null;
    try {
      result = eval(sCode, "unsafe");
      if (result == null) return "";
      if (result == undefined) return "";
      return result.ToString();
    }
    catch (ex) {
      return "ERROR: " + ex.message;
    }
  }

} // class JS

} // package DbDuo
