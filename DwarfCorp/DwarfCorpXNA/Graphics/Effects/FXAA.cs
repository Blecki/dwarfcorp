using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class FXAA : ScreenSpaceComponent
    {
        #region Fields

        private Effect Shader { get; set; }

        // Choose what display settings the FXAA shader should use.
        public FXAASettings Settings
        {
            get { return settings; }
            set { settings = value; }
        }

        private FXAASettings settings = FXAASettings.PresetSettings[0];

        #endregion

        #region Initialization

        public FXAA(Game game) : base(game) { }

        /// <summary>
        /// Load your graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();

            Shader = Game.Content.Load<Effect>(ContentPaths.Shaders.FXAA);
            sceneRenderTarget = new RenderTarget2D(GraphicsDevice, width, height, false, 
                format, DepthFormat.None);

            SetParameters();
            Shader.CurrentTechnique = Shader.Techniques["FXAA"];

        }

        public void SetParameters()
        {
            Viewport viewport = GraphicsDevice.Viewport;
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);
            Matrix halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0);

            Shader.Parameters["World"].SetValue(Matrix.Identity);
            Shader.Parameters["View"].SetValue(Matrix.Identity);
            Shader.Parameters["Projection"].SetValue(halfPixelOffset * projection);
            Shader.Parameters["InverseViewportSize"].SetValue(new Vector2(1f / viewport.Width, 1f / viewport.Height));
            Shader.Parameters["ConsoleSharpness"].SetValue(new Vector4(
                -Settings.N / viewport.Width,
                -Settings.N / viewport.Height,
                Settings.N / viewport.Width,
                Settings.N / viewport.Height
                ));
            Shader.Parameters["ConsoleOpt1"].SetValue(new Vector4(
                -2.0f / viewport.Width,
                -2.0f / viewport.Height,
                2.0f / viewport.Width,
                2.0f / viewport.Height
                ));
            Shader.Parameters["ConsoleOpt2"].SetValue(new Vector4(
                8.0f / viewport.Width,
                8.0f / viewport.Height,
                -4.0f / viewport.Width,
                -4.0f / viewport.Height
                ));
            Shader.Parameters["SubPixelAliasingRemoval"].SetValue(Settings.SubPixelAliasingRemoval);
            Shader.Parameters["EdgeThreshold"].SetValue(Settings.EdgeThreshold);
            Shader.Parameters["EdgeThresholdMin"].SetValue(Settings.EdgeThresholdMin);
            Shader.Parameters["ConsoleEdgeSharpness"].SetValue(Settings.ConsoleEdgeSharpness);
            Shader.Parameters["ConsoleEdgeThreshold"].SetValue(Settings.ConsoleEdgeThreshold);
            Shader.Parameters["ConsoleEdgeThresholdMin"].SetValue(Settings.ConsoleEdgeThresholdMin);
        }

        public override void ValidateBuffers()
        {
            SurfaceFormat format = pp.BackBufferFormat;

            if (sceneRenderTarget == null ||
                sceneRenderTarget.Width != pp.BackBufferWidth ||
                sceneRenderTarget.Height != pp.BackBufferHeight)
            {
                sceneRenderTarget = new RenderTarget2D(GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, pp.BackBufferFormat, DepthFormat.None);

                SetParameters();
            }
        }

        #endregion

        #region Draw

        public override void Draw(GameTime dwarfTime)
        {
            GraphicsDevice.SetRenderTarget(null);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, 
                DepthStencilState.None, RasterizerState.CullNone, Shader, Matrix.Identity);
            spriteBatch.Draw(RenderTarget, GraphicsDevice.Viewport.Bounds, Color.White);
            spriteBatch.End();
        }

        #endregion
    }
}
