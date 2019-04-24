using System;
using System.Collections.Generic;
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using Newtonsoft.Json.Schema;
using Math = System.Math;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public enum ScalarFieldType
    {
        Erosion,
        Weathering,
        Faults,
        Height,
        Temperature,
        Rainfall,
        Factions
    }

    [Serializable]
    public struct OverworldCell
    {
        private static byte ClampValue(float f)
        {
            return (byte)(Math.Min(Math.Max(f * 255.0f, 0.0f), 255.0f));
        }

        [JsonIgnore]
        public float Erosion
        {
            get { return (Erosion_) / 255.0f; }
            set { Erosion_ = ClampValue(value); }
        }

        [JsonIgnore]
        public float Weathering
        {
            get { return (Weathering_) / 255.0f; }
            set { Weathering_ = ClampValue(value); }
        }

        [JsonIgnore]
        public float Faults
        {
            get { return (Faults_) / 255.0f; }
            set { Faults_ = ClampValue(value); }
        }

        [JsonIgnore]
        public float Height
        {
            get { return (Height_) / 255.0f; }
            set { Height_ = ClampValue(value); }
        }

        [JsonIgnore]
        public float Temperature
        {
            get { return (Temperature_) / 255.0f; }
            set { Temperature_ = ClampValue(value); }
        }

        [JsonIgnore]
        public float Rainfall
        {
            get { return (Rainfall_) / 255.0f; }
            set { Rainfall_ = ClampValue(value); }
        }

        public byte Faction { get; set; }

        [JsonProperty] private byte Erosion_;
        [JsonProperty] private byte Weathering_;
        [JsonProperty] private byte Faults_;
        [JsonProperty] public byte Height_;
        [JsonProperty] private byte Temperature_;
        [JsonProperty] public byte Rainfall_;
        public byte Biome;

        public float GetValue(ScalarFieldType type)
        {
            switch (type)
            {
                case ScalarFieldType.Erosion:
                    return Erosion;
                case ScalarFieldType.Faults:
                    return Faults;
                case ScalarFieldType.Height:
                    return Height;
                case ScalarFieldType.Rainfall:
                    return Rainfall;
                case ScalarFieldType.Temperature:
                    return Temperature;
                case ScalarFieldType.Weathering:
                    return Weathering;
                case ScalarFieldType.Factions:
                    return Faction;
            }

            return -1.0f;
        }

        public void SetValue(ScalarFieldType type, float value)
        {
            switch (type)
            {
                case ScalarFieldType.Erosion:
                    Erosion = value;
                    break;
                case ScalarFieldType.Faults:
                    Faults = value;
                    break;
                case ScalarFieldType.Height:
                    Height = value;
                    break;
                case ScalarFieldType.Rainfall:
                    Rainfall = value;
                    break;
                case ScalarFieldType.Temperature:
                    Temperature = value;
                    break;
                case ScalarFieldType.Weathering:
                    Weathering = value;
                    break;
                case ScalarFieldType.Factions:
                    Faction = (byte)(value * 255.0f);
                    break;
            }
        }
    }
}