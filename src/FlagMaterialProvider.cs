using UnityEngine;

namespace ArtFlagControl;

public static class FlagMaterialProvider
{
    private static bool _init;

    private static Material _neutral;
    private static Material _sorcerer;
    private static Material _warlock;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int Metallic = Shader.PropertyToID("_Metallic");
    private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
    private static readonly int SpecColor = Shader.PropertyToID("_SpecularColor");
    private static readonly int NormalMap = Shader.PropertyToID("_NormalMap");

    public static void Init(
        Material baseMat,
        Color neutral,
        Color sorcerer,
        Color warlock)
    {
        if (_init || baseMat == null)
            return;

        _init = true;

        _neutral = Clone(baseMat, neutral, "Neutral");
        _sorcerer = Clone(baseMat, sorcerer, "Sorcerer");
        _warlock = Clone(baseMat, warlock, "Warlock");
    }

    public static Material Get(int index) => index switch
    {
        1 => _sorcerer,
        2 => _warlock,
        _ => _neutral
    };

    private static Material Clone(Material src, Color color, string tag)
    {
        var mat = Object.Instantiate(src);
        mat.name = $"ArtFlag_{tag}";

        ApplyHdrp(mat, color);
        return mat;
    }

    private static void ApplyHdrp(Material mat, Color color)
    {
        // Base Color
        if (mat.HasProperty(BaseColor))
            mat.SetColor(BaseColor, color);

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

        // Normal map causes shimmer on flag in windy conditions.
        if (mat.HasProperty(NormalMap))
            mat.SetTexture(NormalMap, null);

        // Important for skinned mesh + wind
        mat.enableInstancing = false;
    }
}