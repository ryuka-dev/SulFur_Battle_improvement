using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using PerfectRandom.Sulfur.Core.Input;

namespace BattleImprove.Transpiler;

public class MouseScrollTranspiler {
    // InputReader.SelectByScroll decides next/previous weapon with a single branch:
    //   ... ldc.r4 0.0 ; ble.un.s <prevWeapon>   (scroll up -> next, else -> previous)
    // Reversing scroll direction just means flipping that comparison to bge.un.s.
    //
    // The old implementation hard-coded Instructions()[4], but the game now multiplies the
    // scroll delta by mouseScrollMultiplier first (ldarg.0; ldfld mouseScrollMultiplier; mul),
    // which shifted the branch to index 7 and silently disabled this feature. Match the branch
    // by opcode instead so it survives such shifts.
    [HarmonyTranspiler, HarmonyPatch(typeof(InputReader), "SelectByScroll")]
    private static IEnumerable<CodeInstruction> ProjectileTranspiler(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ble_Un_S || i.opcode == OpCodes.Ble_Un));

        if (codeMatcher.IsValid) {
            codeMatcher.Instruction.opcode = OpCodes.Bge_Un_S;
            Plugin.LoggingInfo("MouseScrollTranspiler: reversed scroll comparison branch.", true);
        } else {
            Plugin.LoggingInfo("MouseScrollTranspiler: could not find scroll comparison branch to reverse.");
        }

        return codeMatcher.InstructionEnumeration();
    }
}
