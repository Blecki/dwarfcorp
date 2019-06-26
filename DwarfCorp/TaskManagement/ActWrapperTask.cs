using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class ActWrapperTask : Task
    {
        private Act WrappedAct;

        public ActWrapperTask()
        {
            
        }

        public ActWrapperTask(Act act)
        {
            ReassignOnDeath = false;
            WrappedAct = act;
            Name = WrappedAct.Name;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return WrappedAct != null ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override bool ShouldDelete(Creature agent)
        {
            if (WrappedAct == null)
                return true;
            return base.ShouldDelete(agent);
        }

        public override Act CreateScript(Creature agent)
        {
            if (WrappedAct != null)
                WrappedAct.Initialize();
            return WrappedAct;
        }

        public override bool IsComplete(WorldManager World)
        {
            if (WrappedAct == null)
                return true;
            return base.IsComplete(World);
        }

        public override bool ShouldRetry(Creature agent)
        {
            return false;
        }
    }

}