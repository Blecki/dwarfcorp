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