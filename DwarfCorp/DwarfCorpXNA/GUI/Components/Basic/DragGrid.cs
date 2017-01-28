// DragGrid.cs
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
    /// This is a GUI component which manages DragItems and puts them on a grid.
    /// Items can be dragged from one cell of the grid to another.
    /// </summary>
    public class DragGrid : GUIComponent
    {
        public DragManager DragManager { get; set; }
        public List<DraggableItem> Items { get; set; }
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }
        public int TotalRows { get; set; }
        public int TotalCols { get; set; }
        public GridLayout Layout { get; set; }
        public bool DrawGrid { get; set; }
        public bool DrawBackground { get; set; }
        public Color BackColor { get; set; }
        public Color BorderColor { get; set; }

        public delegate void ItemCreated(DraggableItem item);

        public event ItemCreated OnItemCreated;

        public delegate void ItemDestroyed(DraggableItem item);

        public event ItemDestroyed OnItemDestroyed;

        public delegate void Changed();

        public event Changed OnChanged;

        public DragGrid(DwarfGUI gui, GUIComponent parent, DragManager dragManager, int gridWidth, int gridHeight) :
            base(gui, parent)
        {
            Items = new List<DraggableItem>();
            DragManager = dragManager;
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            SetupLayout();
            DrawGrid = false;
            DragManager.OnDragEnded += DragManager_OnDragEnded;
            DragManager.OnDragStarted += DragManager_OnDragStarted;
            OnItemCreated += DragGrid_OnItemCreated;
            OnItemDestroyed += DragGrid_OnItemDestroyed;
            OnChanged += DragGrid_OnChanged;
            DrawBackground = true;
            BackColor = new Color(255, 255, 255, 100);
            BorderColor = new Color(0, 0, 0, 100);
        }

        private void DragGrid_OnChanged()
        {
        }

        private void DragGrid_OnItemRemoved(DraggableItem item, int amount)
        {
        }

        private void DragGrid_OnItemAdded(DraggableItem item, int amount)
        {
        }

        private void DragGrid_OnItemDestroyed(DraggableItem item)
        {
        }

        private void DragGrid_OnItemCreated(DraggableItem item)
        {
        }

        private void DragManager_OnDragStarted(DraggableItem item, int amount)
        {
            DrawGrid = true;
        }

        private void DragManager_OnDragEnded(DraggableItem dItem, DraggableItem newSpawnedItem, int amount)
        {
            MouseState m = Mouse.GetState();
            DrawGrid = false;

            if(GlobalBounds.Contains(m.X, m.Y))
            {
                if(newSpawnedItem != null)
                {
                    OnItemCreated.Invoke(newSpawnedItem);
                    Items.Add(newSpawnedItem);
                    newSpawnedItem.LocalBounds = new Rectangle(0, 0, GridWidth, GridHeight);
                    newSpawnedItem.OnDragStarted += () => DragManager.StartDrag(newSpawnedItem, 1);

                    newSpawnedItem.OnDragEnded += () => DragManager.Drop();
                }
            }


            if(Items.Contains(dItem) && dItem.Item.CurrentAmount <= 0)
            {
                DragManager.Slots[dItem.Parent] = null;
                dItem.Parent.RemoveChild(dItem);
                Items.Remove(dItem);
                OnItemDestroyed.Invoke(dItem);
            }

            OnChanged.Invoke();
        }

        public override void Update(DwarfTime time)
        {
            base.Update(time);
        }


        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            if(DrawBackground)
            {
                Drawer2D.DrawRect(batch, GlobalBounds, BorderColor, 2);
                Drawer2D.FillRect(batch, GlobalBounds, BackColor);
            }

            if(DrawGrid)
            {
                MouseState m = Mouse.GetState();
                for(int r = 0; r < TotalRows; r++)
                {
                    for(int c = 0; c < TotalCols; c++)
                    {
                        Rectangle rect = new Rectangle(c, r, 1, 1);
                        GUIComponent slot = Layout.ComponentPositions[rect];
                        Rectangle draw = slot.GlobalBounds;
                        //draw.X += 5;
                        //draw.Y += 5;
                        draw.Width += 5;
                        draw.Height += 5;
                        Drawer2D.DrawRect(batch, draw, new Color(0, 0, 0, 50), 2);

                        if(draw.Contains(m.X, m.Y))
                        {
                            Drawer2D.FillRect(batch, draw, new Color(100, 100, 0, 100));
                        }
                    }
                }
            }
            else
            {
                for(int r = 0; r < TotalRows; r++)
                {
                    for(int c = 0; c < TotalCols; c++)
                    {
                        Rectangle rect = new Rectangle(c, r, 1, 1);
                        GUIComponent slot = Layout.ComponentPositions[rect];
                        Rectangle draw = slot.GlobalBounds;
                        //draw.X += 5;
                        //draw.Y += 5;
                        draw.Width += 5;
                        draw.Height += 5;
                        Drawer2D.DrawRect(batch, draw, new Color(0, 0, 0, 25), 2);
                    }
                }
            }

            base.Render(time, batch);
        }

        public void SetupLayout()
        {
            Rectangle globalRect = GlobalBounds;
            TotalRows = globalRect.Height / GridHeight;
            TotalCols = globalRect.Width / GridWidth;
            Layout = new GridLayout(GUI, this, TotalRows, TotalCols);

            for(int r = 0; r < TotalRows; r++)
            {
                for(int c = 0; c < TotalCols; c++)
                {
                    GUIComponent slot = new GUIComponent(GUI, Layout);
                    Layout.SetComponentPosition(slot, c, r, 1, 1);
                    DragManager.Slots[slot] = null;
                }
            }
        }

        public void AddItem(GUIComponent slot, GItem item)
        {
            DraggableItem dItem = new DraggableItem(GUI, slot, item)
            {
                ToolTip = item.Name
            };
            DragManager.Slots[slot] = dItem;
            dItem.OnDragStarted += () => DragManager.StartDrag(dItem, 1);

            dItem.OnDragEnded += () => DragManager.Drop();

            dItem.LocalBounds = new Rectangle(0, 0, GridWidth, GridHeight);
            slot.ClearChildren();
            slot.AddChild(dItem);
            Items.Add(dItem);
        }


        public bool AddItem(GItem item, int r, int c)
        {
            Rectangle rect = new Rectangle(c, r, 1, 1);
            if(Layout.ComponentPositions.ContainsKey(rect))
            {
                GUIComponent slot = Layout.ComponentPositions[rect];
                AddItem(slot, item);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void AddItem(GItem item)
        {
            int index = Items.Count;
            int r = index / TotalCols;
            int c = index % TotalCols;


            AddItem(item, r, c);
        }
    }

}