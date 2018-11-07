using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;

namespace CombatExtended.Harmony
{
    /* This class handles patching of the Core PawnColumnWorkers that are used on the assign tab which are similar to the PawnColumnWorker_Loadout.
     * This patch reads the variables from PawnColumnWorker_Loadout and sets Core to use the same values so all 3 workers use the same size information.
     * This is relatively straight forward in that it's just replacing some constants.
     */
    static class PawnColumnWorkers_Resize
    {
        static readonly string logPrefix = Assembly.GetExecutingAssembly().GetName().Name + " :: " + typeof(PawnColumnWorkers_Resize).Name + " :: ";

        static readonly float orgMinWidth = 194f;
        static readonly float orgOptimalWidth = 354f;

        /// <summary>
        /// Handles the patch work as this is more efficiently handled as a complex patch instead of automatically by Harmony.
        /// </summary>
        public static void Patch()
        {
            Type[] targetTypes =
            {
                typeof(PawnColumnWorker_Outfit),
                typeof(PawnColumnWorker_DrugPolicy)
            };

            string[] targetNames =
            {
                nameof(PawnColumnWorker.GetMinWidth),
                nameof(PawnColumnWorker.GetOptimalWidth)
            };

            HarmonyMethod[] transpilers =
            {
                new HarmonyMethod(typeof(PawnColumnWorkers_Resize), nameof(PawnColumnWorkers_Resize.MinWidth)),
                new HarmonyMethod(typeof(PawnColumnWorkers_Resize), nameof(PawnColumnWorkers_Resize.OptWidth))
            };

            foreach (Type target in targetTypes)
            {
                for (int i = 0; i < targetNames.Length; i++)
                {
                    MethodBase method = target.GetMethod(targetNames[i], AccessTools.all);
                    HarmonyBase.instance.Patch(method, null, null, (HarmonyMethod)transpilers[i]);
                }
            }
        }

        /// <summary>
        /// Transpiler for GetMinWidth method in Core.  Replaces constant values in the original method with constants from CE.
        /// </summary>
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> MinWidth(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                CheckAndUpdate(instruction, orgMinWidth, PawnColumnWorker_Loadout._MinWidth);
                yield return instruction;
            }
        }

        /// <summary>
        /// Transpiler for GetOptimalWidth method in Core.  Replaces constant values in the original method with constants from CE.
        /// </summary>
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> OptWidth(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                CheckAndUpdate(instruction, orgOptimalWidth, PawnColumnWorker_Loadout._OptimalWidth);
                yield return instruction;
            }
        }

        /// <summary>
        /// Will update instruction with newValue, if instruction operand equals oldValue
        /// </summary>
        private static void CheckAndUpdate(CodeInstruction instruction, float oldValue, float newValue)
        {
            if (instruction.opcode != OpCodes.Ldc_R4) return;

            float? currentValue = instruction.operand as float?;
            if (currentValue.HasValue && currentValue.Value.Equals(oldValue))
            {
                instruction.operand = newValue;
            }
        }
    }
}