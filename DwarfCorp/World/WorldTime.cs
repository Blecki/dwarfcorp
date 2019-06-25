using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    /// <summary>
    /// This is a wrapper around the DateTime class which allows the game to go faster
    /// or slower. The days/hours/minutes actually pass in the game.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class WorldTime
    {
        private bool wasBeforeMidnight = false;
        private bool wasDay = false;
        public delegate void DayPassed(DateTime time);
        public event DayPassed NewDay;
        public event DayPassed NewNight;
        public event DayPassed Dawn;

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            wasDay = IsDay();
            wasBeforeMidnight = CurrentDate.Hour > 0;
        }

        protected virtual void OnDawn(DateTime time)
        {
            DayPassed handler = Dawn;
            if (handler != null) handler(time);
        }

        protected virtual void OnNewNight(DateTime time)
        {
            DayPassed handler = NewNight;
            if (handler != null) handler(time);
        }

        protected virtual void OnNewDay(DateTime time)
        {
            DayPassed handler = NewDay;
            if (handler != null) handler(time);
        }


        public DateTime CurrentDate { get; set; }

        public float Speed { get; set; }


        public WorldTime()
        {
            CurrentDate = new DateTime(1432, 4, 1, 8, 0, 0);
            Speed = 100.0f;
            wasDay = IsDay();
            wasBeforeMidnight = CurrentDate.Hour > 0;
        }

        public void Update(DwarfTime t)
        {
            if (t.IsPaused)
            {
                return;
            }

            CurrentDate = CurrentDate.Add(new TimeSpan(0, 0, 0, 0, (int)(t.ElapsedGameTime.Milliseconds * Speed)));

            /*
            if (CurrentDate.Hour == 0 && wasBeforeMidnight)
            {
                OnNewDay(CurrentDate);
            }
            */

            if (wasDay && IsNight())
            {
                OnNewNight(CurrentDate);
            }

            if (!wasDay && IsDay())
            {
                OnDawn(CurrentDate);
                OnNewDay(CurrentDate);
            }
            wasDay = IsDay();
            wasBeforeMidnight = CurrentDate.Hour > 0;
        }

        public float GetTotalSeconds()
        {
            return (float) CurrentDate.TimeOfDay.TotalSeconds;
        }


        public float GetTotalHours()
        {
            return (GetTotalSeconds() / 60.0f) / 60.0f;
        }

        public float GetTimeOfDay()
        {
            return GetTotalHours()/24.0f;
        }

        public float GetSkyLightness()
        {
            return  (float)Math.Cos(GetTotalHours() * 2 * Math.PI / 24.0f ) * 0.5f + 0.5f;
        }

        public bool IsNight()
        {
            return CurrentDate.Hour < 6 || CurrentDate.Hour >= 18;
        }

        public bool IsDay()
        {
            return !IsNight();
        }
    }
}
