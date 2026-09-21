using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using PerfectRandom.Sulfur.Core;
using UnityEngine;
using I2Loc = I2.Loc.LocalizationManager;

namespace BattleImprove.Utils;

/// <summary>
/// Text for the plugin's own menu.
///
/// The strings live in <c>lang/&lt;code&gt;.json</c> next to the plugin, in the layout SULFUR's
/// localization helper looks for, so the same files also localize the config page for players who
/// have an external config UI installed - without the plugin depending on one.
///
/// The language follows the game, not the operating system: <see cref="I2Loc.CurrentLanguageCode"/>
/// is the code the player picked in the options screen, and it is exactly the file name in
/// <c>lang/</c>. Every lookup falls back to the English text passed by the caller, so a missing or
/// unreadable file degrades to English rather than to raw keys.
/// </summary>
public class LocalizationManager {
    /// <summary>The languages the game itself ships, and therefore the file names in <c>lang/</c>.</summary>
    public static readonly string[] SupportedLanguageCodes = {
        "en", "sv", "fr", "it", "de", "es", "pt", "ru", "pl", "ja", "ko", "zh-CN", "tr", "ar"
    };

    private const string BaseLanguage = "en";

    private readonly string pluginDirectory;
    private Dictionary<string, string> strings = new();

    private AsyncAssetLoading subscribedTo;
    private float retryTimer;

    /// <summary>
    /// The game language the current strings were loaded for, or null while the game has not
    /// reported one yet. Also the "have we loaded at all" flag: see <see cref="Tick"/>.
    /// </summary>
    public string LanguageCode { get; private set; }

    /// <summary>
    /// Raised after the strings were replaced. Controls built by UrGUI capture their caption when
    /// they are created, so whoever owns them has to rebuild them.
    /// </summary>
    public event Action LanguageChanged;

    public LocalizationManager(string pluginDirectory) {
        this.pluginDirectory = pluginDirectory;
    }

    /// <param name="fallback">
    /// The English text, verbatim as it appears in <c>lang/en.json</c>. It is what the player sees
    /// if the files did not ship, so it must never be a key or a placeholder.
    /// </param>
    public string GetText(string key, string fallback) {
        return strings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : fallback;
    }

    /// <summary>
    /// Drives the two things that cannot be done once during initialization: localization loads
    /// asynchronously, so the language code is usually still empty when the plugin starts, and
    /// <c>onLanguageChange</c> only fires when the language actually changes - a player who launched
    /// in Japanese would never see it. So the first load is retried every second until the game
    /// answers, and then stops; the event takes over from there.
    /// </summary>
    public void Tick() {
        Subscribe();

        if (LanguageCode != null) return;

        retryTimer -= Time.unscaledDeltaTime;
        if (retryTimer > 0f) return;
        retryTimer = 1f;

        var code = GameLanguageCode();
        if (string.IsNullOrEmpty(code)) return;

        Load(code);
    }

    public void Dispose() {
        if (subscribedTo == null) return;
        subscribedTo.onLanguageChange -= OnGameLanguageChanged;
        subscribedTo = null;
    }

    private void Subscribe() {
        if (subscribedTo != null) return;

        var loader = StaticInstance<AsyncAssetLoading>.Instance;
        if (loader == null) return;

        loader.onLanguageChange += OnGameLanguageChanged;
        subscribedTo = loader;
    }

    private void OnGameLanguageChanged() {
        var code = GameLanguageCode();
        if (string.IsNullOrEmpty(code) || code == LanguageCode) return;

        Load(code);
    }

    private static string GameLanguageCode() {
        try {
            return I2Loc.CurrentLanguageCode;
        } catch (Exception e) {
            Plugin.LoggingInfo("Could not read the game language: " + e.Message);
            return null;
        }
    }

    /// <summary>
    /// English first, then the requested language on top of it, so a term nobody translated yet
    /// shows the English text instead of an empty line.
    /// </summary>
    private void Load(string code) {
        var loaded = new Dictionary<string, string>();
        string directory = null;

        foreach (var candidate in FileCandidates(code)) {
            var read = ReadLanguage(candidate, out var readFrom);
            if (read == null) continue;

            foreach (var entry in read) loaded[entry.Key] = entry.Value;
            directory = readFrom;
        }

        // Only I2 knows whether the language it is currently showing reads right to left, and IMGUI
        // does no shaping of its own. Doing it here keeps it out of the per-frame draw path.
        if (loaded.Count > 0 && IsRightToLeft()) {
            foreach (var key in new List<string>(loaded.Keys)) {
                loaded[key] = I2Loc.FixRTL_IfNeeded(loaded[key]);
            }
        }

        strings = loaded;
        LanguageCode = code;

        // "Which language" and "did any text actually load" are different failures: every lookup
        // carries an English fallback, so a file that was never found looks exactly like a file that
        // was found and is English. Report both.
        var where = directory ?? "no readable lang directory";
        if (loaded.Count > 0) {
            Plugin.LoggingInfo($"Menu language: {code} - {loaded.Count} strings from {where}");
        } else {
            Plugin.Logger.LogWarning($"Menu language: {code} - 0 strings from {where}; falling back to English text.");
        }

        // Also raised for the very first load: the menu may already have been built in English while
        // the game was still starting up.
        LanguageChanged?.Invoke();
    }

    /// <summary>
    /// The file names to overlay, in order. A regional code the plugin has no file for (zh-TW, pt-BR)
    /// falls back to the base language part before English.
    /// </summary>
    private static IEnumerable<string> FileCandidates(string code) {
        yield return BaseLanguage;
        if (string.IsNullOrEmpty(code) || code == BaseLanguage) yield break;

        var separator = code.IndexOf('-');
        if (separator > 0) yield return code.Substring(0, separator);

        yield return code;
    }

    /// <summary>
    /// Mod managers do not agree on whether a package keeps its folders: some install the files as
    /// packaged, some flatten everything next to the DLL. The deciding condition is that entries
    /// were read, not that a directory exists - an empty lang folder must not swallow the others.
    /// </summary>
    private Dictionary<string, string> ReadLanguage(string code, out string directory) {
        directory = null;
        if (string.IsNullOrEmpty(pluginDirectory)) return null;

        var parent = Path.GetDirectoryName(pluginDirectory);
        var directories = new List<string> {
            Path.Combine(pluginDirectory, "lang"),
            pluginDirectory
        };
        if (!string.IsNullOrEmpty(parent)) directories.Add(Path.Combine(parent, "lang"));

        foreach (var candidate in directories) {
            var entries = ReadFile(Path.Combine(candidate, code + ".json"));
            if (entries == null || entries.Count == 0) continue;

            directory = candidate;
            return entries;
        }

        return null;
    }

    private static Dictionary<string, string> ReadFile(string path) {
        if (!File.Exists(path)) return null;

        try {
            using var stream = File.OpenRead(path);
            // Unity's JsonUtility cannot read this shape and returns an empty object without failing;
            // the data contract serializer is also what the game-side loader uses on these files.
            var serializer = new DataContractJsonSerializer(typeof(LangFile));
            var file = serializer.ReadObject(stream) as LangFile;
            if (file?.Entries == null) return null;

            var entries = new Dictionary<string, string>();
            foreach (var entry in file.Entries) {
                if (entry?.Key == null || entry.Value == null) continue;
                entries[entry.Key] = entry.Value;
            }

            return entries;
        } catch (Exception e) {
            Plugin.LoggingInfo($"Could not read localization file '{path}': {e.Message}");
            return null;
        }
    }

    private static bool IsRightToLeft() {
        try {
            return I2Loc.IsRight2Left;
        } catch (Exception) {
            return false;
        }
    }

    // Both DTOs are populated by the serializer, never by us.
#pragma warning disable CS0649
    [DataContract]
    private sealed class LangEntry {
        [DataMember(Name = "key")] public string Key;
        [DataMember(Name = "value")] public string Value;
    }

    [DataContract]
    private sealed class LangFile {
        [DataMember(Name = "entries")] public LangEntry[] Entries;
    }
#pragma warning restore CS0649
}
