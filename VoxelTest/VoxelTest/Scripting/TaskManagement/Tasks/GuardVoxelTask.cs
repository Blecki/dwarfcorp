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
        public VoxelRef VoxelToGuard = null;

        public GuardVoxelTask(VoxelRef vox)
        {
            Name = "Guard Voxel: " + vox.WorldPosition;
            VoxelToGuard = vox;
        }

        public override Act CreateScript(Creature agent)
        {
            return new GuardVoxelAct(agent.AI, VoxelToGuard);
        }

        public override float ComputeCost(Creature agent)
        {
            return VoxelToGuard == null ? 1000 : (agent.AI.Position - VoxelToGuard.WorldPosition).LengthSquared();
        }

        public override void Render(GameTime time)
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