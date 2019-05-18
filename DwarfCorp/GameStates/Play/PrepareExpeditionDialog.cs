using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using System.Linq;
using System.Text;
using System;
using DwarfCorp.Scripting.Adventure;

namespace DwarfCorp.GameStates
{
    public class PrepareExpeditionDialog : Widget
    {
        public WorldManager World;
        public Faction Faction;
        public Faction DestinationFaction;
        public Action<PrepareExpeditionDialog> OnProceed;
        public Action OnCanceled;
        public Adventure SelectedAdventure = null;

        public override void Construct()
        {
            Border = "border-one";
            Text = String.Format("Send an Expedition to the {0} at {1}", DestinationFaction.Race.Plural, DestinationFaction.Name);
            Font = "font16";
            InteriorMargin = new Margin(32, 5, 5, 5);
            var rect = GetDrawableInterior();
            AddChild(new Widget()
            {
                Font = "font10",
                Text = String.Format("{0} is {1} miles away.\nThe expedition will take about {2}.", DestinationFaction.Name, (int)DestinationFaction.DistanceToCapital, 
                TextGenerator.TimeToString(Scripting.Adventure.Adventure.GetETA(Faction.Minions, DestinationFaction.DistanceToCapital))),
                MinimumSize = new Point(0, 64),
                AutoLayout = AutoLayout.DockTop
            });

            List<Adventure> availableAdventures = new List<Adventure>()
            {
                new TradeAdventure()
                {
                    DestinationFaction = DestinationFaction.Name,
                    OwnerFaction = Faction.Name
                },
                new RaidAdventure()
                {
                    DestinationFaction = DestinationFaction.Name,
                    OwnerFaction = Faction.Name
                },
                new PeaceAdventure()
                {
                    DestinationFaction = DestinationFaction.Name,
                    OwnerFaction = Faction.Name
                }
            };
            var politics = World.Diplomacy.GetPolitics(Faction, DestinationFaction);
            List<Adventure> adventures = availableAdventures.Where(a =>
            {
                if (a.RequiresPeace && politics.GetCurrentRelationship() == Relationship.Hateful)
                {
                    return false;
                }

                if (a.RequiresWar && politics.GetCurrentRelationship() != Relationship.Hateful)
                {
                    return false;
                }
                return true;
            }).ToList();

            AddChild(new Widget()
            {
                Text = "Expedition Type:",
                AutoLayout = AutoLayout.DockTop,
                Font = "font16"
            });

            var list = AddChild(new WidgetListView()
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(rect.Width, adventures.Count * 32),
                InteriorMargin = new Margin(16, 16, 16, 16)
            }) as WidgetListView;
            foreach (var adventure in adventures)
            {
                var entry = list.AddItem(new Widget()
                {
                    Background = new TileReference("basic", 0),
                });
                entry.AddChild(new Widget()
                {
                    Text = adventure.Name,
                    AutoLayout = AutoLayout.DockTop,
                    Font = "font10",
                    InteriorMargin = new Margin(5, 5, 5, 5)
                });
                entry.AddChild(new Widget()
                {
                    Text = adventure.Description,
                    AutoLayout = AutoLayout.DockTop,
                    Font = "font8",
                    InteriorMargin = new Margin(5, 5, 5, 5)
                });
            }


            AddChild(new Button()
            {
                Text = "Cancel",
                Tooltip = "Go back to the factions view.",
                AutoLayout = AutoLayout.FloatBottomLeft,
                OnClick = (sender, args) =>
                {
                    if (OnCanceled != null) OnCanceled.Invoke();
                    Close();
                }
            });
            AddChild(new Button()
            {
                Text = "Next",
                Tooltip = "Select employees for the expedition.",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnClick = (sender, args) =>
                {
                    SelectedAdventure = adventures[list.SelectedIndex];
                    if (OnProceed != null) OnProceed.Invoke(this);
                    Close();
                }
            });
            Layout();
            base.Construct();
        }
    }
}