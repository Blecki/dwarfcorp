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

        public override void Apply(Body Entity, WorldManager World)
        {
            var minions = Faction.FilterMinionsWithCapability(World.PlayerFaction.Minions,
                GameMaster.ToolMode.Chop);
            if (minions.Count > 0)
            {
                var task = ChopTool.ChopTree(Entity, World.PlayerFaction);
                if (task != null)
                {
                    var tasks = new List<Task>();
                    tasks.Add(task);
                    World.Master.TaskManager.AddTasks(tasks);
                    //TaskManager.AssignTasks(tasks, minions);
                }
            }
        }
    }
}
