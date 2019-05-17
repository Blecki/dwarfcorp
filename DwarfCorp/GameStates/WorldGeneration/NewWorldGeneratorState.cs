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
        private WorldGeneratorPreview Preview;
        private WorldGenerator Generator;
        private OverworldGenerationSettings Settings;
        private bool AutoGenerate;
        private Gui.Widget RightPanel;
        private Widget MainPanel;
        
        public NewWorldGeneratorState(DwarfGame Game, GameStateManager StateManager, CompanyInformation Company, bool AutoGenerate) :
            base(Game, "NewWorldGeneratorState", StateManager)
        {
            this.AutoGenerate = AutoGenerate;

            this.Settings = new OverworldGenerationSettings()
                {
                    Company = Company,
                    Width = 128,
                    Height = 128,
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

            Preview.Hidden = true;
            GenerationProgress.Hidden = false;
        }

        private void SwitchToLaunchPanel()
        {
            var rect = RightPanel.Rect;
            MainPanel.RemoveChild(RightPanel);

            RightPanel = MainPanel.AddChild(new LaunchPanel(Game, StateManager, Generator, Settings)
            {
                Rect = rect
            });
            RightPanel.Layout();
        }

        public override void OnEnter()
        {
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new MousePointer("mouse", 15.0f, 16, 17, 18, 19, 20, 21, 22, 23);

            MainPanel = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = GuiRoot.RenderData.VirtualScreen,
                Border = "border-fancy",
                Text = Settings.Name,
                Font = "font16",
                TextColor = new Vector4(0, 0, 0, 1),
                Padding = new Gui.Margin(4, 4, 4, 4),
                InteriorMargin = new Gui.Margin(24, 0, 0, 0),
            });

            RightPanel = MainPanel.AddChild(new GenerationPanel(Game, StateManager, Settings)
            {
                RestartGeneration = () => RestartGeneration(),
                GetGenerator = () => Generator,
                OnVerified = () =>
                {
                    SwitchToLaunchPanel();
                },
                AutoLayout = Gui.AutoLayout.DockRight,
                MinimumSize = new Point(256, 0),
                Padding = new Gui.Margin(2,2,2,2)
            });

            GenerationProgress = MainPanel.AddChild(new Gui.Widgets.ProgressBar
            {
                AutoLayout = Gui.AutoLayout.DockBottom,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                Font = "font10",
                TextColor = new Vector4(1,1,1,1)                
            }) as Gui.Widgets.ProgressBar;

            Preview = MainPanel.AddChild(new WorldGeneratorPreview(Game.GraphicsDevice)
            {
                Border = "border-thin",
                AutoLayout = Gui.AutoLayout.DockFill,
                Overworld = Settings.Overworld,
                Hidden = true,
                OnLayout = (sender) =>
                {
                    sender.Rect = new Rectangle(sender.Rect.X, sender.Rect.Y, sender.Rect.Width, GenerationProgress.Rect.Bottom - sender.Rect.Y);
                }
            }) as WorldGeneratorPreview;

            GuiRoot.RootItem.Layout();

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
            {
                Preview.Hidden = false;
                GenerationProgress.Hidden = true;
                Preview.Update();
            }

            base.Update(gameTime);

            Preview.PreparePreview(StateManager.Game.GraphicsDevice);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();

            if (Generator.CurrentState == WorldGenerator.GenerationState.Finished)
            {
                Preview.DrawPreview();
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