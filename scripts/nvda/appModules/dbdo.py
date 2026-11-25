# appModules/dbdo.py -- NVDA app module for DbDo.exe, from the HomerDev template.
#
# Each key has a way and a scope, the same as in DbDo's JAWS scripts (the
# homer-screen-reader skill's references/key-handling.md). The app speaks for
# itself through the kit's Say; these scripts only decide who gets the key. The
# file name is the program's name in lowercase. The build copies the kit's
# exec\Python\keys.py into appModules\homer before packing the add-on.

import appModuleHandler, scriptHandler
from NVDAObjects import NVDAObject

from .homer import keys


class DbDoList(NVDAObject):
	# Way: pass quietly. Where: only here, in the list; elsewhere NVDA's own handling. DbDo moves the record and says
	# what it should; NVDA's announcement of the new row is skipped. The Alt+Control moves are NVDA's own table keys,
	# which DbDo answers by its table model (24 November 2026: they joined the quiet set, as the Alt arrows had).
	@scriptHandler.script(gestures=["kb:upArrow", "kb:downArrow", "kb:pageUp", "kb:pageDown", "kb:alt+upArrow", "kb:alt+downArrow",
		"kb:alt+control+upArrow", "kb:alt+control+downArrow", "kb:alt+control+home", "kb:alt+control+end",
		"kb:alt+control+pageUp", "kb:alt+control+pageDown"])
	def script_passRowKey(self, gesture):
		keys.quietNextFocus()
		keys.passQuietly(gesture)

	# NVDA'S OWN QUESTIONS, ANSWERED BY DbDo IN THE GRID (24 November 2026, Jamal: "Might Insert/JAWS/NVDA+F say the
	# type of data in the current db column?"). Way: answer with the app's key. Where: only here, in the grid; in
	# dialogs and boxes NVDA's own command runs. Each sends the DbDo key that gives the better answer, the same keys
	# DbDo's JAWS scripts send, so both readers hear the same words. NVDA+A is left alone: on a laptop keyboard it is
	# Say All.
	@scriptHandler.script(description="In DbDo's grid, say the current field's declared type.", gesture="kb:nvda+f")
	def script_sayDbDoFieldType(self, gesture):
		keys.answerWithKey("alt+shift+f")

	@scriptHandler.script(description="In DbDo's grid, say the record's position: its number, the count and the percent.",
		gestures=["kb:nvda+numpadDelete", "kb(laptop):nvda+delete"])
	def script_sayDbDoPosition(self, gesture):
		keys.answerWithKey("alt+delete")

	@scriptHandler.script(description="In DbDo's grid, read every shown field of the record.", gesture="kb:nvda+alt+control+leftArrow")
	def script_readDbDoRow(self, gesture):
		keys.answerWithKey("alt+control+shift+rightArrow")

	@scriptHandler.script(description="In DbDo's grid, read every value in the column from the first record.", gesture="kb:nvda+alt+control+upArrow")
	def script_readDbDoColumn(self, gesture):
		keys.answerWithKey("alt+control+shift+downArrow")

	@scriptHandler.script(description="In DbDo's grid, read the column from the current record down.", gesture="kb:nvda+alt+control+downArrow")
	def script_readDbDoColumnFromCurrent(self, gesture):
		keys.answerWithKey("control+l")

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
			clsList.insert(0, DbDoList)

	# Way: pass quietly. Where: everywhere in DbDo.
	@scriptHandler.script(canPropagate=True, gestures=["kb:alt+control+rightArrow", "kb:alt+control+leftArrow"])
	def script_passKey(self, gesture):
		keys.passQuietly(gesture)
