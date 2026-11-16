using System;
using System.Collections.Generic;

namespace Probability
{
    /// <summary>
    /// The (intentionally silly) way to obtain the Molecular Assembly Room: leave a 3D printer plus
    /// a pile of electronics and scrap in the store and go to sleep. In the morning the machine has
    /// assembled itself. The recipe is hinted in the printer tooltip.
    /// </summary>
    internal static class AssemblerRitual
    {
        private const string PrinterId = "printer";
        private const string NeuralCoreId = "system_uncapped_neural_core";
        private const string HypertechElectronicId = "hypertech_electronic";
        private const string PrinterPlasticId = "printer_plastic";
        private const string CommonElectronicId = "common_electronic";
        private const string WireId = "wire";

        private const int HypertechElectronicNeeded = 6;
        private const int PrinterPlasticNeeded = 10;
        private const int CommonElectronicNeeded = 10;
        private const int WireNeeded = 5;

        private static bool subscribed;

        internal static void Subscribe()
        {
            if (subscribed)
            {
                return;
            }
            ModHook.OnGoingSleepEarly += HandleSleep;
            ModHook.OnCreateTooltipLate += HandleTooltip;
            subscribed = true;
        }

        internal static void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }
            ModHook.OnGoingSleepEarly -= HandleSleep;
            ModHook.OnCreateTooltipLate -= HandleTooltip;
            subscribed = false;
        }

        private static void HandleSleep()
        {
            try
            {
                PlayerStore store = PlayerStore.Instance;
                EmporiumEntry emporium = EmporiumEntry.Instance;
                if (store == null || emporium == null)
                {
                    return;
                }

                List<GameItem> owned = CollectOwnedItems(store);
                if (Contains(owned, MolecularAssembler.ItemId, 1))
                {
                    return;
                }
                if (!Contains(owned, PrinterId, 1)
                    || !Contains(owned, NeuralCoreId, 1)
                    || !Contains(owned, HypertechElectronicId, HypertechElectronicNeeded)
                    || !Contains(owned, PrinterPlasticId, PrinterPlasticNeeded)
                    || !Contains(owned, CommonElectronicId, CommonElectronicNeeded)
                    || !Contains(owned, WireId, WireNeeded))
                {
                    return;
                }

                GameItem machine = DirectoryMaster.Item(MolecularAssembler.ItemId);
                if (machine == null)
                {
                    ModEntry.Log.Error("ritual: could not create the molecular assembler.");
                    return;
                }

                int leftover = GraphUtils.TryAcceptAll(emporium.invElement, machine);
                if (leftover > 0)
                {
                    machine.Destroy();
                    ModEntry.Log.Log("ritual: not enough room in the inventory, the ingredients stay untouched.");
                    return;
                }

                Consume(owned, PrinterId, 1);
                Consume(owned, NeuralCoreId, 1);
                Consume(owned, HypertechElectronicId, HypertechElectronicNeeded);
                Consume(owned, PrinterPlasticId, PrinterPlasticNeeded);
                Consume(owned, CommonElectronicId, CommonElectronicNeeded);
                Consume(owned, WireId, WireNeeded);

                if (EmporiumAudioPlayer.Instance != null)
                {
                    EmporiumAudioPlayer.Instance.PlayRegister();
                }
                ModEntry.Log.Log("ritual complete: the molecular assembly room has assembled itself overnight.");
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("assembler ritual failed: " + e);
            }
        }

        private static void HandleTooltip(RichTextBuilder builder, GameItem item)
        {
            try
            {
                if (builder == null || item == null)
                {
                    return;
                }
                if (item.identifier == PrinterId)
                {
                    builder.AddLine(
                        Labels.Get("asm_hint_printer",
                            "Rumor: leave this printer, an uncapped neural core, 6 hypertech electronics, 10 printer plastic, 10 common electronics and 5 wire in the store overnight..."),
                        color: RenderHandler.ColorPalette.Gray,
                        italic: true);
                }

            }
            catch
            {
            }
        }

        private static List<GameItem> CollectOwnedItems(PlayerStore store)
        {
            var found = new List<GameItem>();
            foreach (GameItem item in store.FindAllItem())
            {
                if (item != null && GeneralHelper.IsItemOwned(item))
                {
                    found.Add(item);
                }
            }
            GameMaster master = GameMaster.current;
            if (master != null && master.shipManager != null && master.shipManager.ship != null)
            {
                foreach (GameItem item in GraphUtils.FindAllChildrenType<GameItem>(master.shipManager.ship))
                {
                    if (item != null && GeneralHelper.IsItemOwned(item))
                    {
                        found.Add(item);
                    }
                }
            }
            return found;
        }

        private static bool Contains(List<GameItem> items, string identifier, int count)
        {
            int found = 0;
            foreach (GameItem item in items)
            {
                if (item.identifier == identifier)
                {
                    found++;
                    if (found >= count)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static void Consume(List<GameItem> items, string identifier, int count)
        {
            int consumed = 0;
            foreach (GameItem item in items)
            {
                if (consumed >= count)
                {
                    return;
                }
                if (item.identifier == identifier)
                {
                    consumed++;
                    try
                    {
                        item.Destroy();
                    }
                    catch
                    {
                        // The item may already have been destroyed together with its container.
                    }
                }
            }
        }
    }
}
