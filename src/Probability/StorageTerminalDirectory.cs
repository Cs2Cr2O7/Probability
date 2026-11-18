using System;
using System.Collections.Generic;
using UnityEngine;

namespace Probability
{
    /// <summary>
    /// Registers the storage terminal machine and its five hard-drive tiers. A directory is the
    /// game's item factory registry: once registered, <c>DirectoryMaster.Item(id)</c> works like a
    /// built-in item and the F9 item spawner lists the group automatically.
    /// </summary>
    internal sealed class StorageTerminalDirectory : ItemDirectory
    {
        private static StorageTerminalDirectory instance;

        internal static void EnsureRegistered()
        {
            if (instance != null)
            {
                return;
            }

            var host = new GameObject("Probability.StorageItems");
            UnityEngine.Object.DontDestroyOnLoad(host);
            instance = host.AddComponent<StorageTerminalDirectory>();

            if (instance.factoryDictionary == null)
            {
                instance.factoryDictionary = new Dictionary<string, Func<GameItem>>();
                DirectoryMaster.AddDirectory(instance);
            }

            RegisterAll(instance);
            ModEntry.Log.Log("storage terminal items registered.");
        }

        protected override void InitDirectory()
        {
            RegisterAll(this);
        }

        private static void RegisterAll(ItemDirectory directory)
        {
            directory.factoryDictionary[StorageTerminal.ItemId] = StorageTerminal.Create;
            for (int i = 0; i < StorageTerminal.DriveIds.Length; i++)
            {
                int tier = i;
                directory.factoryDictionary[StorageTerminal.DriveIds[i]] = () => CreateDrive(tier);
            }
        }

        private static GameItem CreateDrive(int tier)
        {
            int index = Mathf.Clamp(tier, 0, StorageTerminal.DriveCapacities.Length - 1);
            string id = StorageTerminal.DriveIds[index];
            int capacity = StorageTerminal.DriveCapacities[index];
            GameItem item = ItemDirectory.CreateEmptyItem()
                .SetSpriteAndShape("Items/items_module", "aug_chip");
            item.name = Labels.Get("storage_drive_mk" + (index + 1) + "_name",
                "Storage Drive MK " + (index + 1));
            item.unitValue = StorageTerminal.DriveValues[index];
            item.SetGameItemType("TOOL");
            item.shortDescription = Labels.Get("storage_drive_desc",
                "A storage drive for the storage terminal. Capacity: ") + capacity + " items.";
            item.flavorText = Labels.Get("storage_drive_flavor",
                "Spin-up whine, then the shelves appear.");
            item.EnableTag(StorageTerminal.DriveTag);
            item.ModifyTag(StorageTerminal.CapacityTag, state => state.Enable().SetInt(capacity));
            item.ModifyTag(StorageTerminal.TierTag, state => state.Enable().SetInt(index + 1));
            item.onLoaded = loaded =>
            {
                loaded.EnableTag(StorageTerminal.DriveTag);
                if (!loaded.IsTag(StorageTerminal.CapacityTag))
                {
                    loaded.ModifyTag(StorageTerminal.CapacityTag, state => state.Enable().SetInt(capacity));
                }
                if (!loaded.IsTag(StorageTerminal.TierTag))
                {
                    loaded.ModifyTag(StorageTerminal.TierTag, state => state.Enable().SetInt(index + 1));
                }
            };
            return item;
        }

        /// <summary>F9 helper: one storage terminal.</summary>
        internal static void GiveMachine()
        {
            try
            {
                EmporiumEntry emporium = EmporiumEntry.Instance;
                if (emporium == null)
                {
                    ModEntry.Log.Error("debug: the store is not loaded yet.");
                    return;
                }
                GameItem machine = DirectoryMaster.Item(StorageTerminal.ItemId);
                if (machine == null)
                {
                    ModEntry.Log.Error("storage terminal could not be created (directory missing).");
                    return;
                }
                GraphUtils.EmporiumTryAdd(emporium.invElement, machine);
                ModEntry.Log.Log("debug: gave one storage terminal.");
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("giving the storage terminal failed: " + e);
            }
        }

        /// <summary>F9 helper: one drive of every tier.</summary>
        internal static void GiveDriveSet()
        {
            try
            {
                EmporiumEntry emporium = EmporiumEntry.Instance;
                if (emporium == null)
                {
                    ModEntry.Log.Error("debug: the store is not loaded yet.");
                    return;
                }
                for (int i = 0; i < StorageTerminal.DriveIds.Length; i++)
                {
                    GameItem drive = DirectoryMaster.Item(StorageTerminal.DriveIds[i]);
                    if (drive != null)
                    {
                        GraphUtils.EmporiumTryAdd(emporium.invElement, drive);
                    }
                }
                ModEntry.Log.Log("debug: gave the storage drive set.");
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("giving storage drives failed: " + e);
            }
        }
    }
}
