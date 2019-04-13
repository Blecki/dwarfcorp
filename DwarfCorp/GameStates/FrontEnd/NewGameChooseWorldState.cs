using System;
using System.Collections.Generic;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{
    public class NewGameChooseWorldState : MenuState
    {
        public NewGameChooseWorldState(DwarfGame game, GameStateManager stateManager) :
            base("MainMenuState", game, stateManager)
        {
        }

        public void MakeMenu()
        {
            var frame = CreateMenu("PLAY DWARFCORP");

            CreateMenuItem(frame, "New World", "Create a new world from scratch.", (sender, args) =>
                StateManager.PushState(new WorldGeneratorState(Game, StateManager, null, true)));

            CreateMenuItem(frame, "Random World", "Just start a game on a completely random world.", (sender, args) => {
                GameStates.GameState.Game.LogSentryBreadcrumb("Menu", "User generating a random world.");
                StateManager.PushState(new LoadState(Game, Game.StateManager, new WorldGenerationSettings() { GenerateFromScratch = true }));
            });

            CreateMenuItem(frame, "Load World", "Load a continent from an existing file.", (sender, args) =>
                StateManager.PushState(new WorldLoaderState(Game, StateManager)));

            CreateMenuItem(frame, "Special World", "Create a special world.", (sender, args) => 
                StateManager.PushState(new NewGameCreateDebugWorldState(Game, StateManager)));

            CreateMenuItem(frame, "Back", "Go back to main menu.", (sender, args) => 
                StateManager.PopState());

            FinishMenu();
        }

        public override void OnEnter()
        {
            base.OnEnter();

            MakeMenu();
            IsInitialized = true;
        }

        public override void Update(DwarfTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            base.Render(gameTime);
        }
    }

}