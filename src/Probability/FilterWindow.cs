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

        /// <summary>Categories that currently have a button, in display order.</summary>
        private static List<string> shownTypes = new List<string>();
        private static Dictionary<string, int> currentCounts = new Dictionary<string, int>();
        private static int currentTotal;
        private static bool setChangeLogged;

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
                RememberPosition();
                ItemFilter.ClearSelection();
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("OnWindowClosed failed: " + e.Message);
            }
            window = null;
        }

        private static void RememberPosition()
        {
            try
            {
                if (window != null && window.IsAlive && window.Rect != null)
                {
                    lastPosition = window.Rect.anchoredPosition;
                }
            }
            catch
            {
                // a dead window has nothing to remember
            }
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

                // Update the existing buttons instead of rebuilding the window:
                // rebuilding closes/recreates it, which moves it and flickers.
                if (window != null && window.IsVisible)
                {
                    UpdateLabels();
                }
                else
                {
                    Build();
                }
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("Select failed: " + e);
            }
        }

        /// <summary>Re-renders the counts and the selection marker on the existing buttons.</summary>
        private static void UpdateLabels()
        {
            try
            {
                CustomUIElement all = window.Get("filter_all");
                if (all != null)
                {
                    all.SetText(Labels.Get("ui_all", "Show all"));
                }

                CustomUIElement header = window.Get("filter_total");
                if (header != null)
                {
                    header.SetText(Labels.Get("ui_total", "Items in view") + ": " + currentTotal);
                }

                foreach (string type in shownTypes)
                {
                    CustomUIElement element = window.Get("filter_" + type);
                    if (element == null)
                    {
                        continue;
                    }
                    element.SetText(Label(type, CountOf(type)));
                }
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("UpdateLabels failed: " + e.Message);
            }
        }

        private static int CountOf(string type)
        {
            return currentCounts.TryGetValue(type, out int count) ? count : 0;
        }

        private static string Label(string type, int count)
        {
            bool selected = ItemFilter.SelectedType == type;
            string name = Labels.Shorten(Labels.Category(type), 14);
            return (selected ? "> " : string.Empty) + name + " (" + count + ")";
        }

        /// <summary>
        /// Picks up item changes while the window stays open (a container was opened/closed,
        /// an item was moved). Existing buttons are re-labelled in place; categories that did
        /// not exist when the window was built only appear after reopening it, because adding
        /// buttons would have to resize the whole window.
        /// </summary>
        internal static void RefreshCounts()
        {
            if (window == null || !window.IsVisible)
            {
                return;
            }
            try
            {
                List<GameItemElement> items = ItemFilter.ScanVisibleItems();
                Dictionary<string, int> counts = ItemFilter.CountByType(items, out int total);

                bool hasUnshown = false;
                foreach (string type in counts.Keys)
                {
                    if (!shownTypes.Contains(type))
                    {
                        hasUnshown = true;
                        break;
                    }
                }

                currentCounts = counts;
                currentTotal = total;
                UpdateLabels();

                if (hasUnshown && !setChangeLogged)
                {
                    setChangeLogged = true;
                    ModEntry.Log.Log("a type outside the known list appeared ("
                        + counts.Count + " total); it shows up after reopening the window.");
                }
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("RefreshCounts failed: " + e.Message);
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
            currentCounts = counts;
            currentTotal = total;
            setChangeLogged = false;
            shownTypes = new List<string>();

            // Keep the window where the player dragged it across rebuilds.
            RememberPosition();

            CustomUIBuilder builder = CustomUIManager.Window(
                WindowId,
                Labels.Get("ui_title", "Item Category Filter"),
                CustomUIManager.LAYER_OVERLAY);
            if (builder == null)
            {
                ModEntry.Log.Error("CustomUIManager is not available in this scene.");
                return;
            }

            List<string> categories = BuildCategoryList(counts);

            int columns = 5;
            int rows = Math.Max(1, (categories.Count + columns - 1) / columns);
            float height = Math.Min(900f, 210f + rows * 40f);

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

            builder.AddHeader(Labels.Get("ui_total", "Items in view") + ": " + total, "filter_total");

            builder.AddButton(
                Labels.Get("ui_all", "Show all"),
                () => Select(null),
                "filter_all");

            builder.AddSeparator();

            builder.BeginGrid(columns, 104f, 32f, 5f);
            foreach (string type in categories)
            {
                shownTypes.Add(type);
                builder.AddButton(Label(type, CountOf(type)), () => Select(type), "filter_" + type);
            }
            builder.End();

            builder.AddSeparator();
            builder.AddCloseButton(Close, "filter_close");

            window = builder.Show();
            if (window != null)
            {
                window.SetTitle(Labels.Get("ui_title", "Item Category Filter"));
                string position = window.Rect != null ? window.Rect.anchoredPosition.ToString() : "?";
                ModEntry.Log.Log("window built: " + items.Count + " item(s), " + categories.Count
                    + " category(ies), " + counts.Count + " with items, visible=" + window.IsVisible
                    + ", pos=" + position);
            }
            else
            {
                ModEntry.Log.Error("window could not be shown.");
            }
        }

        /// <summary>All known types; categories that currently have items come first.</summary>
        private static List<string> BuildCategoryList(Dictionary<string, int> counts)
        {
            var all = new List<string>(ItemFilter.AllTypes);
            foreach (string type in counts.Keys)
            {
                if (type != ItemFilter.Unclassified && !all.Contains(type))
                {
                    all.Add(type);
                }
            }
            all.Add(ItemFilter.Unclassified);

            return all
                .OrderBy(type => CountOf(type) > 0 ? 0 : 1)
                .ThenBy(CategoryRank)
                .ThenByDescending(CountOf)
                .ThenBy(type => type, StringComparer.Ordinal)
                .ToList();
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
