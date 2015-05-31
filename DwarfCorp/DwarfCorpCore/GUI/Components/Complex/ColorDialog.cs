// ColorDialog.cs
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

namespace DwarfCorp
{
    public class ColorDialog : Dialog
    {
        public ColorSelector Selector { get; set; }
        private List<Color> Colors { get; set; } 

        public delegate void ColorSelected(Color arg);
        public event ColorSelector.ColorSelected OnColorSelected;

        public static ColorDialog Popup(DwarfGUI gui, List<Color> colors)
        {
            int w = gui.Graphics.Viewport.Width - 128;
            int h = gui.Graphics.Viewport.Height - 128;
            ColorDialog toReturn = new ColorDialog(gui, gui.RootComponent, colors)
            {
                LocalBounds =
                    new Rectangle(gui.Graphics.Viewport.Width / 2 - w / 2, gui.Graphics.Viewport.Height / 2 - h / 2, w, h)
            };
            toReturn.Initialize(ButtonType.Cancel, "Select Colors", "");
            
            return toReturn;
        }

        public ColorDialog(DwarfGUI gui, GUIComponent parent, List<Color> colors) 
            : base(gui, parent)
        {
            Colors = colors;
        }

        public override void Initialize(ButtonType buttons, string title, string message)
        {
            base.Initialize(buttons, title, message);
            Selector = new ColorSelector(GUI, Layout);
            Layout.LocalBounds = new Rectangle(0, 0, LocalBounds.Width, LocalBounds.Height);
            Layout.SetComponentPosition(Selector, 0, 1, 4, 2);
            Layout.UpdateSizes();
            Selector.InitializeColorPanels(Colors);
            OnColorSelected += ColorSelectedInvoker;
            Selector.OnColorSelected += ColorDialog_OnColorSelected;
        }

        private void ColorSelectedInvoker(Color arg)
        {
        }

        void ColorDialog_OnColorSelected(Color arg)
        {
            OnColorSelected.Invoke(arg);
            Close(ReturnStatus.Ok);
        }

        
    }
}
