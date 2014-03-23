using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class Minimap : ImagePanel
    {
        public RenderTarget2D RenderTarget = null;
        public Texture2D ColorMap { get; set; }
        protected int RenderWidth = 256;
        protected int RenderHeight = 256;

        public OrbitCamera Camera { get; set; }

        public PlayState PlayState { get; set; }

        public Minimap(DwarfGUI gui, GUIComponent parent, int width, int height, PlayState playState, Texture2D colormap):
            base(gui, parent, new ImageFrame())
        {
            ColorMap = colormap;
            RenderWidth = width;
            RenderHeight = height;
            RenderTarget = new RenderTarget2D(GameState.Game.GraphicsDevice, RenderWidth, RenderHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
            Image.Image = RenderTarget;
            Image.SourceRect = new Rectangle(0, 0, RenderWidth, RenderHeight);
            PlayState = playState;

            Camera = new OrbitCamera(0, 0, 0.01f, new Vector3(0, 0, 0), new Vector3(0, 0, 0),  2.5f, 1.0f, 0.1f, 1000.0f)
            {
                Projection = DwarfCorp.Camera.ProjectionMode.Orthographic
            };
        }

        public override void PreRender(GameTime time)
        {
            if(IsVisible)
            {
                Camera.Update(time, PlayState.ChunkManager);
                Camera.Target = PlayState.Camera.Target + Vector3.Up * 50;
                Camera.Phi = -(float) Math.PI * 0.5f;
                Camera.Theta = PlayState.Camera.Theta;
                Camera.UpdateProjectionMatrix();

                PlayState.GraphicsDevice.SetRenderTarget(RenderTarget);
                PlayState.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0, 0);
                PlayState.DefaultShader.Parameters["xView"].SetValue(Camera.ViewMatrix);
                PlayState.DefaultShader.Parameters["xEnableFog"].SetValue(false);

                PlayState.DefaultShader.Parameters["xProjection"].SetValue(Camera.ProjectionMatrix);
                PlayState.DefaultShader.CurrentTechnique = PlayState.DefaultShader.Techniques["Textured_colorscale"];

                PlayState.GraphicsDevice.BlendState = BlendState.AlphaBlend;

                PlayState.ChunkManager.RenderAll(Camera, time, PlayState.GraphicsDevice, PlayState.DefaultShader, Matrix.Identity, ColorMap);
                PlayState.WaterRenderer.DrawWaterFlat(PlayState.GraphicsDevice, Camera.ViewMatrix, Camera.ProjectionMatrix, PlayState.DefaultShader, PlayState.ChunkManager);

                PlayState.GraphicsDevice.SetRenderTarget(null);
                PlayState.GraphicsDevice.Textures[0] = null;
                PlayState.GraphicsDevice.Indices = null;
                PlayState.GraphicsDevice.SetVertexBuffer(null);
                PlayState.DefaultShader.Parameters["xEnableFog"].SetValue(true);
            }

            base.PreRender(time);
        }


    }
}
