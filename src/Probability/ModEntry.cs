using System;
using UnityEngine;

namespace Probability
{
    /// <summary>
    /// Entry point of the mod. The game calls Init once when the mod is loaded,
    /// then OnEnable right after it, and OnDisable when the game shuts down.
    /// All three run from the main menu, where no gameplay systems exist yet.
    /// </summary>
    public class ModEntry : IMod
    {
        public const string ModId = "probability";

        internal static ModLog Log;

        private GameObject hostObject;

        public void Init(ModManifest manifest)
        {
            Log = new ModLog(manifest);
            Log.Log("Probability initializing...");
        }

        public void OnEnable()
        {
            try
            {
                ModHelper.AddDebugFunction(ModId, "Modding", "Probability: item category filter", FilterWindow.Toggle, tall: true);

                if (hostObject != null)
                {
                    UnityEngine.Object.Destroy(hostObject);
                }
                hostObject = new GameObject("Probability.HotkeyHost");
                UnityEngine.Object.DontDestroyOnLoad(hostObject);
                hostObject.AddComponent<HotkeyHost>();

                Log.Log("enabled. Press F7 (or the F9 debug button) to open the category filter.");
            }
            catch (Exception e)
            {
                Log.Error("OnEnable failed: " + e);
            }
        }

        public void OnDisable()
        {
            try
            {
                ModHelper.RemoveDebugFunctions(ModId);
                FilterWindow.Close();

                if (hostObject != null)
                {
                    UnityEngine.Object.Destroy(hostObject);
                    hostObject = null;
                }
                Log.Log("disabled.");
            }
            catch (Exception e)
            {
                Log.Error("OnDisable failed: " + e);
            }
        }

        /// <summary>Called every frame by <see cref="HotkeyHost"/>.</summary>
        internal static void Tick()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                FilterWindow.Toggle();
                return;
            }

            if (!ItemFilter.IsActive)
            {
                return;
            }

            if (!FilterWindow.IsOpen)
            {
                // The window went away with its scene (save loaded, scene switch).
                ItemFilter.ClearSelection();
            }
            else if (Time.frameCount % 30 == 0)
            {
                // Re-apply the dimming so items that appeared after the last
                // pass (opened container, moved item) follow the filter too,
                // and keep the category counts on the buttons up to date.
                ItemFilter.Apply();
                FilterWindow.RefreshCounts();
            }
        }
    }
}
