using System.Collections;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.DevTools;
using PerfectRandom.Sulfur.Core.Units;
using UnityEngine;

namespace BattleImprove.Components.QOL;

public class HealthBar : MonoBehaviour {
    private Camera camera;
    private bool isInitialized;
    private Npc npc;
    private float timer;

    private void Start() {
        npc = gameObject.GetComponent<Npc>();
        if (npc.IsProtectedNpc) {
            Destroy(this);
            return;
        }

        isInitialized = false;
        StartCoroutine(Initialize());
    }

    private void Update() {
        if (!isInitialized) return;

        // The developer tools own the debug frame whenever they are showing it; stay out of their way.
        var devTools = StaticInstance<DevToolsManager>.Instance;
        if (devTools == null || devTools.shouldShow) return;

        // The health bar is switchable from the in-game menu, so the toggle is read here rather than
        // once at startup. Switched off, an already created frame is only hidden: the game owns it,
        // and re-enabling has to bring it back without reloading the level.
        if (!Config.EnableHealthBar.Value) {
            if (npc.debugFrame != null && npc.debugFrame.gameObject.activeSelf) {
                npc.debugFrame.gameObject.SetActive(false);
            }
            return;
        }

        if (!npc.IsAlive) return;

        // Created on first showing rather than during Initialize, so units spawned while the feature
        // is off do not get a frame they would never display.
        if (npc.debugFrame == null) devTools.AddDebugFrameToUnit(npc);
        // AddDebugFrameToUnit declines some units (breakables); there is nothing to show for those.
        if (npc.debugFrame == null) return;

        var position = npc.EyesPosition;
        timer += Time.deltaTime;

        npc.debugFrame.gameObject.SetActive(CheckVisible(position));
    }

    private IEnumerator Initialize() {
        yield return new WaitForSeconds(3);
        camera = StaticInstance<GameManager>.Instance.currentCamera;
        isInitialized = true;
    }

    private bool CheckVisible(Vector3 position) {
        try {
            var screenPoint = camera.WorldToViewportPoint(position);
            return screenPoint is {z: > 0, x: > 0 and < 1, y: > 0 and < 1} &&
                   Vector3.Distance(position, camera.transform.position) < 15;
        }
        catch {
            Plugin.Logger.LogInfo("Player camera lost, trying to find it again");
            camera = StaticInstance<GameManager>.Instance.currentCamera;
            StaticInstance<DevToolsManager>.Instance.AddDebugFrameToUnit(npc);
        }

        return false;
    }

    public bool UpdateValue() {
        if (!(timer > 0.25f)) return false;
        timer = 0;
        return true;
    }
}