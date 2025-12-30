using BepInEx.Configuration;
using UnityEngine;

namespace ArtFlagControl.Config;

public sealed class FlagConfig(ConfigFile config)
{
    public ConfigEntry<bool> Enabled { get; } = BindBool(
        config,
        "General",
        "Enabled",
        true,
        "Enable ArtFlagControl mod"
    );

    private ConfigEntry<string> NeutralHex { get; } = BindColor(
        config,
        "Neutral",
        "#D6D6D6",
        "Neutral faction flag color"
    );

    private ConfigEntry<string> SorcererHex { get; } = BindColor(
        config,
        "Sorcerer",
        "#4B4A6A",
        "Sorcerer faction flag color"
    );

    private ConfigEntry<string> WarlockHex { get; } = BindColor(
        config,
        "Warlock",
        "#2A1E28",
        "Warlock faction flag color"
    );

    public Color NeutralColor => Parse(NeutralHex.Value, new Color(0.84f, 0.84f, 0.84f));
    public Color SorcererColor => Parse(SorcererHex.Value, new Color(0.29f, 0.29f, 0.42f));
    public Color WarlockColor => Parse(WarlockHex.Value, new Color(0.16f, 0.12f, 0.15f));

    private static ConfigEntry<bool> BindBool(
        ConfigFile cfg,
        string section,
        string key,
        bool def,
        string desc)
    {
        return cfg.Bind(section, key, def, desc);
    }

    private static ConfigEntry<string> BindColor(
        ConfigFile cfg,
        string section,
        string def,
        string desc)
    {
        return cfg.Bind(
            "Colors",
            $"{section}HexColor",
            def,
            desc
        );
    }

    private static Color Parse(string hex, Color fallback)
    {
        return ColorUtility.TryParseHtmlString(hex, out var c)
            ? c
            : fallback;
    }
}