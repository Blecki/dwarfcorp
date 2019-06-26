using Newtonsoft.Json;
using System;

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
            set { currentValue = Math.Abs(MaxValue - MinValue) < 1e-12 ? value : Math.Max(Math.Min(value, MaxValue), MinValue); }
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

        public bool IsCritical()
        {
            return CurrentValue <= DissatisfiedThreshold * 0.5f;
        }
    }
}