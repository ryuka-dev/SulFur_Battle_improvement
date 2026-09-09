using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.Units;
using Unity.Collections;

namespace BattleImprove.Transpiler;

// "Bullets pass through dead bodies."
//
// History: this was once a transpiler on Projectile.HandleHit / ProjectileSystem.ProcessSortedHits.
// Both are gone; projectile/target intersection now happens in ProcessProjectileHitsJob, a
// [BurstCompile] IJob that runs as AOT-compiled native code and cannot be patched.
//
// It was then reimplemented by disabling a corpse's hit collider in Npc.Die. That worked, but it
// also removed the corpse from every physics query, so shooting a corpse no longer produced a hit
// at all -- and organ farming is exactly that: Npc.ReceiveDamage spawns an organ pickup when a
// *dead* unit is hit by a *non-melee* player attack (1/12 per hitbox, guarded by
// Hitmesh.spawnedOrgans). Disabling the collider silently removed that whole loop.
//
// What actually retires a projectile is managed code, not the Burst job. In
// ProjectileSystem.FixedUpdate the hit is applied first (ProjectileUtilities.ProcessUnitHit ->
// Unit.ReceiveDamage, which is what drops the organ) and only afterwards:
//
//     if (trackingMode != Orbit && !BouncesLeft && !unit.IsPetrified && penetrationsLeft <= 0) {
//         projectileRay.position = npcHitData.position;      // rewind to the hit point
//         projectileRay.lastPosition = npcHitData.position;
//         _projectilesToStop.Add(in npcHitData.projIndex);   // and retire it
//     }
//
// Only the last line is skipped for a corpse. Skipping the whole test instead (by extending the
// !unit.IsPetrified exemption, which is how this was first written) leaves the projectile alive but
// still loses the shot: the job resolves at most four hits per projectile per frame and breaks out
// of that list after the first one it accepts (penetration being the only exemption). The corpse
// eats the frame's hit slot, and because the projectile keeps the position the job already
// integrated for this frame, anything standing right behind the corpse is stepped straight over --
// exactly the "corpses still block bullets" report from log round Misc/19.
//
// Keeping the rewind and dropping only the Add fixes that: the projectile resumes from the corpse's
// surface on the next frame, so the space behind the corpse is swept normally and whatever stands
// there is hit. The corpse costs one frame of travel and nothing else. It cannot re-register on the
// same corpse either -- ProjectileRay.hitInstIDs already records every collider a projectile has
// hit, and a repeat is rejected before it can consume another hit slot.
//
// If the IL pattern ever stops matching (a game update rewriting that test), we fall back to the
// old collider-disabling behaviour so the advertised feature degrades instead of disappearing.
public static class DeadBodyPassThrough {
    internal static bool ProjectileStopPatched;

    public static void Apply(Harmony harmony) {
        try {
            harmony.PatchAll(typeof(ProjectileStopTranspiler));
        } catch (Exception e) {
            ProjectileStopPatched = false;
            Plugin.Logger.LogWarning("Could not patch the projectile stop rule: " + e);
        }

        if (ProjectileStopPatched) return;

        Plugin.Logger.LogWarning(
            "Bullets-through-corpses falls back to disabling corpse hit colliders; corpses cannot be shot for organs.");
        harmony.PatchAll(typeof(CorpseColliderFallback));
    }
}

[HarmonyPatch(typeof(ProjectileSystem), "FixedUpdate")]
internal static class ProjectileStopTranspiler {
    // The stop rule and the Add that retires the projectile sit 18 instructions apart; the tree and
    // unhandled-hit loops that follow use the same list and must not be picked up instead.
    private const int MaxInstructionsToAdd = 40;

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
        var codes = new List<CodeInstruction>(instructions);

        var stopRule = FindStopRule(codes);
        var loadUnit = stopRule > 0 && IsLoadLocal(codes[stopRule - 1]) ? codes[stopRule - 1] : null;
        var stopCall = loadUnit != null ? FindStopListAdd(codes, stopRule) : -1;

        if (stopCall > 0) {
            // The unit is already in a local for the IsPetrified test just above, so push that same
            // local as the extra argument. Stack in: NativeList<ID>&, ID&, Unit -- exactly what the
            // replacement takes in place of NativeList<ID>.Add(in ID).
            codes.Insert(stopCall, new CodeInstruction(loadUnit.opcode, loadUnit.operand));
            codes[stopCall + 1].opcode = OpCodes.Call;
            codes[stopCall + 1].operand =
                AccessTools.Method(typeof(ProjectileStopTranspiler), nameof(StopUnlessCorpse));
            DeadBodyPassThrough.ProjectileStopPatched = true;
        } else {
            Plugin.Logger.LogWarning(
                "ProjectileSystem.FixedUpdate no longer matches the expected projectile stop pattern.");
        }

        return codes;
    }

    /// <summary>
    /// Finds the <c>unit.IsPetrified</c> test of the npc-hit stop rule. FixedUpdate reads that
    /// property twice: the other one fills HittableData.petrified for the job and is followed by a
    /// store, while this one is branched on directly.
    /// </summary>
    private static int FindStopRule(List<CodeInstruction> codes) {
        for (var i = 1; i < codes.Count - 1; i++) {
            if (codes[i].operand is not MethodInfo method) continue;
            if (method.DeclaringType != typeof(Unit) || method.Name != "get_IsPetrified") continue;
            if (codes[i + 1].opcode != OpCodes.Brtrue && codes[i + 1].opcode != OpCodes.Brtrue_S) continue;

            return i;
        }

        return -1;
    }

    /// <summary>
    /// Finds the <c>_projectilesToStop.Add(...)</c> that the stop rule guards, i.e. the first one
    /// after <paramref name="stopRule" />.
    /// </summary>
    private static int FindStopListAdd(List<CodeInstruction> codes, int stopRule) {
        var last = Math.Min(stopRule + MaxInstructionsToAdd, codes.Count - 1);

        for (var i = stopRule + 1; i <= last; i++) {
            if (codes[i].operand is not MethodInfo method || method.Name != "Add") continue;
            if (codes[i].opcode != OpCodes.Call && codes[i].opcode != OpCodes.Callvirt) continue;

            for (var j = Math.Max(i - 4, 0); j < i; j++) {
                if (codes[j].operand is FieldInfo field && field.Name == "_projectilesToStop") return i;
            }
        }

        return -1;
    }

    private static bool IsLoadLocal(CodeInstruction code) {
        return code.opcode == OpCodes.Ldloc || code.opcode == OpCodes.Ldloc_S || code.opcode == OpCodes.Ldloc_0 ||
               code.opcode == OpCodes.Ldloc_1 || code.opcode == OpCodes.Ldloc_2 || code.opcode == OpCodes.Ldloc_3;
    }

    /// <summary>
    /// Stands in for <c>_projectilesToStop.Add(in projectile)</c> in ProjectileSystem.FixedUpdate.
    /// A hit on a dead body is not queued for retirement, so the projectile carries on -- from the
    /// hit point, which the instructions just above have already rewound it to, so the space behind
    /// the corpse is swept on the next frame rather than skipped.
    /// </summary>
    private static void StopUnlessCorpse(ref NativeList<ProjectileSystem.ID> stopList,
        ref ProjectileSystem.ID projectile, Unit unit) {
        if (!unit.IsAlive) return;

        stopList.Add(in projectile);
    }
}

/// <summary>
/// Fallback used only when the <see cref="ProjectileStopTranspiler" /> pattern no longer matches:
/// disable the corpse's hit collider so projectile raycasts skip it entirely. Bullets pass through,
/// but the corpse can no longer be shot at all, so organs cannot be farmed from it.
/// </summary>
internal static class CorpseColliderFallback {
    [HarmonyWrapSafe]
    [HarmonyPostfix, HarmonyPatch(typeof(Npc), "Die")]
    private static void DisableCorpseCollider(Npc __instance) {
        if (__instance.hitmeshCollider == null) return;

        __instance.hitmeshCollider.enabled = false;
        Plugin.LoggingInfo("Disabled corpse hit collider so bullets pass through.", true);
    }
}
