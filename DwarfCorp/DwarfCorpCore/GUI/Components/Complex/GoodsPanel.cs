// GoodsPanel.cs
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class GoodsPanel : Panel
    {
        public TabSelector Tabs { get; set; }
        public Label BuyTotal { get; set; }
        public Label SellTotal { get; set; }
        public Label TotalMoney { get; set; }
        public Label SpaceLabel { get; set; }

        public ItemSelector BuySelector { get; set; }
        public ItemSelector SellSelector { get; set; }

        public ItemSelector ShoppingCart { get; set; }
        public ItemSelector SellCart { get; set; }


        public List<GItem> GetResources(float priceMultiplier)
        {
            return (from r in ResourceLibrary.Resources.Values
                    select new GItem(r, r.Image, r.Tint, 0, 1000, 1000, r.MoneyValue * priceMultiplier)).ToList();
        }

        public List<GItem> GetResources(List<ResourceAmount> resources )
        {
            return (from r in resources
                    where r.NumResources > 0
                    select new GItem(r.ResourceType, r.ResourceType.Image, r.ResourceType.Tint, 0, 1000, r.NumResources, r.ResourceType.MoneyValue)).ToList();
        }

        public void CreateBuyTab()
        {
            TabSelector.Tab buyTab = Tabs.AddTab("Buy");

            GridLayout buyBoxLayout = new GridLayout(GUI, buyTab, 10, 4);

            BuySelector = new ItemSelector(GUI, buyBoxLayout, "Items")
            {
                Columns = new List<ItemSelector.Column>()
                {
                    ItemSelector.Column.Image,
                    ItemSelector.Column.Name,
                    ItemSelector.Column.PricePerItem,
                    ItemSelector.Column.ArrowRight
                },
                NoItemsMessage = "Nothing to buy",
                ToolTip = "Click items to add them to the shopping cart"
            };

            buyBoxLayout.SetComponentPosition(BuySelector, 0, 0, 2, 10);

            BuySelector.Items.AddRange(GetResources(1.0f));
            BuySelector.ReCreateItems();


            ShoppingCart = new ItemSelector(GUI, buyBoxLayout, "Order")
            {
                Columns = new List<ItemSelector.Column>()
                {
                    ItemSelector.Column.ArrowLeft,
                    ItemSelector.Column.Image,
                    ItemSelector.Column.Name,
                    ItemSelector.Column.Amount,
                    ItemSelector.Column.TotalPrice
                },
                NoItemsMessage = "No items selected",
                ToolTip = "Click items to remove them from the shopping cart",
                PerItemCost = 1.00f
            };
            ShoppingCart.ReCreateItems();
            buyBoxLayout.SetComponentPosition(ShoppingCart, 2, 0, 2, 9);

            BuySelector.OnItemRemoved += ShoppingCart.AddItem;
            ShoppingCart.OnItemRemoved += BuySelector.AddItem;
            ShoppingCart.OnItemChanged += shoppingCart_OnItemChanged;

            Button buyButton = new Button(GUI, buyBoxLayout, "Buy", GUI.DefaultFont, Button.ButtonMode.PushButton, null)
            {
                ToolTip = "Click to order items in the shopping cart"
            };

            buyBoxLayout.SetComponentPosition(buyButton, 3, 9, 1, 2);

            BuyTotal = new Label(GUI, buyBoxLayout, "Order Total: $0.00", GUI.DefaultFont)
            {
                WordWrap = true,
                ToolTip = "Order total"
            };


            buyBoxLayout.SetComponentPosition(BuyTotal, 2, 9, 1, 2);

            buyButton.OnClicked += buyButton_OnClicked;
        }

        public void Buy()
        {
            Faction.Economy.CurrentMoney -= ShoppingCart.ComputeTotal();

            foreach(GItem item in ShoppingCart.Items)
            {
                Faction.AddResources(new ResourceAmount(item.Name) { NumResources = item.CurrentAmount });
            }

            SellSelector.Items = GetResources(Faction.ListResources().Values.ToList());
            SellSelector.ReCreateItems();

        }

        public void Sell()
        {
            Faction.Economy.CurrentMoney += SellCart.ComputeTotal();
            List<ResourceAmount> removals = new List<ResourceAmount>();
            foreach (GItem item in SellCart.Items)
            {
                removals.Add(new ResourceAmount(item.Name) { NumResources = item.CurrentAmount });
            }

            Faction.RemoveResources(removals, Microsoft.Xna.Framework.Vector3.Zero);
            
        }


        void buyButton_OnClicked()
        {
            float total = ShoppingCart.ComputeTotal();


            if(!(total > 0))
            {
                Dialog.Popup(GUI, "Nothing to buy!", "Nothing to buy. Select something from the left panel.", Dialog.ButtonType.OK);
                return;
            }
            if (total > Faction.Economy.CurrentMoney)
            {
                Dialog.Popup(GUI, "Not enough money!", "Can't buy that! We don't have enough money (we have only " + Faction.Economy.CurrentMoney.ToString("C") + " in our treasury).", Dialog.ButtonType.OK);
                return;
            }
            else if (Faction.ComputeStockpileSpace() < ShoppingCart.ComputeSpace())
            {
                Dialog.Popup(GUI, "Too many items!", "Can't buy that! We don't have enough inventory space. Build more stockpiles.", Dialog.ButtonType.OK);
                return;
            }

            SoundManager.PlaySound(ContentPaths.Audio.cash);
            Buy();

            ShoppingCart.Items.Clear();
            ShoppingCart.ReCreateItems();
            ShoppingCart.UpdateItems();
            
            BuyTotal.Text = "Order Total: " + 0.0f.ToString("C");
        }

        void shoppingCart_OnItemChanged(GItem item)
        {

            float total = ShoppingCart.ComputeTotal();
            float shipping = ShoppingCart.ComputeShipping();
            BuyTotal.Text = "Order Total: " + (total).ToString("C") + "\n (" + shipping.ToString("C") + " shipping)";

            if(total > Faction.Economy.CurrentMoney)
            {
                BuyTotal.TextColor = Microsoft.Xna.Framework.Color.DarkRed;
                BuyTotal.ToolTip = "Can't buy! Not enough money!";
            }
            else
            {
                BuyTotal.TextColor = Microsoft.Xna.Framework.Color.Black;
                BuyTotal.ToolTip = "We have enough money to buy this.";
            }
        }

        void SellCart_OnItemChanged(GItem item)
        {
            SellTotal.Text = "Order Total: " + SellCart.ComputeTotal().ToString("C");
        }


        public void CreateSellTab()
        {
            TabSelector.Tab sellTab = Tabs.AddTab("Sell");

            GridLayout sellBoxLayout = new GridLayout(GUI, sellTab, 10, 4);

            SellSelector = new ItemSelector(GUI, sellBoxLayout, "Items")
            {
                Columns = new List<ItemSelector.Column>()
                {
                    ItemSelector.Column.Image,
                    ItemSelector.Column.Name,
                    ItemSelector.Column.Amount,
                    ItemSelector.Column.TotalPrice,
                    ItemSelector.Column.ArrowRight
                },
                NoItemsMessage = "No goods in our stockpiles",
                ToolTip = "Click items to put them in the sell order"
            };

            sellBoxLayout.SetComponentPosition(SellSelector, 0, 0, 2, 10);

            SellSelector.Items.AddRange(GetResources(Faction.ListResources().Values.ToList()));
            SellSelector.ReCreateItems();


            SellCart = new ItemSelector(GUI, sellBoxLayout, "Order")
            {
                Columns = new List<ItemSelector.Column>()
                {
                    ItemSelector.Column.ArrowLeft,
                    ItemSelector.Column.Image,
                    ItemSelector.Column.Name,
                    ItemSelector.Column.Amount,
                    ItemSelector.Column.TotalPrice
                },
                NoItemsMessage = "No items selected",
                ToolTip = "Click items to remove them from the sell order"
            };
            SellCart.ReCreateItems();
            sellBoxLayout.SetComponentPosition(SellCart, 2, 0, 2, 9);

            SellSelector.OnItemRemoved += SellCart.AddItem;
            SellCart.OnItemRemoved += SellSelector.AddItem;

            Button sellButton = new Button(GUI, sellBoxLayout, "Sell", GUI.DefaultFont, Button.ButtonMode.PushButton, null)
            {
                ToolTip = "Click to sell items in the order"
            };

            sellBoxLayout.SetComponentPosition(sellButton, 3, 9, 1, 2);

            sellButton.OnClicked += sellButton_OnClicked;

            SellTotal = new Label(GUI, sellBoxLayout, "Order Total: $0.00", GUI.DefaultFont)
            {
                WordWrap = true,
                ToolTip = "Order total"
            };

            sellBoxLayout.SetComponentPosition(SellTotal, 2, 9, 1, 2);

            SellCart.OnItemChanged += SellCart_OnItemChanged;
        }

        void sellButton_OnClicked()
        {
            float total = SellCart.ComputeTotal();


            if (!(total > 0))
            {
                Dialog.Popup(GUI, "Nothing to sell!", "Nothing to sell. Select something from the left panel.", Dialog.ButtonType.OK);
                return;
            }

            SoundManager.PlaySound(ContentPaths.Audio.cash);
            Sell();

            SellCart.Items.Clear();
            SellCart.ReCreateItems();
            SellCart.UpdateItems();

            SellTotal.Text = "Order Total: " + 0.0f.ToString("C");
        }



        public GoodsPanel(DwarfGUI gui, GUIComponent parent, Faction faction) 
            : base(gui, parent)
        {
            LocalBounds = parent.GlobalBounds;
            Faction = faction;
            GridLayout layout = new GridLayout(GUI, this, 8, 4);
            Tabs = new TabSelector(GUI, layout, 4)
            {
                WidthSizeMode = SizeMode.Fit
            };
            

            layout.SetComponentPosition(Tabs, 0, 0, 4, 8);

            TotalMoney = new Label(GUI, layout, "Total Money: " + Faction.Economy.CurrentMoney.ToString("C"), GUI.DefaultFont)
            {
                ToolTip = "Total amount of money in our treasury",
                WordWrap = true
            };

            TotalMoney.OnUpdate += TotalMoney_OnUpdate;

            layout.SetComponentPosition(TotalMoney, 3, 0, 1, 1);

            SpaceLabel = new Label(GUI, layout, "Space: " + Faction.ComputeStockpileSpace() + "/" + Faction.ComputeStockpileCapacity(), GUI.DefaultFont)
            {
                ToolTip = "Space left in our stockpiles",
                WordWrap = true
            };

            layout.SetComponentPosition(SpaceLabel, 2, 0, 1, 1);

            SpaceLabel.OnUpdate += SpaceLabel_OnUpdate;

            layout.UpdateSizes();

            CreateBuyTab();
            CreateSellTab();
            Tabs.SetTab("Buy");
        }

        void SpaceLabel_OnUpdate()
        {
            SpaceLabel.Text = "Space: " + Faction.ComputeStockpileSpace() + "/" + Faction.ComputeStockpileCapacity();
        }

        void TotalMoney_OnUpdate()
        {
            TotalMoney.Text = "Total Money: " + Faction.Economy.CurrentMoney.ToString("C");
        }

        public Faction Faction { get; set; }
    }

    public class TradeEvent
    {
       public List<ResourceAmount> GoodsReceived { get; set; } 
       public List<ResourceAmount> GoodsSent { get; set; }

        public struct Profit
        {
            public float TotalProfit 
            {
                get { return TheirValue - OurValue; }
            }
            
            public float OurValue { get; set; }
            public float TheirValue { get; set; }
            
            public float PercentProfit
            {
                get
                {
                    if (OurValue + TheirValue > 0) return TotalProfit/(OurValue + TheirValue);
                    else return 0.0f;
                }
            }
        }

        public Profit GetProfit()
        {
            float ourAmount = GoodsReceived.Sum(amount => amount.NumResources*amount.ResourceType.MoneyValue);
            float theirAmount = GoodsSent.Sum(amount => amount.NumResources * amount.ResourceType.MoneyValue);

            return new Profit() { OurValue = ourAmount, TheirValue = theirAmount };
        }
    }

    public class TradeDialog : Dialog
    {
        public TradePanel TradePanel { get; set; }

        public delegate void OnTrade(TradeEvent e);

        public event OnTrade OnTraded;

        protected virtual void OnOnTraded(TradeEvent e)
        {
            OnTrade handler = OnTraded;
            if (handler != null) handler(e);
        }


        public TradeDialog(DwarfGUI gui, GUIComponent parent, Faction otherFaction, WindowButtons buttons)
            : base(gui, parent,buttons)
        {
            TradePanel = new TradePanel(GUI, this, PlayState.PlayerFaction, otherFaction)
            {
                WidthSizeMode = SizeMode.Fit,
                HeightSizeMode = SizeMode.Fit
            };
            TradePanel.OnTraded += TradePanel_OnTraded;
        }

        void TradePanel_OnTraded(TradeEvent e)
        {
            OnTraded.Invoke(e);
            Close(ReturnStatus.Ok);
        }

        public static TradeDialog Popup(DwarfGUI gui, int w, int h, GUIComponent parent, int x, int y, WindowButtons buttons, Faction faction)
        {
            TradeDialog d = new TradeDialog(gui, parent, faction, buttons)
            {
                LocalBounds =
                    new Rectangle(x, y, w, h),
                MinWidth = w - 150,
                MinHeight = h - 150,
                DrawOrder = 100,
                IsModal = true
            };

            return d;
        }

        public static TradeDialog Popup(DwarfGUI gui, GUIComponent parent, Faction faction, WindowButtons buttons = WindowButtons.CloseButton)
        {
            int w = gui.Graphics.Viewport.Width - 200;
            int h = gui.Graphics.Viewport.Height - 200;
            int x = gui.Graphics.Viewport.Width / 2 - w / 2;
            int y = gui.Graphics.Viewport.Height / 2 - h / 2;
            return Popup(gui, w, h, parent, x, y, buttons, faction);
        }

    }

    [JsonObject(IsReference = true)]
    public class TradePanel : GUIComponent
    {
        public GridLayout Layout { get; set; }
        public Label BuyTotal { get; set; }
        public Label SellTotal { get; set; }
        public Label SpaceLabel { get; set; }
        public ItemSelector TheirGoods { get; set; }
        public ItemSelector MyGoods { get; set; }
        public Faction OtherFaction { get; set; }
        public List<ResourceAmount> GoodsSent { get; set; }
        public List<ResourceAmount> GoodsReceived { get; set; } 
        public delegate void OnTrade(TradeEvent e);

        public event OnTrade OnTraded;

        protected virtual void OnOnTraded(TradeEvent e)
        {
            OnTrade handler = OnTraded;
            if (handler != null) handler(e);
        }

        public List<GItem> GetResources(float priceMultiplier)
        {
            return (from r in ResourceLibrary.Resources.Values
                    select new GItem(r, r.Image, r.Tint, 0, 1000, 1000, r.MoneyValue * priceMultiplier)).ToList();
        }

        public List<GItem> GetResources(List<ResourceAmount> resources)
        {
            return (from r in resources
                    where r.NumResources > 0
                    select new GItem(r.ResourceType, r.ResourceType.Image, r.ResourceType.Tint, 0, 1000, r.NumResources, r.ResourceType.MoneyValue)).ToList();
        }

        

        public void Buy()
        {
            OnTraded.Invoke(new TradeEvent(){GoodsReceived = GoodsReceived, GoodsSent = GoodsSent});
        }


        void buyButton_OnClicked()
        {
            if (Faction.ComputeStockpileSpace() < MyGoods.ComputeSpace())
            {
                GUI.ToolTipManager.Popup("Not enough stockpile space!");
                return;
            }

            MyGoods.Items.Clear();
            MyGoods.ReCreateItems();
            MyGoods.UpdateItems();
            Buy();
        }

        void shoppingCart_OnItemChanged(GItem item)
        {
            TradeEvent trade = new TradeEvent() {GoodsReceived = GoodsReceived, GoodsSent = GoodsSent};
            float total = trade.GetProfit().TotalProfit;
            BuyTotal.Text = "Total Profit: " + (total).ToString("C");
        }


        public void CreateSelector()
        {
            TheirGoods = new ItemSelector(GUI, Layout, "Their Items")
            {
                Columns = new List<ItemSelector.Column>()
                {
                    ItemSelector.Column.Image,
                    ItemSelector.Column.Name,
                    ItemSelector.Column.PricePerItem,
                    ItemSelector.Column.ArrowRight
                },
                NoItemsMessage = "Nothing to buy",
                ToolTip = "Click items to trade for them."
            };

            Layout.SetComponentPosition(TheirGoods, 0, 0, 2, 9);

            TheirGoods.Items.AddRange(GetResources(1.0f));
            TheirGoods.ReCreateItems();


            MyGoods = new ItemSelector(GUI, Layout, "Our Items")
            {
                Columns = new List<ItemSelector.Column>()
                {
                    ItemSelector.Column.ArrowLeft,
                    ItemSelector.Column.Image,
                    ItemSelector.Column.Name,
                    ItemSelector.Column.Amount,
                    ItemSelector.Column.TotalPrice
                },
                NoItemsMessage = "No items selected",
                ToolTip = "Click items to offer them for trade.",
                PerItemCost = 1.00f
            };
            MyGoods.Items.AddRange(GetResources(PlayState.PlayerFaction.ListResources().Values.ToList()));
            MyGoods.ReCreateItems();
            Layout.SetComponentPosition(MyGoods, 2, 0, 2, 9);

            TheirGoods.OnItemRemoved += MyGoods.AddItem;
            MyGoods.OnItemRemoved += TheirGoods.AddItem;
            MyGoods.OnItemChanged += shoppingCart_OnItemChanged;
            MyGoods.OnItemRemoved += TheirGoods_OnItemAdded;
            TheirGoods.OnItemRemoved += MyGoods_OnItemAdded;
            Button buyButton = new Button(GUI, Layout, "Offer Trade", GUI.DefaultFont, Button.ButtonMode.PushButton, null)
            {
                ToolTip = "Click to offer up a trade."
            };

            Layout.SetComponentPosition(buyButton, 3, 9, 1, 2);

            BuyTotal = new Label(GUI, Layout, "Profit: $0.00", GUI.DefaultFont)
            {
                WordWrap = true,
                ToolTip = "Their profit from the trade."
            };


            Layout.SetComponentPosition(BuyTotal, 0, 9, 1, 2);

            buyButton.OnClicked += buyButton_OnClicked;
        }

        void MyGoods_OnItemAdded(GItem item, int amount)
        {
            GoodsReceived.Add(new ResourceAmount(ResourceLibrary.GetResourceByName(item.Name), amount));
        }

        void TheirGoods_OnItemAdded(GItem item, int amount)
        {
            GoodsSent.Add(new ResourceAmount(ResourceLibrary.GetResourceByName(item.Name), amount));
        }

        public TradePanel(DwarfGUI gui, GUIComponent parent, Faction faction, Faction otherFaction)
            : base(gui, parent)
        {
            GoodsSent = new List<ResourceAmount>();
            GoodsReceived = new List<ResourceAmount>();
            LocalBounds = parent.GlobalBounds;
            Faction = faction;
            OtherFaction = otherFaction;
            Layout = new GridLayout(GUI, this, 10, 4);

            SpaceLabel = new Label(GUI, Layout, "Space: " + Faction.ComputeStockpileSpace() + "/" + Faction.ComputeStockpileCapacity(), GUI.DefaultFont)
            {
                ToolTip = "Space left in our stockpiles",
                WordWrap = true
            };

            Layout.SetComponentPosition(SpaceLabel, 2, 9, 1, 1);

            SpaceLabel.OnUpdate += SpaceLabel_OnUpdate;

            Layout.UpdateSizes();

            CreateSelector();
        }

        void SpaceLabel_OnUpdate()
        {
            SpaceLabel.Text = "Space: " + Faction.ComputeStockpileSpace() + "/" + Faction.ComputeStockpileCapacity();
        }


        public Faction Faction { get; set; }
    }

}
