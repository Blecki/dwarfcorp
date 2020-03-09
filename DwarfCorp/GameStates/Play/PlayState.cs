using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.GameStates
{
    public partial class PlayState : GameState
    {
        private bool IsShuttingDown { get; set; }
        private bool QuitOnNextUpdate { get; set; }
        public bool ShouldReset { get; set; }
        private DateTime EnterTime;

        public WorldManager World { get; set; }

        public PlayState(DwarfGame game, WorldManager World) :
            base(game)
        {
            this.World = World;
            IsShuttingDown = false;
            QuitOnNextUpdate = false;
            ShouldReset = true;
            World.Paused = false;
            RenderUnderneath = true;
            EnableScreensaver = false;
            IsInitialized = false;

            rememberedViewValue = World.WorldSizeInVoxels.Y;

            VoxSelector = new VoxelSelector(World);
            BodySelector = new BodySelector(World.Renderer.Camera, GameState.Game.GraphicsDevice, World.ComponentManager);
        }

        /// <summary>
        /// Called when the PlayState is entered from the state manager.
        /// </summary>
        public override void OnEnter()
        {
            // Just toss out any pending input.
            DwarfGame.GumInputMapper.GetInputQueue();

            if (!IsInitialized)
            {
                EnterTime = DateTime.Now;
                DiscoverPlayerTools();

                VoxSelector.Selected += (voxels, button) => CurrentTool.OnVoxelsSelected(voxels, button);
                VoxSelector.Dragged += (voxels, button) => CurrentTool.OnVoxelsDragged(voxels, button);
                BodySelector.Selected += (bodies, button) =>
                {
                    CurrentTool.OnBodiesSelected(bodies, button);
                    if (CurrentToolMode == "SelectUnits")
                        SelectedObjects = bodies;
                };
                BodySelector.MouseOver += (bodies) => CurrentTool.OnMouseOver(bodies);

                // Ensure game is not paused.
                World.Paused = false;
                DwarfTime.LastTime.Speed = 1.0f;

                // Setup new gui. Double rendering the mouse?
                Gui = new Gui.Root(DwarfGame.GuiSkin);
                Gui.MousePointer = new Gui.MousePointer("mouse", 4, 0);

                World.UserInterface = this;
                CreateGUIComponents();
                IsInitialized = true;

                SoundManager.PlayMusic("main_theme_day");
                World.Time.Dawn += time =>
                {
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_daytime, 0.15f);
                    SoundManager.PlayMusic("main_theme_day");
                };

                World.Time.NewNight += time =>
                {
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_nighttime, 0.15f);
                    SoundManager.PlayMusic("main_theme_night");
                };

                World.UnpauseThreads();
                AutoSaveTimer = new Timer(GameSettings.Current.AutoSaveTimeMinutes * 60.0f, false, Timer.TimerMode.Real);

                foreach (var contextCommandFactory in AssetManager.EnumerateModHooks(typeof(ContextCommandAttribute), typeof(ContextCommands.ContextCommand), new Type[] { }))
                    ContextCommands.Add(contextCommandFactory.Invoke(null, new Object[] { }) as ContextCommands.ContextCommand);

                World.LogEvent(String.Format("We have arrived at {0}", World.Overworld.Name));
            }

            base.OnEnter();
        }

        /// <summary>
        /// Called when the PlayState is exited and another state (such as the main menu) is loaded.
        /// </summary>
        public override void OnCovered()
        {
            World.PauseThreads();
            base.OnCovered();
        }

        public void Destroy()
        {
            Input.Destroy();
        }

        public void QuitGame()
        {
            QuitGame(new MainMenuState(Game));
        }

        public void QuitGame(GameState state)
        {
            World.Quit();
            GameStateManager.ClearState();
            Destroy();

            GameStateManager.PushState(new WorldGeneratorState(Game, World.Overworld, WorldGeneratorState.PanelStates.Launch));
        }

        public void AutoSave(Action<bool> callback = null)
        {
            bool paused = World.Paused;

            World.Save(
                    (success, exception) =>
                    {
                        World.MakeAnnouncement(success ? "File autosaved." : "Autosave failed - " + exception.Message);
                        World.Paused = paused;
                        callback?.Invoke(success);
                    });
        }
    }
}
