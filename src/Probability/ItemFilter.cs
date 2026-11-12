using System;
using System.Collections.Generic;
using UnityEngine;

namespace Probability
{
    /// <summary>
    /// Reads the game item taxonomy (GameItem.itemTypes: FIREARM, FOOD, TOOL, ...), scans every
    /// container the shop/player owns (not just what is on screen) and dims the visible items
    /// that do not match the selected type. Containers that hold a matching item stay bright, so
    /// the player can see which box/backpack to open.
    /// </summary>
    internal static class ItemFilter
    {
        internal const string Unclassified = "__UNCLASSIFIED__";
        internal const float DimAlpha = 0.25f;

        /// <summary>
        /// Every type the game actually assigns to items (collected from the SetGameItemType
        /// calls in the game's item directories). The filter always offers all of them, even
        /// when nothing carries the type, so the category list never looks incomplete.
        /// </summary>
        internal static readonly string[] AllTypes =
        {
            "FOOD", "MEDICAL", "MATERIAL", "HOUSEHOLD_GOOD", "SUBSTANCE", "WEAPON", "WATER", "ARMOR",
            "TOOL", "MODULE", "AMMUNITION", "BREVAGE", "ALCOHOL", "WATER_RATION", "WATER_PURIFICATION_SUPPLY",
            "SEED", "MODIFIED_SEED", "FARMING_SUPPLIES", "CHEMICAL_SUPPLIES", "PROCESSED_FOOD", "TREAT",
            "BASIC_MATERIAL", "ENERGY_CELL", "NARCOTIC", "POISON", "FILTER", "STORAGE", "CONTRABAND",
            "ACCESS_CARD", "DOCUMENT", "JUNK", "LUXURY_ITEM", "ART", "LOTTERY", "MACHINE", "MAKESHIFT",
            "MIDLINE", "LOWTECH", "MELEE_WEAPON", "FIREARM", "GUN_MOD", "MUNITION", "MODDABLE",
            "HYDROPONIC_MODULE", "ALARM_MODULE", "PRINTER_MODULE", "EVAPORATOR_MODULE", "NODE", "ORGAN",
            "SUPPLY_CRATE", "MEDICAL_TOOL",
        };

        /// <summary>Currently selected item type, or null when the filter is off.</summary>
        internal static string SelectedType { get; private set; }

        internal static bool IsActive => SelectedType != null;

        internal static Dictionary<string, int> Counts => counts;
        internal static int Total => totalItems;

        private static readonly Dictionary<GameObject, float> originalAlphas = new Dictionary<GameObject, float>();

        /// <summary>Items discovered by the last full scan (visible or not).</summary>
        private static readonly List<GameItem> allItems = new List<GameItem>();
        private static readonly Dictionary<GameItem, GameItem> parents = new Dictionary<GameItem, GameItem>();
        private static readonly HashSet<GameItem> containersWithMatch = new HashSet<GameItem>();
        private static Dictionary<string, int> counts = new Dictionary<string, int>();
        private static int totalItems;
        private static int lastScanFrame = -1000;
        private static bool liquidReadWarned;

        // ---------------------------------------------------------------- scanning

        /// <summary>
        /// Rebuilds the item index from every container the store owns plus the open windows.
        /// Returns the types that currently match the selected type (empty when off).
        /// </summary>
        internal static void Rescan(bool force = false)
        {
            if (!force && Time.frameCount - lastScanFrame < 120)
            {
                return;
            }
            lastScanFrame = Time.frameCount;

            try
            {
                allItems.Clear();
                parents.Clear();
                containersWithMatch.Clear();

                var seen = new HashSet<GameItem>();
                foreach (GameInventory inventory in CollectRootInventories())
                {
                    List<GameItem> children;
                    try
                    {
                        children = inventory.childItems;
                    }
                    catch (Exception e)
                    {
                        ModEntry.Log.Error("childItems failed: " + e.Message);
                        continue;
                    }
                    if (children == null)
                    {
                        continue;
                    }
                    foreach (GameItem child in children)
                    {
                        Visit(child, null, seen);
                    }
                }

                totalItems = allItems.Count;
                counts = CountTypes(allItems);

                string selected = SelectedType;
                if (selected != null)
                {
                    foreach (GameItem item in allItems)
                    {
                        if (MatchesType(item, selected))
                        {
                            MarkAncestors(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("scan failed: " + e);
            }
        }

        private static void Visit(GameItem item, GameItem parent, HashSet<GameItem> seen)
        {
            if (item == null || !seen.Add(item))
            {
                return;
            }
            allItems.Add(item);
            if (parent != null)
            {
                parents[item] = parent;
            }

            GameInventory content = GetContent(item);
            if (content == null)
            {
                return;
            }
            List<GameItem> children;
            try
            {
                children = content.childItems;
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("container contents failed for " + SafeName(item) + ": " + e.Message);
                return;
            }
            if (children == null)
            {
                return;
            }
            foreach (GameItem child in children)
            {
                Visit(child, item, seen);
            }
        }

        /// <summary>The inventory behind an item's container window, or null when it is not a container.</summary>
        private static GameInventory GetContent(GameItem item)
        {
            try
            {
                PixelWindow window = item.contentWindow;
                return window != null ? window.child as GameInventory : null;
            }
            catch
            {
                return null;
            }
        }

        private static void MarkAncestors(GameItem item)
        {
            GameItem current = item;
            int guard = 0;
            while (parents.TryGetValue(current, out GameItem parent) && guard++ < 64)
            {
                containersWithMatch.Add(parent);
                current = parent;
            }
        }

        /// <summary>All inventories that can hold items: store panels, store grid and open windows.</summary>
        private static List<GameInventory> CollectRootInventories()
        {
            var roots = new List<GameInventory>();
            try
            {
                EmporiumEntry emporium = EmporiumEntry.Instance;
                if (emporium != null)
                {
                    AddRoot(roots, emporium.invElement);
                    AddRoot(roots, emporium.backInvinvElement);
                    AddRoot(roots, emporium.backInvinvElementCounter);
                    AddRoot(roots, emporium.frontInvinvElement);
                    AddRoot(roots, emporium.showcaseElement);
                    AddRoot(roots, emporium.responseInventory);
                    AddRoot(roots, emporium.responseInventoryClosable);
                    AddRoot(roots, emporium.afterhourInventory);
                    AddRoot(roots, emporium.hirelingInv);
                }

                PlayerStore store = PlayerStore.Instance;
                if (store != null)
                {
                    AddRoot(roots, store.GetStoreInventory());
                }
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("store inventories unavailable: " + e.Message);
            }

            foreach (GameInventory visible in GetVisibleInventories())
            {
                AddRoot(roots, visible);
            }
            return roots;
        }

        private static void AddRoot(List<GameInventory> roots, GameInventory inventory)
        {
            if (inventory != null && !roots.Contains(inventory))
            {
                roots.Add(inventory);
            }
        }

        /// <summary>Inventories behind the container / inventory windows that are currently open.</summary>
        internal static List<GameInventory> GetVisibleInventories()
        {
            var result = new List<GameInventory>();
            try
            {
                WindowsHandler handler = WindowsHandler.current;
                List<PixelWindow> windows = handler != null ? handler.visibleWindows : null;
                if (windows == null)
                {
                    return result;
                }
                foreach (PixelWindow window in windows)
                {
                    if (window == null)
                    {
                        continue;
                    }
                    if (window.childElement is GameInventory inventory && !result.Contains(inventory))
                    {
                        result.Add(inventory);
                    }
                }
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("GetVisibleInventories failed: " + e.Message);
            }
            return result;
        }

        /// <summary>Every visible item in an open container / inventory window.</summary>
        internal static List<GameItemElement> ScanVisibleItems()
        {
            var result = new List<GameItemElement>();
            var seen = new HashSet<GameItemElement>();

            foreach (GameInventory inventory in GetVisibleInventories())
            {
                List<GameItem> children;
                try
                {
                    children = inventory.childItems;
                }
                catch (Exception e)
                {
                    ModEntry.Log.Error("childItems failed: " + e.Message);
                    continue;
                }
                if (children == null)
                {
                    continue;
                }
                foreach (GameItem child in children)
                {
                    if (child is GameItemElement item && item.handler != null && seen.Add(item))
                    {
                        result.Add(item);
                    }
                }
            }

            if (result.Count == 0)
            {
                ScanSceneForItems(result, seen);
            }
            return result;
        }

        private static void ScanSceneForItems(List<GameItemElement> result, HashSet<GameItemElement> seen)
        {
            foreach (TreeNodeRender node in UnityEngine.Object.FindObjectsOfType<TreeNodeRender>())
            {
                if (node == null)
                {
                    continue;
                }
                if (node.element is GameItemElement item && item.handler != null && seen.Add(item))
                {
                    result.Add(item);
                }
            }
        }

        // ---------------------------------------------------------------- matching

        internal static bool Matches(GameItemElement item)
        {
            string selected = SelectedType;
            return selected == null || MatchesType(item, selected);
        }

        private static bool MatchesType(GameItem item, string selected)
        {
            List<string> types = GetTypes(item);
            if (selected == Unclassified)
            {
                return types.Count == 0;
            }
            return types.Contains(selected);
        }

        /// <summary>True when the item is a container that holds at least one matching item.</summary>
        internal static bool IsMatchingContainer(GameItem item)
        {
            return containersWithMatch.Contains(item);
        }

        internal static List<string> GetTypes(GameItem item)
        {
            var types = new List<string>();
            try
            {
                List<string> raw = item.GetGameItemType();
                if (raw != null)
                {
                    foreach (string type in raw)
                    {
                        if (!string.IsNullOrEmpty(type) && !types.Contains(type))
                        {
                            types.Add(type);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("GetGameItemType failed for " + SafeName(item) + ": " + e.Message);
            }

            AddLiquidTypes(item, types);
            return types;
        }

        /// <summary>
        /// Liquid containers (water/beer bottles, chemical bottles) carry no item type for
        /// their contents — the liquid lives in tags. Without this, "水"/"酒精" would never
        /// match the bottles the player is actually looking at.
        /// </summary>
        private static void AddLiquidTypes(GameItem item, List<string> types)
        {
            try
            {
                if (!item.IsTag("LIQUID_CONTAINER_TAG"))
                {
                    return;
                }
                int[] parts = WaterHelper.GetLiquidParts(item);
                if (parts == null)
                {
                    return;
                }
                int count = Math.Min(parts.Length, Liquid.Liquids.Count);
                for (int i = 0; i < count; i++)
                {
                    if (parts[i] <= 0)
                    {
                        continue;
                    }
                    string liquidId = Liquid.Liquids[i].identifier;
                    if (liquidId == "water")
                    {
                        AddType(types, "WATER");
                    }
                    else if (liquidId == "alcohol")
                    {
                        AddType(types, "ALCOHOL");
                    }
                }
            }
            catch (Exception e)
            {
                if (!liquidReadWarned)
                {
                    liquidReadWarned = true;
                    ModEntry.Log.Error("liquid content read failed, bottles may not match WATER/ALCOHOL: " + e.Message);
                }
            }
        }

        private static void AddType(List<string> types, string type)
        {
            if (!types.Contains(type))
            {
                types.Add(type);
            }
        }

        internal static string SafeName(GameItem item)
        {
            try
            {
                return item.GetDisplayName() ?? item.identifier ?? "?";
            }
            catch
            {
                return item?.identifier ?? "?";
            }
        }

        // ---------------------------------------------------------------- filter state

        internal static void Select(string type)
        {
            SelectedType = type;
            Rescan(force: true);
            Apply();
        }

        internal static void ClearSelection()
        {
            SelectedType = null;
            containersWithMatch.Clear();
            Restore();
        }

        /// <summary>Applies the current filter to every visible item (no-op when off).</summary>
        internal static void Apply()
        {
            if (!IsActive)
            {
                return;
            }

            Rescan();

            try
            {
                foreach (GameItemElement item in ScanVisibleItems())
                {
                    GameObject go = item.handler != null ? item.handler.gameObject : null;
                    if (go == null)
                    {
                        continue;
                    }

                    CanvasGroup group = go.GetComponent<CanvasGroup>();
                    if (group == null)
                    {
                        group = go.AddComponent<CanvasGroup>();
                    }
                    if (!originalAlphas.ContainsKey(go))
                    {
                        originalAlphas[go] = group.alpha;
                    }
                    bool bright = Matches(item) || IsMatchingContainer(item);
                    group.alpha = bright ? originalAlphas[go] : DimAlpha;
                }
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("Apply failed: " + e);
            }
        }

        /// <summary>Restores the alpha of every item we touched.</summary>
        internal static void Restore()
        {
            foreach (KeyValuePair<GameObject, float> pair in originalAlphas)
            {
                try
                {
                    GameObject go = pair.Key;
                    if (go == null)
                    {
                        continue;
                    }
                    CanvasGroup group = go.GetComponent<CanvasGroup>();
                    if (group != null)
                    {
                        group.alpha = pair.Value;
                    }
                }
                catch (Exception e)
                {
                    ModEntry.Log.Error("Restore failed: " + e.Message);
                }
            }
            originalAlphas.Clear();
        }

        // ---------------------------------------------------------------- counting

        private static Dictionary<string, int> CountTypes(List<GameItem> items)
        {
            var result = new Dictionary<string, int>();
            foreach (GameItem item in items)
            {
                List<string> types = GetTypes(item);
                if (types.Count == 0)
                {
                    result[Unclassified] = result.TryGetValue(Unclassified, out int none) ? none + 1 : 1;
                    continue;
                }
                foreach (string type in types)
                {
                    result[type] = result.TryGetValue(type, out int current) ? current + 1 : 1;
                }
            }
            return result;
        }
    }
}
