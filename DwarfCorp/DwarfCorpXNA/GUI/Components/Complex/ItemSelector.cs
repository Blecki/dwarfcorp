// ItemSelector.cs
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

        public MoneyEditor MoneyEdit { get; set; }
        public LineEdit SearchBox { get; set; }

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
        public bool AllowControlClick { get; set; }
        public bool AllowAltClick { get; set; }

        public ScrollView ScrollArea { get; set; }

        public float PerItemCost { get; set; }

        public List<GItem> FilteredItems { get; set; }
        public List<Resource.ResourceTags> LikedThings { get; set; }
        public List<Resource.ResourceTags> HatedThings { get; set; }
        public List<Resource.ResourceTags> CommonThings { get; set; }
        public List<Resource.ResourceTags> RareThings { get; set; }
        public int ComputeSpace()
        {
            int total = 0;

            foreach (GItem item in Items)
            {
                total += item.CurrentAmount;
            }


            return total;
        }

        public float ComputeShipping()
        {
            float total = 0;

            foreach (GItem item in Items)
            {
                total += PerItemCost * item.CurrentAmount;
            }

            return total;
        }

        public DwarfBux ComputeTotal()
        {
            DwarfBux total = 0;

            foreach(GItem item in Items)
            {
                total += item.CurrentAmount * item.Price;
                total += PerItemCost * item.CurrentAmount;
            }

            return total;
        }

        bool IsCommon(Resource resource)
        {
            return CommonThings.Any(tags => resource.Tags.Contains(tags));
        }

        bool IsRare(Resource resource)
        {
            return RareThings.Any(tags => resource.Tags.Contains(tags));
        }

        bool IsLiked(Resource resource)
        {
            return LikedThings.Any(tags => resource.Tags.Contains(tags));
        }

        bool IsHated(Resource resource)
        {
            return HatedThings.Any(tags => resource.Tags.Contains(tags));
        }

        private WorldManager World { get; set; }

        public ItemSelector(DwarfGUI gui, GUIComponent parent, WorldManager world, string title, bool hasMoney, bool moneyEditable) :
            base(gui, parent, title)
        {
            World = world;
            LikedThings = new List<Resource.ResourceTags>();
            HatedThings = new List<Resource.ResourceTags>();
            CommonThings = new List<Resource.ResourceTags>();
            RareThings = new List<Resource.ResourceTags>();

            Columns = new List<Column>()
            {
                Column.Image,
                Column.Name,
                Column.PricePerItem
            };

            Items = new List<GItem>();
            FilteredItems = new List<GItem>();
            GridLayout layout = new GridLayout(GUI, this, 11, 1);

            if (hasMoney)
            {
                MoneyEdit = new MoneyEditor(GUI, layout, moneyEditable, 100.0f, 50.0f)
                {
                    ToolTip = "Money to trade"
                };
                layout.SetComponentPosition(MoneyEdit, 0, 9, 1, 1);
            }

            SearchBox = new LineEdit(GUI, layout, "")
            {
                Prompt = "Search...",
                ToolTip = "Type to search for keywords"
            };
            layout.SetComponentPosition(SearchBox, 0, 10, 1, 1);
            SearchBox.OnTextModified += SearchBox_OnTextModified;
            ScrollArea = new ScrollView(GUI, layout)
            {
                DrawBorder = false,
                WidthSizeMode = SizeMode.Fit
            };

            layout.UpdateSizes();
            layout.SetComponentPosition(ScrollArea, 0, 0, 1, 9);
            ReCreateItems();
            OnItemChanged += ItemSelector_OnItemChanged;
            OnItemRemoved += ItemSelector_OnItemRemoved;
            OnItemAdded += ItemSelector_OnItemAdded;
            Behavior = ClickBehavior.RemoveItem;
            AllowShiftClick = true;
            AllowControlClick = true;
            AllowAltClick = true;
        }

        void Filter()
        {
            FilteredItems = new List<GItem>();

            foreach (GItem item in Items)
            {
                if (SearchBox.Text == "" || item.Name.ToUpper().Contains(SearchBox.Text.ToUpper()) && item.CurrentAmount > 0)
                {
                    FilteredItems.Add(item);
                }
            }
        }

        void SearchBox_OnTextModified(string arg)
        {
            ScrollArea.ResetScroll();
            Filter();
            ReCreateItems();
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
                existingItem = new GItem(item.ResourceType, item.Image, item.Tint, 0, 10000, amount, item.Price)
                {
                    CurrentAmount = amount
                };
                Items.Add(existingItem);
                Filter();
                ReCreateItems();
                OnItemChanged(existingItem);
                return;
            }
            else
            {
                existingItem.CurrentAmount += amount;
                ReCreateItems();
                OnItemChanged(existingItem);
            }
        }

        public float GetPrice(GItem item)
        {
            float price = item.Price;
            if (IsRare(item.ResourceType))
            {
                price *= 2;
            }
            else if (IsCommon(item.ResourceType))
            {
                price *= 0.5f;
            }

            return price;
        }

        public void UpdateItem(Column columnType, int row, int column)
        {
            Rectangle key = new Rectangle(column, row, 1, 1);

            if(!Layout.ComponentPositions.ContainsKey(key))
            {
                return;
            }
            GItem item = FilteredItems[row - 1];
            GUIComponent component = Layout.ComponentPositions[key];
            string tooltip = item.ResourceType.Type + "\n" + item.ResourceType.Description + "\n" +
                             item.ResourceType.GetTagDescription(" , ");

            if (item.ResourceType.FoodContent > 0)
            {
                tooltip += "\n" + item.ResourceType.FoodContent + " energy";
            }
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
                    image.Tint = item.Tint;
                    image.ToolTip = tooltip;
                    break;

                case Column.Name:
                    Label label = component as Label;

                    if (label == null) break;

                    label.Text = item.Name;
                    label.ToolTip = tooltip;
                    break;

                case Column.PricePerItem:
                    Label priceLabel = component as Label;

                    if (priceLabel == null) break;
                    float price = GetPrice(item);
                    priceLabel.Text = price.ToString("C");

                    break;


                case Column.TotalPrice:
                    Label totalpriceLabel = component as Label;

                    if (totalpriceLabel == null) break;

                    totalpriceLabel.Text = (item.CurrentAmount * GetPrice(item)).ToString("C");

                    break;

               
            }
        }

        public GUIComponent CreateItem(Column columnType, GItem item, int row, int column)
        {
            string tooltip = item.ResourceType.Type + "\n" + item.ResourceType.Description + "\n" +
                 item.ResourceType.GetTagDescription(" , ");

            if (item.ResourceType.FoodContent > 0)
            {
                tooltip += "\n" + item.ResourceType.FoodContent + " energy";
            }

            switch(columnType)
            {
               case Column.Amount:
                    Label amountLabel = new Label(GUI, Layout, item.CurrentAmount.ToString(), GUI.SmallFont)
                    {
                        ToolTip = "Total Amount",
                        Alignment = Drawer2D.Alignment.Right
                    };
                   
                    Layout.SetComponentPosition(amountLabel, column, row, 1, 1);
                    return amountLabel;
                    
                case Column.Image:
                    ImagePanel image = new ImagePanel(GUI, Layout, item.Image)
                    {
                        KeepAspectRatio = true,
                        ConstrainSize = true,
                        Tint = item.Tint,
                        ToolTip = tooltip,
                    };
                    Layout.SetComponentPosition(image, column, row, 1, 1);

                    return image;

                case Column.Name:
                    Label label = new Label(GUI, Layout, item.Name, GUI.SmallFont)
                    {
                        ToolTip = tooltip
                    };
                                            
                    Layout.SetComponentPosition(label, column, row, 1, 1);

                    return label;
                
                case Column.PricePerItem:
                    Label priceLabel = new Label(GUI, Layout, GetPrice(item).ToString("C"), GUI.SmallFont)
                    {
                        ToolTip = "Price per item",
                    };
                    Layout.SetComponentPosition(priceLabel, column, row, 1, 1);

                    return priceLabel;


                case Column.TotalPrice:
                    Label totalLabel = new Label(GUI, Layout, (GetPrice(item) * item.CurrentAmount).ToString("C"), GUI.SmallFont)
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
            bool altPressed = state.IsKeyDown(Keys.LeftAlt) || state.IsKeyDown(Keys.RightAlt);
            bool controlPressed = state.IsKeyDown(Keys.LeftControl) || state.IsKeyDown(Keys.RightControl);
            
            int amount = 1;

            if (shiftPressed && AllowShiftClick)
            {
                amount = item.CurrentAmount;
                World.ShowToolPopup("Moved " + amount);
            }
            else if (controlPressed && AllowControlClick)
            {
                amount = Math.Min(item.CurrentAmount, 5);
                World.ShowToolPopup("Moved " + amount);
            }
            else if (altPressed && AllowAltClick)
            {
                amount = Math.Min(item.CurrentAmount, 10);
                World.ShowToolPopup("Moved " + amount);
            }

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
                            Filter();
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
            if (FilteredItems.Count == 0)
            {
                Filter();
            }
            RemoveChild(Layout);

            if(Items.Count == 0)
            {
                ScrollArea.RemoveChild(Layout);
                Layout = new GridLayout(GUI, ScrollArea, 1, 1);
                Label label = new Label(GUI, Layout, NoItemsMessage, GUI.DefaultFont);

                Layout.SetComponentPosition(label, 0, 0, 1, 1);

                return;
            }

            int rows = Math.Max(FilteredItems.Count, 6);
            ScrollArea.RemoveChild(Layout);
 
            Layout = new GridLayout(GUI, ScrollArea, rows + 1, Columns.Count)
            {
                LocalBounds = new Rectangle(-ScrollArea.ScrollX, -ScrollArea.ScrollY, Math.Max(ScrollArea.LocalBounds.Width - 64, 200), rows * 40),
                HeightSizeMode = SizeMode.Fixed
            };
            
            for (int i = 0; i < FilteredItems.Count; i++)
            {
                GItem currentResource = FilteredItems[i];
                int j = 0;
                foreach(Column column in Columns)
                {
                    GUIComponent item = CreateItem(column, FilteredItems[i], i + 1, j);
                    item.OnClicked += () => ItemClicked(currentResource);
                    int row = i;
                    item.OnHover += () => HighlightRow(row + 1);
                    j++;
                }
            }

            Layout.UpdateSizeRecursive();

        }

        public override void Update(DwarfTime time)
        {
            if(!IsMouseOver)
            {
                Layout.RowHighlight = -1;
            }
            base.Update(time);
        }


    }

    public class MoneyEditor : GUIComponent
    {
        public float MaxMoney { get; set; }
        public float CurrentMoney 
        {
            get { return _money; }
            set
            {
                _money = Math.Max(0, Math.Min(value, MaxMoney));

                if (Editor != null)
                {
                    Editor.Text = ((int) (_money)).ToString("D");
                }
            }
        }

        private float _money;
        public LineEdit Editor { get; set; }
        public ImagePanel Image { get; set; }
        public Label Text { get; set; }

        public delegate void MoneyChanged(float amount);

        public event MoneyChanged OnMoneyChanged;

        protected virtual void InvokeMoneyChanged(float amount)
        {
            MoneyChanged handler = OnMoneyChanged;
            if (handler != null) handler(amount);
        }

        public MoneyEditor(DwarfGUI	gui, GUIComponent parent, bool editable, float maxmoney, float currentMoney) :
            base(gui, parent)
        {
            MaxMoney = maxmoney;
            
            GridLayout layout = new GridLayout(GUI, this, 1, 5);
            Editor = new LineEdit(GUI, layout, "0")
            {
                TextMode = LineEdit.Mode.Numeric,
                Prefix = "$",
                IsEditable = editable
            };
            CurrentMoney = currentMoney;

            Editor.OnTextModified += Editor_OnTextModified;
            layout.SetComponentPosition(Editor, 2, 0, 3, 1);

            Image = new ImagePanel(GUI, layout,
                new NamedImageFrame(ContentPaths.Entities.DwarfObjects.coinpiles, 32, 1, 0))
            {
                KeepAspectRatio = true,
                ConstrainSize = true
            };

            layout.SetComponentPosition(Image, 0, 0, 1, 1);

            Text = new Label(GUI, layout, "Money: ", GUI.SmallFont)
            {
                WordWrap = false,
                Truncate = false
            };

            layout.SetComponentPosition(Text, 1, 0, 1, 1);
            
        }

        void Editor_OnTextModified(string arg)
        {
            float result = 0;
            if (float.TryParse(arg, out result))
            {
                _money = result;

                if (_money > MaxMoney)
                {
                    _money = MaxMoney;
                    Editor.Text = ((int)(MaxMoney)).ToString("D");
                }
                else if (_money < 0)
                {
                    _money = 0;
                    Editor.Text = "0";
                }

                InvokeMoneyChanged(_money);
            }
        }
    }

}
