using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using DwarfCorp.Rail;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static List<JunctionPattern> RailPatterns;
        private static List<JunctionPattern> RailChainPatterns;
        private static List<RailPiece> RailPieces;
        private static CombinationTable RailCombinationTable;
        private static bool RailInitialized = false;

        private static void InitializeRailLibrary()
        {
            if (RailInitialized)
                return;
            RailInitialized = true;

            RailPieces = FileUtils.LoadJsonListFromMultipleSources<RailPiece>(ContentPaths.rail_pieces, null, p => p.Name);
            RailPatterns = FileUtils.LoadJsonListFromMultipleSources<JunctionPattern>(ContentPaths.rail_patterns, null, p => p.Name);

            RailCombinationTable = new CombinationTable();
            RailCombinationTable.LoadConfiguration(ContentPaths.rail_combinations);

            foreach (var piece in RailPieces)
                piece.ComputeConnections();

            for (var i = 0; i < RailPatterns.Count; ++i)
                RailPatterns[i].Icon = i;

            RailChainPatterns = RailPatterns
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

            Console.WriteLine("Loaded Rail Library.");
        }

        public static IEnumerable<JunctionPattern> EnumerateRailPatterns()
        {
            InitializeRailLibrary();
            return RailPatterns;
        }

        public static IEnumerable<JunctionPattern> EnumerateRailChainPatterns()
        {
            InitializeRailLibrary();
            return RailChainPatterns;
        }

        public static IEnumerable<RailPiece> EnumerateRailPieces()
        {
            InitializeRailLibrary();
            return RailPieces;
        }

        public static MaybeNull<RailPiece> GetRailPiece(String Name)
        {
            InitializeRailLibrary();
            return RailPieces.FirstOrDefault(p => p.Name == Name);
        }

        // Todo: Does this belong here?
        public static CombinationTable.Combination FindRailCombination(String Base, String Overlay, PieceOrientation OverlayRelativeOrientation)
        {
            return RailCombinationTable.FindCombination(Base, Overlay, OverlayRelativeOrientation);
        }

        // Todo: Use Sheet.TileHeight as well.
        [TextureGenerator("RailIcons")]
        public static Texture2D RenderPatternIcons(GraphicsDevice device, Microsoft.Xna.Framework.Content.ContentManager Content, Gui.TileSheetDefinition Sheet)
        {
            InitializeRailLibrary();

            var shader = new Shader(Content.Load<Effect>(ContentPaths.Shaders.TexturedShaders), true);

            var sqrt = (int)(Math.Ceiling(Math.Sqrt(RailPatterns.Count)));
            var width = MathFunctions.NearestPowerOf2(sqrt * Sheet.TileWidth);

            var fitHorizontal = width / Sheet.TileWidth;
            var rowCount = (int)Math.Ceiling((float)RailPatterns.Count / (float)fitHorizontal);
            var height = MathFunctions.NearestPowerOf2(rowCount * Sheet.TileHeight);

            RenderTarget2D toReturn = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
            var tileSheet = new SpriteSheet(ContentPaths.rail_tiles, 32);

            device.SetRenderTarget(toReturn);
            device.Clear(Color.Transparent);
            shader.SetTexturedTechnique();
            shader.MainTexture = AssetManager.GetContentTexture(ContentPaths.rail_tiles);
            shader.SelfIlluminationEnabled = true;
            shader.SelfIlluminationTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_illumination);
            //shader.EnableShadows = false;
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

            shader.SetTexturedTechnique();

            foreach (EffectPass pass in shader.CurrentTechnique.Passes)
            {
                int ID = 0;

                foreach (var type in RailPatterns)
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
                        var rawPiece = Library.GetRailPiece(piece.RailPiece);
                        var bounds = Vector4.Zero;
                        var uvs = tileSheet.GenerateTileUVs(rawPiece.HasValue(out var raw) ? raw.Tile : Point.Zero, out bounds);
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