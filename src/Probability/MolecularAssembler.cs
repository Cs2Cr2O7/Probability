using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Probability
{
    /// <summary>
    /// The "Molecular Assembly Room" machine: a battery-powered machine with a template slot and an
    /// output slot. Every few game turns it prints a fresh copy of whatever sits in the template
    /// slot, drawing energy from the battery in the power slot.
    /// <para>
    /// The copy is created through <c>DirectoryMaster.Item(identifier)</c>, so it is a plain new item
    /// of the same kind: contents (liquids, inserted cards, modules) are not duplicated, which also
    /// keeps containers from being cloned with their contents.
    /// </para>
    /// </summary>
    internal static class MolecularAssembler
    {
        internal const string ItemId = "molecular_assembler";

        private const string TemplateIdTag = "PROBABILITY_ASSEMBLER_TEMPLATE_ID_TAG";
        private const string ProgressTag = "PROBABILITY_ASSEMBLER_PROGRESS_TAG";
        private const string LastTurnTag = "PROBABILITY_ASSEMBLER_LAST_TURN_TAG";

        /// <summary>Game turns needed for one copy (one turn = 20 in-game minutes).</summary>
        private const int TurnsPerCopy = 6;

        /// <summary>Energy drawn from the battery when a copy is produced.</summary>
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
            var outputSlot = new GameSlotInventory();
            var statusText = new MachineryText();

            PixelWindow contentWindow = MachineHelper.CreateMachineryWindow(
                true,
                MachineHelper.PowerPanel(batterySlot),
                MachineHelper.Panel(Labels.Get("asm_template", "Template"), templateSlot, "template"),
                MachineHelper.Panel(Labels.Get("asm_output", "Output"), outputSlot, "output"),
                MachineHelper.Panel(Labels.Get("asm_progress", "Progress"), statusText, "progress"));

            templateSlot.mayInventoryAddItemFunc = (item, inventory) =>
                GeneralHelper.IsItemOwned(item) && CanBeTemplate(item);
            outputSlot.mayInventoryAddItemFunc = (item, inventory) => GeneralHelper.IsItemOwned(item);

            GameItem machine = ItemDirectory.CreateEmptyItem()
                .SetContentWindow(contentWindow)
                .SetSpriteAndShape("Items/items_tool2", "printer_off");
            machine.name = Labels.Get("asm_name", "Molecular Assembly Room");
            machine.flavorText = Labels.Get("asm_flavor", "Feed it a template and it feeds you copies.");
            machine.shortDescription = Labels.Get("asm_desc",
                "Install a battery, put an item in the template slot, and wait: every few turns the output slot receives a fresh copy.");
            machine.unitValue = 750L;
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
            GameSlotInventory outputSlot = MachineHelper.GetBodyStorage(machine, 2) as GameSlotInventory;
            if (batterySlot == null || templateSlot == null || outputSlot == null)
            {
                return;
            }

            MachineryText status = null;
            StatusTexts.TryGetValue(machine, out status);

            if (!MachineryHelper.IsMachineryToggleOn(machine))
            {
                Show(status, Labels.Get("asm_off", "Switched off"));
                return;
            }

            GameItem template = templateSlot.childItem;
            if (template == null)
            {
                SetProgress(machine, 0);
                Show(status, "0/" + TurnsPerCopy);
                return;
            }

            string identifier = template.identifier;
            if (machine.GetTagReadonly(TemplateIdTag).GetString() != identifier)
            {
                machine.ModifyTag(TemplateIdTag, state => state.SetString(identifier));
                SetProgress(machine, 0);
            }

            if (outputSlot.childItem != null)
            {
                Show(status, Labels.Get("asm_blocked", "Output occupied"));
                return;
            }

            int progress = GetProgress(machine) + 1;
            if (progress < TurnsPerCopy)
            {
                SetProgress(machine, progress);
                Show(status, progress + "/" + TurnsPerCopy);
                return;
            }

            if (batterySlot.childItem == null || !PowerHelper.CanDrawPowerSource(batterySlot.childItem, PowerPerCopy))
            {
                SetProgress(machine, TurnsPerCopy);
                Show(status, Labels.Get("asm_no_power", "No power"));
                return;
            }

            GameItem copy = null;
            try
            {
                copy = DirectoryMaster.Item(identifier);
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("assembler could not create '" + identifier + "': " + e.Message);
            }

            if (copy == null)
            {
                SetProgress(machine, TurnsPerCopy);
                Show(status, Labels.Get("asm_failed", "Template failed"));
                return;
            }

            if (!GraphUtils.CanAccept(outputSlot, copy))
            {
                copy.Destroy();
                SetProgress(machine, TurnsPerCopy);
                Show(status, Labels.Get("asm_blocked", "Output occupied"));
                return;
            }

            GraphUtils.TryAcceptAll(outputSlot, copy);
            PowerHelper.DrawPowerSource(batterySlot.childItem, PowerPerCopy);
            SetProgress(machine, 0);
            Show(status, "0/" + TurnsPerCopy);
            ModEntry.Log.Log("assembler produced a copy of '" + identifier + "'.");
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

        private static void Show(MachineryText text, string value)
        {
            if (text == null)
            {
                return;
            }
            try
            {
                text.SetStatus(Labels.Get("asm_progress", "Progress"), value);
            }
            catch
            {
            }
        }

        /// <summary>Puts one assembler into the player inventory (F9 debug button).</summary>
        internal static void GiveToPlayer()
        {
            try
            {
                GameItem item = DirectoryMaster.Item(ItemId);
                if (item == null)
                {
                    ModEntry.Log.Error("molecular assembler could not be created (directory missing).");
                    return;
                }
                EmporiumEntry emporium = EmporiumEntry.Instance;
                if (emporium == null)
                {
                    ModEntry.Log.Error("debug: the store is not loaded yet.");
                    return;
                }
                GraphUtils.EmporiumTryAdd(emporium.invElement, item);
                ModEntry.Log.Log("debug: gave one molecular assembly room.");
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("giving the molecular assembler failed: " + e);
            }
        }
    }
}
