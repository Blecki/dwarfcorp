// ActDisplay.cs
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

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     This is a debug display for behavior trees (acts). It just draws a tree of named Acts.
    /// </summary>
    public class ActDisplay : GUIComponent
    {
        private readonly Timer rebuildTimer = new Timer(2.0f, false, Timer.TimerMode.Real);
        public int ElementHeight = 15;
        public int ElementWidth = 50;
        private Act m_act;

        public ActDisplay(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            DisplayColors = new Color[3];
            DisplayColors[(int) Act.Status.Running] = Color.DarkBlue;
            DisplayColors[(int) Act.Status.Success] = Color.Green;
            DisplayColors[(int) Act.Status.Fail] = Color.Red;

            Elements = new List<ActElement>();
        }

        public Act CurrentAct
        {
            get { return m_act; }
            set
            {
                m_act = value;
                InitAct();
            }
        }

        public Color[] DisplayColors { get; set; }

        public List<ActElement> Elements { get; set; }


        private void CreateSubtreeRecursive(Act root, ref Vector2 lastPosition, ref Vector2 size)
        {
            if (root == null)
            {
            }
            var element = new ActElement();
            element.act = root;
            element.position = lastPosition;

            Elements.Add(element);

            lastPosition.Y += ElementHeight;
            lastPosition.X += ElementWidth;

            size += Datastructures.SafeMeasure(GUI.DefaultFont, element.act.Name);
            size.X += ElementWidth;
            size.Y += ElementHeight;

            if (root.Children != null && root.Enumerator.Current != Act.Status.Success)
            {
                foreach (Act child in root.Children)
                {
                    CreateSubtreeRecursive(child, ref lastPosition, ref size);
                }
            }

            lastPosition.X -= ElementWidth;
        }

        private void InitAct()
        {
            var lastPosition = new Vector2(5, 5);
            var size = new Vector2(0, 0);

            if (CurrentAct != null && CurrentAct.IsInitialized)
            {
                var element = new ActElement();
                element.act = CurrentAct;
                element.position = lastPosition;
                Elements.Clear();
                Elements.Add(element);

                lastPosition += new Vector2(ElementWidth, ElementHeight);
                size.X += ElementWidth;
                size.Y += ElementHeight;

                if (CurrentAct.Children != null)
                {
                    foreach (Act child in CurrentAct.Children)
                    {
                        CreateSubtreeRecursive(child, ref lastPosition, ref size);
                    }
                }
            }
            else if (CurrentAct != null)
            {
                CurrentAct = null;
            }


            LocalBounds = new Rectangle(LocalBounds.X, LocalBounds.Y, (int) size.X, (int) size.Y);
        }

        public override void Update(DwarfTime time)
        {
            rebuildTimer.Update(time);

            if (rebuildTimer.HasTriggered)
            {
                InitAct();
            }

            base.Update(time);
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            foreach (ActElement element in Elements)
            {
                string toDraw = element.act.Name;
                Color color = DisplayColors[(int) (element.act.Enumerator.Current)];

                if (element.act.Children.Count == 0 && element.act.Enumerator.Current == Act.Status.Running)
                {
                    toDraw = ">" + toDraw;
                    color = Color.Blue;
                }

                batch.DrawString(GUI.SmallFont, toDraw, element.position + new Vector2(GlobalBounds.X, GlobalBounds.Y),
                    color);
            }

            base.Render(time, batch);
        }

        public struct ActElement
        {
            public Act act;
            public Vector2 position;
        }
    }
}