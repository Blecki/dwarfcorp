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
    public class FactionViewState : GameState
    {
        private Gui.Root GuiRoot;
        public WorldManager World;
        private Gui.Widget mainPanel;
        public FactionViewState(DwarfGame game, WorldManager world) :
            base(game)
        {
            World = world;
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

            var factions = World.Factions.Factions.Where(f => f.Value.InteractiveFaction && f.Value.Race.IsIntelligent && f.Value != World.PlayerFaction).OrderBy(k =>
            {
                if (k.Value.Race.Name == "Dwarf")
                    return 0;

                var currentExpedition = World.Diplomacy.Adventures.Where(a => a.DestinationFaction == k.Key).FirstOrDefault();
                if (currentExpedition != null)
                    return k.Value.DistanceToCapital;
                return k.Value.DistanceToCapital + 100000.0f;
            });

            foreach (var faction in factions)
            {
                var diplomacy = World.Diplomacy.GetPolitics(faction.Value, World.PlayerFaction);
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
                    Background = new TileReference("map-icons", faction.Value.Race.Icon),
                    MaximumSize = new Point(32, 32),
                    MinimumSize = new Point(32, 32),
                    AutoLayout = AutoLayout.DockLeft,
                });
                titlebar.AddChild(new Widget()
                {
                    Text = String.Format("{0} ({1}){2}", faction.Value.ParentFaction.Name, faction.Value.Race.Name, diplomacy.IsAtWar ? " -- At war!" : ""),
                    TextHorizontalAlign = HorizontalAlign.Right,
                    TextVerticalAlign = VerticalAlign.Bottom,
                    Font = "font10",
                    AutoLayout = AutoLayout.DockLeft
                });

                var currentAdventure = World.Diplomacy.Adventures.Where(a => a.DestinationFaction == faction.Key).FirstOrDefault();

                if (currentAdventure == null && World.PlayerFaction.Minions.Count > 0)
                {
                    titlebar.AddChild(new Button()
                    {
                        Text = "Send Expedition...",
                        TextHorizontalAlign = HorizontalAlign.Center,
                        TextVerticalAlign = VerticalAlign.Center,
                        Font = "font10",
                        AutoLayout = AutoLayout.DockRight,
                        OnClick = (sender, args) =>
                        {
                            World.Tutorial("adventures");
                            GuiRoot.ShowModalPopup(GuiRoot.ConstructWidget(new PrepareExpeditionDialog()
                            {
                                Faction = World.PlayerFaction,




                                World = World,
                                DestinationFaction = faction.Value,
                                Rect = GuiRoot.RenderData.VirtualScreen.Interior(128, 128, 128, 128),
                                OnProceed = (dialog) =>
                                {
                                    GuiRoot.ShowModalPopup(GuiRoot.ConstructWidget(new SelectEmployeesDialog()
                                    {
                                        Faction = World.PlayerFaction,
                                        World = World,
                                        Rect = GuiRoot.RenderData.VirtualScreen.Interior(32, 32, 32, 32),
                                        OnProceed = (selectEmployees) =>
                                        {
                                            GuiRoot.ShowModalPopup(new SelectResourcesDialog()
                                            {
                                                Rect = GuiRoot.RenderData.VirtualScreen.Interior(32, 32, 32, 32),
                                                Faction = World.PlayerFaction,
                                                OnProceed = (selectResources) =>
                                                {
                                                    var adventure = dialog.SelectedAdventure;
                                                    adventure.Party = selectEmployees.GoingCreatures;
                                                    adventure.Money = selectResources.SelectedMoney;
                                                    adventure.Resources = selectResources.SelectedResources;
                                                    adventure.DestinationFaction = faction.Key;
                                                    adventure.OwnerFaction = World.PlayerFaction.ParentFaction.Name;
                                                    adventure.Position = World.Settings.InstanceSettings.Origin;
                                                    adventure.Start = World.Settings.InstanceSettings.Origin;
                                                    World.Diplomacy.Adventures.Add(adventure);
                                                    World.PlayerFaction.RemoveResources(selectResources.SelectedResources, Vector3.Zero, false);
                                                    World.PlayerFaction.AddMoney(-selectResources.SelectedMoney);
                                                    Reset();
                                                }
                                            });
                                        }
                                    }));
                                }
                            }));
                        }
                    });
                }
                else if (currentAdventure != null)
                {
                    var eta = currentAdventure.GetStatusString(World);
                    titlebar.AddChild(new TextProgressBar()
                    {
                        MinimumSize = new Point(128, 32),
                        Percentage = currentAdventure.GetProgress(World),
                        SegmentCount = 10,
                        AutoLayout = AutoLayout.DockRight
                    });
                    titlebar.AddChild(new Widget()
                    {
                        Text = string.Format("Expedition underway ...\n {0}", eta),
                        TextHorizontalAlign = HorizontalAlign.Center,
                        TextVerticalAlign = VerticalAlign.Center,
                        Font = "font10",
                        AutoLayout = AutoLayout.DockRight,
                    });
                }


                var relation = diplomacy.GetCurrentRelationship();
                var relationshipColor = Color.Black.ToVector4();
                if (relation == Relationship.Loving)
                {
                    relationshipColor = GameSettings.Default.Colors.GetColor("Positive", Color.DarkGreen).ToVector4();
                }
                else if (relation == Relationship.Hateful)
                {
                    relationshipColor = GameSettings.Default.Colors.GetColor("Negative", Color.Red).ToVector4();
                }
                entry.AddChild(new Widget()
                {
                    Text = global::System.String.Format("    Relationship: {0}{1}", diplomacy.GetCurrentRelationship(), faction.Value.ClaimsColony ? " (Claims this territory)" : ""),
                    TextHorizontalAlign = HorizontalAlign.Left,
                    TextVerticalAlign = VerticalAlign.Top,
                    Font = "font8",
                    AutoLayout = AutoLayout.DockTop,
                    TextColor = relationshipColor
                });
                entry.AddChild(new Widget()
                {
                    Text = global::System.String.Format("    GDP: {0}    Size: {1}    Distance to capital: {2} miles", faction.Value.TradeMoney, faction.Value.TerritorySize, (int)faction.Value.DistanceToCapital),
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
            World.Tutorial("diplomacy");
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                }
            }
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