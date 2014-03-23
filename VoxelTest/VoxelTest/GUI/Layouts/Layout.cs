using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// Abstract class which modifies the size and position of GUI components
    /// so that they flow to fit the size of a window.
    /// </summary>
    public class Layout : GUIComponent
    {
        public Layout(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
        }

        public virtual void UpdateSizes()
        {
        }

        public override void Update(GameTime time)
        {
            UpdateSizes();
            base.Update(time);
        }

        public override bool IsMouseOverRecursive()
        {
            return  Children.Any(child => child.IsMouseOverRecursive());
        }
    }

}