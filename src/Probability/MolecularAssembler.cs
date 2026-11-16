using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Probability
{
    /// <summary>
    /// The "Molecular Assembly Room" machine: a battery-powered machine with a template slot, an
    /// output grid and a small module bay. Every few game turns it prints fresh copies of whatever
    /// sits in the template slot, drawing energy from the battery in the power slot.
    /// <para>
    /// The copy is created through <c>DirectoryMaster.Item(identifier)</c>, so it is a plain new item
    /// of the same kind: contents (liquids, inserted cards, modules) are not duplicated, which also
    /// keeps containers from being cloned with their contents.
    /// </para>
    /// </summary>
    internal static class MolecularAssembler
    {
        internal const string ItemId = "molecular_assembler";

        /// <summary>Price in credits for the F9 purchase entry.</summary>
        internal const int PurchaseCost = 10000;

        private const string TemplateIdTag = "PROBABILITY_ASSEMBLER_TEMPLATE_ID_TAG";
        private const string ProgressTag = "PROBABILITY_ASSEMBLER_PROGRESS_TAG";
        private const string LastTurnTag = "PROBABILITY_ASSEMBLER_LAST_TURN_TAG";

        /// <summary>Game turns needed for one batch (one turn = 20 in-game minutes).</summary>
        private const int TurnsPerCopy = 3;

        /// <summary>Universal modules that fit into the extension bay.</summary>
        private const int ModuleSlots = 3;

        /// <summary>Energy drawn from the battery when a batch is produced.</summary>
        private const int PowerPerCopy = 20;

        /// <summary>Power usage advertised by the machine, used by the battery tooltip.</summary>
        private const int PowerUsage = 4;

        private static readonly ConditionalWeakTable<GameItem, MachineryText> StatusTexts =
            new ConditionalWeakTable<GameItem, MachineryText>();

        private static int lastProcessedTurn = -1;

        internal static GameItem Create()
        {
            var batterySlot = new GameSlotInventory();
            var templateSlot = new GameSlotInventory();
            var outputGrid = new GameGridInventory(2, 2);
            var moduleGrid = new GameGridInventory(ModuleSlots, 1);
            var statusText = new MachineryText();

            PixelWindow contentWindow = MachineHelper.CreateMachineryWindow(
                true,
                MachineHelper.PowerPanel(batterySlot),
                MachineHelper.Panel(Labels.Get("asm_template", "Template"), templateSlot, "template"),
                MachineHelper.Panel(Labels.Get("asm_output", "Output"), outputGrid, "output"),
                MachineHelper.Panel(Labels.Get("asm_modules", "Modules"), moduleGrid, "modules"),
                MachineHelper.Panel(Labels.Get("asm_progress", "Progress"), statusText, "progress"));

            templateSlot.mayInventoryAddItemFunc = (item, inventory) =>
                GeneralHelper.IsItemOwned(item) && CanBeTemplate(item);
            outputGrid.mayInventoryAddItemFunc = (item, inventory) => GeneralHelper.IsItemOwned(item);
            ContainerHelper.AllowOnlyTaggedItem(moduleGrid, "MODULE_TYPE_UNIVERSAL", ownedOnly: true);

            GameItem machine = ItemDirectory.CreateEmptyItem()
                .SetContentWindow(contentWindow)
                .SetSpriteAndShape("Items/items_tool2", "printer_off");
            machine.name = Labels.Get("asm_name", "Molecular Assembly Room");
            machine.flavorText = Labels.Get("asm_flavor", "Feed it a template and it feeds you copies.");
            machine.shortDescription = Labels.Get("asm_desc",
                "Install a battery, put an item in the template slot, and wait: every few turns the output slot receives fresh copies. Cheaper items come out in bigger batches, and universal modules speed the machine up.");
            machine.unitValue = PurchaseCost;
            machine.SetGameItemType("MACHINE");
            machine.EnableTag("CONTAINER_TAG");
            machine.EnableTag("manufacture");
            machine.EnableTag("STANDARD_MACHINE_TAG");
            machine.EnableTag("IMPORTANT_TAG");
            MachineHelper.SetupBatterySlot(batterySlot, machine);
            MachineryHelper.InitMachinery(machine, PowerUsage);
            machine.ModifyTag(ProgressTag, state => state.Enable().SetInt(0));
            machine.ModifyTag(TemplateIdTag, state => state.Enable().SetString(""));
            machine.ModifyTag(LastTurnTag, state => state.Enable().SetInt(-1));
            machine.onLoaded = loaded => loaded.EnableTag("CONTAINER_TAG");

            statusText.SetText("0/" + TurnsPerCopy);
            StatusTexts.Add(machine, statusText);
            return machine;
        }

        /// <summary>
        /// Progresses every assembler once per game turn. Called from <see cref="ModEntry.Tick"/>;
        /// store machines are not part of the ship item graph, so their turn-end item callbacks are
        /// not driven by <c>TurnManager</c> and we advance them here instead.
        /// </summary>
        internal static void RuntimeTick()
        {
            try
            {
                GameMaster master = GameMaster.current;
                if (master == null || master.turnManager == null || PlayerStore.Instance == null)
                {
                    return;
                }
                int turn = master.turnManager.turns;
                if (turn == lastProcessedTurn)
                {
                    return;
                }
                lastProcessedTurn = turn;

                foreach (GameItem item in FindAssemblers())
                {
                    if (item != null && item.identifier == ItemId)
                    {
                        Process(item, turn);
                    }
                }
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("molecular assembler tick failed: " + e.Message);
            }
        }

        private static HashSet<GameItem> FindAssemblers()
        {
            var found = new HashSet<GameItem>();
            foreach (GameItem item in PlayerStore.Instance.FindAllItem())
            {
                found.Add(item);
            }
            GameMaster master = GameMaster.current;
            if (master != null && master.shipManager != null && master.shipManager.ship != null)
            {
                foreach (GameItem item in GraphUtils.FindAllChildrenType<GameItem>(master.shipManager.ship))
                {
                    found.Add(item);
                }
            }
            return found;
        }

        private static void Process(GameItem machine, int turn)
        {
            if (machine.GetTagReadonly(LastTurnTag).GetInt() == turn)
            {
                return;
            }
            machine.ModifyTag(LastTurnTag, state => state.SetInt(turn));

            GameSlotInventory batterySlot = MachineHelper.GetBatterySlot(machine);
            GameSlotInventory templateSlot = MachineHelper.GetBodyStorage(machine, 1) as GameSlotInventory;
            GameGridInventory outputGrid = MachineHelper.GetBodyStorage(machine, 2) as GameGridInventory;
            GameGridInventory moduleGrid = MachineHelper.GetBodyStorage(machine, 3) as GameGridInventory;
            if (batterySlot == null || templateSlot == null || outputGrid == null)
            {
                return;
            }

            MachineryText status = null;
            StatusTexts.TryGetValue(machine, out status);

            if (!MachineryHelper.IsMachineryToggleOn(machine))
            {
                SetStatus(status, Labels.Get("asm_off", "Switched off"));
                return;
            }

            GameItem template = templateSlot.childItem;
            if (template == null)
            {
                SetProgress(machine, 0);
                SetStatus(status, MakeProgressText(0, CountModules(moduleGrid), 0));
                return;
            }

            int modules = CountModules(moduleGrid);
            int yield = GetYield(template);

            string identifier = template.identifier;
            if (machine.GetTagReadonly(TemplateIdTag).GetString() != identifier)
            {
                machine.ModifyTag(TemplateIdTag, state => state.SetString(identifier));
                SetProgress(machine, 0);
            }

            int speed = 1 + modules;
            int progress = GetProgress(machine) + speed;
            if (progress < TurnsPerCopy)
            {
                SetProgress(machine, progress);
                SetStatus(status, MakeProgressText(progress, modules, yield));
                return;
            }

            if (batterySlot.childItem == null || !PowerHelper.CanDrawPowerSource(batterySlot.childItem, PowerPerCopy))
            {
                SetProgress(machine, TurnsPerCopy);
                SetStatus(status, Labels.Get("asm_no_power", "No power"));
                return;
            }

            int produced = 0;
            for (int i = 0; i < yield; i++)
            {
                GameItem copy = null;
                try
                {
                    copy = DirectoryMaster.Item(identifier);
                }
                catch (Exception e)
                {
                    ModEntry.Log.Error("assembler could not create '" + identifier + "': " + e.Message);
                    break;
                }
                if (copy == null)
                {
                    break;
                }
                if (!GraphUtils.CanAccept(outputGrid, copy))
                {
                    copy.Destroy();
                    break;
                }
                GraphUtils.TryAcceptAll(outputGrid, copy);
                produced++;
            }

            if (produced == 0)
            {
                SetProgress(machine, TurnsPerCopy);
                SetStatus(status, Labels.Get("asm_blocked", "Output occupied"));
                return;
            }

            PowerHelper.DrawPowerSource(batterySlot.childItem, PowerPerCopy);
            SetProgress(machine, 0);
            SetStatus(status, MakeProgressText(0, modules, yield));
            ModEntry.Log.Log("assembler produced " + produced + " copy/copies of '" + identifier + "'.");
        }

        /// <summary>Cheaper templates come out in bigger batches, expensive ones one at a time.</summary>
        private static int GetYield(GameItem template)
        {
            long value;
            try
            {
                value = template.GetValue();
            }
            catch
            {
                value = template.unitValue;
            }
            if (value < 100L)
            {
                return 4;
            }
            if (value < 500L)
            {
                return 3;
            }
            if (value < 2000L)
            {
                return 2;
            }
            return 1;
        }

        private static int CountModules(GameGridInventory moduleGrid)
        {
            if (moduleGrid == null || moduleGrid.childItems == null)
            {
                return 0;
            }
            return Math.Min(moduleGrid.childItems.Count, ModuleSlots);
        }

        private static string MakeProgressText(int current, int modules, int yield)
        {
            int shown = Math.Min(current, TurnsPerCopy);
            return Labels.Get("asm_progress", "Progress") + ": " + shown + "/" + TurnsPerCopy
                + "  " + Labels.Get("asm_module_short", "Modules") + ": " + modules
                + (yield > 0 ? "  " + Labels.Get("asm_yield_short", "Yield") + ": " + yield : "");
        }

        private static bool CanBeTemplate(GameItem item)
        {
            if (item == null)
            {
                return false;
            }
            try
            {
                if (item is GameCharacterItem)
                {
                    return false;
                }
                if (item.IsTag("IMPORTANT_TAG") || item.IsGameItemType("MACHINE"))
                {
                    return false;
                }
                PixelWindow window = item.contentWindow;
                if (window != null)
                {
                    GameInventory inner = window.child as GameInventory;
                    if (inner != null && inner.childItems != null && inner.childItems.Count > 0)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static int GetProgress(GameItem machine)
        {
            return machine.GetTagReadonly(ProgressTag).GetInt();
        }

        private static void SetProgress(GameItem machine, int value)
        {
            machine.ModifyTag(ProgressTag, state => state.SetInt(value));
        }

        private static void SetStatus(MachineryText text, string value)
        {
            if (text == null)
            {
                return;
            }
            try
            {
                text.SetText(value);
            }
            catch
            {
            }
        }

        /// <summary>Buys one assembler for <see cref="PurchaseCost"/> credits (F9 menu entry).</summary>
        internal static void BuyFromMenu()
        {
            try
            {
                PlayerStore store = PlayerStore.Instance;
                EmporiumEntry emporium = EmporiumEntry.Instance;
                if (store == null || emporium == null)
                {
                    ModEntry.Log.Log("buy: the store is not loaded yet.");
                    return;
                }
                if (store.playerCash < PurchaseCost)
                {
                    ModEntry.Log.Log("buy: not enough credits (" + store.playerCash + "/" + PurchaseCost + ").");
                    return;
                }
                GameItem item = DirectoryMaster.Item(ItemId);
                if (item == null)
                {
                    ModEntry.Log.Error("molecular assembler could not be created (directory missing).");
                    return;
                }
                int leftover = GraphUtils.TryAcceptAll(emporium.invElement, item);
                if (leftover > 0)
                {
                    item.Destroy();
                    ModEntry.Log.Log("buy: inventory is full.");
                    return;
                }
                store.playerCash -= PurchaseCost;
                if (EmporiumAudioPlayer.Instance != null)
                {
                    EmporiumAudioPlayer.Instance.PlayRegister();
                }
                ModEntry.Log.Log("bought a molecular assembly room for " + PurchaseCost + " credits.");
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("buying the molecular assembler failed: " + e);
            }
        }
    }
}
