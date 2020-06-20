
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Play
{
    public class CommandPanel : Gui.Widget
    {
        public WorldManager World;
        private Gui.Widgets.EditableTextField FilterBox;
        private Gui.Widget SelectedCommandDisplay;

        private class CommandGrid : Gui.Widgets.GridPanel
        {
            public WorldManager World;
            public List<PossiblePlayerCommand> Commands;
            public Action<PossiblePlayerCommand> OnCommandClicked;

            public override void Construct()
            {
                EnableScrolling = true;
                base.Construct();

                Commands = PlayerCommandEnumerator.EnumeratePlayerCommands(World).ToList();
                foreach (var command in Commands)
                    if (command.HoverWidget != null) Root.ConstructWidget(command.HoverWidget);

                ItemSize = new Point(38, 70);
                foreach (var resource in Commands)
                {
                    var lambdaResource = resource;
                    resource.GuiTag = AddChild(new CommandIcon
                    {
                        Resource = resource,
                        OnClick = (sender, args) => OnCommandClicked(lambdaResource)
                    });
                }
            }

            public void ApplyFilter(String Text)
            {
                var scrollbar = Children[0];
                Children.Clear();
                AddChild(scrollbar);
                foreach (var command in Commands.Where(c => c.Name.ToUpperInvariant().Contains(Text.ToUpperInvariant())))
                    AddChild(command.GuiTag);
                this.Layout();
                this.Invalidate();
            }
        }

        private CommandGrid Grid;

        public override void Construct()
        {
            Padding = new Margin(2, 2, 2, 2);

            base.Construct();

            var bar = AddChild(new Widget
            {
                MinimumSize = new Point(0, 24),
                Padding = new Margin(0, 0, 2, 2),
                AutoLayout = AutoLayout.DockTop
            });

            bar.AddChild(new Widget
            {
                Text = "Filter:",
                AutoLayout = AutoLayout.DockLeft
            });

            FilterBox = bar.AddChild(new Gui.Widgets.EditableTextField
            {
                AutoLayout = AutoLayout.DockFill,
                MinimumSize = new Point(0, 24),
                OnTextChange = (sender) => { Grid.ApplyFilter(FilterBox.Text); }
            }) as Gui.Widgets.EditableTextField;

            SelectedCommandDisplay = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 200)
            });

            Grid = AddChild(new CommandGrid
            {
                AutoLayout = AutoLayout.DockFill,
                World = World,
                OnCommandClicked = (command) =>
                {
                    SelectedCommandDisplay.Children.Clear();
                    if (command.HoverWidget != null)
                    {
                        SelectedCommandDisplay.AddChild(command.HoverWidget);
                        command.HoverWidget.AutoLayout = AutoLayout.DockFill;
                        command.HoverWidget.Border = "";
                    }

                    SelectedCommandDisplay.Layout();

                    command.OnClick?.Invoke(command, null);
                }
            }) as CommandGrid;
        }
    }
}
