using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public static partial class Library
    {
        // Todo: Move to callsite?
        public static MaybeNull<ResourceType> GetRandomApplicableCraftable(Faction faction, WorldManager World)
        {
            const int maxIters = 100;

            for (int i = 0; i < maxIters; i++)
            {
                var item = Datastructures.SelectRandom(Library.EnumerateResourceTypes().Where(r => r.Craft_Craftable));
                if (!World.HasResourcesWithTags(item.Craft_Ingredients))
                    continue;
                if (!faction.OwnedObjects.Any(o => o.Tags.Contains(item.Craft_Location)))
                    continue;
                return item;
            }

            return null;
        }
    }
}
