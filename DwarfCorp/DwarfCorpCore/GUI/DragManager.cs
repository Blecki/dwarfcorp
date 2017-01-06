// DragManager.cs
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
    /// Handles draggable items in the GUI, which can be picked and placed.
    /// </summary>
    public class DragManager
    {
        public Dictionary<GUIComponent, DraggableItem> Slots { get; set; }
        public DraggableItem CurrentItem { get; set; }
        public int CurrentDragAmount { get; set; }

        public delegate void DragStarted(DraggableItem item, int amount);

        public event DragStarted OnDragStarted;

        public delegate void DragEnded(DraggableItem fromItem, DraggableItem item, int amount);

        public event DragEnded OnDragEnded;

        public Dictionary<GUIComponent, Dictionary<GUIComponent, bool>> IllegalDrags { get; set; }

        public DragManager()
        {
            IllegalDrags = new Dictionary<GUIComponent, Dictionary<GUIComponent, bool>>();
            Slots = new Dictionary<GUIComponent, DraggableItem>();
            CurrentItem = null;
            CurrentDragAmount = 0;
            OnDragStarted += DragManager_OnDragStarted;
            OnDragEnded += DragManager_OnDragEnded;
        }

        public void DisallowDragging(GUIComponent component1, GUIComponent component2)
        {
            if(component1 == null || component2 == null)
            {
                return;
            }

            if(!IllegalDrags.ContainsKey(component1))
            {
                IllegalDrags[component1] = new Dictionary<GUIComponent, bool>();
            }

            IllegalDrags[component1][component2] = true;
        }

        private void DragManager_OnDragEnded(DraggableItem fromItem, DraggableItem item, int amount)
        {
        }

        private void DragManager_OnDragStarted(DraggableItem item, int amount)
        {
        }

        public GUIComponent GetIntersectingSlot(Rectangle rect)
        {
            foreach(GUIComponent component in Slots.Keys)
            {
                if(component.GlobalBounds.Contains(rect.X + rect.Width / 2, rect.Y + rect.Height / 2))
                {
                    return component;
                }
            }

            return null;
        }

        public void StartDrag(DraggableItem item, int amount)
        {
            CurrentItem = item;
            CurrentDragAmount = amount;
            CurrentItem.Item.CurrentAmount -= amount;
            OnDragStarted.Invoke(CurrentItem, CurrentDragAmount);
        }

        public DraggableItem Drop()
        {
            MouseState mouseState = Mouse.GetState();
            if(CurrentItem != null)
            {
                Rectangle rect = CurrentItem.GlobalBounds;
                rect.X = mouseState.X - rect.Width / 2;
                rect.Y = mouseState.Y - rect.Height / 2;

                GUIComponent drop = GetIntersectingSlot(rect);


                if(drop != null)
                {
                    foreach(GUIComponent slotDropper in IllegalDrags.Keys)
                    {
                        if(CurrentItem.HasAnscestor(slotDropper))
                        {
                            foreach(GUIComponent illegals in IllegalDrags[slotDropper].Keys)
                            {
                                if(drop.HasAnscestor(illegals))
                                {
                                    CurrentItem.Item.CurrentAmount += CurrentDragAmount;
                                    OnDragEnded.Invoke(CurrentItem, null, 0);
                                    return null;
                                }
                            }
                        }
                    }

                    DraggableItem toReturn = null;
                    bool wasNew = false;
                    bool success = Drag(CurrentItem, CurrentDragAmount, drop, out toReturn, out wasNew);

                    if(!success)
                    {
                        CurrentItem.Item.CurrentAmount += CurrentDragAmount;
                        OnDragEnded.Invoke(CurrentItem, null, 0);
                        return null;
                    }
                    else if(wasNew)
                    {
                        OnDragEnded.Invoke(CurrentItem, toReturn, CurrentDragAmount);
                        return toReturn;
                    }
                    else
                    {
                        OnDragEnded.Invoke(CurrentItem, null, CurrentDragAmount);
                        return null;
                    }
                }
                else
                {
                    CurrentItem.Item.CurrentAmount += CurrentDragAmount;
                    OnDragEnded.Invoke(CurrentItem, null, 0);
                    return null;
                }
            }


            CurrentItem = null;
            CurrentDragAmount = 0;
            OnDragEnded.Invoke(CurrentItem, null, 0);
            return null;
        }

        public bool IsDragValid(DraggableItem item, GUIComponent slot)
        {
            if(!Slots.ContainsKey(slot))
            {
                return true;
            }
            else
            {
                DraggableItem currentItem = Slots[slot];
                GItem gItem = currentItem.Item;
                if(gItem.Name != item.Item.Name)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public bool Drag(DraggableItem item, int amount, GUIComponent slot, out DraggableItem itemDraggedTo, out bool wasNew)
        {
            if(!Slots.ContainsKey(slot))
            {
                DraggableItem currentItem = new DraggableItem(item.GUI, slot, new GItem(item.Item.ResourceType, item.Item.Image, item.Item.Tint, item.Item.MinAmount, item.Item.MaxAmount, item.Item.CurrentAmount, item.Item.Price));
                GItem gItem = currentItem.Item;


                if(gItem.CurrentAmount + amount <= gItem.MaxAmount)
                {
                    gItem.CurrentAmount += amount;
                }


                Slots[slot] = currentItem;
                itemDraggedTo = currentItem;
                wasNew = false;
                return true;
            }
            else
            {
                DraggableItem currentItem = Slots[slot];

                if(currentItem != null)
                {
                    GItem gItem = currentItem.Item;
                    if(gItem.Name != item.Item.Name)
                    {
                        wasNew = false;
                        itemDraggedTo = null;
                        return false;
                    }
                    else
                    {
                        if(gItem.CurrentAmount + amount <= gItem.MaxAmount)
                        {
                            gItem.CurrentAmount += amount;
                            wasNew = false;
                            itemDraggedTo = currentItem;
                            return true;
                        }
                    }
                }
                else
                {
                    DraggableItem ditem = new DraggableItem(item.GUI, slot, new GItem(item.Item.ResourceType, item.Item.Image, item.Item.Tint, item.Item.MinAmount, item.Item.MaxAmount, 0, item.Item.Price));
                    Slots[slot] = ditem;
                    ditem.LocalBounds = new Rectangle(0, 0, 32, 32);
                    ditem.Item.CurrentAmount += amount;
                    itemDraggedTo = ditem;
                    wasNew = true;
                    return true;
                }
                wasNew = false;
                itemDraggedTo = null;
                return false;
            }
        }
    }

}