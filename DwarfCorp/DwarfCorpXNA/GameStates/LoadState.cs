using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates
{
    public class LoadState : GameState
    {
        public WorldManager World { get; set; }
        public InputManager Input = new InputManager();
        public static DwarfGUI GUI = null;

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

        
        public LoadState(DwarfGame game, GameStateManager stateManager) :
            base(game, "LoadState", stateManager)
        {
            
        }

        private void World_OnLoadedEvent()
        {
            IsInitialized = true;

            // Todo: Decouple gui/input from world.
            // Copy important bits to PlayState - This is a hack; decouple world from gui and input instead.
            PlayState.World = World;
            PlayState.Input = Input;
            PlayState.GUI = GUI;

            // Hack: So that saved games still load.
            if (WorldManager.PlayerCompany.Information == null)
                WorldManager.PlayerCompany.Information = new CompanyInformation();

            StateManager.PopState();
            StateManager.PushState("PlayState");            
        }

        public override void OnEnter()
        {
                IsInitialized = false;

                IndicatorManager.SetupStandards();

                World = new WorldManager(Game);
                World.OnLoadedEvent += World_OnLoadedEvent;

            // Todo - Save gui creation for play state. We're only creating it here so we can give it to
            //      the world class. The world doesn't need it until after loading.
                GUI = new DwarfGUI(Game, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default),
                    Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title),
                    Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input);

                GUI.ToolTipManager.InfoLocation = new Point(Game.GraphicsDevice.Viewport.Width / 2, Game.GraphicsDevice.Viewport.Height);
                GUI.MouseMode = GUISkin.MousePointer.Wait;

                World.Setup(GUI);

            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
 
        }

        public override void Render(DwarfTime gameTime)
        {
            return;
            //throw new InvalidOperationException();

            base.Render(gameTime);
        }

        public override void RenderUnitialized(DwarfTime gameTime)
        {
            TipTimer.Update(gameTime);
            if (TipTimer.HasTriggered)
            {
                World.LoadingMessageBottom = LoadingTips[MathFunctions.Random.Next(LoadingTips.Count)];
                TipIndex++;
            }

            EnableScreensaver = true;
            World.Render(gameTime);
            base.RenderUnitialized(gameTime);
        }

    }
}