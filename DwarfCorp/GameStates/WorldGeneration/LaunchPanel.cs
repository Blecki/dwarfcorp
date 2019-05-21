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

        public LaunchPanel(DwarfGame Game, WorldGenerator Generator, OverworldGenerationSettings Settings) 
        {
            this.Generator = Generator;
            this.Settings = Settings;
            this.Game = Game;

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
                                    ExistingFile = saveName
                                },
                                Name = saveName
                            }));
                    }
                    else
                    {
                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", string.Format("User is starting a game with a {0} x {1} world.", Settings.Width, Settings.Height));
                        Settings.Overworld.Name = Settings.Name;
                        Settings.InstanceSettings.ExistingFile = null;

                        if (Settings.Natives == null || Settings.Natives.Count == 0)
                            Settings.Natives = Generator.NativeCivilizations;

                        foreach (var faction in Settings.Natives)
                        {
                            Vector2 center = new Vector2(faction.Center.X, faction.Center.Y);
                            Vector2 spawn = new Vector2(Generator.GetSpawnRectangle().Center.X, Generator.GetSpawnRectangle().Center.Y);
                            faction.DistanceToCapital = (center - spawn).Length();
                            faction.ClaimsColony = false;
                        }

                        foreach (var faction in Generator.GetFactionsInSpawn())
                            faction.ClaimsColony = true;

                        GameStateManager.ClearState();
                        GameStateManager.PushState(new LoadState(Game, Settings));
                    }
                }
            });

            CellInfo = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                TextColor = new Vector4(0, 0, 0, 1)
            });

            UpdateCellInfo();

            base.Construct();
        }

        private void UpdateCellInfo()
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
                    CellInfo.Text = "";
                    StartButton.Hidden = false;
                }
                else
                {
                    StartButton.Hidden = false;
                    StartButton.Text = "Create";
                    CellInfo.Text = "";
                }
            }
        }
    }
}