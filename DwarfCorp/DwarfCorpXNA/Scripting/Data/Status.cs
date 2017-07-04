// CreatureStatus.cs
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
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A creature status is a named value which has minimum and maximum thresholds for satisfaction.
    /// </summary>
    public class Status
    {
        private float currentValue;

        public string Name { get; set; }

        public float CurrentValue
        {
            get { return currentValue; }
            set { SetValue(value); }
        }

        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public float DissatisfiedThreshold { get; set; }
        public float SatisfiedThreshold { get; set; }

        [JsonIgnore]
        public int Percentage
        {
            get { return (int)((CurrentValue - MinValue) / (MaxValue - MinValue) * 100); }
        }

        public bool IsSatisfied()
        {
            return CurrentValue >= SatisfiedThreshold;
        }

        public bool IsDissatisfied()
        {
            return CurrentValue <= DissatisfiedThreshold;
        }

        public void SetValue(float v)
        {
            currentValue = Math.Abs(MaxValue - MinValue) < 1e-12 ? v : Math.Max(Math.Min(v, MaxValue), MinValue);
        }
    }
}