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
                ModHelper.AddDebugFunction(ModId, "Modding", "Probability: give 超级魅惑香水", CharmPerfumeDirectory.GiveToPlayer, tall: true);
                ModHelper.AddDebugFunction(ModId, "Modding", "Probability: give 存储终端", StorageTerminalDirectory.GiveMachine, tall: true);
                ModHelper.AddDebugFunction(ModId, "Modding", "Probability: give 存储硬盘一套", StorageTerminalDirectory.GiveDriveSet, tall: true);

                CharmPerfumeDirectory.EnsureRegistered();
                StorageTerminalDirectory.EnsureRegistered();
                AssemblerRitual.Subscribe();

                if (hostObject != null)
                {
                    UnityEngine.Object.Destroy(hostObject);
                }
                hostObject = new GameObject("Probability.HotkeyHost");
                UnityEngine.Object.DontDestroyOnLoad(hostObject);
                hostObject.AddComponent<HotkeyHost>();

                Log.Log("enabled. The negotiation tablet's Filter button opens the category filter; the F9 Modding tab hands out the mod items.");
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
                CharmEffect.Clear();
                AssemblerRitual.Unsubscribe();

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
            // The negotiation tablet gets its own filter button once it exists.
            TabletFilterEntry.Tick();

            MolecularAssembler.RuntimeTick();

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
