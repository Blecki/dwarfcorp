using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Play.EmployeeInfo
{ 
    public class AllowedTaskFilter : Widget
    {
        public Func<CreatureAI> FetchEmployee = null;
        public CreatureAI Employee
        {
            get { return FetchEmployee?.Invoke(); }
        }

        private int ColumnCount = 3;
        private List<CheckBox> TaskCategories = new List<CheckBox>();
        
        public override void Construct()
        {
            var columns = AddChild(new Columns
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
            
            foreach (var type in Enum.GetValues(typeof(TaskCategory)))
            {
                if (type.ToString().StartsWith("_")) continue;
                if ((TaskCategory)type == TaskCategory.None) continue;

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
            var visibleTypes = TaskCategory.None;
            foreach (var box in TaskCategories)
                if (box.CheckState) visibleTypes |= (TaskCategory)box.Tag;
            Employee.Stats.AllowedTasks = visibleTypes;
        }

        protected override Mesh Redraw()
        {
            foreach (var checkbox in TaskCategories)
            {
                if (Employee == null)
                {
                    checkbox.SilentSetCheckState(false);
                    checkbox.Enabled = false;
                }
                else
                {
                    checkbox.SilentSetCheckState(Employee.Stats.IsTaskAllowed((TaskCategory)checkbox.Tag));
                    checkbox.Enabled = Employee.Stats.IsManager ? ((TaskCategory)checkbox.Tag & (TaskCategory.Attack | TaskCategory.Guard | TaskCategory.Gather | TaskCategory.BuildObject)) == (TaskCategory)checkbox.Tag : true;
                }
            }

            foreach (var child in Children)
                child.Invalidate();

            return base.Redraw();
        }
    }
}
