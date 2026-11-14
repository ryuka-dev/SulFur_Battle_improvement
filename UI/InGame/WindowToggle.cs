using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UrGUI.UWindow;

namespace BattleImprove.UI.InGame;

/// <summary>
/// In-game view over the plugin's own BepInEx config entries.
///
/// The entries themselves stay the single authority for every feature toggle: this window reads
/// their current value when it opens and writes back through <see cref="ConfigEntry{T}.Value"/>, so
/// a player's existing .cfg - including values set by hand, by a mod manager or by an external
/// config UI - is never replaced by a copy kept somewhere else. Nothing is written until a toggle
/// is actually clicked.
/// </summary>
public class WindowToggle : WindowBase {
    private readonly List<(ConfigEntry<bool> Entry, UWindowControls.WToggle Control)> bindings = new();
    private readonly List<(ConfigEntry<float> Entry, UWindowControls.WSlider Control)> sliders = new();

    protected override void Init() {
        window = UWindow.Begin(i18n.GetText("Toggle", "Feature Toggles"));
        // Wide enough for the longest caption in the languages that ship with the plugin.
        window.Width += 180;
        StartPosition(310, 100);

        window.Label(i18n.GetText("Toggle.feedback", "Combat Feedback"));
        Bind("Toggle.sound", "Hit Sound", Config.EnableSoundFeedback);
        Bind("Toggle.crosshair", "Hit Crosshair", Config.EnableXCrossHair);
        Bind("Toggle.message", "Damage & Kill Message", Config.EnableDamageMessage);
        Bind("Toggle.deadfeedback", "Feedback on Corpse Hits", Config.EnableDeadUnitFeedback);
        window.Space();

        window.Label(i18n.GetText("Toggle.qol", "Quality of Life"));
        Bind("Toggle.healthbar", "Enemy Health Bar", Config.EnableHealthBar);
        Bind("Toggle.expshare", "Experience Share", Config.EnableExpShare);
        Bind("Toggle.lootvfx", "Loot Drop VFX", Config.EnableLoopDropVFX);
        window.Space();

        // Both already follow the screen size (the HUD also the game's own HUD size setting); these
        // multiply on top. The menu one is taken when the slider is let go, not while it is dragged.
        window.Label(i18n.GetText("Toggle.size", "Size"));
        BindScale("Toggle.hudscale", "Combat HUD", Config.HudScale, Config.HudScaleMax);
        BindScale("Toggle.menuscale", "This Menu", Config.MenuScale, Config.MenuScaleMax);
        window.Space();

        // Whether these are patched at all is decided once, during Awake: two of them rewrite IL, and
        // the third owns state that must not appear or vanish in the middle of a run.
        window.Label(i18n.GetText("Toggle.restart", "* Applied on the next launch"));
        Bind("Toggle.deadcollision", "Bullets Pass Through Corpses", Config.EnableDeadUnitCollision, true);
        Bind("Toggle.scroll", "Reverse Mouse Scroll", Config.ReverseMouseScroll, true);
        Bind("DeadProtection", "Death Protection", Config.EnableDeadProtection, true);

        base.Init();
    }

    /// <summary>
    /// Re-read the entries every time the window is opened: the config file is shared with whatever
    /// else the player uses to edit it, and this window must show what is actually in effect.
    /// </summary>
    public override void Toggle() {
        if (!window.IsDrawing) {
            foreach (var (entry, control) in bindings) {
                control.Value = entry.Value;
            }
            foreach (var (entry, control) in sliders) {
                control.Value = entry.Value;
            }
        }

        base.Toggle();
    }

    private void Bind(string key, string english, ConfigEntry<bool> entry, bool needsRestart = false) {
        var text = i18n.GetText(key, english);
        var label = needsRestart ? text + " *" : text;
        // UrGUI's toggle never draws its own caption - it only paints the box, pushed to the right
        // edge of whatever rect it is given - so the caption is a label sharing the line with it.
        window.SameLine(3, 1);
        window.Label(label);
        var control = window.Toggle(string.Empty, value => Apply(entry, value), entry.Value);
        bindings.Add((entry, control));
    }

    private void BindScale(string key, string english, ConfigEntry<float> entry, float max) {
        var control = window.Slider(i18n.GetText(key, english), value => Apply(entry, value), entry.Value,
            Config.ScaleMin, max, true, "0.00");
        // Snapping keeps the number shown equal to the value stored, and limits a drag to one config
        // write per step instead of one per frame.
        control.Step = Config.ScaleStep;
        sliders.Add((entry, control));
    }

    private static void Apply(ConfigEntry<float> entry, float value) {
        if (Mathf.Approximately(entry.Value, value)) return;
        entry.Value = value;
    }

    private static void Apply(ConfigEntry<bool> entry, bool value) {
        if (entry.Value == value) return;
        // BepInEx persists the change itself (SaveOnConfigSet). It rewrites the file from the bound
        // entries plus the ones it does not know about, so every other setting - including keys left
        // over from older versions - keeps the value the player had.
        entry.Value = value;
    }
}
