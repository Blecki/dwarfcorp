// ToolTipManager.cs
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

namespace DwarfCorp
{
    /// <summary>
    /// Tooltips are the little bits of text which appear on the screen when a mouse is hovering
    /// over a particular GUI element. Every GUI element can have a tooltip. They are used to help the player
    /// understand what certain buttons do.
    /// </summary>
    public class ToolTipManager
    {
        public DwarfGUI GUI { get; set; }

        public Timer PopupTimer { get; set; }

        public Timer HoverTimer { get; set; }

        public string PopupTip { get; set; }
        public string ToolTip { get; set; }

        private MouseState LastMouse { get; set; }

        public int MovementThreshold { get; set; }

        public enum TipType
        {
            TopLeft,
            BottomRight
        }

        public ToolTipManager(DwarfGUI gui)
        {
            GUI = gui;
            HoverTimer = new Timer(0.8f, true, Timer.TimerMode.Real);
            ToolTip = "";
            LastMouse = Mouse.GetState();
            MovementThreshold = 2;
            PopupTimer = new Timer(2.5f, true, Timer.TimerMode.Real);
        }

        public void Update(DwarfTime time)
        {
            MouseState currentMouse = Mouse.GetState();

            int movement = Math.Abs(LastMouse.X - currentMouse.X) + Math.Abs(LastMouse.Y - currentMouse.Y);

            if(ToolTip != "" && movement > MovementThreshold)
            {
                ToolTip = "";
                HoverTimer.Reset(HoverTimer.TargetTimeSeconds);
            }
            else if(ToolTip == "" && movement < MovementThreshold)
            {
                HoverTimer.Update(time);

                if(HoverTimer.HasTriggered)
                {
                    List<string> tips = new List<string>();
                    GetToolTipsUnderMouseRecursive(GUI.RootComponent, tips);

                    ToolTip = tips.Count > 0 ? tips.Last() : "";
                }
            }

            PopupTimer.Update(time);

            if (PopupTimer.HasTriggered)
            {
                PopupTip = null;
            }

            LastMouse = currentMouse;

        }

        public void GetToolTipsUnderMouseRecursive(GUIComponent root, List<string> tips)
        {
            if(root.IsMouseOver && !string.IsNullOrEmpty(root.ToolTip))
            {
                tips.Add(root.ToolTip);
            }

            foreach(GUIComponent component in root.Children)
            {
                GetToolTipsUnderMouseRecursive(component, tips);   
            }
        }

        public void RenderTip(GraphicsDevice device, SpriteBatch batch, string tip, MouseState mouse, TipType tipType)
        {
            Rectangle viewBounds = device.Viewport.Bounds;

            Vector2 stringMeasure = Datastructures.SafeMeasure(GUI.SmallFont, tip);

            Rectangle bounds;
            
            if(tipType == TipType.BottomRight)
            { 
                bounds = new Rectangle(mouse.X + 24, mouse.Y + 24, (int)(stringMeasure.X + 15), (int)(stringMeasure.Y + 15));
            }
            else
            {
                bounds = new Rectangle(mouse.X - (int)stringMeasure.X - 15, mouse.Y  - (int)stringMeasure.Y - 15, (int)(stringMeasure.X + 15), (int)(stringMeasure.Y + 15));
            }

            if (bounds.Left < viewBounds.Left)
            {
                bounds.X = viewBounds.X;
            }

            if (bounds.Right > viewBounds.Right)
            {
                bounds.X = viewBounds.Right - bounds.Width;
            }

            if (bounds.Top < viewBounds.Top)
            {
                bounds.Y = viewBounds.Y;
            }

            if (bounds.Bottom > viewBounds.Bottom)
            {
                bounds.Y = viewBounds.Bottom - bounds.Height;
            }

            GUI.Skin.RenderToolTip(bounds, batch, Color.White);
            Drawer2D.DrawAlignedText(batch, tip, GUI.SmallFont, Color.White, Drawer2D.Alignment.Center, bounds);
        }

        public void Render(GraphicsDevice device, SpriteBatch batch, DwarfTime time)
        {
            if (!GUI.RootComponent.IsVisible)
            {
                return;
            }

            if (!string.IsNullOrEmpty(PopupTip))
            {
                RenderTip(device, batch, PopupTip, Mouse.GetState(), TipType.TopLeft);
            }

            if (!string.IsNullOrEmpty(ToolTip))
            {
                RenderTip(device, batch, ToolTip, Mouse.GetState(), TipType.BottomRight);
            }
        }


        public void Popup(string text)
        {
            Popup(text, 2.5f);
        }

        public void Popup(string text, float time)
        {
            PopupTip = text;
            PopupTimer.Reset(time);
        }
    }
}
