using System;
using TMPro;
using UnityEngine;

namespace BattleImprove.Utils;

/// <summary>
/// Our kill-banner / damage-number UI ships in an AssetBundle whose font asset no longer survives the
/// game's current TextMeshPro version (Unity 6), so text either fails to render or — once we fell back to
/// the Latin-only default font — drops CJK characters (e.g. Chinese weapon names "80号台风").
///
/// Rather than guess, we simply reuse the exact font the game is already rendering its localized UI with:
/// grab it straight off a live game <see cref="TMP_Text"/> that is currently drawing CJK on screen, and
/// assign that font to our text objects. That guarantees identical glyph coverage (and a valid
/// material/atlas/shader) for whatever language the player has selected.
/// </summary>
public static class TmpFontFixer {
    private static TMP_FontAsset _gameFont;

    // Common characters across CJK locales; if a font can draw one of these it can render localized names.
    private static readonly char[] CjkProbe = { '号', '击', '杀', '测', '的' };

    /// <summary>True once we've locked onto a CJK-capable game font and no longer need to retry.</summary>
    public static bool Resolved { get; private set; }

    public static void Apply(GameObject root) {
        if (root == null) return;
        var font = ResolveGameFont();
        if (font == null) return;

        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true)) {
            try {
                if (tmp.font == font) continue;
                tmp.font = font;
                tmp.SetAllDirty();
            } catch (Exception e) {
                Plugin.LoggingInfo("TmpFontFixer: failed on a text element: " + e.Message);
            }
        }
    }

    private static TMP_FontAsset ResolveGameFont() {
        if (Resolved && _gameFont != null) return _gameFont;

        // 1) Best: the font a live game text is using right now to render CJK on screen.
        foreach (var t in UnityEngine.Object.FindObjectsOfType<TMP_Text>()) {
            var f = t != null ? t.font : null;
            if (IsUsable(f) && CoversCjk(f)) return Lock(f);
        }

        // 2) Any loaded CJK-capable font asset (in case the on-screen text isn't localized yet).
        foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>()) {
            if (IsUsable(f) && CoversCjk(f)) return Lock(f);
        }

        // 3) Provisional: keep at least Latin working until a CJK font shows up (stays unresolved so we retry).
        if (_gameFont == null) {
            foreach (var t in UnityEngine.Object.FindObjectsOfType<TMP_Text>()) {
                var f = t != null ? t.font : null;
                if (IsUsable(f)) { _gameFont = f; break; }
            }
            if (_gameFont == null) _gameFont = TMP_Settings.defaultFontAsset;
            Plugin.LoggingInfo("TmpFontFixer: provisional font '" + Name(_gameFont) + "' (CJK not found yet, will retry).", true);
        }
        return _gameFont;
    }

    private static TMP_FontAsset Lock(TMP_FontAsset f) {
        _gameFont = f;
        Resolved = true;
        Plugin.LoggingInfo("TmpFontFixer: locked game font '" + Name(f) + "'.", true);
        return f;
    }

    private static bool IsUsable(TMP_FontAsset f) => f != null && f.material != null && f.atlasTexture != null;

    private static bool CoversCjk(TMP_FontAsset f) {
        foreach (var c in CjkProbe) {
            if (f.HasCharacter(c)) return true;
        }
        return false;
    }

    private static string Name(TMP_FontAsset f) => f != null ? f.name : "<null>";
}
