using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                    select new GItem(r.ResourceName, r.Image, 0, 1000, 1000, r.MoneyValue * priceMultiplier)).ToList();
        }

        public List<GItem> GetResources(List<ResourceAmount> resources )
        {
            return (from r in resources
                    where r.NumResources > 0
                    select new GItem(r.ResourceType.ResourceName, r.ResourceType.Image, 0, 1000, r.NumResources, r.ResourceType.MoneyValue)).ToList();
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
            ShoppingCart.ClearChildren();
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
            SellCart.OnItemRemoved += SellCart.AddItem;

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
            SellCart.ClearChildren();
            SellCart.UpdateItems();

            SellTotal.Text = "Order Total: " + 0.0f.ToString("C");
        }



        public GoodsPanel(DwarfGUI gui, GUIComponent parent, Faction faction) 
            : base(gui, parent)
        {
            Faction = faction;
            GridLayout layout = new GridLayout(GUI, this, 8, 4);
            Tabs = new TabSelector(GUI, layout, 4);
            
            CreateBuyTab();
            CreateSellTab();

            Tabs.SetTab("Buy");
            
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
}
