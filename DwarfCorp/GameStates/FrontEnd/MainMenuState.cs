using System;
using System.Collections.Generic;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.IO;

namespace DwarfCorp.GameStates
{
    public class MainMenuState : MenuState
    {
        public MainMenuState(DwarfGame game) :
            base(game)
        {
       
        }

        public void MakeMenu(DirectoryInfo GameToContinue)
        {
            var frame = CreateMenu(Library.GetString("main-menu-title"));

            if (GameToContinue != null && NewOverworldFile.CheckCompatibility(GameToContinue.FullName))
            {
                CreateMenuItem(frame,
                    "Continue",
                    NewOverworldFile.GetOverworldName(GameToContinue.FullName),
                    (sender, args) => {

                        var file = NewOverworldFile.Load(GameToContinue.FullName);
                        GameStateManager.PopState();
                        var overworldSettings = file.CreateSettings();
                        overworldSettings.InstanceSettings.LoadType = LoadType.LoadFromFile;
                        GameStateManager.PushState(new LoadState(Game, overworldSettings, LoadTypes.UseExistingOverworld));
                    });
            }

            CreateMenuItem(frame,
                Library.GetString("new-game"),
                Library.GetString("new-game-tooltip"),
                (sender, args) => GameStateManager.PushState(new WorldGeneratorState(Game, Overworld.Create(), WorldGeneratorState.PanelStates.Generate)));

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

            CreateMenuItem(frame, "QUICKPLAY", "",
                (sender, args) =>
                {
                    DwarfGame.LogSentryBreadcrumb("Menu", "User generating a random world.");

                    var overworldSettings = Overworld.Create();
                    overworldSettings.InstanceSettings.InitalEmbarkment = new Embarkment(overworldSettings);
                    overworldSettings.InstanceSettings.InitalEmbarkment.Funds = 1000u;
                    foreach (var loadout in Library.EnumerateLoadouts())
                        overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random(loadout, overworldSettings.Company));
                    GameStateManager.PushState(new LoadState(Game, overworldSettings, LoadTypes.GenerateOverworld));
                });

            CreateMenuItem(frame, "GIANT QUICKPLAY", "",
                (sender, args) =>
                {
                    GameStateManager.PushState(new CheckMegaWorldState(Game));
                });

            CreateMenuItem(frame, "DEBUG WORLD", "",
                (sender, args) =>
                {
                    DwarfGame.LogSentryBreadcrumb("Menu", "User generating a debug world.");

                    var overworldSettings = Overworld.Create();
                    overworldSettings.InstanceSettings.InitalEmbarkment = new Embarkment(overworldSettings);
                    overworldSettings.InstanceSettings.InitalEmbarkment.Funds = 1000000u;
                    overworldSettings.DebugWorld = true;
                    GameStateManager.PushState(new LoadState(Game, overworldSettings, LoadTypes.GenerateOverworld));
                });

            CreateMenuItem(frame, "Dwarf Designer", "Open the dwarf designer.",
                (sender, args) =>
                {
                    GameStateManager.PushState(new Debug.DwarfDesignerState(GameState.Game));
                });

            CreateMenuItem(frame, "What's New", "", (sender, args) =>
            {
                GameStateManager.PushState(new YarnState(null, "whats-new.conv", "Start", new Yarn.MemoryVariableStore()));
            });
#if DEBUG

            CreateMenuItem(frame, "Debug GUI", "", (sender, args) =>
            {
                GameStateManager.PushState(new Debug.GuiDebugState(GameState.Game));
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
            //DwarfSprites.FixDwarfSprites.Process();

            // Make sure that this memory gets cleaned up!!
            EntityFactory.Cleanup();
            Drawer3D.Cleanup();
            ParticleEmitter.Cleanup();
            PlayState.Input = null;
            InputManager.Cleanup();

            base.OnEnter();

            var worldDirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetWorldDirectory());
            var dirs = worldDirectory.EnumerateDirectories().ToList();
            dirs.Sort((a, b) =>
            {
                var aMeta = a.GetFiles("meta.txt");
                var bMeta = b.GetFiles("meta.txt");
                if (aMeta.Length > 0 && bMeta.Length > 0)
                    return bMeta[0].LastWriteTime.CompareTo(aMeta[0].LastWriteTime);

                return b.LastWriteTime.CompareTo(a.LastWriteTime);
            });

            MakeMenu(dirs.FirstOrDefault());
            IsInitialized = true;

            DwarfTime.LastTimeX.Speed = 1.0f;
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
