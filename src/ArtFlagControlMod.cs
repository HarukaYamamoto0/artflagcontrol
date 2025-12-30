using ArtFlagControl.Config;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ArtFlagControl;

[BepInPlugin("com.harukadev.magearena.artflagcontrol", "ArtFlagControlMod", "1.0.0")]
[BepInIncompatibility("com.magearena.hostsettings")]
[BepInProcess("MageArena.exe")]
public class ArtFlagControlMod : BaseUnityPlugin
{
    internal static ManualLogSource Log = null!;
    internal static FlagConfig ConfigData = null!;

    private Harmony _harmony = null!;

    private void Awake()
    {
        Log = Logger;

        ConfigData = new FlagConfig(Config);

        _harmony = new Harmony("com.harukadev.magearena.artflagcontrol");
        _harmony.PatchAll();

        Log.LogInfo("ArtFlagControl loaded successfully");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        Log.LogInfo("ArtFlagControl unloaded");
    }
}