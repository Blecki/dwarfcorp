using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;

namespace DwarfCorp.Play
{
    public class EmployeeBackpackDialog : Widget
    {
        public CreatureAI Employee;

        public override void Construct()
        {
            Rect = Root.RenderData.VirtualScreen;
            Rect.Inflate(-20, -20);
            Border = "border-fancy";
            Font = "font10";

            var backButton = AddChild(new Button()
            {
                Text = "Back",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnLayout = (sender1) => sender1.Rect = new Rectangle(sender1.Rect.X - 16, sender1.Rect.Y - 16, 64, 32),
                OnClick = (sender1, args1) => { this.Close(); },
                MinimumSize = new Point(64, 32)
            });

            var emptyButton = AddChild(new Button()
            {
                Text = "Empty Backpack",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnLayout = (sender1) => sender1.Rect = new Rectangle(backButton.Rect.X - 128 - 8, sender1.Rect.Y - 16, 128, 32),
                OnClick = (sender1, args1) => 
                {
                    if (Employee.Creature != null)
                        Employee.Creature.AssignRestockAllTasks(TaskPriority.Urgent, true);
                },
                MinimumSize = new Point(128, 32)
            });

            if (Employee.GetRoot().GetComponent<Inventory>().HasValue(out var inventory))
                AddChild(new Play.StockpileContentsPanel
                {
                    AutoLayout = AutoLayout.DockFill,
                    Resources = inventory.ContentsAsResourceContainer()
                });
            else
            {
                Text = "This creature has no inventory.";
            }

            this.Layout();

            backButton.BringToFront();
            emptyButton.BringToFront();

            base.Construct();
        }
    }
}
