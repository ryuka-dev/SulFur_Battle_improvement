using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using static UrGUI.Utils.Logger;

namespace UrGUI.UWindow
{
    /// <summary>
    /// Manages all UWindows 
    /// </summary>
    internal static class UWindowManager
    {
        // ------ manager ------ 
        public static List<UWindow> Windows { get; private set; }

        public static GameObject BManagerG = null;
        public static UWindowManagerBehaviour BManager = null;

        public static bool IsDrawing { get; set; } = true;
        public static GUISkin DefaultSkin { get; internal set; } = null;

        // ------- dialog ------- 
        public static bool AllWindowsDisabled = false;
        public static bool AnyWindowDragging = false;
        public static System.Action ActiveOptionMenu = null;
        
        public static readonly Vector2 DynamicWindowsMargin = Vector2.one * 10f;
        private static Vector2 _dynamicWindowsNext = Vector2.one * 10;
        
        internal static void Register(UWindow win)
        {
            if (Windows == null) Windows = new List<UWindow>();
            
            if (BManager == null || BManagerG == null) InitializeManager();
            
            // generate new UID
            var guid = System.Guid.NewGuid().ToString();
            win.WinGuid = guid;
            Windows.Add(win);
        }

        internal static bool LoadGlobalSkin_internal(GUISkin skin)
        {
            if (skin == null) return false;
            foreach (var w in Windows)
            {
                w.LoadSkin(skin);
            }

            return true;
        }
        
        public static Vector2 GetDynamicWindowPos(float currentWidth)
        {
            // returns current and adds margin for the next one
            // TODO: dynamic Y (no idea how)
            var current = _dynamicWindowsNext;
            _dynamicWindowsNext.x += currentWidth + DynamicWindowsMargin.x;
            
            return current;
        }
        
        internal static void BringUWindowToFront(UWindow win)
        {
            // "front" means last to render
            Windows.Remove(win);
            Windows.Add(win);
        }
        
        private static int GetIndexByGuid(string guid)
        {
            return Windows.FindIndex((win) => win.WinGuid == guid);
        }
        
        private static UWindow GetUWinByGuid(string guid)
        {
            return Windows.FirstOrDefault(w => w.WinGuid == guid);
        }
        
        // ------- scale -------
        /// <summary>Asked for the GUI scale; null means 1. See <see cref="UWindow.SetGuiScaleProvider"/>.</summary>
        internal static System.Func<float> ScaleProvider = null;

        /// <summary>The scale every window is drawn at this frame.</summary>
        internal static float Scale { get; private set; } = 1f;

        /// <summary>Screen size in GUI units: what a window has to fit into at the current scale.</summary>
        internal static float ViewWidth => Screen.width / Scale;
        internal static float ViewHeight => Screen.height / Scale;

        private const float MinScale = 0.25f, MaxScale = 8f;

        // The left mouse button is down. Tracked from the raw events rather than read from
        // GUIUtility.hotControl or AnyWindowDragging, which a control released elsewhere, or a window
        // hidden mid-drag, can leave set. A release this OnGUI never sees (another IMGUI used it first)
        // only delays a scale change until the next click.
        private static bool _pointerDown;

        internal static void OnBehaviourGUI()
        {
            if (!IsDrawing) return;

            // Only the left button drags controls and windows; another button's release must not end it.
            var rawType = Event.current.rawType;
            if (Event.current.button == 0)
            {
                if (rawType == EventType.MouseDown) _pointerDown = true;
                else if (rawType == EventType.MouseUp) _pointerDown = false;
            }

            // Take a new scale only on the Layout event that opens each frame, so every event of one frame
            // uses the same one, and never while a mouse button is down: a slider that sets the scale
            // would otherwise move under the cursor as soon as it is dragged.
            if (Event.current.type == EventType.Layout && !_pointerDown)
                Scale = ReadScale();

            // Mouse positions are mapped through GUI.matrix by Unity itself, so every hit test below works
            // in GUI units. Unity resets the matrix before each OnGUI call; it is put back anyway so
            // nothing drawn after the windows in this call inherits it.
            var previousMatrix = GUI.matrix;
            if (Scale != 1f) GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1f));
            try
            {
                foreach (var w in Windows.ToList())
                {
                    // draw window
                    w.Draw();
                }

                // draw currently active/opened dialog window
                if (ActiveOptionMenu != null)
                    ActiveOptionMenu();
            }
            finally
            {
                GUI.matrix = previousMatrix;
            }
        }

        private static float ReadScale()
        {
            float value;
            try
            {
                value = ScaleProvider?.Invoke() ?? 1f;
            }
            catch (System.Exception ex)
            {
                err($"GUI scale provider failed, drawing at scale 1: {ex.Message}");
                ScaleProvider = null;
                return 1f;
            }

            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f) return 1f;
            value = Mathf.Clamp(value, MinScale, MaxScale);

            // Not larger than lets the biggest window fit on the screen: windows taller than the screen
            // are pinned to its top, and whatever hangs below - possibly the very control that sets
            // the scale - could not be reached any more. A scale asked below 1 is left alone, and
            // fitting never takes the scale below 1, so a small screen draws as it always did.
            var fit = LargestFittingScale();
            if (value > 1f && value > fit) value = Mathf.Max(1f, fit);
            return value;
        }

        private static float LargestFittingScale()
        {
            float width = 0f, height = 0f;
            if (Windows != null)
            {
                foreach (var w in Windows)
                {
                    width = Mathf.Max(width, w.Width);
                    height = Mathf.Max(height, w.Height);
                }
            }

            var fitX = width > 0f ? Screen.width / width : float.MaxValue;
            var fitY = height > 0f ? Screen.height / height : float.MaxValue;
            return Mathf.Min(fitX, fitY);
        }

        private static void InitializeManager()
        {
            BManagerG = new GameObject("UrGUI Manager");
            BManager = BManagerG.AddComponent<UWindowManagerBehaviour>();

            UWindowManagerBehaviour.Instance.LoadDefaultSkin();
        }
    }
}