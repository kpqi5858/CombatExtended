using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using Verse;
using RimWorld;
using System.Reflection.Emit;

namespace CombatExtended.Harmony
{
    /// <summary>
    /// Ignore bodypart hit error
    /// </summary>
    [HarmonyPatch(typeof(Hediff_Injury), "PostAdd")]
    internal static class Harmony_Hediff_Injury
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool patched = false;
            foreach(var inst in instructions)
            {
                yield return inst;
                if (inst.opcode == OpCodes.Call && !patched)
                {
                    patched = true;
                    yield return new CodeInstruction(OpCodes.Ret);
                }
            }
        }
    }
}
