using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should destroy a voxel via digging.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class KillVoxelTask : Task
    {
        public Voxel VoxelToKill = null;

        public KillVoxelTask()
        {
            Priority = PriorityType.Low;
        }

        public KillVoxelTask(Voxel vox)
        {
            Name = "Kill Voxel: " + vox.Position;
            VoxelToKill = vox;
            Priority = PriorityType.Low;
        }

        public override Task Clone()
        {
            return new KillVoxelTask(VoxelToKill);
        }

        public override Act CreateScript(Creature creature)
        {
            return new KillVoxelAct(VoxelToKill, creature.AI);
        }

        public override bool ShouldRetry(Creature agent)
        {
            Voxel v = VoxelToKill;
            return !v.IsEmpty && agent.Faction.IsDigDesignation(v);
        }

        public override bool IsFeasible(Creature agent)
        {
            if(VoxelToKill == null || VoxelToKill.IsEmpty || VoxelToKill.IsDead)
            {
                return false;
            }

            Voxel vox = VoxelToKill;

            if(vox == null)
            {
                return false;
            }

            agent.Faction.IsDigDesignation(VoxelToKill);
            return true;
        }

        public override float ComputeCost(Creature agent)
        {
            if(VoxelToKill == null)
            {
                return 1000;
            }
            Voxel vox = VoxelToKill;

            if (vox == null)
            {
                return 10000;
            }

            int surroundedValue = 0;
            if(vox.Chunk.IsCompletelySurrounded(VoxelToKill))
            {
                surroundedValue = 100000;
            }

            return (agent.AI.Position - VoxelToKill.Position).LengthSquared() + 100 * Math.Abs(agent.AI.Position.Y - VoxelToKill.Position.Y) + surroundedValue;
        }

        public override void Render(Microsoft.Xna.Framework.GameTime time)
        {
            base.Render(time);
        }
    }

}