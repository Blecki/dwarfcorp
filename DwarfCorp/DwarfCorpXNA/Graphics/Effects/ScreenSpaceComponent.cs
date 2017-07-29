using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public abstract class ScreenSpaceComponent : DrawableGameComponent
    {
        #region Fields

        protected SpriteBatch spriteBatch;
        protected RenderTarget2D sceneRenderTarget;

        public RenderTarget2D RenderTarget
        {
            get { return sceneRenderTarget; }
        }
        protected int width = 0;
        protected int height = 0;

        protected PresentationParameters pp;
        protected SurfaceFormat format;

        #endregion

        #region Initialization

        public ScreenSpaceComponent(Game game)
            : base(game)
        {
            if(game == null)
            {
                throw new ArgumentNullException("game");
            }
        }

        /// <summary>
        /// Load your graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            spriteBatch = DwarfCorp.DwarfGame.SpriteBatch;

            // Look up the resolution and format of our main backbuffer.
            pp = GraphicsDevice.PresentationParameters;

            width = pp.BackBufferWidth;
            height = pp.BackBufferHeight;

            format = pp.BackBufferFormat;
        }

        public abstract void ValidateBuffers();

        #endregion

        #region Draw

        /// <summary>
        /// This should be called at the very start of the scene rendering. This
        /// component uses it to redirect drawing into its custom rendertarget, so it
        /// can capture the scene image in preparation for applying the filter.
        /// </summary>
        public void BeginDraw()
        {
            ValidateBuffers();
            if (Visible)
            {
                GraphicsDevice.SetRenderTarget(sceneRenderTarget);
            }
        }

        #endregion
    }
}
