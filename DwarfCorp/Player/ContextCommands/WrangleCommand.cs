namespace DwarfCorp.ContextCommands
{
    public class WrangleCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new WrangleCommand(); }

        public WrangleCommand()
        {
            Name = "Catch";
            Description = "Click to catch the selected creature(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(GameComponent Entity, WorldManager World)
        {
            return WrangleTool.CanCatch(World, Entity, false);
        }

        public override void Apply(GameComponent Entity, WorldManager World)
        {
            if (Entity.GetRoot().GetComponent<Creature>().HasValue(out var creature))
                if (Faction.FilterMinionsWithCapability(World.PlayerFaction.Minions, TaskCategory.Wrangle).Count > 0)
                    World.TaskManager.AddTask(new WrangleAnimalTask(creature) { Priority = TaskPriority.Medium });
        }
    }
}