// GameStateManager.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace DwarfCorp.GameStates
{

    /// <summary>
    /// Manages a set of game states. A game state is a generic representation of how the game behaves. Game states live in a stack. The state on the top of the stack is the one currently running.
    /// States can be both rendered and updated. There are brief transition periods between states where animations can occur.
    /// </summary>
    public class GameStateManager
    {
        public List<GameState> StateStack { get; private set; }
        //public Dictionary<string, GameState> States { get; set; }
        public DwarfGame Game { get; set; }
        public GameState CurrentState { get; private set; }
        public GameState NextState { get; private set; }

        public Terrain2D ScreenSaver { get; set; }

        private object _mutex = new object();

        public GameStateManager(DwarfGame game)
        {
            Game = game;
            CurrentState = null;
            NextState = null;
            StateStack = new List<GameState>();
        }

        public T GetState<T>() where T : class
        {
            T state = default(T);
            lock (_mutex)
            {
                state = StateStack.OfType<T>().FirstOrDefault();
            }
            return state;
        }

        public void PopState(bool enterNext = true)
        {
            lock (_mutex)
            {
                Game.LogSentryBreadcrumb("GameState", "Popping state.");
                if (StateStack.Count > 0)
                {
                    Game.LogSentryBreadcrumb("GameState", String.Format("Leaving state {0}", StateStack[0].Name));
                    StateStack.RemoveAt(0);
                }

                if (StateStack.Count > 0 && enterNext)
                {
                    var state = StateStack.ElementAt(0);
                    NextState = state;
                    NextState.OnEnter();
                }
            }
        }

        public void ClearState()
        {
            lock (_mutex)
            {
                StateStack.Clear();
                CurrentState = null;
                NextState = null;
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

        public void ReinsertState(GameState state)
        {
            lock (_mutex)
            {
                StateStack.Remove(state);
                PushState(state);
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
                    {
                        CurrentState.OnExit();
                        CurrentState.OnPopped();
                    }

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
                        {
                            //try
                            //{
                            state.Render(time);
                            //}
                            //catch (InvalidOperationException exception)
                            //{
                            //    removals.Add(state);
                            //}
                        }
                        else if (!state.IsInitialized)
                        {
                            state.RenderUnitialized(time);
                        }
                    }
                }
            }
        }
    }

}