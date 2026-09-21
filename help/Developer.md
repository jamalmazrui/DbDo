# DbDo -- Developer Guide

How to build DbDo from its source and change it. This file is written for
programmers.

## What you need

Nothing by hand. `buildDbDo.cmd` finds or fetches everything it uses:

- the .NET Framework 4.8 C# compiler (`csc.exe`), from Visual Studio Build Tools;
- the Homer Development Kit in `C:\HomerDev`, whose shared C# classes DbDo
  compiles in -- Inix, KeyMap, KeyName, Lbc, Log, Ollama, Paths, Say, Util, Web;
- pandoc, for the .htm versions of the documents;
- Inno Setup, for the installer;
- Python, for the tutorial and hotkey documents;
- ffmpeg and two piper voices, for the spoken tutorials, the first time only.

Missing tools are fetched with winget. Everything the build does is written to
`logs\\DbDo-build-<date>-<time>.log`, a new file for every build.

## Building

From `C:\DbDo`:

    buildDbDo

It compiles `DbDo.cs` into `exec\DbDo.exe`, writes `Hotkeys.md` from the menus,
converts every document to .htm, builds the spoken tutorials if they are
missing, and compiles `DbDo_setup.exe`. The installer will not compile without
the tutorials: a release without them is refused at build time rather than
shipped.

## The layout

The development folder and the installed folder have the same shape. Sources and
build files stay at the top, with ReadMe and License; everything else sits in the
folder it is installed to. To try a build, run `exec\\DbDo.exe`.

`homerTidy`, from the Homer Development Kit, puts the folder back into this
shape: it moves programs into `exec`, documents into `help` and logs into
`logs`, and moves anything the project does not name into `notes`. Run
`homerTidy` to see the plan, `homerTidy --do-it --folder-only` to tidy the
folder alone. `LocalFiles.txt` tells it what belongs on this disk but not in the
repository: the tutorial scripts, the generated audio and the fetched voices.

- `configs` -- settings shipped with the program
- `data` -- the shared lookups database
- `exec` -- the program and its libraries
- `help` -- the documents, the tutorials and their recording
- `logs` -- in the development folder, one file per build, clean, tutorial or
  audit run, named `DbDo-<task>-<date>-<time>.log`. Sort by name and they are in
  order; zip the folder and you have them all.
- `scripts` -- screen reader scripts and tooling
- `templates` -- the sample databases, copied to your own folder on first run

Your own copies live under `%LOCALAPPDATA%\DbDo`.

## Checks

`scripts\auditPatterns.py` checks the rules that are easy to break and hard to
notice, including:

- every Say command answers in the form `name: value`;
- every Shift+letter is a Say command;
- no menu letter falls in the middle of a word, and a menu letter matches its
  hotkey's letter;
- every key a tutorial teaches is bound in the program;
- tutorial scripts are complete.

Run it before a release. It writes `logs\\DbDo-audit-<date>-<time>.log`.

## Keys

Every key follows the Homer rules, written out in `C:\HomerDev\help\HomerDev.md`:
a letter is the first letter of a word in the command; Shift and a letter asks;
adding Shift reverses; each function key is a family. `Hotkeys.md` is generated
from the `addItem` calls, so a key changed in the code is changed in the
document at the next build.

## Tutorials

Each tutorial is a demo script, `help\Tutorial_NN_Topic.inix`: a `[global]`
section for the voices and timing, then one `[step]` per exchange --
`Say=` for the narrator, `Key=` for the keystroke, `Hear=` for the screen reader,
`Note=` for the written transcript only. `scripts\buildTutorials` speaks them
into `help\Tutorials.mkv`; `scripts\makeTutorial` writes `help\Tutorials.md`.
The scripts are not published; the recording and the transcript are.

## Releasing

After a build, `tagRelease` tags the version stamped in `DbDo_setup.exe`,
creates the GitHub release, and checks that the public download link answers.
