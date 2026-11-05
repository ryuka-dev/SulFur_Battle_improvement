using System;
using BattleImprove.Utils;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.Units;
using UnityEngine;

namespace BattleImprove.Patcher.TakeHitPatcher;

// Kills this machine does not resolve itself. In co-op a client's hit goes to the host instead of
// through ReceiveDamage, so the enemy on the client neither loses health nor dies from that hit, and
// KillMessagePatch never sees a kill. What reaches the client is the experience: the co-op mod hands
// it out through DistributeExperience, as the game does.
//
// The game calls DistributeExperience only from GiveExperience, and that run is already announced by
// KillMessagePatch, which knows the hit. A credit from anywhere else is a kill nothing has announced.
// Asking that instead of "are we a co-op client" keeps the co-op mod out of this assembly.
public class CreditedKillPatch : AttackFeedbackPatch {
    private static bool inGameCredit;

    [HarmonyPrefix, HarmonyPatch(typeof(Npc), "GiveExperience")]
    private static void EnterGameCredit() {
        inGameCredit = true;
    }

    [HarmonyFinalizer, HarmonyPatch(typeof(Npc), "GiveExperience")]
    private static void LeaveGameCredit() {
        inGameCredit = false;
    }

    [HarmonyWrapSafe]
    [HarmonyPostfix, HarmonyPatch(typeof(Npc), "DistributeExperience")]
    private static void AnnounceCreditedKill(Npc __instance, int totalExperience) {
        if (inGameCredit || totalExperience <= 0) return;
        if (KilledEnemies.Contains(__instance)) return;
        KilledEnemies.Add(__instance);

        Plugin.LoggingInfo("Credited kill: " + __instance.SourceName + " " + totalExperience);

        if (Config.EnableXCrossHair.Value && PluginInstance<xCrossHair>.Instance != null) {
            PluginInstance<xCrossHair>.Instance.StartTrigger("Kill");
        }

        if (!Config.EnableDamageMessage.Value) return;
        if (PluginInstance<MessageController>.Instance == null) return;

        LastHitWeapon.TryGetValue(__instance, out var weapon);
        var player = StaticInstance<GameManager>.Instance.PlayerUnit;
        var distance = Vector3.Distance(player.EyesPosition, __instance.transform.position);

        KillMessagePatch.Announce(__instance, weapon, Convert.ToString(totalExperience), false, distance);
    }
}
