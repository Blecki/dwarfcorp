using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace DwarfCorp
{
    public partial class VoxelChunk
    {
        private static Perlin MoteNoise = new Perlin(0);
        private static Perlin MoteScaleNoise = new Perlin(250);

        private static float Clamp(float v, float a)
        {
            if (v > a) return a;
            if (v < -a) return -a;
            return v;
        }

        private static Vector3 ClampVector(Vector3 v, float a)
        {
            v.X = Clamp(v.X, a);
            v.Y = Clamp(v.Y, a);
            v.Z = Clamp(v.Z, a);
            return v;
        }

        public void RenderMotes(GraphicsDevice Device, Shader Effect, Camera Camera)
        {
            for (var i = 0; i < Manager.World.Master.MaxViewingLevel + 1 && i < VoxelConstants.ChunkSizeY; ++i)
            {
                var motes = MoteRecords[i];
                if (motes != null)
                    for (int k = 0; k < motes.Count; k++)
                        Manager.World.InstanceRenderer.RenderInstance(motes[k], Device, Effect, Camera, InstanceRenderMode.Normal);
            }
        }

        private static NewInstanceData GenerateGrassMote(WorldManager World, Vector3 Position, Color Color, float Scale, String Name)
        {
            return new NewInstanceData(Name, Matrix.CreateScale(Scale) * Matrix.CreateRotationY(Scale * Scale) * Matrix.CreateTranslation(Position), Color);
        }
        
        public void RebuildMoteLayerIfNull(int LocalY)
        {
            if (MoteRecords[LocalY] == null)
                RebuildMoteLayer(LocalY);
        }

        public void RebuildMoteLayer(int LocalY)
        {
#if DEBUG
            if (LocalY < 0 || LocalY >= VoxelConstants.ChunkSizeY)
                throw new InvalidOperationException();
#endif

            var moteList = new List<NewInstanceData>();
            
            // Enumerate voxels.
            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
            {
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    var v = VoxelHandle.UnsafeCreateLocalHandle(this, new LocalVoxelCoordinate(x, LocalY, z));
                    if (!v.IsValid)
                        continue;

                    // Don't generate in empty voxels.
                    if (v.IsEmpty)
                        continue;

                    if (!v.IsExplored)
                        continue;

                    // Don't generate motes if above is not empty
                    var voxelAbove = VoxelHelpers.GetVoxelAbove(v);
                        if (voxelAbove.IsValid && (!voxelAbove.IsEmpty || voxelAbove.LiquidLevel != 0))
                            continue;

                    // Find biome type.
                    var biomeData = Overworld.GetBiomeAt(v.WorldPosition, Manager.World.WorldScale, Manager.World.WorldOrigin);  

                    // Don't generate if not on grass type.
                    if (v.GrassType == 0 || GrassLibrary.GetGrassType(v.GrassType).Name != biomeData.GrassDecal)
                        continue;

                    // Biomes can contain multiple types of mote.
                    foreach (var moteDetail in biomeData.Motes)
                    {
                        // Lower mote if voxel is ramped.
                        float vOffset = 0.0f;
                        if (v.RampType != RampType.None)
                            vOffset = -0.5f;

                        var vPos = v.WorldPosition * moteDetail.RegionScale;
                        float value = MoteNoise.Noise(vPos.X, vPos.Y, vPos.Z);

                        if (!(Math.Abs(value) > moteDetail.SpawnThreshold))
                            continue;

                        float s = MoteScaleNoise.Noise(vPos.X, vPos.Y, vPos.Z) * moteDetail.MoteScale;

                        var smallNoise = ClampVector(VertexNoise.GetRandomNoiseVector(vPos * 20.0f) * 20.0f, 0.4f);
                        smallNoise.Y = 0.0f;

                        var mote = GenerateGrassMote(
                            Manager.World,
                            v.WorldPosition + new Vector3(0.5f, 1.0f + s * 0.5f + vOffset, 0.5f) + smallNoise,
                            new Color(v.Sunlight ? 255 : 0, 128, 0),
                            s,
                            moteDetail.Name);

                        moteList.Add(mote);
                    }
                }
            }

            MoteRecords[LocalY] = moteList;
        }
    }
}
