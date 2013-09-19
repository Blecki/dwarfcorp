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
    public class ListItem : SillyGUIComponent
    {
        public enum SelectionMode
        {
            Selector,
            ButtonList
        }

        public string Label { get; set; }
        public Texture2D Texture { get; set; }
        public Rectangle TextureBounds { get; set; }
        public Color TextColor { get; set; }
        public Color TextStrokeColor { get; set; }
        public Color ToggledColor { get; set;}
        public Color HoverColor { get; set; }
        public bool Toggleable { get; set;}
        public bool IsToggled { get; set;}
        public SelectionMode Mode { get; set; }

        public ListItem(SillyGUI gui, SillyGUIComponent parent, string label, Texture2D tex, Rectangle texBounds) :
            base(gui, parent)
        {
            Label = label;
            Texture = tex;
            TextureBounds = texBounds;
            TextColor = gui.DefaultTextColor;
            TextStrokeColor = gui.DefaultStrokeColor;
            ToggledColor = Color.DarkRed;
            HoverColor = new Color(255, 20, 20);
            Toggleable = true;
            IsToggled = false;
            Mode = SelectionMode.Selector;
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            //Drawer2D.DrawRect(GlobalBounds, Color.White, Color.Black, 1.0f);
            if (IsMouseOver)
            {
                Drawer2D.DrawStrokedText(batch, Label, GUI.DefaultFont, new Vector2(GlobalBounds.Left, GlobalBounds.Top), HoverColor, TextStrokeColor);
            }
            else
            {
                if (!IsToggled || Mode == SelectionMode.ButtonList)
                {
                    Drawer2D.DrawStrokedText(batch, Label, GUI.DefaultFont, new Vector2(GlobalBounds.Left, GlobalBounds.Top), TextColor, TextStrokeColor);
                }
                else
                {
                    Drawer2D.DrawStrokedText(batch, Label, GUI.DefaultFont, new Vector2(GlobalBounds.Left, GlobalBounds.Top), ToggledColor, TextStrokeColor);
                }
            }
            
            if(Texture != null)
            {
                batch.Draw(Texture, new Vector2(GlobalBounds.Top, GlobalBounds.Left - 10), TextureBounds, Color.White, 0.0f, Vector2.Zero, Vector2.Zero, SpriteEffects.None, 0);
            }

            base.Render(time, batch);
        }

    }

    public class ListSelector : SillyGUIComponent
    {
        public event ClickedDelegate OnItemClicked;
        public List<ListItem> Items { get; set; }
        public ListItem SelectedItem { get; set; }
        public Color BackgroundColor { get; set; }
        public Color StrokeColor { get; set; }
        public float StrokeWeight { get; set; }
        public string Label { get; set; }
        public Color LabelColor { get; set; }
        public Color LabelStroke { get; set; }
        public ListItem.SelectionMode Mode { get; set; }
        public bool DrawPanel { get; set; }
  

        public ListSelector(SillyGUI gui, SillyGUIComponent parent) :
            base(gui, parent)
        {
            Items = new List<ListItem>();
            SelectedItem = null;
            BackgroundColor = new Color(0, 0, 0, 200);

            DrawPanel = true;
            StrokeColor = gui.DefaultStrokeColor;
            StrokeWeight = 2;
            LabelColor = gui.DefaultTextColor;
            LabelStroke = gui.DefaultStrokeColor;
            Mode = ListItem.SelectionMode.Selector;
            OnItemClicked += ItemClicked;
        }

        public void ClearItems()
        {
            foreach(ListItem item in Items)
            {
                RemoveChild(item);
            }

            Items.Clear();
        }

        public void AddItem(ListItem item)
        {
            Items.Add(item);
            item.Mode = Mode;
        }

        public void AddItem(string text)
        {
            int top = 30 + 25 * Items.Count;
            int left = 5;
            
            ListItem item = new ListItem(GUI, this, text, null, new Rectangle());
            item.Toggleable = true;

            item.LocalBounds = new Rectangle(left, top, LocalBounds.Width, 25);
            item.OnClicked += new ClickedDelegate(delegate { item_OnClicked(item); });
            AddItem(item);

            if (SelectedItem == null)
            {
                SelectedItem = item;
                item.IsToggled = true;
            }
        }


        void ItemClicked()
        {

        }

        void item_OnClicked(ListItem item)
        {
            if (SelectedItem != null)
            {
                SelectedItem.IsToggled = false;
            }
            SelectedItem = item;
            SelectedItem.IsToggled = true;
            OnItemClicked();
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            if (IsVisible)
            {
                if (DrawPanel)
                {
                    GUI.Skin.RenderPanel(GlobalBounds, batch);
                }

                Drawer2D.DrawStrokedText(batch, Label, GUI.DefaultFont, new Vector2(GlobalBounds.X + 5, GlobalBounds.Y + 5), LabelColor, LabelStroke);
                //Drawer2D.DrawRect(GlobalBounds, BackgroundColor, StrokeColor, StrokeWeight);
            }
            base.Render(time, batch);
        }
    }
}
