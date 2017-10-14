using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class GoalPanel : Columns
    {
        private Gui.Widgets.ListView GoalList;
        public IEnumerable<DwarfCorp.Goals.Goal> GoalSource;
        private List<Goals.Goal> Goals;
        public WorldManager World;

        private void RebuildGoalList()
        {
            var index = Math.Max(GoalList.SelectedIndex, 0);
            GoalList.Items.Clear();
            Goals = GoalSource.ToList();
            GoalList.Items = Goals.Select(g => g.Name).ToList();
            GoalList.SelectedIndex = index;
        }

        public override void Construct()
        {
            var left = AddChild(new Widget());

            var right = AddChild(new Gui.Widgets.GoalInfo
            {
                AutoLayout = AutoLayout.DockFill,
                Hidden = true,
                OnActivateClicked = (sender) => 
                {
                    if ((sender as GoalInfo).Goal != null)
                        World.GoalManager.TryActivateGoal(World, (sender as GoalInfo).Goal);
                    Invalidate();
                },
                World = World
            }) as Gui.Widgets.GoalInfo;

            GoalList = left.AddChild(new Gui.Widgets.ListView
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "font16",
                OnSelectedIndexChanged = (sender) =>
                {
                    if ((sender as Gui.Widgets.ListView).SelectedIndex >= 0 &&
                        (sender as Gui.Widgets.ListView).SelectedIndex < Goals.Count)
                    {
                        right.Hidden = false;
                        right.Goal = Goals[(sender as Gui.Widgets.ListView).SelectedIndex];
                    }
                    else
                        right.Hidden = true;
                }
            }) as Gui.Widgets.ListView;
        }

        protected override Gui.Mesh Redraw()
        {
            RebuildGoalList();
            return base.Redraw();
        }
    }
}
