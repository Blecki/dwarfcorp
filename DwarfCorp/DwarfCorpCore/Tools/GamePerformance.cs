using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

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
        private Boolean debugToggleKeyPressed;

        /// <summary>
        /// Stores if the key to toggle the overlay flag is pressed to allow it to change only once per keypress.
        /// </summary>
        private Boolean overlayKeyPressed;

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
            public TrackerOptions()
            {

            }
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
            readonly int maxHistory = 60;
            private Queue<int> history;
            private long total;
            private string _name;
            private long time;
            private bool tracking;
            private string[] percentLegend = new string[] { "100%", "0%" };

            public PerformanceTracker(GamePerformance parent, string name)
                : base(parent)
            {
                history = new Queue<int>();
                _name = name;
            }

            public void StartTracking()
            {
                if (tracking)
                    throw new Exception("PerformanceTracker.StartTracking called more than once in a row.");
                tracking = true;

                time = _parent.Elapsed;
            }

            public void StopTracking()
            {
                if (!tracking)
                    throw new Exception("PerformanceTracker.StopTracking called without calling StartTracking first.");
                tracking = false;

                time = _parent.Elapsed - time;
                total += time;
                history.Enqueue((int)time);
                if (history.Count > maxHistory)
                    total -= history.Dequeue();
            }

            public override void Render()
            {
                float average = 0;
                if (history.Count > 0) average = (float)total / history.Count;
                float percent = 0;
                if (_parent.LastRenderTime != 0) percent = average / _parent.LastRenderTime;
                _parent.DrawString(String.Format("{0}: {1:P2}%", _name, percent), Color.White);
                _parent.DrawChangeGraph(percentLegend, history, 40, new Vector2(0, history.Max()));
            }
        }

        /// <summary>
        /// Tracks a single variable and outputs it during the next render call using ToString().
        /// Value types will show the state at the time the TrackVariable call happens.
        /// Reference types will show the state when the text is rendered.  
        /// This should likely be fixed.  Could call .ToString during Update but that would cause constant String GC.
        /// Separate function with reference handling would be better.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class VariableTracker<T> : Tracker
        {
            private T _value;
            private string _name;

            public VariableTracker()
                : base(null)
            {

            }

            public VariableTracker(GamePerformance parent, string name)
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
        /// Counts the number of billiseconds a full Game.Update call takes.
        /// Updates the parent's LastUpdateTime field for use.
        /// </summary>
        public class UpdateTimeTracker : Tracker
        {
            readonly int maxHistory = 10;
            Queue<long> history;
            long total;
            long time;

            public UpdateTimeTracker(GamePerformance parent)
                : base(parent)
            {
                history = new Queue<long>();
            }

            public override void PreUpdate()
            {
                time = _parent.Elapsed;
                base.PreUpdate();
            }

            public override void PostUpdate()
            {
                time = _parent.Elapsed - time;
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
                _parent.DrawString("Update time: " + average.ToString() + "ms", Color.White);
                base.Render();
            }
        }

        /// <summary>
        /// Counts the number of milliseconds a full Game.Render call takes.
        /// Updates the parent's LastRenderTime field for use.
        /// </summary>
        public class RenderTimeTracker : Tracker
        {
            long time;
            public RenderTimeTracker(GamePerformance parent)
                : base(parent)
            {

            }

            public override void PreRender()
            {
                time = _parent.Elapsed;
                base.PreRender();
            }

            public override void PostRender()
            {
                time = _parent.Elapsed - time;
                _parent.LastRenderTime = time;
                base.PostRender();
            }

            public override void Render()
            {
                _parent.DrawString("Render time: " + time.ToString() + "ms", Color.White);
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
                trackers.Add(t);
            }

            (t as PerformanceTracker).StartTracking();
        }

        /// <summary>
        /// Call this at the end of the location you want to track performance of.
        /// </summary>
        /// <param name="name">The display name of the tracker you used to start tracking.</param>
        public void EndTrackPerformance(String name)
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
                Debug.WriteLine("'" + name + "' never hit StartTracking.");
                return;
            }

            (t as PerformanceTracker).StopTracking();
        }

        /// <summary>
        /// Single shot tracker.  Saves the value of that variable at that point to draw during the next Render.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The display name of the tracker.</param>
        /// <param name="variable">The variable you wish to track.</param>
        public void TrackVariable<T>(String name, T variable)
        {
            Tracker t;
            if (internalTrackers.TryGetValue(name, out t))
            {
                if (!(t is VariableTracker<T>))
                {
                    Debug.WriteLine("'" + name + "' is not a VariableTracker type.");
                    return;
                }
            }
            else
            {
                t = new VariableTracker<T>(this, name);
                internalTrackers.Add(name, t);
                trackers.Add(t);
            }

            (t as VariableTracker<T>).Update(variable);
        }
        #endregion

        #region Update Hooks
        /// <summary>
        /// Base hook for things to be done before Game.Update
        /// </summary>
        public void PreUpdate()
        {
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
                if (!debugToggleKeyPressed) debugToggleKeyPressed = true;
            }
            else
            {
                if (debugToggleKeyPressed)
                {
                    SoundManager.PlaySound(ContentPaths.Audio.pick, .25f);
                    GameSettings.Default.DrawDebugData = !GameSettings.Default.DrawDebugData;
                    debugToggleKeyPressed = false;
                }
            }

            if (keyboard.IsKeyDown(ControlSettings.Mappings.DebugToggle1))
            {
                if (!overlayKeyPressed) overlayKeyPressed = true;
            }
            else
            {
                if (overlayKeyPressed)
                {
                    SoundManager.PlaySound(ContentPaths.Audio.pick, .25f);
                    DebugToggle1 = !DebugToggle1;
                    overlayKeyPressed = false;
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
        }

        /// <summary>
        /// Base hook for things to be done before Game.Render
        /// </summary>
        public void PreRender()
        {
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

            spriteBatch.End();
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
