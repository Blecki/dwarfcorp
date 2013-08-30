using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Timer
    {
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
        }

        public bool Update(GameTime t)
        {
            if (!TriggerOnce && HasTriggered)
            {
                HasTriggered = false;
                CurrentTimeSeconds = 0.0f;
            }

            CurrentTimeSeconds += (float)t.ElapsedGameTime.TotalSeconds;

            if (CurrentTimeSeconds > TargetTimeSeconds)
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
        }
    }
}
