using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp.Generation
{
    public static partial class Generator
    {
        public static  void GenerateOres(ChunkData ChunkData)
        {
            // This needs to be changed to a method to determine if any particular voxel should be grouped into an ore cluster. 

            foreach (VoxelType type in VoxelLibrary.GetTypes())
            {
                if (type.SpawnClusters || type.SpawnVeins)
                {
                    BoundingBox clusterBounds = new BoundingBox
                    {
                        Max = new Vector3(ChunkData.MapOrigin.X + ChunkData.MapDimensions.X, type.MaxSpawnHeight, ChunkData.MapOrigin.Z + ChunkData.MapDimensions.Z),
                        Min = new Vector3(ChunkData.MapOrigin.X, Math.Max(type.MinSpawnHeight, 2), ChunkData.MapOrigin.Z)
                    };

                    // Rarity is an inverse, but the max for any type is 100...
                    int numEvents = (int)MathFunctions.Rand(75 * (1.0f - type.Rarity), 100 * (1.0f - type.Rarity)); // Todo: Jesus christ, larger worlds don't have any more resources than small ones!
                    for (int i = 0; i < numEvents; i++)
                    {
                        if (type.SpawnClusters)
                        {
                            OreCluster cluster = new OreCluster()
                            {
                                Size =
                                    new Vector3(MathFunctions.Rand(type.ClusterSize * 0.25f, type.ClusterSize),
                                        MathFunctions.Rand(type.ClusterSize * 0.25f, type.ClusterSize),
                                        MathFunctions.Rand(type.ClusterSize * 0.25f, type.ClusterSize)),
                                Transform = MathFunctions.RandomTransform(clusterBounds),
                                Type = type
                            };

                            GenerateCluster(cluster, ChunkData);
                        }

                        if (type.SpawnVeins)
                        {
                            OreVein vein = new OreVein()
                            {
                                Length = MathFunctions.Rand(type.VeinLength * 0.75f, type.VeinLength * 1.25f),
                                Start = MathFunctions.RandVector3Box(clusterBounds),
                                Type = type
                            };

                            GenerateVein(vein, ChunkData);
                        }
                    }
                }
            }
        }
    }
}
