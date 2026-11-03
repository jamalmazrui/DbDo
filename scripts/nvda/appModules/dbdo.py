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
	# Way: pass quietly. Where: only here, in the list; elsewhere NVDA's own handling.
	@scriptHandler.script(gestures=["kb:upArrow", "kb:downArrow", "kb:pageUp", "kb:pageDown"])
	def script_passRowKey(self, gesture):
		keys.quietNextFocus()
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
			clsList.insert(0, DbDoList)

	# Way: pass quietly. Where: everywhere in DbDo.
	@scriptHandler.script(canPropagate=True, gestures=["kb:alt+control+rightArrow", "kb:alt+control+leftArrow"])
	def script_passKey(self, gesture):
		keys.passQuietly(gesture)
