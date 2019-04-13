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

#if !DEMO
            string latestSave = SaveGame.GetLatestSaveFile();

            if (latestSave != null)
            {
                CreateMenuItem(frame,
                    StringLibrary.GetString("continue"),
                    StringLibrary.GetString("continue-tooltip", latestSave),
                    (sender, args) => {
                        GameStates.GameState.Game.LogSentryBreadcrumb("Menu", "User is continuing from a save file.");
                        StateManager.PushState(new LoadState(Game, Game.StateManager, new WorldGenerationSettings()
                        {
                            ExistingFile = latestSave
                        }));
                    }
                    );
                     
            }
#endif
            /*
            CreateMenuItem(frame, 
                StringLibrary.GetString("new-game"), 
                StringLibrary.GetString("new-game-tooltip"), 
                (sender, args) => StateManager.PushState(new LoadState(Game, Game.StateManager, new WorldGenerationSettings() {GenerateFromScratch = true})));
            */
            CreateMenuItem(frame, 
                StringLibrary.GetString("new-game"), 
                StringLibrary.GetString("new-game-tooltip"),
#if !DEMO
                (sender, args) => StateManager.PushState(new CompanyMakerState(Game, Game.StateManager)));
#else
                (sender, args) => this.GuiRoot.ShowModalPopup(new Gui.Widgets.Confirm() { CancelText = "", Text = StringLibrary.GetString("advanced-world-creation-denied") }));
#endif
            CreateMenuItem(frame, 
                StringLibrary.GetString("load-game"),
                StringLibrary.GetString("load-game-tooltip"),
#if !DEMO
                (sender, args) => StateManager.PushState(new LoadSaveGameState(Game, StateManager)));
#else
                (sender, args) => this.GuiRoot.ShowModalPopup(new Gui.Widgets.Confirm() { CancelText = "", Text = StringLibrary.GetString("save-load-denied") }));
#endif

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
            Overworld.Cleanup();
            ResourceLibrary.Cleanup();
            CraftLibrary.Cleanup();
            VoxelLibrary.Cleanup();
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
