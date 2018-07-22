using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class TaskListPanel : Widget
    {
        public WorldManager World;

        private WidgetListView ListView;
        private EditableTextField FilterBox;

        public override void Construct()
        {
            Border = "border-fancy";

            OnConstruct = (sender) =>
            {
                sender.Root.RegisterForUpdate(sender);

                FilterBox = AddChild(new EditableTextField
                {
                    AutoLayout = AutoLayout.DockTop,
                    MinimumSize = new Point(0, 24),
                    Text = ""
                }) as EditableTextField;

                ListView = AddChild(new WidgetListView
                {
                    AutoLayout = AutoLayout.DockFill,
                    SelectedItemForegroundColor = new Vector4(0,0,0,1),
                    Border = null,
                    ItemHeight = 16
                }) as WidgetListView;

                ListView.Border = null; // Can't make WidgetListView stop defaulting its border without breaking everywhere else its used.
            };

            OnUpdate = (sender, time) =>
            {
                if (sender.Hidden) return;

                //sender.Text = String.Join("\n", World.PlayerFaction.Minions.Select(m => String.Format("{0}: {1}, {2}", m.Name, m.Tasks.Count, (m.CurrentTask == null ? "NULL" : m.CurrentTask.Name))));
                //sender.Text += "\n\n";
                //sender.Text += String.Join("\n", World.Master.TaskManager.EnumerateTasks().Select(t => t.Name));
                //sender.Invalidate();


                ListView.ClearItems();
                foreach (var task in World.Master.TaskManager.EnumerateTasks().Where(t => String.IsNullOrEmpty(FilterBox.Text) ? true : t.Name.Contains(FilterBox.Text)))
                {
                    var tag = task.GuiTag as Widget;
                    var lambdaCopy = task;
                    if (tag != null)
                        ListView.AddItem(tag);
                    else
                    {
                        tag = Root.ConstructWidget(new Widget
                        {
                            Text = task.Name,
                            MinimumSize = new Point(0, 16),
                            Padding = new Margin(0, 0, 4, 4),
                            TextVerticalAlign = VerticalAlign.Center
                        });

                        tag.AddChild(new Widget
                        {
                            Text = "CANCEL",
                            AutoLayout = AutoLayout.DockRight,
                            MinimumSize = new Point(16, 0),
                            TextVerticalAlign = VerticalAlign.Center,
                            OnClick = (_sender, args) =>
                            {
                                World.Master.TaskManager.CancelTask(lambdaCopy);
                            }
                        });

                        Widget priorityDisplay = null;

                        tag.AddChild(new Gui.Widget
                        {
                            Background = new TileReference("round-buttons", 3),
                            MinimumSize = new Point(16, 16),
                            MaximumSize = new Point(16, 16),
                            AutoLayout = AutoLayout.DockRightCentered,
                            OnClick = (_sender, args) =>
                            {
                                lambdaCopy.Priority = (Task.PriorityType)(Math.Min(4, (int)lambdaCopy.Priority + 1));
                                priorityDisplay.Text = lambdaCopy.Priority.ToString();
                                priorityDisplay.Invalidate();
                            },
                            OnMouseEnter = (_sender, args) =>
                            {
                                _sender.BackgroundColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
                                _sender.Invalidate();
                            },
                            OnMouseLeave = (_sender, args) =>
                            {
                                _sender.BackgroundColor = Vector4.One;
                                _sender.Invalidate();
                            }
                        });

                        priorityDisplay = tag.AddChild(new Gui.Widget
                        {
                            AutoLayout = AutoLayout.DockRight,
                            MinimumSize = new Point(64, 0),
                            Text = lambdaCopy.Priority.ToString(),
                            TextHorizontalAlign = HorizontalAlign.Center,
                            TextVerticalAlign = VerticalAlign.Center
                        });

                        tag.AddChild(new Gui.Widget
                        {
                            Background = new TileReference("round-buttons", 7),
                            MinimumSize = new Point(16, 16),
                            MaximumSize = new Point(16, 16),
                            AutoLayout = AutoLayout.DockRightCentered,
                            OnClick = (_sender, args) =>
                            {
                                lambdaCopy.Priority = (Task.PriorityType)(Math.Max(0, (int)lambdaCopy.Priority - 1));
                                priorityDisplay.Text = lambdaCopy.Priority.ToString();
                                priorityDisplay.Invalidate();
                            },
                            OnMouseEnter = (_sender, args) =>
                            {
                                _sender.BackgroundColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
                                _sender.Invalidate();
                            },
                            OnMouseLeave = (_sender, args) =>
                            {
                                _sender.BackgroundColor = Vector4.One;
                                _sender.Invalidate();
                            }
                        });

                        task.GuiTag = tag;
                        ListView.AddItem(tag);
                    }
                }

                ListView.Invalidate();
            };

            base.Construct();
        }

       
    }
}
