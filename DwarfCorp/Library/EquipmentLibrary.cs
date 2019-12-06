using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public partial class Library
    {
        public static List<EquipmentSlotType> EquipmentSlotTypes;
        private static bool EquipmentSlotsInitialized = false;

        private static void InitializeEquipmentSlotTypes()
        {
            if (EquipmentSlotsInitialized) return;
            EquipmentSlotsInitialized = true;

            EquipmentSlotTypes = FileUtils.LoadJsonListFromMultipleSources<EquipmentSlotType>("Entities/Dwarf/ToolIcons/slot-types.json", null, p => p.Name);
        }

        public static IEnumerable<EquipmentSlotType> EnumerateEquipmentSlotTypes()
        {
            InitializeEquipmentSlotTypes();
            return EquipmentSlotTypes;
        }

        public static MaybeNull<EquipmentSlotType> FindEquipmentSlotType(String Name)
        {
            InitializeEquipmentSlotTypes();
            return EquipmentSlotTypes.FirstOrDefault(l => l.Name == Name);
        }
    }
}
