// Timer.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

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

    /// <summary>
    /// A timer fires at a fixed interval when updated. Some timers automatically reset.
    /// Other timers need to be manually reset.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Timer
    {
        [JsonIgnore]
        public float StartTimeSeconds { get; set; }
        public float TargetTimeSeconds { get; set; }
        public float CurrentTimeSeconds { get; set; }
        public bool TriggerOnce { get; set; }
        public bool HasTriggered { get; set; }

        public TimerMode Mode { get; set; }

        public enum TimerMode
        {
            Real,
            Game
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            StartTimeSeconds = -1;
        }

        public Timer()
        {
            
        }

        public static Timer Clone(Timer other)
        {
            if (other == null)
            {
                return null;
            }
            return new Timer(other.TargetTimeSeconds, other.TriggerOnce, other.Mode);
        }

        public Timer(float targetTimeSeconds, bool triggerOnce, TimerMode mode = TimerMode.Game)
        {
            TargetTimeSeconds = targetTimeSeconds;
            CurrentTimeSeconds = 0.0f;
            TriggerOnce = triggerOnce;
            HasTriggered = false;
            StartTimeSeconds = -1;
            Mode = mode;
        }

        public bool Update(DwarfTime t)
        {
            if (null == t)
            {
                return false;
            }

            float seconds = (float)(Mode == TimerMode.Game ? t.TotalGameTime.TotalSeconds : t.TotalRealTime.TotalSeconds);

            if (!TriggerOnce && HasTriggered)
            {
                HasTriggered = false;
                CurrentTimeSeconds = 0.0f;
                StartTimeSeconds = -1;
            }

            if (HasTriggered && TriggerOnce)
            {
                return true;
            }

            if (StartTimeSeconds < 0)
            {
                StartTimeSeconds = seconds;
            }

            CurrentTimeSeconds = seconds - StartTimeSeconds;

            if (CurrentTimeSeconds > TargetTimeSeconds)
            {
                HasTriggered = true;
                CurrentTimeSeconds = TargetTimeSeconds;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            Reset(TargetTimeSeconds);
        }

        public void Reset(float time)
        {
            CurrentTimeSeconds = 0.0f;
            HasTriggered = false;
            TargetTimeSeconds = time;
            StartTimeSeconds = -1;
        }
    }

    public class DateTimer
    {
        public TimeSpan TargetSpan { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan CurrentTime { get; set; }
        public bool HasTriggered { get; set; }
        public bool TriggerOnce { get; set; }

        public DateTimer()
        {

        }

        public DateTimer(DateTime now, TimeSpan target)
        {
            StartTime = now;
            TargetSpan = target;
            HasTriggered = false;
            TriggerOnce = true;
        }

        public void Reset(DateTime now)
        {
            StartTime = now;
            HasTriggered = false;
        }

        public bool Update(DateTime now)
        {
            CurrentTime = now - StartTime;

            HasTriggered = CurrentTime > TargetSpan;

            if (!TriggerOnce && HasTriggered)
            {
                HasTriggered = false;
                StartTime = now;
            }

            if (HasTriggered && TriggerOnce)
            {
                return true;
            }

            return false;
        }

    }

}
