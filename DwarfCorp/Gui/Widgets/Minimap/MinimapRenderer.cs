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

namespace DwarfCorp.Gui.Widgets.Minimap
{
    public class MinimapRenderer : IDisposable
    {
        public Vector3 HomePosition { get; set; }
        protected bool HomeSet = false;
        public RenderTarget2D RenderTarget = null;
        public int RenderWidth = 196;
        public int RenderHeight = 196;
        public OrbitCamera Camera { get; set; }
        public WorldManager World { get; set; }
        private BasicEffect DrawShader = null;
        private VertexPositionTexture[] quad2 = null;

        private Dictionary<GlobalChunkCoordinate, MinimapCell> Cells = new Dictionary<GlobalChunkCoordinate, MinimapCell>();

        public MinimapRenderer(int width, int height, WorldManager world)
        {
            RenderWidth = width;
            RenderHeight = height;
            RenderTarget = new RenderTarget2D(GameState.Game.GraphicsDevice, RenderWidth, RenderHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
            World = world;
            DrawShader = new BasicEffect(World.GraphicsDevice);

            Camera = new OrbitCamera(World, new Vector3(0, 0, 0), new Vector3(0, 0, 0), 2.5f, 1.0f, 0.1f, 1000.0f)
            {
                Projection = DwarfCorp.Camera.ProjectionMode.Orthographic
            };

            quad2 = new VertexPositionTexture[]
            {
                new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(VoxelConstants.ChunkSizeX, 0, 0), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(VoxelConstants.ChunkSizeX, 0, VoxelConstants.ChunkSizeZ), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(VoxelConstants.ChunkSizeX, 0, VoxelConstants.ChunkSizeZ), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(0, 0, VoxelConstants.ChunkSizeZ), new Vector2(0, 1)),
                new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 0)),
            };
        }

        public void OnClicked(int X, int Y)
        {
            Viewport viewPort = new Viewport(RenderTarget.Bounds);
            Vector3 forward = (World.Renderer.Camera.Target - World.Renderer.Camera.Position);
            forward.Normalize();

            Vector3 pos = viewPort.Unproject(new Vector3(X, Y, 0), Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);
            Vector3 target = new Vector3(pos.X, World.Renderer.Camera.Position.Y, pos.Z);
            var height = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                World.ChunkManager, GlobalVoxelCoordinate.FromVector3(target)))
                .Coordinate.Y + 1;
            target.Y = Math.Max(height + 15, target.Y);
            target = MathFunctions.Clamp(target, World.ChunkManager.Bounds);
            World.Renderer.Camera.SetZoomTarget(target);
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
            World.Renderer.Camera.UpdateViewMatrix();
            World.Renderer.Camera.SetZoomTarget(HomePosition);
            World.Renderer.SetMaxViewingLevel(World.WorldSizeInVoxels.Y);
        }

        public void Render(Rectangle Where, Gui.Root Gui)
        {
            Gui.DrawQuad(Where, RenderTarget);
        }

        private void ReDrawChunks()
        {
            var cellToDraw = World.ChunkManager.PopInvalidColumn();
            if (cellToDraw.HasValue)
                do
                {
                    if (!Cells.ContainsKey(cellToDraw.Value))
                        Cells.Add(cellToDraw.Value, new MinimapCell(World.GraphicsDevice));
                    Cells[cellToDraw.Value].RedrawFromColumn(cellToDraw.Value, World.ChunkManager);

                    cellToDraw = World.ChunkManager.PopInvalidColumn();
                } while (cellToDraw.HasValue);
        }

        public void ValidateShader()
        {
        }

        public void PreRender(SpriteBatch sprites)
        {
            if (sprites.IsDisposed || sprites.GraphicsDevice.IsDisposed)
                return;

            try
            {
                if (DrawShader != null && (DrawShader.IsDisposed || DrawShader.GraphicsDevice.IsDisposed))
                    DrawShader = new BasicEffect(World.GraphicsDevice);

                if (RenderTarget.IsDisposed || RenderTarget.IsContentLost)
                {
                    World.ChunkManager.NeedsMinimapUpdate = true;
                    RenderTarget = new RenderTarget2D(GameState.Game.GraphicsDevice, RenderWidth, RenderHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
                    return;
                }

                if (!HomeSet)
                {
                    if (World.EnumerateZones().Any())
                        HomePosition = World.EnumerateZones().First().GetBoundingBox().Center();
                    HomeSet = true;
                }

                ReDrawChunks();

                World.GraphicsDevice.SetRenderTarget(RenderTarget);
                World.GraphicsDevice.Clear(Color.Black);
                Camera.Target = World.Renderer.Camera.Target;
                Vector3 cameraToTarget = World.Renderer.Camera.Target - World.Renderer.Camera.Position;
                cameraToTarget.Normalize();
                Camera.Position = World.Renderer.Camera.Target + Vector3.Up * 50 - cameraToTarget * 4;
                Camera.UpdateViewMatrix();
                Camera.UpdateProjectionMatrix();
                World.Renderer.DefaultShader.View = Camera.ViewMatrix;
                World.Renderer.DefaultShader.Projection = Camera.ProjectionMatrix;
                var bounds = World.ChunkManager.Bounds;
                DrawShader.TextureEnabled = true;
                DrawShader.LightingEnabled = false;
                DrawShader.Projection = Camera.ProjectionMatrix;
                DrawShader.View = Camera.ViewMatrix;
                DrawShader.World = Matrix.Identity;
                DrawShader.VertexColorEnabled = false;
                DrawShader.Alpha = 1.0f;

                foreach (var cell in Cells)
                {
                    DrawShader.Texture = cell.Value.Texture;
                    DrawShader.World = Matrix.CreateTranslation(cell.Key.X * VoxelConstants.ChunkSizeX, 0.0f, cell.Key.Z * VoxelConstants.ChunkSizeZ);

                    foreach (var pass in DrawShader.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        World.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, quad2, 0, 2);
                    }
                }

                World.Renderer.DefaultShader.EnbleFog = true;

                try
                {
                    DwarfGame.SafeSpriteBatchBegin(SpriteSortMode.Deferred,
                        BlendState.NonPremultiplied, Drawer2D.PointMagLinearMin, null, RasterizerState.CullNone, null,
                        Matrix.Identity);

                    var viewPort = new Viewport(RenderTarget.Bounds);

                    if (World.ModuleManager.GetModule<MinimapIconModule>().HasValue(out var iconModule))
                        foreach (var icon in iconModule.GetMinimapIcons())
                        {
                            if (!icon.Parent.HasValue(out var iconParent))
                                continue;

                            if (!iconParent.IsVisible)
                                continue;

                            var screenPos = viewPort.Project(icon.GlobalTransform.Translation, Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);

                            if (RenderTarget.Bounds.Contains((int)screenPos.X, (int)screenPos.Y))
                            {

                                if (iconParent.Position.Y > World.Renderer.PersistentSettings.MaxViewingLevel + 1)
                                    continue;
                                var firstVisible = VoxelHelpers.FindFirstVisibleVoxelOnRay(World.ChunkManager, iconParent.Position, iconParent.Position + Vector3.Up * World.WorldSizeInVoxels.Y);
                                if (firstVisible.IsValid)
                                    continue;

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
