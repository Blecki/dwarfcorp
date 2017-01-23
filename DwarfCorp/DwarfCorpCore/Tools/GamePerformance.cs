using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    public class GamePerformance : IDisposable
    {
        /// <summary>
        /// A boolean toggle changed with a keyboard button press.  Allows realtime switching between two code blocks
        /// for comparsion testing.
        /// </summary>
        public static bool DebugToggle1;

        /// <summary>
        /// A boolean toggle changed with a keyboard button press.  Allows realtime switching between two code blocks
        /// for comparsion testing.
        /// </summary>
        public static bool DebugToggle2;

        /// <summary>
        /// One copy per thread.  Lets us tell when we haven't set up that thread yet.
        /// </summary>
        [ThreadStatic]
        public static bool threadInitialized = false;

        /// <summary>
        /// A thread ID number.  Set when we call a class function from that new thread.
        /// </summary>
        [ThreadStatic]
        public static int threadIdentifier = 0;

        /// <summary>
        /// Keeps track of the current zone we are in for the thread.
        /// </summary>
        [ThreadStatic]
        public static Stack<TrackerZone> zone;

        /// <summary>
        /// A display name for the thread.
        /// </summary>
        [ThreadStatic]
        public static string threadName;

        /// <summary>
        /// A value incremented as we call functions from new threads.
        /// </summary>
        public static int threadCount = 0;

        public enum ThreadIdentifier
        {
            All = 0,
            Main,
            RebuildVoxels,
            RebuildWater,
            UpdateWater
        }


        /// <summary>
        /// The amount of pixels added between lines of text.
        /// </summary>
        private readonly float guiPadding = 5f;

        /// <summary>
        /// Single white pixel texture used to draw lines in DrawChangeGraph
        /// </summary>
        private Texture2D pixel;

        /// <summary>
        /// Singleton instance field.
        /// </summary>
        private static GamePerformance _instance;

        /// <summary>
        /// Internal Dictionary used to store and retrieve trackers set by String value.
        /// </summary>
        private Dictionary<string, Tracker> internalTrackers;

        /// <summary>
        /// Dictionary used to store Trackers that handle thread loops.
        /// </summary>
        private Dictionary<ThreadIdentifier, ThreadLoopTracker> threadLoopTrackers;

        /// <summary>
        /// Object used to synchronize threadLoopTrackers access.
        /// </summary>
        private Object threadLoopLockObject;

        /// <summary>
        /// Object used to synchronize internalTrackers access.
        /// </summary>
        private Object internalTrackerLockObject;

        /// <summary>
        /// Object used to synchronize zoneList access.  Not needed right now as we are using a ConcurrentDictionary.
        /// </summary>
        private Object zoneListLockObject;

        /// <summary>
        /// Dictionary used to store information about the zones.
        /// </summary>
        private ConcurrentDictionary<string, TrackerZone> zoneList;

        /// <summary>
        /// Internal list used to store all set trackers.
        /// </summary>
        private List<Tracker> trackers;

        /// <summary>
        /// Stopwatch object used for millisecond based timing by Tracker classes.
        /// </summary>
        private Stopwatch internalWatch;

        /// <summary>
        /// Stores if the key to toggle the debug flag is pressed to allow it to change only once per keypress.
        /// </summary>
        private Boolean overlayKeyPressed;

        /// <summary>
        /// Stores if the key to toggle the debug flag is pressed to allow it to change only once per keypress.
        /// </summary>
        private Boolean debug2ToggleKeyPressed;

        /// <summary>
        /// Stores if the key to toggle the overlay flag is pressed to allow it to change only once per keypress.
        /// </summary>
        private Boolean debug1ToggleKeyPressed;

        /// <summary>
        /// Game reference used to access GraphicsDevice and ContentManager.
        /// </summary>
        private DwarfGame _game;

        /// <summary>
        /// Font used for handling text drawing on the overlay.
        /// </summary>
        private SpriteFont overlayFont;

        /// <summary>
        /// Cached value of the font height to avoid remeasuring every call.
        /// </summary>
        private float fontHeight;

        /// <summary>
        /// Cached value of the font height with the guiPadding field added in to avoid the constant math.
        /// </summary>
        private float paddedFontHeight;

        /// <summary>
        /// Position of the next overlay element to be drawn using the available Draw functions in the class.
        /// Updates itself after each call.
        /// </summary>
        private Vector2 guiPosition;

        private float LastRenderTime { get; set; }
        private float LastUpdateTime { get; set; }

        /// <summary>
        /// Singleton object.  Not a lazy instantiated one.  Call GamePerformance.Initialize first.
        /// </summary>
        public static GamePerformance Instance
        {
            get { return _instance; }
        }

        /// <summary>
        /// Constructor.
        /// Adds three default trackers.
        /// </summary>
        /// <param name="game"></param>
        public GamePerformance(DwarfGame game)
        {
            _game = game;
            trackers = new List<Tracker>();
            internalTrackers = new Dictionary<string, Tracker>();
            threadLoopTrackers = new Dictionary<ThreadIdentifier, ThreadLoopTracker>();
            zoneList = new ConcurrentDictionary<string, TrackerZone>();

            threadLoopLockObject = new Object();
            internalTrackerLockObject = new Object();
            zoneListLockObject = new Object();

            internalWatch = Stopwatch.StartNew();

            trackers.Add(new FramerateTracker(this));
            trackers.Add(new UpdateTimeTracker(this));
            trackers.Add(new RenderTimeTracker(this));
        }

        /// <summary>
        /// Creates the singleton for use.
        /// </summary>
        /// <param name="game">Reference to the base DwarfGame object.</param>
        public static void Initialize(DwarfGame game)
        {
            _instance = new GamePerformance(game);
            microsecondMultiplier = (1.0f / Stopwatch.Frequency) * 1000000;
        }

        public static float microsecondMultiplier;

        public static int ThreadID
        {
            get
            {
                if (threadInitialized == false)
                {
                    threadInitialized = true;
                    threadIdentifier = threadCount;
                    threadName = System.Threading.Thread.CurrentThread.Name;
                    threadCount++;
                    zone = new Stack<TrackerZone>();
                    Instance.EnterZone("Unknown #" + threadIdentifier);
                }
                return threadIdentifier;
            }
        }

        /// <summary>
        /// Property to expose the shared watch time for Tracker classes.
        /// </summary>
        public long Elapsed
        {
            get { return _instance.internalWatch.ElapsedMilliseconds; }
        }

        // Unused options class to make things more generic and easier to use without rewritting Tracker classes.
        public class TrackerOptions
        {
            public int HistorySize;
            public bool RenderAsPercentofZone;
            public bool vanishWhenUnused;
            public TrackerOptions()
            {

            }
        }

        public enum CollectionZone
        {
            None = 0,
            Update,
            Render,
            Unknown,
            Count
        }

        public class TrackerZone
        {
            private string name;
            private int id;
            private Object lockObject;
            private int entranceCount;
            private long time;
            private long lastZoneTime;
            private static int nextID = 0;

            public TrackerZone()
            {
                id = nextID;
                nextID++;
                lockObject = new Object();
            }

            public TrackerZone(string zoneName)
            {
                id = nextID;
                nextID++;
                lockObject = new object();
                name = zoneName;
            }

            public void Enter()
            {
                lock (lockObject)
                {
                    entranceCount++;
                    time = Stopwatch.GetTimestamp();
                }
            }

            public void Exit()
            {
                if (entranceCount == 0)
                    throw new Exception("TrackerZone " + name + " has called Exit before Enter.");
                lock (lockObject)
                {
                    entranceCount--;
                    time = Stopwatch.GetTimestamp() - time;
                    lastZoneTime = time;
                }
            }

            public bool IsInZone()
            {
                return (entranceCount > 0);
            }

            public long LastZoneTime
            {
                get
                {
                    lock (lockObject)
                    {
                        return lastZoneTime;
                    }
                }
            }

            public int ID { get { return id; } }
            public string Name { get { return name; } }
        }

        // Tracks how many function calls happen per time period.  Aka, update/render or per-second.
        public class FunctionCounterTracker
        {

        }

        // Tracks something while handling recursive calls.
        public class RecursionTracker
        {

        }

        /// <summary>
        /// Tracks performance between two calls.  Does not properly handle recursive calls.
        /// </summary>
        public class PerformanceTracker : Tracker
        {
            readonly int maxHistory = 5;
            private string _name;

            private Dictionary<int, ZoneData> zoneData;
            private Object zoneDataLockObject;
            private int zonesUsed;

            public class ZoneData
            {
                private string threadName;
                private string name;
                private int maxHistory;
                private Queue<long> history;
                private long historyTotal;
                private long time;
                private long total;
                private long callCount;
                private bool tracking;

                public long CallCount { get { return callCount; } }
                public string ZoneName { get { return name; } }
                public string ThreadName { get { return threadName; } }

                public ZoneData(TrackerZone zone, int historySize)
                {
                    threadName = GamePerformance.threadName;
                    if (threadName == null) Debugger.Break();
                    name = zone.Name;
                    history = new Queue<long>(historySize);
                    maxHistory = historySize;
                }

                public void Start()
                {
                    if (threadName == "Main" && name == "Unknown #0") Debugger.Break();
                    if (tracking)
                        throw new Exception("ZoneData.Start called more than once in a row.");
                    tracking = true;
                    time = Stopwatch.GetTimestamp();
                }

                public void Stop(long endTimestamp)
                {
                    if (!tracking)
                        throw new Exception("ZoneData.Stop called without calling Start first.");
                    tracking = false;
                    time = endTimestamp - time;
                    total += time;
                    callCount++;
                }

                public void FinishCollection()
                {
                    if (callCount != 0)
                    {
                        historyTotal += total;
                        history.Enqueue(total);
                        if (history.Count > maxHistory)
                            historyTotal -= history.Dequeue();
                    }
                }

                public void Reset()
                {
                    total = 0;
                    callCount = 0;
                }

                public long GetLastResult()
                {
                    return total;
                }

                public float GetAverageResult()
                {
                    if (history.Count == 0) return 0;
                    return (float)historyTotal / history.Count;
                }
            }

            private string[] percentLegend = new string[] { "100%", "0%" };

            public PerformanceTracker(GamePerformance parent, string name)
                : base(parent)
            {
                _name = name;
                zonesUsed = 0;
                zoneData = new Dictionary<int, ZoneData>();
                zoneDataLockObject = new Object();
            }

            public void StartTracking()
            {
                int id = ThreadID;
                ZoneData data = GetData();
                data.Start();
            }

            public void StopTracking(long endTimestamp)
            {
                ZoneData data = GetData();
                data.Stop(endTimestamp);
            }

            private ZoneData GetData(bool autoCreate = true)
            {
                ZoneData data;
                TrackerZone z = zone.Peek();
                if (!zoneData.TryGetValue(z.ID, out data))
                {
                    if (!autoCreate) return null;
                    data = new ZoneData(z, maxHistory);
                    lock (zoneDataLockObject)
                    {
                        zoneData.Add(z.ID, data);
                    }
                    zonesUsed++;
                }
                return data;
            }

            public override void EnterZone()
            {
                ZoneData data = GetData(false);
                if (data != null) data.Reset();
            }

            public override void ExitZone()
            {
                ZoneData data = GetData(false);
                if (data != null) data.FinishCollection();
            }

            public override void Render()
            {
                lock (zoneDataLockObject)
                {
                    foreach (KeyValuePair<int, ZoneData> kvp in zoneData)
                    {
                        ZoneData data = kvp.Value;
                        if (data == null) continue;

                        float average = data.GetAverageResult();
                        average *= microsecondMultiplier;
                        _parent.DrawString(String.Format("{{{0}}}[{1}] {2}: {3}us {4}", data.ThreadName, data.ZoneName, _name, average, data.CallCount), Color.White);
                    }
                }
            }
        }

        /// <summary>
        /// Tracks a single variable and outputs it during the next render call using ToString().
        /// </summary>
        /// <typeparam name="T">A ValueType based type</typeparam>
        public class ValueTypeTracker<T> : Tracker where T : struct
        {
            private T _value;
            private string _name;

            public ValueTypeTracker()
                : base(null)
            {

            }

            public ValueTypeTracker(GamePerformance parent, string name)
                : base(parent)
            {
                _name = name;
            }

            public void Update(T value)
            {
                _value = value;
            }

            public override void Render()
            {
                _parent.DrawString(_name + ": " + _value.ToString(), Color.White);
                base.Render();
            }
        }

        /// <summary>
        /// Tracks a single variable and outputs it during the next render call using ToString().
        /// </summary>
        /// <typeparam name="T">An Object based type</typeparam>
        public class ReferenceTypeTracker<T> : Tracker where T : class
        {
            private string _value;
            private string _name;

            public ReferenceTypeTracker()
                : base(null)
            {

            }

            public ReferenceTypeTracker(GamePerformance parent, string name)
                : base(parent)
            {
                _name = name;
            }

            public void Update(T value)
            {
                _value = value.ToString();
            }

            public override void Render()
            {
                _parent.DrawString(_name + ": " + _value, Color.White);
                base.Render();
            }
        }

        /// <summary>
        /// Counts the number of billiseconds a full Game.Update call takes.
        /// Updates the parent's LastUpdateTime field for use.
        /// </summary>
        public class UpdateTimeTracker : Tracker
        {
            private readonly int maxHistory = 10;
            private Queue<long> history;
            private long total;
            private long time;

            public UpdateTimeTracker(GamePerformance parent)
                : base(parent)
            {
                history = new Queue<long>(maxHistory);
            }

            public override void PreUpdate()
            {
                time = Stopwatch.GetTimestamp();
                base.PreUpdate();
            }

            public override void PostUpdate()
            {
                time = Stopwatch.GetTimestamp() - time;
                total += time;
                history.Enqueue(time);
                if (history.Count > maxHistory)
                    total -= history.Dequeue();
                _parent.LastUpdateTime = time;
                base.PostUpdate();
            }

            public override void Render()
            {
                float average = 0;
                if (history.Count > 0) average = (float)total / history.Count;
                average = average * microsecondMultiplier;
                _parent.DrawString("Update time: " + average.ToString() + "us", Color.White);
                base.Render();
            }
        }

        /// <summary>
        /// Counts the number of milliseconds a full Game.Render call takes.
        /// Updates the parent's LastRenderTime field for use.
        /// </summary>
        public class RenderTimeTracker : Tracker
        {
            private readonly int maxHistory = 10;
            private Queue<long> history;
            private long total;
            private long time;

            public RenderTimeTracker(GamePerformance parent)
                : base(parent)
            {
                history = new Queue<long>(maxHistory);
            }

            public override void PreRender()
            {
                time = Stopwatch.GetTimestamp();
                base.PreRender();
            }

            public override void PostRender()
            {
                time = Stopwatch.GetTimestamp() - time;
                total += time;
                history.Enqueue(time);
                if (history.Count > maxHistory)
                    total -= history.Dequeue();
                _parent.LastRenderTime = time;
                base.PostRender();
            }

            public override void Render()
            {
                float average = 0;
                if (history.Count > 0) average = (float)total / history.Count;
                average = average * microsecondMultiplier;
                _parent.DrawString("Render time: " + average.ToString() + "us", Color.White);
                base.Render();
            }
        }

        /// <summary>
        /// Counts the number of Render calls that get done every second for the FPS display.
        /// </summary>
        public class FramerateTracker : Tracker
        {
            int fps;
            List<int> lastFPS;
            long lastSecond;
            int counter;
            string[] graphLegend;

            public FramerateTracker(GamePerformance parent)
                : base(parent)
            {
                graphLegend = new string[] { "60", "30", "10" };
                lastFPS = new List<int>();
            }

            public override void Render()
            {
                /*
                DwarfGame.SpriteBatch.DrawString(overlayFont, "Num Chunks " + ChunkManager.ChunkData.ChunkMap.Values.Count, new Vector2(5, 5), Color.White);
                DwarfGame.SpriteBatch.DrawString(overlayFont, "Max Viewing Level " + ChunkManager.ChunkData.MaxViewingLevel, new Vector2(5, 20), Color.White);
                DwarfGame.SpriteBatch.DrawString(font, "FPS " + fps, new Vector2(5, 35), Color.White);
                DwarfGame.SpriteBatch.DrawString(font, "60", new Vector2(5, 150 - 65), Color.White);
                DwarfGame.SpriteBatch.DrawString(font, "30", new Vector2(5, 150 - 35), Color.White);
                DwarfGame.SpriteBatch.DrawString(font, "10", new Vector2(5, 150 - 15), Color.White);
                
                for (int i = 0; i < lastFPS.Count; i++)
                {
                    float FPS = lastFPS[i];
                    DwarfGame.SpriteBatch.Draw(pixel,
                        new Rectangle(30 + i * 2, 150 - (int)FPS, 2, (int)FPS),
                        new Color(1.0f - FPS / 60.0f, FPS / 60.0f, 0.0f, 0.5f));
                }*/


                _parent.DrawString("FPS " + fps, Color.White);
                _parent.DrawChangeGraph(graphLegend, lastFPS, 60, new Vector2(0, 60));

                base.Render();
            }

            public override void PostRender()
            {
                long curTime = _parent.Elapsed;
                long sinceLastSecond = curTime - lastSecond;
                if (sinceLastSecond >= 1000)
                {
                    fps = counter;
                    lastFPS.Add(fps);
                    if (lastFPS.Count > 100) lastFPS.RemoveAt(0);
                    counter = 0;
                    lastSecond += 1000;
                }
                else
                {
                    counter++;
                }

                base.PostRender();
            }
        }

        /// <summary>
        /// Unused tracker type.  Meant to be used for trackers created by provding a name to a Track function.
        /// </summary>
        public class InternalTracker : Tracker
        {
            protected string _name;

            public InternalTracker()
                : base(null)
            {

            }

            public void GenericConstructor(GamePerformance parent, string name)
            {
                _parent = parent;
                _name = name;
            }
        }

        public class ThreadLoopTracker : Tracker
        {
            private object lockObject;
            private string name;
            private int id;
            private long time;
            private long finalTime;
            public ThreadLoopTracker(GamePerformance parent, string name, int threadID)
                : base(parent)
            {
                id = threadID;
                this.name = name;
                lockObject = new Object();
            }

            public override void PreThreadLoop()
            {
                time = Stopwatch.GetTimestamp();
                base.PreThreadLoop();
            }

            public override void PostThreadLoop()
            {
                time = Stopwatch.GetTimestamp() - time;
                lock (lockObject)
                {
                    finalTime = time;
                }
                base.PostThreadLoop();
            }

            public override void Render()
            {
                float average;
                // Currently doesn't average.
                lock (lockObject)
                {
                    average = finalTime * microsecondMultiplier;
                }
                _parent.DrawString(String.Format("[{0}] {1}us", name, average), Color.White);
                base.Render();
            }
        }

        /// <summary>
        /// Abstract Tracker class for the other classes to inherit from.
        /// </summary>
        public class Tracker
        {
            protected GamePerformance _parent;

            public Tracker(GamePerformance parent)
            {
                _parent = parent;
            }

            public virtual void PreUpdate() { }

            public virtual void PostUpdate() { }

            public virtual void PreRender() { }

            public virtual void PostRender() { }

            public virtual void Render() { }

            public virtual void PreThreadLoop() { }

            public virtual void PostThreadLoop() { }

            public virtual void EnterZone() { }

            public virtual void ExitZone() { }
        }

        #region Internal tracking functions
        /*
        public Tracker GetInternalTracker<T>(String name) where T: InternalTracker, new()
        {
            InternalTracker t;
            if (inlineTrackers.TryGetValue(name, out t))
            {
            }
            else
            {
                t = new T();
                t.GenericConstructor(this, name);
                inlineTrackers.Add(name, t);
                trackers.Add(t);
            }

            return t;
        } */

        public void RegisterThreadLoopTracker(String name, ThreadIdentifier identifier)
        {
            int threadID = ThreadID;

            switch (identifier)
            {
                case ThreadIdentifier.RebuildVoxels:
                    //rebuildThreadIdentifier = threadID;
                    break;
                case ThreadIdentifier.RebuildWater:
                    //waterThreadIdentifier = threadID;
                    break;
                default:
                    break;
            }

            ThreadLoopTracker loopTracker = new ThreadLoopTracker(this, name, threadID);

            lock (threadLoopLockObject)
            {
                threadLoopTrackers[identifier] = loopTracker;
            }
        }

        public void RegisterZone(String zoneName)
        {

        }

        public void EnterZone(String zoneName)
        {
            int id = ThreadID;
            TrackerZone z;
            if (!zoneList.TryGetValue(zoneName, out z))
            {
                z = new TrackerZone(zoneName);
                zoneList.TryAdd(zoneName, z);
            }
            zone.Push(z);
            lock (internalTrackerLockObject)
            {
                foreach (KeyValuePair<string, Tracker> kvp in internalTrackers)
                {
                    kvp.Value.EnterZone();
                }
            }
        }

        public void ExitZone(String zoneName)
        {
            TrackerZone z;
            if (!zoneList.TryGetValue(zoneName, out z))
                throw new Exception("Unknown TrackerZone found.");
            TrackerZone cur = zone.Peek();
            if (z != zone.Peek())
                throw new Exception("TrackerZone " + z.Name + " trying to exit but we are actually in " + cur.Name);
            lock (internalTrackerLockObject)
            {
                foreach (KeyValuePair<string, Tracker> kvp in internalTrackers)
                {
                    kvp.Value.ExitZone();
                }
            }
            zone.Pop();
        }

        /// <summary>
        /// Call this at the start of the location you want to track performance of.
        /// </summary>
        /// <param name="name">The display name for the tracker.</param>
        public void StartTrackPerformance(String name)
        {
            Tracker t;
            if (internalTrackers.TryGetValue(name, out t))
            {
                if (!(t is PerformanceTracker))
                {
                    Debug.WriteLine("'" + name + "' is not a PerformanceTracker type.");
                    return;
                }
            }
            else
            {
                t = new PerformanceTracker(this, name);
                internalTrackers.Add(name, t);
            }

            (t as PerformanceTracker).StartTracking();
        }

        /// <summary>
        /// Call this at the end of the location you want to track performance of.
        /// </summary>
        /// <param name="name">The display name of the tracker you used to start tracking.</param>
        public void StopTrackPerformance(String name)
        {
            //if (ThreadID != GamePerformance.mainThreadIdentifier) return;
            long endTime = Stopwatch.GetTimestamp();
            Tracker t;
            if (internalTrackers.TryGetValue(name, out t))
            {
                if (!(t is PerformanceTracker))
                {
                    Debug.WriteLine("'" + name + "' is not a PerformanceTracker type.");
                    return;
                }
            }
            else
            {
                Debug.WriteLine("'" + name + "' never hit StartTracking.");
                return;
            }

            (t as PerformanceTracker).StopTracking(endTime);
        }

        /// <summary>
        /// Single shot tracker.  Saves the value of that variable at that point to draw during the next Render.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The display name of the tracker.</param>
        /// <param name="variable">The ValueType based variable you wish to track.</param>
        public void TrackValueType<T>(String name, T variable) where T : struct
        {
            Tracker t;
            if (internalTrackers.TryGetValue(name, out t))
            {
                if (!(t is ValueTypeTracker<T>))
                {
                    Debug.WriteLine("'" + name + "' is not a ValueTypeTracker type.");
                    return;
                }
            }
            else
            {
                t = new ValueTypeTracker<T>(this, name);
                internalTrackers.Add(name, t);
                trackers.Add(t);
            }

            (t as ValueTypeTracker<T>).Update(variable);
        }

        /// <summary>
        /// Single shot tracker.  Saves the value of that variable at that point to draw during the next Render.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The display name of the tracker.</param>
        /// <param name="variable">The Object based variable you wish to track.</param>
        public void TrackReferenceType<T>(String name, T variable) where T : class
        {
            Tracker t;
            if (internalTrackers.TryGetValue(name, out t))
            {
                if (!(t is ReferenceTypeTracker<T>))
                {
                    Debug.WriteLine("'" + name + "' is not a ReferenceTypeTracker type.");
                    return;
                }
            }
            else
            {
                t = new ReferenceTypeTracker<T>(this, name);
                internalTrackers.Add(name, t);
                trackers.Add(t);
            }

            (t as ReferenceTypeTracker<T>).Update(variable);
        }
        #endregion

        #region Update Hooks
        /// <summary>
        /// Base hook for things to be done before Game.Update
        /// </summary>
        public void PreUpdate()
        {
            EnterZone("Update");
            foreach (Tracker t in trackers)
            {
                if (t != null) t.PreUpdate();
            }

            Update();
        }

        /// <summary>
        /// Hook called after PreUpdate but does not chain down to the trackers.
        /// Used to handle keyboard input and other global state changes.
        /// </summary>
        public void Update()
        {
            KeyboardState keyboard = Keyboard.GetState();

            if (keyboard.IsKeyDown(ControlSettings.Mappings.TogglePerformanceOverlay))
            {
                if (!overlayKeyPressed) overlayKeyPressed = true;
            }
            else
            {
                if (overlayKeyPressed)
                {
                    SoundManager.PlaySound(ContentPaths.Audio.pick, .25f);
                    GameSettings.Default.DrawDebugData = !GameSettings.Default.DrawDebugData;
                    overlayKeyPressed = false;
                }
            }

            if (keyboard.IsKeyDown(ControlSettings.Mappings.DebugToggle1))
            {
                if (!debug1ToggleKeyPressed) debug1ToggleKeyPressed = true;
            }
            else
            {
                if (debug1ToggleKeyPressed)
                {
                    SoundManager.PlaySound(ContentPaths.Audio.pick, .25f);
                    DebugToggle1 = !DebugToggle1;
                    debug1ToggleKeyPressed = false;
                }
            }

            if (keyboard.IsKeyDown(ControlSettings.Mappings.DebugToggle2))
            {
                if (!debug2ToggleKeyPressed) debug2ToggleKeyPressed = true;
            }
            else
            {
                if (debug2ToggleKeyPressed)
                {
                    SoundManager.PlaySound(ContentPaths.Audio.pick, .25f);
                    DebugToggle2 = !DebugToggle2;
                    debug2ToggleKeyPressed = false;
                }
            }

        }

        /// <summary>
        /// Base hook for things to be done after Game.Update
        /// </summary>
        public void PostUpdate()
        {
            foreach (Tracker t in trackers)
            {
                if (t != null) t.PostUpdate();
            }
            ExitZone("Update");
        }

        /// <summary>
        /// Base hook for things to be done before Game.Render
        /// </summary>
        public void PreRender()
        {
            EnterZone("Render");
            foreach (Tracker t in trackers)
            {
                if (t != null) t.PreRender();
            }
        }

        /// <summary>
        /// Base hook for things to be done after Game.Render
        /// </summary>
        public void PostRender()
        {
            foreach (Tracker t in trackers)
            {
                if (t != null) t.PostRender();
            }
            ExitZone("Render");
        }

        /// <summary>
        /// Sets up for and renders all current trackers.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Render(SpriteBatch spriteBatch)
        {
            if (_game.GraphicsDevice.IsDisposed ||
                spriteBatch.IsDisposed ||
                spriteBatch.GraphicsDevice.IsDisposed) return;

            // Do not render if the setting is off.
            if (!GameSettings.Default.DrawDebugData) return;

            // If our magicPixel has vanished recreate it.
            if (pixel == null || pixel.IsDisposed)
            {
                Color[] white = new Color[1];
                white[0] = Color.White;
                pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
                pixel.SetData(white);
            }

            if (overlayFont == null)
            {
                try
                {
                    overlayFont = _game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default);
                }
                catch (Exception)
                {
                    Debug.WriteLine("GamePerformance.Render could not get the Font to use.");
                }

                // We'll stop if we couldn't get a font to use.
                if (overlayFont == null) return;
                fontHeight = overlayFont.LineSpacing;
                paddedFontHeight = fontHeight + guiPadding;
            }

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            guiPosition = new Vector2(5, 35);

            foreach (Tracker t in trackers)
            {
                if (t != null) t.Render();
            }

            lock (threadLoopLockObject)
            {
                foreach (KeyValuePair<ThreadIdentifier, ThreadLoopTracker> kvp in threadLoopTrackers)
                {
                    if (kvp.Value != null) kvp.Value.Render();
                }
            }

            lock (internalTrackerLockObject)
            {
                foreach (KeyValuePair<string, Tracker> kvp in internalTrackers)
                {
                    if (kvp.Value != null) kvp.Value.Render();
                }
            }

            spriteBatch.End();
        }

        public void PreThreadLoop(ThreadIdentifier identifier)
        {
            ThreadLoopTracker loopTracker;
            if (threadLoopTrackers.TryGetValue(identifier, out loopTracker))
            {
                loopTracker.PreThreadLoop();
            }
            else
            {
                throw new Exception("ThreadIdentifier." + Enum.GetName(typeof(ThreadIdentifier), identifier) + " used before RegisterThreadLoopTracker called for the type.");
            }
        }

        public void PostThreadLoop(ThreadIdentifier identifier)
        {
            ThreadLoopTracker loopTracker;
            if (threadLoopTrackers.TryGetValue(identifier, out loopTracker))
            {
                loopTracker.PostThreadLoop();
            }
            else
            {
                throw new Exception("ThreadIdentifier." + Enum.GetName(typeof(ThreadIdentifier), identifier) + " used before RegisterThreadLoopTracker called for the type.");
            }
        }
        #endregion

        #region Drawing Functions
        /// <summary>
        /// Function to draw a string from inside a Tracker.
        /// Uses the font size to automatically adjust downwards.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public void DrawString(string text, Color color)
        {
            DwarfGame.SpriteBatch.DrawString(overlayFont, text, guiPosition, color);

            guiPosition.Y += paddedFontHeight;
        }

        /// <summary>
        /// Draws a graph based on the data presented to it.
        /// </summary>
        /// <param name="legend">A string array containing the labels to print down the side of the graph.</param>
        /// <param name="data">An enumerable block of data in integer format.</param>
        /// <param name="height">The height of the final graph</param>
        /// <param name="bounds">The bounds for the data set, lower then upper.</param>
        public void DrawChangeGraph(string[] legend, IEnumerable<int> data, int height, Vector2 bounds)
        {
            int labelSizeX = 0;

            for (int i = 0; i < legend.Length; i++)
            {
                Vector2 stringSize = overlayFont.MeasureString(legend[i]);
                if (stringSize.X >= labelSizeX)
                    labelSizeX = (int)stringSize.X;

                Vector2 drawPosition = guiPosition;

                drawPosition.Y += i * paddedFontHeight;

                DwarfGame.SpriteBatch.DrawString(overlayFont, legend[i], drawPosition, Color.White);
            }
            labelSizeX += (int)guiPadding;

            int count = 0;
            foreach (int value in data)
            {
                int lineHeight = (int)((value / bounds.Y) * height);
                DwarfGame.SpriteBatch.Draw(pixel,
                    new Rectangle((int)guiPosition.X + labelSizeX + count * 2, (int)guiPosition.Y + height - lineHeight, 2, lineHeight),
                    new Color(1.0f - value / bounds.Y, value / bounds.Y, 0.0f, 0.5f));
                count++;
            }
            /*
            for (int i = 0; i < lastFPS.Count; i++)
            {
                float FPS = lastFPS[i];
                DwarfGame.SpriteBatch.Draw(pixel,
                    new Rectangle(30 + i * 2, 150 - (int)FPS, 2, (int)FPS),
                    new Color(1.0f - FPS / 60.0f, FPS / 60.0f, 0.0f, 0.5f));
            }
             * */
            guiPosition.Y += height + guiPadding;

        }
        #endregion

        public void Dispose()
        {
            if (pixel != null) pixel.Dispose();
        }
    }
}
