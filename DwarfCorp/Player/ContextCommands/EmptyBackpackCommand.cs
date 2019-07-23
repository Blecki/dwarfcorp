using System.Linq;

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
            if (Entity.GetComponent<CreatureAI>().HasValue(out var creature))
                return World.PlayerFaction.Minions.Contains(creature) && creature.Creature.Inventory.Resources.Any();
            else
                return false;
        }

        public override void Apply(GameComponent Entity, WorldManager World)
        {
            if (Entity.GetComponent<CreatureAI>().HasValue(out var creature))
                creature.Creature.AssignRestockAllTasks(TaskPriority.Urgent, true);
        }
    }
}
