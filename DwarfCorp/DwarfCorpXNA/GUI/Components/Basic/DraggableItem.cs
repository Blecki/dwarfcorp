// DraggableItem.cs
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
    /// This is a GUI component which can be moved around on a DragGrid
    /// </summary>
    public class DraggableItem : GUIComponent
    {
        public delegate void DragStarted();

        public event DragStarted OnDragStarted;

        public delegate void DragEnded();

        public event DragEnded OnDragEnded;

        public GItem Item { get; set; }
        public bool IsDragging { get; set; }

        public bool IsHighlighting { get; set; }

        public bool KeepAspectRatio { get; set; }

        public DraggableItem(DwarfGUI gui, GUIComponent parent, GItem item) :
            base(gui, parent)
        {
            IsDragging = false;
            Item = item;
            OnHover += DraggableItem_OnHover;
            OnUnHover += DraggableItem_OnUnHover;
            OnPressed += DraggableItem_OnPressed;
            OnRelease += DraggableItem_OnRelease;
            OnDragStarted += DraggableItem_OnDragStarted;
            OnDragEnded += DraggableItem_OnDragEnded;
            IsHighlighting = false;
            KeepAspectRatio = true;
        }

        private void DraggableItem_OnUnHover()
        {
            IsHighlighting = false;
        }

        private void DraggableItem_OnHover()
        {
            IsHighlighting = true;
        }

        private void DraggableItem_OnDragEnded()
        {
            IsDragging = false;
            OverrideClickBehavior = false;
            GUI.FocusComponent = null;
        }

        private void DraggableItem_OnDragStarted()
        {
            IsDragging = true;
            OverrideClickBehavior = true;
            GUI.FocusComponent = this;
        }

        private void DraggableItem_OnRelease()
        {
            if(IsDragging)
            {
                IsDragging = false;
                OnDragEnded.Invoke();
            }
        }

        private void DraggableItem_OnPressed()
        {
            if(!IsDragging)
            {
                IsDragging = true;
                OnDragStarted.Invoke();
            }
        }

        public Rectangle GetImageBounds()
        {
            Rectangle toDraw = GlobalBounds;


            if(KeepAspectRatio)
            {
                if(toDraw.Width < toDraw.Height)
                {
                    float wPh = (float) toDraw.Width / (float) toDraw.Height;
                    toDraw = new Rectangle(toDraw.X, toDraw.Y, toDraw.Width, (int) (toDraw.Height * wPh));
                }
                else
                {
                    float wPh = (float) toDraw.Height / (float) toDraw.Width;
                    toDraw = new Rectangle(toDraw.X, toDraw.Y, (int) (toDraw.Width * wPh), toDraw.Height);
                }
            }
            return toDraw;
        }


        public override void Update(DwarfTime time)
        {
            base.Update(time);
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            Rectangle toDraw = GetImageBounds();

            MouseState m = Mouse.GetState();
            if(IsDragging)
            {
                toDraw.Y = m.Y - toDraw.Height / 2;
                toDraw.X = m.X - toDraw.Width / 2;
                batch.Draw(Item.Image.Image, GetImageBounds(), Item.Image.SourceRect, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
            }

            //if (Item.CurrentAmount > 0)
            {
                if(!IsHighlighting)
                {
                    batch.Draw(Item.Image.Image, toDraw, Item.Image.SourceRect, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
                }
                else
                {
                    batch.Draw(Item.Image.Image, toDraw, Item.Image.SourceRect, IsMouseOver ? Color.Orange : Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
                }

                if(Item.CurrentAmount < 256)
                {
                    Drawer2D.DrawStrokedText(batch, "" + Item.CurrentAmount, GUI.SmallFont, new Vector2(GetImageBounds().X, GetImageBounds().Y), Color.White, Color.Black);
                    Drawer2D.DrawStrokedText(batch, "" + (Item.CurrentAmount * Item.Price).ToString("C"), GUI.SmallFont, new Vector2(GetImageBounds().X + GetImageBounds().Width / 2, GetImageBounds().Y + GetImageBounds().Height - 20), Color.White, Color.Black);
                }
                else
                {
                    Drawer2D.DrawStrokedText(batch, "" + Item.Price.ToString("C"), GUI.SmallFont, new Vector2(GetImageBounds().X + GetImageBounds().Width / 2, GetImageBounds().Y + GetImageBounds().Height - 20), Color.White, Color.Black);
                }
                //batch.DrawString(GUI.SmallFont, "" + Item.CurrentAmount, new Vector2(GetImageBounds().X, GetImageBounds().Y), Color.Black);
            }

            base.Render(time, batch);
        }
    }

}