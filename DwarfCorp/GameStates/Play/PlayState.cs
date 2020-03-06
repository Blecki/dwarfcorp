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

        #region UserInterface Callbacks

        public Gui.MousePointer MousePointer = new Gui.MousePointer("mouse", 1, 0);

        public void ShowTooltip(String Text)
        {
            Gui.ShowTooltip(Gui.MousePosition, Text);
        }

        public void ShowInfo(UInt32 EntityID, String Text)
        {
            InfoTray.AddMessage(EntityID, Text);
        }

        public void ShowToolPopup(String Text)
        {
            if (String.IsNullOrEmpty(Text))
            {
                if (Gui.TooltipItem != null)
                {
                    Gui.DestroyWidget(Gui.TooltipItem);
                }
                Gui.TooltipItem = null;
            }
            else
            {
                Gui.RootItem.AddChild(
                  new Gui.Widgets.ToolPopup
                  {
                      Text = Text,
                      Rect = new Rectangle(Gui.MousePosition.X - 16, Gui.MousePosition.Y - 16, 128, 64)
                  });
            }
        }

        public void SetMouse(MousePointer Mouse)
        {
            Gui.MousePointer = Mouse;
        }

        public void SetMouseOverlay(String Mouse, int Frame)
        {
            Gui.MouseOverlaySheet = new TileReference(Mouse, Frame);
        }

        public bool IsMouseOverGui
        {
            get
            {
                return Gui.HoverItem != null || Gui.Dragging;
                // Don't detect tooltips and tool popups.
            }
        }

        public WorldPopup MakeWorldPopup(string text, GameComponent body, float screenOffset = -10, float time = 30.0f)
        {
            return MakeWorldPopup(new Events.TimedIndicatorWidget() { Text = text, DeathTimer = new Timer(time, true, Timer.TimerMode.Real) }, body, new Vector2(0, screenOffset));
        }

        public WorldPopup MakeWorldPopup(Widget widget, GameComponent body, Vector2 ScreenOffset)
        {
            if (LastWorldPopup.ContainsKey(body.GlobalID))
                Gui.DestroyWidget(LastWorldPopup[body.GlobalID].Widget);

            Gui.RootItem.AddChild(widget);

            // Todo: Uh - what cleans these up if the body is destroyed?
            LastWorldPopup[body.GlobalID] = new WorldPopup()
            {
                Widget = widget,
                BodyToTrack = body,
                ScreenOffset = ScreenOffset
            };

            Gui.RootItem.SendToBack(widget);

            return LastWorldPopup[body.GlobalID];
        }

        public bool IsCameraRotationModeActive()
        {
            return KeyManager.RotationEnabled(World.Renderer.Camera);
        }

        #endregion

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
