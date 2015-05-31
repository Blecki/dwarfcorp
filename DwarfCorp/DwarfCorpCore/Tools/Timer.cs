// Timer.cs
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

    public class DwarfTime
    {
        public bool IsPaused { get; set; }
        public TimeSpan ElapsedGameTime { get; set; }
        public TimeSpan TotalGameTime { get; set; }
        public TimeSpan ElapsedRealTime { get; set; }
        public TimeSpan TotalRealTime { get; set; }

        public DwarfTime()
        {

        }

        public DwarfTime(TimeSpan total, TimeSpan elapsed)
        {
            ElapsedGameTime = elapsed;
            TotalGameTime = total;
            ElapsedRealTime = ElapsedGameTime;
            TotalRealTime = TotalGameTime;
        }

        public GameTime ToGameTime()
        {
            return new GameTime(TotalGameTime, ElapsedGameTime);
        }

        public DwarfTime(GameTime time)
        {
            ElapsedGameTime = time.ElapsedGameTime;
            TotalGameTime = time.TotalGameTime;
            ElapsedRealTime = time.ElapsedGameTime;
            TotalRealTime = time.TotalGameTime;
        }

        public void Update(GameTime time)
        {
            ElapsedGameTime = new TimeSpan(0);
            ElapsedRealTime = time.ElapsedGameTime;
            TotalRealTime = time.TotalGameTime;
            if (IsPaused) return;
            else
            {
                ElapsedGameTime = time.ElapsedGameTime;
                TotalGameTime += ElapsedGameTime;
            }
        }

        [JsonIgnore]
        public static DwarfTime LastTime { get; set; }

        [JsonIgnore]
        public static float Dt
        {
            get { return (float) LastTime.ElapsedGameTime.TotalSeconds; }
        }
    }

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

        public TimerMode Mode { get; set; }

        public enum TimerMode
        {
            Real,
            Game
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
            if(null == t)
            {
                return false;
            }

            float seconds = (float)(Mode == TimerMode.Game ? t.TotalGameTime.TotalSeconds : t.TotalRealTime.TotalSeconds);

            if(!TriggerOnce && HasTriggered)
            {
                HasTriggered = false;
                CurrentTimeSeconds = 0.0f;
                StartTimeSeconds = -1;
            }

            if(StartTimeSeconds < 0)
            {
                StartTimeSeconds = seconds;
            }

            CurrentTimeSeconds = seconds - StartTimeSeconds;

            if(CurrentTimeSeconds > TargetTimeSeconds)
            {
                HasTriggered = true;
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