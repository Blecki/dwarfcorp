using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    internal class CraftItemTask : Task
    {
        public CraftDesignation CraftDesignation;

        public CraftItemTask()
        {
            MaxAssignable = 3;
            Priority = TaskPriority.Medium;
            AutoRetry = true;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Default.Energy_Tiring;
        }

        public CraftItemTask(CraftDesignation CraftDesignation)
        {
            Category = TaskCategory.BuildObject;
            MaxAssignable = 3;
            Name = Library.GetString("craft-at", CraftDesignation.Entity.GlobalID, CraftDesignation.ItemType.DisplayName, CraftDesignation.Location);
            Priority = TaskPriority.Medium;
            AutoRetry = true;
            this.CraftDesignation = CraftDesignation;

            foreach (var tinter in CraftDesignation.Entity.EnumerateAll().OfType<Tinter>())
                tinter.Stipple = true;

            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Default.Energy_Tiring;

            if (CraftDesignation.ItemType.IsMagical)
                Category = TaskCategory.Research;

            if (CraftDesignation.ExistingResource != null)
            {
                MaxAssignable = 1;
            }
        }

        public override void OnEnqueued(WorldManager World)
        {
            World.PersistentData.Designations.AddEntityDesignation(CraftDesignation.Entity, DesignationType.Craft, CraftDesignation, this);
        }

        public override void OnDequeued(WorldManager World)
        {
            if (!CraftDesignation.Finished)
            {
                if (CraftDesignation.WorkPile != null) CraftDesignation.WorkPile.GetRoot().Delete();
                if (CraftDesignation.HasResources)
                    foreach (var resource in CraftDesignation.SelectedResources)
                    {
                        var resourceEntity = new ResourceEntity(World.ComponentManager, resource, CraftDesignation.Entity.GlobalTransform.Translation);
                        World.ComponentManager.RootComponent.AddChild(resourceEntity);
                    }
                CraftDesignation.Entity.GetRoot().Delete();
            }

            World.PersistentData.Designations.RemoveEntityDesignation(CraftDesignation.Entity, DesignationType.Craft);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return !CraftDesignation.Location.IsValid || !CanBuild(agent) ? 1000 : (agent.AI.Position - CraftDesignation.Location.WorldPosition).LengthSquared();
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
        {
            return new CraftItemAct(creature.AI, CraftDesignation);
        }

        public override bool ShouldRetry(Creature agent)
        {
            return !IsComplete(agent.World);
        }


        public override bool ShouldDelete(Creature agent)
        {
            return CraftDesignation.Finished;
        }

        public override bool IsComplete(WorldManager World)
        {
            return CraftDesignation.Finished;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent.Stats.IsAsleep || agent.IsDead || !agent.Active)
                return Feasibility.Infeasible;

            if (!CraftDesignation.ItemType.IsMagical && !agent.Stats.IsTaskAllowed(TaskCategory.BuildObject))
                return Feasibility.Infeasible;

            if (CraftDesignation.ItemType.IsMagical && !agent.Stats.IsTaskAllowed(TaskCategory.Research))
                return Feasibility.Infeasible;

            if (agent.AI.Stats.IsAsleep)
                return Feasibility.Infeasible;

            return CanBuild(agent) && !IsComplete(agent.World) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public bool CanBuild(Creature agent)
        {
            if (CraftDesignation.ExistingResource != null) // This is a placement of an existing item.
            {
                bool hasResource = agent.World.HasResources(CraftDesignation.ExistingResource);
                return hasResource;
            }

            if (!String.IsNullOrEmpty(CraftDesignation.ItemType.CraftLocation))
            {
                var nearestBuildLocation = agent.Faction.FindNearestItemWithTags(CraftDesignation.ItemType.CraftLocation, Vector3.Zero, false, agent.AI);

                if (nearestBuildLocation == null)
                    return false;
            }

            foreach (var resourceAmount in CraftDesignation.ItemType.RequiredResources)
            {
                var resources = agent.World.ListResourcesWithTag(resourceAmount.Type, CraftDesignation.ItemType.AllowHeterogenous);
                if (resources.Count == 0 || !resources.Any(r => r.Count >= resourceAmount.Count))
                {
                    return false;
                }
            }

            return true;
        }
    }
}