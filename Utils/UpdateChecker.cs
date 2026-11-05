using System;
using System.Collections;
using UnityEngine.Networking;

namespace BattleImprove.Utils;

public class UpdateChecker {
    private const string UPDATE_URL = "https://api.github.com/repos/ryuka-dev/SulFur_Battle_improvement/releases/latest";
    private const int TIMEOUT_SECONDS = 5;

    /// <summary>
    /// Runs as a coroutine: this starts while the game is loading a save, and a synchronous request
    /// would freeze the game for as long as GitHub takes to answer or to time out.
    /// </summary>
    public static IEnumerator CheckForUpdate(Action onNewVersion) {
        using var request = UnityWebRequest.Get(UPDATE_URL);
        request.timeout = TIMEOUT_SECONDS;
        request.SetRequestHeader("User-Agent", "UnityModUpdater");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) {
            Plugin.Logger.LogWarning($"Failed to check for update: {request.error}");
            yield break;
        }

        try {
            var latestVersion = ExtractVersionFromJson(request.downloadHandler.text);

            if (CheckUpdate(MyPluginInfo.PLUGIN_VERSION, latestVersion)) {
                Plugin.Logger.LogInfo($"New version available: {latestVersion}");
                onNewVersion();
            } else {
                Plugin.Logger.LogInfo("No new version available.");
            }
        } catch (Exception e) {
            Plugin.Logger.LogWarning($"Failed to read the update information: {e.Message}");
        }
    }

    private static bool CheckUpdate(string current, string latest)
    {
        Version vCurrent = new Version(current);
        Version vLatest = new Version(latest);
        return vLatest > vCurrent;
    }

    private static string ExtractVersionFromJson(string json)
    {
        int start = json.IndexOf("\"tag_name\":\"") + 12;
        int end = json.IndexOf("\",", start);
        return json.Substring(start, end - start).Replace("v", "");
    }
}
