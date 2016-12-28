// Panel.cs
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     This is a simple GUI component which draws a fancy rectangle thing.
    /// </summary>
    public class Panel : GUIComponent
    {
        public enum PanelMode
        {
            Fancy,
            Simple,
            Window,
            WindowEx,
            SpeechBubble
        }

        public Panel(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            Mode = PanelMode.Fancy;
        }

        public PanelMode Mode { get; set; }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            if (!IsVisible)
            {
                return;
            }

            if (Mode == PanelMode.Fancy)
            {
                GUI.Skin.RenderPanel(GlobalBounds, batch);
            }
            else if (Mode == PanelMode.Simple)
            {
                GUI.Skin.RenderToolTip(GlobalBounds, batch, new Color(255, 255, 255, 150));
            }
            else if (Mode == PanelMode.SpeechBubble)
            {
                GUI.Skin.RenderSpeechBubble(GlobalBounds, batch);
            }
            else
            {
                GUI.Skin.RenderWindow(GlobalBounds, batch, Mode == PanelMode.WindowEx);
            }
            base.Render(time, batch);
        }
    }

    public class ScrollingAnimation : GUIComponent
    {
        public ScrollingAnimation(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            Tint = Color.White;
        }

        public NamedImageFrame Image { get; set; }
        public Color Tint { get; set; }
        public Vector2 ScrollSpeed { get; set; }
        public Vector2 Scroll { get; set; }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            if (Image != null && Image.Image != null)
            {
                Rectangle sourceRect = Image.SourceRect;
                sourceRect.X = (int) (sourceRect.X + Scroll.X);
                sourceRect.Y = (int) (sourceRect.Y + Scroll.Y);
                sourceRect.Width = GlobalBounds.Width;
                sourceRect.Height = GlobalBounds.Height;
                batch.Draw(Image.Image, GlobalBounds, sourceRect, Tint);
            }
            base.Render(time, batch);
        }

        public override void Update(DwarfTime time)
        {
            Scroll += ScrollSpeed*(float) time.ElapsedRealTime.TotalSeconds;
            base.Update(time);
        }
    }


    public class Tray : GUIComponent
    {
        public enum Position
        {
            BottomLeft,
            BottomRight,
            TopLeft,
            TopRight
        }


        public Tray(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            TrayPosition = Position.BottomRight;
        }

        public Position TrayPosition { get; set; }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            Rectangle rect = GlobalBounds;
            rect.Inflate(24, 24);
            GUI.Skin.RenderTray(TrayPosition, rect, batch);
            base.Render(time, batch);
        }
    }
}