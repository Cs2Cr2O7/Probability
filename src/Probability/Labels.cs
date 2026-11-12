using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Probability
{
    /// <summary>Localized labels with a readable English fallback.</summary>
    internal static class Labels
    {
        internal static string Get(string key, string fallback)
        {
            try
            {
                string text = ModHelper.GetLocalized(ModEntry.ModId, key);
                if (string.IsNullOrEmpty(text) || text == key)
                {
                    return fallback;
                }
                return text;
            }
            catch
            {
                return fallback;
            }
        }

        internal static string Category(string type)
        {
            if (type == ItemFilter.Unclassified)
            {
                return Get("cat_unclassified", "Unclassified");
            }

            // The game already ships localized names for every item type
            // ("type_food" -> "Food" / "食物"), so prefer those.
            string key = "type_" + type.ToLowerInvariant();
            try
            {
                string gameName = TypeHelper.GetTypeDisplayName(type);
                if (!string.IsNullOrEmpty(gameName) && gameName != key)
                {
                    return gameName;
                }
            }
            catch
            {
                // fall through to the mod's own labels
            }
            return Get("cat_" + type.ToLowerInvariant(), Prettify(type));
        }

        internal static string Prettify(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return "?";
            }
            var builder = new StringBuilder(type.Length);
            bool upperNext = true;
            foreach (char c in type)
            {
                if (c == '_')
                {
                    builder.Append(' ');
                    upperNext = true;
                    continue;
                }
                builder.Append(upperNext ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
                upperNext = false;
            }
            return builder.ToString();
        }

        /// <summary>Keeps a button label short enough for the fixed-width grid cells.</summary>
        internal static string Shorten(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max)
            {
                return text;
            }
            return text.Substring(0, max - 1).TrimEnd() + "…";
        }
    }
}
