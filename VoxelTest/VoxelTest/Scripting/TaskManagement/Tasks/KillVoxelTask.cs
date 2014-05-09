using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should destroy a voxel via digging.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class KillVoxelTask : Task
    {
        public VoxelRef VoxelToKill = null;

        public KillVoxelTask()
        {

        }

        public KillVoxelTask(VoxelRef vox)
        {
            Name = "Kill Voxel: " + vox.WorldPosition;
            VoxelToKill = vox;
        }

        public override Act CreateScript(Creature creature)
        {
            return new KillVoxelAct(VoxelToKill, creature.AI);
        }

        public override bool ShouldRetry(Creature agent)
        {
            Voxel v = VoxelToKill.GetVoxel(false);
            return v != null && agent.Faction.IsDigDesignation(v);
        }

        public override bool IsFeasible(Creature agent)
        {
            if(VoxelToKill == null || VoxelToKill.TypeName == "empty")
            {
                return false;
            }

            Voxel vox = VoxelToKill.GetVoxel(false);

            if(vox == null)
            {
                return false;
            }

            return true;
        }

        public override float ComputeCost(Creature agent)
        {
            return VoxelToKill == null ? 1000 : (agent.AI.Position - VoxelToKill.WorldPosition).LengthSquared() + 100 * Math.Abs(agent.AI.Position.Y - VoxelToKill.WorldPosition.Y);
        }

        public override void Render(Microsoft.Xna.Framework.GameTime time)
        {
            
            BoundingBox box = VoxelToKill.GetBoundingBox();


            Color drawColor = new Color(205, 200, 10);

            drawColor.R = (byte)(drawColor.R * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * 0.5f)) + 50);
            drawColor.G = (byte)(drawColor.G * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * 0.5f)) + 50);
            drawColor.B = (byte)(drawColor.B * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * 0.5f)) + 50);
            Drawer3D.DrawBox(box, drawColor, 0.05f, true);
            base.Render(time);
        }
    }

}