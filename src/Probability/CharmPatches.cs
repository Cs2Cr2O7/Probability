using System;
using System.Reflection;
using HarmonyLib;

namespace Probability
{
    /// <summary>
    /// Harmony patches behind the "Super Charm Perfume" effect: while the buff is active for the
    /// current customer, bargain attempts cannot fail, reputation cannot drop and fake/forged
    /// item features are not exposed.
    /// </summary>
    internal static class CharmPatches
    {
        private const string HarmonyId = "probability.charm";

        private static Harmony harmony;

        internal static void Apply()
        {
            if (harmony != null)
            {
                return;
            }
            try
            {
                harmony = new Harmony(HarmonyId);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                ModEntry.Log.Log("charm patches applied.");
            }
            catch (Exception e)
            {
                harmony = null;
                ModEntry.Log.Error("charm patches failed: " + e);
            }
        }

        internal static void Remove()
        {
            if (harmony == null)
            {
                return;
            }
            try
            {
                harmony.UnpatchAll(HarmonyId);
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("charm unpatch failed: " + e.Message);
            }
            harmony = null;
        }
    }

    // ---------------------------------------------------------------- bargain success chance

    /// <summary>Faction reputation bonus used by markup / buying-discount rolls.</summary>
    [HarmonyPatch(typeof(BargainUIManager), "GetFactionRepBonus")]
    internal static class PatchBargainFactionBonus
    {
        private static void Postfix(ref int __result)
        {
            if (CharmEffect.IsActive)
            {
                __result += 1000;
            }
        }
    }

    /// <summary>Deal-maker module bonus used by markup / buying-discount / blackmail rolls.</summary>
    [HarmonyPatch(typeof(BargainUIManager), "GetDealMakerBonus")]
    internal static class PatchBargainDealMakerBonus
    {
        private static void Postfix(ref int __result)
        {
            if (CharmEffect.IsActive)
            {
                __result += 1000;
            }
        }
    }

    /// <summary>Faction reputation bonus used by the blackmail roll.</summary>
    [HarmonyPatch(typeof(BargainUIManager), "GetFactionRepBonusBlackmail")]
    internal static class PatchBargainBlackmailBonus
    {
        private static void Postfix(ref int __result)
        {
            if (CharmEffect.IsActive)
            {
                __result += 1000;
            }
        }
    }

    /// <summary>
    /// Customers that normally always reject a bad bargain (or Aug clients that never haggle)
    /// become pliable while the charm is active. The fields are on the client blueprint and
    /// persist for that customer, which is exactly how long the buff lasts.
    /// </summary>
    internal static class CharmClientSupport
    {
        private static readonly AccessTools.FieldRef<BargainUIManager, StoreClient> TrackedClient =
            AccessTools.FieldRefAccess<BargainUIManager, StoreClient>("trackedClient");

        internal static void Neutralize(BargainUIManager manager)
        {
            if (!CharmEffect.IsActive || manager == null)
            {
                return;
            }
            try
            {
                StoreClient client = TrackedClient(manager);
                if (client == null)
                {
                    return;
                }
                client.alwaysRejectBadBargain = false;
                client.isAug = false;
                client.staysAfterFailedBargain = true;
            }
            catch (Exception e)
            {
                ModEntry.Log.Error("charm client tweak failed: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(BargainUIManager), "OfferMarkup")]
    internal static class PatchOfferMarkup
    {
        private static void Prefix(BargainUIManager __instance)
        {
            CharmClientSupport.Neutralize(__instance);
        }
    }

    [HarmonyPatch(typeof(BargainUIManager), "DemandBuyingDiscount")]
    internal static class PatchDemandBuyingDiscount
    {
        private static void Prefix(BargainUIManager __instance)
        {
            CharmClientSupport.Neutralize(__instance);
        }
    }

    [HarmonyPatch(typeof(BargainUIManager), "Blackmail")]
    internal static class PatchBlackmail
    {
        private static void Prefix(BargainUIManager __instance)
        {
            CharmClientSupport.Neutralize(__instance);
        }
    }

    // ---------------------------------------------------------------- reputation loss

    [HarmonyPatch(typeof(StoreReputation), "ModReputation", new[] { typeof(string), typeof(int), typeof(bool) })]
    internal static class PatchReputationLossFaction
    {
        private static bool Prefix(int __1)
        {
            return !(CharmEffect.IsActive && __1 < 0);
        }
    }

    [HarmonyPatch(typeof(StoreReputation), "ModReputation", new[] { typeof(double) })]
    internal static class PatchReputationLossInstance
    {
        private static bool Prefix(double __1)
        {
            return !(CharmEffect.IsActive && __1 < 0.0);
        }
    }

    // ---------------------------------------------------------------- forged labels

    [HarmonyPatch(typeof(ItemFeature), nameof(ItemFeature.WillClientExposeFeature))]
    internal static class PatchWillExposeFeature
    {
        private static void Postfix(ref bool __result)
        {
            if (CharmEffect.IsActive)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(ItemFeature), nameof(ItemFeature.CanExposeFeature))]
    internal static class PatchCanExposeFeature
    {
        private static void Postfix(ref bool __result)
        {
            if (CharmEffect.IsActive)
            {
                __result = false;
            }
        }
    }
}
