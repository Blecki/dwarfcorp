using System;
using System.Linq;

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

        public override bool CanBeAppliedTo(GameComponent Entity, WorldManager World)
        {
            if (Entity.GetComponent<CreatureAI>().HasValue(out var creature))
                return World.PlayerFaction.Minions.Contains(creature) && creature.Stats.IsOverQualified;
            else
                return false;
        }

        public override void Apply(GameComponent Entity, WorldManager World) // Todo: This logic is duplicated
        {
            ImplementPromotion(Entity, World);
        }

        public static void ImplementPromotion(GameComponent Entity, WorldManager World) // Todo: This logic is duplicated
        {
            if (Entity.GetComponent<CreatureAI>().HasValue(out var employee))
            {
                var prevLevel = employee.Stats.CurrentLevel;
                employee.Stats.LevelUp(employee.Creature);
                if (employee.Stats.Title == prevLevel.Name)
                    employee.Stats.Title = employee.Stats.CurrentLevel.Name;

                if (employee.Stats.CurrentLevel.HealingPower > prevLevel.HealingPower)
                    World.MakeAnnouncement(String.Format("{0}'s healing power increased to {1}!", employee.Stats.FullName, employee.Stats.CurrentLevel.HealingPower));

                if (employee.Stats.CurrentLevel.ExtraWeapons.Count > prevLevel.ExtraWeapons.Count)
                    World.MakeAnnouncement(String.Format("{0} learned to cast {1}!", employee.Stats.FullName, employee.Stats.CurrentLevel.ExtraWeapons.Last().Name));

                SoundManager.PlaySound(ContentPaths.Audio.change, 0.5f);
                employee.Creature.AddThought("I got promoted recently.", new TimeSpan(3, 0, 0, 0), 20.0f);
            }
        }
    }
}