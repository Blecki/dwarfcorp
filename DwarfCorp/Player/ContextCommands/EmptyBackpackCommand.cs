using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class EmptyBackpackCommand  : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new EmptyBackpackCommand(); }

        public EmptyBackpackCommand()
        {
            Name = "Empty backpack";
            Description = "Click to force the selected dwarf(s) to empty their backpacks.";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(GameComponent Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return false;
            return World.PlayerFaction.Minions.Contains(creature) && creature.Creature.Inventory.Resources.Any();
        }

        public override void Apply(GameComponent Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return;

            creature.Creature.RestockAllImmediately(true);
        }
    }
}
