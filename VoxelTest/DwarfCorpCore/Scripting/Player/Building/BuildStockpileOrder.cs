using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class BuildStockpileOrder : BuildRoomOrder
    {
        public BuildStockpileOrder(Stockpile toBuild, Faction faction) 
            : base(toBuild, faction)
        {
        }

        public override void Build()
        {
            base.Build();
            Faction.Stockpiles.Add(ToBuild as Stockpile);
        }
    }
}
