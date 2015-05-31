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
        public List<string> StateStack { get; set; }
        public Dictionary<string, GameState> States { get; set; }
        public DwarfGame Game { get; set; }
        public string CurrentState { get; set; }
        public string NextState { get; set; }
        public float TransitionSpeed { get; set; }

        public Terrain2D ScreenSaver { get; set; }

        public GameStateManager(DwarfGame game)
        {
            Game = game;
            States = new Dictionary<string, GameState>();
            CurrentState = "";
            NextState = "";
            TransitionSpeed = 2.0f;
            StateStack = new List<string>();
        }

        public T GetState<T>(string name) where T : class
        {
            if(States.ContainsKey(name) && States[name] is T)
            {
                return States[name] as T;
            }
            else return null;
        }

        public void PopState()
        {
            if(StateStack.Count > 0)
            {
                StateStack.RemoveAt(0);
            }
            if(StateStack.Count > 0)
            {
                string state = StateStack.ElementAt(0);

                NextState = state;
                States[NextState].OnEnter();
                States[NextState].TransitionValue = 0.0f;
                States[NextState].Transitioning = GameState.TransitionMode.Entering;

                if(CurrentState != "")
                {
                    States[CurrentState].Transitioning = GameState.TransitionMode.Exiting;
                    States[CurrentState].TransitionValue = 0.0f;
                }
            }
        }


        public void PushState(GameState state)
        {
            States.Add(state.Name, state);
            PushState(state.Name);
        }

        public void PushState(string state)
        {
            NextState = state;
            States[NextState].OnEnter();
            States[NextState].TransitionValue = 0.0f;
            States[NextState].Transitioning = GameState.TransitionMode.Entering;

            if(CurrentState != "")
            {
                States[CurrentState].Transitioning = GameState.TransitionMode.Exiting;
                States[CurrentState].TransitionValue = 0.0f;
            }

            StateStack.Insert(0, state);
        }

        private void TransitionComplete()
        {
            if(CurrentState != "")
            {
                States[CurrentState].OnExit();
                States[CurrentState].Transitioning = GameState.TransitionMode.Exiting;
                States[CurrentState].OnPopped();
            }

            CurrentState = NextState;
            States[CurrentState].Transitioning = GameState.TransitionMode.Running;
            NextState = "";
        }

        public void Update(DwarfTime time)
        {
            if(ScreenSaver == null)
            {
                ScreenSaver = new Terrain2D(Game);
            }
            if(CurrentState != "" && States[CurrentState].IsInitialized)
            {
                States[CurrentState].Update(time);

                if(CurrentState != "" && States[CurrentState].Transitioning != GameState.TransitionMode.Running)
                {
                    States[CurrentState].TransitionValue += (float) (TransitionSpeed * time.ElapsedRealTime.TotalSeconds);
                    States[CurrentState].TransitionValue = Math.Min(States[CurrentState].TransitionValue, 1.001f);
                }
            }

            if(NextState != "" && States[NextState].IsInitialized)
            {
                //States[NextState].Update(time);

                if(States[NextState].Transitioning != GameState.TransitionMode.Running)
                {
                    States[NextState].TransitionValue += (float) (TransitionSpeed * time.ElapsedRealTime.TotalSeconds);
                    States[NextState].TransitionValue = Math.Min(States[NextState].TransitionValue, 1.001f);
                    if(States[NextState].TransitionValue >= 1.0)
                    {
                        TransitionComplete();
                    }
                }
            }
        }

        public void Render(DwarfTime time)
        {
            Game.GraphicsDevice.Clear(Color.Black);

            if(CurrentState != "" && States[CurrentState].EnableScreensaver)
            {
                ScreenSaver.Render(Game.GraphicsDevice, DwarfGame.SpriteBatch, time);
            }
            for(int i = StateStack.Count - 1; i >= 0; i--)
            {
                GameState state = States[StateStack[i]];

                if(state.RenderUnderneath || i == 0 || state.Name == CurrentState || state.Name == NextState)
                {
                    if(state.IsInitialized)
                    {
                        state.Render(time);
                    }
                    else if(!state.IsInitialized)
                    {
                        state.RenderUnitialized(time);
                    }
                }
            }
        }
    }

}