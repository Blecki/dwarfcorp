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
    public class GameState
    {
        public enum TransitionMode
        {
            Entering,
            Exiting,
            Running
        }

        public DwarfGame Game { get; set; }
        public string Name { get; set; }
        public GameStateManager StateManager { get; set; }
        public bool IsInitialized { get; set; }
        public float TransitionValue { get; set; }
        public TransitionMode Transitioning { get; set; }
        public bool RenderUnderneath { get; set; }
        public bool IsActiveState { get; set; }

        public GameState(DwarfGame game, string name, GameStateManager stateManager)
        {
            Game = game;
            Name = name;
            StateManager = stateManager;
            IsInitialized = false;
            TransitionValue = 0.0f;
            Transitioning = TransitionMode.Entering;
            RenderUnderneath = false;
            IsActiveState = false;
        }

        public virtual void OnEnter()
        {
            IsActiveState = true;
            TransitionValue = 0.0f;
            Transitioning = TransitionMode.Entering;
        }

        public virtual void OnExit()
        {
            IsActiveState = false;
            TransitionValue = 0.0f;
            Transitioning = TransitionMode.Exiting;
        }


        public virtual void RenderUnitialized(GameTime gameTime)
        {

        }

        public virtual void Update(GameTime gameTime)
        {

        }

        public virtual void Render(GameTime gameTime)
        {

        }
    }
}
