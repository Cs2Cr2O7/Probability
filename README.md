# Probability — item category filter for Probably Stolen

A mod for [Probably Stolen](https://store.steampowered.com/) that adds a category filter for
items in the inventory and in open containers.

Open the inventory or a container, press **F7** (or use the *Probability: item category filter*
button in the F9 dev menu), pick a category — every item that does not carry that category is
dimmed, the matching ones stay readable, so you can find "all the medical stuff" or "all the
firearms" at a glance.

## How it works

* Categories come from the game's own taxonomy: `GameItem.itemTypes` (`FOOD`, `MEDICAL`,
  `FIREARM`, `GUN_MOD`, ...). Display names are the game's localized `type_*` strings, so the
  filter follows the game language.
* Items are collected from the inventories behind the currently open windows
  (`WindowsHandler.current.visibleWindows` → `PixelWindow.childElement as GameInventory`), so
  only what is actually in view is filtered.
* Dimming uses a `CanvasGroup` on each item's visual root; nothing in the save file is touched.
  The filter is purely visual and is restored when the window closes.

## Install

1. Build (or grab a release) and copy the mod folder into the game:
   `Probably Stolen\Mods\Probability\` containing `manifest.xml`, `Probability.dll`,
   `Localization\en.csv`, `Localization\zh.csv`.
2. Start the game, open **Mods**, activate **Probability**, restart.
3. Press `F7` in game.

## Build

Requires the .NET SDK (6.0+) and an installed copy of Probably Stolen (its DLLs are referenced,
not redistributed).

```powershell
# default: builds and installs to <game>\Mods\Probability
dotnet build src\Probability -c Release

# override the game folder
dotnet build src\Probability -c Release -p:GameDir="D:\Steam\steamapps\common\Probably Stolen"

# compile only / stage elsewhere
dotnet build src\Probability -c Release -p:DisableModInstall=true
dotnet build src\Probability -c Release -p:ModInstallDir="<repo>\build\Mods\Probability"
```

`src\Directory.Build.props` holds the game path (`GameDir`), the `netstandard2.1` target and the
post-build copy step; each project only declares its assembly references.

## Layout

| Path | Contents |
|---|---|
| `src\Probability\` | the mod (`ModEntry.cs`, `ItemFilter.cs`, `FilterWindow.cs`, `Labels.cs`, `manifest.xml`, `Localization\`) |
| `src\Directory.Build.props` | shared build settings + install-to-Mods step |

Maintenance notes (feature acceptance, architecture, decisions, history) are kept in the local
workspace and are not published in this repository.

## Status

Compiles against game version 1.0.0.x; **not yet verified in a running game**.

## Credits

Modding API and examples by the game's developers (see the
[probably-stolen-modding](https://github.com/dragonpan2/probably-stolen-modding) guide).
