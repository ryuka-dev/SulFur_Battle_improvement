using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BattleImprove.Utils;

public static class PrefabManager {
    internal static AssetBundle AssetBundle;
    public static Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
    public static bool IsLoaded;

    public static IEnumerator LoadAssetBundle() {
        IsLoaded = false;
        Plugin.LoggingInfo("Loading Asset Bundle...", true);
        // Load asset bundle

#if DEBUG
        var sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        AssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "battle_improve"));
#else
        AssetBundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("BattleImprove.Assets.battle_improve"));
#endif
        if (AssetBundle == null) {
            // The bundle is built for a specific Unity version; a game engine upgrade makes it
            // unloadable (LoadFromStream returns null). Feature code must degrade gracefully.
            Plugin.LoggingInfo("Failed to load custom assets. VFX/hitmarker/kill-message/hit-sound " +
                               "visuals are disabled until the asset bundle is rebuilt for the game's Unity version.");
        } else {
            var request = AssetBundle.LoadAssetAsync<GameObject>("AttackFeedback");
            yield return request;
            Prefabs.Add("AttackFeedback", request.asset as GameObject);

            foreach (var style in DataManager.KillMessageStyle.Values) {
                request = AssetBundle.LoadAssetAsync<GameObject>(style);
                yield return request;
                Prefabs.Add(style, request.asset as GameObject);
            }

            for (var i = 1; i <= 5; i++) {
                request = AssetBundle.LoadAssetAsync<GameObject>("LoopDropTier" + i);
                yield return request;
                Prefabs.Add("LoopDropTier" + i, request.asset as GameObject);
            }
        }
        
        Plugin.LoggingInfo("Asset Bundle loaded!", true);
        IsLoaded = true;
    }
    
    public static GameObject LoadPrefab(string name, GameObject parent) {
        // The asset may be absent when the bundle failed to load (engine version mismatch) or when a
        // specific asset is missing. Return null and let callers skip the visual instead of throwing.
        if (!Prefabs.TryGetValue(name, out var prefab) || prefab == null) {
            Plugin.LoggingInfo($"Prefab '{name}' is unavailable; skipping (asset bundle not loaded?).");
            return null;
        }
        Plugin.LoggingInfo($"Loading {name} Prefab...", true);
        return Object.Instantiate(prefab, parent.transform, true);
    }

    internal static void LoadAttackFeedbackPrefab() {
        Plugin.IndicatorGameObject = LoadPrefab("AttackFeedback", Plugin.PluginGameObject);
        if (Plugin.IndicatorGameObject == null) {
            Plugin.LoggingInfo("AttackFeedback prefab unavailable; combat-feedback visuals disabled.");
            return;
        }
        TmpFontFixer.Apply(Plugin.IndicatorGameObject);
        LoadKillMessageStyle(DataManager.KillMessageStyle[DataManager.AttackFeedbackData.messageStyle]);
    }

    internal static void LoadKillMessageStyle(string style = "Battlefield 1") {
        if (Plugin.IndicatorGameObject == null) {
            return;
        }
        if (PluginInstance<MessageController>.Instance != null) {
            Object.Destroy(PluginInstance<MessageController>.Instance.gameObject);
        }
        var styleObject = LoadPrefab(style, Plugin.IndicatorGameObject);
        if (styleObject == null) {
            return;
        }
        TmpFontFixer.Apply(styleObject);
    }
}