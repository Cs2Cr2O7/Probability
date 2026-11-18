using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Probability
{
    /// <summary>
    /// The storage terminal: ten hard-drive bays, a hidden real inventory, and a list view.
    /// The list is only a projection of the hidden inventory; real <see cref="GameItem"/>s are
    /// moved in and out, so tags, contents and use counts survive a round trip.
    /// </summary>
    internal static class StorageTerminal
    {
        internal const string ItemId = "probability_storage_terminal";
        internal const string DriveTag = "PROBABILITY_STORAGE_DRIVE_TAG";
        internal const string CapacityTag = "PROBABILITY_DRIVE_CAPACITY_INT";
        internal const string TierTag = "PROBABILITY_DRIVE_TIER_INT";

        internal static readonly string[] DriveIds =
        {
            "probability_storage_drive_mk1",
            "probability_storage_drive_mk2",
            "probability_storage_drive_mk3",
            "probability_storage_drive_mk4",
            "probability_storage_drive_mk5"
        };

        /// <summary>Item capacity per drive tier. Ten top-tier drives therefore hold 200 items.</summary>
        internal static readonly int[] DriveCapacities = { 4, 8, 12, 16, 20 };

        internal static readonly long[] DriveValues = { 150L, 400L, 900L, 2000L, 4500L };

        // Game tags are internal English identifiers; these aliases let Chinese queries
        // such as #食物 or #武器 hit the corresponding enabled tag without localizing tags.
        private static readonly Dictionary<string, string[]> TagAliases =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "食物", new[] { "FOOD", "EDIBLE", "CONSUMABLE" } },
                { "食品", new[] { "FOOD", "EDIBLE", "CONSUMABLE" } },
                { "饮料", new[] { "DRINK", "BEVERAGE", "LIQUID" } },
                { "酒", new[] { "ALCOHOL", "DRINK" } },
                { "武器", new[] { "WEAPON", "FIREARM", "MELEE" } },
                { "枪", new[] { "FIREARM", "GUN", "WEAPON" } },
                { "弹药", new[] { "AMMO", "AMMUNITION" } },
                { "材料", new[] { "MATERIAL", "RESOURCE" } },
                { "零件", new[] { "PART", "COMPONENT" } },
                { "工具", new[] { "TOOL", "UTILITY" } },
                { "医疗", new[] { "MEDICAL", "MEDICINE", "HEALTH" } },
                { "药品", new[] { "MEDICAL", "MEDICINE", "HEALTH" } },
                { "护甲", new[] { "ARMOR", "ARMOUR", "CLOTHING" } },
                { "服装", new[] { "CLOTHING", "ARMOR", "APPAREL" } },
                { "容器", new[] { "CONTAINER", "BAG", "STORAGE" } },
                { "机械", new[] { "MACHINE", "MACHINERY", "DEVICE" } },
                { "机器", new[] { "MACHINE", "MACHINERY", "DEVICE" } },
                { "电子", new[] { "ELECTRONIC", "CIRCUIT", "TECH" } },
                { "液体", new[] { "LIQUID", "FLUID" } },
                { "垃圾", new[] { "JUNK", "TRASH", "SCRAP" } },
                { "文件", new[] { "DOCUMENT", "PAPER", "FILE" } },
                { "违禁品", new[] { "CONTRABAND", "ILLEGAL" } },
                { "奢侈品", new[] { "LUXURY", "VALUABLE" } },
                { "艺术", new[] { "ART", "DECOR" } },
                { "家具", new[] { "FURNITURE", "DECOR" } },
                { "种子", new[] { "SEED", "PLANT" } },
                { "燃料", new[] { "FUEL", "ENERGY" } },
                { "电池", new[] { "BATTERY", "POWER", "ENERGY" } },
                { "模块", new[] { "MODULE", "UPGRADE" } },
                { "硬盘", new[] { "DRIVE", "STORAGE" } },
                { "钥匙", new[] { "KEY", "ACCESS" } },
                { "卡", new[] { "CARD", "ACCESS" } },
                { "门禁卡", new[] { "ACCESS", "CARD", "KEY" } },
                { "酒类", new[] { "ALCOHOL", "WINE", "DRINK" } },
                { "警报模块", new[] { "ALARM", "MODULE" } },
                { "艺术品", new[] { "ART", "DECOR" } },
                { "基础材料", new[] { "MATERIAL", "BASIC", "RESOURCE" } },
                { "化学用品", new[] { "CHEMICAL", "CHEM" } },
                { "蒸发器模块", new[] { "EVAPORATOR", "MODULE" } },
                { "农用物资", new[] { "FARM", "AGRICULTURE", "SUPPLY" } },
                { "滤芯", new[] { "FILTER" } },
                { "枪械", new[] { "FIREARM", "GUN", "WEAPON" } },
                { "枪械配件", new[] { "GUN", "MOD", "ATTACHMENT" } },
                { "日用百货", new[] { "HOUSEHOLD", "GOOD" } },
                { "水培模块", new[] { "HYDROPONIC", "MODULE" } },
                { "废品", new[] { "JUNK", "SCRAP", "TRASH" } },
                { "彩票", new[] { "LOTTERY", "TICKET" } },
                { "低端货", new[] { "LOWTECH", "LOW" } },
                { "手工制品", new[] { "MAKESHIFT", "CRAFT" } },
                { "加工食品", new[] { "FOOD", "PROCESSED" } },
                { "近战武器", new[] { "MELEE", "WEAPON" } },
                { "中端货", new[] { "MIDLINE", "MID" } },
                { "可改装", new[] { "MODDABLE", "MOD" } },
                { "改良种子", new[] { "SEED", "MODIFIED" } },
                { "军械", new[] { "MUNITION", "AMMO", "WEAPON" } },
                { "毒品", new[] { "NARCOTIC", "DRUG", "ILLEGAL" } },
                { "节点", new[] { "NODE" } },
                { "器官", new[] { "ORGAN" } },
                { "毒药", new[] { "POISON", "TOXIN" } },
                { "打印机模块", new[] { "PRINTER", "MODULE" } },
                { "医疗工具", new[] { "MEDICAL", "TOOL" } },
                { "物质", new[] { "SUBSTANCE", "CHEMICAL", "MATERIAL" } },
                { "补给箱", new[] { "SUPPLY", "CRATE", "CONTAINER" } },
                { "零食", new[] { "SNACK", "FOOD", "TREAT" } },
                { "水", new[] { "WATER", "LIQUID" } },
                { "净水用品", new[] { "WATER", "PURIFICATION", "FILTER" } },
                { "配给水", new[] { "WATER", "RATION" } },
                { "未分类", new[] { "UNCLASSIFIED", "MISC", "MISCELLANEOUS" } }
            };

        private const int DriveSlots = 10;
        private const int PageSize = 10;
        // The hidden inventory is a real grid, but capacity is measured in units. Keep the
        // grid large enough that shape packing does not become the practical limit: 80x80
        // holds the worst case of 200 five-by-five items (5000 cells).
        private const int HiddenWidth = 80;
        private const int HiddenHeight = 80;

        internal static GameItem Create()
        {
            var driveSlots = new GameSlotInventory[DriveSlots];
            var drivesGrid = new GridPixelElement(5, 2);
            for (int i = 0; i < DriveSlots; i++)
            {
                var slot = new GameSlotInventory();
                slot.mayInventoryAddItemFunc = (item, inventory) => IsDrive(item);
                driveSlots[i] = slot;
                drivesGrid.Attach(slot, i % 5, i / 5, 1, 1);
            }

            var depositSlot = new GameSlotInventory(3, 3);
            depositSlot.maxSlotAmountItemFunc = (item, inventory, marker) => 999;

            var hidden = new GameGridInventory(HiddenWidth, HiddenHeight);
            hidden.maxSlotAmountItemFunc = (item, inventory, marker) => 999;

            var rowButtons = new MachineryButton[PageSize];
            var listStack = new MachineryStack();
            listStack.gap = 1;
            listStack.SetAlign(MachineryStack.Align.Start);
            for (int i = 0; i < PageSize; i++)
            {
                var button = new MachineryButton("", null, -1, 16);
                button.SetFillWidth(true);
                rowButtons[i] = button;
                listStack.Add(button);
            }

            var quantitySlider = new MachinerySlider(1f, 120);
            quantitySlider.SetRange(1f, 1f, 1f);
            quantitySlider.SetInt(1);
            var quantityText = new MachineryText("");
            var pageText = new MachineryText("1/1");
            var capacityText = new MachineryText("");
            var searchText = new MachineryText("");
            var messageText = new MachineryText("");
            var previousButton = new MachineryButton("<", null, 28, 18);
            var nextButton = new MachineryButton(">", null, 28, 18);
            var searchButton = new MachineryButton(
                Labels.Get("storage_search", "Search"), null, 110, 18);
            var withdrawButton = new MachineryButton(
                Labels.Get("storage_withdraw", "Take out"), null, 110, 18);

            var pageRow = MachineryStack.Horizontal(previousButton, pageText, nextButton);
            pageRow.SetAlign(MachineryStack.Align.Start);
            var controlStack = MachineryStack.Vertical(
                quantitySlider,
                quantityText,
                withdrawButton,
                pageRow,
                capacityText,
                searchButton,
                searchText,
                messageText);
            controlStack.SetAlign(MachineryStack.Align.Start);

            var storagePanel = MachineHelper.Panel(
                Labels.Get("storage_hidden", "Storage"),
                hidden,
                "storage");
            storagePanel.SetHidden(true);

            MachineryPanel drivesPanel = MachineHelper.Panel(
                Labels.Get("storage_drives", "Drives"), drivesGrid, "drives");
            MachineryPanel depositPanel = MachineHelper.Panel(
                Labels.Get("storage_deposit", "Deposit"), depositSlot, "deposit");
            MachineryPanel listPanel = MachineHelper.Panel(
                Labels.Get("storage_list", "Storage"), listStack, "list");
            MachineryPanel controlPanel = MachineHelper.Panel(
                Labels.Get("storage_control", "Control"), controlStack, "control");

            // Save order is drives, deposit, list, control, storage. The visible right
            // column nests drives/deposit/control; list and control carry no inventories,
            // so the inventory order stays drives, deposit, storage.
            var rightStack = new MachineryStack();
            rightStack.gap = 2;
            rightStack.SetAlign(MachineryStack.Align.Start);
            rightStack.Add(drivesPanel);
            rightStack.Add(depositPanel);
            rightStack.Add(controlPanel);
            var rightColumn = new MachineryPanel((string)null, rightStack, "right");
            listPanel.SetMinWidth(195);
            rightColumn.SetMinWidth(120);

            var panels = new List<MachineryPanel>
            {
                rightColumn,
                listPanel,
                storagePanel
            };

            PixelWindow window = MachineHelper.CreateMachineryWindow(true, panels.ToArray());
            if (window.childElement is MachineryPanelRow panelRow)
            {
                panelRow.SetColumns(2);
                panelRow.SetDisplayOrder(1, 0, 2);
            }

            GameItem machine = ItemDirectory.CreateEmptyItem()
                .SetContentWindow(window)
                .SetSpriteAndShape("Items/items_tool2", "printer_off");
            machine.name = Labels.Get("storage_name", "Storage Terminal");
            machine.flavorText = Labels.Get("storage_flavor",
                "Ten drive bays, one very long shelf.");
            machine.shortDescription = Labels.Get("storage_desc",
                "A list-based storage machine. Install up to ten hard drives; capacity is the sum of their item capacities. Drag items into the deposit slot to store them. Select a row, choose how many to take, and the items go to your bag first, then your side of the counter. Non-empty grid containers cannot be stored, and a drive cannot be removed if the remaining capacity would be smaller than the stored item count.");
            machine.unitValue = 6000L;
            machine.SetGameItemType("MACHINE");
            machine.EnableTag("CONTAINER_TAG");
            machine.EnableTag("STANDARD_MACHINE_TAG");
            machine.EnableTag("IMPORTANT_TAG");
            machine.onLoaded = loaded => loaded.EnableTag("CONTAINER_TAG");

            var state = new TerminalState(
                window,
                machine,
                depositSlot,
                hidden,
                driveSlots,
                rowButtons,
                quantitySlider,
                quantityText,
                pageText,
                capacityText,
                searchText,
                messageText,
                previousButton,
                nextButton,
                searchButton,
                withdrawButton);

            for (int i = 0; i < driveSlots.Length; i++)
            {
                GameSlotInventory slot = driveSlots[i];
                slot.mayInventoryRemoveItemFunc = (item, inventory) => state.CanRemoveDrive(item);
                slot.onSlotAddItemEndFunc += (item, inventory, marker) => state.Refresh();
                slot.onSlotRemoveItemEndFunc += (item, inventory, marker) => state.Refresh();
            }

            // Keep the list honest even if another system/game action moves a hidden item
            // (for example a quick-transfer that found the hidden inventory through the graph).
            hidden.onSlotAddItemEndFunc += (item, inventory, marker) => state.Refresh();
            hidden.onSlotRemoveItemEndFunc += (item, inventory, marker) => state.Refresh();
            hidden.onSlotChangeAmountItemFunc += (item, inventory, marker) => state.Refresh();

            depositSlot.mayInventoryAddItemFunc = (item, inventory) => state.CanStore(item);
            depositSlot.onSlotAddItemEndFunc += (item, inventory, marker) => state.OnDepositAdded(item);
            // Direct drags into the machine are allowed, but the dynamic max amount freezes
            // the transfer at the remaining unit capacity. A stack that only partly fits is
            // split; the rest stays in the source inventory.
            hidden.maxSlotAmountItemFunc = (item, inventory, marker) =>
            {
                int free = state.DirectFreeCapacity();
                int target = marker?.targetItem?.unitCount ?? 0;
                return Mathf.Min(999, free + target);
            };
            hidden.mayInventoryAddItemFunc = (item, inventory) => state.CanAcceptDirect(item);
            searchButton.SetCallback(state.OpenSearch);
            withdrawButton.SetCallback(state.Withdraw);
            previousButton.SetCallback(() => state.ChangePage(-1));
            nextButton.SetCallback(() => state.ChangePage(1));
            quantitySlider.onChanged = value => state.OnQuantityChanged(value);
            window.openCallback += state.Refresh;
            window.closeCallback += state.CloseSearch;

            state.Refresh();
            return machine;
        }

        internal static bool IsDrive(GameItem item)
        {
            return item != null && item.IsTag(DriveTag);
        }

        internal static int UnitCount(GameItem item)
        {
            return item == null ? 0 : Mathf.Max(1, item.unitCount);
        }

        private sealed class Entry
        {
            internal string Signature;
            internal string Name;
            internal int Units;
        }

        private sealed class TerminalState
        {
            private const int PageSize = 10;

            private readonly PixelWindow window;
            private readonly GameItem machine;
            private readonly GameSlotInventory depositSlot;
            private readonly GameGridInventory hidden;
            private readonly GameSlotInventory[] driveSlots;
            private readonly MachineryButton[] rowButtons;
            private readonly MachinerySlider quantitySlider;
            private readonly MachineryText quantityText;
            private readonly MachineryText pageText;
            private readonly MachineryText capacityText;
            private readonly MachineryText searchText;
            private readonly MachineryText messageText;
            private readonly MachineryButton previousButton;
            private readonly MachineryButton nextButton;
            private readonly MachineryButton searchButton;
            private readonly MachineryButton withdrawButton;

            private readonly List<Entry> entries = new List<Entry>();
            private int page;
            private int selected = -1;
            private bool absorbing;
            private string query = "";
            private CustomUIWindow searchWindow;
            private CustomUIElement searchInput;

            internal TerminalState(
                PixelWindow window,
                GameItem machine,
                GameSlotInventory depositSlot,
                GameGridInventory hidden,
                GameSlotInventory[] driveSlots,
                MachineryButton[] rowButtons,
                MachinerySlider quantitySlider,
                MachineryText quantityText,
                MachineryText pageText,
                MachineryText capacityText,
                MachineryText searchText,
                MachineryText messageText,
                MachineryButton previousButton,
                MachineryButton nextButton,
                MachineryButton searchButton,
                MachineryButton withdrawButton)
            {
                this.window = window;
                this.machine = machine;
                this.depositSlot = depositSlot;
                this.hidden = hidden;
                this.driveSlots = driveSlots;
                this.rowButtons = rowButtons;
                this.quantitySlider = quantitySlider;
                this.quantityText = quantityText;
                this.pageText = pageText;
                this.capacityText = capacityText;
                this.searchText = searchText;
                this.messageText = messageText;
                this.previousButton = previousButton;
                this.nextButton = nextButton;
                this.searchButton = searchButton;
                this.withdrawButton = withdrawButton;

                SetMessage(Labels.Get("storage_rule",
                    "Non-empty grid containers cannot be stored."));
            }

            internal void Refresh()
            {
                try
                {
                    RebuildEntries();

                    if (selected >= entries.Count)
                    {
                        selected = entries.Count - 1;
                    }
                    if (selected < -1)
                    {
                        selected = -1;
                    }

                    int pageCount = PageCount();
                    if (page < 0)
                    {
                        page = 0;
                    }
                    if (page >= pageCount)
                    {
                        page = pageCount - 1;
                    }

                    RefreshRows(pageCount);
                    RefreshQuantity();
                    RefreshCapacity();
                    RefreshSearchText();
                }
                catch (Exception e)
                {
                    ModEntry.Log.Error("storage terminal refresh failed: " + e);
                }
            }

            internal void SelectRow(int row)
            {
                int index = page * PageSize + row;
                if (index < 0 || index >= entries.Count)
                {
                    return;
                }
                selected = index;
                RefreshRows(PageCount());
                RefreshQuantity();
            }

            internal void OpenSearch()
            {
                try
                {
                    CloseSearch();
                    if (CustomUIManager.Instance == null)
                    {
                        SetMessage(Labels.Get("storage_search_failed", "Search window is unavailable."));
                        return;
                    }

                    CustomUIBuilder builder = CustomUIManager.Window(
                        "probability_storage_search",
                        Labels.Get("storage_search_title", "Search items"));
                    if (builder == null)
                    {
                        SetMessage(Labels.Get("storage_search_failed", "Search window is unavailable."));
                        return;
                    }

                    builder.UseMachineryStyle();
                    builder.SetSize(300f, 100f);
                    builder.AddInput(
                        Labels.Get("storage_search_hint", "Name or #tag"),
                        query,
                        value => SetQuery(value),
                        onTyping: value => SetQuery(value));
                    CustomUIElement inputElement = builder.Last;

                    builder.BeginRow();
                    builder.AddButton(
                        Labels.Get("storage_search_clear", "Clear"),
                        () =>
                        {
                            inputElement?.SetString("");
                            SetQuery("");
                        });
                    builder.AddButton(
                        Labels.Get("storage_search_close", "Close"),
                        CloseSearch);
                    builder.End();
                    builder.OnClose(CloseSearch);
                    builder.Center();

                    searchWindow = builder.Show();
                    searchInput = inputElement;
                    if (searchInput?.input != null)
                    {
                        searchInput.input.Select();
                        searchInput.input.ActivateInputField();
                    }
                }
                catch (Exception e)
                {
                    ModEntry.Log.Error("storage search window failed: " + e);
                    SetMessage(Labels.Get("storage_search_failed", "Search window is unavailable."));
                }
            }

            internal void CloseSearch()
            {
                CustomUIWindow closing = searchWindow;
                searchWindow = null;
                searchInput = null;
                if (closing != null)
                {
                    closing.Close();
                }
            }

            private void SetQuery(string value)
            {
                string next = (value ?? "").Trim();
                if (next == query)
                {
                    RefreshSearchText();
                    return;
                }
                query = next;
                selected = -1;
                page = 0;
                Refresh();
            }

            private void RefreshSearchText()
            {
                string label = Labels.Get("storage_search", "Search");
                if (query.Length == 0)
                {
                    searchText.SetText(label + ": " + Labels.Get("storage_search_all", "All"));
                    return;
                }
                string display = query.Length > 16 ? query.Substring(0, 16) + "..." : query;
                searchText.SetText(label + ": " + display + " (" + entries.Count + ")");
            }

            private bool Matches(GameItem item)
            {
                string q = query.Trim();
                if (q.Length == 0)
                {
                    return true;
                }

                if (q[0] == '#' || q[0] == '＃')
                {
                    string tagQuery = q.Substring(1).Trim();
                    if (tagQuery.Length == 0)
                    {
                        return false;
                    }
                    string[] tagTerms = tagQuery.Split(
                        new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tagTerms.Length == 0)
                    {
                        return false;
                    }
                    foreach (string term in tagTerms)
                    {
                        if (!TagMatches(item, term))
                        {
                            return false;
                        }
                    }
                    return true;
                }

                string[] words = q.Split(
                    new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string displayName = item.GetDisplayName(preferCustomName: true) ?? "";
                foreach (string word in words)
                {
                    if (!ContainsText(displayName, word)
                        && !ContainsText(item.identifier, word))
                    {
                        return false;
                    }
                }
                return true;
            }

            private static bool ContainsText(string text, string term)
            {
                return !string.IsNullOrEmpty(text)
                    && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private static bool TagMatches(GameItem item, string term)
            {
                if (item?.modifiedState?.dict == null)
                {
                    return false;
                }

                string[] aliases;
                TagAliases.TryGetValue(term, out aliases);
                foreach (KeyValuePair<string, TagState> pair in item.modifiedState.dict)
                {
                    TagState tag = pair.Value;
                    if (tag == null || !tag.valueEnabled)
                    {
                        continue;
                    }
                    if (ContainsText(pair.Key, term)
                        || ContainsText(tag.identifier, term)
                        || ContainsText(tag.identifierName, term))
                    {
                        return true;
                    }
                    if (aliases == null)
                    {
                        continue;
                    }
                    foreach (string alias in aliases)
                    {
                        if (ContainsText(pair.Key, alias)
                            || ContainsText(tag.identifier, alias)
                            || ContainsText(tag.identifierName, alias))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            internal void ChangePage(int delta)
            {
                int pageCount = PageCount();
                int target = Mathf.Clamp(page + delta, 0, pageCount - 1);
                if (target == page)
                {
                    return;
                }
                page = target;
                selected = -1;
                RefreshRows(pageCount);
                RefreshQuantity();
            }

            internal void OnQuantityChanged(float value)
            {
                if (selected < 0 || selected >= entries.Count)
                {
                    return;
                }
                int units = Mathf.Max(1, Mathf.RoundToInt(value));
                quantityText.SetText(Labels.Get("storage_quantity", "Quantity") + " " + units);
            }

            internal bool CanStore(GameItem item)
            {
                if (item == null)
                {
                    return false;
                }
                if (IsNonEmptyGridContainer(item))
                {
                    return false;
                }
                int units = UnitCount(item);
                if (Capacity() - StoredUnits() < units)
                {
                    return false;
                }
                SlotMarker marker = hidden.TryFindOneValidInventorySlot(item);
                return marker != null && marker.numTransfer >= units;
            }

            internal bool CanAcceptDirect(GameItem item)
            {
                if (item == null || IsNonEmptyGridContainer(item))
                {
                    return false;
                }
                // Exact amount limiting happens in hidden.maxSlotAmountItemFunc, so a direct
                // drag may move only the part that fits and leave the rest in the source stack.
                return DirectFreeCapacity() > 0;
            }

            internal int DirectFreeCapacity()
            {
                return Mathf.Max(0, Capacity() - StoredUnits());
            }

            internal bool CanRemoveDrive(GameItem item)
            {
                if (item == null || !IsDrive(item))
                {
                    return true;
                }
                int remaining = Capacity() - DriveCapacity(item);
                return remaining >= StoredUnits();
            }

            internal void OnDepositAdded(GameItem item)
            {
                if (absorbing || SaveManager.IsDecoding || item == null)
                {
                    return;
                }

                absorbing = true;
                try
                {
                    if (!CanStore(item))
                    {
                        SetMessage(Labels.Get("storage_cannot_store",
                            "That item cannot be stored (non-empty grid container or no room)."));
                        return;
                    }

                    int units = UnitCount(item);
                    int remaining = GraphUtils.TryAcceptAll(hidden, item, units);
                    if (remaining == 0)
                    {
                        SetMessage(Labels.Get("storage_stored", "Stored."));
                    }
                    else
                    {
                        SetMessage(Labels.Get("storage_store_partial",
                            "Not enough room; some items remain in the deposit slot."));
                    }
                }
                catch (Exception e)
                {
                    ModEntry.Log.Error("storage deposit failed: " + e);
                    SetMessage(Labels.Get("storage_store_failed", "Could not store that item."));
                }
                finally
                {
                    absorbing = false;
                    Refresh();
                }
            }

            internal void Withdraw()
            {
                if (selected < 0 || selected >= entries.Count)
                {
                    SetMessage(Labels.Get("storage_nothing_selected", "Select an entry first."));
                    return;
                }

                Entry entry = entries[selected];
                int requested = quantitySlider.GetInt();
                requested = Mathf.Clamp(requested, 1, Mathf.Max(1, entry.Units));
                int remaining = requested;
                int taken = 0;

                try
                {
                    var items = new List<GameItem>(hidden.childItems);
                    foreach (GameItem item in items)
                    {
                        if (remaining <= 0)
                        {
                            break;
                        }
                        if (item == null || Signature(item) != entry.Signature)
                        {
                            continue;
                        }

                        int take = Mathf.Min(remaining, UnitCount(item));
                        int unplaced = GiveToPlayer(item, take);
                        int moved = take - unplaced;
                        if (moved <= 0)
                        {
                            break;
                        }
                        remaining -= moved;
                        taken += moved;
                    }
                }
                catch (Exception e)
                {
                    ModEntry.Log.Error("storage withdrawal failed: " + e);
                }

                Refresh();

                if (taken <= 0)
                {
                    SetMessage(Labels.Get("storage_no_space",
                        "No room in the bag or the front counter."));
                }
                else if (remaining > 0)
                {
                    SetMessage(Labels.Get("storage_take_partial",
                        "Bag/counter full; took as many as would fit: ") + taken);
                }
                else
                {
                    SetMessage(Labels.Get("storage_taken", "Taken: ") + taken);
                }
            }

            private int GiveToPlayer(GameItem item, int amount)
            {
                EmporiumEntry store = EmporiumEntry.Instance;
                if (store == null)
                {
                    return amount;
                }

                // Player side first: bag, then the player-side counter (backInv).
                // frontInvinvElement is the customer side and must not receive these items.
                int remaining = amount;

                // Move a whole stack in one step when the destination can hold it, so the
                // game's split path (which copies only the item identifier) is not used.
                if (remaining >= UnitCount(item))
                {
                    remaining = TryGiveWhole(store.invElement, item, remaining);
                    if (remaining > 0)
                    {
                        remaining = TryGiveWhole(store.backInvinvElement, item, remaining);
                    }
                }

                // Fall back to one unit at a time for a partial stack or a nearly full target.
                if (remaining > 0)
                {
                    remaining = GiveToInventory(store.invElement, item, remaining);
                }
                if (remaining > 0)
                {
                    remaining = GiveToInventory(store.backInvinvElement, item, remaining);
                }
                return remaining;
            }

            private static int TryGiveWhole(GameInventory inventory, GameItem item, int remaining)
            {
                if (inventory == null || item == null || item.parentInventory == null)
                {
                    return remaining;
                }
                int units = UnitCount(item);
                if (remaining < units)
                {
                    return remaining;
                }
                SlotMarker marker = inventory.TryFindOneValidInventorySlot(item);
                if (marker == null || marker.numTransfer < units)
                {
                    return remaining;
                }
                int left = GraphUtils.TryAcceptAll(inventory, item, units);
                return left == 0 ? remaining - units : remaining;
            }

            private static int GiveToInventory(GameInventory inventory, GameItem item, int remaining)
            {
                while (remaining > 0 && inventory != null && item != null && item.parentInventory != null)
                {
                    int left = GraphUtils.TryAcceptAll(inventory, item, 1);
                    if (left != 0)
                    {
                        break;
                    }
                    remaining--;
                }
                return remaining;
            }

            private void RebuildEntries()
            {
                entries.Clear();
                var map = new Dictionary<string, Entry>();
                foreach (GameItem item in hidden.childItems)
                {
                    if (item == null || !Matches(item))
                    {
                        continue;
                    }
                    string signature = Signature(item);
                    Entry entry;
                    if (!map.TryGetValue(signature, out entry))
                    {
                        entry = new Entry
                        {
                            Signature = signature,
                            Name = item.GetDisplayName(preferCustomName: true)
                        };
                        map.Add(signature, entry);
                        entries.Add(entry);
                    }
                    entry.Units += UnitCount(item);
                }

                entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }

            private void RefreshRows(int pageCount)
            {
                for (int i = 0; i < rowButtons.Length; i++)
                {
                    int row = i;
                    int index = page * PageSize + row;
                    bool has = index >= 0 && index < entries.Count;
                    if (has)
                    {
                        Entry entry = entries[index];
                        string prefix = index == selected ? "> " : "";
                        rowButtons[row].SetText(prefix + Labels.Shorten(entry.Name, 20) + " x" + entry.Units);
                    }
                    else
                    {
                        rowButtons[row].SetText("");
                    }
                    rowButtons[row].SetCallback(() => SelectRow(row));
                    rowButtons[row].SetDisabled(!has);
                }

                pageText.SetText((page + 1) + "/" + Mathf.Max(1, pageCount));
                previousButton.SetDisabled(page <= 0);
                nextButton.SetDisabled(page >= pageCount - 1);
            }

            private void RefreshQuantity()
            {
                bool has = selected >= 0 && selected < entries.Count;
                int units = has ? entries[selected].Units : 1;
                quantitySlider.SetRange(1f, Mathf.Max(1, units), 1f);
                quantitySlider.SetInt(1);
                quantitySlider.SetDisabled(!has || units <= 1);
                withdrawButton.SetDisabled(!has);
                if (has)
                {
                    quantityText.SetText(Labels.Get("storage_quantity", "Quantity") + " 1");
                }
                else
                {
                    quantityText.SetText("");
                }
            }

            private void RefreshCapacity()
            {
                capacityText.SetText(Labels.Get("storage_capacity", "Capacity") + " "
                    + StoredUnits() + "/" + Capacity());
            }

            private void SetMessage(string message)
            {
                messageText.SetText(message ?? "");
            }

            private int PageCount()
            {
                return Mathf.Max(1, (entries.Count + PageSize - 1) / PageSize);
            }

            private int Capacity()
            {
                int total = 0;
                foreach (GameSlotInventory slot in driveSlots)
                {
                    if (slot != null && slot.childItem != null)
                    {
                        total += DriveCapacity(slot.childItem);
                    }
                }
                return total;
            }

            private int StoredUnits()
            {
                int total = 0;
                foreach (GameItem item in hidden.childItems)
                {
                    if (item != null)
                    {
                        total += UnitCount(item);
                    }
                }
                return total;
            }

            private static int DriveCapacity(GameItem item)
            {
                if (item == null)
                {
                    return 0;
                }
                int capacity = item.GetTagReadonly(CapacityTag).GetInt();
                if (capacity > 0)
                {
                    return capacity;
                }
                for (int i = 0; i < DriveIds.Length; i++)
                {
                    if (item.identifier == DriveIds[i])
                    {
                        return DriveCapacities[i];
                    }
                }
                return 0;
            }

            private static bool IsNonEmptyGridContainer(GameItem item)
            {
                if (item == null || item.contentWindow == null)
                {
                    return false;
                }
                try
                {
                    List<GameGridInventory> grids =
                        GraphUtils.FindAllChildrenType<GameGridInventory>(item.contentWindow);
                    foreach (GameGridInventory grid in grids)
                    {
                        if (grid != null && grid.childItems.Count > 0)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception e)
                {
                    ModEntry.Log.Error("container check failed: " + e);
                    // If the contents cannot be inspected, err on the safe side and reject.
                    return true;
                }
                return false;
            }

            private static string Signature(GameItem item)
            {
                var builder = new StringBuilder(64);
                builder.Append(item.identifier);
                if (item.FindAllChildItems().Count > 0
                    || (item.itemFeatures != null && item.itemFeatures.Count > 0))
                {
                    // Distinct contents/features are not fungible: never merge them.
                    builder.Append("#u").Append(item.GetUniqueID());
                    return builder.ToString();
                }

                var keys = new List<string>(item.modifiedState.dict.Keys);
                keys.Sort(StringComparer.Ordinal);
                foreach (string key in keys)
                {
                    TagState tag = item.modifiedState.dict[key];
                    if (tag == null)
                    {
                        continue;
                    }
                    builder.Append('\u001f').Append(key).Append('=');
                    builder.Append(tag.valueEnabled ? '1' : '0').Append(',');
                    builder.Append(tag.valueInt).Append(',');
                    builder.Append(tag.valueLong).Append(',');
                    builder.Append(tag.valueFloat.ToString("R")).Append(',');
                    builder.Append(tag.valueDouble.ToString("R")).Append(',');
                    builder.Append(tag.valueBool ? '1' : '0').Append(',');
                    builder.Append(tag.valueString ?? "");
                }
                return builder.ToString();
            }
        }
    }
}
