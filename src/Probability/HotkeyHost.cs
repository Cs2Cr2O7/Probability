using System;
using UnityEngine;

namespace Probability
{
    /// <summary>
    /// Per-frame host for the mod.
    ///
    /// It is a separate MonoBehaviour on purpose: ModHelper.StartCoroutine returns null
    /// inside OnEnable, because the loader registers the LoadedMod only after Init/OnEnable
    /// (ModLoader.cs: mods.Add happens after mod.OnEnable), so the "is this mod running"
    /// check fails and the coroutine never starts. A component of our own has no such
    /// dependency and keeps ticking for the whole session.
    /// </summary>
    internal sealed class HotkeyHost : MonoBehaviour
    {
        private void Update()
        {
            try
            {
                ModEntry.Tick();
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("Update failed: " + e);
            }
        }
    }
}
