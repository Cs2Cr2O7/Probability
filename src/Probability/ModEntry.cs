using System;
using System.Collections;
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

        private Coroutine hotkeyLoop;

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
                hotkeyLoop = ModHelper.StartCoroutine(ModId, PollHotkey());
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
                if (hotkeyLoop != null)
                {
                    ModHelper.StopCoroutine(ModId, hotkeyLoop);
                    hotkeyLoop = null;
                }
                ModHelper.RemoveDebugFunctions(ModId);
                FilterWindow.Close();
                Log.Log("disabled.");
            }
            catch (Exception e)
            {
                Log.Error("OnDisable failed: " + e);
            }
        }

        private IEnumerator PollHotkey()
        {
            while (true)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.F7))
                    {
                        FilterWindow.Toggle();
                    }
                    else if (ItemFilter.IsActive)
                    {
                        if (!FilterWindow.IsOpen)
                        {
                            // The window went away with its scene (save loaded, scene switch).
                            ItemFilter.ClearSelection();
                        }
                        else if (Time.frameCount % 30 == 0)
                        {
                            // Re-apply the dimming so items that appeared after the last
                            // pass (opened container, moved item) follow the filter too.
                            ItemFilter.Apply();
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("hotkey loop: " + e);
                }
                yield return null;
            }
        }
    }
}
