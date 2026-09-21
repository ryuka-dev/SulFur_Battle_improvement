# Battle Improvements
Add some feature to improve the sulfur's battle experience

![demo](https://raw.githubusercontent.com/CmmmmmmLau/SulFur_Battle_improvement/refs/heads/master/docs/preview.png)

## Maintenance status
This is a temporary maintenance fork. The original author has been contacted, and this fork provides interim maintenance to keep the mod working on the current SULFUR build. It is temporary by design: once the original author ships an official update, this fork will be retired in favor of it.

## Features
All features can be turned off in the in-game menu (default key F1) or by cfg in BepInEx/Config folder

Battlefield 1/5 style kill message

![hit](https://github.com/CmmmmmmLau/SulFur_Battle_improvement/blob/master/docs/killmessage.gif?raw=true)

Loot drop effect, never miss your loot again.

![lootvfx](https://github.com/CmmmmmmLau/SulFur_Battle_improvement/blob/master/docs/lootdrop_vfx.gif?raw=true)

You can get your weapons back from the donation box.

Of course... you have to lose something...

![deadprotection](https://raw.githubusercontent.com/CmmmmmmLau/SulFur_Battle_improvement/refs/heads/master/docs/deadprotection.gif)

Bullet behavior reworked. Now your bullets won't be blocked by dead body, and neither will theirs.

![deadbody](https://github.com/CmmmmmmLau/SulFur_Battle_improvement/blob/master/docs/deadbodycollision.gif?raw=true)

Enables the health bar in the dev tools.

Every time you gain some experience on current weapon, your second weapon will also gain some experience.


## Configurable
- In Game config menu, default open key is F1
- Feature toggles, under `Feature Toggles` in that menu: every feature can be switched on or off
  without leaving the game. The menu edits the same `BepInEx/config` file a mod manager or an
  external config UI would, so existing settings are read, not replaced. Combat feedback, the health
  bar, experience share and the loot VFX apply immediately; the three entries marked `*` are applied
  on the next launch.
- Volume and distance of the hit sound.
- Color of the hitmarker.
- Volume of the kill message.
- How many weapon durability you will lose when you get your weapon back.
- The chance of the attachment and enhancement will be lost when you get your weapon back.

## Localization
Menu text lives in `thunderstore/lang/<code>.json`, one file per language the game ships (`en sv fr it de es pt ru pl ja ko zh-CN tr ar`). The files are published next to the DLL, which is where SULFUR's localization helper looks for them, so the same files also localize the config page for players who have an external config UI installed - the plugin itself does not depend on one.

`en.json` is the base and must stay complete; every other language is overlaid on top of it key by key, so an untranslated term shows English rather than an empty line. Each lookup in the code also passes the English text as a fallback, verbatim as it appears in `en.json`, so a package installed without the lang folder degrades to English instead of raw keys.

Build with `-p:SulfurPluginDir=<installed plugin folder>` to copy the DLL and the lang files into a local test install.

## Game Compatibility
**Only Latest version of Sulfur is supported.**
For older version, please try the older released mod.

## Manual Installation
1. Download ant install the bepinex 5 from [github](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.2)
2. Download this mod from github [release page](https://github.com/CmmmmmmLau/SulFur_Battle_improvement/releases)
3. Unzip the file in ``SULFUR\BepInEx\plugins\`` folder.
4. Enjoy

## What's Next?
- ~~Death Protection~~ Done
- ~~battlefield 5 style~~ Done
- ~~Loot light beam~~ Done
- Remove Friendly Fire

# Know Issue
Currently only able to record player's bullet damage. Other type of damage date not include the damage source, therefore it will take more work to try to record it.

# Copyright
Audio/Texture: Electronic Arts, DICE

UI library: Hirashi3630/UrGUI

