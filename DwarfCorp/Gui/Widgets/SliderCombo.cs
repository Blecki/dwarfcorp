using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class SliderCombo : Widget
    {
        public Action<Widget> OnSliderChanged = null;
        public int InputWidth = 128;

        private EditableTextField InputBox;
        private HorizontalSlider Slider;

        public int ScrollMin;
        public int ScrollMax;

        public int ScrollPosition
        {
            get { return Slider.ScrollPosition; }
            set { Slider.ScrollPosition = value; }
        }

        public override void Construct()
        {
            InputBox = AddChild(new EditableTextField
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(InputWidth, 0),
                BeforeTextChange = (sender, args) =>
                {
                    if (Int32.TryParse(args.NewText, out var newValue))
                        ScrollPosition = newValue;
                }
            }) as EditableTextField;

            Slider = AddChild(new HorizontalSlider
            {
                AutoLayout = AutoLayout.DockFill,
                ScrollMin = ScrollMin,
                ScrollMax = ScrollMax,
                OnSliderChanged = (sender) =>
                {
                    InputBox.Text = Slider.ScrollPosition.ToString();
                    OnSliderChanged(this);
                }
            }) as HorizontalSlider;

        }        
    }
}
