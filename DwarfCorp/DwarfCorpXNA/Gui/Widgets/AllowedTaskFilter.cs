using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class AllowedTaskFilter : Widget
    {
        private CreatureAI _employee;
        public CreatureAI Employee
        {
            get { return _employee; }
            set { _employee = value; Invalidate(); }
        }

        public int ColumnCount = 2;
        private Widget InteriorPanel;
        private Widget EmployeeName;

        private List<CheckBox> TaskCategories = new List<CheckBox>();
        
        public override void Construct()
        {
            Text = "Choose a single employee to assign their allowed tasks.";
            Font = "font16";
                       
            InteriorPanel = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Hidden = true,
                Background = new TileReference("basic", 0),
                Font = "font8",
            });

            EmployeeName = InteriorPanel.AddChild(new Gui.Widget
            {
                Text = String.Format("Allowed Tasks"),
                TextHorizontalAlign = HorizontalAlign.Center,
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 36),
                Font = "font16"
            });
            
            var columns = InteriorPanel.AddChild(new Gui.Widgets.Columns
            {
                AutoLayout = AutoLayout.DockFill,
                MinimumSize = new Point(0, 120),
                ColumnCount = ColumnCount
            });

            for (var i = 0; i < ColumnCount; ++i)
                columns.AddChild(new Widget
                {
                    Padding = new Margin(2, 2, 2, 2),
                });

            var column = 0;
            
            foreach (var type in Enum.GetValues(typeof(Task.TaskCategory)))
            {
                if (type.ToString().StartsWith("_")) continue;

                var box = columns.GetChild(column).AddChild(new CheckBox
                {
                    Text = type.ToString(),
                    Tag = type,
                    AutoLayout = AutoLayout.DockTop,
                }) as CheckBox;

                TaskCategories.Add(box);
                box.OnCheckStateChange += (sender) => CheckChanged();

                column = (column + 1) % ColumnCount;
            }

            base.Construct();
        }

        private void CheckChanged()
        {
            var visibleTypes = Task.TaskCategory.None;
            foreach (var box in TaskCategories)
                if (box.CheckState) visibleTypes |= (Task.TaskCategory)box.Tag;
            Employee.Stats.AllowedTasks = visibleTypes;
        }

        protected override Mesh Redraw()
        {
            if (Employee == null)
            {
                InteriorPanel.Hidden = true;
            }
            else
            {
                InteriorPanel.Hidden = false;
                EmployeeName.Text = String.Format("{0} Allowed Tasks", Employee.Stats.FullName);
                foreach (var checkbox in TaskCategories)
                {
                    var type = (Task.TaskCategory)checkbox.Tag;
                    checkbox.SilentSetCheckState(Employee.Stats.IsTaskAllowed(type));
                }
            }

            foreach (var child in Children)
                child.Invalidate();
            return base.Redraw();
        }
    }
}
