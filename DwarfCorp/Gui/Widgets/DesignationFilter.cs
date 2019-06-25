using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class DesignationFilter : Widget
    {
        public DesignationSet DesignationSet;
        public int ColumnCount = 2;
        public WorldManager World;

        private List<CheckBox> Designations = new List<CheckBox>();
        
        public override void Construct()
        {
            Padding = new Margin(2, 2, 2, 2);

            AddChild(new Gui.Widget
            {
                Text = "Visible Markers",
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
            
            foreach (var type in Enum.GetValues(typeof(DesignationType)))
            {
                if (type.ToString().StartsWith("_")) continue;

                var box = columns.GetChild(column).AddChild(new CheckBox
                {
                    Text = type.ToString(),
                    Tag = type,
                    AutoLayout = AutoLayout.DockTop,
                }) as CheckBox;

                Designations.Add(box);
                box.CheckState = (World.Renderer.PersistentSettings.VisibleTypes & (DesignationType)type) == (DesignationType)type;
                box.OnCheckStateChange += (sender) => CheckChanged();

                column = (column + 1) % ColumnCount;
            }

            base.Construct();
        }

        private void CheckChanged()
        {
            var visibleTypes = DesignationType._None;
            foreach (var box in Designations)
                if (box.CheckState) visibleTypes |= (DesignationType)box.Tag;
            World.Renderer.PersistentSettings.VisibleTypes = visibleTypes;
            foreach (var designation in DesignationSet.EnumerateDesignations())
                designation.Voxel.Invalidate();
        }
    }
}
