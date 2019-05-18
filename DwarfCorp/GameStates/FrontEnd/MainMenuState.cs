using System;
using System.Collections.Generic;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{
    public class MainMenuState : MenuState
    {
        public MainMenuState(DwarfGame game) :
            base(game)
        {
       
        }

        public void MakeMenu()
        {
            var frame = CreateMenu(StringLibrary.GetString("main-menu-title"));

            CreateMenuItem(frame, 
                StringLibrary.GetString("new-game"), 
                StringLibrary.GetString("new-game-tooltip"),
                (sender, args) => GameStateManager.PushState(new CompanyMakerState(Game)));

            CreateMenuItem(frame, 
                StringLibrary.GetString("load-game"),
                StringLibrary.GetString("load-game-tooltip"),
                (sender, args) => GameStateManager.PushState(new WorldLoaderState(Game)));

            CreateMenuItem(frame, 
                StringLibrary.GetString("options"),
                StringLibrary.GetString("options-tooltip"),
                (sender, args) => GameStateManager.PushState(new OptionsState(Game)));

            CreateMenuItem(frame,
                StringLibrary.GetString("manage-mods"),
                StringLibrary.GetString("manage-mods-tooltip"), 
                (sender, args) => GameStateManager.PushState(new ModManagement.ManageModsState(Game)));

            CreateMenuItem(frame, 
                StringLibrary.GetString("credits"),
                StringLibrary.GetString("credits-tooltip"),
                (sender, args) => GameStateManager.PushState(new CreditsState(GameState.Game)));

#if DEBUG
            CreateMenuItem(frame, "QUICKPLAY", "",
                (sender, args) =>
                {
                    DwarfGame.LogSentryBreadcrumb("Menu", "User generating a random world.");
                    GameStateManager.PushState(new LoadState(Game, new OverworldGenerationSettings()
                    {
                        Company = new CompanyInformation(),
                        GenerateFromScratch = true,
                        InstanceSettings = new InstanceSettings
                        {
                            ColonySize = new Point3(8, 4, 8)
                        }
                    }));
                });

            CreateMenuItem(frame, "Dwarf Designer", "Open the dwarf designer.",
                (sender, args) =>
                {
                    GameStateManager.PushState(new Debug.DwarfDesignerState(GameState.Game));
                });

            CreateMenuItem(frame, "Yarn test", "", (sender, args) =>
            {
                GameStateManager.PushState(new YarnState(null, "test.conv", "Start", new Yarn.MemoryVariableStore()));
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
