using System.Collections.Generic;
using System.Linq;
using DwarfCorp.Gui;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.GameStates
{
    // Todo: Make this use wait cursor while generating.
    public class NewWorldGeneratorState : GameState
    {
        private Gui.Root GuiRoot;
        private Gui.Widgets.ProgressBar GenerationProgress;
        private Gui.Widget ZoomedPreview;
        private WorldGeneratorPreview Preview;
        private Gui.Widget StartButton;
        private Gui.Widgets.CheckBox StartUnderground;
        private Gui.Widgets.CheckBox RevealSurface;
        private WorldGenerator Generator;
        private OverworldGenerationSettings Settings;
        private bool AutoGenerate;
        
        public NewWorldGeneratorState(DwarfGame Game, GameStateManager StateManager, OverworldGenerationSettings Settings, bool AutoGenerate) :
            base(Game, "NewWorldGeneratorState", StateManager)
        {
            this.AutoGenerate = AutoGenerate;
            this.Settings = Settings;
            if (this.Settings == null)
                this.Settings = new OverworldGenerationSettings()
                {
                    Width = 512,
                    Height = 512,
                    Name = TextGenerator.GenerateRandom(TextGenerator.GetAtoms(ContentPaths.Text.Templates.worlds)),
                    NumCivilizations = 5,
                    NumFaults = 3,
                    NumRains = 1000,
                    NumVolcanoes = 3,
                    RainfallScale = 1.0f,
                    SeaLevel = 0.17f,
                    TemperatureScale = 1.0f
                };           
        }       

        private void RestartGeneration()
        {
            if (Generator != null)
                Generator.Abort();
            if (Settings.Natives != null)
                Settings.Natives.Clear();
            Generator = new WorldGenerator(Settings, true);
            if (Preview != null) Preview.SetGenerator(Generator);
            Generator.Generate();
            GuiRoot.RootItem.GetChild(0).Text = Settings.Name;
            GuiRoot.RootItem.GetChild(0).Invalidate();
            GuiRoot.MousePointer = new MousePointer("mouse", 15.0f, 16, 17, 18, 19, 20, 21, 22, 23);
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
                Text = "Regenerate",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockTop,
                OnClick = (sender, args) => {
                    GameStates.GameState.Game.LogSentryBreadcrumb("WorldGenerator", "User is regeneating the world.");
                    //Settings = new OverworldGenerationSettings();
                    RestartGeneration();
                }
            });

            rightPanel.AddChild(new Gui.Widget
            {
                Text = "Save World",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    GameStates.GameState.Game.LogSentryBreadcrumb("WorldGenerator", "User is saving the world.");
                    if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                        GuiRoot.ShowTooltip(GuiRoot.MousePosition, "Generator is not finished.");
                    else
                    {
                        global::System.IO.DirectoryInfo worldDirectory = global::System.IO.Directory.CreateDirectory(DwarfGame.GetWorldDirectory() + global::System.IO.Path.DirectorySeparatorChar + Settings.Name);
                        var file = new NewOverworldFile(Game.GraphicsDevice, Settings.Overworld, Settings.Name, Settings.SeaLevel);
                        file.WriteFile(worldDirectory.FullName);
                        GuiRoot.ShowModalPopup(GuiRoot.ConstructWidget(new Gui.Widgets.Popup
                        {
                            Text = "File saved."
                        }));
                    }
                }
            });

            rightPanel.AddChild(new Gui.Widget
            {
                Text = "Advanced",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    GameStates.GameState.Game.LogSentryBreadcrumb("WorldGenerator", "User is modifying advanced settings.");
                    var advancedSettingsEditor = GuiRoot.ConstructWidget(new Gui.Widgets.WorldGenerationSettingsDialog
                    {
                        Settings = Settings,
                        OnClose = (s) =>
                        {
                            if ((s as Gui.Widgets.WorldGenerationSettingsDialog).Result == Gui.Widgets.WorldGenerationSettingsDialog.DialogResult.Okay)
                                RestartGeneration();
                        }
                    });

                    GuiRoot.ShowModalPopup(advancedSettingsEditor);
                }
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
                    Generator.Abort();
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
                    StateManager.PushState(new OverworldTileChooseState(Game, StateManager, Generator, Settings));
                }
            });

            rightPanel.AddChild(new Gui.Widget
            {
                Text = "Territory size",
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1),
            });

            rightPanel.AddChild(new Gui.Widget
            {
                Text = "Difficulty",
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font8",
                TextColor = new Vector4(0,0,0,1)
            });

            var difficultySelectorCombo = rightPanel.AddChild(new Gui.Widgets.ComboBox
            {
                AutoLayout = Gui.AutoLayout.DockTop,
                Items = EmbarkmentLibrary.Enumerate().Select(e => e.Name).ToList(),
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font8",
                OnSelectedIndexChanged = (sender) =>
                {
                    Settings.InitalEmbarkment = EmbarkmentLibrary.GetEmbarkment((sender as Gui.Widgets.ComboBox).SelectedItem);
                }
            }) as Gui.Widgets.ComboBox;

            StartUnderground = rightPanel.AddChild(new Gui.Widgets.CheckBox
            {
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font8",
                Text = "@world-generation-start-underground"
            }) as Gui.Widgets.CheckBox;

            RevealSurface = rightPanel.AddChild(new Gui.Widgets.CheckBox
            {
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font8",
                Text = "@world-generation-reveal-surface",
                CheckState = true
            }) as Gui.Widgets.CheckBox;

            rightPanel.AddChild(new Gui.Widget
            {
                Text = "Caves",
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1),
            });

            var layerSetting = rightPanel.AddChild(new Gui.Widgets.ComboBox
            {
                AutoLayout = AutoLayout.DockTop,
                Items = new List<string>(new string[] { "Barely any", "Few", "Normal", "Lots", "Way too many" }),
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1),
                OnSelectedIndexChanged = (sender) =>
                {
                    switch ((sender as Gui.Widgets.ComboBox).SelectedItem)
                    {
                        case "Barely any": Settings.NumCaveLayers = 2; break;
                        case "Few": Settings.NumCaveLayers = 3; break;
                        case "Normal": Settings.NumCaveLayers = 4; break;
                        case "Lots": Settings.NumCaveLayers = 6; break;
                        case "Way too many": Settings.NumCaveLayers = 9; break;
                    }
                }
            }) as Gui.Widgets.ComboBox;

            rightPanel.AddChild(new Gui.Widget
            {
                Text = "Z Levels",
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1),
            });

            var zLevelSetting = rightPanel.AddChild(new Gui.Widgets.ComboBox
            {
                AutoLayout = AutoLayout.DockTop,
                Items = new List<string>(new string[] { "16", "64", "128" }),
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1),
                OnSelectedIndexChanged = (sender) =>
                {
                    switch ((sender as Gui.Widgets.ComboBox).SelectedItem)
                    {
                        case "16": Settings.zLevels = 1; break;
                        case "64": Settings.zLevels = 4; break;
                        case "128": Settings.zLevels = 8; break;
                    }
                }
            }) as Gui.Widgets.ComboBox;

            zLevelSetting.SelectedIndex = 1;

            ZoomedPreview = rightPanel.AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockBottom,
                OnLayout = (sender) =>
                {
                    var space = global::System.Math.Min(zLevelSetting.Rect.Width, StartButton.Rect.Top - zLevelSetting.Rect.Bottom - 4);
                    sender.Rect.Height = space;
                    sender.Rect.Width = space;
                    sender.Rect.Y = zLevelSetting.Rect.Bottom + 2;
                    sender.Rect.X = zLevelSetting.Rect.X + ((zLevelSetting.Rect.Width - space) / 2);
                    
                }
            });

            GenerationProgress = mainPanel.AddChild(new Gui.Widgets.ProgressBar
            {
                AutoLayout = Gui.AutoLayout.DockBottom,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                Font = "font10",
                TextColor = new Vector4(1,1,1,1)
            }) as Gui.Widgets.ProgressBar;

            Preview = mainPanel.AddChild(new WorldGeneratorPreview(Game.GraphicsDevice)
            {
                Border = "border-thin",
                AutoLayout = Gui.AutoLayout.DockFill,
                Overworld = Settings.Overworld
            }) as WorldGeneratorPreview;

            GuiRoot.RootItem.Layout();

            difficultySelectorCombo.SelectedIndex = difficultySelectorCombo.Items.IndexOf("Normal");
            layerSetting.SelectedIndex = layerSetting.Items.IndexOf("Normal");

            IsInitialized = true;

            if (AutoGenerate)
                RestartGeneration();
            else // Setup a dummy generator for now.
            {
                Generator = new WorldGenerator(Settings, true);
                Generator.LoadDummy(
                    new Color[Settings.Overworld.Map.GetLength(0) * Settings.Overworld.Map.GetLength(1)], 
                    Game.GraphicsDevice);
                Preview.SetGenerator(Generator);
            }

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

            GenerationProgress.Text = Generator.LoadingMessage;
            GenerationProgress.Percentage = Generator.Progress * 100.0f;

            // Enable or disable start button based on Generator state.

            GuiRoot.Update(gameTime.ToRealTime());
            if (Generator.CurrentState == WorldGenerator.GenerationState.Finished)
                Preview.Update();
            base.Update(gameTime);

            Preview.PreparePreview(StateManager.Game.GraphicsDevice);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();

            if (Generator.CurrentState == WorldGenerator.GenerationState.Finished)
            {
                Preview.DrawPreview();
                GuiRoot.DrawMesh(
                        Gui.Mesh.Quad()
                        .Scale(-ZoomedPreview.Rect.Width, -ZoomedPreview.Rect.Height)
                        .Translate(ZoomedPreview.Rect.X + ZoomedPreview.Rect.Width, 
                            ZoomedPreview.Rect.Y + ZoomedPreview.Rect.Height)
                        .Texture(Preview.ZoomedPreviewMatrix),
                        Preview.PreviewTexture);

                GuiRoot.MousePointer = new MousePointer("mouse", 1, 0);
            }

            // This is a serious hack.
            GuiRoot.RedrawPopups();
          
            GuiRoot.DrawMouse();
            base.Render(gameTime);
        }

        public override void OnPopped()
        {
            // Todo: This is NOT called properly.
            if (this.Generator.LandMesh != null)
            {
                this.Generator.LandMesh.Dispose();
            }
            if (this.Generator.LandIndex != null)
            {
                this.Generator.LandIndex.Dispose();
            }
            //this.Generator.worldData = null;

            if (this.GuiRoot != null)
            {
                this.GuiRoot.DestroyWidget(this.GuiRoot.RootItem);
            }
            base.OnExit();
        }
    }
}