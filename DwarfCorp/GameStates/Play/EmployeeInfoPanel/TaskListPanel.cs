using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Play.EmployeeInfo
{
    public class TaskListPanel : Widget
    {
        private WidgetListView ListView;

        public override void Construct()
        {
            Border = null;

            OnConstruct = (sender) =>
            {
                ListView = AddChild(new WidgetListView
                {
                    AutoLayout = AutoLayout.DockFill,
                    SelectedItemForegroundColor = new Vector4(0,0,0,1),
                    Border = null,
                    ItemHeight = 16
                }) as WidgetListView;

                ListView.Border = null; // Can't make WidgetListView stop defaulting its border without breaking everywhere else its used.
            };

            base.Construct();
        }

        public void UpdateList(CreatureAI Employee)
        {
            var tasksToDisplay = Employee.Tasks;

            ListView.ClearItems();
            foreach (var task in tasksToDisplay)
            {
                var tag = task.GuiTag as Widget;
                var lambdaCopy = task;

                if (tag != null)
                    ListView.AddItem(tag);
                else
                {
                    #region Create gui row

                    tag = Root.ConstructWidget(new Widget
                    {
                        Text = task.Name,
                        MinimumSize = new Point(0, 16),
                        Padding = new Margin(0, 0, 4, 4),
                        TextVerticalAlign = VerticalAlign.Center,
                        OnClick = (_sender, args) =>
                        {
                            var loc = lambdaCopy.GetCameraZoomLocation();
                            if (loc.HasValue)
                            {
                                Employee.World.Renderer.Camera.SetZoomTarget(loc.Value);
                            }
                        }
                    });

                    tag.AddChild(new Widget
                    {
                        Text = "CANCEL",
                        AutoLayout = AutoLayout.DockRight,
                        MinimumSize = new Point(16, 0),
                        TextVerticalAlign = VerticalAlign.Center,
                        OnClick = (_sender, args) =>
                        {
                            Employee.World.TaskManager.CancelTask(lambdaCopy);
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
                            lambdaCopy.Priority = (TaskPriority)(Math.Min(4, (int)lambdaCopy.Priority + 1));
                            priorityDisplay.Text = lambdaCopy.Priority.ToString();
                            priorityDisplay.Invalidate();
                        },
                        OnMouseEnter = (_sender, args) =>
                        {
                            _sender.BackgroundColor = GameSettings.Current.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
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
                            lambdaCopy.Priority = (TaskPriority)(Math.Max(0, (int)lambdaCopy.Priority - 1));
                            priorityDisplay.Text = lambdaCopy.Priority.ToString();
                            priorityDisplay.Invalidate();
                        },
                        OnMouseEnter = (_sender, args) =>
                        {
                            _sender.BackgroundColor = GameSettings.Current.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
                            _sender.Invalidate();
                        },
                        OnMouseLeave = (_sender, args) =>
                        {
                            _sender.BackgroundColor = Vector4.One;
                            _sender.Invalidate();
                        }
                    });

                    #endregion

                    task.GuiTag = tag;
                    ListView.AddItem(tag);
                }

                tag.Text = task.Name;
            }

            ListView.Invalidate();
        }
    }
}
