using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using BloomPostprocess;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using DwarfCorp.Tutorial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.GameStates;
using Newtonsoft.Json;
using DwarfCorp.Events;

namespace DwarfCorp
{
    /// <summary>
    /// This is the main game state for actually playing the game.
    /// </summary>
    public partial class WorldManager : IDisposable // Todo: Rename to just World?
    {
        #region fields

        public WorldRenderer Renderer;
        public OverworldGenerationSettings Settings = null;
        public ChunkManager ChunkManager = null;
        public ComponentManager ComponentManager = null;
        public Yarn.MemoryVariableStore ConversationMemory = new Yarn.MemoryVariableStore();
        public FactionLibrary Factions = null;
        public ParticleManager ParticleManager = null;
        public Events.Scheduler EventScheduler;
        public int MaxViewingLevel = 0;
        private Timer checkFoodTimer = new Timer(60.0f, false, Timer.TimerMode.Real);
        public TaskManager TaskManager;
        private Timer orphanedTaskRateLimiter = new Timer(10.0f, false, Timer.TimerMode.Real);
        public MonsterSpawner MonsterSpawner;
        public Faction PlayerFaction;
        public List<Faction> Natives { get; set; } // Todo: To be rid of this; two concepts of faction - The owner faction in the overworld, and the instance in this game.

        #region Tutorial Hooks

        public Tutorial.TutorialManager TutorialManager;
        
        public void Tutorial(String Name)
        {
            if (TutorialManager != null)
                TutorialManager.ShowTutorial(Name);
        }

        #endregion

        public Diplomacy Diplomacy;
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

                if (DwarfTime.LastTime != null)
                    DwarfTime.LastTime.IsPaused = paused_;
            }
        }

        public PlanService PlanService = null;
        public Weather Weather = new Weather();
        public WorldTime Time = new WorldTime();
        private SaveGame gameFile;
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

        private QueuedAnnouncement SleepPrompt = null;

        // event that is called when the world is done loading
        public delegate void OnLoaded();
        public event OnLoaded OnLoadedEvent;

        // Lazy actions - needed occasionally to spawn entities from threads among other things.
        private static List<Action> LazyActions = new List<Action>();

        public static void DoLazy(Action action)
        {
            LazyActions.Add(action);
        }
        
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

            var resources = PlayerFaction.ListResourcesInStockpilesPlusMinions();
            LogStat("Resources", resources.Values.Select(r => r.First.Count + r.Second.Count).Sum());
            LogStat("Resource Value", (float)resources.Values.Select(r =>
            {
                var value = ResourceLibrary.GetResourceByName(r.First.Type).MoneyValue.Value;
                return (r.First.Count * value) + (r.Second.Count * value);
            }).Sum());
            LogStat("Employees", PlayerFaction.Minions.Count);
            LogStat("Employee Pay", (float)PlayerFaction.Minions.Select(m => m.Stats.CurrentLevel.Pay.Value).Sum());
            LogStat("Furniture",  PlayerFaction.OwnedObjects.Count);
            LogStat("Zones", PlayerFaction.GetRooms().Count);
            LogStat("Employee Level", PlayerFaction.Minions.Sum(r => r.Stats.LevelIndex));
            LogStat("Employee Happiness", (float)PlayerFaction.Minions.Sum(m => m.Stats.Happiness.Percentage) / Math.Max(PlayerFaction.Minions.Count, 1));
        }

        private int _prevHour = 0;
        /// <summary>
        /// Called every frame
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public void Update(DwarfTime gameTime)
        {
            #region Perform Lazy Actions
            int MAX_LAZY_ACTIONS = 32;
            var actionsPerformed = 0;
            for (; actionsPerformed < LazyActions.Count && actionsPerformed < MAX_LAZY_ACTIONS; ++actionsPerformed)
                LazyActions[actionsPerformed]?.Invoke();
            if (actionsPerformed > 0)
                LazyActions.RemoveRange(0, actionsPerformed);
            #endregion

            #region Fast Forward To Day
            if (FastForwardToDay)
            {
                if (Time.IsDay())
                {
                    FastForwardToDay = false;
                    foreach (CreatureAI minion in PlayerFaction.Minions)
                        minion.Stats.Energy.CurrentValue = minion.Stats.Energy.MaxValue;
                    Time.Speed = 100;
                }
                else
                    Time.Speed = 1000;
            }
            #endregion

            IndicatorManager.Update(gameTime);
            HandleAmbientSound();
            UpdateOrphanedTasks();

            TaskManager.Update(PlayerFaction.Minions);
            PlayerFaction.RoomBuilder.Update();

            if (Paused)
                Renderer.Camera.LastWheel = Mouse.GetState().ScrollWheelValue;
           
            // Should we display the out of food message?
            checkFoodTimer.Update(gameTime);
            if (checkFoodTimer.HasTriggered)
            {
                var food = PlayerFaction.CountResourcesWithTag(Resource.ResourceTags.Edible);
                if (food == 0)
                    MakeAnnouncement("We're out of food!", null, () => { return PlayerFaction.CountResourcesWithTag(Resource.ResourceTags.Edible) == 0; });
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
                ParticleManager.Update(gameTime, this);
                TutorialManager.Update(UserInterface.Gui);

                foreach (var updateSystem in UpdateSystems)
                {
                    try
                    {
                        updateSystem.Update(gameTime);
                    }
                    catch (Exception) { }
                }

                Diplomacy.Update(gameTime, Time.CurrentDate, this);
                Factions.Update(gameTime);
                ComponentManager.Update(gameTime, ChunkManager, Renderer.Camera);
                MonsterSpawner.Update(gameTime);
                bool allAsleep = PlayerFaction.AreAllEmployeesAsleep();

#if !UPTIME_TEST
                if (SleepPrompt == null && allAsleep && !FastForwardToDay && Time.IsNight())
                {
                    SleepPrompt = new QueuedAnnouncement()
                    {
                        Text = "All your employees are asleep. Click here to skip to day.",
                        ClickAction = (sender, args) =>
                        {
                            FastForwardToDay = true;
                            SleepPrompt = null;
                        },
                        ShouldKeep = () =>
                        {
                            return FastForwardToDay == false && Time.IsNight() && PlayerFaction.AreAllEmployeesAsleep();
                        }
                    };
                    MakeAnnouncement(SleepPrompt);
                }
                else if (!allAsleep)
                {
                    Time.Speed = 100;
                    FastForwardToDay = false;
                    SleepPrompt = null;
                }
#endif
            }

            // These things are updated even when the game is paused

            Splasher.Splash(gameTime, ChunkManager.Water.GetSplashQueue());

            ChunkManager.Update(gameTime, Renderer.Camera, GraphicsDevice);
            SoundManager.Update(gameTime, Renderer.Camera, Time);
            Weather.Update(this.Time.CurrentDate, this);

            if (gameFile != null)
            {
                // Cleanup game file.
                gameFile = null;
            }

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

        public bool FastForwardToDay { get; set; }

        public void Quit()
        {
            ChunkManager.Destroy();
            ComponentManager.Destroy();
            ComponentManager = null;

            ChunkManager = null;
            GC.Collect();
            PlanService.Die();
        }

        public void Dispose()
        {
            // Todo: Move this to the composite library.
            foreach(var composite in CompositeLibrary.Composites)
                composite.Value.Dispose();
            CompositeLibrary.Composites.Clear();

            if (LoadingThread != null && LoadingThread.IsAlive)
                LoadingThread.Abort();
        }

        // This hack exists to find orphaned tasks not assigned to any dwarf, and to then
        // put them on the task list.
        // Todo: With the new task pool, how often is this used?
        public void UpdateOrphanedTasks()
        {
            orphanedTaskRateLimiter.Update(DwarfTime.LastTime);
            if (orphanedTaskRateLimiter.HasTriggered)
            {
                List<Task> orphanedTasks = new List<Task>();

                foreach (var ent in PlayerFaction.Designations.EnumerateEntityDesignations())
                {
                    if (ent.Type == DesignationType.Attack)
                    {
                        var task = new KillEntityTask(ent.Body, KillEntityTask.KillType.Attack);
                        if (!TaskManager.HasTask(task) &&
                            !PlayerFaction.Minions.Any(minion => minion.Tasks.Contains(task)))
                        {
                            orphanedTasks.Add(task);
                        }
                    }


                    else if (ent.Type == DesignationType.Craft)
                    {
                        var task = new CraftItemTask(ent.Tag as CraftDesignation);
                        if (!TaskManager.HasTask(task) &&
                            !PlayerFaction.Minions.Any(minion => minion.Tasks.Contains(task)))
                        {
                            orphanedTasks.Add(task);
                        }
                    }

                    // TODO ... other entity task types
                }

                if (orphanedTasks.Count > 0)
                    TaskManager.AddTasks(orphanedTasks);
            }
        }
    }
}
