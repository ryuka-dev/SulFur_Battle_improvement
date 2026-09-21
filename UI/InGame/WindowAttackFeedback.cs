using System;
using System.Collections.Generic;
using System.Reflection;
using BattleImprove.Utils;
using UnityEngine;
using UrGUI.UWindow;
using Random = UnityEngine.Random;

namespace BattleImprove.UI.InGame;

public class WindowAttackFeedback : WindowBase{
    PluginData.AttackFeedback data;
    private UWindowControls.WLabel label;
    
    private string resourcePack =>
        Plugin.IndicatorGameObject == null 
            ? i18n.GetText("AttackFeedback.resource.missed", "Missed") : i18n.GetText("AttackFeedback.resource.loaded", "Loaded");

    protected override void Init() {
        data = DataManager.AttackFeedbackData;
        
        window = UWindow.Begin(i18n.GetText("AttackFeedback", "Attack Feedback"));
        window.Width += 50;
        StartPosition(310, 100);

        label = window.Label(i18n.GetText("AttackFeedback.resource", "Resource Pack Detection") + ":" + resourcePack);
        window.Button(i18n.GetText("AttackFeedback.reload", "Reload"), (ReloadPrefab));
        window.Space();
        
        window.Label(i18n.GetText("AttackFeedback.indicator", "Hit Feedback: Sound Effect"));
        window.Slider(i18n.GetText("AttackFeedback.volume", "Volume"), (SetIndicatorVolume), data.indicatorVolume, 0, 1, true);
        window.Slider(i18n.GetText("AttackFeedback.distance", "Impact-Close"), (SetIndicatorDistance), data.indicatorDistance, 0, 1, true);
        window.Slider(i18n.GetText("AttackFeedback.distance.far", "Impact-Far"), (SetIndicatorDistanceFar), data.indicatorDistanceFar, 0, 1, true);
        window.Slider(i18n.GetText("AttackFeedback.distance.headshot", "Impact-Crit"), (SetIndicatorDistanceHeadshoot), data.indicatorDistanceHeadShoot, 0, 1, true);
        window.Space();

        window.Label(i18n.GetText("AttackFeedback.cross", "Hit Feedback: Crosshair"));
        window.ColorPicker(i18n.GetText("AttackFeedback.hitcolor", "Hit Crosshair Color"), (SetHitColor), data.hitColor);
        window.ColorPicker(i18n.GetText("AttackFeedback.killcolor", "Kill Crosshair Color"), (SetChangeKillColor), data.killColor);
        window.Space();
        
        window.Label(i18n.GetText("AttackFeedback.message", "Kill Message"));
        window.DropDown(i18n.GetText("AttackFeedback.style", "Style"), (SetKillMessageStyle), data.messageStyle, DataManager.KillMessageStyle);
        window.Slider(i18n.GetText("AttackFeedback.volume", "Volume"), (SetKillMessageVolume), data.messageVolume, 0, 1, true);
        window.Button(i18n.GetText("AttackFeedback.test", "Test"), (TestKillMessage));
        window.Space();
        
        base.Init();
    }
    
    private void ReloadPrefab() {
        if (Plugin.IndicatorGameObject == null) {
            PrefabManager.LoadAttackFeedbackPrefab();
        }
        
        var info = typeof(UWindowControls.WLabel).GetField("DisplayedString", BindingFlags.NonPublic | BindingFlags.Instance);
        if (info != null) {
            info.SetValue(label, i18n.GetText("AttackFeedback.resource", "Resource Pack Detection") + ":" + resourcePack);
        }
    }

    private void SetIndicatorVolume(float value) {
        data.indicatorVolume = MathF.Round(value, 2);
    }
    
    private void SetIndicatorDistance(float value) {
        data.indicatorDistance = MathF.Round(value, 2);
    }
    
    private void SetIndicatorDistanceFar(float value) {
        data.indicatorDistanceFar = MathF.Round(value, 2);
    }
    
    private void SetIndicatorDistanceHeadshoot(float value) {
        data.indicatorDistanceHeadShoot = MathF.Round(value, 2);
    }
    
    private void SetHitColor(Color color) {
        data.hitColor = color;
    }
    
    private void SetChangeKillColor(Color color) {
        data.killColor = color;
    }
    
    private void SetKillMessageStyle(int value) {
        data.messageStyle = value;
        PrefabManager.LoadKillMessageStyle(DataManager.KillMessageStyle[value]);
    }
    
    private void SetKillMessageVolume(float value) {
        data.messageVolume = MathF.Round(value, 2);
    }

    private void TestKillMessage() {
        PluginInstance<MessageController>.Instance.OnEnemyKill("Enemy Name#" + Random.Range(0, 10)
            , "Weapon Name#" + Random.Range(0, 10)
            , Random.Range(0, 10).ToString()
            , Random.Range(0, 10) < 5
            , Random.Range(0, 10) < 5);
        PluginInstance<MessageController>.Instance.OnEnemyHit("Bullet Damage Type#" + Random.Range(0, 10), Random.Range(0, 100));
    }
}