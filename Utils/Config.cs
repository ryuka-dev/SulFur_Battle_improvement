using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace BattleImprove;

public class Config {
    internal static ConfigEntry<bool> EnableHealthBar;
    internal static ConfigEntry<bool> EnableExpShare;
    internal static ConfigEntry<bool> EnableSoundFeedback;
    internal static ConfigEntry<bool> EnableDeadUnitCollision;
    internal static ConfigEntry<bool> EnableXCrossHair;
    internal static ConfigEntry<bool> EnableDamageMessage;
    internal static ConfigEntry<bool> EnableLoopDropVFX;
    internal static ConfigEntry<bool> EnableDeadProtection;
    internal static ConfigEntry<bool> EnableDeadUnitFeedback;
    
    internal static ConfigEntry<bool> ReverseMouseScroll;
    
    
    internal static ConfigEntry<float> Proportion;

    internal static ConfigEntry<float> HudScale;
    internal static ConfigEntry<float> MenuScale;

    // Both sizes are multipliers on an automatic base (see UiScale), so 1 is right for most players.
    // The menu's top is lower because its tallest window (Feature Toggles, 683 units) stops fitting the
    // screen a little above 1.55 on any screen 1080 or more pixels tall; UrGUI holds it there anyway,
    // and a slider past that point would do nothing.
    internal const float ScaleMin = 0.5f, HudScaleMax = 2f, MenuScaleMax = 1.5f, ScaleStep = 0.05f;

    public static void InitConfig(ConfigFile cfg) {
        Plugin.LoggingInfo("Loading Config...");
        ToggleConfigInit(cfg);
        ExpShareConfigInit(cfg);
        DisplayConfigInit(cfg);
        OtherConfigInit(cfg);
        Plugin.LoggingInfo("Config Loaded.");
    }

    private static void ToggleConfigInit(ConfigFile cfg) {
        EnableExpShare = cfg.Bind("Toggle/开关", "EnableExpShare", true, "Enable experience share/是否开启经验共享");
        EnableHealthBar = cfg.Bind("Toggle/开关", "EnableHealthBar", true, "Enable health bar/是否开启血条");
        EnableXCrossHair = cfg.Bind("Toggle/开关", "EnableHitFeedback", true, "Enable xCrossHair feedback/是否开启击中准心反馈");
        EnableSoundFeedback = cfg.Bind("Toggle/开关", "EnableSoundFeedback", true, "Enable hit sound feedback on enemies/是否开启敌人受击声音反馈");
        EnableDamageMessage = cfg.Bind("Toggle/开关", "EnableDamageMessage", true, "Enable damage and kill message/是否开启伤害与击杀信息");
        EnableDeadUnitCollision = cfg.Bind("Toggle/开关", "EnableDeadUnitCollision", true, "Allowing bullets to pass through deadbody/是否使子弹能穿过尸体");
        // Shooting a corpse is how organs are farmed, so those hits stay silent unless asked for:
        // otherwise every corpse shot would fire the hit sound, the crosshair and a damage number.
        EnableDeadUnitFeedback = cfg.Bind("Toggle/开关", "EnableDeadUnitFeedback", false, "Show combat feedback when hitting an already dead body/是否在击中尸体时显示战斗反馈");
        EnableLoopDropVFX = cfg.Bind("Toggle/开关", "EnableLoopDropVFX", true, "Enable loot drop VFX/是否开启掉落特效");
        // Off by default since 0.18.5: the game now has a native insured-items system (church collection
        // returns items on death), so this feature overlaps and can double-return weapons. Opt-in only.
        EnableDeadProtection = cfg.Bind("Toggle/开关", "EnableDeadProtection", false, "Enable dead protection (overlaps with the game's native insured-items system since 0.18.5)/是否开启死亡保护（0.18.5 起与游戏原生保险箱重叠，默认关闭）");
    }

    private static void ExpShareConfigInit(ConfigFile cfg) {
        Proportion = cfg.Bind("ExpShare/经验共享", "Proportion", 0.5f, "The proportion of experience shared to second weapon/共享给第二把武器的经验比例");
    }
    
    private static void DisplayConfigInit(ConfigFile cfg) {
        HudScale = cfg.Bind("Display/显示", "HudScale", 1f, new ConfigDescription(
            "Size of the hit marker, kill messages and damage numbers. They already follow the screen resolution and the game's own HUD size setting; this multiplies on top of that. Kill messages are kept small enough to stay on screen/命中准心、击杀信息与伤害数字的大小。它们已随分辨率与游戏自带的 HUD 大小设置缩放，此项在其基础上再乘以该倍数。击杀信息不会放大到超出屏幕",
            new AcceptableValueRange<float>(ScaleMin, HudScaleMax)));
        MenuScale = cfg.Bind("Display/显示", "MenuScale", 1f, new ConfigDescription(
            "Size of this mod's F1 menu. It already grows with screens taller than 1080 pixels; this multiplies on top of that. The menu is kept small enough to fit the screen/本 mod 的 F1 菜单大小。屏幕高于 1080 像素时已自动放大，此项在其基础上再乘以该倍数。菜单不会放大到超出屏幕",
            new AcceptableValueRange<float>(ScaleMin, MenuScaleMax)));
    }

    private static void OtherConfigInit(ConfigFile cfg) {
        ReverseMouseScroll = cfg.Bind("Other/其他", "ReverseMouseScroll", true, "Reverse mouse scroll direction/反转鼠标滚轮方向");
    }
}