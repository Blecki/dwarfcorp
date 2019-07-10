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
            Priority = TaskPriority.Medium;
            Category = TaskCategory.Gather;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Default.Energy_Tiring;
        }

        public GatherItemTask(GameComponent entity)
        {
            EntityToGather = entity;
            Name = "Gather Entity: " + entity.Name + " " + entity.GlobalID;
            Priority = TaskPriority.Medium;
            Category = TaskCategory.Gather;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Default.Energy_Tiring;
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
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
            return EntityToGather != null && EntityToGather.Active && !EntityToGather.IsDead;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return EntityToGather == null  || EntityToGather.IsDead ? 1000 : (agent.AI.Position - EntityToGather.GlobalTransform.Translation).LengthSquared();
        }
        
        public override bool IsComplete(WorldManager World)
        {
            return EntityToGather == null || !EntityToGather.Active || EntityToGather.IsDead;
        }

        public override void OnEnqueued(WorldManager World)
        {
            World.PersistentData.Designations.AddEntityDesignation(EntityToGather, DesignationType.Gather, null, this);
        }

        public override void OnDequeued(WorldManager World)
        {
            World.PersistentData.Designations.RemoveEntityDesignation(EntityToGather, DesignationType.Gather);
        }
    }

}