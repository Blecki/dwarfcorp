using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class CancelToolOptions : Widget
    {
        public CheckBox Voxels;
        public CheckBox Entities;

        public override Point GetBestSize()
        {
            return new Point(200, 50);
        }


        public override void Construct()
        {
            Font = "font8";
            TextColor = new Vector4(0, 0, 0, 1);

            Voxels = AddChild(new CheckBox
            {
                Text = "Voxel Tasks",
                CheckState = true,
                AutoLayout = AutoLayout.DockTop,
                OnCheckStateChange = (sender) =>
                {
                    Entities.SilentSetCheckState(!Voxels.CheckState);
                    Entities.Invalidate();
                    Voxels.Invalidate();
                }
            }) as CheckBox;

            Entities = AddChild(new CheckBox
            {
                Text = "Entity Tasks",
                CheckState = false,
                AutoLayout = AutoLayout.DockTop,
                OnCheckStateChange = (sender) =>
                {
                    Voxels.SilentSetCheckState(!Entities.CheckState);
                    Entities.Invalidate();
                    Voxels.Invalidate();
                }
            }) as CheckBox;
        }
    }
}
