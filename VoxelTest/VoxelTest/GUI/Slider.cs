using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class Slider : SillyGUIComponent
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

        public Slider(SillyGUI gui, SillyGUIComponent parent, string label,  float value, float minValue, float maxValue, SliderMode mode) :
            base(gui, parent)
        {
            Orient = Orientation.Horizontal;
            DrawLabel = true;
            SliderValue = value;
            MinValue = minValue;
            MaxValue = maxValue;
            Mode = mode;
            Label = label;
            OnValueModified += new ValueModified(Slider_OnValueModified);
            OnLeftPressed += new ClickedDelegate(Slider_OnLeftPressed);
            Focused = false;
            InvertValue = false;
        }

        void Slider_OnLeftPressed()
        {
            if (IsMouseOver)
            {
                Focused = true;
            }
        }

        void Slider_OnValueModified(float arg)
        {

        }


        public override void Update(GameTime time)
        {
            if (IsMouseOver && Focused)
            {
                MouseState mouse = Mouse.GetState();
                if (mouse.LeftButton == ButtonState.Pressed)
                {
                    float w = GlobalBounds.Width - GlobalBounds.Width * 0.2f;
                    float d = (mouse.X - GlobalBounds.X) / w;

                    if (Orient == Orientation.Vertical)
                    {
                        w = GlobalBounds.Height - GlobalBounds.Height * 0.2f;
                        d = (mouse.Y - GlobalBounds.Y) / w;
                    }

                    if (d > 1.0f)
                    {
                        d = 1.0f;
                    }
                    else if (d < 0)
                    {
                        d = 0.0f;
                    }


                    if (InvertValue)
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
        
        
        public override void Render(GameTime time, SpriteBatch batch)
        {

            if (IsVisible)
            {

                if (Orient == Orientation.Horizontal)
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
