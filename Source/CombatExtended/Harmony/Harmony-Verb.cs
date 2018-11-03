using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using RimWorld;
using Verse.AI;
using System.Reflection;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(Verb), "IsStillUsableBy")]
    internal static class Harmony_Verb
    {
        internal static void Postfix(Verb __instance, ref bool __result, Pawn pawn)
        {
            if (__result)
            {
                var tool = __instance.tool as ToolCE;
                if (tool != null)
                {
                    __result = tool.restrictedGender == Gender.None || tool.restrictedGender == pawn.gender;
                }
            }
        }
    }

    /*
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), "TryMeleeAttack")]
    internal static class TryMeleeAttack_Patch
    {
        public static FieldInfo fi = typeof(Pawn_MeleeVerbs).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static void Postfix(Pawn_MeleeVerbs __instance, ref bool __result, Thing target, Verb verbToUse = null, bool surpriseAttack = false)
        {
            Pawn pawn = (Pawn)fi.GetValue(__instance);
            if (pawn != null && pawn.RaceProps != null && (pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid))
            {
                
                bool hasPrimaryVerb = (pawn.equipment != null && pawn.equipment.PrimaryEq != null && pawn.equipment.PrimaryEq.PrimaryVerb != null);
                if (hasPrimaryVerb)
                {
                    Verb primaryVerb = pawn.equipment.PrimaryEq.PrimaryVerb;
                    if (!primaryVerb.verbProps.MeleeRange)
                    {
                        bool fw = pawn.Faction != null && pawn.Faction.IsPlayer && pawn.drafter != null && (pawn.Drafted || !pawn.drafter.FireAtWill);
                        if (!fw)
                        {
                            TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns | TargetScanFlags.NeedThreat;
                            if (primaryVerb.IsIncendiary())
                            {
                                targetScanFlags |= TargetScanFlags.NeedNonBurning;
                            }
                            Thing thing = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(pawn, null, primaryVerb.verbProps.range, primaryVerb.verbProps.minRange, targetScanFlags);
                            if (thing != null)
                            {
                                Job job = new Job(JobDefOf.AttackStatic, thing)
                                {
                                    checkOverrideOnExpire = true,
                                    expiryInterval = 400
                                };
                                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            }
                        }
                    }
                }
            }
        }
    }
    */
}
