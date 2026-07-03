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
        this.StartCoroutine(SpawnWeapon(keptItems.weapons, transform));
        keptItems.opened = true;
    }

    private IEnumerator SpawnWeapon(List<InventoryData> weapons, Transform transform) {
        
        foreach (var item in weapons) {
            var itemDef = item.id.GetAsset();
            item.attachmentIds = RandomDeleteElement(item.attachmentIds, data.attachmentChance);
            item.enchantmentIds = RandomDeleteElement(item.enchantmentIds, data.enchantmentChance);
            // barrelChance: chance to lose the modded caliber and revert to the weapon's default.
            if (item.caliberId != CaliberTypes.None && Random.Range(0f, 1f) < data.barrelChance) {
                item.caliberId = CaliberTypes.None;
            }

            var room = StaticInstance<GameManager>.Instance.PlayerUnit.currentRoom;
            
            // var pickUp = StaticInstance<LootManager>.Instance.SpawnItem(itemDef, new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.08f), LootSpawnBehaviour.None, false);
            var pickUp = StaticInstance<InteractionManager>.Instance.SpawnPickup(
                new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.08f),
                false,
                itemDef,
                room,
                item,
                null,
                0.75f);
            yield return new WaitForSeconds(1f);
        }
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