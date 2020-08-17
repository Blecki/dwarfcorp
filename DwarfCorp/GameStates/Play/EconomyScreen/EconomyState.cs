using System.Collections.Generic;
using System.Linq;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.Gui;

namespace DwarfCorp.GameStates
{
    public class EconomyState : GameState
    {
        private Gui.Root GuiRoot;
        private WorldManager World;
        private Gui.Widgets.TabPanel TabPanel;

        public EconomyState(DwarfGame Game, WorldManager World) :
            base(Game)
        {
            this.World = World;
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
            
            var mainPanel = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = GuiRoot.RenderData.VirtualScreen,
                Padding = new Margin(4, 4, 4, 4),
                Transparent = true
            });

            mainPanel.AddChild(new Gui.Widgets.Button
            {
                Text = "Close",
                Font = "font16",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    GameStateManager.PopState();
                },
                AutoLayout = AutoLayout.FloatBottomRight
            });

            TabPanel = mainPanel.AddChild(new Gui.Widgets.TabPanel
            {
                AutoLayout = AutoLayout.DockFill,
                TextSize = 1,
                SelectedTabColor = new Vector4(1, 0, 0, 1),
                OnLayout = (sender) => sender.Rect.Height -= 36 // Keep it from overlapping bottom buttons.
            }) as Gui.Widgets.TabPanel;

            var employeePanel = TabPanel.AddTab("Employees", new Gui.Widgets.EmployeePanel
            {
                Font = "font10",
                Border = "border-thin",
                Padding = new Margin(4, 4, 0, 0),
                World = World,
            });

            var financePanel = TabPanel.AddTab("Finance", new FinancePanel
            {
                Border = "border-thin",
                Padding = new Margin(4, 4, 0, 0),
                Faction = World.PlayerFaction,
                World = World
            });

            var policyPanel = TabPanel.AddTab("Policy", new PolicyPanel
            {
                Faction = World.PlayerFaction,
                World = World
            });

            TabPanel.SelectedTab = 0;
            
            GuiRoot.RootItem.Layout();

            IsInitialized = true;
            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            World.Tutorial("economy");

            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                }
            }

            SoundManager.Update(gameTime, World.Renderer.Camera, World.Time);
            World.TutorialManager.Update(GuiRoot);

            GuiRoot.Update(gameTime.ToRealTime());
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}