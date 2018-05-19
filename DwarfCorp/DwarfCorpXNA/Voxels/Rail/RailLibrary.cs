// RailLibrary.cs
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Rail
{
    public class RailLibrary
    {
        private static List<JunctionPattern> Patterns;
        private static List<JunctionPattern> ChainPatterns;
        private static List<RailPiece> Pieces;
        public static CombinationTable CombinationTable;

        private static void Initialize()
        {
            if (Patterns == null)
            {
                Pieces = FileUtils.LoadJsonListFromMultipleSources<RailPiece>(ContentPaths.rail_pieces, null, p => p.Name);
                Patterns = FileUtils.LoadJsonListFromMultipleSources<JunctionPattern>(ContentPaths.rail_patterns, null, p => p.Name);

                CombinationTable = new CombinationTable();
                CombinationTable.LoadConfiguration(ContentPaths.rail_combinations);

                foreach (var piece in Pieces)
                    piece.ComputeConnections();

                for (var i = 0; i < Patterns.Count; ++i)
                    Patterns[i].Icon = i;

                ChainPatterns = Patterns
                    // Only pieces with well defined entrance and exits can be used in chains.
                    .Where(p => p.Entrance != null && p.Exit != null)
                    // We need them in every orientation - not worried about redundancies.
                    .SelectMany(p =>
                    {
                        return new JunctionPattern[]
                        {
                            p.Rotate(PieceOrientation.North),
                            p.Rotate(PieceOrientation.East),
                            p.Rotate(PieceOrientation.South),
                            p.Rotate(PieceOrientation.West)
                        };
                    })
                    // We need them with the endpoints switched as well.
                    .SelectMany(p =>
                    {
                        return new JunctionPattern[]
                        {
                            p,
                            new JunctionPattern
                            {
                                Pieces = p.Pieces,
                                Entrance = p.Exit,
                                Exit = p.Entrance,
                            }
                        };
                    })
                    // And they must be positioned so the entrance is at 0,0
                    .Select(p =>
                    {
                        if (p.Entrance.Offset.X == 0 && p.Entrance.Offset.Y == 0) return p;
                        else return new JunctionPattern
                        {
                            Entrance = new JunctionPortal
                            {
                                Direction = p.Entrance.Direction,
                                Offset = Point.Zero
                            },
                            Exit = new JunctionPortal
                            {
                                Direction = p.Exit.Direction,
                                Offset = new Point(p.Exit.Offset.X - p.Entrance.Offset.X, p.Exit.Offset.Y - p.Entrance.Offset.Y)
                            },
                            Pieces = p.Pieces.Select(piece =>
                                new JunctionPiece
                                {
                                    RailPiece = piece.RailPiece,
                                    Orientation = piece.Orientation,
                                    Offset = new Point(piece.Offset.X - p.Entrance.Offset.X, piece.Offset.Y - p.Entrance.Offset.Y)
                                }).ToList(),
                        };
                    })
                    .ToList();
            }
        }

        public static IEnumerable<JunctionPattern> EnumeratePatterns()
        {
            Initialize();
            return Patterns;
        }

        public static IEnumerable<JunctionPattern> EnumerateChainPatterns()
        {
            Initialize();
            return ChainPatterns;
        }

        public static IEnumerable<RailPiece> EnumeratePieces()
        {
            Initialize();
            return Pieces;
        }

        public static RailPiece GetRailPiece(String Name)
        {
            Initialize();
            return Pieces.FirstOrDefault(p => p.Name == Name);
        }

        // Todo: Use Sheet.TileHeight as well.
        [TextureGenerator("RailIcons")]
        public static Texture2D RenderPatternIcons(GraphicsDevice device, Microsoft.Xna.Framework.Content.ContentManager Content, Gui.JsonTileSheet Sheet)
        {
            Initialize();

            var shader = new Shader(Content.Load<Effect>(ContentPaths.Shaders.TexturedShaders), true);

            var sqrt = (int)(Math.Ceiling(Math.Sqrt(Patterns.Count)));
            var width = MathFunctions.NearestPowerOf2(sqrt * Sheet.TileWidth);

            var fitHorizontal = width / Sheet.TileWidth;
            var rowCount = (int)Math.Ceiling((float)Patterns.Count / (float)fitHorizontal);
            var height = MathFunctions.NearestPowerOf2(rowCount * Sheet.TileHeight);

            RenderTarget2D toReturn = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth16, 16, RenderTargetUsage.PreserveContents);
            var tileSheet = new SpriteSheet(ContentPaths.rail_tiles, 32);

            device.SetRenderTarget(toReturn);
            device.Clear(Color.Transparent);
            shader.SetTexturedTechnique();
            shader.MainTexture = AssetManager.GetContentTexture(ContentPaths.rail_tiles);
            shader.SelfIlluminationEnabled = true;
            shader.SelfIlluminationTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_illumination);
            shader.EnableShadows = false;
            shader.EnableLighting = false;
            shader.ClippingEnabled = false;
            shader.CameraPosition = new Vector3(-0.5f, 0.5f, 0.5f);
            shader.VertexColorTint = Color.White;
            shader.LightRamp = Color.White;
            shader.SunlightGradient = AssetManager.GetContentTexture(ContentPaths.Gradients.sungradient);
            shader.AmbientOcclusionGradient = AssetManager.GetContentTexture(ContentPaths.Gradients.ambientgradient);
            shader.TorchlightGradient = AssetManager.GetContentTexture(ContentPaths.Gradients.torchgradient);

            Viewport oldview = device.Viewport;
            int rows = height / Sheet.TileWidth;
            int cols = width / Sheet.TileWidth;
            device.ScissorRectangle = new Rectangle(0, 0, Sheet.TileWidth, Sheet.TileHeight);
            device.RasterizerState = RasterizerState.CullNone;
            device.DepthStencilState = DepthStencilState.Default;
            Vector3 half = Vector3.One * 0.5f;
            half = new Vector3(half.X, half.Y, half.Z);

            foreach (EffectPass pass in shader.CurrentTechnique.Passes)
            {
                int ID = 0;

                foreach (var type in Patterns)
                {
                    int row = ID / cols;
                    int col = ID % cols;

                    var xboundsMin = 0;
                    var xboundsMax = 0;
                    var yboundsMin = 0;
                    var yboundsMax = 0;

                    var primitive = new RawPrimitive();
                    foreach (var piece in type.Pieces)
                    {
                        var rawPiece = RailLibrary.GetRailPiece(piece.RailPiece);
                        var bounds = Vector4.Zero;
                        var uvs = tileSheet.GenerateTileUVs(rawPiece.Tile, out bounds);
                        primitive.AddQuad(
                            Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)piece.Orientation) 
                            * Matrix.CreateTranslation(new Vector3(piece.Offset.X, 0.0f, piece.Offset.Y)),
                            Color.White, Color.White, uvs, bounds);

                        xboundsMin = Math.Min(xboundsMin, piece.Offset.X);
                        xboundsMax = Math.Max(xboundsMax, piece.Offset.X);
                        yboundsMin = Math.Min(yboundsMin, piece.Offset.Y);
                        yboundsMax = Math.Max(yboundsMax, piece.Offset.Y);
                    }

                    float xSize = xboundsMax - xboundsMin + 1;
                    float ySize = yboundsMax - yboundsMin + 1;

                    var cameraPos = new Vector3(xboundsMin + (xSize / 2), 2.0f, yboundsMax + 1.0f);

                    device.Viewport = new Viewport(col * Sheet.TileWidth, row * Sheet.TileHeight, Sheet.TileWidth, Sheet.TileHeight);
                    shader.View = Matrix.CreateLookAt(cameraPos,
                        new Vector3((xboundsMin + (xSize / 2)), 0.0f, yboundsMin),
                        Vector3.UnitY);
                    shader.Projection = Matrix.CreatePerspectiveFieldOfView(1.0f, 1.0f, 0.1f, 10);
                    shader.World = Matrix.Identity;
                    shader.CameraPosition = cameraPos;
                    pass.Apply();
                    primitive.Render(device);

                    ++ID;
                }               
            }
            device.Viewport = oldview;
            device.SetRenderTarget(null);
            return (Texture2D)toReturn;
        }

    }
}