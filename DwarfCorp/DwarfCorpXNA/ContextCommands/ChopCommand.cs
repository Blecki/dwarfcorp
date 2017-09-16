using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class ChopCommand : ContextCommand
    {
        public ChopCommand()
        {
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            return Entity.Tags.Contains("Vegetation");
        }
    }
}
