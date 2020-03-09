using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class GatherCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new GatherCommand(); }

        public GatherCommand()
        {
            Name = "Gather";
            Description = "Click to gather the selected object(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(GameComponent Entity, WorldManager World)
        {
            return GatherTool.CanGather(World, Entity);
        }

        public override void Apply(GameComponent Entity, WorldManager World)
        {
            World.TaskManager.AddTask(new GatherItemTask(Entity));
        }
    }
}
