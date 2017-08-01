using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// Specifies one of the 6 faces of a box.
    /// </summary>
    public enum BoxFace
    {
        Top = 0,
        Bottom = 1,
        Left = 2,
        Right = 3,
        Front = 4,
        Back = 5
    }


   public sealed class OldBoxPrimitive : GeometricPrimitive
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Depth { get; set; }
        public BoxTextureCoords UVs { get; set; }

        private const int NumFaces = 6;
        private const int NumVertices = 36;

        public BoundingBox BoundingBox;

        public class FaceData
        {
            public Rectangle Rect { get; set; }
            public bool FlipXY { get; set; }

            public FaceData(int x, int y, int tileSize)
            {
                Rect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
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
        }

        public class BoxTextureCoords
        {
            public Vector2[] m_uvs = new Vector2[NumVertices];
            public Point m_front;
            public Point m_back;
            public Point m_top;
            public Point m_left;
            public Point m_right;
            public Point m_bottom;
            public int m_texWidth;
            public int m_texHeight;
            public int m_cellWidth;
            public int m_cellHeight;
            public Vector4[] Bounds = new Vector4[6];

            public BoxTextureCoords()
            {
                
            }

            public BoxTextureCoords(int totalTextureWidth, int totalTextureHeight,
                FaceData front, FaceData back, FaceData top, FaceData bottom, FaceData left, FaceData right)
            {
                m_texWidth = totalTextureWidth;
                m_texHeight = totalTextureHeight;

                Vector2 textureTopLeft = new Vector2(0.0f, 0.0f);
                Vector2 textureTopRight = new Vector2(1.0f, 0.0f);
                Vector2 textureBottomLeft = new Vector2(0.0f, 1.0f);
                Vector2 textureBottomRight = new Vector2(1.0f, 1.0f);

                List<FaceData> cells = new List<FaceData>
                {
                    front,
                    back,
                    top,
                    bottom,
                    left,
                    right
                };


                List<Vector2> baseCoords = new List<Vector2>
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


                for(int face = 0; face < 6; face++)
                {
                    Vector2 pixelCoords = new Vector2(cells[face].Rect.X, cells[face].Rect.Y);
                    float normalizeX = (float) (cells[face].Rect.Width) / (float) (totalTextureWidth);
                    float normalizeY = (float) (cells[face].Rect.Height) / (float) (totalTextureHeight);
                    Vector2 normalizedCoords = new Vector2(pixelCoords.X / (float) totalTextureWidth, pixelCoords.Y / (float) totalTextureHeight);
                    Bounds[face] = new Vector4(normalizedCoords.X + 0.001f, normalizedCoords.Y + 0.001f, normalizedCoords.X + normalizeX - 0.001f, normalizedCoords.Y + normalizeY - 0.001f);


                    for(int vert = 0; vert < 6; vert++)
                    {
                        int index = vert + face * 6;
      
                        if(!cells[face].FlipXY)
                        {
                            m_uvs[index] = new Vector2(normalizedCoords.X + baseCoords[index].Y * normalizeX, normalizedCoords.Y + baseCoords[index].X * normalizeY);
                        }
                        else
                        {
                            m_uvs[index] = new Vector2(normalizedCoords.X + baseCoords[index].X * normalizeX, normalizedCoords.Y + baseCoords[index].Y * normalizeY);
                        }
                    }
                }
            }

            public BoxTextureCoords(int totalTextureWidth, int totalTextureHeight,
                int cellWidth, int cellHeight,
                Point front, Point back,
                Point top, Point bottom,
                Point left, Point right) :this(totalTextureWidth, totalTextureHeight, new FaceData(front.X, front.Y, cellWidth), new FaceData(back.X, back.Y, cellWidth), new FaceData(top.X, top.Y, cellWidth), new FaceData(bottom.X, bottom.Y, cellWidth), new FaceData(left.X, left.Y, cellWidth), new FaceData(right.X, right.Y, cellWidth)    )
            {
            }
        }


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
            Vector3 topLeftFront = new Vector3(0.0f, Height, 0.0f);
            Vector3 topLeftBack = new Vector3(0.0f, Height, Depth);
            Vector3 topRightFront = new Vector3(Width, Height, 0.0f);
            Vector3 topRightBack = new Vector3(Width, Height, Depth);
            

            // Calculate the position of the vertices on the bottom face.
            Vector3 btmLeftFront = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 btmLeftBack = new Vector3(0.0f, 0.0f, Depth);
            Vector3 btmRightFront = new Vector3(Width, 0.0f, 0.0f);
            Vector3 btmRightBack = new Vector3(Width, 0.0f, Depth);

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
            Vertices[24] = new ExtendedVertex(topLeftFront, Color.White,  Color.White, UVs.m_uvs[24], UVs.Bounds[4]);
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
    }

    /// <summary>
    /// A box primitive is just a simple textured rectangular box.
    /// </summary>
    [JsonObject(IsReference = true)]
    public sealed class BoxPrimitive : GeometricPrimitive
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Depth { get; set; }
        public BoxTextureCoords UVs { get; set; }

        private const int NumVertices = 24;

        public VoxelVertex[] Deltas { get; set; }

        public BoundingBox BoundingBox;

        public ushort[] FlippedIndexes = null;

        public class FaceData
        {
            public Rectangle Rect { get; set; }

            public FaceData(int x, int y, int tileSize)
            {
                Rect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
            }

            public FaceData(Rectangle rect)
            {
                Rect = rect;
            }


        }

        public class BoxTextureCoords
        {
            public Vector2[] Uvs = new Vector2[NumVertices];
            public Point Front;
            public Point Back;
            public Point Top;
            public Point Left;
            public Point Right;
            public Point Bottom;
            public int m_texWidth;
            public int m_texHeight;
            public Vector4[] Bounds = new Vector4[6];
            public Vector2[] Scales = new Vector2[6];
            public BoxTextureCoords()
            {
                
            }

            public BoxTextureCoords(int totalTextureWidth, int totalTextureHeight,
                FaceData front, FaceData back, FaceData top, FaceData bottom, FaceData left, FaceData right)
            {
                m_texWidth = totalTextureWidth;
                m_texHeight = totalTextureHeight;

                Vector2 textureTopLeft = new Vector2(1.0f, 0.0f);
                Vector2 textureTopRight = new Vector2(0.0f, 0.0f);
                Vector2 textureBottomLeft = new Vector2(1.0f, 1.0f);
                Vector2 textureBottomRight = new Vector2(0.0f, 1.0f);

                List<FaceData> cells = new List<FaceData>
                {
                    front,
                    back,
                    top,
                    bottom,
                    left,
                    right
                };

                List<Vector2> baseCoords = new List<Vector2>
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


                for(int face = 0; face < 6; face++)
                {
                    Vector2 pixelCoords = new Vector2(cells[face].Rect.X, cells[face].Rect.Y);
                    float normalizeX = (float) (cells[face].Rect.Width) / (float) (totalTextureWidth);
                    float normalizeY = (float) (cells[face].Rect.Height) / (float) (totalTextureHeight);
                    Vector2 normalizedCoords = new Vector2(pixelCoords.X / (float) totalTextureWidth, pixelCoords.Y / (float) totalTextureHeight);
                    Bounds[face] = new Vector4(normalizedCoords.X + 0.001f, normalizedCoords.Y + 0.001f, normalizedCoords.X + normalizeX - 0.001f, normalizedCoords.Y + normalizeY - 0.001f);


                    for(int vert = 0; vert < 4; vert++)
                    {
                        int index = vert + face * 4;
                        Uvs[index] = new Vector2(normalizedCoords.X + baseCoords[index].X * normalizeX, normalizedCoords.Y + baseCoords[index].Y * normalizeY);
                    }
                }
            }

            public BoxTextureCoords(int totalTextureWidth, int totalTextureHeight,
                int cellWidth, int cellHeight,
                Point front, Point back,
                Point top, Point bottom,
                Point left, Point right) :this(totalTextureWidth, totalTextureHeight, new FaceData(front.X, front.Y, cellWidth), new FaceData(back.X, back.Y, cellWidth), new FaceData(top.X, top.Y, cellWidth), new FaceData(bottom.X, bottom.Y, cellWidth), new FaceData(left.X, left.Y, cellWidth), new FaceData(right.X, right.Y, cellWidth)    )
            {
            }
        }


        public BoxPrimitive(GraphicsDevice device, float width, float height, float depth, BoxTextureCoords uvs)
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
                Deltas[i] = GetNearestDelta(Vertices[i].Position);
            }
        }

        private static Vector3[] vertexDeltas = null;

        private static void InitializeDeltas()
        {
            if (vertexDeltas != null) return;

            vertexDeltas = new Vector3[8];

            vertexDeltas[(int)VoxelVertex.BackBottomLeft] = new Vector3(0, 0, 0);
            vertexDeltas[(int)VoxelVertex.BackTopLeft] = new Vector3(0, 1.0f, 0);
            vertexDeltas[(int)VoxelVertex.BackBottomRight] = new Vector3(1.0f, 0, 0);
            vertexDeltas[(int)VoxelVertex.BackTopRight] = new Vector3(1.0f, 1.0f, 0);

            vertexDeltas[(int)VoxelVertex.FrontBottomLeft] = new Vector3(0, 0, 1.0f);
            vertexDeltas[(int)VoxelVertex.FrontTopLeft] = new Vector3(0, 1.0f, 1.0f);
            vertexDeltas[(int)VoxelVertex.FrontBottomRight] = new Vector3(1.0f, 0, 1.0f);
            vertexDeltas[(int)VoxelVertex.FrontTopRight] = new Vector3(1.0f, 1.0f, 1.0f);
        }

        private static VoxelVertex GetNearestDelta(Vector3 position)
        {
            InitializeDeltas();

            float bestDist = float.MaxValue;
            VoxelVertex bestKey = 0;
            for (int i = 0; i < 8; i++)
            {
                float dist = (position - vertexDeltas[i]).LengthSquared();
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestKey = (VoxelVertex)(i);
                }
            }

            return bestKey;
        }

        public override void Render(GraphicsDevice device)
        {
            base.Render(device);
        }

        public struct FaceDescriptor
        {
            public int IndexOffset;
            public int IndexCount;
            public int VertexOffset;
            public int VertexCount;
        }

        public FaceDescriptor GetFace(BoxFace face)
        {
            var r = new FaceDescriptor
            {
                VertexCount = 4,
                IndexCount = 6
            };
            // Todo: Put the underlying vertex data in the right order and this is just some multiplies.
            switch (face)
            {
                case BoxFace.Top:
                    r.IndexOffset = 12;
                    r.VertexOffset = 8;
                    return r;
                case BoxFace.Bottom:
                    r.IndexOffset = 18;
                    r.VertexOffset = 12;
                    return r;
                case BoxFace.Left:
                    r.IndexOffset = 24;
                    r.VertexOffset = 16;
                    return r;
                case BoxFace.Right:
                    r.IndexOffset = 30;
                    r.VertexOffset = 20;
                    return r;
                case BoxFace.Front:
                    r.IndexOffset = 0;
                    r.VertexOffset = 0;
                    return r;
                case BoxFace.Back:
                    r.IndexOffset = 6;
                    r.VertexOffset = 4;
                    return r;
            }

            r.IndexOffset = 0;
            r.VertexOffset = 0;
            return r;
        }


        private void CreateVerticies()
        {
            Indexes = new ushort[36];
            FlippedIndexes = new ushort[36];
            Vertices = new ExtendedVertex[NumVertices];

            // Calculate the position of the vertices on the top face.
            Vector3 topLeftFront = new Vector3(0.0f, Height, 0.0f);
            Vector3 topLeftBack = new Vector3(0.0f, Height, Depth);
            Vector3 topRightFront = new Vector3(Width, Height, 0.0f);
            Vector3 topRightBack = new Vector3(Width, Height, Depth);

            // Calculate the position of the vertices on the bottom face.
            Vector3 btmLeftFront = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 btmLeftBack = new Vector3(0.0f, 0.0f, Depth);
            Vector3 btmRightFront = new Vector3(Width, 0.0f, 0.0f);
            Vector3 btmRightBack = new Vector3(Width, 0.0f, Depth);

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
            Vertices[4] = new ExtendedVertex(topRightBack, Color.White, Color.White, UVs.Uvs[4], UVs.Bounds[1]);
            Vertices[5] = new ExtendedVertex(btmRightBack, Color.White, Color.White, UVs.Uvs[5], UVs.Bounds[1]);
            Vertices[6] = new ExtendedVertex(btmLeftBack, Color.White, Color.White, UVs.Uvs[6], UVs.Bounds[1]);
            Vertices[7] = new ExtendedVertex(topLeftBack, Color.White, Color.White, UVs.Uvs[7], UVs.Bounds[1]);


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
            Vertices[8] = new ExtendedVertex(topLeftBack, Color.White,Color.White, UVs.Uvs[8], UVs.Bounds[2]);
            Vertices[9] = new ExtendedVertex(topLeftFront, Color.White,Color.White, UVs.Uvs[9], UVs.Bounds[2]);
            Vertices[10] = new ExtendedVertex(topRightFront, Color.White, Color.White, UVs.Uvs[10], UVs.Bounds[2]);
            Vertices[11] = new ExtendedVertex(topRightBack, Color.White,Color.White, UVs.Uvs[11], UVs.Bounds[2]);
       

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
            Vertices[12] = new ExtendedVertex(btmLeftFront, Color.White,Color.White, UVs.Uvs[12], UVs.Bounds[3]);
            Vertices[13] = new ExtendedVertex(btmLeftBack, Color.White,Color.White, UVs.Uvs[13], UVs.Bounds[3]);
            Vertices[14] = new ExtendedVertex(btmRightBack, Color.White,Color.White, UVs.Uvs[14], UVs.Bounds[3]);
            Vertices[15] = new ExtendedVertex(btmRightFront, Color.White,Color.White, UVs.Uvs[15], UVs.Bounds[3]);


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
            Vertices[16] = new ExtendedVertex(topLeftBack, Color.White,Color.White, UVs.Uvs[16], UVs.Bounds[4]);
            Vertices[17] = new ExtendedVertex(btmLeftBack, Color.White, Color.White, UVs.Uvs[17], UVs.Bounds[4]);
            Vertices[18] = new ExtendedVertex(btmLeftFront, Color.White,Color.White, UVs.Uvs[18], UVs.Bounds[4]);
            Vertices[19] = new ExtendedVertex(topLeftFront, Color.White,Color.White, UVs.Uvs[19], UVs.Bounds[4]);

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
            Vertices[20] = new ExtendedVertex(topRightFront, Color.White,Color.White, UVs.Uvs[20], UVs.Bounds[5]);
            Vertices[21] = new ExtendedVertex(btmRightFront, Color.White,Color.White, UVs.Uvs[21], UVs.Bounds[5]);
            Vertices[22] = new ExtendedVertex(btmRightBack, Color.White,Color.White, UVs.Uvs[22], UVs.Bounds[5]);
            Vertices[23] = new ExtendedVertex(topRightBack, Color.White,Color.White, UVs.Uvs[23], UVs.Bounds[5]);

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

            IndexBuffer = new IndexBuffer(GameState.Game.GraphicsDevice, typeof(ushort), Indexes.Length, BufferUsage.WriteOnly);
            IndexBuffer.SetData(Indexes);
        }
    }

}