using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class IronDoor : Door
    {
        [EntityFactory("Iron Door")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new IronDoor(Manager, Position, Manager.World.PlayerFaction, Data.GetData<List<ResourceAmount>>("Resources", null), "Iron Door");
        }

        public IronDoor()
        {
        }

        public IronDoor(ComponentManager manager, Vector3 position, Faction team, List<ResourceAmount> resourceType, string craftType) :
            base(manager, position, team, resourceType, craftType, 30.0f)
        {
            Name = "Wooden Door";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Asset = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32);
            Frame = new Point(1, 8);

            base.CreateCosmeticChildren(manager);
        }
    }
}
