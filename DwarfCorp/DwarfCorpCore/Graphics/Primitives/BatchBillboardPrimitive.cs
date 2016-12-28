using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     A batch billboard is a set of billboards which are drawn with aggregate geometry.
    ///     This is used for drawing huge numbers of detail sprites (for instance, for grass motes).
    ///     This should be used for huge numbers of STATIC billboards. It should not be used for dynamically moving things.
    /// </summary>
    public sealed class BatchBillboardPrimitive : GeometricPrimitive
    {
        /// <summary>
        ///     One transform for each billboard relative to the root.
        /// </summary>
        public List<Matrix> RelativeTransforms;

        /// <summary>
        ///     The light tints for each billboard. (R = sun, G = ambient, B = torch)
        /// </summary>
        public List<Color> Tints;

        /// <summary>
        ///     The vertex color tints for each billboard. (RGBA)
        /// </summary>
        public List<Color> VertColors;

        /// <summary>
        ///     Create a batced billboard primitive.
        /// </summary>
        /// <param name="spriteSheet">The texture of the billboard.</param>
        /// <param name="frameWidth">Width in pixels of the billboard.</param>
        /// <param name="frameHeight">Height in pixels of the billboard.</param>
        /// <param name="frame">(x, y) location in the sprite sheet of the billboard.</param>
        /// <param name="width">Width of the billboard in voxels.</param>
        /// <param name="height">Height of the billboard in voxels.</param>
        /// <param name="flipped">If true, flips the sprite horizontally.</param>
        /// <param name="relativeTransforms">List of positions/orientations for a large number of billboards.</param>
        /// <param name="tints">List of light tings for a large number of billboards.</param>
        /// <param name="vertColors">List of vertex colors for a large number of billboards.</param>
        public BatchBillboardPrimitive(Texture2D spriteSheet,
            int frameWidth,
            int frameHeight,
            Point frame,
            float width, float height, bool flipped, List<Matrix> relativeTransforms, List<Color> tints,
            List<Color> vertColors)
        {
            RelativeTransforms = relativeTransforms;
            UVs = new BillboardPrimitive.BoardTextureCoords(spriteSheet.Width, spriteSheet.Height, frameWidth,
                frameHeight, frame, flipped);
            Width = width;
            Tints = tints;
            VertColors = vertColors;
            Height = height;
            CreateVerticies();
            Texture = spriteSheet;
        }

        /// <summary>
        ///     UV coordinates of the billboard.
        /// </summary>
        public BillboardPrimitive.BoardTextureCoords UVs { get; set; }

        /// <summary>
        ///     Width of the billboard in voxels.
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        ///     Height of the billboard in voxels.
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        ///     Texture to use for the billboard.
        /// </summary>
        public Texture2D Texture { get; set; }


        /// <summary>
        ///     Creates a huge vertex buffer consisting of the union of all the billboards. This should NOT be updated
        ///     every frame, but only infrequently. This is the most efficient way of rendering large amounts of static geometry.
        ///     Be careful as it uses a lot of memory.
        /// </summary>
        private void CreateVerticies()
        {
            // Four vertices per billboard.
            Vertices = new ExtendedVertex[RelativeTransforms.Count*4];

            // Create canonical primitive.
            var topLeftFront = new Vector3(-0.5f*Width, 0.5f*Height, 0.0f);
            var topRightFront = new Vector3(0.5f*Width, 0.5f*Height, 0.0f);
            var btmLeftFront = new Vector3(-0.5f*Width, -0.5f*Height, 0.0f);
            var btmRightFront = new Vector3(0.5f*Width, -0.5f*Height, 0.0f);

            // Create index buffer.
            Indexes = new ushort[RelativeTransforms.Count*6];

            // Populate the vertices in the vertex buffer by transforming all of the billboards.
            for (int i = 0; i < RelativeTransforms.Count; i++)
            {
                int vertOffset = i*4;
                Vertices[vertOffset + 0] = new ExtendedVertex(Vector3.Transform(topLeftFront, RelativeTransforms[i]),
                    Tints[i], VertColors[i], UVs.UVs[0], UVs.Bounds);
                Vertices[vertOffset + 1] = new ExtendedVertex(Vector3.Transform(btmLeftFront, RelativeTransforms[i]),
                    Tints[i], VertColors[i], UVs.UVs[1], UVs.Bounds);
                Vertices[vertOffset + 2] = new ExtendedVertex(Vector3.Transform(topRightFront, RelativeTransforms[i]),
                    Tints[i], VertColors[i], UVs.UVs[2], UVs.Bounds);
                Vertices[vertOffset + 3] = new ExtendedVertex(Vector3.Transform(btmRightFront, RelativeTransforms[i]),
                    Tints[i], VertColors[i], UVs.UVs[3], UVs.Bounds);


                int indOffset = i*6;
                Indexes[indOffset + 0] = (ushort) (vertOffset + 1);
                Indexes[indOffset + 1] = (ushort) (vertOffset + 0);
                Indexes[indOffset + 2] = (ushort) (vertOffset + 2);
                Indexes[indOffset + 3] = (ushort) (vertOffset + 1);
                Indexes[indOffset + 4] = (ushort) (vertOffset + 2);
                Indexes[indOffset + 5] = (ushort) (vertOffset + 3);
            }
        }
    }
}