# appModules/dbdo.py
#
# NVDA app module for DbDo.exe.
#
# When DbDo is the foreground application, pass selected keyboard
# chords through to DbDo instead of running NVDA's own default
# scripts. DbDo provides its own table-style cell navigation,
# parent-child drill, search, and command shortcuts; without this
# add-on, NVDA intercepts those chords (especially Alt+Control+arrow
# which is browse-mode table navigation) and DbDo never sees them.
#
# Why this filename is lowercase. Python imports on Windows are
# case-sensitive even though the filesystem is case-insensitive
# (PEP 235). NVDA reads the executable basename from
# FProcessEntry32.szExeFile, strips the .exe, and uses that as the
# Python module name to import. The compiled DbDo.exe stores its
# basename as DbDo, so NVDA tries to import appModules.DbDo. But
# the long-established NVDA add-on convention -- matching what every
# example in the NVDA Developer Guide and every shipped app module
# in NVDA core does -- is to lowercase the .py filename. NVDA's
# import machinery handles the case mismatch via a separate code
# path: it also tries appModules.<lowercase>. Using lowercase here
# matches the convention and works on every NVDA version.
#
# Why this uses the @scriptHandler.script decorator. The decorator
# pattern is the modern NVDA API for declaring scripts with their
# gestures, and is the pattern every example in the NVDA Developer
# Guide demonstrates today. Internally NVDA's ScriptableObject
# metaclass walks the script_* methods and harvests their .gesture
# / .gestures attributes set by the decorator into the class-level
# __gestures dict.
#
# Why canPropagate=True on every script. The @scriptHandler.script
# decorator's canPropagate parameter defaults to False, meaning the
# script only matches when the focused NVDAObject is the AppModule
# itself rather than a child or descendant of it. DbDo's actual
# focus at runtime is typically the data-grid ListView, NOT the
# top-level form. canPropagate=True allows the script to fire when
# our AppModule appears anywhere in the focused object's ancestor
# chain, which is what we want.
#
# Why this uses KeyboardInputGesture.fromName(canonical-name).send()
# instead of gesture.send(). Two reasons.
#
# 1. Six-pack vs numpad gesture-identifier asymmetry. NVDA
# distinguishes six-pack arrow keys from numpad arrow keys at the
# gesture-identifier level using the Windows extended-key bit.
# vkCodes.py maps (0x27, True) to "rightArrow" (six-pack) and
# (0x27, False) to "numpad6" (numpad, NumLock off). These are TWO
# DIFFERENT NVDA identifiers, so a gesture-list binding has to list
# both explicitly. (The earlier v1.0.57 attempt listed nonexistent
# names like "numpadRightArrow" -- those are not valid NVDA
# identifiers and silently never matched, which is why the numpad
# kept "working" via Windows' input-dispatch fallthrough rather than
# via the script.) JAWS does not have this asymmetry because its
# .jkm key map uses logical names like "RightArrow" that resolve to
# both physical sources; one binding catches both.
#
# 2. gesture.send() asymmetry on extended vs non-extended keys.
# NVDA's KeyboardInputGesture.send() uses sequential keybd_event
# calls with the original gesture's isExtended flag. For extended
# keys (six-pack arrows), the synthesized keystroke makes its way
# through Windows' input queue with the KEYEVENTF_EXTENDEDKEY flag;
# for non-extended keys (numpad arrows with NumLock off), it does
# not. In testing with DbDo on NVDA 2025.x and 2026.x, the
# extended-key injection path delivers the keystroke to NVDA's own
# LL hook again rather than to DbDo's window procedure, which
# results in silence. The non-extended path delivers to DbDo
# correctly. The workaround is to construct a FRESH gesture via
# KeyboardInputGesture.fromName(canonical) where canonical is the
# six-pack name, and call .send() on that. The fresh gesture's
# scan-code resolution is done at construction time by VkKeyScanEx
# and produces a path that DbDo reliably receives.
#
# The script function in each group derives the canonical name from
# its own binding category (virtual cursor maps numpad6 -> right,
# numpad4 -> left, numpad8 -> up, numpad2 -> down, numpad7 -> home,
# numpad1 -> end, numpad9 -> pageUp, numpad3 -> pageDown; numpad5
# stays numpad5 since it has no six-pack equivalent).
#
# Diagnostic logging. Every entry point logs via NVDA's log handler
# so the add-on's behavior can be confirmed from the user's nvda.log.
# Set NVDA's log level to "Debug" (Preferences > Settings > General >
# Log level) to see the per-keypress logs. To diagnose:
#   1. Restart NVDA after install (NVDA menu > Restart).
#   2. Open DbDo.
#   3. Press Alt+Control+RightArrow once.
#   4. NVDA menu > Tools > View log; search "dbdo".
# Each script invocation should produce one "dbdo: passVirtualCursor"
# line; absence means NVDA never matched the gesture to our script.

import appModuleHandler, scriptHandler
from logHandler import log
from NVDAObjects import NVDAObject

# THE KIT'S KEY HANDLING (2 November 2026). Every Homer add-on handles keys with
# the kit's exec\Python\keys.py, which DbDo's build copies into appModules\homer:
# the same code in DbDo and HomerView, and the same five ways as their JAWS
# scripts. passQuietly sends the six-pack form of a key, the lesson recorded
# above about number-pad keys and extended keys.
from .homer import keys


# THE GRID'S ROW KEYS, PASSED ONLY IN THE GRID (2 November 2026). Up and Down
# Arrow and Page Up and Page Down are DbDo's in its data grid: DbDo moves the
# row and speaks it itself, the row's shown values and then its position. Left
# to NVDA, each key also read the new row with NVDA's position information
# ("44838 of 49186"). These keys belong to every list and edit field too, so
# they are bound on DbDoGrid, an overlay class NVDA gives only to DbDo's grid --
# its one list view, window class SysListView32 -- and its rows. Anywhere else
# NVDA handles them as always. In the grid the key is passed through, and the
# one focus announcement the move causes is left unspoken; NVDA's keyboard
# interrupt still cuts off DbDo's speech for the previous row.
class DbDoGrid(NVDAObject):
	# Way: pass quietly. Where: only here, in the grid; elsewhere NVDA's own handling.
	@scriptHandler.script(
		description="Pass DbDo's grid row keys to DbDo, which speaks the row itself.",
		gestures=["kb:upArrow", "kb:downArrow", "kb:pageUp", "kb:pageDown"],
	)
	def script_passRowKey(self, gesture):
		# Way: pass quietly. Where: everywhere in DbDo.
		log.debug("dbdo: passRowKey %r" % (gesture.identifiers[0] if gesture.identifiers else ""))
		keys.passQuietly(gesture)

	def event_gainFocus(self):
		if keys.focusIsQuiet():
			return
		super().event_gainFocus()


class AppModule(appModuleHandler.AppModule):

	# WHO THIS ADD-ON IS (2 November 2026). scriptCategory puts every script here
	# in NVDA's Input Gestures dialog under the app's name, where any key can be
	# rebound; NVDA+Shift+V says the program's version and the add-on's, the
	# counterpart of the JAWS scripts' JAWS+Q.
	scriptCategory = "DbDo"

	@scriptHandler.script(description="Say DbDo's version and the add-on's.", gesture="kb:nvda+shift+v")
	def script_sayVersions(self, gesture):
		keys.sayVersions(self)
	def chooseNVDAObjectOverlayClasses(self, obj, clsList):
		if keys.windowClassIs(obj, "SysListView32"):
			clsList.insert(0, DbDoGrid)


	def __init__(self, *args, **kwargs):
		super().__init__(*args, **kwargs)
		log.info(
			"dbdo: app module __init__ running; appName=%r processID=%r"
			% (getattr(self, "appName", "?"),
			   getattr(self, "processID", "?"))
		)

	# Group 1: Virtual cursor (NVDA's table-navigation chords). The
	# Alt+Control+arrow family is what NVDA's browse-mode
	# script_nextColumn / script_previousColumn / script_nextRow /
	# script_previousRow handlers normally claim. We need these back
	# so DbDo's own virtual-cell navigation can run.
	#
	# Documented convention in both JAWS and NVDA is six-pack arrows
	# for Control+Alt+arrow cell navigation. We bind the six-pack
	# identifiers as the documented primary, and ALSO bind the
	# numpad-numeric identifiers (numpad7/1/9/3/8/2/4/6) for users
	# who use the numpad with NumLock off. Numpad5 is the documented
	# numpad-only say-current-cell chord. The script handler
	# translates either source to the six-pack canonical name before
	# sending, so DbDo always sees the same VK code.
	@scriptHandler.script(
		canPropagate=True,
		description="Pass DbDo's virtual-cursor chords through to the application.",
		gestures=[
			# Six-pack inverted-T arrows + Home/End/PageUp/PageDown.
			# Documented convention for both JAWS and NVDA.
			"kb:alt+control+home",
			"kb:alt+control+end",
			"kb:alt+control+rightArrow",
			"kb:alt+control+leftArrow",
			"kb:alt+control+downArrow",
			"kb:alt+control+upArrow",
			"kb:alt+control+pageDown",
			"kb:alt+control+pageUp",
			# Numpad equivalents with NumLock off. NVDA names these
			# by digit (numpad7 = Home, numpad1 = End, numpad9 = PgUp,
			# numpad3 = PgDn, numpad8 = Up, numpad2 = Down,
			# numpad4 = Left, numpad6 = Right) per vkCodes.py.
			"kb:alt+control+numpad7",
			"kb:alt+control+numpad1",
			"kb:alt+control+numpad9",
			"kb:alt+control+numpad3",
			"kb:alt+control+numpad8",
			"kb:alt+control+numpad2",
			"kb:alt+control+numpad4",
			"kb:alt+control+numpad6",
			# Numpad5: say-current-cell. Documented as
			# "Ctrl+Alt+Numpad5 - Read current cell" in JAWS docs.
			# No six-pack equivalent.
			"kb:alt+control+numpad5",
			# The screen readers' table reading keys, with Shift (31 October 2026).
			"kb:alt+control+shift+leftArrow",
			"kb:alt+control+shift+rightArrow",
			"kb:alt+control+shift+upArrow",
			"kb:alt+control+shift+downArrow",
			"kb:alt+control+shift+numpad5",
		],
	)
	def script_passVirtualCursor(self, gesture):
		# Way: pass quietly. Where: everywhere in DbDo.
		log.debug("dbdo: passVirtualCursor %r" % (gesture.identifiers[0] if gesture.identifiers else ""))
		keys.passQuietly(gesture)

	# Group 2: Alt-letter command shortcuts. DbDo uses Alt+A, Alt+C,
	# Alt+D, etc. as direct command hotkeys. Some of these (Alt+R)
	# conflict with NVDA's default chord for things like reviewing
	# recent text -- we need them back so DbDo's menu mnemonics and
	# toolbar shortcuts work. Letter keys are not affected by the
	# six-pack vs numpad distinction; pass through directly.
	@scriptHandler.script(
		canPropagate=True,
		description="Pass DbDo's Alt-letter command chords through to the application.",
		gestures=[
			"kb:alt+a",
			"kb:alt+c",
			"kb:alt+d",
			"kb:alt+e",
			"kb:alt+k",
			"kb:alt+l",
			"kb:alt+p",
			"kb:alt+r",
			"kb:alt+t",
			"kb:alt+y",
			"kb:alt+z",
			"kb:alt+shift+a",
			"kb:alt+shift+d",
			"kb:alt+shift+r",
		],
	)
	def script_passAltLetter(self, gesture):
		# Way: pass quietly. Where: everywhere in DbDo.
		log.debug("dbdo: passAltLetter %r" % (gesture.identifiers[0] if gesture.identifiers else ""))
		keys.passQuietly(gesture)

	# Group 3: Parent-child drill. Alt+arrow plus Alt+Home / Alt+End
	# are DbDo's drill-into-related-table and drill-back chords.
	# Six-pack primary + numpad-numeric variants.
	@scriptHandler.script(
		canPropagate=True,
		description="Pass DbDo's parent-child drill chords through to the application.",
		gestures=[
			"kb:alt+rightArrow",
			"kb:alt+leftArrow",
			"kb:alt+home",
			"kb:alt+end",
			"kb:alt+numpad6",
			"kb:alt+numpad4",
			"kb:alt+numpad7",
			"kb:alt+numpad1",
			# The cell above or below (31 October 2026).
			"kb:alt+upArrow",
			"kb:alt+downArrow",
		],
	)
	def script_passDrill(self, gesture):
		# Way: pass quietly. Where: everywhere in DbDo.
		log.debug("dbdo: passDrill %r" % (gesture.identifiers[0] if gesture.identifiers else ""))
		keys.passQuietly(gesture)

	# Group 4: Search families. Control+F, Control+J, F3 plus Shift
	# variants. NVDA does not have many default bindings for these,
	# but we register them defensively so DbDo's search-from-cell
	# (Control+J) and Find-Next behaviors are stable across NVDA
	# version changes. Function keys do not have the six-pack/numpad
	# distinction; pass through directly.
	@scriptHandler.script(
		canPropagate=True,
		description="Pass DbDo's search chords through to the application.",
		gestures=[
			"kb:control+f",
			"kb:control+j",
			"kb:control+shift+f",
			"kb:control+shift+j",
			"kb:control+f3",
			"kb:control+shift+f3",
			"kb:f3",
			"kb:shift+f3",
		],
	)
	def script_passSearch(self, gesture):
		# Way: pass quietly. Where: everywhere in DbDo.
		log.debug("dbdo: passSearch %r" % (gesture.identifiers[0] if gesture.identifiers else ""))
		keys.passQuietly(gesture)

	# Group 5: Marked-row navigation. Control+Home/End/Up/Down jump
	# among marked rows in DbDo. NVDA's defaults for these are
	# document-level (start/end of document, line up/down) which we
	# do not want. Six-pack primary + numpad-numeric variants.
	@scriptHandler.script(
		canPropagate=True,
		description="Pass DbDo's marked-row chords through to the application.",
		gestures=[
			"kb:control+home",
			"kb:control+end",
			"kb:control+upArrow",
			"kb:control+downArrow",
			"kb:control+numpad7",
			"kb:control+numpad1",
			"kb:control+numpad8",
			"kb:control+numpad2",
			# First and last marked record (31 October 2026).
			"kb:control+pageUp",
			"kb:control+pageDown",
			"kb:control+numpad9",
			"kb:control+numpad3",
		],
	)
	def script_passMarkedRow(self, gesture):
		# Way: pass quietly. Where: everywhere in DbDo.
		log.debug("dbdo: passMarkedRow %r" % (gesture.identifiers[0] if gesture.identifiers else ""))
		keys.passQuietly(gesture)

	# Group 6: Bulk-mark span. Shift+Home/End and Alt+Shift+Home/End
	# select rows or ranges. NVDA's defaults are document/selection
	# manipulation which we do not want here. Six-pack primary +
	# numpad-numeric variants.
	@scriptHandler.script(
		canPropagate=True,
		description="Pass DbDo's bulk-mark chords through to the application.",
		gestures=[
			"kb:shift+home",
			"kb:shift+end",
			"kb:alt+shift+home",
			"kb:alt+shift+end",
			"kb:shift+numpad7",
			"kb:shift+numpad1",
			"kb:alt+shift+numpad7",
			"kb:alt+shift+numpad1",
			# Marking and unmarking to the first or last row (31 October 2026).
			"kb:control+shift+home",
			"kb:control+shift+end",
			"kb:alt+control+shift+home",
			"kb:alt+control+shift+end",
			"kb:control+shift+numpad7",
			"kb:control+shift+numpad1",
		],
	)
	def script_passBulkMark(self, gesture):
		# Way: pass quietly. Where: everywhere in DbDo.
		log.debug("dbdo: passBulkMark %r" % (gesture.identifiers[0] if gesture.identifiers else ""))
		keys.passQuietly(gesture)

	# Group 7: Say-X data-list chords. Shift+D, Shift+L, Shift+T,
	# Shift+Y, Shift+F4 trigger DbDo's data-list announcements
	# (say-Date, say-Lookup, say-Title, etc.). Without passthrough,
	# NVDA's default Shift+letter behaviors win. Letter keys and
	# function keys do not have the six-pack/numpad distinction.
	@scriptHandler.script(
		canPropagate=True,
		description="Pass DbDo's say-X data-list chords through to the application.",
		gestures=[
			"kb:shift+a",
			"kb:shift+c",
			"kb:shift+d",
			"kb:shift+k",
			"kb:shift+l",
			"kb:shift+n",
			"kb:shift+t",
			"kb:shift+y",
			"kb:shift+f4",
			"kb:alt+shift+m",
			"kb:f8",
			"kb:shift+f8",
			"kb:alt+f8",
			"kb:alt+shift+f8",
			"kb:alt+delete",
			"kb:shift+8",
			"kb:numpadMultiply",
			"kb:alt+'",
		],
	)
	def script_passSayX(self, gesture):
		# Way: pass quietly. Where: everywhere in DbDo.
		log.debug("dbdo: passSayX %r" % (gesture.identifiers[0] if gesture.identifiers else ""))
		keys.passQuietly(gesture)
