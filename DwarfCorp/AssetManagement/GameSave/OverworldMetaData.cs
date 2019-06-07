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
        public bool IsCorporate = false;

        public Trade.ITradeEntity CreateTradeEntity(TradeEnvoy Envoy)
        {
            if (IsCorporate)
                return new Trade.CorporateTradeEntity(Envoy);
            else
                return new Trade.EnvoyTradeEntity(Envoy);
        }
    }

    [Serializable]
    public class OverworldMetaData
    {
        public string Version;
        public OverworldGenerationSettings Settings;
        public Dictionary<int, String> BiomeTypeMap;
        public List<Resource> Resources; // Dislike the way resources are generated on the fly.

        public OverworldMetaData()
        {
        }

        public OverworldMetaData(GraphicsDevice device, OverworldGenerationSettings Settings)
        {
            this.Settings = Settings;

            BiomeTypeMap = BiomeLibrary.GetBiomeTypeMap(); // This may need to be saved in branch meta data.
            Resources = ResourceLibrary.Enumerate().Where(r => r.Generated).ToList();
        }
    }
}