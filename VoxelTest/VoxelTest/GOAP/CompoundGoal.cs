using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    internal class CompoundGoal : Goal
    {
        public List<Goal> Goals { get; set; }
        public int CurrentGoalIndex { get; set; }

        public CompoundGoal()
        {
            Goals = new List<Goal>();
            CurrentGoalIndex = 0;
        }

        public override void Reset(GOAP agent)
        {
            foreach(Goal g in Goals)
            {
                g.Reset(agent);
            }
            base.Reset(agent);
        }

        public override List<Action> GetPresetPlan(CreatureAIComponent creature, GOAP agent)
        {
            if(CurrentGoalIndex >= 0 && CurrentGoalIndex < Goals.Count)
            {
                return Goals[CurrentGoalIndex].GetPresetPlan(creature, agent);
            }
            else
            {
                return null;
            }
        }

        public override void ContextReweight(CreatureAIComponent creature)
        {
            //Goals[CurrentGoalIndex].ContextReweight(creature);
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            /*
            if (CurrentGoalIndex >= 0 && CurrentGoalIndex < Goals.Count)
            {
                return Goals[CurrentGoalIndex].ContextValidate(creature);
            }
            else return true;
             */
            return true;
        }
    }

}