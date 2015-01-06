using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This is a wrapper around the DateTime class which allows the game to go faster
    /// or slower. The days/hours/minutes actually pass in the game.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class WorldTime
    {
        public delegate void DayPassed(DateTime time);
        public event DayPassed NewDay;

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
        }

        public void Update(DwarfTime t)
        {
            bool beforeMidnight = CurrentDate.Hour > 0;
            CurrentDate = CurrentDate.Add(new TimeSpan(0, 0, 0, 0, (int)(t.ElapsedGameTime.Milliseconds * Speed)));

            if (CurrentDate.Hour == 0 && beforeMidnight)
            {
                OnNewDay(CurrentDate);
            }
        }

        public float GetTotalSeconds()
        {
            return (float) CurrentDate.TimeOfDay.TotalSeconds;
        }


        public float GetTotalHours()
        {
            return (GetTotalSeconds() / 60.0f) / 60.0f;
        }

        public float GetSkyLightness()
        {
            return  (float)Math.Cos(GetTotalHours() * 2 * Math.PI / 24.0f ) * 0.5f + 0.5f;
        }

        public bool IsNight()
        {
            return CurrentDate.Hour < 6 || CurrentDate.Hour > 18;
        }

        public bool IsDay()
        {
            return !IsNight();
        }
    }
}
