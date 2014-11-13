using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// A batch billboard is a set of billboards which are drawn with the
    /// instance manager.
    /// </summary>
    public sealed class BatchBillboardPrimitive : GeometricPrimitive
    {
        public BillboardPrimitive.BoardTextureCoords UVs { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public Texture2D Texture { get; set; }

        public List<Matrix> RelativeTransforms;
        public List<Color> Tints;

        public BatchBillboardPrimitive(GraphicsDevice device,
            Texture2D spriteSheet,
            int frameWidth,
            int frameHeight,
            Point frame,
            float width, float height, bool flipped, List<Matrix> relativeTransforms, List<Color> tints)
        {
            RelativeTransforms = relativeTransforms;
            UVs = new BillboardPrimitive.BoardTextureCoords(spriteSheet.Width, spriteSheet.Height, frameWidth, frameHeight, frame, flipped);
            Width = width;
            Tints = tints;
            Height = height;
            CreateVerticies(Color.White);
            ResetBuffer(device);
            Texture = spriteSheet;
        }


        private void CreateVerticies(Color color)
        {
            Vertices = new ExtendedVertex[RelativeTransforms.Count * 6];

            // Calculate the position of the vertices on the top face.
            Vector3 topLeftFront = new Vector3(-0.5f * Width, 0.5f * Height, 0.0f);
            Vector3 topRightFront = new Vector3(0.5f * Width, 0.5f * Height, 0.0f);

            // Calculate the position of the vertices on the bottom face.
            Vector3 btmLeftFront = new Vector3(-0.5f * Width, -0.5f * Height, 0.0f);
            Vector3 btmRightFront = new Vector3(0.5f * Width, -0.5f * Height, 0.0f);


            for(int i = 0; i < RelativeTransforms.Count; i++)
            {
                Vertices[i * 6 + 0] = new ExtendedVertex(Vector3.Transform(topLeftFront, RelativeTransforms[i]), Tints[i], UVs.UVs[0], UVs.Bounds);
                Vertices[i * 6 + 1] = new ExtendedVertex(Vector3.Transform(btmLeftFront, RelativeTransforms[i]), Tints[i], UVs.UVs[1], UVs.Bounds);
                Vertices[i * 6 + 2] = new ExtendedVertex(Vector3.Transform(topRightFront, RelativeTransforms[i]), Tints[i], UVs.UVs[2], UVs.Bounds);
                Vertices[i * 6 + 3] = new ExtendedVertex(Vector3.Transform(btmLeftFront, RelativeTransforms[i]), Tints[i], UVs.UVs[3], UVs.Bounds);
                Vertices[i * 6 + 4] = new ExtendedVertex(Vector3.Transform(btmRightFront, RelativeTransforms[i]), Tints[i], UVs.UVs[4], UVs.Bounds);
                Vertices[i * 6 + 5] = new ExtendedVertex(Vector3.Transform(topRightFront, RelativeTransforms[i]), Tints[i], UVs.UVs[5], UVs.Bounds);
            }
        }
    }

}