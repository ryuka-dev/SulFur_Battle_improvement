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
public class CrossHairPatch : AttackFeedbackPatch {
    private static void Prefix(Npc __instance, out bool __state) {
        __state = WasAliveBeforeHit(__instance);
    }

    private static void Postfix(Npc __instance, ref DamageSourceData source, bool __state) {
        if (PluginInstance<xCrossHair>.Instance == null) return;
        if (!TargetCheck(source)) return;
        // Hits on a body that was already dead only animate the crosshair when the player asked for it.
        if (!__state && !Config.EnableDeadUnitFeedback.Value) return;

        PlayHitAnimation(__instance, __state);
    }

    private static void PlayHitAnimation(Unit unit, bool wasAliveBeforeHit) {
        // Only the hit that actually took the unit down plays the kill animation; a hit on an
        // already dead body is an ordinary hit.
        var killedByThisHit = wasAliveBeforeHit && unit.UnitState is not (UnitState.Alive or UnitState.Incapacitated);

        PluginInstance<xCrossHair>.Instance.StartTrigger(killedByThisHit ? "Kill" : "Hit");
    }
}
