using System.Collections.Generic;
using System.Linq;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.GameStates
{
    // Todo: Make this use wait cursor while generating.
    public class WorldGeneratorState : GameState
    {
        private Gum.Root GuiRoot;
        private Gum.Widgets.ProgressBar GenerationProgress;
        private Gum.Widget ZoomedPreview;
        private WorldGeneratorPreview Preview;
        private Gum.Widget StartButton;
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
                    Name = WorldSetupState.GetRandomWorldName(),
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
            Generator.Generate(Game.GraphicsDevice);
            GuiRoot.RootItem.GetChild(0).Text = Settings.Name;
            GuiRoot.RootItem.GetChild(0).Invalidate();
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            if (IsInitialized) // Must have come here from advanced settings editor.
            {
                RestartGeneration();
                return;
            }

            GuiRoot = new Gum.Root(Gum.Root.MinimumSize, DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);

            var mainPanel = GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                Rect = GuiRoot.VirtualScreen,
                Border = "border-fancy",
                Text = Settings.Name,
                Font = "font-hires",
                TextColor = new Vector4(0, 0, 0, 1),
                Padding = new Gum.Margin(4, 4, 4, 4),
                InteriorMargin = new Gum.Margin(24, 0, 0, 0),
            });

            var rightPanel = mainPanel.AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockRight,
                MinimumSize = new Point(256, 0),
                Padding = new Gum.Margin(2,2,2,2)
            });

            rightPanel.AddChild(new Gum.Widget
            {
                Text = "Regenerate",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font-hires",
                AutoLayout = Gum.AutoLayout.DockTop,
                OnClick = (sender, args) => RestartGeneration()
            });

            rightPanel.AddChild(new Gum.Widget
            {
                Text = "Save World",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font-hires",
                AutoLayout = Gum.AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                        GuiRoot.ShowTooltip(GuiRoot.MousePosition, "Generator is not finished.");
                    else
                    {
                        System.IO.DirectoryInfo worldDirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetGameDirectory() + ProgramData.DirChar + "Worlds" + ProgramData.DirChar + Settings.Name);
                        OverworldFile file = new OverworldFile(Game.GraphicsDevice, Overworld.Map, Settings.Name, Settings.SeaLevel);
                        file.WriteFile(worldDirectory.FullName + ProgramData.DirChar + "world." + OverworldFile.CompressedExtension, true, true);
                        file.SaveScreenshot(worldDirectory.FullName + ProgramData.DirChar + "screenshot.png");
                        GuiRoot.ShowPopup(GuiRoot.ConstructWidget(new NewGui.Popup
                        {
                            Text = "File saved."
                        }));
                    }
                }
            });

            rightPanel.AddChild(new Gum.Widget
            {
                Text = "Advanced",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font-hires",
                AutoLayout = Gum.AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    var advancedSettingsEditor = new WorldSetupState(Game, StateManager, Settings);
                    StateManager.PushState(advancedSettingsEditor);
                }
            });

            rightPanel.AddChild(new Gum.Widget
            {
                Text = "Back",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font-hires",
                AutoLayout = Gum.AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    Generator.Abort();
                    StateManager.PopState();
                }
            });

            StartButton = rightPanel.AddChild(new Gum.Widget
            {
                Text = "Start Game",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font-hires",
                AutoLayout = Gum.AutoLayout.DockBottom,
                OnClick = (sender, args) =>
                {
                    if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                        GuiRoot.ShowTooltip(GuiRoot.MousePosition, "World generation is not finished.");
                    else
                    {
                        Overworld.Name = Settings.Name;
                        Settings.ExistingFile = null;
                        Settings.WorldOrigin = Settings.WorldGenerationOrigin;
                        Settings.Natives = Generator.NativeCivilizations;

                        StateManager.ClearState();
                        StateManager.PushState(new LoadState(Game, StateManager, Settings));
                    }
                }
            });

            rightPanel.AddChild(new Gum.Widget
            {
                Text = "Colony size",
                AutoLayout = Gum.AutoLayout.DockTop,
                Font = "font",
                TextColor = new Vector4(0, 0, 0, 1)
            });

            var colonySizeCombo = rightPanel.AddChild(new Gum.Widgets.ComboBox
            {
                AutoLayout = Gum.AutoLayout.DockTop,
                Items = new List<string>(new string[] { "Tiny", "Small", "Medium", "Large", "Huge" }),
                Font = "font",
                TextColor = new Vector4(0, 0, 0, 1),
                OnSelectedIndexChanged = (sender) =>
                {
                    switch ((sender as Gum.Widgets.ComboBox).SelectedItem)
                    {
                        case "Tiny": Settings.ColonySize = new Point3(4, 1, 4); break;
                        case "Small": Settings.ColonySize = new Point3(8, 1, 8); break;
                        case "Medium": Settings.ColonySize = new Point3(10, 1, 10); break;
                        case "Large": Settings.ColonySize = new Point3(16, 1, 16); break;
                        case "Huge": Settings.ColonySize = new Point3(24, 1, 24); break;
                    }

                    float w = Settings.ColonySize.X * Settings.WorldScale;
                    float h = Settings.ColonySize.Z * Settings.WorldScale;
                    float clickX = System.Math.Max(System.Math.Min(Settings.WorldGenerationOrigin.X, Settings.Width - w - 1), w + 1);
                    float clickY = System.Math.Max(System.Math.Min(Settings.WorldGenerationOrigin.Y, Settings.Height - h - 1), h + 1);

                    Settings.WorldGenerationOrigin = new Vector2((int)(clickX), (int)(clickY));
                }
            }) as Gum.Widgets.ComboBox;

            rightPanel.AddChild(new Gum.Widget
            {
                Text = "Difficulty",
                AutoLayout = Gum.AutoLayout.DockTop,
                Font = "font",
                TextColor = new Vector4(0,0,0,1)
            });

            var difficultySelectorCombo = rightPanel.AddChild(new Gum.Widgets.ComboBox
            {
                AutoLayout = Gum.AutoLayout.DockTop,
                Items = Embarkment.EmbarkmentLibrary.Select(e => e.Key).ToList(),
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font",
                OnSelectedIndexChanged = (sender) =>
                {
                    Settings.InitalEmbarkment = Embarkment.EmbarkmentLibrary[(sender as Gum.Widgets.ComboBox).SelectedItem];
                }
            }) as Gum.Widgets.ComboBox;

            ZoomedPreview = rightPanel.AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockBottom,
                OnLayout = (sender) =>
                {
                    var space = System.Math.Min(difficultySelectorCombo.Rect.Width, StartButton.Rect.Top - difficultySelectorCombo.Rect.Bottom - 4);
                    sender.Rect.Height = space;
                    sender.Rect.Width = space;
                    sender.Rect.Y = difficultySelectorCombo.Rect.Bottom + 2;
                    sender.Rect.X = difficultySelectorCombo.Rect.X + 
                        ((difficultySelectorCombo.Rect.Width - space) / 2);
                }
            });


            GenerationProgress = mainPanel.AddChild(new Gum.Widgets.ProgressBar
            {
                AutoLayout = Gum.AutoLayout.DockBottom,
                TextHorizontalAlign = Gum.HorizontalAlign.Center,
                TextVerticalAlign = Gum.VerticalAlign.Center,
                Font = "outline-font",
                TextColor = new Vector4(1,1,1,1)
            }) as Gum.Widgets.ProgressBar;

            Preview = mainPanel.AddChild(new WorldGeneratorPreview(Game.GraphicsDevice)
            {
                Border = "border-thin",
                AutoLayout = Gum.AutoLayout.DockFill
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

            Preview.PreparePreview();
        }

        public override void Render(DwarfTime gameTime)
        {
            var mouse = GuiRoot.MousePointer;
            GuiRoot.MousePointer = null;
            GuiRoot.Draw();

            if (Generator.CurrentState == WorldGenerator.GenerationState.Finished)
            {
                Preview.DrawPreview();
                GuiRoot.DrawMesh(
                        Gum.Mesh.Quad()
                        .Scale(ZoomedPreview.Rect.Width, ZoomedPreview.Rect.Height)
                        .Translate(ZoomedPreview.Rect.X, ZoomedPreview.Rect.Y)
                        .Texture(Preview.ZoomedPreviewMatrix),
                        Preview.PreviewTexture);
            }

            // This is a serious hack.
            GuiRoot.RedrawPopups();

            GuiRoot.MousePointer = mouse;
            GuiRoot.DrawMouse();
            base.Render(gameTime);
        }
    }
}