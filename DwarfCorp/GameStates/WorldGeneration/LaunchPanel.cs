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
                    var saveName = DwarfGame.GetWorldDirectory() + Path.DirectorySeparatorChar + Settings.Overworld.Name + Path.DirectorySeparatorChar + String.Format("{0}-{1}", (int)Settings.InstanceSettings.Origin.X, (int)Settings.InstanceSettings.Origin.Y);
                    var saveGame = SaveGame.LoadMetaFromDirectory(saveName);
                    if (saveGame != null)
                    {
                        GameStateManager.ClearState();
                        GameStateManager.PushState(new LoadState(Game,
                            new OverworldGenerationSettings
                            {
                                InstanceSettings = new InstanceSettings
                                {
                                    ExistingFile = saveName,
                                    LoadType = LoadType.LoadFromFile,
                                    Cell = Settings.InstanceSettings.Cell
                                },
                                Name = saveName
                            }));
                    }
                    else
                    {
                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", string.Format("User is starting a game with a {0} x {1} world.", Settings.Width, Settings.Height));
                        Settings.Overworld.Name = Settings.Name;
                        Settings.InstanceSettings.ExistingFile = null;
                        Settings.InstanceSettings.LoadType = LoadType.CreateNew;

                        GameStateManager.ClearState();
                        GameStateManager.PushState(new LoadState(Game, Settings));
                    }
                }
            });

            CellInfo = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font10"
            });

            ZoomedPreview = AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockBottom,
                OnLayout = (sender) =>
                {
                    var space = System.Math.Min(CellInfo.Rect.Width, StartButton.Rect.Top - CellInfo.Rect.Bottom - 4);
                    sender.Rect.Height = space;
                    sender.Rect.Width = space;
                    sender.Rect.Y = StartButton.Rect.Top - space - 2;
                    sender.Rect.X = CellInfo.Rect.X +  ((CellInfo.Rect.Width - space) / 2);
                }
            });

            UpdateCellInfo();

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
                SaveName = DwarfGame.GetWorldDirectory() + Path.DirectorySeparatorChar + Settings.Overworld.Name + Path.DirectorySeparatorChar + String.Format("{0}-{1}", (int)Settings.InstanceSettings.Origin.X, (int)Settings.InstanceSettings.Origin.Y);
                var saveGame = SaveGame.LoadMetaFromDirectory(SaveName);
                if (saveGame != null)
                {
                    StartButton.Text = "Load";
                    CellInfo.Text = saveGame.Metadata.DescriptionString;
                    StartButton.Hidden = false;
                }
                else
                {
                    StartButton.Hidden = false;
                    StartButton.Text = "Create";
                    CellInfo.Text = String.Format("World Size: {0}x{1}", 
                        Settings.InstanceSettings.Cell.Bounds.Width * VoxelConstants.ChunkSizeX, 
                        Settings.InstanceSettings.Cell.Bounds.Height * VoxelConstants.ChunkSizeZ);
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