using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum;
using System.Linq;

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

        private Gum.Widget CreateTray(IEnumerable<Widget> Icons)
        {
            return GuiRoot.ConstructWidget(new NewGui.IconTray
            {
                SizeToGrid = new Point(Icons.Count(), 1),
                Corners = Scale9Corners.Top | Scale9Corners.Right | Scale9Corners.Left,
                ItemSource = Icons,
                Hidden = true,
                //OnMouseLeave = (sender, args) => sender.Hidden = true
            });
        }

        private Gum.Widget CreateTrayIcon(int Tile, Gum.Widget Child)
        {
            var r = new NewGui.FramedIcon
            {
                Icon = new Gum.TileReference("tool-icons", Tile),
                OnClick = (sender, args) =>
                {
                    
                },
                OnHover = (sender) =>
                {
                    foreach (var child in sender.Parent.EnumerateChildren().Where(c => c is NewGui.FramedIcon)
                    .SelectMany(c => c.EnumerateChildren()))
                    {
                        child.Hidden = true;
                        child.Invalidate();
                    }

                    if (Child != null)
                    {
                        Child.Hidden = false;
                        Child.Invalidate();
                    }
                },
                OnLayout = (sender) =>
                {
                    if (Child != null)
                    {
                        var midPoint = sender.Rect.X + (sender.Rect.Width / 2);
                        Child.Rect.X = midPoint - (Child.Rect.Width / 2);
                        Child.Rect.Y = sender.Rect.Y - 60;
                    }
                }
            };

            GuiRoot.ConstructWidget(r);
            if (Child != null) r.AddChild(Child);
            return r;
        }
        
        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gum.Root(new Point(640, 480), DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);
            MakeMenu();

           Dictionary<GameMaster.ToolMode, Gum.Widget> ToolbarItems = new Dictionary<GameMaster.ToolMode, Gum.Widget>();

            //ToolbarItems[GameMaster.ToolMode.SelectUnits] = CreateIcon(5, GameMaster.ToolMode.SelectUnits);
            //    ToolbarItems[GameMaster.ToolMode.Dig] = CreateIcon(0, GameMaster.ToolMode.Dig);
            //    ToolbarItems[GameMaster.ToolMode.Build] = CreateIcon(2, GameMaster.ToolMode.Build);
            //    ToolbarItems[GameMaster.ToolMode.Cook] = CreateIcon(3, GameMaster.ToolMode.Cook);
            //    ToolbarItems[GameMaster.ToolMode.Farm] = CreateIcon(5, GameMaster.ToolMode.Farm);
            //    ToolbarItems[GameMaster.ToolMode.Magic] = CreateIcon(6, GameMaster.ToolMode.Magic);
            //    ToolbarItems[GameMaster.ToolMode.Gather] = CreateIcon(6, GameMaster.ToolMode.Gather);
            //    ToolbarItems[GameMaster.ToolMode.Chop] = CreateIcon(1, GameMaster.ToolMode.Chop);
            //    ToolbarItems[GameMaster.ToolMode.Guard] = CreateIcon(4, GameMaster.ToolMode.Guard);
            //    ToolbarItems[GameMaster.ToolMode.Attack] = CreateIcon(3, GameMaster.ToolMode.Attack);

            var roomIcons = GuiRoot.GetTileSheet("rooms") as Gum.TileSheet;
            RoomLibrary.InitializeStatics();
            var Tilesheet = TextureManager.GetTexture(ContentPaths.Terrain.terrain_tiles);
            VoxelLibrary.InitializeDefaultLibrary(Game.GraphicsDevice, Tilesheet);


            var bottomRightTray = GuiRoot.RootItem.AddChild(new NewGui.ToolTray.Tray
            {
                Corners = Gum.Scale9Corners.Left | Gum.Scale9Corners.Top,
                AutoLayout = Gum.AutoLayout.FloatBottomRight,
                ItemSource = new Gum.Widget[]
                {
                    new NewGui.ToolTray.ExpandingIcon
                    {
                        Icon = new TileReference("tool-icons", 5),
                        ExpansionChild = new NewGui.ToolTray.Tray
                        {
                            ItemSource = RoomLibrary.GetRoomTypes().Select(name => RoomLibrary.GetData(name))
                                .Select(data => new NewGui.ToolTray.LeafIcon
                                {
                                    Icon = new TileReference("rooms", roomIcons.ConvertRectToIndex(data.Icon.SourceRect)),
                                    ExpansionChild = new NewGui.BuildRoomButton
                                    {
                                        Data = data,
                                        Rect = new Rectangle(0,0,256,128)
                                    },
                                    OnClick = (sender, args) =>
                                    {
                                        (sender as NewGui.FramedIcon).Enabled = false;
                                    }
                                })
                        }
                    },
                    new NewGui.ToolTray.ExpandingIcon
                    {
                        Icon = new TileReference("tool-icons", 6),
                        ExpansionChild = new NewGui.ToolTray.Tray
                        {
                            ItemSource = VoxelLibrary.GetTypes().Where(voxel => voxel.IsBuildable)
                                .Select(data => new NewGui.ToolTray.LeafIcon
                                {
                                    Icon = new TileReference("rooms", 0),
                                    ExpansionChild = new NewGui.BuildWallButton
                                    {
                                        Data = data,
                                        Rect = new Rectangle(0,0,256,128)
                                    }
                                })

                        }
                    }
                }
            });

            bottomRightTray.Hidden = false;
            GuiRoot.RootItem.Layout();

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