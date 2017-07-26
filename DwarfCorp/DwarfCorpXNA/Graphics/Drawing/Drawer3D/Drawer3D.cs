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
    /// This is a convenience class for drawing lines, boxes, etc. to the screen from
    /// threads other than the main drawing thread. 
    /// </summary>
    public class Drawer3D
    {
        private static readonly ConcurrentBag<DrawCommand3D> Commands = new ConcurrentBag<DrawCommand3D>();
        public static OrbitCamera Camera = null;
        private static DynamicVertexBuffer StripBuffer;
        private static VertexPositionColor[] StripVertices = null;
        private static int MaxStripVertex = -1;
        private static VoxelHighlighter Highlighter = new VoxelHighlighter();

        public static void UnHighlightVoxel(TemporaryVoxelHandle voxel)
        {
            Highlighter.Remove(voxel);
        }

        public static void HighlightVoxel(TemporaryVoxelHandle voxel, Color color)
        {
            Highlighter.Highlight(voxel, color);   
        }

        public static void DrawLine(Vector3 p1, Vector3 p2, Color color, float thickness)
        {
            DrawLineList(new List<Vector3>(){p1, p2}, color, thickness);
        }

        public static void DrawLineList(List<Vector3> points, Color color, float thickness)
        {
            Commands.Add(new LineListCommand3D(points.ToArray(), color, thickness));
        }


        public static void DrawLineList(List<Vector3> points, List<Color> color, float thickness)
        {
            Commands.Add(new LineListCommand3D(points.ToArray(), color, thickness));
        }

        public static void DrawBoxList(List<BoundingBox> boxes, Color color, float thickness)
        {
            foreach(BoundingBox box in boxes)
            {
                Commands.Add(new BoxDrawCommand3D(box, color, thickness, false));
            }
        }

        public static void DrawBox(BoundingBox box, Color color, float thickness)
        {
            Commands.Add(new BoxDrawCommand3D(box, color, thickness, false));
        }

        public static void DrawBox(BoundingBox box, Color color, float thickness, bool warp)
        {
            Commands.Add(new BoxDrawCommand3D(box, color, thickness, warp));
        }

        public static void DrawPlane(float y, float minX, float minZ, float maxX, float maxZ, Color color)
        {
            Commands.Add(new PlaneDrawCommand(new Vector3((maxX + minX) * 0.5f, y, (maxZ + minZ) * 0.5f), new Vector3((maxX - minX), 1.0f, (maxZ - minZ)), color));
        }

        public static void Render(GraphicsDevice device, Shader effect, bool delete)
        {
            BlendState origBlen = device.BlendState;
            device.BlendState = BlendState.NonPremultiplied;

            RasterizerState newState = RasterizerState.CullNone;
            RasterizerState oldState = device.RasterizerState;
            device.RasterizerState = newState;

            effect.CurrentTechnique = effect.Techniques[Shader.Technique.Untextured];
            effect.World = Matrix.Identity;

            Highlighter.Render(device, effect);

            DrawCommand3D.LineStrip strips = new DrawCommand3D.LineStrip()
            {
                Vertices = new List<VertexPositionColor>()
            };
            foreach(DrawCommand3D command in Commands)
            {
                if (command.DrawAccumlatedStrips)
                    command.AccumulateStrips(strips);
            }

            if (strips.Vertices.Count > 0 &&
                (StripVertices == null ||
                strips.Vertices.Count > StripVertices.Count()))
            {
                StripVertices = new VertexPositionColor[strips.Vertices.Count * 2];
                StripBuffer = new DynamicVertexBuffer(device, VertexPositionColor.VertexDeclaration, strips.Vertices.Count * 2, BufferUsage.WriteOnly);    
            }

            if (strips.Vertices.Count > 0)
            {
                strips.Vertices.CopyTo(StripVertices);
                MaxStripVertex = strips.Vertices.Count;

                if (MaxStripVertex > 0 && StripBuffer != null)
                {
                    StripBuffer.SetData(StripVertices, 0, MaxStripVertex);
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        device.SetVertexBuffer(StripBuffer);
                        device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, strips.Vertices.Count - 2);
                    }
                }
            }

            effect.CurrentTechnique = effect.Techniques[Shader.Technique.Textured];

            foreach (DrawCommand3D command in Commands)
            {
                if (!command.DrawAccumlatedStrips)
                {
                    command.Render(device, effect);
                }
            }


            if(oldState != null)
            {
                device.RasterizerState = oldState;
            }

            if(origBlen != null)
            {
                device.BlendState = origBlen;
            }


            if(!delete)
            {
                return;
            }

            
            while(Commands.Count > 0)
            {
                DrawCommand3D result = null;
                Commands.TryTake(out result);
            }
             
        }

        public static List<VertexPositionColor> GetTriangleStrip(Vector3[] points, float thickness, Color color, ref int triangleCount, Matrix worldMatrix)
        {
            Vector3 lastPoint = Vector3.Zero;
            List<VertexPositionColor> list = new List<VertexPositionColor>();


            for(int i = 0; i < points.Length; i++)
            {
                if(i == 0)
                {
                    lastPoint = points[i];
                    continue;
                }
                Vector3 t1 = Vector3.Transform(lastPoint, worldMatrix);
                Vector3 t2 = Vector3.Transform(points[i], worldMatrix);
                Vector3 direction = t1 - t2;
                Vector3 normal = Vector3.Cross(direction,
                    MathFunctions.GetClosestPointOnLineSegment(t1, t2, Camera.Position) - Camera.Position);
                direction.Normalize();
                normal.Normalize();


                Vector3 p1 = t1 + normal * thickness;
                Vector3 p2 = t1 - normal * thickness;
                Vector3 p3 = t2 + normal * thickness;
                Vector3 p4 = t2 - normal * thickness;

                list.Add(new VertexPositionColor(p1, color));
                list.Add(new VertexPositionColor(p2, color));
                list.Add(new VertexPositionColor(p3, color));
                list.Add(new VertexPositionColor(p4, color));

                triangleCount += 2;

                lastPoint = points[i];
            }

            return list;
        }

        public static List<VertexPositionColor> GetTriangleStrip(Vector3[] points, float thickness, Color[] color, ref int triangleCount, Matrix worldMatrix)
        {
            Vector3 lastPoint = Vector3.Zero;
            List<VertexPositionColor> list = new List<VertexPositionColor>();

            if (color == null)
            {
                color = new Color[points.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    color[i] = Color.White;
                }
            }

            for (int i = 0; i < points.Length; i++)
            {
                if (i == 0)
                {
                    lastPoint = points[i];
                    continue;
                }
                Vector3 t1 = Vector3.Transform(lastPoint, worldMatrix);
                Vector3 t2 = Vector3.Transform(points[i], worldMatrix);
                Vector3 direction = t1 - t2;
                Vector3 normal = Vector3.Cross(direction,
                    MathFunctions.GetClosestPointOnLineSegment(t1, t2, Camera.Position) - Camera.Position);
                direction.Normalize();
                normal.Normalize();


                Vector3 p1 = t1 + normal * thickness;
                Vector3 p2 = t1 - normal * thickness;
                Vector3 p3 = t2 + normal * thickness;
                Vector3 p4 = t2 - normal * thickness;

                list.Add(new VertexPositionColor(p1, color[i - 1]));
                list.Add(new VertexPositionColor(p2, color[i - 1]));
                list.Add(new VertexPositionColor(p3, color[i - 1]));
                list.Add(new VertexPositionColor(p4, color[i - 1]));

                triangleCount += 2;

                lastPoint = points[i];
            }

            return list;
        }


        public static void DrawAxes(Matrix t, float length)
        {
            Vector3 p = t.Translation;
            DrawLine(p, p + t.Right * length, Color.Red, 0.01f);
            DrawLine(p, p + t.Up * length, Color.Green, 0.01f);
            DrawLine(p, p + t.Forward * length, Color.Blue, 0.01f);
        }
    }

    public class VoxelHighlighter
    {
        public class VoxelHighlightGroup
        {
            public Color Color;
            private float Thickness;
            private List<TemporaryVoxelHandle> Voxels;
            private bool Valid;
            private VertexBuffer VertBuffer;
            private DrawCommand3D.LineStrip Strip;

            public VoxelHighlightGroup()
            {
                Color = Color.White;
                Thickness = 0.05f;
                Voxels = new List<TemporaryVoxelHandle>();
                Valid = false;
            }

            public void AddVoxel(TemporaryVoxelHandle voxel)
            {
                // Todo: Switch to hashset for unique list.
                if (Voxels.Any(v => v == voxel))
                {
                    return;
                }

                Voxels.Add(voxel);
                Valid = false;
            }
             
            public void RemoveVoxel(TemporaryVoxelHandle voxel)
            {
                int before = Voxels.Count;
                Voxels.RemoveAll(v => v == voxel);

                if (Voxels.Count != before)
                    Valid = false;
            }

            public void Rebuild(GraphicsDevice device)
            {
                if (Strip == null)
                {
                    Strip = new DrawCommand3D.LineStrip {Vertices = new List<VertexPositionColor>(), NumTriangles = 0};
                }
                Strip.Vertices.Clear();
                Strip.NumTriangles = 0;
                foreach (var vox in Voxels)
                {
                    BoxDrawCommand3D boxDraw = new BoxDrawCommand3D(vox.GetBoundingBox(), Color.White, Thickness, true);
                    boxDraw.AccumulateStrips(Strip);
                }
                
                VertBuffer = new VertexBuffer(device, VertexPositionColor.VertexDeclaration, Strip.Vertices.Count, BufferUsage.WriteOnly);
                VertBuffer.SetData(Strip.Vertices.ToArray());
                Valid = true;
            }

            public void Render(GraphicsDevice device, Shader shader)
            {
                if (Voxels == null || Voxels.Count == 0)
                {
                    return;
                }

                if (!Valid)
                {
                    Rebuild(device);    
                }

                shader.CurrentTechnique = shader.Techniques[Shader.Technique.Untextured];
                Color drawColor = Color;
                drawColor.R = (byte)(drawColor.R * Math.Abs(Math.Sin(DwarfTime.LastTime.TotalGameTime.TotalSeconds * 2.0f)) + 50);
                drawColor.G = (byte)(drawColor.G * Math.Abs(Math.Sin(DwarfTime.LastTime.TotalGameTime.TotalSeconds * 2.0f)) + 50);
                drawColor.B = (byte)(drawColor.B * Math.Abs(Math.Sin(DwarfTime.LastTime.TotalGameTime.TotalSeconds * 2.0f)) + 50);
                shader.VertexColorTint = drawColor;
                shader.LightRampTint = drawColor;
                device.SetVertexBuffer(VertBuffer);
                shader.World = Matrix.Identity;
                
                foreach (var pass in shader.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, Strip.Vertices.Count - 2);
                }
                shader.LightRampTint = Color.White;
                shader.VertexColorTint = Color.White;
            }
        }

        public Dictionary<Color, VoxelHighlightGroup> HighlightGroups { get; set; }


        public VoxelHighlighter()
        {
            HighlightGroups = new Dictionary<Color, VoxelHighlightGroup>();
        }

        public void Remove(TemporaryVoxelHandle voxel)
        {
            foreach (var pair in HighlightGroups)
            {
                pair.Value.RemoveVoxel(voxel);
            }
        }

        public void Highlight(TemporaryVoxelHandle voxel, Color color)
        {
            foreach (var pair in HighlightGroups.Where(v => v.Key != color))
            {
                pair.Value.RemoveVoxel(voxel);
            }

            VoxelHighlightGroup group;
            if (!HighlightGroups.TryGetValue(color, out group))
            {
                group = new VoxelHighlightGroup()
                {
                    Color = color
                };
                HighlightGroups[color] = group;
            }
            group.AddVoxel(voxel);
        }

        public void Render(GraphicsDevice device, Shader shader)
        {
            foreach (var pair in HighlightGroups)
            {
                pair.Value.Render(device, shader);
            }
        }
    }

}
