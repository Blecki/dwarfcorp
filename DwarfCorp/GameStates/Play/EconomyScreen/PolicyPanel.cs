using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.GameStates
{
    public class PolicyPanel : Gui.Widget
    {
        public Faction Faction;
        private Widget InfoWidget;
        public WorldManager World;
        int numrows = 0;
        private Widget Title;
        private Gui.Widgets.HorizontalFloatSliderCombo FoodCostSlider;

        public PolicyPanel()
        {
        }

        private Widget LabelAndDockWidget(Widget Column, string Label, Widget Widget)
        {
            var r = Column.AddChild(new Widget
            {
                MinimumSize = new Point(0, 20),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(0, 0, 4, 4),
                ChangeColorOnHover = true,
                HoverTextColor = GameSettings.Current.Colors.GetColor("Highlight", Color.DarkRed).ToVector4()
            });

            var label = new Widget
            {
                Text = Label,
                AutoLayout = AutoLayout.DockLeft,
            };

            r.AddChild(label);

            Widget.AutoLayout = AutoLayout.DockFill;
            r.AddChild(Widget);
            Widget.OnMouseEnter += (sender, args) =>
            {
                label.TextColor = GameSettings.Current.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
                label.Invalidate();
            };

            Widget.OnMouseLeave += (sender, args) =>
            {
                label.TextColor = Color.Black.ToVector4();
                label.Invalidate();
            };

            return Widget;
        }

        public override void Construct()
        {
            Border = "border-thin";
            Padding = new Margin(4, 4, 0, 0);
            Font = "font10";

            Title = AddChild(new Widget()
            {
                Font = "font16",
                Text = "Colony Policy",
                AutoLayout = AutoLayout.DockTop
            });

            var split = AddChild(new Gui.Widgets.Columns
            {
                AutoLayout = AutoLayout.DockFill
            }) as Gui.Widgets.Columns;

            var leftPanel = split.AddChild(new Widget
            {
                Padding = new Margin(2, 2, 2, 2)
            });

            var rightPanel = split.AddChild(new Widget
            {
                Padding = new Margin(2, 2, 2, 2)
            });

            FoodCostSlider = LabelAndDockWidget(leftPanel, "Employee Food Cost", new Gui.Widgets.HorizontalFloatSliderCombo
            {
                ScrollMax = 10.0f,
                ScrollMin = 0.1f,
                OnSliderChanged = (widget) =>
                {
                    World.PersistentData.CorporateFoodCostPolicy = FoodCostSlider.ScrollPosition;
                }
            }) as Gui.Widgets.HorizontalFloatSliderCombo;

            FoodCostSlider.ScrollPosition = World.PersistentData.CorporateFoodCostPolicy;

            Layout();
        }
    }
}
