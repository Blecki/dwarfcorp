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

        public Texture2D PreviewTexture { get; private set; }
        private BasicEffect PreviewEffect;
        private RenderTarget2D PreviewRenderTarget;
        private List<Vector2> SpawnRectanglePoints = new List<Vector2>(new Vector2[]
        {
            Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero
        });
        private Gui.Mesh KeyMesh;

        // Todo: This should be a camera class, or something.
        private float phi = 1.2f;
        private float theta = -0.25f;
        private float zoom = 0.9f;
        private Vector3 cameraTarget = new Vector3(0.5f, 0.0f, 0.5f);
        private Vector3 newTarget = new Vector3(0.5f, 0, 0.5f);
        private Point PreviousMousePosition;
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

        private Matrix CameraRotation
        {
            get
            {
                return Matrix.CreateRotationX(phi) * Matrix.CreateRotationY(theta);
            }
        }

        private Matrix ViewMatrix
        {
            get
            {
                return Matrix.CreateLookAt(CameraPos, cameraTarget, Vector3.Up);
            }
        }

        private Vector3 CameraPos
        {
            get
            {
                return zoom*Vector3.Transform(Vector3.Forward, CameraRotation) + cameraTarget;
                
            }
        }

        private Matrix ProjectionMatrix
        {
            get
            {
                return Matrix.CreatePerspectiveFieldOfView(1.5f, (float)PreviewPanel.Rect.Width /
                    (float)PreviewPanel.Rect.Height, 0.01f, 3.0f);
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
#if false
            PreviewRenderTypes.Add("Temperature",
                new PreviewRenderType("Gray", Overworld.ScalarFieldType.Temperature,
                (g) => Overworld.JetColors));
            PreviewRenderTypes.Add("Rain",
                new PreviewRenderType("Gray", Overworld.ScalarFieldType.Rainfall,
                (g) => Overworld.JetColors));
            PreviewRenderTypes.Add("Erosion",
                new PreviewRenderType("Gray", Overworld.ScalarFieldType.Erosion,
                (g) => Overworld.JetColors));
            PreviewRenderTypes.Add("Faults",
                new PreviewRenderType("Gray", Overworld.ScalarFieldType.Faults,
                (g) => Overworld.JetColors));
#endif
            PreviewRenderTypes.Add("Factions",
                new PreviewRenderType("Factions", OverworldField.Factions,
                (g) =>
                {
                    Overworld.NativeFactions = g.NativeCivilizations;
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
#if false
                    "Temperature",
                    "Rain",
                    "Erosion",
                    "Faults",
#endif
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
                },
                OnClick = (sender, args) =>
                {
                    if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                    {
                        return;
                    }

                    if (args.MouseButton == 0)
                    {
                        int chunkSize = 16;
                        var worldSize = Generator.Settings.ColonySize.ToVector3() * chunkSize / Generator.Settings.WorldScale;
                        var clickPoint = ScreenToWorld(new Vector2(args.X, args.Y));
                        Generator.Settings.WorldGenerationOrigin = Generator.GetOrigin(clickPoint, worldSize);
                        previewText = Generator.GetSpawnStats();
                        UpdatePreview = true;
                    }
                },
                OnMouseMove = (sender, args) =>
                {
                    if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                    {
                        return;
                    }
                    if (Microsoft.Xna.Framework.Input.Mouse.GetState().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                    {
                        var delta = new Vector2(args.X, args.Y) - new Vector2(PreviousMousePosition.X,
                             PreviousMousePosition.Y);

                        var keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();
                        if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                            keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift))
                        {
                            zoom = global::System.Math.Min((float)global::System.Math.Max(zoom + delta.Y * 0.001f, 0.1f), 1.5f);
                        }
                        else
                        {
                            phi += delta.Y * 0.01f;
                            theta -= delta.X * 0.01f;
                            phi = global::System.Math.Max(phi, 0.5f);
                            phi = global::System.Math.Min(phi, 1.5f);
                        }
                    }
                },
                OnScroll = (sender, args) =>
                {
                    if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                    {
                        return;
                    }
                    zoom = global::System.Math.Min((float)global::System.Math.Max(args.ScrollValue > 0 ? zoom - 0.1f : zoom + 0.1f, 0.1f), 1.5f);
                }
            });
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
            //PreviewTexture = new Texture2D(Device, Generator.Settings.Width, Generator.Settings.Height);
            Generator.UpdatePreview += () => UpdatePreview = true;
        }

        public Point ScreenToWorld(Vector2 screenCoord)
        {
            // Todo: This can be simplified.
            Viewport port = new Viewport(PreviewPanel.GetDrawableInterior());
            port.MinDepth = 0.0f;
            port.MaxDepth = 1.0f;
            Vector3 rayStart = port.Unproject(new Vector3(screenCoord.X, screenCoord.Y, 0.0f), ProjectionMatrix, ViewMatrix, Matrix.Identity);
            Vector3 rayEnd = port.Unproject(new Vector3(screenCoord.X, screenCoord.Y, 1.0f), ProjectionMatrix,
                ViewMatrix, Matrix.Identity);
            Vector3 bearing = (rayEnd - rayStart);
            bearing.Normalize();
            Ray ray = new Ray(rayStart, bearing);
            Plane worldPlane = new Plane(Vector3.Zero, Vector3.Forward, Vector3.Right);
            float? dist = ray.Intersects(worldPlane);

            if (dist.HasValue)
            {
                Vector3 pos = rayStart + bearing * dist.Value;
                return new Point((int)(pos.X * Overworld.Map.GetLength(0)), (int)(pos.Z * Overworld.Map.GetLength(1)));
            }
            else
            {
                return new Point(0, 0);
            }
        }

        public Vector3 GetWorldSpace(Vector2 worldCoord)
        {
            var height = 0.0f;
            if ((int)worldCoord.X > 0 && (int)worldCoord.Y > 0 &&
                (int)worldCoord.X < Overworld.Map.GetLength(0) && (int)worldCoord.Y < Overworld.Map.GetLength(1))
            {
                height = Overworld.Map[(int)worldCoord.X, (int)worldCoord.Y].Height * 0.05f;
            }
            return new Vector3(worldCoord.X / Overworld.Map.GetLength(0), height, worldCoord.Y / Overworld.Map.GetLength(1));
        }

        public Vector3 WorldToScreen(Vector2 worldCoord)
        {
            Viewport port = new Viewport(PreviewPanel.GetDrawableInterior());
            Vector3 worldSpace = GetWorldSpace(worldCoord);
            return port.Project(worldSpace, ProjectionMatrix, ViewMatrix, Matrix.Identity);
        }

        public void GetSpawnRectangleInScreenSpace(List<Vector2> Points)
        {
            Rectangle spawnRect = Generator.GetSpawnRectangle();
            Points[0] = new Vector2(spawnRect.X, spawnRect.Y);
            Points[1] = new Vector2(spawnRect.X + spawnRect.Width, spawnRect.Y);
            Points[2] = new Vector2(spawnRect.X + spawnRect.Width, spawnRect.Height + spawnRect.Y);
            Points[3] = new Vector2(spawnRect.X, spawnRect.Height + spawnRect.Y);

            for (var i = 0; i < 4; ++i)
            {
                var vec3 = WorldToScreen(Points[i]);  
                Points[i] = new Vector2(vec3.X * GameSettings.Default.GuiScale, vec3.Y * GameSettings.Default.GuiScale);
            }
        }

        public void Update()
        {
            //Because Gum doesn't send deltas on mouse move.
            PreviousMousePosition = Root.MousePosition;
        }

        private float GetIconScale(Point pos)
        {
            float dist = (CameraPos - GetWorldSpace(new Vector2(pos.X, pos.Y))).Length();
            return MathFunctions.Clamp((1.0f - dist) * 4.0f, 1.0f, 4);
        }

        public void DrawPreview()
        {
            if (PreviewRenderTarget == null) return;
            if (Generator.CurrentState != WorldGenerator.GenerationState.Finished) return;

            Root.DrawQuad(PreviewPanel.Rect, PreviewRenderTarget);

            GetSpawnRectangleInScreenSpace(SpawnRectanglePoints);

            try
            {
                DwarfGame.SafeSpriteBatchBegin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Drawer2D.PointMagLinearMin, null, null, null, Matrix.Identity);
                Drawer2D.DrawPolygon(DwarfGame.SpriteBatch, Color.Yellow, 1, SpawnRectanglePoints);
            }
            finally
            {
                DwarfGame.SpriteBatch.End();
            }


            var font = Root.GetTileSheet("font10");
            var icon = Root.GetTileSheet("map-icons");
            var bkg = Root.GetTileSheet("basic");
            var rect = PreviewPanel.GetDrawableInterior();
            foreach (var tree in Trees)
            {
                var treeLocation = WorldToScreen(new Vector2(tree.X, tree.Y));
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

            foreach (var civ in Generator.NativeCivilizations)
            {
                var civLocation = WorldToScreen(new Vector2(civ.Center.X, civ.Center.Y));
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
                float scale = GetIconScale(civ.Center);
                var iconRect = new Rectangle((int)(nameBounds.Center.X - 8 * scale),
                    (int)(nameBounds.Center.Y + 8 * scale), (int)(16 * scale), (int)(16 * scale));
                if (!rect.Contains(iconRect)) continue;
                var iconMesh = Gui.Mesh.FittedSprite(iconRect,
                    icon, civ.Race.Icon);
                Root.DrawMesh(iconMesh, Root.RenderData.Texture);
            }

            Rectangle spawnWorld = Generator.GetSpawnRectangle();
            Vector2 newSpawn = new Vector2(spawnWorld.Center.X, spawnWorld.Center.Y);
            Vector2 spawnCenter = newSpawn * 0.1f + lastSpawnWorld * 0.9f;
            Vector3 newCenter = WorldToScreen(newSpawn);
            Vector3 worldCenter = WorldToScreen(spawnCenter);
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
                    || PreviewTexture.Width != Overworld.Map.GetLength(0) ||
                    PreviewTexture.Height != Overworld.Map.GetLength(1);
        }

        private void CreatePreviewGUI()
        {
            var bkg = Root.GetTileSheet("basic");
            var style = PreviewRenderTypes[PreviewSelector.SelectedItem];
            Overworld.TextureFromHeightMap(style.DisplayType, Overworld.Map, Overworld.NativeFactions,
                style.Scalar, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), null,
                Generator.worldData, PreviewTexture, Generator.Settings.SeaLevel);

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

            PreviewTexture = new Texture2D(graphicsDevice, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1));
        }

        private void RegneratePreviewTexture()
        {
            if (PreviewTextureNeedsRegeneration())
            {
                SetNewPreviewTexture();
            }
            if (PreviewTexture != null && UpdatePreview)
            {
                UpdatePreview = false;
                InitializePreviewRenderTypes();
                Overworld.NativeFactions = Generator.NativeCivilizations;
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
                    var biome = BiomeLibrary.Biomes[Overworld.Map[x, y].Biome];
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
                PreviewRenderTarget = new RenderTarget2D(Device, PreviewPanel.Rect.Width, PreviewPanel.Rect.Height);
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

            PreviewEffect.View = ViewMatrix;
            PreviewEffect.Projection = ProjectionMatrix;
            cameraTarget = newTarget * 0.1f + cameraTarget * 0.9f;
            PreviewEffect.TextureEnabled = true;
            PreviewEffect.Texture = PreviewTexture;
            PreviewEffect.LightingEnabled = true;
        }

        private void DrawPreviewInternal()
        {
            Device.Clear(ClearOptions.Target, Color.Black, 1024.0f, 0);
            Device.DepthStencilState = DepthStencilState.Default;

            if (previewSampler == null)
            {
                previewSampler = new SamplerState
                {
                    Filter = TextureFilter.MinLinearMagPointMipPoint
                };
            }

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
                {
                    UpdateTrees();
                }
                DrawPreviewInternal();
            }
            catch (InvalidOperationException exception)
            {
                return;
            }
        }

        private void PreviewRenderTarget_ContentLost(object sender, EventArgs e)
        {
            PreviewRenderTarget = new RenderTarget2D(Device, PreviewPanel.Rect.Width, PreviewPanel.Rect.Height);
            PreviewRenderTarget.ContentLost += PreviewRenderTarget_ContentLost;
        }
    }

}