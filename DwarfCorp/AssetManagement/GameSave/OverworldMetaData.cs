using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DwarfCorp
{
    public class OverworldFaction
    {
        public string Name { get; set; }
        public string Race { get; set; }
        public Color PrimaryColor { get; set; }
        public Color SecondaryColor { get; set; }
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public float GoodWill { get; set; }
        public bool InteractiveFaction = false;
        public bool IsMotherland = false;
    }

    [Serializable]
    public class OverworldMetaData
    {
        public string Version;
        public OverworldGenerationSettings Settings;
        public Dictionary<int, String> BiomeTypeMap;

        public OverworldMetaData()
        {
        }

        public OverworldMetaData(GraphicsDevice device, OverworldGenerationSettings Settings)
        {
            this.Settings = Settings;

            BiomeTypeMap = BiomeLibrary.GetBiomeTypeMap();
        }
    }
}