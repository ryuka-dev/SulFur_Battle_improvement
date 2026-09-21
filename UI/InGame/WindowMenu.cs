using System;
using BattleImprove.Components;
using BattleImprove.Utils;
using UnityEngine;
using UrGUI.UWindow;

namespace BattleImprove.UI.InGame;

public class WindowMenu : WindowBase {
    protected override void Init() {
        window = UWindow.Begin(i18n.GetText("Menu.title", "Battle Improvement"));
        StartPosition(100, 100);
        
        window.Button(i18n.GetText("Toggle", "Feature Toggles"), () => OpenSubMenu("Toggle"));
        window.Button(i18n.GetText("AttackFeedback", "Attack Feedback"), () => OpenSubMenu("AttackFeedback"));
        window.Button(i18n.GetText("DeadProtection", "Death Protection"), () => OpenSubMenu("DeadProtection"));
        window.Button(i18n.GetText("Settings", "Settings"), () => OpenSubMenu("Setting"));
        base.Init();
    }

    protected override void Close() {
        base.Close();
        this.controller.CloseSubWindow();
        this.controller.Pause(false);
    }

    private void OpenSubMenu(string name) {
        controller.OpenSubWindow(name);
    }
}