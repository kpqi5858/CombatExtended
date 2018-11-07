using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;

namespace CombatExtended
{
    public class JobGiver_TakeAndEquip : ThinkNode_JobGiver
    {
        private enum WorkPriority
        {
            None,
            Unloading,
            LowAmmo,
            Weapon,
            Ammo
            //Apparel
        }

        private WorkPriority GetPriorityWork(Pawn pawn)
        {
            CompAmmoUser primaryammouser = pawn.equipment.Primary.TryGetComp<CompAmmoUser>();
            CompInventory compammo = pawn.TryGetComp<CompInventory>();

            if (pawn.Faction.IsPlayer && pawn.equipment.Primary != null && pawn.equipment.Primary.TryGetComp<CompAmmoUser>() != null)
            {
                Loadout loadout = pawn.GetLoadout();
                // if (loadout != null && !loadout.Slots.NullOrEmpty())
                if (loadout != null && loadout.SlotCount > 0)
                {
                    return WorkPriority.None;
                }
            }

            if (pawn.kindDef.trader)
            {
                return WorkPriority.None;
            }
            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.Tame)
            {
                return WorkPriority.None;
            }

            if (pawn.equipment.Primary == null)
            {
                if (Unload(pawn))
                {
                    return WorkPriority.Unloading;
                }
                else return WorkPriority.Weapon;
            }

            if (pawn.equipment.Primary != null && primaryammouser != null)
            {
                int ammocount = 0;
                foreach (AmmoLink link in primaryammouser.Props.ammoSet.ammoTypes)
                {
                    Thing ammoThing;
                    ammoThing = compammo.ammoList.Find(thing => thing.def == link.ammo);
                    if (ammoThing != null)
                    {
                        ammocount += ammoThing.stackCount;
                    }
                }

                // current ammo bulk 
                float currentAmmoBulk = primaryammouser.CurrentAmmo.GetStatValueAbstract(CE_StatDefOf.Bulk);

                // weapon magazine size 
                float stackSize = primaryammouser.Props.magazineSize;

                // weight projectile ratio to free bulk with x1.5 reserve
                float weightProjectileRatio = Mathf.RoundToInt(((compammo.capacityBulk - compammo.currentBulk) / 1.5f) / currentAmmoBulk);

                if (ammocount < stackSize * 1 && (ammocount < weightProjectileRatio))
                {
                    if (Unload(pawn))
                    {
                        return WorkPriority.Unloading;
                    }
                    else return WorkPriority.LowAmmo;
                }

                if (!PawnUtility.EnemiesAreNearby(pawn, 30, true))
                {
                    if (ammocount < stackSize * 2 && (ammocount < weightProjectileRatio))
                    {
                        if (Unload(pawn))
                        {
                            return WorkPriority.Unloading;
                        }
                        else return WorkPriority.Ammo;
                    }
                }
            }
            /*
            if (!pawn.Faction.IsPlayer && pawn.equipment.Primary != null
                && !PawnUtility.EnemiesAreNearby(pawn, 30, true)
                || (!pawn.apparel.BodyPartGroupIsCovered(BodyPartGroupDefOf.Torso)
                || !pawn.apparel.BodyPartGroupIsCovered(BodyPartGroupDefOf.Legs)))
            {
                return WorkPriority.Apparel;
            }
            */

            return WorkPriority.None;
        }

        public override float GetPriority(Pawn pawn)
        {
            if ((!Controller.settings.AutoTakeAmmo && pawn.IsColonist) || !Controller.settings.EnableAmmoSystem) return 0f;

            var priority = GetPriorityWork(pawn);

            if (priority == WorkPriority.Unloading) return 9.2f;
            else if (priority == WorkPriority.LowAmmo) return 9f;
            else if (priority == WorkPriority.Weapon) return 8f;
            else if (priority == WorkPriority.Ammo) return 6f;
            //else if (priority == WorkPriority.Apparel) return 5f;
            else if (priority == WorkPriority.None) return 0f;

            TimeAssignmentDef assignment = (pawn.timetable != null) ? pawn.timetable.CurrentAssignment : TimeAssignmentDefOf.Anything;
            if (assignment == TimeAssignmentDefOf.Sleep) return 0f;

            if (pawn.health == null || pawn.Downed || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return 0f;
            }
            else return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!Controller.settings.EnableAmmoSystem || !Controller.settings.AutoTakeAmmo)
            {
                return null;
            }

            if (!pawn.RaceProps.Humanlike || (pawn.story != null && pawn.story.WorkTagIsDisabled(WorkTags.Violent)))
            {
                return null;
            }
            if (pawn.Faction.IsPlayer && pawn.Drafted)
            {
                return null;
            }

            if (!Rand.MTBEventOccurs(60, 1, 30))
            {
                return null;
            }

            // Log.Message(pawn.ToString() +  " - priority:" + (GetPriorityWork(pawn)).ToString() + " capacityWeight: " + pawn.TryGetComp<CompInventory>().capacityWeight.ToString() + " currentWeight: " + pawn.TryGetComp<CompInventory>().currentWeight.ToString() + " capacityBulk: " + pawn.TryGetComp<CompInventory>().capacityBulk.ToString() + " currentBulk: " + pawn.TryGetComp<CompInventory>().currentBulk.ToString());

            var brawler = (pawn.story != null && pawn.story.traits != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler));
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            bool hasPrimary = (pawn.equipment != null && pawn.equipment.Primary != null);
            CompAmmoUser primaryammouser = hasPrimary ? pawn.equipment.Primary.TryGetComp<CompAmmoUser>() : null;

            if (inventory != null)
            {
                // Prefer ranged weapon in inventory
                if (!pawn.Faction.IsPlayer && hasPrimary && pawn.equipment.Primary.def.IsMeleeWeapon && !brawler)
                {
                    if ((pawn.skills.GetSkill(SkillDefOf.Shooting).Level >= pawn.skills.GetSkill(SkillDefOf.Melee).Level
                         || pawn.skills.GetSkill(SkillDefOf.Shooting).Level >= 6))
                    {
                        ThingWithComps InvListGun3 = inventory.rangedWeaponList.Find(thing => thing.TryGetComp<CompAmmoUser>() != null && thing.TryGetComp<CompAmmoUser>().HasAmmoOrMagazine);
                        if (InvListGun3 != null)
                        {
                            inventory.TrySwitchToWeapon(InvListGun3);
                        }
                    }
                }

                // Drop excess ranged weapon
                if (!pawn.Faction.IsPlayer && primaryammouser != null && GetPriorityWork(pawn) == WorkPriority.Unloading && inventory.rangedWeaponList.Count >= 1)
                {
                    Thing ListGun = inventory.rangedWeaponList.Find(thing => thing.TryGetComp<CompAmmoUser>() != null && thing.def != pawn.equipment.Primary.def);
                    if (ListGun != null)
                    {
                        Thing ammoListGun = null;
                        if (!ListGun.TryGetComp<CompAmmoUser>().HasAmmoOrMagazine)
                            foreach (AmmoLink link in ListGun.TryGetComp<CompAmmoUser>().Props.ammoSet.ammoTypes)
                            {
                                if (inventory.ammoList.Find(thing => thing.def == link.ammo) == null)
                                {
                                    ammoListGun = ListGun;
                                    break;
                                }
                            }
                        if (ammoListGun != null)
                        {
                            Thing droppedWeapon;
                            if (inventory.container.TryDrop(ListGun, pawn.Position, pawn.Map, ThingPlaceMode.Near, ListGun.stackCount, out droppedWeapon))
                            {
                                pawn.jobs.EndCurrentJob(JobCondition.None, true);
                                pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.DropEquipment, droppedWeapon, 30, true));
                            }
                        }
                    }
                }

                // Find and drop not need ammo from inventory
                if (!pawn.Faction.IsPlayer && hasPrimary && inventory.ammoList.Count > 1 && GetPriorityWork(pawn) == WorkPriority.Unloading)
                {
                    Thing WrongammoThing = null;
                    WrongammoThing = primaryammouser != null
                        ? inventory.ammoList.Find(thing => !primaryammouser.Props.ammoSet.ammoTypes.Any(a => a.ammo == thing.def))
                        : inventory.ammoList.RandomElement<Thing>();

                    if (WrongammoThing != null)
                    {
                        Thing InvListGun = inventory.rangedWeaponList.Find(thing => hasPrimary && thing.TryGetComp<CompAmmoUser>() != null && thing.def != pawn.equipment.Primary.def);
                        if (InvListGun != null)
                        {
                            Thing ammoInvListGun = null;
                            foreach (AmmoLink link in InvListGun.TryGetComp<CompAmmoUser>().Props.ammoSet.ammoTypes)
                            {
                                ammoInvListGun = inventory.ammoList.Find(thing => thing.def == link.ammo);
                                break;
                            }
                            if (ammoInvListGun != null && ammoInvListGun != WrongammoThing)
                            {
                                Thing droppedThingAmmo;
                                if (inventory.container.TryDrop(ammoInvListGun, pawn.Position, pawn.Map, ThingPlaceMode.Near, ammoInvListGun.stackCount, out droppedThingAmmo))
                                {
                                    pawn.jobs.EndCurrentJob(JobCondition.None, true);
                                    pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.DropEquipment, 30, true));
                                }
                            }
                        }
                        else
                        {
                            Thing droppedThing;
                            if (inventory.container.TryDrop(WrongammoThing, pawn.Position, pawn.Map, ThingPlaceMode.Near, WrongammoThing.stackCount, out droppedThing))
                            {
                                pawn.jobs.EndCurrentJob(JobCondition.None, true);
                                pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.DropEquipment, 30, true));
                            }
                        }
                    }
                }

                Room room = RegionAndRoomQuery.RoomAtFast(pawn.Position, pawn.Map);

                // Find weapon in inventory and try to switch if any ammo in inventory.
                if (GetPriorityWork(pawn) == WorkPriority.Weapon && !hasPrimary)
                {
                    ThingWithComps InvListGun2 = inventory.rangedWeaponList.Find(thing => thing.TryGetComp<CompAmmoUser>() != null);

                    if (InvListGun2 != null)
                    {
                        Thing ammoInvListGun2 = null;
                        foreach (AmmoLink link in InvListGun2.TryGetComp<CompAmmoUser>().Props.ammoSet.ammoTypes)
                        {
                            ammoInvListGun2 = inventory.ammoList.Find(thing => thing.def == link.ammo);
                            break;
                        }
                        if (ammoInvListGun2 != null)
                        {
                            inventory.TrySwitchToWeapon(InvListGun2);
                        }
                    }

                    // Find weapon with near ammo for ai.
                    if (!pawn.Faction.IsPlayer)
                    {
                        Predicate<Thing> validatorWS = (Thing w) => w.def.IsWeapon
                            && w.MarketValue > 5
                            && pawn.CanReserve(w, 1)
                            && pawn.Position.InHorDistOf(w.Position, fixedsearchrange(pawn, w, 30f))
                            && pawn.CanReach(w, PathEndMode.Touch, Danger.Deadly, true)
                            && (pawn.Faction.HostileTo(Faction.OfPlayer) || pawn.Faction == Faction.OfPlayer || (!pawn.Faction.HostileTo(Faction.OfPlayer) && !pawn.Map.areaManager.Home[w.Position]));

                        // generate a list of all weapons (this includes melee weapons)
                        List<Thing> allWeapons = (
                            from w in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways)
                            where validatorWS(w)
                            orderby w.MarketValue - w.Position.DistanceToSquared(pawn.Position) * 2f descending
                            select w
                            ).ToList();

                        // now just get the ranged weapons out...
                        List<Thing> rangedWeapons = allWeapons.Where(w => w.def.IsRangedWeapon).ToList();
                        if (!rangedWeapons.NullOrEmpty())
                        {
                            foreach (Thing thing in rangedWeapons)
                            {
                                if (thing.TryGetComp<CompAmmoUser>() == null)
                                {
                                    // pickup a non-CE ranged weapon...
                                    int numToThing = 0;
                                    if (inventory.CanFitInInventory(thing, out numToThing))
                                    {
                                        return new Job(JobDefOf.Equip, thing)
                                        {
                                            checkOverrideOnExpire = true,
                                            expiryInterval = 100
                                        };
                                    }
                                }
                                else
                                {
                                    // pickup a CE ranged weapon...
                                    List<ThingDef> thingDefAmmoList = thing.TryGetComp<CompAmmoUser>().Props.ammoSet.ammoTypes.Select(g => g.ammo as ThingDef).ToList();

                                    Predicate<Thing> validatorA = (Thing t) => t.def.category == ThingCategory.Item
                                        && t is AmmoThing && pawn.CanReserve(t, 1)
                                        && pawn.Position.InHorDistOf(t.Position, fixedsearchrange(pawn, t, 30f))
                                        && pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly, true)
                                        && (pawn.Faction.HostileTo(Faction.OfPlayer) || pawn.Faction == Faction.OfPlayer || (!pawn.Faction.HostileTo(Faction.OfPlayer) && !pawn.Map.areaManager.Home[t.Position]));

                                    List<Thing> thingAmmoList = (
                                        from t in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways)
                                        where validatorA(t)
                                        select t
                                        ).ToList();

                                    if (thingAmmoList.Count > 0 && thingDefAmmoList.Count > 0)
                                    {
                                        int desiredStackSize = thing.TryGetComp<CompAmmoUser>().Props.magazineSize * 2;
                                        Thing th = thingAmmoList.FirstOrDefault(x => thingDefAmmoList.Contains(x.def) && x.stackCount > desiredStackSize);
                                        if (th != null)
                                        {
                                            int numToThing = 0;
                                            if (inventory.CanFitInInventory(thing, out numToThing))
                                            {
                                                return new Job(JobDefOf.Equip, thing)
                                                {
                                                    checkOverrideOnExpire = true,
                                                    expiryInterval = 100
                                                };
                                            }
                                        }
                                    }
                                    else if (isGrenade(thing) && grenadeCountInInventory(pawn, inventory) < 5)
                                    {
                                        int numToThing = thing.stackCount > 7 ? 7 : thing.stackCount;
                                        if (inventory.CanFitInInventory(thing, out numToThing))
                                        {
                                            return new Job(JobDefOf.TakeInventory, thing)
                                            {
                                                count = Mathf.RoundToInt(numToThing * 0.8f),
                                                checkOverrideOnExpire = true,
                                                expiryInterval = 150
                                            };
                                        }
                                    }
                                }
                            }
                        }
                        // else if no ranged weapons with nearby ammo was found, lets consider a melee weapon.
                        if (allWeapons != null && allWeapons.Count > 0)
                        {
                            // since we don't need to worry about ammo, just pick one.
                            Thing meleeWeapon = allWeapons.FirstOrDefault(w => !w.def.IsRangedWeapon && w.def.IsMeleeWeapon);

                            if (meleeWeapon != null)
                            {
                                return new Job(JobDefOf.Equip, meleeWeapon)
                                {
                                    checkOverrideOnExpire = true,
                                    expiryInterval = 100
                                };
                            }
                        }
                    }
                }

                // Find ammo
                if ((GetPriorityWork(pawn) == WorkPriority.Ammo || GetPriorityWork(pawn) == WorkPriority.LowAmmo)
                    && primaryammouser != null)
                {
                    List<ThingDef> curAmmoList = (from AmmoLink g in primaryammouser.Props.ammoSet.ammoTypes
                                                  select g.ammo as ThingDef).ToList();

                    if (curAmmoList.Count > 0)
                    {
                        Predicate<Thing> validator = (Thing t) => t is AmmoThing && pawn.CanReserve(t, 1)
                                        && pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly, true)
                                        && ((pawn.Faction.IsPlayer && !ForbidUtility.IsForbidden(t, pawn)) || (!pawn.Faction.IsPlayer && pawn.Position.InHorDistOf(t.Position, fixedsearchrange(pawn, t, 30f))))
                                        && (pawn.Faction.HostileTo(Faction.OfPlayer) || pawn.Faction == Faction.OfPlayer || (!pawn.Faction.HostileTo(Faction.OfPlayer) && !pawn.Map.areaManager.Home[t.Position]));
                        List<Thing> curThingList = (
                            from t in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways)
                            where validator(t)
                            select t
                            ).ToList();
                        foreach (Thing th in curThingList)
                        {
                            foreach (ThingDef thd in curAmmoList)
                            {
                                if (thd == th.def)
                                {
                                    //Defence from low count loot spam
                                    float thw = (th.GetStatValue(CE_StatDefOf.Bulk)) * th.stackCount;
                                    if (thw > 0.5f)
                                    {
                                        if (pawn.Faction.IsPlayer)
                                        {
                                            int SearchRadius = 0;
                                            if (GetPriorityWork(pawn) == WorkPriority.LowAmmo) SearchRadius = 70;
                                            else SearchRadius = 30;

                                            Thing closestThing = GenClosest.ClosestThingReachable(
                                            pawn.Position,
                                            pawn.Map,
                                            ThingRequest.ForDef(th.def),
                                            PathEndMode.ClosestTouch,
                                            TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn),
                                            SearchRadius,
                                            x => !x.IsForbidden(pawn) && pawn.CanReserve(x));

                                            if (closestThing != null)
                                            {
                                                int numToCarry = 0;
                                                if (inventory.CanFitInInventory(th, out numToCarry))
                                                {
                                                    return new Job(JobDefOf.TakeInventory, th)
                                                    {
                                                        count = numToCarry
                                                    };
                                                }
                                            }
                                        }
                                        else
                                        {
                                            int numToCarry = 0;
                                            if (inventory.CanFitInInventory(th, out numToCarry))
                                            {
                                                return new Job(JobDefOf.TakeInventory, th)
                                                {
                                                    count = numToCarry,
                                                    expiryInterval = 150,
                                                    checkOverrideOnExpire = true
                                                };
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // postfind grenade and ammos.
                if (!pawn.Faction.IsPlayer)
                {
                    int minrange = 2;
                    int maxrange = 14;
                    float rangevariation = hasPrimary ? minrange : maxrange / 2;
                    Predicate<Thing> validatorPost = (Thing w) =>
                        (isGrenade(w) || w is AmmoThing)
                        && pawn.Position.InHorDistOf(w.Position, fixedsearchrange(pawn, w, maxrange, Mathf.RoundToInt(rangevariation + (maxrange / 2)), Mathf.RoundToInt(rangevariation + minrange)))
                        && pawn.CanReach(w, PathEndMode.Touch, Danger.Deadly, true)
                        && (pawn.Faction.HostileTo(Faction.OfPlayer) || (!pawn.Faction.HostileTo(Faction.OfPlayer) && !pawn.Map.areaManager.Home[w.Position]));

                    List<Thing> allPostThings = (
                        from w in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways)
                        where validatorPost(w)
                        select w
                        ).ToList();

                    // Look for grenades
                    List<Thing> grenadelist = (
                        from g in allPostThings
                        where g.def.IsWeapon && pawn.CanReserve(g, 1)
                        orderby g.Position.DistanceToSquared(pawn.Position) * 2f descending
                        select g
                        ).ToList();

                    if (!grenadelist.NullOrEmpty() && grenadeCountInInventory(pawn, inventory) < 5)
                    {
                        foreach (Thing grenade in grenadelist)
                        {
                            int numToThing = grenade.stackCount > 7 ? 7 : grenade.stackCount;
                            if (inventory.CanFitInInventory(grenade, out numToThing))
                            {
                                return new Job(JobDefOf.TakeInventory, grenade)
                                {
                                    count = Mathf.RoundToInt(numToThing * 0.8f),
                                    expiryInterval = 150,
                                    checkOverrideOnExpire = true
                                };
                            }
                        }
                    }
                    if (hasPrimary && primaryammouser != null)
                    {
                        List<ThingDef> curAmmoList = (from AmmoLink g in primaryammouser.Props.ammoSet.ammoTypes
                                                      select g.ammo as ThingDef).ToList();
                        if (curAmmoList.Count > 0)
                        {
                            List<Thing> ammolist = (
                                from p in allPostThings
                                where p is AmmoThing && curAmmoList.Contains(p.def) && pawn.CanReserve(p, 1)
                                orderby p.Position.DistanceToSquared(pawn.Position) * 2f descending
                                select p
                                ).ToList();

                            if (ammolist.Count > 0)
                            {
                                for (var i = 0; i < ammolist.Count; i++)
                                {
                                    //try to take more ammo if it really nearby.
                                    int numToThing2 = 0;
                                    if (!inventory.CanFitInInventory(ammolist[i], out numToThing2))
                                    {
                                        break;
                                    }
                                    if (PawnUtility.EnemiesAreNearby(pawn, 25, true))
                                    {
                                        break;
                                    }
                                    if (pawn.Position.InHorDistOf(ammolist[i].Position, 4f))
                                    {
                                        return new Job(JobDefOf.TakeInventory, ammolist[i])
                                        {
                                            count = numToThing2,
                                            expiryInterval = 150,
                                            checkOverrideOnExpire = true
                                        };
                                    }
                                }
                            }
                        }
                    }
                }

                /*
                if (!pawn.Faction.IsPlayer && pawn.apparel != null && GetPriorityWork(pawn) == WorkPriority.Apparel)
                {
                    if (!pawn.apparel.BodyPartGroupIsCovered(BodyPartGroupDefOf.Torso))
                    {
                        Apparel apparel = this.FindGarmentCoveringPart(pawn, BodyPartGroupDefOf.Torso);
                        if (apparel != null)
                        {
                            int numToapparel = 0;
                            if (inventory.CanFitInInventory(apparel, out numToapparel))
                            {
                                return new Job(JobDefOf.Wear, apparel)
                                {
                                    ignoreForbidden = true
                                };
                            }
                        }
                    }
                    if (!pawn.apparel.BodyPartGroupIsCovered(BodyPartGroupDefOf.Legs))
                    {
                        Apparel apparel2 = this.FindGarmentCoveringPart(pawn, BodyPartGroupDefOf.Legs);
                        if (apparel2 != null)
                        {
                            int numToapparel2 = 0;
                            if (inventory.CanFitInInventory(apparel2, out numToapparel2))
                            {
                                return new Job(JobDefOf.Wear, apparel2)
                                {
                                    ignoreForbidden = true
                                };
                            }
                        }
                    }
                    if (!pawn.apparel.BodyPartGroupIsCovered(BodyPartGroupDefOf.FullHead))
                    {
                        Apparel apparel3 = this.FindGarmentCoveringPart(pawn, BodyPartGroupDefOf.FullHead);
                        if (apparel3 != null)
                        {
                            int numToapparel3 = 0;
                            if (inventory.CanFitInInventory(apparel3, out numToapparel3))
                            {
                                return new Job(JobDefOf.Wear, apparel3)
                                {
                                    ignoreForbidden = true,
                                    locomotionUrgency = LocomotionUrgency.Sprint
                                };
                            }
                        }
                    }
                }
                */
                return null;
            }
            return null;
        }

        /*
        private static Job GotoForce(Pawn pawn, LocalTargetInfo target, PathEndMode pathEndMode)
        {
            using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, target, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings, false), pathEndMode))
            {
                IntVec3 cellBeforeBlocker;
                Thing thing = pawnPath.FirstBlockingBuilding(out cellBeforeBlocker, pawn);
                if (thing != null)
                {
                    Job job = DigUtility.PassBlockerJob(pawn, thing, cellBeforeBlocker, true);
                    if (job != null)
                    {
                        return job;
                    }
                }
                if (thing == null)
                {
                    return new Job(JobDefOf.Goto, target, 100, true);
                }
                if (pawn.equipment.Primary != null)
                {
                    Verb primaryVerb = pawn.equipment.PrimaryEq.PrimaryVerb;
                    if (primaryVerb.verbProps.ai_IsBuildingDestroyer && (!primaryVerb.verbProps.ai_IsIncendiary || thing.FlammableNow))
                    {
                        return new Job(JobDefOf.UseVerbOnThing)
                        {
                            targetA = thing,
                            verbToUse = primaryVerb,
                            expiryInterval = 100
                        };
                    }
                }
                return MeleeOrWaitJob(pawn, thing, cellBeforeBlocker);
            }
        }
        */

        private static int grenadeCountInInventory(Pawn pawn, CompInventory inventory)
        {
            return inventory.rangedWeaponList.Where(gr => pawn.CanReserve(gr, 1) && !gr.def.weaponTags.NullOrEmpty() && gr.def.weaponTags.Contains("CE_AI_Grenade")).Count();
        }

        private static bool isGrenade(Thing thing)
        {
            return !thing.def.thingCategories.NullOrEmpty() && thing.def.thingCategories.Contains(ThingCategoryDef.Named("Grenades"));
        }

        private static bool Unload(Pawn pawn)
        {
            var inv = pawn.TryGetComp<CompInventory>();
            if (inv != null
            && !pawn.Faction.IsPlayer
            && (pawn.CurJob != null && pawn.CurJob.def != JobDefOf.Steal)
            && ((inv.capacityWeight - inv.currentWeight < 3f)
            || (inv.capacityBulk - inv.currentBulk < 4f)))
            {
                return true;
            }
            else return false;
        }

        private static bool NoDangerInPosRadius(Pawn pawn, IntVec3 position, Map map, float distance, bool moreattentive, bool passDoors = false)
        {
            /*
            return Enumerable.Where<Pawn>(map.mapPawns.AllPawns, (p => p.Position.InHorDistOf(position, distance) 
            && (moreattentive = false || (moreattentive = true && GenSight.LineOfSight(position, p.Position, map, true)))
            && !p.RaceProps.Animal && !p.Downed && !p.Dead && FactionUtility.HostileTo(p.Faction, pawn.Faction))).Count() <= 0;
            */
            TraverseParms tp = (!passDoors) ? TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false) : TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false);
            bool foundEnemy = false;
            RegionTraverser.BreadthFirstTraverse(position, map, (Region from, Region to) => to.Allows(tp, false), delegate (Region r)
            {
                List<Thing> list = r.ListerThings.ThingsInGroup(ThingRequestGroup.AttackTarget);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].HostileTo(pawn))
                    {
                        foundEnemy = true;
                        return true;
                    }
                }
                return foundEnemy;
            }, (int)distance, RegionType.Set_Passable);
            return !foundEnemy;
        }

        private static int fixedsearchrange(Pawn pawn, Thing t, float dangerange = 30, int maxtotalrange = 25, int mintotalrange = 6)
        {
            if (NoDangerInPosRadius(pawn, t.Position, Find.CurrentMap, dangerange, false))
            {
                return maxtotalrange;
            }
            return mintotalrange;
        }

        private static Job MeleeOrWaitJob(Pawn pawn, Thing blocker, IntVec3 cellBeforeBlocker)
        {
            if (!pawn.CanReserve(blocker, 1))
            {
                return new Job(JobDefOf.Goto, CellFinder.RandomClosewalkCellNear(cellBeforeBlocker, pawn.Map, 10), 100, true);
            }
            return new Job(JobDefOf.AttackMelee, blocker)
            {
                ignoreDesignations = true,
                expiryInterval = 100,
                checkOverrideOnExpire = true
            };
        }

        /*
        private Apparel FindGarmentCoveringPart(Pawn pawn, BodyPartGroupDef bodyPartGroupDef)
        {
            Room room = pawn.GetRoom();
            Predicate<Thing> validator = (Thing t) => pawn.CanReserve(t, 1) 
            && pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly, true) 
            && (t.Position.DistanceToSquared(pawn.Position) < 12f || room == RegionAndRoomQuery.RoomAtFast(t.Position, t.Map));
            List<Thing> aList = (
                from t in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel)
                orderby t.MarketValue - t.Position.DistanceToSquared(pawn.Position) * 2f descending
                where validator(t)
                select t
                ).ToList();
            foreach (Thing current in aList)
            {
                Apparel ap = current as Apparel;
                if (ap != null && ap.def.apparel.bodyPartGroups.Contains(bodyPartGroupDef) && pawn.CanReserve(ap, 1) && ApparelUtility.HasPartsToWear(pawn, ap.def))
                {
                    return ap;
                }
            }
            return null;
        }
        */
    }
}