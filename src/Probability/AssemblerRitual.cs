using System;
using System.Collections.Generic;

namespace Probability
{
    /// <summary>
    /// The (intentionally silly) way to obtain the Molecular Assembly Room: leave one 3D printer,
    /// three Super Charm Perfumes, one uncapped neural core and twenty pieces of scrap metal in the
    /// store and go to sleep. In the morning the machine has assembled itself.
    /// <para>
    /// The recipe is hinted in the tooltips of the printer and the perfume, so players can find it
    /// without reading the mod source.
    /// </para>
    /// </summary>
    internal static class AssemblerRitual
    {
        private const string PrinterId = "printer";
        private const string NeuralCoreId = "system_uncapped_neural_core";
        private const string ScrapMetalId = "scrap_metal";

        private const int PerfumeNeeded = 3;
        private const int ScrapMetalNeeded = 20;

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
                    || !Contains(owned, CharmPerfumeDirectory.ItemId, PerfumeNeeded)
                    || !Contains(owned, NeuralCoreId, 1)
                    || !Contains(owned, ScrapMetalId, ScrapMetalNeeded))
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
                Consume(owned, CharmPerfumeDirectory.ItemId, PerfumeNeeded);
                Consume(owned, NeuralCoreId, 1);
                Consume(owned, ScrapMetalId, ScrapMetalNeeded);

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
                            "Rumor: leave this printer, three Super Charm Perfumes, an uncapped neural core and twenty pieces of scrap metal in the store overnight..."),
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
