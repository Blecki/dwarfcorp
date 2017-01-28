// SpinBox.cs
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
    /// This component shows a floating point value that can be 
    /// incremented or decremented with buttons.
    /// </summary>
    public class SpinBox : GUIComponent
    {
        public delegate void ValueChangedDelegate(SpinBox box);

        public event ValueChangedDelegate OnValueChanged;

        public enum SpinMode
        {
            Integer,
            Float
        }

        public float SpinValue { get; set; }
        public float MaxValue { get; set; }
        public float MinValue { get; set; }
        public float Increment { get; set; }
        public SpinMode Mode { get; set; }
        public GridLayout Layout { get; set; }
        public Button PlusButton { get; set; }
        public Button MinusButton { get; set; }
        public LineEdit ValueBox { get; set; }

        public SpinBox(DwarfGUI gui, GUIComponent parent, string label, float value, float minValue, float maxValue, SpinMode mode) :
            base(gui, parent)
        {
            Increment = 1.0f;
            SpinValue = value;
            MinValue = minValue;
            MaxValue = maxValue;
            Mode = mode;
            Layout = new GridLayout(GUI, this, 1, 4);
            PlusButton = new Button(GUI, Layout, "", GUI.DefaultFont, Button.ButtonMode.ImageButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.ZoomIn))
            {
                KeepAspectRatio = true,
                DontMakeSmaller = true
            };
            MinusButton = new Button(GUI, Layout, "", GUI.DefaultFont, Button.ButtonMode.ImageButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.ZoomOut))
            {
                KeepAspectRatio = true,
                DontMakeSmaller = true
            };
            ValueBox = new LineEdit(GUI, Layout, value.ToString())
            {
                IsEditable = false
            };
            Layout.SetComponentPosition(ValueBox, 0, 0, 2, 1);
            Layout.SetComponentPosition(PlusButton, 3, 0, 1, 1);
            Layout.SetComponentPosition(MinusButton, 2, 0, 1, 1);

            PlusButton.OnClicked += PlusButton_OnClicked;
            MinusButton.OnClicked += MinusButton_OnClicked;
            OnValueChanged += SpinBox_OnValueChanged;
        }

        private void SpinBox_OnValueChanged(SpinBox value)
        {
        }

        private void MinusButton_OnClicked()
        {
            SpinValue -= Increment;

            SpinValue = Math.Min(Math.Max(SpinValue, MinValue), MaxValue);

            if(Mode == SpinMode.Integer)
            {
                SpinValue = (int) SpinValue;
            }

            ValueBox.Text = SpinValue.ToString();
            OnValueChanged.Invoke(this);
        }

        private void PlusButton_OnClicked()
        {
            SpinValue += Increment;
            SpinValue = Math.Min(Math.Max(SpinValue, MinValue), MaxValue);
            if(Mode == SpinMode.Integer)
            {
                SpinValue = (int) SpinValue;
            }

            ValueBox.Text = SpinValue.ToString();
            OnValueChanged.Invoke(this);
        }
    }

}