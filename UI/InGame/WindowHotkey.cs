using System;
using System.Collections;
using System.Reflection;
using BattleImprove.Utils;
using UnityEngine;
using UrGUI.UWindow;

namespace BattleImprove.UI.InGame;

public class WindowHotkey : WindowBase {
    UWindowControls.WLabel label;
    private KeyCode key;
    
    protected override void Init() {
        window = UWindow.Begin(i18n.GetText("Hotkey.title", "Press a key"));
        var data = DataManager.VersionData;
        label = window.Label(i18n.GetText("Hotkey.current", "Current Hotkey") + " " + data.menuKey.ToString());
        StartPosition(350, 350);
        
        base.Init();
    }

    private void Update() {
        if (window == null) return;
        if (!window.IsDrawing) {
            // Closed with the menu key rather than the Close button: the choice was not confirmed.
            if (key != KeyCode.None) {
                key = KeyCode.None;
                ShowHotkey(DataManager.VersionData.menuKey);
            }
            return;
        }

        if (!InputCompat.TryGetAnyKeyDown(out var pressed)) return;
        // Escape belongs to the game's pause menu.
        if (pressed == KeyCode.Escape) return;

        key = pressed;
        ShowHotkey(key);
    }

    private void ShowHotkey(KeyCode hotkey) {
        var info = typeof(UWindowControls.WLabel).GetField("DisplayedString", BindingFlags.NonPublic | BindingFlags.Instance);
        if (info != null) {
            info.SetValue(label, i18n.GetText("Hotkey.current", "Current Hotkey") + " " + hotkey.ToString());
        }
    }

    protected override void Close() {
        // Closing without pressing anything keeps the hotkey the player already has.
        if (key != KeyCode.None) DataManager.VersionData.menuKey = key;
        key = KeyCode.None;
        base.Close();
    }
}