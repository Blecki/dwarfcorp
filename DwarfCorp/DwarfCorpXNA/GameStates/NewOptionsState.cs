using System.Collections.Generic;
using System.Linq;
using DwarfCorpCore;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum;
using Gum.Widgets;

namespace DwarfCorp.GameStates
{
    public class NewOptionsState : GameState
    {
        private Gum.Root GuiRoot;
        private bool HasChanges = false;

        private Gum.Widget MainPanel;
        private Gum.Widgets.TabPanel TabPanel;

        private HorizontalFloatSlider MoveSpeed;
        private HorizontalFloatSlider ZoomSpeed;
        private CheckBox InvertZoom;
        private CheckBox EdgeScrolling;
        private CheckBox FogOfWar;
        private CheckBox PlayIntro;

        public NewOptionsState(DwarfGame Game, GameStateManager StateManager) :
            base(Game, "NewOptionsState", StateManager)
        { }


        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInput.GetInputQueue();

            GuiRoot = new Gum.Root(new Point(640, 480), DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);

            // CONSTRUCT GUI HERE...
            MainPanel = GuiRoot.RootItem.AddChild(new Gum.Widget
                {
                    Rect = GuiRoot.VirtualScreen,
                    Background = new TileReference("basic", 0),
                    Padding = 4
                });

            MainPanel.AddChild(new Widget
            {
                Text = "CLOSE",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                TextSize = 2,
                OnClick = (sender, args) =>
                {
                    // If changes, prompt before closing.

                    StateManager.PopState();
                },
                AutoLayout = AutoLayout.FloatBottomRight
            });

            MainPanel.AddChild(new Widget
            {
                Text = "APPLY",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                TextSize = 2,
                OnClick = (sender, args) =>
                {
                    ApplySettings();
                },
                AutoLayout = AutoLayout.FloatBottomRight,
                OnLayout = s => s.Rect.X -= 128 // Hack to keep it from floating over the other button.
            });

            TabPanel = MainPanel.AddChild(new Gum.Widgets.TabPanel
            {
                AutoLayout = AutoLayout.DockFill,
                TextSize = 4,
                OnLayout = (sender) => sender.Rect.Height -= 36 // Keep it from overlapping bottom buttons.
            }) as Gum.Widgets.TabPanel;

            CreateGameplayTab();

            TabPanel.SelectedTab = 0;

            GuiRoot.RootItem.Layout();

            // Must be true or Render will not be called.
            IsInitialized = true;

            base.OnEnter();
        }

        private Widget LabelAndDockWidget(string Label, Widget Widget)
        {
            var r = GuiRoot.ConstructWidget(new Widget
            {
                MinimumSize = new Point(0, 20),
                AutoLayout = AutoLayout.DockTop
            });

            r.AddChild(new Widget
            {
                Text = Label,
                TextSize = 2,
                AutoLayout = AutoLayout.DockLeft
            });

            Widget.AutoLayout = AutoLayout.DockFill;
            r.AddChild(Widget);

            return r;
        }

        private void BeforeTextChangeInteger(Widget Sender, Gum.Widgets.EditableTextField.BeforeTextChangeEventArgs args)
        {
            Sender.TextColor = new Vector4(0, 0, 0, 1);
            int value = 0;
            if (!int.TryParse(args.NewText, out value))
                Sender.TextColor = new Vector4(1, 0, 0, 1);
        }

        private void BeforeTextChangeFloat(Widget Sender, Gum.Widgets.EditableTextField.BeforeTextChangeEventArgs args)
        {
            Sender.TextColor = new Vector4(0, 0, 0, 1);
            float value = 0;
            if (!float.TryParse(args.NewText, out value))
                Sender.TextColor = new Vector4(1, 0, 0, 1);
        }

        private System.Action<Widget, EditableTextField.BeforeTextChangeEventArgs> 
            RangedBeforeTextChangeFloat(float Min, float Max)
        {
            return (sender, args) =>
                {
                    sender.TextColor = new Vector4(0, 0, 0, 1);
                    float value = 0;
                    if (!float.TryParse(args.NewText, out value))
                        sender.TextColor = new Vector4(1, 0, 0, 1);
                    else
                    {
                        if (value < Min) sender.TextColor = new Vector4(1, 0, 0, 1);
                        if (value > Max) sender.TextColor = new Vector4(1, 0, 0, 1);
                    }
                };
        }

        private void CreateGameplayTab()
        {
            var panel = TabPanel.AddTab("GAMEPLAY", new Widget
                {
                    Border = "border-thin"
                });

            MoveSpeed = panel.AddChild(LabelAndDockWidget("Camera Move Speed", new HorizontalFloatSlider
                {
                    ScrollArea = 20,
                    OnScroll = OnItemChanged
                })).GetChild(1) as HorizontalFloatSlider;

            ZoomSpeed = panel.AddChild(LabelAndDockWidget("Camera Zoom Speed", new HorizontalFloatSlider
            {
                ScrollArea = 2,
                OnScroll = OnItemChanged
            })).GetChild(1) as HorizontalFloatSlider;

            InvertZoom = panel.AddChild(new CheckBox
                {
                    Text = "Invert Zoom",
                    TextSize = 2,
                    OnCheckStateChange = OnItemChanged,
                    AutoLayout = AutoLayout.DockTop
                }) as CheckBox;

            EdgeScrolling = panel.AddChild(new CheckBox
            {
                Text = "Edge Scrolling",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            PlayIntro = panel.AddChild(new CheckBox
            {
                Text = "Play Intro",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            FogOfWar = panel.AddChild(new CheckBox
            {
                Text = "Fog Of War",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;
        }

        private void OnItemChanged(Gum.Widget Sender)
        {
            HasChanges = true;
        }

        private void ApplySettings()
        {
            // Copy all the states from widgets to game settings.

            HasChanges = false;
        }

        private void LoadSettings()
        {
            // Set all the widget states based on game settings.

            HasChanges = false;
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInput.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                }
            }

            GuiRoot.Update(gameTime.ToGameTime());
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}