using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Collections.Concurrent;
using System.Threading;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class LiquidType_
    {
        public String Name;
        public byte ID;
        public String SplashName = "splat";
        public String SplashEntity = "";
        public int SplashCount = 2;
        public String SplashSound = "Audio/river";
        public float SpreadRate = 0.5f;
        public bool CausesDamage = false;
        public float TemperatureIncrease = 0.0f;
        public Color MinimapColor = Color.White;
        public bool ClearsGrass = false;
        public bool EvaporateToStone = false;

        [JsonIgnore] public Texture2D BaseTexture;
        [JsonIgnore] public Texture2D BumpTexture;
        public String BaseTexturePath;
        public String BumpTexturePath;
        public float Opactiy;
        public float WaveLength;
        public float WaveHeight;
        public float WindForce;
        public float MinOpacity;
        public Vector4 RippleColor;
        public Vector4 FlatColor;
        public float Reflection;

    }
}