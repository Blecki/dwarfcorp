using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class KillEntityTask : Task
    {
        public enum KillType
        {
            Attack,
            Auto
        }
        public GameComponent EntityToKill = null;
        public KillType Mode { get; set; }
        public bool Cancelled = false;

        public KillEntityTask()
        {
            MaxAssignable = 64;
            BoredomIncrease = GameSettings.Current.Boredom_ExcitingTask;
            EnergyDecrease = GameSettings.Current.Energy_Arduous;
        }

        public KillEntityTask(GameComponent entity, KillType type)
        {
            MaxAssignable = 64;
            Mode = type;
            Name = "Kill Entity: " + entity.Name + " " + entity.GlobalID;
            EntityToKill = entity;
            Priority = TaskPriority.Urgent;
            AutoRetry = true;
            Category = TaskCategory.Attack;
            BoredomIncrease = GameSettings.Current.Boredom_ExcitingTask;
            EnergyDecrease = GameSettings.Current.Energy_Arduous;

            if (type == KillType.Auto)
                ReassignOnDeath = false;
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
        {
            if (creature.IsDead || creature.AI.IsDead)
                return null;

            if (EntityToKill.GetRoot().GetComponent<Creature>().HasValue(out var otherCreature))
            {
                if (!otherCreature.IsDead && otherCreature.AI != null)
                {
                    // Flee if the other creature is too scary.
                    if (otherCreature != null && (creature.AI.Position - EntityToKill.Position).Length() < 10 && creature.AI.FightOrFlight(otherCreature.AI) == CreatureAI.FightOrFlightResponse.Flee)
                    {
                        Name = "Flee Entity: " + EntityToKill.Name + " " + EntityToKill.GlobalID;
                        ReassignOnDeath = false;
                        IndicatorManager.DrawIndicator(IndicatorManager.StandardIndicators.Exclaim, creature.AI.Position, 1.0f, 1.0f, Vector2.UnitY * -32);
                        return new FleeEntityAct(creature.AI) { Entity = EntityToKill, PathLength = 20 };
                    }

                    // Make the other creature defend itself.
                    var otherKill = new KillEntityTask(creature.Physics, KillType.Auto)
                    {
                        AutoRetry = true,
                        ReassignOnDeath = false
                    };

                    if (!otherCreature.AI.HasTaskWithName(otherKill))
                        otherCreature.AI.AssignTask(otherKill);
                }
            }

            float radius = this.Mode == KillType.Auto ? 20.0f : 0.0f;
            return new KillEntityAct(EntityToKill, creature.AI) { RadiusDomain = radius, Defensive = Mode == KillType.Auto };
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

            if (Mode == KillType.Attack 
                && Object.ReferenceEquals(agent.Faction, agent.World.PlayerFaction) 
                && !agent.World.PersistentData.Designations.GetEntityDesignation(EntityToKill, DesignationType.Attack).HasValue())
                return true;

            if (Mode == KillType.Auto && (agent.AI.Position - EntityToKill.Position).Length() > 20)
                return true;
            
            return false;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent == null || agent.IsDead || EntityToKill == null || EntityToKill.IsDead)
                return Feasibility.Infeasible;
            else
            {
                if (Mode == KillType.Attack && !agent.Stats.IsTaskAllowed(TaskCategory.Attack))
                    return Feasibility.Infeasible;

                if (Mode == KillType.Auto && (agent.AI.Position - EntityToKill.Position).Length() > 20)
                    return Feasibility.Infeasible;

                if (Mode == KillType.Attack
                && Object.ReferenceEquals(agent.Faction, agent.World.PlayerFaction)
                && !agent.World.PersistentData.Designations.GetEntityDesignation(EntityToKill, DesignationType.Attack).HasValue())
                    return Feasibility.Infeasible;

                return Feasibility.Feasible;
            }
        }

        public override bool IsComplete(WorldManager World)
        {
            return Cancelled || EntityToKill == null || EntityToKill.IsDead;
        }

        public override void OnEnqueued(WorldManager World)
        {
            World.PersistentData.Designations.AddEntityDesignation(EntityToKill, DesignationType.Attack, null, this);
        }

        public override void OnDequeued(WorldManager World)
        {
            World.PersistentData.Designations.RemoveEntityDesignation(EntityToKill, DesignationType.Attack);
            Cancelled = true;
        }

        public override Vector3? GetCameraZoomLocation()
        {
            return EntityToKill?.Position;
        }
    }

}
