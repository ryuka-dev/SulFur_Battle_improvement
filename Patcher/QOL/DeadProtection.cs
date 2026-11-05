using System;
using System.Collections.Generic;
using System.Linq;
using BattleImprove.Components.QOL;
using BattleImprove.Utils;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.Items;
using PerfectRandom.Sulfur.Core.Stats;
using PerfectRandom.Sulfur.Core.World;
using PerfectRandom.Sulfur.Gameplay;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BattleImprove.Patcher.QOL;

public class DeadProtection {
    [HarmonyWrapSafe]
    [HarmonyPrefix, HarmonyPatch(typeof(LootManager), "AddToChurchCollection")]
    private static void SavePlayerEquipment(SulfurSave.Difficulty difficulty) {
        // Since 0.19.9 the game calls AddToChurchCollection on Easy, Medium and Hard.
        // Only Hard clears the paperdoll; on Easy and Medium the equipped weapons are
        // never lost, so saving them there would only duplicate them in the donation box.
        if (difficulty != SulfurSave.Difficulty.Hard) {
            Plugin.LoggingInfo("Death protection skipped: difficulty " + difficulty + " keeps equipped weapons");
            return;
        }

        Plugin.LoggingInfo("Player died, saving weapon! Quick!!!");
        var settings = DataManager.DeadProtectionData;
        // The game's insured items belong to this save slot and are written with it, and the donation
        // box hands them out, stays lit for them and clears them. The death that brought us here saves
        // the slot right after this prefix, so nothing of ours has to be written.
        var insuredItems = SulfurSaveState.active.INSURED_ITEMS;

        var equipment = StaticInstance<GameManager>.Instance.PlayerUnit.GetComponent<EquipmentManager>().EquippedHoldables;
        foreach (var inventoryItem in equipment.Select(item => item.Value)) {
            if (inventoryItem.SlotType != SlotType.Weapon) {
                continue;
            }
            // The game's own insurance returns this one through the same box right after this prefix.
            if (inventoryItem.HasAttachment(ItemIds.Attachment_Insurance.GetAsset())) {
                continue;
            }
            var itemdef = inventoryItem.itemDefinition as WeaponSO;
            Plugin.LoggingInfo("Saved item: " + itemdef.LocalizedDisplayName);
            
            Plugin.LoggingInfo("Durability Current: " + inventoryItem.DurabilityCurrent);
            inventoryItem.ModifyDurability(-inventoryItem.DurabilityCurrent * (1 - settings.weaponDurability));
            Plugin.LoggingInfo("Durability Modified: " + inventoryItem.DurabilityCurrent);

            var saved = inventoryItem.GetSerialized();
            LootSpawnHelper.RollLosses(saved, settings);
            insuredItems.Add(saved);
        }
    }

    /// <summary>
    /// Returns weapons an older version left in the plugin's own file, which every save slot shares.
    /// New deaths go through the game's insured items instead, so this runs at most once per player:
    /// the first box opened in any slot empties the list.
    /// </summary>
    [HarmonyWrapSafe]
    [HarmonyPostfix, HarmonyPatch(typeof(ChurchCollectionLootable), "Loot")]
    private static void ReturnPlayerEquipment(ChurchCollectionLootable __instance) {
        if (DataManager.DeadProtectionData.weapons.Count == 0) return;

        Plugin.LoggingInfo("Donation box opened, popping equipment back!");
        var transform = Traverse.Create(__instance).Field("lootSpawnTransform").GetValue<Transform>();
        if (transform == null) {
            transform = __instance.transform;
        }
        
        var helper = PluginInstance<LootSpawnHelper>.Instance;
        if (helper == null) {
            Plugin.LoggingInfo("LootSpawnHelper not initialized; cannot return saved equipment.");
            return;
        }

        var ItemData = DataManager.DeadProtectionData;
        helper.SpawnItems(ItemData, transform);
    }
}