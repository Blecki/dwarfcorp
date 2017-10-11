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
        public DesignationDrawer DesignationDrawer;

        private List<CheckBox> Designations = new List<CheckBox>();
        
        public override void Construct()
        {
            AddChild(new Gui.Widget
            {
                Text = "Visible Markers",
                TextHorizontalAlign = HorizontalAlign.Center,
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 32),
                Font = "font16"
            });
            
            var columns = AddChild(new Gui.Widgets.TwoColumns
            {
                AutoLayout = AutoLayout.DockFill,
                MinimumSize = new Point(0, 120)
            });
            
            var left = columns.AddChild(new Gui.Widget());
            var right = columns.AddChild(new Gui.Widget());
            var sideToggle = 0;
            
            foreach (var type in Enum.GetValues(typeof(DesignationType)))
            {
                if (type.ToString().StartsWith("_")) continue;

                var box = columns.GetChild(sideToggle).AddChild(new CheckBox
                {
                    Text = type.ToString(),
                    Tag = type,
                    AutoLayout = AutoLayout.DockTop,
                }) as CheckBox;

                Designations.Add(box);
                box.CheckState = (DesignationDrawer.VisibleTypes & (DesignationType)type) == (DesignationType)type;
                box.OnCheckStateChange += (sender) => CheckChanged();

                sideToggle = (sideToggle + 1) % 2;
            }

            base.Construct();
        }

        private void CheckChanged()
        {
            var visibleTypes = DesignationType._None;
            foreach (var box in Designations)
                if (box.CheckState) visibleTypes |= (DesignationType)box.Tag;
            DesignationDrawer.VisibleTypes = visibleTypes;
        }
    }
}
