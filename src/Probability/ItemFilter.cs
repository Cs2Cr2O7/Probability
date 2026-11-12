using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Probability
{
    /// <summary>
    /// Reads the game item taxonomy (GameItem.itemTypes: FIREARM, FOOD, TOOL, ...),
    /// counts what is currently on screen and dims the items that do not match.
    /// </summary>
    internal static class ItemFilter
    {
        internal const string Unclassified = "__UNCLASSIFIED__";
        internal const float DimAlpha = 0.25f;

        /// <summary>Currently selected item type, or null when the filter is off.</summary>
        internal static string SelectedType { get; private set; }

        internal static bool IsActive => SelectedType != null;

        private static readonly Dictionary<GameObject, float> originalAlphas = new Dictionary<GameObject, float>();

        /// <summary>
        /// Every item visible in an open container / inventory window. Falls back to a
        /// scene-wide scan when no inventory window is registered yet.
        /// </summary>
        internal static List<GameItemElement> ScanVisibleItems()
        {
            var result = new List<GameItemElement>();
            var seen = new HashSet<GameItemElement>();

            List<GameInventory> inventories = GetVisibleInventories();
            foreach (GameInventory inventory in inventories)
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

            if (inventories.Count == 0)
            {
                ScanSceneForItems(result, seen);
            }
            return result;
        }

        /// <summary>Inventories behind the container / inventory windows that are currently open.</summary>
        private static List<GameInventory> GetVisibleInventories()
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

        internal static bool Matches(GameItemElement item)
        {
            string selected = SelectedType;
            if (selected == null)
            {
                return true;
            }

            List<string> types = GetTypes(item);
            if (selected == Unclassified)
            {
                return types.Count == 0;
            }
            return types.Contains(selected);
        }

        internal static List<string> GetTypes(GameItemElement item)
        {
            var types = new List<string>();
            try
            {
                List<string> raw = item.GetGameItemType();
                if (raw == null)
                {
                    return types;
                }
                foreach (string type in raw)
                {
                    if (!string.IsNullOrEmpty(type) && !types.Contains(type))
                    {
                        types.Add(type);
                    }
                }
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("GetGameItemType failed for " + SafeName(item) + ": " + e.Message);
            }
            return types;
        }

        internal static string SafeName(GameItemElement item)
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

        internal static void Select(string type)
        {
            SelectedType = type;
            Apply();
        }

        internal static void ClearSelection()
        {
            SelectedType = null;
            Restore();
        }

        /// <summary>Applies the current filter to every visible item (no-op when off).</summary>
        internal static void Apply()
        {
            if (!IsActive)
            {
                return;
            }

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
                    group.alpha = Matches(item) ? originalAlphas[go] : DimAlpha;
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

        /// <summary>Type -> number of visible items carrying that type.</summary>
        internal static Dictionary<string, int> CountByType(List<GameItemElement> items, out int total)
        {
            var counts = new Dictionary<string, int>();
            total = items.Count;

            foreach (GameItemElement item in items)
            {
                List<string> types = GetTypes(item);
                if (types.Count == 0)
                {
                    counts[Unclassified] = counts.TryGetValue(Unclassified, out int none) ? none + 1 : 1;
                    continue;
                }
                foreach (string type in types)
                {
                    counts[type] = counts.TryGetValue(type, out int current) ? current + 1 : 1;
                }
            }
            return counts;
        }
    }
}
