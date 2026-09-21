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
    new[] { typeof(float), typeof(DamageSourceData), typeof(Hitmesh.Data), typeof(Vector3?) })]
public class KillMessagePatch : AttackFeedbackPatch {
    private static void Prefix(Npc __instance, out bool __state) {
        __state = WasAliveBeforeHit(__instance);
    }

    private static void Postfix(Npc __instance, ref DamageSourceData source, Hitmesh.Data hitbox, Vector3? hitPosition,
        bool __state) {
        if (!Config.EnableDamageMessage.Value) return;
        if (PluginInstance<MessageController>.Instance == null) return;
        if (!TargetCheck(source)) return;

        // Only the hit that took the unit down announces a kill. Shooting a body something else
        // killed is a corpse hit, not a kill of the player's, and must not claim one.
        if (!__state) return;

        if (IsAlive(__instance)) return;

        var point = hitPosition ?? __instance.transform.position;
        var distance = Vector3.Distance(StaticInstance<GameManager>.Instance.PlayerUnit.EyesPosition, point);
        var isFarRangeWeapon = source.sourceWeapon != null &&
                               source.sourceWeapon.holdableWeightClass is HoldableWeightClass.Rifle or HoldableWeightClass.Sniper;

        var enemyName = LocalizedUnitName(__instance);
        var weaponName = source.sourceWeapon != null ? source.sourceWeapon.weaponDefinition.LocalizedDisplayName : "";
        var exp = Convert.ToString(__instance.ExperienceOnKill);
        var isHeadshot = hitbox.shapeId.part == HitboxColliders.Parts.Head;

        Plugin.LoggingInfo("KillMessage: " + enemyName + " " + weaponName + " " + exp);

        PluginInstance<MessageController>.Instance.OnEnemyKill(enemyName, weaponName, exp, isHeadshot, distance > 20 && isFarRangeWeapon);
    }

    /// <summary>
    /// <see cref="Unit.SourceName"/> is <c>unitSO.ToString()</c>, which returns the raw English
    /// <c>displayName</c> (prefixed with the faction adjective) — the game uses it for log lines, never
    /// for UI. Its own UI shows unit names through <c>UnitSO.LocalizedIdentifier()</c>, which looks up
    /// the <c>UnitNames/&lt;asset&gt;</c> term. That method returns <c>displayName</c> unchanged when no
    /// term exists, so treat that as "not translated" and keep the faction-prefixed name we showed before
    /// rather than dropping the prefix for locales the game has no unit names for.
    /// </summary>
    private static string LocalizedUnitName(Unit unit) {
        var unitSo = unit.unitSO;
        if (unitSo == null) return unit.SourceName;

        var localized = unitSo.LocalizedIdentifier();
        if (string.IsNullOrEmpty(localized) || localized == unitSo.displayName) return unit.SourceName;

        return localized;
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
