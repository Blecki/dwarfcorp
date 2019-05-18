using System.Globalization;
using System.Threading;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using System;

namespace DwarfCorp.GameStates
{
    /// <summary>
    /// A game state is a generic representation of how the game behaves. Game states live in a stack. The state on the top of the stack is the one currently running.
    /// States can be both rendered and updated. There are brief transition periods between states where animations can occur.
    /// </summary>
    public class GameState
    {
        public static DwarfGame Game { get; set; } // Todo: Kill?
        public bool IsInitialized { get; set; }
        public bool RenderUnderneath { get; set; }
        public bool IsActiveState { get; set; }
        public bool EnableScreensaver { get; set; }

        public GameState(DwarfGame game)
        {
            EnableScreensaver = true;
            Game = game;
            IsInitialized = false;
            RenderUnderneath = false;
            IsActiveState = false;
        }

        public virtual void OnEnter()
        {
            IsActiveState = true;
            DwarfGame.LogSentryBreadcrumb("GameState", this.GetType().FullName);
        }

        public virtual void OnCovered()
        {
            IsActiveState = false;
        }

        public virtual void RenderUnitialized(DwarfTime gameTime)
        {
        }

        public virtual void Update(DwarfTime gameTime)
        {
        }

        public virtual void Render(DwarfTime gameTime)
        {
        }

        public virtual void OnPopped()
        {

        }
    }
}