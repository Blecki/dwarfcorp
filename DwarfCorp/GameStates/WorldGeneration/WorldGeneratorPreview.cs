using System.Collections.Generic;
using System.Linq;
using LibNoise;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.Gui;

namespace DwarfCorp.GameStates
{
    public class WorldGeneratorPreview : Gui.Widget
    {
        private OverworldGenerator Generator;
        private GraphicsDevice Device { get { return GameState.Game.GraphicsDevice; } }
        public Gui.Widget PreviewPanel;
        private IEnumerable<KeyValuePair<string, Color>> previewText = null;
        private bool UpdatePreview = false;
        public Action OnCellSelectionMade = null;
        private bool _showPolitics = false;
        public bool ShowPolitics { get { return _showPolitics; } set { _showPolitics = value; UpdatePreview = true; } }

        public Texture2D PreviewTexture { get; private set; }
        private Effect PreviewEffect;
        private RenderTarget2D PreviewRenderTarget;
        private Gui.Mesh KeyMesh;

        private SimpleOrbitCamera Camera = new SimpleOrbitCamera();
        private Vector2 lastSpawnWorld = Vector2.Zero;
        public Overworld Overworld;

        public VertexBuffer LandMesh;
        public IndexBuffer LandIndex;
        private float HeightScale = 0.02f;

        private RawPrimitive TreePrimitive;
        private Texture2D IconTexture;
        private RawPrimitive BalloonPrimitive;

        private MemoryTexture StripeTexture;

        public Matrix ZoomedPreviewMatrix
        {
            get
            {
                var previewRect = Overworld.InstanceSettings.Cell.Bounds;
                var worldRect = new Rectangle(0, 0, Overworld.Width, Overworld.Height);
                float vScale = 1.0f / worldRect.Width;
                float uScale = 1.0f / worldRect.Height;

                return Matrix.CreateScale(vScale * previewRect.Width, uScale * previewRect.Height, 1.0f) *
                    Matrix.CreateTranslation(vScale * previewRect.X, uScale * previewRect.Y, 0.0f);
            }
        }

        public override void Construct()
        {
            PreviewPanel = AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockFill,
                OnLayout = (sender) =>
                {
                    Camera.Rect = sender.Rect;
                },
                OnClick = (sender, args) =>
                {
                    if (Generator.CurrentState != OverworldGenerator.GenerationState.Finished)
                        return;

                    if (args.MouseButton == 0)
                    {
                        var clickPoint = Camera.ScreenToWorld(new Vector2(args.X, args.Y));

                        var colonyCell = Overworld.ColonyCells.GetCellAt(clickPoint.X, clickPoint.Y);
                        if (colonyCell != null)
                        {
                            Overworld.InstanceSettings.Cell = colonyCell;
                            previewText = Generator.GetSpawnStats();
                            Camera.SetGoalFocus(new Vector3((float)colonyCell.Bounds.Center.X / (float)Overworld.Width, 0, (float)colonyCell.Bounds.Center.Y / (float)Overworld.Height));
                        }

                        UpdatePreview = true;


                        OnCellSelectionMade?.Invoke();
                    }
                },
                OnMouseMove = (sender, args) => 
                {
                    if (Generator.CurrentState != OverworldGenerator.GenerationState.Finished)
                        return;
                    Camera.OnMouseMove(args);
                },
                OnScroll = (sender, args) =>
                {
                    if (Generator.CurrentState != OverworldGenerator.GenerationState.Finished)
                        return;

                    Camera.OnScroll(args);
                }
            });

            OnClose += (sender) =>
            {
                if (LandMesh != null)
                    LandMesh.Dispose();
                if (LandIndex != null)
                    LandIndex.Dispose();

                LandMesh = null;
                LandIndex = null;
            };

            Camera.Overworld = Overworld;
        }
            
        public WorldGeneratorPreview(GraphicsDevice Device)
        {
            PreviewEffect = GameState.Game.Content.Load<Effect>("Content\\Shaders\\OverworldShader");
            StripeTexture = TextureTool.MemoryTextureFromTexture2D(AssetManager.GetContentTexture("World\\stripes"));
        }

        public void SetGenerator(OverworldGenerator Generator, Overworld Overworld)
        {
            this.Generator = Generator;
            this.Overworld = Overworld;

            if (LandMesh != null)
                LandMesh.Dispose();
            LandMesh = null;

            if (LandIndex != null)
                LandIndex.Dispose();
            LandIndex = null;
        }

        public void Update(DwarfTime Time)
        {
            if (Root != null)
                Camera.Update(Root.MousePosition, Time);
        }

        public void DrawPreview()
        {
            if (PreviewRenderTarget == null) return;
            if (Generator.CurrentState != OverworldGenerator.GenerationState.Finished) return;

            Root.DrawQuad(PreviewPanel.Rect, PreviewRenderTarget);
                       
            var rect = PreviewPanel.GetDrawableInterior();

            if (KeyMesh != null)
                Root.DrawMesh(KeyMesh, Root.RenderData.Texture);
        }

        private bool PreviewTextureNeedsRegeneration()
        {
            return PreviewTexture == null || PreviewTexture.IsDisposed
                    || PreviewTexture.GraphicsDevice.IsDisposed
                    || PreviewTexture.Width != Overworld.Width * 4 ||
                    PreviewTexture.Height != Overworld.Height * 4;
        }

        private void DrawRectangle(Rectangle Rect, Color[] Into, int Width, Color Color)
        {
            for (var x = 0; x < Rect.Width; ++x)
            {
                Into[(Rect.Y * Width) + Rect.X + x] = Color;
                Into[((Rect.Y + Rect.Height - 1) * Width) + Rect.X + x] = Color;
            }

            for (var y = 1; y < Rect.Height - 1; ++y)
            {
                Into[((Rect.Y + y) * Width) + Rect.X] = Color;
                Into[((Rect.Y + y) * Width) + Rect.X + Rect.Width - 1] = Color;
            }
        }

        private void CreatePreviewGUI()
        {
            if (Root == null) return;

            var background = Root.GetTileSheet("basic");

            GenerateTerrainTexture();

            var colorKeyEntries = BiomeLibrary.CreateBiomeColors().ToList();
            var font = Root.GetTileSheet("font8");
            var stringMeshes = new List<Gui.Mesh>();
            var y = Rect.Y;
            var maxWidth = 0;

            foreach (var native in Overworld.Natives.Where(n => n.InteractiveFaction && !n.IsCorporate))
                colorKeyEntries.Add(new KeyValuePair<string, Color>(native.Name, native.PrimaryColor));
            colorKeyEntries.Add(new KeyValuePair<string, Color>("Player", Overworld.Natives.FirstOrDefault(n => n.Name == "Player").PrimaryColor));

            foreach (var color in colorKeyEntries)
            {
                Rectangle bounds;
                var mesh = Gui.Mesh.CreateStringMesh(color.Key, font, new Vector2(1, 1), out bounds);
                stringMeshes.Add(mesh.Translate(PreviewPanel.Rect.Right - bounds.Width - (font.TileHeight + 4), y).Colorize(new Vector4(0, 0, 0, 1)));
                if (bounds.Width > maxWidth) maxWidth = bounds.Width;
                stringMeshes.Add(Gui.Mesh.Quad().Scale(font.TileHeight, font.TileHeight)
                    .Translate(PreviewPanel.Rect.Right - font.TileHeight + 2, y)
                    .Texture(Root.GetTileSheet("basic").TileMatrix(1))
                    .Colorize(color.Value.ToVector4()));
                y += bounds.Height;
            }


            if (previewText == null)
                previewText = Generator.GetSpawnStats();

            var dy = 0;
            foreach (var line in previewText)
            {
                Rectangle previewBounds;
                var previewMesh = Mesh.CreateStringMesh(line.Key, font, new Vector2(1, 1), out previewBounds);
                stringMeshes.Add(Mesh.FittedSprite(previewBounds, background, 0).Translate(PreviewPanel.Rect.Left + 16, PreviewPanel.Rect.Top + 16 + dy).Colorize(new Vector4(0.0f, 0.0f, 0.0f, 0.7f)));
                stringMeshes.Add(previewMesh.Translate(PreviewPanel.Rect.Left + 16, PreviewPanel.Rect.Top + 16 + dy).Colorize(line.Value.ToVector4()));
                dy += previewBounds.Height;
            }

            KeyMesh = Gui.Mesh.Merge(stringMeshes.ToArray());
            var thinBorder = Root.GetTileSheet("border-thin");
            var bgMesh = Gui.Mesh.CreateScale9Background(
                new Rectangle(Rect.Right - thinBorder.TileWidth - maxWidth - 8 - font.TileHeight, Rect.Y, maxWidth + thinBorder.TileWidth + 8 + font.TileHeight, y - Rect.Y + thinBorder.TileHeight),
                thinBorder, Scale9Corners.Bottom | Scale9Corners.Left);
            KeyMesh = Gui.Mesh.Merge(bgMesh, KeyMesh);
        }

        private void GenerateTerrainTexture()
        {
            var colorData = new Color[Overworld.Width * Overworld.Height * 4 * 4];

            Overworld.Map.CreateTexture(Overworld.Natives, 4, colorData, Overworld.GenerationSettings.SeaLevel);
            OverworldMap.Smooth(4, Overworld.Width, Overworld.Height, colorData);
            Overworld.Map.ShadeHeight(4, colorData);

            // Draw political boundaries
            if (ShowPolitics)
            {
                foreach (var cell in Overworld.ColonyCells.EnumerateCells())
                {
                    FillPoliticalRectangle(new Rectangle(cell.Bounds.Left * 4, cell.Bounds.Top * 4, cell.Bounds.Width * 4, cell.Bounds.Height * 4), colorData, cell.Faction.PrimaryColor);
                    foreach (var neighbor in Overworld.ColonyCells.EnumerateManhattanNeighbors(cell))
                    {
                        if (Object.ReferenceEquals(cell.Faction, neighbor.Faction))
                            continue;

                        if (neighbor.Bounds.Right <= cell.Bounds.Left)
                            DrawVerticalPoliticalEdge(cell.Bounds.Left, System.Math.Max(cell.Bounds.Top, neighbor.Bounds.Top), System.Math.Min(cell.Bounds.Bottom, neighbor.Bounds.Bottom), colorData, cell.Faction.PrimaryColor);
                        if (neighbor.Bounds.Left >= cell.Bounds.Right)
                            DrawVerticalPoliticalEdge(cell.Bounds.Right - 2, System.Math.Max(cell.Bounds.Top, neighbor.Bounds.Top), System.Math.Min(cell.Bounds.Bottom, neighbor.Bounds.Bottom), colorData, cell.Faction.PrimaryColor);
                        if (neighbor.Bounds.Bottom <= cell.Bounds.Top)
                            DrawHorizontalPoliticalEdge(System.Math.Max(cell.Bounds.Left, neighbor.Bounds.Left), System.Math.Min(cell.Bounds.Right, neighbor.Bounds.Right), cell.Bounds.Top, colorData, cell.Faction.PrimaryColor);
                        if (neighbor.Bounds.Top >= cell.Bounds.Bottom)
                            DrawHorizontalPoliticalEdge(System.Math.Max(cell.Bounds.Left, neighbor.Bounds.Left), System.Math.Min(cell.Bounds.Right, neighbor.Bounds.Right), cell.Bounds.Bottom - 2, colorData, cell.Faction.PrimaryColor);

                    }
                }
            }

            foreach (var cell in Overworld.ColonyCells.EnumerateCells())
                DrawRectangle(new Rectangle(cell.Bounds.X * 4, cell.Bounds.Y * 4, cell.Bounds.Width * 4, cell.Bounds.Height * 4), colorData, Overworld.Width * 4, Color.Black);

            var spawnRect = new Rectangle((int)Overworld.InstanceSettings.Origin.X * 4, (int)Overworld.InstanceSettings.Origin.Y * 4,
                Overworld.InstanceSettings.Cell.Bounds.Width * 4, Overworld.InstanceSettings.Cell.Bounds.Height * 4);
            DrawRectangle(spawnRect, colorData, Overworld.Width * 4, Color.Red);

            PreviewTexture.SetData(colorData);
        }

        private void DrawVerticalPoliticalEdge(int X, int MinY, int MaxY, Color[] Data, Color FactionColor)
        {
            FillSolidRectangle(new Rectangle(X * 4, MinY * 4, 8, (MaxY - MinY) * 4), Data, FactionColor);
        }

        private void DrawHorizontalPoliticalEdge(int MinX, int MaxX, int Y, Color[] Data, Color FactionColor)
        {
            FillSolidRectangle(new Rectangle(MinX * 4, Y * 4, (MaxX - MinX) * 4, 8), Data, FactionColor);
        }

        private void FillPoliticalRectangle(Rectangle R, Color[] Data, Color Color)
        {
            var stride = Overworld.Width * 4;
            for (var x = R.Left; x < R.Right; ++x)
                for (var y = R.Top; y < R.Bottom; ++y)
                {
                    var tX = x % StripeTexture.Width;
                    var tY = y % StripeTexture.Height;
                    if (StripeTexture.Data[(tY * StripeTexture.Width) + tX].R != 0)
                        Data[(y * stride) + x] = Color;
                }
        }

        private void FillSolidRectangle(Rectangle R, Color[] Data, Color Color)
        {
            var stride = Overworld.Width * 4;
            for (var x = R.Left; x < R.Right; ++x)
                for (var y = R.Top; y < R.Bottom; ++y)
                    Data[(y * stride) + x] = Color;
        }

        public void RenderPreview(GraphicsDevice device)
        {
            try
            {
                if (Generator == null || Generator.CurrentState != OverworldGenerator.GenerationState.Finished)
                {
                    KeyMesh = null;
                    return;
                }

                if (LandMesh == null || LandMesh.IsDisposed || LandMesh.GraphicsDevice.IsDisposed)
                {
                    CreateMesh(Device);
                    UpdatePreview = true;
                }

                if (PreviewTextureNeedsRegeneration())
                {
                    var graphicsDevice = Device;
                    if (graphicsDevice == null || graphicsDevice.IsDisposed)
                    {
                        PreviewTexture = null;
                        return;
                    }

                    PreviewTexture = new Texture2D(graphicsDevice, Overworld.Width * 4, Overworld.Height * 4);
                }

                if (PreviewTexture != null && UpdatePreview)
                {
                    UpdatePreview = false;
                    CreatePreviewGUI();
                }

                if (PreviewRenderTarget == null || PreviewRenderTarget.IsDisposed || PreviewRenderTarget.GraphicsDevice.IsDisposed || PreviewRenderTarget.IsContentLost)
                {
                    PreviewRenderTarget = new RenderTarget2D(Device, PreviewPanel.Rect.Width, PreviewPanel.Rect.Height, false, SurfaceFormat.Color, DepthFormat.Depth16);
                    PreviewRenderTarget.ContentLost += PreviewRenderTarget_ContentLost;
                }

                Device.SetRenderTarget(PreviewRenderTarget);
                Device.DepthStencilState = DepthStencilState.Default;
                Device.RasterizerState = RasterizerState.CullNone;
                Device.Clear(ClearOptions.Target, Color.Black, 1024.0f, 0);

                PreviewEffect.Parameters["World"].SetValue(Matrix.Identity);
                PreviewEffect.Parameters["View"].SetValue(Camera.ViewMatrix);
                PreviewEffect.Parameters["Projection"].SetValue(Camera.ProjectionMatrix);
                PreviewEffect.Parameters["Texture"].SetValue(PreviewTexture);
                PreviewEffect.CurrentTechnique = PreviewEffect.Techniques[0];


                foreach (EffectPass pass in PreviewEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Device.SetVertexBuffer(LandMesh);
                    Device.Indices = LandIndex;
                    Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, LandMesh.VertexCount, 0, LandIndex.IndexCount / 3);
                }

                if (TreePrimitive != null)
                {
                    PreviewEffect.Parameters["Texture"].SetValue(IconTexture);

                    foreach (EffectPass pass in PreviewEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        TreePrimitive.Render(Device);
                    }
                }

                if (BalloonPrimitive == null)
                    CreatBalloonMesh();

                var balloonPos = Overworld.InstanceSettings.Cell.Bounds.Center;

                PreviewEffect.Parameters["Texture"].SetValue(IconTexture);
                PreviewEffect.Parameters["World"].SetValue(Matrix.CreateTranslation((float)balloonPos.X / Overworld.Width, 0.1f, (float)balloonPos.Y / Overworld.Height));
                foreach (EffectPass pass in PreviewEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    BalloonPrimitive.Render(Device);
                }

                Device.SetRenderTarget(null);
                Device.Indices = null;
                Device.SetVertexBuffer(null);
            }
            catch (InvalidOperationException exception)
            {
                return;
            }
        }

        private void PreviewRenderTarget_ContentLost(object sender, EventArgs e)
        {
            PreviewRenderTarget = new RenderTarget2D(Device, PreviewPanel.Rect.Width, PreviewPanel.Rect.Height, false, SurfaceFormat.Color, DepthFormat.Depth16);
            PreviewRenderTarget.ContentLost += PreviewRenderTarget_ContentLost;
        }

        private static int[] SetUpTerrainIndices(int width, int height)
        {
            var indices = new int[(width - 1) * (height - 1) * 6];
            int counter = 0;
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int lowerLeft = x + y * width;
                    int lowerRight = (x + 1) + y * width;
                    int topLeft = x + (y + 1) * width;
                    int topRight = (x + 1) + (y + 1) * width;

                    indices[counter++] = topLeft;
                    indices[counter++] = lowerRight;
                    indices[counter++] = lowerLeft;

                    indices[counter++] = topLeft;
                    indices[counter++] = topRight;
                    indices[counter++] = lowerRight;
                }
            }

            return indices;
        }

        private void CreateMesh(GraphicsDevice Device)
        {
            var numVerts = (Overworld.Width + 1) * (Overworld.Height + 1);
            LandMesh = new VertexBuffer(Device, VertexPositionNormalTexture.VertexDeclaration, numVerts, BufferUsage.None);
            var verts = new VertexPositionNormalTexture[numVerts];

            int i = 0;
            for (int x = 0; x <= Overworld.Width; x += 1)
            {
                for (int y = 0; y <= Overworld.Height; y += 1)
                {
                    var landHeight = Overworld.Map.Height((x < Overworld.Width) ? x : x - 1, (y < Overworld.Height) ? y : y - 1);
                    verts[i].Position = new Vector3((float)x / Overworld.Width, landHeight * HeightScale, (float)y / Overworld.Height);
                    verts[i].TextureCoordinate = new Vector2((float)x / Overworld.Width, (float)y / Overworld.Height);

                    var normal = new Vector3(
                        Overworld.Map.Height(MathFunctions.Clamp(x + 1, 0, Overworld.Width - 1), MathFunctions.Clamp(y, 0, Overworld.Height - 1)) - Overworld.Height,
                        1.0f,
                        Overworld.Map.Height(MathFunctions.Clamp(x, 0, Overworld.Width - 1), MathFunctions.Clamp(y + 1, 0, Overworld.Height - 1)) - Overworld.Height);
                    normal.Normalize();
                    verts[i].Normal = normal;

                    i++;
                }
            }

            LandMesh.SetData(verts);

            var indices = SetUpTerrainIndices((Overworld.Width + 1), (Overworld.Height + 1));
            LandIndex = new IndexBuffer(Device, typeof(int), indices.Length, BufferUsage.None);
            LandIndex.SetData(indices);

            // Create tree mesh.

            TreePrimitive = new RawPrimitive();
            if (IconTexture == null)
                IconTexture = AssetManager.GetContentTexture("GUI\\map_icons");
            var iconSheet = new SpriteSheet(IconTexture, 16, 16);

            for (int x = 0; x < Overworld.Width; x += 1)
                for (int y = 0; y < Overworld.Height; y += 1)
                {
                    if (!MathFunctions.RandEvent(0.01f)) continue;
                    var elevation = Overworld.Map.Height(x, y);
                    if (elevation <= Overworld.GenerationSettings.SeaLevel) continue;
                    var biome = BiomeLibrary.GetBiome(Overworld.Map.Map[x, y].Biome);
                    if (biome.Icon.X > 0 || biome.Icon.Y > 0)
                    {
                        var bounds = Vector4.Zero;
                        var uvs = iconSheet.GenerateTileUVs(biome.Icon, out bounds);
                        var angle = MathFunctions.Rand() * (float)System.Math.PI;

                        TreePrimitive.AddQuad(
                            Matrix.CreateRotationX(-(float)System.Math.PI / 2)
                            * Matrix.CreateRotationY(angle)
                            * Matrix.CreateScale(2.0f / Overworld.Width)
                            * Matrix.CreateTranslation((float)x / Overworld.Width, elevation * HeightScale + 1.0f / Overworld.Width, (float)y / Overworld.Height),
                            Color.White, Color.White, uvs, bounds);

                        TreePrimitive.AddQuad(
                            Matrix.CreateRotationX(-(float)System.Math.PI / 2)
                            * Matrix.CreateRotationY((float)System.Math.PI / 2)
                            * Matrix.CreateRotationY(angle)
                            * Matrix.CreateScale(2.0f / Overworld.Width)
                            * Matrix.CreateTranslation((float)x / Overworld.Width, elevation * HeightScale + 1.0f / Overworld.Width, (float)y / Overworld.Height),
                            Color.White, Color.White, uvs, bounds);
                    }
                }
        }

        private void CreatBalloonMesh()
        {
            BalloonPrimitive = new RawPrimitive();
            if (IconTexture == null)
                IconTexture = AssetManager.GetContentTexture("GUI\\map_icons");
            var iconSheet = new SpriteSheet(IconTexture, 16, 16);
            var bounds = Vector4.Zero;
            var uvs = iconSheet.GenerateTileUVs(new Point(2, 0), out bounds);
            var angle = MathFunctions.Rand() * (float)System.Math.PI;

            BalloonPrimitive.AddQuad(
                Matrix.CreateRotationX(-(float)System.Math.PI / 2)
                * Matrix.CreateScale(6.0f / Overworld.Width),
                Color.White, Color.White, uvs, bounds);

            BalloonPrimitive.AddQuad(
                Matrix.CreateRotationX(-(float)System.Math.PI / 2)
                * Matrix.CreateRotationY((float)System.Math.PI / 2)
                * Matrix.CreateScale(6.0f / Overworld.Width),
                Color.White, Color.White, uvs, bounds);
        }    }
}