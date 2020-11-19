using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using System.Linq;
using System.Text;
using System;

namespace DwarfCorp.GameStates
{
    public class FactionRow : Gui.Widget
    {
        private Widget TitleLabel;
        private Widget RelationshipLabel;
        private Widget Icon;

        public override void Construct()
        {
            Background = new TileReference("basic", 0);

            var titlebar = AddChild(new Widget()
            {
                InteriorMargin = new Margin(5, 5, 5, 5),
                MinimumSize = new Point(512, 36),
                AutoLayout = AutoLayout.DockTop,
            });

            Icon = titlebar.AddChild(new Widget()
            {
                MaximumSize = new Point(32, 32),
                MinimumSize = new Point(32, 32),
                AutoLayout = AutoLayout.DockLeft,
            });

            TitleLabel = titlebar.AddChild(new Widget()
            {
                TextHorizontalAlign = HorizontalAlign.Right,
                TextVerticalAlign = VerticalAlign.Bottom,
                Font = "font10",
                AutoLayout = AutoLayout.DockLeft
            });

            RelationshipLabel = AddChild(new Widget()
            {
                TextHorizontalAlign = HorizontalAlign.Left,
                TextVerticalAlign = VerticalAlign.Top,
                Font = "font8",
                AutoLayout = AutoLayout.DockTop
            });

            AddChild(new Widget()
            {
                Text = "",
                TextHorizontalAlign = HorizontalAlign.Left,
                TextVerticalAlign = VerticalAlign.Top,
                Font = "font8",
                AutoLayout = AutoLayout.DockTop
            });

            this.Layout();
        }

        public void UpdateRow(OverworldFaction faction, Politics diplomacy, IEnumerable<string> details)
        {
            var sb = new StringBuilder();
            foreach (var detail in details)
                sb.AppendLine(detail);

            Tooltip = "Recent events:\n" + sb.ToString();
            if (sb.ToString() == "")
                Tooltip = "No recent events.";

            TitleLabel.Text = String.Format("{0} ({1}){2}", faction.Name, faction.Race, diplomacy.IsAtWar ? " -- At war!" : "");

            var relation = diplomacy.GetCurrentRelationship();
            var relationshipColor = Color.Black.ToVector4();

            if (relation == Relationship.Loving)
                relationshipColor = GameSettings.Current.Colors.GetColor("Positive", Color.DarkGreen).ToVector4();
            else if (relation == Relationship.Hateful)
                relationshipColor = GameSettings.Current.Colors.GetColor("Negative", Color.Red).ToVector4();

            RelationshipLabel.Text = String.Format("    Relationship: {0}", diplomacy.GetCurrentRelationship());
            RelationshipLabel.TextColor = relationshipColor;

            RelationshipLabel.Invalidate();
            TitleLabel.Invalidate();

            Icon.Background = new TileReference("map-icons", Library.GetRace(faction.Race).HasValue(out var race) ? race.Icon : 0);
            Icon.Invalidate();
        }
    }

    public class FactionWindow : Window
    {
        public Overworld Overworld;
        private WidgetListView List;

        private void SetupRows()
        {
            var factions = Overworld.Natives.Where(f => f.InteractiveFaction && Library.GetRace(f.Race).HasValue(out var race) && race.IsIntelligent);
            
            foreach (var faction in factions)
                List.AddItem(new FactionRow());
        }

        public void UpdateRows()
        {
            var factions = Overworld.Natives.Where(f => f.InteractiveFaction && Library.GetRace(f.Race).HasValue(out var race) && race.IsIntelligent);
            var listItems = List.GetItems();
            var rowIndex = 0;

            foreach (var faction in factions)
            {
                var diplomacy = Overworld.GetPolitics(faction, Overworld.Natives.FirstOrDefault(n => n.Name == "Player"));
                var details = diplomacy.GetEvents().Select(e => string.Format("{0} ({1})", TextGenerator.ToSentenceCase(e.Description), e.Change > 0 ? "+" + e.Change.ToString() : e.Change.ToString()));
                (listItems[rowIndex] as FactionRow).UpdateRow(faction, diplomacy, details);
                rowIndex += 1;
            }
        }

        public override void Construct()
        {
            base.Construct();

            Text = "Factions";

            List = AddChild(new WidgetListView()
            {
                AutoLayout = AutoLayout.DockFill,
                CheckBorder = false,
                Border = "",
                SelectedItemForegroundColor = Color.Black.ToVector4(),
                SelectedItemBackgroundColor = new Vector4(0, 0, 0, 0),
                ItemBackgroundColor2 = new Vector4(0, 0, 0, 0.1f),
                ItemBackgroundColor1 = new Vector4(0, 0, 0, 0),
                ItemHeight = 64
            }) as WidgetListView;

            SetupRows();

            Root.RegisterForUpdate(this);

            OnUpdate += (sender, time) =>
            {
                if (this.Hidden) return;
                UpdateRows();
            };

            this.Layout();
        }
    }
}