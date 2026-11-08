using System.Runtime.CompilerServices;
using BattleImprove.Integration;
using SULFURTogether.Api;

namespace BattleImprove.SulfurTogether;

// Translates SULFUR Together's events into BattleImprove's own types. A plain class on purpose: the
// handlers' parameters are SULFUR Together types, and a component's methods are scanned by the engine.
// The members called from the plugin are not inlined, so a missing API fails inside its guarded call.
internal sealed class NetCombatRelay {
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static int ReadApiVersion() {
        return NetCombat.ApiVersion;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal bool IsResolvingRemoteHit() {
        return NetCombat.IsResolvingRemoteHit;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Subscribe() {
        NetCombat.LocalHitResolved += OnHitResolved;
        NetCombat.LocalKillCredited += OnKillCredited;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Unsubscribe() {
        NetCombat.LocalHitResolved -= OnHitResolved;
        NetCombat.LocalKillCredited -= OnKillCredited;
    }

    private void OnHitResolved(LocalHitResolvedArgs e) {
        var targetWasDead = e.Outcome == HitOutcome.TargetDead;
        if (e.Outcome != HitOutcome.Applied && !targetWasDead) return;

        var secondary = new ClientHitSecondary[e.Secondary.Count];
        for (var i = 0; i < secondary.Length; i++) {
            secondary[i] = new ClientHitSecondary {
                HealthRemoved = e.Secondary[i].HealthRemoved,
                DamageType = e.Secondary[i].DamageType,
            };
        }

        CoopCombat.ReportClientHit(new ClientHit {
            Target = e.Target,
            TargetWasDead = targetWasDead,
            Weapon = e.Weapon,
            Melee = e.Melee,
            DamageType = e.DamageType,
            IsCritical = e.IsCritical,
            HitPart = e.HitPart,
            HitPosition = e.HitPosition,
            HealthRemoved = e.HealthRemoved,
            Killed = e.Killed,
            Secondary = secondary,
        });
    }

    private void OnKillCredited(LocalKillCreditedArgs e) {
        CoopCombat.ReportClientKill(new ClientKill {
            Target = e.Target,
            Weapon = e.Weapon,
            KillingBlow = e.KillingBlow,
            IsHeadshot = e.IsHeadshot,
            Experience = e.Experience,
        });
    }
}
