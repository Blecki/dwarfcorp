using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class DefaultTaskPriority : Widget
    {
        public GameStates.Overworld Overworld;
        private List<ComboBox> Combos = new List<ComboBox>();
        private List<String> Priorities => Enum.GetNames(typeof(TaskPriority)).ToList();


        public override void Construct()
        {
            Font = "font10";

            AddChild(new Button()
            {
                Text = "Ok",
                AutoLayout = AutoLayout.DockBottom,
                OnClick = (sender, args) => sender.Parent.Close(),
                MinimumSize = new Point(64, 32)
            });

            var InteriorPanel = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Background = new TileReference("basic", 0),
                Font = "font8",
            });


            foreach (var type in Enum.GetValues(typeof(TaskCategory)))
            {
                if (type.ToString().StartsWith("_")) continue;
                if ((TaskCategory)type == TaskCategory.None) continue;

                var tag = InteriorPanel.AddChild(new Widget
                {
                    Text = type.ToString(),
                    MinimumSize = new Point(0, 16),
                    Padding = new Margin(0, 0, 4, 4),
                    TextVerticalAlign = VerticalAlign.Center,
                    AutoLayout = AutoLayout.DockTop
                });

                var combo = tag.AddChild(new ComboBox
                {
                    AutoLayout = AutoLayout.DockRight,
                    MinimumSize = new Point(100, 0),
                    Items = Priorities,
                    Tag = type,
                    OnSelectedIndexChanged = (sender) => SetPriorities()
                }) as ComboBox;

                Combos.Add(combo);
            }

            SetCombos();
            Layout();
            base.Construct();
        }

        private void SetPriorities()
        {
            foreach (var Combo in Combos)
            {
                var category = (TaskCategory)Combo.Tag;
                if (Enum.TryParse<TaskPriority>(Combo.SelectedItem, out var priority))
                    Overworld.DefaultTaskPriorities[category] = priority;
            }
        }

        private void SetCombos()
        {
            foreach (var Combo in Combos)
            {
                var category = (TaskCategory)Combo.Tag;
                Combo.SilentSetSelectedIndex(Priorities.IndexOf(Overworld.GetDefaultTaskPriority(category).ToString()));
            }
        }
    }
}
