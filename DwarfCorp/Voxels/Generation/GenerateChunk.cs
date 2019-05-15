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
        public static VoxelChunk GenerateChunk(GlobalChunkCoordinate ID, GeneratorSettings Settings)
        {
            var origin = new GlobalVoxelCoordinate(ID, new LocalVoxelCoordinate(0, 0, 0));
            var worldDepth = Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY;
            var waterHeight = NormalizeHeight(Settings.OverworldSettings.SeaLevel + 1.0f / worldDepth);

            var c = new VoxelChunk(Settings.World.ChunkManager, ID);

            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    var overworldPosition = Overworld.WorldToOverworld(new Vector2(x + origin.X, z + origin.Z), Settings.OverworldSettings.InstanceSettings.Origin);
                    var biome = Settings.OverworldSettings.Overworld.Map[(int)MathFunctions.Clamp(overworldPosition.X, 0, Settings.OverworldSettings.Overworld.Map.GetLength(0) - 1), (int)MathFunctions.Clamp(overworldPosition.Y, 0, Settings.OverworldSettings.Overworld.Map.GetLength(1) - 1)].Biome;
                    var biomeData = BiomeLibrary.GetBiome(biome);

                    var normalizedHeight = NormalizeHeight(Overworld.LinearInterpolate(overworldPosition, Settings.OverworldSettings.Overworld.Map, OverworldField.Height));
                    var height = MathFunctions.Clamp(normalizedHeight * worldDepth, 0.0f, worldDepth - 2);
                    var stoneHeight = (int)MathFunctions.Clamp((int)(height - (biomeData.SoilLayer.Depth + (Math.Sin(overworldPosition.X) + Math.Cos(overworldPosition.Y)))), 1, height);

                    for (int y = 0; y < VoxelConstants.ChunkSizeY; y++)
                    {
                        var globalY = origin.Y + y;
                        var voxel = VoxelHandle.UnsafeCreateLocalHandle(c, new LocalVoxelCoordinate(x, y, z));

                        if (globalY == 0)
                        {
                            voxel.RawSetType(Library.GetVoxelType("Bedrock"));
                            continue;
                        }

                        // Below the stone line, use subsurface layers.
                        if (globalY <= stoneHeight && stoneHeight > 1)
                        {
                            var depth = stoneHeight - globalY;
                            var subsurfaceLayer = 0;
                            while (depth > 0 && subsurfaceLayer < biomeData.SubsurfaceLayers.Count - 1)
                            {
                                depth -= biomeData.SubsurfaceLayers[subsurfaceLayer].Depth;
                                subsurfaceLayer += 1;
                            }

                            voxel.RawSetType(Library.GetVoxelType(biomeData.SubsurfaceLayers[subsurfaceLayer].VoxelType));
                        }
                        // Otherwise, on the surface.
                        else if ((globalY == (int)height || globalY == stoneHeight) && normalizedHeight > waterHeight)
                        {
                            voxel.RawSetType(Library.GetVoxelType(biomeData.SoilLayer.VoxelType));

                            if (!String.IsNullOrEmpty(biomeData.GrassDecal))
                                if (!biomeData.ClumpGrass || (biomeData.ClumpGrass 
                                    && Settings.NoiseGenerator.Noise(overworldPosition.X / biomeData.ClumpSize, 0, overworldPosition.Y / biomeData.ClumpSize) > biomeData.ClumpTreshold))
                                    voxel.RawSetGrass(Library.GetGrassType(biomeData.GrassDecal).ID);
                        }
                        else if (globalY > height && globalY > 0)
                            voxel.RawSetType(Library.EmptyVoxelType);
                        else if (normalizedHeight <= waterHeight)
                            voxel.RawSetType(Library.GetVoxelType(biomeData.ShoreVoxel));
                        else
                            voxel.RawSetType(Library.GetVoxelType(biomeData.SoilLayer.VoxelType));
                    }
                }

            return c;
        }
    }
}
