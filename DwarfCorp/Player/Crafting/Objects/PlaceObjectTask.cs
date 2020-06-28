using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    internal class PlaceObjectTask : Task
    {
        public PlacementDesignation PlacementDesignation;

        public PlaceObjectTask()
        {
            MaxAssignable = 1;
            Priority = TaskPriority.Medium;
            AutoRetry = true;
            BoredomIncrease = GameSettings.Current.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Current.Energy_Tiring;
        }

        public PlaceObjectTask(PlacementDesignation PlacementDesignation)
        {
            this.PlacementDesignation = PlacementDesignation;

            Category = TaskCategory.BuildObject;
            MaxAssignable = 1;
            Name = Library.GetString("craft-at", PlacementDesignation.Entity.GlobalID, PlacementDesignation.ItemType.DisplayName, PlacementDesignation.Location);
            Priority = TaskPriority.Medium;
            AutoRetry = true;

            foreach (var tinter in PlacementDesignation.Entity.EnumerateAll().OfType<Tinter>())
                tinter.Stipple = true;

            BoredomIncrease = GameSettings.Current.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Current.Energy_Tiring;
        }

        public override void OnEnqueued(WorldManager World)
        {
            World.PersistentData.Designations.AddEntityDesignation(PlacementDesignation.Entity, DesignationType.PlaceObject, PlacementDesignation, this);
        }

        public override void OnDequeued(WorldManager World)
        {
            if (!PlacementDesignation.Finished)
            {
                if (PlacementDesignation.WorkPile != null) PlacementDesignation.WorkPile.GetRoot().Delete();
                if (PlacementDesignation.SelectedResource != null) // Don't try and create an entity if there is no resource.
                {
                    var resourceEntity = new ResourceEntity(World.ComponentManager, PlacementDesignation.SelectedResource, PlacementDesignation.Entity.GlobalTransform.Translation);
                    World.ComponentManager.RootComponent.AddChild(resourceEntity);
                }
                PlacementDesignation.Entity.GetRoot().Delete();
            }

            World.PersistentData.Designations.RemoveEntityDesignation(PlacementDesignation.Entity, DesignationType.PlaceObject);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return !PlacementDesignation.Location.IsValid || !CanBuild(agent) ? 1000 : (agent.AI.Position - PlacementDesignation.Location.WorldPosition).LengthSquared();
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
        {
            return new PlaceObjectAct(creature.AI, PlacementDesignation);
        }

        public override bool ShouldRetry(Creature agent)
        {
            return !IsComplete(agent.World);
        }


        public override bool ShouldDelete(Creature agent)
        {
            return PlacementDesignation.Finished;
        }

        public override bool IsComplete(WorldManager World)
        {
            return PlacementDesignation.Finished;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent.Stats.IsAsleep || agent.IsDead || !agent.Active)
                return Feasibility.Infeasible;

            if (!agent.Stats.IsTaskAllowed(TaskCategory.BuildObject))
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