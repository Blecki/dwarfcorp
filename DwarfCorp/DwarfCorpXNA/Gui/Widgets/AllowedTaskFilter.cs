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
        public Dwarf Employee;
        public int ColumnCount = 2;

        private List<CheckBox> TaskCategories = new List<CheckBox>();
        
        public override void Construct()
        {
            Padding = new Margin(2, 2, 2, 2);

            AddChild(new Gui.Widget
            {
                Text = String.Format("{0} Allowed Tasks", Employee.Stats.FullName),
                TextHorizontalAlign = HorizontalAlign.Center,
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 36),
                Font = "font16"
            });
            
            var columns = AddChild(new Gui.Widgets.Columns
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
                //box.CheckState = (Employee.Creature.VisibleTypes & (DesignationType)type) == (DesignationType)type;
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
            //DesignationDrawer.VisibleTypes = visibleTypes;
        }
    }
}
