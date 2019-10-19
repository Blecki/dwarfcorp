using System;
using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;

namespace DwarfCorp
{
    public static class DebugFactories
    {
        [EntityFactory("RandTrinket")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            if (Library.CreateTrinketResource(Datastructures.SelectRandom(Library.EnumerateResourceTypes().Where(r => r.Tags.Contains("Material"))).TypeName, MathFunctions.Rand(0.1f, 3.5f)).HasValue(out var randResource))
                if (MathFunctions.RandEvent(0.5f))
                    if (Library.CreateEncrustedTrinketResourceType(randResource, new Resource(Datastructures.SelectRandom(Library.EnumerateResourceTypes().Where(r => r.Tags.Contains("Gem"))).TypeName)).HasValue(out var _rr))
                        return new ResourceEntity(Manager, _rr, Position);

            return null;
        }

        [EntityFactory("RandFood")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var foods = Library.EnumerateResourceTypesWithTag("RawFood");
            if (Library.CreateMealResource(Datastructures.SelectRandom(foods).TypeName, Datastructures.SelectRandom(foods).TypeName).HasValue(out var randResource))
                return new ResourceEntity(Manager, randResource, Position);
            return null;
        }
    }
}
