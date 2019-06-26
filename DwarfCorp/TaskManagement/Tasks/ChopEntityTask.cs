using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class ChopEntityTask : Task
    {
        public GameComponent EntityToKill = null;

        public ChopEntityTask()
        {
            MaxAssignable = 3;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        public ChopEntityTask(GameComponent entity)
        {
            MaxAssignable = 3;
            Name = "Harvest Plant: " + entity.Name + " " + entity.GlobalID;
            EntityToKill = entity;
            Priority = PriorityType.Medium;
            AutoRetry = true;
            Category = TaskCategory.Chop;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        public override Act CreateScript(Creature creature)
        {
            if (creature.IsDead || creature.AI.IsDead)
                return null;

            // Todo: Ugh - need to seperate the acts as well
            return new ChopEntityAct(EntityToKill, creature.AI);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            if (agent == null || EntityToKill == null)
                return 10000;
            else return (agent.AI.Position - EntityToKill.LocalTransform.Translation).LengthSquared() * 0.01f;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return EntityToKill != null && !EntityToKill.IsDead && agent.World.PersistentData.Designations.IsDesignation(EntityToKill, DesignationType.Chop);
        }

        public override bool ShouldDelete(Creature agent)
        {
            if (EntityToKill == null || EntityToKill.IsDead || (EntityToKill.Position - agent.AI.Position).Length() > 100)
                return true;

            if (!agent.World.PersistentData.Designations.IsDesignation(EntityToKill, DesignationType.Chop))
            {
                return true;
            }

            return false;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent == null || agent.IsDead || EntityToKill == null || EntityToKill.IsDead)
                return Feasibility.Infeasible;
            else
            {

                if (!agent.Stats.IsTaskAllowed(Task.TaskCategory.Chop))
                    return Feasibility.Infeasible;

                if (!agent.World.PersistentData.Designations.IsDesignation(EntityToKill, DesignationType.Chop))
                {
                    return Feasibility.Infeasible;
                }

                return Feasibility.Feasible;
            }
        }

        public override bool IsComplete(WorldManager World)
        {
            return EntityToKill == null || EntityToKill.IsDead;
        }

        public override void OnEnqueued(WorldManager World)
        {
            World.PersistentData.Designations.AddEntityDesignation(EntityToKill, DesignationType.Chop, null, this);
        }

        public override void OnDequeued(WorldManager World)
        {
            World.PersistentData.Designations.RemoveEntityDesignation(EntityToKill, DesignationType.Chop);
        }
    }

}
