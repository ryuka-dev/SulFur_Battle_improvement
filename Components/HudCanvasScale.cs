using BattleImprove.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace BattleImprove.Components;

/// <summary>
/// Keeps one of the mod's overlay canvases at <see cref="UiScale.Hud"/>, as far as its content still
/// fits on the screen.
///
/// The prefabs' scalers are "constant pixel size", so the factor is written into the scaler, which
/// applies it to its canvas on its own Update. The game's HUD factor has no change event - its own
/// scaler recomputes it every frame from the screen size - so it is compared here every frame too,
/// which costs a few float reads, and written only when it differs.
///
/// The kill messages hang below the screen centre at fixed offsets (the damage list starts about 200
/// units down and grows downward), and a canvas scale moves those offsets with everything else. A
/// factor the game's HUD slider and the player's multiplier allow can therefore push them off the
/// bottom of the screen, which the game's own edge-anchored HUD never has to worry about. So each
/// canvas measures how far its content reaches from the centre and never takes a factor that would
/// carry it past the screen edge. The crosshair sits at the centre and is not limited in practice.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasScaler))]
internal sealed class HudCanvasScale : MonoBehaviour {
    // Room below the measured content for one more damage row (25 units each) than the list's own
    // placeholder already holds.
    private const float GrowthReserve = 25f;
    // Share of the half screen the content may use, so it never touches the edge.
    private const float EdgeMargin = 0.95f;

    private CanvasScaler scaler;
    private float reachX, reachY;
    private bool capped;

    /// <summary>Put every canvas scaler under <paramref name="root"/> on the HUD scale.</summary>
    internal static void AttachTo(GameObject root) {
        if (root == null) return;
        foreach (var canvasScaler in root.GetComponentsInChildren<CanvasScaler>(true)) {
            if (canvasScaler.GetComponent<HudCanvasScale>() == null) {
                canvasScaler.gameObject.AddComponent<HudCanvasScale>();
            }
        }
    }

    private void Awake() {
        scaler = GetComponent<CanvasScaler>();
        // The factor below is a pixel factor; it only means that in this mode.
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        MeasureReach();
        Apply();
    }

    private void LateUpdate() {
        Apply();
    }

    private void Apply() {
        // A minimised window can report no size; keep the last factor until it has one again.
        if (Screen.width <= 0 || Screen.height <= 0) return;

        var factor = UiScale.Hud;
        var fit = LargestFittingFactor();
        // Never below the size the resolution alone gives, which the layout was made for.
        var limit = Mathf.Max(fit, Screen.height / UiScale.ReferenceHeight);
        var nowCapped = factor > limit;
        if (nowCapped) factor = limit;

        if (nowCapped != capped) {
            capped = nowCapped;
            if (capped) {
                Plugin.LoggingInfo($"HUD '{name}': size limited to scale {limit:0.###} so it stays on screen " +
                                   $"(asked {UiScale.Hud:0.###}, content reaches {reachX:0}x{reachY:0} units from the centre).");
            }
        }

        if (!Mathf.Approximately(scaler.scaleFactor, factor)) {
            scaler.scaleFactor = factor;
        }
    }

    private float LargestFittingFactor() {
        var fitX = reachX > 0f ? Screen.width * 0.5f * EdgeMargin / reachX : float.MaxValue;
        var fitY = reachY > 0f ? Screen.height * 0.5f * EdgeMargin / reachY : float.MaxValue;
        return Mathf.Min(fitX, fitY);
    }

    /// <summary>
    /// How far the content reaches from the canvas centre, in canvas units, from the prefab's own
    /// layout. Each rect is carried into canvas space through its own and its parents' local
    /// transforms only, never through the canvas transform: a freshly instantiated root canvas can
    /// still have the scale 0 it is serialized with. Inactive children count too: the banner and
    /// damage rows are hidden until something is shown.
    /// </summary>
    private void MeasureReach() {
        var canvasTransform = transform;
        reachX = 0f;
        reachY = 0f;
        foreach (var rect in GetComponentsInChildren<RectTransform>(true)) {
            if (rect.transform == canvasTransform) continue;

            var toCanvas = Matrix4x4.identity;
            for (var t = rect.transform; t != null && t != canvasTransform; t = t.parent) {
                toCanvas = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale) * toCanvas;
            }

            var r = rect.rect;
            Include(toCanvas.MultiplyPoint3x4(new Vector3(r.xMin, r.yMin, 0f)));
            Include(toCanvas.MultiplyPoint3x4(new Vector3(r.xMin, r.yMax, 0f)));
            Include(toCanvas.MultiplyPoint3x4(new Vector3(r.xMax, r.yMin, 0f)));
            Include(toCanvas.MultiplyPoint3x4(new Vector3(r.xMax, r.yMax, 0f)));
        }

        // Anything that hangs well below the centre is a growing message list.
        if (reachY > 100f) reachY += GrowthReserve;
    }

    private void Include(Vector3 point) {
        if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsInfinity(point.x) || float.IsInfinity(point.y)) return;
        reachX = Mathf.Max(reachX, Mathf.Abs(point.x));
        reachY = Mathf.Max(reachY, Mathf.Abs(point.y));
    }
}
