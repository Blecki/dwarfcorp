using DwarfCorp.GameStates;
using DwarfCorp.Gui.Widgets;
using DwarfCorp.Tutorial;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;

namespace DwarfCorp
{
    /// <summary>
    /// This is the main game state for actually playing the game.
    /// </summary>
    public partial class WorldManager : IDisposable // Todo: Rename to just World?
    {
        #region fields

        public Overworld Overworld = null;
        public PersistentWorldData PersistentData = null; // Extend this class to add things that should be saved with the world.

        public WorldRenderer Renderer;
        public ChunkManager ChunkManager = null;
        public ComponentManager ComponentManager = null;
        public Yarn.MemoryVariableStore ConversationMemory = new Yarn.MemoryVariableStore();
        public FactionSet Factions = null;
        public ParticleManager ParticleManager = null;
        public Events.Scheduler EventScheduler;
        private Timer checkFoodTimer = new Timer(60.0f, false, Timer.TimerMode.Real);
        public TaskManager TaskManager;
        private Timer orphanedTaskRateLimiter = new Timer(10.0f, false, Timer.TimerMode.Real);
        public MonsterSpawner MonsterSpawner;
        public Faction PlayerFaction;
        public HashSet<GameComponent> ComponentUpdateSet = new HashSet<GameComponent>();
        public uint EntityUpdateFrame = 0;

        #region Tutorial Hooks

        public Tutorial.TutorialManager TutorialManager;
        
        public void Tutorial(String Name)
        {
            if (TutorialManager != null)
                TutorialManager.ShowTutorial(Name);
        }

        #endregion

        public Scripting.Gambling GamblingState = new Scripting.Gambling();

        public ContentManager Content;
        public DwarfGame Game;
        public GraphicsDevice GraphicsDevice { get { return GameState.Game.GraphicsDevice; } }
        

        private bool paused_ = false;
        // True if the game's update loop is paused, false otherwise
        public bool Paused
        {
            get { return paused_; }
            set
            {
                paused_ = value;

                if (DwarfTime.LastTimeX != null)
                    DwarfTime.LastTimeX.IsPaused = paused_;
            }
        }

        public PlanService PlanService = null;
        public Weather Weather = new Weather();
        public WorldTime Time = new WorldTime();
        public Point3 WorldSizeInChunks;

        [JsonIgnore]
        public Point3 WorldSizeInVoxels
        {
            get
            {
                return new Point3(WorldSizeInChunks.X * VoxelConstants.ChunkSizeX, WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY, WorldSizeInChunks.Z * VoxelConstants.ChunkSizeZ);
            }
        }


        public EventLog EventLog = new EventLog();
        public StatsTracker Stats = new StatsTracker();

        public void LogEvent(EventLog.LogEntry entry)
        {
            EventLog.AddEntry(entry);
        }

        public void LogEvent(String Message, String Details = "")
        {
            LogEvent(Message, Color.Black, Details);
        }

        public void LogEvent(String Message, Color textColor, String Details = "")
        {
            LogEvent(new EventLog.LogEntry()
            {
                TextColor = textColor,
                Text = Message,
                Details = Details,
                Date = Time.CurrentDate
            });
        }

        public void LogStat(String stat, float value)
        {
            Stats.AddStat(stat, Time.CurrentDate, value);
        }
                
        public struct Screenshot
        {
            public string FileName { get; set; }
            public Point Resolution { get; set; }
        }

        public bool ShowingWorld { get; set; }

        public PlayState UserInterface;

        // event that is called when the world is done loading
        public delegate void OnLoaded();
        public event OnLoaded OnLoadedEvent;

        private Splasher Splasher;
        #endregion

        [JsonIgnore]
        public List<EngineModule> UpdateSystems = new List<EngineModule>();

        public T FindSystem<T>() where T: EngineModule
        {
            return UpdateSystems.FirstOrDefault(s => s is T) as T;
        }

        /// <summary>
        /// Creates a new play state
        /// </summary>
        /// <param name="Game">The program currently running</param>
        public WorldManager(DwarfGame Game)
        {
            this.Game = Game;
            Content = Game.Content;
            Time = new WorldTime();
            Renderer = new WorldRenderer(Game, this);
        }

        public void PauseThreads()
        {
            if (ChunkManager != null)
                ChunkManager.PauseThreads = true;
        }

        public void UnpauseThreads()
        {
            if (ChunkManager != null)
            {
                ChunkManager.PauseThreads = false;
            }

            if (Renderer.Camera != null)
                Renderer.Camera.LastWheel = Mouse.GetState().ScrollWheelValue;
        }

        private void TrackStats()
        {
            LogStat("Money", (float)(decimal)PlayerFaction.Economy.Funds);

            var resources = EnumerateResourcesIncludingMinions();
            LogStat("Resources", resources.Count());
            LogStat("Resource Value", (float)resources.Sum(r => r.MoneyValue));
            LogStat("Employees", PlayerFaction.Minions.Count);
            LogStat("Employee Pay", (float)PlayerFaction.Minions.Select(m => m.Stats.DailyPay.Value).Sum());
            LogStat("Furniture",  PlayerFaction.OwnedObjects.Count);
            LogStat("Zones", EnumerateZones().Count());
            LogStat("Employee Happiness", (float)PlayerFaction.Minions.Sum(m => m.Stats.Happiness.Percentage) / Math.Max(PlayerFaction.Minions.Count, 1));
        }

        private int _prevHour = 0;
        /// <summary>
        /// Called every frame
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public void Update(DwarfTime gameTime)
        {
            IndicatorManager.Update(gameTime);
            HandleAmbientSound(gameTime);

            TaskManager.Update(PlayerFaction.Minions);

            //if (Paused)
            //    Renderer.Camera.LastWheel = Mouse.GetState().ScrollWheelValue;
           
            // Should we display the out of food message?
            checkFoodTimer.Update(gameTime);
            if (checkFoodTimer.HasTriggered)
            {
                var food = CountResourcesWithTag("Edible");
                if (food == 0)
                    MakeAnnouncement("We're out of food!", null, () => { return CountResourcesWithTag("Edible") == 0; });
            }

            GamblingState.Update(gameTime);
            EventScheduler.Update(this, Time.CurrentDate);

            Time.Update(gameTime);
                       
            if (Paused)
            {
                ComponentManager.UpdatePaused(gameTime, ChunkManager, Renderer.Camera);
                TutorialManager.Update(UserInterface.Gui);
            }
            // If not paused, we want to just update the rest of the game.
            else
            {
                EntityUpdateFrame += 1;

                // Choose what entities to update.
                ComponentUpdateSet.Clear();
                ComponentManager.FindComponentsToUpdate(ComponentUpdateSet);
                foreach (var component in ComponentUpdateSet)
                    component.UpdateFrame = EntityUpdateFrame;

                ParticleManager.Update(gameTime, this);
                TutorialManager.Update(UserInterface.Gui);

                foreach (var updateSystem in UpdateSystems)
                {
                    try
                    {
                        updateSystem.Update(gameTime, this);
                    }
                    catch (Exception) { }
                }

                UpdateZones(gameTime);

                #region Mourn dead minions
                foreach (var deadMinion in PlayerFaction.Minions.Where(m => m.IsDead && m.Stats.CurrentClass.HasValue(out var c) && c.TriggersMourning))
                {
                    MakeAnnouncement(String.Format("{0} died!", deadMinion.Stats.FullName));
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic);
                    Tutorial("death");

                    foreach (var minion in PlayerFaction.Minions)
                        minion.Creature.AddThought("A friend died recently.", new TimeSpan(2, 0, 0, 0), -25.0f);
                }
                #endregion

                #region Free stuck minions
                foreach (var minion in PlayerFaction.Minions)
                {
                    if (minion == null) throw new InvalidProgramException("Null minion?");
                    if (minion.Stats == null) throw new InvalidProgramException("Minion has null status?");

                    if (minion.Stats.IsAsleep)
                        continue;

                    if (!minion.CurrentTask.HasValue())
                        continue;

                    if (minion.Stats.IsTaskAllowed(TaskCategory.Dig))
                        minion.Movement.SetCan(MoveType.Dig, GameSettings.Current.AllowAutoDigging);

                    minion.ResetPositionConstraint();
                }
                #endregion

                foreach (var body in PlayerFaction.OwnedObjects)
                    if (body.ReservedFor != null && body.ReservedFor.IsDead)
                        body.ReservedFor = null;

                #region Manage selection circles
                PersistentData.SelectedMinions.RemoveAll(m => m.IsDead);

                foreach (var m in PlayerFaction.Minions)
                {
                    if (m.GetRoot().GetComponent<SelectionCircle>().HasValue(out var selectionCircle))
                        selectionCircle.SetFlagRecursive(GameComponent.Flag.Visible, false);
                    if (m.Creature.Sprite != null)
                        m.Creature.Sprite.SetDrawSilhouette(false);
                }

                foreach (var creature in PersistentData.SelectedMinions)
                {
                    if (creature.GetRoot().GetComponent<SelectionCircle>().HasValue(out var selectionCircle))
                    {
                        selectionCircle.SetFlag(GameComponent.Flag.ShouldSerialize, false);
                        selectionCircle.SetFlagRecursive(GameComponent.Flag.Visible, true);
                    }
                    else
                    {
                        selectionCircle = creature.GetRoot().AddChild(new SelectionCircle(creature.Manager, creature.Creature.Physics)) as SelectionCircle;
                        selectionCircle.SetFlag(GameComponent.Flag.ShouldSerialize, false);
                        selectionCircle.SetFlagRecursive(GameComponent.Flag.Visible, true);
                    }

                    creature.Creature.Sprite.SetDrawSilhouette(true);
                }
                #endregion

                PersistentData.Designations.CleanupDesignations();

                Factions.Update(gameTime);

                foreach (var applicant in PersistentData.NewArrivals)
                    if (Time.CurrentDate >= applicant.ArrivalTime)
                        HireImmediately(applicant.Applicant);

                PersistentData.NewArrivals.RemoveAll(a => Time.CurrentDate >= a.ArrivalTime);



                ComponentManager.Update(gameTime, ChunkManager, ComponentUpdateSet);
                MonsterSpawner.Update(gameTime);

            }

            // These things are updated even when the game is paused

            Splasher.Splash(gameTime, ChunkManager.Water.GetSplashQueue());

            ChunkManager.Update(gameTime, Renderer.Camera, GraphicsDevice);
            SoundManager.Update(gameTime, Renderer.Camera, Time);
            Weather.Update(this.Time.CurrentDate, this);

#if DEBUG
            KeyboardState k = Keyboard.GetState();
            if (k.IsKeyDown(Keys.Home))
            {
                try
                {
                    GameState.Game.GraphicsDevice.Reset();
                }
                catch (Exception exception)
                {

                }
            }
#endif

            if (Time.CurrentDate.Hour != _prevHour)
                TrackStats();
            _prevHour = Time.CurrentDate.Hour;
        }

        public void Quit()
        {
            ChunkManager.Destroy();
            ComponentManager = null;

            ChunkManager = null;
            GC.Collect();
            PlanService.Die();
        }

        public void Dispose()
        {
            if (LoadingThread != null && LoadingThread.IsAlive)
                LoadingThread.Abort();
        }

        public void OnVoxelDestroyed(VoxelHandle V)
        {
            if (!V.IsValid)
                return;

            var toDestroy = new List<Zone>();

            lock (PersistentData.Zones)
            {
                var toCheck = new List<Zone>();
                toCheck.AddRange(PersistentData.Zones.Where(r => r.IsBuilt));
                foreach (var r in toCheck)
                    if (r.RemoveVoxel(V))
                        toDestroy.Add(r);

                foreach (var r in toDestroy)
                {
                    PersistentData.Zones.Remove(r);
                    r.Destroy();
                }
            }
        }

        public void OnVoxelChanged(VoxelChangeEvent e)
        {
            var box = e.Voxel.GetBoundingBox();
            var hashmap = EnumerateIntersectingAnchors(box);

            foreach (var intersectingBody in hashmap)
            {
                var listener = intersectingBody as IVoxelListener; // Aren't they always listeners?
                if (listener != null)
                    listener.OnVoxelChanged(e);
            }

            TaskManager.OnVoxelChanged(e);
            MonsterSpawner.OnVoxelChanged(e);

        }
    }
}
