using HarmonyLib;
using PerfectRandom.Sulfur.Core.Units;

namespace BattleImprove.Transpiler;

// "Bullets pass through dead bodies."
//
// This used to be done by transpiling Projectile.HandleHit / ProjectileSystem.ProcessSortedHits
// to skip the bounce/stop logic when the hit hitbox belonged to a dead NPC. In the current game
// both of those methods are gone: projectile hit resolution now lives inside
// ProjectileSystem.ProcessProjectileHitsJob, which is a [BurstCompile] IJob. Burst jobs execute as
// AOT-compiled native code, so a Harmony transpiler on the managed method has no effect on what
// actually runs — the transpiler approach is no longer possible.
//
// The equivalent managed-side behaviour is to disable a corpse's hit collider the moment it dies.
// The projectile raycast (RaycastCommand batch) then no longer reports the corpse, so the bullet
// simply continues on to whatever is behind it. This works identically for the player's and
// enemies' bullets.
//
// Trade-off vs. the old transpiler: because the corpse collider is disabled, shooting a corpse no
// longer produces on-corpse hit effects (e.g. shattering a frozen corpse with gunfire). That was a
// niche interaction; passing bullets through dead bodies is the advertised feature and is restored.
public class RemoveDeadBodyCollisionTranspiler {
    [HarmonyWrapSafe]
    [HarmonyPostfix, HarmonyPatch(typeof(Npc), "Die")]
    private static void DisableCorpseCollider(Npc __instance) {
        if (__instance.hitmeshCollider != null) {
            __instance.hitmeshCollider.enabled = false;
            Plugin.LoggingInfo("Disabled corpse hit collider so bullets pass through.", true);
        }
    }
}
