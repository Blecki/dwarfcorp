using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Gum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.NewGui
{
    public class MinimapFrame : Gum.Widget
    {
        public String Frame = "minimap-frame";
        public MinimapRenderer Renderer;
        public Widget RestoreButton;

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

            var buttonRow = AddChild(new Gum.Widget
            {
                Transparent = true,
                MinimumSize = new Point(0, 20),
                AutoLayout = Gum.AutoLayout.DockTop,
                Padding = new Gum.Margin(2, 2, 2, 2)
            });

            buttonRow.AddChild(new Gum.Widgets.ImageButton
                {
                    Background = new Gum.TileReference("round-buttons", 0),
                    MinimumSize = new Point(16, 16),
                    MaximumSize = new Point(16, 16),
                    AutoLayout = Gum.AutoLayout.DockLeft,
                    OnClick = (sender, args) => Renderer.ZoomIn(),
                    Tooltip = "Zoom in"
                });

            buttonRow.AddChild(new Gum.Widgets.ImageButton
            {
                Background = new Gum.TileReference("round-buttons", 1),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gum.AutoLayout.DockLeft,
                OnClick = (sender, args) => Renderer.ZoomOut(),
                Tooltip = "Zoom out"
            });

            buttonRow.AddChild(new Gum.Widgets.ImageButton
            {
                Background = new Gum.TileReference("round-buttons", 2),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gum.AutoLayout.DockLeft,
                OnClick = (sender, args) => Renderer.ZoomHome(),
                Tooltip = "Zoom to home base"
            });

            buttonRow.AddChild(new Gum.Widgets.ImageButton
            {
                Background = new Gum.TileReference("round-buttons", 7),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gum.AutoLayout.DockLeft,
                OnClick = (sender, args) =>
                    {
                        RestoreButton.Hidden = false;
                        RestoreButton.Invalidate();
                        this.Hidden = true;
                        this.Invalidate();
                    },
                Tooltip = "Minimize the map"
            });

            base.Construct();
        }

        protected override Gum.Mesh Redraw()
        {
            return Gum.Mesh.CreateScale9Background(Rect, Root.GetTileSheet("tray-border-transparent"), Scale9Corners.Top | Scale9Corners.Right);
        }

    }

    public class MinimapRenderer
    {
        public Vector3 HomePosition { get; set; }
        protected bool HomeSet = false;
        public RenderTarget2D RenderTarget = null;
        public Texture2D ColorMap { get; set; }
        public int RenderWidth = 196;
        public int RenderHeight = 196;
        public OrbitCamera Camera { get; set; }
        public WorldManager World { get; set; }

        public MinimapRenderer(int width, int height, WorldManager world, Texture2D colormap)
        {
            ColorMap = colormap;

            RenderWidth = width;
            RenderHeight = height;
            RenderTarget = new RenderTarget2D(GameState.Game.GraphicsDevice, RenderWidth, RenderHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
            World = world;

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

            Vector3 pos = viewPort.Unproject(new Vector3(X, Y, 0), Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity) - forward * 10;
            Vector3 target = new Vector3(pos.X, World.Camera.Target.Y, pos.Z);
            float height = World.ChunkManager.ChunkData.GetFilledVoxelGridHeightAt(target.X, target.Y, target.Z);
            target.Y = Math.Max(height + 15, target.Y);
            target = MathFunctions.Clamp(target, World.ChunkManager.Bounds);
            World.Camera.ZoomTargets.Clear();
            World.Camera.ZoomTargets.Add(target);
        }

        public void ZoomOut()
        {
            Camera.FOV = Math.Min(Camera.FOV * 1.1f, (float)Math.PI);
            Camera.UpdateProjectionMatrix();
        }

        public void ZoomIn()
        {
            Camera.FOV = Math.Max(Camera.FOV * 0.9f, 0.1f);
            Camera.UpdateProjectionMatrix();
        }

        public void ZoomHome()
        {
            World.Camera.UpdateViewMatrix();
            World.Camera.ZoomTargets.Clear();
            World.Camera.ZoomTargets.Add(HomePosition);
        }

               
        public void Render(Rectangle Where, Gum.Root Gui)
        {
            Gui.DrawQuad(Where, RenderTarget);
        }

        public void PreRender(DwarfTime time, SpriteBatch sprites)
        {
            if (!HomeSet)
            {
                HomePosition = World.Camera.Position;
                HomeSet = true;
            }


            //Camera.Update(time, World.ChunkManager);
            Camera.Target = World.Camera.Target;
            Vector3 cameraToTarget = World.Camera.Target - World.Camera.Position;
            cameraToTarget.Normalize();
            Camera.Position = World.Camera.Target + Vector3.Up*50 - cameraToTarget * 4;
            Camera.UpdateViewMatrix();
            Camera.UpdateProjectionMatrix();

            World.GraphicsDevice.SetRenderTarget(RenderTarget);

            World.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0, 0);
            World.DefaultShader.View = Camera.ViewMatrix;
            World.DefaultShader.EnbleFog = false;

            World.DefaultShader.Projection = Camera.ProjectionMatrix;
            World.DefaultShader.CurrentTechnique = World.DefaultShader.Techniques[Shader.Technique.TexturedWithColorScale];

            World.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            World.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            
            World.ChunkManager.RenderAll(Camera, time, World.GraphicsDevice, World.DefaultShader, Matrix.Identity, ColorMap);
            World.WaterRenderer.DrawWaterFlat(World.GraphicsDevice, Camera.ViewMatrix, Camera.ProjectionMatrix, World.DefaultShader, World.ChunkManager);
            World.GraphicsDevice.Textures[0] = null;
            World.GraphicsDevice.Indices = null;
            World.GraphicsDevice.SetVertexBuffer(null);

            World.DefaultShader.EnbleFog = true;

            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, null, RasterizerState.CullNone);
            Viewport viewPort = new Viewport(RenderTarget.Bounds);
            foreach (MinimapIcon icon in GameObjectCaching.MinimapIcons)
            {
                if (!icon.Parent.IsVisible)
                {
                    continue;
                }

                Vector3 screenPos = viewPort.Project(icon.GlobalTransform.Translation, Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);

                if (RenderTarget.Bounds.Contains((int)screenPos.X, (int)screenPos.Y))
                {
                    DwarfGame.SpriteBatch.Draw(icon.Icon.Image, new Vector2(screenPos.X, screenPos.Y), icon.Icon.SourceRect, Color.White, 0.0f, new Vector2(icon.Icon.SourceRect.Width / 2.0f, icon.Icon.SourceRect.Height / 2.0f), icon.IconScale, SpriteEffects.None, 0);
                }
            }

            DwarfGame.SpriteBatch.End();

            World.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            World.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            World.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            World.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            World.GraphicsDevice.SetRenderTarget(null);
        }
    }
}
