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
    public class ContextMenu : Widget
    {
        public List<ContextCommands.ContextCommand> Commands;
        public GameComponent Body;
        public List<GameComponent> MultiBody;

        public WorldManager World;
        public int Width;
        public override void Construct()
        {
            var text = Body != null ? DwarfSelectorTool.GetMouseOverText(new List<GameComponent>() { Body }) : "Selected Objects";
            var font = Root.GetTileSheet("font10");
            var size = font.MeasureString(text).Scale(TextSize);
            Width = Math.Max(size.X + 32, 128);
            Rect = new Rectangle(0, 0, Width, Commands.Count * 16 + 32);
            MaximumSize = new Point(Width, Commands.Count * 16 + 32);
            MaximumSize = new Point(Width, Commands.Count * 16 + 32);
            Border = "border-dark";
            TextColor = Color.White.ToVector4();
            Root.RegisterForUpdate(this);
            base.Construct();

            AddChild(new Gui.Widget()
            {
                Font = "font10",
                MinimumSize = new Point(128, 16),
                AutoLayout = AutoLayout.DockTop,
                Text = text
            });

            foreach (var command in Commands)
            {
                var iconSheet = Root.GetTileSheet(command.Icon.Sheet);
                var lambdaCommand = command;
                AddChild(new Gui.Widget()
                {
                    AutoLayout = AutoLayout.DockTop,
                    MinimumSize = new Point(Width, 16),
                    Text = command.Name,
                    Tooltip = command.Description,
                    OnClick = (sender, args) =>
                    {
                        if (MultiBody != null && MultiBody.Count > 0)
                        {
                            foreach (var body in MultiBody.Where(body => !body.IsDead && lambdaCommand.CanBeAppliedTo(body, body.World)))
                            {
                                lambdaCommand.Apply(body, World);
                            }
                        }
                        else
                        {
                            lambdaCommand.Apply(Body, World);
                        }
                        sender.Parent.Close();
                    },
                    ChangeColorOnHover = true,
                    HoverTextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4()
                });
            }

            OnUpdate += (sender, time) =>
            {
                if (Body == null)
                {
                    return;
                }

                if (Body.IsDead)
                {
                    this.Close();
                    return;
                }

                var menuCenter = World.Renderer.Camera.Project(Body.Position) / Root.RenderData.ScaleRatio;
                Rect = new Rectangle((int)menuCenter.X, (int)menuCenter.Y, Width, Commands.Count * 16 + 32);
                Layout();
                Invalidate();
            };

        }
    }

    public class HorizontalContextMenu : Widget
    {
        public List<ContextCommands.ContextCommand> Commands;
        public GameComponent Body;
        public List<GameComponent> MultiBody;
        public Action ClickAction;

        public WorldManager World;
        public int Height;
        public override void Construct()
        {
            Height = 28;
            Rect = new Rectangle(0, 0, Commands.Count * 64 + 32, Height);
            MaximumSize = new Point(Commands.Count * 64 + 32, Height);
            MaximumSize = new Point(Commands.Count * 64 + 32, Height);
            TextColor = Color.White.ToVector4();
            Padding = new Margin(2, 2, 8, 8);
            Root.RegisterForUpdate(this);
            base.Construct();

            foreach (var command in Commands)
            {
                var iconSheet = Root.GetTileSheet(command.Icon.Sheet);
                var lambdaCommand = command;
                AddChild(new Gui.Widget()
                {
                    AutoLayout = AutoLayout.DockLeft,
                    Font = "font10",
                    TextVerticalAlign = VerticalAlign.Center,
                    TextHorizontalAlign = HorizontalAlign.Center,
                    Background = new TileReference("basic", 0),
                    BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 0.75f),
                    MinimumSize = new Point(Height, 16),
                    InteriorMargin = new Margin(2, 2, 2, 2),
                    Text = command.Name,
                    Tooltip = command.Description,
                    OnClick = (sender, args) =>
                    {
                        if (MultiBody != null && MultiBody.Count > 0)
                        {
                            foreach (var body in MultiBody.Where(body => !body.IsDead && lambdaCommand.CanBeAppliedTo(body, body.World)))
                            {
                                lambdaCommand.Apply(body, World);
                            }
                        }
                        else
                        {
                            lambdaCommand.Apply(Body, World);
                        }
                        if (ClickAction != null)
                        {
                            ClickAction.Invoke();
                        }
                        sender.Parent.Close();
                    },
                    ChangeColorOnHover = true,
                    HoverTextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4()
                });
            }

            OnUpdate += (sender, time) =>
            {
                if (Body == null)
                {
                    return;
                }

                if (Body.IsDead)
                {
                    this.Close();
                    return;
                }

                var menuCenter = World.Renderer.Camera.Project(Body.Position) / Root.RenderData.ScaleRatio;
                Rect = new Rectangle((int)menuCenter.X, (int)menuCenter.Y, Commands.Count * 16 + 32, Height);
                Layout();
                Invalidate();
            };

        }
    }
}
