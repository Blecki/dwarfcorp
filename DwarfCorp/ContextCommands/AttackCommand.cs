using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class AttackCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new AttackCommand(); }

        public AttackCommand()
        {
            Name = "Attack";
            Description = "Click to attack the selected creature(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(GameComponent Entity, WorldManager World)
        {
            return (World.UserInterface.Tools["Attack"] as AttackTool).CanAttack(Entity);
        }

        public override void Apply(GameComponent Entity, WorldManager World)
        {
            var minions = Faction.FilterMinionsWithCapability(World.PlayerFaction.Minions, Task.TaskCategory.Attack);
            if (minions.Count > 0)
                World.Master.TaskManager.AddTask(new KillEntityTask(Entity, KillEntityTask.KillType.Attack));
        }
    }
}