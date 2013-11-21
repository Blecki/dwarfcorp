using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    public sealed class BillboardPrimitive : GeometricPrimitive
    {
        private Texture2D SpriteSheet { get; set; }

        public const int NUM_VERTICES = 6;

        public class BoardTextureCoords
        {
            public Vector2[] m_uvs = new Vector2[BillboardPrimitive.NUM_VERTICES];
            public Vector4 Bounds = new Vector4(0, 0, 0, 0);

            public BoardTextureCoords(int totalTextureWidth, int totalTextureHeight,
                int cellWidth, int cellHeight,
                Point cell, bool flipped)
            {
                Vector2 textureTopLeft = new Vector2(1.0f, 0.0f);
                Vector2 textureTopRight = new Vector2(0.0f, 0.0f);
                Vector2 textureBottomLeft = new Vector2(1.0f, 1.0f);
                Vector2 textureBottomRight = new Vector2(0.0f, 1.0f);


                if(flipped)
                {
                    textureTopRight = new Vector2(1.0f, 0.0f);
                    textureTopLeft = new Vector2(0.0f, 0.0f);
                    textureBottomRight = new Vector2(1.0f, 1.0f);
                    textureBottomLeft = new Vector2(0.0f, 1.0f);
                }

                List<Point> cells = new List<Point>
                {
                    cell
                };

                float normalizeX = (float) (cellWidth) / (float) (totalTextureWidth);
                float normalizeY = (float) (cellHeight) / (float) (totalTextureHeight);


                List<Vector2> baseCoords = new List<Vector2>
                {
                    textureTopLeft,
                    textureBottomLeft,
                    textureTopRight,
                    textureBottomLeft,
                    textureBottomRight,
                    textureTopRight
                };


                for(int face = 0; face < 1; face++)
                {
                    Vector2 pixelCoords = new Vector2(cells[face].X * cellWidth, cells[face].Y * cellHeight);
                    Vector2 normalizedCoords = new Vector2(pixelCoords.X / (float) totalTextureWidth, pixelCoords.Y / (float) totalTextureHeight);
                    Bounds = new Vector4(normalizedCoords.X + 0.001f, normalizedCoords.Y + 0.001f, normalizedCoords.X + normalizeX - 0.001f, normalizedCoords.Y + normalizeY - 0.001f);

                    for(int vert = 0; vert < NUM_VERTICES; vert++)
                    {
                        int index = vert + face * 6;
                        m_uvs[index] = new Vector2(normalizedCoords.X + baseCoords[index].X * normalizeX, normalizedCoords.Y + baseCoords[index].Y * normalizeY);
                    }
                }
            }
        }

        public BoardTextureCoords UVs { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }


        public BillboardPrimitive(GraphicsDevice device, Texture2D spriteSheet, int frameWidth, int frameHeight, Point frame, float width, float height, bool flipped)
        {
            UVs = new BoardTextureCoords(spriteSheet.Width, spriteSheet.Height, frameWidth, frameHeight, frame, flipped);
            Width = width;
            Height = height;
            CreateVerticies(Color.White);
            ResetBuffer(device);
        }

        public void ResetColor(Color color, GraphicsDevice graphics)
        {
            CreateVerticies(color);
            ResetBuffer(graphics);
        }

        private void CreateVerticies(Color color)
        {
            // Calculate the position of the vertices on the top face.
            Vector3 topLeftFront = new Vector3(-0.5f * Width, 0.5f * Height, 0.0f);
            Vector3 topRightFront = new Vector3(0.5f * Width, 0.5f * Height, 0.0f);

            // Calculate the position of the vertices on the bottom face.
            Vector3 btmLeftFront = new Vector3(-0.5f * Width, -0.5f * Height, 0.0f);
            Vector3 btmRightFront = new Vector3(0.5f * Width, -0.5f * Height, 0.0f);


            // Add the vertices for the FRONT face.
            m_vertices = new ExtendedVertex[NUM_VERTICES];
            m_vertices[0] = new ExtendedVertex(topLeftFront, color, UVs.m_uvs[0], UVs.Bounds);
            m_vertices[1] = new ExtendedVertex(btmLeftFront, color, UVs.m_uvs[1], UVs.Bounds);
            m_vertices[2] = new ExtendedVertex(topRightFront, color, UVs.m_uvs[2], UVs.Bounds);
            m_vertices[3] = new ExtendedVertex(btmLeftFront, color, UVs.m_uvs[3], UVs.Bounds);
            m_vertices[4] = new ExtendedVertex(btmRightFront, color, UVs.m_uvs[4], UVs.Bounds);
            m_vertices[5] = new ExtendedVertex(topRightFront, color, UVs.m_uvs[5], UVs.Bounds);
        }
    }

}