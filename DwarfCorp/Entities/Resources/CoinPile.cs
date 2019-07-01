using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class CoinPile : ResourceEntity
    {
        public DwarfBux Money { get; set; }

        public CoinPile()
        {

        }

        public CoinPile(ComponentManager manager, Vector3 position) :
            base(manager, new ResourceAmount("Coins"), position)
        {
            Name = "Coins";
            Tags.Add("Coins");
        }
    }
}
