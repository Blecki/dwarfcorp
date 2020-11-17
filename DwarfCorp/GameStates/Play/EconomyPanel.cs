using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace DwarfCorp.GameStates
{
    public class EconomyPanel : Gui.Widgets.Window
    {
        public WorldManager World { get; set; }

        public override void Construct()
        {
            Text = "Economy";

            var tabPanel = AddChild(new Gui.Widgets.TabPanel
            {
                AutoLayout = AutoLayout.DockFill,
                TextSize = 1,
                Font = "font10",
                ButtonFont = "font10",
                SelectedTabColor = new Vector4(1, 0, 0, 1)
            }) as Gui.Widgets.TabPanel;

            var employeePanel = tabPanel.AddTab("Employees", new Gui.Widgets.EmployeePanel
            {
                Font = "font10",
                Padding = new Margin(4, 4, 0, 0),
                World = World,
            });

            var financePanel = tabPanel.AddTab("Finance", new FinancePanel
            {
                Padding = new Margin(4, 4, 0, 0),
                Faction = World.PlayerFaction,
                World = World
            });

            var policyPanel = tabPanel.AddTab("Policy", new PolicyPanel
            {
                Faction = World.PlayerFaction,
                World = World
            });

            tabPanel.SelectedTab = 0;

            Layout();
            base.Construct();
        }
    }
}
