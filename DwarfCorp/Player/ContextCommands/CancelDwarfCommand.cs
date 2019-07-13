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

        public override bool CanBeAppliedTo(GameComponent Entity, WorldManager World)
        {
            if (Entity.GetComponent<CreatureAI>().HasValue(out var creature))
                return World.PlayerFaction.Minions.Contains(creature) && creature.CurrentTask != null;
            else
                return false;
        }

        public override void Apply(GameComponent Entity, WorldManager World)
        {
            if (Entity.GetComponent<CreatureAI>().HasValue(out var creature))
                creature.CancelCurrentTask();
        }
    }
}
