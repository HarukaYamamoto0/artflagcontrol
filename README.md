# ArtFlagControl

**ArtFlagControl** is a lightweight visual mod for **Mage Arena** that allows you to customize faction flag colors in a clean, HDRP-friendly way.

The mod replaces the default flag materials at runtime, giving each faction a distinct visual identity while avoiding common HDRP issues such as excessive shine, flickering, or neon colors.

## ✨ Features

* 🎨 Customizable flag colors per faction
* 🧙 Designed specifically for **Mage Arena**
* 🏳️ Supports:

    * Neutral
    * Sorcerers
    * Warlocks
* 🌑 Dark-fantasy friendly color presets
* 🚫 No shimmer / flickering on moving flags
* 🧵 Cloth-like, matte appearance (no plastic look)
* ⚙️ Simple `.cfg` configuration
* 🔧 Built with **BepInEx + Harmony**

## 🖼 Visual Philosophy

Mage Arena has:

* dark environments
* strong HDR lighting
* heavy contrast and shadows

Because of that, **pure white, pure black, or highly saturated colors look bad in HDRP**.

ArtFlagControl uses:

* muted colors
* low smoothness
* no emission
* no GPU instancing on flag materials

This results in flags that:

* remain readable at a distance
* feel like fabric
* blend naturally into the world

## 📦 Installation

1. Install **BepInEx 5** for Mage Arena
2. Download the latest release of **ArtFlagControl**
3. Extract the DLL into:

```
MageArena/BepInEx/plugins/
```

4. Launch the game once to generate the config file
5. Edit the config if desired
6. Restart the game

## ⚙️ Configuration

The config file is generated at:

```
BepInEx/config/com.harukadev.magearena.artflagcontrol.cfg
```

### Default configuration

```ini
## Settings file was created by plugin ArtFlagControlMod v1.0.0
## Plugin GUID: com.harukadev.magearena.artflagcontrol

[Colors]

## Neutral faction flag color
# Setting type: String
# Default value: #D6D6D6
NeutralHexColor = #D6D6D6

## Sorcerer faction flag color
# Setting type: String
# Default value: #4B4A6A
SorcererHexColor = #4B4A6A

## Warlock faction flag color
# Setting type: String
# Default value: #2A1E28
WarlockHexColor = #2A1E28

[General]

## Enable ArtFlagControl mod
# Setting type: Boolean
# Default value: true
Enabled = true
```

### Notes

* Colors must be valid **HEX color strings**
* Invalid values automatically fall back to safe defaults
* Changes require a game restart (for now)

## 🧠 Technical Overview

* Hooks into Mage Arena using **Harmony**
* Runs after the main menu initializes
* Locates all `FlagController` instances
* Clones the original flag material
* Applies:

    * Custom base color
    * Metallic = 0
    * Low smoothness
    * Disabled emission
    * Disabled GPU instancing
    * Disabled normal map (to prevent shimmer)
* Applies materials using `.material` to allow **per-flag customization**

## 🔌 Compatibility

* ✅ Mage Arena (Unity 2023 / HDRP)
* ✅ BepInEx 5.x
* ❌ Incompatible with:

    * `com.magearena.hostsettings` (explicitly blocked)

## 🛠 Development

* Language: **C#**
* Frameworks:

    * BepInEx
    * Harmony
* Rendering Pipeline:

    * HDRP

### Plugin ID

```
com.harukadev.magearena.artflagcontrol
```

## 🚀 Future Ideas

* Live reload when config changes
* Support for additional factions
* Preset profiles (PVP / PVE / Dark Fantasy)
* Optional normal-map strength instead of full disable

## 📜 License

MIT License
Feel free to fork, modify, and contribute.

## 👤 Author

**HarukaDev**
Backend & Game Mod Developer