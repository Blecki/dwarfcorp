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

        public List<GItem> GetResources(float priceMultiplier)
        {
            return (from r in ResourceLibrary.Resources.Values
                    select new GItem(r.ResourceName, r.Image, 0, 1000, 1000, r.MoneyValue * priceMultiplier, r.Tags)).ToList();
        }

        public List<GItem> GetResources(List<ResourceAmount> resources )
        {
            return (from r in resources
                    where r.NumResources > 0
                    select new GItem(r.ResourceType.ResourceName, r.ResourceType.Image, 0, 1000, r.NumResources, r.ResourceType.MoneyValue, r.ResourceType.Tags)).ToList();
        }

        public void CreateBuyTab()
        {
            TabSelector.Tab buyTab = Tabs.AddTab("Buy");

            GridLayout buyBoxLayout = new GridLayout(GUI, buyTab, 10, 4);

            ItemSelector buySelector = new ItemSelector(GUI, buyBoxLayout, "Items")
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

            buyBoxLayout.SetComponentPosition(buySelector, 0, 0, 2, 10);

            buySelector.Items.AddRange(GetResources(1.0f));
            buySelector.ReCreateItems();


            ItemSelector shoppingCart = new ItemSelector(GUI, buyBoxLayout, "Order")
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
                ToolTip = "Click items to remove them from the shopping cart"
            };
            shoppingCart.ReCreateItems();
            buyBoxLayout.SetComponentPosition(shoppingCart, 2, 0, 2, 9);

            buySelector.OnItemRemoved += shoppingCart.AddItem;
            shoppingCart.OnItemRemoved += buySelector.AddItem;

            Button buyButton = new Button(GUI, buyBoxLayout, "Buy", GUI.DefaultFont, Button.ButtonMode.PushButton, null)
            {
                ToolTip = "Click to order items in the shopping cart"
            };

            buyBoxLayout.SetComponentPosition(buyButton, 4, 9, 1, 1);
        }

        public void CreateGItems()
        {
            
        }

        public void CreateSellTab()
        {
            TabSelector.Tab sellTab = Tabs.AddTab("Sell");

            GridLayout sellBoxLayout = new GridLayout(GUI, sellTab, 10, 4);

            ItemSelector sellSelector = new ItemSelector(GUI, sellBoxLayout, "Items")
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

            sellBoxLayout.SetComponentPosition(sellSelector, 0, 0, 2, 10);

            sellSelector.Items.AddRange(GetResources(Faction.ListResources()));
            sellSelector.ReCreateItems();


            ItemSelector shoppingCart = new ItemSelector(GUI, sellBoxLayout, "Order")
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
            shoppingCart.ReCreateItems();
            sellBoxLayout.SetComponentPosition(shoppingCart, 2, 0, 2, 9);

            sellSelector.OnItemRemoved += shoppingCart.AddItem;
            shoppingCart.OnItemRemoved += sellSelector.AddItem;

            Button sellButton = new Button(GUI, sellBoxLayout, "Sell", GUI.DefaultFont, Button.ButtonMode.PushButton, null)
            {
                ToolTip = "Click to sell items in the order"
            };

            sellBoxLayout.SetComponentPosition(sellButton, 4, 9, 1, 1);
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
        }

        public Faction Faction { get; set; }
    }
}
