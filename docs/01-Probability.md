# Probability：设计说明与验证清单

## 目标

在物品栏和已打开的容器里，按"物品类别"筛选，快速找到某一类物品（药品、枪械、材料……）。

## v1 范围（已实现，未进游戏验证）

- `F7` 打开 / 关闭筛选窗口；F9 开发菜单里也有一个同名按钮。
- 窗口列出**当前可见物品**里出现的所有类别及数量，类别名用游戏自带本地化（`type_*`）。
- 点类别 → 命中的物品保持原样，其余变暗（`CanvasGroup.alpha = 0.25`）。
- "显示全部"清除筛选；关闭窗口自动恢复。
- 窗口可拖动，重建时保留位置。

## 实现要点（源码均在 `ref/full` 反编译结果里核对过）

| 环节 | 做法 | 依据 |
|---|---|---|
| 类别来源 | `GameItem.GetGameItemType()` → `List<string>`，值如 `FOOD`/`FIREARM`/`GUN_MOD` | `GameItem.cs:12, 2288-2330` |
| 类别显示名 | `TypeHelper.GetTypeDisplayName(type)` = `LocHelper.GetLocalizedMechanic("type_" + lower)`，缺失时回退到 mod 自带的 `Localization/*.csv`，再回退到 `Prettify` | `TypeHelper.cs:166` |
| 大类排序 | 先按 `TypeHelper.MAJOR_TYPES_LIST`（11 类）顺序，其余按数量 | `TypeHelper.cs:24-28` |
| 可见容器 | `WindowsHandler.current.visibleWindows` → `window.childElement as GameInventory` → `childItems` | `WindowsHandler.cs:10`, `PixelWindow.cs:102`, `GameInventory.cs:17` |
| 无窗口时兜底 | 扫描场景里的 `TreeNodeRender`，取 `element as GameItemElement` | `TreeNodeRender.cs:20`, `GameItemElement.cs:65` |
| 物品视觉根 | `GameItemElement.handler`（`TreeNodeRender`），`handler.gameObject` 上挂 `CanvasGroup` | `GameItemElement.cs:65, 91` |
| 窗口 | `CustomUIManager.Window(id, title, "overlay")` + `CustomUIBuilder` | `CustomUIManager.cs:387` |
| 每帧轮询 | `ModHelper.StartCoroutine`（`ModHook` 没有每帧事件） | `ModHelper.cs:1056` |

## 游戏内验证清单（尚未执行）

1. 把 `build\Mods\Probability` 或直接构建产物装进 `<游戏>\Mods\Probability`，游戏内启用并重启。
2. F8 控制台出现 `[Probability] enabled. Press F7 ...`，无 `Error`。
3. 打开背包/容器，F7 → 窗口出现、列出的数量与实际物品对得上。
4. 点"药品/枪械/…" → 只有该类别保持明亮；再点"显示全部" → 全部恢复。
5. 关闭窗口（X）→ 变暗恢复；再次 F7 能正常重开。
6. 切换存档/场景后再按 F7，行为正常（不应残留变暗）。
7. `mod-scan probability`：`declared` 为空且 `detected` 也为空，无拒绝项。

## 已知风险

- **窗口内容高度**：类别多时窗口高度按行数自适应（上限 760），仍然很多时可能溢出，需要改成滚动列表。
- **物品变暗的时机**：每 0.5 秒重刷一次；新打开的容器在下一拍才跟随筛选。若游戏在某些操作里重建物品节点，`CanvasGroup` 会不会被清掉尚未验证（源码里没有任何地方用 `CanvasGroup`）。
- **"可见物品"的定义**：只覆盖 `visibleWindows` 里的容器；世界里平铺展示、但不属于任何窗口的物品可能漏掉，兜底扫描只在没有窗口时生效。
- **F7 冲突**：游戏只用到 F2/F9，暂未发现冲突。
- **未做**：设置项（改热键/变暗强度）、按容器分组显示、搜索框、图标。

## 后续可做

- 用 `ModConfig` 暴露热键（`AddGameOptionKeyBind` 受 `KeyBindHelper` 私有限制，需另想办法）。
- 结果列表按容器分组（`PixelWindow.parentItems` 给出来源容器名）。
- 用 `TypeHelper.GetTypeColor` 给类别按钮上色。
