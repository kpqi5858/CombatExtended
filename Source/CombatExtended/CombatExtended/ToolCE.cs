using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class ToolCE : Tool
    {
        public new string id = ""; //Susspress id error
        public new float armorPenetration = 0; //Armor Penetration standard value is -1f.
        public Gender restrictedGender = Gender.None;
    }
}
