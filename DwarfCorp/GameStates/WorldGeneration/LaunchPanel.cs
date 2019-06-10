using System;
using System.Collections.Generic;
using System.Linq;
using DwarfCorp.Gui;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using System.IO;

namespace DwarfCorp.GameStates
{
    // Todo: Make this use wait cursor while generating.
    public class LaunchPanel : Widget
    {
        private Gui.Widget StartButton;
        public WorldGenerator Generator = null;
        private OverworldGenerationSettings Settings;
        private Widget CellInfo;
        private String SaveName;
        private DwarfGame Game;
        private WorldGeneratorState Preview;
        private Widget ZoomedPreview;

        public LaunchPanel(DwarfGame Game, WorldGenerator Generator, OverworldGenerationSettings Settings, WorldGeneratorState Preview) 
        {
            this.Generator = Generator;
            this.Settings = Settings;
            this.Game = Game;
            this.Preview = Preview;

            if (Generator != null && Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                throw new InvalidProgramException();
        }

        public override void Construct()
        {
            AddChild(new Gui.Widget
            {
                Text = "Back",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    GameStateManager.PopState();
                }
            });

            StartButton = AddChild(new Gui.Widget
            {
                Text = "Start Game",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockBottom,
                OnClick = (sender, args) =>
                {
                    var saveName = DwarfGame.GetWorldDirectory() + Path.DirectorySeparatorChar + Settings.Name + Path.DirectorySeparatorChar + String.Format("{0}-{1}", (int)Settings.InstanceSettings.Origin.X, (int)Settings.InstanceSettings.Origin.Y);
                    var saveGame = SaveGame.LoadMetaFromDirectory(saveName);
                    if (saveGame != null)
                    {
                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", "User is loading a saved game.");
                        Settings.InstanceSettings.ExistingFile = saveName;
                        Settings.InstanceSettings.LoadType = LoadType.LoadFromFile;
                        
                        GameStateManager.ClearState();
                        GameStateManager.PushState(new LoadState(Game, Settings));
                    }
                    else
                    {
                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", string.Format("User is starting a game with a {0} x {1} world.", Settings.Width, Settings.Height));
                        Settings.InstanceSettings.ExistingFile = null;
                        Settings.InstanceSettings.LoadType = LoadType.CreateNew;

                        var message = "";
                        var valid = Settings.InstanceSettings.InitalEmbarkment.ValidateEmbarkment(Settings, out message);
                        if (valid == Embarkment.ValidationResult.Pass)
                        {
                            GameStateManager.ClearState();
                            GameStateManager.PushState(new LoadState(Game, Settings));
                        }
                        else if (valid == Embarkment.ValidationResult.Query)
                        {
                            var popup = new Gui.Widgets.Confirm()
                            {
                                Text = message,
                                OnClose = (_sender) =>
                                {
                                    if ((_sender as Gui.Widgets.Confirm).DialogResult == Gui.Widgets.Confirm.Result.OKAY)
                                    {
                                        GameStateManager.ClearState();
                                        GameStateManager.PushState(new LoadState(Game, Settings));
                                    }
                                }
                            };
                            Root.ShowModalPopup(popup);
                        }
                        else if (valid == Embarkment.ValidationResult.Reject)
                        {
                            var popup = new Gui.Widgets.Confirm()
                            {
                                Text = message,
                                CancelText = ""
                            };
                            Root.ShowModalPopup(popup);
                        }
                    }
                }
            });

            CellInfo = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font10"
            });

            ZoomedPreview = AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockBottom,
                OnLayout = (sender) =>
                {
                    sender.Rect.Height = StartButton.Rect.Width;
                    sender.Rect.Width = StartButton.Rect.Width;
                    sender.Rect.Y = StartButton.Rect.Top - StartButton.Rect.Width - 2;
                    sender.Rect.X = StartButton.Rect.X;
                }
            });

            UpdateCellInfo();
            this.Layout();

            base.Construct();
        }

        public void UpdateCellInfo()
        {
            if (Settings.InstanceSettings.Origin.X < 0 || Settings.InstanceSettings.Origin.X >= Settings.Width ||
                Settings.InstanceSettings.Origin.Y < 0 || Settings.InstanceSettings.Origin.Y >= Settings.Height)
            {
                StartButton.Hidden = true;
                CellInfo.Text = "Select a spawn cell to continue";
                SaveName = "";
            }
            else
            {
                SaveName = DwarfGame.GetWorldDirectory() + Path.DirectorySeparatorChar + Settings.Name + Path.DirectorySeparatorChar + String.Format("{0}-{1}", (int)Settings.InstanceSettings.Origin.X, (int)Settings.InstanceSettings.Origin.Y);
                var saveGame = SaveGame.LoadMetaFromDirectory(SaveName);

                if (saveGame != null)
                {
                    StartButton.Text = "Load";
                    CellInfo.Clear();
                    CellInfo.Text = saveGame.Metadata.DescriptionString;
                    StartButton.Hidden = false;
                    CellInfo.Layout();
                }
                else
                {
                    StartButton.Hidden = false;
                    StartButton.Text = "Create";
                    CellInfo.Clear();
                    CellInfo.Text = "";

                    CellInfo.AddChild(new Gui.Widget
                    {
                        Text = "Edit Embarkment",
                        Border = "border-button",
                        ChangeColorOnHover = true,
                        TextColor = new Vector4(0, 0, 0, 1),
                        Font = "font16",
                        AutoLayout = Gui.AutoLayout.DockTop,
                        MinimumSize = new Point(0, 32),
                        OnClick = (sender, args) =>
                        {
                            DwarfGame.LogSentryBreadcrumb("Game Launcher", "User is modifying embarkment.");
                            var embarkmentEditor = Root.ConstructWidget(new EmbarkmentEditor(Settings)
                            {
                                OnClose = (s) =>
                                {
                                }
                            });

                            Root.ShowModalPopup(embarkmentEditor);
                        }
                    });

                    CellInfo.AddChild(new Widget
                    {
                        Text = String.Format("World Size: {0}x{1}",
                            Settings.InstanceSettings.Cell.Bounds.Width * VoxelConstants.ChunkSizeX,
                            Settings.InstanceSettings.Cell.Bounds.Height * VoxelConstants.ChunkSizeZ), // Todo: Display cost of embarkment here
                        AutoLayout = AutoLayout.DockFill
                    });

                    CellInfo.Layout();
                }
            }
        }

        public void DrawPreview()
        {
            Root.DrawMesh(
                    Gui.Mesh.Quad()
                    .Scale(-ZoomedPreview.Rect.Width, -ZoomedPreview.Rect.Height)
                    .Translate(ZoomedPreview.Rect.X + ZoomedPreview.Rect.Width,
                        ZoomedPreview.Rect.Y + ZoomedPreview.Rect.Height)
                    .Texture(Preview.Preview.ZoomedPreviewMatrix),
                    Preview.Preview.PreviewTexture);
        }
    }
}