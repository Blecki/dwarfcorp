using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     Specifies one of the 6 faces of a box.
    /// </summary>
    public enum BoxFace
    {
        Top,
        Bottom,
        Left,
        Right,
        Front,
        Back
    }

    #region legacy

    /// <summary>
    ///     This is a legacy primitive that existed before boxes were using index buffers.
    /// </summary>
    public sealed class OldBoxPrimitive : GeometricPrimitive
    {
        private const int NumFaces = 6;
        private const int NumVertices = 36;

        public BoundingBox BoundingBox;


        public OldBoxPrimitive(GraphicsDevice device, float width, float height, float depth, BoxTextureCoords uvs)
        {
            Width = width;
            Height = height;
            Depth = depth;

            UVs = uvs;
            CreateVerticies();
            ResetBuffer(device);
            BoundingBox = new BoundingBox(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(width, height, depth));
        }

        public float Width { get; set; }
        public float Height { get; set; }
        public float Depth { get; set; }
        public BoxTextureCoords UVs { get; set; }


        public void GetFace(BoxFace face, BoxPrimitive.BoxTextureCoords uvs, out int index, out int count)
        {
            switch (face)
            {
                case BoxFace.Back:
                    GetBackFace(uvs, out index, out count);
                    return;
                case BoxFace.Front:
                    GetFrontFace(uvs, out index, out count);
                    return;
                case BoxFace.Left:
                    GetLeftFace(uvs, out index, out count);
                    return;
                case BoxFace.Right:
                    GetRightFace(uvs, out index, out count);
                    return;
                case BoxFace.Top:
                    GetTopFace(uvs, out index, out count);
                    return;
                case BoxFace.Bottom:
                    GetBottomFace(uvs, out index, out count);
                    return;
            }
            index = 0;
            count = 0;
        }

        public void GetFrontFace(BoxPrimitive.BoxTextureCoords uvs, out int idx, out int count)
        {
            idx = 0;
            count = 6;
        }

        public void GetBackFace(BoxPrimitive.BoxTextureCoords uvs, out int idx, out int count)
        {
            idx = 6;
            count = 6;
        }

        public void GetTopFace(BoxPrimitive.BoxTextureCoords uvs, out int idx, out int count)
        {
            idx = 12;
            count = 6;
        }

        public void GetBottomFace(BoxPrimitive.BoxTextureCoords uvs, out int idx, out int count)
        {
            idx = 18;
            count = 6;
        }

        public void GetLeftFace(BoxPrimitive.BoxTextureCoords uvs, out int idx, out int count)
        {
            idx = 24;
            count = 6;
        }

        public void GetRightFace(BoxPrimitive.BoxTextureCoords uvs, out int idx, out int count)
        {
            idx = 30;
            count = 6;
        }

        private void CreateVerticies()
        {
            Vertices = new ExtendedVertex[NumVertices];

            // Calculate the position of the vertices on the top face.
            var topLeftFront = new Vector3(0.0f, Height, 0.0f);
            var topLeftBack = new Vector3(0.0f, Height, Depth);
            var topRightFront = new Vector3(Width, Height, 0.0f);
            var topRightBack = new Vector3(Width, Height, Depth);


            // Calculate the position of the vertices on the bottom face.
            var btmLeftFront = new Vector3(0.0f, 0.0f, 0.0f);
            var btmLeftBack = new Vector3(0.0f, 0.0f, Depth);
            var btmRightFront = new Vector3(Width, 0.0f, 0.0f);
            var btmRightBack = new Vector3(Width, 0.0f, Depth);

            // Normal vectors for each face (needed for lighting / display)
            /*
            Vector3 normalFront = new Vector3(0.0f, 0.0f, 1.0f) * SideLength;
            Vector3 normalBack = new Vector3(0.0f, 0.0f, -1.0f) * SideLength;
            Vector3 normalTop = new Vector3(0.0f, 1.0f, 0.0f) * SideLength;
            Vector3 normalBottom = new Vector3(0.0f, -1.0f, 0.0f) * SideLength;
            Vector3 normalLeft = new Vector3(-1.0f, 0.0f, 0.0f) * SideLength;
            Vector3 normalRight = new Vector3(1.0f, 0.0f, 0.0f) * SideLength;
             */


            // Add the vertices for the FRONT face.

            Vertices[0] = new ExtendedVertex(topLeftFront, Color.White, Color.White, UVs.m_uvs[0], UVs.Bounds[0]);
            Vertices[1] = new ExtendedVertex(btmLeftFront, Color.White, Color.White, UVs.m_uvs[1], UVs.Bounds[0]);
            Vertices[2] = new ExtendedVertex(topRightFront, Color.White, Color.White, UVs.m_uvs[2], UVs.Bounds[0]);
            Vertices[3] = new ExtendedVertex(btmLeftFront, Color.White, Color.White, UVs.m_uvs[3], UVs.Bounds[0]);
            Vertices[4] = new ExtendedVertex(btmRightFront, Color.White, Color.White, UVs.m_uvs[4], UVs.Bounds[0]);
            Vertices[5] = new ExtendedVertex(topRightFront, Color.White, Color.White, UVs.m_uvs[5], UVs.Bounds[0]);

            // Add the vertices for the BACK face.
            Vertices[6] = new ExtendedVertex(topLeftBack, Color.White, Color.White, UVs.m_uvs[6], UVs.Bounds[1]);
            Vertices[7] = new ExtendedVertex(topRightBack, Color.White, Color.White, UVs.m_uvs[7], UVs.Bounds[1]);
            Vertices[8] = new ExtendedVertex(btmLeftBack, Color.White, Color.White, UVs.m_uvs[8], UVs.Bounds[1]);
            Vertices[9] = new ExtendedVertex(btmLeftBack, Color.White, Color.White, UVs.m_uvs[9], UVs.Bounds[1]);
            Vertices[10] = new ExtendedVertex(topRightBack, Color.White, Color.White, UVs.m_uvs[10], UVs.Bounds[1]);
            Vertices[11] = new ExtendedVertex(btmRightBack, Color.White, Color.White, UVs.m_uvs[11], UVs.Bounds[1]);

            // Add the vertices for the TOP face.
            Vertices[12] = new ExtendedVertex(topLeftFront, Color.White, Color.White, UVs.m_uvs[12], UVs.Bounds[2]);
            Vertices[13] = new ExtendedVertex(topRightBack, Color.White, Color.White, UVs.m_uvs[13], UVs.Bounds[2]);
            Vertices[14] = new ExtendedVertex(topLeftBack, Color.White, Color.White, UVs.m_uvs[14], UVs.Bounds[2]);
            Vertices[15] = new ExtendedVertex(topLeftFront, Color.White, Color.White, UVs.m_uvs[15], UVs.Bounds[2]);
            Vertices[16] = new ExtendedVertex(topRightFront, Color.White, Color.White, UVs.m_uvs[16], UVs.Bounds[2]);
            Vertices[17] = new ExtendedVertex(topRightBack, Color.White, Color.White, UVs.m_uvs[17], UVs.Bounds[2]);

            // Add the vertices for the BOTTOM face. 
            Vertices[18] = new ExtendedVertex(btmLeftFront, Color.White, Color.White, UVs.m_uvs[18], UVs.Bounds[3]);
            Vertices[19] = new ExtendedVertex(btmLeftBack, Color.White, Color.White, UVs.m_uvs[19], UVs.Bounds[3]);
            Vertices[20] = new ExtendedVertex(btmRightBack, Color.White, Color.White, UVs.m_uvs[20], UVs.Bounds[3]);
            Vertices[21] = new ExtendedVertex(btmLeftFront, Color.White, Color.White, UVs.m_uvs[21], UVs.Bounds[3]);
            Vertices[22] = new ExtendedVertex(btmRightBack, Color.White, Color.White, UVs.m_uvs[22], UVs.Bounds[3]);
            Vertices[23] = new ExtendedVertex(btmRightFront, Color.White, Color.White, UVs.m_uvs[23], UVs.Bounds[3]);

            // Add the vertices for the LEFT face.
            Vertices[24] = new ExtendedVertex(topLeftFront, Color.White, Color.White, UVs.m_uvs[24], UVs.Bounds[4]);
            Vertices[25] = new ExtendedVertex(btmLeftBack, Color.White, Color.White, UVs.m_uvs[25], UVs.Bounds[4]);
            Vertices[26] = new ExtendedVertex(btmLeftFront, Color.White, Color.White, UVs.m_uvs[26], UVs.Bounds[4]);
            Vertices[27] = new ExtendedVertex(topLeftBack, Color.White, Color.White, UVs.m_uvs[27], UVs.Bounds[4]);
            Vertices[28] = new ExtendedVertex(btmLeftBack, Color.White, Color.White, UVs.m_uvs[28], UVs.Bounds[4]);
            Vertices[29] = new ExtendedVertex(topLeftFront, Color.White, Color.White, UVs.m_uvs[29], UVs.Bounds[4]);

            // Add the vertices for the RIGHT face. 
            Vertices[30] = new ExtendedVertex(topRightFront, Color.White, Color.White, UVs.m_uvs[30], UVs.Bounds[5]);
            Vertices[31] = new ExtendedVertex(btmRightFront, Color.White, Color.White, UVs.m_uvs[31], UVs.Bounds[5]);
            Vertices[32] = new ExtendedVertex(btmRightBack, Color.White, Color.White, UVs.m_uvs[32], UVs.Bounds[5]);
            Vertices[33] = new ExtendedVertex(topRightBack, Color.White, Color.White, UVs.m_uvs[33], UVs.Bounds[5]);
            Vertices[34] = new ExtendedVertex(topRightFront, Color.White, Color.White, UVs.m_uvs[34], UVs.Bounds[5]);
            Vertices[35] = new ExtendedVertex(btmRightBack, Color.White, Color.White, UVs.m_uvs[35], UVs.Bounds[5]);
        }

        public class BoxTextureCoords
        {
            public Vector4[] Bounds = new Vector4[6];
            public Point m_back;
            public Point m_bottom;
            public int m_cellHeight;
            public int m_cellWidth;
            public Point m_front;
            public Point m_left;
            public Point m_right;
            public int m_texHeight;
            public int m_texWidth;
            public Point m_top;
            public Vector2[] m_uvs = new Vector2[NumVertices];

            public BoxTextureCoords()
            {
            }

            public BoxTextureCoords(int totalTextureWidth, int totalTextureHeight,
                FaceData front, FaceData back, FaceData top, FaceData bottom, FaceData left, FaceData right)
            {
                m_texWidth = totalTextureWidth;
                m_texHeight = totalTextureHeight;

                var textureTopLeft = new Vector2(0.0f, 0.0f);
                var textureTopRight = new Vector2(1.0f, 0.0f);
                var textureBottomLeft = new Vector2(0.0f, 1.0f);
                var textureBottomRight = new Vector2(1.0f, 1.0f);

                var cells = new List<FaceData>
                {
                    front,
                    back,
                    top,
                    bottom,
                    left,
                    right
                };


                var baseCoords = new List<Vector2>
                {
                    textureTopLeft,
                    textureBottomLeft,
                    textureTopRight,
                    textureBottomLeft,
                    textureBottomRight,
                    textureTopRight,
                    textureTopRight,
                    textureTopLeft,
                    textureBottomRight,
                    textureBottomRight,
                    textureTopLeft,
                    textureBottomLeft,
                    textureBottomLeft,
                    textureTopRight,
                    textureTopLeft,
                    textureBottomLeft,
                    textureBottomRight,
                    textureTopRight,
                    textureTopLeft,
                    textureBottomLeft,
                    textureBottomRight,
                    textureTopLeft,
                    textureBottomRight,
                    textureTopRight,
                    textureTopRight,
                    textureBottomLeft,
                    textureBottomRight,
                    textureTopLeft,
                    textureBottomLeft,
                    textureTopRight,
                    textureTopLeft,
                    textureBottomLeft,
                    textureBottomRight,
                    textureTopRight,
                    textureTopLeft,
                    textureBottomRight
                };


                for (int face = 0; face < 6; face++)
                {
                    var pixelCoords = new Vector2(cells[face].Rect.X, cells[face].Rect.Y);
                    float normalizeX = cells[face].Rect.Width/(float) (totalTextureWidth);
                    float normalizeY = cells[face].Rect.Height/(float) (totalTextureHeight);
                    var normalizedCoords = new Vector2(pixelCoords.X/totalTextureWidth, pixelCoords.Y/totalTextureHeight);
                    Bounds[face] = new Vector4(normalizedCoords.X + 0.001f, normalizedCoords.Y + 0.001f,
                        normalizedCoords.X + normalizeX - 0.001f, normalizedCoords.Y + normalizeY - 0.001f);


                    for (int vert = 0; vert < 6; vert++)
                    {
                        int index = vert + face*6;

                        if (!cells[face].FlipXY)
                        {
                            m_uvs[index] = new Vector2(normalizedCoords.X + baseCoords[index].Y*normalizeX,
                                normalizedCoords.Y + baseCoords[index].X*normalizeY);
                        }
                        else
                        {
                            m_uvs[index] = new Vector2(normalizedCoords.X + baseCoords[index].X*normalizeX,
                                normalizedCoords.Y + baseCoords[index].Y*normalizeY);
                        }
                    }
                }
            }

            public BoxTextureCoords(int totalTextureWidth, int totalTextureHeight,
                int cellWidth, int cellHeight,
                Point front, Point back,
                Point top, Point bottom,
                Point left, Point right)
                : this(
                    totalTextureWidth, totalTextureHeight, new FaceData(front.X, front.Y, cellWidth),
                    new FaceData(back.X, back.Y, cellWidth), new FaceData(top.X, top.Y, cellWidth),
                    new FaceData(bottom.X, bottom.Y, cellWidth), new FaceData(left.X, left.Y, cellWidth),
                    new FaceData(right.X, right.Y, cellWidth))
            {
            }
        }

        public class FaceData
        {
            public FaceData(int x, int y, int tileSize)
            {
                Rect = new Rectangle(x*tileSize, y*tileSize, tileSize, tileSize);
                FlipXY = false;
            }

            public FaceData(Rectangle rect)
            {
                Rect = rect;
                FlipXY = false;
            }

            public FaceData(Rectangle rect, bool flip)
            {
                Rect = rect;
                FlipXY = flip;
            }

            public Rectangle Rect { get; set; }
            public bool FlipXY { get; set; }
        }
    }

    #endregion

    /// <summary>
    ///     A box primitive is just a simple textured rectangular box.
    /// </summary>
    [JsonObject(IsReference = true)]
    public sealed class BoxPrimitive : GeometricPrimitive
    {
        private const int NumVertices = 24;

        /// <summary>
        ///     This is the bounding box encasing the box primitive.
        /// </summary>
        public BoundingBox BoundingBox;

        /// <summary>
        ///     These are a set of indices in which the top face of the box is flipped. What do I mean by this?
        ///     Normally, the indices are like this:
        ///     1 ------ 2
        ///     |.       |
        ///     |  .     |
        ///     |    .   |
        ///     |      . |
        ///     3--------4
        ///     The flipped indices are like this:
        ///     1 ------ 2
        ///     |       .|
        ///     |     .  |
        ///     |   .    |
        ///     | .      |
        ///     3--------4
        ///     Why is this necessary? Because sometimes the topology of boxes laid out side-by-side like this does not
        ///     make for very pretty lighting, so the top face sometimes has to be flipped to make the lighting prettier.
        ///     The things I do for aesthetics...
        /// </summary>
        public ushort[] FlippedIndexes = null;

        /// <summary>
        ///     Creates a box primitive vertex buffer.
        /// </summary>
        /// <param name="width">Width (x) of the box in voxels.</param>
        /// <param name="height">Height (y) of the box in voxels.</param>
        /// <param name="depth">Depth (z) of the box in voxels.</param>
        /// <param name="uvs">UV texture coordinates for the box.</param>
        public BoxPrimitive(float width, float height, float depth, BoxTextureCoords uvs)
        {
            Width = width;
            Height = height;
            Depth = depth;
            Deltas = new VoxelVertex[NumVertices];
            UVs = uvs;
            CreateVerticies();
            BoundingBox = new BoundingBox(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(width, height, depth));
            for (int i = 0; i < NumVertices; i++)
            {
                Deltas[i] = VoxelChunk.GetNearestDelta(Vertices[i].Position);
            }
        }

        /// <summary>
        ///     X size of the box.
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        ///     Y size of the box.
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        ///     Z size of the box.
        /// </summary>
        public float Depth { get; set; }

        /// <summary>
        ///     Box texture coordinates.
        /// </summary>
        public BoxTextureCoords UVs { get; set; }

        /// <summary>
        ///     These are the offsets of each of the vertices of the box.
        /// </summary>
        public VoxelVertex[] Deltas { get; set; }

        public override void Render(GraphicsDevice device)
        {
            base.Render(device);
        }

        /// <summary>
        ///     Returns a face of the box in terms of indices.
        /// </summary>
        /// <param name="face">The face to query.</param>
        /// <param name="index">Index buffer offset into the face</param>
        /// <param name="count">Number of indexes in the face.</param>
        /// <param name="vertexOffset">Vertex buffer offset into the face.</param>
        /// <param name="vertexCount">Number of vertices in the face.</param>
        public void GetFace(BoxFace face, out int index, out int count, out int vertexOffset, out int vertexCount)
        {
            vertexCount = 4;
            count = 6;
            vertexOffset = 0;
            switch (face)
            {
                case BoxFace.Back:
                    index = 6;
                    vertexOffset = 4;
                    return;
                case BoxFace.Front:
                    index = 0;
                    vertexOffset = 0;
                    return;
                case BoxFace.Left:
                    index = 24;
                    vertexOffset = 16;
                    return;
                case BoxFace.Right:
                    index = 30;
                    vertexOffset = 20;
                    return;
                case BoxFace.Top:
                    index = 12;
                    vertexOffset = 8;
                    return;
                case BoxFace.Bottom:
                    index = 18;
                    vertexOffset = 12;
                    return;
            }
            index = 0;
            count = 0;
        }


        /// <summary>
        ///     Create a new vertex buffer for the box.
        /// </summary>
        private void CreateVerticies()
        {
            Indexes = new ushort[36];
            FlippedIndexes = new ushort[36];
            Vertices = new ExtendedVertex[NumVertices];

            // Calculate the position of the vertices on the top face.
            var topLeftFront = new Vector3(0.0f, Height, 0.0f);
            var topLeftBack = new Vector3(0.0f, Height, Depth);
            var topRightFront = new Vector3(Width, Height, 0.0f);
            var topRightBack = new Vector3(Width, Height, Depth);

            // Calculate the position of the vertices on the bottom face.
            var btmLeftFront = new Vector3(0.0f, 0.0f, 0.0f);
            var btmLeftBack = new Vector3(0.0f, 0.0f, Depth);
            var btmRightFront = new Vector3(Width, 0.0f, 0.0f);
            var btmRightBack = new Vector3(Width, 0.0f, Depth);

            // Normal vectors for each face (needed for lighting / display)


            // Add the vertices for the FRONT face.
            Vertices[0] = new ExtendedVertex(topLeftFront, Color.White, Color.White, UVs.Uvs[0], UVs.Bounds[0]);
            Vertices[1] = new ExtendedVertex(btmLeftFront, Color.White, Color.White, UVs.Uvs[1], UVs.Bounds[0]);
            Vertices[2] = new ExtendedVertex(btmRightFront, Color.White, Color.White, UVs.Uvs[2], UVs.Bounds[0]);
            Vertices[3] = new ExtendedVertex(topRightFront, Color.White, Color.White, UVs.Uvs[3], UVs.Bounds[0]);

            /* 
             *  0 . . . 3
                .     . .
                .   .   .
                . .     .
                1 . . . 2
             */
            Indexes[0] = 0;
            Indexes[1] = 1;
            Indexes[2] = 3;
            Indexes[3] = 3;
            Indexes[4] = 1;
            Indexes[5] = 2;

            /* 
             *  0 . . . 3
                . .     .
                .   .   .
                .     . .
                1 . . . 2
             */
            FlippedIndexes[0] = 0;
            FlippedIndexes[1] = 1;
            FlippedIndexes[2] = 2;
            FlippedIndexes[3] = 3;
            FlippedIndexes[4] = 0;
            FlippedIndexes[5] = 2;

            // Add the vertices for the BACK face.
            Vertices[4] = new ExtendedVertex(btmLeftBack, Color.White, Color.White, UVs.Uvs[4], UVs.Bounds[1]);
            Vertices[5] = new ExtendedVertex(topLeftBack, Color.White, Color.White, UVs.Uvs[5], UVs.Bounds[1]);
            Vertices[6] = new ExtendedVertex(topRightBack, Color.White, Color.White, UVs.Uvs[6], UVs.Bounds[1]);
            Vertices[7] = new ExtendedVertex(btmRightBack, Color.White, Color.White, UVs.Uvs[7], UVs.Bounds[1]);


            /*  
             *  4 . . . 7
                .     . .
                .   .   .
                . .     .
                5 . . . 6
             */
            Indexes[6] = 4;
            Indexes[7] = 5;
            Indexes[8] = 7;
            Indexes[9] = 7;
            Indexes[10] = 5;
            Indexes[11] = 6;

            /*             
             *  4 . . . 7
                . .     .
                .   .   .
                .     . .
                5 . . . 6
             */
            FlippedIndexes[6] = 4;
            FlippedIndexes[7] = 5;
            FlippedIndexes[8] = 6;
            FlippedIndexes[9] = 6;
            FlippedIndexes[10] = 7;
            FlippedIndexes[11] = 4;

            // Add the vertices for the TOP face.
            Vertices[8] = new ExtendedVertex(topLeftBack, Color.White, Color.White, UVs.Uvs[8], UVs.Bounds[2]);
            Vertices[9] = new ExtendedVertex(topLeftFront, Color.White, Color.White, UVs.Uvs[9], UVs.Bounds[2]);
            Vertices[10] = new ExtendedVertex(topRightFront, Color.White, Color.White, UVs.Uvs[10], UVs.Bounds[2]);
            Vertices[11] = new ExtendedVertex(topRightBack, Color.White, Color.White, UVs.Uvs[11], UVs.Bounds[2]);


            /* 
             *  8 . . .11
                .     . .
                .   .   .
                .  .    .
                9 . . . 10
             */
            Indexes[12] = 8;
            Indexes[13] = 9;
            Indexes[14] = 11;
            Indexes[15] = 10;
            Indexes[16] = 11;
            Indexes[17] = 9;


            /* 
             *  8 . . .11
                . .     .
                .   .   .
                .     . .
                9 . . .10
             */
            FlippedIndexes[12] = 8;
            FlippedIndexes[13] = 10;
            FlippedIndexes[14] = 11;
            FlippedIndexes[15] = 10;
            FlippedIndexes[16] = 8;
            FlippedIndexes[17] = 9;

            // Add the vertices for the BOTTOM face. 
            Vertices[12] = new ExtendedVertex(btmLeftFront, Color.White, Color.White, UVs.Uvs[12], UVs.Bounds[3]);
            Vertices[13] = new ExtendedVertex(btmLeftBack, Color.White, Color.White, UVs.Uvs[13], UVs.Bounds[3]);
            Vertices[14] = new ExtendedVertex(btmRightBack, Color.White, Color.White, UVs.Uvs[14], UVs.Bounds[3]);
            Vertices[15] = new ExtendedVertex(btmRightFront, Color.White, Color.White, UVs.Uvs[15], UVs.Bounds[3]);


            /* 
             *  12. . .15
                .     . .
                .   .   .
                .  .    .
                13. . .14
             */

            Indexes[18] = 12;
            Indexes[19] = 13;
            Indexes[20] = 15;
            Indexes[21] = 15;
            Indexes[22] = 13;
            Indexes[23] = 14;


            /* 
             *  12. . .15
                . .     .
                .   .   .
                .     . .
                13. . .14
             */
            FlippedIndexes[18] = 12;
            FlippedIndexes[19] = 13;
            FlippedIndexes[20] = 14;
            FlippedIndexes[21] = 15;
            FlippedIndexes[22] = 12;
            FlippedIndexes[23] = 14;

            // Add the vertices for the LEFT face.
            Vertices[16] = new ExtendedVertex(btmLeftFront, Color.White, Color.White, UVs.Uvs[16], UVs.Bounds[4]);
            Vertices[17] = new ExtendedVertex(topLeftFront, Color.White, Color.White, UVs.Uvs[17], UVs.Bounds[4]);
            Vertices[18] = new ExtendedVertex(topLeftBack, Color.White, Color.White, UVs.Uvs[18], UVs.Bounds[4]);
            Vertices[19] = new ExtendedVertex(btmLeftBack, Color.White, Color.White, UVs.Uvs[19], UVs.Bounds[4]);

            /* 
             *  16. . .19
                .     . .
                .   .   .
                .  .    .
                17. . .18
             */
            Indexes[24] = 16;
            Indexes[25] = 17;
            Indexes[26] = 19;
            Indexes[27] = 18;
            Indexes[28] = 19;
            Indexes[29] = 17;


            /* 
             *  16. . .19
                . .     .
                .   .   .
                .     . .
                17. . .18
             */
            FlippedIndexes[24] = 16;
            FlippedIndexes[25] = 17;
            FlippedIndexes[26] = 18;
            FlippedIndexes[27] = 18;
            FlippedIndexes[28] = 19;
            FlippedIndexes[29] = 16;


            // Add the vertices for the RIGHT face. 
            Vertices[20] = new ExtendedVertex(topRightFront, Color.White, Color.White, UVs.Uvs[20], UVs.Bounds[5]);
            Vertices[21] = new ExtendedVertex(btmRightFront, Color.White, Color.White, UVs.Uvs[21], UVs.Bounds[5]);
            Vertices[22] = new ExtendedVertex(btmRightBack, Color.White, Color.White, UVs.Uvs[22], UVs.Bounds[5]);
            Vertices[23] = new ExtendedVertex(topRightBack, Color.White, Color.White, UVs.Uvs[23], UVs.Bounds[5]);

            /* 
             *  20. . .23
                .     . .
                .   .   .
                .  .    .
                21. . .22
             */
            Indexes[30] = 20;
            Indexes[31] = 21;
            Indexes[32] = 23;
            Indexes[33] = 23;
            Indexes[34] = 21;
            Indexes[35] = 22;

            /* 
             *  20. . .23
                . .     .
                .   .   .
                .     . .
                21. . .22
             */
            FlippedIndexes[30] = 20;
            FlippedIndexes[31] = 21;
            FlippedIndexes[32] = 22;
            FlippedIndexes[33] = 23;
            FlippedIndexes[34] = 20;
            FlippedIndexes[35] = 22;

            IndexBuffer = new IndexBuffer(GameState.Game.GraphicsDevice, typeof (ushort), Indexes.Length,
                BufferUsage.WriteOnly);
            IndexBuffer.SetData(Indexes);
        }

        /// <summary>
        ///     Represents a set of UV coordinates associated with a textured box.
        /// </summary>
        public class BoxTextureCoords
        {
            /// <summary>
            ///     The position of the back face's UV coordinates (as a sprite sheet grid position)
            /// </summary>
            public Point Back;

            /// <summary>
            ///     The position of the bottom face's UV coordinates (as a sprite sheet grid position)
            /// </summary>
            public Point Bottom;

            /// <summary>
            ///     Bounding boxes in normalized coordinates for each of the 6 faces.
            /// </summary>
            public Vector4[] Bounds = new Vector4[6];

            /// <summary>
            ///     The position of the front face's UV coordinates (as a sprite sheet grid position)
            /// </summary>
            public Point Front;

            /// <summary>
            ///     The position of the left face's UV coordinates (as a sprite sheet grid position)
            /// </summary>
            public Point Left;

            /// <summary>
            ///     The position of the right face's UV coordinates (as a sprite sheet grid position)
            /// </summary>
            public Point Right;

            /// <summary>
            ///     Normalizng factors for each of the 6 faces. That is, a number of pixels
            ///     times this value is the normalized UV coordinate.
            /// </summary>
            public Vector2[] Scales = new Vector2[6];

            /// <summary>
            ///     The position of the top face's UV coordinates (as a sprite sheet grid position)
            /// </summary>
            public Point Top;

            /// <summary>
            ///     List of normalized UV coordinates for each vertex.
            /// </summary>
            public Vector2[] Uvs = new Vector2[NumVertices];

            /// <summary>
            ///     Heigh of the texture in pixels.
            /// </summary>
            public int m_texHeight;

            /// <summary>
            ///     Width of the texture in pixels.
            /// </summary>
            public int m_texWidth;

            public BoxTextureCoords()
            {
            }

            /// <summary>
            ///     Creates a new box UV coordinates.
            /// </summary>
            /// <param name="totalTextureWidth">The width of the texture in pixels.</param>
            /// <param name="totalTextureHeight">The height of the texture in pixels</param>
            /// <param name="front">Front (+z) face data</param>
            /// <param name="back">Back (-z) face data</param>
            /// <param name="top">Top (+y) face data</param>
            /// <param name="bottom">Bottom (-y) face data</param>
            /// <param name="left">Left (-x) face data</param>
            /// <param name="right">Right (+x) face data</param>
            public BoxTextureCoords(int totalTextureWidth, int totalTextureHeight,
                FaceData front, FaceData back, FaceData top, FaceData bottom, FaceData left, FaceData right)
            {
                m_texWidth = totalTextureWidth;
                m_texHeight = totalTextureHeight;

                var textureTopLeft = new Vector2(1.0f, 0.0f);
                var textureTopRight = new Vector2(0.0f, 0.0f);
                var textureBottomLeft = new Vector2(1.0f, 1.0f);
                var textureBottomRight = new Vector2(0.0f, 1.0f);

                var cells = new List<FaceData>
                {
                    front,
                    back,
                    top,
                    bottom,
                    left,
                    right
                };

                int i = 0;
                foreach (FaceData face in cells)
                {
                    Scales[i] = new Vector2(face.Rect.Width/(float) totalTextureWidth,
                        face.Rect.Height/(float) totalTextureHeight);
                    i++;
                }

                var baseCoords = new List<Vector2>
                {
                    // front
                    textureTopLeft,
                    textureBottomLeft,
                    textureBottomRight,
                    textureTopRight,

                    // back
                    textureTopLeft,
                    textureBottomLeft,
                    textureBottomRight,
                    textureTopRight,
                    
                    // top
                    textureTopLeft,
                    textureBottomLeft,
                    textureBottomRight,
                    textureTopRight,

                    // bottom
                    textureTopLeft,
                    textureBottomLeft,
                    textureBottomRight,
                    textureTopRight,

                    // left
                    textureTopLeft,
                    textureBottomLeft,
                    textureBottomRight,
                    textureTopRight,
                    
                    // right
                    textureTopLeft,
                    textureBottomLeft,
                    textureBottomRight,
                    textureTopRight,
                };


                for (int face = 0; face < 6; face++)
                {
                    var pixelCoords = new Vector2(cells[face].Rect.X, cells[face].Rect.Y);
                    float normalizeX = cells[face].Rect.Width/(float) (totalTextureWidth);
                    float normalizeY = cells[face].Rect.Height/(float) (totalTextureHeight);
                    var normalizedCoords = new Vector2(pixelCoords.X/totalTextureWidth, pixelCoords.Y/totalTextureHeight);
                    Bounds[face] = new Vector4(normalizedCoords.X + 0.001f, normalizedCoords.Y + 0.001f,
                        normalizedCoords.X + normalizeX - 0.001f, normalizedCoords.Y + normalizeY - 0.001f);


                    for (int vert = 0; vert < 4; vert++)
                    {
                        int index = vert + face*4;

                        if (cells[face].FlipXY)
                        {
                            Uvs[index] = new Vector2(normalizedCoords.X + baseCoords[index].Y*normalizeX,
                                normalizedCoords.Y + baseCoords[index].X*normalizeY);
                        }
                        else
                        {
                            Uvs[index] = new Vector2(normalizedCoords.X + baseCoords[index].X*normalizeX,
                                normalizedCoords.Y + baseCoords[index].Y*normalizeY);
                        }
                    }
                }
            }

            /// <summary>
            ///     Create new box UV coordinates.
            /// </summary>
            /// <param name="totalTextureWidth">Widh of the texture in pixels</param>
            /// <param name="totalTextureHeight">Height of the texture in pixels</param>
            /// <param name="cellWidth">Width of a sprite sheet cell in pixels</param>
            /// <param name="cellHeight">Height of a sprite sheet cell in pixels</param>
            /// <param name="front">Front cell</param>
            /// <param name="back">Back cell</param>
            /// <param name="top">Top cell</param>
            /// <param name="bottom">Bottom cell</param>
            /// <param name="left">Left cell</param>
            /// <param name="right">Right cell</param>
            public BoxTextureCoords(int totalTextureWidth, int totalTextureHeight,
                int cellWidth, int cellHeight,
                Point front, Point back,
                Point top, Point bottom,
                Point left, Point right)
                : this(
                    totalTextureWidth, totalTextureHeight, new FaceData(front.X, front.Y, cellWidth),
                    new FaceData(back.X, back.Y, cellWidth), new FaceData(top.X, top.Y, cellWidth),
                    new FaceData(bottom.X, bottom.Y, cellWidth), new FaceData(left.X, left.Y, cellWidth),
                    new FaceData(right.X, right.Y, cellWidth))
            {
            }
        }

        /// <summary>
        ///     A box has 6 faces. Each face has texture coordinates in a rectangle on a texture.
        ///     Sometimes we want to transpose the texture coordinates so that X and Y are flipped.
        ///     Why do we do this? So that we can flip textures around to align them without duplicating.
        ///     This only necessary sometimes.
        /// </summary>
        public class FaceData
        {
            /// <summary>
            ///     Creates a new FaceData.
            /// </summary>
            /// <param name="x">The x coordinate in a spritesheet grid of the face.</param>
            /// <param name="y">The y coordinate in a spritesheet grid of the face.</param>
            /// <param name="tileSize">The size of spritesheet tiles in pixels.</param>
            public FaceData(int x, int y, int tileSize)
            {
                Rect = new Rectangle(x*tileSize, y*tileSize, tileSize, tileSize);
                FlipXY = false;
            }

            /// <summary>
            ///     Create a new FaceData
            /// </summary>
            /// <param name="rect">The rectangle on the texture that the face corresponds to.</param>
            public FaceData(Rectangle rect)
            {
                Rect = rect;
                FlipXY = false;
            }

            /// <summary>
            ///     Creates a new FaceData
            /// </summary>
            /// <param name="rect">The rectangle on the texture that the face corresponds to.</param>
            /// <param name="flip">If true, the X and Y coords are transposed.</param>
            public FaceData(Rectangle rect, bool flip)
            {
                Rect = rect;
                FlipXY = flip;
            }

            /// <summary>
            ///     Rectangle in the image that this face corresponds to.
            /// </summary>
            public Rectangle Rect { get; set; }

            /// <summary>
            ///     If true, the X and Y coordinates will be transposed, rotating the texture.
            /// </summary>
            public bool FlipXY { get; set; }
        }
    }
}