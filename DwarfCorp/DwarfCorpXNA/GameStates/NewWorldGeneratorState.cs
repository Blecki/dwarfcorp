using System.Collections.Generic;
using System.Linq;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.GameStates
{
    public class NewWorldGeneratorState : GameState
    {
        private Gum.Root GuiRoot;
        private Gum.Widgets.ProgressBar GenerationProgress;
        private WorldGeneratorPreview Preview;
        private Gum.Widget StartButton;
        private WorldGenerator Generator;
        private WorldGenerationSettings Settings;
        
        public NewWorldGeneratorState(DwarfGame Game, GameStateManager StateManager) :
            base(Game, "NewWorldGeneratorState", StateManager)
        {
            Settings = new WorldGenerationSettings()
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
            Generator = new WorldGenerator(Settings);
            Generator.Generate(Game.GraphicsDevice);
        }       

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            if (IsInitialized) return;

            GuiRoot = new Gum.Root(Gum.Root.MinimumSize, DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);

            var mainPanel = GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                Rect = GuiRoot.VirtualScreen,
                Border = "border-fancy",
                Text = "World Name Here",
                Padding = new Gum.Margin(4, 4, 4, 4),
                InteriorMargin = new Gum.Margin(24, 0, 0, 0),
                TextSize = 2
            });

            var rightPanel = mainPanel.AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockRight,
                MinimumSize = new Point(256, 0)
            });

            rightPanel.AddChild(new Gum.Widget
            {
                Text = "Regenerate",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "outline-font",
                AutoLayout = Gum.AutoLayout.DockTop
            });

            rightPanel.AddChild(new Gum.Widget
            {
                Text = "Save World",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "outline-font",
                AutoLayout = Gum.AutoLayout.DockTop
            });

            rightPanel.AddChild(new Gum.Widget
            {
                Text = "Advanced",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "outline-font",
                AutoLayout = Gum.AutoLayout.DockTop
            });

            StartButton = rightPanel.AddChild(new Gum.Widget
            {
                Text = "Start Game",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "outline-font",
                AutoLayout = Gum.AutoLayout.DockBottom
            });

            rightPanel.AddChild(new Gum.Widget
            {
                Text = "Colony size",
                Font = "outline-font",
                AutoLayout = Gum.AutoLayout.DockTop
            });

            rightPanel.AddChild(new Gum.Widgets.ComboBox
            {
                AutoLayout = Gum.AutoLayout.DockTop,
                Items = new List<string>(new string[] { "Small", "Medium", "Large" })
            });

            rightPanel.AddChild(new Gum.Widget
            {
                Text = "Difficulty",
                Font = "outline-font",
                AutoLayout = Gum.AutoLayout.DockTop
            });

            rightPanel.AddChild(new Gum.Widgets.ComboBox
            {
                AutoLayout = Gum.AutoLayout.DockTop,
                Items = new List<string>(new string[] { "Small", "Medium", "Large" })
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

            Preview.SetGenerator(Generator);

            GuiRoot.RootItem.Layout();

            IsInitialized = true;

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
            Preview.Update();
            base.Update(gameTime);

            Preview.PreparePreview();
        }

        public override void Render(DwarfTime gameTime)
        {
            var mouse = GuiRoot.MousePointer;
            GuiRoot.MousePointer = null;
            GuiRoot.Draw();
            Preview.DrawPreview();
            GuiRoot.MousePointer = mouse;
            GuiRoot.DrawMouse();
            base.Render(gameTime);
        }
    }

}