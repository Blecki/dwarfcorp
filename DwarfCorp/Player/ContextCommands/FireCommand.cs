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

        public override bool CanBeAppliedTo(GameComponent Entity, WorldManager World)
        {
            if (Entity.GetComponent<CreatureAI>().HasValue(out var creature))
                return World.PlayerFaction.Minions.Contains(creature);
            else
                return false;
        }
        
        public override void Apply(GameComponent Entity, WorldManager World)
        {
            if (Entity.GetComponent<CreatureAI>().HasValue(out var creature))
            {
                World.PlayerFaction.Minions.Remove(creature);
                World.PersistentData.SelectedMinions.Remove(creature);
                Entity.GetRoot().Delete();
                SoundManager.PlaySound(ContentPaths.Audio.change, 0.5f);
            }
        }
    }
}
