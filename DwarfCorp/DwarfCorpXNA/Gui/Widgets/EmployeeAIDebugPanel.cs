using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class EmployeeAIDebugPanel : HorizontalMenuTray.MenuItem
    {
        public WorldManager World;
        private WidgetListView ListView;
        private List<CreatureAIDebugWidget> CreatureWidgets = new List<CreatureAIDebugWidget>();

        public class CreatureAIDebugWidget : Widget
        {
            public CreatureAI CreatureAI = null;

            public override void Construct()
            {
                OnUpdate = (sender, time) =>
                {
                    Text = String.Format("{0}: {1}, {2}\n{3}",
                        CreatureAI.Name,
                        CreatureAI.Tasks.Count,
                        (CreatureAI.CurrentTask == null ? "NO TASK" : CreatureAI.CurrentTask.Name),
                        (CreatureAI.CurrentAct == null ? "NO ACT" : CreatureAI.CurrentAct.Name));

                    this.Invalidate();
                };

                base.Construct();
            }
        }

        public override void Construct()
        {
            Rect = new Rectangle(0, 0, 300, 200);

            OnConstruct = (sender) =>
            {
                sender.Root.RegisterForUpdate(sender);

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

                CreatureWidgets.RemoveAll(w => w.CreatureAI.IsDead);

                ListView.ClearItems();
                foreach (var creature in World.PlayerFaction.Minions)
                {
                    var existingWidget = CreatureWidgets.FirstOrDefault(w => Object.ReferenceEquals(creature, w.CreatureAI));
                    if (existingWidget == null)
                    {
                        existingWidget = Root.ConstructWidget(new CreatureAIDebugWidget
                        {
                            CreatureAI = creature,
                            MinimumSize = new Point(0, 32)
                        }) as CreatureAIDebugWidget;
                    }
                    ListView.AddItem(existingWidget);

                    existingWidget.OnUpdate(existingWidget, time);
                }

                //sender.Text = String.Join("\n", World.PlayerFaction.Minions.Select(m => String.Format("{0}: {1}, {2}", m.Name, m.Tasks.Count, (m.CurrentTask == null ? "NULL" : m.CurrentTask.Name))));
                //sender.Text += "\n\n";
                //sender.Text += String.Join("\n", World.Master.TaskManager.EnumerateTasks().Select(t => t.Name));
                //sender.Invalidate();

                ListView.Invalidate();
            };

            base.Construct();
        }

       
    }
}
