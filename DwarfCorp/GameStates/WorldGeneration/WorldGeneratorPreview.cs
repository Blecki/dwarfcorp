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

        private Texture2D PreviewTexture;
        public Texture2D TerrainTexture { get; private set; }
        private Effect PreviewEffect;
        private RenderTarget2D PreviewRenderTarget;
        private Gui.Mesh KeyMesh;

        private SimpleOrbitCamera Camera = new SimpleOrbitCamera();
        private Vector2 lastSpawnWorld = Vector2.Zero;
        public Overworld Overworld;
        private OverworldPreviewMesh Mesh;

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
                Mesh.Dispose();
                TerrainTexture.Dispose();
                PreviewTexture.Dispose();
            };

            Camera.Overworld = Overworld;
        }
            
        public WorldGeneratorPreview(GraphicsDevice Device)
        {
            PreviewEffect = GameState.Game.Content.Load<Effect>("Content\\Shaders\\OverworldShader");
        }

        public void SetGenerator(OverworldGenerator Generator, Overworld Overworld)
        {
            this.Generator = Generator;
            this.Overworld = Overworld;

            if (Mesh != null)
                Mesh.Dispose();
            Mesh = new OverworldPreviewMesh();
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

        private void CreatePreviewGUI()
        {
            if (Root == null) return;

            var background = Root.GetTileSheet("basic");

            OverworldTextureGenerator.Generate(Overworld, ShowPolitics, TerrainTexture, PreviewTexture);

            var colorKeyEntries = Library.CreateBiomeColors().ToList();
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
                var previewMesh = Gui.Mesh.CreateStringMesh(line.Key, font, new Vector2(1, 1), out previewBounds);
                stringMeshes.Add(Gui.Mesh.FittedSprite(previewBounds, background, 0).Translate(PreviewPanel.Rect.Left + 16, PreviewPanel.Rect.Top + 16 + dy).Colorize(new Vector4(0.0f, 0.0f, 0.0f, 0.7f)));
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

        public void RenderPreview(GraphicsDevice device)
        {
            try
            {
                if (Generator == null || Generator.CurrentState != OverworldGenerator.GenerationState.Finished)
                {
                    KeyMesh = null;
                    return;
                }

                UpdatePreview |= Mesh.CreateIfNeeded(Overworld);

                if (PreviewTextureNeedsRegeneration())
                {
                    var graphicsDevice = Device;
                    if (graphicsDevice == null || graphicsDevice.IsDisposed)
                    {
                        PreviewTexture = null;
                        return;
                    }

                    PreviewTexture = new Texture2D(graphicsDevice, Overworld.Width * 4, Overworld.Height * 4);
                    TerrainTexture = new Texture2D(graphicsDevice, Overworld.Width * 4, Overworld.Height * 4);
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
                    Device.SetVertexBuffer(Mesh.LandMesh);
                    Device.Indices = Mesh.LandIndex;
                    Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, Mesh.LandMesh.VertexCount, 0, Mesh.LandIndex.IndexCount / 3);
                }

                if (Mesh.TreePrimitive != null)
                {
                    PreviewEffect.Parameters["Texture"].SetValue(Mesh.IconTexture);

                    foreach (EffectPass pass in PreviewEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        Mesh.TreePrimitive.Render(Device);
                    }
                }

                if (Mesh.BalloonPrimitive == null)
                    Mesh.CreatBalloonMesh(Overworld);

                var balloonPos = Overworld.InstanceSettings.Cell.Bounds.Center;

                PreviewEffect.Parameters["Texture"].SetValue(Mesh.IconTexture);
                PreviewEffect.Parameters["World"].SetValue(Matrix.CreateTranslation((float)balloonPos.X / Overworld.Width, 0.1f, (float)balloonPos.Y / Overworld.Height));
                foreach (EffectPass pass in PreviewEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Mesh.BalloonPrimitive.Render(Device);
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
    }
}