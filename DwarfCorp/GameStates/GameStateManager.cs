using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace DwarfCorp.GameStates
{
    public class GameStateManager
    {
        private List<GameState> StateStack;
        public bool StateStackIsEmpty => StateStack.Count == 0;
        public DwarfGame Game { get; set; }
        private GameState CurrentState;
        private GameState NextState;

        public Terrain2D ScreenSaver { get; set; }

        private object _mutex = new object();

        public GameStateManager(DwarfGame game)
        {
            Game = game;
            CurrentState = null;
            NextState = null;
            StateStack = new List<GameState>();
        }

        public void PopState(bool enterNext = true)
        {
            lock (_mutex)
            {
                if (StateStack.Count > 0)
                {
                    Game.LogSentryBreadcrumb("GameState", String.Format("Leaving state {0}", StateStack[0].GetType().FullName));
                    StateStack[0].OnPopped();
                    StateStack.RemoveAt(0);
                }

                if (StateStack.Count > 0 && enterNext)
                {
                    var state = StateStack.ElementAt(0);
                    NextState = state;
                    NextState.OnEnter(); // Split into OnEnter and OnExposed
                }
            }
        }

        public void ClearState()
        {
            lock (_mutex)
            {
                while (StateStack.Count > 0)
                    PopState(false);
            }
        }

        public void PushState(GameState state)
        {
            lock (_mutex)
            {
                NextState = state;
                NextState.OnEnter();
                StateStack.Insert(0, NextState);
            }
        }

        public void Update(DwarfTime time)
        {
#if DEBUG
            Microsoft.Xna.Framework.Input.KeyboardState k = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            if (k.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Home))
            {
                try
                {
                    GameState.Game.GraphicsDevice.Reset();
                }
                catch (Exception exception)
                {

                }
            }
#endif

            if (ScreenSaver == null)
                ScreenSaver = new Terrain2D(Game);

            lock (_mutex)
            {
                if (NextState != null && NextState.IsInitialized)
                {
                    if (CurrentState != null)
                        CurrentState.OnCovered();

                    CurrentState = NextState;
                    NextState = null;
                }

                if (CurrentState != null && CurrentState.IsInitialized)
                    CurrentState.Update(time);
            }
        }

        public void Render(DwarfTime time)
        {
            lock (_mutex)
            {
                Game.GraphicsDevice.Clear(Color.Black);

                if (CurrentState != null && CurrentState.EnableScreensaver)
                    ScreenSaver.Render(Game.GraphicsDevice, time);

                for (int i = StateStack.Count - 1; i >= 0; i--)
                {
                    GameState state = StateStack[i];

                    if (state != null &&
                        (state.RenderUnderneath || i == 0
                        || Object.ReferenceEquals(state, CurrentState)
                        || Object.ReferenceEquals(state, NextState)))
                    {
                        if (state.IsInitialized)
                            state.Render(time);
                        else if (!state.IsInitialized)
                            state.RenderUnitialized(time);
                    }
                }
            }
        }
    }

}