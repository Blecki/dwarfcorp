using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class FleeEntityTask : Task
    {
        public GameComponent ScaryEntity = null;
        public int Distance = 10;

        public FleeEntityTask()
        {
        }

        public FleeEntityTask(GameComponent entity, int Distance)
        {
            Name = "Kill Entity: " + entity.Name + " " + entity.GlobalID;
            ScaryEntity = entity;
            Priority = TaskPriority.Urgent;
            AutoRetry = true;
                Category = TaskCategory.Other;
            this.Distance = Distance;
            BoredomIncrease = GameSettings.Default.Boredom_ExcitingTask;
            EnergyDecrease = GameSettings.Default.Energy_Arduous;
        }

        public override Act CreateScript(Creature creature)
        {
            Name = "Flee Entity: " + ScaryEntity.Name + " " + ScaryEntity.GlobalID;
            IndicatorManager.DrawIndicator(IndicatorManager.StandardIndicators.Exclaim, creature.AI.Position, 1.0f, 1.0f, Vector2.UnitY * -32);
            return new FleeEntityAct(creature.AI) { Entity = ScaryEntity, PathLength = Distance };
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 0.0f;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return ScaryEntity != null && !ScaryEntity.IsDead;
        }

        public override bool ShouldDelete(Creature agent)
        {
            if (ScaryEntity == null || ScaryEntity.IsDead || (ScaryEntity.Position - agent.AI.Position).Length() > Distance)
                return true;
 
            return false;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent == null || agent.IsDead || ScaryEntity == null || ScaryEntity.IsDead)
                return Feasibility.Infeasible;
            else
                return Feasibility.Feasible;
        }

        public override bool IsComplete(WorldManager World)
        {
            return ScaryEntity == null || ScaryEntity.IsDead;
        }

    }
}
