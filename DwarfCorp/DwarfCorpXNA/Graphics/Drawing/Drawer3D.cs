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
        public int MaxTriangles = 2048;
        public int VertexCount = 0;
        private VertexPositionColor[] Verticies;
        private VertexPositionColor[] VertexScratch;
        private VertexBuffer Buffer; 
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
            Verticies = new VertexPositionColor[MaxTriangles * 3];
            VertexScratch = new VertexPositionColor[MaxTriangles * 3];
            VertexCount = 0;
        }

        private void Grow()
        {
            MaxTriangles *= 2;
            var newVerts = new VertexPositionColor[MaxTriangles * 3];
            Verticies.CopyTo(newVerts, 0);
            VertexScratch = new VertexPositionColor[MaxTriangles * 3];
            Verticies = newVerts;
            if (Buffer != null)
            {
                Buffer.Dispose();
            }

            Buffer = new VertexBuffer(Device, VertexPositionColor.VertexDeclaration, MaxTriangles * 3, BufferUsage.None);
            Buffer.SetData(Verticies);
        }

        private void RebuildSegments()
        {
            VertexCount = 0;
            Dictionary<uint, TriangleSegment> newSegments = new Dictionary<uint, TriangleSegment>();
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
            {
                Buffer = new VertexBuffer(Device, VertexPositionColor.VertexDeclaration, MaxTriangles * 3, BufferUsage.None);
            }
            Buffer.SetData(Verticies);
            return newSegment.ID;
        }

        // Adds a bounding box to the vertex cache, and returns the index of the segment it belongs to.
        public uint AddTopBox(BoundingBox box, Color color, float thickness, bool warp)
        {
            if (VertexCount + (int)Drawer3D.PrimitiveVertexCounts.TopBox >= MaxTriangles * 3)
            {
                Grow();
            }

            Drawer3D.WriteTopBox(box.Min, box.Max - box.Min, color, thickness, warp, VertexCount, Verticies);
            return AddSegment((int)Drawer3D.PrimitiveVertexCounts.TopBox);
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
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawPrimitives(PrimitiveType.TriangleList, 0, VertexCount / 3);
            }

            Effect.SetTexturedTechnique();

            if (oldState != null)
            {
                Device.RasterizerState = oldState;
            }

            if (origBlen != null)
            {
                Device.BlendState = origBlen;
            }
        }

        public void Dispose()
        {
            if (Buffer != null)
            {
                Buffer.Dispose();
            }
        }
    }

    /// <summary>
    /// This is a convenience class for drawing lines, boxes, etc. to the screen.
    /// </summary>
    public class Drawer3D
    {
        private const int MaxTriangles = 64;
        private static VertexPositionColor[] Verticies = new VertexPositionColor[MaxTriangles * 3];
        private static int VertexCount;
        private static GraphicsDevice Device {  get { return GameStates.GameState.Game.GraphicsDevice; } }
        private static Shader Effect;
        private static OrbitCamera Camera;
        private static object renderLock = new object();

        private struct Box
        {
            public BoundingBox RealBox;
            public float Thickness;
            public Color Color;
            public bool Warp;
        }

        private static List<Box> Boxes = new List<Box>();

        private struct Segment
        {
            public Vector3 A;
            public Vector3 B;
            public Color Color;
            public float Thickness;
        }

        private static List<Segment> Segments = new List<Segment>();

        public enum PrimitiveVertexCounts
        {
            Triangle = 3,
            LineSegment = 2 * Triangle,
            Box = 4 * 6 * LineSegment,
            TopBox = 4 * 1 * LineSegment
        }


        private static void _flush()
        {
            if (VertexCount == 0) return;

            //for (var i = 0; i < VertexCount; ++i)
            //    Verticies[i].Position += VertexNoise.GetNoiseVectorFromRepeatingTexture(Verticies[i].Position);

            BlendState origBlen = Device.BlendState;
            Device.BlendState = BlendState.NonPremultiplied;

            RasterizerState oldState = Device.RasterizerState;
            Device.RasterizerState = RasterizerState.CullNone;

            Effect.CurrentTechnique = Effect.Techniques[Shader.Technique.Untextured];
            Effect.View = Camera.ViewMatrix;
            Effect.Projection = Camera.ProjectionMatrix;
            Effect.World = Matrix.Identity;
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawUserPrimitives(PrimitiveType.TriangleList, Verticies, 0, VertexCount / 3);
            }
            
            Effect.SetTexturedTechnique();
            
            if (oldState != null)
            {
                Device.RasterizerState = oldState;
            }

            if (origBlen != null)
            {
                Device.BlendState = origBlen;
            }

            VertexCount = 0;
        }

        public static int WriteTriangle(Vector3 A, Vector3 B, Vector3 C, Color Color, int start, VertexPositionColor[] buffer)
        {
            buffer[start] = new VertexPositionColor
            {
                Position = A,
                Color = Color,
            };

            buffer[start + 1] = new VertexPositionColor
            {
                Position = B,
                Color = Color,
            };

            buffer[start + 2] = new VertexPositionColor
            {
                Position = C,
                Color = Color,
            };
            return start + 3;
        }

        private static void _addTriangle(Vector3 A, Vector3 B, Vector3 C, Color Color)
        {
            WriteTriangle(A, B, C, Color, VertexCount, Verticies);
            VertexCount += 3;
            if (VertexCount >= MaxTriangles * 3)
                _flush();
        }

        public static int WriteLineSegment(Vector3 A, Vector3 B, Color Color, float Thickness, bool Warp, int start, VertexPositionColor[] buffer)
        {
            if (Warp)
            {
                A += VertexNoise.GetNoiseVectorFromRepeatingTexture(A);
                B += VertexNoise.GetNoiseVectorFromRepeatingTexture(B);
            }

            var aRay = Vector3.Up;
            var bRay = A - B;
            if (Math.Abs(Vector3.Dot(aRay, bRay)) > 0.99)
            {
                aRay = Vector3.Right;
            }
            var perp = Vector3.Cross(aRay, bRay);
            perp.Normalize();
            perp *= Thickness / 2;

            start = WriteTriangle(A + perp, B + perp, A - perp, Color, start, buffer);
            return WriteTriangle(A - perp, B - perp, B + perp, Color, start, buffer);
        }

        private static void _addLineSegment(Vector3 A, Vector3 B, Color Color, float Thickness, bool Warp)
        {
            if (Warp)
            {
                A += VertexNoise.GetNoiseVectorFromRepeatingTexture(A);
                B += VertexNoise.GetNoiseVectorFromRepeatingTexture(B);
            }

            var aRay = A - Camera.Position;
            var bRay = A - B;
            var perp = Vector3.Cross(aRay, bRay);
            perp.Normalize();
            perp *= Thickness / 2;

            _addTriangle(A + perp, B + perp, A - perp, Color);
            _addTriangle(A - perp, B - perp, B + perp, Color);
        }

        private static void _addBox(Vector3 M, Vector3 S, Color C, float T, bool Warp)
        {
            float halfT = T * 0.5f;
            S += Vector3.One * T;
            M -= Vector3.One * halfT;
            // Draw bottom loop.
            _addLineSegment(new Vector3(M.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y, M.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X + S.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y, M.Z + S.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X + S.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y, M.Z + S.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y, M.Z), C, T, Warp);

            // Draw top loop.
            _addLineSegment(new Vector3(M.X, M.Y + S.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X + S.X, M.Y + S.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z + S.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X, M.Y + S.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z), C, T, Warp);

            // Draw uprights
            _addLineSegment(new Vector3(M.X, M.Y, M.Z), new Vector3(M.X, M.Y + S.Y, M.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X + S.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X + S.X, M.Y, M.Z + S.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z + S.Z), C, T, Warp);

        }

        public static int WriteBox(Vector3 M, Vector3 S, Color C, float T, bool Warp, int start, VertexPositionColor[] buffer)
        {
            float halfT = T * 0.5f;
            S += Vector3.One * halfT;
            M -= Vector3.One * halfT;
            // Draw bottom loop.
            start = WriteLineSegment(new Vector3(M.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y, M.Z), C, T, Warp, start, buffer);
            start = WriteLineSegment(new Vector3(M.X + S.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y, M.Z + S.Z), C, T, Warp, start, buffer);
            start = WriteLineSegment(new Vector3(M.X + S.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y, M.Z + S.Z), C, T, Warp, start, buffer);
            start = WriteLineSegment(new Vector3(M.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y, M.Z), C, T, Warp, start, buffer);

            // Draw top loop.
            start = WriteLineSegment(new Vector3(M.X, M.Y + S.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z), C, T, Warp, start, buffer);
            start = WriteLineSegment(new Vector3(M.X + S.X, M.Y + S.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z), C, T, Warp, start, buffer);
            start = WriteLineSegment(new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z + S.Z), C, T, Warp, start, buffer);
            start = WriteLineSegment(new Vector3(M.X, M.Y + S.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z), C, T, Warp, start, buffer);

            // Draw uprights
            start = WriteLineSegment(new Vector3(M.X, M.Y, M.Z), new Vector3(M.X, M.Y + S.Y, M.Z), C, T, Warp, start, buffer);
            start = WriteLineSegment(new Vector3(M.X + S.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z), C, T, Warp, start, buffer);
            start = WriteLineSegment(new Vector3(M.X + S.X, M.Y, M.Z + S.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z), C, T, Warp, start, buffer);
            start = WriteLineSegment(new Vector3(M.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z + S.Z), C, T, Warp, start, buffer);
            return start;
        }

        public static int WriteTopBox(Vector3 M, Vector3 S, Color color, float T, bool Warp, int start, VertexPositionColor[] buffer)
        {
            float halfT = T * 0.5f;

            float heightOffset = 0.05f;
            Vector3 A = M + new Vector3(0, S.Y, 0);
            Vector3 B = M + new Vector3(S.X, S.Y, 0);
            Vector3 C = M + new Vector3(S.X, S.Y, S.Z);
            Vector3 D = M + new Vector3(0, S.Y, S.Z);
            if (Warp)
            {
                A += VertexNoise.GetNoiseVectorFromRepeatingTexture(A);
                B += VertexNoise.GetNoiseVectorFromRepeatingTexture(B);
                C += VertexNoise.GetNoiseVectorFromRepeatingTexture(C);
                D += VertexNoise.GetNoiseVectorFromRepeatingTexture(D);
            }
            // Draw top loop.
            // A   1      B
            //   *+---+*
            // 4 |     | 2
            //   *+---+*
            // D   3      C
            start = WriteLineSegment(A + new Vector3(0, heightOffset, halfT), B + new Vector3(0, heightOffset, halfT), color, T, false, start, buffer);
            start = WriteLineSegment(B + new Vector3(-halfT, heightOffset, halfT), C + new Vector3(-halfT, heightOffset, -halfT), color, T, false, start, buffer);
            start = WriteLineSegment(C + new Vector3(0, heightOffset, -halfT),  D + new Vector3(halfT, heightOffset, -halfT), color, T, false, start, buffer);
            start = WriteLineSegment(D + new Vector3(halfT, heightOffset, 0), A + new Vector3(halfT, heightOffset, halfT), color, T, false, start, buffer);
            return start;
        }

        public static void Render(
            GraphicsDevice Device, 
            Shader Effect, 
            OrbitCamera Camera,
            DesignationDrawer DesignationDrawer,
            DesignationSet Designations,
            WorldManager World)
        {
            lock (renderLock)
            {
                Drawer3D.Effect = Effect;
                Drawer3D.Camera = Camera;

                var colorModulation = Math.Abs(Math.Sin(DwarfTime.LastTime.TotalGameTime.TotalSeconds*2.0f));

                DesignationDrawer.DrawHilites(
                    Designations,
                    _addBox,
                    (pos, type) =>
                    {
                        Effect.MainTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_tiles);
                        Effect.LightRampTint = Color.White;
                        // Todo: Alpha pulse
                        Effect.VertexColorTint = new Color(0.1f, 0.9f, 1.0f, 1.0f);
                        Effect.CurrentTechnique = Effect.Techniques[Shader.Technique.Stipple];
                        var pos_distorted = pos + Vector3.Up * 0.15f + VertexNoise.GetNoiseVectorFromRepeatingTexture(pos + Vector3.One * 0.5f);
                        Effect.World = Matrix.CreateTranslation(pos_distorted);

                        foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            VoxelLibrary.GetPrimitive(type).Render(Device);
                        }

                        Effect.LightRampTint = Color.White;
                        Effect.VertexColorTint = Color.White;
                        Effect.World = Matrix.Identity;
                    });
                DesignationSet.TriangleCache.Draw(Effect, Camera);
                foreach (var box in Boxes)
                    _addBox(box.RealBox.Min, box.RealBox.Max - box.RealBox.Min, box.Color, box.Thickness, box.Warp);

                foreach (var segment in Segments)
                    _addLineSegment(segment.A, segment.B, segment.Color, segment.Thickness, false);

                _flush();

                Boxes.Clear();
                Segments.Clear();
            }
        }

        public static void DrawBox(BoundingBox box, Color color, float thickness, bool warp)
        {
            if (!DwarfGame.HasRendered) return;
            lock (renderLock)
            {
                Boxes.Add(new Box
                {
                    RealBox = box,
                    Color = color,
                    Thickness = thickness,
                    Warp = warp
                });
            }
        }

        public static void DrawLine(Vector3 A, Vector3 B, Color Color, float Thickness)
        {
            if (!DwarfGame.HasRendered) return;
            lock (renderLock)
            {
                Segments.Add(new Segment
                {
                    A = A,
                    B = B,
                    Color = Color,
                    Thickness = Thickness
                });
            }
        }

        public static void DrawLineList(List<Vector3> points, List<Color> colors, float v)
        {
            if (!DwarfGame.HasRendered) return;
            lock (renderLock)
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Segments.Add(new Segment { A = points[i], B = points[i + 1], Color = colors[i], Thickness = v});
                }
            }

        }
    }
}
