using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    class DrawCommand
    {
        public Color m_colorToDraw = Color.White;

        public DrawCommand(Color color)
        {
            m_colorToDraw = color;
        }

        public virtual void Render(GraphicsDevice device, Effect effect)
        {

        }
    }

    class LineListCommand : DrawCommand
    {
        public Vector3[] m_points;
        private VertexPositionColor[] m_triangles;
        private int m_triangleCount = 0;

        public LineListCommand(Vector3[] points, Color color, float thickness) :
            base(color)
        {
            m_points = points;
            Matrix worldMatrix = Matrix.Identity;
            List<VertexPositionColor> vertices = SimpleDrawing.GetTriangleStrip(points, thickness, color, ref m_triangleCount, worldMatrix);
            m_triangles = new VertexPositionColor[vertices.Count];
            vertices.CopyTo(m_triangles);
        }

        public override void Render(GraphicsDevice device, Effect effect)
        {
            if (m_triangleCount <= 0 || m_points.Count() < 2) return;
            Matrix w = Matrix.Identity;
            effect.Parameters["xWorld"].SetValue(w);
            effect.CurrentTechnique = effect.Techniques["Untextured"];
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                device.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, m_triangles, 0, m_triangleCount - 2);

            }
            effect.CurrentTechnique = effect.Techniques["Textured"];

            base.Render(device, effect);
        }
    }


    class BoxDrawCommand : DrawCommand
    {
        public BoundingBox m_boundingBox;
        private List<VertexPositionColor[]> m_triangles = new List<VertexPositionColor[]>();
        private List<int> m_triangleCounts = new List<int>();
        private static Vector3 topLeftFront = new Vector3(0.0f, 1.0f, 0.0f);
        private static Vector3 topLeftBack = new Vector3(0.0f, 1.0f, 1.0f);
        private static Vector3 topRightFront = new Vector3(1.0f, 1.0f, 0.0f);
        private static Vector3 topRightBack = new Vector3(1.0f, 1.0f, 1.0f);
        private static Vector3 btmLeftFront = new Vector3(0.0f, 0.0f, 0.0f);
        private static Vector3 btmLeftBack = new Vector3(0.0f, 0.0f, 1.0f);
        private static Vector3 btmRightFront = new Vector3(1.0f, 0.0f, 0.0f);
        private static Vector3 btmRightBack = new Vector3(1.0f, 0.0f, 1.0f);
        private static Vector3[] frontFace = { btmLeftFront, topLeftFront, topRightFront, btmRightFront, btmLeftFront };
        private static Vector3[] backFace = { btmRightBack, topRightBack, topLeftBack, btmLeftBack, btmRightBack };
        private static Vector3[] topFace = { topRightFront, topRightBack, topLeftBack, topLeftFront, topRightFront };
        private static Vector3[] btmFace = { btmLeftFront, btmLeftBack, btmRightBack, btmRightFront, btmLeftFront };
        private static Vector3[][] m_boxPoints = { frontFace, backFace, topFace, btmFace };

        public BoxDrawCommand(BoundingBox box, Color color, float thickness, bool warp) :
            base(color)
        {
            m_boundingBox = box;
            Matrix worldMatrix = Matrix.CreateScale(box.Max.X - box.Min.X, box.Max.Y - box.Min.Y, box.Max.Z - box.Min.Z);
            worldMatrix.Translation = box.Min;
            for(int i = 0; i < 4; i++)
            {
                Vector3[] points;
                if (warp)
                {
                    points = VertexNoise.WarpPoints(m_boxPoints[i], new Vector3(box.Max.X - box.Min.X, box.Max.Y - box.Min.Y, box.Max.Z - box.Min.Z), box.Min );
                }
                else
                {
                    points = m_boxPoints[i];
                }

                int count = -2;
               
                List<VertexPositionColor> triangles = SimpleDrawing.GetTriangleStrip(points, thickness, color, ref count, worldMatrix);
                m_triangles.Add(new VertexPositionColor[triangles.Count]);
                m_triangleCounts.Add(count);
                triangles.CopyTo(m_triangles[i]);
            }


        }

        public override void Render(GraphicsDevice device, Effect effect)
        {
            Matrix w = Matrix.Identity;

            effect.Parameters["xWorld"].SetValue(w);
            effect.CurrentTechnique = effect.Techniques["Untextured"];
            for (int i = 0; i < m_triangles.Count; i++)
            {
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    device.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, m_triangles[i], 0, m_triangleCounts[i]);

                }
            }
            effect.CurrentTechnique = effect.Techniques["Textured"];
            base.Render(device, effect);
        }
    }


    public class SimpleDrawing
    {
        private static ConcurrentBag<DrawCommand> m_commands = new ConcurrentBag<DrawCommand>();
        public static OrbitCamera m_camera = null;

        public static void DrawLineList(List<Vector3> points, Color color, float thickness)
        {
            m_commands.Add(new LineListCommand(points.ToArray(), color, thickness));
        }

        public static void DrawBoxList(List<BoundingBox> boxes, Color color, float thickness)
        {
            foreach (BoundingBox box in boxes)
            {
                m_commands.Add(new BoxDrawCommand(box, color, thickness, false));
            }
        }

        public static void DrawBox(BoundingBox box, Color color, float thickness)
        {
            m_commands.Add(new BoxDrawCommand(box, color, thickness, false));
        }

        public static void DrawBox(BoundingBox box, Color color, float thickness, bool warp)
        {
            m_commands.Add(new BoxDrawCommand(box, color, thickness, warp));
        }

        public static void Render(GraphicsDevice device, Effect effect, bool delete)
        {
            //effect.TextureEnabled = false;
            //effect.LightingEnabled = false;
            //effect.VertexColorEnabled = true;

            BlendState origBlen = device.BlendState;
            device.BlendState = BlendState.AlphaBlend;

            RasterizerState newState = RasterizerState.CullNone;
            RasterizerState oldState = device.RasterizerState;
            device.RasterizerState = newState;

            //Matrix originalWorld = effect.Parameters["xWorld"].GetValueMatrix();

            foreach (DrawCommand command in m_commands)
            {
                command.Render(device, effect);
                //effect.Parameters["xWorld"].SetValue(originalWorld);
            }

            if (oldState != null)
            {
                device.RasterizerState = oldState;
            }

            if (origBlen != null)
            {
                device.BlendState = origBlen;
            }


            if (delete)
            {
                while (m_commands.Count > 0)
                {
                    DrawCommand result = null;
                    m_commands.TryTake(out result);
                }
            }
        }

        public static List<VertexPositionColor> GetTriangleStrip(Vector3[] points, float thickness, Color color, ref int triangleCount, Matrix worldMatrix)
        {
            
            Vector3 lastPoint = Vector3.Zero;
            List<VertexPositionColor> list = new List<VertexPositionColor>();



            for (int i=0;i<points.Length;i++)
            {
                if (i == 0) { lastPoint = points[i]; continue; }
                Vector3 t1 = Vector3.Transform(lastPoint, worldMatrix);
                Vector3 t2 = Vector3.Transform(points[i], worldMatrix);
                Vector3 direction = t1 - t2;
                Vector3 normal = Vector3.Cross(direction,
                    LinearMathHelpers.GetClosestPointOnLineSegment(t1, t2, m_camera.Position) - m_camera.Position);
                direction.Normalize();
                normal.Normalize();


                Vector3 p1 = t1 + normal * thickness; triangleCount++;
                Vector3 p2 = t1 - normal * thickness; triangleCount++;
                Vector3 p3 = t2 + normal * thickness; triangleCount++;
                Vector3 p4 = t2 - normal * thickness; triangleCount++; 
                list.Add(new VertexPositionColor(p1, color));
                list.Add(new VertexPositionColor(p2, color));
                list.Add(new VertexPositionColor(p3, color));
                list.Add(new VertexPositionColor(p4, color));
                lastPoint = points[i];
            }

            return list;
        }
        
    }
}
