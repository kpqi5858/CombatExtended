using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class VerbPropertiesCE : VerbProperties
    {
        public RecoilPattern recoilPattern = RecoilPattern.None;
        public float recoilAmount = 0;
        public float indirectFirePenalty = 0;
        public new float meleeArmorPenetrationBase = 0;
        public bool ejectsCasings = true;
        public bool ignorePartialLoSBlocker = false;


        public BodyPartGroupDef AdjustedLinkedBodyPartsGroupCE(ToolCE tool)
        {
            if (tool != null)
            {
                return tool.linkedBodyPartsGroup;
            }
            return this.linkedBodyPartsGroup;
        }

        public float AdjustedArmorPenetrationCE(ToolCE tool, Pawn attacker, Thing equipment, HediffComp_VerbGiver hediffCompSource)
        {
            float num;
            if (tool != null)
            {
                num = tool.armorPenetration;
            }
            else
            {
                num = this.meleeArmorPenetrationBase;
            }
            if (num < 0f)
            {
                float num2 = this.AdjustedMeleeDamageAmount(tool, attacker, equipment, hediffCompSource);
                num = num2 * 0.015f;
            }
            return num;
        }
    }
}