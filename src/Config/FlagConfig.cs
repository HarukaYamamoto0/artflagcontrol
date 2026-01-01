using BepInEx.Configuration;
using UnityEngine;

namespace ArtFlagControl.Config;

public sealed class FlagConfig
{
    public ConfigEntry<bool> Enabled { get; }
    public ConfigEntry<bool> UseCache { get; }
    public ConfigEntry<bool> DebugMode { get; }
    public ConfigEntry<string> NeutralHex { get; }
    public ConfigEntry<string> SorcererHex { get; }
    public ConfigEntry<string> WarlockHex { get; }

    public ConfigEntry<string> NeutralImagePath { get; }
    public ConfigEntry<string> SorcererImagePath { get; }
    public ConfigEntry<string> WarlockImagePath { get; }

    public ConfigEntry<string> NeutralImageUrl { get; }
    public ConfigEntry<string> SorcererImageUrl { get; }
    public ConfigEntry<string> WarlockImageUrl { get; }

    public Color NeutralColor => Parse(NeutralHex.Value, new Color(0.84f, 0.84f, 0.84f));
    public Color SorcererColor => Parse(SorcererHex.Value, new Color(0.29f, 0.29f, 0.42f));
    public Color WarlockColor => Parse(WarlockHex.Value, new Color(0.16f, 0.12f, 0.15f));

    public FlagConfig(ConfigFile config)
    {
        Enabled = config.Bind(
            "General",
            "Enabled",
            true,
            "Toggle the ArtFlagControl mod. When disabled, flag appearances will revert to game defaults upon match restart."
        );

        UseCache = config.Bind(
            "General",
            "UseCache",
            true,
            "Enable local caching for textures downloaded via URL. This reduces bandwidth usage and prevents rate limiting from external image hosts."
        );

        DebugMode = config.Bind(
            "General",
            "DebugMode",
            false,
            "Enable verbose logging in the console. Useful for troubleshooting texture application issues and identifying internal object names."
        );

        NeutralHex = config.Bind(
            "Colors",
            "NeutralHexColor",
            "#D6D6D6",
            "Hexadecimal color code for the Neutral faction. This color is applied only if both ImagePath and ImageUrl are empty. Example: #FFFFFF for white."
        );

        SorcererHex = config.Bind(
            "Colors",
            "SorcererHexColor",
            "#4B4A6A",
            "Hexadecimal color code for the Sorcerer faction. This color is applied only if both ImagePath and ImageUrl are empty."
        );

        WarlockHex = config.Bind(
            "Colors",
            "WarlockHexColor",
            "#2A1E28",
            "Hexadecimal color code for the Warlock faction. This color is applied only if both ImagePath and ImageUrl are empty."
        );

        NeutralImagePath = config.Bind(
            "Textures",
            "NeutralImagePath",
            "",
            "The local file path to a custom image (.png or .jpg) for the Neutral faction. Priority order: ImageUrl > ImagePath > HexColor."
        );

        SorcererImagePath = config.Bind(
            "Textures",
            "SorcererImagePath",
            "",
            "The local file path to a custom image (.png or .jpg) for the Sorcerer faction."
        );

        WarlockImagePath = config.Bind(
            "Textures",
            "WarlockImagePath",
            "",
            "The local file path to a custom image (.png or .jpg) for the Warlock faction."
        );

        NeutralImageUrl = config.Bind(
            "DynamicTextures",
            "NeutralImageUrl",
            "",
            "Direct web URL to an image for the Neutral faction. This setting has the highest priority over local paths and hex colors."
        );

        SorcererImageUrl = config.Bind(
            "DynamicTextures",
            "SorcererImageUrl",
            "https://www.harukadev.com/img/public/sorceres_banner.jpg",
            "Direct web URL to an image for the Sorcerer faction."
        );

        WarlockImageUrl = config.Bind(
            "DynamicTextures",
            "WarlockImageUrl",
            "https://www.harukadev.com/img/public/warlocks_banner.jpg",
            "Direct web URL to an image for the Warlock faction."
        );
    }

    private static Color Parse(string hex, Color fallback)
    {
        return ColorUtility.TryParseHtmlString(hex, out var c)
            ? c
            : fallback;
    }
}