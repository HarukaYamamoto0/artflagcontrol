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
            "Enables or disables the ArtFlagControl mod. If disabled, flags will return to the game default upon restarting the match."
        );

        UseCache = config.Bind(
            "General",
            "UseCache",
            true,
            "Enables local cache for textures via URL. This avoids repeated downloads and prevents rate limiting from sites like Imgur."
        );

        DebugMode = config.Bind(
            "General",
            "DebugMode",
            false,
            "Enables detailed logs in the console. Useful for identifying why a texture is not being applied or discovering internal field names."
        );

        NeutralHex = config.Bind(
            "Colors",
            "NeutralHexColor",
            "#D6D6D6",
            "Hexadecimal color for the Neutral faction. Used only if ImagePath and ImageUrl are empty. Example: #FF0000 for red."
        );

        SorcererHex = config.Bind(
            "Colors",
            "SorcererHexColor",
            "#4B4A6A",
            "Hexadecimal color for the Sorcerer faction. Used only if ImagePath and ImageUrl are empty."
        );

        WarlockHex = config.Bind(
            "Colors",
            "WarlockHexColor",
            "#2A1E28",
            "Hexadecimal color for the Warlock faction. Used only if ImagePath and ImageUrl are empty."
        );

        NeutralImagePath = config.Bind(
            "Textures",
            "NeutralImagePath",
            "",
            "Local path to an image (.png or .jpg) for the Neutral faction. Priority: ImageUrl > ImagePath > HexColor. Example: C:\\MyImages\\flag.png"
        );

        SorcererImagePath = config.Bind(
            "Textures",
            "SorcererImagePath",
            "",
            "Local path to an image (.png or .jpg) for the Sorcerer faction. Example: D:\\Games\\MageArena\\my_texture.jpg"
        );

        WarlockImagePath = config.Bind(
            "Textures",
            "WarlockImagePath",
            "",
            "Local path to an image (.png or .jpg) for the Warlock faction."
        );

        NeutralImageUrl = config.Bind(
            "DynamicTextures",
            "NeutralImageUrl",
            "",
            "Direct URL to an image on the internet for the Neutral faction. Has the highest priority. Example: https://i.imgur.com/image_link.png"
        );

        SorcererImageUrl = config.Bind(
            "DynamicTextures",
            "SorcererImageUrl",
            "https://www.harukadev.com/img/public/sorceres_banner.jpg",
            "Direct URL to an image on the internet for the Sorcerer faction."
        );

        WarlockImageUrl = config.Bind(
            "DynamicTextures",
            "WarlockImageUrl",
            "https://www.harukadev.com/img/public/warlocks_banner.jpg",
            "Direct URL to an image on the internet for the Warlock faction."
        );
    }

    private static Color Parse(string hex, Color fallback)
    {
        return ColorUtility.TryParseHtmlString(hex, out var c)
            ? c
            : fallback;
    }
}