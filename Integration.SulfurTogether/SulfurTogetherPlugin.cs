using System;
using System.Runtime.CompilerServices;
using BattleImprove.Integration;
using BepInEx;

namespace BattleImprove.SulfurTogether;

/// <summary>
/// Feeds SULFUR Together's hit results into BattleImprove. Both dependencies are hard and by GUID, which
/// couples no code: without SULFUR Together BepInEx skips this plugin before loading its assembly.
/// </summary>
/// <remarks>
/// The game calls Assembly.GetTypes() on every assembly while loading a level, and one type that cannot be
/// loaded hangs that load forever. Versions of either mod without the members used here must only leave
/// this plugin inactive, so no type in this assembly may have a field, base type or interface from
/// SULFUR Together or BattleImprove, and only method bodies reached from the guarded calls in Awake name
/// them. The handlers are instance methods because the compiler caches a static method group in a
/// generated field of the delegate's type.
/// </remarks>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(BattleImproveGuid)]
[BepInDependency(SulfurTogetherGuid)]
public sealed class SulfurTogetherPlugin : BaseUnityPlugin {
    private const string PluginGuid = "ryuka.sulfur.battle_improve_sulfur_together";
    private const string PluginName = "BattleImprove - SULFUR Together Adapter";
    private const string PluginVersion = "1.6.0";
    private const string BattleImproveGuid = "BattleImprove";
    private const string SulfurTogetherGuid = "com.ryuka.sulfur.together";
    private const int RequiredApiVersion = 1;

    private NetCombatRelay relay;
    private IDisposable registration;

    private void Awake() {
        try {
            var apiVersion = NetCombatRelay.ReadApiVersion();
            if (apiVersion < RequiredApiVersion) {
                LogUnavailable($"NetCombat API {apiVersion}, need {RequiredApiVersion}");
                return;
            }

            var candidate = new NetCombatRelay();
            registration = Register(candidate);
            if (registration == null) {
                Logger.LogWarning("Another co-op integration is already registered; this one stays inactive.");
                return;
            }

            candidate.Subscribe();
            relay = candidate;
            Logger.LogInfo($"Using SULFUR Together NetCombat API {apiVersion} for co-op combat feedback.");
        } catch (Exception e) when (e is TypeLoadException or MissingMemberException
                                        or TypeInitializationException or BadImageFormatException) {
            // A registration without events would silence the fallback BattleImprove keeps for this case.
            registration?.Dispose();
            registration = null;
            LogUnavailable($"{e.GetType().Name}: {e.Message}");
        }
    }

    private void OnDestroy() {
        if (registration == null) return;
        registration.Dispose();
        registration = null;
        relay?.Unsubscribe();
        relay = null;
    }

    // Not inlined so that a BattleImprove without this method fails here, inside Awake's try block, and
    // not while Awake itself is being compiled.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IDisposable Register(NetCombatRelay candidate) {
        return CoopCombat.Register(new Func<bool>(candidate.IsResolvingRemoteHit));
    }

    private void LogUnavailable(string reason) {
        Logger.LogInfo($"Co-op hit results are unavailable ({reason}); a client's combat feedback stays estimated.");
    }
}
