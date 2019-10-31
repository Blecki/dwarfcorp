using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Linq;

namespace DwarfCorp
{
    public partial class Creature : Health
    {
        public override void Die()
        {
            if (World == null) return; // WUT
            if (Stats == null) return; // SERIOUSLY WTF??

            if (!FirstUpdate)
                World.RemoveFromSpeciesTracking(Stats.Species);

            NoiseMaker.MakeNoise("Die", Physics.Position, true);

            if (AI.Stats.Money > 0)
                EntityFactory.CreateEntity<CoinPile>("Coins Resource", AI.Position, Blackboard.Create("Money", AI.Stats.Money));

            if (Stats.Species.HasMeat)
            {
                var meatResource = new Resource(Stats.Species.BaseMeatResource);
                meatResource.DisplayName = Stats.CurrentClass.Name + " Meat";
                Inventory.AddResource(meatResource);
            }

            if (Stats.Species.HasBones)
            {
                var generatedResource = new Resource("Bone");
                generatedResource.DisplayName = Stats.CurrentClass.Name + " Bone";
                Inventory.AddResource(generatedResource);
            }

            base.Die();
        }
    }
}
