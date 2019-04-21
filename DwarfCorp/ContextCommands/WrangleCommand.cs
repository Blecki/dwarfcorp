using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class WrangleCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new WrangleCommand(); }

        public WrangleCommand()
        {
            Name = "Catch";
            Description = "Click to catch the selected creature(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(GameComponent Entity, WorldManager World)
        {
            return (World.Master.Tools[GameMaster.ToolMode.Wrangle] as WrangleTool).CanCatch(Entity);
        }

        public override void Apply(GameComponent Entity, WorldManager World)
        {
            var minions = Faction.FilterMinionsWithCapability(World.PlayerFaction.Minions, Task.TaskCategory.Wrangle);
            if (minions.Count > 0)
                World.Master.TaskManager.AddTask(new WrangleAnimalTask(Entity.GetRoot().GetComponent<Creature>()) { Priority = Task.PriorityType.Medium });
        }
    }
}