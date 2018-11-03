﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CombatExtended
{
    [DefOf]
    public static class CE_ThingDefOf
    {
        public static readonly ThingDef Mote_SuppressIcon;
        public static readonly ThingDef Mote_HunkerIcon;

        public static ThingDef FSX;
        public static ThingDef ExplosionCE;

        public static ThingDef AmmoBench;
        public static ThingDef FilthPrometheum;

        static CE_ThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_ThingDefOf));
        }
    }
}