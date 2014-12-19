using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// A billboard is a sprite drawn in the world space, and can be made to
    /// face the camera constantly.
    /// </summary>
    public sealed class BillboardPrimitive : GeometricPrimitive
    {
        public const int NumVertices = 6;

        public class BoardTextureCoords
        {
            public Vector2[] UVs = new Vector2[NumVertices];
            public Vector4 Bounds = new Vector4(0, 0, 0, 0);

            public BoardTextureCoords()
            {
                
            }

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

                float normalizeX = cellWidth / (float) (totalTextureWidth);
                float normalizeY = cellHeight / (float) (totalTextureHeight);


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

                    for(int vert = 0; vert < NumVertices; vert++)
                    {
                        int index = vert + face * 6;
                        UVs[index] = new Vector2(normalizedCoords.X + baseCoords[index].X * normalizeX, normalizedCoords.Y + baseCoords[index].Y * normalizeY);
                    }
                }
            }
        }

        public BoardTextureCoords UVs { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }


        public BillboardPrimitive()
        {
            
        }

        public BillboardPrimitive(GraphicsDevice device, Texture2D spriteSheet, int frameWidth, int frameHeight, Point frame, float width, float height, bool flipped = false)
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

        public void UpdateVertexUvs()
        {
            for(int i = 0; i < NumVertices; i++)
            {
                Vertices[i].TextureCoordinate = UVs.UVs[i];
                Vertices[i].TextureBounds = UVs.Bounds;
            }

         
            VertexBuffer.SetData(Vertices);
        }

        public void CreateVerticies(Color color)
        {
            // Calculate the position of the vertices on the top face.
            Vector3 topLeftFront = new Vector3(-0.5f * Width, 0.5f * Height, 0.0f);
            Vector3 topRightFront = new Vector3(0.5f * Width, 0.5f * Height, 0.0f);

            // Calculate the position of the vertices on the bottom face.
            Vector3 btmLeftFront = new Vector3(-0.5f * Width, -0.5f * Height, 0.0f);
            Vector3 btmRightFront = new Vector3(0.5f * Width, -0.5f * Height, 0.0f);


            // Add the vertices for the FRONT face.
            Vertices = new[]
            {
                new ExtendedVertex(topLeftFront, color, UVs.UVs[0], UVs.Bounds),
                new ExtendedVertex(btmLeftFront, color, UVs.UVs[1], UVs.Bounds),
                new ExtendedVertex(topRightFront, color, UVs.UVs[2], UVs.Bounds),
                new ExtendedVertex(btmLeftFront, color, UVs.UVs[3], UVs.Bounds),
                new ExtendedVertex(btmRightFront, color, UVs.UVs[4], UVs.Bounds),
                new ExtendedVertex(topRightFront, color, UVs.UVs[5], UVs.Bounds)
            };

        }
    }

}