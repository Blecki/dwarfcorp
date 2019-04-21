using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class MoveCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new MoveCommand(); }

        public MoveCommand()
        {
            Name = "Move";
            Description = "Click to move the selected object";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(GameComponent Entity, WorldManager World)
        {
            return (World.Master.Tools[GameMaster.ToolMode.MoveObjects] as MoveObjectTool).CanMove(Entity);
        }

        public override void Apply(GameComponent Entity, WorldManager World)
        {
            World.Master.ChangeTool(GameMaster.ToolMode.MoveObjects);
        }
    }
}
