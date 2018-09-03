// CompanyMakerState.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates
{
    public class FactionViewState : GameState
    {
        private Gui.Root GuiRoot;
        public WorldManager World;

        public FactionViewState(DwarfGame game, GameStateManager stateManager, WorldManager world) :
            base(game, "FactionViewState", stateManager)
        {
            World = world;
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);

            Rectangle rect = GuiRoot.RenderData.VirtualScreen;
            var mainPanel = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = rect,
                MinimumSize = new Point(3 * GuiRoot.RenderData.VirtualScreen.Width / 4, 
                3 * GuiRoot.RenderData.VirtualScreen.Height / 4),
                AutoLayout = AutoLayout.FloatCenter,
                Border = "border-fancy",
                Padding = new Margin(4, 4, 4, 4),
                InteriorMargin = new Margin(2, 0, 0, 0),
                TextSize = 1,
                Font = "font10"
            });

            mainPanel.AddChild(new Gui.Widgets.Button
            {
                Text = "< Back",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                Font = "font16",
                OnClick = (sender, args) =>
                {
                    StateManager.PopState();
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

            var factions = World.Factions.Factions.Where(f => !f.Value.IsRaceFaction && f.Value.Race.IsIntelligent && f.Value != World.PlayerFaction).OrderBy(k => k.Value.Race.Name == "Dwarf" ? 0 : k.Value.DistanceToCapital);
            foreach (var faction in factions)
            {
                var diplomacy = World.Diplomacy.GetPolitics(faction.Value, World.PlayerFaction);
                var details = diplomacy.RecentEvents.Select(e => string.Format("{0} ({1})", TextGenerator.ToSentenceCase(e.Description), e.Change > 0 ? "+" + e.Change.ToString() : e.Change.ToString()));
                
                var entry = widgetList.AddItem(new Widget()
                {
                    Background = new TileReference("basic", 0),
                });
                StringBuilder sb = new StringBuilder();
                foreach(var detail in details)
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
                    Text = System.String.Format("{0} ({1}){2}", faction.Value.Name, faction.Value.Race.Name, diplomacy.WasAtWar ? " -- At war!" : ""),
                    TextHorizontalAlign = HorizontalAlign.Right,
                    TextVerticalAlign = VerticalAlign.Bottom,
                    Font = "font10",
                    AutoLayout = AutoLayout.DockLeft
                });

                var currentAdventure = World.Diplomacy.Adventures.Where(a => a.DestinationFaction == faction.Key).FirstOrDefault();

                if (currentAdventure == null)
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
                            List<ResourceAmount> resources = new List<ResourceAmount>();
                            foreach (var resource in World.PlayerFaction.ListResources())
                            {
                                resources.Add(new ResourceAmount(resource.Value.ResourceType, 1));
                            }

                            World.Diplomacy.Adventures.Add(new Scripting.Adventure.TradeAdventure()
                            {
                                Party = Datastructures.SelectRandom(World.Master.Faction.Minions, 3).ToList(),
                                Money = (decimal)MathFunctions.Rand(1, (float)(decimal)World.PlayerEconomy.CurrentMoney),
                                DestinationFaction = faction.Key,
                                OwnerFaction = World.PlayerFaction.Name,
                                Position = World.WorldOrigin,
                                Start = World.WorldOrigin,
                                Resources = resources
                            });
                            OnEnter();
                        }
                    });
                }
                else
                {
                    var eta = currentAdventure.GetETA(World);
                    titlebar.AddChild(new Widget()
                    {
                        Text = string.Format("Expedition underway ...\n {0}", eta.ToString("c")),
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
                    Text = System.String.Format("    Relationship: {0}{1}", diplomacy.GetCurrentRelationship(), faction.Value.ClaimsColony ? " (Claims this territory)" : ""),
                    TextHorizontalAlign = HorizontalAlign.Left,
                    TextVerticalAlign = VerticalAlign.Top,
                    Font = "font8",
                    AutoLayout = AutoLayout.DockTop,
                    TextColor = relationshipColor
                });
                entry.AddChild(new Widget()
                {
                    Text = System.String.Format("    GDP: {0}    Size: {1}    Distance to capital: {2} miles", faction.Value.TradeMoney, faction.Value.TerritorySize, (int)faction.Value.DistanceToCapital),
                    TextHorizontalAlign = HorizontalAlign.Left,
                    TextVerticalAlign = VerticalAlign.Top,
                    Font = "font8",
                    AutoLayout = AutoLayout.DockTop
                });
            }

            mainPanel.Layout();

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