using System;
using System.Collections.Generic;
using System.Reflection;
using BattleImprove.Utils;
using UnityEngine;
using UrGUI.UWindow;

namespace BattleImprove.UI.InGame;

public class WindowDeadProtection : WindowBase{
    PluginData.DeadProtection data;
    

    protected override void Init() {
        data = DataManager.DeadProtectionData;
        
        window = UWindow.Begin(i18n.GetText("DeadProtection", "Death Protection"));
        window.Width += 50;
        StartPosition(310, 100);
        window.Label(i18n.GetText("DeadProtection.durability", "Weapon Durability Loss (%)"));
        window.Slider("", OnDurabilityChange, data.weaponDurability, 0, 1, true);
        window.Label(i18n.GetText("DeadProtection.barrel", "Barrel Loss Probability (%)"));
        window.Slider("", OnBarrelChange, data.barrelChance, 0, 1, true);
        window.Label(i18n.GetText("DeadProtection.attachment", "Attachment Loss Probability (%)"));
        window.Slider("", OnAttachmentChange, data.attachmentChance, 0, 1, true);
        window.Label(i18n.GetText("DeadProtection.enchantment", "Enchantment Loss Probability (%)"));
        window.Slider("", OnEnchantmentChange, data.enchantmentChance, 0, 1, true);
        
        base.Init();
    }
    
    private void OnDurabilityChange(float value) {
        data.weaponDurability = MathF.Round(value, 2);
    }
    
    private void OnAttachmentChange(float value) {
        data.attachmentChance = MathF.Round(value, 2);
    }
    
    private void OnEnchantmentChange(float value) {
        data.enchantmentChance = MathF.Round(value, 2);
    }
    
    private void OnBarrelChange(float value) {
        data.barrelChance = MathF.Round(value, 2);
    }
}