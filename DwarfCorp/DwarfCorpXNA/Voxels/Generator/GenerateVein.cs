using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DwarfCorp.GameStates;
using LibNoise;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Math = System.Math;

namespace DwarfCorp.Generation
{
    public static partial class Generator
    {
        public static void GenerateVein(OreVein Vein, ChunkData Chunks)
        {
            Vector3 curr = Vein.Start;
            Vector3 directionBias = MathFunctions.RandVector3Box(-1, 1, -0.1f, 0.1f, -1, 1);
            for (float t = 0; t < Vein.Length; t++)
            {
                if (curr.Y > Vein.Type.MaxSpawnHeight ||
                    curr.Y < Vein.Type.MinSpawnHeight ||
                    curr.Y <= 1) continue;
                Vector3 p = new Vector3(curr.X, curr.Y, curr.Z);

                var vox = new VoxelHandle(Chunks, GlobalVoxelCoordinate.FromVector3(p));

                if (!vox.IsValid || vox.IsEmpty) continue;

                if (!MathFunctions.RandEvent(Vein.Type.SpawnProbability)) continue;

                if (!Vein.Type.SpawnOnSurface && (vox.Type.IsSurface || vox.Type.IsSoil)) continue;

                vox.RawSetType(Vein.Type);
                Vector3 step = directionBias + MathFunctions.RandVector3Box(-1, 1, -1, 1, -1, 1) * 0.25f;
                step.Normalize();
                curr += step;
            }
        }
    }
}