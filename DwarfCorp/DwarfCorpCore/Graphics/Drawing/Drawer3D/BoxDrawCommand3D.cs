// BoxDrawCommand3D.cs
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

using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     Draws a 3D axis aligned wireframe box to the screen. This is for convenience. It's quite slow.
    /// </summary>
    internal class BoxDrawCommand3D : DrawCommand3D
    {
        // Vertices associated with the box.
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

        /// <summary>
        ///     Number of triangles in each strip.
        /// </summary>
        private readonly List<int> _stripTriangleCounts = new List<int>();

        /// <summary>
        ///     Vertices to draw. Each array is a single triangle strip.
        /// </summary>
        private readonly List<VertexPositionColor[]> _stripVertices = new List<VertexPositionColor[]>();

        /// <summary>
        ///     Box to draw.
        /// </summary>
        public BoundingBox BoundingBox;

        /// <summary>
        ///     Create and draw a wireframe bounding box.
        /// </summary>
        /// <param name="box">The box to draw.</param>
        /// <param name="color">The color of the box.</param>
        /// <param name="thickness">Thickness (in voxels) of the wireframe.</param>
        /// <param name="warp">If true, warps the box to match the terrain.</param>
        public BoxDrawCommand3D(BoundingBox box, Color color, float thickness, bool warp) :
            base(color)
        {
            BoundingBox = box;
            Matrix worldMatrix = Matrix.CreateScale(box.Max.X - box.Min.X, box.Max.Y - box.Min.Y, box.Max.Z - box.Min.Z);
            worldMatrix.Translation = box.Min;

            for (int i = 0; i < 4; i++)
            {
                Vector3[] points = warp
                    ? VertexNoise.WarpPoints(BoxPoints[i],
                        new Vector3(box.Max.X - box.Min.X, box.Max.Y - box.Min.Y, box.Max.Z - box.Min.Z), box.Min)
                    : BoxPoints[i];

                int count = 0;

                List<VertexPositionColor> triangleStrip = Drawer3D.GetTriangleStrip(points, thickness, color, ref count,
                    worldMatrix);
                _stripVertices.Add(new VertexPositionColor[triangleStrip.Count]);
                _stripTriangleCounts.Add(count);
                triangleStrip.CopyTo(_stripVertices[i]);
            }
        }

        /// <summary>
        ///     Create the vertices in the triangle strips.
        /// </summary>
        /// <param name="strips">Triangle strip to build from the box draw command.</param>
        public override void AccumulateStrips(LineStrip strips)
        {
            for (int i = 0; i < _stripVertices.Count; i++)
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

        /// <summary>
        ///     Draws a 3D axis aligned box to the screen.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="effect"></param>
        public override void Render(GraphicsDevice device, Effect effect)
        {
            Matrix w = Matrix.Identity;

            effect.Parameters["xWorld"].SetValue(w);
            effect.CurrentTechnique = effect.Techniques["Untextured"];
            for (int i = 0; i < _stripVertices.Count; i++)
            {
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    device.DrawUserPrimitives(PrimitiveType.TriangleStrip, _stripVertices[i], 0, _stripTriangleCounts[i]);
                }
            }
            effect.CurrentTechnique = effect.Techniques["Textured"];
        }
    }

    /// <summary>
    ///     Draws a 3D plane to the screen. This is for convenience/debugging and should not be used often.
    ///     It is very slow.
    /// </summary>
    internal class PlaneDrawCommand : DrawCommand3D
    {
        public static VertexBuffer VertBuffer = null;
        public static IndexBuffer IndexBuffer = null;

        private static readonly Vector3 TopLeftFront = new Vector3(0.0f, 0.0f, 0.0f);
        private static readonly Vector3 TopLeftBack = new Vector3(0.0f, 0.0f, 1.0f);
        private static readonly Vector3 TopRightFront = new Vector3(1.0f, 0.0f, 0.0f);
        private static readonly Vector3 TopRightBack = new Vector3(1.0f, 0.0f, 1.0f);

        private static readonly VertexPositionColor[] TopFace =
        {
            new VertexPositionColor {Position = TopRightFront, Color = Color.White},
            new VertexPositionColor {Position = TopRightBack, Color = Color.White},
            new VertexPositionColor {Position = TopLeftBack, Color = Color.White},
            new VertexPositionColor {Position = TopLeftFront, Color = Color.White}
        };

        private static readonly short[] Idx =
        {
            0, 1, 2,
            0, 3, 2
        };

        /// <summary>
        ///     Draw a plane at the specified position and scale assume identity orientation)
        /// </summary>
        /// <param name="pos">Position of the plane.</param>
        /// <param name="scale">Size of the plane (in voxels)</param>
        /// <param name="color">RGBA color of the plane.</param>
        public PlaneDrawCommand(Vector3 pos, Vector3 scale, Color color) :
            base(color)
        {
            DrawAccumlatedStrips = false;
            if (VertBuffer == null)
            {
                VertBuffer = new VertexBuffer(PlayState.GUI.Graphics, VertexPositionColor.VertexDeclaration, 4,
                    BufferUsage.None);
                IndexBuffer = new IndexBuffer(PlayState.GUI.Graphics, typeof (short), 6, BufferUsage.None);
                VertBuffer.SetData(TopFace);
                IndexBuffer.SetData(Idx);
            }
            PlaneTransform = Matrix.CreateScale(scale)*Matrix.CreateTranslation(pos - scale*0.5f);
        }

        /// <summary>
        ///     Position/orientaiton of the plane with respect to the world.
        /// </summary>
        public Matrix PlaneTransform { get; set; }


        public override void Render(GraphicsDevice device, Effect effect)
        {
            effect.Parameters["xWorld"].SetValue(PlaneTransform);
            effect.Parameters["xTint"].SetValue(ColorToDraw.ToVector4());
            effect.CurrentTechnique = effect.Techniques["Untextured"];
            device.SetVertexBuffer(VertBuffer);
            device.Indices = IndexBuffer;
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
            }
            effect.CurrentTechnique = effect.Techniques["Textured"];
        }

        public override void AccumulateStrips(LineStrip vertices)
        {
        }
    }
}