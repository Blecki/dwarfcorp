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
            var frame = CreateMenu(Library.GetString("main-menu-title"));

            CreateMenuItem(frame,
                Library.GetString("new-game"),
                Library.GetString("new-game-tooltip"),
                (sender, args) => GameStateManager.PushState(new WorldGeneratorState(Game, new OverworldGenerationSettings(), WorldGeneratorState.PanelStates.Generate)));

            CreateMenuItem(frame, 
                Library.GetString("load-game"),
                Library.GetString("load-game-tooltip"),
                (sender, args) => GameStateManager.PushState(new WorldLoaderState(Game)));

            CreateMenuItem(frame, 
                Library.GetString("options"),
                Library.GetString("options-tooltip"),
                (sender, args) => GameStateManager.PushState(new OptionsState(Game)));

            CreateMenuItem(frame,
                Library.GetString("manage-mods"),
                Library.GetString("manage-mods-tooltip"), 
                (sender, args) => GameStateManager.PushState(new ModManagement.ManageModsState(Game)));

            CreateMenuItem(frame, 
                Library.GetString("credits"),
                Library.GetString("credits-tooltip"),
                (sender, args) => GameStateManager.PushState(new CreditsState(GameState.Game)));

#if DEBUG
            CreateMenuItem(frame, "QUICKPLAY", "",
                (sender, args) =>
                {
                    DwarfGame.LogSentryBreadcrumb("Menu", "User generating a random world.");
                    var company = new CompanyInformation();

                    var employees = new List<Applicant>();
                    employees.Add(Applicant.Random("Crafter", company));
                    employees.Add(Applicant.Random("Manager", company));
                    employees.Add(Applicant.Random("Miner", company));
                    employees.Add(Applicant.Random("Wizard", company));
                    employees.Add(Applicant.Random("Soldier", company));
                    employees.Add(Applicant.Random("Musketeer", company));

                    GameStateManager.PushState(new LoadState(Game, new OverworldGenerationSettings()
                    {
                        Company = new CompanyInformation(),
                        GenerateFromScratch = true,
                        InstanceSettings = new InstanceSettings(),
                        InitalEmbarkment = new Embarkment
                        {
                            Employees = employees,
                            Money = 1000u
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
                Library.GetString("quit"),
                Library.GetString("quit-tooltip"),
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
