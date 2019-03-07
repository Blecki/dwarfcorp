using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class CancelDwarfCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new CancelDwarfCommand(); }

        public CancelDwarfCommand()
        {
            Name = "Cancel Task";
            Description = "Click to force the selected dwarf(s) to cancel their current task.";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return false;
            return World.Master.Faction.Minions.Contains(creature) && creature.CurrentTask != null;
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return;

            creature.CancelCurrentTask();
        }
    }
}
