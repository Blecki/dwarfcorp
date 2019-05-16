using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class ViewAllowedTasksCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new ViewAllowedTasksCommand(); }

        public ViewAllowedTasksCommand()
        {
            Name = "Allowed tasks...";
            Description = "Click to view the selected dwarfs allowed tasks.";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override void Apply(GameComponent Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return;

            var screen = World.UserInterface.Gui.RenderData.VirtualScreen;
            World.UserInterface.Gui.ShowMinorPopup(new AllowedTaskFilter
            {
                Employee = creature,
                Tag = "selected-employee-allowable-tasks",
                AutoLayout = AutoLayout.DockFill,
                MinimumSize = new Point(256, 256),
                Border = "border-fancy",
                Rect = new Rectangle(screen.Center.X - 128, screen.Center.Y - 128, 256, 256)
            });

            base.Apply(Entity, World);
        }

        public override bool CanBeAppliedTo(GameComponent Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return false;
            return World.PlayerFaction.Minions.Contains(creature);
        }
    }
}