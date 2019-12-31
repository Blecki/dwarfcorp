using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class KillVoxelTask : Task
    {
        public VoxelHandle Voxel = VoxelHandle.InvalidHandle;
        public float VoxelHealth { get; set; }
        private Feasibility CachedIsFeasible = Feasibility.Feasible;
        private bool CacheDirty = true;

        public KillVoxelTask()
        {
            MaxAssignable = 3;
            Priority = TaskPriority.Medium;
            Category = TaskCategory.Dig;
            BoredomIncrease = GameSettings.Current.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Current.Energy_Arduous;
        }

        public KillVoxelTask(VoxelHandle vox)
        {
            MaxAssignable = 3;
            Name = "Mine Block " + vox.Coordinate;
            Voxel = vox;
            Priority = TaskPriority.Medium;
            Category = TaskCategory.Dig;
            VoxelHealth = Voxel.Type.StartingHealth;
            BoredomIncrease = GameSettings.Current.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Current.Energy_Arduous;
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
        {
            return new KillVoxelAct(creature.AI, this);
        }

        public override bool ShouldRetry(Creature agent)
        {
            return !Voxel.IsEmpty;
        }

        private Feasibility ComputeFeasible()
        {
            if (!Voxel.IsValid || Voxel.IsEmpty)
                return Feasibility.Infeasible;

            if (VoxelHelpers.VoxelIsCompletelySurrounded(Voxel) || VoxelHelpers.VoxelIsSurroundedByWater(Voxel))
                return Feasibility.Infeasible;

            return Feasibility.Feasible;
        }


        public override void OnVoxelChange(VoxelChangeEvent changeEvent)
        {
            CacheDirty = true;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent.Stats.IsAsleep || agent.IsDead || !agent.Active)
                return Feasibility.Infeasible;

            if (!agent.Stats.IsTaskAllowed(TaskCategory.Dig))
                return Feasibility.Infeasible;

            if (agent.AI.Stats.IsAsleep)
                return Feasibility.Infeasible;

            if (!CacheDirty)
            {
                return CachedIsFeasible;
            }

            CachedIsFeasible = ComputeFeasible();
            CacheDirty = false;
            return CachedIsFeasible;
        }

        public override bool ShouldDelete(Creature agent)
        {
            return !Voxel.IsValid || Voxel.IsEmpty;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return (agent.AI.Position - Voxel.WorldPosition).LengthSquared() + 10 * Math.Abs(agent.World.WorldSizeInVoxels.Y - Voxel.Coordinate.Y); // Is this a bias to make deeper voxels more costly?
        }

        public override bool IsComplete(WorldManager World)
        {
            if (!Voxel.IsValid) return false;
            return Voxel.IsEmpty;
        }

        public override void OnEnqueued(WorldManager World)
        {
            World.PersistentData.Designations.AddVoxelDesignation(Voxel, DesignationType.Dig, null, this);
        }

        public override void OnDequeued(WorldManager World)
        {
            World.PersistentData.Designations.RemoveVoxelDesignation(Voxel, DesignationType.Dig);
        }

        public override Vector3? GetCameraZoomLocation()
        {
            return Voxel.Center;
        }

    }

}