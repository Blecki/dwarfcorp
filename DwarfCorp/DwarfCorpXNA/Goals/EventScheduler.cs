using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwarfCorp.Goals
{
    public class EventScheduler
    {
        public float CurrentDifficulty = 5;
        public float TargetDifficulty = 30;
        public float DifficultyDecayPerHour = 0.5f;
        public int MaxForecast = 10;
        public int MinSpacingHours = 1;
        public int MaxSpacingHours = 4;
        public int MinimumStartTime = 8;
        public struct EventEntry
        {
            public ScheduledEvent Event;
            public DateTime Date;
        }

        public List<EventEntry> Forecast = new List<EventEntry>();
        public List<ScheduledEvent> ActiveEvents = new List<ScheduledEvent>();
        public EventLibrary Events = new EventLibrary();
        private int previousHour = -1;

        [JsonIgnore]
        public WorldManager World;

        [OnDeserialized]
        void OnDeserializing(StreamingContext context)
        {
            // Assume the context passed in is a WorldManager
            World = ((WorldManager)context.Context);
        }

        public EventScheduler()
        {

        }

        private void PopEvent(WorldManager world)
        {
            if (Forecast.Count > 0)
            {
                ScheduledEvent currentEvent = Forecast[0].Event;
                Forecast.RemoveAt(0);
                ActivateEvent(world, currentEvent);
            }
        }

        public void ActivateEvent(WorldManager world, ScheduledEvent currentEvent)
        {
            CurrentDifficulty += currentEvent.Difficulty;
            CurrentDifficulty = Math.Max(CurrentDifficulty, 0);
            currentEvent.Trigger(world);
            ActiveEvents.Add(currentEvent);
        }

        public float ForecastDifficulty(DateTime now)
        {
            float difficulty = CurrentDifficulty;
            DateTime curr = now;
            foreach (var entry in Forecast)
            {
                var e = entry.Event;
                var duration = entry.Date - curr;
                difficulty = Math.Max((float)(difficulty - DifficultyDecayPerHour * duration.TotalHours), 0);
                difficulty += e.Difficulty;
                difficulty = Math.Max(difficulty, 0);
                curr = entry.Date;
            }
            return difficulty;
        }
        
        private bool IsNight(DateTime time)
        {
            return time.Hour < 6 || time.Hour > 20;
        }

        public void AddRandomEvent(DateTime now)
        {
            float forecast = ForecastDifficulty(now);
            bool foundEvent = false;
            var randomEvent = new ScheduledEvent();
            int iters = 0;
            var filteredEvents = Forecast.Count == 0 ? Events.Events : Events.Events.Where(e => e.Name != Forecast.Last().Event.Name).ToList();
            while (!foundEvent && iters < 100)
            {
                iters++;
                float sumLikelihood = filteredEvents.Sum(ev => ev.Likelihood);
                float randLikelihood = MathFunctions.Rand(0, sumLikelihood);
                float p = 0;
                foreach (var ev in filteredEvents)
                {
                    if (forecast + ev.Difficulty > TargetDifficulty || forecast + ev.Difficulty < 0)
                    {
                        continue;
                    }
                    p += ev.Likelihood;
                    if (randLikelihood < p)
                    {
                        randomEvent = ev;
                        foundEvent = true;
                    }
                }
            }

            if (!foundEvent)
            {
                return;
            }
            DateTime randomTime = now;
            if (Forecast.Count == 0)
            {
                randomTime = now + new TimeSpan(MinimumStartTime + MathFunctions.RandInt(MinSpacingHours, MaxSpacingHours), 0, 0);
            }
            else
            {
                randomTime = Forecast.Last().Date + new TimeSpan(MathFunctions.RandInt(MinSpacingHours, MaxSpacingHours) + Forecast.Last().Event.CooldownHours, 0, 0);
            }

            if (randomEvent.AllowedTime == ScheduledEvent.TimeRestriction.OnlyDayTime)
            {
                while (IsNight(randomTime))
                {
                    randomTime += new TimeSpan(1, 0, 0);
                }
            }
            else if (randomEvent.AllowedTime == ScheduledEvent.TimeRestriction.OnlyNightTime)
            {
                while (!IsNight(randomTime))
                {
                    randomTime += new TimeSpan(1, 0, 0);
                }
            }
            Forecast.Add(new EventEntry()
            {
                Event = randomEvent,
                Date = randomTime
            });

        }

        public void Update(WorldManager world, DateTime now)
        {
            int hour = now.Hour;

            foreach (var e in ActiveEvents)
            {
                if (e.ShouldKeep(world))
                {
                    e.Update(world);
                }
                else
                {
                    e.Deactivate(world);
                }
            }
            ActiveEvents.RemoveAll(e => !e.ShouldKeep(world));
            if (hour == previousHour)
             return;
            previousHour = hour;
            CurrentDifficulty = Math.Max(CurrentDifficulty - DifficultyDecayPerHour, 0);

            if (Forecast.Count > 0 && now > Forecast[0].Date)
            {
                PopEvent(world);
            }

            int iters = 0;
            while (Forecast.Count < MaxForecast && iters < MaxForecast * 2)
            {
                iters++;
                AddRandomEvent(now);
            }
        }
    }
}
