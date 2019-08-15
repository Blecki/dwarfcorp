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

        StockpileContentsPanel ContentsPanel = null;

        public override void Construct()
        {
            Font = "font10";

            var emptyButton = AddChild(new Button()
            {
                Text = "Empty Backpack",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnLayout = (sender1) => sender1.Rect = new Rectangle(sender1.Rect.X - 16, sender1.Rect.Y - 16, 128, 32),
                OnClick = (sender1, args1) =>
                {
                    if (Employee.Creature != null)
                        Employee.Creature.AssignRestockAllTasks(TaskPriority.Urgent, true);
                },
                MinimumSize = new Point(128, 32)
            });

            ContentsPanel = AddChild(new StockpileContentsPanel
            {
                AutoLayout = AutoLayout.DockFill,
            }) as StockpileContentsPanel;

            this.Layout();

            emptyButton.BringToFront();

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
                    ContentsPanel.Resources = inventory.ContentsAsResourceContainer();
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
