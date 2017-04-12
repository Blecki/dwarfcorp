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
        private static Texture2D PreviewTexture;
        private static BasicEffect PreviewEffect;
        private static RenderTarget2D PreviewRenderTarget;
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
                return Matrix.CreatePerspectiveFieldOfView(1.5f, (float)PreviewRenderTarget.Width /
                    (float)PreviewRenderTarget.Height, 0.01f, 3.0f);
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
                        (int)System.Math.Max(System.Math.Min(clickPoint.X, Generator.Settings.Width - worldSize.X - 1), worldSize.X + 1),
                        (int)System.Math.Max(System.Math.Min(clickPoint.Y, Generator.Settings.Height - worldSize.Y - 1), worldSize.Y + 1));
                },
                OnMouseMove = (sender, args) =>
                {
                    // Todo: Status of mouse buttons should be passed in args to event handlers.
                    // Todo: Uh, Gum doesn't call mouse move unless you LEFT click??
                    if (Microsoft.Xna.Framework.Input.Mouse.GetState().LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                    {
                        var delta = new Vector2(args.X, args.Y) - new Vector2(PreviousMousePosition.X,
                            PreviousMousePosition.Y);
                        phi += delta.Y * 0.01f;
                        theta -= delta.X * 0.01f;
                        phi = System.Math.Max(phi, 0.5f);
                        phi = System.Math.Min(phi, 1.5f);
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
            PreviewTexture = new Texture2D(Device, Generator.Settings.Width, Generator.Settings.Height);
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

            if (KeyMesh != null)
                Root.DrawMesh(KeyMesh, Root.RenderData.Texture);
        }

        public void PreparePreview()
        {
            if (PreviewEffect == null) return;
            if (PreviewTexture == null) return;
            if (Generator.CurrentState != WorldGenerator.GenerationState.Finished) return;

            if (PreviewRenderTarget == null)
                PreviewRenderTarget = new RenderTarget2D(Device, PreviewPanel.Rect.Width, PreviewPanel.Rect.Height);

            if (UpdatePreview)
            {
                UpdatePreview = false;
                InitializePreviewRenderTypes();
                // Check combo box for style of preview to draw.

                var style = PreviewRenderTypes[PreviewSelector.SelectedItem];
                Overworld.TextureFromHeightMap(style.DisplayType, Overworld.Map,
                    style.Scalar, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), null, 
                    Generator.worldData, PreviewTexture, Generator.Settings.SeaLevel);

                var colorKeyEntries = style.GetColorKeys(Generator);
                var font = Root.GetTileSheet("font");
                var stringMeshes = new List<Gum.Mesh>();
                var y = PreviewPanel.Rect.Y;
                foreach (var color in colorKeyEntries)
                {
                    Rectangle bounds;
                    var mesh = Gum.Mesh.CreateStringMesh(color.Key, font, Vector2.One, out bounds);
                    stringMeshes.Add(mesh.Translate(PreviewPanel.Rect.X, y).Colorize(
                        color.Value.ToVector4()));
                    y += bounds.Height;
                }
                KeyMesh = Gum.Mesh.Merge(stringMeshes.ToArray());
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