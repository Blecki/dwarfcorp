using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
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

        public Timer(float targetTimeSeconds, bool triggerOnce)
        {
            TargetTimeSeconds = targetTimeSeconds;
            CurrentTimeSeconds = 0.0f;
            TriggerOnce = triggerOnce;
            HasTriggered = false;
            StartTimeSeconds = -1;
        }

        public bool Update(GameTime t)
        {
            if(null == t)
            {
                return false;
            }


            if(!TriggerOnce && HasTriggered)
            {
                HasTriggered = false;
                CurrentTimeSeconds = 0.0f;
                StartTimeSeconds = -1;
            }

            if(StartTimeSeconds < 0)
            {
                StartTimeSeconds = (float) t.TotalGameTime.TotalSeconds;
            }

            CurrentTimeSeconds = (float) t.TotalGameTime.TotalSeconds - StartTimeSeconds;

            if(CurrentTimeSeconds > TargetTimeSeconds)
            {
                HasTriggered = true;
                return true;
            }

            return false;
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