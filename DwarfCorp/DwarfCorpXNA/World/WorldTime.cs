// WorldTime.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
