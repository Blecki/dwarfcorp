using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp.GameStates
{

    /// <summary>
    ///  This game state displays the company and game credits or whatever else needs to go at the beginning of the game.
    /// </summary>
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
            Logo = TextureManager.GetTexture("CompanyLogo");
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

            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            Vector2 screenCenter = new Vector2(Game.GraphicsDevice.Viewport.Width / 2 - Logo.Width / 2, Game.GraphicsDevice.Viewport.Height / 2 - Logo.Height / 2);
            switch(Transitioning)
            {
                case TransitionMode.Running:
                    DwarfGame.SpriteBatch.Draw(Logo, screenCenter, null, new Color(1f, 1f, 1f));
                    break;
                case TransitionMode.Entering:
                    DwarfGame.SpriteBatch.Draw(Logo, screenCenter, null, new Color(1f, 1f, 1f, TransitionValue));
                    break;
                case TransitionMode.Exiting:
                    DwarfGame.SpriteBatch.Draw(Logo, screenCenter, null, new Color(1f, 1f, 1f, 1.0f - TransitionValue));
                    break;
            }
            DwarfGame.SpriteBatch.End();

            base.Render(gameTime);
        }
    }

}