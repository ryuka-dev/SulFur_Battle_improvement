using System.Linq;
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
public class SoundPatch {
    private static PluginData.AttackFeedback data;

    private static void Postfix(Npc __instance, ref DamageSourceData source, Hitmesh.Data hitbox, Vector3? hitPosition) {
        if (PluginInstance<HitSoundEffect>.Instance == null) return;
        // Only play the hit sound for the player's own hits.
        if (source.sourceUnit == null || !source.sourceUnit.isPlayer) return;

        data ??= DataManager.AttackFeedbackData;

        if (__instance.UnitState == UnitState.Dead) return;
        if (AttackFeedbackPatch.Enemies == null || !AttackFeedbackPatch.Enemies.Contains(__instance)) return;

        var player = StaticInstance<GameManager>.Instance.PlayerUnit;
        var collisionPoint = hitPosition ?? __instance.transform.position;
        var distance = Vector3.Distance(player.EyesPosition, __instance.transform.position);

        var isHeadshot = hitbox.shapeId.part == HitboxColliders.Parts.Head;

        if (source.damageType == DamageTypes.Critical || isHeadshot) {
            var soundPosition = Vector3.LerpUnclamped(player.transform.position, collisionPoint, data.indicatorDistanceHeadShoot);
            PluginInstance<HitSoundEffect>.Instance.PlayHitSound(soundPosition, true, volume: data.indicatorVolume);
        } else if (distance < 20) {
            var soundPosition = Vector3.LerpUnclamped(player.transform.position, collisionPoint, data.indicatorDistance);
            PluginInstance<HitSoundEffect>.Instance.PlayHitSound(soundPosition, false, volume: data.indicatorVolume);
        } else {
            var soundPosition = Vector3.LerpUnclamped(player.transform.position, collisionPoint, data.indicatorDistanceFar);
            PluginInstance<HitSoundEffect>.Instance.PlayHitSound(soundPosition, false, true, volume: data.indicatorVolume);
        }
    }
}
