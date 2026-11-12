using System;

namespace Probability
{
    /// <summary>
    /// State of the "Super Charm Perfume" buff. It is tied to the customer that was behind the
    /// counter when the perfume was used: the effect ends as soon as that customer leaves.
    /// </summary>
    internal static class CharmEffect
    {
        private static object boundClient;

        internal static bool IsActive
        {
            get
            {
                if (boundClient == null)
                {
                    return false;
                }
                try
                {
                    object current = PlayerStore.Instance != null ? PlayerStore.Instance.currentClientInstance : null;
                    if (current == null || !ReferenceEquals(current, boundClient))
                    {
                        boundClient = null;
                        return false;
                    }
                    return true;
                }
                catch
                {
                    boundClient = null;
                    return false;
                }
            }
        }

        internal static void Activate()
        {
            boundClient = PlayerStore.Instance != null ? PlayerStore.Instance.currentClientInstance : null;
        }

        internal static void Clear()
        {
            boundClient = null;
        }
    }
}
