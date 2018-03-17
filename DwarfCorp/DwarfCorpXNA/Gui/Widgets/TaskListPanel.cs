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

        public override void Construct()
        {
            OnConstruct = (sender) =>
            {
                sender.Root.RegisterForUpdate(sender);

                ListView = AddChild(new WidgetListView
                {
                    AutoLayout = AutoLayout.DockFill,
                    SelectedItemForegroundColor = new Vector4(0,0,0,1),
                    ItemHeight = 16
                }) as WidgetListView;
            };

            OnUpdate = (sender, time) =>
            {
                if (sender.Hidden) return;

                //sender.Text = String.Join("\n", World.PlayerFaction.Minions.Select(m => String.Format("{0}: {1}, {2}", m.Name, m.Tasks.Count, (m.CurrentTask == null ? "NULL" : m.CurrentTask.Name))));
                //sender.Text += "\n\n";
                //sender.Text += String.Join("\n", World.Master.TaskManager.EnumerateTasks().Select(t => t.Name));
                //sender.Invalidate();


                ListView.ClearItems();
                foreach (var task in World.Master.TaskManager.EnumerateTasks())
                {
                    var tag = task.GuiTag as Widget;
                    var lambdaCopy = task;
                    if (tag != null)
                        ListView.AddItem(tag);
                    else
                    {
                        tag = Root.ConstructWidget(new Widget
                        {
                            Text = task.Name
                        });

                        tag.AddChild(new Widget
                        {
                            Text = "CANCEL",
                            AutoLayout = AutoLayout.DockRight,
                            MinimumSize = new Point(32, 0),
                            OnClick = (_sender, args) =>
                            {
                                World.Master.TaskManager.CancelTask(lambdaCopy);
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
