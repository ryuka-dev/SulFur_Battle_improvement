using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.Items;
using PerfectRandom.Sulfur.Core.Units;

namespace BattleImprove.Patcher.QOL;

public class ExpSharePatch {
    // DistributeExperience, not GiveExperience: the latter runs for every death and only hands out
    // XP when the player damaged the unit, so a kill by another unit would share XP the player never
    // earned.
    [HarmonyWrapSafe]
    [HarmonyPostfix, HarmonyPatch(typeof(Npc), "DistributeExperience")]
    private static void ShareExperience(int totalExperience) {
        if (!Config.EnableExpShare.Value) return;

        var lastUsedWeapon = StaticInstance<GameManager>.Instance.PlayerUnit.lastUsedWeapon;
        // The player has not fired or swung anything yet this life.
        if (lastUsedWeapon == null) return;

        var secondWeapon = lastUsedWeapon.inventorySlot == InventorySlot.Weapon0
            ? InventorySlot.Weapon1
            : InventorySlot.Weapon0;
        var equippedWeapon = StaticInstance<GameManager>.Instance.PlayerUnit.GetComponent<EquipmentManager>()
            .EquippedHoldables;

        float exp = totalExperience;
        var proportion = Config.Proportion.Value;

        if (!equippedWeapon.ContainsKey(secondWeapon)) return;

        // The game leaves max rank weapons out of the XP distribution; the share has to as well.
        if (equippedWeapon[secondWeapon].IsMaxRank) return;

        equippedWeapon[secondWeapon].AddExperience(exp * proportion);
    }
}