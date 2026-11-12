using System;
using HarmonyLib;

namespace Probability
{
    /// <summary>
    /// Harmony patch classes for the "Super Charm Perfume" effect: while the buff is active for
    /// the current customer, bargain attempts cannot fail, reputation cannot drop and forged
    /// item features are not exposed.
    /// <para>
    /// Do NOT create a Harmony instance here: <c>ModLoader.LoadModDll</c> runs
    /// <c>harmony.PatchAll(assembly)</c> with its own instance ("mod.&lt;id&gt;") before
    /// <c>Init</c>/<c>OnEnable</c>, and unpatches the same instance when the mod is disabled.
    /// Patching again from the mod would apply every patch twice.
    /// </para>
    /// </summary>
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
        internal static void Neutralize(BargainUIManager manager)
        {
            if (!CharmEffect.IsActive || manager == null)
            {
                return;
            }
            try
            {
                var field = AccessTools.Field(typeof(BargainUIManager), "trackedClient");
                var client = field != null ? field.GetValue(manager) as StoreClient : null;
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

    /// <summary>Static helper: StoreReputation.ModReputation(string factionId, int value, bool notify).</summary>
    [HarmonyPatch(typeof(StoreReputation), "ModReputation", new[] { typeof(string), typeof(int), typeof(bool) })]
    internal static class PatchReputationLossFaction
    {
        private static bool Prefix(int __1)
        {
            return !(CharmEffect.IsActive && __1 < 0);
        }
    }

    /// <summary>Instance helper: StoreReputation.ModReputation(double modValue).</summary>
    [HarmonyPatch(typeof(StoreReputation), "ModReputation", new[] { typeof(double) })]
    internal static class PatchReputationLossInstance
    {
        private static bool Prefix(double __0)
        {
            return !(CharmEffect.IsActive && __0 < 0.0);
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
