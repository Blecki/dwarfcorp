using Microsoft.Xna.Framework;

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


        public virtual void RenderUnitialized(DwarfTime DwarfTime)
        {
        }

        public virtual void Update(DwarfTime DwarfTime)
        {
        }

        public virtual void Render(DwarfTime DwarfTime)
        {
        }


    }

}