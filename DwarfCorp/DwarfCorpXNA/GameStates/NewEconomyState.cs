using System.Collections.Generic;
using System.Linq;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Gum;

namespace DwarfCorp.GameStates
{
    public class NewEconomyState : GameState
    {
        private Gum.Root GuiRoot;
        private WorldManager World;

        public NewEconomyState(DwarfGame Game, GameStateManager StateManager, WorldManager World) :
            base(Game, "GuiStateTemplate", StateManager)
        {
            this.World = World;
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gum.Root(DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);
            GuiRoot.SetMouseOverlay(null, 0);
            var mainPanel = GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                Rect = GuiRoot.RenderData.VirtualScreen,
                Padding = new Margin(4, 4, 4, 4),
                Transparent = true
            });

            mainPanel.AddChild(new Gum.Widgets.Button
            {
                Text = "Close",
                Font = "font-hires",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                        StateManager.PopState();
                },
                AutoLayout = AutoLayout.FloatBottomRight
            });

            var tabPanel = mainPanel.AddChild(new Gum.Widgets.TabPanel
            {
                AutoLayout = AutoLayout.DockFill,
                TextSize = 1,
                SelectedTabColor = new Vector4(1, 0, 0, 1),
                OnLayout = (sender) => sender.Rect.Height -= 36 // Keep it from overlapping bottom buttons.
            }) as Gum.Widgets.TabPanel;

            var employeePanel = tabPanel.AddTab("Employees", new NewGui.EmployeePanel
            {
                Border = "border-thin",
                Padding = new Margin(4, 4, 0, 0),
                Faction = World.PlayerFaction
            });
            

            //var financePanel = tabPanel.AddTab("Finance", new NewGui.FinancePanel
            //{
            //    Border = "border-thin",
            //    Padding = new Margin(4, 4, 0, 0),
            //    Economy = World.PlayerEconomy
            //});

            tabPanel.AddTab("Available Goals", new NewGui.GoalPanel
            {
                GoalSource = World.GoalManager.EnumerateGoals().Where(g =>
                    g.State == Goals.GoalState.Available),
                World = World
            });

            tabPanel.AddTab("Active Goals", new NewGui.GoalPanel
            {
                GoalSource = World.GoalManager.EnumerateGoals().Where(g =>
                    g.State == Goals.GoalState.Active && g.GoalType != Goals.GoalTypes.Achievement)
            });

            tabPanel.AddTab("Completed Goals", new NewGui.GoalPanel
            {
                GoalSource = World.GoalManager.EnumerateGoals().Where(g =>
                    g.State == Goals.GoalState.Complete)
            });
            
            tabPanel.SelectedTab = 0;
            
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