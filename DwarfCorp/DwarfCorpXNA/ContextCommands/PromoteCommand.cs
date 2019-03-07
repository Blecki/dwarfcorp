using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class PromoteCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new PromoteCommand(); }

        public PromoteCommand()
        {
            Name = "Promote";
            Description = "Click to promote the selected dwarf(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return false;
            return World.Master.Faction.Minions.Contains(creature) && creature.Stats.IsOverQualified;
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            var Employee = Entity.GetComponent<CreatureAI>();
            var prevLevel = Employee.Stats.CurrentLevel;
            Employee.Stats.LevelUp();
            if (Employee.Stats.CurrentLevel.HealingPower > prevLevel.HealingPower)
            {
                World.MakeAnnouncement(String.Format("{0}'s healing power increased to {1}!", Employee.Stats.FullName, Employee.Stats.CurrentLevel.HealingPower));
            }

            if (Employee.Stats.CurrentLevel.ExtraAttacks.Count > prevLevel.ExtraAttacks.Count)
            {
                World.MakeAnnouncement(String.Format("{0} learned to cast {1}!", Employee.Stats.FullName, Employee.Stats.CurrentLevel.ExtraAttacks.Last().Name));
            }
            SoundManager.PlaySound(ContentPaths.Audio.change, 0.5f);
            Employee.Creature.AddThought(Thought.ThoughtType.GotPromoted);
        }
    }
}