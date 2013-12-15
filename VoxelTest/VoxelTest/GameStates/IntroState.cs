using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace DwarfCorp
{

    public class IntroState : GameState
    {
        public Texture2D Logo { get; set; }
        public Timer IntroTimer = new Timer(1, true);

        public IntroState(DwarfGame game, GameStateManager stateManager) :
            base(game, "IntroState", stateManager)
        {
            ResourceLibrary library = new ResourceLibrary(game);
        }


        public override void OnEnter()
        {
            IsInitialized = true;
            Logo = TextureManager.GetTexture("companylogo");
            IntroTimer.Reset(3);

            base.OnEnter();
        }


        public override void Update(GameTime gameTime)
        {
            Game.IsMouseVisible = false;
            IntroTimer.Update(gameTime);

            if(IntroTimer.HasTriggered && Transitioning == TransitionMode.Running)
            {
                Game.IsMouseVisible = true;
                StateManager.PushState("MainMenuState");
            }

            if(Keyboard.GetState().GetPressedKeys().Length > 0 && Transitioning == TransitionMode.Running)
            {
                StateManager.PushState("MainMenuState");
            }

            base.Update(gameTime);
        }


        public override void Render(GameTime gameTime)
        {
            DwarfGame.SpriteBatch.Begin();

            float x = Easing.CubeInOut(TransitionValue, 0.0f, 1.0f, 0.5f);

            if(Transitioning == TransitionMode.Running)
            {
                DwarfGame.SpriteBatch.Draw(Logo, new Vector2(Game.GraphicsDevice.Viewport.Width / 2 - Logo.Width / 2, Game.GraphicsDevice.Viewport.Height / 2 - Logo.Height / 2), null, new Color(1f, 1f, 1f));
            }
            else if(Transitioning == TransitionMode.Entering)
            {
                DwarfGame.SpriteBatch.Draw(Logo, new Vector2(Game.GraphicsDevice.Viewport.Width / 2 - Logo.Width / 2, Game.GraphicsDevice.Viewport.Height / 2 - Logo.Height / 2), null, new Color(x, x, x));
            }
            else if(Transitioning == TransitionMode.Exiting)
            {
                DwarfGame.SpriteBatch.Draw(Logo, new Vector2(Game.GraphicsDevice.Viewport.Width / 2 - Logo.Width / 2, Game.GraphicsDevice.Viewport.Height / 2 - Logo.Height / 2), null, new Color(1.0f - x, 1.0f - x, 1.0f - x));
            }
            DwarfGame.SpriteBatch.End();

            base.Render(gameTime);
        }
    }

}