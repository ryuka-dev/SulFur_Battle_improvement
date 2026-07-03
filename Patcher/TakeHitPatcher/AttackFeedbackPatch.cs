using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.Input;
using PerfectRandom.Sulfur.Core.Units;

namespace BattleImprove.Patcher.TakeHitPatcher;

// Base for all combat-feedback patches. The game reworked its damage pipeline:
// the old single choke point Hitbox.TakeHit(float, DamageType, DamageSourceData, Vector3)
// no longer exists. Damage now funnels through Npc.ReceiveDamage(float, DamageTypes,
// DamageSourceData, Hitmesh.Data, Vector3?), which every enemy hit (bullet or melee)
// passes through. Patching Npc directly also removes the need to filter out player/Breakable.
public class AttackFeedbackPatch {
    internal static Npc[] Enemies;
    internal static List<Unit> KilledEnemies;

    [HarmonyWrapSafe]
    [HarmonyPostfix, HarmonyPatch(typeof(InputReader), "LoadingContinue")]
    private static void ResetList() {
        Enemies = StaticInstance<UnitManager>.Instance.GetAllNpcs()
            .Where(npc => npc.IsHostileTo(StaticInstance<GameManager>.Instance.PlayerUnit)).ToArray();
        KilledEnemies = new List<Unit>();
    }

    // Only react to damage dealt by the player.
    protected static bool TargetCheck(DamageSourceData source) {
        return source.sourceUnit != null && source.sourceUnit.isPlayer;
    }
}
