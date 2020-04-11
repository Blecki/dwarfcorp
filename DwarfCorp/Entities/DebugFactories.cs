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
            var materialResource = new Resource(Datastructures.SelectRandom(Library.EnumerateResourceTypesWithTag("TrinketMaterial")));
            var trinketResource = Library.CreateMetaResource("Trinket", null, new Resource("Trinket"), new List<Resource> { materialResource });

            if (trinketResource.HasValue(out var _trinketResource) && MathFunctions.RandEvent(0.5f))
            {
                var gemResource = new Resource(Datastructures.SelectRandom(Library.EnumerateResourceTypesWithTag("Gem")));
                trinketResource = Library.CreateMetaResource("GemTrinket", null, new Resource("Gem-set Trinket"), new List<Resource> { _trinketResource, gemResource });
            }

            if (trinketResource.HasValue(out var _trinketResource2))
                return new ResourceEntity(Manager, _trinketResource2, Position);

            return null;
        }

        [EntityFactory("RandFood")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var foods = Library.EnumerateResourceTypesWithTag("RawFood");
            if (Library.CreateMetaResource("Meal", null, new Resource("Meal"), new List<Resource> {
                new Resource(Datastructures.SelectRandom(foods).TypeName),
                new Resource(Datastructures.SelectRandom(foods).TypeName)
                }).HasValue(out var randResource))
                return new ResourceEntity(Manager, randResource, Position);
            return null;
        }
    }
}
