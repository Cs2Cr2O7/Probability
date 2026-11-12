using System;
using System.Collections.Generic;
using UnityEngine;

namespace Probability
{
    /// <summary>
    /// Registers the "Super Charm Perfume" item. The game builds items from directories
    /// (<see cref="Directory{T}"/>) that are registered in <see cref="DirectoryMaster"/>, so the
    /// mod brings its own instance and adds the item factory to it. Once registered,
    /// <c>DirectoryMaster.Item("super_charm_perfume")</c> works like any built-in item.
    /// </summary>
    internal sealed class CharmPerfumeDirectory : ItemDirectory
    {
        internal const string ItemId = "super_charm_perfume";

        private static CharmPerfumeDirectory instance;

        internal static void EnsureRegistered()
        {
            if (instance != null)
            {
                return;
            }

            var host = new GameObject("Probability.ItemDirectory");
            UnityEngine.Object.DontDestroyOnLoad(host);
            instance = host.AddComponent<CharmPerfumeDirectory>();

            // Awake (which normally initializes the dictionary and registers the directory) is
            // a private method on the base class; if Unity did not run it, do it ourselves.
            if (instance.factoryDictionary == null)
            {
                instance.factoryDictionary = new Dictionary<string, Func<GameItem>>();
                DirectoryMaster.AddDirectory(instance);
            }
            instance.factoryDictionary[ItemId] = CreatePerfume;
            ModEntry.Log.Log("charm perfume item registered as '" + ItemId + "'.");
        }

        protected override void InitDirectory()
        {
            Add(ItemId, CreatePerfume);
        }

        internal static GameItem CreatePerfume()
        {
            GameItem item = ItemDirectory.CreateEmptyItem();
            item.SetSpriteAndShape("Items/items_amenities", "perfume2");
            item.name = Labels.Get("charm_name", "Super Charm Perfume");
            item.SetValue(800L);
            item.SetGameItemType("LUXURY_ITEM");
            item.EnableManufacturer("solana");
            item.flavorText = Labels.Get("charm_flavor", "Active ingredient extracted from succubus secretions.");
            item.shortDescription = Labels.Get("charm_desc", "Spray it on yourself: the current customer stops resisting your price and ignores forged labels.");
            AudioHelper.InitGlass(item);
            UseCountHelper.InitUseCountItem(item, 3, transferable: true, destroyOnEmpty: true, useAutoValue: true, 0, 40);

            item.canActivateSlotItemFunc = CanUse;
            item.mayActivateSlotItemFunc = MayUse;
            item.onActivateSlotItemFunc = Use;
            return item;
        }

        private static bool CanUse(GameItem item, GameInventory inventory, SlotMarker marker)
        {
            return GeneralHelper.IsItemOwned(item)
                && PlayerStore.Instance != null
                && PlayerStore.Instance.currentClientInstance != null
                && UseCountHelper.CanUse(item);
        }

        private static bool MayUse(GameItem item, GameInventory inventory, SlotMarker marker)
        {
            return GeneralHelper.IsItemOwned(item)
                && PlayerStore.Instance != null
                && PlayerStore.Instance.currentClientInstance != null;
        }

        private static void Use(GameItem item, GameInventory inventory, SlotMarker marker)
        {
            try
            {
                UseCountHelper.Use(item);
                CharmEffect.Activate();
                PlayerStore.Instance.RefreshCounterItem();
                AudioManager.Instance.Play2("perfume_spray");
                ModEntry.Log.Log("charm perfume sprayed on the current customer.");
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("charm perfume activation failed: " + e);
            }
        }

        /// <summary>Puts one perfume into the player inventory (F9 debug button).</summary>
        internal static void GiveToPlayer()
        {
            try
            {
                GameItem item = DirectoryMaster.Item(ItemId);
                if (item == null)
                {
                    ModEntry.Log.Error("charm perfume could not be created (directory missing).");
                    return;
                }
                EmporiumEntry emporium = EmporiumEntry.Instance;
                if (emporium == null)
                {
                    ModEntry.Log.Error("debug: the store is not loaded yet.");
                    return;
                }
                GraphUtils.EmporiumTryAdd(emporium.invElement, item);
                ModEntry.Log.Log("debug: gave one super charm perfume.");
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("giving the charm perfume failed: " + e);
            }
        }
    }
}
