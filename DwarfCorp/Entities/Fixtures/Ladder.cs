using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Ladder : CraftedFixture
    {
        [EntityFactory("Ladder")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var resources = Data.GetData<List<ResourceAmount>>("Resources", null);
            var craftType = Data.GetData<string>("CraftType", null);
            if (resources == null && craftType != null)
            {
                resources = new List<ResourceAmount>();
                if (Library.GetCraftable(craftType).HasValue(out var craftItem))
                    foreach (var resource in craftItem.RequiredResources)
                    {
                        var genericResource = Library.EnumerateResourceTypesWithTag(resource.Type).FirstOrDefault();
                        resources.Add(new ResourceAmount(genericResource, resource.Count));
                    }
            }
            else if (resources == null && craftType == null)
            {
                craftType = "Wooden Ladder";
                resources = new List<ResourceAmount>() { new ResourceAmount("Wood") };
            }
            else if (craftType == null)
            {
                craftType = "Wooden Ladder";
            }

            return new Ladder(
                Manager,
                Position,
                resources, craftType);
        }

        protected static Dictionary<Resource.ResourceTags, Point> Sprites = new Dictionary<Resource.ResourceTags, Point>()
        {
            {
                Resource.ResourceTags.Metal,
                new Point(3, 8)
            },
            {
                Resource.ResourceTags.Stone,
                new Point(2, 8)
            },
            {
                Resource.ResourceTags.Wood,
                new Point(2, 0)
            }
        };

        protected static Point DefaultSprite = new Point(2, 8);


        public Ladder()
        {

        }

        public Ladder(ComponentManager manager, Vector3 position, List<ResourceAmount> resourceType, string craftType) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new FixtureCraftDetails(manager)
            {
                Resources = resourceType.ConvertAll(p => new ResourceAmount(p)),
                Sprites = Ladder.Sprites,
                DefaultSpriteFrame = Ladder.DefaultSprite,
                CraftType = craftType
            }, SimpleSprite.OrientMode.Fixed)
        {
            this.LocalBoundingBoxOffset = new Vector3(0, 0, 0.45f);
            this.BoundingBoxSize = new Vector3(0.7f, 1, 0.1f);
            this.SetFlag(Flag.RotateBoundingBox, true);

            Name = resourceType[0].Type + " Ladder";
            Tags.Add("Climbable");
            OrientToWalls();
            CollisionType = CollisionType.Static;
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            if (GetComponent<SimpleSprite>().HasValue(out var sprite))
            {
                sprite.OrientationType = SimpleSprite.OrientMode.Fixed;
                sprite.LocalTransform = Matrix.CreateTranslation(new Vector3(0, 0, 0.45f)) * Matrix.CreateRotationY(0.0f);
            }

            if (GetComponent<GenericVoxelListener>().HasValue(out var sensor))
            {
                sensor.LocalBoundingBoxOffset = new Vector3(0.0f, 0.0f, 1.0f);
                sensor.SetFlag(Flag.RotateBoundingBox, true);
                sensor.PropogateTransforms();
            }

            AddChild(new Flammable(manager, "Flammable"));
        }
    }
}
