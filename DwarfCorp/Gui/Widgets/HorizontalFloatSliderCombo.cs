using Microsoft.Xna.Framework;
using System;

namespace DwarfCorp.Gui.Widgets
{
    public class HorizontalFloatSliderCombo : Widget
    {
        public Action<Widget> OnSliderChanged = null;
        public int InputWidth = 128;

        private EditableTextField InputBox;
        private HorizontalFloatSlider Slider;

        public float ScrollMin;
        public float ScrollMax;

        public float ScrollPosition
        {
            get { return Slider.ScrollPosition + ScrollMin; }
            set { Slider.ScrollPosition = value - ScrollMin; }
        }

        public override void Construct()
        {
            InputBox = AddChild(new EditableTextField
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(InputWidth, 0),
                BeforeTextChange = (sender, args) =>
                {
                    if (Single.TryParse(args.NewText, out var newValue))
                        ScrollPosition = newValue - ScrollMin;
                }
            }) as EditableTextField;

            Slider = AddChild(new HorizontalFloatSlider
            {
                AutoLayout = AutoLayout.DockFill,
                ScrollArea = ScrollMax - ScrollMin,
                OnSliderChanged = (sender) =>
                {
                    InputBox.Text = (Slider.ScrollPosition + ScrollMin).ToString();
                    OnSliderChanged(this);
                }
            }) as HorizontalFloatSlider;

        }        
    }
}
