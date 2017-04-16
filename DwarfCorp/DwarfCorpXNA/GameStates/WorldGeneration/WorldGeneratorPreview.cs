using System.Collections.Generic;
using System.Linq;
using LibNoise;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Gum;

namespace DwarfCorp.GameStates
{
    public class WorldGeneratorPreview : Gum.Widget
    {
        private WorldGenerator Generator;
        private GraphicsDevice Device;
        private Gum.Widget PreviewPanel;

        private bool UpdatePreview = false;
        public Texture2D PreviewTexture { get; private set; }
        private BasicEffect PreviewEffect;
        private RenderTarget2D PreviewRenderTarget;
        private List<Vector2> SpawnRectanglePoints = new List<Vector2>(new Vector2[]
        {
            Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero
        });
        private Gum.Mesh KeyMesh;

        // Todo: This should be a camera class, or something.
        private float phi = 1.2f;
        private float theta = -0.25f;
        private float zoom = 0.9f;
        private Vector3 cameraTarget = new Vector3(0.5f, 0.0f, 0.5f);
        private Vector3 newTarget = new Vector3(0.5f, 0, 0.5f);
        private Point PreviousMousePosition;

        public Matrix ZoomedPreviewMatrix
        {
            get
            {
                var previewRect = GetSpawnRectangleInWorldSpace();
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
                return Matrix.CreateLookAt(zoom * Vector3.Transform(Vector3.Forward, CameraRotation) + cameraTarget, cameraTarget, Vector3.Up);
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

        private Gum.Widgets.ComboBox PreviewSelector;

        class PreviewRenderType
        {
            public Func<WorldGenerator, Dictionary<string, Color>> GetColorKeys;
            public String DisplayType;
            public Overworld.ScalarFieldType Scalar;

            public PreviewRenderType(String DisplayType, Overworld.ScalarFieldType Scalar,
                Func<WorldGenerator, Dictionary<String, Color>> GetColorKeys)
            {
                this.DisplayType = DisplayType;
                this.Scalar = Scalar;
                this.GetColorKeys = GetColorKeys;
            }
        }

        private static Dictionary<String, PreviewRenderType> PreviewRenderTypes;

        private static void InitializePreviewRenderTypes()
        {
            if (PreviewRenderTypes != null) return;
            PreviewRenderTypes = new Dictionary<string, PreviewRenderType>();
            PreviewRenderTypes.Add("Height",
                new PreviewRenderType("Height", Overworld.ScalarFieldType.Height,
                (g) => Overworld.HeightColors));
            PreviewRenderTypes.Add("Biomes",
                new PreviewRenderType("Biomes", Overworld.ScalarFieldType.Height,
                (g) => BiomeLibrary.CreateBiomeColors()));
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
            PreviewRenderTypes.Add("Factions",
                new PreviewRenderType("Factions", Overworld.ScalarFieldType.Factions,
                (g) =>
                {
                    Overworld.NativeFactions = g.NativeCivilizations;
                    return g.GenerateFactionColors();
                }));
        }

        public override void Construct()
        {
            PreviewSelector = AddChild(new Gum.Widgets.ComboBox
            {
                Items = new List<string>(new string[] {
                    "Height",
                    "Biomes",
                    "Temperature",
                    "Rain",
                    "Erosion",
                    "Faults",
                    "Factions" }),
                AutoLayout = Gum.AutoLayout.FloatTopLeft,
                MinimumSize = new Point(128, 0),
                OnSelectedIndexChanged = (sender) => UpdatePreview = true,
                Font = "font",
                TextColor = new Vector4(0, 0, 0, 1)
            }) as Gum.Widgets.ComboBox;

            PreviewSelector.SelectedIndex = 0;

            PreviewPanel = AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockFill,
                OnLayout = (sender) => 
                {
                    sender.Rect = sender.Rect.Interior(0, PreviewSelector.Rect.Height + 2, 0, 0);
                },
                OnClick = (sender, args) =>
                {
                    var worldSize = Generator.Settings.ColonySize.ToVector3() * Generator.Settings.WorldScale;
                    var clickPoint = ScreenToWorld(new Vector2(args.X, args.Y));
                    Generator.Settings.WorldGenerationOrigin = new Vector2(
                        System.Math.Max(System.Math.Min(clickPoint.X, Generator.Settings.Width - worldSize.X - 1), worldSize.X + 1),
                        System.Math.Max(System.Math.Min(clickPoint.Y, Generator.Settings.Height - worldSize.Z - 1), worldSize.Z + 1));
                },
                OnMouseMove = (sender, args) =>
                {
                    // Todo: Status of mouse buttons should be passed in args to event handlers.
                    // Todo: Uh, Gum doesn't call mouse move unless you LEFT click??
                    if (Microsoft.Xna.Framework.Input.Mouse.GetState().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                    {
                        var delta = new Vector2(args.X, args.Y) - new Vector2(PreviousMousePosition.X,
                             PreviousMousePosition.Y);

                        var keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();
                        if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                            keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift))
                        {
                            zoom = System.Math.Min((float)System.Math.Max(zoom + delta.Y * 0.001f, 0.1f), 1.5f);
                        }
                        else
                        {
                            phi += delta.Y * 0.01f;
                            theta -= delta.X * 0.01f;
                            phi = System.Math.Max(phi, 0.5f);
                            phi = System.Math.Min(phi, 1.5f);
                        }
                    }
                }
            });
        }
            
        public WorldGeneratorPreview(GraphicsDevice Device)
        {
            this.Device = Device;

            PreviewEffect = new BasicEffect(Device);
            PreviewEffect.EnableDefaultLighting();
            PreviewEffect.LightingEnabled = false;
            PreviewEffect.AmbientLightColor = new Vector3(1, 1, 1);
            PreviewEffect.FogEnabled = false;
        }

        public void SetGenerator(WorldGenerator Generator)
        {
            this.Generator = Generator;
            //PreviewTexture = new Texture2D(Device, Generator.Settings.Width, Generator.Settings.Height);
            Generator.UpdatePreview += () => UpdatePreview = true;
        }

        public Point ScreenToWorld(Vector2 screenCoord)
        {
            // Todo: This can be simplified.
            Viewport port = new Viewport(PreviewPanel.Rect);
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

        public Vector2 WorldToScreen(Vector2 worldCoord)
        {
            Viewport port = new Viewport(PreviewPanel.Rect);
            Vector3 worldSpace = new Vector3(worldCoord.X / Overworld.Map.GetLength(0), 0, worldCoord.Y / Overworld.Map.GetLength(1));
            Vector3 screenSpace = port.Project(worldSpace, ProjectionMatrix, ViewMatrix, Matrix.Identity);
            return new Vector2(screenSpace.X, screenSpace.Y);
        }

        public Rectangle GetSpawnRectangleInWorldSpace()
        {
            int w = (int)(Generator.Settings.ColonySize.X * Generator.Settings.WorldScale);
            int h = (int)(Generator.Settings.ColonySize.Z * Generator.Settings.WorldScale);
            return new Rectangle((int)Generator.Settings.WorldGenerationOrigin.X - w, (int)Generator.Settings.WorldGenerationOrigin.Y - h, w * 2, h * 2);
        }

        public void GetSpawnRectangleInScreenSpace(List<Vector2> Points)
        {
            Rectangle spawnRect = GetSpawnRectangleInWorldSpace();
            Points[0] = new Vector2(spawnRect.X, spawnRect.Y);
            Points[1] = new Vector2(spawnRect.X + spawnRect.Width, spawnRect.Y);
            Points[2] = new Vector2(spawnRect.X + spawnRect.Width, spawnRect.Height + spawnRect.Y);
            Points[3] = new Vector2(spawnRect.X, spawnRect.Height + spawnRect.Y);
            newTarget = new Vector3((Points[0].X + Points[2].X) / (float)Overworld.Map.GetLength(0), 0, (Points[0].Y + Points[2].Y) / (float)(Overworld.Map.GetLength(1))) * 0.5f;
            for (var i = 0; i < 4; ++i)
                Points[i] = WorldToScreen(Points[i]);
        }

        public void Update()
        {
            //Because Gum doesn't send deltas on mouse move.
            PreviousMousePosition = Root.MousePosition;
        }

        public void DrawPreview()
        {
            if (PreviewRenderTarget == null) return;
            if (Generator.CurrentState != WorldGenerator.GenerationState.Finished) return;

            Root.DrawQuad(PreviewPanel.Rect, PreviewRenderTarget);

            GetSpawnRectangleInScreenSpace(SpawnRectanglePoints);

            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp,
            null, null);
            Drawer2D.DrawPolygon(DwarfGame.SpriteBatch, Color.Yellow, 1, SpawnRectanglePoints);
            //Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, "Spawn", DefaultFont, new Vector2(a.X - 5, a.Y - 20),
            //    Color.White, Color.Black);
            DwarfGame.SpriteBatch.End();

            if (PreviewSelector.SelectedItem == "Factions") // Draw dots for capitals.
            {
                var font = Root.GetTileSheet("font");

                foreach (var civ in Generator.NativeCivilizations)
                {
                    var civLocation = WorldToScreen(new Vector2(civ.Center.X, civ.Center.Y));
                    Rectangle nameBounds;
                    var mesh = Gum.Mesh.CreateStringMesh(civ.Name, font, Vector2.One, out nameBounds);
                    nameBounds.X = (int)civLocation.X - (nameBounds.Width / 2);
                    nameBounds.Y = (int)civLocation.Y - (nameBounds.Height / 2);
                    nameBounds = MathFunctions.SnapRect(nameBounds, PreviewPanel.Rect);
                    mesh.Translate(nameBounds.X, nameBounds.Y);
                    Root.DrawMesh(mesh, Root.RenderData.Texture);
                }
            }

            if (KeyMesh != null)
                Root.DrawMesh(KeyMesh, Root.RenderData.Texture);
        }

        public void PreparePreview()
        {
            if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
            {
                KeyMesh = null;
                return;
            }

            if (PreviewRenderTarget == null)
                PreviewRenderTarget = new RenderTarget2D(Device, PreviewPanel.Rect.Width, PreviewPanel.Rect.Height);

            if (PreviewTexture == null || UpdatePreview)
            {
                UpdatePreview = false;
                InitializePreviewRenderTypes();

                if (PreviewTexture == null || PreviewTexture.Width != Overworld.Map.GetLength(0) ||
                    PreviewTexture.Height != Overworld.Map.GetLength(1))
                    PreviewTexture = new Texture2D(Device, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1));

                // Check combo box for style of preview to draw.
                Overworld.NativeFactions = Generator.NativeCivilizations;

                var style = PreviewRenderTypes[PreviewSelector.SelectedItem];
                Overworld.TextureFromHeightMap(style.DisplayType, Overworld.Map,
                    style.Scalar, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), null, 
                    Generator.worldData, PreviewTexture, Generator.Settings.SeaLevel);

                var colorKeyEntries = style.GetColorKeys(Generator);
                var font = Root.GetTileSheet("font");
                var stringMeshes = new List<Gum.Mesh>();
                var y = Rect.Y;
                var maxWidth = 0;

                foreach (var color in colorKeyEntries)
                {
                    Rectangle bounds;
                    var mesh = Gum.Mesh.CreateStringMesh(color.Key, font, new Vector2(1,1), out bounds);
                    stringMeshes.Add(mesh.Translate(PreviewPanel.Rect.Right - bounds.Width - (font.TileHeight + 4), y).Colorize(new Vector4(0,0,0,1)));
                    if (bounds.Width > maxWidth) maxWidth = bounds.Width;
                    stringMeshes.Add(Gum.Mesh.Quad().Scale(font.TileHeight, font.TileHeight)
                        .Translate(PreviewPanel.Rect.Right - font.TileHeight + 2, y)
                        .Texture(Root.GetTileSheet("basic").TileMatrix(1))
                        .Colorize(color.Value.ToVector4()));
                    y += bounds.Height;
                }


                KeyMesh = Gum.Mesh.Merge(stringMeshes.ToArray());
                var thinBorder = Root.GetTileSheet("border-thin");
                var bgMesh = Gum.Mesh.CreateScale9Background(
                    new Rectangle(Rect.Right - thinBorder.TileWidth - maxWidth - 8 - font.TileHeight,
                    Rect.Y, maxWidth + thinBorder.TileWidth + 8 + font.TileHeight, y - Rect.Y + thinBorder.TileHeight),
                    thinBorder, Scale9Corners.Bottom | Scale9Corners.Left);
                KeyMesh = Gum.Mesh.Merge(bgMesh, KeyMesh);
            }

            Device.SetRenderTarget(PreviewRenderTarget);
            PreviewEffect.World = Matrix.Identity;

            PreviewEffect.View = ViewMatrix;
            PreviewEffect.Projection = ProjectionMatrix;
            cameraTarget = newTarget * 0.1f + cameraTarget * 0.9f;
            PreviewEffect.TextureEnabled = true;
            PreviewEffect.Texture = PreviewTexture;
            Device.Clear(ClearOptions.Target, Color.Transparent, 3.0f, 0);

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

    }

}