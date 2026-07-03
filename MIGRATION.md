# Battle Improvements Compatibility Migration Notes

Record of all changes made to port this mod from an older SULFUR build to the current one.

## Target environment

The environment this port was done against (visible in the BepInEx log header):

- Game: SULFUR (Steam), engine **Unity 6000.3.6** (upgraded from the earlier Unity 2022.3)
- Loader: **BepInEx 5.4.23.x** (Mono, not IL2CPP)
- Patching: HarmonyLib (ships with BepInEx.Core 5.x)
- Game managed assemblies: `D:\SteamLibrary\steamapps\common\SULFUR\Sulfur_Data\Managed`
- Target framework: `netstandard2.1`, assembly name `BattleImprove`

The engine upgrade brought a large number of game API changes that broke the mod end to end, from compile time through runtime, so each feature had to be handled individually.

---

## 1. Build environment

### 1.1 Files you must supply locally but that are NOT committed

The following three categories are not in git (copyrighted assets / binaries / decompile cache, see `.gitignore`). After cloning you must provide them yourself before the project will build:

| Path | Description | Source |
|------|-------------|--------|
| `Assets/battle_improve` | Custom AssetBundle (UI art + kill sounds, contains copyrighted assets) | Extract from the embedded resource of an old release DLL, or rebuild in Unity |
| `libs/UrGUI.dll` | Third-party open-source IMGUI library (used by the F1 menu) | Build from the UrGUI project, or extract from an old release |
| `_decompiled/` | Decompiled game DLL cache (reference only) | See section 2 |

The game's own managed DLLs do not need to be copied; the csproj references the `Managed` directory directly.

### 1.2 csproj key points (`BattleImprove.csproj`)

- `TargetFramework=netstandard2.1`, `AssemblyName=BattleImprove`
- The `SulfurManaged` property points at the game's `Managed` directory; override with `-p:SulfurManaged=...`
- References (`Private="false"`, not copied to output):
  - `$(SulfurManaged)\UnityEngine*.dll`, `$(SulfurManaged)\Unity.*.dll` (this glob also pulls in **Unity.InputSystem.dll**, needed in section 4)
  - `PerfectRandom.Sulfur.Core.dll`, `PerfectRandom.Sulfur.Gameplay.dll`
  - `Sonity.Public.Runtime.dll`, `EasySave3.dll`
  - `libs\UrGUI.dll` (`Private="true"`, must ship with the plugin)
- Embedded resources (the LogicalName must match the runtime lookup name):
  - `Assets\battle_improve` → `BattleImprove.Assets.battle_improve`
  - `Lang\English.lang`, `Lang\ChineseSimplified.lang`
- Excluded from compilation: `_decompiled\**`, `UI\UrGUI\**` (submodule source is not compiled; `libs\UrGUI.dll` is used instead)

### 1.3 Build and deploy

```
dotnet build BattleImprove.csproj -c Release
```

Copy the output `bin/Release/BattleImprove.dll` (plus `UrGUI.dll`) into `BepInEx/plugins/`. This port used Gale for management, deploying to the profile's `BepInEx/plugins/BattleImprove/`.

---

## 2. Reverse-engineering workflow

The game ships no symbols, so most changes require decompiling to confirm signatures. Use ilspycmd:

```
ilspycmd <game dll> -p -o _decompiled/<subdir>
```

Decompile Core / Gameplay / Assembly-CSharp into `_decompiled/` (already gitignored). After that, **grep `_decompiled/`** rather than re-running ilspycmd. When verifying an index-based transpiler, inspect the IL with `ilspycmd -il -t <TypeName>`.

---

## 3. Game API change summary

| Old | New |
|-----|-----|
| `Hitbox` class, `Hitbox.TakeHit(float,DamageType,DamageSourceData,Vector3)` | Removed. Damage now funnels through `Npc.ReceiveDamage(float, DamageTypes, DamageSourceData, Hitmesh.Data, Vector3?)` |
| `Hitbox.bodyPart.label == "Head"` | `Hitmesh.Data.shapeId.part == HitboxColliders.Parts.Head` |
| `DamageType` (class, `.shortLabel`) | Callback param is `DamageTypes` (enum:byte); get the label via `damageType.GetAsset().shortLabel` |
| `DamageSourceData.sourceProjectile` | `projectile`; also convenience fields `sourceUnit`, `sourceUnit.isPlayer`, `melee`, `sourceWeapon` |
| `Projectile.HandleHit`, `ProjectileSystem.ProcessSortedHits` | Removed. Hit resolution moved into `ProjectileSystem.ProcessProjectileHitsJob` (a `[BurstCompile]` IJob) |
| `InventoryData(...)` constructor | Added `int boughtForPrice` between enchantments and xSize (now 14 params) |
| `InventoryItem.CurrentCaliber.identifier` | `CurrentCaliber` is now a `CaliberTypes` enum, no `.identifier` |
| Input: Active Input Handling = Both | Changed to **Input System Package only**; all `UnityEngine.Input` calls throw |
| TMP (TextMeshPro) version | Upgraded with Unity 6; the font asset in the old AssetBundle no longer works |

---

## 4. Fixes, item by item

Each item gives: symptom → root cause → fix → verification.

### 1. Input system migration (F1 menu dead + log flooded every frame)

- **Symptom**: One `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class, but you have switched active Input handling to Input System package` per frame, with the stack pointing at `MenuController.Update`. The F1 menu won't open; the exception also aborts the rest of the logic in Update.
- **Root cause**: The game changed Player Settings' Active Input Handling to **"Input System Package (New)" only**, so the legacy `UnityEngine.Input` (GetKeyDown / GetMouseButton, etc.) throws entirely.
- **Fix**: Added `Utils/InputCompat.cs`, reimplementing `GetKeyDown(KeyCode)` and `GetMouseButton(int)` on top of `UnityEngine.InputSystem`'s `Keyboard.current` / `Mouse.current`, with an internal `KeyCode → UnityEngine.InputSystem.Key` mapping table. Replace every `UnityEngine.Input` call that **runs in Release**:
  - `Components/MenuController.cs`: menu toggle key → `InputCompat.GetKeyDown(menuKey)`
  - `Components/QOL/LootDropVFX.cs`: right-click inspect → `InputCompat.GetMouseButton(1)`
  - The `Input.GetKeyDown(Alpha1..4)` calls in `MessageController` / `KillStreakController` are inside `#if DEBUG` and are not compiled in Release, so they can be left alone.
  - `WindowHotkey` reads keys via IMGUI's `Event.current`, not `UnityEngine.Input`, so it is unaffected.
  - `Unity.InputSystem.dll` is already pulled in by the csproj `Unity.*.dll` glob; no extra reference needed.
- **Verify**: In game, F1 opens/closes the menu; the log no longer floods with `InvalidOperationException`.

### 2. Combat feedback set of four (crosshair hit marker / damage numbers / kill message / hit sound)

- **Symptom**: Compile error that the `Hitbox` type no longer exists; all four patches broken.
- **Root cause**: The `Hitbox` class and `Hitbox.TakeHit(...)` were removed; damage now funnels through `Npc.ReceiveDamage(float, DamageTypes, DamageSourceData, Hitmesh.Data, Vector3?)` (both bullets and melee pass through it).
- **Fix**: All four patches retarget to:
  ```
  [HarmonyPatch(typeof(Npc), "ReceiveDamage",
      new[] { typeof(float), typeof(DamageTypes), typeof(DamageSourceData), typeof(Hitmesh.Data), typeof(Vector3?) })]
  ```
  Shared base class `Patcher/TakeHitPatcher/AttackFeedbackPatch.cs`:
  - `ResetList()` is a postfix on `InputReader.LoadingContinue`, building the enemy list `Enemies` via `UnitManager.GetAllNpcs().Where(n => n.IsHostileTo(PlayerUnit))` and clearing `KilledEnemies`.
  - `TargetCheck(DamageSourceData source)` = `source.sourceUnit != null && source.sourceUnit.isPlayer` (only react to damage dealt by the player).
  - Headshot test: `hitbox.shapeId.part == HitboxColliders.Parts.Head`.
  - Damage-type label: `damageType.GetAsset()?.shortLabel`; melee uses `sourceWeapon.weaponDefinition.LocalizedDisplayName`; ranged additionally appends the caliber `def.caliber.GetAsset().label` and `def.projectileType`.

  Per-patch notes:
  - **Damage numbers** (`DamageInfoPatch`): Prefix records `__state = __instance.GetCurrentHealth()`; Postfix computes this hit's damage as `__state - GetCurrentHealth()`, and only calls `OnEnemyHit(type, damage)` when `> 0`.
  - **Kill message** (`KillMessagePatch`): `IsAlive(Unit)` decides death — `UnitState` of `Alive`/`Incapacitated` counts as alive; `LastDamagedBy.sourceUnit == null` is also skipped; then the static `KilledEnemies` set deduplicates so each enemy only fires a kill once. Long-range rifle/sniper detection uses `sourceWeapon.holdableWeightClass`.
  - **Hit sound** (`SoundPatch`): first filter on `source.sourceUnit.isPlayer`, only sound when `UnitState != Dead` and the target is in `Enemies`; headshot/critical use the `DamageTypes.Critical || isHeadshot` branch.
  - **Crosshair hit marker** (`CrossHairPatch`): Prefix records `__state = UnitState is Alive or Incapacitated` (whether alive before the hit), Postfix triggers the `"Hit"` or `"Kill"` animation accordingly.
- **Verify**: Hitting an enemy shows a hit marker + hit sound and the damage number accumulates; a kill pops the banner.

### 3. Bullets pass through dead bodies (target method got Burst-compiled, transpiler no longer works)

- **Symptom**: The old transpiler has no effect even when it patches.
- **Root cause**: The old implementation transpiled `Projectile.HandleHit` / `ProjectileSystem.ProcessSortedHits`, both now removed. Hit resolution now lives in `ProjectileSystem.ProcessProjectileHitsJob`, a `[BurstCompile]` IJob. **Burst jobs run as AOT-compiled native code, so a Harmony transpiler that edits managed IL has no effect on what actually runs** — the transpiler approach is a dead end.
- **Fix** (`Transpiler/RemoveDeadBodyCollisionTranspiler.cs`): switched to the managed-side equivalent — a postfix on `Npc.Die()` that sets the corpse's `hitmeshCollider.enabled = false`. Afterwards the raycasts (RaycastCommand batch) no longer report the corpse, so bullets pass straight through. Works identically for player and enemy bullets.
- **Trade-off**: With the corpse collider disabled, shooting a corpse no longer produces on-corpse hit effects (e.g. shattering a frozen corpse with gunfire). That is a niche interaction; the trade-off keeps the advertised "bullets pass through dead bodies" feature working.
- **Verify**: After an enemy dies, shoot the corpse — the bullet hits whatever is behind it.

### 4. Reverse mouse-scroll weapon switch (hard-coded index broke — a silent-failure real bug)

- **Symptom**: Feature does nothing, but no error.
- **Root cause**: The old implementation edited `Instructions()[4]` of `InputReader.SelectByScroll` (a `ble.un` compare branch, flipping it to `bge.un` reverses direction). The game inserted `ldarg.0; ldfld mouseScrollMultiplier; mul` before the compare, pushing the branch to index 7, so the old code edited the wrong instruction and the feature silently died.
- **Fix** (`Transpiler/MouseScrollTranspiler.cs`): instead of an index, use `CodeMatcher` to find the branch by opcode: `MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ble_Un_S || i.opcode == OpCodes.Ble_Un))`, then change its `opcode` to `OpCodes.Bge_Un_S`. This survives future instruction shifts.
- **Verify**: Enable reverse scroll in settings; scrolling switches weapons in the opposite direction.

### 5. Dead protection (InventoryData constructor signature change)

- **Symptom**: Compile errors that InventoryData params don't match and `CurrentCaliber.identifier` doesn't exist.
- **Root cause**: The `InventoryData` constructor inserted `int boughtForPrice` between enchantments and xSize (now 14 params); `InventoryItem.CurrentCaliber` is now a `CaliberTypes` enum (no `.identifier`); the weapon asset id uses `WeaponSO.id`.
- **Fix**:
  - `Patcher/QOL/DeadProtection.cs` constructs with the new signature:
    ```
    new InventoryData(itemdef.id, gridPosition.x, gridPosition.y, quantity, currentAmmo,
        inventoryItem.CurrentCaliber, stats.SerializedAttributeData(),
        GetSerializedAttachments(), GetSerializedEnchantments(),
        0 /*boughtForPrice*/, InventorySize.x, InventorySize.y, false, false);
    ```
  - `Components/QOL/LootSpawnHelper.cs`: get the weapon asset via `item.id.GetAsset()`; randomly dropping attachments/enchantments now operates on `ItemId[]` (`item.attachmentIds` / `item.enchantmentIds`); caliber reversion uses `item.caliberId` (`CaliberTypes`, reverting to `None` by chance); spawning uses `InteractionManager.SpawnPickup(pos, false, itemDef, room, item, null, 0.75f)` (the old `LootManager.SpawnItem` path is commented out).
- **Verify**: Die with weapons → church donation box → open it, and the weapons return according to the durability/attachment retention chances.

### 6. Loot drop VFX (how the Pickup shadow is obtained)

- **Symptom**: Can't find the "Shadow" child, so the VFX won't attach.
- **Root cause**: `Pickup` now exposes a `shadow` Transform field directly.
- **Fix** (`Patcher/QOL/LootDropPatch.cs`): use `__instance.shadow`, falling back to `transform.Find("Shadow")` if null, and skipping if still null. Pick the `LoopDropTier1..5` prefab by `ItemSO.itemQuality`.
- **Verify**: Dropped items show the beam VFX matching their quality; the VFX hides while right-click-inspecting (depends on item 1's InputCompat).

### 7. Health bar (Npc.Start injected parameter type)

- **Symptom**: Compile error over the `__instance` type.
- **Root cause**: The patch target is `Npc.Start`, so the injected `__instance` must be declared as `Npc`.
- **Fix** (`Patcher/QOL/HealthBarPatch.cs`): change `AddDebugFrame`'s parameter `Player __instance` → `Npc __instance`. The bar refresh logic is patched on `UnitDebugFrame.Update` (a prefix returning `false` takes over).
- **Verify**: Enemies have a health bar above them that hides on death.

### 8. Save file abort during init (took down #1/2/3/4 and the menu too)

- **Symptom**: Plugin init terminated partway with an exception, so combat feedback and the menu never loaded.
- **Root cause**: In `Utils/SaveManager.cs`, the legacy save-migration step `ES3.KeyExists("CmPlugin", SulfurSave.Imp.saveSettings)` threw an NRE, and the catch block's `throw;` rethrew it, which aborted the `Plugin.InitPluginGameobject` coroutine (which still had to load the AttackFeedback prefab and the menu afterwards).
- **Fix**: Make the "legacy vanilla-save migration" best-effort — wrap it in try/catch, log on failure without throwing; the main load also only calls `LoadDefaults()` on failure and never rethrows, so the rest of init runs. EasySave3 (ES3) requires referencing `Managed\EasySave3.dll` in the csproj.
- **Verify**: On first run the log shows "Save data not found, creating new one...", and the rest of the features load normally.

### 9. TMP text not showing / CJK turning blank (kill message, damage numbers)

- **Symptom**:
  - (a) Initially: the very first layout pass throws `NullReferenceException` in `TMPro.MaterialReference..ctor`, and text doesn't render at all.
  - (b) After falling back to the default font: English shows, but CJK weapon names (e.g. "80号台风") turn blank, and the log floods with `The character 号 was not found in the [Merriweather-Regular SDF] font asset...`.
- **Root cause**: Our UI (kill banner, damage numbers) lives in the AssetBundle, whose font asset was packed against an older TMP version. Under the game's current TMP (Unity 6) it has lost its material/atlas, so rendering NREs; and the game's default font Merriweather is Latin-only and can't draw CJK.
- **Fix**: Added `Utils/TmpFontFixer.cs` — instead of shipping a font, **reuse the font the game is already using to render localized text**:
  1. Iterate the active `TMP_Text` in the scene, pick one that can draw CJK (`HasCharacter('号'/'击'/'杀'/'测'/'的')`) and has a valid material/atlas, and assign it to all of our `TMP_Text`.
  2. The localized font may not be loaded yet at plugin init, so `MessageController.OnEnemyKill/OnEnemyHit` retry lazily: `if (!TmpFontFixer.Resolved) TmpFontFixer.Apply(gameObject);`, which is just a bool check once locked.
  3. Dynamically created damage rows (`GetDamageInfo`) call `Apply` right after instantiation.
  4. `PrefabManager` also calls `Apply` once after loading the AttackFeedback / kill-message-style prefabs.
- **Verify**: CJK weapon names show correctly in the kill banner; the log no longer has MaterialReference NREs or "not found in ... font asset" warnings; it prints `TmpFontFixer: locked game font '...'`.

### 10. EXP share (no API breakage, verification only)

- **Logic** (`Patcher/QOL/ExpSharePatch.cs`): a postfix on `Npc.GiveExperience` grants the player's currently **holstered** secondary weapon `ExperienceOnKill * Config.Proportion` experience. Uses `PlayerUnit.lastUsedWeapon.inventorySlot` and `EquipmentManager.EquippedHoldables[slot].AddExperience(float)`.
- **Verify**: Carry two weapons, get a kill with the primary, switch to the secondary and check that its experience went up.

---

## 5. Remaining items and caveats

- **Copyrighted assets not committed**: `Assets/battle_improve` (UI art + kill sounds) and `libs/UrGUI.dll` are gitignored; after cloning you must supply them to build. `EasySave3.dll` is referenced by the csproj from the game's `Managed` directory and is not committed.
- **UI/UrGUI submodule**: `.gitmodules` declares it, but it was never registered as a gitlink; an existing local clone is enough to build (the csproj uses `libs/UrGUI.dll` and does not compile the submodule source). To let others get it via `git submodule update --init`, the submodule needs to be registered properly.
- **Harmony003 warnings**: the build produces 5, all analyzer false positives (method calls / struct reads inside ternary expressions misread as parameter assignment); harmless.
- **Bullets-through-corpses trade-off**: see section 4, item 3 (corpse collider disabled, can't shatter a frozen corpse).
- **The decompile cache** `_decompiled/` is reference-only and gitignored.

---

## Appendix: changed files

Added: `BattleImprove.csproj`, `.gitignore`, `Utils/InputCompat.cs`, `Utils/TmpFontFixer.cs`, this file.

Changed: `Utils/SaveManager.cs`, `Utils/PrefabManager.cs`, `Components/MenuController.cs`, `Components/QOL/LootDropVFX.cs`, `Components/QOL/LootSpawnHelper.cs`, `Components/AttackFeedback/KillMessage/MessageController.cs`, `Patcher/QOL/DeadProtection.cs`, `Patcher/QOL/HealthBarPatch.cs`, `Patcher/QOL/LootDropPatch.cs`, `Patcher/TakeHitPatcher/*` (AttackFeedbackPatch / CrossHairPatch / DamageInfoPatch / KillMessagePatch / SoundPatch), `Transpiler/MouseScrollTranspiler.cs`, `Transpiler/RemoveDeadBodyCollisionTranspiler.cs`, `UI/InGame/WindowAttackFeedback.cs`.
