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
        public static void GenerateCluster(OreCluster Cluster, ChunkData Chunks)
        {
            for (float x = -Cluster.Size.X * 0.5f; x < Cluster.Size.X * 0.5f; x += 1.0f)
            {
                for (float y = -Cluster.Size.Y * 0.5f; y < Cluster.Size.Y * 0.5f; y += 1.0f)
                {
                    for (float z = -Cluster.Size.Z * 0.5f; z < Cluster.Size.Z * 0.5f; z += 1.0f)
                    {
                        float radius = (float)(Math.Pow(x / Cluster.Size.X, 2.0f) + Math.Pow(y / Cluster.Size.Y, 2.0f) +
                                       Math.Pow(z / Cluster.Size.Z, 2.0f));

                        if (radius > 1.0f + MathFunctions.Rand(0.0f, 0.25f)) continue;
                        Vector3 locPosition = new Vector3(x, y, z);

                        Vector3 globalPosition = Vector3.Transform(locPosition, Cluster.Transform);

                        if (globalPosition.Y > Cluster.Type.MaxSpawnHeight ||
                            globalPosition.Y < Cluster.Type.MinSpawnHeight ||
                            globalPosition.Y <= 1) continue;

                        var vox = new VoxelHandle(Chunks, GlobalVoxelCoordinate.FromVector3(globalPosition));

                        if (!vox.IsValid || vox.IsEmpty) continue;

                        if (!Cluster.Type.SpawnOnSurface && (vox.Type.IsSurface || vox.Type.IsSoil)) continue;

                        if (!MathFunctions.RandEvent(Cluster.Type.SpawnProbability)) continue;

                        vox.RawSetType(Cluster.Type);
                    }
                }
            }
        }
    }
}