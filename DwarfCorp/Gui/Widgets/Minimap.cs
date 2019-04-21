using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.Gui.Widgets
{
    public class MinimapFrame : Gui.Widget
    {
        public String Frame = "minimap-frame";
        public MinimapRenderer Renderer;

        public override Point GetBestSize()
        {
            return new Point(Renderer.RenderWidth + 16, Renderer.RenderHeight + 12);
        }

        public override void Construct()
        {
            MinimumSize = GetBestSize();
            MaximumSize = GetBestSize();

            OnClick = (sender, args) =>
                {
                    var localX = args.X - Rect.X;
                    var localY = args.Y - Rect.Y;

                    if (localX < Renderer.RenderWidth && localY > 12)
                        Renderer.OnClicked(localX, localY);
                };


            var buttonRow = AddChild(new Gui.Widget
            {
                Transparent = true,
                MinimumSize = new Point(0, 20),
                AutoLayout = Gui.AutoLayout.DockTop,
                Padding = new Gui.Margin(2, 2, 2, 2)
            });

            buttonRow.AddChild(new Gui.Widgets.ImageButton
                {
                    Background = new Gui.TileReference("round-buttons", 0),
                    MinimumSize = new Point(16, 16),
                    MaximumSize = new Point(16, 16),
                    AutoLayout = Gui.AutoLayout.DockLeft,
                    OnClick = (sender, args) => Renderer.ZoomIn(),
                    Tooltip = "Zoom in"
                });

            buttonRow.AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 1),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gui.AutoLayout.DockLeft,
                OnClick = (sender, args) => Renderer.ZoomOut(),
                Tooltip = "Zoom out"
            });

            buttonRow.AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 2),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gui.AutoLayout.DockLeft,
                OnClick = (sender, args) => Renderer.ZoomHome(),
                Tooltip = "Zoom to home base"
            });

            OnScroll = (sender, args) =>
            {
                float multiplier = GameSettings.Default.InvertZoom ? 0.001f : -0.001f;
                Renderer.Zoom(args.ScrollValue * multiplier);
            };

            base.Construct();
        }

        protected override Gui.Mesh Redraw()
        {
            return Gui.Mesh.CreateScale9Background(Rect, Root.GetTileSheet("tray-border-transparent"), Scale9Corners.Top | Scale9Corners.Right);
        }

    }

    public class MinimapRenderer : IDisposable
    {
        public Vector3 HomePosition { get; set; }
        protected bool HomeSet = false;
        public RenderTarget2D RenderTarget = null;
        public RenderTarget2D TerrainTexture = null;
        public string ColorMap { get; set; }
        public int RenderWidth = 196;
        public int RenderHeight = 196;
        public OrbitCamera Camera { get; set; }
        public WorldManager World { get; set; }
        private BasicEffect DrawShader = null;
        public MinimapRenderer(int width, int height, WorldManager world, string colormap)
        {
            ColorMap = colormap;

            RenderWidth = width;
            RenderHeight = height;
            RenderTarget = new RenderTarget2D(GameState.Game.GraphicsDevice, RenderWidth, RenderHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
            World = world;
            DrawShader = new BasicEffect(World.GraphicsDevice);
            Camera = new OrbitCamera(World, new Vector3(0, 0, 0), new Vector3(0, 0, 0), 2.5f, 1.0f, 0.1f, 1000.0f)
            {
                Projection = global::DwarfCorp.Camera.ProjectionMode.Orthographic
            };
        }

        public void OnClicked(int X, int Y)
        {
            Viewport viewPort = new Viewport(RenderTarget.Bounds);
            Vector3 forward = (World.Camera.Target - World.Camera.Position);
            forward.Normalize();

            Vector3 pos = viewPort.Unproject(new Vector3(X, Y, 0), Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);
            Vector3 target = new Vector3(pos.X, World.Camera.Position.Y, pos.Z);
            var height = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                World.ChunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(target)))
                .Coordinate.Y + 1;
            target.Y = Math.Max(height + 15, target.Y);
            target = MathFunctions.Clamp(target, World.ChunkManager.Bounds);
            World.Camera.ZoomTargets.Clear();
            World.Camera.ZoomTargets.Add(target);
        }

        public void Zoom(float f)
        {
            Camera.FOV = Math.Max(Math.Min(Camera.FOV + f, (float)Math.PI), 0.1f);
            Camera.UpdateProjectionMatrix();
        }

        public void ZoomOut()
        {
            Zoom(0.5f);
        }

        public void ZoomIn()
        {
            Zoom(-0.5f);
        }

        public void ZoomHome()
        {
            World.Camera.UpdateViewMatrix();
            World.Camera.ZoomTargets.Clear();
            World.Camera.ZoomTargets.Add(HomePosition);
            World.Master.SetMaxViewingLevel(World.WorldSizeInVoxels.Y);
        }


        public void Render(Rectangle Where, Gui.Root Gui)
        {
            Gui.DrawQuad(Where, RenderTarget);
        }
        private int _iters = 0;
        public void ReDrawChunks(DwarfTime time)
        {
            _renderTimer.Update(time);
            if (!_renderTimer.HasTriggered)
                return;
            _iters++;
            if (_iters > 5 && !(World.ChunkManager.NeedsMinimapUpdate || World.ChunkManager.Water.NeedsMinimapUpdate))
                return;
            World.ChunkManager.NeedsMinimapUpdate = false;
            World.ChunkManager.Water.NeedsMinimapUpdate = false;

            var bounds = World.ChunkManager.Bounds;
            float scale = 2.0f;
            int numPixelsX = (int)((bounds.Max.X - bounds.Min.X) * scale);
            int numPixelsZ = (int)((bounds.Max.Z - bounds.Min.Z) * scale);

            if (TerrainTexture == null || TerrainTexture.IsDisposed || TerrainTexture.IsContentLost)
            {
                TerrainTexture = new RenderTarget2D(GameState.Game.GraphicsDevice, numPixelsX, numPixelsZ, false, SurfaceFormat.Color, DepthFormat.Depth24);
            }

            World.GraphicsDevice.SetRenderTarget(TerrainTexture);

            World.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0, 0);
            World.DefaultShader.View = Matrix.CreateLookAt(bounds.Center() + Vector3.Up * 128, bounds.Center(), -Vector3.UnitZ);
            World.DefaultShader.EnbleFog = false;

            World.DefaultShader.Projection = Matrix.CreateOrthographic((float)numPixelsX / scale, (float)numPixelsZ / scale, 1.0f, 512);
            World.DefaultShader.CurrentTechnique = World.DefaultShader.Techniques[Shader.Technique.TexturedWithColorScale];

            World.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            World.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            World.ChunkRenderer.RenderForMinimap(Camera, time, World.GraphicsDevice, World.DefaultShader, Matrix.Identity, AssetManager.GetContentTexture(ColorMap));

            World.GraphicsDevice.Textures[0] = null;
            World.GraphicsDevice.Indices = null;
            World.GraphicsDevice.SetVertexBuffer(null);
            World.WaterRenderer.DrawWaterFlat(World.GraphicsDevice, World.DefaultShader.View, World.DefaultShader.Projection, World.DefaultShader, World.ChunkManager);
        }

        private Timer _renderTimer = new Timer(0.15f, false, Timer.TimerMode.Real);
        private VertexPositionTexture[] quad = null;

        public void ValidateShader()
        {
            if (DrawShader != null && (DrawShader.IsDisposed || DrawShader.GraphicsDevice.IsDisposed))
            {
                DrawShader = new BasicEffect(World.GraphicsDevice);
            }

            if (TerrainTexture == null || TerrainTexture.IsDisposed || TerrainTexture.IsContentLost)
            {
                var bounds = World.ChunkManager.Bounds;
                float scale = 2.0f;
                int numPixelsX = (int)((bounds.Max.X - bounds.Min.X) * scale);
                int numPixelsZ = (int)((bounds.Max.Z - bounds.Min.Z) * scale);
                TerrainTexture = new RenderTarget2D(GameState.Game.GraphicsDevice, numPixelsX, numPixelsZ, false, SurfaceFormat.Color, DepthFormat.Depth24);
            }
        }

        public void PreRender(DwarfTime time, SpriteBatch sprites)
        {
                if (sprites.IsDisposed || sprites.GraphicsDevice.IsDisposed)
                {
                    return;
                }
                try
                {
                    ValidateShader();
                    if (RenderTarget.IsDisposed || RenderTarget.IsContentLost)
                    {
                        World.ChunkManager.NeedsMinimapUpdate = true;
                        RenderTarget = new RenderTarget2D(GameState.Game.GraphicsDevice, RenderWidth, RenderHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
                        return;
                    }

                    if (!HomeSet)
                    {
                        if (World.PlayerFaction.GetRooms().Count > 0)
                        {
                            HomePosition = World.PlayerFaction.GetRooms().First().GetBoundingBox().Center();
                        }
                        HomeSet = true;
                    }
                    ReDrawChunks(time);
                    World.GraphicsDevice.SetRenderTarget(RenderTarget);
                    World.GraphicsDevice.Clear(Color.Black);
                    Camera.Target = World.Camera.Target;
                    Vector3 cameraToTarget = World.Camera.Target - World.Camera.Position;
                    cameraToTarget.Normalize();
                    Camera.Position = World.Camera.Target + Vector3.Up * 50 - cameraToTarget * 4;
                    Camera.UpdateViewMatrix();
                    Camera.UpdateProjectionMatrix();
                    World.DefaultShader.View = Camera.ViewMatrix;
                    World.DefaultShader.Projection = Camera.ProjectionMatrix;
                    var bounds = World.ChunkManager.Bounds;
                    DrawShader.Texture = TerrainTexture;
                    DrawShader.TextureEnabled = true;
                    DrawShader.LightingEnabled = false;
                    DrawShader.Projection = Camera.ProjectionMatrix;
                    DrawShader.View = Camera.ViewMatrix;
                    DrawShader.World = Matrix.Identity;
                    DrawShader.VertexColorEnabled = false;
                    DrawShader.Alpha = 1.0f;

                    if (quad == null)
                    {
                        quad = new VertexPositionTexture[]
                        {
                                        new VertexPositionTexture(new Vector3(bounds.Min.X, 0, bounds.Min.Z), new Vector2(0, 0)),
                                        new VertexPositionTexture(new Vector3(bounds.Max.X, 0, bounds.Min.Z), new Vector2(1, 0)),
                                        new VertexPositionTexture(new Vector3(bounds.Max.X, 0, bounds.Max.Z), new Vector2(1, 1)),
                                        new VertexPositionTexture(new Vector3(bounds.Max.X, 0, bounds.Max.Z), new Vector2(1, 1)),
                                        new VertexPositionTexture(new Vector3(bounds.Min.X, 0, bounds.Max.Z), new Vector2(0, 1)),
                                        new VertexPositionTexture(new Vector3(bounds.Min.X, 0, bounds.Min.Z), new Vector2(0, 0)),
                        };
                    }
                    foreach (var pass in DrawShader.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        World.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
                    }

                    World.DefaultShader.EnbleFog = true;
                    try
                    {
                        DwarfGame.SafeSpriteBatchBegin(SpriteSortMode.Deferred,
                            BlendState.NonPremultiplied, Drawer2D.PointMagLinearMin, null, RasterizerState.CullNone, null,
                            Matrix.Identity);
                        Viewport viewPort = new Viewport(RenderTarget.Bounds);

                        foreach (var icon in World.ComponentManager.GetMinimapIcons())
                        {
                            if (!icon.Parent.IsVisible)
                                continue;

                            Vector3 screenPos = viewPort.Project(icon.GlobalTransform.Translation, Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);

                            if (RenderTarget.Bounds.Contains((int)screenPos.X, (int)screenPos.Y))
                            {

                                GameComponent parentBody = icon.Parent as GameComponent;
                                if (parentBody != null)
                                {
                                    if (parentBody.Position.Y > World.Master.MaxViewingLevel + 1)
                                        continue;
                                    var firstVisible = VoxelHelpers.FindFirstVisibleVoxelOnRay(World.ChunkManager.ChunkData, parentBody.Position, parentBody.Position + Vector3.Up * World.WorldSizeInVoxels.Y);
                                    if (firstVisible.IsValid)
                                        continue;
                                }

                                DwarfGame.SpriteBatch.Draw(icon.Icon.SafeGetImage(), new Vector2(screenPos.X, screenPos.Y), icon.Icon.SourceRect, Color.White, 0.0f, new Vector2(icon.Icon.SourceRect.Width / 2.0f, icon.Icon.SourceRect.Height / 2.0f), icon.IconScale, SpriteEffects.None, 0);
                            }
                        }
                    }

                    finally
                    {
                        sprites.End();
                    }

                    World.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                    World.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                    World.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                    World.GraphicsDevice.SamplerStates[0] = Drawer2D.PointMagLinearMin;
                    World.GraphicsDevice.SetRenderTarget(null);
            }
            catch (Exception exception)
            {
                Console.Out.WriteLine(exception);
            }
        }

        public void Dispose()
        {
            if (RenderTarget != null && !RenderTarget.IsDisposed)
                RenderTarget.Dispose();
        }
    }
}
