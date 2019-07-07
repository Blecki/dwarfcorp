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

                World.RemoveFromSpeciesTracking(Stats.CurrentClass);

            NoiseMaker.MakeNoise("Die", Physics.Position, true);

            if (AI.Stats.Money > 0)
                EntityFactory.CreateEntity<CoinPile>("Coins Resource", AI.Position, Blackboard.Create("Money", AI.Stats.Money));

            if (Stats.Species.HasMeat)
            {
                String type = Stats.CurrentClass.Name + " " + "Meat";

                if (!Library.DoesResourceTypeExist(type))
                {
                    var r = Library.CreateResourceType(Library.GetResourceType(Stats.Species.BaseMeatResource));
                    r.Name = type;
                    r.ShortName = type;
                    Library.AddResourceType(r);
                }

                Inventory.AddResource(new ResourceAmount(type, 1));
            }

            if (Stats.Species.HasBones)
            {
                String type = Stats.CurrentClass.Name + " Bone";

                if (!Library.DoesResourceTypeExist(type))
                {
                    var r = Library.CreateResourceType(Library.GetResourceType("Bone"));
                    r.Name = type;
                    r.ShortName = type;
                    Library.AddResourceType(r);
                }

                Inventory.AddResource(new ResourceAmount(type, 1));
            }

            base.Die();
        }
    }
}
