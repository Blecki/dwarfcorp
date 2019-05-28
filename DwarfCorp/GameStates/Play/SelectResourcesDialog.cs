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
            var existingResources = World.ListResources();
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
}