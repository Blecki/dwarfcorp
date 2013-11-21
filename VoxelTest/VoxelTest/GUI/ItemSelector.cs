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

    public class ItemSelector : SillyGUIComponent
    {
        public delegate void ItemChanged(GItem item);

        public event ItemChanged OnItemChanged;

        public List<GItem> Items { get; set; }
        public string Filter { get; set; }
        public GridLayout Layout { get; set; }

        public ItemSelector(SillyGUI gui, SillyGUIComponent parent) :
            base(gui, parent)
        {
            Items = new List<GItem>();
            Filter = "";
            Layout = new GridLayout(gui, this, 15, 5);
            OnItemChanged += ItemSelector_OnItemChanged;
        }

        public void SetItemNumber(GItem item, float number)
        {
            item.CurrentAmount = (int) number;
            OnItemChanged.Invoke(item);
        }

        private void ItemSelector_OnItemChanged(GItem item)
        {
        }

        public List<GItem> GetItemsWithTag(string tag)
        {
            if(tag == "")
            {
                return Items;
            }
            else
            {
                List<GItem> toReturn = new List<GItem>();

                foreach(GItem item in Items)
                {
                    if(item.Tags.Contains(tag))
                    {
                        toReturn.Add(item);
                    }
                }
                return toReturn;
            }
        }


        public void UpdateCurrentItems()
        {
            RemoveChild(Layout);

            List<GItem> toDisplay = GetItemsWithTag(Filter);

            int rows = Math.Max(toDisplay.Count, 8);

            Layout = new GridLayout(GUI, this, rows, 6);

            for(int i = 0; i < toDisplay.Count; i++)
            {
                ImagePanel image = new ImagePanel(GUI, Layout, toDisplay[i].Image);
                image.KeepAspectRatio = true;
                Label label = new Label(GUI, Layout, toDisplay[i].Name, GUI.DefaultFont);
                Label priceLabel = new Label(GUI, Layout, toDisplay[i].Price.ToString("C"), GUI.DefaultFont);
                SpinBox spinbox = new SpinBox(GUI, Layout, "", toDisplay[i].CurrentAmount, toDisplay[i].MinAmount, toDisplay[i].MaxAmount, SpinBox.SpinMode.Integer);
                GItem item = toDisplay[i];
                spinbox.OnValueChanged += (SpinBox x) => SetItemNumber(item, x.SpinValue);

                Layout.SetComponentPosition(image, 0, i, 1, 1);
                Layout.SetComponentPosition(label, 1, i, 1, 1);
                Layout.SetComponentPosition(priceLabel, 2, i, 1, 1);
                Layout.SetComponentPosition(spinbox, 3, i, 2, 1);
            }
        }
    }

}