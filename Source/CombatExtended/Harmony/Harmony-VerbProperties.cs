using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Harmony;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(VerbProperties))]
    [HarmonyPatch("LaunchesProjectile", MethodType.Getter)]
    internal static class Harmony_VerbProperties
    {
        internal static void Postfix(VerbProperties __instance, ref bool __result)
        {
            if (!__result)
            {
                __result = typeof(Verb_LaunchProjectileCE).IsAssignableFrom(__instance.verbClass);
            }
        }
    }

    [HarmonyPatch(typeof(VerbProperties))]
    [HarmonyPatch("CausesExplosion", MethodType.Getter)]
    internal static class VerbProperties_Patch
    {
        internal static void Postfix(VerbProperties __instance, ref bool __result)
        {
            __result = __instance.defaultProjectile != null && (typeof(Projectile_Explosive).IsAssignableFrom(__instance.defaultProjectile.thingClass) || typeof(ProjectileCE_Explosive).IsAssignableFrom(__instance.defaultProjectile.thingClass) || typeof(Projectile_DoomsdayRocket).IsAssignableFrom(__instance.defaultProjectile.thingClass));
        }
    }

}
