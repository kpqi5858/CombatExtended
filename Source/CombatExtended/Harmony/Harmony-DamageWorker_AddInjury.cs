using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using UnityEngine;
using Harmony;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(DamageWorker_AddInjury), "ApplyDamageToPart")]
    internal static class Harmony_DamageWorker_AddInjury_ApplyDamageToPart
    {
        private static bool armorAbsorbed = false;

        private static void ArmorReroute(Pawn pawn, ref DamageInfo dinfo)
        {
            var newDinfo = ArmorUtilityCE.GetAfterArmorDamage(dinfo, pawn, dinfo.HitPart, out armorAbsorbed);
            if (dinfo.HitPart != newDinfo.HitPart)
            {
                if (pawn.Spawned) LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_ArmorSystem, OpportunityType.Critical);   // Inform the player about armor deflection
            }
            dinfo = newDinfo;
        }

        //[HarmonyPatch(typeof(ArmorUtility), "GetPostArmorDamage", new[] { typeof(Pawn), typeof(float), typeof(float), typeof(BodyPartRecord), typeof(DamageDef), typeof(bool), typeof(bool) })]
        //internal static class Harmony_GetPostArmorDamage
        //{
        //    internal static bool Prefix(ref float __result, Pawn pawn, float amount, float armorPenetration, BodyPartRecord part, ref DamageDef damageDef, out bool deflectedByMetalArmor, out bool diminishedByMetalArmor)
        //    {
        //        deflectedByMetalArmor = false;
        //        diminishedByMetalArmor = false;
        //        if (damageDef.armorCategory == null)
        //        {
        //            __result = amount;
        //            return false;
        //        }
        //        StatDef armorRatingStat = damageDef.armorCategory.armorRatingStat;
        //        if (pawn.apparel != null)
        //        {
        //            List<Apparel> wornApparel = pawn.apparel.WornApparel;
        //            for (int i = wornApparel.Count - 1; i >= 0; i--)
        //            {
        //                Apparel apparel = wornApparel[i];
        //                if (apparel.def.apparel.CoversBodyPart(part))
        //                {
        //                    float num = amount;
        //                    bool flag;

        //                //    DamageInfo dinfo = new DamageInfo();
        //                //    ArmorReroute(pawn, ref dinfo);

        //                    ArmorUtility.ApplyArmor(ref amount, armorPenetration, apparel.GetStatValue(armorRatingStat, true), apparel, ref damageDef, pawn, out flag);
        //                    if (amount < 0.001f)
        //                    {
        //                        deflectedByMetalArmor = flag;
        //                        __result = 0f;
        //                        return false;
        //                    }
        //                    if (amount < num && flag)
        //                    {
        //                        diminishedByMetalArmor = true;
        //                    }
        //                }
        //            }
        //        }
        //        float num2 = amount;
        //        bool flag2;
        //        ArmorUtility.ApplyArmor(ref amount, armorPenetration, pawn.GetStatValue(armorRatingStat, true), null, ref damageDef, pawn, out flag2);
        //        if (amount < 0.001f)
        //        {
        //            deflectedByMetalArmor = flag2;
        //            __result = 0f;
        //            return false;
        //        }
        //        if (amount < num2 && flag2)
        //        {
        //            diminishedByMetalArmor = true;
        //        }
        //        __result = amount;
        //        return false;
        //    }
        //}

        //internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    var codes = instructions.ToList();

        //    // Find armor block
        //    var armorBlockEnd = codes.FirstIndexOf(c => c.operand == typeof(ArmorUtility).GetMethod(nameof(ArmorUtility.GetPostArmorDamage)));
        //    int armorBlockStart = -1;
        //    for (int i = armorBlockEnd; i > 0; i--)
        //    {
        //        if (codes[i].opcode == OpCodes.Ldarg_2)
        //        {
        //            armorBlockStart = i;
        //            break;
        //        }
        //    }
        //    if (armorBlockStart == -1)
        //    {
        //        Log.Error("CE failed to transpile DamageWorker_AddInjury: could not identify armor block start");
        //        return instructions;
        //    }

        //    // Replace armor block with our new instructions
        //    // First, load arguments for ArmorReroute method onto stack (pawn is already loaded by vanilla)
        //    var curCode = codes[armorBlockStart + 1];
        //    curCode.opcode = OpCodes.Ldarga_S;
        //    curCode.operand = 1;

        //    curCode = codes[armorBlockStart + 2];
        //    curCode.opcode = OpCodes.Call;
        //    curCode.operand = typeof(Harmony_DamageWorker_AddInjury_ApplyDamageToPart).GetMethod(nameof(Harmony_DamageWorker_AddInjury_ApplyDamageToPart.ArmorReroute), AccessTools.all);

        //    // OpCode + 3 loads the dinfo we just modified and we want to access its damage value to store in the vanilla local variable at the end of the block
        //    curCode = codes[armorBlockStart + 4];
        //    curCode.opcode = OpCodes.Call;
        //    curCode.operand = typeof(DamageInfo).GetMethod("get_" + nameof(DamageInfo.Amount), AccessTools.all);

        //    curCode = codes[armorBlockStart + 5];
        //    curCode.opcode = OpCodes.Stloc_1;
        //    curCode.operand = null;

        //    // Null out the rest
        //    for (int i = armorBlockStart + 6; i <= armorBlockEnd + 1; i++)
        //    {
        //        curCode = codes[i];
        //        curCode.opcode = OpCodes.Nop;
        //        curCode.operand = null;
        //    }

        //    return codes;
        //}

        internal static void Postfix(DamageInfo dinfo, Pawn pawn)
        {
            if (!armorAbsorbed)
            {
                var props = dinfo.Weapon?.projectile as ProjectilePropertiesCE;
                if (props != null && !props.secondaryDamage.NullOrEmpty() && dinfo.Def == props.damageDef)
                {
                    foreach (SecondaryDamage sec in props.secondaryDamage)
                    {
                        if (pawn.Dead) return;
                        var secDinfo = sec.GetDinfo(dinfo);
                        pawn.TakeDamage(secDinfo);
                    }
                }
            }
            armorAbsorbed = false;
        }
    }
}
