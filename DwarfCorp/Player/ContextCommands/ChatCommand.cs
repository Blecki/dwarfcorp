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

        public override bool CanBeAppliedTo(GameComponent Entity, WorldManager World)
        {
            if (Entity.GetComponent<CreatureAI>().HasValue(out var creature) && Entity.GetComponent<DwarfThoughts>().HasValue(out var thoughts))
                return World.PlayerFaction.Minions.Contains(creature);
            else
                return false;
        }

        public override void Apply(GameComponent Entity, WorldManager World)
        {
            if (Entity.GetComponent<CreatureAI>().HasValue(out var creature))
                creature.Chat();
        }
    }
}
