﻿using System;
using System.Collections.Generic;
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

        public static void DrawLine(Vector3 p1, Vector3 p2, Color color, float thickness)
        {
            DrawLineList(new List<Vector3>(){p1, p2}, color, thickness);
        }

        public static void DrawLineList(List<Vector3> points, Color color, float thickness)
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

        public static void Render(GraphicsDevice device, Effect effect, bool delete)
        {

            BlendState origBlen = device.BlendState;
            device.BlendState = BlendState.AlphaBlend;

            RasterizerState newState = RasterizerState.CullNone;
            RasterizerState oldState = device.RasterizerState;
            device.RasterizerState = newState;

            effect.CurrentTechnique = effect.Techniques["Untextured"];
            effect.Parameters["xWorld"].SetValue(Matrix.Identity);

            DrawCommand3D.LineStrip strips = new DrawCommand3D.LineStrip()
            {
                Vertices = new List<VertexPositionColor>()
            };
            foreach(DrawCommand3D command in Commands)
            {
                command.AccumulateStrips(strips);

            }

            if (strips.Vertices.Count > 0 && strips.NumTriangles > 0)
            {
                foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawUserPrimitives(PrimitiveType.TriangleStrip, strips.Vertices.ToArray(), 0, strips.Vertices.Count - 2);
                }
            }


            effect.CurrentTechnique = effect.Techniques["Textured"];

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
    }

}