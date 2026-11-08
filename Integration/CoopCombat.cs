using System;
using BattleImprove.Patcher.TakeHitPatcher;
using UnityEngine;

namespace BattleImprove.Integration;

/// <summary>Further damage the game dealt to the same target inside a client's hit.</summary>
public sealed class ClientHitSecondary {
    public float HealthRemoved;
    public int DamageType;
}

/// <summary>
/// A hit of the local co-op client that the host applied, or that found the target already dead on the
/// host. Target and Weapon are the game's local objects.
/// </summary>
public sealed class ClientHit {
    public object Target;
    public bool TargetWasDead;
    public object Weapon;
    public bool Melee;
    public int DamageType;
    public bool IsCritical;
    public int HitPart;
    public Vector3? HitPosition;
    public float HealthRemoved;
    public bool Killed;
    public ClientHitSecondary[] Secondary;
}

/// <summary>A kill the host credited to the local co-op client: its killing blow or an assist.</summary>
public sealed class ClientKill {
    public object Target;
    public object Weapon;
    public bool KillingBlow;
    public bool IsHeadshot;
    public int Experience;
}

/// <summary>
/// The only place a co-op integration meets this mod. An optional companion plugin registers here; this
/// assembly never refers to that plugin or to the co-op mod, so it loads and plays the same without them.
/// </summary>
public static class CoopCombat {
    private static Func<bool> isResolvingRemoteHit;

    /// <summary>
    /// Whether a client's hits are reported through this class. While false, a client's suppressed hit is
    /// estimated from the local damage call instead.
    /// </summary>
    internal static bool ClientHitsReported => isResolvingRemoteHit != null;

    internal static bool IsResolvingRemoteHit => isResolvingRemoteHit != null && isResolvingRemoteHit();

    /// <summary>
    /// Claims the single slot. The callback answers whether this machine, as the host, is settling another
    /// player's hit right now. Returns null when the slot is taken; dispose the result to release it.
    /// </summary>
    public static IDisposable Register(Func<bool> resolvingRemoteHit) {
        if (resolvingRemoteHit == null || isResolvingRemoteHit != null) return null;
        isResolvingRemoteHit = resolvingRemoteHit;
        return new Registration(resolvingRemoteHit);
    }

    public static void ReportClientHit(ClientHit hit) {
        ClientHitFeedback.Show(hit);
    }

    public static void ReportClientKill(ClientKill kill) {
        ClientHitFeedback.Show(kill);
    }

    private sealed class Registration : IDisposable {
        private readonly Func<bool> owner;

        internal Registration(Func<bool> owner) {
            this.owner = owner;
        }

        public void Dispose() {
            if (isResolvingRemoteHit == owner) isResolvingRemoteHit = null;
        }
    }
}
