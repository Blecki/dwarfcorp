using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;

namespace DwarfCorp
{
    /// <summary>
    /// This is a special GameTime class that allows the time to be sped up,
    /// slowed down, or paused. There is a notion of "Real Time" and "Game Time".
    /// Real time always ticks with the system clock. "Game Time" ticks with a speed
    /// multiplier and knows about pausing.
    /// </summary>
    public class DwarfTime
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is paused.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is paused; otherwise, <c>false</c>.
        /// </value>
        public bool IsPaused { get; set; }
        /// <summary>
        /// Gets or sets the elapsed game time since the last frame.
        /// </summary>
        /// <value>
        /// The elapsed game time.
        /// </value>
        public TimeSpan ElapsedGameTime { get; set; }
        /// <summary>
        /// Gets or sets the total game time since the start of the game.
        /// </summary>
        /// <value>
        /// The total game time.
        /// </value>
        public TimeSpan TotalGameTime { get; set; }
        /// <summary>
        /// Gets or sets the elapsed real time since the last frame.
        /// </summary>
        /// <value>
        /// The elapsed real time.
        /// </value>
        public TimeSpan ElapsedRealTime { get; set; }
        /// <summary>
        /// Gets or sets the total real time since the start of the game.
        /// </summary>
        /// <value>
        /// The total real time.
        /// </value>
        public TimeSpan TotalRealTime { get; set; }
        /// <summary>
        /// Gets or sets the speed multiplier.
        /// </summary>
        /// <value>
        /// The speed.
        /// </value>
        public float Speed { get; set; }
        public DwarfTime()
        {
            Speed = 1.0f;
        }

        public DwarfTime(TimeSpan total, TimeSpan elapsed)
        {
            ElapsedGameTime = elapsed;
            TotalGameTime = total;
            ElapsedRealTime = ElapsedGameTime;
            TotalRealTime = TotalGameTime;
            Speed = 1.0f;
        }


        public GameTime ToRealTime()
        {
            return new GameTime(TotalRealTime, ElapsedRealTime);
        }

        public GameTime ToGameTime()
        {
            return new GameTime(TotalGameTime, ElapsedGameTime);
        }

        public DwarfTime(GameTime time)
        {
            ElapsedGameTime = time.ElapsedGameTime;
            TotalGameTime = time.TotalGameTime;
            ElapsedRealTime = time.ElapsedGameTime;
            TotalRealTime = time.TotalGameTime;
            Speed = 1.0f;
        }

        public void Update(GameTime time)
        {
            ElapsedGameTime = new TimeSpan(0);
            ElapsedRealTime = time.ElapsedGameTime;
            TotalRealTime = time.TotalGameTime;
            float slowmo = GameSettings.Current.EnableSlowMotion ? 0.1f : 1.0f;
            if (IsPaused) return;
            else
            {
                ElapsedGameTime = new TimeSpan((long)(time.ElapsedGameTime.Ticks * Speed * slowmo));
                if (ElapsedGameTime.TotalSeconds > MaximumElapsedGameTime * Speed * slowmo)
                    ElapsedGameTime = TimeSpan.FromSeconds(MaximumElapsedGameTime * Speed * slowmo);
                TotalGameTime += ElapsedGameTime;
            }
        }

        [JsonIgnore]
        public static DwarfTime LastTimeX = new DwarfTime();

        [JsonIgnore]
        public static double MaximumElapsedGameTime = 60.0f / 10.0f;

        public static double Tick()
        {
            return DwarfTime.LastTimeX.TotalRealTime.TotalSeconds;
        }

        public static double Tock(double start)
        {
            return Tick() - start;
        }
    }
}
