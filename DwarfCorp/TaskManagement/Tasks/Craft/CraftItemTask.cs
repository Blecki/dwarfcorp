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
        public Stockpile ItemSource;

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
                if (CraftDesignation.PreviewResource != null) CraftDesignation.PreviewResource.GetRoot().Delete();
                var resourceEntity = new ResourceEntity(World.ComponentManager, CraftDesignation.SelectedResource, CraftDesignation.Entity.GlobalTransform.Translation);
                World.ComponentManager.RootComponent.AddChild(resourceEntity);
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
            return new CraftItemAct(creature.AI, CraftDesignation) { ItemSource = ItemSource };
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
            return true;
        }

        public override void OnCancelled(TaskManager Manager, WorldManager World)
        {
            base.OnCancelled(Manager, World);
        }
    }
}