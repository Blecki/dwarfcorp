using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class FireCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new FireCommand(); }

        public FireCommand()
        {
            Name = "Fire";
            Description = "Click to fire the selected dwarf(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return false;
            return World.Master.Faction.Minions.Contains(creature);
        }
        
        public override void Apply(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            World.PlayerFaction.Minions.Remove(creature);
            World.PlayerFaction.SelectedMinions.Remove(creature);
            Entity.GetRoot().Delete();
            SoundManager.PlaySound(ContentPaths.Audio.change, 0.5f);
        }
    }
}
