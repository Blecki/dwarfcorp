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
    public class OverworldTileChooseState : GameState
    {
        private Gui.Root GuiRoot;
        private WorldGeneratorPreview Preview;
        private Gui.Widget StartButton;
        private WorldGenerator Generator = null;
        private OverworldGenerationSettings Settings;
        private Widget CellInfo;
        private String SaveName;

        public OverworldTileChooseState(DwarfGame Game, GameStateManager StateManager, WorldGenerator Generator, OverworldGenerationSettings Settings) :
            base(Game, "NewWorldGeneratorState", StateManager)
        {
            this.Generator = Generator;
            this.Settings = Settings;

            if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                throw new InvalidProgramException();
        }

        public OverworldTileChooseState(DwarfGame Game, GameStateManager StateManager, OverworldGenerationSettings Settings) :
            base(Game, "NewWorldGeneratorState", StateManager)
        {
            this.Settings = Settings;
        }

        public override void OnEnter()
        {
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new MousePointer("mouse", 15.0f, 16, 17, 18, 19, 20, 21, 22, 23);

            var mainPanel = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = GuiRoot.RenderData.VirtualScreen,
                Border = "border-fancy",
                Text = Settings.Name,
                Font = "font16",
                TextColor = new Vector4(0, 0, 0, 1),
                Padding = new Gui.Margin(4, 4, 4, 4),
                InteriorMargin = new Gui.Margin(24, 0, 0, 0),
            });

            var rightPanel = mainPanel.AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockRight,
                MinimumSize = new Point(256, 0),
                Padding = new Gui.Margin(2,2,2,2)
            });

            rightPanel.AddChild(new Gui.Widget
            {
                Text = "Back",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    if (StateManager.CurrentState == this)
                        StateManager.PopState();
                }
            });

            StartButton = rightPanel.AddChild(new Gui.Widget
            {
                Text = "Start Game",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockBottom,
                OnClick = (sender, args) =>
                {
                    var saveName = DwarfGame.GetWorldDirectory() + Path.DirectorySeparatorChar + Settings.Overworld.Name + Path.DirectorySeparatorChar + String.Format("{0}-{1}", (int)Settings.Origin.X, (int)Settings.Origin.Y);
                    var saveGame = SaveGame.LoadMetaFromDirectory(saveName);
                    if (saveGame != null)
                    {
                        StateManager.ClearState();
                        StateManager.PushState(new LoadState(Game, Game.StateManager,
                            new OverworldGenerationSettings
                            {
                                ExistingFile = saveName,
                                Name = saveName
                            }));
                    }
                    else
                    {
                        GameStates.GameState.Game.LogSentryBreadcrumb("WorldGenerator", string.Format("User is starting a game with a {0} x {1} world.", Settings.Width, Settings.Height));
                        Settings.Overworld.Name = Settings.Name;
                            Settings.ExistingFile = null;
                            Settings.SpawnRect = Generator.GetSpawnRectangle();
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

                            StateManager.ClearState();
                            StateManager.PushState(new LoadState(Game, StateManager, Settings));
                    }
                }
            });

            CellInfo = rightPanel.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                TextColor = new Vector4(0, 0, 0, 1)
            });

            Preview = mainPanel.AddChild(new WorldGeneratorPreview(Game.GraphicsDevice)
            {
                Border = "border-thin",
                AutoLayout = Gui.AutoLayout.DockFill,
                Overworld = Settings.Overworld,
                OnCellSelectionMade = UpdateCellInfo
            }) as WorldGeneratorPreview;

            if (Generator == null)
            {
                Generator = new WorldGenerator(Settings, false);
                Generator.LoadDummy(
                    new Color[Settings.Overworld.Map.GetLength(0) * Settings.Overworld.Map.GetLength(1)],
                    Game.GraphicsDevice);
            }

            Preview.SetGenerator(Generator);
            UpdateCellInfo();

            GuiRoot.RootItem.Layout();
                        
            IsInitialized = true;

            base.OnEnter();
        }

        private void UpdateCellInfo()
        {
            if (Settings.Origin.X < 0 || Settings.Origin.X >= Settings.Width ||
                Settings.Origin.Y < 0 || Settings.Origin.Y >= Settings.Height)
            {
                StartButton.Hidden = true;
                CellInfo.Text = "Select a spawn cell to continue";
                SaveName = "";
            }
            else
            {
                SaveName = DwarfGame.GetWorldDirectory() + Path.DirectorySeparatorChar + Settings.Overworld.Name + Path.DirectorySeparatorChar + String.Format("{0}-{1}", (int)Settings.Origin.X, (int)Settings.Origin.Y);
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


            GuiRoot.Update(gameTime.ToRealTime());
            Preview.Update();
            base.Update(gameTime);

            Preview.PreparePreview(StateManager.Game.GraphicsDevice);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();

                Preview.DrawPreview();
                GuiRoot.MousePointer = new MousePointer("mouse", 1, 0);

            // This is a serious hack.
            GuiRoot.RedrawPopups();
          
            GuiRoot.DrawMouse();
            base.Render(gameTime);
        }
    }
}