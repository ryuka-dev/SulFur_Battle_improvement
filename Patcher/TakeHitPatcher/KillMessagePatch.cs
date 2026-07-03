using System;
using BattleImprove.Utils;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.Stats;
using PerfectRandom.Sulfur.Core.Units;
using PerfectRandom.Sulfur.Core.Weapons;
using UnityEngine;

namespace BattleImprove.Patcher.TakeHitPatcher;

[HarmonyWrapSafe]
[HarmonyPatch(typeof(Npc), "ReceiveDamage",
    new[] { typeof(float), typeof(DamageTypes), typeof(DamageSourceData), typeof(Hitmesh.Data), typeof(Vector3?) })]
public class KillMessagePatch : AttackFeedbackPatch {
    private static void Postfix(Npc __instance, ref DamageSourceData source, Hitmesh.Data hitbox, Vector3? hitPosition) {
        if (PluginInstance<MessageController>.Instance == null) return;
        if (!TargetCheck(source)) return;

        if (IsAlive(__instance)) return;

        var point = hitPosition ?? __instance.transform.position;
        var distance = Vector3.Distance(StaticInstance<GameManager>.Instance.PlayerUnit.EyesPosition, point);
        var isFarRangeWeapon = source.sourceWeapon != null &&
                               source.sourceWeapon.holdableWeightClass is HoldableWeightClass.Rifle or HoldableWeightClass.Sniper;

        var enemyName = __instance.SourceName;
        var weaponName = source.sourceWeapon != null ? source.sourceWeapon.weaponDefinition.LocalizedDisplayName : "";
        var exp = Convert.ToString(__instance.ExperienceOnKill);
        var isHeadshot = hitbox.shapeId.part == HitboxColliders.Parts.Head;

        Plugin.LoggingInfo("KillMessage: " + enemyName + " " + weaponName + " " + exp);

        PluginInstance<MessageController>.Instance.OnEnemyKill(enemyName, weaponName, exp, isHeadshot, distance > 20 && isFarRangeWeapon);
    }

    private static bool IsAlive(Unit unit) {
        var isAliveOrIncapacitated = unit.UnitState is UnitState.Alive or UnitState.Incapacitated;
        if (isAliveOrIncapacitated) return true;

        if (unit.LastDamagedBy.sourceUnit == null) return true;

        // Make sure the kill feedback is only shown once per enemy.
        if (AttackFeedbackPatch.KilledEnemies.Contains(unit)) return true;
        AttackFeedbackPatch.KilledEnemies.Add(unit);
        return false;
    }
}
