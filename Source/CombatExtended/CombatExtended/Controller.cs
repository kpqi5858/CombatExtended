using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using CombatExtended.Harmony;

namespace CombatExtended
{
    public class Controller : Mod
    {
        public static Settings settings;

        public Controller(ModContentPack content) : base(content)
        {
            settings = GetSettings<Settings>();

            Exception e = null; //Ignore some exceptions..
            try 
            {
                // Apply Harmony patches
                HarmonyBase.InitPatches();
            } catch (Exception ex) { e = ex; }

            // Initialize loadout generator
            LongEventHandler.QueueLongEvent(LoadoutPropertiesExtension.Reset, "Other def binding, resetting and global operations.", false, null);

            // Inject ammo
            LongEventHandler.QueueLongEvent(AmmoInjector.Inject, "LibraryStartup", false, null);
			
            // Inject pawn and plant bounds
            LongEventHandler.QueueLongEvent(BoundsInjector.Inject, "CE_LongEvent_BoundingBoxes", false, null);

            LongEventHandler.QueueLongEvent(ShowWarningMessage, "Show Unofficial CE warning", false, null);

            if (e != null) throw e;
            Log.Message("Combat Extended :: initialized");
        }
		
        private void ShowWarningMessage()
        {
            if (!settings.HasShowedWarningMessage)
            {
                settings.HasShowedWarningMessage = true;
                Find.WindowStack.Add(new Dialog_MessageBox("You are using unofficial fork of Combat Extended.\n\nIt may not save-compatible with future releases of original CE mod."));
                WriteSettings();
            }
        }

        public override string SettingsCategory()
        {
            return "Combat Extended";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoWindowContents(inRect);
        }
    }
}
