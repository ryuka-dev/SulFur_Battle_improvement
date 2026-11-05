#region

using System;
using System.Collections;
using System.IO;
using BattleImprove.Components;
using BattleImprove.Components.QOL;
using BattleImprove.Patcher.QOL;
using BattleImprove.Patcher.TakeHitPatcher;
using BattleImprove.Transpiler;
using BattleImprove.Utils;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

namespace BattleImprove;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    internal new static ManualLogSource Logger;
    internal static Harmony Harmony;
    internal static LocalizationManager i18n;

    private static bool debugMode = false;
    
    internal static GameObject PluginGameObject;
    internal static GameObject IndicatorGameObject;

    public void Awake() {
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} {MyPluginInfo.PLUGIN_VERSION} is loading!");
        
#if DEBUG
        debugMode = true;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} debug mode is enable!");
#endif
        // Config
        BattleImprove.Config.InitConfig(Config);
        // Localization. The strings are read from lang/<code>.json next to this assembly, and the
        // language they are read for is the game's, which is not known yet this early - Update drives
        // the first load once the game reports one.
        i18n = new LocalizationManager(Path.GetDirectoryName(Info.Location));
        // Harmony patching
        Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        Patching();
        // AssetBundle
        StartCoroutine(PrefabManager.LoadAssetBundle());
        
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        StartCoroutine(InitPluginGameobject());
    }

    private IEnumerator InitPluginGameobject() {
        // 0.18.5: the save singleton (SulfurSave.Imp) was replaced by SulfurSaveState.active,
        // which becomes non-null once a save slot has been launched into a run.
        while (SulfurSaveState.active == null || !PrefabManager.IsLoaded) {
            LoggingInfo("Waiting for game to load...", true);
            yield return new WaitForSeconds(1f);
        }
        LoggingInfo("Starting plugin initialization...");
        
        LoggingInfo("Initializing plugin gameobject...");
        // Plugin GameObject
        var plugin = GameObject.Find("CmPlugin");
        if (plugin == null) {
            PluginGameObject = new GameObject("CmPlugin");
            Object.DontDestroyOnLoad(PluginGameObject);
        } else {
            PluginGameObject = plugin;
        }
        
        // load plugin save data
        LoggingInfo("Loading plugin data...");
        DataManager.SetUpData();
        // load indicator prefab
        
        LoggingInfo("Loading plugin prefab...");
        // An exception here used to abort the whole coroutine, silently taking the F1 menu and the
        // loot helper with it. The combat-feedback visuals are optional; the rest of the plugin is not.
        try {
            PrefabManager.LoadAttackFeedbackPrefab();
        } catch (Exception e) {
            LoggingInfo("Failed to load the combat-feedback prefab; its visuals are disabled: " + e);
        }

        // load other gameobject
        LoggingInfo("Loading other gameobject...");
        PluginGameObject.AddComponent<LootSpawnHelper>();
        var menu = new GameObject("Menu") {
            transform = {
                parent = PluginGameObject.transform
            }
        };
        menu.AddComponent<MenuController>();
    }

    private void Update() {
        i18n?.Tick();
    }

    private void OnDestroy() {
        i18n?.Dispose();
        Harmony.UnpatchSelf();
    }
    

    private void Patching() {
        LoggingInfo("Patching...", true);
        TryPatch(typeof(AttackFeedbackPatch));
        TryPatch(typeof(DataManager));
        
        // QOL
        // These patches are installed unconditionally and read their toggle at the point where they
        // act, so the in-game menu can switch the feature on and off without a restart. Only the
        // toggles that decide whether IL is rewritten (the transpilers) or that own persisted state
        // (dead protection) are still resolved once, here, and need a restart to change.
        TryPatch(typeof(ExpSharePatch));
        TryPatch(typeof(HealthBarPatch));
        TryPatch(typeof(LootDropPatch));
        if (BattleImprove.Config.EnableDeadUnitCollision.Value) DeadBodyPassThrough.Apply(Harmony);
        if (BattleImprove.Config.EnableDeadProtection.Value) TryPatch(typeof(DeadProtection));

        // Other
        if (BattleImprove.Config.ReverseMouseScroll.Value) TryPatch(typeof(MouseScrollTranspiler));
        
        // BF
        TryPatch(typeof(SoundPatch));
        TryPatch(typeof(DamageInfoPatch));
        TryPatch(typeof(KillMessagePatch));
        TryPatch(typeof(CrossHairPatch));

        LoggingInfo("Patching complete!");
    }

    // A game update that moves one patch target costs that one feature. Letting the exception out of
    // Awake would stop the bundle and the menu from loading as well.
    private static void TryPatch(Type patch) {
        try {
            Harmony.PatchAll(patch);
        } catch (Exception e) {
            Logger.LogError($"Could not apply {patch.Name}; that feature may not work: {e}");
        }
    }
    
    public static void LoggingInfo(string info, bool needDebug = false) {
        switch (needDebug) {
            case true when debugMode:
                Logger.LogInfo("Debug info: " + info);
                break;
            case false:
                Logger.LogInfo(info);
                break;
        }
    }
}