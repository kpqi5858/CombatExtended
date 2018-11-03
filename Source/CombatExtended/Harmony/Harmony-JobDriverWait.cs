using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Verse;
using Verse.AI;

/*
 * Targetting the Verse.AI.JobDriver_Wait.CheckForAutoAttack()
 * Target Line:
 * >Thing thing = AttackTargetFinder.BestShootTargetFromCurrentPosition(base.pawn, targetScanFlags);
 *  
 * Basically modify that line to read something like:
 * >Thing thing = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(base.pawn, targetScanFlags, GetValidTargetPredicate(verb));
 * 
 * Overall does a couple of things.  First it locates the local variable with the verb used to attack with and second it locates the null argument in the above method call
 * for Predicate and replaces that with an arg stack load of the verb and a call to create a predicate. That call removes the verb from the call stack and replaces it
 * with a predicate (or a null).
 * 
 * A couple of helper functions to turn a bunch of ifs into a single call since IL can use one of 6 instructions for local variable load/save.
 */

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(JobDriver_Wait), "CheckForAutoAttack")]
    static class Harmony_JobDriverWait_CheckForAutoAttack
    {
        static readonly string logPrefix = Assembly.GetExecutingAssembly().GetName().Name + " :: " + typeof(Harmony_JobDriverWait_CheckForAutoAttack).Name + " :: ";

        /// <summary>
        /// Transpiler runs through the IL code of the method CheckForAutoAttack and makes some tweaks to a call so as to avoid having the pawn attack a target it can't hit.
        /// </summary>
        /// <param name="instructions">IEnumerable of CodeInstruction, required by Harmony, the IL code Harmony fetched/uses.</param>
        /// <returns>IEnumerable of CodeInstruction containing the changes to the method's IL code.</returns>
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int verbLocalIndex = -1;
            int indexKeyCall = 0;

            // turn instructions into a list so we can walk through it variably (instead of forward only).
            List<CodeInstruction> code = instructions.ToList();

            // walk forward to find some key information.
            for (int i = 0; i < code.Count; i++)
            {
                CodeInstruction current = code[i];

                // look for the verb instantiation/storage.
                var methodBase = current.operand as MethodBase;
                if (current.opcode == OpCodes.Callvirt && methodBase != null &&
                    methodBase.DeclaringType == typeof(Pawn) && methodBase.Name == "get_" + nameof(Pawn.CurrentEffectiveVerb) &&
                    code.Count >= i + 1)
                {
                    verbLocalIndex = HarmonyBase.OpcodeStoreIndex(code[i + 1]);
                }

                // see if we've found the instruction index of the key call.
                var methodInfo = current.operand as MethodInfo;
                if (current.opcode == OpCodes.Call && methodInfo != null &&
                    methodInfo.DeclaringType == typeof(AttackTargetFinder) && methodInfo.Name == nameof(AttackTargetFinder.BestShootTargetFromCurrentPosition))
                {
                    indexKeyCall = i;
                    break;
                }
            }

            // if verb didn't find
            if (verbLocalIndex < 0)
            {
                Log.Warning("Verb didn't find in " + nameof(Harmony_JobDriverWait_CheckForAutoAttack));
                return code;
            }

            // walk backwards from the key call to locate the null load and replace it with our call to drop in our predicate into the arg stack.
            for (int i = indexKeyCall; i >= 0; i--)
            {
                if (code[i].opcode == OpCodes.Ldnull)
                {
                    code[i++] = HarmonyBase.MakeLocalLoadInstruction(verbLocalIndex);
                    var call = new CodeInstruction(
                        OpCodes.Call,
                        typeof(Harmony_JobDriverWait_CheckForAutoAttack)
                            .GetMethod(nameof(Harmony_JobDriverWait_CheckForAutoAttack.GetValidTargetPredicate), AccessTools.all));
                    code.Insert(i, call);
                    break;
                }
            }

            return code;
        }

        /// <summary>
        /// Returns a predicate for valid targets if the verb is a Verb_LaunchProjectileCE or descendent.
        /// </summary>
        /// <param name="verb">Verb that is to be checked for type and used for valid target checking</param>
        /// <returns>Predicate of type Thing which indicates if that thing is a valid target for the pawn.</returns>
        static Predicate<Thing> GetValidTargetPredicate(Verb verb)
        {
            Predicate<Thing> predicate = null;
            var verbCe = verb as Verb_LaunchProjectileCE;
            if (verbCe != null)
            {
                predicate = t => verbCe.CanHitTargetFrom(verb.caster.Position, new LocalTargetInfo(t));
            }

            return predicate;
        }
    }
}