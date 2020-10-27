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
        private Gui.Mesh ZoomedPreviewMesh;

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
            if (Settings.InstanceSettings == null || Settings.Natives == null)
                return; // Someone crashed here.

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
                    var saveName = DwarfGame.GetWorldDirectory() + Path.DirectorySeparatorChar + Settings.Name;
                    var saveGame = SaveGame.LoadMetaFromDirectory(saveName);
                    if (saveGame != null)
                    {
                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", "User is loading a saved game.");
                        Settings.InstanceSettings.LoadType = LoadType.LoadFromFile;

                        GameStateManager.ClearState();
                        GameStateManager.PushState(new LoadState(Game, Settings, LoadTypes.UseExistingOverworld));
                    }
                    else
                    {
                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", string.Format("User is starting a game with a {0} x {1} world.", Settings.Width, Settings.Height));
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
            SaveName = DwarfGame.GetWorldDirectory() + Path.DirectorySeparatorChar + Settings.Name;
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
        

        private void SetCreateCellInfoText(Widget Widget)
        {
            try
            { //Todo: Cleanup
                Widget.Text = String.Format("\nCorporate Funds: {5}\nWorld Size: {0}x{1}\nLand Cost: {2}\nEmbarkment Cost: {3}\nTotal Cost: {4}",
                    256, 256, 0,
                    Settings.InstanceSettings.InitalEmbarkment.TotalCost(),
                    Settings.InstanceSettings.TotalCreationCost(),
                    Settings.PlayerCorporationFunds);

                if (Settings.InstanceSettings.TotalCreationCost() > Settings.PlayerCorporationFunds)
                    Widget.TextColor = new Vector4(1, 0, 0, 1);
                else
                    Widget.TextColor = new Vector4(0, 0, 0, 1);
            }
            catch (Exception e)
            {
                Widget.Text = "Something went wrong when calculating the embarkment cost.";
            }
        }
    }
}