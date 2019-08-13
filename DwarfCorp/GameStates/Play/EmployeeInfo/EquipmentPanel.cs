#define ENABLE_CHAT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.Play.EmployeeInfo
{
    public class EquipmentPanel : Widget
    {
        public Func<CreatureAI> FetchEmployee = null;
        public CreatureAI Employee
        {
            get { return FetchEmployee?.Invoke(); }
        }

        public override void Construct()
        {
            Font = "font8";



            base.Construct();
        }

        protected override Gui.Mesh Redraw()
        {
            // Set values from CreatureAI
            if (Employee != null && !Employee.IsDead)
            {
                Hidden = false;
                Text = "";
                var equipment = Employee.Stats.Equipment;
                if (equipment != null)
                {
                    foreach (var item in equipment.EquippedItems)
                    {
                        Text += item.Key + ": " + item.Value.Resource + "\n";
                    }
                }
            }
            else
                Hidden = true;

            foreach (var child in Children)
                child.Invalidate();

            return base.Redraw();
        }
    }
}
