using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates
{
    public class PlayState : GameState, WorldManagerCallback
    {
        public bool ShouldReset { get; set; }
        public WorldManager World { get; set; }
        public static bool Paused
        {
            get { return WorldManager.Paused; }
            set { WorldManager.Paused = value; }
        }

        public static DwarfGUI GUI
        {
            get { return WorldManager.GUI; }
            set { WorldManager.GUI = value; }
        }

        // Displays tips when the game is loading.
        public List<string> LoadingTips = new List<string>()
        {
            "Can't get the right angle? Hold SHIFT to move the camera around!",
            "Need to see tiny dwarves? Use the mousewheel to zoom!",
            "Press Q to quickly slice the terrain at the height of the cursor.",
            "Press E to quickly un-slice the terrain.",
            "The number keys can be used to quickly switch between tools.",
            "Employees will not work if they are unhappy.",
            "Monsters got you down? Try hiring some thugs!",
            "The most lucrative resources are beneath the earth.",
            "Dwarves can swim!",
            "Stockpiles are free!",
            "Payday occurs at midnight. Make sure to sell your goods before then!",
            "Dwarves prefer to eat in common rooms, but they will eat out of stockpiles if necessary.",
            "The minimap can be closed and opened.",
            "Monsters are shown on the minimap.",
            "Axedwarves are better at chopping trees than miners."
        };
        private Timer TipTimer = new Timer(5, false);
        private int TipIndex = 0;

        /// <summary>
        /// Creates a new play state
        /// </summary>
        /// <param name="game">The program currently running</param>
        /// <param name="stateManager">The game state manager this state will belong to</param>
        public PlayState(DwarfGame game, GameStateManager stateManager) :
            base(game, "PlayState", stateManager)
        {
            ShouldReset = true;
            World = new WorldManager(game, this);
            World.gameState = this;
            Paused = false;
            RenderUnderneath = true;
        }

        void WorldManagerCallback.OnLoaded()
        {
            IsInitialized = true;
        }

        void WorldManagerCallback.OnLose()
        {
            //Paused = true;
            //StateManager.PushState("LoseState");
        }

        /// <summary>
        /// Called when the PlayState is entered from the state manager.
        /// </summary>
        public override void OnEnter()
        {
            // If the game should reset, we initialize everything
            if (ShouldReset)
            {
                IsInitialized = false;
                ShouldReset = false;
                World.Reset();
            }
            else
            {
                // Otherwise, we just unpause everything and re-enter the game.
                World.Unpause();
            }
            base.OnEnter();
        }

        /// <summary>
        /// Called when the PlayState is exited and another state (such as the main menu) is loaded.
        /// </summary>
        public override void OnExit()
        {
            World.Pause();
            base.OnExit();
        }

        /// <summary>
        /// Called every frame
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void Update(DwarfTime gameTime)
        {
            // If this playstate is not supposed to be running,
            // just exit.
            if (!Game.IsActive || !IsActiveState)
            {
                return;
            }

            World.Update2(gameTime);
        }

        /// <summary>
        /// Called when a frame is to be drawn to the screen
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void Render(DwarfTime gameTime)
        {
            EnableScreensaver = !World.ShowingWorld;
            World.Render2(gameTime);
            base.Render(gameTime);
        }

        /// <summary>
        /// If the game is not loaded yet, just draws a loading message centered
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void RenderUnitialized(DwarfTime gameTime)
        {
            TipTimer.Update(gameTime);
            if (TipTimer.HasTriggered)
            {
                World.LoadingMessageBottom = LoadingTips[WorldManager.Random.Next(LoadingTips.Count)];
                TipIndex++;
            }

            EnableScreensaver = true;
            World.Render2(gameTime);
            base.RenderUnitialized(gameTime);
        }
    }
}