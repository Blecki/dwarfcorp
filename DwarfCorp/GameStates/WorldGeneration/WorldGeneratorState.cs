using System;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.GameStates
{
    public class WorldGeneratorState : GameState
    {
        private Gui.Root GuiRoot;
        private Gui.Widgets.ProgressBar GenerationProgress;
        private WorldGeneratorPreview Preview;
        private WorldGenerator Generator;
        private OverworldGenerationSettings Settings;
        private Gui.Widget RightPanel;
        private Widget MainPanel;

        public enum PanelStates
        {
            Generate,
            Launch
        }

        private PanelStates PanelState = PanelStates.Generate;
        
        public WorldGeneratorState(DwarfGame Game, OverworldGenerationSettings Settings, PanelStates StartingState) :
            base(Game)
        {
            this.PanelState = StartingState;
            this.Settings = Settings;
        }       

        private void RestartGeneration()
        {
            if (Generator != null)
                Generator.Abort();

            if (Settings.Natives != null)
                Settings.Natives.Clear();

            Generator = new WorldGenerator(Settings, true);
            if (Preview != null) Preview.SetGenerator(Generator);

            GuiRoot.RootItem.GetChild(0).Text = Settings.Name;
            GuiRoot.RootItem.GetChild(0).Invalidate();
            GuiRoot.MousePointer = new MousePointer("mouse", 15.0f, 16, 17, 18, 19, 20, 21, 22, 23);
            Preview.Hidden = true;
            GenerationProgress.Hidden = false;

            Generator.Generate();
        }

        private void SwitchToLaunchPanel()
        {
            if (PanelState != PanelStates.Generate)
                throw new InvalidProgramException();

            PanelState = PanelStates.Launch;

            var rect = RightPanel.Rect;
            MainPanel.RemoveChild(RightPanel);

            RightPanel = MainPanel.AddChild(new LaunchPanel(Game, Generator, Settings)
            {
                Rect = rect
            });

            RightPanel.Layout();
        }

        public override void OnEnter()
        {
            DwarfGame.GumInputMapper.GetInputQueue();

            #region Setup GUI
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

            switch (PanelState)
            {
                case PanelStates.Generate:
                    RightPanel = MainPanel.AddChild(new GenerationPanel(Game, Settings)
                    {
                        RestartGeneration = () => RestartGeneration(),
                        GetGenerator = () => Generator,
                        OnVerified = () =>
                        {
                            SwitchToLaunchPanel();
                        },
                        AutoLayout = Gui.AutoLayout.DockRight,
                        MinimumSize = new Point(256, 0),
                        Padding = new Gui.Margin(2, 2, 2, 2)
                    });
                    break;
                case PanelStates.Launch:
                    RightPanel = MainPanel.AddChild(new LaunchPanel(Game, Generator, Settings)
                    {
                        AutoLayout = AutoLayout.DockRight,
                        MinimumSize = new Point(256, 0),
                        Padding = new Gui.Margin(2, 2, 2, 2)
                    });
                    break;
            }

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
            #endregion

            switch (PanelState)
            {
                case PanelStates.Generate:
                    RestartGeneration();
                    break;

                case PanelStates.Launch:
                    // Setup a dummy generator.
                    Generator = new WorldGenerator(Settings, false);
                    Generator.LoadDummy(new Color[Settings.Overworld.Map.GetLength(0) * Settings.Overworld.Map.GetLength(1)], Game.GraphicsDevice);
                    Preview.SetGenerator(Generator);
                    (RightPanel as LaunchPanel).Generator = Generator;
                    break;
            }

            IsInitialized = true;
            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
                GuiRoot.HandleInput(@event.Message, @event.Args);

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

            Preview.PreparePreview(Game.GraphicsDevice);
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();

            if (Generator.CurrentState == WorldGenerator.GenerationState.Finished)
            {
                Preview.DrawPreview();
                GuiRoot.MousePointer = new MousePointer("mouse", 1, 0);
            }

            GuiRoot.RedrawPopups();
            GuiRoot.DrawMouse();

            base.Render(gameTime);
        }

        public override void OnPopped()
        {
            if (this.Generator.LandMesh != null)
                this.Generator.LandMesh.Dispose();
            if (this.Generator.LandIndex != null)
                this.Generator.LandIndex.Dispose();

            base.OnPopped();
        }
    }
}