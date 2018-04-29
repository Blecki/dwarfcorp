using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals
{
    [JsonObject(IsReference = true)]
    public struct ScheduledEvent
    {
        public string Name;
        public string Description;
        public float Difficulty;
        [JsonIgnore]
        public DateTime Date;
    }

    public class EventLibrary
    {
        public List<ScheduledEvent> Events = new List<ScheduledEvent>();
    }


    [JsonObject(IsReference = true)]
    public class EventScheduler
    {
        public float CurrentDifficulty = 0;
        public float TargetDifficulty = 10;
        public float DifficultyDecayPerHour = 1f;
        public int MaxForecast = 10;
        public int MinSpacingHours = 1;
        public int MaxSpacingHours = 12;
        public List<ScheduledEvent> Forecast = new List<ScheduledEvent>();
        public EventLibrary Events = new EventLibrary();
        private int previousHour = -1;

        public EventScheduler()
        {

        }

        public void PopEvent()
        {
            if (Forecast.Count > 0)
            {
                ScheduledEvent currentEvent = Forecast[0];
                Forecast.RemoveAt(0);
                CurrentDifficulty += currentEvent.Difficulty;

                // TODO trigger the event somehow.
            }
        }

        public void Update(DateTime now)
        {
            int hour = now.Hour;
            if (hour == previousHour)
                return;
            previousHour = hour;
            CurrentDifficulty = Math.Max(CurrentDifficulty - DifficultyDecayPerHour, TargetDifficulty);
        }
    }
}
