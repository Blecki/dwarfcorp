using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class OverworldFaction
    {
        public string Name { get; set; }
        public string Race { get; set; }
        public Color PrimaryColor { get; set; }
        public float GoodWill { get; set; }
        public bool InteractiveFaction = false;
        public bool IsCorporate = false;

        public Dictionary<String, Politics> Politics = new Dictionary<string, Politics>();

        public Trade.ITradeEntity CreateTradeEntity(TradeEnvoy Envoy)
        {
            if (IsCorporate)
                return new Trade.CorporateTradeEntity(Envoy);
            else
                return new Trade.EnvoyTradeEntity(Envoy);
        }
    }
}
