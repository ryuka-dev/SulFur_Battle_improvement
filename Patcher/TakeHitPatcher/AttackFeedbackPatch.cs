using System.Collections.Generic;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.Units;
using PerfectRandom.Sulfur.Core.Weapons;

namespace BattleImprove.Patcher.TakeHitPatcher;

// Base for all combat-feedback patches. The game reworked its damage pipeline:
// the old single choke point Hitbox.TakeHit(float, DamageType, DamageSourceData, Vector3)
// no longer exists. Damage now funnels through Npc.ReceiveDamage(float, DamageTypes,
// DamageSourceData, Hitmesh.Data, Vector3?), which every enemy hit (bullet or melee)
// passes through. Patching Npc directly also removes the need to filter out player/Breakable.
public class AttackFeedbackPatch {
    internal static List<Unit> KilledEnemies = new();
    // The weapon of the player's latest hit on each enemy, for a kill reported after the hit.
    internal static Dictionary<Unit, Weapon> LastHitWeapon = new();

    // The game's own "leave the loading screen" step. The continue key is only one caller of it, so
    // hooking the key's input callback misses every other way past the screen.
    [HarmonyWrapSafe]
    [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.ContinueFromTransition))]
    private static void ResetKilledEnemies() {
        KilledEnemies = new List<Unit>();
        LastHitWeapon = new Dictionary<Unit, Weapon>();
    }

    // Only react to damage dealt by the player.
    protected static bool TargetCheck(DamageSourceData source) {
        return source.sourceUnit != null && source.sourceUnit.isPlayer;
    }

    /// <summary>
    /// Whether the unit was still standing when the hit landed. Bullets no longer stop on corpses but
    /// still resolve against them (that is what makes organs drop), so feedback patches have to tell a
    /// killing blow apart from a shot into a body that was already dead.
    /// </summary>
    internal static bool WasAliveBeforeHit(Npc npc) {
        return npc.UnitState is UnitState.Alive or UnitState.Incapacitated;
    }
}
