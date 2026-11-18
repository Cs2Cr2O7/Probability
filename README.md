# Probability — item category filter for Probably Stolen

A mod for [Probably Stolen](https://store.steampowered.com/) that adds a category filter for
items in the inventory and in open containers, plus custom items and machines: the **Super Charm
Perfume**, the **Molecular Assembly Room** and the **Storage Terminal**.

## Item category filter

Open the inventory or a container, then click the **Filter** button in the negotiation tablet's top
bezel (or use the *Probability: item category filter* button in the F9 dev menu), pick a category —
every item that does not carry that category is dimmed, the matching ones stay readable, so you can
find "all the medical stuff" or "all the firearms" at a glance.

The negotiation tablet is the screen you use while a customer is buying; its **Filter** button sits
next to the resize/close icons, so the filter is one click away during a trade. It is the only
in-game entry (the F7 hotkey was removed in 0.3.4.1); the F9 dev-menu button remains for debugging.

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
* Cheaper templates come out in bigger batches (4 / 3 / 2 / 1 copies depending on item value). The four pocket output slots each hold one item whatever its shape, and a batch is delivered whole.
* The extension bay holds up to three universal modules: each adds progress per tick and also raises
  the battery drain by 25% per tick (25% base, 100% with three modules).
* The template slot refuses quest/important items, machines and containers that still hold items.

**How to get it:** there is no shop entry. Leave one 3D printer, one uncapped neural core, six
hypertech electronics, ten printer plastic, ten common electronics and five wire in the store, then
go to sleep — in the morning the machine has assembled itself and the ingredients are consumed. The
hint is written in the gray italic line of the printer tooltip.

## Storage Terminal

A list-based storage machine (存储终端). It has ten hard-drive bays; the five drive tiers currently
hold 4 / 8 / 12 / 16 / 20 items each. Stored items live in a real hidden inventory, while the window
shows one row per item identity and state (`toilet paper x2`), ten rows per page. Select a row, pick a
quantity, and take the items out: the bag is filled first, then the player-side counter, taking as
many as fit. Non-empty grid containers are refused, and a drive cannot be removed if the remaining
capacity would be smaller than the stored item count. No battery and no filter integration.

The control panel's **Search** button opens a search window. Plain text matches item names and
identifiers (space-separated words are ANDed); a `#` prefix searches enabled tags, e.g.
`#FOOD_TAG` or `#食物`. Chinese tag aliases cover the game's filter categories (food, weapons,
materials, medical, containers, machinery, electronics, liquids, junk, documents, and so on).

* The machine and drives are handed out through the F9 dev menu for now:
  **F9 → Modding → Probability: give 存储终端 / give 存储硬盘一套**.
* Both the basic flows and search were accepted in game on 2026-11-18.

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
3. Open the negotiation tablet and click *Filter* in game.

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
| `src\Probability\` | the mod (`ModEntry.cs`, `ItemFilter.cs`, `FilterWindow.cs`, `TabletFilterEntry.cs`, `Labels.cs`, `CharmPerfume.cs`, `CharmEffect.cs`, `CharmPatches.cs`, `MolecularAssembler.cs`, `StorageTerminal.cs`, `StorageTerminalDirectory.cs`, `manifest.xml`, `Localization\`) |
| `src\Directory.Build.props` | shared build settings + install-to-Mods step |

Maintenance notes (feature acceptance, architecture, decisions, history) are kept in the local
workspace and are not published in this repository.

## Status

Current version: **0.3.5.0**. Compiles against game version 1.0.0.x; the filter (with its
negotiation-tablet button), the charm perfume, the molecular assembly room and the storage terminal
are verified in a running game. The F7 hotkey was removed in 0.3.4.1 (per user request); the tablet
button is the only in-game entry. The storage terminal's list/deposit/withdraw flows, capacity-gated
direct drag and name/tag search (including Chinese tag aliases) are accepted.

## Credits

Modding API and examples by the game's developers (see the
[probably-stolen-modding](https://github.com/dragonpan2/probably-stolen-modding) guide).
