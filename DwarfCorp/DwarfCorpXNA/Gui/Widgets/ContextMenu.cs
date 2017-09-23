using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Gui.Widgets
{
    public class ContextMenu : Gui.Widget
    {
        public List<ContextCommands.ContextCommand> Commands;
        public Body Body;
        public WorldManager World;

        public override void Construct()
        {
            Rect = new Rectangle(0, 0, 0, 0);
            Transparent = true;

            foreach (var command in Commands)
            {
                var iconSheet = Root.GetTileSheet(command.Icon.Sheet);
                var lambdaCommand = command;
                AddChild(new Widget
                {
                    Rect = new Rectangle(0,0, iconSheet.TileWidth, iconSheet.TileHeight),
                    Background = command.Icon,
                    OnClick = (sender, args) =>
                    {
                        lambdaCommand.Apply(Body, World);
                        sender.Parent.Close();
                    }
                });
            }

            OnUpdate += (sender, time) =>
                {
                    if (Body.IsDead)
                    {
                        this.Close();
                        return;
                    }

                    var menuCenter = World.Camera.Project(Body.Position);

                    var degrees = (Math.PI * 2) / Commands.Count;
                    double pos = 0.0f;

                    for (var i = 0; i < Commands.Count; ++i)
                    {
                        var buttonOffset = Vector2.Transform(Vector2.UnitY,
                            Matrix.CreateRotationZ((float)pos));
                        var buttonCenter = new Vector2(menuCenter.X, menuCenter.Y) + (buttonOffset * 32);
                        var iconSheet = Root.GetTileSheet(Commands[i].Icon.Sheet);
                        var child = GetChild(i);
                        child.Rect = new Rectangle(
                                (int)(buttonCenter.X - (iconSheet.TileWidth / 2)),
                                (int)(buttonCenter.Y - (iconSheet.TileHeight / 2)),
                            iconSheet.TileWidth, iconSheet.TileHeight);
                        child.Invalidate();
                    }

                    Invalidate();
                };

            Root.RegisterForUpdate(this);
            base.Construct();
        }
    }
}
