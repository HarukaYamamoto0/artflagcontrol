using System.IO;
using ArtFlagControl.Config;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ArtFlagControl;

[BepInPlugin("com.harukadev.magearena.artflagcontrol", "ArtFlagControlMod", "1.0.0")]
[BepInIncompatibility("com.magearena.hostsettings")]
[BepInProcess("MageArena.exe")]
public class ModSystem : BaseUnityPlugin
{
    internal static ModSystem Instance = null!;
    internal static ManualLogSource Log = null!;
    internal static FlagConfig ConfigData = null!;
    internal static FlagMaterialProvider FlagMaterialProvider = null!;

    internal static void LogDebug(string message)
    {
        if (ConfigData?.DebugMode.Value == true)
        {
            Log.LogInfo(message);
        }
    }

    private Harmony _harmony = null!;
    private FileSystemWatcher _configWatcher = null!;
    private bool _configNeedsReload;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        Log.LogInfo($"ArtFlagControl: Initializing mod. Config file: {Config.ConfigFilePath}");

        ConfigData = new FlagConfig(Config);
        FlagMaterialProvider = new FlagMaterialProvider();

        // Use global config event for better reliability and detailed logging
        Config.SettingChanged += OnConfigSettingChanged;

        SetupConfigWatcher();

        _harmony = new Harmony("com.harukadev.magearena.artflagcontrol");
        _harmony.PatchAll();

        Log.LogInfo("ArtFlagControl loaded successfully");
    }

    private void OnConfigSettingChanged(object sender, SettingChangedEventArgs args)
    {
        LogDebug($"ArtFlagControl: Config changed ({args.ChangedSetting.Definition.Section}.{args.ChangedSetting.Definition.Key} = {args.ChangedSetting.BoxedValue})");
        
        // If Enabled is toggled, we might need to refresh materials or clear them
        if (args.ChangedSetting.Definition.Key == "Enabled")
        {
            if (!(bool)args.ChangedSetting.BoxedValue)
            {
                // Mod disabled, maybe we should revert materials? 
                // For now just stop updating.
            }
            else
            {
                FlagMaterialProvider.UpdateMaterials();
            }
            return;
        }

        FlagMaterialProvider.UpdateMaterials();
    }

    private void SetupConfigWatcher()
    {
        var configPath = Config.ConfigFilePath;
        var directory = Path.GetDirectoryName(configPath);
        var filename = Path.GetFileName(configPath);

        if (directory == null || !Directory.Exists(directory))
        {
            Log.LogWarning($"ArtFlagControl: Could not start config watcher. Directory not found: {directory}");
            return;
        }

        _configWatcher = new FileSystemWatcher(directory, filename)
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        _configWatcher.Changed += (sender, args) =>
        {
            _configNeedsReload = true;
        };
        
        LogDebug("ArtFlagControl: Config file watcher initialized");
    }

    private void Update()
    {
        if (_configNeedsReload)
        {
            _configNeedsReload = false;
            try
            {
                LogDebug("ArtFlagControl: Config file change detected, reloading...");
                Config.Reload();
            }
            catch (IOException)
            {
                // The editor might lock a file. 
                // We don't set _configNeedsReload = true here to avoid a tight loop.
                // The FileSystemWatcher will likely fire again when the file is released.
                Log.LogWarning("ArtFlagControl: Config file is busy, will try again on next change.");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"ArtFlagControl: Error reloading config: {ex.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        Config.SettingChanged -= OnConfigSettingChanged;
        
        if (_configWatcher != null)
        {
            _configWatcher.EnableRaisingEvents = false;
            _configWatcher.Dispose();
        }

        FlagMaterialProvider?.Cleanup();
        
        _harmony?.UnpatchAll(_harmony.Id);
        Log.LogInfo("ArtFlagControl unloaded");
    }
}