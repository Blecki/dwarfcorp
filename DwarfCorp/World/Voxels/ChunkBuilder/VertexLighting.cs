using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp.Voxels
{
    public static class VertexLighting
    {
        public struct VertexColorInfo
        {
            public int SunColor;
            public int AmbientColor;
            public int DynamicColor;

            public Color AsColor()
            {
                return new Color(SunColor, AmbientColor, DynamicColor);
            }
        }

        private static float GetAmbienceBoost(VoxelVertex vertex)
        {
            switch (vertex)
            {
                case VoxelVertex.FrontTopLeft:
                case VoxelVertex.FrontTopRight:
                case VoxelVertex.BackTopLeft:
                case VoxelVertex.BackTopRight:
                    return 0.25f;
                case VoxelVertex.BackBottomRight:
                case VoxelVertex.FrontBottomRight:
                    return 0.15f;
                default:
                    return 0.0f;
            }
        }

        public static VertexColorInfo CalculateVertexLight(VoxelHandle Vox, VoxelVertex Vertex, ChunkManager chunks)
        {
            var neighborsEmpty = 0;
            var neighborsChecked = 0;

            var color = new VertexColorInfo();
            color.DynamicColor = 0;
            color.SunColor = 0;

            foreach (var c in VoxelHelpers.EnumerateVertexNeighbors(Vox.Coordinate, Vertex))
            {
                var v = chunks.CreateVoxelHandle(c);
                if (!v.IsValid) continue;

                color.SunColor += v.Sunlight ? 255 : 0;

                if (!v.IsEmpty || !v.IsExplored)
                {
                    if (v.Type.EmitsLight)
                        color.DynamicColor = 255;

                    neighborsEmpty += 1;
                    neighborsChecked += 1;
                }
                else
                    neighborsChecked += 1;
            }

            var boost = GetAmbienceBoost(Vertex);
            var proportionHit = (float)neighborsEmpty / (float)neighborsChecked;
            color.AmbientColor = (int)Math.Min((1.0f - proportionHit) * 255.0f, 255);
            color.SunColor = (int)Math.Min((float)color.SunColor / (float)neighborsChecked + boost * 255.0f, 255);

            return color;
        }

        public static VertexColorInfo CalculateVertexLight(VoxelHandle Vox, VoxelVertex Vertex, ChunkManager Chunks, SliceCache Cache)
        {
            var r = new VertexColorInfo();

            var cacheKey = SliceCache.GetCacheKey(Vox, Vertex);

            if (!Cache.LightCache.TryGetValue(cacheKey, out r))
            {
                r = CalculateVertexLight(Vox, Vertex, Chunks);
                Cache.LightCache.Add(cacheKey, r);
            }

            return r;
        }
    }
}