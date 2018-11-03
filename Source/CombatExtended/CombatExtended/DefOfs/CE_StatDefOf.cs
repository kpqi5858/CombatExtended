using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CombatExtended
{
    [DefOf]
    public static class CE_StatDefOf
    {
        // *** Item stats ***
        public static readonly StatDef Bulk; // for items in inventory
        public static readonly StatDef WornBulk; // worn apparel

        // *** Ranged weapon stats ***
        public static readonly StatDef ShotSpread; // pawn capacity
        public static readonly StatDef SwayFactor; // pawn capacity
        public static StatDef SightsEfficiency;
        public static readonly StatDef AimingAccuracy; // pawn capacity
        public static readonly StatDef ReloadSpeed; // pawn capacity

        // *** Melee weapon stats ***
        public static StatDef MeleePenetrationFactor;
        public static StatDef MeleeCounterParryBonus;

        // *** Pawn stats ***
        public static StatDef CarryBulk;    // Inventory max space
        public static StatDef CarryWeight;  // Inventory max weight
        public static StatDef MeleeCritChance;
        public static StatDef MeleeDodgeChance;
        public static StatDef MeleeParryChance;

        public static StatDef Suppressability;

        static CE_StatDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_StatDefOf));
        }
    }
}