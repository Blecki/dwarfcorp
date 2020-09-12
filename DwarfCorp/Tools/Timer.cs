using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    /// <summary>
    /// A timer fires at a fixed interval when updated. Some timers automatically reset.
    /// Other timers need to be manually reset.
    /// </summary>
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
}
