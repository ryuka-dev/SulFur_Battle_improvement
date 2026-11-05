using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BattleImprove.Utils;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.Items;
using PerfectRandom.Sulfur.Core.World;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BattleImprove.Components.QOL;

public class LootSpawnHelper : PluginInstance<LootSpawnHelper> {
    private PluginData.DeadProtection data;

    private void Start() {
        data = DataManager.DeadProtectionData;
    }

    public void SpawnItems(PluginData.DeadProtection keptItems, Transform transform) {
        // Persist the "already returned" state as soon as the box is opened. The plugin does not
        // save on quit, so a flag set only in memory left the file holding the weapons with
        // opened=false after the game was closed, and the next session returned them again.
        // Saving with opened=true clears keptItems.weapons, so the coroutine iterates a copy.
        var weapons = new List<InventoryData>(keptItems.weapons);
        keptItems.opened = true;
        // Started before the save: if writing the file fails, the copy above is all that is left.
        this.StartCoroutine(SpawnWeapon(weapons, transform));
        DataManager.SaveDeadProtectionData();
    }

    private IEnumerator SpawnWeapon(List<InventoryData> weapons, Transform transform) {
        // The box already counts as opened, so a weapon that cannot be spawned has to go back in by
        // hand or it is gone. It may be an item from a mod that is switched off for now.
        var notSpawned = new List<InventoryData>();

        foreach (var item in weapons) {
            try {
                SpawnWeapon(item, transform);
            } catch (Exception e) {
                Plugin.Logger.LogWarning($"Could not return saved weapon {item.id}, keeping it for the next time: {e}");
                notSpawned.Add(item);
            }

            yield return new WaitForSeconds(1f);
        }

        if (notSpawned.Count == 0) yield break;

        DataManager.DeadProtectionData.weapons.AddRange(notSpawned);
        DataManager.SaveDeadProtectionData(true);
    }

    private void SpawnWeapon(InventoryData item, Transform transform) {
        InventoryData.HANDLE_LEGACY(item);
        var itemDef = item.id.GetAsset();
        if (itemDef == null) throw new InvalidOperationException("the item no longer exists");

        item.attachmentIds = RandomDeleteElement(item.attachmentIds, data.attachmentChance);
        item.enchantmentIds = RandomDeleteElement(item.enchantmentIds, data.enchantmentChance);
        // barrelChance: chance to lose the modded caliber and revert to the weapon's default.
        if (item.caliberId != CaliberTypes.None && Random.Range(0f, 1f) < data.barrelChance) {
            item.caliberId = CaliberTypes.None;
        }

        var room = StaticInstance<GameManager>.Instance.PlayerUnit.currentRoom;

        var pickup = StaticInstance<InteractionManager>.Instance.SpawnPickup(
            new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.08f),
            false,
            itemDef,
            room,
            item,
            null,
            0.75f);
        // The game refuses items that are not allowed in this build without throwing.
        if (pickup == null) throw new InvalidOperationException("the game refused to spawn the item");
    }
    
    private ItemId[] RandomDeleteElement(ItemId[] array, float chance) {
        if (array == null) return null;
        var list = new List<ItemId>(array);

        foreach (var element in list.ToList()) {
            var num = Random.Range(0f, 1f);
            Plugin.LoggingInfo("Rolling for " + element + " with chance " + chance + " got " + num);
            if (num < chance) {
                list.Remove(element);
            }
        }
        return list.ToArray();
    }
}