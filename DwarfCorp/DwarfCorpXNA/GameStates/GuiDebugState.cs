using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using System.Linq;

namespace DwarfCorp.GameStates
{
    public class GuiDebugState : GameState
    {
        private Gui.Root GuiRoot;
        private Gui.Widgets.ProgressBar Progress;

        public class ShowTextureDialog : Gui.Widget
        {
            private Rectangle ImagePosition;

            public override void Construct()
            {
                AutoLayout = AutoLayout.DockFill;
                Background = new TileReference("basic", 1);
                BackgroundColor = new Vector4(1, 1, 1, 1);

                var imageSize = new Point(Root.RenderData.Texture.Width, Root.RenderData.Texture.Height);
                var availableArea = new Point(Root.RenderData.VirtualScreen.Width, Root.RenderData.VirtualScreen.Height - 32);

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

                ImagePosition = new Rectangle((Root.RenderData.VirtualScreen.Width / 2) - (imageSize.X / 2),
                    ((Root.RenderData.VirtualScreen.Height - 32) / 2) - (imageSize.Y / 2),
                    imageSize.X, imageSize.Y);

                var bottomBar = AddChild(new Widget
                    {
                        MinimumSize = new Point(0,32),
                        AutoLayout = Gui.AutoLayout.DockBottom,
                        Padding = new Margin(2,2,2,2)
                    });

                bottomBar.AddChild(new Widget
                {
                    Text = "CLOSE",
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Center,
                    Border = "border-thin",
                    Font = "font18-outline",
                    OnClick = (sender, args) => this.Close(),
                    AutoLayout = AutoLayout.DockRight
                });

                bottomBar.AddChild(new Widget
                    {
                        Border = "border-thin",
                        Font = "font18-outline",
                        TextHorizontalAlign = HorizontalAlign.Center,
                        TextVerticalAlign = VerticalAlign.Center,
                        TextColor = new Vector4(1, 1, 1, 1),
                        Text = "WHITE",
                        AutoLayout = Gui.AutoLayout.DockLeft,
                        OnClick = (sender, args) =>
                            {
                                this.BackgroundColor = new Vector4(1, 1, 1, 1);
                                this.Invalidate();
                            }
                    });

                bottomBar.AddChild(new Widget
                {
                    Border = "border-thin",
                    Font = "font18-outline",
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Center,
                    TextColor = new Vector4(0, 0, 0, 1),
                    Text = "BLACK",
                    AutoLayout = Gui.AutoLayout.DockLeft,
                    OnClick = (sender, args) =>
                    {
                        this.BackgroundColor = new Vector4(0, 0, 0, 1);
                        this.Invalidate();
                    }
                });

                bottomBar.AddChild(new Widget
                {
                    Font = "font18-outline",
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Center,
                    TextColor = new Vector4(1, 1, 1, 1),
                    Text = String.Format("{0} x {1}", Root.RenderData.Texture.Width, Root.RenderData.Texture.Height),
                    AutoLayout = Gui.AutoLayout.DockLeft,
                });
            }

            protected override Gui.Mesh Redraw()
            {
                var mesh = Gui.Mesh.Quad()
                    .Scale(ImagePosition.Width, ImagePosition.Height)
                    .Translate(ImagePosition.X, ImagePosition.Y);

                return Gui.Mesh.Merge(base.Redraw(), Gui.Mesh.CreateScale9Background(
                    ImagePosition.Interior(-1,-1,-1,-1), Root.GetTileSheet("border-thin-transparent")), mesh);
            }
        }

        public GuiDebugState(DwarfGame game, GameStateManager stateManager) :
            base(game, "GuiDebugState", stateManager)
        {
        }

        private Gui.Widget MakeMenuFrame(String Name)
        {
           
            return GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                MinimumSize = new Point(256, 200),
                Border = "border-fancy",
                AutoLayout = Gui.AutoLayout.FloatCenter,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                Text = Name,
                InteriorMargin = new Gui.Margin(12,0,0,0),
                Padding = new Gui.Margin(2, 2, 2, 2)
            });
        }

        private void MakeMenuItem(Gui.Widget Menu, string Name, string Tooltip, Action<Gui.Widget, Gui.InputEventArgs> OnClick)
        {
            Menu.AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockTop,
                Border = "border-thin",
                Text = Name,
                OnClick = OnClick,
                Tooltip = Tooltip,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center
            });
        }

        public void MakeMenu()
        {
            GuiRoot.RootItem.Clear();

            var frame = MakeMenuFrame("GUI DEBUG");

            MakeMenuItem(frame, "View Atlas \u00DF", "", (sender, args) =>
                {
                    var pane = GuiRoot.ConstructWidget(new ShowTextureDialog());
                    GuiRoot.ShowDialog(pane);
                    GuiRoot.RootItem.Layout();
                });

            MakeMenuItem(frame, "BACK", "Goodbye.", (sender, args) => StateManager.PopState());
        }

        private Gui.Widget CreateTray(IEnumerable<Widget> Icons)
        {
            return GuiRoot.ConstructWidget(new Gui.Widgets.IconTray
            {
                SizeToGrid = new Point(Icons.Count(), 1),
                Corners = Scale9Corners.Top | Scale9Corners.Right | Scale9Corners.Left,
                ItemSource = Icons,
                Hidden = true,
                //OnMouseLeave = (sender, args) => sender.Hidden = true
            });
        }

        private Gui.Widget CreateTrayIcon(int Tile, Gui.Widget Child)
        {
            var r = new Gui.Widgets.FramedIcon
            {
                Icon = new Gui.TileReference("tool-icons", Tile),
                OnClick = (sender, args) =>
                {
                    
                },
                OnHover = (sender) =>
                {
                    foreach (var child in sender.Parent.EnumerateChildren().Where(c => c is Gui.Widgets.FramedIcon)
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

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
            MakeMenu();

            Progress = GuiRoot.RootItem.AddChild(new Gui.Widgets.ProgressBar
            {
                Rect = new Rectangle(0, 0, GuiRoot.RenderData.VirtualScreen.Width, 32)
            }) as Gui.Widgets.ProgressBar;

           Dictionary<GameMaster.ToolMode, Gui.Widget> ToolbarItems = new Dictionary<GameMaster.ToolMode, Gui.Widget>();

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

            var roomIcons = GuiRoot.GetTileSheet("rooms") as Gui.TileSheet;
            RoomLibrary.InitializeStatics();
            var Tilesheet = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_tiles);
            VoxelLibrary.InitializeDefaultLibrary(Game.GraphicsDevice, Tilesheet);


            var bottomRightTray = GuiRoot.RootItem.AddChild(new Gui.Widgets.ToolTray.Tray
            {
                IsRootTray = true,
                Corners = Gui.Scale9Corners.Left | Gui.Scale9Corners.Top,
                AutoLayout = Gui.AutoLayout.FloatBottom,
                ItemSource = new Gui.Widget[]
                {
                    new Gui.Widgets.ToolTray.Icon
                    {
                        Icon = new TileReference("tool-icons", 5),
                        KeepChildVisible = true,
                        ExpansionChild = new Gui.Widgets.ToolTray.Tray
                        {
                            ItemSource = RoomLibrary.GetRoomTypes().Select(name => RoomLibrary.GetData(name))
                                .Select(data => new Gui.Widgets.ToolTray.Icon
                                {
                                    Icon = data.NewIcon,
                                    ExpansionChild = new Gui.Widgets.BuildRoomInfo
                                    {
                                        Data = data,
                                        Rect = new Rectangle(0,0,256,128)
                                    },
                                    OnClick = (sender, args) =>
                                    {
                                        (sender as Gui.Widgets.FramedIcon).Enabled = false;
                                    }
                                })
                        }
                    },
                    new Gui.Widgets.ToolTray.Icon
                    {
                        Icon = new TileReference("tool-icons", 6),
                        KeepChildVisible = true,
                        ExpansionChild = new Gui.Widgets.ToolTray.Tray
                        {
                            ItemSource = VoxelLibrary.GetTypes().Where(voxel => voxel.IsBuildable)
                                .Select(data => new Gui.Widgets.ToolTray.Icon
                                {
                                    Icon = new TileReference("rooms", 0),
                                    ExpansionChild = new Gui.Widgets.BuildWallInfo
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

            Progress.Percentage = (float)gameTime.TotalGameTime.TotalSeconds / 10.0f;
            Progress.Percentage = Progress.Percentage - (float)Math.Floor(Progress.Percentage);

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