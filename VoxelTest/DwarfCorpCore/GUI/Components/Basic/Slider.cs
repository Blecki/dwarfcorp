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
    /// This GUI component has a handle which can be dragged
    /// to specify a value.
    /// </summary>
    public class Slider : GUIComponent
    {
        public enum SliderMode
        {
            Integer,
            Float
        }

        public enum Orientation
        {
            Horizontal,
            Vertical
        }

        public float SliderValue { get; set; }
        public float MaxValue { get; set; }
        public float MinValue { get; set; }
        public SliderMode Mode { get; set; }
        public Orientation Orient { get; set; }
        public string Label { get; set; }

        public delegate void ValueModified(float arg);

        public event ValueModified OnValueModified;
        public bool DrawLabel { get; set; }
        public bool Focused { get; set; }
        public bool InvertValue { get; set; }

        public Slider(DwarfGUI gui, GUIComponent parent, string label, float value, float minValue, float maxValue, SliderMode mode) :
            base(gui, parent)
        {
            Orient = Orientation.Horizontal;
            DrawLabel = true;
            SliderValue = value;
            MinValue = minValue;
            MaxValue = maxValue;
            Mode = mode;
            Label = label;
            OnValueModified += Slider_OnValueModified;
            OnLeftPressed += Slider_OnLeftPressed;
            Focused = false;
            InvertValue = false;
        }

        private void Slider_OnLeftPressed()
        {
            if(IsMouseOver)
            {
                Focused = true;
            }
        }

        private void Slider_OnValueModified(float arg)
        {
        }


        public override void Update(DwarfTime time)
        {
            if(IsMouseOver && Focused)
            {
                MouseState mouse = Mouse.GetState();
                if(mouse.LeftButton == ButtonState.Pressed)
                {
                    const int padding = 5;
                    const int fieldWidth = 64;
                    const int fieldHeight = 32;
                    float w = GlobalBounds.Width - padding * 2 - fieldWidth;
                    float d = (mouse.X - (GlobalBounds.X + padding)) / w;

                    if(Orient == Orientation.Vertical)
                    {
                        w = GlobalBounds.Height - padding * 2 - fieldHeight;
                        d = (mouse.Y - (GlobalBounds.Y - padding)) / w;
                    }

                    if(d > 1.0f)
                    {
                        d = 1.0f;
                    }
                    else if(d < 0)
                    {
                        d = 0.0f;
                    }


                    if(InvertValue)
                    {
                        d = (1.0f - d);
                    }

                    SliderValue = d * (MaxValue - MinValue) + MinValue;

                    OnValueModified.Invoke(SliderValue);
                }
            }
            else
            {
                Focused = false;
            }

            base.Update(time);
        }


        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            if(IsVisible)
            {
                if(Orient == Orientation.Horizontal)
                {
                    GUI.Skin.RenderSliderHorizontal(GUI.DefaultFont, GlobalBounds, SliderValue, MinValue, MaxValue, Mode, DrawLabel, InvertValue, batch);
                }
                else
                {
                    GUI.Skin.RenderSliderVertical(GUI.DefaultFont, GlobalBounds, SliderValue, MinValue, MaxValue, Mode, DrawLabel, InvertValue, batch);
                }
            }
            base.Render(time, batch);
        }
    }

}