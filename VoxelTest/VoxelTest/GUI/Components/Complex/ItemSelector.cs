using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    /// <summary>
    ///This GUI component holds a list of items that the player can drag around.
    /// </summary>
    public class ItemSelector : GroupBox
    {

        public enum Column
        {
            Image,
            Name,
            PricePerItem,
            TotalPrice,
            Amount,
            ArrowRight,
            ArrowLeft
        }

        public enum ClickBehavior
        {
            RemoveItem,
            AddItem,
            None
        }

        public delegate void ItemChanged(GItem item);

        public delegate void ItemRemoved(GItem item, int amount);

        public delegate void ItemAdded(GItem item, int amount);

        public event ItemChanged OnItemChanged;
        public event ItemAdded OnItemAdded;

        public string NoItemsMessage { get; set; }

        protected virtual void OnOnItemAdded(GItem item, int amount)
        {
            ItemAdded handler = OnItemAdded;
            if(handler != null)
            {
                handler(item, amount);
            }
        }

        public event ItemRemoved OnItemRemoved;

        protected virtual void OnOnItemRemoved(GItem item, int amount)
        {
            ItemRemoved handler = OnItemRemoved;
            if(handler != null)
            {
                handler(item, amount);
            }
        }

        public List<GItem> Items { get; set; }
        public GridLayout Layout { get; set; }

        public List<Column> Columns { get; set; }

        public ClickBehavior Behavior { get; set; }

        public bool AllowShiftClick { get; set; }

        public int ComputeSpace()
        {
            int total = 0;

            foreach (GItem item in Items)
            {
                total += item.CurrentAmount;
            }


            return total;
        }

        public float ComputeTotal()
        {
            float total = 0;

            foreach(GItem item in Items)
            {
                total += item.CurrentAmount * item.Price;
            }

            return total;
        }

        public ItemSelector(DwarfGUI gui, GUIComponent parent, string title) :
            base(gui, parent, title)
        {
            Columns = new List<Column>()
            {
                Column.Image,
                Column.Name,
                Column.PricePerItem
            };

            Items = new List<GItem>();
            Layout = new GridLayout(gui, this, 14, 5);
            OnItemChanged += ItemSelector_OnItemChanged;
            OnItemRemoved += ItemSelector_OnItemRemoved;
            OnItemAdded += ItemSelector_OnItemAdded;
            Behavior = ClickBehavior.RemoveItem;
            AllowShiftClick = true;
        }

        void ItemSelector_OnItemAdded(GItem item, int amount)
        {
          
        }

        void ItemSelector_OnItemRemoved(GItem item, int amount)
        {
         
        }

        public void SetItemNumber(GItem item, float number)
        {
            item.CurrentAmount = (int) number;
            OnItemChanged.Invoke(item);
        }

        private void ItemSelector_OnItemChanged(GItem item)
        {
            UpdateItems();
        }


        public void AddItem(GItem item, int amount)
        {
            GItem existingItem = Items.FirstOrDefault(myItem => myItem.Name == item.Name);

            if(existingItem == null)
            {
                existingItem = new GItem(item.Name, item.Image, 0, 10000, amount, item.Price, item.Tags)
                {
                    CurrentAmount = amount
                };
                Items.Add(existingItem);
                ReCreateItems();
                OnItemChanged(existingItem);
                return;
            }
            else
            {
                existingItem.CurrentAmount += amount;
                UpdateItems();
                OnItemChanged(existingItem);
            }

 
        }

        public void UpdateItem(Column columnType, int row, int column)
        {
            Rectangle key = new Rectangle(column, row, 1, 1);

            if(!Layout.ComponentPositions.ContainsKey(key))
            {
                return;
            }
            GItem item = Items[row - 1];
            GUIComponent component = Layout.ComponentPositions[key];

            switch (columnType)
            {
                case Column.Amount:
                    Label amountLabel = component as Label;

                    if(amountLabel == null) break;

                    amountLabel.Text = item.CurrentAmount.ToString();

                    break;

                case Column.Image:
                    ImagePanel image = component as ImagePanel;

                    if (image == null) break;

                    image.Image = item.Image;

                    break;

                case Column.Name:
                    Label label = component as Label;

                    if (label == null) break;

                    label.Text = item.Name;

                    break;

                case Column.PricePerItem:
                    Label priceLabel = component as Label;

                    if (priceLabel == null) break;

                    priceLabel.Text = item.Price.ToString("C");

                    break;


                case Column.TotalPrice:
                    Label totalpriceLabel = component as Label;

                    if (totalpriceLabel == null) break;

                    totalpriceLabel.Text = (item.CurrentAmount * item.Price).ToString("C");

                    break;

               
            }
        }

        public GUIComponent CreateItem(Column columnType, GItem item, int row, int column)
        {
            switch(columnType)
            {
               case Column.Amount:
                    Label amountLabel = new Label(GUI, Layout, item.CurrentAmount.ToString(), GUI.DefaultFont)
                    {
                        ToolTip = "Total Amount"
                    };
                   
                    Layout.SetComponentPosition(amountLabel, column, row, 1, 1);
                    return amountLabel;
                    
                case Column.Image:
                    ImagePanel image = new ImagePanel(GUI, Layout, item.Image)
                    {
                        KeepAspectRatio = true
                    };
                    Layout.SetComponentPosition(image, column, row, 1, 1);

                    return image;

                case Column.Name:
                    Label label = new Label(GUI, Layout, item.Name, GUI.DefaultFont);
                    Layout.SetComponentPosition(label, column, row, 1, 1);

                    return label;
                
                case Column.PricePerItem:
                    Label priceLabel = new Label(GUI, Layout, item.Price.ToString("C"), GUI.DefaultFont)
                    {
                        ToolTip = "Price per item"
                    };
                    Layout.SetComponentPosition(priceLabel, column, row, 1, 1);

                    return priceLabel;


                case Column.TotalPrice:
                    Label totalLabel = new Label(GUI, Layout, (item.Price * item.CurrentAmount).ToString("C"), GUI.DefaultFont)
                    {
                        ToolTip = "Total price"
                    };
                    Layout.SetComponentPosition(totalLabel, column, row, 1, 1);

                    return totalLabel;

                case Column.ArrowRight:
                    ImagePanel panel = new ImagePanel(GUI, Layout, GUI.Skin.GetSpecialFrame(GUISkin.Tile.SmallArrowRight))
                    {
                        KeepAspectRatio = true,
                        ConstrainSize = true
                    };
                    Layout.SetComponentPosition(panel, column, row, 1, 1);
                    return panel;

                case Column.ArrowLeft:
                    ImagePanel panelLeft = new ImagePanel(GUI, Layout, GUI.Skin.GetSpecialFrame(GUISkin.Tile.SmallArrowLeft))
                    {
                        KeepAspectRatio = true,
                        ConstrainSize = true
                    };
                    Layout.SetComponentPosition(panelLeft, column, row, 1, 1);
                    return panelLeft;
            }

            return null;
        }

        void ItemClicked(GItem item)
        {
            KeyboardState state = Keyboard.GetState();

            bool shiftPressed = state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift);


            int amount = (shiftPressed && AllowShiftClick) ? item.CurrentAmount : 1;

            switch(Behavior)
            {
                    case ClickBehavior.AddItem:
                        item.CurrentAmount += amount;
                        OnItemAdded(item, amount);
                        OnItemChanged(item);
                        break;

                    case ClickBehavior.None:
                        break;

                    case ClickBehavior.RemoveItem:
                        item.CurrentAmount -= amount;

                    if(item.CurrentAmount <= 0)
                    {
                        Items.Remove(item);
                        ReCreateItems();
                    }

                        OnItemRemoved(item, amount);
                        OnItemChanged(item);
                        break;
            }
        }

        public void UpdateItems()
        {
            for(int i = 0; i < Items.Count; i++)
            {

                int j = 0;
                foreach(Column column in Columns)
                {
                    UpdateItem(column, i + 1, j);
                    j++;
                }
            }
        }

        public void HighlightRow(int row)
        {
            Layout.HighlightRow(row, new Color(255, 100, 100, 200));
        }

        public void ReCreateItems()
        {
            RemoveChild(Layout);

            List<GItem> toDisplay = Items;


            if(Items.Count == 0)
            {
                Layout = new GridLayout(GUI, this, 1, 1);
                Label label = new Label(GUI, Layout, NoItemsMessage, GUI.DefaultFont);

                Layout.SetComponentPosition(label, 0, 0, 1, 1);

                return;
            }

            int rows = Math.Max(toDisplay.Count, 6);

            Layout = new GridLayout(GUI, this, rows + 1, 6);

            for(int i = 0; i < toDisplay.Count; i++)
            {
                GItem currentResource = toDisplay[i];
                int j = 0;
                foreach(Column column in Columns)
                {
                    GUIComponent item = CreateItem(column, toDisplay[i], i + 1, j);
                    item.OnClicked += () => ItemClicked(currentResource);
                    int row = i;
                    item.OnHover += () => HighlightRow(row + 1);
                    j++;
                }
            }
        }

        public override void Update(GameTime time)
        {
            if(!IsMouseOver)
            {
                Layout.RowHighlight = -1;
            }
            base.Update(time);
        }


    }

}