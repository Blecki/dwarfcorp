using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals
{
    [JsonObject(IsReference = true)]
    public class ScheduledEvent
    {
        public string Name;
        public string Description;
        public DateTime Date;
        public float Difficulty;
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
        public float DifficultyDecayPerHour = 0.1f;
        public int MaxForecast = 10;
        public List<ScheduledEvent> Forecast = new List<ScheduledEvent>();
        public EventLibrary Events = new EventLibrary();

        public EventScheduler()
        {

        }

        public void Update(DateTime now)
        {

        }
    }
}
