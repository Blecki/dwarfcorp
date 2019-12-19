using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;

namespace DwarfCorp.Play.EmployeeInfo
{
    public class PackPanel : Widget
    {
        public Func<CreatureAI> FetchEmployee = null;
        public CreatureAI Employee
        {
            get { return FetchEmployee?.Invoke(); }
        }

        ContentsPanel ContentsPanel = null;

        public override void Construct()
        {
            Font = "font10";

            ContentsPanel = AddChild(new ContentsPanel
            {
                AutoLayout = AutoLayout.DockFill,
            }) as ContentsPanel;

            base.Construct();
        }

        protected override Gui.Mesh Redraw()
        {
            // Set values from CreatureAI
            if (Employee != null && !Employee.IsDead)
            {
                if (Employee.GetRoot().GetComponent<Inventory>().HasValue(out var inventory))
                {
                    ContentsPanel.Hidden = false;
                    ContentsPanel.Resources = inventory.ContentsAsResourceSet();
                    ContentsPanel.Invalidate();
                    Text = "";
                }
                else
                {
                    ContentsPanel.Hidden = true;
                    ContentsPanel.Resources = null;
                    Text = "This creature has no inventory.";
                }
            }
            else
            {
                ContentsPanel.Hidden = true;
                ContentsPanel.Resources = null;
                Text = "No employee selected.";
            }

            return base.Redraw();
        }
    }
}
