using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using System;

namespace DwarfCorp.Play.EmployeeInfo
{
    public class LevelUpPanel : Widget
    {
        public Func<CreatureAI> FetchEmployee = null;
        public CreatureAI Employee
        {
            get { return FetchEmployee?.Invoke(); }
        }

        private Widget CurrentLevel;
        private Widget AvailableExperience;
        private Widget StatCost;
        private Widget StatDexterity;
        private Widget StatStrength;
        private Widget StatWisdom;
        private Widget StatCharisma;
        private Widget StatConstitution;
        private Widget StatIntelligence;

        private Widget StatsPanel;
        private Widget ActivateButton;

        public void ResetHiddenStatus()
        {
            ActivateButton.Hidden = false;
            StatsPanel.Hidden = true;
            ActivateButton.Invalidate();
        }

        private void AdjustStat(String StatName)
        {
            if (Employee == null) return;
            var currentLevel = Employee.Stats.GetCurrentLevel();
            var cost = CreatureStats.GetLevelUpCost(currentLevel);
            if (cost > Employee.Stats.XP) return;
            var baseStats = Employee.Stats.FindAdjustment("base stats");
            if (baseStats == null) return;
            var property = baseStats.GetType().GetField(StatName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (property == null) return;
            var value = property.GetValue(baseStats) as float?;
            if (!value.HasValue) return;
            property.SetValue(baseStats, value.Value + 1);
            Employee.Stats.XP -= cost;

            Employee.Creature.AddThought("I levelled up!", new TimeSpan(4, 0, 0), 50);
        }

        public override void Construct()
        {
            Font = "font8";

            var topBar = AddChild(new Gui.Widgets.AutoGridPanel
            {
                Rows = 1,
                Columns = 3,
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 10)
            });

            CurrentLevel = topBar.AddChild(new Widget());
            AvailableExperience = topBar.AddChild(new Widget());
            StatCost = topBar.AddChild(new Widget());

            StatsPanel = AddChild(new Gui.Widgets.AutoGridPanel
            {
                Rows = 1,
                Columns = 6,
                AutoLayout = AutoLayout.DockFill,
                Hidden = true
            });

            StatStrength = StatsPanel.AddChild(new Widget
            {
                Border = "border-thin",
                Padding = new Margin(0, 0, 2, 2),
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                OnClick = (sender, args) => AdjustStat("Strength")
            });

            StatDexterity = StatsPanel.AddChild(new Widget
            {
                Border = "border-thin",
                Padding = new Margin(0, 0, 2, 2),
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                OnClick = (sender, args) => AdjustStat("Dexterity")
            });

            StatConstitution = StatsPanel.AddChild(new Widget
            {
                Border = "border-thin",
                Padding = new Margin(0, 0, 2, 2),
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                OnClick = (sender, args) => AdjustStat("Constitution")
            });

            StatIntelligence = StatsPanel.AddChild(new Widget
            {
                Border = "border-thin",
                Padding = new Margin(0, 0, 2, 2),
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                OnClick = (sender, args) => AdjustStat("Intelligence")
            });

            StatWisdom = StatsPanel.AddChild(new Widget
            {
                Border = "border-thin",
                Padding = new Margin(0, 0, 2, 2),
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                OnClick = (sender, args) => AdjustStat("Wisdom")
            });

            StatCharisma = StatsPanel.AddChild(new Widget
            {
                Border = "border-thin",
                Padding = new Margin(0, 0, 2, 2),
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                OnClick = (sender, args) => AdjustStat("Charisma")
            });

            ActivateButton = AddChild(new Widget
            {
                OnLayout = (sender) => sender.Rect = StatsPanel.Rect,
                Border = "border-thin",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                OnClick = (sender, args) =>
                {
                    ActivateButton.Hidden = true;
                    StatsPanel.Hidden = false;
                    StatsPanel.Invalidate();
                },
                Text = "Click to Level Up Stats"
            });

            base.Construct();
        }

        protected override Gui.Mesh Redraw()
        {
            // Set values from CreatureAI
            if (Employee != null && !Employee.IsDead)
            {
                Hidden = false;

                CurrentLevel.Text = String.Format("Level: {0}", Employee.Stats.GetCurrentLevel());
                AvailableExperience.Text = String.Format("Available: {0}", Employee.Stats.XP);
                StatCost.Text = String.Format("Cost: {0}", CreatureStats.GetLevelUpCost(Employee.Stats.GetCurrentLevel()));

                var baseStats = Employee.Stats.FindAdjustment("base stats");

                StatStrength.Text = "STR " + baseStats.Strength.ToString();
                StatDexterity.Text = "DEX " + baseStats.Dexterity.ToString();
                StatConstitution.Text = "CON " + baseStats.Constitution.ToString();
                StatIntelligence.Text = "INT " + baseStats.Intelligence.ToString();
                StatWisdom.Text = "WIS " + baseStats.Wisdom.ToString();
                StatCharisma.Text = "CHA " + baseStats.Charisma.ToString();
            }
            else
                Hidden = true;

            foreach (var child in Children)
                child.Invalidate();

            return base.Redraw();
        }
    }
}
