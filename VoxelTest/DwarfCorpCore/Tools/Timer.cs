using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    public class DwarfTime
    {
        public bool IsPaused { get; set; }
        public TimeSpan ElapsedGameTime { get; set; }
        public TimeSpan TotalGameTime { get; set; }
        public TimeSpan ElapsedRealTime { get; set; }
        public TimeSpan TotalRealTime { get; set; }

        public DwarfTime()
        {

        }

        public DwarfTime(TimeSpan total, TimeSpan elapsed)
        {
            ElapsedGameTime = elapsed;
            TotalGameTime = total;
            ElapsedRealTime = ElapsedGameTime;
            TotalRealTime = TotalGameTime;
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
        }

        public void Update(GameTime time)
        {
            ElapsedGameTime = new TimeSpan(0);
            ElapsedRealTime = time.ElapsedGameTime;
            TotalRealTime = time.TotalGameTime;
            if (IsPaused) return;
            else
            {
                ElapsedGameTime = time.ElapsedGameTime;
                TotalGameTime += ElapsedGameTime;
            }
        }
    }

    /// <summary>
    /// A timer fires at a fixed interval when updated. Some timers automatically reset.
    /// Other timers need to be manually reset.
    /// </summary>
    public class Timer
    {
        private float StartTimeSeconds { get; set; }
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

        public Timer(float targetTimeSeconds, bool triggerOnce, TimerMode mode = TimerMode.Game)
        {
            TargetTimeSeconds = targetTimeSeconds;
            CurrentTimeSeconds = 0.0f;
            TriggerOnce = triggerOnce;
            HasTriggered = false;
            StartTimeSeconds = -1;
        }

        public bool Update(DwarfTime t)
        {
            if(null == t)
            {
                return false;
            }

            float seconds = (float)(Mode == TimerMode.Game ? t.TotalGameTime.TotalSeconds : t.TotalRealTime.TotalSeconds);

            if(!TriggerOnce && HasTriggered)
            {
                HasTriggered = false;
                CurrentTimeSeconds = 0.0f;
                StartTimeSeconds = -1;
            }

            if(StartTimeSeconds < 0)
            {
                StartTimeSeconds = seconds;
            }

            CurrentTimeSeconds = seconds - StartTimeSeconds;

            if(CurrentTimeSeconds > TargetTimeSeconds)
            {
                HasTriggered = true;
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

}