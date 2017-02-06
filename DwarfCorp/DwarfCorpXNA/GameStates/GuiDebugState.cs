using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum;

namespace DwarfCorp.GameStates
{
    public class GuiDebugState : GameState
    {
        private Gum.Root GuiRoot;

        public class ShowTextureDialog : Gum.Widget
        {
            private Rectangle ImagePosition;

            public override void Construct()
            {
                AutoLayout = AutoLayout.DockFill;
                Background = new TileReference("basic", 1);
                BackgroundColor = new Vector4(1, 1, 1, 1);

                var imageSize = new Point(Root.RenderData.Texture.Width, Root.RenderData.Texture.Height);
                var availableArea = new Point(Root.VirtualScreen.Width, Root.VirtualScreen.Height - 32);

                if (imageSize.X > availableArea.X)
                {
                    var ratio = (float)availableArea.X / (float)imageSize.X;
                    imageSize.X = availableArea.X;
                    imageSize.Y = (int)(imageSize.Y * ratio);
                }

                if (imageSize.Y > availableArea.Y)
                {
                    var ratio = (float)availableArea.Y / (float)imageSize.Y;
                    imageSize.Y = availableArea.Y;
                    imageSize.X = (int)(imageSize.X * ratio);
                }

                ImagePosition = new Rectangle((Root.VirtualScreen.Width / 2) - (imageSize.X / 2),
                    ((Root.VirtualScreen.Height - 32) / 2) - (imageSize.Y / 2),
                    imageSize.X, imageSize.Y);

                var bottomBar = AddChild(new Widget
                    {
                        MinimumSize = new Point(0,32),
                        AutoLayout = Gum.AutoLayout.DockBottom,
                        Padding = new Margin(2,2,2,2)
                    });

                bottomBar.AddChild(new Widget
                {
                    Text = "CLOSE",
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Center,
                    Border = "border-thin",
                    Font = "outline-font",
                    OnClick = (sender, args) => this.Close(),
                    AutoLayout = AutoLayout.DockRight
                });

                bottomBar.AddChild(new Widget
                    {
                        Border = "border-thin",
                        Font = "outline-font",
                        TextHorizontalAlign = HorizontalAlign.Center,
                        TextVerticalAlign = VerticalAlign.Center,
                        TextColor = new Vector4(1, 1, 1, 1),
                        Text = "WHITE",
                        AutoLayout = Gum.AutoLayout.DockLeft,
                        OnClick = (sender, args) =>
                            {
                                this.BackgroundColor = new Vector4(1, 1, 1, 1);
                                this.Invalidate();
                            }
                    });

                bottomBar.AddChild(new Widget
                {
                    Border = "border-thin",
                    Font = "outline-font",
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Center,
                    TextColor = new Vector4(0, 0, 0, 1),
                    Text = "BLACK",
                    AutoLayout = Gum.AutoLayout.DockLeft,
                    OnClick = (sender, args) =>
                    {
                        this.BackgroundColor = new Vector4(0, 0, 0, 1);
                        this.Invalidate();
                    }
                });

                bottomBar.AddChild(new Widget
                {
                    Font = "outline-font",
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Center,
                    TextColor = new Vector4(1, 1, 1, 1),
                    Text = String.Format("{0} x {1}", Root.RenderData.Texture.Width, Root.RenderData.Texture.Height),
                    AutoLayout = Gum.AutoLayout.DockLeft,
                });
            }

            protected override Gum.Mesh Redraw()
            {
                var mesh = Gum.Mesh.Quad()
                    .Scale(ImagePosition.Width, ImagePosition.Height)
                    .Translate(ImagePosition.X, ImagePosition.Y);

                return Gum.Mesh.Merge(base.Redraw(), Gum.Mesh.CreateScale9Background(
                    ImagePosition.Interior(-1,-1,-1,-1), Root.GetTileSheet("border-thin-transparent")), mesh);
            }
        }

        public GuiDebugState(DwarfGame game, GameStateManager stateManager) :
            base(game, "GuiDebugState", stateManager)
        {
        }

        private Gum.Widget MakeMenuFrame(String Name)
        {
           
            return GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                MinimumSize = new Point(256, 200),
                Border = "border-fancy",
                AutoLayout = Gum.AutoLayout.FloatCenter,
                TextHorizontalAlign = Gum.HorizontalAlign.Center,
                Text = Name,
                InteriorMargin = new Gum.Margin(12,0,0,0),
                Padding = new Gum.Margin(2, 2, 2, 2)
            });
        }

        private void MakeMenuItem(Gum.Widget Menu, string Name, string Tooltip, Action<Gum.Widget, Gum.InputEventArgs> OnClick)
        {
            Menu.AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockTop,
                Border = "border-thin",
                Text = Name,
                OnClick = OnClick,
                Tooltip = Tooltip,
                TextHorizontalAlign = Gum.HorizontalAlign.Center,
                TextVerticalAlign = Gum.VerticalAlign.Center
            });
        }

        public void MakeMenu()
        {
            GuiRoot.RootItem.Clear();

            var frame = MakeMenuFrame("GUI DEBUG");

            MakeMenuItem(frame, "View Atlas", "", (sender, args) =>
                {
                    var pane = GuiRoot.ConstructWidget(new ShowTextureDialog());
                    GuiRoot.ShowDialog(pane);
                    GuiRoot.RootItem.Layout();
                });   

            MakeMenuItem(frame, "BACK", "Goodbye.", (sender, args) => StateManager.PopState());

            GuiRoot.RootItem.Layout();
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gum.Root(new Point(640, 480), DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);
            MakeMenu();
            IsInitialized = true;

            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                }
            }

            GuiRoot.Update(gameTime.ToGameTime());

            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}