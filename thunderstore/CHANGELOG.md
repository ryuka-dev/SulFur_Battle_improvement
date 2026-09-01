# Changelog

## 1.5.5
Fixed the kill banner losing its text partway through a session.

The mod adopts the game's own font when it starts up, which happens while the main menu is still on screen. That font belongs to the menu, and the game frees it once you load into a level - after which the kill banner, the damage numbers and the damage counter were left with no font at all, drew nothing, and threw an error on every kill for the rest of the session. The font is now re-acquired whenever the one in use goes away.

Enemy names in the kill banner now follow your game language instead of always being English. They also match how the game labels units elsewhere, so some lose the faction word they used to carry: "Goblin Spearman" is now just "Spearman". Names the game ships no translation for are unchanged.

## 1.5.4
Fixed hit sounds, the hit marker and the kill message staying dead for players whose plugin save file had been damaged by an earlier release.

A version of the mod that failed to start could write empty settings blocks into `BattleImproveSaveData` when you died or left to the menu. Those empty blocks were loaded back on every later launch, so 1.5.3 still had no settings to work with and threw on every hit. The file is now repaired automatically on startup, and settings are never written before they have been read.

The F1 menu and the loot helper no longer disappear when the combat-feedback prefab fails to load.

## 1.5.3
Compatibility fix for SULFUR 0.18.5.

Combat feedback (hit marker, damage numbers, kill message, hit sound) rewired to the reworked damage pipeline (ReceiveDamage signature changed).

Fixed loot VFX and dead protection failing to start on the new save system.

Dead Protection now defaults to off: SULFUR 0.18.5 has a built-in insured-items system (church collection) that already returns your gear on death. Existing configs are left as-is; turn it on again if you want the mod's version.

Fixed a per-kill stutter caused by repeated font lookups when the game runs in a non-CJK language.

## 1.5.2
Compatibility fix for the current SULFUR build (Unity 6).

Combat feedback (hit marker, damage numbers, kill message, hit sound) rewired to the reworked damage pipeline.

Bullets-through-corpses and reverse mouse scroll restored.

Kill message / damage text render again, using the game's own font (CJK included).

F1 menu fixed for the new input system.

Fixed a save-migration error that could abort plugin init.

## 1.5.1
v0.11.2 compatibility fix.

## 1.5.0
v0.10.12 compatibility fix.
Barrel also in dead proection now
Loot VFX now fixde to const size

## 1.4.3
v0.10.4 compatibility fix.

## 1.4.2
Corrected the death protection logic to match the textual description, i.e. the value is now set to the chance of losing enchantments and accessories instead of the chance of keeping them.

## 1.4.1
v0.10.2 compatibility fix.

## 1.4.0
v0.10.1 compatibility fix.

fix when player die with grenade will cause game soft lock

Add battlefield 5 style kill message

## 1.3.0
Add Dead Protection

Bullet behavior reworked

## 1.2.0

Add loot beams

## 1.1.0

Add Menu

Add i18n

Add configurable feature - all configurations will save into game save file

Code Refactoring - Further split attack feedback related patches into different patches

## 1.0.4
Updated v0.9.17 compatibility.

Hit crosshair animation adjust

## 1.0.3
Now asset bundle embedded with dll file

Bug fix: damage count only trigger when kill enemy

## 1.0.2
Speed up the kill message animation

Add configurable file for all feature to turn on/off

## 1.0.1
Now damage info will merged if have same damage type

## 1.0.0
Release
