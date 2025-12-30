using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ArtFlagControl.Patches;

[HarmonyPatch(typeof(MainMenuManager))]
// ReSharper disable once UnusedType.Global
public class FlagMaterialReplacePatch
{
    [HarmonyPatch("ActuallyStartGameActually")]
    [HarmonyPostfix]
    // ReSharper disable once UnusedMember.Global
    public static void Postfix()
    {
        if (!ModSystem.ConfigData.Enabled.Value) return;

        var flagType = AccessTools.TypeByName("FlagController");
        if (flagType == null)
        {
            ModSystem.Log.LogError("ArtFlagControl: FlagController type not found!");
            return;
        }

        var flags = Object.FindObjectsByType(
            flagType,
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        if (flags.Length == 0) return;

        var flagMatsField = flagType.GetField("flagMats", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (flagMatsField == null) return;

        foreach (var flag in flags)
        {
            var mats = (Material[])flagMatsField.GetValue(flag);
            if (mats == null || mats.Length == 0) continue;
            
            // Try to find a good base material (prefer Neutral)
            Material baseMat = null;
            foreach (var m in mats)
            {
                if (m == null) continue;
                if (m.name.ToLowerInvariant().Contains("neutral"))
                {
                    baseMat = m;
                    break;
                }
            }
            
            if (baseMat == null) baseMat = mats[0]; // Fallback to first
            if (baseMat == null) continue;

            ModSystem.FlagMaterialProvider.Init(baseMat);
            break;
        }
    }
}