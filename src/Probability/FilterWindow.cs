using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Probability
{
    /// <summary>
    /// The floating category filter window: lists every item type found among the
    /// items currently on screen, with how many items carry it.
    /// </summary>
    internal static class FilterWindow
    {
        private const string WindowId = "probability.filter";

        private static CustomUIWindow window;
        private static Vector2? lastPosition;
        private static bool rebuilding;

        internal static bool IsOpen
        {
            get
            {
                try
                {
                    return window != null && window.IsVisible;
                }
                catch
                {
                    return false;
                }
            }
        }

        internal static void Toggle()
        {
            if (window != null && window.IsVisible)
            {
                ModEntry.Log.Log("closing the filter window.");
                Close();
                return;
            }
            ModEntry.Log.Log("opening the filter window.");
            Open();
        }

        internal static void Open()
        {
            try
            {
                Build();
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("Open failed: " + e);
            }
        }

        internal static void Close()
        {
            try
            {
                ItemFilter.ClearSelection();
                if (window != null)
                {
                    window.Close();
                    window = null;
                }
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("Close failed: " + e.Message);
            }
        }

        private static void OnWindowClosed()
        {
            if (rebuilding)
            {
                // The rebuild below closes the old window on purpose; keep the filter.
                return;
            }
            try
            {
                ItemFilter.ClearSelection();
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("OnWindowClosed failed: " + e.Message);
            }
            window = null;
        }

        private static void Select(string type)
        {
            try
            {
                ModEntry.Log.Log("filter = " + (type ?? "(all)"));
                if (type == null)
                {
                    ItemFilter.ClearSelection();
                }
                else
                {
                    ItemFilter.Select(type);
                }
                Build();
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("Select failed: " + e);
            }
        }

        private static void Build()
        {
            // Creating a window with the same id closes the previous one, which fires
            // its OnClose callback. Keep the flag set for the whole rebuild so that
            // callback does not clear the active filter.
            rebuilding = true;
            try
            {
                BuildCore();
            }
            finally
            {
                rebuilding = false;
            }
        }

        private static void BuildCore()
        {
            List<GameItemElement> items = ItemFilter.ScanVisibleItems();
            Dictionary<string, int> counts = ItemFilter.CountByType(items, out int total);

            // Keep the window where the player dragged it across rebuilds.
            if (window != null && window.IsAlive && window.Rect != null)
            {
                lastPosition = window.Rect.anchoredPosition;
            }

            CustomUIBuilder builder = CustomUIManager.Window(
                WindowId,
                Labels.Get("ui_title", "Item Category Filter"),
                CustomUIManager.LAYER_OVERLAY);
            if (builder == null)
            {
                ModEntry.Log.Error("CustomUIManager is not available in this scene.");
                return;
            }

            int columns = 4;
            int rows = Math.Max(1, (counts.Count + columns - 1) / columns);
            float height = Math.Min(760f, 210f + rows * 40f);

            builder.SetSize(560f, height);
            builder.SetDraggable(true);
            builder.SetCloseOnEscape(false);
            builder.OnClose(OnWindowClosed);
            if (lastPosition.HasValue)
            {
                builder.SetPosition(lastPosition.Value);
            }
            else
            {
                builder.Center();
            }

            builder.AddHeader(Labels.Get("ui_total", "Items in view") + ": " + total);

            builder.AddButton(
                Labels.Get("ui_all", "Show all"),
                () => Select(null),
                "filter_all");

            builder.AddSeparator();

            if (counts.Count == 0)
            {
                builder.AddLabel(Labels.Get("ui_empty", "No items in view. Open a container or the inventory first."));
            }
            else
            {
                builder.BeginGrid(columns, 126f, 34f, 6f);
                foreach (KeyValuePair<string, int> entry in OrderCategories(counts))
                {
                    string type = entry.Key;
                    bool selected = ItemFilter.SelectedType == type;
                    string label = (selected ? "> " : string.Empty)
                        + Labels.Category(type) + " (" + entry.Value + ")";
                    builder.AddButton(label, () => Select(type), "filter_" + type);
                }
                builder.End();
            }

            builder.AddSeparator();
            builder.AddCloseButton(Close, "filter_close");

            window = builder.Show();
            if (window != null)
            {
                window.SetTitle(Labels.Get("ui_title", "Item Category Filter"));
                string position = window.Rect != null ? window.Rect.anchoredPosition.ToString() : "?";
                ModEntry.Log.Log("window built: " + items.Count + " item(s), " + counts.Count
                    + " category(ies), visible=" + window.IsVisible + ", pos=" + position);
            }
            else
            {
                ModEntry.Log.Error("window could not be shown.");
            }
        }

        /// <summary>Game "major" categories first, then the rest by how many items carry them.</summary>
        private static IEnumerable<KeyValuePair<string, int>> OrderCategories(Dictionary<string, int> counts)
        {
            return counts
                .OrderBy(pair => CategoryRank(pair.Key))
                .ThenByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal);
        }

        private static int CategoryRank(string type)
        {
            if (type == ItemFilter.Unclassified)
            {
                return 2000;
            }
            try
            {
                int index = TypeHelper.MAJOR_TYPES_LIST.IndexOf(type);
                return index >= 0 ? index : 1000;
            }
            catch
            {
                return 1000;
            }
        }
    }
}
