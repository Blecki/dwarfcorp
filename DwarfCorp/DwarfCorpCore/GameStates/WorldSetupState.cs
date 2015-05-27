using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This game state allows the player to load generated worlds from files.
    /// </summary>
    public class WorldSetupState : GameState
    {

        public WorldSetupState(DwarfGame game, GameStateManager stateManager) :
            base(game, "WorldSetupState", stateManager)
        {
            IsInitialized = false;
        }

        public void CreateGUI()
        {
            
        }
        public override void OnEnter()
        {
            CreateGUI();
            IsInitialized = true;
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }
        public override void Update(DwarfTime gameTime)
        {
            base.Update(gameTime);
        }


        private void DrawGUI(DwarfTime gameTime, float dx)
        {

        }

        public override void Render(DwarfTime gameTime)
        {
            switch(Transitioning)
            {
                case TransitionMode.Running:
                    DrawGUI(gameTime, 0);
                    break;
                case TransitionMode.Entering:
                {
                    float dx = Easing.CubeInOut(TransitionValue, -Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Width, 1.0f);
                    DrawGUI(gameTime, dx);
                }
                    break;
                case TransitionMode.Exiting:
                {
                    float dx = Easing.CubeInOut(TransitionValue, 0, Game.GraphicsDevice.Viewport.Width, 1.0f);
                    DrawGUI(gameTime, dx);
                }
                    break;
            }

            base.Render(gameTime);
        }
    }

}