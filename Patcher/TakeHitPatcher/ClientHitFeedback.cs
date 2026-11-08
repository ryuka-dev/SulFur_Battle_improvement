using System;
using BattleImprove.Integration;
using BattleImprove.Utils;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.Stats;
using PerfectRandom.Sulfur.Core.Units;
using PerfectRandom.Sulfur.Core.Weapons;
using UnityEngine;

namespace BattleImprove.Patcher.TakeHitPatcher;

// Combat feedback for a co-op client, driven by what the host settled. The ReceiveDamage patches stand
// down for such a hit (AttackFeedbackPatch.IsReportedByHost), so this is the only feedback it gets.
internal static class ClientHitFeedback {
    internal static void Show(ClientHit hit) {
        if (hit == null) return;
        // A hit on a body that was already dead only gives feedback when the player asked for it.
        if (hit.TargetWasDead && !Config.EnableDeadUnitFeedback.Value) return;

        ShowDamage(hit);
        PlaySound(hit);

        if (Config.EnableXCrossHair.Value && PluginInstance<xCrossHair>.Instance != null) {
            PluginInstance<xCrossHair>.Instance.StartTrigger(hit.Killed ? "Kill" : "Hit");
        }
    }

    internal static void Show(ClientKill kill) {
        if (kill == null) return;

        var unit = kill.Target as Unit;
        if (unit != null) {
            if (AttackFeedbackPatch.KilledEnemies.Contains(unit)) return;
            AttackFeedbackPatch.KilledEnemies.Add(unit);
        }

        // The killing hit already played this when it resolved; an assist has no hit of its own to play it.
        if (!kill.KillingBlow && Config.EnableXCrossHair.Value && PluginInstance<xCrossHair>.Instance != null) {
            PluginInstance<xCrossHair>.Instance.StartTrigger("Kill");
        }

        if (!Config.EnableDamageMessage.Value) return;
        if (PluginInstance<MessageController>.Instance == null) return;
        // The banner names the enemy, and a body already removed has no name left to read.
        if (unit == null) return;

        var player = StaticInstance<GameManager>.Instance.PlayerUnit;
        if (player == null) return;

        var distance = Vector3.Distance(player.EyesPosition, unit.transform.position);
        KillMessagePatch.Announce(unit, kill.Weapon as Weapon, Convert.ToString(kill.Experience), kill.IsHeadshot, distance);
    }

    private static void ShowDamage(ClientHit hit) {
        if (!Config.EnableDamageMessage.Value) return;
        if (PluginInstance<MessageController>.Instance == null) return;

        // Health removed, as the local path reports it: damage past the enemy's last health is not counted.
        ShowNumber((DamageTypes)hit.DamageType, hit.Weapon as Weapon, hit.Melee, hit.HealthRemoved);

        if (hit.Secondary == null) return;
        foreach (var secondary in hit.Secondary) {
            ShowNumber((DamageTypes)secondary.DamageType, null, false, secondary.HealthRemoved);
        }
    }

    private static void ShowNumber(DamageTypes type, Weapon weapon, bool melee, float amount) {
        if (amount <= 0) return;
        PluginInstance<MessageController>.Instance.OnEnemyHit(DescribeDamage(type, weapon, melee), Convert.ToInt32(amount));
    }

    // Shared with DamageInfoPatch. It lives outside that class because the Harmony analyzer treats every
    // method of a patch class as a patch.
    internal static string DescribeDamage(DamageTypes damageType, Weapon weapon, bool melee) {
        var def = weapon != null ? weapon.weaponDefinition : null;
        if (melee && def != null) return def.LocalizedDisplayName;

        var type = damageType.GetAsset() != null ? damageType.GetAsset().shortLabel : damageType.ToString();
        if (def == null) return type;

        var caliber = def.caliber.GetAsset();
        return type + " " + (caliber != null ? caliber.label : def.caliber.ToString()) + " " + def.projectileType;
    }

    private static void PlaySound(ClientHit hit) {
        if (!Config.EnableSoundFeedback.Value) return;
        if (PluginInstance<HitSoundEffect>.Instance == null) return;
        // The killing blow stays silent, as on the local path; the kill banner has its own sound.
        if (hit.Killed) return;

        var player = StaticInstance<GameManager>.Instance.PlayerUnit;
        if (player == null) return;

        var unit = hit.Target as Unit;
        if (unit != null && !unit.IsHostileTo(player)) return;

        Vector3 collisionPoint;
        if (hit.HitPosition.HasValue) collisionPoint = hit.HitPosition.Value;
        else if (unit != null) collisionPoint = unit.transform.position;
        else return;

        var distance = Vector3.Distance(player.EyesPosition, unit != null ? unit.transform.position : collisionPoint);
        var part = (HitboxColliders.Parts)hit.HitPart;
        var isHeadshot = part is HitboxColliders.Parts.Head or HitboxColliders.Parts.Eye;
        var isStrongHit = hit.IsCritical || (DamageTypes)hit.DamageType == DamageTypes.Critical || isHeadshot;

        SoundPatch.Play(player, collisionPoint, distance, isStrongHit);
    }
}
