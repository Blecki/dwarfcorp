// Drawer3D.cs
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
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    // This is a cached alternative to Drawer3D for drawings that are expects to remain around for a long time.
    public class TriangleCache : IDisposable
    {
        public const int MaxVertexSpace = 4096 * 16;
        public int MaxTriangles = 2048;
        public int VertexCount = 0;
        private ThickLineVertex[] Verticies;
        private ThickLineVertex[] VertexScratch;
        private DynamicVertexBuffer Buffer; 
        private static GraphicsDevice Device { get { return GameStates.GameState.Game.GraphicsDevice; } }
        private uint MaxSegment = 1;

        private struct TriangleSegment
        {
            public uint ID;
            public int StartIndex;
            public int EndIndex;
        }

        private Dictionary<uint, TriangleSegment> Segments = new Dictionary<uint, TriangleSegment>();

        public TriangleCache()
        {
            Verticies = new ThickLineVertex[MaxTriangles * 3];
            VertexScratch = new ThickLineVertex[MaxTriangles * 3];
            VertexCount = 0;
        }

        private void Grow()
        {
            MaxTriangles *= 2;
            var newVerts = new ThickLineVertex[MaxTriangles * 3];
            Verticies.CopyTo(newVerts, 0);
            VertexScratch = new ThickLineVertex[MaxTriangles * 3];
            Verticies = newVerts;
            if (Buffer != null)
            {
                Buffer.Dispose();
            }

            Buffer = new DynamicVertexBuffer(Device, ThickLineVertex.VertexDeclaration, MaxTriangles * 3, BufferUsage.None);
            Buffer.SetData(Verticies);
        }

        private void RebuildSegments()
        {
            VertexCount = 0;
            var newSegments = new Dictionary<uint, TriangleSegment>();
            foreach(var segment in Segments)
            {
                TriangleSegment newSegment = new TriangleSegment
                {
                    ID = segment.Key,
                    StartIndex = VertexCount
                };

                for (int i = segment.Value.StartIndex; i < segment.Value.EndIndex; i++)
                {
                    VertexScratch[VertexCount] = Verticies[i];
                    VertexCount++;
                }
                newSegment.EndIndex = VertexCount;
                newSegments[newSegment.ID] = newSegment;
            }
            Datastructures.Swap(ref Segments, ref newSegments);
            Datastructures.Swap(ref Verticies, ref VertexScratch);
            Buffer.SetData(Verticies);
        }

        public void EraseSegment(uint segment)
        {
            Segments.Remove(segment);
            RebuildSegments();
        }

        public void EraseSegments(IEnumerable<uint> segments)
        {
            bool removed = false;
            foreach (var seg in segments)
            {
                removed = true;
                Segments.Remove(seg);
            }

            if (removed)
                RebuildSegments();
        }

        private uint AddSegment(int count)
        {
            if (VertexCount >= MaxVertexSpace)
            {
                return 0;
            }

            TriangleSegment newSegment = new TriangleSegment
            {
                ID = MaxSegment,
                StartIndex = VertexCount,
                EndIndex = VertexCount + count
            };

            MaxSegment++;
            VertexCount += count;
            Segments.Add(newSegment.ID, newSegment);

            if (Buffer == null)
                Buffer = new DynamicVertexBuffer(Device, ThickLineVertex.VertexDeclaration, MaxTriangles * 3, BufferUsage.None);

            Buffer.SetData(Verticies);
            return newSegment.ID;
        }

        // Adds a bounding box to the vertex cache, and returns the index of the segment it belongs to.
        public uint AddTopBox(BoundingBox box, Color color, float thickness, bool warp)
        {
            if (VertexCount >= MaxVertexSpace)
            {
                return 0;
            }

            int numVertex = VertexCount + (int)Drawer3D.PrimitiveVertexCounts.TopBox;
            if (numVertex >= MaxTriangles * 3)
            {
                Grow();
            }

            Drawer3D.WriteTopBox(box.Min, box.Max - box.Min, color, thickness, warp, VertexCount, Verticies);
            return AddSegment((int)Drawer3D.PrimitiveVertexCounts.TopBox);
        }

        public uint AddBox(BoundingBox box, Color color, float thickness, bool warp)
        {
            if (VertexCount >= MaxVertexSpace)
                return 0;

            if (VertexCount + (int)Drawer3D.PrimitiveVertexCounts.Box >= MaxTriangles * 3)
                Grow();

            Drawer3D.WriteBox(box.Min, box.Max - box.Min, color, thickness, warp, VertexCount, Verticies);
            return AddSegment((int)Drawer3D.PrimitiveVertexCounts.Box);
        }

        public void Draw(Shader Effect, Camera Camera)
        {
            if (VertexCount == 0)
                return;

            BlendState origBlen = Device.BlendState;
            Device.BlendState = BlendState.NonPremultiplied;

            RasterizerState oldState = Device.RasterizerState;
            Device.RasterizerState = RasterizerState.CullNone;
            Effect.CurrentTechnique = Effect.Techniques[Shader.Technique.Untextured_Pulse];
            Effect.View = Camera.ViewMatrix;
            Effect.Projection = Camera.ProjectionMatrix;
            Effect.World = Matrix.Identity;
            Device.Indices = null;
            Device.SetVertexBuffer(Buffer);
            Effect.VertexColorTint = Color.White;

            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawPrimitives(PrimitiveType.TriangleList, 0, VertexCount / 3);
            }

            Effect.SetTexturedTechnique();

            if (oldState != null)
                Device.RasterizerState = oldState;

            if (origBlen != null)
                Device.BlendState = origBlen;
        }

        public void Dispose()
        {
            if (Buffer != null)
            {
                Buffer.Dispose();
            }
        }
    }
}
