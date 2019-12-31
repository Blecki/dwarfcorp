using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class DestroyObjectTask : Task
    {
        public GameComponent EntityToKill = null;

        public DestroyObjectTask()
        {
            MaxAssignable = 1;
            BoredomIncrease = GameSettings.Current.Boredom_ExcitingTask;
            EnergyDecrease = GameSettings.Current.Energy_Arduous;
        }

        public DestroyObjectTask(GameComponent entity)
        {
            MaxAssignable = 1;
            Name = "Destroy Object: " + entity.Name + " " + entity.GlobalID;
            EntityToKill = entity;
            Priority = TaskPriority.Medium;
            AutoRetry = true;
            Category = TaskCategory.CraftItem;
            BoredomIncrease = GameSettings.Current.Boredom_ExcitingTask;
            EnergyDecrease = GameSettings.Current.Energy_Arduous;
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
        {
            if (creature.IsDead || creature.AI.IsDead)
                return null;

            return new KillEntityAct(EntityToKill, creature.AI) { RadiusDomain = 0.0f, Defensive = false };
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            if (agent == null || EntityToKill == null)
            {
                return 10000;
            }

            else return (agent.AI.Position - EntityToKill.LocalTransform.Translation).LengthSquared() * 0.01f;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return EntityToKill != null && !EntityToKill.IsDead;
        }

        public override bool ShouldDelete(Creature agent)
        {
            if (EntityToKill == null || EntityToKill.IsDead || (EntityToKill.Position - agent.AI.Position).Length() > 100)
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
                if (!agent.Stats.IsTaskAllowed(TaskCategory.CraftItem))
                    return Feasibility.Infeasible;

                return Feasibility.Feasible;
            }
        }

        public override bool IsComplete(WorldManager World)
        {
            return EntityToKill == null || EntityToKill.IsDead;
        }

        public override void OnEnqueued(WorldManager World)
        {
            World.PersistentData.Designations.AddEntityDesignation(EntityToKill, DesignationType.Gather, null, this);
        }

        public override void OnDequeued(WorldManager World)
        {
            World.PersistentData.Designations.RemoveEntityDesignation(EntityToKill, DesignationType.Gather);
        }

        public override Vector3? GetCameraZoomLocation()
        {
            return EntityToKill?.Position;
        }
    }

}
