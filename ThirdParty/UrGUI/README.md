# UrGUI (vendored)

IMGUI window library used by the in-game F1 menu, compiled into `BattleImprove.dll`.

- Upstream: https://github.com/Hirashi3630/UrGUI, commit `e24563e` (2022-07-11), MIT licence (`LICENSE`).
- Copied: `UrGUI/UWindow`, `UrGUI/Utils` and the default skin `UrGUI/Skins/main` (embedded as
  `UrGUI.Skins.main`). `Properties/AssemblyInfo.cs` is left out, since this code no longer is an assembly of
  its own.

Until 1.5.10 the plugin shipped a separate `UrGUI.dll`. That build was not plain upstream: its control
builders return the control and the control classes are public, which the menu code relies on. Both changes
are made here again. When the DLL was missing from an install, the game's type scan failed on the menu
types and every save load hung, which is why the library is now part of the plugin.

Changes against upstream:

- `UWindow.Space`, `Label`, `Button`, `Toggle`, `Slider`, `TextField`, `IntField`, `FloatField`,
  `ColorPicker`, `DropDown` and `Separator` return the control they add; `UWindowControls` and its control
  classes are public (as in the DLL shipped up to 1.5.10).
- A duplicate `using UnityEngine;` in `UWindowManager.cs` is removed.

Not a change of code, but of behaviour: the shipped DLL was a Debug build, so its logger also printed Info
lines (the skin loading progress). Built into a Release plugin, upstream's `#if DEBUG` keeps them out.
