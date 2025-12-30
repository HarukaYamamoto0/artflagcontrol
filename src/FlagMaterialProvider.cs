using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace ArtFlagControl;

public enum Faction
{
    Sorcerer = 0,
    Warlock = 1,
    Neutral = 2
}

public class FlagMaterialProvider
{
    private bool _init;

    private Material _baseMaterial;
    private Material _neutral;
    private Material _sorcerer;
    private Material _warlock;

    private readonly Coroutine[] _activeLoads = new Coroutine[3];
    private readonly Dictionary<int, int> _valToIndexMap = new();

    // Shader Properties
    private static readonly int BaseMap = Shader.PropertyToID("_BaseColorMap");
    private static readonly int Metallic = Shader.PropertyToID("_Metallic");
    private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
    private static readonly int SpecColor = Shader.PropertyToID("_SpecularColor");

    // Reflection Cache
    private Type _flagControllerType;
    private FieldInfo _flagVisualField;
    private FieldInfo _flagMatsField;
    private FieldInfo _factionField;

    private int _sorcererIdx = -1;
    private int _warlockIdx = -1;
    private int _neutralIdx = -1;

    public void Init(Material baseMat)
    {
        if (baseMat == null || (_init && _baseMaterial == baseMat)) return;

        _baseMaterial = baseMat;
        _init = true;

        EnsureReflectionInit();
        UpdateMaterials();
    }

    private void DetectIndices(Material[] mats)
    {
        if (mats == null || mats.Length == 0) return;

        bool needsDetection = _sorcererIdx == -1 || _warlockIdx == -1 || _neutralIdx == -1;

        if (ModSystem.ConfigData.DebugMode.Value)
        {
            var names = string.Join(", ", mats.Select((m, i) => $"[{i}] {(m != null ? m.name : "null")}"));
            ModSystem.LogDebug($"ArtFlagControl: Flag materials: {names}");
        }

        if (needsDetection)
        {
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string n = mats[i].name.ToLowerInvariant();

                if (n.Contains("sorcerer")) _sorcererIdx = i;
                else if (n.Contains("warlock")) _warlockIdx = i;
                else if (n.Contains("neutral") || n.Contains("blank")) _neutralIdx = i;
                else if (n.Contains("flag1")) _sorcererIdx = i;
                else if (n.Contains("flag2")) _warlockIdx = i;
            }

            if (_sorcererIdx != -1 || _warlockIdx != -1 || _neutralIdx != -1)
            {
                ModSystem.LogDebug($"ArtFlagControl: Dynamic indices detected - Sorcerer: {_sorcererIdx}, Warlock: {_warlockIdx}, Neutral: {_neutralIdx}");
            }
            else
            {
                // Fallback to defaults if no names found
                ModSystem.LogDebug($"ArtFlagControl: No faction keywords found in material names. Using default indices (Total mats: {mats.Length})");
                _sorcererIdx = 0;
                _warlockIdx = mats.Length > 1 ? 1 : 0;
                _neutralIdx = mats.Length > 2 ? 2 : 0;
            }
        }
    }

    private void EnsureReflectionInit()
    {
        if (_flagControllerType != null) return;

        _flagControllerType = AccessTools.TypeByName("FlagController");
        if (_flagControllerType == null)
        {
            ModSystem.Log.LogError("ArtFlagControl: Could not find FlagController type");
            return;
        }

        _flagVisualField = _flagControllerType.GetField("flagvisual", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        _flagMatsField = _flagControllerType.GetField("flagMats", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        string[] possibleFactionFields = { "controlTeam", "faction", "team", "side", "owner", "teamIndex", "factionIndex" };
        foreach (var name in possibleFactionFields)
        {
            _factionField = _flagControllerType.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (_factionField != null) break;
        }

        if (_factionField == null)
        {
            var fields = _flagControllerType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            ModSystem.Log.LogWarning($"ArtFlagControl: Faction field not found. Available: {string.Join(", ", fields.Select(f => f.Name))}");
        }
    }

    public void UpdateMaterials()
    {
        if (!_init || _baseMaterial == null) return;

        var cfg = ModSystem.ConfigData;

        UpdateFactionAsync(Faction.Sorcerer, cfg.SorcererColor, cfg.SorcererImagePath.Value, cfg.SorcererImageUrl.Value);
        UpdateFactionAsync(Faction.Warlock, cfg.WarlockColor, cfg.WarlockImagePath.Value, cfg.WarlockImageUrl.Value);
        UpdateFactionAsync(Faction.Neutral, cfg.NeutralColor, cfg.NeutralImagePath.Value, cfg.NeutralImageUrl.Value);
    }

    private void UpdateFactionAsync(Faction faction, Color color, string path, string url)
    {
        int index = (int)faction;
        if (_activeLoads[index] != null)
        {
            ModSystem.Instance.StopCoroutine(_activeLoads[index]);
        }

        _activeLoads[index] = ModSystem.Instance.StartCoroutine(ProcessFactionLoad(faction, color, path, url));
    }

    private IEnumerator ProcessFactionLoad(Faction faction, Color color, string path, string url)
    {
        int index = (int)faction;
        string tag = faction.ToString();
        Texture2D texture = null;
        string finalSource = "Color";

        // 1. Try URL
        if (!string.IsNullOrEmpty(url))
        {
            yield return LoadTextureAsync(url, (tex) => texture = tex);
            if (texture != null) 
            {
                finalSource = url;
            }
            else
            {
                ModSystem.Log.LogWarning($"ArtFlagControl: Failed to load texture via URL for {tag}. Check the link or your connection.");
            }
        }

        // 2. Fallback to Local Path if URL failed or was empty
        if (texture == null && !string.IsNullOrEmpty(path))
        {
            yield return LoadTextureAsync(path, (tex) => texture = tex);
            if (texture != null)
            {
                finalSource = path;
            }
            else
            {
                ModSystem.Log.LogWarning($"ArtFlagControl: Failed to load local texture for {tag}. Check if the path is correct.");
            }
        }

        Material newMat = texture != null 
            ? CreateFromTexture(_baseMaterial, texture, tag) 
            : CreateFromColor(_baseMaterial, color, tag);

        ReplaceMaterial(faction, newMat);

        ModSystem.LogDebug($"ArtFlagControl: Material for {tag} updated (Source: {finalSource}, Texture: {(texture != null ? texture.name : "None")})");

        ApplyToAllFlags();

        _activeLoads[index] = null;
    }

    public void Cleanup()
    {
        for (int i = 0; i < _activeLoads.Length; i++)
        {
            if (_activeLoads[i] != null)
            {
                ModSystem.Instance.StopCoroutine(_activeLoads[i]);
                _activeLoads[i] = null;
            }
        }

        ReplaceMaterial(Faction.Sorcerer, null);
        ReplaceMaterial(Faction.Warlock, null);
        ReplaceMaterial(Faction.Neutral, null);
        
        _init = false;
    }

    private void ReplaceMaterial(Faction faction, Material newMat)
    {
        Material oldMat = faction switch
        {
            Faction.Sorcerer => _sorcerer,
            Faction.Warlock => _warlock,
            Faction.Neutral => _neutral,
            _ => null
        };

        switch (faction)
        {
            case Faction.Sorcerer: _sorcerer = newMat; break;
            case Faction.Warlock: _warlock = newMat; break;
            case Faction.Neutral: _neutral = newMat; break;
        }

        if (oldMat && oldMat != newMat && oldMat.name.StartsWith("ArtFlag_"))
        {
            // Cleanup texture if it was a custom one
            if (oldMat.HasProperty(BaseMap))
            {
                var oldTex = oldMat.GetTexture(BaseMap);
                if (oldTex != null && oldTex.name.StartsWith("ArtFlagTex_") && oldTex is Texture2D)
                {
                    Object.Destroy(oldTex);
                }
            }
            Object.Destroy(oldMat);
        }
    }

    private string CachePath => Path.Combine(Paths.CachePath, "ArtFlagControl");

    private string GetCacheFilePath(string url)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
        var fileName = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant() + ".cache";
        return Path.Combine(CachePath, fileName);
    }

    private IEnumerator LoadTextureAsync(string source, Action<Texture2D> callback)
    {
        string uri = source;
        bool isUrl = source.StartsWith("http") || source.Contains("://");

        // Handle local paths
        if (!isUrl)
        {
            try
            {
                if (!File.Exists(source))
                {
                    ModSystem.Log.LogWarning($"ArtFlagControl: File not found: {source}");
                    callback?.Invoke(null);
                    yield break;
                }

                uri = "file://" + Path.GetFullPath(source);
            }
            catch (Exception ex)
            {
                ModSystem.Log.LogError($"ArtFlagControl: Error resolving path {source}: {ex.Message}");
                callback?.Invoke(null);
                yield break;
            }
        }
        else
        {
            // Handle URL Cache
            if (ModSystem.ConfigData.UseCache.Value)
            {
                var cacheFile = GetCacheFilePath(source);
                if (File.Exists(cacheFile))
                {
                    ModSystem.LogDebug($"ArtFlagControl: Loading from cache: {source}");
                    uri = "file://" + Path.GetFullPath(cacheFile);
                }
            }
        }

        using var request = UnityWebRequest.Get(uri);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] data = request.downloadHandler.data;
            if (data == null || data.Length == 0)
            {
                ModSystem.Log.LogError($"ArtFlagControl: No data received from {uri}");
                callback?.Invoke(null);
                yield break;
            }

            var tex = new Texture2D(2, 2);
            if (tex.LoadImage(data))
            {
                tex.name = "ArtFlagTex_" + Path.GetFileName(source);

                // Save to cache if it was a new download
                if (isUrl && !uri.StartsWith("file://") && ModSystem.ConfigData.UseCache.Value)
                {
                    try
                    {
                        if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
                        File.WriteAllBytes(GetCacheFilePath(source), data);
                        ModSystem.LogDebug($"ArtFlagControl: Saved to cache: {source}");
                    }
                    catch (Exception ex)
                    {
                        ModSystem.Log.LogWarning($"ArtFlagControl: Failed to save cache for {source}: {ex.Message}");
                    }
                }

                callback?.Invoke(tex);
            }
            else
            {
                ModSystem.Log.LogError($"ArtFlagControl: Failed to decode texture from {uri} (Format might not be supported)");
                Object.Destroy(tex);
                callback?.Invoke(null);
            }
        }
        else
        {
            ModSystem.Log.LogError($"ArtFlagControl: Failed to load texture from {uri}: {request.error}");
            callback?.Invoke(null);
        }
    }

    public void ApplyToAllFlags()
    {
        if (!_init) return;

        EnsureReflectionInit();
        if (_flagControllerType == null) return;

        var flags = Object.FindObjectsByType(
            _flagControllerType,
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        if (flags.Length == 0) return;

        if (_flagVisualField == null || _flagMatsField == null)
        {
            ModSystem.LogDebug("ArtFlagControl: Essential fields (flagvisual/flagMats) not found on FlagController");
            return;
        }

        int appliedCount = 0;
        foreach (var flag in flags)
        {
            var mats = (Material[])_flagMatsField.GetValue(flag);
            if (mats == null || mats.Length == 0) continue;

            // Detect indices if not already done
            DetectIndices(mats);

            var renderer = _flagVisualField.GetValue(flag) as Renderer;
            if (renderer != null)
            {
                var currentMat = renderer.sharedMaterial;
                int matCount = renderer.sharedMaterials.Length;

                // Don't overwrite dynamic textures from other sources if specifically marked
                if (currentMat != null && currentMat.name.Contains("Dynamic") && !currentMat.name.StartsWith("ArtFlag_")) continue;

                object factionVal = _factionField?.GetValue(flag);
                
                // Observe mapping if not already known
                if (factionVal != null && currentMat != null && !currentMat.name.StartsWith("ArtFlag_"))
                {
                    int val = Convert.ToInt32(factionVal);
                    if (!_valToIndexMap.ContainsKey(val))
                    {
                        for (int i = 0; i < mats.Length; i++)
                        {
                            if (currentMat == mats[i])
                            {
                                _valToIndexMap[val] = i;
                                ModSystem.LogDebug($"ArtFlagControl: Observed mapping FactionVal {val} -> Index {i} (Mat: {currentMat.name})");
                                
                                // Improve detection based on observed mapping
                                if (currentMat.name.ToLowerInvariant().Contains("sorcerer")) _sorcererIdx = i;
                                else if (currentMat.name.ToLowerInvariant().Contains("warlock")) _warlockIdx = i;
                                else if (currentMat.name.ToLowerInvariant().Contains("neutral") || currentMat.name.ToLowerInvariant().Contains("blank")) _neutralIdx = i;
                                
                                break;
                            }
                        }
                    }
                }

                // Update the controller's material palette using detected indices (possibly improved by the observation above)
                if (_sorcererIdx != -1 && _sorcerer != null && mats.Length > _sorcererIdx) mats[_sorcererIdx] = _sorcerer;
                if (_warlockIdx != -1 && _warlock != null && mats.Length > _warlockIdx) mats[_warlockIdx] = _warlock;
                if (_neutralIdx != -1 && _neutral != null && mats.Length > _neutralIdx) mats[_neutralIdx] = _neutral;

                int applyIndex = -1;

                // 1. Try to detect it by observed mapping or faction field (controlTeam)
                if (factionVal != null)
                {
                    try
                    {
                        int val = Convert.ToInt32(factionVal);
                        
                        if (_valToIndexMap.TryGetValue(val, out int mappedIdx))
                        {
                            applyIndex = mappedIdx;
                        }
                        else if (val == 99) 
                        {
                            applyIndex = _neutralIdx;
                        }
                        else
                        {
                            if (val == (int)Faction.Sorcerer) applyIndex = _sorcererIdx;
                            else if (val == (int)Faction.Warlock) applyIndex = _warlockIdx;
                            else if (val == (int)Faction.Neutral) applyIndex = _neutralIdx;
                            else if (val >= 0 && val < mats.Length) applyIndex = val;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }

                // 2. Try to detect by comparing with the current palette
                if (applyIndex == -1 && currentMat != null)
                {
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (currentMat == mats[i])
                        {
                            applyIndex = i;
                            break;
                        }
                    }
                }

                // 3. Try to detect by name (case-insensitive)
                if (applyIndex == -1 && currentMat != null)
                {
                    string name = currentMat.name.ToLowerInvariant();
                    if (name.Contains("sorcerer")) applyIndex = _sorcererIdx;
                    else if (name.Contains("warlock")) applyIndex = _warlockIdx;
                    else if (name.Contains("neutral")) applyIndex = _neutralIdx;
                }

                // 4. Default to Neutral index
                if (applyIndex < 0 || applyIndex >= mats.Length) 
                {
                    applyIndex = _neutralIdx != -1 ? _neutralIdx : (mats.Length > 2 ? 2 : 0);
                }

                Material targetMat = mats[applyIndex];
                if (targetMat != null)
                {
                    renderer.sharedMaterial = targetMat;
                    appliedCount++;
                }
                
                if (ModSystem.ConfigData.DebugMode.Value)
                {
                    ModSystem.LogDebug($"ArtFlagControl: Flag {flag.GetHashCode()} | FactionVal: {factionVal ?? "N/A"} | CurMat: {currentMat?.name ?? "None"} (Shader: {currentMat?.shader.name ?? "N/A"}, Mats: {matCount}) | Final Index: {applyIndex} | Applied: {targetMat?.name ?? "NULL"}");
                }
            }
        }

        ModSystem.LogDebug($"ArtFlagControl: Applied materials to {appliedCount}/{flags.Length} flags");
    }

    public static Material CreateFromColor(Material src, Color color, string tag)
    {
        var mat = Object.Instantiate(src);
        mat.name = $"ArtFlag_Color_{tag}";
        ApplyHdrp(mat, color, null);
        return mat;
    }

    public static Material CreateFromTexture(Material src, Texture2D tex, string tag)
    {
        var mat = Object.Instantiate(src);
        mat.name = $"ArtFlag_Tex_{tag}";
        ApplyHdrp(mat, Color.white, tex);
        return mat;
    }

    private static void ApplyHdrp(Material mat, Color color, Texture2D tex)
    {
        // Property name fallbacks
        string[] colorProps = { "_BaseColor", "_Color" };
        string[] texProps = { "_BaseColorMap", "_BaseMap", "_MainTex" };

        foreach (var prop in colorProps)
        {
            if (mat.HasProperty(prop))
            {
                mat.SetColor(prop, color);
                break;
            }
        }

        foreach (var prop in texProps)
        {
            if (mat.HasProperty(prop))
            {
                mat.SetTexture(prop, tex);
                break;
            }
        }

        // Matte fabric
        if (mat.HasProperty(Metallic))
            mat.SetFloat(Metallic, 0f);

        if (mat.HasProperty(Smoothness))
            mat.SetFloat(Smoothness, 0.15f);

        if (mat.HasProperty(SpecColor))
            mat.SetColor(SpecColor, new Color(0.04f, 0.04f, 0.04f));

        // Removes unwanted shine.
        mat.DisableKeyword("_EMISSIVE_COLOR_MAP");
        mat.DisableKeyword("_EmissiveColor");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

        // Normal map causes shimmer on a flag in windy conditions.
        string[] normalProps = { "_NormalMap", "_BumpMap" };
        foreach (var prop in normalProps)
        {
            if (mat.HasProperty(prop))
                mat.SetTexture(prop, null);
        }

        // Important for skinned mesh + wind
        mat.enableInstancing = false;
    }
}