using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class Goal
    {
        public WorldState State { get; set; }
        public float Priority { get; set; }
        public float Cost { get; set; }
        public string Name { get; set; }
        public GOAP Agent { get; set; }

        public Goal()
        {
            State = new WorldState();
            Priority = 0;
            Cost = 0;
            Name = "";
        }

        public Goal(WorldState state, string name, float priority)
        {
            State = state;
            Priority = priority;
            Name = name;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Goal))
            {
                return false;
            }
            else
            {
                return ((Goal)obj).Name == Name;
            }
        }

        public virtual void Reset(GOAP agent)
        {
        }

        public virtual void ContextReweight(CreatureAIComponent creature)
        {

        }

        public virtual bool ContextValidate(CreatureAIComponent creature)
        {
            return true;
        }

        public virtual List<Action> GetPresetPlan(CreatureAIComponent creature, GOAP agent)
        {
            return null;
        }

    }

}
