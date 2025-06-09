using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

// https://github.com/developer9998/GorillaBountyHunter/blob/15cb1863147a829718e6061dbc84b71331a25c36/GorillaBountyHunter/Patches/OverrideCosmeticPatch.cs

namespace Cosmetic_ReTextureizer.Patches;

[HarmonyPatch(typeof(VRRig), nameof(VRRig.BuildInitialize_AfterCosmeticsV2Instantiated)), HarmonyWrapSafe]
internal class OverrideCosmeticPatch
{
    public static Dictionary<string, GameObject> OverridenObjectLookup = [];

    public static void Prefix(VRRig __instance)
    {
        if (!__instance.isOfflineVRRig || __instance._rigBuildFullyInitialized) return;

        __instance.cosmetics.Concat(__instance.overrideCosmetics).ForEach(gameObject =>
        {
            if (!OverridenObjectLookup.ContainsKey(gameObject.name))
            {
                OverridenObjectLookup.Add(gameObject.name, gameObject);
            }
        });
    }

    public static void Postfix(VRRig __instance)
    {
        if (!__instance.isOfflineVRRig) return;

        var clone = new Dictionary<string, GameObject>(OverridenObjectLookup);
        clone.ForEach(pair =>
        {
            if (pair.Key == pair.Value.name)
            {
                //Logging.Warning($"-{pair.Key} ({pair.Value.name})");
                OverridenObjectLookup.Remove(pair.Key);
            }
            else
            {
                //Logging.Info($"{pair.Key} ({pair.Value.name})");
            }
        });
    }
}