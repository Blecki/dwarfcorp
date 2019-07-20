using System;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.GameStates
{
    public class WorldGeneratorState : GameState
    {
        private Gui.Root GuiRoot;
        private Gui.Widgets.ProgressBar GenerationProgress;
        public WorldGeneratorPreview Preview;
        private OverworldGenerator Generator;
        private Overworld Settings;
        private Gui.Widget RightPanel;
        private Widget MainPanel;
        public Gui.Widgets.CheckBox PoliticsToggle;
        private bool SuppressEnter = false;

        public enum PanelStates
        {
            Generate,
            Launch
        }

        private PanelStates PanelState = PanelStates.Generate;
        
        public WorldGeneratorState(DwarfGame Game, Overworld Settings, PanelStates StartingState) :
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

            Generator = new OverworldGenerator(Settings, true);
            if (Preview != null) Preview.SetGenerator(Generator, Settings);

            GuiRoot.RootItem.GetChild(0).Text = Settings.Name;
            GuiRoot.RootItem.GetChild(0).Invalidate();
            GuiRoot.MousePointer = new MousePointer("mouse", 15.0f, 16, 17, 18, 19, 20, 21, 22, 23);
            Preview.Hidden = true;
            GenerationProgress.Hidden = false;
            PoliticsToggle.Hidden = true;

            Generator.Generate();
        }

        private void SwitchToLaunchPanel()
        {
            if (PanelState != PanelStates.Generate)
                return;

            PanelState = PanelStates.Launch;

            var rect = RightPanel.Rect;
            var parent = RightPanel.Parent;
            RightPanel.Close();

            RightPanel = parent.AddChild(new LaunchPanel(Game, Generator, Settings, this)
            {
                Rect = rect
            });

            RightPanel.Layout();
        }

        public override void OnEnter()
        {
            if (SuppressEnter)
            {
                SuppressEnter = false;
                return;
            }

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
                Padding = new Margin(4, 4, 4, 4),
                InteriorMargin = new Margin(24, 0, 0, 0)
            });

            var rightPanel = MainPanel.AddChild(new Widget
            {
                MinimumSize = new Point(256, 0),
                Padding = new Margin(2, 2, 2, 2),
                AutoLayout = AutoLayout.DockRight
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
                    GameStateManager.ClearState();
                    GameStateManager.PushState(new MainMenuState(Game));
                }
            });

            rightPanel.AddChild(new Widget
            {
                Text = "Factions",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    SuppressEnter = true;
                    GameStateManager.PushState(new FactionViewState(GameState.Game, Settings));
                }
            });

            switch (PanelState)
            {
                case PanelStates.Generate:
                    RightPanel = rightPanel.AddChild(new GenerationPanel(Game, Settings)
                    {
                        RestartGeneration = () => RestartGeneration(),
                        GetGenerator = () => Generator,
                        OnVerified = () =>
                        {
                            SwitchToLaunchPanel();
                        },
                        AutoLayout = Gui.AutoLayout.DockFill,
                    });
                    break;
                case PanelStates.Launch:
                    RightPanel = rightPanel.AddChild(new LaunchPanel(Game, Generator, Settings, this)
                    {
                        AutoLayout = AutoLayout.DockFill,
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

            PoliticsToggle = MainPanel.AddChild(new Gui.Widgets.CheckBox
            {
                Text = "Show Political Boundaries",
                Hidden = true,
                OnLayout = (sender) =>
                {
                    sender.Rect = GenerationProgress.Rect;
                },
                OnCheckStateChange = (sender) =>
                {
                    Preview.ShowPolitics = (sender as Gui.Widgets.CheckBox).CheckState;
                }
            }) as Gui.Widgets.CheckBox;

            Preview = MainPanel.AddChild(new WorldGeneratorPreview(Game.GraphicsDevice)
            {
                Border = "border-thin",
                AutoLayout = Gui.AutoLayout.DockFill,
                Overworld = Settings,
                Hidden = true,
                OnLayout = (sender) =>
                {
                    //sender.Rect = new Rectangle(sender.Rect.X, sender.Rect.Y, sender.Rect.Width, GenerationProgress.Rect.Bottom - sender.Rect.Y);
                },
                OnCellSelectionMade = () =>
                {
                    if (RightPanel is LaunchPanel launch) launch.UpdateCellInfo();
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
                    Generator = new OverworldGenerator(Settings, false);
                    Generator.LoadDummy();
                    Preview.SetGenerator(Generator, Settings);
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

            if (Generator.CurrentState == OverworldGenerator.GenerationState.Finished)
            {
                Preview.Hidden = false;
                GenerationProgress.Hidden = true;
                PoliticsToggle.Hidden = false;
                Preview.Update(gameTime);
                Preview.RenderPreview(Game.GraphicsDevice);
            }

            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();

            if (Generator.CurrentState == OverworldGenerator.GenerationState.Finished)
            {
                Preview.DrawPreview();
                GuiRoot.MousePointer = new MousePointer("mouse", 1, 0);
                if (RightPanel is LaunchPanel launch)
                    launch.DrawPreview();
            }

            GuiRoot.RedrawPopups();
            GuiRoot.Postdraw();
            GuiRoot.DrawMouse();

            base.Render(gameTime);
        }

        public override void OnPopped()
        {
            Preview.Close();
            base.OnPopped();
        }
    }
}