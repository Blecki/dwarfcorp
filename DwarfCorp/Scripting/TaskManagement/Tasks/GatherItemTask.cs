using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should pick up an item and put it in a stockpile.
    /// </summary>
    internal class GatherItemTask : Task
    {
        public GameComponent EntityToGather = null;
        public string ZoneType = "Stockpile";

        public GatherItemTask()
        {
            Priority = PriorityType.Medium;
            Category = TaskCategory.Gather;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        public GatherItemTask(GameComponent entity)
        {
            EntityToGather = entity;
            Name = "Gather Entity: " + entity.Name + " " + entity.GlobalID;
            Priority = PriorityType.Medium;
            Category = TaskCategory.Gather;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        public override Act CreateScript(Creature creature)
        {
            return new GatherItemAct(creature.AI, EntityToGather);
        }

        public override bool ShouldDelete(Creature agent)
        {
            return IsFeasible(agent) == Feasibility.Infeasible;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return agent.Stats.IsTaskAllowed(Category) &&
                      EntityToGather != null
                   && EntityToGather.Active
                   && !agent.AI.Movement.IsSessile
                   && !agent.AI.Stats.IsAsleep ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return EntityToGather != null &&
                  EntityToGather.Active &&
                  !EntityToGather.IsDead &&
                  !agent.AI.GatherManager.ItemsToGather.Contains(EntityToGather);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return EntityToGather == null  || EntityToGather.IsDead ? 1000 : (agent.AI.Position - EntityToGather.GlobalTransform.Translation).LengthSquared();
        }
        
        public override bool IsComplete(Faction Faction)
        {
            return EntityToGather == null || !EntityToGather.Active || EntityToGather.IsDead;
        }

        public override void OnEnqueued(Faction Faction)
        {
            Faction.Designations.AddEntityDesignation(EntityToGather, DesignationType.Gather, null, this);
        }

        public override void OnDequeued(Faction Faction)
        {
            Faction.Designations.RemoveEntityDesignation(EntityToGather, DesignationType.Gather);
        }
    }

}