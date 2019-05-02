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
using System;
using DwarfCorp.Scripting.Adventure;

namespace DwarfCorp.GameStates
{
    public class SelectEmployeesDialog : Widget
    {
        public WorldManager World;
        public Faction Faction;
        public List<CreatureAI> StayingCreatures = new List<CreatureAI>();
        public List<CreatureAI> GoingCreatures = new List<CreatureAI>();
        public WidgetListView LeftColumns = null;
        public WidgetListView RightColumns = null;
        public Action OnCanceled = null;
        public Action<SelectEmployeesDialog> OnProceed = null;

        public SelectEmployeesDialog()
        {
        }

        private void AddCreature(CreatureAI employee, WidgetListView column, List<CreatureAI> creaturesA, List<CreatureAI> creaturesB)
        {
            var bar = Root.ConstructWidget(new Widget
            {
                Background = new TileReference("basic", 0),
                TriggerOnChildClick = true,
                OnClick = (sender, args) =>
                {
                    creaturesA.Remove(employee);
                    creaturesB.Add(employee);
                    ReconstructColumns();
                }
            });
            var employeeSprite = employee.GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>();

            if (employeeSprite != null)
                bar.AddChild(new EmployeePortrait
                {
                    AutoLayout = AutoLayout.DockLeft,
                    MinimumSize = new Point(48, 40),
                    MaximumSize = new Point(48, 40),
                    Sprite = employeeSprite.GetLayers(),
                    AnimationPlayer = new AnimationPlayer(employeeSprite.Animations["IdleFORWARD"])
                });

            var title = employee.Stats.Title ?? employee.Stats.CurrentLevel.Name;
            bar.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                TextVerticalAlign = VerticalAlign.Center,
                MinimumSize = new Point(128, 64),
                Text = (employee.Stats.FullName) + " (" + title + ")"
            });

            column.AddItem(bar);
        }

        public void ReconstructColumns()
        {
            LeftColumns.ClearItems();
            RightColumns.ClearItems();
            foreach (var employee in StayingCreatures)
            {
                AddCreature(employee, LeftColumns, StayingCreatures, GoingCreatures);
            }

            foreach (var employee in GoingCreatures)
            {
                AddCreature(employee, RightColumns, GoingCreatures, StayingCreatures);
            }
        }

        public override void Construct()
        {
            Border = "border-one";
            Text = "Prepare for Expedition";
            Font = "font16";
            InteriorMargin = new Margin(32, 5, 5, 5);
            StayingCreatures.AddRange(Faction.Minions.Where(minion => !Faction.World.Diplomacy.Adventures.Any(adventure => adventure.Party.Contains(minion))));
            var rect = GetDrawableInterior();
            var leftSide = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(rect.Width / 2 - 28, rect.Height - 100),
            });
            leftSide.AddChild(new Widget()
            {
                Font= "font16",
                Text = "Staying",
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockTop
            });
            LeftColumns = leftSide.AddChild(new WidgetListView()
            {
                Font = "font10",
                ItemHeight = 40,
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(rect.Width / 2 - 32, rect.Height - 132),
                InteriorMargin = new Margin(32, 5, 5, 5),
                ChangeColorOnSelected = false,
                Tooltip = "Click to select dwarves for the journey."
            }) as WidgetListView;


            var rightSide = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(rect.Width / 2 - 28, rect.Height - 100),
            });

            rightSide.AddChild(new Widget()
            {
                Font= "font16",
                Text = "Going",
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockTop
            });

            RightColumns = rightSide.AddChild(new WidgetListView()
            {
                Font = "font10",
                ItemHeight = 40,
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(rect.Width / 2 - 32, rect.Height - 132),
                InteriorMargin = new Margin(32, 5, 5, 5),
                ChangeColorOnSelected = false,
                Tooltip = "Click to leave dwarves at home."
            }) as WidgetListView;

            ReconstructColumns();

            leftSide.AddChild(new Button()
            {
                Text = "Back",
                Tooltip = "Go back to the factions view.",
                AutoLayout = AutoLayout.FloatBottomLeft,
                OnClick = (sender, args) =>
                {
                    if(OnCanceled != null) OnCanceled.Invoke();
                    Close();
                }
            });

            rightSide.AddChild(new Button()
            {
                Text = "Next",
                Tooltip = "Select resources for the expedition.",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnClick = (sender, args) =>
                {
                    if (GoingCreatures.Count == 0)
                    {
                        Root.ShowModalPopup(new Gui.Widgets.Confirm()
                        {
                            Text = "Please select at least one employee for the expedition.",
                            CancelText = ""
                        });
                        return;
                    }
                    if (OnProceed != null) OnProceed.Invoke(this);
                    Close();
                }
            });

            Layout();
            base.Construct();
        }
    }

    public class SelectResourcesDialog : Widget
    {
        public WorldManager World;
        public Faction Faction;
        public List<ResourceAmount> StayingResources;
        public Action OnCanceled;
        public Action<SelectResourcesDialog> OnProceed;
        public List<ResourceAmount> SelectedResources;
        public DwarfBux SelectedMoney;

        public class ExpeditionTradeEntity : Trade.ITradeEntity
        {
            public Faction Faction;
            public int Space;
            public DwarfBux AvailableMoney;
            public List<ResourceAmount> AvailableResources;
            public List<ResourceAmount> Resources
            {
                get { return AvailableResources; }
            }

            public DwarfBux Money
            {
                get { return AvailableMoney; }
            }

            public int AvailableSpace
            {
                get { return Space; }
            }

            public Race TraderRace
            {
                get { return Faction.Race; }
            }


            public Faction TraderFaction
            {
                get { return Faction; }
            }


            public void AddMoney(DwarfBux Money)
            {
                AvailableMoney += Money;
            }

            public void AddResources(List<ResourceAmount> Resources)
            {
                foreach(var resource in Resources)
                {
                    var existing = AvailableResources.FirstOrDefault(r => r.Type == resource.Type);
                    if (existing != null)
                    {
                        existing.Count += resource.Count;
                    }
                    else
                    {
                        AvailableResources.Add(resource);
                    }
                }
            }

            public DwarfBux ComputeValue(List<ResourceAmount> Resources)
            {
                return Resources.Sum(r => ComputeValue(r.Type) * r.Count);
            }

            public DwarfBux ComputeValue(String Resource)
            {
                return ResourceLibrary.GetResourceByName(Resource).MoneyValue;
            }

            public void RemoveResources(List<ResourceAmount> Resources)
            {
                foreach (var resource in Resources)
                {
                    var existing = AvailableResources.FirstOrDefault(r => r.Type == resource.Type);
                    if (existing != null)
                    {
                        existing.Count -= resource.Count;
                    }
                    else
                    {
                        //
                    }
                }
            }
        }

        public SelectResourcesDialog()
        {

        }

        public override void Construct()
        {
            Border = "border-one";
            Text = "Select Resources to Trade";
            Font = "font16";
            InteriorMargin = new Margin(32, 5, 5, 5);
            var existingResources = Faction.ListResources();
            StayingResources = new List<ResourceAmount>();
            foreach(var resource in existingResources)
            {
                StayingResources.Add(resource.Value);
            }
            var TradeEntity = new ExpeditionTradeEntity()
            {
                Faction = this.Faction,
                AvailableMoney = this.Faction.Economy.Funds,
                AvailableResources = StayingResources,
                Space = 9999
            };
            var container = AddChild(new Widget()
            {
                Rect = GetDrawableInterior().Interior(new Margin(64, 64, 32, 32))
            });
            ResourceColumns columns = container.AddChild(new ResourceColumns()
            {
                AutoLayout = AutoLayout.DockFill,
                TradeEntity = TradeEntity,
                ValueSourceEntity = TradeEntity,
                LeftHeader = "In Stockpiles",
                RightHeader = "With Expedition",
                MoneyLabel = "Trade Money"
                
            }) as ResourceColumns;

            columns.Reconstruct(StayingResources, new List<ResourceAmount>(), (int)Faction.Economy.Funds);

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
                Text = "Send Expedition",
                Tooltip = "The expedition will begin immediately!",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnClick = (sender, args) =>
                {
                    SelectedResources = columns.SelectedResources;
                    SelectedMoney = columns.TradeMoney;
                    if (OnProceed != null) OnProceed.Invoke(this);
                    Close();
                }
            });

            Layout();
            base.Construct();
        }

    }

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


    public class FactionViewState : GameState
    {
        private Gui.Root GuiRoot;
        public WorldManager World;
        private Gui.Widget mainPanel;
        public FactionViewState(DwarfGame game, GameStateManager stateManager, WorldManager world) :
            base(game, "FactionViewState", stateManager)
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
                    if (StateManager.CurrentState == this)
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

            var factions = World.Factions.Factions.Where(f => !f.Value.IsRaceFaction && f.Value.Race.IsIntelligent && f.Value != World.PlayerFaction).OrderBy(k =>
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
                    Text = System.String.Format("{0} ({1}){2}", faction.Value.Name, faction.Value.Race.Name, diplomacy.IsAtWar ? " -- At war!" : ""),
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
                                                    adventure.OwnerFaction = World.PlayerFaction.Name;
                                                    adventure.Position = World.WorldOrigin;
                                                    adventure.Start = World.WorldOrigin;
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