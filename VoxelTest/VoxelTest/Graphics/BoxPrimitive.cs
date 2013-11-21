using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    public enum BoxFace
    {
        Top,
        Bottom,
        Left,
        Right,
        Front,
        Back
    }

    [JsonObject(IsReference = true)]
    public class BoxPrimitive : GeometricPrimitive
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Depth { get; set; }
        public BoxTextureCoords UVs { get; set; }

        private const int NUM_FACES = 6;
        private const int NUM_TRIANGLES = 12;
        private const int NUM_VERTICES = 36;

        public BoundingBox boundingBox;

        public class FaceData
        {
            public Rectangle Rect { get; set; }
            public bool flipXY { get; set; }

            public FaceData(Rectangle rect)
            {
                Rect = rect;
                flipXY = false;
            }

            public FaceData(Rectangle rect, bool flip)
            {
                Rect = rect;
                flipXY = flip;
            }
        }

        public class BoxTextureCoords
        {
            public Vector2[] m_uvs = new Vector2[NUM_VERTICES];
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
            public Vector4[] Bounds = new Vector4[NUM_FACES];


            public BoxTextureCoords(int totalTextureWidth, int totalTextureHeight,
                FaceData front, FaceData back, FaceData top, FaceData bottom, FaceData left, FaceData right)
            {
                m_texWidth = totalTextureWidth;
                m_texHeight = totalTextureHeight;
                m_texWidth = totalTextureWidth;

                Vector2 textureTopLeft = new Vector2(1.0f, 0.0f);
                Vector2 textureTopRight = new Vector2(0.0f, 0.0f);
                Vector2 textureBottomLeft = new Vector2(1.0f, 1.0f);
                Vector2 textureBottomRight = new Vector2(0.0f, 1.0f);

                List<FaceData> cells = new List<FaceData>();
                cells.Add(front);
                cells.Add(back);
                cells.Add(top);
                cells.Add(bottom);
                cells.Add(left);
                cells.Add(right);


                List<Vector2> baseCoords = new List<Vector2>();

                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureTopRight);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopRight);

                baseCoords.Add(textureTopRight);
                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomLeft);

                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureTopRight);
                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopRight);

                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopRight);

                baseCoords.Add(textureTopRight);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureTopRight);

                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopRight);
                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomRight);


                for(int face = 0; face < 6; face++)
                {
                    Vector2 pixelCoords = new Vector2(cells[face].Rect.Top, cells[face].Rect.Left);
                    float normalizeX = (float) (cells[face].Rect.Width) / (float) (totalTextureWidth);
                    float normalizeY = (float) (cells[face].Rect.Height) / (float) (totalTextureHeight);
                    Vector2 normalizedCoords = new Vector2(pixelCoords.X / (float) totalTextureWidth, pixelCoords.Y / (float) totalTextureHeight);

                    Bounds[face] = new Vector4(normalizedCoords.X + 0.001f, normalizedCoords.Y + 0.001f, normalizedCoords.X + normalizeX - 0.001f, normalizedCoords.Y + normalizeY - 0.001f);

                    for(int vert = 0; vert < 6; vert++)
                    {
                        int index = vert + face * 6;

                        if(!cells[face].flipXY)
                        {
                            m_uvs[index] = new Vector2(normalizedCoords.X + baseCoords[index].X * normalizeX, normalizedCoords.Y + baseCoords[index].Y * normalizeY);
                        }
                        else
                        {
                            m_uvs[index] = new Vector2(normalizedCoords.X + baseCoords[index].Y * normalizeX, normalizedCoords.Y + baseCoords[index].X * normalizeY);
                        }
                    }
                }
            }

            public BoxTextureCoords(int totalTextureWidth, int totalTextureHeight,
                int cellWidth, int cellHeight,
                Point front, Point back,
                Point top, Point bottom,
                Point left, Point right)
            {
                m_front = front;
                m_top = top;
                m_bottom = bottom;
                m_left = left;
                m_right = right;
                m_back = back;
                m_texWidth = totalTextureWidth;
                m_texHeight = totalTextureHeight;
                m_texWidth = totalTextureWidth;
                m_cellWidth = cellWidth;
                m_cellHeight = cellHeight;

                Vector2 textureTopLeft = new Vector2(1.0f, 0.0f);
                Vector2 textureTopRight = new Vector2(0.0f, 0.0f);
                Vector2 textureBottomLeft = new Vector2(1.0f, 1.0f);
                Vector2 textureBottomRight = new Vector2(0.0f, 1.0f);

                List<Point> cells = new List<Point>();
                cells.Add(front);
                cells.Add(back);
                cells.Add(top);
                cells.Add(bottom);
                cells.Add(left);
                cells.Add(right);

                float normalizeX = (float) (cellWidth) / (float) (totalTextureWidth);
                float normalizeY = (float) (cellHeight) / (float) (totalTextureHeight);


                List<Vector2> baseCoords = new List<Vector2>();

                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureTopRight);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopRight);

                baseCoords.Add(textureTopRight);
                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomLeft);

                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureTopRight);
                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopRight);

                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopRight);

                baseCoords.Add(textureTopRight);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureTopRight);

                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomLeft);
                baseCoords.Add(textureBottomRight);
                baseCoords.Add(textureTopRight);
                baseCoords.Add(textureTopLeft);
                baseCoords.Add(textureBottomRight);


                for(int face = 0; face < 6; face++)
                {
                    Vector2 pixelCoords = new Vector2(cells[face].X * cellWidth, cells[face].Y * cellHeight);
                    Vector2 normalizedCoords = new Vector2(pixelCoords.X / (float) totalTextureWidth, pixelCoords.Y / (float) totalTextureHeight);

                    Bounds[face] = new Vector4(normalizedCoords.X + 0.001f, normalizedCoords.Y + 0.001f, normalizedCoords.X + normalizeX - 0.001f, normalizedCoords.Y + normalizeY - 0.001f);

                    for(int vert = 0; vert < 6; vert++)
                    {
                        int index = vert + face * 6;
                        m_uvs[index] = new Vector2(normalizedCoords.X + baseCoords[index].X * normalizeX, normalizedCoords.Y + baseCoords[index].Y * normalizeY);
                    }
                }
            }
        }


        public BoxPrimitive(GraphicsDevice device, float width, float height, float depth, BoxTextureCoords uvs)
        {
            Width = width;
            Height = height;
            Depth = depth;

            UVs = uvs;
            CreateVerticies();
            ResetBuffer(device);
            boundingBox = new BoundingBox(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(width, height, depth));
        }


        public static BoxPrimitive RetextureTop(BoxPrimitive other, GraphicsDevice graphics, Point newTexture)
        {
            BoxPrimitive.BoxTextureCoords coords =
                new BoxPrimitive.BoxTextureCoords(other.UVs.m_texWidth,
                    other.UVs.m_texHeight, other.UVs.m_cellWidth, other.UVs.m_cellHeight, other.UVs.m_front,
                    other.UVs.m_back, newTexture, other.UVs.m_bottom, other.UVs.m_left, other.UVs.m_right);

            BoxPrimitive toReturn = new BoxPrimitive(graphics, other.Width, other.Height, other.Depth, coords);
            return toReturn;
        }


        public ExtendedVertex[] GetFace(BoxFace face)
        {
            switch(face)
            {
                case BoxFace.Back:
                    return GetBackFace();
                case BoxFace.Front:
                    return GetFrontFace();
                case BoxFace.Left:
                    return GetLeftFace();
                case BoxFace.Right:
                    return GetRightFace();
                case BoxFace.Top:
                    return GetTopFace();
                case BoxFace.Bottom:
                    return GetBottomFace();
            }

            return null;
        }

        public ExtendedVertex[] GetFrontFace()
        {
            return Program.SubArray<ExtendedVertex>(m_vertices, 0, 6);
        }

        public ExtendedVertex[] GetBackFace()
        {
            return Program.SubArray<ExtendedVertex>(m_vertices, 6, 6);
        }

        public ExtendedVertex[] GetTopFace()
        {
            return Program.SubArray<ExtendedVertex>(m_vertices, 12, 6);
        }

        public ExtendedVertex[] GetBottomFace()
        {
            return Program.SubArray<ExtendedVertex>(m_vertices, 18, 6);
        }

        public ExtendedVertex[] GetLeftFace()
        {
            return Program.SubArray<ExtendedVertex>(m_vertices, 24, 6);
        }

        public ExtendedVertex[] GetRightFace()
        {
            return Program.SubArray<ExtendedVertex>(m_vertices, 30, 6);
        }

        protected void CreateVerticies()
        {
            m_vertices = new ExtendedVertex[NUM_VERTICES];

            // Calculate the position of the vertices on the top face.
            Vector3 topLeftFront = new Vector3(0.0f, Height, 0.0f);
            Vector3 topLeftBack = new Vector3(0.0f, Height, Depth);
            Vector3 topRightFront = new Vector3(Width, Height, 0.0f);
            Vector3 topRightBack = new Vector3(Width, Height, Depth);
            ;

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

            m_vertices[0] = new ExtendedVertex(topLeftFront, Color.White, UVs.m_uvs[0], UVs.Bounds[0]);
            m_vertices[1] = new ExtendedVertex(btmLeftFront, Color.White, UVs.m_uvs[1], UVs.Bounds[0]);
            m_vertices[2] = new ExtendedVertex(topRightFront, Color.White, UVs.m_uvs[2], UVs.Bounds[0]);
            m_vertices[3] = new ExtendedVertex(btmLeftFront, Color.White, UVs.m_uvs[3], UVs.Bounds[0]);
            m_vertices[4] = new ExtendedVertex(btmRightFront, Color.White, UVs.m_uvs[4], UVs.Bounds[0]);
            m_vertices[5] = new ExtendedVertex(topRightFront, Color.White, UVs.m_uvs[5], UVs.Bounds[0]);

            // Add the vertices for the BACK face.
            m_vertices[6] = new ExtendedVertex(topLeftBack, Color.White, UVs.m_uvs[6], UVs.Bounds[1]);
            m_vertices[7] = new ExtendedVertex(topRightBack, Color.White, UVs.m_uvs[7], UVs.Bounds[1]);
            m_vertices[8] = new ExtendedVertex(btmLeftBack, Color.White, UVs.m_uvs[8], UVs.Bounds[1]);
            m_vertices[9] = new ExtendedVertex(btmLeftBack, Color.White, UVs.m_uvs[9], UVs.Bounds[1]);
            m_vertices[10] = new ExtendedVertex(topRightBack, Color.White, UVs.m_uvs[10], UVs.Bounds[1]);
            m_vertices[11] = new ExtendedVertex(btmRightBack, Color.White, UVs.m_uvs[11], UVs.Bounds[1]);

            // Add the vertices for the TOP face.
            m_vertices[12] = new ExtendedVertex(topLeftFront, Color.White, UVs.m_uvs[12], UVs.Bounds[2]);
            m_vertices[13] = new ExtendedVertex(topRightBack, Color.White, UVs.m_uvs[13], UVs.Bounds[2]);
            m_vertices[14] = new ExtendedVertex(topLeftBack, Color.White, UVs.m_uvs[14], UVs.Bounds[2]);
            m_vertices[15] = new ExtendedVertex(topLeftFront, Color.White, UVs.m_uvs[15], UVs.Bounds[2]);
            m_vertices[16] = new ExtendedVertex(topRightFront, Color.White, UVs.m_uvs[16], UVs.Bounds[2]);
            m_vertices[17] = new ExtendedVertex(topRightBack, Color.White, UVs.m_uvs[17], UVs.Bounds[2]);

            // Add the vertices for the BOTTOM face. 
            m_vertices[18] = new ExtendedVertex(btmLeftFront, Color.White, UVs.m_uvs[18], UVs.Bounds[3]);
            m_vertices[19] = new ExtendedVertex(btmLeftBack, Color.White, UVs.m_uvs[19], UVs.Bounds[3]);
            m_vertices[20] = new ExtendedVertex(btmRightBack, Color.White, UVs.m_uvs[20], UVs.Bounds[3]);
            m_vertices[21] = new ExtendedVertex(btmLeftFront, Color.White, UVs.m_uvs[21], UVs.Bounds[3]);
            m_vertices[22] = new ExtendedVertex(btmRightBack, Color.White, UVs.m_uvs[22], UVs.Bounds[3]);
            m_vertices[23] = new ExtendedVertex(btmRightFront, Color.White, UVs.m_uvs[23], UVs.Bounds[3]);

            // Add the vertices for the LEFT face.
            m_vertices[24] = new ExtendedVertex(topLeftFront, Color.White, UVs.m_uvs[24], UVs.Bounds[4]);
            m_vertices[25] = new ExtendedVertex(btmLeftBack, Color.White, UVs.m_uvs[25], UVs.Bounds[4]);
            m_vertices[26] = new ExtendedVertex(btmLeftFront, Color.White, UVs.m_uvs[26], UVs.Bounds[4]);
            m_vertices[27] = new ExtendedVertex(topLeftBack, Color.White, UVs.m_uvs[27], UVs.Bounds[4]);
            m_vertices[28] = new ExtendedVertex(btmLeftBack, Color.White, UVs.m_uvs[28], UVs.Bounds[4]);
            m_vertices[29] = new ExtendedVertex(topLeftFront, Color.White, UVs.m_uvs[29], UVs.Bounds[4]);

            // Add the vertices for the RIGHT face. 
            m_vertices[30] = new ExtendedVertex(topRightFront, Color.White, UVs.m_uvs[30], UVs.Bounds[5]);
            m_vertices[31] = new ExtendedVertex(btmRightFront, Color.White, UVs.m_uvs[31], UVs.Bounds[5]);
            m_vertices[32] = new ExtendedVertex(btmRightBack, Color.White, UVs.m_uvs[32], UVs.Bounds[5]);
            m_vertices[33] = new ExtendedVertex(topRightBack, Color.White, UVs.m_uvs[33], UVs.Bounds[5]);
            m_vertices[34] = new ExtendedVertex(topRightFront, Color.White, UVs.m_uvs[34], UVs.Bounds[5]);
            m_vertices[35] = new ExtendedVertex(btmRightBack, Color.White, UVs.m_uvs[35], UVs.Bounds[5]);
        }
    }

}