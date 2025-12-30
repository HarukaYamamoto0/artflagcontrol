using HarmonyLib;
using UnityEngine;

namespace ArtFlagControl.Patches;

[HarmonyPatch(typeof(MainMenuManager))]
public class FlagMaterialReplacePatch
{
    [HarmonyPatch("ActuallyStartGameActually")]
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!ArtFlagControlMod.ConfigData.Enabled.Value) return;

        var flagType = AccessTools.TypeByName("FlagController");
        if (flagType == null)
        {
            ArtFlagControlMod.Log.LogError("ArtFlagControl: FlagController type not found!");
            return;
        }

        var flags = Object.FindObjectsByType(
            flagType,
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        if (flags.Length == 0) return;

        ArtFlagControlMod.Log.LogInfo("ArtFlagControl: Re-applying materials to " + flags.Length + " flags");

        var flagVisualField = AccessTools.Field(flagType, "flagvisual");
        var flagMatsField = AccessTools.Field(flagType, "flagMats");
        
        var cfg = ArtFlagControlMod.ConfigData;

        foreach (var flag in flags)
        {
            var mats = (Material[])flagMatsField.GetValue(flag);
            if (mats == null || mats.Length == 0) continue;

            FlagMaterialProvider.Init(
                mats[0],
                cfg.NeutralColor,
                cfg.SorcererColor,
                cfg.WarlockColor
            );

            var renderer = (SkinnedMeshRenderer)flagVisualField.GetValue(flag);
            if (renderer != null)
            {
                renderer.material = FlagMaterialProvider.Get(0);
            }

            if (mats.Length > 1) mats[1] = FlagMaterialProvider.Get(1);
            if (mats.Length > 2) mats[2] = FlagMaterialProvider.Get(2);

            ArtFlagControlMod.Log.LogInfo("Flag replaced");
        }
    }
}