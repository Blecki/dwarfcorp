using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;

namespace DwarfCorp
{

    public class MetaState
    {
        public State StartState { get; set; }
        public ConcurrentDictionary<string, State> States { get; set; }
        public string Name { get; set; }
        public State CurrentState { get; set; }
        public StateMachine Parent { get; set; }
        public bool ShouldTransition { get; set; }
        public string TransitionTo { get; set; }

        public MetaState(string name, StateMachine stateMachine)
        {
            Name = name;
            StartState = null;
            CurrentState = null;
            Parent = stateMachine;
            States = new ConcurrentDictionary<string, State>();
            ShouldTransition = false;
            TransitionTo = "";
        }

        public void AddState(State state)
        {
            States[state.Name] = state;
            state.Parent = this;
        }

        public void RemoveState(State state)
        {
            if (States.ContainsKey(state.Name))
            {
                State dummy = null;
                while (!States.TryRemove(state.Name, out dummy))
                {
                    // nothing.
                }
            }
        }

        public virtual void OnEnter()
        {
            CurrentState = StartState;

            if (CurrentState != null)
            {
                CurrentState.OnEnter();
            }
        }

        public virtual void OnExit()
        {
            if (CurrentState != null)
            {
                CurrentState.OnExit();
            }
        }

        public virtual void Update(GameTime time)
        {
            if (CurrentState != null)
            {
                CurrentState.Update(time);

                if (CurrentState.ShouldTransition)
                {
                    if (States.ContainsKey(CurrentState.TransitionTo))
                    {
                        CurrentState.OnExit();
                        CurrentState = States[CurrentState.TransitionTo];
                        CurrentState.OnEnter();
                    }
                }
            }
        }

        public virtual void Render(GameTime time, GraphicsDevice device)
        {
            if (CurrentState != null)
            {
                CurrentState.Render(time, device);
            }
        }

    }

    public class State
    {
        public string Name { get; set; }
        public MetaState Parent { get; set;}
        public bool ShouldTransition { get; set; }
        public string TransitionTo { get; set; }

        public State(string name) 
        {
            Name = name;
            ShouldTransition = false;
            TransitionTo = "";
        }

        public virtual void OnEnter()
        {

        }
        public virtual void OnExit()
        {

        }

        public virtual void Update(GameTime time)
        {

        }

        public virtual void Render(GameTime time, GraphicsDevice device)
        {
        }

    }

    public class StateMachine
    {
        public MetaState StartState { get; set; }
        public ConcurrentDictionary<string, MetaState> States { get; set; }
        public string Name { get; set; }
        public MetaState CurrentState { get; set; }
        
        public StateMachine(string name)
        {
            Name = name;
            StartState = null;
            CurrentState = null;
            States = new ConcurrentDictionary<string, MetaState>();
        }

        public void AddState(MetaState state)
        {
            States[state.Name] = state;
            state.Parent = this;
        }

        public void RemoveState(MetaState state)
        {
            if (States.ContainsKey(state.Name))
            {
                MetaState dummy = null;
                while (!States.TryRemove(state.Name, out dummy))
                {
                    // nothing.
                }
            }
        }

        public virtual void OnEnter()
        {
            CurrentState = StartState;

            if (CurrentState != null)
            {
                CurrentState.OnEnter();
            }
        }

        public virtual void OnExit()
        {
            if (CurrentState != null)
            {
                CurrentState.OnExit();
            }
        }

        public virtual void Update(GameTime time)
        {
            if (CurrentState != null)
            {
                CurrentState.Update(time);

                if (CurrentState.ShouldTransition)
                {
                    if (States.ContainsKey(CurrentState.TransitionTo))
                    {
                        CurrentState.OnExit();
                        CurrentState = States[CurrentState.TransitionTo];
                        CurrentState.OnEnter();
                    }
                }
            }
        }

        public virtual void Render(GameTime time, GraphicsDevice device)
        {
            if (CurrentState != null)
            {
                CurrentState.Render(time, device);
            }
        }
    }

}
