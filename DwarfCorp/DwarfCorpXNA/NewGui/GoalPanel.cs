using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class GoalPanel : TwoColumns
    {
        private Gum.Widgets.ListView GoalList;
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

            var right = AddChild(new NewGui.GoalInfo
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
            }) as NewGui.GoalInfo;

            GoalList = left.AddChild(new Gum.Widgets.ListView
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "font-hires",
                OnSelectedIndexChanged = (sender) =>
                {
                    if ((sender as Gum.Widgets.ListView).SelectedIndex >= 0 &&
                        (sender as Gum.Widgets.ListView).SelectedIndex < Goals.Count)
                    {
                        right.Hidden = false;
                        right.Goal = Goals[(sender as Gum.Widgets.ListView).SelectedIndex];
                    }
                    else
                        right.Hidden = true;
                }
            }) as Gum.Widgets.ListView;
        }

        protected override Gum.Mesh Redraw()
        {
            RebuildGoalList();
            return base.Redraw();
        }
    }
}
