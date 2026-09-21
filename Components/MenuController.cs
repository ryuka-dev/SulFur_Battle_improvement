using System.Collections;
using System.Collections.Generic;
using BattleImprove.UI.InGame;
using BattleImprove.Utils;
using PerfectRandom.Sulfur.Core;
using UnityEngine;
using UrGUI.UWindow;

namespace BattleImprove.Components;

public class MenuController : PluginInstance<MenuController> {
    protected Dictionary<string, WindowBase> windos = new Dictionary<string, WindowBase>();
    protected WindowBase currentWindow;
    protected WindowBase menu;
    protected KeyCode menuKey {
        get {
            var data = DataManager.VersionData;
            return data.menuKey;
        }
    }

    private void Start() {
        var window = this.gameObject.AddComponent<WindowUpdateCheck>().SetController(this);
        InitWindow();

        // UrGUI controls keep the caption they were created with, so the windows are the thing that
        // has to be rebuilt when the player changes the game's language.
        if (Plugin.i18n != null) Plugin.i18n.LanguageChanged += OnLanguageChanged;
    }

    protected override void OnDestroy() {
        if (Plugin.i18n != null) Plugin.i18n.LanguageChanged -= OnLanguageChanged;
        base.OnDestroy();
    }

    private void OnLanguageChanged() {
        var wasOpen = menu != null && menu.window != null && menu.window.IsDrawing;
        RebuildWindows();
        // Reopen so the pause the old menu put the game into still has a menu to close it again.
        // Not this frame: a freshly added window only builds its UrGUI window in Start.
        if (wasOpen) StartCoroutine(ReopenMenu());
    }

    private IEnumerator ReopenMenu() {
        yield return null;
        ToggleMenu();
    }
    
    public void Update() {
        if(InputCompat.GetKeyDown(menuKey)) {
            ToggleMenu();
        }
    }
    
    public void ResetWindow() {
        ToggleMenu();
        RebuildWindows();
    }

    private void RebuildWindows() {
        // The replaced windows stay registered with UrGUI - it has no way to unregister one - so they
        // must be hidden before being dropped, or they would keep drawing with nothing to close them.
        CloseSubWindow();
        foreach (var window in windos) {
            window.Value.Destroy();
        }
        windos.Clear();
        currentWindow = null;
        InitWindow();
    }

    private void InitWindow() {
        menu = this.gameObject.AddComponent<WindowMenu>().SetController(this);
        windos.Add("Menu", menu);
        
        var toggle = this.gameObject.AddComponent<WindowToggle>().SetController(this);
        windos.Add("Toggle", toggle);
        
        var attackFeedback = this.gameObject.AddComponent<WindowAttackFeedback>().SetController(this);
        windos.Add("AttackFeedback", attackFeedback);
        
        var deadProtection = this.gameObject.AddComponent<WindowDeadProtection>().SetController(this);
        windos.Add("DeadProtection", deadProtection);
        
        var setting = this.gameObject.AddComponent<WindowSetting>().SetController(this);
        windos.Add("Setting", setting);
        
        var hotkey = this.gameObject.AddComponent<WindowHotkey>().SetController(this);
        windos.Add("Hotkey", hotkey);
    }

    public void ToggleMenu() {
        // A window that was added this frame has not built its UrGUI window yet.
        if (menu == null || menu.window == null) return;

        if (menu.window.ActiveSkin == null) {
            UWindow.RestoreGlobalDefaultSkin();
        }
        
        menu.Toggle();
        if (menu.window.IsDrawing) {
            if (currentWindow != null) {
                currentWindow.Toggle();
            }
            Pause(true);
        } else {
            CloseSubWindow();
            Pause(false);
        }
        this.SaveData();
    }
    
    public void SaveData() {
        DataManager.SaveAllData();
    }

    public void Pause(bool state) {
        var manager = StaticInstance<GameManager>.Instance;
        if (manager == null) return;
        manager.ModifyCursorState(LockStatePadlock.Paused, state);
        manager.ModifyControllerLock(LockStatePadlock.Paused, state);
    }
    
    public void OpenSubWindow(string name, bool closeCurrent = true) {
        if (currentWindow != null 
            && currentWindow != windos[name] 
            && currentWindow.window.IsDrawing 
            && closeCurrent) {
            currentWindow.Toggle();
        }

        if (closeCurrent) {
            currentWindow = windos[name];
            currentWindow.Toggle();
        } else {
            windos[name].Toggle();  
        }
    }
    
    public void CloseSubWindow() {
        foreach (var windowBase in windos) {
            if (windowBase.Value.window == null) continue;
            windowBase.Value.window.IsDrawing = false;
        }
    }
}