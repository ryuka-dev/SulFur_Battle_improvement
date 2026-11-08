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
    private static void Prefix(Npc __instance, out (float Health, bool Alive) __state) {
        __state = (__instance.GetCurrentHealth(), WasAliveBeforeHit(__instance));
    }

    private static void Postfix(Npc __instance, float damage, ref DamageSourceData source,
        (float Health, bool Alive) __state, bool __runOriginal) {
        if (IsReportedByHost(__runOriginal)) return;
        if (PluginInstance<MessageController>.Instance == null) return;
        if (!TargetCheck(source)) return;

        if (!Config.EnableDamageMessage.Value) return;

        // Read through locals: the Harmony analyzer reports any member access on a non-ref patch
        // parameter as an assignment to it.
        var (healthBeforeHit, wasAliveBeforeHit) = __state;

        // Damage numbers for hits on an already dead body are opt-in.
        if (!wasAliveBeforeHit && !Config.EnableDeadUnitFeedback.Value) return;

        var dealt = healthBeforeHit - __instance.GetCurrentHealth();
        // A co-op client whose hits are not reported back (a co-op mod without that API) hands its hit to
        // the host and skips the game's method, so health has not moved yet. The amount passed in here is
        // the closest estimate, up to the health the enemy has left.
        if (!__runOriginal && !__instance.isInvulnerable) dealt = Math.Min(damage, healthBeforeHit);
        if (dealt <= 0) return;

        PluginInstance<MessageController>.Instance.OnEnemyHit(ClientHitFeedback.DescribeDamage(source.damageType, source.sourceWeapon, source.melee), Convert.ToInt32(dealt));
    }
}
