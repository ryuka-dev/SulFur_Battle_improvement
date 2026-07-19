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
            Console.WriteLine(e);
            Plugin.LoggingInfo("Failed to load save data, creating new one...");
            // Do NOT rethrow: the rest of plugin init (AttackFeedback prefab, menu) must still run.
            LoadDefaults();
        }
    }

    private static void LoadAll() {
        LoadVersionData();
        LoadAttackMessageData();
        LoadDeadProtectionData();
    }

    private static void LoadDeadProtectionData() {
        var data = ES3.Load("DeadProtection", SaveFileName, new PluginData.DeadProtection());
        DataManager.DeadProtectionData = data;
    }

    private static void LoadAttackMessageData() {
        var data = ES3.Load("AttackFeedback", SaveFileName, new PluginData.AttackFeedback());
        DataManager.AttackFeedbackData = data;
    }

    private static void LoadVersionData() {
        var data = ES3.Load("Version", SaveFileName, new PluginData.Version());
        DataManager.VersionData = data;
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
        ES3.Save("BattleImprove", new PluginData.Version(), SaveFileName);
        ES3.Save("AttackFeedback", new PluginData.AttackFeedback(), SaveFileName);
        ES3.Save("DeadProtection", new PluginData.DeadProtection(), SaveFileName);
        LoadAll();
    }
}