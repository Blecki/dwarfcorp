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
    public class DebugPanel : Widget
    {
        public Func<CreatureAI> FetchEmployee = null;
        public CreatureAI Employee => FetchEmployee?.Invoke();

        private Widget ActLabel;
        private TaskListPanel TaskList;

        public override void Construct()
        {
            Font = "font8";
            Padding = new Margin(8, 8, 4, 4);

            AddChild(new Button
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 24),
                Text = "Break Into Update",
                ChangeColorOnHover = true,
                OnClick = (sender, args) =>
                {
                    if (Employee is DwarfAI dwarf)
                        dwarf.BreakOnUpdate = true;
                }
            });

            ActLabel = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 24)
            });

            TaskList = AddChild(new TaskListPanel
            {
                AutoLayout = AutoLayout.DockFill,
            }) as TaskListPanel;

            base.Construct();
        }

        protected override Gui.Mesh Redraw()
        {
            // Set values from CreatureAI
            if (Employee != null && !Employee.IsDead)
            {
                Hidden = false;
                ActLabel.Text = Employee.GetCurrentActString();
                ActLabel.Invalidate();

                TaskList.UpdateList(Employee);
            }
            else
                Hidden = true;

            foreach (var child in Children)
                child.Invalidate();

            return base.Redraw();
        }
    }
}
