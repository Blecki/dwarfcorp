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