using System.Collections.Generic;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.UI;

namespace BattleImprove.Utils;

[HarmonyPatch]
public static class DataManager {
    private static PluginData.Version _versionData;
    private static PluginData.AttackFeedback _attackFeedbackData;
    private static PluginData.DeadProtection _deadProtectionData;

    // These blocks are the single authority for the plugin's own settings, and every consumer
    // (UI windows, hit sound, crosshair, kill message, dead protection) dereferences them directly.
    // A save file can legitimately hand back null - an ES3 key that was written as null stays null on
    // load, the supplied default is only used when the key is absent - so coerce here instead of
    // spreading null checks over every call site.
    public static PluginData.Version VersionData {
        get => _versionData ??= new PluginData.Version();
        set => _versionData = value ?? new PluginData.Version();
    }

    public static PluginData.AttackFeedback AttackFeedbackData {
        get => _attackFeedbackData ??= new PluginData.AttackFeedback();
        set => _attackFeedbackData = value ?? new PluginData.AttackFeedback();
    }

    public static PluginData.DeadProtection DeadProtectionData {
        get => _deadProtectionData ??= new PluginData.DeadProtection();
        set => _deadProtectionData = value ?? new PluginData.DeadProtection();
    }

    /// <summary>
    /// True once <see cref="SetUpData"/> has read the save file. The save hooks below are installed
    /// during Awake, long before that happens, and must not write the not-yet-loaded defaults over
    /// the player's stored settings.
    /// </summary>
    public static bool Initialized { get; private set; }

    public static Dictionary<int, string> KillMessageStyle = new() {
        {0, "Battlefield 1"},
        {1, "Battlefield 5"}
    };

    public static void SetUpData() {
        SaveManager.LoadSaveFile();
        Initialized = true;
    }

    public static void SaveAllData() {
        SaveVersionData();
        SaveAttackMessageData();
        SaveDeadProtectionData();
    }

    public static void SaveVersionData(bool reset = false) {
        if (!Initialized) return;
        SaveManager.SaveVersionData();
    }

    public static void SaveAttackMessageData(bool reset = false) {
        if (!Initialized) return;
        SaveManager.SaveAttackMessageData();
    }

    public static void SaveDeadProtectionData(bool reset = false) {
        if (reset) DataManager.DeadProtectionData.opened = false;

        if (DataManager.DeadProtectionData.opened) {
            DataManager.DeadProtectionData.weapons.Clear();
        }
        if (!Initialized) return;
        SaveManager.SaveDeadProtectionData();
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PauseMenu), "ExitAction")]
    [HarmonyPatch(typeof(GameManager), "PlayerDied")]
    private static void ExitActionPostfix() {
        // Guarded by SaveAllData: dying or leaving to the menu before the save file has been read
        // must not persist placeholder data.
        SaveAllData();
    }
}