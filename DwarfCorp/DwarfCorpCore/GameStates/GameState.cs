﻿// GameState.cs
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
using System.Threading;
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
        public enum TransitionMode
        {
            Entering,
            Exiting,
            Running
        }

        public static DwarfGame Game { get; set; }
        public string Name { get; set; }
        public GameStateManager StateManager { get; set; }
        public bool IsInitialized { get; set; }
        public float TransitionValue { get; set; }
        public TransitionMode Transitioning { get; set; }
        public bool RenderUnderneath { get; set; }
        public bool IsActiveState { get; set; }
        public bool EnableScreensaver { get; set; }

        public GameState(DwarfGame game, string name, GameStateManager stateManager)
        {
            EnableScreensaver = true;
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

    public class WaitStateException : Exception {
        public WaitStateException(string message) : base(message) { }
    }

    public class WaitState : GameState
    {
        public Thread WaitThread { get; set; }
        public DwarfGUI GUI { get; set; }

        public event Finished OnFinished;

        protected virtual void OnOnFinished()
        {
            Finished handler = OnFinished;
            if (handler != null) handler(true, null);
        }
        public bool Done { get; protected set; }
        public delegate void Finished(bool success, WaitStateException e);

        /// <summary>
        /// the routine to be run in a thread while the wait state is active
        /// </summary>
        /// <returns>success</returns>
        public delegate bool WaitThreadRoutine();

        public WaitStateException exception;
        public bool success = false;

        public WaitState(DwarfGame game, string name, GameStateManager stateManager, WaitThreadRoutine routine, DwarfGUI gui)
            : base(game, name, stateManager)
        {
            WaitThread = new Thread(() => runRoutine(routine));
            GUI = gui;
            OnFinished = (Boolean, Exception) => { };
            Done = false;
        }

        protected void runRoutine(WaitThreadRoutine routine)
        {
            try
            {
                success = routine();
            } catch (WaitStateException e)  // allows any exceptions we didn't throw on purpose to be thrown properly
            {
                this.exception = e;
            }
        }

        public override void OnEnter()
        {
            IsInitialized = true;
            WaitThread.Start();
            base.OnEnter();
        }

        public override void OnPopped()
        {
            StateManager.States.Remove(Name);
            OnFinished.Invoke(success, exception);
            base.OnPopped();
        }

        public override void Update(DwarfTime gameTime)
        {
            GUI.MouseMode = GUISkin.MousePointer.Wait;

            if (!WaitThread.IsAlive && StateManager.CurrentState == Name && !Done)
            {
                StateManager.PopState();
                Done = true;
            }

            base.Update(gameTime);
        }

    }

}