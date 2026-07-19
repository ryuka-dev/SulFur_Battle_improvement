using System;
using BattleImprove.Utils;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.Stats;
using PerfectRandom.Sulfur.Core.Units;
using UnityEngine;

namespace BattleImprove.Patcher.TakeHitPatcher;

[HarmonyWrapSafe]
[HarmonyPatch(typeof(Npc), "ReceiveDamage",
    new[] { typeof(float), typeof(DamageSourceData), typeof(Hitmesh.Data), typeof(Vector3?) })]
public class DamageInfoPatch : AttackFeedbackPatch {
    private static void Prefix(Npc __instance, out float __state) {
        __state = __instance.GetCurrentHealth();
    }

    private static void Postfix(Npc __instance, ref DamageSourceData source, float __state) {
        if (PluginInstance<MessageController>.Instance == null) return;
        if (!TargetCheck(source)) return;

        if (!Config.EnableDamageMessage.Value) return;

        var damage = __state - __instance.GetCurrentHealth();
        if (damage <= 0) return;

        var def = source.sourceWeapon != null ? source.sourceWeapon.weaponDefinition : null;
        string type;
        if (source.melee && def != null) {
            type = def.LocalizedDisplayName;
        } else {
            type = source.damageType.GetAsset() != null ? source.damageType.GetAsset().shortLabel : source.damageType.ToString();
            if (def != null) {
                var caliber = def.caliber.GetAsset();
                type += " " + (caliber != null ? caliber.label : def.caliber.ToString()) + " " + def.projectileType;
            }
        }

        PluginInstance<MessageController>.Instance.OnEnemyHit(type, Convert.ToInt32(damage));
    }
}
