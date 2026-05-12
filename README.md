# HabitatInfo

A mod for Voodoo Fishin' that displays the current habitat tags when your bobber lands in the water.

## What it does

When you cast your line, a notification appears showing the habitat tags of the fishing area — exactly the ones shown in the fish journal. The display disappears automatically when you reel in.

Habitat names are displayed in your current game language.

## Example

> Slow Water | Woody | Shoreline

## Installation

1. Install BepInEx IL2CPP for Voodoo Fishin'
2. Drop `HabitatInfo.dll` into `BepInEx/plugins/`
3. Launch the game


## Changelog

### v1.0.3
- Fixed: internal location tags (e.g. Pier) are now correctly filtered out
- Only habitat tags that appear in the fish journal are shown

### v1.0.2
- Habitat tags now display in the player's current game language
- Fixed tag names showing descriptions and rich text formatting

### v1.0.1
- Fixed multiplayer compatibility

### v1.0.0
- Initial release

## Credits

- **mahyknaps** — mod author
- **Claude (Anthropic AI)** — coding assistance
