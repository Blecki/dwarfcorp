using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class CancelCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new CancelCommand(); }

        public CancelCommand()
        {
            Name = "Cancel";
            Description = "Click to cancel the selected command(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            return World.Master.Faction.Designations.EnumerateEntityDesignations(Entity).Any();
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            foreach (var des in World.PlayerFaction.Designations.EnumerateEntityDesignations(Entity).ToList())
                if (des.Task != null)
                    World.Master.TaskManager.CancelTask(des.Task);
        }
    }
}
