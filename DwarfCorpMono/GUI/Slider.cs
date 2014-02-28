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

        public float SliderValue { get; set; }
        public float MaxValue { get; set; }
        public float MinValue { get; set; }
        public SliderMode Mode { get; set; }
        public string Label { get; set; }
        public delegate void ValueModified(float arg);
        public event ValueModified OnValueModified;

        public Slider(SillyGUI gui, SillyGUIComponent parent, string label,  float value, float minValue, float maxValue, SliderMode mode) :
            base(gui, parent)
        {
            SliderValue = value;
            MinValue = minValue;
            MaxValue = maxValue;
            Mode = mode;
            Label = label;
            OnValueModified += new ValueModified(Slider_OnValueModified);
        }

        void Slider_OnValueModified(float arg)
        {

        }


        public override void Update(GameTime time)
        {
            if (IsMouseOver)
            {
                MouseState mouse = Mouse.GetState();
                if (mouse.LeftButton == ButtonState.Pressed)
                {
                    float w = GlobalBounds.Width - GlobalBounds.Width * 0.2f;
                    float d = (mouse.X - GlobalBounds.X) / w;

                    if (d > 1.0f)
                    {
                        d = 1.0f;
                    }
                    else if (d < 0)
                    {
                        d = 0.0f;
                    }

                    SliderValue = d * (MaxValue - MinValue) + MinValue;

                    OnValueModified.Invoke(SliderValue);

                }
            }

            base.Update(time);
        }
        
        
        public override void Render(GameTime time, SpriteBatch batch)
        {
            GUI.Skin.RenderSlider(GUI.DefaultFont, GlobalBounds, SliderValue, MinValue, MaxValue, Mode, batch);
            base.Render(time, batch);
        }

        

    }
}
