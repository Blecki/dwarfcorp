using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    public class Layout : SillyGUIComponent
    {
        public Layout(SillyGUI gui, SillyGUIComponent parent) :
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
    }

}