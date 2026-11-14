using PerfectRandom.Sulfur.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleImprove.Utils;

/// <summary>
/// How big the mod's own interface is drawn. The mod's HUD prefabs and its menu were laid out for a
/// 1080-pixel-tall screen at a fixed pixel size, which made them half as big on a 2160p screen.
/// </summary>
internal static class UiScale {
    /// <summary>The screen height everything here was laid out for, and the game's own HUD reference height.</summary>
    internal const float ReferenceHeight = 1080f;

    private static CanvasScaler gameHudScaler;
    private static Canvas gameHudCanvas;

    /// <summary>
    /// Scale factor for the mod's HUD canvases (hit marker, kill messages, damage numbers).
    ///
    /// The game sizes its HUD with a CanvasScaler set to scale with the screen height (1080 reference)
    /// and changes that reference height from its "HUD size" accessibility slider
    /// (<c>CanvasManager.SetCanvasScale</c>). The factor that ends up on the game's HUD canvas therefore
    /// already includes the resolution, the game's aspect-ratio rule and the slider, so the mod's HUD
    /// takes that factor rather than recomputing any of it. Without a game HUD (main menu) it falls
    /// back to the resolution alone, which is what the game shows with the slider at its PC default.
    /// </summary>
    internal static float Hud => Sanitize(GameHudScaleFactor() * Config.HudScale.Value);

    /// <summary>
    /// Scale for the mod's F1 menu: grows with screens taller than 1080 pixels and never shrinks below
    /// the size the menu has always had, so a 1080p or smaller screen sees it exactly as before.
    /// </summary>
    internal static float Menu => Sanitize(Mathf.Max(1f, Screen.height / ReferenceHeight) * Config.MenuScale.Value);

    private static float GameHudScaleFactor() {
        var manager = StaticInstance<CanvasManager>.Instance;
        var scaler = manager != null ? manager.canvasScaler : null;
        if (scaler != null) {
            if (!ReferenceEquals(scaler, gameHudScaler)) {
                gameHudScaler = scaler;
                gameHudCanvas = scaler.GetComponent<Canvas>();
            }

            if (gameHudCanvas != null && gameHudCanvas.scaleFactor > 0f) return gameHudCanvas.scaleFactor;
        }

        return Screen.height / ReferenceHeight;
    }

    private static float Sanitize(float value) {
        return float.IsNaN(value) || float.IsInfinity(value) || value <= 0f ? 1f : value;
    }
}
