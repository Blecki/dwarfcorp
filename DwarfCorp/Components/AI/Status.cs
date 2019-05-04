using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Status
    {
        private float currentValue;

        public string Name;
        public string Adjective;

        public float CurrentValue
        {
            get { return currentValue; }
            set { SetValue(value); } // Todo: Migrate all to SetValue function?
        }

        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public float DissatisfiedThreshold { get; set; }
        public float SatisfiedThreshold { get; set; }

        [JsonIgnore] public int Percentage => (int)((CurrentValue - MinValue) / (MaxValue - MinValue) * 100);

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

        public bool IsCritical()
        {
            return CurrentValue <= DissatisfiedThreshold * 0.5f;
        }
    }
}