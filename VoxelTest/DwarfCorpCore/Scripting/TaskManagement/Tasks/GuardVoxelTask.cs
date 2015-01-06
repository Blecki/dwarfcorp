using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should guard a voxel.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class GuardVoxelTask : Task
    {
        public Voxel VoxelToGuard = null;

        public GuardVoxelTask(Voxel vox)
        {
            Name = "Guard Voxel: " + vox.Position;
            VoxelToGuard = vox;
            Priority = PriorityType.Medium;
        }

        public override Task Clone()
        {
            return new GuardVoxelTask(VoxelToGuard);
        }

        public override Act CreateScript(Creature agent)
        {
            return new GuardVoxelAct(agent.AI, VoxelToGuard);
        }

        public override float ComputeCost(Creature agent)
        {
            return VoxelToGuard == null ? 1000 : (agent.AI.Position - VoxelToGuard.Position).LengthSquared();
        }

        public override bool ShouldRetry(Creature agent)
        {
            return agent.Faction.IsGuardDesignation(VoxelToGuard);
        }

        public override void Render(DwarfTime time)
        {
            BoundingBox box = VoxelToGuard.GetBoundingBox();


            Color drawColor = Color.LightBlue;

      

            drawColor.R = (byte)(Math.Min(drawColor.R * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * 0.5f)) + 50, 255));
            drawColor.G = (byte)(Math.Min(drawColor.G * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * 0.5f)) + 50, 255));
            drawColor.B = (byte)(Math.Min(drawColor.B * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * 0.5f)) + 50, 255));
            Drawer3D.DrawBox(box, drawColor, 0.05f, true);
            base.Render(time);
        }
    }

}