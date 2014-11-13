using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Has a number of labeled rows with different GUI components 
    /// on each row.
    /// </summary>
    public class FormLayout : Layout
    {
        public Dictionary<string, FormEntry> Items { get; set; }

        public int NumColumns { get; set; }

        public int MaxRows { get; set; }

        public int EdgePadding { get; set; }
        public bool FitToParent { get; set; }

        public int RowHeight { get; set; }

        public int ColumnWidth { get; set; }

        public FormLayout(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            NumColumns = 1;
            MaxRows = 30; 
            FitToParent = true;
            EdgePadding = 0;
            RowHeight = 32;
            ColumnWidth = 400;
            Items = new Dictionary<string, FormEntry>();
        }

        public void AddItem(string label, GUIComponent item)
        {
            Items[label] = new FormEntry
            {
                Component = item,
                Label = new Label(GUI, this, label, GUI.DefaultFont)
                {
                    Alignment = Drawer2D.Alignment.Right
                }
            };

            UpdateSizes();
        }

        public void RemoveItem(string label)
        {
            Items.Remove(label);
            UpdateSizes();
        }


        public GUIComponent GetItem(string label)
        {
            return Items[label].Component;
        }


        public override void UpdateSizes()
        {
            int c = 0;
            int r = 0;

            if (FitToParent)
            {
                LocalBounds = new Rectangle(LocalBounds.X, LocalBounds.Y, Parent.LocalBounds.Width - EdgePadding, Parent.LocalBounds.Height - EdgePadding);
                ColumnWidth = LocalBounds.Width / NumColumns;
            }

            if(NumColumns <= 0 || MaxRows <= 0)
            {
                return;
            }


            foreach (KeyValuePair<string, FormEntry> item in Items)
            {
                item.Value.Label.LocalBounds
                    = new Rectangle(EdgePadding + c * ColumnWidth, EdgePadding + r * RowHeight, ColumnWidth / 2, RowHeight);

                item.Value.Component.LocalBounds
                    = new Rectangle(EdgePadding + c * ColumnWidth + ColumnWidth / 2, EdgePadding + r * RowHeight, ColumnWidth / 2, RowHeight);
                r++;

                if(r > MaxRows)
                {
                    r = 0;
                    c++;

                    if(c >= NumColumns)
                    {
                        NumColumns++;
                    }
                }
            }

            if (!FitToParent)
            {
                LocalBounds = new Rectangle(LocalBounds.X, LocalBounds.Y, (c + 1) * ColumnWidth + EdgePadding, (r + 1) * RowHeight + EdgePadding);
                ColumnWidth = LocalBounds.Width / NumColumns;
            }
        }
    }

}
