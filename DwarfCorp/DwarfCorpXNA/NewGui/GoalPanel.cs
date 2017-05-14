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
            GoalList.Items.Clear();
            Goals = GoalSource.ToList();
            GoalList.Items = Goals.Select(g => g.Name).ToList();
            GoalList.SelectedIndex = 0;
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
                        World.Master.GoalManager.ActivateGoal(World, (sender as GoalInfo).Goal);
                    Invalidate();
                }
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
            var index = GoalList.SelectedIndex;
            RebuildGoalList();
            GoalList.SelectedIndex = index;
            return base.Redraw();
        }
    }
}
