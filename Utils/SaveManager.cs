using System;

namespace BattleImprove.Utils;

public static class SaveManager {
    public const string SaveFileName = "BattleImproveSaveData";

    public static void LoadSaveFile() {
        // The legacy vanilla-save migration (very old versions stored plugin data inside the game's
        // Profile save via SulfurSave.Imp) was removed: 0.18.5 replaced that save API entirely, so the
        // old data is unreachable. The plugin has kept its own ES3 file (SaveFileName) for many releases.
        try {
            if (ES3.FileExists(SaveFileName)) {
                LoadAll();
            } else {
                Plugin.LoggingInfo("Save data not found, creating new one...");
                LoadDefaults();
                Plugin.LoggingInfo("Save data created!");
            }
        } catch (Exception e) {
            // Do NOT rethrow: the rest of plugin init (AttackFeedback prefab, menu) must still run,
            // and DataManager hands out usable defaults even if nothing could be read or rewritten.
            Plugin.LoggingInfo("Failed to load save data, creating new one... " + e);
            try {
                LoadDefaults();
            } catch (Exception inner) {
                Plugin.LoggingInfo("Could not write a fresh save file; running on defaults: " + inner.Message);
            }
        }
    }

    private static void LoadAll() {
        LoadVersionData();
        LoadAttackMessageData();
        LoadDeadProtectionData();
    }

    // ES3 only substitutes the supplied default when the key is *absent*. A key that was written as
    // null - older releases could persist not-yet-loaded data when the player died or exited - loads
    // back as null forever, which left the settings blocks null and broke the hit sound, the
    // crosshair and the kill message. Detect that and heal the file instead of carrying it forward.
    private static void LoadDeadProtectionData() {
        var data = ES3.Load("DeadProtection", SaveFileName, new PluginData.DeadProtection());
        var repaired = data == null;
        DataManager.DeadProtectionData = data;
        if (repaired) RepairKey("DeadProtection", () => SaveDeadProtectionData());
    }

    private static void LoadAttackMessageData() {
        var data = ES3.Load("AttackFeedback", SaveFileName, new PluginData.AttackFeedback());
        var repaired = data == null;
        DataManager.AttackFeedbackData = data;
        if (repaired) RepairKey("AttackFeedback", SaveAttackMessageData);
    }

    private static void LoadVersionData() {
        var data = ES3.Load("Version", SaveFileName, new PluginData.Version());
        var repaired = data == null;
        DataManager.VersionData = data;
        if (repaired) RepairKey("Version", SaveVersionData);
    }

    /// <summary>
    /// Overwrite a save entry that loaded as null with the defaults the DataManager just substituted.
    /// Failing to write is not fatal - the in-memory defaults are already usable this session.
    /// </summary>
    private static void RepairKey(string key, Action save) {
        Plugin.LoggingInfo($"Save entry '{key}' was empty; restoring defaults.");
        try {
            save();
        } catch (Exception e) {
            Plugin.LoggingInfo($"Could not repair save entry '{key}': {e.Message}");
        }
    }
    
    
    public static void SaveAll() {
        SaveVersionData();
        SaveAttackMessageData();
        SaveDeadProtectionData();
    }
    
    public static void SaveVersionData() {
        ES3.Save("Version", DataManager.VersionData, SaveManager.SaveFileName);
    }
    
    public static void SaveAttackMessageData() {
        ES3.Save("AttackFeedback", DataManager.AttackFeedbackData, SaveManager.SaveFileName);
    }
    
    public static void SaveDeadProtectionData(bool reset = false) {
        ES3.Save("DeadProtection", DataManager.DeadProtectionData, SaveManager.SaveFileName);
    }

    private static void LoadDefaults() {
        // Key must match LoadVersionData's "Version"; the old "BattleImprove" key was never read back.
        ES3.Save("Version", new PluginData.Version(), SaveFileName);
        ES3.Save("AttackFeedback", new PluginData.AttackFeedback(), SaveFileName);
        ES3.Save("DeadProtection", new PluginData.DeadProtection(), SaveFileName);
        LoadAll();
    }
}