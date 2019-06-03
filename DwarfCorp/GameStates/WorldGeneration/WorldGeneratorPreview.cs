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
        private WorldGenerator Generator;
        private GraphicsDevice Device { get { return GameState.Game.GraphicsDevice; } }
        public Gui.Widget PreviewPanel;
        private IEnumerable<KeyValuePair<string, Color>> previewText = null;
        private bool UpdatePreview = false;
        public Action OnCellSelectionMade = null;

        public Texture2D PreviewTexture { get; private set; }
        private BasicEffect PreviewEffect;
        private RenderTarget2D PreviewRenderTarget;
        private Gui.Mesh KeyMesh;

        // Todo: This should be a camera class, or something.
        private SimpleOrbitCamera Camera = new SimpleOrbitCamera();
        private Vector2 lastSpawnWorld = Vector2.Zero;
        private List<Point3> Trees { get; set; }
        private float TreeProbability = 0.001f;
        private SamplerState previewSampler = null;
        public Overworld Overworld;

        public Matrix ZoomedPreviewMatrix
        {
            get
            {
                var previewRect = Generator.GetSpawnRectangle();
                var worldRect = new Rectangle(0, 0, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1));
                float vScale = 1.0f / worldRect.Width;
                float uScale = 1.0f / worldRect.Height;

                return Matrix.CreateScale(vScale * previewRect.Width, uScale * previewRect.Height, 1.0f) *
                    Matrix.CreateTranslation(vScale * previewRect.X, uScale * previewRect.Y, 0.0f);
            }
        }

        private Gui.Widgets.ComboBox PreviewSelector;

        class PreviewRenderType
        {
            public Func<WorldGenerator, Dictionary<string, Color>> GetColorKeys;
            public String DisplayType;
            public OverworldField Scalar;

            public PreviewRenderType(String DisplayType, OverworldField Scalar, Func<WorldGenerator, Dictionary<String, Color>> GetColorKeys)
            {
                this.DisplayType = DisplayType;
                this.Scalar = Scalar;
                this.GetColorKeys = GetColorKeys;
            }
        }

        private static Dictionary<String, PreviewRenderType> PreviewRenderTypes;

        private void InitializePreviewRenderTypes()
        {
            if (PreviewRenderTypes != null) return;
            PreviewRenderTypes = new Dictionary<string, PreviewRenderType>();
            PreviewRenderTypes.Add("Height",
                new PreviewRenderType("Height", OverworldField.Height,
                (g) => Overworld.HeightColors));
            PreviewRenderTypes.Add("Biomes",
                new PreviewRenderType("Biomes", OverworldField.Height,
                (g) => BiomeLibrary.CreateBiomeColors()));
            PreviewRenderTypes.Add("Factions",
                new PreviewRenderType("Factions", OverworldField.Factions,
                (g) =>
                {
                    return g.GenerateFactionColors();
                }));
        }

        public override void Construct()
        {
            PreviewSelector = AddChild(new Gui.Widgets.ComboBox
            {
                Items = new List<string>(new string[] {
                    "Height",
                    "Biomes",
                    "Factions" }),
                AutoLayout = Gui.AutoLayout.FloatTopLeft,
                MinimumSize = new Point(128, 0),
                OnSelectedIndexChanged = (sender) => UpdatePreview = true,
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1)
            }) as Gui.Widgets.ComboBox;

            PreviewSelector.SelectedIndex = 1;

            PreviewPanel = AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockFill,
                OnLayout = (sender) =>
                {
                    sender.Rect = sender.Rect.Interior(0, PreviewSelector.Rect.Height + 2, 0, 0);
                    Camera.Rect = sender.Rect;
                },
                OnClick = (sender, args) =>
                {
                    if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                        return;

                    if (args.MouseButton == 0)
                    {
                        var clickPoint = Camera.ScreenToWorld(new Vector2(args.X, args.Y));

                        var colonyCell = Overworld.ColonyCells.FirstOrDefault(c => c.Bounds.Contains(new Point(clickPoint.X, clickPoint.Y)));
                        if (colonyCell != null)
                        {
                            Generator.Settings.InstanceSettings.Cell = colonyCell;
                            previewText = Generator.GetSpawnStats();
                            Camera.SetGoalFocus(new Vector3((float)colonyCell.Bounds.Center.X / (float)Overworld.Map.GetLength(0), 0, (float)colonyCell.Bounds.Center.Y / (float)Overworld.Map.GetLength(1)));
                        }

                        UpdatePreview = true;


                        OnCellSelectionMade?.Invoke();
                    }
                },
                OnMouseMove = (sender, args) => 
                {
                    if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                        return;
                    Camera.OnMouseMove(args);
                },
                OnScroll = (sender, args) =>
                {
                    if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                        return;

                    Camera.OnScroll(args);
                }
            });

            Camera.Overworld = Overworld;
        }
            
        public WorldGeneratorPreview(GraphicsDevice Device)
        {
            PreviewEffect = new BasicEffect(Device);
            PreviewEffect.LightingEnabled = false;
            PreviewEffect.FogEnabled = false;
            PreviewEffect.Alpha = 1.0f;
            PreviewEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            PreviewEffect.AmbientLightColor = new Vector3(1.0f, 1.0f, 1.0f);
        }

        public void SetGenerator(WorldGenerator Generator)
        {
            this.Generator = Generator;
            this.Overworld = Generator.Settings.Overworld;
            Generator.UpdatePreview += () => UpdatePreview = true;
        }

        public void Update(DwarfTime Time)
        {
            Camera.Update(Root.MousePosition, Time);
        }

        private float GetIconScale(Point pos)
        {
            float dist = (Camera.CameraPos - Camera.GetWorldSpace(new Vector2(pos.X, pos.Y))).Length();
            return MathFunctions.Clamp((1.0f - dist) * 4.0f, 1.0f, 4);
        }

        public void DrawPreview()
        {
            if (PreviewRenderTarget == null) return;
            if (Generator.CurrentState != WorldGenerator.GenerationState.Finished) return;

            Root.DrawQuad(PreviewPanel.Rect, PreviewRenderTarget);
                       
            var font = Root.GetTileSheet("font10");
            var icon = Root.GetTileSheet("map-icons");
            var bkg = Root.GetTileSheet("basic");
            var rect = PreviewPanel.GetDrawableInterior();
            foreach (var tree in Trees)
            {
                var treeLocation = Camera.WorldToScreen(new Vector2(tree.X, tree.Y));
                if (treeLocation.Z > 0.9999f)
                    continue;
                float scale = GetIconScale(new Point(tree.X, tree.Y));
                Rectangle nameBounds = new Rectangle(0, 0, (int)(16 * scale), (int)(16 * scale));
                nameBounds.X = (int)treeLocation.X - (nameBounds.Width / 2);
                nameBounds.Y = (int)treeLocation.Y - (nameBounds.Height / 2);
                if (!rect.Contains(nameBounds)) continue;
                var mesh = Gui.Mesh.FittedSprite(new Rectangle(nameBounds.Center.X - (int)(8 * scale), 
                    nameBounds.Center.Y - (int)(8 * scale), (int)(16 * scale), (int)(16 * scale)),
                    icon, tree.Z);
                Root.DrawMesh(mesh, Root.RenderData.Texture);
            }

            foreach (var civ in Generator.Settings.Natives.Where(n => n.InteractiveFaction))
            {
                var civLocation = Camera.WorldToScreen(new Vector2(civ.CenterX, civ.CenterY));
                if (civLocation.Z > 0.9999f)
                    continue;
                Rectangle nameBounds;
                var mesh = Gui.Mesh.CreateStringMesh(civ.Name, font, Vector2.One, out nameBounds);
                nameBounds.X = (int)civLocation.X - (nameBounds.Width / 2);
                nameBounds.Y = (int)civLocation.Y - (nameBounds.Height / 2);
                nameBounds = MathFunctions.SnapRect(nameBounds, rect);
                mesh.Translate(nameBounds.X, nameBounds.Y);
                if (!rect.Contains(nameBounds)) continue;
                if (PreviewSelector.SelectedItem == "Factions") // Draw dots for capitals.
                {
                    var bkgmesh = Gui.Mesh.FittedSprite(nameBounds, bkg, 0)
                        .Colorize(new Vector4(0.0f, 0.0f, 0.0f, 0.7f));
                   
                    Root.DrawMesh(bkgmesh, Root.RenderData.Texture);
                    Root.DrawMesh(mesh, Root.RenderData.Texture);
                }
                float scale = GetIconScale(new Point(civ.CenterX, civ.CenterY));
                var iconRect = new Rectangle((int)(nameBounds.Center.X - 8 * scale),
                    (int)(nameBounds.Center.Y + 8 * scale), (int)(16 * scale), (int)(16 * scale));
                if (!rect.Contains(iconRect)) continue;
                var iconMesh = Gui.Mesh.FittedSprite(iconRect,
                    icon, Library.GetRace(civ.Race).Icon);
                Root.DrawMesh(iconMesh, Root.RenderData.Texture);
            }

            Rectangle spawnWorld = Generator.GetSpawnRectangle();
            Vector2 newSpawn = new Vector2(spawnWorld.Center.X, spawnWorld.Center.Y);
            Vector2 spawnCenter = newSpawn * 0.1f + lastSpawnWorld * 0.9f;
            Vector3 newCenter = Camera.WorldToScreen(newSpawn);
            Vector3 worldCenter = Camera.WorldToScreen(spawnCenter);
            if (worldCenter.Z < 0.9999f)
            {
                float scale = GetIconScale(new Point((int)spawnCenter.X, (int)spawnCenter.Y));
                Rectangle balloon = new Rectangle((int)(worldCenter.X - 8 * scale), (int)(worldCenter.Y + 5 * global::System.Math.Sin(DwarfTime.LastTime.TotalRealTime.TotalSeconds * 2.0f)) - (int)(8 * scale), (int)(16 * scale), (int)(16 * scale));
                var balloonMesh = Gui.Mesh.FittedSprite(MathFunctions.SnapRect(balloon, PreviewPanel.Rect), icon, 2);
                Root.DrawMesh(balloonMesh, Root.RenderData.Texture);

                Rectangle nameBounds;
                var mesh = Gui.Mesh.CreateStringMesh("Colony Location", font, Vector2.One, out nameBounds);
                nameBounds.X = (int)newCenter.X - (nameBounds.Width / 2);
                nameBounds.Y = (int)newCenter.Y - (nameBounds.Height / 2) + 16;
                nameBounds = MathFunctions.SnapRect(nameBounds, PreviewPanel.Rect);
                mesh.Translate(nameBounds.X, nameBounds.Y);
                var bkgmesh = Gui.Mesh.FittedSprite(nameBounds, bkg, 0).Colorize(new Vector4(0.0f, 0.0f, 0.0f, 0.7f));

                Root.DrawMesh(bkgmesh, Root.RenderData.Texture);
                Root.DrawMesh(mesh, Root.RenderData.Texture);
            }

            lastSpawnWorld = spawnCenter;

            if (KeyMesh != null)
                Root.DrawMesh(KeyMesh, Root.RenderData.Texture);
        }

        private bool PreviewTextureNeedsRegeneration()
        {
            return PreviewTexture == null || PreviewTexture.IsDisposed
                    || PreviewTexture.GraphicsDevice.IsDisposed
                    || PreviewTexture.Width != Overworld.Map.GetLength(0) * 4 ||
                    PreviewTexture.Height != Overworld.Map.GetLength(1) * 4;
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
            var bkg = Root.GetTileSheet("basic");
            var style = PreviewRenderTypes[PreviewSelector.SelectedItem];
            var colorData = new Color[Overworld.Map.GetLength(0) * Overworld.Map.GetLength(1) * 4 * 4];
            
            Overworld.TextureFromHeightMap(style.DisplayType, Overworld.Map, Generator.Settings.Natives,
                4, colorData, Generator.Settings.SeaLevel);
            Overworld.Smooth(4, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), colorData);
            Overworld.ShadeHeight(Overworld.Map, 4, colorData);

            foreach (var cell in Overworld.ColonyCells)
                DrawRectangle(new Rectangle(cell.Bounds.X * 4, cell.Bounds.Y * 4, cell.Bounds.Width * 4, cell.Bounds.Height * 4), colorData, Overworld.Map.GetLength(0) * 4, Color.Yellow);

            var spawnRect = new Rectangle((int)Generator.Settings.InstanceSettings.Origin.X * 4, (int)Generator.Settings.InstanceSettings.Origin.Y * 4,
                Generator.Settings.InstanceSettings.Cell.Bounds.Width * 4, Generator.Settings.InstanceSettings.Cell.Bounds.Height * 4);
            DrawRectangle(spawnRect, colorData, Overworld.Map.GetLength(0) * 4, Color.Red);

            PreviewTexture.SetData(colorData);


            var colorKeyEntries = style.GetColorKeys(Generator);
            var font = Root.GetTileSheet("font8");
            var stringMeshes = new List<Gui.Mesh>();
            var y = Rect.Y;
            var maxWidth = 0;

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
            {
                previewText = Generator.GetSpawnStats();
            }
            int dy = 0;
            foreach (var line in previewText)
            {
                Rectangle previewBounds;
                var previewMesh = Gui.Mesh.CreateStringMesh(line.Key, font, new Vector2(1, 1), out previewBounds);
                stringMeshes.Add(Gui.Mesh.FittedSprite(previewBounds, bkg, 0).Translate(PreviewPanel.Rect.Left + 16, PreviewPanel.Rect.Top + 16 + dy).Colorize(new Vector4(0.0f, 0.0f, 0.0f, 0.7f)));
                stringMeshes.Add(previewMesh.Translate(PreviewPanel.Rect.Left + 16, PreviewPanel.Rect.Top + 16 + dy).Colorize(line.Value.ToVector4()));
                dy += previewBounds.Height;
            }

            KeyMesh = Gui.Mesh.Merge(stringMeshes.ToArray());
            var thinBorder = Root.GetTileSheet("border-thin");
            var bgMesh = Gui.Mesh.CreateScale9Background(
                new Rectangle(Rect.Right - thinBorder.TileWidth - maxWidth - 8 - font.TileHeight,
                Rect.Y, maxWidth + thinBorder.TileWidth + 8 + font.TileHeight, y - Rect.Y + thinBorder.TileHeight),
                thinBorder, Scale9Corners.Bottom | Scale9Corners.Left);
            KeyMesh = Gui.Mesh.Merge(bgMesh, KeyMesh);
        }

        private void SetNewPreviewTexture()
        {
            var graphicsDevice = Device;
            if (graphicsDevice == null || graphicsDevice.IsDisposed || Overworld.Map == null)
            {
                PreviewTexture = null;
                return;
            }

            PreviewTexture = new Texture2D(graphicsDevice, Overworld.Map.GetLength(0) * 4, Overworld.Map.GetLength(1) * 4);
        }

        private void RegneratePreviewTexture()
        {
            if (PreviewTextureNeedsRegeneration())
                SetNewPreviewTexture();

            if (PreviewTexture != null && UpdatePreview)
            {
                UpdatePreview = false;
                InitializePreviewRenderTypes();
                CreatePreviewGUI();
            }
        }

        private void UpdateTrees()
        {
            Trees = new List<Point3>();
            int width = Overworld.Map.GetLength(0);
            int height = Overworld.Map.GetLength(1);

            TreeProbability = 100.0f / (width * height);
            const int resolution = 1;

            for (int x = 0; x < width; x += resolution)
            {
                for (int y = 0; y < height; y += resolution)
                {
                    if (!MathFunctions.RandEvent(TreeProbability)) continue;
                    var h = Overworld.Map[x, y].Height;
                    if (!(h > Generator.Settings.SeaLevel)) continue;
                    var biome = BiomeLibrary.GetBiome(Overworld.Map[x, y].Biome);
                    if (biome.Icon > 0)
                    {
                        Trees.Add(new Point3(x, y, biome.Icon));
                    }
                }
            }
        }

        private void SetupPreviewShader()
        {
            Device.BlendState = BlendState.Opaque;
            Device.DepthStencilState = DepthStencilState.Default;
            if (PreviewRenderTarget == null || PreviewRenderTarget.IsDisposed || PreviewRenderTarget.GraphicsDevice.IsDisposed || PreviewRenderTarget.IsContentLost)
            {
                PreviewRenderTarget = new RenderTarget2D(Device, PreviewPanel.Rect.Width, PreviewPanel.Rect.Height, false, SurfaceFormat.Color, DepthFormat.Depth16);
                PreviewRenderTarget.ContentLost += PreviewRenderTarget_ContentLost;
            }
            Device.SetRenderTarget(PreviewRenderTarget);
            if (PreviewEffect.IsDisposed || PreviewEffect.GraphicsDevice.IsDisposed)
            {
                PreviewEffect = new BasicEffect(Device);
                PreviewEffect.LightingEnabled = false;
                PreviewEffect.FogEnabled = false;
                PreviewEffect.Alpha = 1.0f;
                PreviewEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
                PreviewEffect.AmbientLightColor = new Vector3(1.0f, 1.0f, 1.0f);
            }

            PreviewEffect.World = Matrix.Identity;

            PreviewEffect.View = Camera.ViewMatrix;
            PreviewEffect.Projection = Camera.ProjectionMatrix;
            PreviewEffect.TextureEnabled = true;
            PreviewEffect.Texture = PreviewTexture;
            PreviewEffect.LightingEnabled = true;
        }

        private void DrawPreviewInternal()
        {
            Device.Clear(ClearOptions.Target, Color.Black, 1024.0f, 0);
            Device.DepthStencilState = DepthStencilState.Default;

            if (previewSampler == null)
                previewSampler = new SamplerState
                {
                    Filter = TextureFilter.MinLinearMagPointMipPoint
                };

            Device.SamplerStates[0] = previewSampler;
            foreach (EffectPass pass in PreviewEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.SetVertexBuffer(Generator.LandMesh);
                Device.Indices = Generator.LandIndex;
                Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, Generator.LandMesh.VertexCount, 0,
                        Generator.LandIndex.IndexCount / 3);
            }
            Device.SetRenderTarget(null);
            Device.Textures[0] = null;
            Device.Indices = null;
            Device.SetVertexBuffer(null);
        }

        public void PreparePreview(GraphicsDevice device)
        {
            try
            {
                if (Generator == null || Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                {
                    KeyMesh = null;
                    return;
                }

                RegneratePreviewTexture();
                SetupPreviewShader();

                if (Generator.LandMesh == null || Generator.LandMesh.IsDisposed || Generator.LandMesh.GraphicsDevice.IsDisposed)
                {
                    Generator.CreateMesh(Device);
                    UpdatePreview = true;
                }

                if (UpdatePreview || Trees == null)
                    UpdateTrees();
                DrawPreviewInternal();
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
    }

}