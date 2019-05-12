using System;
using System.Collections.Generic;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{
    public class MainMenuState : MenuState
    {
        public MainMenuState(DwarfGame game, GameStateManager stateManager) :
            base("MainMenuState", game, stateManager)
        {
       
        }

        public void MakeMenu()
        {
            var frame = CreateMenu(StringLibrary.GetString("main-menu-title"));

            CreateMenuItem(frame, 
                StringLibrary.GetString("new-game"), 
                StringLibrary.GetString("new-game-tooltip"),
                (sender, args) => StateManager.PushState(new CompanyMakerState(Game, Game.StateManager)));

            CreateMenuItem(frame, 
                StringLibrary.GetString("load-game"),
                StringLibrary.GetString("load-game-tooltip"),
                (sender, args) => StateManager.PushState(new WorldLoaderState(Game, StateManager)));

            CreateMenuItem(frame, 
                StringLibrary.GetString("options"),
                StringLibrary.GetString("options-tooltip"),
                (sender, args) => StateManager.PushState(new OptionsState(Game, StateManager)));

            CreateMenuItem(frame,
                StringLibrary.GetString("manage-mods"),
                StringLibrary.GetString("manage-mods-tooltip"), 
                (sender, args) => StateManager.PushState(new ModManagement.ManageModsState(Game, StateManager)));

            CreateMenuItem(frame, 
                StringLibrary.GetString("credits"),
                StringLibrary.GetString("credits-tooltip"),
                (sender, args) => StateManager.PushState(new CreditsState(GameState.Game, StateManager)));

#if DEBUG
            CreateMenuItem(frame, "GUI Debug", "Open the GUI debug screen.",
                (sender, args) =>
                {
                    StateManager.PushState(new Debug.GuiDebugState(GameState.Game, StateManager));
                });

            CreateMenuItem(frame, "Dwarf Designer", "Open the dwarf designer.",
                (sender, args) =>
                {
                    StateManager.PushState(new Debug.DwarfDesignerState(GameState.Game, StateManager));
                });

            CreateMenuItem(frame, "Yarn test", "", (sender, args) =>
            {
                StateManager.PushState(new YarnState(null, "test.conv", "Start", new Yarn.MemoryVariableStore()));
            });
#endif

            CreateMenuItem(frame, 
                StringLibrary.GetString("quit"),
                StringLibrary.GetString("quit-tooltip"),
                (sender, args) => Game.Exit());

            FinishMenu();
        }

        public override void OnEnter()
        {
            // Make sure that this memory gets cleaned up!!
            EntityFactory.Cleanup();
            Drawer3D.Cleanup();
            ParticleEmitter.Cleanup();
            //Overworld.Cleanup();
            PlayState.Input = null;
            InputManager.Cleanup();
            LayeredSprites.LayerLibrary.Cleanup();

            base.OnEnter();

            MakeMenu();
            IsInitialized = true;

            DwarfTime.LastTime.Speed = 1.0f;
            SoundManager.PlayMusic("menu_music");
            SoundManager.StopAmbience();
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
