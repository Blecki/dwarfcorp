using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class DestroyCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new DestroyCommand(); }

        public DestroyCommand()
        {
            Name = "Destroy";
            Description = "Click to destroy the selected object(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(GameComponent Entity, WorldManager World)
        {
            return (World.UserInterface.Tools["DeconstructObject"] as DeconstructObjectTool).CanDestroy(Entity);
        }

        public override void Apply(GameComponent Entity, WorldManager World)
        {
            var task = new DestroyObjectTask(Entity);
            World.TaskManager.AddTask(task);
            World.UserInterface.ShowToolPopup("Will destroy this " + Entity.Name);
        }
    }
}
