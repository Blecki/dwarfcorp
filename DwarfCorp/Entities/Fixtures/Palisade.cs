using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Palisade : CraftedFixture
    {
        [EntityFactory("Palisade")]
        private static GameComponent __factory01(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Palisade(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null));
        }

        public Palisade()
        {

        }

        public Palisade(ComponentManager manager, Vector3 position, List<ResourceAmount> resources) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(2, 4), new DwarfCorp.CraftDetails(manager, "Palisade", resources))
        {
            Name = "Palisade";
            Tags.Add("Defensive");
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            if (GetComponent<SimpleSprite>().HasValue(out var sprite))
            {
                sprite.OrientationType = SimpleSprite.OrientMode.Fixed;
                var transform = Matrix.CreateRotationY((float)(0.5f * Math.PI));
                transform.Translation = Vector3.UnitX * 0.5f;
                sprite.LocalTransform = transform;
            }
        }
    }
}
