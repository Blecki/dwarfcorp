using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// Draws a 3D axis aligned box to the screen.
    /// </summary>
    internal class BoxDrawCommand3D : DrawCommand3D
    {
        public BoundingBox BoundingBox;
        private readonly List<VertexPositionColor[]> _stripVertices = new List<VertexPositionColor[]>();
        private readonly List<int> _stripTriangleCounts = new List<int>();
        private static readonly Vector3 TopLeftFront = new Vector3(0.0f, 1.0f, 0.0f);
        private static readonly Vector3 TopLeftBack = new Vector3(0.0f, 1.0f, 1.0f);
        private static readonly Vector3 TopRightFront = new Vector3(1.0f, 1.0f, 0.0f);
        private static readonly Vector3 TopRightBack = new Vector3(1.0f, 1.0f, 1.0f);
        private static readonly Vector3 BtmLeftFront = new Vector3(0.0f, 0.0f, 0.0f);
        private static readonly Vector3 BtmLeftBack = new Vector3(0.0f, 0.0f, 1.0f);
        private static readonly Vector3 BtmRightFront = new Vector3(1.0f, 0.0f, 0.0f);
        private static readonly Vector3 BtmRightBack = new Vector3(1.0f, 0.0f, 1.0f);

        private static readonly Vector3[] FrontFace =
        {
            BtmLeftFront,
            TopLeftFront,
            TopRightFront,
            BtmRightFront,
            BtmLeftFront
        };

        private static readonly Vector3[] BackFace =
        {
            BtmRightBack,
            TopRightBack,
            TopLeftBack,
            BtmLeftBack,
            BtmRightBack
        };

        private static readonly Vector3[] TopFace =
        {
            TopRightFront,
            TopRightBack,
            TopLeftBack,
            TopLeftFront,
            TopRightFront
        };

        private static readonly Vector3[] BtmFace =
        {
            BtmLeftFront,
            BtmLeftBack,
            BtmRightBack,
            BtmRightFront,
            BtmLeftFront
        };

        private static readonly Vector3[][] BoxPoints =
        {
            FrontFace,
            BackFace,
            TopFace,
            BtmFace
        };

        public BoxDrawCommand3D(BoundingBox box, Color color, float thickness, bool warp) :
            base(color)
        {
            BoundingBox = box;
            Matrix worldMatrix = Matrix.CreateScale(box.Max.X - box.Min.X, box.Max.Y - box.Min.Y, box.Max.Z - box.Min.Z);
            worldMatrix.Translation = box.Min;

            for(int i = 0; i < 4; i++)
            {
                Vector3[] points = warp ? VertexNoise.WarpPoints(BoxPoints[i], new Vector3(box.Max.X - box.Min.X, box.Max.Y - box.Min.Y, box.Max.Z - box.Min.Z), box.Min) : BoxPoints[i];

                int count = 0;

                List<VertexPositionColor> triangleStrip = Drawer3D.GetTriangleStrip(points, thickness, color, ref count, worldMatrix);
                _stripVertices.Add(new VertexPositionColor[triangleStrip.Count]);
                _stripTriangleCounts.Add(count);
                triangleStrip.CopyTo(_stripVertices[i]);
            }
        }

        public override void AccumulateStrips(LineStrip strips)
        {
            for(int i = 0; i < _stripVertices.Count; i++)
            {
                strips.NumTriangles += _stripTriangleCounts[i];

                if (strips.Vertices.Count > 0)
                {
                    strips.Vertices.Add(strips.Vertices.Last());
                    strips.Vertices.Add(_stripVertices[i][0]);
                    strips.Vertices.Add(_stripVertices[i][0]);
                    strips.NumTriangles += 1;
                }
                strips.Vertices.AddRange(_stripVertices[i]);
            }
        }

        public override void Render(GraphicsDevice device, Effect effect)
        {
            Matrix w = Matrix.Identity;

            effect.Parameters["xWorld"].SetValue(w);
            effect.CurrentTechnique = effect.Techniques["Untextured"];
            for(int i = 0; i < _stripVertices.Count; i++)
            {
                foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    device.DrawUserPrimitives(PrimitiveType.TriangleStrip, _stripVertices[i], 0, _stripTriangleCounts[i]);
                }
            }
            effect.CurrentTechnique = effect.Techniques["Textured"];
        }
    }

}