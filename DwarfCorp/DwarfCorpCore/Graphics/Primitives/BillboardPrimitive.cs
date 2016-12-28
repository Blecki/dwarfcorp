using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     A billboard is a sprite drawn in the world space, and can be made to
    ///     face the camera constantly. Almost everything in the game is displayed using billboards
    ///     in one way or another.
    /// </summary>
    public sealed class BillboardPrimitive : GeometricPrimitive
    {
        public const int NumVertices = 4;

        public BillboardPrimitive()
        {
        }

        /// <summary>
        ///     Create a billboard primitive.
        /// </summary>
        /// <param name="spriteSheet">The texture of the billboard.</param>
        /// <param name="frameWidth">Width of the sub-texture grid in pixels.</param>
        /// <param name="frameHeight">Height of the sub-texture grid in pixels.</param>
        /// <param name="frame">(x, y) grid coordinate of the subtexture</param>
        /// <param name="width">Width of the billboard in voxels</param>
        /// <param name="height">Height of the billboard in voxels</param>
        /// <param name="tint">Vertex color tint of the billboard.</param>
        /// <param name="flipped">If true, flips the texture horizontally.</param>
        public BillboardPrimitive(Texture2D spriteSheet, int frameWidth, int frameHeight,
            Point frame, float width, float height, Color tint, bool flipped = false)
        {
            UVs = new BoardTextureCoords(spriteSheet.Width, spriteSheet.Height, frameWidth, frameHeight, frame, flipped);
            Width = width;
            Height = height;
            CreateVerticies(Color.White, tint);
        }

        /// <summary>
        ///     UV coordinates of the billboard.
        /// </summary>
        public BoardTextureCoords UVs { get; set; }

        /// <summary>
        ///     Width of the billboard in voxels.
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        ///     Height of the billboard in voxels.
        /// </summary>
        public float Height { get; set; }


        /// <summary>
        ///     Set the vertex color of the billboard using the color passed in.
        /// </summary>
        /// <param name="color">The vertex color of the billboard.</param>
        /// <param name="tint">The lightmap color of the billboard.</param>
        /// <param name="graphics">Graphcis device to use to reset the vertex buffer.</param>
        public void ResetColor(Color color, Color tint, GraphicsDevice graphics)
        {
            CreateVerticies(color, tint);
            ResetBuffer(graphics);
        }

        /// <summary>
        ///     Reset the UV coordinates of the vertex buffer.
        /// </summary>
        public void UpdateVertexUvs()
        {
            for (int i = 0; i < NumVertices; i++)
            {
                Vertices[i].TextureCoordinate = UVs.UVs[i];
                Vertices[i].TextureBounds = UVs.Bounds;
            }
            if (VertexBuffer == null)
                ResetBuffer(GameState.Game.GraphicsDevice);
            else
            {
                VertexBuffer.SetData(Vertices);
            }
        }


        /// <summary>
        ///     Create the vertex buffer.
        /// </summary>
        /// <param name="color">The light map tint of the billboard.</param>
        /// <param name="vertColor">The vertex color of the billboard.</param>
        public void CreateVerticies(Color color, Color vertColor)
        {
            var topLeftFront = new Vector3(-0.5f*Width, 0.5f*Height, 0.0f);
            var topRightFront = new Vector3(0.5f*Width, 0.5f*Height, 0.0f);
            var btmLeftFront = new Vector3(-0.5f*Width, -0.5f*Height, 0.0f);
            var btmRightFront = new Vector3(0.5f*Width, -0.5f*Height, 0.0f);

            Vertices = new[]
            {
                new ExtendedVertex(topLeftFront, color, vertColor, UVs.UVs[0], UVs.Bounds), // 0
                new ExtendedVertex(btmLeftFront, color, vertColor, UVs.UVs[1], UVs.Bounds), // 1
                new ExtendedVertex(topRightFront, color, vertColor, UVs.UVs[2], UVs.Bounds), // 2
                new ExtendedVertex(btmRightFront, color, vertColor, UVs.UVs[3], UVs.Bounds) // 3
            };

            Indexes = new ushort[]
            {
                1, 0, 2,
                1, 2, 3
            };
        }

        /// <summary>
        ///     UV coordinates of a billboard.
        /// </summary>
        public class BoardTextureCoords
        {
            /// <summary>
            ///     Bounding box of the billboard.
            /// </summary>
            public Vector4 Bounds = new Vector4(0, 0, 0, 0);

            /// <summary>
            ///     2D coordinates of the billboard in a texture.
            /// </summary>
            public Vector2[] UVs = new Vector2[NumVertices];

            public BoardTextureCoords()
            {
            }

            /// <summary>
            ///     Create texture coordinate for a billboard. This is done by considering a sub-rectangle
            ///     inside a texture and generating relative UV texture coords for that sub-rectangle.
            /// </summary>
            /// <param name="totalTextureWidth">Width of the billboard supertexture in pixels.</param>
            /// <param name="totalTextureHeight">Height of the billboard supertexture in pixels.</param>
            /// <param name="cellWidth">Width of the billboard subtexture in pixels.</param>
            /// <param name="cellHeight">Height of the billboard subtexture in pixels.</param>
            /// <param name="cell">Position of the billboard subtexture in grid coords.</param>
            /// <param name="flipped">If true, flip the billboard texcoords horizontally.</param>
            public BoardTextureCoords(int totalTextureWidth, int totalTextureHeight,
                int cellWidth, int cellHeight,
                Point cell, bool flipped)
            {
                var textureTopLeft = new Vector2(1.0f, 0.0f);
                var textureTopRight = new Vector2(0.0f, 0.0f);
                var textureBottomLeft = new Vector2(1.0f, 1.0f);
                var textureBottomRight = new Vector2(0.0f, 1.0f);


                if (flipped)
                {
                    textureTopRight = new Vector2(1.0f, 0.0f);
                    textureTopLeft = new Vector2(0.0f, 0.0f);
                    textureBottomRight = new Vector2(1.0f, 1.0f);
                    textureBottomLeft = new Vector2(0.0f, 1.0f);
                }

                float normalizeX = cellWidth/(float) (totalTextureWidth);
                float normalizeY = cellHeight/(float) (totalTextureHeight);


                var baseCoords = new List<Vector2>
                {
                    textureTopLeft,
                    textureBottomLeft,
                    textureTopRight,
                    textureBottomRight,
                };


                var pixelCoords = new Vector2(cell.X*cellWidth, cell.Y*cellHeight);
                var normalizedCoords = new Vector2(pixelCoords.X/totalTextureWidth,
                    pixelCoords.Y/totalTextureHeight);

                // Put a tiny amount of padding on the bounds for antialiasing not to bleed over.
                Bounds = new Vector4(normalizedCoords.X + 0.001f, normalizedCoords.Y + 0.001f,
                    normalizedCoords.X + normalizeX - 0.001f, normalizedCoords.Y + normalizeY - 0.001f);

                for (int vert = 0; vert < NumVertices; vert++)
                {
                    UVs[vert] = new Vector2(normalizedCoords.X + baseCoords[vert].X*normalizeX,
                        normalizedCoords.Y + baseCoords[vert].Y*normalizeY);
                }
            }
        }
    }
}