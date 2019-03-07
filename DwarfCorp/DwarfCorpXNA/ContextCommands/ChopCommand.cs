using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class ChopCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new ChopCommand(); }

        public ChopCommand()
        {
            Name = "Harvest";
            Description = "Click to harvest the selected plant(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            return Entity.Tags.Contains("Vegetation");
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            var minions = Faction.FilterMinionsWithCapability(World.PlayerFaction.Minions, Task.TaskCategory.Chop);
            if (minions.Count > 0)
                World.Master.TaskManager.AddTask(new ChopEntityTask(Entity));
        }
    }
}
