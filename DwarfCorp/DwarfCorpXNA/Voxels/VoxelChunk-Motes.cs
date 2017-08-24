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
using System.Threading;
using System.Collections.Concurrent;
using Color = Microsoft.Xna.Framework.Color;
using System.Diagnostics;
using System.Linq;

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

        private static void DestroyGrassMote(String Name, InstanceData Data)
        {
            EntityFactory.InstanceManager.RemoveInstance(Name, Data);
            // Todo: Should this be automatically set whenever an instance is added or removed?
            EntityFactory.InstanceManager.Instances[Name].HasSelectionBuffer = false;
        }

        private static InstanceData GenerateGrassMote(Vector3 Position, Color Color, float Scale, String Name)
        {
            var mote = new InstanceData(
                Matrix.CreateScale(Scale) * Matrix.CreateRotationY(Scale * Scale)
                * Matrix.CreateTranslation(Position), Color, true);
            EntityFactory.InstanceManager.AddInstance(Name, mote);
            return mote;
        }

        private class MoteRecord
        {
            public String Name;
            public InstanceData InstanceData;
        }

        private List<MoteRecord>[] MoteRecords = new List<MoteRecord>[VoxelConstants.ChunkSizeY];

        public void RebuildMoteLayerIfNull(int Y)
        {
            if (MoteRecords[Y] == null)
                RebuildMoteLayer(Y);
        }

        public void RebuildMoteLayer(int Y)
        {
            BoundingBox box = GetBoundingBox();
            box.Min.Y = Y;
            box.Max.Y = Y + 1;
            //Drawer3D.DrawBox(box, Color.Red, 0.1f, true);
            // Destroy old motes.
            if (MoteRecords[Y] != null)
            {
                foreach (var record in MoteRecords[Y])
                    DestroyGrassMote(record.Name, record.InstanceData);
                MoteRecords[Y].Clear();
            }
            else
            {
                MoteRecords[Y] = new List<MoteRecord>();
            }

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

                    // Find biome type.
                    var biome = Overworld.Map[
                        (int)MathFunctions.Clamp(v.WorldPosition.X / Manager.World.WorldScale, 0, Overworld.Map.GetLength(0) - 1),
                        (int)MathFunctions.Clamp(v.WorldPosition.Z / Manager.World.WorldScale, 0, Overworld.Map.GetLength(1) - 1)]
                        .Biome;

                    var biomeData = BiomeLibrary.Biomes[biome];

                    // Don't generate if not on grass type.
                    if (v.Type.Name != biomeData.GrassLayer.VoxelType)
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
                            v.WorldPosition + new Vector3(0.5f, 1.0f + s * 0.5f + vOffset, 0.5f) + smallNoise,
                            new Color(v.SunColor, 128, 0),
                            s,
                            moteDetail.Name);
                        //Drawer3D.DrawBox(v.GetBoundingBox(), Color.Red, 0.1f, true);
                        MoteRecords[Y].Add(new MoteRecord
                        {
                            Name = moteDetail.Name,
                            InstanceData = mote
                        });
                    }
                }
            }
        }
    }
}
