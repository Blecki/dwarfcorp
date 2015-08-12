// FormLayout.cs
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
        public SpriteFont LabelFont { get; set; }

        public FormLayout(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            LabelFont = GUI.DefaultFont;
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
                Label = new Label(GUI, this, label, LabelFont)
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
