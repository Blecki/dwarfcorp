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
    public class WorldGeneratorState : GameState
    {
        private Gui.Root GuiRoot;
        private Gui.Widgets.ProgressBar GenerationProgress;
        private Gui.Widget ZoomedPreview;
        private WorldGeneratorPreview Preview;
        private Gui.Widget StartButton;
        private WorldGenerator Generator;
        private WorldGenerationSettings Settings;
        private bool AutoGenerate;
        
        public WorldGeneratorState(DwarfGame Game, GameStateManager StateManager, WorldGenerationSettings Settings, bool AutoGenerate) :
            base(Game, "NewWorldGeneratorState", StateManager)
        {
            this.AutoGenerate = AutoGenerate;
            this.Settings = Settings;
            if (this.Settings == null)
                this.Settings = new WorldGenerationSettings()
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
            Generator = new WorldGenerator(Settings);
            if (Preview != null) Preview.SetGenerator(Generator);
            Generator.Generate();
            GuiRoot.RootItem.GetChild(0).Text = Settings.Name;
            GuiRoot.RootItem.GetChild(0).Invalidate();
            GuiRoot.MousePointer = new MousePointer("mouse", 15.0f, 16, 17, 18, 19, 20, 21, 22, 23);
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GumSkin);
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
                OnClick = (sender, args) => RestartGeneration()
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
                    if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                        GuiRoot.ShowTooltip(GuiRoot.MousePosition, "Generator is not finished.");
                    else
                    {
                        System.IO.DirectoryInfo worldDirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetGameDirectory() + ProgramData.DirChar + "Worlds" + ProgramData.DirChar + Settings.Name);
                        OverworldFile file = new OverworldFile(Game.GraphicsDevice, Overworld.Map, Settings.Name, Settings.SeaLevel);
                        file.WriteFile(worldDirectory.FullName + ProgramData.DirChar + "world." + (DwarfGame.COMPRESSED_BINARY_SAVES ? OverworldFile.CompressedExtension : OverworldFile.Extension), DwarfGame.COMPRESSED_BINARY_SAVES, DwarfGame.COMPRESSED_BINARY_SAVES);
                        file.SaveScreenshot(worldDirectory.FullName + ProgramData.DirChar + "screenshot.png");
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
                    var advancedSettingsEditor = GuiRoot.ConstructWidget(new Gui.Widgets.WorldGenerationSettingsDialog
                    {
                        Settings = Settings,
                        OnClose = (s) => RestartGeneration()
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
                    if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                        GuiRoot.ShowTooltip(GuiRoot.MousePosition, "World generation is not finished.");
                    else
                    {
                        Overworld.Name = Settings.Name;
                        Settings.ExistingFile = null;
                        Settings.WorldOrigin = Settings.WorldGenerationOrigin;
                        if (Settings.Natives == null || Settings.Natives.Count == 0)
                            Settings.Natives = Generator.NativeCivilizations;

                        StateManager.ClearState();
                        StateManager.PushState(new LoadState(Game, StateManager, Settings));
                    }
                }
            });

            rightPanel.AddChild(new Gui.Widget
            {
                Text = "Colony size",
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1)
            });

            var colonySizeCombo = rightPanel.AddChild(new Gui.Widgets.ComboBox
            {
                AutoLayout = Gui.AutoLayout.DockTop,
                Items = new List<string>(new string[] { "Small", "Medium", "Large" }),
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1),
                OnSelectedIndexChanged = (sender) =>
                {
                    switch ((sender as Gui.Widgets.ComboBox).SelectedItem)
                    {
                        case "Small": Settings.ColonySize = new Point3(4, 1, 4); break;
                        case "Medium": Settings.ColonySize = new Point3(8, 1, 8); break;
                        case "Large": Settings.ColonySize = new Point3(10, 1, 10); break;
                    }

                    var worldSize = Settings.ColonySize.ToVector3() * VoxelConstants.ChunkSizeX / Settings.WorldScale;

                    float w = worldSize.X / 2;
                    float h = worldSize.Z / 2;

                    float clickX = System.Math.Max(System.Math.Min(Settings.WorldGenerationOrigin.X, Settings.Width - w), w);
                    float clickY = System.Math.Max(System.Math.Min(Settings.WorldGenerationOrigin.Y, Settings.Height - h), h);

                    Settings.WorldGenerationOrigin = new Vector2((int)(clickX), (int)(clickY));
                }
            }) as Gui.Widgets.ComboBox;

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
                Items = Embarkment.EmbarkmentLibrary.Select(e => e.Key).ToList(),
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font8",
                OnSelectedIndexChanged = (sender) =>
                {
                    Settings.InitalEmbarkment = Embarkment.EmbarkmentLibrary[(sender as Gui.Widgets.ComboBox).SelectedItem];
                }
            }) as Gui.Widgets.ComboBox;

            ZoomedPreview = rightPanel.AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockBottom,
                OnLayout = (sender) =>
                {
                    var space = System.Math.Min(
                        difficultySelectorCombo.Rect.Width, StartButton.Rect.Top - difficultySelectorCombo.Rect.Bottom - 4);
                    sender.Rect.Height = space;
                    sender.Rect.Width = space;
                    sender.Rect.Y = difficultySelectorCombo.Rect.Bottom + 2;
                    sender.Rect.X = difficultySelectorCombo.Rect.X + 
                        ((difficultySelectorCombo.Rect.Width - space) / 2);
                }
            });


            GenerationProgress = mainPanel.AddChild(new Gui.Widgets.ProgressBar
            {
                AutoLayout = Gui.AutoLayout.DockBottom,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                Font = "font18-outline",
                TextColor = new Vector4(1,1,1,1)
            }) as Gui.Widgets.ProgressBar;

            Preview = mainPanel.AddChild(new WorldGeneratorPreview(Game.GraphicsDevice)
            {
                Border = "border-thin",
                AutoLayout = Gui.AutoLayout.DockFill
            }) as WorldGeneratorPreview;

            GuiRoot.RootItem.Layout();

            difficultySelectorCombo.SelectedIndex = difficultySelectorCombo.Items.IndexOf("Normal");
            colonySizeCombo.SelectedIndex = colonySizeCombo.Items.IndexOf("Medium");

            IsInitialized = true;

            if (AutoGenerate)
                RestartGeneration();
            else // Setup a dummy generator for now.
            {
                Generator = new WorldGenerator(Settings);
                Generator.LoadDummy(
                    new Color[Overworld.Map.GetLength(0) * Overworld.Map.GetLength(1)], 
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
            GenerationProgress.Percentage = Generator.Progress;

            // Enable or disable start button based on Generator state.

            GuiRoot.Update(gameTime.ToGameTime());
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
    }
}