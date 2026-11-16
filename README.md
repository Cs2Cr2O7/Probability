# Probability — item category filter for Probably Stolen

A mod for [Probably Stolen](https://store.steampowered.com/) that adds a category filter for
items in the inventory and in open containers, plus one custom item: the **Super Charm Perfume**.

## Item category filter

Open the inventory or a container, press **F7** (or use the *Probability: item category filter*
button in the F9 dev menu), pick a category — every item that does not carry that category is
dimmed, the matching ones stay readable, so you can find "all the medical stuff" or "all the
firearms" at a glance.

## Super Charm Perfume

A custom consumable ("有效成分提取自魅魔分泌物" / *active ingredient extracted from succubus
secretions*). Spray it while a customer is at the counter: for that customer, barganing always
succeeds no matter how steep the multiplier is, reputation never drops, and forged item labels
are not exposed.

* The item is registered through the game's own item directory, so it saves and loads like a
  normal item.
* Effects are applied with a few Harmony patches and end when that customer leaves.
* For now it is handed out through the dev menu: **F9 → Modding → Probability: give 超级魅惑香水**.

## Molecular Assembly Room

A custom battery-powered machine (分子装配室). Put a battery in the power slot and an item in the
template slot: every 30 seconds of real time the output grid receives fresh copies of that item.

* Copies are created through the game's item directory, so they are normal items that save and load.
* Contents are not duplicated: liquids, inserted cards and modules stay with the template.
* Cheaper templates come out in bigger batches (4 / 3 / 2 / 1 copies depending on item value).
* The extension bay holds up to three universal modules: each adds progress per tick and also raises
  the battery drain by 25% per tick (25% base, 100% with three modules).
* The template slot refuses quest/important items, machines and containers that still hold items.

**How to get it:** there is no shop entry. Leave one 3D printer, three Super Charm Perfumes, one
uncapped neural core and twenty pieces of scrap metal in the store, then go to sleep — in the
morning the machine has assembled itself and the ingredients are consumed. The hint is written in
the gray italic lines of the printer and perfume tooltips.

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
| `src\Probability\` | the mod (`ModEntry.cs`, `ItemFilter.cs`, `FilterWindow.cs`, `Labels.cs`, `CharmPerfume.cs`, `CharmEffect.cs`, `CharmPatches.cs`, `MolecularAssembler.cs`, `manifest.xml`, `Localization\`) |
| `src\Directory.Build.props` | shared build settings + install-to-Mods step |

Maintenance notes (feature acceptance, architecture, decisions, history) are kept in the local
workspace and are not published in this repository.

## Status

Compiles against game version 1.0.0.x; the filter is verified in a running game. The charm
perfume and the molecular assembly room are implemented but have not been verified in a live
save yet.

## Credits

Modding API and examples by the game's developers (see the
[probably-stolen-modding](https://github.com/dragonpan2/probably-stolen-modding) guide).
