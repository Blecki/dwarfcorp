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
    public class EquipmentSlotType
    {
        public String Name;
        public Point GuiOffset = new Point(0, 0);
        public Gui.TileReference UnselectedBackground = new Gui.TileReference("equipment_sheet", 1);
        public Gui.TileReference SelectedBackground = new Gui.TileReference("equipment_sheet", 2);
    }
}