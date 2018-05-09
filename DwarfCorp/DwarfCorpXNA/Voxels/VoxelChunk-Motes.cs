// VoxelChunk.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
                if (MoteRecords[i] != null)
                    foreach (var mote in MoteRecords[i])
                        Manager.World.InstanceRenderer.RenderInstance(mote, Device, Effect, Camera, InstanceRenderMode.Normal);
        }

        private static NewInstanceData GenerateGrassMote(WorldManager World, Vector3 Position, Color Color, float Scale, String Name)
        {
            return new NewInstanceData(Name, Matrix.CreateScale(Scale) * Matrix.CreateRotationY(Scale * Scale) * Matrix.CreateTranslation(Position), Color);
        }
        
        private List<NewInstanceData>[] MoteRecords = new List<NewInstanceData>[VoxelConstants.ChunkSizeY];

        public void RebuildMoteLayerIfNull(int Y)
        {
            if (MoteRecords[Y] == null)
                RebuildMoteLayer(Y);
        }

        public void RebuildMoteLayer(int Y)
        {
            MoteRecords[Y] = new List<NewInstanceData>();
            
            // Enumerate voxels.
            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
            {
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    // Don't generate motes if above is not empty
                    if (Y < VoxelConstants.ChunkSizeY - 1)
                    {
                        var voxelAbove = new VoxelHandle(this, new LocalVoxelCoordinate(x, Y + 1, z));
                        if (!voxelAbove.IsEmpty || voxelAbove.WaterCell.WaterLevel != 0)
                            continue;
                    }

                    var v = new VoxelHandle(this, new LocalVoxelCoordinate(x, Y, z));

                    // Don't generate in empty voxels.
                    if (v.IsEmpty)
                        continue;

                    if (!v.IsExplored)
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
                            new Color(v.SunColor, 128, 0),
                            s,
                            moteDetail.Name);

                        MoteRecords[Y].Add(mote);
                    }
                }
            }
        }
    }
}
