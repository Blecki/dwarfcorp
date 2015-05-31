// ColorSelector.cs
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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class ColorSelector : GUIComponent
    {
        public GridLayout Layout { get; set; }
        public int PanelWidth = 32;
        public int PanelHeight = 32;
        public Color CurrentColor { get; set; }
        public delegate void ColorSelected(Color arg);

        public event ColorSelected OnColorSelected;


        public ColorSelector(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            OnColorSelected += ColorSelector_OnColorSelected;
        }

        void ColorSelector_OnColorSelected(Color arg)
        {
           
        }

        public void InitializeColorPanels(List<Color> colors)
        {
            int numRows = GlobalBounds.Height/PanelHeight;
            int numCols = GlobalBounds.Width/PanelWidth;
            Layout = new GridLayout(GUI, this, GlobalBounds.Height / PanelHeight, GlobalBounds.Width / PanelWidth);

            int rc = Math.Max((int)(Math.Sqrt(colors.Count)), 2);

            for (int i = 0; i < colors.Count; i++)
            {
                ColorPanel panel = new ColorPanel(GUI, Layout)
                {
                    CurrentColor = colors[i]
                };

                int row = i / numCols;
                int col = i % numCols;
                panel.OnClicked += () => panel_OnClicked(panel.CurrentColor);

                Layout.SetComponentPosition(panel, col, row, 1, 1);
            }
        }

        void panel_OnClicked(Color color)
        {
            CurrentColor = color;
            OnColorSelected.Invoke(color);
        }
    }
}
