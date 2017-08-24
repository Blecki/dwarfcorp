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
    /// <summary>
    /// This is a convenience class for drawing lines, boxes, etc. to the screen.
    /// </summary>
    public class Drawer3D
    {
        private const int MaxTriangles = 64;
        private static VertexPositionColor[] Verticies = new VertexPositionColor[MaxTriangles * 3];
        private static int VertexCount;
        private static GraphicsDevice Device;
        private static Shader Effect;
        private static OrbitCamera Camera;
        private static Dictionary<Color, List<GlobalVoxelCoordinate>> HighlightGroups = new Dictionary<Color, List<GlobalVoxelCoordinate>>();
        private static object renderLock = new object();
        private struct Box
        {
            public BoundingBox RealBox;
            public float Thickness;
            public Color Color;
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

        private static void _flush()
        {
            if (VertexCount == 0) return;

            for (var i = 0; i < VertexCount; ++i)
                Verticies[i].Position += VertexNoise.GetNoiseVectorFromRepeatingTexture(Verticies[i].Position);

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

        private static void _addTriangle(Vector3 A, Vector3 B, Vector3 C, Color Color)
        {
            Verticies[VertexCount] = new VertexPositionColor
            {
                Position = A,
                Color = Color,
            };

            Verticies[VertexCount + 1] = new VertexPositionColor
            {
                Position = B,
                Color = Color,
            };

            Verticies[VertexCount + 2] = new VertexPositionColor
            {
                Position = C,
                Color = Color,
            };

            VertexCount += 3;
            if (VertexCount >= MaxTriangles * 3)
                _flush();
        }

        private static void _addLineSegment(Vector3 A, Vector3 B, Color Color, float Thickness)
        {
            var aRay = A - Camera.Position;
            var bRay = A - B;
            var perp = Vector3.Cross(aRay, bRay);
            perp.Normalize();
            perp *= Thickness / 2;

            _addTriangle(A + perp, B + perp, A - perp, Color);
            _addTriangle(A - perp, B - perp, B + perp, Color);
        }

        private static void _addBox(Vector3 M, Vector3 S, Color C, float T)
        {
            float halfT = T * 0.5f;
            S += Vector3.One * halfT;
            M -= Vector3.One * halfT;
            // Draw bottom loop.
            _addLineSegment(new Vector3(M.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y, M.Z), C, T);
            _addLineSegment(new Vector3(M.X + S.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y, M.Z + S.Z), C, T);
            _addLineSegment(new Vector3(M.X + S.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y, M.Z + S.Z), C, T);
            _addLineSegment(new Vector3(M.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y, M.Z), C, T);

            // Draw top loop.
            _addLineSegment(new Vector3(M.X, M.Y + S.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z), C, T);
            _addLineSegment(new Vector3(M.X + S.X, M.Y + S.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z), C, T);
            _addLineSegment(new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z + S.Z), C, T);
            _addLineSegment(new Vector3(M.X, M.Y + S.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z), C, T);

            // Draw uprights
            _addLineSegment(new Vector3(M.X, M.Y, M.Z), new Vector3(M.X, M.Y + S.Y, M.Z), C, T);
            _addLineSegment(new Vector3(M.X + S.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z), C, T);
            _addLineSegment(new Vector3(M.X + S.X, M.Y, M.Z + S.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z), C, T);
            _addLineSegment(new Vector3(M.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z + S.Z), C, T);

        }

        public static void Render(GraphicsDevice Device, Shader Effect, OrbitCamera Camera)
        {
            lock (renderLock)
            {
                Drawer3D.Device = Device;
                Drawer3D.Effect = Effect;
                Drawer3D.Camera = Camera;

                var colorModulation = Math.Abs(Math.Sin(DwarfTime.LastTime.TotalGameTime.TotalSeconds*2.0f));

                foreach (var hilitedVoxelGroup in HighlightGroups)
                {
                    var groupColor = new Color(
                        (byte) (hilitedVoxelGroup.Key.R*colorModulation + 50),
                        (byte) (hilitedVoxelGroup.Key.G*colorModulation + 50),
                        (byte) (hilitedVoxelGroup.Key.B*colorModulation + 50),
                        255);

                    foreach (var hilitedVoxel in hilitedVoxelGroup.Value)
                        _addBox(hilitedVoxel.ToVector3(), Vector3.One, groupColor, 0.1f);
                }

                foreach (var box in Boxes)
                    _addBox(box.RealBox.Min, box.RealBox.Max - box.RealBox.Min, box.Color, box.Thickness);

                foreach (var segment in Segments)
                    _addLineSegment(segment.A, segment.B, segment.Color, segment.Thickness);

                _flush();

                Boxes.Clear();
                Segments.Clear();
            }
        }

        public static void UnHighlightVoxel(VoxelHandle voxel)
        {
            lock (renderLock)
            {
                foreach (var group in HighlightGroups)
                    group.Value.Remove(voxel.Coordinate);
            }
        }

        public static void HighlightVoxel(VoxelHandle voxel, Color color)
        {
            lock (renderLock)
            {
                if (!HighlightGroups.ContainsKey(color))
                    HighlightGroups.Add(color, new List<GlobalVoxelCoordinate>());
            }
            UnHighlightVoxel(voxel);

            lock (renderLock)
            {
                HighlightGroups[color].Add(voxel.Coordinate);
            }

        }
        
        public static void DrawBox(BoundingBox box, Color color, float thickness, bool warp)
        {
            lock (renderLock)
            {
                Boxes.Add(new Box
                {
                    RealBox = box,
                    Color = color,
                    Thickness = thickness
                });
            }
        }

        public static void DrawLine(Vector3 A, Vector3 B, Color Color, float Thickness)
        {
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
    }
}
