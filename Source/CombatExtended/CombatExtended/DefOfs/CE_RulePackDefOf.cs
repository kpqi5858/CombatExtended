﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    [DefOf]
    public static class CE_RulePackDefOf
    {
        public static RulePackDef AttackMote;
        public static RulePackDef SuppressedMote;

        static CE_RulePackDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_RulePackDefOf));
        }
    }
}