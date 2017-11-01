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
            S += Vector3.One * T;
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
                Drawer3D.Device = Device;
                Drawer3D.Effect = Effect;
                Drawer3D.Camera = Camera;

                var colorModulation = Math.Abs(Math.Sin(DwarfTime.LastTime.TotalGameTime.TotalSeconds*2.0f));

                DesignationDrawer.DrawHilites(
                    Designations,
                    _addBox,
                    (pos, type) =>
                    {
                        var saveState = Device.DepthStencilState;
                        Device.DepthStencilState = DepthStencilState.DepthRead;

                        Effect.MainTexture = World.ChunkManager.ChunkData.Tilemap;
                        Effect.LightRampTint = Color.White;
                        // Todo: Alpha pulse
                        Effect.VertexColorTint = new Color(0.1f, 0.9f, 1.0f, 0.5f + 0.45f);
                        Effect.SetTexturedTechnique();
                        Effect.World = Matrix.CreateTranslation(pos + Vector3.Up * 0.15f); // Why the offset?

                        foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            VoxelLibrary.GetPrimitive(type).Render(Device);
                        }

                        Effect.LightRampTint = Color.White;
                        Effect.VertexColorTint = Color.White;
                        Effect.World = Matrix.Identity;
                        Device.DepthStencilState = saveState;
                    });

                foreach (var box in Boxes)
                    _addBox(box.RealBox.Min, box.RealBox.Max - box.RealBox.Min, box.Color, box.Thickness);

                foreach (var segment in Segments)
                    _addLineSegment(segment.A, segment.B, segment.Color, segment.Thickness);

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
                    Thickness = thickness
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
