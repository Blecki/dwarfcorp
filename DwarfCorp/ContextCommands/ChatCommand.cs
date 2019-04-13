using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class ChatCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new ChatCommand(); }

        public ChatCommand()
        {
            Name = "Chat";
            Description = "Click to talk to the selected dwarf.";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return false;
            var thoughts = Entity.GetComponent<DwarfThoughts>();
            if (thoughts == null)
                return false;
            return World.Master.Faction.Minions.Contains(creature);
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            creature.Chat();
        }
    }
}
