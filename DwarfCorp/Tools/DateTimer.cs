using System;

namespace DwarfCorp
{
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
