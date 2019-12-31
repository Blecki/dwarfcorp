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
    public class FactionViewState : GameState
    {
        protected Root GuiRoot;
        private Overworld Overworld;
        private Widget mainPanel;

        public FactionViewState(DwarfGame game, Overworld Overworld) : base(game)
        {
            this.Overworld = Overworld;
        }

        public void Reset()
        {
            mainPanel.Clear();
            Rectangle rect = GuiRoot.RenderData.VirtualScreen;

            mainPanel.AddChild(new Gui.Widgets.Button
            {
                Text = "< Back",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                Font = "font16",
                OnClick = (sender, args) =>
                {
                    GameStateManager.PopState();
                },
                AutoLayout = AutoLayout.FloatBottomLeft,
            });


            var widgetList = mainPanel.AddChild(new WidgetListView()
            {
                AutoLayout = AutoLayout.DockTop,
                SelectedItemForegroundColor = Color.Black.ToVector4(),
                SelectedItemBackgroundColor = new Vector4(0, 0, 0, 0),
                ItemBackgroundColor2 = new Vector4(0, 0, 0, 0.1f),
                ItemBackgroundColor1 = new Vector4(0, 0, 0, 0),
                ItemHeight = 64,
                MinimumSize = new Point(0, 3 * GuiRoot.RenderData.VirtualScreen.Height / 4)
            }) as WidgetListView;

            var factions = Overworld.Natives.Where(f => f.InteractiveFaction && Library.GetRace(f.Race).HasValue(out var race) && race.IsIntelligent);

            foreach (var faction in factions)
            {
                var diplomacy = Overworld.GetPolitics(faction, Overworld.Natives.FirstOrDefault(n => n.Name == "Player"));
                var details = diplomacy.GetEvents().Select(e => string.Format("{0} ({1})", TextGenerator.ToSentenceCase(e.Description), e.Change > 0 ? "+" + e.Change.ToString() : e.Change.ToString()));

                var entry = widgetList.AddItem(new Widget()
                {
                    Background = new TileReference("basic", 0),
                });
                StringBuilder sb = new StringBuilder();
                foreach (var detail in details)
                {
                    sb.AppendLine(detail);
                }
                entry.Tooltip = "Recent events:\n" + sb.ToString();
                if (sb.ToString() == "")
                {
                    entry.Tooltip = "No recent events.";
                }
                var titlebar = entry.AddChild(new Widget()
                {
                    InteriorMargin = new Margin(5, 5, 5, 5),
                    MinimumSize = new Point(512, 36),
                    AutoLayout = AutoLayout.DockTop,
                });
                titlebar.AddChild(new Widget()
                {
                    Background = new TileReference("map-icons", Library.GetRace(faction.Race).HasValue(out var race) ? race.Icon : 0),
                    MaximumSize = new Point(32, 32),
                    MinimumSize = new Point(32, 32),
                    AutoLayout = AutoLayout.DockLeft,
                });
                titlebar.AddChild(new Widget()
                {
                    Text = String.Format("{0} ({1}){2}", faction.Name, faction.Race, diplomacy.IsAtWar ? " -- At war!" : ""),
                    TextHorizontalAlign = HorizontalAlign.Right,
                    TextVerticalAlign = VerticalAlign.Bottom,
                    Font = "font10",
                    AutoLayout = AutoLayout.DockLeft
                });


                var relation = diplomacy.GetCurrentRelationship();
                var relationshipColor = Color.Black.ToVector4();
                if (relation == Relationship.Loving)
                {
                    relationshipColor = GameSettings.Current.Colors.GetColor("Positive", Color.DarkGreen).ToVector4();
                }
                else if (relation == Relationship.Hateful)
                {
                    relationshipColor = GameSettings.Current.Colors.GetColor("Negative", Color.Red).ToVector4();
                }
                entry.AddChild(new Widget()
                {
                    Text = String.Format("    Relationship: {0}", diplomacy.GetCurrentRelationship()),
                    TextHorizontalAlign = HorizontalAlign.Left,
                    TextVerticalAlign = VerticalAlign.Top,
                    Font = "font8",
                    AutoLayout = AutoLayout.DockTop,
                    TextColor = relationshipColor
                });
                entry.AddChild(new Widget()
                {
                    Text = "",
                    TextHorizontalAlign = HorizontalAlign.Left,
                    TextVerticalAlign = VerticalAlign.Top,
                    Font = "font8",
                    AutoLayout = AutoLayout.DockTop
                });
            }

            mainPanel.Layout();
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
            var rect = GuiRoot.RenderData.VirtualScreen;
            mainPanel = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = rect,
                MinimumSize = new Point(3 * GuiRoot.RenderData.VirtualScreen.Width / 4, 3 * GuiRoot.RenderData.VirtualScreen.Height / 4),
                AutoLayout = AutoLayout.FloatCenter,
                Border = "border-fancy",
                Padding = new Margin(4, 4, 4, 4),
                InteriorMargin = new Margin(2, 0, 0, 0),
                TextSize = 1,
                Font = "font10"
            });
            Reset();

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

            GuiRoot.Update(gameTime.ToRealTime());
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }

        public virtual void UpdateTutorialHandler()
        {

        }
    }

    public class PlayFactionViewState : FactionViewState
    {
        private WorldManager World;

        public PlayFactionViewState(DwarfGame Game, WorldManager World) : base(Game, World.Overworld)
        {
            this.World = World;
        }

        public override void UpdateTutorialHandler()
        {
            World.Tutorial("diplomacy");
            World.TutorialManager.Update(GuiRoot);
            base.UpdateTutorialHandler();
        }
    }

}