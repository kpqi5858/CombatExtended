using System.Collections.Generic;
using System.Reflection;
using Harmony;
using Verse;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(ThingListGroupHelper), "Includes")]
    internal static class Harmony_ThingListGroupHelper
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return Transpilers.MethodReplacer(instructions, typeof(ThingDef).GetMethod("get_IsShell"), typeof(AmmoUtility).GetMethod("IsShell", BindingFlags.Static | BindingFlags.Public));
        }
    }
}