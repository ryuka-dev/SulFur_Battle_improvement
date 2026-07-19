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
        __state = __instance.UnitState is UnitState.Alive or UnitState.Incapacitated;
    }

    private static void Postfix(Npc __instance, ref DamageSourceData source, bool __state) {
        if (PluginInstance<xCrossHair>.Instance == null) return;
        if (!TargetCheck(source)) return;
        // Only trigger on hits that landed on a living target this shot.
        if (!__state) return;

        PlayHitAnimation(__instance);
    }

    private static void PlayHitAnimation(Unit unit) {
        var isAliveOrIncapacitated = unit.UnitState is UnitState.Alive or UnitState.Incapacitated;

        if (Config.EnableXCrossHair.Value && isAliveOrIncapacitated) {
            PluginInstance<xCrossHair>.Instance.StartTrigger("Hit");
        } else {
            PluginInstance<xCrossHair>.Instance.StartTrigger("Kill");
        }
    }
}
