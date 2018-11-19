using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class SecondaryDamage
    {
        public DamageDef def;
        public int amount;

        public DamageInfo GetDinfo()
        {
            return new DamageInfo(def, amount);
        }

        public DamageInfo GetDinfo(DamageInfo primaryDinfo)
        {
            var projectilePropertiesCE = primaryDinfo.Weapon.projectile as ProjectilePropertiesCE;
            float ap = primaryDinfo.ArmorPenetrationInt;
            if (projectilePropertiesCE != null)
            {
                ap = projectilePropertiesCE.GetArmorPenetration(1);
            }

            var dinfo = new DamageInfo(def,
                            amount,
                            ap, //Armor Penetration TODO: Fix this after DamageWorker restructuring.
                            primaryDinfo.Angle,
                            primaryDinfo.Instigator,
                            primaryDinfo.HitPart,
                            primaryDinfo.Weapon);
            dinfo.SetBodyRegion(primaryDinfo.Height, primaryDinfo.Depth);
            return dinfo;
        }
    }
}