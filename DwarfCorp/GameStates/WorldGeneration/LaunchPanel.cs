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
    public class LaunchPanel : Widget
    {
        private Gui.Widget StartButton;
        public OverworldGenerator Generator = null;
        private Overworld Settings;
        private Widget CellInfo;
        private String SaveName;
        private DwarfGame Game;
        private WorldGeneratorState Preview;
        private Widget ZoomedPreview;

        public LaunchPanel(DwarfGame Game, OverworldGenerator Generator, Overworld Settings, WorldGeneratorState Preview) 
        {
            this.Generator = Generator;
            this.Settings = Settings;
            this.Game = Game;
            this.Preview = Preview;

            if (Generator != null && Generator.CurrentState != OverworldGenerator.GenerationState.Finished)
                throw new InvalidProgramException();
        }

        private void LaunchNewGame()
        {
            // Todo: Anger faction when you claim their land.
            var playerFaction = Settings.Natives.FirstOrDefault(f => f.Name == "Player");
            var politics = Settings.GetPolitics(Settings.InstanceSettings.Cell.Faction, playerFaction);
            politics.AddEvent(new PoliticalEvent
            {
                Change = -2.0f,
                Description = "You stole our land."
            });

            Settings.InstanceSettings.Cell.Faction = playerFaction;

            GameStateManager.ClearState();
            GameStateManager.PushState(new LoadState(Game, Settings, LoadTypes.UseExistingOverworld));
        }

        public override void Construct()
        {
            Padding = new Margin(2, 2, 0, 0);
            
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
                        GameStateManager.PushState(new LoadState(Game, Settings, LoadTypes.UseExistingOverworld));
                    }
                    else
                    {
                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", string.Format("User is starting a game with a {0} x {1} world.", Settings.Width, Settings.Height));
                        Settings.InstanceSettings.ExistingFile = null;
                        Settings.InstanceSettings.LoadType = LoadType.CreateNew;

                        var message = "";
                        var valid = InstanceSettings.ValidateEmbarkment(Settings, out message);
                        if (valid == InstanceSettings.ValidationResult.Pass)
                            LaunchNewGame();
                        else if (valid == InstanceSettings.ValidationResult.Query)
                        {
                            var popup = new Gui.Widgets.Confirm()
                            {
                                Text = message,
                                OnClose = (_sender) =>
                                {
                                    if ((_sender as Gui.Widgets.Confirm).DialogResult == Gui.Widgets.Confirm.Result.OKAY)
                                        LaunchNewGame();
                                }
                            };
                            Root.ShowModalPopup(popup);
                        }
                        else if (valid == InstanceSettings.ValidationResult.Reject)
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
                CellInfo.Text = "\nSelect a spawn cell to continue";
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
                    CellInfo.Text = "\n" + saveGame.Metadata.DescriptionString;
                    StartButton.Hidden = false;
                    CellInfo.Layout();
                }
                else
                {
                    StartButton.Hidden = false;
                    StartButton.Text = "Create";
                    CellInfo.Clear();
                    CellInfo.Text = "";

                    var cellInfoText = Root.ConstructWidget(new Widget
                    {
                        AutoLayout = AutoLayout.DockFill
                    });

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
                                    SetCreateCellInfoText(cellInfoText);
                                }
                            });

                            Root.ShowModalPopup(embarkmentEditor);
                        }
                    });

                    CellInfo.AddChild(cellInfoText);
                    SetCreateCellInfoText(cellInfoText);

                    CellInfo.Layout();
                }
            }
        }

        private void SetCreateCellInfoText(Widget Widget)
        {
            Widget.Text = String.Format("\nCorporate Funds: {5}\nWorld Size: {0}x{1}\nLand Cost: {2}\nEmbarkment Cost: {3}\nTotal Cost: {4}",
                Settings.InstanceSettings.Cell.Bounds.Width * VoxelConstants.ChunkSizeX,
                Settings.InstanceSettings.Cell.Bounds.Height * VoxelConstants.ChunkSizeZ,
                Settings.InstanceSettings.CalculateLandValue(),
                Settings.InstanceSettings.InitalEmbarkment.TotalCost(),
                Settings.InstanceSettings.TotalCreationCost(),
                Settings.PlayerCorporationFunds);

            if (Settings.InstanceSettings.TotalCreationCost() > Settings.PlayerCorporationFunds)
                Widget.TextColor = new Vector4(1, 0, 0, 1);
            else
                Widget.TextColor = new Vector4(0, 0, 0, 1);
        }

        public void DrawPreview()
        {
            Root.DrawMesh(
                    Gui.Mesh.Quad()
                    .Scale(-ZoomedPreview.Rect.Width, -ZoomedPreview.Rect.Height)
                    .Translate(ZoomedPreview.Rect.X + ZoomedPreview.Rect.Width,
                        ZoomedPreview.Rect.Y + ZoomedPreview.Rect.Height)
                    .Texture(Preview.Preview.ZoomedPreviewMatrix),
                    Preview.Preview.TerrainTexture);
        }
    }
}